# Mocking Framework Comparison: Moq vs KnockOff vs Rocks

This document compares three .NET mocking/stubbing frameworks through realistic test scenarios.

## Table of Contents

- [Performance Benchmarks](#performance-benchmarks)
- [Framework Overview](#framework-overview)
- [Approach Comparison](#approach-comparison)
- [Feature Support Matrix](#feature-support-matrix)
- [Side-by-Side Examples](#side-by-side-examples)
  - [Scenario 1: Order Processing](#scenario-1-order-processing)
  - [Scenario 2: Cached Repository](#scenario-2-cached-repository)
- [Key Differences Illustrated](#key-differences-illustrated)
- [When to Use Each](#when-to-use-each)
- [Migration](#migration)

## Performance Benchmarks

Benchmarks run on realistic multi-dependency test patterns (see [Side-by-Side Examples](#side-by-side-examples) below).

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

## Framework Overview

| Framework | Approach | Generation | Primary Use Case |
|-----------|----------|------------|------------------|
| **Moq** | Runtime proxy | Dynamic (Castle.Core) | Full-featured mocking |
| **KnockOff** | Source generator | Compile-time | Simple interface stubs |
| **Rocks** | Source generator | Compile-time | Strict expectation-based mocking |

## Approach Comparison

| Aspect | Moq | KnockOff | Rocks |
|--------|-----|----------|-------|
| Configuration | Runtime fluent API | Compile-time partial class | Compile-time expectations |
| Type safety | Expression-based | Strongly-typed generated | Strongly-typed generated |
| Verification style | After-the-fact | Property inspection | Expectation-based |
| Setup location | Test method | Stub class or test method | Test method |
| Learning curve | Moderate | Low | Moderate |
| **Performance** | ~340 µs/test | **~0.2 µs/test** | ~0.8 µs/test |
| **Memory/test** | ~45 KB | **~1 KB** | ~3 KB |

## Feature Support Matrix

| Feature | Moq | KnockOff | Rocks |
|---------|-----|----------|-------|
| Properties (get/set) | Yes | Yes | Yes |
| Void methods | Yes | Yes | Yes |
| Methods with return values | Yes | Yes | Yes |
| Async methods | Yes | Yes | Yes |
| Generic interfaces | Yes | Yes | Yes |
| Generic methods | Yes | Yes | Yes |
| Multiple interfaces | Yes | Yes | Yes |
| Interface inheritance | Yes | Yes | Yes |
| Call verification | Yes | Yes | Yes |
| Argument capture | Callback | Automatic | Callback |
| Indexers | Yes | Yes | Yes |
| Events | Yes | Yes | Yes |
| ref/out parameters | Yes | Partial | Yes |
| Strict mode | Yes | No | Yes |
| Class mocking | Virtual only | Yes (inline) | Virtual only |

## Side-by-Side Examples

### Scenario 1: Order Processing

A business scenario with payment processing, inventory management, and notifications.

#### Successful Order

**Moq**

<!-- snippet: docs:framework-comparison:order-success-moq -->
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
<!-- /snippet -->

**KnockOff**

<!-- snippet: docs:framework-comparison:order-success-knockoff -->
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
<!-- /snippet -->

**Rocks**

<!-- snippet: docs:framework-comparison:order-success-rocks -->
```csharp
[Fact]
    public void OrderProcessing_Success_Rocks()
    {
        // Arrange
        var order = new FcOrder
        {
            Id = 1,
            CustomerId = 100,
            Amount = 99.99m,
            Items = [new FcOrderItem { ProductId = 1, Quantity = 2, UnitPrice = 49.99m }]
        };

        var orderRepoExpectations = new FcOrderRepositoryExpectations();
        orderRepoExpectations.Methods.GetById(Arg.Any<int>()).ReturnValue(order);
        orderRepoExpectations.Methods.Save(Arg.Any<FcOrder>()).ExpectedCallCount(1);

        var paymentExpectations = new FcPaymentServiceExpectations();
        paymentExpectations.Methods.ProcessPayment(Arg.Any<int>(), Arg.Any<decimal>())
            .ReturnValue(new FcPaymentResult { Success = true, TransactionId = "TXN-123" });

        var notificationExpectations = new FcNotificationServiceExpectations();
        notificationExpectations.Methods.SendOrderConfirmation(Arg.Any<int>(), Arg.Any<int>()).ExpectedCallCount(1);

        var inventoryExpectations = new FcInventoryServiceExpectations();
        inventoryExpectations.Methods.ReserveItems(Arg.Any<IEnumerable<FcOrderItem>>()).ReturnValue(true);

        var processor = new FcOrderProcessor(
            orderRepoExpectations.Instance(),
            paymentExpectations.Instance(),
            notificationExpectations.Instance(),
            inventoryExpectations.Instance());

        // Act
        var result = processor.ProcessOrder(1);

        // Assert
        Assert.True(result);
        orderRepoExpectations.Verify();
        notificationExpectations.Verify();
    }
```
<!-- /snippet -->

#### Payment Failure Handling

**Moq**

<!-- snippet: docs:framework-comparison:order-payment-failure-moq -->
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
<!-- /snippet -->

**KnockOff**

<!-- snippet: docs:framework-comparison:order-payment-failure-knockoff -->
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
<!-- /snippet -->

**Rocks**

<!-- snippet: docs:framework-comparison:order-payment-failure-rocks -->
```csharp
[Fact]
    public void OrderProcessing_PaymentFailure_Rocks()
    {
        // Arrange
        var order = new FcOrder
        {
            Id = 1,
            CustomerId = 100,
            Amount = 99.99m,
            Items = [new FcOrderItem { ProductId = 1, Quantity = 2, UnitPrice = 49.99m }]
        };

        var orderRepoExpectations = new FcOrderRepositoryExpectations();
        orderRepoExpectations.Methods.GetById(Arg.Any<int>()).ReturnValue(order);
        orderRepoExpectations.Methods.Save(Arg.Any<FcOrder>()).ExpectedCallCount(1);

        var paymentExpectations = new FcPaymentServiceExpectations();
        paymentExpectations.Methods.ProcessPayment(Arg.Any<int>(), Arg.Any<decimal>())
            .ReturnValue(new FcPaymentResult { Success = false, ErrorMessage = "Insufficient funds" });

        var notificationExpectations = new FcNotificationServiceExpectations();
        notificationExpectations.Methods.SendPaymentFailure(Arg.Any<int>(), Arg.Any<string>()).ExpectedCallCount(1);

        var inventoryExpectations = new FcInventoryServiceExpectations();
        inventoryExpectations.Methods.ReserveItems(Arg.Any<IEnumerable<FcOrderItem>>()).ReturnValue(true);
        inventoryExpectations.Methods.ReleaseItems(Arg.Any<IEnumerable<FcOrderItem>>()).ExpectedCallCount(1);

        var processor = new FcOrderProcessor(
            orderRepoExpectations.Instance(),
            paymentExpectations.Instance(),
            notificationExpectations.Instance(),
            inventoryExpectations.Instance());

        // Act
        var result = processor.ProcessOrder(1);

        // Assert
        Assert.False(result);
        orderRepoExpectations.Verify();
        notificationExpectations.Verify();
        inventoryExpectations.Verify();
    }
```
<!-- /snippet -->

### Scenario 2: Cached Repository

A data access scenario with async operations and caching.

#### Cache Hit

**Moq**

<!-- snippet: docs:framework-comparison:cache-hit-moq -->
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
<!-- /snippet -->

**KnockOff**

<!-- snippet: docs:framework-comparison:cache-hit-knockoff -->
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
<!-- /snippet -->

**Rocks**

<!-- snippet: docs:framework-comparison:cache-hit-rocks -->
```csharp
[Fact]
    public async Task CachedRepository_CacheHit_Rocks()
    {
        // Arrange
        var cachedProduct = new FcProduct { Id = 1, Name = "Widget", Price = 19.99m };

        var repositoryExpectations = new FcProductRepositoryExpectations();
        // No setup needed - should never be called

        var cacheExpectations = new FcCacheServiceExpectations();
        cacheExpectations.Methods.Get<FcProduct>(Arg.Any<string>()).ReturnValue(cachedProduct);

        var loggerExpectations = new FcLoggerExpectations();
        loggerExpectations.Methods.LogInfo(Arg.Any<string>()).ExpectedCallCount(1);

        var service = new FcCachedProductService(
            repositoryExpectations.Instance(),
            cacheExpectations.Instance(),
            loggerExpectations.Instance());

        // Act
        var result = await service.GetProductAsync(1);

        // Assert
        Assert.Equal("Widget", result?.Name);
        cacheExpectations.Verify();
        loggerExpectations.Verify();
    }
```
<!-- /snippet -->

#### Cache Miss

**Moq**

<!-- snippet: docs:framework-comparison:cache-miss-moq -->
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
<!-- /snippet -->

**KnockOff**

<!-- snippet: docs:framework-comparison:cache-miss-knockoff -->
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
<!-- /snippet -->

**Rocks**

<!-- snippet: docs:framework-comparison:cache-miss-rocks -->
```csharp
[Fact]
    public async Task CachedRepository_CacheMiss_Rocks()
    {
        // Arrange
        var product = new FcProduct { Id = 1, Name = "Widget", Price = 19.99m };

        var repositoryExpectations = new FcProductRepositoryExpectations();
        repositoryExpectations.Methods.GetByIdAsync(Arg.Any<int>())
            .ReturnValue(Task.FromResult<FcProduct?>(product));

        var cacheExpectations = new FcCacheServiceExpectations();
        cacheExpectations.Methods.Get<FcProduct>(Arg.Any<string>()).ReturnValue(null);
        cacheExpectations.Methods.Set(Arg.Any<string>(), Arg.Any<FcProduct>(), Arg.Any<TimeSpan>()).ExpectedCallCount(1);

        var loggerExpectations = new FcLoggerExpectations();
        loggerExpectations.Methods.LogInfo(Arg.Any<string>()).ExpectedCallCount(1);

        var service = new FcCachedProductService(
            repositoryExpectations.Instance(),
            cacheExpectations.Instance(),
            loggerExpectations.Instance());

        // Act
        var result = await service.GetProductAsync(1);

        // Assert
        Assert.Equal("Widget", result?.Name);
        repositoryExpectations.Verify();
        cacheExpectations.Verify();
        loggerExpectations.Verify();
    }
```
<!-- /snippet -->

## Key Differences Illustrated

### Setup Style

| Framework | Pattern |
|-----------|---------|
| Moq | `mock.Setup(x => x.Method()).Returns(value)` |
| KnockOff | `stub.Method.OnCall = (ko, args) => value` |
| Rocks | `expectations.Methods.Method().ReturnValue(value)` |

### Verification Style

| Framework | Pattern |
|-----------|---------|
| Moq | `mock.Verify(x => x.Method(), Times.Once)` |
| KnockOff | `Assert.Equal(1, stub.Method.CallCount)` |
| Rocks | `expectations.Verify()` (checks all expectations) |

### Argument Access

| Framework | Pattern |
|-----------|---------|
| Moq | Callback capture: `Callback<T>(x => captured = x)` |
| KnockOff | Automatic: `stub.Method.LastCallArg` or `LastCallArgs` |
| Rocks | Callback capture in handler |

## When to Use Each

### Choose Moq When

- Team is already experienced with Moq
- Need `VerifyNoOtherCalls()` strict verification
- Want extensive community support and documentation
- Prototyping where compile-time isn't a concern

### Choose KnockOff When

- Stubs are reused across many tests
- Prefer compile-time errors over runtime failures
- Want automatic argument capture without callbacks
- Debugging generated code is easier than expression trees
- Want minimal ceremony for simple interface stubs

### Choose Rocks When

- Want compile-time generation with strict expectations
- Prefer expectation-based verification (setup counts upfront)
- Need explicit verification that all expected calls occurred
- Want generated code without runtime proxy overhead

## Migration

- **From Moq to KnockOff**: See [Migration from Moq](migration-from-moq.md)
- **From Moq to Rocks**: Replace `Mock<T>` with `[RockPartial]` expectations classes
