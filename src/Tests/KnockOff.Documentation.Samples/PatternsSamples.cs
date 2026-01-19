namespace KnockOff.Documentation.Samples.Patterns;

// =============================================================================
// Stand-Alone / Flat Pattern
// =============================================================================

#region patterns-standalone-basic
public interface IUserRepoStandalone
{
    User? GetById(int id);
    void Save(User user);
}

[KnockOff]
public partial class UserRepoStandaloneStub : IUserRepoStandalone
{
    // Optionally add user methods for default behavior
    protected User? GetById(int id) => new User { Id = id, Name = $"User{id}" };
}
#endregion

public class StandalonePatternTests
{
    #region patterns-standalone-usage
    [Fact]
    public void StandaloneStub_CanBeConfiguredAndVerified()
    {
        // Arrange - instantiate the reusable stub
        var stub = new UserRepoStandaloneStub();

        // Configure void method via OnCall
        var saveTracking = stub.Save.OnCall((ko, user) => { });

        // Act - cast to interface for use
        IUserRepoStandalone repository = stub;
        var user = repository.GetById(42);
        repository.Save(user!);

        // Assert - verify via tracking
        Assert.NotNull(user);
        Assert.True(saveTracking.WasCalled);
        // User method tracks via special interceptor
        Assert.Equal(1, stub.GetById2.CallCount);
    }
    #endregion
}

// =============================================================================
// Inline Interface Pattern
// =============================================================================

public interface IUserRepoInline
{
    User? GetById(int id);
    void Save(User user);
}

#region patterns-inline-interface-basic
[KnockOff<IUserRepoInline>]
public partial class InlineInterfaceTests
{
    // The generator creates Stubs.IUserRepoInline
}
#endregion

public partial class InlineInterfaceTests
{
    #region patterns-inline-interface-usage
    [Fact]
    public void InlineInterfaceStub_GeneratedInStubsNamespace()
    {
        // Arrange - use generated Stubs.InterfaceName class
        var stub = new Stubs.IUserRepoInline();

        // Configure behavior
        var getByIdTracking = stub.GetById.OnCall((ko, id) => new User { Id = id, Name = "Test" });
        var saveTracking = stub.Save.OnCall((ko, user) => { });

        // Act
        IUserRepoInline repository = stub;
        var user = repository.GetById(1);
        repository.Save(user!);

        // Assert
        Assert.NotNull(user);
        Assert.Equal("Test", user.Name);
        Assert.True(saveTracking.WasCalled);
    }
    #endregion
}

// =============================================================================
// Inline Class Pattern
// =============================================================================

#region patterns-inline-class-basic
// Target class with virtual members
public class UserServiceClass
{
    public virtual User? GetUser(int id) => null;
    public virtual void SaveUser(User user) { }
    public virtual bool IsConnected { get; set; }
}

[KnockOff<UserServiceClass>]
public partial class InlineClassTests
{
    // The generator creates Stubs.UserServiceClass
}
#endregion

public partial class InlineClassTests
{
    #region patterns-inline-class-usage
    [Fact]
    public void InlineClassStub_UsesObjectProperty()
    {
        // Arrange - create wrapper stub
        var stub = new Stubs.UserServiceClass();

        // Configure virtual member behavior
        var getTracking = stub.GetUser.OnCall((ko, id) => new User { Id = id, Name = "FromStub" });

        // Act - use .Object to get the actual class instance
        UserServiceClass service = stub.Object;
        var user = service.GetUser(42);

        // Assert
        Assert.NotNull(user);
        Assert.Equal("FromStub", user.Name);
        Assert.True(getTracking.WasCalled);
    }
    #endregion
}

// =============================================================================
// Complete Example - All Three Patterns Together
// =============================================================================

public interface ILogSvc
{
    void Log(string message);
}

public abstract class AuditSvcBase
{
    public abstract void Audit(string action);
}

// Stand-alone stub for email service
public interface IEmailSvcPattern
{
    bool Send(string to, string subject, string body);
    bool IsConfigured { get; }
}

[KnockOff]
public partial class EmailSvcPatternStub : IEmailSvcPattern { }

#region patterns-complete-example
[KnockOff<ILogSvc>]
[KnockOff<AuditSvcBase>]
public partial class PatternComparisonTests
{
    [Fact]
    public void AllThreePatterns_WorkTogether()
    {
        // Stand-Alone: Reusable email stub
        var emailStub = new EmailSvcPatternStub();
        var sendTracking = emailStub.Send.OnCall((ko, to, subject, body) => true);
        emailStub.IsConfigured.Value = true;

        // Inline Interface: Test-local logger stub
        var loggerStub = new Stubs.ILogSvc();
        var logMessages = new List<string>();
        var logTracking = loggerStub.Log.OnCall((ko, msg) => logMessages.Add(msg));

        // Inline Class: Stub for abstract base class
        var auditStub = new Stubs.AuditSvcBase();
        var auditTracking = auditStub.Audit.OnCall((ko, action) => { });

        // Act - simulate integration scenario
        IEmailSvcPattern email = emailStub;
        ILogSvc logger = loggerStub;
        AuditSvcBase audit = auditStub.Object;

        logger.Log("Starting operation");
        var sent = email.Send("user@test.com", "Hello", "World");
        audit.Audit("email_sent");
        logger.Log("Operation complete");

        // Assert - each pattern provides verification
        Assert.True(sent);
        Assert.Equal(2, logTracking.CallCount);
        Assert.True(auditTracking.WasCalled);
        Assert.Contains("Starting operation", logMessages);
    }
}
#endregion
