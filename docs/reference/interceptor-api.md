# Interceptor API Reference

Every interface member gets a dedicated Interceptor class in its interface Interceptors property. This reference covers the complete API for each intercept type.

## Interceptor Types

| Interface Member | Interceptor Type | Access Pattern |
|------------------|--------------|----------------|
| Method | `{InterfaceName}_{MethodName}Interceptor` | `IInterface.MethodName` |
| Property | `{InterfaceName}_{PropertyName}Interceptor` | `IInterface.PropertyName` |
| Indexer | `{InterfaceName}_{KeyType}IndexerInterceptor` | `IInterface.StringIndexer`, `IInterface.IntIndexer`, etc. |
| Event | `{InterfaceName}_{EventName}Interceptor` | `IInterface.EventName` |
| Generic Method | `{InterfaceName}_{MethodName}Interceptor` | `IInterface.MethodName.Of<T>()` |

## Method Interceptor

For interface methods: `void M()`, `T M()`, `void M(args)`, `T M(args)`

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `CallCount` | `int` | Number of times the method was called |
| `WasCalled` | `bool` | `true` if `CallCount > 0` |
| `LastCallArg` | `T?` | Last argument (single-param methods only) |
| `LastCallArgs` | `(T1, T2, ...)?` | Last arguments as named tuple (multi-param methods) |

### Callbacks

| Property | Type | Description |
|----------|------|-------------|
| `OnCall` | See below | Callback invoked when method is called |

**OnCall Signatures:**

| Method Signature | OnCall Type |
|------------------|-------------|
| `void M()` | `Action<TKnockOff>?` |
| `void M(T arg)` | `Action<TKnockOff, T>?` |
| `void M(T1 a, T2 b, ...)` | `Action<TKnockOff, (T1 a, T2 b, ...)>?` |
| `R M()` | `Func<TKnockOff, R>?` |
| `R M(T arg)` | `Func<TKnockOff, T, R>?` |
| `R M(T1 a, T2 b, ...)` | `Func<TKnockOff, (T1 a, T2 b, ...), R>?` |

### Methods

| Method | Description |
|--------|-------------|
| `Reset()` | Clears `CallCount`, `LastCallArg`/`LastCallArgs`, and `OnCall` |
| `RecordCall(...)` | Internal - records invocation (called by generated code) |

### Examples

```csharp
// Void method, no params
Assert.True(knockOff.IService.Initialize.WasCalled);
knockOff.IService.Initialize.OnCall = (ko) => { /* custom */ };

// Return method, single param
Assert.Equal(42, knockOff.IService.GetById.LastCallArg);
knockOff.IService.GetById.OnCall = (ko, id) => new User { Id = id };

// Void method, multiple params
var args = knockOff.IService.Log.LastCallArgs;
Assert.Equal("error", args?.level);
Assert.Equal("Failed", args?.message);

knockOff.IService.Log.OnCall = (ko, level, message) =>
{
    Console.WriteLine($"[{level}] {message}");
};
```

## Property Interceptor

For interface properties: `T Prop { get; }`, `T Prop { set; }`, `T Prop { get; set; }`

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `GetCount` | `int` | Number of getter invocations |
| `SetCount` | `int` | Number of setter invocations |
| `LastSetValue` | `T?` | Last value passed to setter |

### Callbacks

| Property | Type | Description |
|----------|------|-------------|
| `OnGet` | `Func<TKnockOff, T>?` | Callback for getter (replaces backing field) |
| `OnSet` | `Action<TKnockOff, T>?` | Callback for setter (replaces backing field) |

### Methods

| Method | Description |
|--------|-------------|
| `Reset()` | Clears counts, `LastSetValue`, `OnGet`, and `OnSet` |
| `RecordGet()` | Internal - records getter invocation |
| `RecordSet(T value)` | Internal - records setter invocation |

### Behavior Notes

- When `OnGet` is set, the backing field is NOT read
- When `OnSet` is set, the backing field is NOT written
- `Reset()` does NOT clear the backing field

### Examples

```csharp
// Track property access
Assert.Equal(3, knockOff.IService.Name.GetCount);
Assert.Equal(2, knockOff.IService.Name.SetCount);
Assert.Equal("LastValue", knockOff.IService.Name.LastSetValue);

// Override getter
knockOff.IService.Name.OnGet = (ko) => "Always this";

// Override setter (capture without storing)
knockOff.IService.Name.OnSet = (ko, value) =>
{
    capturedValues.Add(value);
    // Value does NOT go to backing field
};

// Reset
knockOff.IService.Name.Reset();
Assert.Equal(0, knockOff.IService.Name.GetCount);
```

## Indexer Interceptor

For interface indexers: `T this[K key] { get; }`, `T this[K key] { get; set; }`

Interceptor naming: `{KeyTypeName}IndexerInterceptor`
- `this[string key]` → `StringIndexerInterceptor`
- `this[int index]` → `IntIndexerInterceptor`

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `GetCount` | `int` | Number of getter invocations |
| `SetCount` | `int` | Number of setter invocations |
| `LastGetKey` | `TKey?` | Last key used in getter |
| `LastSetEntry` | `(TKey key, TValue value)?` | Last key-value pair from setter |

