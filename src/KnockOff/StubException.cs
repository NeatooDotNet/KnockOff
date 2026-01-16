namespace KnockOff;

/// <summary>
/// Exception thrown when a stub is invoked in strict mode without a configured callback.
/// </summary>
public class StubException : Exception
{
    public StubException()
    {
    }

    public StubException(string message)
        : base(message)
    {
    }

    public StubException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Creates a StubException for an unconfigured member invocation.
    /// </summary>
    /// <param name="interfaceName">The interface or type name.</param>
    /// <param name="memberName">The method or property name.</param>
    public static StubException NotConfigured(string interfaceName, string memberName)
    {
        return new StubException(
            $"{interfaceName}.{memberName} invocation failed with strict behavior. " +
            "Configure OnCall before invoking.");
    }

    /// <summary>
    /// Creates a StubException when a callback sequence has been exhausted.
    /// </summary>
    /// <param name="memberName">The method name.</param>
    public static StubException SequenceExhausted(string memberName)
    {
        return new StubException(
            $"Callback sequence exhausted for '{memberName}'. " +
            "All configured callbacks have been used the specified number of times.");
    }
}
