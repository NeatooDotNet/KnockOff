namespace KnockOff;

/// <summary>
/// Returned by Get() on indexers. Supports tracking and optional sequence chaining.
/// </summary>
/// <remarks>
/// The builder IS the tracking implementation. When ThenGet is invoked,
/// the builder lazily elevates from repeating to sequence mode.
/// </remarks>
public interface IIndexerGetBuilder<TKey, TValue> : IIndexerGetTracking<TKey>
{
    /// <summary>
    /// Elevates to sequence mode and adds another getter callback.
    /// The current callback becomes the first element; the new callback is appended.
    /// </summary>
    IIndexerGetSequence<TKey, TValue> ThenGet(Func<TKey, TValue> callback);

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify().
    /// Uses Called.AtLeastOnce as the constraint.
    /// Returns this for fluent chaining.
    /// </summary>
    new IIndexerGetBuilder<TKey, TValue> Verifiable();
}

/// <summary>
/// Returned by Set() on indexers. Supports tracking and optional sequence chaining.
/// </summary>
/// <remarks>
/// The builder IS the tracking implementation. When ThenSet is invoked,
/// the builder lazily elevates from repeating to sequence mode.
/// </remarks>
public interface IIndexerSetBuilder<TKey, TValue> : IIndexerSetTracking<TKey, TValue>
{
    /// <summary>
    /// Elevates to sequence mode and adds another setter callback.
    /// The current callback becomes the first element; the new callback is appended.
    /// </summary>
    IIndexerSetSequence<TKey, TValue> ThenSet(Action<TKey, TValue> callback);

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify().
    /// Uses Called.AtLeastOnce as the constraint.
    /// Returns this for fluent chaining.
    /// </summary>
    new IIndexerSetBuilder<TKey, TValue> Verifiable();
}
