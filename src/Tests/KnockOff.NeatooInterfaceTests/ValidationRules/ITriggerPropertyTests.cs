using Neatoo;
using Neatoo.Rules;

namespace KnockOff.NeatooInterfaceTests.ValidationRules;

/// <summary>
/// Tests for ITriggerProperty - property trigger interface.
/// </summary>
[KnockOff<ITriggerProperty>]
public partial class ITriggerPropertyTests
{
    [Fact]
    public void InlineStub_CanBeInstantiated()
    {
        var stub = new Stubs.ITriggerProperty();
        Assert.NotNull(stub);
    }

    [Fact]
    public void InlineStub_ImplementsInterface()
    {
        var stub = new Stubs.ITriggerProperty();
        ITriggerProperty trigger = stub;
        Assert.NotNull(trigger);
    }

    #region Property Tests

    [Fact]
    public void PropertyName_CanBeConfigured()
    {
        var stub = new Stubs.ITriggerProperty();
        ITriggerProperty trigger = stub;

        stub.PropertyName.Value = "TestProperty";

        Assert.Equal("TestProperty", trigger.PropertyName);
        Assert.Equal(1, stub.PropertyName.GetCount);
    }

    [Fact]
    public void PropertyName_OnGet_ExecutesCallback()
    {
        var stub = new Stubs.ITriggerProperty();
        ITriggerProperty trigger = stub;

        stub.PropertyName.OnGet = (ko) => "DynamicProperty";

        Assert.Equal("DynamicProperty", trigger.PropertyName);
    }

    #endregion

    #region Method Tests

    [Fact]
    public void IsMatch_TracksCall()
    {
        var stub = new Stubs.ITriggerProperty();
        ITriggerProperty trigger = stub;

        trigger.IsMatch("SomeProperty");

        Assert.True(stub.IsMatch.WasCalled);
        Assert.Equal(1, stub.IsMatch.CallCount);
    }

    [Fact]
    public void IsMatch_CapturesArgument()
    {
        var stub = new Stubs.ITriggerProperty();
        ITriggerProperty trigger = stub;

        trigger.IsMatch("TestProperty");

        Assert.Equal("TestProperty", stub.IsMatch.LastCallArg);
    }

    [Fact]
    public void IsMatch_ReturnsConfiguredValue()
    {
        var stub = new Stubs.ITriggerProperty();
        ITriggerProperty trigger = stub;

        stub.IsMatch.OnCall = (ko, propName) => propName == "ExpectedProperty";

        Assert.True(trigger.IsMatch("ExpectedProperty"));
        Assert.False(trigger.IsMatch("OtherProperty"));
    }

    [Fact]
    public void IsMatch_MultipleCallsTracked()
    {
        var stub = new Stubs.ITriggerProperty();
        ITriggerProperty trigger = stub;

        trigger.IsMatch("First");
        trigger.IsMatch("Second");
        trigger.IsMatch("Third");

        Assert.Equal(3, stub.IsMatch.CallCount);
        Assert.Equal("Third", stub.IsMatch.LastCallArg);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsPropertyTracking()
    {
        var stub = new Stubs.ITriggerProperty();
        ITriggerProperty trigger = stub;

        stub.PropertyName.Value = "Test";
        _ = trigger.PropertyName;
        _ = trigger.PropertyName;

        stub.PropertyName.Reset();

        Assert.Equal(0, stub.PropertyName.GetCount);
    }

    [Fact]
    public void Reset_ClearsMethodTracking()
    {
        var stub = new Stubs.ITriggerProperty();
        ITriggerProperty trigger = stub;

        trigger.IsMatch("Test");
        trigger.IsMatch("Test");

        stub.IsMatch.Reset();

        Assert.False(stub.IsMatch.WasCalled);
        Assert.Equal(0, stub.IsMatch.CallCount);
        Assert.Null(stub.IsMatch.LastCallArg);
    }

    #endregion
}

/// <summary>
/// Standalone stub for ITriggerProperty.
/// </summary>
[KnockOff]
public partial class TriggerPropertyStub : ITriggerProperty
{
}

public class ITriggerPropertyStandaloneTests
{
    [Fact]
    public void StandaloneStub_CanBeInstantiated()
    {
        var stub = new TriggerPropertyStub();
        Assert.NotNull(stub);
    }

    [Fact]
    public void StandaloneStub_ImplementsInterface()
    {
        var stub = new TriggerPropertyStub();
        ITriggerProperty trigger = stub;
        Assert.NotNull(trigger);
    }

    [Fact]
    public void PropertyName_CanBeConfigured()
    {
        var stub = new TriggerPropertyStub();
        ITriggerProperty trigger = stub;

        stub.PropertyName.OnGet = (ko) => "ConfiguredName";

        Assert.Equal("ConfiguredName", trigger.PropertyName);
    }

