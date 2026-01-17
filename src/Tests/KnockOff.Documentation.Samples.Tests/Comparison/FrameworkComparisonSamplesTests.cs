using KnockOff.Documentation.Samples.Comparison;
using Moq;
using Rocks;

namespace KnockOff.Documentation.Samples.Tests.Comparison;

/// <summary>
/// Side-by-side comparison tests showing the same scenarios implemented with
/// Moq, KnockOff, and Rocks.
/// </summary>
[Trait("Category", "Documentation")]
[Trait("Category", "FrameworkComparison")]
public class FrameworkComparisonSamplesTests
{
    // ========================================================================
    // README Side-by-Side Example
    // Simple example for the README introduction
    // ========================================================================

    #region readme-side-by-side-moq
    [Fact]
    public void OrderProcessor_ProcessesValidOrder_Moq()
    {
        // Arrange
        var mock = new Mock<IOrderService>();
        mock.Setup(x => x.GetOrder(It.IsAny<int>()))
            .Returns((int id) => new Order { Id = id, CustomerId = 1 });
        mock.Setup(x => x.ValidateOrder(It.IsAny<Order>())).Returns(true);
        mock.Setup(x => x.CalculateTotal(It.IsAny<Order>())).Returns(100m);

        var sut = new OrderProcessor(mock.Object);

        // Act
        sut.Process(1);

        // Assert
        mock.Verify(x => x.GetOrder(1), Moq.Times.Once());
        mock.Verify(x => x.ValidateOrder(It.IsAny<Order>()), Moq.Times.Once());
        mock.Verify(x => x.SaveOrder(It.IsAny<Order>()), Moq.Times.Once());
    }
    #endregion

    #region readme-side-by-side-knockoff
    [Fact]
    public void OrderProcessor_ProcessesValidOrder_KnockOff()
    {
        // Arrange
        var stub = new OrderServiceStub();
        var getOrderTracking = stub.GetOrder.OnCall((ko, id) => new Order { Id = id, CustomerId = 1 });
        var validateTracking = stub.ValidateOrder.OnCall((ko, _) => true);
        stub.CalculateTotal.OnCall((ko, _) => 100m);
        var saveTracking = stub.SaveOrder.OnCall((ko, _) => { });

        var sut = new OrderProcessor(stub);

        // Act
        sut.Process(1);

        // Assert
        Assert.Equal(1, getOrderTracking.CallCount);
        Assert.Equal(1, validateTracking.CallCount);
        Assert.Equal(1, saveTracking.CallCount);
    }
    #endregion

    // ========================================================================
    // Scenario 1: Order Processing - Successful Order (Complex)
    // Tests a happy path where order processing succeeds
    // ========================================================================

