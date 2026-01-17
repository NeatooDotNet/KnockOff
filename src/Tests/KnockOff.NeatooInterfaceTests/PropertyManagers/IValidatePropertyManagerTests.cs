using Neatoo;
using Neatoo.Rules;
using System.ComponentModel;

namespace KnockOff.NeatooInterfaceTests.PropertyManagers;

/// <summary>
/// Tests for IValidatePropertyManager&lt;P&gt; - property manager interface.
/// This is a generic interface with covariant type parameter (out P).
/// Extends INotifyNeatooPropertyChanged and INotifyPropertyChanged.
/// </summary>
[KnockOff<IValidatePropertyManager<IValidateProperty>>]
public partial class IValidatePropertyManagerTests
{
    [Fact]
    public void InlineStub_CanBeInstantiated()
    {
        var stub = new Stubs.IValidatePropertyManager();
        Assert.NotNull(stub);
    }

    [Fact]
    public void InlineStub_ImplementsInterface()
    {
        var stub = new Stubs.IValidatePropertyManager();
        IValidatePropertyManager<IValidateProperty> manager = stub;
        Assert.NotNull(manager);
    }

    [Fact]
    public void InlineStub_ImplementsINotifyPropertyChanged()
    {
        var stub = new Stubs.IValidatePropertyManager();
        INotifyPropertyChanged notify = stub;
        Assert.NotNull(notify);
    }

    [Fact]
    public void InlineStub_ImplementsINotifyNeatooPropertyChanged()
    {
        var stub = new Stubs.IValidatePropertyManager();
        INotifyNeatooPropertyChanged neatooNotify = stub;
        Assert.NotNull(neatooNotify);
    }

    #region Property Tests

    [Fact]
    public void IsBusy_CanBeConfigured()
    {
        var stub = new Stubs.IValidatePropertyManager();
        IValidatePropertyManager<IValidateProperty> manager = stub;

        stub.IsBusy.Value = true;

        Assert.True(manager.IsBusy);
        Assert.Equal(1, stub.IsBusy.GetCount);
    }

    [Fact]
    public void IsSelfValid_CanBeConfigured()
    {
        var stub = new Stubs.IValidatePropertyManager();
        IValidatePropertyManager<IValidateProperty> manager = stub;

        stub.IsSelfValid.Value = true;

        Assert.True(manager.IsSelfValid);
    }

    [Fact]
    public void IsValid_CanBeConfigured()
    {
        var stub = new Stubs.IValidatePropertyManager();
        IValidatePropertyManager<IValidateProperty> manager = stub;

        stub.IsValid.Value = false;

        Assert.False(manager.IsValid);
    }

    [Fact]
    public void PropertyMessages_CanBeConfigured()
    {
        var stub = new Stubs.IValidatePropertyManager();
        IValidatePropertyManager<IValidateProperty> manager = stub;

        var messages = new List<IPropertyMessage>();
        stub.PropertyMessages.Value = messages;

        Assert.Same(messages, manager.PropertyMessages);
    }

    [Fact]
    public void IsPaused_CanBeConfigured()
    {
        var stub = new Stubs.IValidatePropertyManager();
        IValidatePropertyManager<IValidateProperty> manager = stub;

        stub.IsPaused.Value = true;

        Assert.True(manager.IsPaused);
    }

    #endregion

    #region Method Tests

    [Fact]
    public async Task WaitForTasks_TracksCall()
    {
        var stub = new Stubs.IValidatePropertyManager();
        IValidatePropertyManager<IValidateProperty> manager = stub;

        await manager.WaitForTasks();

        Assert.True(stub.WaitForTasks.WasCalled);
        Assert.Equal(1, stub.WaitForTasks.CallCount);
    }

    [Fact]
    public void HasProperty_TracksCall()
    {
        var stub = new Stubs.IValidatePropertyManager();
        IValidatePropertyManager<IValidateProperty> manager = stub;

        manager.HasProperty("TestProperty");

        Assert.True(stub.HasProperty.WasCalled);
    }

    [Fact]
    public void HasProperty_ReturnsConfiguredValue()
    {
        var stub = new Stubs.IValidatePropertyManager();
        IValidatePropertyManager<IValidateProperty> manager = stub;

        stub.HasProperty.OnCall((ko, name) => name == "ExistingProperty");

        Assert.True(manager.HasProperty("ExistingProperty"));
        Assert.False(manager.HasProperty("NonExistent"));
    }

    [Fact]
    public void GetProperty_TracksCall()
    {
        var stub = new Stubs.IValidatePropertyManager();
        IValidatePropertyManager<IValidateProperty> manager = stub;

        var propertyStub = new ValidatePropertyStubForManager();
        stub.GetProperty.OnCall((ko, name) => propertyStub);

        manager.GetProperty("Name");

        Assert.True(stub.GetProperty.WasCalled);
        Assert.Equal("Name", stub.GetProperty.LastCallArg);
    }

    [Fact]
    public void GetProperty_ReturnsConfiguredValue()
    {
        var stub = new Stubs.IValidatePropertyManager();
        IValidatePropertyManager<IValidateProperty> manager = stub;

        var propertyStub = new ValidatePropertyStubForManager();
        stub.GetProperty.OnCall((ko, name) => propertyStub);

        var result = manager.GetProperty("TestProperty");

        Assert.Same(propertyStub, result);
    }

