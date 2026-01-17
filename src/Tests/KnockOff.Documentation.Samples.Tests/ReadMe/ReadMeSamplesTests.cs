using KnockOff.Documentation.Samples.ReadMe;

namespace KnockOff.Documentation.Samples.Tests.ReadMe;

/// <summary>
/// Tests for README.md code samples.
/// These tests verify that the README examples actually work.
/// The snippet code in ReadMeSamples.cs compiles; these tests verify behavior.
/// </summary>
[Trait("Category", "Documentation")]
[Trait("Category", "ReadMe")]
public class ReadMeSamplesTests
{
    // ========================================================================
    // Inline Stubs Example
    // ========================================================================

    [Fact]
    public void InlineStubs_NotifiesUser_WhenOrderShipped()
    {
        // This tests the pattern shown in readme-inline-stubs
        var userStub = new UserTests.Stubs.IUserService();
        var emailStub = new UserTests.Stubs.EmailService();

        userStub.GetUser.OnCall = (ko, id) => new User { Id = id, Email = "test@example.com" };
        emailStub.Send.OnCall = (ko, to, subject, body) => { };

        var service = new OrderService(userStub, emailStub.Object);
        service.ShipOrder(orderId: 42, userId: 1);

        Assert.True(userStub.GetUser.WasCalled);
        Assert.Equal(1, userStub.GetUser.LastCallArg);
        Assert.Equal("test@example.com", emailStub.Send.LastCallArgs?.to);
    }

    // ========================================================================
    // Reusable Stubs Example
    // ========================================================================

    [Fact]
    public void ReusableStub_UsesUserMethod()
    {
        // This tests the pattern shown in readme-reusable-stubs-usage
        var calc = new CalculatorKnockOff();
        ICalculator calculator = calc;

        Assert.Equal(5, calculator.Add(2, 3));  // Uses user method
        Assert.Equal(1, calc.Add2.CallCount);   // Add2: renamed to avoid collision
    }

    [Fact]
    public void ReusableStub_TracksUserMethodCalls()
    {
        var calc = new CalculatorKnockOff();
        // User method interceptors (suffix "2") track calls but cannot override behavior
        ((ICalculator)calc).Add(2, 3);
        Assert.Equal(1, calc.Add2.CallCount);
        Assert.Equal((2, 3), calc.Add2.LastArgs);
    }

    // ========================================================================
    // Delegate Stubs Example
    // ========================================================================

    [Fact]
    public void DelegateStubs_RejectsNonUniqueName()
    {
        // This tests the pattern shown in readme-delegate-stubs
        var uniqueCheck = new ValidationTests.Stubs.IsUniqueRule();
        uniqueCheck.Interceptor.OnCall = (ko, value) => value != "duplicate";

        IsUniqueRule rule = uniqueCheck;
        Assert.False(rule("duplicate"));
        Assert.True(uniqueCheck.Interceptor.WasCalled);
    }

    // ========================================================================
    // Properties Example
    // ========================================================================

    [Fact]
    public void Properties_ConfigureGettersAndSetters()
    {
        // This tests the pattern shown in readme-properties
        var stub = new ConfigServiceStub();
        IConfigService config = stub;

        stub.ConnectionString.Value = "Server=localhost;Database=test";
        stub.LogLevel.OnGet = (ko) => Environment.GetEnvironmentVariable("LOG_LEVEL") ?? "Info";

        config.LogLevel = "Debug";
        _ = config.ConnectionString;

        Assert.Equal(1, stub.ConnectionString.GetCount);
        Assert.Equal(1, stub.LogLevel.SetCount);
        Assert.Equal("Debug", stub.LogLevel.LastSetValue);
    }

    // ========================================================================
    // Verification Example
    // ========================================================================

    [Fact]
    public void Verification_TracksCallsAndArguments()
    {
        // This tests the pattern shown in readme-verification
        var stub = new DataServiceStub();
        var tracking = stub.GetDescription.OnCall((ko, id) => $"Item {id}");
        IDataService service = stub;

        service.GetDescription(1);
        service.GetDescription(2);
        service.GetDescription(42);

        Assert.True(tracking.WasCalled);
        Assert.Equal(3, tracking.CallCount);
        Assert.Equal(42, tracking.LastArg);

        service.Name = "First";
        service.Name = "Second";
        Assert.Equal(2, stub.Name.SetCount);
        Assert.Equal("Second", stub.Name.LastSetValue);
    }
}