### Callbacks

| Property | Type | Description |
|----------|------|-------------|
| `OnGet` | `Func<TKnockOff, TKey, TValue>?` | Callback for getter |
| `OnSet` | `Action<TKnockOff, TKey, TValue>?` | Callback for setter |

### Methods

| Method | Description |
|--------|-------------|
| `Reset()` | Clears all counts, lists, and callbacks |
| `RecordGet(TKey key)` | Internal - records getter invocation |
| `RecordSet(TKey key, TValue value)` | Internal - records setter invocation |

### Backing Dictionary

Each indexer has a backing dictionary:
- `{InterfaceName}_{KeyType}IndexerBacking` — e.g., `IPropertyStore_StringIndexerBacking`, `IList_IntIndexerBacking`
- Type: `Dictionary<TKey, TValue>`

### Getter Behavior

When **no `OnGet`** is set:
- Backing dictionary checked first, then `default(TValue)`

When **`OnGet` is set**:
- Callback completely replaces getter logic
- Backing dictionary NOT checked automatically
- Include it manually in your callback if needed

### Behavior Notes

- When `OnSet` is set, backing dictionary is NOT updated
- `Reset()` does NOT clear the backing dictionary

### Examples

```csharp
// Pre-populate backing
knockOff.IPropertyStore_StringIndexerBacking["Key1"] = value1;
knockOff.IPropertyStore_StringIndexerBacking["Key2"] = value2;

// Track access
_ = store["Key1"];
_ = store["Key2"];
Assert.Equal(2, knockOff.IPropertyStore.StringIndexer.GetCount);
Assert.Equal("Key2", knockOff.IPropertyStore.StringIndexer.LastGetKey);

// Dynamic getter
knockOff.IPropertyStore.StringIndexer.OnGet = (ko, key) =>
{
    if (key == "special") return specialValue;
    return ko.IPropertyStore_StringIndexerBacking.GetValueOrDefault(key);
};

// Track setter
store["NewKey"] = newValue;
Assert.Equal("NewKey", knockOff.IPropertyStore.StringIndexer.LastSetEntry?.key);

// Interceptor setter
knockOff.IPropertyStore.StringIndexer.OnSet = (ko, key, value) =>
{
    // Custom logic
    // Value does NOT go to backing dictionary
};
```

## Event Interceptor

For interface events: `event EventHandler E`, `event EventHandler<T> E`, `event Action<T> E`

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `SubscribeCount` | `int` | Number of times handlers were added |
| `UnsubscribeCount` | `int` | Number of times handlers were removed |
| `HasSubscribers` | `bool` | `true` if at least one handler is attached |
| `RaiseCount` | `int` | Number of times event was raised |
| `WasRaised` | `bool` | `true` if `RaiseCount > 0` |
| `LastRaiseArgs` | `T?` | Arguments from most recent raise |
| `AllRaises` | `IReadOnlyList<T>` | All raise arguments in order |

### Args Type by Delegate

| Delegate Type | Args Tracking Type |
|--------------|-------------------|
| `EventHandler` | `(object? sender, EventArgs e)` |
| `EventHandler<T>` | `(object? sender, T e)` |
| `Action` | None (no args) |
| `Action<T>` | `T` |
| `Action<T1, T2>` | `(T1 arg1, T2 arg2)` |
| `Action<T1, T2, T3>` | `(T1 arg1, T2 arg2, T3 arg3)` |

### Methods

| Method | Description |
|--------|-------------|
| `Raise(...)` | Raises the event and records arguments |
| `Reset()` | Clears counts and `AllRaises`, keeps handlers attached |
| `Clear()` | Clears counts, `AllRaises`, AND removes all handlers |

### Raise Signatures

| Delegate Type | Raise Overloads |
|--------------|----------------|
| `EventHandler` | `Raise()`, `Raise(sender, e)` |
| `EventHandler<T>` | `Raise(e)`, `Raise(sender, e)` |
| `Action` | `Raise()` |
| `Action<T>` | `Raise(arg)` |
| `Action<T1, T2>` | `Raise(arg1, arg2)` |

### Behavior Notes

- `Raise()` works even with no subscribers (no exception)
- `Reset()` clears tracking but keeps handlers attached
- `Clear()` clears both tracking and handlers
- All raises are tracked in `AllRaises` regardless of subscriber count

### Examples

```csharp
// Subscribe tracking
source.DataReceived += handler;
Assert.Equal(1, knockOff.IEventSource.DataReceived.SubscribeCount);
Assert.True(knockOff.IEventSource.DataReceived.HasSubscribers);

// Raise event
knockOff.IEventSource.DataReceived.Raise("test data");
Assert.True(knockOff.IEventSource.DataReceived.WasRaised);
Assert.Equal("test data", knockOff.IEventSource.DataReceived.LastRaiseArgs?.e);

// EventHandler (non-generic)
knockOff.IEventSource.Completed.Raise(); // null sender, EventArgs.Empty

// Action with params
knockOff.IEventSource.ProgressChanged.Raise(75);
knockOff.IEventSource.DataUpdated.Raise("key", 42);

// All raises
var allRaises = knockOff.IEventSource.DataReceived.AllRaises;
Assert.Equal(3, allRaises.Count);

// Reset vs Clear
knockOff.IEventSource.DataReceived.Reset();  // Clears tracking, keeps handlers
knockOff.IEventSource.DataReceived.Clear();  // Clears tracking AND handlers
```

