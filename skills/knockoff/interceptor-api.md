---
skill: knockoff
topic: interceptor-api
audience: intermediate
---

# Interceptor API Reference

Complete reference for method, property, indexer, and event interceptors.

## Overview

Every interface/class member gets a dedicated interceptor:

| Member Type | Interceptor Access | Notes |
|-------------|-------------------|-------|
| Method | `knockOff.MethodName` | Suffixed if overloaded (`Method1`, `Method2`) |
| Property | `knockOff.PropertyName` | |
| Indexer | `knockOff.Indexer` | Type-suffixed if multiple (`IndexerString`) |
| Event | `knockOff.EventName` | |

## Method Interceptors

### API Surface

| Member | Type | Description |
|--------|------|-------------|
| `OnCall` | Delegate property | Callback when method is called (inline stubs) |
| `OnCall(callback)` | Method | Registers callback, returns tracking (standalone stubs) |
| `CallCount` | `int` | Number of times called |
| `WasCalled` | `bool` | `CallCount > 0` |
| `LastArg` | `T?` | Last argument (single-param methods) |
| `LastCallArgs` | Named tuple | Last arguments (multi-param methods) |
| `Reset()` | Method | Clears all tracking and callbacks |

### OnCall Signatures

Delegate type depends on method signature:

| Method | OnCall Type |
|--------|-------------|
| `void M()` | `Action<TStub>` |
| `void M(T arg)` | `Action<TStub, T>` |
| `void M(T1 a, T2 b)` | `Action<TStub, T1, T2>` |
| `R M()` | `Func<TStub, R>` |
| `R M(T arg)` | `Func<TStub, T, R>` |
| `R M(T1 a, T2 b)` | `Func<TStub, T1, T2, R>` |

**First parameter is always the stub instance (`ko`).**

### Basic Examples

```csharp
// Void method, no params
knockOff.Initialize.OnCall((ko) => { /* custom */ });
Assert.True(knockOff.Initialize.WasCalled);

// Return method, single param
knockOff.GetById.OnCall((ko, id) => new User { Id = id });
Assert.Equal(42, knockOff.GetById.LastArg);

// Multiple params - named tuple access
knockOff.Log.OnCall((ko, level, message) => Console.WriteLine($"[{level}] {message}"));
var args = knockOff.Log.LastCallArgs;
Assert.Equal("error", args?.level);
Assert.Equal("Failed", args?.message);
```

### Async Methods

Async methods use the same pattern:

| Return Type | OnCall Return Type |
|-------------|-------------------|
| `Task` | `Task` |
| `Task<T>` | `Task<T>` |
| `ValueTask` | `ValueTask` |
| `ValueTask<T>` | `ValueTask<T>` |

<!-- snippet: skill-SKILL-pattern-async -->
```cs
[KnockOff]
public partial class SkAsyncPatternRepositoryKnockOff : ISkAsyncPatternRepository { }
```
<!-- endSnippet -->

**Examples:**
```csharp
// Task<T>
knockOff.GetByIdAsync.OnCall((ko, id) =>
    Task.FromResult<User?>(new User { Id = id }));

// Task (void equivalent)
knockOff.InitializeAsync.OnCall((ko) => Task.CompletedTask);

// Simulate failure
knockOff.SaveAsync.OnCall((ko, entity) =>
    Task.FromException<int>(new DbException("Connection lost")));

// Simulate delay
knockOff.FetchAsync.OnCall(async (ko, url) =>
{
    await Task.Delay(100);
    return "response";
});
```

### Common Patterns

#### Conditional Returns

<!-- snippet: skill-SKILL-pattern-conditional -->
```cs
[KnockOff]
public partial class SkPatternServiceKnockOff : ISkPatternService { }
```
<!-- endSnippet -->

```csharp
knockOff.GetUser.OnCall((ko, id) => id switch
{
    1 => new User { Name = "Admin" },
    2 => new User { Name = "Guest" },
    _ => null
});
```

#### Throwing Exceptions

```csharp
knockOff.Connect.OnCall((ko) =>
    throw new InvalidOperationException("Connection failed"));
```

#### Sequential Returns

```csharp
var values = new Queue<int>([1, 2, 3]);
knockOff.GetNext.OnCall((ko) => values.Dequeue());

Assert.Equal(1, service.GetNext());
Assert.Equal(2, service.GetNext());
Assert.Equal(3, service.GetNext());
```

#### Call History

```csharp
var allCalls = new List<(string to, string subject)>();
knockOff.SendEmail.OnCall((ko, to, subject, body) =>
{
    allCalls.Add((to, subject));
});

service.SendEmail("a@test.com", "S1", "B1");
service.SendEmail("b@test.com", "S2", "B2");

Assert.Equal(2, allCalls.Count);
Assert.Equal("a@test.com", allCalls[0].to);
```

