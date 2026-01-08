/// <summary>
/// Code samples for docs/concepts/customization-patterns.md
///
/// Snippets in this file:
/// - docs:customization-patterns:user-method-basic
/// - docs:customization-patterns:user-method-async
/// - docs:customization-patterns:callback-method
/// - docs:customization-patterns:callback-property
/// - docs:customization-patterns:callback-indexer
/// - docs:customization-patterns:priority-example
/// - docs:customization-patterns:reset-behavior
/// - docs:customization-patterns:combining-patterns
///
/// Corresponding tests: CustomizationPatternsSamplesTests.cs
/// </summary>

namespace KnockOff.Documentation.Samples.Concepts;

// ============================================================================
// User-Defined Methods - Basic Example
// ============================================================================

#region docs:customization-patterns:user-method-basic
public interface IPatternUserService
{
    PatternUser GetUser(int id);
    int CalculateScore(string name, int baseScore);
}

[KnockOff]
public partial class PatternUserServiceKnockOff : IPatternUserService
{
    // Generator detects this and calls it for IPatternUserService.GetUser
    protected PatternUser GetUser(int id) => new PatternUser { Id = id, Name = "Default User" };

    // Multi-parameter methods work the same way
    protected int CalculateScore(string name, int baseScore) => baseScore * 2;
}
#endregion

public class PatternUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// ============================================================================
// User-Defined Methods - Async Example
// ============================================================================

#region docs:customization-patterns:user-method-async
public interface IPatternRepository
{
    Task<PatternUser?> GetByIdAsync(int id);
    ValueTask<int> CountAsync();
}

[KnockOff]
public partial class PatternRepositoryKnockOff : IPatternRepository
{
    protected Task<PatternUser?> GetByIdAsync(int id) =>
        Task.FromResult<PatternUser?>(new PatternUser { Id = id });

    protected ValueTask<int> CountAsync() =>
        new ValueTask<int>(42);
}
#endregion

// ============================================================================
// Method Callbacks Example
// ============================================================================

public interface IPatternCallbackService
{
    void DoSomething();
    PatternUser GetUser(int id);
    int Calculate(string name, int value, bool flag);
}

[KnockOff]
public partial class PatternCallbackServiceKnockOff : IPatternCallbackService { }

// ============================================================================
// Property Callbacks Example
// ============================================================================

public interface IPatternPropertyService
{
    string Name { get; set; }
}

[KnockOff]
public partial class PatternPropertyServiceKnockOff : IPatternPropertyService { }

// ============================================================================
// Indexer Callbacks Example
// ============================================================================

public interface IPatternIndexerService
{
    PatternPropertyInfo? this[string key] { get; set; }
}

[KnockOff]
public partial class PatternIndexerServiceKnockOff : IPatternIndexerService { }

public class PatternPropertyInfo
{
    public object? Value { get; set; }
}

// ============================================================================
// Priority Example
// ============================================================================

#region docs:customization-patterns:priority-example
public interface IPatternService
{
    int Calculate(int input);
}

[KnockOff]
public partial class PatternServiceKnockOff : IPatternService
{
    // User method returns input * 2
    protected int Calculate(int input) => input * 2;
}
#endregion

// ============================================================================
// Combining Patterns Example
// ============================================================================

#region docs:customization-patterns:combining-patterns
public interface IPatternCombinedRepository
{
    PatternUser? GetById(int id);
}

[KnockOff]
public partial class PatternCombinedRepositoryKnockOff : IPatternCombinedRepository
{
    // Default: return null (not found)
    protected PatternUser? GetById(int id) => null;
}
#endregion

// ============================================================================
// Usage Examples
// ============================================================================

/// <summary>
/// Usage examples demonstrating customization patterns.
/// Each method is compilable; snippets extract key portions.
/// </summary>
public static class CustomizationPatternsUsageExamples
{
    public static void MethodCallbackExample()
    {
        var knockOff = new PatternCallbackServiceKnockOff();

        #region docs:customization-patterns:callback-method
        // Void method
        knockOff.DoSomething.OnCall = (ko) =>
        {
            // Custom logic for this test
        };

        // Method with return value
        knockOff.GetUser.OnCall = (ko, id) => new PatternUser { Id = id, Name = "Mocked" };

        // Method with multiple parameters (individual params)
        knockOff.Calculate.OnCall = (ko, name, value, flag) =>
        {
            return flag ? value * 2 : value;
        };
        #endregion
    }

    public static void PropertyCallbackExample()
    {
        var knockOff = new PatternPropertyServiceKnockOff();

        #region docs:customization-patterns:callback-property
        // Getter callback
        knockOff.Name.OnGet = (ko) => "Dynamic Value";

        // Setter callback
        knockOff.Name.OnSet = (ko, value) =>
        {
            // Custom logic when property is set
            // Note: When OnSet is set, value does NOT go to backing field
        };
        #endregion
    }

    public static void IndexerCallbackExample()
    {
        var knockOff = new PatternIndexerServiceKnockOff();

        #region docs:customization-patterns:callback-indexer
        // Getter with key parameter
        knockOff.StringIndexer.OnGet = (ko, key) => key switch
        {
            "Name" => new PatternPropertyInfo { Value = "Test" },
            "Age" => new PatternPropertyInfo { Value = "25" },
            _ => null
        };

        // Setter with key and value parameters
        knockOff.StringIndexer.OnSet = (ko, key, value) =>
        {
            // Custom logic
            // Note: When OnSet is set, value does NOT go to backing dictionary
        };
        #endregion
    }

    // NOTE: Priority/Reset/Combining examples are left inline in docs.
    // The documentation describes callback > user method priority, but
    // current generator implementation always calls user methods directly
    // when they're defined, ignoring OnCall callbacks.
}