## Reset Behavior Summary

| Interceptor Type | Reset Clears | Reset Does NOT Clear |
|--------------|--------------|----------------------|
| Method | `CallCount`, `LastCallArg`/`LastCallArgs`, `OnCall` | — |
| Property | `GetCount`, `SetCount`, `LastSetValue`, `OnGet`, `OnSet` | Backing field |
| Indexer | `GetCount`, `SetCount`, `LastGetKey`, `LastSetEntry`, `OnGet`, `OnSet` | Backing dictionary |
| Event | `SubscribeCount`, `UnsubscribeCount`, `RaiseCount`, `AllRaises` | Event handlers (use `Clear()` to remove) |
| Generic Method | All typed intercepts, `CalledTypeArguments` | — |
| Generic Method `.Of<T>()` | `CallCount`, `LastCallArg`, `OnCall` | — |

## Async Method Interceptors

Async methods use the same intercept structure as sync methods. The `OnCall` callback returns the async type:

| Return Type | OnCall Return Type |
|-------------|-------------------|
| `Task` | `Task` |
| `Task<T>` | `Task<T>` |
| `ValueTask` | `ValueTask` |
| `ValueTask<T>` | `ValueTask<T>` |

```csharp
knockOff.IRepository.GetByIdAsync.OnCall = (ko, id) =>
    Task.FromResult<User?>(new User { Id = id });

knockOff.IRepository.SaveAsync.OnCall = (ko, entity) =>
    Task.FromException<int>(new DbException("Failed"));
```

## Generic Method Interceptors

Generic methods use a two-tier intercept structure with the `.Of<T>()` pattern.

### Base Interceptor Properties

| Property | Type | Description |
|----------|------|-------------|
| `TotalCallCount` | `int` | Total calls across all type arguments |
| `WasCalled` | `bool` | `true` if called with any type argument |
| `CalledTypeArguments` | `IReadOnlyList<Type>` | All type arguments that were used |

### Base Interceptor Methods

| Method | Description |
|--------|-------------|
| `Of<T>()` | Get typed intercept for specific type argument(s) |
| `Reset()` | Clear all typed intercepts |

For multiple type parameters, use `Of<T1, T2>()` or `Of<T1, T2, T3>()`.

### Typed Interceptor Properties

Accessed via `.Of<T>()`:

| Property | Type | Description |
|----------|------|-------------|
| `CallCount` | `int` | Calls with this type argument |
| `WasCalled` | `bool` | `true` if `CallCount > 0` |
| `LastCallArg` | `T?` | Last non-generic argument (if method has params) |
| `OnCall` | Delegate | Callback for this type argument |

### Typed Interceptor Methods

| Method | Description |
|--------|-------------|
| `Reset()` | Clear this typed intercept's tracking and callback |
| `RecordCall(...)` | Internal - records invocation |

### OnCall Signatures

| Method Signature | OnCall Type |
|------------------|-------------|
| `void M<T>()` | `Action<TKnockOff>?` |
| `void M<T>(T value)` | `Action<TKnockOff, T>?` |
| `void M<T>(string s)` | `Action<TKnockOff, string>?` |
| `T M<T>()` | `Func<TKnockOff, T>?` |
| `T M<T>(string json)` | `Func<TKnockOff, string, T>?` |
| `TOut M<TIn, TOut>(TIn input)` | `Func<TKnockOff, TIn, TOut>?` |

### Examples

```csharp
// Configure per type
knockOff.ISerializer.Deserialize.Of<User>().OnCall = (ko, json) =>
    JsonSerializer.Deserialize<User>(json)!;

// Per-type tracking
Assert.Equal(2, knockOff.ISerializer.Deserialize.Of<User>().CallCount);
Assert.Equal("{...}", knockOff.ISerializer.Deserialize.Of<User>().LastCallArg);

// Aggregate tracking
Assert.Equal(5, knockOff.ISerializer.Deserialize.TotalCallCount);
Assert.True(knockOff.ISerializer.Deserialize.WasCalled);

// See which types were called
var types = knockOff.ISerializer.Deserialize.CalledTypeArguments;
// [typeof(User), typeof(Order)]

// Multiple type parameters
knockOff.IConverter.Convert.Of<string, int>().OnCall = (ko, s) => s.Length;

// Reset single type
knockOff.ISerializer.Deserialize.Of<User>().Reset();

// Reset all types
knockOff.ISerializer.Deserialize.Reset();
```

### Smart Defaults

When `OnCall` is not set, generic methods use runtime defaults:

| Return Type | Default Behavior |
|-------------|------------------|
| Value type (`int`, `bool`, etc.) | `default(T)` |
| Type with parameterless ctor | `new T()` |
| Nullable reference type (`T?`) | `null` |
| Other (no ctor) | Throws `InvalidOperationException` |
