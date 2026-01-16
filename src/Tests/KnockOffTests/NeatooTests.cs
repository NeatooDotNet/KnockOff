using Neatoo;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace KnockOff.Tests;

#region Standalone Stubs - Class definitions with [KnockOff] attribute

/// <summary>
/// Standalone stub for IEntityBase - tests full entity functionality.
/// </summary>
[KnockOff]
public partial class EntityBaseStub : IEntityBase
{
}

/// <summary>
/// Standalone stub for IValidateBase - tests validation functionality.
/// </summary>
[KnockOff]
public partial class ValidateBaseStub : IValidateBase
{
}

#endregion

#region Standalone Stub Tests

/// <summary>
/// Tests for standalone IEntityBase stubs using [KnockOff] attribute on class.
/// </summary>
public class EntityBaseStandaloneTests
{
    [Fact]
    public void CanBeStubbed()
    {
        var stub = new EntityBaseStub();
        IEntityBase entity = stub;

        Assert.NotNull(entity);
        // Flat API: interceptors are accessed directly on stub (e.g., stub.IsNew, stub.Delete)
    }

    [Fact]
    public void IsNew_CanBeConfiguredViaOnGet()
    {
        var stub = new EntityBaseStub();
        IEntityBase entity = stub;

        stub.IsNew.OnGet = (ko) => true;

        Assert.True(entity.IsNew);
        Assert.Equal(1, stub.IsNew.GetCount);
    }

    [Fact]
    public void IsDeleted_TracksMultipleAccesses()
    {
        var stub = new EntityBaseStub();
        IEntityBase entity = stub;

        stub.IsDeleted.Value = false;

        _ = entity.IsDeleted;
        _ = entity.IsDeleted;
        _ = entity.IsDeleted;

        Assert.Equal(3, stub.IsDeleted.GetCount);
    }

    [Fact]
    public void IsModified_CanBeConfigured()
    {
        var stub = new EntityBaseStub();
        IEntityBase entity = stub;

        stub.IsModified.Value = true;

        Assert.True(entity.IsModified);
    }

    [Fact]
    public void IsSelfModified_CanBeConfigured()
    {
        var stub = new EntityBaseStub();
        IEntityBase entity = stub;

        stub.IsSelfModified.Value = true;

        Assert.True(entity.IsSelfModified);
    }

    [Fact]
    public void IsChild_CanBeConfigured()
    {
        var stub = new EntityBaseStub();
        IEntityBase entity = stub;

        stub.IsChild.Value = true;

        Assert.True(entity.IsChild);
    }

    [Fact]
    public void IsSavable_CanBeConfigured()
    {
        var stub = new EntityBaseStub();
        IEntityBase entity = stub;

        stub.IsSavable.Value = true;

        Assert.True(entity.IsSavable);
    }

    [Fact]
    public void Indexer_TracksAccessWithKey()
    {
        var stub = new EntityBaseStub();
        IEntityBase entity = stub;

        stub.Indexer.OnGet = (ko, propertyName) => null!;

        _ = entity["FirstName"];
        _ = entity["LastName"];

        Assert.Equal(2, stub.Indexer.GetCount);
        Assert.Equal("LastName", stub.Indexer.LastGetKey);
    }

    [Fact]
    public void Delete_TracksCall()
    {
        var stub = new EntityBaseStub();
        var tracking = stub.Delete.OnCall(ko => { });
        IEntityBase entity = stub;

        entity.Delete();

        Assert.True(tracking.WasCalled);
        Assert.Equal(1, tracking.CallCount);
    }

    [Fact]
    public void UnDelete_TracksCall()
    {
        var stub = new EntityBaseStub();
        var tracking = stub.UnDelete.OnCall(ko => { });
        IEntityBase entity = stub;

        entity.UnDelete();

        Assert.True(tracking.WasCalled);
        Assert.Equal(1, tracking.CallCount);
    }

    [Fact]
    public async Task Save_ReturnsConfiguredValue()
    {
        var stub = new EntityBaseStub();
        IEntityBase entity = stub;

        var tracking = stub.Save.OnCall((ko) => Task.FromResult<IEntityBase>(ko));

        var result = await entity.Save();

        Assert.Same(entity, result);
        Assert.True(tracking.WasCalled);
        Assert.Equal(1, tracking.CallCount);
    }

