using KnockOff;

namespace KnockOff.Documentation.Samples.InterceptorApi;

// =============================================================================
// Interfaces for Interceptor API Samples
// =============================================================================

public interface IApiMethodRepo
{
    User? GetById(int id);
    void Save(User user);
    void Update(int id, string name);
}

public interface IApiPropertyRepo
{
    string ConnectionString { get; set; }
    bool IsConnected { get; }
    int Timeout { get; set; }
}

public interface IApiIndexerRepo
{
    User? this[int id] { get; set; }
}

public interface IApiEventRepo
{
    event EventHandler? Changed;
    event Action<User>? UserAdded;
}

public interface IApiGenericRepo
{
    T? GetById<T>(int id) where T : class;
    void Save<T>(T item) where T : class;
}

// =============================================================================
// Stubs for Interceptor API Samples
// =============================================================================

[KnockOff]
public partial class ApiMethodRepoStub : IApiMethodRepo { }

[KnockOff]
public partial class ApiPropertyRepoStub : IApiPropertyRepo { }

[KnockOff]
public partial class ApiIndexerRepoStub : IApiIndexerRepo { }

[KnockOff]
public partial class ApiEventRepoStub : IApiEventRepo { }

[KnockOff]
public partial class ApiGenericRepoStub : IApiGenericRepo { }

// =============================================================================
// Method Interceptor API
// =============================================================================

public class MethodInterceptorApiTests
{
    #region api-method-interceptor
    [Fact]
    public void MethodInterceptor_CompleteApiDemonstration()
    {
        var stub = new ApiMethodRepoStub();

        // Configure void method with OnCall and mark verifiable
        var saveTracking = stub.Save.OnCall((user) => { }).Verifiable();

        // Configure return method with OnCall and mark verifiable
        var getTracking = stub.GetById.OnCall((id) =>
            new User { Id = id, Name = $"User{id}" }).Verifiable();

        // Configure multi-parameter method and mark verifiable
        var updateTracking = stub.Update.OnCall((id, name) => { }).Verifiable();

        IApiMethodRepo repository = stub;

        // Exercise the stub
        repository.Save(new User { Id = 1, Name = "Alice" });
        var user = repository.GetById(42);
        repository.Update(1, "UpdatedName");

        // Verify all methods were called
        stub.Verify();

        // Access last argument (single parameter) via tracking
        Assert.Equal(42, getTracking.LastArg);

        // Access last arguments (multi parameter via tracking)
        var (id, name) = updateTracking.LastArgs;
        Assert.Equal(1, id);
        Assert.Equal("UpdatedName", name);

        // Verify return value was computed by callback
        Assert.NotNull(user);
        Assert.Equal("User42", user.Name);
    }
    #endregion
}

// =============================================================================
// Property Interceptor API
// =============================================================================

public class PropertyInterceptorApiTests
{
    #region api-property-interceptor
    [Fact]
    public void PropertyInterceptor_CompleteApiDemonstration()
    {
        var stub = new ApiPropertyRepoStub();

        // Set Value directly - returned by getter
        stub.ConnectionString.Value = "Server=localhost";

        IApiPropertyRepo repository = stub;

        // Read property - uses Value
        var conn = repository.ConnectionString;
        Assert.Equal("Server=localhost", conn);

        // Verify property was read
        stub.ConnectionString.VerifyGet(Times.Once);

        // Write property
        repository.ConnectionString = "Server=production";

        // Verify property was written
        stub.ConnectionString.VerifySet(Times.Once);

        // LastSetValue captures what was written
        Assert.Equal("Server=production", stub.ConnectionString.LastSetValue);

        // OnGet replaces Value with callback return
        // Note: OnGet takes the stub as first parameter (ko)
        stub.Timeout.OnGet = () => 30;
        var timeout = repository.Timeout;
        Assert.Equal(30, timeout);

        // OnSet provides custom setter behavior
        // Note: OnSet takes (value) - does NOT automatically update Value
        var setWasCalled = false;
        stub.Timeout.OnSet = (val) =>
        {
            setWasCalled = true;
            // Manually update Value if needed:
            // stub.Timeout.Value = val;
        };
        repository.Timeout = 60;
        Assert.True(setWasCalled);
        // Value is NOT updated because OnSet didn't do it
        // (OnGet returns 30 from callback, not Value)
    }
    #endregion
}

// =============================================================================
// Indexer Interceptor API
// =============================================================================

