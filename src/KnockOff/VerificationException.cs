using System.Text;

namespace KnockOff;

/// <summary>
/// Details about a single verification failure.
/// </summary>
public class VerificationFailure
{
    /// <summary>The member that failed verification.</summary>
    public string Member { get; }

    /// <summary>The expected Times constraint.</summary>
    public Times Expected { get; }

    /// <summary>The actual call count.</summary>
    public int Actual { get; }

    /// <summary>Human-readable failure message.</summary>
    public string Message { get; }

    /// <summary>
    /// Creates a verification failure for Times-based failures.
    /// </summary>
    /// <param name="member">The member that failed verification.</param>
    /// <param name="expected">The expected Times constraint.</param>
    /// <param name="actual">The actual call count.</param>
    public VerificationFailure(string member, Times expected, int actual)
    {
        Member = member;
        Expected = expected;
        Actual = actual;
        Message = $"{member}: expected {expected}, actual {actual} calls";
    }

    /// <summary>
    /// Creates a verification failure for sequence failures.
    /// </summary>
    /// <param name="member">The member that failed verification.</param>
    /// <param name="sequenceLength">The total number of callbacks in the sequence.</param>
    /// <param name="completedCount">The number of callbacks that were invoked.</param>
    /// <returns>A VerificationFailure for the sequence failure.</returns>
    public static VerificationFailure SequenceIncomplete(string member, int sequenceLength, int completedCount)
    {
        return new VerificationFailure(
            member,
            Times.Exactly(sequenceLength),
            completedCount,
            $"{member}: sequence incomplete - {completedCount} of {sequenceLength} callbacks invoked");
    }

    private VerificationFailure(string member, Times expected, int actual, string message)
    {
        Member = member;
        Expected = expected;
        Actual = actual;
        Message = message;
    }

    /// <inheritdoc />
    public override string ToString() => Message;
}

/// <summary>
/// Thrown when verification fails. Contains all failures when multiple exist.
/// </summary>
public class VerificationException : Exception
{
    /// <summary>All verification failures that occurred.</summary>
    public IReadOnlyList<VerificationFailure> Failures { get; }

    /// <summary>
    /// Creates a default verification exception.
    /// </summary>
    public VerificationException()
        : base("Verification failed.")
    {
        Failures = Array.Empty<VerificationFailure>();
    }

    /// <summary>
    /// Creates a verification exception with a single failure.
    /// </summary>
    /// <param name="failure">The verification failure.</param>
    public VerificationException(VerificationFailure failure)
        : base(FormatMessage(new[] { failure }))
    {
        Failures = new[] { failure };
    }

    /// <summary>
    /// Creates a verification exception with multiple failures.
    /// </summary>
    /// <param name="failures">The verification failures.</param>
    public VerificationException(IReadOnlyList<VerificationFailure> failures)
        : base(FormatMessage(failures))
    {
        Failures = failures;
    }

    /// <summary>
    /// Creates a verification exception with a message.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public VerificationException(string message)
        : base(message)
    {
        Failures = Array.Empty<VerificationFailure>();
    }

    /// <summary>
    /// Creates a verification exception with a message and inner exception.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public VerificationException(string message, Exception innerException)
        : base(message, innerException)
    {
        Failures = Array.Empty<VerificationFailure>();
    }

    private static string FormatMessage(IReadOnlyList<VerificationFailure> failures)
    {
        if (failures.Count == 0)
            return "Verification failed.";

        if (failures.Count == 1)
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "Verification failed: {0}", failures[0].Message);

        var sb = new StringBuilder();
        sb.Append(System.Globalization.CultureInfo.InvariantCulture, $"Verification failed with {failures.Count} error(s):");
        sb.AppendLine();
        foreach (var failure in failures)
        {
            sb.Append(System.Globalization.CultureInfo.InvariantCulture, $"  - {failure.Message}");
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }
}