    [Fact]
    public async Task Save_CanReturnDifferentEntity()
    {
        var stub = new EntityBaseStub();
        var savedStub = new EntityBaseStub();
        IEntityBase entity = stub;

        stub.Save.OnCall((ko) => Task.FromResult<IEntityBase>(savedStub));

        var result = await entity.Save();

        Assert.Same(savedStub, result);
    }

    [Fact]
    public void ModifiedProperties_CanBeConfigured()
    {
        var stub = new EntityBaseStub();
        IEntityBase entity = stub;

        var modified = new[] { "FirstName", "LastName" };
        // ModifiedProperties is on IEntityBase interceptor, not IEntityMetaProperties
        stub.ModifiedProperties.OnGet = (ko) => modified;

        Assert.Equal(modified, entity.ModifiedProperties);
    }

    [Fact]
    public void Parent_CanBeConfigured()
    {
        var stub = new EntityBaseStub();
        var parentStub = new EntityBaseStub();
        IEntityBase entity = stub;

        // Parent is now on IValidateBase (IBase was removed in Neatoo 10.6)
        stub.Parent.OnGet = (ko) => parentStub;

        Assert.Same(parentStub, entity.Parent);
    }

    [Fact]
    public async Task WaitForTasks_TracksCall()
    {
        var stub = new EntityBaseStub();
        // WaitForTasks is overloaded - use OnCall with no-arg lambda for the parameterless overload
        var tracking = stub.WaitForTasks.OnCall((ko) => Task.CompletedTask);
        IEntityBase entity = stub;

        await entity.WaitForTasks();

        Assert.True(tracking.WasCalled);
    }

    [Fact]
    public void IsBusy_CanBeConfigured()
    {
        var stub = new EntityBaseStub();
        IEntityBase entity = stub;

        // IsBusy is now on IValidateMetaProperties
        stub.IsBusy.Value = true;

        Assert.True(entity.IsBusy);
    }

    [Fact]
    public void Reset_ClearsAllTracking()
    {
        var stub = new EntityBaseStub();
        var deleteTracking = stub.Delete.OnCall(ko => { });
        IEntityBase entity = stub;

        stub.IsNew.Value = true;

        // Perform some operations
        _ = entity.IsNew;
        entity.Delete();

        // Reset tracking
        stub.IsNew.Reset();
        stub.Delete.Reset();

        // Verify reset
        Assert.Equal(0, stub.IsNew.GetCount);
        Assert.False(deleteTracking.WasCalled);
        Assert.Equal(0, deleteTracking.CallCount);
    }
}

/// <summary>
/// Tests for standalone IValidateBase stubs.
/// </summary>
public class ValidateBaseStandaloneTests
{
    [Fact]
    public void CanBeStubbed()
    {
        var stub = new ValidateBaseStub();
        IValidateBase validate = stub;

        Assert.NotNull(validate);
    }

    [Fact]
    public void IsValid_CanBeConfigured()
    {
        var stub = new ValidateBaseStub();
        IValidateBase validate = stub;

        stub.IsValid.Value = true;

        Assert.True(validate.IsValid);
    }

    [Fact]
    public void IsSelfValid_CanBeConfigured()
    {
        var stub = new ValidateBaseStub();
        IValidateBase validate = stub;

        stub.IsSelfValid.Value = false;

        Assert.False(validate.IsSelfValid);
    }

    [Fact]
    public void IsPaused_CanBeConfigured()
    {
        var stub = new ValidateBaseStub();
        IValidateBase validate = stub;

        stub.IsPaused.Value = true;

        Assert.True(validate.IsPaused);
    }

    [Fact]
    public void PropertyMessages_CanBeConfigured()
    {
        var stub = new ValidateBaseStub();
        IValidateBase validate = stub;

        var messages = new List<IPropertyMessage>();
        stub.PropertyMessages.OnGet = (ko) => messages;

        Assert.Same(messages, validate.PropertyMessages);
    }

    [Fact]
    public async Task RunRules_WithPropertyName_TracksCall()
    {
        var stub = new ValidateBaseStub();
        // RunRules is overloaded - use explicit delegate type to disambiguate
        var tracking = stub.RunRules.OnCall((ValidateBaseStub.RunRulesInterceptor.RunRulesDelegate_String_Threading_CancellationToken_Threading_Tasks_Task)((ko, propertyName, token) => Task.CompletedTask));
        IValidateBase validate = stub;

        await validate.RunRules("FirstName", null);

        Assert.True(tracking.WasCalled);
        Assert.Equal(1, tracking.CallCount);
    }

