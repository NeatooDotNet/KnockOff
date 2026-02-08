using Neatoo;
using System.Reflection;

namespace KnockOff.NeatooInterfaceTests.Properties;

/// <summary>
/// Tests for IPropertyInfo - property metadata interface.
/// This interface wraps System.Reflection.PropertyInfo.
/// </summary>
[KnockOff<IPropertyInfo>]
public partial class IPropertyInfoTests
{
    [Fact]
    public void InlineStub_CanBeInstantiated()
    {
        var stub = new Stubs.IPropertyInfo();
        Assert.NotNull(stub);
    }

    [Fact]
    public void InlineStub_ImplementsInterface()
    {
        var stub = new Stubs.IPropertyInfo();
        IPropertyInfo propertyInfo = stub;
        Assert.NotNull(propertyInfo);
    }

    #region Property Tests

    [Fact]
    public void PropertyInfo_CanBeConfigured()
    {
        var stub = new Stubs.IPropertyInfo();
        IPropertyInfo propertyInfo = stub;

        var reflectedPropertyInfo = typeof(TestClass).GetProperty("Name")!;
        stub.PropertyInfo.Get(reflectedPropertyInfo);

        Assert.Same(reflectedPropertyInfo, propertyInfo.PropertyInfo);
        stub.PropertyInfo.VerifyGet(Called.Once);
    }

    [Fact]
    public void Name_CanBeConfigured()
    {
        var stub = new Stubs.IPropertyInfo();
        IPropertyInfo propertyInfo = stub;

        stub.Name.Get("TestProperty");

        Assert.Equal("TestProperty", propertyInfo.Name);
    }

    [Fact]
    public void Type_CanBeConfigured()
    {
        var stub = new Stubs.IPropertyInfo();
        IPropertyInfo propertyInfo = stub;

        stub.Type.Get(typeof(string));

        Assert.Equal(typeof(string), propertyInfo.Type);
    }

    [Fact]
    public void Key_CanBeConfigured()
    {
        var stub = new Stubs.IPropertyInfo();
        IPropertyInfo propertyInfo = stub;

        stub.Key.Get("TestClass.Name");

        Assert.Equal("TestClass.Name", propertyInfo.Key);
    }

    [Fact]
    public void IsPrivateSetter_CanBeConfigured()
    {
        var stub = new Stubs.IPropertyInfo();
        IPropertyInfo propertyInfo = stub;

        stub.IsPrivateSetter.Get(true);

        Assert.True(propertyInfo.IsPrivateSetter);
    }

    #endregion

    #region Method Tests

    [Fact]
    public void GetCustomAttribute_Generic_TracksCall()
    {
        var stub = new Stubs.IPropertyInfo();
        IPropertyInfo propertyInfo = stub;

        propertyInfo.GetCustomAttribute<ObsoleteAttribute>();

        stub.GetCustomAttribute.Verify();
    }

    [Fact]
    public void GetCustomAttribute_Generic_TracksTypeArgument()
    {
        var stub = new Stubs.IPropertyInfo();
        IPropertyInfo propertyInfo = stub;

        propertyInfo.GetCustomAttribute<ObsoleteAttribute>();

        stub.GetCustomAttribute.Of<ObsoleteAttribute>().Verify();
    }

    [Fact]
    public void GetCustomAttribute_Generic_ReturnsConfiguredValue()
    {
        var stub = new Stubs.IPropertyInfo();
        IPropertyInfo propertyInfo = stub;

        var expected = new ObsoleteAttribute("Test");
        stub.GetCustomAttribute.Of<ObsoleteAttribute>().Return(() => expected);

        var result = propertyInfo.GetCustomAttribute<ObsoleteAttribute>();

        Assert.Same(expected, result);
    }

    [Fact]
    public void GetCustomAttribute_Generic_CanReturnNull()
    {
        var stub = new Stubs.IPropertyInfo();
        IPropertyInfo propertyInfo = stub;

        stub.GetCustomAttribute.Of<ObsoleteAttribute>().Return(() => null);

        var result = propertyInfo.GetCustomAttribute<ObsoleteAttribute>();

        Assert.Null(result);
    }

