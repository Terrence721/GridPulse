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

Push-Location $appHostDir
try {
    dotnet run --no-launch-profile
}
finally {
    Pop-Location
    Write-Host ""
    Write-Host "Running cleanup sweep..." -ForegroundColor Cyan
    & (Join-Path $scriptDir "stop-app.ps1")
}
