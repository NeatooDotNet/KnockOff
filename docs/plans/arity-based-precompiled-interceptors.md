# Arity-Based Pre-compiled Interceptors

**Status:** Ready for Implementation
**Last Updated:** 2026-02-14 (Developer approved, implementation contract created)
**Created:** 2026-02-14
**Related Todo:** [Arity-Based Pre-compiled Interceptors](../todos/arity-based-precompiled-interceptors.md)

---

## Problem Statement

The v0.49.0 base class work reduced generated code lines by ~26%, but benchmarking showed **no improvement in MSBuild build time**. The bottleneck is the **number of generated types**, not lines of code. The test suite generates ~8,860 sealed classes (interceptors + inner builder/when/chain types). Each type requires metadata generation, type checking, and generic instantiation regardless of how few lines it contains.

The current architecture generates per member:
- 1 interceptor sealed class (~100 lines)
- 1 MethodCallBuilderImpl inner class (~48 lines)
- 1 WhenBuilder inner class (~10 lines)
- 1 WhenChain inner class (~30 lines)
- **= 4 types, ~200 lines per method member**

---

## Solution

Replace generated interceptor classes with **fields of pre-compiled generic types parameterized by arity**. All behavioral logic (Return, When, sequences, verification, builders, When chains) lives in the KnockOff library, compiled once.

**Per method member: 0 generated types, 1 field declaration.**

---

## Pre-compiled Type Families

### Sync Methods (non-void)

`MethodInterceptor0<TReturn>` through `MethodInterceptor8<T1, ..., T8, TReturn>`

Each contains:
- `Invoke(bool strict, T1 arg1, ..., TN argN) → TReturn`
- `Return(Func<T1, ..., TN, TReturn> callback) → MethodCallBuilderN`
- `Return(TReturn value) → MethodCallBuilderN`
- `Return(TReturn first, params TReturn[] rest) → MethodSequenceN`
- `When(T1 arg1, ..., TN argN) → WhenBuilderN`
- `When(Func<T1, ..., TN, bool> predicate) → WhenBuilderN`
- `Verify(Called)`, `Reset()`, `CheckVerification()`, `CheckVerificationAll()`
- `SetFallback(Func<T1, ..., TN, TReturn>?)` for stub overrides
- `SetSourceFallback(Func<T1, ..., TN, TReturn>?)` for source delegation
- Inner classes: `MethodCallBuilderN`, `MethodSequenceN`, `WhenBuilderN`, `WhenChainN` (all pre-compiled)

### Sync Methods (void)

`VoidMethodInterceptor0` through `VoidMethodInterceptor8<T1, ..., T8>`

Same as above but:
- `Invoke(bool strict, ...)` returns `void`
- `Call(Action<T1, ..., TN> callback)` instead of `Return`
- No `Return(value)` overloads

### Async Methods (non-void)

`AsyncMethodInterceptor0<TReturn>` through `AsyncMethodInterceptor8<T1, ..., T8, TReturn>`

Handles `Task<TReturn>` and `ValueTask<TReturn>` interface methods.

Key difference from sync: two Return overloads for sync and async callbacks:
- `Return(Func<T1, ..., TN, TReturn> callback)` - simplified, wrapped in `Task.FromResult` internally
- `Return(TReturn value)` - simplified
- `Return(Func<T1, ..., TN, Task<TReturn>> asyncCallback)` - full async chain support
- `Invoke(bool strict, ...) → Task<TReturn>`

User can write either:
```csharp
stub.GetAsync.Return(key => 42);                              // simplified
stub.GetAsync.Return(async key => await localAsync(key));      // full async
```

Generated interface implementations:
```csharp
// Task<T>:
Task<int> IService.GetAsync(string key) => GetAsync.Invoke(Strict, key);

// ValueTask<T> - wraps the Task:
ValueTask<int> IService.ComputeAsync(int x) => new(ComputeAsync.Invoke(Strict, x));
```

### Async Methods (void)

`AsyncVoidMethodInterceptor0` through `AsyncVoidMethodInterceptor8<T1, ..., T8>`

Handles `Task` and `ValueTask` interface methods (no inner return type).

- `Call(Action<T1, ..., TN> callback)` - simplified
- `Call(Func<T1, ..., TN, Task> asyncCallback)` - full async
- `Invoke(bool strict, ...) → Task`

Generated interface implementations:
```csharp
// Task:
Task IService.ExecuteAsync(string cmd) => ExecuteAsync.Invoke(Strict, cmd);

// ValueTask:
ValueTask IService.RunAsync(string cmd) => new(RunAsync.Invoke(Strict, cmd));
```

### Properties

Made concrete (non-abstract) with delegate fields replacing abstract methods:

- `PropertyGetInterceptor<TValue>` - get-only properties
- `PropertySetInterceptor<TValue>` - set-only properties
- `PropertyGetSetInterceptor<TValue>` - get+set properties

The current abstract methods (`InvokeGetUnconfigured`, `InvokeSetUnconfigured`) become virtual with default behavior (strict throw or return default). Stub override and source fallbacks wired via `SetFallback`/`SetSourceFallback` delegates.

### Indexers

- `IndexerGetSetInterceptor<TKey, TValue>` - made concrete, same delegate field pattern as properties

### Events (Out of Scope)

Events are **out of scope** for this feature. They generate only 1 type per event with no inner classes -- minimal ROI for conversion. Events continue using the current generated-class approach unchanged.

### Type Count Summary

| Family | Count |
|---|---|
| `MethodInterceptor0..8` | 9 |
| `VoidMethodInterceptor0..8` | 9 |
| `AsyncMethodInterceptor0..8` | 9 |
| `AsyncVoidMethodInterceptor0..8` | 9 |
| Property types | 3 |
| Indexer types | 1 |
| **Total library types** | **~40** |

All pre-compiled in the KnockOff library. Compiled once, reused across all consumer projects.

---

## Generated Code: Before and After

### CalculatorStub (4 methods: Add, Subtract, Divide, Reset)

**Before: 776 lines, 16 generated types**

```csharp
partial class CalculatorStub : CalculatorStubBase, ICalculator, IKnockOffStub
{
    // Per method: ~200 lines, 4 types (interceptor + builder + when builder + when chain)
    public sealed class AddInterceptor : MethodInterceptorBase<AddDelegate, (int a, int b), int>
    {
        public delegate int AddDelegate(int a, int b);
        // InvokeDelegate, CreateValueDelegate, RecordArgs, RecordUnconfiguredArgs overrides
        // LastArgs property with complex getter
        // Return(callback), Return(value), Return(first, params rest)
        // When(a, b), When(predicate)
        // Invoke method with full priority chain
        // Reset override

        public sealed class MethodCallBuilderImpl : ReturnMethodCallBuilderBase, IMethodReturnBuilderArgs<...>
        {
            // LastArgs, RecordArg, Reset, ThenReturn overloads, Verifiable
            // 7 explicit interface implementations
        }

        public sealed class WhenBuilder : WhenBuilderBase { /* Return(value) */ }

        public sealed class WhenChain : WhenChainBase
        {
            // ThenWhen(a, b), ThenWhen(predicate), ThenCall, ThenNone, Verifiable
        }
    }

    // Repeat for SubtractInterceptor, DivideInterceptor, ResetInterceptor...

    public AddInterceptor Add { get; } = new();
    public SubtractInterceptor Subtract { get; } = new();
    public DivideInterceptor Divide { get; } = new();
    public ResetInterceptor Reset { get; } = new();

    // Strict, Object, Verify, VerifyAll, Source, interface implementations
}
```

**After: ~60 lines, 0 generated types**

```csharp
partial class CalculatorStub : CalculatorStubBase, ICalculator, IKnockOffStub
{
    public MethodInterceptor2<int, int, int> Add { get; } = new("Add");
    public MethodInterceptor2<int, int, int> Subtract { get; } = new("Subtract");
    public MethodInterceptor2<int, int, int> Divide { get; } = new("Divide");
    public VoidMethodInterceptor0 Reset { get; } = new("Reset");

    public bool Strict { get; set; } = false;
    public ICalculator Object => this;

    int ICalculator.Add(int a, int b) => Add.Invoke(Strict, a, b);
    int ICalculator.Subtract(int a, int b) => Subtract.Invoke(Strict, a, b);
    int ICalculator.Divide(int a, int b) => Divide.Invoke(Strict, a, b);
    void ICalculator.Reset() => Reset.Invoke(Strict);

    public void Source(ICalculator? source)
    {
        Add.SetSourceFallback(source != null ? source.Add : null);
        Subtract.SetSourceFallback(source != null ? source.Subtract : null);
        Divide.SetSourceFallback(source != null ? source.Divide : null);
        Reset.SetSourceFallback(source != null ? source.Reset : null);
    }

    public void Verify()
    {
        var failures = new List<VerificationFailure>();
        if (Add.CheckVerification() is { } f1) failures.Add(f1);
        if (Subtract.CheckVerification() is { } f2) failures.Add(f2);
        if (Divide.CheckVerification() is { } f3) failures.Add(f3);
        if (Reset.CheckVerification() is { } f4) failures.Add(f4);
        if (failures.Count > 0) throw new VerificationException(failures);
    }

    public void VerifyAll()
    {
        var failures = new List<VerificationFailure>();
        if (Add.CheckVerificationAll() is { } f1) failures.Add(f1);
        if (Subtract.CheckVerificationAll() is { } f2) failures.Add(f2);
        if (Divide.CheckVerificationAll() is { } f3) failures.Add(f3);
        if (Reset.CheckVerificationAll() is { } f4) failures.Add(f4);
        if (failures.Count > 0) throw new VerificationException(failures);
    }
}
```

---

## Stub Override Support

The generated base class is **unchanged**:

```csharp
public class BasicStubOverrideStubBase
{
    protected virtual string Process_(string input) => default!;
    protected virtual int Calculate_(int a, int b) => default!;
}
```

The stub wires fallbacks in a generated constructor:

