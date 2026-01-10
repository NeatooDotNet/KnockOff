using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Moq;
using Rocks;

#pragma warning disable CA1716 // Identifiers should not match keywords (Get/Set are standard cache method names)
#pragma warning disable CA2007 // ConfigureAwait - benchmarks run single-threaded
#pragma warning disable CA1307 // StringComparison - benchmark assertion code

namespace KnockOff.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks matching the exact scenarios from docs/framework-comparison.md.
/// These measure realistic multi-dependency test scenarios.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class FrameworkComparisonBenchmarks
{
    // ========================================================================
    // Scenario 1: Order Processing - Success Path
    // 4 dependencies: order repo, payment service, notification, inventory
    // ========================================================================

    [Benchmark(Baseline = true)]
    public bool OrderProcessing_Success_Moq()
    {
        // Arrange
        var order = new FcBenchOrder
        {
            Id = 1,
            CustomerId = 100,
            Amount = 99.99m,
            Items = [new FcBenchOrderItem { ProductId = 1, Quantity = 2, UnitPrice = 49.99m }]
        };

        var orderRepo = new Mock<IFcBenchOrderRepository>();
        orderRepo.Setup(x => x.GetById(1)).Returns(order);

        var paymentService = new Mock<IFcBenchPaymentService>();
        paymentService
            .Setup(x => x.ProcessPayment(100, 99.99m))
            .Returns(new FcBenchPaymentResult { Success = true, TransactionId = "TXN-123" });

        var notificationService = new Mock<IFcBenchNotificationService>();
        var inventoryService = new Mock<IFcBenchInventoryService>();
        inventoryService
            .Setup(x => x.ReserveItems(It.IsAny<IEnumerable<FcBenchOrderItem>>()))
            .Returns(true);

        var processor = new FcBenchOrderProcessor(
            orderRepo.Object,
            paymentService.Object,
            notificationService.Object,
            inventoryService.Object);

        // Act
        var result = processor.ProcessOrder(1);

        // Assert
        orderRepo.Verify(x => x.Save(It.Is<FcBenchOrder>(o => o.Status == "Completed")), Times.Once);
        notificationService.Verify(x => x.SendOrderConfirmation(100, 1), Times.Once);
        inventoryService.Verify(x => x.ReleaseItems(It.IsAny<IEnumerable<FcBenchOrderItem>>()), Times.Never);

        return result;
    }

    [Benchmark]
    public bool OrderProcessing_Success_KnockOff()
    {
        // Arrange
        var order = new FcBenchOrder
        {
            Id = 1,
            CustomerId = 100,
            Amount = 99.99m,
            Items = [new FcBenchOrderItem { ProductId = 1, Quantity = 2, UnitPrice = 49.99m }]
        };

        var orderRepo = new FcBenchOrderRepositoryStub();
        orderRepo.GetById.OnCall = (ko, id) => order;

        var paymentService = new FcBenchPaymentServiceStub();
        paymentService.ProcessPayment.OnCall = (ko, customerId, amount) =>
            new FcBenchPaymentResult { Success = true, TransactionId = "TXN-123" };

        var notificationService = new FcBenchNotificationServiceStub();

        var inventoryService = new FcBenchInventoryServiceStub();
        inventoryService.ReserveItems.OnCall = (ko, items) => true;

        var processor = new FcBenchOrderProcessor(
            orderRepo,
            paymentService,
            notificationService,
            inventoryService);

        // Act
        var result = processor.ProcessOrder(1);

        // Assert
        _ = orderRepo.Save.CallCount == 1;
        _ = orderRepo.Save.LastCallArg?.Status == "Completed";
        _ = notificationService.SendOrderConfirmation.CallCount == 1;
        _ = inventoryService.ReleaseItems.CallCount == 0;

        return result;
    }

    [Benchmark]
    public bool OrderProcessing_Success_Rocks()
    {
        // Arrange
        var order = new FcBenchOrder
        {
            Id = 1,
            CustomerId = 100,
            Amount = 99.99m,
            Items = [new FcBenchOrderItem { ProductId = 1, Quantity = 2, UnitPrice = 49.99m }]
        };

        var orderRepoExpectations = new IFcBenchOrderRepositoryCreateExpectations();
        orderRepoExpectations.Methods.GetById(Arg.Any<int>()).ReturnValue(order);
        orderRepoExpectations.Methods.Save(Arg.Any<FcBenchOrder>()).ExpectedCallCount(1);

        var paymentExpectations = new IFcBenchPaymentServiceCreateExpectations();
        paymentExpectations.Methods.ProcessPayment(Arg.Any<int>(), Arg.Any<decimal>())
            .ReturnValue(new FcBenchPaymentResult { Success = true, TransactionId = "TXN-123" });

        var notificationExpectations = new IFcBenchNotificationServiceCreateExpectations();
        notificationExpectations.Methods.SendOrderConfirmation(Arg.Any<int>(), Arg.Any<int>()).ExpectedCallCount(1);

        var inventoryExpectations = new IFcBenchInventoryServiceCreateExpectations();
        inventoryExpectations.Methods.ReserveItems(Arg.Any<IEnumerable<FcBenchOrderItem>>()).ReturnValue(true);

        var processor = new FcBenchOrderProcessor(
            orderRepoExpectations.Instance(),
            paymentExpectations.Instance(),
            notificationExpectations.Instance(),
            inventoryExpectations.Instance());

        // Act
        var result = processor.ProcessOrder(1);

        // Assert
        orderRepoExpectations.Verify();
        notificationExpectations.Verify();

        return result;
    }

    // ========================================================================
    // Scenario 2: Cached Repository - Cache Miss (more complex path)
    // 3 dependencies: repository, cache, logger
    // ========================================================================

    [Benchmark]
    public async Task<FcBenchProduct?> CachedRepository_CacheMiss_Moq()
    {
        // Arrange
        var product = new FcBenchProduct { Id = 1, Name = "Widget", Price = 19.99m };

        var repository = new Mock<IFcBenchProductRepository>();
        repository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(product);

        var cache = new Mock<IFcBenchCacheService>();
        cache.Setup(x => x.Get<FcBenchProduct>("product:1")).Returns((FcBenchProduct?)null);

        var logger = new Mock<IFcBenchLogger>();

        var service = new FcBenchCachedProductService(
            repository.Object,
            cache.Object,
            logger.Object);

        // Act
        var result = await service.GetProductAsync(1);

        // Assert
        repository.Verify(x => x.GetByIdAsync(1), Times.Once);
        cache.Verify(x => x.Set("product:1", product, It.IsAny<TimeSpan>()), Times.Once);
        logger.Verify(x => x.LogInfo(It.Is<string>(s => s.Contains("Cache miss"))), Times.Once);

        return result;
    }

    [Benchmark]
    public async Task<FcBenchProduct?> CachedRepository_CacheMiss_KnockOff()
    {
        // Arrange
        var product = new FcBenchProduct { Id = 1, Name = "Widget", Price = 19.99m };

        var repository = new FcBenchProductRepositoryStub();
        repository.GetByIdAsync.OnCall = (ko, id) => Task.FromResult<FcBenchProduct?>(product);

        var cache = new FcBenchCacheServiceStub();
        cache.Get.Of<FcBenchProduct>().OnCall = (ko, key) => null;

        var logger = new FcBenchLoggerStub();

        var service = new FcBenchCachedProductService(repository, cache, logger);

        // Act
        var result = await service.GetProductAsync(1);

        // Assert
        _ = repository.GetByIdAsync.CallCount == 1;
        _ = cache.Set.Of<FcBenchProduct>().CallCount == 1;
        _ = logger.LogInfo.LastCallArg?.Contains("Cache miss");

        return result;
    }

    [Benchmark]
    public async Task<FcBenchProduct?> CachedRepository_CacheMiss_Rocks()
    {
        // Arrange
        var product = new FcBenchProduct { Id = 1, Name = "Widget", Price = 19.99m };

        var repositoryExpectations = new IFcBenchProductRepositoryCreateExpectations();
        repositoryExpectations.Methods.GetByIdAsync(Arg.Any<int>())
            .ReturnValue(Task.FromResult<FcBenchProduct?>(product));

        var cacheExpectations = new IFcBenchCacheServiceCreateExpectations();
        cacheExpectations.Methods.Get<FcBenchProduct>(Arg.Any<string>()).ReturnValue(null);
        cacheExpectations.Methods.Set(Arg.Any<string>(), Arg.Any<FcBenchProduct>(), Arg.Any<TimeSpan>()).ExpectedCallCount(1);

        var loggerExpectations = new IFcBenchLoggerCreateExpectations();
        loggerExpectations.Methods.LogInfo(Arg.Any<string>()).ExpectedCallCount(1);

        var service = new FcBenchCachedProductService(
            repositoryExpectations.Instance(),
            cacheExpectations.Instance(),
            loggerExpectations.Instance());

        // Act
        var result = await service.GetProductAsync(1);

        // Assert
        repositoryExpectations.Verify();
        cacheExpectations.Verify();
        loggerExpectations.Verify();

        return result;
    }
}

