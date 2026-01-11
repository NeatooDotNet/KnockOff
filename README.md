# KnockOff

A Roslyn Source Generator for creating unit test stubs. Unlike Moq's fluent runtime configuration, KnockOff uses partial classes for compile-time setup—trading flexibility for readability and performance.

## Why KnockOff? Your Tests Run Faster

KnockOff has the benefits of source generation—no runtime reflection, no Castle.Core dependency:

| Scenario | Moq | KnockOff | Speedup |
|----------|-----|----------|---------|
| Method invocation | 216 ns | 0.4 ns | **500x faster** |
| Create 1000 stubs | 745 μs | 5.6 μs | **133x faster** |
| Typical unit test | 72 μs | 31 ns | **2,300x faster** |

**Zero allocations on invocations.** Moq allocates 288-408 bytes per call for its interception machinery. KnockOff generates direct method calls—no allocations, no GC pressure.

**What this means for your test suite:**
- A project with 5,000 tests using mocks could see test runs drop from minutes to seconds
- CI/CD pipelines complete faster, giving you quicker feedback
- Local test runs feel instant, encouraging you to run tests more often

## Compile-Time Advantages

KnockOff generates real C# code. This unlocks benefits that runtime mocking frameworks can't provide.

### Compile-Time Safety

When an interface changes, KnockOff fails at compile time. Moq fails at runtime.

```csharp
// IUserService adds a new method: Task<User> GetUserAsync(int id);

// Moq: Compiles fine, fails at runtime in CI
var mock = new Mock<IUserService>();
mock.Setup(x => x.GetUser(1)).Returns(new User());  // Oops, missed GetUserAsync

// KnockOff: Compiler error immediately
[KnockOff]
public partial class UserServiceKnockOff : IUserService { }
// CS0535: 'UserServiceKnockOff' does not implement interface member 'IUserService.GetUserAsync(int)'
```

### Full IDE Support

Source-generated code means full IntelliSense, Ctrl+Click navigation, and refactoring:

- **Rename a method?** All stubs update automatically
- **Find all references?** Includes stub usages
- **Hover for docs?** Shows parameter names and types

### Debuggable Stubs

Set breakpoints in your user-defined methods. Step through your stub logic like normal code.

```csharp
[KnockOff]
public partial class RepositoryKnockOff : IRepository
{
    protected User? GetById(int id)
    {
        // Set a breakpoint here — it works!
        return _testUsers.FirstOrDefault(u => u.Id == id);
    }
}
```

With Moq, you're stepping through Castle.Core proxy internals.

## Side-by-Side Examples

### Scenario 1: Order Processing

A business scenario with payment processing, inventory management, and notifications.

#### Successful Order

**Moq**

```csharp
[Fact]
public void OrderProcessing_Success_Moq()
{
    // Arrange
    var order = new FcOrder
    {
        Id = 1,
        CustomerId = 100,
        Amount = 99.99m,
        Items = [new FcOrderItem { ProductId = 1, Quantity = 2, UnitPrice = 49.99m }]
    };

    var orderRepo = new Mock<IFcOrderRepository>();
    orderRepo.Setup(x => x.GetById(1)).Returns(order);

    var paymentService = new Mock<IFcPaymentService>();
    paymentService
        .Setup(x => x.ProcessPayment(100, 99.99m))
        .Returns(new FcPaymentResult { Success = true, TransactionId = "TXN-123" });

    var notificationService = new Mock<IFcNotificationService>();
    var inventoryService = new Mock<IFcInventoryService>();
    inventoryService
        .Setup(x => x.ReserveItems(It.IsAny<IEnumerable<FcOrderItem>>()))
        .Returns(true);

    var processor = new FcOrderProcessor(
        orderRepo.Object,
        paymentService.Object,
        notificationService.Object,
        inventoryService.Object);

    // Act
    var result = processor.ProcessOrder(1);

    // Assert
    Assert.True(result);
    orderRepo.Verify(x => x.Save(It.Is<FcOrder>(o => o.Status == "Completed")), Times.Once);
    notificationService.Verify(x => x.SendOrderConfirmation(100, 1), Times.Once);
    inventoryService.Verify(x => x.ReleaseItems(It.IsAny<IEnumerable<FcOrderItem>>()), Times.Never);
}
```

**KnockOff**

