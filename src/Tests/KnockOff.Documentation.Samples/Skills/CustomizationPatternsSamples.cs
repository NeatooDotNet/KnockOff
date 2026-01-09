/// <summary>
/// Code samples for ~/.claude/skills/knockoff/customization-patterns.md
///
/// Snippets in this file:
/// - skill:customization-patterns:user-method-basic
/// - skill:customization-patterns:user-method-async
/// - skill:customization-patterns:callback-method
/// - skill:customization-patterns:callback-property
/// - skill:customization-patterns:callback-indexer
/// - skill:customization-patterns:callback-overloads
/// - skill:customization-patterns:callback-out-params
/// - skill:customization-patterns:callback-ref-params
/// - skill:customization-patterns:priority-in-action
/// - skill:customization-patterns:reset-behavior
/// - skill:customization-patterns:combining-patterns
///
/// Corresponding tests: CustomizationPatternsSamplesTests.cs
/// </summary>

namespace KnockOff.Documentation.Samples.Skills;

// ============================================================================
// Domain Types for Customization Samples
// ============================================================================

public class CpUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// ============================================================================
// User Method - Basic
// ============================================================================

public interface ICpCalculator
{
    int Add(int a, int b);
    double Divide(int numerator, int denominator);
}

#region skill:customization-patterns:user-method-basic
[KnockOff]
public partial class CpCalculatorKnockOff : ICpCalculator
{
    protected int Add(int a, int b) => a + b;

    protected double Divide(int numerator, int denominator) =>
        denominator == 0 ? 0 : (double)numerator / denominator;
}
#endregion

// ============================================================================
// User Method - Async
// ============================================================================

public interface ICpRepository
{
    Task<CpUser?> GetByIdAsync(int id);
    ValueTask<int> CountAsync();
}

#region skill:customization-patterns:user-method-async
[KnockOff]
public partial class CpRepoKnockOff : ICpRepository
{
    protected Task<CpUser?> GetByIdAsync(int id) =>
        Task.FromResult<CpUser?>(new CpUser { Id = id });

    protected ValueTask<int> CountAsync() =>
        new ValueTask<int>(100);
}
#endregion

// ============================================================================
// Callback - Method
// ============================================================================

public interface ICpCallbackService
{
    void Initialize();
    CpUser GetById(int id);
    List<CpUser> Search(string query, int limit, int offset);
}

#region skill:customization-patterns:callback-method
[KnockOff]
public partial class CpCallbackServiceKnockOff : ICpCallbackService { }
#endregion

// ============================================================================
// Callback - Property
// ============================================================================

public interface ICpPropertyService
{
    CpUser? CurrentUser { get; set; }
}

#region skill:customization-patterns:callback-property
[KnockOff]
public partial class CpPropertyServiceKnockOff : ICpPropertyService { }
#endregion

// ============================================================================
// Callback - Indexer
// ============================================================================

public interface ICpPropertyStore
{
    object? this[string key] { get; set; }
}

#region skill:customization-patterns:callback-indexer
[KnockOff]
public partial class CpPropertyStoreKnockOff : ICpPropertyStore { }
#endregion

// ============================================================================
// Callback - Overloads
// ============================================================================

public interface ICpOverloadService
{
    void Process(string data);
    void Process(string data, int priority);
    int Calculate(int value);
    int Calculate(int a, int b);
}

#region skill:customization-patterns:callback-overloads
[KnockOff]
public partial class CpOverloadServiceKnockOff : ICpOverloadService { }
#endregion

// ============================================================================
// Callback - Out Parameters
// ============================================================================

public interface ICpParser
{
    bool TryParse(string input, out int result);
    void GetStats(out int count, out double average);
}

#region skill:customization-patterns:callback-out-params
[KnockOff]
public partial class CpParserKnockOff : ICpParser { }
#endregion

// ============================================================================
// Callback - Ref Parameters
// ============================================================================

public interface ICpProcessor
{
    void Increment(ref int value);
    bool TryUpdate(string key, ref string value);
}

#region skill:customization-patterns:callback-ref-params
[KnockOff]
public partial class CpProcessorKnockOff : ICpProcessor { }
#endregion

// ============================================================================
// Priority in Action
// ============================================================================

public interface ICpPriorityService
{
    int Calculate(int x);
}

#region skill:customization-patterns:priority-in-action
[KnockOff]
public partial class CpPriorityServiceKnockOff : ICpPriorityService
{
    protected int Calculate(int x) => x * 2;  // User method
}
#endregion

// ============================================================================
// Reset Behavior
// ============================================================================

public interface ICpResetRepository
{
    CpUser? GetUser(int id);
}

#region skill:customization-patterns:reset-behavior
[KnockOff]
public partial class CpResetRepositoryKnockOff : ICpResetRepository { }
#endregion

// ============================================================================
// Combining Patterns
// ============================================================================

public interface ICpCombinedRepository
{
    CpUser? GetById(int id);
}

#region skill:customization-patterns:combining-patterns
[KnockOff]
public partial class CpCombinedRepoKnockOff : ICpCombinedRepository
{
    // Default: not found
    protected CpUser? GetById(int id) => null;
}
#endregion
