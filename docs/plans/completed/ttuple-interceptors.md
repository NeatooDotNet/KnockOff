# TTuple Interceptors - Collapse Arities + Restore Named Parameters

**Date:** 2026-02-15
**Related Todo:** [TTuple Interceptors](../todos/ttuple-interceptors.md)
**Status:** Ready for Implementation
**Last Updated:** 2026-02-15

---

## Overview

Replace the arity-based interceptor system (36 sealed types with ~180 inner classes) with a TTuple approach that collapses arities 1-8 into one type per family. The new system uses `MethodInterceptor<TDelegate, TArgs, TReturn>` where TDelegate is a generated delegate (giving named callback parameters) and TArgs is a ValueTuple or raw type (giving named When parameters via tuple element names).

Zero-param methods continue using `MethodInterceptor0<TReturn>` (no TDelegate or TArgs needed).

**Key design decision:** The library invokes TDelegate directly -- it never converts to `Func<>`. This eliminates converter lambdas from generated code and unlocks future ref/out parameter support.

---

## Problem Statement

v0.50.0 introduced pre-compiled arity-based generic types (`MethodInterceptor0<TReturn>` through `MethodInterceptor8<T1,...,T8,TReturn>`) achieving a 53% build time reduction. However:

1. **Lost named parameters** -- IntelliSense shows `arg1, arg2` instead of original parameter names in all callbacks (Return, Call, ThenReturn, ThenCall) and When/ThenWhen.
2. **36 library types** -- 9 arities x 4 families (sync, void, async, async-void) of near-identical code to maintain, plus ~180 inner classes.

---

## Current Architecture (Two Parallel Systems)

The codebase has TWO parallel interceptor systems. Understanding this is critical to the plan.

### System 1: Arity-Based Sealed Types (Pre-Compiled)

**Files:** `src/KnockOff/Interceptors/MethodInterceptor{0-8}.cs`, `VoidMethodInterceptor{0-8}.cs`, `AsyncMethodInterceptor{0-8}.cs`, `AsyncVoidMethodInterceptor{0-8}.cs`

These are 36 standalone sealed classes. Each one duplicates ALL behavioral logic: Invoke, Return/Call, When chain, sequence, verification, inner classes (MethodCallBuilder, MethodSequence, WhenBuilder, WhenChain).

**Usage:** `PreCompiledInterceptorRenderer` selects these types. Generated code is just a field declaration:
```csharp
public MethodInterceptor2<int, int, int> Add { get; } = new("Add");
```

**Invoke call from generated stub:**
```csharp
return Add.Invoke(Strict, a, b);
```

**Selection criteria (from `PreCompiledInterceptorRenderer.CanUsePreCompiled`):**
- Not an overload group
- No ref/out parameters
- No ref returns
- 0-8 parameters

### System 2: Base Class Hierarchy (Generated Classes)

**Files:** `src/KnockOff/Interceptors/VoidMethodInterceptorBase.cs`, `MethodInterceptorBase.cs`

These base classes already use `<TDelegate, TArgs>` (and `<TDelegate, TArgs, TReturn>` for non-void). Generated interceptor classes inherit from these bases and override abstract methods to unpack TArgs into individual parameters.

**Usage:** `MethodInterceptorRenderer.RenderBaseClassContent` generates a sealed class deriving from the base. Used as fallback for ref/out, ref returns, async methods, and overload groups.

**The base classes already use ValueTuple for TArgs** (0 params = `Unit`, 1 param = raw type, 2+ params = named ValueTuple). The generated subclass provides the bridge between TDelegate (which takes individual params) and TArgs (which is a tuple).

### What This Plan Changes

**Replace System 1 (arity types) with concrete subclasses of System 2 (base classes).** The 36 arity types become unnecessary -- instead, the generator emits a delegate type per method and uses the base class system with TDelegate as the generated delegate.

The base class system ALREADY handles TArgs, When chains, sequences, verification, etc. The only gap is that generated delegates need to be invocable without converting to `Func<>`.

---

## Approach: Extend Base Classes, Not Replace (SUPERSEDED -- see Architect Resolution)

> **NOTE:** This section's base-class-inheritance approach was invalidated by the developer review. The Architect Resolution section (below the Developer Review) replaces this approach with standalone sealed types that do NOT inherit from the base classes. Read the Architect Resolution for the authoritative design.

### What Already Works

`VoidMethodInterceptorBase<TDelegate, TArgs>` and `MethodInterceptorBase<TDelegate, TArgs, TReturn>` already provide:
- All behavioral logic (priority chain, When chain, sequences, verification)
- TArgs-based When matching with `Func<TArgs, bool>` predicates
- LastArg/LastArgs tracking
- Reset, Verifiable, CheckVerification, CheckVerificationAll
- Inner class base types (MethodCallBuilderBase, MethodSequenceBase, ReturnMethodCallBuilderBase, etc.)

### What Needs to Change

1. **Direct TDelegate invocation:** The base classes call `InvokeVoidDelegate(TDelegate, TArgs)` and `InvokeDelegate(TDelegate, TArgs)` as abstract methods. Currently, generated subclasses override these to unpack TArgs and call the delegate. For the TTuple approach, we need a way to invoke TDelegate without generating a subclass override for each method.

2. **Generated delegate types:** The generator must emit a delegate per 1+ param method (it already does this for some cases via `NeedsCustomDelegate`).

3. **Field declarations:** Instead of `MethodInterceptor2<int, int, int>`, emit something like:
   ```csharp
   delegate int AddDelegate(int a, int b);
   public MethodInterceptor<AddDelegate, (int a, int b), int> Add = new("Add");
   ```

### Direct TDelegate Invocation Strategy

The base classes have abstract `InvokeVoidDelegate(TDelegate, TArgs)` and `InvokeDelegate(TDelegate, TArgs)` methods. Currently, each generated subclass overrides these. To avoid generating subclasses, we have three options:

**Option A: Expression Trees (compiled once per type combo)**
- Build an expression tree in the base class constructor that unpacks TArgs into TDelegate parameter positions
- Cache the compiled delegate in a `static` field keyed by `<TDelegate, TArgs>` type combo
- First invocation pays compilation cost; subsequent invocations are nearly free
- **Pros:** Zero per-method generated code, maximum reuse
- **Cons:** Expression tree compilation has measurable first-call cost; complexity in the library

**Option B: Delegate.DynamicInvoke**
- Store TDelegate as `Delegate` and call `DynamicInvoke(args)` with an object array
- **Pros:** Simple implementation
- **Cons:** Boxing of value-type args, significant per-call overhead, no AOT support

**Option C: Generated thin subclass (current approach, refined)**
- Continue generating a sealed subclass per method that overrides `InvokeDelegate`/`InvokeVoidDelegate`
- But the subclass body is minimal: just the delegate invocation bridge
- **Pros:** Zero runtime overhead, simple, AOT-friendly
- **Cons:** Still generates a class per method (though much smaller than current full interceptor classes)

**Recommended: Option C (Generated thin subclass)**

Option C keeps the current approach but makes the generated subclass much thinner. The generated class only needs:
- A constructor calling `base(memberName)`
- An `InvokeDelegate` or `InvokeVoidDelegate` override (one line)
- A `RecordArgs` override (one line)
- A `RecordUnconfiguredArgs` override (one line)
- `CreateValueDelegate` override for non-void (one line)
- Thin inner classes for typed builder/sequence/when

This is what the MethodInterceptorRenderer ALREADY generates via `RenderBaseClassContent`. The base class approach IS the TTuple approach -- it just needs the delegate type to be a generated delegate (for named params) rather than `Func<>`/`Action<>`.

**Wait -- this means the "TTuple approach" as described in the todo is actually about making the ARITY TYPES use TDelegate for named params, not about extending the base classes.**

Let me reconsider. The todo says:
> Replace the arity-based type system with a TTuple approach: `MethodInterceptor<TDelegate, TArgs, TReturn>`

The intent is to have **8 concrete library types** (not 36) that take TDelegate as a type parameter. The arity types are sealed classes that cannot take TDelegate. The base classes CAN take TDelegate but are abstract -- they need generated subclasses.

So the real design is: **Make the base classes non-abstract by internalizing the TDelegate-to-TArgs bridge using expression trees or DynamicInvoke.**

### Revised Strategy: Concrete Library Types with Internal Invocation Bridge

Create 8 new concrete types (4 sync families x 2 param groups):

**For 1+ param methods:**
- `MethodInterceptor<TDelegate, TArgs, TReturn>` (sync non-void)
- `VoidMethodInterceptor<TDelegate, TArgs>` (sync void)
- `AsyncMethodInterceptor<TDelegate, TArgs, TReturn>` (async non-void)
- `AsyncVoidMethodInterceptor<TDelegate, TArgs>` (async void)

**For 0-param methods (unchanged):**
- `MethodInterceptor0<TReturn>`
- `VoidMethodInterceptor0`
- `AsyncMethodInterceptor0<TReturn>`
- `AsyncVoidMethodInterceptor0`

The 1+ param types inherit from the existing base classes and use expression trees (compiled once, cached per `<TDelegate, TArgs>` combo in a static field) to bridge TDelegate invocation without generated subclass overrides.

### Expression Tree Bridge Design

```csharp
public sealed class MethodInterceptor<TDelegate, TArgs, TReturn>
    : MethodInterceptorBase<TDelegate, TArgs, TReturn>
    where TDelegate : Delegate
{
    // Cached compiled invokers -- one static per closed generic type combo
    private static readonly Func<TDelegate, TArgs, TReturn>? s_invoker;
    private static readonly Func<TReturn, TDelegate>? s_valueDelegate;

    static MethodInterceptor()
    {
        // Build expression tree: (del, args) => del(args.Item1, args.Item2, ...)
        // For 1-param: (del, args) => del(args)  (TArgs is raw type)
        // For 2+ param: (del, args) => del(args.Item1, args.Item2, ...)
        s_invoker = BuildInvoker();
        s_valueDelegate = BuildValueDelegate();
    }

    public MethodInterceptor(string memberName) : base(memberName) { }
    public MethodInterceptor(string memberName, Func<TReturn> defaultFactory)
        : base(memberName) { _defaultFactory = defaultFactory; }

    protected override TReturn InvokeDelegate(TDelegate del, TArgs args)
        => s_invoker!(del, args);

    protected override TDelegate CreateValueDelegate(TReturn value)
        => s_valueDelegate!(value);

    protected override void RecordArgs(TArgs args, MethodCallBuilderBase tracking)
    {
        if (tracking is MethodCallBuilderImpl impl) impl.RecordArg(args);
    }

    protected override void RecordUnconfiguredArgs(TArgs args)
        => _unconfiguredLastArgs = args;

    private static Func<TDelegate, TArgs, TReturn>? BuildInvoker() { /* expression tree logic */ }
    private static Func<TReturn, TDelegate>? BuildValueDelegate() { /* expression tree logic */ }
}
```

The expression tree compilation happens once per unique `<TDelegate, TArgs, TReturn>` combo, triggered by the static constructor. After that, invocations are as fast as regular delegates.

**Critical consideration: netstandard2.0.** The KnockOff library targets netstandard2.0. Expression trees (`System.Linq.Expressions`) are available in netstandard2.0 via `Expression.Lambda<T>().Compile()`. This works.

### Generated Code Pattern

For a method `int Add(int a, int b)`:

**Before (v0.50.0 arity types):**
```csharp
public MethodInterceptor2<int, int, int> Add { get; } = new("Add");
// Invoke: Add.Invoke(Strict, a, b)
// Return: Add.Return((a, b) => a + b)  -- IntelliSense shows arg1, arg2
// When:   Add.When(1, 2)
```

**After (TTuple):**
```csharp
delegate int AddDelegate(int a, int b);
public MethodInterceptor<AddDelegate, (int a, int b), int> Add { get; } = new("Add");
// Invoke: Add.Invoke(Strict, (a, b))  -- pass args as tuple
// Return: Add.Return((int a, int b) => a + b)  -- IntelliSense shows a, b via TDelegate
// When:   Add.When((1, 2))  -- IntelliSense shows a, b via tuple element names
```

For a method `string Process(string input)` (1-param):

```csharp
delegate string ProcessDelegate(string input);
public MethodInterceptor<ProcessDelegate, string, string> Process { get; } = new("Process");
// Invoke: Process.Invoke(Strict, input)
// Return: Process.Return((string input) => input.ToUpper())
// When:   Process.When("hello")
```

For a void method `void Execute(int count, string name)`:

```csharp
delegate void ExecuteDelegate(int count, string name);
public VoidMethodInterceptor<ExecuteDelegate, (int count, string name)> Execute { get; } = new("Execute");
// Invoke: Execute.Invoke(Strict, (count, name))
// Call:   Execute.Call((int count, string name) => { ... })
// When:   Execute.When((1, "test"))
```

### Invoke Call Change

**Current:** `Add.Invoke(Strict, a, b)` -- individual parameters
**New:** `Add.Invoke(Strict, (a, b))` -- tuple literal for 2+ params, raw value for 1 param

The base class `Invoke` method takes `TArgs`, not individual params. For 2+ params, the generated stub code wraps arguments in a tuple. For 1 param, it passes the raw value.

### Async Families

The async families need their own concrete types because async invocation requires `await` handling:

**`AsyncMethodInterceptor<TDelegate, TArgs, TReturn>`:**
- TDelegate is `Func<params..., Task<TReturn>>` or a generated delegate returning `Task<TReturn>`
- The Return() method offers simplified overloads taking `TReturn` (auto-wrapped)
- The Invoke method returns `Task<TReturn>`

**`AsyncVoidMethodInterceptor<TDelegate, TArgs>`:**
- TDelegate is a generated delegate returning `Task`
- The Call() method offers simplified overloads taking `Action<params...>` (auto-wrapped)
- The Invoke method returns `Task`

**Issue: Async base classes do not exist yet.** The current `VoidMethodInterceptorBase` and `MethodInterceptorBase` are sync-only. The async arity types (`AsyncMethodInterceptor{0-8}`, `AsyncVoidMethodInterceptor{0-8}`) are standalone sealed classes with their own duplicated logic (they use `Task.FromResult`, `ConfigureAwait(false)`, etc.).

**This means we need to create async base classes** or extend the existing ones to handle async invocation. This is the most significant new library work in this plan.

---

## Scope

### Member Types Affected

| Member Type | Affected? | Notes |
|---|---|---|
| Methods | Yes | Core of this plan |
| Properties | No | Property interceptors are already pre-compiled (PropertyGetInterceptor, etc.) |
| Indexers | No | Indexer interceptors are already pre-compiled (IndexerGetSetInterceptor, etc.) |
| Events | No | Event interceptors are already pre-compiled |

### Pattern Impact

All 9 patterns are affected equally because the change is in the **library types** (what the field type resolves to) and the **renderer** (how field declarations and Invoke calls are generated). The builder/transform pipeline is not affected.

| Pattern | Pipeline | Affected Renderer |
|---|---|---|
| Standalone (1) | FlatModelBuilder -> FlatRenderer | PreCompiledInterceptorRenderer |
| Generic Standalone (2) | FlatModelBuilder -> FlatRenderer | PreCompiledInterceptorRenderer |
| Standalone Class (3) | StandaloneClassModelBuilder -> StandaloneClassRenderer | PreCompiledInterceptorRenderer |
| Generic Standalone Class (4) | StandaloneClassModelBuilder -> StandaloneClassRenderer | PreCompiledInterceptorRenderer |
| Inline Interface (5) | InlineModelBuilder -> InlineRenderer | PreCompiledInterceptorRenderer |
| Inline Class (6) | InlineModelBuilder -> InlineRenderer | PreCompiledInterceptorRenderer |
| Inline Delegate (7) | InlineModelBuilder -> InlineRenderer | PreCompiledInterceptorRenderer |
| Open Generic Interface (8) | InlineModelBuilder -> InlineRenderer | PreCompiledInterceptorRenderer |
| Open Generic Class (9) | InlineModelBuilder -> InlineRenderer | PreCompiledInterceptorRenderer |

All renderers call `PreCompiledInterceptorRenderer.GetMethodInterceptorType()` for field type computation and `PreCompiledInterceptorRenderer.GetMethodInvokeExpression()` for Invoke calls. Changing these two methods (plus delegate generation) affects all patterns.

### Renderer Pipeline Changes

| Renderer | Change |
|---|---|
| `PreCompiledInterceptorRenderer.GetMethodInterceptorType()` | Compute `MethodInterceptor<TDelegate, TArgs, TReturn>` instead of `MethodInterceptorN<T1,...,TN, TReturn>` |
| `PreCompiledInterceptorRenderer.GetMethodInvokeExpression()` | Emit `Invoke(strict, (a, b))` instead of `Invoke(strict, a, b)` |
| `PreCompiledInterceptorRenderer.GetOverloadInterceptorType()` | Same change for overload compositor inner fields |
| `PreCompiledInterceptorRenderer.GetMethodSourceFallbackExpression()` | Update for TDelegate-based SetSourceFallback |
| `PreCompiledInterceptorRenderer.GetStubOverrideFallbackExpression()` | Update for TDelegate-based SetFallback |
| All renderers (FlatRenderer, InlineRenderer, StandaloneClassRenderer, ClassRenderer) | Emit delegate type declaration before field |
| `PreCompiledInterceptorRenderer.RenderOverloadCompositorClass` | Emit slot interface implementations instead of forwarding methods; emit `IReadOnlyList<IInterceptor> Interceptors` property |
| `PreCompiledInterceptorRenderer.CanUsePreCompiled()` | 8-param limit retained (see Concern 6 resolution) |

### What Does NOT Change

- `MethodInterceptorRenderer` (generates subclasses for fallback cases: ref/out, ref returns)
- Existing base classes `VoidMethodInterceptorBase<TDelegate, TArgs>` and `MethodInterceptorBase<TDelegate, TArgs, TReturn>` (only extended, never modified)
- Property, indexer, and event interceptors
- Zero-param method interceptors (`MethodInterceptor0<TReturn>`, etc.) -- except that `IInterceptor` interface is added to them (see Compositor Slot Interface Design)
- Builder and Transform pipeline
- Model types

