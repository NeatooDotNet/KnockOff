// -----------------------------------------------------------------------------
// Design.Tests - Indexer Sequence Tests
// -----------------------------------------------------------------------------

using Design.Stubs.Indexers;

namespace Design.Tests.IndexerTests;

/// <summary>
/// Tests for indexer sequences: Get().ThenGet() and Set().ThenSet() chains.
/// </summary>
public class IndexerSequenceTests
{
    [Fact]
    public void ThenGet_CreatesGetterSequence()
    {
        var stub = new IndexerSequencesDemo.Stubs.ICollection();

        stub.Indexer.Get(k => 1)
            .ThenGet(k => 2)
            .ThenGet(k => 3);

        Design.Domain.Entities.ICollection<string, int> collection = stub;

        Assert.Equal(1, collection["a"]);
        Assert.Equal(2, collection["b"]);
        Assert.Equal(3, collection["c"]);
    }

    [Fact]
    public void ThenGet_RepeatsLastValueAfterExhaustion()
    {
        // ACTUAL BEHAVIOR: Sequences repeat the last callback (NSubstitute-like).
        // Use ThenDefault() to return default(T) after exhaustion instead.
        var stub = new IndexerSequencesDemo.Stubs.ICollection();

        stub.Indexer.Get(k => 1)
            .ThenGet(k => 999);

        Design.Domain.Entities.ICollection<string, int> collection = stub;

        Assert.Equal(1, collection["x"]); // First callback
        Assert.Equal(999, collection["x"]); // Second callback
        Assert.Equal(999, collection["x"]); // Repeats last value
        Assert.Equal(999, collection["x"]); // Still repeats
    }

    [Fact]
    public void ThenDefault_ReturnsDefaultAfterExhaustion()
    {
        // ThenDefault() causes sequence to return default(T) after exhaustion
        var stub = new IndexerSequencesDemo.Stubs.ICollection();

        stub.Indexer.Get(k => 1)
            .ThenGet(k => 999)
            .ThenDefault();

        Design.Domain.Entities.ICollection<string, int> collection = stub;

        Assert.Equal(1, collection["x"]); // First callback
        Assert.Equal(999, collection["x"]); // Second callback
        Assert.Equal(0, collection["x"]); // Exhausted - returns default(int)
    }

    [Fact]
    public void ThenGet_SequenceIsGlobalNotPerKey()
    {
        var stub = new IndexerSequencesDemo.Stubs.ICollection();

        stub.Indexer.Get(k => 1)
            .ThenGet(k => 2);

        Design.Domain.Entities.ICollection<string, int> collection = stub;

        // Access same key twice - sequence still advances
        Assert.Equal(1, collection["x"]); // First in sequence
        Assert.Equal(2, collection["x"]); // Second in sequence (same key)
    }

    [Fact]
    public void ThenSet_CreatesSetterSequence()
    {
        var stub = new IndexerSequencesDemo.Stubs.ICollection();
        var log = new List<string>();

        stub.Indexer.Set((k, v) => log.Add($"First: {k}={v}"))
            .ThenSet((k, v) => log.Add($"Second: {k}={v}"))
            .ThenSet((k, v) => log.Add($"Third: {k}={v}"));

        Design.Domain.Entities.ICollection<string, int> collection = stub;

        collection["a"] = 1;
        collection["b"] = 2;
        collection["c"] = 3;

        Assert.Equal(["First: a=1", "Second: b=2", "Third: c=3"], log);
    }

    [Fact]
    public void ThenSet_RepeatsLastCallbackAfterExhaustion()
    {
        // ACTUAL BEHAVIOR: Sequences repeat the last callback (NSubstitute-like).
        // Use ThenDefault() to skip callbacks after exhaustion instead.
        var stub = new IndexerSequencesDemo.Stubs.ICollection();
        var log = new List<string>();

        stub.Indexer.Set((k, v) => log.Add($"First: {k}={v}"))
            .ThenSet((k, v) => log.Add($"Final: {k}={v}"));

        Design.Domain.Entities.ICollection<string, int> collection = stub;

        collection["a"] = 1;
        collection["b"] = 2;
        collection["c"] = 3; // Repeats last callback

        Assert.Equal(["First: a=1", "Final: b=2", "Final: c=3"], log);
    }

    [Fact]
    public void ThenSet_ThenDefault_SkipsAfterExhaustion()
    {
        // ThenDefault() causes sequence to skip callbacks after exhaustion
        var stub = new IndexerSequencesDemo.Stubs.ICollection();
        var log = new List<string>();

        stub.Indexer.Set((k, v) => log.Add($"First: {k}={v}"))
            .ThenSet((k, v) => log.Add($"Final: {k}={v}"))
            .ThenDefault();

        Design.Domain.Entities.ICollection<string, int> collection = stub;

        collection["a"] = 1;
        collection["b"] = 2;
        collection["c"] = 3; // Exhausted - no callback

        Assert.Equal(["First: a=1", "Final: b=2"], log);
    }
}
