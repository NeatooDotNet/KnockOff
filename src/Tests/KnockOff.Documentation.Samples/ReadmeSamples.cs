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
}

public class ReadmeTeaserKnockOffTests
{
    [Fact]
    public void KnockOff_CompileTimeSetup()
    {
        var stub = new ReadmeUserRepoStub();
        stub.GetUser.Return((id) => new User { Id = id, Name = "Test User" });

        IReadmeUserRepo repository = stub;
        var user = repository.GetUser(42);

        Assert.NotNull(user);
        Assert.Equal("Test User", user.Name);
    }
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
    public void ConfigureStub_WithReturn()
    {
        var stub = new QuickStartRepoStub();

        stub.GetUser.Return((id) => new User { Id = id, Name = "Test User" });

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
    public void VerifyCalls_WithVerifiable()
    {
        var stub = new QuickStartRepoStub();
        stub.GetUser.Return((id) => new User { Id = id, Name = "Test" }).Verifiable();

        IQuickStartRepo repository = stub;

        var user = repository.GetUser(42);

        // Verify() checks all members marked with .Verifiable()
        stub.Verify();
    }
    #endregion
}

