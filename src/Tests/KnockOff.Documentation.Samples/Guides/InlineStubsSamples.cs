/// <summary>
/// Code samples for docs/guides/inline-stubs.md
///
/// Snippets in this file:
/// - docs:inline-stubs:basic-example
/// - docs:inline-stubs:basic-usage
/// - docs:inline-stubs:multiple-interfaces
/// - docs:inline-stubs:partial-property
/// - docs:inline-stubs:direct-instantiation
/// - docs:inline-stubs:nested-stubs-interfaces
/// - docs:inline-stubs:nested-stubs-usage
/// - docs:inline-stubs:interceptor-api
/// - docs:inline-stubs:test-isolation-reset
///
/// Corresponding tests: InlineStubsSamplesTests.cs
/// </summary>

namespace KnockOff.Documentation.Samples.Guides.InlineStubs;

// ============================================================================
// Domain Types for Inline Stubs Samples
// ============================================================================

public class InUser
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
}

public class InPropertyInfo
{
	public string Name { get; set; } = string.Empty;
	public object? Value { get; set; }
}

// ============================================================================
// Interfaces for Samples
// ============================================================================

public interface IInUserService
{
	InUser? GetUser(int id);
	void SaveUser(InUser user);
	string ConnectionString { get; set; }
}

public interface IInLogger
{
	void Log(string message);
	void LogError(string message, Exception? ex);
}

public interface IInRepository
{
	void Save(object entity);
	InUser? FindById(int id);
}

// ============================================================================
// Basic Inline Stub Example
// ============================================================================

#region docs:inline-stubs:basic-example
[KnockOff<IInUserService>]
public partial class UserServiceTests
{
	// The [KnockOff<IInUserService>] attribute generates:
	// - A nested Stubs class
	// - Stubs.IInUserService implementing the interface
	// - Interceptor properties for verification and callbacks
}
#endregion

#region docs:inline-stubs:basic-usage
// In your test method:
// var stub = new UserServiceTests.Stubs.IInUserService();
//
// // Configure behavior
// stub.GetUser.OnCall = (ko, id) => new InUser { Id = id, Name = "Test" };
//
// // Use the stub (implicit interface conversion)
// IInUserService service = stub;
// var user = service.GetUser(42);
//
// // Verify interactions
// Assert.True(stub.GetUser.WasCalled);
// Assert.Equal(42, stub.GetUser.LastCallArg);
#endregion

// ============================================================================
// Multiple Interfaces Example
// ============================================================================

#region docs:inline-stubs:multiple-interfaces
[KnockOff<IInUserService>]
[KnockOff<IInLogger>]
[KnockOff<IInRepository>]
public partial class MultiServiceTests
{
	// Each attribute generates a separate stub class:
	// - Stubs.IInUserService
	// - Stubs.IInLogger
	// - Stubs.IInRepository
}
#endregion

// ============================================================================
// Partial Property Auto-Instantiation (C# 13 / .NET 9+)
// ============================================================================

#if NET9_0_OR_GREATER
#region docs:inline-stubs:partial-property
[KnockOff<IInUserService>]
[KnockOff<IInLogger>]
public partial class PartialPropertyTests
{
	// Partial properties are auto-instantiated by the generator
	public partial Stubs.IInUserService UserService { get; }
	protected partial Stubs.IInLogger Logger { get; }

	// Use directly in tests - no instantiation needed:
	// UserService.GetUser.OnCall = (ko, id) => new InUser { Id = id };
	// Logger.Log.OnCall = (ko, msg) => Console.WriteLine(msg);
}
#endregion
#endif

// ============================================================================
// Direct Instantiation (Works on all .NET versions)
// ============================================================================

#region docs:inline-stubs:direct-instantiation
[KnockOff<IInUserService>]
public partial class DirectInstantiationTests
{
	// Without partial properties, instantiate stubs directly:
	// var userService = new Stubs.IInUserService();
	// var logger = new MultiServiceTests.Stubs.IInLogger();
}
#endregion

// ============================================================================
// Nested Stubs Pattern
// ============================================================================

public interface IInPropertyStore
{
	IInPropertyInfo this[int index] { get; }
	int Count { get; }
}

public interface IInPropertyInfo
{
	string Name { get; }
	object? Value { get; set; }
}

#region docs:inline-stubs:nested-stubs-interfaces
// When an interface returns another interface:
[KnockOff]                      // Explicit pattern for outer interface
[KnockOff<IInPropertyInfo>]     // Inline pattern for nested interface
public partial class PropertyStoreKnockOff : IInPropertyStore
{
	// Access the nested stub via partial property or direct instantiation
}
#endregion

#region docs:inline-stubs:nested-stubs-usage
// var store = new PropertyStoreKnockOff();
// var propStub = new PropertyStoreKnockOff.Stubs.IInPropertyInfo();
//
// // Configure nested stub
// propStub.Name.Value = "TestProp";
// propStub.Value.Value = 42;
//
// // Wire nested stub to indexer
// store.IInPropertyStore.Indexer.OnGet = (ko, index) => propStub;
//
// // Now store[0].Name returns "TestProp"
// IInPropertyStore service = store;
// Assert.Equal("TestProp", service[0].Name);
#endregion

// ============================================================================
// Interceptor API Reference
// ============================================================================

#region docs:inline-stubs:interceptor-api
// Method interceptors:
// stub.MethodName.CallCount        // int - number of calls
// stub.MethodName.WasCalled        // bool - called at least once
// stub.MethodName.LastCallArg      // T? - last argument (single param)
// stub.MethodName.LastCallArgs     // (T1, T2)? - last args (multi param)
// stub.MethodName.OnCall           // Func/Action - callback
// stub.MethodName.Reset()          // Clear all tracking and callback

// Property interceptors:
// stub.PropertyName.GetCount       // int - getter call count
// stub.PropertyName.SetCount       // int - setter call count
// stub.PropertyName.LastSetValue   // T? - last value passed to setter
// stub.PropertyName.Value          // T - backing value
// stub.PropertyName.OnGet          // Func - getter callback
// stub.PropertyName.OnSet          // Action - setter callback
// stub.PropertyName.Reset()        // Clear all tracking
#endregion

// ============================================================================
// Test Isolation (Reset Pattern)
// ============================================================================

#region docs:inline-stubs:test-isolation-reset
// xUnit: Creates new test class instance per test - automatic isolation

// NUnit/MSTest: Shared instance - use [SetUp]/[TestInitialize]:
// [SetUp]
// public void Setup()
// {
//     UserService.GetUser.Reset();
//     UserService.SaveUser.Reset();
//     UserService.ConnectionString.Reset();
// }
#endregion
