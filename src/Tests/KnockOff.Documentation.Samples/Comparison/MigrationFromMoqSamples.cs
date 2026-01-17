/// <summary>
/// Code samples for docs/migration-from-moq.md
///
/// Snippets in this file:
/// - docs:migration-from-moq:create-knockoff-class
/// - docs:migration-from-moq:replace-mock-object
/// - docs:migration-from-moq:convert-setup-returns
/// - docs:migration-from-moq:convert-async-returns
/// - docs:migration-from-moq:convert-verification
/// - docs:migration-from-moq:convert-callback
/// - docs:migration-from-moq:convert-property-setup
/// - docs:migration-from-moq:static-returns-user-method
/// - docs:migration-from-moq:static-returns-callback
/// - docs:migration-from-moq:conditional-returns
/// - docs:migration-from-moq:throwing-exceptions
/// - docs:migration-from-moq:sequential-returns
/// - docs:migration-from-moq:multiple-interfaces
/// - docs:migration-from-moq:argument-matching
/// - docs:migration-from-moq:shared-stubs
/// - docs:migration-from-moq:complex-callbacks
/// - docs:migration-from-moq:automatic-tracking
///
/// Corresponding tests: MigrationFromMoqSamplesTests.cs
/// </summary>

namespace KnockOff.Documentation.Samples.Comparison;

// ============================================================================
// Domain Types for Migration
// ============================================================================

public class MigUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class MigConfig
{
    public int Timeout { get; set; }
}

public class MigEntity
{
    public int Id { get; set; }
}

// ============================================================================
// Step 1: Create KnockOff Class
// ============================================================================

public interface IMigUserService
{
    MigUser GetUser(int id);
    Task<MigUser?> GetUserAsync(int id);
    void Save(MigUser user);
    void Delete(int id);
    IEnumerable<MigUser> GetAll();
    void Update(MigUser user);
    string Name { get; set; }
}

#region migration-from-moq-create-knockoff-class
[KnockOff]
public partial class MigUserServiceKnockOff : IMigUserService { }
#endregion

// ============================================================================
// Static Returns - User Method
// ============================================================================

public interface IMigConfigService
{
    MigConfig GetConfig();
}

#region migration-from-moq-static-returns-user-method
[KnockOff]
public partial class MigConfigServiceKnockOff : IMigConfigService
{
    protected MigConfig GetConfig() => new MigConfig { Timeout = 30 };
}
#endregion

// ============================================================================
// Multiple Interfaces
// ============================================================================

public interface IMigRepository
{
    MigEntity? GetById(int id);
}

public interface IMigUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

// Note: Multi-interface standalone stubs are no longer supported (KO0010).
// Use inline stubs [KnockOff<T>] or separate single-interface stubs.
#region migration-from-moq-multiple-interfaces
[KnockOff]
public partial class MigRepositoryKnockOff : IMigRepository { }

[KnockOff]
public partial class MigUnitOfWorkKnockOff : IMigUnitOfWork { }
#endregion

// ============================================================================
// Shared Stubs
// ============================================================================

public interface IMigSharedRepository
{
    MigUser? GetById(int id);
}

#region migration-from-moq-shared-stubs
[KnockOff]
public partial class MigSharedRepositoryKnockOff : IMigSharedRepository
{
    // Test data shared across tests
    private static readonly List<MigUser> TestUsers = new()
    {
        new MigUser { Id = 1, Name = "Admin" },
        new MigUser { Id = 2, Name = "Guest" }
    };

    protected MigUser? GetById(int id) => TestUsers.FirstOrDefault(u => u.Id == id);
}
#endregion

// ============================================================================
// Logging Service for Argument Matching
// ============================================================================

public interface IMigLogger
{
    void Log(string message);
}

#region migration-from-moq-argument-matching
[KnockOff]
public partial class MigLoggerKnockOff : IMigLogger { }
#endregion

// ============================================================================
// Sequence Service
// ============================================================================

public interface IMigSequence
{
    int GetNext();
}

#region migration-from-moq-sequential-returns
[KnockOff]
public partial class MigSequenceKnockOff : IMigSequence { }
#endregion

// ============================================================================
// Connection Service (for throwing exceptions)
// ============================================================================

public interface IMigConnection
{
    void Connect();
}

