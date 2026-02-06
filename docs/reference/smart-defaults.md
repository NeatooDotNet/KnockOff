# Smart Defaults

**Smart defaults** are the fallback values returned by stub methods when no explicit configuration is provided. Understanding the priority order and default behavior helps you write cleaner tests by only configuring what matters.

## Priority Order

KnockOff determines what to return from a stub method using this priority:

1. **OnCall callback** - Explicit configuration via `MethodName.OnCall(...)`
2. **User method** - Your implementation in the stub class
3. **Source** - Delegation to another instance via `Source(T)`
4. **Smart default** - Automatic default value (this document)

Smart defaults apply only when none of the higher-priority options (OnCall, user method, or Source) are configured.

---

## Value Types

Value types return `default(T)`:
- Numeric types (`int`, `long`, `decimal`, etc.) return `0`
- `bool` returns `false`
- `DateTime` returns `0001-01-01T00:00:00`
- Structs return default-initialized instances

<!-- snippet: smart-defaults-value-types -->
```cs
// No configuration needed - value types return default(T)
int count = service.GetCount();      // returns 0
bool enabled = service.IsEnabled();  // returns false
```
<!-- endSnippet -->

---

## Nullable Types

Nullable types (both reference and value types) return `null`:
- Nullable reference types: `string?`, `T?`
- Nullable value types: `int?`, `bool?`, `DateTime?`

<!-- snippet: smart-defaults-nullable -->
```cs
// Nullable types return null (both reference and value types)
string? name = service.GetOptionalName();  // returns null
int? count = service.GetOptionalCount();   // returns null
```
<!-- endSnippet -->

This matches C#'s nullable type semantics where null is an expected value for both nullable reference types and nullable value types.

---

## Types with Parameterless Constructor

Types with a public parameterless constructor return `new T()`:

<!-- snippet: smart-defaults-ctor -->
```cs
// Types with parameterless constructor return new T()
AppConfig config = service.GetConfig();  // returns new AppConfig()
```
<!-- endSnippet -->

This ensures non-nullable return types provide valid instances rather than throwing.

---

## Collection Interfaces

Common collection interfaces return new, empty collections:

| Return Type | Default Value |
|------------|---------------|
| `IEnumerable<T>` | `new List<T>()` |
| `ICollection<T>` | `new List<T>()` |
| `IList<T>` | `new List<T>()` |
| `IReadOnlyCollection<T>` | `new List<T>()` |
| `IReadOnlyList<T>` | `new List<T>()` |
| `ISet<T>` | `new HashSet<T>()` |
| `IDictionary<TKey, TValue>` | `new Dictionary<TKey, TValue>()` |
| `IReadOnlyDictionary<TKey, TValue>` | `new Dictionary<TKey, TValue>()` |

<!-- snippet: smart-defaults-collections -->
```cs
// Collection interfaces return empty, non-null collections
IEnumerable<User> users = service.GetUsers();       // returns new List<User>()
IDictionary<string, string> meta = service.GetMetadata();  // returns new Dictionary<>()
```
<!-- endSnippet -->

Empty collections prevent null reference exceptions in test code that iterates or queries results.

---

## Non-Nullable Without Constructor

Non-nullable reference types **without** a parameterless constructor cannot be instantiated automatically. When no configuration is provided, the stub method throws `InvalidOperationException`.

This fail-fast behavior prevents subtle bugs from returning null where the type system says null is invalid.

<!-- snippet: smart-defaults-throw -->
```cs
// Types without parameterless constructor throw if not configured
// factory.GetUser(); // throws InvalidOperationException

// Fix: configure OnCall to provide the value
stub.GetUser.OnCall(() => new UserWithRequiredCtor(1, "Configured"));
```
<!-- endSnippet -->

**How to fix:**
- **Configure OnCall** - Provide explicit return value
- **Implement user method** - Add your own implementation in the stub class
- **Use Source** - Delegate to a real instance

