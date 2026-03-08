using KnockOff;
using System.Threading.Tasks;

namespace KnockOff.Documentation.Samples.StubOverrides;

// =============================================================================
// Interfaces for Stub Overrides Samples
// =============================================================================

public interface IStubOverridesRepo
{
    User? GetUserById(int id);
    bool IsActive(int userId);
    decimal GetBalance(int userId);
}

public interface IAsyncStubOverrideRepo
{
    Task<User?> GetUserByIdAsync(int id);
}

public interface IAsyncOverrideDemo
{
    Task<string> ProcessAsync(string input);
    ValueTask<int> ComputeAsync(int value);
}

// =============================================================================
// Stubs with Stub Overrides
// =============================================================================

#region stub-overrides-basic
[KnockOff]
public partial class StubOverridesRepoStub : IStubOverridesRepo
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

// Async stub override stub
[KnockOff]
public partial class AsyncStubOverrideRepoStub : IAsyncStubOverrideRepo
{
    protected override Task<User?> GetUserByIdAsync_(int id)
    {
        return Task.FromResult<User?>(new User { Id = id, Name = "Default User" });
    }
}

// Async stub override demo with Task<T> and ValueTask<T>
#region async-stub-overrides-define
[KnockOff]
public partial class AsyncOverrideDemoStub : IAsyncOverrideDemo
{
    protected override async Task<string> ProcessAsync_(string input)
    {
        await Task.Delay(1);
        return $"[Async: {input}]";
    }

    protected override async ValueTask<int> ComputeAsync_(int value)
    {
        await Task.Yield();
        return value * 2;
    }
}
#endregion

// =============================================================================
// Fallback Tests - Stub overrides provide defaults when no Return configured
// =============================================================================

public class StubOverrideFallbackTests
{
    [Fact]
    public void StubOverride_ProvidesDefaultBehavior()
    {
        var stub = new StubOverridesRepoStub();
        IStubOverridesRepo repository = stub;

        #region stub-overrides-fallback
        // No Return configured - stub override provides behavior
        var user = repository.GetUserById(1);

        // Verify the call was tracked
        stub.GetUserById.Verify(Called.Once);
        #endregion

        Assert.NotNull(user);
        Assert.Equal("Default User", user.Name);
        Assert.Equal(1, stub.GetUserById.LastArg);
    }
}

// =============================================================================
// Return Tests - Return supersedes stub override
// =============================================================================

public class StubOverrideReturnTests
{
    [Fact]
    public void Return_SupersedesStubOverride()
    {
        var stub = new StubOverridesRepoStub();
        IStubOverridesRepo repository = stub;

        #region stub-overrides-oncall
        // Return supersedes the stub override
        stub.GetUserById.Call(id => new User { Id = id, Name = "Overridden" });

        var user = repository.GetUserById(42);
        Assert.Equal("Overridden", user!.Name);
        #endregion

        stub.GetUserById.Verify(Called.Once);
    }

