# TTuple Interceptors - Collapse Arities + Restore Named Parameters

**Status:** Complete
**Priority:** High
**Created:** 2026-02-15
**Last Updated:** 2026-02-15
**Completed:** 2026-02-15
**Plan:** [TTuple Interceptors](../plans/ttuple-interceptors.md)

---

## Problem

v0.50.0 replaced generated interceptor classes with pre-compiled arity-based generic types (`MethodInterceptor0<TReturn>`, `MethodInterceptor1<T1, TReturn>`, ..., `MethodInterceptor8<T1,...,T8, TReturn>`), achieving a **53% build time reduction** (28.5s -> 13.4s). However:

1. **Lost named parameters** - IntelliSense shows `arg1, arg2` instead of original parameter names in all callbacks (Return, Call, ThenReturn, ThenCall) and When/ThenWhen.
2. **36 library types** - 9 arities x 4 families of near-identical code to maintain, plus ~180 inner classes.

## Solution

Replace the arity-based type system with a **TTuple approach**: `MethodInterceptor<TDelegate, TArgs, TReturn>` where:

- **TDelegate** (a generated delegate per method) gives named params on all callbacks (Return, Call, ThenReturn, ThenCall)
- **TArgs** (a ValueTuple for 2+ params, raw type for 1 param) gives named params on When/ThenWhen via tuple element names propagating through generics
- **Zero-param methods** continue using `MethodInterceptor0<TReturn>` (no TDelegate or TArgs needed)

This collapses arities 1-8 into a single type per family: **8 library types instead of 36** (plus inner classes), while providing named parameters on both callbacks AND When — strictly better than the previous TDelegate-only plan.

### Key Design Points

- **TArgs for 1-param**: Use the raw type (e.g., `int`), not a 1-tuple. `When(int args)` works cleanly.
- **TArgs for 2+ params**: Use ValueTuple (e.g., `(int a, int b)`). `When((1, 2))` with named IntelliSense.
- **LastArgs**: Keep it. `TArgs LastArgs` gives `(int a, int b)` with named `.a`, `.b` access for 2+ params, or raw value for 1-param.
- **When syntax change**: `When(1, 2)` becomes `When((1, 2))` for 2+ params (extra parens for tuple literal).
- **Direct TDelegate invocation**: The library invokes TDelegate directly via expression trees (compiled once per type combo) or `DynamicInvoke` — never converts to `Func<>`. This eliminates converter lambdas from generated code AND unlocks ref/out parameter support (since `Func<>`/`Action<>` cannot express ref/out, but custom delegates can).
- **Generated code is minimal**: Just a delegate type + field declaration. No converter lambda. The library handles invocation generically at runtime.
- **Library types don't affect consumer build time** — only generated delegates do.

### Generated Type Count

