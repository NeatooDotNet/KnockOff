namespace KnockOff.Documentation.Samples.Troubleshooting;

// =============================================================================
// Interfaces and Classes for Troubleshooting Samples
// =============================================================================

public interface ITroubleshootRepo
{
    User? GetById(int id);
    Task<User?> GetByIdAsync(int id);
    string GetName();
}

public interface IConfigSvc
{
    string Host { get; set; }
    int Port { get; set; }
}

public class EmailService
{
    public virtual bool Send(string to, string subject) => false;
}

// =============================================================================
// Stubs for Troubleshooting Samples
// =============================================================================

// Correct: partial keyword is required
[KnockOff]
public partial class TroubleshootRepoStub : ITroubleshootRepo { }

[KnockOff]
public partial class ConfigSvcStub : IConfigSvc { }

// Class stub for .Object property demonstration
[KnockOff<EmailService>]
public partial class EmailServiceTests { }

// =============================================================================
// Partial Keyword Required
// =============================================================================

#region troubleshoot-partial
// ERROR: Without `partial`, generated code won't merge
// public class BadStub : IRepository { }  // CS0535: does not implement interface

// CORRECT: Add `partial` keyword
[KnockOff]
public partial class TroubleshootGoodStub : ITroubleshootRepo { }
#endregion

public class PartialKeywordTests
{
    [Fact]
    public void PartialKeyword_AllowsGeneratedCodeToMerge()
    {
        // TroubleshootGoodStub compiles because it's partial
        var stub = new TroubleshootGoodStub();
        stub.GetById.OnCall((id) => new User { Id = id });

        ITroubleshootRepo repository = stub;
        var user = repository.GetById(1);

        Assert.NotNull(user);
    }
}

// =============================================================================
// Object Property for Class Stubs
// =============================================================================

public partial class EmailServiceTests
{
    #region troubleshoot-object
    [Fact]
    public void ClassStub_RequiresObjectProperty()
    {
        var stub = new Stubs.EmailService();

        // Configure the stub
        stub.Send.OnCall((to, subject) => true);

        // ERROR (commented out): Cannot pass stub directly
        // Method expects EmailService, not Stubs.EmailService
        // SomeMethodExpectingEmailService(stub);

        // CORRECT: Use .Object to get the EmailService instance
        EmailService service = stub.Object;

        // Now it can be used wherever EmailService is expected
        var result = service.Send("test@example.com", "Hello");
        Assert.True(result);
    }

    // Example method expecting the base class type
    private void UseEmailService(EmailService service)
    {
        service.Send("a@b.com", "Test");
    }

    [Fact]
    public void PassingStubObjectToMethod()
    {
        var stub = new Stubs.EmailService();
        stub.Send.OnCall((to, subject) => true);

        // Pass stub.Object to method expecting EmailService
        UseEmailService(stub.Object);

        stub.Send.Verify();
    }
    #endregion
}

// =============================================================================
// OnCall Signature - Parameter Types
// =============================================================================

public class OnCallSignatureTests
{
    #region troubleshoot-oncall-signature
    [Fact]
    public void OnCallSignature_MustMatchParameters()
    {
        var stub = new TroubleshootRepoStub();

        // ERROR (won't compile): Wrong parameter type
        // stub.GetByIdAsync.OnCall((string id) => Task.FromResult<User?>(null));

        // CORRECT: Match parameter type (int id)
        stub.GetByIdAsync.OnCall((int id) =>
            Task.FromResult<User?>(new User { Id = id, Name = "Test" }));

        ITroubleshootRepo repository = stub;
        var user = repository.GetByIdAsync(42).Result;

        Assert.NotNull(user);
        Assert.Equal(42, user.Id);
    }
    #endregion
}

// =============================================================================
// No Callback Configured
// =============================================================================

