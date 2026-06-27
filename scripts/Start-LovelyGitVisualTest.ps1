param(
    [switch] $UseDotNetRun
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$appProject = Join-Path $repoRoot "LovelyGit\LovelyGit.csproj"
$appDirectory = Join-Path $repoRoot "LovelyGit"
$compiledApp = Join-Path $appDirectory "bin\Debug\net10.0\win-x64\LovelyGit.exe"

$env:WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS = "--remote-debugging-port=9333"
$env:LOVELYGIT_TEST_WINDOW_OFFSCREEN = "true"

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
    -WindowStyle Minimized `
    -PassThru
$process.Id
