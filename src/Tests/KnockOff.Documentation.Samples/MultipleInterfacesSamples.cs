using KnockOff;

namespace KnockOff.Documentation.Samples.MultipleInterfaces;

// =============================================================================
// Interface definitions for multiple interfaces samples
// =============================================================================

#region multi-hierarchy-interfaces
public interface IReadableStore
{
    string? GetById(int id);
    int Count { get; }
}

public interface IStore : IReadableStore
{
    void Save(int id, string value);
    void Delete(int id);
}
#endregion

#region multi-unrelated-interfaces
public interface IRepository
{
    User? GetUser(int id);
    void Save(User user);
}

public interface IUnitOfWork
{
    void Commit();
    void Rollback();
}
#endregion

// =============================================================================
// ReadOnlyStore for Source delegation example
// =============================================================================

public class ReadOnlyStore : IReadableStore
{
    private readonly Dictionary<int, string> _data = new();

    public ReadOnlyStore(Dictionary<int, string> data) => _data = data;

    public string? GetById(int id) => _data.GetValueOrDefault(id);
    public int Count => _data.Count;
}

// =============================================================================
// Simple service for unrelated interfaces example
// =============================================================================

public class MyService
{
    private readonly IRepository _repo;
    private readonly IUnitOfWork _uow;

    public MyService(IRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public void UpdateUser(int id)
    {
        var user = _repo.GetUser(id);
        if (user != null)
        {
            _repo.Save(user);
            _uow.Commit();
        }
    }
}

// =============================================================================
// Inline stubs
// =============================================================================

[KnockOff<IStore>]
public partial class HierarchyTests { }

public partial class HierarchyTests
{
    [Fact]
    public void InterfaceHierarchy_UnifiedInterceptors()
    {
        #region multi-hierarchy-unified
        var stub = new Stubs.IStore();

        // All members from IStore AND IReadableStore — same flat API
        stub.GetById.Call((id) => $"item-{id}");  // from IReadableStore
        stub.Count.Get(42);                           // from IReadableStore
        stub.Save.Call((int id, string value) => { });           // from IStore
        stub.Delete.Call((id) => { });                // from IStore

        IStore store = stub;
        Assert.Equal("item-1", store.GetById(1));
        Assert.Equal(42, store.Count);
        #endregion
    }

    [Fact]
    public void Source_DelegationPerLevel()
    {
        #region multi-source-per-level
        var stub = new Stubs.IStore();
        var readOnlySource = new ReadOnlyStore(
            new Dictionary<int, string> { [1] = "value-1" });

        // Only delegates GetById and Count — Save and Delete remain unconfigured
        stub.Source(readOnlySource);
        #endregion

        IStore store = stub;
        Assert.Equal("value-1", store.GetById(1));
        Assert.Equal(1, store.Count);
    }
}

[KnockOff<IRepository>]
[KnockOff<IUnitOfWork>]
public partial class UnrelatedInterfacesTests { }

public partial class UnrelatedInterfacesTests
{
    [Fact]
    public void SeparateStubs_SameTestClass()
    {
        #region multi-unrelated-stubs
        var repo = new Stubs.IRepository();
        var uow = new Stubs.IUnitOfWork();

        repo.GetUser.Call((id) => new User { Id = id });
        uow.Commit.Call(() => { }).Verifiable();

        // Pass both to the system under test
        var service = new MyService(repo, uow);
        service.UpdateUser(1);

        uow.Verify();
        #endregion
    }
}
