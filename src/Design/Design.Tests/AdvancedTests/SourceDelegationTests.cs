// -----------------------------------------------------------------------------
// Design.Tests - Source Delegation Tests
// -----------------------------------------------------------------------------

using Design.Domain.Services;
using Design.Stubs.Advanced;

namespace Design.Tests.AdvancedTests;

/// <summary>
/// Tests for Source() delegation to real implementations.
/// </summary>
public class SourceDelegationTests
{
    [Fact]
    public void Source_DelegatesUnconfiguredMethods()
    {
        var stub = new SourceDelegationDemo.Stubs.ICalculator();
        var realCalc = new RealCalculator();

        stub.Source(realCalc);

        ICalculator calc = stub;

        Assert.Equal(5, calc.Add(2, 3)); // From real implementation
    }

    [Fact]
    public void Source_ConfiguredMethodsOverrideSource()
    {
        var stub = new SourceDelegationDemo.Stubs.ICalculator();
        var realCalc = new RealCalculator();

        stub.Source(realCalc);
        stub.Add.Returns(999);

        ICalculator calc = stub;

        Assert.Equal(999, calc.Add(2, 3)); // Stub takes priority
        Assert.Equal(7, calc.Subtract(10, 3)); // Delegates to source
    }

    [Fact]
    public void Source_Null_RemovesDelegation()
    {
        var stub = new SourceDelegationDemo.Stubs.ICalculator();
        var realCalc = new RealCalculator();

        stub.Source(realCalc);
        ICalculator calc = stub;

        Assert.Equal(5, calc.Add(2, 3)); // Delegates

        stub.Source(null);

        Assert.Equal(0, calc.Add(2, 3)); // Returns default
    }

    [Fact]
    public void Source_PartialStubbing()
    {
        var stub = new SourceDelegationDemo.Stubs.ICalculator();
        var realCalc = new RealCalculator();

        stub.Source(realCalc);

        // Override just Add to throw
        stub.Add.OnCall((a, b) => throw new InvalidOperationException("Simulated error"));

        ICalculator calc = stub;

        // Add uses stub (throws)
        Assert.Throws<InvalidOperationException>(() => calc.Add(1, 2));

        // Subtract uses real implementation
        Assert.Equal(8, calc.Subtract(10, 2));
    }

    private sealed class RealCalculator : ICalculator
    {
        public int Add(int a, int b) => a + b;
        public int Subtract(int a, int b) => a - b;
        public int Divide(int a, int b) => b == 0 ? 0 : a / b;
        public void Reset() { }
    }
}
