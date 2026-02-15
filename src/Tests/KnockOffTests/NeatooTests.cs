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

        stub.IsNew.Get(() => true);

        Assert.True(entity.IsNew);
        stub.IsNew.VerifyGet(Called.Once);
    }

    [Fact]
    public void IsDeleted_TracksMultipleAccesses()
    {
        var stub = new EntityBaseStub();
        IEntityBase entity = stub;

        stub.IsDeleted.Get(false);

        _ = entity.IsDeleted;
        _ = entity.IsDeleted;
        _ = entity.IsDeleted;

        stub.IsDeleted.VerifyGet(Called.Exactly(3));
    }

    [Fact]
    public void IsModified_CanBeConfigured()
    {
        var stub = new EntityBaseStub();
        IEntityBase entity = stub;

        stub.IsModified.Get(true);

        Assert.True(entity.IsModified);
    }

    [Fact]
    public void IsSelfModified_CanBeConfigured()
    {
        var stub = new EntityBaseStub();
        IEntityBase entity = stub;

        stub.IsSelfModified.Get(true);

        Assert.True(entity.IsSelfModified);
    }

    [Fact]
    public void IsChild_CanBeConfigured()
    {
        var stub = new EntityBaseStub();
        IEntityBase entity = stub;

        stub.IsChild.Get(true);

        Assert.True(entity.IsChild);
    }

    [Fact]
    public void IsSavable_CanBeConfigured()
    {
        var stub = new EntityBaseStub();
        IEntityBase entity = stub;

        stub.IsSavable.Get(true);

        Assert.True(entity.IsSavable);
    }

    [Fact]
    public void Indexer_TracksAccessWithKey()
    {
        var stub = new EntityBaseStub();
        IEntityBase entity = stub;

        stub.Indexer.Get((string propertyName) => null!);

        _ = entity["FirstName"];
        _ = entity["LastName"];

        stub.Indexer.VerifyGet(Called.Exactly(2));
        Assert.Equal("LastName", stub.Indexer.LastGetKey);
    }

    [Fact]
    public void Delete_TracksCall()
    {
        var stub = new EntityBaseStub();
        var tracking = stub.Delete.Call(() => { });
        IEntityBase entity = stub;

        entity.Delete();

        tracking.Verify();
        tracking.Verify(Called.Once);
    }

    [Fact]
    public void UnDelete_TracksCall()
    {
        var stub = new EntityBaseStub();
        var tracking = stub.UnDelete.Call(() => { });
        IEntityBase entity = stub;

        entity.UnDelete();

        tracking.Verify();
        tracking.Verify(Called.Once);
    }

    [Fact]
    public async Task Save_ReturnsConfiguredValue()
    {
        var stub = new EntityBaseStub();
        IEntityBase entity = stub;

        var tracking = stub.Save.Return(() => Task.FromResult<IEntityBase>(stub));

        var result = await entity.Save();

        Assert.Same(entity, result);
        tracking.Verify();
        tracking.Verify(Called.Once);
    }

    [Fact]
    public async Task Save_CanReturnDifferentEntity()
    {
        var stub = new EntityBaseStub();
        var savedStub = new EntityBaseStub();
        IEntityBase entity = stub;

        stub.Save.Return(() => Task.FromResult<IEntityBase>(savedStub));

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
        stub.ModifiedProperties.Get(() => modified);

        Assert.Equal(modified, entity.ModifiedProperties);
    }

    [Fact]
    public void Parent_CanBeConfigured()
    {
        var stub = new EntityBaseStub();
        var parentStub = new EntityBaseStub();
        IEntityBase entity = stub;

        // Parent is now on IValidateBase (IBase was removed in Neatoo 10.6)
        stub.Parent.Get(() => parentStub);

        Assert.Same(parentStub, entity.Parent);
    }

    [Fact]
    public async Task WaitForTasks_TracksCall()
    {
        var stub = new EntityBaseStub();
        // WaitForTasks is overloaded - use OnCall with no-arg lambda for the parameterless overload
        var tracking = stub.WaitForTasks.Call(() => Task.CompletedTask);
        IEntityBase entity = stub;

        await entity.WaitForTasks();

        tracking.Verify();
    }

    [Fact]
    public void IsBusy_CanBeConfigured()
    {
        var stub = new EntityBaseStub();
        IEntityBase entity = stub;

        // IsBusy is now on IValidateMetaProperties
        stub.IsBusy.Get(true);

        Assert.True(entity.IsBusy);
    }

    [Fact]
    public void Reset_ClearsAllTracking()
    {
        var stub = new EntityBaseStub();
        var deleteTracking = stub.Delete.Call(() => { });
        IEntityBase entity = stub;

        stub.IsNew.Get(true);

        // Perform some operations
        _ = entity.IsNew;
        entity.Delete();

        // Reset tracking
        stub.IsNew.Reset();
        stub.Delete.Reset();

        // Verify reset
        stub.IsNew.VerifyGet(Called.Never);
        deleteTracking.Verify(Called.Never);
        deleteTracking.Verify(Called.Never);
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

        stub.IsValid.Get(true);

        Assert.True(validate.IsValid);
    }

    [Fact]
    public void IsSelfValid_CanBeConfigured()
    {
        var stub = new ValidateBaseStub();
        IValidateBase validate = stub;

        stub.IsSelfValid.Get(false);

        Assert.False(validate.IsSelfValid);
    }

    [Fact]
    public void IsPaused_CanBeConfigured()
    {
        var stub = new ValidateBaseStub();
        IValidateBase validate = stub;

        stub.IsPaused.Get(true);

        Assert.True(validate.IsPaused);
    }

    [Fact]
    public void PropertyMessages_CanBeConfigured()
    {
        var stub = new ValidateBaseStub();
        IValidateBase validate = stub;

        var messages = new List<IPropertyMessage>();
        stub.PropertyMessages.Get(() => messages);

        Assert.Same(messages, validate.PropertyMessages);
    }

    [Fact]
    public async Task RunRules_WithPropertyName_TracksCall()
    {
        var stub = new ValidateBaseStub();
        // RunRules is overloaded - disambiguate via Func type
        var tracking = stub.RunRules.Call((Func<string, CancellationToken?, Task>)((propertyName, token) => Task.CompletedTask));
        IValidateBase validate = stub;

        await validate.RunRules("FirstName", null);

        tracking.Verify();
        tracking.Verify(Called.Once);
    }

    [Fact]
    public async Task RunRules_WithFlag_TracksCall()
    {
        var stub = new ValidateBaseStub();
        // RunRules is overloaded - disambiguate via Func type
        var tracking = stub.RunRules.Call((Func<RunRulesFlag, CancellationToken?, Task>)((flag, token) => Task.CompletedTask));
        IValidateBase validate = stub;

        await validate.RunRules(RunRulesFlag.All, null);

        tracking.Verify();
    }

    [Fact]
    public void ClearAllMessages_TracksCall()
    {
        var stub = new ValidateBaseStub();
        var tracking = stub.ClearAllMessages.Call(() => { });
        IValidateBase validate = stub;

        validate.ClearAllMessages();

        tracking.Verify();
    }

    [Fact]
    public void ClearSelfMessages_TracksCall()
    {
        var stub = new ValidateBaseStub();
        var tracking = stub.ClearSelfMessages.Call(() => { });
        IValidateBase validate = stub;

        validate.ClearSelfMessages();

        tracking.Verify();
    }

    [Fact]
    public void GetProperty_TracksCall()
    {
        var stub = new ValidateBaseStub();
        IValidateBase validate = stub;

        var tracking = stub.GetProperty.Return((name) => null!);

        _ = validate.GetProperty("Age");

        tracking.Verify();
        Assert.Equal("Age", tracking.LastArg);
    }

    [Fact]
    public void Indexer_TracksAccess()
    {
        var stub = new ValidateBaseStub();
        IValidateBase validate = stub;

        stub.Indexer.Get((name) => null!);

        _ = validate["Email"];

        stub.Indexer.VerifyGet(Called.Once);
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

        stub.IsValid.Get(true);

        Assert.True(validate.IsValid);
        stub.IsValid.VerifyGet(Called.Once);
    }

    [Fact]
    public void InlineStub_IsValid_CanBeConfiguredViaOnGet()
    {
        var stub = new Stubs.IValidateBase();
        IValidateBase validate = stub;

        stub.IsValid.Get(() => true);

        Assert.True(validate.IsValid);
    }

    [Fact]
    public void InlineStub_IsPaused_CanBeConfigured()
    {
        var stub = new Stubs.IValidateBase();
        IValidateBase validate = stub;

        stub.IsPaused.Get(true);

        Assert.True(validate.IsPaused);
    }

    [Fact]
    public void InlineStub_Parent_CanBeConfigured()
    {
        var stub = new Stubs.IValidateBase();
        var parentStub = new Stubs.IValidateBase();
        IValidateBase validate = stub;

        stub.Parent.Get(parentStub);

        Assert.Same(parentStub, validate.Parent);
    }

    [Fact]
    public void InlineStub_Indexer_TracksAccess()
    {
        var stub = new Stubs.IValidateBase();
        IValidateBase validate = stub;

        stub.Indexer.Get((name) => null!);

        _ = validate["PropertyName"];

        stub.Indexer.VerifyGet(Called.Once);
        Assert.Equal("PropertyName", stub.Indexer.LastGetKey);
    }

    [Fact]
    public void InlineStub_GetProperty_TracksCall()
    {
        var stub = new Stubs.IValidateBase();
        IValidateBase validate = stub;

        stub.GetProperty.Return((name) => null!);

        _ = validate.GetProperty("TestProp");

        stub.GetProperty.Verify();
        Assert.Equal("TestProp", stub.GetProperty.LastArg);
    }

    [Fact]
    public void InlineStub_TryGetProperty_TracksCall()
    {
        var stub = new Stubs.IValidateBase();
        IValidateBase validate = stub;

        stub.TryGetProperty.Return((InlineValidateBaseTests.Stubs.IValidateBase_TryGetPropertyInterceptor.TryGetPropertyDelegate)((string name, out IValidateProperty prop) => { prop = default!; return true; }));

        var result = validate.TryGetProperty("TestProp", out _);

        stub.TryGetProperty.Verify();
        Assert.Equal("TestProp", stub.TryGetProperty.LastArg);
    }

    [Fact]
    public async Task InlineStub_RunRules_WithPropertyName_TracksCall()
    {
        var stub = new Stubs.IValidateBase();
        IValidateBase validate = stub;

        await validate.RunRules("PropertyName", null);

        stub.RunRules.Verify();
        stub.RunRules.Verify(Called.Once);
    }

    [Fact]
    public async Task InlineStub_RunRules_WithFlag_TracksCall()
    {
        var stub = new Stubs.IValidateBase();
        IValidateBase validate = stub;

        await validate.RunRules(RunRulesFlag.All, null);

        stub.RunRules.Verify();
    }

    [Fact]
    public async Task InlineStub_RunRules_CanExecuteCallback()
    {
        var stub = new Stubs.IValidateBase();
        IValidateBase validate = stub;
        var callbackExecuted = false;

        stub.RunRules.Call((Func<string, CancellationToken?, Task>)((prop, token) =>
        {
            callbackExecuted = true;
            return Task.CompletedTask;
        }));

        await validate.RunRules("Property", null);

        Assert.True(callbackExecuted);
    }

    [Fact]
    public void InlineStub_ClearAllMessages_TracksCall()
    {
        var stub = new Stubs.IValidateBase();
        IValidateBase validate = stub;

        validate.ClearAllMessages();

        stub.ClearAllMessages.Verify();
    }

    [Fact]
    public void InlineStub_ClearSelfMessages_TracksCall()
    {
        var stub = new Stubs.IValidateBase();
        IValidateBase validate = stub;

        validate.ClearSelfMessages();

        stub.ClearSelfMessages.Verify();
    }

    [Fact]
    public async Task InlineStub_WaitForTasks_TracksCall()
    {
        var stub = new Stubs.IValidateBase();
        IValidateBase validate = stub;

        await validate.WaitForTasks();

        stub.WaitForTasks.Verify();
    }

    [Fact]
    public void InlineStub_IsBusy_CanBeConfigured()
    {
        var stub = new Stubs.IValidateBase();
        IValidateBase validate = stub;

        stub.IsBusy.Get(true);

        Assert.True(validate.IsBusy);
    }

    [Fact]
    public void InlineStub_PropertyChanged_EventCanBeSubscribed()
    {
        var stub = new Stubs.IValidateBase();
        IValidateBase validate = stub;

        validate.PropertyChanged += (s, e) => { };

        stub.PropertyChanged.VerifyAdd(Called.Once);
    }

    [Fact]
    public void InlineStub_PropertyChanged_EventCanBeUnsubscribed()
    {
        var stub = new Stubs.IValidateBase();
        IValidateBase validate = stub;

        PropertyChangedEventHandler handler = (s, e) => { };
        validate.PropertyChanged += handler;
        validate.PropertyChanged -= handler;

        stub.PropertyChanged.VerifyAdd(Called.Once);
        stub.PropertyChanged.VerifyRemove(Called.Once);
    }

    [Fact]
    public void InlineStub_NeatooPropertyChanged_EventCanBeSubscribed()
    {
        var stub = new Stubs.IValidateBase();
        IValidateBase validate = stub;

        // NeatooPropertyChanged delegate takes only 1 arg (NeatooPropertyChangedEventArgs)
        validate.NeatooPropertyChanged += (args) => Task.CompletedTask;

        stub.NeatooPropertyChanged.VerifyAdd(Called.Once);
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

        stub.RunRule.Verify();
        stub.RunRule.Verify(Called.Once);
    }

    [Fact]
    public async Task InlineStub_RunRule_Generic_TracksCall()
    {
        var stub = new Stubs.IRuleManager();
        Neatoo.Rules.IRuleManager ruleManager = stub;

        // Generic RunRule<T> should use the RunRuleGeneric interceptor with Of<T>()
        await ruleManager.RunRule<TestRule>(null);

        stub.RunRuleGeneric.Verify();
        stub.RunRuleGeneric.Of<TestRule>().Verify();
        stub.RunRuleGeneric.Of<TestRule>().Verify(Called.Once);
    }

    [Fact]
    public async Task InlineStub_RunRules_TracksCall()
    {
        var stub = new Stubs.IRuleManager();
        Neatoo.Rules.IRuleManager ruleManager = stub;

        await ruleManager.RunRules("PropertyName", null);

        stub.RunRules.Verify();
        stub.RunRules.Verify(Called.Once);
    }

    [Fact]
    public void InlineStub_AddRule_Generic_TracksCall()
    {
        var stub = new Stubs.IRuleManager();
        Neatoo.Rules.IRuleManager ruleManager = stub;

        ruleManager.AddRule<IValidateBase>(null!);

        stub.AddRule.Verify();
        stub.AddRule.Of<IValidateBase>().Verify();
    }

    [Fact]
    public void InlineStub_Rules_Property_CanBeConfigured()
    {
        var stub = new Stubs.IRuleManager();
        Neatoo.Rules.IRuleManager ruleManager = stub;

        var rules = new List<Neatoo.Rules.IRule>();
        stub.Rules.Get(rules);

        Assert.Same(rules, ruleManager.Rules);
        stub.Rules.VerifyGet(Called.Once);
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
        stub.Interceptor.Return((args) => Task.CompletedTask);

        // NeatooPropertyChanged takes only 1 arg: NeatooPropertyChangedEventArgs
        // EventArgs constructor: (propertyName, source)
        await del(new NeatooPropertyChangedEventArgs("TestProperty", this));

        stub.Interceptor.Verify();
        stub.Interceptor.Verify(Called.Once);
    }

    [Fact]
    public async Task DelegateStub_TracksMultipleInvocations()
    {
        var stub = new Stubs.NeatooPropertyChanged();
        NeatooPropertyChanged del = stub;

        // Must set OnCall to return Task.CompletedTask
        stub.Interceptor.Return((args) => Task.CompletedTask);

        await del(new NeatooPropertyChangedEventArgs("Prop1", this));
        await del(new NeatooPropertyChangedEventArgs("Prop2", this));
        await del(new NeatooPropertyChangedEventArgs("Prop3", this));

        stub.Interceptor.Verify(Called.Exactly(3));
    }

    [Fact]
    public async Task DelegateStub_CanExecuteCallback()
    {
        var stub = new Stubs.NeatooPropertyChanged();
        NeatooPropertyChanged del = stub;
        var callbackExecuted = false;
        string? capturedPropertyName = null;

        // OnCall takes (stub, args) - only 2 parameters
        stub.Interceptor.Return((args) =>
        {
            callbackExecuted = true;
            capturedPropertyName = args.PropertyName;
            return Task.CompletedTask;
        });

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
        stub.Interceptor.Return((args) => Task.CompletedTask);

        await del(new NeatooPropertyChangedEventArgs("Prop", this));

        stub.Interceptor.Reset();

        stub.Interceptor.Verify(Called.Never);
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

        stub.IsValid.Get(true);

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

        stub.IsNew.Get(true);

        Assert.True(entity.IsNew);
    }

    [Fact]
    public void NestedStub_Validate_Works()
    {
        var stub = new NestedValidateStub();
        IValidateBase validate = stub;

        stub.IsValid.Get(true);

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

            stub.IsValid.Get(true);

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

            stub.IsValid.Get(true);

            Assert.True(entity.IsValid);
        }
    }
}

#endregion
