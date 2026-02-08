using KnockOff;

namespace KnockOff.Documentation.Samples.ReusableStubs;

// =============================================================================
// Domain Types
// =============================================================================

#region reusable-interface
public interface IOrderRepository
{
    Order? GetOrder(int orderId);
    void SaveOrder(Order order);
    decimal GetTotal(int orderId);
}
#endregion

public class Order
{
    public int Id { get; set; }
    public string Customer { get; set; } = "";
    public decimal Total { get; set; }
    public string Status { get; set; } = "Pending";
}

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

// =============================================================================
// Consumer class (the thing that depends on the interface)
// =============================================================================

#region reusable-consumer
public class OrderService(IOrderRepository repository)
{
    public decimal CalculateDiscount(int orderId)
    {
        var order = repository.GetOrder(orderId);
        if (order == null) return 0m;
        return order.Total > 100m ? order.Total * 0.1m : 0m;
    }

    public void MarkShipped(int orderId)
    {
        var order = repository.GetOrder(orderId);
        if (order == null) throw new InvalidOperationException("Order not found");
        order.Status = "Shipped";
        repository.SaveOrder(order);
    }
}
#endregion

// =============================================================================
// Pattern 1: Standalone Interface Stub (Reusable)
// =============================================================================

#region reusable-stub-definition
[KnockOff]
public partial class OrderRepoStub(List<Order> orders) : IOrderRepository { }

public partial class OrderRepoStub
{
    protected override Order? GetOrder_(int orderId)
    {
        return orders.SingleOrDefault(o => o.Id == orderId);
    }

    protected override decimal GetTotal_(int orderId)
    {
        return orders.Where(o => o.Id == orderId).Sum(o => o.Total);
    }
}
#endregion

// =============================================================================
// Pattern 2: Generic Standalone Interface Stub
// =============================================================================

#region reusable-generic-interface
public interface IReusableRepo<T> where T : class
{
    T? GetById(int id);
    void Save(T entity);
}
#endregion

#region reusable-generic-stub
[KnockOff]
public partial class ReusableRepoStub<T> : IReusableRepo<T> where T : class { }
#endregion

// =============================================================================
// Pattern 3: Standalone Class Stub
// =============================================================================

#region reusable-class-stub
public abstract class ReusablePaymentGateway
{
    public abstract decimal ProcessPayment(decimal amount);
    public virtual string GetProvider() => "Default";
}

[KnockOffBase<ReusablePaymentGateway>]
public partial class ReusablePaymentGatewayStub { }
#endregion

// =============================================================================
// Pattern 4: Generic Standalone Class Stub
// =============================================================================

#region reusable-generic-class-stub
public abstract class ValidatorBase<T> where T : class
{
    public abstract bool Validate(T entity);
}

[KnockOffBase(typeof(ValidatorBase<>))]
public partial class ValidatorStub<T> where T : class { }
#endregion

// =============================================================================
// Tests
// =============================================================================

public class ReusableStubsTests
{
    // -------------------------------------------------------------------------
    // Pattern 1: Same stub class, different data per test
    // -------------------------------------------------------------------------

    [Fact]
    public void SameStub_MultipleTests_DifferentData()
    {
        #region reusable-different-data
        // Test 1: small order — no discount
        var smallOrders = new List<Order>
        {
            new() { Id = 1, Customer = "Alice", Total = 50m }
        };
        var stub1 = new OrderRepoStub(smallOrders);
        var service1 = new OrderService(stub1);

        Assert.Equal(0m, service1.CalculateDiscount(1));

        // Test 2: large order — 10% discount
        var largeOrders = new List<Order>
        {
            new() { Id = 1, Customer = "Bob", Total = 200m }
        };
        var stub2 = new OrderRepoStub(largeOrders);
        var service2 = new OrderService(stub2);

        Assert.Equal(20m, service2.CalculateDiscount(1));
        #endregion
    }

