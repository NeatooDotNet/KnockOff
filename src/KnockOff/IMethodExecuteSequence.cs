namespace KnockOff;

/// <summary>
/// Represents a sequence of void method callbacks.
/// Each callback in the sequence is invoked exactly once.
/// </summary>
public interface IMethodExecuteSequence : IMethodSequence
{
}

/// <summary>
/// Typed sequence for void methods that enables ThenExecute chaining.
/// </summary>
public interface IMethodExecuteSequence<TCallback> : IMethodExecuteSequence
{
    /// <summary>
    /// Marks this sequence for verification by Stub.Verify().
    /// Returns this for fluent chaining.
    /// </summary>
    new IMethodExecuteSequence<TCallback> Verifiable();

    /// <summary>
    /// Adds another callback to the sequence.
    /// Each callback in the sequence is invoked exactly once.
    /// </summary>
    IMethodExecuteSequence<TCallback> ThenExecute(TCallback callback);
}
