---
paths:
  - "docs/**/*.md"
  - "skills/**/*.md"
  - "README.md"
---

# Documentation Code Samples

## Goal

**ALL C# code blocks in markdown (`cs` or `csharp`) must be sourced from compiled, tested code in the Documentation.Samples project.**

This ensures:
- Code examples actually compile and work
- Examples stay in sync with API changes
- Tests catch documentation drift

## Adding New Code Blocks

When adding new C# code to documentation:

1. **Use the docs-code-samples agent** to create samples in `src/Tests/KnockOff.Documentation.Samples/`
2. Add region markers (`#region snippet-name` / `#endregion`) around the sample code
3. Add `<!-- snippet: snippet-name -->` and `<!-- endSnippet -->` markers in the markdown
4. Run `dotnet mdsnippets` to sync

**Never add raw C# code blocks without snippet integration.**

## No Commented Code as Examples

**Do not use commented-out code as examples.**

Bad:
```csharp
// Example usage:
// var stub = new MyStub();
// stub.GetUser.OnCall((id) => new User { Id = id });
// IMyRepo repo = stub;
```

Good:
```csharp
var stub = new MyStub();
stub.GetUser.OnCall((id) => new User { Id = id });
IMyRepo repo = stub;
```

**Comments explaining code are fine.** The issue is commented-out code blocks pretending to be examples.

Commented code is only acceptable when:
- Showing code that intentionally should NOT compile (e.g., "wrong" examples in gotcha sections)
- Showing what NOT to do with a clear "// WRONG:" or "// ERROR:" prefix

## Editing Existing Documentation

When rewriting or editing markdown files with `<!-- snippet: -->` markers:

- **Never remove snippet markers** without explicit instruction
- If rewriting a section, preserve or migrate the snippet reference
- If content changes significantly, update the sample code to match

## Before Major Rewrites

Check if the file has snippets:
```bash
grep -c '<!-- snippet:' <file>
```

If > 0, plan how to preserve or migrate each reference.

## Verification

After documentation changes, run:
```bash
dotnet mdsnippets
dotnet test src/Tests/KnockOff.Documentation.Samples/
```

## Exceptions

These may remain as inline code (no snippet):
- Shell/bash commands
- Single-line API signatures for quick reference
- "Wrong" examples in gotcha/migration sections that intentionally don't compile
