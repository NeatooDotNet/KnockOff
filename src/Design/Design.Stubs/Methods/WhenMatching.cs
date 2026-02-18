// -----------------------------------------------------------------------------
// Design.Stubs - When() Parameter-Specific Matching
// -----------------------------------------------------------------------------
// This file demonstrates the When() API for parameter-specific matching:
// - When(args).Return(value) for value equality matching
// - When(predicate).Return(value) for predicate matching
// - ThenWhen() for chaining multiple matchers
// - Void method variants with IVoidWhenChain
// - When chain verification with Verify(Called)
// -----------------------------------------------------------------------------

using Design.Domain.Services;
using KnockOff;

namespace Design.Stubs.Methods;

// =============================================================================
// WHEN() PARAMETER-SPECIFIC MATCHING
// =============================================================================

[KnockOff<ICalculator>]
[KnockOff<IDataService>]
public partial class WhenMatchingDemo
{
    // =========================================================================
    // When(args).Return(value) - Value Equality Matching
    // =========================================================================
    // DESIGN DECISION: When() matches specific argument values using equality.
    // When the method is called with matching arguments, the configured return
    // value is used instead of the default Returns behavior.
    //
    // GENERATOR BEHAVIOR: When() generates a WhenBuilder specific to the method:
    //
    //   public sealed class MethodInterceptor<TDelegate, TArgs, TReturn>
    //   {
    //       // TArgs is (int a, int b) for Add(int a, int b)
    //       public WhenBuilder When(TArgs args) { ... }  // Value equality
    //       public WhenBuilder When(Func<TArgs, bool> predicate) { ... }  // Predicate
    //   }
    //
    //   public sealed class WhenBuilder
    //   {
    //       public WhenChain Return(TReturn value) { ... }
    //   }
    //
    //   public sealed class WhenChain
    //   {
    //       public WhenBuilder ThenWhen(TArgs args) { ... }
    //       public WhenBuilder ThenWhen(Func<TArgs, bool> predicate) { ... }
    //   }
    //
    // DESIGN DECISION: Method-specific WhenBuilder/WhenChain classes are needed
    // because ThenWhen() needs to know the exact parameter types to generate
    // the correct method signature.
    // =========================================================================

    public void When_MatchesSpecificValues()
    {
        var stub = new Stubs.ICalculator();

        // Default behavior for unmatched calls
        stub.Add.Return(0);

        // Specific matches take precedence
        stub.Add.When(1, 2).Return(100);
        stub.Add.When(5, 5).Return(500);

        ICalculator calc = stub;

        var r1 = calc.Add(1, 2);   // 100 (matched)
        var r2 = calc.Add(5, 5);   // 500 (matched)
        var r3 = calc.Add(3, 4);   // 0 (no match, falls to Returns)
    }

    // =========================================================================
    // When(predicate).Return(value) - Predicate Matching
    // =========================================================================
    // DESIGN DECISION: When() also accepts a predicate function for complex
    // matching logic. The predicate receives the arguments and returns bool.
    //
    // This is useful for:
    // - Range checks: When(args => args.a > 0 && args.b > 0)
    // - Pattern matching: When((s) => s.StartsWith("prefix"))
    // - Complex conditions that can't be expressed with equality
    //
    // GENERATOR BEHAVIOR: The predicate version uses Func<TArgs, bool>:
    //
    //   public WhenBuilder When(Func<TArgs, bool> predicate) { ... }
    //   // For Add(int a, int b): When(Func<(int a, int b), bool> predicate)
    // =========================================================================

    public void When_PredicateForComplexMatching()
    {
        var stub = new Stubs.ICalculator();

        stub.Add.Return(0);

        // Match when both arguments are positive
        stub.Add.When(args => args.a > 0 && args.b > 0).Return(42);

        // Match when either argument is negative
        stub.Add.When(args => args.a < 0 || args.b < 0).Return(-1);

        ICalculator calc = stub;

        var r1 = calc.Add(1, 2);   // 42 (both positive)
        var r2 = calc.Add(-1, 5);  // -1 (has negative)
        var r3 = calc.Add(0, 0);   // 0 (no match, returns default)
    }

