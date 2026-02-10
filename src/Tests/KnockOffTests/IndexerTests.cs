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

		knockOff.Indexer["Name"].Returns(new PropertyInfo { Name = "Name", Value = "Test" });

		var result = store["Name"];

		knockOff.Indexer.VerifyGet(Called.Once);
		Assert.Equal("Name", knockOff.Indexer.LastGetKey);
		Assert.NotNull(result);
		Assert.Equal("Name", result.Name);
	}

	[Fact]
	public void Indexer_Get_MultipleKeys_TracksAllKeys()
	{
		var knockOff = new PropertyStoreKnockOff();
		IPropertyStore store = knockOff;

		knockOff.Indexer["First"].Returns(new PropertyInfo { Name = "First", Value = "1" });
		knockOff.Indexer["Second"].Returns(new PropertyInfo { Name = "Second", Value = "2" });
		knockOff.Indexer["Third"].Returns(new PropertyInfo { Name = "Third", Value = "3" });

		_ = store["First"];
		_ = store["Second"];
		_ = store["Third"];

		knockOff.Indexer.VerifyGet(Called.Exactly(3));
		Assert.Equal("Third", knockOff.Indexer.LastGetKey); // Last key accessed
	}

	[Fact]
	public void Indexer_OnGet_CallbackReturnsValue()
	{
		var knockOff = new PropertyStoreKnockOff();
		IPropertyStore store = knockOff;

		var mockProperty = new PropertyInfo { Name = "FromCallback", Value = "Mocked" };
		knockOff.Indexer.Get((key) =>
		{
			if (key == "Special") return mockProperty;
			return null;
		});

		var result = store["Special"];

		Assert.Same(mockProperty, result);
		Assert.Equal("Special", knockOff.Indexer.LastGetKey);
	}

	[Fact]
	public void Indexer_OnGet_CallbackCanAccessKnockOffInstance()
	{
		var knockOff = new PropertyStoreKnockOff();
		IPropertyStore store = knockOff;

		var accessCount = 0;
		knockOff.Indexer.Get((key) =>
		{
			// Track access count via closure
			accessCount++;
			return new PropertyInfo { Name = key, Value = $"Accessed {accessCount} times" };
		});

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

		knockOff.Indexer.VerifySet(Called.Once);
		Assert.NotNull(knockOff.Indexer.LastSetEntry);
		Assert.Equal("Test", knockOff.Indexer.LastSetEntry.Value.Key);
		Assert.Same(prop, knockOff.Indexer.LastSetEntry.Value.Value);
	}

	[Fact]
	public void Indexer_Set_CapturedValueCanBeRetrievedViaPerKey()
	{
		var knockOff = new ReadWriteStoreKnockOff();
		IReadWriteStore store = knockOff;

		// Use a Set callback to capture what was stored, then configure a Get to return it
		var stored = new Dictionary<string, PropertyInfo?>();
		knockOff.Indexer.Set((key, value) => stored[key] = value);
		knockOff.Indexer.Get(key => stored.TryGetValue(key, out var v) ? v : null);

		var prop = new PropertyInfo { Name = "Stored", Value = "InBacking" };
		store["Stored"] = prop;

		Assert.True(stored.ContainsKey("Stored"));
		Assert.Same(prop, stored["Stored"]);

		var retrieved = store["Stored"];
		Assert.Same(prop, retrieved);
	}

	[Fact]
	public void Indexer_OnSet_CallbackInterceptsSetter()
	{
		var knockOff = new ReadWriteStoreKnockOff();
		IReadWriteStore store = knockOff;

		(string key, PropertyInfo? value)? capturedEntry = null;
		knockOff.Indexer.Set((key, value) =>
		{
			capturedEntry = (key, value);
		});

		var prop = new PropertyInfo { Name = "Intercepted", Value = "NotStored" };
		store["MyKey"] = prop;

		Assert.NotNull(capturedEntry);
		Assert.Equal("MyKey", capturedEntry.Value.key);
		Assert.Same(prop, capturedEntry.Value.value);
	}

	[Fact]
	public void Indexer_Reset_ClearsTrackingButPreservesConfiguration()
	{
		var knockOff = new ReadWriteStoreKnockOff();
		IReadWriteStore store = knockOff;

		var prop = new PropertyInfo { Name = "Test", Value = "Value" };
		knockOff.Indexer.Get((key) => prop);

		_ = store["Key1"];
		_ = store["Key2"];
		store["Key3"] = prop;

		knockOff.Indexer.VerifyGet(Called.Exactly(2));
		knockOff.Indexer.VerifySet(Called.Once);

		knockOff.Indexer.Reset();

		// Tracking state is cleared
		knockOff.Indexer.VerifyGet(Called.Never);
		Assert.Null(knockOff.Indexer.LastGetKey);
		knockOff.Indexer.VerifySet(Called.Never);
		Assert.Null(knockOff.Indexer.LastSetEntry);

		// Configuration is preserved - Get callback still works
		var retrieved = store["AfterReset"];
		Assert.Same(prop, retrieved); // Get callback still returns our prop
	}

	[Fact]
	public void Indexer_NullableReturn_ReturnsDefaultWhenNotFound()
	{
		var knockOff = new PropertyStoreKnockOff();
		IPropertyStore store = knockOff;

		var result = store["NonExistent"];

		Assert.Null(result);
		knockOff.Indexer.VerifyGet(Called.Once);
	}
}
