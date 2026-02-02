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

// Interface for overview example with all five member types
public interface IApiOverviewRepo
{
    // Method
    void Save(object item);

    // Generic method
    T? GetById<T>(int id) where T : class;

    // Property
    string Name { get; set; }

    // Indexer
    object? this[string key] { get; set; }

    // Event
    event EventHandler? Changed;
}

// Interface for Times and batch verification examples
public interface IApiUserRepo
{
    User? GetById(int id);
    void Save(User user);
    void Delete(int id);
}

// Class for Inline Class pattern demo
public class ApiServiceClass
{
    public virtual User? GetUser(int id) => null;
    public virtual void SaveUser(User user) { }
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

[KnockOff]
public partial class ApiOverviewRepoStub : IApiOverviewRepo { }

[KnockOff]
public partial class ApiUserRepoStub : IApiUserRepo { }

// =============================================================================
// Overview Quick Example - All Five Interceptor Types
// =============================================================================

public class OverviewQuickExampleTests
{
    [Fact]
    public void InterceptorOverview_AllFiveTypes()
    {
        var stub = new ApiOverviewRepoStub();
        IApiOverviewRepo repo = stub;

        #region interceptor-overview-quick-example
        // Method interceptor
        stub.Save.OnCall((item) => { }).Verifiable();

        // Generic method interceptor
        stub.GetById.Of<User>().OnCall((id) => new User { Id = id });

        // Property interceptor
        stub.Name.OnGet("TestRepo");

        // Indexer interceptor
        stub.Indexer.Backing["key1"] = new User { Id = 1 };

        // Event interceptor
        repo.Changed += (s, e) => { };
        stub.Changed.VerifyAdd();
        #endregion

        // Exercise the stub
        repo.Save(new User());
        var user = repo.GetById<User>(1);
        var name = repo.Name;
        var item = repo["key1"];

        stub.Verify();
    }
}

// =============================================================================
// Access Patterns - Standalone, Inline Interface, Inline Class
// =============================================================================

public class AccessPatternsTests
{
    [Fact]
    public void Standalone_PatternAccess()
    {
        #region api-access-standalone-pattern
        // Standalone: [KnockOff] on class implementing IUserRepo
        var stub = new ApiUserRepoStub();

        // Interceptor accessed via interface-named property
        stub.GetById.OnCall((id) => new User { Id = id });
        stub.Save.OnCall((user) => { }).Verifiable();

        IApiUserRepo repository = stub;
        repository.Save(new User { Id = 1 });

        stub.Verify();
        #endregion
    }
}

// Partial class required for inline patterns
[KnockOff<IApiUserRepo>]
[KnockOff<ApiServiceClass>]
public partial class InlinePatternTests
{
}

public partial class InlinePatternTests
{
    [Fact]
    public void InlineInterface_PatternAccess()
    {
        #region api-access-inline-interface-pattern
        // Inline Interface: [KnockOff<IApiUserRepo>] generates Stubs.IApiUserRepo
        var stub = new Stubs.IApiUserRepo();

        // Same interceptor API as standalone
        stub.GetById.OnCall((id) => new User { Id = id });
        stub.Save.OnCall((user) => { }).Verifiable();

        IApiUserRepo repository = stub;
        repository.Save(new User { Id = 1 });

        stub.Verify();
        #endregion
    }