    [Fact]
    public void PerTest_Override()
    {
        #region reusable-per-test-override
        var orders = new List<Order>
        {
            new() { Id = 1, Customer = "Alice", Total = 50m }
        };
        var stub = new OrderRepoStub(orders);

        // Override GetTotal for this specific test
        stub.GetTotal.Return((id) => 999m);

        IOrderRepository repo = stub;
        Assert.Equal(999m, repo.GetTotal(1));  // Override wins
        Assert.NotNull(repo.GetOrder(1));       // User method still works
        #endregion
    }

    [Fact]
    public void ConstructorInjection_PassToService()
    {
        #region reusable-constructor-injection
        var orders = new List<Order>
        {
            new() { Id = 1, Customer = "Alice", Total = 200m, Status = "Pending" }
        };
        var stub = new OrderRepoStub(orders);

        // Pass stub directly — it IS the IOrderRepository implementation
        var service = new OrderService(stub);

        service.MarkShipped(1);

        Assert.Equal("Shipped", orders[0].Status);
        #endregion
    }

    [Fact]
    public void Verification_OnReusableStub()
    {
        #region reusable-verification
        var orders = new List<Order>
        {
            new() { Id = 1, Customer = "Alice", Total = 200m, Status = "Pending" }
        };
        var stub = new OrderRepoStub(orders);

        // Track SaveOrder calls
        stub.SaveOrder.Call((order) => { }).Verifiable();

        var service = new OrderService(stub);
        service.MarkShipped(1);

        // Verify SaveOrder was called
        stub.SaveOrder.Verify(Called.Once);
        Assert.Equal(1, stub.SaveOrder.LastArg!.Id);
        #endregion
    }

    // -------------------------------------------------------------------------
    // Pattern 2: Generic Standalone — same stub, multiple types
    // -------------------------------------------------------------------------

    [Fact]
    public void GenericStub_MultipleTypes()
    {
        #region reusable-generic-usage
        // Same stub class, different type arguments
        var orderRepo = new ReusableRepoStub<Order>();
        orderRepo.GetById.Return((id) => new Order { Id = id, Customer = "Test" });

        var customerRepo = new ReusableRepoStub<Customer>();
        customerRepo.GetById.Return((id) => new Customer { Id = id, Name = "Test" });

        IReusableRepo<Order> orders = orderRepo;
        IReusableRepo<Customer> customers = customerRepo;

        Assert.Equal("Test", orders.GetById(1)!.Customer);
        Assert.Equal("Test", customers.GetById(1)!.Name);
        #endregion
    }

    // -------------------------------------------------------------------------
    // Pattern 3: Standalone Class — reusable with .Object
    // -------------------------------------------------------------------------

    [Fact]
    public void ClassStub_Reusable()
    {
        #region reusable-class-usage
        // Same stub class, different configuration per test
        var stub = new ReusablePaymentGatewayStub();
        stub.ProcessPayment.Return((amount) => amount * 0.98m); // 2% fee

        ReusablePaymentGateway gateway = stub.Object;

        Assert.Equal(98m, gateway.ProcessPayment(100m));
        Assert.Equal("Default", gateway.GetProvider()); // Virtual method keeps default
        #endregion
    }

    // -------------------------------------------------------------------------
    // Pattern 4: Generic Standalone Class — reusable across types
    // -------------------------------------------------------------------------

    [Fact]
    public void GenericClassStub_MultipleTypes()
    {
        #region reusable-generic-class-usage
        var orderValidator = new ValidatorStub<Order>();
        orderValidator.Validate.Return((entity) => entity.Total > 0);

        var customerValidator = new ValidatorStub<Customer>();
        customerValidator.Validate.Return((entity) => !string.IsNullOrEmpty(entity.Name));

        ValidatorBase<Order> ov = orderValidator.Object;
        ValidatorBase<Customer> cv = customerValidator.Object;

        Assert.True(ov.Validate(new Order { Total = 50m }));
        Assert.False(ov.Validate(new Order { Total = 0m }));
        Assert.True(cv.Validate(new Customer { Name = "Alice" }));
        Assert.False(cv.Validate(new Customer { Name = "" }));
        #endregion
    }
}