```csharp
partial class BasicStubOverrideStub : BasicStubOverrideStubBase, IStubOverrideService, IKnockOffStub
{
    public MethodInterceptor1<string, string> Process { get; } = new("Process");
    public MethodInterceptor2<int, int, int> Calculate { get; } = new("Calculate");

    public BasicStubOverrideStub()
    {
        Process.SetFallback(Process_);       // method group → virtual method
        Calculate.SetFallback(Calculate_);
    }

    string IStubOverrideService.Process(string input) => Process.Invoke(Strict, input);
    int IStubOverrideService.Calculate(int a, int b) => Calculate.Invoke(Strict, a, b);
}
```

Priority chain in the pre-compiled `Invoke`: When → Sequence → Return → **Fallback** → Source → Strict/Default.

User code is unchanged:
```csharp
protected override string Process_(string input) => $"[Processed: {input}]";
```

---

## Overloaded Methods

Overloaded methods generate a **thin compositor class** - one type with zero behavioral logic, just delegation to inner arity-based interceptor fields:

```csharp
// IFormatter has Format(string), Format(string, FormatOptions), Format(string, FormatOptions, int)
public sealed class FormatInterceptor
{
    internal MethodInterceptor1<string, string> _ov1 = new("Format");
    internal MethodInterceptor2<string, FormatOptions, string> _ov2 = new("Format");
    internal MethodInterceptor3<string, FormatOptions, int, string> _ov3 = new("Format");

    // Return - compiler resolves by lambda arity
    public MethodCallBuilder1<string, string> Return(Func<string, string> cb) => _ov1.Return(cb);
    public MethodCallBuilder2<string, FormatOptions, string> Return(Func<string, FormatOptions, string> cb) => _ov2.Return(cb);
    public MethodCallBuilder3<string, FormatOptions, int, string> Return(Func<string, FormatOptions, int, string> cb) => _ov3.Return(cb);

    // When - compiler resolves by param count
    public WhenBuilder1<string, string> When(string arg1) => _ov1.When(arg1);
    public WhenBuilder2<string, FormatOptions, string> When(string arg1, FormatOptions arg2) => _ov2.When(arg1, arg2);
    public WhenBuilder3<string, FormatOptions, int, string> When(string arg1, FormatOptions arg2, int arg3) => _ov3.When(arg1, arg2, arg3);

    // Verify/Reset across all overloads
    public void Verify(Called times) { /* aggregate across _ov1, _ov2, _ov3 */ }
    public void Reset() { _ov1.Reset(); _ov2.Reset(); _ov3.Reset(); }
}
```

**Compared to today: 1 generated type with zero inner classes** vs **9-12 generated types with full behavioral logic.**

User API is unchanged:
```csharp
stub.Format.Return((input) => input.ToUpper());
stub.Format.Return((input, options) => options.Uppercase ? input.ToUpper() : input);
stub.Format.When("special").Return("SPECIAL");
```

---

## Source Delegation

Source delegation moves from a hardcoded call inside the generated `Invoke` method to a delegate set via `SetSourceFallback`:

```csharp
public void Source(ICalculator? source)
{
    Add.SetSourceFallback(source != null ? source.Add : null);
    Subtract.SetSourceFallback(source != null ? source.Subtract : null);
    Divide.SetSourceFallback(source != null ? source.Divide : null);
    Reset.SetSourceFallback(source != null ? source.Reset : null);
}
```

Method groups (e.g., `source.Add`) resolve directly to the interface method. No lambdas needed for most cases.

---

## User-Facing API Changes

### No change (what user writes is identical)

| API | Notes |
|---|---|
| `stub.Add.Return(42)` | |
| `stub.Add.Return((a, b) => a + b)` | User names their own lambda params |
| `stub.Add.Return(1, 2, 3)` | Params sequence |
| `stub.Add.When((a, b) => a > 0).Return(99)` | User names their own predicate params |
| `stub.Add.Verify(Called.Once)` | |
| `tracking.Verifiable()` | |
| `tracking.ThenReturn(10)` | |
| `stub.Reset.Call(() => counter++)` | |
| `stub.Source(realCalculator)` | |
| `stub.Strict = true` | |
| `stub.GetAsync.Return(key => 42)` | Simplified async |
| `stub.GetAsync.Return(async key => await f(key))` | Full async chain |
| Stub overrides (`protected override ...`) | |

### IntelliSense differences (what user sees, not writes)

| API | Before | After |
|---|---|---|
| `stub.Add.When(_, _)` | `When(int a, int b)` | `When(int arg1, int arg2)` |
| `stub.Add.Return(callback)` tooltip | `AddDelegate callback` | `Func<int, int, int> callback` |
| `.ThenWhen(_, _)` | `ThenWhen(int a, int b)` | `ThenWhen(int arg1, int arg2)` |

### Breaking changes

| API | Before | After |
|---|---|---|
| `tracking.LastArgs.Value.a` | Named tuple fields | `.Item1` (unnamed) |
| Explicit type `AddInterceptor` | Generated nested type | `MethodInterceptor2<int, int, int>` |

### Future enhancement (non-breaking, deferred)

Named `When`/`ThenWhen` parameters can be added later via thin generated subclasses (~4 lines per member). This is purely additive and does not break existing code.

---

## Edge Case Fallbacks

These cases continue using the current generated-class approach. They are all genuinely rare and already use inline mode today:

| Case | Why | Frequency |
|---|---|---|
| `ref`/`out` parameters | `Func<>` cannot express ref/out | Rare |
| `ref` returns | Need backing field in generated class | Rare |
| >8 parameters | No `MethodInterceptor9+` | Extremely rare |

**Not fallbacks** (handled by pre-compiled types):
- Async `Task<T>`/`ValueTask<T>` → `AsyncMethodInterceptorN`
- Async `Task`/`ValueTask` → `AsyncVoidMethodInterceptorN`
- Overloaded methods → thin compositor class
- Stub overrides → `SetFallback` delegate in constructor
- Source delegation → `SetSourceFallback` delegate
- Properties, indexers → concrete pre-compiled types

---

## Scope

### All 9 Patterns

| # | Pattern | Benefits? |
|---|---|---|
| 1 | Standalone `[KnockOff]` | Yes |
| 2 | Generic Standalone | Yes - generic T flows into field type params |
| 3 | Standalone Class `[KnockOffBase<T>]` | Yes |
| 4 | Generic Standalone Class | Yes |
| 5 | Inline Interface `[KnockOff<IFoo>]` | Yes |
| 6 | Inline Class `[KnockOff<ConcreteClass>]` | Yes |
| 7 | Inline Delegate | N/A - single Invoke, no per-member interceptors |
| 8 | Open Generic Interface | Yes |
| 9 | Open Generic Class | Yes |

### Member Types (3 of 4 In Scope)

- Methods (sync void, sync non-void, async void, async non-void) -- **in scope**
- Properties (get, set, get+set) -- **in scope**
- Indexers -- **in scope**
- Events -- **out of scope** (1 type per event, no inner classes, minimal ROI)

### All 4 Renderer Pipelines

All renderers change from emitting interceptor classes to emitting field declarations:

| Pipeline | Renderer |
|---|---|
| Standalone interface (1,2) | FlatRenderer |
| Standalone class (3,4) | StandaloneClassRenderer |
| Inline interface/class (5,6) | InlineRenderer / ClassRenderer |
| Open generic (8,9) | InlineRenderer |

---

## Implementation Phases

### Phase 1: Library Types

Create the pre-compiled type families in `src/KnockOff/Interceptors/`:

1. **Sync method types**: `MethodInterceptor0..8`, `VoidMethodInterceptor0..8`
2. **Async method types**: `AsyncMethodInterceptor0..8`, `AsyncVoidMethodInterceptor0..8`
3. **Property types**: Make existing base classes concrete with delegate fields
4. **Indexer types**: Make existing base class concrete with delegate fields
5. Each type includes pre-compiled inner classes: builders, sequences, WhenBuilder, WhenChain
6. `SetFallback` and `SetSourceFallback` methods on all types

### Phase 2: Generator Changes

Modify all 4 renderers to emit the new pattern:

1. **Field declarations** instead of interceptor class definitions
2. **Interface implementation one-liners** forwarding to `Invoke`
3. **Constructor generation** for stub override fallback wiring
4. **Source method** using `SetSourceFallback` with method groups/lambdas
5. **Async wrapping** in interface implementation lines (`Task.FromResult`, `new ValueTask(...)`)
6. **Overload compositor** generation for overloaded method groups
7. **Inline mode fallback** preserved for ref/out, ref returns, >8 params

### Phase 3: Verification

1. All Design project tests pass
2. All test projects compile and pass
3. Benchmark build time comparison vs v0.49.0

### Phase 4 (Future, Non-breaking): Named When Parameters

Optional thin generated subclasses for named `When`/`ThenWhen` parameters. Deferred - purely additive, does not break existing code.

---

## Estimated Impact

| Metric | Before | After |
|---|---|---|
| Generated types (test suite) | ~8,860 | ~200-500 (overload compositors + edge case fallbacks) |
| Generated lines (CalculatorStub) | 776 | ~60 |
| Library types added | 0 | ~40 |
| Types per non-overloaded method | 4 | 0 |
| Types per overloaded method group | 9-12 | 1 |
| Types per property | 1 | 0 |
| Types per indexer | 1 | 0 |

---

## Open Design Questions

1. **Builder return types**: What concrete type does `Return(callback)` return from the pre-compiled interceptor? Needs to support `Verifiable()`, `ThenReturn()`, `Verify(Called)`, and `LastArgs`. The pre-compiled inner class needs to handle `LastArgs` without named tuple fields.

2. **Overload compositor Verify semantics**: Today `stub.Format.Verify(Called.Exactly(3))` counts all overloads. The compositor needs to aggregate call counts across inner interceptor fields.

3. **Overload compositor and When chains**: Does `stub.Format.When("x").Return("y")` work when the compositor delegates to `_ov1.When("x")`? Need to verify the When chain flows correctly through the compositor.

4. **Generic method interceptors (`Of<T>()`)**: How do generic methods (stub.Create.Of<List<int>>()) work with the arity-based approach? These are currently excluded from base class mode. May need separate handling.

5. **Event interceptors**: Evaluate whether events need changes or already work as simple fields.