```csharp
[Fact]
public void OrderProcessing_Success_KnockOff()
{
    // Arrange
    var order = new FcOrder
    {
        Id = 1,
        CustomerId = 100,
        Amount = 99.99m,
        Items = [new FcOrderItem { ProductId = 1, Quantity = 2, UnitPrice = 49.99m }]
    };

    var orderRepo = new FcOrderRepositoryStub();
    orderRepo.GetById.OnCall = (ko, id) => order;

    var paymentService = new FcPaymentServiceStub();
    paymentService.ProcessPayment.OnCall = (ko, customerId, amount) =>
        new FcPaymentResult { Success = true, TransactionId = "TXN-123" };

    var notificationService = new FcNotificationServiceStub();

    var inventoryService = new FcInventoryServiceStub();
    inventoryService.ReserveItems.OnCall = (ko, items) => true;

    var processor = new FcOrderProcessor(
        orderRepo,
        paymentService,
        notificationService,
        inventoryService);

    // Act
    var result = processor.ProcessOrder(1);

    // Assert
    Assert.True(result);
    Assert.Equal(1, orderRepo.Save.CallCount);
    Assert.Equal("Completed", orderRepo.Save.LastCallArg?.Status);
    Assert.Equal(1, notificationService.SendOrderConfirmation.CallCount);
    Assert.Equal(0, inventoryService.ReleaseItems.CallCount);
}
```

#### Payment Failure Handling

**Moq**

```csharp
[Fact]
public void OrderProcessing_PaymentFailure_Moq()
{
    // Arrange
    var order = new FcOrder
    {
        Id = 1,
        CustomerId = 100,
        Amount = 99.99m,
        Items = [new FcOrderItem { ProductId = 1, Quantity = 2, UnitPrice = 49.99m }]
    };

    var orderRepo = new Mock<IFcOrderRepository>();
    orderRepo.Setup(x => x.GetById(1)).Returns(order);

    var paymentService = new Mock<IFcPaymentService>();
    paymentService
        .Setup(x => x.ProcessPayment(It.IsAny<int>(), It.IsAny<decimal>()))
        .Returns(new FcPaymentResult { Success = false, ErrorMessage = "Insufficient funds" });

    var notificationService = new Mock<IFcNotificationService>();
    var inventoryService = new Mock<IFcInventoryService>();
    inventoryService
        .Setup(x => x.ReserveItems(It.IsAny<IEnumerable<FcOrderItem>>()))
        .Returns(true);

    var processor = new FcOrderProcessor(
        orderRepo.Object,
        paymentService.Object,
        notificationService.Object,
        inventoryService.Object);

    // Act
    var result = processor.ProcessOrder(1);

    // Assert
    Assert.False(result);
    notificationService.Verify(
        x => x.SendPaymentFailure(100, "Insufficient funds"),
        Times.Once);
    inventoryService.Verify(
        x => x.ReleaseItems(It.IsAny<IEnumerable<FcOrderItem>>()),
        Times.Once);
    orderRepo.Verify(
        x => x.Save(It.Is<FcOrder>(o => o.Status == "PaymentFailed")),
        Times.Once);
}
```

**KnockOff**

```csharp
[Fact]
public void OrderProcessing_PaymentFailure_KnockOff()
{
    // Arrange
    var order = new FcOrder
    {
        Id = 1,
        CustomerId = 100,
        Amount = 99.99m,
        Items = [new FcOrderItem { ProductId = 1, Quantity = 2, UnitPrice = 49.99m }]
    };

    var orderRepo = new FcOrderRepositoryStub();
    orderRepo.GetById.OnCall = (ko, id) => order;

    var paymentService = new FcPaymentServiceStub();
    paymentService.ProcessPayment.OnCall = (ko, customerId, amount) =>
        new FcPaymentResult { Success = false, ErrorMessage = "Insufficient funds" };

    var notificationService = new FcNotificationServiceStub();

    var inventoryService = new FcInventoryServiceStub();
    inventoryService.ReserveItems.OnCall = (ko, items) => true;

    var processor = new FcOrderProcessor(
        orderRepo,
        paymentService,
        notificationService,
        inventoryService);

    // Act
    var result = processor.ProcessOrder(1);

    // Assert
    Assert.False(result);
    Assert.Equal(1, notificationService.SendPaymentFailure.CallCount);
    Assert.Equal((100, "Insufficient funds"), notificationService.SendPaymentFailure.LastCallArgs);
    Assert.Equal(1, inventoryService.ReleaseItems.CallCount);
    Assert.Equal("PaymentFailed", orderRepo.Save.LastCallArg?.Status);
}
```

### Scenario 2: Cached Repository

A data access scenario with async operations and caching.

#### Cache Hit

