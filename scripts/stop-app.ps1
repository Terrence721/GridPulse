# Safety-net cleanup for GridPulse's AppHost: finds and stops every Docker
# container and OS process it spawned, even if the AppHost itself crashed or
# was force-killed without a chance to tear them down cleanly. Also runs
# automatically from run-app.ps1's own shutdown path.
#
# Process identification is Docker-independent: every dotnet/node process
# GridPulse's AppHost spawns, plus the AppHost's own native executable
# (GridPulse.AppHost.exe), has this repo's own path in its command line
# (e.g. `dotnet run --project ...\GridPulse\src\AccountCustomer\...`), so
# matching on that directly finds the right processes even if Docker itself
# isn't reachable - the scenario where cleanup matters most, and one an
# earlier version of this script got backwards (it derived which processes
# to kill FROM the containers, so a broken Docker daemon silently skipped
# process cleanup too).
#
# The AppHost's own native exe is matched by name explicitly, not just
# folded into the dotnet/node walk below: it sits ABOVE dcp.exe in the real
# process tree (it's what launches dcp.exe), and that parent link is
# frequently already broken at the OS level by the time this script runs -
# so no amount of walking up from a dotnet/node anchor or down from a
# dcp.exe root can ever reach it. It has to be its own anchor.
#
# Container cleanup still uses Aspire's own "gridpulse.apphost" volume-mount
# label plus the "creatorProcessId" label shared by every container from one
# AppHost run, since several other Aspire-based projects on this machine use
# the same resource names (kafka/postgres) and name matching alone isn't
# safe. Every docker call runs with its own timeout so an unresponsive
# Docker daemon can't hang the whole script - that doesn't fix the daemon,
# but it does mean this script always finishes rather than hanging forever.

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

function Invoke-DockerWithTimeout {
    param(
        [string[]]$ArgumentList,
        [int]$TimeoutSeconds = 15
    )

    $job = Start-Job -ScriptBlock { param($a) & docker @a } -ArgumentList (, $ArgumentList)
    $completed = Wait-Job $job -Timeout $TimeoutSeconds

    if (-not $completed) {
        Write-Host "docker $($ArgumentList -join ' ') timed out after ${TimeoutSeconds}s" -ForegroundColor DarkYellow
        Stop-Job $job
        Remove-Job $job -Force
        return $null
    }

    $result = Receive-Job $job
    Remove-Job $job -Force
    return $result
}

# --- Process sweep (works with or without Docker) ---
try {
    $allProcesses = Get-CimInstance Win32_Process
}
catch {
    Write-Host "Could not enumerate processes ($($_.Exception.Message)) - skipping process cleanup" -ForegroundColor DarkYellow
    $allProcesses = $null
}

if ($allProcesses) {
    try {
        $repoRootLower = $repoRoot.ToLowerInvariant()
        $anchors = $allProcesses | Where-Object {
            ($_.Name -eq "dotnet.exe" -or $_.Name -eq "node.exe" -or $_.Name -like "GridPulse.*.exe") -and
            $_.CommandLine -and
            $_.CommandLine.ToLowerInvariant().Contains($repoRootLower)
        }

        if (-not $anchors) {
            Write-Host "No GridPulse processes found." -ForegroundColor DarkGray
        }
        else {
            $byPid = @{}
            foreach ($p in $allProcesses) { $byPid[[int]$p.ProcessId] = $p }

            # Walk each anchor's ancestry up to find its governing dcp.exe (if
            # any), then walk back down from there too, so plumbing/sibling
            # processes dcp spawned alongside the anchor get caught as well,
            # not just the anchor itself.
            $dcpRoots = [System.Collections.Generic.HashSet[int]]::new()
            foreach ($anchor in $anchors) {
                $current = $anchor
                while ($current) {
                    if ($current.Name -eq "dcp.exe") {
                        [void]$dcpRoots.Add([int]$current.ProcessId)
                        break
                    }
                    $parentId = [int]$current.ParentProcessId
                    if ($byPid.ContainsKey($parentId)) {
                        $current = $byPid[$parentId]
                    }
                    else {
                        $current = $null
                    }
                }
            }

            $toKill = [System.Collections.Generic.HashSet[int]]::new()
            foreach ($anchor in $anchors) { [void]$toKill.Add([int]$anchor.ProcessId) }

            foreach ($rootId in $dcpRoots) {
                $queue = [System.Collections.Generic.Queue[int]]::new()
                $queue.Enqueue($rootId)
                while ($queue.Count -gt 0) {
                    $processId = $queue.Dequeue()
                    [void]$toKill.Add($processId)
                    $allProcesses | Where-Object { $_.ParentProcessId -eq $processId } | ForEach-Object {
                        $queue.Enqueue([int]$_.ProcessId)
                    }
                }
            }

            Write-Host "Stopping $($toKill.Count) GridPulse process(es)..." -ForegroundColor Cyan
            foreach ($processId in $toKill) {
                Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
            }
        }
    }
    catch {
        Write-Host "Process sweep failed partway ($($_.Exception.Message))" -ForegroundColor DarkYellow
    }
}

# --- Container sweep (independent of the process sweep above) ---
function Get-DcpManagedContainers {
    $idsRaw = Invoke-DockerWithTimeout -ArgumentList @("ps", "-a", "--filter", "label=com.microsoft.developer.usvc-dev.build", "--format", "{{.ID}}")
    if (-not $idsRaw) { return @() }

    $idsRaw | Where-Object { $_ } | ForEach-Object {
        $id = $_.Trim()
        $labelsJson = Invoke-DockerWithTimeout -ArgumentList @("inspect", $id, "--format", "{{json .Config.Labels}}")
        if (-not $labelsJson) { return }
        $labels = $labelsJson | ConvertFrom-Json
        [PSCustomObject]@{
            Id               = $id
            MountsLabel      = $labels.'com.microsoft.developer.usvc-dev.mountsLabel'
            CreatorProcessId = $labels.'com.microsoft.developer.usvc-dev.creatorProcessId'
        }
    }
}

try {
    $allDcpContainers = Get-DcpManagedContainers
}
catch {
    Write-Host "Docker isn't reachable - skipping container cleanup ($($_.Exception.Message))" -ForegroundColor DarkYellow
    $allDcpContainers = @()
}

$gridPulseAnchorContainers = $allDcpContainers | Where-Object { $_.MountsLabel -like "*gridpulse.apphost*" }

if (-not $gridPulseAnchorContainers) {
    Write-Host "No GridPulse containers found." -ForegroundColor DarkGray
}
else {
    $creatorIds = $gridPulseAnchorContainers.CreatorProcessId | Sort-Object -Unique
    $toRemove = $allDcpContainers | Where-Object { $_.CreatorProcessId -in $creatorIds }

    Write-Host "Stopping $($toRemove.Count) GridPulse container(s)..." -ForegroundColor Cyan
    foreach ($c in $toRemove) {
        Invoke-DockerWithTimeout -ArgumentList @("stop", $c.Id) | Out-Null
        Invoke-DockerWithTimeout -ArgumentList @("rm", $c.Id) | Out-Null
    }
}

Write-Host "Cleanup complete." -ForegroundColor Green
