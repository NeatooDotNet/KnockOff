namespace KnockOff;

/// <summary>
/// Returned by Return(callback). Supports tracking and optional sequence chaining via ThenReturn.
/// </summary>
/// <remarks>
/// The builder IS the tracking implementation. When ThenReturn is invoked,
/// the builder lazily elevates from repeating to sequence mode.
/// </remarks>
public interface IMethodReturnBuilder<TCallback> : IMethodTracking
{
    /// <summary>
    /// Elevates to sequence mode and adds another callback.
    /// The current callback becomes the first element; the new callback is appended.
    /// </summary>
    IMethodReturnSequence<TCallback> ThenReturn(TCallback callback);

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify().
    /// Uses Called.AtLeastOnce as the constraint.
    /// Returns this for fluent chaining.
    /// </summary>
    new IMethodReturnBuilder<TCallback> Verifiable();

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify()
    /// with a specific Called constraint. Returns this for fluent chaining.
    /// </summary>
    /// <param name="called">The Called constraint to verify against.</param>
    new IMethodReturnBuilder<TCallback> Verifiable(Called called);
}

/// <summary>
/// Returned by Return(callback) for methods with a single trackable parameter.
/// Supports tracking with LastArg access and optional sequence chaining via ThenReturn.
/// </summary>
public interface IMethodReturnBuilder<TCallback, TArg> : IMethodTracking<TArg>
{
    /// <summary>
    /// Elevates to sequence mode and adds another callback.
    /// The current callback becomes the first element; the new callback is appended.
    /// </summary>
    IMethodReturnSequence<TCallback> ThenReturn(TCallback callback);

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify().
    /// Uses Called.AtLeastOnce as the constraint.
    /// Returns this for fluent chaining with LastArg access.
    /// </summary>
    new IMethodReturnBuilder<TCallback, TArg> Verifiable();

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify()
    /// with a specific Called constraint. Returns this for fluent chaining.
    /// </summary>
    /// <param name="called">The Called constraint to verify against.</param>
    new IMethodReturnBuilder<TCallback, TArg> Verifiable(Called called);
}

/// <summary>
/// Returned by Return(callback) for methods with multiple trackable parameters.
/// Supports tracking with LastArgs access and optional sequence chaining via ThenReturn.
/// </summary>
public interface IMethodReturnBuilderArgs<TCallback, TArgs> : IMethodTrackingArgs<TArgs>
{
    /// <summary>
    /// Elevates to sequence mode and adds another callback.
    /// The current callback becomes the first element; the new callback is appended.
    /// </summary>
    IMethodReturnSequence<TCallback> ThenReturn(TCallback callback);

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify().
    /// Uses Called.AtLeastOnce as the constraint.
    /// Returns this for fluent chaining with LastArgs access.
    /// </summary>
    new IMethodReturnBuilderArgs<TCallback, TArgs> Verifiable();

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify()
    /// with a specific Called constraint. Returns this for fluent chaining.
    /// </summary>
    /// <param name="called">The Called constraint to verify against.</param>
    new IMethodReturnBuilderArgs<TCallback, TArgs> Verifiable(Called called);
}
