using Moq;
using KnockOff;

namespace KnockOff.Documentation.Samples.MoqMigration;

// =============================================================================
// Interfaces for Migration Samples
// =============================================================================

public interface IMoqUserRepo
{
    User? GetUser(int id);
    Task<User?> GetUserAsync(int id);
    void SaveUser(User user);
    string ConnectionString { get; set; }
}

// =============================================================================
// KnockOff Stub (replaces Mock<IMoqUserRepo>)
// =============================================================================

#region moq-migration-stub-declaration
[KnockOff]
public partial class MoqUserRepoStub : IMoqUserRepo { }
#endregion

// =============================================================================
// Creating Stubs - Side-by-Side Comparison
// =============================================================================

public class CreateStubMoqTests
{
    #region moq-migration-create-stub-moq
    [Fact]
    public void CreateStub_MoqApproach()
    {
        var mock = new Mock<IMoqUserRepo>();
        IMoqUserRepo repository = mock.Object;

        Assert.NotNull(repository);
    }
    #endregion
}

public class CreateStubKnockOffTests
{
    #region moq-migration-create-stub-knockoff
    [Fact]
    public void CreateStub_KnockOffApproach()
    {
        var stub = new MoqUserRepoStub();
        IMoqUserRepo repository = stub;

        Assert.NotNull(repository);
    }
    #endregion
}

// =============================================================================
// Setup Method Returns - Side-by-Side Comparison
// =============================================================================

public class SetupMethodMoqTests
{
    #region moq-migration-setup-method-moq
    [Fact]
    public void SetupMethod_MoqApproach()
    {
        var mock = new Mock<IMoqUserRepo>();
        var testUser = new User { Id = 42, Name = "Alice" };

        mock.Setup(x => x.GetUser(It.IsAny<int>())).Returns(testUser);

        IMoqUserRepo repository = mock.Object;
        var user = repository.GetUser(42);

        Assert.NotNull(user);
        Assert.Equal("Alice", user.Name);
    }
    #endregion
}

public class SetupMethodKnockOffTests
{
    #region moq-migration-setup-method-knockoff
    [Fact]
    public void SetupMethod_KnockOffApproach()
    {
        var stub = new MoqUserRepoStub();
        var testUser = new User { Id = 42, Name = "Alice" };

        stub.GetUser.OnCall((id) => testUser);

        IMoqUserRepo repository = stub;
        var user = repository.GetUser(42);

        Assert.NotNull(user);
        Assert.Equal("Alice", user.Name);
    }
    #endregion
}

// =============================================================================
// Setup Property - Side-by-Side Comparison
// =============================================================================

public class SetupPropertyMoqTests
{
    #region moq-migration-setup-property-moq
    [Fact]
    public void SetupProperty_MoqApproach()
    {
        var mock = new Mock<IMoqUserRepo>();

        mock.Setup(x => x.ConnectionString).Returns("server=localhost");

        IMoqUserRepo repository = mock.Object;
        var connStr = repository.ConnectionString;

        Assert.Equal("server=localhost", connStr);
    }
    #endregion
}

public class SetupPropertyKnockOffTests
{
    #region moq-migration-setup-property-knockoff
    [Fact]
    public void SetupProperty_KnockOffApproach()
    {
        var stub = new MoqUserRepoStub();

        stub.ConnectionString.OnGet("server=localhost");

        IMoqUserRepo repository = stub;
        var connStr = repository.ConnectionString;

        Assert.Equal("server=localhost", connStr);
    }
    #endregion
}

// =============================================================================
// Verify Calls - Side-by-Side Comparison
// =============================================================================

public class VerifyCallsMoqTests
{
    #region moq-migration-verify-moq
    [Fact]
    public void VerifyCalls_MoqApproach()
    {
        var mock = new Mock<IMoqUserRepo>();

        IMoqUserRepo repository = mock.Object;
        repository.SaveUser(new User { Id = 1, Name = "Bob" });

        mock.Verify(x => x.SaveUser(It.IsAny<User>()), Moq.Times.Once());
    }
    #endregion
}

