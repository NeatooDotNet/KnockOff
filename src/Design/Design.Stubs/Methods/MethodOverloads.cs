// -----------------------------------------------------------------------------
// Design.Stubs - Method Overloads
// -----------------------------------------------------------------------------
// This file demonstrates how KnockOff handles method overloads:
// - Call() with lambda parameter type resolution
// - When() with parameter signature resolution
// - Why Return(value) is not available at interceptor level for overloads
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
// - Overloaded: stub.Method.Return(42) does NOT exist - use Call(callback) instead
//
// WHY: With overloads, Return(value) would be ambiguous - should it
// configure one overload or all? Call(callback) makes the target explicit.
//
// DISAMBIGUATION: With custom delegates, overloads are disambiguated by:
// - 1-param overloads: Explicit parameter type: (string input) => ...
// - 2+ param overloads: Named parameters: (string input, FormatOptions options) => ...
// - The parameter types uniquely identify the overload.
// =============================================================================

[KnockOff<IFormatter>]
public partial class MethodOverloadsDemo
{
    // =========================================================================
    // Call(callback) - Different Parameter Counts Disambiguate Automatically
    // =========================================================================
    // DESIGN DECISION: When overloads have different parameter counts, the
    // compiler resolves the correct Call(callback) overload based on lambda arity.
    //
    // GENERATOR BEHAVIOR: Each overload gets its own Call():
    //
    //   public class FormatInterceptor
    //   {
    //       public MethodCallBuilder Call(Func<string, string> callback) { ... }
    //       public MethodCallBuilder Call(Func<(string input, FormatOptions options), string> callback) { ... }
    //       public MethodCallBuilder Call(Func<(string input, FormatOptions options, int maxLength), string> callback) { ... }
    //   }
    //
    // NOTE: For 1-param overloads in a group, explicit type is needed to
    // disambiguate from the multi-param overloads. For 2+ params,
    // use named parameters: (Type1 name1, Type2 name2) => ...
    // =========================================================================

