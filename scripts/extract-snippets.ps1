<#
.SYNOPSIS
    Extracts code snippets from KnockOff.Documentation.Samples and updates documentation and skills.

.DESCRIPTION
    This script scans the Documentation.Samples project for #region docs:* and #region skill:* markers,
    extracts the code snippets, and can optionally update the corresponding markdown
    documentation files and Claude skill files.

.PARAMETER Verify
    Only verify that snippets exist and report status. Does not modify any files.

.PARAMETER Update
    Update the markdown documentation and skill files with extracted snippets.

.PARAMETER SamplesPath
    Path to the samples project. Defaults to src/Tests/KnockOff.Documentation.Samples

.PARAMETER DocsPath
    Path to the docs directory. Defaults to docs/

.PARAMETER SkillsPath
    Path to the Claude skills directory. Defaults to $env:USERPROFILE\.claude\skills\knockoff

.EXAMPLE
    .\extract-snippets.ps1 -Verify
    Verifies all snippet markers are valid without modifying files.

.EXAMPLE
    .\extract-snippets.ps1 -Update
    Extracts snippets and updates documentation and skill files.
#>

param(
    [switch]$Verify,
    [switch]$Update,
    [string]$SamplesPath = "src/Tests/KnockOff.Documentation.Samples",
    [string]$SamplesTestsPath = "src/Tests/KnockOff.Documentation.Samples.Tests",
    [string]$DocsPath = "docs",
    [string]$SkillsPath = "$env:USERPROFILE\.claude\skills\knockoff"
)

$ErrorActionPreference = "Stop"

# Get the repository root
$RepoRoot = Split-Path -Parent $PSScriptRoot
$SamplesFullPath = Join-Path $RepoRoot $SamplesPath
$SamplesTestsFullPath = Join-Path $RepoRoot $SamplesTestsPath
$DocsFullPath = Join-Path $RepoRoot $DocsPath
$SkillsFullPath = $SkillsPath

Write-Host "KnockOff Documentation & Skill Snippet Extractor" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Samples Path: $SamplesFullPath"
Write-Host "Samples Tests Path: $SamplesTestsFullPath"
Write-Host "Docs Path: $DocsFullPath"
Write-Host "Skills Path: $SkillsFullPath"
Write-Host ""

# Patterns to match region markers
# #region docs:{doc-file}:{snippet-id} - for documentation
# #region skill:{skill-file}:{snippet-id} - for Claude skills
$docsRegionPattern = '#region\s+docs:([^:\s]+):([^\s]+)'
$skillRegionPattern = '#region\s+skill:([^:\s]+):([^\s]+)'

# Find all C# files in samples and samples tests (excluding obj, bin, Generated)
$samplesFiles = Get-ChildItem -Path $SamplesFullPath -Recurse -Include "*.cs" |
    Where-Object { $_.FullName -notmatch '[\\/](obj|bin|Generated)[\\/]' }

$samplesTestsFiles = @()
if (Test-Path $SamplesTestsFullPath) {
    $samplesTestsFiles = Get-ChildItem -Path $SamplesTestsFullPath -Recurse -Include "*.cs" |
        Where-Object { $_.FullName -notmatch '[\\/](obj|bin|Generated)[\\/]' }
}

$sourceFiles = @($samplesFiles) + @($samplesTestsFiles)

$docsSnippets = @{}
$skillSnippets = @{}
$errors = @()

# Helper function to extract snippet content
function Extract-SnippetContent {
    param(
        [string]$Content,
        [System.Text.RegularExpressions.Match]$Match,
        [string]$FileName
    )

    $afterRegion = $Content.Substring($Match.Index + $Match.Length)
    $endRegionMatch = [regex]::Match($afterRegion, '#endregion')

    if (-not $endRegionMatch.Success) {
        return $null
    }

    $snippetContent = $afterRegion.Substring(0, $endRegionMatch.Index).Trim()
    $snippetContent = $snippetContent -replace '^\s*\r?\n', ''
    $snippetContent = $snippetContent -replace '\r?\n\s*$', ''

    return $snippetContent
}

Write-Host "Scanning source files..." -ForegroundColor Yellow

