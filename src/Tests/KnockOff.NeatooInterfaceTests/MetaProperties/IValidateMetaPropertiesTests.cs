using Neatoo;
using Neatoo.Rules;

namespace KnockOff.NeatooInterfaceTests.MetaProperties;

/// <summary>
/// Tests for IValidateMetaProperties - validation state tracking meta-properties.
/// This interface has properties, async methods, and method overloads.
/// </summary>
[KnockOff<IValidateMetaProperties>]
[KnockOff<IPropertyMessage>]
public partial class IValidateMetaPropertiesTests
{
    [Fact]
    public void InlineStub_CanBeInstantiated()
    {
        var stub = new Stubs.IValidateMetaProperties();
        Assert.NotNull(stub);
    }

    [Fact]
    public void InlineStub_ImplementsInterface()
    {
        var stub = new Stubs.IValidateMetaProperties();
        IValidateMetaProperties meta = stub;
        Assert.NotNull(meta);
    }

    #region Property Tests

    [Fact]
    public void IsBusy_CanBeConfiguredViaValue()
    {
        var stub = new Stubs.IValidateMetaProperties();
        IValidateMetaProperties meta = stub;

        stub.IsBusy.Value = true;

        Assert.True(meta.IsBusy);
        Assert.Equal(1, stub.IsBusy.GetCount);
    }

    [Fact]
    public void IsBusy_CanBeConfiguredViaOnGet()
    {
        var stub = new Stubs.IValidateMetaProperties();
        IValidateMetaProperties meta = stub;

        stub.IsBusy.OnGet = (ko) => true;

        Assert.True(meta.IsBusy);
    }

    [Fact]
    public void IsValid_CanBeConfigured()
    {
        var stub = new Stubs.IValidateMetaProperties();
        IValidateMetaProperties meta = stub;

        stub.IsValid.Value = true;

        Assert.True(meta.IsValid);
    }

    [Fact]
    public void IsSelfValid_CanBeConfigured()
    {
        var stub = new Stubs.IValidateMetaProperties();
        IValidateMetaProperties meta = stub;

        stub.IsSelfValid.Value = false;

        Assert.False(meta.IsSelfValid);
    }

    [Fact]
    public void PropertyMessages_CanBeConfigured()
    {
        var stub = new Stubs.IValidateMetaProperties();
        IValidateMetaProperties meta = stub;

        var messages = new List<IPropertyMessage>();
        stub.PropertyMessages.Value = messages;

        Assert.Same(messages, meta.PropertyMessages);
    }

    #endregion

    #region Method Tests

    [Fact]
    public async Task WaitForTasks_NoArg_TracksCall()
    {
        var stub = new Stubs.IValidateMetaProperties();
        IValidateMetaProperties meta = stub;

        await meta.WaitForTasks();

        // Method overloads should be distinguishable
        Assert.True(stub.WaitForTasks.WasCalled);
        Assert.Equal(1, stub.WaitForTasks.CallCount);
    }

    [Fact]
    public async Task WaitForTasks_WithToken_TracksCall()
    {
        var stub = new Stubs.IValidateMetaProperties();
        IValidateMetaProperties meta = stub;
        using var cts = new CancellationTokenSource();

        await meta.WaitForTasks(cts.Token);

        // The overload with CancellationToken should be tracked
        // Generator should distinguish overloads
        Assert.True(stub.WaitForTasks.WasCalled);
    }

    [Fact]
    public async Task RunRules_PropertyName_TracksCall()
    {
        var stub = new Stubs.IValidateMetaProperties();
        IValidateMetaProperties meta = stub;

        await meta.RunRules("TestProperty", null);

        // RunRules has overloads - string and RunRulesFlag versions
        Assert.True(stub.RunRules.WasCalled);
    }

