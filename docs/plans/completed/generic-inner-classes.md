# Generic Inner Classes: Eliminate Generated Thin Subclasses

**Status:** Awaiting Verification
**Created:** 2026-02-13
**Last Updated:** 2026-02-13 (Phase 3 complete, awaiting verification)
**Related Todo:** [Reduce Generated Code Size](../todos/reduce-generated-code-size.md)

---

## Problem Statement

The interceptor base class work (completed in the previous plan) moved interceptor logic into pre-compiled generic base classes, reducing per-interceptor generated code by ~91%. However, every interceptor still generates thin inner subclasses for builders and sequences. These subclasses exist solely to implement typed library interfaces (e.g., `IPropertyGetBuilder<TValue>`, `IMethodCallSequence<TCallback>`) by delegating to `protected` base methods with `Base` suffix.

For example, every get-only property interceptor generates two inner classes totaling ~39 lines:

```csharp
// Generated: ~26 lines
private sealed class PropertyGetBuilderImpl : PropertyGetBuilderBase, IPropertyGetBuilder<TValue>
{
    private readonly NameInterceptor _typedInterceptor;
    public PropertyGetBuilderImpl(NameInterceptor interceptor) : base(interceptor) { _typedInterceptor = interceptor; }
    public IPropertyGetSequence<TValue> ThenGet(Func<TValue> callback) { ThenGetBase(callback); return new PropertyGetSequenceImpl(_typedInterceptor); }
    public IPropertyGetSequence<TValue> ThenGet(TValue value) => ThenGet(() => value);
    public IPropertyGetSequence<TValue> ThenGet(params TValue[] values) { ... }
    public IPropertyGetBuilder<TValue> Verifiable() { VerifiableBase(); return this; }
    IPropertyGetTracking IPropertyGetTracking.Verifiable() => Verifiable();
    IPropertyGetTracking IPropertyGetTracking.Verifiable(Called times) => Verifiable(); // BUG: times parameter discarded
}

// Generated: ~13 lines
private sealed class PropertyGetSequenceImpl : PropertyGetSequenceBase, IPropertyGetSequence<TValue>
{
    private readonly NameInterceptor _typedInterceptor;
    public PropertyGetSequenceImpl(NameInterceptor interceptor) : base(interceptor) { _typedInterceptor = interceptor; }
    public IPropertyGetSequence<TValue> ThenGet(Func<TValue> callback) { ThenGetBase(callback); return this; }
    public IPropertyGetSequence<TValue> ThenGet(TValue value) => ThenGet(() => value);
    public IPropertyGetSequence<TValue> ThenGet(params TValue[] values) { foreach (var v in values) ThenGet(v); return this; }
    public IPropertyGetSequence<TValue> Verifiable() { VerifiableBase(); return this; }
}
```

Since the base classes are already nested inside a generic outer class (e.g., `PropertyGetInterceptorBase<TValue>`), they already have access to `TValue`. If the base classes directly implemented the library interfaces, these generated subclasses would be eliminated entirely.

---

## Overview

This plan analyzes three categories of inner classes and determines which can implement library interfaces directly in the base class (eliminating generated subclasses) versus which must remain generated.

---

## Codebase Investigation

### Files Examined

- `src/KnockOff/Interceptors/PropertyGetInterceptorBase.cs` -- Contains `PropertyGetBuilderBase` (lines 179-229) and `PropertyGetSequenceBase` (lines 235-274)
- `src/KnockOff/Interceptors/PropertySetInterceptorBase.cs` -- Contains `PropertySetBuilderBase` (lines 185-237) and `PropertySetSequenceBase` (lines 243-280)
- `src/KnockOff/Interceptors/PropertyGetSetInterceptorBase.cs` -- Contains its own `PropertySetBuilderBase` (lines 285-337) and `PropertySetSequenceBase` (lines 343-380), separate from the set-only versions
- `src/KnockOff/Interceptors/IndexerGetSetInterceptorBase.cs` -- Contains `IndexerGetBuilderBase`, `IndexerGetSequenceBase`, `IndexerSetBuilderBase`, `IndexerSetSequenceBase`, `IndexerWhenBuilderBase`, `IndexerGetWhenChainBase`, `IndexerSetWhenChainBase`, `PerKeyBuilder`, `PerKeySequence`
- `src/KnockOff/Interceptors/VoidMethodInterceptorBase.cs` -- Contains `MethodCallBuilderBase` (lines 392-457) and `MethodSequenceBase` (lines 464-530), plus When chain inner classes
- `src/KnockOff/Interceptors/MethodInterceptorBase.cs` -- Contains `ReturnMethodCallBuilderBase` (lines 365-421) and `ReturnMethodSequenceBase` (lines 428-459), plus When chain inner classes
- `src/KnockOff/IPropertyCallBuilder.cs` -- `IPropertyGetBuilder<TValue>`, `IPropertySetBuilder<TValue>`
- `src/KnockOff/IPropertySequence.cs` -- `IPropertyGetSequence<TValue>`, `IPropertySetSequence<TValue>`
- `src/KnockOff/IPropertyTracking.cs` -- `IPropertyGetTracking`, `IPropertySetTracking<TValue>`
- `src/KnockOff/IIndexerCallBuilder.cs` -- `IIndexerGetBuilder<TKey, TValue>`, `IIndexerSetBuilder<TKey, TValue>`
- `src/KnockOff/IIndexerSequence.cs` -- `IIndexerGetSequence<TKey, TValue>`, `IIndexerSetSequence<TKey, TValue>`
- `src/KnockOff/IIndexerTracking.cs` -- `IIndexerGetTracking<TKey>`, `IIndexerSetTracking<TKey, TValue>`
- `src/KnockOff/IMethodCallBuilder.cs` -- `IMethodCallBuilder<TCallback>`, `IMethodCallBuilder<TCallback, TArg>`, `IMethodCallBuilderArgs<TCallback, TArgs>`
- `src/KnockOff/IMethodReturnBuilder.cs` -- `IMethodReturnBuilder<TCallback>`, `IMethodReturnBuilder<TCallback, TArg>`, `IMethodReturnBuilderArgs<TCallback, TArgs>`
- `src/KnockOff/IMethodCallSequence.cs` -- `IMethodCallSequence<TCallback>`
- `src/KnockOff/IMethodReturnSequence.cs` -- `IMethodReturnSequence<TCallback>`
- `src/KnockOff/IMethodSequence.cs` -- `IMethodSequence` (base)
- `src/KnockOff/IMethodTracking.cs` -- `IMethodTracking`, `IMethodTracking<TArg>`, `IMethodTrackingArgs<TArgs>`
- `src/Prototype/Prototype.Stubs/GeneratedAnalysis/EntityListBaseStub.g.cs` -- Real-world generated output showing all inner class patterns
- `src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs` -- Generator code producing the thin subclasses

---

## Category Analysis

### Category 1: Property Inner Classes -- FULLY ELIMINABLE

**Base classes affected:**
- `PropertyGetInterceptorBase<TValue>` -- inner classes `PropertyGetBuilderBase`, `PropertyGetSequenceBase`
- `PropertySetInterceptorBase<TValue>` -- inner classes `PropertySetBuilderBase`, `PropertySetSequenceBase`
- `PropertyGetSetInterceptorBase<TValue>` -- inner classes `PropertySetBuilderBase`, `PropertySetSequenceBase`

**Why they can implement interfaces directly:**

The library interfaces are:
- `IPropertyGetBuilder<TValue>` extends `IPropertyGetTracking`
- `IPropertyGetSequence<TValue>`
- `IPropertySetBuilder<TValue>` extends `IPropertySetTracking<TValue>`
- `IPropertySetSequence<TValue>`

All these interfaces are parameterized only by `TValue`, which is already the type parameter of the enclosing base class. The base class inner classes already have access to `TValue` through the outer class.

**Current base class methods (protected, "Base" suffix):**

| Base Class Method | Interface Method | Mapping |
|---|---|---|
| `ThenGetBase(Func<TValue>)` returns `PropertyGetSequenceBase` | `ThenGet(Func<TValue>)` returns `IPropertyGetSequence<TValue>` | Return type changes from `PropertyGetSequenceBase` to `IPropertyGetSequence<TValue>` (covariant - `this` implements the interface) |
| `VerifiableBase()` returns `void` | `Verifiable()` returns `IPropertyGetBuilder<TValue>` | Needs to return `this` |

**Change required:** Rename `protected` methods, add interface implementation, and return `this` where needed. The `ThenGet(TValue value)` and `ThenGet(params TValue[])` overloads currently exist only in generated code and would be added to the base class.

**Verifiable return type covariance issue:**

`IPropertyGetTracking.Verifiable()` returns `IPropertyGetTracking`, while `IPropertyGetBuilder<TValue>.Verifiable()` returns `IPropertyGetBuilder<TValue>`. C# does not support covariant return types on interface implementations. The solution is explicit interface implementation:

```csharp
// In PropertyGetBuilderBase implementing IPropertyGetBuilder<TValue>:
public PropertyGetBuilderBase Verifiable() { _interceptor._isGetVerifiable = true; _interceptor._getVerifiableTimes = null; return this; }
public PropertyGetBuilderBase Verifiable(Called times) { _interceptor._isGetVerifiable = true; _interceptor._getVerifiableTimes = times; return this; }
IPropertyGetBuilder<TValue> IPropertyGetBuilder<TValue>.Verifiable() => (IPropertyGetBuilder<TValue>)Verifiable();
IPropertyGetTracking IPropertyGetTracking.Verifiable() => (IPropertyGetTracking)Verifiable();
IPropertyGetTracking IPropertyGetTracking.Verifiable(Called times) => (IPropertyGetTracking)Verifiable(times); // FIX: passes times (current generated code discards it)
```