    [Fact]
    public async Task RunRules_WithFlag_TracksCall()
    {
        var stub = new ValidateBaseStub();
        // RunRules is overloaded - use explicit delegate type to disambiguate
        var tracking = stub.RunRules.OnCall((ValidateBaseStub.RunRulesInterceptor.RunRulesDelegate_Neatoo_RunRulesFlag_Threading_CancellationToken_Threading_Tasks_Task)((ko, flag, token) => Task.CompletedTask));
        IValidateBase validate = stub;

        await validate.RunRules(RunRulesFlag.All, null);

        Assert.True(tracking.WasCalled);
    }

    [Fact]
    public void ClearAllMessages_TracksCall()
    {
        var stub = new ValidateBaseStub();
        var tracking = stub.ClearAllMessages.OnCall(ko => { });
        IValidateBase validate = stub;

        validate.ClearAllMessages();

        Assert.True(tracking.WasCalled);
    }

    [Fact]
    public void ClearSelfMessages_TracksCall()
    {
        var stub = new ValidateBaseStub();
        var tracking = stub.ClearSelfMessages.OnCall(ko => { });
        IValidateBase validate = stub;

        validate.ClearSelfMessages();

        Assert.True(tracking.WasCalled);
    }

    [Fact]
    public void GetProperty_TracksCall()
    {
        var stub = new ValidateBaseStub();
        IValidateBase validate = stub;

        var tracking = stub.GetProperty.OnCall((ko, name) => null!);

        _ = validate.GetProperty("Age");

        Assert.True(tracking.WasCalled);
        Assert.Equal("Age", tracking.LastArg);
    }

    [Fact]
    public void Indexer_TracksAccess()
    {
        var stub = new ValidateBaseStub();
        IValidateBase validate = stub;

        stub.Indexer.OnGet = (ko, name) => null!;

        _ = validate["Email"];

        Assert.Equal(1, stub.Indexer.GetCount);
        Assert.Equal("Email", stub.Indexer.LastGetKey);
    }
}

#endregion

#region Inline Stub Tests

/// <summary>
/// Tests for inline IValidateBase stubs using [KnockOff&lt;T&gt;] attribute.
/// </summary>
[KnockOff<IValidateBase>]
public partial class InlineValidateBaseTests
{
    [Fact]
    public void InlineStub_CanBeInstantiated()
    {
        var stub = new Stubs.IValidateBase();
        IValidateBase validate = stub;

        Assert.NotNull(validate);
    }

    [Fact]
    public void InlineStub_IsValid_CanBeConfiguredViaValue()
    {
        var stub = new Stubs.IValidateBase();
        IValidateBase validate = stub;

        stub.IsValid.Value = true;

        Assert.True(validate.IsValid);
        Assert.Equal(1, stub.IsValid.GetCount);
    }

    [Fact]
    public void InlineStub_IsValid_CanBeConfiguredViaOnGet()
    {
        var stub = new Stubs.IValidateBase();
        IValidateBase validate = stub;

        stub.IsValid.OnGet = (s) => true;

        Assert.True(validate.IsValid);
    }

    [Fact]
    public void InlineStub_IsPaused_CanBeConfigured()
    {
        var stub = new Stubs.IValidateBase();
        IValidateBase validate = stub;

        stub.IsPaused.Value = true;

        Assert.True(validate.IsPaused);
    }

    [Fact]
    public void InlineStub_Parent_CanBeConfigured()
    {
        var stub = new Stubs.IValidateBase();
        var parentStub = new Stubs.IValidateBase();
        IValidateBase validate = stub;

        stub.Parent.Value = parentStub;

        Assert.Same(parentStub, validate.Parent);
    }

    [Fact]
    public void InlineStub_Indexer_TracksAccess()
    {
        var stub = new Stubs.IValidateBase();
        IValidateBase validate = stub;

        stub.Indexer.OnGet = (s, name) => null!;

        _ = validate["PropertyName"];

        Assert.Equal(1, stub.Indexer.GetCount);
        Assert.Equal("PropertyName", stub.Indexer.LastGetKey);
    }

    [Fact]
    public void InlineStub_GetProperty_TracksCall()
    {
        var stub = new Stubs.IValidateBase();
        IValidateBase validate = stub;

        stub.GetProperty.OnCall = (s, name) => null!;

        _ = validate.GetProperty("TestProp");

        Assert.True(stub.GetProperty.WasCalled);
        Assert.Equal("TestProp", stub.GetProperty.LastCallArg);
    }

