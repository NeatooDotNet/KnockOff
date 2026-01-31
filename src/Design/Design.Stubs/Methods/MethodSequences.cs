// -----------------------------------------------------------------------------
// Design.Stubs - Method Sequences
// -----------------------------------------------------------------------------
// This file demonstrates sequence APIs for methods:
// - OnCall().ThenCall() for callback sequences
// - Returns().ThenReturns() for value sequences (Note: This pattern doesn't exist)
// - ThenNone() to exhaust sequences
// - Lazy elevation from builder to sequence mode
// - Sequence exhaustion behavior
// -----------------------------------------------------------------------------

using Design.Domain.Services;
using KnockOff;

namespace Design.Stubs.Methods;

// =============================================================================
// METHOD SEQUENCES
// =============================================================================

[KnockOff<ICalculator>]
public partial class MethodSequencesDemo
{
    // =========================================================================
    // OnCall().ThenCall() - Callback Sequences
    // =========================================================================
    // DESIGN DECISION: ThenCall() chains multiple callbacks that execute in order.
    // Each call to the method advances through the sequence.
    //
    // GENERATOR BEHAVIOR: ThenCall() converts the interceptor to sequence mode:
    //
    //   public class AddInterceptor
    //   {
    //       private Queue<Func<int, int, int>>? _callbackSequence;
    //
    //       public IMethodSequence<Func<int, int, int>, int> ThenCall(Func<int, int, int> callback)
    //       {
    //           _callbackSequence ??= new Queue<...>();
    //           _callbackSequence.Enqueue(callback);
    //           return this;
    //       }
    //   }
    //
    // DESIGN DECISION: Lazy elevation - the interceptor only enters sequence mode
    // when ThenCall() is first called. Before that, OnCall() behaves normally.
    // =========================================================================

    public void ThenCall_CreatesSequenceOfCallbacks()
    {
        var stub = new Stubs.ICalculator();
        int callCount = 0;

        // First call: returns 1
        // Second call: returns 2
        // Third call: returns 3
        // Fourth+ calls: sequence exhausted, returns default (or throws in strict mode)
        stub.Add.OnCall((a, b) =>
        {
            callCount++;
            return 1;
        }).ThenCall((a, b) =>
        {
            callCount++;
            return 2;
        }).ThenCall((a, b) =>
        {
            callCount++;
            return 3;
        });

        ICalculator calc = stub;

        var r1 = calc.Add(0, 0); // r1 == 1
        var r2 = calc.Add(0, 0); // r2 == 2
        var r3 = calc.Add(0, 0); // r3 == 3
        var r4 = calc.Add(0, 0); // r4 == 0 (sequence exhausted, returns default)
    }

    // =========================================================================
    // COMMON MISTAKE: Returns().ThenCall() Pattern
    // =========================================================================
    //
    // COMMON MISTAKE: Mixing Returns() and ThenCall() in sequences
    //
    // WRONG:
    //   stub.Add.Returns(1).ThenCall((a, b) => 2);
    //
    // WHY WRONG: Returns() sets _callback to null. ThenCall() expects an
    // existing callback to chain from. The sequence won't work correctly.
    //
    // RIGHT:
    //   stub.Add.OnCall((a, b) => 1).ThenCall((a, b) => 2);
    //
    // DESIGN DECISION: We don't provide Returns().ThenReturns() because:
    // 1. It would require a different sequence implementation
    // 2. OnCall with constant-returning callbacks achieves the same result
    // 3. Keeping one consistent pattern (OnCall-based) is simpler
    //
    // DID NOT DO THIS: Provide Returns().ThenReturns() API
    //
    // REJECTED PATTERN:
    //   stub.Add.Returns(1).ThenReturns(2).ThenReturns(3);
    //
    // WHY NOT: Would require two parallel sequence systems (one for values,
    // one for callbacks). The OnCall-based approach is more flexible since
    // callbacks can return constants or compute values.
    // =========================================================================

    public void Sequence_UseOnCallForConstantSequences()
    {
        var stub = new Stubs.ICalculator();

        // For constant sequences, use OnCall with constant-returning lambdas
        stub.Add
            .OnCall((_, _) => 1)
            .ThenCall((_, _) => 2)
            .ThenCall((_, _) => 3);

        ICalculator calc = stub;

        var r1 = calc.Add(0, 0); // 1
        var r2 = calc.Add(0, 0); // 2
        var r3 = calc.Add(0, 0); // 3
    }

