/// <summary>
/// Code samples for docs/reference/attributes.md
/// </summary>

namespace KnockOff.Documentation.Samples.Reference;

// ============================================================================
// Domain Types
// ============================================================================

public class AttrUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public interface IAttrService
{
    void DoWork();
}

public interface IAttrUserRepository
{
    AttrUser? GetById(int id);
}

public interface IAttrEmailService
{
    void Send(string to, string body);
}

public interface IAttrRepository<T>
{
    T? GetById(int id);
}

public delegate bool AttrValidationRule(string value);

// Class to stub
public class AttrEmailServiceClass
{
    public AttrEmailServiceClass() { }
    public AttrEmailServiceClass(string host, int port)
    {
        Host = host;
        Port = port;
    }

    public string Host { get; } = "localhost";
    public int Port { get; } = 25;

    public virtual void Send(string to, string body) { }
}

// ============================================================================
// Standalone Stubs
// ============================================================================

#region attr-standalone-basic
[KnockOff]
public partial class AttrUserRepositoryStub : IAttrUserRepository { }
#endregion

#region attr-standalone-valid
// Basic usage
[KnockOff]
public partial class AttrServiceStub : IAttrService { }

// Generic interface (with concrete type)
[KnockOff]
public partial class AttrUserRepoStub : IAttrRepository<AttrUser> { }

// Internal visibility
[KnockOff]
internal partial class AttrInternalServiceStub : IAttrService { }

// Nested in test class
public partial class AttrMyTests
{
    [KnockOff]
    public partial class NestedStub : IAttrService { }
}
#endregion

// ============================================================================
// Inline Stubs Test Classes
// ============================================================================

#region attr-inline-usage
[KnockOff<IAttrUserRepository>]
[KnockOff<IAttrEmailService>]
public partial class AttrUserServiceTests
{
    public void Test()
    {
        var repoStub = new Stubs.IAttrUserRepository();
        var emailStub = new Stubs.IAttrEmailService();

        _ = (repoStub, emailStub);
    }
}
#endregion

#region attr-inline-interface
[KnockOff<IAttrUserRepository>]
public partial class AttrInterfaceTests
{
    public void Test()
    {
        var stub = new Stubs.IAttrUserRepository();
        stub.GetById.OnCall((ko, id) => new AttrUser { Id = id });

        IAttrUserRepository repo = stub;  // Implicit conversion

        _ = repo;
    }
}
#endregion

#region attr-inline-class
[KnockOff<AttrEmailServiceClass>]
public partial class AttrClassTests
{
    public void Test()
    {
        var stub = new Stubs.AttrEmailServiceClass("smtp.test.com", 587);
        stub.Send.OnCall((ko, to, body) => { });

        AttrEmailServiceClass service = stub.Object;  // Use .Object for class instance

        _ = service;
    }
}
#endregion

#region attr-inline-delegate
[KnockOff<Func<int, string>>]
[KnockOff<AttrValidationRule>]  // Named delegate
public partial class AttrDelegateTests
{
    public void Test()
    {
        var funcStub = new Stubs.Func();
        funcStub.Interceptor.OnCall = (ko, id) => $"Item-{id}";

        Func<int, string> func = funcStub;  // Implicit conversion

        _ = func;
    }
}
#endregion

#region attr-namespace-qualified
[KnockOff.KnockOff]
public partial class AttrFullyQualifiedStub : IAttrService { }

[KnockOff.KnockOff<IAttrService>]
public partial class AttrFullyQualifiedTests { }
#endregion
