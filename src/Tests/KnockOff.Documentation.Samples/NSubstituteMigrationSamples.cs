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
    #region nsub-migration-create-stub-nsub
    [Fact]
    public void CreateStub_NSubstituteApproach()
    {
        var substitute = Substitute.For<INSubUserRepo>();
        INSubUserRepo repository = substitute;

        Assert.NotNull(repository);
    }
    #endregion
}

public class CreateStubKnockOffTests
{
    #region nsub-migration-create-stub-knockoff
    [Fact]
    public void CreateStub_KnockOffApproach()
    {
        var stub = new NSubUserRepoStub();
        INSubUserRepo repository = stub;

        Assert.NotNull(repository);
    }
    #endregion
}

// =============================================================================
// Returns Setup - Side-by-Side Comparison
// =============================================================================

public class ReturnsNSubTests
{
    #region nsub-migration-returns-nsub
    [Fact]
    public void Returns_NSubstituteApproach()
    {
        var substitute = Substitute.For<INSubUserRepo>();
        var testUser = new User { Id = 42, Name = "Alice" };

        // NSubstitute's elegant fluent API
        substitute.GetUser(Arg.Any<int>()).Returns(testUser);

        INSubUserRepo repository = substitute;
        var user = repository.GetUser(42);

        Assert.NotNull(user);
        Assert.Equal("Alice", user.Name);
    }
    #endregion
}

public class ReturnsKnockOffTests
{
    #region nsub-migration-returns-knockoff
    [Fact]
    public void Returns_KnockOffApproach()
    {
        var stub = new NSubUserRepoStub();
        var testUser = new User { Id = 42, Name = "Alice" };

        // KnockOff uses OnCall with typed delegate
        stub.GetUser.OnCall((id) => testUser);

        INSubUserRepo repository = stub;
        var user = repository.GetUser(42);

        Assert.NotNull(user);
        Assert.Equal("Alice", user.Name);
    }
    #endregion
}

// =============================================================================
// Returns with Argument Access - Side-by-Side Comparison
// =============================================================================

public class ReturnsWithArgsNSubTests
{
    #region nsub-migration-returns-args-nsub
    [Fact]
    public void ReturnsWithArgs_NSubstituteApproach()
    {
        var substitute = Substitute.For<INSubUserRepo>();

        // NSubstitute: Access args through callback in Returns
        substitute.GetUser(Arg.Any<int>())
            .Returns(callInfo => new User
            {
                Id = callInfo.Arg<int>(),
                Name = $"User{callInfo.Arg<int>()}"
            });

        INSubUserRepo repository = substitute;
        var user = repository.GetUser(42);

        Assert.NotNull(user);
        Assert.Equal(42, user.Id);
        Assert.Equal("User42", user.Name);
    }
    #endregion
}

public class ReturnsWithArgsKnockOffTests
{
    #region nsub-migration-returns-args-knockoff
    [Fact]
    public void ReturnsWithArgs_KnockOffApproach()
    {
        var stub = new NSubUserRepoStub();

        // KnockOff: Arguments are directly available in the delegate
        stub.GetUser.OnCall((id) => new User
        {
            Id = id,
            Name = $"User{id}"
        });

        INSubUserRepo repository = stub;
        var user = repository.GetUser(42);

        Assert.NotNull(user);
        Assert.Equal(42, user.Id);
        Assert.Equal("User42", user.Name);
    }
    #endregion
}

// =============================================================================
// ReturnsForAnyArgs - Side-by-Side Comparison
// =============================================================================

public class ReturnsForAnyArgsNSubTests
{
    #region nsub-migration-returns-anyargs-nsub
    [Fact]
    public void ReturnsForAnyArgs_NSubstituteApproach()
    {
        var substitute = Substitute.For<INSubUserRepo>();
        var testUser = new User { Id = 1, Name = "Default" };

        // ReturnsForAnyArgs: matches any argument combination
        substitute.GetUser(default).ReturnsForAnyArgs(testUser);

        INSubUserRepo repository = substitute;
        var user1 = repository.GetUser(1);
        var user2 = repository.GetUser(999);

        Assert.Equal("Default", user1?.Name);
        Assert.Equal("Default", user2?.Name);
    }
    #endregion
}

