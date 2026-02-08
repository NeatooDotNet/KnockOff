namespace KnockOff.Documentation.Samples.SkillReadme;

// =============================================================================
// Skill README Samples - Match skills/knockoff/README.md exactly
// =============================================================================

// -----------------------------------------------------------------------------
// User Methods (Stand-Alone Only) Section
// The README shows stub declaration AND usage patterns together.
// We need an interface, stub class, and usage examples.
// -----------------------------------------------------------------------------

public interface IRepo
{
    User? GetById(int id);
}

#region skill-readme-user-methods
[KnockOff]
public partial class RepoStub : IRepo { }

public partial class RepoStub
{
    // Override virtual method with underscore suffix - compiler enforces signature!
    protected override User? GetById_(int id) => new User { Id = id, Name = "Default" };
}
#endregion

public class SkillReadmeUserMethodsTests
{
    [Fact]
    public void UserMethods_ReturnSupersedes()
    {
        #region skill-readme-user-methods-usage
        var stub = new RepoStub();

        // User override is fallback; Return supersedes it
        stub.GetById.Return(id => new User { Id = id, Name = "Override" });

        // Returns for constant values (auto-wraps for async)
        stub.GetById.Return(new User { Id = 99 });
        #endregion

        IRepo repo = stub;
        var user = repo.GetById(42);

        Assert.NotNull(user);
        Assert.Equal(99, user.Id); // Return() was set last
    }

    [Fact]
    public void UserMethods_FallbackToUserOverride()
    {
        var stub = new RepoStub();
        // No Return configured - falls back to user override

        IRepo repo = stub;
        var user = repo.GetById(42);

        Assert.NotNull(user);
        Assert.Equal(42, user.Id);
        Assert.Equal("Default", user.Name);
    }
}

// -----------------------------------------------------------------------------
// Method Configuration Section
// The README shows multiple configuration patterns in one block.
// We create an interface that has all the methods needed.
// -----------------------------------------------------------------------------

public interface IMethodConfigService
{
    User? GetUser(int id);
    int GetNext();
    int Add(int a, int b);
}

[KnockOff]
public partial class MethodConfigStub : IMethodConfigService { }

public class SkillReadmeMethodConfigTests
{
    [Fact]
    public void MethodConfig_AllPatterns()
    {
        var stub = new MethodConfigStub();
        var adminUser = new User { Id = 42, Name = "Admin" };

        #region skill-readme-method-config
        // Fixed value
        stub.GetUser.Return(new User { Id = 1, Name = "Alice" });

        // Dynamic callback
        stub.GetUser.Return((id) => new User { Id = id, Name = $"User{id}" });

        // Argument matching
        stub.GetUser.When(42).Return(adminUser);
        stub.GetUser.When(id => id < 0).Return(null);

        // Value sequences (NSubstitute-style) - repeats last after exhaustion
        stub.GetNext.Return(1, 2, 3);
        // Returns: 1, 2, 3, 3, 3... (repeats last value)

        // Mix callbacks with value sequences
        stub.Add.Return((a, b) => a + b).ThenReturn(100, 200);
        // First call: computed. Then: 100, 200, 200, 200...

        // Use ThenDefault() to return default(T) instead of repeating
        stub.GetNext.Return(1, 2).ThenDefault();
        // Returns: 1, 2, 0, 0... (default after exhaustion)
        #endregion

        IMethodConfigService svc = stub;

        // Verify the final configuration works (ThenDefault was set last on GetNext)
        Assert.Equal(1, svc.GetNext());
        Assert.Equal(2, svc.GetNext());
        Assert.Equal(0, svc.GetNext()); // default after ThenDefault()
    }

    [Fact]
    public void MethodConfig_FixedValue()
    {
        var stub = new MethodConfigStub();

        stub.GetUser.Return(new User { Id = 1, Name = "Alice" });

        IMethodConfigService svc = stub;
        var user = svc.GetUser(999);

        Assert.NotNull(user);
        Assert.Equal(1, user.Id);
        Assert.Equal("Alice", user.Name);
    }

