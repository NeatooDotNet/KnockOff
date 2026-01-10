# KnockOff Generic Methods Design

## Overview

This document provides a comprehensive design for supporting generic methods in KnockOff. Generic methods present a unique challenge: type parameters are only known at call sites, not at generation time. The solution uses a `.Of<T>()` pattern to provide type-keyed handlers.

**Status:** Complete
**Priority:** High - Required for full Moq feature parity
**Completed:** 2026-01-09

---

## The Challenge

For a non-generic method like `User GetUser(int id)`, the generator knows all types at compile time and can generate strongly-typed handlers:

```csharp
public sealed class GetUserHandler
{
    private readonly List<int> _calls = new();
    public int? LastCallArg => _calls.Count > 0 ? _calls[^1] : null;
    public Func<MyKnockOff, int, User>? OnCall { get; set; }
    // ...
}
```

For a generic method like `T Deserialize<T>(string json)`, the generator cannot know `T` at compile time. We need a mechanism to:
1. Track calls with their actual type arguments
2. Allow configuring behavior per type argument
3. Maintain type safety where possible

---

## The Solution: `.Of<T>()` Pattern

The core API uses type-keyed handlers accessed via `.Of<T>()`:

```csharp
// Interface
public interface ISerializer
{
    T Deserialize<T>(string json);
}

// Usage
var knockOff = new SerializerKnockOff();
ISerializer serializer = knockOff;

// Configure behavior per type
knockOff.Spy.Deserialize.Of<Customer>().OnCall = (ko, json) => new Customer { Name = "Test" };
knockOff.Spy.Deserialize.Of<Order>().OnCall = (ko, json) => new Order { Id = 1 };

// Use the stub
var customer = serializer.Deserialize<Customer>("{}");  // Returns configured Customer
var order = serializer.Deserialize<Order>("{}");         // Returns configured Order
var product = serializer.Deserialize<Product>("{}");     // Smart default: new Product() if has ctor, else throws

// Verify calls
Assert.True(knockOff.Spy.Deserialize.Of<Customer>().WasCalled);
Assert.Equal("{}", knockOff.Spy.Deserialize.Of<Customer>().LastCallArg);
```

---

## Generic Method Scenarios

### Scenario 1: Generic Return Type Only

```csharp
public interface IFactory
{
    T Create<T>() where T : new();
}
```

**Handler Design:**
```csharp
public sealed class CreateHandler
{
    private readonly Dictionary<Type, object> _typedHandlers = new();

    /// <summary>Gets the typed handler for type argument T.</summary>
    public CreateTypedHandler<T> Of<T>() where T : new()
    {
        if (!_typedHandlers.TryGetValue(typeof(T), out var handler))
        {
            handler = new CreateTypedHandler<T>();
            _typedHandlers[typeof(T)] = handler;
        }
        return (CreateTypedHandler<T>)handler;
    }

    // Aggregate tracking across all type arguments
    public int TotalCallCount => _typedHandlers.Values.Sum(h => ((ICallTracker)h).CallCount);
    public IReadOnlyList<Type> CalledTypeArguments => _typedHandlers.Keys.ToList();

    public void Reset()
    {
        foreach (var handler in _typedHandlers.Values)
            ((IResettable)handler).Reset();
        _typedHandlers.Clear();
    }
}

public sealed class CreateTypedHandler<T> : ICallTracker, IResettable where T : new()
{
    public delegate T CreateDelegate(MyKnockOff ko);

    public int CallCount { get; private set; }
    public bool WasCalled => CallCount > 0;
    public CreateDelegate? OnCall { get; set; }

    public void RecordCall() => CallCount++;
    public void Reset() { CallCount = 0; OnCall = null; }
}
```

**Test Usage:**
```csharp
[Fact]
public void GenericReturnOnly_ConfigurePerType()
{
    var knockOff = new FactoryKnockOff();
    IFactory factory = knockOff;

    knockOff.Spy.Create.Of<Customer>().OnCall = ko => new Customer { Name = "Test" };
    knockOff.Spy.Create.Of<Order>().OnCall = ko => new Order { Id = 42 };

    var customer = factory.Create<Customer>();
    var order = factory.Create<Order>();

    Assert.Equal("Test", customer.Name);
    Assert.Equal(42, order.Id);

    Assert.Equal(1, knockOff.Spy.Create.Of<Customer>().CallCount);
    Assert.Equal(1, knockOff.Spy.Create.Of<Order>().CallCount);
    Assert.Equal(2, knockOff.Spy.Create.TotalCallCount);
}
```

---

### Scenario 2: Generic Parameter Only

```csharp
public interface IProcessor
{
    void Process<T>(T value);
}
```

