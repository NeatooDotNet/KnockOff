using KnockOff;
using System.Threading.Tasks;

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

public interface IAsyncUserMethodRepo
{
    Task<User?> GetUserByIdAsync(int id);
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
    // Protected override method with underscore suffix
    // This is the fallback when no Return is configured
    protected override User? GetUserById_(int id)
    {
        return new User { Id = id, Name = "Default User" };
    }

    protected override bool IsActive_(int userId)
    {
        return true; // Default: users are active
    }

    protected override decimal GetBalance_(int userId)
    {
        return 100.00m; // Default test balance
    }
}
#endregion

// Async user method stub
[KnockOff]
public partial class AsyncUserMethodRepoStub : IAsyncUserMethodRepo { }

public partial class AsyncUserMethodRepoStub
{
    protected override Task<User?> GetUserByIdAsync_(int id)
    {
        return Task.FromResult<User?>(new User { Id = id, Name = "Default User" });
    }
}

// =============================================================================
// Fallback Tests - User methods provide defaults when no Return configured
// =============================================================================

public class UserMethodFallbackTests
{
    [Fact]
    public void UserMethod_ProvidesDefaultBehavior()
    {
        var stub = new UserMethodsRepoStub();
        IUserMethodsRepo repository = stub;

        #region user-methods-fallback
        // No Return configured - user method provides behavior
        var user = repository.GetUserById(1);

        // Verify the call was tracked
        stub.GetUserById.Verify(Times.Once);
        #endregion

        Assert.NotNull(user);
        Assert.Equal("Default User", user.Name);
        Assert.Equal(1, stub.GetUserById.LastArg);
    }
}

// =============================================================================
// Return Tests - Return supersedes user method
// =============================================================================

public class UserMethodReturnTests
{
    [Fact]
    public void Return_SupersedesUserMethod()
    {
        var stub = new UserMethodsRepoStub();
        IUserMethodsRepo repository = stub;

        #region user-methods-oncall
        // Return supersedes the user method
        stub.GetUserById.Return(id => new User { Id = id, Name = "Overridden" });

        var user = repository.GetUserById(42);
        Assert.Equal("Overridden", user!.Name);
        #endregion

        stub.GetUserById.Verify(Times.Once);
    }

    [Fact]
    public void Returns_SupersedesUserMethod()
    {
        var stub = new UserMethodsRepoStub();
        IUserMethodsRepo repository = stub;

        #region user-methods-returns
        // Return() for constant values
        stub.GetBalance.Return(500.00m);

        var balance = repository.GetBalance(1);
        Assert.Equal(500.00m, balance);
        #endregion
    }
}

// =============================================================================
// Async Return Tests - Returns auto-wraps in Task.FromResult
// =============================================================================

public class UserMethodAsyncReturnTests
{
    [Fact]
    public async Task Returns_AutoWrapsForAsyncMethods()
    {
        var stub = new AsyncUserMethodRepoStub();
        IAsyncUserMethodRepo repository = stub;

        #region user-methods-async-returns
        // Returns auto-wraps value in Task.FromResult for async methods
        stub.GetUserByIdAsync.Return(new User { Id = 99, Name = "Test User" });

        var user = await repository.GetUserByIdAsync(99);
        Assert.Equal("Test User", user!.Name);
        #endregion
    }
}

// =============================================================================
// Verification Tests - Tracking still works with Return
// =============================================================================

public class UserMethodVerificationTests
{
    [Fact]
    public void TrackingWorksWithReturn()
    {
        var stub = new UserMethodsRepoStub();
        IUserMethodsRepo repository = stub;

        #region user-methods-tracking
        stub.IsActive.Return(false);
        repository.IsActive(42);

        // Tracking works whether using Return or user method
        stub.IsActive.Verify(Times.Once);
        Assert.Equal(42, stub.IsActive.LastArg);
        #endregion
    }
}

// =============================================================================
// Reset Tests
// =============================================================================

