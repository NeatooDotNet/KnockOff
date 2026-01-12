# Update docs-snippets Skill: Marker Definitions

## Task

Update the `/docs-snippets` skill to include clear, detailed definitions of each code block marker type. The current lack of clarity led to incorrect categorization during an audit.

## Problem

During an audit of 67 "pseudo-code" blocks, many were incorrectly marked as pseudo-code when they were actually compilable code that should be in docs/samples. The confusion stemmed from:

1. Treating "code from a different library" (Moq) as pseudo-code - **wrong**
2. Treating "code that isn't in samples yet" as pseudo-code - **wrong**
3. No clear definition of what each marker type means

## Marker Types to Document

### `<!-- snippet: {id} -->` - Synced from Compiled Samples

**Definition:** Code that is compiled and tested in the `docs/samples` project, then synced to documentation via MarkdownSnippets.

**When to use:**
- ANY code that demonstrates real API usage (KnockOff, Moq, or any library)
- Code that a user might copy and adapt
- Examples showing correct usage patterns
- Comparison examples between libraries

**Key principle:** If the code is compilable, it should be compiled. This catches errors before they reach documentation.

**Examples:**
- KnockOff stub definitions and usage
- Moq setup/verify patterns (with Moq package reference in samples)
- Any C# that could be pasted into a real project

---

### `<!-- invalid: {id} -->` - Intentionally Broken Code

**Definition:** Code that intentionally shows errors, wrong patterns, or code that triggers diagnostics.

**When to use:**
- Diagnostic documentation showing what triggers KO1001, KO0010, etc.
- "Don't do this" anti-pattern examples
- Code demonstrating compile errors
- Examples with intentional mistakes

**Key principle:** The code is meant to NOT work. It illustrates what to avoid.

**Examples:**
- `[KnockOff<MyStruct>]` - triggers KO1001
- `public class BadStub : IService, IRepository` - triggers KO0010
- Wrong access modifiers, missing partial keyword, etc.

---

### `<!-- pseudo-code: {description} -->` - Truly Illustrative Only

**Definition:** Code fragments or conceptual illustrations that are NOT meant to be compiled because they are inherently incomplete or hypothetical.

**When to use - VERY LIMITED:**
1. **Incomplete fragments** - Property names or API references without complete statements
   ```csharp
   knockOff.Indexer         // just showing the property exists
   knockOff.Indexer.Backing // just showing the property exists
   ```

2. **Hypothetical/future features** - Features explicitly marked as not yet implemented
   ```csharp
   // Hypothetical - NOT YET IMPLEMENTED
   [KnockOff(Strict = true)]
   ```

3. **Generated code illustrations** - Showing the shape of what a generator produces (though consider syncing from actual Generated/ files instead)

**When NOT to use:**
- Code from other libraries (Moq, NSubstitute, etc.) - these ARE compilable
- Code that "just isn't in samples yet" - add it to samples
- Complete, valid C# statements - compile them
- Usage examples - compile them

**Key principle:** If you could paste it into a .cs file and it would compile (with appropriate usings/references), it's NOT pseudo-code.

---

## Decision Flowchart

```
Is this code intentionally broken/wrong?
├── YES → Use <!-- invalid: -->
└── NO ↓

Is this a complete, compilable C# statement or block?
├── YES → Use <!-- snippet: --> (add to docs/samples)
└── NO ↓

Is this just an API reference fragment or hypothetical feature?
├── YES → Use <!-- pseudo-code: -->
└── NO → Reconsider - probably should be a snippet
```

## Changes Required

- [x] Update `/.claude/skills/docs-snippets/*.md` with these definitions
- [x] Add the decision flowchart
- [x] Include examples of each marker type
- [x] Emphasize: "If it compiles, it should be compiled"
- [x] Note that Moq/other library code should be in samples with appropriate package references

## Completed

Updated `~/.claude/skills/docs-snippets/` (global skill):

**SKILL.md:**
- Added "If it compiles, it should be compiled" principle
- Added compact decision flowchart
- Added "Release notes needed?" to checklist
- Trimmed duplicate content (moved to detail files)
- Updated detailed guides table with When/Load/Why columns

**09-marker-types.md (new):**
- Full definitions for all four marker types (snippet, invalid, generated, pseudo)
- Detailed decision flowchart
- Quick reference table
- Common mistakes section with examples

**05-ready-to-commit.md:**
- Added step 4: "Release Notes Needed?"
- Updated decision tree and renumbered sections

## Related

- `docs/todos/pseudo-code-audit.md` - The audit that revealed this gap
