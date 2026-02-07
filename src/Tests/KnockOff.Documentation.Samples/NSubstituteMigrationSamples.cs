using NSubstitute;
using NSubstitute.ReceivedExtensions;
using KnockOff;

namespace KnockOff.Documentation.Samples.NSubstituteMigration;

// =============================================================================
// Interfaces for Migration Samples
// =============================================================================

public interface INSubUserRepo
{
    User? GetUser(int id);
    Task<User?> GetUserAsync(int id);
    void SaveUser(User user);
    void DeleteUser(int id);
    string ConnectionString { get; set; }
    bool IsConnected { get; }
    IEnumerable<User> FindUsers(string name, int limit);
}

// =============================================================================
// KnockOff Stub (replaces Substitute.For<INSubUserRepo>())
// =============================================================================

[KnockOff]
public partial class NSubUserRepoStub : INSubUserRepo { }

// =============================================================================
// Creating Stubs - Side-by-Side Comparison
// =============================================================================

public class CreateStubNSubTests
{
    [Fact]
    public void CreateStub_NSubstituteApproach()
    {
        #region nsub-migration-create-stub-nsub
        // NSubstitute: Create proxy at runtime with a single line
        var substitute = Substitute.For<INSubUserRepo>();
        #endregion

        INSubUserRepo repository = substitute;

        Assert.NotNull(repository);
    }
}

public class CreateStubKnockOffTests
{
    [Fact]
    public void CreateStub_KnockOffApproach()
    {
        #region nsub-migration-create-stub-knockoff
        // KnockOff: Instantiate the generated stub class
        var stub = new NSubUserRepoStub();
        #endregion

        INSubUserRepo repository = stub;

        Assert.NotNull(repository);
    }
}

// =============================================================================
// Returns Setup - Side-by-Side Comparison
// =============================================================================

public class ReturnsNSubTests
{
    [Fact]
    public void Returns_NSubstituteApproach()
    {
        var substitute = Substitute.For<INSubUserRepo>();
        var testUser = new User { Id = 42, Name = "Alice" };

        #region nsub-migration-returns-nsub
        // NSubstitute's elegant fluent API
        substitute.GetUser(Arg.Any<int>()).Returns(testUser);
        #endregion

        INSubUserRepo repository = substitute;
        var user = repository.GetUser(42);

        Assert.NotNull(user);
        Assert.Equal("Alice", user.Name);
    }
}

public class ReturnsKnockOffTests
{
    [Fact]
    public void Returns_KnockOffApproach()
    {
        var stub = new NSubUserRepoStub();
        var testUser = new User { Id = 42, Name = "Alice" };

        #region nsub-migration-returns-knockoff
        // KnockOff uses Return with typed delegate
        stub.GetUser.Return((id) => testUser);
        #endregion

        INSubUserRepo repository = stub;
        var user = repository.GetUser(42);

        Assert.NotNull(user);
        Assert.Equal("Alice", user.Name);
    }
}

// =============================================================================
// Returns with Argument Access - Side-by-Side Comparison
// =============================================================================

public class ReturnsWithArgsNSubTests
{
    [Fact]
    public void ReturnsWithArgs_NSubstituteApproach()
    {
        var substitute = Substitute.For<INSubUserRepo>();

        #region nsub-migration-returns-args-nsub
        // NSubstitute: Access args through callInfo.Arg<T>()
        substitute.GetUser(Arg.Any<int>())
            .Returns(callInfo => new User { Id = callInfo.Arg<int>(), Name = $"User{callInfo.Arg<int>()}" });
        #endregion

        INSubUserRepo repository = substitute;
        var user = repository.GetUser(42);

        Assert.NotNull(user);
        Assert.Equal(42, user.Id);
        Assert.Equal("User42", user.Name);
    }
}

public class ReturnsWithArgsKnockOffTests
{
    [Fact]
    public void ReturnsWithArgs_KnockOffApproach()
    {
        var stub = new NSubUserRepoStub();

        #region nsub-migration-returns-args-knockoff
        // KnockOff: Arguments are directly available in the delegate
        stub.GetUser.Return((id) => new User { Id = id, Name = $"User{id}" });
        #endregion

        INSubUserRepo repository = stub;
        var user = repository.GetUser(42);

        Assert.NotNull(user);
        Assert.Equal(42, user.Id);
        Assert.Equal("User42", user.Name);
    }
}

// =============================================================================
// ReturnsForAnyArgs - Side-by-Side Comparison
// =============================================================================

