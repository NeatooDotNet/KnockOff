// -----------------------------------------------------------------------------
// Design.Domain - Interface for demonstrating void stub override fallback bug
// -----------------------------------------------------------------------------
// This interface exercises the case where void (and non-void) methods have
// custom-type parameters. The stub override override detection uses syntactic
// type names (e.g., "Order") while the semantic model uses fully-qualified
// names (e.g., "Design.Domain.Services.Order"), causing a signature key
// mismatch for non-primitive parameter types.
//
// BUG: Stub override overrides are not recognized when parameters use custom
// types because the signature key built from syntax ("Method_(Order)")
// doesn't match the key built from the semantic model
// ("Method_(Design.Domain.Services.Order)").
//
// This affects ALL stub overrides with custom-type parameters, not just void
// ones. The void case is more visible because unconfigured void calls
// silently do nothing, while unconfigured non-void calls return default.
// -----------------------------------------------------------------------------

namespace Design.Domain.Services;

/// <summary>
/// Simple entity used as a parameter type to trigger the signature
/// key mismatch in stub override override detection.
/// </summary>
public class Order
{
    public int Id { get; set; }
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
}

/// <summary>
/// Interface with void and non-void methods that take custom-type parameters.
/// Used to demonstrate the void stub override fallback bug: when a standalone
/// stub defines stub overrides for members with custom-type parameters, the
/// generated interceptor does not call the stub override as a fallback.
/// </summary>
public interface IVoidStubOverrideService
{
    /// <summary>
    /// Void method with custom-type parameter.
    /// BUG: Stub override override (SaveOrder_) is not called as fallback.
    /// </summary>
    void SaveOrder(Order order);

    /// <summary>
    /// Non-void method with custom-type parameter.
    /// BUG: Stub override override (FormatOrder_) is not called as fallback.
    /// The return value is default! instead of the stub override result.
    /// </summary>
    string FormatOrder(Order order);

    /// <summary>
    /// Void method with primitive parameter (control case).
    /// WORKS: Stub override override (LogMessage_) is correctly called.
    /// </summary>
    void LogMessage(string message);

    /// <summary>
    /// Non-void method with primitive parameter (control case).
    /// WORKS: Stub override override (GetStatus_) is correctly called.
    /// </summary>
    string GetStatus(int code);
}