    [Fact]
    public async Task RunRules_WithFlag_TracksCall()
    {
        var stub = new Stubs.IValidateMetaProperties();
        IValidateMetaProperties meta = stub;

        await meta.RunRules(RunRulesFlag.All, null);

        Assert.True(stub.RunRules.WasCalled);
    }

    [Fact]
    public async Task RunRules_CanExecuteCallback()
    {
        var stub = new Stubs.IValidateMetaProperties();
        IValidateMetaProperties meta = stub;
        var callbackExecuted = false;

        stub.RunRules.OnCall = (ko, propOrFlag, token, extra) =>
        {
            callbackExecuted = true;
            return Task.CompletedTask;
        };

        await meta.RunRules("Property", null);

        Assert.True(callbackExecuted);
    }

    [Fact]
    public void ClearAllMessages_TracksCall()
    {
        var stub = new Stubs.IValidateMetaProperties();
        IValidateMetaProperties meta = stub;

        meta.ClearAllMessages();

        Assert.True(stub.ClearAllMessages.WasCalled);
        Assert.Equal(1, stub.ClearAllMessages.CallCount);
    }

    [Fact]
    public void ClearSelfMessages_TracksCall()
    {
        var stub = new Stubs.IValidateMetaProperties();
        IValidateMetaProperties meta = stub;

        meta.ClearSelfMessages();

        Assert.True(stub.ClearSelfMessages.WasCalled);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsPropertyTracking()
    {
        var stub = new Stubs.IValidateMetaProperties();
        IValidateMetaProperties meta = stub;

        stub.IsBusy.Value = true;
        _ = meta.IsBusy;
        _ = meta.IsBusy;

        stub.IsBusy.Reset();

        Assert.Equal(0, stub.IsBusy.GetCount);
    }

    [Fact]
    public async Task Reset_ClearsMethodTracking()
    {
        var stub = new Stubs.IValidateMetaProperties();
        IValidateMetaProperties meta = stub;

        await meta.WaitForTasks();

        stub.WaitForTasks.Reset();

        Assert.False(stub.WaitForTasks.WasCalled);
        Assert.Equal(0, stub.WaitForTasks.CallCount);
    }

    #endregion
}

/// <summary>
/// Standalone stub for IValidateMetaProperties.
/// </summary>
[KnockOff]
public partial class ValidateMetaPropertiesStub : IValidateMetaProperties
{
}

public class IValidateMetaPropertiesStandaloneTests
{
    [Fact]
    public void StandaloneStub_CanBeInstantiated()
    {
        var stub = new ValidateMetaPropertiesStub();
        Assert.NotNull(stub);
    }

    [Fact]
    public void StandaloneStub_ImplementsInterface()
    {
        var stub = new ValidateMetaPropertiesStub();
        IValidateMetaProperties meta = stub;
        Assert.NotNull(meta);
    }

    [Fact]
    public void IsBusy_CanBeConfigured()
    {
        var stub = new ValidateMetaPropertiesStub();
        IValidateMetaProperties meta = stub;

        stub.IsBusy.OnGet = (ko) => true;

        Assert.True(meta.IsBusy);
    }

    [Fact]
    public async Task WaitForTasks_TracksCall()
    {
        var stub = new ValidateMetaPropertiesStub();
        IValidateMetaProperties meta = stub;

        await meta.WaitForTasks();

        // Standalone stubs use flat API - interceptors on stub directly
        Assert.True(stub.WaitForTasks1.WasCalled);
    }

    [Fact]
    public void PropertyMessages_CanBeConfigured()
    {
        var stub = new ValidateMetaPropertiesStub();
        IValidateMetaProperties meta = stub;

        var propertyMessageStub = new IValidateMetaPropertiesTests.Stubs.IPropertyMessage();
        propertyMessageStub.Message.Value = "Required";
        var messages = new List<IPropertyMessage> { propertyMessageStub };
        stub.PropertyMessages.OnGet = (ko) => messages;

        Assert.Same(messages, meta.PropertyMessages);
    }
}
