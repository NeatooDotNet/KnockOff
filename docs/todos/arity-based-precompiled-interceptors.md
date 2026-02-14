# Arity-Based Pre-compiled Interceptors

**Status:** Not Started
**Priority:** High
**Created:** 2026-02-14
**Related:** [reduce-generated-code-size.md](reduce-generated-code-size.md) (predecessor - reduced lines but not build time)

---

## Problem

The v0.49.0 base class work reduced generated code lines by ~26%, but benchmarking showed **no improvement in MSBuild build time**. The bottleneck is not lines of code - it's the **number of generated types** the C# compiler must process. The test suite currently generates ~8,860 sealed classes (interceptors + inner builder/when types). Each type requires metadata generation, type checking, and generic instantiation regardless of how few lines it contains.

## Solution

Replace generated interceptor classes with **fields of pre-compiled generic types** parameterized by arity. Instead of generating a `sealed class AddInterceptor` with nested `MethodCallBuilderImpl`, `WhenBuilder`, and `WhenChain`, generate a single field declaration:

```csharp
// Before: 4 types, 202 lines per method member
public sealed class AddInterceptor : MethodInterceptorBase<AddDelegate, (int a, int b), int>
{
    // delegate, Invoke, Return, When, Reset, LastArgs, ...
    public sealed class MethodCallBuilderImpl : ReturnMethodCallBuilderBase { ... }
    public sealed class WhenBuilder : WhenBuilderBase { ... }
    public sealed class WhenChain : WhenChainBase { ... }
}

// After: 0 types, 1 line per method member
public MethodInterceptor2<int, int, int> Add { get; } = new("Add");
```

All behavioral logic (Return, When, sequences, verification, builders, When chains) lives in pre-compiled types in the KnockOff library, compiled once.

### Estimated Impact

- Generated types: **~8,860 → ~200-500** (95%+ reduction, only edge case fallbacks remain)
- Generated lines per stub (CalculatorStub): **776 → ~60**
- Library types added: ~42 (arity-based families, compiled once)

## Approach

### Pre-compiled Type Families (in KnockOff library)

**Methods (non-void):**
- `MethodInterceptor0<TReturn>` through `MethodInterceptor8<T1, ..., T8, TReturn>`
- Contains: `Invoke`, `Return(Func<...>)`, `Return(TReturn)`, `Return(first, params rest[])`, `When(T1, ..., TN)`, `When(Func<..., bool>)`, verification, sequences, builders, When chains

**Methods (void):**
- `VoidMethodInterceptor0` through `VoidMethodInterceptor8<T1, ..., T8>`
- Contains: `Invoke`, `Call(Action<...>)`, `When`, verification, sequences

**Properties:**
- `PropertyGetInterceptor<TValue>`, `PropertySetInterceptor<TValue>`, `PropertyGetSetInterceptor<TValue>`
- Made concrete (non-abstract) with delegate fields replacing abstract methods

**Indexers:**
- `IndexerGetSetInterceptor<TKey, TValue>` (concrete, non-abstract)

### What Gets Generated (per stub)

1. Field declarations (one per member)
2. Interface implementation methods (one-line forwarders)
3. Constructor (wires stub override fallbacks and source delegation via lambdas)
4. `Verify()` / `VerifyAll()` methods
5. `Source()` method
6. `Strict` property, `Object` property (class stubs)

