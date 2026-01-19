namespace KnockOff.Documentation.Samples.GettingStarted;

// =============================================================================
// Installation Sample
// =============================================================================

#region getting-started-install
// Add KnockOff to your test project via CLI:
// dotnet add package KnockOff

// Or add to your .csproj:
// <PackageReference Include="KnockOff" Version="10.23.0" />
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
    #region getting-started-standalone-use
    [Fact]
    public void SaveUser_WhenCalled_TracksInvocation()
    {
        // Arrange - create the stub
        var stub = new UserRepoStub();

        // Configure method behavior using OnCall
        // Chain .Verifiable() to mark for batch verification
        stub.SaveUser.OnCall((user) => true).Verifiable();

        // Act - use through the interface
        IUserRepo repository = stub;
        var result = repository.SaveUser(new User { Id = 1, Name = "Alice" });

        // Assert - Verify() checks all members marked with .Verifiable()
        Assert.True(result);
        stub.Verify();
    }
    #endregion
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
    #region getting-started-inline-use
    [Fact]
    public void Send_WhenCalled_TracksMessage()
    {
        // Arrange - instantiate the generated stub
        var stub = new Stubs.IEmailSvc();

        // Configure behavior and mark as verifiable
        // OnCall returns a tracking object for argument access
        var tracking = stub.Send.OnCall((to, subject, body) => { }).Verifiable();

        // Act - use through the interface
        IEmailSvc emailService = stub;
        emailService.Send("user@example.com", "Welcome", "Hello!");

        // Assert - Verify() checks method was called
        stub.Verify();
        // Access last arguments from tracking
        var args = tracking.LastArgs;
        Assert.Equal("user@example.com", args.to);
    }
    #endregion
}
