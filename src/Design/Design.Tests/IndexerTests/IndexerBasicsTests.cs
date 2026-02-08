// -----------------------------------------------------------------------------
// Design.Tests - Indexer Basics Tests
// -----------------------------------------------------------------------------

using Design.Stubs.Indexers;
using KnockOff;

namespace Design.Tests.IndexerTests;

/// <summary>
/// Tests for basic indexer stubbing: Get, Set, Backing dictionary.
/// </summary>
public class IndexerBasicsTests
{
    [Fact]
    public void Get_WithCallback_ReceivesKey()
    {
        var stub = new IndexerBasicsDemo.Stubs.ICollection();
        stub.Indexer.Get(key => key.Length);

        Design.Domain.Entities.ICollection<string, int> collection = stub;

        Assert.Equal(5, collection["hello"]);
        Assert.Equal(3, collection["foo"]);
    }

    [Fact]
    public void Set_WithCallback_ReceivesKeyAndValue()
    {
        var stub = new IndexerBasicsDemo.Stubs.ICollection();
        string? capturedKey = null;
        int capturedValue = 0;

        stub.Indexer.Set((k, v) =>
        {
            capturedKey = k;
            capturedValue = v;
        });

        Design.Domain.Entities.ICollection<string, int> collection = stub;
        collection["test"] = 42;

        Assert.Equal("test", capturedKey);
        Assert.Equal(42, capturedValue);
    }

    [Fact]
    public void Backing_DictionaryPattern()
    {
        var stub = new IndexerBasicsDemo.Stubs.ICollection();
        var dict = new Dictionary<string, int>();

        stub.Indexer.Get(k => dict.TryGetValue(k, out var v) ? v : -1);
        stub.Indexer.Set((k, v) => dict[k] = v);

        Design.Domain.Entities.ICollection<string, int> collection = stub;

        collection["key1"] = 100;
        collection["key2"] = 200;

        Assert.Equal(100, collection["key1"]);
        Assert.Equal(200, collection["key2"]);
        Assert.Equal(-1, collection["missing"]);
    }

    [Fact]
    public void LastGetKey_CapturesLastKeyAccessed()
    {
        var stub = new IndexerBasicsDemo.Stubs.ICollection();
        stub.Indexer.Get(_ => 0);

        Design.Domain.Entities.ICollection<string, int> collection = stub;
        _ = collection["first"];
        _ = collection["second"];

        Assert.Equal("second", stub.Indexer.LastGetKey);
    }

    [Fact]
    public void LastSetEntry_CapturesLastSet()
    {
        var stub = new IndexerBasicsDemo.Stubs.ICollection();

        Design.Domain.Entities.ICollection<string, int> collection = stub;
        collection["first"] = 1;
        collection["second"] = 2;

        var entry = stub.Indexer.LastSetEntry;
        Assert.NotNull(entry);
        Assert.Equal("second", entry.Value.Key);
        Assert.Equal(2, entry.Value.Value);
    }

    [Fact]
    public void VerifyGet_ChecksGetterAccess()
    {
        var stub = new IndexerBasicsDemo.Stubs.ICollection();
        stub.Indexer.Get(_ => 0);

        Design.Domain.Entities.ICollection<string, int> collection = stub;
        _ = collection["a"];
        _ = collection["b"];

        stub.Indexer.VerifyGet(Called.Exactly(2));
    }

    [Fact]
    public void VerifySet_ChecksSetterAccess()
    {
        var stub = new IndexerBasicsDemo.Stubs.ICollection();

        Design.Domain.Entities.ICollection<string, int> collection = stub;
        collection["a"] = 1;
        collection["b"] = 2;
        collection["c"] = 3;

        stub.Indexer.VerifySet(Called.Exactly(3));
    }

    [Fact]
    public void UnconfiguredIndexer_ReturnsDefault()
    {
        var stub = new IndexerBasicsDemo.Stubs.ICollection();

        Design.Domain.Entities.ICollection<string, int> collection = stub;

        Assert.Equal(0, collection["any"]); // default(int) = 0
    }
}