**Handler Design:**
```csharp
public sealed class ProcessHandler
{
    private readonly Dictionary<Type, object> _typedHandlers = new();

    public ProcessTypedHandler<T> Of<T>()
    {
        if (!_typedHandlers.TryGetValue(typeof(T), out var handler))
        {
            handler = new ProcessTypedHandler<T>();
            _typedHandlers[typeof(T)] = handler;
        }
        return (ProcessTypedHandler<T>)handler;
    }

    public int TotalCallCount => _typedHandlers.Values.Sum(h => ((ICallTracker)h).CallCount);
    public void Reset() { /* ... */ }
}

public sealed class ProcessTypedHandler<T> : ICallTracker, IResettable
{
    public delegate void ProcessDelegate(MyKnockOff ko, T value);

    private readonly List<T> _calls = new();

    public int CallCount => _calls.Count;
    public bool WasCalled => _calls.Count > 0;
    public T? LastCallArg => _calls.Count > 0 ? _calls[^1] : default;
    public IReadOnlyList<T> AllCalls => _calls;
    public ProcessDelegate? OnCall { get; set; }

    public void RecordCall(T value) => _calls.Add(value);
    public void Reset() { _calls.Clear(); OnCall = null; }
}
```

**Test Usage:**
```csharp
[Fact]
public void GenericParameterOnly_TracksArguments()
{
    var knockOff = new ProcessorKnockOff();
    IProcessor processor = knockOff;

    var capturedStrings = new List<string>();
    knockOff.Spy.Process.Of<string>().OnCall = (ko, value) => capturedStrings.Add(value);

    processor.Process("hello");
    processor.Process("world");
    processor.Process(42);  // Different type - no callback

    Assert.Equal(2, knockOff.Spy.Process.Of<string>().CallCount);
    Assert.Equal(["hello", "world"], knockOff.Spy.Process.Of<string>().AllCalls);
    Assert.Equal(1, knockOff.Spy.Process.Of<int>().CallCount);
    Assert.Equal(42, knockOff.Spy.Process.Of<int>().LastCallArg);
}
```

---

### Scenario 3: Generic Return and Parameter (Same Type)

```csharp
public interface ICloner
{
    T Clone<T>(T value);
}
```

**Handler Design:**
```csharp
public sealed class CloneTypedHandler<T> : ICallTracker, IResettable
{
    public delegate T CloneDelegate(MyKnockOff ko, T value);

    private readonly List<T> _calls = new();

    public int CallCount => _calls.Count;
    public bool WasCalled => _calls.Count > 0;
    public T? LastCallArg => _calls.Count > 0 ? _calls[^1] : default;
    public IReadOnlyList<T> AllCalls => _calls;
    public CloneDelegate? OnCall { get; set; }

    public void RecordCall(T value) => _calls.Add(value);
    public void Reset() { _calls.Clear(); OnCall = null; }
}
```

**Test Usage:**
```csharp
[Fact]
public void GenericReturnAndParameter_SameType()
{
    var knockOff = new ClonerKnockOff();
    ICloner cloner = knockOff;

    knockOff.Spy.Clone.Of<Customer>().OnCall = (ko, c) => new Customer { Name = c.Name + " (Clone)" };

    var original = new Customer { Name = "Alice" };
    var clone = cloner.Clone(original);

    Assert.Equal("Alice (Clone)", clone.Name);
    Assert.Same(original, knockOff.Spy.Clone.Of<Customer>().LastCallArg);
}
```

---

### Scenario 4: Multiple Type Parameters

```csharp
public interface IConverter
{
    TOut Convert<TIn, TOut>(TIn value);
}
```

**Handler Design - `.Of<TIn, TOut>()` Pattern:**
```csharp
public sealed class ConvertHandler
{
    // Key by tuple of types
    private readonly Dictionary<(Type, Type), object> _typedHandlers = new();

    public ConvertTypedHandler<TIn, TOut> Of<TIn, TOut>()
    {
        var key = (typeof(TIn), typeof(TOut));
        if (!_typedHandlers.TryGetValue(key, out var handler))
        {
            handler = new ConvertTypedHandler<TIn, TOut>();
            _typedHandlers[key] = handler;
        }
        return (ConvertTypedHandler<TIn, TOut>)handler;
    }

    public int TotalCallCount => _typedHandlers.Values.Sum(h => ((ICallTracker)h).CallCount);

    /// <summary>Gets all (TIn, TOut) type pairs that were called.</summary>
    public IReadOnlyList<(Type TIn, Type TOut)> CalledTypePairs =>
        _typedHandlers.Keys.ToList();

    public void Reset() { /* ... */ }
}

public sealed class ConvertTypedHandler<TIn, TOut> : ICallTracker, IResettable
{
    public delegate TOut ConvertDelegate(MyKnockOff ko, TIn value);

    private readonly List<TIn> _calls = new();

    public int CallCount => _calls.Count;
    public bool WasCalled => _calls.Count > 0;
    public TIn? LastCallArg => _calls.Count > 0 ? _calls[^1] : default;
    public IReadOnlyList<TIn> AllCalls => _calls;
    public ConvertDelegate? OnCall { get; set; }

    public void RecordCall(TIn value) => _calls.Add(value);
    public void Reset() { _calls.Clear(); OnCall = null; }
}
```

