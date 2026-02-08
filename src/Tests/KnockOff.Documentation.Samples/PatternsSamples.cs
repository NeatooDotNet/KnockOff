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

#region patterns-stub-overrides
[KnockOff]
public partial class UserRepoWithStubOverridesStub : IUserRepoStandalone
{
    // Override base class method with underscore suffix
    protected override User? GetById_(int id)
    {
        return new User { Id = id, Name = "Default User" };
    }
}
#endregion

public class StandalonePatternTests
{
    [Fact]
    public void StandaloneStub_CanBeConfiguredAndVerified()
    {
        #region patterns-standalone-usage
        // Stand-Alone: instantiate like any class, configure via Verify()
        var stub = new UserRepoStandaloneStub();
        stub.GetById.Return((id) => new User { Id = id, Name = $"User{id}" }).Verifiable();
        stub.Save.Call((user) => { }).Verifiable();
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
        stub.GetById.Return((id) => new User { Id = id, Name = "Test" }).Verifiable();
        stub.Save.Call((user) => { }).Verifiable();
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
        stub.GetUser.Return((id) => new User { Id = id, Name = "FromStub" }).Verifiable();
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
        ruleStub.Interceptor.Return((value) => value != "invalid");
        ValidationRule rule = ruleStub;
        #endregion

        bool result1 = rule("valid");
        bool result2 = rule("invalid");

        Assert.True(result1);
        Assert.False(result2);
        ruleStub.Interceptor.Verify(Called.Exactly(2));
        Assert.Equal("invalid", ruleStub.Interceptor.LastArg);
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
        userRepo.GetById.Return((id) => new User { Id = id, Name = "Test" }).Verifiable();
        userRepo.Save.Call((entity) => { }).Verifiable();

        var productRepo = new RepositoryGenericStub<Product>();
        productRepo.GetById.Return((id) => new Product { Id = id, Name = "Widget" }).Verifiable();
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
// Standalone Class Pattern (Pattern 3)
// =============================================================================

#region patterns-standalone-class-basic
public abstract class ServiceBaseNonGeneric
{
    public abstract string Name { get; }
    public abstract void Execute(string command);
}

[KnockOffBase<ServiceBaseNonGeneric>]
public partial class ServiceBaseStub { }
#endregion

public class StandaloneClassPatternTests
{
    [Fact]
    public void StandaloneClassStub_UsesObjectProperty()
    {
        #region patterns-standalone-class-usage
        // Standalone Class: configure stub, use .Object for the class instance
        var stub = new ServiceBaseStub();
        stub.Name.Get(() => "test").Verifiable();
        stub.Execute.Call((cmd) => { }).Verifiable();
        ServiceBaseNonGeneric service = stub.Object;
        #endregion

        Assert.Equal("test", service.Name);
        service.Execute("run");

        stub.Verify();
    }
}

// =============================================================================
// Generic Standalone Class Pattern (Pattern 4)
// =============================================================================

#region patterns-generic-standalone-class-basic
public abstract class RepositoryBase<T> where T : class
{
    public abstract T? GetItem(int id);
    public abstract void Save(T entity);
}

[KnockOffBase(typeof(RepositoryBase<>))]
public partial class RepositoryBaseStub<T> where T : class { }
#endregion

public class GenericStandaloneClassPatternTests
{
    [Fact]
    public void GenericStandaloneClassStub_UsesObjectProperty()
    {
        #region patterns-generic-standalone-class-usage
        // Generic Standalone Class: reusable across multiple type arguments, uses .Object
        var stub = new RepositoryBaseStub<User>();
        stub.GetItem.Return((id) => new User { Id = id, Name = "Test" }).Verifiable();
        stub.Save.Call((entity) => { }).Verifiable();
        RepositoryBase<User> service = stub.Object;
        #endregion

        var user = service.GetItem(42);
        service.Save(user!);

        Assert.NotNull(user);
        Assert.Equal("Test", user.Name);
        stub.Verify();
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
        userStub.GetItem.Return((id) => new User { Id = id, Name = "FromStub" }).Verifiable();

        var productStub = new Stubs.IServiceOpenGeneric<Product>();
        productStub.GetItem.Return((id) => new Product { Id = id, Name = "FromStub" }).Verifiable();
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
// Open Generic Class Pattern (Pattern 7)
// =============================================================================

#region patterns-open-generic-class-basic
public abstract class ServiceBaseOpenGeneric<T>
{
    public abstract T? GetItem(int id);
    public abstract void Process(T item);
}

[KnockOff(typeof(ServiceBaseOpenGeneric<>))]
public partial class OpenGenericClassTests
{
    // The generator creates Stubs.ServiceBaseOpenGeneric<T>
}
#endregion

public partial class OpenGenericClassTests
{
    [Fact]
    public void OpenGenericClassStub_UsesObjectProperty()
    {
        #region patterns-open-generic-class-usage
        // Open Generic Class: instantiate with any type argument, use .Object
        var userStub = new Stubs.ServiceBaseOpenGeneric<User>();
        userStub.GetItem.Return((id) => new User { Id = id, Name = "FromStub" }).Verifiable();

        // IMPORTANT: .Object gives you the actual class instance
        ServiceBaseOpenGeneric<User> service = userStub.Object;
        var user = service.GetItem(1);

        userStub.Verify();
        #endregion

        Assert.NotNull(user);
        Assert.Equal("FromStub", user.Name);
    }
}

// =============================================================================
// Complete Example - All Nine Patterns Together
// =============================================================================

public interface ILogSvc
{
    void Log(string message);
}

public abstract class AuditSvcBase
{
    public abstract void Audit(string action);
}

// Stand-alone stub for email service (Pattern 1)
public interface IEmailSvcPattern
{
    bool Send(string to, string subject, string body);
    bool IsConfigured { get; }
}

// Generic standalone stub for notifications (Pattern 2)
public interface INotifier<T>
{
    void Notify(T item);
}

// Standalone class stub (Pattern 3) - uses ServiceBaseStub from earlier in file

// Generic standalone class stub (Pattern 4) - uses RepositoryBaseStub from earlier in file

// Open generic interface for complete example (Pattern 8)
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

// Separate host for open generic interface stub
[KnockOff(typeof(IProcessor<>))]
public partial class CompleteExampleOpenGenericHost { }

// Open generic abstract class for pattern 9
public abstract class ServiceBase<T>
{
    public abstract T? GetItem(int id);
    public abstract void Process(T item);
}

// Separate host for open generic class stub
[KnockOff(typeof(ServiceBase<>))]
public partial class CompleteExampleOpenGenericClassHost { }

public class PatternComparisonTests
{
    [Fact]
    public void AllNinePatterns_WorkTogether()
    {
        #region patterns-complete-example-nine
        // STANDALONE PATTERNS (file-based, reusable across tests)

        // 1. Standalone Interface: direct instantiation, stub IS implementation
        var emailStub = new EmailSvcPatternStub();
        emailStub.Send.Return((to, subject, body) => true).Verifiable();
        IEmailSvcPattern email = emailStub;

        // 2. Generic Standalone Interface: reusable with type args
        var notifierStub = new NotifierStub<User>();
        notifierStub.Notify.Call((item) => { }).Verifiable();
        INotifier<User> notifier = notifierStub;

        // 3. Standalone Class: stub wraps instance, use .Object
        var serviceBaseStub = new ServiceBaseStub();
        serviceBaseStub.Name.Get(() => "TestService").Verifiable();
        serviceBaseStub.Execute.Call((cmd) => { }).Verifiable();
        ServiceBaseNonGeneric serviceBase = serviceBaseStub.Object;

        // 4. Generic Standalone Class: reusable class stub with type args
        var repoStub = new RepositoryBaseStub<Product>();
        repoStub.GetItem.Return((id) => new Product { Id = id, Name = "Widget" }).Verifiable();
        repoStub.Save.Call((entity) => { }).Verifiable();
        RepositoryBase<Product> repo = repoStub.Object;

        // INLINE PATTERNS (nested within test class)

        // 5. Inline Interface: via Stubs namespace
        var loggerStub = new CompleteExampleInlineHost.Stubs.ILogSvc();
        loggerStub.Log.Call((msg) => { }).Verifiable();
        ILogSvc logger = loggerStub;

        // 6. Inline Class: use .Object for class instance
        var auditStub = new CompleteExampleInlineHost.Stubs.AuditSvcBase();
        auditStub.Audit.Call((action) => { }).Verifiable();
        AuditSvcBase audit = auditStub.Object;

        // 7. Inline Delegate: implicit conversion
        var ruleStub = new InlineDelegateTests.Stubs.ValidationRule();
        ruleStub.Interceptor.Return((value) => true);
        ValidationRule rule = ruleStub;

        // 8. Open Generic Interface: inline stub with type args
        var processorStub = new CompleteExampleOpenGenericHost.Stubs.IProcessor<Order>();
        processorStub.Process.Call((item) => { }).Verifiable();
        IProcessor<Order> processor = processorStub;

        // 9. Open Generic Class: inline stub with type args, uses .Object
        var serviceStub = new CompleteExampleOpenGenericClassHost.Stubs.ServiceBase<Order>();
        serviceStub.GetItem.Return((id) => new Order { Id = id }).Verifiable();
        ServiceBase<Order> service = serviceStub.Object;
        #endregion

        // Exercise all patterns
        logger.Log("Starting operation");
        var sent = email.Send("user@test.com", "Hello", "World");
        notifier.Notify(new User { Id = 1, Name = "Test" });
        _ = serviceBase.Name;
        serviceBase.Execute("run");
        var product = repo.GetItem(1);
        repo.Save(product!);
        audit.Audit("email_sent");
        var isValid = rule("test");
        processor.Process(new Order { Id = 1 });
        var order = service.GetItem(42);
        logger.Log("Operation complete");

        Assert.True(sent);
        Assert.True(isValid);
        Assert.NotNull(product);
        Assert.NotNull(order);
        Assert.Equal(42, order.Id);
        emailStub.Verify();
        notifierStub.Verify();
        serviceBaseStub.Verify();
        repoStub.Verify();
        loggerStub.Verify();
        auditStub.Verify();
        processorStub.Verify();
        serviceStub.Verify();
    }
}