6. **ValueTask wrapping overhead**: `new ValueTask<T>(task)` wraps a `Task<T>` in a `ValueTask<T>`. For hot paths this adds a small allocation. Evaluate whether a separate ValueTask invoke path is needed.

---

## Architectural Verification

**Architect:** knockoff-architect
**Date:** 2026-02-14

### Codebase Deep-Dive

Files examined during verification:

**Library (src/KnockOff/Interceptors/):**
- `MethodInterceptorBase.cs` - Non-void method base: TDelegate/TArgs/TReturn generic, abstract InvokeDelegate/CreateValueDelegate, inner classes WhenMatcherBase/WhenBuilderBase/WhenChainBase/ReturnMethodCallBuilderBase/ReturnMethodSequenceBase
- `VoidMethodInterceptorBase.cs` - Void method base: TDelegate/TArgs generic, abstract InvokeVoidDelegate/RecordArgs/RecordUnconfiguredArgs, inner classes VoidWhenMatcherBase/VoidWhenChainBase/MethodCallBuilderBase/MethodSequenceBase
- `PropertyGetInterceptorBase.cs` - Abstract, requires InvokeGetUnconfigured override
- `PropertySetInterceptorBase.cs` - Abstract, requires InvokeSetUnconfigured override
- `PropertyGetSetInterceptorBase.cs` - Extends PropertyGetInterceptorBase, abstract InvokeSetUnconfigured/InvokeGetUnconfiguredFinal
- `IndexerGetSetInterceptorBase.cs` - Abstract, requires InvokeGetUnconfigured/InvokeSetUnconfigured overrides

**Generator (src/Generator/):**
- `Renderer/FlatRenderer.cs` - Standalone interface pipeline (patterns 1,2)
- `Renderer/StandaloneClassRenderer.cs` - Standalone class pipeline (patterns 3,4)
- `Renderer/InlineRenderer.cs` - Inline pipeline (patterns 5,6,8,9)
- `Renderer/ClassRenderer.cs` - Inline class pipeline (pattern 6)
- `Renderer/Shared/MethodInterceptorRenderer.cs` - Shared method interceptor generation (3 modes: SingleSignature, BaseClass, OverloadGroup)
- `Renderer/Shared/PropertyInterceptorRenderer.cs` - Shared property interceptor generation
- `Renderer/Shared/IndexerInterceptorRenderer.cs` - Shared indexer interceptor generation
- `Builder/FlatModelBuilder.cs` - Builds FlatGenerationUnit models
- `Builder/StandaloneClassModelBuilder.cs` - Builds StandaloneClassGenerationUnit models
- `Builder/InlineModelBuilder.cs` - Builds inline stub models
- `Builder/ClassModelBuilder.cs` - Builds inline class stub models

**Key interfaces (src/KnockOff/):**
- `IMethodTracking.cs` - IMethodTracking, IMethodTracking<TArg>, IMethodTrackingArgs<TArgs>
- `IMethodReturnBuilder.cs` - IMethodReturnBuilder<TCallback>, IMethodReturnBuilderArgs<TCallback, TArgs>

**Design projects (src/Design/):**
- Design.Stubs, Design.Domain, Design.Tests across all member types and patterns

### Findings

#### 1. Current Base Class Architecture Partially Solved This

The v0.49.0 work already moved method interceptors to a base class mode (`MethodInterceptorBase<TDelegate, TArgs, TReturn>`). The generated code inherits from the base and only generates:
- Abstract method overrides (InvokeDelegate, CreateValueDelegate, RecordArgs, RecordUnconfiguredArgs)
- Entry points (Return/Call, When) that call base helpers
- Thin inner classes (MethodCallBuilderImpl, WhenBuilder, WhenChain) that subclass base inner classes

**However**, each method still generates 4 types (the sealed interceptor class + 3 inner classes). The arity-based approach eliminates all of them by moving the arity-specific logic (delegate unpacking, arg recording) into the library types directly.

The key architectural insight is correct: the remaining generated types are thin wrappers that differ only in how they pack/unpack arguments to/from `TArgs` and `TDelegate`. If we parameterize by individual argument types instead of a tuple, we can pre-compile all logic.

#### 2. Existing Base Classes Cannot Be Extended -- Must Be Replaced

The current base classes use `TDelegate` (a custom delegate type per method) and `TArgs` (a named tuple). These are the wrong abstractions for pre-compiled types because:
- `TDelegate` requires generating a custom delegate per method (e.g., `AddDelegate`)
- `TArgs` uses named tuples for named field access, which cannot be pre-compiled generically
- `Func<T1, ..., TN, TReturn>` is the correct delegate abstraction for pre-compiled types
- Individual `T1, ..., TN` type parameters replace the collapsed `TArgs`

**Recommendation:** The pre-compiled `MethodInterceptorN` types should NOT inherit from `MethodInterceptorBase<TDelegate, TArgs, TReturn>`. They should be independent concrete types that replicate the behavioral logic but use `Func<>` / `Action<>` and individual type parameters. The existing base classes remain for the fallback mode (ref/out, >8 params).

#### 3. `LastArgs` Breaking Change is Wider Than Documented

The plan identifies `tracking.LastArgs.Value.a` becoming `.Item1` as a breaking change. However, the impact is broader:

**On the interceptor itself:** `stub.Add.LastArgs` currently returns a nullable named tuple `(int a, int b)?`. With pre-compiled types, it would return `(T1, T2)?` (unnamed). This affects:
- Direct interceptor `LastArgs` access: `stub.Add.LastArgs.Value.a` breaks
- Builder `tracking.LastArgs` access: `tracking.LastArgs.a` breaks (this uses `IMethodTrackingArgs<(int a, int b)>`)

**Scope of breakage found in tests:**
- `src/Tests/KnockOffTests/DelegateValueOverloadTests.cs` lines 148-149: `stub.Interceptor.LastArgs.Value.name`
- `src/Tests/KnockOffTests/InlineStubTests.cs` lines 388-389, 836-837: `stub.Interceptor.LastArgs.Value.name`, `stub.Format.LastArgs.Value.input`
- `src/Tests/KnockOff.Documentation.Samples/`: multiple files use named tuple destructuring on tracking.LastArgs
- `src/Design/Design.Tests/GenericOverloadTests/GenericStandaloneOverloadTests.cs` line 189: `var (item, uppercase, maxLength) = tracking.LastArgs`

**Decision:** Accept unnamed tuples (`Item1`, `Item2`, etc.) as a known breaking change. Destructuring via `var (a, b) = tracking.LastArgs` continues to work identically. Only direct `.fieldName` access breaks and can be search/replaced in tests. Named tuples can be restored in a future enhancement via thin generated properties (zero new types, one property per multi-param method).

#### 4. `tracking.LastArgs` on Builders Is a Separate Problem

Currently, `Return(callback)` returns a `MethodCallBuilderImpl` that implements `IMethodReturnBuilderArgs<TCallback, TArgs>` where `TArgs` is a named tuple. This provides `tracking.LastArgs` with named fields.

With pre-compiled types, the builder inner class cannot expose named tuple fields either. The builder needs to be generic too: `MethodCallBuilder2<T1, T2, TReturn>`. Its `LastArgs` would return `(T1, T2)?` instead of `(int a, int b)?`.

**Decision:** Same as Finding 3 -- accept unnamed tuples. Both interceptor-level and builder-level `LastArgs` use unnamed tuple fields. This is a consistent breaking change across the entire API surface.

#### 5. Async Method Interceptors: Separate Types Required

The plan proposes separate `AsyncMethodInterceptorN` and `AsyncVoidMethodInterceptorN` type families. This is the correct approach. Four type families are required.

**Why unified types do NOT work:** A unified `MethodInterceptorN` where `TReturn` can be `Task<T>` would break the simplified async API. For `Task<int> GetAsync(string key)`, the simplified API allows:
```csharp
stub.GetAsync.Return(key => 42);   // Simplified - auto-wraps in Task.FromResult
stub.GetAsync.Return(42);           // Value - auto-wraps in Task.FromResult
```

With unified `MethodInterceptor1<string, Task<int>>`, `Return(Func<string, Task<int>>)` requires the lambda to return `Task<int>`, not `int`. And `Return(Task<int> value)` requires `Task.FromResult(42)`, not `42`. The simplified callbacks would NOT compile.

The simplified async API is a deliberate design decision exercised across all 9 patterns (see `src/Design/Design.Stubs/Methods/AsyncConsistency.cs`). It is not optional convenience -- it is a core usability feature.

**Decision:** Keep all 4 type families as the plan proposes:
1. `MethodInterceptor0..8` (sync non-void) -- 9 types
2. `VoidMethodInterceptor0..8` (sync void) -- 9 types
3. `AsyncMethodInterceptor0..8` (async non-void: `Task<T>`, `ValueTask<T>`) -- 9 types
4. `AsyncVoidMethodInterceptor0..8` (async void: `Task`, `ValueTask`) -- 9 types

Total method interceptor types: ~36. Plus property/indexer types: ~4. Grand total: ~40 library types.

The ~40 types are compiled once in the KnockOff library and reused by all consumer projects. This is a small, fixed cost compared to eliminating thousands of generated types per consumer project.

#### 6. Generic Methods (`Of<T>()`) Cannot Use Pre-compiled Types

Generic method handlers (`stub.Create.Of<List<int>>()`) are fundamentally different from arity-based interceptors. They use runtime type dictionaries to store per-type-argument interceptors. The current architecture generates:
- A `CreateInterceptor` class with a `Dictionary<Type, object>`
- Nested `TypedHandler<T>` classes
- `Of<T>()` method that lazily creates typed handlers

These handlers already have minimal generated code per handler. The arity-based approach does NOT apply to generic methods because the type parameters are open -- they cannot be collapsed into `Func<>` signatures at compile time.

**Recommendation:** Generic method handlers continue using the current generated-class approach. This is consistent with the plan's statement but should be explicitly documented as a non-goal. Generic handlers are rare and their type count is bounded by usage.

#### 7. Event Interceptors Already Have Minimal Type Overhead -- OUT OF SCOPE

