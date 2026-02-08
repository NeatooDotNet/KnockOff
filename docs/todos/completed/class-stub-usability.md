# Class Stub Usability: Event AccessModifier + Silent Skip Diagnostic

**Status:** Complete
**Priority:** High
**Created:** 2026-02-07
**Last Updated:** 2026-02-07

---

## Problem

Two issues affecting class stub usability:

1. **Protected virtual events generate wrong access modifier (CS0507):** Class stubs (patterns 3, 4, 6, 8, 9) hardcode `public override event` in generated code. When a base class has `protected virtual event`, the generated `public override event` causes CS0507 ("cannot change access modifiers when overriding"). All four member types (methods, properties, indexers) already carry `AccessModifier` through the pipeline correctly — events are the gap.

2. **`[KnockOff]` on class with base type silently generates nothing:** When a user applies `[KnockOff]` to a class that inherits from a concrete class (not an interface), `TransformClass` returns `null` with no diagnostic. The user gets no feedback about why nothing was generated and no guidance to use `[KnockOffBase<T>]` or `[KnockOff<T>]` instead.

## Solution

**Fix 1 — Event AccessModifier:** Add `AccessModifier` field to `EventMemberInfo` and `InlineClassImplEventModel`. Extract from `DeclaredAccessibility` in `FromEvent()`. Pass through builders. Use in renderers instead of hardcoded `public`.

**Fix 2 — KO0201 Diagnostic:** In `TransformClass`, check for non-object base type before the interface check. If found, emit KO0201 diagnostic guiding user to `[KnockOffBase<T>]` or `[KnockOff<T>]`.

**Design.Domain setup:** Add protected members to `ServiceBase` (method, abstract method, property, abstract property, indexer, event) so Design.Stubs can verify the fix compiles.

### Prior art

This work was previously completed on the `origin/protectedMethods` branch but cannot be cherry-picked due to large unrelated changes on that branch. The design plan from that branch (`docs/plans/completed/protected-methods-design.md`) can be referenced but must be recreated against current main.

---

## Plans

- [Class Stub Usability Fixes](../plans/completed/class-stub-usability-fixes.md)

---

## Tasks

- [x] Add protected members to `ServiceBase` in Design.Domain
- [x] Fix event AccessModifier pipeline (EventMemberInfo, InlineClassImplEventModel, builders, renderers)
- [x] Add KO0201 diagnostic for `[KnockOff]` on class with base type
- [x] Verify Design.Stubs compiles with zero errors
- [x] Verify all tests pass

---

## Progress Log

### 2026-02-07
- Created todo based on analysis of `origin/protectedMethods` branch
- Prior branch had the work complete but mixed with ~290 files of unrelated changes
- Architect created plan at `docs/plans/class-stub-usability-fixes.md`
- Added protected members to `ServiceBase` in Design.Domain (event, property, methods, indexer)
- Added CA1070 suppression to Design.Domain.csproj for virtual event testing
- Created `src/Design/Design.Stubs/ProtectedMembers/ProtectedMemberStubs.cs` as acceptance criteria
- Verified 18 CS0507 errors across 6 stubs x 3 frameworks (confirming the bug exists)
- Plan handed off to developer review

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] Design project builds successfully
- [x] Design project tests pass

**Verification results:**
- Design build: Pass (0 errors, 0 warnings across net8.0/net9.0/net10.0)
- Design tests: Pass (777 tests, 0 failures)

---

## Results / Conclusions

Both fixes implemented and verified. The event AccessModifier pipeline now matches the existing method/property/indexer pattern. KO0201 diagnostic guides users who misapply `[KnockOff]` to classes with concrete base types. Three new diagnostic tests added via `CSharpGeneratorDriver`.
