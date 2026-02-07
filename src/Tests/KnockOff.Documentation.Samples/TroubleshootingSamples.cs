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
    [Fact]
    public void ClassStub_RequiresObjectProperty()
    {
        var stub = new Stubs.EmailService();
        stub.Send.Return((to, subject) => true);

        #region troubleshoot-object
        // Use .Object to get the typed instance for class stubs
        EmailService service = stub.Object;
        #endregion

        var result = service.Send("test@example.com", "Hello");
        Assert.True(result);
    }

    private void UseEmailService(EmailService service)
    {
        service.Send("a@b.com", "Test");
    }

    [Fact]
    public void PassingStubObjectToMethod()
    {
        var stub = new Stubs.EmailService();
        stub.Send.Return((to, subject) => true);
        UseEmailService(stub.Object);
        stub.Send.Verify();
    }
}

// =============================================================================
// Issue: Return Signature Must Match Method Parameters
// =============================================================================

public class ReturnSignatureTests
{
    [Fact]
    public void ReturnSignature_MustMatchParameters()
    {
        var stub = new TroubleshootRepoStub();

        #region troubleshoot-oncall-signature
        // Return signature must match method parameters exactly
        stub.GetByIdAsync.Return((int id) =>
            Task.FromResult<User?>(new User { Id = id, Name = "Test" }));
        #endregion

        ITroubleshootRepo repository = stub;
        var user = repository.GetByIdAsync(42).Result;

        Assert.NotNull(user);
        Assert.Equal(42, user.Id);
    }
}

// =============================================================================
// Using Return with Static Values
// =============================================================================

public class ReturnValueTests
{
    [Fact]
    public void Return_WithStaticValue()
    {
        var stub = new TroubleshootRepoStub();

        #region troubleshoot-oncall-value
        // Use Return(value) when the return doesn't depend on parameters
        stub.GetById.Return(new User { Id = 999, Name = "Static User" });
        #endregion

        ITroubleshootRepo repository = stub;
        var user1 = repository.GetById(1);
        var user2 = repository.GetById(2);

        Assert.Equal(999, user1?.Id);
        Assert.Equal(999, user2?.Id);
        Assert.Equal("Static User", user1?.Name);
    }
}

// =============================================================================
// Issue: No Callback Configured
// =============================================================================

public class NoCallbackTests
{
    [Fact]
    public void MethodWithoutCallback_UsesSmartDefaults()
    {
        var stub = new TroubleshootRepoStub();
        ITroubleshootRepo repository = stub;

        // Nullable User? returns null by default
        var user = repository.GetById(1);
        Assert.Null(user);

        #region troubleshoot-no-callback
        // Configure required (non-nullable) return values explicitly
        stub.GetName.Return(() => "Configured Name");
        #endregion

        var name = repository.GetName();
        Assert.Equal("Configured Name", name);
    }

    [Fact]
    public void FixOptions_ForRequiredReturnValues()
    {
        var stub = new ConfigSvcStub();
        IConfigSvc config = stub;

        stub.Host.OnGet("localhost");
        Assert.Equal("localhost", config.Host);

        stub.Port.OnGet(() => 8080);
        Assert.Equal(8080, config.Port);
    }
}

// =============================================================================
// Issue: OnGet Priority
// =============================================================================

public class OnGetPriorityTests
{
    [Fact]
    public void OnGet_MostRecentTakesPrecedence()
    {
        var stub = new ConfigSvcStub();
        IConfigSvc config = stub;

        stub.Host.OnGet(() => "from-callback");
        Assert.Equal("from-callback", config.Host);

        #region troubleshoot-onget-priority
        // Most recent OnGet configuration wins
        stub.Host.OnGet("from-value");
        #endregion

        Assert.Equal("from-value", config.Host);

        stub.Host.OnGet(() => "back-to-callback");
        Assert.Equal("back-to-callback", config.Host);
    }

    [Fact]
    public void Understanding_Property_Priority()
    {
        var stub = new ConfigSvcStub();
        IConfigSvc config = stub;

        stub.Port.OnGet(80);
        Assert.Equal(80, config.Port);

        stub.Port.OnGet(() => 443);
        Assert.Equal(443, config.Port);

        stub.Port.OnGet(8080);
        Assert.Equal(8080, config.Port);
    }
}

// =============================================================================
// Issue: Reset Behavior
// =============================================================================

public class ResetBehaviorTests
{
    [Fact]
    public void Reset_ClearsTracking_ButPreservesConfiguration()
    {
        var stub = new ConfigSvcStub();
        IConfigSvc config = stub;

        stub.Host.OnGet("configured-host");

        _ = config.Host;
        _ = config.Host;
        stub.Host.VerifyGet(Times.Exactly(2));

        #region troubleshoot-reset-value
        // Reset() clears tracking but preserves OnGet configuration
        stub.Host.Reset();
        stub.Host.VerifyGet(Times.Never);  // Tracking cleared
        Assert.Equal("configured-host", config.Host);  // Config preserved
        #endregion
    }