**Moq**

```csharp
[Fact]
public async Task CachedRepository_CacheHit_Moq()
{
    // Arrange
    var cachedProduct = new FcProduct { Id = 1, Name = "Widget", Price = 19.99m };

    var repository = new Mock<IFcProductRepository>();
    var cache = new Mock<IFcCacheService>();
    cache.Setup(x => x.Get<FcProduct>("product:1")).Returns(cachedProduct);

    var logger = new Mock<IFcLogger>();

    var service = new FcCachedProductService(
        repository.Object,
        cache.Object,
        logger.Object);

    // Act
    var result = await service.GetProductAsync(1);

    // Assert
    Assert.Equal("Widget", result?.Name);
    repository.Verify(x => x.GetByIdAsync(It.IsAny<int>()), Times.Never);
    logger.Verify(x => x.LogInfo(It.Is<string>(s => s.Contains("Cache hit"))), Times.Once);
}
```

**KnockOff**

```csharp
[Fact]
public async Task CachedRepository_CacheHit_KnockOff()
{
    // Arrange
    var cachedProduct = new FcProduct { Id = 1, Name = "Widget", Price = 19.99m };

    var repository = new FcProductRepositoryStub();

    var cache = new FcCacheServiceStub();
    cache.Get.Of<FcProduct>().OnCall = (ko, key) => cachedProduct;

    var logger = new FcLoggerStub();

    var service = new FcCachedProductService(repository, cache, logger);

    // Act
    var result = await service.GetProductAsync(1);

    // Assert
    Assert.Equal("Widget", result?.Name);
    Assert.Equal(0, repository.GetByIdAsync.CallCount);
    Assert.True(logger.LogInfo.LastCallArg?.Contains("Cache hit"));
}
```

#### Cache Miss

**Moq**

```csharp
[Fact]
public async Task CachedRepository_CacheMiss_Moq()
{
    // Arrange
    var product = new FcProduct { Id = 1, Name = "Widget", Price = 19.99m };

    var repository = new Mock<IFcProductRepository>();
    repository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(product);

    var cache = new Mock<IFcCacheService>();
    cache.Setup(x => x.Get<FcProduct>("product:1")).Returns((FcProduct?)null);

    var logger = new Mock<IFcLogger>();

    var service = new FcCachedProductService(
        repository.Object,
        cache.Object,
        logger.Object);

    // Act
    var result = await service.GetProductAsync(1);

    // Assert
    Assert.Equal("Widget", result?.Name);
    repository.Verify(x => x.GetByIdAsync(1), Times.Once);
    cache.Verify(
        x => x.Set("product:1", product, It.IsAny<TimeSpan>()),
        Times.Once);
    logger.Verify(
        x => x.LogInfo(It.Is<string>(s => s.Contains("Cache miss"))),
        Times.Once);
}
```

**KnockOff**

```csharp
[Fact]
public async Task CachedRepository_CacheMiss_KnockOff()
{
    // Arrange
    var product = new FcProduct { Id = 1, Name = "Widget", Price = 19.99m };

    var repository = new FcProductRepositoryStub();
    repository.GetByIdAsync.OnCall = (ko, id) => Task.FromResult<FcProduct?>(product);

    var cache = new FcCacheServiceStub();
    cache.Get.Of<FcProduct>().OnCall = (ko, key) => null;

    var logger = new FcLoggerStub();

    var service = new FcCachedProductService(repository, cache, logger);

    // Act
    var result = await service.GetProductAsync(1);

    // Assert
    Assert.Equal("Widget", result?.Name);
    Assert.Equal(1, repository.GetByIdAsync.CallCount);
    Assert.Equal(1, cache.Set.Of<FcProduct>().CallCount);
    Assert.Equal("product:1", cache.Set.Of<FcProduct>().LastCallArg);
    Assert.True(logger.LogInfo.LastCallArg?.Contains("Cache miss"));
}
```

## Performance Benchmarks

Benchmarks run on the exact scenarios shown above, measuring realistic multi-dependency test patterns.

```
BenchmarkDotNet v0.14.0, Ubuntu 24.04.3 LTS (WSL)
Intel Core i7-11800H 2.30GHz, .NET 9.0.11

| Method                              | Mean         | Allocated |
|------------------------------------ |-------------:|----------:|
| OrderProcessing_Success_Moq         | 339,693.0 ns |   44,646 B |
| OrderProcessing_Success_KnockOff    |     115.3 ns |      736 B |
| OrderProcessing_Success_Rocks       |     750.2 ns |    3,048 B |
| CachedRepository_CacheMiss_Moq      | 436,504.9 ns |   45,180 B |
| CachedRepository_CacheMiss_KnockOff |     385.2 ns |    1,576 B |
| CachedRepository_CacheMiss_Rocks    |     781.2 ns |    3,112 B |
```

