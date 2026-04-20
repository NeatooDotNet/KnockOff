# Property `new` narrowing for class-based stub patterns (3, 4, 6, 9)

**Type:** Bug
**Status:** Not Started
**Priority:** Medium
**Created:** 2026-04-20

---

## Problem

`property-new-narrowing-bug` fixed shadowed-property stubs for interface-based patterns (1, 2, 5, 7, 8) — `InlineModelBuilder` and `FlatRenderer`. The class-based pipelines were explicitly deferred:

- Pattern 3 — `[KnockOffBase<ConcreteClass>]` on partial class
- Pattern 4 — `[KnockOffBase(typeof(ClassBase<>))]` on generic partial class
- Pattern 6 — `[KnockOff<ConcreteClass>]` on test-nested stub class
- Pattern 9 — `[KnockOff(typeof(ServiceBase<>))]` on generic nested stub

Class hierarchies reach `new` through `StandaloneClassModelBuilder` / `ClassRenderer` with `override`-chain semantics that differ from interface shadowing. The fix needs a repro (no class-level shadowed-property repro exists today) and a pipeline-specific design.

## Context

- Parent bug: [property-new-narrowing-bug](../plans/completed/property-new-narrowing-bug.md)
- `docs/guides/api-consistency-matrix.md` — Feature 3 (Property Interception) silently non-compliant for patterns 3, 4, 6, 9 until this lands.

## Task List

- [ ] Write a class-hierarchy shadowed-property repro in Design.Stubs
- [ ] Confirm which pipelines are affected (`StandaloneClassModelBuilder`, `ClassRenderer`, possibly `InlineRenderer` pattern 6)
- [ ] Design union-accessor fix per pipeline
- [ ] Extend Design.Tests with routing coverage for all four patterns
- [ ] Update `api-consistency-matrix.md` to reflect restored consistency