**Test Usage:**
```csharp
[Fact]
public void MultipleTypeParameters_KeyByBoth()
{
    var knockOff = new ConverterKnockOff();
    IConverter converter = knockOff;

    knockOff.Spy.Convert.Of<string, int>().OnCall = (ko, s) => int.Parse(s);
    knockOff.Spy.Convert.Of<int, string>().OnCall = (ko, i) => i.ToString();

    var intResult = converter.Convert<string, int>("42");
    var stringResult = converter.Convert<int, string>(100);

    Assert.Equal(42, intResult);
    Assert.Equal("100", stringResult);

    Assert.Equal("42", knockOff.Spy.Convert.Of<string, int>().LastCallArg);
    Assert.Equal(100, knockOff.Spy.Convert.Of<int, string>().LastCallArg);
}
```

---

### Scenario 5: Mixed Generic and Non-Generic Parameters

```csharp
public interface IRepository
{
    T FindByKey<T>(string key, int maxResults);
}
```

**Handler Design:**
```csharp
public sealed class FindByKeyTypedHandler<T> : ICallTracker, IResettable
{
    public delegate T FindByKeyDelegate(MyKnockOff ko, string key, int maxResults);

    // Track the non-generic arguments in a tuple
    private readonly List<(string key, int maxResults)> _calls = new();

    public int CallCount => _calls.Count;
    public bool WasCalled => _calls.Count > 0;
    public (string key, int maxResults)? LastCallArgs => _calls.Count > 0 ? _calls[^1] : null;
    public IReadOnlyList<(string key, int maxResults)> AllCalls => _calls;
    public FindByKeyDelegate? OnCall { get; set; }

    public void RecordCall(string key, int maxResults) => _calls.Add((key, maxResults));
    public void Reset() { _calls.Clear(); OnCall = null; }
}
```

**Test Usage:**
```csharp
[Fact]
public void MixedGenericAndNonGeneric_TracksNonGenericArgs()
{
    var knockOff = new RepositoryKnockOff();
    IRepository repo = knockOff;

    knockOff.Spy.FindByKey.Of<Customer>().OnCall = (ko, key, max) =>
        new Customer { Name = $"Found: {key}" };

    var result = repo.FindByKey<Customer>("alice", 10);

    Assert.Equal("Found: alice", result.Name);
    Assert.Equal(("alice", 10), knockOff.Spy.FindByKey.Of<Customer>().LastCallArgs);
}
```

---

### Scenario 6: Constrained Generics

```csharp
public interface IEntityFactory
{
    T Create<T>() where T : class, IEntity, new();
}
```

**Handler Design:**

Constraints are propagated to the typed handler:

```csharp
public sealed class CreateHandler
{
    private readonly Dictionary<Type, object> _typedHandlers = new();

    /// <summary>Gets the typed handler. T must satisfy: class, IEntity, new().</summary>
    public CreateTypedHandler<T> Of<T>() where T : class, IEntity, new()
    {
        if (!_typedHandlers.TryGetValue(typeof(T), out var handler))
        {
            handler = new CreateTypedHandler<T>();
            _typedHandlers[typeof(T)] = handler;
        }
        return (CreateTypedHandler<T>)handler;
    }

    // ...
}

public sealed class CreateTypedHandler<T> : ICallTracker, IResettable
    where T : class, IEntity, new()
{
    // Same structure as before
}
```

**Key Point:** The constraints on `.Of<T>()` match the interface method constraints. This provides compile-time safety:

```csharp
// Compile error: 'int' does not satisfy constraint 'class'
knockOff.Spy.Create.Of<int>();

// Compile error: 'string' does not satisfy constraint 'IEntity'
knockOff.Spy.Create.Of<string>();

// OK: Customer is class, implements IEntity, has parameterless constructor
knockOff.Spy.Create.Of<Customer>();
```

---

## Handler Class Architecture

### Base Handler (Aggregate Tracking)

Each generic method gets a base handler that provides aggregate tracking and type-keyed access:

```csharp
/// <summary>Base handler for generic method '{MethodName}'.</summary>
public sealed class {MethodName}Handler
{
    private readonly Dictionary<{TypeKey}, object> _typedHandlers = new();

    /// <summary>Gets the typed handler for the specified type argument(s).</summary>
    public {MethodName}TypedHandler<{TypeParams}> Of<{TypeParams}>()
        {Constraints}
    {
        var key = {KeyExpression};
        if (!_typedHandlers.TryGetValue(key, out var handler))
        {
            handler = new {MethodName}TypedHandler<{TypeParams}>();
            _typedHandlers[key] = handler;
        }
        return ({MethodName}TypedHandler<{TypeParams}>)handler;
    }

    /// <summary>Total number of calls across all type arguments.</summary>
    public int TotalCallCount =>
        _typedHandlers.Values.Sum(h => ((IGenericMethodCallTracker)h).CallCount);

    /// <summary>True if this method was called with any type argument.</summary>
    public bool WasCalled => _typedHandlers.Values.Any(h => ((IGenericMethodCallTracker)h).WasCalled);

    /// <summary>All type argument(s) that were used in calls.</summary>
    public IReadOnlyList<{TypeKey}> CalledTypeArguments => _typedHandlers.Keys.ToList();

    /// <summary>Resets all typed handlers.</summary>
    public void Reset()
    {
        foreach (var handler in _typedHandlers.Values)
            ((IResettable)handler).Reset();
        _typedHandlers.Clear();
    }
}
```