    [Fact]
    public void Returns_SupersedesStubOverride()
    {
        var stub = new StubOverridesRepoStub();
        IStubOverridesRepo repository = stub;

        #region stub-overrides-returns
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

public class StubOverrideAsyncReturnTests
{
    [Fact]
    public async Task Returns_AutoWrapsForAsyncMethods()
    {
        var stub = new AsyncStubOverrideRepoStub();
        IAsyncStubOverrideRepo repository = stub;

        #region stub-overrides-async-returns
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

public class StubOverrideVerificationTests
{
    [Fact]
    public void TrackingWorksWithReturn()
    {
        var stub = new StubOverridesRepoStub();
        IStubOverridesRepo repository = stub;

        #region stub-overrides-tracking
        stub.IsActive.Return(false);
        repository.IsActive(42);

        // Tracking works whether using Return or stub override
        stub.IsActive.Verify(Called.Once);
        Assert.Equal(42, stub.IsActive.LastArg);
        #endregion
    }
}

// =============================================================================
// Reset Tests
// =============================================================================

public class StubOverrideResetTests
{
    [Fact]
    public void Reset_ClearsTrackingButPreservesReturn()
    {
        var stub = new StubOverridesRepoStub();
        IStubOverridesRepo repository = stub;

        stub.GetBalance.Return(999.99m);
        repository.GetBalance(1);
        stub.GetBalance.Verify(Called.Once);

        #region stub-overrides-reset
        // Reset clears tracking state but preserves Return configuration
        stub.GetBalance.Reset();
        stub.GetBalance.Verify(Called.Never);
        #endregion

        // Return configuration is preserved after reset
        var balance = repository.GetBalance(2);
        Assert.Equal(999.99m, balance);
        stub.GetBalance.Verify(Called.Once);
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
#region stub-overrides-shareable-base
[KnockOff]
public partial class NotificationServiceStub : INotificationService
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

        #region stub-overrides-shareable-default
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

        #region stub-overrides-shareable-override
        // Specific test overrides to simulate failure
        stub.SendEmail.Return(false);

        var sent = service.SendEmail("user@test.com", "Welcome");
        Assert.False(sent); // Return supersedes stub override
        #endregion
    }
}

// =============================================================================
// Standalone Pattern Example - Stub Override with Return Override
// =============================================================================

public interface ISkillRepo
{
    User? GetById(int id);
}

#region stub-overrides-standalone-example
[KnockOff]
public partial class SkillRepoStub : ISkillRepo
{
    // Override virtual method with underscore suffix - compiler enforces signature!
    protected override User? GetById_(int id) => new User { Id = id, Name = "Default" };
}
#endregion

// =============================================================================
// API Reference Example - Comprehensive Stub Override Interceptor Demo
// =============================================================================

public interface IApiRepo
{
    User? GetById(int id);
}

#region user-method-interceptor-standalone-api-example
[KnockOff]
public partial class ApiRepoStub : IApiRepo
{
    // Override virtual method with underscore suffix - compiler enforces signature!
    protected override User? GetById_(int id) => new User { Id = id, Name = "Default" };
}
#endregion

public class StubOverrideInterceptorApiExampleTests
{
    [Fact]
    public void StubOverrideInterceptor_CompleteApiExample()
    {
        #region user-method-interceptor-usage-api-example
        var stub = new ApiRepoStub();
        IApiRepo repo = stub;

        // Stub override provides default behavior
        var user1 = repo.GetById(1);  // Returns "Default"

        // Return supersedes stub override (clean interceptor name)
        stub.GetById.Call(id => new User { Id = id, Name = "Override" });
        var user2 = repo.GetById(2);  // Returns "Override"

        // Full tracking works - counts all calls regardless of configuration
        stub.GetById.Verify(Called.Exactly(2));
        Assert.Equal(2, stub.GetById.LastArg);

        // Return for constant values (auto-wraps for async)
        stub.GetById.Return(new User { Id = 99 });
        var user3 = repo.GetById(3);  // Returns User { Id = 99 }
        #endregion

        Assert.Equal("Default", user1!.Name);
        Assert.Equal("Override", user2!.Name);
        Assert.Equal(99, user3!.Id);
    }
}

public class StubOverrideStandalonePatternTests
{
    [Fact]
    public void StubOverride_FullExample()
    {
        #region stub-overrides-standalone-usage
        // Usage:
        var stub = new SkillRepoStub();
        ISkillRepo repo = stub;

        // Without Return: stub override provides behavior
        var user1 = repo.GetById(1);  // Name = "Default"

        // With Return: callback supersedes stub override (clean interceptor name)
        stub.GetById.Call(id => new User { Id = id, Name = "Override" });
        var user2 = repo.GetById(2);  // Name = "Override"
        #endregion

        Assert.Equal("Default", user1!.Name);
        Assert.Equal("Override", user2!.Name);
    }

    [Fact]
    public void StubOverride_TrackingWithReturn()
    {
        var stub = new SkillRepoStub();
        ISkillRepo repo = stub;

        #region stub-overrides-tracking-with-oncall
        stub.GetById.Call(id => new User { Id = id });
        repo.GetById(42);

        stub.GetById.Verify(Called.Once);
        Assert.Equal(42, stub.GetById.LastArg);
        #endregion
    }

    [Fact]
    public void StubOverride_ResetPreservesReturn()
    {
        var stub = new SkillRepoStub();
        ISkillRepo repo = stub;

        #region stub-overrides-reset-preserves-oncall
        stub.GetById.Call(id => new User { Id = id });
        repo.GetById(1);
        stub.GetById.Verify(Called.Once);

        stub.GetById.Reset();
        stub.GetById.Verify(Called.Never);  // Tracking cleared

        repo.GetById(2);  // Still uses Return callback (not reset to stub override)
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
        #region stub-overrides-generated-base
        // Generated base class (you don't write this -- KnockOff generates it):
        //
        //   public partial class StubOverridesRepoStubBase
        //   {
        //       protected virtual User? GetUserById_(int id) => default!;
        //       protected virtual bool IsActive_(int userId) => default!;
        //       protected virtual decimal GetBalance_(int userId) => default!;
        //   }
        //
        // Your override in the partial class:
        //   protected override User? GetUserById_(int id) => new User { Id = id };
        #endregion

        var stub = new StubOverridesRepoStub();
        IStubOverridesRepo repo = stub;
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

#region stub-overrides-overloads
[KnockOff]
public partial class StubOverrideFormatterStub : IFormatter
{
    // Override only the overloads you need
    protected override string Format_(string input) => input.ToUpperInvariant();

    // Override other overloads with custom logic
    protected override string Format_(string input, bool uppercase)
        => uppercase ? input.ToUpperInvariant() : input.ToLowerInvariant();
}
#endregion

public class OverloadStubOverrideTests
{
    [Fact]
    public void Overloads_SelectiveOverride()
    {
        var stub = new StubOverrideFormatterStub();
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

// =============================================================================
// Stub Override Reference Samples (for stub-overrides.md)
// =============================================================================

public interface IStubOverrideMixedSvc
{
    string WithOverride(string input);
    string WithoutOverride(string input);
}

[KnockOff]
public partial class StubOverrideMixedStub : IStubOverrideMixedSvc
{
    protected override string WithOverride_(string input) => $"[User: {input}]";
    // WithoutOverride_ is NOT overridden
}

public class StubOverrideRefMixedTests
{
    [Fact]
    public void StubOverrides_Mixed()
    {
        var stub = new StubOverrideMixedStub();
        IStubOverrideMixedSvc service = stub;

        #region stub-overrides-ref-mixed
        // Methods WITH override use it as default
        service.WithOverride("test");    // "[User: test]"

        // Methods WITHOUT override need configuration or return default
        stub.WithoutOverride.Call((input) => $"[Configured: {input}]");
        service.WithoutOverride("test"); // "[Configured: test]"
        #endregion

        Assert.Equal("[User: test]", service.WithOverride("test"));
        Assert.Equal("[Configured: test]", service.WithoutOverride("test"));
    }
}

public class StubOverrideRefReturnTests
{
    [Fact]
    public void StubOverrides_ReturnSupersedes()
    {
        var stub = new StubOverridesRepoStub();
        IStubOverridesRepo service = stub;

        #region stub-overrides-ref-return-supersedes
        // Default behavior from override
        service.GetUserById(1); // "Default User"

        // Supersede with Return for this test
        stub.GetUserById.Call(id => new User { Id = id, Name = "Override" });
        service.GetUserById(1); // "Override"
        #endregion

        Assert.Equal("Override", service.GetUserById(1)!.Name);
    }

    [Fact]
    public void StubOverrides_WhenChain()
    {
        var stub = new StubOverridesRepoStub();
        IStubOverridesRepo service = stub;

        #region stub-overrides-ref-when
        stub.GetUserById.When(42).Return(new User { Id = 42, Name = "SPECIAL" });

        service.GetUserById(42); // "SPECIAL" (When matched)
        service.GetUserById(1);  // "Default User" (stub override)
        #endregion

        Assert.Equal("SPECIAL", service.GetUserById(42)!.Name);
        Assert.Equal("Default User", service.GetUserById(1)!.Name);
    }
}

public class StubOverrideRefStrictTests
{
    [Fact]
    public void StubOverrides_StrictBypass()
    {
        #region stub-overrides-ref-strict
        var stub = new StubOverridesRepoStub();
        stub.Strict = true;

        IStubOverridesRepo service = stub;

        // Stub overrides bypass strict mode -- they ARE the configuration
        var user = service.GetUserById(1); // Works -- override IS the config
        Assert.Equal("Default User", user!.Name);
        #endregion
    }
}

public class CompleteStubOverrideExampleTests
{
    [Fact]
    public void StandardUserRetrieval_UsesStubOverrideDefaults()
    {
        var stub = new StubOverridesRepoStub();
        IStubOverridesRepo repository = stub;

        #region stub-overrides-complete-example
        // Stub override provides default; Return can override
        var user = repository.GetUserById(42);
        stub.GetUserById.Verify(Called.Once);

        // Override for next call
        stub.GetUserById.Call(id => new User { Id = id, Name = "Custom" });
        var customUser = repository.GetUserById(99);
        Assert.Equal("Custom", customUser!.Name);
        #endregion

        Assert.NotNull(user);
        Assert.Equal("Default User", user.Name);
    }

    [Fact]
    public void MultipleCallsTrackedCorrectly()
    {
        var stub = new StubOverridesRepoStub();
        IStubOverridesRepo repository = stub;

        repository.GetUserById(1);
        repository.GetUserById(2);
        repository.GetUserById(3);

        stub.GetUserById.Verify(Called.Exactly(3));
        Assert.Equal(3, stub.GetUserById.LastArg);
    }
}
