// -----------------------------------------------------------------------------
// Design.Stubs - Method Sequences
// -----------------------------------------------------------------------------
// This file demonstrates sequence APIs for methods:
// - Return(first, params rest) for concise value sequences (NSubstitute-style)
// - Call().ThenReturn(params values) for adding multiple values to sequences
// - Call().ThenReturn() for callback sequences
// - Call().ThenReturn() for value sequences
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
// NSUBSTITUTE COMPARISON: KnockOff now matches NSubstitute's concise syntax.
//
// NSubstitute:
//   substitute.Method().Return(1, 2, 3);
//   substitute.Method(); // 1
//   substitute.Method(); // 2
//   substitute.Method(); // 3
//   substitute.Method(); // 3 (repeats last)
//
// KnockOff (NEW - NSubstitute-style params syntax):
//   stub.Method.Return(1, 2, 3);          // Concise sequence syntax
//   stub.Method(); // 1
//   stub.Method(); // 2
//   stub.Method(); // 3
//   stub.Method(); // 3 (repeats last)
//
// KnockOff (original explicit syntax - still supported):
//   stub.Method.Call(() => 1).ThenReturn(2).ThenReturn(3);
//
// KnockOff extends this with ThenDefault() for explicit default termination:
//   stub.Method.Call(() => 1).ThenReturn(2).ThenDefault();
//   stub.Method(); // 1
//   stub.Method(); // 2
//   stub.Method(); // default(T) - NSubstitute has no equivalent
//
// =============================================================================

[KnockOff<ICalculator>]
[KnockOff<IDataService>]
public partial class MethodSequencesDemo
{
    // =========================================================================
    // Return(first, params rest) - Concise Value Sequences (NSubstitute-style)
    // =========================================================================
    // DESIGN DECISION: Return(first, params rest) creates a sequence from
    // multiple values in a single call, matching NSubstitute's Returns(x, y, z).
    //
    // NSUBSTITUTE COMPARISON:
    //   NSubstitute:  substitute.Method().Return(1, 2, 3);
    //   KnockOff:     stub.Method.Return(1, 2, 3);
    //
    // Both produce identical behavior: returns 1, 2, 3, then repeats 3.
    //
    // GENERATOR BEHAVIOR: The params overload is distinct from Return(value):
    //
    //   // Single value - repeats indefinitely
    //   public MethodCallBuilderImpl Return(int value) { ... }
    //
    //   // Params - creates sequence, returns MethodSequenceImpl
    //   public MethodSequenceImpl Return(int first, params int[] rest) { ... }
    //
    // C# overload resolution ensures Return(42) calls the single-value overload.
    // =========================================================================

    public void Returns_Params_CreatesSequence()
    {
        var stub = new Stubs.ICalculator();

        // NSubstitute-style: Returns(first, params rest)
        // Creates sequence: 1, 2, 3, then repeats 3
        stub.Add.Return(1, 2, 3);

        ICalculator calc = stub;

        var r1 = calc.Add(0, 0); // 1
        var r2 = calc.Add(0, 0); // 2
        var r3 = calc.Add(0, 0); // 3
        var r4 = calc.Add(0, 0); // 3 (repeats last)
    }

    public void Returns_SingleValue_RepeatsIndefinitely()
    {
        var stub = new Stubs.ICalculator();

        // Single value - no params - repeats forever
        stub.Add.Return(42);

        ICalculator calc = stub;

        var r1 = calc.Add(0, 0); // 42
        var r2 = calc.Add(0, 0); // 42
        var r3 = calc.Add(0, 0); // 42 (still 42)
    }

    // =========================================================================
    // ThenReturn(params values) - Add Multiple Values to Sequence
    // =========================================================================
    // DESIGN DECISION: ThenReturn(params values) adds multiple values at once.
    // This is useful when building sequences with Call(callback) for the first value.
    //
    // GENERATOR BEHAVIOR: Params version loops over single-value ThenReturn:
    //
    //   public MethodSequenceImpl ThenReturn(params int[] values)
    //   {
    //       foreach (var value in values)
    //           ThenReturn(value);
    //       return this;
    //   }
    // =========================================================================

    public void ThenReturns_Params_AddsMultipleValues()
    {
        var stub = new Stubs.ICalculator();

        // Returns callback for first, then add multiple with params
        stub.Add
            .Call(args => args.a + args.b)  // First: compute a + b
            .ThenReturn(100, 200, 300);  // Then: 100, 200, 300

        ICalculator calc = stub;

        var r1 = calc.Add(1, 2);   // 3 (computed)
        var r2 = calc.Add(0, 0);   // 100
        var r3 = calc.Add(0, 0);   // 200
        var r4 = calc.Add(0, 0);   // 300
        var r5 = calc.Add(0, 0);   // 300 (repeats last)
    }