    #region framework-comparison-order-success-moq
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
        orderRepo.Verify(x => x.Save(It.Is<FcOrder>(o => o.Status == "Completed")), Moq.Times.Once());
        notificationService.Verify(x => x.SendOrderConfirmation(100, 1), Moq.Times.Once());
        inventoryService.Verify(x => x.ReleaseItems(It.IsAny<IEnumerable<FcOrderItem>>()), Moq.Times.Never());
    }
    #endregion

    #region framework-comparison-order-success-knockoff
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
        orderRepo.GetById.OnCall((ko, id) => order);
        var saveTracking = orderRepo.Save.OnCall((ko, o) => { });

        var paymentService = new FcPaymentServiceStub();
        paymentService.ProcessPayment.OnCall((ko, customerId, amount) =>
            new FcPaymentResult { Success = true, TransactionId = "TXN-123" });

        var notificationService = new FcNotificationServiceStub();
        var notifTracking = notificationService.SendOrderConfirmation.OnCall((ko, custId, orderId) => { });

        var inventoryService = new FcInventoryServiceStub();
        inventoryService.ReserveItems.OnCall((ko, items) => true);
        var releaseTracking = inventoryService.ReleaseItems.OnCall((ko, items) => { });

        var processor = new FcOrderProcessor(
            orderRepo,
            paymentService,
            notificationService,
            inventoryService);

        // Act
        var result = processor.ProcessOrder(1);

        // Assert
        Assert.True(result);
        Assert.Equal(1, saveTracking.CallCount);
        Assert.Equal("Completed", saveTracking.LastArg?.Status);
        Assert.Equal(1, notifTracking.CallCount);
        Assert.Equal(0, releaseTracking.CallCount);
    }
    #endregion

    #region framework-comparison-order-success-rocks
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
    #endregion

    // ========================================================================
    // Scenario 1: Order Processing - Payment Failure
    // Tests error handling when payment fails
    // ========================================================================

    #region framework-comparison-order-payment-failure-moq
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
            Moq.Times.Once());
        inventoryService.Verify(
            x => x.ReleaseItems(It.IsAny<IEnumerable<FcOrderItem>>()),
            Moq.Times.Once());
        orderRepo.Verify(
            x => x.Save(It.Is<FcOrder>(o => o.Status == "PaymentFailed")),
            Moq.Times.Once());
    }
    #endregion

    #region framework-comparison-order-payment-failure-knockoff
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
        orderRepo.GetById.OnCall((ko, id) => order);
        var saveTracking = orderRepo.Save.OnCall((ko, o) => { });

        var paymentService = new FcPaymentServiceStub();
        paymentService.ProcessPayment.OnCall((ko, customerId, amount) =>
            new FcPaymentResult { Success = false, ErrorMessage = "Insufficient funds" });

        var notificationService = new FcNotificationServiceStub();
        var notifTracking = notificationService.SendPaymentFailure.OnCall((ko, custId, msg) => { });

        var inventoryService = new FcInventoryServiceStub();
        inventoryService.ReserveItems.OnCall((ko, items) => true);
        var releaseTracking = inventoryService.ReleaseItems.OnCall((ko, items) => { });

        var processor = new FcOrderProcessor(
            orderRepo,
            paymentService,
            notificationService,
            inventoryService);

        // Act
        var result = processor.ProcessOrder(1);

        // Assert
        Assert.False(result);
        Assert.Equal(1, notifTracking.CallCount);
        Assert.Equal((100, "Insufficient funds"), notifTracking.LastArgs);
        Assert.Equal(1, releaseTracking.CallCount);
        Assert.Equal("PaymentFailed", saveTracking.LastArg?.Status);
    }
    #endregion

    #region framework-comparison-order-payment-failure-rocks
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
    #endregion

    // ========================================================================
    // Scenario 2: Cached Repository - Cache Hit
    // Tests that cached data is returned without hitting the repository
    // ========================================================================

    #region framework-comparison-cache-hit-moq
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
        repository.Verify(x => x.GetByIdAsync(It.IsAny<int>()), Moq.Times.Never());
        logger.Verify(x => x.LogInfo(It.Is<string>(s => s.Contains("Cache hit"))), Moq.Times.Once());
    }
    #endregion

    #region framework-comparison-cache-hit-knockoff
    [Fact]
    public async Task CachedRepository_CacheHit_KnockOff()
    {
        // Arrange
        var cachedProduct = new FcProduct { Id = 1, Name = "Widget", Price = 19.99m };

        var repository = new FcProductRepositoryStub();
        var repoTracking = repository.GetByIdAsync.OnCall((ko, id) => Task.FromResult<FcProduct?>(null));

        var cache = new FcCacheServiceStub();
        cache.Get.Of<FcProduct>().OnCall = (ko, key) => cachedProduct;

        var logger = new FcLoggerStub();
        var logTracking = logger.LogInfo.OnCall((ko, message) => { });

        var service = new FcCachedProductService(repository, cache, logger);

        // Act
        var result = await service.GetProductAsync(1);

        // Assert
        Assert.Equal("Widget", result?.Name);
        Assert.Equal(0, repoTracking.CallCount);
        Assert.True(logTracking.LastArg?.Contains("Cache hit"));
    }
    #endregion

    #region framework-comparison-cache-hit-rocks
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
    #endregion

    // ========================================================================
    // Scenario 2: Cached Repository - Cache Miss
    // Tests that repository is called on cache miss and result is cached
    // ========================================================================

    #region framework-comparison-cache-miss-moq
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
        repository.Verify(x => x.GetByIdAsync(1), Moq.Times.Once());
        cache.Verify(
            x => x.Set("product:1", product, It.IsAny<TimeSpan>()),
            Moq.Times.Once());
        logger.Verify(
            x => x.LogInfo(It.Is<string>(s => s.Contains("Cache miss"))),
            Moq.Times.Once());
    }
    #endregion

    #region framework-comparison-cache-miss-knockoff
    [Fact]
    public async Task CachedRepository_CacheMiss_KnockOff()
    {
        // Arrange
        var product = new FcProduct { Id = 1, Name = "Widget", Price = 19.99m };

        var repository = new FcProductRepositoryStub();
        var repoTracking = repository.GetByIdAsync.OnCall((ko, id) => Task.FromResult<FcProduct?>(product));

        var cache = new FcCacheServiceStub();
        cache.Get.Of<FcProduct>().OnCall = (ko, key) => null;

        var logger = new FcLoggerStub();
        var logTracking = logger.LogInfo.OnCall((ko, message) => { });

        var service = new FcCachedProductService(repository, cache, logger);

        // Act
        var result = await service.GetProductAsync(1);

        // Assert
        Assert.Equal("Widget", result?.Name);
        Assert.Equal(1, repoTracking.CallCount);
        Assert.Equal(1, cache.Set.Of<FcProduct>().CallCount);
        Assert.Equal("product:1", cache.Set.Of<FcProduct>().LastCallArg);
        Assert.True(logTracking.LastArg?.Contains("Cache miss"));
    }
    #endregion

    #region framework-comparison-cache-miss-rocks
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
    #endregion
}
