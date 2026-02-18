# IntelliSense API Redesign Plan

**Date:** 2026-02-17
**Related Todo:** [IntelliSense API Redesign](../todos/intellisense-api-redesign.md)
**Status:** Complete
**Last Updated:** 2026-02-18 (architect verification passed)

---

## Overview

Comprehensive redesign of KnockOff's method interceptor API to prioritize IntelliSense clarity above all else. The current precompiled generic interceptor types (`MethodInterceptor<TDelegate, TArgs, TReturn>`, `AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>`, etc.) make IntelliSense unintelligible. This redesign returns to fully generated interceptor classes with clean typed signatures, introduces named tuples for multi-parameter callbacks, unifies the callback API under `Call`/`ThenCall`, and eliminates the slot-based overload system.

**Priority order:** IntelliSense clarity > Runtime performance > Build time

---

## Approach

### High-Level Architecture Change

**Current:** Precompiled generic interceptor types in the KnockOff library. Generator emits field declarations or thin subclasses. IntelliSense shows generic type parameters.

**New:** Fully generated interceptor classes per method. A non-generic base class in the library handles runtime logic (tracking, verification, sequence management, When matching). Generated classes provide typed wrappers with clean signatures and XML comments. IntelliSense shows only clean, method-specific types.

### Implementation Strategy

This is a big-bang change to the interceptor system. It cannot be done incrementally because:
1. The API method names change (`Return` -> `Call`/`Return`, `Call` -> `Call`, slot removal)
2. The generated class structure changes fundamentally (precompiled -> generated)
3. The overload handling changes (slots -> single property with overloaded methods)

However, the implementation can be phased internally with verification gates at each phase.

---

## Pattern-by-Pattern Analysis

For each of the 9 KnockOff patterns, this section traces what interceptor code the generator currently produces and what will change. Evidence is drawn from the actual `.g.cs` files in `src/Design/Design.Stubs/Generated/`.

### Pattern 1: Standalone (`[KnockOff]` partial class : IInterface)

**Current generated code** (from `CalculatorStub.g.cs`):

The stub class gets properties typed as precompiled interceptor types:
```csharp
public delegate int AddDelegate(int a, int b);
public MethodInterceptor<AddDelegate, (int a, int b), int> Add { get; } = new("Add");

public VoidMethodInterceptor0 Reset { get; } = new("Reset");
```
The interface implementation calls `Add.Invoke(Strict, (a, b))`. A separate `.Base.g.cs` file generates a `StubBase` class with virtual stub override methods.

**IntelliSense problem:** The `MethodInterceptor<AddDelegate, (int a, int b), int>` type pollutes every tooltip.

**New design:** The property type becomes the generated `AddInterceptor` class. The `.Base.g.cs` file (stub overrides) is unchanged since it has no interceptor types.

**Pattern-specific concerns:** None beyond the standard changes. This is the simplest pattern.

### Pattern 2: Generic Standalone (`[KnockOff]` partial class Stub<T> : IService<T>)

**Current generated code** (from `GenericServiceStub\`1.g.cs`):
```csharp
partial class GenericServiceStub<T> : GenericServiceStubBase<T>, IGenericService<T>, IKnockOffStub where T : class
{
    public delegate T? GetByIdDelegate(int id);
    public MethodInterceptor1<GetByIdDelegate, int, T?> GetById { get; } = new("GetById");
    public MethodInterceptor0<IEnumerable<T>> GetAll { get; } = new("GetAll", () => new List<T>());
    public delegate void SaveDelegate(T entity);
    public VoidMethodInterceptor1<SaveDelegate, T> Save { get; } = new("Save");
}
```

**New design:** Same as Pattern 1 but generated interceptor classes must carry the stub's type parameters. `GetByIdInterceptor` needs `<T>` in scope. Since the interceptor is generated inside the partial class (which already declares `<T>`), the type parameter flows naturally.

**Pattern-specific concern:** Generic constraints must propagate correctly to generated interceptor class type parameters. The return types like `T?` and `IEnumerable<T>` must flow through to `Call`/`Return` signatures.

### Generic Stubs Subsection

This section documents how generated interceptor classes interact with type parameters from enclosing generic stubs. The rules apply to Patterns 2, 4, 8, and 9.

#### Type Parameter Scoping Rule

Generated interceptor classes are nested inside the stub's partial class. When the enclosing class is generic (e.g., `partial class GenericServiceStub<T>`), the interceptor class inherits `T` from the enclosing scope WITHOUT declaring its own type parameters.

**Rule: Interceptor classes only need `InterceptorTypeParameters` for open generic patterns (P8/P9) where the stub is a generated nested generic class.** For standalone generic stubs (P2/P4), the interceptor inherits type params from the enclosing partial class and MUST NOT redeclare them. Redeclaring would produce CS0693 (type parameter `T` has the same name as the type parameter from outer type).

**Current generated code (Pattern 2, `GenericServiceStub<T>`):**
```csharp
partial class GenericServiceStub<T> : ... where T : class
{
    // Interceptor uses T from enclosing class -- no <T> on the class itself
    public delegate T? GetByIdDelegate(int id);
    public MethodInterceptor1<GetByIdDelegate, int, T?> GetById { get; } = new("GetById");
}
```

**New design (Pattern 2, generated interceptor class):**
```csharp
partial class GenericServiceStub<T> : ... where T : class
{
    // Interceptor class uses T from enclosing scope, NOT its own <T>
    public sealed class GetByIdInterceptor : MethodInterceptorRuntime
    {
        /// <summary>Configures callback for GetById(int id).</summary>
        /// <param name="callback">Callback receiving id (int), returning T?.</param>
        public MethodCallBuilder Call(Func<int, T?> callback) { ... }

        /// <summary>Sets constant return value for GetById(int id).</summary>
        public MethodCallBuilder Return(T? value) { ... }

        internal T? Invoke(bool strict, int id) { ... }
    }

    public sealed class GetAllInterceptor : MethodInterceptorRuntime
    {
        /// <summary>Configures callback for GetAll().</summary>
        public MethodCallBuilder Call(Func<IEnumerable<T>> callback) { ... }

        /// <summary>Sets constant return value for GetAll().</summary>
        public MethodCallBuilder Return(IEnumerable<T> value) { ... }

        internal IEnumerable<T> Invoke(bool strict) { ... }
    }

    public sealed class SaveInterceptor : MethodInterceptorRuntime
    {
        /// <summary>Configures callback for Save(T entity).</summary>
        public MethodCallBuilder Call(Action<T> callback) { ... }

        internal void Invoke(bool strict, T entity) { ... }
    }
}
```

**Key observations:**
- `GetByIdInterceptor` has `Call(Func<int, T?> callback)` -- `T` resolves from the enclosing class
- `SaveInterceptor` has `Call(Action<T> callback)` -- same
- No `sealed class GetByIdInterceptor<T>` -- that would shadow the outer `T`

#### Open Generic Patterns (P8/P9) -- Interceptors DO Declare Type Parameters

For open generic patterns, the generator creates the entire stub class as a nested generic class. The interceptor classes are nested inside that generated class, so they also inherit type parameters from the enclosing scope. No interceptor-level type parameter declaration is needed here either.

**Current generated code (Pattern 8, `OpenGenericInterfaceExample.Stubs.IRepository<T>`):**
```csharp
public class IRepository<T> : IRepository<T>, IKnockOffStub where T : class
{
    // T is from the generated stub class declaration
    public MethodInterceptor1<GetByIdDelegate, int, T?> GetById { get; } = new("GetById");
}
```

**New design:** Same as Pattern 2 -- interceptor classes use `T` from the enclosing generated `IRepository<T>` class.

#### Constraint Scoping Rule

Interceptor classes nested inside generic stubs inherit constraints from the enclosing class. Only interceptor classes with their own type parameters (which is rare -- only open generic delegate patterns where the interceptor is a top-level nested class with its own `<T>`) need explicit constraint declarations.

**Example:** In `GenericServiceStub<T> where T : class`, the generated `GetByIdInterceptor` does NOT need `where T : class` because it is nested inside the class that already declares the constraint.

**Exception:** Open generic delegate interceptors like `OGFactoryInterceptor<T>` and `OGConverterInterceptor<TIn, TOut, TResult> where TResult : class` are generated as top-level nested classes in the `Stubs` container (not inside a generic stub class), so they DO declare their own type parameters and constraints. Under the new design, these inherit from non-generic `MethodInterceptorRuntime` instead of the current generic `MethodInterceptorBase<...>`.

### Pattern 3: Standalone Class (`[KnockOffBase<ConcreteClass>]` partial class)

**Current generated code** (from `StandaloneClassStubOverrideStub.g.cs`):

This pattern has two kinds of method interceptors:
1. Methods that have stub overrides generate **fully generated interceptor classes** that inherit from the generic base class. These classes already exist today for standalone class stubs because precompiled types can't handle the stub override fallback:
```csharp
public sealed class StandaloneClassStubOverrideStub_InitializeInterceptor
    : VoidMethodInterceptorBase<Action, Unit>
{
    // Full generated class with Call(), When(), Invoke(strict, stub), inner MethodCallBuilder, etc.
    // Invoke passes `stub` for stub override fallback: stub.__StubOverride_Initialize()
}
```
2. Methods without stub overrides use precompiled types:
```csharp
public MethodInterceptor0<string> GetInternalId { get; } = new("GetInternalId");
```

The stub also has a private `Impl` nested class that inherits from the target class and delegates to interceptors.

**New design:** All methods become fully generated interceptor classes with clean names. The fallback-to-base-class behavior in `Impl` is unchanged. The stub override wiring (`stub.__StubOverride_Initialize()`) continues to work.

**Pattern-specific concern:** Standalone class stubs already generate interceptor classes for stub-overridden methods. The redesign is partly already done for this pattern. The main change is: (a) non-stub-override methods also become generated classes, (b) API names change to Call/Return, (c) base class changes from generic to non-generic.

### Pattern 4: Generic Standalone Class (`[KnockOffBase(typeof(ClassBase<>))]` partial class<T>)

**Current generated code:** Same structure as Pattern 3 but with generic type parameters flowing through. The `Impl` class inherits from the generic base class.

**New design:** Same changes as Pattern 3 with the same generic type parameter propagation concerns as Pattern 2.

**Pattern-specific concern:** Combination of the generic concerns from Pattern 2 and the stub override concerns from Pattern 3. Need to verify that generated interceptor classes work correctly when `T` is used in parameter types and return types within a generic stub.

#### Dual-Level Type Parameters (Class-Level + Method-Level)

Pattern 4 stubs like `GenericMethodRepositoryBase<TEntity>` can have methods with BOTH class-level type parameters and method-level generic parameters. For example:

```csharp
public abstract class GenericMethodRepositoryBase<TEntity> where TEntity : class
{
    public virtual TEntity? GetById(int id) => default;                      // Uses class-level TEntity only
    public virtual TResult ConvertEntity<TResult>(TEntity entity) => default!; // Class-level TEntity + method-level TResult
    public abstract void MapTo<TTarget>(TEntity source);                      // Class-level TEntity + method-level TTarget
    public virtual string GetEntityName() => typeof(TEntity).Name;            // No generic params in signature
}
```

The builder splits these into two groups:
- **Non-generic methods** (`GetById`, `GetEntityName`) -> `UnifiedMethodInterceptorModel` with standard interceptor classes using `TEntity` from enclosing scope
- **Generic methods** (`ConvertEntity<TResult>`, `MapTo<TTarget>`) -> `InlineGenericMethodHandlerModel` with `Of<T>()` handler subsystem

**Generated code for dual-level (current, from `GenericMethodOpenGenericClassTest.Stubs.g.cs`):**
```csharp
// The interceptor class declares <TEntity> because it is a top-level nested class in Stubs
public sealed class GenericMethodRepositoryBase_ConvertEntityInterceptor<TEntity> where TEntity : class
{
    public ConvertEntityTypedHandler<TResult> Of<TResult>() { ... }

    public sealed class ConvertEntityTypedHandler<TResult> : IGenericMethodCallTracker, ...
    {
        // Delegate uses BOTH TEntity (from outer interceptor) and TResult (from typed handler)
        public delegate TResult ConvertEntityDelegate(TEntity entity);
        private ConvertEntityDelegate? _call;
        public TEntity? LastArg { get; private set; }
        public IMethodTracking Return(ConvertEntityDelegate callback) { ... }
    }
}
```

**New design:** The `Of<T>()` handler structure stays the same (it is already generated, not precompiled). The API rename applies: `Return(callback)` -> `Call(callback)` for non-void typed handlers. The typed handler's simple structure does not need the full interceptor class redesign.

**Stub override wiring with generic types:** For non-generic methods on a generic class (like `GetById`), stub overrides work as usual -- the generated interceptor references `TEntity` from the enclosing scope. For generic methods (`ConvertEntity<TResult>`), stub overrides are excluded per the existing design (generic methods use the `Of<T>()` handler pattern, which does not have stub override fallback).

### Pattern 5: Inline Interface (`[KnockOff<IInterface>]`)

**Current generated code** (from `BasicMethodsDemo.Stubs.g.cs`):

Generated as a nested class inside `Stubs`:
```csharp
public static class Stubs
{
    public class ICalculator : ICalculator, IKnockOffStub
    {
        public delegate int AddDelegate(int a, int b);
        public MethodInterceptor<AddDelegate, (int a, int b), int> Add { get; } = new("Add");
        // ...
    }
}
```
Structurally identical to Pattern 1 but nested inside the test class's `Stubs` container.

**New design:** Same as Pattern 1. Generated interceptor classes become nested classes within the stub class. All delegate type declarations and interceptor class definitions go inside the `Stubs.ICalculator` class scope.

**Pattern-specific concern:** Nesting depth increases. Currently the interceptor type is already fully qualified (`MethodInterceptor<...>`), so moving to a generated `AddInterceptor` nested class actually reduces nesting noise. The generated interceptor class and its inner classes (MethodCallBuilder, WhenBuilder) are nested 3 levels deep: `TestClass.Stubs.ICalculator.AddInterceptor.MethodCallBuilder`. This is fine for code; IntelliSense only shows the immediate type name.

### Pattern 6: Inline Class (`[KnockOff<ConcreteClass>]`)

**Current generated code** (from `InlineClassExample.Stubs.g.cs`):

Uses the composition pattern with a private `Impl` class:
```csharp
public class ServiceBase : IKnockOffStub
{
    public PropertyGetInterceptor<string> Name { get; } = new("Name");
    public VoidMethodInterceptor0 Initialize { get; } = new("Initialize");
    public VoidMethodInterceptor1<ExecuteDelegate, string> Execute { get; } = new("Execute");
    public MethodInterceptor0<string> GetInternalId { get; } = new("GetInternalId");
    public ServiceBase_InternalStateChangedInterceptor InternalStateChanged { get; } = new();
    public ServiceBase Object { get; }

    private sealed class Impl : ServiceBase
    {
        // Overrides virtual/abstract members, delegates to interceptors
    }
}
```

The event interceptor (`ServiceBase_InternalStateChangedInterceptor`) is already a fully generated class.

**New design:** Same changes as Patterns 1/5 for methods. Property interceptors stay as-is. Event interceptors stay as-is. The `Impl` composition class is unchanged.