public class ReturnsForAnyArgsNSubTests
{
    [Fact]
    public void ReturnsForAnyArgs_NSubstituteApproach()
    {
        var substitute = Substitute.For<INSubUserRepo>();
        var testUser = new User { Id = 1, Name = "Default" };

        #region nsub-migration-returns-anyargs-nsub
        // ReturnsForAnyArgs: matches any argument combination
        substitute.GetUser(default).ReturnsForAnyArgs(testUser);
        #endregion

        INSubUserRepo repository = substitute;
        var user1 = repository.GetUser(1);
        var user2 = repository.GetUser(999);

        Assert.Equal("Default", user1?.Name);
        Assert.Equal("Default", user2?.Name);
    }
}

public class ReturnsForAnyArgsKnockOffTests
{
    [Fact]
    public void ReturnsForAnyArgs_KnockOffApproach()
    {
        var stub = new NSubUserRepoStub();
        var testUser = new User { Id = 1, Name = "Default" };

        #region nsub-migration-returns-anyargs-knockoff
        // KnockOff: Return inherently matches any arguments (no "ForAnyArgs" needed)
        stub.GetUser.Return((id) => testUser);
        #endregion

        INSubUserRepo repository = stub;
        var user1 = repository.GetUser(1);
        var user2 = repository.GetUser(999);

        Assert.Equal("Default", user1?.Name);
        Assert.Equal("Default", user2?.Name);
    }
}

// =============================================================================
// Property Setup - Side-by-Side Comparison
// =============================================================================

public class PropertySetupNSubTests
{
    [Fact]
    public void PropertySetup_NSubstituteApproach()
    {
        var substitute = Substitute.For<INSubUserRepo>();

        #region nsub-migration-property-nsub
        // NSubstitute: elegant property Returns
        substitute.ConnectionString.Returns("server=localhost");
        substitute.IsConnected.Returns(true);
        #endregion

        INSubUserRepo repository = substitute;

        Assert.Equal("server=localhost", repository.ConnectionString);
        Assert.True(repository.IsConnected);
    }
}

public class PropertySetupKnockOffTests
{
    [Fact]
    public void PropertySetup_KnockOffApproach()
    {
        var stub = new NSubUserRepoStub();

        #region nsub-migration-property-knockoff
        // KnockOff: Use OnGet(value) for property getters
        stub.ConnectionString.OnGet("server=localhost");
        stub.IsConnected.OnGet(true);
        #endregion

        INSubUserRepo repository = stub;

        Assert.Equal("server=localhost", repository.ConnectionString);
        Assert.True(repository.IsConnected);
    }
}

// =============================================================================
// Received Verification - Side-by-Side Comparison
// =============================================================================

public class ReceivedNSubTests
{
    [Fact]
    public void Received_NSubstituteApproach()
    {
        var substitute = Substitute.For<INSubUserRepo>();

        INSubUserRepo repository = substitute;
        repository.SaveUser(new User { Id = 1, Name = "Bob" });

        #region nsub-migration-received-nsub
        // NSubstitute's intuitive Received() syntax
        substitute.Received().SaveUser(Arg.Any<User>());
        substitute.Received(1).SaveUser(Arg.Any<User>());
        #endregion
    }
}

public class ReceivedKnockOffTests
{
    [Fact]
    public void Received_KnockOffApproach()
    {
        var stub = new NSubUserRepoStub();

        #region nsub-migration-received-knockoff
        // KnockOff: Mark as verifiable during setup, then Verify()
        stub.SaveUser.Call((user) => { }).Verifiable();
        #endregion

        INSubUserRepo repository = stub;
        repository.SaveUser(new User { Id = 1, Name = "Bob" });

        // Verify() checks all members marked with .Verifiable()
        stub.Verify();

        // Or verify with explicit Times
        stub.SaveUser.Verify(Times.Once);
    }
}

// =============================================================================
// DidNotReceive Verification - Side-by-Side Comparison
// =============================================================================

public class DidNotReceiveNSubTests
{
    [Fact]
    public void DidNotReceive_NSubstituteApproach()
    {
        var substitute = Substitute.For<INSubUserRepo>();

        INSubUserRepo repository = substitute;
        // Don't call DeleteUser

        #region nsub-migration-didnotreceive-nsub
        // NSubstitute's DidNotReceive - beautifully readable
        substitute.DidNotReceive().DeleteUser(Arg.Any<int>());
        #endregion
    }
}

public class DidNotReceiveKnockOffTests
{
    [Fact]
    public void DidNotReceive_KnockOffApproach()
    {
        var stub = new NSubUserRepoStub();

        INSubUserRepo repository = stub;
        // Don't call DeleteUser

        #region nsub-migration-didnotreceive-knockoff
        // KnockOff: Use Verify(Times.Never) for "did not receive"
        stub.DeleteUser.Verify(Times.Never);
        #endregion
    }
}

// =============================================================================
// When...Do (Side Effects) - Side-by-Side Comparison
// =============================================================================

