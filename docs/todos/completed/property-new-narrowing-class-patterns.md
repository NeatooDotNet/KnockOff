# Property `new` narrowing for class-based stub patterns (3, 4, 6, 9)

**Type:** Bug
**Status:** Complete (resolved as: not a bug — working as designed)
**Priority:** Medium
**Created:** 2026-04-20
**Closed:** 2026-04-20

---

## Resolution

**Not a bug.** Investigation concluded that class `new virtual` is materially different from interface `new`:

- **Interfaces** share one dispatch slot across a hierarchy. The generator had to pick one declaration's accessors and got it wrong — the real compile bug, fixed in v0.56.0 for patterns 1, 2, 5, 7, 8.
- **Classes** with `new virtual` create a **genuinely separate v-table slot**. The stub's target type (e.g., `NarrowClassDerived`) owns the derived slot; the base type (e.g., `WideClassBase`) owns its own slot. The stub intercepts the derived slot, which matches the target type. Access through a base-typed reference targets the base slot — that is exactly what C# `new` means.

If a caller wants to intercept the base slot, they should target the base class with its own stub.

## Evidence

- Repro stubs: `src/Design/Design.Stubs/Properties/ShadowedClassPropertyRepro.cs`
- Pinning tests: `src/Design/Design.Tests/PropertyTests/ShadowedClassPropertyTests.cs` (6 passing)
- Matrix entry updated in `docs/guides/api-consistency-matrix.md` with the class-patterns note.

## Context

Follow-up from [property-new-narrowing-bug](../../plans/completed/property-new-narrowing-bug.md).