    // =========================================================================
    // Async Methods with Params - Auto-Wrapping
    // =========================================================================
    // DESIGN DECISION: For Task<T> and ValueTask<T> methods, params values are
    // auto-wrapped just like single-value Return(). No Task.FromResult needed.
    //
    // GENERATOR BEHAVIOR: The first value uses Task.FromResult in the Return,
    // and subsequent values use the existing ThenReturn auto-wrapping:
    //
    //   public MethodSequenceImpl Return(string first, params string[] rest)
    //   {
    //       var seq = Call(() => Task.FromResult(first));
    //       foreach (var value in rest)
    //           seq = seq.ThenReturn(value);  // ThenReturn auto-wraps
    //       return seq;
    //   }
    // =========================================================================

    public async Task Returns_Params_AsyncAutoWraps()
    {
        var stub = new Stubs.IDataService();

        // Params with async - values auto-wrapped
        stub.GetDataAsync.Return("first", "second", "third");

        IDataService service = stub;

        var r1 = await service.GetDataAsync(1); // "first"
        var r2 = await service.GetDataAsync(2); // "second"
        var r3 = await service.GetDataAsync(3); // "third"
        var r4 = await service.GetDataAsync(4); // "third" (repeats)
    }

    // =========================================================================
    // Params Sequence Verification
    // =========================================================================
    // DESIGN DECISION: Params sequences return MethodSequenceImpl, which
    // supports Verify() and Verifiable() for sequence completion checks.
    // =========================================================================

    public void Returns_Params_SupportsVerification()
    {
        var stub = new Stubs.ICalculator();

        // Returns with params is verifiable
        var sequence = stub.Add.Return(1, 2, 3);

        ICalculator calc = stub;

        calc.Add(0, 0); // 1
        calc.Add(0, 0); // 2
        calc.Add(0, 0); // 3

        // Verify sequence was fully consumed
        sequence.Verify(); // Passes - all 3 values used
    }

    // =========================================================================
    // Return(value).ThenReturn(value) - Value-Based Sequence Start
    // =========================================================================
    // DESIGN DECISION: Return(value) can now be followed by ThenReturn(value)
    // to create sequences entirely from values. Previously this caused an NRE
    // because _call was null during sequence elevation.
    //
    // This is the most concise syntax for constant-value sequences:
    //   stub.Method.Return(1).ThenReturn(2).ThenReturn(3);
    //
    // For even more concise syntax, use params:
    //   stub.Method.Return(1, 2, 3);
    // =========================================================================

    public void ReturnValue_ThenReturnValue_Sequence()
    {
        var stub = new Stubs.ICalculator();

        // Value-based sequence: Return(value).ThenReturn(value)
        stub.Add.Return(1).ThenReturn(2).ThenReturn(3);

        ICalculator calc = stub;

        var r1 = calc.Add(0, 0); // 1
        var r2 = calc.Add(0, 0); // 2
        var r3 = calc.Add(0, 0); // 3
        var r4 = calc.Add(0, 0); // 3 (repeats last)
    }

    // =========================================================================
    // Call().ThenReturn() - Callback Sequences
    // =========================================================================
    // DESIGN DECISION: ThenReturn(callback) chains multiple callbacks that execute
    // in order. Each call to the method advances through the sequence.
    //
    // GENERATOR BEHAVIOR: ThenReturn(callback) converts the interceptor to
    // sequence mode. The fully generated interceptor manages the sequence
    // internally.
    //
    // DESIGN DECISION: Lazy elevation - the interceptor only enters sequence mode
    // when ThenReturn() is first called. Before that, Call/Return behaves normally.
    // =========================================================================

