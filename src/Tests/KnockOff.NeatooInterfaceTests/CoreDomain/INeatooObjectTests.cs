using Neatoo;

namespace KnockOff.NeatooInterfaceTests.CoreDomain;

/// <summary>
/// Tests for INeatooObject - a marker interface with no members.
/// This tests the edge case where an interface has no members to stub.
/// </summary>
[KnockOff<INeatooObject>]
public partial class INeatooObjectTests
{
    [Fact]
    public void InlineStub_CanBeInstantiated()
    {
        var stub = new Stubs.INeatooObject();
        Assert.NotNull(stub);
    }

    [Fact]
    public void InlineStub_ImplementsInterface()
    {
        var stub = new Stubs.INeatooObject();
        INeatooObject marker = stub;
        Assert.NotNull(marker);
    }

    [Fact]
    public void InlineStub_CanBeUsedInGenericConstraint()
    {
        var stub = new Stubs.INeatooObject();
        var result = AcceptsNeatooObject(stub);
        Assert.True(result);
    }

    private static bool AcceptsNeatooObject<T>(T obj) where T : INeatooObject
    {
        return obj != null;
    }
}

/// <summary>
/// Standalone stub for INeatooObject.
/// </summary>
[KnockOff]
public partial class NeatooObjectStub : INeatooObject
{
}

public class INeatooObjectStandaloneTests
{
    [Fact]
    public void StandaloneStub_CanBeInstantiated()
    {
        var stub = new NeatooObjectStub();
        Assert.NotNull(stub);
    }

    [Fact]
    public void StandaloneStub_ImplementsInterface()
    {
        var stub = new NeatooObjectStub();
        INeatooObject marker = stub;
        Assert.NotNull(marker);
    }
}
