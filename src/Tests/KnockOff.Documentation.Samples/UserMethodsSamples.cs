namespace KnockOff.Documentation.Samples.UserMethods;

// =============================================================================
// Interfaces for User Methods Samples
// =============================================================================

public interface IUserMethodsRepo
{
    User? GetUserById(int id);
    bool IsActive(int userId);
    decimal GetBalance(int userId);
}

// =============================================================================
// Stubs with User Methods
// =============================================================================

#region user-methods-basic
[KnockOff]
public partial class UserMethodsRepoStub : IUserMethodsRepo { }

// User methods provide default behavior
public partial class UserMethodsRepoStub
{
    // Protected method matches interface method signature
    // This becomes the default behavior when no OnCall is set
    protected User? GetUserById(int id)
    {
        return new User { Id = id, Name = "Default User" };
    }

    protected bool IsActive(int userId)
    {
        return true; // Default: users are active
    }

    protected decimal GetBalance(int userId)
    {
        return 100.00m; // Default test balance
    }
}
#endregion

// =============================================================================
// Priority Tests - User methods provide defaults
// =============================================================================

public class UserMethodPriorityTests
{
    #region user-methods-priority
    [Fact]
    public void UserMethod_ProvidesDefaultBehavior()
    {
        var stub = new UserMethodsRepoStub();
        IUserMethodsRepo repository = stub;

        // User method provides default behavior automatically
        var user = repository.GetUserById(1);

        Assert.NotNull(user);
        Assert.Equal("Default User", user.Name);

        // Interceptor tracks that the method was called
        Assert.Equal(1, stub.GetUserById2.CallCount);
        Assert.Equal(1, stub.GetUserById2.LastArg);
    }
    #endregion
}

// =============================================================================
// Override Tests - Cannot override with OnCall (tracking only)
// =============================================================================

public class UserMethodOverrideTests
{
    #region user-methods-override
    [Fact]
    public void UserMethod_InterceptorTracksCallsOnly()
    {
        var stub = new UserMethodsRepoStub();
        IUserMethodsRepo repository = stub;

        // User method returns default value
        var isActive = repository.IsActive(42);
        Assert.True(isActive);

        // User method interceptors are tracking-only
        // They don't have OnCall - use Source delegation to override
        Assert.True(stub.IsActive2.WasCalled);
        Assert.Equal(42, stub.IsActive2.LastArg);
    }
    #endregion
}

// =============================================================================
// Reset Tests
// =============================================================================

public class UserMethodResetTests
{
    #region user-methods-reset
    [Fact]
    public void Reset_ClearsUserMethodTracking()
    {
        var stub = new UserMethodsRepoStub();
        IUserMethodsRepo repository = stub;

        // Call method
        repository.GetBalance(1);
        Assert.Equal(1, stub.GetBalance2.CallCount);

        // Reset clears tracking
        stub.GetBalance2.Reset();
        Assert.Equal(0, stub.GetBalance2.CallCount);

        // User method still works after reset
        var balance = repository.GetBalance(2);
        Assert.Equal(100.00m, balance);
        Assert.Equal(1, stub.GetBalance2.CallCount);
    }
    #endregion
}

// =============================================================================
// Complete Example
// =============================================================================

public class CompleteUserMethodExampleTests
{
    #region user-methods-complete-example
    [Fact]
    public void StandardUserRetrieval_UsesUserMethodDefaults()
    {
        var stub = new UserMethodsRepoStub();
        IUserMethodsRepo repository = stub;

        // All user methods provide defaults automatically
        var user = repository.GetUserById(42);
        var isActive = repository.IsActive(42);
        var balance = repository.GetBalance(42);

        // User methods return expected defaults
        Assert.NotNull(user);
        Assert.Equal("Default User", user.Name);
        Assert.True(isActive);
        Assert.Equal(100.00m, balance);

        // All calls are tracked via *2 interceptors
        Assert.Equal(1, stub.GetUserById2.CallCount);
        Assert.Equal(1, stub.IsActive2.CallCount);
        Assert.Equal(1, stub.GetBalance2.CallCount);
    }

    [Fact]
    public void MultipleCallsTrackedCorrectly()
    {
        var stub = new UserMethodsRepoStub();
        IUserMethodsRepo repository = stub;

        // Make multiple calls
        repository.GetUserById(1);
        repository.GetUserById(2);
        repository.GetUserById(3);

        // All calls tracked
        Assert.Equal(3, stub.GetUserById2.CallCount);
        Assert.Equal(3, stub.GetUserById2.LastArg); // Last call was id=3
    }
    #endregion
}
