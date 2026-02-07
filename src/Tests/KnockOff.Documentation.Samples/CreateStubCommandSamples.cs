using KnockOff;

namespace KnockOff.Documentation.Samples.CreateStubCommand;

// =============================================================================
// Interfaces and Classes for Create Stub Command Samples
// =============================================================================

public interface IDataService
{
    User? GetById(int id);
    bool Save(User user);
}

public class DataServiceClass
{
    public virtual User? GetUser(int id) => null;
    public virtual bool SaveUser(User user) => false;
}

// =============================================================================
// Stand-Alone Pattern Template
// =============================================================================

#region command-create-stub-standalone-pattern
[KnockOff]
public partial class DataServiceStub : IDataService { }
#endregion

// =============================================================================
// Stand-Alone Pattern Usage
// =============================================================================

public class StandAlonePatternTests
{
    #region command-create-stub-standalone-usage
    [Fact]
    public void StandAloneStub_Usage()
    {
        // Instantiate the stub
        var stub = new DataServiceStub();

        // Configure return value
        stub.GetById.Returns((id) => new User { Id = id, Name = "Test" });

        // Use through interface
        IDataService service = stub;
        var user = service.GetById(42);

        Assert.Equal("Test", user!.Name);
    }
    #endregion
}

// =============================================================================
// Inline Interface Pattern
// =============================================================================

public interface INotificationService
{
    void Notify(string message);
    bool IsEnabled { get; }
}

#region command-create-stub-inline-interface-pattern
[KnockOff<INotificationService>]
public partial class CmdInlineInterfaceTests { }
#endregion

public partial class CmdInlineInterfaceTests
{
    #region command-create-stub-inline-interface-usage
    [Fact]
    public void InlineInterfaceStub_Usage()
    {
        // Instantiate via Stubs namespace
        var stub = new Stubs.INotificationService();

        // Configure behavior
        stub.Notify.Execute((msg) => { });
        stub.IsEnabled.Get(true);

        // Use through interface
        INotificationService service = stub;
        service.Notify("Hello");

        Assert.True(service.IsEnabled);
    }
    #endregion
}

// =============================================================================
// Inline Class Pattern
// =============================================================================

public class PaymentProcessor
{
    public virtual bool ProcessPayment(decimal amount) => false;
    public virtual string Status { get; set; } = "Unknown";
}

#region command-create-stub-inline-class-pattern
[KnockOff<PaymentProcessor>]
public partial class CmdInlineClassTests { }
#endregion

public partial class CmdInlineClassTests
{
    #region command-create-stub-inline-class-usage
    [Fact]
    public void InlineClassStub_Usage()
    {
        // Instantiate via Stubs namespace
        var stub = new Stubs.PaymentProcessor();

        // Configure virtual member
        stub.ProcessPayment.Returns((amount) => amount > 0);

        // Access class instance via .Object
        PaymentProcessor processor = stub.Object;
        var result = processor.ProcessPayment(100m);

        Assert.True(result);
    }
    #endregion
}

// =============================================================================
// User Methods for Stand-Alone Stubs
// =============================================================================

public interface IUserRepoCmd
{
    User? FindById(int id);
    IEnumerable<User> FindAll();
    bool Save(User user);
}

[KnockOff]
public partial class UserRepoCmdStub : IUserRepoCmd { }

#region command-create-stub-user-methods
public partial class UserRepoCmdStub
{
    // Override the generated virtual method (with _ suffix) to provide default test data
    protected override IEnumerable<User> FindAll_()
    {
        return new[]
        {
            new User { Id = 1, Name = "Alice" },
            new User { Id = 2, Name = "Bob" }
        };
    }
}
#endregion

public class UserMethodsCommandTests
{
    [Fact]
    public void UserMethod_ProvidesDefaultBehavior()
    {
        var stub = new UserRepoCmdStub();
        IUserRepoCmd repository = stub;

        // User method provides default data
        var users = repository.FindAll().ToList();

        Assert.Equal(2, users.Count);
        Assert.Equal("Alice", users[0].Name);
    }
}
