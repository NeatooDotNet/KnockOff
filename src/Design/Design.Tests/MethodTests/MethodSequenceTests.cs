// -----------------------------------------------------------------------------
// Design.Tests - Method Sequence Tests
// -----------------------------------------------------------------------------

using Design.Domain.Services;
using Design.Stubs.Methods;
using KnockOff;

namespace Design.Tests.MethodTests;

/// <summary>
/// Tests for method sequences: Call().ThenReturn() chains, value sequences,
/// and NSubstitute-style params syntax.
/// </summary>
public class MethodSequenceTests
{
    // =========================================================================
    // Return(first, params rest) - NSubstitute-style Params Sequences
    // =========================================================================

    [Fact]
    public void Returns_Params_CreatesSequence()
    {
        // NSubstitute-style: Return(first, params rest)
        var stub = new MethodSequencesDemo.Stubs.ICalculator();

        stub.Add.Return(1, 2, 3);

        ICalculator calc = stub;

        Assert.Equal(1, calc.Add(0, 0));
        Assert.Equal(2, calc.Add(0, 0));
        Assert.Equal(3, calc.Add(0, 0));
        Assert.Equal(3, calc.Add(0, 0)); // Repeats last
    }

    [Fact]
    public void Returns_SingleValue_RepeatsIndefinitely()
    {
        // Single value uses non-params overload - repeats forever
        var stub = new MethodSequencesDemo.Stubs.ICalculator();

        stub.Add.Return(42);

        ICalculator calc = stub;

        Assert.Equal(42, calc.Add(0, 0));
        Assert.Equal(42, calc.Add(0, 0));
        Assert.Equal(42, calc.Add(0, 0));
        Assert.Equal(42, calc.Add(0, 0)); // Still 42
    }

    [Fact]
    public void ThenReturns_Params_AddsMultipleValues()
    {
        // Params version on ThenReturn
        var stub = new MethodSequencesDemo.Stubs.ICalculator();

        stub.Add
            .Call(args => args.a + args.b)
            .ThenReturn(100, 200, 300);

        ICalculator calc = stub;

        Assert.Equal(3, calc.Add(1, 2));     // Computed: 1+2
        Assert.Equal(100, calc.Add(0, 0));   // First from params
        Assert.Equal(200, calc.Add(0, 0));   // Second from params
        Assert.Equal(300, calc.Add(0, 0));   // Third from params
        Assert.Equal(300, calc.Add(0, 0));   // Repeats last
    }

    [Fact]
    public async Task Returns_Params_AsyncAutoWraps()
    {
        // Params with async methods auto-wraps values
        var stub = new MethodSequencesDemo.Stubs.IDataService();

        stub.GetDataAsync.Return("first", "second", "third");

        IDataService service = stub;

        Assert.Equal("first", await service.GetDataAsync(1));
        Assert.Equal("second", await service.GetDataAsync(2));
        Assert.Equal("third", await service.GetDataAsync(3));
        Assert.Equal("third", await service.GetDataAsync(4)); // Repeats
    }

    [Fact]
    public void Returns_Params_SupportsVerification()
    {
        // Params sequence supports Verify()
        var stub = new MethodSequencesDemo.Stubs.ICalculator();

        var sequence = stub.Add.Return(1, 2, 3);

        ICalculator calc = stub;

        calc.Add(0, 0);
        calc.Add(0, 0);

        // Incomplete
        Assert.Throws<VerificationException>(() => sequence.Verify());

        calc.Add(0, 0);

        // Now complete
        sequence.Verify(); // Should not throw
    }

    [Fact]
    public void Returns_Params_ExhaustionRepeatsLast()
    {
        // After exhaustion, last value repeats (NSubstitute behavior)
        var stub = new MethodSequencesDemo.Stubs.ICalculator();

        stub.Add.Return(10, 20);

        ICalculator calc = stub;

        Assert.Equal(10, calc.Add(0, 0));
        Assert.Equal(20, calc.Add(0, 0));
        Assert.Equal(20, calc.Add(0, 0)); // Repeats
        Assert.Equal(20, calc.Add(0, 0)); // Still repeats
    }

    [Fact]
    public void Returns_Params_StrictModeThrowsOnExhaustion()
    {
        // In strict mode, exhaustion throws
        var stub = new MethodSequencesDemo.Stubs.ICalculator();
        stub.Strict = true;

        stub.Add.Return(1, 2);

        ICalculator calc = stub;

        Assert.Equal(1, calc.Add(0, 0));
        Assert.Equal(2, calc.Add(0, 0));

        // Exhausted in strict mode - throws
        Assert.Throws<StubException>(() => calc.Add(0, 0));
    }

    // =========================================================================
    // Return(value).ThenReturn(value) - Value-Based Sequence (NRE Bug Fix)
    // =========================================================================

