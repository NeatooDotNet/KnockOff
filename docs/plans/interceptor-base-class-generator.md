# Interceptor Base Class Generator Changes

**Status:** Ready for Implementation
**Created:** 2026-02-13
**Last Updated:** 2026-02-13 (developer approved, contract created)
**Related Todo:** [Reduce Generated Code Size](../todos/reduce-generated-code-size.md)

---

## Problem Statement

The prototype at `src/Prototype/` has been verified: generic interceptor base classes reduce per-compilation generated code by ~91%. Now the actual KnockOff generator must be modified to emit this pattern instead of the current fully-inlined pattern, and the base classes must be added to the KnockOff NuGet package.

---

## Overview

This plan covers three categories of work:

1. **KnockOff Library** -- Port base classes from `Prototype.Library` into `src/KnockOff/`
2. **Generator Renderers** -- Modify the three shared renderers (`MethodInterceptorRenderer.cs`, `PropertyInterceptorRenderer.cs`, `IndexerInterceptorRenderer.cs`) to emit thin subclasses instead of fully-inlined interceptors
3. **Verification** -- All 9 patterns must continue to work, all existing tests must pass

---

## Scope

### Patterns Affected

All 9 patterns use the same shared renderers, so all are affected:

| # | Pattern | Renderer | Builder |
|---|---------|----------|---------|
| 1 | Standalone | `FlatRenderer` -> `MethodInterceptorRenderer` | `FlatModelBuilder` |
| 2 | Generic Standalone | `FlatRenderer` -> `MethodInterceptorRenderer` | `FlatModelBuilder` |
| 3 | Standalone Class | `StandaloneClassRenderer` -> `MethodInterceptorRenderer` | `StandaloneClassModelBuilder` |
| 4 | Generic Standalone Class | `StandaloneClassRenderer` -> `MethodInterceptorRenderer` | `StandaloneClassModelBuilder` |
| 5 | Inline Interface | `InlineRenderer` -> `MethodInterceptorRenderer` | `InlineModelBuilder` |
| 6 | Inline Class | `ClassRenderer` -> `MethodInterceptorRenderer` | `InlineModelBuilder` |
| 7 | Inline Delegate | `InlineRenderer` -> `MethodInterceptorRenderer` | (delegate adapter) |
| 8 | Open Generic Interface | `InlineRenderer` -> `MethodInterceptorRenderer` | `InlineModelBuilder` |
| 9 | Open Generic Class | `ClassRenderer` -> `MethodInterceptorRenderer` | `InlineModelBuilder` |

### Member Types Affected

