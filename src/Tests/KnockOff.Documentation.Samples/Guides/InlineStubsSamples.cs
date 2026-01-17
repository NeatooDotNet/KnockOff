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
/// - inline-stubs-collision-naming-pattern
/// - inline-stubs-no-collision-naming
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

#region inline-stubs-basic-example
[KnockOff<IInUserService>]
public partial class UserServiceTests
{
	// The [KnockOff<IInUserService>] attribute generates:
	// - A nested Stubs class
	// - Stubs.IInUserService implementing the interface
	// - Interceptor properties for verification and callbacks
}
#endregion

#region inline-stubs-basic-usage
// In your test method:
// var stub = new UserServiceTests.Stubs.IInUserService();
//
// // Configure behavior
// var tracking = stub.GetUser.OnCall((ko, id) => new InUser { Id = id, Name = "Test" });
//
// // Use the stub (implicit interface conversion)
// IInUserService service = stub;
// var user = service.GetUser(42);
//
// // Verify interactions
// Assert.True(tracking.WasCalled);
// Assert.Equal(42, tracking.LastArg);
#endregion

// ============================================================================
// Multiple Interfaces Example
// ============================================================================

#region inline-stubs-multiple-interfaces
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
#region inline-stubs-partial-property
[KnockOff<IInUserService>]
[KnockOff<IInLogger>]
public partial class PartialPropertyTests
{
	// Partial properties are auto-instantiated by the generator
	public partial Stubs.IInUserService UserService { get; }
	protected partial Stubs.IInLogger Logger { get; }

	// Use directly in tests - no instantiation needed:
	// UserService.GetUser.OnCall((ko, id) => new InUser { Id = id });
	// Logger.Log.OnCall((ko, msg) => Console.WriteLine(msg));
}
#endregion
#endif

// ============================================================================
// Direct Instantiation (Works on all .NET versions)
// ============================================================================

#region inline-stubs-direct-instantiation
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

#region inline-stubs-nested-stubs-interfaces
// When an interface returns another interface:
[KnockOff]                      // Explicit pattern for outer interface
[KnockOff<IInPropertyInfo>]     // Inline pattern for nested interface
public partial class PropertyStoreKnockOff : IInPropertyStore
{
	// Access the nested stub via partial property or direct instantiation
}
#endregion

#region inline-stubs-nested-stubs-usage
// var store = new PropertyStoreKnockOff();
// var propStub = new PropertyStoreKnockOff.Stubs.IInPropertyInfo();
//
// // Configure nested stub
// propStub.Name.Value = "TestProp";
// propStub.Value.Value = 42;
//
// // Wire nested stub to indexer
// store.Indexer.OnGet = (ko, index) => propStub;
//
// // Now store[0].Name returns "TestProp"
// IInPropertyStore service = store;
// Assert.Equal("TestProp", service[0].Name);
#endregion

// ============================================================================
// Interceptor API Reference
// ============================================================================

#region inline-stubs-interceptor-api
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

#region inline-stubs-test-isolation-reset
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

// ============================================================================
// Class Stubs Section
// ============================================================================

// ============================================================================
// Class Stubs - Type Definitions (supporting code, not extracted)
// ============================================================================

public class EmailService
{
	public virtual void Send(string to, string subject, string body)
		=> Console.WriteLine($"Sending to {to}: {subject}");

	public virtual string ServerName { get; set; } = "default";
}

public class MixedService
{
	public virtual string VirtualProp { get; set; } = "";
	public string NonVirtualProp { get; set; } = "";
}

public class Repository
{
	public string ConnectionString { get; }
	public Repository(string connectionString) => ConnectionString = connectionString;
	public virtual InUser? GetUser(int id) => null;
}

public abstract class BaseRepository
{
	public abstract string? ConnectionString { get; }
	public abstract int Execute(string sql);
}

// ============================================================================
// Class Stub Declarations
// ============================================================================

#region inline-stubs-class-stub-example
public class CsEmailService
{
	public virtual void Send(string to, string subject, string body)
		=> Console.WriteLine($"Sending to {to}: {subject}");

