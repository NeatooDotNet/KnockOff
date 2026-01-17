/// <summary>
/// Code samples for docs/for-ai-assistants.md
/// </summary>

namespace KnockOff.Documentation.Samples;

// ============================================================================
// Domain Types
// ============================================================================

public class AiUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class AiOrder
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public interface IAiUserRepository
{
    AiUser? GetById(int id);
    void Save(AiUser user);
}

public interface IAiEmailService
{
    void Send(string to, string body);
}

// Class to stub
public class AiEmailServiceClass
{
    public virtual void Send(string to, string body) { }
}

public interface IAiProcessor
{
    void Process(string data);
    void Process(string data, int n);
}

#region ai-order-service-interface
public interface IAiOrderService
{
    AiOrder? GetOrder(int id);
    void SaveOrder(AiOrder order);
    Task<bool> ValidateAsync(AiOrder order);
    decimal TotalAmount { get; }
}
#endregion

public interface IAiCalculator
{
    int Add(int a, int b);
}

// ============================================================================
// KnockOff Stubs
// ============================================================================

#region ai-standalone-stub
[KnockOff]
public partial class AiUserRepositoryStub : IAiUserRepository { }
#endregion

#region ai-user-methods
[KnockOff]
public partial class AiUserRepositoryWithDefaultsStub : IAiUserRepository
{
    // Called by default when IAiUserRepository.GetById is invoked
    protected AiUser? GetById(int id) => new AiUser { Id = id, Name = "Default" };
}
#endregion

[KnockOff]
public partial class AiProcessorStub : IAiProcessor { }

#region ai-user-method-collision
[KnockOff]
public partial class AiCalcStub : IAiCalculator
{
    protected int Add(int a, int b) => a + b;  // User method
}

// Interceptor becomes Add2 to avoid collision
// stub.Add2.CallCount
#endregion

#region ai-order-service-stub
// Standalone stub with user method defaults
[KnockOff]
public partial class AiOrderServiceStub : IAiOrderService
{
    protected AiOrder? GetOrder(int id) => new AiOrder { Id = id, Status = "Pending" };
    protected Task<bool> ValidateAsync(AiOrder order) => Task.FromResult(true);
}
#endregion

// ============================================================================
// Sample Usage Classes
// ============================================================================

public static class ForAISamples
{
    #region ai-standalone-usage
    public static void StandaloneUsage()
    {
        var stub = new AiUserRepositoryStub();
        stub.GetById.OnCall((ko, id) => new AiUser { Id = id });
        IAiUserRepository repo = stub;

        _ = repo;
    }
    #endregion

    #region ai-overloads
    public static void OverloadedMethods()
    {
        var stub = new AiProcessorStub();

        // OnCall overloads distinguish by delegate signature
        stub.Process.OnCall((AiProcessorStub ko, string data) => { });
        stub.Process.OnCall((AiProcessorStub ko, string data, int n) => { });
    }
    #endregion
}

// ============================================================================
// Inline Stubs Test Class
// ============================================================================

#region ai-inline-stub
[KnockOff<IAiUserRepository>]
[KnockOff<IAiEmailService>]
public partial class AiUserServiceTests
{
    public void Test()
    {
        var repoStub = new Stubs.IAiUserRepository();
        var emailStub = new Stubs.IAiEmailService();
        // ...

        _ = (repoStub, emailStub);
    }
}
#endregion

#region ai-class-stub
[KnockOff<AiEmailServiceClass>]
public partial class AiNotificationTests
{
    public void Test()
    {
        var stub = new Stubs.AiEmailServiceClass();
        stub.Send.OnCall = (ko, to, body) => { };

        // Use .Object to get the class instance
        AiEmailServiceClass service = stub.Object;

        _ = service;
    }
}
#endregion

#region ai-delegate-stub
[KnockOff<Func<int, bool>>]
public partial class AiValidationTests
{
    public void Test()
    {
        var stub = new Stubs.Func();
        stub.Interceptor.OnCall = (ko, value) => value > 0;

        Func<int, bool> func = stub;

        _ = func;
    }
}
#endregion

// ============================================================================
// Complete Example
// ============================================================================

// Minimal OrderProcessor for compilation
public class AiOrderProcessor
{
    private readonly IAiOrderService _orderService;

    public AiOrderProcessor(IAiOrderService orderService)
    {
        _orderService = orderService;
    }

    public async Task<bool> ProcessAsync(int orderId)
    {
        var order = _orderService.GetOrder(orderId);
        if (order == null) return false;

        var isValid = await _orderService.ValidateAsync(order);
        if (!isValid) return false;

        order.Status = "Completed";
        _orderService.SaveOrder(order);
        return true;
    }
}

#region ai-complete-example
public class OrderProcessingTests
{
    public async Task ProcessOrder_Succeeds()
    {
        // Arrange
        var stub = new AiOrderServiceStub();
        stub.TotalAmount.Value = 150.00m;

        // User methods (GetOrder, ValidateAsync) provide defaults
        // ValidateAsync2 tracks calls but can't override behavior
        // (User methods are compile-time only)

        // Track save calls
        var saveTracking = stub.SaveOrder.OnCall((ko, order) => { });

        var processor = new AiOrderProcessor(stub);

        // Act
        var result = await processor.ProcessAsync(orderId: 1);

        // Assert
        Assert.True(result);
        stub.VerifyAll();  // Verifies all configured callbacks were called
        Assert.Equal(1, saveTracking.CallCount);
        Assert.Equal("Completed", saveTracking.LastArg?.Status);

        // User method interceptors track calls
        Assert.True(stub.ValidateAsync2.WasCalled);
    }
}
#endregion

// Minimal Assert for compilation
file static class Assert
{
    public static void True(bool condition) { }
    public static void Equal<T>(T expected, T actual) { }
}
