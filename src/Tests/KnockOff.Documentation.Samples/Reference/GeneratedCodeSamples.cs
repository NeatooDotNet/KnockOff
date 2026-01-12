/// <summary>
/// Code samples for docs/reference/generated-code.md
///
/// Snippets in this file:
/// - generated-code-input-example
/// - generated-code-multiple-parameters
/// - generated-code-interface-constraint-separate
///
/// Corresponding tests: GeneratedCodeSamplesTests.cs
/// </summary>

namespace KnockOff.Documentation.Samples.Reference;

// ============================================================================
// Domain Types
// ============================================================================

public class GenUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// ============================================================================
// Input Example (shows what you write)
// ============================================================================

#region generated-code-input-example
public interface IGenUserService
{
    string Name { get; set; }
    GenUser? GetUser(int id);
}

[KnockOff]
public partial class GenUserServiceKnockOff : IGenUserService
{
    protected GenUser? GetUser(int id) => new GenUser { Id = id };
}
#endregion

// ============================================================================
// Multiple Parameters Tracking
// ============================================================================

public interface IGenLogger
{
    void Log(string level, string message, int code);
}

[KnockOff]
public partial class GenLoggerKnockOff : IGenLogger { }

#region generated-code-multiple-parameters
public static class MultipleParametersExample
{
    public static void TrackingUsage()
    {
        var knockOff = new GenLoggerKnockOff();
        IGenLogger logger = knockOff;

        // Callback receives individual parameters
        knockOff.Log.OnCall = (ko, level, message, code) =>
        {
            Console.WriteLine($"[{level}] {message} ({code})");
        };

        logger.Log("INFO", "Started", 100);

        // Tracking uses LastCallArgs tuple
        var args = knockOff.Log.LastCallArgs;
        var level = args?.level;    // "INFO"
        var message = args?.message; // "Started"
        var code = args?.code;      // 100

        _ = (level, message, code);
    }
}
#endregion

// ============================================================================
// Interface Constraint - Separate Stubs
// ============================================================================

public interface IGenAuditLogger
{
    void Log(string message);
}

public interface IGenAuditor
{
    void Audit(string action);
}

#region generated-code-interface-constraint-separate
[KnockOff]
public partial class GenAuditLoggerKnockOff : IGenAuditLogger { }

[KnockOff]
public partial class GenAuditorKnockOff : IGenAuditor { }

public static class SeparateStubsExample
{
    public static void Usage()
    {
        // In test - use separate stubs
        var logger = new GenAuditLoggerKnockOff();
        var auditor = new GenAuditorKnockOff();

        logger.Log.OnCall = (ko, msg) => Console.WriteLine(msg);
        auditor.Audit.OnCall = (ko, action) => Console.WriteLine($"Audit: {action}");
    }
}
#endregion
