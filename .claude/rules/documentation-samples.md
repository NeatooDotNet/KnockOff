---
paths:
  - "src/Tests/KnockOff.Documentation.Samples/**"
---

# Documentation Samples Project

This project contains compiled, tested code samples that feed into markdown documentation via MarkdownSnippets.

## Purpose

Every C# code block in documentation originates from this project. Samples must:
- Compile without errors
- Pass their tests
- Demonstrate real, working KnockOff usage

## Region Naming

Use descriptive, kebab-case names for regions:

```csharp
#region getting-started-standalone-define
// sample code here
#endregion
```

**Good names**: `method-callback-with-args`, `property-onget-basic`, `verify-all-calls`
**Bad names**: `example1`, `code`, `sample`

Region names become the `<!-- snippet: region-name -->` reference in markdown.

## File Organization

- One file per documentation topic: `*Samples.cs`
- Namespace matches the topic: `KnockOff.Documentation.Samples.GettingStarted`
- Related samples grouped in the same file
- Shared types go in `SharedTypes.cs`

## Code Style for Samples

Samples prioritize clarity for documentation readers:

- **Executable code only** - No commented-out examples
- **Self-contained** - Each region should make sense in isolation
- **Realistic** - Use meaningful variable names and domain examples
- **Minimal** - Show one concept per sample, avoid unnecessary complexity

## Test Requirements

Every sample region should be exercised by a test:

```csharp
[Fact]
public void SampleName_Scenario_ExpectedBehavior()
{
    #region sample-name
    // The sample code
    #endregion

    // Assertions verifying the sample works
}
```

Tests ensure samples stay valid as the API evolves.

## Creating New Samples

1. Add region in the appropriate `*Samples.cs` file (or create new file if new topic)
2. Wrap in a test method
3. Add corresponding `<!-- snippet: region-name -->` in the target markdown file
4. Run verification (see below)

## Verification Before Completing Work

After modifying samples, always run:

```bash
dotnet build src/Tests/KnockOff.Documentation.Samples/
dotnet test src/Tests/KnockOff.Documentation.Samples/
dotnet mdsnippets
```

This ensures:
- Samples compile
- Tests pass
- Markdown files are synced with latest sample code
