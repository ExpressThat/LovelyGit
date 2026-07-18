param(
    [ValidateRange(1, 3600)] [int] $TimeoutSeconds = 90,
    [switch] $Coverage,
    [Parameter(ValueFromRemainingArguments)] [string[]] $AdditionalArguments
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Import-Module (Join-Path $PSScriptRoot 'ProcessSafety.psm1') -Force

$arguments = @('test', (Join-Path $repoRoot 'LovelyGit.slnx'))
if ($Coverage) {
    $arguments += @(
        '--settings', (Join-Path $repoRoot 'coverlet.runsettings'),
        '--collect:XPlat Code Coverage',
        '--results-directory', (Join-Path $repoRoot 'artifacts\coverage\backend')
    )
}
if ($AdditionalArguments) {
    $arguments += $AdditionalArguments
}

$env:LOVELYGIT_PREWARM_PERFORMANCE_TEMPLATES = '1'
$exitCode = Invoke-KillOnCloseProcess `
    -FilePath 'dotnet' `
    -ArgumentList $arguments `
    -WorkingDirectory $repoRoot `
    -TimeoutSeconds $TimeoutSeconds
if ($exitCode -ne 0) {
    throw "The LovelyGit backend test gate failed with exit code $exitCode."
}