    // =========================================================================
    // When Chain with Computed Values
    // =========================================================================
    // DESIGN DECISION: When chains use Return(value) only - no Call(callback) variant.
    // For dynamic behavior based on matched arguments, use Call(callback) without When.
    //
    // DID NOT DO THIS: Add When().Call(callback) for dynamic returns on match
    //
    // REJECTED PATTERN:
    //   stub.Add.When(10, 10).Call((a, b) => a * b);  // Does NOT exist
    //
    // WHY NOT: When() is for simple "if args match, return value" scenarios.
    // For dynamic behavior, use Call(callback):
    // =========================================================================

    public void When_UsesReturnsValue()
    {
        var stub = new Stubs.ICalculator();

        stub.Add.Return(0);

        // When() uses Return() for the match result
        stub.Add.When(10, 10).Return(100);

        // For dynamic behavior on ALL calls:
        // stub.Add.Call((a, b) => a * b);

        ICalculator calc = stub;

        var r1 = calc.Add(10, 10); // 100 (matched)
        var r2 = calc.Add(5, 5);   // 0 (no match, falls to Returns)
    }

    // =========================================================================
    // ThenWhen() - Chaining Multiple Matchers
    // =========================================================================
    // DESIGN DECISION: ThenWhen() allows chaining multiple matchers on the
    // same When chain. Each matcher is checked in order until one matches.
    //
    // GENERATOR BEHAVIOR: ThenWhen() returns a new WhenBuilder:
    //
    //   public sealed class WhenChain
    //   {
    //       public WhenBuilder ThenWhen(TArgs args) { ... }
    //       public WhenBuilder ThenWhen(Func<TArgs, bool> predicate) { ... }
    //   }
    //
    // DID NOT DO THIS: Require separate When() calls for each matcher
    //
    // REJECTED PATTERN:
    //   stub.Add.When(1, 1).Return(1);
    //   stub.Add.When(2, 2).Return(2);
    //   stub.Add.When(3, 3).Return(3);
    //
    // WHY ThenWhen EXISTS: While separate When() calls work fine, ThenWhen()
    // allows building related matchers as a logical group. This can be
    // clearer when the matchers are related or need to be verified together.
    // =========================================================================

    public void ThenWhen_ChainsMultipleMatchers()
    {
        var stub = new Stubs.ICalculator();

        stub.Add.Return(0);

        // Chain of related matchers
        stub.Add
            .When(1, 1).Return(1)
            .ThenWhen(2, 2).Return(2)
            .ThenWhen(3, 3).Return(3);

        ICalculator calc = stub;

        var r1 = calc.Add(1, 1); // 1
        var r2 = calc.Add(2, 2); // 2
        var r3 = calc.Add(3, 3); // 3
        var r4 = calc.Add(4, 4); // 0 (no match)
    }

    // =========================================================================
    // Null Matching
    // =========================================================================
    // COMMON MISTAKE: Trying to match null with When(null)
    //
    // For reference types, When(null) may not work as expected because C#
    // might interpret it as a predicate overload.
    //
    // RIGHT:
    //   stub.Method.When((s) => s == null).Return(defaultValue);
    //
    // Use a predicate to explicitly check for null.
    // =========================================================================

    public async Task When_NullMatching_UsePredicate()
    {
        var stub = new Stubs.IDataService();

        // Use predicate to match null
        stub.GetDataAsync.When((id) => id < 0).Return("not found");

        // For nullable parameters, use predicate form
        // stub.SomeMethod.When((s) => s == null).Return("default");

        IDataService service = stub;
        var result = await service.GetDataAsync(-1);
        // result == "not found"
    }

    // =========================================================================
    // When Chain Priority
    // =========================================================================
    // DESIGN DECISION: When chains are checked in the order they were added.
    // The first matching chain wins.
    //
    // This means more specific matchers should be added first if there's
    // potential for overlap.
    // =========================================================================

