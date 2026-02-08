namespace KnockOff.Tests;

/// <summary>
/// Tests for Get(value) property value overloads.
/// Phase 2 of value-based overloads feature.
/// </summary>
public partial class PropertyValueOverloadTests
{
	#region Basic Value Return Tests

	[Fact]
	public void OnGet_WithValue_ReturnsConfiguredValue()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		var tracking = knockOff.Name.Get("configured value");

		var result = service.Name;

		Assert.Equal("configured value", result);
		tracking.Verify(Called.Once);
	}

	[Fact]
	public void OnGet_WithValue_ReturnsNullWhenConfiguredNull()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		var tracking = knockOff.Name.Get((string?)null);

		var result = service.Name;

		Assert.Null(result);
		tracking.Verify(Called.Once);
	}

	[Fact]
	public void OnGet_WithValue_RepeatsIndefinitely()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		knockOff.Name.Get("repeated");

		Assert.Equal("repeated", service.Name);
		Assert.Equal("repeated", service.Name);
		Assert.Equal("repeated", service.Name);
	}

	#endregion

	#region Value Type Tests

	[Fact]
	public void OnGet_WithIntValue_ReturnsConfiguredValue()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		knockOff.Count.Get(42);

		Assert.Equal(42, service.Count);
	}

	[Fact]
	public void OnGet_WithBoolValue_ReturnsConfiguredValue()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		knockOff.IsEnabled.Get(true);

		Assert.True(service.IsEnabled);
	}

	[Fact]
	public void OnGet_WithDefaultValue_ReturnsDefault()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		knockOff.Count.Get(0);

		Assert.Equal(0, service.Count);
	}

	#endregion

	#region Tracking Tests

	[Fact]
	public void OnGet_WithValue_ReturnsTrackingInterface()
	{
		var knockOff = new PropertyTestKnockOff();

		var tracking = knockOff.Name.Get("test");

		Assert.NotNull(tracking);
		Assert.IsAssignableFrom<IPropertyGetTracking>(tracking);
	}

	[Fact]
	public void OnGet_WithValue_SupportsVerifiable()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		var tracking = knockOff.Name.Get("test");

		_ = service.Name;

		tracking.Verify(Called.Once);
	}

	[Fact]
	public void OnGet_WithValue_SupportsVerifiableWithTimes()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		var tracking = knockOff.Name.Get("test");

		_ = service.Name;
		_ = service.Name;

		tracking.Verify(Called.Exactly(2));
	}

	#endregion

	#region Mutual Exclusivity Tests

	[Fact]
	public void OnGet_WithValue_ClearsCallback()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		// First configure with callback
		knockOff.Name.Get(() => "from callback");
		Assert.Equal("from callback", service.Name);

		// Then configure with value - should override callback
		knockOff.Name.Get("from value");
		Assert.Equal("from value", service.Name);
	}

	[Fact]
	public void OnGet_WithCallback_ClearsValue()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		// First configure with value
		knockOff.Name.Get("from value");
		Assert.Equal("from value", service.Name);

		// Then configure with callback - should override value
		knockOff.Name.Get(() => "from callback");
		Assert.Equal("from callback", service.Name);
	}

	#endregion

	#region Sequence Tests

	[Fact]
	public void OnGet_WithValue_ThenGet_CreatesSequence()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		knockOff.Name.Get("first").ThenGet("second");

		Assert.Equal("first", service.Name);
		Assert.Equal("second", service.Name);
	}

	#endregion

	#region Test Stubs

	public interface IPropertyTest
	{
		string? Name { get; set; }
		int Count { get; set; }
		bool IsEnabled { get; set; }
	}

	[KnockOff]
	public partial class PropertyTestKnockOff : IPropertyTest
	{
	}

	#endregion
}
