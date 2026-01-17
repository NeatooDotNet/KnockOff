/// <summary>
/// Code samples for docs/getting-started.md
///
/// Snippets in this file:
/// - getting-started:interface-definition
/// - getting-started:stub-declaration
/// - getting-started:using-stub
/// - getting-started:verification-basic
/// - getting-started:configuration-callback
/// - getting-started:configuration-property
/// - getting-started:multiple-calls
/// - getting-started:inline-stub-declaration
/// - getting-started:inline-stub-usage
///
/// Corresponding tests: GettingStartedSamplesTests.cs
/// </summary>

namespace KnockOff.Documentation.Samples.GettingStarted;

// ============================================================================
// Domain Types
// ============================================================================

public class Product
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public decimal Price { get; set; }
}

public class OrderResult
{
	public bool Success { get; set; }
	public string? ErrorMessage { get; set; }
}

// ============================================================================
// Step 1: Interface Definition
// ============================================================================

#region getting-started-interface-definition
public interface IOrderService
{
	Product? GetProduct(int id);
	OrderResult PlaceOrder(int productId, int quantity);
	bool IsAvailable(int productId);
}
#endregion

// ============================================================================
// Step 2: Stub Declaration
// ============================================================================

#region getting-started-stub-declaration
[KnockOff]
public partial class OrderServiceStub : IOrderService
{
	// That's it! The source generator creates the implementation.
	// No methods to write, no properties to define.
}
#endregion

// ============================================================================
// Step 3: Using the Stub
// ============================================================================

public static class UsingStubExample
{
	public static void Example()
	{
		#region getting-started-using-stub
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
		#endregion

		_ = product;
	}
}

// ============================================================================
// Step 4: Basic Verification
// ============================================================================

public static class VerificationExample
{
	public static void Example()
	{
		var stub = new OrderServiceStub();
		IOrderService service = stub;
		service.GetProduct(123);

		#region getting-started-verification-basic
		// Check if method was called
		var wasCalled = stub.GetProduct.WasCalled;  // true

		// Check how many times
		var callCount = stub.GetProduct.CallCount;  // 1

		// Capture the argument
		var lastId = stub.GetProduct.LastArg;  // 123
		#endregion

		_ = (wasCalled, callCount, lastId);
	}
}

// ============================================================================
// Configuration via Callback
// ============================================================================

public static class CallbackConfigExample
{
	public static void Example()
	{
		var stub = new OrderServiceStub();

		#region getting-started-configuration-callback
		stub.GetProduct.OnCall((ko, id) =>
		{
			if (id == 123)
				return new Product { Id = 123, Name = "Widget", Price = 9.99m };

			return null;  // Product not found
		});
		#endregion

		IOrderService service = stub;
		var product = service.GetProduct(123);
		_ = product;
	}
}

// ============================================================================
// Configuration via Property
// ============================================================================

public static class PropertyConfigExample
{
	public static void Example()
	{
		var stub = new OrderServiceStub();

		#region getting-started-configuration-property
		// Set property backing value
		stub.IsAvailable.Value = true;

		// Or use a dynamic getter
		stub.IsAvailable.OnGet = (ko) => DateTime.Now.Hour < 17;  // Available until 5 PM
		#endregion

		IOrderService service = stub;
		var available = service.IsAvailable(123);
		_ = available;
	}
}

// ============================================================================
// Multiple Calls Tracking
// ============================================================================

public static class MultipleCallsExample
{
	public static void Example()
	{
		var stub = new OrderServiceStub();
		IOrderService service = stub;

		#region getting-started-multiple-calls
		service.GetProduct(100);
		service.GetProduct(200);
		service.GetProduct(300);

		var totalCalls = stub.GetProduct.CallCount;  // 3
		var lastCall = stub.GetProduct.LastArg;      // 300
		#endregion

		_ = (totalCalls, lastCall);
	}
}

// ============================================================================
// Inline Stub Pattern
// ============================================================================

#region getting-started-inline-stub-declaration
[KnockOff<IOrderService>]
public partial class OrderProcessingTests
{
	// The generator creates a nested Stubs class with IOrderService implementation
}
#endregion

public static class InlineStubUsageExample
{
	public static void Example()
	{
		#region getting-started-inline-stub-usage
		// Create the inline stub
		var stub = new OrderProcessingTests.Stubs.IOrderService();

		// Configure and use just like standalone stubs
		stub.GetProduct.OnCall((ko, id) => new Product { Id = id });

		IOrderService service = stub;
		var product = service.GetProduct(123);
		#endregion

		_ = product;
	}
}
