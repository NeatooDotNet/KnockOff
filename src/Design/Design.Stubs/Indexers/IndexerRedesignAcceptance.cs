// -----------------------------------------------------------------------------
// Design.Stubs - Indexer API Redesign Acceptance Criteria
// -----------------------------------------------------------------------------
// This file contains FAILING code that defines the new indexer API.
// Each section exercises a specific pattern+feature combination from the
// indexer-api-redesign plan. The developer's job is to make this compile.
//
// Related plan: docs/plans/indexer-api-redesign.md
// Related todo: docs/todos/indexer-api-redesign.md
// -----------------------------------------------------------------------------

using Design.Domain.Entities;
using KnockOff;

namespace Design.Stubs.Indexers;

// =============================================================================
// INLINE INTERFACE STUBS (Pattern 5)
// =============================================================================

[KnockOff<ICollection<string, int>>]
[KnockOff<IMatrix>]
[KnockOff<IMultiIndexerCollection>]
[KnockOff<IInitIndexerCollection<string, int>>]
public partial class IndexerRedesignAcceptanceDemo
{
    // =========================================================================
    // AC-1: Per-key configuration via indexer syntax (single-key)
    // Pattern: Inline Interface (5), ICollection<string, int>
    // =========================================================================
    public void PerKeyReturns_SingleKey()
    {
        var stub = new Stubs.ICollection();

        // NEW API: Per-key configuration returns a per-key builder
        stub.Indexer["foo"].Returns(42);
        stub.Indexer["bar"].Returns(99);

        ICollection<string, int> collection = stub;

        var val1 = collection["foo"]; // Returns 42
        var val2 = collection["bar"]; // Returns 99
    }

    // =========================================================================
    // AC-2: Per-key Get callback (single-key)
    // Pattern: Inline Interface (5), ICollection<string, int>
    // =========================================================================
    public void PerKeyGet_SingleKey()
    {
        var stub = new Stubs.ICollection();

        // Per-key callback -- key is already bound, so no key param
        stub.Indexer["foo"].Get(() => 42);

        ICollection<string, int> collection = stub;
        var val = collection["foo"]; // Returns 42
    }

    // =========================================================================
    // AC-3: Per-key Set callback (single-key)
    // Pattern: Inline Interface (5), ICollection<string, int>
    // =========================================================================
    public void PerKeySet_SingleKey()
    {
        var stub = new Stubs.ICollection();

        int captured = 0;
        stub.Indexer["foo"].Set((value) => captured = value);

        ICollection<string, int> collection = stub;
        collection["foo"] = 42;
        // captured == 42
    }

    // =========================================================================
    // AC-4: Per-key sequences
    // Pattern: Inline Interface (5), ICollection<string, int>
    // =========================================================================
    public void PerKeySequences()
    {
        var stub = new Stubs.ICollection();

        stub.Indexer["foo"].Returns(1).ThenReturns(2).ThenReturns(3);

        ICollection<string, int> collection = stub;

        var r1 = collection["foo"]; // 1
        var r2 = collection["foo"]; // 2
        var r3 = collection["foo"]; // 3
        var r4 = collection["foo"]; // 3 (repeats last)
    }

    // =========================================================================
    // AC-5: All-keys callback (replaces current Get/Set -- same API)
    // Pattern: Inline Interface (5), ICollection<string, int>
    // =========================================================================
    public void AllKeysCallback()
    {
        var stub = new Stubs.ICollection();

        // All-keys callbacks use same signatures as today
        stub.Indexer.Get((string key) => key.Length);
        stub.Indexer.Set((string key, int value) => { });

        ICollection<string, int> collection = stub;
        var val = collection["hello"]; // Returns 5
    }

    // =========================================================================
    // AC-6: Per-key with all-keys fallback
    // Pattern: Inline Interface (5), ICollection<string, int>
    // =========================================================================
    public void PerKeyWithFallback()
    {
        var stub = new Stubs.ICollection();

        // Per-key wins for "foo"
        stub.Indexer["foo"].Returns(42);

        // All-keys callback handles everything else
        stub.Indexer.Get((string key) => key.Length);

        ICollection<string, int> collection = stub;

        var val1 = collection["foo"];   // 42 (per-key)
        var val2 = collection["hello"]; // 5 (fallback to all-keys)
    }

    // =========================================================================
    // AC-7: Multi-param indexer -- flattened indexer syntax
    // Pattern: Inline Interface (5), IMatrix
    // =========================================================================
    public void MultiParam_FlattenedIndexer()
    {
        var stub = new Stubs.IMatrix();

        // FLATTENED: stub.Indexer[row, col] not stub.Indexer[(row, col)]
        stub.Indexer[1, 2].Returns(3.14);

        IMatrix matrix = stub;
        var val = matrix[1, 2]; // Returns 3.14
    }

    // =========================================================================
    // AC-8: Multi-param indexer -- callbacks use tuple TKey (via library interfaces)
    // Pattern: Inline Interface (5), IMatrix
    // =========================================================================
    public void MultiParam_CallbacksUseTupleKey()
    {
        var stub = new Stubs.IMatrix();

        // Callbacks use tuple key -- library interfaces keep TKey = (int, int)
        stub.Indexer.Get(((int row, int col) key) => key.row * 10.0 + key.col);
        stub.Indexer.Set(((int row, int col) key, double value) => { });

        IMatrix matrix = stub;
        var val = matrix[2, 3]; // Returns 23.0
    }