public class WhenDoNSubTests
{
    [Fact]
    public void WhenDo_NSubstituteApproach()
    {
        var substitute = Substitute.For<INSubUserRepo>();
        var savedUsers = new List<User>();

        #region nsub-migration-whendo-nsub
        // NSubstitute: When...Do for void methods with side effects
        substitute.When(x => x.SaveUser(Arg.Any<User>()))
            .Do(callInfo => savedUsers.Add(callInfo.Arg<User>()));
        #endregion

        INSubUserRepo repository = substitute;
        repository.SaveUser(new User { Id = 1, Name = "Alice" });
        repository.SaveUser(new User { Id = 2, Name = "Bob" });

        Assert.Equal(2, savedUsers.Count);
        Assert.Equal("Alice", savedUsers[0].Name);
        Assert.Equal("Bob", savedUsers[1].Name);
    }
}

public class WhenDoKnockOffTests
{
    [Fact]
    public void WhenDo_KnockOffApproach()
    {
        var stub = new NSubUserRepoStub();
        var savedUsers = new List<User>();

        #region nsub-migration-whendo-knockoff
        // KnockOff: Return handles side effects directly
        stub.SaveUser.Call((user) => { savedUsers.Add(user); });
        #endregion

        INSubUserRepo repository = stub;
        repository.SaveUser(new User { Id = 1, Name = "Alice" });
        repository.SaveUser(new User { Id = 2, Name = "Bob" });

        Assert.Equal(2, savedUsers.Count);
        Assert.Equal("Alice", savedUsers[0].Name);
        Assert.Equal("Bob", savedUsers[1].Name);
    }
}

// =============================================================================
// Returns And Does (Return + Side Effect) - Side-by-Side Comparison
// =============================================================================

public class ReturnsAndDoesNSubTests
{
    [Fact]
    public void ReturnsAndDoes_NSubstituteApproach()
    {
        var substitute = Substitute.For<INSubUserRepo>();
        var accessLog = new List<int>();

        #region nsub-migration-returnsanddoes-nsub
        // NSubstitute: AndDoes for side effects with return value
        substitute.GetUser(Arg.Any<int>())
            .Returns(callInfo => new User { Id = callInfo.Arg<int>(), Name = "Test" })
            .AndDoes(callInfo => accessLog.Add(callInfo.Arg<int>()));
        #endregion

        INSubUserRepo repository = substitute;
        var user1 = repository.GetUser(1);
        var user2 = repository.GetUser(2);

        Assert.Equal(new[] { 1, 2 }, accessLog);
        Assert.Equal("Test", user1?.Name);
    }
}

public class ReturnsAndDoesKnockOffTests
{
    [Fact]
    public void ReturnsAndDoes_KnockOffApproach()
    {
        var stub = new NSubUserRepoStub();
        var accessLog = new List<int>();

        #region nsub-migration-returnsanddoes-knockoff
        // KnockOff: Side effects and return in same delegate
        stub.GetUser.Return((id) => { accessLog.Add(id); return new User { Id = id, Name = "Test" }; });
        #endregion

        INSubUserRepo repository = stub;
        var user1 = repository.GetUser(1);
        var user2 = repository.GetUser(2);

        Assert.Equal(new[] { 1, 2 }, accessLog);
        Assert.Equal("Test", user1?.Name);
    }
}

// =============================================================================
// Argument Matchers - Side-by-Side Comparison
// =============================================================================

public class ArgMatchersNSubTests
{
    [Fact]
    public void ArgMatchers_NSubstituteApproach()
    {
        var substitute = Substitute.For<INSubUserRepo>();

        #region nsub-migration-argmatchers-nsub
        // Arg.Is<T>() for conditional matching
        substitute.GetUser(Arg.Is<int>(id => id > 0))
            .Returns(callInfo => new User { Id = callInfo.Arg<int>(), Name = "Valid User" });
        #endregion

        INSubUserRepo repository = substitute;

        var validUser = repository.GetUser(1);
        var invalidUser = repository.GetUser(-1);

        Assert.NotNull(validUser);
        Assert.Null(invalidUser); // No setup matched
    }
}

public class ArgMatchersKnockOffTests
{
    [Fact]
    public void ArgMatchers_KnockOffApproach()
    {
        var stub = new NSubUserRepoStub();

        #region nsub-migration-argmatchers-knockoff
        // KnockOff: Conditional logic in the callback
        stub.GetUser.Return((id) => id > 0 ? new User { Id = id, Name = "Valid User" } : null);
        #endregion

        INSubUserRepo repository = stub;

        var validUser = repository.GetUser(1);
        var invalidUser = repository.GetUser(-1);

        Assert.NotNull(validUser);
        Assert.Null(invalidUser);
    }
}