---

## Design: New Library Types (SUPERSEDED -- see Architect Resolution)

> **NOTE:** This section's type inventory and base class hierarchy was invalidated by the developer review. The Architect Resolution section provides the revised type inventory and concrete type designs. Read the Architect Resolution for the authoritative design.

### Type Inventory

**New types (4):**

| Type | Base Class | Purpose |
|---|---|---|
| `MethodInterceptor<TDelegate, TArgs, TReturn>` | `MethodInterceptorBase<TDelegate, TArgs, TReturn>` | Sync non-void, 1+ params |
| `VoidMethodInterceptor<TDelegate, TArgs>` | `VoidMethodInterceptorBase<TDelegate, TArgs>` | Sync void, 1+ params |
| `AsyncMethodInterceptor<TDelegate, TArgs, TReturn>` | New `AsyncMethodInterceptorBase<TDelegate, TArgs, TReturn>` | Async non-void, 1+ params |
| `AsyncVoidMethodInterceptor<TDelegate, TArgs>` | New `AsyncVoidMethodInterceptorBase<TDelegate, TArgs>` | Async void, 1+ params |

**New base types (2):**

| Type | Extends | Purpose |
|---|---|---|
| `AsyncMethodInterceptorBase<TDelegate, TArgs, TReturn>` | `MethodInterceptorBase<TDelegate, TArgs, TReturn>` | Adds async invocation, Task.FromResult wrapping |
| `AsyncVoidMethodInterceptorBase<TDelegate, TArgs>` | `VoidMethodInterceptorBase<TDelegate, TArgs>` | Adds async invocation, Task.CompletedTask handling |

**Retained unchanged (4):**
- `MethodInterceptor0<TReturn>`
- `VoidMethodInterceptor0`
- `AsyncMethodInterceptor0<TReturn>`
- `AsyncVoidMethodInterceptor0`

**Deleted (36):**
- `MethodInterceptor{1-8}<T1,...,TN, TReturn>` (8 types)
- `VoidMethodInterceptor{1-8}<T1,...,TN>` (8 types)
- `AsyncMethodInterceptor{1-8}<T1,...,TN, TReturn>` (8 types)
- `AsyncVoidMethodInterceptor{1-8}<T1,...,TN>` (8 types)
- Plus their ~180 inner classes

**Net change:** 36 deleted, 6 new = 30 fewer types. Inner classes drop from ~180 to ~20.

### Expression Tree Invoker

Each concrete type has a static constructor that builds and caches the TDelegate invocation bridge:

```csharp
// For MethodInterceptor<TDelegate, TArgs, TReturn>
private static readonly Func<TDelegate, TArgs, TReturn> s_invoker;

static MethodInterceptor()
{
    s_invoker = DelegateInvokerFactory.BuildInvoker<TDelegate, TArgs, TReturn>();
}
```

**`DelegateInvokerFactory`** is a shared utility class:

```csharp
internal static class DelegateInvokerFactory
{
    public static Func<TDelegate, TArgs, TReturn> BuildInvoker<TDelegate, TArgs, TReturn>()
        where TDelegate : Delegate
    {
        var delegateInvokeMethod = typeof(TDelegate).GetMethod("Invoke")!;
        var delegateParams = delegateInvokeMethod.GetParameters();
        var paramCount = delegateParams.Length;

        // Parameters for the outer lambda: (TDelegate del, TArgs args)
        var delParam = Expression.Parameter(typeof(TDelegate), "del");
        var argsParam = Expression.Parameter(typeof(TArgs), "args");

        // Build argument expressions by extracting from TArgs
        var argExprs = new Expression[paramCount];
        if (paramCount == 1)
        {
            // TArgs is the raw type -- just pass it directly
            argExprs[0] = argsParam;
        }
        else
        {
            // TArgs is ValueTuple -- access .Item1, .Item2, etc.
            for (int i = 0; i < paramCount; i++)
            {
                argExprs[i] = Expression.Field(argsParam, $"Item{i + 1}");
            }
        }

        // Build: del.Invoke(arg1, arg2, ...)
        var invokeExpr = Expression.Invoke(delParam, argExprs);

        // Compile to Func<TDelegate, TArgs, TReturn>
        return Expression.Lambda<Func<TDelegate, TArgs, TReturn>>(
            invokeExpr, delParam, argsParam).Compile();
    }
}
```

**Performance:** Expression tree compilation happens once per unique `<TDelegate, TArgs, TReturn>` type combo (in the static constructor). The compiled invoker is a regular delegate with near-zero overhead per call.

### Concrete Type: MethodInterceptor<TDelegate, TArgs, TReturn>

```csharp
public sealed class MethodInterceptor<TDelegate, TArgs, TReturn>
    : MethodInterceptorBase<TDelegate, TArgs, TReturn>
    where TDelegate : Delegate
{
    private static readonly Func<TDelegate, TArgs, TReturn> s_invoker
        = DelegateInvokerFactory.BuildInvoker<TDelegate, TArgs, TReturn>();
    private static readonly Func<TReturn, TDelegate> s_valueDelegate
        = DelegateInvokerFactory.BuildValueDelegate<TDelegate, TArgs, TReturn>();

    private readonly Func<TReturn>? _defaultFactory;
    private TDelegate? _fallback;
    private TDelegate? _sourceFallback;

    public MethodInterceptor(string memberName) : base(memberName) { }
    public MethodInterceptor(string memberName, Func<TReturn> defaultFactory)
        : base(memberName) { _defaultFactory = defaultFactory; }

    // --- Abstract overrides ---
    protected override TReturn InvokeDelegate(TDelegate del, TArgs args)
        => s_invoker(del, args);
    protected override TDelegate CreateValueDelegate(TReturn value)
        => s_valueDelegate(value);
    protected override void RecordArgs(TArgs args, MethodCallBuilderBase tracking)
        => (tracking as MethodCallBuilder)?.RecordArg(args);
    protected override void RecordUnconfiguredArgs(TArgs args)
        => _unconfiguredLastArgs = args;

    // --- Public API ---
    public TArgs? LastArgs => /* FindLastArgInTracking pattern */;

    public MethodCallBuilder Return(TDelegate callback) { ... }
    public MethodCallBuilder Return(TReturn value) { ... }
    public MethodSequence Return(TReturn first, params TReturn[] rest) { ... }

    // When takes TArgs (tuple for 2+, raw for 1)
    public WhenBuilder When(TArgs args) { ... }
    public WhenBuilder When(Func<TArgs, bool> predicate) { ... }

    public void SetFallback(TDelegate? fallback) => _fallback = fallback;
    public void SetSourceFallback(TDelegate? sourceFallback) => _sourceFallback = sourceFallback;

    // Full Invoke method using base RunPriorityChain + fallback + strict
    public TReturn Invoke(bool strict, TArgs args)
    {
        var (handled, result) = RunPriorityChain(args);
        if (handled) return result;

        _unconfiguredCallCount++;
        RecordUnconfiguredArgs(args);

        var (exHandled, exResult) = HandleNonVoidSequenceExhaustedRepeat(strict, args);
        if (exHandled) return exResult;

        if (_fallback != null) return s_invoker(_fallback, args);
        if (_sourceFallback != null) return s_invoker(_sourceFallback, args);
        if (strict) throw StubException.NotConfigured("", _memberName);
        if (_defaultFactory != null) return _defaultFactory();
        return default!;
    }

    // Inner classes
    public sealed class MethodCallBuilder : ReturnMethodCallBuilderBase { ... }
    public sealed class MethodSequence : ReturnMethodSequenceBase { ... }
    public sealed class WhenBuilder : WhenBuilderBase { ... }
    public sealed class WhenChain : WhenChainBase { ... }
}
```

### Async Base Classes

Create `AsyncMethodInterceptorBase<TDelegate, TArgs, TReturn>` extending `MethodInterceptorBase`:

```csharp
public abstract class AsyncMethodInterceptorBase<TDelegate, TArgs, TReturn>
    : MethodInterceptorBase<TDelegate, TArgs, TReturn>
    where TDelegate : Delegate
{
    protected AsyncMethodInterceptorBase(string memberName) : base(memberName) { }

    // Async-specific invocation
    protected abstract Task<TReturn> InvokeAsyncDelegate(TDelegate del, TArgs args);

    // Async Invoke
    public async Task<TReturn> Invoke(bool strict, TArgs args)
    {
        // Same priority chain pattern as sync, but with await on delegate invocations
        // ...
    }

    // Simplified Return overloads accepting sync callbacks
    // Return(Func<TArgs, TReturn>) that wraps in Task.FromResult
}
```

The async concrete types follow the same pattern as the sync ones, using expression trees for direct invocation.

### When API Change

**Current (arity types):**
```csharp
// 1 param:  stub.Process.When("hello")
// 2 params: stub.Add.When(1, 2)
```

**New (TTuple):**
```csharp
// 1 param:  stub.Process.When("hello")    -- unchanged (TArgs is raw string)
// 2 params: stub.Add.When((1, 2))         -- extra parens (tuple literal)
```

This is a **breaking API change** for 2+ param When calls. The `When` method now takes `TArgs` instead of individual parameters. Users must wrap arguments in tuple syntax.

**Mitigation:** IntelliSense shows named tuple elements: `When((int a, int b) args)` displays as `When((a: 1, b: 2))`, which is arguably clearer than `When(1, 2)` with unnamed `arg1, arg2`.

---

## Implementation Phases (SUPERSEDED -- see Architect Resolution)

> **NOTE:** These phases are superseded by the "Revised Implementation Phases" in the Architect Resolution section.

### Phase 1: Library Foundation (New Base + Concrete Types)

**Deliverables:**
1. `DelegateInvokerFactory` utility with expression tree builders
2. `AsyncMethodInterceptorBase<TDelegate, TArgs, TReturn>`
3. `AsyncVoidMethodInterceptorBase<TDelegate, TArgs>`
4. `MethodInterceptor<TDelegate, TArgs, TReturn>` concrete type
5. `VoidMethodInterceptor<TDelegate, TArgs>` concrete type
6. `AsyncMethodInterceptor<TDelegate, TArgs, TReturn>` concrete type
7. `AsyncVoidMethodInterceptor<TDelegate, TArgs>` concrete type

**Verification:** Unit tests for the new types in isolation (create test delegates, verify Return/Call/When/Verify all work).

### Phase 2: Generator/Renderer Changes

**Deliverables:**
1. Update `PreCompiledInterceptorRenderer.GetMethodInterceptorType()` to return TTuple type
2. Update `PreCompiledInterceptorRenderer.GetMethodInvokeExpression()` to emit tuple args
3. Update `PreCompiledInterceptorRenderer.GetOverloadInterceptorType()` for compositor fields
4. Add delegate type emission to all renderers (FlatRenderer, InlineRenderer, StandaloneClassRenderer, ClassRenderer)
5. Update `PreCompiledInterceptorRenderer.GetMethodSourceFallbackExpression()` for TDelegate
6. Update `PreCompiledInterceptorRenderer.GetStubOverrideFallbackExpression()` for TDelegate
7. Remove `> 8 params` limit from `CanUsePreCompiled()`

**Verification:** All existing tests pass. Generated .g.cs files show delegate + TTuple field declarations.

### Phase 3: Delete Arity Types

**Deliverables:**
1. Delete `MethodInterceptor{1-8}.cs` (8 files)
2. Delete `VoidMethodInterceptor{1-8}.cs` (8 files)
3. Delete `AsyncMethodInterceptor{1-8}.cs` (8 files)
4. Delete `AsyncVoidMethodInterceptor{1-8}.cs` (8 files)
5. Delete related interface types if they become unused (`IMethodReturnBuilder<Func<T1, TReturn>, T1?>`, etc.)

**Verification:** Full build succeeds. All tests pass. No remaining references to deleted types.

### Phase 4: Design Project Verification + Benchmarks

**Deliverables:**
1. Design.Stubs compiles with TTuple types
2. Design.Tests passes
3. IntelliSense verification (named params on Return/Call, named tuple elements on When)
4. Build time benchmark vs v0.49.0 (28.5s) and v0.50.0 (13.4s)

---

## Breaking Changes

### API Breaking Changes

1. **When syntax for 2+ params:** `stub.Add.When(1, 2)` becomes `stub.Add.When((1, 2))`. This is a source-level breaking change.

2. **ThenWhen syntax for 2+ params:** Same tuple wrapping required.

3. **Return/Call callback type:** `stub.Add.Return((a, b) => a + b)` -- the lambda parameter types change from `Func<int, int, int>` to `AddDelegate`. In practice, lambda syntax is identical; only explicit delegate references would break.

4. **SetFallback/SetSourceFallback:** These now take TDelegate instead of `Func<>`. Source delegation expressions in generated code must be updated.

5. **LastArg becomes LastArgs:** For 1-param methods, `LastArg` (returning `T1?`) changes to `LastArgs` (returning `TArgs`). Since TArgs is the raw type for 1-param, the type is the same but the property name changes. **This is significant** -- we should keep `LastArg` for 1-param backward compatibility.

6. **LastArgs type change for 2+ params:** Currently `(T1, T2)?` (unnamed tuple). New: `(T1 a, T2 b)?` (named tuple). Named tuples are structurally compatible with unnamed ones, so existing code accessing `.Item1`, `.Item2` still works. `.a`, `.b` are new aliases.

### Binary Breaking Changes

All interceptor types change. This is a major version bump.

---

## Estimated Impact

### Type Count

| Category | Before | After | Change |
|---|---|---|---|
| Arity sealed types | 36 | 0 | -36 |
| Concrete TTuple types | 0 | 4 | +4 |
| Utility types | 0 | 1 (DelegateInvokerFactory) | +1 |
| Compositor slot interfaces | 0 | 32 | +32 |
| Compositor extension classes | 0 | 4 | +4 |
| IInterceptor interface | 0 | 1 | +1 |
| Zero-param types | 4 | 4 | 0 |
| Base classes | 2 | 2 | 0 |
| Inner classes (est.) | ~180 | ~20 | -160 |
| **Total library types** | ~222 | ~68 | **-154** |

Note: The 32 slot interfaces are trivial single-property interfaces (no behavioral logic). The 4 extension classes contain the forwarding methods that were previously generated per-compositor. This is a net reduction in total code despite the higher type count, because the slot interfaces replace hundreds of generated forwarding methods across all compositors.

### Generated Code Per Method

| Component | Before | After | Change |
|---|---|---|---|
| Delegate type | 0 | 1 per method | +1 |
| Interceptor field | 1 | 1 | 0 |
| Converter lambda | 0 | 0 | 0 |
| **Net per method** | 1 line | 2 lines (delegate + field) | +1 line |

### Build Time Target

