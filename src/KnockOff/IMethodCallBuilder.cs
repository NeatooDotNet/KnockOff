namespace KnockOff;

/// <summary>
/// Returned by Call(callback) on void methods. Supports tracking and optional sequence chaining via ThenCall.
/// </summary>
/// <remarks>
/// The builder IS the tracking implementation. When ThenCall is invoked,
/// the builder lazily elevates from repeating to sequence mode.
/// </remarks>
public interface IMethodCallBuilder<TCallback> : IMethodTracking
{
    /// <summary>
    /// Elevates to sequence mode and adds another callback.
    /// The current callback becomes the first element; the new callback is appended.
    /// </summary>
    IMethodCallSequence<TCallback> ThenCall(TCallback callback);

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify().
    /// Uses Called.AtLeastOnce as the constraint.
    /// Returns this for fluent chaining.
    /// </summary>
    new IMethodCallBuilder<TCallback> Verifiable();

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify()
    /// with a specific Called constraint. Returns this for fluent chaining.
    /// </summary>
    /// <param name="called">The Called constraint to verify against.</param>
    new IMethodCallBuilder<TCallback> Verifiable(Called called);
}

/// <summary>
/// Returned by Call(callback) for void methods with a single trackable parameter.
/// Supports tracking with LastArg access and optional sequence chaining via ThenCall.
/// </summary>
public interface IMethodCallBuilder<TCallback, TArg> : IMethodTracking<TArg>
{
    /// <summary>
    /// Elevates to sequence mode and adds another callback.
    /// The current callback becomes the first element; the new callback is appended.
    /// </summary>
    IMethodCallSequence<TCallback> ThenCall(TCallback callback);

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify().
    /// Uses Called.AtLeastOnce as the constraint.
    /// Returns this for fluent chaining with LastArg access.
    /// </summary>
    new IMethodCallBuilder<TCallback, TArg> Verifiable();

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify()
    /// with a specific Called constraint. Returns this for fluent chaining.
    /// </summary>
    /// <param name="called">The Called constraint to verify against.</param>
    new IMethodCallBuilder<TCallback, TArg> Verifiable(Called called);
}

/// <summary>
/// Returned by Call(callback) for void methods with multiple trackable parameters.
/// Supports tracking with LastArgs access and optional sequence chaining via ThenCall.
/// </summary>
public interface IMethodCallBuilderArgs<TCallback, TArgs> : IMethodTrackingArgs<TArgs>
{
    /// <summary>
    /// Elevates to sequence mode and adds another callback.
    /// The current callback becomes the first element; the new callback is appended.
    /// </summary>
    IMethodCallSequence<TCallback> ThenCall(TCallback callback);

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify().
    /// Uses Called.AtLeastOnce as the constraint.
    /// Returns this for fluent chaining with LastArgs access.
    /// </summary>
    new IMethodCallBuilderArgs<TCallback, TArgs> Verifiable();

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify()
    /// with a specific Called constraint. Returns this for fluent chaining.
    /// </summary>
    /// <param name="called">The Called constraint to verify against.</param>
    new IMethodCallBuilderArgs<TCallback, TArgs> Verifiable(Called called);
}