public class ReturnsForAnyArgsKnockOffTests
{
    #region nsub-migration-returns-anyargs-knockoff
    [Fact]
    public void ReturnsForAnyArgs_KnockOffApproach()
    {
        var stub = new NSubUserRepoStub();
        var testUser = new User { Id = 1, Name = "Default" };

        // KnockOff: OnCall inherently matches any arguments
        // (no separate "ForAnyArgs" needed)
        stub.GetUser.OnCall((id) => testUser);

        INSubUserRepo repository = stub;
        var user1 = repository.GetUser(1);
        var user2 = repository.GetUser(999);

        Assert.Equal("Default", user1?.Name);
        Assert.Equal("Default", user2?.Name);
    }
    #endregion
}

// =============================================================================
// Property Setup - Side-by-Side Comparison
// =============================================================================

public class PropertySetupNSubTests
{
    #region nsub-migration-property-nsub
    [Fact]
    public void PropertySetup_NSubstituteApproach()
    {
        var substitute = Substitute.For<INSubUserRepo>();

        // NSubstitute: elegant property Returns
        substitute.ConnectionString.Returns("server=localhost");
        substitute.IsConnected.Returns(true);

        INSubUserRepo repository = substitute;

        Assert.Equal("server=localhost", repository.ConnectionString);
        Assert.True(repository.IsConnected);
    }
    #endregion
}

public class PropertySetupKnockOffTests
{
    #region nsub-migration-property-knockoff
    [Fact]
    public void PropertySetup_KnockOffApproach()
    {
        var stub = new NSubUserRepoStub();

        // KnockOff: Use OnGet(value) for all properties
        stub.ConnectionString.OnGet("server=localhost");
        // For read-only properties, also use OnGet(value)
        stub.IsConnected.OnGet(true);

        INSubUserRepo repository = stub;

        Assert.Equal("server=localhost", repository.ConnectionString);
        Assert.True(repository.IsConnected);
    }
    #endregion
}

// =============================================================================
// Received Verification - Side-by-Side Comparison
// =============================================================================

public class ReceivedNSubTests
{
    #region nsub-migration-received-nsub
    [Fact]
    public void Received_NSubstituteApproach()
    {
        var substitute = Substitute.For<INSubUserRepo>();

        INSubUserRepo repository = substitute;
        repository.SaveUser(new User { Id = 1, Name = "Bob" });

        // NSubstitute's intuitive Received() syntax
        substitute.Received().SaveUser(Arg.Any<User>());
        substitute.Received(1).SaveUser(Arg.Any<User>());
    }
    #endregion
}

public class ReceivedKnockOffTests
{
    #region nsub-migration-received-knockoff
    [Fact]
    public void Received_KnockOffApproach()
    {
        var stub = new NSubUserRepoStub();

        // Mark method as verifiable during setup
        stub.SaveUser.OnCall((user) => { }).Verifiable();

        INSubUserRepo repository = stub;
        repository.SaveUser(new User { Id = 1, Name = "Bob" });

        // Verify() checks all members marked with .Verifiable()
        stub.Verify();

        // Or verify with explicit Times via tracking
        stub.SaveUser.Verify(Times.Once);
    }
    #endregion
}

// =============================================================================
// DidNotReceive Verification - Side-by-Side Comparison
// =============================================================================

public class DidNotReceiveNSubTests
{
    #region nsub-migration-didnotreceive-nsub
    [Fact]
    public void DidNotReceive_NSubstituteApproach()
    {
        var substitute = Substitute.For<INSubUserRepo>();

        INSubUserRepo repository = substitute;
        // Don't call DeleteUser

        // NSubstitute's DidNotReceive - beautifully readable
        substitute.DidNotReceive().DeleteUser(Arg.Any<int>());
    }
    #endregion
}

public class DidNotReceiveKnockOffTests
{
    #region nsub-migration-didnotreceive-knockoff
    [Fact]
    public void DidNotReceive_KnockOffApproach()
    {
        var stub = new NSubUserRepoStub();
        var tracking = stub.DeleteUser.OnCall((id) => { });

        INSubUserRepo repository = stub;
        // Don't call DeleteUser

        // KnockOff: Use Times.Never for "did not receive"
        tracking.Verify(Times.Never);
    }
    #endregion
}

// =============================================================================
// When...Do (Side Effects) - Side-by-Side Comparison
// =============================================================================