    [Fact]
    public void InlineClass_PatternAccess()
    {
        #region api-access-inline-class-pattern
        // Inline Class: [KnockOff<ApiServiceClass>] generates Stubs.ApiServiceClass
        var stub = new Stubs.ApiServiceClass();

        // Interceptors accessed via class-named container
        stub.GetUser.OnCall((id) => new User { Id = id, Name = "FromStub" });
        stub.SaveUser.OnCall((user) => { }).Verifiable();

        // Use .Object to get the actual class instance
        ApiServiceClass service = stub.Object;
        var user = service.GetUser(1);
        service.SaveUser(user!);

        stub.Verify();
        #endregion
    }
}

// =============================================================================
// Method Interceptor API
// =============================================================================

public class MethodInterceptorApiTests
{
    [Fact]
    public void MethodInterceptor_CompleteApiDemonstration()
    {
        var stub = new ApiMethodRepoStub();
        IApiMethodRepo repository = stub;

        #region method-interceptor-complete-api-demo
        // Configure void method with OnCall and mark verifiable
        stub.Save.OnCall((user) => { }).Verifiable();

        // Configure return method with OnCall
        var getTracking = stub.GetById.OnCall((id) =>
            new User { Id = id, Name = $"User{id}" }).Verifiable();

        // Configure multi-parameter method
        var updateTracking = stub.Update.OnCall((id, name) => { }).Verifiable();
        #endregion

        // Exercise the stub
        repository.Save(new User { Id = 1, Name = "Alice" });
        var user = repository.GetById(42);
        repository.Update(1, "UpdatedName");

        // Batch verify all Verifiable() interceptors
        stub.Verify();

        // Tracking object's LastArg for single-parameter methods
        Assert.Equal(42, getTracking.LastArg);

        // Tracking object's LastArgs tuple for multi-parameter methods
        var (id, name) = updateTracking.LastArgs;
        Assert.Equal(1, id);
        Assert.Equal("UpdatedName", name);

        // Additional assertions outside snippet
        Assert.NotNull(user);
        Assert.Equal("User42", user.Name);
    }
}

// =============================================================================
// Property Interceptor API
// =============================================================================

public class PropertyInterceptorApiTests
{
    [Fact]
    public void PropertyInterceptor_CompleteApiDemonstration()
    {
        var stub = new ApiPropertyRepoStub();
        IApiPropertyRepo repository = stub;

        #region property-interceptor-complete-api-demo
        // OnGet with value: configure getter return value
        stub.ConnectionString.OnGet("Server=localhost");

        // OnGet with callback: dynamic value
        stub.Timeout.OnGet(() => 30);

        // OnSet: configure setter callback
        stub.Timeout.OnSet((val) => { /* handle set */ });
        #endregion

        // Exercise getter
        var conn = repository.ConnectionString;
        var timeout = repository.Timeout;

        // VerifyGet: Check read count
        stub.ConnectionString.VerifyGet(Times.Once);
        stub.Timeout.VerifyGet(Times.Once);

        // Exercise setter
        repository.ConnectionString = "Server=production";
        repository.Timeout = 60;

        // VerifySet: Check write count
        stub.ConnectionString.VerifySet(Times.Once);

        // LastSetValue: Captured value from setter
        Assert.Equal("Server=production", stub.ConnectionString.LastSetValue);

        Assert.Equal("Server=localhost", conn);
        Assert.Equal(30, timeout);
    }
}

// =============================================================================
// Indexer Interceptor API
// =============================================================================

public class IndexerInterceptorApiTests
{
    [Fact]
    public void IndexerInterceptor_CompleteApiDemonstration()
    {
        var stub = new ApiIndexerRepoStub();
        IApiIndexerRepo repository = stub;

        #region indexer-interceptor-complete-api-demo
        // Backing: default dictionary storage for indexer
        stub.Indexer.Backing[1] = new User { Id = 1, Name = "Alice" };

        // OnGet: override backing lookup with callback
        stub.Indexer.OnGet((key) => new User { Id = key, Name = "FromCallback" });

        // OnSet: configure setter callback
        stub.Indexer.OnSet((key, value) => { /* handle set */ });
        #endregion

        // OnGet configured above, so it returns FromCallback
        var fromCallback = repository[1];
        Assert.Equal("FromCallback", fromCallback?.Name);

        // VerifyGet: Check read count
        stub.Indexer.VerifyGet(Times.Once);

        // LastGetKey: Key from most recent get
        Assert.Equal(1, stub.Indexer.LastGetKey);

        // OnSet configured above, callback fires (but doesn't update Backing)
        repository[3] = new User { Id = 3, Name = "Charlie" };

        // VerifySet: Check write count
        stub.Indexer.VerifySet(Times.Once);

        // LastSetEntry: Key-value tuple from most recent set
        var lastEntry = stub.Indexer.LastSetEntry;
        Assert.Equal(3, lastEntry?.Key);
        Assert.Equal("Charlie", lastEntry?.Value?.Name);

        // Backing wasn't updated by OnSet (unless callback explicitly does it)
        Assert.False(stub.Indexer.Backing.ContainsKey(3));
    }
}

// =============================================================================
// Event Interceptor API
// =============================================================================

public class EventInterceptorApiTests
{
    [Fact]
    public void EventInterceptor_CompleteApiDemonstration()
    {
        var stub = new ApiEventRepoStub();
        IApiEventRepo repository = stub;

        // Subscribe to event
        var changedInvoked = false;
        repository.Changed += (sender, e) => changedInvoked = true;

        #region event-interceptor-complete-api-demo
        // HasSubscribers: check for active subscriptions
        var hasSubscribers = stub.Changed.HasSubscribers;

        // Raise: fire event to all subscribers
        stub.Changed.Raise(repository, EventArgs.Empty);

        // VerifyAdd/VerifyRemove: check subscription counts
        stub.Changed.VerifyAdd(Times.Once);
        #endregion

        Assert.True(hasSubscribers);
        Assert.True(changedInvoked);

        // Unsubscribe tracking
        EventHandler handler = (sender, e) => { };
        repository.Changed += handler;
        repository.Changed -= handler;

        // VerifyRemove: Check unsubscription count
        stub.Changed.VerifyRemove(Times.Once);

        // Action<T> events: Same API, different Raise signature
        User? addedUser = null;
        repository.UserAdded += user => addedUser = user;

        // Raise with typed argument
        stub.UserAdded.Raise(new User { Id = 1, Name = "Alice" });
        Assert.NotNull(addedUser);
        Assert.Equal("Alice", addedUser.Name);
    }
}

// =============================================================================
// Generic Method Interceptor API
// =============================================================================

public class GenericMethodInterceptorApiTests
{
    [Fact]
    public void GenericMethodInterceptor_CompleteApiDemonstration()
    {
        var stub = new ApiGenericRepoStub();
        IApiGenericRepo repository = stub;

        #region generic-method-interceptor-complete-api-demo
        // Of<T>(): access typed interceptor for specific type argument
        stub.GetById.Of<User>().OnCall((id) =>
            new User { Id = id, Name = $"User{id}" });

        stub.GetById.Of<Product>().OnCall((id) =>
            new Product { Id = id, Name = $"Product{id}" });

        // CalledTypeArguments: list of all type arguments used
        var typeArgs = stub.GetById.CalledTypeArguments;
        #endregion

        // Call with different type arguments
        var user1 = repository.GetById<User>(1);
        var product = repository.GetById<Product>(2);
        var user2 = repository.GetById<User>(3);

        // CalledTypeArguments: List of all type arguments used
        Assert.Contains(typeof(User), stub.GetById.CalledTypeArguments);
        Assert.Contains(typeof(Product), stub.GetById.CalledTypeArguments);

        // Typed verification: Per-type call counts
        stub.GetById.Of<User>().Verify(Times.Exactly(2));
        stub.GetById.Of<Product>().Verify(Times.Once);

        // Typed LastCallArg: Per-type argument capture
        Assert.Equal(3, stub.GetById.Of<User>().LastCallArg);
        Assert.Equal(2, stub.GetById.Of<Product>().LastCallArg);

        // Typed Reset: Clears only specific type
        stub.GetById.Of<User>().Reset();
        stub.GetById.Of<User>().Verify(Times.Never);
        stub.GetById.Of<Product>().Verify(Times.Once); // Preserved

        // Base Reset: Clears all type arguments
        stub.GetById.Reset();
        stub.GetById.Of<Product>().Verify(Times.Never);
    }
}

// Support class for generic samples
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

// =============================================================================
// Times Constraint Examples
// =============================================================================

public class TimesConstraintTests
{
    [Fact]
    public void TimesConstraint_AllMethods()
    {
        var stub = new ApiUserRepoStub();
        IApiUserRepo repository = stub;

        // Setup
        stub.GetById.OnCall((id) => new User { Id = id });
        stub.Save.OnCall((user) => { });
        stub.Delete.OnCall((id) => { });

        #region times-constraint-usage-examples
        // Times.Never - Expected 0 calls
        stub.Delete.Verify(Times.Never);

        // Exercise stub
        repository.GetById(1);
        repository.Save(new User { Id = 1 });
        repository.Save(new User { Id = 2 });

        // Times.Once - Expected exactly 1 call
        stub.GetById.Verify(Times.Once);

        // Times.AtLeastOnce - Expected 1+ calls
        stub.Save.Verify(Times.AtLeastOnce);

        // Times.Exactly(n) - Expected exactly n calls
        stub.Save.Verify(Times.Exactly(2));

        // Times.AtLeast(n) - Expected n+ calls
        stub.Save.Verify(Times.AtLeast(1));

        // Times.AtMost(n) - Expected 0 to n calls
        stub.GetById.Verify(Times.AtMost(5));
        #endregion
    }
}

// =============================================================================
// Batch Verification Workflow
// =============================================================================

public class BatchVerificationTests
{
    [Fact]
    public void BatchVerification_Workflow()
    {
        var stub = new ApiUserRepoStub();
        IApiUserRepo repository = stub;

        #region batch-verification-workflow-example
        // Step 1: Mark interceptors with Verifiable()
        stub.GetById.OnCall((id) => new User { Id = id }).Verifiable();
        stub.Save.OnCall((user) => { }).Verifiable(Times.Exactly(2));
        stub.Delete.OnCall((id) => { }).Verifiable(Times.Never);

        // Step 2: Exercise the stub through the interface
        var user = repository.GetById(1);
        repository.Save(user!);
        repository.Save(new User { Id = 2 });
        // Note: Delete is NOT called (expected per Times.Never)

        // Step 3: Single Verify() call validates all marked interceptors
        stub.Verify();
        // Throws if any Verifiable() constraint is violated
        #endregion
    }
}
