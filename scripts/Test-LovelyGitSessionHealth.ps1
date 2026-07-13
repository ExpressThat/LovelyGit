param([switch] $Json)

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
Import-Module (Join-Path $PSScriptRoot 'VisualTestProcess.psm1') -Force
$statePath = Get-VisualTestStatePath -RepositoryRoot $repoRoot
$trackedVisual = Get-TrackedVisualTestProcess -StatePath $statePath
$processes = @(Get-CimInstance Win32_Process)
$byId = @{}
foreach ($process in $processes) {
    $byId[[int]$process.ProcessId] = $process
}

function Test-DescendantOf {
    param([int] $ProcessId, [int] $AncestorId)
    $visited = @{}
    while ($byId.ContainsKey($ProcessId) -and -not $visited.ContainsKey($ProcessId)) {
        if ($ProcessId -eq $AncestorId) {
            return $true
        }
        $visited[$ProcessId] = $true
        $ProcessId = [int]$byId[$ProcessId].ParentProcessId
    }
    return $false
}

function Test-MissingParent {
    param($Process)
    $parentId = [int]$Process.ParentProcessId
    return $parentId -gt 0 -and -not $byId.ContainsKey($parentId)
}

$escapedRoot = [regex]::Escape($repoRoot)
$testProcesses = @($processes | Where-Object {
    ($_.Name -eq 'dotnet.exe' -and $_.CommandLine -match '\btest\b' -and
        $_.CommandLine -match $escapedRoot) -or
    ($_.Name -eq 'testhost.exe' -and $_.ExecutablePath -match $escapedRoot)
})
$orphanedTests = @($testProcesses | Where-Object { Test-MissingParent $_ })

$trackedId = if ($null -ne $trackedVisual) { $trackedVisual.Id } else { -1 }
$visualApps = @($processes | Where-Object {
    $_.Name -eq 'LovelyGit.exe' -and $_.ExecutablePath -match $escapedRoot
})
$untrackedVisualApps = @($visualApps | Where-Object {
    -not (Test-DescendantOf -ProcessId $_.ProcessId -AncestorId $trackedId)
})
$lovelyGitWebViews = @($processes | Where-Object {
    $_.Name -eq 'msedgewebview2.exe' -and
    $_.CommandLine -match '--webview-exe-name=LovelyGit\.exe'
})
$highHandleProcesses = @(Get-Process -ErrorAction SilentlyContinue | Where-Object {
    $_.HandleCount -ge 10000
} | Sort-Object HandleCount -Descending | Select-Object ProcessName,Id,HandleCount)

$os = Get-CimInstance Win32_OperatingSystem
$summary = [ordered]@{
    Healthy = $testProcesses.Count -eq 0 -and $untrackedVisualApps.Count -eq 0 -and
        $visualApps.Count -le 1
    ActiveTestProcesses = @($testProcesses | ForEach-Object {
        [ordered]@{
            Name = $_.Name
            ProcessId = $_.ProcessId
            ParentProcessId = $_.ParentProcessId
            MissingParent = Test-MissingParent $_
        }
    })
    OrphanedTestCount = $orphanedTests.Count
    TrackedVisualProcessId = if ($trackedId -gt 0) { $trackedId } else { $null }
    VisualAppCount = $visualApps.Count
    UntrackedVisualProcesses = @($untrackedVisualApps | ForEach-Object { $_.ProcessId })
    LovelyGitWebViewCount = $lovelyGitWebViews.Count
    HighHandleProcesses = $highHandleProcesses
    FreeRamGB = [math]::Round($os.FreePhysicalMemory / 1MB, 2)
}

if ($Json) {
    $summary | ConvertTo-Json -Depth 5
} else {
    "LovelyGit session health: $(if ($summary.Healthy) { 'HEALTHY' } else { 'ACTION REQUIRED' })"
    "Active test processes: $($testProcesses.Count) (orphaned roots: $($orphanedTests.Count))"
    "Visual apps: $($visualApps.Count) (untracked: $($untrackedVisualApps.Count))"
    "LovelyGit WebView2 processes: $($lovelyGitWebViews.Count)"
    "Free RAM: $($summary.FreeRamGB) GB"
    foreach ($process in $highHandleProcesses) {
        "WARNING: $($process.ProcessName) ($($process.Id)) has $($process.HandleCount) handles."
    }
}

if (-not $summary.Healthy) {
    exit 1
}
