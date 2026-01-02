# Handler API Reference

Every interface member gets a dedicated Handler class in the `Spy` property. This reference covers the complete API for each handler type.

## Handler Types

| Interface Member | Handler Type | Access Pattern |
|------------------|--------------|----------------|
| Method | `{MethodName}Handler` | `Spy.MethodName` |
| Property | `{PropertyName}Handler` | `Spy.PropertyName` |
| Indexer | `{KeyType}IndexerHandler` | `Spy.StringIndexer`, `Spy.IntIndexer`, etc. |
| Event | `{EventName}Handler` | `Spy.EventName` |

## Method Handler

For interface methods: `void M()`, `T M()`, `void M(args)`, `T M(args)`

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `CallCount` | `int` | Number of times the method was called |
| `WasCalled` | `bool` | `true` if `CallCount > 0` |
| `LastCallArg` | `T` | Last argument (single-param methods only) |
| `LastCallArgs` | `(T1, T2, ...)?` | Last arguments as named tuple (multi-param methods) |
| `AllCalls` | `List<T>` or `List<(T1, T2, ...)>` | All call arguments in order |

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
| `Reset()` | Clears `CallCount`, `AllCalls`, and `OnCall` |
| `RecordCall(...)` | Internal - records invocation (called by generated code) |

### Examples

```csharp
// Void method, no params
Assert.True(knockOff.Spy.Initialize.WasCalled);
knockOff.Spy.Initialize.OnCall = (ko) => { /* custom */ };

// Return method, single param
Assert.Equal(42, knockOff.Spy.GetById.LastCallArg);
knockOff.Spy.GetById.OnCall = (ko, id) => new User { Id = id };

// Void method, multiple params
var args = knockOff.Spy.Log.LastCallArgs;
Assert.Equal("error", args?.level);
Assert.Equal("Failed", args?.message);

knockOff.Spy.Log.OnCall = (ko, args) =>
{
    var (level, message) = args;
    Console.WriteLine($"[{level}] {message}");
};
```

## Property Handler

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
Assert.Equal(3, knockOff.Spy.Name.GetCount);
Assert.Equal(2, knockOff.Spy.Name.SetCount);
Assert.Equal("LastValue", knockOff.Spy.Name.LastSetValue);

// Override getter
knockOff.Spy.Name.OnGet = (ko) => "Always this";

// Override setter (capture without storing)
knockOff.Spy.Name.OnSet = (ko, value) =>
{
    capturedValues.Add(value);
    // Value does NOT go to backing field
};

// Reset
knockOff.Spy.Name.Reset();
Assert.Equal(0, knockOff.Spy.Name.GetCount);
```

## Indexer Handler

For interface indexers: `T this[K key] { get; }`, `T this[K key] { get; set; }`

Handler naming: `{KeyTypeName}IndexerHandler`
- `this[string key]` → `StringIndexerHandler`
- `this[int index]` → `IntIndexerHandler`

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `GetCount` | `int` | Number of getter invocations |
| `SetCount` | `int` | Number of setter invocations |
| `LastGetKey` | `TKey?` | Last key used in getter |
| `AllGetKeys` | `List<TKey>` | All keys used in getter, in order |
| `LastSetEntry` | `(TKey key, TValue value)?` | Last key-value pair from setter |
| `AllSetEntries` | `List<(TKey, TValue)>` | All key-value pairs from setter |

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
- `{KeyType}IndexerBacking` — e.g., `StringIndexerBacking`, `IntIndexerBacking`
- Type: `Dictionary<TKey, TValue>`

### Priority Order (Getter)

1. `OnGet` callback (if set)
2. Backing dictionary (if key exists)
3. `default(TValue)`

**Note**: When `OnGet` is set, backing dictionary is NOT automatically checked. Include it manually in your callback if needed.

### Behavior Notes

- When `OnSet` is set, backing dictionary is NOT updated
- `Reset()` does NOT clear the backing dictionary

### Examples

```csharp
// Pre-populate backing
knockOff.StringIndexerBacking["Key1"] = value1;
knockOff.StringIndexerBacking["Key2"] = value2;

// Track access
_ = store["Key1"];
_ = store["Key2"];
Assert.Equal(2, knockOff.Spy.StringIndexer.GetCount);
Assert.Equal("Key2", knockOff.Spy.StringIndexer.LastGetKey);

// Dynamic getter
knockOff.Spy.StringIndexer.OnGet = (ko, key) =>
{
    if (key == "special") return specialValue;
    return ko.StringIndexerBacking.GetValueOrDefault(key);
};

// Track setter
store["NewKey"] = newValue;
Assert.Equal("NewKey", knockOff.Spy.StringIndexer.LastSetEntry?.key);

// Intercept setter
knockOff.Spy.StringIndexer.OnSet = (ko, key, value) =>
{
    // Custom logic
    // Value does NOT go to backing dictionary
};
```

## Event Handler

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
Assert.Equal(1, knockOff.Spy.DataReceived.SubscribeCount);
Assert.True(knockOff.Spy.DataReceived.HasSubscribers);

// Raise event
knockOff.Spy.DataReceived.Raise("test data");
Assert.True(knockOff.Spy.DataReceived.WasRaised);
Assert.Equal("test data", knockOff.Spy.DataReceived.LastRaiseArgs?.e);

// EventHandler (non-generic)
knockOff.Spy.Completed.Raise(); // null sender, EventArgs.Empty

// Action with params
knockOff.Spy.ProgressChanged.Raise(75);
knockOff.Spy.DataUpdated.Raise("key", 42);

// All raises
var allRaises = knockOff.Spy.DataReceived.AllRaises;
Assert.Equal(3, allRaises.Count);

// Reset vs Clear
knockOff.Spy.DataReceived.Reset();  // Clears tracking, keeps handlers
knockOff.Spy.DataReceived.Clear();  // Clears tracking AND handlers
```

## Reset Behavior Summary

| Handler Type | Reset Clears | Reset Does NOT Clear |
|--------------|--------------|----------------------|
| Method | `CallCount`, `AllCalls`, `OnCall` | — |
| Property | `GetCount`, `SetCount`, `LastSetValue`, `OnGet`, `OnSet` | Backing field |
| Indexer | `GetCount`, `SetCount`, `AllGetKeys`, `AllSetEntries`, `OnGet`, `OnSet` | Backing dictionary |
| Event | `SubscribeCount`, `UnsubscribeCount`, `RaiseCount`, `AllRaises` | Handlers (use `Clear()` to remove) |

## Async Method Handlers

Async methods use the same handler structure as sync methods. The `OnCall` callback returns the async type:

| Return Type | OnCall Return Type |
|-------------|-------------------|
| `Task` | `Task` |
| `Task<T>` | `Task<T>` |
| `ValueTask` | `ValueTask` |
| `ValueTask<T>` | `ValueTask<T>` |

```csharp
knockOff.Spy.GetByIdAsync.OnCall = (ko, id) =>
    Task.FromResult<User?>(new User { Id = id });

knockOff.Spy.SaveAsync.OnCall = (ko, entity) =>
    Task.FromException<int>(new DbException("Failed"));
```