Where:
- `{TypeKey}` is `Type` for single type parameter, `(Type, Type)` for two, etc.
- `{KeyExpression}` is `typeof(T)` for single, `(typeof(TIn), typeof(TOut))` for two, etc.

### Typed Handler (Per-Type Tracking)

```csharp
/// <summary>Handler for generic method '{MethodName}' with type argument(s) {TypeParams}.</summary>
public sealed class {MethodName}TypedHandler<{TypeParams}> : IGenericMethodCallTracker, IResettable
    {Constraints}
{
    /// <summary>Callback delegate for this method.</summary>
    public delegate {ReturnType} {MethodName}Delegate({KnockOffType} ko{, Parameters});

    // Tracking storage (varies by parameter count)
    {TrackingStorage}

    /// <summary>Number of times this method was called with these type arguments.</summary>
    public int CallCount => {CountExpression};

    /// <summary>True if called at least once with these type arguments.</summary>
    public bool WasCalled => CallCount > 0;

    /// <summary>Arguments from the most recent call.</summary>
    public {ArgType}? LastCallArg{s} => {LastArgExpression};

    /// <summary>All recorded calls.</summary>
    public IReadOnlyList<{ArgType}> AllCalls => _calls;

    /// <summary>Callback invoked when called. If set, its return value is used.</summary>
    public {MethodName}Delegate? OnCall { get; set; }

    /// <summary>Records a call.</summary>
    public void RecordCall({Parameters}) => {RecordExpression};

    /// <summary>Resets tracking state.</summary>
    public void Reset() { _calls.Clear(); OnCall = null; }
}
```

### Support Interfaces

```csharp
/// <summary>Marker interface for generic method call tracking.</summary>
internal interface IGenericMethodCallTracker
{
    int CallCount { get; }
    bool WasCalled { get; }
}

/// <summary>Marker interface for resettable handlers.</summary>
internal interface IResettable
{
    void Reset();
}
```

---

## Explicit Interface Implementation

For each generic method, generate an implementation that:
1. Records the call with the actual type arguments
2. Invokes the type-specific callback if set
3. Falls back to user method if defined
4. Returns smart default (matching non-generic behavior)

```csharp
T ISerializer.Deserialize<T>(string json)
{
    // Get or create the typed handler
    var typedHandler = Spy.Deserialize.Of<T>();
    typedHandler.RecordCall(json);

    // Priority 1: OnCall callback (type-specific)
    if (typedHandler.OnCall is { } callback)
        return callback(this, json);

    // Priority 2: User-defined method (applies to ALL type arguments)
    // Generator detects: protected T Deserialize<T>(string json) in partial class
    return Deserialize<T>(json);

    // Priority 3 (if no user method): Smart default
    // return SmartDefault<T>("Deserialize");
}
```

**With user method defined:**
```csharp
[KnockOff]
public partial class SerializerKnockOff : ISerializer
{
    // User implementation - called for any T without OnCall configured
    protected T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json)!;
}
```

**Without user method:** Generator emits `SmartDefault<T>()` call instead.

### Nullable Return Types: `T?`

When the return type is nullable (`T?`), no smart default logic is needed - just return `default`:

```csharp
// Interface with nullable return
public interface IRepository
{
    T? Find<T>(int id);  // Nullable - null is always valid
}

// Generated implementation
T? IRepository.Find<T>(int id)
{
    var typedHandler = Spy.Find.Of<T>();
    typedHandler.RecordCall(id);

    if (typedHandler.OnCall is { } callback)
        return callback(this, id);

    // Nullable return → default is always valid (no SmartDefault needed)
    return default;
}
```

| Return Type | Unconfigured Behavior |
|-------------|----------------------|
| `T?` | `return default;` (null) |
| `T` | `return SmartDefault<T>(...);` (may throw) |

---

## Unconfigured Generic Method Behavior: Smart Defaults

**Design Decision:** Generic methods follow the **same smart default behavior** as non-generic methods, evaluated at runtime based on the actual type argument.

### Non-Generic Smart Default Behavior (Reference)

From the existing KnockOff implementation (`SmartDefaultsTests.cs`):

