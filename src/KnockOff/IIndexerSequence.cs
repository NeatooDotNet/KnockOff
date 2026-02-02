namespace KnockOff;

/// <summary>
/// Represents a sequence of indexer getter callbacks.
/// Returned by OnGetSequence() to enable ThenGet chaining.
/// Each callback in the sequence is invoked exactly once.
/// </summary>
public interface IIndexerGetSequence<TKey, TValue>
{
    /// <summary>
    /// Adds another getter callback to the sequence.
    /// Each callback in the sequence is invoked exactly once.
    /// </summary>
    IIndexerGetSequence<TKey, TValue> ThenGet(Func<TKey, TValue> callback);

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
    IIndexerGetSequence<TKey, TValue> Verifiable();

    /// <summary>
    /// Terminates sequence with default(T) after exhaustion instead of repeating last value.
    /// </summary>
    void ThenDefault();
}

/// <summary>
/// Represents a sequence of indexer setter callbacks.
/// Returned by OnSetSequence() to enable ThenSet chaining.
/// Each callback in the sequence is invoked exactly once.
/// </summary>
public interface IIndexerSetSequence<TKey, TValue>
{
    /// <summary>
    /// Adds another setter callback to the sequence.
    /// Each callback in the sequence is invoked exactly once.
    /// </summary>
    IIndexerSetSequence<TKey, TValue> ThenSet(Action<TKey, TValue> callback);

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
    IIndexerSetSequence<TKey, TValue> Verifiable();

    /// <summary>
    /// Terminates sequence with default(T) after exhaustion instead of repeating last value.
    /// </summary>
    void ThenDefault();
}
