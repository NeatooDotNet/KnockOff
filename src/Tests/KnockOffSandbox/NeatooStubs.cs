namespace KnockOff.Sandbox;

/// <summary>
/// Test interfaces for verifying KnockOff fixes.
/// Note: Cannot use Neatoo.IEntityBase directly because it has internal members.
/// KnockOff cannot stub interfaces with internal members from external assemblies.
/// </summary>

// Test interface with out parameters
public interface IOutParameterService
{
	bool TryGetValue(string key, out string? value);
	bool TryParse(string input, out int result);
	void GetData(out string name, out int count);
}

// Test interface with ref parameters
public interface IRefParameterService
{
	void Increment(ref int value);
	bool TryUpdate(string key, ref string value);
}

// Test interface with method overloads (different parameter counts)
public interface IOverloadedService
{
	string Format(string input);
	string Format(string input, bool uppercase);
	string Format(string input, int maxLength);
}

[KnockOff]
public partial class OutParameterServiceKnockOff : IOutParameterService
{
}

[KnockOff]
public partial class RefParameterServiceKnockOff : IRefParameterService
{
}

[KnockOff]
public partial class OverloadedServiceKnockOff : IOverloadedService
{
}
