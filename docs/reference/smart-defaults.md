# Smart Defaults

**Smart defaults** are the fallback values returned by stub methods when no explicit configuration is provided. Understanding the priority order and default behavior helps you write cleaner tests by only configuring what matters.

## Priority Order

KnockOff determines what to return from a stub method using this priority:

1. **OnCall callback** - Explicit configuration via `Interceptors.MethodName.OnCall = ...`
2. **User method** - Your implementation in the stub class
3. **Source** - Delegation to another instance via `Source(T)`
4. **Smart default** - Automatic default value (this document)

If none of the above are configured, KnockOff provides a sensible default value based on the return type.

---

## Value Types

Value types return `default(T)`:
- Numeric types (`int`, `long`, `decimal`, etc.) return `0`
- `bool` returns `false`
- `DateTime` returns `0001-01-01T00:00:00`
- Structs return default-initialized instances

<!-- snippet: smart-defaults-value-types -->
```cs
[Fact]
public void ValueTypes_ReturnDefault()
{
    var stub = new ValueTypeServiceStub();
    IValueTypeService service = stub;

    // No configuration - smart defaults apply

    // int defaults to 0
    Assert.Equal(0, service.GetCount());

    // bool defaults to false
    Assert.False(service.IsEnabled());

    // decimal defaults to 0.0m
    Assert.Equal(0.0m, service.GetRate());

    // DateTime defaults to default(DateTime)
    Assert.Equal(default(DateTime), service.GetTimestamp());
}
```
<!-- endSnippet -->

---

## Nullable Reference Types

Nullable reference types (`string?`, `T?`) return `null`:

<!-- snippet: smart-defaults-nullable -->
```cs
[Fact]
public void NullableTypes_ReturnNull()
{
    var stub = new NullableServiceStub();
    INullableService service = stub;

    // No configuration - smart defaults apply

    // string? returns null
    Assert.Null(service.GetOptionalName());

    // User? returns null
    Assert.Null(service.FindUserById(42));
}
```
<!-- endSnippet -->

This matches C#'s nullable reference type semantics where null is an expected value.

---

## Types with Parameterless Constructor

Types with a public parameterless constructor return `new T()`:

<!-- snippet: smart-defaults-ctor -->
```cs
[Fact]
public void TypesWithCtor_ReturnNewInstance()
{
    var stub = new ConfigServiceStub();
    IConfigService service = stub;

    // No configuration - smart defaults apply

    // Types with parameterless constructor return new T()
    var config = service.GetConfig();
    Assert.NotNull(config);
    Assert.Equal("default", config.Environment); // Default property value

    var options = service.GetOptions();
    Assert.NotNull(options);
    Assert.False(options.FeatureA); // Default property value
}
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
[Fact]
public void Collections_ReturnEmptyInstances()
{
    var stub = new CollectionServiceStub();
    ICollectionService service = stub;

    // No configuration - smart defaults apply

    // IEnumerable<T> returns empty List<T>
    var users = service.GetUsers();
    Assert.NotNull(users);
    Assert.Empty(users);

    // IList<T> returns empty List<T>
    var tags = service.GetTags();
    Assert.NotNull(tags);
    Assert.Empty(tags);

    // IReadOnlyList<T> returns empty List<T>
    var ids = service.GetIds();
    Assert.NotNull(ids);
    Assert.Empty(ids);

    // IDictionary<K,V> returns empty Dictionary<K,V>
    var metadata = service.GetMetadata();
    Assert.NotNull(metadata);
    Assert.Empty(metadata);

    // ISet<T> returns empty HashSet<T>
    var keys = service.GetUniqueKeys();
    Assert.NotNull(keys);
    Assert.Empty(keys);
}
```
<!-- endSnippet -->

Empty collections prevent null reference exceptions in test code that iterates or queries results.

---

## Non-Nullable Without Constructor

Non-nullable reference types **without** a parameterless constructor cannot be instantiated automatically. When no configuration is provided, the stub method throws `InvalidOperationException`:

<!-- snippet: smart-defaults-throw -->
```cs
[Fact]
public void TypeWithoutCtor_ThrowsWithoutConfiguration()
{
    var stub = new UserFactoryStub();
    IUserFactory factory = stub;

    // User has no parameterless constructor, so smart defaults can't create one
    // Without OnCall, user method, or Source, it throws

    // Note: The actual behavior depends on how the generator handles this.
    // In strict mode, it would throw. In non-strict, it returns default (null).
    // For non-nullable return types without ctor, configure explicitly.

    // Configure OnCall to provide value
    stub.GetUser.OnCall((ko) => new User { Id = 1, Name = "Configured" });

    var user = factory.GetUser();
    Assert.NotNull(user);
    Assert.Equal("Configured", user.Name);
}
```
<!-- endSnippet -->

**When this happens:**
- **Configure OnCall** - Provide explicit return value
- **Implement user method** - Add your own implementation in the stub class
- **Use Source** - Delegate to a real instance

This fail-fast behavior prevents subtle bugs from returning null where the type system says null is invalid.

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
[Fact]
public async Task AsyncTypes_ReturnCompletedTasks()
{
    var stub = new AsyncDefaultsServiceStub();
    IAsyncDefaultsService service = stub;

    // No configuration - smart defaults apply

    // Task<User?> returns completed task with null
    var user = await service.GetUserAsync(1);
    Assert.Null(user);

    // Task<int> returns completed task with 0
    var count = await service.GetCountAsync();
    Assert.Equal(0, count);

    // Task returns completed task
    await service.CompleteAsync(); // Should not throw

    // ValueTask<bool> returns completed with false
    var isValid = await service.IsValidAsync();
    Assert.False(isValid);
}
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
```csharp
stub.Interceptors.GetUser.OnCall = _ => new User("Test");
```

**Option 2: User method**
```csharp
public User GetUser() => new User("Test");
```

**Option 3: Source**
```csharp
stub.Source(realRepository);
```

---

## See Also

- [OnCall Reference](oncall.md) - Explicit configuration
- [User Methods](../guides/user-methods.md) - Custom implementations
- [Source Delegation](../guides/source-delegation.md) - Delegating to real instances
