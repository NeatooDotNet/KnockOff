namespace KnockOff.Documentation.Samples.SampleDomain;

/// <summary>
/// Logger interface for multiple interface examples.
/// </summary>
public interface ILogger
{
    void Log(string message);
    void LogError(string message, Exception ex);
    string Name { get; set; }
}

/// <summary>
/// Notifier interface for multiple interface examples.
/// </summary>
public interface INotifier
{
    void Notify(string message);
    string Name { get; set; }
}

/// <summary>
/// Auditor interface for multiple interface examples.
/// </summary>
public interface IAuditor
{
    void Audit(string action, string details);
    int AuditCount { get; }
}
