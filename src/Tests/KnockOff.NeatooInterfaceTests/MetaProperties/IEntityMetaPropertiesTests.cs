using Neatoo;
using Neatoo.RemoteFactory;

namespace KnockOff.NeatooInterfaceTests.MetaProperties;

/// <summary>
/// Tests for IEntityMetaProperties - entity state tracking meta-properties.
/// This interface extends IFactorySaveMeta (from RemoteFactory).
/// </summary>
[KnockOff<IEntityMetaProperties>]
public partial class IEntityMetaPropertiesTests
{
    [Fact]
    public void InlineStub_CanBeInstantiated()
    {
        var stub = new Stubs.IEntityMetaProperties();
        Assert.NotNull(stub);
    }

    [Fact]
    public void InlineStub_ImplementsInterface()
    {
        var stub = new Stubs.IEntityMetaProperties();
        IEntityMetaProperties meta = stub;
        Assert.NotNull(meta);
    }

    [Fact]
    public void InlineStub_ImplementsIFactorySaveMeta()
    {
        var stub = new Stubs.IEntityMetaProperties();
        IFactorySaveMeta saveMeta = stub;
        Assert.NotNull(saveMeta);
    }

    #region IEntityMetaProperties Properties

    [Fact]
    public void IsChild_CanBeConfigured()
    {
        var stub = new Stubs.IEntityMetaProperties();
        IEntityMetaProperties meta = stub;

        stub.IsChild.Value = true;

        Assert.True(meta.IsChild);
        Assert.Equal(1, stub.IsChild.GetCount);
    }

    [Fact]
    public void IsModified_CanBeConfigured()
    {
        var stub = new Stubs.IEntityMetaProperties();
        IEntityMetaProperties meta = stub;

        stub.IsModified.Value = true;

        Assert.True(meta.IsModified);
    }

    [Fact]
    public void IsSelfModified_CanBeConfigured()
    {
        var stub = new Stubs.IEntityMetaProperties();
        IEntityMetaProperties meta = stub;

        stub.IsSelfModified.Value = true;

        Assert.True(meta.IsSelfModified);
    }

    [Fact]
    public void IsMarkedModified_CanBeConfigured()
    {
        var stub = new Stubs.IEntityMetaProperties();
        IEntityMetaProperties meta = stub;

        stub.IsMarkedModified.Value = true;

        Assert.True(meta.IsMarkedModified);
    }

    [Fact]
    public void IsSavable_CanBeConfigured()
    {
        var stub = new Stubs.IEntityMetaProperties();
        IEntityMetaProperties meta = stub;

        stub.IsSavable.Value = true;

        Assert.True(meta.IsSavable);
    }

    #endregion

    #region IFactorySaveMeta Properties (inherited)

    [Fact]
    public void IsDeleted_CanBeConfigured()
    {
        var stub = new Stubs.IEntityMetaProperties();
        IEntityMetaProperties meta = stub;

        stub.IsDeleted.Value = true;

        Assert.True(meta.IsDeleted);
    }

    [Fact]
    public void IsNew_CanBeConfigured()
    {
        var stub = new Stubs.IEntityMetaProperties();
        IEntityMetaProperties meta = stub;

        stub.IsNew.Value = true;

        Assert.True(meta.IsNew);
    }

    [Fact]
    public void IFactorySaveMeta_IsDeleted_SameAsIEntityMetaProperties()
    {
        var stub = new Stubs.IEntityMetaProperties();
        IEntityMetaProperties entityMeta = stub;
        IFactorySaveMeta saveMeta = stub;

        stub.IsDeleted.Value = true;

        // Both interfaces should return the same value
        Assert.True(entityMeta.IsDeleted);
        Assert.True(saveMeta.IsDeleted);
    }

    [Fact]
    public void IFactorySaveMeta_IsNew_SameAsIEntityMetaProperties()
    {
        var stub = new Stubs.IEntityMetaProperties();
        IEntityMetaProperties entityMeta = stub;
        IFactorySaveMeta saveMeta = stub;

        stub.IsNew.Value = true;

        Assert.True(entityMeta.IsNew);
        Assert.True(saveMeta.IsNew);
    }

    #endregion

    #region OnGet Callback Tests

    [Fact]
    public void IsChild_OnGet_ExecutesCallback()
    {
        var stub = new Stubs.IEntityMetaProperties();
        IEntityMetaProperties meta = stub;
        var callbackExecuted = false;

        stub.IsChild.OnGet = (ko) =>
        {
            callbackExecuted = true;
            return true;
        };

        var result = meta.IsChild;

        Assert.True(callbackExecuted);
        Assert.True(result);
    }

    [Fact]
    public void IsSavable_OnGet_DynamicComputation()
    {
        var stub = new Stubs.IEntityMetaProperties();
        IEntityMetaProperties meta = stub;

        // Set up stub to compute IsSavable based on other properties
        stub.IsModified.Value = true;
        stub.IsChild.Value = false;

        stub.IsSavable.OnGet = (ko) =>
        {
            // Use stub values for computation
            return stub.IsModified.Value && !stub.IsChild.Value;
        };

        Assert.True(meta.IsSavable);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsPropertyTracking()
    {
        var stub = new Stubs.IEntityMetaProperties();
        IEntityMetaProperties meta = stub;

        stub.IsChild.Value = true;
        _ = meta.IsChild;
        _ = meta.IsChild;
        _ = meta.IsChild;

        stub.IsChild.Reset();

        Assert.Equal(0, stub.IsChild.GetCount);
    }

    #endregion
}

/// <summary>
/// Standalone stub for IEntityMetaProperties.
/// </summary>
[KnockOff]
public partial class EntityMetaPropertiesStub : IEntityMetaProperties
{
}

public class IEntityMetaPropertiesStandaloneTests
{
    [Fact]
    public void StandaloneStub_CanBeInstantiated()
    {
        var stub = new EntityMetaPropertiesStub();
        Assert.NotNull(stub);
    }

    [Fact]
    public void StandaloneStub_ImplementsInterface()
    {
        var stub = new EntityMetaPropertiesStub();
        IEntityMetaProperties meta = stub;
        Assert.NotNull(meta);
    }

    [Fact]
    public void StandaloneStub_ImplementsIFactorySaveMeta()
    {
        var stub = new EntityMetaPropertiesStub();
        IFactorySaveMeta saveMeta = stub;
        Assert.NotNull(saveMeta);
    }

    [Fact]
    public void IsChild_CanBeConfigured()
    {
        var stub = new EntityMetaPropertiesStub();
        IEntityMetaProperties meta = stub;

        stub.IsChild.OnGet = (ko) => true;

        Assert.True(meta.IsChild);
    }

    [Fact]
    public void IsDeleted_CanBeConfigured()
    {
        var stub = new EntityMetaPropertiesStub();
        IEntityMetaProperties meta = stub;

        stub.IsDeleted.OnGet = (ko) => true;

        Assert.True(meta.IsDeleted);
    }

    [Fact]
    public void IsNew_CanBeConfigured()
    {
        var stub = new EntityMetaPropertiesStub();
        IEntityMetaProperties meta = stub;

        stub.IsNew.OnGet = (ko) => true;

        Assert.True(meta.IsNew);
    }
}
