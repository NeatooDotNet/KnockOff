// -----------------------------------------------------------------------------
// Design.Tests - Strict Mode Tests
// -----------------------------------------------------------------------------

using Design.Domain.Services;
using Design.Stubs.Advanced;
using KnockOff;

namespace Design.Tests.AdvancedTests;

/// <summary>
/// Tests for strict mode behavior.
/// </summary>
public class StrictModeTests
{
    [Fact]
    public void LooseMode_IsDefault()
    {
        var stub = new StrictModeDemo.Stubs.ICalculator();

        Assert.False(stub.Strict);
    }

    [Fact]
    public void LooseMode_ReturnsDefault()
    {
        var stub = new StrictModeDemo.Stubs.ICalculator();

        ICalculator calc = stub;

        Assert.Equal(0, calc.Add(1, 2)); // default(int)
    }

    [Fact]
    public void StrictMode_ViaConstructor()
    {
        var stub = new StrictModeDemo.Stubs.ICalculator(strict: true);

        Assert.True(stub.Strict);
    }

    [Fact]
    public void StrictMode_UnconfiguredMethodThrows()
    {
        var stub = new StrictModeDemo.Stubs.ICalculator(strict: true);

        ICalculator calc = stub;

        Assert.Throws<StubException>(() => calc.Add(1, 2));
    }

    [Fact]
    public void StrictMode_ConfiguredMethodWorks()
    {
        var stub = new StrictModeDemo.Stubs.ICalculator(strict: true);
        stub.Add.Returns(42);

        ICalculator calc = stub;

        Assert.Equal(42, calc.Add(1, 2)); // Configured, works fine
    }

    [Fact]
    public void StrictMode_ViaProperty()
    {
        var stub = new StrictModeDemo.Stubs.ICalculator();
        ICalculator calc = stub;

        // Start loose
        Assert.Equal(0, calc.Add(1, 2));

        // Switch to strict
        stub.Strict = true;

        Assert.Throws<StubException>(() => calc.Add(1, 2));

        // Switch back to loose
        stub.Strict = false;

        Assert.Equal(0, calc.Add(1, 2));
    }

    [Fact]
    public void StrictMode_WithSource_DelegatesInsteadOfThrowing()
    {
        var stub = new StrictModeDemo.Stubs.ICalculator(strict: true);
        var realCalc = new RealCalculator();

        stub.Source(realCalc);

        ICalculator calc = stub;

        // Source provides implementation, so no exception
        Assert.Equal(5, calc.Add(2, 3));
    }

    private sealed class RealCalculator : ICalculator
    {
        public int Add(int a, int b) => a + b;
        public int Subtract(int a, int b) => a - b;
        public int Divide(int a, int b) => b == 0 ? 0 : a / b;
        public void Reset() { }
    }
}
