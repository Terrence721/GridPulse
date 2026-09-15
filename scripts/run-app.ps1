# Runs GridPulse's AppHost in the foreground so Ctrl+C reaches it directly -
# Aspire's own graceful shutdown then tears down its containers/processes
# normally, the same as running `dotnet run` by hand in a real terminal.
#
# stop-app.ps1 always runs afterward as a safety-net sweep, whether this
# exits via a clean Ctrl+C, a crash, or anything else - the piece that was
# actually missing before: no separate cleanup step to remember, and nothing
# left orphaned if the graceful path didn't fully complete.

$ErrorActionPreference = "Stop"
$scriptDir = $PSScriptRoot
$repoRoot = Split-Path -Parent $scriptDir
$appHostDir = Join-Path $repoRoot "src\AppHost"

Write-Host "Starting GridPulse AppHost. Press Ctrl+C to stop (cleanup runs automatically)." -ForegroundColor Cyan

# Captures Postgres's state as of the end of the previous session before this
# one's writes begin - see backup-postgres.ps1 for why startup, not shutdown.
# Runs as a job so it never delays the foreground dotnet run below. -RepoRoot
# is passed explicitly because $PSScriptRoot (and $PSCommandPath, and
# $MyInvocation.MyCommand.Path) are all empty inside a script started via
# Start-Job -FilePath under Windows PowerShell 5.1 - confirmed by direct
# testing, not assumed - so backup-postgres.ps1 cannot reliably locate itself
# when run this way.
$backupJob = Start-Job -FilePath (Join-Path $scriptDir "backup-postgres.ps1") -ArgumentList $repoRoot

Push-Location $appHostDir
try {
    dotnet run --no-launch-profile
}
finally {
    Pop-Location
    Write-Host ""
    # Waited on BEFORE stop-app.ps1 runs below - otherwise stop-app.ps1 can
    # remove the Postgres container out from under a still-in-progress backup
    # if dotnet run exits unusually fast (e.g. an immediate crash). Wrapped in
    # try/catch: a failure surfacing here must never skip stop-app.ps1 below -
    # a real bug once let an unhandled job error abort the rest of this
    # finally block, leaving containers/processes orphaned.
    Write-Host "Waiting for background backup to finish..." -ForegroundColor Cyan
    try {
        Wait-Job $backupJob -Timeout 180 | Out-Null
        Receive-Job $backupJob
    }
    catch {
        Write-Host "Background backup reported an error: $($_.Exception.Message)" -ForegroundColor DarkYellow
    }
    finally {
        Remove-Job $backupJob -Force -ErrorAction SilentlyContinue
    }
    Write-Host "Running cleanup sweep..." -ForegroundColor Cyan
    & (Join-Path $scriptDir "stop-app.ps1")
}
