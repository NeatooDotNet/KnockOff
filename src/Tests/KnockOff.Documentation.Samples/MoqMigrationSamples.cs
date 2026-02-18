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
    [Fact]
    public void CreateStub_MoqApproach()
    {
        #region moq-migration-create-stub-moq
        // Create mock wrapper, access instance via .Object
        var mock = new Mock<IMoqUserRepo>();
        IMoqUserRepo repository = mock.Object;
        #endregion

        Assert.NotNull(repository);
    }
}

public class CreateStubKnockOffTests
{
    [Fact]
    public void CreateStub_KnockOffApproach()
    {
        #region moq-migration-create-stub-knockoff
        // Stub IS the instance - no wrapper needed
        var stub = new MoqUserRepoStub();
        IMoqUserRepo repository = stub;
        #endregion

        Assert.NotNull(repository);
    }
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
    [Fact]
    public void SetupMethod_MoqApproach()
    {
        var mock = new Mock<IMoqUserRepo>();
        var testUser = new User { Id = 42, Name = "Alice" };

        #region moq-migration-setup-method-moq
        // Setup with expression tree and It.IsAny<T>() matcher
        mock.Setup(x => x.GetUser(It.IsAny<int>())).Returns(testUser);
        #endregion

        IMoqUserRepo repository = mock.Object;
        var user = repository.GetUser(42);

        Assert.NotNull(user);
        Assert.Equal("Alice", user.Name);
    }
}

public class SetupMethodKnockOffTests
{
    [Fact]
    public void SetupMethod_KnockOffApproach()
    {
        var stub = new MoqUserRepoStub();
        var testUser = new User { Id = 42, Name = "Alice" };

        #region moq-migration-setup-method-knockoff
        // Return with typed delegate - arguments available directly
        stub.GetUser.Call((id) => testUser);
        #endregion

        IMoqUserRepo repository = stub;
        var user = repository.GetUser(42);

        Assert.NotNull(user);
        Assert.Equal("Alice", user.Name);
    }
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
        stub.GetUser.Call((id) => testUser);
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
    [Fact]
    public void SetupProperty_MoqApproach()
    {
        var mock = new Mock<IMoqUserRepo>();

        #region moq-migration-setup-property-moq
        // Properties use same Setup/Returns pattern as methods
        mock.Setup(x => x.ConnectionString).Returns("server=localhost");
        #endregion

        IMoqUserRepo repository = mock.Object;
        var connStr = repository.ConnectionString;

        Assert.Equal("server=localhost", connStr);
    }
}

public class SetupPropertyKnockOffTests
{
    [Fact]
    public void SetupProperty_KnockOffApproach()
    {
        var stub = new MoqUserRepoStub();

        #region moq-migration-setup-property-knockoff
        // Get configures property getter return value
        stub.ConnectionString.Get("server=localhost");
        #endregion

        IMoqUserRepo repository = stub;
        var connStr = repository.ConnectionString;

        Assert.Equal("server=localhost", connStr);
    }
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
        stub.ConnectionString.Get("server=localhost");
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
    [Fact]
    public void VerifyCalls_MoqApproach()
    {
        var mock = new Mock<IMoqUserRepo>();

        IMoqUserRepo repository = mock.Object;
        repository.SaveUser(new User { Id = 1, Name = "Bob" });

        #region moq-migration-verify-moq
        // Verify with expression tree and Times constraint
        mock.Verify(x => x.SaveUser(It.IsAny<User>()), Moq.Times.Once());
        #endregion
    }
}

public class VerifyCallsKnockOffTests
{
    [Fact]
    public void VerifyCalls_KnockOffApproach()
    {
        var stub = new MoqUserRepoStub();

        #region moq-migration-verify-knockoff
        // Mark as verifiable during setup, then verify all at once
        stub.SaveUser.Call((user) => { }).Verifiable();
        #endregion

        IMoqUserRepo repository = stub;
        repository.SaveUser(new User { Id = 1, Name = "Bob" });

        // Verify() checks all members marked with .Verifiable()
        stub.Verify();
    }
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
        stub.SaveUser.Call((user) => { }).Verifiable();
        ((IMoqUserRepo)stub).SaveUser(new User { Name = "Bob" });
        stub.Verify();

        // KNOCKOFF (individual verification):
        var stub2 = new MoqUserRepoStub();
        var tracking = stub2.SaveUser.Call((user) => { });
        ((IMoqUserRepo)stub2).SaveUser(new User { Name = "Bob" });
        tracking.Verify(Called.Once);
        #endregion
    }
}

