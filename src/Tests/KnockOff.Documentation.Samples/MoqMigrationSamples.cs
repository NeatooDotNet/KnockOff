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
// KnockOff Stub Declaration
// =============================================================================

#region moq-migration-stub-declaration
[KnockOff]
public partial class MoqUserRepoStub : IMoqUserRepo { }
#endregion

// =============================================================================
// Mock Creation Samples
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
// Mock Creation - Combined (for skills/commands documentation)
// =============================================================================

public class MockCreationCombinedSamples
{
    [Fact]
    public void MockCreation_Comparison()
    {
        #region moq-to-knockoff-mock-creation
        // MOQ:
        var mock = new Mock<IMoqUserRepo>();
        IMoqUserRepo moqRepo = mock.Object;

        // KNOCKOFF:
        var stub = new MoqUserRepoStub();
        IMoqUserRepo knockoffRepo = stub;
        #endregion

        Assert.NotNull(moqRepo);
        Assert.NotNull(knockoffRepo);
    }
}

// =============================================================================
// Method Setup Samples
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
// Method Setup - Combined (for skills/commands documentation)
// =============================================================================

public class MethodReturnsCombinedSamples
{
    [Fact]
    public void MethodReturns_Comparison()
    {
        var testUser = new User { Id = 42, Name = "Alice" };

        #region moq-to-knockoff-method-returns
        // MOQ:
        var mock = new Mock<IMoqUserRepo>();
        mock.Setup(x => x.GetUser(It.IsAny<int>())).Returns(testUser);

        // KNOCKOFF:
        var stub = new MoqUserRepoStub();
        stub.GetUser.OnCall((id) => testUser);
        #endregion

        Assert.Equal("Alice", mock.Object.GetUser(1)?.Name);
        Assert.Equal("Alice", ((IMoqUserRepo)stub).GetUser(1)?.Name);
    }
}

// =============================================================================
// Property Setup Samples
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
// Property Setup - Combined (for skills/commands documentation)
// =============================================================================

public class PropertySetupCombinedSamples
{
    [Fact]
    public void PropertySetup_Comparison()
    {
        #region moq-to-knockoff-property-setup
        // MOQ:
        var mock = new Mock<IMoqUserRepo>();
        mock.Setup(x => x.ConnectionString).Returns("server=localhost");

        // KNOCKOFF:
        var stub = new MoqUserRepoStub();
        stub.ConnectionString.OnGet("server=localhost");
        #endregion

        Assert.Equal("server=localhost", mock.Object.ConnectionString);
        Assert.Equal("server=localhost", ((IMoqUserRepo)stub).ConnectionString);
    }
}

// =============================================================================
// Verification Samples
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
// Verification - Combined (for skills/commands documentation)
// =============================================================================

public class VerificationCombinedSamples
{
    [Fact]
    public void Verification_Comparison()
    {
        #region moq-to-knockoff-verification
        // MOQ:
        var mock = new Mock<IMoqUserRepo>();
        mock.Object.SaveUser(new User { Name = "Bob" });
        mock.Verify(x => x.SaveUser(It.IsAny<User>()), Moq.Times.Once());

        // KNOCKOFF (batch verification):
        var stub = new MoqUserRepoStub();
        stub.SaveUser.OnCall((user) => { }).Verifiable();
        ((IMoqUserRepo)stub).SaveUser(new User { Name = "Bob" });
        stub.Verify();

        // KNOCKOFF (individual verification):
        var stub2 = new MoqUserRepoStub();
        var tracking = stub2.SaveUser.OnCall((user) => { });
        ((IMoqUserRepo)stub2).SaveUser(new User { Name = "Bob" });
        tracking.Verify(Times.Once);
        #endregion
    }
}

// =============================================================================
// Async Method Samples
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
// Async Methods - Combined (for skills/commands documentation)
// =============================================================================

public class AsyncMethodsCombinedSamples
{
    [Fact]
    public async Task AsyncMethod_Comparison()
    {
        var testUser = new User { Id = 42, Name = "Alice" };

        #region moq-to-knockoff-async-methods
        // MOQ:
        var mock = new Mock<IMoqUserRepo>();
        mock.Setup(x => x.GetUserAsync(It.IsAny<int>())).ReturnsAsync(testUser);

        // KNOCKOFF:
        var stub = new MoqUserRepoStub();
        stub.GetUserAsync.OnCall((id) => Task.FromResult<User?>(testUser));
        #endregion

        var moqResult = await mock.Object.GetUserAsync(42);
        var knockoffResult = await ((IMoqUserRepo)stub).GetUserAsync(42);

        Assert.Equal("Alice", moqResult?.Name);
        Assert.Equal("Alice", knockoffResult?.Name);
    }
}

// =============================================================================
// Callback Samples
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
// Callbacks - Combined (for skills/commands documentation)
// =============================================================================

