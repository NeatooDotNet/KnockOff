using Neatoo;
using Neatoo.Rules;

namespace KnockOff.NeatooInterfaceTests.ValidationRules;

/// <summary>
/// Tests for IRule - the base business rule interface.
/// This interface has properties, methods, and async methods.
/// </summary>
[KnockOff<IRule>]
public partial class IRuleTests
{
    [Fact]
    public void InlineStub_CanBeInstantiated()
    {
        var stub = new Stubs.IRule();
        Assert.NotNull(stub);
    }

    [Fact]
    public void InlineStub_ImplementsInterface()
    {
        var stub = new Stubs.IRule();
        IRule rule = stub;
        Assert.NotNull(rule);
    }

    #region Property Tests

    [Fact]
    public void Executed_CanBeConfigured()
    {
        var stub = new Stubs.IRule();
        IRule rule = stub;

        stub.Executed.Value = true;

        Assert.True(rule.Executed);
        Assert.Equal(1, stub.Executed.GetCount);
    }

    [Fact]
    public void RuleOrder_CanBeConfigured()
    {
        var stub = new Stubs.IRule();
        IRule rule = stub;

        stub.RuleOrder.Value = 5;

        Assert.Equal(5, rule.RuleOrder);
    }

    [Fact]
    public void UniqueIndex_CanBeConfigured()
    {
        var stub = new Stubs.IRule();
        IRule rule = stub;

        stub.UniqueIndex.Value = 42;

        Assert.Equal(42u, rule.UniqueIndex);
    }

    [Fact]
    public void Messages_CanBeConfigured()
    {
        var stub = new Stubs.IRule();
        IRule rule = stub;

        var messages = new List<IRuleMessage>();
        stub.Messages.Value = messages;

        Assert.Same(messages, rule.Messages);
    }

    [Fact]
    public void TriggerProperties_CanBeConfigured()
    {
        var stub = new Stubs.IRule();
        IRule rule = stub;

        var triggers = new List<ITriggerProperty>();
        stub.TriggerProperties.Value = triggers;

        Assert.Same(triggers, rule.TriggerProperties);
    }

    #endregion

    #region Method Tests

    [Fact]
    public async Task RunRule_TracksCall()
    {
        var stub = new Stubs.IRule();
        IRule rule = stub;

        // Create a mock IValidateBase
        var validateStub = new ValidateBaseStubForRule();

        // Must provide OnCall for methods with return types
        stub.RunRule.OnCall = (ko, target, token) => Task.FromResult<IRuleMessages>(RuleMessages.None);

        await rule.RunRule(validateStub, null);

        Assert.True(stub.RunRule.WasCalled);
        Assert.Equal(1, stub.RunRule.CallCount);
    }

    [Fact]
    public async Task RunRule_CapturesArguments()
    {
        var stub = new Stubs.IRule();
        IRule rule = stub;

        var validateStub = new ValidateBaseStubForRule();
        using var cts = new CancellationTokenSource();

        // Must provide OnCall for methods with return types
        stub.RunRule.OnCall = (ko, target, token) => Task.FromResult<IRuleMessages>(RuleMessages.None);

        await rule.RunRule(validateStub, cts.Token);

        // Verify arguments were captured
        Assert.NotNull(stub.RunRule.LastCallArgs);
    }

    [Fact]
    public async Task RunRule_CanExecuteCallback()
    {
        var stub = new Stubs.IRule();
        IRule rule = stub;
        IValidateBase? capturedTarget = null;

        stub.RunRule.OnCall = (ko, target, token) =>
        {
            capturedTarget = target;
            return Task.FromResult<IRuleMessages>(RuleMessages.None);
        };

        var validateStub = new ValidateBaseStubForRule();
        await rule.RunRule(validateStub, null);

        Assert.Same(validateStub, capturedTarget);
    }

    [Fact]
    public async Task RunRule_ReturnsConfiguredResult()
    {
        var stub = new Stubs.IRule();
        IRule rule = stub;

        var expectedMessages = new RuleMessages();
        expectedMessages.Add("Property", "Error message");

        stub.RunRule.OnCall = (ko, target, token) => Task.FromResult<IRuleMessages>(expectedMessages);

        var validateStub = new ValidateBaseStubForRule();
        var result = await rule.RunRule(validateStub, null);

        Assert.Same(expectedMessages, result);
    }

    [Fact]
    public void OnRuleAdded_TracksCall()
    {
        var stub = new Stubs.IRule();
        IRule rule = stub;

        // Create a mock IRuleManager
        var ruleManagerStub = new RuleManagerStubForRule();

        rule.OnRuleAdded(ruleManagerStub, 10);

        Assert.True(stub.OnRuleAdded.WasCalled);
        Assert.Equal(1, stub.OnRuleAdded.CallCount);
    }

    [Fact]
    public void OnRuleAdded_CapturesArguments()
    {
        var stub = new Stubs.IRule();
        IRule rule = stub;

        var ruleManagerStub = new RuleManagerStubForRule();

        rule.OnRuleAdded(ruleManagerStub, 42);

        Assert.NotNull(stub.OnRuleAdded.LastCallArgs);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsPropertyTracking()
    {
        var stub = new Stubs.IRule();
        IRule rule = stub;

        stub.Executed.Value = true;
        _ = rule.Executed;
        _ = rule.Executed;

        stub.Executed.Reset();

        Assert.Equal(0, stub.Executed.GetCount);
    }

    [Fact]
    public async Task Reset_ClearsMethodTracking()
    {
        var stub = new Stubs.IRule();
        IRule rule = stub;

        var validateStub = new ValidateBaseStubForRule();

        // Must provide OnCall for methods with return types
        stub.RunRule.OnCall = (ko, target, token) => Task.FromResult<IRuleMessages>(RuleMessages.None);

        await rule.RunRule(validateStub, null);
        await rule.RunRule(validateStub, null);

        stub.RunRule.Reset();

        Assert.False(stub.RunRule.WasCalled);
        Assert.Equal(0, stub.RunRule.CallCount);
    }

    #endregion
}

/// <summary>
/// Standalone stub for IRule.
/// </summary>
[KnockOff]
public partial class RuleStub : IRule
{
}

/// <summary>
/// Standalone stub for IValidateBase used in IRule tests.
/// </summary>
[KnockOff]
public partial class ValidateBaseStubForRule : IValidateBase
{
}

/// <summary>
/// Standalone stub for IRuleManager used in IRule tests.
/// </summary>
[KnockOff]
public partial class RuleManagerStubForRule : IRuleManager
{
}

public class IRuleStandaloneTests
{
    [Fact]
    public void StandaloneStub_CanBeInstantiated()
    {
        var stub = new RuleStub();
        Assert.NotNull(stub);
    }

    [Fact]
    public void StandaloneStub_ImplementsInterface()
    {
        var stub = new RuleStub();
        IRule rule = stub;
        Assert.NotNull(rule);
    }

    [Fact]
    public void Executed_CanBeConfigured()
    {
        var stub = new RuleStub();
        IRule rule = stub;

        stub.Executed.Value = true;

        Assert.True(rule.Executed);
    }

    [Fact]
    public async Task RunRule_TracksCall()
    {
        var stub = new RuleStub();
        IRule rule = stub;

        var validateStub = new ValidateBaseStubForRule();

        // Must provide OnCall for methods with return types
        var tracking = stub.RunRule.OnCall((ko, target, token) => Task.FromResult<IRuleMessages>(RuleMessages.None));

        await rule.RunRule(validateStub, null);

        Assert.True(tracking.WasCalled);
    }
}
