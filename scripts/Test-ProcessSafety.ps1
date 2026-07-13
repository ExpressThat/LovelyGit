$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$testRoot = Join-Path ([IO.Path]::GetTempPath()) "lovelygit-process-safety-$PID"
$fixture = Join-Path $PSScriptRoot 'tests\ProcessTreeFixture.ps1'
$harness = Join-Path $PSScriptRoot 'tests\KillOnCloseHarness.ps1'
Import-Module (Join-Path $PSScriptRoot 'ProcessSafety.psm1') -Force
Import-Module (Join-Path $PSScriptRoot 'VisualTestProcess.psm1') -Force
New-Item -ItemType Directory -Path $testRoot | Out-Null

function Wait-ForFile {
    param([string] $Path)
    $deadline = [DateTime]::UtcNow.AddSeconds(10)
    while (-not (Test-Path -LiteralPath $Path) -and [DateTime]::UtcNow -lt $deadline) {
        Start-Sleep -Milliseconds 50
    }
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Timed out waiting for $Path."
    }
}

function Assert-ProcessStopped {
    param([int] $ProcessId)
    $deadline = [DateTime]::UtcNow.AddSeconds(5)
    while ($null -ne (Get-Process -Id $ProcessId -ErrorAction SilentlyContinue) -and
        [DateTime]::UtcNow -lt $deadline) {
        Start-Sleep -Milliseconds 50
    }
    if ($null -ne (Get-Process -Id $ProcessId -ErrorAction SilentlyContinue)) {
        throw "Process $ProcessId was not stopped."
    }
}

try {
    $timeoutState = Join-Path $testRoot 'timeout.json'
    try {
        Invoke-KillOnCloseProcess `
            -FilePath 'powershell.exe' `
            -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $fixture, $timeoutState) `
            -WorkingDirectory $testRoot `
            -TimeoutSeconds 2 | Out-Null
        throw 'Expected the fixture to time out.'
    } catch [TimeoutException] {
    }
    Wait-ForFile $timeoutState
    $timeoutIds = Get-Content -LiteralPath $timeoutState -Raw | ConvertFrom-Json
    Assert-ProcessStopped $timeoutIds.ParentId
    Assert-ProcessStopped $timeoutIds.ChildId

    $forcedState = Join-Path $testRoot 'forced.json'
    $wrapper = Start-Process `
        -FilePath 'powershell.exe' `
        -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $harness, $forcedState) `
        -WindowStyle Hidden `
        -PassThru
    Wait-ForFile $forcedState
    $forcedIds = Get-Content -LiteralPath $forcedState -Raw | ConvertFrom-Json
    Stop-Process -Id $wrapper.Id -Force
    Assert-ProcessStopped $forcedIds.ParentId
    Assert-ProcessStopped $forcedIds.ChildId

    $exitCode = Invoke-KillOnCloseProcess `
        -FilePath 'powershell.exe' `
        -ArgumentList @('-NoProfile', '-Command', 'exit 7') `
        -WorkingDirectory $testRoot
    if ($exitCode -ne 7) {
        throw "Expected exit code 7, received $exitCode."
    }

    $statePath = Join-Path $testRoot 'visual.json'
    $sleeper = Start-Process `
        -FilePath 'powershell.exe' `
        -ArgumentList @('-NoProfile', '-Command', 'Start-Sleep -Seconds 120') `
        -WindowStyle Hidden `
        -PassThru
    Write-VisualTestState -StatePath $statePath -Process $sleeper -Mode test -DebugPort 0
    $forged = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
    $forged.StartTimeUtcTicks = [long]$forged.StartTimeUtcTicks + 1
    [IO.File]::WriteAllText($statePath, ($forged | ConvertTo-Json))
    if (Stop-TrackedVisualTest -StatePath $statePath) {
        throw 'A forged ownership record was accepted.'
    }
    if ($null -eq (Get-Process -Id $sleeper.Id -ErrorAction SilentlyContinue)) {
        throw 'The unrelated process was stopped by a forged record.'
    }
    Write-VisualTestState -StatePath $statePath -Process $sleeper -Mode test -DebugPort 0
    if (-not (Stop-TrackedVisualTest -StatePath $statePath)) {
        throw 'A valid ownership record was rejected.'
    }
    Assert-ProcessStopped $sleeper.Id
    'PASS: process-tree and visual-test ownership safety checks.'
} finally {
    if ($null -ne $sleeper) {
        Stop-Process -Id $sleeper.Id -Force -ErrorAction SilentlyContinue
    }
    Remove-Item -LiteralPath $testRoot -Recurse -Force -ErrorAction SilentlyContinue
}