// =============================================================================
// Async Methods - Side-by-Side Comparison
// =============================================================================

public class AsyncMethodNSubTests
{
    [Fact]
    public async Task AsyncMethod_NSubstituteApproach()
    {
        var substitute = Substitute.For<INSubUserRepo>();
        var testUser = new User { Id = 42, Name = "Alice" };

        #region nsub-migration-async-nsub
        // NSubstitute: Returns works seamlessly with Task (auto-wraps)
        substitute.GetUserAsync(Arg.Any<int>()).Returns(testUser);
        #endregion

        INSubUserRepo repository = substitute;
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
        var stub = new NSubUserRepoStub();
        var testUser = new User { Id = 42, Name = "Alice" };

        #region nsub-migration-async-knockoff
        // KnockOff: Must wrap in Task.FromResult explicitly
        stub.GetUserAsync.Return((id) => Task.FromResult<User?>(testUser));
        #endregion

        INSubUserRepo repository = stub;
        var user = await repository.GetUserAsync(42);

        Assert.NotNull(user);
        Assert.Equal("Alice", user.Name);
    }
}

// =============================================================================
// Multiple Arguments - Side-by-Side Comparison
// =============================================================================

public class MultipleArgsNSubTests
{
    [Fact]
    public void MultipleArgs_NSubstituteApproach()
    {
        var substitute = Substitute.For<INSubUserRepo>();

        #region nsub-migration-multiargs-nsub
        // NSubstitute: Multiple Arg matchers, access via ArgAt<T>(index)
        substitute.FindUsers(Arg.Any<string>(), Arg.Is<int>(x => x > 0))
            .Returns(callInfo => new[] { new User { Name = callInfo.ArgAt<string>(0) } });
        #endregion

        INSubUserRepo repository = substitute;
        var users = repository.FindUsers("Alice", 10);

        Assert.Single(users);
        Assert.Equal("Alice", users.First().Name);
    }
}

public class MultipleArgsKnockOffTests
{
    [Fact]
    public void MultipleArgs_KnockOffApproach()
    {
        var stub = new NSubUserRepoStub();

        #region nsub-migration-multiargs-knockoff
        // KnockOff: Named parameters directly in delegate
        stub.FindUsers.Return((name, limit) =>
            limit <= 0 ? Enumerable.Empty<User>() : new[] { new User { Name = name } });
        #endregion

        INSubUserRepo repository = stub;
        var users = repository.FindUsers("Alice", 10);

        Assert.Single(users);
        Assert.Equal("Alice", users.First().Name);
    }
}

// =============================================================================
// Received with Specific Arguments - Side-by-Side Comparison
// =============================================================================

public class ReceivedWithArgsNSubTests
{
    [Fact]
    public void ReceivedWithArgs_NSubstituteApproach()
    {
        var substitute = Substitute.For<INSubUserRepo>();

        INSubUserRepo repository = substitute;
        repository.GetUser(42);
        repository.GetUser(99);

        #region nsub-migration-received-args-nsub
        // NSubstitute: Verify specific argument was used
        substitute.Received().GetUser(42);
        substitute.Received().GetUser(99);
        substitute.DidNotReceive().GetUser(1);
        #endregion
    }
}

public class ReceivedWithArgsKnockOffTests
{
    [Fact]
    public void ReceivedWithArgs_KnockOffApproach()
    {
        var stub = new NSubUserRepoStub();
        var calledIds = new List<int>();

        // Capture arguments for later verification
        stub.GetUser.Return((id) => { calledIds.Add(id); return null; });

        INSubUserRepo repository = stub;
        repository.GetUser(42);
        repository.GetUser(99);

        #region nsub-migration-received-args-knockoff
        // KnockOff: Inspect captured arguments or use LastArg
        Assert.Contains(42, calledIds);
        Assert.Equal(99, stub.GetUser.LastArg);
        #endregion
    }
}

// =============================================================================
// ClearReceivedCalls - Side-by-Side Comparison
// =============================================================================

public class ClearReceivedNSubTests
{
    [Fact]
    public void ClearReceived_NSubstituteApproach()
    {
        var substitute = Substitute.For<INSubUserRepo>();

        INSubUserRepo repository = substitute;
        repository.GetUser(1);
        repository.GetUser(2);

        #region nsub-migration-clear-nsub
        // NSubstitute: Clear all call history at once
        substitute.ClearReceivedCalls();
        #endregion

        substitute.DidNotReceive().GetUser(Arg.Any<int>());
    }
}

