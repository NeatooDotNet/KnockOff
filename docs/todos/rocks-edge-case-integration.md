# Rocks Edge Case Integration

**Status:** Complete
**Result:** 1,336 tests, 16 bugs fixed, 12 pointer tests skipped (unsafe not supported)

**Goal:** Port edge case tests from `Rocks.Analysis.IntegrationTests` into `KnockOff.Analysis.Tests`. Each file triaged, rewritten for KnockOff's 9 patterns, bugs fixed as found.

**Source:** `Rocks/src/Rocks.Analysis.IntegrationTests/` (63 files, ~425 tests)

---

## Bugs Fixed (16)

1. **Source() lambda cast** — CS1503 in conditional with lambda (PreCompiledInterceptorRenderer)
2. **Diamond ambiguity** — CS0229 on diamond-inherited interface members (FlatRenderer, InlineRenderer)
3. **Open generic inherited members** — Pattern 8 missing inherited interface members (KnockOffGenerator.Transform)
4. **Ref struct parameter support** — ReadOnlySpan<T> params couldn't be boxed (11 generator files)
5. **Constructor default values** — Not preserved in generated constructors (SymbolHelpers, both class builders)
6. **Class ref/out Invoke args** — Used InputArgumentList instead of ArgumentList (ClassRenderer, StandaloneClassRenderer)
7. **Class out param early return** — CS0177, out params not assigned on early return (both class renderers)
8. **Class virtual out param body** — CS0177, empty body for virtual override with out params (StandaloneClassRenderer)
9. **Class sequence exhaustion** — Fell back to base instead of repeating last value (MethodInterceptorRenderer)
10. **Constructor ref/out/params** — Modifiers stripped from generated constructor params (both class builders)
11. **Nullable unconstrained generics** — CS0453, TData? emitted verbatim in overrides (both class builders)
12. **Ref struct return types** — Not detected, caused boxing attempts (all 4 pipelines)
13. **Return(value) for ref struct** — Generated boxed Return/ThenReturn for ref structs (MethodInterceptorRenderer)
14. **Scoped modifier** — Not detected or emitted (all 4 pipelines)
15. **Ref struct property types** — Used generic PropertyGetInterceptor<T> (PropertyInterceptorRenderer)
16. **Ref struct property pipeline** — IsRefStructType not propagated through model pipeline (6 files)

---

## Group 1: Interface Tests (11 files) — COMPLETE

| Status | File | KnockOff Tests | Bugs |
|--------|------|---------------|------|
| [x] | InterfaceMethodReturnTests.cs | 20 | 0 |
| [x] | InterfaceMethodVoidTests.cs | 24 | 0 |
| [x] | InterfacePropertyTests.cs | 36 | 0 |
| [x] | InterfaceIndexerTests.cs | 40 | 0 |
| [x] | InterfaceGenericMethodTests.cs | 42 | 0 |
| [x] | InterfaceGenericPropertyTests.cs | 29 | 0 |
| [x] | InterfaceGenericEventsTests.cs | 14 | 0 |
| [x] | InterfaceGenericIndexerTests.cs | 26 | 0 |
| [x] | InterfaceMethodReturnWithEventsTests.cs | 10 | 0 |
| [x] | InterfaceMethodVoidWithEventsTests.cs | 10 | 0 |
| [x] | InterfaceStaticVirtualTests.cs | 6 | 0 |

## Group 2: Class Tests (11 files) — COMPLETE

| Status | File | KnockOff Tests | Bugs |
|--------|------|---------------|------|
| [x] | ClassMethodReturnTests.cs | 20 | 1 |
| [x] | ClassMethodVoidTests.cs | 24 | 0 |
| [x] | ClassPropertyTests.cs | 34 | 0 |
| [x] | ClassIndexerTests.cs | 40 | 0 |
| [x] | ClassGenericMethodTests.cs | 60 | 1 |
| [x] | ClassGenericPropertyTests.cs | 40 | 0 |
| [x] | ClassGenericEventsTests.cs | 16 | 0 |
| [x] | ClassGenericIndexerTests.cs | 45 | 0 |
| [x] | ClassMethodReturnWithEventsTests.cs | 10 | 0 |
| [x] | ClassMethodVoidWithEventsTests.cs | 10 | 0 |
| [x] | ClassConstructorTests.cs | 24 | 1 |

