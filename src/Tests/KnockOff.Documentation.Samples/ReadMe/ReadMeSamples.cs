/// <summary>
/// Code samples for README.md - Quick Example section.
///
/// These snippets are structured to match how the README displays them,
/// with stub definitions and test code shown together.
///
/// Snippets in this file:
/// - readme-inline-stubs (class definition + test method)
/// - readme-reusable-stubs (stub class + test methods)
/// - readme-delegate-stubs (delegate + stub + test)
/// - readme-properties (usage example)
/// - readme-verification (usage example)
///
/// Note: Some snippets show partial patterns. The types they reference
/// are defined separately to make the code compile.
/// </summary>

namespace KnockOff.Documentation.Samples.ReadMe;

// ============================================================================
// Supporting Types (outside snippets - provide compilation context)
// ============================================================================

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
}

public interface IUserService
{
    User GetUser(int id);
}

public class EmailService
{
    public virtual void Send(string to, string subject, string body) { }
}

public class OrderService(IUserService userService, EmailService emailService)
{
    public void ShipOrder(int orderId, int userId)
    {
        var user = userService.GetUser(userId);
        emailService.Send(user.Email, "Order Shipped", $"Order {orderId} has shipped!");
    }
}

// ============================================================================
// Inline Stubs Example - shows test class with KnockOff attributes
// ============================================================================

#region readme-inline-stubs
[KnockOff<IUserService>]
[KnockOff<EmailService>]
public partial class UserTests
{
    public void NotifiesUser_WhenOrderShipped()
    {
        // Create stubs
        var userStub = new Stubs.IUserService();
        var emailStub = new Stubs.EmailService();

        // Configure behavior (unified API for both interface and class stubs)
        userStub.GetUser.OnCall = (ko, id) => new User { Id = id, Email = "test@example.com" };
        emailStub.Send.OnCall = (ko, to, subject, body) => { };

        // Inject and test (.Object for class stubs)
        var service = new OrderService(userStub, emailStub.Object);
        service.ShipOrder(orderId: 42, userId: 1);

        // Verify
        System.Diagnostics.Debug.Assert(userStub.GetUser.WasCalled);
        System.Diagnostics.Debug.Assert(userStub.GetUser.LastCallArg == 1);
        System.Diagnostics.Debug.Assert(emailStub.Send.LastCallArgs?.to == "test@example.com");
    }
}
#endregion

// ============================================================================
// Reusable Stubs Example - shows standalone stub with user methods
// ============================================================================

public interface ICalculator
{
    int Add(int a, int b);
    int Multiply(int a, int b);
}

#region readme-reusable-stubs
[KnockOff]
public partial class CalculatorKnockOff : ICalculator
{
    // User-defined method - called by generated code
    protected int Add(int a, int b) => a + b;

    // Multiply not defined - returns default(int) = 0
}
#endregion

#region readme-reusable-stubs-usage
public class CalculatorUsageExample
{
    public void Test1()
    {
        var calc = new CalculatorKnockOff();
        ICalculator calculator = calc;

        var result = calculator.Add(2, 3);      // Returns 5 (uses your method)
        var callCount = calc.Add2.CallCount;    // 1 (Add2: renamed to avoid collision)
    }

    public void Test2_OverrideForThisTest()
    {
        var calc = new CalculatorKnockOff();
        calc.Add2.OnCall = (ko, a, b) => 999;   // Override just here
        var result = ((ICalculator)calc).Add(2, 3);  // Returns 999
    }
}
#endregion

// ============================================================================
// Delegate Stubs Example
// ============================================================================

#region readme-delegate-stubs
public delegate bool IsUniqueRule(string value);

[KnockOff<IsUniqueRule>]
public partial class ValidationTests
{
    public void RejectsNonUniqueName()
    {
        var uniqueCheck = new Stubs.IsUniqueRule();
        uniqueCheck.Interceptor.OnCall = (ko, value) => value != "duplicate";

        IsUniqueRule rule = uniqueCheck;  // Implicit conversion
        var result = rule("duplicate");   // Returns false
        var wasCalled = uniqueCheck.Interceptor.WasCalled;  // true
    }
}
#endregion

// ============================================================================
// Properties Example
// ============================================================================

public interface IConfigService
{
    string ConnectionString { get; }
    string LogLevel { get; set; }
}

[KnockOff]
public partial class ConfigServiceStub : IConfigService { }

#region readme-properties
public class PropertiesUsageExample
{
    public void ConfigureGettersAndSetters()
    {
        var stub = new ConfigServiceStub();
        IConfigService config = stub;

        // Simple value - most common pattern
        stub.ConnectionString.Value = "Server=localhost;Database=test";

        // Dynamic getter
        stub.LogLevel.OnGet = (ko) =>
            Environment.GetEnvironmentVariable("LOG_LEVEL") ?? "Info";

        // Setter tracking
        config.LogLevel = "Debug";

        // Verify property access
        _ = config.ConnectionString;
        var getCount = stub.ConnectionString.GetCount;     // 1
        var setCount = stub.LogLevel.SetCount;             // 1
        var lastValue = stub.LogLevel.LastSetValue;        // "Debug"
    }
}
#endregion

// ============================================================================
// Verification Example
// ============================================================================

public interface IDataService
{
    string GetDescription(int id);
    string Name { get; set; }
}

[KnockOff]
public partial class DataServiceStub : IDataService { }

#region readme-verification
public class VerificationUsageExample
{
    public void TracksCallsAndArguments()
    {
        var stub = new DataServiceStub();
        stub.GetDescription.OnCall = (ko, id) => $"Item {id}";
        IDataService service = stub;

        service.GetDescription(1);
        service.GetDescription(2);
        service.GetDescription(42);

        // Assert method calls
        System.Diagnostics.Debug.Assert(stub.GetDescription.WasCalled);
        System.Diagnostics.Debug.Assert(stub.GetDescription.CallCount == 3);
        System.Diagnostics.Debug.Assert(stub.GetDescription.LastCallArg == 42);

        // Assert property access
        service.Name = "First";
        service.Name = "Second";
        System.Diagnostics.Debug.Assert(stub.Name.SetCount == 2);
        System.Diagnostics.Debug.Assert(stub.Name.LastSetValue == "Second");
    }
}
#endregion
