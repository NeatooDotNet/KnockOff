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

        stub.IsChild.OnGet(true);

        Assert.True(meta.IsChild);
        stub.IsChild.VerifyGet(Times.Once);
    }

    [Fact]
    public void IsModified_CanBeConfigured()
    {
        var stub = new Stubs.IEntityMetaProperties();
        IEntityMetaProperties meta = stub;

        stub.IsModified.OnGet(true);

        Assert.True(meta.IsModified);
    }

    [Fact]
    public void IsSelfModified_CanBeConfigured()
    {
        var stub = new Stubs.IEntityMetaProperties();
        IEntityMetaProperties meta = stub;

        stub.IsSelfModified.OnGet(true);

        Assert.True(meta.IsSelfModified);
    }

    [Fact]
    public void IsMarkedModified_CanBeConfigured()
    {
        var stub = new Stubs.IEntityMetaProperties();
        IEntityMetaProperties meta = stub;

        stub.IsMarkedModified.OnGet(true);

        Assert.True(meta.IsMarkedModified);
    }

    [Fact]
    public void IsSavable_CanBeConfigured()
    {
        var stub = new Stubs.IEntityMetaProperties();
        IEntityMetaProperties meta = stub;

        stub.IsSavable.OnGet(true);

        Assert.True(meta.IsSavable);
    }

    #endregion

    #region IFactorySaveMeta Properties (inherited)

    [Fact]
    public void IsDeleted_CanBeConfigured()
    {
        var stub = new Stubs.IEntityMetaProperties();
        IEntityMetaProperties meta = stub;

        stub.IsDeleted.OnGet(true);

        Assert.True(meta.IsDeleted);
    }

    [Fact]
    public void IsNew_CanBeConfigured()
    {
        var stub = new Stubs.IEntityMetaProperties();
        IEntityMetaProperties meta = stub;

        stub.IsNew.OnGet(true);

        Assert.True(meta.IsNew);
    }

    [Fact]
    public void IFactorySaveMeta_IsDeleted_SameAsIEntityMetaProperties()
    {
        var stub = new Stubs.IEntityMetaProperties();
        IEntityMetaProperties entityMeta = stub;
        IFactorySaveMeta saveMeta = stub;

        stub.IsDeleted.OnGet(true);

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

        stub.IsNew.OnGet(true);

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

        stub.IsChild.OnGet(() =>
        {
            callbackExecuted = true;
            return true;
        });

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
        stub.IsModified.OnGet(true);
        stub.IsChild.OnGet(false);

        stub.IsSavable.OnGet(() =>
        {
            // Access properties through interface for computation
            return meta.IsModified && !meta.IsChild;
        });

        Assert.True(meta.IsSavable);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsPropertyTracking()
    {
        var stub = new Stubs.IEntityMetaProperties();
        IEntityMetaProperties meta = stub;

        stub.IsChild.OnGet(true);
        _ = meta.IsChild;
        _ = meta.IsChild;
        _ = meta.IsChild;

        stub.IsChild.Reset();

        stub.IsChild.VerifyGet(Times.Never);
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

        stub.IsChild.OnGet(true);

        Assert.True(meta.IsChild);
    }

    [Fact]
    public void IsDeleted_CanBeConfigured()
    {
        var stub = new EntityMetaPropertiesStub();
        IEntityMetaProperties meta = stub;

        stub.IsDeleted.OnGet(true);

        Assert.True(meta.IsDeleted);
    }

    [Fact]
    public void IsNew_CanBeConfigured()
    {
        var stub = new EntityMetaPropertiesStub();
        IEntityMetaProperties meta = stub;

        stub.IsNew.OnGet(true);

        Assert.True(meta.IsNew);
    }
}