    public void Returns_DifferentParamCounts_AutoResolves()
    {
        var stub = new Stubs.IFormatter();

        // 1-param: Explicit type needed to distinguish from multi-param overloads
        stub.Format.Call((string input) => input.ToUpperInvariant());
        // 2-param: Named parameters for disambiguation
        stub.Format.Call((string input, FormatOptions options) =>
            options.Uppercase ? input.ToUpperInvariant() : input);
        // 3-param: Named parameters for disambiguation
        stub.Format.Call((string input, FormatOptions options, int maxLength) =>
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
    // Return() - NOT Available at Interceptor Level for Overloads
    // =========================================================================
    // DESIGN DECISION: Unlike non-overloaded methods, overloaded methods do
    // NOT have a direct Return() method on the interceptor.
    //
    // NON-OVERLOADED (ICalculator.Add):
    //   stub.Add.Return(42);  // Available
    //
    // OVERLOADED (IFormatter.Format):
    //   stub.Format.Return("x");  // Does not exist - which overload?
    //
    // WHY NOT: Return(value) would be ambiguous:
    // - Should it configure ALL overloads to return "x"?
    // - Should it configure just ONE (which one)?
    //
    // WORKAROUND: Use Call(callback) with explicit overload targeting:
    //   stub.Format.Call((string input) => "constant");
    //   stub.Format.Call((string input, FormatOptions options) => "constant");
    //
    // DID NOT DO THIS: Add ReturnForAll(value) to configure all overloads
    //
    // WHY NOT (yet): Adds complexity. Most tests configure specific overloads.
    // Could be added if there's strong demand.
    // =========================================================================

    public void Returns_NotAvailable_UseReturnsCallbackInstead()
    {
        var stub = new Stubs.IFormatter();

        // For a constant return value, use Call with a constant lambda
        stub.Format.Call((string input) => "formatted");
        stub.Format.Call((string input, FormatOptions options) => "formatted");
        stub.Format.Call((string input, FormatOptions options, int maxLength) => "formatted");

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
    // GENERATOR BEHAVIOR: The compositor generates When() per overload:
    //
    //   public class IFormatter_FormatInterceptor
    //   {
    //       public WhenBuilder When(string input) { ... }  // 1-param overload
    //       public WhenBuilder When(string input, FormatOptions options) { ... }  // 2-param overload
    //       public WhenBuilder When((string input, FormatOptions options, int maxLength) args) { ... }
    //   }
    // =========================================================================

    public void When_ParameterSignatureResolvesOverload()
    {
        var stub = new Stubs.IFormatter();

        // Default behavior via Call
        stub.Format.Call((string input) => "default-1");
        stub.Format.Call((string input, FormatOptions options) => "default-2");

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

        // 1-param predicate: explicit type needed for disambiguation
        stub.Format.When((string input) => input.StartsWith('X')).Return("X-PREFIX");
        // 2-param predicate: named parameters for disambiguation
        stub.Format.When((string input, FormatOptions options) => options.Uppercase).Return("UPPER-MODE");

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
    // Call(callback) returns a builder specific to that overload with its own
    // LastArg/LastArgs and call count.
    //
    // GENERATOR BEHAVIOR:
    //
    //   public class FormatInterceptor
    //   {
    //       // Each Call returns overload-specific builder
    //       public MethodCallBuilderImpl_String Call(Func<string, string> cb);
    //       public MethodCallBuilderImpl_String_Options Call(Func<(string input, FormatOptions options), string> cb);
    //   }
    // =========================================================================

    public void Tracking_EachOverloadTrackedSeparately()
    {
        var stub = new Stubs.IFormatter();

        // Each Call returns a separate tracking object
        var tracking1 = stub.Format.Call((string input) => input);
        var tracking2 = stub.Format.Call((string input, FormatOptions options) => input);
        var tracking3 = stub.Format.Call((string input, FormatOptions options, int maxLength) => input);

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

        stub.Format.Call((string input) => input);
        stub.Format.Call((string input, FormatOptions options) => input);

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
        var tracking1 = stub.Log.Call((string msg) => logs.Add($"[INFO] {msg}"));
        var tracking2 = stub.Log.Call((string message, int level) => logs.Add($"[L{level}] {message}"));
        var tracking3 = stub.Log.Call((string message, int level, string category) => logs.Add($"[{category}:L{level}] {message}"));

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

        // Configure async overload without cancellation (explicit type for disambiguation)
        var tracking1 = stub.TransformAsync.Call((string input) => $"[{input}]");

        // Configure async overload with cancellation (named parameters for 2-param overload)
        var tracking2 = stub.TransformAsync.Call((string input, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
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
    // DESIGN DECISION: Sequences (ThenReturn, ThenCall) work per-overload.
    // Each overload has its own sequence that advances independently.
    // =========================================================================

    public void Sequences_PerOverload()
    {
        var stub = new Stubs.IFormatter();

        // Sequence for single-param overload
        stub.Format
            .Call((string input) => "first")
            .ThenReturn("second")
            .ThenReturn("third");

        // Sequence for two-param overload (independent)
        stub.Format
            .Call((string input, FormatOptions options) => "A")
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
    // DISAMBIGUATION:
    // - 1-param overloads: Use explicit type — (string input) => ...
    // - 2+ param overloads: Use named parameters — (T1 n1, T2 n2) => n1 ...
    // - The parameter types uniquely identify which overload is targeted
    //
    // AVAILABLE ON INTERCEPTOR:
    // - Call(lambda)       - Configures specific overload based on lambda signature
    // - When(params)       - Configures specific overload based on parameter signature
    // - When(predicate)    - Configures specific overload based on predicate type
    // - Verify()           - Verifies total calls across ALL overloads
    // - Reset()            - Resets tracking for ALL overloads
    //
    // NOT AVAILABLE ON INTERCEPTOR (for overloaded methods):
    // - Return(value)      - Ambiguous which overload to configure
    // - Return(v1, v2...)  - Ambiguous which overload to configure
    //
    // RETURNED FROM Call():
    // - Overload-specific builder with:
    //   - LastArg / LastArgs  - Last call arguments for THIS overload
    //   - Verify()            - Verify calls to THIS overload
    //   - ThenReturn()        - Add callback to sequence for THIS overload
    //   - ThenReturn(value)   - Add value to sequence for THIS overload
    //   - Verifiable()        - Mark THIS overload for stub.Verify()
    //
    // =========================================================================
}
