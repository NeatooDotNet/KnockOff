namespace KnockOff.Documentation.Samples.GettingStarted;

// =============================================================================
// Installation Sample
// =============================================================================

#region getting-started-install
// Add KnockOff to your test project via CLI:
// dotnet add package KnockOff

// Or add to your .csproj:
// <PackageReference Include="KnockOff" Version="0.34.0" />
#endregion

// =============================================================================
// Stand-Alone Pattern - Interface and Stub Definition
// =============================================================================

#region getting-started-standalone-define
// Define the interface you want to stub
public interface IUserRepo
{
    User? GetById(int id);
    bool SaveUser(User user);
}

// Create a partial class with [KnockOff] attribute
[KnockOff]
public partial class UserRepoStub : IUserRepo
{
    // No implementations needed - the generator creates them
}
#endregion

// =============================================================================
// Stand-Alone Pattern - Usage in Tests
// =============================================================================

public class StandaloneUsageTests
{
    [Fact]
    public void SaveUser_WhenCalled_TracksInvocation()
    {
        var stub = new UserRepoStub();

        #region getting-started-standalone-use
        // Configure behavior and mark for verification
        stub.SaveUser.OnCall((user) => true).Verifiable();

        // Call the method through the interface
        IUserRepo repository = stub;
        repository.SaveUser(new User { Id = 1, Name = "Alice" });

        // Verify() checks all .Verifiable() members were called
        stub.Verify();
        #endregion

        // Additional assertion for test completeness
        Assert.True(((IUserRepo)stub).SaveUser(new User { Id = 1, Name = "Test" }));
    }
}

// =============================================================================
// Inline Pattern - Definition and Usage
// =============================================================================

public interface IEmailSvc
{
    void Send(string to, string subject, string body);
    bool IsConfigured { get; }
}

#region getting-started-inline-define
// Add [KnockOff<T>] attribute to your test class
[KnockOff<IEmailSvc>]
public partial class InlineStubTests
{
    // The source generator creates Stubs.IEmailSvc for you
}
#endregion

public partial class InlineStubTests
{
    [Fact]
    public void Send_WhenCalled_TracksMessage()
    {
        var stub = new Stubs.IEmailSvc();

        #region getting-started-inline-use
        // OnCall returns a tracking object for argument access
        var tracking = stub.Send.OnCall((to, subject, body) => { }).Verifiable();

        IEmailSvc emailService = stub;
        emailService.Send("user@example.com", "Welcome", "Hello!");

        // Access captured arguments from the tracking object
        var args = tracking.LastArgs;
        #endregion

        stub.Verify();
        Assert.Equal("user@example.com", args.to);
    }
}

// =============================================================================
// Configuring Return Values - Returns()
// =============================================================================

public class ValueOverloadTests
{
    [Fact]
    public void GetById_ValueOverload_SimplerSyntax()
    {
        var stub = new UserRepoStub();

        #region getting-started-value-overloads
        // Returns() - simple fixed value
        stub.GetById.Returns(new User { Id = 1, Name = "Alice" });

        // OnCall() - dynamic value based on arguments
        stub.GetById.OnCall((id) => new User { Id = id, Name = "Dynamic" });
        #endregion

        IUserRepo repository = stub;
        var user = repository.GetById(1);

        Assert.Equal("Dynamic", user!.Name);
    }
}

// =============================================================================
// Configuring Properties - OnGet/OnSet
// =============================================================================

public interface IUserConfig
{
    User? CurrentUser { get; set; }
}

[KnockOff]
public partial class UserConfigStub : IUserConfig { }

public class PropertyConfigurationTests
{
    [Fact]
    public void Property_OnGetAndOnSet()
    {
        var stub = new UserConfigStub();
        User? capturedUser = null;

        #region getting-started-property-configuration
        // OnGet - configure the getter return value
        stub.CurrentUser.OnGet(new User { Id = 1, Name = "Alice" });

        // OnSet - capture or validate setter calls
        stub.CurrentUser.OnSet((user) => capturedUser = user);
        #endregion

        IUserConfig config = stub;

        var user = config.CurrentUser;
        Assert.Equal("Alice", user!.Name);

        config.CurrentUser = new User { Id = 2, Name = "Bob" };
        Assert.Equal("Bob", capturedUser!.Name);
    }
}

// =============================================================================
// Configuring Async Methods - Auto-Wrapping
// =============================================================================

public interface IAsyncUserRepo
{
    Task<User?> GetUserAsync(int id);
}

[KnockOff]
public partial class AsyncUserRepoStub : IAsyncUserRepo { }

public class AsyncWrappingTests
{
    [Fact]
    public async Task AsyncMethod_ValueAutoWrapped()
    {
        var stub = new AsyncUserRepoStub();

        #region getting-started-async-wrapping
        // Returns() auto-wraps in Task.FromResult - no manual wrapping needed
        stub.GetUserAsync.Returns(new User { Id = 1, Name = "Alice" });
        #endregion

        IAsyncUserRepo repository = stub;
        var user = await repository.GetUserAsync(1);

        Assert.Equal("Alice", user!.Name);
    }
}

// =============================================================================
// OnCall Dynamic Behavior
// =============================================================================

public class OnCallDynamicTests
{
    [Fact]
    public void OnCall_DynamicBehavior()
    {
        var stub = new UserRepoStub();

        #region getting-started-oncall-dynamic
        // Use Returns for fixed values
        stub.GetById.Returns(new User { Id = 1, Name = "Alice" });

        // Use OnCall for dynamic values, side effects, or conditional logic
        stub.GetById.OnCall((id) => new User { Id = id, Name = id > 100 ? "Admin" : "Regular" });

        // Both return tracking objects for verification
        #endregion

        IUserRepo repository = stub;
        var user = repository.GetById(1);
        Assert.Equal("Regular", user!.Name);
    }
}