// =============================================================================
// Async Method Samples
// =============================================================================

public class AsyncMethodMoqTests
{
    [Fact]
    public async Task AsyncMethod_MoqApproach()
    {
        var mock = new Mock<IMoqUserRepo>();
        var testUser = new User { Id = 42, Name = "Alice" };

        #region moq-migration-async-moq
        // ReturnsAsync helper wraps value in Task
        mock.Setup(x => x.GetUserAsync(It.IsAny<int>())).ReturnsAsync(testUser);
        #endregion

        IMoqUserRepo repository = mock.Object;
        var user = await repository.GetUserAsync(42);

        Assert.NotNull(user);
        Assert.Equal("Alice", user.Name);
    }
}

public class AsyncMethodKnockOffTests
{
    [Fact]
    public async Task AsyncMethod_KnockOffApproach()
    {
        var stub = new MoqUserRepoStub();
        var testUser = new User { Id = 42, Name = "Alice" };

        #region moq-migration-async-knockoff
        // Use Task.FromResult to wrap the return value
        stub.GetUserAsync.Call((id) => Task.FromResult<User?>(testUser));
        #endregion

        IMoqUserRepo repository = stub;
        var user = await repository.GetUserAsync(42);

        Assert.NotNull(user);
        Assert.Equal("Alice", user.Name);
    }
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
        stub.GetUserAsync.Call((id) => Task.FromResult<User?>(testUser));
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
    [Fact]
    public void Callback_MoqApproach()
    {
        var mock = new Mock<IMoqUserRepo>();
        var savedUsers = new List<User>();

        #region moq-migration-callback-moq
        // Callback is separate from Returns
        mock.Setup(x => x.SaveUser(It.IsAny<User>()))
            .Callback<User>(u => savedUsers.Add(u));
        #endregion

        IMoqUserRepo repository = mock.Object;
        repository.SaveUser(new User { Id = 1, Name = "Alice" });
        repository.SaveUser(new User { Id = 2, Name = "Bob" });

        Assert.Equal(2, savedUsers.Count);
        Assert.Equal("Alice", savedUsers[0].Name);
        Assert.Equal("Bob", savedUsers[1].Name);
    }
}

public class CallbackKnockOffTests
{
    [Fact]
    public void Callback_KnockOffApproach()
    {
        var stub = new MoqUserRepoStub();
        var savedUsers = new List<User>();

        #region moq-migration-callback-knockoff
        // Logic goes directly in Return delegate
        stub.SaveUser.Call((user) => savedUsers.Add(user));
        #endregion

        IMoqUserRepo repository = stub;
        repository.SaveUser(new User { Id = 1, Name = "Alice" });
        repository.SaveUser(new User { Id = 2, Name = "Bob" });

        Assert.Equal(2, savedUsers.Count);
        Assert.Equal("Alice", savedUsers[0].Name);
        Assert.Equal("Bob", savedUsers[1].Name);
    }
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
        stub.SaveUser.Call((user) => knockoffSavedUsers.Add(user));
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
    [Fact]
    public void ArgumentMatching_MoqApproach()
    {
        var mock = new Mock<IMoqUserRepo>();

        #region moq-migration-arguments-moq
        // It.Is<T>() for conditional matching, Returns<T> to access args
        mock.Setup(x => x.GetUser(It.Is<int>(id => id > 0)))
            .Returns<int>(id => new User { Id = id, Name = "Valid User" });
        #endregion

        IMoqUserRepo repository = mock.Object;

        var validUser = repository.GetUser(1);
        var invalidUser = repository.GetUser(-1);

        Assert.NotNull(validUser);
        Assert.Null(invalidUser);
    }
}

