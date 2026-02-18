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

public interface IAsyncFetchSvc
{
    Task<string> FetchAsync(int id);
    Task ExecuteAsync(string command);
    Task<string> GetDataAsync(int id);
}

[KnockOff]
public partial class AsyncFetchSvcStub : IAsyncFetchSvc { }

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
        stub.GetUserAsync.Call((id) => new User { Id = id, Name = "Alice" }).Verifiable();
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
        stub.GetUserAsync.Call((id) =>
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
        stub.UpdateUserAsync.Call((user) => updatedUsers.Add(user)).Verifiable();
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
        // Call() auto-returns Task.CompletedTask for async void methods
        stub.UpdateUserAsync.Call((user) =>
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
        stub.GetCachedUserAsync.Call((id) => new User { Id = id, Name = "Cached" }).Verifiable();

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
        // ValueTask<T> methods use the same Return() API — just return the inner value
        stub.GetCachedUserAsync.Call((id) =>
            new User { Id = id, Name = "Cached" }).Verifiable();
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
        stub.GetUserAsync.Call(async (id) =>
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
        stub.GetUserAsync.Call((id) =>
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
        // Throw directly - cast to sync delegate to disambiguate overloads
        stub.GetUserAsync.Call((Func<int, User?>)((int id) =>
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

// =============================================================================
// Async Reference Samples (for async-methods.md)
// =============================================================================

public class AsyncTierTests
{
    [Fact]
    public async Task Async_Tier1_Value()
    {
        var stub = new AsyncFetchSvcStub();

        #region async-tier1-value
        stub.FetchAsync.Return("value");
        // Internally: Task.FromResult("value")

        IAsyncFetchSvc service = stub;
        var result = await service.FetchAsync(1); // "value"
        #endregion

        Assert.Equal("value", result);
    }

    [Fact]
    public async Task Async_Tier2_SimplifiedCallback()
    {
        var stub = new AsyncFetchSvcStub();

        #region async-tier2-callback
        stub.FetchAsync.Call((id) => $"Fetch-{id}");
        // Internally: Task.FromResult(callback(id))

        IAsyncFetchSvc service = stub;
        var result = await service.FetchAsync(42); // "Fetch-42"
        #endregion

        Assert.Equal("Fetch-42", result);
    }

    [Fact]
    public async Task Async_Tier3_FullCallback()
    {
        var stub = new AsyncFetchSvcStub();

        #region async-tier3-full
        stub.FetchAsync.Call((int id) => Task.FromResult($"Full-{id}"));
        // Used as-is -- for custom async behavior

        IAsyncFetchSvc service = stub;
        var result = await service.FetchAsync(99); // "Full-99"
        #endregion

        Assert.Equal("Full-99", result);
    }

    [Fact]
    public async Task Async_VoidMethod()
    {
        var stub = new AsyncFetchSvcStub();

        #region async-void-method
        stub.ExecuteAsync.Call((command) => { /* side effect */ });

        IAsyncFetchSvc service = stub;
        await service.ExecuteAsync("test"); // Callback invoked
        #endregion
    }

    [Fact]
    public async Task Async_Sequences()
    {
        var stub = new AsyncFetchSvcStub();

        #region async-sequences-autowrap
        stub.GetDataAsync.Return("first", "second", "third");

        IAsyncFetchSvc service = stub;
        var r1 = await service.GetDataAsync(1); // "first"
        var r2 = await service.GetDataAsync(2); // "second"
        var r3 = await service.GetDataAsync(3); // "third"
        var r4 = await service.GetDataAsync(4); // "third" (repeats)
        #endregion

        Assert.Equal("first", r1);
        Assert.Equal("third", r4);
    }

    [Fact]
    public async Task Async_CallbackSequences()
    {
        var stub = new AsyncFetchSvcStub();

        #region async-callback-sequences
        stub.FetchAsync.Call((id) => $"First-{id}")
            .ThenReturn((id) => Task.FromResult($"Second-{id}"))
            .ThenReturn("constant");
        #endregion

        IAsyncFetchSvc service = stub;
        Assert.Equal("First-1", await service.FetchAsync(1));
        Assert.Equal("Second-2", await service.FetchAsync(2));
        Assert.Equal("constant", await service.FetchAsync(3));
    }

    [Fact]
    public async Task Async_WhenChains()
    {
        var stub = new AsyncFetchSvcStub();

        #region async-when-chains
        stub.GetDataAsync.When(1).Return("Item 1");
        stub.GetDataAsync.When(2).Return("Item 2");
        stub.GetDataAsync.When((id) => id > 100).Return("Bulk item");

        IAsyncFetchSvc service = stub;
        var r = await service.GetDataAsync(1); // "Item 1"
        #endregion

        Assert.Equal("Item 1", r);
    }

    [Fact]
    public async Task Async_Verification()
    {
        var stub = new AsyncFetchSvcStub();
        stub.FetchAsync.Return("result");

        IAsyncFetchSvc service = stub;

        #region async-verification
        await service.FetchAsync(1);
        await service.FetchAsync(2);

        stub.FetchAsync.Verify(Called.Exactly(2));
        Assert.Equal(2, stub.FetchAsync.LastArg); // last argument
        #endregion
    }
}

// =============================================================================
// Async Delegate Samples (for async-methods.md)
// =============================================================================

public delegate Task<int> AsyncOp(int x);

[KnockOff<AsyncOp>]
public partial class AsyncDelegateHost { }

public partial class AsyncDelegateHost
{
    [Fact]
    public async Task Async_Delegate_ThreeTier()
    {
        #region async-delegate-tiers
        var stub = new Stubs.AsyncOp();

        // Tier 1: auto-wraps int -> Task<int>
        stub.Interceptor.Return(42);

        AsyncOp op = stub;
        var result = await op(10); // 42
        #endregion

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task Async_Delegate_Sequences()
    {
        #region async-delegate-sequences
        var stub = new Stubs.AsyncOp();
        stub.Interceptor.Return(10, 20);

        AsyncOp op = stub;
        var r1 = await op(0); // 10
        var r2 = await op(0); // 20
        var r3 = await op(0); // 20 (repeats)
        #endregion

        Assert.Equal(10, r1);
        Assert.Equal(20, r2);
        Assert.Equal(20, r3);
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
        stub.FindAsync.Call((id) =>
            Task.FromResult<User?>(new User { Id = id, Name = "Original" })).Verifiable();
        stub.SaveAsync.Call((user) => { }).Verifiable();
        #endregion

        var manager = new UserManager(stub);

        var result = await manager.UpdateUserNameAsync(42, "Updated");

        Assert.True(result);
        stub.Verify();
    }
}
