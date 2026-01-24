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
    #region async-task-value-overload
    [Fact]
    public async Task TaskResult_ValueOverload_AutoWraps()
    {
        var stub = new AsyncUserSvcStub();

        // VALUE OVERLOAD: KnockOff auto-wraps the value in Task.FromResult
        // This is the simplest syntax for returning async values
        stub.GetUserAsync.OnCall(new User { Id = 42, Name = "Alice" });

        IAsyncUserSvc service = stub;
        var user = await service.GetUserAsync(42);

        Assert.NotNull(user);
        Assert.Equal("Alice", user.Name);
    }
    #endregion

    #region async-task-result
    [Fact]
    public async Task TaskResult_ReturnedWithFromResult()
    {
        var stub = new AsyncUserSvcStub();

        // CALLBACK: Use Task.FromResult when you need dynamic logic
        stub.GetUserAsync.OnCall((id) =>
            Task.FromResult<User?>(new User { Id = id, Name = "Alice" })).Verifiable();

        IAsyncUserSvc service = stub;
        var user = await service.GetUserAsync(42);

        Assert.NotNull(user);
        Assert.Equal("Alice", user.Name);
        stub.Verify();
    }
    #endregion

    #region async-task-void
    [Fact]
    public async Task TaskVoid_ReturnedWithCompletedTask()
    {
        var stub = new AsyncUserSvcStub();

        var updatedUsers = new List<User>();

        // Use Task.CompletedTask for async void methods
        stub.UpdateUserAsync.OnCall((user) =>
        {
            updatedUsers.Add(user);
            return Task.CompletedTask;
        }).Verifiable();

        IAsyncUserSvc service = stub;
        await service.UpdateUserAsync(new User { Id = 1, Name = "Bob" });

        Assert.Single(updatedUsers);
        stub.Verify();
    }
    #endregion
}

// =============================================================================
// ValueTask<T> Methods
// =============================================================================

public class ValueTaskMethodTests
{
    #region async-valuetask
    [Fact]
    public async Task ValueTaskResult_ReturnedDirectly()
    {
        var stub = new AsyncUserSvcStub();

        // Create ValueTask directly with the value
        stub.GetCachedUserAsync.OnCall((id) =>
            new ValueTask<User?>(new User { Id = id, Name = "Cached" })).Verifiable();

        IAsyncUserSvc service = stub;
        var user = await service.GetCachedUserAsync(42);

        Assert.NotNull(user);
        Assert.Equal("Cached", user.Name);
        stub.Verify();
    }
    #endregion
}

// =============================================================================
// Simulating Delays
// =============================================================================

public class AsyncDelayTests
{
    #region async-delay
    [Fact]
    public async Task AsyncDelay_SimulatesLatency()
    {
        var stub = new AsyncUserSvcStub();

        // Use async lambda to simulate delay
        var tracking = stub.GetUserAsync.OnCall(async (id) =>
        {
            await Task.Delay(50); // Simulate network latency
            return new User { Id = id, Name = "Delayed" };
        });

        IAsyncUserSvc service = stub;

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var user = await service.GetUserAsync(1);
        sw.Stop();

        Assert.NotNull(user);
        Assert.True(sw.ElapsedMilliseconds >= 40); // Account for timing variance
    }
    #endregion
}

// =============================================================================
// Simulating Failures
// =============================================================================

public class AsyncExceptionTests
{
    #region async-exception
    [Fact]
    public async Task TaskFromException_ReturnsFaultedTask()
    {
        var stub = new AsyncUserSvcStub();

        // Return a faulted task using Task.FromException
        var tracking = stub.GetUserAsync.OnCall((id) =>
            Task.FromException<User?>(new NotFoundException($"User {id} not found")));

        IAsyncUserSvc service = stub;

        // Awaiting throws the configured exception
        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetUserAsync(999));
    }
    #endregion

    #region async-throw
    [Fact]
    public async Task ThrowDirectly_InOnCallCallback()
    {
        var stub = new AsyncUserSvcStub();

        // Throw exception directly in the callback
        var tracking = stub.GetUserAsync.OnCall((id) =>
        {
            throw new NotFoundException($"User {id} not found");
        });

        IAsyncUserSvc service = stub;

        // Exception is thrown when the method is called
        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetUserAsync(999));
    }
    #endregion
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
    #region async-complete-example
    [Fact]
    public async Task AsyncService_SuccessScenario()
    {
        var stub = new AsyncRepoStub();

        // Configure success case with Verifiable markers
        stub.FindAsync.OnCall((id) =>
            Task.FromResult<User?>(new User { Id = id, Name = "Original" })).Verifiable();

        stub.SaveAsync.OnCall((user) => Task.CompletedTask).Verifiable();

        var manager = new UserManager(stub);

        // Act
        var result = await manager.UpdateUserNameAsync(42, "Updated");

        // Assert
        Assert.True(result);
        stub.Verify(); // Verifies both FindAsync and SaveAsync were called
    }
    #endregion
}
