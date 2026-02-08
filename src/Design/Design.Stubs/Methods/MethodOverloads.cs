// -----------------------------------------------------------------------------
// Design.Stubs - Method Overloads
// -----------------------------------------------------------------------------
// This file demonstrates how KnockOff handles method overloads:
// - Returns()/Execute() with lambda parameter type resolution
// - When() with parameter signature resolution
// - Why Returns() is not available at interceptor level for overloads
// - Tracking and verification per overload
// - Sequences with overloaded methods
// -----------------------------------------------------------------------------

using Design.Domain.Services;
using KnockOff;

namespace Design.Stubs.Methods;

// =============================================================================
// METHOD OVERLOADS
// =============================================================================
//
// DESIGN DECISION: When an interface has overloaded methods (same name,
// different signatures), KnockOff generates a SINGLE interceptor with
// multiple overloads of each API method. C# compiler resolution determines
// which overload is configured based on lambda parameter types.
//
// KEY DIFFERENCE FROM NON-OVERLOADED METHODS:
// - Non-overloaded: stub.Method.Return(42) is available
// - Overloaded: stub.Method.Return(42) does NOT exist - use Returns(callback) instead
//
// WHY: With overloads, Returns(value) would be ambiguous - should it
// configure one overload or all? Returns(callback) makes the target explicit.
// =============================================================================

[KnockOff<IFormatter>]
public partial class MethodOverloadsDemo
{
    // =========================================================================
    // Returns(callback) - Different Parameter Counts Disambiguate Automatically
    // =========================================================================
    // DESIGN DECISION: When overloads have different parameter counts, the
    // compiler resolves the correct Returns(callback) overload based on lambda arity.
    //
    // GENERATOR BEHAVIOR: Each overload gets its own Returns():
    //
    //   public class FormatInterceptor
    //   {
    //       public MethodCallBuilder Returns(Func<string, string> callback) { ... }
    //       public MethodCallBuilder Returns(Func<string, FormatOptions, string> callback) { ... }
    //       public MethodCallBuilder Returns(Func<string, FormatOptions, int, string> callback) { ... }
    //   }
    // =========================================================================

    public void Returns_DifferentParamCounts_AutoResolves()
    {
        var stub = new Stubs.IFormatter();

        // Compiler resolves by parameter count - no explicit types needed
        stub.Format.Return((input) => input.ToUpperInvariant());
        stub.Format.Return((input, options) => options.Uppercase ? input.ToUpperInvariant() : input);
        stub.Format.Return((input, options, maxLength) =>
        {
            var result = options.Uppercase ? input.ToUpperInvariant() : input;
            return result[..Math.Min(result.Length, maxLength)];
        });

        IFormatter formatter = stub;

        var r1 = formatter.Format("hello");                                         // "HELLO"
        var r2 = formatter.Format("hello", new FormatOptions(Uppercase: false));    // "hello"
        var r3 = formatter.Format("hello", new FormatOptions(Uppercase: true), 3);  // "HEL"
    }

    // =========================================================================
    // Returns() - NOT Available at Interceptor Level for Overloads
    // =========================================================================
    // DESIGN DECISION: Unlike non-overloaded methods, overloaded methods do
    // NOT have a direct Returns() method on the interceptor.
    //
    // NON-OVERLOADED (ICalculator.Add):
    //   stub.Add.Return(42);  // Available
    //
    // OVERLOADED (IFormatter.Format):
    //   stub.Format.Return("x");  // Does not exist - which overload?
    //
    // WHY NOT: Returns(value) would be ambiguous:
    // - Should it configure ALL overloads to return "x"?
    // - Should it configure just ONE (which one)?
    //
    // WORKAROUND: Use Returns(callback) with explicit overload targeting:
    //   stub.Format.Return((input) => "constant");
    //   stub.Format.Return((input, options) => "constant");
    //
    // DID NOT DO THIS: Add ReturnsForAll(value) to configure all overloads
    //
    // WHY NOT (yet): Adds complexity. Most tests configure specific overloads.
    // Could be added if there's strong demand.
    // =========================================================================

    public void Returns_NotAvailable_UseReturnsCallbackInstead()
    {
        var stub = new Stubs.IFormatter();

        // For a constant return value, use Returns with a constant lambda
        stub.Format.Return((input) => "formatted");
        stub.Format.Return((input, options) => "formatted");
        stub.Format.Return((input, options, maxLength) => "formatted");

        IFormatter formatter = stub;

        // All overloads return "formatted"
        var r1 = formatter.Format("a");
        var r2 = formatter.Format("b", new FormatOptions());
        var r3 = formatter.Format("c", new FormatOptions(), 10);
        // r1, r2, r3 all == "formatted"
    }