    // =========================================================================
    // AC-9: Multi-indexer -- overloads on interceptor by key type
    // Pattern: Inline Interface (5), IMultiIndexerCollection
    // =========================================================================
    public void MultiIndexer_OverloadsByKeyType()
    {
        var stub = new Stubs.IMultiIndexerCollection();

        // C# indexer overloads resolve by key type -- no OfXxx needed
        stub.Indexer["foo"].Returns("bar");   // string indexer
        stub.Indexer[3].Returns(42);          // int indexer

        IMultiIndexerCollection svc = stub;

        var s = svc["foo"]; // "bar"
        var i = svc[3];     // 42
    }

    // =========================================================================
    // AC-10: Multi-indexer -- callback overloads by delegate signature
    // Pattern: Inline Interface (5), IMultiIndexerCollection
    // =========================================================================
    public void MultiIndexer_CallbackOverloads()
    {
        var stub = new Stubs.IMultiIndexerCollection();

        // All-keys callbacks overloaded by delegate signature
        stub.Indexer.Get((string key) => key.ToUpperInvariant());   // string indexer
        stub.Indexer.Get((int key) => key * 10);            // int indexer
        stub.Indexer.Set((string key, string value) => { }); // string setter

        IMultiIndexerCollection svc = stub;

        var s = svc["hello"]; // "HELLO"
        var i = svc[3];       // 30
    }

    // =========================================================================
    // AC-11: Init-only indexer -- per-key Returns
    // Pattern: Inline Interface (5), IInitIndexerCollection<string, int>
    // =========================================================================
    public void InitIndexer_PerKeyReturns()
    {
        var stub = new Stubs.IInitIndexerCollection();

        // Per-key works the same for init indexers
        stub.Indexer["key"].Returns(42);

        IInitIndexerCollection<string, int> collection = stub;
        var val = collection["key"]; // Returns 42
    }

    // =========================================================================
    // AC-12: Tracking -- LastGetKey, LastSetEntry, VerifyGet, VerifySet
    // Pattern: Inline Interface (5), ICollection<string, int>
    // =========================================================================
    public void Tracking_Unchanged()
    {
        var stub = new Stubs.ICollection();
        stub.Indexer["a"].Returns(1);
        stub.Indexer["b"].Returns(2);

        ICollection<string, int> collection = stub;

        _ = collection["a"];
        _ = collection["b"];
        collection["x"] = 10;

        // Tracking API unchanged
        var lastKey = stub.Indexer.LastGetKey; // "b"
        var lastEntry = stub.Indexer.LastSetEntry; // ("x", 10)

        stub.Indexer.VerifyGet(Called.Exactly(2));
        stub.Indexer.VerifySet(Called.Once);
    }

    // =========================================================================
    // AC-13: All-keys sequences -- unchanged API
    // Pattern: Inline Interface (5), ICollection<string, int>
    // =========================================================================
    public void AllKeysSequences_Unchanged()
    {
        var stub = new Stubs.ICollection();

        stub.Indexer.Get((k) => k.Length)
            .ThenGet((k) => 100)
            .ThenGet((k) => 999);

        ICollection<string, int> collection = stub;

        var r1 = collection["hello"]; // 5
        var r2 = collection["world"]; // 100
        var r3 = collection["foo"];   // 999
    }

    // =========================================================================
    // AC-14: No Backing dictionary -- compile error if used
    // Pattern: Inline Interface (5), ICollection<string, int>
    // =========================================================================
    // UNCOMMENT to verify Backing is removed:
    // public void Backing_ShouldNotExist()
    // {
    //     var stub = new Stubs.ICollection();
    //     stub.Indexer.Backing["key"] = 42; // Should NOT compile
    // }

    // =========================================================================
    // AC-15: No OfXxx pattern -- compile error if used
    // Pattern: Inline Interface (5), IMultiIndexerCollection
    // =========================================================================
    // UNCOMMENT to verify OfXxx is removed:
    // public void OfXxx_ShouldNotExist()
    // {
    //     var stub = new Stubs.IMultiIndexerCollection();
    //     stub.Indexer.OfString.Backing["key"] = "val"; // Should NOT compile
    // }
}

// =============================================================================
// STANDALONE STUBS (Pattern 1)
// =============================================================================

[KnockOff]
public partial class IndexerRedesignStandaloneStub : ICollection<string, int>
{
}

[KnockOff]
public partial class IndexerRedesignMatrixStandaloneStub : IMatrix
{
}

/// <summary>
/// Standalone acceptance tests -- verify same API on standalone pattern.
/// </summary>
public class StandaloneIndexerAcceptance
{
    // =========================================================================
    // AC-S1: Standalone per-key Returns
    // Pattern: Standalone (1), ICollection<string, int>
    // =========================================================================
    public void Standalone_PerKeyReturns()
    {
        var stub = new IndexerRedesignStandaloneStub();

        stub.Indexer["foo"].Returns(42);

        ICollection<string, int> collection = stub;
        var val = collection["foo"]; // Returns 42
    }

    // =========================================================================
    // AC-S2: Standalone multi-param flattened indexer
    // Pattern: Standalone (1), IMatrix
    // =========================================================================
    public void Standalone_MultiParam_FlattenedIndexer()
    {
        var stub = new IndexerRedesignMatrixStandaloneStub();

        // Flattened: stub.Indexer[1, 2] not stub.Indexer[(1, 2)]
        stub.Indexer[1, 2].Returns(3.14);

        IMatrix matrix = stub;
        var val = matrix[1, 2]; // Returns 3.14
    }

    // =========================================================================
    // AC-S3: Standalone all-keys callback
    // Pattern: Standalone (1), ICollection<string, int>
    // =========================================================================
    public void Standalone_AllKeysCallback()
    {
        var stub = new IndexerRedesignStandaloneStub();

        stub.Indexer.Get((string key) => key.Length);

        ICollection<string, int> collection = stub;
        var val = collection["hello"]; // Returns 5
    }
}