    // =========================================================================
    // Sequence Exhaustion Behavior
    // =========================================================================
    // DESIGN DECISION: Method sequences (OnCall/ThenCall) exhaust after all
    // callbacks have been consumed. After exhaustion:
    // - Non-strict mode: returns default(T)
    // - Strict mode: throws StubException.SequenceExhausted
    //
    // DID NOT DO THIS: Last callback repeats indefinitely
    //
    // REJECTED BEHAVIOR:
    //   stub.Add.OnCall(() => 1).ThenCall(() => 999);
    //   calc.Add(0, 0);  // 1
    //   calc.Add(0, 0);  // 999
    //   calc.Add(0, 0);  // 999 (repeats forever)
    //
    // ACTUAL BEHAVIOR: After all callbacks consumed, sequence is exhausted:
    //   calc.Add(0, 0);  // 1
    //   calc.Add(0, 0);  // 999
    //   calc.Add(0, 0);  // 0 (default - sequence exhausted)
    //
    // WHY: This matches the verification semantics. If you configured 3 callbacks,
    // you expect exactly 3 calls. Repeating the last callback would mask bugs
    // where code calls the method more times than expected.
    // =========================================================================

    public void SequenceExhaustion_StrictModeThrows()
    {
        var stub = new Stubs.ICalculator();
        stub.Strict = true;

        // With only one ThenCall, we have two total callbacks
        stub.Add
            .OnCall((a, b) => 100)
            .ThenCall((a, b) => 200);

        ICalculator calc = stub;

        var r1 = calc.Add(0, 0); // 100
        var r2 = calc.Add(0, 0); // 200
        // var r3 = calc.Add(0, 0); // Would throw StubException.SequenceExhausted
    }

    // =========================================================================
    // Sequence Behavior: Sequence Exhausts After All Callbacks
    // =========================================================================
    // DESIGN DECISION: Sequences exhaust after all callbacks are consumed.
    // In non-strict mode, exhausted sequences return default(T).
    //
    // This allows tests to verify that methods are called the expected number
    // of times. If the last callback repeated, extra calls would be silently
    // handled instead of detected.
    // =========================================================================

    public void Sequence_ExhaustsAfterAllCallbacks()
    {
        var stub = new Stubs.ICalculator();

        stub.Add
            .OnCall((a, b) => 1)
            .ThenCall((a, b) => 999);

        ICalculator calc = stub;

        var r1 = calc.Add(0, 0); // 1 (first callback)
        var r2 = calc.Add(0, 0); // 999 (second callback)
        var r3 = calc.Add(0, 0); // 0 (default - sequence exhausted)
        var r4 = calc.Add(0, 0); // 0 (still exhausted)
    }

    // =========================================================================
    // Void Method Sequences
    // =========================================================================
    // DESIGN DECISION: Void methods support OnCall().ThenCall() sequences too.
    // Each callback is invoked in sequence.
    //
    // GENERATOR BEHAVIOR: VoidMethodInterceptor has the same sequence support:
    //
    //   public class ResetInterceptor : VoidMethodInterceptor<Unit>
    //   {
    //       public IVoidMethodSequence ThenCall(Action callback) { ... }
    //       public IVoidMethodSequence ThenNone() { ... }
    //   }
    // =========================================================================

    public void VoidMethods_SupportSequences()
    {
        var stub = new Stubs.ICalculator();
        var log = new List<string>();

        stub.Reset
            .OnCall(() => log.Add("First reset"))
            .ThenCall(() => log.Add("Second reset"))
            .ThenCall(() => log.Add("Subsequent reset"));

        ICalculator calc = stub;

        calc.Reset(); // log: ["First reset"]
        calc.Reset(); // log: ["First reset", "Second reset"]
        calc.Reset(); // log: ["First reset", "Second reset", "Subsequent reset"]
        calc.Reset(); // No callback - sequence exhausted
    }

    // =========================================================================
    // Sequences vs. When Chains - Priority
    // =========================================================================
    // PRIORITY ORDER for method resolution:
    //
    // 1. When chains (highest) - stub.Add.When(1, 2).Returns(100)
    // 2. Sequences            - stub.Add.OnCall(() => 1).ThenCall(() => 2)
    // 3. Returns              - stub.Add.Returns(42)
    // 4. OnCall               - stub.Add.OnCall((a, b) => a + b)
    // 5. Source               - stub.Source(realImpl)
    // 6. SmartDefault         - default(T) or null
    //
    // When chains take precedence over sequences. This allows specific
    // argument matches to override the general sequence behavior.
    // =========================================================================

    public void Sequences_InteractWithWhenChains()
    {
        var stub = new Stubs.ICalculator();

        // General sequence behavior
        stub.Add
            .OnCall((a, b) => 1)
            .ThenCall((a, b) => 2);

        // Specific argument match (higher priority)
        stub.Add.When(99, 99).Returns(9999);

        ICalculator calc = stub;

        var r1 = calc.Add(0, 0);   // 1 (sequence)
        var r2 = calc.Add(99, 99); // 9999 (When match takes priority)
        var r3 = calc.Add(0, 0);   // 2 (sequence advances)
        var r4 = calc.Add(99, 99); // 9999 (When still matches)
    }
}
