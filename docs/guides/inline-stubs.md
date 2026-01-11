# Inline Stubs

Inline stubs generate test doubles inside your test class using `KnockOffAttribute<T>`. This pattern keeps stubs close to tests and enables partial property auto-instantiation in C# 13.

## Basic Usage

Add `[KnockOff<TInterface>]` to your test class:

<!-- snippet: inline-stubs-basic-example -->
```cs
[KnockOff<IInUserService>]
public partial class UserServiceTests
{
	// The [KnockOff<IInUserService>] attribute generates:
	// - A nested Stubs class
	// - Stubs.IInUserService implementing the interface
	// - Interceptor properties for verification and callbacks
}
```
<!-- endSnippet -->

The generator creates a `Stubs` nested class containing stub implementations:

<!-- snippet: inline-stubs-basic-usage -->
```cs
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
```
<!-- endSnippet -->

## Multiple Interfaces

Stack multiple `[KnockOff<T>]` attributes on a single test class:

<!-- snippet: inline-stubs-multiple-interfaces -->
```cs
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
```
<!-- endSnippet -->

Each stub tracks invocations independently.

## Instantiation Options

### Partial Properties (C# 13 / .NET 9+)

Declare partial properties for auto-instantiation:

<!-- snippet: inline-stubs-partial-property -->
```cs
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
```
<!-- endSnippet -->

### Direct Instantiation (All .NET Versions)

Without partial properties, instantiate stubs directly:

<!-- snippet: inline-stubs-direct-instantiation -->
```cs
[KnockOff<IInUserService>]
public partial class DirectInstantiationTests
{
	// Without partial properties, instantiate stubs directly:
	// var userService = new Stubs.IInUserService();
	// var logger = new MultiServiceTests.Stubs.IInLogger();
}
```
<!-- endSnippet -->

## Nested Stubs

When interfaces return other interfaces, combine explicit and inline patterns:

<!-- snippet: inline-stubs-nested-stubs-interfaces -->
```cs
// When an interface returns another interface:
[KnockOff]                      // Explicit pattern for outer interface
[KnockOff<IInPropertyInfo>]     // Inline pattern for nested interface
public partial class PropertyStoreKnockOff : IInPropertyStore
{
	// Access the nested stub via partial property or direct instantiation
}
```
<!-- endSnippet -->

Wire stubs together using callbacks:

<!-- snippet: inline-stubs-nested-stubs-usage -->
```cs
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
```
<!-- endSnippet -->

## Interceptor API

### Method Interceptors

<!-- snippet: inline-stubs-interceptor-api -->
```cs
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
```
<!-- endSnippet -->

| Interceptor Type | Properties |
|-------------|------------|
| Method (void) | `CallCount`, `WasCalled`, `LastCallArg`/`LastCallArgs`, `OnCall`, `Reset()` |
| Method (return) | Same as void, `OnCall` returns value |
| Property (get-only) | `GetCount`, `Value`, `OnGet`, `Reset()` |
| Property (set-only) | `SetCount`, `LastSetValue`, `OnSet`, `Reset()` |
| Property (get/set) | All of the above |

## Test Isolation

### xUnit (Recommended)

xUnit creates new test class instances per test—stubs are automatically isolated.

### NUnit / MSTest

Use setup methods to reset stubs between tests:

<!-- snippet: inline-stubs-test-isolation-reset -->
```cs
// xUnit: Creates new test class instance per test - automatic isolation

// NUnit/MSTest: Shared instance - use [SetUp]/[TestInitialize]:
// [SetUp]
// public void Setup()
// {
//     UserService.GetUser.Reset();
//     UserService.SaveUser.Reset();
//     UserService.ConnectionString.Reset();
// }
```
<!-- endSnippet -->

## Explicit vs Inline Pattern

| Feature | Explicit (`[KnockOff]`) | Inline (`[KnockOff<T>]`) |
|---------|-------------------------|--------------------------|
| Interface binding | Implements interface directly | Generates nested Stubs class |
| Partial properties | Not supported | Auto-instantiation (C# 13+) |
| Class type | Must be separate class | Can be test class |
| Use case | Production DI, shared stubs | Test-local stubs |
| Generated code location | On the class itself | Nested `Stubs` class |

### When to Use Each

**Explicit pattern** — when you need:
- A stub class usable across multiple test files
- Dependency injection in production scenarios
- User-defined fallback methods

**Inline pattern** — when you need:
- Stubs scoped to a single test class
- Multiple stub types in one place
- Partial property auto-instantiation

## Class Stubs

Use `[KnockOff<TClass>]` to stub virtual/abstract class members:

<!-- snippet: inline-stubs-class-stub-example -->
```cs
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
```
<!-- endSnippet -->

### Key Differences from Interface Stubs

| Aspect | Interface Stubs | Class Stubs |
|--------|-----------------|-------------|
| Get typed instance | `stub` (direct) | `stub.Object` |
| Interceptor access | `stub.Member` | `stub.Member` (unified!) |
| Base behavior | N/A | Calls base class when no callback |
| Virtual only | N/A | Only virtual/abstract members intercepted |

### Accessing Non-Virtual Members

Non-virtual members are not intercepted. Access them through `.Object`:

<!-- snippet: inline-stubs-class-stub-mixed -->
```cs
public static void MixedServiceExample()
{
	var stub = new MixedTests.Stubs.MixedService();

	// Virtual member - has interceptor
	stub.VirtualProp.OnGet = (ko) => "Intercepted";
	var virtualValue = stub.Object.VirtualProp;  // "Intercepted"

	// Non-virtual member - no interceptor, use .Object
	stub.Object.NonVirtualProp = "Direct";
	var nonVirtualValue = stub.Object.NonVirtualProp;  // "Direct"

	_ = (virtualValue, nonVirtualValue);
}
```
<!-- endSnippet -->

### Constructor Chaining

Class stubs support constructor parameters:

<!-- snippet: inline-stubs-class-stub-constructor -->
```cs
public static void ConstructorChainingExample()
{
	var stub = new RepoTests.Stubs.Repository("Server=test");
	var connectionString = stub.Object.ConnectionString;  // "Server=test"

	_ = connectionString;
}
```
<!-- endSnippet -->

### Abstract Classes

Abstract members return defaults unless configured:

<!-- snippet: inline-stubs-class-stub-abstract -->
```cs
public static void AbstractClassExample()
{
	var stub = new AbstractTests.Stubs.BaseRepository();

	// Without callback - returns defaults
	var defaultConnection = stub.Object.ConnectionString;  // null
	var defaultExecute = stub.Object.Execute("SELECT 1");  // 0

	// With callback
	stub.ConnectionString.OnGet = (ko) => "Server=test";
	stub.Execute.OnCall = (ko, sql) => sql.Length;

	var configuredConnection = stub.Object.ConnectionString;  // "Server=test"
	var configuredExecute = stub.Object.Execute("SELECT 1");  // 8

	_ = (defaultConnection, defaultExecute, configuredConnection, configuredExecute);
}
```
<!-- endSnippet -->
