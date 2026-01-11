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

<!-- snippet: async-methods-basic-interface -->
```cs
public interface IAsyncRepository
{
    Task InitializeAsync();
    Task<AsyncUser?> GetByIdAsync(int id);
    ValueTask<int> CountAsync();
}

[KnockOff]
public partial class AsyncRepositoryKnockOff : IAsyncRepository { }
```
<!-- endSnippet -->

### Default Behavior

Without callbacks or user methods, async methods return completed tasks:

<!-- snippet: async-methods-default-behavior -->
```cs
await repo.InitializeAsync();       // Completes immediately
var user = await repo.GetByIdAsync(1);  // Returns null (default)
var count = await repo.CountAsync();    // Returns 0 (default)
```
<!-- endSnippet -->

## User-Defined Methods

Define protected async methods for default behavior:

<!-- snippet: async-methods-user-defined -->
```cs
[KnockOff]
public partial class AsyncUserDefinedKnockOff : IAsyncUserDefined
{
    protected Task<AsyncUser?> GetByIdAsync(int id) =>
        Task.FromResult<AsyncUser?>(new AsyncUser { Id = id, Name = "Default" });

    protected ValueTask<int> CountAsync() =>
        new ValueTask<int>(42);
}
```
<!-- endSnippet -->

## Callbacks

### Task Methods

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

### ValueTask Methods

<!-- snippet: async-methods-valuetask-callbacks -->
```cs
// ValueTask<T>
knockOff.CountAsync.OnCall = (ko) => new ValueTask<int>(100);
```
<!-- endSnippet -->

## Tracking

Async methods use the same tracking as sync methods:

<!-- snippet: async-methods-tracking -->
```cs
await repo.GetByIdAsync(1);
await repo.GetByIdAsync(2);
await repo.GetByIdAsync(3);

var callCount = knockOff.GetByIdAsync.CallCount;  // 3
var lastArg = knockOff.GetByIdAsync.LastCallArg;  // 3
```
<!-- endSnippet -->

## Common Patterns

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

Note: This requires the callback to be `async`.

### Simulating Failures

<!-- snippet: async-methods-simulating-failures -->
```cs
[KnockOff]
public partial class AsyncSaveKnockOff : IAsyncSave { }
```
<!-- endSnippet -->

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

### Conditional Async Behavior

<!-- snippet: async-methods-conditional-behavior -->
```cs
knockOff.GetByIdAsync.OnCall = (ko, id) =>
{
    if (id <= 0)
        return Task.FromException<AsyncUser?>(new ArgumentException("Invalid ID"));

    return Task.FromResult<AsyncUser?>(new AsyncUser { Id = id });
};
```
<!-- endSnippet -->

### Cancellation Support

For methods with `CancellationToken`:

<!-- snippet: async-methods-cancellation -->
```cs
public interface IAsyncFetch
{
    Task<AsyncData> FetchAsync(int id, CancellationToken ct);
}

[KnockOff]
public partial class AsyncFetchKnockOff : IAsyncFetch { }
```
<!-- endSnippet -->

<!-- snippet: async-methods-cancellation-usage -->
```cs
knockOff.FetchAsync.OnCall = (ko, id, ct) =>
{
    ct.ThrowIfCancellationRequested();
    return Task.FromResult(new AsyncData { Id = id });
};
```
<!-- endSnippet -->

### Sequential Async Returns

<!-- snippet: async-methods-sequential-returns -->
```cs
var results = new Queue<AsyncUser?>([
    new AsyncUser { Name = "First" },
    new AsyncUser { Name = "Second" },
    null  // Then not found
]);

knockOff.GetByIdAsync.OnCall = (ko, id) =>
    Task.FromResult(results.Dequeue());
```
<!-- endSnippet -->

### Verifying Async Call Order

<!-- snippet: async-methods-call-order -->
```cs
[KnockOff]
public partial class AsyncCallOrderKnockOff : IAsyncCallOrder { }
```
<!-- endSnippet -->

<!-- snippet: async-methods-call-order-usage -->
```cs
var order = new List<string>();

knockOff.StartAsync.OnCall = (ko) =>
{
    order.Add("Start");
    return Task.CompletedTask;
};

knockOff.ProcessAsync.OnCall = (ko) =>
{
    order.Add("Process");
    return Task.CompletedTask;
};

await service.StartAsync();
await service.ProcessAsync();

// order is ["Start", "Process"]
```
<!-- endSnippet -->

## ValueTask vs Task

`ValueTask` is more efficient for methods that often complete synchronously. KnockOff handles both - see the callback examples above.

## Reset

Reset works the same for async methods:

<!-- snippet: async-methods-reset -->
```cs
await repo.GetByIdAsync(1);
knockOff.GetByIdAsync.Reset();

var callCount = knockOff.GetByIdAsync.CallCount;  // 0
var onCall = knockOff.GetByIdAsync.OnCall;        // null
```
<!-- endSnippet -->
