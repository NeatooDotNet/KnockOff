// -----------------------------------------------------------------------------
// Design.Tests - Source Delegation Tests
// -----------------------------------------------------------------------------

using Design.Domain.Services;
using Design.Stubs.Advanced;

namespace Design.Tests.AdvancedTests;

/// <summary>
/// Tests for Source() delegation to real implementations.
/// </summary>
public class SourceDelegationTests
{
    [Fact]
    public void Source_DelegatesUnconfiguredMethods()
    {
        var stub = new SourceDelegationDemo.Stubs.ICalculator();
        var realCalc = new RealCalculator();

        stub.Source(realCalc);

        ICalculator calc = stub;

        Assert.Equal(5, calc.Add(2, 3)); // From real implementation
    }

    [Fact]
    public void Source_ConfiguredMethodsOverrideSource()
    {
        var stub = new SourceDelegationDemo.Stubs.ICalculator();
        var realCalc = new RealCalculator();

        stub.Source(realCalc);
        stub.Add.Return(999);

        ICalculator calc = stub;

        Assert.Equal(999, calc.Add(2, 3)); // Stub takes priority
        Assert.Equal(7, calc.Subtract(10, 3)); // Delegates to source
    }

    [Fact]
    public void Source_Null_RemovesDelegation()
    {
        var stub = new SourceDelegationDemo.Stubs.ICalculator();
        var realCalc = new RealCalculator();

        stub.Source(realCalc);
        ICalculator calc = stub;

        Assert.Equal(5, calc.Add(2, 3)); // Delegates

        stub.Source(null);

        Assert.Equal(0, calc.Add(2, 3)); // Returns default
    }

    [Fact]
    public void Source_PartialStubbing()
    {
        var stub = new SourceDelegationDemo.Stubs.ICalculator();
        var realCalc = new RealCalculator();

        stub.Source(realCalc);

        // Override just Add to throw
        stub.Add.Return((a, b) => throw new InvalidOperationException("Simulated error"));

        ICalculator calc = stub;

        // Add uses stub (throws)
        Assert.Throws<InvalidOperationException>(() => calc.Add(1, 2));

        // Subtract uses real implementation
        Assert.Equal(8, calc.Subtract(10, 2));
    }

    private sealed class RealCalculator : ICalculator
    {
        public int Add(int a, int b) => a + b;
        public int Subtract(int a, int b) => a - b;
        public int Divide(int a, int b) => b == 0 ? 0 : a / b;
        public void Reset() { }
    }
}

/// <summary>
/// Tests for Source() delegation with interface hierarchies.
/// IStore : IReadableStore — demonstrates partial implementation delegation.
/// </summary>
public class SourceHierarchyTests
{
    [Fact]
    public void Source_FullImplementation_DelegatesAllMembers()
    {
        var stub = new SourceHierarchyDemo.Stubs.IStore();
        var realStore = new InMemoryStore();

        stub.Source(realStore);

        IStore store = stub;

        store.Save(1, "hello");
        Assert.Equal("hello", store.GetById(1));
        Assert.Equal(1, store.Count);
    }

    [Fact]
    public void Source_PartialImplementation_DelegatesOnlyMatchingMembers()
    {
        var stub = new SourceHierarchyDemo.Stubs.IStore();
        var readOnlySource = new ReadOnlyStore();

        // Source(IReadableStore) — only GetById and Count delegate
        stub.Source(readOnlySource);

        IStore store = stub;

        // IReadableStore members delegate to source
        Assert.Equal("item1", store.GetById(1));
        Assert.Equal(3, store.Count);

        // IStore-only members do NOT delegate — return defaults
        store.Save(99, "test");  // No-op (void default)
        store.Delete(1);          // No-op (void default)

        // Source is unchanged — Save/Delete didn't affect it
        Assert.Equal(3, store.Count);
    }

    [Fact]
    public void Source_PartialImplementation_CanConfigureNonDelegatedMembers()
    {
        var stub = new SourceHierarchyDemo.Stubs.IStore();
        var readOnlySource = new ReadOnlyStore();

        // Delegate reads
        stub.Source(readOnlySource);

        // Configure writes explicitly
        var saved = new Dictionary<int, string>();
        stub.Save.Call((id, value) => saved[id] = value);

        IStore store = stub;

        // Reads from source
        Assert.Equal("item1", store.GetById(1));

        // Writes from OnCall configuration
        store.Save(99, "new item");
        Assert.Equal("new item", saved[99]);
    }

    [Fact]
    public void Source_FullImplementation_OverridesPartial()
    {
        var stub = new SourceHierarchyDemo.Stubs.IStore();
        var readOnlySource = new ReadOnlyStore();
        var fullStore = new InMemoryStore();
        fullStore.Save(1, "full-store-item");

        // Start with partial source
        stub.Source(readOnlySource);
        IStore store = stub;
        Assert.Equal("item1", store.GetById(1)); // From readOnlySource

        // Switch to full source — all members now delegate
        stub.Source(fullStore);
        Assert.Equal("full-store-item", store.GetById(1)); // From fullStore

        // Save now works via delegation
        store.Save(2, "saved");
        Assert.Equal("saved", fullStore.GetById(2));
    }

    private sealed class InMemoryStore : IStore
    {
        private readonly Dictionary<int, string> _items = new();

        public string? GetById(int id) => _items.GetValueOrDefault(id);
        public int Count => _items.Count;
        public void Save(int id, string value) => _items[id] = value;
        public void Delete(int id) => _items.Remove(id);
    }

    private sealed class ReadOnlyStore : IReadableStore
    {
        private readonly Dictionary<int, string> _items = new()
        {
            [1] = "item1",
            [2] = "item2",
            [3] = "item3"
        };

        public string? GetById(int id) => _items.GetValueOrDefault(id);
        public int Count => _items.Count;
    }
}