### Tracking

```csharp
// Was called?
Assert.True(knockOff.Method.WasCalled);
Assert.False(knockOff.Method.WasCalled);

// Call count
Assert.Equal(1, knockOff.Method.CallCount);
Assert.Equal(0, knockOff.Method.CallCount);

// Single argument
service.GetById(42);
Assert.Equal(42, knockOff.GetById.LastArg);

// Multiple arguments (named tuple)
service.Log("error", "Failed");
Assert.Equal("error", knockOff.Log.LastCallArgs?.level);
Assert.Equal("Failed", knockOff.Log.LastCallArgs?.message);
```

### Reset

```csharp
knockOff.Process.Reset();

// Clears:
Assert.Equal(0, knockOff.Process.CallCount);
Assert.Null(knockOff.Process.LastArg);
Assert.Null(knockOff.Process.OnCall);
```

## Property Interceptors

### API Surface

| Member | Type | Description |
|--------|------|-------------|
| `Value` | `T` | Backing field value |
| `OnGet` | `Func<TStub, T>?` | Dynamic getter callback |
| `OnSet` | `Action<TStub, T>?` | Setter callback |
| `GetCount` | `int` | Getter invocation count |
| `SetCount` | `int` | Setter invocation count |
| `LastSetValue` | `T?` | Last value passed to setter |
| `Reset()` | Method | Clears counts/callbacks (NOT Value) |

### Behavior

**Getter priority:**
1. `OnGet` (if set) - returns dynamic value
2. `Value` - returns backing field
3. `default(T)` - if Value not set

**Setter behavior:**
- If `OnSet` is set → calls callback, Value NOT updated
- If `OnSet` is null → updates Value

### Examples

```csharp
// Static value (preferred for simple cases)
knockOff.Name.Value = "TestUser";
Assert.Equal("TestUser", service.Name);

// Dynamic getter
knockOff.CurrentTime.OnGet = (ko) => DateTime.UtcNow;

// Track setter calls
service.Name = "First";
service.Name = "Second";
Assert.Equal(2, knockOff.Name.SetCount);
Assert.Equal("Second", knockOff.Name.LastSetValue);

// Intercept setter
var captured = new List<string>();
knockOff.Name.OnSet = (ko, value) => captured.Add(value);
service.Name = "Test";
Assert.Contains("Test", captured);
```

### State-Dependent Properties

```csharp
// Property depends on method call
knockOff.IsConnected.OnGet = (ko) => ko.Connect.WasCalled;
```

### Reset Behavior

```csharp
knockOff.Name.Value = "Test";
knockOff.Name.Reset();

// Clears:
Assert.Equal(0, knockOff.Name.GetCount);
Assert.Equal(0, knockOff.Name.SetCount);
Assert.Null(knockOff.Name.LastSetValue);
Assert.Null(knockOff.Name.OnGet);
Assert.Null(knockOff.Name.OnSet);

// Does NOT clear:
Assert.Equal("Test", knockOff.Name.Value);  // Still there!
```

## Indexer Interceptors

### API Surface

| Member | Type | Description |
|--------|------|-------------|
| `Backing` | `Dictionary<TKey, TValue>` | Backing dictionary |
| `OnGet` | `Func<TStub, TKey, TValue>?` | Dynamic getter |
| `OnSet` | `Action<TStub, TKey, TValue>?` | Setter callback |
| `GetCount` | `int` | Getter invocation count |
| `SetCount` | `int` | Setter invocation count |
| `LastGetKey` | `TKey?` | Last key accessed via getter |
| `LastSetEntry` | `(TKey, TValue)?` | Last key-value from setter |
| `Reset()` | Method | Clears counts/callbacks (NOT Backing) |

### Behavior

**Getter priority:**
1. `OnGet` (if set) - returns dynamic value
2. `Backing[key]` - checks dictionary
3. `default(TValue)` - if key not in Backing

**Setter behavior:**
- If `OnSet` is set → calls callback, Backing NOT updated
- If `OnSet` is null → updates Backing[key]

### Examples

```csharp
// Pre-populate backing
knockOff.Indexer.Backing["Key1"] = "Value1";
knockOff.Indexer.Backing["Key2"] = "Value2";

// Access returns backing values
var v = store["Key1"];  // "Value1"
Assert.Equal(1, knockOff.Indexer.GetCount);
Assert.Equal("Key1", knockOff.Indexer.LastGetKey);

// Dynamic getter
knockOff.Indexer.OnGet = (ko, key) =>
{
    if (key == "special") return "computed";
    return ko.Indexer.Backing.GetValueOrDefault(key, "default");
};

// Intercept setter
var changes = new List<(string, string)>();
knockOff.Indexer.OnSet = (ko, key, value) => changes.Add((key, value));
```