public class NoCallbackConfiguredTests
{
    #region troubleshoot-no-callback
    [Fact]
    public void MethodWithoutCallback_UsesSmartDefaults()
    {
        var stub = new TroubleshootRepoStub();
        ITroubleshootRepo repository = stub;

        // Without configuration, smart defaults apply:
        // - Nullable returns null
        // - Non-nullable with ctor returns new instance
        // - Value types return default

        // Nullable User? returns null by default
        var user = repository.GetById(1);
        Assert.Null(user);

        // For non-nullable string, configure explicitly:
        stub.GetName.OnCall(() => "Configured Name");
        var name = repository.GetName();
        Assert.Equal("Configured Name", name);
    }

    [Fact]
    public void FixOptions_ForRequiredReturnValues()
    {
        var stub = new ConfigSvcStub();
        IConfigSvc config = stub;

        // Fix Option 1: Use OnGet with a static value
        stub.Host.OnGet("localhost");
        Assert.Equal("localhost", config.Host);

        // Fix Option 2: Use OnGet with callback for dynamic behavior
        stub.Port.OnGet(() => 8080);
        Assert.Equal(8080, config.Port);
    }
    #endregion
}

// =============================================================================
// OnGet Priority
// =============================================================================

public class OnGetPriorityTests
{
    #region troubleshoot-onget-priority
    [Fact]
    public void OnGet_MostRecentTakesPrecedence()
    {
        var stub = new ConfigSvcStub();
        IConfigSvc config = stub;

        // Configure OnGet with callback
        stub.Host.OnGet(() => "from-callback");

        // Access uses OnGet
        Assert.Equal("from-callback", config.Host);

        // OnGet with value overrides previous callback
        stub.Host.OnGet("from-value");

        // Most recent OnGet configuration wins
        Assert.Equal("from-value", config.Host);

        // OnGet with callback can override again
        stub.Host.OnGet(() => "back-to-callback");
        Assert.Equal("back-to-callback", config.Host);
    }

    [Fact]
    public void Understanding_Property_Priority()
    {
        var stub = new ConfigSvcStub();
        IConfigSvc config = stub;

        // Priority order (from highest to lowest):
        // 1. OnGetSequence (if configured and not exhausted)
        // 2. OnGet callback/value (most recent takes precedence)
        // 3. Source delegation (if configured)
        // 4. Strict mode check (throws if enabled and nothing configured)
        // 5. Default (fallback)

        // OnGet with value
        stub.Port.OnGet(80);
        Assert.Equal(80, config.Port);

        // OnGet with callback overrides previous value
        stub.Port.OnGet(() => 443);
        Assert.Equal(443, config.Port);

        // OnGet with value overrides previous callback
        stub.Port.OnGet(8080);
        Assert.Equal(8080, config.Port);
    }
    #endregion
}

// =============================================================================
// Reset Behavior
// =============================================================================

public class ResetBehaviorTests
{
    #region troubleshoot-reset-value
    [Fact]
    public void Reset_ClearsTracking_ButPreservesConfiguration()
    {
        var stub = new ConfigSvcStub();
        IConfigSvc config = stub;

        // Configure value via OnGet
        stub.Host.OnGet("configured-host");

        // Access property to verify reads
        _ = config.Host;
        _ = config.Host;
        stub.Host.VerifyGet(Times.Exactly(2));

        // Reset clears tracking
        stub.Host.Reset();

        // Verify tracking was cleared
        stub.Host.VerifyGet(Times.Never);

        // OnGet configuration is preserved after Reset
        Assert.Equal("configured-host", config.Host);
    }

    [Fact]
    public void ManuallyClearing_OnGetConfiguration()
    {
        var stub = new ConfigSvcStub();

        // Configure with OnGet
        stub.Port.OnGet(8080);

        // To clear, reconfigure with default value
        stub.Port.OnGet(default(int));

        // Now returns default value
        IConfigSvc config = stub;
        Assert.Equal(0, config.Port);
    }
    #endregion
}

// =============================================================================
// Build Commands
// =============================================================================

#region troubleshoot-build-commands
// Rebuild to trigger source generator:
// dotnet build

// If issues persist, clean first:
// dotnet clean
// dotnet build
#endregion
