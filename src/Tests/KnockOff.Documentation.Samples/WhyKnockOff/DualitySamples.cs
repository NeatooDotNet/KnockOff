/// <summary>
/// Code samples for docs/why-knockoff/user-methods.md
/// Demonstrates user-defined methods for providing default stub behavior.
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

#region user-methods-interface
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

#region user-methods-stub
[KnockOff]
public partial class DuUserRepositoryStub : IDuUserRepository
{
    // Default: return a test user for any ID
    protected DuUser? GetById(int id) => new DuUser { Id = id, Name = $"User-{id}" };

    // Save: no user method = requires OnCall setup or will use default void behavior
}
#endregion

#region user-methods-order-stub
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
    #region user-methods-usage
    public static void UserMethodUsage()
    {
        var stub = new DuUserRepositoryStub();
        IDuUserRepository repo = stub;

        // User method provides default behavior automatically
        var user = repo.GetById(42);

        Assert.Equal(42, user?.Id);
        Assert.Equal("User-42", user?.Name);
    }
    #endregion

    #region user-methods-tracking
    public static void UserMethodWithTracking()
    {
        var stub = new DuUserRepositoryStub();
        IDuUserRepository repo = stub;

        repo.GetById(42);
        repo.GetById(99);

        // User methods still get full tracking
        Assert.Equal(2, stub.GetById2.CallCount);
        Assert.Equal(99, stub.GetById2.LastArg);
    }
    #endregion

    #region user-methods-order-defaults
    public static void OrderRepositoryDefaults()
    {
        var stub = new DuOrderRepositoryStub();
        IDuOrderRepository repo = stub;

        // GetById returns a pending order by default
        var order = repo.GetById(123);
        Assert.Equal("Pending", order?.Status);

        // GetByCustomer returns empty by default
        var orders = repo.GetByCustomer(1);
        Assert.Empty(orders);
    }
    #endregion
}

// Minimal Assert for compilation
file static class Assert
{
    public static void Equal<T>(T expected, T actual) { }
    public static void Empty<T>(IEnumerable<T> collection) { }
}
