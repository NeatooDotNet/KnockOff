using Neatoo;
using System.Threading.Tasks;
using Xunit;

namespace KnockOff.Tests;

/// <summary>
/// Tests for stubbing Neatoo framework interfaces with KnockOff.
/// </summary>
[KnockOff]
public partial class EntityBaseStub : IEntityBase
{
}

public class NeatooTests
{
    [Fact]
    public void IEntityBase_CanBeStubbed()
    {
        var stub = new EntityBaseStub();
        IEntityBase entity = stub;

        Assert.NotNull(entity);
        Assert.NotNull(stub.Spy);
    }

    [Fact]
    public void IEntityBase_IsNew_CanBeConfigured()
    {
        var stub = new EntityBaseStub();
        IEntityBase entity = stub;

        // Configure via OnGet callback
        stub.Spy.IsNew.OnGet = (ko) => true;

        Assert.True(entity.IsNew);
        Assert.Equal(1, stub.Spy.IsNew.GetCount);
    }

    [Fact]
    public void IEntityBase_IsModified_CanBeConfigured()
    {
        var stub = new EntityBaseStub();
        IEntityBase entity = stub;

        stub.Spy.IsModified.OnGet = (ko) => true;

        Assert.True(entity.IsModified);
    }

    [Fact]
    public void IEntityBase_Indexer_CanBeConfigured()
    {
        var stub = new EntityBaseStub();
        IEntityBase entity = stub;

        // Configure indexer to return a mock property
        stub.Spy.StringIndexer.OnGet = (ko, propertyName) =>
        {
            // Return null for now - would need IEntityProperty stub
            return null!;
        };

        // Access triggers tracking
        var prop = entity["FirstName"];

        Assert.Equal(1, stub.Spy.StringIndexer.GetCount);
        Assert.Equal("FirstName", stub.Spy.StringIndexer.LastGetKey);
    }

    [Fact]
    public void IEntityBase_Delete_TracksCall()
    {
        var stub = new EntityBaseStub();
        IEntityBase entity = stub;

        entity.Delete();

        Assert.True(stub.Spy.Delete.WasCalled);
        Assert.Equal(1, stub.Spy.Delete.CallCount);
    }

    [Fact]
    public async Task IEntityBase_Save_CanReturnConfiguredValue()
    {
        var stub = new EntityBaseStub();
        IEntityBase entity = stub;

        // Configure Save to return the stub itself
        stub.Spy.Save.OnCall((ko) => Task.FromResult<IEntityBase>(ko));

        var result = await entity.Save();

        Assert.Same(entity, result);
        Assert.True(stub.Spy.Save.WasCalled);
    }
}