Looking at `FlatRenderer.RenderEventInterceptorClass()` (line 1246), event interceptors generate a single sealed class with:
- `_handler` field
- `RecordAdd` / `RecordRemove`
- `Raise` method
- `Verify` / `VerifyAdd` / `VerifyRemove`
- `Reset`
- `Verifiable` / `CheckVerification` / `CheckVerificationAll`

There are NO inner classes. Events generate exactly 1 type per event member.

**Decision:** Events are explicitly **out of scope** for this feature. The ROI is minimal -- only 1 type per event with no inner classes. Events continue using the current generated-class approach. Converting them to pre-compiled types (via `EventInterceptor<TDelegate>` or arity-based `EventInterceptor0`, etc.) is a future optimization that can be pursued independently.

#### 8. Property/Indexer Interceptors Require Careful Concretization

The current property base classes (`PropertyGetInterceptorBase`, etc.) are abstract with abstract methods like `InvokeGetUnconfigured(bool strict)`. These are overridden in generated code to handle:
- Source delegation (calling `_source.PropertyName`)
- Strict mode (throwing `StubException`)
- Default return (`default!`)

To make these concrete (non-abstract), the default behavior must be:
```csharp
protected virtual TValue InvokeGetUnconfigured(bool strict)
{
    if (_sourceFallback != null) return _sourceFallback();
    if (strict) throw StubException.Unconfigured(_memberName);
    return default!;
}
```

This is straightforward. The `SetSourceFallback(Func<TValue>?)` method would set a `_sourceFallback` delegate. The generated stub's `Source()` method would wire it:
```csharp
PropertyName.SetSourceFallback(source != null ? () => source.PropertyName : null);
```

The `IndexerGetSetInterceptorBase<TKey, TValue>` follows the same pattern. Its abstract `InvokeGetUnconfigured(bool strict, TKey key)` and `InvokeSetUnconfigured(bool strict, TKey key, TValue value)` become virtual with delegate-based fallbacks.

**Recommendation:** This is sound as proposed. No architectural concerns.

#### 9. Overload Compositor: Ambiguity Problem with Return(value)

The overload compositor delegates `Return(Func<...>)` calls to inner interceptors. The compiler resolves which overload to call based on the lambda parameter count. This works for callbacks.

But `Return(TReturn value)` is problematic when multiple overloads share the same return type. For example, if `Format(string)` and `Format(string, FormatOptions)` both return `string`:
```csharp
stub.Format.Return("hello"); // Which overload does this target?
```

Currently, the generated interceptor class has a single `Return(string value)` method because it handles one signature. With the compositor, there are two interceptors that both accept `Return(string value)`.

**Resolution:** The compositor should NOT expose `Return(value)` -- only `Return(callback)` and per-overload access:
```csharp
stub.Format.Return((input) => "hello");  // Resolves to 1-param overload
stub.Format._ov1.Return("hello");         // Direct access for value return (internal)
```

Or the compositor can expose value return only when there is a single overload, which is already the non-compositor case. The compositor is only generated for actual overloads, and in that case, `Return(value)` is inherently ambiguous.

**Current behavior:** Today, overloaded methods generate per-signature `Return(callback)` methods that resolve by delegate arity. `Return(value)` is NOT generated on the overload group -- it only exists on individual overloads. This behavior should be preserved.

#### 10. Source Delegation with Method Groups: Type Inference Concern

The plan proposes:
```csharp
Add.SetSourceFallback(source != null ? source.Add : null);
```

Where `source.Add` is a method group. Method group to `Func<int, int, int>` conversion works in C#, but the ternary expression `source != null ? source.Add : null` requires the compiler to infer the delegate type. This should work because `SetSourceFallback` has a specific `Func<T1, ..., TN, TReturn>?` parameter type, giving the compiler enough context for method group conversion.

**However**, for void methods, method groups do NOT convert to `Action<>` in all contexts. The ternary `source != null ? source.Reset : null` requires inference of `Action`. This works in C# 10+ with natural type for method groups.

**Recommendation:** Since KnockOff targets net8.0+, method group to delegate conversion in ternary expressions is supported. No issue.

### Open Design Question Resolutions

**Q1: Builder return types**

The pre-compiled inner class `MethodCallBuilderN<T1, ..., TN, TReturn>` can implement `IMethodReturnBuilderArgs<Func<T1,...,TN,TReturn>, (T1, T2, ..., TN)>`. The `TArgs` of the tracking interface becomes an unnamed tuple. The `LastArgs` property returns `(T1, T2, ..., TN)?`.

For named field access on `LastArgs`, see Finding 3 above -- this is a breaking change. The simplest fix is to generate a `LastArgs` property on the stub interceptor field that returns a named tuple. No new types needed.

**Q2: Overload compositor Verify semantics**

The compositor's `Verify(Called times)` method should sum call counts across all inner interceptors:
```csharp
public void Verify(Called times)
{
    var total = _ov1.TotalCallCount + _ov2.TotalCallCount + _ov3.TotalCallCount;
    if (!times.Validate(total)) throw new VerificationException(...);
}
```

The pre-compiled interceptors need to expose a `TotalCallCount` property (or the existing `UnconfiguredCallCount` + tracked counts). The `VoidMethodInterceptorBase` already has a `protected TotalCallCount` -- this should be made `public` or exposed via an interface.

**Q3: Overload compositor and When chains**

When chains work through the compositor because `When(arg1)` delegates to `_ov1.When(arg1)` which returns a `WhenBuilder1<T1, TReturn>`. The chain flows through the pre-compiled type's When infrastructure without any generated code. The compositor is just a routing layer.

**Q4: Generic method interceptors (`Of<T>()`)**

These remain generated types (see Finding 6). The `Of<T>()` pattern requires a dictionary-based approach that cannot be pre-compiled into a fixed set of types. This is correctly identified in the plan as not a fallback -- it is an orthogonal feature.

**Q5: Event interceptors**

**Decision:** Events are explicitly **out of scope**. They generate only 1 type per event with NO inner classes (see Finding 7). The ROI of converting them is minimal compared to methods (4 types each). Events continue using the current generated-class approach. Converting them to pre-compiled types is a future optimization.

**Q6: ValueTask wrapping overhead**

`new ValueTask<T>(Task.FromResult(value))` allocates. For tests, this is negligible. For production use of KnockOff stubs (e.g., in integration tests with hot paths), the overhead exists but is tiny compared to the test infrastructure cost.

**Decision:** The separate `AsyncMethodInterceptorN` types internally return `Task<TReturn>`. The generator wraps in the interface implementation:
```csharp
// Task<T> method:
Task<int> IService.GetAsync(string key) => GetAsync.Invoke(Strict, key);

// ValueTask<T> method - wraps the Task:
ValueTask<int> IService.ComputeAsync(int x) => new(ComputeAsync.Invoke(Strict, x));
```

No separate ValueTask interceptor types needed. The allocation from `new ValueTask<T>(task)` is negligible for test usage.

### Scope Table Verification

| # | Pattern | Methods | Properties | Indexers | Events | Notes |
|---|---|---|---|---|---|---|
| 1 | Standalone | Yes | Yes | Yes | Out of scope | FlatRenderer: methods/properties/indexers affected |
| 2 | Generic Standalone | Yes | Yes | Yes | Out of scope | Same pipeline as #1, generic T flows to field type params |
| 3 | Standalone Class | Yes | Yes | Yes | Out of scope | StandaloneClassRenderer: separate pipeline, same changes needed |
| 4 | Generic Standalone Class | Yes | Yes | Yes | Out of scope | Same pipeline as #3 |
| 5 | Inline Interface | Yes | Yes | Yes | Out of scope | InlineRenderer: separate pipeline |
| 6 | Inline Class | Yes | Yes | Yes | Out of scope | ClassRenderer: separate pipeline, uses .Object |
| 7 | Inline Delegate | N/A | N/A | N/A | N/A | Single Invoke, no per-member interceptors |
| 8 | Open Generic Interface | Yes | Yes | Yes | Out of scope | Uses InlineRenderer |
| 9 | Open Generic Class | Yes | Yes | Yes | Out of scope | Uses InlineRenderer, uses .Object |

**Pipeline change summary:**

| Pipeline | Renderer File | Lines | Scope of Change |
|---|---|---|---|
| Standalone interface (1,2) | `FlatRenderer.cs` | ~2000 | Major rewrite of interceptor class emission, Source methods, interface implementations |
| Standalone class (3,4) | `StandaloneClassRenderer.cs` | ~600 | Major rewrite, parallel to FlatRenderer changes |
| Inline interface (5,8) | `InlineRenderer.cs` | ~900 | Major rewrite within nested class structure |
| Inline class (6,9) | `ClassRenderer.cs` | ~600 | Major rewrite, .Object wrapping considerations |
| Shared renderer | `MethodInterceptorRenderer.cs` | ~2000 | Largely bypassed for new code path, retained for fallback mode |

### Breaking Changes Assessment

| Change | Severity | Scope | Mitigation |
|---|---|---|---|
| `LastArgs.Value.fieldName` loses named fields | Medium | Users accessing multi-param LastArgs by field name | **Decided:** Accept as breaking. Destructuring (`var (a, b) = ...`) still works. Named tuples deferred to future enhancement. |
| Generated type names gone (e.g., `AddInterceptor`) | Low | Users explicitly referencing generated interceptor type names | Rare -- users typically use `var` or `stub.Add.Return(...)` |
| `tracking.LastArgs.fieldName` loses named fields | Medium | Users accessing builder tracking LastArgs by name | **Decided:** Accept as breaking. Same as above. |
| Interceptor property type changes (from nested class to generic) | Low | Users storing interceptor references in typed variables | Rare -- typically accessed inline |
| `Return(callback)` return type changes from `MethodCallBuilderImpl` to `MethodCallBuilder2<...>` | Low | Users storing builder references in concrete type | Rare -- typically used via `var` |

