/// <summary>
/// Code samples for docs/guides/best-practices.md
///
/// Snippets in this file:
/// - docs:best-practices:stub-minimalism
/// - docs:best-practices:user-methods
/// - docs:best-practices:callbacks-test
/// - docs:best-practices:value-usage
/// - docs:best-practices:dynamic-onget
/// - docs:best-practices:reset-behavior
/// - docs:best-practices:out-param-correct
/// - docs:best-practices:method-overloads
/// - docs:best-practices:partial-container
/// - docs:best-practices:standalone-stub
/// - docs:best-practices:inline-stubs
///
/// Corresponding tests: BestPracticesSamplesTests.cs
/// </summary>

using KnockOff.Documentation.Samples.SampleDomain;

namespace KnockOff.Documentation.Samples.Guides;

// ============================================================================
// Stub Minimalism - Only define what you need
// ============================================================================

public interface IBpUserService
{
    User? GetUser(int id);
    int GetCount();
    List<User> GetUsers();
    void Save(User user);
}

#region best-practices-stub-minimalism
[KnockOff]
public partial class BpUserServiceKnockOff : IBpUserService
{
    // Only define methods needing custom behavior
    protected User? GetUser(int id) => new User { Id = id };
    // GetCount() returns 0, GetUsers() returns empty list, etc.
}
#endregion

// ============================================================================
// User Methods - Shared default behavior
// ============================================================================

public interface IBpRepository
{
    User? GetById(int id);
}

#region best-practices-user-methods
[KnockOff]
public partial class BpRepositoryKnockOff : IBpRepository
{
    // Default for ALL tests using this stub
    protected User? GetById(int id) => new User { Id = id, Name = "Test User" };
}
#endregion

// ============================================================================
// Callbacks Test - Per-test customization (separate interface to avoid suffix)
// ============================================================================

public interface IBpCallbackRepo
{
    User? GetById(int id);
}

[KnockOff]
public partial class BpCallbackRepoKnockOff : IBpCallbackRepo { }

#region best-practices-callbacks-test
public class BpCallbacksExample
{
    public void ReturnsAdmin_WhenIdIs1()
    {
        var knockOff = new BpCallbackRepoKnockOff();
        IBpCallbackRepo repo = knockOff;

        // Override just for this test
        knockOff.GetById.OnCall = (ko, id) => id == 1
            ? new User { Id = 1, Name = "Admin" }
            : null;

        // Use through interface
        var admin = repo.GetById(1);
        var other = repo.GetById(2);
    }
}
#endregion

// ============================================================================
// Property Value Usage - Static data
// ============================================================================

public interface IBpPropertyService
{
    string Name { get; set; }
    bool IsActive { get; set; }
}

[KnockOff]
public partial class BpPropertyServiceKnockOff : IBpPropertyService { }

#region best-practices-value-usage
public class BpValueUsageExample
{
    public void ConfigureWithValue()
    {
        var knockOff = new BpPropertyServiceKnockOff();

        // GOOD - simple and clear
        knockOff.Name.Value = "John Doe";
        knockOff.IsActive.Value = true;
    }
}
#endregion

// ============================================================================
// Dynamic OnGet - Computed values
// ============================================================================

public interface IBpDynamicService
{
    string RequestId { get; }
    bool IsReady { get; }
    void Initialize();
}

[KnockOff]
public partial class BpDynamicServiceKnockOff : IBpDynamicService { }

#region best-practices-dynamic-onget
public class BpDynamicOnGetExample
{
    public void ConfigureWithOnGet()
    {
        var knockOff = new BpDynamicServiceKnockOff();
        IBpDynamicService service = knockOff;

        // Different value each call
        var counter = 0;
        knockOff.RequestId.OnGet = (ko) => $"REQ-{++counter}";

        // Depends on stub state
        knockOff.IsReady.OnGet = (ko) => ko.Initialize.WasCalled;
    }
}
#endregion

// ============================================================================
// Reset Behavior - Tracking cleared, Value preserved
// ============================================================================

public interface IBpResetService
{
    string Name { get; set; }
}

[KnockOff]
public partial class BpResetServiceKnockOff : IBpResetService { }

#region best-practices-reset-behavior
public class BpResetBehaviorExample
{
    public void ResetPreservesValue()
    {
        var knockOff = new BpResetServiceKnockOff();
        IBpResetService service = knockOff;

        knockOff.Name.Value = "John";
        service.Name = "Jane";  // SetCount = 1

        knockOff.Name.Reset();

        // After reset:
        // Assert.Equal(0, knockOff.Name.SetCount);  // Tracking cleared
        // Assert.Null(knockOff.Name.OnGet);         // Callback cleared
        // Assert.Equal("Jane", knockOff.Name.Value); // Value preserved!
    }
}
#endregion

// ============================================================================
// Out Parameter Handling
// ============================================================================

public interface IBpParser
{
    bool TryParse(string input, out int result);
}

[KnockOff]
public partial class BpParserKnockOff : IBpParser { }

#region best-practices-out-param-correct
public class BpOutParamExample
{
    public void ConfigureOutParam()
    {
        var knockOff = new BpParserKnockOff();

        // CORRECT - explicit delegate type
        knockOff.TryParse.OnCall =
            (BpParserKnockOff.TryParseInterceptor.TryParseDelegate)((BpParserKnockOff ko, string input, out int result) =>
            {
                return int.TryParse(input, out result);
            });
    }
}
#endregion

// ============================================================================
// Method Overloads - Numeric suffixes
// ============================================================================

#region best-practices-method-overloads
public interface IBpProcessor
{
    void Process(string data);
    void Process(string data, int priority);
}

[KnockOff]
public partial class BpProcessorKnockOff : IBpProcessor { }

public class BpOverloadsExample
{
    public void AccessOverloads()
    {
        var knockOff = new BpProcessorKnockOff();

        // Generated interceptors:
        // knockOff.Process1.CallCount;  // Process(string)
        // knockOff.Process2.CallCount;  // Process(string, int)
    }
}
#endregion

// ============================================================================
// Partial Container - Nested classes
// ============================================================================

public interface IBpService
{
    void DoWork();
}

#region best-practices-partial-container
public partial class BpMyTests  // <-- partial required
{
    [KnockOff]
    public partial class BpServiceKnockOff : IBpService { }
}
#endregion

// ============================================================================
// Stand-Alone Stub Pattern
// ============================================================================

public interface IBpUserRepository
{
    User? GetById(int id);
}

#region best-practices-standalone-stub
[KnockOff]
public partial class BpUserRepositoryKnockOff : IBpUserRepository
{
    protected User? GetById(int id) => new User { Id = id };
}

// All test classes use the same stub
public class BpUserServiceTests
{
    private readonly BpUserRepositoryKnockOff _repoKnockOff = new();
}
#endregion

// ============================================================================
// Inline Stubs Pattern
// ============================================================================

public interface IBpInlineUserService
{
    User? GetUser(int id);
}

public interface IBpInlineLogger
{
    void Log(string message);
}

#region best-practices-inline-stubs
[KnockOff<IBpInlineUserService>]
[KnockOff<IBpInlineLogger>]
public partial class BpInlineTests
{
    public void Test()
    {
        var userStub = new Stubs.IBpInlineUserService();
        var loggerStub = new Stubs.IBpInlineLogger();
        // Configure via callbacks only
    }
}
#endregion