public class WhenDoNSubTests
{
    #region nsub-migration-whendo-nsub
    [Fact]
    public void WhenDo_NSubstituteApproach()
    {
        var substitute = Substitute.For<INSubUserRepo>();
        var savedUsers = new List<User>();

        // NSubstitute: When...Do for void methods with side effects
        substitute.When(x => x.SaveUser(Arg.Any<User>()))
            .Do(callInfo => savedUsers.Add(callInfo.Arg<User>()));

        INSubUserRepo repository = substitute;
        repository.SaveUser(new User { Id = 1, Name = "Alice" });
        repository.SaveUser(new User { Id = 2, Name = "Bob" });

        Assert.Equal(2, savedUsers.Count);
        Assert.Equal("Alice", savedUsers[0].Name);
        Assert.Equal("Bob", savedUsers[1].Name);
    }
    #endregion
}

public class WhenDoKnockOffTests
{
    #region nsub-migration-whendo-knockoff
    [Fact]
    public void WhenDo_KnockOffApproach()
    {
        var stub = new NSubUserRepoStub();
        var savedUsers = new List<User>();

        // KnockOff: OnCall handles side effects directly
        stub.SaveUser.OnCall((user) =>
        {
            savedUsers.Add(user);
        });

        INSubUserRepo repository = stub;
        repository.SaveUser(new User { Id = 1, Name = "Alice" });
        repository.SaveUser(new User { Id = 2, Name = "Bob" });

        Assert.Equal(2, savedUsers.Count);
        Assert.Equal("Alice", savedUsers[0].Name);
        Assert.Equal("Bob", savedUsers[1].Name);
    }
    #endregion
}

// =============================================================================
// Returns And Does (Return + Side Effect) - Side-by-Side Comparison
// =============================================================================

public class ReturnsAndDoesNSubTests
{
    #region nsub-migration-returnsanddoes-nsub
    [Fact]
    public void ReturnsAndDoes_NSubstituteApproach()
    {
        var substitute = Substitute.For<INSubUserRepo>();
        var accessLog = new List<int>();

        // NSubstitute: AndDoes for side effects with return value
        substitute.GetUser(Arg.Any<int>())
            .Returns(callInfo => new User { Id = callInfo.Arg<int>(), Name = "Test" })
            .AndDoes(callInfo => accessLog.Add(callInfo.Arg<int>()));

        INSubUserRepo repository = substitute;
        var user1 = repository.GetUser(1);
        var user2 = repository.GetUser(2);

        Assert.Equal(new[] { 1, 2 }, accessLog);
        Assert.Equal("Test", user1?.Name);
    }
    #endregion
}

public class ReturnsAndDoesKnockOffTests
{
    #region nsub-migration-returnsanddoes-knockoff
    [Fact]
    public void ReturnsAndDoes_KnockOffApproach()
    {
        var stub = new NSubUserRepoStub();
        var accessLog = new List<int>();

        // KnockOff: Side effects and return in same delegate
        stub.GetUser.OnCall((id) =>
        {
            accessLog.Add(id);
            return new User { Id = id, Name = "Test" };
        });

        INSubUserRepo repository = stub;
        var user1 = repository.GetUser(1);
        var user2 = repository.GetUser(2);

        Assert.Equal(new[] { 1, 2 }, accessLog);
        Assert.Equal("Test", user1?.Name);
    }
    #endregion
}

// =============================================================================
// Argument Matchers - Side-by-Side Comparison
// =============================================================================

public class ArgMatchersNSubTests
{
    #region nsub-migration-argmatchers-nsub
    [Fact]
    public void ArgMatchers_NSubstituteApproach()
    {
        var substitute = Substitute.For<INSubUserRepo>();

        // Arg.Is<T>() for conditional matching
        substitute.GetUser(Arg.Is<int>(id => id > 0))
            .Returns(callInfo => new User
            {
                Id = callInfo.Arg<int>(),
                Name = "Valid User"
            });

        INSubUserRepo repository = substitute;

        var validUser = repository.GetUser(1);
        var invalidUser = repository.GetUser(-1);

        Assert.NotNull(validUser);
        Assert.Null(invalidUser); // No setup matched
    }
    #endregion
}

public class ArgMatchersKnockOffTests
{
    #region nsub-migration-argmatchers-knockoff
    [Fact]
    public void ArgMatchers_KnockOffApproach()
    {
        var stub = new NSubUserRepoStub();

        // KnockOff: Conditional logic in the callback
        stub.GetUser.OnCall((id) =>
            id > 0 ? new User { Id = id, Name = "Valid User" } : null);

        INSubUserRepo repository = stub;

        var validUser = repository.GetUser(1);
        var invalidUser = repository.GetUser(-1);

        Assert.NotNull(validUser);
        Assert.Null(invalidUser);
    }
    #endregion
}

