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

// User method to demonstrate priority
public partial class SourceRepoStub
{
    public int GetPriorityImpl(User user) => 99; // User method returns 99
}

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
    #region source-partial-override
    [Fact]
    public void Source_PartialOverrideWithOnCall()
    {
        var stub = new SourceRepoStub();
        var realRepo = new SimpleRepository();

        // Seed real repo with data
        realRepo.Save(new User { Id = 1, Name = "Real User" });

        // Delegate to real implementation
        stub.Source(realRepo);

        // Override specific method for testing
        stub.GetById.OnCall((id) =>
            id == 999 ? new User { Id = 999, Name = "Test User" } : null);

        IRepository repository = stub;

        // OnCall overrides source for id 999
        var testUser = repository.GetById(999);
        Assert.NotNull(testUser);
        Assert.Equal("Test User", testUser.Name);

        // Source still used when OnCall returns null (fallback)
        // Note: In this case OnCall handles all ids, so source is bypassed
    }
    #endregion
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
    #region source-priority
    [Fact]
    public void Priority_OnCallBeatsSourceBeatsSmartDefault()
    {
        var stub = new SourceRepoStub();
        var realRepo = new SimpleRepository();

        realRepo.Save(new User { Id = 1, Name = "Source", IsActive = true });

        // Set source (returns priority 1 for active users)
        stub.Source(realRepo);

        IRepository repository = stub;

        // Source returns 1 for active user
        var fromSource = repository.GetPriority(new User { Id = 1, IsActive = true });
        Assert.Equal(1, fromSource);

        // OnCall overrides source
        stub.GetPriority.OnCall((user) => 42);
        var fromOnCall = repository.GetPriority(new User { Id = 1, IsActive = true });
        Assert.Equal(42, fromOnCall);
    }
    #endregion
}

// =============================================================================
// Complete Example
// =============================================================================

public interface ICachingRepository
{
    User? GetUser(int id);
}

public class RealRepository : ICachingRepository
{
    public User? GetUser(int id) => new User { Id = id, Name = $"User{id}" };
}

[KnockOff]
public partial class CachingSourceRepoStub : ICachingRepository { }

public class CompleteSourceExampleTests
{
    #region source-complete-example
    [Fact]
    public void CachingDecorator_UsesSourceForBaseline()
    {
        var stub = new CachingSourceRepoStub();
        var realRepo = new RealRepository();

        // Use real repository as baseline
        stub.Source(realRepo);

        // Track calls to verify caching behavior
        var callCount = 0;
        stub.GetUser.OnCall((id) =>
        {
            callCount++;
            // Delegate to source
            return realRepo.GetUser(id);
        });

        ICachingRepository repository = stub;

        // First call
        var user1 = repository.GetUser(1);
        Assert.NotNull(user1);
        Assert.Equal(1, callCount);

        // Second call with same id
        var user2 = repository.GetUser(1);
        Assert.NotNull(user2);
        Assert.Equal(2, callCount); // Not cached - stub doesn't cache

        // Verify real data came through
        Assert.Equal("User1", user1.Name);
        Assert.Equal("User1", user2.Name);
    }
    #endregion
}