    [Fact]
    public void ReturnValue_ThenReturnValue_ReturnsSequence()
    {
        var stub = new MethodSequencesDemo.Stubs.ICalculator();

        // Value-based Return followed by value-based ThenReturn
        // This was previously an NRE bug: _call was null during sequence elevation
        stub.Add.Return(1).ThenReturn(2).ThenReturn(3);

        ICalculator calc = stub;

        Assert.Equal(1, calc.Add(0, 0));
        Assert.Equal(2, calc.Add(0, 0));
        Assert.Equal(3, calc.Add(0, 0));
        Assert.Equal(3, calc.Add(0, 0)); // Repeats last
    }

    // =========================================================================
    // Original Call().ThenReturn() - Callback Sequences
    // =========================================================================

    [Fact]
    public void ThenCall_CreatesSequence()
    {
        var stub = new MethodSequencesDemo.Stubs.ICalculator();
        stub.Add.Call(_ => 1)
            .ThenReturn(_ => 2)
            .ThenReturn(_ => 3);

        ICalculator calc = stub;

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
        stub.Add.Call(_ => 1)
            .ThenReturn(_ => 999);

        ICalculator calc = stub;

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
        stub.Add.Call(_ => 1)
            .ThenReturn(_ => 999)
            .ThenDefault();

        ICalculator calc = stub;

        Assert.Equal(1, calc.Add(0, 0)); // First callback
        Assert.Equal(999, calc.Add(0, 0)); // Second callback
        Assert.Equal(0, calc.Add(0, 0)); // Exhausted - returns default
    }

    [Fact]
    public void Sequence_MixCallbacksAndConstants()
    {
        var stub = new MethodSequencesDemo.Stubs.ICalculator();
        var counter = 0;

        stub.Add.Call(args => args.a + args.b) // Use args
            .ThenReturn(_ => ++counter); // Use closure

        ICalculator calc = stub;

        Assert.Equal(3, calc.Add(1, 2)); // Uses args
        Assert.Equal(1, calc.Add(0, 0)); // counter = 1
        Assert.Equal(2, calc.Add(0, 0)); // Repeats last callback, counter = 2
    }

    [Fact]
    public void VoidSequence_ExecutesInOrder()
    {
        var stub = new MethodSequencesDemo.Stubs.ICalculator();
        var log = new List<string>();

        stub.Reset.Call(() => log.Add("First"))
            .ThenCall(() => log.Add("Second"))
            .ThenCall(() => log.Add("Third"));

        ICalculator calc = stub;

        calc.Reset();
        calc.Reset();
        calc.Reset();

        Assert.Equal(["First", "Second", "Third"], log);
    }

    [Fact]
    public void Sequence_Verify_ChecksCompletion()
    {
        var stub = new MethodSequencesDemo.Stubs.ICalculator();

        var sequence = stub.Add.Call(_ => 1)
            .ThenReturn(_ => 2)
            .ThenReturn(_ => 3);

        ICalculator calc = stub;

        calc.Add(0, 0);
        calc.Add(0, 0);

        // Incomplete sequence
        Assert.Throws<VerificationException>(() => sequence.Verify());

        calc.Add(0, 0);

        // Now complete
        sequence.Verify(); // Should not throw
    }

    // =========================================================================
    // Call().ThenReturn() - Value Sequences (Explicit Syntax)
    // =========================================================================

    [Fact]
    public void ThenReturns_CreatesValueSequence()
    {
        var stub = new MethodSequencesDemo.Stubs.ICalculator();

        // Call starts the sequence, ThenReturn adds constant values
        stub.Add.Call(_ => 1)
            .ThenReturn(2)
            .ThenReturn(3);

        ICalculator calc = stub;

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
        stub.Add.Call(args => args.a + args.b)
            .ThenReturn(100)
            .ThenReturn(args => args.a * args.b)
            .ThenReturn(999);

        ICalculator calc = stub;

        Assert.Equal(5, calc.Add(2, 3));   // Callback: 2+3
        Assert.Equal(100, calc.Add(0, 0)); // Value
        Assert.Equal(20, calc.Add(4, 5));  // Callback: 4*5
        Assert.Equal(999, calc.Add(0, 0)); // Value
    }

    [Fact]
    public void ThenReturns_Verify_Works()
    {
        var stub = new MethodSequencesDemo.Stubs.ICalculator();

        var sequence = stub.Add.Call(_ => 1)
            .ThenReturn(2)
            .ThenReturn(3);

        ICalculator calc = stub;

        calc.Add(0, 0);
        calc.Add(0, 0);
        calc.Add(0, 0);

        // Sequence fully consumed
        sequence.Verify(); // Should not throw
    }
}
