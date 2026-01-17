using Neatoo;
using Neatoo.Rules;
using Neatoo.Rules.Rules;

namespace KnockOff.NeatooInterfaceTests.BuiltInRules;

/// <summary>
/// Tests for IRequiredRule - required validation rule interface.
/// This interface extends IRule.
/// </summary>
[KnockOff<IRequiredRule>]
public partial class IRequiredRuleTests
{
    [Fact]
    public void InlineStub_CanBeInstantiated()
    {
        var stub = new Stubs.IRequiredRule();
        Assert.NotNull(stub);
    }

    [Fact]
    public void InlineStub_ImplementsInterface()
    {
        var stub = new Stubs.IRequiredRule();
        IRequiredRule rule = stub;
        Assert.NotNull(rule);
    }

    [Fact]
    public void InlineStub_ImplementsIRule()
    {
        var stub = new Stubs.IRequiredRule();
        IRule baseRule = stub;
        Assert.NotNull(baseRule);
    }

    #region IRequiredRule Specific Property Tests

    [Fact]
    public void ErrorMessage_CanBeConfigured()
    {
        var stub = new Stubs.IRequiredRule();
        IRequiredRule rule = stub;

        stub.ErrorMessage.Value = "This field is required.";

        Assert.Equal("This field is required.", rule.ErrorMessage);
        Assert.Equal(1, stub.ErrorMessage.GetCount);
    }

    [Fact]
    public void ErrorMessage_OnGet_ExecutesCallback()
    {
        var stub = new Stubs.IRequiredRule();
        IRequiredRule rule = stub;

        stub.ErrorMessage.OnGet = (ko) => "Dynamic error message";

        Assert.Equal("Dynamic error message", rule.ErrorMessage);
    }

    #endregion

    #region Inherited IRule Property Tests

    [Fact]
    public void Executed_FromBaseInterface_CanBeConfigured()
    {
        var stub = new Stubs.IRequiredRule();
        IRequiredRule rule = stub;

        stub.Executed.Value = true;

        Assert.True(rule.Executed);
    }

    [Fact]
    public void RuleOrder_FromBaseInterface_CanBeConfigured()
    {
        var stub = new Stubs.IRequiredRule();
        IRequiredRule rule = stub;

        stub.RuleOrder.Value = 0; // Required rules typically run first

        Assert.Equal(0, rule.RuleOrder);
    }

    [Fact]
    public void UniqueIndex_FromBaseInterface_CanBeConfigured()
    {
        var stub = new Stubs.IRequiredRule();
        IRequiredRule rule = stub;

        stub.UniqueIndex.Value = 100u;

        Assert.Equal(100u, rule.UniqueIndex);
    }

    [Fact]
    public void Messages_FromBaseInterface_CanBeConfigured()
    {
        var stub = new Stubs.IRequiredRule();
        IRequiredRule rule = stub;

        var messages = new List<IRuleMessage>
        {
            new RuleMessage("Name", "Name is required.")
        };
        stub.Messages.Value = messages;

        Assert.Same(messages, rule.Messages);
    }

    [Fact]
    public void TriggerProperties_FromBaseInterface_CanBeConfigured()
    {
        var stub = new Stubs.IRequiredRule();
        IRequiredRule rule = stub;

        var triggers = new List<ITriggerProperty>();
        stub.TriggerProperties.Value = triggers;

        Assert.Same(triggers, rule.TriggerProperties);
    }

    #endregion

    #region Inherited IRule Method Tests

    [Fact]
    public async Task RunRule_FromBaseInterface_TracksCall()
    {
        var stub = new Stubs.IRequiredRule();
        IRequiredRule rule = stub;

        var validateStub = new ValidateBaseStubForRequiredRule();

        // Must provide OnCall for methods with return types
        stub.RunRule.OnCall((ko, target, token) => Task.FromResult<IRuleMessages>(RuleMessages.None));

        await rule.RunRule(validateStub, null);

        Assert.True(stub.RunRule.WasCalled);
    }

    [Fact]
    public async Task RunRule_FromBaseInterface_ReturnsConfiguredResult()
    {
        var stub = new Stubs.IRequiredRule();
        IRequiredRule rule = stub;

        var errorMessages = new RuleMessages();
        errorMessages.Add("Name", "Name is required.");

        stub.RunRule.OnCall((ko, target, token) => Task.FromResult<IRuleMessages>(errorMessages));

        var validateStub = new ValidateBaseStubForRequiredRule();
        var result = await rule.RunRule(validateStub, null);

        Assert.Same(errorMessages, result);
    }

    [Fact]
    public void OnRuleAdded_FromBaseInterface_TracksCall()
    {
        var stub = new Stubs.IRequiredRule();
        IRequiredRule rule = stub;

        var ruleManagerStub = new RuleManagerStubForRequiredRule();
        rule.OnRuleAdded(ruleManagerStub, 5);

        Assert.True(stub.OnRuleAdded.WasCalled);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsPropertyTracking()
    {
        var stub = new Stubs.IRequiredRule();
        IRequiredRule rule = stub;

        stub.ErrorMessage.Value = "Test";
        _ = rule.ErrorMessage;
        _ = rule.ErrorMessage;

        stub.ErrorMessage.Reset();

        Assert.Equal(0, stub.ErrorMessage.GetCount);
    }

    #endregion
}

/// <summary>
/// Standalone stub for IRequiredRule.
/// </summary>
[KnockOff]
public partial class RequiredRuleStub : IRequiredRule
{
}

/// <summary>
/// Standalone stub for IValidateBase used in IRequiredRule tests.
/// </summary>
[KnockOff]
public partial class ValidateBaseStubForRequiredRule : IValidateBase
{
}

/// <summary>
/// Standalone stub for IRuleManager used in IRequiredRule tests.
/// </summary>
[KnockOff]
public partial class RuleManagerStubForRequiredRule : IRuleManager
{
}

public class IRequiredRuleStandaloneTests
{
    [Fact]
    public void StandaloneStub_CanBeInstantiated()
    {
        var stub = new RequiredRuleStub();
        Assert.NotNull(stub);
    }

    [Fact]
    public void StandaloneStub_ImplementsInterface()
    {
        var stub = new RequiredRuleStub();
        IRequiredRule rule = stub;
        Assert.NotNull(rule);
    }

    [Fact]
    public void StandaloneStub_ImplementsIRule()
    {
        var stub = new RequiredRuleStub();
        IRule baseRule = stub;
        Assert.NotNull(baseRule);
    }

    [Fact]
    public void ErrorMessage_CanBeConfigured()
    {
        var stub = new RequiredRuleStub();
        IRequiredRule rule = stub;

        stub.ErrorMessage.Value = "Required field";

        Assert.Equal("Required field", rule.ErrorMessage);
    }

    [Fact]
    public void Executed_FromBaseInterface_CanBeConfigured()
    {
        var stub = new RequiredRuleStub();
        IRule rule = stub;

        stub.Executed.Value = true;

        Assert.True(rule.Executed);
    }
}
