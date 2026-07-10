$ErrorActionPreference = 'Stop'

$maximumLines = 250
$checkedExtensions = @(
    '.cs',
    '.css',
    '.js',
    '.jsx',
    '.md',
    '.mjs',
    '.ps1',
    '.ts',
    '.tsx'
)
$excludedPrefixes = @(
    'LovelyGit/Frontend/src/components/ui/',
    'LovelyGit/Frontend/src/generated/',
    'LovelyGit/wwwroot/'
)
$excludedFiles = @(
    'LovelyGit/Services/NativeMessaging/NativeMessageType.cs',
    'LovelyGit/Frontend/src/lib/settings/theme/themeCatalog.ts'
)

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$trackedFiles = git -C $repositoryRoot ls-files
$untrackedFiles = git -C $repositoryRoot ls-files --others --exclude-standard
$violations = @(
    @($trackedFiles; $untrackedFiles) |
        Sort-Object -Unique |
        Where-Object {
            $path = $_.Replace('\', '/')
            $extension = [IO.Path]::GetExtension($path).ToLowerInvariant()
            $isExcluded = $excludedPrefixes.Where(
                { $path.StartsWith($_, [StringComparison]::OrdinalIgnoreCase) },
                'First'
            ).Count -gt 0
            $isExcludedFile = $excludedFiles.Contains($path)
            $checkedExtensions.Contains($extension) -and -not $isExcluded -and -not $isExcludedFile
        } |
        ForEach-Object {
            $relativePath = $_.Replace('\', '/')
            $fullPath = Join-Path $repositoryRoot $relativePath
            if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
                return
            }

            $lineCount = (Get-Content -LiteralPath $fullPath).Count
            if ($lineCount -gt $maximumLines) {
                [pscustomobject]@{
                    File = $relativePath
                    Lines = $lineCount
                }
            }
        }
)

if ($violations.Count -eq 0) {
    Write-Output "PASS: all first-party authored files are at or below $maximumLines lines."
    exit 0
}

$violations |
    Sort-Object Lines -Descending |
    Format-Table Lines, File -AutoSize |
    Out-String |
    Write-Output
throw "$($violations.Count) first-party files exceed the $maximumLines-line limit."