This is the same EII pattern the generated code uses today, moved into the base class. The `Verifiable(Called times)` EII is corrected to pass the `times` parameter through rather than discarding it (see Concern 3 resolution below). Since `PropertyGetBuilderBase` implements both interfaces, all casts are safe.

**Lines eliminated per interceptor:**

| Property Type | Inner Classes Eliminated | Lines Saved |
|---|---|---|
| Get-only | `PropertyGetBuilderImpl` + `PropertyGetSequenceImpl` | ~39 lines |
| Set-only | `PropertySetBuilderImpl` + `PropertySetSequenceImpl` | ~35 lines |
| Get+set | All 4 builder/sequence impls | ~74 lines |

---

### Category 2: Indexer Inner Classes -- FULLY ELIMINABLE

**Base class affected:**
- `IndexerGetSetInterceptorBase<TKey, TValue>` -- inner classes `IndexerGetBuilderBase`, `IndexerGetSequenceBase`, `IndexerSetBuilderBase`, `IndexerSetSequenceBase`, `IndexerWhenBuilderBase`, `IndexerGetWhenChainBase`, `IndexerSetWhenChainBase`

**Two distinct elimination mechanisms:**

**2a: Builder and Sequence classes -- eliminated via library interface implementation.**

The library interfaces are:
- `IIndexerGetBuilder<TKey, TValue>` extends `IIndexerGetTracking<TKey>`
- `IIndexerGetSequence<TKey, TValue>`
- `IIndexerSetBuilder<TKey, TValue>` extends `IIndexerSetTracking<TKey, TValue>`
- `IIndexerSetSequence<TKey, TValue>`

All parameterized by `TKey` and `TValue`, which are already the type parameters of the enclosing `IndexerGetSetInterceptorBase<TKey, TValue>`. Same pattern as properties -- rename `Base` methods to interface methods, implement the interfaces, handle Verifiable covariance with explicit interface implementation.

**2b: When chain classes -- eliminated via method renaming (no library interfaces exist).**

There are NO library interfaces `IIndexerWhenBuilder<TKey, TValue>`, `IIndexerGetWhenChain<TKey, TValue>`, or `IIndexerSetWhenChain<TKey, TValue>`. The generated thin subclasses (`IndexerWhenBuilder`, `IndexerGetWhenChain`, `IndexerSetWhenChain`) return concrete types, not interfaces. They exist solely to provide typed `Returns()`, `Get()`, `Set()`, `ThenWhen()`, and `Verifiable()` methods that delegate to `*Base()` methods on the base class.

Since `TKey` and `TValue` are available as outer class type parameters, the base class inner classes can provide these typed methods directly. The change is renaming base methods to their final typed signatures:
- `IndexerWhenBuilderBase.ReturnsBase(TValue)` becomes `Returns(TValue)`
- `IndexerWhenBuilderBase.GetBase(Func<TKey, TValue>)` becomes `Get(Func<TKey, TValue>)`
- `IndexerWhenBuilderBase.SetBase(Action<TKey, TValue>)` becomes `Set(Action<TKey, TValue>)`
- `IndexerGetWhenChainBase.ThenWhenBase(Func<TKey, bool>)` becomes `ThenWhen(Func<TKey, bool>)`
- `IndexerGetWhenChainBase.VerifiableBase()` becomes `Verifiable()` (returns `this`)
- Same pattern for `IndexerSetWhenChainBase`

The return types also change from base types to the base types themselves (no interface cast needed since there are no interfaces to implement). For example, `Returns()` returns `IndexerGetWhenChainBase` instead of requiring a cast. The generated `When()` method on the interceptor must also change to return `IndexerWhenBuilderBase` instead of the generated `IndexerWhenBuilder` type.

**Lines eliminated per indexer interceptor:**

| Inner Class | Lines Saved |
|---|---|
| `IndexerGetBuilderImpl` | ~17 lines |
| `IndexerGetSequenceImpl` | ~12 lines |
| `IndexerSetBuilderImpl` | ~17 lines |
| `IndexerSetSequenceImpl` | ~12 lines |
| `IndexerWhenBuilder` | ~30 lines |
| `IndexerGetWhenChain` | ~19 lines |
| `IndexerSetWhenChain` | ~19 lines |
| **Total per indexer** | **~126 lines** |

---

### Category 3: Method Inner Classes -- PARTIALLY ELIMINABLE

Method inner classes are more complex because of the `TCallback` type parameter.

#### 3a: Builders and Sequences -- NOT directly eliminable

**Base classes affected:**
- `VoidMethodInterceptorBase<TDelegate, TArgs>` -- inner classes `MethodCallBuilderBase`, `MethodSequenceBase`
- `MethodInterceptorBase<TDelegate, TArgs, TReturn>` -- inner classes `ReturnMethodCallBuilderBase`, `ReturnMethodSequenceBase`

**The problem: `TCallback` vs `TDelegate`**

The library interfaces use `TCallback` as their type parameter:
- `IMethodCallBuilder<TCallback>` / `IMethodCallBuilder<TCallback, TArg>` / `IMethodCallBuilderArgs<TCallback, TArgs>`
- `IMethodCallSequence<TCallback>`
- `IMethodReturnBuilder<TCallback>` / `IMethodReturnBuilder<TCallback, TArg>` / `IMethodReturnBuilderArgs<TCallback, TArgs>`
- `IMethodReturnSequence<TCallback>`

In generated code, `TCallback` equals `TDelegate` -- the delegate type parameter of the outer interceptor base class. So `MethodCallBuilderBase` is nested inside `VoidMethodInterceptorBase<TDelegate, TArgs>` and has access to `TDelegate`.

**However**, the builder interfaces also have variants with `TArg`/`TArgs`:
- `IMethodCallBuilder<TCallback, TArg>` has `TArg LastArg { get; }` (from `IMethodTracking<TArg>`)
- `IMethodCallBuilderArgs<TCallback, TArgs>` has `TArgs LastArgs { get; }` (from `IMethodTrackingArgs<TArgs>`)

The `TArg` type depends on the method's parameter count:
- 0 params: No `TArg`, uses `IMethodCallBuilder<TCallback>` (implements `IMethodTracking` with no arg capture)
- 1 param: `TArg` = the single parameter type, uses `IMethodCallBuilder<TCallback, TArg>`
- 2+ params: `TArgs` = a named `ValueTuple` of parameter types, uses `IMethodCallBuilderArgs<TCallback, TArgs>`

The `TArg`/`TArgs` types are NOT type parameters of the enclosing interceptor base class. They are constructed by the generator based on the specific method's parameter list. Therefore, the base class `MethodCallBuilderBase` cannot implement `IMethodCallBuilder<TCallback, TArg>` because it does not know `TArg`.

**What CAN be moved to the base class:**

The `IMethodCallBuilder<TCallback>` interface (no-arg variant, for 0-param methods) and `IMethodCallSequence<TCallback>` / `IMethodReturnSequence<TCallback>` could potentially be implemented by the base class since they only need `TDelegate` (which equals `TCallback`). However, this creates a split:
- 0-param methods: base class implements the interface directly
- 1-param methods: still need generated subclass for `LastArg`
- 2+ param methods: still need generated subclass for `LastArgs`

**The `LastArg`/`LastArgs` tracking fields are already generated.** Each generated `MethodCallBuilderImpl` adds a `_lastArg` field, a `RecordArg()` method, and a `LastArg` property. These are method-specific and cannot be in the base class.

**Recommendation for method builders:** Leave method builder inner classes as generated subclasses. The cost/benefit ratio is poor:
- Only 0-param methods would benefit (partial elimination)
- The `LastArg`/`LastArgs` tracking creates a hard boundary for 1+ param methods
- Splitting behavior between "base class handles 0-param" and "generated class handles 1+ param" adds complexity to the renderer for minimal line savings

#### 3b: Method Sequences -- CAN be eliminated for 0-param and 1-param methods

The sequence interfaces `IMethodCallSequence<TCallback>` and `IMethodReturnSequence<TCallback>` do NOT have `TArg`/`TArgs` variants. They only depend on `TCallback`, which equals `TDelegate`. The base class `MethodSequenceBase` could implement `IMethodCallSequence<TDelegate>` directly.

**However**, the sequence methods `ThenCall(TCallback)` and `ThenReturn(TCallback)` return the sequence itself. The value overloads like `ThenReturn(TReturn value)` and `ThenReturn(params TReturn[] values)` need `TReturn`, which IS available in `MethodInterceptorBase<TDelegate, TArgs, TReturn>`.

**Conclusion for method sequences:** The sequences CAN implement `IMethodCallSequence<TDelegate>` / `IMethodReturnSequence<TDelegate>` in the base class. The only methods that need `TCallback`-specific knowledge are `ThenCall`/`ThenReturn`, which just delegate to `ThenCallBase`/`ThenReturnBase`. These can become direct implementations.

**Recommended approach for methods:**