| Return Type | Behavior |
|-------------|----------|
| Value type (`int`, `bool`, `DateTime`) | `default(T)` (0, false, etc.) |
| Nullable reference (`string?`, `Entity?`) | `null` |
| Non-nullable with parameterless ctor (`List<T>`, custom class) | `new T()` |
| Non-nullable WITHOUT parameterless ctor (`string`, interfaces) | **Throws** |

### Generic Methods: Same Logic at Runtime

For generic methods, we apply the same logic using runtime type inspection:

```csharp
T ISerializer.Deserialize<T>(string json)
{
    var typedHandler = Spy.Deserialize.Of<T>();
    typedHandler.RecordCall(json);

    // Priority 1: OnCall callback
    if (typedHandler.OnCall is { } callback)
        return callback(this, json);

    // Priority 2: User-defined method (if exists)
    // ...

    // Priority 3: Smart default based on runtime type
    return SmartDefault<T>("Deserialize");
}
```

### SmartDefault Implementation

A shared helper method applies the same logic as non-generic methods:

```csharp
/// <summary>
/// Returns a smart default value for type T, matching non-generic method behavior.
/// </summary>
private static T SmartDefault<T>(string methodName)
{
    var type = typeof(T);

    // Value types → default(T)
    if (type.IsValueType)
        return default!;

    // Nullable reference types → null
    // Note: At runtime, T and T? are the same type for reference types,
    // but if the method signature allows null, this is safe.
    // The NRT annotation is compile-time only.

    // Check for public parameterless constructor
    var ctor = type.GetConstructor(
        BindingFlags.Public | BindingFlags.Instance,
        binder: null,
        types: Type.EmptyTypes,
        modifiers: null);

    if (ctor != null)
        return (T)ctor.Invoke(null);

    // No parameterless constructor → throw (matches non-generic behavior)
    throw new InvalidOperationException(
        $"No implementation provided for {methodName}<{type.Name}>. " +
        $"The type '{type.Name}' has no public parameterless constructor. " +
        $"Configure via: Spy.{methodName}.Of<{type.Name}>().OnCall = ...");
}
```

### Compile-Time Optimization with `new()` Constraint

When the generic method has a `where T : new()` constraint, we can skip reflection:

```csharp
// With where T : new() constraint, use compile-time new T()
T IFactory.Create<T>() where T : new()
{
    var typedHandler = Spy.Create.Of<T>();
    typedHandler.RecordCall();

    if (typedHandler.OnCall is { } callback)
        return callback(this);

    // T has new() constraint - compile-time safe, no reflection needed
    return new T();
}
```

### Examples by Type

```csharp
var knockOff = new SerializerKnockOff();
ISerializer serializer = knockOff;

// Value type → default(int) = 0
var count = serializer.Deserialize<int>("{}");  // Returns 0

// Nullable reference → null
var maybeUser = serializer.Deserialize<User?>("{}");  // Returns null

// Has parameterless constructor → new List<string>()
var list = serializer.Deserialize<List<string>>("{}");  // Returns empty list

// No parameterless constructor → THROWS
var str = serializer.Deserialize<string>("{}");  // Throws InvalidOperationException
var disposable = serializer.Deserialize<IDisposable>("{}");  // Throws
```

### Rationale

1. **Consistency** - Generic and non-generic methods behave identically
2. **Predictable** - Users learn one set of rules for all methods
3. **Useful defaults** - `List<T>`, `Dictionary<K,V>`, custom DTOs work out of the box
4. **Fail-fast for impossible types** - `string`, interfaces, abstract classes throw immediately

---

## User Method Detection for Generic Methods

Detecting user-defined methods for generic methods is more complex than non-generic methods.

### Option A: Exact Signature Match (Recommended)

The user defines a protected generic method with the same signature:

```csharp
[KnockOff]
public partial class SerializerKnockOff : ISerializer
{
    // User-defined implementation for all T
    protected T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json)!;
}

// Generated code checks for and calls user method
T ISerializer.Deserialize<T>(string json)
{
    var typedHandler = Spy.Deserialize.Of<T>();
    typedHandler.RecordCall(json);

    if (typedHandler.OnCall is { } callback)
        return callback(this, json);

    // Call user method
    return Deserialize<T>(json);
}
```

### Option B: Type-Specific Overloads (Not Recommended)

User could define specific type overloads:

```csharp
protected Customer Deserialize(string json) => new Customer { ... };
protected Order Deserialize(string json) => new Order { ... };
```

**Problem:** The generator would need to find these via reflection at runtime, losing compile-time safety. Not recommended.

### Detection Rules

1. Look for protected method with same name
2. Check if it has matching type parameters with same constraints
3. Check if non-generic parameters match
4. If found, call it; otherwise use default

---

## Handling Overloads with Generic Methods

When a method has both generic and non-generic overloads:

```csharp
public interface IMixedService
{
    void Process(string value);           // Non-generic
    void Process<T>(T value);             // Generic
    T Process<T>(string key, T default);  // Generic with non-generic param
}
```

### Generated Structure

