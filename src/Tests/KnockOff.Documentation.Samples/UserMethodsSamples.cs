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
    [Fact]
    public void UserMethod_ProvidesDefaultBehavior()
    {
        var stub = new UserMethodsRepoStub();
        IUserMethodsRepo repository = stub;

        #region user-methods-priority
        // User method provides default behavior automatically
        var user = repository.GetUserById(1);

        // Verify the call was tracked (user method interceptors end with "2")
        stub.GetUserById2.Verify(Times.Once);
        #endregion

        Assert.NotNull(user);
        Assert.Equal("Default User", user.Name);
        Assert.Equal(1, stub.GetUserById2.LastArg);
    }
}

// =============================================================================
// Override Tests - Cannot override with OnCall (tracking only)
// =============================================================================

public class UserMethodOverrideTests
{
    [Fact]
    public void UserMethod_InterceptorTracksCallsOnly()
    {
        var stub = new UserMethodsRepoStub();
        IUserMethodsRepo repository = stub;

        var isActive = repository.IsActive(42);
        Assert.True(isActive);

        #region user-methods-override
        // User method interceptors are tracking-only (no OnCall available)
        stub.IsActive2.Verify(Times.Once);
        Assert.Equal(42, stub.IsActive2.LastArg);
        #endregion
    }
}

// =============================================================================
// Reset Tests
// =============================================================================

public class UserMethodResetTests
{
    [Fact]
    public void Reset_ClearsUserMethodTracking()
    {
        var stub = new UserMethodsRepoStub();
        IUserMethodsRepo repository = stub;

        repository.GetBalance(1);
        stub.GetBalance2.Verify(Times.Once);

        #region user-methods-reset
        // Reset clears call count and argument tracking
        stub.GetBalance2.Reset();
        stub.GetBalance2.Verify(Times.Never);
        #endregion

        var balance = repository.GetBalance(2);
        Assert.Equal(100.00m, balance);
        stub.GetBalance2.Verify(Times.Once);
    }
}

// =============================================================================
// Runtime-Configurable Behavior - Use a regular stub when OnCall is needed
// =============================================================================

/// <summary>
/// When you need runtime-configurable behavior, use a stub without user methods.
/// User method interceptors don't have OnCall - the protected method IS the behavior.
/// </summary>
[KnockOff]
public partial class OverridableRepoStub : IUserMethodsRepo { }

public class SourceOverrideTests
{
    [Fact]
    public void WhenOverrideNeeded_UseRegularStubWithOnCall()
    {
        var stub = new OverridableRepoStub();
        IUserMethodsRepo repository = stub;

        #region user-methods-source-override
        // For runtime-configurable behavior, use a regular stub with OnCall
        stub.GetUserById.OnCall((id) => new User { Id = id, Name = "Overridden" });
        stub.IsActive.Returns(true);
        stub.GetBalance.Returns(999.99m);
        #endregion

        var user = repository.GetUserById(1);
        Assert.Equal("Overridden", user!.Name);
        Assert.True(repository.IsActive(1));
        Assert.Equal(999.99m, repository.GetBalance(1));
        stub.GetUserById.Verify(Times.Once);
    }
}

// =============================================================================
// Complete Example
// =============================================================================

public class CompleteUserMethodExampleTests
{
    [Fact]
    public void StandardUserRetrieval_UsesUserMethodDefaults()
    {
        var stub = new UserMethodsRepoStub();
        IUserMethodsRepo repository = stub;

        #region user-methods-complete-example
        // User methods provide defaults; interceptors track calls
        var user = repository.GetUserById(42);
        var isActive = repository.IsActive(42);

        // Verify with *2 interceptors (user method tracking)
        stub.GetUserById2.Verify(Times.Once);
        stub.IsActive2.Verify(Times.Once);
        #endregion

        Assert.NotNull(user);
        Assert.Equal("Default User", user.Name);
        Assert.True(isActive);
    }

    [Fact]
    public void MultipleCallsTrackedCorrectly()
    {
        var stub = new UserMethodsRepoStub();
        IUserMethodsRepo repository = stub;

        repository.GetUserById(1);
        repository.GetUserById(2);
        repository.GetUserById(3);

        stub.GetUserById2.Verify(Times.Exactly(3));
        Assert.Equal(3, stub.GetUserById2.LastArg);
    }
}
