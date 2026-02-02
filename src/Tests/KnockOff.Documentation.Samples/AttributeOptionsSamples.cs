using KnockOff;

namespace KnockOff.Documentation.Samples.AttributeOptions;

// =============================================================================
// Interfaces and Classes for Attribute Options Samples
// =============================================================================

public interface IAttrUserRepository
{
    User? GetById(int id);
    void Save(User user);
}

public interface IAttrEmailService
{
    void Send(string to, string subject, string body);
}

public interface IAttrLogger
{
    void Log(string message);
}

public class EmailServiceBase
{
    public virtual void Send(string to, string subject, string body)
    {
        // Default implementation
    }

    public virtual bool IsConfigured => true;
}

// =============================================================================
// Stand-Alone Pattern
// =============================================================================

#region attr-standalone
// Stand-alone pattern: [KnockOff] on a partial class implementing an interface
[KnockOff]
public partial class AttrUserRepositoryStub : IAttrUserRepository { }
#endregion

public class StandAlonePatternTests
{
    [Fact]
    public void StandAlone_CreatesStubDirectly()
    {
        // Stand-alone stub is created directly
        var stub = new AttrUserRepositoryStub();

        stub.GetById.OnCall((id) => new User { Id = id, Name = "Test User" });

        IAttrUserRepository repository = stub;
        var user = repository.GetById(42);

        Assert.NotNull(user);
        Assert.Equal("Test User", user.Name);
    }
}

// =============================================================================
// Inline Interface Pattern
// =============================================================================

#region attr-inline-interface
// Inline interface pattern: [KnockOff<IInterface>] generates stub in Stubs namespace
[KnockOff<IAttrUserRepository>]
public partial class InlineInterfacePatternTests { }

// Generated stub accessed via Stubs namespace
// var stub = new InlineInterfacePatternTests.Stubs.IAttrUserRepository();
#endregion

public partial class InlineInterfacePatternTests
{
    [Fact]
    public void InlineInterface_GeneratesStubInStubsNamespace()
    {
        var stub = new Stubs.IAttrUserRepository();

        stub.GetById.OnCall((id) => new User { Id = id, Name = "Inline User" });

        IAttrUserRepository repository = stub;
        var user = repository.GetById(1);

        Assert.NotNull(user);
        Assert.Equal("Inline User", user.Name);
    }
}

// =============================================================================
// Inline Class Pattern
// =============================================================================

#region attr-inline-class
// Inline class pattern: [KnockOff<ConcreteClass>] generates stub inheriting from class
[KnockOff<EmailServiceBase>]
public partial class InlineClassPatternTests { }

// Generated stub inherits from EmailServiceBase
// var stub = new InlineClassPatternTests.Stubs.EmailServiceBase();
// EmailServiceBase service = stub.Object;  // Cast to base class type
#endregion

public partial class InlineClassPatternTests
{
    [Fact]
    public void InlineClass_ProvidesObjectProperty()
    {
        var stub = new Stubs.EmailServiceBase();

        EmailServiceBase service = stub.Object;

        stub.Send.OnCall((to, subject, body) => { }).Verifiable();

        service.Send("test@example.com", "Hello", "World");

        stub.Verify();
    }
}

// =============================================================================
// Multiple Stubs Pattern
// =============================================================================

#region attr-multiple
// Multiple attributes generate independent stubs in the Stubs namespace
[KnockOff<IAttrUserRepository>]
[KnockOff<IAttrEmailService>]
[KnockOff<IAttrLogger>]
public partial class MultipleStubsPatternTests { }

// Each interface gets its own stub in the Stubs namespace
// var userRepo = new MultipleStubsPatternTests.Stubs.IAttrUserRepository();
// var emailService = new MultipleStubsPatternTests.Stubs.IAttrEmailService();
// var logger = new MultipleStubsPatternTests.Stubs.IAttrLogger();
#endregion

public partial class MultipleStubsPatternTests
{
    [Fact]
    public void MultipleStubs_GeneratesEachInStubsNamespace()
    {
        var userRepo = new Stubs.IAttrUserRepository();
        var emailService = new Stubs.IAttrEmailService();
        var logger = new Stubs.IAttrLogger();

        userRepo.GetById.OnCall((id) => new User { Id = id, Name = "Test" }).Verifiable();
        emailService.Send.OnCall((to, subject, body) => { }).Verifiable();
        logger.Log.OnCall((message) => { }).Verifiable();

        IAttrUserRepository repo = userRepo;
        IAttrEmailService email = emailService;
        IAttrLogger log = logger;

        repo.GetById(1);
        email.Send("a@b.com", "Subject", "Body");
        log.Log("Test message");

        userRepo.Verify();
        emailService.Verify();
        logger.Verify();
    }
}

// =============================================================================
// Complete Example: Choosing a Pattern
// =============================================================================

public class ChoosingPatternTests
{
    [Fact]
    public void AllPatterns_SupportSameInterceptorAPI()
    {
        // Stand-alone pattern
        var standalone = new AttrUserRepositoryStub();
        var saveTracking = standalone.Save.OnCall((user) => { }).Verifiable();

        // All patterns have the same interceptor capabilities:
        // - OnCall for behavior
        // - Verifiable() for batch verification
        // - Verify() to check all marked members
        // - LastArg on tracking interface for argument capture

        IAttrUserRepository repo = standalone;
        repo.Save(new User { Id = 1, Name = "Alice" });

        // Verify using the new API
        standalone.Verify();
        Assert.Equal("Alice", saveTracking.LastArg.Name);
    }
}
