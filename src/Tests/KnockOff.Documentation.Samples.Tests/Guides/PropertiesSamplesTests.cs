using KnockOff.Documentation.Samples.Guides;

namespace KnockOff.Documentation.Samples.Tests.Guides;

/// <summary>
/// Tests for docs/guides/properties.md samples.
/// </summary>
[Trait("Category", "Documentation")]
[Trait("Category", "Guides")]
public class PropertiesSamplesTests
{
	// ========================================================================
	// Set-Only Property
	// ========================================================================

	[Fact]
	public void SetOnlyProperty_TracksSetCallsAndValue()
	{
		var knockOff = new PropLoggerKnockOff();
		IPropLogger logger = knockOff;

		// Set only has OnSet, SetCount, LastSetValue (no getter/Value)
		logger.Output = "First message";
		logger.Output = "Second message";

		Assert.Equal(2, knockOff.Output.SetCount);
		Assert.Equal("Second message", knockOff.Output.LastSetValue);
	}

	[Fact]
	public void SetOnlyProperty_OnSetCallback()
	{
		var knockOff = new PropLoggerKnockOff();
		IPropLogger logger = knockOff;

		var captured = new List<string>();
		knockOff.Output.OnSet = (ko, value) => captured.Add(value);

		logger.Output = "Log 1";
		logger.Output = "Log 2";
		logger.Output = "Log 3";

		Assert.Equal(["Log 1", "Log 2", "Log 3"], captured);
	}
}
