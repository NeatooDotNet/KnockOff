// -----------------------------------------------------------------------------
// Design.Stubs - Void User Method Fallback Bug
// -----------------------------------------------------------------------------
// This file demonstrates the user method override detection bug for methods
// with custom-type parameters.
//
// ROOT CAUSE:
// The generator detects user overrides via syntactic analysis (looks for
// 'override' keyword + '_' suffix in source code) and builds a signature key
// using the type name as written (e.g., "SaveOrder_(Order)").
//
// Later, the builder checks for user overrides using the semantic model's
// fully-qualified type name (e.g., "SaveOrder_(Design.Domain.Services.Order)").
//
// These keys don't match for custom types, so HasUserOverride is false, and
// the generated interceptor uses the standard fallback path (Source > Strict >
// default) instead of calling the user method.
//
// Primitive types (string, int, etc.) are not affected because both the
// syntactic and semantic normalization produce the same keyword (e.g.,
// "string", "int").
//
// EXPECTED BEHAVIOR:
// - SaveOrder_ should be called when no Execute is configured
// - FormatOrder_ should be called when no Returns is configured
// - LogMessage_ and GetStatus_ should work (primitive types -- control case)
//
// ACTUAL BEHAVIOR:
// - SaveOrder_ is NOT called (void silently does nothing)
// - FormatOrder_ is NOT called (returns default!)
// - LogMessage_ and GetStatus_ work correctly (primitives match)
// -----------------------------------------------------------------------------

using Design.Domain.Services;
using KnockOff;

namespace Design.Stubs.UserMethods;

// =============================================================================
// STANDALONE STUB WITH CUSTOM-TYPE USER METHOD OVERRIDES
// =============================================================================
// This stub defines user method overrides for all four methods.
// The overrides for methods with custom-type parameters (Order) are NOT
// recognized by the generator due to the signature key mismatch bug.
// =============================================================================

[KnockOff]
public partial class VoidUserMethodFallbackStub : IVoidUserMethodService { }

#pragma warning disable CA1062 // Validate arguments of public methods
public partial class VoidUserMethodFallbackStub
{
    // Track whether void user methods were called
    public bool SaveOrderCalled { get; private set; }
    public Order? LastSavedOrder { get; private set; }
    private readonly List<string> _loggedMessages = new();
    public IReadOnlyList<string> LoggedMessages => _loggedMessages;

    // =========================================================================
    // BUG CASE: Void method with custom-type parameter
    // =========================================================================
    // EXPECTED: When no Execute is configured, calling SaveOrder should
    //           invoke this user method as fallback.
    // ACTUAL:   This method is never called because the generator does not
    //           recognize it as a user override (signature key mismatch).
    // =========================================================================
    protected override void SaveOrder_(Order order)
    {
        SaveOrderCalled = true;
        LastSavedOrder = order;
    }

    // =========================================================================
    // BUG CASE: Non-void method with custom-type parameter
    // =========================================================================
    // EXPECTED: When no Returns is configured, calling FormatOrder
    //           should invoke this user method and return its result.
    // ACTUAL:   This method is never called; FormatOrder returns default (null).
    // =========================================================================
    protected override string FormatOrder_(Order order)
    {
        return $"Order #{order.Id}: {order.Description} (${order.Amount})";
    }

    // =========================================================================
    // CONTROL CASE: Void method with primitive parameter (string)
    // =========================================================================
    // WORKS: This user method IS correctly called because "string" normalizes
    //        the same way in both syntactic and semantic analysis.
    // =========================================================================
    protected override void LogMessage_(string message)
    {
        _loggedMessages.Add(message);
    }

    // =========================================================================
    // CONTROL CASE: Non-void method with primitive parameter (int)
    // =========================================================================
    // WORKS: This user method IS correctly called because "int" normalizes
    //        the same way in both syntactic and semantic analysis.
    // =========================================================================
    protected override string GetStatus_(int code)
    {
        return code switch
        {
            200 => "OK",
            404 => "Not Found",
            500 => "Internal Error",
            _ => $"Status {code}"
        };
    }
}
#pragma warning restore CA1062
