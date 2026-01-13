namespace KnockOff.Tests;

/// <summary>
/// Tests for strict mode behavior - throwing StubException for unconfigured calls.
/// </summary>
public class StrictModeTests
{
	#region Standalone Stub Tests

	[Fact]
	public void StandaloneStub_NonStrict_Method_ReturnsDefault()
	{
		var stub = new StrictModeTestStub();
		IStrictModeTest service = stub;

		// Non-strict (default) - returns default value
		var result = service.GetValue(42);

		Assert.Equal(0, result);
	}

	[Fact]
	public void StandaloneStub_Strict_Method_ThrowsStubException()
	{
		var stub = new StrictModeTestStub().Strict();
		IStrictModeTest service = stub;

		// Strict mode - throws StubException
		var ex = Assert.Throws<StubException>(() => service.GetValue(42));
		Assert.Contains("GetValue", ex.Message);
	}

	[Fact]
	public void StandaloneStub_Strict_VoidMethod_ThrowsStubException()
	{
		var stub = new StrictModeTestStub().Strict();
		IStrictModeTest service = stub;

		var ex = Assert.Throws<StubException>(() => service.DoSomething());
		Assert.Contains("DoSomething", ex.Message);
	}

	[Fact]
	public void StandaloneStub_Strict_PropertyGetter_ThrowsStubException()
	{
		var stub = new StrictModeTestStub().Strict();
		IStrictModeTest service = stub;

		var ex = Assert.Throws<StubException>(() => _ = service.Name);
		Assert.Contains("Name", ex.Message);
	}

	[Fact]
	public void StandaloneStub_Strict_PropertySetter_ThrowsStubException()
	{
		var stub = new StrictModeTestStub().Strict();
		IStrictModeTest service = stub;

		var ex = Assert.Throws<StubException>(() => service.Name = "test");
		Assert.Contains("Name", ex.Message);
	}

	[Fact]
	public void StandaloneStub_Strict_WithOnCall_DoesNotThrow()
	{
		var stub = new StrictModeTestStub().Strict();
		stub.GetValue.OnCall = (ko, x) => x * 2;
		IStrictModeTest service = stub;

		// Callback configured - should work even in strict mode
		var result = service.GetValue(5);

		Assert.Equal(10, result);
	}

	[Fact]
	public void StandaloneStub_Strict_WithOnGet_DoesNotThrow()
	{
		var stub = new StrictModeTestStub().Strict();
		stub.Name.OnGet = ko => "configured";
		IStrictModeTest service = stub;

		var result = service.Name;

		Assert.Equal("configured", result);
	}

	[Fact]
	public void StandaloneStub_DefaultStrict_UsesAttributeValue()
	{
		// StrictByDefaultStub has [KnockOff(Strict = true)]
		var stub = new StrictByDefaultStub();
		IStrictModeTest service = stub;

		// Should throw because strict is true by default from attribute
		Assert.Throws<StubException>(() => service.GetValue(1));
	}

	[Fact]
	public void StandaloneStub_DefaultStrict_CanBeDisabled()
	{
		var stub = new StrictByDefaultStub();
		stub.Strict = false; // Override the default
		IStrictModeTest service = stub;

		// Should not throw - strict disabled
		var result = service.GetValue(1);
		Assert.Equal(0, result);
	}

	#endregion

	#region Inline Stub Tests

	[Fact]
	public void InlineStub_NonStrict_Method_ReturnsDefault()
	{
		var stub = new StrictModeInlineTests.Stubs.IStrictModeTest();
		IStrictModeTest service = stub;

		var result = service.GetValue(42);

		Assert.Equal(0, result);
	}

	[Fact]
	public void InlineStub_Strict_Method_ThrowsStubException()
	{
		var stub = new StrictModeInlineTests.Stubs.IStrictModeTest(strict: true);
		IStrictModeTest service = stub;

		var ex = Assert.Throws<StubException>(() => service.GetValue(42));
		Assert.Contains("GetValue", ex.Message);
	}

	[Fact]
	public void InlineStub_Strict_VoidMethod_ThrowsStubException()
	{
		var stub = new StrictModeInlineTests.Stubs.IStrictModeTest(strict: true);
		IStrictModeTest service = stub;

		var ex = Assert.Throws<StubException>(() => service.DoSomething());
		Assert.Contains("DoSomething", ex.Message);
	}

