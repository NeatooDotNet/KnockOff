using KnockOff;

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
    // This becomes the behavior (user method interceptors have no OnCall)
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

        // Interceptor tracks that the method was called - verify with Times
        stub.GetUserById2.Verify(Times.Once);
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
        stub.IsActive2.Verify(Times.Once);
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
        stub.GetBalance2.Verify(Times.Once);

        // Reset clears tracking
        stub.GetBalance2.Reset();
        stub.GetBalance2.Verify(Times.Never);

        // User method still works after reset
        var balance = repository.GetBalance(2);
        Assert.Equal(100.00m, balance);
        stub.GetBalance2.Verify(Times.Once);
    }
    #endregion
}

// =============================================================================
// Source Override - Use a regular stub when you need override capability
// =============================================================================

/// <summary>
/// When you need to override behavior, use a regular stub with OnCall
/// instead of user methods. User methods are for permanent defaults.
/// </summary>
[KnockOff]
public partial class OverridableRepoStub : IUserMethodsRepo { }

public class SourceOverrideTests
{
    #region user-methods-source-override
    [Fact]
    public void WhenOverrideNeeded_UseRegularStubWithOnCall()
    {
        // Use a stub WITHOUT user methods when you need OnCall
        var stub = new OverridableRepoStub();

        // Configure specific behavior with OnCall
        stub.GetUserById.OnCall((id) => new User { Id = id, Name = "Overridden" });
        stub.IsActive.OnCall(true);
        stub.GetBalance.OnCall(999.99m);

        IUserMethodsRepo repository = stub;

        // OnCall provides the behavior
        var user = repository.GetUserById(1);
        Assert.Equal("Overridden", user!.Name);
        Assert.True(repository.IsActive(1));
        Assert.Equal(999.99m, repository.GetBalance(1));

        // Still get full verification
        stub.GetUserById.Verify(Times.Once);
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

        // All calls are tracked via *2 interceptors - verify with Times
        stub.GetUserById2.Verify(Times.Once);
        stub.IsActive2.Verify(Times.Once);
        stub.GetBalance2.Verify(Times.Once);
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

        // Verify call count using Times
        stub.GetUserById2.Verify(Times.Exactly(3));
        Assert.Equal(3, stub.GetUserById2.LastArg); // Last call was id=3
    }
    #endregion
}