public class CallbacksCombinedSamples
{
    [Fact]
    public void Callback_Comparison()
    {
        var moqSavedUsers = new List<User>();
        var knockoffSavedUsers = new List<User>();

        #region moq-to-knockoff-callbacks
        // MOQ:
        var mock = new Mock<IMoqUserRepo>();
        mock.Setup(x => x.SaveUser(It.IsAny<User>()))
            .Callback<User>(u => moqSavedUsers.Add(u));

        // KNOCKOFF:
        var stub = new MoqUserRepoStub();
        stub.SaveUser.OnCall((user) => knockoffSavedUsers.Add(user));
        #endregion

        mock.Object.SaveUser(new User { Name = "Alice" });
        ((IMoqUserRepo)stub).SaveUser(new User { Name = "Alice" });

        Assert.Single(moqSavedUsers);
        Assert.Single(knockoffSavedUsers);
    }
}

// =============================================================================
// Argument Matching Samples
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
// Argument Matching - Combined (for skills/commands documentation)
// =============================================================================

public class ArgumentMatchingCombinedSamples
{
    [Fact]
    public void ArgumentMatching_Comparison()
    {
        #region moq-to-knockoff-argument-matching
        // MOQ:
        var mock = new Mock<IMoqUserRepo>();
        mock.Setup(x => x.GetUser(It.Is<int>(id => id > 0)))
            .Returns<int>(id => new User { Id = id, Name = "Valid" });

        // KNOCKOFF:
        var stub = new MoqUserRepoStub();
        stub.GetUser.OnCall((id) =>
            id > 0 ? new User { Id = id, Name = "Valid" } : null);
        #endregion

        Assert.NotNull(mock.Object.GetUser(1));
        Assert.Null(mock.Object.GetUser(-1));
        Assert.NotNull(((IMoqUserRepo)stub).GetUser(1));
        Assert.Null(((IMoqUserRepo)stub).GetUser(-1));
    }
}

// =============================================================================
// Sequence Pattern - Combined (for skills/commands documentation)
// =============================================================================

public class SequencePatternCombinedSamples
{
    [Fact]
    public void Sequence_Comparison()
    {
        var firstUser = new User { Id = 1, Name = "First" };
        var secondUser = new User { Id = 2, Name = "Second" };

        #region moq-to-knockoff-sequence-pattern
        // MOQ:
        var mock = new Mock<IMoqUserRepo>();
        mock.SetupSequence(x => x.GetUser(It.IsAny<int>()))
            .Returns(firstUser)
            .Returns(secondUser);

        // KNOCKOFF:
        var stub = new MoqUserRepoStub();
        int callCount = 0;
        stub.GetUser.OnCall((id) =>
        {
            callCount++;
            return callCount == 1 ? firstUser : secondUser;
        });
        #endregion

        Assert.Equal("First", mock.Object.GetUser(1)?.Name);
        Assert.Equal("Second", mock.Object.GetUser(1)?.Name);
        Assert.Equal("First", ((IMoqUserRepo)stub).GetUser(1)?.Name);
        Assert.Equal("Second", ((IMoqUserRepo)stub).GetUser(1)?.Name);
    }
}

// =============================================================================
// Using Statements - Combined (for skills/commands documentation)
// =============================================================================

public class UsingStatementsCombinedSamples
{
    [Fact]
    public void UsingStatements_Demonstration()
    {
        #region moq-to-knockoff-using-statements
        // BEFORE (Moq):
        // using Moq;

        // AFTER (KnockOff):
        // using KnockOff;
        #endregion

        // Both work in this file since we have both using statements
        var mock = new Mock<IMoqUserRepo>();
        var stub = new MoqUserRepoStub();

        Assert.NotNull(mock.Object);
        Assert.NotNull(stub);
    }
}

// =============================================================================
// Complete Example - Service Under Test
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

// =============================================================================
// Complete Example - Moq Version
// =============================================================================

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

// =============================================================================
// Complete Migration - Combined (for skills/commands documentation)
// =============================================================================

public class CompleteMigrationCombinedExample
{
    [Fact]
    public async Task CompleteMigration_Demonstration()
    {
        #region moq-to-knockoff-complete-migration
        // ========== MOQ VERSION ==========
        var mockRepo = new Mock<IMoqUserRepo>();
        var moqService = new UserServiceMigration(mockRepo.Object);

        var user = new User { Id = 1, Name = "Alice" };
        mockRepo.Setup(x => x.GetUserAsync(1)).ReturnsAsync(user);

        var moqResult = await moqService.GetUserAsync(1);
        mockRepo.Verify(x => x.GetUserAsync(1), Moq.Times.Once());

        // ========== KNOCKOFF VERSION ==========
        var stub = new MoqUserRepoStub();
        var knockoffService = new UserServiceMigration(stub);

        stub.GetUserAsync.OnCall((id) => Task.FromResult<User?>(user)).Verifiable();

        var knockoffResult = await knockoffService.GetUserAsync(1);
        stub.Verify();
        #endregion

        Assert.Equal("Alice", moqResult?.Name);
        Assert.Equal("Alice", knockoffResult?.Name);
    }
}