// =============================================================================
// Async Methods - Side-by-Side Comparison
// =============================================================================

public class AsyncMethodNSubTests
{
    #region nsub-migration-async-nsub
    [Fact]
    public async Task AsyncMethod_NSubstituteApproach()
    {
        var substitute = Substitute.For<INSubUserRepo>();
        var testUser = new User { Id = 42, Name = "Alice" };

        // NSubstitute: Returns works seamlessly with Task
        substitute.GetUserAsync(Arg.Any<int>()).Returns(testUser);

        INSubUserRepo repository = substitute;
        var user = await repository.GetUserAsync(42);

        Assert.NotNull(user);
        Assert.Equal("Alice", user.Name);
    }
    #endregion
}

public class AsyncMethodKnockOffTests
{
    #region nsub-migration-async-knockoff
    [Fact]
    public async Task AsyncMethod_KnockOffApproach()
    {
        var stub = new NSubUserRepoStub();
        var testUser = new User { Id = 42, Name = "Alice" };

        // KnockOff: Must wrap in Task.FromResult explicitly
        stub.GetUserAsync.OnCall((id) => Task.FromResult<User?>(testUser));

        INSubUserRepo repository = stub;
        var user = await repository.GetUserAsync(42);

        Assert.NotNull(user);
        Assert.Equal("Alice", user.Name);
    }
    #endregion
}

// =============================================================================
// Multiple Arguments - Side-by-Side Comparison
// =============================================================================

public class MultipleArgsNSubTests
{
    #region nsub-migration-multiargs-nsub
    [Fact]
    public void MultipleArgs_NSubstituteApproach()
    {
        var substitute = Substitute.For<INSubUserRepo>();

        // NSubstitute: Multiple Arg matchers
        substitute.FindUsers(Arg.Any<string>(), Arg.Is<int>(x => x > 0))
            .Returns(callInfo => new[]
            {
                new User { Name = callInfo.ArgAt<string>(0) }
            });

        INSubUserRepo repository = substitute;
        var users = repository.FindUsers("Alice", 10);

        Assert.Single(users);
        Assert.Equal("Alice", users.First().Name);
    }
    #endregion
}

public class MultipleArgsKnockOffTests
{
    #region nsub-migration-multiargs-knockoff
    [Fact]
    public void MultipleArgs_KnockOffApproach()
    {
        var stub = new NSubUserRepoStub();

        // KnockOff: Named parameters directly in delegate
        stub.FindUsers.OnCall((name, limit) =>
        {
            if (limit <= 0) return Enumerable.Empty<User>();
            return new[] { new User { Name = name } };
        });

        INSubUserRepo repository = stub;
        var users = repository.FindUsers("Alice", 10);

        Assert.Single(users);
        Assert.Equal("Alice", users.First().Name);
    }
    #endregion
}

// =============================================================================
// Received with Specific Arguments - Side-by-Side Comparison
// =============================================================================

public class ReceivedWithArgsNSubTests
{
    #region nsub-migration-received-args-nsub
    [Fact]
    public void ReceivedWithArgs_NSubstituteApproach()
    {
        var substitute = Substitute.For<INSubUserRepo>();

        INSubUserRepo repository = substitute;
        repository.GetUser(42);
        repository.GetUser(99);

        // NSubstitute: Verify specific argument was used
        substitute.Received().GetUser(42);
        substitute.Received().GetUser(99);
        substitute.DidNotReceive().GetUser(1);
    }
    #endregion
}

public class ReceivedWithArgsKnockOffTests
{
    #region nsub-migration-received-args-knockoff
    [Fact]
    public void ReceivedWithArgs_KnockOffApproach()
    {
        var stub = new NSubUserRepoStub();
        var calledIds = new List<int>();

        // Capture arguments for later verification
        stub.GetUser.OnCall((id) =>
        {
            calledIds.Add(id);
            return null;
        });

        INSubUserRepo repository = stub;
        repository.GetUser(42);
        repository.GetUser(99);

        // KnockOff: Inspect captured arguments
        Assert.Contains(42, calledIds);
        Assert.Contains(99, calledIds);
        Assert.DoesNotContain(1, calledIds);

        // Or use LastCallArg for the most recent call
        Assert.Equal(99, stub.GetUser.LastCallArg);
    }
    #endregion
}

// =============================================================================
// ClearReceivedCalls - Side-by-Side Comparison
// =============================================================================

