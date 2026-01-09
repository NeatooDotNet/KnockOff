/// <summary>
/// Code samples for ~/.claude/skills/knockoff/migrations.md
///
/// Snippets in this file:
/// - skill:migrations:v10-9-after
/// - skill:migrations:v10-8-works-same
/// - skill:migrations:v10-7-works-same
/// - skill:migrations:v10-7-callback-capture
///
/// Note: "Before" examples showing old API are kept inline in migrations.md
/// since they won't compile with the current KnockOff version.
///
/// Corresponding tests: MigrationSamplesTests.cs
/// </summary>

namespace KnockOff.Documentation.Samples.Skills;

// ============================================================================
// Domain Types for Migration Samples
// ============================================================================

public class MgUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// ============================================================================
// v10.9.0 - Class Stub Composition Pattern
// ============================================================================

public class MgUserService
{
    public virtual MgUser? GetUser(int id) => null;
    public virtual string Name { get; set; } = "default";
}

#region skill:migrations:v10-9-after
[KnockOff<MgUserService>]
public partial class MgClassStubTests
{
    // Generates: Stubs.MgUserService
}

// var stub = new MgClassStubTests.Stubs.MgUserService();
// stub.GetUser.OnCall = (ko, id) => new MgUser { Id = id };  // Direct access!
// MgUserService service = stub.Object;  // Use .Object for type
#endregion

// ============================================================================
// v10.8.0 - Handler → Interceptor Rename
// ============================================================================

public interface IMgUserService
{
    MgUser? GetUser(int id);
    string Name { get; set; }
}

public interface IMgParser
{
    bool TryParse(string input, out int result);
}

#region skill:migrations:v10-8-works-same
[KnockOff]
public partial class MgUserServiceKnockOff : IMgUserService { }

// Works the same
// knockOff.GetUser.CallCount
// knockOff.GetUser.OnCall = (ko, id) => new MgUser();
#endregion

// ============================================================================
// v10.7.0 - Spy → KO Rename
// ============================================================================

public interface IMgLogger
{
    void Log(string message);
}

#region skill:migrations:v10-7-works-same
[KnockOff]
public partial class MgLoggerKnockOff : IMgLogger { }

// Works the same
// knockOff.Log.CallCount
// knockOff.Name.OnGet = (ko) => "Test";
#endregion

// ============================================================================
// v10.7.0 - Callback Capture Workaround
// ============================================================================

public interface IMgService
{
    MgUser? GetUser(int id);
}

#region skill:migrations:v10-7-callback-capture
[KnockOff]
public partial class MgServiceKnockOff : IMgService { }

// Before
// var allCalls = knockOff.GetUser.AllCalls;
// Assert.Equal(3, allCalls.Count);

// After - use count and last value
// Assert.Equal(3, knockOff.GetUser.CallCount);
// Assert.Equal(42, knockOff.GetUser.LastCallArg);

// Or capture in callback if you need full history
// var allCalls = new List<int>();
// knockOff.GetUser.OnCall = (ko, id) =>
// {
//     allCalls.Add(id);
//     return new MgUser { Id = id };
// };
#endregion
