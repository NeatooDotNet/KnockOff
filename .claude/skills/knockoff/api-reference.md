# Interceptor API Reference

Every interface/class member gets a dedicated interceptor on the stub.

## Interceptor Types

| Member Type | Interceptor Access | Notes |
|-------------|-------------------|-------|
| Method | `stub.MethodName` | Suffixed if overloaded (Method1, Method2) |
| Property | `stub.PropertyName` | |
| Indexer | `stub.Indexer` | `IndexerString`, `IndexerInt32` if multiple |
| Event | `stub.EventName` | |
| Generic Method | `stub.MethodName.Of<T>()` | Type-specific access |

## Method Interceptor

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `CallCount` | `int` | Number of times called |
| `WasCalled` | `bool` | `CallCount > 0` |
| `LastCallArg` | `T?` | Last argument (single-param methods) |
| `LastCallArgs` | `(T1 a, T2 b)?` | Named tuple (multi-param methods) |
| `OnCall` | Delegate | Callback when method is called |

### OnCall Signatures

| Method Signature | OnCall Type |
|------------------|-------------|
| `void M()` | `Action<TStub>` |
| `void M(T arg)` | `Action<TStub, T>` |
| `void M(T1 a, T2 b)` | `Action<TStub, T1, T2>` |
| `R M()` | `Func<TStub, R>` |
| `R M(T arg)` | `Func<TStub, T, R>` |
| `R M(T1 a, T2 b)` | `Func<TStub, T1, T2, R>` |

### Methods

| Method | Description |
|--------|-------------|
| `Reset()` | Clears `CallCount`, `LastCallArg`/`LastCallArgs`, and `OnCall` |

### Examples

```csharp
// Void method, no params
stub.Initialize.OnCall = (ko) => { /* custom */ };
Assert.True(stub.Initialize.WasCalled);

// Return method, single param
stub.GetById.OnCall = (ko, id) => new User { Id = id };
Assert.Equal(42, stub.GetById.LastCallArg);

// Multiple params - named tuple access
stub.Log.OnCall = (ko, level, message) => Console.WriteLine($"[{level}] {message}");
var args = stub.Log.LastCallArgs;
Assert.Equal("error", args?.level);
Assert.Equal("Failed", args?.message);
```

## Property Interceptor

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Value` | `T` | Backing field value |
| `GetCount` | `int` | Getter invocation count |
| `SetCount` | `int` | Setter invocation count |
| `LastSetValue` | `T?` | Last value passed to setter |
| `OnGet` | `Func<TStub, T>?` | Dynamic getter callback |
| `OnSet` | `Action<TStub, T>?` | Setter callback |

### Behavior

- `Value` is the backing field. Read/write directly.
- `OnGet` **replaces** the backing field when set (Value is ignored).
- `OnSet` **intercepts** setter (Value is NOT updated).
- `Reset()` clears counts and callbacks but **NOT** the backing Value.

### Examples

```csharp
// Static value (preferred for simple cases)
stub.Name.Value = "TestUser";
Assert.Equal("TestUser", service.Name);

// Dynamic getter
stub.CurrentTime.OnGet = (ko) => DateTime.UtcNow;

// Track setter calls
service.Name = "First";
service.Name = "Second";
Assert.Equal(2, stub.Name.SetCount);
Assert.Equal("Second", stub.Name.LastSetValue);

// Intercept setter
var captured = new List<string>();
stub.Name.OnSet = (ko, value) => captured.Add(value);

// Reset (clears counts/callbacks, NOT Value)
stub.Name.Reset();
Assert.Equal(0, stub.Name.GetCount);
```

## Indexer Interceptor

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Backing` | `Dictionary<TKey, TValue>` | Backing dictionary |
| `GetCount` | `int` | Getter invocation count |
| `SetCount` | `int` | Setter invocation count |
| `LastGetKey` | `TKey?` | Last key accessed via getter |
| `LastSetEntry` | `(TKey, TValue)?` | Last key-value from setter |
| `OnGet` | `Func<TStub, TKey, TValue>?` | Dynamic getter |
| `OnSet` | `Action<TStub, TKey, TValue>?` | Setter callback |

### Behavior

- Default getter: checks `Backing` dictionary, then returns `default(TValue)`.
- `OnGet` **replaces** default behavior (Backing is NOT checked).
- `OnSet` **intercepts** setter (Backing is NOT updated).
- `Reset()` clears counts/callbacks but **NOT** the Backing dictionary.

### Examples

```csharp
// Pre-populate backing
stub.Indexer.Backing["Key1"] = "Value1";
stub.Indexer.Backing["Key2"] = "Value2";

// Access returns backing values
var v = store["Key1"];  // "Value1"

// Track access
Assert.Equal(1, stub.Indexer.GetCount);
Assert.Equal("Key1", stub.Indexer.LastGetKey);

// Dynamic getter (must handle Backing manually if needed)
stub.Indexer.OnGet = (ko, key) =>
{
    if (key == "special") return "computed";
    return ko.Indexer.Backing.GetValueOrDefault(key, "default");
};

// Intercept setter
stub.Indexer.OnSet = (ko, key, value) => changes.Add((key, value));
```

