using Neatoo;

namespace KnockOff.NeatooInterfaceTests.Properties;

/// <summary>
/// Tests for IEntityProperty - entity property interface with modification tracking.
/// This interface extends IValidateProperty.
/// </summary>
[KnockOff<IEntityProperty>]
public partial class IEntityPropertyTests
{
    [Fact]
    public void InlineStub_CanBeInstantiated()
    {
        var stub = new Stubs.IEntityProperty();
        Assert.NotNull(stub);
    }

    [Fact]
    public void InlineStub_ImplementsInterface()
    {
        var stub = new Stubs.IEntityProperty();
        IEntityProperty property = stub;
        Assert.NotNull(property);
    }

    [Fact]
    public void InlineStub_ImplementsIValidateProperty()
    {
        var stub = new Stubs.IEntityProperty();
        IValidateProperty baseProperty = stub;
        Assert.NotNull(baseProperty);
    }

    #region IEntityProperty Specific Property Tests

    [Fact]
    public void IsPaused_CanBeConfiguredForGet()
    {
        var stub = new Stubs.IEntityProperty();
        IEntityProperty property = stub;

        stub.IsPaused.Value = true;

        Assert.True(property.IsPaused);
        Assert.Equal(1, stub.IsPaused.GetCount);
    }

    [Fact]
    public void IsPaused_CanTrackSetter()
    {
        var stub = new Stubs.IEntityProperty();
        IEntityProperty property = stub;

        property.IsPaused = true;

        Assert.Equal(1, stub.IsPaused.SetCount);
        Assert.Equal(true, stub.IsPaused.LastSetValue);
    }

    [Fact]
    public void IsModified_CanBeConfigured()
    {
        var stub = new Stubs.IEntityProperty();
        IEntityProperty property = stub;

        stub.IsModified.Value = true;

        Assert.True(property.IsModified);
    }

    [Fact]
    public void IsSelfModified_CanBeConfigured()
    {
        var stub = new Stubs.IEntityProperty();
        IEntityProperty property = stub;

        stub.IsSelfModified.Value = true;

        Assert.True(property.IsSelfModified);
    }

    [Fact]
    public void DisplayName_CanBeConfigured()
    {
        var stub = new Stubs.IEntityProperty();
        IEntityProperty property = stub;

        stub.DisplayName.Value = "Customer Name";

        Assert.Equal("Customer Name", property.DisplayName);
    }

    #endregion

    #region IEntityProperty Specific Method Tests

    [Fact]
    public void MarkSelfUnmodified_TracksCall()
    {
        var stub = new Stubs.IEntityProperty();
        IEntityProperty property = stub;

        property.MarkSelfUnmodified();

        Assert.True(stub.MarkSelfUnmodified.WasCalled);
        Assert.Equal(1, stub.MarkSelfUnmodified.CallCount);
    }

    [Fact]
    public void ApplyPropertyInfo_TracksCall()
    {
        var stub = new Stubs.IEntityProperty();
        IEntityProperty property = stub;

        var propertyInfoStub = new PropertyInfoStub();
        property.ApplyPropertyInfo(propertyInfoStub);

        Assert.True(stub.ApplyPropertyInfo.WasCalled);
    }

    [Fact]
    public void ApplyPropertyInfo_CapturesArgument()
    {
        var stub = new Stubs.IEntityProperty();
        IEntityProperty property = stub;
        IPropertyInfo? capturedPropertyInfo = null;

        stub.ApplyPropertyInfo.OnCall = (ko, propInfo) =>
        {
            capturedPropertyInfo = propInfo;
        };

        var propertyInfoStub = new PropertyInfoStub();
        property.ApplyPropertyInfo(propertyInfoStub);

        Assert.Same(propertyInfoStub, capturedPropertyInfo);
    }

    #endregion

    #region Inherited IValidateProperty Property Tests

    [Fact]
    public void Name_FromBaseInterface_CanBeConfigured()
    {
        var stub = new Stubs.IEntityProperty();
        IEntityProperty property = stub;

        stub.Name.Value = "EntityPropertyName";

        Assert.Equal("EntityPropertyName", property.Name);
    }

    [Fact]
    public void Value_FromBaseInterface_CanBeConfigured()
    {
        var stub = new Stubs.IEntityProperty();
        IEntityProperty property = stub;

        stub.Value.Value = "EntityValue";

        Assert.Equal("EntityValue", property.Value);
    }

    [Fact]
    public void IsBusy_FromBaseInterface_CanBeConfigured()
    {
        var stub = new Stubs.IEntityProperty();
        IEntityProperty property = stub;

        stub.IsBusy.Value = true;

        Assert.True(property.IsBusy);
    }

    [Fact]
    public void IsValid_FromBaseInterface_CanBeConfigured()
    {
        var stub = new Stubs.IEntityProperty();
        IEntityProperty property = stub;

        stub.IsValid.Value = false;

        Assert.False(property.IsValid);
    }

    #endregion

    #region Inherited IValidateProperty Method Tests

    [Fact]
    public async Task SetValue_FromBaseInterface_TracksCall()
    {
        var stub = new Stubs.IEntityProperty();
        IEntityProperty property = stub;

        await property.SetValue("NewEntityValue");

        Assert.True(stub.SetValue.WasCalled);
    }

    [Fact]
    public void LoadValue_FromBaseInterface_TracksCall()
    {
        var stub = new Stubs.IEntityProperty();
        IEntityProperty property = stub;

        property.LoadValue("LoadedValue");

        Assert.True(stub.LoadValue.WasCalled);
    }

