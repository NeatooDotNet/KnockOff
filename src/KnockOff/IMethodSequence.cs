namespace KnockOff;

/// <summary>
/// Represents a sequence of method callbacks.
/// Each callback in the sequence is invoked exactly once.
/// </summary>
public interface IMethodSequence
{
    /// <summary>
    /// Verifies the entire sequence was executed (all callbacks invoked).
    /// Throws VerificationException if sequence incomplete.
    /// </summary>
    void Verify();

    /// <summary>Reset all tracking in the sequence.</summary>
    void Reset();

    /// <summary>
    /// Marks this sequence for verification by Stub.Verify().
    /// The sequence must complete (all callbacks invoked) to pass.
    /// Returns this for fluent chaining.
    /// </summary>
    IMethodSequence Verifiable();

    /// <summary>
    /// Terminates sequence with default(T) after exhaustion instead of repeating last value.
    /// </summary>
    void ThenDefault();
}
