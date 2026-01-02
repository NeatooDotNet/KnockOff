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

		knockOff.StringIndexerBacking["Name"] = new PropertyInfo { Name = "Name", Value = "Test" };

		var result = store["Name"];

		Assert.Equal(1, knockOff.Spy.StringIndexer.GetCount);
		Assert.Equal("Name", knockOff.Spy.StringIndexer.LastGetKey);
		Assert.NotNull(result);
		Assert.Equal("Name", result.Name);
	}

	[Fact]
	public void Indexer_Get_MultipleKeys_TracksAllKeys()
	{
		var knockOff = new PropertyStoreKnockOff();
		IPropertyStore store = knockOff;

		knockOff.StringIndexerBacking["First"] = new PropertyInfo { Name = "First", Value = "1" };
		knockOff.StringIndexerBacking["Second"] = new PropertyInfo { Name = "Second", Value = "2" };
		knockOff.StringIndexerBacking["Third"] = new PropertyInfo { Name = "Third", Value = "3" };

		_ = store["First"];
		_ = store["Second"];
		_ = store["Third"];

		Assert.Equal(3, knockOff.Spy.StringIndexer.GetCount);
		Assert.Equal(3, knockOff.Spy.StringIndexer.AllGetKeys.Count);
		Assert.Equal("First", knockOff.Spy.StringIndexer.AllGetKeys[0]);
		Assert.Equal("Second", knockOff.Spy.StringIndexer.AllGetKeys[1]);
		Assert.Equal("Third", knockOff.Spy.StringIndexer.AllGetKeys[2]);
	}

	[Fact]
	public void Indexer_OnGet_CallbackReturnsValue()
	{
		var knockOff = new PropertyStoreKnockOff();
		IPropertyStore store = knockOff;

		var mockProperty = new PropertyInfo { Name = "FromCallback", Value = "Mocked" };
		knockOff.Spy.StringIndexer.OnGet = (ko, key) =>
		{
			if (key == "Special") return mockProperty;
			return null;
		};

		var result = store["Special"];

		Assert.Same(mockProperty, result);
		Assert.Equal("Special", knockOff.Spy.StringIndexer.LastGetKey);
	}

	[Fact]
	public void Indexer_OnGet_CallbackCanAccessKnockOffInstance()
	{
		var knockOff = new PropertyStoreKnockOff();
		IPropertyStore store = knockOff;

		knockOff.Spy.StringIndexer.OnGet = (ko, key) =>
		{
			Assert.Same(knockOff, ko);
			return new PropertyInfo { Name = key, Value = $"Accessed {ko.Spy.StringIndexer.GetCount} times" };
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

		Assert.Equal(1, knockOff.Spy.StringIndexer.SetCount);
		Assert.NotNull(knockOff.Spy.StringIndexer.LastSetEntry);
		Assert.Equal("Test", knockOff.Spy.StringIndexer.LastSetEntry.Value.key);
		Assert.Same(prop, knockOff.Spy.StringIndexer.LastSetEntry.Value.value);
	}

	[Fact]
	public void Indexer_Set_StoresInBackingDictionary()
	{
		var knockOff = new ReadWriteStoreKnockOff();
		IReadWriteStore store = knockOff;

		var prop = new PropertyInfo { Name = "Stored", Value = "InBacking" };
		store["Stored"] = prop;

		Assert.True(knockOff.StringIndexerBacking.ContainsKey("Stored"));
		Assert.Same(prop, knockOff.StringIndexerBacking["Stored"]);

		var retrieved = store["Stored"];
		Assert.Same(prop, retrieved);
	}

	[Fact]
	public void Indexer_OnSet_CallbackInterceptsSetter()
	{
		var knockOff = new ReadWriteStoreKnockOff();
		IReadWriteStore store = knockOff;

		(string key, PropertyInfo? value)? capturedEntry = null;
		knockOff.Spy.StringIndexer.OnSet = (ko, key, value) =>
		{
			capturedEntry = (key, value);
		};

		var prop = new PropertyInfo { Name = "Intercepted", Value = "NotStored" };
		store["MyKey"] = prop;

		Assert.NotNull(capturedEntry);
		Assert.Equal("MyKey", capturedEntry.Value.key);
		Assert.Same(prop, capturedEntry.Value.value);

		// Since OnSet was used, backing was NOT updated
		Assert.False(knockOff.StringIndexerBacking.ContainsKey("MyKey"));
	}

	[Fact]
	public void Indexer_Reset_ClearsAllState()
	{
		var knockOff = new ReadWriteStoreKnockOff();
		IReadWriteStore store = knockOff;

		var prop = new PropertyInfo { Name = "Test", Value = "Value" };
		knockOff.StringIndexerBacking["Existing"] = prop;
		knockOff.Spy.StringIndexer.OnGet = (ko, key) => prop;

		_ = store["Key1"];
		_ = store["Key2"];
		store["Key3"] = prop;

		Assert.Equal(2, knockOff.Spy.StringIndexer.GetCount);
		Assert.Equal(1, knockOff.Spy.StringIndexer.SetCount);
		Assert.NotNull(knockOff.Spy.StringIndexer.OnGet);

		knockOff.Spy.StringIndexer.Reset();

		Assert.Equal(0, knockOff.Spy.StringIndexer.GetCount);
		Assert.Empty(knockOff.Spy.StringIndexer.AllGetKeys);
		Assert.Equal(0, knockOff.Spy.StringIndexer.SetCount);
		Assert.Empty(knockOff.Spy.StringIndexer.AllSetEntries);
		Assert.Null(knockOff.Spy.StringIndexer.OnGet);
		Assert.Null(knockOff.Spy.StringIndexer.OnSet);

		// Backing dictionary is NOT cleared by Reset
		Assert.True(knockOff.StringIndexerBacking.ContainsKey("Existing"));
	}

	[Fact]
	public void Indexer_NullableReturn_ReturnsDefaultWhenNotFound()
	{
		var knockOff = new PropertyStoreKnockOff();
		IPropertyStore store = knockOff;

		var result = store["NonExistent"];

		Assert.Null(result);
		Assert.Equal(1, knockOff.Spy.StringIndexer.GetCount);
	}
}