### Multiple Indexers

If interface has multiple indexers, they're named by key type:

```csharp
public interface IMultiIndexer
{
    string this[string key] { get; set; }  // knockOff.IndexerString
    int this[int index] { get; set; }      // knockOff.IndexerInt32
}
```

### Reset Behavior

```csharp
knockOff.Indexer.Backing["key"] = "value";
knockOff.Indexer.Reset();

// Clears:
Assert.Equal(0, knockOff.Indexer.GetCount);
Assert.Equal(0, knockOff.Indexer.SetCount);

// Does NOT clear:
Assert.Equal("value", knockOff.Indexer.Backing["key"]);  // Still there!
```

## Event Interceptors

### API Surface

| Member | Type | Description |
|--------|------|-------------|
| `Raise(...)` | Method | Raises event to all subscribers |
| `AddCount` | `int` | Times handlers added (+=) |
| `RemoveCount` | `int` | Times handlers removed (-=) |
| `HasSubscribers` | `bool` | At least one handler attached |
| `Reset()` | Method | Clears counts AND removes all handlers |

### Raise Signatures

| Delegate Type | Raise Signature |
|---------------|-----------------|
| `EventHandler` | `Raise(object? sender, EventArgs e)` |
| `EventHandler<T>` | `Raise(object? sender, T e)` |
| `Action` | `Raise()` |
| `Action<T>` | `Raise(T arg)` |
| `Action<T1, T2>` | `Raise(T1 arg1, T2 arg2)` |

### Examples

<!-- snippet: skill-SKILL-pattern-events -->
```cs
[KnockOff]
public partial class SkEventPatternSourceKnockOff : ISkEventPatternSource { }
```
<!-- endSnippet -->

```csharp
// Subscribe tracking
source.DataReceived += handler;
Assert.Equal(1, knockOff.DataReceived.AddCount);
Assert.True(knockOff.DataReceived.HasSubscribers);

// Raise event
string? received = null;
source.DataReceived += (s, e) => received = e;
knockOff.DataReceived.Raise(null, "test data");
Assert.Equal("test data", received);

// Raise with no subscribers is safe (no exception)
knockOff.DataReceived.Raise(null, "no one listening");

// Test unsubscription
viewModel.Dispose();
Assert.Equal(1, knockOff.DataChanged.RemoveCount);
Assert.False(knockOff.DataChanged.HasSubscribers);
```

### Reset Behavior

```csharp
knockOff.DataReceived.Reset();

// Clears counts
Assert.Equal(0, knockOff.DataReceived.AddCount);
Assert.Equal(0, knockOff.DataReceived.RemoveCount);

// Removes all handlers
Assert.False(knockOff.DataReceived.HasSubscribers);
```

**Note:** `Reset()` for events is destructive - it removes all subscribed handlers!

## Cross-Interceptor Access

Interceptors can check each other's state:

```csharp
// Check if method was called
knockOff.Process.OnCall((ko, value) =>
{
    if (!ko.Initialize.WasCalled)
        throw new InvalidOperationException("Not initialized");
});

// Property depends on method
knockOff.IsConnected.OnGet = (ko) => ko.Connect.WasCalled;
```

## Reset Summary

| Interceptor | Reset Clears | Reset Does NOT Clear |
|-------------|--------------|----------------------|
| **Method** | CallCount, LastArg, OnCall | — |
| **Property** | GetCount, SetCount, LastSetValue, OnGet, OnSet | **Value (backing field)** |
| **Indexer** | GetCount, SetCount, LastGetKey, LastSetEntry, OnGet, OnSet | **Backing dictionary** |
| **Event** | AddCount, RemoveCount, **handlers** | — |

## Smart Defaults

When no callback or user method is set:

| Return Type | Default |
|-------------|---------|
| `int`, `bool`, value types | `default(T)` (0, false) |
| `string?`, nullable refs | `null` |
| `List<T>` | `new List<T>()` |
| `IList<T>`, `IEnumerable<T>` | `new List<T>()` |
| `ICollection<T>`, `IReadOnlyCollection<T>` | `new List<T>()` |
| `Dictionary<K,V>` | `new Dictionary<K,V>()` |
| `IDictionary<K,V>` | `new Dictionary<K,V>()` |
| `IReadOnlyDictionary<K,V>` | `new Dictionary<K,V>()` |
| `ISet<T>` | `new HashSet<T>()` |
| Types with `new()` | `new T()` |
| Non-nullable without ctor | Throws `InvalidOperationException` |

## Next Steps

- [advanced.md](advanced.md) - Generics, overloads, out/ref parameters
- [troubleshooting.md](troubleshooting.md) - Common issues and debugging
- [creating-stubs.md](creating-stubs.md) - Stub patterns and user methods
