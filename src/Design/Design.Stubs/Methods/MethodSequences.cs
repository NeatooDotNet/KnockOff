// -----------------------------------------------------------------------------
// Design.Stubs - Method Sequences
// -----------------------------------------------------------------------------
// This file demonstrates sequence APIs for methods:
// - Returns(first, params rest) for concise value sequences (NSubstitute-style)
// - Returns().ThenReturns(params values) for adding multiple values to sequences
// - Returns().ThenReturns() for callback sequences
// - Returns().ThenReturns() for value sequences
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
//   substitute.Method().Returns(1, 2, 3);
//   substitute.Method(); // 1
//   substitute.Method(); // 2
//   substitute.Method(); // 3
//   substitute.Method(); // 3 (repeats last)
//
// KnockOff (NEW - NSubstitute-style params syntax):
//   stub.Method.Returns(1, 2, 3);          // Concise sequence syntax
//   stub.Method(); // 1
//   stub.Method(); // 2
//   stub.Method(); // 3
//   stub.Method(); // 3 (repeats last)
//
// KnockOff (original explicit syntax - still supported):
//   stub.Method.Returns(() => 1).ThenReturns(2).ThenReturns(3);
//
// KnockOff extends this with ThenDefault() for explicit default termination:
//   stub.Method.Returns(() => 1).ThenReturns(2).ThenDefault();
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
    // Returns(first, params rest) - Concise Value Sequences (NSubstitute-style)
    // =========================================================================
    // DESIGN DECISION: Returns(first, params rest) creates a sequence from
    // multiple values in a single call, matching NSubstitute's Returns(x, y, z).
    //
    // NSUBSTITUTE COMPARISON:
    //   NSubstitute:  substitute.Method().Returns(1, 2, 3);
    //   KnockOff:     stub.Method.Returns(1, 2, 3);
    //
    // Both produce identical behavior: returns 1, 2, 3, then repeats 3.
    //
    // GENERATOR BEHAVIOR: The params overload is distinct from Returns(value):
    //
    //   // Single value - repeats indefinitely
    //   public MethodCallBuilderImpl Returns(int value) { ... }
    //
    //   // Params - creates sequence, returns MethodSequenceImpl
    //   public MethodSequenceImpl Returns(int first, params int[] rest) { ... }
    //
    // C# overload resolution ensures Returns(42) calls the single-value overload.
    // =========================================================================

    public void Returns_Params_CreatesSequence()
    {
        var stub = new Stubs.ICalculator();

        // NSubstitute-style: Returns(first, params rest)
        // Creates sequence: 1, 2, 3, then repeats 3
        stub.Add.Returns(1, 2, 3);

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
        stub.Add.Returns(42);

        ICalculator calc = stub;

        var r1 = calc.Add(0, 0); // 42
        var r2 = calc.Add(0, 0); // 42
        var r3 = calc.Add(0, 0); // 42 (still 42)
    }

    // =========================================================================
    // ThenReturns(params values) - Add Multiple Values to Sequence
    // =========================================================================
    // DESIGN DECISION: ThenReturns(params values) adds multiple values at once.
    // This is useful when building sequences with Returns(callback) for the first value.
    //
    // GENERATOR BEHAVIOR: Params version loops over single-value ThenReturns:
    //
    //   public MethodSequenceImpl ThenReturns(params int[] values)
    //   {
    //       foreach (var value in values)
    //           ThenReturns(value);
    //       return this;
    //   }
    // =========================================================================

    public void ThenReturns_Params_AddsMultipleValues()
    {
        var stub = new Stubs.ICalculator();

        // Returns callback for first, then add multiple with params
        stub.Add
            .Returns((a, b) => a + b)  // First: compute a + b
            .ThenReturns(100, 200, 300);  // Then: 100, 200, 300

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
    // auto-wrapped just like single-value Returns(). No Task.FromResult needed.
    //
    // GENERATOR BEHAVIOR: The first value uses Task.FromResult in the Returns,
    // and subsequent values use the existing ThenReturns auto-wrapping:
    //
    //   public MethodSequenceImpl Returns(string first, params string[] rest)
    //   {
    //       var seq = Returns(() => Task.FromResult(first));
    //       foreach (var value in rest)
    //           seq = seq.ThenReturns(value);  // ThenReturns auto-wraps
    //       return seq;
    //   }
    // =========================================================================

    public async Task Returns_Params_AsyncAutoWraps()
    {
        var stub = new Stubs.IDataService();

        // Params with async - values auto-wrapped
        stub.GetDataAsync.Returns("first", "second", "third");

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
        var sequence = stub.Add.Returns(1, 2, 3);

        ICalculator calc = stub;

        calc.Add(0, 0); // 1
        calc.Add(0, 0); // 2
        calc.Add(0, 0); // 3

        // Verify sequence was fully consumed
        sequence.Verify(); // Passes - all 3 values used
    }

    // =========================================================================
    // Returns().ThenReturns() - Callback Sequences
    // =========================================================================
    // DESIGN DECISION: ThenReturns(callback) chains multiple callbacks that execute
    // in order. Each call to the method advances through the sequence.
    //
    // GENERATOR BEHAVIOR: ThenReturns(callback) converts the interceptor to sequence mode:
    //
    //   public class AddInterceptor
    //   {
    //       private Queue<Func<int, int, int>>? _callbackSequence;
    //
    //       public IMethodSequence<Func<int, int, int>, int> ThenReturns(Func<int, int, int> callback)
    //       {
    //           _callbackSequence ??= new Queue<...>();
    //           _callbackSequence.Enqueue(callback);
    //           return this;
    //       }
    //   }
    //
    // DESIGN DECISION: Lazy elevation - the interceptor only enters sequence mode
    // when ThenReturns() is first called. Before that, Returns() behaves normally.
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
        stub.Add.Returns((a, b) =>
        {
            callCount++;
            return 1;
        }).ThenReturns((a, b) =>
        {
            callCount++;
            return 2;
        }).ThenReturns((a, b) =>
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
    // Returns().ThenReturns() - Value Sequences (Explicit Syntax)
    // =========================================================================
    // DESIGN DECISION: ThenReturns(value) provides a cleaner syntax for
    // sequences of constant values, avoiding the need for explicit lambdas.
    //
    // For most use cases, prefer Returns(x, y, z) which is more concise.
    // Use Returns(callback).ThenReturns() when you need a callback for the first value.
    //
    // GENERATOR BEHAVIOR: ThenReturns() wraps the value in a callback:
    //
    //   public class MethodSequenceImpl
    //   {
    //       public MethodSequenceImpl ThenReturns(int value)
    //           => ThenReturns((_, _) => value);
    //   }
    //
    // For async methods, ThenReturns auto-wraps values:
    //   Task<T>:      ThenReturns(value) => ThenReturns(() => Task.FromResult(value))
    //   ValueTask<T>: ThenReturns(value) => ThenReturns(() => new ValueTask<T>(value))
    // =========================================================================

    public void ThenReturns_CreatesSequenceOfValues()
    {
        var stub = new Stubs.ICalculator();

        // Returns starts the sequence, ThenReturns adds values
        stub.Add
            .Returns((_, _) => 1)
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
    // DESIGN DECISION: ThenReturns(value) and ThenReturns(callback) can be freely mixed in the
    // same sequence. This allows computed values at some positions and constants
    // at others.
    // =========================================================================

    public void MixedSequence_CallbacksAndValues()
    {
        var stub = new Stubs.ICalculator();
        int computedValue = 0;

        // Mix callbacks and values in the same sequence
        stub.Add
            .Returns((a, b) =>
            {
                computedValue = a + b;
                return computedValue;
            })
            .ThenReturns(100)  // Constant value
            .ThenReturns((a, b) => a * b)  // Computed value
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
    //   stub.Add.Returns(() => 1).ThenReturns(() => 999);
    //   calc.Add(0, 0);  // 1
    //   calc.Add(0, 0);  // 999
    //   calc.Add(0, 0);  // 999 (repeats forever)
    //
    // EXPLICIT DEFAULT TERMINATION (ThenDefault):
    //   stub.Add.Returns(() => 1).ThenReturns(() => 999).ThenDefault();
    //   calc.Add(0, 0);  // 1
    //   calc.Add(0, 0);  // 999
    //   calc.Add(0, 0);  // 0 (default(T) after exhaustion)
    //
    // STRICT MODE (throws exception):
    //   stub.Strict = true;
    //   stub.Add.Returns(() => 1).ThenReturns(() => 999);
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

        // With only one ThenReturns, we have two total callbacks
        stub.Add
            .Returns((a, b) => 100)
            .ThenReturns((a, b) => 200);

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
            .Returns((a, b) => 1)
            .ThenReturns((a, b) => 999);

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
            .Returns((a, b) => 1)
            .ThenReturns((a, b) => 999)
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
    // DESIGN DECISION: Void methods support Execute().ThenExecute() sequences too.
    // Each callback is invoked in sequence.
    //
    // GENERATOR BEHAVIOR: VoidMethodInterceptor has the same sequence support:
    //
    //   public class ResetInterceptor : VoidMethodInterceptor<Unit>
    //   {
    //       public IVoidMethodSequence ThenExecute(Action callback) { ... }
    //       public IVoidMethodSequence ThenNone() { ... }
    //   }
    // =========================================================================

    public void VoidMethods_SupportSequences()
    {
        var stub = new Stubs.ICalculator();
        var log = new List<string>();

        stub.Reset
            .Execute(() => log.Add("First reset"))
            .ThenExecute(() => log.Add("Second reset"))
            .ThenExecute(() => log.Add("Subsequent reset"));

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
    // 2. Sequences            - stub.Add.Returns(() => 1).ThenReturns(() => 2)
    // 3. Returns              - stub.Add.Returns(42)
    // 4. Returns(callback)    - stub.Add.Returns((a, b) => a + b)
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
            .Returns((a, b) => 1)
            .ThenReturns((a, b) => 2);

        // Specific argument match (higher priority)
        stub.Add.When(99, 99).Returns(9999);

        ICalculator calc = stub;

        var r1 = calc.Add(0, 0);   // 1 (sequence)
        var r2 = calc.Add(99, 99); // 9999 (When match takes priority)
        var r3 = calc.Add(0, 0);   // 2 (sequence advances)
        var r4 = calc.Add(99, 99); // 9999 (When still matches)
    }
}
