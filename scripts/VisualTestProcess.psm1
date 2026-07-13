function Get-VisualTestStatePath {
    param([Parameter(Mandatory)] [string] $RepositoryRoot)
    return Join-Path $RepositoryRoot 'artifacts\.lovelygit-visual-test.json'
}

function Get-ProcessPath {
    param([Parameter(Mandatory)] [Diagnostics.Process] $Process)
    try {
        return [IO.Path]::GetFullPath($Process.Path)
    } catch {
        return $null
    }
}

function Read-VisualTestState {
    param([Parameter(Mandatory)] [string] $StatePath)
    if (-not (Test-Path -LiteralPath $StatePath)) {
        return $null
    }
    try {
        return Get-Content -LiteralPath $StatePath -Raw | ConvertFrom-Json
    } catch {
        return $null
    }
}

function Get-TrackedVisualTestProcess {
    param([Parameter(Mandatory)] [string] $StatePath)
    $state = Read-VisualTestState -StatePath $StatePath
    if ($null -eq $state) {
        return $null
    }
    $process = Get-Process -Id ([int]$state.ProcessId) -ErrorAction SilentlyContinue
    if ($null -eq $process) {
        return $null
    }
    $actualPath = Get-ProcessPath -Process $process
    $actualTicks = $process.StartTime.ToUniversalTime().Ticks
    $pathMatches = $null -ne $actualPath -and
        [string]::Equals($actualPath, [string]$state.ExecutablePath, 'OrdinalIgnoreCase')
    if (-not $pathMatches -or $actualTicks -ne [long]$state.StartTimeUtcTicks) {
        return $null
    }
    return $process
}

function Write-VisualTestState {
    param(
        [Parameter(Mandatory)] [string] $StatePath,
        [Parameter(Mandatory)] [Diagnostics.Process] $Process,
        [Parameter(Mandatory)] [string] $Mode,
        [Parameter(Mandatory)] [int] $DebugPort
    )
    $directory = Split-Path -Parent $StatePath
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
    $state = [ordered]@{
        Schema = 1
        ProcessId = $Process.Id
        StartTimeUtcTicks = $Process.StartTime.ToUniversalTime().Ticks
        ExecutablePath = Get-ProcessPath -Process $Process
        Mode = $Mode
        DebugPort = $DebugPort
    }
    $temporaryPath = "$StatePath.$PID.tmp"
    [IO.File]::WriteAllText($temporaryPath, ($state | ConvertTo-Json))
    Move-Item -LiteralPath $temporaryPath -Destination $StatePath -Force
}

function Remove-StaleVisualTestState {
    param([Parameter(Mandatory)] [string] $StatePath)
    if ($null -eq (Get-TrackedVisualTestProcess -StatePath $StatePath)) {
        Remove-Item -LiteralPath $StatePath -Force -ErrorAction SilentlyContinue
    }
}

function Stop-TrackedVisualTest {
    param([Parameter(Mandatory)] [string] $StatePath)
    $process = Get-TrackedVisualTestProcess -StatePath $StatePath
    if ($null -eq $process) {
        return $false
    }
    $stopper = Start-Process `
        -FilePath 'taskkill.exe' `
        -ArgumentList @('/PID', $process.Id, '/T', '/F') `
        -WindowStyle Hidden `
        -Wait `
        -PassThru
    $deadline = [DateTime]::UtcNow.AddSeconds(5)
    while ($null -ne (Get-Process -Id $process.Id -ErrorAction SilentlyContinue) -and
        [DateTime]::UtcNow -lt $deadline) {
        Start-Sleep -Milliseconds 50
    }
    if ($null -ne (Get-Process -Id $process.Id -ErrorAction SilentlyContinue)) {
        throw "Failed to stop tracked visual-test process $($process.Id)."
    }
    Remove-Item -LiteralPath $StatePath -Force -ErrorAction SilentlyContinue
    return $true
}

function Test-LovelyGitDebugTarget {
    param([int] $DebugPort = 9333)
    try {
        $targets = Invoke-RestMethod `
            -Uri "http://127.0.0.1:$DebugPort/json" `
            -TimeoutSec 1
        return $null -ne ($targets | Where-Object {
            $_.title -eq 'LovelyGit' -and $_.url -eq 'http://localhost:5000/'
        } | Select-Object -First 1)
    } catch {
        return $false
    }
}

Export-ModuleMember -Function @(
    'Get-VisualTestStatePath',
    'Get-TrackedVisualTestProcess',
    'Write-VisualTestState',
    'Remove-StaleVisualTestState',
    'Stop-TrackedVisualTest',
    'Test-LovelyGitDebugTarget'
)