## Group 3: Abstract Class Tests (11 files) — COMPLETE

| Status | File | KnockOff Tests | Bugs |
|--------|------|---------------|------|
| [x] | AbstractClassMethodReturnTests.cs | 20 | 0 |
| [x] | AbstractClassMethodVoidTests.cs | 24 | 0 |
| [x] | AbstractClassPropertyTests.cs | 30 | 0 |
| [x] | AbstractClassIndexerTests.cs | 34 | 0 |
| [x] | AbstractClassGenericMethodTests.cs | 54 | 0 |
| [x] | AbstractClassGenericPropertyTests.cs | 38 | 0 |
| [x] | AbstractClassGenericEventsTests.cs | 12 | 0 |
| [x] | AbstractClassGenericIndexerTests.cs | 38 | 0 |
| [x] | AbstractClassMethodReturnWithEventsTests.cs | 10 | 0 |
| [x] | AbstractClassMethodVoidWithEventsTests.cs | 10 | 0 |
| [x] | AbstractClassConstructorTests.cs | 12 | 0 |

## Group 4: Feature-Specific Tests — COMPLETE

| Status | File | KnockOff Tests | Bugs |
|--------|------|---------------|------|
| [x] | GenericTests.cs | 22 | 1 |
| [x] | ConstraintTests.cs | 12 | 0 |
| [x] | ExplicitInterfaceImplementationTests.cs | 48 | 2 |
| [x] | EventTests.cs | 8 | 0 |
| [x] | OptionalArgumentsTests.cs | 24 | 1 |
| [x] | ParamsTests.cs | 45 | 1 |
| [x] | AsynchronousTests.cs | 53 | 0 |
| [x] | RefStructTests.cs | 20 | 4 |
| [x] | RecordTests.cs | 10 | 0 |
| [x] | RequiredInitPropertyTests.cs | 12 | 0 |
| [x] | VirtualsWithImplementationsTests.cs | 38 | 0 |
| [x] | OpenGenericsTests.cs | 24 | 0 |
| [x] | AllowNullTests.cs | 20 | 0 |
| [x] | AttributeTests.cs | 29 | 1 |
| [x] | NonPublicMemberTests.cs | 8 | 0 |
| [x] | HttpMessageHandlerTests.cs | 6 | 0 |
| [x] | DoesNotReturnTests.cs | 8 | 0 |
| [x] | PointerTests.cs | 12 (skipped) | 0 |
| [x] | MethodMemberTests.cs | 43 | 3 |

## Group 5: Excluded (Rocks-Specific)

These files test Rocks-specific API concepts that don't map to KnockOff:

| Status | File | Reason |
|--------|------|--------|
| N/A | AnalyzerTests.cs | Rocks analyzer infrastructure |
| N/A | ArgTests.cs | Rocks Arg matching API (no KnockOff equivalent) |
| N/A | ExpectationExceptionTests.cs | Rocks strict mode expectations |
| N/A | MockDefinitions.cs | Assembly attribute manifest |
| N/A | MultipleRockCallsTests.cs | Rocks RockContext lifecycle |
| N/A | RockContextTests.cs | Rocks context management |
| N/A | ShimTests.cs | Rocks shim concept |
| N/A | Shared.cs | NUnit config |
| N/A | VerificationTests.cs | Rocks verification lifecycle |
| N/A | PartialTests.cs | Rocks [RockPartial] attribute |

## Not Ported

| File | Reason |
|------|--------|
| VisibilityTests.cs | Requires separate referenced project for InternalsVisibleTo |