public class ArgumentMatchingKnockOffTests
{
    [Fact]
    public void ArgumentMatching_KnockOffApproach()
    {
        var stub = new MoqUserRepoStub();

        #region moq-migration-arguments-knockoff
        // Arguments available directly - use standard C# conditionals
        stub.GetUser.Call((id) =>
            id > 0 ? new User { Id = id, Name = "Valid User" } : null);
        #endregion

        IMoqUserRepo repository = stub;

        var validUser = repository.GetUser(1);
        var invalidUser = repository.GetUser(-1);

        Assert.NotNull(validUser);
        Assert.Null(invalidUser);
    }
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
        stub.GetUser.Call((id) =>
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
        stub.GetUser.Call((id) =>
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

        #region moq-migration-complete-moq
        // Setup with expression tree
        _mockRepo.Setup(x => x.GetUserAsync(1)).ReturnsAsync(user);

        var result = await _service.GetUserAsync(1);

        // Verify with expression tree and Times
        _mockRepo.Verify(x => x.GetUserAsync(1), Moq.Times.Once());
        #endregion

        Assert.Equal("Alice", result?.Name);
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
}

// =============================================================================
// Complete Example - KnockOff Version
// =============================================================================

public class CompleteKnockOffTests
{
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

        #region moq-migration-complete-knockoff
        // Return with Verifiable marks for batch verification
        _stub.GetUserAsync.Call((id) => Task.FromResult<User?>(user)).Verifiable();

        var result = await _service.GetUserAsync(1);

        // stub.Verify() checks all .Verifiable() members
        _stub.Verify();
        #endregion

        Assert.Equal("Alice", result?.Name);
    }

    [Fact]
    public void SaveUser_CallsRepository()
    {
        User? savedUser = null;
        var tracking = _stub.SaveUser.Call((user) =>
        {
            savedUser = user;
        }).Verifiable();

        _service.SaveUser(new User { Id = 1, Name = "Bob" });

        Assert.NotNull(savedUser);
        Assert.Equal("Bob", savedUser?.Name);
        // Or verify with Times constraint on tracking object
        tracking.Verify(Called.Once);
    }
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

        stub.GetUserAsync.Call((id) => Task.FromResult<User?>(user)).Verifiable();

        var knockoffResult = await knockoffService.GetUserAsync(1);
        stub.Verify();
        #endregion

        Assert.Equal("Alice", moqResult?.Name);
        Assert.Equal("Alice", knockoffResult?.Name);
    }
}

// =============================================================================
// Common Gotchas - Forgetting Partial Keyword
// =============================================================================

#region moq-migration-gotcha-partial-wrong
// Wrong
[KnockOff<IMoqUserRepo>]
class MoqUserRepoStubWrong { }
#endregion

#region moq-migration-gotcha-partial-correct
// Correct
[KnockOff<IMoqUserRepo>]
partial class MoqUserRepoStubCorrect { }
#endregion

// =============================================================================
// Common Gotchas - Wrong Return Signature
// =============================================================================

public class GotchaSignatureTests
{
    [Fact]
    public void WrongSignature_Examples()
    {
        var stub = new MoqUserRepoStub();
        var user = new User { Id = 1, Name = "Alice" };

        #region moq-migration-gotcha-signature-wrong
        // Wrong: GetUser(int id) expects (int) callback
        // stub.GetUser.Call(() => user);  // Compile error
        #endregion

        #region moq-migration-gotcha-signature-correct
        // Correct
        stub.GetUser.Call((id) => user);
        #endregion
    }
}

// =============================================================================
// Common Gotchas - Forgetting .Object Equivalence
// =============================================================================

public class GotchaObjectTests
{
    [Fact]
    public void ObjectEquivalence_Examples()
    {
        #region moq-migration-gotcha-object-moq
        // Moq: needed .Object
        var mock = new Mock<IMoqUserRepo>();
        var moqService = new UserServiceMigration(mock.Object);
        #endregion

        #region moq-migration-gotcha-object-knockoff
        // KnockOff: use stub directly
        var stub = new MoqUserRepoStub();
        var knockoffService = new UserServiceMigration(stub);
        #endregion

        Assert.NotNull(moqService);
        Assert.NotNull(knockoffService);
    }
}

// =============================================================================
// Common Gotchas - Async Auto-Wrap
// =============================================================================

public class GotchaAsyncAutoWrapTests
{
    [Fact]
    public async Task AsyncAutoWrap_Examples()
    {
        var stub = new MoqUserRepoStub();
        var user = new User { Id = 1, Name = "Alice" };

        #region moq-migration-gotcha-async-autowrap
        // Returns - auto-wraps in Task.FromResult
        stub.GetUserAsync.Return(user);

        // Simplified callback - also auto-wraps (return unwrapped type)
        stub.GetUserAsync.Call((id) => user);

        // Only use Task.FromResult when callback needs actual async operations
        stub.GetUserAsync.Call(async (id) =>
        {
            await Task.Delay(1); // Some actual async work
            return user;
        });
        #endregion

        var result = await ((IMoqUserRepo)stub).GetUserAsync(1);
        Assert.Equal("Alice", result?.Name);
    }
}

// =============================================================================
// Common Gotchas - Async Configuration Options
// =============================================================================

