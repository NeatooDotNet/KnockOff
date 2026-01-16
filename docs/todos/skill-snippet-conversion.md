# Convert Skill Code Examples to Compiled Snippets

**Status:** Pending
**Priority:** Medium
**Created:** 2026-01-15

---

## Problem

The rebuilt knockoff skill (`.claude/skills/knockoff/`) uses inline code examples that can drift out of sync with the actual KnockOff API. The docs use MarkdownSnippets to keep code examples compiled and tested - the skill should follow the same pattern.

## Solution

Convert inline code examples in skill files to `snippet:` references backed by compiled samples in `src/Tests/KnockOff.Documentation.Samples/Skills/`.

---

## Scope

| File | Approx Code Blocks | Notes |
|------|-------------------|-------|
| SKILL.md | ~15 | Core examples - highest priority |
| api-reference.md | ~20 | API signatures and examples |
| moq-migration.md | ~25 | Migration patterns |
| patterns.md | ~30 | Common usage patterns |
| stub-types.md | ~15 | Stub type examples |
| **Total** | **~105** | |

## Approach

### Phase 1: Critical Examples (SKILL.md)

Convert the most important examples that users see first:
- [ ] Quick Start (create stub, configure, verify)
- [ ] Core API (OnCall, Value, OnGet patterns)
- [ ] Common Mistakes (the 4 anti-patterns)
- [ ] Moq Migration Quick Reference

### Phase 2: API Reference

- [ ] Method interceptor examples
- [ ] Property interceptor examples
- [ ] Indexer interceptor examples
- [ ] Event interceptor examples
- [ ] Generic method examples
- [ ] Async method examples

### Phase 3: Migration Guide

- [ ] Step-by-step migration examples
- [ ] Common patterns (conditional, sequential, exceptions)
- [ ] KnockOff-only features

### Phase 4: Patterns & Stub Types

- [ ] patterns.md examples
- [ ] stub-types.md examples

### Phase 5: Pseudo Markers for Fragments

Some code blocks are intentionally fragmentary (API signatures, single-line illustrations). These should use `<!-- pseudo:{id} -->` markers:
- [ ] Audit remaining code blocks
- [ ] Add pseudo markers where appropriate

---

## Implementation Notes

### Sample File Structure

Create new sample files or extend existing ones:

```
src/Tests/KnockOff.Documentation.Samples/Skills/
├── SkillSamples.cs          # Existing - may need updates
├── SkillApiSamples.cs       # New - for api-reference.md
├── SkillMigrationSamples.cs # New - for moq-migration.md
├── SkillPatternSamples.cs   # New - for patterns.md
└── SkillStubTypeSamples.cs  # New - for stub-types.md
```

### Naming Convention

Use prefix `skill-{file}-{concept}`:
- `skill-main-quick-start-stub`
- `skill-api-method-oncall`
- `skill-migration-setup-returns`
- `skill-patterns-sequential`
- `skill-types-inline-usage`

### Verification

After conversion:
```powershell
dotnet build
dotnet mdsnippets
git diff --exit-code .claude/skills/knockoff/
```

---

## Tasks

- [ ] Create sample files with domain types
- [ ] Add `#region` markers for SKILL.md examples
- [ ] Update SKILL.md to use `snippet:` references
- [ ] Add `#region` markers for api-reference.md
- [ ] Update api-reference.md to use `snippet:` references
- [ ] Add `#region` markers for moq-migration.md
- [ ] Update moq-migration.md to use `snippet:` references
- [ ] Add `#region` markers for patterns.md
- [ ] Update patterns.md to use `snippet:` references
- [ ] Add `#region` markers for stub-types.md
- [ ] Update stub-types.md to use `snippet:` references
- [ ] Add `<!-- pseudo:{id} -->` markers to fragment code blocks
- [ ] Run verification script
- [ ] Run mdsnippets and verify no drift

---

## Progress Log

### 2026-01-15
- Created todo for tracking this work
- Skill files currently use inline code examples
- Existing skill samples in `Skills/SkillSamples.cs` may be reusable

---

## Results / Conclusions

*To be filled when complete*
