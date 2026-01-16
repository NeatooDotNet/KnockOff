/// <summary>
/// Code samples for docs/why-knockoff/duality-pattern.md
/// </summary>

namespace KnockOff.Documentation.Samples.WhyKnockOff;

// ============================================================================
// Domain Types
// ============================================================================

public class DuUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class DuOrder
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string Status { get; set; } = string.Empty;
}

#region duality-user-repository-interface
public interface IDuUserRepository
{
    DuUser? GetById(int id);
    void Save(DuUser user);
}
#endregion

public interface IDuOrderRepository
{
    DuOrder? GetById(int id);
    void Save(DuOrder order);
    IEnumerable<DuOrder> GetByCustomer(int customerId);
}

// ============================================================================
// KnockOff Stubs
// ============================================================================

#region duality-user-repository-stub
[KnockOff]
public partial class DuUserRepositoryStub : IDuUserRepository
{
    // Default: return a test user for any ID
    protected DuUser? GetById(int id) => new DuUser { Id = id, Name = $"User-{id}" };

    // Save: no user method = default behavior (void returns nothing)
}
#endregion

#region duality-order-repository-stub
[KnockOff]
public partial class DuOrderRepositoryStub : IDuOrderRepository
{
    // Default: most tests need an order to exist
    protected DuOrder? GetById(int id) => new DuOrder
    {
        Id = id,
        CustomerId = 1,
        Status = "Pending"
    };

    // Default: return empty list (no orders for customer)
    protected IEnumerable<DuOrder> GetByCustomer(int customerId) => [];
}
#endregion

// ============================================================================
// Sample Usage
// ============================================================================

public static class DualitySamples
{
    #region duality-user-method-usage
    public static void UserMethodUsage()
    {
        var stub = new DuUserRepositoryStub();
        IDuUserRepository repo = stub;

        var user = repo.GetById(42);

        Assert.Equal(42, user?.Id);
        Assert.Equal("User-42", user?.Name);
    }
    #endregion

    #region duality-callback-override
    public static void CallbackOverride()
    {
        var stub = new DuUserRepositoryStub();

        // Override the user method for just this test
        stub.GetById2.OnCall = (ko, id) => null;

        var user = stub.Object.GetById(999);

        Assert.Null(user);
    }
    #endregion

    #region duality-combining-not-found
    public static void CombiningNotFound()
    {
        var stub = new DuOrderRepositoryStub();
        stub.GetById2.OnCall = (ko, id) => null;  // Override for this test

        // var processor = new OrderProcessor(stub);
        // var result = processor.Process(orderId: 999);
        // Assert.False(result);
    }
    #endregion

    #region duality-combining-multiple-orders
    public static void CombiningMultipleOrders()
    {
        var stub = new DuOrderRepositoryStub();
        stub.GetByCustomer2.OnCall = (ko, customerId) => new[]
        {
            new DuOrder { Id = 1, CustomerId = customerId },
            new DuOrder { Id = 2, CustomerId = customerId }
        };

        // var service = new CustomerService(stub);
        // var orders = service.GetOrderHistory(customerId: 42);
        // Assert.Equal(2, orders.Count());
    }
    #endregion

    #region duality-reset-behavior
    public static void ResetBehavior()
    {
        var stub = new DuUserRepositoryStub();

        // Override default
        stub.GetById2.OnCall = (ko, id) => null;
        var user1 = stub.Object.GetById(1);  // null

        // Reset
        stub.GetById2.Reset();
        var user2 = stub.Object.GetById(1);  // User { Id = 1, Name = "User-1" }

        _ = (user1, user2);
    }
    #endregion
}

// Minimal Assert for compilation
file static class Assert
{
    public static void Equal<T>(T expected, T actual) { }
    public static void Null<T>(T value) { }
    public static void False(bool condition) { }
}