    // =========================================================================
    // When() - Parameter Signature Resolves Overload
    // =========================================================================
    // DESIGN DECISION: When() works naturally with overloads because the
    // parameter values themselves determine which overload is being configured.
    //
    // GENERATOR BEHAVIOR: Each overload gets its own When():
    //
    //   public class FormatInterceptor
    //   {
    //       public WhenBuilder When(string input) { ... }
    //       public WhenBuilder When(string input, FormatOptions options) { ... }
    //       public WhenBuilder When(string input, FormatOptions options, int maxLength) { ... }
    //   }
    // =========================================================================

    public void When_ParameterSignatureResolvesOverload()
    {
        var stub = new Stubs.IFormatter();

        // Default behavior via Returns
        stub.Format.Return((input) => "default-1");
        stub.Format.Return((input, options) => "default-2");

        // Each When targets a specific overload based on parameter signature
        stub.Format.When("special").Return("SPECIAL-1");
        stub.Format.When("special", new FormatOptions(Uppercase: true)).Return("SPECIAL-2");

        IFormatter formatter = stub;

        var r1 = formatter.Format("special");                                     // "SPECIAL-1"
        var r2 = formatter.Format("special", new FormatOptions(Uppercase: true)); // "SPECIAL-2"
        var r3 = formatter.Format("other");                                        // "default-1"
        var r4 = formatter.Format("other", new FormatOptions());                   // "default-2"
    }

    // =========================================================================
    // When() with Predicate - Parameter Count Determines Overload
    // =========================================================================

    public void When_PredicateResolvesOverload()
    {
        var stub = new Stubs.IFormatter();

        // Predicate parameter count determines which overload
        stub.Format.When((input) => input.StartsWith("X", StringComparison.Ordinal)).Return("X-PREFIX");
        stub.Format.When((input, options) => options.Uppercase).Return("UPPER-MODE");

        IFormatter formatter = stub;

        var r1 = formatter.Format("X-file");                              // "X-PREFIX" (predicate matched)
        var r2 = formatter.Format("other");                                // default (no match)
        var r3 = formatter.Format("test", new FormatOptions(Uppercase: true));  // "UPPER-MODE"
        var r4 = formatter.Format("test", new FormatOptions(Uppercase: false)); // default (no match)
    }

    // =========================================================================
    // Tracking - Each Overload Has Independent Tracking
    // =========================================================================
    // DESIGN DECISION: Each overload maintains separate tracking state.
    // Returns(callback) returns a builder specific to that overload with its own
    // LastArg/LastArgs and call count.
    //
    // GENERATOR BEHAVIOR:
    //
    //   public class FormatInterceptor
    //   {
    //       // Each Returns returns overload-specific builder
    //       public MethodCallBuilderImpl_String Returns(Func<string, string> cb);
    //       public MethodCallBuilderImpl_String_Options Returns(Func<string, FormatOptions, string> cb);
    //   }
    // =========================================================================

    public void Tracking_EachOverloadTrackedSeparately()
    {
        var stub = new Stubs.IFormatter();

        // Each Returns returns a separate tracking object
        var tracking1 = stub.Format.Return((input) => input);
        var tracking2 = stub.Format.Return((input, options) => input);
        var tracking3 = stub.Format.Return((input, options, maxLength) => input);

        IFormatter formatter = stub;

        formatter.Format("a");
        formatter.Format("b");
        formatter.Format("c", new FormatOptions());
        formatter.Format("d", new FormatOptions(), 5);
        formatter.Format("e", new FormatOptions(), 10);

        // Each tracker only sees calls to its overload
        tracking1.Verify(Called.Exactly(2));  // "a", "b"
        // tracking1.LastArg == "b"

        tracking2.Verify(Called.Once);        // "c"
        // tracking2.LastArgs == ("c", FormatOptions)

        tracking3.Verify(Called.Exactly(2));  // "d", "e"
        // tracking3.LastArgs == ("e", FormatOptions, 10)
    }

    // =========================================================================
    // Verify() on Interceptor - Counts ALL Overloads
    // =========================================================================
    // DESIGN DECISION: stub.Method.Verify() verifies total calls across ALL
    // overloads of that method. Use per-overload tracking for granular control.
    // =========================================================================

