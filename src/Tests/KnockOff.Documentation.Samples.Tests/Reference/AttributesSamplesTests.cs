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
    public void BasicUsage_StubGenerated()
    {
        var stub = new AttrMyServiceKnockOff();
        Assert.NotNull(stub);
        Assert.NotNull(stub.DoWork);
    }

    [Fact]
    public void GenericInterface_StubGenerated()
    {
        var stub = new AttrUserRepoKnockOff();
        Assert.NotNull(stub);
        Assert.NotNull(stub.GetById);
    }

    [Fact]
    public void InterfaceInheritance_StubGenerated()
    {
        var stub = new AttrAuditableKnockOff();
        Assert.NotNull(stub);
        Assert.NotNull(stub.CreatedAt);
        Assert.NotNull(stub.CreatedBy);
    }

    [Fact]
    public void InternalClass_StubGenerated()
    {
        var stub = new AttrInternalServiceKnockOff();
        Assert.NotNull(stub);
    }

    [Fact]
    public void NestedClass_StubGenerated()
    {
        var stub = new AttrTestFixture.NestedKnockOff();
        Assert.NotNull(stub);
    }

    [Fact]
    public void QualifiedNamespace_StubGenerated()
    {
        var stub = new AttrQualifiedServiceKnockOff();
        Assert.NotNull(stub);
    }
}
