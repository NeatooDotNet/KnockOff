using Neatoo;
using Neatoo.Rules;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KnockOff.NeatooInterfaceTests.Properties;

/// <summary>
/// Tests for IValidateProperty - managed property interface with validation.
/// This interface extends INotifyPropertyChanged and INotifyNeatooPropertyChanged.
/// It has many properties, async methods, and default interface implementations.
/// </summary>
[KnockOff<IValidateProperty>]
public partial class IValidatePropertyTests
{
    [Fact]
    public void InlineStub_CanBeInstantiated()
    {
        var stub = new Stubs.IValidateProperty();
        Assert.NotNull(stub);
    }

    [Fact]
    public void InlineStub_ImplementsInterface()
    {
        var stub = new Stubs.IValidateProperty();
        IValidateProperty property = stub;
        Assert.NotNull(property);
    }

    [Fact]
    public void InlineStub_ImplementsINotifyPropertyChanged()
    {
        var stub = new Stubs.IValidateProperty();
        INotifyPropertyChanged notifyPropertyChanged = stub;
        Assert.NotNull(notifyPropertyChanged);
    }

    [Fact]
    public void InlineStub_ImplementsINotifyNeatooPropertyChanged()
    {
        var stub = new Stubs.IValidateProperty();
        INotifyNeatooPropertyChanged notifyNeatoo = stub;
        Assert.NotNull(notifyNeatoo);
    }

    #region Property Tests

    [Fact]
    public void Name_CanBeConfigured()
    {
        var stub = new Stubs.IValidateProperty();
        IValidateProperty property = stub;

        stub.Name.Get("TestProperty");

        Assert.Equal("TestProperty", property.Name);
        stub.Name.VerifyGet(Times.Once);
    }

    [Fact]
    public void Value_CanBeConfiguredForGet()
    {
        var stub = new Stubs.IValidateProperty();
        IValidateProperty property = stub;

        stub.Value.Get("TestValue");

        Assert.Equal("TestValue", property.Value);
    }

    [Fact]
    public void Value_CanTrackSetter()
    {
        var stub = new Stubs.IValidateProperty();
        IValidateProperty property = stub;

        property.Value = "NewValue";

        stub.Value.VerifySet(Times.Once);
        Assert.Equal("NewValue", stub.Value.LastSetValue);
    }

    [Fact]
    public void Task_CanBeConfigured()
    {
        var stub = new Stubs.IValidateProperty();
        IValidateProperty property = stub;

        stub.Task.Get(Task.CompletedTask);

        Assert.Same(Task.CompletedTask, property.Task);
    }

    [Fact]
    public void IsBusy_CanBeConfigured()
    {
        var stub = new Stubs.IValidateProperty();
        IValidateProperty property = stub;

        stub.IsBusy.Get(true);

        Assert.True(property.IsBusy);
    }

    [Fact]
    public void IsReadOnly_CanBeConfigured()
    {
        var stub = new Stubs.IValidateProperty();
        IValidateProperty property = stub;

        stub.IsReadOnly.Get(true);

        Assert.True(property.IsReadOnly);
    }

    [Fact]
    public void Type_CanBeConfigured()
    {
        var stub = new Stubs.IValidateProperty();
        IValidateProperty property = stub;

        stub.Type.Get(typeof(string));

        Assert.Equal(typeof(string), property.Type);
    }

    [Fact]
    public void IsSelfValid_CanBeConfigured()
    {
        var stub = new Stubs.IValidateProperty();
        IValidateProperty property = stub;

        stub.IsSelfValid.Get(true);

        Assert.True(property.IsSelfValid);
    }

    [Fact]
    public void IsValid_CanBeConfigured()
    {
        var stub = new Stubs.IValidateProperty();
        IValidateProperty property = stub;

        stub.IsValid.Get(false);

        Assert.False(property.IsValid);
    }

    [Fact]
    public void PropertyMessages_CanBeConfigured()
    {
        var stub = new Stubs.IValidateProperty();
        IValidateProperty property = stub;

        var messages = new List<IPropertyMessage>();
        stub.PropertyMessages.Get(messages);

        Assert.Same(messages, property.PropertyMessages);
    }

    #endregion

    #region Method Tests

