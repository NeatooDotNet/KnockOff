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

        stub.IsModified.Value = true;

        Assert.True(manager.IsModified);
        Assert.Equal(1, stub.IsModified.GetCount);
    }

    [Fact]
    public void IsSelfModified_CanBeConfigured()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        stub.IsSelfModified.Value = true;

        Assert.True(manager.IsSelfModified);
    }

    [Fact]
    public void ModifiedProperties_CanBeConfigured()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        var modified = new[] { "FirstName", "LastName", "Email" };
        stub.ModifiedProperties.Value = modified;

        Assert.Equal(modified, manager.ModifiedProperties);
    }

    [Fact]
    public void IsBusy_CanBeConfigured()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        stub.IsBusy.Value = true;

        Assert.True(manager.IsBusy);
    }

    [Fact]
    public void IsValid_CanBeConfigured()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        stub.IsValid.Value = false;

        Assert.False(manager.IsValid);
    }

    [Fact]
    public void IsPaused_CanBeConfigured()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        stub.IsPaused.Value = true;

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

        Assert.True(stub.MarkSelfUnmodified.WasCalled);
        Assert.Equal(1, stub.MarkSelfUnmodified.CallCount);
    }

    [Fact]
    public void MarkSelfUnmodified_CanBeCalledMultipleTimes()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        manager.MarkSelfUnmodified();
        manager.MarkSelfUnmodified();
        manager.MarkSelfUnmodified();

        Assert.Equal(3, stub.MarkSelfUnmodified.CallCount);
    }

    [Fact]
    public async Task WaitForTasks_TracksCall()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        await manager.WaitForTasks();

        Assert.True(stub.WaitForTasks.WasCalled);
        Assert.Equal(1, stub.WaitForTasks.CallCount);
    }

    [Fact]
    public void HasProperty_TracksCall()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        manager.HasProperty("TestProperty");

        Assert.True(stub.HasProperty.WasCalled);
    }

    [Fact]
    public void HasProperty_ReturnsConfiguredValue()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        stub.HasProperty.OnCall = (ko, name) => name == "ExistingProperty";

        Assert.True(manager.HasProperty("ExistingProperty"));
        Assert.False(manager.HasProperty("NonExistent"));
    }

    [Fact]
    public void GetProperty_TracksCall()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        var propertyStub = new EntityPropertyStubForManager();
        stub.GetProperty.OnCall = (ko, name) => propertyStub;

        manager.GetProperty("Name");

        Assert.True(stub.GetProperty.WasCalled);
        Assert.Equal("Name", stub.GetProperty.LastCallArg);
    }

    [Fact]
    public void Indexer_TracksAccess()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        var propertyStub = new EntityPropertyStubForManager();
        stub.Indexer.OnGet = (ko, name) => propertyStub;

        _ = manager["PropertyName"];

        Assert.Equal(1, stub.Indexer.GetCount);
        Assert.Equal("PropertyName", stub.Indexer.LastGetKey);
    }

    [Fact]
    public void SetProperties_TracksCall()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        var properties = new List<IEntityProperty>();
        manager.SetProperties(properties);

        Assert.True(stub.SetProperties.WasCalled);
    }

    [Fact]
    public void PauseAllActions_TracksCall()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        manager.PauseAllActions();

        Assert.True(stub.PauseAllActions.WasCalled);
    }

    [Fact]
    public void ResumeAllActions_TracksCall()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        manager.ResumeAllActions();

        Assert.True(stub.ResumeAllActions.WasCalled);
    }

    [Fact]
    public void ClearAllMessages_TracksCall()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        manager.ClearAllMessages();

        Assert.True(stub.ClearAllMessages.WasCalled);
    }

    #endregion

    #region Event Tests

    [Fact]
    public void PropertyChanged_EventCanBeSubscribed()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        manager.PropertyChanged += (s, e) => { };

        Assert.Equal(1, stub.PropertyChangedInterceptor.AddCount);
    }

    [Fact]
    public void NeatooPropertyChanged_EventCanBeSubscribed()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        manager.NeatooPropertyChanged += (args) => Task.CompletedTask;

        Assert.Equal(1, stub.NeatooPropertyChangedInterceptor.AddCount);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsPropertyTracking()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        stub.IsModified.Value = true;
        _ = manager.IsModified;
        _ = manager.IsModified;

        stub.IsModified.Reset();

        Assert.Equal(0, stub.IsModified.GetCount);
    }

    [Fact]
    public void Reset_ClearsMethodTracking()
    {
        var stub = new Stubs.IEntityPropertyManager();
        IEntityPropertyManager manager = stub;

        manager.MarkSelfUnmodified();
        manager.MarkSelfUnmodified();

        stub.MarkSelfUnmodified.Reset();

        Assert.False(stub.MarkSelfUnmodified.WasCalled);
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

        stub.IsModified.Value = true;

        Assert.True(manager.IsModified);
    }

    [Fact]
    public void MarkSelfUnmodified_TracksCall()
    {
        var stub = new EntityPropertyManagerStub();
        IEntityPropertyManager manager = stub;

        manager.MarkSelfUnmodified();

        Assert.True(stub.MarkSelfUnmodified.WasCalled);
    }
}