### Risks

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Pre-compiled types too large for NuGet package | Low | Medium | Monitor library size; types are small per-class, just many of them |
| C# compiler struggles with deeply nested generic types | Low | High | Test with complex stubs early (8-param methods, overloads, generics) |
| Overload compositor ambiguity with same-type overloads | Medium | Medium | Compositor only exposes Return(callback), not Return(value) for overloads |
| Method group conversion fails in some edge cases | Low | Low | Fall back to lambda syntax in Source() method |
| Some tests rely on generated type names | Medium | Low | Search all test files for interceptor class name references before implementation |
| Performance regression from virtual dispatch through Func<> | Low | Low | Benchmark Invoke hot path; Func<> invocation is already fast |

### Design Project Verification

**Status:** Deferred to implementation phase.

The current plan is a pure design document. Design.Stubs verification for pre-compiled types cannot be performed until Phase 1 (library types) is implemented. Once the library types exist, Design.Stubs code should be written to exercise every pattern+member combination.

**Acceptance criteria for Design.Stubs (to be written during implementation):**
- Each of the 8 applicable patterns has at least one stub using pre-compiled interceptor fields
- Methods: sync void, sync non-void, async Task<T>, async ValueTask<T>, async Task, async ValueTask
- Properties: get-only, set-only, get+set
- Indexers: get+set
- Overloaded methods: compositor pattern
- Stub overrides: SetFallback wiring
- Source delegation: SetSourceFallback wiring
- Generic methods: Of<T>() pattern (unchanged, continues to generate types)
- Events: unchanged (out of scope, continue using current generated-class approach)

### Test Migration (In-Scope)

The following test modifications are **pre-approved as in-scope** for this feature. The developer does not need to STOP and ASK for these categories:

**LastArgs named tuple field access (~21 locations):**
- `tracking.LastArgs.fieldName` becomes `tracking.LastArgs.Item1` or destructured via `var (name, value) = tracking.LastArgs`
- `stub.Interceptor.LastArgs.Value.fieldName` becomes `stub.Interceptor.LastArgs.Value.Item1` or destructured
- Locations identified:
  - `src/Tests/KnockOffTests/DelegateValueOverloadTests.cs` (~2 locations)
  - `src/Tests/KnockOffTests/InlineStubTests.cs` (~4 locations)
  - `src/Tests/KnockOffTests/BasicTests.cs` (~3 locations)
  - `src/Tests/KnockOff.Documentation.Samples/` (~5 locations)
  - `src/Design/Design.Tests/` (~7 locations)

**Generated type name references:**
- Any test referencing generated interceptor class names (e.g., `AddInterceptor`) may need updating. Only ~1 location found (`RocksGapReproductionTests.cs`).

**Design.Stubs and Design.Tests:**
- These files will need significant updates since the generated code shape changes completely. All modifications are in-scope.

**Documentation samples:**
- Documentation samples using named tuple destructuring will need updating AND markdown docs will need regeneration via `dotnet mdsnippets`.

**Out-of-scope test modifications (STOP and ASK):**
- Any test failure NOT related to LastArgs naming, generated type names, or the structural change from generated classes to pre-compiled fields
- Any test that exercises event behavior (events are out of scope for this feature)

### Architectural Recommendations

1. **Keep all 4 type families (sync, void, async, async-void).** Separate `AsyncMethodInterceptorN` types are required to preserve the simplified async API (`Return(key => 42)` for `Task<int>` methods). The ~40 library types are compiled once and eliminate thousands of generated types per consumer.

2. **Retain existing base classes for fallback mode.** ref/out, ref returns, >8 params continue to use the current generated-class approach with MethodInterceptorBase/VoidMethodInterceptorBase.

3. **Accept unnamed tuples for LastArgs (decided).** `tracking.LastArgs.fieldName` becomes `tracking.LastArgs.Item1` or destructured via `var (a, b) = tracking.LastArgs`. This is a known breaking change. Named tuples can be restored later via thin generated properties (zero new types) in a follow-up enhancement. Test modifications for ~21 LastArgs locations are pre-approved as in-scope.

4. **Implement Phase 1 (library types) first, then Phase 2 (generator changes).** The library types can be validated independently with hand-written test code before the generator is modified. This reduces risk significantly.

5. **Use a feature flag or compilation constant to gate the new code path.** During development, both old and new generators can coexist. Tests can be run against both to verify behavioral equivalence.

6. **Events are explicitly out of scope.** Events generate only 1 type per event with no inner classes -- minimal ROI for conversion. They continue using the current generated-class approach. Converting them to pre-compiled types is a future optimization.

7. **The ~40 library types estimate is accurate.** 36 method interceptor types (4 families x 9 arities) + 3 property types + 1 indexer type = ~40. Inner classes (builders, sequences, When chains) are pre-compiled within each type and do not add to the "type count from the consumer project's perspective" but they do add to the KnockOff library's type count. This is acceptable since they are compiled once.

### Fallback Mode Decision Tree

The renderer must decide for each method member whether to emit a pre-compiled field declaration or fall back to the current generated-class approach. This decision replaces and extends the existing `useBaseClass` logic in `MethodInterceptorRenderer.cs` (lines 31-39).

**Current decision logic** (for reference):
```csharp
var useBaseClass = false;
if (model.Overloads.Count == 0)
{
    var hasRefOrOut = model.Parameters.Any(p => p.RefKind == RefKind.Ref || p.RefKind == RefKind.Out);
    var (_, isAsyncTaskT, isAsyncValueTaskT) = GetAsyncTypeInfo(model.ReturnType);
    var isAsyncWithInnerType = isAsyncTaskT || isAsyncValueTaskT;
    var (isVoidTask, isVoidValueTask) = GetVoidAsyncInfo(model.ReturnType);
    var isVoidAsync = isVoidTask || isVoidValueTask;
    useBaseClass = !hasRefOrOut && !isAsyncWithInnerType && !isVoidAsync && !model.IsRefReturn;
}
```

Note: The current `useBaseClass` excludes async methods because the existing base classes do not support simplified async APIs. The new pre-compiled types DO support async via separate `AsyncMethodInterceptorN` families, so async is no longer a disqualifier.

**New decision tree for pre-compiled vs. fallback:**

```
For each method member:
  1. Is this part of an overload group?
     -> YES: Use pre-compiled fields inside a thin compositor class
             (each overload evaluated individually for pre-compiled eligibility)
     -> NO: Continue to step 2

  2. Does the method have ref or out parameters?
     -> YES: FALLBACK to generated class (Func<> cannot express ref/out)
     -> NO: Continue to step 3

  3. Does the method have a ref return type?
     -> YES: FALLBACK to generated class (needs backing field)
     -> NO: Continue to step 4

  4. Does the method have > 8 parameters?
     -> YES: FALLBACK to generated class (no MethodInterceptor9+)
     -> NO: Continue to step 5

  5. Determine the type family:
     a. void return type
        -> VoidMethodInterceptorN<T1, ..., TN>
     b. Task/ValueTask return (no inner type)
        -> AsyncVoidMethodInterceptorN<T1, ..., TN>
     c. Task<T>/ValueTask<T> return
        -> AsyncMethodInterceptorN<T1, ..., TN, TReturn>
     d. Non-void, non-async return
        -> MethodInterceptorN<T1, ..., TN, TReturn>

  6. Emit pre-compiled field declaration
```

**For properties:** Always use pre-compiled concrete types (`PropertyGetInterceptor<TValue>`, etc.). No fallback needed -- properties have no ref/out or arity concerns.

**For indexers:** Always use pre-compiled concrete type (`IndexerGetSetInterceptor<TKey, TValue>`). No fallback needed.

**For events:** Out of scope. Continue using current generated-class approach.

**Where the decision lives:** The decision should be made at the renderer level (not the model builder). The model already contains all information needed (parameter count, ref kinds, return type). The renderer checks eligibility and emits either a field declaration (pre-compiled path) or a class definition (fallback path, using existing `MethodInterceptorRenderer` logic).

This means `MethodInterceptorRenderer.cs` gains a fourth mode:
1. **PreCompiled** (new) -- emit field declaration of pre-compiled type
2. **BaseClass** (existing, retained for fallback) -- emit class extending base
3. **SingleSignature** (existing, retained for fallback) -- emit standalone class
4. **OverloadGroup** (existing, partially retained) -- compositor delegates to pre-compiled fields, but individual overloads that need fallback use generated classes

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-02-14 (Re-reviewed after architect addressed all 5 concerns)

### Concern Resolution Verification

**Concern 1 (Unified Async - High):** RESOLVED. Finding 5 now explicitly states "Separate Types Required" and explains why unified types break the simplified async API. Recommendation 1 confirms 4 type families. The plan body, findings, and todo are now consistent.

**Concern 2 (LastArgs - Medium):** RESOLVED. Finding 3 has an explicit "Decision: Accept unnamed tuples" paragraph. Finding 4 confirms the same for builder-level LastArgs. Breaking changes table updated with "Decided" markers.

**Concern 3 (Test Impact - Medium):** RESOLVED. New "Test Migration (In-Scope)" section (lines 797-822) lists ~21 LastArgs locations, generated type name references, Design.Stubs/Design.Tests, and documentation samples as pre-approved in-scope modifications. Out-of-scope test modifications are defined.

**Concern 4 (Events - Low-Medium):** RESOLVED. Events are explicitly marked "Out of Scope" throughout the plan (header, scope table, Q5 resolution, finding 7, recommendation 6).

**Concern 5 (Fallback Decision Tree - Medium):** RESOLVED. New section (lines 840-907) provides complete pseudocode decision tree, covers properties/indexers/events, specifies the decision happens at renderer level, and maps to existing `useBaseClass` logic.

**Minor inconsistency noted:** The todo file (line 173) still lists "Events" under member types without the "out of scope" qualifier. This should be updated during implementation to match the plan.

### Why This Plan Is Approved

All five concerns have been addressed with concrete decisions, not vague recommendations. The plan is now internally consistent: the plan body, architectural verification, findings, recommendations, and scope table all agree on the same set of decisions. The critical async type family question has a clear answer backed by code evidence. The fallback mode decision tree is implementable. The test migration scope is defined with enough specificity to prevent unnecessary STOP-and-ASK cycles during implementation.

