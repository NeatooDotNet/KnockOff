# Script to revert delegate stub OnCall changes (they still use property-based API)
# Delegate stubs use .Interceptor.OnCall, while interface stubs use .MethodName.OnCall
# Usage: .\revert-delegate-oncall.ps1 -Path "path\to\directory"

param(
    [string]$Path = "..\src\Tests",
    [switch]$WhatIf
)

# Pattern matches: .Interceptor.OnCall(lambda);
# Reverts back to: .Interceptor.OnCall = lambda;
$pattern = '(\.Interceptor\.OnCall)\(([^)]+\)([^;]*));'
$replacement = '$1 = $2$3;'

# Find all .cs files
$files = Get-ChildItem -Path $Path -Filter "*.cs" -Recurse

$totalReplacements = 0
$filesModified = 0

foreach ($file in $files) {
    $content = Get-Content -Path $file.FullName -Raw

    # Check if file contains the pattern
    $matches = [regex]::Matches($content, $pattern)

    if ($matches.Count -gt 0) {
        Write-Host "Found $($matches.Count) matches in $($file.Name)"

        if (-not $WhatIf) {
            $newContent = [regex]::Replace($content, $pattern, $replacement)

            if ($newContent -ne $content) {
                Set-Content -Path $file.FullName -Value $newContent -NoNewline
                Write-Host "  Reverted $($file.FullName)" -ForegroundColor Yellow
                $filesModified++
            }
        }

        $totalReplacements += $matches.Count
    }
}

Write-Host ""
if ($WhatIf) {
    Write-Host "Would revert $totalReplacements occurrences in $($files.Count) files" -ForegroundColor Yellow
} else {
    Write-Host "Reverted $totalReplacements occurrences in $filesModified files" -ForegroundColor Yellow
}
