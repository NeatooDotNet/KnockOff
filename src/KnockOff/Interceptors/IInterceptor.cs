namespace KnockOff.Interceptors;

/// <summary>
/// Common interface for all interceptor types, enabling collection-based
/// verification and reset operations on compositors.
/// </summary>
public interface IInterceptor
{
    /// <summary>Checks verification for Stub.Verify() - only checks if marked verifiable.</summary>
    VerificationFailure? CheckVerification();

    /// <summary>Checks verification for Stub.VerifyAll() - checks if configured.</summary>
    VerificationFailure? CheckVerificationAll();

    /// <summary>Resets tracking state but preserves configuration and verifiable marking.</summary>
    void Reset();
}