public class ClearReceivedKnockOffTests
{
    [Fact]
    public void ClearReceived_KnockOffApproach()
    {
        var stub = new NSubUserRepoStub();
        stub.GetUser.Return((id) => null);

        INSubUserRepo repository = stub;
        repository.GetUser(1);
        repository.GetUser(2);

        stub.GetUser.Verify(Times.Exactly(2));

        #region nsub-migration-clear-knockoff
        // KnockOff: Reset clears call tracking per-interceptor
        stub.GetUser.Reset();
        #endregion

        stub.GetUser.Verify(Times.Never);
    }
}

// =============================================================================
// Throwing Exceptions - Side-by-Side Comparison
// =============================================================================

public class ThrowsNSubTests
{
    [Fact]
    public void Throws_NSubstituteApproach()
    {
        var substitute = Substitute.For<INSubUserRepo>();

        #region nsub-migration-throws-nsub
        // NSubstitute: Throw in Returns callback
        substitute.GetUser(Arg.Any<int>())
            .Returns<User?>(_ => throw new InvalidOperationException("Database offline"));
        #endregion

        INSubUserRepo repository = substitute;

        Assert.Throws<InvalidOperationException>(() => repository.GetUser(1));
    }
}

public class ThrowsKnockOffTests
{
    [Fact]
    public void Throws_KnockOffApproach()
    {
        var stub = new NSubUserRepoStub();

        #region nsub-migration-throws-knockoff
        // KnockOff: Throw directly in callback
        stub.GetUser.Return((id) => throw new InvalidOperationException("Database offline"));
        #endregion

        INSubUserRepo repository = stub;

        Assert.Throws<InvalidOperationException>(() => repository.GetUser(1));
    }
}

// =============================================================================
// Complete Before/After Example - Service Class
// =============================================================================

public class UserServiceNSub
{
    private readonly INSubUserRepo _repository;

    public UserServiceNSub(INSubUserRepo repository)
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

    public bool TryDeleteUser(int id)
    {
        var user = _repository.GetUser(id);
        if (user == null) return false;

        _repository.DeleteUser(id);
        return true;
    }
}

// =============================================================================
// Complete Example - NSubstitute Version
// =============================================================================

public class CompleteNSubstituteTests
{
    #region nsub-migration-complete-nsub
    // NSubstitute: Create substitute in constructor
    private readonly INSubUserRepo _substitute = Substitute.For<INSubUserRepo>();

    // Setup: .Returns() for return values
    // _substitute.GetUserAsync(1).Returns(user);

    // Verification: .Received() after the call
    // await _substitute.Received(1).GetUserAsync(1);

    // Argument matching: Arg.Is<T>() predicates
    // _substitute.Received().SaveUser(Arg.Is<User>(u => u.Name == "Bob"));

    // Negative verification: .DidNotReceive()
    // _substitute.DidNotReceive().DeleteUser(Arg.Any<int>());
    #endregion

    private readonly UserServiceNSub _service;

    public CompleteNSubstituteTests()
    {
        _service = new UserServiceNSub(_substitute);
    }

    [Fact]
    public async Task GetUser_ReturnsUser()
    {
        var user = new User { Id = 1, Name = "Alice" };
        _substitute.GetUserAsync(1).Returns(user);

        var result = await _service.GetUserAsync(1);

        Assert.Equal("Alice", result?.Name);
        await _substitute.Received(1).GetUserAsync(1);
    }

    [Fact]
    public void SaveUser_CallsRepository()
    {
        var user = new User { Id = 1, Name = "Bob" };

        _service.SaveUser(user);

        _substitute.Received().SaveUser(Arg.Is<User>(u => u.Name == "Bob"));
    }

    [Fact]
    public void TryDeleteUser_WhenUserExists_DeletesAndReturnsTrue()
    {
        var user = new User { Id = 1, Name = "Charlie" };
        _substitute.GetUser(1).Returns(user);

        var result = _service.TryDeleteUser(1);

        Assert.True(result);
        _substitute.Received().DeleteUser(1);
    }

    [Fact]
    public void TryDeleteUser_WhenUserNotFound_ReturnsFalse()
    {
        _substitute.GetUser(1).Returns((User?)null);

        var result = _service.TryDeleteUser(1);

        Assert.False(result);
        _substitute.DidNotReceive().DeleteUser(Arg.Any<int>());
    }
}

// =============================================================================
// Complete Example - KnockOff Version
// =============================================================================

public class CompleteKnockOffNSubTests
{
    #region nsub-migration-complete-knockoff
    // KnockOff: Instantiate stub in constructor
    private readonly NSubUserRepoStub _stub = new NSubUserRepoStub();

    // Setup: .Return() with typed delegate, .Verifiable() for verification
    // _stub.GetUserAsync.Return((id) => Task.FromResult<User?>(user)).Verifiable();