    [Fact]
    public void MethodConfig_DynamicCallback()
    {
        var stub = new MethodConfigStub();

        stub.GetUser.Return((id) => new User { Id = id, Name = $"User{id}" });

        IMethodConfigService svc = stub;
        var user = svc.GetUser(42);

        Assert.NotNull(user);
        Assert.Equal(42, user.Id);
        Assert.Equal("User42", user.Name);
    }

    [Fact]
    public void MethodConfig_ArgumentMatchingValue()
    {
        var stub = new MethodConfigStub();
        var adminUser = new User { Id = 42, Name = "Admin" };

        stub.GetUser.When(42).Return(adminUser);
        stub.GetUser.When(id => id < 0).Return(null);

        IMethodConfigService svc = stub;

        var admin = svc.GetUser(42);
        Assert.NotNull(admin);
        Assert.Equal("Admin", admin.Name);

        var invalid = svc.GetUser(-1);
        Assert.Null(invalid);
    }

    [Fact]
    public void MethodConfig_ValueSequences()
    {
        var stub = new MethodConfigStub();

        stub.GetNext.Return(1, 2, 3);

        IMethodConfigService svc = stub;

        Assert.Equal(1, svc.GetNext());
        Assert.Equal(2, svc.GetNext());
        Assert.Equal(3, svc.GetNext());
        Assert.Equal(3, svc.GetNext()); // Repeats last
    }

    [Fact]
    public void MethodConfig_MixCallbacksWithSequences()
    {
        var stub = new MethodConfigStub();

        stub.Add.Return((a, b) => a + b).ThenReturn(100, 200);

        IMethodConfigService svc = stub;

        Assert.Equal(3, svc.Add(1, 2)); // Computed: 1 + 2
        Assert.Equal(100, svc.Add(1, 2)); // First ThenReturn value
        Assert.Equal(200, svc.Add(1, 2)); // Second ThenReturn value
        Assert.Equal(200, svc.Add(1, 2)); // Repeats last
    }

    [Fact]
    public void MethodConfig_ThenDefault()
    {
        var stub = new MethodConfigStub();

        stub.GetNext.Return(1, 2).ThenDefault();

        IMethodConfigService svc = stub;

        Assert.Equal(1, svc.GetNext());
        Assert.Equal(2, svc.GetNext());
        Assert.Equal(0, svc.GetNext()); // default(int)
    }
}

// -----------------------------------------------------------------------------
// Verification Section
// The README shows batch and individual verification patterns.
// -----------------------------------------------------------------------------

public interface ISaver
{
    void Save(User user);
}

[KnockOff]
public partial class SaverStub : ISaver { }

public class SkillReadmeVerificationTests
{
    [Fact]
    public void Verification_CombinedPatterns()
    {
        var stub = new SaverStub();
        ISaver svc = stub;

        #region skill-readme-verification
        // Mark for batch verification
        stub.Save.Call((user) => { }).Verifiable();
        svc.Save(new User { Id = 1 }); // Call the method
        stub.Verify();  // Checks all Verifiable() members

        // Or verify individually
        stub.Save.Reset(); // Reset for second pattern demo
        var tracking = stub.Save.Call((user) => { });
        svc.Save(new User { Id = 2 }); // Call the method
        tracking.Verify(Called.Once);
        #endregion
    }

    [Fact]
    public void Verification_BatchWithVerifiable()
    {
        var stub = new SaverStub();

        stub.Save.Call((user) => { }).Verifiable();

        ISaver svc = stub;
        svc.Save(new User { Id = 1 });

        stub.Verify();
    }

    [Fact]
    public void Verification_IndividualWithTracking()
    {
        var stub = new SaverStub();

        var tracking = stub.Save.Call((user) => { });

        ISaver svc = stub;
        svc.Save(new User { Id = 1 });

        tracking.Verify(Called.Once);
    }
}
