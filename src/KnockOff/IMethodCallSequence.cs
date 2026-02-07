namespace KnockOff;

/// <summary>
/// Represents a sequence of void method callbacks.
/// Each callback in the sequence is invoked exactly once.
/// </summary>
public interface IMethodCallSequence : IMethodSequence
{
}

/// <summary>
/// Typed sequence for void methods that enables ThenCall chaining.
/// </summary>
public interface IMethodCallSequence<TCallback> : IMethodCallSequence
{
    /// <summary>
    /// Marks this sequence for verification by Stub.Verify().
    /// Returns this for fluent chaining.
    /// </summary>
    new IMethodCallSequence<TCallback> Verifiable();

    /// <summary>
    /// Adds another callback to the sequence.
    /// Each callback in the sequence is invoked exactly once.
    /// </summary>
    IMethodCallSequence<TCallback> ThenCall(TCallback callback);
}