    // Verification: .Verify() checks all .Verifiable() members
    // _stub.Verify();

    // Argument capture: capture in the callback delegate
    // _stub.SaveUser.Call((user) => { savedUser = user; }).Verifiable();

    // Negative verification: .Verify(Times.Never)
    // _stub.DeleteUser.Verify(Times.Never);
    #endregion

    private readonly UserServiceNSub _service;

    public CompleteKnockOffNSubTests()
    {
        _service = new UserServiceNSub(_stub);
    }

    [Fact]
    public async Task GetUser_ReturnsUser()
    {
        var user = new User { Id = 1, Name = "Alice" };
        _stub.GetUserAsync.Return((id) => Task.FromResult<User?>(user)).Verifiable();

        var result = await _service.GetUserAsync(1);

        Assert.Equal("Alice", result?.Name);
        _stub.Verify();
    }

    [Fact]
    public void SaveUser_CallsRepository()
    {
        User? savedUser = null;
        _stub.SaveUser.Call((user) => { savedUser = user; }).Verifiable();

        _service.SaveUser(new User { Id = 1, Name = "Bob" });

        _stub.Verify();
        Assert.Equal("Bob", savedUser?.Name);
    }

    [Fact]
    public void TryDeleteUser_WhenUserExists_DeletesAndReturnsTrue()
    {
        var user = new User { Id = 1, Name = "Charlie" };
        _stub.GetUser.Return((id) => user);
        _stub.DeleteUser.Call((id) => { }).Verifiable();

        var result = _service.TryDeleteUser(1);

        Assert.True(result);
        _stub.Verify();
    }

    [Fact]
    public void TryDeleteUser_WhenUserNotFound_ReturnsFalse()
    {
        _stub.GetUser.Return((id) => null);

        var result = _service.TryDeleteUser(1);

        Assert.False(result);
        _stub.DeleteUser.Verify(Times.Never);
    }
}

// =============================================================================
// When() API - Predicate Matching (Step 13b)
// =============================================================================

public class WhenPredicateNSubTests
{
    [Fact]
    public void WhenPredicate_NSubstituteApproach()
    {
        var substitute = Substitute.For<INSubUserRepo>();

        #region nsub-migration-when-predicate-nsub
        // Arg.Is<T>() with predicate per parameter for conditional matching
        substitute.GetUser(Arg.Is<int>(id => id > 0))
            .Returns(callInfo => new User { Id = callInfo.Arg<int>(), Name = "Valid User" });
        substitute.GetUser(Arg.Is<int>(id => id <= 0)).Returns((User?)null);
        #endregion

        INSubUserRepo repository = substitute;

        Assert.NotNull(repository.GetUser(1));
        Assert.NotNull(repository.GetUser(100));
        Assert.Null(repository.GetUser(0));
        Assert.Null(repository.GetUser(-5));
    }
}

public class WhenPredicateKnockOffTests
{
    [Fact]
    public void WhenPredicate_KnockOffApproach()
    {
        var stub = new NSubUserRepoStub();

        #region nsub-migration-when-predicate-knockoff
        // Return with conditionals for permanent predicate matching
        stub.GetUser.Return((id) => id > 0 ? new User { Id = id, Name = "Valid User" } : null);
        #endregion

        INSubUserRepo repository = stub;

        Assert.NotNull(repository.GetUser(1));
        Assert.NotNull(repository.GetUser(100));
        Assert.Null(repository.GetUser(0));
        Assert.Null(repository.GetUser(-5));
    }
}

// =============================================================================
// When() API - Value Matching (Step 13c)
// =============================================================================

public class WhenValuesNSubTests
{
    [Fact]
    public void WhenValues_NSubstituteApproach()
    {
        var substitute = Substitute.For<INSubUserRepo>();

        #region nsub-migration-when-values-nsub
        // Exact value matching with literals
        substitute.GetUser(42).Returns(new User { Id = 42, Name = "Alice" });
        substitute.GetUser(99).Returns(new User { Id = 99, Name = "Bob" });
        #endregion

        INSubUserRepo repository = substitute;

        Assert.Equal("Alice", repository.GetUser(42)?.Name);
        Assert.Equal("Bob", repository.GetUser(99)?.Name);
        Assert.Null(repository.GetUser(1)); // No match
    }
}

public class WhenValuesKnockOffTests
{
    [Fact]
    public void WhenValues_KnockOffApproach()
    {
        var stub = new NSubUserRepoStub();

        #region nsub-migration-when-values-knockoff
        // When() with exact values
        stub.GetUser.When(42).Return(new User { Id = 42, Name = "Alice" });
        stub.GetUser.When(99).Return(new User { Id = 99, Name = "Bob" });
        #endregion

        INSubUserRepo repository = stub;

        Assert.Equal("Alice", repository.GetUser(42)?.Name);
        Assert.Equal("Bob", repository.GetUser(99)?.Name);
        Assert.Null(repository.GetUser(1)); // No match
    }
}