    [Fact]
    public void ManuallyClearing_OnGetConfiguration()
    {
        var stub = new ConfigSvcStub();

        stub.Port.OnGet(8080);

        stub.Port.OnGet(default(int));

        IConfigSvc config = stub;
        Assert.Equal(0, config.Port);
    }
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

public class ReturnSignatureAdditionalTests
{
    [Fact]
    public void ReturnSignature_WrongAndCorrect()
    {
        var stub = new UserServiceStub();

        #region troubleshoot-oncall-signature-wrong
        // Interface method: User GetUser(int id, bool includeDeleted)

        // ERROR: Wrong - no parameters (CS1593)
        // stub.GetUser.Return(() => new User());

        // ERROR: Wrong - only one parameter (CS1593)
        // stub.GetUser.Return((id) => new User());
        #endregion

        #region troubleshoot-oncall-signature-correct
        // CORRECT: Match all parameters from method signature
        stub.GetUser.Return((int id, bool includeDeleted) =>
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
        stub.Send.Return((to, subject) => true);

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
        // stub.GetUserAsync.Return((id) => new User());
        #endregion

        #region troubleshoot-async-return-correct
        // CORRECT: Return Task.FromResult for async methods
        stub.GetUserAsync.Return((int id) =>
            Task.FromResult<User?>(new User { Id = id }));

        // For Task (void async), use Task.CompletedTask:
        stub.SaveAsync.Call((user) => { });
        #endregion

        #region troubleshoot-async-simpler-alternatives
        // Return() auto-wraps in Task.FromResult
        stub.GetUserAsync.Return(new User { Id = 1, Name = "Alice" });

        // Simplified Return also auto-wraps
        stub.GetUserAsync.Return((id) => new User { Id = id });
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

        // ARRANGE: Configure Return with Verifiable BEFORE acting
        stub.GetUserAsync.Return((id) =>
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
        stub.GetUserAsync.Return((id) =>
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
        stub.DoWork.Call(() => { }).Verifiable();

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

// =============================================================================
// Delegate Stubs: Named Type Requirement
// =============================================================================

#region troubleshoot-delegate-named-type
// Does NOT work:
// [KnockOff<Func<int, int, int>>]  // Compiler error

// Define a named delegate:
public delegate int CalcOperation(int a, int b);

// Then use it:
[KnockOff<CalcOperation>]
public partial class CalcDelegateTests { }
#endregion

// =============================================================================
// Delegate Stubs: Interceptor Pattern Comparison
// =============================================================================

public partial class CalcDelegateTests
{
    [Fact]
    public void DelegateStub_UsesInterceptorProperty()
    {
        var interfaceStub = new TroubleshootRepoStub();
        var delegateStub = new Stubs.CalcOperation();
        var user = new User { Id = 1, Name = "Test" };

        #region troubleshoot-delegate-interceptor-pattern
        // Interface stub pattern:
        interfaceStub.GetById.Return((id) => user);

        // Delegate stub pattern (different!):
        delegateStub.Interceptor.Return((a, b) => a + b);
        delegateStub.Interceptor.Return(42);
        #endregion

        CalcOperation op = delegateStub;
        Assert.Equal(42, op(1, 2));
        delegateStub.Interceptor.Verify(Times.Once);
    }

    [Fact]
    public void DelegateReturn_SignatureMustMatch()
    {
        var stub = new Stubs.CalcOperation();

        #region troubleshoot-delegate-oncall-wrong
        // Delegate: int CalcOperation(int a, int b)

        // Wrong: missing parameter
        // stub.Interceptor.Return((a) => a);

        // Wrong: wrong parameter type
        // stub.Interceptor.Return((string a, string b) => 0);
        #endregion

        #region troubleshoot-delegate-oncall-correct
        // Correct: matches delegate signature
        stub.Interceptor.Return((int a, int b) => a + b);
        // Or with inferred types:
        stub.Interceptor.Return((a, b) => a + b);
        #endregion

        CalcOperation op = stub;
        Assert.Equal(5, op(2, 3));
    }
}

// =============================================================================
// KO0200: Standalone Stub Cannot Have Base Class
// =============================================================================

#region troubleshoot-ko0200-error
// This pattern produces diagnostic KO0200:
// public class MyBaseClass { }
//
// [KnockOff]
// public partial class MyStub : MyBaseClass, IMyService { }  // ERROR: KO0200
#endregion

// =============================================================================
// User Methods: Override Virtual Methods for Default Behavior
// =============================================================================

#region troubleshoot-user-method-definition
public interface ITroubleshootUserRepo
{
    User? GetById(int id);
}

[KnockOff]
public partial class TroubleshootUserMethodStub : ITroubleshootUserRepo
{
    // Override the generated virtual method with underscore suffix
    protected override User? GetById_(int id)
    {
        return new User { Id = id, Name = "Default User" };
    }
}
#endregion

public class UserMethodExampleTests
{
    [Fact]
    public void UserMethod_ProvidesDefaultBehavior()
    {
        #region troubleshoot-user-method-usage
        var stub = new TroubleshootUserMethodStub();
        // Calls your GetById_ override by default
        ITroubleshootUserRepo repo = stub;
        var user = repo.GetById(123);  // Returns User { Id = 123, Name = "Default User" }

        // You can still override per-test with Return
        stub.GetById.Return(id => new User { Id = id, Name = "Test User" });
        #endregion

        Assert.Equal("Default User", user!.Name);
        var overridden = repo.GetById(456);
        Assert.Equal("Test User", overridden!.Name);
    }
}

// =============================================================================
// Inline Alternative for KO0200
// =============================================================================

public interface ITroubleshootMyService
{
    void DoSomething();
}

#region troubleshoot-inline-alternative
[KnockOff<ITroubleshootMyService>]
public partial class TroubleshootInlineContainer { }
#endregion
