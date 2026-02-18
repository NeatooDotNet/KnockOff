// -----------------------------------------------------------------------------
// Design.Tests - Inline Interface Pattern Tests
// -----------------------------------------------------------------------------

using Design.Stubs.StubPatterns;

namespace Design.Tests.PatternTests;

/// <summary>
/// Tests for the inline interface pattern: [KnockOff{T}] where T is an interface.
/// </summary>
public class InlineInterfacePatternTests
{
    [Fact]
    public void InlineInterface_GeneratesNestedStubsClass()
    {
        // Access the nested Stubs class
        var stub = new InlineInterfaceExample.Stubs.ICalculator();

        Assert.NotNull(stub);
    }

    [Fact]
    public void InlineInterface_StubImplementsInterface()
    {
        var stub = new InlineInterfaceExample.Stubs.ICalculator();

        Assert.IsAssignableFrom<Design.Domain.Services.ICalculator>(stub);
    }

    [Fact]
    public void InlineInterface_Returns_ConfiguresConstantReturnValue()
    {
        var stub = new InlineInterfaceExample.Stubs.ICalculator();
        stub.Add.Return(100);

        Design.Domain.Services.ICalculator calc = stub;
        var result = calc.Add(5, 10);

        Assert.Equal(100, result);
    }

    [Fact]
    public void InlineInterface_OnCall_ConfiguresCallback()
    {
        var stub = new InlineInterfaceExample.Stubs.ICalculator();
        stub.Add.Call(args => args.a * args.b);

        Design.Domain.Services.ICalculator calc = stub;
        var result = calc.Add(5, 10);

        Assert.Equal(50, result);
    }

    [Fact]
    public void InlineInterface_UnconfiguredMethod_ReturnsDefault()
    {
        var stub = new InlineInterfaceExample.Stubs.ICalculator();

        Design.Domain.Services.ICalculator calc = stub;
        var result = calc.Add(1, 2);

        Assert.Equal(0, result);
    }
}
