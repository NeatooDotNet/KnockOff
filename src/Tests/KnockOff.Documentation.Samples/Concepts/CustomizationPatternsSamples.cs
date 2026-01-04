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