public class UserMethodResetTests
{
    [Fact]
    public void Reset_ClearsTrackingButPreservesReturn()
    {
        var stub = new UserMethodsRepoStub();
        IUserMethodsRepo repository = stub;

        stub.GetBalance.Return(999.99m);
        repository.GetBalance(1);
        stub.GetBalance.Verify(Times.Once);

        #region user-methods-reset
        // Reset clears tracking state but preserves Return configuration
        stub.GetBalance.Reset();
        stub.GetBalance.Verify(Times.Never);
        #endregion

        // Return configuration is preserved after reset
        var balance = repository.GetBalance(2);
        Assert.Equal(999.99m, balance);
        stub.GetBalance.Verify(Times.Once);
    }
}

// =============================================================================
// Shareable Stub Pattern - Base class defaults, tests override
// =============================================================================

/// <summary>
/// Interface for shareable stub pattern example.
/// </summary>
public interface INotificationService
{
    bool SendEmail(string to, string subject);
    int GetPendingCount();
}

/// <summary>
/// Base stub with sensible defaults for common test scenarios.
/// Tests can override specific methods using Return when needed.
/// </summary>
#region user-methods-shareable-base
[KnockOff]
public partial class NotificationServiceStub : INotificationService { }

public partial class NotificationServiceStub
{
    // Default: emails succeed
    protected override bool SendEmail_(string to, string subject) => true;

    // Default: no pending notifications
    protected override int GetPendingCount_() => 0;
}
#endregion

public class ShareableStubPatternTests
{
    [Fact]
    public void TestUsesDefaultBehavior()
    {
        var stub = new NotificationServiceStub();
        INotificationService service = stub;

        #region user-methods-shareable-default
        // Most tests use the defaults
        var sent = service.SendEmail("user@test.com", "Welcome");
        Assert.True(sent); // Default behavior: success
        #endregion
    }

    [Fact]
    public void TestOverridesForFailureScenario()
    {
        var stub = new NotificationServiceStub();
        INotificationService service = stub;

        #region user-methods-shareable-override
        // Specific test overrides to simulate failure
        stub.SendEmail.Return(false);

        var sent = service.SendEmail("user@test.com", "Welcome");
        Assert.False(sent); // Return supersedes user method
        #endregion
    }
}

// =============================================================================
// Standalone Pattern Example - User Method with Return Override
// =============================================================================

public interface ISkillRepo
{
    User? GetById(int id);
}

#region user-methods-standalone-example
[KnockOff]
public partial class SkillRepoStub : ISkillRepo { }

public partial class SkillRepoStub
{
    // Override virtual method with underscore suffix - compiler enforces signature!
    protected override User? GetById_(int id) => new User { Id = id, Name = "Default" };
}
#endregion

// =============================================================================
// API Reference Example - Comprehensive User Method Interceptor Demo
// =============================================================================

public interface IApiRepo
{
    User? GetById(int id);
}

#region user-method-interceptor-standalone-api-example
[KnockOff]
public partial class ApiRepoStub : IApiRepo { }

public partial class ApiRepoStub
{
    // Override virtual method with underscore suffix - compiler enforces signature!
    protected override User? GetById_(int id) => new User { Id = id, Name = "Default" };
}
#endregion

public class UserMethodInterceptorApiExampleTests
{
    [Fact]
    public void UserMethodInterceptor_CompleteApiExample()
    {
        #region user-method-interceptor-usage-api-example
        var stub = new ApiRepoStub();
        IApiRepo repo = stub;

        // User method provides default behavior
        var user1 = repo.GetById(1);  // Returns "Default"

        // Return supersedes user method (clean interceptor name)
        stub.GetById.Return(id => new User { Id = id, Name = "Override" });
        var user2 = repo.GetById(2);  // Returns "Override"

        // Full tracking works - counts all calls regardless of configuration
        stub.GetById.Verify(Times.Exactly(2));
        Assert.Equal(2, stub.GetById.LastArg);

        // Returns for constant values (auto-wraps for async)
        stub.GetById.Return(new User { Id = 99 });
        var user3 = repo.GetById(3);  // Returns User { Id = 99 }
        #endregion

        Assert.Equal("Default", user1!.Name);
        Assert.Equal("Override", user2!.Name);
        Assert.Equal(99, user3!.Id);
    }
}

