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

#region customization-patterns-user-method-basic
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

#region customization-patterns-user-method-async
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

#region customization-patterns-priority-example
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

#region customization-patterns-combining-patterns
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
// Callback Ko Access Example
// ============================================================================

public interface IPatternKoAccessService
{
    PatternUser GetUser(int id);
    void Initialize();
    string Name { get; set; }
}

[KnockOff]
public partial class PatternKoAccessServiceKnockOff : IPatternKoAccessService { }

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

        #region customization-patterns-callback-method
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

        #region customization-patterns-callback-property
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

        #region customization-patterns-callback-indexer
        // Getter with key parameter
        knockOff.Indexer.OnGet = (ko, key) => key switch
        {
            "Name" => new PatternPropertyInfo { Value = "Test" },
            "Age" => new PatternPropertyInfo { Value = "25" },
            _ => null
        };

        // Setter with key and value parameters
        knockOff.Indexer.OnSet = (ko, key, value) =>
        {
            // Custom logic
            // Note: When OnSet is set, value does NOT go to backing dictionary
        };
        #endregion
    }

    public static void CallbackKoAccessExample()
    {
        var knockOff = new PatternKoAccessServiceKnockOff();
        IPatternKoAccessService service = knockOff;

        #region customization-patterns-callback-ko-access
        knockOff.GetUser.OnCall = (ko, id) =>
        {
            // Access other interceptors
            if (ko.Initialize.WasCalled)
                return new PatternUser { Id = id, Name = "Initialized" };

            // Access backing fields via interceptor
            return new PatternUser { Id = id, Name = ko.Name.Value };
        };
        #endregion
    }

    public static void PriorityExampleUsage()
    {
        #region customization-patterns-priority-example-usage
        // Test
        var knockOff = new PatternServiceKnockOff();
        IPatternService service = knockOff;

        // No callback set → uses user method
        var result1 = service.Calculate(5);  // Returns 10 (5 * 2)

        // Set callback → overrides user method
        // Note: Interceptor has "2" suffix because user method "Calculate" exists
        knockOff.Calculate2.OnCall = (ko, input) => input * 100;
        var result2 = service.Calculate(5);  // Returns 500 (callback)

        // Reset clears callback → back to user method
        knockOff.Calculate2.Reset();
        var result3 = service.Calculate(5);  // Returns 10 (user method)
        #endregion
    }

    public static void ResetBehaviorExample()
    {
        var knockOff = new PatternKoAccessServiceKnockOff();
        IPatternKoAccessService service = knockOff;

        #region customization-patterns-reset-behavior
        // Set up state
        knockOff.GetUser.OnCall = (ko, id) => new PatternUser { Name = "Callback" };
        service.GetUser(1);
        service.GetUser(2);

        Assert.Equal(2, knockOff.GetUser.CallCount);

        // Reset
        knockOff.GetUser.Reset();

        Assert.Equal(0, knockOff.GetUser.CallCount);  // Tracking cleared
        Assert.Null(knockOff.GetUser.OnCall);  // Callback cleared

        // Now uses default (no user method defined)
        var user = service.GetUser(3);
        #endregion
    }

    public static void CombiningPatternsUsageExample()
    {
        #region customization-patterns-combining-patterns-usage
        // Test 1: Uses default (null)
        var knockOff = new PatternCombinedRepositoryKnockOff();
        IPatternCombinedRepository repo = knockOff;
        Assert.Null(repo.GetById(999));

        // Test 2: Override for specific IDs
        // Note: Interceptor has "2" suffix because user method "GetById" exists
        knockOff.GetById2.OnCall = (ko, id) => id switch
        {
            1 => new PatternUser { Id = 1, Name = "Admin" },
            2 => new PatternUser { Id = 2, Name = "Guest" },
            _ => null  // Fall through to "not found"
        };

        Assert.Equal("Admin", repo.GetById(1)?.Name);
        Assert.Null(repo.GetById(999));  // Still null

        // Test 3: Reset and use different callback
        knockOff.GetById2.Reset();
        knockOff.GetById2.OnCall = (ko, id) =>
            new PatternUser { Id = id, Name = $"User-{id}" };

        Assert.Equal("User-999", repo.GetById(999)?.Name);
        #endregion
    }
}

// Minimal Assert class for compilation (tests use xUnit)
file static class Assert
{
    public static void Equal<T>(T expected, T actual) { }
    public static void Null(object? value) { }
}
