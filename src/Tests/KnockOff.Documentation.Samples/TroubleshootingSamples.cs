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
    string Host { get; }
    int Port { get; }
}

public class EmailService
{
    public virtual bool Send(string to, string subject) => false;
}

// A stub that demonstrates the correct pattern
[KnockOff]
public partial class TroubleshootRepoStub : ITroubleshootRepo { }

[KnockOff]
public partial class ConfigSvcStub : IConfigSvc { }

// Class stub for .Object property demonstration
[KnockOff<EmailService>]
public partial class TroubleshootEmailServiceTests { }

// =============================================================================
// Issue: Missing Partial Keyword
// =============================================================================

#region troubleshoot-partial
// ERROR: Without `partial`, generated code won't merge
// public class BadStub : IRepository { }  // CS0535: does not implement interface

// CORRECT: Add `partial` keyword
[KnockOff]
public partial class TroubleshootGoodStub : ITroubleshootRepo { }
#endregion

// =============================================================================
// Issue: Class Stubs Need .Object Property
// =============================================================================

public partial class TroubleshootEmailServiceTests
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
// Issue: OnCall Signature Must Match Method Parameters
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
// Using OnCall with Static Values
// =============================================================================

public class OnCallValueTests
{
    #region troubleshoot-oncall-value
    [Fact]
    public void OnCall_WithStaticValue()
    {
        var stub = new TroubleshootRepoStub();

        // Instead of: stub.GetById.OnCall((id) => new User { Id = id, Name = "Test" });
        // Use Returns(value) when the return value doesn't depend on parameters:
        stub.GetById.Returns(new User { Id = 999, Name = "Static User" });

        ITroubleshootRepo repository = stub;
        var user1 = repository.GetById(1);
        var user2 = repository.GetById(2);

        // Both calls return the same value
        Assert.Equal(999, user1?.Id);
        Assert.Equal(999, user2?.Id);
        Assert.Equal("Static User", user1?.Name);
    }
    #endregion
}

// =============================================================================
// Issue: No Callback Configured
// =============================================================================

public class NoCallbackTests
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
// Issue: OnGet Priority
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
        // 1. Sequence (if elevated via ThenGet() and not exhausted)
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
// Issue: Reset Behavior
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
// Build Commands (for documentation reference)
// =============================================================================

#region troubleshoot-build-commands
// Rebuild to trigger source generator:
// dotnet build

// If issues persist, clean first:
// dotnet clean
// dotnet build
#endregion

// =============================================================================
// Additional Troubleshooting Samples (preserved from original)
// =============================================================================

public interface IUserService
{
    User GetUser(int id, bool includeDeleted);
    Task<User?> GetUserAsync(int id);
    Task SaveAsync(User user);
}

public interface IFoo
{
    void DoWork();
}

public interface IBar
{
    void DoWork();
}

public interface IReadOnlyConfig
{
    string Version { get; }
}

public interface IReadWriteConfig
{
    string Version { get; set; }
}

// A service that depends on IUserService for verification tests
public class NotificationService
{
    private readonly IUserService _userService;

    public NotificationService(IUserService userService)
    {
        _userService = userService;
    }

    public async Task NotifyUser(int userId)
    {
        var user = await _userService.GetUserAsync(userId);
        if (user != null)
        {
            // Notification logic
        }
    }
}

[KnockOff]
public partial class UserServiceStub : IUserService { }

[KnockOff]
public partial class FooStub : IFoo { }

[KnockOff]
public partial class BarStub : IBar { }

[KnockOff]
public partial class ReadOnlyConfigStub : IReadOnlyConfig { }

[KnockOff]
public partial class ReadWriteConfigStub : IReadWriteConfig { }

// =============================================================================
// Additional Samples (used by skills/commands documentation)
// =============================================================================

#region troubleshoot-missing-partial-before
// ERROR: Without `partial`, you get CS0102 duplicate member errors
// public class MyStub : IUserService { }
#endregion

#region troubleshoot-missing-partial-after
// CORRECT: Add `partial` keyword to class declaration
[KnockOff]
public partial class CorrectUserServiceStub : IUserService { }
#endregion

public class OnCallSignatureAdditionalTests
{
    [Fact]
    public void OnCallSignature_WrongAndCorrect()
    {
        var stub = new UserServiceStub();

        #region troubleshoot-oncall-signature-wrong
        // Interface method: User GetUser(int id, bool includeDeleted)

        // ERROR: Wrong - no parameters (CS1593)
        // stub.GetUser.OnCall(() => new User());

        // ERROR: Wrong - only one parameter (CS1593)
        // stub.GetUser.OnCall((id) => new User());
        #endregion

        #region troubleshoot-oncall-signature-correct
        // CORRECT: Match all parameters from method signature
        stub.GetUser.OnCall((int id, bool includeDeleted) =>
            new User { Id = id, Name = includeDeleted ? "All" : "Active" });
        #endregion

        IUserService service = stub;
        var user = service.GetUser(1, false);
        Assert.NotNull(user);
        Assert.Equal(1, user.Id);
    }
}

// Class stub for .Object property demonstration (additional)
[KnockOff<EmailService>]
public partial class EmailServiceAdditionalTests { }

