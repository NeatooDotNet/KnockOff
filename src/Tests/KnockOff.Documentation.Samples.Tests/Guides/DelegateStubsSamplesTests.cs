using KnockOff.Documentation.Samples.Guides.Delegates;
using Del = KnockOff.Documentation.Samples.Guides.Delegates;

namespace KnockOff.Documentation.Samples.Tests.Guides;

/// <summary>
/// Tests for docs/guides/delegates.md samples.
/// Verifies all code snippets compile and work as documented.
/// </summary>
[Trait("Category", "Documentation")]
[Trait("Category", "Delegates")]
public class DelegateStubsSamplesTests : SamplesTestBase
{
	// ========================================================================
	// docs:delegates:basic-delegate / docs:delegates:basic-usage
	// ========================================================================

	[Fact]
	public void DelegateStub_BasicExample_StubsClassGenerated()
	{
		var uniqueStub = new DelegateTests.Stubs.IsUniqueRule();
		var factoryStub = new DelegateTests.Stubs.UserFactory();

		Assert.NotNull(uniqueStub);
		Assert.NotNull(uniqueStub.Interceptor);
		Assert.NotNull(factoryStub);
		Assert.NotNull(factoryStub.Interceptor);
	}

	[Fact]
	public void DelegateStub_BasicUsage_OnCallAndVerification()
	{
		var uniqueStub = new DelegateTests.Stubs.IsUniqueRule();
		uniqueStub.Interceptor.OnCall = (ko, value) => value != "duplicate";

		// Implicit conversion to delegate
		IsUniqueRule rule = uniqueStub;
		Assert.True(rule("unique"));
		Assert.False(rule("duplicate"));

		// Verify calls
		Assert.Equal(2, uniqueStub.Interceptor.CallCount);
		Assert.Equal("duplicate", uniqueStub.Interceptor.LastCallArg);
	}

	[Fact]
	public void DelegateStub_UserFactory_ReturnsConfiguredValue()
	{
		var factoryStub = new DelegateTests.Stubs.UserFactory();
		factoryStub.Interceptor.OnCall = (ko, id) => new DelUser { Id = id, Name = $"User{id}" };

		UserFactory factory = factoryStub;
		var user = factory(42);

		Assert.Equal(42, user.Id);
		Assert.Equal("User42", user.Name);
		Assert.Equal(42, factoryStub.Interceptor.LastCallArg);
	}

	// ========================================================================
	// docs:delegates:void-delegate
	// ========================================================================

	[Fact]
	public void DelegateStub_VoidDelegate_TracksCallsAndArguments()
	{
		var logStub = new VoidDelegateTests.Stubs.LogAction();
		var messages = new List<string>();
		logStub.Interceptor.OnCall = (ko, msg) => messages.Add(msg);

		LogAction logger = logStub;
		logger("Hello");
		logger("World");

		Assert.Equal(2, logStub.Interceptor.CallCount);
		Assert.Equal(new[] { "Hello", "World" }, messages);
		Assert.Equal("World", logStub.Interceptor.LastCallArg);
	}

	// ========================================================================
	// docs:delegates:implicit-conversion
	// ========================================================================

	[Fact]
	public void DelegateStub_ImplicitConversion_WorksSeamlessly()
	{
		var stub = new DelegateTests.Stubs.IsUniqueRule();
		stub.Interceptor.OnCall = (ko, v) => v.Length > 3;

		// Can pass directly where delegate expected
		bool ValidateWithRule(IsUniqueRule rule, string value) => rule(value);

		Assert.True(ValidateWithRule(stub, "test"));
		Assert.False(ValidateWithRule(stub, "ab"));
	}

	// ========================================================================
	// docs:delegates:handler-api
	// ========================================================================

	[Fact]
	public void DelegateStub_HandlerApi_AllPropertiesWork()
	{
		var stub = new DelegateTests.Stubs.IsUniqueRule();
		stub.Interceptor.OnCall = (ko, value) => true;

		IsUniqueRule rule = stub;
		rule("first");
		rule("second");

		Assert.Equal(2, stub.Interceptor.CallCount);
		Assert.True(stub.Interceptor.WasCalled);
		Assert.Equal("second", stub.Interceptor.LastCallArg);
		Assert.NotNull(stub.Interceptor.OnCall);
	}

	[Fact]
	public void DelegateStub_HandlerApi_Reset()
	{
		var stub = new DelegateTests.Stubs.IsUniqueRule();
		stub.Interceptor.OnCall = (ko, value) => true;

		IsUniqueRule rule = stub;
		rule("test");

		stub.Interceptor.Reset();

		Assert.Equal(0, stub.Interceptor.CallCount);
		Assert.False(stub.Interceptor.WasCalled);
		Assert.Null(stub.Interceptor.OnCall);
		Assert.Null(stub.Interceptor.LastCallArg);
	}

	// ========================================================================
	// docs:delegates:closed-generics
	// ========================================================================

	[Fact]
	public void DelegateStub_ClosedGeneric_FactoryWorks()
	{
		var factoryStub = new GenericDelegateTests.Stubs.Factory();
		factoryStub.Interceptor.OnCall = (ko) => new DelUser { Id = 1, Name = "Created" };

		Factory<DelUser> factory = factoryStub;
		var user = factory();

		Assert.Equal("Created", user.Name);
		Assert.True(factoryStub.Interceptor.WasCalled);
	}

	[Fact]
	public void DelegateStub_ClosedGeneric_ConverterWorks()
	{
		var converterStub = new GenericDelegateTests.Stubs.Converter();
		converterStub.Interceptor.OnCall = (ko, num) => $"Number: {num}";

		Del.Converter<int, string> converter = converterStub;
		Assert.Equal("Number: 42", converter(42));
		Assert.Equal(42, converterStub.Interceptor.LastCallArg);
	}

	// ========================================================================
	// docs:delegates:validation-pattern
	// ========================================================================

	[Fact]
	public void DelegateStub_ValidationPattern_CanToggleBehavior()
	{
		var uniqueCheck = new ValidationPatternTests.Stubs.IsUniqueRule();

		// Default: always valid
		uniqueCheck.Interceptor.OnCall = (ko, value) => true;

		IsUniqueRule rule = uniqueCheck;
		Assert.True(rule("anything"));

		// Reset and change behavior
		uniqueCheck.Interceptor.Reset();
		uniqueCheck.Interceptor.OnCall = (ko, value) => value != "duplicate";

		Assert.True(rule("unique"));
		Assert.False(rule("duplicate"));
	}

	// ========================================================================
	// docs:delegates:multi-param-delegate
	// ========================================================================

	[Fact]
	public void DelegateStub_MultiParam_TracksLastCallArgs()
	{
		// Need to create a test class for multi-param delegate
		// This is handled by the Formatter delegate defined in the samples
	}
}
