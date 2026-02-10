# Partial Class Diagnostic for All KnockOff Patterns

**Status:** In Progress
**Priority:** Medium
**Created:** 2026-02-09
**Last Updated:** 2026-02-09

---

## Problem

When a user applies `[KnockOff]` or `[KnockOff<T>]` to a non-partial class, the generator silently skips it — no diagnostic, no output. The user gets no feedback about why their stub isn't working.

The `[KnockOffBase<T>]` pattern already has KO2008 (Error: "Class must be partial"), but the other patterns rely on predicate filtering (`IsCandidateClass`, `IsInlineStubCandidate`) that quietly rejects non-partial classes.

### Current Behavior

| Pattern | Non-partial behavior |
|---|---|
| `[KnockOff]` standalone (1, 2) | Silent skip — predicate rejects |
| `[KnockOff<T>]` inline (5, 6) | Silent skip — predicate rejects |
| `[KnockOff(typeof(...))]` open generic (7, 8, 9) | Silent skip — predicate rejects |
| `[KnockOffBase<T>]` standalone class (3, 4) | KO2008 Error — already implemented |

### Desired Behavior

All patterns should emit an Error diagnostic when `partial` is missing, consistent with KO2008. Additionally, a code fix should offer to add the `partial` modifier.

## Solution

1. **Extend diagnostic coverage**: Emit a diagnostic (Error) for all `[KnockOff]`-family attributes on non-partial classes. Consider whether KO2008 should be reused/generalized or new IDs created.

2. **Add a code fix provider**: Implement a `CodeFixProvider` that offers to add the `partial` keyword. This would be KnockOff's first code fix.

### Design Considerations

- **Diagnostic approach**: The current predicates filter out non-partial classes before the transform stage. The diagnostic needs to be emitted from a separate pipeline that detects the attribute on non-partial classes. Options:
  - Add a second `ForAttributeWithMetadataName` pipeline per attribute that catches non-partial and emits the diagnostic
  - Use a Roslyn Analyzer (separate from the generator) — analyzers are better suited for diagnostics+fixes
  - Modify predicates to accept non-partial, then emit diagnostic in transform and skip generation

- **Code fix packaging**: Code fixes ship as analyzers, not generators. Need to understand how to package a `CodeFixProvider` alongside the source generator. The fix itself is straightforward — insert `partial` keyword before `class`.

- **Diagnostic ID**: KO2008 message says "to use [KnockOffBase<T>]". Either generalize the message or create a new ID. Generalizing is simpler since the fix is the same.

- **Scope**: All 9 patterns need coverage. The 4 KnockOffBase patterns (3, 4) already have KO2008.

---

## Plans

[None yet]

---

## Tasks

- [ ] Decide: Analyzer vs generator pipeline for the diagnostic
- [ ] Decide: Reuse KO2008 (generalized message) vs new diagnostic ID
- [ ] Implement diagnostic for `[KnockOff]` standalone on non-partial class
- [ ] Implement diagnostic for `[KnockOff<T>]` inline on non-partial class
- [ ] Implement diagnostic for `[KnockOff(typeof(...))]` open generic on non-partial class
- [ ] Implement `CodeFixProvider` to add `partial` modifier
- [ ] Add diagnostic tests using existing `DiagnosticTests` infrastructure
- [ ] Verify KO2008 still works correctly for `[KnockOffBase<T>]` patterns

---

## Progress Log

### 2026-02-09
- Created todo
- Confirmed KO2008 exists for `[KnockOffBase<T>]` (Error severity) in `KnockOffGenerator.cs:195`
- Confirmed predicates silently reject non-partial for all other patterns
- Confirmed diagnostic test infrastructure exists in `DiagnosticTests.cs`
- No code fix providers exist yet in the project

---

## Completion Verification

Before marking this todo as Complete, verify:

- [ ] Design project builds successfully
- [ ] Design project tests pass

**Verification results:**
- Design build: [Pending]
- Design tests: [Pending]

---

## Results / Conclusions

[What was learned? What decisions were made?]
