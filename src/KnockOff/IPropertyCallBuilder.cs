namespace KnockOff;

/// <summary>
/// Returned by Get(). Supports tracking and optional sequence chaining.
/// </summary>
/// <remarks>
/// The builder IS the tracking implementation. When ThenGet is invoked,
/// the builder lazily elevates from repeating to sequence mode.
/// </remarks>
public interface IPropertyGetBuilder<TValue> : IPropertyGetTracking
{
    /// <summary>
    /// Elevates to sequence mode and adds another getter callback.
    /// The current callback becomes the first element; the new callback is appended.
    /// </summary>
    IPropertyGetSequence<TValue> ThenGet(Func<TValue> callback);

    /// <summary>
    /// Elevates to sequence mode and adds another value to return.
    /// The current callback becomes the first element; the new value is appended.
    /// </summary>
    IPropertyGetSequence<TValue> ThenGet(TValue value);

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify().
    /// Uses Called.AtLeastOnce as the constraint.
    /// Returns this for fluent chaining.
    /// </summary>
    new IPropertyGetBuilder<TValue> Verifiable();
}

/// <summary>
/// Returned by Set(). Supports tracking and optional sequence chaining.
/// </summary>
/// <remarks>
/// The builder IS the tracking implementation. When ThenSet is invoked,
/// the builder lazily elevates from repeating to sequence mode.
/// </remarks>
public interface IPropertySetBuilder<TValue> : IPropertySetTracking<TValue>
{
    /// <summary>
    /// Elevates to sequence mode and adds another setter callback.
    /// The current callback becomes the first element; the new callback is appended.
    /// </summary>
    IPropertySetSequence<TValue> ThenSet(Action<TValue> callback);

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify().
    /// Uses Called.AtLeastOnce as the constraint.
    /// Returns this for fluent chaining.
    /// </summary>
    new IPropertySetBuilder<TValue> Verifiable();
}
