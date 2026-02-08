using KnockOff;

namespace KnockOff.Documentation.Samples.Async;

// =============================================================================
// Interfaces for Async Samples
// =============================================================================

public interface IAsyncUserSvc
{
    Task<User?> GetUserAsync(int id);
    Task UpdateUserAsync(User user);
    ValueTask<User?> GetCachedUserAsync(int id);
}

public interface IAsyncRepo
{
    Task<User?> FindAsync(int id);
    Task SaveAsync(User user);
}

// =============================================================================
// Stubs for Async Samples
// =============================================================================

[KnockOff]
public partial class AsyncUserSvcStub : IAsyncUserSvc { }

[KnockOff]
public partial class AsyncRepoStub : IAsyncRepo { }

// =============================================================================
// Task<T> Methods
// =============================================================================

public class TaskMethodTests
{
    [Fact]
    public async Task TaskResult_ValueOverload_AutoWraps()
    {
        var stub = new AsyncUserSvcStub();

        #region async-task-value-overload
        // KnockOff auto-wraps the value in Task.FromResult
        stub.GetUserAsync.Return(new User { Id = 42, Name = "Alice" });
        #endregion

        IAsyncUserSvc service = stub;
        var user = await service.GetUserAsync(42);

        Assert.NotNull(user);
        Assert.Equal("Alice", user.Name);
    }

    [Fact]
    public async Task TaskResult_SimplifiedCallback_AutoWraps()
    {
        var stub = new AsyncUserSvcStub();

        #region async-task-simplified-callback
        // Return() with unwrapped return type - auto-wrapped in Task.FromResult
        stub.GetUserAsync.Return((id) => new User { Id = id, Name = "Alice" }).Verifiable();
        #endregion

        IAsyncUserSvc service = stub;
        var user = await service.GetUserAsync(42);

        Assert.NotNull(user);
        Assert.Equal("Alice", user.Name);
        stub.Verify();
    }

    [Fact]
    public async Task TaskResult_ReturnedWithFromResult()
    {
        var stub = new AsyncUserSvcStub();

        #region async-task-result
        // Use Task.FromResult when you need parameter-based return values
        stub.GetUserAsync.Return((id) =>
            Task.FromResult<User?>(new User { Id = id, Name = "Alice" })).Verifiable();
        #endregion

        IAsyncUserSvc service = stub;
        var user = await service.GetUserAsync(42);

        Assert.NotNull(user);
        Assert.Equal("Alice", user.Name);
        stub.Verify();
    }

    [Fact]
    public async Task TaskVoid_SimplifiedCallback_AutoReturnsCompletedTask()
    {
        var stub = new AsyncUserSvcStub();
        var updatedUsers = new List<User>();

        #region async-task-simplified-void
        // Action callback for void async - Task.CompletedTask auto-returned
        stub.UpdateUserAsync.Return((user) => updatedUsers.Add(user)).Verifiable();
        #endregion

        IAsyncUserSvc service = stub;
        await service.UpdateUserAsync(new User { Id = 1, Name = "Bob" });

        Assert.Single(updatedUsers);
        stub.Verify();
    }

    [Fact]
    public async Task TaskVoid_ReturnedWithCompletedTask()
    {
        var stub = new AsyncUserSvcStub();
        var updatedUsers = new List<User>();

        #region async-task-void
        // Return() auto-returns Task.CompletedTask for async void methods
        stub.UpdateUserAsync.Return((user) =>
        {
            updatedUsers.Add(user);
        }).Verifiable();
        #endregion

        IAsyncUserSvc service = stub;
        await service.UpdateUserAsync(new User { Id = 1, Name = "Bob" });

        Assert.Single(updatedUsers);
        stub.Verify();
    }
}

// =============================================================================
// ValueTask<T> Methods
// =============================================================================

public class ValueTaskMethodTests
{
    [Fact]
    public async Task ValueTaskResult_ValueOverload_AutoWraps()
    {
        var stub = new AsyncUserSvcStub();

        #region async-valuetask-value-overload
        // KnockOff auto-wraps the value in new ValueTask<T>(value)
        stub.GetCachedUserAsync.Return(new User { Id = 42, Name = "Cached" });
        #endregion

        IAsyncUserSvc service = stub;
        var user = await service.GetCachedUserAsync(42);

        Assert.NotNull(user);
        Assert.Equal("Cached", user.Name);
    }

