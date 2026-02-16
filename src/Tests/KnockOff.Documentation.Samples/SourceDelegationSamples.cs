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
    [Fact]
    public void Source_DelegatesToRealImplementation()
    {
        var stub = new DataStoreStub();
        var realStore = new InMemoryDataStore();

        #region source-basic
        // Configure stub to delegate to real implementation
        stub.Source(realStore);
        #endregion

        IDataStore store = stub;

        // All calls delegate to the real implementation
        store.Add("first");
        store.Add("second");

        Assert.Equal(2, store.Count);
        Assert.Equal("first", store.Get(0));
        Assert.Equal("second", store.Get(1));
    }
}

// =============================================================================
// Partial Override with Source
// =============================================================================

public class PartialOverrideTests
{
    [Fact]
    public void Source_PartialOverrideWithReturn()
    {
        var stub = new SourceRepoStub();
        var realRepo = new SimpleRepository();

        // Seed real repo with data
        realRepo.Save(new User { Id = 1, Name = "Real User" });

        // Delegate to real implementation
        stub.Source(realRepo);

        #region source-partial-override
        // Override specific member while source handles the rest
        stub.GetById.Return((id) => new User { Id = id, Name = "Test User" });
        #endregion

        IRepository repository = stub;

        // GetById uses Return override
        var testUser = repository.GetById(1);
        Assert.NotNull(testUser);
        Assert.Equal("Test User", testUser.Name);

        // Save delegates to source (no Return configured)
        repository.Save(new User { Id = 2, Name = "New User" });
        Assert.NotNull(realRepo.GetById(2));
    }
}

// =============================================================================
// Interface Hierarchies
// =============================================================================

public class HierarchyTests
{
    [Fact]
    public void Source_AppliesAcrossInterfaceHierarchy()
    {
        var stub = new DataStoreStub();
        var realStore = new InMemoryDataStore();

        // Add items to real store
        realStore.Add("item1");
        realStore.Add("item2");

        #region source-hierarchy
        // Source applies to all interface hierarchy levels
        stub.Source(realStore);
        #endregion

        IDataStore store = stub;

        // All interface methods delegate to source
        Assert.Equal(2, store.Count);
        Assert.Equal("item1", store.Get(0));
    }
}

// =============================================================================
// Clearing Source
// =============================================================================

public class ClearSourceTests
{
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

        #region source-clear
        // Clear source to revert to smart defaults
        stub.Source(null);
        #endregion

        // Now smart defaults are used (Count returns 0)
        Assert.Equal(0, store.Count);
    }
}

// =============================================================================
// Priority Order
// =============================================================================

public class PriorityOrderTests
{
    [Fact]
    public void Priority_ReturnBeatsSourceBeatsSmartDefault()
    {
        var stub = new SourceRepoStub();
        var realRepo = new SimpleRepository();

        realRepo.Save(new User { Id = 1, Name = "Source", IsActive = true });

        // Set source (returns priority 1 for active users)
        stub.Source(realRepo);

        IRepository repository = stub;

        // Source returns 1 for active user (when no Return is set)
        var fromSource = repository.GetPriority(new User { Id = 1, IsActive = true });
        Assert.Equal(1, fromSource);

        #region source-priority
        // Return takes precedence over source
        stub.GetPriority.Return((user) => 42);
        #endregion
        var fromReturn = repository.GetPriority(new User { Id = 1, IsActive = true });
        Assert.Equal(42, fromReturn);
    }

    [Fact]
    public void ValueVsCallback_OverrideOverloads()
    {
        var stub = new SourceRepoStub();
        var realRepo = new SimpleRepository();
        stub.Source(realRepo);
        IRepository repository = stub;

        #region source-oncall-value-vs-callback
        // Value overload - simpler for fixed values
        stub.GetPriority.Return(99);

        // Callback overload - use when you need logic or side effects
        stub.GetPriority.Return((user) => user.IsActive ? 1 : 0);
        #endregion

        var result = repository.GetPriority(new User { IsActive = true });
        Assert.Equal(1, result);
    }

    [Fact]
    public void ReturnCallback_OverridesSource()
    {
        var stub = new SourceRepoStub();
        var realRepo = new SimpleRepository();
        stub.Source(realRepo);
        IRepository repository = stub;

        #region source-oncall-api-callback
        stub.GetById.Return((id) => new User { Id = id, Name = $"User{id}" });
        #endregion

        var user = repository.GetById(1);
        Assert.Equal("User1", user?.Name);
    }

    [Fact]
    public void ReturnValue_OverridesSource()
    {
        var stub = new SourceRepoStub();
        var realRepo = new SimpleRepository();
        stub.Source(realRepo);
        IRepository repository = stub;

        #region source-oncall-api-value
        stub.GetById.Return(new User { Id = 1, Name = "Fixed User" });
        #endregion

        var user = repository.GetById(99);
        Assert.Equal("Fixed User", user?.Name);
    }
}

// =============================================================================
// Interface Hierarchy - Partial Source Delegation
// =============================================================================

#region source-hierarchy-interface
public interface IStepList : IList<string>
{
    void AddRange(IEnumerable<string> items);
}
#endregion

