# Arity-Based Pre-compiled Interceptors

**Status:** Not Started
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

### Events

Events use a simpler pattern. Evaluate whether they need changes or already work as fields.

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

### All 4 Member Types

- Methods (sync void, sync non-void, async void, async non-void)
- Properties (get, set, get+set)
- Indexers
- Events

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
