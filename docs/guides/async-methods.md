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

<!-- snippet: docs:async-methods:default-behavior -->
```csharp
await repo.InitializeAsync();       // Completes immediately
        var user = await repo.GetByIdAsync(1);  // Returns null (default)
        var count = await repo.CountAsync();    // Returns 0 (default)
```
<!-- /snippet -->

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

<!-- snippet: docs:async-methods:task-callbacks -->
```csharp
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
<!-- /snippet -->

### ValueTask Methods

<!-- snippet: docs:async-methods:valuetask-callbacks -->
```csharp
// ValueTask<T>
        knockOff.CountAsync.OnCall = (ko) => new ValueTask<int>(100);
```
<!-- /snippet -->

## Tracking

Async methods use the same tracking as sync methods:

<!-- snippet: docs:async-methods:tracking -->
```csharp
await repo.GetByIdAsync(1);
        await repo.GetByIdAsync(2);
        await repo.GetByIdAsync(3);

        var callCount = knockOff.GetByIdAsync.CallCount;  // 3
        var lastArg = knockOff.GetByIdAsync.LastCallArg;  // 3
```
<!-- /snippet -->

## Common Patterns

### Simulating Delays

<!-- snippet: docs:async-methods:simulating-delays -->
```csharp
knockOff.GetByIdAsync.OnCall = async (ko, id) =>
        {
            await Task.Delay(100);  // Simulate network latency
            return new AsyncUser { Id = id };
        };
```
<!-- /snippet -->

Note: This requires the callback to be `async`.

### Simulating Failures

<!-- snippet: docs:async-methods:simulating-failures -->
```csharp
[KnockOff]
public partial class AsyncSaveKnockOff : IAsyncSave { }
```
<!-- /snippet -->

<!-- snippet: docs:async-methods:simulating-failures-usage -->
```csharp
// Faulted task
        knockOff.SaveAsync.OnCall = (ko, entity) =>
            Task.FromException<int>(new InvalidOperationException("Connection lost"));

        // Or throw directly in callback
        knockOff.SaveAsync.OnCall = (ko, entity) =>
        {
            throw new InvalidOperationException("Connection lost");
        };
```
<!-- /snippet -->

### Conditional Async Behavior

<!-- snippet: docs:async-methods:conditional-behavior -->
```csharp
knockOff.GetByIdAsync.OnCall = (ko, id) =>
        {
            if (id <= 0)
                return Task.FromException<AsyncUser?>(new ArgumentException("Invalid ID"));

            return Task.FromResult<AsyncUser?>(new AsyncUser { Id = id });
        };
```
<!-- /snippet -->

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

<!-- snippet: docs:async-methods:cancellation-usage -->
```csharp
knockOff.FetchAsync.OnCall = (ko, id, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new AsyncData { Id = id });
        };
```
<!-- /snippet -->

### Sequential Async Returns

<!-- snippet: docs:async-methods:sequential-returns -->
```csharp
var results = new Queue<AsyncUser?>([
            new AsyncUser { Name = "First" },
            new AsyncUser { Name = "Second" },
            null  // Then not found
        ]);

        knockOff.GetByIdAsync.OnCall = (ko, id) =>
            Task.FromResult(results.Dequeue());
```
<!-- /snippet -->

### Verifying Async Call Order

<!-- snippet: docs:async-methods:call-order -->
```csharp
[KnockOff]
public partial class AsyncCallOrderKnockOff : IAsyncCallOrder { }
```
<!-- /snippet -->

<!-- snippet: docs:async-methods:call-order-usage -->
```csharp
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
<!-- /snippet -->

## ValueTask vs Task

`ValueTask` is more efficient for methods that often complete synchronously. KnockOff handles both - see the callback examples above.

## Reset

Reset works the same for async methods:

<!-- snippet: docs:async-methods:reset -->
```csharp
await repo.GetByIdAsync(1);
        knockOff.GetByIdAsync.Reset();

        var callCount = knockOff.GetByIdAsync.CallCount;  // 0
        var onCall = knockOff.GetByIdAsync.OnCall;        // null
```
<!-- /snippet -->
