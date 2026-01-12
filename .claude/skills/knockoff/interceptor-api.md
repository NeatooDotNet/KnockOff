# Interceptor API Reference

Every interface member gets a dedicated Interceptor class. Access interceptors directly on the stub instance via `knockOff.MemberName`.

## Interceptor Types

| Interface Member | Interceptor Type | Access Pattern |
|------------------|-----------------|----------------|
| Method | `{MethodName}Interceptor` | `stub.MethodName` |
| Property | `{PropertyName}Interceptor` | `stub.PropertyName` |
| Indexer (single) | `IndexerInterceptor` | `stub.Indexer` |
| Indexer (multiple) | `Indexer{KeyType}Interceptor` | `stub.IndexerString`, `stub.IndexerInt32`, etc. |
| Event | `{EventName}Interceptor` | `stub.EventNameInterceptor` |
| Generic Method | `{MethodName}Interceptor` | `stub.MethodName.Of<T>()` |

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
| `void M(T1 a, T2 b, ...)` | `Action<TKnockOff, T1, T2, ...>?` |
| `R M()` | `Func<TKnockOff, R>?` |
| `R M(T arg)` | `Func<TKnockOff, T, R>?` |
| `R M(T1 a, T2 b, ...)` | `Func<TKnockOff, T1, T2, ..., R>?` |

### Methods

| Method | Description |
|--------|-------------|
| `Reset()` | Clears `CallCount`, `LastCallArg`/`LastCallArgs`, and `OnCall` |
| `RecordCall(...)` | Internal - records invocation (called by generated code) |

### Examples

snippet: skill-interceptor-api-method-interceptor-example

## Property Interceptor

For interface properties: `T Prop { get; }`, `T Prop { set; }`, `T Prop { get; set; }`

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Value` | `T` | Backing value returned by getter (when `OnGet` not set) |
| `GetCount` | `int` | Number of getter invocations |
| `SetCount` | `int` | Number of setter invocations |
| `LastSetValue` | `T?` | Last value passed to setter |
| `OnGet` | `Func<TKnockOff, T>?` | Getter callback (overrides `Value`) |
| `OnSet` | `Action<TKnockOff, T>?` | Setter callback (overrides writing to `Value`) |

### Methods

| Method | Description |
|--------|-------------|
| `Reset()` | Clears counts, `LastSetValue`, `Value`, `OnGet`, and `OnSet` |

### Setting Property Values

**Prefer `.Value` for simple cases:**

```csharp
// Simple - just set the value
stub.Name.Value = "Test User";