public class UserMethodStandalonePatternTests
{
    [Fact]
    public void UserMethod_FullExample()
    {
        #region user-methods-standalone-usage
        // Usage:
        var stub = new SkillRepoStub();
        ISkillRepo repo = stub;

        // Without Return: user method provides behavior
        var user1 = repo.GetById(1);  // Name = "Default"

        // With Return: callback supersedes user method (clean interceptor name)
        stub.GetById.Return(id => new User { Id = id, Name = "Override" });
        var user2 = repo.GetById(2);  // Name = "Override"
        #endregion

        Assert.Equal("Default", user1!.Name);
        Assert.Equal("Override", user2!.Name);
    }

    [Fact]
    public void UserMethod_TrackingWithReturn()
    {
        var stub = new SkillRepoStub();
        ISkillRepo repo = stub;

        #region user-methods-tracking-with-oncall
        stub.GetById.Return(id => new User { Id = id });
        repo.GetById(42);

        stub.GetById.Verify(Times.Once);
        Assert.Equal(42, stub.GetById.LastArg);
        #endregion
    }

    [Fact]
    public void UserMethod_ResetPreservesReturn()
    {
        var stub = new SkillRepoStub();
        ISkillRepo repo = stub;

        #region user-methods-reset-preserves-oncall
        stub.GetById.Return(id => new User { Id = id });
        repo.GetById(1);
        stub.GetById.Verify(Times.Once);

        stub.GetById.Reset();
        stub.GetById.Verify(Times.Never);  // Tracking cleared

        repo.GetById(2);  // Still uses Return callback (not reset to user method)
        #endregion

        Assert.Equal(2, stub.GetById.LastArg);
    }
}

// =============================================================================
// Generated Base Class Pattern
// =============================================================================

public class GeneratedBaseClassTests
{
    [Fact]
    public void GeneratedBaseClass_ShowsPattern()
    {
        #region user-methods-generated-base
        // Generated base class (you don't write this -- KnockOff generates it):
        //
        //   public partial class UserMethodsRepoStubBase
        //   {
        //       protected virtual User? GetUserById_(int id) => default!;
        //       protected virtual bool IsActive_(int userId) => default!;
        //       protected virtual decimal GetBalance_(int userId) => default!;
        //   }
        //
        // Your override in the partial class:
        //   protected override User? GetUserById_(int id) => new User { Id = id };
        #endregion

        var stub = new UserMethodsRepoStub();
        IUserMethodsRepo repo = stub;
        var user = repo.GetUserById(1);
        Assert.NotNull(user);
        Assert.Equal("Default User", user.Name);
    }
}

// =============================================================================
// Overloads Support
// =============================================================================

public interface IFormatter
{
    string Format(string input);
    string Format(string input, bool uppercase);
}

[KnockOff]
public partial class UserMethodsFormatterStub : IFormatter { }

#region user-methods-overloads
public partial class UserMethodsFormatterStub
{
    // Override only the overloads you need
    protected override string Format_(string input) => input.ToUpperInvariant();

    // Override other overloads with custom logic
    protected override string Format_(string input, bool uppercase)
        => uppercase ? input.ToUpperInvariant() : input.ToLowerInvariant();
}
#endregion

public class OverloadUserMethodTests
{
    [Fact]
    public void Overloads_SelectiveOverride()
    {
        var stub = new UserMethodsFormatterStub();
        IFormatter formatter = stub;

        var result1 = formatter.Format("hello");
        Assert.Equal("HELLO", result1);

        var result2 = formatter.Format("Hello", false);
        Assert.Equal("hello", result2);
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
        // User method provides default; Return can override
        var user = repository.GetUserById(42);
        stub.GetUserById.Verify(Times.Once);

        // Override for next call
        stub.GetUserById.Return(id => new User { Id = id, Name = "Custom" });
        var customUser = repository.GetUserById(99);
        Assert.Equal("Custom", customUser!.Name);
        #endregion

        Assert.NotNull(user);
        Assert.Equal("Default User", user.Name);
    }

    [Fact]
    public void MultipleCallsTrackedCorrectly()
    {
        var stub = new UserMethodsRepoStub();
        IUserMethodsRepo repository = stub;

        repository.GetUserById(1);
        repository.GetUserById(2);
        repository.GetUserById(3);

        stub.GetUserById.Verify(Times.Exactly(3));
        Assert.Equal(3, stub.GetUserById.LastArg);
    }
}