    [Fact]
    public async Task SetValue_TracksCall()
    {
        var stub = new Stubs.IValidateProperty();
        IValidateProperty property = stub;

        await property.SetValue("NewValue");

        stub.SetValue.Verify(Times.Once);
    }

    [Fact]
    public async Task SetValue_CapturesArgument()
    {
        var stub = new Stubs.IValidateProperty();
        IValidateProperty property = stub;

        await property.SetValue("CapturedValue");

        Assert.Equal("CapturedValue", stub.SetValue.LastArg);
    }

    [Fact]
    public void AddMarkedBusy_TracksCall()
    {
        var stub = new Stubs.IValidateProperty();
        IValidateProperty property = stub;

        property.AddMarkedBusy(12345);

        stub.AddMarkedBusy.Verify();
        Assert.Equal(12345L, stub.AddMarkedBusy.LastArg);
    }

    [Fact]
    public void RemoveMarkedBusy_TracksCall()
    {
        var stub = new Stubs.IValidateProperty();
        IValidateProperty property = stub;

        property.RemoveMarkedBusy(12345);

        stub.RemoveMarkedBusy.Verify();
        Assert.Equal(12345L, stub.RemoveMarkedBusy.LastArg);
    }

    [Fact]
    public void LoadValue_TracksCall()
    {
        var stub = new Stubs.IValidateProperty();
        IValidateProperty property = stub;

        property.LoadValue("LoadedValue");

        stub.LoadValue.Verify();
        Assert.Equal("LoadedValue", stub.LoadValue.LastArg);
    }

    [Fact]
    public async Task WaitForTasks_TracksCall()
    {
        var stub = new Stubs.IValidateProperty();
        IValidateProperty property = stub;

        await property.WaitForTasks();

        stub.WaitForTasks.Verify();
    }

    [Fact]
    public async Task RunRules_TracksCall()
    {
        var stub = new Stubs.IValidateProperty();
        IValidateProperty property = stub;

        await property.RunRules(RunRulesFlag.All, null);

        stub.RunRules.Verify();
    }

    [Fact]
    public async Task RunRules_CapturesArguments()
    {
        var stub = new Stubs.IValidateProperty();
        IValidateProperty property = stub;
        RunRulesFlag? capturedFlag = null;

        stub.RunRules.Return((flag, token) =>
        {
            capturedFlag = flag;
            return Task.CompletedTask;
        });

        await property.RunRules(RunRulesFlag.All, null);

        Assert.Equal(RunRulesFlag.All, capturedFlag);
    }

    #endregion

    #region Event Tests

    [Fact]
    public void PropertyChanged_EventCanBeSubscribed()
    {
        var stub = new Stubs.IValidateProperty();
        IValidateProperty property = stub;

        property.PropertyChanged += (s, e) => { };

        stub.PropertyChanged.VerifyAdd(Times.Once);
    }

    [Fact]
    public void PropertyChanged_EventCanBeUnsubscribed()
    {
        var stub = new Stubs.IValidateProperty();
        IValidateProperty property = stub;

        PropertyChangedEventHandler handler = (s, e) => { };
        property.PropertyChanged += handler;
        property.PropertyChanged -= handler;

        stub.PropertyChanged.VerifyAdd(Times.Once);
        stub.PropertyChanged.VerifyRemove(Times.Once);
    }

    [Fact]
    public void NeatooPropertyChanged_EventCanBeSubscribed()
    {
        var stub = new Stubs.IValidateProperty();
        IValidateProperty property = stub;

        property.NeatooPropertyChanged += (args) => Task.CompletedTask;

        stub.NeatooPropertyChanged.VerifyAdd(Times.Once);
    }

    #endregion

    #region Default Interface Implementation Tests

    [Fact]
    public void StringValue_CanBeConfigured()
    {
        var stub = new Stubs.IValidateProperty();
        IValidateProperty property = stub;

        // Configure StringValue directly (stubs override DIMs with explicit implementations)
        stub.StringValue.Get("TestValue");

        var stringValue = property.StringValue;

        Assert.Equal("TestValue", stringValue);
    }

    [Fact]
    public void StringValue_ReturnsNull_WhenValueIsNull()
    {
        var stub = new Stubs.IValidateProperty();
        IValidateProperty property = stub;

        stub.Value.Get((string?)null);

        var stringValue = property.StringValue;

        Assert.Null(stringValue);
    }

