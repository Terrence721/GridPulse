# Restores GridPulse's live Postgres data (gridpulsedb/accountsdb) from a
# backup produced by scripts/backup-postgres.ps1. Irreversibly discards
# whatever is currently in those databases - requires -Force. Failures here
# are loud (nonzero exit), unlike the backup script's best-effort silence:
# this is a foreground, user-watched, destructive action.
#
# Same "never pipe the SQL payload through PowerShell" reasoning as the
# backup script: the backup file is transferred into the container via
# `docker cp` and replayed with `psql -f` entirely inside the container.

param(
    [string]$BackupFile,
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$backupDir = Join-Path $repoRoot "backups"
$tempTag = $PID

function Invoke-DockerWithTimeout {
    param(
        [string[]]$ArgumentList,
        [int]$TimeoutSeconds = 15
    )

    $job = Start-Job -ScriptBlock {
        param($a)
        # 2>$null, not 2>&1: merging stderr wraps each stderr line in an
        # ErrorRecord object rather than a plain string, which can silently
        # break string methods called on the result later - see
        # backup-postgres.ps1 for the real failure this caused there.
        $output = & docker @a 2>$null
        # Normalized to @() when null, not left as $null: confirmed by
        # direct testing that Receive-Job corrupts a $null PROPERTY VALUE on
        # a custom object into a non-null, truthy empty PSCustomObject
        # placeholder - the real cause of a "Deserialized...PSCustomObject
        # does not contain a method named 'Trim'" failure, since every
        # `-not $x.Output` guard downstream silently failed to detect the
        # empty case. A directly-returned (unwrapped) $null survives
        # Receive-Job correctly; it's specifically a null value held in an
        # object's property that gets corrupted.
        if ($null -eq $output) { $output = @() }
        [PSCustomObject]@{ Output = $output; ExitCode = $LASTEXITCODE }
    } -ArgumentList (, $ArgumentList)

    $completed = Wait-Job $job -Timeout $TimeoutSeconds

    if (-not $completed) {
        Stop-Job $job
        Remove-Job $job -Force
        return [PSCustomObject]@{ Output = @(); ExitCode = -1; TimedOut = $true }
    }

    $result = Receive-Job $job
    Remove-Job $job -Force
    return [PSCustomObject]@{ Output = $result.Output; ExitCode = $result.ExitCode; TimedOut = $false }
}

function Get-RunningGridPulsePostgresContainer {
    $idsResult = Invoke-DockerWithTimeout -ArgumentList @("ps", "--filter", "label=com.microsoft.developer.usvc-dev.build", "--format", "{{.ID}}")
    if ($idsResult.TimedOut -or $idsResult.ExitCode -ne 0 -or -not $idsResult.Output) { return $null }

    $matches = @()
    foreach ($id in ($idsResult.Output | Where-Object { $_ })) {
        $id = $id.Trim()
        $inspectResult = Invoke-DockerWithTimeout -ArgumentList @("inspect", $id, "--format", "{{.Config.Image}}|{{json .Config.Labels}}")
        if ($inspectResult.TimedOut -or $inspectResult.ExitCode -ne 0 -or -not $inspectResult.Output) { continue }

        $parts = $inspectResult.Output -split '\|', 2
        $image = $parts[0]
        $labels = $parts[1] | ConvertFrom-Json
        $mountsLabel = $labels.'com.microsoft.developer.usvc-dev.mountsLabel'

        if ($image -like "*postgres*" -and $mountsLabel -like "*gridpulse.apphost*") {
            $matches += $id
        }
    }

    if ($matches.Count -eq 0) { return $null }
    if ($matches.Count -gt 1) {
        Write-Host "Found more than one running GridPulse Postgres container - refusing to guess for a destructive restore: $($matches -join ', ')" -ForegroundColor Red
        exit 1
    }
    return $matches[0]
}

# --- Resolve target file ---
if ($BackupFile) {
    if (-not (Test-Path $BackupFile)) {
        Write-Host "Backup file not found: $BackupFile" -ForegroundColor Red
        exit 1
    }
    $resolvedFile = (Resolve-Path $BackupFile).Path
}
else {
    $latest = Get-ChildItem -Path $backupDir -Filter "gridpulse-backup-*.sql" -ErrorAction SilentlyContinue | Sort-Object Name -Descending | Select-Object -First 1
    if (-not $latest) {
        Write-Host "No backups found in $backupDir. Pass -BackupFile explicitly, or run backup-postgres.ps1 first." -ForegroundColor Red
        exit 1
    }
    $resolvedFile = $latest.FullName
}

Write-Host "Selected backup: $resolvedFile" -ForegroundColor Cyan

# --- Destructive-action gate ---
if (-not $Force) {
    Write-Host "This will DROP and recreate gridpulsedb and accountsdb from '$resolvedFile', discarding all current data. Re-run with -Force to proceed." -ForegroundColor Yellow
    exit 1
}

# --- Find the running container (no polling - assumes the app is already up) ---
$containerId = Get-RunningGridPulsePostgresContainer
if (-not $containerId) {
    Write-Host "No running GridPulse Postgres container found. Start the app (scripts/run-app.ps1) first." -ForegroundColor Red
    exit 1
}

# --- Credentials ---
$userResult = Invoke-DockerWithTimeout -ArgumentList @("exec", $containerId, "printenv", "POSTGRES_USER")
$passResult = Invoke-DockerWithTimeout -ArgumentList @("exec", $containerId, "printenv", "POSTGRES_PASSWORD")
$pgUser = ($userResult.Output | Select-Object -First 1)
$pgPassword = ($passResult.Output | Select-Object -First 1)

if (-not $pgUser -or -not $pgPassword) {
    Write-Host "Could not read Postgres credentials from the container." -ForegroundColor Red
    exit 1
}

# --- Terminate other backends holding connections (EF pools) before dropping ---
Write-Host "Terminating other connections to gridpulsedb/accountsdb..." -ForegroundColor Cyan
Invoke-DockerWithTimeout -ArgumentList @("exec", "-e", "PGPASSWORD=$pgPassword", $containerId, "psql", "-U", $pgUser, "-d", "postgres", "-c", "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname IN ('gridpulsedb','accountsdb') AND pid <> pg_backend_pid();") | Out-Null

# --- Copy the backup file in and replay it ---
$remotePath = "/tmp/gp-restore-$tempTag.sql"
Write-Host "Copying backup into the container..." -ForegroundColor Cyan
$cpResult = Invoke-DockerWithTimeout -ArgumentList @("cp", $resolvedFile, "${containerId}:$remotePath") -TimeoutSeconds 60
if ($cpResult.ExitCode -ne 0) {
    Write-Host "Could not copy the backup file into the container: $($cpResult.Output -join ' ')" -ForegroundColor Red
    exit 1
}

Write-Host "Replaying backup..." -ForegroundColor Cyan
$restoreResult = Invoke-DockerWithTimeout -ArgumentList @("exec", "-e", "PGPASSWORD=$pgPassword", $containerId, "psql", "-U", $pgUser, "-d", "postgres", "-v", "ON_ERROR_STOP=1", "-f", $remotePath) -TimeoutSeconds 120

Invoke-DockerWithTimeout -ArgumentList @("exec", $containerId, "rm", "-f", $remotePath) | Out-Null

if ($restoreResult.TimedOut -or $restoreResult.ExitCode -ne 0) {
    Write-Host "Restore failed:" -ForegroundColor Red
    Write-Host ($restoreResult.Output -join "`n")
    exit 1
}

Write-Host "Restore complete from $resolvedFile" -ForegroundColor Green
