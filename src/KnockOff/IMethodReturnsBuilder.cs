namespace KnockOff;

/// <summary>
/// Returned by Returns(callback). Supports tracking and optional sequence chaining via ThenReturns.
/// </summary>
/// <remarks>
/// The builder IS the tracking implementation. When ThenReturns is invoked,
/// the builder lazily elevates from repeating to sequence mode.
/// </remarks>
public interface IMethodReturnsBuilder<TCallback> : IMethodTracking
{
    /// <summary>
    /// Elevates to sequence mode and adds another callback.
    /// The current callback becomes the first element; the new callback is appended.
    /// </summary>
    IMethodReturnsSequence<TCallback> ThenReturns(TCallback callback);

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify().
    /// Uses Times.AtLeastOnce as the constraint.
    /// Returns this for fluent chaining.
    /// </summary>
    new IMethodReturnsBuilder<TCallback> Verifiable();

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify()
    /// with a specific Times constraint. Returns this for fluent chaining.
    /// </summary>
    /// <param name="times">The Times constraint to verify against.</param>
    new IMethodReturnsBuilder<TCallback> Verifiable(Times times);
}

/// <summary>
/// Returned by Returns(callback) for methods with a single trackable parameter.
/// Supports tracking with LastArg access and optional sequence chaining via ThenReturns.
/// </summary>
public interface IMethodReturnsBuilder<TCallback, TArg> : IMethodTracking<TArg>
{
    /// <summary>
    /// Elevates to sequence mode and adds another callback.
    /// The current callback becomes the first element; the new callback is appended.
    /// </summary>
    IMethodReturnsSequence<TCallback> ThenReturns(TCallback callback);

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify().
    /// Uses Times.AtLeastOnce as the constraint.
    /// Returns this for fluent chaining with LastArg access.
    /// </summary>
    new IMethodReturnsBuilder<TCallback, TArg> Verifiable();

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify()
    /// with a specific Times constraint. Returns this for fluent chaining.
    /// </summary>
    /// <param name="times">The Times constraint to verify against.</param>
    new IMethodReturnsBuilder<TCallback, TArg> Verifiable(Times times);
}

/// <summary>
/// Returned by Returns(callback) for methods with multiple trackable parameters.
/// Supports tracking with LastArgs access and optional sequence chaining via ThenReturns.
/// </summary>
public interface IMethodReturnsBuilderArgs<TCallback, TArgs> : IMethodTrackingArgs<TArgs>
{
    /// <summary>
    /// Elevates to sequence mode and adds another callback.
    /// The current callback becomes the first element; the new callback is appended.
    /// </summary>
    IMethodReturnsSequence<TCallback> ThenReturns(TCallback callback);

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify().
    /// Uses Times.AtLeastOnce as the constraint.
    /// Returns this for fluent chaining with LastArgs access.
    /// </summary>
    new IMethodReturnsBuilderArgs<TCallback, TArgs> Verifiable();

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify()
    /// with a specific Times constraint. Returns this for fluent chaining.
    /// </summary>
    /// <param name="times">The Times constraint to verify against.</param>
    new IMethodReturnsBuilderArgs<TCallback, TArgs> Verifiable(Times times);
}
