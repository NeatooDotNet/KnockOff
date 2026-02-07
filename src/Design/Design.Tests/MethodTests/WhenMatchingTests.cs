// -----------------------------------------------------------------------------
// Design.Tests - When Matching Tests
// -----------------------------------------------------------------------------

using Design.Stubs.Methods;
using KnockOff;

namespace Design.Tests.MethodTests;

/// <summary>
/// Tests for When() parameter-specific matching.
/// </summary>
public class WhenMatchingTests
{
    [Fact]
    public void When_MatchesExactValues()
    {
        var stub = new WhenMatchingDemo.Stubs.ICalculator();
        stub.Add.When(1, 2).Returns(100);

        Design.Domain.Services.ICalculator calc = stub;

        Assert.Equal(100, calc.Add(1, 2));
    }

    [Fact]
    public void When_NoMatchReturnsDefault()
    {
        var stub = new WhenMatchingDemo.Stubs.ICalculator();
        stub.Add.When(1, 2).Returns(100);

        Design.Domain.Services.ICalculator calc = stub;

        Assert.Equal(0, calc.Add(3, 4)); // No match
    }

    [Fact]
    public void When_WithPredicate()
    {
        var stub = new WhenMatchingDemo.Stubs.ICalculator();
        stub.Add.When((a, b) => a > 10).Returns(999);

        Design.Domain.Services.ICalculator calc = stub;

        Assert.Equal(999, calc.Add(11, 0));
        Assert.Equal(0, calc.Add(5, 0)); // Doesn't match
    }

    [Fact]
    public void When_ThenWhen_ChainMultipleConditions()
    {
        var stub = new WhenMatchingDemo.Stubs.ICalculator();
        stub.Add.When(1, 2).Returns(10)
            .ThenWhen(3, 4).Returns(20)
            .ThenCall((a, b) => a + b);

        Design.Domain.Services.ICalculator calc = stub;

        Assert.Equal(10, calc.Add(1, 2));
        Assert.Equal(20, calc.Add(3, 4));
        Assert.Equal(100, calc.Add(50, 50)); // Falls to ThenCall
    }

    [Fact]
    public void When_ThenNone_StopsMatching()
    {
        var stub = new WhenMatchingDemo.Stubs.ICalculator();
        stub.Add.When(1, 2).Returns(100)
            .ThenNone();

        Design.Domain.Services.ICalculator calc = stub;

        Assert.Equal(100, calc.Add(1, 2));
        Assert.Equal(0, calc.Add(1, 2)); // ThenNone consumed, returns default
    }

    [Fact]
    public void VoidWhen_Call_ConfiguresCallback()
    {
        var stub = new WhenMatchingDemo.Stubs.ICalculator();
        var called = false;

        stub.Reset.Execute(() => called = true);

        Design.Domain.Services.ICalculator calc = stub;
        calc.Reset();

        Assert.True(called);
    }

    [Fact]
    public void When_Verify_ChecksSpecificMatch()
    {
        var stub = new WhenMatchingDemo.Stubs.ICalculator();

        var chain = stub.Add.When(1, 2).Returns(100)
            .ThenWhen(3, 4).Returns(200)
            .ThenCall((a, b) => 999);

        Design.Domain.Services.ICalculator calc = stub;

        calc.Add(1, 2);
        calc.Add(3, 4);
        calc.Add(5, 6);

        // Complete chain consumed
        chain.Verify(); // Should not throw
    }
}
