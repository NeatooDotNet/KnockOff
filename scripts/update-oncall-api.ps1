# Script to update test files from old OnCall = pattern to new OnCall() pattern
# Usage: .\update-oncall-api.ps1 -Path "path\to\directory"
# Run with -WhatIf to preview changes without modifying files
#
# Handles both single-line and multi-line lambdas:
# - stub.Method.OnCall = (ko) => result;
# - stub.Method.OnCall = (ko) => { ... };
# - stub.Method.Of<T>().OnCall = (ko) => result;
#
# Does NOT handle delegate stubs (.Interceptor.OnCall) as they still use property-based API

param(
    [string]$Path = "..\src\Tests",
    [switch]$WhatIf
)

# Find all .cs files
$files = Get-ChildItem -Path $Path -Filter "*.cs" -Recurse

$totalReplacements = 0
$filesModified = 0

foreach ($file in $files) {
    $content = Get-Content -Path $file.FullName -Raw

    # Skip delegate stub patterns (.Interceptor.OnCall)
    # We'll handle this by checking each match

    $modified = $false

    # Regex that matches .OnCall = followed by a lambda until the matching };
    # This handles both single-line and multi-line cases
    # Pattern: .OnCall = (params) => expression; OR .OnCall = (params) => { ... };

    # First, handle single-line: .OnCall = (lambda);
    $singleLinePattern = '(?<!\.Interceptor)(\.OnCall)\s*=\s*(\([^)]*\)\s*=>\s*[^{;]+)(;)'
    $matches = [regex]::Matches($content, $singleLinePattern)
    if ($matches.Count -gt 0) {
        $content = [regex]::Replace($content, $singleLinePattern, '$1($2)$3')
        $totalReplacements += $matches.Count
        $modified = $true
    }

    # Second, handle multi-line: .OnCall = (params) => { ... };
    # This uses a more complex pattern with balanced braces
    $multiLinePattern = '(?<!\.Interceptor)(\.OnCall)\s*=\s*(\([^)]*\)\s*=>\s*\{)'
    $multiLineMatches = [regex]::Matches($content, $multiLinePattern)

    foreach ($match in $multiLineMatches) {
        $startIndex = $match.Index
        $endOfMatch = $startIndex + $match.Length

        # Find the matching closing brace and semicolon
        $braceCount = 1
        $i = $endOfMatch
        while ($i -lt $content.Length -and $braceCount -gt 0) {
            $char = $content[$i]
            if ($char -eq '{') { $braceCount++ }
            elseif ($char -eq '}') { $braceCount-- }
            $i++
        }

        # After closing brace, skip whitespace and expect semicolon
        while ($i -lt $content.Length -and $content[$i] -match '\s') { $i++ }

        if ($i -lt $content.Length -and $content[$i] -eq ';') {
            # Extract the full match from startIndex to $i (inclusive of ;)
            $fullMatchLength = $i - $startIndex + 1
            $originalText = $content.Substring($startIndex, $fullMatchLength)

            # Transform: .OnCall = lambda; -> .OnCall(lambda);
            # The lambda is everything between "= " and the final ";"
            $lambdaStart = $originalText.IndexOf('=') + 1
            $lambdaText = $originalText.Substring($lambdaStart).TrimStart()
            $lambdaText = $lambdaText.Substring(0, $lambdaText.Length - 1) # Remove trailing ;

            $prefix = $originalText.Substring(0, $originalText.IndexOf('.OnCall') + 7)
            $newText = "$prefix($lambdaText);"

            $content = $content.Substring(0, $startIndex) + $newText + $content.Substring($startIndex + $fullMatchLength)
            $totalReplacements++
            $modified = $true
        }
    }

    if ($modified) {
        Write-Host "Found matches in $($file.Name)"

        if (-not $WhatIf) {
            Set-Content -Path $file.FullName -Value $content -NoNewline
            Write-Host "  Updated $($file.FullName)" -ForegroundColor Green
            $filesModified++
        }
    }
}

Write-Host ""
if ($WhatIf) {
    Write-Host "Would update $totalReplacements occurrences" -ForegroundColor Yellow
} else {
    Write-Host "Updated $totalReplacements occurrences in $filesModified files" -ForegroundColor Green
}