---

## Complete Mapping Table

| Return Type | Default Value | Notes |
|------------|---------------|-------|
| `int`, `long`, `byte`, etc. | `0` | All numeric value types |
| `bool` | `false` | |
| `decimal`, `float`, `double` | `0.0` | Floating point types |
| `DateTime` | `0001-01-01T00:00:00` | `default(DateTime)` |
| `TimeSpan` | `00:00:00` | `default(TimeSpan)` |
| `Guid` | `00000000-0000-0000-0000-000000000000` | `default(Guid)` |
| Custom struct | Default-initialized | All fields set to their defaults |
| `string?` | `null` | Nullable reference type |
| `T?` (class) | `null` | Nullable reference type |
| `int?`, `bool?`, etc. | `null` | Nullable value type |
| `T` with `new()` constraint | `new T()` | Has parameterless constructor |
| `IEnumerable<T>` | `new List<T>()` | Empty list |
| `ICollection<T>` | `new List<T>()` | Empty list |
| `IList<T>` | `new List<T>()` | Empty list |
| `IReadOnlyCollection<T>` | `new List<T>()` | Empty list |
| `IReadOnlyList<T>` | `new List<T>()` | Empty list |
| `ISet<T>` | `new HashSet<T>()` | Empty set |
| `IDictionary<TKey, TValue>` | `new Dictionary<TKey, TValue>()` | Empty dictionary |
| `IReadOnlyDictionary<TKey, TValue>` | `new Dictionary<TKey, TValue>()` | Empty dictionary |
| `T` (class, no ctor) | **Throws** `InvalidOperationException` | Cannot instantiate |
| `void` | N/A | No return value |
| `Task` | `Task.CompletedTask` | Async void equivalent |
| `Task<T>` | `Task.FromResult(default(T))` | Async with smart default for T |
| `ValueTask` | `default(ValueTask)` | Completed ValueTask |
| `ValueTask<T>` | `new ValueTask<T>(default(T))` | ValueTask with smart default for T |

---

## Task and ValueTask Defaults

Async return types use smart defaults for their inner type:

- `Task` returns a completed task
- `Task<T>` returns a completed task with `default(T)` (following smart default rules)
- `ValueTask` returns a completed value task
- `ValueTask<T>` returns a completed value task with `default(T)` (following smart default rules)

<!-- snippet: smart-defaults-async -->
```cs
// Async methods return completed tasks with smart defaults for inner type
int count = await service.GetCountAsync();  // returns Task.FromResult(0)
await service.CompleteAsync();              // returns Task.CompletedTask
```
<!-- endSnippet -->

This allows async stub methods to complete synchronously with predictable values when not configured.

---

## When Smart Defaults Apply

Smart defaults only apply when:
1. No `OnCall` callback is configured
2. No user method is implemented in the stub
3. No `Source` delegation is configured

To override smart defaults:

**Option 1: OnCall**
<!-- snippet: smart-defaults-override-oncall -->
```cs
stub.GetUser.OnCall(() => new User { Name = "Test" });
```
<!-- endSnippet -->

**Option 2: User method**
<!-- snippet: smart-defaults-override-user-method -->
```cs
protected override User? GetUser_() => new User { Name = "Test" };
```
<!-- endSnippet -->

**Option 3: Source**
<!-- snippet: smart-defaults-override-source -->
```cs
stub.Source(new RealOverridableService());
```
<!-- endSnippet -->

---

## See Also

- [Interceptor API Reference](interceptor-api.md) - OnCall and other explicit configuration
- [User Methods](../guides/user-methods.md) - Custom implementations
- [Source Delegation](../guides/source-delegation.md) - Delegating to real instances
- [Getting Started](../getting-started.md) - First steps with KnockOff
- [Reference Documentation](../README.md) - Complete API reference

---

**UPDATED:** 2026-01-25
