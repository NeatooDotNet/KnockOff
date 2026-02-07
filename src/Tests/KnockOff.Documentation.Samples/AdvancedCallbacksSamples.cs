namespace KnockOff.Documentation.Samples.AdvancedCallbacks;

// =============================================================================
// Interfaces for Advanced Callback Samples
// =============================================================================

public interface IEmailService
{
    bool Send(string to, string message);
}

public interface IRetryService
{
    bool Attempt();
}

public interface IUserRepository
{
    User? FindById(int id);
}

public interface IPaymentGateway
{
    void Charge(decimal amount);
}

public interface IConnection
{
    bool IsConnected { get; }
    void Connect();
}

public interface IDatabase
{
    void Initialize();
    string Query(string sql);
}

public interface IOrderService
{
    int PlaceOrder(Order order);
}

public interface ICache
{
    string? Get(string key);
    void Set(string key, string value);
    void Clear();
    CacheStats Stats { get; }
}

// =============================================================================
// Supporting Types
// =============================================================================

public class PaymentException : Exception
{
    public PaymentException(string message) : base(message) { }
}

public class CacheStats
{
    public int Hits { get; set; }
    public int Misses { get; set; }
}

// =============================================================================
// Stubs for Advanced Callback Samples
// =============================================================================

[KnockOff]
public partial class EmailServiceStub : IEmailService { }

[KnockOff]
public partial class RetryServiceStub : IRetryService { }

[KnockOff]
public partial class UserRepositoryStub : IUserRepository { }

[KnockOff]
public partial class PaymentGatewayStub : IPaymentGateway { }

[KnockOff]
public partial class ConnectionStub : IConnection { }

[KnockOff]
public partial class DatabaseStub : IDatabase { }

[KnockOff]
public partial class OrderServiceStub : IOrderService { }

[KnockOff]
public partial class CacheStub : ICache { }

// =============================================================================
// Sequential Returns - Queue Pattern
// =============================================================================

public class SequentialQueueTests
{
    [Fact]
    public void QueuePattern_ReturnsDifferentValuesOnSuccessiveCalls()
    {
        var stub = new EmailServiceStub();

        #region advanced-sequential-queue
        // Queue of results: first succeeds, second fails
        var results = new Queue<bool>(new[] { true, false });
        stub.Send.Return((to, message) => results.Dequeue());
        #endregion

        IEmailService service = stub;

        Assert.True(service.Send("user@test.com", "Welcome"));   // First call: success
        Assert.False(service.Send("user@test.com", "Reminder")); // Second call: failure
    }
}

// =============================================================================
// Sequential Returns - Counter Pattern
// =============================================================================

public class SequentialCounterTests
{
    [Fact]
    public void CounterPattern_SucceedsAfterMultipleFailures()
    {
        var stub = new RetryServiceStub();

        #region advanced-sequential-counter
        // Counter tracks call count for conditional behavior
        var attempts = 0;
        stub.Attempt.Return(() =>
        {
            attempts++;
            return attempts > 3; // Succeed on 4th attempt
        });
        #endregion

        IRetryService service = stub;

        Assert.False(service.Attempt()); // Attempt 1: fail
        Assert.False(service.Attempt()); // Attempt 2: fail
        Assert.False(service.Attempt()); // Attempt 3: fail
        Assert.True(service.Attempt());  // Attempt 4: success
    }
}

// =============================================================================
// Conditional Returns
// =============================================================================

public class ConditionalReturnsTests
{
    [Fact]
    public void SwitchExpression_ReturnsDifferentUsersById()
    {
        var stub = new UserRepositoryStub();

        #region advanced-conditional-switch
        // Pattern matching for argument-based return values
        stub.FindById.Return((id) => id switch
        {
            1 => new User { Id = 1, Name = "Admin", Email = "admin@test.com" },
            2 => new User { Id = 2, Name = "User", Email = "user@test.com" },
            _ => null
        });
        #endregion

        IUserRepository repository = stub;

        var admin = repository.FindById(1);
        Assert.NotNull(admin);
        Assert.Equal("Admin", admin.Name);

        var user = repository.FindById(2);
        Assert.NotNull(user);
        Assert.Equal("User", user.Name);

        var unknown = repository.FindById(999);
        Assert.Null(unknown);
    }
}

// =============================================================================
// Throwing Exceptions
// =============================================================================

public class ExceptionTests
{
    [Fact]
    public void ThrowException_WhenAmountExceedsLimit()
    {
        var stub = new PaymentGatewayStub();

        #region advanced-exception
        // Throw exceptions based on argument conditions
        stub.Charge.Call((amount) =>
        {
            if (amount > 1000)
                throw new PaymentException("Insufficient funds");
        });
        #endregion

        IPaymentGateway gateway = stub;

        // Valid amount works
        gateway.Charge(500);

        // Amount exceeding limit throws
        Assert.Throws<PaymentException>(() => gateway.Charge(1500));
    }
}