    [Fact]
    public void GetCustomAttributes_TracksCall()
    {
        var stub = new Stubs.IPropertyInfo();
        IPropertyInfo propertyInfo = stub;
        stub.GetCustomAttributes.Return(() => Array.Empty<Attribute>());

        propertyInfo.GetCustomAttributes();

        stub.GetCustomAttributes.Verify(Called.Once);
    }

    [Fact]
    public void GetCustomAttributes_ReturnsConfiguredValue()
    {
        var stub = new Stubs.IPropertyInfo();
        IPropertyInfo propertyInfo = stub;

        var attributes = new List<Attribute> { new ObsoleteAttribute("Test") };
        stub.GetCustomAttributes.Return(() => attributes);

        var result = propertyInfo.GetCustomAttributes();

        Assert.Same(attributes, result);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsPropertyTracking()
    {
        var stub = new Stubs.IPropertyInfo();
        IPropertyInfo propertyInfo = stub;

        stub.Name.Get("Test");
        _ = propertyInfo.Name;
        _ = propertyInfo.Name;

        stub.Name.Reset();

        stub.Name.VerifyGet(Called.Never);
    }

    [Fact]
    public void Reset_ClearsMethodTracking()
    {
        var stub = new Stubs.IPropertyInfo();
        IPropertyInfo propertyInfo = stub;
        stub.GetCustomAttributes.Return(() => Array.Empty<Attribute>());

        propertyInfo.GetCustomAttributes();
        propertyInfo.GetCustomAttributes();

        stub.GetCustomAttributes.Reset();

        stub.GetCustomAttributes.Verify(Called.Never);
    }

    #endregion

    // Test class for reflection tests
    private class TestClass
    {
        public string Name { get; set; } = "";
    }
}

/// <summary>
/// Standalone stub for IPropertyInfo.
/// </summary>
[KnockOff]
public partial class PropertyInfoStub : IPropertyInfo
{
}

public class IPropertyInfoStandaloneTests
{
    [Fact]
    public void StandaloneStub_CanBeInstantiated()
    {
        var stub = new PropertyInfoStub();
        Assert.NotNull(stub);
    }

    [Fact]
    public void StandaloneStub_ImplementsInterface()
    {
        var stub = new PropertyInfoStub();
        IPropertyInfo propertyInfo = stub;
        Assert.NotNull(propertyInfo);
    }

    [Fact]
    public void Name_CanBeConfigured()
    {
        var stub = new PropertyInfoStub();
        IPropertyInfo propertyInfo = stub;

        stub.Name.Get("StandaloneName");

        Assert.Equal("StandaloneName", propertyInfo.Name);
    }

    [Fact]
    public void GetCustomAttribute_Generic_TracksCall()
    {
        var stub = new PropertyInfoStub();
        IPropertyInfo propertyInfo = stub;

        propertyInfo.GetCustomAttribute<ObsoleteAttribute>();

        stub.GetCustomAttribute.Verify();
    }
}

/// <summary>
/// Tests for IPropertyInfoList - property metadata collection interface.
/// </summary>
[KnockOff<IPropertyInfoList>]
public partial class IPropertyInfoListTests
{
    [Fact]
    public void InlineStub_CanBeInstantiated()
    {
        var stub = new Stubs.IPropertyInfoList();
        Assert.NotNull(stub);
    }

    [Fact]
    public void InlineStub_ImplementsInterface()
    {
        var stub = new Stubs.IPropertyInfoList();
        IPropertyInfoList list = stub;
        Assert.NotNull(list);
    }

    #region Method Tests

    [Fact]
    public void GetPropertyInfo_TracksCall()
    {
        var stub = new Stubs.IPropertyInfoList();
        IPropertyInfoList list = stub;

        list.GetPropertyInfo("Name");

        stub.GetPropertyInfo.Verify(Called.Once);
    }

    [Fact]
    public void GetPropertyInfo_CapturesArgument()
    {
        var stub = new Stubs.IPropertyInfoList();
        IPropertyInfoList list = stub;

        list.GetPropertyInfo("TestProperty");

        Assert.Equal("TestProperty", stub.GetPropertyInfo.LastArg);
    }

