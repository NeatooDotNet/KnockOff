namespace KnockOff.Tests;

/// <summary>
/// Tests for smart default return values feature.
/// Verifies that unconfigured methods return sensible defaults based on return type.
/// </summary>
public class SmartDefaultsTests
{
	#region Value Types - Should return default

	[Fact]
	public void ValueType_Int_ReturnsDefault()
	{
		var knockOff = new SmartDefaultsKnockOff();
		ISmartDefaultsService service = knockOff;

		var result = service.GetInt();

		Assert.Equal(0, result);
	}

	[Fact]
	public void ValueType_Bool_ReturnsDefault()
	{
		var knockOff = new SmartDefaultsKnockOff();
		ISmartDefaultsService service = knockOff;

		var result = service.GetBool();

		Assert.False(result);
	}

	[Fact]
	public void ValueType_DateTime_ReturnsDefault()
	{
		var knockOff = new SmartDefaultsKnockOff();
		ISmartDefaultsService service = knockOff;

		var result = service.GetDateTime();

		Assert.Equal(default(DateTime), result);
	}

	#endregion

	#region Nullable Reference Types - Should return null

	[Fact]
	public void NullableReference_String_ReturnsNull()
	{
		var knockOff = new SmartDefaultsKnockOff();
		ISmartDefaultsService service = knockOff;

		var result = service.GetNullableString();

		Assert.Null(result);
	}

	[Fact]
	public void NullableReference_Entity_ReturnsNull()
	{
		var knockOff = new SmartDefaultsKnockOff();
		ISmartDefaultsService service = knockOff;

		var result = service.GetNullableEntity();

		Assert.Null(result);
	}

	#endregion

	#region Non-Nullable with Parameterless Constructor - Should return new instance

	[Fact]
	public void NewInstance_List_ReturnsNewList()
	{
		var knockOff = new SmartDefaultsKnockOff();
		ISmartDefaultsService service = knockOff;

		var result = service.GetList();

		Assert.NotNull(result);
		Assert.IsType<List<string>>(result);
		Assert.Empty(result); // New list is empty
	}

	[Fact]
	public void NewInstance_Dictionary_ReturnsNewDictionary()
	{
		var knockOff = new SmartDefaultsKnockOff();
		ISmartDefaultsService service = knockOff;

		var result = service.GetDictionary();

		Assert.NotNull(result);
		Assert.IsType<Dictionary<string, int>>(result);
		Assert.Empty(result); // New dictionary is empty
	}

	[Fact]
	public void NewInstance_CustomClass_ReturnsNewInstance()
	{
		var knockOff = new SmartDefaultsKnockOff();
		ISmartDefaultsService service = knockOff;

		var result = service.GetEntity();

		Assert.NotNull(result);
		Assert.IsType<TestEntity>(result);
		Assert.Equal(0, result.Id); // Default values
		Assert.Equal("", result.Name);
	}

	[Fact]
	public void NewInstance_IList_ReturnsNewList()
	{
		var knockOff = new SmartDefaultsKnockOff();
		ISmartDefaultsService service = knockOff;

		var result = service.GetIList();

		Assert.NotNull(result);
		Assert.IsType<List<string>>(result); // Concrete type is List<string>
		Assert.Empty(result); // New list is empty
	}

	[Fact]
	public void NewInstance_MultipleCallsReturnDifferentInstances()
	{
		var knockOff = new SmartDefaultsKnockOff();
		ISmartDefaultsService service = knockOff;

		var result1 = service.GetList();
		var result2 = service.GetList();

		Assert.NotSame(result1, result2); // Each call creates new instance
	}

	#endregion

	#region Non-Nullable without Parameterless Constructor - Should throw

	[Fact]
	public void ThrowException_String_Throws()
	{
		var knockOff = new SmartDefaultsKnockOff();
		ISmartDefaultsService service = knockOff;

		var ex = Assert.Throws<InvalidOperationException>(() => service.GetString());
		Assert.Contains("No implementation provided", ex.Message);
	}

	[Fact]
	public void ThrowException_Interface_Throws()
	{
		var knockOff = new SmartDefaultsKnockOff();
		ISmartDefaultsService service = knockOff;

		var ex = Assert.Throws<InvalidOperationException>(() => service.GetDisposable());
		Assert.Contains("No implementation provided", ex.Message);
	}

	#endregion

	#region Task<T> Variants

	[Fact]
	public async Task TaskOfT_ValueType_ReturnsTaskWithDefault()
	{
		var knockOff = new SmartDefaultsKnockOff();
		ISmartDefaultsService service = knockOff;

		var result = await service.GetIntAsync();

		Assert.Equal(0, result);
	}

	[Fact]
	public async Task TaskOfT_WithNewInstance_ReturnsTaskWithNewInstance()
	{
		var knockOff = new SmartDefaultsKnockOff();
		ISmartDefaultsService service = knockOff;

		var result = await service.GetListAsync();

		Assert.NotNull(result);
		Assert.IsType<List<string>>(result);
		Assert.Empty(result);
	}

	[Fact]
	public async Task TaskOfT_WithoutNewInstance_Throws()
	{
		var knockOff = new SmartDefaultsKnockOff();
		ISmartDefaultsService service = knockOff;

		// The throw happens synchronously before the task is created
		var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await service.GetStringAsync());
		Assert.Contains("No implementation provided", ex.Message);
	}

	#endregion

	#region Property Backing Fields

	[Fact]
	public void PropertyBacking_ValueType_HasDefaultValue()
	{
		var knockOff = new SmartDefaultsKnockOff();
		ISmartDefaultsService service = knockOff;

		var result = service.Count;

		Assert.Equal(0, result);
	}

	[Fact]
	public void PropertyBacking_ListType_HasNewInstance()
	{
		var knockOff = new SmartDefaultsKnockOff();
		ISmartDefaultsService service = knockOff;

		var result = service.Items;

		Assert.NotNull(result);
		Assert.IsType<List<string>>(result);
		Assert.Empty(result);
	}

	#endregion

	#region OnCall Callback Still Works

	[Fact]
	public void OnCall_OverridesDefaultBehavior()
	{
		var knockOff = new SmartDefaultsKnockOff();
		ISmartDefaultsService service = knockOff;

		knockOff.ISmartDefaultsService.GetInt.OnCall = (ko) => 42;

		var result = service.GetInt();

		Assert.Equal(42, result);
	}

	[Fact]
	public void OnCall_OverridesThrowBehavior()
	{
		var knockOff = new SmartDefaultsKnockOff();
		ISmartDefaultsService service = knockOff;

		knockOff.ISmartDefaultsService.GetString.OnCall = (ko) => "Hello";

		var result = service.GetString();

		Assert.Equal("Hello", result);
	}

	#endregion
}
