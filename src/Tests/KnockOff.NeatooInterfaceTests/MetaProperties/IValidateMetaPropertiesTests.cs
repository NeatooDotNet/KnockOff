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

        stub.IsBusy.OnGet(true);

        Assert.True(meta.IsBusy);
        stub.IsBusy.VerifyGet(Times.Once);
    }

    [Fact]
    public void IsBusy_CanBeConfiguredViaOnGet()
    {
        var stub = new Stubs.IValidateMetaProperties();
        IValidateMetaProperties meta = stub;

        stub.IsBusy.OnGet(() => true);

        Assert.True(meta.IsBusy);
    }

    [Fact]
    public void IsValid_CanBeConfigured()
    {
        var stub = new Stubs.IValidateMetaProperties();
        IValidateMetaProperties meta = stub;

        stub.IsValid.OnGet(true);

        Assert.True(meta.IsValid);
    }

    [Fact]
    public void IsSelfValid_CanBeConfigured()
    {
        var stub = new Stubs.IValidateMetaProperties();
        IValidateMetaProperties meta = stub;

        stub.IsSelfValid.OnGet(false);

        Assert.False(meta.IsSelfValid);
    }

    [Fact]
    public void PropertyMessages_CanBeConfigured()
    {
        var stub = new Stubs.IValidateMetaProperties();
        IValidateMetaProperties meta = stub;

        var messages = new List<IPropertyMessage>();
        stub.PropertyMessages.OnGet(messages);

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
        stub.WaitForTasks.Verify(Times.Once);
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
        stub.WaitForTasks.Verify();
    }

    [Fact]
    public async Task RunRules_PropertyName_TracksCall()
    {
        var stub = new Stubs.IValidateMetaProperties();
        IValidateMetaProperties meta = stub;

        await meta.RunRules("TestProperty", null);

        // RunRules has overloads - string and RunRulesFlag versions
        stub.RunRules.Verify();
    }

    [Fact]
    public async Task RunRules_WithFlag_TracksCall()
    {
        var stub = new Stubs.IValidateMetaProperties();
        IValidateMetaProperties meta = stub;

        await meta.RunRules(RunRulesFlag.All, null);

        stub.RunRules.Verify();
    }

    [Fact]
    public async Task RunRules_CanExecuteCallback()
    {
        var stub = new Stubs.IValidateMetaProperties();
        IValidateMetaProperties meta = stub;
        var callbackExecuted = false;

        stub.RunRules.OnCall((Stubs.IValidateMetaProperties_RunRulesInterceptor.RunRulesDelegate_String_Threading_CancellationToken_Threading_Tasks_Task)((propOrFlag, token) =>
        {
            callbackExecuted = true;
            return Task.CompletedTask;
        }));

        await meta.RunRules("Property", null);

        Assert.True(callbackExecuted);
    }

    [Fact]
    public void ClearAllMessages_TracksCall()
    {
        var stub = new Stubs.IValidateMetaProperties();
        IValidateMetaProperties meta = stub;

        meta.ClearAllMessages();

        stub.ClearAllMessages.Verify(Times.Once);
    }

    [Fact]
    public void ClearSelfMessages_TracksCall()
    {
        var stub = new Stubs.IValidateMetaProperties();
        IValidateMetaProperties meta = stub;

        meta.ClearSelfMessages();

        stub.ClearSelfMessages.Verify();
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsPropertyTracking()
    {
        var stub = new Stubs.IValidateMetaProperties();
        IValidateMetaProperties meta = stub;

        stub.IsBusy.OnGet(true);
        _ = meta.IsBusy;
        _ = meta.IsBusy;

        stub.IsBusy.Reset();

        stub.IsBusy.VerifyGet(Times.Never);
    }

    [Fact]
    public async Task Reset_ClearsMethodTracking()
    {
        var stub = new Stubs.IValidateMetaProperties();
        IValidateMetaProperties meta = stub;

        await meta.WaitForTasks();

        stub.WaitForTasks.Reset();

        stub.WaitForTasks.Verify(Times.Never);
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

        stub.IsBusy.OnGet(true);

        Assert.True(meta.IsBusy);
    }

    [Fact]
    public async Task WaitForTasks_TracksCall()
    {
        var stub = new ValidateMetaPropertiesStub();
        IValidateMetaProperties meta = stub;

        // Configure callback to enable tracking
        var tracking = stub.WaitForTasks.OnCall(() => Task.CompletedTask);

        await meta.WaitForTasks();

        // Tracking is available via the returned tracking object
        tracking.Verify();
    }

    [Fact]
    public void PropertyMessages_CanBeConfigured()
    {
        var stub = new ValidateMetaPropertiesStub();
        IValidateMetaProperties meta = stub;

        var propertyMessageStub = new IValidateMetaPropertiesTests.Stubs.IPropertyMessage();
        propertyMessageStub.Message.OnGet("Required");
        var messages = new List<IPropertyMessage> { propertyMessageStub };
        stub.PropertyMessages.OnGet(() => messages);

        Assert.Same(messages, meta.PropertyMessages);
    }
}
