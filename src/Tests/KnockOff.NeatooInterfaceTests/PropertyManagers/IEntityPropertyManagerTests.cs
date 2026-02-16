using Neatoo;
using System.ComponentModel;

namespace KnockOff.NeatooInterfaceTests.PropertyManagers;

/// <summary>
/// Tests for IEntityPropertyManager - entity property manager interface.
/// This interface has entity-specific methods for modification tracking.
/// </summary>
[KnockOff<IEntityPropertyManager>]
public partial class IEntityPropertyManagerTests
{
    [Fact]
    public void InlineStub_CanBeInstantiated()
    {
        var stub = new Stubs.IEntityPropertyManager();
        Assert.NotNull(stub);
    }

    [Fact]
    public void InlineStub_ImplementsInterface()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;
        Assert.NotNull(manager);
    }

    [Fact]
    public void InlineStub_ImplementsINotifyPropertyChanged()
    {
        var stub = new Stubs.IEntityPropertyManager();
        INotifyPropertyChanged notify = stub;
        Assert.NotNull(notify);
    }

    [Fact]
    public void InlineStub_ImplementsINotifyNeatooPropertyChanged()
    {
        var stub = new Stubs.IEntityPropertyManager();
        INotifyNeatooPropertyChanged neatooNotify = stub;
        Assert.NotNull(neatooNotify);
    }

    #region Property Tests

    [Fact]
    public void IsModified_CanBeConfigured()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        stub.IsModified.Get(true);

        Assert.True(manager.IsModified);
        stub.IsModified.VerifyGet(Called.Once);
    }

    [Fact]
    public void IsSelfModified_CanBeConfigured()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        stub.IsSelfModified.Get(true);

        Assert.True(manager.IsSelfModified);
    }

    [Fact]
    public void ModifiedProperties_CanBeConfigured()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        var modified = new[] { "FirstName", "LastName", "Email" };
        stub.ModifiedProperties.Get(modified);

        Assert.Equal(modified, manager.ModifiedProperties);
    }

    [Fact]
    public void IsBusy_CanBeConfigured()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        stub.IsBusy.Get(true);

        Assert.True(manager.IsBusy);
    }

    [Fact]
    public void IsValid_CanBeConfigured()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        stub.IsValid.Get(false);

        Assert.False(manager.IsValid);
    }

    [Fact]
    public void IsPaused_CanBeConfigured()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        stub.IsPaused.Get(true);

        Assert.True(manager.IsPaused);
    }

    #endregion

    #region Method Tests

    [Fact]
    public void MarkSelfUnmodified_TracksCall()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        manager.MarkSelfUnmodified();

        stub.MarkSelfUnmodified.Verify(Called.Once);
    }

    [Fact]
    public void MarkSelfUnmodified_CanBeCalledMultipleTimes()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        manager.MarkSelfUnmodified();
        manager.MarkSelfUnmodified();
        manager.MarkSelfUnmodified();

        stub.MarkSelfUnmodified.Verify(Called.Exactly(3));
    }

    [Fact]
    public async Task WaitForTasks_TracksCall()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        await manager.WaitForTasks();

        stub.WaitForTasks.Verify(Called.Once);
    }

    [Fact]
    public void HasProperty_TracksCall()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        manager.HasProperty("TestProperty");

        stub.HasProperty.Verify();
    }

    [Fact]
    public void HasProperty_ReturnsConfiguredValue()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        stub.HasProperty.Return((name) => name == "ExistingProperty");

        Assert.True(manager.HasProperty("ExistingProperty"));
        Assert.False(manager.HasProperty("NonExistent"));
    }

    [Fact]
    public void GetProperty_TracksCall()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        var propertyStub = new EntityPropertyStubForManager();
        stub.GetProperty.Return((name) => propertyStub);

        manager.GetProperty("Name");

        stub.GetProperty.Verify();
        Assert.Equal("Name", stub.GetProperty.LastArgs);
    }

    [Fact]
    public void Indexer_TracksAccess()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        var propertyStub = new EntityPropertyStubForManager();
        stub.Indexer.Get((name) => propertyStub);

        _ = manager["PropertyName"];

        stub.Indexer.VerifyGet(Called.Once);
        Assert.Equal("PropertyName", stub.Indexer.LastGetKey);
    }

    [Fact]
    public void SetProperties_TracksCall()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        var properties = new List<IEntityProperty>();
        manager.SetProperties(properties);

        stub.SetProperties.Verify();
    }

    [Fact]
    public void PauseAllActions_TracksCall()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        manager.PauseAllActions();

        stub.PauseAllActions.Verify();
    }

    [Fact]
    public void ResumeAllActions_TracksCall()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        manager.ResumeAllActions();

        stub.ResumeAllActions.Verify();
    }

    [Fact]
    public void ClearAllMessages_TracksCall()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        manager.ClearAllMessages();

        stub.ClearAllMessages.Verify();
    }

    #endregion

    #region Event Tests

    [Fact]
    public void PropertyChanged_EventCanBeSubscribed()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        manager.PropertyChanged += (s, e) => { };

        stub.PropertyChanged.VerifyAdd(Called.Once);
    }

    [Fact]
    public void NeatooPropertyChanged_EventCanBeSubscribed()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        manager.NeatooPropertyChanged += (args) => Task.CompletedTask;

        stub.NeatooPropertyChanged.VerifyAdd(Called.Once);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsPropertyTracking()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        stub.IsModified.Get(true);
        _ = manager.IsModified;
        _ = manager.IsModified;

        stub.IsModified.Reset();

        stub.IsModified.VerifyGet(Called.Never);
    }

    [Fact]
    public void Reset_ClearsMethodTracking()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        manager.MarkSelfUnmodified();
        manager.MarkSelfUnmodified();

        stub.MarkSelfUnmodified.Reset();

        stub.MarkSelfUnmodified.Verify(Called.Never);
    }

    #endregion
}

/// <summary>
/// Standalone stub for IEntityPropertyManager.
/// </summary>
[KnockOff]
public partial class EntityPropertyManagerStub : IEntityPropertyManager
{
}

/// <summary>
/// Standalone stub for IEntityProperty used in property manager tests.
/// </summary>
[KnockOff]
public partial class EntityPropertyStubForManager : IEntityProperty
{
}

public class IEntityPropertyManagerStandaloneTests
{
    [Fact]
    public void StandaloneStub_CanBeInstantiated()
    {
        var stub = new EntityPropertyManagerStub();
        Assert.NotNull(stub);
    }

    [Fact]
    public void StandaloneStub_ImplementsInterface()
    {
        var stub = new EntityPropertyManagerStub();
        IEntityPropertyManager manager = stub;
        Assert.NotNull(manager);
    }

    [Fact]
    public void IsModified_CanBeConfigured()
    {
        var stub = new EntityPropertyManagerStub();
        IEntityPropertyManager manager = stub;

        stub.IsModified.Get(true);

        Assert.True(manager.IsModified);
    }

    [Fact]
    public void MarkSelfUnmodified_TracksCall()
    {
        var stub = new EntityPropertyManagerStub();
        IEntityPropertyManager manager = stub;

        // Configure callback to enable tracking
        var tracking = stub.MarkSelfUnmodified.Call(() => { });

        manager.MarkSelfUnmodified();

        // Tracking is available via the returned tracking object
        tracking.Verify();
    }
}
