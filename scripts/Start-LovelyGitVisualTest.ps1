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
    Start-Process `
        -FilePath "dotnet" `
        -ArgumentList @("run", "--project", $appProject, "--launch-profile", "http") `
        -WorkingDirectory $appDirectory `
        -WindowStyle Minimized `
        -PassThru
    return
}

if (-not (Test-Path $compiledApp))
{
    throw "Compiled app not found at $compiledApp. Run dotnet build LovelyGit\LovelyGit.csproj first."
}

Start-Process `
    -FilePath $compiledApp `
    -WorkingDirectory $appDirectory `
    -WindowStyle Minimized `
    -PassThru
