namespace KnockOff.Tests;

/// <summary>
/// Tests for assembly-wide strict mode behavior.
/// Note: This file does NOT have [assembly: KnockOffStrict] - it tests opt-in scenarios
/// and verifies the feature works when assembly attribute is present elsewhere.
/// </summary>
public class AssemblyStrictModeTests
{
	#region Precedence Tests - Attribute Overrides Assembly Default

	[Fact]
	public void AttributeStrictFalse_OverridesAssemblyDefault_ForStandalone()
	{
		// [KnockOff(Strict = false)] should override [assembly: KnockOffStrict]
		// StrictFalseOverrideStub has Strict = false on attribute
		var stub = new StrictFalseOverrideStub();
		IAssemblyStrictTest service = stub;

		// Should NOT throw - attribute says Strict = false
		var result = service.GetValue(42);

		Assert.Equal(0, result); // Default value, no throw
	}

	[Fact]
	public void AttributeStrictFalse_OverridesAssemblyDefault_ForInline()
	{
		// [KnockOff<T>(Strict = false)] should override [assembly: KnockOffStrict]
		var stub = new AssemblyStrictOverrideInlineTests.Stubs.IStrictFalseOverride();
		IStrictFalseOverride service = stub;

		// Should NOT throw - attribute says Strict = false
		var result = service.GetValue(42);

		Assert.Equal(0, result);
	}

	[Fact]
	public void ConstructorStrictFalse_OverridesAttributeStrict_ForInline()
	{
		// Constructor parameter should override attribute setting
		var stub = new AssemblyStrictOverrideInlineTests.Stubs.IStrictTrueService(strict: false);
		IStrictTrueService service = stub;

		// Should NOT throw - constructor override takes priority
		var result = service.GetValue(42);

		Assert.Equal(0, result);
	}

	[Fact]
	public void ConstructorStrictTrue_OverridesAttributeStrictFalse_ForInline()
	{
		// Constructor can also enable strict when attribute disables it
		var stub = new AssemblyStrictOverrideInlineTests.Stubs.IStrictFalseOverride(strict: true);
		IStrictFalseOverride service = stub;

		// Should throw - constructor override takes priority
		var ex = Assert.Throws<StubException>(() => service.GetValue(42));
		Assert.Contains("GetValue", ex.Message);
	}

	[Fact]
	public void StrictPropertySet_OverridesEverything_AtRuntime()
	{
		// Runtime property assignment should always work
		var stub = new AssemblyStrictOverrideInlineTests.Stubs.IStrictFalseOverride(strict: false);
		IStrictFalseOverride service = stub;

		// Initially not strict
		var result = service.GetValue(1);
		Assert.Equal(0, result);

		// Change at runtime
		stub.Strict = true;

		// Now should throw
		Assert.Throws<StubException>(() => service.GetValue(2));
	}

	#endregion

	#region Integration Tests - Assembly Without Attribute

	[Fact]
	public void WithoutAssemblyAttribute_StandaloneStub_DefaultsToFalse()
	{
		// This test assembly doesn't have [assembly: KnockOffStrict]
		// So default behavior should be non-strict
		var stub = new AssemblyDefaultTestStub();
		IAssemblyDefaultTest service = stub;

		// Should NOT throw - no assembly attribute, no stub attribute
		var result = service.GetValue(42);

		Assert.Equal(0, result);
	}

	[Fact]
	public void WithoutAssemblyAttribute_InlineStub_DefaultsToFalse()
	{
		var stub = new AssemblyDefaultInlineTests.Stubs.IAssemblyDefaultTest();
		IAssemblyDefaultTest service = stub;

		// Should NOT throw
		var result = service.GetValue(42);

		Assert.Equal(0, result);
	}

	[Fact]
	public void WithoutAssemblyAttribute_AttributeStrictTrue_StillWorks()
	{
		// [KnockOff(Strict = true)] without assembly attribute should still make stub strict
		var stub = new StrictByAttributeOnlyStub();
		IAssemblyDefaultTest service = stub;

		// Should throw - attribute says Strict = true
		Assert.Throws<StubException>(() => service.GetValue(42));
	}

	#endregion

	#region Fluent API Tests

	[Fact]
	public void StrictExtension_Works_WithAssemblyOverride()
	{
		var stub = new AssemblyStrictOverrideInlineTests.Stubs.IStrictFalseOverride(strict: false);
		IStrictFalseOverride service = stub;

		// Start non-strict
		var result = service.GetValue(1);
		Assert.Equal(0, result);

		// Use fluent API to enable
		stub.Strict();

		// Now strict
		Assert.Throws<StubException>(() => service.GetValue(2));
	}

	[Fact]
	public void FluentChaining_WithConfiguration_Works()
	{
		var stub = new AssemblyStrictOverrideInlineTests.Stubs.IStrictFalseOverride(strict: false).Strict();
		stub.GetValue.OnCall((x) => x * 2);
		IStrictFalseOverride service = stub;

		// Should work - OnCall is configured
		var result = service.GetValue(5);
		Assert.Equal(10, result);
	}

	#endregion
}

#region Test Interfaces

public interface IAssemblyStrictTest
{
	int GetValue(int x);
	void DoSomething();
}

public interface IAssemblyDefaultTest
{
	int GetValue(int x);
}

public interface IStrictFalseOverride
{
	int GetValue(int x);
}

public interface IStrictTrueService
{
	int GetValue(int x);
}

#endregion

#region Standalone Stubs

/// <summary>
/// Stub that explicitly opts out of strict mode.
/// Tests that [KnockOff(Strict = false)] overrides [assembly: KnockOffStrict].
/// </summary>
[KnockOff(Strict = false)]
public partial class StrictFalseOverrideStub : IAssemblyStrictTest
{
}

/// <summary>
/// Stub with no Strict setting - uses assembly or default.
/// </summary>
[KnockOff]
public partial class AssemblyDefaultTestStub : IAssemblyDefaultTest
{
}

/// <summary>
/// Stub with Strict = true on attribute (no assembly attribute needed).
/// </summary>
[KnockOff(Strict = true)]
public partial class StrictByAttributeOnlyStub : IAssemblyDefaultTest
{
}

#endregion

#region Inline Stub Test Classes

/// <summary>
/// Test class for inline stubs with override scenarios.
/// </summary>
[KnockOff<IStrictFalseOverride>(Strict = false)]
[KnockOff<IStrictTrueService>(Strict = true)]
public partial class AssemblyStrictOverrideInlineTests
{
}

/// <summary>
/// Test class for inline stubs without any Strict settings.
/// </summary>
[KnockOff<IAssemblyDefaultTest>]
public partial class AssemblyDefaultInlineTests
{
}

#endregion