// ============================================================================
// Domain Types - Order Processing (matches framework-comparison samples)
// ============================================================================

public class FcBenchOrder
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
    public string? Status { get; set; }
    public List<FcBenchOrderItem> Items { get; set; } = [];
}

public class FcBenchOrderItem
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class FcBenchPaymentResult
{
    public bool Success { get; set; }
    public string? TransactionId { get; set; }
    public string? ErrorMessage { get; set; }
}

// ============================================================================
// Interfaces - Order Processing
// ============================================================================

public interface IFcBenchOrderRepository
{
    FcBenchOrder? GetById(int id);
    void Save(FcBenchOrder order);
}

public interface IFcBenchPaymentService
{
    FcBenchPaymentResult ProcessPayment(int customerId, decimal amount);
}

public interface IFcBenchNotificationService
{
    void SendOrderConfirmation(int customerId, int orderId);
    void SendPaymentFailure(int customerId, string reason);
}

public interface IFcBenchInventoryService
{
    bool ReserveItems(IEnumerable<FcBenchOrderItem> items);
    void ReleaseItems(IEnumerable<FcBenchOrderItem> items);
}

// ============================================================================
// KnockOff Stubs - Order Processing
// ============================================================================

[KnockOff]
public partial class FcBenchOrderRepositoryStub : IFcBenchOrderRepository { }