```csharp
public sealed class MixedServiceKnockOffSpy
{
    // Non-generic overload gets regular handler
    public Process_String_Handler Process_String { get; } = new();

    // Generic overloads get generic handlers
    public Process_T_Handler Process_T { get; } = new();
    public Process_String_T_Handler Process_String_T { get; } = new();

    // Convenience property for the most common case (if unambiguous)
    // If only one, can use short name
    public Process_T_Handler Process => Process_T;
}
```

### Naming Convention for Overloads

| Signature | Handler Name |
|-----------|--------------|
| `Process(string)` | `Process` or `Process_String` |
| `Process<T>(T)` | `Process_T` |
| `Process<T>(string, T)` | `Process_String_T` |
| `Process<TIn, TOut>(TIn)` | `Process_TIn_TOut` |

---

## Edge Cases

### 1. Unregistered Type Arguments

When `.Of<T>()` is called with a type that has no `OnCall` configured, smart defaults apply:

```csharp
var knockOff = new SerializerKnockOff();
ISerializer serializer = knockOff;

// No configuration - smart defaults apply based on type

// Value type → default(int) = 0
var count = serializer.Deserialize<int>("{}");
Assert.Equal(0, count);

// Has parameterless constructor → new Product()
var product = serializer.Deserialize<Product>("{}");
Assert.NotNull(product);  // New instance created

// No parameterless constructor → throws
Assert.Throws<InvalidOperationException>(() =>
    serializer.Deserialize<string>("{}"));

// All calls are still tracked regardless of return behavior
Assert.True(knockOff.Spy.Deserialize.Of<int>().WasCalled);
Assert.True(knockOff.Spy.Deserialize.Of<Product>().WasCalled);
```

### 2. Nullable Type Arguments

```csharp
// Are these the same handler?
knockOff.Spy.Deserialize.Of<string>();
knockOff.Spy.Deserialize.Of<string?>();
```

**Answer:** Yes, at runtime `typeof(string)` == `typeof(string?)`. Nullability is compile-time only. The handler dictionary uses `Type` as key, so they map to the same handler.

**Documentation Note:** Mention that nullable and non-nullable reference types share the same handler because nullability is erased at runtime.

### 3. Value Types vs Reference Types

```csharp
knockOff.Spy.Process.Of<int>().OnCall = (ko, v) => Console.WriteLine(v);
knockOff.Spy.Process.Of<Customer>().OnCall = (ko, v) => Console.WriteLine(v.Name);
```

Both work correctly. Value types are boxed when stored in the `Dictionary<Type, object>` handler lookup, but the typed handler itself uses the correct generic type.

### 4. Nested Generic Types

```csharp
knockOff.Spy.Deserialize.Of<List<Customer>>().OnCall = (ko, json) =>
    new List<Customer> { new Customer() };

knockOff.Spy.Deserialize.Of<Dictionary<string, List<int>>>().OnCall = (ko, json) =>
    new Dictionary<string, List<int>>();
```

Works correctly. `typeof(List<Customer>)` and `typeof(Dictionary<string, List<int>>)` are distinct types.

### 5. Generic Constraints Preventing Some Types

```csharp
public interface IEntityRepo
{
    T Get<T>(int id) where T : class, IEntity;
}

// Compile error: int doesn't satisfy 'class' constraint
knockOff.Spy.Get.Of<int>();

// Compile error: string doesn't satisfy 'IEntity' constraint
knockOff.Spy.Get.Of<string>();
```

The constraints on `.Of<T>()` prevent invalid usage at compile time.

### 6. Open Generic Type Arguments

```csharp
// This is NOT supported - T must be a closed type
knockOff.Spy.Deserialize.Of<T>();  // Error: T is not defined
```

The `.Of<T>()` method requires concrete type arguments known at the call site.

---

## Thread Safety Considerations

Generic method handlers use `Dictionary<Type, object>` for handler storage. This is **not thread-safe** by default.

### Options

**Option A: No Thread Safety (Recommended for v1)**

Document that KnockOff stubs are not thread-safe. This matches existing behavior for non-generic handlers and is sufficient for typical unit test usage.

**Option B: ConcurrentDictionary**

Use `ConcurrentDictionary<Type, object>` for handler storage. Adds slight overhead but enables multi-threaded test scenarios.

```csharp
private readonly ConcurrentDictionary<Type, object> _typedHandlers = new();

public TypedHandler<T> Of<T>()
{
    return (TypedHandler<T>)_typedHandlers.GetOrAdd(
        typeof(T),
        _ => new TypedHandler<T>());
}
```

**Recommendation:** Start with non-thread-safe. Add thread safety as opt-in via attribute parameter if needed:

```csharp
[KnockOff(ThreadSafe = true)]
```

---

## Reset Semantics

### Per-Type Reset

```csharp
knockOff.Spy.Deserialize.Of<Customer>().Reset();  // Clears Customer handler only
```

### Full Handler Reset

