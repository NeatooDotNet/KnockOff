using Neatoo;
using Neatoo.Rules;

namespace KnockOff.NeatooInterfaceTests.ValidationRules;

/// <summary>
/// Tests for IRule&lt;T&gt; - the strongly-typed business rule interface.
/// This is a generic interface with a constraint (where T : IValidateBase).
/// </summary>
[KnockOff<IRule<IValidateBase>>]
public partial class IRuleOfTTests
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
        IRule<IValidateBase> rule = stub;
        Assert.NotNull(rule);
    }

    [Fact]
    public void InlineStub_ImplementsBaseIRule()
    {
        var stub = new Stubs.IRule();
        IRule baseRule = stub;
        Assert.NotNull(baseRule);
    }

    #region IRule<T> Specific Method Tests

    [Fact]
    public async Task RunRule_StronglyTyped_TracksCall()
    {
        var stub = new Stubs.IRule();
        IRule<IValidateBase> rule = stub;

        var validateStub = new ValidateBaseStubForRuleT();

        // Must provide OnCall for methods with return types
        stub.RunRule.OnCall = (ko, target, token) => Task.FromResult<IRuleMessages>(RuleMessages.None);

        await rule.RunRule(validateStub, null);

        Assert.True(stub.RunRule.WasCalled);
        Assert.Equal(1, stub.RunRule.CallCount);
    }

    [Fact]
    public async Task RunRule_StronglyTyped_CapturesTypedTarget()
    {
        var stub = new Stubs.IRule();
        IRule<IValidateBase> rule = stub;
        IValidateBase? capturedTarget = null;

        stub.RunRule.OnCall = (ko, target, token) =>
        {
            capturedTarget = target;
            return Task.FromResult<IRuleMessages>(RuleMessages.None);
        };

        var validateStub = new ValidateBaseStubForRuleT();
        await rule.RunRule(validateStub, null);

        Assert.Same(validateStub, capturedTarget);
    }

    [Fact]
    public async Task RunRule_StronglyTyped_ReturnsConfiguredResult()
    {
        var stub = new Stubs.IRule();
        IRule<IValidateBase> rule = stub;

        var expectedMessages = new RuleMessages();
        expectedMessages.Add("Name", "Required");

        stub.RunRule.OnCall = (ko, target, token) => Task.FromResult<IRuleMessages>(expectedMessages);

        var validateStub = new ValidateBaseStubForRuleT();
        var result = await rule.RunRule(validateStub, null);

        Assert.Same(expectedMessages, result);
    }

    [Fact]
    public async Task RunRule_StronglyTyped_WithCancellation()
    {
        var stub = new Stubs.IRule();
        IRule<IValidateBase> rule = stub;
        CancellationToken? capturedToken = null;

        stub.RunRule.OnCall = (ko, target, token) =>
        {
            capturedToken = token;
            return Task.FromResult<IRuleMessages>(RuleMessages.None);
        };

        using var cts = new CancellationTokenSource();
        var validateStub = new ValidateBaseStubForRuleT();

        await rule.RunRule(validateStub, cts.Token);

        Assert.Equal(cts.Token, capturedToken);
    }

    #endregion

    #region Inherited IRule Properties

    [Fact]
    public void Executed_FromBaseInterface_CanBeConfigured()
    {
        var stub = new Stubs.IRule();
        IRule<IValidateBase> rule = stub;

        stub.Executed.Value = true;

        Assert.True(rule.Executed);
    }

    [Fact]
    public void RuleOrder_FromBaseInterface_CanBeConfigured()
    {
        var stub = new Stubs.IRule();
        IRule<IValidateBase> rule = stub;

        stub.RuleOrder.Value = 10;

        Assert.Equal(10, rule.RuleOrder);
    }

    [Fact]
    public void Messages_FromBaseInterface_CanBeConfigured()
    {
        var stub = new Stubs.IRule();
        IRule<IValidateBase> rule = stub;

        var messages = new List<IRuleMessage> { new RuleMessage("Prop", "Error") };
        stub.Messages.Value = messages;

        Assert.Same(messages, rule.Messages);
    }

    #endregion
}

/// <summary>
/// Standalone stub for IRule&lt;IValidateBase&gt;.
/// </summary>
[KnockOff]
public partial class RuleOfTStub : IRule<IValidateBase>
{
}

/// <summary>
/// Standalone stub for IValidateBase used in IRule&lt;T&gt; tests.
/// </summary>
[KnockOff]
public partial class ValidateBaseStubForRuleT : IValidateBase
{
}

public class IRuleOfTStandaloneTests
{
    [Fact]
    public void StandaloneStub_CanBeInstantiated()
    {
        var stub = new RuleOfTStub();
        Assert.NotNull(stub);
    }

    [Fact]
    public void StandaloneStub_ImplementsInterface()
    {
        var stub = new RuleOfTStub();
        IRule<IValidateBase> rule = stub;
        Assert.NotNull(rule);
    }

    [Fact]
    public void StandaloneStub_ImplementsBaseIRule()
    {
        var stub = new RuleOfTStub();
        IRule baseRule = stub;
        Assert.NotNull(baseRule);
    }

    [Fact]
    public async Task RunRule_StronglyTyped_TracksCall()
    {
        var stub = new RuleOfTStub();
        IRule<IValidateBase> rule = stub;

        var validateStub = new ValidateBaseStubForRuleT();

        // Must provide OnCall for methods with return types
        var tracking = stub.RunRule.OnCall((ko, target, token) => Task.FromResult<IRuleMessages>(RuleMessages.None));

        await rule.RunRule(validateStub, null);

        Assert.True(tracking.WasCalled);
    }
}

/// <summary>
/// Test with a concrete type constraint.
/// This tests generic interface stubbing with a specific type argument.
/// </summary>
public interface ICustomValidateBase : IValidateBase
{
    string CustomProperty { get; }
}

[KnockOff]
public partial class CustomValidateBaseStub : ICustomValidateBase
{
}

[KnockOff<IRule<ICustomValidateBase>>]
public partial class IRuleOfCustomTypeTests
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
        IRule<ICustomValidateBase> rule = stub;
        Assert.NotNull(rule);
    }

    [Fact]
    public async Task RunRule_AcceptsCustomType()
    {
        var stub = new Stubs.IRule();
        IRule<ICustomValidateBase> rule = stub;

        var customStub = new CustomValidateBaseStub();

        // Must provide OnCall for methods with return types
        stub.RunRule1.OnCall = (ko, target, token) => Task.FromResult<IRuleMessages>(RuleMessages.None);

        await rule.RunRule(customStub, null);

        Assert.True(stub.RunRule1.WasCalled);
    }

    /// <summary>
    /// Regression test for bug: inherited interface method with different parameter types.
    /// When IRule&lt;ICustomValidateBase&gt; inherits from IRule, the base IRule.RunRule(IValidateBase)
    /// method must also work, not just IRule&lt;T&gt;.RunRule(T).
    /// </summary>
    [Fact]
    public async Task RunRule_BaseInterfaceMethod_WorksWithDerivedType()
    {
        var stub = new Stubs.IRule();

        // Cast to base interface - this is the critical test
        IRule baseRule = stub;

        // Call via base interface with a type that implements IValidateBase
        var customStub = new CustomValidateBaseStub();

        // Must provide OnCall for methods with return types
        stub.RunRule1.OnCall = (ko, target, token) => Task.FromResult<IRuleMessages>(RuleMessages.None);

        await baseRule.RunRule(customStub, null);

        // The call should be tracked (delegated to the typed implementation)
        Assert.True(stub.RunRule1.WasCalled);
    }
}
