namespace KnockOff.Documentation.Samples.SourceDelegation;

// =============================================================================
// Interfaces for Source Delegation Samples
// =============================================================================

public interface IDataStore
{
    int Count { get; }
    void Add(string item);
    string? Get(int index);
    void Clear();
}

public interface IRepository
{
    User? GetById(int id);
    void Save(User user);
    int GetPriority(User user);
}

// =============================================================================
// Simple in-memory implementation for delegation
// =============================================================================

public class InMemoryDataStore : IDataStore
{
    private readonly List<string> _items = new();

    public int Count => _items.Count;

    public void Add(string item) => _items.Add(item);

    public string? Get(int index) =>
        index >= 0 && index < _items.Count ? _items[index] : null;

    public void Clear() => _items.Clear();
}

public class SimpleRepository : IRepository
{
    private readonly Dictionary<int, User> _users = new();

    public User? GetById(int id) => _users.GetValueOrDefault(id);

    public void Save(User user) => _users[user.Id] = user;

    public int GetPriority(User user) => user.IsActive ? 1 : 0;
}

// =============================================================================
// Stubs for Source Delegation Samples
// =============================================================================

[KnockOff]
public partial class DataStoreStub : IDataStore { }

[KnockOff]
public partial class SourceRepoStub : IRepository { }

// =============================================================================
// Basic Source Delegation
// =============================================================================

public class BasicSourceDelegationTests
{
    #region source-basic
    [Fact]
    public void Source_DelegatesToRealImplementation()
    {
        var stub = new DataStoreStub();
        var realStore = new InMemoryDataStore();

        // Configure stub to delegate to real implementation
        stub.Source(realStore);

        IDataStore store = stub;

        // All calls delegate to the real implementation
        store.Add("first");
        store.Add("second");

        Assert.Equal(2, store.Count);
        Assert.Equal("first", store.Get(0));
        Assert.Equal("second", store.Get(1));
    }
    #endregion
}

// =============================================================================
// Partial Override with Source
// =============================================================================

public class PartialOverrideTests
{
    [Fact]
    public void Source_PartialOverrideWithOnCall()
    {
        var stub = new SourceRepoStub();
        var realRepo = new SimpleRepository();

        // Seed real repo with data
        realRepo.Save(new User { Id = 1, Name = "Real User" });

        // Delegate to real implementation
        stub.Source(realRepo);

        #region source-partial-override
        // Override GetById for test data
        stub.GetById.OnCall((id) => new User { Id = id, Name = "Test User" });

        IRepository repository = stub;

        // GetById uses OnCall override
        var testUser = repository.GetById(1);
        Assert.NotNull(testUser);
        Assert.Equal("Test User", testUser.Name);

        // Save delegates to source (no OnCall configured)
        repository.Save(new User { Id = 2, Name = "New User" });
        Assert.NotNull(realRepo.GetById(2));
        #endregion
    }
}

// =============================================================================
// Interface Hierarchies
// =============================================================================

public class HierarchyTests
{
    #region source-hierarchy
    [Fact]
    public void Source_AppliesAcrossInterfaceHierarchy()
    {
        var stub = new DataStoreStub();
        var realStore = new InMemoryDataStore();

        // Add items to real store
        realStore.Add("item1");
        realStore.Add("item2");

        // Delegate to real implementation
        stub.Source(realStore);

        IDataStore store = stub;

        // All interface methods delegate to source
        Assert.Equal(2, store.Count);
        Assert.Equal("item1", store.Get(0));
    }
    #endregion
}

// =============================================================================
// Clearing Source
// =============================================================================

public class ClearSourceTests
{
    #region source-clear
    [Fact]
    public void Source_CanBeClearedWithNull()
    {
        var stub = new DataStoreStub();
        var realStore = new InMemoryDataStore();

        realStore.Add("item");
        stub.Source(realStore);

        IDataStore store = stub;

        // Source is active
        Assert.Equal(1, store.Count);

        // Clear source
        stub.Source(null);

        // Now smart defaults are used (Count returns 0)
        Assert.Equal(0, store.Count);
    }
    #endregion
}

// =============================================================================
// Priority Order
// =============================================================================

public class PriorityOrderTests
{
    [Fact]
    public void Priority_OnCallBeatsSourceBeatsSmartDefault()
    {
        var stub = new SourceRepoStub();
        var realRepo = new SimpleRepository();

        realRepo.Save(new User { Id = 1, Name = "Source", IsActive = true });

        // Set source (returns priority 1 for active users)
        stub.Source(realRepo);

        IRepository repository = stub;

        #region source-priority
        // Source returns 1 for active user (when no OnCall is set)
        var fromSource = repository.GetPriority(new User { Id = 1, IsActive = true });
        Assert.Equal(1, fromSource);

        // OnCall overrides source
        stub.GetPriority.OnCall((user) => 42);
        var fromOnCall = repository.GetPriority(new User { Id = 1, IsActive = true });
        Assert.Equal(42, fromOnCall);
        #endregion
    }
}

// =============================================================================
// Complete Example - Decorator Pattern Testing
// =============================================================================

public interface IDataSource
{
    string? Read(string filename);
    void Write(string filename, string content);
}

public class FileDataSource : IDataSource
{
    private readonly Dictionary<string, string> _data = new();

    public string? Read(string filename) => _data.GetValueOrDefault(filename);
    public void Write(string filename, string content) => _data[filename] = content;
}

[KnockOff]
public partial class DataSourceStub : IDataSource { }

public class CompleteSourceExampleTests
{
    [Fact]
    public void Decorator_UsesSourceWithSelectiveOverrides()
    {
        var stub = new DataSourceStub();
        var realDataSource = new FileDataSource();

        // Populate real data source
        realDataSource.Write("config.txt", "Production Config");
        realDataSource.Write("data.txt", "Production Data");

        // Delegate to real implementation
        stub.Source(realDataSource);

        #region source-complete-example
        // Override Read for specific test scenario
        stub.Read.OnCall((filename) =>
            filename == "config.txt" ? "Test Config" : null);

        IDataSource dataSource = stub;

        // OnCall handles config.txt
        var config = dataSource.Read("config.txt");
        Assert.Equal("Test Config", config);

        // OnCall returned null for data.txt, but source is NOT consulted
        // once OnCall is configured - it takes full control
        var data = dataSource.Read("data.txt");
        Assert.Null(data);

        // Write delegates entirely to source (no OnCall configured)
        dataSource.Write("output.txt", "New Data");
        Assert.Equal("New Data", realDataSource.Read("output.txt"));
        #endregion
    }
}
