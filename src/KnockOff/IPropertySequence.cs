namespace KnockOff;

/// <summary>
/// Represents a sequence of property getter callbacks.
/// Returned by OnGetSequence() to enable ThenGet chaining.
/// Each callback in the sequence is invoked exactly once.
/// </summary>
public interface IPropertyGetSequence<TValue>
{
    /// <summary>
    /// Adds another getter callback to the sequence.
    /// Each callback in the sequence is invoked exactly once.
    /// </summary>
    IPropertyGetSequence<TValue> ThenGet(Func<TValue> callback);

    /// <summary>
    /// Adds another value to return in the sequence.
    /// Each value in the sequence is returned exactly once.
    /// </summary>
    IPropertyGetSequence<TValue> ThenGet(TValue value);

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
    IPropertyGetSequence<TValue> Verifiable();
}

/// <summary>
/// Represents a sequence of property setter callbacks.
/// Returned by OnSetSequence() to enable ThenSet chaining.
/// Each callback in the sequence is invoked exactly once.
/// </summary>
public interface IPropertySetSequence<TValue>
{
    /// <summary>
    /// Adds another setter callback to the sequence.
    /// Each callback in the sequence is invoked exactly once.
    /// </summary>
    IPropertySetSequence<TValue> ThenSet(Action<TValue> callback);

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
    IPropertySetSequence<TValue> Verifiable();
}
