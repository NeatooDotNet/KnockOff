namespace KnockOff;

/// <summary>
/// Represents a sequence of method callbacks.
/// Returned by OnCallSequence() to enable ThenCall chaining.
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
}

/// <summary>
/// Typed sequence that enables ThenCall chaining.
/// </summary>
public interface IMethodSequence<TCallback> : IMethodSequence
{
    /// <summary>
    /// Marks this sequence for verification by Stub.Verify().
    /// Returns this for fluent chaining.
    /// </summary>
    new IMethodSequence<TCallback> Verifiable();

    /// <summary>
    /// Adds another callback to the sequence.
    /// Each callback in the sequence is invoked exactly once.
    /// </summary>
    IMethodSequence<TCallback> ThenCall(TCallback callback);
}
