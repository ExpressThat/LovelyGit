$type = 'LovelyGit.Tools.KillOnCloseProcess' -as [type]
if ($null -eq $type) {
    Add-Type -Path (Join-Path $PSScriptRoot 'ProcessTreeJob.cs')
}

function Invoke-KillOnCloseProcess {
    param(
        [Parameter(Mandatory)] [string] $FilePath,
        [Parameter(Mandatory)] [string[]] $ArgumentList,
        [Parameter(Mandatory)] [string] $WorkingDirectory,
        [int] $TimeoutSeconds = 0
    )

    $owned = [LovelyGit.Tools.KillOnCloseProcess]::Start(
        $FilePath,
        $ArgumentList,
        $WorkingDirectory)
    try {
        $timeoutMilliseconds = if ($TimeoutSeconds -gt 0) {
            [Math]::Min($TimeoutSeconds * 1000L, [int]::MaxValue)
        } else {
            -1
        }
        if (-not $owned.WaitForExit([int]$timeoutMilliseconds)) {
            throw [TimeoutException]::new(
                "Process $($owned.Id) exceeded the $TimeoutSeconds second timeout. Its process tree was terminated.")
        }
        return $owned.ExitCode
    } finally {
        $owned.Dispose()
    }
}

Export-ModuleMember -Function Invoke-KillOnCloseProcess