All 4 member types:
- **Methods** -- `MethodInterceptorRenderer.cs` (3,424 lines)
- **Properties** -- `PropertyInterceptorRenderer.cs` (1,325 lines)
- **Indexers** -- `IndexerInterceptorRenderer.cs` (2,004 lines)
- **Events** -- Not affected (events use a simpler pattern that doesn't have the same duplication)

---

## Phase 1: Port Base Classes to KnockOff Library

### New Files in `src/KnockOff/`

Port from `src/Prototype/Prototype.Library/Interceptors/` to `src/KnockOff/Interceptors/`:

| Prototype File | KnockOff File | Lines | Description |
|---|---|---|---|
| `VoidMethodInterceptorBase.cs` | `Interceptors/VoidMethodInterceptorBase.cs` | ~498 | Void method base class (fields, RunVoidPriorityChain, void When chain, MethodCallBuilderBase, MethodSequenceBase, FindLastArgInTracking) |
| `MethodInterceptorBase.cs` | `Interceptors/MethodInterceptorBase.cs` | ~413 | Non-void method base class (extends void base, RunPriorityChain, non-void When chain, ReturnMethodCallBuilderBase, ReturnMethodSequenceBase, FindLastArgInTracking override) |
| `PropertyGetInterceptorBase.cs` | `Interceptors/PropertyGetInterceptorBase.cs` | ~282 | Get-only property base class (InvokeGet, PropertyGetBuilderBase, PropertyGetSequenceBase) |
| (new) | `Interceptors/PropertySetInterceptorBase.cs` | ~200 est. | Set-only property base class (InvokeSet, PropertySetBuilderBase, PropertySetSequenceBase) -- not in prototype, mirrors get-only pattern |
| (new) | `Interceptors/PropertyGetSetInterceptorBase.cs` | ~350 est. | Get+set property base class extending get-only, adds set-side fields + `_valueSet`/`_value` round-trip storage -- not in prototype |
| `IndexerGetSetInterceptorBase.cs` | `Interceptors/IndexerGetSetInterceptorBase.cs` | ~951 | Indexer base class (InvokeGet/InvokeSet, PerKeyBuilder, dual When chains, all builder/sequence bases) |
| `Unit.cs` | `Unit.cs` | ~9 | Zero-size sentinel for 0-param methods |

### Porting Rules

1. **Namespace**: Change from `Prototype.Library.Interceptors` to `KnockOff.Interceptors`
2. **Fully-qualify KnockOff types**: The prototype uses `using KnockOff;` but the library classes ARE in the KnockOff assembly. References to `Called`, `VerificationFailure`, `StubException`, etc. should use appropriate namespace resolution.
3. **Accessibility**: Base classes must be `public` (they are used by generated code in user projects)
4. **No breaking changes**: Existing KnockOff library interfaces (`IMethodCallBuilder<T>`, `IPropertyGetBuilder<T>`, etc.) remain unchanged. Generated code that doesn't use base classes should still work.
5. **Add `SetupReturnCallback` / `SetupReturnValue` / `SetupCallback`**: These helpers were identified in the post-prototype analysis and should be included from the start.

### What NOT to Port

- `SimpleIntMethodInterceptor<TDelegate, TReturn>` -- This was a DataReader-specific optimization in the prototype. The generator should not emit this pattern; it was a hand-optimization that demonstrated reuse but is not generalizable.

**Concern 2 Resolution (FindLastArgInTracking):** The original plan incorrectly listed `FindLastArgInTracking` as "do NOT port." This contradicts the prototype, which includes the method on both `VoidMethodInterceptorBase` (line 229) and `MethodInterceptorBase` (line 192, using `new` to shadow the void version with a version that also checks `_returnValueTracking`). The refactored stubs actively use it -- for example, `BasicUserMethodStub.cs` line 52: `FindLastArgInTracking<MethodCallBuilderImpl, string>(b => b.LastArg)`.

The original reasoning ("nullable type widening makes a generic base class method impractical") was wrong. The prototype solves this cleanly: `FindLastArgInTracking<TBuilder, TResult>(Func<TBuilder, TResult> selector)` is generic over the builder type and the result type. The caller provides a lambda that extracts the typed property (e.g., `b => b.LastArg`). This works because the search logic (iterate tracking objects, find last one with calls, cast to TBuilder, extract value) is structurally identical for both `LastArg` (single param) and `LastArgs` (multi-param). The nullable type widening is handled by the caller, not the base class.

**Resolution:** Port `FindLastArgInTracking` as it exists in the prototype. The generated `LastArg` / `LastArgs` property getters will use `FindLastArgInTracking` to search configured tracking sources (callback, return value, sequence), then fall back to `_unconfiguredLastArg` / `_unconfiguredLastArgs`. This eliminates the multi-line inline search logic from generated code. The generated code pattern becomes:

```csharp
public string? LastArg
{
    get
    {
        var found = FindLastArgInTracking<MethodCallBuilderImpl, string>(b => b.LastArg);
        return found ?? (_unconfiguredCallCount > 0 ? _unconfiguredLastArg : default);
    }
}
```

Note: `FindLastArgInTracking` does NOT search simplified callback tracking (`_callSimplifiedTracking` / `_callSimplifiedVoidTracking`) because those fields are not in the base class. Methods with simplified callbacks remain in inline mode anyway, so this is not a gap.

### Interaction with Existing KnockOff Library Interfaces

**Critical design point from prototype Concern #2:** The base class builder (`MethodCallBuilderBase`, `ReturnMethodCallBuilderBase`) does NOT implement KnockOff library interfaces (`IMethodCallBuilder<T>`, `IMethodReturnBuilder<T,TArg>`, etc.). Generated thin subclasses add explicit interface implementations. This means:

- `MethodCallBuilderBase` has `RecordCallBase()`, `_callCount`, `VerifiableBase()`, `ThenCallBase()` -- all protected/public
- Generated `MethodCallBuilderImpl` inherits from `MethodCallBuilderBase` (for void) or `ReturnMethodCallBuilderBase` (for non-void) and implements the appropriate KnockOff library interface
- The library interface implementation in the generated code adds `RecordCall(args)` which delegates to `RecordCallBase()` plus stores `LastArg`/`LastArgs`

---

## Phase 2: Modify Generator Renderers

### Strategy

The shared renderers (`MethodInterceptorRenderer`, `PropertyInterceptorRenderer`, `IndexerInterceptorRenderer`) are called by all 4 outer renderers (`FlatRenderer`, `InlineRenderer`, `ClassRenderer`, `StandaloneClassRenderer`). By modifying only the shared renderers, all 9 patterns benefit.

### Key Principle: Emission Mode

The plan adds a new concept: **emission mode**. Each interceptor can be emitted in one of two modes:

- **Inline mode** (current): Everything generated inline, no base class dependency. Used as a fallback for edge cases the base class cannot handle (e.g., ref return, ref/out parameters, method overloads).
- **Base class mode** (new): Interceptor inherits from a library base class. Only thin overrides and the generated Invoke method are emitted.

The emission mode is determined per-interceptor by the renderer based on the model:

| Feature | Base Class Mode? | Reason |
|---|---|---|
| Regular non-void method | Yes | `MethodInterceptorBase<TDelegate, TArgs, TReturn>` |
| Regular void method | Yes | `VoidMethodInterceptorBase<TDelegate, TArgs>` |
| Get-only property | Yes | `PropertyGetInterceptorBase<TValue>` |
| Get+set property | Yes | `PropertyGetSetInterceptorBase<TValue>` (new -- see Concern 4 resolution) |
| Set-only property | Yes | `PropertySetInterceptorBase<TValue>` (new -- see Concern 3 resolution) |
| Indexer (get+set) | Yes | `IndexerGetSetInterceptorBase<TKey, TValue>` |
| Ref return method/property | No (inline) | Ref return requires `_refReturnBacking` pattern not in base |
| Ref/out parameters | No (inline) | Ref/out params cannot be packed into `TArgs` tuple |
| Method overload groups | No (inline) | Per-signature storage uses suffixed fields not compatible with base |
| Task\<T\>/ValueTask\<T\> methods | No (inline) | `_callSimplified` fields not in base class (simplified callback overloads) |
| Task/ValueTask void methods | No (inline) | `_callSimplifiedVoid` fields not in base class (simplified void callback overloads) |
| Init-only properties | No (inline) | Special init-only pattern with `_valueSet`, `RecordSet` |

**Concern 1 Resolution (Void-Async Task/ValueTask):** The original emission mode table listed "Async simplified callbacks" as a single row without distinguishing between `Task<T>`/`ValueTask<T>` (which generate `_callSimplified` + `_callSimplifiedTracking` fields) and plain `Task`/`ValueTask` (which generate `_callSimplifiedVoid` + `_callSimplifiedVoidTracking` fields). The renderer treats these as two independent code paths: `GetAsyncTypeInfo()` checks for `Task<T>`/`ValueTask<T>` and `GetVoidAsyncInfo()` (at line 3173) checks for plain `Task`/`ValueTask`. Both paths generate extra fields and extra Invoke branches not present in the base class.

**Resolution:** Any method that triggers either `_callSimplified` or `_callSimplifiedVoid` fields remains in inline mode. The emission mode determination should check: `isAsyncWithInnerType || isVoidAsync` (where `isVoidAsync` = return type is `Task` or `ValueTask` without generic argument). The table above now lists these as two separate rows for clarity. The developer's suggested approach of "any method with simplified callback fields stays inline" is correct and is adopted.

**Approximately 70-80% of interceptors in real code will use base class mode.** The inline mode serves as the safety net for edge cases. (Estimate adjusted downward from 80-90% to account for the explicit exclusion of void-async methods.)

### 2A: MethodInterceptorRenderer.cs Changes

**File:** `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` (3,424 lines)

#### What Gets Removed (Moved to Base Class)

These sections are no longer emitted for base class mode interceptors:

1. **Fields** (lines ~73-157): `_call`, `_callTracking`, `_returnValue`, `_hasReturnValue`, `_returnValueTracking`, `_sequence`, `_sequenceIndex`, `_repeatLastValue`, `_whenChain`, `_whenChainHead`, `_whenVerifiable`, `_isVerifiable`, `_verifiableTimes`, `_unconfiguredCallCount`
2. **TotalCallCount property** (computed from tracking): Now in base class
3. **Verification methods**: `Verify()`, `Verify(Called)`, `CheckVerification()`, `CheckVerificationAll()`, `IsConfigured`, `IsVerifiable` -- all in base class
4. **Reset method**: Base class handles resetting base fields; generated code only resets `_source` and `_unconfiguredLastArg`/`_unconfiguredLastArgs`
5. **Invoke priority chain core**: The When chain check, sequence check, return value check, callback check, sequence exhausted repeat -- all in `RunPriorityChain` / `RunVoidPriorityChain` / `HandleNonVoidSequenceExhaustedRepeat` / `HandleSequenceExhaustedRepeat` in the base class

#### What Gets Added (New Patterns)

1. **Class declaration**: Changes from `public sealed class FooInterceptor` to `public sealed class FooInterceptor : MethodInterceptorBase<FooDelegate, TArgs, TReturn>` (or `VoidMethodInterceptorBase<Action<TArgs>, TArgs>` for void)
2. **Constructor**: `public FooInterceptor() : base("Foo") { }`
3. **Abstract override `InvokeDelegate`**: `protected override TReturn InvokeDelegate(FooDelegate del, TArgs args) => del(args.a, args.b);`
4. **Abstract override `RecordArgs`**: `protected override void RecordArgs(TArgs args, MethodCallBuilderBase tracking) { if (tracking is MethodCallBuilderImpl impl) impl.RecordArg(args); }`
5. **Abstract override `RecordUnconfiguredArgs`**: `protected override void RecordUnconfiguredArgs(TArgs args) => _unconfiguredLastArg = args;`
6. **Abstract override `CreateValueDelegate`**: `protected override FooDelegate CreateValueDelegate(TReturn value) => (_, _) => value;` (non-void only)
7. **`SetupReturnCallback` / `SetupReturnValue` usage**: `Return(callback)` method calls `SetupReturnCallback(callback, builder)` instead of manually resetting 7 fields
8. **Thin Invoke**: Only emits the unconfigured tail (source delegation, stub override, strict check, default return). The priority chain is a single call to `RunPriorityChain(args)` or `RunVoidPriorityChain(args)`.

#### What Stays the Same

1. **Source field** (`internal IFoo? _source;`)
2. **Custom delegate** (if `NeedsCustomDelegate`)
3. **`_unconfiguredLastArg` / `_unconfiguredLastArgs` fields and `LastArg` / `LastArgs` properties** -- These stay generated but use `FindLastArgInTracking` from the base class to search configured tracking sources (see Concern 2 resolution)
4. **Return(callback) / Return(value)** -- Stay generated but use `SetupReturnCallback` / `SetupReturnValue` helpers
5. **Return(first, params rest)** -- Stays generated
6. **When() entry points** -- These bridge individual params to TArgs tuple predicates
7. **MethodCallBuilderImpl** inner class -- Stays generated (thin subclass of `ReturnMethodCallBuilderBase` or `MethodCallBuilderBase`)
8. **MethodSequenceImpl** inner class -- Stays generated (thin subclass of `ReturnMethodSequenceBase` or `MethodSequenceBase`)
9. **WhenBuilder / WhenChain** inner classes -- Stay generated (thin subclasses of `WhenBuilderBase` / `WhenChainBase`)
10. **VoidWhenChain** inner class -- Stays generated (thin subclass of `VoidWhenChainBase`)

#### Estimated Savings Per Interceptor

| Section | Current Lines | Base Class Lines | Savings |
|---|---|---|---|
| Fields | ~25 | 0 | ~25 |
| TotalCallCount | ~8 | 0 | ~8 |
| Verification (Verify, CheckVerification, etc.) | ~30 | 0 | ~30 |
| Reset body | ~12 | ~3 (source + unconfigured) | ~9 |
| Invoke priority chain | ~80 | ~5 (RunPriorityChain + tail) | ~75 |
| Return/Call body | ~16 | ~6 (SetupReturn helpers) | ~10 |
| **Subtotal removed** | **~171** | **~14 new** | **~157** |
| MethodCallBuilderImpl | ~35 | ~20 (thin subclass) | ~15 |
| MethodSequenceImpl | ~25 | ~15 (thin subclass) | ~10 |
| WhenBuilder | ~10 | ~8 (thin subclass) | ~2 |
| WhenChain | ~25 | ~20 (thin subclass) | ~5 |
| **Total per interceptor** | **~430** | **~100** | **~330** |

This matches the prototype's observed ~77% per-interceptor reduction for full-featured interceptors. Simpler interceptors (no When chain, no arg tracking) see higher ratios.

### 2B: PropertyInterceptorRenderer.cs Changes

**File:** `src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs` (1,325 lines)

#### Get-Only Properties (Base Class Mode)

**Class declaration**: Changes to `public sealed class FooInterceptor : PropertyGetInterceptorBase<TValue>`

**Removed**: All fields, InvokeGet body (priority chain), TotalGetCount, verification methods, Reset body, PropertyGetBuilderImpl, PropertyGetSequenceImpl

**Added**: Constructor (`base("Foo")`), abstract override `InvokeGetUnconfigured(bool strict)` (source delegation + strict check + default)

**Stays**: Source field, Get() / Get(value) methods, Reset override (clears source), stub override helpers (RecordGet, HasGet, InvokeGetCallback -- now in base class)

#### Get+Set Properties (Base Class Mode)

**Concern 4 Resolution:** The prototype does not include a `PropertyGetSetInterceptorBase`. The developer raised a valid concern about the `_valueSet`/`_value` round-trip storage pattern that get+set properties currently generate.

**Background:** For regular get+set properties, the current renderer emits `_valueSet` (bool) and `_value` (TValue) backing fields at `PropertyInterceptorRenderer.cs` lines 165-170. These enable "round-trip storage": when a value is set via the interface setter (unconfigured path), it can be read back via the getter (also unconfigured path). The `InvokeSet` method stores the value at lines 576-580 (`_value = value; _valueSet = true;`), and `InvokeGet` returns it as the fallback at line 496 (`return _valueSet ? _value : default!;`).

**Recommendation: Option A (extend get-only with set support), including `_valueSet`/`_value` round-trip fields.**

Create `PropertyGetSetInterceptorBase<TValue>` extending `PropertyGetInterceptorBase<TValue>`. This class adds:

1. **Set-side fields** (mirroring the get-side pattern):
   - `_set` (Action\<TValue\>?), `_setTracking`, `_setSequence`, `_setSequenceIndex`, `_setRepeatLastValue`
   - `_isSetVerifiable`, `_setVerifiableTimes`, `_unconfiguredSetCount`, `_unconfiguredLastSetValue`
2. **Round-trip storage fields**: `_valueSet` (bool), `_value` (TValue) -- these are specific to get+set properties and DO NOT exist in get-only or set-only base classes
3. **`InvokeSet` with priority chain**: Sequence -> Callback -> Unconfigured (track count, store in `_value`/`_valueSet` if has getter) -> Sequence exhausted repeat -> Source delegation -> Strict check -> Round-trip store
4. **`InvokeGetUnconfigured` override** in this class adds the round-trip check: `if (_valueSet) return _value;` before delegating to the abstract `InvokeGetUnconfiguredFinal(bool strict)` (which the generated subclass implements for source delegation/strict/default)
5. **TotalSetCount**, **set verification** (VerifySet, CheckVerificationSet), **Reset** (set-side + `_valueSet = false`)
6. **`PropertySetBuilderBase`**, **`PropertySetSequenceBase`** inner classes (mirroring the get-side base classes)
7. **Stub override helpers** for set: `HasSet`, `InvokeSetCallback(TValue value)`, `RecordSet(TValue value)`

**Note on `InvokeGetUnconfigured` layering:** The get-only base class has `InvokeGetUnconfigured(bool strict)` as an abstract method. For get+set properties, the get+set base class overrides this to insert the round-trip check (`_valueSet ? _value : InvokeGetUnconfiguredFinal(strict)`). The generated subclass then overrides `InvokeGetUnconfiguredFinal` instead of `InvokeGetUnconfigured`. The developer may choose a different layering approach (e.g., a template method pattern or a protected virtual method on the get-only base), but the key requirement is that the round-trip check happens before source delegation.

**This class is unvalidated (not in the prototype).** The developer should create it carefully by mirroring the get-side and set-side patterns from the prototype and current renderer, then verify thoroughly with tests. The property set priority chain is simpler than the method priority chain (no When chain, no args tracking beyond unconfigured), so the risk is manageable. If difficulties arise, the fallback is to leave get+set properties in inline mode and revisit in a follow-up.

#### Set-Only Properties (Base Class Mode)

**Concern 3 Resolution:** The original plan discussed get-only and get+set properties but omitted set-only properties (`model.HasSetter && !model.HasGetter`). The property renderer does handle set-only properties -- they get setter fields (`_set`, `_setTracking`, `_setSequence`, etc.), `InvokeSet`, `TotalSetCount`, set verification, and `PropertySetBuilderImpl` / `PropertySetSequenceImpl` inner classes, but no getter-related fields or methods.

**Resolution:** Create a `PropertySetInterceptorBase<TValue>` to handle set-only properties. This base class holds the set-side fields (callback, tracking, sequence, verification) and provides `InvokeSet` with the priority chain. It follows the same structural pattern as `PropertyGetInterceptorBase<TValue>` but for the set side. The generated thin subclass provides `InvokeSetUnconfigured(bool strict, TValue value)` for source delegation and strict mode, plus typed inner classes.

This new base class is added to Phase 5.1 (Library) step 7. It is structurally simple -- it mirrors the get-only base class. `PropertyGetSetInterceptorBase<TValue>` (see Concern 4) should extend `PropertyGetInterceptorBase<TValue>` and compose with the set-side logic from `PropertySetInterceptorBase<TValue>` (either through inheritance or duplication). The simplest approach is to have `PropertyGetSetInterceptorBase<TValue>` extend `PropertyGetInterceptorBase<TValue>` and duplicate the set-side fields directly (since C# does not support multiple inheritance).

#### Init-Only Properties (Inline Mode)

Init-only properties have a unique pattern (`_valueSet`, `RecordSet`) that does not fit the base class model. They remain fully inline. This is acceptable because init-only properties are rare.

#### Ref Return Properties (Inline Mode)

Ref return properties use `_refReturnBacking` and `InvokeRefGet`, which are not in the base class. They remain fully inline.

### 2C: IndexerInterceptorRenderer.cs Changes

**File:** `src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs` (2,004 lines)

#### Single-Key Indexers (Base Class Mode)

**Class declaration**: Changes to `public sealed class IndexerInterceptor : IndexerGetSetInterceptorBase<TKey, TValue>`

**Removed**: All fields (per-key dictionary, get/set callbacks, sequences, When chains, unconfigured tracking), InvokeGet/InvokeSet bodies, TotalGetCount/TotalSetCount, verification methods, Reset body, all inner builder/sequence classes (IndexerGetBuilderBase, IndexerGetSequenceBase, IndexerSetBuilderBase, IndexerSetSequenceBase), all When matcher classes

**Added**: Constructor, abstract overrides `InvokeGetUnconfigured(bool strict, TKey key)` and `InvokeSetUnconfigured(bool strict, TKey key, TValue value)`, indexer accessor wrapping tuple/unpacking

**Stays**: Source field, typed indexer accessor (`this[int row, int col]` -> `GetOrCreatePerKeyBuilder((row, col))`), Get/Set public methods (thin wrappers), When entry points, Reset override (clears source), thin inner class subclasses for typed API

#### Multi-Key Indexers (Inline Mode)

Multi-key indexers (e.g., `this[int]` and `this[string]` on the same interface) use suffixed fields (`_get_Int32`, `_get_String`) which do not map to the single-key base class. They remain fully inline.

This is acceptable because multi-key indexers are uncommon. The `DualKeyIndexerInterceptor` composition pattern from the DataReader prototype was a hand-optimization, not a generalizable pattern.

#### Init-Only Indexers (Inline Mode)

Init-only indexers remain fully inline (rare).

---

## Phase 3: Model and Builder Changes

### Are Model Changes Needed?

**Minimal.** The models carry all the information needed for both inline and base class emission. The renderer can determine the emission mode from existing model properties:

- `model.IsVoid` -- determines VoidMethodInterceptorBase vs MethodInterceptorBase
- `model.Parameters` -- determines TArgs type (single, tuple, Unit)
- `model.ReturnType` -- determines TReturn
- `model.Overloads.Count > 0` -- multi-overload -> inline mode
- Ref/out params -> inline mode
- `GetAsyncTypeInfo(model.ReturnType)` returns Task\<T\>/ValueTask\<T\> -> inline mode
- `GetVoidAsyncInfo(model.ReturnType)` returns Task/ValueTask -> inline mode

### Possible Model Addition

A single boolean flag on the unified models could simplify renderer logic:

```csharp
// On UnifiedMethodInterceptorModel:
bool UseBaseClass { get; }

// On UnifiedPropertyInterceptorModel:
bool UseBaseClass { get; }

// On UnifiedIndexerInterceptorModel:
bool UseBaseClass { get; }
```

This flag would be computed by the builder (or adapter) based on the criteria listed in the emission mode table. The renderer then branches on this flag rather than re-evaluating multiple conditions.

**Whether to add this is a developer decision.** The renderer can also compute it locally. The flag makes the code cleaner but adds a model property.

### Builder Changes

No builder changes are required. The builders produce the same models; only the renderer interprets them differently.

### TArgs Type Computation

The renderer needs to compute the `TArgs` type for the base class type parameter. This is a new computation:

| Param Count | TArgs | Example |
|---|---|---|
| 0 | `global::KnockOff.Unit` | `VoidMethodInterceptorBase<Action, Unit>` |
| 1 | Single param type | `MethodInterceptorBase<ProcessDelegate, string, string>` |
| 2+ | `(T1 name1, T2 name2)` | `MethodInterceptorBase<CalculateDelegate, (int a, int b), int>` |

This can be computed from `model.Parameters` in the renderer.

---

## Phase 4: Test Strategy

### Existing Tests

**All existing tests must pass unchanged.** The API surface does not change -- users interact with the same `Return()`, `Call()`, `ThenReturn()`, `When()`, `Verify()`, etc. methods. Only the internal structure of the generated code changes.

The existing test suite is comprehensive:
- `src/Tests/KnockOffTests/` -- Unit tests for all patterns
- `src/Design/Design.Tests/` -- Design project tests
- `src/Tests/NeatooInterfaceTests/` -- Integration tests with Neatoo interfaces

### New Tests

No new test files are needed. The existing tests verify behavior. The generated code structure changes, but if the behavior is identical, the tests pass.

### Manual Verification

After implementation, the developer should:
1. Compare a few generated `.g.cs` files before and after to verify the structural changes
2. Verify that base class mode interceptors are shorter than inline mode
3. Verify that inline mode fallback still works for edge cases (ref return, ref/out params, overload groups)

---

## Phase 5: Implementation Order

### Phase 5.1: Library (Must Be First)

1. Create `src/KnockOff/Interceptors/` directory
2. Port `Unit.cs` to `src/KnockOff/Unit.cs`
3. Port `VoidMethodInterceptorBase.cs` (includes `MethodCallBuilderBase`, `MethodSequenceBase`, void When chain classes, `FindLastArgInTracking`)
4. Port `MethodInterceptorBase.cs` (includes `ReturnMethodCallBuilderBase`, `ReturnMethodSequenceBase`, non-void When chain classes, `FindLastArgInTracking` override with `new`)
5. Add `SetupReturnCallback` / `SetupReturnValue` / `SetupCallback` helpers
6. Port `PropertyGetInterceptorBase.cs` (includes `PropertyGetBuilderBase`, `PropertyGetSequenceBase`)
7. Create `PropertySetInterceptorBase.cs` (new -- mirrors get-only base for set-only properties)
8. Create `PropertyGetSetInterceptorBase.cs` (extends get-only with set support + `_valueSet`/`_value` round-trip -- not in prototype, must be created. See Concern 4 resolution for requirements.)
9. Port `IndexerGetSetInterceptorBase.cs` (includes all indexer inner classes)
10. Build `src/KnockOff/KnockOff.csproj` -- must compile with no errors

### Phase 5.2: Method Interceptor Renderer

1. Add emission mode determination logic to `MethodInterceptorRenderer.RenderInterceptorClass`
2. Create new method `RenderBaseClassContent` alongside existing `RenderSingleSignatureContent`
3. `RenderBaseClassContent` emits:
   - Class declaration with base class inheritance
   - Constructor
   - Abstract overrides (InvokeDelegate, RecordArgs, RecordUnconfiguredArgs, CreateValueDelegate)
   - Source field
   - Custom delegate
   - Unconfigured last arg/args fields + LastArg/LastArgs property
   - Return/Call entry points (using SetupReturn helpers)
   - When entry points
   - Thin Invoke method (RunPriorityChain + unconfigured tail)
   - Reset override
   - Thin inner classes (MethodCallBuilderImpl, MethodSequenceImpl, WhenBuilder, WhenChain)
4. `RenderSingleSignatureContent` is preserved for inline mode (ref return, ref/out params, async simplified)
5. Run all tests -- must pass

### Phase 5.3: Property Interceptor Renderer

1. Create `PropertySetInterceptorBase<TValue>` and `PropertyGetSetInterceptorBase<TValue>` in library (if not done in 5.1)
2. Add emission mode determination to `PropertyInterceptorRenderer.RenderInterceptorClass`
3. Create `RenderBaseClassGetOnlyContent` for get-only properties
4. Create `RenderBaseClassSetOnlyContent` for set-only properties
5. Create `RenderBaseClassGetSetContent` for get+set properties
6. Preserve `RenderInitOnlyPropertyContent` and `RenderRegularPropertyContent` for inline mode (init-only, ref return)
7. Run all tests -- must pass

### Phase 5.4: Indexer Interceptor Renderer

1. Add emission mode determination to `IndexerInterceptorRenderer.RenderInterceptorClass`
2. Create `RenderBaseClassContent` for single-key indexers
3. Preserve existing renderer for multi-key and init-only indexers
4. Run all tests -- must pass

### Phase 5.5: Final Verification

1. Build entire solution: `dotnet build src/KnockOff.sln`
2. Run all tests: `dotnet test src/KnockOff.sln`
3. Build Design projects: `dotnet build src/Design/Design.Stubs` and `dotnet test src/Design/Design.Tests`
4. Spot-check generated `.g.cs` files for structural correctness

---

## Risk Areas

### Risk 1: Accessibility of Base Class Fields

**Risk:** Base class fields like `_call`, `_sequence`, `_whenChain` are `protected` in the prototype. Generated inner classes (MethodCallBuilderImpl, etc.) need access to them. In the prototype, inner classes are nested inside the interceptor and can access protected members of the outer class.

**Mitigation:** This works because C# allows nested classes to access protected members of their containing class's base. The generated inner classes are nested inside the interceptor class which inherits from the base class. Verified in prototype.

### Risk 2: Method Overload Groups

**Risk:** Method overload groups use per-signature suffixed fields (`_call_Int32`, `_sequence_String_Int32`). These cannot use the single-set-of-fields base class.

**Mitigation:** Overload groups remain in inline mode. The emission mode determination explicitly checks `model.Overloads.Count > 0`.

### Risk 3: Async Simplified Callbacks (Both Generic and Void-Async)

**Risk:** The base class does not include `_callSimplified` / `_callSimplifiedVoid` fields. Methods returning `Task<T>` or `ValueTask<T>` generate simplified callback overloads (`_callSimplified`). Methods returning plain `Task` or `ValueTask` generate void simplified callback overloads (`_callSimplifiedVoid`). Neither pattern is in the base class.

**Mitigation:** All methods with simplified callback fields remain in inline mode. The emission mode determination checks both `isAsyncWithInnerType` (for `Task<T>`/`ValueTask<T>`) and `isVoidAsync` (for plain `Task`/`ValueTask`). See Concern 1 resolution for details. Simplified callback fields can be added to the base class in a follow-up if the savings justify the complexity.

### Risk 4: Init-Only Property Interceptors

**Risk:** Init-only properties have a fundamentally different pattern (value storage, RecordSet).

**Mitigation:** Init-only properties remain in inline mode. Explicitly excluded from base class mode.

### Risk 5: Diamond Inheritance in Indexers

**Risk:** Multi-key indexers use the `isMulti` codepath with suffixed fields.

**Mitigation:** Multi-key indexers remain in inline mode. Only single-key indexers use base class mode.

### ~~Risk 6: netstandard2.0 Compatibility~~ (REMOVED)

**Concern 5 Resolution:** This risk was based on a factual error. The KnockOff library targets `net8.0;net9.0;net10.0` (per `src/Directory.Build.props`), not netstandard2.0. The `where TDelegate : Delegate` constraint used in the prototype base classes requires C# 7.3+, which is fully supported on all three target frameworks. There are no TFM compatibility concerns for the base classes.

Note: The *generator* project targets netstandard2.0 (required by Roslyn), but the generator does not need to reference the base classes. The generator only emits code that references them. The base classes live in the KnockOff *library* which targets modern .NET only.

### Risk 7: Breaking Backward Compatibility

**Risk:** Generated code changes could break user code that references generated types.

**Mitigation:** The public API surface of generated code does not change. Users interact with `stub.Process.Return("value")`, `stub.OnRows.Get(5)`, etc. These methods and their return types are preserved. The internal implementation (base class vs inline) is invisible to users.

---

## Architectural Verification

### Scope Table

| Pattern | Methods | Properties | Indexers | Events |
|---|---|---|---|---|
| Standalone (1) | Base class | Base class | Base class | N/A (inline) |
| Generic Standalone (2) | Base class | Base class | Base class | N/A |
| Standalone Class (3) | Base class | Base class | Base class | N/A |
| Generic Standalone Class (4) | Base class | Base class | Base class | N/A |
| Inline Interface (5) | Base class | Base class | Base class | N/A |
| Inline Class (6) | Base class | Base class | Base class | N/A |
| Inline Delegate (7) | Base class | N/A | N/A | N/A |
| Open Generic Interface (8) | Base class | Base class | Base class | N/A |
| Open Generic Class (9) | Base class | Base class | Base class | N/A |

**Exceptions** (remain inline regardless of pattern):
- Ref return methods/properties
- Ref/out parameter methods
- Method overload groups
- Task\<T\>/ValueTask\<T\> methods (generate `_callSimplified` fields)
- Task/ValueTask void-async methods (generate `_callSimplifiedVoid` fields)
- Init-only properties
- Multi-key indexers (multiple indexer types on same interface)

### Design Project Verification

Deferred to implementation phase. After the renderer changes, the Design projects must compile and all Design.Tests must pass. This is the primary acceptance criterion.

### Breaking Changes

**None.** The public API surface of generated code is unchanged. The base classes are additive to the KnockOff library.

### Codebase Analysis

Files examined:
- `src/Prototype/Prototype.Library/Interceptors/VoidMethodInterceptorBase.cs` (498 lines) -- void method base
- `src/Prototype/Prototype.Library/Interceptors/MethodInterceptorBase.cs` (413 lines) -- non-void method base
- `src/Prototype/Prototype.Library/Interceptors/PropertyGetInterceptorBase.cs` (282 lines) -- property get base
- `src/Prototype/Prototype.Library/Interceptors/IndexerGetSetInterceptorBase.cs` (951 lines) -- indexer base
- `src/Prototype/Prototype.Library/Unit.cs` (9 lines) -- sentinel type
- `src/Prototype/Prototype.Stubs/Refactored/BasicUserMethodStub.cs` (763 lines) -- refactored method stubs
- `src/Prototype/Prototype.Stubs/Refactored/MatrixStandaloneStub.cs` (422 lines) -- refactored property/indexer stubs
- `src/Prototype/Prototype.Stubs/Refactored/DataReaderStub.cs` (763 lines) -- refactored large-scale stubs
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` (3,424 lines) -- current method renderer
- `src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs` (1,325 lines) -- current property renderer
- `src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs` (2,004 lines) -- current indexer renderer
- `src/Generator/Renderer/Shared/ModelAdapters.cs` (398 lines) -- model adapters
- `src/Generator/Renderer/FlatRenderer.cs` -- flat stub outer renderer
- `src/Generator/Renderer/InlineRenderer.cs` -- inline stub outer renderer
- `src/Generator/Renderer/ClassRenderer.cs` -- class stub outer renderer
- `src/Generator/Renderer/StandaloneClassRenderer.cs` -- standalone class outer renderer
- `src/KnockOff/` -- all existing library files (22 files)

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-02-13 (initial), 2026-02-13 (re-review after concern resolution)

### My Understanding of This Plan

**Core Change:** Port validated prototype interceptor base classes into the KnockOff library, then modify the three shared renderers to emit thin subclasses inheriting from those base classes instead of fully-inlined interceptor code, with inline fallback for edge cases.

**User-Facing API:** No change. Users interact with the same `Return()`, `Call()`, `When()`, `Verify()`, etc. methods. Only the internal structure of generated interceptor classes changes (base class inheritance instead of fully inlined).

**Internal Changes:** (1) Add ~6 new files to `src/KnockOff/Interceptors/` (base classes + Unit), (2) Modify `MethodInterceptorRenderer.cs`, `PropertyInterceptorRenderer.cs`, `IndexerInterceptorRenderer.cs` to add a new "base class mode" rendering path alongside the existing "inline mode" path, (3) Create `PropertyGetSetInterceptorBase<TValue>` which does not exist in the prototype.

**Patterns Affected:** All 9 patterns (they all use the same shared renderers).

### Codebase Investigation

**Files Examined:**
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` (3,424 lines) -- Confirmed structure: `RenderInterceptorClass` -> `RenderSingleSignatureContent` (for single-sig) / `RenderOverloadGroupContent` (for overloads). The single-sig path is where base class mode would apply.
- `src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs` (1,325 lines) -- Confirmed: dispatches on `model.IsInitOnly` to `RenderInitOnlyPropertyContent` vs `RenderRegularPropertyContent`. Regular properties handle HasGetter/HasSetter combinations.
- `src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs` (2,004 lines) -- Confirmed: handles `isMulti` (multiple key types) vs single-key. Dedup for diamond inheritance built in.
- `src/Prototype/Prototype.Library/Interceptors/VoidMethodInterceptorBase.cs` (523 lines) -- Confirmed all fields, priority chain, When chain bases, builder/sequence bases.
- `src/Prototype/Prototype.Library/Interceptors/MethodInterceptorBase.cs` (451 lines) -- Confirmed return value fields, `RunPriorityChain`, `SetupReturnCallback`/`SetupReturnValue`, `CreateValueDelegate`, `new` keyword hiding for Verify/IsConfigured/TotalCallCount/CheckVerification.
- `src/Prototype/Prototype.Library/Interceptors/PropertyGetInterceptorBase.cs` (282 lines) -- Confirmed get-only property base with InvokeGet priority chain and InvokeGetUnconfigured abstract.
- `src/Prototype/Prototype.Library/Interceptors/IndexerGetSetInterceptorBase.cs` (951 lines) -- Confirmed per-key builder, dual When chains, all verification, all inner classes.
- `src/Prototype/Prototype.Library/Unit.cs` (9 lines) -- Trivial sentinel struct.
- `src/Prototype/Prototype.Stubs/Refactored/BasicUserMethodStub.cs` -- Confirmed refactored method interceptors: thin Invoke with RunPriorityChain, builder/sequence thin subclasses, When entry points.
- `src/Prototype/Prototype.Stubs/Refactored/MatrixStandaloneStub.cs` -- Confirmed property and indexer refactored interceptors.
- `src/KnockOff/KnockOff.csproj` -- Library packages the generator DLL in analyzers path.
- `src/Directory.Build.props` -- TFMs are `net8.0;net9.0;net10.0` (NOT netstandard2.0).
- `src/KnockOff/VerificationException.cs`, `StubException.cs`, `Called.cs` -- Confirmed these types are in namespace `KnockOff`, which is the root namespace of the library project.
- Outer renderers (`FlatRenderer.cs`, `InlineRenderer.cs`, `ClassRenderer.cs`, `StandaloneClassRenderer.cs`) -- Confirmed all 4 call the shared renderers' `RenderInterceptorClass` methods.

**Searches Performed:**
- `IsRefReturn` -- found in all 3 shared renderer files and both class/standalone outer renderers. Confirmed it is a model property computed from `ReturnsByRef || ReturnsByRefReadonly`.
- `_callSimplified` -- found in MethodInterceptorRenderer at ~45 locations. Confirmed simplified callback is extensive and deeply woven into the renderer.
- `hasRefOrOut` -- found at ~80 locations in MethodInterceptorRenderer. Confirmed ref/out exclusion is correct.
- `model.Overloads.Count` -- confirmed this is the dispatch for single-sig vs overload groups.
- `model.HasSetter && model.HasGetter` -- confirmed property renderer handles all 3 combinations (get+set, get-only, set-only).
- `IsInitOnly` in IndexerInterceptorRenderer -- only 1 usage at line 590 for source delegation exclusion.

**Design.Stubs Verification:**
- The architect explicitly states "Deferred to implementation phase" for Design Project Verification. The plan does NOT provide Design.Stubs compilation evidence. However, per my understanding of the scope of this plan, this is a generator internals change with no API changes, making Design.Stubs verification less meaningful for the *plan* stage. The acceptance criterion is "all existing tests pass and Design projects compile," which is the right gate. **I accept this deferral** because no new user-facing API is being added -- the same Design.Stubs code should compile before and after the change.

**Discrepancies Found:**
1. **Risk 6 (netstandard2.0) is factually wrong.** The KnockOff library targets `net8.0;net9.0;net10.0` per `src/Directory.Build.props`, NOT netstandard2.0. The `Delegate` constraint used in the prototype base classes (`where TDelegate : Delegate`) requires C# 7.3+, which is fine on .NET 8+. This risk should be removed or corrected.
2. **Plan mentions `FindLastArgInTracking` as "not to port"** but the prototype base classes DO include it (VoidMethodInterceptorBase line 229, MethodInterceptorBase line 192). The refactored stubs actually USE it (BasicUserMethodStub line 52: `FindLastArgInTracking<MethodCallBuilderImpl, string>(b => b.LastArg)`). The plan contradicts the prototype's actual code.
3. **Plan says `PropertyGetInterceptorBase.Get()` returns `PropertyGetBuilderBase`** but the generated code needs to return `IPropertyGetBuilder<TValue>`. Looking at the prototype, it uses `public new IPropertyGetBuilder<int> Get(...)` to shadow the base's `Get()` method. The plan does not mention this `new` keyword shadowing pattern explicitly for properties.

### Structured Question Checklist

**Completeness:**
- [x] All nine patterns addressed -- Yes, all use the same shared renderers.
- [x] Null/empty/default values -- Not applicable (no new user-facing API).
- [x] Generic type parameters -- TArgs/TDelegate/TReturn parametrization is clear.
- [x] Nested types / inherited members -- Not affected (change is internal to interceptor generation).
- [x] Interaction with existing features -- Plan addresses When chains, sequences, verification, source delegation.

**Correctness:**
- [x] Generated code examples compile -- Checked the "After" examples against prototype. They match.
- [x] Consistent with existing patterns -- Yes, adds a parallel code path in the renderer.
- [x] Model/builder/renderer responsibilities -- Correctly keeps models unchanged, minimal optional model addition.
- [x] Breaking changes -- None. Public API surface unchanged. Confirmed.

**Clarity:**
- [ ] Could I implement without clarifying questions? -- **No.** Several concerns below.
- [ ] Ambiguous requirements? -- **Yes.** The `PropertyGetSetInterceptorBase` is recommended but has no prototype validation. Also the async simplified callback scope boundary is unclear.
- [x] Edge cases explicitly handled? -- Ref return, ref/out, overloads, init-only, multi-key all explicitly excluded to inline mode.
- [x] Test strategy specific enough? -- Yes: "all existing tests pass." No new tests needed since behavior is unchanged.

**Risk:**
- [x] What could go wrong? -- Covered in 7 risks. One factual error (Risk 6).
- [x] Existing test failures? -- Plan correctly expects zero test failures.
- [x] Performance implications -- Marginal: one extra virtual call through base class. Acceptable for test code.
- [x] Backward compatibility -- No breaking changes to public API. Confirmed.

### Devil's Advocate Analysis

**Edge cases NOT explicitly covered:**

1. **Void methods returning `Task` or `ValueTask` (not `Task<T>`/`ValueTask<T>`)**: The emission mode table lists "Async Task<T>/ValueTask<T> methods with simplified callback overloads" as inline-only. But what about methods returning plain `Task` or `ValueTask`? These also have `_callSimplifiedVoid` fields (confirmed at MethodInterceptorRenderer line 102-109). The plan does not clarify whether void-async methods (returning `Task`/`ValueTask`) are also excluded from base class mode. If they are, the "80-90% base class mode" estimate drops because many async methods return `Task` (not `Task<T>`).

2. **Set-only properties**: The plan discusses get-only, get+set, and init-only properties. But properties can be set-only (no getter). The property renderer handles this (`model.HasSetter && !model.HasGetter`). The prototype's `PropertyGetInterceptorBase` is get-focused. Neither the prototype nor the plan addresses set-only properties for base class mode. These would need to remain inline, or a separate `PropertySetInterceptorBase` would be needed. The plan should explicitly state this.

3. **Property `_valueSet` round-trip storage**: For regular get+set properties, the current renderer emits `_valueSet` and `_value` backing fields (PropertyInterceptorRenderer line 165-170) that enable "set a value via the setter, read it back via the getter" behavior. Neither the prototype `PropertyGetInterceptorBase` nor the proposed `PropertyGetSetInterceptorBase` accounts for this round-trip pattern. The developer creating `PropertyGetSetInterceptorBase` would need to include this, but the plan does not describe it.

**Ways this could break existing functionality:**

1. **`new` keyword hiding in `MethodInterceptorBase`**: The prototype uses `new` to hide `Verify()`, `IsConfigured`, `TotalCallCount`, `CheckVerification()`, and `CheckVerificationAll()` on `MethodInterceptorBase`. This works because the generated code always calls these through the concrete interceptor type (not through the base). But if any generated code holds a reference typed as `VoidMethodInterceptorBase`, the wrong (void-version) method would be called, producing incorrect verification counts. The plan should confirm that the generated code always uses the concrete type. I *believe* this is safe because interceptors are always accessed as their concrete type (e.g., `stub.Process.Verify()`), but it deserves explicit acknowledgment.

**Ways users could misunderstand the API:**

1. Not applicable -- no user-facing API changes.

### Concerns

1. **[Ambiguity]: Void-Async methods (`Task`/`ValueTask` return, not `Task<T>`/`ValueTask<T>`)**
   - Details: The emission mode table says "Async Task<T>/ValueTask<T> methods with simplified callback overloads" fall back to inline mode. But the renderer also generates `_callSimplifiedVoid` fields for methods returning plain `Task` or `ValueTask` (not generic). The base class does not include these fields either. Are void-async methods also excluded from base class mode?
   - Question: Should the emission mode table explicitly list `Task`/`ValueTask` (non-generic) methods as inline-only alongside `Task<T>`/`ValueTask<T>`? Or can void-async methods use base class mode since their "simplified void callback" is just a convenience and the full callback still works through `_call`?
   - Suggestion: The simplest approach is: any method with simplified callback fields (either `_callSimplified` or `_callSimplifiedVoid`) stays inline. This is easy to detect and avoids subtle bugs. The plan should say this explicitly.

2. **[Contradiction]: `FindLastArgInTracking` listed as "NOT to port" but used in prototype**
   - Details: Section "What NOT to Port" says `FindLastArgInTracking` should not be ported. But the prototype base classes include it (`VoidMethodInterceptorBase` line 229, `MethodInterceptorBase` line 192), and the refactored stubs use it (`BasicUserMethodStub` line 52). The plan contradicts the validated prototype.
   - Question: Should `FindLastArgInTracking` be ported (as it is in the prototype) or not? If not, what replaces the `LastArg` property implementation in generated code?
   - Suggestion: Port it. The prototype includes it and tests pass with it. The plan's reasoning ("nullable type widening makes a generic base class method impractical") appears to be outdated since the prototype successfully uses it with a generic approach (`FindLastArgInTracking<TBuilder, TResult>(Func<TBuilder, TResult> selector)`).

3. **[Gap]: Set-only properties not addressed**
   - Details: Properties can have only a setter (no getter). The current renderer handles this (`model.HasSetter && !model.HasGetter` paths in PropertyInterceptorRenderer). The plan discusses get-only and get+set but does not mention set-only.
   - Question: Should set-only properties remain inline, or should a `PropertySetInterceptorBase` be created?
   - Suggestion: The simplest approach is to keep set-only properties inline. They are rare. The plan should explicitly list this as an inline-mode case.

4. **[Gap]: `PropertyGetSetInterceptorBase` is not in the prototype -- no validation**
   - Details: The plan recommends creating `PropertyGetSetInterceptorBase<TValue>` extending `PropertyGetInterceptorBase<TValue>` with set support. This class does not exist in the validated prototype. The plan acknowledges this ("not in prototype, must be created") but this means the developer is designing a new base class without prototype validation.
   - Question: Should the developer create this class from scratch during implementation, or should a more conservative approach be used (e.g., only base-class-ify the get side of get+set properties, leave set inline)?
   - Suggestion: I am comfortable creating this class since the property set pattern closely mirrors the get pattern (callback, tracking, sequence, verification). But the plan should acknowledge this is unvalidated territory and the developer should test it thoroughly. The `_valueSet`/`_value` round-trip fields also need to be addressed in this new class.

5. **[Factual Error]: Risk 6 -- netstandard2.0 is wrong**
   - Details: The plan states "The KnockOff library targets multiple TFMs. Base classes must be compatible with all targets" and discusses netstandard2.0. But `src/Directory.Build.props` shows `TargetFrameworks` is `net8.0;net9.0;net10.0`. There is no netstandard2.0 target.
   - Question: Is this just a documentation error, or is there a plan to add netstandard2.0 support?
   - Suggestion: Correct Risk 6 to reflect the actual TFMs. The `where TDelegate : Delegate` constraint is fine on .NET 8+.

### What Looks Good

- The emission mode concept is sound -- it provides a clean escape hatch for edge cases while capturing the majority of interceptors in base class mode.
- The plan correctly identifies that only the shared renderers need modification -- all 4 outer renderers call through to them.
- The prototype has been thoroughly validated (110 tests pass, confirmed ~91% reduction).
- The phased implementation order is logical (library first, then method renderer, then property, then indexer, with test gates between each).
- The "What Gets Removed" / "What Gets Added" / "What Stays the Same" breakdown for the method renderer is detailed and accurate based on my reading of both the prototype and the current renderer.
- The risk analysis covers the major failure modes (overloads, ref/out, ref return, init-only, multi-key indexers).

### Re-Review (2026-02-13) -- After Concern Resolution

All 5 concerns have been satisfactorily addressed by the architect. Verification of each resolution:

**Concern 1 (Void-Async):** RESOLVED. The emission mode table now has two separate rows for `Task<T>`/`ValueTask<T>` and `Task`/`ValueTask`. The detection logic (`isAsyncWithInnerType || isVoidAsync`) correctly maps to the existing renderer code at MethodInterceptorRenderer lines 92-109. The estimate was appropriately adjusted from 80-90% to 70-80%. I confirmed `GetVoidAsyncInfo()` exists in the renderer and `isVoidAsync` is used at ~15 locations.

**Concern 2 (FindLastArgInTracking):** RESOLVED. The plan now correctly includes `FindLastArgInTracking` in the port list (Phase 5.1 steps 3-4). The "What NOT to Port" section was updated to remove it. The explanation of why the original exclusion was wrong is thorough and accurate -- the generic `FindLastArgInTracking<TBuilder, TResult>(Func<TBuilder, TResult> selector)` pattern works cleanly. The note about simplified callback tracking not being searched is correct since those methods stay inline anyway.

**Concern 3 (Set-only properties):** RESOLVED. A new `PropertySetInterceptorBase<TValue>` is now planned, with its own section in the plan and a step in Phase 5.1. The emission mode table updated to include set-only properties in base class mode. This is the right approach -- set-only properties mirror the get-only structural pattern.

**Concern 4 (PropertyGetSetInterceptorBase unvalidated):** RESOLVED. The architect provided detailed requirements for the new class including: (a) set-side fields mirroring get-side, (b) `_valueSet`/`_value` round-trip storage explicitly listed, (c) `InvokeGetUnconfiguredFinal` layering pattern for the round-trip check, (d) stub override helpers for set, (e) explicit fallback plan (leave get+set inline if difficulties arise). One implementation detail not explicitly called out: the `InvokeGet` in the base class returns `default!` when sequence is exhausted and `_repeatLastValue = false` (PropertyGetInterceptorBase line 109), but get+set properties should return the round-trip `_value` instead. The developer should handle this when implementing `PropertyGetSetInterceptorBase` -- this is an implementation detail within the architect's stated design.

**Concern 5 (netstandard2.0):** RESOLVED. Risk 6 is struck through with a clear explanation that the library targets `net8.0;net9.0;net10.0`. The note about the generator targeting netstandard2.0 (for Roslyn) is a helpful clarification.

### Verdict: APPROVED

This plan is ready for implementation. The architect has addressed every concern with clear resolutions, updated the relevant plan sections, and provided fallback plans for unvalidated territory. The emission mode concept is sound, the inline fallback provides a safety net, and the phased implementation order with test gates between each phase reduces risk.

---

## Concern Resolution Summary

All 5 developer concerns have been addressed. Resolutions are embedded inline in the relevant plan sections (marked with "Concern N Resolution" headings). Summary:

| # | Concern | Resolution | Plan Section Updated |
|---|---------|------------|---------------------|
| 1 | Void-Async Task/ValueTask ambiguity | Both `Task`/`ValueTask` (void-async) and `Task<T>`/`ValueTask<T>` methods stay inline. Two separate rows in emission mode table. Estimate adjusted to 70-80%. | Emission Mode table, Risk 3, Scope Table exceptions |
| 2 | FindLastArgInTracking contradiction | **Port it.** The plan was wrong; the prototype includes and uses it. Generated `LastArg`/`LastArgs` will call `FindLastArgInTracking<TBuilder, TResult>`. | "What NOT to Port" section (item removed), "What Stays the Same" item 3, Phase 5.1 steps 3-4 |
| 3 | Set-only properties gap | Create `PropertySetInterceptorBase<TValue>`. Set-only properties use base class mode. | New section "Set-Only Properties (Base Class Mode)", emission mode table, Phase 5.1 step 7, Phase 5.3 step 4 |
| 4 | PropertyGetSetInterceptorBase unvalidated | Acknowledged as unvalidated. Detailed requirements added: `_valueSet`/`_value` round-trip, `InvokeGetUnconfiguredFinal` layering, `PropertySetBuilderBase`/`PropertySetSequenceBase`. Fallback plan: leave get+set inline if difficulties arise. | "Get+Set Properties (Base Class Mode)" section fully rewritten |
| 5 | Risk 6 netstandard2.0 factual error | Risk removed. KnockOff library targets `net8.0;net9.0;net10.0`. `where TDelegate : Delegate` is fine. | Risk 6 section struck through and corrected |

---

## Implementation Contract

**Created:** 2026-02-13
**Approved by:** knockoff-developer

### Acceptance Criteria

No Design.Stubs acceptance criteria (this is an internal generator change, no new user-facing API). The primary acceptance criteria are:
- All existing tests pass unchanged across all target frameworks
- `dotnet build src/Design/Design.Stubs` succeeds
- `dotnet test src/Design/Design.Tests` passes
- Generated `.g.cs` files show base class inheritance for eligible interceptors and inline mode for edge cases

### In Scope

**Phase 5.1: Library (Must Be First)**

- [ ] Create `src/KnockOff/Interceptors/` directory
- [ ] Port `Unit.cs` to `src/KnockOff/Unit.cs` -- zero-size sentinel struct for 0-param methods
- [ ] Port `VoidMethodInterceptorBase.cs` to `src/KnockOff/Interceptors/VoidMethodInterceptorBase.cs` -- includes MethodCallBuilderBase, MethodSequenceBase, void When chain classes, FindLastArgInTracking, SetupCallback
- [ ] Port `MethodInterceptorBase.cs` to `src/KnockOff/Interceptors/MethodInterceptorBase.cs` -- includes ReturnMethodCallBuilderBase, ReturnMethodSequenceBase, non-void When chain classes, FindLastArgInTracking (new override), SetupReturnCallback, SetupReturnValue, CreateValueDelegate
- [ ] Port `PropertyGetInterceptorBase.cs` to `src/KnockOff/Interceptors/PropertyGetInterceptorBase.cs` -- includes PropertyGetBuilderBase, PropertyGetSequenceBase, InvokeGet priority chain, InvokeGetUnconfigured abstract
- [ ] Create `src/KnockOff/Interceptors/PropertySetInterceptorBase.cs` (new, not in prototype) -- mirrors get-only pattern for set-only properties. Includes PropertySetBuilderBase, PropertySetSequenceBase, InvokeSet priority chain, InvokeSetUnconfigured abstract
- [ ] Create `src/KnockOff/Interceptors/PropertyGetSetInterceptorBase.cs` (new, not in prototype) -- extends PropertyGetInterceptorBase with set-side fields, `_valueSet`/`_value` round-trip storage, InvokeSet, InvokeGetUnconfiguredFinal layering. See Concern 4 resolution for detailed requirements. **Fallback plan: if this proves problematic, leave get+set properties in inline mode and revisit later.**
- [ ] Port `IndexerGetSetInterceptorBase.cs` to `src/KnockOff/Interceptors/IndexerGetSetInterceptorBase.cs` -- includes PerKeyBuilder, dual When chains, all builder/sequence bases
- [ ] Change namespace from `Prototype.Library.Interceptors` to `KnockOff.Interceptors` in all ported files
- [ ] Ensure all types are `public` (used by generated code in user projects)
- [ ] **Checkpoint:** `dotnet build src/KnockOff/KnockOff.csproj` must compile with no errors

**Phase 5.2: Method Interceptor Renderer**

- [ ] Add emission mode determination to `MethodInterceptorRenderer.RenderInterceptorClass` -- check `model.Overloads.Count == 0` AND NOT `hasRefOrOut` AND NOT `isAsyncWithInnerType` AND NOT `isVoidAsync` AND NOT `model.IsRefReturn`
- [ ] Create `RenderBaseClassContent` method alongside existing `RenderSingleSignatureContent`
- [ ] `RenderBaseClassContent` emits: class declaration with base class inheritance, constructor, abstract overrides (InvokeDelegate/InvokeVoidDelegate, RecordArgs, RecordUnconfiguredArgs, CreateValueDelegate for non-void), source field, custom delegate, unconfigured last arg/args fields + LastArg/LastArgs using FindLastArgInTracking, Return/Call entry points using SetupReturnCallback/SetupReturnValue/SetupCallback, When entry points, thin Invoke (RunPriorityChain/RunVoidPriorityChain + unconfigured tail), Reset override, thin inner classes (MethodCallBuilderImpl, MethodSequenceImpl, WhenBuilder, WhenChain / VoidWhenChain)
- [ ] Preserve `RenderSingleSignatureContent` for inline mode (ref return, ref/out, async simplified, void-async)
- [ ] **Checkpoint:** `dotnet test src/KnockOff.sln` -- all tests must pass

**Phase 5.3: Property Interceptor Renderer**

- [ ] Add emission mode determination to `PropertyInterceptorRenderer.RenderInterceptorClass` -- check NOT `model.IsInitOnly` AND NOT `model.IsRefReturn`
- [ ] Create `RenderBaseClassGetOnlyContent` for get-only properties (`model.HasGetter && !model.HasSetter`)
- [ ] Create `RenderBaseClassSetOnlyContent` for set-only properties (`model.HasSetter && !model.HasGetter`)
- [ ] Create `RenderBaseClassGetSetContent` for get+set properties (`model.HasGetter && model.HasSetter`)
- [ ] Preserve `RenderInitOnlyPropertyContent` for init-only properties (inline mode)
- [ ] Preserve `RenderRegularPropertyContent` for ref return properties (inline mode)
- [ ] **Checkpoint:** `dotnet test src/KnockOff.sln` -- all tests must pass

**Phase 5.4: Indexer Interceptor Renderer**

- [ ] Add emission mode determination to `IndexerInterceptorRenderer.RenderInterceptorClass` -- check `isMulti == false` AND NOT any model `IsInitOnly` AND NOT any model `IsRefReturn`
- [ ] Create `RenderBaseClassContent` for single-key indexers
- [ ] Preserve existing renderer for multi-key indexers, init-only indexers (inline mode)
- [ ] **Checkpoint:** `dotnet test src/KnockOff.sln` -- all tests must pass

**Phase 5.5: Final Verification**

- [ ] `dotnet build src/KnockOff.sln` -- full solution build
- [ ] `dotnet test src/KnockOff.sln` -- all tests pass across all TFMs (net8.0, net9.0, net10.0)
- [ ] `dotnet build src/Design/Design.Stubs` -- Design.Stubs compiles
- [ ] `dotnet test src/Design/Design.Tests` -- Design.Tests pass
- [ ] Spot-check 3+ generated `.g.cs` files to verify: (a) base class mode interceptors inherit from the correct base class, (b) inline mode interceptors still have full inlined code, (c) generated code is structurally shorter for base class mode

### Explicitly Out of Scope

- **Async simplified callback in base class** -- `_callSimplified` / `_callSimplifiedVoid` fields are not added to base classes. Methods returning Task/ValueTask/Task\<T\>/ValueTask\<T\> remain in inline mode. This is a potential follow-up optimization.
- **Multi-key indexer base class** -- No `DualKeyIndexerInterceptorBase` or similar. Multi-key indexers remain inline.
- **Events** -- Event interceptors use a simpler pattern and are not affected.
- **New test files** -- No new test files needed. Existing tests verify behavioral correctness.
- **Prototype modifications** -- The prototype (`src/Prototype/`) is not modified.
- **Documentation updates** -- No docs changes needed for internal generator changes.

### Verification Gates

1. **After Phase 5.1 (Library):** `dotnet build src/KnockOff/KnockOff.csproj` compiles with no errors across all 3 TFMs. No tests run yet (library additions are additive).
2. **After Phase 5.2 (Method Renderer):** `dotnet test src/KnockOff.sln` -- all tests pass. This is the highest-risk phase because method interceptors are the most complex and most numerous.
3. **After Phase 5.3 (Property Renderer):** `dotnet test src/KnockOff.sln` -- all tests pass. The `PropertyGetSetInterceptorBase` is unvalidated territory; if tests fail due to the round-trip storage pattern, evaluate whether to fall back to inline mode for get+set properties.
4. **After Phase 5.4 (Indexer Renderer):** `dotnet test src/KnockOff.sln` -- all tests pass.
5. **Final:** Full solution builds, all tests pass, Design.Stubs compiles, Design.Tests pass, spot-check confirms structural changes.

### Stop Conditions

If any of these occur, STOP and report:
- Out-of-scope test fails (a test that was passing before AND is not directly related to interceptor generation)
- Architectural contradiction discovered (e.g., generated inner class cannot access protected base class fields as the prototype assumes)
- `PropertyGetSetInterceptorBase` round-trip pattern fails and cannot be resolved within reasonable effort -- **fall back to inline mode for get+set properties** and report
- Generated code does not compile for any pattern that should be in base class mode
- Base class `new` keyword hiding causes incorrect behavior through any code path (e.g., `VoidMethodInterceptorBase.Verify()` called instead of `MethodInterceptorBase.Verify()`)

### Implementation Notes for Developer

1. **Start with the method renderer (Phase 5.2)** -- it is the most complex and the prototype provides the most detailed reference. If the method renderer works, the property and indexer renderers will follow the same structural pattern.
2. **The prototype refactored stubs are the ground truth** for what generated base class mode code should look like. Compare against `src/Prototype/Prototype.Stubs/Refactored/BasicUserMethodStub.cs` (methods), `MatrixStandaloneStub.cs` (properties + indexers), and `DataReaderStub.cs` (large-scale).
3. **The `new` keyword hiding in MethodInterceptorBase** (for Verify, IsConfigured, TotalCallCount, CheckVerification, CheckVerificationAll) is intentional and verified in the prototype. The generated code always references interceptors through their concrete type, so the correct (non-void-aware) versions are always called.
4. **The `PropertyGetSetInterceptorBase` is the one piece of new design.** If it proves problematic, the explicit fallback is to leave get+set properties in inline mode. This is the safest option and sacrifices some code savings for zero risk.
5. **Emission mode determination should be a simple boolean check** computed at the top of the renderer method. If adding `UseBaseClass` to the model improves readability, do so. If computing locally is cleaner, that is also fine.

---

## Appendix A: Generated Code Examples

### Before (Current Inline Mode) -- Method Interceptor

```csharp
public sealed class ProcessInterceptor
{
    internal IService? _source;
    public delegate string ProcessDelegate(string input);
    private ProcessDelegate? _call;
    private MethodCallBuilderImpl? _callTracking;
    private string _returnValue = default!;
    private bool _hasReturnValue;
    private MethodCallBuilderImpl? _returnValueTracking;
    private List<(ProcessDelegate Callback, MethodCallBuilderImpl Tracking)>? _sequence;
    private int _sequenceIndex;
    private bool _repeatLastValue = true;
    private List<WhenMatcher>? _whenChain;
    private int _whenChainHead;
    private bool _whenVerifiable;
    private bool _isVerifiable;
    private Called? _verifiableTimes;
    private int _unconfiguredCallCount;
    private string? _unconfiguredLastArg;

    // ~15 lines: LastArg property
    // ~8 lines: TotalCallCount property
    // ~10 lines: Verify methods
    // ~15 lines: Return(callback) method
    // ~15 lines: Return(value) method
    // ~10 lines: Return(first, params rest) method
    // ~6 lines: When entry points
    // ~80 lines: Invoke method (full priority chain)
    // ~12 lines: Reset method
    // ~25 lines: CheckVerification / CheckVerificationAll
    // ~35 lines: MethodCallBuilderImpl class
    // ~25 lines: MethodSequenceImpl class
    // ~40 lines: WhenMatcher classes
    // ~10 lines: WhenBuilder class
    // ~25 lines: WhenChain class
    // Total: ~430 lines
}
```

### After (Base Class Mode) -- Method Interceptor

```csharp
public sealed class ProcessInterceptor : MethodInterceptorBase<ProcessInterceptor.ProcessDelegate, string, string>
{
    internal IService? _source;
    public delegate string ProcessDelegate(string input);

    public ProcessInterceptor() : base("Process") { }

    // Abstract overrides (~4 lines)
    protected override string InvokeDelegate(ProcessDelegate del, string args) => del(args);
    protected override void RecordArgs(string args, MethodCallBuilderBase tracking)
    {
        if (tracking is MethodCallBuilderImpl impl) impl.RecordArg(args);
    }
    protected override void RecordUnconfiguredArgs(string args) => _unconfiguredLastArg = args;
    protected override ProcessDelegate CreateValueDelegate(string value) => (_) => value;

    // Unconfigured arg tracking (~5 lines, using FindLastArgInTracking from base class)
    private string? _unconfiguredLastArg;
    public string? LastArg
    {
        get
        {
            var found = FindLastArgInTracking<MethodCallBuilderImpl, string>(b => b.LastArg);
            return found ?? (_unconfiguredCallCount > 0 ? _unconfiguredLastArg : default);
        }
    }

    // Return methods (~6 lines each, using SetupReturn helpers)
    public MethodCallBuilderImpl Return(ProcessDelegate callback)
    {
        var b = new MethodCallBuilderImpl(this);
        SetupReturnCallback(callback, b);
        return b;
    }
    public MethodCallBuilderImpl Return(string value)
    {
        var b = new MethodCallBuilderImpl(this);
        SetupReturnValue(value, b);
        return b;
    }

    // When entry points (~4 lines each)
    public WhenBuilder When(string input) { ... }
    public WhenBuilder When(Func<string, bool> predicate) { ... }

    // Thin Invoke (~10 lines -- only unconfigured tail)
    internal string Invoke(bool strict, Stub stub, string input)
    {
        var (handled, result) = RunPriorityChain(input);
        if (handled) return result;
        _unconfiguredCallCount++;
        RecordUnconfiguredArgs(input);
        var (seqHandled, seqResult) = HandleNonVoidSequenceExhaustedRepeat(strict, input);
        if (seqHandled) return seqResult;
        if (_source is { } src) return src.Process(input);
        if (strict) throw StubException.NotConfigured("", "Process");
        return stub.Process_(input);
    }

    // Reset override (~3 lines)
    public override void Reset() { base.Reset(); _unconfiguredLastArg = default; _source = null; }

    // Thin inner classes (~20 + ~15 + ~8 + ~20 lines)
    public sealed class MethodCallBuilderImpl : ReturnMethodCallBuilderBase { ... }
    public sealed class MethodSequenceImpl : ReturnMethodSequenceBase { ... }
    public sealed class WhenBuilder : WhenBuilderBase { ... }
    public sealed class WhenChain : WhenChainBase { ... }

    // Total: ~100 lines
}
```

### Before (Current) -- Property Interceptor

```csharp
public sealed class RowsInterceptor
{
    internal IMatrix? _source;
    private Func<int>? _get;
    private PropertyGetBuilderImpl? _getTracking;
    private List<(Func<int> Callback, PropertyGetBuilderImpl Tracking)>? _getSequence;
    private int _getSequenceIndex;
    private bool _getRepeatLastValue = true;
    private bool _isGetVerifiable;
    private Called? _getVerifiableTimes;
    private int _unconfiguredGetCount;

    // ~8 lines: TotalGetCount
    // ~10 lines: Get() methods
    // ~6 lines: Stub override helpers
    // ~30 lines: InvokeGet
    // ~10 lines: Reset
    // ~30 lines: Verification methods
    // ~20 lines: Internal verification
    // ~50 lines: PropertyGetBuilderImpl
    // ~40 lines: PropertyGetSequenceImpl
    // Total: ~250 lines
}
```

### After (Base Class Mode) -- Property Interceptor

```csharp
public sealed class RowsInterceptor : PropertyGetInterceptorBase<int>
{
    internal IMatrix? _source;

    public RowsInterceptor() : base("Rows") { }

    protected override int InvokeGetUnconfigured(bool strict)
    {
        if (_source is { } src) return src.Rows;
        if (strict) throw StubException.NotConfigured("", "Rows");
        return default!;
    }

    public override void Reset() { base.Reset(); _source = null; }

    // Typed Get() methods and inner classes for IPropertyGetBuilder<int>
    // Total: ~40-50 lines
}
```