    [Fact]
    public async Task ValueTaskResult_SimplifiedCallback_AutoWraps()
    {
        var stub = new AsyncUserSvcStub();

        // Return unwrapped type - auto-wrapped in new ValueTask<T>()
        stub.GetCachedUserAsync.Return((id) => new User { Id = id, Name = "Cached" }).Verifiable();

        IAsyncUserSvc service = stub;
        var user = await service.GetCachedUserAsync(42);

        Assert.NotNull(user);
        Assert.Equal("Cached", user.Name);
        stub.Verify();
    }

    [Fact]
    public async Task ValueTaskResult_ReturnedDirectly()
    {
        var stub = new AsyncUserSvcStub();

        #region async-valuetask
        // Create ValueTask directly when you need parameter-based return values
        stub.GetCachedUserAsync.Return((id) =>
            new ValueTask<User?>(new User { Id = id, Name = "Cached" })).Verifiable();
        #endregion

        IAsyncUserSvc service = stub;
        var user = await service.GetCachedUserAsync(42);

        Assert.NotNull(user);
        Assert.Equal("Cached", user.Name);
        stub.Verify();
    }
}

// =============================================================================
// Simulating Delays
// =============================================================================

public class AsyncDelayTests
{
    [Fact]
    public async Task AsyncDelay_SimulatesLatency()
    {
        var stub = new AsyncUserSvcStub();

        #region async-delay
        // Use async lambda to simulate network latency
        stub.GetUserAsync.Return(async (id) =>
        {
            await Task.Delay(50);
            return new User { Id = id, Name = "Delayed" };
        });
        #endregion

        IAsyncUserSvc service = stub;

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var user = await service.GetUserAsync(1);
        sw.Stop();

        Assert.NotNull(user);
        Assert.True(sw.ElapsedMilliseconds >= 40); // Account for timing variance
    }
}

// =============================================================================
// Simulating Failures
// =============================================================================

public class AsyncExceptionTests
{
    [Fact]
    public async Task TaskFromException_ReturnsFaultedTask()
    {
        var stub = new AsyncUserSvcStub();

        #region async-exception
        // Return a faulted task using Task.FromException
        stub.GetUserAsync.Return((id) =>
            Task.FromException<User?>(new NotFoundException($"User {id} not found")));
        #endregion

        IAsyncUserSvc service = stub;

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetUserAsync(999));
    }

    [Fact]
    public async Task ThrowDirectly_InReturnCallback()
    {
        var stub = new AsyncUserSvcStub();

        #region async-throw
        // Throw directly - use explicit delegate type to disambiguate overloads
        stub.GetUserAsync.Return((AsyncUserSvcStub.GetUserAsyncInterceptor.GetUserAsyncDelegate)(id =>
            throw new NotFoundException($"User {id} not found")));
        #endregion

        IAsyncUserSvc service = stub;

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetUserAsync(999));
    }
}

// =============================================================================
// Complete Example
// =============================================================================

// System under test
public class UserManager
{
    private readonly IAsyncRepo _repository;

    public UserManager(IAsyncRepo repository)
    {
        _repository = repository;
    }

    public async Task<bool> UpdateUserNameAsync(int userId, string newName)
    {
        var user = await _repository.FindAsync(userId);
        if (user == null) return false;

        user.Name = newName;
        await _repository.SaveAsync(user);
        return true;
    }
}

public class CompleteAsyncExampleTests
{
    [Fact]
    public async Task AsyncService_SuccessScenario()
    {
        var stub = new AsyncRepoStub();

        #region async-complete-example
        // Configure multiple async methods with verification
        stub.FindAsync.Return((id) =>
            Task.FromResult<User?>(new User { Id = id, Name = "Original" })).Verifiable();
        stub.SaveAsync.Return((user) => { }).Verifiable();
        #endregion

        var manager = new UserManager(stub);

        var result = await manager.UpdateUserNameAsync(42, "Updated");

        Assert.True(result);
        stub.Verify();
    }
}
