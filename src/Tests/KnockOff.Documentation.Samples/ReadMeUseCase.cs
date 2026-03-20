using NSubstitute;

namespace KnockOff.Documentation.Samples.Readme;

// =============================================================================
// Interface: The contract our code depends on
// =============================================================================

#region readme-repo-interface
public interface IUserRepository
{
    void Add(User user);
    User? GetById(int id);
    List<User> GetAll();
    bool Delete(int id);
}
#endregion

// =============================================================================
// The Split-Abstraction Problem: NSubstitute requires two separate objects
// =============================================================================

public class NSubstituteSplitAbstractionExample
{
    #region readme-nsub-split-abstraction
    public static IUserRepository CreateNSubstituteRepository(List<User> users)
    {
        var repo = Substitute.For<IUserRepository>();

        // Wire each method to the backing list via lambda callbacks
        repo.When(x => x.Add(Arg.Any<User>()))
            .Do(callInfo => users.Add(callInfo.Arg<User>()));

        repo.GetById(Arg.Any<int>())
            .Returns(callInfo => users.SingleOrDefault(u => u.Id == callInfo.Arg<int>()));

        repo.GetAll()
            .Returns(callInfo => users.ToList());

        repo.Delete(Arg.Any<int>())
            .Returns(callInfo =>
            {
                var id = callInfo.Arg<int>();
                var user = users.SingleOrDefault(u => u.Id == id);
                return user != null && users.Remove(user);
            });

        return repo;
    }
    #endregion
}

// =============================================================================
// The Manual Solution: Hand-written fake with full boilerplate
// =============================================================================

#region readme-manual-fake
public class ManualUserRepositoryFake(List<User> users) : IUserRepository
{
    public void Add(User user) => users.Add(user);

    public User? GetById(int id) => users.SingleOrDefault(u => u.Id == id);

    public List<User> GetAll() => users.ToList();

    public bool Delete(int id)
    {
        var user = users.SingleOrDefault(u => u.Id == id);
        return user != null && users.Remove(user);
    }
    // Every new interface member requires a manual implementation here
}
#endregion

// =============================================================================
// The KnockOff Solution: Stub overrides + full mock capabilities
// =============================================================================

#region readme-knockoff-fake
[KnockOff]
public partial class ReadmeUserRepositoryStub(List<User> users) : IUserRepository
{
    protected override void Add_(User user) => users.Add(user);

    protected override User? GetById_(int id) => users.SingleOrDefault(u => u.Id == id);

    protected override List<User> GetAll_() => users.ToList();

    protected override bool Delete_(int id)
    {
        var user = users.SingleOrDefault(u => u.Id == id);
        return user != null && users.Remove(user);
    }
}
#endregion

// =============================================================================
// Tests: Demonstrating KnockOff's fake repository in action
// =============================================================================

public class FakeRepositoryTests
{
    [Fact]
    public void AddAndQuery()
    {
        #region readme-fake-add-and-query
        var stub = new ReadmeUserRepositoryStub(new List<User>());
        IUserRepository repo = stub;

        // Add users through the interface
        repo.Add(new User { Id = 1, Name = "Alice" });
        repo.Add(new User { Id = 2, Name = "Bob" });

        // Query them back — the stub owns its state
        var alice = repo.GetById(1);
        Assert.NotNull(alice);
        Assert.Equal("Alice", alice.Name);

        var all = repo.GetAll();
        Assert.Equal(2, all.Count);
        #endregion
    }

    [Fact]
    public void VerifyCalls()
    {
        #region readme-fake-verify
        var stub = new ReadmeUserRepositoryStub(new List<User>
        {
            new() { Id = 1, Name = "Alice" }
        });
        IUserRepository repo = stub;

        // Delete through the interface
        var deleted = repo.Delete(1);
        Assert.True(deleted);

        // Verify the call was made — it's still a full mock
        stub.Delete.Verify(Called.Once);
        #endregion
    }

    [Fact]
    public void PerTestOverride()
    {
        #region readme-fake-per-test-override
        var stub = new ReadmeUserRepositoryStub(new List<User>());
        IUserRepository repo = stub;

        // Override GetById for this test only — Return takes priority over stub override
        var specialUser = new User { Id = 99, Name = "Override" };
        stub.GetById.Return(specialUser);

        var result = repo.GetById(99);
        Assert.Same(specialUser, result);
        #endregion
    }
}
