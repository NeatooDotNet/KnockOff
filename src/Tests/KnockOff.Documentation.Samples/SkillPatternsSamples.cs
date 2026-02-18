using KnockOff;
using KnockOff.Documentation.Samples; // For User, Product, Order types

namespace KnockOff.Documentation.Samples.SkillPatterns;

// =============================================================================
// Standalone Pattern Samples (for patterns.md)
// Uses "Pt" prefix on stub names to avoid conflicts with other sample files
// =============================================================================

#region skill-patterns-standalone-basic
public interface IUserRepository
{
    User? GetById(int id);
    void Save(User user);
}

[KnockOff]
public partial class PtUserRepositoryStub : IUserRepository { }
#endregion

public class StandalonePatternTests
{
    [Fact]
    public void StandaloneStub_BasicUsage()
    {
        #region skill-patterns-standalone-usage
        // Standalone: instantiate like any class, configure via Verify()
        var stub = new PtUserRepositoryStub();
        stub.GetById.Call((id) => new User { Id = id, Name = $"User{id}" }).Verifiable();
        stub.Save.Call((user) => { }).Verifiable();

        IUserRepository repo = stub;
        var user = repo.GetById(42);
        repo.Save(user!);

        stub.Verify();
        #endregion

        Assert.NotNull(user);
        Assert.Equal("User42", user.Name);
    }
}

#region skill-patterns-stub-overrides
[KnockOff]
public partial class PtUserRepositoryStubWithDefaults : IUserRepository
{
    // Override base class method with underscore suffix
    protected override User? GetById_(int id)
    {
        return new User { Id = id, Name = "Default User" };
    }
}
#endregion

public class StubOverridesTests
{
    [Fact]
    public void StubOverrides_ProvideDefaultImplementation()
    {
        var stub = new PtUserRepositoryStubWithDefaults();
        IUserRepository repo = stub;
        var user = repo.GetById(1);

        Assert.NotNull(user);
        Assert.Equal("Default User", user.Name);
    }
}

// =============================================================================
// Generic Standalone Pattern Samples
// =============================================================================

#region skill-patterns-generic-standalone-basic
public interface IRepository<T> where T : class
{
    T? GetById(int id);
    void Save(T entity);
    IEnumerable<T> GetAll();
}

[KnockOff]
public partial class PtRepositoryStub<T> : IRepository<T> where T : class { }
#endregion