public partial class EmailServiceAdditionalTests
{
    #region troubleshoot-class-stub-wrong
    // When stubbing a CLASS (not interface), assignment fails:
    // var stub = new Stubs.EmailService();
    // EmailService service = stub;  // ERROR: Cannot convert Stubs.EmailService to EmailService
    #endregion

    #region troubleshoot-class-stub-correct
    [Fact]
    public void ClassStub_UseObjectProperty()
    {
        var stub = new Stubs.EmailService();
        stub.Send.OnCall((to, subject) => true);

        // Use .Object to get the typed instance
        EmailService service = stub.Object;

        var result = service.Send("test@example.com", "Hello");
        Assert.True(result);
    }
    #endregion
}

public class AsyncReturnTests
{
    [Fact]
    public void AsyncReturn_WrongAndCorrect()
    {
        var stub = new UserServiceStub();

        #region troubleshoot-async-return-wrong
        // Interface: Task<User?> GetUserAsync(int id)

        // ERROR: Returning unwrapped value (CS0029)
        // stub.GetUserAsync.OnCall((id) => new User());
        #endregion

        #region troubleshoot-async-return-correct
        // CORRECT: Return Task.FromResult for async methods
        stub.GetUserAsync.OnCall((int id) =>
            Task.FromResult<User?>(new User { Id = id }));

        // For Task (void async), use Task.CompletedTask:
        stub.SaveAsync.OnCall((user) => Task.CompletedTask);
        #endregion

        IUserService service = stub;
        var result = service.GetUserAsync(1).Result;
        Assert.NotNull(result);
    }
}

public class VerificationTests
{
    #region troubleshoot-verification-setup-order
    [Fact]
    public void Verification_SetupBeforeAct()
    {
        var stub = new UserServiceStub();

        // ARRANGE: Configure OnCall with Verifiable BEFORE acting
        stub.GetUserAsync.OnCall((id) =>
            Task.FromResult<User?>(new User { Id = id }))
            .Verifiable();

        IUserService service = stub;

        // ACT: Call the method
        service.GetUserAsync(42).Wait();

        // ASSERT: Verify the call was made
        stub.Verify();
    }
    #endregion

    #region troubleshoot-verification-same-instance
    [Fact]
    public void Verification_SameInstanceThroughout()
    {
        // Create stub once
        var stub = new UserServiceStub();

        // Configure the stub
        stub.GetUserAsync.OnCall((id) =>
            Task.FromResult<User?>(new User { Id = id }))
            .Verifiable();

        // Pass same stub to service constructor
        var service = new NotificationService(stub);

        // Act via the service (which uses the stub)
        service.NotifyUser(1).Wait();

        // Verify on the original stub instance
        stub.Verify();
    }
    #endregion
}

// Stub implementing multiple interfaces with same method name
public interface IBoth : IFoo, IBar { }

[KnockOff]
public partial class BothStub : IBoth { }

public class MultipleInterfaceTests
{
    #region troubleshoot-multiple-interfaces
    [Fact]
    public void MultipleInterfaces_SharedInterceptor()
    {
        var stub = new BothStub();

        // When multiple interfaces share the same method signature,
        // KnockOff generates a single shared interceptor
        stub.DoWork.OnCall(() => { }).Verifiable();

        // Calls through either interface use the same interceptor
        IFoo foo = stub;
        foo.DoWork();

        IBar bar = stub;
        bar.DoWork();

        // Verify tracks calls from both interfaces combined
        stub.DoWork.Verify(Times.Exactly(2));
    }
    #endregion
}

public class PropertySetterTests
{
    #region troubleshoot-property-readonly
    [Fact]
    public void Property_ReadOnlyVsReadWrite()
    {
        // Read-only property (get only in interface): Only OnGet available
        var readOnlyStub = new ReadOnlyConfigStub();
        readOnlyStub.Version.OnGet("1.0.0");

        IReadOnlyConfig readOnlyConfig = readOnlyStub;
        Assert.Equal("1.0.0", readOnlyConfig.Version);

        // Read-write property ({ get; set; } in interface): OnGet AND OnSet available
        var readWriteStub = new ReadWriteConfigStub();
        readWriteStub.Version.OnGet("2.0.0");

        IReadWriteConfig readWriteConfig = readWriteStub;

        // Read the property (triggers get)
        var version = readWriteConfig.Version;
        Assert.Equal("2.0.0", version);

        // Write the property (triggers set)
        readWriteConfig.Version = "3.0.0";

        // Can verify both get and set
        readWriteStub.Version.VerifyGet(Times.Once);
        readWriteStub.Version.VerifySet(Times.Once);
    }
    #endregion
}

#region troubleshoot-internals-visible-to
// In your SOURCE project (not test project), add to AssemblyInfo.cs or .csproj:
//
// AssemblyInfo.cs:
// [assembly: InternalsVisibleTo("YourTestProject")]
//
// Or in .csproj:
// <ItemGroup>
//   <InternalsVisibleToSuffix Include="YourTestProject" />
// </ItemGroup>
//
// Then internal interfaces can be stubbed in your test project:
// internal interface IInternalService { }
//
// [KnockOff]
// public partial class InternalServiceStub : IInternalService { }
#endregion