// Verbose - only needed for dynamic behavior
stub.Name.OnGet = (ko) => "Test User";
```

### Behavior

- Getter returns `OnGet` result if set, otherwise `Value`
- Setter calls `OnSet` if set, otherwise writes to `Value`
- `Reset()` clears `Value` to `default`

### Examples

snippet: skill-interceptor-api-property-interceptor-example

## Indexer Interceptor

For interface indexers. Single indexer uses `Indexer`; multiple indexers use type suffix: `IndexerString`, `IndexerInt32`, etc.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `GetCount` | `int` | Number of getter invocations |
| `SetCount` | `int` | Number of setter invocations |
| `LastGetKey` | `TKey?` | Last key accessed |
| `LastSetEntry` | `(TKey, TValue)?` | Last key-value from setter |
| `OnGet` | `Func<TKnockOff, TKey, TValue>?` | Getter callback |
| `OnSet` | `Action<TKnockOff, TKey, TValue>?` | Setter callback |

### Methods

| Method | Description |
|--------|-------------|
| `Reset()` | Clears tracking and callbacks |

### Backing Dictionary

Each indexer has a backing dictionary accessible via the `Backing` property on the interceptor:
- `stub.Indexer.Backing` for single indexer
- `stub.IndexerString.Backing` for `this[string key]` (when multiple indexers)
- `stub.IndexerInt32.Backing` for `this[int index]` (when multiple indexers)

### Getter Behavior

When **no `OnGet`** is set:
1. Backing dictionary checked first
2. Then `default(TValue)`

When **`OnGet` is set**:
- Callback completely replaces getter logic
- Backing dictionary NOT checked automatically
- Include it manually in your callback if needed

### Examples

snippet: skill-interceptor-api-indexer-interceptor-example

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

snippet: skill-interceptor-api-event-interceptor-example

## Overloaded Method Interceptors

When an interface has overloaded methods, each overload gets its own interceptor with a **numeric suffix** (1-based):

- `Process1` - first overload
- `Process2` - second overload
- `Process3` - third overload

Methods without overloads don't get a suffix.

### Properties

Each overload interceptor has its own tracking:

| Property | Type | Description |
|----------|------|-------------|
| `CallCount` | `int` | Calls to this specific overload |
| `WasCalled` | `bool` | `CallCount > 0` |
| `LastCallArg` / `LastCallArgs` | varies | Arguments for this overload only |

### Examples

snippet: skill-interceptor-api-overload-interceptor-example

## Out Parameter Methods

Methods with `out` parameters require explicit delegate type for callbacks.

### Tracking Behavior

- **Out params are NOT tracked** - they're outputs, not inputs
- Only input parameters appear in `LastCallArg`/`LastCallArgs`
- Methods with only out params track `CallCount` only

### Callback Syntax

snippet: skill-interceptor-api-out-param-callback

### No Callback Behavior

Without a callback:
- Out params initialize to `default`
- Void methods complete silently
- Methods with return values throw `InvalidOperationException`

## Ref Parameter Methods

Methods with `ref` parameters track the **input value** before any modification.

### Tracking Behavior

- **Ref params ARE tracked** - they have an input value
- `LastCallArg`/`LastCallArgs` contains the value at call time
- Modifications by callback are NOT reflected in tracking

### Callback Syntax

snippet: skill-interceptor-api-ref-param-callback

### Tracking Example

snippet: skill-interceptor-api-ref-param-tracking

## Reset Behavior Summary

| Interceptor Type | Reset Clears | Reset Does NOT Clear |
|-----------------|--------------|----------------------|
| Method | `CallCount`, `LastCallArg`/`LastCallArgs`, `OnCall` | - |
| Property | `GetCount`, `SetCount`, `LastSetValue`, `Value`, `OnGet`, `OnSet` | - |
| Indexer | `GetCount`, `SetCount`, `LastGetKey`, `LastSetEntry`, `OnGet`, `OnSet` | Backing dictionary |
| Event | `SubscribeCount`, `UnsubscribeCount`, `RaiseCount`, `AllRaises` | Handlers (use `Clear()` to remove) |
| Generic Method | All typed interceptors, `CalledTypeArguments` | - |
| Generic Method `.Of<T>()` | `CallCount`, `LastCallArg`, `OnCall` | - |

## Async Method Interceptors

Async methods use the same interceptor structure as sync methods. The `OnCall` callback returns the async type:

| Return Type | OnCall Return Type |
|-------------|-------------------|
| `Task` | `Task` |
| `Task<T>` | `Task<T>` |
| `ValueTask` | `ValueTask` |
| `ValueTask<T>` | `ValueTask<T>` |

snippet: skill-interceptor-api-async-interceptor-example

## Generic Method Interceptors

Generic methods use a two-tier interceptor with the `.Of<T>()` pattern.

### Base Interceptor Properties

| Property | Type | Description |
|----------|------|-------------|
| `TotalCallCount` | `int` | Total calls across all type arguments |
| `WasCalled` | `bool` | `true` if called with any type argument |
| `CalledTypeArguments` | `IReadOnlyList<Type>` | All type arguments used |

### Base Interceptor Methods

| Method | Description |
|--------|-------------|
| `Of<T>()` | Get typed interceptor for specific type argument(s) |
| `Reset()` | Clear all typed interceptors |

For multiple type parameters: `Of<T1, T2>()` or `Of<T1, T2, T3>()`.

### Typed Interceptor Properties (via `.Of<T>()`)

| Property | Type | Description |
|----------|------|-------------|
| `CallCount` | `int` | Calls with this type argument |
| `WasCalled` | `bool` | `true` if `CallCount > 0` |
| `LastCallArg` | `T?` | Last non-generic argument (if method has params) |
| `OnCall` | Delegate | Callback for this type argument |

### Typed Interceptor Methods

| Method | Description |
|--------|-------------|
| `Reset()` | Clear this typed interceptor |

### OnCall Signatures

| Method Signature | OnCall Type |
|------------------|-------------|
| `void M<T>()` | `Action<TKnockOff>?` |
| `void M<T>(T value)` | `Action<TKnockOff, T>?` |
| `T M<T>()` | `Func<TKnockOff, T>?` |
| `T M<T>(string json)` | `Func<TKnockOff, string, T>?` |
| `TOut M<TIn, TOut>(TIn input)` | `Func<TKnockOff, TIn, TOut>?` |

### Examples

snippet: skill-interceptor-api-generic-interceptor-example

### Smart Defaults for Generic Methods

Without `OnCall`, generic methods use runtime defaults:

| Return Type | Default Behavior |
|-------------|------------------|
| Value type | `default(T)` |
| Type with parameterless ctor | `new T()` |
| Nullable reference (`T?`) | `null` |
| Other | Throws `InvalidOperationException` |

## Smart Default Return Values

Without callback or user method, KnockOff returns sensible defaults:

| Return Type | Default Value | Example |
|-------------|---------------|---------|
| Value types | `default` | `int` → `0`, `bool` → `false` |
| Nullable refs | `null` | `string?` → `null` |
| Types with `new()` | `new T()` | `List<T>` → empty list |
| Collection interfaces | concrete type | `IList<T>` → `new List<T>()` |
| `Task` | `Task.CompletedTask` | async void-like |
| `Task<T>` | `Task.FromResult(smartDefault)` | applies smart default to `T` |
| `ValueTask` | `default(ValueTask)` | async void-like |
| `ValueTask<T>` | `new ValueTask<T>(smartDefault)` | applies smart default to `T` |
| Other non-nullable | throws `InvalidOperationException` | `string`, `IDisposable` |

**Collection Interface Mapping:**

| Interface | Concrete Type |
|-----------|---------------|
| `IEnumerable<T>`, `ICollection<T>`, `IList<T>` | `List<T>` |
| `IReadOnlyList<T>`, `IReadOnlyCollection<T>` | `List<T>` |
| `IDictionary<K,V>`, `IReadOnlyDictionary<K,V>` | `Dictionary<K,V>` |
| `ISet<T>` | `HashSet<T>` |

snippet: skill-interceptor-api-smart-defaults-example