The plan remains ambitious (rewriting all 4 renderer pipelines and creating ~40 library types), but Phase 1/Phase 2 separation provides a natural checkpoint. The feature flag recommendation enables incremental development.

### Review Summary

- Files examined: 14 source files across library, generator, design stubs, and test projects
- Questions checked: 16 of 16 (all checklist items)
- Devil's advocate items: 8 generated (from original review), all either addressed in plan or noted as implementation-time decisions
- Design.Stubs verification: Deferred (acceptable -- library types do not exist yet)

### My Understanding of This Plan

**Core Change:** Replace per-member generated interceptor classes (4 types per method) with fields of pre-compiled generic library types parameterized by arity (`MethodInterceptor2<int, int, int>`), reducing generated type count from ~8,860 to ~200-500.

**User-Facing API:** Nearly identical. Users write `stub.Add.Return(42)`, `stub.Add.When(1, 2).Return(3)`, etc. Breaking changes: `LastArgs` named tuple fields become unnamed (`Item1`/`Item2`), and explicit interceptor type names change from `AddInterceptor` to `MethodInterceptor2<int, int, int>`.

**Internal Changes:** (1) Create ~20-40 pre-compiled library types in `src/KnockOff/Interceptors/`. (2) Rewrite all 4 renderer pipelines (FlatRenderer, StandaloneClassRenderer, InlineRenderer, ClassRenderer) to emit field declarations instead of interceptor class definitions. (3) Retain fallback for ref/out, ref returns, >8 params.

**Patterns Affected:** All 9 patterns (pattern 7/delegate is N/A). All 4 member types. All 4 renderer pipelines.

### Codebase Investigation

