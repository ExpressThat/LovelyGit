param([Parameter(Mandatory)] [string] $StatePath)

$scriptsRoot = Split-Path -Parent $PSScriptRoot
Import-Module (Join-Path $scriptsRoot 'ProcessSafety.psm1') -Force
$fixture = Join-Path $PSScriptRoot 'ProcessTreeFixture.ps1'
Invoke-KillOnCloseProcess `
    -FilePath 'powershell.exe' `
    -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $fixture, $StatePath) `
    -WorkingDirectory $PSScriptRoot | Out-Null
