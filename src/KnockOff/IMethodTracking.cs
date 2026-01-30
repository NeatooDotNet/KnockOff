namespace KnockOff;

/// <summary>
/// Tracks invocations of a method callback registration.
/// </summary>
public interface IMethodTracking : ITracking
{
    /// <summary>Clears tracking state for this registration (LastArg/LastArgs = default, call count = 0).</summary>
    new void Reset();

    /// <summary>
    /// Verifies the callback was invoked at least once.
    /// Throws VerificationException if not satisfied.
    /// </summary>
    new void Verify();

    /// <summary>
    /// Verifies the callback was invoked according to the Times constraint.
    /// Throws VerificationException if not satisfied.
    /// </summary>
    /// <param name="times">The Times constraint to verify against.</param>
    void Verify(Times times);

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify().
    /// Uses Times.AtLeastOnce as the constraint.
    /// Returns this for fluent chaining.
    /// </summary>
    IMethodTracking Verifiable();

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify()
    /// with a specific Times constraint. Returns this for fluent chaining.
    /// </summary>
    /// <param name="times">The Times constraint to verify against.</param>
    IMethodTracking Verifiable(Times times);
}

/// <summary>
/// Tracks invocations with single argument capture.
/// </summary>
public interface IMethodTracking<TArg> : IMethodTracking
{
    /// <summary>Last argument passed to this callback. Default if never called.</summary>
    TArg LastArg { get; }

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify().
    /// Uses Times.AtLeastOnce as the constraint.
    /// Returns this for fluent chaining with LastArg access.
    /// </summary>
    new IMethodTracking<TArg> Verifiable();

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify()
    /// with a specific Times constraint. Returns this for fluent chaining.
    /// </summary>
    /// <param name="times">The Times constraint to verify against.</param>
    new IMethodTracking<TArg> Verifiable(Times times);
}

/// <summary>
/// Tracks invocations with multiple argument capture as named tuple.
/// </summary>
public interface IMethodTrackingArgs<TArgs> : IMethodTracking
{
    /// <summary>Last arguments passed to this callback as named tuple. Default if never called.</summary>
    TArgs LastArgs { get; }

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify().
    /// Uses Times.AtLeastOnce as the constraint.
    /// Returns this for fluent chaining with LastArgs access.
    /// </summary>
    new IMethodTrackingArgs<TArgs> Verifiable();

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify()
    /// with a specific Times constraint. Returns this for fluent chaining.
    /// </summary>
    /// <param name="times">The Times constraint to verify against.</param>
    new IMethodTrackingArgs<TArgs> Verifiable(Times times);
}
