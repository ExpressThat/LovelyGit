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

$tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
foreach ($directory in Get-ChildItem -LiteralPath $tempRoot -Directory `
        -Filter 'lovelygit-template-process-*') {
    $resolved = [IO.Path]::GetFullPath($directory.FullName)
    if (-not $resolved.StartsWith($tempRoot, [StringComparison]::OrdinalIgnoreCase)) {
        continue
    }
    try {
        $owner = Get-Content (Join-Path $resolved '.owner') -Raw -ErrorAction SilentlyContinue
        $ownerParts = @($owner -split '\|')
        $ownerActive = $false
        if (-not [string]::IsNullOrWhiteSpace($owner)) {
            if ($ownerParts.Count -ne 2) {
                $ownerActive = $true
            } else {
                try {
                    $ownerProcess = Get-Process -Id ([int]$ownerParts[0]) `
                        -ErrorAction SilentlyContinue
                    $ownerActive = $null -ne $ownerProcess -and `
                        $ownerProcess.StartTime.ToUniversalTime().Ticks -eq [long]$ownerParts[1]
                } catch {
                    $ownerActive = $true
                }
            }
        }
        if ($ownerActive) {
            continue
        }
        try {
            [IO.Directory]::Delete($resolved, $true)
        } catch [UnauthorizedAccessException] {
            foreach ($file in [IO.Directory]::EnumerateFiles(
                    $resolved, '*', [IO.SearchOption]::AllDirectories)) {
                [IO.File]::SetAttributes($file, [IO.FileAttributes]::Normal)
            }
            [IO.Directory]::Delete($resolved, $true)
        }
    } catch [IO.IOException] {
        Write-Warning "Could not reclaim test template root $resolved."
    } catch [UnauthorizedAccessException] {
        Write-Warning "Could not reclaim test template root $resolved."
    }
}
if ($exitCode -ne 0) {
    throw "The LovelyGit backend test gate failed with exit code $exitCode."
}