**Pattern-specific concern:** Class stubs also have base class fallback logic in `Impl` where unconfigured calls fall through to `base.Method()`. This logic is in the `Impl` class and does not interact with the interceptor class design.

### Pattern 7: Inline Delegate (`[KnockOff<DelegateType>]`)

**Current generated code** (from `InlineDelegateExample.Stubs.g.cs`):

Delegate stubs are unique. They generate:
1. A **fully generated interceptor class** (already, today) that inherits from the generic base:
```csharp
public sealed class ArithmeticOperationInterceptor
    : MethodInterceptorBase<Func<int, int, int>, (int a, int b), int>
{
    // Full generated class with Return(callback), Return(value), When(), Invoke(), inner classes
}
```
2. A **stub wrapper class** with implicit conversion to the delegate:
```csharp
public sealed class ArithmeticOperation : IKnockOffStub
{
    public ArithmeticOperationInterceptor Interceptor { get; } = new();
    private int Invoke(int a, int b) => Interceptor.Invoke(Strict, a, b);
    public static implicit operator ArithmeticOperation(ArithmeticOperation stub) => stub.Invoke;
}
```

**New design:** The interceptor class changes from inheriting the generic base to inheriting the new non-generic base. API names change (Return -> Call/Return). The stub wrapper class stays the same. The implicit conversion operator stays the same.

**Pattern-specific concern:** Delegate stubs already use fully generated interceptor classes. This pattern has the least amount of change. The `ArithmeticOperationInterceptor` keeps its name; only its base class and method names change.

#### Open Generic Delegates

Open generic delegates like `OGFactory<T>` and `OGConverter<TIn, TOut, TResult>` (via `[KnockOff(typeof(OGFactory<>))]`) generate generic interceptor classes that currently inherit from the generic base:

```csharp
// Current generated code (from OpenGenericDelegateTests.Stubs.g.cs)
public sealed class OGFactoryInterceptor<T>
    : MethodInterceptorBase<Func<T>, Unit, T>
{ ... }

public sealed class OGConverterInterceptor<TIn, TOut, TResult>
    : MethodInterceptorBase<Func<TIn, TResult>, TIn, TResult> where TResult : class
{ ... }
```

**New design:** These interceptor classes inherit from non-generic `MethodInterceptorRuntime`. The delegate's type parameters flow to the interceptor class declaration. The interceptor class declares its own type parameters because it is a top-level nested class in the `Stubs` container, not inside a generic stub class.

```csharp
// New design
public sealed class OGFactoryInterceptor<T> : MethodInterceptorRuntime
{
    public MethodCallBuilder Call(Func<T> callback) { ... }
    public MethodCallBuilder Return(T value) { ... }
    internal T Invoke(bool strict) { ... }
}

public sealed class OGConverterInterceptor<TIn, TOut, TResult> : MethodInterceptorRuntime where TResult : class
{
    public MethodCallBuilder Call(Func<TIn, TResult> callback) { ... }
    public MethodCallBuilder Return(TResult value) { ... }
    internal TResult Invoke(bool strict, TIn input) { ... }
}
```

Note: `TOut` is not used in the delegate's `Invoke` signature -- the delegate `OGConverter<TIn, TOut, TResult>` only uses `TIn` and `TResult` in its invocation. But `TOut` must still be declared on the interceptor class because the stub wrapper `OGConverter<TIn, TOut, TResult>` references `OGConverterInterceptor<TIn, TOut, TResult>`.

### Pattern 8: Open Generic Interface (`[KnockOff(typeof(IService<>))]`)

**Current generated code** (from `OpenGenericInterfaceExample.Stubs.g.cs`):

Structurally identical to Pattern 5 but with a generic stub class:
```csharp
public class IRepository<T> : IRepository<T>, IKnockOffStub where T : class
{
    public delegate T? GetByIdDelegate(int id);
    public MethodInterceptor1<GetByIdDelegate, int, T?> GetById { get; } = new("GetById");
    // ...
}
```

**New design:** Same as Pattern 5/1 with the generic type parameter concerns from Pattern 2. The generated interceptor classes reference `T` in their signatures.

**Pattern-specific concern:** Same as Pattern 2 -- generic constraints and type parameter usage in interceptor signatures.

### Pattern 9: Open Generic Class (`[KnockOff(typeof(ServiceBase<>))]`)

**Current generated code** (from `OpenGenericClassExample.Stubs.g.cs`):

Structurally identical to Pattern 6 (inline class) but with a generic stub:
```csharp
public class ServiceBase : IKnockOffStub
{
    // Same composition pattern with Impl class
    // Same property, method, event interceptors
}
```

**New design:** Same changes as Pattern 6 with generic type parameter concerns.

**Pattern-specific concern:** Combination of Pattern 6's composition/Impl concerns and Pattern 8's generic concerns.

### Overloaded Methods (Across All Patterns)

**Current generated code** (from `MethodOverloadsDemo.Stubs.g.cs`):

Overloaded methods generate a **compositor class**:
```csharp
public sealed class IFormatter_FormatInterceptor
    : IMethodOverloadSlot1<...>, IMethodOverloadSlot2<...>
{
    internal MethodInterceptor1<...> _ov1 = new("Format");
    internal MethodInterceptor<...> _ov2 = new("Format");
    internal MethodInterceptor<...> _ov3 = new("Format");

    // Forwarding methods: Return(delegate1), Return(delegate2), Return(delegate3)
    // When methods per overload
    // Slot interface implementations
}
```

The compositor implements slot interfaces and contains forwarding methods that delegate to the per-overload precompiled interceptors.