**Files Examined:**
- `src/KnockOff/Interceptors/MethodInterceptorBase.cs` - Confirmed: uses `TDelegate` (custom delegate) and `TArgs` (named tuple) as generic params. These cannot be pre-compiled because each method generates its own delegate type and tuple shape. The architect is correct that the pre-compiled types must be new, not extensions of these.
- `src/KnockOff/Interceptors/VoidMethodInterceptorBase.cs` - Same pattern. Abstract `RecordArgs`, `InvokeVoidDelegate`, `RecordUnconfiguredArgs` all depend on the TArgs/TDelegate abstraction.
- `src/KnockOff/Interceptors/PropertyGetInterceptorBase.cs` - Abstract class with `abstract TValue InvokeGetUnconfigured(bool strict)`. Making concrete with delegate-based fallback is feasible as architect described.
- `src/KnockOff/Interceptors/PropertyGetSetInterceptorBase.cs` - Extends PropertyGetInterceptorBase. Also abstract. Concretization plan is sound.
- `src/KnockOff/Interceptors/IndexerGetSetInterceptorBase.cs` - Abstract with `InvokeGetUnconfigured(bool strict, TKey key)` and `InvokeSetUnconfigured(bool strict, TKey key, TValue value)`. Concretization plan is sound.
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` - Confirmed three rendering modes: BaseClass, SingleSignature, OverloadGroup. Async methods use SingleSignature mode (not BaseClass) due to simplified callback handling. This confirms async is already special-cased in the generator.
- `src/Design/Design.Stubs/Generated/.../CalculatorStub.g.cs` - Confirmed generated interceptor structure: sealed class with custom delegate, named tuple TArgs, inner MethodCallBuilderImpl/WhenBuilder/WhenChain classes.
- `src/Design/Design.Stubs/Generated/.../AsyncRepositoryStub~1.g.cs` - Confirmed async interceptor is NOT using base class mode. Has THREE Return overloads: full delegate, inner value, simplified callback.
- `src/Design/Design.Stubs/Methods/AsyncConsistency.cs` - Confirmed the simplified async API is used across all 9 patterns.
- `src/Design/Design.Stubs/Methods/MethodOverloads.cs` - Confirmed overloads do NOT have `Return(value)` on the interceptor. Only `Return(callback)` resolved by lambda arity.
- `src/KnockOff/IMethodTracking.cs` - Confirmed `IMethodTrackingArgs<TArgs>` exposes `LastArgs` with named tuple. Pre-compiled types would need to implement this with unnamed tuples.
- `src/KnockOff/IMethodReturnBuilder.cs` - Confirmed `IMethodReturnBuilderArgs<TCallback, TArgs>` extends `IMethodTrackingArgs<TArgs>`. The builder must implement this interface.
- `src/Tests/KnockOffTests/BasicTests.cs:76-88` - Confirmed tests use `tracking.LastArgs.name`, `tracking.LastArgs.value`, `tracking.LastArgs.flag` (named tuple destructuring).
- `src/Tests/KnockOff.Documentation.Samples/ReadmeComparisonSamples.cs:448` - Confirmed documentation samples use `var (a, b) = tracking.LastArgs` (named tuple destructuring).

**Searches Performed:**
- `LastArgs.Value.\w+` in `src/` - Found 6 usages in tests using named tuple field access on interceptor-level LastArgs (e.g., `stub.Interceptor.LastArgs.Value.name`)
- `tracking.LastArgs.` in `src/Tests/` - Found 15 usages in tests and documentation samples using named tuple destructuring on builder-level LastArgs
- `tracking.LastArgs` in `src/Design/` - Found 0 usages in Design files (Design.Stubs uses the API but Design.Tests may use different patterns)
- `bool Strict` in `src/` - Confirmed Strict is a property on the generated stub, passed to `Invoke(bool strict, ...)` calls
- `Interceptor.*class` in `src/Tests/` - Only 1 test file references generated interceptor class names directly

**Design.Stubs Verification:**
- The architect explicitly states "Deferred to implementation phase" for Design.Stubs verification (plan line 777). This is understandable since the library types do not exist yet and there is nothing to compile against. However, this means scope claims in the plan are NOT backed by compilation evidence. I am noting this but NOT rejecting the plan on this basis because it is a greenfield library design -- the types literally do not exist yet. The acceptance criteria listed (plan lines 781-791) are reasonable as post-implementation verification targets.

**Discrepancies Found:**
1. The todo (line 146-147) lists `AsyncMethodInterceptor` and `AsyncVoidMethodInterceptor` types, but the architect's recommendation (plan line 794) says to NOT create separate async types. The todo and plan contradict each other on this point.
2. The plan says async handling is "handled by pre-compiled types" (plan line 386), but the architect says to use unified `MethodInterceptorN` where `TReturn` can be `Task<T>`. These are incompatible visions -- the plan section proposes 4 type families, the architect proposes 2.

### Structured Question Checklist

**Completeness Questions:**
- [x] Are all nine patterns addressed? Yes. Pattern 7 (delegate) correctly marked N/A. Patterns 1-6, 8-9 all marked "Yes" with specific pipeline mapping.
- [x] What happens with null, empty, or default values? Not explicitly addressed. `MethodInterceptor0<TReturn>` with `TReturn` being a nullable reference type -- does `Return(null)` work? Answer: should work via `Return(TReturn value)` since TReturn accepts null.
- [x] What happens with generic type parameters? Plan says "generic T flows into field type params" (line 398). Confirmed feasible: `MethodInterceptor1<T, string>` where T is the stub's generic parameter.
- [x] What happens with nested types or inherited members? Not explicitly addressed. Inherited interface members should work since the generator already flattens them. No concern.
- [x] How does this interact with existing features? Verification, sequences, When chains, stub overrides, source delegation all explicitly addressed.

**Correctness Questions:**
- [x] Do the generated code examples compile? The "After" examples (plan lines 186-228) look plausible but cannot be verified until library types exist. CONCERN: the `Source()` method uses `source.Add` as a method group in a ternary -- this should work with C# 10+ natural method group types, but see Concern #4.
- [ ] Is the proposed implementation consistent with existing patterns? CONCERN: The architect's unified async proposal is inconsistent with the current user API. See Concern #1.
- [x] Are model/builder/renderer responsibilities correctly assigned? Yes. Library types hold all behavioral logic; renderers emit field declarations and interface forwarders.
- [x] Breaking changes -- migration path clear? CONCERN: migration path for `LastArgs` is vague ("accept as breaking" or "generate thin property" or "Phase 4"). No decision made. See Concern #2.

**Clarity Questions:**
- [ ] Could I implement this without asking clarifying questions? NO. Several open decisions prevent implementation. See Concerns below.
- [ ] Ambiguous requirements? YES. The async type family decision is unresolved (plan proposes 4 families, architect says 2).
- [ ] Edge cases explicitly handled? Mostly yes, but events are vague ("Evaluate whether they need changes" on plan line 120).
- [ ] Test strategy specific enough? No. "All Design project tests pass" and "All test projects compile and pass" is the entirety of the test strategy. No mention of which existing tests will break and how to update them.

**Risk Questions:**
- [x] What could go wrong? Identified: overload compositor ambiguity (addressed), method group conversion (addressed), performance (addressed).
- [ ] Which existing tests might fail? CONCERN: Not analyzed. See Concern #3.
- [x] Performance implications? Addressed (Func<> invocation overhead is minimal).
- [x] Backward compatibility? Breaking changes documented but mitigation undecided.

### Devil's Advocate Analysis

**Edge cases NOT explicitly covered:**
1. **Multi-key indexers**: The plan mentions `IndexerGetSetInterceptor<TKey, TValue>` but the current base class constrains `TKey : notnull`. Multi-key indexers (e.g., `this[int x, int y]`) use `ValueTuple<int, int>` as TKey. Does the pre-compiled concrete type handle this correctly? The `where TKey : notnull` constraint would need to remain.
2. **Init-only properties**: The plan mentions properties but does not address init-only setters. The current generator has special handling for init properties. How does the pre-compiled `PropertyGetSetInterceptor<TValue>` handle init accessors?
3. **Generic method interceptors with async return types**: `Of<T>()` is correctly excluded, but what about a generic method that returns `Task<T>` where T is the method's own type parameter? This is the intersection of two special cases.
4. **Zero-parameter void methods**: `VoidMethodInterceptor0` has no generic parameters at all. It needs a `Call(Action callback)` method. The plan mentions `Call(Action<T1, ..., TN>)` but for N=0 this would be `Call(Action)`. Is `Invoke(bool strict)` the signature? Currently `VoidMethodInterceptorBase<TDelegate, TArgs>` requires `TArgs` -- for zero-param it uses `Unit`. The pre-compiled version eliminates `TArgs` entirely.
5. **Overloaded methods with mixed sync/async overloads**: If `Format(string)` returns `string` and `FormatAsync(string)` returns `Task<string>`, these are different method names and not overloaded. But what if `Process(string)` returns `string` and `Process(string, CancellationToken)` returns `Task<string>`? The compositor would have one sync and one async interceptor field. Is this handled?

**Ways this could break existing functionality:**
1. The `IMethodReturnBuilderArgs<TCallback, TArgs>` interface is part of the public API. Pre-compiled builders would implement `IMethodReturnBuilderArgs<Func<int, int, int>, (int, int)>` with unnamed tuple. Any user code that stores a reference as `IMethodReturnBuilderArgs<AddDelegate, (int a, int b)>` would break at the type level (both TCallback and TArgs change).
2. The `IMethodReturnSequence<TDelegate>` interface uses the custom delegate type. With pre-compiled types, the sequence would use `Func<>` instead. User code storing `IMethodReturnSequence<AddDelegate>` would break.

**Ways users could misunderstand the API:**
1. Users might try `Return(Task.FromResult(42))` for a `Task<int>` method, expecting it to work like `Return(42)`. With unified types where `TReturn = Task<int>`, `Return(Task<int> value)` WOULD work, but `Return(42)` would NOT compile (int is not Task<int>). This is the core of Concern #1.

### Concerns

#### Concern 1: The "Unified Async" Recommendation Breaks the Simplified Async API

**Category:** Architectural Decision
**Severity:** High -- would block implementation or degrade user experience

**Details:**
The architect recommends (plan line 794, findings section 5) using unified `MethodInterceptorN` types where `TReturn` can be `Task<T>`, eliminating separate `AsyncMethodInterceptorN` types. The rationale is reducing library type count from ~36 to ~18.

However, this breaks the simplified async API that is a major usability feature of KnockOff. Currently, for `Task<int> GetAsync(string key)`, users write:

```csharp
stub.GetAsync.Return(key => 42);           // Simplified - auto-wraps in Task.FromResult
stub.GetAsync.Return(42);                   // Value - auto-wraps in Task.FromResult
stub.GetAsync.Return(key => Task.FromResult(42)); // Full delegate
```

With unified `MethodInterceptor1<string, Task<int>>`, the type's `Return` method signature would be:
- `Return(Func<string, Task<int>> callback)` -- full delegate only
- `Return(Task<int> value)` -- value must be `Task<int>`, not `int`

The simplified `Return(key => 42)` and `Return(42)` would NOT compile because:
- `Return(Func<string, Task<int>> callback)` requires the lambda to return `Task<int>`, not `int`
- `Return(Task<int> value)` requires `Task.FromResult(42)`, not `42`

The architect acknowledges this (plan line 587): "The simplified sync callback (`Return(Func<..., int>)` for `Task<int>` methods) is the only reason for separate async types." But then dismisses it as optional convenience.

**Evidence from codebase:** The simplified async API is demonstrated and tested across all 9 patterns in `src/Design/Design.Stubs/Methods/AsyncConsistency.cs`. The three-tier API (value, simplified callback, full delegate) is a deliberate design decision documented in that file.

**Question:** The plan body proposes 4 type families (sync, void, async, async-void). The architect proposes 2 (sync, void). Which direction should we take?

**Suggestion:** Keep the 4 type families as the plan proposes. The additional ~20 library types are a small price for preserving the simplified async API. The types are compiled once in the library and reused everywhere. Alternatively, add `Return` overloads on `MethodInterceptorN` that detect `Task<T>` via compile-time overload resolution (e.g., `Return(Func<T1, TInner> callback) where TReturn : Task<TInner>`) -- but this requires type constraints that C# does not support in this form.

#### Concern 2: LastArgs Breaking Change -- No Decision Made

**Category:** API Design Decision
**Severity:** Medium -- affects implementation approach and test update scope

**Details:**
The plan documents the `LastArgs` breaking change (plan line 362) and the architect identifies it as wider than documented (findings section 3). Three options are proposed (plan line 577-579):
1. Accept unnamed tuple (breaking)
2. Generate thin wrapper property returning named tuple (no new types)
3. Defer to Phase 4

But no decision is made. The architect's recommendation (plan line 798) says "Accept unnamed tuples initially. Document as a known breaking change." But this is presented as a recommendation, not a confirmed decision.

The implementation needs a clear answer because:
- If option 1: The `IMethodTrackingArgs<TArgs>` interface and `IMethodReturnBuilderArgs<TCallback, TArgs>` interface become `IMethodTrackingArgs<(int, int)>` with unnamed fields. Tests using `tracking.LastArgs.name` must be updated to `tracking.LastArgs.Item1` or destructuring `var (name, value) = tracking.LastArgs`. There are at least 15 test locations and 6 interceptor-level LastArgs usages that need updating.
- If option 2: The pre-compiled type's builder exposes `(T1, T2)?` but the generated code adds a thin property that wraps it in `(T1 name, T2 value)?`. This requires generated code per method but zero new types.
- If option 3: Same as option 1 initially, with option 2 added later.

**Question:** Which option should we implement? This affects the scope of test changes and the generated code shape.

**Suggestion:** Accept option 1 (unnamed tuples) with option 2 deferred. This aligns with the plan's "breaking change" acknowledgment and keeps the initial implementation simpler. Destructuring (`var (a, b) = tracking.LastArgs`) still works identically. Only direct `.fieldName` access breaks, and this can be searched/replaced in tests.

#### Concern 3: Test Impact Analysis Missing

**Category:** Completeness
**Severity:** Medium -- needed before implementation starts

**Details:**
The plan does not analyze which existing tests will fail due to this change and how they should be updated. Given the CLAUDE.md rules about test preservation ("Never gut out-of-scope tests"), I need clarity on what constitutes "in-scope" for this change.

Specifically:
1. Tests using `tracking.LastArgs.fieldName` -- at least 15 locations in `src/Tests/KnockOffTests/` and `src/Tests/KnockOff.Documentation.Samples/` (found via grep). These are in-scope for the LastArgs breaking change.
2. Tests using `stub.Interceptor.LastArgs.Value.fieldName` -- 6 locations in inline stub tests. In-scope.
3. Documentation samples using named tuple destructuring -- at least 5 locations. These would need updating AND the markdown docs would need regeneration via `dotnet mdsnippets`.
4. Any tests referencing generated type names like `AddInterceptor` -- only 1 found in `RocksGapReproductionTests.cs`, minimal impact.
5. All Design.Stubs and Design.Tests files -- these will need significant updates since the generated code shape changes completely.

**Question:** Should the plan include an explicit test migration section listing affected test files and the expected changes? Or is "all tests must pass" sufficient, with the understanding that some test assertions will need updating?

**Suggestion:** At minimum, the plan should acknowledge these test categories and confirm they are in-scope for modification. This prevents the developer (me) from hitting the "STOP and ASK" protocol on every test file.

#### Concern 4: Events Are Underspecified

**Category:** Completeness
**Severity:** Low-Medium -- events exist in all patterns but the plan is vague

**Details:**
The plan says "Events use a simpler pattern. Evaluate whether they need changes or already work as fields" (line 120). The architect provides more detail (finding 7) noting events generate 1 type per event with no inner classes, and recommends a `EventInterceptorBase<TDelegate>` with generated `Raise` method.

But the plan's implementation phases (lines 429-455) do not mention events at all. Phase 1 lists methods, properties, indexers. Phase 2 lists renderer changes. Events are absent.

The Scope section (lines 407-410) lists events as affected, and the Design.Stubs acceptance criteria (line 787) includes "Events: add/remove/raise". But there is no concrete plan for how events will be handled.

**Question:** Are events in scope for this implementation, or deferred? If in scope, what is the approach -- a pre-compiled `EventInterceptor<TDelegate>` with generated `Raise`, or leaving events as-is (generated classes)?

**Suggestion:** Events already generate only 1 type with no inner classes. The ROI of converting them is much lower than methods (which generate 4 types each). Consider explicitly deferring events to keep the initial scope manageable: "Events continue using the current generated-class approach. Converting them to pre-compiled types is a future optimization."

#### Concern 5: The Fallback Mode Coexistence Is Not Specified

**Category:** Architectural Clarity
**Severity:** Medium -- needed for implementation

**Details:**
The plan states ref/out, ref returns, and >8 params continue using the "current generated-class approach" (line 141). The architect recommends retaining existing base classes for fallback mode (plan line 796).

But the implementation phases do not describe how the renderer decides between the two modes. Currently, `MethodInterceptorRenderer.RenderInterceptorClass` already has three modes (BaseClass, SingleSignature, OverloadGroup) with the decision logic at line 31-39 of the renderer. The new pre-compiled path would be a fourth mode (or replace BaseClass mode).

Questions:
1. Does the `useBaseClass` decision logic in `MethodInterceptorRenderer.cs` (line 31-39) get modified, or does a higher-level decision happen before calling the renderer?
2. For the renderer change: is `MethodInterceptorRenderer.cs` still used for fallback mode, and a completely new code path used for pre-compiled mode? Or does the existing renderer gain a new mode?
3. How do properties and indexers determine whether to use pre-compiled vs generated? Currently all properties use generated classes from `PropertyInterceptorRenderer.cs`.

**Suggestion:** The plan should specify the decision tree: "If method has ref/out params, ref return, or >8 params -> fallback to existing generated class. Otherwise -> emit pre-compiled field declaration." And clarify whether this decision happens in the model builder, the renderer, or a new routing layer.

### What Looks Good

- The core architectural insight is sound: eliminating generated types by parameterizing on arity is the right approach for reducing build time.
- The "Before and After" examples (plan lines 140-228) are clear and compelling.
- The overload compositor design (plan lines 275-308) is well-thought-out, preserving the existing `Return(callback)` disambiguation pattern.
- The stub override support via `SetFallback` (plan lines 232-269) is clean and preserves the existing user experience.
- The source delegation via `SetSourceFallback` (plan lines 312-327) is elegant, using method groups.
- The edge case fallback list (plan lines 372-387) correctly identifies which cases need special handling.
- The architect's analysis of base class incompatibility (finding 2) is accurate and well-supported by the code.
- The architect's analysis of overload compositor ambiguity with `Return(value)` (finding 9) correctly identifies the issue and confirms the existing behavior to preserve.
- The feature flag recommendation (architect recommendation 5) is pragmatic for incremental development.

### Recommendation

Send back to architect to address concerns before implementation. The critical blocker is Concern #1 (async type family decision), which fundamentally affects the library type design. The other concerns are important but could be resolved during implementation if the async question is settled.

Specifically, the architect should:
1. Make a clear decision on 2 vs 4 type families (Concern #1). If 2, explain how the simplified async API is preserved. If 4, update the "Architectural Recommendations" section.
2. Make a clear decision on LastArgs handling (Concern #2). Pick option 1, 2, or 3.
3. Add a brief test migration section or explicitly mark all tests as in-scope for modification (Concern #3).
4. Clarify event scope (Concern #4) -- in scope or deferred.
5. Describe the renderer decision tree for pre-compiled vs fallback mode (Concern #5).

---

## Implementation Contract

**Created:** 2026-02-14
**Approved by:** knockoff-developer

### Design.Stubs Acceptance Criteria

Design.Stubs verification is deferred to implementation because the library types do not exist yet. Once Phase 1 is complete, the following must compile in `src/Design/Design.Stubs`:

- [ ] Each of the 8 applicable patterns has at least one stub using pre-compiled interceptor fields
- [ ] Methods: sync void, sync non-void, async Task<T>, async ValueTask<T>, async Task, async ValueTask
- [ ] Properties: get-only, set-only, get+set
- [ ] Indexers: get+set
- [ ] Overloaded methods: compositor pattern
- [ ] Stub overrides: SetFallback wiring in constructor
- [ ] Source delegation: SetSourceFallback wiring
- [ ] Generic methods: Of<T>() pattern (unchanged, continues to generate types)
- [ ] Events: unchanged (out of scope, continue current generated-class approach)

### In Scope

**Phase 1: Library Types** (`src/KnockOff/Interceptors/`)

- [ ] Create `MethodInterceptor0<TReturn>` through `MethodInterceptor8<T1,...,T8,TReturn>` (9 types)
  - Each with: `Invoke`, `Return(Func<...>)`, `Return(TReturn)`, `Return(first, params rest[])`, `When(T1,...,TN)`, `When(Func<...,bool>)`, `Verify`, `Reset`, `CheckVerification`, `CheckVerificationAll`, `SetFallback`, `SetSourceFallback`
  - Each with pre-compiled inner classes: `MethodCallBuilderN`, `MethodSequenceN`, `WhenBuilderN`, `WhenChainN`
- [ ] Create `VoidMethodInterceptor0` through `VoidMethodInterceptor8<T1,...,T8>` (9 types)
  - Same as above but with `Call(Action<...>)` instead of `Return`, void `Invoke`
- [ ] Create `AsyncMethodInterceptor0<TReturn>` through `AsyncMethodInterceptor8<T1,...,T8,TReturn>` (9 types)
  - With simplified `Return(Func<...,TReturn>)` (auto-wraps in Task.FromResult) and full `Return(Func<...,Task<TReturn>>)`
  - With `Return(TReturn value)` (auto-wraps in Task.FromResult)
  - `Invoke` returns `Task<TReturn>`
- [ ] Create `AsyncVoidMethodInterceptor0` through `AsyncVoidMethodInterceptor8<T1,...,T8>` (9 types)
  - With simplified `Call(Action<...>)` and full `Call(Func<...,Task>)`
  - `Invoke` returns `Task`
- [ ] Make `PropertyGetInterceptorBase<TValue>` concrete (non-abstract) with delegate-based `InvokeGetUnconfigured`, add `SetSourceFallback(Func<TValue>?)`, `SetFallback(Func<TValue>?)`
- [ ] Make `PropertySetInterceptorBase<TValue>` concrete with delegate-based `InvokeSetUnconfigured`, add `SetSourceFallback(Action<TValue>?)`, `SetFallback(Action<TValue>?)`
- [ ] Make `PropertyGetSetInterceptorBase<TValue>` concrete (inherits from concrete PropertyGetInterceptorBase), add fallback/source delegates for both get and set
- [ ] Make `IndexerGetSetInterceptorBase<TKey,TValue>` concrete with delegate-based `InvokeGetUnconfigured`/`InvokeSetUnconfigured`, add `SetSourceFallback`/`SetFallback` for get and set
- [ ] Expose `TotalCallCount` as public (or via interface) on all method interceptor types for overload compositor aggregation
- [ ] **Checkpoint: Write hand-written unit tests for at least `MethodInterceptor2`, `VoidMethodInterceptor0`, `AsyncMethodInterceptor1`, `AsyncVoidMethodInterceptor0` exercising Return/Call, When chains, sequences, verification, SetFallback, SetSourceFallback. All tests must pass.**

**Phase 2: Generator Changes** (all 4 renderer pipelines)

- [ ] Implement fallback decision tree in renderer: method has ref/out, ref return, or >8 params -> fallback to generated class; otherwise -> pre-compiled field
- [ ] Modify `FlatRenderer.cs` (patterns 1, 2): emit pre-compiled field declarations, interface implementation forwarders, constructor for SetFallback, Source method using SetSourceFallback, Verify/VerifyAll aggregation
- [ ] Modify `StandaloneClassRenderer.cs` (patterns 3, 4): same changes as FlatRenderer adapted for class stubs (.Object pattern)
- [ ] Modify `InlineRenderer.cs` (patterns 5, 8): same changes within nested class structure
- [ ] Modify `ClassRenderer.cs` (patterns 6, 9): same changes for inline class stubs (.Object pattern)
- [ ] Property rendering: emit concrete property interceptor fields instead of generated property interceptor classes. Wire SetSourceFallback in Source method, SetFallback in constructor for stub overrides.
- [ ] Indexer rendering: emit concrete indexer interceptor field instead of generated indexer interceptor class. Same fallback/source wiring.
- [ ] Overload compositor: generate thin compositor class with pre-compiled interceptor fields, delegation methods for Return/Call/When, aggregated Verify/Reset
- [ ] Events: leave unchanged (out of scope)
- [ ] Generic methods (`Of<T>()`): leave unchanged (continue generating classes)
- [ ] Retain `MethodInterceptorRenderer.cs` for fallback mode (ref/out, ref return, >8 params)
- [ ] **Checkpoint: `dotnet build src/Design/Design.Stubs` succeeds for all target frameworks.**

**Phase 2b: Test Migration**

- [ ] Update ~6 interceptor-level `LastArgs.Value.fieldName` usages to use `Item1`/`Item2` or destructuring (in `DelegateValueOverloadTests.cs`, `InlineStubTests.cs`)
- [ ] Update ~3 builder-level `tracking.LastArgs.fieldName` usages in `BasicTests.cs`
- [ ] Update ~5 documentation sample `tracking.LastArgs` usages in `src/Tests/KnockOff.Documentation.Samples/`
- [ ] Update ~7 Design.Tests `tracking.LastArgs` usages
- [ ] Update any tests referencing generated interceptor type names (e.g., `AddInterceptor` in `RocksGapReproductionTests.cs`)
- [ ] Regenerate markdown documentation: `dotnet mdsnippets`
- [ ] **Checkpoint: `dotnet test src/KnockOff.sln` passes all tests across all target frameworks.**

**Phase 3: Final Verification**

- [ ] `dotnet build src/Design/Design.Stubs` succeeds
- [ ] `dotnet test src/KnockOff.sln` passes all tests
- [ ] Benchmark: compare build time of test suite vs v0.49.0 baseline
- [ ] Verify generated code for at least one stub per pattern matches the plan's "After" examples

### Explicitly Out of Scope

- **Events** -- Continue using current generated-class approach. 1 type per event with no inner classes; minimal ROI for conversion.
- **Named `When`/`ThenWhen` parameters** -- Deferred to Phase 4 (future, non-breaking enhancement).
- **Named `LastArgs` tuple fields** -- Accepted as breaking change. Deferred to future enhancement via thin generated properties.
- **Generic method interceptors (`Of<T>()`)** -- Continue using current generated-class approach. Dictionary-based runtime pattern cannot be pre-compiled.
- **Inline delegate stubs (pattern 7)** -- N/A, single Invoke with no per-member interceptors.
- **Separate ValueTask interceptor types** -- AsyncMethodInterceptorN returns `Task<TReturn>`; generator wraps in `new ValueTask<T>(...)` for ValueTask interface methods.

### Verification Gates

1. **After Phase 1 (Library Types):** Hand-written unit tests pass for core interceptor types (MethodInterceptor2, VoidMethodInterceptor0, AsyncMethodInterceptor1, AsyncVoidMethodInterceptor0). Tests exercise Return/Call, When chains, sequences, verification, fallback, source delegation.
2. **After Phase 2 (Generator Changes):** `dotnet build src/Design/Design.Stubs` succeeds for all target frameworks. The generated code uses pre-compiled field declarations (not generated interceptor classes) for eligible methods/properties/indexers.
3. **After Phase 2b (Test Migration):** `dotnet test src/KnockOff.sln` passes all tests across all target frameworks. Zero failures.
4. **Final:** All of the above, plus build time benchmark comparison vs v0.49.0.

### Stop Conditions

If any of these occur, STOP and report to the orchestrator:

- An out-of-scope test fails that is NOT related to LastArgs naming, generated type names, or structural change from generated classes to pre-compiled fields
- An event-related test fails (events are out of scope)
- An architectural contradiction is discovered (e.g., pre-compiled types cannot replicate existing behavioral logic faithfully)
- Generated code does not compile for a pattern that should be supported
- The fallback mode (ref/out, ref return, >8 params) stops working
- Performance regression is observed in interceptor invocation (unlikely but monitor)
