// -----------------------------------------------------------------------------
// Design.Stubs - Method Sequences
// -----------------------------------------------------------------------------
// This file demonstrates sequence APIs for methods:
// - OnCall().ThenCall() for callback sequences
// - OnCall().ThenReturns() for value sequences
// - ThenDefault() to return default(T) after exhaustion instead of repeating
// - Lazy elevation from builder to sequence mode
// - Sequence exhaustion behavior (repeat last value by default, NSubstitute-like)
// -----------------------------------------------------------------------------

using Design.Domain.Services;
using KnockOff;

namespace Design.Stubs.Methods;

// =============================================================================
// METHOD SEQUENCES
// =============================================================================
//
// NSUBSTITUTE COMPARISON: KnockOff matches NSubstitute's sequence exhaustion behavior.
//
// NSubstitute:
//   substitute.Method().Returns(1, 2, 3);
//   substitute.Method(); // 1
//   substitute.Method(); // 2
//   substitute.Method(); // 3
//   substitute.Method(); // 3 (repeats last)
//
// KnockOff (equivalent):
//   stub.Method.OnCall(() => 1).ThenReturns(2).ThenReturns(3);
//   stub.Method(); // 1
//   stub.Method(); // 2
//   stub.Method(); // 3
//   stub.Method(); // 3 (repeats last)
//
// KnockOff extends this with ThenDefault() for explicit default termination:
//   stub.Method.OnCall(() => 1).ThenReturns(2).ThenDefault();
//   stub.Method(); // 1
//   stub.Method(); // 2
//   stub.Method(); // default(T) - NSubstitute has no equivalent
//
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
        // Fourth+ calls: repeats last value (3) - NSubstitute-like behavior
        // Use ThenDefault() to return default(T) instead, or Strict mode to throw
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
        var r4 = calc.Add(0, 0); // r4 == 3 (repeats last value)
    }

    // =========================================================================
    // OnCall().ThenReturns() - Value Sequences
    // =========================================================================
    // DESIGN DECISION: ThenReturns(value) provides a cleaner syntax for
    // sequences of constant values, avoiding the need for explicit lambdas.
    //
    // GENERATOR BEHAVIOR: ThenReturns() wraps the value in a callback:
    //
    //   public class MethodSequenceImpl
    //   {
    //       public MethodSequenceImpl ThenReturns(int value)
    //           => ThenCall((_, _) => value);
    //   }
    //
    // For async methods, ThenReturns auto-wraps values:
    //   Task<T>:      ThenReturns(value) => ThenCall(() => Task.FromResult(value))
    //   ValueTask<T>: ThenReturns(value) => ThenCall(() => new ValueTask<T>(value))
    //
    // NOTE: Returns().ThenReturns() is NOT supported. Start sequences with
    // OnCall() to enable chaining. Use OnCall(() => value) for constant first value.
    // =========================================================================

    public void ThenReturns_CreatesSequenceOfValues()
    {
        var stub = new Stubs.ICalculator();

        // OnCall starts the sequence, ThenReturns adds values
        stub.Add
            .OnCall((_, _) => 1)
            .ThenReturns(2)
            .ThenReturns(3);

        ICalculator calc = stub;

        var r1 = calc.Add(0, 0); // 1 (first callback)
        var r2 = calc.Add(0, 0); // 2 (value wrapped as callback)
        var r3 = calc.Add(0, 0); // 3 (value wrapped as callback)
        var r4 = calc.Add(0, 0); // 3 (repeats last value)
    }

    // =========================================================================
    // Mixed Sequences - Callbacks and Values Together
    // =========================================================================
    // DESIGN DECISION: ThenReturns() and ThenCall() can be freely mixed in the
    // same sequence. This allows computed values at some positions and constants
    // at others.
    // =========================================================================

    public void MixedSequence_CallbacksAndValues()
    {
        var stub = new Stubs.ICalculator();
        int computedValue = 0;

        // Mix callbacks and values in the same sequence
        stub.Add
            .OnCall((a, b) =>
            {
                computedValue = a + b;
                return computedValue;
            })
            .ThenReturns(100)  // Constant value
            .ThenCall((a, b) => a * b)  // Computed value
            .ThenReturns(999);  // Another constant

        ICalculator calc = stub;

        var r1 = calc.Add(2, 3);   // 5 (computed: 2+3)
        var r2 = calc.Add(0, 0);   // 100 (constant)
        var r3 = calc.Add(4, 5);   // 20 (computed: 4*5)
        var r4 = calc.Add(0, 0);   // 999 (constant)
    }

    // =========================================================================
    // Sequence Exhaustion Behavior
    // =========================================================================
    // DESIGN DECISION: Method sequences repeat the last callback after exhaustion.
    // This matches NSubstitute's behavior for easier migration and more forgiving tests.
    //
    // DEFAULT BEHAVIOR (repeat last value):
    //   stub.Add.OnCall(() => 1).ThenCall(() => 999);
    //   calc.Add(0, 0);  // 1
    //   calc.Add(0, 0);  // 999
    //   calc.Add(0, 0);  // 999 (repeats forever)
    //
    // EXPLICIT DEFAULT TERMINATION (ThenDefault):
    //   stub.Add.OnCall(() => 1).ThenCall(() => 999).ThenDefault();
    //   calc.Add(0, 0);  // 1
    //   calc.Add(0, 0);  // 999
    //   calc.Add(0, 0);  // 0 (default(T) after exhaustion)
    //
    // STRICT MODE (throws exception):
    //   stub.Strict = true;
    //   stub.Add.OnCall(() => 1).ThenCall(() => 999);
    //   calc.Add(0, 0);  // 1
    //   calc.Add(0, 0);  // 999
    //   calc.Add(0, 0);  // Throws StubException.SequenceExhausted
    //
    // WHY REPEAT LAST (NSubstitute-like): More forgiving default reduces test
    // brittleness. Tests that verify specific call counts can use strict mode.
    // ThenDefault() provides explicit opt-in for "return default after exhaustion".
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
    // Sequence Behavior: Repeat Last Value After Exhaustion
    // =========================================================================
    // DESIGN DECISION: After all callbacks are consumed, the last callback
    // repeats indefinitely. This matches NSubstitute's behavior.
    //
    // Use ThenDefault() when you want the old "return default after exhaustion"
    // behavior, or Strict mode when you want to enforce exact call counts.
    // =========================================================================

    public void Sequence_RepeatsLastValueAfterExhaustion()
    {
        var stub = new Stubs.ICalculator();

        stub.Add
            .OnCall((a, b) => 1)
            .ThenCall((a, b) => 999);

        ICalculator calc = stub;

        var r1 = calc.Add(0, 0); // 1 (first callback)
        var r2 = calc.Add(0, 0); // 999 (second callback)
        var r3 = calc.Add(0, 0); // 999 (repeats last)
        var r4 = calc.Add(0, 0); // 999 (repeats last)
    }

    // =========================================================================
    // ThenDefault() - Explicit Default Termination
    // =========================================================================
    // DESIGN DECISION: ThenDefault() terminates the fluent chain and configures
    // the sequence to return default(T) after exhaustion instead of repeating.
    //
    // This is useful when tests need to detect or handle exhaustion explicitly.
    // =========================================================================

    public void ThenDefault_ReturnsDefaultAfterExhaustion()
    {
        var stub = new Stubs.ICalculator();

        stub.Add
            .OnCall((a, b) => 1)
            .ThenCall((a, b) => 999)
            .ThenDefault();  // Terminates chain, return default after exhaustion

        ICalculator calc = stub;

        var r1 = calc.Add(0, 0); // 1 (first callback)
        var r2 = calc.Add(0, 0); // 999 (second callback)
        var r3 = calc.Add(0, 0); // 0 (default - sequence exhausted)
        var r4 = calc.Add(0, 0); // 0 (still default)
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
        calc.Reset(); // log: [..., "Subsequent reset"] (repeats last callback)
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