    public void When_OrderMatters()
    {
        var stub = new Stubs.ICalculator();

        stub.Add.Return(0);

        // Order matters! First match wins.
        stub.Add.When(args => args.a > 0).Return(100);  // Added first
        stub.Add.When(5, 5).Return(500);              // Added second

        ICalculator calc = stub;

        // (5, 5) matches FIRST predicate (a > 0), so returns 100, not 500
        var result = calc.Add(5, 5); // 100!

        // To get expected behavior, add specific matchers first:
        // stub.Add.When(5, 5).Return(500);
        // stub.Add.When((a, b) => a > 0).Return(100);
    }

    // =========================================================================
    // Void Method When Chains
    // =========================================================================
    // DESIGN DECISION: Void methods use IVoidWhenChain instead of IWhenChain.
    // There's no Return() - instead use Call() for optional callback.
    //
    // GENERATOR BEHAVIOR: Void method When chains:
    //
    //   public class ResetInterceptor
    //   {
    //       public ResetWhenChain When() { ... }  // No args for parameterless
    //   }
    //
    //   public class ResetWhenChain : IVoidWhenChain
    //   {
    //       public IVoidWhenChain Call(Action callback) { ... }
    //       public IVoidWhenChain ThenWhen() { ... }
    //       public void Verify(Called called) { ... }
    //   }
    // =========================================================================

    // Note: ICalculator.Reset() has no parameters, so When() doesn't make sense.
    // A parameterized void method would demonstrate this better.
    // See Verification for void method examples.

    // =========================================================================
    // When Chain Verification (Void Methods Only)
    // =========================================================================
    // DESIGN DECISION: Parameter-specific verification with Verify(Called) is
    // available on IVoidWhenChain (void methods) but not on IWhenChain
    // (return methods).
    //
    // For return methods, use Verifiable() to mark for batch verification:
    //
    //   stub.Add.When(1, 2).Return(100).Verifiable();
    //   // Later:
    //   stub.Verify();  // Checks all Verifiable() marked items
    //
    // DID NOT DO THIS: Add Verify(Called) to IWhenChain for return methods
    //
    // WHY NOT: The implementation complexity of tracking call counts per
    // When matcher for return methods was deferred. Batch verification via
    // Verifiable() covers most use cases.
    // =========================================================================

    public void When_UseVerifiableForBatchVerification()
    {
        var stub = new Stubs.ICalculator();

        // Mark When chains for batch verification
        stub.Add.When(1, 2).Return(100).Verifiable();
        stub.Add.When(5, 5).Return(500).Verifiable();

        ICalculator calc = stub;

        calc.Add(1, 2);
        calc.Add(5, 5);

        // Verify all Verifiable() marked items
        stub.Verify();
    }

    // =========================================================================
    // Async Method When Chains
    // =========================================================================
    // DESIGN DECISION: Async methods work identically to sync methods with
    // When chains. Return() auto-wraps with Task.FromResult.
    // =========================================================================

    public async Task When_WorksWithAsyncMethods()
    {
        var stub = new Stubs.IDataService();

        stub.GetDataAsync.When(1).Return("Item 1");
        stub.GetDataAsync.When(2).Return("Item 2");
        stub.GetDataAsync.When((id) => id > 100).Return("Bulk item");

        IDataService service = stub;

        var r1 = await service.GetDataAsync(1);    // "Item 1"
        var r2 = await service.GetDataAsync(2);    // "Item 2"
        var r3 = await service.GetDataAsync(999);  // "Bulk item"
    }

    // =========================================================================
    // DESIGN DECISION SUMMARY: Why Method-Specific When Classes
    // =========================================================================
    //
    // Each method generates its own WhenBuilder and WhenChain classes because:
    //
    // With the TTuple approach, When/ThenWhen are pre-compiled on the
    // interceptor class itself. TArgs is the tuple type (e.g., (int a, int b)),
    // so When() and ThenWhen() accept tuples directly:
    //
    //   stub.Add.When(1, 2).Return(100);   // Tuple value matching
    //   stub.Add.When(args => args.a > 0).Return(42);  // Predicate matching
    //
    // Named tuple fields (args.a, args.b) provide natural access in predicates.
    // =========================================================================
}