    [Fact]
    public void InlineStub_TryGetProperty_TracksCall()
    {
        var stub = new Stubs.IValidateBase();
        IValidateBase validate = stub;

        stub.TryGetProperty.OnCall = (s, name) => true;

        var result = validate.TryGetProperty("TestProp", out _);

        Assert.True(stub.TryGetProperty.WasCalled);
        Assert.Equal("TestProp", stub.TryGetProperty.LastCallArg);
    }

    [Fact]
    public async Task InlineStub_RunRules_WithPropertyName_TracksCall()
    {
        var stub = new Stubs.IValidateBase();
        IValidateBase validate = stub;

        await validate.RunRules("PropertyName", null);

        Assert.True(stub.RunRules.WasCalled);
        Assert.Equal(1, stub.RunRules.CallCount);
    }

    [Fact]
    public async Task InlineStub_RunRules_WithFlag_TracksCall()
    {
        var stub = new Stubs.IValidateBase();
        IValidateBase validate = stub;

        await validate.RunRules(RunRulesFlag.All, null);

        Assert.True(stub.RunRules.WasCalled);
    }

    [Fact]
    public async Task InlineStub_RunRules_CanExecuteCallback()
    {
        var stub = new Stubs.IValidateBase();
        IValidateBase validate = stub;
        var callbackExecuted = false;

        stub.RunRules.OnCall = (s, prop, token, flag) =>
        {
            callbackExecuted = true;
            return Task.CompletedTask;
        };

        await validate.RunRules("Property", null);

        Assert.True(callbackExecuted);
    }

    [Fact]
    public void InlineStub_ClearAllMessages_TracksCall()
    {
        var stub = new Stubs.IValidateBase();
        IValidateBase validate = stub;

        validate.ClearAllMessages();

        Assert.True(stub.ClearAllMessages.WasCalled);
    }

    [Fact]
    public void InlineStub_ClearSelfMessages_TracksCall()
    {
        var stub = new Stubs.IValidateBase();
        IValidateBase validate = stub;

        validate.ClearSelfMessages();

        Assert.True(stub.ClearSelfMessages.WasCalled);
    }

    [Fact]
    public async Task InlineStub_WaitForTasks_TracksCall()
    {
        var stub = new Stubs.IValidateBase();
        IValidateBase validate = stub;

        await validate.WaitForTasks();

        Assert.True(stub.WaitForTasks.WasCalled);
    }

    [Fact]
    public void InlineStub_IsBusy_CanBeConfigured()
    {
        var stub = new Stubs.IValidateBase();
        IValidateBase validate = stub;

        stub.IsBusy.Value = true;

        Assert.True(validate.IsBusy);
    }

    [Fact]
    public void InlineStub_PropertyChanged_EventCanBeSubscribed()
    {
        var stub = new Stubs.IValidateBase();
        IValidateBase validate = stub;

        validate.PropertyChanged += (s, e) => { };

        Assert.Equal(1, stub.PropertyChangedInterceptor.AddCount);
    }

    [Fact]
    public void InlineStub_PropertyChanged_EventCanBeUnsubscribed()
    {
        var stub = new Stubs.IValidateBase();
        IValidateBase validate = stub;

        PropertyChangedEventHandler handler = (s, e) => { };
        validate.PropertyChanged += handler;
        validate.PropertyChanged -= handler;

        Assert.Equal(1, stub.PropertyChangedInterceptor.AddCount);
        Assert.Equal(1, stub.PropertyChangedInterceptor.RemoveCount);
    }

    [Fact]
    public void InlineStub_NeatooPropertyChanged_EventCanBeSubscribed()
    {
        var stub = new Stubs.IValidateBase();
        IValidateBase validate = stub;

        // NeatooPropertyChanged delegate takes only 1 arg (NeatooPropertyChangedEventArgs)
        validate.NeatooPropertyChanged += (args) => Task.CompletedTask;

        Assert.Equal(1, stub.NeatooPropertyChangedInterceptor.AddCount);
    }
}

// NOTE: IBase was removed in Neatoo 10.6.0. IValidateBase now contains Parent property.
// IEntityBase inline stubs have a known issue with duplicate indexer members
// from inherited interfaces (IEntityBase.this[string] and IValidateBase.this[string]).
// Use standalone stubs for IEntityBase until this is resolved.
// See EntityBaseStandaloneTests for IEntityBase testing.