| Component | Eliminable? | Rationale |
|---|---|---|
| `MethodCallBuilderImpl` (0-param void) | Partially -- but complexity not worth it | Would need separate code path for 0-param vs 1+ param |
| `MethodCallBuilderImpl` (1-param) | No | Needs `TArg LastArg` field |
| `MethodCallBuilderImpl` (2+ param) | No | Needs `TArgs LastArgs` field |
| `MethodSequenceImpl` (void) | Yes | Only depends on `TDelegate` |
| `ReturnMethodCallBuilderImpl` (0-param) | Partially -- same complexity concern | |
| `ReturnMethodCallBuilderImpl` (1-param) | No | Needs `TArg LastArg` field |
| `ReturnMethodCallBuilderImpl` (2+ param) | No | Needs `TArgs LastArgs` field |
| `ReturnMethodSequenceImpl` | Yes | Only depends on `TDelegate` and `TReturn` |
| When chain inner classes (0-param) | N/A | 0-param methods do not generate When chains |
| When chain inner classes (1+ param) | No | `ThenWhen` and `Call` overloads unwrap `TArgs` into named parameters -- parameter-specific |

#### 3c: When Chain Inner Classes -- NOT ELIMINABLE (except for 0-param methods, which have no When chains)

**Corrected analysis:** The developer review identified that method When chains are NOT eliminable for methods with parameters. The generated thin subclasses contain **parameter-specific `ThenWhen` overloads** that unwrap `TArgs` into individual parameters:

```csharp
// Generated: parameter-specific -- depends on method's parameter names and types
public VoidWhenChain ThenWhen(int index, object? @value)
{
    var matcher = new VoidWhenMatcherPredicateBase(
        (args) => Equals(args.index, index) && Equals(args.value, value));
    _typedInterceptor._whenChain.Add(matcher);
    return new VoidWhenChain(_typedInterceptor, matcher);
}

// Generated: parameter-specific callback unwrapping
public VoidWhenChain Call(Action<int, object?> callback)
{
    _typedMatcher?.SetCallback((args) => callback(args.index, args.@value));
    return this;
}
```

The base class only knows `TArgs` as a generic type parameter and cannot decompose it into named tuple fields. These `ThenWhen` and `Call` overloads must remain generated for all methods with 1+ parameters.

**For 0-param methods:** No When chains are generated at all (confirmed by examining `ClearInterceptor`, `ClearAllMessagesInterceptor`, `ClearSelfMessagesInterceptor` in EntityListBaseStub.g.cs). Since the whole purpose of When chains is parameter-specific matching, 0-param methods have no use for them and the generator does not produce When chain classes.

**For non-void When chains (WhenBuilder/WhenChain):** The `WhenBuilder.Return(TReturn)` method uses `TReturn` which IS available in the base class. However, the `WhenChain.ThenWhen(...)` overloads unwrap parameters just like `VoidWhenChain`. The library interfaces `IWhenBuilder<TDelegate, TReturn>`, `IWhenChain<TDelegate, TReturn>`, and `IVoidWhenChain<TDelegate>` exist, but `ThenWhen` overloads are documented as "generated per-method" in the interface comments. The base class cannot implement these overloads.

**Conclusion:** Method When chain inner classes must remain fully generated. No savings from When chains.

**Corrected lines eliminated per method interceptor (sequence only):**

| Method Type | Sequence Savings | When Chain Savings | Total Saved |
|---|---|---|---|
| Void 0-param | ~28 lines | 0 (no When chains generated) | ~28 lines |
| Void 1-param | ~28 lines | 0 (parameter-specific ThenWhen) | ~28 lines |
| Void 2+ param | ~28 lines | 0 (parameter-specific ThenWhen) | ~28 lines |
| Return 0-param | ~31 lines | 0 (no When chains generated) | ~31 lines |
| Return 1-param | ~31 lines | 0 (parameter-specific ThenWhen) | ~31 lines |
| Return 2+ param | ~31 lines | 0 (parameter-specific ThenWhen) | ~31 lines |

---

## Estimated Impact

### Per-Interceptor Line Reduction

| Interceptor Type | Current Inner Class Lines | Lines Eliminated | Remaining |
|---|---|---|---|
| Get-only property | ~39 | ~39 (100%) | 0 |
| Set-only property | ~35 | ~35 (100%) | 0 |
| Get+set property | ~74 | ~74 (100%) | 0 |
| Indexer (get+set) | ~126 | ~126 (100%) | 0 |
| Void method (0-param) | ~28 | ~28 (100%) | 0 (no builder -- 0-param uses base; no When chains) |
| Void method (1-param) | ~100 | ~28 (~28%) | ~72 (builder + LastArg + When chains) |
| Void method (2+ param) | ~110 | ~28 (~25%) | ~82 (builder + LastArgs + When chains) |
| Return method (0-param) | ~62 | ~31 (~50%) | ~31 (builder + ThenReturn value overloads) |
| Return method (1-param) | ~130 | ~31 (~24%) | ~99 (builder + LastArg + When chains) |
| Return method (2+ param) | ~140 | ~31 (~22%) | ~109 (builder + LastArgs + When chains) |

**Note:** Previous estimates claimed ~50 lines of When chain savings per method. This was incorrect -- method When chain thin subclasses contain parameter-specific `ThenWhen` and `Call` overloads that cannot be moved to the base class. Only sequences are eliminable for methods.

### Aggregate Impact (EntityListBaseStub.g.cs example)

The EntityListBaseStub has:
- 17 get-only properties = 17 x 39 = **663 lines eliminated**
- 1 indexer = 1 x 126 = **126 lines eliminated**
- ~10 methods (mix of void/return, 0/1/2+ params) = ~10 x 30 avg = **300 lines eliminated** (sequences only)

**Total estimated: ~1,089 lines eliminated** from a file that is currently ~5,813 lines (the generated portion after base class changes). This represents a further **~19% reduction** on top of the previous ~91% reduction.

---

## Implementation Phases

### Phase 1: Property Inner Classes (Lowest Risk)

**Scope:** Make `PropertyGetBuilderBase`, `PropertyGetSequenceBase`, `PropertySetBuilderBase`, and `PropertySetSequenceBase` implement their respective library interfaces directly.

**Steps:**

1. **Modify `PropertyGetInterceptorBase<TValue>`:**
   - `PropertyGetBuilderBase` implements `IPropertyGetBuilder<TValue>`, `IPropertyGetTracking`
   - Rename `ThenGetBase(Func<TValue>)` to `ThenGet(Func<TValue>)` returning `IPropertyGetSequence<TValue>`
   - Add `ThenGet(TValue value)` and `ThenGet(params TValue[] values)` overloads
   - Add `Verifiable()` returning `IPropertyGetBuilder<TValue>` with explicit interface implementations for `IPropertyGetTracking.Verifiable()` and `IPropertyGetTracking.Verifiable(Called)`
   - **Fix `Verifiable(Called times)` bug:** The explicit interface implementation for `IPropertyGetTracking.Verifiable(Called times)` must delegate to `VerifiableBase(times)` (passing the `times` parameter), NOT to the parameterless `Verifiable()`. The current generated code silently discards the `times` parameter -- this is a pre-existing bug. Fix it when moving to the base class. Same fix applies to all Verifiable(Called) EII implementations across all builder/sequence classes.
   - `PropertyGetSequenceBase` implements `IPropertyGetSequence<TValue>`
   - Rename `ThenGetBase(Func<TValue>)` to `ThenGet(Func<TValue>)` returning `IPropertyGetSequence<TValue>` (returns `this`)
   - Add `ThenGet(TValue value)` and `ThenGet(params TValue[] values)` overloads
   - Add `Verifiable()` returning `IPropertyGetSequence<TValue>`
   - **Change base class `Get()` return type** from `PropertyGetBuilderBase` to `IPropertyGetBuilder<TValue>`. This is a public API change to the base class. Since `PropertyGetBuilderBase` now implements `IPropertyGetBuilder<TValue>`, the method body remains the same (returns the same object) but the return type widens to the interface. The `Get(TValue value)` overload changes similarly.

2. **Repeat for `PropertySetInterceptorBase<TValue>`** -- same pattern for set-side classes, including `Set()` return type change from `PropertySetBuilderBase` to `IPropertySetBuilder<TValue>`

3. **Repeat for `PropertyGetSetInterceptorBase<TValue>`** -- set-side classes within the get+set base

4. **Update `PropertyInterceptorRenderer.cs`:**
   - Remove `RenderBaseClassPropertyGetBuilderImpl()` method
   - Remove `RenderBaseClassPropertyGetSequenceImpl()` method
   - Remove `RenderBaseClassPropertySetBuilderImpl()` method
   - Remove `RenderBaseClassPropertySetSequenceImpl()` method
   - Remove the generated `new Get()` / `new Set()` methods from interceptors (the base class `Get()` / `Set()` now returns the correct interface type, so no shadowing is needed)
   - Update `RenderBaseClassGetOnlyContent()` to use `PropertyGetBuilderBase` directly (no `PropertyGetBuilderImpl`)
   - Update `RenderBaseClassGetSetContent()` similarly
   - Update `RenderBaseClassSetOnlyContent()` similarly

5. **Verify:** Build solution, run all tests

**Base class `Get()`/`Set()` return type change (explicit):**

Current base class signature:
```csharp
public PropertyGetBuilderBase Get(Func<TValue> callback) { ... }
```

After this change:
```csharp
public IPropertyGetBuilder<TValue> Get(Func<TValue> callback) { ... }
```

The generated interceptor currently shadows with `new Get()` returning `IPropertyGetBuilder<TValue>`:
```csharp
public new IPropertyGetBuilder<TValue> Get(Func<TValue> callback) { ... }
```

