---
skill: knockoff
topic: advanced
audience: advanced
---

# Advanced Topics

Generics, overloads, out/ref parameters, delegates, and events.

## Generic Methods

Methods with type parameters require `.Of<T>()` for type-specific configuration.

<!-- snippet: skill-SKILL-pattern-generics -->
```cs
[KnockOff]
public partial class SkGenericSerializerKnockOff : ISkGenericSerializer { }
```
<!-- endSnippet -->

### Single Type Parameter

```csharp
public interface ISerializer
{
    T Deserialize<T>(string json);
}

// Configure per type
stub.Deserialize.Of<User>().OnCall((ko, json) => new User { Name = "Test" });
stub.Deserialize.Of<Order>().OnCall((ko, json) => new Order { Id = 1 });

// Per-type tracking
Assert.Equal(2, stub.Deserialize.Of<User>().CallCount);
Assert.Equal("{}", stub.Deserialize.Of<User>().LastArg);

// Aggregate tracking
Assert.Equal(5, stub.Deserialize.TotalCallCount);
Assert.Contains(typeof(User), stub.Deserialize.CalledTypeArguments);
```

### Multiple Type Parameters

```csharp
public interface IConverter
{
    TOut Convert<TIn, TOut>(TIn input);
}

stub.Convert.Of<string, int>().OnCall((ko, s) => s.Length);
stub.Convert.Of<int, string>().OnCall((ko, i) => i.ToString());
```

### Generic Interceptor API

| Member | Type | Description |
|--------|------|-------------|
| `.Of<T>()` | Method | Returns typed interceptor |
| `.TotalCallCount` | `int` | Calls across all types |
| `.WasCalled` | `bool` | Called with any type |
| `.CalledTypeArguments` | `IReadOnlyList<Type>` | All types used |
| `.Reset()` | Method | Clears all typed intercepts |

**Per-type interceptor:**
- `CallCount`, `WasCalled`, `LastArg`, `OnCall`, `Reset()`

### Reset Behavior

```csharp
// Reset single type
stub.Deserialize.Of<User>().Reset();
Assert.Equal(0, stub.Deserialize.Of<User>().CallCount);
Assert.True(stub.Deserialize.Of<Order>().WasCalled);  // Other types unaffected

// Reset all types
stub.Deserialize.Reset();
Assert.Equal(0, stub.Deserialize.TotalCallCount);
```

## Method Overloads

Overloaded methods get numeric suffixes based on parameter count/types.

<!-- snippet: skill-SKILL-pattern-overloads -->
```cs
[KnockOff]
public partial class SkOverloadedServiceKnockOff : ISkOverloadedService { }
```
<!-- endSnippet -->

### Suffix Rules

```csharp
public interface IProcessor
{
    void Process(string data);                   // Process1
    void Process(string data, int priority);     // Process2
    void Process(string data, int p, bool async); // Process3
}

stub.Process1.OnCall((ko, data) => { });
stub.Process2.OnCall((ko, data, priority) => { });
stub.Process3.OnCall((ko, data, priority, async) => { });
```

**Single method = no suffix:**
```csharp
public interface ISingle
{
    void Method();  // knockOff.Method (no suffix)
}
```

### User Method Collision

User methods add suffix:

```csharp
public interface IService
{
    int GetValue(int id);
}

[KnockOff]
public partial class ServiceStub : IService
{
    protected int GetValue(int id) => id * 2;  // User method exists
}

// Interceptor is GetValue2 (not GetValue)
stub.GetValue2.OnCall((ko, id) => id * 100);
```

**Why suffix?** Prevents naming conflict between user method and interceptor.

### Naming Edge Cases

```csharp
public interface IEdgeCases
{
    void Method();           // Method (no overloads)
    void Method(int x);      // Method1 (first overload)
    void Method(string s);   // Method2 (second overload)
}

// All three get suffixes due to overloading
stub.Method.OnCall((ko) => { });     // ❌ Doesn't exist
stub.Method1.OnCall((ko, x) => { }); // ✅ Correct
```

## Out Parameters

Out parameters require explicit delegate type.

<!-- snippet: skill-SKILL-pattern-out-params -->
```cs
[KnockOff]
public partial class SkOutParamParserKnockOff : ISkOutParamParser { }
```
<!-- endSnippet -->