    [Fact]
    public void GetPropertyInfo_ReturnsConfiguredValue()
    {
        var stub = new Stubs.IPropertyInfoList();
        IPropertyInfoList list = stub;

        var propertyInfoStub = new PropertyInfoStub();
        stub.GetPropertyInfo.Return((name) => propertyInfoStub);

        var result = list.GetPropertyInfo("Name");

        Assert.Same(propertyInfoStub, result);
    }

    [Fact]
    public void GetPropertyInfo_CanReturnNull()
    {
        var stub = new Stubs.IPropertyInfoList();
        IPropertyInfoList list = stub;

        stub.GetPropertyInfo.Return((name) => null);

        var result = list.GetPropertyInfo("NonExistent");

        Assert.Null(result);
    }

    [Fact]
    public void Properties_TracksCall()
    {
        var stub = new Stubs.IPropertyInfoList();
        IPropertyInfoList list = stub;
        stub.Properties.Return(() => Array.Empty<IPropertyInfo>());

        list.Properties();

        stub.Properties.Verify();
    }

    [Fact]
    public void Properties_ReturnsConfiguredValue()
    {
        var stub = new Stubs.IPropertyInfoList();
        IPropertyInfoList list = stub;

        var properties = new List<IPropertyInfo> { new PropertyInfoStub() };
        stub.Properties.Return(() => properties);

        var result = list.Properties();

        Assert.Same(properties, result);
    }

    [Fact]
    public void HasProperty_TracksCall()
    {
        var stub = new Stubs.IPropertyInfoList();
        IPropertyInfoList list = stub;

        list.HasProperty("Name");

        stub.HasProperty.Verify();
    }

    [Fact]
    public void HasProperty_ReturnsConfiguredValue()
    {
        var stub = new Stubs.IPropertyInfoList();
        IPropertyInfoList list = stub;

        stub.HasProperty.Return((name) => name == "Name");

        Assert.True(list.HasProperty("Name"));
        Assert.False(list.HasProperty("Other"));
    }

    #endregion
}

/// <summary>
/// Standalone stub for IPropertyInfoList.
/// </summary>
[KnockOff]
public partial class PropertyInfoListStub : IPropertyInfoList
{
}

/// <summary>
/// Tests for IPropertyInfoList&lt;T&gt; - typed property metadata collection.
/// This is a generic interface.
/// </summary>
[KnockOff<IPropertyInfoList<IValidateBase>>]
public partial class IPropertyInfoListOfTTests
{
    [Fact]
    public void InlineStub_CanBeInstantiated()
    {
        var stub = new Stubs.IPropertyInfoList();
        Assert.NotNull(stub);
    }

    [Fact]
    public void InlineStub_ImplementsInterface()
    {
        var stub = new Stubs.IPropertyInfoList();
        IPropertyInfoList<IValidateBase> list = stub;
        Assert.NotNull(list);
    }

    [Fact]
    public void InlineStub_ImplementsBaseInterface()
    {
        var stub = new Stubs.IPropertyInfoList();
        IPropertyInfoList baseList = stub;
        Assert.NotNull(baseList);
    }

    [Fact]
    public void GetPropertyInfo_FromBaseInterface_TracksCall()
    {
        var stub = new Stubs.IPropertyInfoList();
        IPropertyInfoList<IValidateBase> list = stub;

        list.GetPropertyInfo("Property");

        stub.GetPropertyInfo.Verify();
    }

    [Fact]
    public void HasProperty_FromBaseInterface_TracksCall()
    {
        var stub = new Stubs.IPropertyInfoList();
        IPropertyInfoList<IValidateBase> list = stub;

        list.HasProperty("Property");

        stub.HasProperty.Verify();
    }
}

/// <summary>
/// Standalone stub for IPropertyInfoList&lt;IValidateBase&gt;.
/// </summary>
[KnockOff]
public partial class PropertyInfoListOfTStub : IPropertyInfoList<IValidateBase>
{
}
