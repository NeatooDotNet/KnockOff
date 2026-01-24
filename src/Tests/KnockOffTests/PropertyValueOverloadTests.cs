namespace KnockOff.Tests;

/// <summary>
/// Tests for OnGet(value) property value overloads.
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

		var tracking = knockOff.Name.OnGet("configured value");

		var result = service.Name;

		Assert.Equal("configured value", result);
		tracking.Verify(Times.Once);
	}

	[Fact]
	public void OnGet_WithValue_ReturnsNullWhenConfiguredNull()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		var tracking = knockOff.Name.OnGet((string?)null);

		var result = service.Name;

		Assert.Null(result);
		tracking.Verify(Times.Once);
	}

	[Fact]
	public void OnGet_WithValue_RepeatsIndefinitely()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		knockOff.Name.OnGet("repeated");

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

		knockOff.Count.OnGet(42);

		Assert.Equal(42, service.Count);
	}

	[Fact]
	public void OnGet_WithBoolValue_ReturnsConfiguredValue()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		knockOff.IsEnabled.OnGet(true);

		Assert.True(service.IsEnabled);
	}

	[Fact]
	public void OnGet_WithDefaultValue_ReturnsDefault()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		knockOff.Count.OnGet(0);

		Assert.Equal(0, service.Count);
	}

	#endregion

	#region Tracking Tests

	[Fact]
	public void OnGet_WithValue_ReturnsTrackingInterface()
	{
		var knockOff = new PropertyTestKnockOff();

		var tracking = knockOff.Name.OnGet("test");

		Assert.NotNull(tracking);
		Assert.IsAssignableFrom<IPropertyGetTracking>(tracking);
	}

	[Fact]
	public void OnGet_WithValue_SupportsVerifiable()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		var tracking = knockOff.Name.OnGet("test");

		_ = service.Name;

		tracking.Verify(Times.Once);
	}

	[Fact]
	public void OnGet_WithValue_SupportsVerifiableWithTimes()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		var tracking = knockOff.Name.OnGet("test");

		_ = service.Name;
		_ = service.Name;

		tracking.Verify(Times.Exactly(2));
	}

	#endregion

	#region Mutual Exclusivity Tests

	[Fact]
	public void OnGet_WithValue_ClearsCallback()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		// First configure with callback
		knockOff.Name.OnGet(() => "from callback");
		Assert.Equal("from callback", service.Name);

		// Then configure with value - should override callback
		knockOff.Name.OnGet("from value");
		Assert.Equal("from value", service.Name);
	}

	[Fact]
	public void OnGet_WithCallback_ClearsValue()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		// First configure with value
		knockOff.Name.OnGet("from value");
		Assert.Equal("from value", service.Name);

		// Then configure with callback - should override value
		knockOff.Name.OnGet(() => "from callback");
		Assert.Equal("from callback", service.Name);
	}

	#endregion

	#region Sequence Tests

	[Fact]
	public void OnGetSequence_WithValue_StartsSequence()
	{
		var knockOff = new PropertyTestKnockOff();
		IPropertyTest service = knockOff;

		knockOff.Name.OnGetSequence("first");

		Assert.Equal("first", service.Name);
	}

	// Note: ThenGet(value) chaining requires interface changes (Phase 5)
	// For now, use lambda syntax for chaining: .ThenGet(() => "value")

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