public class ClearReceivedNSubTests
{
    #region nsub-migration-clear-nsub
    [Fact]
    public void ClearReceived_NSubstituteApproach()
    {
        var substitute = Substitute.For<INSubUserRepo>();

        INSubUserRepo repository = substitute;
        repository.GetUser(1);
        repository.GetUser(2);

        // NSubstitute: Clear call history
        substitute.ClearReceivedCalls();

        substitute.DidNotReceive().GetUser(Arg.Any<int>());
    }
    #endregion
}

public class ClearReceivedKnockOffTests
{
    #region nsub-migration-clear-knockoff
    [Fact]
    public void ClearReceived_KnockOffApproach()
    {
        var stub = new NSubUserRepoStub();
        var tracking = stub.GetUser.OnCall((id) => null);

        INSubUserRepo repository = stub;
        repository.GetUser(1);
        repository.GetUser(2);

        // Verify calls were made
        tracking.Verify(Times.Exactly(2));

        // KnockOff: Reset clears call tracking
        stub.GetUser.Reset();

        // Now verify no calls
        tracking.Verify(Times.Never);
    }
    #endregion
}

// =============================================================================
// Throwing Exceptions - Side-by-Side Comparison
// =============================================================================

public class ThrowsNSubTests
{
    #region nsub-migration-throws-nsub
    [Fact]
    public void Throws_NSubstituteApproach()
    {
        var substitute = Substitute.For<INSubUserRepo>();

        // NSubstitute: Throws extension
        substitute.GetUser(Arg.Any<int>())
            .Returns<User?>(_ => throw new InvalidOperationException("Database offline"));

        INSubUserRepo repository = substitute;

        Assert.Throws<InvalidOperationException>(() => repository.GetUser(1));
    }
    #endregion
}

public class ThrowsKnockOffTests
{
    #region nsub-migration-throws-knockoff
    [Fact]
    public void Throws_KnockOffApproach()
    {
        var stub = new NSubUserRepoStub();

        // KnockOff: Throw directly in callback
        stub.GetUser.OnCall((id) =>
            throw new InvalidOperationException("Database offline"));

        INSubUserRepo repository = stub;

        Assert.Throws<InvalidOperationException>(() => repository.GetUser(1));
    }
    #endregion
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
    private readonly INSubUserRepo _substitute;
    private readonly UserServiceNSub _service;

    public CompleteNSubstituteTests()
    {
        _substitute = Substitute.For<INSubUserRepo>();
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
    #endregion
}

// =============================================================================
// Complete Example - KnockOff Version
// =============================================================================

public class CompleteKnockOffNSubTests
{
    #region nsub-migration-complete-knockoff
    private readonly NSubUserRepoStub _stub;
    private readonly UserServiceNSub _service;

    public CompleteKnockOffNSubTests()
    {
        _stub = new NSubUserRepoStub();
        _service = new UserServiceNSub(_stub);
    }

    [Fact]
    public async Task GetUser_ReturnsUser()
    {
        var user = new User { Id = 1, Name = "Alice" };
        _stub.GetUserAsync.OnCall((id) => Task.FromResult<User?>(user)).Verifiable();

        var result = await _service.GetUserAsync(1);

        Assert.Equal("Alice", result?.Name);
        _stub.Verify();
    }

    [Fact]
    public void SaveUser_CallsRepository()
    {
        User? savedUser = null;
        _stub.SaveUser.OnCall((user) =>
        {
            savedUser = user;
        }).Verifiable();

        _service.SaveUser(new User { Id = 1, Name = "Bob" });

        _stub.Verify();
        Assert.Equal("Bob", savedUser?.Name);
    }

    [Fact]
    public void TryDeleteUser_WhenUserExists_DeletesAndReturnsTrue()
    {
        var user = new User { Id = 1, Name = "Charlie" };
        _stub.GetUser.OnCall((id) => user);
        _stub.DeleteUser.OnCall((id) => { }).Verifiable();

        var result = _service.TryDeleteUser(1);

        Assert.True(result);
        _stub.Verify();
    }

    [Fact]
    public void TryDeleteUser_WhenUserNotFound_ReturnsFalse()
    {
        _stub.GetUser.OnCall((id) => null);
        var deleteTracking = _stub.DeleteUser.OnCall((id) => { });

        var result = _service.TryDeleteUser(1);

        Assert.False(result);
        deleteTracking.Verify(Times.Never);
    }
    #endregion
}
