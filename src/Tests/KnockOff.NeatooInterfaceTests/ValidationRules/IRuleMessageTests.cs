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

        stub.RuleIndex.Get(42u);

        Assert.Equal(42u, message.RuleIndex);
        stub.RuleIndex.VerifyGet(Times.Once);
    }

    [Fact]
    public void PropertyName_CanBeConfigured()
    {
        var stub = new Stubs.IRuleMessage();
        IRuleMessage message = stub;

        stub.PropertyName.Get("TestProperty");

        Assert.Equal("TestProperty", message.PropertyName);
    }

    [Fact]
    public void Message_CanBeConfigured()
    {
        var stub = new Stubs.IRuleMessage();
        IRuleMessage message = stub;

        stub.Message.Get("Error message");

        Assert.Equal("Error message", message.Message);
    }

    [Fact]
    public void Message_CanBeNull()
    {
        var stub = new Stubs.IRuleMessage();
        IRuleMessage message = stub;

        stub.Message.Get((string?)null);

        Assert.Null(message.Message);
    }

    #endregion

    #region Get Callback Tests

    [Fact]
    public void RuleIndex_OnGet_ExecutesCallback()
    {
        var stub = new Stubs.IRuleMessage();
        IRuleMessage message = stub;
        var callbackExecuted = false;

        stub.RuleIndex.Get(() =>
        {
            callbackExecuted = true;
            return 10u;
        });

        var index = message.RuleIndex;

        Assert.True(callbackExecuted);
        Assert.Equal(10u, index);
    }

    [Fact]
    public void PropertyName_OnGet_ExecutesCallback()
    {
        var stub = new Stubs.IRuleMessage();
        IRuleMessage message = stub;

        stub.PropertyName.Get(() => "DynamicPropertyName");

        Assert.Equal("DynamicPropertyName", message.PropertyName);
    }

    [Fact]
    public void Message_OnGet_ExecutesCallback()
    {
        var stub = new Stubs.IRuleMessage();
        IRuleMessage message = stub;

        stub.Message.Get(() => "Dynamic error message");

        Assert.Equal("Dynamic error message", message.Message);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsPropertyTracking()
    {
        var stub = new Stubs.IRuleMessage();
        IRuleMessage message = stub;

        stub.RuleIndex.Get(1);
        _ = message.RuleIndex;
        _ = message.RuleIndex;
        _ = message.RuleIndex;

        stub.RuleIndex.Reset();

        stub.RuleIndex.VerifyGet(Times.Never);
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

        stub.RuleIndex.Get(5u);

        Assert.Equal(5u, message.RuleIndex);
    }

    [Fact]
    public void PropertyName_CanBeConfigured()
    {
        var stub = new RuleMessageStub();
        IRuleMessage message = stub;

        stub.PropertyName.Get("Name");

        Assert.Equal("Name", message.PropertyName);
    }

    [Fact]
    public void Message_CanBeConfigured()
    {
        var stub = new RuleMessageStub();
        IRuleMessage message = stub;

        stub.Message.Get("Validation error");

        Assert.Equal("Validation error", message.Message);
    }
}
