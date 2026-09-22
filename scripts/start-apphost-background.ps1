# Runs GridPulse's AppHost in the background (no interactive console), output
# redirected to a log file - the unattended counterpart to run-app.ps1, used
# by the "GridPulse AppHost" Scheduled Task (see
# setup-gridpulse-autostart-task.ps1). Same cleanup guarantee as run-app.ps1:
# stop-app.ps1 always runs afterward, whether this exits cleanly, crashes, or
# is killed - so a restart (the Scheduled Task's own retry policy, or the
# next logon) never starts from a state contaminated by the last run.

$ErrorActionPreference = "Stop"
$scriptDir = $PSScriptRoot
$repoRoot = Split-Path -Parent $scriptDir
$appHostDir = Join-Path $repoRoot "src\AppHost"
$logDir = "$env:LOCALAPPDATA\GridPulse\logs"
New-Item -ItemType Directory -Path $logDir -Force | Out-Null
$logPath = Join-Path $logDir "apphost.log"

# Same reasoning as run-app.ps1: captures Postgres's state as of the end of
# the previous session before this one's writes begin, as a background job so
# it never delays the dotnet run below.
$backupJob = Start-Job -FilePath (Join-Path $scriptDir "backup-postgres.ps1") -ArgumentList $repoRoot

Push-Location $appHostDir
try {
    dotnet run --no-launch-profile *>> $logPath
}
finally {
    Pop-Location
    try {
        Wait-Job $backupJob -Timeout 180 | Out-Null
        Receive-Job $backupJob *>> $logPath
    }
    catch {
        "Background backup reported an error: $($_.Exception.Message)" | Out-File -Append $logPath
    }
    finally {
        Remove-Job $backupJob -Force -ErrorAction SilentlyContinue
    }
    & (Join-Path $scriptDir "stop-app.ps1") *>> $logPath
}
