// -----------------------------------------------------------------------------
// Design.Tests - Indexer Post-Review Tests
// -----------------------------------------------------------------------------
// Tests for per-key verification and When(predicate) features added in the
// indexer post-review fixes.

using Design.Stubs.Indexers;
using KnockOff;

namespace Design.Tests.IndexerTests;

/// <summary>
/// Tests for per-key verification: VerifyGet/VerifySet on individual keys.
/// </summary>
public class IndexerPerKeyVerificationTests
{
    [Fact]
    public void PerKey_VerifyGet_TracksPerKeyGetCount()
    {
        var stub = new IndexerPostReviewAcceptanceDemo.Stubs.ICollection();
        stub.Indexer["key"].Returns(42);

        Design.Domain.Entities.ICollection<string, int> collection = stub;
        _ = collection["key"];
        _ = collection["key"];

        stub.Indexer["key"].VerifyGet(Called.Exactly(2));
    }

    [Fact]
    public void PerKey_VerifySet_TracksPerKeySetCount()
    {
        var stub = new IndexerPostReviewAcceptanceDemo.Stubs.ICollection();
        int captured = 0;
        stub.Indexer["key"].Set(v => captured = v);

        Design.Domain.Entities.ICollection<string, int> collection = stub;
        collection["key"] = 42;

        stub.Indexer["key"].VerifySet(Called.Once);
        Assert.Equal(42, captured);
    }

    [Fact]
    public void PerKey_VerifyGet_DifferentKeysTrackedSeparately()
    {
        var stub = new IndexerPostReviewAcceptanceDemo.Stubs.ICollection();
        stub.Indexer["alpha"].Returns(1);
        stub.Indexer["beta"].Returns(2);

        Design.Domain.Entities.ICollection<string, int> collection = stub;
        _ = collection["alpha"];
        _ = collection["alpha"];
        _ = collection["alpha"];
        _ = collection["beta"];

        stub.Indexer["alpha"].VerifyGet(Called.Exactly(3));
        stub.Indexer["beta"].VerifyGet(Called.Once);
    }

    [Fact]
    public void PerKey_VerifyGet_FailsWhenCountDoesNotMatch()
    {
        var stub = new IndexerPostReviewAcceptanceDemo.Stubs.ICollection();
        stub.Indexer["key"].Returns(1);

        Design.Domain.Entities.ICollection<string, int> collection = stub;
        _ = collection["key"];

        Assert.Throws<VerificationException>(() =>
            stub.Indexer["key"].VerifyGet(Called.Exactly(5)));
    }

    [Fact]
    public void PerKey_VerifySet_FailsWhenNotCalled()
    {
        var stub = new IndexerPostReviewAcceptanceDemo.Stubs.ICollection();
        stub.Indexer["key"].Set(_ => { });

        Assert.Throws<VerificationException>(() =>
            stub.Indexer["key"].VerifySet(Called.Once));
    }

    [Fact]
    public void PerKey_VerifyGet_NoArgOverload_VerifiesAtLeastOnce()
    {
        var stub = new IndexerPostReviewAcceptanceDemo.Stubs.ICollection();
        stub.Indexer["key"].Returns(1);

        Design.Domain.Entities.ICollection<string, int> collection = stub;
        _ = collection["key"];

        // No-arg VerifyGet should pass when called at least once
        stub.Indexer["key"].VerifyGet();
    }

    [Fact]
    public void PerKey_VerifySet_NoArgOverload_VerifiesAtLeastOnce()
    {
        var stub = new IndexerPostReviewAcceptanceDemo.Stubs.ICollection();
        stub.Indexer["key"].Set(_ => { });

        Design.Domain.Entities.ICollection<string, int> collection = stub;
        collection["key"] = 99;

        // No-arg VerifySet should pass when called at least once
        stub.Indexer["key"].VerifySet();
    }
}

/// <summary>
/// Tests for When(predicate) feature on indexers.
/// </summary>
public class IndexerWhenPredicateTests
{
    [Fact]
    public void When_Predicate_ReturnsMatchedValue()
    {
        var stub = new IndexerPostReviewAcceptanceDemo.Stubs.ICollection();

        stub.Indexer.When(key => key.StartsWith("prefix_", StringComparison.Ordinal)).Returns(99);

        Design.Domain.Entities.ICollection<string, int> collection = stub;

        Assert.Equal(99, collection["prefix_hello"]);
        Assert.Equal(99, collection["prefix_world"]);
    }

