namespace KnockOff;

/// <summary>
/// Thrown when verification fails (Times constraints not satisfied).
/// </summary>
public class VerificationException : Exception
{
    /// <summary>
    /// Creates a verification exception.
    /// </summary>
    public VerificationException() : base() { }

    /// <summary>
    /// Creates a verification exception with a message.
    /// </summary>
    public VerificationException(string message) : base(message) { }

    /// <summary>
    /// Creates a verification exception with a message and inner exception.
    /// </summary>
    public VerificationException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// Creates a verification exception with details about the failure.
    /// </summary>
    public VerificationException(string member, Times expected, int actual)
        : base($"Verification failed for '{member}': expected {FormatTimes(expected)}, actual {actual} calls")
    {
        Member = member;
        Expected = expected;
        Actual = actual;
    }

    /// <summary>The member that failed verification.</summary>
    public string? Member { get; }

    /// <summary>The expected Times constraint.</summary>
    public Times? Expected { get; }

    /// <summary>The actual call count.</summary>
    public int? Actual { get; }

    private static string FormatTimes(Times times)
    {
        if (times.IsForever) return "any number of calls";
        return $"{times.Count} calls";
    }
}
