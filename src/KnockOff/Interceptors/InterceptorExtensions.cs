using System.Collections.Generic;

namespace KnockOff.Interceptors;

/// <summary>
/// Extension methods for IInterceptor collections, enabling compositor-level
/// verification and reset without generated forwarding methods.
/// </summary>
public static class InterceptorExtensions
{
    /// <summary>
    /// Checks verification for all interceptors (VerifyAll semantics).
    /// Throws VerificationException on the first failure found.
    /// </summary>
    public static void VerifyAll(this IReadOnlyList<IInterceptor> interceptors)
    {
        ArgumentNullException.ThrowIfNull(interceptors);
        foreach (var i in interceptors)
        {
            var failure = i.CheckVerificationAll();
            if (failure != null)
                throw new VerificationException(failure);
        }
    }

    /// <summary>
    /// Resets tracking state on all interceptors.
    /// </summary>
    public static void ResetAll(this IReadOnlyList<IInterceptor> interceptors)
    {
        ArgumentNullException.ThrowIfNull(interceptors);
        foreach (var i in interceptors)
            i.Reset();
    }
}