### Single Out Parameter

```csharp
public interface IParser
{
    bool TryParse(string input, out int result);
}

// Use explicit delegate type for out params
stub.TryParse.OnCall = (TryParseInterceptor.Delegate)((ko, input, out int result) =>
{
    result = 42;
    return true;
});
```

### Multiple Out Parameters

```csharp
public interface IParser
{
    void GetData(out string name, out int count);
}

stub.GetData.OnCall = (GetDataInterceptor.Delegate)((ko, out string name, out int count) =>
{
    name = "Test";
    count = 10;
});
```

### Tracking

Out parameters track the **input value** (before callback modifies it):

```csharp
int result;
bool success = parser.TryParse("123", out result);

// Tracks input argument (before out assignment)
Assert.Equal("123", stub.TryParse.LastCallArgs?.input);
```

## Ref Parameters

Ref parameters work similarly to out parameters.

<!-- snippet: skill-SKILL-pattern-ref-params -->
```cs
[KnockOff]
public partial class SkRefProcessorKnockOff : ISkRefProcessor { }
```
<!-- endSnippet -->

### Basic Usage

```csharp
public interface IProcessor
{
    void Increment(ref int value);
}

stub.Increment.OnCall = (IncrementInterceptor.Delegate)((ko, ref int value) =>
{
    value++;
});

int x = 5;
processor.Increment(ref x);
Assert.Equal(6, x);
```

### Tracking

Ref parameters track the **input value** (before callback modifies it):

```csharp
Assert.Equal(5, stub.Increment.LastArg);  // Input value, not output
```

## Generic Interfaces

Generic interfaces can be stubbed like regular interfaces.

### Standalone Stubs

```csharp
public interface IRepository<T>
{
    T? GetById(int id);
    void Save(T entity);
}

[KnockOff]
public partial class UserRepositoryStub : IRepository<User> { }

[KnockOff]
public partial class GenericRepositoryStub<T> : IRepository<T> where T : class { }
```

**Usage:**
```csharp
// Closed generic
var userRepo = new UserRepositoryStub();

// Open generic
var userRepo = new GenericRepositoryStub<User>();
var orderRepo = new GenericRepositoryStub<Order>();
```

### Inline Stubs

```csharp
[KnockOff<IRepository<User>>]
[KnockOff<IRepository<Order>>]
public partial class Tests
{
    [Fact]
    public void Test()
    {
        var userRepo = new Stubs.IRepositoryOfUser();
        var orderRepo = new Stubs.IRepositoryOfOrder();
    }
}
```

**Generated class names:** `IRepositoryOfUser`, `IRepositoryOfOrder`

## Delegate Stubs

Stub Func<>/Action<> delegates.

### Basic Delegates

```csharp
[KnockOff<Func<int, bool>>]
public partial class Tests
{
    [Fact]
    public void Test()
    {
        var stub = new Stubs.FuncOfInt32AndBoolean();
        stub.Invoke.OnCall = (ko, value) => value > 0;

        Func<int, bool> predicate = stub;
        bool result = predicate(42);  // true
    }
}
```

### Named Delegates

<!-- snippet: skill-SKILL-delegate-stubs -->
```cs
[KnockOff<SkIsUniqueRule>]
[KnockOff<SkUserFactory>]
public partial class SkValidationTests
{
    // Generates: Stubs.SkIsUniqueRule, Stubs.SkUserFactory
}
```
<!-- endSnippet -->

**Usage:**
```csharp
public delegate bool IsUniqueRule(string value);

[KnockOff<IsUniqueRule>]
public partial class Tests
{
    [Fact]
    public void Test()
    {
        var rule = new Stubs.IsUniqueRule();
        rule.Invoke.OnCall = (ko, value) => value != "taken";

        IsUniqueRule ruleDelegate = rule;
        bool isUnique = ruleDelegate("newValue");  // true

        Assert.Equal(1, rule.Invoke.CallCount);
    }
}
```

### Delegate Interceptor API

All delegates have an `Invoke` interceptor:

```csharp
stub.Invoke.OnCall = (ko, args...) => result;
stub.Invoke.CallCount
stub.Invoke.WasCalled
stub.Invoke.LastArg / LastCallArgs
stub.Invoke.Reset()
```

## Events

Events support subscription tracking and programmatic raising.

