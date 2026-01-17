using Neatoo;
using Neatoo.Rules;

namespace KnockOff.NeatooInterfaceTests.ValidationRules;

/// <summary>
/// Tests for IRuleManager - the rule management interface.
/// This interface has method overloads and generic methods.
/// </summary>
[KnockOff<IRuleManager>]
public partial class IRuleManagerTests
{
    [Fact]
    public void InlineStub_CanBeInstantiated()
    {
        var stub = new Stubs.IRuleManager();
        Assert.NotNull(stub);
    }

    [Fact]
    public void InlineStub_ImplementsInterface()
    {
        var stub = new Stubs.IRuleManager();
        IRuleManager ruleManager = stub;
        Assert.NotNull(ruleManager);
    }

    #region Property Tests

    [Fact]
    public void Rules_CanBeConfigured()
    {
        var stub = new Stubs.IRuleManager();
        IRuleManager ruleManager = stub;

        var rules = new List<IRule>();
        stub.Rules.Value = rules;

        Assert.Same(rules, ruleManager.Rules);
        Assert.Equal(1, stub.Rules.GetCount);
    }

    #endregion

    #region RunRules Method Tests (overloaded)

    [Fact]
    public async Task RunRules_PropertyName_TracksCall()
    {
        var stub = new Stubs.IRuleManager();
        IRuleManager ruleManager = stub;

        await ruleManager.RunRules("TestProperty", null);

        Assert.True(stub.RunRules.WasCalled);
    }

    [Fact]
    public async Task RunRules_PropertyName_CapturesArgument()
    {
        var stub = new Stubs.IRuleManager();
        IRuleManager ruleManager = stub;
        string? capturedPropertyName = null;

        stub.RunRules.OnCall = (ko, propOrFlag, token, extra) =>
        {
            capturedPropertyName = propOrFlag?.ToString();
            return Task.CompletedTask;
        };

        await ruleManager.RunRules("TargetProperty", null);

        Assert.Equal("TargetProperty", capturedPropertyName);
    }

    [Fact]
    public async Task RunRules_WithFlag_TracksCall()
    {
        var stub = new Stubs.IRuleManager();
        IRuleManager ruleManager = stub;

        await ruleManager.RunRules(RunRulesFlag.All, null);

        Assert.True(stub.RunRules.WasCalled);
    }

    [Fact]
    public async Task RunRules_WithCancellation_PassesToken()
    {
        var stub = new Stubs.IRuleManager();
        IRuleManager ruleManager = stub;
        CancellationToken? capturedToken = null;

        stub.RunRules.OnCall = (ko, prop, token, extra) =>
        {
            capturedToken = token;
            return Task.CompletedTask;
        };

        using var cts = new CancellationTokenSource();
        await ruleManager.RunRules("Property", cts.Token);

        Assert.Equal(cts.Token, capturedToken);
    }

    #endregion

    #region RunRule Method Tests (non-generic and generic)

    [Fact]
    public async Task RunRule_NonGeneric_TracksCall()
    {
        var stub = new Stubs.IRuleManager();
        IRuleManager ruleManager = stub;

        var ruleStub = new RuleStubForManager();
        await ruleManager.RunRule(ruleStub, null);

        Assert.True(stub.RunRule.WasCalled);
        Assert.Equal(1, stub.RunRule.CallCount);
    }

    [Fact]
    public async Task RunRule_NonGeneric_CapturesRule()
    {
        var stub = new Stubs.IRuleManager();
        IRuleManager ruleManager = stub;
        IRule? capturedRule = null;

        stub.RunRule.OnCall = (ko, rule, token) =>
        {
            capturedRule = rule;
            return Task.CompletedTask;
        };

        var ruleStub = new RuleStubForManager();
        await ruleManager.RunRule(ruleStub, null);

        Assert.Same(ruleStub, capturedRule);
    }

    [Fact]
    public async Task RunRule_Generic_TracksCall()
    {
        var stub = new Stubs.IRuleManager();
        IRuleManager ruleManager = stub;

        await ruleManager.RunRule<TestRuleForManager>(null);

        // Generic methods should use a separate interceptor
        Assert.True(stub.RunRuleGeneric.WasCalled);
    }

    [Fact]
    public async Task RunRule_Generic_TracksTypeArgument()
    {
        var stub = new Stubs.IRuleManager();
        IRuleManager ruleManager = stub;

        await ruleManager.RunRule<TestRuleForManager>(null);

        // Generic method tracking should track the type argument
        Assert.True(stub.RunRuleGeneric.Of<TestRuleForManager>().WasCalled);
        Assert.Equal(1, stub.RunRuleGeneric.Of<TestRuleForManager>().CallCount);
    }