    [Fact]
    public void IsMatch_TracksCall()
    {
        var stub = new TriggerPropertyStub();
        ITriggerProperty trigger = stub;

        trigger.IsMatch("Test");

        Assert.True(stub.IsMatch.WasCalled);
    }
}

/// <summary>
/// Tests for ITriggerProperty&lt;T&gt; - the strongly-typed trigger property interface.
/// </summary>
[KnockOff<ITriggerProperty<IValidateBase>>]
public partial class ITriggerPropertyOfTTests
{
    [Fact]
    public void InlineStub_CanBeInstantiated()
    {
        var stub = new Stubs.ITriggerProperty();
        Assert.NotNull(stub);
    }

    [Fact]
    public void InlineStub_ImplementsInterface()
    {
        var stub = new Stubs.ITriggerProperty();
        ITriggerProperty<IValidateBase> trigger = stub;
        Assert.NotNull(trigger);
    }

    [Fact]
    public void InlineStub_ImplementsBaseInterface()
    {
        var stub = new Stubs.ITriggerProperty();
        ITriggerProperty baseTrigger = stub;
        Assert.NotNull(baseTrigger);
    }

    #region GetValue Method Tests

    [Fact]
    public void GetValue_TracksCall()
    {
        var stub = new Stubs.ITriggerProperty();
        ITriggerProperty<IValidateBase> trigger = stub;

        var targetStub = new ValidateBaseStubForTrigger();
        trigger.GetValue(targetStub);

        Assert.True(stub.GetValue.WasCalled);
        Assert.Equal(1, stub.GetValue.CallCount);
    }

    [Fact]
    public void GetValue_CapturesTarget()
    {
        var stub = new Stubs.ITriggerProperty();
        ITriggerProperty<IValidateBase> trigger = stub;
        IValidateBase? capturedTarget = null;

        stub.GetValue.OnCall = (ko, target) =>
        {
            capturedTarget = target;
            return "Value";
        };

        var targetStub = new ValidateBaseStubForTrigger();
        trigger.GetValue(targetStub);

        Assert.Same(targetStub, capturedTarget);
    }

    [Fact]
    public void GetValue_ReturnsConfiguredValue()
    {
        var stub = new Stubs.ITriggerProperty();
        ITriggerProperty<IValidateBase> trigger = stub;

        stub.GetValue.OnCall = (ko, target) => "ConfiguredValue";

        var targetStub = new ValidateBaseStubForTrigger();
        var result = trigger.GetValue(targetStub);

        Assert.Equal("ConfiguredValue", result);
    }

    #endregion

    #region Inherited Property Tests

    [Fact]
    public void PropertyName_FromBaseInterface_CanBeConfigured()
    {
        var stub = new Stubs.ITriggerProperty();
        ITriggerProperty<IValidateBase> trigger = stub;

        stub.PropertyName.Value = "InheritedProperty";

        Assert.Equal("InheritedProperty", trigger.PropertyName);
    }

    [Fact]
    public void IsMatch_FromBaseInterface_TracksCall()
    {
        var stub = new Stubs.ITriggerProperty();
        ITriggerProperty<IValidateBase> trigger = stub;

        trigger.IsMatch("Test");

        Assert.True(stub.IsMatch.WasCalled);
    }

    #endregion
}

/// <summary>
/// Standalone stub for ITriggerProperty&lt;IValidateBase&gt;.
/// </summary>
[KnockOff]
public partial class TriggerPropertyOfTStub : ITriggerProperty<IValidateBase>
{
}

/// <summary>
/// Standalone stub for IValidateBase used in ITriggerProperty tests.
/// </summary>
[KnockOff]
public partial class ValidateBaseStubForTrigger : IValidateBase
{
}

public class ITriggerPropertyOfTStandaloneTests
{
    [Fact]
    public void StandaloneStub_CanBeInstantiated()
    {
        var stub = new TriggerPropertyOfTStub();
        Assert.NotNull(stub);
    }

    [Fact]
    public void StandaloneStub_ImplementsInterface()
    {
        var stub = new TriggerPropertyOfTStub();
        ITriggerProperty<IValidateBase> trigger = stub;
        Assert.NotNull(trigger);
    }

    [Fact]
    public void GetValue_TracksCall()
    {
        var stub = new TriggerPropertyOfTStub();
        ITriggerProperty<IValidateBase> trigger = stub;

        var targetStub = new ValidateBaseStubForTrigger();
        trigger.GetValue(targetStub);

        Assert.True(stub.GetValue.WasCalled);
    }
}