    [Fact]
    public void GetAwaiter_CanBeConfigured()
    {
        var stub = new Stubs.IValidateProperty();
        IValidateProperty property = stub;

        // Configure GetAwaiter directly (stubs override DIMs with explicit implementations)
        stub.GetAwaiter.Return(() => Task.CompletedTask.GetAwaiter());

        var awaiter = property.GetAwaiter();

        Assert.True(awaiter.IsCompleted);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsPropertyTracking()
    {
        var stub = new Stubs.IValidateProperty();
        IValidateProperty property = stub;

        stub.Name.Get("Test");
        _ = property.Name;
        _ = property.Name;

        stub.Name.Reset();

        stub.Name.VerifyGet(Times.Never);
    }

    [Fact]
    public async Task Reset_ClearsMethodTracking()
    {
        var stub = new Stubs.IValidateProperty();
        IValidateProperty property = stub;

        await property.SetValue("Test");
        await property.SetValue("Test2");

        stub.SetValue.Reset();

        stub.SetValue.Verify(Times.Never);
    }

    #endregion
}

/// <summary>
/// Standalone stub for IValidateProperty.
/// </summary>
[KnockOff]
public partial class ValidatePropertyStub : IValidateProperty
{
}

public class IValidatePropertyStandaloneTests
{
    [Fact]
    public void StandaloneStub_CanBeInstantiated()
    {
        var stub = new ValidatePropertyStub();
        Assert.NotNull(stub);
    }

    [Fact]
    public void StandaloneStub_ImplementsInterface()
    {
        var stub = new ValidatePropertyStub();
        IValidateProperty property = stub;
        Assert.NotNull(property);
    }

    [Fact]
    public void Name_CanBeConfigured()
    {
        var stub = new ValidatePropertyStub();
        IValidateProperty property = stub;

        stub.Name.Get("StandaloneName");

        Assert.Equal("StandaloneName", property.Name);
    }

    [Fact]
    public void IsBusy_CanBeConfigured()
    {
        var stub = new ValidatePropertyStub();
        IValidateProperty property = stub;

        stub.IsBusy.Get(true);

        Assert.True(property.IsBusy);
    }
}

/// <summary>
/// Tests for IValidateProperty&lt;T&gt; - typed managed property interface.
/// This extends IValidateProperty.
/// </summary>
[KnockOff<IValidateProperty<string>>]
public partial class IValidatePropertyOfTTests
{
    [Fact]
    public void InlineStub_CanBeInstantiated()
    {
        var stub = new Stubs.IValidateProperty();
        Assert.NotNull(stub);
    }

    [Fact]
    public void InlineStub_ImplementsInterface()
    {
        var stub = new Stubs.IValidateProperty();
        IValidateProperty<string> property = stub;
        Assert.NotNull(property);
    }

    [Fact]
    public void InlineStub_ImplementsBaseInterface()
    {
        var stub = new Stubs.IValidateProperty();
        IValidateProperty baseProperty = stub;
        Assert.NotNull(baseProperty);
    }

    [Fact]
    public void Value_Typed_CanBeConfiguredForGet()
    {
        var stub = new Stubs.IValidateProperty();
        IValidateProperty<string> property = stub;

        stub.Value.Get("TypedValue");

        Assert.Equal("TypedValue", property.Value);
    }

    [Fact]
    public void Value_Typed_CanTrackSetter()
    {
        var stub = new Stubs.IValidateProperty();
        IValidateProperty<string> property = stub;

        property.Value = "NewTypedValue";

        stub.Value.VerifySet(Times.Once);
        Assert.Equal("NewTypedValue", stub.Value.LastSetValue);
    }

    [Fact]
    public void Name_FromBaseInterface_CanBeConfigured()
    {
        var stub = new Stubs.IValidateProperty();
        IValidateProperty<string> property = stub;

        stub.Name.Get("TypedPropertyName");

        Assert.Equal("TypedPropertyName", property.Name);
    }
}

/// <summary>
/// Standalone stub for IValidateProperty&lt;string&gt;.
/// </summary>
[KnockOff]
public partial class ValidatePropertyOfStringStub : IValidateProperty<string>
{
}
