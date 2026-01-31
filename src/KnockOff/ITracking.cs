namespace KnockOff;

/// <summary>
/// Base interface for tracking invocations.
/// Shared by <see cref="IMethodTracking"/> and <see cref="IWhenTracking"/>.
/// </summary>
public interface ITracking
{
    /// <summary>
    /// Verifies the tracking constraint was satisfied.
    /// Throws <see cref="VerificationException"/> if not satisfied.
    /// </summary>
    void Verify();

    /// <summary>
    /// Resets tracking state (call counts, position, etc.).
    /// </summary>
    void Reset();
}