Same as TDelegate-only plan: ~1 delegate per 1+ param method. Estimated ~1,400 delegates for full test suite. Build time target: ~15-16s (~44% improvement over v0.49.0's 28.5s).

### ref/out Parameter Unlock

By never converting TDelegate to `Func<>`, ref/out parameters become naturally supported through the delegate's own signature. `Func<string, out int, bool>` is invalid C#, but `delegate bool TryParseDelegate(string input, out int result)` works. The library invokes TDelegate directly, so ref/out flows through naturally. TArgs would only cover matchable/input parameters for When.

---

## Plans

- [TTuple Interceptors Plan](../plans/ttuple-interceptors.md)
- [TSyncDelegate Type Parameter](../plans/tsync-delegate.md)

---

## Tasks

- [x] Design TTuple interceptor library types (architect)
- [x] Developer review plan
- [x] Implement TTuple interceptors
- [x] Update generator/renderer to emit delegates and TTuple field types
- [x] Verify all tests pass
- [x] Benchmark build time vs v0.49.0 and v0.50.0
- [x] Verify IntelliSense in Design projects
- [x] TSyncDelegate for async simplified callbacks (architect-developer review loop + implementation)

---

## Progress Log

### 2026-02-15
- Brainstormed TTuple approach as improvement over TDelegate-only plan
- Confirmed tuple element names propagate through C# generic type parameters
- Confirmed 1-param case works with raw type (no 1-tuple needed)
- Confirmed LastArgs works naturally with TArgs
- Deleted previous TDelegate-only todo and plan
- Created this todo
- Key insight: invoke TDelegate directly (never convert to Func<>) — eliminates converter from generated code AND unlocks ref/out parameter support. Inspired by RemoteFactory's `ForDelegate(Type, object[])` pattern.
- Updated todo with direct invocation design and ref/out unlock
- DynamicInvoke prototype (37 tests, all pass): confirmed DynamicInvoke handles ref/out, async, generics. Expression trees CANNOT handle ref/out. Added findings to plan.
- Developer review raised 7 concerns (2 fundamental): fallback/source delegation mismatch between base classes and arity types; async base class design incomplete.
- Key realization: resolving developer concerns by falling back to base class approach (thin generated subclasses) loops back to v0.48 with ~same build time regression. The expression tree approach is what enables "named params WITHOUT build time regression."

### Approach Comparison

| Approach | Named Params | Build Time | Complexity |
|---|---|---|---|
| v0.48 base classes (Func<> as TDelegate) | No | ~28.5s | Low (proven) |
| v0.50 arity types | No | ~13.4s | Medium (36 library types) |
| **TTuple library types + expression trees** | **Yes** | **~10.1s** | **Medium (8 library types)** |
| v0.48 base classes + custom delegates | Callbacks only | ~28.5s? | Low (proven) |

The challenge: the expression tree approach (row 3) is the only path that delivers named params without a build time regression, but it has unresolved architectural concerns from developer review. Sending back to architect to resolve.

- **Architect addressed all 7 developer concerns.** Key resolution: TTuple types are standalone sealed classes (like the arity types), NOT base class inheritors. This eliminates the fallback/source delegation mismatch (Concern 2) and async base class design issues (Concern 3) entirely. The TTuple types duplicate all behavioral logic with TDelegate/TArgs type parameters instead of T1/T2/.../TN. Expression trees bridge TDelegate invocation at runtime. No base classes extended, no async base classes created.
- Updated approach table row 3: complexity reduced from "High (unresolved concerns)" to "Medium (concerns resolved)"
- Kept 8-param limit (Concern 6); documented When predicate breaking change (Concern 5); resolved compositor inner class naming (Concern 4); detailed async simplified callbacks (Concern 7); documented post-implementation Design.Stubs verification plan (Concern 1)
- Plan status changed from "Concerns Raised" to "Under Review (Developer)"

### Compositor Brainstorming (2026-02-15)

Explored approaches to reduce generated compositor boilerplate by moving forwarding methods (Call, When, Return, Verify, Reset) into pre-compiled library code.

**Approach 1: Default Interface Methods (DIM)**
- Generic interface `IVoidOverload<TDelegate, TArgs>` with default Call/When methods
- **Dead.** Requires .NET Core 3.0+ runtime. Default methods only accessible through interface reference, not concrete type.

**Approach 2: Static extension methods on shared generic interface**
- Extension methods on `IVoidOverload<TDelegate, TArgs>`, compositor implements multiple instantiations
- **Dead.** CS1061 when a class implements the same generic interface multiple times — compiler can't resolve which type args to use for extension method lookup. Confirmed via prototype (`src/Prototypes/ExtensionOverloadPrototype/`). Single implementation works; two+ fails.

**Approach 3: Numbered slot interfaces (VIABLE)**
- Separate interfaces per slot: `IVoidOverloadSlot1<TDelegate, TArgs>`, `IVoidOverloadSlot2<TDelegate, TArgs>`, etc. (up to 8)
- Each implemented at most once on a compositor, so compiler resolves extension methods unambiguously
- Prototype confirmed: 33 tests passing (`src/Prototypes/NumberedSlotPrototype/`)
- Library cost: 8 slots × 4 families = 32 interfaces + extension methods. All pre-compiled.
- Edge cases (same-family/same-signature, cross-family/same-TArgs) are invalid C# — overloads must differ by parameter types, so they can't occur in a compositor.

**Approach 3a: Arity-specific slot interfaces (REJECTED)**
- Interfaces with individual type params: `IVoidOverloadSlot1_2<TDelegate, T1, T2>` to eliminate tuple syntax from When
- Would require 8 slots × 8 arities × 4 families = 256 interfaces
- **Rejected:** Loses named When params (shows `arg1, arg2` instead of named tuple elements). Tuple approach is better.

**Approach 3b: Verify/Reset via IInterceptor collection (VIABLE)**
- Add `IInterceptor` interface with `CheckVerification()`, `CheckVerificationAll()`, `Reset()` to all interceptor types
- Compositor exposes `IReadOnlyList<IInterceptor> Interceptors` — one-line generated property
- Verify/Reset become library extension methods iterating the collection
- Eliminates last remaining generated forwarding methods from compositors

**Final compositor shape with slots + IInterceptor:**
```
Generated compositor contains ONLY:
- Interceptor fields (1 per overload)
- Explicit interface property implementations (1 per overload)
- IReadOnlyList<IInterceptor> Interceptors property (1 line)
All behavioral methods (Call, When, Return, Verify, Reset) are library extension methods.
```

---

## Completion Verification

- [x] Design project builds successfully
- [x] Design project tests pass
- [x] Build time benchmark shows improvement vs v0.49.0
- [x] IntelliSense shows named params for Return, Call, ThenReturn, ThenCall
- [x] IntelliSense shows named tuple elements for When (2+ params)

**Verification results:**
- Design build: 0 errors, 0 warnings (all 3 TFMs: net8.0, net9.0, net10.0)
- Design tests: 370 passed, 0 failed (all 3 TFMs)
- Build time: **~10.1s** (65% faster than v0.49.0's 28.5s, 25% faster than v0.50.0's 13.4s)
- IntelliSense (callbacks): Confirmed. Generated delegates have named params (e.g., `delegate int GetValueDelegate(int input)`, `delegate void CalculateDelegate(string name, int value, bool flag)`)
- IntelliSense (When): Confirmed. TArgs uses named ValueTuples for 2+ params (e.g., `When((int a, int b) args)` shows `.a`, `.b`)

**Full test suite results:**
- KnockOffTests: 1710-1711 passed, 4 skipped, 0 failed (all 3 TFMs)
- Design.Tests: 370 passed, 0 failed (all 3 TFMs)
- Documentation.Samples: 691 passed, 0 failed (all 3 TFMs)
- NeatooInterfaceTests: 473 passed, 0 failed (all 3 TFMs)
- AssemblyStrict: 14 passed, 0 failed (all 3 TFMs)
- NumberedSlotPrototype: 33 passed, 0 failed

**Spot-check of generated files:** All 9 patterns verified to use TTuple types. Zero references to old arity types (MethodInterceptor1-8, etc.) in any generated file.

---

## Results / Conclusions

### What was achieved

Replaced 36 arity-based interceptor types (MethodInterceptor{1-8}, VoidMethodInterceptor{1-8}, AsyncMethodInterceptor{1-8}, AsyncVoidMethodInterceptor{1-8}) with 4 TTuple types + 4 zero-param types = **8 library types total** (78% reduction).

### Build time progression

| Version | Build Time | Change |
|---|---|---|
| v0.49.0 (generated classes) | 28.5s | baseline |
| v0.50.0 (arity types) | 13.4s | -53% |
| v0.51.0 (TTuple types) | ~10.1s | -65% from v0.49.0, -25% from v0.50.0 |

### Library type count

| Version | Library Types | Inner Classes | Total |
|---|---|---|---|
| v0.50.0 (arities) | 36 + 4 zero-param = 40 | ~180 | ~220 |
| v0.51.0 (TTuple) | 4 + 4 zero-param = 8 | ~40 | ~48 |

### Key design decisions

1. **TDelegate via generated delegates** — Each 1+ param method gets a custom delegate type (e.g., `delegate int GetValueDelegate(int input)`). Provides named IntelliSense on Return/Call callbacks.
2. **TArgs via ValueTuple** — 1-param uses raw type, 2+ params use named ValueTuple (e.g., `(int a, int b)`). Named tuple elements propagate through generics to When/ThenWhen.
3. **Expression trees for TDelegate invocation** — `DelegateInvokerFactory` compiles expression trees once per type combo. Bridges TDelegate to TArgs at runtime without `Func<>` conversion.
4. **Numbered slot interfaces for compositors** — 32 slot interfaces (8 slots x 4 families) enable pre-compiled extension methods for overload compositor forwarding. Generic compositors skip slot interfaces (delegates are nested inside the compositor class where type params are in scope).
5. **IInterceptor interface** — Unified CheckVerification/CheckVerificationAll/Reset across all interceptor types. Compositors expose `IReadOnlyList<IInterceptor> Interceptors`.

### Breaking changes (test API)

- `When(a, b)` → `When((a, b))` for 2+ param methods (tuple literal syntax)
- `When((a, b) => expr)` → `When(args => args.a ...)` for 2+ param predicates
- `LastArg` → `LastArgs` on TTuple interceptors
- Explicit `Func<>`/`Action<>` casts in tests → generated delegate type casts
- Async compositor disambiguation requires typed tuple params for `Func<TArgs, TReturn>` overloads

### Bug found and fixed during implementation

Open generic overload compositors (patterns 8, 9) emitted delegate declarations outside the generic compositor class, where type parameters like `T` were not in scope. Fixed by moving delegates inside the compositor class body for generic compositors and skipping slot interfaces (which reference delegate types in the base list).

