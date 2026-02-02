using KnockOff;

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
public partial class UserRepoStandaloneStub : IUserRepoStandalone { }
#endregion

public class StandalonePatternTests
{
    [Fact]
    public void StandaloneStub_CanBeConfiguredAndVerified()
    {
        #region patterns-standalone-usage
        // Stand-Alone: instantiate like any class, configure via Verify()
        var stub = new UserRepoStandaloneStub();
        stub.GetById.OnCall((id) => new User { Id = id, Name = $"User{id}" }).Verifiable();
        stub.Save.OnCall((user) => { }).Verifiable();
        #endregion

        IUserRepoStandalone repository = stub;
        var user = repository.GetById(42);
        repository.Save(user!);

        Assert.NotNull(user);
        Assert.Equal("User42", user.Name);
        stub.Verify();
    }
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
    [Fact]
    public void InlineInterfaceStub_GeneratedInStubsNamespace()
    {
        #region patterns-inline-interface-usage
        // Inline Interface: access via Stubs namespace
        var stub = new Stubs.IUserRepoInline();
        stub.GetById.OnCall((id) => new User { Id = id, Name = "Test" }).Verifiable();
        stub.Save.OnCall((user) => { }).Verifiable();
        #endregion

        IUserRepoInline repository = stub;
        var user = repository.GetById(1);
        repository.Save(user!);

        Assert.NotNull(user);
        Assert.Equal("Test", user.Name);
        stub.Verify();
    }
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
    [Fact]
    public void InlineClassStub_UsesObjectProperty()
    {
        #region patterns-inline-class-usage
        // Inline Class: configure stub, use .Object for the class instance
        var stub = new Stubs.UserServiceClass();
        stub.GetUser.OnCall((id) => new User { Id = id, Name = "FromStub" }).Verifiable();
        UserServiceClass service = stub.Object;
        #endregion

        var user = service.GetUser(42);

        Assert.NotNull(user);
        Assert.Equal("FromStub", user.Name);
        stub.Verify();
    }
}

// =============================================================================
// Inline Delegate Pattern
// =============================================================================

#region patterns-inline-delegate-basic
// Define delegate types
public delegate bool ValidationRule(string value);
public delegate T Factory<T>();

[KnockOff<ValidationRule>]
[KnockOff<Factory<User>>]
public partial class InlineDelegateTests
{
    // The generator creates Stubs.ValidationRule and Stubs.Factory<User>
}
#endregion

public partial class InlineDelegateTests
{
    [Fact]
    public void InlineDelegateStub_TracksInvocationsAndConfiguresBehavior()
    {
        #region patterns-inline-delegate-usage
        // Inline Delegate: configure via Interceptor, implicit conversion to delegate
        var ruleStub = new Stubs.ValidationRule();
        ruleStub.Interceptor.OnCall((value) => value != "invalid");
        ValidationRule rule = ruleStub;
        #endregion

        bool result1 = rule("valid");
        bool result2 = rule("invalid");

        Assert.True(result1);
        Assert.False(result2);
        ruleStub.Interceptor.Verify(Times.Exactly(2));
        Assert.Equal("invalid", ruleStub.Interceptor.LastCallArg);
    }
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

[KnockOff<ILogSvc>]
[KnockOff<AuditSvcBase>]
public partial class PatternComparisonTests
{
    [Fact]
    public void AllThreePatterns_WorkTogether()
    {
        #region patterns-complete-example
        // Stand-Alone: direct instantiation
        var emailStub = new EmailSvcPatternStub();
        emailStub.Send.OnCall((to, subject, body) => true).Verifiable();

        // Inline Interface: via Stubs namespace
        var loggerStub = new Stubs.ILogSvc();
        loggerStub.Log.OnCall((msg) => { }).Verifiable();

        // Inline Class: use .Object for class instance
        var auditStub = new Stubs.AuditSvcBase();
        auditStub.Audit.OnCall((action) => { }).Verifiable();
        AuditSvcBase audit = auditStub.Object;
        #endregion

        IEmailSvcPattern email = emailStub;
        ILogSvc logger = loggerStub;
        var logMessages = new List<string>();

        logger.Log("Starting operation");
        var sent = email.Send("user@test.com", "Hello", "World");
        audit.Audit("email_sent");
        logger.Log("Operation complete");

        Assert.True(sent);
        emailStub.Verify();
        loggerStub.Verify();
        auditStub.Verify();
    }
}
