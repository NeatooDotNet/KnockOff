// -----------------------------------------------------------------------------
// Design.Tests - Method Sequence Tests
// -----------------------------------------------------------------------------

using Design.Stubs.Methods;
using KnockOff;

namespace Design.Tests.MethodTests;

/// <summary>
/// Tests for method sequences: OnCall().ThenCall() chains.
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
    public void Sequence_ExhaustsAfterAllCallbacks()
    {
        // ACTUAL BEHAVIOR: Sequences do NOT repeat the last callback.
        // After all callbacks are consumed, unconfigured calls return default.
        var stub = new MethodSequencesDemo.Stubs.ICalculator();
        stub.Add.OnCall((a, b) => 1)
            .ThenCall((a, b) => 999);

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
        Assert.Equal(0, calc.Add(0, 0)); // Exhausted - returns default
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
}
