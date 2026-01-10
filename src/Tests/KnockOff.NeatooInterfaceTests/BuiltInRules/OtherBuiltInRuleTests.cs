using Neatoo;
using Neatoo.Rules;
using Neatoo.Rules.Rules;
using System.ComponentModel;

namespace KnockOff.NeatooInterfaceTests.BuiltInRules;

/// <summary>
/// Tests for IMaxLengthRule - max length validation rule interface.
/// </summary>
[KnockOff<IMaxLengthRule>]
public partial class IMaxLengthRuleTests
{
    [Fact]
    public void InlineStub_CanBeInstantiated()
    {
        var stub = new Stubs.IMaxLengthRule();
        Assert.NotNull(stub);
    }

    [Fact]
    public void InlineStub_ImplementsInterface()
    {
        var stub = new Stubs.IMaxLengthRule();
        IMaxLengthRule rule = stub;
        Assert.NotNull(rule);
    }

    [Fact]
    public void InlineStub_ImplementsIRule()
    {
        var stub = new Stubs.IMaxLengthRule();
        IRule baseRule = stub;
        Assert.NotNull(baseRule);
    }

    [Fact]
    public void ErrorMessage_CanBeConfigured()
    {
        var stub = new Stubs.IMaxLengthRule();
        IMaxLengthRule rule = stub;

        stub.ErrorMessage.Value = "Max length exceeded.";

        Assert.Equal("Max length exceeded.", rule.ErrorMessage);
    }

    [Fact]
    public void Length_CanBeConfigured()
    {
        var stub = new Stubs.IMaxLengthRule();
        IMaxLengthRule rule = stub;

        stub.Length.Value = 100;

        Assert.Equal(100, rule.Length);
    }
}

/// <summary>
/// Standalone stub for IMaxLengthRule.
/// </summary>
[KnockOff]
public partial class MaxLengthRuleStub : IMaxLengthRule
{
}

public class IMaxLengthRuleStandaloneTests
{
    [Fact]
    public void StandaloneStub_CanBeInstantiated()
    {
        var stub = new MaxLengthRuleStub();
        Assert.NotNull(stub);
    }

    [Fact]
    public void Length_CanBeConfigured()
    {
        var stub = new MaxLengthRuleStub();
        IMaxLengthRule rule = stub;

        stub.Length.OnGet = (ko) => 50;

        Assert.Equal(50, rule.Length);
    }
}

/// <summary>
/// Tests for IMinLengthRule - min length validation rule interface.
/// </summary>
[KnockOff<IMinLengthRule>]
public partial class IMinLengthRuleTests
{
    [Fact]
    public void InlineStub_CanBeInstantiated()
    {
        var stub = new Stubs.IMinLengthRule();
        Assert.NotNull(stub);
    }

    [Fact]
    public void InlineStub_ImplementsInterface()
    {
        var stub = new Stubs.IMinLengthRule();
        IMinLengthRule rule = stub;
        Assert.NotNull(rule);
    }

    [Fact]
    public void ErrorMessage_CanBeConfigured()
    {
        var stub = new Stubs.IMinLengthRule();
        IMinLengthRule rule = stub;

        stub.ErrorMessage.Value = "Min length not met.";

        Assert.Equal("Min length not met.", rule.ErrorMessage);
    }

    [Fact]
    public void Length_CanBeConfigured()
    {
        var stub = new Stubs.IMinLengthRule();
        IMinLengthRule rule = stub;

        stub.Length.Value = 5;

        Assert.Equal(5, rule.Length);
    }
}

/// <summary>
/// Standalone stub for IMinLengthRule.
/// </summary>
[KnockOff]
public partial class MinLengthRuleStub : IMinLengthRule
{
}

/// <summary>
/// Tests for IStringLengthRule - string length validation rule interface.
/// </summary>
[KnockOff<IStringLengthRule>]
public partial class IStringLengthRuleTests
{
    [Fact]
    public void InlineStub_CanBeInstantiated()
    {
        var stub = new Stubs.IStringLengthRule();
        Assert.NotNull(stub);
    }

    [Fact]
    public void ErrorMessage_CanBeConfigured()
    {
        var stub = new Stubs.IStringLengthRule();
        IStringLengthRule rule = stub;

        stub.ErrorMessage.Value = "String length invalid.";

        Assert.Equal("String length invalid.", rule.ErrorMessage);
    }

    [Fact]
    public void MinimumLength_CanBeConfigured()
    {
        var stub = new Stubs.IStringLengthRule();
        IStringLengthRule rule = stub;

        stub.MinimumLength.Value = 2;

        Assert.Equal(2, rule.MinimumLength);
    }

