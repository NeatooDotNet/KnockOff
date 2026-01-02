# Async Methods

KnockOff supports all async return types: `Task`, `Task<T>`, `ValueTask`, and `ValueTask<T>`.

## Supported Types

| Return Type | Default Behavior | Callback Return |
|-------------|------------------|-----------------|
| `Task` | `Task.CompletedTask` | `Task` |
| `Task<T>` | `Task.FromResult(default(T))` | `Task<T>` |
| `ValueTask` | `default(ValueTask)` | `ValueTask` |
| `ValueTask<T>` | `new ValueTask<T>(default(T))` | `ValueTask<T>` |

## Basic Usage

```csharp
public interface IAsyncRepository
{
    Task InitializeAsync();
    Task<User?> GetByIdAsync(int id);
    ValueTask<int> CountAsync();
}

[KnockOff]
public partial class AsyncRepositoryKnockOff : IAsyncRepository { }
```

### Default Behavior

Without callbacks or user methods, async methods return completed tasks:

```csharp
var knockOff = new AsyncRepositoryKnockOff();
IAsyncRepository repo = knockOff;

await repo.InitializeAsync();  // Completes immediately
var user = await repo.GetByIdAsync(1);  // Returns null (default)
var count = await repo.CountAsync();  // Returns 0 (default)
```

## User-Defined Methods

Define protected async methods for default behavior:

```csharp
[KnockOff]
public partial class AsyncRepositoryKnockOff : IAsyncRepository
{
    protected Task<User?> GetByIdAsync(int id) =>
        Task.FromResult<User?>(new User { Id = id, Name = "Default" });

    protected ValueTask<int> CountAsync() =>
        new ValueTask<int>(42);
}
```

## Callbacks

### Task Methods

```csharp
// Task (void equivalent)
knockOff.Spy.InitializeAsync.OnCall((ko) =>
{
    // Custom logic
    return Task.CompletedTask;
});

// Task<T>
knockOff.Spy.GetByIdAsync.OnCall((ko, id) =>
    Task.FromResult<User?>(new User { Id = id, Name = "Mocked" }));
```

### ValueTask Methods

```csharp
// ValueTask (void equivalent)
knockOff.Spy.DoWorkAsync.OnCall((ko) => default(ValueTask));

// ValueTask<T>
knockOff.Spy.CountAsync.OnCall((ko) => new ValueTask<int>(100));
```

## Tracking

Async methods use the same tracking as sync methods:

```csharp
await repo.GetByIdAsync(1);
await repo.GetByIdAsync(2);
await repo.GetByIdAsync(3);

Assert.Equal(3, knockOff.Spy.GetByIdAsync.CallCount);
Assert.Equal(3, knockOff.Spy.GetByIdAsync.LastCallArg);

var allIds = knockOff.Spy.GetByIdAsync.AllCalls;  // [1, 2, 3]
```

## Common Patterns

### Simulating Delays

```csharp
knockOff.Spy.GetByIdAsync.OnCall(async (ko, id) =>
{
    await Task.Delay(100);  // Simulate network latency
    return new User { Id = id };
});
```

Note: This requires the callback to be `async`.

### Simulating Failures

```csharp
// Faulted task
knockOff.Spy.SaveAsync.OnCall((ko, entity) =>
    Task.FromException<int>(new DbException("Connection lost")));

// Or throw directly in callback
knockOff.Spy.SaveAsync.OnCall((ko, entity) =>
{
    throw new DbException("Connection lost");
});
```

### Conditional Async Behavior

```csharp
knockOff.Spy.GetByIdAsync.OnCall((ko, id) =>
{
    if (id <= 0)
        return Task.FromException<User?>(new ArgumentException("Invalid ID"));

    return Task.FromResult<User?>(new User { Id = id });
});
```

### Cancellation Support

For methods with `CancellationToken`:

```csharp
public interface IService
{
    Task<Data> FetchAsync(int id, CancellationToken ct);
}

knockOff.Spy.FetchAsync.OnCall((ko, id, ct) =>
{
    ct.ThrowIfCancellationRequested();
    return Task.FromResult(new Data { Id = id });
});
```

### Sequential Async Returns

```csharp
var results = new Queue<User?>([
    new User { Name = "First" },
    new User { Name = "Second" },
    null  // Then not found
]);

knockOff.Spy.GetByIdAsync.OnCall((ko, id) =>
    Task.FromResult(results.Dequeue()));
```

### Verifying Async Call Order

```csharp
var order = new List<string>();

knockOff.Spy.StartAsync.OnCall((ko) =>
{
    order.Add("Start");
    return Task.CompletedTask;
});

knockOff.Spy.ProcessAsync.OnCall((ko) =>
{
    order.Add("Process");
    return Task.CompletedTask;
});

await service.StartAsync();
await service.ProcessAsync();

Assert.Equal(["Start", "Process"], order);
```

## ValueTask vs Task

`ValueTask` is more efficient for methods that often complete synchronously. KnockOff handles both:

```csharp
// ValueTask<T> - synchronous completion
knockOff.Spy.GetCachedAsync.OnCall((ko, key) =>
    new ValueTask<string?>(cache.GetOrDefault(key)));

// Task<T> - async completion
knockOff.Spy.FetchRemoteAsync.OnCall((ko, key) =>
    Task.FromResult(remoteData[key]));
```

## Reset

Reset works the same for async methods:

```csharp
await repo.GetByIdAsync(1);
knockOff.Spy.GetByIdAsync.Reset();

Assert.Equal(0, knockOff.Spy.GetByIdAsync.CallCount);
Assert.Null(knockOff.Spy.GetByIdAsync.OnCall);
```
