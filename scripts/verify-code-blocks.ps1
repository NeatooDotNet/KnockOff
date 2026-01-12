# verify-code-blocks.ps1
# Verifies all C# code blocks have appropriate markers
param(
    [string]$DocsPath = "docs",
    [switch]$Verbose
)

# Directories to exclude from verification (matches mdsnippets.json)
$excludeDirs = @("todos", "release-notes", "design")

$errors = @()
$stats = @{
    Files = 0
    CompiledSnippets = 0
    PseudoSnippets = 0
    InvalidSnippets = 0
    GeneratedSnippets = 0
    Unmarked = 0
}

# Get all markdown files: README.md in root + docs folder
$mdFiles = @()
if (Test-Path "README.md") {
    $mdFiles += Get-Item "README.md"
}
$mdFiles += Get-ChildItem -Path $DocsPath -Recurse -Include "*.md" -Exclude "*.source.md" | Where-Object {
    $path = $_.FullName
    $excluded = $false
    foreach ($dir in $excludeDirs) {
        if ($path -match "[\\/]$dir[\\/]") {
            $excluded = $true
            break
        }
    }
    -not $excluded
}

$mdFiles | ForEach-Object {
    $file = $_
    $stats.Files++
    $content = Get-Content $file.FullName -Raw
    $lines = Get-Content $file.FullName

    # Count snippet types
    # MarkdownSnippets format: snippet: {id}
    $stats.CompiledSnippets += ([regex]'(?m)^snippet:\s+\S+').Matches($content).Count
    # Also count generated snippets: <!-- snippet: {id} --> (output from mdsnippets)
    $generatedSnippets = ([regex]'<!-- snippet: .+ -->').Matches($content)
    $stats.CompiledSnippets += $generatedSnippets.Count

    # Count manual markers
    $stats.PseudoSnippets += ([regex]'<!-- pseudo:').Matches($content).Count
    $stats.InvalidSnippets += ([regex]'<!-- invalid:').Matches($content).Count
    $stats.GeneratedSnippets += ([regex]'<!-- generated:').Matches($content).Count

    # Check for unclosed pseudo/invalid snippets
    $pseudoOpens = ([regex]'<!-- pseudo:').Matches($content).Count
    $invalidOpens = ([regex]'<!-- invalid:').Matches($content).Count
    $generatedOpens = ([regex]'<!-- generated:').Matches($content).Count
    $manualCloses = ([regex]'<!-- /snippet -->').Matches($content).Count

    if (($pseudoOpens + $invalidOpens + $generatedOpens) -ne $manualCloses) {
        $errors += "$($file.Name): Unclosed snippet (pseudo:$pseudoOpens + invalid:$invalidOpens + generated:$generatedOpens opens, $manualCloses closes)"
    }

    # Find unmarked code blocks
    $lineNum = 0
    $inManagedSnippet = $false

    foreach ($line in $lines) {
        $lineNum++

        # Track managed snippets (MarkdownSnippets output or manual pseudo/invalid/generated)
        if ($line -match '^snippet:\s+\S+' -or $line -match '<!-- snippet:' -or $line -match '<!-- (pseudo|invalid|generated):') {
            $inManagedSnippet = $true
        }
        if ($line -match '<!-- endSnippet -->' -or $line -match '<!-- /snippet -->') {
            $inManagedSnippet = $false
        }

        # Find ```csharp or ```cs blocks
        if ($line -match '^```(csharp|cs)') {
            if (-not $inManagedSnippet) {
                # Check if previous line has any snippet marker
                $prevLine = if ($lineNum -gt 1) { $lines[$lineNum - 2] } else { "" }
                if ($prevLine -notmatch 'snippet:' -and $prevLine -notmatch '<!-- (pseudo|invalid|generated):') {
                    $stats.Unmarked++
                    $errors += "$($file.Name):$lineNum - Unmarked C# code block"
                }
            }
        }
    }
}

# Output results
Write-Host "`n=== Code Block Verification ===" -ForegroundColor Cyan
Write-Host "Files scanned: $($stats.Files)"
Write-Host "Compiled snippets (MarkdownSnippets): $($stats.CompiledSnippets)" -ForegroundColor Green
Write-Host "Pseudo-code blocks: $($stats.PseudoSnippets)" -ForegroundColor Yellow
Write-Host "Invalid/anti-pattern blocks: $($stats.InvalidSnippets)" -ForegroundColor Yellow
Write-Host "Generated code blocks: $($stats.GeneratedSnippets)" -ForegroundColor Yellow
Write-Host "Unmarked blocks: $($stats.Unmarked)" -ForegroundColor $(if ($stats.Unmarked -gt 0) { 'Red' } else { 'Green' })

if ($errors) {
    Write-Host "`nErrors found:" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    exit 1
} else {
    Write-Host "`nAll code blocks are properly marked" -ForegroundColor Green
    exit 0
}
