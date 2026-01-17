namespace KnockOff.Documentation.Samples.Tests.GettingStarted;

using KnockOff.Documentation.Samples.GettingStarted;

/// <summary>
/// Tests to verify getting-started.md samples compile and work correctly.
/// </summary>
public class GettingStartedSamplesTests
{
	[Fact]
	public void StubDeclaration_Works()
	{
		var stub = new OrderServiceStub();
		Assert.NotNull(stub);
		Assert.NotNull(stub.GetProduct);
		Assert.NotNull(stub.PlaceOrder);
		Assert.NotNull(stub.IsAvailable);
	}

	[Fact]
	public void UsingStub_Works()
	{
		var stub = new OrderServiceStub();
		stub.GetProduct.OnCall((ko, id) => new Product
		{
			Id = id,
			Name = "Test Product",
			Price = 19.99m
		});

		IOrderService service = stub;
		var product = service.GetProduct(123);

		Assert.NotNull(product);
		Assert.Equal(123, product.Id);
		Assert.Equal("Test Product", product.Name);
		Assert.Equal(19.99m, product.Price);
	}

	[Fact]
	public void BasicVerification_Works()
	{
		var stub = new OrderServiceStub();
		IOrderService service = stub;

		service.GetProduct(123);

		Assert.True(stub.GetProduct.WasCalled);
		Assert.Equal(1, stub.GetProduct.CallCount);
		Assert.Equal(123, stub.GetProduct.LastArg);
	}

	[Fact]
	public void CallbackConfiguration_Works()
	{
		var stub = new OrderServiceStub();
		stub.GetProduct.OnCall((ko, id) =>
		{
			if (id == 123)
				return new Product { Id = 123, Name = "Widget", Price = 9.99m };
			return null;
		});

		IOrderService service = stub;

		var found = service.GetProduct(123);
		Assert.NotNull(found);
		Assert.Equal("Widget", found.Name);

		var notFound = service.GetProduct(999);
		Assert.Null(notFound);
	}

	[Fact]
	public void PropertyConfiguration_Works()
	{
		var stub = new OrderServiceStub();
		stub.IsAvailable.Value = true;

		IOrderService service = stub;
		Assert.True(service.IsAvailable(123));
	}

	[Fact]
	public void MultipleCalls_TrackedCorrectly()
	{
		var stub = new OrderServiceStub();
		IOrderService service = stub;

		service.GetProduct(100);
		service.GetProduct(200);
		service.GetProduct(300);

		Assert.Equal(3, stub.GetProduct.CallCount);
		Assert.Equal(300, stub.GetProduct.LastArg);
	}

	[Fact]
	public void InlineStub_Works()
	{
		var stub = new OrderProcessingTests.Stubs.IOrderService();
		stub.GetProduct.OnCall((ko, id) => new Product { Id = id });

		IOrderService service = stub;
		var product = service.GetProduct(123);

		Assert.NotNull(product);
		Assert.Equal(123, product.Id);
	}
}