```csharp
knockOff.Spy.Deserialize.Reset();  // Clears ALL typed handlers
```

The base handler's `Reset()` clears the dictionary, removing all typed handlers. This is different from iterating and calling `Reset()` on each - it fully removes them.

### Tracking vs Callback Reset

Following the pattern proposed in `future-improvements-plan.md`:

```csharp
// Future: separate reset methods
knockOff.Spy.Deserialize.Of<Customer>().ResetTracking();  // Clear call history
knockOff.Spy.Deserialize.Of<Customer>().ResetCallback();  // Clear OnCall
knockOff.Spy.Deserialize.Of<Customer>().Reset();          // Clear both
```

---

## Integration with Existing KnockOff

### Backward Compatibility

Non-generic methods are completely unchanged. The generic method support is purely additive.

### Handler Placement in Spy Class

```csharp
public sealed class SerializerKnockOffSpy
{
    // Non-generic methods (unchanged)
    public ParseHandler Parse { get; } = new();
    public ValidateHandler Validate { get; } = new();

    // Generic methods (new)
    public DeserializeHandler Deserialize { get; } = new();
    public SerializeHandler Serialize { get; } = new();
}
```

### Interface-Scoped Handlers

For generic interfaces with generic methods:

```csharp
public interface IRepository<TEntity>
{
    T Transform<T>(TEntity entity);
}

// Multiple implementations
[KnockOff]
public partial class UserRepoKnockOff : IRepository<User> { }

[KnockOff]
public partial class OrderRepoKnockOff : IRepository<Order> { }
```

Each gets its own handler:
```csharp
knockOff.IRepository_User.Transform.Of<Dto>().OnCall = ...
knockOff.IRepository_Order.Transform.Of<Summary>().OnCall = ...
```

---

## Generator Implementation

### Model Changes

```csharp
internal sealed record InterfaceMemberInfo(
    string Name,
    string ReturnType,
    bool IsProperty,
    bool IsIndexer,
    bool HasGetter,
    bool HasSetter,
    bool IsNullable,
    DefaultValueStrategy DefaultStrategy,
    string? ConcreteTypeForNew,
    EquatableArray<ParameterInfo> Parameters,
    EquatableArray<ParameterInfo> IndexerParameters,
    // NEW: Generic method support
    bool IsGenericMethod,
    EquatableArray<TypeParameterInfo> TypeParameters
) : IEquatable<InterfaceMemberInfo>;

internal sealed record TypeParameterInfo(
    string Name,
    EquatableArray<string> Constraints  // "class", "new()", "IEntity", etc.
) : IEquatable<TypeParameterInfo>;
```

### Detection Logic

```csharp
private static InterfaceMemberInfo CreateMethodInfo(IMethodSymbol method)
{
    // Existing logic...

    // NEW: Check for type parameters
    var isGenericMethod = method.IsGenericMethod;
    var typeParameters = EquatableArray<TypeParameterInfo>.Empty;

    if (isGenericMethod)
    {
        typeParameters = new EquatableArray<TypeParameterInfo>(
            method.TypeParameters
                .Select(tp => new TypeParameterInfo(
                    tp.Name,
                    new EquatableArray<string>(GetConstraints(tp).ToArray())))
                .ToArray());
    }

    return new InterfaceMemberInfo(
        // ... existing properties ...
        IsGenericMethod: isGenericMethod,
        TypeParameters: typeParameters);
}

private static IEnumerable<string> GetConstraints(ITypeParameterSymbol tp)
{
    if (tp.HasReferenceTypeConstraint)
        yield return "class";
    if (tp.HasValueTypeConstraint)
        yield return "struct";
    if (tp.HasUnmanagedTypeConstraint)
        yield return "unmanaged";
    if (tp.HasConstructorConstraint)
        yield return "new()";
    if (tp.HasNotNullConstraint)
        yield return "notnull";
    foreach (var type in tp.ConstraintTypes)
        yield return type.ToDisplayString(FullyQualifiedWithNullability);
}
```

### Code Generation Templates

The generator needs new templates for:

1. **Base Handler Class** - with `Of<T>()` method and aggregate tracking
2. **Typed Handler Class** - per-type tracking and callbacks
3. **Explicit Interface Implementation** - calls through to typed handler
4. **Support Interfaces** - `IGenericMethodCallTracker`, `IResettable`

### Incremental Generation

The `TypeParameterInfo` record must be equatable for incremental generation:

```csharp
internal sealed record TypeParameterInfo(
    string Name,
    EquatableArray<string> Constraints
) : IEquatable<TypeParameterInfo>;
```

`EquatableArray<string>` is already defined in the project.

---

## Implementation Phases

### Phase 1: Basic Generic Methods
- [x] Add `TypeParameterInfo` record
- [x] Update `InterfaceMemberInfo` with generic method fields
- [x] Detect generic methods in `CreateMethodInfo`
- [x] Generate base handler with `Of<T>()` for single type parameter
- [x] Generate typed handler for single type parameter
- [x] Generate explicit interface implementation
- [x] Add basic tests for `T Get<T>()` pattern

