using Rocks;

/// <summary>
/// Code samples for docs comparing Moq vs KnockOff vs Rocks.
///
/// This file contains:
/// - Domain interfaces and classes for realistic scenarios
/// - KnockOff stub definitions
/// - Rocks assembly attributes for source generation
///
/// Corresponding tests: FrameworkComparisonSamplesTests.cs
/// </summary>

namespace KnockOff.Documentation.Samples.Comparison;

// ============================================================================
// Scenario 1: Order Processing
// A business scenario with validation, payment, and notification services
// ============================================================================

#region Domain Types - Order Processing

public class FcOrder
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
    public string? Status { get; set; }
    public List<FcOrderItem> Items { get; set; } = [];
}

public class FcOrderItem
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class FcPaymentResult
{
    public bool Success { get; set; }
    public string? TransactionId { get; set; }
    public string? ErrorMessage { get; set; }
}

#endregion

#region Interfaces - Order Processing

public interface IFcOrderRepository
{
    FcOrder? GetById(int id);
    void Save(FcOrder order);
}

public interface IFcPaymentService
{
    FcPaymentResult ProcessPayment(int customerId, decimal amount);
}

public interface IFcNotificationService
{
    void SendOrderConfirmation(int customerId, int orderId);
    void SendPaymentFailure(int customerId, string reason);
}

public interface IFcInventoryService
{
    bool ReserveItems(IEnumerable<FcOrderItem> items);
    void ReleaseItems(IEnumerable<FcOrderItem> items);
}

#endregion

#region KnockOff Stubs - Order Processing

[KnockOff]
public partial class FcOrderRepositoryStub : IFcOrderRepository { }

[KnockOff]
public partial class FcPaymentServiceStub : IFcPaymentService { }

[KnockOff]
public partial class FcNotificationServiceStub : IFcNotificationService { }

[KnockOff]
public partial class FcInventoryServiceStub : IFcInventoryService { }

#endregion

#region Rocks Expectations - Order Processing

[RockPartial(typeof(IFcOrderRepository), BuildType.Create)]
public sealed partial class FcOrderRepositoryExpectations;

[RockPartial(typeof(IFcPaymentService), BuildType.Create)]
public sealed partial class FcPaymentServiceExpectations;

[RockPartial(typeof(IFcNotificationService), BuildType.Create)]
public sealed partial class FcNotificationServiceExpectations;

[RockPartial(typeof(IFcInventoryService), BuildType.Create)]
public sealed partial class FcInventoryServiceExpectations;

#endregion

#region System Under Test - Order Processing

/// <summary>
/// The class we're testing - processes orders using multiple dependencies.
/// </summary>
public class FcOrderProcessor
{
    private readonly IFcOrderRepository _orderRepository;
    private readonly IFcPaymentService _paymentService;
    private readonly IFcNotificationService _notificationService;
    private readonly IFcInventoryService _inventoryService;

    public FcOrderProcessor(
        IFcOrderRepository orderRepository,
        IFcPaymentService paymentService,
        IFcNotificationService notificationService,
        IFcInventoryService inventoryService)
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

#endregion

// ============================================================================
// Scenario 2: Async Repository with Caching
// A data access scenario with async operations and caching
// ============================================================================

#region Domain Types - Repository with Caching

public class FcProduct
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockCount { get; set; }
}

#endregion

#region Interfaces - Repository with Caching

public interface IFcProductRepository
{
    Task<FcProduct?> GetByIdAsync(int id);
    Task<IEnumerable<FcProduct>> GetAllAsync();
    Task SaveAsync(FcProduct product);
    Task DeleteAsync(int id);
}

public interface IFcCacheService
{
    T? Get<T>(string key) where T : class;
    void Set<T>(string key, T value, TimeSpan expiration) where T : class;
    void Remove(string key);
}

public interface IFcLogger
{
    void LogInfo(string message);
    void LogWarning(string message);
    void LogError(string message, Exception? exception = null);
}

#endregion

#region KnockOff Stubs - Repository with Caching

[KnockOff]
public partial class FcProductRepositoryStub : IFcProductRepository { }

[KnockOff]
public partial class FcCacheServiceStub : IFcCacheService { }

[KnockOff]
public partial class FcLoggerStub : IFcLogger { }

#endregion

#region Rocks Expectations - Repository with Caching

[RockPartial(typeof(IFcProductRepository), BuildType.Create)]
public sealed partial class FcProductRepositoryExpectations;

[RockPartial(typeof(IFcCacheService), BuildType.Create)]
public sealed partial class FcCacheServiceExpectations;

[RockPartial(typeof(IFcLogger), BuildType.Create)]
public sealed partial class FcLoggerExpectations;

#endregion

#region System Under Test - Repository with Caching

/// <summary>
/// A cached product service that we're testing.
/// </summary>
public class FcCachedProductService
{
    private readonly IFcProductRepository _repository;
    private readonly IFcCacheService _cache;
    private readonly IFcLogger _logger;
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

    public FcCachedProductService(
        IFcProductRepository repository,
        IFcCacheService cache,
        IFcLogger logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<FcProduct?> GetProductAsync(int id)
    {
        var cacheKey = $"product:{id}";

        // Try cache first
        var cached = _cache.Get<FcProduct>(cacheKey);
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

    public async Task UpdateProductAsync(FcProduct product)
    {
        await _repository.SaveAsync(product);

        // Invalidate cache
        var cacheKey = $"product:{product.Id}";
        _cache.Remove(cacheKey);
        _logger.LogInfo($"Cache invalidated for product {product.Id}");
    }
}

#endregion
