namespace KnockOff;

/// <summary>
/// Tracks invocations of a method callback registration.
/// </summary>
public interface IMethodTracking
{
    /// <summary>Number of times this callback was invoked.</summary>
    int CallCount { get; }

    /// <summary>True if CallCount > 0.</summary>
    bool WasCalled { get; }

    /// <summary>Clears tracking for this registration only.</summary>
    void Reset();
}

/// <summary>
/// Tracks invocations with single argument capture.
/// </summary>
public interface IMethodTracking<TArg> : IMethodTracking
{
    /// <summary>Last argument passed to this callback.</summary>
    TArg? LastArg { get; }
}

/// <summary>
/// Tracks invocations with multiple argument capture as named tuple.
/// </summary>
public interface IMethodTrackingArgs<TArgs> : IMethodTracking
{
    /// <summary>Last arguments passed to this callback as named tuple.</summary>
    TArgs? LastArgs { get; }
}
