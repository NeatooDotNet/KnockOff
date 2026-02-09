# Generic Type Gaps

**Status:** In Progress
**Priority:** High
**Created:** 2026-02-08
**Last Updated:** 2026-02-09
**Plan:** [generic-type-gaps.md](../plans/generic-type-gaps.md)

---

## Problem

Feedback indicates KnockOff may not support more than one generic type parameter. Need to validate the existing API across all applicable patterns for comprehensive generic type support.

## Solution

Validate (and fix if needed) all generic type combinations across all applicable patterns by:
1. Adding test interfaces/classes with multi-type generics to Design.Domain
2. Creating Design.Stubs entries to verify compilation
3. Writing KnockOffTests for runtime behavior

## Scope

### Generic Features to Validate

| # | Feature | Example |
|---|---------|---------|
| A | Multi-type-param interface | `IService<T, A>` |
| B | Multi-type-param class | `ServiceBase<T, A>` |
| C | Methods using class type params | `T GetItem(A key)` on `IService<T, A>` |
| D | Methods with own type params | `TResult Map<TResult>(T input)` |
| E | Methods with multiple own type params | `TOut Convert<TIn, TOut>(TIn input)` |
| F | Where clauses on class/interface type params | `where T : new(), IFoo where A : IRepo` |
| G | Where clauses on method type params | `TResult Map<TResult>() where TResult : class, new()` |
| H | Multiple where clauses on methods in ALL patterns | Every pattern must handle `where T : ... where A : ...` |
| I | Generic delegates with multiple type params | `delegate TResult MyFunc<T, TResult>(T input)` |

### Pattern Applicability Matrix

| Feature | P1 Standalone | P2 Generic Standalone | P3 Standalone Class | P4 Generic Standalone Class | P5 Inline Interface | P6 Inline Class | P7 Inline Delegate | P8 Open Generic Interface | P9 Open Generic Class |
|---|---|---|---|---|---|---|---|---|---|
| A: Multi-type interface | Closed | Open | - | - | Closed | - | - | Open | - |
| B: Multi-type class | - | - | Closed | Open | - | Closed | - | - | Open |
| C: Methods using class types | Yes | Yes | Yes | Yes | Yes | Yes | - | Yes | Yes |
| D: Method own type param | Yes | Yes | Yes | Yes | Yes | Yes | - | Yes | Yes |
| E: Method multiple own types | Yes | Yes | Yes | Yes | Yes | Yes | - | Yes | Yes |
| F: Where on class/interface types | Must handle | **Must propagate** | Must handle | **Must propagate** | Must handle | Must handle | - | **Must propagate** | **Must propagate** |
| G: Where on method types | Yes | Yes | Yes | Yes | Yes | Yes | - | Yes | Yes |
| H: Multiple where on methods | Yes | Yes | Yes | Yes | Yes | Yes | - | Yes | Yes |
| I: Generic delegates | - | - | - | - | - | - | Yes | - | - |

**"Must handle"** = interface/class has where clauses; generator must not break (constraints already satisfied by concrete types).
**"Must propagate"** = generator must emit where clauses on the generated stub class/method.

### Where Clause Combinations to Test

Where clauses must be validated in ALL patterns, not just open generics:

1. **Single constraint:** `where T : class`
2. **Multiple constraints:** `where T : class, IComparable<T>, new()`
3. **Multiple type params with constraints:** `where T : class, new() where A : IRepository<T>`
4. **Method-level where clauses:** `TResult Map<TResult>(T input) where TResult : class, new()`
5. **Method-level multiple wheres:** `TOut Convert<TIn, TOut>(TIn a) where TIn : struct where TOut : class`
6. **Cross-referencing constraints:** `where A : IHandler<T>` (one type param referencing another)
7. **Struct constraint:** `where T : struct`
8. **Unmanaged constraint:** `where T : unmanaged`
9. **notnull constraint:** `where T : notnull`

### Critical Combinations (most likely to have bugs)

1. Open generic + multiple type params + where clauses (patterns 2, 4, 8, 9)
2. Generic method with own type params inside a generic class (type param name collision risk)
3. Methods using a mix of class type params AND method type params
4. Where clauses with multiple constraints (`where T : class, IFoo, new()`)
5. Cross-referencing constraints between type params
6. Generic delegates with constraints

## Plans

- [Generic Type Gaps - Validation and Fix Plan](../plans/generic-type-gaps.md)

## Rocks Library Findings (Gaps 25-31)

Gaps discovered while using KnockOff to stub interfaces from the Rocks test library. Reproduction tests in `src/Tests/KnockOffTests/RocksGapReproductionTests.cs`.

### Reproduced Bugs

| Gap | Issue | Root Cause | Patterns Affected | Error |
|-----|-------|-----------|-------------------|-------|
| 26 | `in` params stripped from indexer params | Generator doesn't emit `in` modifier on indexer parameter in explicit interface impl | All patterns (indexers only) | CS0535, CS0539 |
| 27 | Generic methods with `out` params | Delegate params missing RefKind in inline and class model builders | Inline + Class (P3, P4, P5, P6, P8, P9) for generic methods only | CS1615 |
| 28 | Generic methods with `ref` params | Same root cause as Gap 27 | Inline + Class (P3, P4, P5, P6, P8, P9) for generic methods only | CS1615 |
| 31 | Generic methods with 2+ type params | Interceptor only supports `Of<T>()` with 1 type arg; no `Of<T1,T2>()` | ALL patterns with generic method handlers (P1-P6, P8, P9) | CS0246 (inline), CS0305 (standalone) |

