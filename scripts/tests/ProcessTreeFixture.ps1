param([Parameter(Mandatory)] [string] $StatePath)

$child = Start-Process `
    -FilePath 'powershell.exe' `
    -ArgumentList @('-NoProfile', '-Command', 'Start-Sleep -Seconds 120') `
    -WindowStyle Hidden `
    -PassThru
$state = @{ ParentId = $PID; ChildId = $child.Id }
[IO.File]::WriteAllText($StatePath, ($state | ConvertTo-Json))
Wait-Process -Id $child.Id