foreach ($file in $sourceFiles) {
    $content = Get-Content $file.FullName -Raw

    # Find all docs region markers
    $docsMatches = [regex]::Matches($content, $docsRegionPattern)
    foreach ($match in $docsMatches) {
        $docFile = $match.Groups[1].Value
        $snippetId = $match.Groups[2].Value
        $key = "${docFile}:${snippetId}"

        $snippetContent = Extract-SnippetContent -Content $content -Match $match -FileName $file.Name

        if ($null -eq $snippetContent) {
            $errors += "Missing #endregion for docs:'$key' in $($file.Name)"
            continue
        }

        if ($docsSnippets.ContainsKey($key)) {
            $errors += "Duplicate docs snippet key '$key' found in $($file.Name)"
        } else {
            $docsSnippets[$key] = @{
                Content = $snippetContent
                SourceFile = $file.Name
                TargetFile = $docFile
                SnippetId = $snippetId
            }
        }
    }

    # Find all skill region markers
    $skillMatches = [regex]::Matches($content, $skillRegionPattern)
    foreach ($match in $skillMatches) {
        $skillFile = $match.Groups[1].Value
        $snippetId = $match.Groups[2].Value
        $key = "${skillFile}:${snippetId}"

        $snippetContent = Extract-SnippetContent -Content $content -Match $match -FileName $file.Name

        if ($null -eq $snippetContent) {
            $errors += "Missing #endregion for skill:'$key' in $($file.Name)"
            continue
        }

        if ($skillSnippets.ContainsKey($key)) {
            $errors += "Duplicate skill snippet key '$key' found in $($file.Name)"
        } else {
            $skillSnippets[$key] = @{
                Content = $snippetContent
                SourceFile = $file.Name
                TargetFile = $skillFile
                SnippetId = $snippetId
            }
        }
    }
}

Write-Host ""

# Group docs snippets by target file
$byDocsFile = $docsSnippets.GetEnumerator() | Group-Object { $_.Value.TargetFile }

if ($docsSnippets.Count -gt 0) {
    Write-Host "Found $($docsSnippets.Count) docs snippets:" -ForegroundColor Green
    foreach ($group in $byDocsFile | Sort-Object Name) {
        Write-Host "  $($group.Name).md:" -ForegroundColor White
        foreach ($snippet in $group.Group | Sort-Object { $_.Value.SnippetId }) {
            Write-Host "    - $($snippet.Value.SnippetId) ($($snippet.Value.SourceFile))" -ForegroundColor Gray
        }
    }
}

# Group skill snippets by target file
$bySkillFile = $skillSnippets.GetEnumerator() | Group-Object { $_.Value.TargetFile }

if ($skillSnippets.Count -gt 0) {
    Write-Host ""
    Write-Host "Found $($skillSnippets.Count) skill snippets:" -ForegroundColor Green
    foreach ($group in $bySkillFile | Sort-Object Name) {
        Write-Host "  $($group.Name).md:" -ForegroundColor White
        foreach ($snippet in $group.Group | Sort-Object { $_.Value.SnippetId }) {
            Write-Host "    - $($snippet.Value.SnippetId) ($($snippet.Value.SourceFile))" -ForegroundColor Gray
        }
    }
}

if ($docsSnippets.Count -eq 0 -and $skillSnippets.Count -eq 0) {
    Write-Host "No snippets found." -ForegroundColor Yellow
}

if ($errors.Count -gt 0) {
    Write-Host ""
    Write-Host "Errors:" -ForegroundColor Red
    foreach ($error in $errors) {
        Write-Host "  - $error" -ForegroundColor Red
    }
    exit 1
}

