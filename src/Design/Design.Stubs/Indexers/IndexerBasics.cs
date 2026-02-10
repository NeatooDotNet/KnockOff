// -----------------------------------------------------------------------------
// Design.Stubs - Basic Indexer Stubbing
// -----------------------------------------------------------------------------
// This file demonstrates the fundamental indexer stubbing APIs:
// - Per-key Returns(value) for simple value configuration
// - Get(callback) with key access for dynamic behavior
// - Set(callback) with key and value
// - LastGetKey and LastSetEntry tracking
// - Multi-key indexers (tuple keys)
// - Per-key builder pattern: stub.Indexer[key].Returns(value)
// -----------------------------------------------------------------------------

using Design.Domain.Entities;
using KnockOff;

namespace Design.Stubs.Indexers;

// =============================================================================
// BASIC INDEXER STUB CONFIGURATION
// =============================================================================

[KnockOff<ICollection<string, int>>]
[KnockOff<IMatrix>]
[KnockOff<IInitIndexerCollection<string, int>>]
public partial class IndexerBasicsDemo
{
    // =========================================================================
    // Per-Key Returns - Simple Value Configuration
    // =========================================================================
    // DESIGN DECISION: Each key gets its own PerKeyBuilder accessed via the
    // interceptor's C# indexer: stub.Indexer[key].Returns(value)
    //
    // GENERATOR BEHAVIOR: For indexer:
    //
    //   public interface ICollection<TKey, TValue> { TValue this[TKey key] { get; set; } }
    //
    // The generator produces:
    //
    //   public class IndexerInterceptor
    //   {
    //       private readonly Dictionary<string, PerKeyBuilder> _perKeyBuilders = new();
    //
    //       public PerKeyBuilder this[string key] => ...;
    //
    //       public sealed class PerKeyBuilder
    //       {
    //           public PerKeyBuilder Returns(int value) { ... }
    //           public PerKeyBuilder Get(Func<int> callback) { ... }
    //           public PerKeyBuilder Set(Action<int> callback) { ... }
    //       }
    //   }
    //
    // DID NOT DO THIS: Use a Backing dictionary that magically connects get/set
    //
    // WHY NOT: Per-key builders are explicit, composable, and avoid confusion
    // about whether Get/Set callbacks override Backing or vice versa.
    // =========================================================================

    public void PerKey_Returns()
    {
        var stub = new Stubs.ICollection();

        // Configure specific keys to return specific values
        stub.Indexer["existing"].Returns(100);

        ICollection<string, int> collection = stub;

        // Get returns configured value
        var val = collection["existing"]; // Returns 100
    }

    // =========================================================================
    // Get(callback) - Getter with Key Access
    // =========================================================================
    // DESIGN DECISION: The all-keys Get callback receives the key as a parameter.
    // This is the fallback for keys not configured with per-key Returns.
    //
    // GENERATOR BEHAVIOR:
    //
    //   public IIndexerGetBuilder<string, int> Get(Func<string, int> callback) { ... }
    //
    // PRIORITY CHAIN:
    //   1. Per-key builder (stub.Indexer[key].Returns(value))
    //   2. All-keys sequence (Get(...).ThenGet(...))
    //   3. All-keys Get callback
    //   4. Source delegation
    //   5. Strict mode check
    //   6. Default value
    // =========================================================================

    public void Get_ReceivesKey()
    {
        var stub = new Stubs.ICollection();

        // Callback receives the key, returns value based on it
        stub.Indexer.Get((key) => key.Length);

        ICollection<string, int> collection = stub;

        var len1 = collection["hello"]; // Returns 5
        var len2 = collection["hi"];    // Returns 2
    }

    // =========================================================================
    // Set(callback) - Setter with Key and Value
    // =========================================================================
    // DESIGN DECISION: Indexer setters receive both the key AND the value.
    // This allows tracking what's being stored where.
    //
    // GENERATOR BEHAVIOR:
    //
    //   public IIndexerSetBuilder<string, int> Set(Action<string, int> callback) { ... }
    // =========================================================================

    public void Set_ReceivesKeyAndValue()
    {
        var stub = new Stubs.ICollection();
        var storage = new Dictionary<string, int>();

        // Callback receives key and value
        stub.Indexer.Set((key, value) => storage[key] = value);

        ICollection<string, int> collection = stub;

        collection["one"] = 1;
        collection["two"] = 2;

        // storage now contains: { "one": 1, "two": 2 }
    }

    // =========================================================================
    // Per-Key with All-Keys Fallback
    // =========================================================================
    // DESIGN DECISION: Per-key builders take priority over all-keys callbacks.
    // This allows configuring specific keys with Returns while using a callback
    // as a fallback for unconfigured keys.
    // =========================================================================

    public void PerKey_WithAllKeysFallback()
    {
        var stub = new Stubs.ICollection();

        // Specific keys get specific values
        stub.Indexer["special"].Returns(999);

        // All other keys use the callback
        stub.Indexer.Get((key) => key.Length);

        ICollection<string, int> collection = stub;

        var special = collection["special"]; // Returns 999 (per-key wins)
        var other = collection["hello"];     // Returns 5 (callback fallback)
    }

    // =========================================================================
    // LastGetKey and LastSetEntry - Tracking
    // =========================================================================
    // DESIGN DECISION: Indexer interceptors track:
    // - LastGetKey: The key used in the most recent get (any path)
    // - LastSetEntry: A (Key, Value) tuple from the most recent set (any path)
    //
    // These track ALL accesses regardless of whether the call was handled by
    // a per-key builder, all-keys callback, or was unconfigured.
    // =========================================================================

