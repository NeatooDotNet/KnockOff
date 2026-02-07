# Delete all 'nul' files recursively.
# Windows reserves 'nul' as a device name, so standard deletion fails.
# Using the \\?\ extended-length path prefix bypasses reserved name restrictions.

param(
    [string]$Path = ".",
    [switch]$WhatIf
)

$resolvedPath = (Resolve-Path $Path).Path
$nulFiles = Get-ChildItem -Path $resolvedPath -Recurse -Filter "nul" -File -Force -ErrorAction SilentlyContinue

if (-not $nulFiles) {
    Write-Host "No 'nul' files found under $resolvedPath"
    return
}

foreach ($file in $nulFiles) {
    $extendedPath = "\\?\$($file.FullName)"
    if ($WhatIf) {
        Write-Host "Would delete: $($file.FullName)"
    } else {
        try {
            [System.IO.File]::Delete($extendedPath)
            Write-Host "Deleted: $($file.FullName)"
        } catch {
            Write-Warning "Failed to delete: $($file.FullName) - $_"
        }
    }
}
