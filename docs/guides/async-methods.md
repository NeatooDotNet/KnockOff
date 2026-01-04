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

<!-- snippet: docs:async-methods:basic-interface -->
```csharp
public interface IAsyncRepository
{
    Task InitializeAsync();
    Task<AsyncUser?> GetByIdAsync(int id);
    ValueTask<int> CountAsync();
}

[KnockOff]
public partial class AsyncRepositoryKnockOff : IAsyncRepository { }
```
<!-- /snippet -->

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

<!-- snippet: docs:async-methods:user-defined -->
```csharp
[KnockOff]
public partial class AsyncUserDefinedKnockOff : IAsyncUserDefined
{
    protected Task<AsyncUser?> GetByIdAsync(int id) =>
        Task.FromResult<AsyncUser?>(new AsyncUser { Id = id, Name = "Default" });

    protected ValueTask<int> CountAsync() =>
        new ValueTask<int>(42);
}
```
<!-- /snippet -->

## Callbacks

### Task Methods

```csharp
// Task (void equivalent)
knockOff.IAsyncRepository.InitializeAsync.OnCall = (ko) =>
{
    // Custom logic
    return Task.CompletedTask;
};

// Task<T>
knockOff.IAsyncRepository.GetByIdAsync.OnCall = (ko, id) =>
    Task.FromResult<User?>(new User { Id = id, Name = "Mocked" });
```

### ValueTask Methods

```csharp
// ValueTask (void equivalent)
knockOff.IAsyncService.DoWorkAsync.OnCall = (ko) => default(ValueTask);

// ValueTask<T>
knockOff.IAsyncRepository.CountAsync.OnCall = (ko) => new ValueTask<int>(100);
```

## Tracking

Async methods use the same tracking as sync methods:

```csharp
await repo.GetByIdAsync(1);
await repo.GetByIdAsync(2);
await repo.GetByIdAsync(3);

Assert.Equal(3, knockOff.IAsyncRepository.GetByIdAsync.CallCount);
Assert.Equal(3, knockOff.IAsyncRepository.GetByIdAsync.LastCallArg);

var allIds = knockOff.IAsyncRepository.GetByIdAsync.AllCalls;  // [1, 2, 3]
```

## Common Patterns

### Simulating Delays

```csharp
knockOff.IAsyncRepository.GetByIdAsync.OnCall = async (ko, id) =>
{
    await Task.Delay(100);  // Simulate network latency
    return new User { Id = id };
};
```

Note: This requires the callback to be `async`.

### Simulating Failures

<!-- snippet: docs:async-methods:simulating-failures -->
```csharp
[KnockOff]
public partial class AsyncSaveKnockOff : IAsyncSave { }
```
<!-- /snippet -->

```csharp
// Faulted task
knockOff.IAsyncSave.SaveAsync.OnCall = (ko, entity) =>
    Task.FromException<int>(new DbException("Connection lost"));

// Or throw directly in callback
knockOff.IAsyncSave.SaveAsync.OnCall = (ko, entity) =>
{
    throw new DbException("Connection lost");
};
```

### Conditional Async Behavior

```csharp
knockOff.IAsyncRepository.GetByIdAsync.OnCall = (ko, id) =>
{
    if (id <= 0)
        return Task.FromException<User?>(new ArgumentException("Invalid ID"));

    return Task.FromResult<User?>(new User { Id = id });
};
```

### Cancellation Support

For methods with `CancellationToken`:

<!-- snippet: docs:async-methods:cancellation -->
```csharp
public interface IAsyncFetch
{
    Task<AsyncData> FetchAsync(int id, CancellationToken ct);
}

[KnockOff]
public partial class AsyncFetchKnockOff : IAsyncFetch { }
```
<!-- /snippet -->

```csharp
knockOff.IAsyncFetch.FetchAsync.OnCall = (ko, id, ct) =>
{
    ct.ThrowIfCancellationRequested();
    return Task.FromResult(new AsyncData { Id = id });
};
```

### Sequential Async Returns

```csharp
var results = new Queue<User?>([
    new User { Name = "First" },
    new User { Name = "Second" },
    null  // Then not found
]);

knockOff.IAsyncRepository.GetByIdAsync.OnCall = (ko, id) =>
    Task.FromResult(results.Dequeue());
```

### Verifying Async Call Order

<!-- snippet: docs:async-methods:call-order -->
```csharp
[KnockOff]
public partial class AsyncCallOrderKnockOff : IAsyncCallOrder { }
```
<!-- /snippet -->

```csharp
var order = new List<string>();

knockOff.IAsyncCallOrder.StartAsync.OnCall = (ko) =>
{
    order.Add("Start");
    return Task.CompletedTask;
};

knockOff.IAsyncCallOrder.ProcessAsync.OnCall = (ko) =>
{
    order.Add("Process");
    return Task.CompletedTask;
};

await service.StartAsync();
await service.ProcessAsync();

Assert.Equal(["Start", "Process"], order);
```

## ValueTask vs Task

`ValueTask` is more efficient for methods that often complete synchronously. KnockOff handles both:

```csharp
// ValueTask<T> - synchronous completion
knockOff.ICache.GetCachedAsync.OnCall = (ko, key) =>
    new ValueTask<string?>(cache.GetOrDefault(key));

// Task<T> - async completion
knockOff.IRemoteService.FetchRemoteAsync.OnCall = (ko, key) =>
    Task.FromResult(remoteData[key]);
```

## Reset

Reset works the same for async methods:

```csharp
await repo.GetByIdAsync(1);
knockOff.IAsyncRepository.GetByIdAsync.Reset();

Assert.Equal(0, knockOff.IAsyncRepository.GetByIdAsync.CallCount);
Assert.Null(knockOff.IAsyncRepository.GetByIdAsync.OnCall);
```
