# Methods

This guide covers stubbing methods: void, return values, async, overloads, and generics.

## Basic Methods

### Void Methods

<!-- snippet: methods-void-no-params -->
```cs
public interface IMethodService
{
    void Initialize();
}

[KnockOff]
public partial class MethodServiceKnockOff : IMethodService { }
```
<!-- endSnippet -->

Configure with `OnCall`:

<!-- snippet: methods-void-callbacks -->
```cs
// No parameters
serviceKnockOff.Initialize.OnCall = (ko) =>
{
    // Custom initialization logic
};

// Single parameter
loggerKnockOff.Log.OnCall = (ko, message) =>
{
    Console.WriteLine($"Logged: {message}");
};

// Multiple parameters
loggerKnockOff.LogError.OnCall = (ko, message, ex) =>
{
    Console.WriteLine($"Error: {message} - {ex.Message}");
};
```
<!-- endSnippet -->

### Methods with Return Values

<!-- snippet: methods-return-callbacks -->
```cs
// No parameters
knockOff.Count.OnCall = (ko) => 42;

// Single parameter
knockOff.GetById.OnCall = (ko, id) => new MethodUser { Id = id };
```
<!-- endSnippet -->

### User-Defined Defaults

Define protected methods for consistent behavior across tests:

<!-- snippet: methods-user-defined -->
```cs
[KnockOff]
public partial class MethodUserDefinedKnockOff : IMethodUserDefined
{
    protected MethodUser? GetById(int id) => new MethodUser { Id = id, Name = "Default" };

    protected int Count() => 100;
}
```
<!-- endSnippet -->

Callbacks override user methods when set.

## Argument Tracking

### Single Parameter

<!-- snippet: methods-single-param -->
```cs
service.GetUser(42);

// Tracking - single parameter uses raw type (not a tuple)
int? lastId = knockOff.GetUser.LastCallArg;  // 42, not (42,)
```
<!-- endSnippet -->

### Multiple Parameters

<!-- snippet: methods-multiple-params -->
```cs
service.Process("test", 42, true);

// Tracking - named tuple with original parameter names
var args = knockOff.Process.LastCallArgs;
var name = args?.name;   // "test"
var value = args?.value; // 42
var flag = args?.flag;   // true
```
<!-- endSnippet -->

## Async Methods

KnockOff supports `Task`, `Task<T>`, `ValueTask`, and `ValueTask<T>`.

### Default Behavior

Without callbacks, async methods return completed tasks with default values:

<!-- snippet: async-methods-default-behavior -->
```cs
await repo.InitializeAsync();       // Completes immediately
var user = await repo.GetByIdAsync(1);  // Returns null (default)
var count = await repo.CountAsync();    // Returns 0 (default)
```
<!-- endSnippet -->

### Task Callbacks

<!-- snippet: async-methods-task-callbacks -->
```cs
// Task (void equivalent)
knockOff.InitializeAsync.OnCall = (ko) =>
{
    // Custom logic
    return Task.CompletedTask;
};

// Task<T>
knockOff.GetByIdAsync.OnCall = (ko, id) =>
    Task.FromResult<AsyncUser?>(new AsyncUser { Id = id, Name = "Mocked" });
```
<!-- endSnippet -->

### ValueTask Callbacks

<!-- snippet: async-methods-valuetask-callbacks -->
```cs
// ValueTask<T>
knockOff.CountAsync.OnCall = (ko) => new ValueTask<int>(100);
```
<!-- endSnippet -->

### Simulating Delays

<!-- snippet: async-methods-simulating-delays -->
```cs
knockOff.GetByIdAsync.OnCall = async (ko, id) =>
{
    await Task.Delay(100);  // Simulate network latency
    return new AsyncUser { Id = id };
};
```
<!-- endSnippet -->

### Simulating Failures

<!-- snippet: async-methods-simulating-failures-usage -->
```cs
// Faulted task
knockOff.SaveAsync.OnCall = (ko, entity) =>
    Task.FromException<int>(new InvalidOperationException("Connection lost"));

// Or throw directly in callback
knockOff.SaveAsync.OnCall = (ko, entity) =>
{
    throw new InvalidOperationException("Connection lost");
};
```
<!-- endSnippet -->

## Method Overloads

Overloaded methods get numbered interceptors:

```csharp
public interface IProcessor
{
    void Process(string data);               // Process1
    void Process(string data, int priority); // Process2
}

// Each overload has its own interceptor
stub.Process1.OnCall = (ko, data) => { };
stub.Process2.OnCall = (ko, data, priority) => { };

// Verify specific overload
Assert.Equal(1, stub.Process2.CallCount);
```

Methods without overloads don't get numeric suffixes.

## Generic Methods

```csharp
public interface ISerializer
{
    T Deserialize<T>(string json);
}

// Use .Of<T>() to access type-specific interceptors
stub.Deserialize.Of<User>().OnCall = (ko, json) => new User { Name = "Test" };
stub.Deserialize.Of<Order>().OnCall = (ko, json) => new Order { Id = 1 };

Assert.Equal(1, stub.Deserialize.Of<User>().CallCount);
```

## Common Patterns

### Conditional Returns

<!-- snippet: methods-conditional-returns -->
```cs
knockOff.GetById.OnCall = (ko, id) => id switch
{
    1 => new MethodUser { Id = 1, Name = "Admin" },
    2 => new MethodUser { Id = 2, Name = "Guest" },
    _ => null
};
```
<!-- endSnippet -->

### Sequential Returns

<!-- snippet: methods-sequential-returns-usage -->
```cs
var results = new Queue<int>([1, 2, 3]);
knockOff.GetNext.OnCall = (ko) => results.Dequeue();

var first = service.GetNext();   // 1
var second = service.GetNext();  // 2
var third = service.GetNext();   // 3
```
<!-- endSnippet -->

### Throwing Exceptions

<!-- snippet: methods-simulating-failures-usage -->
```cs
knockOff.Save.OnCall = (ko, entity) =>
{
    throw new InvalidOperationException("Connection failed");
};
```
<!-- endSnippet -->

### Accessing Stub State

<!-- snippet: methods-accessing-handler-state-usage -->
```cs
knockOff.Process.OnCall = (ko) =>
{
    if (!ko.Initialize.WasCalled)
        throw new InvalidOperationException("Not initialized");
};
```
<!-- endSnippet -->

## Priority Order

1. **Callback** (if set) — takes precedence
2. **User method** (if defined) — fallback
3. **Default** — `default(T)` for return methods

<!-- snippet: methods-priority-order-usage -->
```cs
// No callback → uses user method
var result1 = service.Calculate(5);  // 10 (5 * 2)

// Callback → overrides user method
knockOff.Calculate2.OnCall = (ko, x) => x * 100;
var result2 = service.Calculate(5);  // 500 (callback)

// Reset → back to user method
knockOff.Calculate2.Reset();
var result3 = service.Calculate(5);  // 10 (user method again)
```
<!-- endSnippet -->
