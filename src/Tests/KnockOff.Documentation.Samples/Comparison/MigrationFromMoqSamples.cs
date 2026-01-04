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

#region docs:migration-from-moq:create-knockoff-class
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

#region docs:migration-from-moq:static-returns-user-method
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

#region docs:migration-from-moq:multiple-interfaces
[KnockOff]
public partial class MigDataContextKnockOff : IMigRepository, IMigUnitOfWork { }
#endregion

// ============================================================================
// Shared Stubs
// ============================================================================

public interface IMigSharedRepository
{
    MigUser? GetById(int id);
}

#region docs:migration-from-moq:shared-stubs
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

#region docs:migration-from-moq:argument-matching
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

#region docs:migration-from-moq:sequential-returns
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

#region docs:migration-from-moq:throwing-exceptions
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

#region docs:migration-from-moq:automatic-tracking
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

#region docs:migration-from-moq:complex-callbacks
[KnockOff]
public partial class MigSaveServiceKnockOff : IMigSaveService { }
#endregion
