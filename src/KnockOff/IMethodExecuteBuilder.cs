namespace KnockOff;

/// <summary>
/// Returned by Execute(callback) on void methods. Supports tracking and optional sequence chaining via ThenExecute.
/// </summary>
/// <remarks>
/// The builder IS the tracking implementation. When ThenExecute is invoked,
/// the builder lazily elevates from repeating to sequence mode.
/// </remarks>
public interface IMethodExecuteBuilder<TCallback> : IMethodTracking
{
    /// <summary>
    /// Elevates to sequence mode and adds another callback.
    /// The current callback becomes the first element; the new callback is appended.
    /// </summary>
    IMethodExecuteSequence<TCallback> ThenExecute(TCallback callback);

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify().
    /// Uses Times.AtLeastOnce as the constraint.
    /// Returns this for fluent chaining.
    /// </summary>
    new IMethodExecuteBuilder<TCallback> Verifiable();

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify()
    /// with a specific Times constraint. Returns this for fluent chaining.
    /// </summary>
    /// <param name="times">The Times constraint to verify against.</param>
    new IMethodExecuteBuilder<TCallback> Verifiable(Times times);
}

/// <summary>
/// Returned by Execute(callback) for void methods with a single trackable parameter.
/// Supports tracking with LastArg access and optional sequence chaining via ThenExecute.
/// </summary>
public interface IMethodExecuteBuilder<TCallback, TArg> : IMethodTracking<TArg>
{
    /// <summary>
    /// Elevates to sequence mode and adds another callback.
    /// The current callback becomes the first element; the new callback is appended.
    /// </summary>
    IMethodExecuteSequence<TCallback> ThenExecute(TCallback callback);

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify().
    /// Uses Times.AtLeastOnce as the constraint.
    /// Returns this for fluent chaining with LastArg access.
    /// </summary>
    new IMethodExecuteBuilder<TCallback, TArg> Verifiable();

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify()
    /// with a specific Times constraint. Returns this for fluent chaining.
    /// </summary>
    /// <param name="times">The Times constraint to verify against.</param>
    new IMethodExecuteBuilder<TCallback, TArg> Verifiable(Times times);
}

/// <summary>
/// Returned by Execute(callback) for void methods with multiple trackable parameters.
/// Supports tracking with LastArgs access and optional sequence chaining via ThenExecute.
/// </summary>
public interface IMethodExecuteBuilderArgs<TCallback, TArgs> : IMethodTrackingArgs<TArgs>
{
    /// <summary>
    /// Elevates to sequence mode and adds another callback.
    /// The current callback becomes the first element; the new callback is appended.
    /// </summary>
    IMethodExecuteSequence<TCallback> ThenExecute(TCallback callback);

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify().
    /// Uses Times.AtLeastOnce as the constraint.
    /// Returns this for fluent chaining with LastArgs access.
    /// </summary>
    new IMethodExecuteBuilderArgs<TCallback, TArgs> Verifiable();

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify()
    /// with a specific Times constraint. Returns this for fluent chaining.
    /// </summary>
    /// <param name="times">The Times constraint to verify against.</param>
    new IMethodExecuteBuilderArgs<TCallback, TArgs> Verifiable(Times times);
}
