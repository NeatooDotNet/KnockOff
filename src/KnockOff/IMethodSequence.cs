namespace KnockOff;

/// <summary>
/// Represents a sequence of method callbacks with Times constraints.
/// Returned by OnCall(callback, Times) to enable ThenCall chaining.
/// </summary>
public interface IMethodSequence
{
    /// <summary>Total calls across all callbacks in sequence.</summary>
    int TotalCallCount { get; }

    /// <summary>Verify all Times constraints in the sequence were satisfied.</summary>
    bool Verify();

    /// <summary>Reset all tracking in the sequence.</summary>
    void Reset();
}

/// <summary>
/// Typed sequence that enables ThenCall chaining for specific signatures.
/// </summary>
public interface IMethodSequence<TCallback> : IMethodSequence
{
    /// <summary>Add another callback to the sequence.</summary>
    IMethodSequence<TCallback> ThenCall(TCallback callback, Times times);
}
