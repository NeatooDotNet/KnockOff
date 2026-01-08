using KnockOff.Documentation.Samples.Guides.InlineStubs;

namespace KnockOff.Documentation.Samples.Tests.Guides;

/// <summary>
/// Tests for docs/guides/inline-stubs.md samples.
/// Verifies all code snippets compile and work as documented.
/// </summary>
[Trait("Category", "Documentation")]
[Trait("Category", "InlineStubs")]
public class InlineStubsSamplesTests : SamplesTestBase
{
	// ========================================================================
	// docs:inline-stubs:basic-example
	// ========================================================================

	[Fact]
	public void InlineStub_BasicExample_StubsClassGenerated()
	{
		// Verify Stubs class exists
		var stub = new UserServiceTests.Stubs.IInUserService();
		Assert.NotNull(stub);
	}

	[Fact]
	public void InlineStub_BasicUsage_OnCallAndVerification()
	{
		var stub = new UserServiceTests.Stubs.IInUserService();

		// Configure behavior
		stub.GetUser.OnCall = (ko, id) => new InUser { Id = id, Name = "Test" };

		// Use the stub
		IInUserService service = stub;
		var user = service.GetUser(42);

		// Verify
		Assert.NotNull(user);
		Assert.Equal(42, user.Id);
		Assert.Equal("Test", user.Name);
		Assert.True(stub.GetUser.WasCalled);
		Assert.Equal(42, stub.GetUser.LastCallArg);
	}

	// ========================================================================
	// docs:inline-stubs:multiple-interfaces
	// ========================================================================

	[Fact]
	public void InlineStub_MultipleInterfaces_GeneratesSeparateStubs()
	{
		var userStub = new MultiServiceTests.Stubs.IInUserService();
		var loggerStub = new MultiServiceTests.Stubs.IInLogger();
		var repoStub = new MultiServiceTests.Stubs.IInRepository();

		Assert.NotNull(userStub);
		Assert.NotNull(loggerStub);
		Assert.NotNull(repoStub);
	}

	[Fact]
	public void InlineStub_MultipleInterfaces_IndependentTracking()
	{
		var userStub = new MultiServiceTests.Stubs.IInUserService();
		var loggerStub = new MultiServiceTests.Stubs.IInLogger();

		IInUserService userService = userStub;
		IInLogger logger = loggerStub;

		userService.SaveUser(new InUser { Id = 1 });
		logger.Log("test");

		Assert.True(userStub.SaveUser.WasCalled);
		Assert.True(loggerStub.Log.WasCalled);
		Assert.Equal("test", loggerStub.Log.LastCallArg);
	}

	// ========================================================================
	// docs:inline-stubs:partial-property (NET9+)
	// ========================================================================

#if NET9_0_OR_GREATER
	[Fact]
	public void InlineStub_PartialProperty_AutoInstantiated()
	{
		var tests = new PartialPropertyTests();

		// Partial properties are auto-instantiated
		Assert.NotNull(tests.UserService);

		// Can configure and use directly
		tests.UserService.GetUser.OnCall = (ko, id) => new InUser { Id = id };

		IInUserService service = tests.UserService;
		var user = service.GetUser(99);

		Assert.Equal(99, user?.Id);
	}
#endif

	// ========================================================================
	// docs:inline-stubs:direct-instantiation
	// ========================================================================

	[Fact]
	public void InlineStub_DirectInstantiation_WorksOnAllVersions()
	{
		// Direct instantiation works without partial properties
		var userService = new DirectInstantiationTests.Stubs.IInUserService();

		userService.ConnectionString.Value = "Server=test";

		IInUserService service = userService;
		Assert.Equal("Server=test", service.ConnectionString);
	}

	// ========================================================================
	// docs:inline-stubs:nested-stubs
	// ========================================================================

	[Fact]
	public void InlineStub_NestedStubs_CanWireInterfaces()
	{
		var store = new PropertyStoreKnockOff();
		var propStub = new PropertyStoreKnockOff.Stubs.IInPropertyInfo();

		// Configure nested stub
		propStub.Name.Value = "TestProp";
		propStub.Value.Value = 42;

		// Wire nested stub to indexer (indexer named by parameter type: Int32Indexer)
		store.IInPropertyStore.Int32Indexer.OnGet = (ko, index) => propStub;

		// Verify wiring works
		IInPropertyStore service = store;
		Assert.Equal("TestProp", service[0].Name);
		Assert.Equal(42, service[0].Value);
	}

	// ========================================================================
	// docs:inline-stubs:handler-api
	// ========================================================================

	[Fact]
	public void InlineStub_HandlerApi_MethodTracking()
	{
		var stub = new UserServiceTests.Stubs.IInUserService();
		IInUserService service = stub;

		service.SaveUser(new InUser { Id = 1 });
		service.SaveUser(new InUser { Id = 2 });

		Assert.Equal(2, stub.SaveUser.CallCount);
		Assert.True(stub.SaveUser.WasCalled);
		Assert.Equal(2, stub.SaveUser.LastCallArg?.Id);
	}

	[Fact]
	public void InlineStub_HandlerApi_PropertyTracking()
	{
		var stub = new UserServiceTests.Stubs.IInUserService();
		IInUserService service = stub;

		_ = service.ConnectionString;
		_ = service.ConnectionString;
		service.ConnectionString = "NewValue";

		Assert.Equal(2, stub.ConnectionString.GetCount);
		Assert.Equal(1, stub.ConnectionString.SetCount);
		Assert.Equal("NewValue", stub.ConnectionString.LastSetValue);
	}

	// ========================================================================
	// docs:inline-stubs:test-isolation-reset
	// ========================================================================

	[Fact]
	public void InlineStub_Reset_ClearsAllState()
	{
		var stub = new UserServiceTests.Stubs.IInUserService();
		stub.GetUser.OnCall = (ko, id) => new InUser { Id = id };

		IInUserService service = stub;
		service.GetUser(1);
		service.GetUser(2);

		// Reset clears everything
		stub.GetUser.Reset();

		Assert.Equal(0, stub.GetUser.CallCount);
		Assert.False(stub.GetUser.WasCalled);
		Assert.Null(stub.GetUser.OnCall);
		Assert.Null(stub.GetUser.LastCallArg);
	}
}