public class VerifyCallsKnockOffTests
{
    #region moq-migration-verify-knockoff
    [Fact]
    public void VerifyCalls_KnockOffApproach()
    {
        var stub = new MoqUserRepoStub();

        // Mark method as verifiable during setup
        stub.SaveUser.OnCall((user) => { }).Verifiable();

        IMoqUserRepo repository = stub;
        repository.SaveUser(new User { Id = 1, Name = "Bob" });

        // Verify() checks all members marked with .Verifiable()
        stub.Verify();

        // Or verify with Times constraint directly on tracking
        // stub.SaveUser.Verify(Times.Once);
    }
    #endregion
}

// =============================================================================
// Async Methods - Side-by-Side Comparison
// =============================================================================

public class AsyncMethodMoqTests
{
    #region moq-migration-async-moq
    [Fact]
    public async Task AsyncMethod_MoqApproach()
    {
        var mock = new Mock<IMoqUserRepo>();
        var testUser = new User { Id = 42, Name = "Alice" };

        mock.Setup(x => x.GetUserAsync(It.IsAny<int>())).ReturnsAsync(testUser);

        IMoqUserRepo repository = mock.Object;
        var user = await repository.GetUserAsync(42);

        Assert.NotNull(user);
        Assert.Equal("Alice", user.Name);
    }
    #endregion
}

public class AsyncMethodKnockOffTests
{
    #region moq-migration-async-knockoff
    [Fact]
    public async Task AsyncMethod_KnockOffApproach()
    {
        var stub = new MoqUserRepoStub();
        var testUser = new User { Id = 42, Name = "Alice" };

        stub.GetUserAsync.OnCall((id) => Task.FromResult<User?>(testUser));

        IMoqUserRepo repository = stub;
        var user = await repository.GetUserAsync(42);

        Assert.NotNull(user);
        Assert.Equal("Alice", user.Name);
    }
    #endregion
}

// =============================================================================
// Callbacks - Side-by-Side Comparison
// =============================================================================

public class CallbackMoqTests
{
    #region moq-migration-callback-moq
    [Fact]
    public void Callback_MoqApproach()
    {
        var mock = new Mock<IMoqUserRepo>();
        var savedUsers = new List<User>();

        mock.Setup(x => x.SaveUser(It.IsAny<User>()))
            .Callback<User>(u => savedUsers.Add(u));

        IMoqUserRepo repository = mock.Object;
        repository.SaveUser(new User { Id = 1, Name = "Alice" });
        repository.SaveUser(new User { Id = 2, Name = "Bob" });

        Assert.Equal(2, savedUsers.Count);
        Assert.Equal("Alice", savedUsers[0].Name);
        Assert.Equal("Bob", savedUsers[1].Name);
    }
    #endregion
}

public class CallbackKnockOffTests
{
    #region moq-migration-callback-knockoff
    [Fact]
    public void Callback_KnockOffApproach()
    {
        var stub = new MoqUserRepoStub();
        var savedUsers = new List<User>();

        stub.SaveUser.OnCall((user) =>
        {
            savedUsers.Add(user);
        });

        IMoqUserRepo repository = stub;
        repository.SaveUser(new User { Id = 1, Name = "Alice" });
        repository.SaveUser(new User { Id = 2, Name = "Bob" });

        Assert.Equal(2, savedUsers.Count);
        Assert.Equal("Alice", savedUsers[0].Name);
        Assert.Equal("Bob", savedUsers[1].Name);
    }
    #endregion
}

// =============================================================================
// Argument Matching - Side-by-Side Comparison
// =============================================================================

public class ArgumentMatchingMoqTests
{
    #region moq-migration-arguments-moq
    [Fact]
    public void ArgumentMatching_MoqApproach()
    {
        var mock = new Mock<IMoqUserRepo>();

        mock.Setup(x => x.GetUser(It.Is<int>(id => id > 0)))
            .Returns<int>(id => new User { Id = id, Name = "Valid User" });

        IMoqUserRepo repository = mock.Object;

        var validUser = repository.GetUser(1);
        var invalidUser = repository.GetUser(-1);

        Assert.NotNull(validUser);
        Assert.Null(invalidUser);
    }
    #endregion
}

