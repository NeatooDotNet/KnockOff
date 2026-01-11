namespace KnockOff.Tests;

/// <summary>
/// Tests for indexer support (get-only and get/set).
/// </summary>
public class IndexerTests
{
	[Fact]
	public void Indexer_Get_TracksKeyAccessed()
	{
		var knockOff = new PropertyStoreKnockOff();
		IPropertyStore store = knockOff;

		knockOff.Indexer.Backing["Name"] = new PropertyInfo { Name = "Name", Value = "Test" };

		var result = store["Name"];

		Assert.Equal(1, knockOff.Indexer.GetCount);
		Assert.Equal("Name", knockOff.Indexer.LastGetKey);
		Assert.NotNull(result);
		Assert.Equal("Name", result.Name);
	}

	[Fact]
	public void Indexer_Get_MultipleKeys_TracksAllKeys()
	{
		var knockOff = new PropertyStoreKnockOff();
		IPropertyStore store = knockOff;

		knockOff.Indexer.Backing["First"] = new PropertyInfo { Name = "First", Value = "1" };
		knockOff.Indexer.Backing["Second"] = new PropertyInfo { Name = "Second", Value = "2" };
		knockOff.Indexer.Backing["Third"] = new PropertyInfo { Name = "Third", Value = "3" };

		_ = store["First"];
		_ = store["Second"];
		_ = store["Third"];

		Assert.Equal(3, knockOff.Indexer.GetCount);
		Assert.Equal("Third", knockOff.Indexer.LastGetKey); // Last key accessed
	}

	[Fact]
	public void Indexer_OnGet_CallbackReturnsValue()
	{
		var knockOff = new PropertyStoreKnockOff();
		IPropertyStore store = knockOff;

		var mockProperty = new PropertyInfo { Name = "FromCallback", Value = "Mocked" };
		knockOff.Indexer.OnGet = (ko, key) =>
		{
			if (key == "Special") return mockProperty;
			return null;
		};

		var result = store["Special"];

		Assert.Same(mockProperty, result);
		Assert.Equal("Special", knockOff.Indexer.LastGetKey);
	}

	[Fact]
	public void Indexer_OnGet_CallbackCanAccessKnockOffInstance()
	{
		var knockOff = new PropertyStoreKnockOff();
		IPropertyStore store = knockOff;

		knockOff.Indexer.OnGet = (ko, key) =>
		{
			Assert.Same(knockOff, ko);
			return new PropertyInfo { Name = key, Value = $"Accessed {ko.Indexer.GetCount} times" };
		};

		_ = store["First"];
		var result = store["Second"];

		Assert.Equal("Second", result?.Name);
		Assert.Contains("2", result?.Value);
	}

	[Fact]
	public void Indexer_Set_TracksKeyAndValue()
	{
		var knockOff = new ReadWriteStoreKnockOff();
		IReadWriteStore store = knockOff;

		var prop = new PropertyInfo { Name = "Test", Value = "Value" };
		store["Test"] = prop;

		Assert.Equal(1, knockOff.Indexer.SetCount);
		Assert.NotNull(knockOff.Indexer.LastSetEntry);
		Assert.Equal("Test", knockOff.Indexer.LastSetEntry.Value.Key);
		Assert.Same(prop, knockOff.Indexer.LastSetEntry.Value.Value);
	}

	[Fact]
	public void Indexer_Set_StoresInBackingDictionary()
	{
		var knockOff = new ReadWriteStoreKnockOff();
		IReadWriteStore store = knockOff;

		var prop = new PropertyInfo { Name = "Stored", Value = "InBacking" };
		store["Stored"] = prop;

		Assert.True(knockOff.Indexer.Backing.ContainsKey("Stored"));
		Assert.Same(prop, knockOff.Indexer.Backing["Stored"]);

		var retrieved = store["Stored"];
		Assert.Same(prop, retrieved);
	}

	[Fact]
	public void Indexer_OnSet_CallbackInterceptsSetter()
	{
		var knockOff = new ReadWriteStoreKnockOff();
		IReadWriteStore store = knockOff;

		(string key, PropertyInfo? value)? capturedEntry = null;
		knockOff.Indexer.OnSet = (ko, key, value) =>
		{
			capturedEntry = (key, value);
		};

		var prop = new PropertyInfo { Name = "Intercepted", Value = "NotStored" };
		store["MyKey"] = prop;

		Assert.NotNull(capturedEntry);
		Assert.Equal("MyKey", capturedEntry.Value.key);
		Assert.Same(prop, capturedEntry.Value.value);

		// Since OnSet was used, backing was NOT updated
		Assert.False(knockOff.Indexer.Backing.ContainsKey("MyKey"));
	}

	[Fact]
	public void Indexer_Reset_ClearsAllState()
	{
		var knockOff = new ReadWriteStoreKnockOff();
		IReadWriteStore store = knockOff;

		var prop = new PropertyInfo { Name = "Test", Value = "Value" };
		knockOff.Indexer.Backing["Existing"] = prop;
		knockOff.Indexer.OnGet = (ko, key) => prop;

		_ = store["Key1"];
		_ = store["Key2"];
		store["Key3"] = prop;

		Assert.Equal(2, knockOff.Indexer.GetCount);
		Assert.Equal(1, knockOff.Indexer.SetCount);
		Assert.NotNull(knockOff.Indexer.OnGet);

		knockOff.Indexer.Reset();

		Assert.Equal(0, knockOff.Indexer.GetCount);
		Assert.Null(knockOff.Indexer.LastGetKey);
		Assert.Equal(0, knockOff.Indexer.SetCount);
		Assert.Null(knockOff.Indexer.LastSetEntry);
		Assert.Null(knockOff.Indexer.OnGet);
		Assert.Null(knockOff.Indexer.OnSet);

		// Backing dictionary is NOT cleared by Reset
		Assert.True(knockOff.Indexer.Backing.ContainsKey("Existing"));
	}

	[Fact]
	public void Indexer_NullableReturn_ReturnsDefaultWhenNotFound()
	{
		var knockOff = new PropertyStoreKnockOff();
		IPropertyStore store = knockOff;

		var result = store["NonExistent"];

		Assert.Null(result);
		Assert.Equal(1, knockOff.Indexer.GetCount);
	}
}