After the base class returns `IPropertyGetBuilder<TValue>` directly, the generated `new Get()` is no longer needed -- the base implementation is correct. This is a significant simplification: generated interceptors only need constructors, `InvokeGetUnconfigured`, and source delegation.

### Phase 2: Indexer Inner Classes

**Scope:** Make all indexer builder/sequence inner classes implement their library interfaces directly, and rename When chain `*Base()` methods to final typed methods (no library interfaces exist for When chains).

**Steps:**

1. **Modify `IndexerGetSetInterceptorBase<TKey, TValue>` -- Builders and Sequences (interface implementation):**
   - `IndexerGetBuilderBase` implements `IIndexerGetBuilder<TKey, TValue>`, `IIndexerGetTracking<TKey>`
   - `IndexerGetSequenceBase` implements `IIndexerGetSequence<TKey, TValue>`
   - `IndexerSetBuilderBase` implements `IIndexerSetBuilder<TKey, TValue>`, `IIndexerSetTracking<TKey, TValue>`
   - `IndexerSetSequenceBase` implements `IIndexerSetSequence<TKey, TValue>`
   - Same Verifiable covariance / EII pattern as properties

2. **Modify `IndexerGetSetInterceptorBase<TKey, TValue>` -- When chains (method renaming):**
   - `IndexerWhenBuilderBase`: Rename `ReturnsBase(TValue)` to `Returns(TValue)`, `GetBase(Func<TKey, TValue>)` to `Get(Func<TKey, TValue>)`, `SetBase(Action<TKey, TValue>)` to `Set(Action<TKey, TValue>)`. Return types change from base types to themselves (e.g., `Returns()` returns `IndexerGetWhenChainBase` directly).
   - `IndexerGetWhenChainBase`: Rename `ThenWhenBase(Func<TKey, bool>)` to `ThenWhen(Func<TKey, bool>)`, `VerifiableBase()` to `Verifiable()` returning `this`.
   - `IndexerSetWhenChainBase`: Same pattern as `IndexerGetWhenChainBase`.
   - Note: No library interfaces to implement -- these return concrete base types.

3. **Update `IndexerInterceptorRenderer.cs`:**
   - Remove all inner class generation for base class mode (builders, sequences, AND When chains)

4. **Verify:** Build solution, run all tests

### Phase 3: Method Sequence Inner Classes (Sequences Only -- When Chains Remain Generated)

**Scope:** Make `MethodSequenceBase` and `ReturnMethodSequenceBase` implement their library interfaces. Leave builder AND When chain inner classes generated.

**Why When chains are excluded:** Method When chain thin subclasses contain parameter-specific `ThenWhen` and `Call` overloads that unwrap `TArgs` into individual named parameters. The base class only knows `TArgs` as a generic type and cannot decompose it. For 0-param methods, no When chains are generated at all (nothing to match on). See Section 3c for detailed analysis.

**Steps:**

1. **Modify `VoidMethodInterceptorBase<TDelegate, TArgs>`:**
   - `MethodSequenceBase` implements `IMethodCallSequence<TDelegate>`, `IMethodSequence`
   - Rename `ThenCallBase(TDelegate)` to `ThenCall(TDelegate)` returning `IMethodCallSequence<TDelegate>`
   - Add `Verifiable()` returning `IMethodCallSequence<TDelegate>`

2. **Modify `MethodInterceptorBase<TDelegate, TArgs, TReturn>`:**
   - `ReturnMethodSequenceBase` implements `IMethodReturnSequence<TDelegate>`, `IMethodSequence`
   - Rename `ThenReturnBase(TDelegate)` to `ThenReturn(TDelegate)` returning `IMethodReturnSequence<TDelegate>`
   - Add `ThenReturn(TReturn value)` and `ThenReturn(params TReturn[] values)` overloads
   - Add `Verifiable()` returning `IMethodReturnSequence<TDelegate>`

3. **Update `MethodInterceptorRenderer.cs`:**
   - Remove sequence inner class generation for base class mode
   - Keep builder inner class generation (still needed for `LastArg`/`LastArgs`)
   - Keep When chain inner class generation (still needed for parameter-specific `ThenWhen`/`Call` overloads)

4. **Verify:** Build solution, run all tests

### Phase 4: Method Builder Simplification (Optional, Lower Priority)

**Scope:** For 0-param methods only, explore whether `MethodCallBuilderBase` can implement `IMethodCallBuilder<TDelegate>` directly (since there is no `LastArg`).

**Decision criteria:** If the renderer complexity of having two code paths (0-param base class vs 1+ param generated) outweighs the per-interceptor savings (~3 lines for void, ~31 lines for return), skip this phase.

**Recommendation:** Skip Phase 4. The savings are marginal and the renderer complexity is not justified.

---

## Risks

### Risk 1: Verifiable() Return Type Covariance (Medium Likelihood, Medium Impact)

**Description:** Interface methods like `IPropertyGetBuilder<TValue>.Verifiable()` return `IPropertyGetBuilder<TValue>`, while the base interface `IPropertyGetTracking.Verifiable()` returns `IPropertyGetTracking`. C# does not support covariant return types on interface implementations.

**Mitigation:** Use explicit interface implementation (EII). The base class provides a concrete `Verifiable()` method that returns the concrete type, and explicit implementations for each interface slot cast and return. This is the exact same pattern used in the current generated thin subclasses.

### Risk 2: PropertyGetBuilderBase Creates PropertyGetSequenceBase in ThenGet (Low Likelihood, Medium Impact)

**Description:** Currently, `ThenGetBase()` does `return new PropertyGetSequenceBase(_interceptor)`. After the change, it should return `this` cast to `IPropertyGetSequence<TValue>` -- but `PropertyGetBuilderBase` does not implement `IPropertyGetSequence<TValue>`. It creates a separate `PropertyGetSequenceBase` instance.

**Mitigation:** This is not a covariance problem -- the builder creates and returns a new sequence object. The change is: `new PropertyGetSequenceBase(...)` (which now implements `IPropertyGetSequence<TValue>`) is returned as `IPropertyGetSequence<TValue>`. This works naturally since `PropertyGetSequenceBase` now implements the interface.

### Risk 3: Breaking Change to Protected API (Low Likelihood, High Impact)

**Description:** Renaming `ThenGetBase()` to `ThenGet()` changes the protected API surface. Any user who has subclassed these base classes (unlikely but possible) would break.

**Mitigation:** Since the base classes were just introduced in the previous plan and have not been released, there are no external consumers to break. If a release has already occurred, the old `Base`-suffixed methods could be kept as `[Obsolete]` wrappers.

### Risk 4: Generated Code That Shadows Base Methods (Medium Likelihood, Low Impact)

**Description:** Generated interceptor classes currently have `new Get()` methods that return the typed builder. After the base class directly returns the interface type, the generated `new Get()` may conflict or become unnecessary.

**Mitigation:** Remove the generated `new Get()` / `new Set()` methods. The base class `Get()` now returns `IPropertyGetBuilder<TValue>`, which is the correct return type. The generated interceptor only needs to provide the constructor, `InvokeGetUnconfigured`, and source delegation -- no builder/sequence code at all.

### Risk 5: `params` Array Overloads Require `TValue` to Be Non-Abstract (Low Likelihood, Low Impact)

**Description:** Adding `ThenGet(params TValue[] values)` to the base class requires `TValue[]` array creation, which works for all types including interfaces and abstract classes.

**Mitigation:** No issue -- C# allows array creation for any type, including interfaces (`new IFoo[0]` is valid). The `params` keyword works with any type parameter.

### Risk 6: Init-Only and Ref-Return Properties Use Inline Mode (Low Likelihood, No Impact)

**Description:** Init-only and ref-return properties use "inline mode" (fully-generated interceptors, no base class). This plan's changes only affect base class mode.

**Mitigation:** No action needed. Init-only and ref-return properties are already excluded from base class mode and will continue to generate their own builder/sequence classes as before.

---

## Patterns Affected

All 9 patterns use the same shared renderers, so all are affected equally:

| # | Pattern | Impact |
|---|---|---|
| 1 | Standalone | Property/indexer inner classes eliminated, method sequences eliminated |
| 2 | Generic Standalone | Same |
| 3 | Standalone Class | Same |
| 4 | Generic Standalone Class | Same |
| 5 | Inline Interface | Same |
| 6 | Inline Class | Same |
| 7 | Inline Delegate | N/A (delegates don't have property/indexer interceptors) |
| 8 | Open Generic Interface | Same |
| 9 | Open Generic Class | Same |

---

## Member Types Affected

| Member Type | Inner Classes Eliminated | Inner Classes Remaining |
|---|---|---|
| Properties | All (builder + sequence) | None |
| Indexers | All (builder + sequence + When chain) | None |
| Methods | Sequences only | Builders (due to `LastArg`/`LastArgs`) + When chains (due to parameter-specific `ThenWhen`/`Call`) |
| Events | N/A (no builder/sequence inner classes) | N/A |

---

## Open Questions

1. **Have the interceptor base classes been released to NuGet yet?** If yes, renaming `ThenGetBase()` to `ThenGet()` is a breaking change to the protected API. If no, there is no compatibility concern.

2. ~~**Should the generated interceptor's `Get()`/`Set()` methods be removed?**~~ **RESOLVED:** Yes. The base class `Get()` return type changes from `PropertyGetBuilderBase` to `IPropertyGetBuilder<TValue>`. The generated `new Get()` is no longer needed. See Phase 1 steps for explicit detail.

3. ~~**Are there indexer When chain library interfaces?**~~ **RESOLVED:** No. There are no `IIndexerWhenBuilder`, `IIndexerGetWhenChain`, or `IIndexerSetWhenChain` interfaces. The indexer When chain thin subclasses are eliminated by renaming `*Base()` methods to final typed methods on the base class, not by implementing interfaces. See corrected Category 2 analysis.

---

## Acceptance Criteria

1. All property interceptor inner class thin subclasses (`PropertyGetBuilderImpl`, `PropertyGetSequenceImpl`, `PropertySetBuilderImpl`, `PropertySetSequenceImpl`) eliminated from base class mode generated code
2. All indexer interceptor inner class thin subclasses (builder, sequence, and When chain) eliminated from base class mode generated code
3. All method sequence inner class thin subclasses (`MethodSequenceImpl`, `ReturnMethodSequenceImpl`) eliminated from base class mode generated code
4. Method builder inner classes (`MethodCallBuilderImpl`) continue to be generated (for `LastArg`/`LastArgs` tracking)
5. Method When chain inner classes (`VoidWhenChain`, `WhenBuilder`, `WhenChain`) continue to be generated (for parameter-specific `ThenWhen`/`Call` overloads)
6. Base class `Get()` returns `IPropertyGetBuilder<TValue>` (not `PropertyGetBuilderBase`) -- generated `new Get()` eliminated
7. Base class `Set()` returns `IPropertySetBuilder<TValue>` (not `PropertySetBuilderBase`) -- generated `new Set()` eliminated
8. `Verifiable(Called times)` EII implementations correctly pass `times` parameter through (bug fix)
9. All existing tests pass without modification
10. Design projects compile and tests pass
11. Init-only and ref-return properties (inline mode) are unaffected

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-02-13 (initial), 2026-02-13 (re-review after architect resolution)

### My Understanding of This Plan

**Core Change:** Make interceptor base class inner classes (builders, sequences, When chains) implement library interfaces directly, eliminating the generated thin subclasses that bridge between base classes and library interfaces.

**User-Facing API:** No change to user-facing API. Users continue to interact through library interfaces (`IPropertyGetBuilder<TValue>`, etc.).

**Internal Changes:** (1) Base class inner classes gain interface implementations, (2) renderer methods that generate thin subclasses are removed, (3) generated interceptors no longer need `new Get()`/`new Set()` methods.

**Patterns Affected:** All 9 patterns equally (shared renderers). Inline mode (init-only, ref return, async) is unaffected.

### Re-Review: Verification of Architect Resolutions

All five concerns from the initial review have been adequately addressed.

**Concern 1 (Indexer When chains): RESOLVED.** The plan now explicitly separates two elimination mechanisms in Category 2: (a) builder/sequence classes eliminated via library interface implementation, and (b) When chain classes eliminated via method renaming (no library interfaces exist). The savings estimate is unchanged since the thin subclasses are still eliminable. The corrected rationale is accurate -- confirmed that `TKey` and `TValue` are available through the outer class `IndexerGetSetInterceptorBase<TKey, TValue>`, so `ReturnsBase(TValue)` can become `Returns(TValue)` directly.

**Concern 2 (Method When chains): RESOLVED.** Section 3c is rewritten as "NOT ELIMINABLE" with a detailed explanation citing parameter-specific `ThenWhen` overloads that unwrap `TArgs` into named parameters. The recommended approach table is updated. Phase 3 is correctly scoped to sequences only. Acceptance criteria 4 and 5 explicitly state method builders and When chains remain generated. Savings tables correctly show 0 When chain savings for methods.

**Concern 3 (Verifiable(Called times) bug): RESOLVED.** Decision is to fix the bug when moving code to the base class. The before/after code example is clear. The fix is straightforward: `Verifiable(Called times)` EII now delegates to `Verifiable(times)` instead of `Verifiable()`. Acceptance criterion 8 explicitly calls this out.

**Concern 4 (Get()/Set() return type): RESOLVED.** Phase 1 now has an explicit step with before/after code showing the base class `Get()` return type change from `PropertyGetBuilderBase` to `IPropertyGetBuilder<TValue>`. Acceptance criteria 6 and 7 cover both `Get()` and `Set()`.

**Concern 5 (Line savings): RESOLVED.** Aggregate estimate recalculated to ~1,089 lines / ~19% reduction (down from ~1,539 / ~26%). Method savings correctly reflect sequence-only elimination. The revised estimate is reasonable and still substantial.

### Codebase Re-Investigation

**Additional verification performed during re-review:**

1. **Confirmed the Verifiable(Called times) bug in both the renderer and generated code:**
   - `PropertyInterceptorRenderer.cs:684` generates `Verifiable(Called times) => Verifiable()` -- times discarded
   - `PropertyInterceptorRenderer.cs:1544` (inline mode) has the same bug
   - `EntityListBaseStub.g.cs:76` confirms the bug in generated output
   - `VerifiableBase(Called times)` overload EXISTS in base classes (`PropertyGetInterceptorBase.cs:224`, `PropertySetInterceptorBase.cs:232`, `VoidMethodInterceptorBase.cs:452`, `PropertyGetSetInterceptorBase.cs:332`), so the fix is simply calling `VerifiableBase(times)` instead of `VerifiableBase()`.

2. **Verified indexer base classes lack `VerifiableBase(Called times)`:**
   - `IndexerGetBuilderBase` and `IndexerSetBuilderBase` only have parameterless `VerifiableBase()`.
   - But `IIndexerGetTracking<TKey>.Verifiable(Called called)` exists in the library interface.
   - **Implementation note:** Adding `VerifiableBase(Called times)` to indexer builder bases is required to implement `IIndexerGetTracking<TKey>`. This is additive (not in the plan text, but implied by the requirement to implement the interface). The implementer should be aware.

3. **Verified `IMethodReturnSequence<TCallback>` does NOT have `ThenReturn(TReturn value)` or `ThenReturn(params TReturn[] values)` overloads.** The plan's Phase 3 step 2 mentions "Add `ThenReturn(TReturn value)` and `ThenReturn(params TReturn[] values)` overloads" -- these are not interface methods. They are convenience methods that currently exist in generated code and need to move to the base class. The base class `ReturnMethodSequenceBase` already has `ThenReturnValueBase(TReturn value)` which wraps `ThenReturnBase`, so the change is a rename plus making the methods public. This is fine.

4. **Verified all library interface type parameters match base class outer type parameters:**
   - `IPropertyGetBuilder<TValue>` -- `TValue` from `PropertyGetInterceptorBase<TValue>` -- match
   - `IIndexerGetBuilder<TKey, TValue>` -- `TKey, TValue` from `IndexerGetSetInterceptorBase<TKey, TValue>` -- match
   - `IMethodCallSequence<TCallback>` -- `TCallback` = `TDelegate` from `VoidMethodInterceptorBase<TDelegate, TArgs>` -- match (note: library uses `TCallback`, base uses `TDelegate`, but they bind to the same type)
   - `IMethodReturnSequence<TCallback>` -- same situation, and `TReturn` available from `MethodInterceptorBase<TDelegate, TArgs, TReturn>` -- match

### Why This Plan Is Approved

The revised plan is exceptionally clear for the following reasons:

1. **All five concerns are fully resolved.** The corrections are substantive, not cosmetic. The architect understood the root causes and made targeted fixes.

2. **The scope is well-defined.** Each phase has clear boundaries -- which inner classes are eliminated (properties: all; indexers: all; methods: sequences only) and which remain generated (method builders, method When chains).

3. **The implementation steps are specific enough to code from.** Phase 1 lists exact method renames, interface implementations, EII patterns, and return type changes. Phases 2 and 3 follow the same pattern.

4. **Edge cases are addressed.** Init-only/ref-return properties excluded (Risk 6). Verifiable covariance handled with EII (Risk 1). Method `TArg`/`TArgs` boundary correctly prevents builder elimination. 0-param methods correctly identified as having no When chains.

5. **The phased approach enables incremental verification.** Each phase can be built and tested independently, with Phase 1 (properties) being the lowest risk starting point.

6. **Inline mode is explicitly unaffected.** The plan only modifies base-class-mode rendering, leaving inline mode (init-only, ref return, regular non-base-class properties) untouched. The renderer already branches on `useBaseClass`.

### Implementation Notes for the Developer

1. **Indexer `VerifiableBase(Called times)` gap:** The indexer builder bases (`IndexerGetBuilderBase`, `IndexerSetBuilderBase`) lack `VerifiableBase(Called times)`. When implementing Phase 2, add this method to both before implementing the library interfaces. Model after the property builder pattern at `PropertyGetInterceptorBase.cs:224-228`.

2. **Method sequence `Verifiable()` covariance:** `IMethodCallSequence<TCallback>.Verifiable()` returns `IMethodCallSequence<TCallback>`, but `IMethodSequence.Verifiable()` returns `IMethodSequence`. The same EII pattern used for property builders applies here.

3. **The generated `new Get()`/`new Set()` creates `PropertyGetBuilderImpl`, not `PropertyGetBuilderBase`.** After the change, the base class `Get()` creates `PropertyGetBuilderBase` which now implements the interface directly. Make sure the base class `Get()` body creates `PropertyGetBuilderBase` (not a subclass), and that `PropertyGetBuilderBase` is the concrete type that implements `IPropertyGetBuilder<TValue>`.

4. **Two separate `PropertySetBuilderBase` classes exist** -- one in `PropertySetInterceptorBase<TValue>` and one in `PropertyGetSetInterceptorBase<TValue>`. Both need the same interface implementation treatment.

---

## Architect Resolution

**Date:** 2026-02-13

All five developer concerns have been addressed. Here is the resolution for each:

### Concern 1 Resolution: Indexer When chain classes -- rationale corrected

**Status:** Corrected.

The developer is right: no library interfaces exist for indexer When chains (`IIndexerWhenBuilder`, `IIndexerGetWhenChain`, `IIndexerSetWhenChain` do not exist). The original plan incorrectly assumed they might exist (Open Question #3) while simultaneously treating them as eliminable in the savings tables.

**Correction:** Category 2 now explicitly separates two elimination mechanisms:
- **Builder/Sequence classes** -- eliminated via library interface implementation (same as properties)
- **When chain classes** -- eliminated via method renaming (`*Base()` methods become final typed methods), NOT via interface implementation

The thin subclasses ARE still eliminable because `TKey` and `TValue` are available from the outer class. The mechanism is different (renaming vs implementing interfaces) but the outcome is the same: no generated thin subclasses needed.

**Indexer line savings remain unchanged** (~126 lines per indexer interceptor). The savings were correctly calculated -- only the rationale was wrong.

### Concern 2 Resolution: Method When chain classes -- corrected to NOT eliminable

**Status:** Corrected. Significant impact on estimates.

The developer identified two fundamental problems with the "FULLY ELIMINABLE" claim:

1. **`ThenWhen` overloads unwrap `TArgs` into named parameters.** The base class only knows `TArgs` as a generic type and cannot decompose `(int index, object? @value)` into `ThenWhen(int index, object? @value)`. These are parameter-specific and must remain generated.

2. **`Call` overloads unwrap `TArgs` similarly.** `VoidWhenChain.Call(Action<int, object?> callback)` wraps to `(args) => callback(args.index, args.@value)`.

3. **0-param methods have no When chains at all.** Confirmed by examining `ClearInterceptor`, `ClearAllMessagesInterceptor`, and `ClearSelfMessagesInterceptor` in EntityListBaseStub.g.cs -- none generate When chain classes. The generator correctly omits them since there are no parameters to match on.

**Corrections made:**
- Section 3c rewritten: "NOT ELIMINABLE" with detailed explanation
- Recommended approach table updated: When chains split by parameter count
- Phase 3 renamed and scope reduced to sequences only
- When chains explicitly listed as "continue to be generated" in acceptance criteria

### Concern 3 Resolution: `Verifiable(Called times)` bug -- fix it

**Status:** Decision made. Fix the bug.

The generated code has a pre-existing bug where `IPropertyGetTracking.Verifiable(Called times)` delegates to the parameterless `Verifiable()`, silently discarding the `times` parameter. When moving this code to the base class, we will fix it:

```csharp
// Before (generated -- bug):
IPropertyGetTracking IPropertyGetTracking.Verifiable(Called times) => Verifiable(); // times discarded!

// After (base class -- fixed):
IPropertyGetTracking IPropertyGetTracking.Verifiable(Called times) => (IPropertyGetTracking)Verifiable(times); // times passed through
```

**Rationale:** This is a bug fix, not a behavior change. The `Called` constraint being silently ignored is clearly unintended. Fixing it during the move to the base class is the right time because:
- The code is being rewritten anyway
- The bug-to-correct behavior change is unambiguously an improvement
- No user would intentionally depend on the times parameter being discarded

**Updated in:** Phase 1 step 1, Category 1 code example, Acceptance Criteria #8.

### Concern 4 Resolution: `Get()`/`Set()` return type change -- made explicit

**Status:** Made explicit in Phase 1.

The developer correctly identified that the plan hinted at but never explicitly stated the base class `Get()` return type change. The change is now documented in Phase 1 with a before/after code example:

```csharp
// Before:
public PropertyGetBuilderBase Get(Func<TValue> callback) { ... }

// After:
public IPropertyGetBuilder<TValue> Get(Func<TValue> callback) { ... }
```

This change is what enables eliminating the generated `new Get()` method entirely. The same applies to `Set()`.

**Updated in:** Phase 1 step 1 (new bullet point), Phase 1 code block at end, Acceptance Criteria #6 and #7.

### Concern 5 Resolution: Line savings recalculated

**Status:** Corrected.

The estimated impact tables have been recalculated:
- **Indexer savings:** Unchanged (~126 lines). The savings were correct; only the rationale needed correction.
- **Method savings:** Reduced significantly. Previous estimates claimed ~50 lines of When chain savings per method. Actual When chain savings are 0 lines (they must remain generated). Only sequence savings (~28-31 lines per method) apply.

**Revised aggregate for EntityListBaseStub.g.cs:**
- Properties: 663 lines (unchanged)
- Indexer: 126 lines (unchanged)
- Methods: ~300 lines (down from ~750 -- sequences only, no When chain savings)
- **Total: ~1,089 lines** (down from ~1,539) = **~19% further reduction** (down from ~26%)

The reduction is still substantial and worthwhile, just ~29% less than originally estimated.

---

## Implementation Contract

**Created:** 2026-02-13
**Approved by:** knockoff-developer

### Acceptance Criteria

All 11 acceptance criteria from the plan's "Acceptance Criteria" section apply. The compiler and test suite are the verification authority.

### In Scope

**Phase 1: Property Inner Classes**
- [x] `PropertyGetInterceptorBase<TValue>`: `PropertyGetBuilderBase` implements `IPropertyGetBuilder<TValue>`, `IPropertyGetTracking`
- [x] `PropertyGetInterceptorBase<TValue>`: `PropertyGetSequenceBase` implements `IPropertyGetSequence<TValue>`
- [x] `PropertyGetInterceptorBase<TValue>`: Change `Get()` return type from `PropertyGetBuilderBase` to `IPropertyGetBuilder<TValue>`
- [x] `PropertySetInterceptorBase<TValue>`: `PropertySetBuilderBase` implements `IPropertySetBuilder<TValue>`, `IPropertySetTracking<TValue>`
- [x] `PropertySetInterceptorBase<TValue>`: `PropertySetSequenceBase` implements `IPropertySetSequence<TValue>`
- [x] `PropertySetInterceptorBase<TValue>`: Change `Set()` return type from `PropertySetBuilderBase` to `IPropertySetBuilder<TValue>`
- [x] `PropertyGetSetInterceptorBase<TValue>`: Same changes for its own `PropertySetBuilderBase` and `PropertySetSequenceBase`
- [x] Fix `Verifiable(Called times)` bug: all EII implementations pass `times` through (not discard it)
- [x] `PropertyInterceptorRenderer.cs`: Remove `RenderBaseClassPropertyGetBuilderImpl`, `RenderBaseClassPropertyGetSequenceImpl`, `RenderBaseClassPropertySetBuilderImpl`, `RenderBaseClassPropertySetSequenceImpl`
- [x] `PropertyInterceptorRenderer.cs`: Remove generated `new Get()` / `new Set()` methods from `RenderBaseClassGetOnlyContent`, `RenderBaseClassSetOnlyContent`, `RenderBaseClassGetSetContent`
- [x] **Checkpoint: Build solution, run all tests**

**Phase 2: Indexer Inner Classes**
- [x] `IndexerGetSetInterceptorBase<TKey, TValue>`: Add `Verifiable(Called called)` to `IndexerGetBuilderBase` and `IndexerSetBuilderBase` (was missing -- now added with EII pattern matching properties)
- [x] `IndexerGetSetInterceptorBase<TKey, TValue>`: `IndexerGetBuilderBase` implements `IIndexerGetBuilder<TKey, TValue>`, `IIndexerGetTracking<TKey>`
- [x] `IndexerGetSetInterceptorBase<TKey, TValue>`: `IndexerGetSequenceBase` implements `IIndexerGetSequence<TKey, TValue>`
- [x] `IndexerGetSetInterceptorBase<TKey, TValue>`: `IndexerSetBuilderBase` implements `IIndexerSetBuilder<TKey, TValue>`, `IIndexerSetTracking<TKey, TValue>`
- [x] `IndexerGetSetInterceptorBase<TKey, TValue>`: `IndexerSetSequenceBase` implements `IIndexerSetSequence<TKey, TValue>`
- [x] `IndexerGetSetInterceptorBase<TKey, TValue>`: Rename When chain `*Base()` methods to final typed methods (`ReturnsBase` -> `Returns`, `GetBase` -> `Get`, `SetBase` -> `Set`, `ThenWhenBase` -> `ThenWhen`, `VerifiableBase` -> `Verifiable`)
- [x] `IndexerInterceptorRenderer.cs`: Remove all inner class generation for base class mode (builders, sequences, When chains)
- [x] **Checkpoint: Build solution, run all tests**

**Phase 3: Method Sequence Inner Classes**
- [x] `VoidMethodInterceptorBase<TDelegate, TArgs>`: `MethodSequenceBase` implements `IMethodCallSequence<TDelegate>`, `IMethodCallSequence`, `IMethodSequence`
- [x] `MethodInterceptorBase<TDelegate, TArgs, TReturn>`: `ReturnMethodSequenceBase` implements `IMethodReturnSequence<TDelegate>`, `IMethodReturnSequence`, `IMethodSequence`
- [x] `MethodInterceptorBase<TDelegate, TArgs, TReturn>`: Add `ThenReturn(TReturn value)` and `ThenReturn(params TReturn[] values)` convenience methods to `ReturnMethodSequenceBase`
- [x] `MethodInterceptorRenderer.cs`: Remove sequence inner class generation for base class mode
- [x] `MethodInterceptorRenderer.cs`: Keep builder inner class generation (for `LastArg`/`LastArgs`)
- [x] `MethodInterceptorRenderer.cs`: Keep When chain inner class generation (for parameter-specific `ThenWhen`/`Call`)
- [x] **Checkpoint: Build solution, run all tests**

### Explicitly Out of Scope

- **Phase 4 (method builder 0-param optimization):** Skipped per plan recommendation. Marginal savings vs renderer complexity.
- **Inline mode changes:** Init-only, ref-return, and regular non-base-class properties use fully-generated interceptors. This plan only modifies base-class mode.
- **Method When chain elimination:** Method When chains remain fully generated for all parameter counts (0-param methods do not generate When chains at all).
- **Method builder elimination:** Method builders remain generated for all parameter counts (due to `LastArg`/`LastArgs` tracking).
- **Library interface changes:** No changes to `IPropertyGetBuilder<T>`, `IMethodCallSequence<T>`, etc.

### Verification Gates

1. **After Phase 1:** All property interceptor tests pass. Generated code for base-class-mode properties contains NO `PropertyGetBuilderImpl`, `PropertyGetSequenceImpl`, `PropertySetBuilderImpl`, `PropertySetSequenceImpl` classes. Generated code has no `new Get()` / `new Set()` methods. Init-only and ref-return property tests still pass (unaffected).
2. **After Phase 2:** All indexer interceptor tests pass. Generated code for base-class-mode indexers contains NO inner class thin subclasses (builder, sequence, or When chain). Inline mode indexer tests still pass.
3. **After Phase 3:** All method interceptor tests pass. Generated code for base-class-mode methods contains NO `MethodSequenceImpl` or `ReturnMethodSequenceImpl`. Builder and When chain inner classes still generated. Inline mode method tests still pass.
4. **Final:** `dotnet build src/KnockOff.sln` succeeds. All tests pass across all target frameworks. Design projects compile.

### Stop Conditions

If any of these occur, STOP and report:
- Out-of-scope test fails (init-only, ref-return, inline mode, or any test not directly related to base-class-mode inner classes)
- The `Verifiable(Called times)` EII fix causes unexpected failures (report which tests and what they expect)
- Architectural contradiction: a base class inner class cannot implement the library interface due to a type parameter mismatch not identified in the plan
- Generated code that uses the eliminated inner classes in ways not covered by the plan (e.g., cross-references between builder/sequence types)
- Any existing test needs its assertions modified (not just setup changes to accommodate new types)

---

## Implementation Progress

**Started:** 2026-02-13

### Phase 1: Property Inner Classes -- COMPLETE

**Files modified:**
- `src/KnockOff/Interceptors/PropertyGetInterceptorBase.cs` -- `PropertyGetBuilderBase` now implements `IPropertyGetBuilder<TValue>`, `IPropertyGetTracking`; `PropertyGetSequenceBase` now implements `IPropertyGetSequence<TValue>`; `Get()` return type changed to `IPropertyGetBuilder<TValue>`
- `src/KnockOff/Interceptors/PropertySetInterceptorBase.cs` -- `PropertySetBuilderBase` now implements `IPropertySetBuilder<TValue>`, `IPropertySetTracking<TValue>`; `PropertySetSequenceBase` now implements `IPropertySetSequence<TValue>`; `Set()` return type changed to `IPropertySetBuilder<TValue>`
- `src/KnockOff/Interceptors/PropertyGetSetInterceptorBase.cs` -- Same changes for its own `PropertySetBuilderBase` and `PropertySetSequenceBase`; `Set()` return type changed to `IPropertySetBuilder<TValue>`
- `src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs` -- Removed entire `#region Base Class Mode: Thin Inner Classes` (4 methods: `RenderBaseClassPropertyGetBuilderImpl`, `RenderBaseClassPropertyGetSequenceImpl`, `RenderBaseClassPropertySetBuilderImpl`, `RenderBaseClassPropertySetSequenceImpl`); Removed generated `new Get()` / `new Set()` methods from `RenderBaseClassGetOnlyContent`, `RenderBaseClassSetOnlyContent`, `RenderBaseClassGetSetContent`; Fixed `Verifiable(Called times)` EII bug in inline-mode renderers

**Additional changes:**
- Renamed `Verify(Called times)` parameter to `Verify(Called called)` in all three base class builder classes to match interface declaration (`IPropertyGetTracking.Verify(Called called)`, `IPropertySetTracking<TValue>.Verify(Called called)`)
- Added `#pragma warning disable CA1062` for `ThenGet(params TValue[] values)` methods in both `PropertyGetBuilderBase` and `PropertyGetSequenceBase`
- Fixed `Verifiable(Called times)` EII bug in **inline mode** renderers (`RenderPropertyGetBuilderImpl` and `RenderPropertySetBuilderImpl`) -- the generated EII for `IPropertyGetTracking.Verifiable(Called times)` and `IPropertySetTracking<TValue>.Verifiable(Called times)` now correctly sets `_getVerifiableTimes = times` / `_setVerifiableTimes = times` instead of discarding the parameter

**Verification results:**
- Build: 0 warnings, 0 errors across all target frameworks (net8.0, net9.0, net10.0)
- No `PropertyGetBuilderImpl`, `PropertyGetSequenceImpl`, `PropertySetBuilderImpl`, `PropertySetSequenceImpl` classes in any `.Base.g.cs` files
- No `new Get()` or `new Set()` methods in any `.Base.g.cs` files
- Inline-mode files (ref-return, init-only) still contain their own `PropertyGetBuilderImpl` etc. as expected

---

## Phase 1 Completion Evidence

**Tests Passing:**
| Project | Framework | Passed | Skipped | Failed |
|---------|-----------|--------|---------|--------|
| KnockOffTests | net8.0 | 1492 | 4 | 0 |
| KnockOffTests | net9.0 | 1493 | 4 | 0 |
| KnockOffTests | net10.0 | 1493 | 4 | 0 |
| KnockOff.Documentation.Samples | net8.0 | 691 | 0 | 0 |
| KnockOff.Documentation.Samples | net9.0 | 691 | 0 | 0 |
| KnockOff.Documentation.Samples | net10.0 | 691 | 0 | 0 |
| KnockOff.NeatooInterfaceTests | net8.0 | 473 | 0 | 0 |
| KnockOff.NeatooInterfaceTests | net9.0 | 473 | 0 | 0 |
| KnockOff.NeatooInterfaceTests | net10.0 | 473 | 0 | 0 |
| KnockOffTests.AssemblyStrict | net8.0 | 14 | 0 | 0 |
| KnockOffTests.AssemblyStrict | net9.0 | 14 | 0 | 0 |
| KnockOffTests.AssemblyStrict | net10.0 | 14 | 0 | 0 |

**4 skipped tests** are pre-existing `BugRegressionTests.*_Verifiable_CalledConstraint_IsApplied` tests that document the `Verifiable(Called times)` bug. These tests are marked `[Fact(Skip = ...)]` and were skipped before this change. The bug fix in this phase is for the base class and inline-mode renderer code; these regression tests may need to be un-skipped in a follow-up.

**Build:** `dotnet build src/KnockOff.sln` succeeds with 0 warnings, 0 errors.

**No existing test assertions were modified.**

### Phase 2: Indexer Inner Classes -- COMPLETE

**Files modified:**
- `src/KnockOff/Interceptors/IndexerGetSetInterceptorBase.cs` -- `IndexerGetBuilderBase` now implements `IIndexerGetBuilder<TKey, TValue>`, `IIndexerGetTracking<TKey>`; `IndexerGetSequenceBase` now implements `IIndexerGetSequence<TKey, TValue>`; `IndexerSetBuilderBase` now implements `IIndexerSetBuilder<TKey, TValue>`, `IIndexerSetTracking<TKey, TValue>`; `IndexerSetSequenceBase` now implements `IIndexerSetSequence<TKey, TValue>`; When chain methods renamed from `*Base()` to final typed names (`Returns`, `Get`, `Set`, `ThenWhen`, `Verifiable`)
- `src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs` -- Removed all 6 thin inner class generation methods (`RenderBaseClassIndexerGetBuilderImpl`, `RenderBaseClassIndexerGetSequenceImpl`, `RenderBaseClassIndexerSetBuilderImpl`, `RenderBaseClassIndexerSetSequenceImpl`, `RenderBaseClassIndexerWhenBuilder`, `RenderBaseClassIndexerGetWhenChain`, `RenderBaseClassIndexerSetWhenChain`); Updated `Get()` to use `IndexerGetBuilderBase` instead of `IndexerGetBuilderImpl`; Updated `Set()` to use `IndexerSetBuilderBase` instead of `IndexerSetBuilderImpl`; Updated `When()` to return `IndexerWhenBuilderBase` instead of `IndexerWhenBuilder`
- `src/Prototype/Prototype.Stubs/Refactored/MatrixStandaloneStub.cs` -- Updated prototype to use base class inner classes directly instead of thin subclasses; removed all 7 generated inner classes

**Key changes in base class:**
- Added `Verifiable(Called called)` overload to both `IndexerGetBuilderBase` and `IndexerSetBuilderBase` (was missing -- needed to implement `IIndexerGetTracking<TKey>.Verifiable(Called)` and `IIndexerSetTracking<TKey, TValue>.Verifiable(Called)`)
- Renamed `Verify(Called times)` parameter to `Verify(Called called)` in builder classes to match interface declarations
- Builder `ThenGetBase`/`ThenSetBase` renamed to `ThenGet`/`ThenSet` returning interface types
- Sequence `ThenGetBase`/`ThenSetBase` renamed to `ThenGet`/`ThenSet` returning interface types
- Sequence `VerifiableBase()` renamed to `Verifiable()` returning typed sequence interface
- When chain `ReturnsBase`/`GetBase`/`SetBase` renamed to `Returns`/`Get`/`Set` returning concrete base types
- When chain `ThenWhenBase` renamed to `ThenWhen` returning `IndexerWhenBuilderBase`
- When chain `VerifiableBase()` renamed to `Verifiable()` returning `this`

**Verification results:**
- Build: 0 warnings, 0 errors across all target frameworks (net8.0, net9.0, net10.0)
- No `IndexerGetBuilderImpl`, `IndexerGetSequenceImpl`, `IndexerSetBuilderImpl`, `IndexerSetSequenceImpl`, `IndexerWhenBuilder` (non-base), `IndexerGetWhenChain` (non-base), or `IndexerSetWhenChain` (non-base) classes in any `.Base.g.cs` files
- Inline mode indexer tests still pass (inline mode generates its own builder/sequence classes independently)

---

## Phase 2 Completion Evidence

**Tests Passing:**
| Project | Framework | Passed | Skipped | Failed |
|---------|-----------|--------|---------|--------|
| KnockOffTests | net8.0 | 1492 | 4 | 0 |
| KnockOffTests | net9.0 | 1493 | 4 | 0 |
| KnockOffTests | net10.0 | 1493 | 4 | 0 |
| KnockOff.Documentation.Samples | net8.0 | 691 | 0 | 0 |
| KnockOff.Documentation.Samples | net9.0 | 691 | 0 | 0 |
| KnockOff.Documentation.Samples | net10.0 | 691 | 0 | 0 |
| KnockOff.NeatooInterfaceTests | net8.0 | 473 | 0 | 0 |
| KnockOff.NeatooInterfaceTests | net9.0 | 473 | 0 | 0 |
| KnockOff.NeatooInterfaceTests | net10.0 | 473 | 0 | 0 |
| KnockOffTests.AssemblyStrict | net8.0 | 14 | 0 | 0 |
| KnockOffTests.AssemblyStrict | net9.0 | 14 | 0 | 0 |
| KnockOffTests.AssemblyStrict | net10.0 | 14 | 0 | 0 |

**4 skipped tests** are the same pre-existing `BugRegressionTests.*_Verifiable_CalledConstraint_IsApplied` tests from Phase 1. No new skips.

**Build:** `dotnet build src/KnockOff.sln` succeeds with 0 warnings, 0 errors.

**No existing test assertions were modified.**

### Phase 3: Method Sequence Inner Classes -- COMPLETE

**Files modified:**
- `src/KnockOff/Interceptors/VoidMethodInterceptorBase.cs` -- `MethodSequenceBase` now implements `IMethodCallSequence<TDelegate>`, `IMethodCallSequence`, `IMethodSequence`; renamed `ThenCallBase` to `ThenCall` (public, returns `MethodSequenceBase`); renamed `VerifiableBase` to `Verifiable` (public, returns `MethodSequenceBase`); added EII for `IMethodCallSequence<TDelegate>.ThenCall`, `IMethodCallSequence<TDelegate>.Verifiable`, `IMethodSequence.Verifiable`; added builder factory constructor and `_builderFactory` field for typed builder creation in sequences; updated `MethodCallBuilderBase.ThenCallBase` to pass `CreateNextBuilder` delegate to `MethodSequenceBase` constructor
- `src/KnockOff/Interceptors/MethodInterceptorBase.cs` -- `ReturnMethodSequenceBase` now implements `IMethodReturnSequence<TDelegate>`, `IMethodReturnSequence`, `IMethodSequence` (no longer extends `MethodSequenceBase` -- broken inheritance to avoid `IMethodCallSequence` contamination); added `ThenReturn(TDelegate)`, `ThenReturn(TReturn value)`, `ThenReturn(params TReturn[] values)`, `Verifiable()`, `Verify()`, `Reset()`, `ThenDefault()` methods; added EII for `IMethodReturnSequence<TDelegate>.ThenReturn`, `IMethodReturnSequence<TDelegate>.Verifiable`, `IMethodSequence.Verifiable`; added builder factory constructor and `_returnBuilderFactory` field; updated `ReturnMethodCallBuilderBase.ThenReturnBase` to pass `CreateNextReturnBuilder` delegate to `ReturnMethodSequenceBase` constructor
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` -- Removed call to `RenderBaseClassMethodSequenceImpl` (sequence inner class no longer generated for base class mode); updated builder's `ThenCall`/`ThenReturn` to return `MethodSequenceBase`/`ReturnMethodSequenceBase` from `ThenCallBase`/`ThenReturnBase` instead of creating `MethodSequenceImpl`; updated builder's `ThenReturn(value)` and `ThenReturn(params values[])` to use `ReturnMethodSequenceBase` return type; updated `Return(first, params rest)` entry point to return `ReturnMethodSequenceBase` instead of `MethodSequenceImpl`; removed `CreateValueDelegate` helper from builder (no longer needed); `RenderBaseClassMethodSequenceImpl` method body kept in source (not called, available for reference)

**Key architectural decision: Breaking `ReturnMethodSequenceBase` inheritance from `MethodSequenceBase`**

The plan stated `ReturnMethodSequenceBase` extends `MethodSequenceBase`. However, when `MethodSequenceBase` implements `IMethodCallSequence<TDelegate>`, `ReturnMethodSequenceBase` would also inherit this interface, making non-void method sequences incorrectly implement the void sequence interface. This was resolved by breaking the inheritance -- `ReturnMethodSequenceBase` is now a standalone class that directly implements `IMethodReturnSequence<TDelegate>` and duplicates the shared sequence methods (`Verify()`, `Reset()`, `ThenDefault()`). This duplication is minimal (~20 lines) and avoids interface contamination.

**Builder factory pattern for typed builder creation:**

The plan did not anticipate that `MethodSequenceBase.ThenCall` / `ReturnMethodSequenceBase.ThenReturn` would need to create `MethodCallBuilderImpl` (the generated typed builder with `LastArg`/`LastArgs`) instead of plain `MethodCallBuilderBase`. This is because `RecordArgs` on the generated interceptor casts the tracking to `MethodCallBuilderImpl`. The solution: both `MethodSequenceBase` and `ReturnMethodSequenceBase` accept an optional `Func<MethodCallBuilderBase>` / `Func<ReturnMethodCallBuilderBase>` factory via constructor. The builder's `ThenCallBase` / `ThenReturnBase` passes its `CreateNextBuilder` / `CreateNextReturnBuilder` delegate (which the generated builder overrides to create `MethodCallBuilderImpl`). This ensures sequence entries always use the correct typed builder.

**Concrete return types on sequence methods:**

All public `ThenCall`/`ThenReturn`/`Verifiable` methods on `MethodSequenceBase` and `ReturnMethodSequenceBase` return the concrete type (not the interface). This preserves access to convenience methods like `ThenReturn(TReturn value)` during chaining, matching the behavior of the old generated `MethodSequenceImpl`. EII is used for the interface methods, which cast to the interface return type.

**Verification results:**
- Build: 0 warnings, 0 errors across all target frameworks (net8.0, net9.0, net10.0)
- No `MethodSequenceImpl` in any `.g.cs` files for base-class-mode interceptors (verified via grep)
- `MethodCallBuilderImpl` (builder), `WhenBuilder`, `WhenChain`, `VoidWhenChain` still generated in base-class-mode interceptors (verified)
- Inline-mode `.Stubs.g.cs` files still contain their own `MethodSequenceImpl` as expected
- Design.Stubs compiles successfully

---

## Phase 3 Completion Evidence

**Tests Passing:**
| Project | Framework | Passed | Skipped | Failed |
|---------|-----------|--------|---------|--------|
| KnockOffTests | net8.0 | 1492 | 4 | 0 |
| KnockOffTests | net9.0 | 1493 | 4 | 0 |
| KnockOffTests | net10.0 | 1493 | 4 | 0 |
| KnockOff.Documentation.Samples | net8.0 | 691 | 0 | 0 |
| KnockOff.Documentation.Samples | net9.0 | 691 | 0 | 0 |
| KnockOff.Documentation.Samples | net10.0 | 691 | 0 | 0 |
| KnockOff.NeatooInterfaceTests | net8.0 | 473 | 0 | 0 |
| KnockOff.NeatooInterfaceTests | net9.0 | 473 | 0 | 0 |
| KnockOff.NeatooInterfaceTests | net10.0 | 473 | 0 | 0 |
| KnockOffTests.AssemblyStrict | net8.0 | 14 | 0 | 0 |
| KnockOffTests.AssemblyStrict | net9.0 | 14 | 0 | 0 |
| KnockOffTests.AssemblyStrict | net10.0 | 14 | 0 | 0 |

**4 skipped tests** are the same pre-existing `BugRegressionTests.*_Verifiable_CalledConstraint_IsApplied` tests from Phase 1 and Phase 2. No new skips.

**Build:** `dotnet build src/KnockOff.sln` succeeds with 0 warnings, 0 errors.

**Design Projects:** `dotnet build src/Design/Design.Stubs` succeeds with 0 warnings, 0 errors.

**No existing test assertions were modified.**

**All Contract Items:** Confirmed complete. All Phase 1, Phase 2, and Phase 3 items are checked.
