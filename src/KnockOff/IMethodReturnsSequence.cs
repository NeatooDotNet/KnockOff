namespace KnockOff;

/// <summary>
/// Represents a sequence of non-void method callbacks.
/// Each callback in the sequence is invoked exactly once.
/// </summary>
public interface IMethodReturnsSequence : IMethodSequence
{
}

/// <summary>
/// Typed sequence for non-void methods that enables ThenReturns chaining.
/// </summary>
public interface IMethodReturnsSequence<TCallback> : IMethodReturnsSequence
{
    /// <summary>
    /// Marks this sequence for verification by Stub.Verify().
    /// Returns this for fluent chaining.
    /// </summary>
    new IMethodReturnsSequence<TCallback> Verifiable();

    /// <summary>
    /// Adds another callback to the sequence.
    /// Each callback in the sequence is invoked exactly once.
    /// </summary>
    IMethodReturnsSequence<TCallback> ThenReturns(TCallback callback);
}