    [Fact]
    public void MaximumLength_CanBeConfigured()
    {
        var stub = new Stubs.IStringLengthRule();
        IStringLengthRule rule = stub;

        stub.MaximumLength.Value = 50;

        Assert.Equal(50, rule.MaximumLength);
    }
}

/// <summary>
/// Standalone stub for IStringLengthRule.
/// </summary>
[KnockOff]
public partial class StringLengthRuleStub : IStringLengthRule
{
}

/// <summary>
/// Tests for IEmailAddressRule - email validation rule interface.
/// </summary>
[KnockOff<IEmailAddressRule>]
public partial class IEmailAddressRuleTests
{
    [Fact]
    public void InlineStub_CanBeInstantiated()
    {
        var stub = new Stubs.IEmailAddressRule();
        Assert.NotNull(stub);
    }

    [Fact]
    public void ErrorMessage_CanBeConfigured()
    {
        var stub = new Stubs.IEmailAddressRule();
        IEmailAddressRule rule = stub;

        stub.ErrorMessage.Value = "Invalid email address.";

        Assert.Equal("Invalid email address.", rule.ErrorMessage);
    }
}

/// <summary>
/// Standalone stub for IEmailAddressRule.
/// </summary>
[KnockOff]
public partial class EmailAddressRuleStub : IEmailAddressRule
{
}

/// <summary>
/// Tests for IRegularExpressionRule - regex validation rule interface.
/// </summary>
[KnockOff<IRegularExpressionRule>]
public partial class IRegularExpressionRuleTests
{
    [Fact]
    public void InlineStub_CanBeInstantiated()
    {
        var stub = new Stubs.IRegularExpressionRule();
        Assert.NotNull(stub);
    }

    [Fact]
    public void ErrorMessage_CanBeConfigured()
    {
        var stub = new Stubs.IRegularExpressionRule();
        IRegularExpressionRule rule = stub;

        stub.ErrorMessage.Value = "Pattern mismatch.";

        Assert.Equal("Pattern mismatch.", rule.ErrorMessage);
    }

    [Fact]
    public void Pattern_CanBeConfigured()
    {
        var stub = new Stubs.IRegularExpressionRule();
        IRegularExpressionRule rule = stub;

        stub.Pattern.Value = @"^\d{5}(-\d{4})?$";

        Assert.Equal(@"^\d{5}(-\d{4})?$", rule.Pattern);
    }
}

/// <summary>
/// Standalone stub for IRegularExpressionRule.
/// </summary>
[KnockOff]
public partial class RegularExpressionRuleStub : IRegularExpressionRule
{
}

/// <summary>
/// Tests for IRangeRule - range validation rule interface.
/// </summary>
[KnockOff<IRangeRule>]
public partial class IRangeRuleTests
{
    [Fact]
    public void InlineStub_CanBeInstantiated()
    {
        var stub = new Stubs.IRangeRule();
        Assert.NotNull(stub);
    }

    [Fact]
    public void ErrorMessage_CanBeConfigured()
    {
        var stub = new Stubs.IRangeRule();
        IRangeRule rule = stub;

        stub.ErrorMessage.Value = "Value out of range.";

        Assert.Equal("Value out of range.", rule.ErrorMessage);
    }

    [Fact]
    public void Minimum_CanBeConfigured()
    {
        var stub = new Stubs.IRangeRule();
        IRangeRule rule = stub;

        stub.Minimum.Value = 0;

        Assert.Equal(0, rule.Minimum);
    }

    [Fact]
    public void Maximum_CanBeConfigured()
    {
        var stub = new Stubs.IRangeRule();
        IRangeRule rule = stub;

        stub.Maximum.Value = 100;

        Assert.Equal(100, rule.Maximum);
    }

    [Fact]
    public void Minimum_OnGet_ReturnsConfiguredValue()
    {
        var stub = new Stubs.IRangeRule();
        IRangeRule rule = stub;

        stub.Minimum.OnGet = (ko) => 10.5m; // Decimal value

        Assert.Equal(10.5m, rule.Minimum);
    }

    [Fact]
    public void Maximum_OnGet_ReturnsConfiguredValue()
    {
        var stub = new Stubs.IRangeRule();
        IRangeRule rule = stub;

        stub.Maximum.OnGet = (ko) => DateTime.Today; // DateTime value

        Assert.Equal(DateTime.Today, rule.Maximum);
    }
}

/// <summary>
/// Standalone stub for IRangeRule.
/// </summary>
[KnockOff]
public partial class RangeRuleStub : IRangeRule
{
}

/// <summary>
/// Tests for IAttributeToRule - attribute-to-rule converter interface.
/// This interface has a generic method.
/// </summary>
[KnockOff<IAttributeToRule>]
public partial class IAttributeToRuleTests
{
    [Fact]
    public void InlineStub_CanBeInstantiated()
    {
        var stub = new Stubs.IAttributeToRule();
        Assert.NotNull(stub);
    }

