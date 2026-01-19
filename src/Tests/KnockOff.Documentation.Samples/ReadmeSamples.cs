using Moq;

namespace KnockOff.Documentation.Samples.Readme;

// =============================================================================
// Interfaces for README Samples
// =============================================================================

public interface IReadmeUserRepo
{
    User? GetUser(int id);
    void Save(User user);
}

// =============================================================================
// Stubs for README Samples
// =============================================================================

[KnockOff]
public partial class ReadmeUserRepoStub : IReadmeUserRepo { }

// =============================================================================
// README Teaser - Moq vs KnockOff Comparison
// =============================================================================

public class ReadmeTeaserMoqTests
{
    #region readme-teaser-moq
    [Fact]
    public void Moq_RuntimeSetup()
    {
        var mock = new Mock<IReadmeUserRepo>();
        mock.Setup(x => x.GetUser(It.IsAny<int>()))
            .Returns(new User { Id = 42, Name = "Test User" });

        IReadmeUserRepo repository = mock.Object;
        var user = repository.GetUser(42);

        Assert.NotNull(user);
        Assert.Equal("Test User", user.Name);
    }
    #endregion
}

public class ReadmeTeaserKnockOffTests
{
    #region readme-teaser-knockoff
    [Fact]
    public void KnockOff_CompileTimeSetup()
    {
        var stub = new ReadmeUserRepoStub();
        stub.GetUser.OnCall((ko, id) => new User { Id = id, Name = "Test User" });

        IReadmeUserRepo repository = stub;
        var user = repository.GetUser(42);

        Assert.NotNull(user);
        Assert.Equal("Test User", user.Name);
    }
    #endregion
}

// =============================================================================
// Quick Start: Create a Stub
// =============================================================================

#region readme-quickstart-stub
public interface IQuickStartRepo
{
    User? GetUser(int id);
}

[KnockOff]
public partial class QuickStartRepoStub : IQuickStartRepo { }

public class QuickStartCreateStubTests
{
    [Fact]
    public void CreateStub_IsReady()
    {
        var stub = new QuickStartRepoStub();

        IQuickStartRepo repository = stub;
        Assert.NotNull(repository);
    }
}
#endregion

// =============================================================================
// Quick Start: Configure Behavior
// =============================================================================

public class QuickStartConfigureTests
{
    #region readme-quickstart-configure
    [Fact]
    public void ConfigureStub_WithOnCall()
    {
        var stub = new QuickStartRepoStub();

        stub.GetUser.OnCall((ko, id) => new User { Id = id, Name = "Test User" });

        IQuickStartRepo repository = stub;
        var user = repository.GetUser(42);

        Assert.NotNull(user);
        Assert.Equal(42, user.Id);
        Assert.Equal("Test User", user.Name);
    }
    #endregion
}

// =============================================================================
// Quick Start: Verify Calls
// =============================================================================

public class QuickStartVerifyTests
{
    #region readme-quickstart-verify
    [Fact]
    public void VerifyCalls_WithCallCount()
    {
        var stub = new QuickStartRepoStub();
        stub.GetUser.OnCall((ko, id) => new User { Id = id, Name = "Test" });

        IQuickStartRepo repository = stub;

        var user = repository.GetUser(42);

        Assert.Equal(1, stub.GetUser.CallCount);
    }
    #endregion
}