    public void Verify_OnInterceptor_CountsAllOverloads()
    {
        var stub = new Stubs.IFormatter();

        stub.Format.Return((input) => input);
        stub.Format.Return((input, options) => input);

        IFormatter formatter = stub;

        formatter.Format("a");
        formatter.Format("b", new FormatOptions());
        formatter.Format("c", new FormatOptions());

        // Verify on interceptor counts all overloads: 1 + 2 = 3
        stub.Format.Verify(Called.Exactly(3));
    }

    // =========================================================================
    // Void Method Overloads - Same Pattern
    // =========================================================================

    public void VoidOverloads_SamePattern()
    {
        var stub = new Stubs.IFormatter();
        var logs = new List<string>();

        // Configure each void overload
        var tracking1 = stub.Log.Call((msg) => logs.Add($"[INFO] {msg}"));
        var tracking2 = stub.Log.Call((msg, level) => logs.Add($"[L{level}] {msg}"));
        var tracking3 = stub.Log.Call((msg, level, cat) => logs.Add($"[{cat}:L{level}] {msg}"));

        IFormatter formatter = stub;

        formatter.Log("hello");
        formatter.Log("warning", 2);
        formatter.Log("error", 3, "SYSTEM");

        // logs.Count == 3
        tracking1.Verify(Called.Once);
        tracking2.Verify(Called.Once);
        tracking3.Verify(Called.Once);
    }

    // =========================================================================
    // Async Method Overloads - Common Pattern
    // =========================================================================
    // DESIGN PATTERN: Many async methods have overloads with/without
    // CancellationToken. KnockOff handles these naturally.
    // =========================================================================

    public async Task AsyncOverloads_WithAndWithoutCancellation()
    {
        var stub = new Stubs.IFormatter();

        // Configure async overload without cancellation
        var tracking1 = stub.TransformAsync.Return((input) => $"[{input}]");

        // Configure async overload with cancellation
        var tracking2 = stub.TransformAsync.Return((input, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            return $"[{input}:ct]";
        });

        IFormatter formatter = stub;

        var r1 = await formatter.TransformAsync("a");
        var r2 = await formatter.TransformAsync("b", CancellationToken.None);

        // r1 == "[a]"
        // r2 == "[b:ct]"

        tracking1.Verify(Called.Once);
        tracking2.Verify(Called.Once);
    }

    // =========================================================================
    // Sequences with Overloads
    // =========================================================================
    // DESIGN DECISION: Sequences (ThenReturns, ThenCall) work per-overload.
    // Each overload has its own sequence that advances independently.
    // =========================================================================

    public void Sequences_PerOverload()
    {
        var stub = new Stubs.IFormatter();

        // Sequence for single-param overload
        stub.Format
            .Return((input) => "first")
            .ThenReturn("second")
            .ThenReturn("third");

        // Sequence for two-param overload (independent)
        stub.Format
            .Return((input, options) => "A")
            .ThenReturn("B");

        IFormatter formatter = stub;

        // Single-param sequence
        var r1 = formatter.Format("x");     // "first"
        var r2 = formatter.Format("x");     // "second"

        // Two-param sequence (independent)
        var r3 = formatter.Format("x", new FormatOptions());  // "A"
        var r4 = formatter.Format("x", new FormatOptions());  // "B"

        // Back to single-param (continues its own sequence)
        var r5 = formatter.Format("x");     // "third"
        var r6 = formatter.Format("x");     // "third" (repeats last)
    }

    // =========================================================================
    // DESIGN DECISION SUMMARY: Overloaded Method API
    // =========================================================================
    //
    // AVAILABLE ON INTERCEPTOR:
    // - Returns(lambda)    - Configures specific overload based on lambda signature
    // - When(params)       - Configures specific overload based on parameter signature
    // - When(predicate)    - Configures specific overload based on predicate arity
    // - Verify()           - Verifies total calls across ALL overloads
    // - Reset()            - Resets tracking for ALL overloads
    //
    // NOT AVAILABLE ON INTERCEPTOR (for overloaded methods):
    // - Returns(value)     - Ambiguous which overload to configure
    // - Returns(v1, v2...) - Ambiguous which overload to configure
    //
    // RETURNED FROM Returns()/Execute():
    // - Overload-specific builder with:
    //   - LastArg / LastArgs  - Last call arguments for THIS overload
    //   - Verify()            - Verify calls to THIS overload
    //   - ThenReturns()       - Add callback to sequence for THIS overload
    //   - ThenReturns(value)  - Add value to sequence for THIS overload
    //   - Verifiable()        - Mark THIS overload for stub.Verify()
    //
    // =========================================================================
}