public class ArgumentMatchingKnockOffTests
{
    #region moq-migration-arguments-knockoff
    [Fact]
    public void ArgumentMatching_KnockOffApproach()
    {
        var stub = new MoqUserRepoStub();

        stub.GetUser.OnCall((id) =>
            id > 0 ? new User { Id = id, Name = "Valid User" } : null);

        IMoqUserRepo repository = stub;

        var validUser = repository.GetUser(1);
        var invalidUser = repository.GetUser(-1);

        Assert.NotNull(validUser);
        Assert.Null(invalidUser);
    }
    #endregion
}

// =============================================================================
// Complete Example - Moq Version
// =============================================================================

public class UserServiceMigration
{
    private readonly IMoqUserRepo _repository;

    public UserServiceMigration(IMoqUserRepo repository)
    {
        _repository = repository;
    }

    public async Task<User?> GetUserAsync(int id)
    {
        return await _repository.GetUserAsync(id);
    }

    public void SaveUser(User user)
    {
        _repository.SaveUser(user);
    }
}

public class CompleteMoqTests
{
    #region moq-migration-complete-moq
    private readonly Mock<IMoqUserRepo> _mockRepo;
    private readonly UserServiceMigration _service;

    public CompleteMoqTests()
    {
        _mockRepo = new Mock<IMoqUserRepo>();
        _service = new UserServiceMigration(_mockRepo.Object);
    }

    [Fact]
    public async Task GetUser_ReturnsUser()
    {
        var user = new User { Id = 1, Name = "Alice" };
        _mockRepo.Setup(x => x.GetUserAsync(1)).ReturnsAsync(user);

        var result = await _service.GetUserAsync(1);

        Assert.Equal("Alice", result?.Name);
        _mockRepo.Verify(x => x.GetUserAsync(1), Moq.Times.Once());
    }

    [Fact]
    public void SaveUser_CallsRepository()
    {
        User? savedUser = null;
        _mockRepo.Setup(x => x.SaveUser(It.IsAny<User>()))
            .Callback<User>(u => savedUser = u);

        _service.SaveUser(new User { Id = 1, Name = "Bob" });

        Assert.NotNull(savedUser);
        Assert.Equal("Bob", savedUser?.Name);
        _mockRepo.Verify(x => x.SaveUser(It.IsAny<User>()), Moq.Times.Once());
    }
    #endregion
}

// =============================================================================
// Complete Example - KnockOff Version
// =============================================================================

public class CompleteKnockOffTests
{
    #region moq-migration-complete-knockoff
    private readonly MoqUserRepoStub _stub;
    private readonly UserServiceMigration _service;

    public CompleteKnockOffTests()
    {
        _stub = new MoqUserRepoStub();
        _service = new UserServiceMigration(_stub);
    }

    [Fact]
    public async Task GetUser_ReturnsUser()
    {
        var user = new User { Id = 1, Name = "Alice" };
        // Similar to Moq: Setup + Verifiable
        _stub.GetUserAsync.OnCall((id) => Task.FromResult<User?>(user)).Verifiable();

        var result = await _service.GetUserAsync(1);

        Assert.Equal("Alice", result?.Name);
        // Similar to Moq: mock.Verify() -> stub.Verify()
        _stub.Verify();
    }

    [Fact]
    public void SaveUser_CallsRepository()
    {
        User? savedUser = null;
        var tracking = _stub.SaveUser.OnCall((user) =>
        {
            savedUser = user;
        }).Verifiable();

        _service.SaveUser(new User { Id = 1, Name = "Bob" });

        Assert.NotNull(savedUser);
        Assert.Equal("Bob", savedUser?.Name);
        // Similar to Moq: mock.Verify(x => x.SaveUser(...), Times.Once())
        tracking.Verify(Times.Once);
    }
    #endregion
}