    [Fact]
    public void When_NoMatch_FallsToGetCallback()
    {
        var stub = new IndexerPostReviewAcceptanceDemo.Stubs.ICollection();

        stub.Indexer.When(key => key.StartsWith("x_", StringComparison.Ordinal)).Returns(99);
        stub.Indexer.Get(key => key.Length);

        Design.Domain.Entities.ICollection<string, int> collection = stub;

        // "x_test" matches the When predicate
        Assert.Equal(99, collection["x_test"]);

        // "hello" does not match the When, falls through to Get callback
        Assert.Equal(5, collection["hello"]);
    }

    [Fact]
    public void When_PriorityBelowPerKey()
    {
        var stub = new IndexerPostReviewAcceptanceDemo.Stubs.ICollection();

        // Per-key exact match (highest priority)
        stub.Indexer["exact"].Returns(100);

        // When predicate also matches "exact", but per-key wins
        stub.Indexer.When(key => key.Length > 3).Returns(42);

        Design.Domain.Entities.ICollection<string, int> collection = stub;

        Assert.Equal(100, collection["exact"]);  // per-key wins
        Assert.Equal(42, collection["hello"]);   // When matches
    }

    [Fact]
    public void When_SetCallback_CapturesMatchedKeyValue()
    {
        var stub = new IndexerPostReviewAcceptanceDemo.Stubs.ICollection();

        (string key, int value)? captured = null;
        stub.Indexer.When(key => key.StartsWith("temp_", StringComparison.Ordinal)).Set((key, value) =>
        {
            captured = (key, value);
        });

        Design.Domain.Entities.ICollection<string, int> collection = stub;
        collection["temp_data"] = 42;

        Assert.NotNull(captured);
        Assert.Equal("temp_data", captured.Value.key);
        Assert.Equal(42, captured.Value.value);
    }

    [Fact]
    public void When_GetSetChains_Independent()
    {
        var stub = new IndexerPostReviewAcceptanceDemo.Stubs.ICollection();

        // Configure getter When chain
        stub.Indexer.When(key => key.StartsWith("a", StringComparison.Ordinal)).Returns(1);

        // Configure setter When chain (separate from getter)
        bool setCallbackInvoked = false;
        stub.Indexer.When(key => key.StartsWith("a", StringComparison.Ordinal)).Set((key, value) =>
        {
            setCallbackInvoked = true;
        });

        Design.Domain.Entities.ICollection<string, int> collection = stub;

        // Set operation should NOT consume getter When matcher
        collection["alpha"] = 99;
        Assert.True(setCallbackInvoked);

        // Get operation should still return 1 (getter chain unaffected by set)
        Assert.Equal(1, collection["alpha"]);
    }

    [Fact]
    public void When_ThenWhen_Chain()
    {
        var stub = new IndexerPostReviewAcceptanceDemo.Stubs.ICollection();

        stub.Indexer
            .When(key => key.StartsWith("a", StringComparison.Ordinal)).Returns(1)
            .ThenWhen(key => key.StartsWith("b", StringComparison.Ordinal)).Returns(2);

        Design.Domain.Entities.ICollection<string, int> collection = stub;

        // First call matches "a" predicate, advances chain
        Assert.Equal(1, collection["alpha"]);

        // Second call matches "b" predicate (chain advanced)
        Assert.Equal(2, collection["beta"]);
    }

    [Fact]
    public void When_LastMatcher_Repeats()
    {
        var stub = new IndexerPostReviewAcceptanceDemo.Stubs.ICollection();

        stub.Indexer
            .When(key => key.StartsWith("a", StringComparison.Ordinal)).Returns(1)
            .ThenWhen(key => key.StartsWith("b", StringComparison.Ordinal)).Returns(2);

        Design.Domain.Entities.ICollection<string, int> collection = stub;

        Assert.Equal(1, collection["alpha"]);
        Assert.Equal(2, collection["beta"]);
        // Last matcher repeats
        Assert.Equal(2, collection["bravo"]);
        Assert.Equal(2, collection["banana"]);
    }
}
