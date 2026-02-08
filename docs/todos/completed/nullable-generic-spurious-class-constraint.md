# Bug: Spurious `where TData : class` on Nullable Unconstrained Generic Methods

**Status:** Complete
**Priority:** High
**Created:** 2026-02-08
**Last Updated:** 2026-02-08

---

## Problem

When an interface declares a generic method with nullable unconstrained type parameters like:

```csharp
TData? NullableValues<TData>(TData? data)
```

KnockOff's generator incorrectly adds `where TData : class` to the generated stub method. This causes compiler error **CS8665**: the constraint cannot be added because the original method has no such constraint.

In C# 9+, `T?` on an unconstrained type parameter means "default value" (null for reference types, default for value types) — it does NOT imply a `class` constraint. The generator's previous fix (for `IPropertyInfo.GetCustomAttribute<T>() where T : Attribute`) over-corrected: it now assumes any nullable return or parameter means `class`, but that's only valid when the original method already has a constraint that implies reference type.

**Blocks 2 Rocks-parity tests** from `InterfaceGenericMethodTests.cs`:
- `CreateWithNullableGenericParameterTypes`
- `MakeWithNullableGenericParameterTypes`

**Source:** Feedback from Rocks comparison at `https://github.com/keithdv/Rocks/blob/main/src/Rocks.Analysis.IntegrationTests/InterfaceGenericMethodTests.cs`

## Related Previous Fix

`docs/todos/completed/bug-generic-method-constraints-not-preserved.md` — fixed the opposite problem (missing `where T : class` when the original DID have a constraint implying reference type). The current bug is likely an over-correction from that fix.

## Solution

Investigate the constraint generation logic and fix it so that:

1. `where T : class` is emitted ONLY when the original type parameter has a constraint that implies reference type (e.g., `class`, a class-type constraint like `Attribute`, etc.)
2. `where T : class` is NOT emitted when the type parameter is unconstrained, even if `T?` appears in the return type or parameters

---

## Plans

- [Fix Nullable Generic Constraint](../plans/completed/fix-nullable-generic-constraint.md)

---

## Tasks

- [x] Reproduce the bug: create a test interface with `TData? NullableValues<TData>(TData? data)` and verify CS8665
- [x] Locate the constraint generation logic in the generator (likely `GetConstraintsForExplicitImpl` or equivalent in the current pipeline)
- [x] Identify the condition that incorrectly triggers `where T : class` for unconstrained nullable generics
- [x] Fix the condition to only emit `class` constraint when the original has a reference-type-implying constraint
- [x] Verify fix: the new test compiles and passes
- [x] Verify no regression: all existing generic constraint tests still pass
- [x] Check all pipelines (standalone, inline, open generic) for the same issue

---

## Progress Log

**2026-02-08:** Created todo from Rocks comparison feedback.

**2026-02-08:** Architect investigation complete. Root cause identified in `GetConstraintsForExplicitImpl()` in FlatModelBuilder and InlineModelBuilder. Plan created at `docs/plans/fix-nullable-generic-constraint.md`. Design.Stubs failing code in place as acceptance criteria.

**2026-02-08:** Developer review raised 2 concerns. (1) CRITICAL: Original `ImpliesReferenceType` computation was wrong for interface constraints -- `IDisposable.IsReferenceType == true` in Roslyn but `where T : IDisposable` does NOT imply reference type. Fix: use `tp.IsReferenceType` directly on the type parameter. (2) MINOR: Added `InterfaceConstrainedReturn<T>() where T : IDisposable` test case to Design.Stubs. Plan updated with corrected approach.

---

## Results / Conclusions

**Fixed.** Root cause was `GetConstraintsForExplicitImpl()` in `FlatModelBuilder` and `InlineModelBuilder` — it added `where T : class` for any method with `T?` in its signature, regardless of whether the type parameter was actually constrained to be a reference type.

**Fix:** Added `IsKnownReferenceType` field to `TypeParameterInfo` (computed from Roslyn's `ITypeParameterSymbol.IsReferenceType`) and guarded the constraint emission. Additionally, for unconstrained nullable type parameters, the `T?` annotation is stripped from explicit impl signatures and wrapped in `#nullable disable`/`#nullable restore` to avoid CS0453.

**Developer review caught a critical issue** in the architect's original design: the proposed `tp.ConstraintTypes.Any(ct => ct.IsReferenceType)` would have been wrong for interface constraints (e.g., `IDisposable`). The simpler `tp.IsReferenceType` directly on the type parameter was the correct approach.

**8,264 tests pass across all TFMs. Design.Stubs compiles cleanly with all 4 test methods.**
