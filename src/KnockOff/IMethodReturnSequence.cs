namespace KnockOff;

/// <summary>
/// Represents a sequence of non-void method callbacks.
/// Each callback in the sequence is invoked exactly once.
/// </summary>
public interface IMethodReturnSequence : IMethodSequence
{
}

/// <summary>
/// Typed sequence for non-void methods that enables ThenReturn chaining.
/// </summary>
public interface IMethodReturnSequence<TCallback> : IMethodReturnSequence
{
    /// <summary>
    /// Marks this sequence for verification by Stub.Verify().
    /// Returns this for fluent chaining.
    /// </summary>
    new IMethodReturnSequence<TCallback> Verifiable();

    /// <summary>
    /// Adds another callback to the sequence.
    /// Each callback in the sequence is invoked exactly once.
    /// </summary>
    IMethodReturnSequence<TCallback> ThenReturn(TCallback callback);
}
