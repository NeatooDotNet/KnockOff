namespace KnockOff;

/// <summary>
/// Tracking interface for When chains.
/// Extends <see cref="ITracking"/> with verifiable support.
/// </summary>
public interface IWhenTracking : ITracking
{
    /// <summary>
    /// Marks this When chain for verification by Stub.Verify().
    /// Returns this for fluent chaining.
    /// </summary>
    IWhenTracking Verifiable();
}

/// <summary>
/// Chainable When for non-void methods. Allows adding more matchers
/// or terminal operations after <see cref="IWhenBuilder{TDelegate, TReturn}.Returns"/>.
/// </summary>
/// <typeparam name="TDelegate">The delegate type for callbacks (e.g., Func&lt;int, int, int&gt;).</typeparam>
/// <typeparam name="TReturn">The return type of the method.</typeparam>
public interface IWhenChain<TDelegate, TReturn> : IWhenTracking
{
    // ThenWhen overloads are generated per-method because they depend on parameter types.
    // The generated methods will have signatures like:
    //   IWhenBuilder<TDelegate, TReturn> ThenWhen(T1 arg1, T2 arg2, ...);
    //   IWhenBuilder<TDelegate, TReturn> ThenWhen(Func<T1, T2, ..., bool> predicate);

    /// <summary>
    /// Adds an unconditional callback as the terminal matcher.
    /// This matcher always matches and repeats forever.
    /// </summary>
    /// <param name="callback">The callback to invoke when reached.</param>
    IWhenTracking ThenCall(TDelegate callback);

    /// <summary>
    /// Closes the chain with no matcher.
    /// When the chain is exhausted, invocations fall through to the next priority.
    /// </summary>
    IWhenTracking ThenNone();

    /// <summary>
    /// Marks this When chain for verification by Stub.Verify().
    /// Returns this for fluent chaining with ThenWhen access.
    /// </summary>
    new IWhenChain<TDelegate, TReturn> Verifiable();
}

/// <summary>
/// Builder returned by When() and ThenWhen() for non-void methods.
/// Captures the matcher predicate and awaits a return value via <see cref="Returns"/>.
/// </summary>
/// <typeparam name="TDelegate">The delegate type for callbacks (e.g., Func&lt;int, int, int&gt;).</typeparam>
/// <typeparam name="TReturn">The return type of the method.</typeparam>
public interface IWhenBuilder<TDelegate, TReturn>
{
    /// <summary>
    /// Specifies the return value when the matcher predicate matches.
    /// For async methods, the value is automatically wrapped with Task.FromResult().
    /// </summary>
    /// <param name="value">The value to return when matched.</param>
    IWhenChain<TDelegate, TReturn> Returns(TReturn value);
}

/// <summary>
/// When chain for void methods. Returned directly by When() (no builder needed - nothing to return).
/// Allows adding matchers, optional callbacks, and parameter-specific verification.
/// </summary>
/// <typeparam name="TDelegate">The delegate type for callbacks (e.g., Action&lt;int, int&gt;).</typeparam>
public interface IVoidWhenChain<TDelegate> : IWhenTracking
{
    // ThenWhen overloads are generated per-method because they depend on parameter types.
    // The generated methods will have signatures like:
    //   IVoidWhenChain<TDelegate> ThenWhen(T1 arg1, T2 arg2, ...);
    //   IVoidWhenChain<TDelegate> ThenWhen(Func<T1, T2, ..., bool> predicate);

    /// <summary>
    /// Configures an optional callback to invoke when this matcher matches.
    /// The callback receives the same arguments as the method call.
    /// Returns this for fluent chaining.
    /// </summary>
    /// <param name="callback">The callback to invoke when matched.</param>
#pragma warning disable CA1716 // Identifiers should not match keywords
    IVoidWhenChain<TDelegate> Call(TDelegate callback);
#pragma warning restore CA1716

    /// <summary>
    /// Adds an unconditional callback as the terminal matcher.
    /// This matcher always matches and repeats forever.
    /// </summary>
    /// <param name="callback">The callback to invoke when reached.</param>
    IWhenTracking ThenCall(TDelegate callback);

    /// <summary>
    /// Closes the chain with no matcher.
    /// When the chain is exhausted, invocations fall through to the next priority.
    /// </summary>
    IWhenTracking ThenNone();

    /// <summary>
    /// Verifies this specific matcher was called the expected number of times.
    /// Different from Verify() which checks if the chain reached terminal state.
    /// </summary>
    /// <param name="times">The expected number of times this matcher was called.</param>
    void Verify(Times times);

    /// <summary>
    /// Marks this When chain for verification by Stub.Verify().
    /// Returns this for fluent chaining with ThenWhen access.
    /// </summary>
    new IVoidWhenChain<TDelegate> Verifiable();
}
