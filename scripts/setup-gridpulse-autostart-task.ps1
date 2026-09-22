# Registers (or re-registers) the "GridPulse AppHost" Windows Scheduled Task,
# which auto-starts the AppHost at logon via start-apphost-background.ps1 -
# independent of VS Code or any editor being open. Run this once, from a real
# elevated terminal (not Claude Code's own shell - registering/modifying a
# Scheduled Task needs a session this tool's process doesn't have access to,
# confirmed directly: Register-ScheduledTask and schtasks both fail with
# "Access is denied" from inside it, even with every workaround tried).
#
# Delay is 5 minutes, not Windows Task Scheduler's own default of none - a
# real incident traced via Windows Event Log showed the Wi-Fi adapter
# resetting repeatedly for several minutes after a fresh boot, destabilizing
# the network stack badly enough to abort the AppHost's own health-check HTTP
# calls mid-flight and kill it silently. 2 minutes wasn't enough buffer; 5
# gives real margin without meaningfully delaying a normal work session.

$ErrorActionPreference = "Stop"
$scriptDir = $PSScriptRoot
$repoRoot = Split-Path -Parent $scriptDir
$wrapperScript = Join-Path $scriptDir "start-apphost-background.ps1"

if (-not (Test-Path $wrapperScript)) {
    throw "start-apphost-background.ps1 not found at $wrapperScript - run this from within the repo's scripts/ folder."
}

$action = New-ScheduledTaskAction `
    -Execute "powershell.exe" `
    -Argument "-NoProfile -ExecutionPolicy Bypass -File `"$wrapperScript`"" `
    -WorkingDirectory $repoRoot

$trigger = New-ScheduledTaskTrigger -AtLogOn
$trigger.Delay = "PT5M"

$settings = New-ScheduledTaskSettingsSet `
    -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries `
    -ExecutionTimeLimit ([TimeSpan]::Zero) `
    -RestartCount 3 `
    -RestartInterval (New-TimeSpan -Minutes 1) `
    -MultipleInstances IgnoreNew

Register-ScheduledTask -TaskName "GridPulse AppHost" `
    -Action $action -Trigger $trigger -Settings $settings `
    -Description "Runs the GridPulse Aspire AppHost (Postgres, Kafka, all services) in the background, starting 5 minutes after logon (a real incident showed 2 minutes wasn't enough buffer for post-boot network instability to settle)." `
    -RunLevel Limited -Force

Write-Host "Registered. Verifying..." -ForegroundColor Cyan
Get-ScheduledTask -TaskName "GridPulse AppHost" | Select-Object TaskName, State