public class MoqAsyncConfigOptionsTests
{
    [Fact]
    public async Task AsyncOptions_ThreeTiers()
    {
        var stub = new MoqUserRepoStub();
        var testUser = new User { Id = 42, Name = "Alice" };

        #region moq-gotcha-async-options
        // 1. Return() -- auto-wraps in Task.FromResult (recommended for fixed values)
        stub.GetUserAsync.Return(testUser);

        // 2. Return() simplified -- callback returns unwrapped type, auto-wrapped
        stub.GetUserAsync.Call((id) => new User { Id = id });

        // 3. Return() full -- callback returns Task<T> directly
        stub.GetUserAsync.Call((id) => Task.FromResult<User?>(testUser));
        #endregion

        IMoqUserRepo repository = stub;
        var user = await repository.GetUserAsync(42);

        Assert.NotNull(user);
    }
}

// =============================================================================
// Common Gotchas - Property Configuration
// =============================================================================

public class GotchaPropertyTests
{
    [Fact]
    public void PropertyConfiguration_Examples()
    {
        var stub = new MoqUserRepoStub();

        #region moq-migration-gotcha-property-wrong
        // Wrong: Return is for methods
        // stub.ConnectionString.Call(() => "connection");  // Compile error
        #endregion

        #region moq-migration-gotcha-property-correct
        // Correct: use Get for property getters
        stub.ConnectionString.Get("connection");

        // For setters, use Set
        stub.ConnectionString.Set((value) => { /* handle set */ });
        #endregion

        Assert.Equal("connection", ((IMoqUserRepo)stub).ConnectionString);
    }
}

// =============================================================================
// Common Gotchas - Void Methods Need Delegate Body
// =============================================================================

public class GotchaVoidMethodTests
{
    [Fact]
    public void VoidMethod_Examples()
    {
        var stub = new MoqUserRepoStub();

        #region moq-migration-gotcha-void-wrong
        // Wrong: no delegate body
        // stub.SaveUser.Return();  // Compile error
        #endregion

        #region moq-migration-gotcha-void-correct
        // Correct
        stub.SaveUser.Call((user) => { });
        #endregion
    }
}

// =============================================================================
// Times Matcher Reference
// =============================================================================

public class TimesMatcherTests
{
    [Fact]
    public void TimesMatchers_Examples()
    {
        var mock = new Mock<IMoqUserRepo>();
        var stub = new MoqUserRepoStub();
        stub.SaveUser.Call((user) => { });

        // Call 3 times
        ((IMoqUserRepo)stub).SaveUser(new User { Name = "A" });
        ((IMoqUserRepo)stub).SaveUser(new User { Name = "B" });
        ((IMoqUserRepo)stub).SaveUser(new User { Name = "C" });

        mock.Object.SaveUser(new User { Name = "A" });
        mock.Object.SaveUser(new User { Name = "B" });
        mock.Object.SaveUser(new User { Name = "C" });

        #region moq-migration-times-example
        // Moq
        mock.Verify(x => x.SaveUser(It.IsAny<User>()), Moq.Times.Exactly(3));

        // KnockOff
        stub.SaveUser.Verify(Called.Exactly(3));

        // For range verification (no Called.Between in KnockOff):
        stub.SaveUser.Verify(Called.AtLeast(1));
        stub.SaveUser.Verify(Called.AtMost(5));
        #endregion
    }
}

// =============================================================================
// CallBase Behavior Samples
// =============================================================================

public class MoqCallBaseService
{
    public virtual string GetStatus() => "real-status";
    public virtual void Initialize() { }
}

[KnockOff<MoqCallBaseService>]
public partial class CallBaseDemoTests
{
    [Fact]
    public void CallBase_MoqApproach()
    {
        #region moq-migration-callbase-moq
        // Moq requires explicit opt-in to call base implementations
        var mock = new Mock<MoqCallBaseService>();
        mock.CallBase = true;  // Without this, virtual methods return default
        mock.Setup(x => x.GetStatus()).Returns("overridden");

        // Virtual methods not configured in Setup call the real implementation
        mock.Object.Initialize();  // Calls real Initialize()
        #endregion

        Assert.Equal("overridden", mock.Object.GetStatus());
    }

    [Fact]
    public void CallBase_KnockOffApproach()
    {
        #region moq-migration-callbase-knockoff
        // KnockOff class stubs call base by default -- no opt-in needed
        var stub = new Stubs.MoqCallBaseService();
        stub.GetStatus.Return("overridden");  // Override just this method

        MoqCallBaseService service = stub.Object;
        service.Initialize();  // Calls real Initialize() -- this is the default!
        #endregion

        Assert.Equal("overridden", service.GetStatus());
    }
}