    [Fact]
    public async Task RunRule_Generic_DifferentTypes_TrackedSeparately()
    {
        var stub = new Stubs.IRuleManager();
        IRuleManager ruleManager = stub;

        await ruleManager.RunRule<TestRuleForManager>(null);
        await ruleManager.RunRule<AnotherTestRule>(null);
        await ruleManager.RunRule<TestRuleForManager>(null);

        Assert.Equal(2, stub.RunRuleGeneric.Of<TestRuleForManager>().CallCount);
        Assert.Equal(1, stub.RunRuleGeneric.Of<AnotherTestRule>().CallCount);
        Assert.Equal(3, stub.RunRuleGeneric.TotalCallCount);
    }

    #endregion

    #region AddRule Method Tests (generic)

    [Fact]
    public void AddRule_Generic_TracksCall()
    {
        var stub = new Stubs.IRuleManager();
        IRuleManager ruleManager = stub;

        var ruleStub = new RuleOfTStubForManager();
        ruleManager.AddRule<IValidateBase>(ruleStub);

        Assert.True(stub.AddRule.WasCalled);
    }

    [Fact]
    public void AddRule_Generic_TracksTypeArgument()
    {
        var stub = new Stubs.IRuleManager();
        IRuleManager ruleManager = stub;

        var ruleStub = new RuleOfTStubForManager();
        ruleManager.AddRule<IValidateBase>(ruleStub);

        Assert.True(stub.AddRule.Of<IValidateBase>().WasCalled);
    }

    [Fact]
    public void AddRules_Generic_TracksCall()
    {
        var stub = new Stubs.IRuleManager();
        IRuleManager ruleManager = stub;

        var rule1 = new RuleOfTStubForManager();
        var rule2 = new RuleOfTStubForManager();

        ruleManager.AddRules<IValidateBase>(rule1, rule2);

        Assert.True(stub.AddRules.WasCalled);
    }

    [Fact]
    public void AddRules_Generic_TracksTypeArgument()
    {
        var stub = new Stubs.IRuleManager();
        IRuleManager ruleManager = stub;

        var rule1 = new RuleOfTStubForManager();
        var rule2 = new RuleOfTStubForManager();

        ruleManager.AddRules<IValidateBase>(rule1, rule2);

        Assert.True(stub.AddRules.Of<IValidateBase>().WasCalled);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public async Task Reset_ClearsMethodTracking()
    {
        var stub = new Stubs.IRuleManager();
        IRuleManager ruleManager = stub;

        await ruleManager.RunRules("Property", null);
        await ruleManager.RunRules(RunRulesFlag.All, null);

        stub.RunRules.Reset();

        Assert.False(stub.RunRules.WasCalled);
        Assert.Equal(0, stub.RunRules.CallCount);
    }

    [Fact]
    public async Task Reset_GenericMethod_ClearsAllTypeArgs()
    {
        var stub = new Stubs.IRuleManager();
        IRuleManager ruleManager = stub;

        await ruleManager.RunRule<TestRuleForManager>(null);
        await ruleManager.RunRule<AnotherTestRule>(null);

        stub.RunRuleGeneric.Reset();

        Assert.False(stub.RunRuleGeneric.WasCalled);
        Assert.Equal(0, stub.RunRuleGeneric.TotalCallCount);
    }

    #endregion
}

/// <summary>
/// Standalone stub for IRuleManager.
/// </summary>
[KnockOff]
public partial class RuleManagerStub : IRuleManager
{
}

/// <summary>
/// Test rule for generic method tests.
/// </summary>
[KnockOff]
public partial class TestRuleForManager : IRule
{
}

/// <summary>
/// Another test rule for generic method tests.
/// </summary>
[KnockOff]
public partial class AnotherTestRule : IRule
{
}

/// <summary>
/// Stub for IRule used in IRuleManager tests.
/// </summary>
[KnockOff]
public partial class RuleStubForManager : IRule
{
}

/// <summary>
/// Stub for IRule&lt;IValidateBase&gt; used in IRuleManager tests.
/// </summary>
[KnockOff]
public partial class RuleOfTStubForManager : IRule<IValidateBase>
{
}

public class IRuleManagerStandaloneTests
{
    [Fact]
    public void StandaloneStub_CanBeInstantiated()
    {
        var stub = new RuleManagerStub();
        Assert.NotNull(stub);
    }

    [Fact]
    public void StandaloneStub_ImplementsInterface()
    {
        var stub = new RuleManagerStub();
        IRuleManager ruleManager = stub;
        Assert.NotNull(ruleManager);
    }

    [Fact]
    public void Rules_CanBeConfigured()
    {
        var stub = new RuleManagerStub();
        IRuleManager ruleManager = stub;

        var rules = new List<IRule>();
        stub.Rules.OnGet = (ko) => rules;

        Assert.Same(rules, ruleManager.Rules);
    }

    [Fact]
    public async Task RunRules_TracksCall()
    {
        var stub = new RuleManagerStub();
        IRuleManager ruleManager = stub;

        // Configure callback to enable tracking (string overload)
        var tracking = stub.RunRules.OnCall((RuleManagerStub ko, string propertyName, CancellationToken? token) => Task.CompletedTask);

        await ruleManager.RunRules("Property", null);

        // Tracking is available via the returned tracking object
        Assert.True(tracking.WasCalled);
    }
}