/// <summary>
/// Tests for inline IRuleManager stubs using [KnockOff&lt;T&gt;] attribute.
/// IRuleManager has mixed overloads: RunRule(IRule, token) and RunRule&lt;T&gt;(token).
/// </summary>
[KnockOff<Neatoo.Rules.IRuleManager>]
public partial class InlineRuleManagerTests
{
    [Fact]
    public void InlineStub_CanBeInstantiated()
    {
        var stub = new Stubs.IRuleManager();
        Neatoo.Rules.IRuleManager ruleManager = stub;

        Assert.NotNull(ruleManager);
    }

    [Fact]
    public async Task InlineStub_RunRule_NonGeneric_TracksCall()
    {
        var stub = new Stubs.IRuleManager();
        Neatoo.Rules.IRuleManager ruleManager = stub;

        // Non-generic RunRule should use the RunRule interceptor (not RunRuleGeneric)
        await ruleManager.RunRule(null!, null);

        Assert.True(stub.RunRule.WasCalled);
        Assert.Equal(1, stub.RunRule.CallCount);
    }

    [Fact]
    public async Task InlineStub_RunRule_Generic_TracksCall()
    {
        var stub = new Stubs.IRuleManager();
        Neatoo.Rules.IRuleManager ruleManager = stub;

        // Generic RunRule<T> should use the RunRuleGeneric interceptor with Of<T>()
        await ruleManager.RunRule<TestRule>(null);

        Assert.True(stub.RunRuleGeneric.WasCalled);
        Assert.True(stub.RunRuleGeneric.Of<TestRule>().WasCalled);
        Assert.Equal(1, stub.RunRuleGeneric.Of<TestRule>().CallCount);
    }

    [Fact]
    public async Task InlineStub_RunRules_TracksCall()
    {
        var stub = new Stubs.IRuleManager();
        Neatoo.Rules.IRuleManager ruleManager = stub;

        await ruleManager.RunRules("PropertyName", null);

        Assert.True(stub.RunRules.WasCalled);
        Assert.Equal(1, stub.RunRules.CallCount);
    }

    [Fact]
    public void InlineStub_AddRule_Generic_TracksCall()
    {
        var stub = new Stubs.IRuleManager();
        Neatoo.Rules.IRuleManager ruleManager = stub;

        ruleManager.AddRule<IValidateBase>(null!);

        Assert.True(stub.AddRule.WasCalled);
        Assert.True(stub.AddRule.Of<IValidateBase>().WasCalled);
    }

    [Fact]
    public void InlineStub_Rules_Property_CanBeConfigured()
    {
        var stub = new Stubs.IRuleManager();
        Neatoo.Rules.IRuleManager ruleManager = stub;

        var rules = new List<Neatoo.Rules.IRule>();
        stub.Rules.Value = rules;

        Assert.Same(rules, ruleManager.Rules);
        Assert.Equal(1, stub.Rules.GetCount);
    }

    // Use KnockOff to stub IRule for the generic method test
    [KnockOff]
    private partial class TestRule : Neatoo.Rules.IRule
    {
    }
}

#endregion

#region Inline Delegate Stub Tests

/// <summary>
/// Tests for inline delegate stubs (NeatooPropertyChanged).
/// </summary>
[KnockOff<NeatooPropertyChanged>]
public partial class InlineDelegateTests
{
    [Fact]
    public void DelegateStub_CanBeInstantiated()
    {
        var stub = new Stubs.NeatooPropertyChanged();

        Assert.NotNull(stub);
    }

    [Fact]
    public void DelegateStub_CanBeConvertedToDelegate()
    {
        var stub = new Stubs.NeatooPropertyChanged();
        NeatooPropertyChanged del = stub;

        Assert.NotNull(del);
    }

    [Fact]
    public async Task DelegateStub_TracksInvocation()
    {
        var stub = new Stubs.NeatooPropertyChanged();
        NeatooPropertyChanged del = stub;

        // Must set OnCall to return Task.CompletedTask, otherwise await on null
        // TODO: Generator should return Task.CompletedTask for async delegates by default
        stub.Interceptor.OnCall = (s, args) => Task.CompletedTask;

        // NeatooPropertyChanged takes only 1 arg: NeatooPropertyChangedEventArgs
        // EventArgs constructor: (propertyName, source)
        await del(new NeatooPropertyChangedEventArgs("TestProperty", this));

        Assert.True(stub.Interceptor.WasCalled);
        Assert.Equal(1, stub.Interceptor.CallCount);
    }