    [Fact]
    public async Task WaitForTasks_FromBaseInterface_TracksCall()
    {
        var stub = new Stubs.IEntityProperty();
        IEntityProperty property = stub;

        await property.WaitForTasks();

        Assert.True(stub.WaitForTasks.WasCalled);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsPropertyTracking()
    {
        var stub = new Stubs.IEntityProperty();
        IEntityProperty property = stub;

        stub.IsModified.Value = true;
        _ = property.IsModified;
        _ = property.IsModified;

        stub.IsModified.Reset();

        Assert.Equal(0, stub.IsModified.GetCount);
    }

    [Fact]
    public void Reset_ClearsMethodTracking()
    {
        var stub = new Stubs.IEntityProperty();
        IEntityProperty property = stub;

        property.MarkSelfUnmodified();
        property.MarkSelfUnmodified();

        stub.MarkSelfUnmodified.Reset();

        Assert.False(stub.MarkSelfUnmodified.WasCalled);
    }

    #endregion
}

/// <summary>
/// Standalone stub for IEntityProperty.
/// </summary>
[KnockOff]
public partial class EntityPropertyStub : IEntityProperty
{
}

public class IEntityPropertyStandaloneTests
{
    [Fact]
    public void StandaloneStub_CanBeInstantiated()
    {
        var stub = new EntityPropertyStub();
        Assert.NotNull(stub);
    }

    [Fact]
    public void StandaloneStub_ImplementsInterface()
    {
        var stub = new EntityPropertyStub();
        IEntityProperty property = stub;
        Assert.NotNull(property);
    }

    [Fact]
    public void StandaloneStub_ImplementsIValidateProperty()
    {
        var stub = new EntityPropertyStub();
        IValidateProperty baseProperty = stub;
        Assert.NotNull(baseProperty);
    }

    [Fact]
    public void IsModified_CanBeConfigured()
    {
        var stub = new EntityPropertyStub();
        IEntityProperty property = stub;

        stub.IsModified.OnGet = (ko) => true;

        Assert.True(property.IsModified);
    }

    [Fact]
    public void DisplayName_CanBeConfigured()
    {
        var stub = new EntityPropertyStub();
        IEntityProperty property = stub;

        stub.DisplayName.OnGet = (ko) => "Standalone Display Name";

        Assert.Equal("Standalone Display Name", property.DisplayName);
    }
}

/// <summary>
/// Tests for IEntityProperty&lt;T&gt; - typed entity property interface.
/// This extends both IEntityProperty and IValidateProperty&lt;T&gt;.
/// Tests multiple interface inheritance.
/// </summary>
[KnockOff<IEntityProperty<string>>]
public partial class IEntityPropertyOfTTests
{
    [Fact]
    public void InlineStub_CanBeInstantiated()
    {
        var stub = new Stubs.IEntityProperty();
        Assert.NotNull(stub);
    }

    [Fact]
    public void InlineStub_ImplementsInterface()
    {
        var stub = new Stubs.IEntityProperty();
        IEntityProperty<string> property = stub;
        Assert.NotNull(property);
    }

    [Fact]
    public void InlineStub_ImplementsIEntityProperty()
    {
        var stub = new Stubs.IEntityProperty();
        IEntityProperty baseProperty = stub;
        Assert.NotNull(baseProperty);
    }

    [Fact]
    public void InlineStub_ImplementsIValidatePropertyOfT()
    {
        var stub = new Stubs.IEntityProperty();
        IValidateProperty<string> validateProperty = stub;
        Assert.NotNull(validateProperty);
    }

    [Fact]
    public void Value_Typed_CanBeConfiguredForGet()
    {
        var stub = new Stubs.IEntityProperty();
        IEntityProperty<string> property = stub;

        stub.Value.Value = "TypedEntityValue";

        Assert.Equal("TypedEntityValue", property.Value);
    }

    [Fact]
    public void Value_Typed_CanTrackSetter()
    {
        var stub = new Stubs.IEntityProperty();
        IEntityProperty<string> property = stub;

        property.Value = "NewTypedEntityValue";

        Assert.Equal(1, stub.Value.SetCount);
        Assert.Equal("NewTypedEntityValue", stub.Value.LastSetValue);
    }

    [Fact]
    public void IsModified_FromIEntityProperty_CanBeConfigured()
    {
        var stub = new Stubs.IEntityProperty();
        IEntityProperty<string> property = stub;

        stub.IsModified.Value = true;

        Assert.True(property.IsModified);
    }

    [Fact]
    public void IsPaused_FromIEntityProperty_CanBeConfigured()
    {
        var stub = new Stubs.IEntityProperty();
        IEntityProperty<string> property = stub;

        stub.IsPaused.Value = true;

        Assert.True(property.IsPaused);
    }
}

/// <summary>
/// Standalone stub for IEntityProperty&lt;string&gt;.
/// </summary>
[KnockOff]
public partial class EntityPropertyOfStringStub : IEntityProperty<string>
{
}

public class IEntityPropertyOfTStandaloneTests
{
    [Fact]
    public void StandaloneStub_CanBeInstantiated()
    {
        var stub = new EntityPropertyOfStringStub();
        Assert.NotNull(stub);
    }

    [Fact]
    public void StandaloneStub_ImplementsInterface()
    {
        var stub = new EntityPropertyOfStringStub();
        IEntityProperty<string> property = stub;
        Assert.NotNull(property);
    }

    [Fact]
    public void StandaloneStub_ImplementsIEntityProperty()
    {
        var stub = new EntityPropertyOfStringStub();
        IEntityProperty baseProperty = stub;
        Assert.NotNull(baseProperty);
    }

    [Fact]
    public void StandaloneStub_ImplementsIValidatePropertyOfT()
    {
        var stub = new EntityPropertyOfStringStub();
        IValidateProperty<string> validateProperty = stub;
        Assert.NotNull(validateProperty);
    }
}