## Event Interceptor

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `AddCount` | `int` | Times handlers added (+=) |
| `RemoveCount` | `int` | Times handlers removed (-=) |
| `HasSubscribers` | `bool` | At least one handler attached |

### Methods

| Method | Description |
|--------|-------------|
| `Raise(...)` | Raises event to all subscribers |
| `Reset()` | Clears counts **AND removes all handlers** |

### Raise Signatures

| Delegate Type | Raise Signature |
|---------------|-----------------|
| `EventHandler` | `Raise(object? sender, EventArgs e)` |
| `EventHandler<T>` | `Raise(object? sender, T e)` |
| `Action` | `Raise()` |
| `Action<T>` | `Raise(T arg)` |
| `Action<T1, T2>` | `Raise(T1 arg1, T2 arg2)` |

### Examples

```csharp
// Subscribe tracking
source.DataReceived += handler;
Assert.Equal(1, stub.DataReceived.AddCount);
Assert.True(stub.DataReceived.HasSubscribers);

// Raise event
string? received = null;
source.DataReceived += (s, e) => received = e;
stub.DataReceived.Raise(null, "test data");
Assert.Equal("test data", received);

// Raise with no subscribers is safe (no exception)
stub.DataReceived.Raise(null, "no one listening");

// Reset clears counts AND handlers
stub.DataReceived.Reset();
Assert.False(stub.DataReceived.HasSubscribers);
```

## Generic Method Interceptor

### Base Interceptor Properties

| Property | Type | Description |
|----------|------|-------------|
| `TotalCallCount` | `int` | Calls across all type arguments |
| `WasCalled` | `bool` | Called with any type argument |
| `CalledTypeArguments` | `IReadOnlyList<Type>` | All type arguments used |

### Typed Access via `.Of<T>()`

```csharp
stub.Deserialize.Of<User>().OnCall = (ko, json) => new User();
stub.Deserialize.Of<Order>().OnCall = (ko, json) => new Order();
```

### Typed Interceptor Properties

| Property | Type | Description |
|----------|------|-------------|
| `CallCount` | `int` | Calls with this type argument |
| `WasCalled` | `bool` | `CallCount > 0` |
| `LastCallArg` | `T?` | Last non-generic argument |
| `OnCall` | Delegate | Callback for this type argument |

### Examples

```csharp
// Configure per type
stub.Deserialize.Of<User>().OnCall = (ko, json) => JsonSerializer.Deserialize<User>(json);

// Per-type tracking
Assert.Equal(2, stub.Deserialize.Of<User>().CallCount);
Assert.Equal("{}", stub.Deserialize.Of<User>().LastCallArg);

// Aggregate tracking
Assert.Equal(5, stub.Deserialize.TotalCallCount);
Assert.Contains(typeof(User), stub.Deserialize.CalledTypeArguments);

// Multiple type parameters
stub.Convert.Of<string, int>().OnCall = (ko, s) => s.Length;

// Reset single type
stub.Deserialize.Of<User>().Reset();

// Reset all types
stub.Deserialize.Reset();
```

## Async Methods

Async methods use the same interceptor structure. `OnCall` returns the async type:

| Return Type | OnCall Return Type |
|-------------|-------------------|
| `Task` | `Task` |
| `Task<T>` | `Task<T>` |
| `ValueTask` | `ValueTask` |
| `ValueTask<T>` | `ValueTask<T>` |

```csharp
// Task<T>
stub.GetByIdAsync.OnCall = (ko, id) =>
    Task.FromResult<User?>(new User { Id = id });

// Task (void equivalent)
stub.InitializeAsync.OnCall = (ko) => Task.CompletedTask;

// Simulate failure
stub.SaveAsync.OnCall = (ko, entity) =>
    Task.FromException<int>(new DbException("Connection lost"));

// Simulate delay
stub.FetchAsync.OnCall = async (ko, url) =>
{
    await Task.Delay(100);
    return "response";
};
```

## Reset Behavior Summary

| Interceptor | Reset Clears | Reset Does NOT Clear |
|-------------|--------------|----------------------|
| Method | CallCount, LastCallArg, OnCall | — |
| Property | GetCount, SetCount, LastSetValue, OnGet, OnSet | **Value (backing field)** |
| Indexer | GetCount, SetCount, LastGetKey, LastSetEntry, OnGet, OnSet | **Backing dictionary** |
| Event | AddCount, RemoveCount, **handlers** | — |
| Generic | All typed intercepts, CalledTypeArguments | — |

## Smart Defaults

When no callback is set, methods return sensible defaults:

| Return Type | Default |
|-------------|---------|
| Value types (`int`, `bool`) | `default(T)` (0, false) |
| Nullable refs (`string?`) | `null` |
| Types with `new()` | `new T()` |
| Collection interfaces | Concrete type (`IList<T>` → `new List<T>()`) |
| Non-nullable without ctor | Throws `InvalidOperationException` |

Collection mappings:
- `IEnumerable<T>`, `IList<T>`, `ICollection<T>` → `List<T>`
- `IReadOnlyList<T>`, `IReadOnlyCollection<T>` → `List<T>`
- `IDictionary<K,V>`, `IReadOnlyDictionary<K,V>` → `Dictionary<K,V>`
- `ISet<T>` → `HashSet<T>`