    [Fact]
    public void Indexer_TracksAccess()
    {
        var stub = new Stubs.IValidatePropertyManager();
        IValidatePropertyManager<IValidateProperty> manager = stub;

        var propertyStub = new ValidatePropertyStubForManager();
        stub.Indexer.OnGet = (ko, name) => propertyStub;

        _ = manager["PropertyName"];

        Assert.Equal(1, stub.Indexer.GetCount);
        Assert.Equal("PropertyName", stub.Indexer.LastGetKey);
    }

    [Fact]
    public void SetProperties_TracksCall()
    {
        var stub = new Stubs.IValidatePropertyManager();
        IValidatePropertyManager<IValidateProperty> manager = stub;

        var properties = new List<IValidateProperty>();
        manager.SetProperties(properties);

        Assert.True(stub.SetProperties.WasCalled);
    }

    [Fact]
    public async Task RunRules_TracksCall()
    {
        var stub = new Stubs.IValidatePropertyManager();
        IValidatePropertyManager<IValidateProperty> manager = stub;

        await manager.RunRules(RunRulesFlag.All, null);

        Assert.True(stub.RunRules.WasCalled);
    }

    [Fact]
    public void PauseAllActions_TracksCall()
    {
        var stub = new Stubs.IValidatePropertyManager();
        IValidatePropertyManager<IValidateProperty> manager = stub;

        manager.PauseAllActions();

        Assert.True(stub.PauseAllActions.WasCalled);
    }

    [Fact]
    public void ResumeAllActions_TracksCall()
    {
        var stub = new Stubs.IValidatePropertyManager();
        IValidatePropertyManager<IValidateProperty> manager = stub;

        manager.ResumeAllActions();

        Assert.True(stub.ResumeAllActions.WasCalled);
    }

    [Fact]
    public void ClearAllMessages_TracksCall()
    {
        var stub = new Stubs.IValidatePropertyManager();
        IValidatePropertyManager<IValidateProperty> manager = stub;

        manager.ClearAllMessages();

        Assert.True(stub.ClearAllMessages.WasCalled);
    }

    [Fact]
    public void ClearSelfMessages_TracksCall()
    {
        var stub = new Stubs.IValidatePropertyManager();
        IValidatePropertyManager<IValidateProperty> manager = stub;

        manager.ClearSelfMessages();

        Assert.True(stub.ClearSelfMessages.WasCalled);
    }

    #endregion

    #region Event Tests

    [Fact]
    public void PropertyChanged_EventCanBeSubscribed()
    {
        var stub = new Stubs.IValidatePropertyManager();
        IValidatePropertyManager<IValidateProperty> manager = stub;

        manager.PropertyChanged += (s, e) => { };

        Assert.Equal(1, stub.PropertyChangedInterceptor.AddCount);
    }

    [Fact]
    public void NeatooPropertyChanged_EventCanBeSubscribed()
    {
        var stub = new Stubs.IValidatePropertyManager();
        IValidatePropertyManager<IValidateProperty> manager = stub;

        manager.NeatooPropertyChanged += (args) => Task.CompletedTask;

        Assert.Equal(1, stub.NeatooPropertyChangedInterceptor.AddCount);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsPropertyTracking()
    {
        var stub = new Stubs.IValidatePropertyManager();
        IValidatePropertyManager<IValidateProperty> manager = stub;

        stub.IsBusy.Value = true;
        _ = manager.IsBusy;
        _ = manager.IsBusy;

        stub.IsBusy.Reset();

        Assert.Equal(0, stub.IsBusy.GetCount);
    }

    [Fact]
    public void Reset_ClearsMethodTracking()
    {
        var stub = new Stubs.IValidatePropertyManager();
        IValidatePropertyManager<IValidateProperty> manager = stub;

        manager.PauseAllActions();
        manager.PauseAllActions();

        stub.PauseAllActions.Reset();

        Assert.False(stub.PauseAllActions.WasCalled);
    }

    #endregion
}

/// <summary>
/// Standalone stub for IValidatePropertyManager&lt;IValidateProperty&gt;.
/// </summary>
[KnockOff]
public partial class ValidatePropertyManagerStub : IValidatePropertyManager<IValidateProperty>
{
}

/// <summary>
/// Standalone stub for IValidateProperty used in property manager tests.
/// </summary>
[KnockOff]
public partial class ValidatePropertyStubForManager : IValidateProperty
{
}

public class IValidatePropertyManagerStandaloneTests
{
    [Fact]
    public void StandaloneStub_CanBeInstantiated()
    {
        var stub = new ValidatePropertyManagerStub();
        Assert.NotNull(stub);
    }

    [Fact]
    public void StandaloneStub_ImplementsInterface()
    {
        var stub = new ValidatePropertyManagerStub();
        IValidatePropertyManager<IValidateProperty> manager = stub;
        Assert.NotNull(manager);
    }

    [Fact]
    public void IsBusy_CanBeConfigured()
    {
        var stub = new ValidatePropertyManagerStub();
        IValidatePropertyManager<IValidateProperty> manager = stub;

        stub.IsBusy.Value = true;

        Assert.True(manager.IsBusy);
    }

    [Fact]
    public void HasProperty_TracksCall()
    {
        var stub = new ValidatePropertyManagerStub();
        IValidatePropertyManager<IValidateProperty> manager = stub;

        // Configure callback to enable tracking
        var tracking = stub.HasProperty.OnCall((ko, name) => false);

        manager.HasProperty("Test");

        // Tracking is available via the returned tracking object
        Assert.True(tracking.WasCalled);
    }
}
