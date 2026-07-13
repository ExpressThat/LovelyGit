param([int] $DebugPort = 9333)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Import-Module (Join-Path $PSScriptRoot 'VisualTestProcess.psm1') -Force
$statePath = Get-VisualTestStatePath -RepositoryRoot $repoRoot

if (Stop-TrackedVisualTest -StatePath $statePath) {
    "Stopped the tracked LovelyGit visual-test process."
    return
}

Remove-StaleVisualTestState -StatePath $statePath
if (Test-LovelyGitDebugTarget -DebugPort $DebugPort) {
    throw "LovelyGit is running on port $DebugPort, but it is not owned by the visual-test helper and was not stopped."
}
"No tracked LovelyGit visual-test process is running."