// =============================================================================
// Interfaces and Types for Gotcha/Feature Samples
// =============================================================================

public class Customer
{
    public string Name { get; set; } = "";
}

public interface ICustomer
{
    string Name { get; set; }
}

public interface IOrder
{
    ICustomer Customer { get; set; }
}

public interface IOrderService
{
    Task<User?> GetUserAsync(int id);
}

[KnockOff]
public partial class NSubCustomerStub : ICustomer { }

[KnockOff]
public partial class NSubOrderStub : IOrder { }

[KnockOff]
public partial class NSubOrderServiceStub : IOrderService { }

// =============================================================================
// Common Gotchas - Async Configuration Options
// =============================================================================

public class GotchaAsyncOptionsTests
{
    [Fact]
    public async Task AsyncOptions_ThreeTiers()
    {
        var stub = new NSubOrderServiceStub();
        var testUser = new User { Id = 42, Name = "Alice" };

        #region nsub-gotcha-async-options
        // 1. Return() -- auto-wraps in Task.FromResult (recommended for fixed values)
        stub.GetUserAsync.Return(testUser);

        // 2. Return() simplified -- callback returns unwrapped type, auto-wrapped
        stub.GetUserAsync.Return((id) => new User { Id = id });

        // 3. Return() full -- callback returns Task<T> directly
        stub.GetUserAsync.Return((id) => Task.FromResult<User?>(testUser));
        #endregion

        IOrderService service = stub;
        var user = await service.GetUserAsync(42);

        Assert.NotNull(user);
    }
}

// =============================================================================
// Common Gotchas - Missing Verifiable
// =============================================================================

public class GotchaVerifiableTests
{
    [Fact]
    public void Verifiable_Required()
    {
        var stub = new NSubUserRepoStub();

        #region nsub-gotcha-verifiable
        // Wrong: Returns without Verifiable -- Verify() won't check this
        stub.GetUser.Return((id) => new User { Id = id });

        // Correct: Mark as Verifiable
        stub.GetUser.Return((id) => new User { Id = id }).Verifiable();
        #endregion

        INSubUserRepo repository = stub;
        repository.GetUser(1);

        stub.Verify();
    }
}

// =============================================================================
// Common Gotchas - Async Simple
// =============================================================================

public class GotchaAsyncSimpleTests
{
    [Fact]
    public async Task AsyncSimple_UseReturns()
    {
        var stub = new NSubOrderServiceStub();
        var testUser = new User { Id = 42, Name = "Alice" };

        #region nsub-gotcha-async-simple
        // Verbose: full Return with Task.FromResult
        stub.GetUserAsync.Return((id) => Task.FromResult<User?>(testUser));

        // Simpler: Return() auto-wraps for you
        stub.GetUserAsync.Return(testUser);
        #endregion

        IOrderService service = stub;
        var user = await service.GetUserAsync(42);

        Assert.NotNull(user);
    }
}

// =============================================================================
// Common Gotchas - Received Syntax
// =============================================================================

public class GotchaReceivedSyntaxTests
{
    [Fact]
    public void ReceivedSyntax_NotAvailable()
    {
        var stub = new NSubUserRepoStub();
        stub.GetUser.Return((id) => null);

        INSubUserRepo repository = stub;
        repository.GetUser(42);

        #region nsub-gotcha-received-syntax
        // Wrong: NSubstitute syntax doesn't exist in KnockOff
        // stub.Received().GetUser(42);          // Compile error
        // stub.DidNotReceive().DeleteUser(1);    // Compile error

        // Correct: Use Verify with Times
        stub.GetUser.Verify(Times.Once);
        stub.DeleteUser.Verify(Times.Never);
        #endregion
    }
}

// =============================================================================
// Common Gotchas - Partial Keyword
// =============================================================================

#region nsub-gotcha-partial-keyword
// Wrong: missing partial keyword
// [KnockOff]
// public class MyStub : INSubUserRepo { }  // won't generate interceptors

// Correct: always use partial
[KnockOff]
public partial class NSubGotchaPartialStub : INSubUserRepo { }
#endregion

// =============================================================================
// Common Gotchas - Return Signature
// =============================================================================

public class GotchaReturnSignatureTests
{
    [Fact]
    public void ReturnSignature_MustMatch()
    {
        var stub = new NSubUserRepoStub();

        #region nsub-gotcha-oncall-signature
        // Wrong: GetUser(int id) expects (int) callback
        // stub.GetUser.Return(() => user);  // Compile error

        // Correct: match the method signature
        stub.GetUser.Return((id) => new User { Id = id });
        #endregion

        INSubUserRepo repository = stub;
        var user = repository.GetUser(1);

        Assert.NotNull(user);
    }
}

