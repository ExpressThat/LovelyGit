param(
    [switch] $UseDotNetRun,
    [switch] $Restart,
    [int] $Width = 1440,
    [int] $Height = 1000,
    [int] $DebugPort = 9333
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$appProject = Join-Path $repoRoot "LovelyGit\LovelyGit.csproj"
$appDirectory = Join-Path $repoRoot "LovelyGit"
$compiledApp = Join-Path $appDirectory "bin\Debug\net10.0\win-x64\LovelyGit.exe"
$processModule = Join-Path $PSScriptRoot 'VisualTestProcess.psm1'
Import-Module $processModule -Force
$statePath = Get-VisualTestStatePath -RepositoryRoot $repoRoot

$trackedProcess = Get-TrackedVisualTestProcess -StatePath $statePath
if ($Restart -and $null -ne $trackedProcess) {
    Stop-TrackedVisualTest -StatePath $statePath | Out-Null
    $trackedProcess = $null
}
if ($null -ne $trackedProcess -and (Test-LovelyGitDebugTarget -DebugPort $DebugPort)) {
    $trackedProcess.Id
    return
}
if ($null -ne $trackedProcess) {
    Stop-TrackedVisualTest -StatePath $statePath | Out-Null
} else {
    Remove-StaleVisualTestState -StatePath $statePath
    if (Test-LovelyGitDebugTarget -DebugPort $DebugPort) {
        throw "Port $DebugPort belongs to an untracked LovelyGit instance. Stop it manually or use its existing window."
    }
}

$env:WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS = "--remote-debugging-port=$DebugPort"
$env:LOVELYGIT_TEST_WINDOW_WIDTH = $Width
$env:LOVELYGIT_TEST_WINDOW_HEIGHT = $Height
Remove-Item Env:\LOVELYGIT_TEST_WINDOW_OFFSCREEN -ErrorAction SilentlyContinue

if ($UseDotNetRun)
{
    $process = Start-Process `
        -FilePath "dotnet" `
        -ArgumentList @(
            "run",
            "--project",
            $appProject,
            "--launch-profile",
            "http",
            "-p:LovelyGitPublishAsWinExe=true"
        ) `
        -WorkingDirectory $appDirectory `
        -WindowStyle Hidden `
        -PassThru
    Write-VisualTestState `
        -StatePath $statePath `
        -Process $process `
        -Mode 'dotnet-run' `
        -DebugPort $DebugPort
    $process.Id
    return
}

$buildOutput = dotnet build $appProject -p:LovelyGitPublishAsWinExe=true --no-restore --no-incremental --verbosity quiet
if ($LASTEXITCODE -ne 0)
{
    $buildOutput
    throw "Failed to build LovelyGit as WinExe for visual testing."
}

$process = Start-Process `
    -FilePath $compiledApp `
    -WorkingDirectory $appDirectory `
    -PassThru
Write-VisualTestState `
    -StatePath $statePath `
    -Process $process `
    -Mode 'compiled' `
    -DebugPort $DebugPort
$process.Id
