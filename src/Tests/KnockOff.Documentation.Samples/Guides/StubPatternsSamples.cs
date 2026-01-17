/// <summary>
/// Code samples for docs/guides/stub-patterns.md
/// </summary>

namespace KnockOff.Documentation.Samples.Guides;

// ============================================================================
// Domain Types
// ============================================================================

public class SpUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class SpOrder
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
}

public interface ISpUserRepository
{
    SpUser? GetById(int id);
    IEnumerable<SpUser> GetAll();
    void Save(SpUser user);
}

public interface ISpEmailService
{
    void Send(string to, string body);
}

public interface ISpOrderRepository
{
    SpOrder? GetById(int id);
    void Save(SpOrder order);
}

public interface ISpPaymentService
{
    bool Process(decimal amount);
}

public interface ISpNotificationService
{
    void Notify(string message);
}

// Class to stub
public class SpEmailServiceClass
{
    public SpEmailServiceClass() { }
    public SpEmailServiceClass(string host, int port)
    {
        Host = host;
        Port = port;
    }

    public string Host { get; } = "localhost";
    public int Port { get; } = 25;

    public virtual void Send(string to, string body) { }
}

// Named delegate
public delegate bool SpValidationRule(string value);

// ============================================================================
// Standalone Stubs
// ============================================================================

#region stub-patterns-standalone-basic
[KnockOff]
public partial class SpUserRepositoryStub : ISpUserRepository { }
#endregion

#region stub-patterns-standalone-with-defaults
[KnockOff]
public partial class SpUserRepositoryWithDefaultsStub : ISpUserRepository
{
    protected SpUser? GetById(int id) => new SpUser { Id = id, Name = $"User-{id}" };
    protected IEnumerable<SpUser> GetAll() => [];
}
#endregion

// ============================================================================
// Sample Usage Classes
// ============================================================================

public static class StubPatternsSamples
{
    #region stub-patterns-standalone-usage
    public static void StandaloneUsage()
    {
        var stub = new SpUserRepositoryStub();
        stub.GetById.OnCall((ko, id) => new SpUser { Id = id });

        ISpUserRepository repo = stub;
        // or
        // var repo2 = stub.AsISpUserRepository(); // Alternative helper

        _ = repo;
    }
    #endregion
}

// ============================================================================
// Inline Stubs Test Class
// ============================================================================

#region stub-patterns-inline-basic
[KnockOff<ISpUserRepository>]
[KnockOff<ISpEmailService>]
public partial class SpUserServiceTests
{
    public void Test()
    {
        var repoStub = new Stubs.ISpUserRepository();
        var emailStub = new Stubs.ISpEmailService();

        repoStub.GetById.OnCall((ko, id) => new SpUser { Id = id });

        // var service = new UserService(repoStub.Object, emailStub.Object);
        _ = (repoStub, emailStub);
    }
}
#endregion

#region stub-patterns-inline-multiple
[KnockOff<ISpOrderRepository>]
[KnockOff<ISpPaymentService>]
[KnockOff<ISpNotificationService>]
public partial class SpOrderProcessorTests
{
    // Each generates a nested stub class in Stubs namespace
}
#endregion

// ============================================================================
// Class Stubs
// ============================================================================

#region stub-patterns-class-stub
[KnockOff<SpEmailServiceClass>]
public partial class SpNotificationTests
{
    public void Test()
    {
        var stub = new Stubs.SpEmailServiceClass();

        // Configure virtual methods (OnCall is a property for inline stubs)
        stub.Send.OnCall((ko, to, body) => { });

        // Use .Object to get the class instance
        SpEmailServiceClass service = stub.Object;

        // var notifier = new Notifier(service);
        _ = service;
    }
}
#endregion

#region stub-patterns-class-constructor
[KnockOff<SpEmailServiceClass>]
public partial class SpEmailServiceConstructorTests
{
    public void Test()
    {
        var stub = new Stubs.SpEmailServiceClass("smtp.test.com", 587);
        SpEmailServiceClass service = stub.Object;

        _ = service;
    }
}
#endregion

// ============================================================================
// Delegate Stubs
// ============================================================================

#region stub-patterns-delegate
[KnockOff<SpValidationRule>]
[KnockOff<Func<int, string>>]
[KnockOff<Action<string>>]
public partial class SpValidationTests
{
    public void Test()
    {
        // Named delegate
        var ruleStub = new Stubs.SpValidationRule();
        ruleStub.Interceptor.OnCall = (ko, value) => value.Length > 0;
        SpValidationRule rule = ruleStub;

        // Func<>
        var funcStub = new Stubs.Func();
        funcStub.Interceptor.OnCall = (ko, id) => $"Item-{id}";
        Func<int, string> func = funcStub;

        // Action<>
        var actionStub = new Stubs.Action();
        actionStub.Interceptor.OnCall = (ko, msg) => { /* captured */ };
        Action<string> action = actionStub;

        _ = (rule, func, action);
    }
}
#endregion

#region stub-patterns-delegate-implicit
[KnockOff<SpValidationRule>]
public partial class SpImplicitConversionTests
{
    public void Test()
    {
        var stub = new Stubs.SpValidationRule();
        SpValidationRule rule = stub;  // Implicit conversion

        var result = rule("test");  // Invokes through interceptor
        Assert.True(stub.Interceptor.WasCalled);

        _ = result;
    }
}
#endregion

// ============================================================================
// Nested Stubs
// ============================================================================

#region stub-patterns-nested
public partial class SpOrderTests  // Must be partial!
{
    [KnockOff]
    public partial class OrderRepositoryStub : ISpOrderRepository { }

    public void Test()
    {
        var stub = new OrderRepositoryStub();
        _ = stub;
    }
}
#endregion

// Minimal Assert for compilation
file static class Assert
{
    public static void True(bool condition) { }
}