// =============================================================================
// State-Dependent Behavior - Property Depends on Method
// =============================================================================

public class StateDependentPropertyTests
{
    [Fact]
    public void Property_ReflectsMethodCallState()
    {
        var stub = new ConnectionStub();

        #region advanced-state-property
        // Shared state between property and method
        var isConnected = false;
        stub.IsConnected.Get(() => isConnected);
        stub.Connect.Call(() => { isConnected = true; });
        #endregion

        IConnection connection = stub;

        // Initially not connected
        Assert.False(connection.IsConnected);

        // After Connect(), IsConnected returns true
        connection.Connect();
        Assert.True(connection.IsConnected);
    }
}

// =============================================================================
// State-Dependent Behavior - Method Requires Initialization
// =============================================================================

public class StateDependentMethodTests
{
    [Fact]
    public void Method_ThrowsIfNotInitialized()
    {
        var stub = new DatabaseStub();

        #region advanced-state-method
        // Enforce method ordering with shared state
        var isInitialized = false;
        stub.Initialize.Call(() => { isInitialized = true; });
        stub.Query.Return((sql) =>
        {
            if (!isInitialized)
                throw new InvalidOperationException("Must call Initialize() first");
            return "result";
        });
        #endregion

        IDatabase database = stub;

        // Query throws before Initialize
        Assert.Throws<InvalidOperationException>(() => database.Query("SELECT * FROM users"));

        // After Initialize, Query works
        database.Initialize();
        var result = database.Query("SELECT * FROM users");
        Assert.Equal("result", result);
    }
}

// =============================================================================
// Side Effects
// =============================================================================

public class SideEffectsTests
{
    [Fact]
    public void Callback_PerformsMultipleActions()
    {
        var stub = new OrderServiceStub();

        var placedOrders = new List<Order>();
        var notifications = new List<string>();
        var nextOrderId = 100;

        #region advanced-side-effects
        // Callbacks can track state and perform side effects
        stub.PlaceOrder.Return((order) =>
        {
            placedOrders.Add(order);
            notifications.Add($"Order {nextOrderId} placed for user {order.UserId}");
            return nextOrderId++;
        });
        #endregion

        IOrderService service = stub;

        var orderId = service.PlaceOrder(new Order { UserId = 42, Amount = 99.99m });

        Assert.Equal(100, orderId);
        Assert.Single(placedOrders);
        Assert.Single(notifications);
        Assert.Contains("Order 100 placed for user 42", notifications);
    }
}

// =============================================================================
// Complete Example - Stateful Cache
// =============================================================================

public class CacheSimulationTests
{
    [Fact]
    public void Cache_SimulatesRealisticBehavior()
    {
        var stub = new CacheStub();

        // Internal state
        var cache = new Dictionary<string, (string Value, DateTime Added)>();
        var maxCapacity = 2;
        var expirationSeconds = 60;
        var hits = 0;
        var misses = 0;

        #region advanced-complete-example
        // Get: Check expiration, track hits/misses
        stub.Get.Return((key) =>
        {
            if (cache.TryGetValue(key, out var entry))
            {
                if ((DateTime.UtcNow - entry.Added).TotalSeconds < expirationSeconds)
                {
                    hits++;
                    return entry.Value;
                }
                cache.Remove(key); // Expired
            }
            misses++;
            return null;
        });

        // Set: Enforce capacity, evict oldest if needed
        stub.Set.Call((key, value) =>
        {
            if (cache.Count >= maxCapacity && !cache.ContainsKey(key))
            {
                var oldest = cache.OrderBy(kvp => kvp.Value.Added).First();
                cache.Remove(oldest.Key);
            }
            cache[key] = (value, DateTime.UtcNow);
        });

        // Clear: Reset everything
        stub.Clear.Call(() =>
        {
            cache.Clear();
            hits = 0;
            misses = 0;
        });

        // Stats: Return current counts
        stub.Stats.Get(() => new CacheStats { Hits = hits, Misses = misses });
        #endregion

        ICache cacheService = stub;

        // Miss on empty cache
        Assert.Null(cacheService.Get("key1"));
        Assert.Equal(1, cacheService.Stats.Misses);

        // Add items
        cacheService.Set("key1", "value1");
        cacheService.Set("key2", "value2");

        // Hit on cached item
        var value = cacheService.Get("key1");
        Assert.Equal("value1", value);
        Assert.Equal(1, cacheService.Stats.Hits);

        // Adding third item evicts oldest (key1)
        cacheService.Set("key3", "value3");
        Assert.Null(cacheService.Get("key1")); // Evicted

        // Clear resets state
        cacheService.Clear();
        Assert.Equal(0, cacheService.Stats.Hits);
        Assert.Equal(0, cacheService.Stats.Misses);
    }
}
