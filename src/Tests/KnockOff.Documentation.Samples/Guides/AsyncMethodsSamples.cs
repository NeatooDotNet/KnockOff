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

#region docs:async-methods:basic-interface
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

#region docs:async-methods:user-defined
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

#region docs:async-methods:cancellation
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

#region docs:async-methods:simulating-failures
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

#region docs:async-methods:call-order
[KnockOff]
public partial class AsyncCallOrderKnockOff : IAsyncCallOrder { }
#endregion
