/// <summary>
/// Code samples for docs/knockoff-vs-moq.md
///
/// Snippets in this file:
/// - docs:knockoff-vs-moq:basic-stub
/// - docs:knockoff-vs-moq:basic-verification
/// - docs:knockoff-vs-moq:property-stub
/// - docs:knockoff-vs-moq:async-stub
/// - docs:knockoff-vs-moq:argument-capture
/// - docs:knockoff-vs-moq:multiple-interfaces
/// - docs:knockoff-vs-moq:indexer-stub
/// - docs:knockoff-vs-moq:event-stub
/// - docs:knockoff-vs-moq:verification-patterns
/// - docs:knockoff-vs-moq:sequential-returns
/// - docs:knockoff-vs-moq:per-test-override
/// - docs:knockoff-vs-moq:reset-and-reuse
///
/// Corresponding tests: KnockOffVsMoqSamplesTests.cs
/// </summary>

namespace KnockOff.Documentation.Samples.Comparison;

// ============================================================================
// Domain Types for Comparison
// ============================================================================

public class VsUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class VsEntity
{
    public int Id { get; set; }
}

public class VsPropertyInfo
{
    public object? Value { get; set; }
}

// ============================================================================
// Basic Setup and Verification
// ============================================================================

public interface IVsUserService
{
    VsUser GetUser(int id);
    VsUser? CurrentUser { get; set; }
    VsUser? Save(VsUser user);
    void Delete(int id);
    IEnumerable<VsUser> GetAll();
    void Update(VsUser user);
}

#region docs:knockoff-vs-moq:basic-stub
[KnockOff]
public partial class VsUserServiceKnockOff : IVsUserService
{
    protected VsUser GetUser(int id) => new VsUser { Id = id, Name = "Test User" };
}
#endregion

// ============================================================================
// Async Methods
// ============================================================================

public interface IVsRepository
{
    Task<VsEntity?> GetByIdAsync(int id);
    void Save(VsEntity entity);
}

#region docs:knockoff-vs-moq:async-stub
[KnockOff]
public partial class VsRepositoryKnockOff : IVsRepository
{
    protected Task<VsEntity?> GetByIdAsync(int id) =>
        Task.FromResult<VsEntity?>(new VsEntity { Id = id });
}
#endregion

// ============================================================================
// Multiple Interface Implementation
// ============================================================================

public interface IVsEmployeeRepository
{
    VsUser? GetEmployee(int id);
}

public interface IVsUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

#region docs:knockoff-vs-moq:multiple-interfaces
[KnockOff]
public partial class VsEmployeeRepoKnockOff : IVsEmployeeRepository, IVsUnitOfWork
{
    protected Task<int> SaveChangesAsync(CancellationToken ct) => Task.FromResult(1);
}
#endregion

// ============================================================================
// Indexer Mocking
// ============================================================================

public interface IVsPropertyStore
{
    VsPropertyInfo? this[string key] { get; set; }
}

#region docs:knockoff-vs-moq:indexer-stub
[KnockOff]
public partial class VsPropertyStoreKnockOff : IVsPropertyStore { }
#endregion

// ============================================================================
// Event Mocking
// ============================================================================

public interface IVsEventSource
{
    event EventHandler<string>? DataReceived;
}

#region docs:knockoff-vs-moq:event-stub
[KnockOff]
public partial class VsEventSourceKnockOff : IVsEventSource { }
#endregion

// ============================================================================
// Verification Patterns
// ============================================================================

public interface IVsVerificationRepository
{
    void Save(VsEntity entity);
    void Delete(int id);
    IEnumerable<VsEntity> GetAll();
    void Update(VsEntity entity);
}

#region docs:knockoff-vs-moq:verification-patterns
[KnockOff]
public partial class VsVerificationRepositoryKnockOff : IVsVerificationRepository { }
#endregion

// ============================================================================
// Sequential Returns
// ============================================================================

public interface IVsSequence
{
    int GetNext();
}

#region docs:knockoff-vs-moq:sequential-returns
[KnockOff]
public partial class VsSequenceKnockOff : IVsSequence { }
#endregion

// ============================================================================
// Per-Test Override
// ============================================================================

public interface IVsOverrideService
{
    VsUser GetUser(int id);
}

#region docs:knockoff-vs-moq:per-test-override
[KnockOff]
public partial class VsOverrideServiceKnockOff : IVsOverrideService
{
    // Default behavior for most tests
    protected VsUser GetUser(int id) => new VsUser { Id = id, Name = "Default" };
}
#endregion
