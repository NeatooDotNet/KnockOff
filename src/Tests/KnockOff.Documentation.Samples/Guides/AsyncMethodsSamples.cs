/// <summary>
/// Code samples for docs/guides/async-methods.md
///
/// Snippets in this file:
/// - docs:async-methods:basic-interface
/// - docs:async-methods:user-defined
/// - docs:async-methods:task-callbacks
/// - docs:async-methods:valuetask-callbacks
/// - docs:async-methods:tracking
/// - docs:async-methods:simulating-delays
/// - docs:async-methods:simulating-failures
/// - docs:async-methods:conditional-behavior
/// - docs:async-methods:cancellation
/// - docs:async-methods:sequential-returns
/// - docs:async-methods:call-order
///
/// Corresponding tests: AsyncMethodsSamplesTests.cs
/// </summary>

namespace KnockOff.Documentation.Samples.Guides;

// ============================================================================
// Domain Types
// ============================================================================

public class AsyncUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class AsyncData
{
    public int Id { get; set; }
}

// ============================================================================
// Basic Usage
// ============================================================================

#region async-methods-basic-interface
public interface IAsyncRepository
{
    Task InitializeAsync();
    Task<AsyncUser?> GetByIdAsync(int id);
    ValueTask<int> CountAsync();
}

[KnockOff]
public partial class AsyncRepositoryKnockOff : IAsyncRepository { }
#endregion

// ============================================================================
// User-Defined Methods
// ============================================================================

public interface IAsyncUserDefined
{
    Task<AsyncUser?> GetByIdAsync(int id);
    ValueTask<int> CountAsync();
}

#region async-methods-user-defined
[KnockOff]
public partial class AsyncUserDefinedKnockOff : IAsyncUserDefined
{
    protected Task<AsyncUser?> GetByIdAsync(int id) =>
        Task.FromResult<AsyncUser?>(new AsyncUser { Id = id, Name = "Default" });

    protected ValueTask<int> CountAsync() =>
        new ValueTask<int>(42);
}
#endregion

// ============================================================================
// Cancellation Support
// ============================================================================

#region async-methods-cancellation
public interface IAsyncFetch
{
    Task<AsyncData> FetchAsync(int id, CancellationToken ct);
}

[KnockOff]
public partial class AsyncFetchKnockOff : IAsyncFetch { }
#endregion

// ============================================================================
// Async Failure Service
// ============================================================================

public interface IAsyncSave
{
    Task<int> SaveAsync(AsyncData entity);
}

#region async-methods-simulating-failures
[KnockOff]
public partial class AsyncSaveKnockOff : IAsyncSave { }
#endregion

// ============================================================================
// Call Order Service
// ============================================================================

public interface IAsyncCallOrder
{
    Task StartAsync();
    Task ProcessAsync();
}

#region async-methods-call-order
[KnockOff]
public partial class AsyncCallOrderKnockOff : IAsyncCallOrder { }
#endregion

// ============================================================================
// Usage Examples
// ============================================================================

/// <summary>
/// Usage examples demonstrating async method patterns.
/// Each method is compilable; snippets extract key portions.
/// </summary>
public static class AsyncMethodsUsageExamples
{
    public static async Task DefaultBehavior()
    {
        var knockOff = new AsyncRepositoryKnockOff();
        IAsyncRepository repo = knockOff;

        #region async-methods-default-behavior
        await repo.InitializeAsync();       // Completes immediately
        var user = await repo.GetByIdAsync(1);  // Returns null (default)
        var count = await repo.CountAsync();    // Returns 0 (default)
        #endregion

        _ = (user, count);
    }

    public static void TaskCallbacks()
    {
        var knockOff = new AsyncRepositoryKnockOff();

        #region async-methods-task-callbacks
        // Task (void equivalent)
        knockOff.InitializeAsync.OnCall = (ko) =>
        {
            // Custom logic
            return Task.CompletedTask;
        };

        // Task<T>
        knockOff.GetByIdAsync.OnCall = (ko, id) =>
            Task.FromResult<AsyncUser?>(new AsyncUser { Id = id, Name = "Mocked" });
        #endregion
    }

    public static void ValueTaskCallbacks()
    {
        var knockOff = new AsyncRepositoryKnockOff();

        #region async-methods-valuetask-callbacks
        // ValueTask<T>
        knockOff.CountAsync.OnCall = (ko) => new ValueTask<int>(100);
        #endregion
    }

    public static async Task Tracking()
    {
        var knockOff = new AsyncRepositoryKnockOff();
        IAsyncRepository repo = knockOff;

        #region async-methods-tracking
        await repo.GetByIdAsync(1);
        await repo.GetByIdAsync(2);
        await repo.GetByIdAsync(3);

        var callCount = knockOff.GetByIdAsync.CallCount;  // 3
        var lastArg = knockOff.GetByIdAsync.LastCallArg;  // 3
        #endregion

        _ = (callCount, lastArg);
    }

    public static void SimulatingDelays()
    {
        var knockOff = new AsyncRepositoryKnockOff();

        #region async-methods-simulating-delays
        knockOff.GetByIdAsync.OnCall = async (ko, id) =>
        {
            await Task.Delay(100);  // Simulate network latency
            return new AsyncUser { Id = id };
        };
        #endregion
    }

    public static void SimulatingFailuresUsage()
    {
        var knockOff = new AsyncSaveKnockOff();

        #region async-methods-simulating-failures-usage
        // Faulted task
        knockOff.SaveAsync.OnCall = (ko, entity) =>
            Task.FromException<int>(new InvalidOperationException("Connection lost"));

        // Or throw directly in callback
        knockOff.SaveAsync.OnCall = (ko, entity) =>
        {
            throw new InvalidOperationException("Connection lost");
        };
        #endregion
    }

    public static void ConditionalBehavior()
    {
        var knockOff = new AsyncRepositoryKnockOff();

        #region async-methods-conditional-behavior
        knockOff.GetByIdAsync.OnCall = (ko, id) =>
        {
            if (id <= 0)
                return Task.FromException<AsyncUser?>(new ArgumentException("Invalid ID"));

            return Task.FromResult<AsyncUser?>(new AsyncUser { Id = id });
        };
        #endregion
    }

    public static void CancellationUsage()
    {
        var knockOff = new AsyncFetchKnockOff();

        #region async-methods-cancellation-usage
        knockOff.FetchAsync.OnCall = (ko, id, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new AsyncData { Id = id });
        };
        #endregion
    }

    public static void SequentialReturns()
    {
        var knockOff = new AsyncRepositoryKnockOff();

        #region async-methods-sequential-returns
        var results = new Queue<AsyncUser?>([
            new AsyncUser { Name = "First" },
            new AsyncUser { Name = "Second" },
            null  // Then not found
        ]);

        knockOff.GetByIdAsync.OnCall = (ko, id) =>
            Task.FromResult(results.Dequeue());
        #endregion
    }

    public static async Task CallOrderUsage()
    {
        var knockOff = new AsyncCallOrderKnockOff();
        IAsyncCallOrder service = knockOff;

        #region async-methods-call-order-usage
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
        #endregion

        _ = order;
    }

    public static async Task ResetUsage()
    {
        var knockOff = new AsyncRepositoryKnockOff();
        IAsyncRepository repo = knockOff;

        #region async-methods-reset
        await repo.GetByIdAsync(1);
        knockOff.GetByIdAsync.Reset();

        var callCount = knockOff.GetByIdAsync.CallCount;  // 0
        var onCall = knockOff.GetByIdAsync.OnCall;        // null
        #endregion

        _ = (callCount, onCall);
    }
}