### Not Reproduced (Already Working / Fixed)

| Gap | Issue | Status |
|-----|-------|--------|
| 30 | Multiple closed generic stubs of same open generic | Works — stubs named with type args appended (e.g., `IServiceInt32String`). User likely couldn't find them due to naming convention. Documentation gap. |

### Notes

- Gaps 27/28 are NOT about out/ref params in general (those work fine in standalone). They're specifically about **generic methods** with out/ref params in the **inline and class patterns** (InlineModelBuilder, ClassModelBuilder, StandaloneClassModelBuilder). The flat pipeline (FlatModelBuilder) is NOT affected because it correctly uses `FormatParameterWithRefKind(p)`.
- Gap 31 is a fundamental limitation: the generic method interceptor only supports `Of<T>()` with a single type parameter. Methods like `TReturn Run<TInput, TReturn>(TInput input)` need `Of<TInput, TReturn>()` which doesn't exist.

## Tasks

- [x] Create implementation plan
- [x] Add test interfaces/classes to Design.Domain
- [x] Create Design.Stubs entries
- [x] Fix SmartDefault<T> type parameter collision (Bug 1 from architect)
- [x] Fix Gap 26: `in` modifier stripped from indexer parameters
- [x] Fix Gap 27/28: Generic methods with out/ref params in inline pattern
- [x] Fix Gap 31: Generic methods with 2+ type parameters (needs Of<T1,T2>() support)
- [ ] Validate Feature A: Multi-type-param interfaces across applicable patterns
- [ ] Validate Feature B: Multi-type-param classes across applicable patterns
- [ ] Validate Feature C: Methods using class type params
- [ ] Validate Feature D: Methods with own type params (single)
- [ ] Validate Feature E: Methods with multiple own type params
- [ ] Validate Feature F: Where clauses on class/interface type params
- [ ] Validate Feature G: Where clauses on method type params
- [ ] Validate Feature H: Multiple where clauses on methods in ALL patterns
- [ ] Validate Feature I: Generic delegates with multiple type params
- [ ] Validate where clause combinations (struct, unmanaged, notnull, cross-referencing)
- [ ] Update documentation

## Progress Log

- 2026-02-08: Created todo with full pattern/feature matrix.
- 2026-02-08: Architect completed codebase analysis and Design.Stubs verification. Discovered Bug 1: SmartDefault<T> type parameter name collision (CS0693) in FlatRenderer and InlineRenderer. Affects P2 and P8 when generic stubs have interfaces with generic methods. P4 and P9 verified working with new CacheBase<TKey, TValue> type. Plan updated to "Under Review (Developer)".
- 2026-02-08: Rocks library gap analysis. Reproduced 4 of 7 reported gaps (26, 27, 28, 31). Gaps 25 and 30 already work. Gap 29 needs further investigation. Added reproduction tests in RocksGapReproductionTests.cs with commented-out stubs for failing cases.
- 2026-02-08: Developer review identified 5 concerns about plan scope: class patterns (P3, P4, P6, P9) also affected by Bugs 3 and 4; additional Bug 2 locations in class model builders and ModelAdapters.cs; Bug 4 inline approach needed commitment. Architect verified all 5 concerns and updated plan.
- 2026-02-09: Implementation complete. All 4 bugs fixed in 4 phases. Architect verification passed with zero test failures.

## Results / Conclusions

All 4 generator bugs fixed and verified:

1. **Bug 1 (SmartDefault collision):** Renamed `SmartDefault<T>` to `SmartDefault<TSmartDefault>` in FlatRenderer and InlineRenderer. Unblocks P2/P8 generic stubs with method-level generics.

2. **Bug 2 (Gap 26 — `in` indexer params):** Preserved `in` modifier across all 4 pipelines (14+ locations). Also fixed pre-existing bug where `in` was incorrectly passed at delegate call sites.

3. **Bug 3 (Gaps 27/28 — generic method out/ref delegates):** Added RefKind to delegate parameter formatting in InlineModelBuilder, ClassModelBuilder, and StandaloneClassModelBuilder.

4. **Bug 4 (Gap 31 — multi-arity generic methods):** Introduced `InlineGenericTypeArityGroup` and `FlatGenericMethodArityGroup` model records to support `Of<T>()` and `Of<T1,T2>()` on the same interceptor. All 4 pipelines updated.

**Architect verification passed:** All 5 test projects pass across net8.0/net9.0/net10.0 with zero failures (1351+ KnockOffTests, 599 Documentation.Samples, 473 NeatooInterfaceTests, 356 Design.Tests, 14 AssemblyStrict).

**Remaining:** Systematic validation of Features A-I and where clause combinations across all applicable patterns. Documentation update for closed generic naming convention (Gap 30).