    public void Tracking_LastGetKeyAndLastSetEntry()
    {
        var stub = new Stubs.ICollection();
        stub.Indexer["a"].Returns(1);
        stub.Indexer["b"].Returns(2);

        ICollection<string, int> collection = stub;

        // Access different keys
        _ = collection["a"];
        _ = collection["b"];
        _ = collection["a"];

        // LastGetKey is the most recent
        var lastKey = stub.Indexer.LastGetKey; // "a"

        // Set values
        collection["x"] = 10;
        collection["y"] = 20;

        // LastSetEntry is the most recent (Key, Value) tuple
        var lastEntry = stub.Indexer.LastSetEntry; // ("y", 20)
    }

    // =========================================================================
    // Multi-Key Indexers (Tuple Keys)
    // =========================================================================
    // DESIGN DECISION: Multi-key indexers like this[int row, int col] use
    // tuple keys internally. The KeyType is (int row, int col).
    // All callbacks (Get, Set, ThenGet, ThenSet) use the tuple key type.
    // Per-key builders use flattened multi-param indexer accessors.
    //
    // GENERATOR BEHAVIOR:
    //
    //   public class IndexerInterceptor
    //   {
    //       public PerKeyBuilder this[int row, int col] { get { ... } }
    //       public IIndexerGetBuilder<...> Get(Func<(int row, int col), double> callback) { ... }
    //       public IIndexerSetBuilder<...> Set(Action<(int row, int col), double> callback) { ... }
    //   }
    // =========================================================================

    public void MultiKeyIndexer_PerKey()
    {
        var stub = new Stubs.IMatrix();

        // Flattened indexer accessor for per-key configuration
        stub.Indexer[1, 2].Returns(12.0);
        stub.Indexer[3, 4].Returns(34.0);

        IMatrix matrix = stub;
        var val = matrix[1, 2]; // Returns 12.0
    }

    public void MultiKeyIndexer_TupleKeys()
    {
        var stub = new Stubs.IMatrix();

        // Callback receives tuple key
        stub.Indexer.Get(key => key.row * 10.0 + key.col);

        IMatrix matrix = stub;
        var val = matrix[2, 3]; // Returns 23.0 (2*10 + 3)
    }

    public void MultiKeyIndexer_SetCallback()
    {
        var stub = new Stubs.IMatrix();

        // Set callback receives tuple key and value
        (int row, int col, double val)? captured = null;
        stub.Indexer.Set((key, value) =>
        {
            captured = (key.row, key.col, value);
        });

        IMatrix matrix = stub;
        matrix[2, 3] = 23.0;

        // captured is (2, 3, 23.0)
    }

    // =========================================================================
    // Init-Only Indexers
    // =========================================================================
    // DESIGN DECISION: Init-only indexer accessors use 'init' keyword instead
    // of 'set'. This follows the same pattern as init-only properties.
    //
    // GENERATOR BEHAVIOR: For indexer with { get; init; }:
    //
    //   int IInitIndexerCollection<string, int>.this[string key]
    //   {
    //       get => Indexer.InvokeGet(Strict, key);
    //       init => Indexer.InvokeSet(Strict, key, value);
    //   }
    //
    // The interceptor class itself is unchanged -- only the accessor keyword
    // differs.
    // =========================================================================

    public void InitIndexer_PerKey()
    {
        var stub = new Stubs.IInitIndexerCollection();

        // Init-only indexer uses per-key Returns for get
        stub.Indexer["key"].Returns(42);

        IInitIndexerCollection<string, int> collection = stub;
        var val = collection["key"]; // Returns 42
    }

    // =========================================================================
    // VerifyGet() and VerifySet()
    // =========================================================================
    // DESIGN DECISION: Like properties, indexers have separate verification
    // for get and set operations. Verification counts include ALL access paths
    // (per-key, all-keys callback, unconfigured).
    // =========================================================================

    public void Verify_GetterAndSetter()
    {
        var stub = new Stubs.ICollection();
        stub.Indexer["test"].Returns(1);

        ICollection<string, int> collection = stub;

        _ = collection["test"];
        _ = collection["test"];
        collection["new"] = 42;

        stub.Indexer.VerifyGet(Called.Exactly(2));
        stub.Indexer.VerifySet(Called.Once);
    }

    // =========================================================================
    // Per-Key vs All-Keys Get Priority
    // =========================================================================
    //
    // Per-key Returns always takes priority over all-keys Get callback.
    // This is the recommended pattern: use per-key for specific values,
    // and all-keys Get as a fallback.
    //
    //   stub.Indexer["key"].Returns(100);  // Per-key: always returns 100 for "key"
    //   stub.Indexer.Get((k) => 999);      // All-keys: returns 999 for other keys
    //   collection["key"];                 // Returns 100 (per-key wins)
    //   collection["other"];               // Returns 999 (callback fallback)
    // =========================================================================

    public void PerKey_VsGet()
    {
        var stub = new Stubs.ICollection();

        // Per-key for specific keys
        stub.Indexer["key"].Returns(100);

        ICollection<string, int> collection = stub;
        var val1 = collection["key"]; // 100

        // All-keys Get as fallback
        stub.Indexer.Get((k) => 999);
        var val2 = collection["key"];   // 100 - per-key still wins!
        var val3 = collection["other"]; // 999 - callback handles unconfigured keys
    }
}