[KnockOff]
public partial class StepListStub : IStepList { }

public class HierarchyPartialSourceTests
{
    [Fact]
    public void Source_PartialImplementation_DelegatesMatchingMembers()
    {
        var stub = new StepListStub();

        #region source-hierarchy-partial
        var realList = new List<string> { "step1", "step2", "step3" };

        // List<string> doesn't implement IStepList, but it does implement IList<string>
        // KnockOff delegates IList/ICollection/IEnumerable members to the real list
        stub.Source(realList);

        IStepList list = stub;

        // These work — delegated to List<string>
        Assert.Equal(3, list.Count);          // ICollection<T>.Count
        Assert.Equal("step1", list[0]);       // IList<T> indexer
        var items = new List<string>();
        foreach (var item in list)            // IEnumerable<T>
        {
            items.Add(item);
        }
        Assert.Equal(new[] { "step1", "step2", "step3" }, items);

        // AddRange is NOT delegated — it's on IStepList, which List<string> doesn't implement
        // Configure it explicitly, or it returns the smart default
        stub.AddRange.Call((newItems) =>
        {
            foreach (var newItem in newItems)
            {
                list.Add(newItem);
            }
        });
        #endregion

        list.AddRange(new[] { "step4" });
        Assert.Equal(4, list.Count);
    }

    [Fact]
    public void Source_PartialImplementation_NonMatchingMembersReturnDefaults()
    {
        var stub = new StepListStub();
        var realList = new List<string> { "a", "b", "c" };

        stub.Source(realList);

        IStepList list = stub;

        // IList<T> members work
        Assert.Equal(3, list.Count);

        // AddRange is IStepList-only — not delegated, does nothing (void default)
        list.AddRange(new[] { "d", "e" });
        Assert.Equal(3, list.Count); // Still 3 — AddRange was a no-op
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

// =============================================================================
// Source Delegation Reference Samples (for source-delegation.md)
// =============================================================================

public interface ISourceCalc
{
    int Add(int a, int b);
    int Subtract(int a, int b);
    int Divide(int a, int b);
}

public class RealCalc : ISourceCalc
{
    public int Add(int a, int b) => a + b;
    public int Subtract(int a, int b) => a - b;
    public int Divide(int a, int b) => b == 0 ? 0 : a / b;
}

[KnockOff]
public partial class SourceCalcStub : ISourceCalc { }

public class SourceRefBasicTests
{
    [Fact]
    public void Source_BasicDelegation()
    {
        #region source-delegation-ref-basic
        var stub = new SourceCalcStub();
        var realCalculator = new RealCalc();

        stub.Source(realCalculator);

        ISourceCalc calc = stub;

        // No methods configured -- all delegate to source
        var r1 = calc.Add(2, 3);      // Returns 5 (from real implementation)
        var r2 = calc.Subtract(10, 4); // Returns 6 (from real implementation)
        #endregion

        Assert.Equal(5, r1);
        Assert.Equal(6, r2);
    }

    [Fact]
    public void Source_PartialStubbing()
    {
        #region source-delegation-ref-partial
        var stub = new SourceCalcStub();
        stub.Source(new RealCalc());

        // Override just one method
        stub.Add.Return(999);

        ISourceCalc calc = stub;
        calc.Add(2, 3);      // 999 (stub configuration wins)
        calc.Subtract(10, 4); // 6 (delegates to source)
        #endregion

        Assert.Equal(999, calc.Add(0, 0));
    }

    [Fact]
    public void Source_RemoveWithNull()
    {
        var stub = new SourceCalcStub();
        var realCalculator = new RealCalc();

        #region source-delegation-ref-null
        stub.Source(realCalculator);
        ISourceCalc calc = stub;
        calc.Add(2, 3); // 5 (from source)

        stub.Source(null);
        calc.Add(2, 3); // 0 (default -- no source, no configuration)
        #endregion

        Assert.Equal(0, calc.Add(2, 3));
    }

    [Fact]
    public void Source_PriorityWithWhen()
    {
        var stub = new SourceCalcStub();
        var realCalculator = new RealCalc();

        #region source-delegation-ref-priority
        stub.Source(realCalculator);
        stub.Divide.When((10, 2)).Return(5);

        ISourceCalc calc = stub;
        calc.Divide(10, 2);  // 5 (When chain matched)
        calc.Divide(20, 4);  // 5 (falls to source -- real implementation)
        #endregion
    }
}

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
        // Return takes full control - source not consulted even for non-matches
        stub.Read.Return((filename) =>
            filename == "config.txt" ? "Test Config" : null);
        #endregion

        IDataSource dataSource = stub;

        // Return handles config.txt
        var config = dataSource.Read("config.txt");
        Assert.Equal("Test Config", config);

        // Return returned null for data.txt, but source is NOT consulted
        // once Return is configured - it takes full control
        var data = dataSource.Read("data.txt");
        Assert.Null(data);

        // Write delegates entirely to source (no Return configured)
        dataSource.Write("output.txt", "New Data");
        Assert.Equal("New Data", realDataSource.Read("output.txt"));
    }
}