    [Fact]
    public async Task DelegateStub_TracksMultipleInvocations()
    {
        var stub = new Stubs.NeatooPropertyChanged();
        NeatooPropertyChanged del = stub;

        // Must set OnCall to return Task.CompletedTask
        stub.Interceptor.OnCall = (s, args) => Task.CompletedTask;

        await del(new NeatooPropertyChangedEventArgs("Prop1", this));
        await del(new NeatooPropertyChangedEventArgs("Prop2", this));
        await del(new NeatooPropertyChangedEventArgs("Prop3", this));

        Assert.Equal(3, stub.Interceptor.CallCount);
    }

    [Fact]
    public async Task DelegateStub_CanExecuteCallback()
    {
        var stub = new Stubs.NeatooPropertyChanged();
        NeatooPropertyChanged del = stub;
        var callbackExecuted = false;
        string? capturedPropertyName = null;

        // OnCall takes (stub, args) - only 2 parameters
        stub.Interceptor.OnCall = (s, args) =>
        {
            callbackExecuted = true;
            capturedPropertyName = args.PropertyName;
            return Task.CompletedTask;
        };

        await del(new NeatooPropertyChangedEventArgs("CapturedProp", this));

        Assert.True(callbackExecuted);
        Assert.Equal("CapturedProp", capturedPropertyName);
    }

    [Fact]
    public async Task DelegateStub_Reset_ClearsTracking()
    {
        var stub = new Stubs.NeatooPropertyChanged();
        NeatooPropertyChanged del = stub;

        // Must set OnCall to return Task.CompletedTask
        stub.Interceptor.OnCall = (s, args) => Task.CompletedTask;

        await del(new NeatooPropertyChangedEventArgs("Prop", this));

        stub.Interceptor.Reset();

        Assert.False(stub.Interceptor.WasCalled);
        Assert.Equal(0, stub.Interceptor.CallCount);
    }
}

#endregion

#region Inline Multiple Interface Tests

/// <summary>
/// Inline stub tests for multiple interfaces.
/// NOTE: IEntityBase inline excluded due to duplicate indexer issue.
/// NOTE: IBase was removed in Neatoo 10.6.0.
/// </summary>
[KnockOff<IValidateBase>]
[KnockOff<INotifyNeatooPropertyChanged>]
public partial class MultipleInlineTests
{
    [Fact]
    public void MultipleInline_ValidateBase_Works()
    {
        var stub = new Stubs.IValidateBase();
        IValidateBase validate = stub;

        stub.IsValid.Value = true;

        Assert.True(validate.IsValid);
    }

    [Fact]
    public void MultipleInline_NotifyNeatooPropertyChanged_Works()
    {
        var stub = new Stubs.INotifyNeatooPropertyChanged();
        INotifyNeatooPropertyChanged notify = stub;

        Assert.NotNull(notify);
    }
}

#endregion

#region Nested Class Tests

/// <summary>
/// Tests with nested class stubs.
/// </summary>
public partial class NestedClassTests
{
    [KnockOff]
    public partial class NestedEntityStub : IEntityBase
    {
    }

    [KnockOff]
    public partial class NestedValidateStub : IValidateBase
    {
    }

    [Fact]
    public void NestedStub_Entity_Works()
    {
        var stub = new NestedEntityStub();
        IEntityBase entity = stub;

        stub.IsNew.Value = true;

        Assert.True(entity.IsNew);
    }

    [Fact]
    public void NestedStub_Validate_Works()
    {
        var stub = new NestedValidateStub();
        IValidateBase validate = stub;

        stub.IsValid.Value = true;

        Assert.True(validate.IsValid);
    }
}

/// <summary>
/// Inline stubs in nested class.
/// </summary>
public partial class NestedInlineTests
{
    [KnockOff<IValidateBase>]
    public partial class InlineValidateContainer
    {
        [Fact]
        public void NestedInline_Works()
        {
            var stub = new Stubs.IValidateBase();
            IValidateBase validate = stub;

            stub.IsValid.Value = true;

            Assert.True(validate.IsValid);
        }
    }

    /// <summary>
    /// Test for IEntityBase inline stub - verifies duplicate indexer fix.
    /// IEntityBase inherits from IValidateBase and both have this[string] with different return types.
    /// </summary>
    [KnockOff<IEntityBase>]
    public partial class InlineEntityContainer
    {
        [Fact]
        public void InlineEntityBase_Works()
        {
            var stub = new Stubs.IEntityBase();
            IEntityBase entity = stub;

            stub.IsValid.Value = true;

            Assert.True(entity.IsValid);
        }
    }
}

#endregion