#region migration-from-moq-throwing-exceptions
[KnockOff]
public partial class MigConnectionKnockOff : IMigConnection { }
#endregion

// ============================================================================
// Process Service (for automatic tracking)
// ============================================================================

public interface IMigProcessor
{
    void Process(string data);
}

#region migration-from-moq-automatic-tracking
[KnockOff]
public partial class MigProcessorKnockOff : IMigProcessor { }
#endregion

// ============================================================================
// Save Service (for complex callbacks)
// ============================================================================

public interface IMigSaveService
{
    void Save(MigEntity entity);
}

#region migration-from-moq-complex-callbacks
[KnockOff]
public partial class MigSaveServiceKnockOff : IMigSaveService { }
#endregion

// ============================================================================
// Comment-based usage examples
// ============================================================================

#region migration-from-moq-instantiate-knockoff
// var knockOff = new MigUserServiceKnockOff();
#endregion

#region migration-from-moq-replace-mock-object
// IMigUserService service = knockOff;
// // or
// DoSomething(knockOff.AsIMigUserService());
#endregion

#region migration-from-moq-convert-setup-returns
// knockOff.GetUser.OnCall((ko, id) =>
//     new User { Id = id, Name = "Test" });
#endregion

#region migration-from-moq-convert-async-returns
// knockOff.GetUserAsync.OnCall((ko, id) =>
//     Task.FromResult<User?>(new User { Id = id }));
#endregion

#region migration-from-moq-convert-verification
// Assert.Equal(1, knockOff.Save.CallCount);
// Assert.Equal(0, knockOff.Delete.CallCount);
// Assert.True(knockOff.GetAll.WasCalled);
// Assert.Equal(3, knockOff.Update.CallCount);
#endregion

#region migration-from-moq-convert-callback
// // Arguments are captured automatically via tracking object
// var tracking = knockOff.Save.OnCall((ko, user) =>
// {
//     customList.Add(user);
// });
// var captured = tracking.LastArg;
#endregion

#region migration-from-moq-convert-property-setup
// knockOff.Name.OnGet = (ko) => "Test";
//
// // Setter tracking is automatic
// service.Name = "Value";
// Assert.Equal("Value", knockOff.Name.LastSetValue);
#endregion

#region migration-from-moq-static-returns-callback
// knockOff.GetConfig.OnCall((ko) => new MigConfig { Timeout = 30 });
#endregion

#region migration-from-moq-conditional-returns
// knockOff.GetUser.OnCall((ko, id) => id switch
// {
//     1 => new User { Name = "Admin" },
//     2 => new User { Name = "Guest" },
//     _ => null
// });
#endregion

#region migration-from-moq-throwing-exceptions-usage
// knockOff.Connect.OnCall((ko) =>
//     throw new TimeoutException());
#endregion

#region migration-from-moq-sequential-returns-usage
// var values = new Queue<int>([1, 2, 3]);
// knockOff.GetNext.OnCall((ko) => values.Dequeue());
#endregion

#region migration-from-moq-multiple-interfaces-usage
// // Separate stubs for each interface
// var repoKnockOff = new MigRepositoryKnockOff();
// IMigRepository repo = repoKnockOff.AsIMigRepository();
//
// var uowKnockOff = new MigUnitOfWorkKnockOff();
// uowKnockOff.SaveChangesAsync.OnCall((ko, ct) => Task.FromResult(1));
// IMigUnitOfWork uow = uowKnockOff.AsIMigUnitOfWork();
#endregion

#region migration-from-moq-argument-matching-usage
// knockOff.Log.OnCall((ko, message) =>
// {
//     if (message.Contains("error"))
//         errors.Add(message);
// });
#endregion

#region migration-from-moq-simple-verification
// // These translate directly
// Assert.True(knockOff.Method.WasCalled);
// Assert.Equal(expectedCount, knockOff.Method.CallCount);
#endregion

#region migration-from-moq-complex-callbacks-usage
// knockOff.Save.OnCall((ko, entity) =>
// {
//     entity.Id = nextId++;
//     savedEntities.Add(entity);
// });
#endregion

#region migration-from-moq-automatic-tracking-usage
// // No setup needed - just call the method
// service.Process("data");
//
// // Args are captured
// Assert.Equal("data", knockOff.Process.LastCallArg);
#endregion