### Generated Stub Example (CalculatorStub)

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

    public void Verify() { /* aggregate CheckVerification across fields */ }
    public void VerifyAll() { /* aggregate CheckVerificationAll across fields */ }
}
```

### Stub Override Support

Stub overrides (protected virtual methods in generated base class) work via `SetFallback` delegate, wired in the constructor:

```csharp
public BasicStubOverrideStub()
{
    Process.SetFallback(Process_);     // method group → virtual method
    Calculate.SetFallback(Calculate_);
}
```

The generated base class (`BasicStubOverrideStubBase`) is unchanged.

### User-Facing API Changes

| API | Change |
|---|---|
| `stub.Add.Return(42)` | No change |
| `stub.Add.Return((a, b) => a + b)` | No change (user names lambda params) |
| `stub.Add.Return(1, 2, 3)` | No change |
| `stub.Add.When(1, 2).Return(3)` | IntelliSense shows `arg1, arg2` instead of `a, b` |
| `stub.Add.When((a, b) => a > 0)` | No change (user names predicate params) |
| `stub.Add.Verify(Called.Once)` | No change |
| `tracking.Verifiable()` | No change |
| `tracking.ThenReturn(10)` | No change |
| `stub.Reset.Call(() => ...)` | No change |
| `stub.Source(real)` | No change |
| `tracking.LastArgs.Value.a` | **Breaking:** becomes `.Item1` (unnamed tuple) |
| Explicit type names (`AddInterceptor`) | **Breaking:** becomes `MethodInterceptor2<int, int, int>` |
| `Return(callback)` tooltip | Shows `Func<int, int, int>` instead of `AddDelegate` |
| `When().Return().ThenWhen()` | `ThenWhen` shows `arg1, arg2` |

### Future Enhancement (non-breaking)

Named `When`/`ThenWhen` parameters can be added later via thin generated subclasses (1 type, ~4 lines per member) without breaking any existing user code. Deferred to keep initial implementation minimal.

### Edge Case Fallbacks (continue using current generated-class approach)

Only genuinely rare cases fall back to the old approach:

- `ref`/`out` parameters (already inline mode today)
- `ref` returns (already inline mode today)
- Methods with >8 parameters (extremely rare)

**Not fallbacks** (handled by pre-compiled types):
- Async `Task<T>`/`ValueTask<T>` → `AsyncMethodInterceptorN` with simplified and full-async Return overloads
- Async `Task`/`ValueTask` → `AsyncVoidMethodInterceptorN`
- Overloaded methods → thin compositor class (1 generated type, zero inner classes)
- Stub overrides → `SetFallback` delegate in constructor
- Source delegation → `SetSourceFallback` delegate

## Scope

### All 9 Patterns

| Pattern | Affected? |
|---|---|
| 1. Standalone `[KnockOff]` | Yes |
| 2. Generic Standalone | Yes |
| 3. Standalone Class `[KnockOffBase<T>]` | Yes |
| 4. Generic Standalone Class | Yes |
| 5. Inline Interface `[KnockOff<IFoo>]` | Yes |
| 6. Inline Class `[KnockOff<ConcreteClass>]` | Yes |
| 7. Inline Delegate | N/A (single Invoke, no per-member interceptors) |
| 8. Open Generic Interface | Yes |
| 9. Open Generic Class | Yes |

### All 4 Member Types

- Methods (non-void, void)
- Properties (get, set, get+set)
- Indexers (get+set)
- Events

### All 4 Renderer Pipelines

| Pipeline | Renderer | Change |
|---|---|---|
| Standalone interface (1,2) | FlatRenderer | Field declarations instead of interceptor classes |
| Standalone class (3,4) | StandaloneClassRenderer | Field declarations instead of interceptor classes |
| Inline (5,6) | InlineRenderer | Field declarations instead of interceptor classes |
| Open generic (8,9) | InlineRenderer | Field declarations instead of interceptor classes |

---

## Plans

- [Arity-Based Pre-compiled Interceptors](../plans/arity-based-precompiled-interceptors.md) -- Full design: type families, async handling, overload compositors, stub overrides, before/after examples

---

## Phases

### Phase 1: Library - Pre-compiled Type Families
- Create `MethodInterceptor0<TReturn>` through `MethodInterceptor8<T1,...,T8,TReturn>`
- Create `VoidMethodInterceptor0` through `VoidMethodInterceptor8<T1,...,T8>`
- Create `AsyncMethodInterceptor0<TReturn>` through `AsyncMethodInterceptor8<T1,...,T8,TReturn>`
- Create `AsyncVoidMethodInterceptor0` through `AsyncVoidMethodInterceptor8<T1,...,T8>`
- Make property interceptor base classes concrete (non-abstract) with delegate fields
- Make indexer interceptor base class concrete with delegate fields
- Pre-compiled builder, WhenBuilder, WhenChain inner classes for each arity
- `SetFallback` and `SetSourceFallback` methods on all types

### Phase 2: Generator - Renderer Changes
- Modify all 4 renderers to emit field declarations instead of interceptor classes
- Generate constructor for fallback/source wiring
- Generate interface implementation one-liners
- Preserve inline mode fallback for edge cases

### Phase 3: Verification
- All Design project tests pass
- All test projects compile and pass
- Benchmark build time comparison vs v0.49.0

### Phase 4 (Future, Non-breaking): Named When Parameters
- Optional thin generated subclasses for named `When`/`ThenWhen` params
- Deferred - can be added without breaking changes

---

## Tasks

- [ ] Design pre-compiled type family API (library types)
- [ ] Implement Phase 1 (library types)
- [ ] Implement Phase 2 (generator changes)
- [ ] Benchmark build time vs v0.49.0
- [ ] Verify all Design tests pass
- [ ] Verify all test projects pass

---

## Completion Verification

Before marking this todo as Complete, verify:

- [ ] Design project builds successfully
- [ ] Design project tests pass
- [ ] Build time benchmark shows improvement vs v0.49.0

**Verification results:**
- Design build: [Pending]
- Design tests: [Pending]
- Build time: [Pending]

---

## Results / Conclusions

[What was learned? What decisions were made?]