[KnockOff]
public partial class FcBenchPaymentServiceStub : IFcBenchPaymentService { }

[KnockOff]
public partial class FcBenchNotificationServiceStub : IFcBenchNotificationService { }

[KnockOff]
public partial class FcBenchInventoryServiceStub : IFcBenchInventoryService { }

// ============================================================================
// System Under Test - Order Processing
// ============================================================================

public class FcBenchOrderProcessor
{
    private readonly IFcBenchOrderRepository _orderRepository;
    private readonly IFcBenchPaymentService _paymentService;
    private readonly IFcBenchNotificationService _notificationService;
    private readonly IFcBenchInventoryService _inventoryService;

    public FcBenchOrderProcessor(
        IFcBenchOrderRepository orderRepository,
        IFcBenchPaymentService paymentService,
        IFcBenchNotificationService notificationService,
        IFcBenchInventoryService inventoryService)
    {
        _orderRepository = orderRepository;
        _paymentService = paymentService;
        _notificationService = notificationService;
        _inventoryService = inventoryService;
    }

    public bool ProcessOrder(int orderId)
    {
        var order = _orderRepository.GetById(orderId);
        if (order == null)
            return false;

        // Reserve inventory
        if (!_inventoryService.ReserveItems(order.Items))
        {
            order.Status = "InventoryUnavailable";
            _orderRepository.Save(order);
            return false;
        }

        // Process payment
        var paymentResult = _paymentService.ProcessPayment(order.CustomerId, order.Amount);
        if (!paymentResult.Success)
        {
            _inventoryService.ReleaseItems(order.Items);
            _notificationService.SendPaymentFailure(order.CustomerId, paymentResult.ErrorMessage ?? "Unknown error");
            order.Status = "PaymentFailed";
            _orderRepository.Save(order);
            return false;
        }

        // Success
        order.Status = "Completed";
        _orderRepository.Save(order);
        _notificationService.SendOrderConfirmation(order.CustomerId, order.Id);
        return true;
    }
}

// ============================================================================
// Domain Types - Repository with Caching
// ============================================================================

public class FcBenchProduct
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockCount { get; set; }
}

// ============================================================================
// Interfaces - Repository with Caching
// ============================================================================

public interface IFcBenchProductRepository
{
    Task<FcBenchProduct?> GetByIdAsync(int id);
    Task<IEnumerable<FcBenchProduct>> GetAllAsync();
    Task SaveAsync(FcBenchProduct product);
    Task DeleteAsync(int id);
}

public interface IFcBenchCacheService
{
    T? Get<T>(string key) where T : class;
    void Set<T>(string key, T value, TimeSpan expiration) where T : class;
    void Remove(string key);
}

public interface IFcBenchLogger
{
    void LogInfo(string message);
    void LogWarning(string message);
    void LogError(string message, Exception? exception = null);
}

// ============================================================================
// KnockOff Stubs - Repository with Caching
// ============================================================================

[KnockOff]
public partial class FcBenchProductRepositoryStub : IFcBenchProductRepository { }

[KnockOff]
public partial class FcBenchCacheServiceStub : IFcBenchCacheService { }

[KnockOff]
public partial class FcBenchLoggerStub : IFcBenchLogger { }

// ============================================================================
// System Under Test - Cached Product Service
// ============================================================================

public class FcBenchCachedProductService
{
    private readonly IFcBenchProductRepository _repository;
    private readonly IFcBenchCacheService _cache;
    private readonly IFcBenchLogger _logger;
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

    public FcBenchCachedProductService(
        IFcBenchProductRepository repository,
        IFcBenchCacheService cache,
        IFcBenchLogger logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<FcBenchProduct?> GetProductAsync(int id)
    {
        var cacheKey = $"product:{id}";

        // Try cache first
        var cached = _cache.Get<FcBenchProduct>(cacheKey);
        if (cached != null)
        {
            _logger.LogInfo($"Cache hit for product {id}");
            return cached;
        }

        // Cache miss - fetch from repository
        _logger.LogInfo($"Cache miss for product {id}, fetching from repository");
        var product = await _repository.GetByIdAsync(id);

        if (product != null)
        {
            _cache.Set(cacheKey, product, CacheExpiration);
        }

        return product;
    }
}
