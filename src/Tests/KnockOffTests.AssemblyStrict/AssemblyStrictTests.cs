using KnockOff;

namespace KnockOff.Tests.AssemblyStrict;

/// <summary>
/// Tests that verify assembly-wide strict mode works correctly.
/// This project has [assembly: KnockOffStrict] in AssemblyInfo.cs.
/// </summary>
public class AssemblyStrictTests
{
	#region Assembly Default Tests - Stubs Are Strict By Default

	[Fact]
	public void StandaloneStub_NoAttribute_DefaultsToStrict()
	{
		// This assembly has [assembly: KnockOffStrict]
		// So stubs without explicit Strict setting should be strict by default
		var stub = new DefaultStrictStub();
		IDefaultStrictTest service = stub;

		// Should throw - assembly default is strict
		var ex = Assert.Throws<StubException>(() => service.GetValue(42));
		Assert.Contains("GetValue", ex.Message);
	}

	[Fact]
	public void StandaloneStub_NoAttribute_VoidMethod_ThrowsStubException()
	{
		var stub = new DefaultStrictStub();
		IDefaultStrictTest service = stub;

		var ex = Assert.Throws<StubException>(() => service.DoSomething());
		Assert.Contains("DoSomething", ex.Message);
	}

	[Fact]
	public void StandaloneStub_NoAttribute_Property_ThrowsStubException()
	{
		var stub = new DefaultStrictStub();
		IDefaultStrictTest service = stub;

		var ex = Assert.Throws<StubException>(() => _ = service.Name);
		Assert.Contains("Name", ex.Message);
	}

	[Fact]
	public void InlineStub_NoAttribute_DefaultsToStrict()
	{
		// Inline stub without Strict setting should be strict by default
		var stub = new DefaultStrictInlineTests.Stubs.IDefaultStrictTest();
		IDefaultStrictTest service = stub;

		// Should throw - assembly default is strict
		Assert.Throws<StubException>(() => service.GetValue(42));
	}

	[Fact]
	public void InlineStub_Constructor_DefaultsToTrue()
	{
		// Constructor without explicit parameter should default to true
		var stub = new DefaultStrictInlineTests.Stubs.IDefaultStrictTest();

		Assert.True(stub.Strict);
	}

	#endregion

	#region Override Tests - Attribute Overrides Assembly

	[Fact]
	public void StandaloneStub_StrictFalse_OverridesAssemblyDefault()
	{
		// [KnockOff(Strict = false)] should override [assembly: KnockOffStrict]
		var stub = new OptOutStrictStub();
		IDefaultStrictTest service = stub;

		// Should NOT throw - attribute says Strict = false
		var result = service.GetValue(42);

		Assert.Equal(0, result);
	}

	[Fact]
	public void InlineStub_StrictFalse_OverridesAssemblyDefault()
	{
		// [KnockOff<T>(Strict = false)] should override [assembly: KnockOffStrict]
		var stub = new OptOutStrictInlineTests.Stubs.IOptOutTest();
		IOptOutTest service = stub;

		// Should NOT throw - attribute says Strict = false
		var result = service.GetValue(42);

		Assert.Equal(0, result);
	}

	[Fact]
	public void InlineStub_Constructor_CanOverrideToFalse()
	{
		// Constructor parameter should override assembly default
		var stub = new DefaultStrictInlineTests.Stubs.IDefaultStrictTest(strict: false);
		IDefaultStrictTest service = stub;

		// Should NOT throw - constructor overrides
		var result = service.GetValue(42);

		Assert.Equal(0, result);
	}

	#endregion

	#region Configured Stubs - OnCall Works With Assembly Strict

	[Fact]
	public void StandaloneStub_WithOnCall_DoesNotThrow()
	{
		var stub = new DefaultStrictStub();
		stub.GetValue.Return((x) => x * 2);
		IDefaultStrictTest service = stub;

		// Callback configured - should work even with assembly strict
		var result = service.GetValue(5);

		Assert.Equal(10, result);
	}

	[Fact]
	public void InlineStub_WithOnCall_DoesNotThrow()
	{
		var stub = new DefaultStrictInlineTests.Stubs.IDefaultStrictTest();
		stub.GetValue.Return((x) => x * 3);
		IDefaultStrictTest service = stub;

		var result = service.GetValue(5);

		Assert.Equal(15, result);
	}

	[Fact]
	public void StandaloneStub_WithOnGet_DoesNotThrow()
	{
		var stub = new DefaultStrictStub();
		stub.Name.OnGet(() => "configured");
		IDefaultStrictTest service = stub;

		var result = service.Name;

		Assert.Equal("configured", result);
	}

	#endregion

	#region Mixed Scenarios - Some Configured, Some Not

	[Fact]
	public void MixedStub_ConfiguredMethodWorks_UnconfiguredThrows()
	{
		var stub = new DefaultStrictStub();
		stub.GetValue.Return((x) => x * 2);
		IDefaultStrictTest service = stub;

		// Configured method works
		var result = service.GetValue(5);
		Assert.Equal(10, result);

		// Unconfigured methods still throw
		Assert.Throws<StubException>(() => service.DoSomething());
		Assert.Throws<StubException>(() => _ = service.Name);
	}

	#endregion

	#region Fluent API Tests

	[Fact]
	public void StrictExtension_CanReEnableAfterDisable()
	{
		var stub = new OptOutStrictStub();
		IDefaultStrictTest service = stub;

		// Initially not strict (attribute override)
		var result = service.GetValue(1);
		Assert.Equal(0, result);

		// Re-enable with fluent API
		stub.Strict();

		// Now strict
		Assert.Throws<StubException>(() => service.GetValue(2));
	}

	[Fact]
	public void StrictProperty_CanToggleAtRuntime()
	{
		var stub = new DefaultStrictInlineTests.Stubs.IDefaultStrictTest();
		IDefaultStrictTest service = stub;

		// Default is strict (assembly default)
		Assert.True(stub.Strict);
		Assert.Throws<StubException>(() => service.GetValue(1));

		// Disable
		stub.Strict = false;
		var result = service.GetValue(2);
		Assert.Equal(0, result);

		// Re-enable
		stub.Strict = true;
		Assert.Throws<StubException>(() => service.GetValue(3));
	}

	#endregion
}

#region Test Interfaces

public interface IDefaultStrictTest
{
	string Name { get; set; }
	int GetValue(int x);
	void DoSomething();
}

public interface IOptOutTest
{
	int GetValue(int x);
}

#endregion

#region Standalone Stubs - Will Pick Up Assembly Default

/// <summary>
/// Stub with no Strict setting - should pick up [assembly: KnockOffStrict].
/// </summary>
[KnockOff]
public partial class DefaultStrictStub : IDefaultStrictTest
{
}

/// <summary>
/// Stub that explicitly opts out of strict mode.
/// </summary>
[KnockOff(Strict = false)]
public partial class OptOutStrictStub : IDefaultStrictTest
{
}

#endregion

#region Inline Stub Test Classes

/// <summary>
/// Test class with inline stubs that should pick up assembly default.
/// </summary>
[KnockOff<IDefaultStrictTest>]
public partial class DefaultStrictInlineTests
{
}

/// <summary>
/// Test class that explicitly opts out of strict mode.
/// </summary>
[KnockOff<IOptOutTest>(Strict = false)]
public partial class OptOutStrictInlineTests
{
}

#endregion