	public virtual string ServerName { get; set; } = "default";
}

[KnockOff<CsEmailService>]
public partial class CsEmailServiceTests
{
}
#endregion

[KnockOff<EmailService>]
public partial class EmailServiceTests { }

[KnockOff<MixedService>]
public partial class MixedTests { }

[KnockOff<Repository>]
public partial class RepoTests { }

[KnockOff<BaseRepository>]
public partial class AbstractTests { }

// ============================================================================
// Class Stub Usage Examples
// ============================================================================

public static class ClassStubUsageExamples
{
	public static void EmailServiceExample()
	{
		#region inline-stubs-class-stub-usage
		var stub = new CsEmailServiceTests.Stubs.CsEmailService();
		stub.Send.OnCall = (ko, to, subject, body) =>
			Console.WriteLine($"STUBBED: {to}");

		// Use .Object to get the class instance
		CsEmailService service = stub.Object;
		service.Send("test@example.com", "Hello", "World");

		var wasCalled = stub.Send.WasCalled;               // true
		var lastTo = stub.Send.LastCallArgs?.to;           // "test@example.com"
		#endregion

		_ = (wasCalled, lastTo);
	}

	#region inline-stubs-class-stub-mixed
	public static void MixedServiceExample()
	{
		var stub = new MixedTests.Stubs.MixedService();

		// Virtual member - has interceptor (property uses OnGet property, not method)
		stub.VirtualProp.OnGet = (ko) => "Intercepted";
		var virtualValue = stub.Object.VirtualProp;  // "Intercepted"

		// Non-virtual member - no interceptor, use .Object
		stub.Object.NonVirtualProp = "Direct";
		var nonVirtualValue = stub.Object.NonVirtualProp;  // "Direct"

		_ = (virtualValue, nonVirtualValue);
	}
	#endregion

	#region inline-stubs-class-stub-constructor
	public static void ConstructorChainingExample()
	{
		var stub = new RepoTests.Stubs.Repository("Server=test");
		var connectionString = stub.Object.ConnectionString;  // "Server=test"

		_ = connectionString;
	}
	#endregion

	#region inline-stubs-class-stub-abstract
	public static void AbstractClassExample()
	{
		var stub = new AbstractTests.Stubs.BaseRepository();

		// Without callback - returns defaults
		var defaultConnection = stub.Object.ConnectionString;  // null
		var defaultExecute = stub.Object.Execute("SELECT 1");  // 0

		// With callback (property uses OnGet property, method uses OnCall property)
		stub.ConnectionString.OnGet = (ko) => "Server=test";
		stub.Execute.OnCall = (ko, sql) => sql.Length;

		var configuredConnection = stub.Object.ConnectionString;  // "Server=test"
		var configuredExecute = stub.Object.Execute("SELECT 1");  // 8

		_ = (defaultConnection, defaultExecute, configuredConnection, configuredExecute);
	}
	#endregion
}

// ============================================================================
// Generic Type Collision Naming
// ============================================================================

#region inline-stubs-collision-naming-pattern
[KnockOff<IList<string>>]
[KnockOff<IList<int>>]
public partial class MultiListTests
{
	public void CollisionNamingGeneratesSuffixedNames()
	{
		// When same interface name with different type args:
		// Generator appends type suffix to avoid collision
		var stringList = new Stubs.IListString();
		var intList = new Stubs.IListInt32();

		// Configure each independently
		stringList.Count.Value = 5;
		intList.Count.Value = 10;

		IList<string> strings = stringList;
		IList<int> ints = intList;

		var stringCount = strings.Count;  // 5
		var intCount = ints.Count;        // 10

		_ = (stringCount, intCount);
	}
}
#endregion

#region inline-stubs-no-collision-naming
[KnockOff<IList<string>>]
public partial class SingleListTests
{
	public void SingleGenericUsesSimpleName()
	{
		// No collision - simple name without suffix
		var list = new Stubs.IList();

		list.Count.Value = 3;
		IList<string> strings = list;

		var count = strings.Count;  // 3
		_ = count;
	}
}
#endregion