public class GenericStandalonePatternTests
{
    [Fact]
    public void GenericStandaloneStub_MultipleTypeArguments()
    {
        #region skill-patterns-generic-standalone-usage
        // Generic Standalone: reusable across multiple type arguments
        var userRepo = new PtRepositoryStub<User>();
        userRepo.GetById.Call((id) => new User { Id = id, Name = "Test" }).Verifiable();
        userRepo.Save.Call((entity) => { }).Verifiable();

        var productRepo = new PtRepositoryStub<Product>();
        productRepo.GetById.Call((id) => new Product { Id = id, Name = "Widget" }).Verifiable();
        #endregion

        IRepository<User> userRepository = userRepo;
        var user = userRepository.GetById(1);
        userRepository.Save(user!);

        IRepository<Product> productRepository = productRepo;
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
// Standalone Class Pattern Samples
// =============================================================================

#region skill-patterns-standalone-class-basic
// Target class with virtual members
public abstract class ServiceBase
{
    public abstract void Initialize();
    public virtual string Name { get; set; } = "";
}

[KnockOffBase<ServiceBase>]
public partial class PtServiceStub { }
#endregion

public class StandaloneClassPatternTests
{
    [Fact]
    public void StandaloneClassStub_UsesObjectProperty()
    {
        #region skill-patterns-standalone-class-usage
        // Standalone Class: instantiate like any class, use .Object
        var stub = new PtServiceStub();
        stub.Initialize.Call(() => { }).Verifiable();
        stub.Name.Get(() => "TestService");

        ServiceBase service = stub.Object;  // Use .Object!
        service.Initialize();

        stub.Verify();
        #endregion

        Assert.Equal("TestService", service.Name);
    }
}

// =============================================================================
// Generic Standalone Class Pattern Samples
// =============================================================================

#region skill-patterns-generic-standalone-class-basic
public abstract class RepositoryBase<T> where T : class
{
    public abstract T? GetById(int id);
    public abstract void Save(T entity);
}

[KnockOffBase(typeof(RepositoryBase<>))]
public partial class PtRepositoryBaseStub<T> where T : class { }
#endregion

public class GenericStandaloneClassPatternTests
{
    [Fact]
    public void GenericStandaloneClassStub_UsesObjectProperty()
    {
        #region skill-patterns-generic-standalone-class-usage
        // Generic Standalone Class: reusable across multiple type arguments
        var userRepo = new PtRepositoryBaseStub<User>();
        userRepo.GetById.Call((id) => new User { Id = id, Name = "Test" }).Verifiable();
        userRepo.Save.Call((entity) => { }).Verifiable();

        RepositoryBase<User> repo = userRepo.Object;  // Use .Object!
        var user = repo.GetById(1);
        repo.Save(user!);

        userRepo.Verify();
        #endregion

        Assert.NotNull(user);
        Assert.Equal("Test", user.Name);
    }
}

// =============================================================================
// Inline Interface Pattern Samples
// =============================================================================

public interface IEmailService
{
    bool Send(string to, string subject);
}

#region skill-patterns-inline-interface-basic
[KnockOff<IEmailService>]
public partial class PtEmailServiceTests
{
    // The generator creates Stubs.IEmailService
}
#endregion

public partial class PtEmailServiceTests
{
    [Fact]
    public void InlineInterfaceStub_AccessViaStubsNamespace()
    {
        #region skill-patterns-inline-interface-usage
        // Inline Interface: access via Stubs namespace
        var stub = new Stubs.IEmailService();
        stub.Send.Call(_ => true).Verifiable();

        IEmailService email = stub;
        email.Send("test@example.com", "Hello");

        stub.Verify();
        #endregion

        Assert.True(true);
    }
}

// =============================================================================
// Inline Class Pattern Samples
// =============================================================================

#region skill-patterns-inline-class-basic
// Target class with virtual members
public class UserService
{
    public virtual User? GetUser(int id) => null;
    public virtual void SaveUser(User user) { }
    public virtual bool IsConnected { get; set; }
}

[KnockOff<UserService>]
public partial class PtUserServiceTests
{
    // The generator creates Stubs.UserService
}
#endregion

public partial class PtUserServiceTests
{
    [Fact]
    public void InlineClassStub_UsesObjectProperty()
    {
        #region skill-patterns-inline-class-usage
        // Inline Class: configure stub, use .Object for the class instance
        var stub = new Stubs.UserService();
        stub.GetUser.Call((id) => new User { Id = id, Name = "FromStub" }).Verifiable();

        UserService service = stub.Object;  // Use .Object!
        var user = service.GetUser(42);

        stub.Verify();
        #endregion

        Assert.NotNull(user);
        Assert.Equal("FromStub", user.Name);
    }
}

// =============================================================================
// Inline Delegate Pattern Samples
// =============================================================================

#region skill-patterns-inline-delegate-basic
// Define delegate types
public delegate bool ValidationRule(string value);
public delegate T Factory<T>();

[KnockOff<ValidationRule>]
[KnockOff<Factory<User>>]
public partial class PtDelegateTests
{
    // The generator creates Stubs.ValidationRule and Stubs.Factory
}
#endregion

public partial class PtDelegateTests
{
    [Fact]
    public void InlineDelegateStub_ImplicitConversion()
    {
        #region skill-patterns-inline-delegate-usage
        // Inline Delegate: configure via Interceptor, implicit conversion to delegate
        var ruleStub = new Stubs.ValidationRule();
        ruleStub.Interceptor.Call((value) => value != "invalid");

        ValidationRule rule = ruleStub;  // Implicit conversion
        bool isValid = rule("test");

        ruleStub.Interceptor.Verify(Called.Once);
        #endregion

        Assert.True(isValid);
    }
}

// =============================================================================
// Open Generic Interface Pattern Samples
// =============================================================================

public interface IService<T>
{
    T? GetItem(int id);
    void Process(T item);
}

#region skill-patterns-open-generic-interface-basic
[KnockOff(typeof(IService<>))]
public partial class PtOpenGenericTests
{
    // The generator creates Stubs.IService<T>
}
#endregion

public partial class PtOpenGenericTests
{
    [Fact]
    public void OpenGenericInterfaceStub_MultipleTypeArguments()
    {
        #region skill-patterns-open-generic-interface-usage
        // Open Generic Interface: instantiate with any type argument
        var userStub = new Stubs.IService<User>();
        userStub.GetItem.Call((id) => new User { Id = id, Name = "FromStub" }).Verifiable();

        var productStub = new Stubs.IService<Product>();
        productStub.GetItem.Call((id) => new Product { Id = id, Name = "FromStub" }).Verifiable();

        // The stub IS the interface implementation (no .Object needed)
        IService<User> userService = userStub;
        var user = userService.GetItem(1);

        userStub.Verify();
        #endregion

        Assert.NotNull(user);
        Assert.Equal("FromStub", user.Name);
    }
}

// =============================================================================
// Open Generic Class Pattern Samples
// =============================================================================

#region skill-patterns-open-generic-class-basic
public abstract class ServiceBaseGeneric<T>
{
    public abstract T? GetItem(int id);
    public abstract void Process(T item);
}

[KnockOff(typeof(ServiceBaseGeneric<>))]
public partial class PtOpenGenericClassTests
{
    // The generator creates Stubs.ServiceBaseGeneric<T>
}
#endregion

public partial class PtOpenGenericClassTests
{
    [Fact]
    public void OpenGenericClassStub_UsesObjectProperty()
    {
        #region skill-patterns-open-generic-class-usage
        // Open Generic Class: instantiate with any type argument, use .Object
        var userStub = new Stubs.ServiceBaseGeneric<User>();
        userStub.GetItem.Call((id) => new User { Id = id, Name = "FromStub" }).Verifiable();

        // IMPORTANT: .Object gives you the actual class instance
        ServiceBaseGeneric<User> service = userStub.Object;
        var user = service.GetItem(1);

        userStub.Verify();
        #endregion

        Assert.NotNull(user);
        Assert.Equal("FromStub", user.Name);
    }
}

// =============================================================================
// Complete Example - All Nine Patterns Together
// Uses separate host classes for different inline patterns to avoid generator issues
// =============================================================================

// Additional types for complete example
public interface IEmailSvc
{
    bool Send(string to, string subject, string body);
}

public interface INotifier<T>
{
    void Notify(T item);
}

public interface ILogger
{
    void Log(string message);
}

public abstract class AuditService
{
    public abstract void Audit(string action);
}

public interface IProcessor<T>
{
    void Process(T item);
}

// Standalone stubs (Patterns 1 and 2)
[KnockOff]
public partial class PtEmailSvcStub : IEmailSvc { }

[KnockOff]
public partial class PtNotifierStub<T> : INotifier<T> { }

// Host for inline interface/class stubs (Patterns 5, 6)
[KnockOff<ILogger>]
[KnockOff<AuditService>]
public partial class PtInlineHost { }

// Host for inline delegate stub (Pattern 7)
[KnockOff<ValidationRule>]
public partial class PtDelegateHost { }

// Host for open generic interface stub (Pattern 8)
[KnockOff(typeof(IProcessor<>))]
public partial class PtOpenGenericInterfaceHost { }

// Host for open generic class stub (Pattern 9)
[KnockOff(typeof(ServiceBaseGeneric<>))]
public partial class PtOpenGenericClassHost { }

public class AllNinePatternsTests
{
    [Fact]
    public void AllNinePatterns_WorkTogether()
    {
        #region skill-patterns-complete-example
        // 1. Standalone: direct instantiation
        var emailStub = new PtEmailSvcStub();
        emailStub.Send.Call(_ => true).Verifiable();
        IEmailSvc email = emailStub;

        // 2. Generic Standalone: reusable with type args
        var notifierStub = new PtNotifierStub<User>();
        notifierStub.Notify.Call((item) => { }).Verifiable();
        INotifier<User> notifier = notifierStub;

        // 3. Standalone Class: reusable class stub, uses .Object
        var cacheStub = new PtServiceStub();
        cacheStub.Initialize.Call(() => { }).Verifiable();
        cacheStub.Name.Get(() => "TestService");
        ServiceBase cache = cacheStub.Object;

        // 4. Generic Standalone Class: reusable generic class stub, uses .Object
        var repoStub = new PtRepositoryBaseStub<User>();
        repoStub.GetById.Call((id) => new User { Id = id }).Verifiable();
        RepositoryBase<User> repo = repoStub.Object;

        // 5. Inline Interface: via Stubs namespace
        var loggerStub = new PtInlineHost.Stubs.ILogger();
        loggerStub.Log.Call((msg) => { }).Verifiable();
        ILogger logger = loggerStub;

        // 6. Inline Class: use .Object for class instance
        var auditStub = new PtInlineHost.Stubs.AuditService();
        auditStub.Audit.Call((action) => { }).Verifiable();
        AuditService audit = auditStub.Object;

        // 7. Inline Delegate: implicit conversion
        var ruleStub = new PtDelegateHost.Stubs.ValidationRule();
        ruleStub.Interceptor.Call((value) => true);
        ValidationRule rule = ruleStub;

        // 8. Open Generic Interface: inline stub with type args
        var processorStub = new PtOpenGenericInterfaceHost.Stubs.IProcessor<Order>();
        processorStub.Process.Call((item) => { }).Verifiable();
        IProcessor<Order> processor = processorStub;

        // 9. Open Generic Class: inline stub with type args, uses .Object
        var serviceStub = new PtOpenGenericClassHost.Stubs.ServiceBaseGeneric<Order>();
        serviceStub.GetItem.Call((id) => new Order { Id = id }).Verifiable();
        ServiceBaseGeneric<Order> service = serviceStub.Object;  // .Object required for class patterns
        #endregion

        // Exercise all patterns
        var sent = email.Send("user@test.com", "Hello", "World");
        notifier.Notify(new User { Id = 1, Name = "Test" });
        cache.Initialize();
        var repoUser = repo.GetById(1);
        logger.Log("Starting operation");
        audit.Audit("email_sent");
        var isValid = rule("test");
        processor.Process(new Order { Id = 1 });
        var order = service.GetItem(42);
        logger.Log("Operation complete");

        Assert.True(sent);
        Assert.True(isValid);
        Assert.NotNull(repoUser);
        Assert.NotNull(order);
        Assert.Equal(42, order.Id);

        emailStub.Verify();
        notifierStub.Verify();
        cacheStub.Verify();
        repoStub.Verify();
        loggerStub.Verify();
        auditStub.Verify();
        processorStub.Verify();
        serviceStub.Verify();
    }
}
