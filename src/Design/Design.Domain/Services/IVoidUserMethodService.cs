// -----------------------------------------------------------------------------
// Design.Domain - Interface for demonstrating void user method fallback bug
// -----------------------------------------------------------------------------
// This interface exercises the case where void (and non-void) methods have
// custom-type parameters. The user method override detection uses syntactic
// type names (e.g., "Order") while the semantic model uses fully-qualified
// names (e.g., "Design.Domain.Services.Order"), causing a signature key
// mismatch for non-primitive parameter types.
//
// BUG: User method overrides are not recognized when parameters use custom
// types because the signature key built from syntax ("Method_(Order)")
// doesn't match the key built from the semantic model
// ("Method_(Design.Domain.Services.Order)").
//
// This affects ALL user methods with custom-type parameters, not just void
// ones. The void case is more visible because unconfigured void calls
// silently do nothing, while unconfigured non-void calls return default.
// -----------------------------------------------------------------------------

namespace Design.Domain.Services;

/// <summary>
/// Simple entity used as a parameter type to trigger the signature
/// key mismatch in user method override detection.
/// </summary>
public class Order
{
    public int Id { get; set; }
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
}

/// <summary>
/// Interface with void and non-void methods that take custom-type parameters.
/// Used to demonstrate the void user method fallback bug: when a standalone
/// stub defines user methods for members with custom-type parameters, the
/// generated interceptor does not call the user method as a fallback.
/// </summary>
public interface IVoidUserMethodService
{
    /// <summary>
    /// Void method with custom-type parameter.
    /// BUG: User method override (SaveOrder_) is not called as fallback.
    /// </summary>
    void SaveOrder(Order order);

    /// <summary>
    /// Non-void method with custom-type parameter.
    /// BUG: User method override (FormatOrder_) is not called as fallback.
    /// The return value is default! instead of the user method result.
    /// </summary>
    string FormatOrder(Order order);

    /// <summary>
    /// Void method with primitive parameter (control case).
    /// WORKS: User method override (LogMessage_) is correctly called.
    /// </summary>
    void LogMessage(string message);

    /// <summary>
    /// Non-void method with primitive parameter (control case).
    /// WORKS: User method override (GetStatus_) is correctly called.
    /// </summary>
    string GetStatus(int code);
}