// =============================================================================
// Features Not Supported - Recursive Mocks
// =============================================================================

public class RecursiveMocksTests
{
    [Fact]
    public void RecursiveMocks_NSubstitute()
    {
        #region nsub-no-recursive-nsub
        // NSubstitute: auto-creates substitutes for return types
        var order = Substitute.For<IOrder>();
        // order.Customer automatically returns a substitute
        order.Customer.Name.Returns("Alice");
        #endregion

        Assert.Equal("Alice", order.Customer.Name);
    }

    [Fact]
    public void RecursiveMocks_KnockOff()
    {
        #region nsub-no-recursive-knockoff
        // KnockOff: Create each stub explicitly
        var customerStub = new NSubCustomerStub();
        customerStub.Name.OnGet("Alice");

        var orderStub = new NSubOrderStub();
        orderStub.Customer.OnGet(customerStub);
        #endregion

        IOrder order = orderStub;
        Assert.Equal("Alice", order.Customer.Name);
    }
}

// =============================================================================
// Features Not Supported - Arg.Do
// =============================================================================

public class ArgDoTests
{
    [Fact]
    public void ArgDo_NSubstitute()
    {
        var substitute = Substitute.For<INSubUserRepo>();
        var capturedIds = new List<int>();

        #region nsub-no-argdo-nsub
        // NSubstitute: Arg.Do<T>() captures arguments during setup
        substitute.GetUser(Arg.Do<int>(id => capturedIds.Add(id))).Returns((User?)null);
        #endregion

        INSubUserRepo repository = substitute;
        repository.GetUser(1);
        repository.GetUser(2);

        Assert.Equal(new[] { 1, 2 }, capturedIds);
    }

    [Fact]
    public void ArgDo_KnockOff()
    {
        var stub = new NSubUserRepoStub();
        var capturedIds = new List<int>();

        #region nsub-no-argdo-knockoff
        // KnockOff: Capture in the callback
        stub.GetUser.Return((id) => { capturedIds.Add(id); return null; });
        #endregion

        INSubUserRepo repository = stub;
        repository.GetUser(1);
        repository.GetUser(2);

        Assert.Equal(new[] { 1, 2 }, capturedIds);
    }
}

// =============================================================================
// Features - Sequences
// =============================================================================

public class SequenceTests
{
    [Fact]
    public void Sequence_NSubstitute()
    {
        var substitute = Substitute.For<INSubUserRepo>();
        var user1 = new User { Id = 1, Name = "First" };
        var user2 = new User { Id = 2, Name = "Second" };
        var user3 = new User { Id = 3, Name = "Third" };

        #region nsub-sequence-nsub
        // NSubstitute: multiple values in Returns()
        substitute.GetUser(Arg.Any<int>()).Returns(user1, user2, user3);
        #endregion

        INSubUserRepo repository = substitute;

        Assert.Equal("First", repository.GetUser(1)?.Name);
        Assert.Equal("Second", repository.GetUser(2)?.Name);
        Assert.Equal("Third", repository.GetUser(3)?.Name);
    }

    [Fact]
    public void Sequence_KnockOff()
    {
        var stub = new NSubUserRepoStub();
        var user1 = new User { Id = 1, Name = "First" };
        var user2 = new User { Id = 2, Name = "Second" };
        var user3 = new User { Id = 3, Name = "Third" };

        #region nsub-sequence-knockoff
        // KnockOff: identical syntax -- multiple values in Return()
        stub.GetUser.Return(user1, user2, user3);
        #endregion

        INSubUserRepo repository = stub;

        Assert.Equal("First", repository.GetUser(1)?.Name);
        Assert.Equal("Second", repository.GetUser(2)?.Name);
        Assert.Equal("Third", repository.GetUser(3)?.Name);
    }

    [Fact]
    public void Sequence_Advanced_KnockOff()
    {
        var stub = new NSubUserRepoStub();
        var user1 = new User { Id = 1, Name = "First" };

        #region nsub-sequence-advanced
        // KnockOff extension: Return/ThenCall for computed sequences
        stub.GetUser
            .Return((id) => user1)
            .ThenReturn((id) => new User { Id = id, Name = "Subsequent" });
        #endregion

        INSubUserRepo repository = stub;

        Assert.Equal("First", repository.GetUser(1)?.Name);
        Assert.Equal("Subsequent", repository.GetUser(2)?.Name);
        Assert.Equal("Subsequent", repository.GetUser(3)?.Name);
    }
}
