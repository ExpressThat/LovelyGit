param(
    [string] $ArtifactDirectory = "artifacts/performance",
    [string] $CmgPath = "C:/CMG/CMG.exe",
    [int] $Port = 9333,
    [int] $Width = 1440,
    [int] $Height = 1000,
    [switch] $UseDotNetRun,
    [switch] $SkipLaunch,
    [switch] $KeepAppRunning
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$artifactRoot = if ([System.IO.Path]::IsPathRooted($ArtifactDirectory)) {
    $ArtifactDirectory
} else {
    Join-Path $repoRoot $ArtifactDirectory
}
$artifactRoot = [System.IO.Path]::GetFullPath($artifactRoot)
$cmg = if ([System.IO.Path]::IsPathRooted($CmgPath)) {
    [System.IO.Path]::GetFullPath($CmgPath)
} else {
    [System.IO.Path]::GetFullPath((Join-Path $repoRoot $CmgPath))
}

if (-not (Test-Path -LiteralPath $cmg)) {
    throw "CMG was not found at '$cmg'. Pass -CmgPath if it is installed elsewhere."
}

New-Item -ItemType Directory -Force -Path $artifactRoot | Out-Null

$initScript = Join-Path $repoRoot "scripts/performance/lovelygit-perf-init.js"
$journeyScript = Join-Path $artifactRoot "lovelygit-performance-guardrail.cmgscript"
$summaryPath = Join-Path $artifactRoot "summary.json"
$screenshotPath = Join-Path $artifactRoot "final.png"
$tracePath = Join-Path $artifactRoot "journey-trace.json"
$launchedProcessId = $null

function Convert-ToCmgPath([string] $Path) {
    return ([System.IO.Path]::GetFullPath($Path)).Replace("\", "/")
}

function Invoke-Cmg([string[]] $Arguments) {
    & $cmg @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "CMG command failed: $cmg $($Arguments -join ' ')"
    }
}

function Wait-ForWebViewTarget {
    $deadline = [DateTimeOffset]::Now.AddSeconds(45)
    $endpoint = "http://127.0.0.1:$Port/json"
    do {
        try {
            $targets = Invoke-RestMethod -UseBasicParsing -Uri $endpoint -TimeoutSec 2
            $target = @($targets) | Where-Object {
                $_.title -eq "LovelyGit" -and $_.url -eq "http://localhost:5000/"
            } | Select-Object -First 1
            if ($target) {
                return
            }
        } catch {
            Start-Sleep -Milliseconds 500
        }
    } while ([DateTimeOffset]::Now -lt $deadline)

    throw "LovelyGit WebView2 target was not available at $endpoint."
}

try {
    if (-not $SkipLaunch) {
        $startScript = Join-Path $repoRoot "scripts/Start-LovelyGitVisualTest.ps1"
        $startArgs = @(
            "-NoProfile",
            "-ExecutionPolicy",
            "Bypass",
            "-File",
            $startScript,
            "-Width",
            $Width,
            "-Height",
            $Height
        )
        if ($UseDotNetRun) {
            $startArgs += "-UseDotNetRun"
        }

        $launchedProcessId = (& powershell @startArgs | Select-Object -Last 1)
    }

    Wait-ForWebViewTarget
    Invoke-Cmg @("browser", "app", "attach", "--port", "$Port")

    $summaryCmgPath = Convert-ToCmgPath $summaryPath
    $screenshotCmgPath = Convert-ToCmgPath $screenshotPath
    $initCmgPath = Convert-ToCmgPath $initScript

    $journeyTemplate = @'
reload timeout=15000
waitForFunction "document.querySelector('[data-lg-perf=commit-row]')" timeout=15000
addScriptTag path="__INIT_SCRIPT__"
waitForFunction "window.__lovelyGitPerf !== undefined" timeout=5000
evaluate "window.__lovelyGitPerf.mark('beforeScroll')"
wheel "[data-lg-perf=commit-graph-scroll]" deltaY=880
waitForTimeout 500
scrollTo selector="[data-lg-perf=commit-graph-scroll]" top=0
evaluate "window.__lovelyGitPerf.mark('beforeDetailsClick')"
click "[data-lg-perf=commit-row]"
waitForElement "[data-lg-perf=commit-details]" timeout=8000
screenshotPage output="__SCREENSHOT__"
set perfSummary {
  evaluate "window.__lovelyGitPerf.runGuardrail().then((summary) => JSON.stringify(summary, null, 2))"
}
writeFile path="__SUMMARY__" text="${perfSummary}"
'@
    $journeyContent = $journeyTemplate.Replace("__INIT_SCRIPT__", $initCmgPath)
    $journeyContent = $journeyContent.Replace("__SCREENSHOT__", $screenshotCmgPath)
    $journeyContent = $journeyContent.Replace("__SUMMARY__", $summaryCmgPath)
    $journeyContent | Set-Content -LiteralPath $journeyScript -Encoding UTF8

    Invoke-Cmg @(
        "browser",
        "--port",
        "$Port",
        "control",
        "script",
        "--file",
        $journeyScript,
        "--trace",
        $tracePath
    )

    Invoke-Cmg @("browser", "--port", "$Port", "control", "events", "pageErrors", "expectNoPageError", "--timeout", "250")
    Invoke-Cmg @("browser", "--port", "$Port", "control", "events", "console", "expectNoConsole", "--level", "error", "--timeout", "250")

    $summary = Get-Content -LiteralPath $summaryPath -Raw | ConvertFrom-Json
    $misses = @($summary.misses)
    $commandMisses = @($summary.commandMisses)

    Write-Host "LovelyGit performance guardrail summary:"
    foreach ($check in $summary.checks) {
        $actual = if ($null -eq $check.actual) { "missing" } else { $check.actual }
        Write-Host "  $($check.name): $actual $($check.unit) (limit $($check.limit))"
    }
    Write-Host "  command round-trip misses: $($commandMisses.Count)"
    Write-Host "  artifacts: $artifactRoot"

    if ($misses.Count -gt 0 -or $commandMisses.Count -gt 0) {
        exit 1
    }
} finally {
    if ($launchedProcessId -and -not $KeepAppRunning) {
        Stop-Process -Id $launchedProcessId -ErrorAction SilentlyContinue
    }
}
