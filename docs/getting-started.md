# Getting Started with KnockOff

This guide walks you through creating your first KnockOff stub in under 5 minutes.

## Prerequisites

- .NET 8.0 or later
- A test project (xUnit, NUnit, or MSTest)

## Installation

Add KnockOff to your test project:

```bash
dotnet add package KnockOff
```

## Your First Stub

### Step 1: Define Your Interface

Start with the interface you want to stub:

<!-- snippet: getting-started-interface-definition -->
```cs
public interface IOrderService
{
	Product? GetProduct(int id);
	OrderResult PlaceOrder(int productId, int quantity);
	bool IsAvailable(int productId);
}
```
<sup><a href='/src/Tests/KnockOff.Documentation.Samples/GettingStarted/GettingStartedSamples.cs#L43-L49' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-interface-definition' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Step 2: Create the Stub

Mark a partial class with `[KnockOff]`:

<!-- snippet: getting-started-stub-declaration -->
```cs
[KnockOff]
public partial class OrderServiceStub : IOrderService
{
	// That's it! The source generator creates the implementation.
	// No methods to write, no properties to define.
}
```
<sup><a href='/src/Tests/KnockOff.Documentation.Samples/GettingStarted/GettingStartedSamples.cs#L56-L63' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-stub-declaration' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**What just happened?** The KnockOff source generator saw your `[KnockOff]` attribute and generated:
- Explicit interface implementations for all members
- Interceptor properties for tracking and configuration (`stub.GetProduct`, `stub.PlaceOrder`, etc.)
- An implicit conversion operator to `IOrderService`

### Step 3: Use the Stub in Tests

<!-- snippet: getting-started-using-stub -->
```cs
// Create the stub
var stub = new OrderServiceStub();

// Configure behavior
stub.GetProduct.OnCall((ko, id) => new Product
{
	Id = id,
	Name = "Test Product",
	Price = 19.99m
});

// Use as interface
IOrderService service = stub;
var product = service.GetProduct(123);
```
<sup><a href='/src/Tests/KnockOff.Documentation.Samples/GettingStarted/GettingStartedSamples.cs#L74-L88' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-using-stub' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Step 4: Verify Interactions

Every stub tracks calls automatically:

<!-- snippet: getting-started-verification-basic -->
```cs
// Check if method was called
var wasCalled = stub.GetProduct.WasCalled;  // true

// Check how many times
var callCount = stub.GetProduct.CallCount;  // 1

// Capture the argument
var lastId = stub.GetProduct.LastArg;  // 123
```
<sup><a href='/src/Tests/KnockOff.Documentation.Samples/GettingStarted/GettingStartedSamples.cs#L105-L113' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-verification-basic' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Understanding the Duality Pattern

KnockOff uses a "duality pattern" where the stub serves two purposes:

- **`stub.Member`** - Access the interceptor for configuration and verification
- **`stub` (implicit cast)** - Use as the interface instance in production code

This separation keeps test orchestration (configuration/verification) separate from production usage.

## Configuring Behavior

### Via Callbacks

Use `OnCall` for dynamic behavior:

<!-- snippet: getting-started-configuration-callback -->
```cs
stub.GetProduct.OnCall((ko, id) =>
{
	if (id == 123)
		return new Product { Id = 123, Name = "Widget", Price = 9.99m };

	return null;  // Product not found
});
```
<sup><a href='/src/Tests/KnockOff.Documentation.Samples/GettingStarted/GettingStartedSamples.cs#L128-L136' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-configuration-callback' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The first parameter (`ko`) is the stub instance, allowing you to access other interceptors or cross-member state.

### Via Properties

For simple values, use the `Value` property or `OnGet`:

<!-- snippet: getting-started-configuration-property -->
```cs
// Set property backing value
stub.IsAvailable.Value = true;

// Or use a dynamic getter
stub.IsAvailable.OnGet = (ko) => DateTime.Now.Hour < 17;  // Available until 5 PM
```
<sup><a href='/src/Tests/KnockOff.Documentation.Samples/GettingStarted/GettingStartedSamples.cs#L154-L159' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-configuration-property' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Tracking Multiple Calls

Interceptors track all calls, with `LastArg` showing the most recent:

<!-- snippet: getting-started-multiple-calls -->
```cs
service.GetProduct(100);
service.GetProduct(200);
service.GetProduct(300);

var totalCalls = stub.GetProduct.CallCount;  // 3
var lastCall = stub.GetProduct.LastArg;      // 300
```
<sup><a href='/src/Tests/KnockOff.Documentation.Samples/GettingStarted/GettingStartedSamples.cs#L178-L184' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-multiple-calls' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

For methods with multiple parameters, use `LastCallArgs` (a named tuple with original parameter names).

## Inline Stubs (Test-Local Pattern)

For stubs used in a single test class, use the inline pattern:

<!-- snippet: getting-started-inline-stub-declaration -->
```cs
[KnockOff<IOrderService>]
public partial class OrderProcessingTests
{
	// The generator creates a nested Stubs class with IOrderService implementation
}
```
<sup><a href='/src/Tests/KnockOff.Documentation.Samples/GettingStarted/GettingStartedSamples.cs#L195-L200' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-inline-stub-declaration' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Usage is identical, just access via the nested `Stubs` class:

<!-- snippet: getting-started-inline-stub-usage -->
```cs
// Create the inline stub
var stub = new OrderProcessingTests.Stubs.IOrderService();

// Configure and use just like standalone stubs
stub.GetProduct.OnCall((ko, id) => new Product { Id = id });

IOrderService service = stub;
var product = service.GetProduct(123);
```
<sup><a href='/src/Tests/KnockOff.Documentation.Samples/GettingStarted/GettingStartedSamples.cs#L207-L215' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-inline-stub-usage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Next Steps

Now that you have a working stub, explore:

- **[Stub Patterns](guides/stub-patterns.md)** - Standalone, inline, class stubs, and delegates
- **[Methods Guide](guides/methods.md)** - Method interceptors, callbacks, and verification
- **[Properties Guide](guides/properties.md)** - Property interceptors and configuration
- **[Verification Guide](guides/verification.md)** - Comprehensive testing patterns
- **[Complete Reference](reference/interceptor-api.md)** - Full API documentation

## Common First-Time Questions

**Q: Do I need to write any interface implementations?**
No. KnockOff generates all implementations automatically. Just mark your class with `[KnockOff]`.

**Q: Can I see the generated code?**
Yes. Enable `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>` in your .csproj. Generated files appear in `obj/Debug/generated/`.

**Q: When should I use standalone vs inline stubs?**
Use **standalone** (`[KnockOff]`) for stubs shared across test files. Use **inline** (`[KnockOff<T>]`) for test-local stubs used in a single test class.

**Q: What if my interface method isn't configured?**
Unconfigured methods return default values (`null`, `0`, `false`, `default(T)`). For stricter behavior, enable [strict mode](guides/strict-mode.md).

**Q: Does this work with async methods?**
Yes. Configure async methods the same way, returning `Task` or `Task<T>` from your callbacks. Full async support including `ValueTask`, `IAsyncEnumerable<T>`, etc.
