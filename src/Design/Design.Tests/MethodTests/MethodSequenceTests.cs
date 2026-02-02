// -----------------------------------------------------------------------------
// Design.Tests - Method Sequence Tests
// -----------------------------------------------------------------------------

using Design.Stubs.Methods;
using KnockOff;

namespace Design.Tests.MethodTests;

/// <summary>
/// Tests for method sequences: OnCall().ThenCall() chains and value sequences.
/// </summary>
public class MethodSequenceTests
{
    [Fact]
    public void ThenCall_CreatesSequence()
    {
        var stub = new MethodSequencesDemo.Stubs.ICalculator();
        stub.Add.OnCall((a, b) => 1)
            .ThenCall((a, b) => 2)
            .ThenCall((a, b) => 3);

        Design.Domain.Services.ICalculator calc = stub;

        Assert.Equal(1, calc.Add(0, 0));
        Assert.Equal(2, calc.Add(0, 0));
        Assert.Equal(3, calc.Add(0, 0));
    }

    [Fact]
    public void Sequence_RepeatsLastValueAfterExhaustion()
    {
        // ACTUAL BEHAVIOR: Sequences repeat the last callback (NSubstitute-like).
        // Use ThenDefault() to return default(T) after exhaustion instead.
        var stub = new MethodSequencesDemo.Stubs.ICalculator();
        stub.Add.OnCall((a, b) => 1)
            .ThenCall((a, b) => 999);

        Design.Domain.Services.ICalculator calc = stub;

        Assert.Equal(1, calc.Add(0, 0)); // First callback
        Assert.Equal(999, calc.Add(0, 0)); // Second callback
        Assert.Equal(999, calc.Add(0, 0)); // Repeats last value
        Assert.Equal(999, calc.Add(0, 0)); // Still repeats
    }

    [Fact]
    public void ThenDefault_ReturnsDefaultAfterExhaustion()
    {
        // ThenDefault() causes sequence to return default(T) after exhaustion
        var stub = new MethodSequencesDemo.Stubs.ICalculator();
        stub.Add.OnCall((a, b) => 1)
            .ThenCall((a, b) => 999)
            .ThenDefault();

        Design.Domain.Services.ICalculator calc = stub;

        Assert.Equal(1, calc.Add(0, 0)); // First callback
        Assert.Equal(999, calc.Add(0, 0)); // Second callback
        Assert.Equal(0, calc.Add(0, 0)); // Exhausted - returns default
    }

    [Fact]
    public void Sequence_MixCallbacksAndConstants()
    {
        var stub = new MethodSequencesDemo.Stubs.ICalculator();
        var counter = 0;

        stub.Add.OnCall((a, b) => a + b) // Use args
            .ThenCall((a, b) => ++counter); // Use closure

        Design.Domain.Services.ICalculator calc = stub;

        Assert.Equal(3, calc.Add(1, 2)); // Uses args
        Assert.Equal(1, calc.Add(0, 0)); // counter = 1
        Assert.Equal(2, calc.Add(0, 0)); // Repeats last callback, counter = 2
    }

    [Fact]
    public void VoidSequence_ExecutesInOrder()
    {
        var stub = new MethodSequencesDemo.Stubs.ICalculator();
        var log = new List<string>();

        stub.Reset.OnCall(() => log.Add("First"))
            .ThenCall(() => log.Add("Second"))
            .ThenCall(() => log.Add("Third"));

        Design.Domain.Services.ICalculator calc = stub;

        calc.Reset();
        calc.Reset();
        calc.Reset();

        Assert.Equal(["First", "Second", "Third"], log);
    }

    [Fact]
    public void Sequence_Verify_ChecksCompletion()
    {
        var stub = new MethodSequencesDemo.Stubs.ICalculator();

        var sequence = stub.Add.OnCall((a, b) => 1)
            .ThenCall((a, b) => 2)
            .ThenCall((a, b) => 3);

        Design.Domain.Services.ICalculator calc = stub;

        calc.Add(0, 0);
        calc.Add(0, 0);

        // Incomplete sequence
        Assert.Throws<VerificationException>(() => sequence.Verify());

        calc.Add(0, 0);

        // Now complete
        sequence.Verify(); // Should not throw
    }

    // =========================================================================
    // ThenReturns - Value Sequences
    // =========================================================================

    [Fact]
    public void ThenReturns_CreatesValueSequence()
    {
        var stub = new MethodSequencesDemo.Stubs.ICalculator();

        // OnCall starts the sequence, ThenReturns adds constant values
        stub.Add.OnCall((_, _) => 1)
            .ThenReturns(2)
            .ThenReturns(3);

        Design.Domain.Services.ICalculator calc = stub;

        Assert.Equal(1, calc.Add(0, 0));
        Assert.Equal(2, calc.Add(0, 0));
        Assert.Equal(3, calc.Add(0, 0));
        Assert.Equal(3, calc.Add(0, 0)); // Repeats last value
    }

    [Fact]
    public void ThenReturns_MixedWithThenCall()
    {
        var stub = new MethodSequencesDemo.Stubs.ICalculator();

        // Mix callbacks and values in the same sequence
        stub.Add.OnCall((a, b) => a + b)
            .ThenReturns(100)
            .ThenCall((a, b) => a * b)
            .ThenReturns(999);

        Design.Domain.Services.ICalculator calc = stub;

        Assert.Equal(5, calc.Add(2, 3));   // Callback: 2+3
        Assert.Equal(100, calc.Add(0, 0)); // Value
        Assert.Equal(20, calc.Add(4, 5));  // Callback: 4*5
        Assert.Equal(999, calc.Add(0, 0)); // Value
    }

    [Fact]
    public void ThenReturns_Verify_Works()
    {
        var stub = new MethodSequencesDemo.Stubs.ICalculator();

        var sequence = stub.Add.OnCall((_, _) => 1)
            .ThenReturns(2)
            .ThenReturns(3);

        Design.Domain.Services.ICalculator calc = stub;

        calc.Add(0, 0);
        calc.Add(0, 0);
        calc.Add(0, 0);

        // Sequence fully consumed
        sequence.Verify(); // Should not throw
    }
}