    [Fact]
    public void InlineStub_ImplementsInterface()
    {
        var stub = new Stubs.IAttributeToRule();
        IAttributeToRule converter = stub;
        Assert.NotNull(converter);
    }

    [Fact]
    public void GetRule_Generic_TracksCall()
    {
        var stub = new Stubs.IAttributeToRule();
        IAttributeToRule converter = stub;

        var propertyInfoStub = new PropertyInfoStubForAttr();
        converter.GetRule<ValidateBaseStubForAttr>(propertyInfoStub, null);

        Assert.True(stub.GetRule.WasCalled);
    }

    [Fact]
    public void GetRule_Generic_TracksTypeArgument()
    {
        var stub = new Stubs.IAttributeToRule();
        IAttributeToRule converter = stub;

        var propertyInfoStub = new PropertyInfoStubForAttr();
        converter.GetRule<ValidateBaseStubForAttr>(propertyInfoStub, null);

        Assert.True(stub.GetRule.Of<ValidateBaseStubForAttr>().WasCalled);
    }

    [Fact]
    public void GetRule_Generic_ReturnsConfiguredRule()
    {
        var stub = new Stubs.IAttributeToRule();
        IAttributeToRule converter = stub;

        var ruleStub = new RuleStubForAttr();
        stub.GetRule.Of<ValidateBaseStubForAttr>().OnCall = (ko, propInfo, attr) => ruleStub;

        var propertyInfoStub = new PropertyInfoStubForAttr();
        var result = converter.GetRule<ValidateBaseStubForAttr>(propertyInfoStub, null);

        Assert.Same(ruleStub, result);
    }

    [Fact]
    public void GetRule_Generic_CanReturnNull()
    {
        var stub = new Stubs.IAttributeToRule();
        IAttributeToRule converter = stub;

        stub.GetRule.Of<ValidateBaseStubForAttr>().OnCall = (ko, propInfo, attr) => null;

        var propertyInfoStub = new PropertyInfoStubForAttr();
        var result = converter.GetRule<ValidateBaseStubForAttr>(propertyInfoStub, new object());

        Assert.Null(result);
    }
}

/// <summary>
/// Standalone stub for IAttributeToRule.
/// </summary>
[KnockOff]
public partial class AttributeToRuleStub : IAttributeToRule
{
}

/// <summary>
/// Standalone stub for IPropertyInfo used in IAttributeToRule tests.
/// </summary>
[KnockOff]
public partial class PropertyInfoStubForAttr : IPropertyInfo
{
}

/// <summary>
/// Standalone stub for IValidateBase used in IAttributeToRule tests.
/// This must be a class (not interface) due to the generic constraint.
/// </summary>
public class ValidateBaseStubForAttr : IValidateBase
{
    public IValidateBase? Parent => null;
    public bool IsPaused => false;
    public bool IsBusy => false;
    public Task WaitForTasks() => Task.CompletedTask;
    public Task WaitForTasks(CancellationToken token) => Task.CompletedTask;
    public bool IsValid => true;
    public bool IsSelfValid => true;
    public IReadOnlyCollection<IPropertyMessage> PropertyMessages => new List<IPropertyMessage>();
    public Task RunRules(string propertyName, CancellationToken? token = null) => Task.CompletedTask;
    public Task RunRules(RunRulesFlag runRules = RunRulesFlag.All, CancellationToken? token = null) => Task.CompletedTask;
    public void ClearAllMessages() { }
    public void ClearSelfMessages() { }
    public IValidateProperty GetProperty(string propertyName) => null!;
    public bool TryGetProperty(string propertyName, out IValidateProperty validateProperty) { validateProperty = null!; return false; }
    public IValidateProperty this[string propertyName] => null!;
#pragma warning disable CS0067 // Event is never used
    public event PropertyChangedEventHandler? PropertyChanged;
    public event NeatooPropertyChanged? NeatooPropertyChanged;
#pragma warning restore CS0067
}

/// <summary>
/// Standalone stub for IRule used in IAttributeToRule tests.
/// </summary>
[KnockOff]
public partial class RuleStubForAttr : IRule
{
}

public class IAttributeToRuleStandaloneTests
{
    [Fact]
    public void StandaloneStub_CanBeInstantiated()
    {
        var stub = new AttributeToRuleStub();
        Assert.NotNull(stub);
    }

    [Fact]
    public void GetRule_Generic_TracksCall()
    {
        var stub = new AttributeToRuleStub();
        IAttributeToRule converter = stub;

        var propertyInfoStub = new PropertyInfoStubForAttr();
        converter.GetRule<ValidateBaseStubForAttr>(propertyInfoStub, null);

        // Standalone stub may have different interceptor naming
        Assert.True(stub.GetRule.WasCalled);
    }
}
