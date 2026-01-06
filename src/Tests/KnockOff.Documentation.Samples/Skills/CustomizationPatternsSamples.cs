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

// Void, no params
// knockOff.ICpCallbackService.Initialize.OnCall = (ko) =>
// {
//     // Custom initialization logic
// };

// Return, single param
// knockOff.ICpCallbackService.GetById.OnCall = (ko, id) =>
//     new CpUser { Id = id, Name = "Mocked" };

// Return, multiple params - individual parameters
// knockOff.ICpCallbackService.Search.OnCall = (ko, query, limit, offset) =>
//     results.Skip(offset).Take(limit).ToList();
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

// Getter
// knockOff.ICpPropertyService.CurrentUser.OnGet = (ko) =>
//     new CpUser { Name = "TestUser" };

// Setter
// knockOff.ICpPropertyService.CurrentUser.OnSet = (ko, value) =>
// {
//     capturedUser = value;
//     // Note: Value does NOT go to backing field
// };
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

// Getter (receives key)
// knockOff.ICpPropertyStore.StringIndexer.OnGet = (ko, key) => key switch
// {
//     "admin" => adminConfig,
//     "guest" => guestConfig,
//     _ => null
// };

// Setter (receives key and value)
// knockOff.ICpPropertyStore.StringIndexer.OnSet = (ko, key, value) =>
// {
//     // Custom logic
//     // Note: Value does NOT go to backing dictionary
// };
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

// var knockOff = new CpOverloadServiceKnockOff();

// Each overload has its own handler
// knockOff.ICpOverloadService.Process1.OnCall = (ko, data) => { /* 1-param overload */ };
// knockOff.ICpOverloadService.Process2.OnCall = (ko, data, priority) => { /* 2-param overload */ };

// Methods with return values work the same way
// knockOff.ICpOverloadService.Calculate1.OnCall = (ko, value) => value * 2;
// knockOff.ICpOverloadService.Calculate2.OnCall = (ko, a, b) => a + b;
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

// var knockOff = new CpParserKnockOff();

// REQUIRED: Explicit delegate type
// knockOff.ICpParser.TryParse.OnCall =
//     (ICpParser_TryParseHandler.TryParseDelegate)((ko, string input, out int result) =>
//     {
//         if (int.TryParse(input, out result))
//             return true;
//         result = 0;
//         return false;
//     });

// Void with multiple out params
// knockOff.ICpParser.GetStats.OnCall =
//     (ICpParser_GetStatsHandler.GetStatsDelegate)((ko, out int count, out double average) =>
//     {
//         count = 42;
//         average = 3.14;
//     });
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

// var knockOff = new CpProcessorKnockOff();

// Explicit delegate type required
// knockOff.ICpProcessor.Increment.OnCall =
//     (ICpProcessor_IncrementHandler.IncrementDelegate)((ko, ref int value) =>
//     {
//         value = value * 2;  // Modify the ref param
//     });

// Mixed regular + ref params
// knockOff.ICpProcessor.TryUpdate.OnCall =
//     (ICpProcessor_TryUpdateHandler.TryUpdateDelegate)((ko, string key, ref string value) =>
//     {
//         if (key == "valid")
//         {
//             value = value.ToUpper();
//             return true;
//         }
//         return false;
//     });

// Tracking captures INPUT value (before modification)
// int x = 5;
// processor.Increment(ref x);
// Assert.Equal(10, x);  // Modified
// Assert.Equal(5, knockOff.ICpProcessor.Increment.LastCallArg);  // Original input
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

// var knockOff = new CpPriorityServiceKnockOff();
// ICpPriorityService service = knockOff;

// No callback -> uses user method
// var r1 = service.Calculate(5);  // Returns 10

// Callback -> overrides user method
// knockOff.ICpPriorityService.Calculate.OnCall = (ko, x) => x * 100;
// var r2 = service.Calculate(5);  // Returns 500

// Reset -> back to user method
// knockOff.ICpPriorityService.Calculate.Reset();
// var r3 = service.Calculate(5);  // Returns 10
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

// knockOff.ICpResetRepository.GetUser.OnCall = (ko, id) => specialUser;
// service.GetUser(1);
// service.GetUser(2);

// knockOff.ICpResetRepository.GetUser.Reset();

// Assert.Equal(0, knockOff.ICpResetRepository.GetUser.CallCount);  // Tracking cleared
// Callback also cleared - now falls back to user method or default
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

// Test 1: Uses default
// Assert.Null(knockOff.AsCustCombinedRepository().GetById(999));

// Test 2: Override for specific IDs
// knockOff.ICpCombinedRepository.GetById.OnCall = (ko, id) => id == 1
//     ? new CpUser { Name = "Admin" }
//     : null;

// Test 3: Reset and different behavior
// knockOff.ICpCombinedRepository.GetById.Reset();
// knockOff.ICpCombinedRepository.GetById.OnCall = (ko, id) =>
//     new CpUser { Name = $"User-{id}" };
#endregion
