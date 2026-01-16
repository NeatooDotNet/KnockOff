using KnockOff.Documentation.Samples.Reference;

namespace KnockOff.Documentation.Samples.Tests.Reference;

/// <summary>
/// Tests for docs/reference/attributes.md samples.
/// </summary>
[Trait("Category", "Documentation")]
[Trait("Category", "Reference")]
public class AttributesSamplesTests
{
    [Fact]
    public void StandaloneBasic_StubGenerated()
    {
        var stub = new AttrUserRepositoryStub();
        Assert.NotNull(stub);
        Assert.NotNull(stub.GetById);
    }

    [Fact]
    public void StandaloneService_StubGenerated()
    {
        var stub = new AttrServiceStub();
        Assert.NotNull(stub);
        Assert.NotNull(stub.DoWork);
    }

    [Fact]
    public void GenericInterface_StubGenerated()
    {
        var stub = new AttrUserRepoStub();
        Assert.NotNull(stub);
        Assert.NotNull(stub.GetById);
    }

    [Fact]
    public void InternalClass_StubGenerated()
    {
        var stub = new AttrInternalServiceStub();
        Assert.NotNull(stub);
    }

    [Fact]
    public void NestedClass_StubGenerated()
    {
        var stub = new AttrMyTests.NestedStub();
        Assert.NotNull(stub);
    }

    [Fact]
    public void FullyQualifiedNamespace_StubGenerated()
    {
        var stub = new AttrFullyQualifiedStub();
        Assert.NotNull(stub);
    }

    [Fact]
    public void InlineInterface_StubGenerated()
    {
        var stub = new AttrInterfaceTests.Stubs.IAttrUserRepository();
        Assert.NotNull(stub);
    }

    [Fact]
    public void InlineClass_StubGenerated()
    {
        var stub = new AttrClassTests.Stubs.AttrEmailServiceClass();
        Assert.NotNull(stub);
        Assert.NotNull(stub.Object);
    }

    [Fact]
    public void InlineDelegate_StubGenerated()
    {
        var stub = new AttrDelegateTests.Stubs.Func();
        Assert.NotNull(stub);
    }
}
