// -----------------------------------------------------------------------------
// Design.Stubs - Indexer Sequences
// -----------------------------------------------------------------------------
// This file demonstrates sequence APIs for indexers:
// - Get().ThenGet() for getter sequences
// - Set().ThenSet() for setter sequences
// -----------------------------------------------------------------------------

using Design.Domain.Entities;
using KnockOff;

namespace Design.Stubs.Indexers;

// =============================================================================
// INDEXER SEQUENCES
// =============================================================================

[KnockOff<ICollection<string, int>>]
public partial class IndexerSequencesDemo
{
    // =========================================================================
    // Get().ThenGet() - Getter Sequences
    // =========================================================================
    // DESIGN DECISION: Indexer getters support sequences. Each access advances
    // through the sequence, regardless of which key is accessed.
    //
    // Note: The sequence is per-indexer, not per-key. All key accesses share
    // the same sequence state.
    //
    // GENERATOR BEHAVIOR:
    //
    //   public IIndexerGetSequence<Func<string, int>> ThenGet(Func<string, int> callback) { ... }
    // =========================================================================

    public void ThenGet_GetterSequence()
    {
        var stub = new Stubs.ICollection();

        // First access returns key length, second returns 100, then repeats 999
        stub.Indexer.Get((k) => k.Length)
            .ThenGet((k) => 100)
            .ThenGet((k) => 999);

        ICollection<string, int> collection = stub;

        var r1 = collection["hello"]; // 5 (first callback: length)
        var r2 = collection["world"]; // 100 (second callback)
        var r3 = collection["foo"];   // 999 (third callback)
        var r4 = collection["bar"];   // 999 (repeats last value)
    }

    // =========================================================================
    // Set().ThenSet() - Setter Sequences
    // =========================================================================
    // DESIGN DECISION: Setter sequences work similarly. Each set advances
    // through the sequence of callbacks.
    // =========================================================================

    public void ThenSet_SetterSequence()
    {
        var stub = new Stubs.ICollection();
        var log = new List<string>();

        stub.Indexer.Set((k, v) => log.Add($"First: {k}={v}"))
            .ThenSet((k, v) => log.Add($"Second: {k}={v}"))
            .ThenSet((k, v) => log.Add($"Final: {k}={v}"));

        ICollection<string, int> collection = stub;

        collection["a"] = 1; // log: ["First: a=1"]
        collection["b"] = 2; // log: [..., "Second: b=2"]
        collection["c"] = 3; // log: [..., "Final: c=3"]
        collection["d"] = 4; // log: [..., "Final: d=4"] (repeats last callback)
    }

    // =========================================================================
    // ThenDefault() - Explicit Default Termination
    // =========================================================================
    // DESIGN DECISION: ThenDefault() configures the sequence to return default(T)
    // after exhaustion instead of repeating the last value.
    //
    // This is useful when tests need to detect or handle exhaustion explicitly.
    // =========================================================================

    public void ThenDefault_ReturnsDefaultAfterExhaustion()
    {
        var stub = new Stubs.ICollection();

        stub.Indexer.Get((k) => k.Length)
            .ThenGet((k) => 100)
            .ThenDefault();  // Return default after exhaustion

        ICollection<string, int> collection = stub;

        var r1 = collection["hello"]; // 5 (first callback: length)
        var r2 = collection["world"]; // 100 (second callback)
        var r3 = collection["foo"];   // 0 (default - sequence exhausted)
        var r4 = collection["bar"];   // 0 (still default)
    }

    // =========================================================================
    // Sequences are Shared Across Keys
    // =========================================================================
    // DESIGN DECISION: Unlike a real collection where each key has independent
    // state, indexer sequences are global. The sequence position advances on
    // ANY access, not per-key.
    //
    // This matches how method sequences work - tracking the sequence of calls,
    // not the sequence of calls per argument value.
    //
    // For per-key behavior, use per-key Returns (stub.Indexer[key].Returns(value))
    // or use Get with a callback that maintains its own dictionary.
    // =========================================================================

    public void Sequences_GlobalNotPerKey()
    {
        var stub = new Stubs.ICollection();

        stub.Indexer.Get((k) => 1)
            .ThenGet((k) => 2)
            .ThenGet((k) => 3);

        ICollection<string, int> collection = stub;

        // Same key accessed multiple times
        var r1 = collection["x"]; // 1 (first in sequence)
        var r2 = collection["x"]; // 2 (second in sequence)
        var r3 = collection["x"]; // 3 (third, now repeating)

        // Different keys also advance the sequence
        stub.Indexer.Reset(); // Reset to start
        stub.Indexer.Get((k) => 1).ThenGet((k) => 2).ThenGet((k) => 3);

        r1 = collection["a"]; // 1
        r2 = collection["b"]; // 2 (sequence advanced despite different key)
        r3 = collection["c"]; // 3
    }
}
