namespace KnockOff;

/// <summary>
/// Tracks invocations of an indexer getter callback registration with key capture.
/// Returned by OnGet() to enable verification, key inspection, and fluent chaining.
/// </summary>
public interface IIndexerGetTracking<TKey>
{
    /// <summary>Last key passed to this getter callback. Default if never called.</summary>
    TKey LastKey { get; }

    /// <summary>Clears tracking state for this registration (LastKey = default, call count = 0).</summary>
    void Reset();

    /// <summary>
    /// Verifies the getter callback was invoked at least once.
    /// Throws VerificationException if not satisfied.
    /// </summary>
    void Verify();

    /// <summary>
    /// Verifies the getter callback was invoked according to the Times constraint.
    /// Throws VerificationException if not satisfied.
    /// </summary>
    /// <param name="times">The Times constraint to verify against.</param>
    void Verify(Times times);

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify().
    /// Uses Times.AtLeastOnce as the constraint.
    /// Returns this for fluent chaining.
    /// </summary>
    IIndexerGetTracking<TKey> Verifiable();

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify()
    /// with a specific Times constraint. Returns this for fluent chaining.
    /// </summary>
    /// <param name="times">The Times constraint to verify against.</param>
    IIndexerGetTracking<TKey> Verifiable(Times times);
}

/// <summary>
/// Tracks invocations of an indexer setter callback registration with key and value capture.
/// Returned by OnSet() to enable verification, entry inspection, and fluent chaining.
/// </summary>
public interface IIndexerSetTracking<TKey, TValue>
{
    /// <summary>Last key and value passed to this setter callback. Null if never called.</summary>
    (TKey Key, TValue Value)? LastEntry { get; }

    /// <summary>Clears tracking state for this registration (LastEntry = null, call count = 0).</summary>
    void Reset();

    /// <summary>
    /// Verifies the setter callback was invoked at least once.
    /// Throws VerificationException if not satisfied.
    /// </summary>
    void Verify();

    /// <summary>
    /// Verifies the setter callback was invoked according to the Times constraint.
    /// Throws VerificationException if not satisfied.
    /// </summary>
    /// <param name="times">The Times constraint to verify against.</param>
    void Verify(Times times);

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify().
    /// Uses Times.AtLeastOnce as the constraint.
    /// Returns this for fluent chaining.
    /// </summary>
    IIndexerSetTracking<TKey, TValue> Verifiable();

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify()
    /// with a specific Times constraint. Returns this for fluent chaining.
    /// </summary>
    /// <param name="times">The Times constraint to verify against.</param>
    IIndexerSetTracking<TKey, TValue> Verifiable(Times times);
}
