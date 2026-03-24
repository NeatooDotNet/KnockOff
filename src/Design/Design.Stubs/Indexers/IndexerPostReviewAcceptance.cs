// -----------------------------------------------------------------------------
// Design.Stubs - Indexer Post-Review Acceptance Criteria
// -----------------------------------------------------------------------------
// This file contains acceptance criteria for the indexer post-review fixes.
// Uncommented code represents features that DO NOT YET EXIST and will fail
// to compile until the developer implements them. The failing code IS the
// acceptance criteria.
//
// Related plan: docs/plans/indexer-post-review-fixes.md
// Related todo: docs/todos/indexer-post-review-fixes.md
// -----------------------------------------------------------------------------

using Design.Domain.Entities;
using KnockOff;

namespace Design.Stubs.Indexers;

// =============================================================================
// INLINE INTERFACE STUBS FOR POST-REVIEW ACCEPTANCE
// =============================================================================

[KnockOff<ICollection<string, int>>]
[KnockOff<IMatrix>]
public partial class IndexerPostReviewAcceptanceDemo
{
    // =========================================================================
    // PHASE 3: Per-Key Verification
    // =========================================================================

    // AC-PKV-1: Per-key VerifyGet (single-key)
    // Needs Implementation: PerKeyBuilder.VerifyGet() does not exist yet
    // CS1061: PerKeyBuilder does not contain 'VerifyGet'
    public void PerKey_VerifyGet()
    {
        var stub = new Stubs.ICollection();
        stub.Indexer["key"].Returns(42);

        ICollection<string, int> collection = stub;
        _ = collection["key"];
        _ = collection["key"];

        // Verify specific key was read
        stub.Indexer["key"].VerifyGet();
        stub.Indexer["key"].VerifyGet(Called.Exactly(2));
    }

    // AC-PKV-2: Per-key VerifySet (single-key)
    // Needs Implementation: PerKeyBuilder.VerifySet() does not exist yet
    // CS1061: PerKeyBuilder does not contain 'VerifySet'
    public void PerKey_VerifySet()
    {
        var stub = new Stubs.ICollection();
        int captured = 0;
        stub.Indexer["key"].Set(v => captured = v);

        ICollection<string, int> collection = stub;
        collection["key"] = 42;

        // Verify specific key was written
        stub.Indexer["key"].VerifySet();
        stub.Indexer["key"].VerifySet(Called.Once);
    }

    // AC-PKV-3: Multi-param per-key verification
    // Needs Implementation: PerKeyBuilder.VerifyGet() does not exist yet
    // CS1061: PerKeyBuilder does not contain 'VerifyGet'
    public void PerKey_MultiParam_VerifyGet()
    {
        var stub = new Stubs.IMatrix();
        stub.Indexer[1, 2].Returns(3.14);

        IMatrix matrix = stub;
        _ = matrix[1, 2];

        // Verify specific multi-param key was read
        stub.Indexer[1, 2].VerifyGet(Called.Once);
    }

    // =========================================================================
    // PHASE 4: When(predicate) for Indexers
    // =========================================================================

    // AC-WHEN-1: Basic When predicate with Returns
    // Needs Implementation: Interceptor.When() does not exist yet
    // CS1061: IndexerInterceptor does not contain 'When'
    public void When_BasicPredicate_Returns()
    {
        var stub = new Stubs.ICollection();

        stub.Indexer.When(key => key.StartsWith("prefix_", global::System.StringComparison.Ordinal)).Returns(99);

        ICollection<string, int> collection = stub;
        var val = collection["prefix_hello"]; // Returns 99
    }

    // AC-WHEN-2: When chain with ThenWhen
    // Needs Implementation: When chain with ThenWhen does not exist yet
    public void When_Chain_ThenWhen()
    {
        var stub = new Stubs.ICollection();

        stub.Indexer
            .When(key => key.StartsWith('a')).Returns(1)
            .ThenWhen(key => key.StartsWith('b')).Returns(2);

        ICollection<string, int> collection = stub;
        var val1 = collection["alpha"]; // 1
        var val2 = collection["beta"];  // 2
    }

    // AC-WHEN-3: When with priority below per-key
    // Needs Implementation: When() does not exist yet
    public void When_PriorityBelowPerKey()
    {
        var stub = new Stubs.ICollection();

        // Per-key exact match takes priority
        stub.Indexer["exact"].Returns(100);

        // When predicate matches "exact" too, but per-key wins
        stub.Indexer.When(key => key.Length > 3).Returns(42);

        ICollection<string, int> collection = stub;
        var val1 = collection["exact"];  // 100 (per-key wins)
        var val2 = collection["hello"];  // 42 (When matches)
    }

    // AC-WHEN-4: When with Set callback (setter chain, independent from getter)
    // Needs Implementation: When() does not exist yet
    // CS1061: IndexerInterceptor does not contain 'When'
    public void When_SetCallback()
    {
        var stub = new Stubs.ICollection();

        (string key, int value)? captured = null;
        stub.Indexer.When(key => key.StartsWith("temp_", global::System.StringComparison.Ordinal)).Set((key, value) =>
        {
            captured = (key, value);
        });

        ICollection<string, int> collection = stub;
        collection["temp_data"] = 42;
        // captured should be ("temp_data", 42)
    }

    // AC-WHEN-5: Get and Set When chains are independent
    // Needs Implementation: When() does not exist yet
    // CS1061: IndexerInterceptor does not contain 'When'
    public void When_GetSetIndependence()
    {
        var stub = new Stubs.ICollection();

        // Configure getter When chain
        stub.Indexer.When(key => key.StartsWith('a')).Returns(1);

        // Configure setter When chain (separate from getter)
        stub.Indexer.When(key => key.StartsWith('a')).Set((key, value) => { });

        ICollection<string, int> collection = stub;

        // Set operation should NOT consume getter When matcher
        collection["alpha"] = 99;

        // Get operation should still return 1 (getter chain unaffected by set)
        var val = collection["alpha"]; // Returns 1
    }

    // AC-WHEN-6: Multi-param indexer When (tuple key)
    // Needs Implementation: When() does not exist yet
    // CS1061: IndexerInterceptor does not contain 'When'
    public void When_MultiParam_TupleKey()
    {
        var stub = new Stubs.IMatrix();

        stub.Indexer.When(key => key.row > 0 && key.col > 0).Returns(1.0);

        IMatrix matrix = stub;
        var val = matrix[1, 2]; // Returns 1.0
    }
}

// =============================================================================
// STANDALONE STUBS FOR POST-REVIEW ACCEPTANCE
// =============================================================================

[KnockOff]
public partial class IndexerPostReviewStandaloneStub : ICollection<string, int>
{
}

public class StandaloneIndexerPostReviewAcceptance
{
    // AC-PKV-S1: Standalone per-key VerifyGet
    // Needs Implementation: PerKeyBuilder.VerifyGet() does not exist yet
    public void Standalone_PerKey_VerifyGet()
    {
        var stub = new IndexerPostReviewStandaloneStub();
        stub.Indexer["key"].Returns(42);

        ICollection<string, int> collection = stub;
        _ = collection["key"];

        stub.Indexer["key"].VerifyGet(Called.Once);
    }

    // AC-WHEN-S1: Standalone When predicate
    // Needs Implementation: Interceptor.When() does not exist yet
    public void Standalone_When_Predicate()
    {
        var stub = new IndexerPostReviewStandaloneStub();

        stub.Indexer.When(key => key.Length > 3).Returns(42);

        ICollection<string, int> collection = stub;
        var val = collection["hello"]; // Returns 42
    }
}
