param(
    [switch] $UseDotNetRun,
    [string] $CmgPath = "C:/CMG/CMG.exe",
    [int] $Port = 9333,
    [int] $Width = 1440,
    [int] $Height = 1000
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $repoRoot "artifacts\cmg\lovelygit-smoke"
$suite = Join-Path $repoRoot "qa\cmg\lovelygit-smoke.cmgscript"
$launcher = Join-Path $repoRoot "scripts\Start-LovelyGitVisualTest.ps1"

New-Item -ItemType Directory -Force -Path $artifacts | Out-Null

$launchArgs = @(
    "-NoProfile",
    "-ExecutionPolicy",
    "Bypass",
    "-File",
    $launcher,
    "-Width",
    $Width,
    "-Height",
    $Height
)

if ($UseDotNetRun)
{
    $launchArgs += "-UseDotNetRun"
}

$launchOutput = & powershell @launchArgs
if ($LASTEXITCODE -ne 0)
{
    $launchOutput
    throw "Failed to launch LovelyGit visual test app."
}

$appProcessId = $launchOutput | Select-Object -Last 1

$deadline = (Get-Date).AddSeconds(45)
$jsonUrl = "http://127.0.0.1:$Port/json"
do
{
    try
    {
        $targets = Invoke-WebRequest -UseBasicParsing $jsonUrl | ConvertFrom-Json
        $lovelyGitTarget = $targets | Where-Object { $_.title -eq "LovelyGit" -and $_.url -eq "http://localhost:5000/" } | Select-Object -First 1
        if ($lovelyGitTarget)
        {
            break
        }
    }
    catch
    {
        Start-Sleep -Milliseconds 500
    }
} while ((Get-Date) -lt $deadline)

if (-not $lovelyGitTarget)
{
    throw "LovelyGit WebView2 target was not available at $jsonUrl."
}

& $CmgPath browser app attach --port $Port
if ($LASTEXITCODE -ne 0)
{
    throw "CMG failed to attach to LovelyGit on port $Port."
}

& $CmgPath run $suite `
    --browser-port $Port `
    --report-json (Join-Path $artifacts "report.json") `
    --trace (Join-Path $artifacts "trace") `
    --gif (Join-Path $artifacts "gifs")

$runExitCode = $LASTEXITCODE

& $CmgPath browser --port $Port control events pageErrors listPageErrors
& $CmgPath browser --port $Port control events console listConsole --level error

if ($runExitCode -ne 0)
{
    throw "LovelyGit CMG smoke suite failed with exit code $runExitCode. See $artifacts."
}

"LovelyGit CMG smoke suite passed. Artifacts: $artifacts. App process: $appProcessId"
