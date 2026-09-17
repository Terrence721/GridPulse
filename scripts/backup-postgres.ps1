# Backs up GridPulse's live Postgres data (every real database in the
# cluster, discovered dynamically below - not the static UCI reference
# dataset, which already has its own separate refresh mechanism). Runs
# from run-app.ps1 as a background job at the start of each dev session,
# capturing the previous session's ending state before this session's
# writes begin - see todo.md for why
# startup, not shutdown. Also safe to run manually at any time.
#
# Best-effort by design: a missed backup must never break the dev loop, so
# every failure path here logs clearly and exits 0 rather than throwing.
#
# Dumps per-database (pg_dump --create --clean --if-exists), not pg_dumpall.
# pg_dumpall --clean emits DROP ROLE/DROP DATABASE for the very superuser and
# maintenance database you're connected as/to when restoring into the same
# live cluster it came from - Postgres refuses both, a long-standing Postgres
# rough edge, not a hypothetical. Per-database dumps never touch the
# "postgres" role or database at all, sidestepping the problem entirely.
#
# The dump payload never touches a PowerShell pipeline: pg_dump writes inside
# the container's own filesystem and a single `docker cp` streams the result
# out byte-exact, avoiding both the text-pipeline re-encoding risk and the
# stdout/stdin deadlock risk of piping a large, statement-heavy SQL file
# through `docker exec -i`.

# $RepoRoot must come first and be passed explicitly when this script is
# started via `Start-Job -FilePath` (as run-app.ps1 does) - $PSScriptRoot,
# $PSCommandPath, and $MyInvocation.MyCommand.Path are all empty inside a
# Start-Job -FilePath context under Windows PowerShell 5.1, confirmed by
# direct testing, not assumed. Falls back to self-location for direct/manual
# invocation, where $PSScriptRoot works normally.
param(
    [string]$RepoRoot,
    [int]$ReadyTimeoutSeconds = 120,
    [int]$RetentionCount = 7
)

$ErrorActionPreference = "Stop"
if (-not $RepoRoot) {
    $RepoRoot = Split-Path -Parent $PSScriptRoot
}
$backupDir = Join-Path $RepoRoot "backups"
$tempTag = $PID