### Basic Usage

<!-- snippet: skill-SKILL-pattern-events -->
```cs
[KnockOff]
public partial class SkEventPatternSourceKnockOff : ISkEventPatternSource { }
```
<!-- endSnippet -->

```csharp
public interface IEventSource
{
    event EventHandler<string> DataReceived;
}

// Subscribe
source.DataReceived += handler;
Assert.Equal(1, stub.DataReceived.AddCount);
Assert.True(stub.DataReceived.HasSubscribers);

// Raise
string? received = null;
source.DataReceived += (s, e) => received = e;
stub.DataReceived.Raise(null, "test data");
Assert.Equal("test data", received);

// Unsubscribe
source.DataReceived -= handler;
Assert.Equal(1, stub.DataReceived.RemoveCount);
```

### Event Types Supported

| Delegate Type | Raise Signature |
|---------------|-----------------|
| `EventHandler` | `Raise(object? sender, EventArgs e)` |
| `EventHandler<T>` | `Raise(object? sender, T e)` |
| `Action` | `Raise()` |
| `Action<T>` | `Raise(T arg)` |
| `Action<T1, T2>` | `Raise(T1 a1, T2 a2)` |

### Testing Subscriptions

```csharp
[Fact]
public void Should_Subscribe_To_Event()
{
    var viewModel = new ViewModel(service);

    Assert.True(stub.DataChanged.HasSubscribers);
    Assert.Equal(1, stub.DataChanged.AddCount);
}

[Fact]
public void Should_Unsubscribe_On_Dispose()
{
    var viewModel = new ViewModel(service);
    viewModel.Dispose();

    Assert.False(stub.DataChanged.HasSubscribers);
    Assert.Equal(1, stub.DataChanged.RemoveCount);
}
```

## Interface Inheritance

Inherited interface members are included.

```csharp
public interface IBase
{
    void BaseMethod();
}

public interface IDerived : IBase
{
    void DerivedMethod();
}

[KnockOff]
public partial class DerivedStub : IDerived { }

// Both methods available
stub.BaseMethod.OnCall((ko) => { });
stub.DerivedMethod.OnCall((ko) => { });
```

### Casting to Base

```csharp
IDerived derived = stub;
IBase baseInterface = stub;  // Works - implemented via IDerived
```

## Abstract Classes

Stub abstract classes like sealed classes.

```csharp
public abstract class BaseRepository
{
    public abstract string? ConnectionString { get; }
    public abstract void Save(object entity);
}

[KnockOff<BaseRepository>]
public partial class Tests
{
    [Fact]
    public void Test()
    {
        var stub = new Stubs.BaseRepository();
        stub.ConnectionString.Value = "test";
        stub.Save.OnCall = (ko, entity) => { };

        BaseRepository repo = stub.Object;
    }
}
```

## Nested Classes

Stubs can be nested in other classes.

<!-- snippet: skill-SKILL-pattern-nested -->
```cs
public partial class SkUserServiceTests  // Must be partial!
{
    [KnockOff]
    public partial class SkRepoNestedKnockOff : ISkRepository { }
}
```
<!-- endSnippet -->

**Requirements:**
- Parent class must be `partial`
- Stub class must be `partial`

**Usage:**
```csharp
var stub = new SkUserServiceTests.SkRepoNestedKnockOff();
```

## Summary

**Quick reference:**

| Feature | Syntax / Pattern |
|---------|------------------|
| **Generic methods** | `.Of<T>().OnCall(...)` |
| **Overloads** | Numeric suffix (`Method1`, `Method2`) |
| **Out params** | Explicit delegate type cast |
| **Ref params** | Explicit delegate type cast |
| **Generic interfaces** | `IRepository<User>` or `GenericStub<T>` |
| **Delegates** | `[KnockOff<Func<T>>]`, access via `Invoke` |
| **Events** | `.Raise(...)`, `.AddCount`, `.HasSubscribers` |
| **Inheritance** | Base members included automatically |
| **Abstract classes** | Use `.Object` like concrete classes |
| **Nested stubs** | Parent must be `partial` |

**Next:**
- [SKILL.md](SKILL.md) - Quick start
- [creating-stubs.md](creating-stubs.md) - Stub patterns
- [interceptor-api.md](interceptor-api.md) - Complete API reference
