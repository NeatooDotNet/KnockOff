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
    #region advanced-sequential-queue
    [Fact]
    public void QueuePattern_ReturnsDifferentValuesOnSuccessiveCalls()
    {
        var stub = new EmailServiceStub();

        // Queue of results: first succeeds, second fails
        var results = new Queue<bool>(new[] { true, false });

        var tracking = stub.Send.OnCall((to, message) => results.Dequeue());

        IEmailService service = stub;

        Assert.True(service.Send("user@test.com", "Welcome"));   // First call: success
        Assert.False(service.Send("user@test.com", "Reminder")); // Second call: failure
    }
    #endregion
}

// =============================================================================
// Sequential Returns - Counter Pattern
// =============================================================================

public class SequentialCounterTests
{
    #region advanced-sequential-counter
    [Fact]
    public void CounterPattern_SucceedsAfterMultipleFailures()
    {
        var stub = new RetryServiceStub();

        var attempts = 0;

        var tracking = stub.Attempt.OnCall(() =>
        {
            attempts++;
            return attempts > 3; // Succeed on 4th attempt
        });

        IRetryService service = stub;

        Assert.False(service.Attempt()); // Attempt 1: fail
        Assert.False(service.Attempt()); // Attempt 2: fail
        Assert.False(service.Attempt()); // Attempt 3: fail
        Assert.True(service.Attempt());  // Attempt 4: success
    }
    #endregion
}

// =============================================================================
// Conditional Returns
// =============================================================================

public class ConditionalReturnsTests
{
    #region advanced-conditional-switch
    [Fact]
    public void SwitchExpression_ReturnsDifferentUsersById()
    {
        var stub = new UserRepositoryStub();

        var tracking = stub.FindById.OnCall((id) => id switch
        {
            1 => new User { Id = 1, Name = "Admin", Email = "admin@test.com" },
            2 => new User { Id = 2, Name = "User", Email = "user@test.com" },
            _ => null
        });

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
    #endregion
}

// =============================================================================
// Throwing Exceptions
// =============================================================================

public class ExceptionTests
{
    #region advanced-exception
    [Fact]
    public void ThrowException_WhenAmountExceedsLimit()
    {
        var stub = new PaymentGatewayStub();

        var tracking = stub.Charge.OnCall((amount) =>
        {
            if (amount > 1000)
                throw new PaymentException("Insufficient funds");
        });

        IPaymentGateway gateway = stub;

        // Valid amount works
        gateway.Charge(500);

        // Amount exceeding limit throws
        Assert.Throws<PaymentException>(() => gateway.Charge(1500));
    }
    #endregion
}

// =============================================================================
// State-Dependent Behavior - Property Depends on Method
// =============================================================================

public class StateDependentPropertyTests
{
    #region advanced-state-property
    [Fact]
    public void Property_ReflectsMethodCallState()
    {
        var stub = new ConnectionStub();

        // Track connection state with local variable
        var isConnected = false;

        // IsConnected returns the tracked state
        stub.IsConnected.OnGet(() => isConnected);

        // Connect() updates the tracked state
        var connectTracking = stub.Connect.OnCall(() => { isConnected = true; });

        IConnection connection = stub;

        // Initially not connected
        Assert.False(connection.IsConnected);

        // After Connect(), IsConnected returns true
        connection.Connect();
        Assert.True(connection.IsConnected);
    }
    #endregion
}

// =============================================================================
// State-Dependent Behavior - Method Requires Initialization
// =============================================================================

public class StateDependentMethodTests
{
    #region advanced-state-method
    [Fact]
    public void Method_ThrowsIfNotInitialized()
    {
        var stub = new DatabaseStub();

        // Track initialization state with local variable
        var isInitialized = false;

        var initTracking = stub.Initialize.OnCall(() => { isInitialized = true; });

        var queryTracking = stub.Query.OnCall((sql) =>
        {
            if (!isInitialized)
                throw new InvalidOperationException("Must call Initialize() first");
            return "result";
        });

        IDatabase database = stub;

        // Query throws before Initialize
        Assert.Throws<InvalidOperationException>(() => database.Query("SELECT * FROM users"));

        // After Initialize, Query works
        database.Initialize();
        var result = database.Query("SELECT * FROM users");
        Assert.Equal("result", result);
    }
    #endregion
}

// =============================================================================
// Side Effects
// =============================================================================

public class SideEffectsTests
{
    #region advanced-side-effects
    [Fact]
    public void Callback_PerformsMultipleActions()
    {
        var stub = new OrderServiceStub();

        var placedOrders = new List<Order>();
        var notifications = new List<string>();
        var nextOrderId = 100;

        var tracking = stub.PlaceOrder.OnCall((order) =>
        {
            // Track the order
            placedOrders.Add(order);

            // Simulate notification
            notifications.Add($"Order {nextOrderId} placed for user {order.UserId}");

            // Return generated ID
            return nextOrderId++;
        });

        IOrderService service = stub;

        var orderId = service.PlaceOrder(new Order { UserId = 42, Amount = 99.99m });

        Assert.Equal(100, orderId);
        Assert.Single(placedOrders);
        Assert.Single(notifications);
        Assert.Contains("Order 100 placed for user 42", notifications);
    }
    #endregion
}

// =============================================================================
// Complete Example - Stateful Cache
// =============================================================================

public class CacheSimulationTests
{
    #region advanced-complete-example
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

        // Get: Check expiration, track hits/misses
        stub.Get.OnCall((key) =>
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
        stub.Set.OnCall((key, value) =>
        {
            if (cache.Count >= maxCapacity && !cache.ContainsKey(key))
            {
                // Evict oldest entry
                var oldest = cache.OrderBy(kvp => kvp.Value.Added).First();
                cache.Remove(oldest.Key);
            }
            cache[key] = (value, DateTime.UtcNow);
        });

        // Clear: Reset everything
        stub.Clear.OnCall(() =>
        {
            cache.Clear();
            hits = 0;
            misses = 0;
        });

        // Stats: Return current counts
        stub.Stats.OnGet(() => new CacheStats { Hits = hits, Misses = misses });

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
    #endregion
}
