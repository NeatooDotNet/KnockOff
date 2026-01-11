# migrate-regions.ps1
# Converts #region docs:{file}:{id} to #region {file}-{id}
# Converts #region skill:{file}:{id} to #region skill-{file}-{id} (keeps skill- prefix for uniqueness)
param(
    [string]$Path = "src/Tests/KnockOff.Documentation.Samples",
    [switch]$WhatIf
)

$files = Get-ChildItem -Path $Path -Recurse -Include "*.cs"
$converted = 0

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $original = $content

    # Convert: #region docs:{file}:{id} -> #region {file}-{id}
    # Convert: #region skill:{file}:{id} -> #region skill-{file}-{id} (keep skill prefix)
    $content = $content -replace '#region docs:([^:]+):([^\r\n]+)', '#region $1-$2'
    $content = $content -replace '#region skill:([^:]+):([^\r\n]+)', '#region skill-$1-$2'

    if ($content -ne $original) {
        $converted++
        Write-Host "Converting: $($file.FullName)" -ForegroundColor Yellow

        # Show what changed - docs:
        $oldDocsRegions = [regex]::Matches($original, '#region docs:([^:]+):([^\r\n]+)')
        foreach ($match in $oldDocsRegions) {
            $filePrefix = $match.Groups[1].Value
            $id = $match.Groups[2].Value
            Write-Host "  $($match.Value) -> #region $filePrefix-$id" -ForegroundColor Gray
        }

        # Show what changed - skill:
        $oldSkillRegions = [regex]::Matches($original, '#region skill:([^:]+):([^\r\n]+)')
        foreach ($match in $oldSkillRegions) {
            $filePrefix = $match.Groups[1].Value
            $id = $match.Groups[2].Value
            Write-Host "  $($match.Value) -> #region skill-$filePrefix-$id" -ForegroundColor Gray
        }

        if (-not $WhatIf) {
            Set-Content -Path $file.FullName -Value $content -NoNewline
        }
    }
}

Write-Host "`nConverted $converted files" -ForegroundColor Cyan
if ($WhatIf) {
    Write-Host "(WhatIf mode - no files were modified)" -ForegroundColor Yellow
}
