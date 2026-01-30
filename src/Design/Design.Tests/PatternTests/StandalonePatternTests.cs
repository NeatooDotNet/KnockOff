// -----------------------------------------------------------------------------
// Design.Tests - Standalone Pattern Tests
// -----------------------------------------------------------------------------
// These tests verify the standalone stub pattern works correctly.
// They serve as both tests AND documentation of correct usage.
// -----------------------------------------------------------------------------

using Design.Stubs.StubPatterns;

namespace Design.Tests.PatternTests;

/// <summary>
/// Tests for the standalone stub pattern: [KnockOff] on a partial class
/// that implements an interface directly.
/// </summary>
public class StandalonePatternTests
{
    [Fact]
    public void Standalone_CanInstantiate()
    {
        // Standalone stubs are instantiated directly
        var stub = new CalculatorStub();

        // The stub is the interface implementation
        Assert.IsAssignableFrom<Design.Domain.Services.ICalculator>(stub);
    }

    [Fact]
    public void Standalone_Returns_ConfiguresConstantReturnValue()
    {
        var stub = new CalculatorStub();

        // Returns() sets a constant return value for all calls
        stub.Add.Returns(42);

        // Call the method and verify
        var result = ((Design.Domain.Services.ICalculator)stub).Add(1, 2);

        Assert.Equal(42, result);
    }

    [Fact]
    public void Standalone_OnCall_ConfiguresCallback()
    {
        var stub = new CalculatorStub();

        // OnCall() sets a callback that receives the arguments
        stub.Add.OnCall((a, b) => a + b);

        var result = ((Design.Domain.Services.ICalculator)stub).Add(5, 3);

        Assert.Equal(8, result);
    }

    [Fact]
    public void Standalone_CanHaveCustomMethods()
    {
        var stub = new CalculatorStub();

        // Standalone stubs can have user-defined methods
        stub.MarkUsed();

        Assert.True(stub.WasUsed);
    }

    [Fact]
    public void Standalone_UnconfiguredMethod_ReturnsDefault()
    {
        var stub = new CalculatorStub();

        // Without configuration, methods return default(T)
        var result = ((Design.Domain.Services.ICalculator)stub).Add(1, 2);

        Assert.Equal(0, result); // default(int) = 0
    }
}