    public void ThenReturns_CreatesSequenceOfCallbacks()
    {
        var stub = new Stubs.ICalculator();
        int callCount = 0;

        // First call: returns 1
        // Second call: returns 2
        // Third call: returns 3
        // Fourth+ calls: repeats last value (3) - NSubstitute-like behavior
        // Use ThenDefault() to return default(T) instead, or Strict mode to throw
        stub.Add.Call(args =>
        {
            callCount++;
            return 1;
        }).ThenReturn(args =>
        {
            callCount++;
            return 2;
        }).ThenReturn(args =>
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
    // Call().ThenReturn() - Value Sequences (Explicit Syntax)
    // =========================================================================
    // DESIGN DECISION: ThenReturn(value) provides a cleaner syntax for
    // sequences of constant values, avoiding the need for explicit lambdas.
    //
    // For most use cases, prefer Return(x, y, z) which is more concise.
    // Use Call(callback).ThenReturn() when you need a callback for the first value.
    //
    // GENERATOR BEHAVIOR: ThenReturn(value) wraps the value in a callback
    // internally.
    //
    // For async methods, ThenReturn auto-wraps values:
    //   Task<T>:      ThenReturn(value) => wraps in Task.FromResult(value)
    //   ValueTask<T>: ThenReturn(value) => wraps in new ValueTask<T>(value)
    // =========================================================================

    public void ThenReturns_CreatesSequenceOfValues()
    {
        var stub = new Stubs.ICalculator();

        // Call starts the sequence, ThenReturn adds values
        stub.Add
            .Call(_ => 1)
            .ThenReturn(2)
            .ThenReturn(3);

        ICalculator calc = stub;

        var r1 = calc.Add(0, 0); // 1 (first callback)
        var r2 = calc.Add(0, 0); // 2 (value wrapped as callback)
        var r3 = calc.Add(0, 0); // 3 (value wrapped as callback)
        var r4 = calc.Add(0, 0); // 3 (repeats last value)
    }

    // =========================================================================
    // Mixed Sequences - Callbacks and Values Together
    // =========================================================================
    // DESIGN DECISION: ThenReturn(value) and ThenReturn(callback) can be freely mixed in the
    // same sequence. This allows computed values at some positions and constants
    // at others.
    // =========================================================================

    public void MixedSequence_CallbacksAndValues()
    {
        var stub = new Stubs.ICalculator();
        int computedValue = 0;

        // Mix callbacks and values in the same sequence
        stub.Add
            .Call(args =>
            {
                computedValue = args.a + args.b;
                return computedValue;
            })
            .ThenReturn(100)  // Constant value
            .ThenReturn(args => args.a * args.b)  // Computed value
            .ThenReturn(999);  // Another constant

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
    //   stub.Add.Call(() => 1).ThenReturn(() => 999);
    //   calc.Add(0, 0);  // 1
    //   calc.Add(0, 0);  // 999
    //   calc.Add(0, 0);  // 999 (repeats forever)
    //
    // EXPLICIT DEFAULT TERMINATION (ThenDefault):
    //   stub.Add.Call(() => 1).ThenReturn(() => 999).ThenDefault();
    //   calc.Add(0, 0);  // 1
    //   calc.Add(0, 0);  // 999
    //   calc.Add(0, 0);  // 0 (default(T) after exhaustion)
    //
    // STRICT MODE (throws exception):
    //   stub.Strict = true;
    //   stub.Add.Call(() => 1).ThenReturn(() => 999);
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

        // With only one ThenReturn, we have two total callbacks
        stub.Add
            .Call(_ => 100)
            .ThenReturn(_ => 200);

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
            .Call(_ => 1)
            .ThenReturn(_ => 999);

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
            .Call(_ => 1)
            .ThenReturn(_ => 999)
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
    // DESIGN DECISION: Void methods support Call().ThenCall() sequences too.
    // Each callback is invoked in sequence.
    //
    // GENERATOR BEHAVIOR: The fully generated interceptor has the same sequence
    // support:
    //
    //   public sealed class ResetInterceptor : MethodInterceptorRuntime
    //   {
    //       public MethodSequenceImpl ThenCall(Action callback) { ... }
    //       public MethodSequenceImpl ThenNone() { ... }
    //   }
    // =========================================================================

    public void VoidMethods_SupportSequences()
    {
        var stub = new Stubs.ICalculator();
        var log = new List<string>();

        stub.Reset
            .Call(() => log.Add("First reset"))
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
    // 1. When chains (highest) - stub.Add.When(1, 2).Return(100)
    // 2. Sequences            - stub.Add.Call(() => 1).ThenReturn(() => 2)
    // 3. Return(value)         - stub.Add.Return(42)
    // 4. Call(callback)       - stub.Add.Call((a, b) => a + b)
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
            .Call(_ => 1)
            .ThenReturn(_ => 2);

        // Specific argument match (higher priority)
        stub.Add.When(99, 99).Return(9999);

        ICalculator calc = stub;

        var r1 = calc.Add(0, 0);   // 1 (sequence)
        var r2 = calc.Add(99, 99); // 9999 (When match takes priority)
        var r3 = calc.Add(0, 0);   // 2 (sequence advances)
        var r4 = calc.Add(99, 99); // 9999 (When still matches)
    }
}