The delegate generation adds ~1 line per method. Estimated ~1,400 delegates for full test suite. Build time target: ~15-16s (comparable to v0.50.0's 13.4s, possibly slightly higher due to delegate types).

---

## Edge Cases and Risks

### Edge Case: Generic Method Delegates

For generic methods like `T Process<T>(T input)`, the delegate must be generic:
```csharp
delegate T ProcessDelegate<T>(T input);
```
This is already handled by the existing `NeedsCustomDelegate` / `CustomDelegateSignature` model fields.

### Edge Case: `in` Parameters

`in` parameters work with delegates: `delegate int FooDelegate(in string x)`. The expression tree invoker needs to handle `in` parameter semantics. ValueTuple fields are always by-value, so `in` parameters in the delegate signature receive the tuple field value by value -- this is correct since `in` is just a performance hint.

### Edge Case: Overload Compositors

Overload compositor classes contain inner interceptor fields. These fields change from arity types to TTuple types. The compositor rendering in `PreCompiledInterceptorRenderer.RenderOverloadCompositorClass` uses `GetOverloadInterceptorType()` which must be updated.

Additionally, compositor forwarding methods (Call, When, Return, Verify, Reset) are replaced by numbered slot interface implementations. See the "Compositor Slot Interface Design" section for the complete design including edge case analysis for ambiguity scenarios.

### Risk: Expression Tree Compilation Cost

First invocation of each unique `<TDelegate, TArgs, TReturn>` type combo triggers expression tree compilation. In a test suite with many different method signatures, this could add measurable startup time.

**Mitigation:** The static constructor runs at first access. For a test suite, this is amortized across the entire run. Measured DynamicMethod compilation typically takes 0.01-0.1ms per type combo.

### Risk: ref/out Future Unlock

The todo mentions ref/out as a future unlock. The expression tree approach supports ref/out through `Expression.MakeMemberAccess` and `ByRef` parameter expressions. However, ValueTuple fields cannot be ref/out. The TArgs tuple would only contain input (non-ref) parameters for When matching. The TDelegate handles ref/out natively since it's a real delegate type. **The architecture supports this future work.**

### Risk: AOT Compatibility

Expression trees compile to DynamicMethod which requires runtime code generation. For NativeAOT scenarios, this would fail. However, KnockOff is a test-time library and tests typically run on full CLR, not NativeAOT.

---

## DynamicInvoke Prototype Results

A prototype at `src/Prototypes/DynamicInvokePrototype/` verified the invocation strategy (37 tests, all passing).

### Confirmed Working

- **DynamicInvoke handles ref/out params** -- modified values propagate back through the `object[]` array
- **Async delegates** -- DynamicInvoke returns the `Task<T>` which can be cast and awaited
- **Tuple decomposition** -- `ITuple` interface provides `Length` + indexer for converting ValueTuple to `object[]`
- **Generic delegates, nullable params, void delegates** -- all work as expected

### Key Findings That Affect This Plan

1. **Expression trees CANNOT handle ref/out parameters** -- `Expression.Invoke` throws `ArgumentException` for delegates with ref/out params. This means the expression tree invoker in `DelegateInvokerFactory` must fall back to `DynamicInvoke` for ref/out delegates (future ref/out feature), or the generated code must handle those cases differently. For the current scope (no ref/out support), expression trees work for all cases.

2. **Tuples cannot preserve ref semantics** -- ValueTuple copies values, so TArgs-as-tuple loses ref tracking. For future ref/out support, the `Invoke` signature and `When` matching would need an `object[]`-based path for ref/out methods, not TArgs. This does NOT affect the current plan (no ref/out methods use pre-compiled interceptors today).

3. **DynamicInvoke wraps exceptions in `TargetInvocationException`** -- If the library ever uses DynamicInvoke (as fallback or for ref/out), it must unwrap `.InnerException` to preserve the user's expected exception type.

4. **DynamicInvoke performance** -- ~5-10x slower than compiled expression trees (~70ms vs ~8ms for 1M iterations). Negligible for test stubs, but confirms expression trees as the right default.

5. **DynamicInvoke silently converts null to default(T) for non-nullable value types on .NET 9** -- no exception thrown. This is surprising but not a problem for the current design.

---

## Files Examined

| File | What Was Learned |
|---|---|
| `src/KnockOff/Interceptors/MethodInterceptor0.cs` | Zero-param interceptor structure; no TDelegate/TArgs needed |
| `src/KnockOff/Interceptors/MethodInterceptor1.cs` | 1-param arity type; full duplicated logic (~667 lines) |
| `src/KnockOff/Interceptors/MethodInterceptor2.cs` | 2-param arity type; LastArgs uses unnamed `(T1, T2)?` tuple |
| `src/KnockOff/Interceptors/VoidMethodInterceptor1.cs` | Void 1-param; uses `Action<T1>` callbacks, different When pattern |
| `src/KnockOff/Interceptors/AsyncMethodInterceptor1.cs` | Async 1-param; duplicated logic with Task.FromResult wrapping |
| `src/KnockOff/Interceptors/AsyncVoidMethodInterceptor1.cs` | Async void 1-param; uses `Func<T1, Task>` callbacks |
| `src/KnockOff/Interceptors/VoidMethodInterceptorBase.cs` | Base class with TDelegate/TArgs; RunVoidPriorityChain, inner class bases |
| `src/KnockOff/Interceptors/MethodInterceptorBase.cs` | Non-void base; RunPriorityChain, ReturnMethodCallBuilderBase, WhenBuilderBase |
| `src/KnockOff/Unit.cs` | Zero-size struct for 0-param TArgs |
| `src/Generator/Renderer/Shared/PreCompiledInterceptorRenderer.cs` | Field type computation, Invoke expression generation, CanUsePreCompiled decision tree |
| `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` | RenderBaseClassContent generates thin subclasses; ComputeTArgsType computes named tuples |
| `src/Generator/Renderer/FlatRenderer.cs` | How preCompiledInterceptors dict is populated and used for field emission |
| `src/Generator/Renderer/InlineRenderer.cs` | Same preCompiled pattern for inline stubs |
| `src/Generator/Renderer/StandaloneClassRenderer.cs` | Same preCompiled pattern for standalone class stubs |
| `src/Generator/Renderer/ClassRenderer.cs` | Same preCompiled pattern for class stubs |
| `src/Design/Design.Stubs/Generated/...CalculatorStub.g.cs` | Current generated output showing arity type fields and Invoke calls |

---

## Architectural Verification

### Scope Table

| Pattern | TTuple Methods | Notes |
|---|---|---|
| Standalone (1) | Yes | FlatRenderer + PreCompiledInterceptorRenderer |
| Generic Standalone (2) | Yes | Same pipeline as (1) |
| Standalone Class (3) | Yes | StandaloneClassRenderer + PreCompiledInterceptorRenderer |
| Generic Standalone Class (4) | Yes | Same pipeline as (3) |
| Inline Interface (5) | Yes | InlineRenderer + PreCompiledInterceptorRenderer |
| Inline Class (6) | Yes | InlineRenderer + PreCompiledInterceptorRenderer |
| Inline Delegate (7) | Yes | InlineRenderer + PreCompiledInterceptorRenderer |
| Open Generic Interface (8) | Yes | InlineRenderer + PreCompiledInterceptorRenderer |
| Open Generic Class (9) | Yes | InlineRenderer + PreCompiledInterceptorRenderer |

### Design Project Verification

Deferred to implementation. The plan changes library types and generated code; Design.Stubs verification requires the generator changes to be in place.

### Breaking Changes Assessment

**Major version required.** The When syntax change (`When(1, 2)` -> `When((1, 2))`) and LastArg -> LastArgs rename are source-breaking changes. All consuming code that uses When with 2+ params or accesses LastArg on 1+ param interceptors must be updated.

### Pattern Consistency Verified

All 9 patterns use the same `PreCompiledInterceptorRenderer` codepath for field type computation and Invoke expression generation. Changing this shared renderer affects all patterns consistently.

### Diagnostic Requirements

No new diagnostics needed. The existing `CanUsePreCompiled` decision tree is simplified (remove `> 8 params` check) rather than made more complex.

### Test Strategy

1. **Library unit tests:** Test new concrete types directly with hand-crafted delegates
2. **Generator snapshot tests:** Verify generated .g.cs output contains delegate + TTuple field declarations
3. **Integration tests:** All existing test suites must pass after migration
4. **Design.Stubs compilation:** Verify IntelliSense-observable named parameters
5. **Benchmark:** Measure build time vs v0.49.0 and v0.50.0

---

## Developer Review (Re-Review)

**Status:** Approved
**Reviewed:** 2026-02-15 (re-review after architect resolutions)

### My Understanding of This Plan

**Core Change:** Replace 36 arity-based sealed interceptor types (MethodInterceptor1 through MethodInterceptor8, across 4 families) with 4 new concrete generic types using `<TDelegate, TArgs, TReturn>` type parameters. Generated code will emit a delegate type per method and use ValueTuples for TArgs, restoring named parameters in IntelliSense.

**User-Facing API:** Users configure stubs the same way (Return, Call, When), but When for 2+ params changes from `When(a, b)` to `When((a, b))`. Return/Call callbacks use generated delegate types instead of Func/Action (lambda syntax is identical). SetFallback/SetSourceFallback take TDelegate. Generated code shows delegate declarations plus TTuple-parameterized field types.

**Internal Changes:** (1) New DelegateInvokerFactory with expression trees, (2) New async base classes, (3) 4 new concrete library types, (4) Renderer changes in PreCompiledInterceptorRenderer, (5) Delegate emission in all renderers, (6) Deletion of 36 arity types.

**Patterns Affected:** All 9 patterns equally (all share PreCompiledInterceptorRenderer).

### Codebase Investigation

**Files Examined:**
- `src/KnockOff/Interceptors/VoidMethodInterceptorBase.cs` - Confirmed: has TDelegate constraint, RunVoidPriorityChain, inner class base types (VoidWhenMatcherBase, VoidWhenChainBase, MethodCallBuilderBase, MethodSequenceBase). Does NOT have fallback fields, SetFallback, SetSourceFallback, or _unconfiguredLastArg/Args fields.
- `src/KnockOff/Interceptors/MethodInterceptorBase.cs` - Confirmed: extends VoidMethodInterceptorBase, adds RunPriorityChain, return value support, WhenMatcherBase, WhenBuilderBase, WhenChainBase, ReturnMethodCallBuilderBase, ReturnMethodSequenceBase. Also does NOT have fallback or _unconfiguredLastArg fields.
- `src/KnockOff/Interceptors/MethodInterceptor1.cs` - Standalone sealed class, 667 lines, complete self-contained logic. Has LastArg, SetFallback, SetSourceFallback, _defaultFactory. Inner classes: MethodCallBuilder1, MethodSequence1, WhenBuilder1, WhenChain1.
- `src/KnockOff/Interceptors/MethodInterceptor2.cs` - Same pattern but with 2 params. Has LastArgs as `(T1, T2)?` unnamed tuple. When takes individual params.
- `src/KnockOff/Interceptors/AsyncMethodInterceptor1.cs` - Standalone sealed class, NO base class, has `async Task<TReturn> Invoke` with ConfigureAwait(false) calls. Duplicated behavioral logic. Has Func<T1, Task<TReturn>> callbacks. Has both SetFallback(Func<T1, Task<TReturn>>) and SetFallback(Func<T1, TReturn>) overloads.
- `src/Generator/Renderer/Shared/PreCompiledInterceptorRenderer.cs` - Confirmed: GetMethodInterceptorType builds `MethodInterceptorN<T1,...,TN,TReturn>`. GetOverloadInterceptorType does the same. GetCallBuilderType returns `{interceptorType}.MethodCallBuilder{paramCount}`. GetWhenBuilderType returns `{interceptorType}.WhenBuilder{paramCount}` or `VoidWhenBuilder{paramCount}`. CanUsePreCompiled has >8 params check.
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` - ComputeTArgsType confirmed: 0->Unit, 1->raw type, 2+->named ValueTuple. RenderBaseClassContent generates thin subclasses with InvokeDelegate/InvokeVoidDelegate/RecordArgs/RecordUnconfiguredArgs overrides. Base class mode is ONLY used for sync, non-ref/out, non-ref-return methods (line 39). Async methods fall through to self-contained class generation. The generated Invoke method handles source delegation via `_source` field access and stub override fallback via direct method call -- NOT via SetFallback/SetSourceFallback.
- `src/KnockOff/IMethodReturnBuilder.cs` - Has IMethodReturnBuilder<TCallback, TArg> with LastArg. Arity types implement these interfaces with their specific builder types.
- `src/KnockOff/IMethodTracking.cs` - IMethodTracking<TArg> has LastArg. IMethodTrackingArgs<TArgs> has LastArgs.

**Searches Performed:**
- Searched for `.When(` in Design projects - found 30+ usages, including `stub.Add.When(1, 2)` in Design.Stubs and Design.Tests. All 2+ param When calls use individual params. This confirms the breaking change scope.
- Searched for `.LastArg` and `.LastArgs` - found extensive usage across Design.Stubs, Tests, Documentation.Samples, Prototype. Both LastArg (1-param) and LastArgs (2+ param) are widely used.
- Searched for MethodInterceptorN references in Tests and Benchmarks - found NO direct type references. Tests interact through generated stubs, not through arity types directly. This means test breakage will be from API changes (When syntax, LastArg rename), not from type name references.
- Searched for SetFallback/SetSourceFallback in renderer code - found extensive usage in FlatRenderer and InlineRenderer for wiring source delegation and stub overrides to pre-compiled interceptors.
- Searched for `MethodCallBuilder{N}` in PreCompiledInterceptorRenderer - found GetCallBuilderType and GetWhenBuilderType methods that compose inner class names by appending the param count. These are used by overload compositor rendering.

**Design.Stubs Verification:**
- The architect stated: "Design Project Verification: Deferred to implementation." No Design.Stubs compilation evidence was provided for any scope claim.

**Discrepancies Found:**
1. Plan says base classes provide "All behavioral logic" including "LastArg/LastArgs tracking" - but the base classes do NOT have _unconfiguredLastArg/Args fields, LastArg/LastArgs properties, SetFallback, SetSourceFallback, or _defaultFactory. These are all in the generated subclass or arity type.
2. Plan says "SetFallback(TDelegate? fallback)" and "SetSourceFallback(TDelegate? sourceFallback)" will be on the new concrete types. But the base class system handles source delegation differently: the generated Invoke method accesses `_source` directly and calls the source method, rather than storing a delegate via SetSourceFallback. These are two different approaches. The plan must clarify: will the new TTuple types use the arity-type approach (SetFallback/SetSourceFallback delegates) or the base-class approach (generated code in Invoke)?
3. Plan says the async arity types have "duplicated logic" which is correct. But the plan proposes creating async base classes that EXTEND the sync base classes. The async Invoke methods use `async/await` with `ConfigureAwait(false)` throughout. The plan's AsyncMethodInterceptorBase sketch inherits from MethodInterceptorBase but the sync base's RunPriorityChain returns `(bool, TReturn)` where TReturn is the final value, not Task<TReturn>. Async interceptors need to `await` the delegate calls. Simply extending MethodInterceptorBase won't work without significant restructuring of RunPriorityChain.

### Structured Question Checklist

**Completeness Questions:**
- [x] Are all nine patterns addressed? Yes, the plan correctly identifies all 9 use PreCompiledInterceptorRenderer.
- [x] What happens with null/empty/default values? Not discussed for expression tree invocation edge cases.
- [x] What happens with generic type parameters? Addressed via "Generic Method Delegates" edge case.
- [ ] What happens with nested types or inherited members? Not discussed.
- [x] How does this interact with existing features? Partially addressed but see concerns below.

**Correctness Questions:**
- [ ] Do the generated code examples actually compile? Cannot verify without implementation, and Design.Stubs evidence was not provided.
- [ ] Is the proposed implementation consistent with existing patterns? **No** - see Concern 2 about fallback/source delegation approach mismatch.
- [ ] Are model/builder/renderer responsibilities correctly assigned? The plan correctly identifies PreCompiledInterceptorRenderer as the central change point but underestimates renderer changes.
- [x] Breaking changes migration path? Partially clear but see concerns.

**Clarity Questions:**
- [ ] Could I implement this without clarifying questions? **No** - multiple ambiguities identified.
- [ ] Are there ambiguous requirements? **Yes** - see Concern 2 (fallback approach) and Concern 3 (async inheritance).
- [ ] Are edge cases explicitly handled? Partially.
- [x] Is the test strategy specific enough? Yes, at a high level.

**Risk Questions:**
- [x] What could go wrong? Expression tree compilation, async inheritance, compositor inner class names.
- [x] Which existing tests might fail? All tests using When with 2+ params, all tests using LastArg on 1-param methods.
- [x] Performance implications? Expression tree first-call cost discussed.
- [x] Backward compatibility concerns? Breaking change correctly identified.

### Devil's Advocate Analysis

**Edge cases NOT explicitly covered:**

1. **Overload compositor inner class name change.** The overload compositor uses `GetCallBuilderType()` which returns `{interceptorType}.MethodCallBuilder{paramCount}` and `GetWhenBuilderType()` which returns `{interceptorType}.WhenBuilder{paramCount}`. With TTuple types, the inner classes would NOT have arity suffixes (they'd be just `MethodCallBuilder`, `WhenBuilder`). The plan doesn't mention this at all, but the compositor rendering depends on these inner class names.

2. **Async simplified callbacks.** The async arity types provide simplified overloads: `Return(Func<T1, TReturn>)` alongside `Return(Func<T1, Task<TReturn>>)`, wrapping via `Task.FromResult`. The plan mentions this exists but doesn't detail how the TTuple concrete type will provide this. Since TDelegate is a generated delegate (e.g., `Func<int, Task<int>>`), the Return method must take TDelegate directly. How does `Return(TReturn value)` work? The plan's code shows it uses `CreateValueDelegate(value)` which builds a TDelegate via expression tree. But what about `Return(Func<T1, TReturn> simplifiedCallback)` -- how do you convert a `Func<T1, TReturn>` to a generated `ProcessDelegate` that returns `Task<TReturn>`?

3. **IMethodReturnBuilder interface implementation.** The arity types implement `IMethodReturnBuilder<Func<T1, TReturn>, T1?>` and `IMethodReturnBuilder<Func<T1, Task<TReturn>>, T1?>` for their inner MethodCallBuilder classes. The TTuple types' inner classes would need to implement the equivalent interfaces with TDelegate as the callback type parameter. This affects users who reference these interfaces in their code.

4. **When predicate type change.** Currently: `When(Func<T1, T2, bool> predicate)` with individual params. New: `When(Func<TArgs, bool> predicate)` where TArgs is a ValueTuple. The predicate signature changes from `(int a, int b) => a > 10` to `((int a, int b) args) => args.a > 10` for 2-param methods. This is a second source breaking change beyond the exact-value When syntax.

5. **8+ parameter ValueTuples.** For methods with 8+ params, ValueTuple nests: `(T1, T2, T3, T4, T5, T6, T7, TRest)` where TRest is another ValueTuple. The expression tree invoker's `Expression.Field(argsParam, $"Item{i + 1}")` stops working at Item8 because the 8th field is `Rest`, not `Item8`. The plan says "Remove >8 params limit from CanUsePreCompiled()" but the DelegateInvokerFactory.BuildInvoker code as written would fail for 8+ params.

**Ways this could break existing functionality:**

1. Overload compositors would fail to compile because inner class names change from `MethodCallBuilder{N}` to `MethodCallBuilder` (or whatever the TTuple type uses).
2. All test code using `When(a, b)` with 2+ params would fail with CS1501 (wrong number of arguments) or CS1503 (cannot convert individual args to tuple).
3. All test code using `When((a, b) => expr)` predicate-based matching with individual params would fail because the predicate now takes TArgs (a tuple) instead of individual params.

**Ways users could misunderstand the API:**

1. The When syntax change from `When(1, 2)` to `When((1, 2))` is subtle -- users may miss the extra parens and get confusing compiler errors.
2. The predicate When change from `When((a, b) => a > 0)` to `When(args => args.a > 0)` is non-obvious and looks like a regression in ergonomics.

### Concerns

1. **Missing: Design.Stubs Compilation Evidence**
   - Details: The plan states "Design Project Verification: Deferred to implementation." CLAUDE.md's verification protocol requires the architect to provide compilable Design.Stubs code for each "Yes" in the scope table, and failing code for features that need implementation.
   - Question: Can this be reasonably deferred since the change requires the generator to be modified first? The plan is modifying the foundation that generated code depends on, making pre-implementation Design.Stubs verification impractical.
   - Suggestion: I accept that Design.Stubs verification must be deferred for this plan since it changes generated output. But the plan should explicitly document what Design.Stubs compilations should be verified post-implementation.

2. **Ambiguity: Fallback/Source Delegation Approach Mismatch**
   - Details: The plan proposes `SetFallback(TDelegate?)` and `SetSourceFallback(TDelegate?)` on the new TTuple types, matching the arity types' approach. But the base class system uses a completely different approach: the generated Invoke method accesses `_source` directly and calls stub override methods directly. These are two different source delegation strategies. The plan does not reconcile them. Since the TTuple types inherit from the base classes, which approach will they use? If SetFallback/SetSourceFallback (arity approach), the Invoke method must be fully implemented in the concrete type, not delegated to the base class's RunPriorityChain. If direct source access (base class approach), the generated code must emit an Invoke method override rather than relying on a library Invoke.
   - Question: Which source delegation approach will the TTuple types use? If the arity approach (stored delegates), the base classes are barely used -- the TTuple types must duplicate the full Invoke logic. If the base class approach (generated Invoke), the TTuple types are really just thin generated subclasses, which is Option C from the plan (the option the plan explicitly chose, then pivoted away from).
   - Suggestion: This is the fundamental architectural question. The plan starts with Option C (generated thin subclass), then pivots to concrete library types with expression trees. But the reason for the pivot was to avoid generating a subclass per method. However, pre-compiled interceptors already avoid generating subclasses -- the whole point is a library type used directly. The key question is: can a library-level Invoke method handle source delegation and stub override fallback without generated code? If so, SetFallback/SetSourceFallback work. If not, you need generated code in the Invoke body (like the base class approach), which means Option C.

3. **Missing: Async Base Class Design is Incomplete**
   - Details: The plan proposes `AsyncMethodInterceptorBase<TDelegate, TArgs, TReturn>` extending `MethodInterceptorBase<TDelegate, TArgs, TReturn>`. But MethodInterceptorBase's `RunPriorityChain` calls `InvokeDelegate(callback, args)` synchronously and returns `(bool, TReturn)`. For async, you need `await InvokeAsyncDelegate(callback, args)` and the return type changes to `Task<(bool, TReturn)>` or similar. The base class's RunPriorityChain cannot be reused for async invocation without significant modification -- you'd need to either: (a) add a parallel `RunAsyncPriorityChain` method, (b) make RunPriorityChain generic over the invocation strategy, or (c) override the entire chain in the async subclass. The plan's sketch shows a new `InvokeAsyncDelegate` abstract method but doesn't explain how it integrates with RunPriorityChain.
   - Question: How exactly does the async TTuple type invoke delegates? The expression tree invoker returns `TReturn` which for async methods would be `Task<TReturn>`. Does `s_invoker(del, args)` return a `Task<TReturn>` that needs to be awaited? If TReturn IS `Task<TReturn>`, then RunPriorityChain already works. But the existing async arity types store the INNER return type (e.g., `int` for `Task<int>`), not the wrapped type.
   - Suggestion: Clarify whether TReturn in `AsyncMethodInterceptor<TDelegate, TArgs, TReturn>` is the inner type (int) or the wrapped type (Task<int>). This determines whether RunPriorityChain can be reused and whether expression tree invocation needs unwrapping.

4. **Missing: Overload Compositor Inner Class Name Changes**
   - Details: `GetCallBuilderType` returns `{interceptorType}.MethodCallBuilder{paramCount}` and `GetWhenBuilderType` returns `{interceptorType}.WhenBuilder{paramCount}`. The arity types name their inner classes with the arity suffix (MethodCallBuilder1, WhenBuilder1, etc.). The new TTuple types would presumably name them without the suffix (MethodCallBuilder, WhenBuilder). This breaks the compositor rendering.
   - Question: What will the inner class names be in the new TTuple types? How will GetCallBuilderType and GetWhenBuilderType be updated?
   - Suggestion: The inner classes should probably be named without arity suffixes (just `MethodCallBuilder`, `WhenBuilder`, `WhenChain`, `MethodSequence`). The compositor rendering methods need to be updated accordingly.

5. **Missing: When Predicate Signature Breaking Change**
   - Details: The plan discusses the exact-value When syntax change (`When(1, 2)` to `When((1, 2))`) but does NOT mention the predicate-based When change. Currently: `When(Func<T1, T2, bool> predicate)` -- user writes `When((a, b) => a > 10)`. New: `When(Func<TArgs, bool> predicate)` where TArgs is `(int a, int b)` -- user writes `When(args => args.a > 10)` or `When(((int a, int b) args) => args.a > 10)`. This is a significant ergonomics change that the plan should address.
   - Question: Is the predicate-based When API change acceptable? It makes lambda When usage more verbose.
   - Suggestion: Document this as a second API breaking change. Consider whether the compositor can still expose the old-style predicate API (with individual params) by wrapping.

6. **Risk: 8+ Parameter ValueTuple Nesting**
   - Details: The plan says "Remove >8 params limit from CanUsePreCompiled()" but the DelegateInvokerFactory.BuildInvoker as written uses `Expression.Field(argsParam, $"Item{i + 1}")` which fails for ValueTuples with 8+ elements due to nesting (`Item8` doesn't exist; the 8th element is `Rest`).
   - Question: Will 8+ param support be deferred or included?
   - Suggestion: Either handle ValueTuple nesting in BuildInvoker (recursively access Rest.Item1, etc.) or keep the >8 param limit and document it. The current arity types also max out at 8, so keeping the limit is not a regression.

7. **Missing: Async Simplified Callback Overloads**
   - Details: The async arity types provide `Return(Func<T1, TReturn> syncCallback)` alongside `Return(Func<T1, Task<TReturn>> asyncCallback)`, wrapping the sync version in `Task.FromResult`. The plan's code shows `Return(TDelegate callback)` where TDelegate is the async delegate type. How does `Return(Func<T1, TReturn>)` work when TDelegate is a generated async delegate? The concrete type needs additional Return overloads that accept simplified sync callbacks and wrap them.
   - Question: How are simplified async callbacks handled in the TTuple types?
   - Suggestion: The concrete AsyncMethodInterceptor type needs Return overloads that accept both the full TDelegate and simplified non-async lambdas. This may require additional expression tree magic or generated adapter code.

### What Looks Good

- The overall architecture is sound: using TDelegate for named callback params and TArgs (ValueTuple) for named When params is an elegant design.
- The decision to use expression trees (compiled once per type combo) is well-reasoned and the prototype validates basic feasibility.
- Correctly identifying that all 9 patterns share PreCompiledInterceptorRenderer is accurate and simplifies the impact analysis.
- The type inventory (4 new concrete types + 2 new base types) is clean.
- The generated code examples (delegate + field declaration) are clear and achievable.
- The ComputeTArgsType method already exists in MethodInterceptorRenderer, confirming the TArgs computation is proven.
- The approach of retaining zero-param interceptors unchanged is pragmatic and correct.

### Recommendation

Send back to architect to address concerns before implementation. The fundamental ambiguity (Concern 2) about fallback/source delegation approach needs resolution before implementation can proceed. The async base class design (Concern 3) also needs fleshing out. The compositor inner class naming (Concern 4) and predicate When breaking change (Concern 5) need to be documented.

---

## Architect Resolution of Developer Concerns

**Date:** 2026-02-15

### Fundamental Architectural Revision

The developer's concerns expose a critical flaw in the original plan: it attempted to build the TTuple types as subclasses of `MethodInterceptorBase<TDelegate, TArgs, TReturn>` / `VoidMethodInterceptorBase<TDelegate, TArgs>`. This creates irreconcilable problems:

1. The base classes lack fallback fields (`SetFallback`, `SetSourceFallback`, `_defaultFactory`)
2. The base classes lack unconfigured tracking fields (`_unconfiguredLastArg`, `_unconfiguredLastArgs`)
3. The base classes' `RunPriorityChain` is synchronous and cannot handle async delegate invocation
4. The base classes use abstract methods (`InvokeVoidDelegate`, `InvokeDelegate`, `RecordArgs`, etc.) that require per-method overrides -- which means generated subclasses -- which means Option C -- which means ~28.5s build time

**The revised architecture: TTuple types are standalone sealed classes, identical in structure to the current arity types.**

The TTuple types follow the exact same pattern as `MethodInterceptor1<T1, TReturn>`, `AsyncMethodInterceptor1<T1, TReturn>`, etc. They are self-contained sealed classes with all behavioral logic inlined. The only difference is:

- Instead of `<T1, TReturn>`, they take `<TDelegate, TArgs, TReturn>`
- Instead of `Func<T1, TReturn>` as the callback type, they use `TDelegate`
- Instead of accepting individual parameters in `Invoke(bool strict, T1 arg1)`, they accept `Invoke(bool strict, TArgs args)`
- Expression trees bridge between TDelegate (for callback invocation) and TArgs (for When matching / arg recording)

**No base classes are involved. No async base classes need to be created. No base class is extended.** The existing base classes (`VoidMethodInterceptorBase`, `MethodInterceptorBase`) remain untouched and continue to serve the generated-subclass fallback path (ref/out, ref returns).

This approach:
- Preserves the exact same Invoke logic as the arity types (including SetFallback, SetSourceFallback, _defaultFactory)
- Requires zero changes to how renderers wire source delegation and stub overrides
- Eliminates all async base class design questions
- Is a straightforward "find-replace" transformation: take `MethodInterceptor1<T1, TReturn>`, replace `T1` with `TArgs`, replace `Func<T1, TReturn>` with `TDelegate`, add expression tree bridges

### Type Inventory (Revised)

**New TTuple interceptor types (4 concrete, 1 utility):**

| Type | Base | Purpose |
|---|---|---|
| `MethodInterceptor<TDelegate, TArgs, TReturn>` | None (sealed) | Sync non-void, 1+ params |
| `VoidMethodInterceptor<TDelegate, TArgs>` | None (sealed) | Sync void, 1+ params |
| `AsyncMethodInterceptor<TDelegate, TArgs, TReturn>` | None (sealed) | Async non-void, 1+ params |
| `AsyncVoidMethodInterceptor<TDelegate, TArgs>` | None (sealed) | Async void, 1+ params |
| `DelegateInvokerFactory` | None (static) | Expression tree compilation |

**New compositor slot interfaces (32) and extension classes (4):**

| Type | Count | Purpose |
|---|---|---|
| `IVoidOverloadSlot{1-8}<TDelegate, TArgs>` | 8 | Void compositor slots |
| `IMethodOverloadSlot{1-8}<TDelegate, TArgs, TReturn>` | 8 | Non-void compositor slots |
| `IAsyncVoidOverloadSlot{1-8}<TDelegate, TArgs>` | 8 | Async void compositor slots |
| `IAsyncMethodOverloadSlot{1-8}<TDelegate, TArgs, TReturn>` | 8 | Async non-void compositor slots |
| `VoidSlotExtensions` | 1 | Extension methods for void slots (Call, When, Verify) |
| `MethodSlotExtensions` | 1 | Extension methods for non-void slots (Return, When, Verify) |
| `AsyncVoidSlotExtensions` | 1 | Extension methods for async void slots |
| `AsyncMethodSlotExtensions` | 1 | Extension methods for async non-void slots |

**New IInterceptor interface (1):**

| Type | Purpose |
|---|---|
| `IInterceptor` | Common interface with `CheckVerification()`, `CheckVerificationAll()`, `Reset()` for collection-based Verify/Reset |

**Retained unchanged:**
- `MethodInterceptor0<TReturn>`, `VoidMethodInterceptor0`, `AsyncMethodInterceptor0<TReturn>`, `AsyncVoidMethodInterceptor0`
- `VoidMethodInterceptorBase<TDelegate, TArgs>`, `MethodInterceptorBase<TDelegate, TArgs, TReturn>` (still used by generated-subclass fallback path)

**Deleted (36):**
- All `MethodInterceptor{1-8}`, `VoidMethodInterceptor{1-8}`, `AsyncMethodInterceptor{1-8}`, `AsyncVoidMethodInterceptor{1-8}`

**Net change:** 36 deleted, 42 new (5 interceptor + 32 slot interfaces + 4 extension classes + 1 IInterceptor). The slot interfaces are simple single-property interfaces with zero behavioral logic; the extension classes are the only place behavioral forwarding lives. The base classes are NOT part of this change at all.

### Concern 1: Design.Stubs Verification Deferred

**Resolution: Accepted with documented post-implementation verification plan.**

The developer correctly notes that Design.Stubs verification must be deferred because this plan changes the generator output that Design.Stubs depends on. Pre-implementation compilation verification is impractical.

**Post-implementation verification checklist:**

1. `dotnet build src/Design/Design.Stubs` must succeed
2. `dotnet test src/Design/Design.Tests` must pass
3. Spot-check generated .g.cs files in Design.Stubs for:
   - Delegate type declarations per 1+ param method
   - TTuple field types (`MethodInterceptor<AddDelegate, (int a, int b), int>`)
   - Invoke calls with tuple args for 2+ params
   - SetSourceFallback and SetFallback calls using TDelegate
4. IntelliSense verification (manual):
   - Return/Call callbacks show named parameters via TDelegate
   - When shows named tuple elements for 2+ params
5. Each of the 9 patterns must have at least one method interceptor using TTuple types

### Concern 2: Fallback/Source Delegation Approach (RESOLVED)

**Resolution: TTuple types use the arity-type approach (SetFallback/SetSourceFallback stored delegates), not the base-class approach.**

The TTuple types are standalone sealed classes that follow the same pattern as `MethodInterceptor1<T1, TReturn>`. They have their own:

- `_fallback` field of type `TDelegate?`
- `_sourceFallback` field of type `TDelegate?`
- `_defaultFactory` field of type `Func<TReturn>?`
- `SetFallback(TDelegate? fallback)` method
- `SetSourceFallback(TDelegate? sourceFallback)` method
- Complete `Invoke(bool strict, TArgs args)` method with the full priority chain inline

The Invoke method is structurally identical to `MethodInterceptor1.Invoke`:

```csharp
public sealed class MethodInterceptor<TDelegate, TArgs, TReturn> where TDelegate : Delegate
{
    private static readonly Func<TDelegate, TArgs, TReturn> s_invoker
        = DelegateInvokerFactory.BuildInvoker<TDelegate, TArgs, TReturn>();

    private TDelegate? _fallback;
    private TDelegate? _sourceFallback;
    private readonly Func<TReturn>? _defaultFactory;
    // ... all other fields identical to MethodInterceptor1 ...

    public TReturn Invoke(bool strict, TArgs args)
    {
        // When chain (uses TArgs directly for matching)
        if (_whenChain != null && _whenChainHead < _whenChain.Count)
        {
            var matcher = _whenChain[_whenChainHead];
            if (matcher.Matches(args)) { ... return matcher.CallReturn(args); }
            else if (matcher.IsTerminal) _whenChainHead++;
        }

        // Sequence
        if (_sequence != null && _sequenceIndex < _sequence.Count)
        {
            var (callback, tracking) = _sequence[_sequenceIndex];
            tracking.RecordCall(args);
            _sequenceIndex++;
            return s_invoker(callback, args);  // <-- expression tree invokes TDelegate
        }

        // Return value
        if (_hasReturnValue && _returnValueTracking != null) { ... return _returnValue; }

        // Callback
        if (_call != null && _callTracking != null)
        {
            _callTracking.RecordCall(args);
            return s_invoker(_call, args);  // <-- expression tree invokes TDelegate
        }

        // Unconfigured
        _unconfiguredCallCount++;
        _unconfiguredLastArgs = args;

        // Sequence exhaustion repeat
        if (_sequence != null && _sequenceIndex >= _sequence.Count) { ... }

        // Fallback (stub override) -- SAME as arity type
        if (_fallback != null) return s_invoker(_fallback, args);

        // Source fallback -- SAME as arity type
        if (_sourceFallback != null) return s_invoker(_sourceFallback, args);

        // Strict
        if (strict) throw StubException.NotConfigured("", _memberName);
        if (_defaultFactory != null) return _defaultFactory();
        return default!;
    }
}
```

**Impact on renderers:** ZERO changes to how `PreCompiledInterceptorRenderer.GetMethodSourceFallbackExpression` and `GetStubOverrideFallbackExpression` work. They already emit `SetSourceFallback(lambda)` and `SetFallback(methodGroup)` calls. The only change is the delegate type used in those lambdas, which is driven by `GetDelegateType()` -- that method needs updating to emit TDelegate-compatible lambdas or the generated delegate type name.

Wait -- there IS a subtlety here. Currently `SetSourceFallback` takes `Func<T1, TReturn>?` and `SetFallback` takes `Func<T1, TReturn>?`. With TTuple, they take `TDelegate?`. The renderer currently constructs `Func<>` lambdas or method groups. For TTuple, the renderer must construct TDelegate-compatible expressions.

**How this works:** The renderer already knows the delegate type. For pre-compiled interceptors, `PreCompiledInterceptorRenderer.GetMethodSourceFallbackExpression` constructs a `new Func<T1, TReturn>(source.Method)` or a lambda `(a) => source.Method(a)`. For TTuple, this becomes `new AddDelegate(source.Add)` or `(int a, int b) => source.Add(a, b)`. The renderer emits the generated delegate type name instead of `Func<>`.

The same applies to `GetStubOverrideFallbackExpression`: instead of `SetFallback(ProcessOverride_)`, it becomes `SetFallback(new ProcessDelegate(ProcessOverride_))` or `SetFallback((int a, int b) => ProcessOverride_(a, b))`.

**Key change to `PreCompiledInterceptorRenderer`:** Replace `GetDelegateType()` (which returns `Func<>`/`Action<>`) with a method that returns the generated delegate type name. The generated delegate type name is computable from the method model since the renderer also emits the delegate declaration.

### Concern 3: Async Design (RESOLVED)

**Resolution: No async base classes needed. Async TTuple types are standalone sealed classes, identical in structure to the current async arity types.**

The developer correctly identified that `RunPriorityChain` cannot handle async invocation. The resolution: **there is no RunPriorityChain.** The async TTuple type (`AsyncMethodInterceptor<TDelegate, TArgs, TReturn>`) is a standalone sealed class that duplicates the async Invoke logic, exactly as `AsyncMethodInterceptor1<T1, TReturn>` does today.

**TReturn is the INNER type** (e.g., `int` for `Task<int>`), matching the existing async arity convention. The `Invoke` method returns `Task<TReturn>`.

The async TTuple type stores callbacks as `Func<TArgs, Task<TReturn>>` internally (NOT as TDelegate). Here is why and how:

**The async callback storage strategy:**

```csharp
public sealed class AsyncMethodInterceptor<TDelegate, TArgs, TReturn> where TDelegate : Delegate
{
    // Static invoker bridges
    private static readonly Func<TDelegate, TArgs, Task<TReturn>> s_asyncInvoker
        = DelegateInvokerFactory.BuildAsyncInvoker<TDelegate, TArgs, TReturn>();
    private static readonly Func<TReturn, TDelegate> s_valueDelegate
        = DelegateInvokerFactory.BuildValueDelegate<TDelegate, TReturn>();

    // Callbacks stored as Func<TArgs, Task<TReturn>> for internal use
    // (converted from TDelegate via s_asyncInvoker on entry)
    private Func<TArgs, Task<TReturn>>? _call;
    private MethodCallBuilder? _callTracking;

    // Fallback delegates stored as TDelegate (for SetFallback/SetSourceFallback)
    private TDelegate? _fallback;
    private TDelegate? _sourceFallback;

    // Return and Invoke follow AsyncMethodInterceptor1 exactly
    public MethodCallBuilder Return(TDelegate asyncCallback)
    {
        var builder = new MethodCallBuilder(this);
        _call = (args) => s_asyncInvoker(asyncCallback, args);
        _callTracking = builder;
        return builder;
    }

    // Simplified sync callback: accepts Func<TArgs, TReturn>, wraps in Task.FromResult
    public MethodCallBuilder Return(Func<TArgs, TReturn> callback)
    {
        return Return(WrapSync(callback));
    }

    public MethodCallBuilder Return(TReturn value)
    {
        var builder = new MethodCallBuilder(this);
        _hasReturnValue = true; _returnValue = value;
        _returnValueTracking = builder;
        return builder;
    }

    public async Task<TReturn> Invoke(bool strict, TArgs args)
    {
        // When chain
        if (_whenChain != null && _whenChainHead < _whenChain.Count)
        {
            var matcher = _whenChain[_whenChainHead];
            if (matcher.Matches(args))
            {
                matcher.CallCount++;
                if (_whenChainHead < _whenChain.Count - 1) _whenChainHead++;
                return await matcher.CallReturn(args).ConfigureAwait(false);
            }
            else if (matcher.IsTerminal) _whenChainHead++;
        }

        // Sequence
        if (_sequence != null && _sequenceIndex < _sequence.Count)
        {
            var (callback, tracking) = _sequence[_sequenceIndex];
            tracking.RecordCall(args);
            _sequenceIndex++;
            return await callback(args).ConfigureAwait(false);
        }

        // Return value
        if (_hasReturnValue && _returnValueTracking != null)
        {
            _returnValueTracking.RecordCall(args);
            return _returnValue;
        }

        // Callback
        if (_call != null && _callTracking != null)
        {
            _callTracking.RecordCall(args);
            return await _call(args).ConfigureAwait(false);
        }

        // Unconfigured path...
        _unconfiguredCallCount++;
        _unconfiguredLastArgs = args;

        // Fallback -- uses s_asyncInvoker to call TDelegate
        if (_fallback != null) return await s_asyncInvoker(_fallback, args).ConfigureAwait(false);
        if (_sourceFallback != null) return await s_asyncInvoker(_sourceFallback, args).ConfigureAwait(false);
        if (strict) throw StubException.NotConfigured("", _memberName);
        if (_defaultFactory != null) return _defaultFactory();
        return default!;
    }
}
```

**Key design point:** Internally, callbacks are stored as `Func<TArgs, Task<TReturn>>` (converted from TDelegate on entry via `s_asyncInvoker`). Fallback delegates are stored as raw `TDelegate` and invoked via `s_asyncInvoker` at call time. This matches how the current async arity types convert between `Func<T1, TReturn>` (simplified) and `Func<T1, Task<TReturn>>` (stored) forms.

**`DelegateInvokerFactory.BuildAsyncInvoker`** builds an expression tree: `(del, args) => del(args.Item1, args.Item2, ...)` where the return type is `Task<TReturn>`. The expression tree handles the TArgs-to-individual-params unpacking. The compiled delegate is `Func<TDelegate, TArgs, Task<TReturn>>`.

### Concern 4: Compositor Inner Class Names (RESOLVED)

**Resolution: Inner classes drop arity suffixes. `GetCallBuilderType` and `GetWhenBuilderType` are updated.**

The TTuple types use unsuffixed inner class names:

| Arity Type | TTuple Type |
|---|---|
| `MethodCallBuilder1` | `MethodCallBuilder` |
| `MethodSequence1` | `MethodSequence` |
| `WhenBuilder1` | `WhenBuilder` |
| `WhenChain1` | `WhenChain` |
| `VoidWhenBuilder1` | `VoidWhenBuilder` |
| `VoidWhenChain1` | `VoidWhenChain` |

**Changes to `PreCompiledInterceptorRenderer`:**

```csharp
// BEFORE:
public static string GetCallBuilderType(MethodOverloadSignature overload)
{
    var interceptorType = GetOverloadInterceptorType(overload);
    var paramCount = overload.Parameters.Count;
    return $"{interceptorType}.MethodCallBuilder{paramCount}";
}

// AFTER:
public static string GetCallBuilderType(MethodOverloadSignature overload)
{
    var interceptorType = GetOverloadInterceptorType(overload);
    var paramCount = overload.Parameters.Count;
    // Zero-param types retain arity suffix (MethodCallBuilder0)
    // TTuple types (1+ params) use unsuffixed names (MethodCallBuilder)
    if (paramCount == 0) return $"{interceptorType}.MethodCallBuilder0";
    return $"{interceptorType}.MethodCallBuilder";
}
```

Same pattern for `GetWhenBuilderType`: zero-param retains suffix, 1+ param drops suffix.

**Note:** The zero-param interceptors (`MethodInterceptor0`, etc.) are retained unchanged, so their inner class names (`MethodCallBuilder0`, `WhenBuilder0`, etc.) remain as-is. Only the TTuple types (1+ params) change to unsuffixed names.

### Concern 5: When Predicate Signature Breaking Change (RESOLVED)

**Resolution: Documented as a known breaking change. Ergonomics analysis shows the regression is real but has a C# mitigation.**

The developer correctly identifies two When breaking changes:

1. **Exact-value When:** `When(1, 2)` becomes `When((1, 2))` -- extra parens
2. **Predicate When:** `When((a, b) => a > 10)` becomes `When(args => args.a > 10)` -- tuple parameter

Both are source-breaking changes. The predicate change is more impactful because it changes how users write lambda bodies, not just parameter syntax.

**Ergonomics analysis:**

For 1-param methods, there is NO change:
```csharp
// Before and After (identical):
stub.Process.When("hello")
stub.Process.When(x => x.Length > 5)
```

For 2+ param methods:
```csharp
// Exact-value When:
// Before: stub.Add.When(1, 2)
// After:  stub.Add.When((1, 2))    -- one extra paren pair

// Predicate When:
// Before: stub.Add.When((a, b) => a > 10)
// After:  stub.Add.When(args => args.a > 10)     -- tuple deconstruction
// Alternative: stub.Add.When(((int a, int b) t) => t.a > 10)  -- explicit types
```

**C# tuple deconstruction mitigation:** Users can destructure in the lambda parameter:
```csharp
// Using deconstruction (requires explicit types in some cases):
stub.Add.When(((int a, int b) args) => args.a > 10)  // C# named tuple access
```

However, C# does NOT support deconstruction in lambda parameters like `((a, b)) => a > 10`. The tuple must be accessed via a single parameter name with dot notation.

**This is an acceptable trade-off** because:
1. Named tuple elements via IntelliSense (`args.a`, `args.b`) are more discoverable than unnamed positional params (`arg1`, `arg2`) in the current system
2. The When predicate is a relatively uncommon API surface compared to Return/Call
3. The exact-value When (`When((1, 2))`) is actually clearer about what's happening (constructing a match tuple)
4. KnockOff is pre-1.0 -- breaking changes are expected

**Documentation requirement:** The migration guide must show before/after examples for both exact-value and predicate When syntax.

### Concern 6: 8+ Parameter ValueTuple Nesting (RESOLVED)

**Resolution: Keep the 8-param limit. Do not remove the `> 8 params` check from `CanUsePreCompiled`.**

The developer is correct that `Expression.Field(argsParam, $"Item{i+1}")` breaks at 8 parameters due to ValueTuple nesting (`Rest` field). Handling nested ValueTuples in expression trees is technically possible but adds significant complexity for a rare edge case.

**Decision:** Keep the current 8-parameter limit. Methods with 9+ parameters continue to use the existing generated-subclass fallback path (`MethodInterceptorRenderer.RenderBaseClassContent` or `RenderSingleSignatureContent`). This is not a regression -- the current system also limits pre-compiled interceptors to 8 params.

**The original plan's claim "Remove >8 params limit from CanUsePreCompiled()" is withdrawn.** The `CanUsePreCompiled` method retains its `> 8 params` check.

Future work could support 8+ params by:
1. Having `BuildInvoker` recursively access `Rest.Item1`, `Rest.Item2`, etc. for params 8+
2. Using `ITuple` interface for dynamic access (but this involves boxing)
3. Or simply not supporting it -- 8+ param methods in interfaces are extremely rare

### Concern 7: Async Simplified Callback Overloads (RESOLVED)

**Resolution: Async TTuple types provide simplified overloads using `Func<TArgs, TReturn>` for sync callbacks, with `Task.FromResult` wrapping.**

The current async arity types provide:
```csharp
// AsyncMethodInterceptor1<T1, TReturn>:
public MethodCallBuilder1 Return(Func<T1, Task<TReturn>> asyncCallback)  // Full async
public MethodCallBuilder1 Return(Func<T1, TReturn> callback)            // Simplified sync
public MethodCallBuilder1 Return(TReturn value)                          // Value
```

The async TTuple types provide analogous overloads:
```csharp
// AsyncMethodInterceptor<TDelegate, TArgs, TReturn>:
public MethodCallBuilder Return(TDelegate asyncCallback)                 // Full async (TDelegate)
public MethodCallBuilder Return(Func<TArgs, TReturn> callback)          // Simplified sync
public MethodCallBuilder Return(TReturn value)                           // Value
```

**How the simplified sync overload works:**

```csharp
public MethodCallBuilder Return(Func<TArgs, TReturn> callback)
{
    // Convert sync callback to async internal form
    var asyncCallback = (TArgs args) => Task.FromResult(callback(args));
    var builder = new MethodCallBuilder(this);
    _call = asyncCallback;
    _callTracking = builder;
    return builder;
}
```

This does NOT require converting `Func<TArgs, TReturn>` to TDelegate. The simplified overload accepts `Func<TArgs, TReturn>` directly (not TDelegate) and wraps it in `Task.FromResult` to produce the internal `Func<TArgs, Task<TReturn>>` storage format.

**For SetFallback and SetSourceFallback on async types:**

The async TTuple types take `TDelegate` for SetFallback/SetSourceFallback (not simplified sync forms). The current `AsyncMethodInterceptor1` has two SetFallback overloads -- one async (`Func<T1, Task<TReturn>>`) and one sync (`Func<T1, TReturn>`). For TTuple, we simplify to a single `TDelegate`-based overload:

```csharp
public void SetFallback(TDelegate? fallback) => _fallback = fallback;
public void SetSourceFallback(TDelegate? sourceFallback) => _sourceFallback = sourceFallback;
```

The renderer already generates async-compatible lambdas for source fallback (see `GetMethodSourceFallbackLambdaExpression` which wraps ValueTask/async sources). For stub override fallback, `GetStubOverrideFallbackExpression` generates wrapping lambdas for ValueTask returns. Both patterns emit lambdas that match TDelegate's signature, so the single-overload approach works.

For async TTuple types, stub override methods return `Task<TReturn>` (the interface method's return type), so the fallback lambda naturally matches TDelegate's return type. No sync-to-async wrapping is needed because the stub override method itself is already async.

**Summary:** The async TTuple type provides:
1. `Return(TDelegate asyncCallback)` -- full form
2. `Return(Func<TArgs, TReturn> callback)` -- simplified sync, wraps in Task.FromResult
3. `Return(TReturn value)` -- value form
4. `SetFallback(TDelegate? fallback)` -- TDelegate-based (renderer generates matching lambdas)
5. `SetSourceFallback(TDelegate? fallback)` -- TDelegate-based (renderer generates matching lambdas)

The simplified `Return(Func<TArgs, TReturn>)` does NOT use TDelegate -- it uses `Func<TArgs, TReturn>` directly. This avoids the expression-tree-conversion problem the developer identified.

### Additional Concern: What WrapSync Looks Like

For the `Return(Func<TArgs, TReturn> callback)` simplified overload in async types, there is no need for expression tree magic. The wrapping is trivial:

```csharp
// In AsyncMethodInterceptor<TDelegate, TArgs, TReturn>:
public MethodCallBuilder Return(Func<TArgs, TReturn> callback)
{
    // Store as internal async form
    Func<TArgs, Task<TReturn>> asyncForm = (args) => Task.FromResult(callback(args));
    var builder = new MethodCallBuilder(this);
    _sequence = null; _sequenceIndex = 0;
    _isVerifiable = false; _verifiableTimes = null;
    _hasReturnValue = false; _returnValue = default!; _returnValueTracking = null;
    _call = asyncForm;
    _callTracking = builder;
    return builder;
}
```

`ThenReturn` chain entries similarly accept both forms.

### Revised Concrete Type Design

Below are the complete type signatures for all 4 TTuple types. Each is a standalone sealed class.

**`MethodInterceptor<TDelegate, TArgs, TReturn>`** (sync non-void, 1+ params):
```
Fields: _call (TDelegate?), _callTracking, _returnValue (TReturn), _sequence (List<(TDelegate, MethodCallBuilder)>),
        _whenChain, _fallback (TDelegate?), _sourceFallback (TDelegate?), _defaultFactory (Func<TReturn>?),
        _unconfiguredLastArgs (TArgs)
Static: s_invoker (Func<TDelegate, TArgs, TReturn>), s_valueDelegate (Func<TReturn, TDelegate>)
Invoke: TReturn Invoke(bool strict, TArgs args) -- uses s_invoker for delegate calls
API: Return(TDelegate), Return(TReturn), Return(TReturn, params TReturn[]),
     When(TArgs), When(Func<TArgs, bool>), SetFallback(TDelegate?), SetSourceFallback(TDelegate?)
Inner: MethodCallBuilder, MethodSequence, WhenBuilder, WhenChain
```

**`VoidMethodInterceptor<TDelegate, TArgs>`** (sync void, 1+ params):
```
Fields: _call (TDelegate?), _callTracking, _sequence (List<(TDelegate, MethodCallBuilder)>),
        _whenChain, _fallback (TDelegate?), _sourceFallback (TDelegate?),
        _unconfiguredLastArgs (TArgs)
Static: s_voidInvoker (Action<TDelegate, TArgs>)
Invoke: void Invoke(bool strict, TArgs args) -- uses s_voidInvoker for delegate calls
API: Call(TDelegate), When(TArgs), When(Func<TArgs, bool>),
     SetFallback(TDelegate?), SetSourceFallback(TDelegate?)
Inner: MethodCallBuilder, MethodSequence, VoidWhenBuilder, VoidWhenChain
```

**`AsyncMethodInterceptor<TDelegate, TArgs, TReturn>`** (async non-void, 1+ params):
```
Fields: _call (Func<TArgs, Task<TReturn>>?), _callTracking, _returnValue (TReturn),
        _sequence (List<(Func<TArgs, Task<TReturn>>, MethodCallBuilder)>),
        _whenChain, _fallback (TDelegate?), _sourceFallback (TDelegate?),
        _defaultFactory (Func<TReturn>?), _unconfiguredLastArgs (TArgs)
Static: s_asyncInvoker (Func<TDelegate, TArgs, Task<TReturn>>)
Invoke: async Task<TReturn> Invoke(bool strict, TArgs args) -- uses s_asyncInvoker for fallback calls
API: Return(TDelegate), Return(Func<TArgs, TReturn>), Return(TReturn), Return(TReturn, params TReturn[]),
     When(TArgs), When(Func<TArgs, bool>), SetFallback(TDelegate?), SetSourceFallback(TDelegate?)
Inner: MethodCallBuilder, MethodSequence, WhenBuilder, WhenChain
Note: _call and _sequence store Func<TArgs, Task<TReturn>> (converted from TDelegate on entry).
      _fallback/_sourceFallback store raw TDelegate (invoked via s_asyncInvoker at call time).
```

**`AsyncVoidMethodInterceptor<TDelegate, TArgs>`** (async void, 1+ params):
```
Fields: _call (Func<TArgs, Task>?), _callTracking,
        _sequence (List<(Func<TArgs, Task>, MethodCallBuilder)>),
        _whenChain, _fallback (TDelegate?), _sourceFallback (TDelegate?),
        _unconfiguredLastArgs (TArgs)
Static: s_asyncVoidInvoker (Func<TDelegate, TArgs, Task>)
Invoke: async Task Invoke(bool strict, TArgs args) -- uses s_asyncVoidInvoker for fallback calls
API: Call(TDelegate), Call(Action<TArgs>), When(TArgs), When(Func<TArgs, bool>),
     SetFallback(TDelegate?), SetSourceFallback(TDelegate?)
Inner: MethodCallBuilder, MethodSequence, VoidWhenBuilder, VoidWhenChain
Note: Same internal storage pattern as async non-void but without TReturn.
```

### DelegateInvokerFactory Methods

```csharp
internal static class DelegateInvokerFactory
{
    // Sync non-void: (del, args) => del(args.Item1, args.Item2, ...) : TReturn
    public static Func<TDelegate, TArgs, TReturn> BuildInvoker<TDelegate, TArgs, TReturn>()
        where TDelegate : Delegate;

    // Sync void: (del, args) => del(args.Item1, args.Item2, ...)
    public static Action<TDelegate, TArgs> BuildVoidInvoker<TDelegate, TArgs>()
        where TDelegate : Delegate;

    // Async non-void: (del, args) => del(args.Item1, args.Item2, ...) : Task<TReturn>
    // (Same as BuildInvoker but TReturn constraint is Task<T>)
    public static Func<TDelegate, TArgs, Task<TReturn>> BuildAsyncInvoker<TDelegate, TArgs, TReturn>()
        where TDelegate : Delegate;

    // Async void: (del, args) => del(args.Item1, args.Item2, ...) : Task
    public static Func<TDelegate, TArgs, Task> BuildAsyncVoidInvoker<TDelegate, TArgs>()
        where TDelegate : Delegate;

    // Value delegate: (value) => (args) => value  (creates TDelegate ignoring args)
    public static Func<TReturn, TDelegate> BuildValueDelegate<TDelegate, TReturn>()
        where TDelegate : Delegate;
}
```

Note: `BuildInvoker` and `BuildAsyncInvoker` can be the same method -- the expression tree is identical regardless of whether TReturn is `int` or `Task<int>`. The difference is only in the return type parameter. Similarly, `BuildVoidInvoker` and `BuildAsyncVoidInvoker` differ only in that the void delegate returns `void` vs `Task`. The expression tree for both calls `del(args.Item1, ...)` -- it just needs to know the delegate's Invoke signature.

In practice, all four invokers can be a single generic `BuildInvoker<TDelegate, TArgs, TResult>()` where TResult is the delegate's actual return type (TReturn, void, Task<TReturn>, or Task). The four distinct methods are just convenience wrappers for type safety.

### Revised Implementation Phases

**Phase 1: DelegateInvokerFactory**
- Implement `DelegateInvokerFactory` with expression tree builders
- Unit tests for each builder method with various delegate/tuple combos
- Verification: all factory unit tests pass

**Phase 2: Sync TTuple Types**
- `MethodInterceptor<TDelegate, TArgs, TReturn>` (copy from MethodInterceptor1, replace T1 with TArgs, Func<T1, TReturn> with TDelegate, add s_invoker)
- `VoidMethodInterceptor<TDelegate, TArgs>` (copy from VoidMethodInterceptor1, same transformation)
- Unit tests for each type
- Verification: library unit tests pass

**Phase 3: Async TTuple Types**
- `AsyncMethodInterceptor<TDelegate, TArgs, TReturn>` (copy from AsyncMethodInterceptor1, same transformation + async internal storage)
- `AsyncVoidMethodInterceptor<TDelegate, TArgs>` (copy from AsyncVoidMethodInterceptor1, same transformation)
- Unit tests for each type
- Verification: library unit tests pass

**Phase 4: IInterceptor Interface + Slot Interfaces + Extension Methods**
- Add `IInterceptor` interface with `CheckVerification()`, `CheckVerificationAll()`, `Reset()` methods
- Implement `IInterceptor` on all 8 interceptor types (4 TTuple types from Phases 2-3 + 4 zero-param types)
- Create 32 slot interfaces: `IVoidOverloadSlot{1-8}<TDelegate, TArgs>`, `IMethodOverloadSlot{1-8}<TDelegate, TArgs, TReturn>`, `IAsyncVoidOverloadSlot{1-8}<TDelegate, TArgs>`, `IAsyncMethodOverloadSlot{1-8}<TDelegate, TArgs, TReturn>`
- Create 4 extension classes: `VoidSlotExtensions`, `MethodSlotExtensions`, `AsyncVoidSlotExtensions`, `AsyncMethodSlotExtensions`
- Each extension class provides Call/Return, When, Verify forwarding methods per slot number
- Verification: prototype-level unit tests confirming extension method resolution works on compositors with multiple slots

**Phase 5: Generator/Renderer Changes**
- Update `PreCompiledInterceptorRenderer.GetMethodInterceptorType()` for TTuple types
- Update `PreCompiledInterceptorRenderer.GetMethodInvokeExpression()` for tuple args
- Update `GetCallBuilderType()` and `GetWhenBuilderType()` to drop arity suffixes
- Update `GetOverloadInterceptorType()` for TTuple types
- Update `GetMethodSourceFallbackExpression()` for TDelegate lambdas
- Update `GetStubOverrideFallbackExpression()` for TDelegate method groups/lambdas
- Update `GetMethodSourceFallbackClearExpression()` for TDelegate cast
- Add delegate type emission to all renderers
- Update Invoke expression to wrap args in tuples
- Update `RenderOverloadCompositorClass` to emit slot interface implementations instead of forwarding methods (see Compositor Slot Interface Design section)
- Emit `IReadOnlyList<IInterceptor> Interceptors` property in compositors
- Verification: all existing tests pass (after updating When syntax and LastArg references)

**Phase 6: Delete Arity Types + Test Migration**
- Delete all 32 arity files (MethodInterceptor{1-8}, VoidMethodInterceptor{1-8}, AsyncMethodInterceptor{1-8}, AsyncVoidMethodInterceptor{1-8})
- Update all test When(a, b) calls to When((a, b)) for 2+ params
- Update all test When((a, b) => expr) calls to When(args => args.a > ...) for 2+ params
- Verification: full build succeeds, all tests pass

**Phase 7: Design Project Verification + Benchmarks**
- Verify Design.Stubs compiles
- Verify Design.Tests passes
- Build time benchmark
- IntelliSense spot-check

### Revised Breaking Changes

1. **When syntax for 2+ params:** `When(1, 2)` becomes `When((1, 2))` (tuple literal)
2. **When predicate for 2+ params:** `When((a, b) => a > 10)` becomes `When(args => args.a > 10)` (tuple parameter)
3. **ThenWhen syntax:** Same changes as When
4. **Return/Call callback type:** `Func<T1, T2, TReturn>` becomes TDelegate (lambda syntax identical)
5. **SetFallback/SetSourceFallback type:** `Func<>` becomes TDelegate (renderer handles this)
6. **Inner class names in overload compositors:** `MethodCallBuilder1` becomes `MethodCallBuilder`, etc.
7. **LastArg for 1-param methods:** Type unchanged (TArgs = T1). Property name remains `LastArg` for 1-param. Changed to `LastArgs` for 2+ params with named tuple access (`.a`, `.b` instead of `.Item1`, `.Item2`).

All breaking changes are source-level. This requires a major version bump per the todo.

---

## Compositor Slot Interface Design

This section describes the optimization for overload compositor generated code, using numbered slot interfaces to move all behavioral forwarding methods (Call, Return, When, Verify, Reset) from generated code into pre-compiled library extension methods.

**Prototype validation:** 33 tests passing at `src/Prototypes/NumberedSlotPrototype/`. The prototype covers void and non-void slots, mixed-family compositors, edge cases, and IntelliSense simulation.

### Problem: Compositor Boilerplate

Without slot interfaces, each overload compositor must generate forwarding methods for every overload. For an `IOverloadedService.Process` method with 3 overloads, the compositor currently generates:
- 3 interceptor fields
- 3 Return/Call forwarding methods
- 3 When forwarding methods
- 3 Verify forwarding methods
- Reset/CheckVerification methods iterating all interceptors

This is pure boilerplate. With the TTuple approach, the forwarding methods take TDelegate/TArgs parameters that differ per overload -- they cannot be collapsed into a shared base method because C# resolves overloads by parameter type.

### Solution: Numbered Slot Interfaces

Each overload is assigned to a numbered slot (1 through 8). The compositor implements one slot interface per overload, and static extension methods on each slot forward to the interceptor.

**32 library interfaces (8 slots x 4 families):**

```csharp
// Void family
public interface IVoidOverloadSlot1<TDelegate, TArgs> where TDelegate : Delegate
{
    VoidMethodInterceptor<TDelegate, TArgs> VoidSlot1Interceptor { get; }
}
// ... through IVoidOverloadSlot8<TDelegate, TArgs>

// Non-void family
public interface IMethodOverloadSlot1<TDelegate, TArgs, TReturn> where TDelegate : Delegate
{
    MethodInterceptor<TDelegate, TArgs, TReturn> MethodSlot1Interceptor { get; }
}
// ... through IMethodOverloadSlot8<TDelegate, TArgs, TReturn>

// Async void family
public interface IAsyncVoidOverloadSlot1<TDelegate, TArgs> where TDelegate : Delegate
{
    AsyncVoidMethodInterceptor<TDelegate, TArgs> AsyncVoidSlot1Interceptor { get; }
}
// ... through IAsyncVoidOverloadSlot8<TDelegate, TArgs>

// Async non-void family
public interface IAsyncMethodOverloadSlot1<TDelegate, TArgs, TReturn> where TDelegate : Delegate
{
    AsyncMethodInterceptor<TDelegate, TArgs, TReturn> AsyncMethodSlot1Interceptor { get; }
}
// ... through IAsyncMethodOverloadSlot8<TDelegate, TArgs, TReturn>
```

**4 extension classes with forwarding methods per slot:**

```csharp
public static class VoidSlotExtensions
{
    // Slot 1
    public static ... Call<TDelegate, TArgs>(
        this IVoidOverloadSlot1<TDelegate, TArgs> self, TDelegate callback)
        where TDelegate : Delegate
        => self.VoidSlot1Interceptor.Call(callback);

    public static ... When<TDelegate, TArgs>(
        this IVoidOverloadSlot1<TDelegate, TArgs> self, TArgs args)
        where TDelegate : Delegate
        => self.VoidSlot1Interceptor.When(args);

    // ... Verify, Reset for Slot 1
    // ... Slots 2-8 follow same pattern
}

public static class MethodSlotExtensions { /* Same for Return/When/Verify per slot */ }
public static class AsyncVoidSlotExtensions { /* Same for Call/When/Verify per slot */ }
public static class AsyncMethodSlotExtensions { /* Same for Return/When/Verify per slot */ }
```

**Why this works:** Each compositor implements each slot interface at most once (because each overload is assigned a unique slot number). The C# compiler sees a single implementation of `IVoidOverloadSlot1<ProcessDelegate1, string>` on the compositor, so extension methods on that interface resolve unambiguously. This is in contrast to implementing the same generic interface multiple times (e.g., `IVoidOverload<ProcessDelegate1, string>` and `IVoidOverload<ProcessDelegate2, (string, int)>` on the same class), which causes CS1061.

**Slot numbering:** The generator assigns overloads to slots sequentially: first overload gets Slot1, second gets Slot2, etc. Slot numbering is per-family (void overloads get void slots, non-void overloads get method slots, etc.), as demonstrated by the MixedCompositor prototype.

### IInterceptor Interface for Verify/Reset

To eliminate the remaining generated Verify/Reset forwarding methods, all interceptor types implement a common `IInterceptor` interface:

```csharp
public interface IInterceptor
{
    void CheckVerification();
    void CheckVerificationAll();
    void Reset();
}
```

**Implemented by all 8 interceptor types:**
- `MethodInterceptor<TDelegate, TArgs, TReturn>` (TTuple)
- `VoidMethodInterceptor<TDelegate, TArgs>` (TTuple)
- `AsyncMethodInterceptor<TDelegate, TArgs, TReturn>` (TTuple)
- `AsyncVoidMethodInterceptor<TDelegate, TArgs>` (TTuple)
- `MethodInterceptor0<TReturn>` (zero-param)
- `VoidMethodInterceptor0` (zero-param)
- `AsyncMethodInterceptor0<TReturn>` (zero-param)
- `AsyncVoidMethodInterceptor0` (zero-param)

The compositor exposes a single generated property:

```csharp
public IReadOnlyList<IInterceptor> Interceptors => new IInterceptor[] { _interceptor1, _interceptor2, _interceptor3 };
```

Verify/Reset become library extension methods (or utility methods) that iterate the collection:

```csharp
public static class InterceptorExtensions
{
    public static void VerifyAll(IReadOnlyList<IInterceptor> interceptors)
    {
        foreach (var i in interceptors) i.CheckVerificationAll();
    }

    public static void ResetAll(IReadOnlyList<IInterceptor> interceptors)
    {
        foreach (var i in interceptors) i.Reset();
    }
}
```

### Updated Compositor Generated Code

**Example: `IOverloadedService.Process` with 3 void overloads**

`void Process(string data)`, `void Process(string data, int priority)`, `void Process(string data, int priority, bool async)`

**Before (current generated compositor):**
```csharp
public class ProcessCompositor
{
    // Fields
    private readonly VoidMethodInterceptor2<string> _interceptor1 = new("Process");
    private readonly VoidMethodInterceptor3<string, int> _interceptor2 = new("Process");
    private readonly VoidMethodInterceptor4<string, int, bool> _interceptor3 = new("Process");

    // Forwarding methods -- ALL generated
    public VoidMethodInterceptor2<string>.MethodCallBuilder2 Call(Action<string> callback) => _interceptor1.Call(callback);
    public VoidMethodInterceptor3<string, int>.MethodCallBuilder3 Call(Action<string, int> callback) => _interceptor2.Call(callback);
    public VoidMethodInterceptor4<string, int, bool>.MethodCallBuilder4 Call(Action<string, int, bool> callback) => _interceptor3.Call(callback);
    public VoidMethodInterceptor2<string>.VoidWhenBuilder2 When(string arg1) => _interceptor1.When(arg1);
    public VoidMethodInterceptor3<string, int>.VoidWhenBuilder3 When(string arg1, int arg2) => _interceptor2.When(arg1, arg2);
    public VoidMethodInterceptor4<string, int, bool>.VoidWhenBuilder4 When(string arg1, int arg2, bool arg3) => _interceptor3.When(arg1, arg2, arg3);
    // ... Verify/Reset methods for each interceptor
}
```

**After (TTuple + slot interfaces):**
```csharp
// Delegate types (generated)
delegate void ProcessDelegate1(string data);
delegate void ProcessDelegate2(string data, int priority);
delegate void ProcessDelegate3(string data, int priority, bool @async);

// Compositor (generated) -- NO forwarding methods
public class ProcessCompositor
    : IVoidOverloadSlot1<ProcessDelegate1, string>,
      IVoidOverloadSlot2<ProcessDelegate2, (string data, int priority)>,
      IVoidOverloadSlot3<ProcessDelegate3, (string data, int priority, bool @async)>
{
    // Interceptor fields (1 per overload)
    private readonly VoidMethodInterceptor<ProcessDelegate1, string> _interceptor1 = new("Process");
    private readonly VoidMethodInterceptor<ProcessDelegate2, (string data, int priority)> _interceptor2 = new("Process");
    private readonly VoidMethodInterceptor<ProcessDelegate3, (string data, int priority, bool @async)> _interceptor3 = new("Process");

    // Explicit interface property implementations (1 per overload)
    VoidMethodInterceptor<ProcessDelegate1, string>
        IVoidOverloadSlot1<ProcessDelegate1, string>.VoidSlot1Interceptor => _interceptor1;
    VoidMethodInterceptor<ProcessDelegate2, (string data, int priority)>
        IVoidOverloadSlot2<ProcessDelegate2, (string data, int priority)>.VoidSlot2Interceptor => _interceptor2;
    VoidMethodInterceptor<ProcessDelegate3, (string data, int priority, bool @async)>
        IVoidOverloadSlot3<ProcessDelegate3, (string data, int priority, bool @async)>.VoidSlot3Interceptor => _interceptor3;

    // IInterceptor collection (1 line)
    public IReadOnlyList<IInterceptor> Interceptors => new IInterceptor[] { _interceptor1, _interceptor2, _interceptor3 };
}
```

**What moved from generated to library:** All Call, When, Return, Verify, Reset forwarding methods. The compositor now contains ONLY structural declarations (fields, interface implementations, interceptor collection).

### Compositor Edge Cases

**CS0121 ambiguity -- same-family, same-signature overloads:** Impossible. C# does not permit two methods with identical parameter types in the same interface. The compiler rejects the interface definition, so a compositor for such an interface cannot exist.

**CS0121 ambiguity -- cross-family, same-TArgs overloads:** Impossible. Two overloads that differ only by return type (e.g., `void Process(string data)` and `int Process(string data)`) are invalid C# -- method overloads must differ by parameter types. Since TArgs is derived from parameter types, two overloads in different families cannot have the same TArgs unless they also have the same parameters, which makes them invalid C# overloads.

**Mixed-family compositors:** A compositor can implement both void and non-void slots. Slot numbering is per-family: void overloads are assigned to `IVoidOverloadSlot{N}`, non-void overloads to `IMethodOverloadSlot{N}`. This is validated in the prototype's `MixedCompositor` and `MixedFamilyTests`.

**8-overload limit:** The slot interface design supports up to 8 overloads per family per compositor. Methods with 9+ overloads of the same family would need a fallback strategy (generated forwarding methods for the excess). In practice, 8 overloads of the same method name in a single interface is vanishingly rare.

### Approaches Explored and Rejected

Three alternative approaches were explored during brainstorming before arriving at numbered slot interfaces:

**1. Default Interface Methods (DIM)**
- Generic interface `IVoidOverload<TDelegate, TArgs>` with default `Call`/`When` methods
- **Rejected:** Requires .NET Core 3.0+ runtime. Default methods are only accessible through an interface reference, not through the concrete type (`compositor.Call(...)` would not compile; `((IVoidOverload<D,A>)compositor).Call(...)` would). This breaks the KnockOff API ergonomics.

**2. Shared Generic Interface with Extension Methods**
- Single `IVoidOverload<TDelegate, TArgs>` interface. Compositor implements it multiple times (once per overload). Extension methods on the interface forward to the interceptor.
- **Rejected:** When a class implements the same generic interface with different type arguments (e.g., `IVoidOverload<D1, A1>` and `IVoidOverload<D2, A2>`), C# cannot resolve which instantiation the extension method should target. Results in CS1061. Confirmed via prototype at `src/Prototypes/ExtensionOverloadPrototype/`.

**3. Arity-Specific Slot Interfaces**
- Interfaces with individual type params per arity: `IVoidOverloadSlot1_2<TDelegate, T1, T2>` to eliminate tuple syntax from When.
- **Rejected:** Would require 8 slots x 8 arities x 4 families = 256 interfaces. More importantly, this approach loses named When params (IntelliSense shows `arg1, arg2` instead of named tuple elements), defeating the purpose of the TTuple design.

---

## Developer Re-Review (After Architect Resolutions + Compositor Design)

**Status:** Approved
**Reviewed:** 2026-02-15

### Why This Plan Is Approved

The architect's fundamental revision -- making TTuple types standalone sealed classes instead of base class inheritors -- resolves the three most serious original concerns (Concerns 2, 3, and 4) cleanly. The approach is a mechanical transformation of the existing arity types, replacing individual type parameters with TDelegate/TArgs and adding expression tree bridges. This is low-risk because the behavioral logic is identical.

The compositor slot interface design is validated by a working prototype (33 tests passing at `src/Prototypes/NumberedSlotPrototype/`). The numbered slot approach correctly avoids the CS1061 ambiguity that killed the shared-interface approach.

### Review of Architect's 7 Concern Resolutions

All 7 resolutions are sound:

1. **Concern 1 (Design.Stubs deferred):** Accepted. Generator output must change before Design.Stubs can verify. Post-implementation checklist is documented.

2. **Concern 2 (Fallback/source delegation):** Resolved definitively. Standalone sealed classes use the same SetFallback/SetSourceFallback pattern as the arity types. The Invoke method code at lines 1038-1084 is structurally identical to MethodInterceptor1.Invoke (confirmed at `/home/keithvoels/KnockOff/src/KnockOff/Interceptors/MethodInterceptor1.cs:112-183`). The renderer's `GetMethodSourceFallbackExpression` and `GetStubOverrideFallbackExpression` need only change the delegate type from `Func<>` to the generated delegate name -- the call structure is unchanged.

3. **Concern 3 (Async design):** Resolved. No async base classes needed. Async TTuple types are standalone sealed classes matching the structure of `AsyncMethodInterceptor1` (confirmed at `/home/keithvoels/KnockOff/src/KnockOff/Interceptors/AsyncMethodInterceptor1.cs:17-161`). Internal storage as `Func<TArgs, Task<TReturn>>` with TDelegate-to-internal conversion on entry is the same pattern the async arity types use.

4. **Concern 4 (Compositor inner class names):** Resolved. Inner classes drop arity suffixes. `GetCallBuilderType` and `GetWhenBuilderType` updated with zero-param special case.

5. **Concern 5 (When predicate breaking change):** Resolved. Documented as accepted trade-off. Named tuple elements are more discoverable than unnamed positional parameters.

6. **Concern 6 (8+ param ValueTuple nesting):** Resolved. 8-param limit retained. Not a regression.

7. **Concern 7 (Async simplified callbacks):** Resolved. Async types accept `Func<TArgs, TReturn>` directly (no TDelegate conversion needed). `Task.FromResult` wrapping is trivial.

### Review of Compositor Slot Interface Design

**Prototype validation:** Confirmed 33 tests passing at `src/Prototypes/NumberedSlotPrototype/`. The prototype covers:
- Void overloads (ProcessCompositor with 3 void overloads)
- Non-void overloads (CalculateCompositor with 2 non-void overloads)
- Mixed families (MixedCompositor with void + non-void)
- Single overload (SingleOverloadCompositor)
- Edge cases: same-TArgs different families, same-TArgs same family

**Edge case analysis confirmed:**
- Same-family/same-signature overloads: invalid C# (compiler rejects the interface)
- Cross-family/same-TArgs: extension methods become ambiguous (CS0121), resolvable via interface cast
- Same-family/same-TArgs/different-TDelegate: Call becomes ambiguous (CS0121), resolvable via explicit delegate or cast

**IInterceptor interface:** Sound design. Adding `CheckVerification()`, `CheckVerificationAll()`, `Reset()` to all 8 interceptor types (4 TTuple + 4 zero-param) enables collection-based Verify/Reset on compositors. The generated `IReadOnlyList<IInterceptor> Interceptors` property is one line.

### Observations (Non-Blocking)

These are items I noted during review. None are blocking, but the implementer should be aware of them:

1. **Compositor with zero-param overloads.** The slot interfaces require `<TDelegate, TArgs>` which zero-param interceptors do not have. If a compositor contains a mix of zero-param and 1+ param overloads of the same method name, the zero-param overload cannot use slot interfaces. The generated compositor would need to keep generated forwarding methods for zero-param overloads alongside slot interfaces for 1+ param overloads. The current Design.Domain interfaces do not have this pattern (all overloaded methods have 1+ params), so this is theoretical. The implementer should handle this edge case or document it as a limitation.

2. **Async slot extension methods need two Return overloads.** The current compositor generates both `Return(Func<T1,...,TN,TReturn>)` (simplified sync) and `Return(Func<T1,...,TN,Task<TReturn>>)` (full async) for async non-void overloads. The async method slot extension methods must similarly provide `Return(TDelegate callback)` (full async) and `Return(Func<TArgs, TReturn> callback)` (simplified sync). The prototype only covers sync slots. The implementer should prototype async extension methods to confirm overload resolution works correctly when TDelegate is a delegate returning Task<TReturn> and `Func<TArgs, TReturn>` is also available.

3. **Return type of extension methods.** The prototype returns `string` for simplicity. The real extension methods return inner class types like `MethodInterceptor<TDelegate, TArgs, TReturn>.MethodCallBuilder`. This should work since the generic type parameters flow through, but should be confirmed during implementation.

4. **Compositor Verifiable/IsVerifiable/TotalCallCount/UnconfiguredCallCount.** The current compositor generates these as aggregated properties/methods across all inner interceptors (lines 612-658 of PreCompiledInterceptorRenderer.cs). The plan's IInterceptor interface only covers CheckVerification/CheckVerificationAll/Reset. The remaining aggregated properties (IsVerifiable, IsConfigured, TotalCallCount, UnconfiguredCallCount) and the Verifiable() method would still need to be generated as forwarding code in the compositor. The plan should acknowledge that not ALL compositor boilerplate is eliminated -- only the behavioral methods (Call/Return/When/Verify/Reset) move to extension methods.

5. **Compositor When deduplication.** The current compositor deduplicates When methods across overloads with matching parameter types (lines 588-607). With slot interfaces, each overload gets its own When via its slot extension method. If two overloads share the same parameter types but different return types (e.g., `string Format(string input)` and `int Parse(string input)` as overloads), the When extension methods would both match `When("hello")`, causing CS0121. The current code handles this by generating a single When method that calls When on all matching interceptors. The slot approach would lose this deduplication. However, this only matters for overloads with identical parameter types but different return types, which is uncommon.

6. **Compositor source delegation wiring.** The current compositor rendering includes source delegation wiring for each inner interceptor (SetSourceFallback calls in the Source method). These need to continue being generated since they are per-overload and depend on the specific interface being delegated to. The plan's "what moved from generated to library" summary correctly does not include source delegation, but the implementer should be aware that compositor rendering still has significant generated code beyond the structural declarations.

### Review Summary

- Files examined: MethodInterceptor1.cs, AsyncMethodInterceptor1.cs, PreCompiledInterceptorRenderer.cs (full file), FlatRenderer.cs (SetFallback/SetSourceFallback sections), IMethodReturnBuilder.cs, prototype Library/ and Consumer/ files, prototype Tests/ files, VoidMethodInterceptorBase.cs, MethodInterceptorBase.cs, IFormatter.cs
- Questions checked: 16 of 16
- Devil's advocate items: 6 observations generated, all non-blocking
- Prototype validation: 33 tests confirmed passing

---

## Implementation Contract

**Created:** 2026-02-15
**Approved by:** knockoff-developer

### Design.Stubs Acceptance Criteria

Design.Stubs verification is deferred because this plan changes the generator output. Post-implementation, the following must compile and pass:

- [ ] `dotnet build src/Design/Design.Stubs` succeeds
- [ ] `dotnet test src/Design/Design.Tests` passes (all target frameworks)
- [ ] Generated .g.cs files show delegate declarations + TTuple field types for all 9 patterns
- [ ] IntelliSense shows named parameters on Return/Call callbacks (via TDelegate)
- [ ] IntelliSense shows named tuple elements on When for 2+ params (via TArgs)

### In Scope

**Phase 1: DelegateInvokerFactory**
- [ ] Create `src/KnockOff/Interceptors/DelegateInvokerFactory.cs` with expression tree builders: BuildInvoker, BuildVoidInvoker, BuildAsyncInvoker, BuildAsyncVoidInvoker, BuildValueDelegate
- [ ] Unit tests for DelegateInvokerFactory with various delegate/tuple combos (1-param raw type, 2+ param ValueTuple, void delegates, async delegates)
- [ ] Checkpoint: DelegateInvokerFactory unit tests pass

**Phase 2: Sync TTuple Types**
- [ ] Create `MethodInterceptor<TDelegate, TArgs, TReturn>` as standalone sealed class (transform from MethodInterceptor1: replace T1 with TArgs, Func<T1, TReturn> with TDelegate, add s_invoker)
- [ ] Create `VoidMethodInterceptor<TDelegate, TArgs>` as standalone sealed class (transform from VoidMethodInterceptor1)
- [ ] Inner classes: MethodCallBuilder, MethodSequence, WhenBuilder, WhenChain, VoidWhenBuilder, VoidWhenChain (no arity suffixes)
- [ ] Unit tests for each type
- [ ] Checkpoint: sync TTuple unit tests pass

**Phase 3: Async TTuple Types**
- [ ] Create `AsyncMethodInterceptor<TDelegate, TArgs, TReturn>` as standalone sealed class (transform from AsyncMethodInterceptor1 + async internal storage pattern)
- [ ] Create `AsyncVoidMethodInterceptor<TDelegate, TArgs>` as standalone sealed class (transform from AsyncVoidMethodInterceptor1)
- [ ] Provide Return(TDelegate asyncCallback), Return(Func<TArgs, TReturn> callback), Return(TReturn value), Return(TReturn first, params TReturn[]) overloads on async non-void type
- [ ] Provide Call(TDelegate asyncCallback), Call(Action<TArgs> callback) overloads on async void type
- [ ] Provide SetFallback(TDelegate?), SetSourceFallback(TDelegate?) on both async types (single overload each, renderer generates matching lambdas)
- [ ] Unit tests for each type
- [ ] Checkpoint: async TTuple unit tests pass

**Phase 4: IInterceptor Interface + Slot Interfaces + Extension Methods**
- [x] Create `IInterceptor` interface with CheckVerification(), CheckVerificationAll(), Reset()
- [x] Implement IInterceptor on all 8 interceptor types (4 TTuple + 4 zero-param)
- [x] Create 32 slot interfaces (8 slots x 4 families): IVoidOverloadSlot{1-8}, IMethodOverloadSlot{1-8}, IAsyncVoidOverloadSlot{1-8}, IAsyncMethodOverloadSlot{1-8}
- [x] Create 4 extension classes: VoidSlotExtensions, MethodSlotExtensions, AsyncVoidSlotExtensions, AsyncMethodSlotExtensions
- [x] Extension methods per slot: Call/Return (with async simplified overload), When (exact + predicate)
- [x] Prototype-level tests confirming extension method overload resolution for all 4 families
- [x] Checkpoint: slot interface tests pass

**Phase 5: Generator/Renderer Changes**
- [ ] Update `PreCompiledInterceptorRenderer.GetMethodInterceptorType()` to return TTuple type names for 1+ param methods
- [ ] Update `PreCompiledInterceptorRenderer.GetMethodInvokeExpression()` to emit tuple-wrapped args for 2+ params
- [ ] Update `PreCompiledInterceptorRenderer.GetOverloadInterceptorType()` to return TTuple types
- [ ] Update `PreCompiledInterceptorRenderer.GetCallBuilderType()` to drop arity suffix for 1+ params
- [ ] Update `PreCompiledInterceptorRenderer.GetWhenBuilderType()` to drop arity suffix for 1+ params
- [ ] Update `PreCompiledInterceptorRenderer.GetDelegateType()` or replace with generated delegate type name emission
- [ ] Update `PreCompiledInterceptorRenderer.GetMethodSourceFallbackExpression()` to use generated delegate type instead of Func<>
- [ ] Update `PreCompiledInterceptorRenderer.GetMethodSourceFallbackClearExpression()` for TDelegate-based null cast
- [ ] Update `PreCompiledInterceptorRenderer.GetStubOverrideFallbackExpression()` for TDelegate method groups/lambdas
- [ ] Add delegate type declaration emission to all renderers (FlatRenderer, InlineRenderer, StandaloneClassRenderer, ClassRenderer)
- [ ] Update `RenderOverloadCompositorClass` to emit slot interface implementations (interface list on class, explicit property implementations, IReadOnlyList<IInterceptor> Interceptors property)
- [ ] Keep generated forwarding methods for Verifiable, IsVerifiable, IsConfigured, TotalCallCount, UnconfiguredCallCount on compositors (IInterceptor only covers CheckVerification/CheckVerificationAll/Reset)
- [ ] Handle zero-param overloads within compositors: keep generated forwarding for zero-param, use slots for 1+ param
- [ ] Checkpoint: build succeeds, run full test suite

**Phase 6: Delete Arity Types + Test Migration**
- [ ] Delete 32 arity files: MethodInterceptor{1-8}.cs, VoidMethodInterceptor{1-8}.cs, AsyncMethodInterceptor{1-8}.cs, AsyncVoidMethodInterceptor{1-8}.cs
- [ ] Delete related arity-specific interface types if they become unused
- [ ] Update all test When(a, b) calls to When((a, b)) for 2+ params
- [ ] Update all test When((a, b) => expr) predicate calls to When(args => args.a > ...) for 2+ params
- [ ] Update any LastArg references that change for multi-param interceptors
- [ ] Checkpoint: full build succeeds, all tests pass

**Phase 7: Design Project Verification + Benchmarks**
- [ ] `dotnet build src/Design/Design.Stubs` succeeds
- [ ] `dotnet test src/Design/Design.Tests` passes
- [ ] Spot-check generated .g.cs files for delegate + TTuple patterns in all 9 pattern types
- [ ] Build time benchmark vs v0.49.0 (28.5s) and v0.50.0 (13.4s)
- [ ] IntelliSense spot-check on Return/Call/When

### Explicitly Out of Scope

- ref/out parameter support (future work -- TDelegate unlocks this but not implemented here)
- 9+ parameter methods (8-param limit retained)
- Changes to property, indexer, or event interceptors (already pre-compiled)
- Changes to base classes (VoidMethodInterceptorBase, MethodInterceptorBase -- untouched)
- Changes to MethodInterceptorRenderer (generated subclass fallback path -- untouched)
- Compositor `Return(TReturn value)` or `Return(TReturn, params TReturn[])` through extension methods (these are not currently exposed on compositors)
- NativeAOT compatibility (expression trees require runtime code generation)

### Verification Gates

1. **After Phase 1:** DelegateInvokerFactory tests pass for 1-param, 2-param, void, async, and value-delegate expression trees
2. **After Phase 2+3:** All 4 TTuple types have passing unit tests covering Return, Call, When (exact + predicate), sequence, verification, SetFallback, SetSourceFallback, and Invoke priority chain
3. **After Phase 4:** Slot interface extension methods resolve correctly for compositors with 2+ overloads across all 4 families; IInterceptor collection enables Verify/Reset
4. **After Phase 5:** All existing tests pass (with When syntax and inner class name updates). Generated .g.cs output verified for delegate + TTuple patterns.
5. **After Phase 6:** Full build succeeds with no remaining references to deleted arity types. All tests pass.
6. **Final (Phase 7):** `dotnet build src/Design/Design.Stubs` succeeds, `dotnet test src/Design/Design.Tests` passes on all target frameworks, build time benchmarked

### Stop Conditions

If any of these occur, STOP and report:
- Out-of-scope test fails that cannot be attributed to the expected breaking changes (When syntax, inner class names)
- Expression tree compilation fails for a delegate/tuple combo that is within the 8-param limit
- Compositor extension method resolution is ambiguous (CS0121) for a legitimate overload pattern (not same-TArgs edge case)
- Generated code does not compile after renderer changes
- Build time exceeds 20s (would indicate a performance regression beyond acceptable range)
- Async TTuple type's internal storage pattern produces incorrect results (wrong value returned, exception wrapping issues)

---

## Implementation Progress

**Started:** 2026-02-15

### Phase 1: DelegateInvokerFactory

- [x] Create `src/KnockOff/Interceptors/DelegateInvokerFactory.cs` with expression tree builders: BuildInvoker, BuildVoidInvoker, BuildAsyncInvoker, BuildAsyncVoidInvoker, BuildValueDelegate
- [x] Unit tests for DelegateInvokerFactory with various delegate/tuple combos (1-param raw type, 2+ param ValueTuple, void delegates, async delegates)
- [x] Checkpoint: DelegateInvokerFactory unit tests pass
- [x] Added `InternalsVisibleTo("KnockOffTests")` to KnockOff.csproj to enable testing internal factory
- [x] Fixed 8-param ValueTuple nesting: Item8 does not exist on ValueTuple<T1,...,T7,TRest> -- the 8th element is .Rest.Item1

**Verification:** All 21 DelegateInvokerFactory tests pass across net8.0, net9.0, net10.0. Full solution test suite passes with zero failures.

---

## Completion Evidence (Phase 1)

- **DelegateInvokerFactory Tests (21 tests, all passing):**
  - BuildInvoker: 1-param (raw type), 2-param (ValueTuple), 3-param, 8-param (max supported)
  - BuildVoidInvoker: 1-param, 2-param
  - BuildAsyncInvoker: 1-param, 2-param, sync delegate returning Task.FromResult, 8-param async
  - BuildAsyncVoidInvoker: 1-param, 2-param
  - BuildValueDelegate: 1-param, 2-param, different values produce distinct delegates
  - Edge cases: nullable string params (null and non-null), reference type params with ValueTuple, null value delegate, string value delegate

- **Full Solution Test Suite (zero failures):**
  - KnockOffTests: 1514 passed, 4 skipped (net9.0/net10.0); 1513 passed, 4 skipped (net8.0)
  - KnockOff.Documentation.Samples: 691 passed x 3 frameworks
  - KnockOff.NeatooInterfaceTests: 473 passed x 3 frameworks
  - KnockOffTests.AssemblyStrict: 14 passed x 3 frameworks
  - NumberedSlotPrototype: 33 passed (net9.0)

- **All Contract Items for Phase 1:** Confirmed complete

---

## Implementation Progress (Phase 4)

**Started:** 2026-02-15

### Phase 4: IInterceptor Interface + Slot Interfaces + Extension Methods

- [x] Create `IInterceptor` interface with `CheckVerification()`, `CheckVerificationAll()`, `Reset()` at `src/KnockOff/Interceptors/IInterceptor.cs`
  - **Note:** Plan specified `void` return types for CheckVerification/CheckVerificationAll. Actual interceptor methods return `VerificationFailure?`. Interface uses correct `VerificationFailure?` return types to match existing method signatures.
- [x] Implement IInterceptor on all 8 interceptor types (4 TTuple + 4 zero-param):
  - `MethodInterceptor<TDelegate, TArgs, TReturn> : IInterceptor`
  - `VoidMethodInterceptor<TDelegate, TArgs> : IInterceptor`
  - `AsyncMethodInterceptor<TDelegate, TArgs, TReturn> : IInterceptor`
  - `AsyncVoidMethodInterceptor<TDelegate, TArgs> : IInterceptor`
  - `MethodInterceptor0<TReturn> : IInterceptor`
  - `VoidMethodInterceptor0 : IInterceptor`
  - `AsyncMethodInterceptor0<TReturn> : IInterceptor`
  - `AsyncVoidMethodInterceptor0 : IInterceptor`
- [x] Create `InterceptorExtensions` with `VerifyAll` and `ResetAll` at `src/KnockOff/Interceptors/InterceptorExtensions.cs`
  - `VerifyAll` iterates interceptors, calls `CheckVerificationAll()`, throws `VerificationException` on first failure
  - `ResetAll` iterates interceptors, calls `Reset()` on each
- [x] Create 32 slot interfaces (8 slots x 4 families) in `src/KnockOff/Interceptors/Slots/`:
  - `IVoidOverloadSlots.cs` - IVoidOverloadSlot{1-8}<TDelegate, TArgs>
  - `IMethodOverloadSlots.cs` - IMethodOverloadSlot{1-8}<TDelegate, TArgs, TReturn>
  - `IAsyncVoidOverloadSlots.cs` - IAsyncVoidOverloadSlot{1-8}<TDelegate, TArgs>
  - `IAsyncMethodOverloadSlots.cs` - IAsyncMethodOverloadSlot{1-8}<TDelegate, TArgs, TReturn>
- [x] Create 4 extension classes in `src/KnockOff/Interceptors/Slots/`:
  - `VoidSlotExtensions.cs` - Call(TDelegate), When(TArgs), When(Func<TArgs,bool>) per slot 1-8
  - `MethodSlotExtensions.cs` - Return(TDelegate), Return(TReturn), When(TArgs), When(Func<TArgs,bool>) per slot 1-8
  - `AsyncVoidSlotExtensions.cs` - Call(TDelegate), Call(Action<TArgs>), When(TArgs), When(Func<TArgs,bool>) per slot 1-8
  - `AsyncMethodSlotExtensions.cs` - Return(TDelegate), Return(Func<TArgs,TReturn>), Return(TReturn), When(TArgs), When(Func<TArgs,bool>) per slot 1-8
- [x] Unit tests: 37 tests in `src/Tests/KnockOffTests/Interceptors/SlotExtensionTests.cs`
- [x] Checkpoint: all slot interface tests pass, all existing tests pass

**Verification:** All 37 slot extension tests pass across net8.0, net9.0, net10.0. Full solution test suite passes with zero failures.

---

## Completion Evidence (Phase 4)

- **Slot Extension Tests (37 tests, all passing):**
  - Void family: Call resolves to correct slot (slot 1, slot 2), When exact match (slot 1, slot 2), When predicate (slot 1)
  - Non-void family: Return callback (slot 1, slot 2), Return value via slot cast, When exact match (slot 1, slot 2), When->Return end-to-end
  - Async void family: Call TDelegate (slot 1, slot 2), Call Action<TArgs> sync shortcut (slot 1), When exact match (slot 1)
  - Async non-void family: Return TDelegate (slot 1, slot 2), Return Func<TArgs,TReturn> sync shortcut (slot 1), Return value via slot cast, When exact match (slot 1, slot 2), When->Return end-to-end
  - Mixed family: Call resolves to void slot, Return resolves to method slot, both resolve independently on same compositor
  - Single slot: Call, When, VerifyAll all work with a single interface implementation
  - IInterceptor collection: VerifyAll passes for unconfigured, VerifyAll passes for configured+called, VerifyAll throws for configured-not-called, ResetAll resets all interceptors, ResetAll works across mixed families
  - IInterceptor interface: All 8 types implement IInterceptor, CheckVerification returns failure when verifiable+not-called, CheckVerificationAll returns failure when configured+not-called, Reset clears tracking

- **Full Solution Test Suite (zero failures):**
  - KnockOffTests: 1711 passed, 4 skipped (net9.0/net10.0); 1710 passed, 4 skipped (net8.0)
  - KnockOff.Documentation.Samples: 691 passed x 3 frameworks
  - KnockOff.NeatooInterfaceTests: 473 passed x 3 frameworks

- **All Contract Items for Phase 4:** Confirmed complete

---

## Implementation Progress (Phase 2)

**Started:** 2026-02-15

### Phase 2: Sync TTuple Types

- [x] Create `src/KnockOff/Interceptors/MethodInterceptor.cs` — standalone sealed `MethodInterceptor<TDelegate, TArgs, TReturn>` with `where TDelegate : Delegate` constraint
- [x] Static expression tree invokers: `s_invoker` via `DelegateInvokerFactory.BuildInvoker`, `s_valueDelegate` via `DelegateInvokerFactory.BuildValueDelegate`
- [x] All delegate invocations replaced with `s_invoker(delegate, args)` and `s_valueDelegate(value)` calls
- [x] Inner classes renamed: `MethodCallBuilder` (not `MethodCallBuilder1`), `MethodSequence` (not `MethodSequence1`), `WhenBuilder` (not `WhenBuilder1`), `WhenChain` (not `WhenChain1`)
- [x] `LastArgs` property (not `LastArg`) returning `TArgs?`
- [x] `Invoke(bool strict, TArgs args)` with full priority chain using `s_invoker`
- [x] `Return(TDelegate callback)`, `Return(TReturn value)`, `Return(TReturn first, params TReturn[] rest)`
- [x] `When(TArgs args)`, `When(Func<TArgs, bool> predicate)`
- [x] `SetFallback(TDelegate?)`, `SetSourceFallback(TDelegate?)`
- [x] Implements `IMethodReturnBuilder<TDelegate, TArgs?>` on MethodCallBuilder, with explicit `IMethodTracking<TArgs?>.LastArg` implementation
- [x] Implements `IMethodReturnSequence<TDelegate>`, `IMethodReturnSequence`, `IMethodSequence` on MethodSequence
- [x] Create `src/KnockOff/Interceptors/VoidMethodInterceptor.cs` — standalone sealed `VoidMethodInterceptor<TDelegate, TArgs>` with `where TDelegate : Delegate` constraint
- [x] Static expression tree invoker: `s_voidInvoker` via `DelegateInvokerFactory.BuildVoidInvoker`
- [x] All delegate invocations replaced with `s_voidInvoker(delegate, args)` calls
- [x] Inner classes renamed: `MethodCallBuilder`, `MethodSequence`, `VoidWhenBuilder`, `VoidWhenChain`
- [x] `LastArgs` property returning `TArgs?`
- [x] `Invoke(bool strict, TArgs args)` void priority chain using `s_voidInvoker`
- [x] `Call(TDelegate callback)`
- [x] `When(TArgs args)`, `When(Func<TArgs, bool> predicate)`
- [x] `SetFallback(TDelegate?)`, `SetSourceFallback(TDelegate?)`
- [x] Implements `IMethodCallBuilder<TDelegate, TArgs?>` on MethodCallBuilder
- [x] Implements `IMethodCallSequence<TDelegate>`, `IMethodCallSequence`, `IMethodSequence` on MethodSequence
- [x] Unit tests for MethodInterceptor<TDelegate, TArgs, TReturn>: 38 tests covering Return value, Return delegate, When exact match, When predicate, sequences, LastArgs, SetFallback, SetSourceFallback, verification, strict mode, reset, default factory, TotalCallCount, IsConfigured, When chains (ThenWhen, ThenCall, ThenNone), builder tracking — both 1-param and 2-param cases
- [x] Unit tests for VoidMethodInterceptor<TDelegate, TArgs>: 37 tests covering Call, When exact match, When predicate, sequences, LastArgs, SetFallback, SetSourceFallback, verification, strict mode, reset, TotalCallCount, IsConfigured, When chains (ThenWhen, ThenCall, ThenNone), builder tracking — both 1-param and 2-param cases
- [x] Checkpoint: All new tests pass, full solution test suite passes with zero failures
- [x] No existing files modified — new types coexist alongside arity types

**Verification:** All 75 new TTuple interceptor tests pass across net8.0, net9.0, net10.0. Full solution test suite passes with zero failures.

---

## Completion Evidence (Phase 2)

- **New TTuple Interceptor Types (2 files created):**
  - `src/KnockOff/Interceptors/MethodInterceptor.cs` — `MethodInterceptor<TDelegate, TArgs, TReturn> where TDelegate : Delegate`
  - `src/KnockOff/Interceptors/VoidMethodInterceptor.cs` — `VoidMethodInterceptor<TDelegate, TArgs> where TDelegate : Delegate`

- **TTuple Interceptor Tests (75 tests, all passing across 3 target frameworks):**
  - MethodInterceptorTests (38 tests): Return with value (1-param, 2-param), Return with delegate callback (1-param, 2-param), When exact match (1-param, 2-param, no-match), When predicate (1-param, 2-param), sequences (values in order, repeat last, ThenDefault, ThenReturn chaining), LastArgs (1-param, 2-param, unconfigured), SetFallback (1-param, 2-param), SetSourceFallback, fallback precedence, verification (Verify, Verifiable/CheckVerification, CheckVerificationAll), strict mode (unconfigured, sequence exhausted), Reset, default factory, TotalCallCount, UnconfiguredCallCount, IsConfigured, When chain ThenWhen, ThenCall terminal, ThenNone, MethodCallBuilder tracking
  - VoidMethodInterceptorTests (37 tests): Call with delegate (1-param, 2-param), When exact match (1-param, 2-param), When predicate (1-param, 2-param), sequences (ThenCall, repeat last, ThenDefault), LastArgs (1-param, 2-param, unconfigured), SetFallback (1-param, 2-param), SetSourceFallback, fallback precedence, verification (Verify, Verifiable/CheckVerification, CheckVerificationAll), strict mode (unconfigured, sequence exhausted), Reset, TotalCallCount, UnconfiguredCallCount, IsConfigured, When chain ThenWhen, ThenCall terminal, ThenNone, MethodCallBuilder tracking

- **Full Solution Test Suite (zero failures):**
  - net8.0: 1592 passed, 4 skipped
  - net9.0: 1593 passed, 4 skipped
  - net10.0: 1593 passed, 4 skipped

- **No Existing Files Modified:** Both new types coexist alongside the arity types. No changes to any existing interceptor, base class, or test file.

- **All Phase 2 Contract Items:** Confirmed complete

---

## Implementation Progress (Phase 3)

**Started:** 2026-02-15

**Phase 3: Async TTuple Types**
- [x] Read AsyncMethodInterceptor1.cs and AsyncVoidMethodInterceptor1.cs as source templates
- [x] Read sync TTuple types (MethodInterceptor.cs, VoidMethodInterceptor.cs) for consistency
- [x] Read DelegateInvokerFactory.cs for async invoker APIs
- [x] Verify baseline build passes
- [x] Create AsyncMethodInterceptor<TDelegate, TArgs, TReturn> at src/KnockOff/Interceptors/AsyncMethodInterceptor.cs
- [x] Verify AsyncMethodInterceptor compiles
- [x] Create AsyncVoidMethodInterceptor<TDelegate, TArgs> at src/KnockOff/Interceptors/AsyncVoidMethodInterceptor.cs
- [x] Fix syntax errors (block lambda in tuple add required local variable extraction)
- [x] Verify AsyncVoidMethodInterceptor compiles
- [x] Create AsyncMethodInterceptorTests.cs (46 tests)
- [x] Create AsyncVoidMethodInterceptorTests.cs (39 tests)
- [x] Verify test project compiles
- [x] Run full test suite — all tests pass, zero failures
- [x] **Verification**: 85 new async TTuple tests pass across net8.0, net9.0, net10.0

---

## Completion Evidence (Phase 3)

- **New Async TTuple Interceptor Types (2 files created):**
  - `src/KnockOff/Interceptors/AsyncMethodInterceptor.cs` — `AsyncMethodInterceptor<TDelegate, TArgs, TReturn> where TDelegate : Delegate`
  - `src/KnockOff/Interceptors/AsyncVoidMethodInterceptor.cs` — `AsyncVoidMethodInterceptor<TDelegate, TArgs> where TDelegate : Delegate`

- **Internal Storage Strategy (per plan Concern 3 resolution):**
  - Callbacks (`_call`, sequence entries) stored as `Func<TArgs, Task<TReturn>>` / `Func<TArgs, Task>` internally (converted from TDelegate on entry via `s_asyncInvoker` / `s_asyncVoidInvoker`)
  - Fallback delegates (`_fallback`, `_sourceFallback`) stored as raw `TDelegate` and invoked via static invoker at call time

- **Return Overloads (AsyncMethodInterceptor):**
  1. `Return(TDelegate asyncCallback)` — full async form, converts via expression tree
  2. `Return(Func<TArgs, TReturn> callback)` — simplified sync, wraps in Task.FromResult
  3. `Return(TReturn value)` — value form
  4. `Return(TReturn first, params TReturn[] rest)` — sequence

- **Call Overloads (AsyncVoidMethodInterceptor):**
  1. `Call(TDelegate asyncCallback)` — full async form, converts via expression tree
  2. `Call(Action<TArgs> callback)` — simplified sync, wraps in Task.CompletedTask

- **ConfigureAwait(false):** Applied to all await expressions in Invoke methods, matching async arity-1 patterns

- **Interface Implementation:**
  - AsyncMethodInterceptor.MethodCallBuilder implements `IMethodReturnBuilder<TDelegate, TArgs?>`
  - AsyncMethodInterceptor.MethodSequence implements `IMethodReturnSequence<TDelegate>, IMethodReturnSequence, IMethodSequence`
  - AsyncVoidMethodInterceptor.MethodCallBuilder implements `IMethodCallBuilder<TDelegate, TArgs?>`
  - AsyncVoidMethodInterceptor.MethodSequence implements `IMethodCallSequence<TDelegate>, IMethodCallSequence, IMethodSequence`

- **Async TTuple Tests (85 tests, all passing across 3 target frameworks):**
  - AsyncMethodInterceptorTests (46 tests): Return with value (1-param, 2-param), Return with async delegate (1-param, 2-param), Return with sync callback (1-param, 2-param), When exact match (1-param, 2-param, no-match), When predicate (1-param, 2-param), sequences (values in order, repeat last, ThenDefault, ThenReturn with delegates, ThenReturn with sync callbacks), LastArgs (1-param, 2-param, unconfigured), SetFallback (1-param, 2-param), SetSourceFallback, fallback precedence, verification (Verify, Verifiable/CheckVerification, CheckVerificationAll), strict mode (unconfigured, sequence exhausted), Reset, default factory, TotalCallCount, UnconfiguredCallCount, IsConfigured, When chain ThenWhen, ThenCall terminal with delegate, ThenCall terminal with sync callback, ThenNone, MethodCallBuilder tracking, ConfigureAwait behavior
  - AsyncVoidMethodInterceptorTests (39 tests): Call with async delegate (1-param, 2-param), Call with sync callback (1-param, 2-param), When exact match (1-param, 2-param), When predicate (1-param, 2-param), When with sync callback, sequences (ThenCall async, ThenCall sync, repeat last, ThenDefault), LastArgs (1-param, 2-param, unconfigured), SetFallback (1-param, 2-param), SetSourceFallback, fallback precedence, verification (Verify, Verifiable/CheckVerification, CheckVerificationAll), strict mode (unconfigured, sequence exhausted), Reset, TotalCallCount, UnconfiguredCallCount, IsConfigured, When chain ThenWhen, ThenCall terminal, ThenNone, MethodCallBuilder tracking, ConfigureAwait behavior

- **Full Solution Test Suite (zero failures):**
  - net8.0: 1673 passed, 4 skipped (was 1592 before Phase 3 — +81 new tests)
  - net9.0: 1674 passed, 4 skipped (was 1593 before Phase 3 — +81 new tests)
  - net10.0: 1674 passed, 4 skipped (was 1593 before Phase 3 — +81 new tests)

- **No Existing Files Modified:** Both new async types coexist alongside the arity types. No changes to any existing interceptor, base class, or test file.

- **All Phase 3 Contract Items:** Confirmed complete