# Helper function to verify snippets in a target directory
function Verify-Snippets {
    param(
        [hashtable]$Snippets,
        [string]$TargetPath,
        [string]$MarkerPrefix,
        [string]$Label
    )

    $result = @{
        OutOfSync = @()
        Orphans = @()
        Verified = 0
    }

    $byFile = $Snippets.GetEnumerator() | Group-Object { $_.Value.TargetFile }

    foreach ($group in $byFile) {
        $fileName = "$($group.Name).md"
        $filePath = Get-ChildItem -Path $TargetPath -Recurse -Filter $fileName -ErrorAction SilentlyContinue | Select-Object -First 1

        if (-not $filePath) {
            Write-Host "  Warning: $Label file not found: $fileName" -ForegroundColor Yellow
            continue
        }

        $fileContent = Get-Content $filePath.FullName -Raw

        foreach ($snippet in $group.Group) {
            $snippetId = $snippet.Value.SnippetId
            $expectedContent = $snippet.Value.Content
            $targetFile = $group.Name

            # Pattern to extract current content
            $markerPattern = "<!--\s*snippet:\s*${MarkerPrefix}:${targetFile}:${snippetId}\s*-->\s*\r?\n``````(?:csharp|razor)?\r?\n([\s\S]*?)``````\s*\r?\n<!--\s*/snippet\s*-->"

            if ($fileContent -match $markerPattern) {
                $currentContent = $Matches[1].Trim()
                $expectedTrimmed = $expectedContent.Trim()

                # Normalize line endings for comparison
                $currentNormalized = $currentContent -replace '\r\n', "`n"
                $expectedNormalized = $expectedTrimmed -replace '\r\n', "`n"

                if ($currentNormalized -ne $expectedNormalized) {
                    $result.OutOfSync += "  - ${fileName}: ${snippetId}"
                } else {
                    $result.Verified++
                }
            } else {
                # Snippet exists in samples but no marker in target - track as orphan
                $result.Orphans += "  - ${fileName}: ${snippetId}"
            }
        }
    }

    return $result
}

# Helper function to update snippets in a target directory
function Update-Snippets {
    param(
        [hashtable]$Snippets,
        [string]$TargetPath,
        [string]$MarkerPrefix,
        [string]$Label
    )

    $result = @{
        UpdatedFiles = 0
        UpdatedSnippets = 0
    }

    $byFile = $Snippets.GetEnumerator() | Group-Object { $_.Value.TargetFile }

    foreach ($group in $byFile) {
        $fileName = "$($group.Name).md"
        $filePath = Get-ChildItem -Path $TargetPath -Recurse -Filter $fileName -ErrorAction SilentlyContinue | Select-Object -First 1

        if (-not $filePath) {
            Write-Host "  Warning: $Label file not found: $fileName" -ForegroundColor Yellow
            continue
        }

        $fileContent = Get-Content $filePath.FullName -Raw
        $originalContent = $fileContent
        $fileUpdated = $false

        foreach ($snippet in $group.Group) {
            $snippetId = $snippet.Value.SnippetId
            $snippetContent = $snippet.Value.Content
            $targetFile = $group.Name

            # Pattern to match snippet markers
            $markerPattern = "<!--\s*snippet:\s*${MarkerPrefix}:${targetFile}:${snippetId}\s*-->\s*\r?\n``````(?:csharp|razor)?\r?\n([\s\S]*?)``````\s*\r?\n<!--\s*/snippet\s*-->"

            if ($fileContent -match $markerPattern) {
                $replacement = "<!-- snippet: ${MarkerPrefix}:${targetFile}:${snippetId} -->`n``````csharp`n$snippetContent`n```````n<!-- /snippet -->"
                $fileContent = $fileContent -replace $markerPattern, $replacement
                $result.UpdatedSnippets++
                $fileUpdated = $true
            }
        }

        if ($fileUpdated -and $fileContent -ne $originalContent) {
            Set-Content -Path $filePath.FullName -Value $fileContent -NoNewline
            $result.UpdatedFiles++
            Write-Host "  Updated: $fileName" -ForegroundColor Green
        }
    }

    return $result
}