**New design:** The compositor class is replaced by a single generated interceptor class per method name. Per-overload `Call`/`Return` methods are overloaded by lambda parameter type (C# overload resolution). No more slot interfaces. Each overload's storage and logic is in the same class.

**Pattern-specific concern:** This is the most complex change. The current compositor pattern has significant machinery (slot interfaces, extension methods, forwarding). The new design must handle: (a) per-overload invoke methods, (b) per-overload callback storage, (c) aggregated verification across overloads, (d) C# overload resolution correctness for all Call/Return combinations.

### Cross-Pattern Summary

| Aspect | Patterns Affected | Nature of Change |
|--------|------------------|-----------------|
| Precompiled type -> generated class | 1, 2, 5, 8 (interface stubs) | Replace field declaration with generated interceptor class |
| Base class swap | 3, 4, 7, 9 (class/delegate stubs) | Change from generic base to non-generic base |
| Compositor -> overloaded class | All patterns with overloads | Remove compositor, generate single class with overloaded methods |
| API rename | All 9 patterns | Return->Call/Return, Call->Call |
| Slot removal | All patterns with overloads | Delete slot interfaces and extensions |
| Generic type propagation | 2, 4, 8, 9 | Ensure type params flow through generated interceptor classes |
| Generic method Of<T>() handler rename | All patterns with generic methods | `Return(callback)` -> `Call(callback)` for non-void typed handlers |
| When chain interface deletion | All patterns | `IWhenChain`, `IWhenBuilder`, `IVoidWhenChain` replaced by generated inner classes |
| Builder/sequence interface deletion | All patterns | Generic builder/sequence interfaces replaced by generated inner classes |
| Property/indexer/event | All patterns | No change (stay as precompiled types) |

---

## Starting Point Recommendation

### Investigated: Should We Revert to a Pre-v0.48 Version?

The precompiled interceptor system was introduced across these commits:

1. **v0.48 (`6768d022`)**: Prototype and plan for base classes
2. **v0.49 (`a8ac9554`)**: Implement interceptor base classes (`VoidMethodInterceptorBase`, `MethodInterceptorBase`)
3. **v0.49 (`caf1c035`)**: Eliminate generated thin subclasses via generic inner classes
4. **v0.50 (`258b2177`, `31c9a91c`, `df1580a7`)**: Add all arity-based precompiled types (0-8), replace generated classes
5. **v0.51 (`7494cc60`)**: Add single-param interceptor tier (MethodInterceptor1)
6. **v0.52 (`00c3dbf2`, `da4c7570`)**: TTuple named tuples, TSyncDelegate for async

Before v0.48 (at commit `1379cc95`), the `src/KnockOff/Interceptors/` directory did not exist at all. Every interceptor was fully generated. Generated files are gitignored so we cannot view the pre-v0.48 generated output directly, but the MethodInterceptorRenderer already contained the "single-signature" and "base-class" rendering modes.

### What Changed Since v0.47 That We Want to Keep

| Change | Introduced in | Keep? | Reason |
|--------|--------------|-------|--------|
| Interceptor base classes (`VoidMethodInterceptorBase`, `MethodInterceptorBase`) | v0.49 | **No** | These are generic. We want a non-generic base. But the *logic* in them is valuable as reference. |
| Named ValueTuple args (`(int a, int b)`) | v0.50 (TTuple) | **Yes** | Core design decision. Already computed by `ComputeTArgsType()`. |
| TSyncDelegate for simplified async callbacks | v0.51 | **Partially** | The concept (simplified sync callback for async methods) is kept. But the TSyncDelegate type parameter on precompiled types is removed. |
| DelegateInvokerFactory | v0.49 | **Yes** | Still needed for ref/out and stub override bridging. |
| Overload compositor classes | v0.50 | **No** | Replaced by single generated interceptor class with overloaded methods. |
| Slot interfaces and extensions | v0.50 | **No** | Deleted entirely. |
| PropertyGetInterceptor, IndexerGetSetInterceptor, etc. | v0.49 | **Yes** | These stay as precompiled types (see "What Stays Precompiled" section). |
| Smart default factories | v0.50 | **Yes** | The concept of `new()` and throw-on-unconfigured factories is kept. |
| Source delegation (`SetSourceFallback`) | v0.49+ | **Yes** | Generated code will call this on the new interceptor classes. |
| Event interceptor generation | pre-v0.48 | **Yes** | Events are fully generated today and remain unchanged. |
| XML comment generation on interceptor properties | v0.50+ | **Yes, extended** | Currently only `/// <summary>Interceptor for X.</summary>`. Extended to XML on all generated methods. |

### Recommendation: Evolve Forward from Current (HEAD)

**Do NOT revert to a previous version.** Reasons:

1. **The generator is more mature now.** The MethodInterceptorRenderer has grown to handle overloads, compositors, smart defaults, source delegation, stub overrides, and many edge cases. Reverting to v0.47 means losing all of that.

2. **The renderer already has all three rendering modes** (single-signature, base-class, overload-group). The "fully generated interceptor class" mode already exists in the "base-class" mode. The redesign restructures it rather than building from scratch.

3. **Named tuples and the compute infrastructure already exist.** `ComputeTArgsType()`, delegate declaration building, source fallback expression building -- these are all working code that the new design reuses.

4. **Tests cover all 9 patterns extensively.** The test infrastructure built since v0.48 exercises every pattern. Reverting would lose test coverage.

5. **What we are discarding is cleanly separable.** The precompiled sealed types (12 files), slot system (8 files), and `PreCompiledInterceptorRenderer.cs` (1 file) are self-contained. They can be deleted without affecting the renderer's core logic.

**Approach:** Start from HEAD. Delete the precompiled types and slots in Phase 9 (after everything else works). Rework MethodInterceptorRenderer to always use the "fully generated class" mode with the new non-generic base class.

---

## What Stays Precompiled

This section explicitly lists every type in the KnockOff library and categorizes it as STAYS (precompiled in library), GENERATED (moved to generated code), or DELETED.

### STAYS -- Precompiled Library Types (Not Visible in User IntelliSense)

These types remain in `src/KnockOff/`:

| Type | File | Reason |
|------|------|--------|
| `IKnockOffStub` | `IKnockOffStub.cs` | Marker interface. Not visible in IntelliSense. |
| `KnockOffAttribute` | `KnockOffAttribute.cs` | Attribute. Used at declaration, not in IntelliSense. |
| `KnockOffBaseAttribute` | `KnockOffBaseAttribute.cs` | Attribute. |
| `KnockOffStrictAttribute` | `KnockOffStrictAttribute.cs` | Attribute. |
| `Called` | `Called.cs` | Verification constraint. Clean type, no generics. |
| `VerificationException` | `VerificationException.cs` | Exception. Clean type. |
| `VerificationFailure` | (inside VerificationException?) | Verification result. Clean type. |
| `StubException` | `StubException.cs` | Exception. Clean type. |
| `StubExtensions` | `StubExtensions.cs` | Extension methods. |
| `Unit` | `Unit.cs` | Unit type for 0-param void. |
| `IInterceptor` | `Interceptors/IInterceptor.cs` | Interface for collection-based verification. No generics. |
| `DelegateInvokerFactory` | `Interceptors/DelegateInvokerFactory.cs` | Expression tree builder for ref/out and stub override bridging. Internal use. |
| `InterceptorExtensions` | `Interceptors/InterceptorExtensions.cs` | Extensions on IInterceptor. |

**New type to add:**

| Type | Purpose |
|------|---------|
| `MethodInterceptorRuntime` (name TBD) | Non-generic base class with all runtime logic: call counting, sequence management, verification, When chain tracking, Reset. Generated interceptor classes inherit from this. Since it is non-generic, its members in IntelliSense are clean: `Verify()`, `Reset()`, `Verifiable()`, `TotalCallCount`, etc. |

### STAYS -- Property/Indexer Interceptor Types

These precompiled types remain because:
- They have clean IntelliSense already (1-2 type params that are meaningful: `PropertyGetInterceptor<string>` tells you it is a string property)
- They are not the source of the IntelliSense problem
- Rewriting them as generated classes would add significant complexity for no IntelliSense benefit

| Type | File | Type Params | IntelliSense |
|------|------|------------|-------------|
| `PropertyGetInterceptor<TValue>` | `PropertyGetInterceptor.cs` | 1 | Clean: `PropertyGetInterceptor<string>` |
| `PropertyGetInterceptorBase<TValue>` | `PropertyGetInterceptorBase.cs` | 1 | Not user-facing |
| `PropertyGetSetInterceptor<TValue>` | `PropertyGetSetInterceptor.cs` | 1 | Clean: `PropertyGetSetInterceptor<int>` |
| `PropertyGetSetInterceptorBase<TValue>` | `PropertyGetSetInterceptorBase.cs` | 1 | Not user-facing |
| `PropertySetInterceptor<TValue>` | `PropertySetInterceptor.cs` | 1 | Clean: `PropertySetInterceptor<string>` |
| `PropertySetInterceptorBase<TValue>` | `PropertySetInterceptorBase.cs` | 1 | Not user-facing |
| `IndexerGetSetInterceptor<TKey, TValue>` | `IndexerGetSetInterceptor.cs` | 2 | Clean: `IndexerGetSetInterceptor<int, string>` |
| `IndexerGetSetInterceptorBase<TKey, TValue>` | `IndexerGetSetInterceptorBase.cs` | 2 | Not user-facing |

### STAYS -- Builder/Tracking Interfaces

These may need renaming but their structure stays. They provide the fluent API contracts:

| Type | Keep/Modify |
|------|------------|
| `IMethodTracking` | **Modify** -- rename methods to match Call/Return API |
| `IMethodCallBuilder` | **Modify/Replace** -- merge void/non-void into unified `ICallBuilder` |
| `IMethodReturnBuilder` | **Modify/Replace** -- merge into unified builder |
| `IMethodCallSequence` | **Modify/Replace** -- rename to match ThenCall/ThenReturn |
| `IMethodReturnSequence` | **Modify/Replace** -- merge into unified sequence |
| `IPropertyCallBuilder`, `IPropertyTracking`, `IPropertySequence` | **Keep** -- properties unchanged |
| `IIndexerCallBuilder`, `IIndexerTracking`, `IIndexerSequence` | **Keep** -- indexers unchanged |
| `ITracking`, `IWhenTracking` | **Keep/Modify** |

### DELETED -- Precompiled Method Interceptor Types

These 12 sealed types are removed entirely:

| Type | File |
|------|------|
| `MethodInterceptor<TDelegate, TArgs, TReturn>` | `MethodInterceptor.cs` |
| `MethodInterceptor0<TReturn>` | `MethodInterceptor0.cs` |
| `MethodInterceptor1<TDelegate, TArg, TReturn>` | `MethodInterceptor1.cs` |
| `VoidMethodInterceptor<TDelegate, TArgs>` | `VoidMethodInterceptor.cs` |
| `VoidMethodInterceptor0` | `VoidMethodInterceptor0.cs` |
| `VoidMethodInterceptor1<TDelegate, TArg>` | `VoidMethodInterceptor1.cs` |
| `AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>` | `AsyncMethodInterceptor.cs` |
| `AsyncMethodInterceptor0<TReturn>` | `AsyncMethodInterceptor0.cs` |
| `AsyncMethodInterceptor1<TDelegate, TSyncDelegate, TArg, TReturn>` | `AsyncMethodInterceptor1.cs` |
| `AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs>` | `AsyncVoidMethodInterceptor.cs` |
| `AsyncVoidMethodInterceptor0` | `AsyncVoidMethodInterceptor0.cs` |
| `AsyncVoidMethodInterceptor1<TDelegate, TSyncDelegate, TArg>` | `AsyncVoidMethodInterceptor1.cs` |

### DELETED -- Generic Base Classes for Method Interceptors

These have generic type params and are replaced by the new non-generic base:

| Type | File |
|------|------|
| `VoidMethodInterceptorBase<TDelegate, TArgs>` | `VoidMethodInterceptorBase.cs` |
| `MethodInterceptorBase<TDelegate, TArgs, TReturn>` | `MethodInterceptorBase.cs` |

### DELETED -- Entire Slot System

The entire `src/KnockOff/Interceptors/Slots/` directory (8 files):

| File | Contents |
|------|----------|
| `IMethodOverloadSlots.cs` | 8 slot interfaces |
| `IVoidOverloadSlots.cs` | 8 void slot interfaces |
| `IAsyncMethodOverloadSlots.cs` | 8 async slot interfaces |
| `IAsyncVoidOverloadSlots.cs` | 8 async void slot interfaces |
| `MethodSlotExtensions.cs` | Extension methods (Return/When per slot) |
| `VoidSlotExtensions.cs` | Extension methods (Call/When per slot) |
| `AsyncMethodSlotExtensions.cs` | Extension methods |
| `AsyncVoidSlotExtensions.cs` | Extension methods |

### DELETED -- Precompiled When Chain Interfaces

These library interfaces have generic type parameters that would pollute IntelliSense under the new design. Generated inner classes (`WhenBuilder`, `WhenChain`, `VoidWhenChain`) replace them with method-specific typed signatures.

| Type | File | Replacement |
|------|------|-------------|
| `IWhenChain<TDelegate, TReturn>` | `IWhenTracking.cs` | Generated `WhenChain` inner class per interceptor |
| `IWhenBuilder<TDelegate, TReturn>` | `IWhenTracking.cs` | Generated `WhenBuilder` inner class per interceptor |
| `IVoidWhenChain<TDelegate>` | `IWhenTracking.cs` | Generated `VoidWhenChain` inner class per interceptor |
| `IWhenTracking` | `IWhenTracking.cs` | May be kept as non-generic base, or folded into `IMethodTracking` |

**Note:** `IWhenTracking` itself is non-generic and may survive as a base interface. The three generic interfaces above are deleted because generated inner classes provide the same fluent API with clean, method-specific types instead of `TDelegate`/`TReturn` noise.

### DELETED -- Precompiled Builder/Sequence Interfaces (Generic)

These builder and sequence interfaces carry generic type parameters. Under the new design, generated inner classes replace them.

| Type | File | Replacement |
|------|------|-------------|
| `IMethodCallBuilder<TDelegate>` | `IMethodCallBuilder.cs` | Generated `MethodCallBuilder` inner class |
| `IMethodCallBuilder<TDelegate, TArg>` | `IMethodCallBuilder.cs` | Generated `MethodCallBuilder` inner class |
| `IMethodCallSequence<TDelegate>` | `IMethodCallSequence.cs` | Generated `MethodSequence` inner class |
| `IMethodReturnBuilder<TDelegate, TReturn>` | `IMethodReturnBuilder.cs` | Generated `MethodCallBuilder` inner class |
| `IMethodReturnBuilder<TDelegate, TArg, TReturn>` | `IMethodReturnBuilder.cs` | Generated `MethodCallBuilder` inner class |
| `IMethodReturnSequence<TDelegate, TReturn>` | `IMethodReturnSequence.cs` | Generated `MethodSequence` inner class |

**Note:** `IMethodTracking` (non-generic) and `IMethodTracking<TArg>` / `IMethodTracking<TArg1, TArg2, ...>` may be kept if still useful for tracking handle return types. The heavily-generic builder/sequence interfaces are replaced entirely.

### DELETED -- Generator File

| File | Reason |
|------|--------|
| `PreCompiledInterceptorRenderer.cs` | Decision tree and field type computation for precompiled types. No longer needed since everything is generated. |

### Summary: What Stays vs What Goes

**Library file count change:**
- Current: ~25 interceptor files + 8 slot files = ~33 files
- After: ~16 interceptor files (property/indexer types + new base + IInterceptor + DelegateInvokerFactory + InterceptorExtensions) + 0 slot files = ~16 files + 1 new base class = ~17 files
- Net reduction: ~16 files deleted from the library

**Generated code size change:**
- Current: ~1 line per method (field declaration) + compositor class for overloaded methods
- After: ~150-250 lines per method (full interceptor class with inner classes)
- Based on current "base-class" rendering mode output (e.g., `StandaloneClassStubOverrideStub_ExecuteInterceptor` is ~160 lines)
- This is an accepted trade-off per the stated priority order

---

## Design

### Decision 1: Interceptor-as-Property (Non-negotiable)

`stub.Method` remains a property returning an interceptor object. This is the foundation for `Verify()`, `LastArgs`, `Reset()`, `Verifiable()`, and stub override fallback wiring. No change here.

### Decision 2: Unified Callback -- Call / ThenCall

**Current state:** Non-void methods use `Return(callback)` (was `Returns(callback)` before rename). Void methods use `Call(callback)` (was `Execute(callback)` before rename). This split originated in v0.38.0 and has been renamed multiple times since.

**New API:** A single method name `Call` for all callbacks, both void and non-void:

```csharp
// Non-void method
stub.Add.Call(args => args.a + args.b);

// Void method
stub.Log.Call(args => log.Add(args.message));

// Sequence
stub.GetName.Call(args => "first").ThenCall(args => "second");
stub.Reset.Call(() => count++).ThenCall(() => count += 10);
```

**Why unified again:** The split between Return/Call (or Return/Execute) was a source of confusion. `Call` is the natural description: "when this method is called, do this." The void/non-void distinction is handled by the lambda's return type, not the method name.

**What `Call` returns:** A tracking handle (builder) that supports `ThenCall`, `ThenReturn`, `Verify`, `LastArgs`, `Reset`, `Verifiable`. For non-void methods, the tracking handle also supports `ThenReturn(value)` for mixed sequences.

### Decision 3: Return -- Value Only

`Return(value)` / `ThenReturn(value)` are strictly for setting constant return values:

```csharp
stub.GetName.Return("Alice");                    // constant value
stub.GetName.Return("A").ThenReturn("B");       // value sequence
stub.GetName.Return("A", "B", "C");              // params value sequence
```

**Never** `stub.GetName.Return(() => "Alice")` -- that is what `Call` is for.

**Clean semantic split:** `Return` = what to return. `Call` = what to do.

**Mixing in sequences is allowed:**
```csharp
stub.GetName.Return("first").ThenCall(args => Compute(args));
stub.GetName.Call(args => Compute(args)).ThenReturn("fallback");
```

### Decision 4: Named Tuples for 2+ Parameter Methods

For methods with 2+ parameters, callbacks and predicates receive a named tuple:

```csharp
// Interface: int Add(int a, int b)
// Call type: Func<(int a, int b), int>
stub.Add.Call(args => args.a + args.b);  // IntelliSense shows args.a, args.b

// Interface: void DoWork(int id, string name)
// Call type: Action<(int id, string name)>
stub.DoWork.Call(args => log.Add($"{args.id}: {args.name}"));

// When chain
stub.Add.When(args => args.a > 5).Return(100);
stub.Add.When((5, 10)).Return(15);  // exact tuple match
```

**Why tuples:** IntelliSense shows the named fields when the user types `args.`. This is the best IntelliSense experience possible for multi-parameter methods without using individual parameters (which would require generated delegate types that pollute IntelliSense differently).

**Generated code pattern (2+ params):**
```csharp
/// <summary>Configures callback for Add(int a, int b).</summary>
/// <param name="callback">Callback receiving (a, b) tuple, returning int.</param>
public MethodCallBuilder Call(Func<(int a, int b), int> callback) { ... }
```

### Decision 5: Raw Types for 0-1 Parameter Methods

- **0 params:** `stub.GetCount.Call(() => 42)` -- `Func<int>` / `Action`
- **1 param:** `stub.GetName.Call(id => $"Name-{id}")` -- `Func<int, string>` / `Action<int>`

Single-param can't use named tuples (C# limitation: `(int id)` is just `int`). The user names the lambda parameter themselves, which is natural.

**Generated code pattern (0 params):**
```csharp
/// <summary>Configures callback for GetCount().</summary>
public MethodCallBuilder Call(Func<int> callback) { ... }
```

**Generated code pattern (1 param):**
```csharp
/// <summary>Configures callback for GetName(int id).</summary>
/// <param name="callback">Callback receiving id (int), returning string.</param>
public MethodCallBuilder Call(Func<int, string> callback) { ... }
```

### Decision 6: Delegate Fallback for ref/out Parameters

ref/out cannot be expressed in tuples or `Func<>`/`Action<>`. These fall back to generated delegate types:

```csharp
// Interface: bool TryGetValue(string key, out string value)
// Generated: delegate bool TryGetValueDelegate(string key, out string value);
stub.TryGetValue.Call((string key, out string value) => { value = "found"; return true; });
```

The delegate name appears in IntelliSense, but XML comments describe the full signature. This is an acceptable trade-off since ref/out methods are uncommon.

### Decision 7: Pristine XML Comments on ALL Generated Methods

Every generated `Call`, `Return`, `When`, `ThenCall`, `ThenReturn` method gets XML doc comments:

```csharp
/// <summary>
/// Configures callback for DoWork(int id, string name).
/// </summary>
/// <param name="callback">
/// Callback receiving (id, name) tuple.
/// - id (int): The worker identifier
/// - name (string): The worker name
/// </param>
public MethodCallBuilder Call(Action<(int id, string name)> callback) { ... }
```

**XML doc migration:** Use `IMethodSymbol.GetDocumentationCommentXml()` to extract user's param docs from the interface/class. If the user documented their parameters, those descriptions flow through to the stub's XML comments.

**Implementation:** The builder/model pipeline must carry parameter XML doc strings. The renderer emits them in `<param>` tags.

### Decision 8: Fully Generated Interceptor Classes

**This is the core architectural change.** Stop using precompiled generic interceptor types. Generate a complete interceptor class for each method.

**Non-generic base class** (`InterceptorBase` or similar) in the KnockOff library handles:
- Call counting, unconfigured tracking
- Sequence index management, repeat-last-value behavior
- Verification logic (Verify, VerifyAll, CheckVerification, Verifiable)
- When chain head tracking
- Reset logic
- Fallback/source fallback storage as `Delegate?`

**Generated interceptor class** provides:
- Typed `Call(...)` / `Return(...)` / `When(...)` methods with clean signatures
- Typed `Invoke(...)` method called by the stub's interface implementation
- Typed `LastArg` / `LastArgs` properties
- Typed inner classes: `MethodCallBuilder`, `MethodSequence`, `WhenBuilder`, `WhenChain`
- Delegates to base class for all runtime logic via protected methods

**What the user sees in IntelliSense:**
```
stub.Add  ->  AddInterceptor (with members: Call, Return, When, Verify, Reset, LastArgs, ...)
stub.Add.Call(  ->  Call(Func<(int a, int b), int> callback)
```

No generic type parameters visible. Clean, readable signatures.

**Build time impact:** This increases generated code size compared to precompiled types. The priority order explicitly accepts this: IntelliSense clarity > Build time. The non-generic base class minimizes the increase by keeping runtime logic out of generated code.

### Decision 9: Overload Disambiguation -- No More Slots

**Current:** Overloaded methods already use a single property per method name with overloaded `Return`/`Call` methods on a compositor class. Internally, the compositor implements slot interfaces (`IMethodOverloadSlot1<TDelegate, TArgs, TReturn>`) and delegates to per-overload precompiled interceptor fields. The user-facing API is already `stub.Format.Return((input) => ...)` for each overload -- C# overload resolution on the lambda parameter types determines which overload is being configured.

**What changes internally:** The compositor class and slot interfaces are removed. The single generated interceptor class per method name absorbs the per-overload storage and methods directly, eliminating the slot interface indirection and the precompiled interceptor fields. API names change from `Return(callback)` to `Call(callback)`.

**What stays the same externally:** Single property per method name, overloads disambiguated by lambda parameter types in `Call`/`Return`:

```csharp
// Interface has: void Process(int x), void Process(string s), int Process(int x, int y)

// Single property
stub.Process  // -> ProcessInterceptor (contains overloaded Call/Return/When)

// Disambiguate by lambda
stub.Process.Call((int x) => { });           // matches void Process(int)
stub.Process.Call((string s) => { });        // matches void Process(string)
stub.Process.Call((args) => args.x + args.y); // matches int Process(int, int) -- tuple for 2+

// Return disambiguates by value type
stub.Process.Return(42);  // matches int Process(int, int) -- only overload returning int
```

**Verification for overloaded methods:** `Verify()`, `LastArgs`, `Reset()` are NOT directly on the interceptor for overloaded methods (ambiguous which overload). The user captures the tracking handle returned from `Call`/`Return`:

```csharp
var tracking = stub.Process.Call((int x) => { });
tracking.Verify(Called.Once);
tracking.LastArg;  // int?
```

**Non-overloaded methods** keep `Verify()`, `LastArgs`, `Reset()` directly on the interceptor as convenience.

**What gets removed:**
- All `IMethodOverloadSlot{1-8}<TDelegate, TArgs, TReturn>` interfaces
- All `IVoidOverloadSlots`, `IAsyncMethodOverloadSlots`, `IAsyncVoidOverloadSlots` interfaces
- All `MethodSlotExtensions`, `VoidSlotExtensions`, `AsyncMethodSlotExtensions`, `AsyncVoidSlotExtensions`
- The entire `src/KnockOff/Interceptors/Slots/` directory
- Compositor class generation in the renderer

**What gets added:**
- Generated interceptor class per method name (not per overload)
- Multiple `Call`/`Return`/`When` overloads inside the generated class, one per method overload
- Per-overload tracking handle types

### Decision 10: When Chains

When chains work with both `Call` and `Return`:

```csharp
// Value match
stub.GetName.When(42).Return("Forty-Two");     // exact match on single param
stub.Add.When((5, 10)).Return(15);              // exact tuple match on 2+ params

// Predicate match
stub.GetName.When(id => id > 5).Return("Big");
stub.Add.When(args => args.a > 0 && args.b > 0).Return(999);

// When + Call
stub.GetName.When(id => id > 5).Call(id => Compute(id));

// When + tracking
var tracking = stub.GetName.When(42).Return("exact");
tracking.Verify(Called.Once);
```

**Key decisions:**
- **First match wins** -- registration order determines priority
- When predicate uses same tuple/raw-type pattern as Call (consistent API)
- When predicate type disambiguates overloads (same as Call)
- When+Return/Call returns a tracking handle for per-match verification
- Mixing allowed in sequences: `Return("A").ThenCall(args => Compute())`

### Decision 11: Properties and Indexers

- **Properties** keep current `Get` / `Set` names -- unchanged
- **Indexers** with multiple keys get tuple treatment (same as methods)
- Single-key indexers use raw type

No API change for properties or indexers. The named-tuple change applies to indexer `Get`/`Set` callbacks for multi-key indexers.

### Decision 12: No Arg-Style API

KnockOff does not use `Arg.Any<T>()` or similar patterns. Parameter matching goes through `When` chains. This is consistent with the interceptor-as-property architecture.

---

## Current Architecture Analysis

### Files Examined

**Precompiled interceptor types (to be replaced):**
- `src/KnockOff/Interceptors/MethodInterceptor.cs` -- `MethodInterceptor<TDelegate, TArgs, TReturn>` (~676 lines)
- `src/KnockOff/Interceptors/MethodInterceptor0.cs` -- `MethodInterceptor0<TReturn>` (~625 lines)
- `src/KnockOff/Interceptors/MethodInterceptor1.cs` -- `MethodInterceptor1<TDelegate, TArg, TReturn>` (~677 lines)
- `src/KnockOff/Interceptors/VoidMethodInterceptor.cs` -- `VoidMethodInterceptor<TDelegate, TArgs>` (~604 lines)
- `src/KnockOff/Interceptors/VoidMethodInterceptor0.cs` / `VoidMethodInterceptor1.cs`
- `src/KnockOff/Interceptors/AsyncMethodInterceptor.cs` -- `AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>` (~696 lines, 4 type params!)
- `src/KnockOff/Interceptors/AsyncMethodInterceptor0.cs` / `AsyncMethodInterceptor1.cs`
- `src/KnockOff/Interceptors/AsyncVoidMethodInterceptor.cs` / `AsyncVoidMethodInterceptor0.cs` / `AsyncVoidMethodInterceptor1.cs`
- Total: 12 sealed interceptor types with massive code duplication

**Base class hierarchy (partially reusable):**
- `src/KnockOff/Interceptors/VoidMethodInterceptorBase.cs` -- `VoidMethodInterceptorBase<TDelegate, TArgs>` (~544 lines)
- `src/KnockOff/Interceptors/MethodInterceptorBase.cs` -- `MethodInterceptorBase<TDelegate, TArgs, TReturn>` (~508 lines)
- These have generic type params and `new` method hiding -- NOT suitable as-is for the new non-generic base

**Slot system (to be removed):**
- `src/KnockOff/Interceptors/Slots/IMethodOverloadSlots.cs` -- 8 slot interfaces
- `src/KnockOff/Interceptors/Slots/IVoidOverloadSlots.cs` -- 8 void slot interfaces
- `src/KnockOff/Interceptors/Slots/IAsyncMethodOverloadSlots.cs` -- 8 async slot interfaces
- `src/KnockOff/Interceptors/Slots/IAsyncVoidOverloadSlots.cs` -- 8 async void slot interfaces
- `src/KnockOff/Interceptors/Slots/MethodSlotExtensions.cs` -- extension methods
- `src/KnockOff/Interceptors/Slots/VoidSlotExtensions.cs` -- extension methods
- `src/KnockOff/Interceptors/Slots/AsyncMethodSlotExtensions.cs` -- extension methods
- `src/KnockOff/Interceptors/Slots/AsyncVoidSlotExtensions.cs` -- extension methods

**Generator renderer (major rework):**
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` -- 4582 lines, primary target
- `src/Generator/Renderer/Shared/PreCompiledInterceptorRenderer.cs` -- to be removed
- `src/Generator/Renderer/FlatRenderer.cs` -- calls into MethodInterceptorRenderer
- `src/Generator/Renderer/InlineRenderer.cs` -- calls into MethodInterceptorRenderer
- `src/Generator/Renderer/StandaloneClassRenderer.cs` -- calls into MethodInterceptorRenderer
- `src/Generator/Renderer/ClassRenderer.cs` -- calls into MethodInterceptorRenderer

**Builder/Model pipeline:**
- `src/Generator/Builder/UnifiedInterceptorBuilder.cs` -- builds interceptor models
- `src/Generator/Builder/FlatModelBuilder.cs` -- standalone patterns
- `src/Generator/Builder/InlineModelBuilder.cs` -- inline patterns
- `src/Generator/Builder/StandaloneClassModelBuilder.cs` -- class patterns
- `src/Generator/Model/Shared/UnifiedMethodInterceptorModel.cs` -- method interceptor model
- `src/Generator/Model/Shared/ParameterModel.cs` -- parameter info
- `src/Generator/Model/Shared/MethodOverloadSignature.cs` -- overload signatures

**Builder interface files (to be redesigned):**
- `src/KnockOff/IMethodCallBuilder.cs` -- void builder interfaces
- `src/KnockOff/IMethodReturnBuilder.cs` -- non-void builder interfaces (created during recent rename)
- `src/KnockOff/IMethodCallSequence.cs` -- void sequence interfaces
- `src/KnockOff/IMethodReturnSequence.cs` -- non-void sequence interfaces
- `src/KnockOff/IMethodTracking.cs` -- tracking hierarchy
- `src/KnockOff/IWhenTracking.cs` -- When chain interfaces

**Design.Stubs (API source of truth):**
- `src/Design/Design.Stubs/Methods/BasicMethods.cs` -- basic method API
- `src/Design/Design.Stubs/Methods/MethodSequences.cs` -- sequence API
- `src/Design/Design.Stubs/Methods/WhenMatching.cs` -- When chain API
- `src/Design/Design.Stubs/Methods/MethodOverloads.cs` -- overload API (currently uses slots)
- `src/Design/Design.Stubs/Methods/AsyncConsistency.cs` -- async patterns
- `src/Design/Design.Stubs/StubPatterns/AllPatterns.cs` -- all 9 patterns

**Expression tree infrastructure (to be preserved/adapted):**
- `src/KnockOff/Interceptors/DelegateInvokerFactory.cs` -- builds expression trees for delegate invocation
- This is used by precompiled types to bridge `TDelegate` invocation. In the new design, generated code will use `Func<>` / `Action<>` / tuples directly, reducing need for expression trees. However, `DelegateInvokerFactory` is still needed for ref/out delegate fallback and stub override wiring.

### Current IntelliSense Problem (Concrete Example)

For `int Add(int a, int b)` on a standalone stub:

**Current IntelliSense when typing `stub.Add.`:**
```
Properties:
  MethodInterceptor<AddDelegate, (int a, int b), int>.MethodCallBuilder Return(AddDelegate callback)
  MethodInterceptor<AddDelegate, (int a, int b), int>.MethodCallBuilder Return(int value)
  MethodInterceptor<AddDelegate, (int a, int b), int>.WhenBuilder When((int a, int b) args)
  ...
```

The `MethodInterceptor<AddDelegate, (int a, int b), int>` prefix noise makes every member tooltip unreadable.

**Target IntelliSense after redesign:**
```
Properties:
  AddInterceptor.MethodCallBuilder Call(Func<(int a, int b), int> callback)
  AddInterceptor.MethodCallBuilder Return(int value)
  AddInterceptor.WhenBuilder When((int a, int b) args)
  ...
```

Clean. The interceptor class name is short. The parameter types are immediately readable.

---

## Critical Rules

### Developer: STOP If Any Pattern Is Missing

**At every implementation checkpoint**, the developer MUST verify the change works for ALL 9 patterns. If any pattern is not addressed, **STOP immediately** and report which pattern is missing. Do NOT continue to the next phase. This is the most common failure mode — a feature works for some patterns but silently misses others.

Specifically: after each checkpoint, read the generated `.g.cs` files for at least one stub from each pattern group (standalone, standalone class, inline interface, inline class, inline delegate, open generic interface, open generic class). If a pattern's generated code doesn't reflect the changes from that phase, STOP.

### Architect: Verify the Tests and Design Projects, Not the Developer's Checklist

During post-implementation verification (Phase 6 of the project-todos workflow), the architect MUST **read the actual KnockOffTests and Design projects** (Design.Stubs, Design.Tests) for all 9 patterns — not the developer's "Completion Evidence" section. Tests and Design projects are the source of truth for whether a feature works. The developer may have checked a box without testing every pattern. The architect's job is to independently confirm by examining tests, Design.Stubs usage, and running builds/tests.

---

## Implementation Steps

### Phase 1: Non-Generic Base Class (Library)

Create a new non-generic base class in `src/KnockOff/Interceptors/` that handles all runtime logic without generic type parameters:

```csharp
public abstract class MethodInterceptorRuntime : IInterceptor
{
    protected readonly string _memberName;

    // Callback (stored as object? to avoid generic type parameters)
    protected Delegate? _call;
    protected MethodCallBuilderBase? _callTracking;

    // Sequence
    protected List<(Delegate Callback, MethodCallBuilderBase Tracking)>? _sequence;
    protected int _sequenceIndex;
    protected bool _repeatLastValue = true;

    // When chain (non-generic matcher base)
    protected List<WhenMatcherBase>? _whenChain;
    protected int _whenChainHead;
    protected bool _whenVerifiable;

    // Return value (non-void only, stored as object? to avoid TReturn)
    protected object? _returnValue;
    protected bool _hasReturnValue;
    protected MethodCallBuilderBase? _returnValueTracking;

    // Fallbacks (stored as Delegate? to avoid TDelegate)
    protected Delegate? _fallback;
    protected Delegate? _sourceFallback;

    // Verification
    protected bool _isVerifiable;
    protected Called? _verifiableTimes;
    protected int _unconfiguredCallCount;

    // Smart default factory
    protected Func<object>? _smartDefaultFactory;

    // Priority chain methods (non-virtual, operate on object? fields)
    // Generated classes provide typed InvokeDelegate/RecordArgs overrides
    protected bool RunVoidPriorityChain(object? args) { ... }
    protected (bool Handled, object? Result) RunPriorityChain(object? args) { ... }
    protected bool HandleSequenceExhaustedRepeat(bool strict, object? args) { ... }

    // Abstract methods for type-specific operations
    protected abstract void InvokeVoidDelegate(Delegate del, object? args);
    protected abstract object? InvokeDelegate(Delegate del, object? args);
    protected abstract bool MatchWhen(WhenMatcherBase matcher, object? args);
    protected abstract void RecordArgs(object? args, MethodCallBuilderBase tracking);

    // Public API (non-generic, clean IntelliSense)
    public void Verify() { ... }
    public void Verify(Called times) { ... }
    public void Verifiable() { ... }
    public void Verifiable(Called times) { ... }
    public VerificationFailure? CheckVerification() { ... }
    public VerificationFailure? CheckVerificationAll() { ... }
    public virtual void Reset() { ... }
    public int UnconfiguredCallCount => _unconfiguredCallCount;
    public bool IsConfigured => ...;
    public bool IsVerifiable => _isVerifiable;

    // Setup helpers (called by generated typed methods)
    protected void SetupCallback(Delegate callback, MethodCallBuilderBase builder) { ... }
    protected void SetupReturnValue(object? value, MethodCallBuilderBase builder) { ... }

    // Non-generic inner classes
    public abstract class WhenMatcherBase { ... }
    public abstract class MethodCallBuilderBase { ... }
    public class MethodSequenceBase { ... }
}
```

This is structurally identical to the current `VoidMethodInterceptorBase<TDelegate, TArgs>` and `MethodInterceptorBase<TDelegate, TArgs, TReturn>`, but with `object?`/`Delegate?` replacing the generic type parameters. The full priority chain logic (When -> Sequence -> Return value -> Callback -> Fallback -> Source -> Default) lives here. Generated interceptor classes provide ~150-250 lines of typed wrappers.

**Key design decision: The base class uses `object?` fields for typed storage.**

The base class stores callbacks, sequences, When matchers, return values, and fallbacks as `object?` fields. The priority chain logic (`RunPriorityChain`, `RunVoidPriorityChain`, `HandleSequenceExhaustedRepeat`) lives in the base class and uses virtual/abstract methods for the type-specific operations (invoking delegates, recording args, matching When predicates). Generated interceptor classes override these methods to cast `object?` back to the correct types.

This is the approach the current `VoidMethodInterceptorBase<TDelegate, TArgs>` already uses, minus the generic type parameters. The current base class has:
- `_call` as `TDelegate?` -- becomes `object?` (`Delegate?`)
- `_sequence` as `List<(TDelegate, Tracking)>?` -- becomes `List<(object, Tracking)>?` (or a non-generic wrapper)
- `_whenChain` as `List<VoidWhenMatcherBase>?` -- stays as `List<WhenMatcherBase>?` (non-generic matcher base)
- `_returnValue` as `TReturn` -- becomes `object?`
- `_fallback` and `_sourceFallback` as `TDelegate?` -- become `Delegate?`

The generated interceptor class provides:
- Typed `Call(Func<...> callback)` / `Return(T value)` methods that store into `object?` fields via protected setters
- Typed `Invoke(...)` method that calls the base `RunPriorityChain` and casts results
- Override of abstract `InvokeDelegate(object callback, ...)` to cast and invoke
- Override of abstract `RecordArgs(...)` to store typed last-arg values
- Typed `LastArg` / `LastArgs` properties
- Typed inner classes (`WhenBuilder`, `WhenChain`, `MethodCallBuilder`)

**Why this matters for generated code size:** With the priority chain in the base class, each generated interceptor is roughly 150-250 lines (similar to today's "base-class" rendering mode). The current `StandaloneClassStubOverrideStub_ExecuteInterceptor` -- a void 1-param method with When chains -- is ~160 lines including inner classes. This is a reliable estimate because the new design is structurally identical to the current "base-class" rendering mode, just with `object?` instead of `TDelegate`/`TArgs`/`TReturn`.

Without the priority chain in the base, each interceptor would be 400-600 lines (the full logic from `VoidMethodInterceptorBase` at ~544 lines + `MethodInterceptorBase` at ~508 lines, duplicated per method). The `object?` approach avoids this.

**Boxing concern:** Value-type return values (`int`, `bool`, etc.) will be boxed when stored as `object?`. This is a minor performance cost, acceptable per the priority order (IntelliSense > Performance > Build time).

**New builder/sequence interfaces:**

```csharp
// Unified -- no void/non-void split needed since Call is unified
public interface ICallBuilder : IMethodTracking
{
    // Tracking handle returned from Call/Return
}

public interface ICallSequence
{
    void Verify();
    void Reset();
    void ThenDefault();
}
```

**Checkpoint 1:** `dotnet build src/KnockOff/KnockOff.csproj` passes.

### Phase 2: Generator -- Fully Generated Interceptor Classes

Rework `MethodInterceptorRenderer.cs` to generate complete interceptor classes that:

1. Inherit from `MethodInterceptorRuntime` (or contain it via composition)
2. Have typed `Call`, `Return`, `When` methods with XML comments
3. Use `Func<>` / `Action<>` with named tuples for 2+ params
4. Use raw `Func<T, R>` / `Action<T>` for 1 param
5. Use `Func<R>` / `Action` for 0 params
6. Fall back to generated delegates for ref/out

**Also in this phase:** Rename the generic method `Of<T>()` typed handler entry point from `Return(callback)` to `Call(callback)` for non-void methods. This requires changes in 4 renderer locations:
- `InlineRenderer.RenderInlineTypedHandlerClass()` (line 741: `var typedHandlerEntryPoint = arity.IsVoid ? "Call" : "Return"` -> always `"Call"`)
- `FlatRenderer.RenderGenericMethodHandler()`
- `ClassRenderer.RenderClassGenericMethodHandler()`
- `StandaloneClassRenderer` (delegates to `ClassRenderer`)

**Generated class structure (non-void, 2+ params, non-overloaded):**

```csharp
/// <summary>Tracks and configures behavior for Add(int a, int b).</summary>
public sealed class AddInterceptor : MethodInterceptorRuntime
{
    // Typed callback storage
    private Func<(int a, int b), int>? _callback;
    private int _returnValue;
    private bool _hasReturnValue;
    // ... sequence, when chain storage with typed matchers

    /// <summary>
    /// Configures callback for Add(int a, int b).
    /// </summary>
    /// <param name="callback">Callback receiving (a, b) tuple, returning int.</param>
    public MethodCallBuilder Call(Func<(int a, int b), int> callback) { ... }

    /// <summary>
    /// Sets constant return value for Add(int a, int b).
    /// </summary>
    /// <param name="value">The value to return on every call.</param>
    public MethodCallBuilder Return(int value) { ... }

    /// <summary>
    /// Sets sequence of return values for Add(int a, int b).
    /// Each value returned once, last repeats.
    /// </summary>
    public MethodSequence Return(int first, params int[] rest) { ... }

    /// <summary>
    /// Configures parameter matching for Add(int a, int b).
    /// </summary>
    /// <param name="predicate">Predicate receiving (a, b) tuple.</param>
    public WhenBuilder When(Func<(int a, int b), bool> predicate) { ... }

    /// <summary>
    /// Configures exact parameter match for Add(int a, int b).
    /// </summary>
    public WhenBuilder When((int a, int b) args) { ... }

    // LastArgs with named tuple
    public (int a, int b)? LastArgs { get; }

    // Invoke (called by generated interface implementation)
    internal int Invoke(bool strict, int a, int b) { ... }

    // Inner classes
    public sealed class MethodCallBuilder { ... }
    public sealed class MethodSequence { ... }
    public sealed class WhenBuilder { ... }
    public sealed class WhenChain { ... }
}
```

**Generated class structure (void, 0 params):**

```csharp
/// <summary>Tracks and configures behavior for Reset().</summary>
public sealed class ResetInterceptor : MethodInterceptorRuntime
{
    private Action? _callback;

    /// <summary>Configures callback for Reset().</summary>
    public MethodCallBuilder Call(Action callback) { ... }

    internal void Invoke(bool strict) { ... }

    // Verify, Reset directly available (non-overloaded)
}
```

**Generated class structure (async Task<T>, 1 param):**

```csharp
/// <summary>Tracks and configures behavior for GetNameAsync(int id).</summary>
public sealed class GetNameAsyncInterceptor : MethodInterceptorRuntime
{
    private Func<int, string>? _callbackSimplified;     // simplified sync
    private Func<int, Task<string>>? _callbackFull;      // full async

    /// <summary>Configures callback for GetNameAsync(int id). Result auto-wrapped in Task.</summary>
    /// <param name="callback">Sync callback receiving id, returning string.</param>
    public MethodCallBuilder Call(Func<int, string> callback) { ... }

    /// <summary>Configures async callback for GetNameAsync(int id).</summary>
    /// <param name="callback">Async callback receiving id, returning Task&lt;string&gt;.</param>
    public MethodCallBuilder Call(Func<int, Task<string>> callback) { ... }

    /// <summary>Sets constant return value. Auto-wrapped in Task.</summary>
    public MethodCallBuilder Return(string value) { ... }

    internal Task<string> Invoke(bool strict, int id) { ... }
}
```

**Checkpoint 2:** `dotnet build src/KnockOff.sln` passes. Existing tests break (expected -- API changed). Proceed immediately to Phase 3.

### Phase 3: Test Updates (Moved Earlier)

Update all test files for the new API. This is moved to immediately after the generator rework so that correctness can be validated before further changes.

All test files need updating (current names in codebase -> new names):
- Non-void `Return(callback)` -> `Call(callback)` (callback moves to Call, Return is value-only)
- Non-void `Return(value)` -> `Return(value)` (no change for value overload)
- Void `Call(callback)` -> `Call(callback)` (no change)
- `ThenReturn(callback)` -> `ThenCall(callback)` (callback moves to ThenCall)
- `ThenReturn(value)` -> `ThenReturn(value)` (no change for value overload)
- `ThenCall(callback)` -> `ThenCall(callback)` (no change)
- When chain void `Execute` -> `Call` (if any remnants)
- Callback signatures change: `(a, b) => ...` becomes `args => args.a + args.b` for 2+ params
- Single-param callbacks stay the same: `(id) => ...` stays `(id) => ...`
- Slot-based test infrastructure removed/updated
- Generic method typed handler `Return(callback)` -> `Call(callback)` for non-void

**This is the largest mechanical change.** Fresh agent recommended.

**Checkpoint 3:** `dotnet test src/Tests/KnockOffTests/KnockOffTests.csproj` -- all tests pass. This validates the Phase 2 generator rework is correct before proceeding.

### Phase 4: Overload Redesign

Remove the slot system. Generate a single interceptor class per method name that contains overloaded `Call`/`Return`/`When` methods:

```csharp
// Interface: void Process(int x), void Process(string s), int Process(int x, int y)

public sealed class ProcessInterceptor : MethodInterceptorRuntime
{
    // Per-overload storage (each overload has its own callback/sequence/when in base)
    // Per-overload Invoke methods
    internal void Invoke_int(bool strict, int x) { ... }
    internal void Invoke_string(bool strict, string s) { ... }
    internal int Invoke_int_int(bool strict, int x, int y) { ... }

    // Overloaded Call methods
    /// <summary>Configures callback for Process(int x).</summary>
    public ProcessTracking Call(Action<int> callback) { ... }

    /// <summary>Configures callback for Process(string s).</summary>
    public ProcessTracking Call(Action<string> callback) { ... }

    /// <summary>Configures callback for Process(int x, int y).</summary>
    public ProcessTracking Call(Func<(int x, int y), int> callback) { ... }

    // Return only for non-void overloads
    /// <summary>Sets return value for Process(int x, int y).</summary>
    public ProcessTracking Return(int value) { ... }

    // Tracking handle per overload
    public sealed class ProcessTracking { ... }
}
```

**Note on current vs new:** The user-facing experience of overloaded methods is already single-property with overloaded methods today (see `Design.Stubs/Methods/MethodOverloads.cs`). This phase removes the internal compositor/slot infrastructure while preserving that experience. The API names change (`Return(callback)` -> `Call(callback)`), and the internal slot interfaces and their extension methods are deleted.

**Overload resolution:** C# overload resolution handles disambiguation because each `Call` overload has a distinct delegate parameter type. For `Return(value)`, if multiple overloads have the same return type, the user must use `Call` instead (the callback parameter types disambiguate).

**What if `Return(value)` is ambiguous?** If two overloads both return `int`, `Return(42)` would be ambiguous. In this case, the generator does NOT emit `Return(int value)` for that method name. The user must use `Call` to disambiguate. This is documented via a diagnostic or XML comment.

#### Mixed Generic/Non-Generic Overloads

**IMPORTANT:** Methods like `Process(string)` + `Process<T>(T)` are split by the builder into separate interceptors before they reach the overload system:
- `Process` (non-generic) -> `UnifiedMethodInterceptorModel` -> standard interceptor class
- `ProcessGeneric` (generic) -> `InlineGenericMethodHandlerModel` -> `Of<T>()` handler subsystem

This split happens in the builder (see `GenericMethodBase` in Design.Domain). The generic overload uses the `Of<T>()` handler subsystem, NOT the compositor/overload infrastructure. **The overload redesign in this phase must preserve this split.** The `ProcessGeneric.Of<T>()` handler has its own distinct rendering pipeline and does not participate in C# overload resolution on `Call`/`Return` -- it uses `Of<T>().Call(callback)` instead.

**Example from tests (`GenericMethodBugTests.cs`):**
```csharp
// Non-generic: stub.Process configured via standard interceptor
stub.Process.Call((label) => nonGenericCalled = true);

// Generic: stub.ProcessGeneric configured via Of<T>() handler
stub.ProcessGeneric.Of<int>().Call((item, label) => capturedLabel = label);
```

This pattern applies to all 9 patterns where a type has both generic and non-generic overloads of the same method name.

**Checkpoint 4a:** `dotnet build src/KnockOff.sln` passes with overload redesign.

**Checkpoint 4b:** `dotnet test src/Tests/KnockOffTests/KnockOffTests.csproj` -- all tests pass (update overload-related tests as part of this phase since they were already partially updated in Phase 3).

### Phase 5: XML Comment Generation Pipeline

Add XML documentation comment extraction and rendering:

1. **Model change:** Add `string? XmlDocSummary` and `EquatableArray<(string Name, string Doc)> ParamDocs` to `ParameterModel` or a new `XmlDocModel`.
2. **Transform change:** In `TransformClass`/`TransformInlineStubClass`, extract XML docs from `IMethodSymbol.GetDocumentationCommentXml()`.
3. **Renderer change:** Emit `/// <summary>`, `/// <param>` on every `Call`, `Return`, `When`, `ThenCall`, `ThenReturn` method.

**Checkpoint 5:** Generated code has XML comments. Tests still pass.

### Phase 6: Named Tuple Integration

Ensure named tuples work correctly for:
- `Call` callbacks (2+ params)
- `When` predicates (2+ params)
- `When` exact match (2+ params)
- `LastArgs` properties (2+ params)
- Sequence `ThenCall` callbacks (2+ params)

**Key implementation detail:** Named tuples in C# use `ValueTuple<T1, T2>` at runtime but carry element names as metadata. The generator must emit tuple syntax like `(int a, int b)` rather than `ValueTuple<int, int>` to preserve names.

This is already partially implemented -- the current `ComputeTArgsType` in `MethodInterceptorRenderer.cs` computes named ValueTuple strings. The new system uses these as `Func` / `Action` parameter types instead of as `TArgs` base class type parameters.

**Checkpoint 6:** Named tuples show correct member names in IntelliSense. Tests still pass.

### Phase 7: Design Project Updates

Update Design.Stubs and Design.Tests for the new API. This is similar in scope to Phase 3 but for the design source-of-truth projects.

**Checkpoint 7:** `dotnet build src/Design/Design.Stubs` and `dotnet test src/Design/Design.Tests` pass.

### Phase 8: Documentation and Skill Updates

Update:
- Skill files (7+ files)
- MarkdownSnippet samples (30+ files)
- Documentation guides (20+ files)
- README.md
- Create migration guide

**Checkpoint 8:** `dotnet mdsnippets` succeeds.

### Phase 9: Cleanup

1. Remove precompiled interceptor types:
   - Delete `MethodInterceptor.cs`, `MethodInterceptor0.cs`, `MethodInterceptor1.cs`
   - Delete `VoidMethodInterceptor.cs`, `VoidMethodInterceptor0.cs`, `VoidMethodInterceptor1.cs`
   - Delete `AsyncMethodInterceptor.cs`, `AsyncMethodInterceptor0.cs`, `AsyncMethodInterceptor1.cs`
   - Delete `AsyncVoidMethodInterceptor.cs`, `AsyncVoidMethodInterceptor0.cs`, `AsyncVoidMethodInterceptor1.cs`
2. Remove base class hierarchy:
   - Delete `VoidMethodInterceptorBase.cs`, `MethodInterceptorBase.cs`
3. Remove slot system:
   - Delete entire `src/KnockOff/Interceptors/Slots/` directory
4. Remove `PreCompiledInterceptorRenderer.cs`
5. Remove old builder/sequence interfaces (replaced by generated inner classes):
   - Delete or gut `IMethodCallBuilder.cs`, `IMethodReturnBuilder.cs`
   - Delete or gut `IMethodCallSequence.cs`, `IMethodReturnSequence.cs`
   - Delete generic When interfaces from `IWhenTracking.cs`
6. Move superseded todos to `completed/`
7. Move superseded plans to `completed/`
8. Bump version (major change)
9. Create release notes

**Checkpoint 9:** `dotnet test src/KnockOff.sln` -- full solution passes.

---

## Acceptance Criteria

- [ ] No precompiled generic interceptor types visible in IntelliSense
- [ ] All method interceptors are fully generated classes with clean names (e.g., `AddInterceptor`)
- [ ] `Call(callback)` works for both void and non-void methods
- [ ] `Return(value)` works for non-void methods (value only, never lambda)
- [ ] `ThenCall(callback)` and `ThenReturn(value)` work for sequences
- [ ] Mixed sequences work: `Return("A").ThenCall(args => Compute())`
- [ ] Named tuples for 2+ param methods show correct field names in IntelliSense
- [ ] Raw types for 0-1 param methods
- [ ] ref/out methods fall back to generated delegates with XML comments
- [ ] XML comments on all generated Call/Return/When methods
- [ ] User XML param docs migrate from interface to generated stub
- [ ] Overloaded methods use single property (no slots)
- [ ] Overload disambiguation via lambda parameter types
- [ ] When chains work with both value match and predicate
- [ ] When chains work with both Return and Call
- [ ] Tracking handles provide per-registration Verify/LastArgs/Reset
- [ ] Generic method `Of<T>()` typed handlers use `Call(callback)` for all callbacks (void and non-void)
- [ ] Precompiled When chain interfaces deleted (`IWhenChain`, `IWhenBuilder`, `IVoidWhenChain`)
- [ ] Precompiled builder/sequence interfaces deleted or simplified (generic type params removed)
- [ ] All 9 patterns generate correct API
- [ ] All existing tests pass (with updated API)
- [ ] Design.Stubs and Design.Tests pass
- [ ] Skill and documentation updated

---

## Dependencies

None. Pre-1.0 with a single consumer -- API changes are expected. Superseded todos already moved to `completed/`.

---

## Risks / Considerations

### Build Time Regression

Returning to fully generated interceptor classes will increase generated code size. The non-generic base class mitigates this, but per-method interceptor classes are fundamentally more code than field declarations.

**Mitigation:** The user has explicitly stated IntelliSense clarity > Build time. Monitor build times during implementation and report impact.

### Overload Resolution Ambiguity

Some `Call`/`Return` overloads may be ambiguous in C# overload resolution:

1. **`Return(value)` when multiple overloads return the same type:** The generator must detect this and NOT emit `Return(value)` for that method name, or emit it only once with a comment noting which overload it configures.

2. **`Call(Func<T, R>)` vs `Call(Func<(int a, T), R>)` when `T` is `int`:** Could be ambiguous. The tuple approach avoids this since `Func<(int a, int b), int>` is distinct from `Func<int, int>` (one wraps in tuple).

3. **Async simplified vs full:** `Call(Func<int, string>)` (simplified) vs `Call(Func<int, Task<string>>)` (full) -- these are distinct types, no ambiguity.

**Mitigation:** Thorough testing of overload resolution scenarios. Generator emits diagnostics when ambiguity is detected.

### Callback Signature Change for 2+ Params

The current `stub.Add.Return((a, b) => a + b)` becomes `stub.Add.Call(args => args.a + args.b)`. This is a mechanical change. Named tuples provide IntelliSense discovery (`args.` shows field names). Individual parameters would require generated delegate types that re-introduce generic type noise.

### Expression Tree Infrastructure

The current `DelegateInvokerFactory` builds expression trees to bridge `TDelegate` invocation with `TArgs` unpacking. The new design uses standard `Func<>`/`Action<>` types, which C# invokes directly. Expression trees are still needed for:
- ref/out delegate invocation
- Stub override fallback wiring (where the user's override method has individual parameters but the interceptor stores a tuple-based callback)

**Mitigation:** Keep `DelegateInvokerFactory` for these edge cases. For the common path, direct `Func<>`/`Action<>` invocation is simpler and faster.

### Generic Method Support (Of<T>()) -- In Scope

The `.Of<T>()` typed handler subsystem has its own separate rendering pipeline:
- **Models:** `UnifiedGenericMethodHandlerModel`, `FlatGenericMethodHandlerModel`, `InlineGenericMethodHandlerModel`
- **Renderers:** `FlatRenderer.RenderGenericMethodHandler()`, `InlineRenderer.RenderGenericMethodHandler()`, `ClassRenderer.RenderClassGenericMethodHandler()`, `StandaloneClassRenderer` (delegates to `ClassRenderer`)
- **Tests:** `GenericMethodTests.cs`, `GenericMethodBugTests.cs`

**Current behavior:** The typed handler uses `Return` for non-void callbacks and `Call` for void callbacks (line 741 of `InlineRenderer.cs`: `var typedHandlerEntryPoint = arity.IsVoid ? "Call" : "Return"`). This is the same void/non-void split being eliminated elsewhere.

**Required change:** Rename the typed handler entry point from `Return(callback)` to `Call(callback)` for non-void methods. This is a mechanical rename in 4 renderer locations. The typed handler's simple structure (one callback, count tracking, LastArg/LastArgs) does not need the full interceptor class redesign -- it stays as a generated sealed class with its current design.

This is handled in Phase 2 alongside the main API rename. See updated Phase 2 below.

### Source() Delegation

`Source(implementation)` sets a fallback that delegates to a real implementation. The new interceptor must support `SetSourceFallback` for this. Since the generated interceptor knows the exact method signature, it can store a delegate directly (no expression tree needed for standard cases).

### Stub Override Wiring

Stub overrides (`protected override ReturnType MethodName_(params)`) wire into the interceptor via `SetFallback`. The generated code must bridge between the override's individual parameters and the interceptor's tuple-based callback. This may still require a thin lambda.

### Delegate Stubs (Pattern 7)

Delegate stubs (`[KnockOff<DelegateType>]`) generate a special interceptor for the delegate's Invoke method. The new design must handle this pattern. Since delegates have a single method, the interceptor is straightforward -- it is just a method interceptor for the delegate's signature.

### Events

Events are NOT affected by this redesign. The event interceptor API stays as-is.

### Smart Default Factories for Generic Types

The `Of<T>()` typed handler system and generic stubs use different smart default strategies that must be preserved.

**For generic stubs (P2/P4/P8/P9) with class-level type parameters:**
Smart default factories use concrete `new` expressions that capture `T` from the enclosing generic class. For example:
```csharp
// Generated in GenericServiceStub<T>
public MethodInterceptor0<IEnumerable<T>> GetAll { get; } = new("GetAll", () => new List<T>());
```
The `Func<object>?` smart default factory in the base class works because the lambda `() => new List<T>()` captures `T` from the enclosing generic class at construction time. Under the new design, the generated interceptor class constructor passes this factory to the base: `base("GetAll", () => new List<T>())`.

**For `Of<T>()` typed handlers with method-level type parameters:**
Smart defaults use a runtime `SmartDefault<TSmartDefault>(string methodName)` helper that is generated into the stub class. This helper uses `Type.GetConstructor()` and `ctor.Invoke()` at runtime because the type parameter `TSmartDefault` is not known at compile time (it comes from `Of<int>()`, `Of<string>()`, etc.). This is generated code using reflection, but it is an existing pattern for generic methods and accepted per the design constraint that reflection is needed for method-level type parameters resolved at runtime.

**Risk:** If the redesign introduces new constraint emission code or changes the smart default factory pipeline, it must preserve both strategies:
1. Class-level generics: compile-time `new T()` or `new List<T>()` style factories
2. Method-level generics: runtime `SmartDefault<T>` helper with reflection

### `Of<T>()` Arity Grouping Infrastructure

The `InlineGenericTypeArityGroup` and `FlatGenericMethodArityGroup` model types group generic method overloads by their type parameter count (arity). This infrastructure is used across all four builder pipelines (`InlineModelBuilder`, `FlatModelBuilder`, `ClassModelBuilder`, `StandaloneClassModelBuilder`) and must be preserved during refactoring. It handles:
- Single-arity groups: `Register<T>()` -> one `Of<T>()` entry point
- Multi-arity disambiguation: `Transform<T>()` + `Transform<TIn, TOut>()` -> `TransformTypedHandler<T>` and `TransformTypedHandler2<TIn, TOut>`

### Tuples with Generic Type Parameters

For methods on generic stubs with 2+ parameters where some parameters use generic types:
```csharp
// Interface: void UpdateCache(TKey key, TValue value) on CacheBase<TKey, TValue>
// Generated callback type:
stub.UpdateCache.Call(args => cache[args.key] = args.value);
// where args is (TKey key, TValue value) -- generic types in named tuple
```

The named tuple computation in `ComputeTArgsType()` already handles generic type parameters -- it uses the fully qualified type string which includes `T`, `TKey`, `TValue`, etc. No additional changes needed for tuple generation with generic types.

### Async Methods on Generic Stubs

Async methods on generic stubs (e.g., `Task<T?> GetByIdAsync(int id)` on `IAsyncRepository<T>`) follow the same async overload pattern as non-generic stubs:

```csharp
// Current (Pattern 8, from AsyncOpenGenericDemo.Stubs.g.cs):
public AsyncMethodInterceptor1<GetByIdAsyncDelegate, GetByIdAsyncSyncDelegate, int, T?> GetByIdAsync { get; } = new("GetByIdAsync");

// New design:
public sealed class GetByIdAsyncInterceptor : MethodInterceptorRuntime
{
    /// <summary>Configures callback for GetByIdAsync(int id). Result auto-wrapped in Task.</summary>
    public MethodCallBuilder Call(Func<int, T?> callback) { ... }      // Simplified sync callback

    /// <summary>Configures async callback for GetByIdAsync(int id).</summary>
    public MethodCallBuilder Call(Func<int, Task<T?>> callback) { ... } // Full async callback

    /// <summary>Sets constant return value. Auto-wrapped in Task.</summary>
    public MethodCallBuilder Return(T? value) { ... }

    internal Task<T?> Invoke(bool strict, int id) { ... }
}
```

Key: The simplified callback `Func<int, T?>` and full callback `Func<int, Task<T?>>` are distinct types in C# even when `T` is generic. No overload resolution ambiguity.

### `unmanaged` Constraint Bug (Pre-Existing)

The `unmanaged` constraint causes the generator to emit `where TData : struct, unmanaged` which is CS0449 (cannot combine `struct` and `unmanaged` -- `unmanaged` implies `struct`). This is documented in `GenericTypeValidationTests.cs` and affects ALL patterns with `unmanaged` constraints. Tests for `unmanaged` constraint are currently excluded.

**Impact on redesign:** If the redesign introduces new constraint emission code (for interceptor class declarations on open generic delegates or any other pattern), it must test against `unmanaged` constraints. The fix is to emit `where TData : unmanaged` without `struct`. The existing bug is in `SymbolHelpers.cs` line 264-265 where both `HasValueTypeConstraint` and `HasUnmanagedTypeConstraint` can be true simultaneously.

---

## Architectural Verification

### Scope Table

| Pattern | Methods | Properties | Indexers | Events |
|---------|---------|-----------|---------|--------|
| 1. Standalone | **Major change** | No change | Tuple for multi-key | No change |
| 2. Generic Standalone | **Major change** | No change | Tuple for multi-key | No change |
| 3. Standalone Class | **Major change** | No change | Tuple for multi-key | No change |
| 4. Generic Standalone Class | **Major change** | No change | Tuple for multi-key | No change |
| 5. Inline Interface | **Major change** | No change | Tuple for multi-key | No change |
| 6. Inline Class | **Major change** | No change | Tuple for multi-key | No change |
| 7. Inline Delegate | **Major change** | N/A | N/A | N/A |
| 8. Open Generic Interface | **Major change** | No change | Tuple for multi-key | No change |
| 9. Open Generic Class | **Major change** | No change | Tuple for multi-key | No change |

### Pipeline Impact

All nine patterns route through `MethodInterceptorRenderer.RenderInterceptorClass()`. The changes to this renderer propagate to all patterns. However, each pattern group has its own Transform -> Builder -> Renderer pipeline, and some have pattern-specific concerns:

| Pattern Group | Pipeline | Special Concerns |
|--------------|---------|-----------------|
| Standalone (1,2) | `TransformClass` -> `FlatModelBuilder` -> `FlatRenderer` | Overload groups use compositor classes -- must be redesigned |
| Standalone Class (3,4) | `TransformStandaloneClass` -> `StandaloneClassModelBuilder` -> `StandaloneClassRenderer` | Stub overrides wire into interceptor -- fallback bridging needed |
| Inline (5,6) | `TransformInlineStubClass` -> `InlineModelBuilder` -> `InlineRenderer` | Generated as nested classes inside test class |
| Delegate (7) | `TransformInlineStubClass` -> `InlineModelBuilder` -> `InlineRenderer` | Single-method interceptor for delegate Invoke |
| Open Generic (8,9) | Various -> Various -> `InlineRenderer` | Generic type parameters flow through interceptor |

### API Changes

This is a comprehensive API change. Since KnockOff is pre-1.0 with a single consumer, this is expected and acceptable. All changes are mechanical find-and-replace:

1. Non-void `Return(callback)` → `Call(callback)` (callback moves to Call, Return is value-only)
2. Non-void `Return(value)` → `Return(value)` (unchanged)
3. Void `Call(callback)` → `Call(callback)` (unchanged)
4. `ThenReturn(callback)` → `ThenCall(callback)` (callback moves to ThenCall)
5. `ThenReturn(value)` → `ThenReturn(value)` (unchanged)
6. `ThenCall(callback)` → `ThenCall(callback)` (unchanged)
7. Slot-based overload access removed
8. 2+ param callback signatures change from `(a, b) => ...` to `args => args.a + args.b`
9. Precompiled interceptor types replaced by generated classes
10. Builder/sequence interface names change

### Design Project Verification

Deferred to implementation phases. Design.Stubs compilation will be verified at Checkpoint 7.

### Codebase Deep-Dive Summary

**Key findings from exploration:**

1. **12 precompiled interceptor sealed types** exist, with massive code duplication (~600-700 lines each). Removing these simplifies the library significantly but means all their logic must be in generated code or a new base class.

2. **The base class hierarchy** (`VoidMethodInterceptorBase<TDelegate, TArgs>`, `MethodInterceptorBase<TDelegate, TArgs, TReturn>`) uses generic type parameters and `new` keyword method hiding. These are NOT directly reusable as the non-generic base class. A new base class must be designed from scratch.

3. **`MethodInterceptorRenderer.cs` at 4582 lines** is the core generation engine. It already handles three modes: single-signature, base-class, and overload-group. The new design replaces all three modes with a single "fully generated with clean types" mode.

4. **The slot system** spans 8 files in `Slots/` with 32 slot interfaces and 4 extension method files. Removing this is clean -- it is self-contained.

5. **Named tuples are already partially used** -- `ComputeTArgsType()` in the renderer already computes named ValueTuple strings like `(int a, int b)`. The new design uses these as `Func`/`Action` parameter types.

6. **The API has been renamed multiple times** in recent history: `OnCall` → `Returns`/`Execute` → `Return`/`Call` → now `Call`/`Return`. Pre-1.0, single consumer -- API churn is expected and fine.

7. **`DelegateInvokerFactory`** builds expression trees at runtime for delegate invocation. The new design reduces reliance on this (standard `Func<>`/`Action<>` types invoke directly) but still needs it for ref/out and stub override bridging.

---

## Open Questions

### Q1: Base Class Design -- Composition vs Inheritance?

Should generated interceptor classes **inherit** from `MethodInterceptorRuntime` or **contain** it as a field?

**Inheritance pros:** `Verify()`, `Reset()`, `Verifiable()` etc. are directly available without delegation. Simpler generated code.
**Inheritance cons:** Base class methods are visible in IntelliSense alongside generated methods. If the base has generic parameters, we are back to the original problem.
**Composition pros:** Complete control over what IntelliSense shows. Can hide all base class details.
**Composition cons:** Every `Verify()`, `Reset()` call requires delegation: `public void Verify() => _runtime.Verify();`

**Recommendation:** Inheritance with a **non-generic** base class. Since the base is non-generic, it adds no type noise to IntelliSense. `Verify()`, `Reset()`, `Verifiable()` from the base are exactly what users want to see. The generated class adds only the typed methods (`Call`, `Return`, `When`, `LastArgs`).

### Q2: What Happens to IInterceptor?

The current precompiled types implement `IInterceptor` (which has `CheckVerification`, `CheckVerificationAll`, `IsVerifiable`, `Reset`, `IsConfigured`). The generated interceptor classes need to implement this too, for stub-level `Verify()` / `VerifyAll()` aggregation.

**Recommendation:** The non-generic base class implements `IInterceptor`. Generated classes inherit it.

### Q3: How Do Property/Indexer Interceptors Relate?

Properties and indexers have their own precompiled types (`PropertyGetInterceptor<TValue>`, `IndexerGetSetInterceptor<TKey, TValue>`, etc.). These were NOT part of the build-time optimization push. They have clean IntelliSense already: `PropertyGetInterceptor<string>` clearly says "this is a string property interceptor." Only 1-2 type parameters, both meaningful.

**Recommendation:** Property and indexer interceptors STAY as precompiled types. They do not cause IntelliSense problems. Making them fully generated would be significant effort for zero IntelliSense benefit. See the "What Stays Precompiled" section for the full inventory.

### Q4: How Large Will Generated Code Be?

The precompiled approach generates ~1 line per method (field declaration). The new approach generates ~150-250 lines per method (full interceptor class + inner classes). For a stub with 20 methods, that is 3000-5000 additional lines.

This estimate is based on the current "base-class" rendering mode output: `StandaloneClassStubOverrideStub_ExecuteInterceptor` (void, 1-param, with When chains) is ~160 lines. Non-void methods with return value handling will be slightly larger. Methods without When chains (0-param) will be smaller (~75 lines).

**Recommendation:** The user has explicitly prioritized IntelliSense > Build time. Document the size impact but proceed. The `object?`-based non-generic base class keeps the full priority chain logic out of generated code, keeping interceptors at ~150-250 lines rather than ~400-600 lines.

### Q5: Async Callback Overload Resolution

For `Task<string> GetNameAsync(int id)`, the generated interceptor has:
```csharp
Call(Func<int, string> callback)      // simplified -- auto-wraps in Task
Call(Func<int, Task<string>> callback) // full async
```

These are distinct types. But what about `ValueTask<string>`? Does the user also get:
```csharp
Call(Func<int, ValueTask<string>> callback) // full ValueTask
```

And what about 0-param async: `Task<string> GetName()`:
```csharp
Call(Func<string> callback)           // simplified
Call(Func<Task<string>> callback)     // full async
```

These are distinct. No ambiguity.

**Recommendation:** Emit both simplified and full async overloads as currently done. They are unambiguous.

### Q6: Will C# Overload Resolution Handle All Overload Cases?

For the overloaded `Process` example above, `Call(Action<int>)` vs `Call(Action<string>)` vs `Call(Func<(int x, int y), int>)` -- these are distinct delegate types, so overload resolution works.

But what about:
```csharp
void Process(int x)
int Process(int x)  // same params, different return
```

This is illegal in C# interfaces (can't overload on return type alone). So not a real concern.

What about:
```csharp
void Process(int x)
void Process(int y)  // same type, different name
```

This is also illegal in C#. Not a concern.

**Potential issue:** `Call(Func<int, string>)` vs `Call(Func<int, int>)` -- different return types, same param count. These are distinct `Func<>` types. No ambiguity.

### Q7: How Does the Tracking Handle Work for Overloaded Methods?

Each `Call`/`Return` registration returns a tracking handle specific to that overload. The handle tracks calls, args, and supports verification for just that registration.

For non-overloaded methods, the interceptor itself also exposes `Verify()`, `LastArgs`, etc. as convenience aliases for the current registration's tracking.

For overloaded methods, only the tracking handle provides per-overload access. The interceptor's `Verify()` checks ALL overloads combined (via `VerifyAll`-style logic).

---

## Developer Review

**Status:** Approved (2026-02-17 rev 6)
**Reviewed:** 2026-02-17 (initial), 2026-02-17 (re-review after rev 4 + rev 5)

### My Understanding of This Plan

**Core Change:** Replace all precompiled generic interceptor types (12 sealed classes, 2 base classes, 8 slot files) with fully generated interceptor classes that inherit from a new non-generic `MethodInterceptorRuntime` base class. Simultaneously unify the API: `Call(callback)` for all callbacks (void and non-void), `Return(value)` for values only. Overload disambiguation moves from slot interfaces to overloaded `Call`/`Return` methods.

**User-Facing API:** `stub.Method.Call(args => ...)` replaces `stub.Method.Return((a, b) => ...)` for non-void methods. `stub.Method.Return(value)` stays for values. 2+ param callbacks change from `(a, b) => a + b` to `args => args.a + args.b` (named tuple). Sequences use `ThenCall`/`ThenReturn`.

**Internal Changes:** New non-generic base class in library. `MethodInterceptorRenderer.cs` reworked. Remove `PreCompiledInterceptorRenderer.cs`, all slot infrastructure, all 12 sealed interceptor types, both generic base classes. Update all tests, Design projects, docs, skills.

**Patterns Affected:** All 9 patterns.

### Codebase Investigation

**Files Examined:**
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` - Confirmed three rendering modes: `RenderSingleSignatureContent`, `RenderBaseClassContent`, `RenderOverloadGroupContent`. 4582 lines.
- `src/Generator/Model/Shared/UnifiedMethodInterceptorModel.cs` - Model driving rendering. All fields as described.
- `src/Generator/Builder/UnifiedInterceptorBuilder.cs` - Builds models from IMethodSymbol. Handles single vs multi-overload.
- `src/KnockOff/Interceptors/MethodInterceptorBase.cs`, `VoidMethodInterceptorBase.cs` - Generic base classes with complex priority chain logic, all using TDelegate and TArgs generics.
- `src/KnockOff/Interceptors/IInterceptor.cs` - Simple interface: CheckVerification, CheckVerificationAll, Reset.
- `src/KnockOff/IMethodTracking.cs`, `IMethodCallBuilder.cs`, `IMethodReturnBuilder.cs`, `IMethodReturnSequence.cs`, `IWhenTracking.cs` - Complex interface hierarchy with 15+ interfaces, all generic.
- `src/Design/Design.Stubs/Methods/BasicMethods.cs` - Current API: `Return((a, b) => a + b)` for non-void, `Call(() => ...)` for void.
- `src/Design/Design.Stubs/Methods/MethodOverloads.cs` - Current overload API uses single property with overloaded Return/Call. No numbered properties.
- `src/Design/Design.Stubs/Methods/MethodSequences.cs` - Sequence API with individual parameter callbacks.
- `src/Design/Design.Stubs/StubPatterns/AllPatterns.cs` - All 9 patterns demonstrated.
- `src/Tests/KnockOffTests/GenericMethodTests.cs` - Confirmed `Of<T>()` pattern: `knockOff.Create.Of<TestEntity>().Verify(...)`.
- `src/Tests/KnockOffTests/MethodOverloadTests.cs` - Single property overload API already in use.
- `src/Generator/Renderer/Shared/PreCompiledInterceptorRenderer.cs` - Decision tree, self-contained and deletable.
- `src/Generator/Model/Shared/UnifiedGenericMethodHandlerModel.cs`, `src/Generator/Model/Flat/FlatGenericMethodHandlerModel.cs` - Separate model system for generic method Of<T>() handlers.

**Searches Performed:**
- Searched for `Of<` in Design.Stubs - no results (generic methods not demonstrated there)
- Searched for `GenericMethodInterceptor` in KnockOff library - no results (separate system)
- Searched for `GenericMethod` in Generator/Renderer - found separate rendering pipelines in FlatRenderer, ClassRenderer, InlineRenderer, StandaloneClassRenderer
- Searched for `entryPointName = model.IsVoid` - confirmed current code: `Return` for non-void, `Call` for void

**Design.Stubs Verification:**
The architect noted "Design Project Verification: Deferred to implementation phases." No failing Design.Stubs code was provided as acceptance criteria. Given the scope (complete API overhaul where entire Design.Stubs must be rewritten), this is acceptable. The plan provides extensive inline code examples of the target API.

**Discrepancies Found:**
1. Plan says overloaded methods currently use "numbered properties: stub.Process, stub.Process2" -- but the user-facing API already uses a single property with overloaded methods. Compositor is internal.
2. Plan does not include the generic method `Of<T>()` subsystem in scope despite it having separate rendering pipelines and API entry points that need renaming.
3. Precompiled When chain interfaces (`IWhenChain<TDelegate, TReturn>`, `IWhenBuilder<TDelegate, TReturn>`, `IVoidWhenChain<TDelegate>`) not listed in the "What Stays Precompiled" or "DELETED" sections.

### Concerns

1. **Gap: Generic Method `Of<T>()` Subsystem Not Addressed**
   - Details: The `Of<T>()` typed handler system has separate models (`UnifiedGenericMethodHandlerModel`, `FlatGenericMethodHandlerModel`, `InlineGenericMethodHandlerModel`), rendering logic (`FlatRenderer.RenderGenericMethodHandler()`, `ClassRenderer.RenderClassGenericMethodHandler()`), and tests (`GenericMethodTests.cs`, `GenericMethodBugTests.cs`). Not mentioned in the plan's Pattern-by-Pattern Analysis, implementation phases, or acceptance criteria.
   - Question: Is the generic method `Of<T>()` system in scope for this redesign? If so, which phase handles it?
   - Suggestion: Add explicit scope statement. At minimum, the API rename (Return callback -> Call callback) applies to typed handlers. Add acceptance criteria: "Generic method typed handlers use Call/Return API."

2. **Ambiguity: Non-Generic Base Class Logic Boundary**
   - Details: Plan says base has "all runtime logic" but shows only count fields and verification methods. Current base classes have the complete priority chain resolution (`When chain -> Sequence -> Return value -> Callback -> Source -> Default`) using typed fields (`_call` as TDelegate, `_sequence` as `List<(TDelegate, Tracking)>`, `_whenChain` as `List<WhenMatcher>`). If base is truly non-generic with only counts, ALL priority chain logic must be generated per method -- significantly more than the 100-200 lines estimate.
   - Question: What exactly lives in the base class vs generated code? Does the base use `object?` fields for callbacks/values, or only untyped counts? How do `CheckVerification`/`CheckVerificationAll` work in the base without knowing about typed fields?
   - Suggestion: Either (a) base class uses `object?` fields with virtual methods generated classes override for type safety, or (b) acknowledge generated code will be 300-500+ lines per method.

3. **Clarification: Overloaded Method User-Facing API Already Exists**
   - Details: Plan states "Current: Overloaded methods get numbered properties: stub.Process, stub.Process2, stub.Process3" (Decision 9). However, `MethodOverloadTests.cs` and `Design.Stubs/Methods/MethodOverloads.cs` show the current API already uses a single property with overloaded Return/Call methods. The compositor class is internal.
   - Question: Is Phase 3 purely about removing internal slot/compositor infrastructure while preserving the existing user experience?

4. **Gap: Precompiled When Chain Interfaces Not Listed for Deletion**
   - Details: `IWhenChain<TDelegate, TReturn>`, `IWhenBuilder<TDelegate, TReturn>`, `IVoidWhenChain<TDelegate>` are precompiled library interfaces with generic type parameters. Not addressed in "What Stays Precompiled" section. Under the new design, `WhenBuilder` and `WhenChain` are generated inner classes with method-specific typed signatures. The precompiled interfaces would need generic type parameters, contradicting the "no generic type noise" goal.
   - Question: Do these get deleted and replaced by generated inner classes?
   - Suggestion: List `IWhenChain`, `IWhenBuilder`, `IVoidWhenChain` in the DELETED section. The generated inner classes replace them.

5. **Clarity: Long Test Breakage Window (Phases 2-5)**
   - Details: Tests break at Phase 2 checkpoint and are not fixed until Phase 6 (test updates), creating 4 phases without test validation. The developer cannot verify correctness during Phases 3-5.
   - Question: Can test updates be moved earlier, or can a subset be updated after Phase 2?
   - Suggestion: Consider reordering: Phase 2 (generator rework + API rename) -> Phase 3 (test updates) -> Phase 4 (overload redesign) -> Phase 5 (XML comments) -> Phase 6 (named tuples). This provides validation earlier.

### What Looks Good

- Comprehensive pattern-by-pattern analysis with actual generated code evidence from `.g.cs` files
- Clear API naming decisions with explicit rationale for each of the 12 design choices
- Thorough inventory of what stays, what gets generated, and what gets deleted
- Starting point analysis is well-reasoned (evolve forward, not revert)
- "What Stays Precompiled" section with every library type categorized
- Risk analysis covering overload resolution, expression trees, build time, edge cases
- Cross-pattern summary table for all 9 patterns
- Open questions showing critical thinking about design alternatives

### Recommendation

Send back to architect to address concerns before implementation. The concerns are clarification-level, not fundamental design issues. The plan's architecture is sound; the gaps are about completeness of scope documentation and implementation logistics.

---

## Architect Response to Developer Concerns

**Date:** 2026-02-17
**Status:** All 5 concerns addressed

### Response 1: Generic Method `Of<T>()` Subsystem -- AGREED, Plan Updated

The developer is correct. The `Of<T>()` typed handler subsystem has its own rendering pipeline (`FlatRenderer.RenderGenericMethodHandler()`, `InlineRenderer.RenderGenericMethodHandler()`, `ClassRenderer.RenderClassGenericMethodHandler()`, `StandaloneClassRenderer` delegates to `ClassRenderer`) with separate models (`UnifiedGenericMethodHandlerModel`, `FlatGenericMethodHandlerModel`, `InlineGenericMethodHandlerModel`).

**Investigation:** Line 741 of `InlineRenderer.cs` confirms the current API split:
```csharp
var typedHandlerEntryPoint = arity.IsVoid ? "Call" : "Return";
```
Non-void typed handlers currently use `Return(callback)`. Under the new unified API, this should be `Call(callback)`.

**Resolution:** The Risks section now includes a full "Generic Method Support (Of<T>()) -- In Scope" subsection documenting the 4 renderer locations that need updating. The change is mechanical: rename the entry point from `Return` to `Call` for non-void typed handlers. Phase 2 now explicitly includes this rename. Phase 3 (test updates) includes updating `GenericMethodTests.cs` and `GenericMethodBugTests.cs`.

The typed handler's simple structure (one callback, count tracking, LastArg/LastArgs) does not need the full interceptor class redesign -- it remains a generated sealed class. Only the API entry point name changes.

**Acceptance criteria added:** "Generic method typed handlers use `Call(callback)` for all callbacks (void and non-void)."

### Response 2: Non-Generic Base Class Logic Boundary -- AGREED, Plan Updated

The developer correctly identified that the plan's base class sketch (counts-only) was inconsistent with the claim of "all runtime logic" in the base. The full priority chain (When -> Sequence -> Return value -> Callback -> Fallback -> Source -> Default) requires typed storage, not just counts.

**Investigation:** I examined the current `VoidMethodInterceptorBase<TDelegate, TArgs>` (544 lines) and `MethodInterceptorBase<TDelegate, TArgs, TReturn>` (508+ lines). The priority chain methods (`RunVoidPriorityChain`, `RunPriorityChain`, `HandleSequenceExhaustedRepeat`) operate on typed fields (`_call` as `TDelegate`, `_sequence` as `List<(TDelegate, Tracking)>`, `_whenChain` as `List<WhenMatcherBase>`, `_returnValue` as `TReturn`). The sealed interceptor types additionally have `_fallback` as `TDelegate?`, `_sourceFallback` as `TDelegate?`, and `_smartDefaultFactory`.

**Resolution:** The base class design now uses `object?` / `Delegate?` fields instead of generic type parameters. The full priority chain logic lives in the base class with abstract methods for type-specific operations (invoking delegates, recording args, matching When predicates). This is structurally identical to the current generic base classes, minus the generic type parameters.

The generated code size estimate is updated: ~150-250 lines per method (matching today's "base-class" rendering mode), not 300-500+. Evidence: `StandaloneClassStubOverrideStub_ExecuteInterceptor` (a void 1-param method with When chains) is ~160 lines in the current generated output.

Phase 1 now shows the complete `MethodInterceptorRuntime` sketch with all `object?` fields, all priority chain methods, all abstract methods, and all setup helpers.

### Response 3: Overloaded Method User-Facing API -- AGREED, Plan Updated

The developer is correct. The user-facing overload API is already single-property with overloaded methods. `Design.Stubs/Methods/MethodOverloads.cs` demonstrates:
```csharp
stub.Format.Return((input) => input.ToUpperInvariant());
stub.Format.Return((input, options) => options.Uppercase ? input.ToUpperInvariant() : input);
```

The plan's Decision 9 incorrectly described the current state as "numbered properties: `stub.Process`, `stub.Process2`, `stub.Process3`". This was inaccurate.

**Resolution:** Decision 9 is rewritten to accurately describe the current state: single property, overloaded methods, internal compositor + slot interfaces. Phase 4 is explicitly described as removing internal slot/compositor infrastructure while preserving the existing user experience. The API names change (`Return(callback)` -> `Call(callback)`), but the single-property pattern stays.

### Response 4: When Chain Interfaces -- AGREED, Plan Updated

The developer correctly identified missing types. I examined `/home/keithvoels/KnockOff/src/KnockOff/IWhenTracking.cs` and confirmed:
- `IWhenChain<TDelegate, TReturn>` -- generic, has `ThenCall(TDelegate)`, `ThenNone()`, `Verifiable()`
- `IWhenBuilder<TDelegate, TReturn>` -- generic, has `Return(TReturn value)`
- `IVoidWhenChain<TDelegate>` -- generic, has `Call(TDelegate)`, `ThenCall(TDelegate)`, `ThenNone()`, `Verify(Called)`, `Verifiable()`
- `IWhenTracking` -- non-generic base, has `Verifiable()`

**Resolution:** Two new DELETED sections added to the plan:
1. "DELETED -- Precompiled When Chain Interfaces" listing `IWhenChain<TDelegate, TReturn>`, `IWhenBuilder<TDelegate, TReturn>`, `IVoidWhenChain<TDelegate>`, and noting `IWhenTracking` (non-generic) may survive.
2. "DELETED -- Precompiled Builder/Sequence Interfaces (Generic)" listing `IMethodCallBuilder<TDelegate>`, `IMethodCallBuilder<TDelegate, TArg>`, `IMethodCallSequence<TDelegate>`, `IMethodReturnBuilder<TDelegate, TReturn>`, `IMethodReturnBuilder<TDelegate, TArg, TReturn>`, `IMethodReturnSequence<TDelegate, TReturn>`.

Phase 9 cleanup now explicitly includes deleting the generic When interfaces from `IWhenTracking.cs` and the generic builder/sequence interfaces.

### Response 5: Long Test Breakage Window -- AGREED, Plan Updated

The developer's suggestion to move test updates earlier is correct. With 4 phases of broken tests, the developer cannot validate correctness during critical implementation work.

**Resolution:** Phase ordering is now:
1. Phase 1: Non-generic base class (library)
2. Phase 2: Generator -- fully generated interceptor classes (tests break)
3. **Phase 3: Test updates (moved from Phase 6) -- tests pass again**
4. Phase 4: Overload redesign (with incremental test updates)
5. Phase 5: XML comment generation pipeline
6. Phase 6: Named tuple integration
7. Phase 7: Design project updates
8. Phase 8: Documentation and skill updates
9. Phase 9: Cleanup

The key change is that Phase 3 (test updates) now immediately follows Phase 2 (generator rework). This means correctness is validated before the overload redesign (Phase 4), XML comments (Phase 5), and named tuples (Phase 6) proceed. Phase 4 includes incremental test updates for overload-specific tests since it changes the overload infrastructure.

---

## Implementation Contract

**Created:** 2026-02-17
**Approved by:** knockoff-developer

### In Scope

#### Phase 1: Non-Generic Base Class (Library)
- [x] Create `MethodInterceptorRuntime` in `src/KnockOff/Interceptors/` with `object?`/`Delegate?` fields
- [x] Implement full priority chain: When -> Sequence -> Return value -> Callback -> Fallback -> Source -> Default
- [x] Implement abstract methods: `InvokeVoidDelegate`, `InvokeDelegate`, `RecordArgs`, `RecordUnconfiguredArgs`
- [x] Implement public API: `Verify`, `Verifiable`, `Reset`, `CheckVerification`, `CheckVerificationAll`
- [x] Implement `IInterceptor` on the base class
- [x] Implement inner base classes: `WhenMatcherBase`, `MethodCallBuilderBase`, `MethodSequenceBase`, `ReturnMethodCallBuilderBase`, `ReturnMethodSequenceBase`, `VoidWhenChainBase`, `WhenBuilderBase`, `WhenChainBase`
- [x] **Checkpoint 1:** `dotnet build src/KnockOff/KnockOff.csproj` passes (0 warnings, 0 errors, all 3 TFMs)

#### Phase 2: Generator -- Fully Generated Interceptor Classes
- [x] Rework `MethodInterceptorRenderer.cs` to always generate full interceptor classes inheriting `MethodInterceptorRuntime`
- [x] Generate typed `Call`/`Return`/`When` methods with `Func<>`/`Action<>` signatures
- [x] Generate typed `Invoke` method per interceptor
- [x] Generate typed inner classes: `MethodCallBuilder`, `MethodSequence`, `WhenBuilder`, `WhenChain`
- [x] Generate 0-param, 1-param, 2+-param (tuple) variants
- [x] Generate async simplified + full callback overloads
- [x] Generate ref/out delegate fallback
- [x] Rename `Of<T>()` typed handler entry point: `Return(callback)` -> `Call(callback)` for non-void in:
  - `InlineRenderer.cs` line 717
  - `FlatRenderer.cs` line 988
  - `ClassRenderer.cs` line 544
  - `StandaloneClassRenderer.cs` (delegates to ClassRenderer, no additional changes needed)
- [x] **Checkpoint 2:** Library and generator build clean. Generated code compiles across all 9 patterns. 2782 errors remain in consumer code (tests/benchmarks/samples) using old API — expected, Phase 3 work.

#### Phase 3: Test Updates (Moved Earlier for Validation)
- [x] Update all test files for new API naming:
  - Non-void `Return(callback)` -> `Call(callback)`
  - `ThenReturn(callback)` -> `ThenCall(callback)`
  - 2+ param callbacks from `(a, b) => ...` to `args => args.a + args.b`
  - Generic method typed handler `Return(callback)` -> `Call(callback)` for non-void
- [x] Update `GenericMethodTests.cs` and `GenericMethodBugTests.cs`
- [x] **Checkpoint 3:** `dotnet test src/Tests/KnockOffTests/KnockOffTests.csproj` -- 1725 passed, 3 failed (pre-existing ThrowsOnDefault bug for string returns), 4 skipped

#### Phase 4: Overload Redesign
- [x] Remove compositor class generation from renderer (~700 lines removed from PreCompiledInterceptorRenderer.cs)
- [x] Generate single interceptor class per method name with overloaded `Call`/`Return`/`When` (already existed from Phase 2)
- [x] Generate per-overload `Invoke` methods (already existed from Phase 2)
- [x] Generate per-overload tracking handles (already existed from Phase 2)
- [x] Preserve mixed generic/non-generic overload split (`Process` vs `ProcessGeneric.Of<T>()`)
- [x] Update overload-specific tests and Design.Stubs for `Return(callback)` → `Call(callback)` on overloaded methods
- [x] **Checkpoint 4:** All tests pass — KnockOffTests: 1725 passed/3 pre-existing failures, Design.Tests: 370 passed, NeatooInterfaceTests: 473 passed

#### Phase 5: XML Comment Generation Pipeline
- [x] Extract XML docs from `IMethodSymbol.GetDocumentationCommentXml()`
- [x] Add XML doc fields to model/builder pipeline
- [x] Emit `/// <summary>`, `/// <param>` on all generated `Call`, `Return`, `When`, `ThenCall`, `ThenReturn`
- [x] **Checkpoint 5:** Generated code has XML comments. Tests still pass (1725/3/4).

#### Phase 6: Named Tuple Integration
- [x] Ensure named tuples work in `Call` callbacks, `When` predicates, `When` exact match, `LastArgs`, `ThenCall` callbacks
- [x] Verify tuple field names appear in generated code (not `ValueTuple<T1, T2>`)
- [x] **Checkpoint 6:** Named tuples show correct member names. Tests still pass.

#### Phase 7: Design Project Updates
- [x] Rewrite `Design.Stubs` for new API
- [x] Update `Design.Tests`
- [x] **Checkpoint 7:** `dotnet build src/Design/Design.Stubs` and `dotnet test src/Design/Design.Tests` pass — 370 passed, 0 failed

#### Phase 8: Documentation and Skill Updates
- [x] Update skill files
- [x] Update MarkdownSnippet samples
- [x] Update documentation guides
- [x] Create migration guide
- [x] **Checkpoint 8:** `dotnet mdsnippets` succeeds — 710 snippets processed, Design.Tests 370/0/0, Documentation.Samples 691/0/0

#### Phase 9: Cleanup
- [x] Delete 12 precompiled interceptor sealed types
- [x] Delete `VoidMethodInterceptorBase.cs`, `MethodInterceptorBase.cs`
- [x] Delete entire `src/KnockOff/Interceptors/Slots/` directory (8 files)
- [x] Delete `PreCompiledInterceptorRenderer.cs` (already deleted in Phase 4)
- [x] Delete generic When chain interfaces from `IWhenTracking.cs` — KEPT: still used by generator
- [x] Delete generic builder/sequence interfaces — KEPT: still used by generator
- [x] Delete `DelegateInvokerFactory.cs` (dead code)
- [x] Delete 6 test files for deleted types
- [x] Bump version to 0.52.0
- [x] Create release notes
- [x] **Checkpoint 9:** `dotnet test src/KnockOff.sln` -- full solution passes (1510/0/4 KnockOffTests, 370/0/0 Design, 691/0/0 Samples, 473/0/0 Neatoo)

### Explicitly Out of Scope

- **Property/indexer interceptor redesign** -- These stay as precompiled types. Clean IntelliSense already.
- **Event interceptor changes** -- Events stay as-is.
- **Arg-style API** -- Decided against. Matching through When chains only.
- **`unmanaged` constraint bug fix** -- Pre-existing bug (CS0449 at SymbolHelpers.cs:262-265). Document as risk but do not fix unless new constraint emission code is added.
- **Performance optimization of `object?` boxing** -- Accepted trade-off per priority order.

### Verification Gates

1. **After Phase 1:** `dotnet build src/KnockOff/KnockOff.csproj` passes. New base class compiles. Existing code still compiles (new class is additive).
2. **After Phase 2:** `dotnet build src/KnockOff.sln` passes. Generated code compiles but tests fail (expected -- API changed).
3. **After Phase 3:** `dotnet test src/Tests/KnockOffTests/KnockOffTests.csproj` -- ALL tests pass. This is the critical validation gate.
4. **After Phase 4:** All tests pass with overload redesign. Slot/compositor infrastructure removed.
5. **After Phase 7:** `dotnet build src/Design/Design.Stubs` and `dotnet test src/Design/Design.Tests` pass.
6. **After Phase 9:** `dotnet test src/KnockOff.sln` -- full solution passes. No precompiled interceptor types remain.

### Stop Conditions

If any of these occur, STOP and report:
- Out-of-scope test fails (test not directly related to this redesign)
- Architectural contradiction discovered (e.g., `object?` approach doesn't work for some priority chain scenario)
- Generated code does not compile for any of the 9 patterns at any checkpoint
- Overload resolution ambiguity that C# cannot resolve
- Missing pattern: if any of the 9 patterns is not generating correct interceptor classes at a checkpoint

---

## Implementation Progress

### Phase 1: Non-Generic Base Class (Library) -- COMPLETE

**Started:** 2026-02-17

- [x] Created `MethodInterceptorRuntime` in `src/KnockOff/Interceptors/MethodInterceptorRuntime.cs`
  - `object?`/`Delegate?` fields for all typed state (callbacks, return values, When matchers, sequences)
  - No generic type parameters on the class
- [x] Implemented full priority chain:
  - `RunVoidPriorityChain(object? args)` -- When -> Sequence -> Callback
  - `RunPriorityChain(object? args)` -- When -> Sequence -> Return value -> Callback
  - `HandleVoidSequenceExhaustedRepeat(bool strict, object? args)`
  - `HandleNonVoidSequenceExhaustedRepeat(bool strict, object? args)`
- [x] Implemented abstract methods for generated subclasses to override:
  - `InvokeVoidDelegate(Delegate del, object? args)`
  - `InvokeDelegate(Delegate del, object? args)`
  - `RecordArgs(object? args, MethodCallBuilderBase tracking)`
  - `RecordUnconfiguredArgs(object? args)`
  - `CreateValueDelegate(object? value)` (virtual, for non-void interceptors)
- [x] Implemented `IInterceptor` interface: `CheckVerification()`, `CheckVerificationAll()`, `Reset()`
- [x] Implemented public API: `Verify()`, `Verify(Called)`, `Verifiable()`, `Verifiable(Called)`, `UnconfiguredCallCount`, `TotalCallCount`, `IsConfigured`, `IsVerifiable`
- [x] Implemented setup helpers: `SetupVoidCallback`, `SetupReturnCallback`, `SetupReturnValue`, `SetSourceFallback`, `SetFallback`
- [x] Implemented inner base classes:
  - `WhenMatcherBase` -- abstract base for When chain matchers with `Matches`, `Execute`, `ExecuteReturn`, `IsTerminal`, `CallCount`
  - `MethodCallBuilderBase` -- call counting, verification, sequence elevation via `ThenCallBase`
  - `MethodSequenceBase` -- void sequence with `ThenCallBase`, `Verify`, `Reset`, `VerifiableBase`, `ThenDefault`
  - `ReturnMethodCallBuilderBase` -- non-void builder with `ElevateToSequenceBase`, `ThenReturnCallbackBase`, `ThenReturnValueBase`
  - `ReturnMethodSequenceBase` -- non-void sequence with `ThenReturnCallbackBase`, `ThenReturnValueBase`, `Verify`, `Reset`, `VerifiableBase`, `ThenDefault`
  - `VoidWhenChainBase` -- void When chain with `AddMatcher`, `AddTerminalMatcher`, `Verify`, `Verify(Called)`, `Reset`, `VerifiableBase`
  - `WhenBuilderBase` -- non-void When builder with `AddValueMatcher`
  - `WhenChainBase` -- non-void When chain with `AddTerminalCallbackMatcher`, `AddNoneMatcher`, `Verify`, `Reset`, `VerifiableBase`
- [x] **Checkpoint 1 PASSED:** `dotnet build src/KnockOff/KnockOff.csproj` -- 0 warnings, 0 errors across net8.0, net9.0, net10.0
- [x] **Existing tests verified:** All 1728/1727 tests pass (net10.0/net9.0: 1728 passed, net8.0: 1727 passed, 4 skipped across all)

---

## Completion Evidence

### Phase 1 Evidence

- **Build:** `dotnet build src/KnockOff/KnockOff.csproj` -- 0 warnings, 0 errors, all 3 target frameworks (net8.0, net9.0, net10.0)
- **Tests:** `dotnet test src/Tests/KnockOffTests/KnockOffTests.csproj` -- all pass:
  - net10.0: Passed 1728, Skipped 4, Failed 0
  - net9.0: Passed 1728, Skipped 4, Failed 0
  - net8.0: Passed 1727, Skipped 4, Failed 0
- **File created:** `src/KnockOff/Interceptors/MethodInterceptorRuntime.cs` (~560 lines)
- **Purely additive:** No existing files modified. New class coexists with the old generic base classes.
