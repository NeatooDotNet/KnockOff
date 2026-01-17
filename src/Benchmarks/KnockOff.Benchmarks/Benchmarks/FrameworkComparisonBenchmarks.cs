using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using KnockOff.Benchmarks.Interfaces;
using KnockOff.Benchmarks.Stubs;
using Moq;
using Rocks;

#pragma warning disable CA1716 // Identifiers should not match keywords (Get/Set are standard cache method names)
#pragma warning disable CA2007 // ConfigureAwait - benchmarks run single-threaded
#pragma warning disable CA1307 // StringComparison - benchmark assertion code

namespace KnockOff.Benchmarks.Benchmarks;

/// <summary>
/// Benchmarks matching the exact scenarios from README.md.
/// These measure realistic test scenarios as shown in the documentation.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class FrameworkComparisonBenchmarks
{
    // ========================================================================
    // Scenario 1: Order Processing (matches README "Scenario 1: Order Processing")
    // Single IOrderService dependency with GetOrder, ValidateOrder, CalculateTotal, SaveOrder
    // ========================================================================

    [Benchmark(Baseline = true)]
    public void Moq_TypicalUnitTest()
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

    [Benchmark]
    public void KnockOff_TypicalUnitTest()
    {
        // Arrange
        var stub = new OrderServiceStub();
        var getOrderTracking = stub.GetOrder.OnCall((ko, id) => new Order { Id = id, CustomerId = 1 });
        var validateOrderTracking = stub.ValidateOrder.OnCall((ko, _) => true);
        stub.CalculateTotal.OnCall((ko, _) => 100m);
        var saveOrderTracking = stub.SaveOrder.OnCall((ko, _) => { });

        var sut = new OrderProcessor(stub);

        // Act
        sut.Process(1);

        // Assert
        _ = getOrderTracking.CallCount == 1;
        _ = validateOrderTracking.CallCount == 1;
        _ = saveOrderTracking.CallCount == 1;
    }

    [Benchmark]
    public void Rocks_TypicalUnitTest()
    {
        // Arrange
        var expectations = new IOrderServiceCreateExpectations();
        expectations.Methods.GetOrder(Arg.Any<int>())
            .Callback((int id) => new Order { Id = id, CustomerId = 1 });
        expectations.Methods.ValidateOrder(Arg.Any<Order>()).ReturnValue(true);
        expectations.Methods.CalculateTotal(Arg.Any<Order>()).ReturnValue(100m);
        expectations.Methods.SaveOrder(Arg.Any<Order>());

        var sut = new OrderProcessor(expectations.Instance());

        // Act
        sut.Process(1);

        // Assert
        expectations.Verify();
    }

    // ========================================================================
    // Scenario 2: Cached Repository - Cache Miss (matches README "Scenario 2")
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
        repository.Verify(x => x.GetByIdAsync(1), Moq.Times.Once());
        cache.Verify(x => x.Set("product:1", product, It.IsAny<TimeSpan>()), Moq.Times.Once());
        logger.Verify(x => x.LogInfo(It.Is<string>(s => s.Contains("Cache miss"))), Moq.Times.Once());

        return result;
    }

    [Benchmark]
    public async Task<FcBenchProduct?> CachedRepository_CacheMiss_KnockOff()
    {
        // Arrange
        var product = new FcBenchProduct { Id = 1, Name = "Widget", Price = 19.99m };

        var repository = new FcBenchProductRepositoryStub();
        var getByIdTracking = repository.GetByIdAsync.OnCall((ko, id) => Task.FromResult<FcBenchProduct?>(product));

        var cache = new FcBenchCacheServiceStub();
        cache.Get.Of<FcBenchProduct>().OnCall = (ko, key) => null;
        cache.Set.Of<FcBenchProduct>().OnCall = (ko, key, value, expiration) => { };

        var logger = new FcBenchLoggerStub();
        var logInfoTracking = logger.LogInfo.OnCall((ko, message) => { });

        var service = new FcBenchCachedProductService(repository, cache, logger);

        // Act
        var result = await service.GetProductAsync(1);

        // Assert
        _ = getByIdTracking.CallCount == 1;
        _ = cache.Set.Of<FcBenchProduct>().CallCount == 1;
        _ = logInfoTracking.LastArg?.Contains("Cache miss");

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
// Domain Types - Repository with Caching (matches README Scenario 2)
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
