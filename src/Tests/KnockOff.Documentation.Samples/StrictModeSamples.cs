using KnockOff;

namespace KnockOff.Documentation.Samples.StrictMode;

// =============================================================================
// Interfaces for Strict Mode Samples
// =============================================================================

public interface IStrictUserRepo
{
    User? GetUser(int id);
    void Save(User user);
}

public interface IStrictUserService
{
    User? GetUser(int id);
}

public interface IStrictLegacyService
{
    int Process(string input);
}

// =============================================================================
// Per-Stub Strict Mode Samples
// =============================================================================

// Via attribute property (default for all instances)
#region strict-attribute-property
[KnockOff(Strict = true)]
public partial class StrictUserRepoStub : IStrictUserRepo { }
#endregion

// For generic attribute pattern
#region strict-inline-attribute
[KnockOff<IStrictUserRepo>(Strict = true)]
public partial class StrictMyTests { }
#endregion

public partial class StrictMyTests
{
    [Fact]
    public void InlineStrict_ConstructorOverride()
    {
        #region strict-constructor
        // Via constructor (inline stubs only)
        var stub = new Stubs.IStrictUserRepo(strict: true);
        #endregion

        Assert.True(stub.Strict);
    }

    [Fact]
    public void InlineStrict_ThrowsOnUnconfigured()
    {
        var stub = new Stubs.IStrictUserRepo(strict: true);
        IStrictUserRepo repo = stub;

        Assert.Throws<StubException>(() => repo.GetUser(42));
    }
}

public class StrictModeStandaloneTests
{
    [Fact]
    public void StrictMode_FluentExtension()
    {
        #region strict-fluent-extension
        // Via fluent extension method
        var stub = new StrictUserRepoStub().Strict();
        #endregion

        Assert.True(stub.Strict);
    }

    [Fact]
    public void StrictMode_PropertyAtRuntime()
    {
        var stub = new StrictUserRepoStub();

        #region strict-property-runtime
        // Via property at runtime
        stub.Strict = true;
        #endregion

        Assert.True(stub.Strict);
    }

    [Fact]
    public void StrictMode_FromAttribute_ThrowsOnUnconfigured()
    {
        var stub = new StrictUserRepoStub();
        IStrictUserRepo repo = stub;

        // StrictUserRepoStub has [KnockOff(Strict = true)] so defaults to strict
        Assert.Throws<StubException>(() => repo.GetUser(42));
    }

    [Fact]
    public void StrictMode_ConfiguredMethod_Works()
    {
        var stub = new StrictUserRepoStub();
        stub.GetUser.Returns(new User { Id = 1, Name = "Test" });
        IStrictUserRepo repo = stub;

        var user = repo.GetUser(1);

        Assert.NotNull(user);
        Assert.Equal("Test", user.Name);
    }
}

// =============================================================================
// Assembly-Wide Strict Mode Samples
// =============================================================================

// Note: We cannot add [assembly: KnockOffStrict] here as it would affect all stubs
// in this project. The sample shows the pattern, and tests verify the behavior
// using stubs that have Strict = true on the attribute (simulating assembly default).

#region strict-assembly-declaration
// In AssemblyInfo.cs or any file in your test project
// [assembly: KnockOffStrict]
#endregion

#region strict-assembly-usage
[KnockOff<IStrictUserService>]
public partial class StrictUserTests { }
#endregion

public partial class StrictUserTests
{
    [Fact]
    public void AssemblyStrict_Example()
    {
        // When [assembly: KnockOffStrict] is present, all stubs default to strict.
        // Simulated here by using explicit Strict = true constructor:
        var stub = new Stubs.IStrictUserService(strict: true);
        IStrictUserService service = stub;

        #region strict-assembly-throws
        // Unconfigured calls throw StubException
        // service.GetUser(42);  // Throws StubException!
        #endregion

        Assert.Throws<StubException>(() => service.GetUser(42));
    }
}

// =============================================================================
// Opting Out of Assembly-Wide Strict Mode
// =============================================================================

#region strict-opt-out-attribute
// Opt out via attribute property
[KnockOff<IStrictLegacyService>(Strict = false)]
public partial class StrictLegacyTests { }
#endregion

public partial class StrictLegacyTests
{
    [Fact]
    public void OptOut_ViaAttribute_ReturnsDefault()
    {
        // Even if [assembly: KnockOffStrict] is present,
        // Strict = false on the attribute opts out
        var stub = new Stubs.IStrictLegacyService();
        IStrictLegacyService service = stub;

        // No exception - returns default
        var result = service.Process("test");

        Assert.Equal(0, result);
    }

    [Fact]
    public void OptOut_ViaConstructor()
    {
        #region strict-opt-out-constructor
        // Opt out via constructor (inline stubs only)
        var stub = new Stubs.IStrictLegacyService(strict: false);
        #endregion

        IStrictLegacyService service = stub;

        // No exception
        var result = service.Process("test");

        Assert.Equal(0, result);
    }

    [Fact]
    public void OptOut_ViaPropertyAtRuntime()
    {
        var stub = new Stubs.IStrictLegacyService(strict: true);
        IStrictLegacyService service = stub;

        // Start strict
        Assert.Throws<StubException>(() => service.Process("test"));

        #region strict-opt-out-runtime
        // Opt out at runtime
        stub.Strict = false;
        #endregion

        // Now returns default
        var result = service.Process("test");
        Assert.Equal(0, result);
    }
}
