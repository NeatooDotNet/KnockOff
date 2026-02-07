namespace KnockOff;

/// <summary>
/// Tracks invocations of a property getter callback registration.
/// Returned by Get() to enable verification and fluent chaining.
/// </summary>
/// <remarks>
/// Unlike method tracking, property getter tracking does not capture a "LastValue"
/// because getter callbacks are deterministic - they always return the same configured value.
/// </remarks>
public interface IPropertyGetTracking
{
    /// <summary>Clears tracking state for this registration (call count = 0).</summary>
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
    IPropertyGetTracking Verifiable();

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify()
    /// with a specific Times constraint. Returns this for fluent chaining.
    /// </summary>
    /// <param name="times">The Times constraint to verify against.</param>
    IPropertyGetTracking Verifiable(Times times);
}

/// <summary>
/// Tracks invocations of a property setter callback registration with value capture.
/// Returned by Set() to enable verification, value inspection, and fluent chaining.
/// </summary>
public interface IPropertySetTracking<TValue>
{
    /// <summary>Last value passed to this setter callback. Default if never called.</summary>
    TValue LastValue { get; }

    /// <summary>Clears tracking state for this registration (LastValue = default, call count = 0).</summary>
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
    IPropertySetTracking<TValue> Verifiable();

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify()
    /// with a specific Times constraint. Returns this for fluent chaining.
    /// </summary>
    /// <param name="times">The Times constraint to verify against.</param>
    IPropertySetTracking<TValue> Verifiable(Times times);
}