### Phase 2: Multiple Type Parameters
- [x] Generate `Of<T1, T2>()` for two type parameters
- [x] Generate `Of<T1, T2, T3>()` for three type parameters (not tested, but pattern is established)
- [x] Handle type tuple keys in dictionary
- [x] Add tests for `TOut Convert<TIn, TOut>(TIn)` pattern

### Phase 3: Constraints
- [x] Extract and format constraints from `ITypeParameterSymbol`
- [x] Apply constraints to `Of<T>()` method
- [x] Apply constraints to typed handler class
- [x] Test compile-time constraint enforcement

### Phase 4: Mixed Parameters
- [x] Handle generic methods with non-generic parameters
- [x] Track non-generic parameters in typed handler
- [x] Test `T Find<T>(string key, int limit)` pattern

### Phase 5: User Method Detection
- [x] Detect user-defined generic methods by including type parameters in signature matching
- [x] Call user method with correct type arguments (e.g., `Create<T>()` instead of `Create()`)
- [x] Test user method priority over default

### Phase 6: Overload Handling
- [x] Handle mixed generic/non-generic overloads (e.g., `Process(string)` and `Process<T>(T)`)
- [x] Generate separate interceptors for generic vs non-generic overloads
- [x] Test mixed overload scenarios with independent tracking

### Phase 7: Documentation and Polish
- [x] Update README.md feature table
- [x] Add guides/generics.md (covers both generic interfaces and generic methods)
- [x] Update knockoff-vs-moq.md
- [x] Add samples to Documentation.Samples project

---

## Test Cases Summary

```csharp
// Scenario 1: Generic return only
public interface IFactory { T Create<T>() where T : new(); }

// Scenario 2: Generic parameter only
public interface IProcessor { void Process<T>(T value); }

// Scenario 3: Generic return and parameter
public interface ICloner { T Clone<T>(T value); }

// Scenario 4: Multiple type parameters
public interface IConverter { TOut Convert<TIn, TOut>(TIn value); }

// Scenario 5: Mixed generic and non-generic
public interface IRepository { T FindByKey<T>(string key, int max); }

// Scenario 6: Constrained generics
public interface IEntityFactory { T Create<T>() where T : class, IEntity, new(); }

// Edge cases
public interface ISerializer
{
    T Deserialize<T>(string json);  // Test with nested types
    string Serialize<T>(T value);   // Test with value types
}
```

---

## Open Questions

### 1. Handler Property Naming

For generic methods, should we use:
- `Spy.Deserialize` (same as method name)
- `Spy.Deserialize_T` (explicit generic marker)
- Both (alias)?

**Recommendation:** Use method name directly. The `.Of<T>()` call makes it clear it's a generic method.

### 2. Async Generic Methods

```csharp
Task<T> GetAsync<T>(int id);
```

Should the typed handler use the same pattern as async non-generic methods? Yes - the return type handling is orthogonal to the generic parameter handling.

### 3. Void Generic Methods

```csharp
void Process<T>(T value);
```

Typed handler has no return value in callback:
```csharp
public delegate void ProcessDelegate(MyKnockOff ko, T value);
public ProcessDelegate? OnCall { get; set; }
```

Works the same as non-generic void methods.

### 4. Default Value for Unconstrained T

```csharp
T Get<T>(int id);  // No constraints
```

**Answer:** Use `SmartDefault<T>()` - same logic as non-generic methods:
- Value types → `default(T)`
- Reference types with parameterless ctor → `new T()` via reflection
- Reference types without ctor → throw `InvalidOperationException`

### 5. Performance of Dictionary<Type, object>

For high-frequency test scenarios, is dictionary lookup acceptable?

Yes - unit tests rarely call methods thousands of times. If performance becomes an issue, could use `ConcurrentDictionary` with faster `GetOrAdd` or a lock-free pattern.

---

## Summary

The `.Of<T>()` pattern provides a clean, type-safe API for configuring generic method behavior:

```csharp
// Configure
knockOff.Spy.Deserialize.Of<Customer>().OnCall = (ko, json) => new Customer();

// Use
var customer = serializer.Deserialize<Customer>("{}");

// Verify
Assert.True(knockOff.Spy.Deserialize.Of<Customer>().WasCalled);
Assert.Equal("{}", knockOff.Spy.Deserialize.Of<Customer>().LastCallArg);

// Aggregate tracking
Assert.Equal(1, knockOff.Spy.Deserialize.TotalCallCount);
Assert.Contains(typeof(Customer), knockOff.Spy.Deserialize.CalledTypeArguments);
```

This design:
- Maintains KnockOff's philosophy of compile-time setup with readable verification
- Provides type safety through generic constraints on `.Of<T>()`
- Integrates seamlessly with existing handler patterns
- Supports all generic method scenarios encountered in real codebases
