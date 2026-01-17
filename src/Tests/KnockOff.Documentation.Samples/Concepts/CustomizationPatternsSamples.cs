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
        knockOff.DoSomething.OnCall((ko) =>
        {
            // Custom logic for this test
        });

        // Method with return value
        knockOff.GetUser.OnCall((ko, id) => new PatternUser { Id = id, Name = "Mocked" });

        // Method with multiple parameters (individual params)
        knockOff.Calculate.OnCall((ko, name, value, flag) =>
        {
            return flag ? value * 2 : value;
        });
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
        // Track Initialize calls
        var initTracking = knockOff.Initialize.OnCall((ko) => { /* initialization logic */ });

        knockOff.GetUser.OnCall((ko, id) =>
        {
            // Access tracking from other callbacks
            if (initTracking.WasCalled)
                return new PatternUser { Id = id, Name = "Initialized" };

            // Access backing fields via interceptor
            return new PatternUser { Id = id, Name = ko.Name.Value };
        });
        #endregion
    }

    public static void PriorityExampleUsage()
    {
        #region customization-patterns-priority-example-usage
        // Test
        var knockOff = new PatternServiceKnockOff();
        IPatternService service = knockOff;

        // User method always provides implementation
        var result1 = service.Calculate(5);  // Returns 10 (5 * 2)
        var result2 = service.Calculate(10); // Returns 20 (10 * 2)

        // Interceptor with "2" suffix tracks calls (no OnCall - user method is the implementation)
        Assert.Equal(2, knockOff.Calculate2.CallCount);
        Assert.True(knockOff.Calculate2.WasCalled);
        Assert.Equal(10, knockOff.Calculate2.LastArg);

        // Reset clears tracking
        knockOff.Calculate2.Reset();
        Assert.Equal(0, knockOff.Calculate2.CallCount);
        #endregion
    }

    public static void ResetBehaviorExample()
    {
        var knockOff = new PatternKoAccessServiceKnockOff();
        IPatternKoAccessService service = knockOff;

        #region customization-patterns-reset-behavior
        // Set up state
        var tracking = knockOff.GetUser.OnCall((ko, id) => new PatternUser { Name = "Callback" });
        service.GetUser(1);
        service.GetUser(2);

        Assert.Equal(2, tracking.CallCount);

        // Reset
        knockOff.GetUser.Reset();

        // After reset, tracking object's callback is cleared
        // Now uses default (no user method defined)
        var user = service.GetUser(3);
        #endregion
    }

    public static void CombiningPatternsUsageExample()
    {
        #region customization-patterns-combining-patterns-usage
        // User method returns null (not found)
        var knockOff = new PatternCombinedRepositoryKnockOff();
        IPatternCombinedRepository repo = knockOff;
        Assert.Null(repo.GetById(999));

        // User method is the implementation - always used
        Assert.Null(repo.GetById(1));
        Assert.Null(repo.GetById(2));

        // Interceptor with "2" suffix tracks calls (no OnCall)
        Assert.Equal(3, knockOff.GetById2.CallCount);
        Assert.True(knockOff.GetById2.WasCalled);
        Assert.Equal(2, knockOff.GetById2.LastArg);

        // Reset clears tracking
        knockOff.GetById2.Reset();
        Assert.Equal(0, knockOff.GetById2.CallCount);
        #endregion
    }
}

// Minimal Assert class for compilation (tests use xUnit)
file static class Assert
{
    public static void True(bool condition) { }
    public static void Equal<T>(T expected, T actual) { }
    public static void Null(object? value) { }
}
