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
// Generic Standalone Pattern
// =============================================================================

#region patterns-generic-standalone-basic
public interface IRepositoryGeneric<T> where T : class
{
    T? GetById(int id);
    void Save(T entity);
    IEnumerable<T> GetAll();
}

[KnockOff]
public partial class RepositoryGenericStub<T> : IRepositoryGeneric<T> where T : class { }
#endregion

public class GenericStandalonePatternTests
{
    [Fact]
    public void GenericStandaloneStub_CanBeInstantiatedWithDifferentTypes()
    {
        #region patterns-generic-standalone-usage
        // Generic Standalone: reusable across multiple type arguments
        var userRepo = new RepositoryGenericStub<User>();
        userRepo.GetById.OnCall((id) => new User { Id = id, Name = "Test" }).Verifiable();
        userRepo.Save.OnCall((entity) => { }).Verifiable();

        var productRepo = new RepositoryGenericStub<Product>();
        productRepo.GetById.OnCall((id) => new Product { Id = id, Name = "Widget" }).Verifiable();
        #endregion

        IRepositoryGeneric<User> userRepository = userRepo;
        var user = userRepository.GetById(1);
        userRepository.Save(user!);

        IRepositoryGeneric<Product> productRepository = productRepo;
        var product = productRepository.GetById(42);

        Assert.NotNull(user);
        Assert.Equal("Test", user.Name);
        Assert.NotNull(product);
        Assert.Equal("Widget", product.Name);
        userRepo.Verify();
        productRepo.Verify();
    }
}

// =============================================================================
// Open Generic Pattern
// =============================================================================

public interface IServiceOpenGeneric<T>
{
    T? GetItem(int id);
    void Process(T item);
}

#region patterns-open-generic-basic
[KnockOff(typeof(IServiceOpenGeneric<>))]
public partial class OpenGenericTests
{
    // The generator creates Stubs.IServiceOpenGeneric<T>
}
#endregion

public partial class OpenGenericTests
{
    [Fact]
    public void OpenGenericStub_CanBeInstantiatedWithDifferentTypes()
    {
        #region patterns-open-generic-usage
        // Open Generic: instantiate with any type argument
        var userStub = new Stubs.IServiceOpenGeneric<User>();
        userStub.GetItem.OnCall((id) => new User { Id = id, Name = "FromStub" }).Verifiable();

        var productStub = new Stubs.IServiceOpenGeneric<Product>();
        productStub.GetItem.OnCall((id) => new Product { Id = id, Name = "FromStub" }).Verifiable();
        #endregion

        IServiceOpenGeneric<User> userService = userStub;
        var user = userService.GetItem(1);

        IServiceOpenGeneric<Product> productService = productStub;
        var product = productService.GetItem(42);

        Assert.NotNull(user);
        Assert.Equal("FromStub", user.Name);
        Assert.NotNull(product);
        Assert.Equal("FromStub", product.Name);
        userStub.Verify();
        productStub.Verify();
    }
}

// =============================================================================
// Complete Example - All Six Patterns Together
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

// Generic standalone stub for notifications
public interface INotifier<T>
{
    void Notify(T item);
}

// Open generic interface for complete example
public interface IProcessor<T>
{
    void Process(T item);
}

[KnockOff]
public partial class EmailSvcPatternStub : IEmailSvcPattern { }

[KnockOff]
public partial class NotifierStub<T> : INotifier<T> { }

// Host for inline interface and inline class stubs used in complete example
[KnockOff<ILogSvc>]
[KnockOff<AuditSvcBase>]
public partial class CompleteExampleInlineHost { }

// Separate host for open generic stub to avoid filename conflict
[KnockOff(typeof(IProcessor<>))]
public partial class CompleteExampleOpenGenericHost { }

public class PatternComparisonTests
{
    [Fact]
    public void AllSixPatterns_WorkTogether()
    {
        #region patterns-complete-example
        // 1. Standalone: direct instantiation
        var emailStub = new EmailSvcPatternStub();
        emailStub.Send.OnCall((to, subject, body) => true).Verifiable();

        // 2. Generic Standalone: reusable with type args
        var notifierStub = new NotifierStub<User>();
        notifierStub.Notify.OnCall((item) => { }).Verifiable();

        // 3. Inline Interface: via Stubs namespace
        var loggerStub = new CompleteExampleInlineHost.Stubs.ILogSvc();
        loggerStub.Log.OnCall((msg) => { }).Verifiable();

        // 4. Inline Class: use .Object for class instance
        var auditStub = new CompleteExampleInlineHost.Stubs.AuditSvcBase();
        auditStub.Audit.OnCall((action) => { }).Verifiable();
        AuditSvcBase audit = auditStub.Object;

        // 5. Inline Delegate: implicit conversion
        var ruleStub = new InlineDelegateTests.Stubs.ValidationRule();
        ruleStub.Interceptor.OnCall((value) => true);
        ValidationRule rule = ruleStub;

        // 6. Open Generic: inline stub with type args
        var processorStub = new CompleteExampleOpenGenericHost.Stubs.IProcessor<Order>();
        processorStub.Process.OnCall((item) => { }).Verifiable();
        #endregion

        IEmailSvcPattern email = emailStub;
        ILogSvc logger = loggerStub;
        INotifier<User> notifier = notifierStub;
        IProcessor<Order> processor = processorStub;

        logger.Log("Starting operation");
        var sent = email.Send("user@test.com", "Hello", "World");
        notifier.Notify(new User { Id = 1, Name = "Test" });
        audit.Audit("email_sent");
        var isValid = rule("test");
        processor.Process(new Order { Id = 1 });
        logger.Log("Operation complete");

        Assert.True(sent);
        Assert.True(isValid);
        emailStub.Verify();
        notifierStub.Verify();
        loggerStub.Verify();
        auditStub.Verify();
        processorStub.Verify();
    }
}
