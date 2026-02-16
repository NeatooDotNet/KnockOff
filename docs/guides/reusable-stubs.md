[Home](../../README.md) > [Guides](.) > Reusable Stubs

# Reusable Stub Classes

Standalone stubs are real C# classes. Define one once, add constructor parameters and stub overrides, then reuse it across your entire test project. Pass it through constructors, register it in a DI container, share it between test fixtures — it's just a class.

---

## The Problem

With Moq or NSubstitute, reusing mock setups means writing shared factory methods. These methods start simple but accumulate parameters as different tests need different variations. The setup logic lives in lambda expressions and `Arg.Any<>()` calls instead of real methods. You can't add fields, constructors, or helper methods to a `Mock<T>`.

KnockOff's standalone patterns solve this by giving you a real class that implements the interface (or extends the base class) with full mocking capabilities built in.

---

## The Solution: Standalone Stubs

Define the interface and a consumer that depends on it:

<!-- snippet: reusable-interface -->
```cs
public interface IOrderRepository
{
    Order? GetOrder(int orderId);
    void SaveOrder(Order order);
    decimal GetTotal(int orderId);
}
```
<!-- endSnippet -->

<!-- snippet: reusable-consumer -->
```cs
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
```
<!-- endSnippet -->

Create a standalone stub with constructor parameters and stub overrides:

<!-- snippet: reusable-stub-definition -->
```cs
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
```
<!-- endSnippet -->

- **Constructor parameters** — `List<Order> orders` flows test data in naturally.
- **Stub overrides** — `GetOrder_` and `GetTotal_` provide default behavior. The underscore suffix overrides the generated base class method.
- **`SaveOrder`** is not overridden — it still works with `Return`/`Call`/`Verify` per-test.

---

## Reuse Across Tests

Same stub class, different data per test:

<!-- snippet: reusable-different-data -->
```cs
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
```
<!-- endSnippet -->

Override specific behavior for one test without affecting the defaults:

<!-- snippet: reusable-per-test-override -->
```cs
var orders = new List<Order>
{
    new() { Id = 1, Customer = "Alice", Total = 50m }
};
var stub = new OrderRepoStub(orders);

// Override GetTotal for this specific test
stub.GetTotal.Return((id) => 999m);

IOrderRepository repo = stub;
Assert.Equal(999m, repo.GetTotal(1));  // Override wins
Assert.NotNull(repo.GetOrder(1));       // Stub override still works
```
<!-- endSnippet -->

`Return(callback)` supersedes the stub override. Other members (`GetOrder`) keep their stub override behavior.

---

## Constructor Injection

Your stub IS the `IOrderRepository` implementation — no `.Object`, no proxy wrapper. Pass it directly:

<!-- snippet: reusable-constructor-injection -->
```cs
var orders = new List<Order>
{
    new() { Id = 1, Customer = "Alice", Total = 200m, Status = "Pending" }
};
var stub = new OrderRepoStub(orders);

// Pass stub directly — it IS the IOrderRepository implementation
var service = new OrderService(stub);

service.MarkShipped(1);

Assert.Equal("Shipped", orders[0].Status);
```
<!-- endSnippet -->

This works with any DI container too. Register the stub as the interface implementation and it flows through your dependency graph like any real class.

---

## Verification

Reusable stubs support the full `Verify`/`Call`/`Return` API:

<!-- snippet: reusable-verification -->
```cs
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
Assert.Equal(1, stub.SaveOrder.LastArgs!.Id);
```
<!-- endSnippet -->

---

## The Four Standalone Patterns

All four standalone patterns produce reusable classes. The only difference is whether you need `.Object` to get the target type.

### Pattern 1: Standalone Interface

`[KnockOff]` on a class implementing an interface. The stub IS the implementation — no `.Object`.

<!-- snippet: reusable-stub-definition -->
```cs
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
```
<!-- endSnippet -->

### Pattern 2: Generic Standalone Interface

Same as Pattern 1, but generic. Define one stub class, use it with any type argument:

<!-- snippet: reusable-generic-interface -->
```cs
public interface IReusableRepo<T> where T : class
{
    T? GetById(int id);
    void Save(T entity);
}
```
<!-- endSnippet -->

<!-- snippet: reusable-generic-stub -->
```cs
[KnockOff]
public partial class ReusableRepoStub<T> : IReusableRepo<T> where T : class { }
```
<!-- endSnippet -->

<!-- snippet: reusable-generic-usage -->
```cs
// Same stub class, different type arguments
var orderRepo = new ReusableRepoStub<Order>();
orderRepo.GetById.Return((id) => new Order { Id = id, Customer = "Test" });

var customerRepo = new ReusableRepoStub<Customer>();
customerRepo.GetById.Return((id) => new Customer { Id = id, Name = "Test" });

IReusableRepo<Order> orders = orderRepo;
IReusableRepo<Customer> customers = customerRepo;

Assert.Equal("Test", orders.GetById(1)!.Customer);
Assert.Equal("Test", customers.GetById(1)!.Name);
```
<!-- endSnippet -->

### Pattern 3: Standalone Class

`[KnockOffBase<T>]` for abstract/virtual classes. Use `.Object` to get the class instance:

<!-- snippet: reusable-class-stub -->
```cs
public abstract class ReusablePaymentGateway
{
    public abstract decimal ProcessPayment(decimal amount);
    public virtual string GetProvider() => "Default";
}

[KnockOffBase<ReusablePaymentGateway>]
public partial class ReusablePaymentGatewayStub { }
```
<!-- endSnippet -->

<!-- snippet: reusable-class-usage -->
```cs
// Same stub class, different configuration per test
var stub = new ReusablePaymentGatewayStub();
stub.ProcessPayment.Return((amount) => amount * 0.98m); // 2% fee

ReusablePaymentGateway gateway = stub.Object;

Assert.Equal(98m, gateway.ProcessPayment(100m));
Assert.Equal("Default", gateway.GetProvider()); // Virtual method keeps default
```
<!-- endSnippet -->

### Pattern 4: Generic Standalone Class

Combines generics with class stubbing. Use `.Object` to get the class instance:

<!-- snippet: reusable-generic-class-stub -->
```cs
public abstract class ValidatorBase<T> where T : class
{
    public abstract bool Validate(T entity);
}

[KnockOffBase(typeof(ValidatorBase<>))]
public partial class ValidatorStub<T> where T : class { }
```
<!-- endSnippet -->

<!-- snippet: reusable-generic-class-usage -->
```cs
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
```
<!-- endSnippet -->

---

## Summary

Standalone patterns give you:

- **A real C# class** — constructor parameters, fields, helper methods, inheritance
- **Reuse across tests** — define once, instantiate with different data per test
- **Per-test overrides** — `Return`/`Call`/`When` override stub overrides for specific tests
- **Constructor injection** — pass the stub directly as a dependency, register it in DI
- **Full mocking API** — `Verify`, `Call`, `Return`, `When`, `Strict` all work on the same reusable class
- **No `.Object` for interfaces** — the stub IS the implementation (patterns 1, 2)
- **`.Object` for classes** — class stubs wrap the instance (patterns 3, 4)

Next: [Stub Overrides](stub-overrides.md) for details on the underscore-suffix override convention.

---

**UPDATED:** 2026-02-08
