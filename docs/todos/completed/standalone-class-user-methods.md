# Standalone Class Pipeline: User Method Support for Methods

**Status:** Complete
**Priority:** High
**Created:** 2026-02-05
**Last Updated:** 2026-02-05

---

## Problem

The standalone class pipeline (`[KnockOffBase<T>]` — patterns 3 and 4) does not support user method overrides for methods. The `_` suffix user method pattern works on standalone interface stubs (patterns 1 and 2) but was never wired into the standalone class pipeline.

This was discovered during the `.When()` + user methods feature work. The architect's scope table claimed "Standalone Class: User Methods: Yes" without verifying, and nobody caught it until implementation.

### What works (patterns 1 and 2):

```csharp
[KnockOff]
public partial class MyRepoStub : IMyRepo
{
    protected override User? GetUser_(int id) => Users.Single(u => u.Id == id);
}
// GetUser_ is detected, generates user method interceptor with fallback
```

### What doesn't work (patterns 3 and 4):

```csharp
[KnockOffBase<MyRepoBase>]
public partial class MyRepoStub
{
    // No way to define user method overrides for methods
    // The standalone class pipeline (StandaloneClassModelBuilder / StandaloneClassRenderer)
    // has never been wired for user method detection
}
```

### Root Cause

The standalone class pipeline uses completely separate code paths from the interface pipeline:

| Component | Interface Pipeline (1,2) | Class Pipeline (3,4) |
|---|---|---|
| Transform | `TransformClass` | `TransformStandaloneClass` |
| Builder | `FlatModelBuilder` | `StandaloneClassModelBuilder` |
| Renderer | `FlatRenderer` | `StandaloneClassRenderer` |

User method detection and the `_` suffix pattern were implemented only in `FlatModelBuilder` / `FlatRenderer`. The `StandaloneClassModelBuilder` and `StandaloneClassRenderer` were never updated to support this.

---

## Plans

- [Standalone Class User Methods - Implementation Plan](../plans/standalone-class-stub-overrides.md)

---

## Tasks

- [x] Investigate what `StandaloneClassModelBuilder` needs for user method detection
- [x] Investigate what `StandaloneClassRenderer` needs for user method interceptor rendering
- [x] Design the integration (may be able to reuse `UnifiedInterceptorBuilder` patterns)
- [x] Add Design.Stubs compilation verification for patterns 3 and 4 with user methods
- [x] Implement user method support in standalone class pipeline
- [x] Add tests for `[KnockOffBase<T>]` with user method overrides on methods
- [x] Verify `.When()` API also works on standalone class user method interceptors

---

## Progress Log

**2026-02-05**: Created todo. Gap discovered during `.When()` + user methods implementation on branch `when-with-user-methods`. The architect scope table falsely claimed support; the standalone class pipeline has never been wired for user method detection.

**2026-02-05**: Architectural plan created at `docs/plans/standalone-class-stub-overrides.md`. Deep codebase analysis completed. All four gaps identified (Transform, Builder, Model, Renderer). Design.Stubs acceptance criteria verified (CS0115 errors confirmed). Most infrastructure already exists -- work is primarily wiring existing components through the standalone class pipeline.

**2026-02-05**: Clarification review completed. Seven design decisions confirmed and recorded in plan's Design Decisions section. Key decisions: (1) user method completely replaces base.Method() call, (2) interceptor-internal fallback pattern, (3) generate virtual _ methods for ALL target class methods, (4) pattern 4 needs explicit verification, (5) .When() is in scope, (6) HasUserOverride on shared InlineClassImplMethodModel, (7) CS0115 sufficient -- no custom diagnostic. Added pattern 4 (generic standalone class) acceptance criteria to Design.Stubs using `[KnockOffBase(typeof(RepositoryBase<>))]` with three user method overrides exercising generic type parameters and partial overload coverage. All 5 CS0115 errors confirmed (2 pattern 3, 3 pattern 4). Plan ready for developer review.

**2026-02-05**: Developer raised two concerns: (1) signature key matching for ClassMemberInfo underspecified -- extract vs duplicate, (2) partial overload coverage interceptor splitting not described. Architect investigation completed. Concern 1 resolved: extract `BuildOverrideSignatureKey` and `NormalizeTypeForOverrideMatching` into shared `SymbolHelpers` method -- both pipelines call the same code, preventing drift. Concern 2 resolved: use single interceptor with per-signature `UserMethodName` tracking (not split interceptors like FlatModelBuilder) -- extend `MethodSignatureInfo` with `UserMethodName`, set `InterceptorRenderOptions.UserMethodFallback: true` when any overload has user override, `RenderImplMethodOverride` branches per-overload based on `HasUserOverride`. Both concerns addressed in plan with full code examples and generated output specifications.

**2026-02-05**: All phases complete. Phases 1-6 (Transform, Model, Builder, Renderer Base, Renderer Impl, Tests) implemented by prior agent. Phase 7 (Documentation) completed: updated API consistency matrix to reflect standalone class user method support for patterns 3 and 4, corrected user-methods guide availability note to include all four standalone patterns, expanded Design.Stubs acceptance criteria file with documentation-quality comments. `dotnet build src/Design/Design.Stubs` succeeded (0 warnings, 0 errors). 39 new tests passing across net8.0/net9.0/net10.0.
