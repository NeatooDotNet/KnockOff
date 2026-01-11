# migrate-markdown.ps1
# Converts old snippet format to MarkdownSnippets format
# Old: <!-- snippet: docs:{file}:{id} --> ... <!-- /snippet -->
# New: snippet: {file}-{id}
param(
    [string]$Path = "docs",
    [switch]$WhatIf
)

$files = Get-ChildItem -Path $Path -Recurse -Include "*.md" -Exclude "*.source.md"
$converted = 0
$snippetsConverted = 0

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $original = $content

    # Pattern to match old docs snippet blocks:
    # <!-- snippet: docs:{file}:{id} -->
    # ```csharp (or ```cs or ```razor)
    # ... any content ...
    # ```
    # <!-- /snippet -->
    $docsPattern = '(?s)<!-- snippet: docs:([^:]+):([^\s]+)\s*-->\r?\n```(?:csharp|cs|razor)\r?\n.*?```\r?\n<!-- /snippet -->'

    $docsMatches = [regex]::Matches($content, $docsPattern)
    $snippetsConverted += $docsMatches.Count

    # Replace docs: patterns with new format (file-id)
    $content = [regex]::Replace($content, $docsPattern, 'snippet: $1-$2')

    # Pattern to match old skill snippet blocks:
    # <!-- snippet: skill:{file}:{id} -->
    # ```csharp
    # ... any content ...
    # ```
    # <!-- /snippet -->
    $skillPattern = '(?s)<!-- snippet: skill:([^:]+):([^\s]+)\s*-->\r?\n```(?:csharp|cs|razor)\r?\n.*?```\r?\n<!-- /snippet -->'

    $skillMatches = [regex]::Matches($content, $skillPattern)
    $snippetsConverted += $skillMatches.Count

    # Replace skill: patterns with new format (skill-file-id)
    $content = [regex]::Replace($content, $skillPattern, 'snippet: skill-$1-$2')

    if ($content -ne $original) {
        $converted++
        Write-Host "Converting: $($file.FullName) ($($docsMatches.Count + $skillMatches.Count) snippets)" -ForegroundColor Yellow

        if (-not $WhatIf) {
            Set-Content -Path $file.FullName -Value $content -NoNewline
        }
    }
}

Write-Host "`nConverted $snippetsConverted snippets in $converted files" -ForegroundColor Cyan
if ($WhatIf) {
    Write-Host "(WhatIf mode - no files were modified)" -ForegroundColor Yellow
}