	[Fact]
	public void InlineStub_Strict_PropertyGetter_ThrowsStubException()
	{
		var stub = new StrictModeInlineTests.Stubs.IStrictModeTest(strict: true);
		IStrictModeTest service = stub;

		var ex = Assert.Throws<StubException>(() => _ = service.Name);
		Assert.Contains("Name", ex.Message);
	}

	[Fact]
	public void InlineStub_Strict_PropertySetter_ThrowsStubException()
	{
		var stub = new StrictModeInlineTests.Stubs.IStrictModeTest(strict: true);
		IStrictModeTest service = stub;

		var ex = Assert.Throws<StubException>(() => service.Name = "test");
		Assert.Contains("Name", ex.Message);
	}

	[Fact]
	public void InlineStub_Strict_WithOnCall_DoesNotThrow()
	{
		var stub = new StrictModeInlineTests.Stubs.IStrictModeTest(strict: true);
		stub.GetValue.OnCall = (ko, x) => x * 2;
		IStrictModeTest service = stub;

		var result = service.GetValue(5);

		Assert.Equal(10, result);
	}

	[Fact]
	public void InlineStub_StrictByDefault_ThrowsWithoutConfiguration()
	{
		// IStrictByDefault has [KnockOff<T>(Strict = true)]
		var stub = new StrictModeInlineTests.Stubs.IStrictByDefault();
		IStrictByDefault service = stub;

		Assert.Throws<StubException>(() => service.GetData());
	}

	[Fact]
	public void InlineStub_StrictByDefault_CanBeOverriddenToNonStrict()
	{
		var stub = new StrictModeInlineTests.Stubs.IStrictByDefault(strict: false);
		IStrictByDefault service = stub;

		// Should not throw - strict overridden to false
		var result = service.GetData();
		Assert.Equal(0, result);
	}

	#endregion

	#region Extension Method Tests

	[Fact]
	public void StrictExtension_StandaloneStub_EnablesStrictMode()
	{
		var stub = new StrictModeTestStub().Strict();
		IStrictModeTest service = stub;

		Assert.True(stub.Strict);
		Assert.Throws<StubException>(() => service.GetValue(42));
	}

	[Fact]
	public void StrictExtension_StandaloneStub_ReturnsSameInstance()
	{
		var stub = new StrictModeTestStub();
		var result = stub.Strict();

		Assert.Same(stub, result);
	}

	[Fact]
	public void StrictExtension_InlineStub_EnablesStrictMode()
	{
		var stub = new StrictModeInlineTests.Stubs.IStrictModeTest().Strict();
		IStrictModeTest service = stub;

		Assert.True(stub.Strict);
		Assert.Throws<StubException>(() => service.GetValue(42));
	}

	[Fact]
	public void StrictExtension_InlineStub_ReturnsSameInstance()
	{
		var stub = new StrictModeInlineTests.Stubs.IStrictModeTest();
		var result = stub.Strict();

		Assert.Same(stub, result);
	}

	[Fact]
	public void StrictExtension_FluentChaining_Works()
	{
		// Test that .Strict() can be used in a fluent chain
		var stub = new StrictModeTestStub().Strict();
		stub.GetValue.OnCall = (ko, x) => x * 2;
		IStrictModeTest service = stub;

		// Should not throw because OnCall is configured
		var result = service.GetValue(5);
		Assert.Equal(10, result);

		// But unconfigured members should throw
		Assert.Throws<StubException>(() => service.DoSomething());
	}

	[Fact]
	public void IKnockOffStub_StandaloneStub_ImplementsInterface()
	{
		var stub = new StrictModeTestStub();
		Assert.IsAssignableFrom<IKnockOffStub>(stub);
	}

	[Fact]
	public void IKnockOffStub_InlineStub_ImplementsInterface()
	{
		var stub = new StrictModeInlineTests.Stubs.IStrictModeTest();
		Assert.IsAssignableFrom<IKnockOffStub>(stub);
	}

	#endregion
}

#region Test Interfaces

public interface IStrictModeTest
{
	string Name { get; set; }
	int GetValue(int x);
	void DoSomething();
}

public interface IStrictByDefault
{
	int GetData();
}

#endregion

#region Standalone Stubs

[KnockOff]
public partial class StrictModeTestStub : IStrictModeTest
{
}

[KnockOff(Strict = true)]
public partial class StrictByDefaultStub : IStrictModeTest
{
}

#endregion

#region Inline Stub Test Classes

[KnockOff<IStrictModeTest>]
[KnockOff<IStrictByDefault>(Strict = true)]
public partial class StrictModeInlineTests
{
}

#endregion
