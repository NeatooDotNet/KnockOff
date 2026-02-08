# Class Stub Generic Method Support

**Status:** Complete
**Priority:** Medium
**Created:** 2026-02-07
**Last Updated:** 2026-02-07 (updated)
**Plan:** [Class Stub Generic Method Support](../plans/class-stub-generic-method-support.md)

---

## Problem

Class stubs currently skip generic virtual methods entirely. When a base class has a generic method like `TypeDescriptionProvider.RegisterType<T>()`, it is silently excluded from the generated stub. The base class implementation is inherited as-is, but users cannot intercept, configure, or verify calls to these methods.

This was an intentional decision in the [TypeDescriptionProvider bug fix](../plans/completed/class-stub-typedescriptionprovider-fixes.md) -- the pipeline had no support for method-level type parameters, so generating them produced uncompilable code (CS0115). The fix was to skip them rather than generate broken overrides.

---

## Solution

Add full support for generic virtual methods on class stubs. This requires changes across the entire class stub pipeline:

- **Model:** `InlineClassImplMethodModel` needs generic type parameter fields
- **Builders:** `ClassModelBuilder` and `StandaloneClassModelBuilder` need to pass generic type parameters through
- **Renderers:** `ClassRenderer` and `StandaloneClassRenderer` need to emit `override void Method<T>(...)` with type parameters and constraints
- **Interceptor system:** Needs to handle generic callbacks (`OnCall<T>`, `Return<T>`, etc.) or a workable alternative

---

## Plans

- [Class Stub Generic Method Support](../plans/class-stub-generic-method-support.md)

---

## Tasks

- [x] Investigate how generic methods interact with the interceptor system
- [x] Design the API surface for configuring generic method stubs
- [x] Add generic type parameter fields to `InlineClassImplMethodModel`
- [x] Update builders to pass generic type parameters
- [x] Update renderers to emit generic method overrides with constraints
- [x] Add Design.Stubs examples for generic method class stubs
- [x] Add tests (26 new Design.Tests for all 4 patterns)

---

## Progress Log

- 2026-02-07: Created. Originated from TypeDescriptionProvider bug fix where generic methods were intentionally skipped (see `docs/plans/completed/class-stub-typedescriptionprovider-fixes.md`).
- 2026-02-07: Architecture plan created. Reuses existing Of\<T\>() handler pattern from interface stubs. Affects patterns 3, 4, 6, 9. All pipeline layers touched: transform, model, builder, renderer.
- 2026-02-07: Design.Stubs acceptance criteria created and verified. `GenericMethodBase` domain class and stubs for patterns 3 and 6 produce expected CS0534 errors (6 errors across 3 TFMs). Plan ready for developer review.
- 2026-02-07: Developer raised 5 concerns (2 blocking, 3 non-blocking). Architect addressed all 5: (1) concrete solution for IGenericMethodCallTracker/IResettable helper interfaces in both inline and standalone paths, (2) patterns 4 and 9 now have Design.Stubs acceptance criteria with GenericMethodRepositoryBase\<TEntity\> (12 errors across 3 TFMs), (3) clarified CallArgumentList vs ArgumentList for callback invocation, (4) added async return type handling (Task\<T\>/ValueTask\<T\>) for abstract generic method fallback, (5) added mixed overload edge case (Process + Process\<T\>) to GenericMethodBase. Plan returned for developer re-review.
- 2026-02-07: Implementation complete. All 4 phases done. Design.Stubs builds with 0 errors. 26 new tests pass. Full test suite passes (0 failures across all projects and TFMs). Plan status set to Awaiting Verification.
- 2026-02-07: Architect verification passed. All builds and tests independently confirmed passing. Implementation matches design. Marked Complete.

---

## Results / Conclusions

Full generic method support added to all 4 class stub patterns (3, 4, 6, 9). Reuses the existing Of\<T\>() handler pattern from interface stubs. 26 new tests covering Return, Call, Verify, multiple type params with constraints, mixed overloads, virtual base fallback, abstract default, Reset, and CalledTypeArguments. All Design.Stubs acceptance criteria compile. Zero test regressions.