### Analysis

| Scenario | KnockOff vs Moq | Rocks vs Moq |
|----------|-----------------|--------------|
| Order Processing | **2,945x faster**, 60x less memory | 453x faster, 15x less memory |
| Cached Repository | **1,133x faster**, 29x less memory | 559x faster, 15x less memory |

**Why the difference?**

- **Moq** uses runtime reflection, expression tree compilation, and Castle.Core dynamic proxy generation
- **Rocks** generates code at compile-time but creates expectation tracking infrastructure per-test
- **KnockOff** generates minimal stub classes with direct delegate invocation and no verification overhead

For large test suites (1000+ tests), these differences compound significantly. A test suite taking 30 seconds with Moq might complete in under 1 second with KnockOff.

### No Lambda Ceremony

Define behavior with normal methods, not expression trees:

```csharp
// Moq
mock.Setup(x => x.GetUser(It.IsAny<int>())).Returns((int id) => new User { Id = id });
mock.Setup(x => x.IsActive).Returns(true);
mock.Setup(x => x.SaveAsync(It.IsAny<User>())).ReturnsAsync(true);

// KnockOff — just write methods
protected User GetUser(int id) => new User { Id = id };
protected bool IsActive => true;
protected Task<bool> SaveAsync(User user) => Task.FromResult(true);
```

## Documentation

- [Getting Started](docs/getting-started.md) - Installation and first steps
- [Customization Patterns](docs/concepts/customization-patterns.md) - The two ways to customize stub behavior
- [Generic Interfaces](docs/guides/generics.md) - Generic interfaces and standalone stubs
- [KnockOff vs Moq Comparison](docs/knockoff-vs-moq.md) - Side-by-side comparison for supported scenarios
- [Migration from Moq](docs/migration-from-moq.md) - Step-by-step migration guide
- [Diagnostics](docs/diagnostics.md) - Compiler diagnostics and how to resolve them
- [Release Notes](docs/release-notes/index.md) - Version history

## Concept

Mark a partial class with `[KnockOff]` that implements an interface. The source generator:
1. Generates explicit interface implementations for all interface members
2. Tracks invocations via interface-named properties for test verification
3. Detects user-defined methods in the partial class and calls them from the generated intercepts
4. Provides `OnCall`/`OnGet`/`OnSet` callbacks for runtime customization

## Quick Example

### Inline Stubs — Interfaces and Classes

Add `[KnockOff<T>]` to your test class. Works with interfaces and unsealed classes:

```csharp
[KnockOff<IUserService>]        // Interface
[KnockOff<EmailService>]        // Unsealed class (virtual members only)
public partial class UserTests
{
    [Fact]
    public void NotifiesUser_WhenOrderShipped()
    {
        // Create stubs
        var userStub = new Stubs.IUserService();
        var emailStub = new Stubs.EmailService();

        // Configure behavior (unified API for both interface and class stubs)
        userStub.GetUser.OnCall = (ko, id) => new User { Id = id, Email = "test@example.com" };
        emailStub.Send.OnCall = (ko, to, subject, body) => { };

        // Inject and test (.Object for class stubs)
        var service = new OrderService(userStub, emailStub.Object);
        service.ShipOrder(orderId: 42, userId: 1);

        // Verify
        Assert.True(userStub.GetUser.WasCalled);
        Assert.Equal(1, userStub.GetUser.LastCallArg);
        Assert.Equal("test@example.com", emailStub.Send.LastCallArgs?.to);
    }
}
```

**Class stubs use composition** — access the target class via `.Object`. Virtual/abstract members are intercepted; non-virtual members accessed through `.Object`.

### Reusable Stubs with Default Behavior

For stubs shared across test files, use the explicit pattern with user-defined methods:

```csharp
[KnockOff]
public partial class CalculatorKnockOff : ICalculator
{
    // User-defined method — called by generated code
    protected int Add(int a, int b) => a + b;

    // Multiply not defined — returns default(int) = 0
}

// Every test gets the same behavior
[Fact]
public void Test1()
{
    var calc = new CalculatorKnockOff();
    Assert.Equal(5, calc.AsCalculator().Add(2, 3));  // Uses your method
    Assert.Equal(1, calc.ICalculator.Add.CallCount);
}

[Fact]
public void Test2_OverrideForThisTest()
{
    var calc = new CalculatorKnockOff();
    calc.ICalculator.Add.OnCall = (ko, a, b) => 999;  // Override just here
    Assert.Equal(999, calc.AsCalculator().Add(2, 3));
}
```

