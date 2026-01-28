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
        stub.GetUser.OnCall((id) => new User { Id = id, Name = "Test User" });

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

        stub.GetUser.OnCall((id) => new User { Id = id, Name = "Test User" });

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
        stub.GetUser.OnCall((id) => new User { Id = id, Name = "Test" }).Verifiable();

        IQuickStartRepo repository = stub;

        var user = repository.GetUser(42);

        // Verify() checks all members marked with .Verifiable()
        stub.Verify();
    }
    #endregion
}

// =============================================================================
// Plugin README Samples
// =============================================================================

public interface IPluginReadmeValueRepo
{
    int GetValue();
}

public interface IPluginReadmeGenericRepo
{
    T? GetById<T>(int id) where T : class, new();
}

[KnockOff]
public partial class PluginReadmeValueRepoStub : IPluginReadmeValueRepo { }

[KnockOff]
public partial class PluginReadmeGenericRepoStub : IPluginReadmeGenericRepo { }

public class PluginReadmeOnCallTests
{
    [Fact]
    public void OnCall_WithCallback()
    {
        var stub = new ReadmeUserRepoStub();

        #region plugin-readme-oncall-callback
        stub.GetUser.OnCall((id) => new User { Id = id, Name = "Dynamic" });
        #endregion

        IReadmeUserRepo repository = stub;
        var user = repository.GetUser(42);

        Assert.NotNull(user);
        Assert.Equal(42, user.Id);
        Assert.Equal("Dynamic", user.Name);
    }

    [Fact]
    public void OnCall_WithValue()
    {
        var stub = new ReadmeUserRepoStub();

        #region plugin-readme-oncall-value
        stub.GetUser.Returns(new User { Id = 1, Name = "Alice" });
        #endregion

        IReadmeUserRepo repository = stub;
        var user = repository.GetUser(42);

        Assert.NotNull(user);
        Assert.Equal(1, user.Id);
        Assert.Equal("Alice", user.Name);
    }
}

public class PluginReadmeSequentialTests
{
    [Fact]
    public void OnCallSequence_ThenCall()
    {
        var stub = new PluginReadmeValueRepoStub();

        #region plugin-readme-sequential
        stub.GetValue
            .OnCallSequence(() => 10)
            .ThenCall(() => 20)
            .ThenCall(() => 30)
            .Verifiable();
        #endregion

        IPluginReadmeValueRepo service = stub;

        Assert.Equal(10, service.GetValue());
        Assert.Equal(20, service.GetValue());
        Assert.Equal(30, service.GetValue());

        stub.Verify();
    }
}

public class PluginReadmeVerifyTests
{
    [Fact]
    public void Verify_Immediate()
    {
        var stub = new ReadmeUserRepoStub();

        #region plugin-readme-verify-immediate
        var tracking = stub.Save.OnCall((user) => { });

        // ... exercise stub ...
        #endregion

        IReadmeUserRepo repository = stub;
        repository.Save(new User { Id = 1 });

        tracking.Verify(Times.Once);
    }

    [Fact]
    public void Verify_Batch()
    {
        var stub = new ReadmeUserRepoStub();

        #region plugin-readme-verify-batch
        stub.GetUser.OnCall((id) => new User { Id = id }).Verifiable();
        stub.Save.OnCall((user) => { }).Verifiable(Times.Exactly(2));

        // ... exercise stub ...
        #endregion

        IReadmeUserRepo repository = stub;
        repository.GetUser(1);
        repository.Save(new User { Id = 1 });
        repository.Save(new User { Id = 2 });

        stub.Verify();
    }
}

public class PluginReadmeGenericMethodTests
{
    [Fact]
    public void GenericMethods_WithOfT()
    {
        var stub = new PluginReadmeGenericRepoStub();

        #region plugin-readme-generic-methods
        stub.GetById.Of<User>().OnCall((id) => new User { Id = id });
        #endregion

        IPluginReadmeGenericRepo repository = stub;
        var user = repository.GetById<User>(42);

        stub.GetById.Of<User>().Verify(Times.Once);

        Assert.NotNull(user);
        Assert.Equal(42, user.Id);
    }
}