function Invoke-DockerWithTimeout {
    param(
        [string[]]$ArgumentList,
        [int]$TimeoutSeconds = 15
    )

    $job = Start-Job -ScriptBlock {
        param($a)
        # 2>$null, not 2>&1: merging stderr wraps each stderr line in an
        # ErrorRecord object rather than a plain string, and once that
        # crosses this job boundary (and, when invoked from run-app.ps1,
        # a second outer job boundary too) it stops behaving like a string -
        # a real "Deserialized...PSCustomObject does not contain a method
        # named 'Trim'" failure this caused. None of this script's parsing
        # needs docker's stderr text; the exit code alone tells us success.
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

    foreach ($id in ($idsResult.Output | Where-Object { $_ })) {
        $id = $id.Trim()
        $inspectResult = Invoke-DockerWithTimeout -ArgumentList @("inspect", $id, "--format", "{{.Config.Image}}|{{json .Config.Labels}}")
        if ($inspectResult.TimedOut -or $inspectResult.ExitCode -ne 0 -or -not $inspectResult.Output) { continue }

        $parts = $inspectResult.Output -split '\|', 2
        $image = $parts[0]
        $labels = $parts[1] | ConvertFrom-Json
        $mountsLabel = $labels.'com.microsoft.developer.usvc-dev.mountsLabel'

        if ($image -like "*postgres*" -and $mountsLabel -like "*gridpulse.apphost*") {
            return $id
        }
    }

    return $null
}

try {
    Write-Host "Looking for a running GridPulse Postgres container..." -ForegroundColor Cyan
    $containerId = $null
    $deadline = (Get-Date).AddSeconds($ReadyTimeoutSeconds)

    while ((Get-Date) -lt $deadline) {
        $containerId = Get-RunningGridPulsePostgresContainer
        if ($containerId) {
            $userResult = Invoke-DockerWithTimeout -ArgumentList @("exec", $containerId, "printenv", "POSTGRES_USER")
            $pgUser = if ($userResult.ExitCode -eq 0) { ($userResult.Output | Select-Object -First 1) } else { $null }

            if ($pgUser) {
                $readyResult = Invoke-DockerWithTimeout -ArgumentList @("exec", $containerId, "pg_isready", "-U", $pgUser)
                if ($readyResult.ExitCode -eq 0) { break }
            }
        }
        Start-Sleep -Seconds 3
    }

    if (-not $containerId) {
        Write-Host "No GridPulse Postgres container became ready within ${ReadyTimeoutSeconds}s - skipping backup." -ForegroundColor DarkYellow
        exit 0
    }

    $userResult = Invoke-DockerWithTimeout -ArgumentList @("exec", $containerId, "printenv", "POSTGRES_USER")
    $passResult = Invoke-DockerWithTimeout -ArgumentList @("exec", $containerId, "printenv", "POSTGRES_PASSWORD")
    $pgUser = ($userResult.Output | Select-Object -First 1)
    $pgPassword = ($passResult.Output | Select-Object -First 1)

    if (-not $pgUser -or -not $pgPassword) {
        Write-Host "Could not read Postgres credentials from the container - skipping backup." -ForegroundColor DarkYellow
        exit 0
    }

    $dbListResult = Invoke-DockerWithTimeout -ArgumentList @("exec", "-e", "PGPASSWORD=$pgPassword", $containerId, "psql", "-U", $pgUser, "-d", "postgres", "-t", "-A", "-c", "SELECT datname FROM pg_database WHERE datistemplate = false AND datname <> 'postgres';")
    if ($dbListResult.ExitCode -ne 0 -or -not $dbListResult.Output) {
        Write-Host "Could not list databases to back up - skipping backup." -ForegroundColor DarkYellow
        exit 0
    }
    $databases = $dbListResult.Output | Where-Object { $_ -and $_.Trim() } | ForEach-Object { $_.Trim() }

    if (-not $databases) {
        Write-Host "No databases found to back up." -ForegroundColor DarkGray
        exit 0
    }

    Write-Host "Backing up: $($databases -join ', ')" -ForegroundColor Cyan

    $dumpFailed = $false
    foreach ($db in $databases) {
        $dumpFile = "/tmp/gp-$db-$tempTag.sql"
        $dumpResult = Invoke-DockerWithTimeout -ArgumentList @("exec", "-e", "PGPASSWORD=$pgPassword", $containerId, "pg_dump", "-U", $pgUser, "--clean", "--if-exists", "--create", "-d", $db, "-f", $dumpFile) -TimeoutSeconds 120
        if ($dumpResult.ExitCode -ne 0) {
            Write-Host "pg_dump failed for '$db': $($dumpResult.Output -join ' ')" -ForegroundColor DarkYellow
            $dumpFailed = $true
        }
    }

    if ($dumpFailed) {
        Invoke-DockerWithTimeout -ArgumentList @("exec", $containerId, "sh", "-c", "rm -f /tmp/gp-*-$tempTag.sql") | Out-Null
        Write-Host "One or more databases failed to dump - skipping this backup." -ForegroundColor DarkYellow
        exit 0
    }

    $combinedPath = "/tmp/gp-backup-$tempTag.sql"
    $catResult = Invoke-DockerWithTimeout -ArgumentList @("exec", $containerId, "sh", "-c", "cat /tmp/gp-*-$tempTag.sql > $combinedPath") -TimeoutSeconds 60
    if ($catResult.ExitCode -ne 0) {
        Invoke-DockerWithTimeout -ArgumentList @("exec", $containerId, "sh", "-c", "rm -f /tmp/gp-*-$tempTag.sql") | Out-Null
        Write-Host "Could not combine per-database dumps - skipping this backup." -ForegroundColor DarkYellow
        exit 0
    }

    if (-not (Test-Path $backupDir)) {
        New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
    }

    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $localFile = Join-Path $backupDir "gridpulse-backup-$timestamp.sql"

    $cpResult = Invoke-DockerWithTimeout -ArgumentList @("cp", "${containerId}:$combinedPath", $localFile) -TimeoutSeconds 60
    Invoke-DockerWithTimeout -ArgumentList @("exec", $containerId, "sh", "-c", "rm -f /tmp/gp-*-$tempTag.sql") | Out-Null

    if ($cpResult.ExitCode -ne 0 -or -not (Test-Path $localFile)) {
        Write-Host "Could not copy the backup out of the container - skipping this backup." -ForegroundColor DarkYellow
        exit 0
    }

    $fileInfo = Get-Item $localFile
    $trailerCount = @(Select-String -Path $localFile -Pattern "-- PostgreSQL database dump complete" -SimpleMatch).Count

    if ($fileInfo.Length -lt 100 -or $trailerCount -lt $databases.Count) {
        Write-Host "Backup file looks incomplete (size $($fileInfo.Length) bytes, $trailerCount/$($databases.Count) dump-complete markers) - leaving it for inspection but treating this backup as failed." -ForegroundColor DarkYellow
        exit 0
    }

    Write-Host "Backup complete: $localFile ($($fileInfo.Length) bytes, $($databases.Count) database(s))" -ForegroundColor Green

    $existing = Get-ChildItem -Path $backupDir -Filter "gridpulse-backup-*.sql" | Sort-Object Name -Descending
    if ($existing.Count -gt $RetentionCount) {
        $toDelete = $existing | Select-Object -Skip $RetentionCount
        foreach ($old in $toDelete) {
            Remove-Item $old.FullName -Force
            Write-Host "Pruned old backup: $($old.Name)" -ForegroundColor DarkGray
        }
    }
}
catch {
    Write-Host "Backup failed unexpectedly ($($_.Exception.Message)) - continuing without a backup." -ForegroundColor DarkYellow
    exit 0
}