### Delegate Stubs

Stub named delegates for validation rules, factories, and callbacks:

```csharp
public delegate bool IsUniqueRule(string value);

[KnockOff<IsUniqueRule>]
public partial class ValidationTests
{
    [Fact]
    public void RejectsNonUniqueName()
    {
        var uniqueCheck = new Stubs.IsUniqueRule();
        uniqueCheck.Interceptor.OnCall = (ko, value) => value != "duplicate";

        IsUniqueRule rule = uniqueCheck;  // Implicit conversion
        Assert.False(rule("duplicate"));
        Assert.True(uniqueCheck.Interceptor.WasCalled);
    }
}
```

## Verification

```csharp
var knockOff = new DataServiceKnockOff();
IDataService service = knockOff;

service.GetDescription(1);
service.GetDescription(2);
service.GetDescription(42);

// Check invocation
Assert.True(knockOff.IDataService.GetDescription.WasCalled);
Assert.Equal(3, knockOff.IDataService.GetDescription.CallCount);

// Check arguments
Assert.Equal(42, knockOff.IDataService.GetDescription.LastCallArg);

// Check properties
service.Name = "First";
service.Name = "Second";
Assert.Equal(2, knockOff.IDataService.Name.SetCount);
Assert.Equal("Second", knockOff.IDataService.Name.LastSetValue);
```

## Features

| Feature | Status |
|---------|--------|
| Properties (get/set, get-only, set-only) | Supported |
| Void methods | Supported |
| Methods with return values | Supported |
| Methods with parameters (single and multiple) | Supported |
| Method overloads | Supported |
| Async methods (Task, Task&lt;T&gt;, ValueTask, ValueTask&lt;T&gt;) | Supported |
| Generic interfaces | Supported |
| Generic standalone stubs (`Stub<T> : IRepo<T>`) | Supported |
| Multiple interface implementation | Supported |
| Interface inheritance | Supported |
| Indexers (get-only, get/set) | Supported |
| User-defined method detection | Supported |
| OnCall/OnGet/OnSet callbacks | Supported |
| Named tuple argument tracking | Supported |
| Events | Supported |
| Generic methods | Supported |
| ref/out parameters | Supported |

## Limitation: Interfaces with Internal Members

KnockOff **cannot stub interfaces with `internal` members from external assemblies**. This is a C# language constraint, not a tooling limitation.

```csharp
// In ExternalLibrary.dll
public interface IEntity
{
    bool IsModified { get; }       // public - stubbable
    internal void MarkModified();  // internal - impossible to implement externally
}
```

Internal members are invisible to external assemblies. No C# syntax—implicit or explicit interface implementation—can reference an invisible member. The compiler errors are CS0122 ("inaccessible due to protection level") and CS9044 ("cannot implicitly implement inaccessible member").

**KnockOff's behavior:** Internal members from external assemblies are filtered out. Public members are stubbed normally. If your tests require mocking internal members, use a runtime proxy library that the target assembly has declared as a friend via `[InternalsVisibleTo]`.

## Generated Code

For each interface member, KnockOff generates:
- **Handler class** with tracking properties and callbacks
- **Explicit interface implementation** that records invocations
- **Backing storage** (field for properties, dictionary for indexers)
- **`AsXYZ()` helper** for typed interface access

Example generated structure:
```csharp
public partial class UserServiceKnockOff
{
    public IUserServiceKO IUserService { get; } = new();

    public sealed class IUserServiceKO
    {
        public IUserService_GetUserHandler GetUser { get; } = new();
        public IUserService_NameHandler Name { get; } = new();
        // ... handlers for each member
    }

    User IUserService.GetUser(int id)
    {
        IUserService.GetUser.RecordCall(id);
        if (IUserService.GetUser.OnCall is { } callback)
            return callback(this, id);
        return GetUser(id); // Calls user method
    }
}
```

## Installation

```bash
dotnet add package KnockOff
```

Or add to your `.csproj`:
```xml
<PackageReference Include="KnockOff" Version="1.0.0" />
```

## Viewing Generated Code

Enable in your test project:
```xml
<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
<CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
```

Generated files appear in `Generated/KnockOff.Generator/KnockOff.KnockOffGenerator/`.