if ($Verify) {
    Write-Host ""
    $totalVerified = 0
    $totalOrphans = 0
    $hasOutOfSync = $false

    # Verify docs snippets
    if ($docsSnippets.Count -gt 0) {
        Write-Host "Verifying documentation snippets..." -ForegroundColor Yellow
        $docsResult = Verify-Snippets -Snippets $docsSnippets -TargetPath $DocsFullPath -MarkerPrefix "docs" -Label "Docs"

        if ($docsResult.Orphans.Count -gt 0) {
            Write-Host ""
            Write-Host "Orphan docs snippets (in samples but not in docs):" -ForegroundColor Yellow
            foreach ($item in $docsResult.Orphans) {
                Write-Host $item -ForegroundColor Yellow
            }
        }

        if ($docsResult.OutOfSync.Count -gt 0) {
            Write-Host ""
            Write-Host "Documentation out of sync with samples:" -ForegroundColor Red
            foreach ($item in $docsResult.OutOfSync) {
                Write-Host $item -ForegroundColor Red
            }
            $hasOutOfSync = $true
        }

        $totalVerified += $docsResult.Verified
        $totalOrphans += $docsResult.Orphans.Count
    }

    # Verify skill snippets
    if ($skillSnippets.Count -gt 0) {
        Write-Host ""
        Write-Host "Verifying skill snippets..." -ForegroundColor Yellow

        if (-not (Test-Path $SkillsFullPath)) {
            Write-Host "  Warning: Skills directory not found: $SkillsFullPath" -ForegroundColor Yellow
        } else {
            $skillResult = Verify-Snippets -Snippets $skillSnippets -TargetPath $SkillsFullPath -MarkerPrefix "skill" -Label "Skill"

            if ($skillResult.Orphans.Count -gt 0) {
                Write-Host ""
                Write-Host "Orphan skill snippets (in samples but not in skills):" -ForegroundColor Yellow
                foreach ($item in $skillResult.Orphans) {
                    Write-Host $item -ForegroundColor Yellow
                }
            }

            if ($skillResult.OutOfSync.Count -gt 0) {
                Write-Host ""
                Write-Host "Skills out of sync with samples:" -ForegroundColor Red
                foreach ($item in $skillResult.OutOfSync) {
                    Write-Host $item -ForegroundColor Red
                }
                $hasOutOfSync = $true
            }

            $totalVerified += $skillResult.Verified
            $totalOrphans += $skillResult.Orphans.Count
        }
    }

    if ($hasOutOfSync) {
        Write-Host ""
        Write-Host "Run '.\scripts\extract-snippets.ps1 -Update' to sync." -ForegroundColor Yellow
        exit 1
    }

    Write-Host ""
    Write-Host "Verification complete. $totalVerified snippets verified, $totalOrphans orphan snippets." -ForegroundColor Green
    exit 0
}

if ($Update) {
    Write-Host ""
    $totalUpdatedFiles = 0
    $totalUpdatedSnippets = 0

    # Update docs snippets
    if ($docsSnippets.Count -gt 0) {
        Write-Host "Updating documentation files..." -ForegroundColor Yellow
        $docsResult = Update-Snippets -Snippets $docsSnippets -TargetPath $DocsFullPath -MarkerPrefix "docs" -Label "Docs"
        $totalUpdatedFiles += $docsResult.UpdatedFiles
        $totalUpdatedSnippets += $docsResult.UpdatedSnippets
    }

    # Update skill snippets
    if ($skillSnippets.Count -gt 0) {
        Write-Host ""
        Write-Host "Updating skill files..." -ForegroundColor Yellow

        if (-not (Test-Path $SkillsFullPath)) {
            Write-Host "  Warning: Skills directory not found: $SkillsFullPath" -ForegroundColor Yellow
        } else {
            $skillResult = Update-Snippets -Snippets $skillSnippets -TargetPath $SkillsFullPath -MarkerPrefix "skill" -Label "Skill"
            $totalUpdatedFiles += $skillResult.UpdatedFiles
            $totalUpdatedSnippets += $skillResult.UpdatedSnippets
        }
    }

    Write-Host ""
    Write-Host "Update complete. $totalUpdatedFiles files updated, $totalUpdatedSnippets snippets processed." -ForegroundColor Green
}

if (-not $Verify -and -not $Update) {
    Write-Host ""
    Write-Host "Usage:" -ForegroundColor Cyan
    Write-Host "  .\extract-snippets.ps1 -Verify    # Verify snippets without updating"
    Write-Host "  .\extract-snippets.ps1 -Update    # Update documentation and skill files"
}