public class IndexerInterceptorApiTests
{
    #region api-indexer-interceptor
    [Fact]
    public void IndexerInterceptor_CompleteApiDemonstration()
    {
        var stub = new ApiIndexerRepoStub();

        // Populate Backing dictionary directly
        stub.Indexer.Backing[1] = new User { Id = 1, Name = "Alice" };
        stub.Indexer.Backing[2] = new User { Id = 2, Name = "Bob" };

        IApiIndexerRepo repository = stub;

        // Read from indexer - uses Backing by default
        var user1 = repository[1];
        Assert.NotNull(user1);
        Assert.Equal("Alice", user1.Name);

        // Verify indexer was read
        stub.Indexer.VerifyGet(Times.Once);

        // LastGetKey captures the key used
        Assert.Equal(1, stub.Indexer.LastGetKey);

        // Write to indexer - updates Backing by default
        repository[3] = new User { Id = 3, Name = "Charlie" };

        // Verify indexer was written
        stub.Indexer.VerifySet(Times.Once);

        // LastSetEntry captures key and value (nullable tuple)
        var lastEntry = stub.Indexer.LastSetEntry;
        Assert.NotNull(lastEntry);
        Assert.Equal(3, lastEntry.Value.Key);
        Assert.Equal("Charlie", lastEntry.Value.Value?.Name);

        // OnGet overrides Backing lookup
        // Note: OnGet takes (key)
        stub.Indexer.OnGet = (k) => new User { Id = k, Name = "FromCallback" };
        var fromCallback = repository[999];
        Assert.Equal("FromCallback", fromCallback?.Name);

        // OnSet overrides Backing storage
        // Note: OnSet takes (key, value) - does NOT automatically update Backing
        var onSetCalled = false;
        stub.Indexer.OnSet = (k, v) =>
        {
            onSetCalled = true;
            // Manually update Backing if needed:
            // stub.Indexer.Backing[k] = v;
        };
        repository[4] = new User { Id = 4, Name = "Dave" };
        Assert.True(onSetCalled);
        // Backing[4] is NOT set because OnSet didn't do it
        Assert.False(stub.Indexer.Backing.ContainsKey(4));
    }
    #endregion
}

// =============================================================================
// Event Interceptor API
// =============================================================================

public class EventInterceptorApiTests
{
    #region api-event-interceptor
    [Fact]
    public void EventInterceptor_CompleteApiDemonstration()
    {
        var stub = new ApiEventRepoStub();
        IApiEventRepo repository = stub;

        // Subscribe to EventHandler event
        var changedInvoked = false;
        repository.Changed += (sender, e) => changedInvoked = true;

        // Verify subscription occurred
        stub.Changed.VerifyAdd(Times.Once);

        // HasSubscribers indicates active subscriptions
        Assert.True(stub.Changed.HasSubscribers);

        // Raise fires all subscribers
        stub.Changed.Raise(repository, EventArgs.Empty);
        Assert.True(changedInvoked);

        // Unsubscribe
        EventHandler handler = (sender, e) => { };
        repository.Changed += handler;
        stub.Changed.VerifyAdd(Times.Exactly(2));

        repository.Changed -= handler;
        stub.Changed.VerifyRemove(Times.Once);

        // Action<T> events work similarly
        User? addedUser = null;
        repository.UserAdded += user => addedUser = user;

        // Raise with typed argument
        stub.UserAdded.Raise(new User { Id = 1, Name = "Alice" });
        Assert.NotNull(addedUser);
        Assert.Equal("Alice", addedUser.Name);
    }
    #endregion
}

// =============================================================================
// Generic Method Interceptor API
// =============================================================================

public class GenericMethodInterceptorApiTests
{
    #region api-generic-interceptor
    [Fact]
    public void GenericMethodInterceptor_CompleteApiDemonstration()
    {
        var stub = new ApiGenericRepoStub();

        // Configure OnCall for specific type arguments
        var userTracking = stub.GetById.Of<User>().OnCall((id) =>
            new User { Id = id, Name = $"User{id}" });

        var productTracking = stub.GetById.Of<Product>().OnCall((id) =>
            new Product { Id = id, Name = $"Product{id}" });

        IApiGenericRepo repository = stub;

        // Call with different type arguments
        var user = repository.GetById<User>(1);
        var product = repository.GetById<Product>(2);
        var user2 = repository.GetById<User>(3);

        // Verify calls via tracking with Times
        userTracking.Verify(Times.Exactly(2));
        productTracking.Verify(Times.Once);

        // CalledTypeArguments lists all types used
        Assert.Contains(typeof(User), stub.GetById.CalledTypeArguments);
        Assert.Contains(typeof(Product), stub.GetById.CalledTypeArguments);

        // LastCallArg for specific type (captures the 'id' parameter)
        Assert.Equal(3, stub.GetById.Of<User>().LastCallArg);
        Assert.Equal(2, stub.GetById.Of<Product>().LastCallArg);

        // Reset typed tracking only
        stub.GetById.Of<User>().Reset();
        stub.GetById.Of<User>().Verify(Times.Never);
        productTracking.Verify(Times.Once); // Preserved

        // Reset all tracking
        stub.GetById.Reset();
        stub.GetById.Of<User>().Verify(Times.Never);
        stub.GetById.Of<Product>().Verify(Times.Never);
    }
    #endregion
}

// Support class for generic samples
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}
