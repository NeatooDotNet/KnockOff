/// <summary>
/// Code samples for docs/guides/multiple-interfaces.md
///
/// Snippets in this file:
/// - multiple-interfaces-separate-standalone-stubs
/// - multiple-interfaces-inline-stubs-multiple
/// - multiple-interfaces-migration-from-multiple
///
/// Corresponding tests: MultipleInterfacesSamplesTests.cs
/// </summary>

namespace KnockOff.Documentation.Samples.Guides;

// ============================================================================
// Domain Types
// ============================================================================

public class MultiUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// ============================================================================
// Interfaces
// ============================================================================

public interface IMultiRepository
{
    MultiUser? GetById(int id);
    void Add(MultiUser user);
}

public interface IMultiUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

// ============================================================================
// Separate Standalone Stubs
// ============================================================================

#region multiple-interfaces-separate-standalone-stubs
[KnockOff]
public partial class MultiRepositoryKnockOff : IMultiRepository { }

[KnockOff]
public partial class MultiUnitOfWorkKnockOff : IMultiUnitOfWork { }
#endregion

// ============================================================================
// Inline Stubs on Test Class
// ============================================================================

#region multiple-interfaces-inline-stubs-multiple
[KnockOff<IMultiRepository>]
[KnockOff<IMultiUnitOfWork>]
public partial class MultiDataContextTests
{
    public void SaveChanges_ReturnsAddCount_Example()
    {
        var repo = new Stubs.IMultiRepository();
        var uow = new Stubs.IMultiUnitOfWork();

        // Configure via flat API
        uow.SaveChangesAsync.OnCall = (ko, ct) => Task.FromResult(repo.Add.CallCount);

        repo.Add.OnCall = (ko, user) => { };

        // Use in test
        IMultiRepository repoService = repo;
        IMultiUnitOfWork uowService = uow;

        repoService.Add(new MultiUser { Name = "New" });
        repoService.Add(new MultiUser { Name = "Another" });
        // var saved = await uowService.SaveChangesAsync();
        // Assert.Equal(2, saved);
    }
}
#endregion

// ============================================================================
// Usage Examples
// ============================================================================

/// <summary>
/// Usage examples demonstrating multiple interface patterns.
/// </summary>
public static class MultipleInterfacesUsageExamples
{
    public static void SeparateStubsUsage()
    {
        #region multiple-interfaces-separate-stubs-usage
        // Usage
        var repo = new MultiRepositoryKnockOff();
        var uow = new MultiUnitOfWorkKnockOff();

        // Configure each independently
        repo.GetById.OnCall = (ko, id) => new MultiUser { Id = id };
        uow.SaveChangesAsync.OnCall = (ko, ct) => Task.FromResult(1);
        #endregion
    }

    public static void MigrationFromMultiple()
    {
        #region multiple-interfaces-migration-from-multiple
        // v10.9 - Option A: Separate stubs
        var repo = new MultiRepositoryKnockOff();
        var uow = new MultiUnitOfWorkKnockOff();

        // Configure independently
        repo.GetById.OnCall = (ko, id) => new MultiUser { Id = id };
        uow.SaveChangesAsync.OnCall = (ko, ct) => Task.FromResult(1);

        // v10.9 - Option B: Inline stubs on test class
        // Use [KnockOff<IMultiRepository>] and [KnockOff<IMultiUnitOfWork>]
        // on your test class (see MultiDataContextTests)
        #endregion
    }
}
