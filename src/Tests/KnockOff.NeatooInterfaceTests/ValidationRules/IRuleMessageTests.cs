using Neatoo.Rules;

namespace KnockOff.NeatooInterfaceTests.ValidationRules;

/// <summary>
/// Tests for IRuleMessage - validation message interface.
/// This is a simple interface with read-only properties.
/// </summary>
[KnockOff<IRuleMessage>]
public partial class IRuleMessageTests
{
    [Fact]
    public void InlineStub_CanBeInstantiated()
    {
        var stub = new Stubs.IRuleMessage();
        Assert.NotNull(stub);
    }

    [Fact]
    public void InlineStub_ImplementsInterface()
    {
        var stub = new Stubs.IRuleMessage();
        IRuleMessage message = stub;
        Assert.NotNull(message);
    }

    #region Property Tests

    [Fact]
    public void RuleIndex_CanBeConfigured()
    {
        var stub = new Stubs.IRuleMessage();
        IRuleMessage message = stub;

        stub.RuleIndex.Value = 42u;

        Assert.Equal(42u, message.RuleIndex);
        Assert.Equal(1, stub.RuleIndex.GetCount);
    }

    [Fact]
    public void PropertyName_CanBeConfigured()
    {
        var stub = new Stubs.IRuleMessage();
        IRuleMessage message = stub;

        stub.PropertyName.Value = "TestProperty";

        Assert.Equal("TestProperty", message.PropertyName);
    }

    [Fact]
    public void Message_CanBeConfigured()
    {
        var stub = new Stubs.IRuleMessage();
        IRuleMessage message = stub;

        stub.Message.Value = "Error message";

        Assert.Equal("Error message", message.Message);
    }

    [Fact]
    public void Message_CanBeNull()
    {
        var stub = new Stubs.IRuleMessage();
        IRuleMessage message = stub;

        stub.Message.Value = null;

        Assert.Null(message.Message);
    }

    #endregion

    #region OnGet Callback Tests

    [Fact]
    public void RuleIndex_OnGet_ExecutesCallback()
    {
        var stub = new Stubs.IRuleMessage();
        IRuleMessage message = stub;
        var callbackExecuted = false;

        stub.RuleIndex.OnGet = (ko) =>
        {
            callbackExecuted = true;
            return 10u;
        };

        var index = message.RuleIndex;

        Assert.True(callbackExecuted);
        Assert.Equal(10u, index);
    }

    [Fact]
    public void PropertyName_OnGet_ExecutesCallback()
    {
        var stub = new Stubs.IRuleMessage();
        IRuleMessage message = stub;

        stub.PropertyName.OnGet = (ko) => "DynamicPropertyName";

        Assert.Equal("DynamicPropertyName", message.PropertyName);
    }

    [Fact]
    public void Message_OnGet_ExecutesCallback()
    {
        var stub = new Stubs.IRuleMessage();
        IRuleMessage message = stub;

        stub.Message.OnGet = (ko) => "Dynamic error message";

        Assert.Equal("Dynamic error message", message.Message);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsPropertyTracking()
    {
        var stub = new Stubs.IRuleMessage();
        IRuleMessage message = stub;

        stub.RuleIndex.Value = 1;
        _ = message.RuleIndex;
        _ = message.RuleIndex;
        _ = message.RuleIndex;

        stub.RuleIndex.Reset();

        Assert.Equal(0, stub.RuleIndex.GetCount);
    }

    #endregion
}

/// <summary>
/// Standalone stub for IRuleMessage.
/// </summary>
[KnockOff]
public partial class RuleMessageStub : IRuleMessage
{
}

public class IRuleMessageStandaloneTests
{
    [Fact]
    public void StandaloneStub_CanBeInstantiated()
    {
        var stub = new RuleMessageStub();
        Assert.NotNull(stub);
    }

    [Fact]
    public void StandaloneStub_ImplementsInterface()
    {
        var stub = new RuleMessageStub();
        IRuleMessage message = stub;
        Assert.NotNull(message);
    }

    [Fact]
    public void RuleIndex_CanBeConfigured()
    {
        var stub = new RuleMessageStub();
        IRuleMessage message = stub;

        stub.RuleIndex.Value = 5u;

        Assert.Equal(5u, message.RuleIndex);
    }

    [Fact]
    public void PropertyName_CanBeConfigured()
    {
        var stub = new RuleMessageStub();
        IRuleMessage message = stub;

        stub.PropertyName.Value = "Name";

        Assert.Equal("Name", message.PropertyName);
    }

    [Fact]
    public void Message_CanBeConfigured()
    {
        var stub = new RuleMessageStub();
        IRuleMessage message = stub;

        stub.Message.Value = "Validation error";

        Assert.Equal("Validation error", message.Message);
    }
}
