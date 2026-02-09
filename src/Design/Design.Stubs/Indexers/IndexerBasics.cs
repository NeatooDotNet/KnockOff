// -----------------------------------------------------------------------------
// Design.Stubs - Basic Indexer Stubbing
// -----------------------------------------------------------------------------
// This file demonstrates the fundamental indexer stubbing APIs:
// - Get(callback) with key access
// - Set(callback) with key and value
// - Backing dictionary for storage
// - LastGetKey and LastSetEntry tracking
// - Multi-key indexers (tuple keys)
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
    // Get(callback) - Getter with Key Access
    // =========================================================================
    // DESIGN DECISION: Indexer getters receive the key as a callback parameter.
    // This differs from property getters which have no parameters.
    //
    // GENERATOR BEHAVIOR: For indexer:
    //
    //   public interface ICollection<TKey, TValue> { TValue this[TKey key] { get; set; } }
    //
    // The generator produces:
    //
    //   public class IndexerInterceptor
    //   {
    //       public void Get(Func<string, int> callback) { _getCallback = callback; }
    //
    //       public int Get(string key)
    //       {
    //           LastGetKey = key;
    //           return _getCallback?.Invoke(key) ?? default;
    //       }
    //   }
    //
    // DID NOT DO THIS: Make indexer getters parameterless like properties
    //
    // REJECTED PATTERN:
    //   stub.Indexer.Get(42);  // How would we know which key returns 42?
    //
    // WHY NOT: Indexers are fundamentally key-based. The callback MUST receive
    // the key to provide meaningful behavior.
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
    //   public void Set(Action<string, int> callback) { _setCallback = callback; }
    //
    //   public void Set(string key, int value)
    //   {
    //       LastSetEntry = (key, value);
    //       _setCallback?.Invoke(key, value);
    //   }
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
    // Backing - Dictionary for Storage
    // =========================================================================
    // DESIGN DECISION: The Backing property provides a Dictionary<TKey, TValue>
    // for simple indexer scenarios. This is analogous to Value for properties.
    //
    // When Backing is used:
    // - Gets retrieve from the dictionary
    // - Sets store into the dictionary
    // - No Get/Set callbacks needed
    //
    // GENERATOR BEHAVIOR:
    //
    //   public Dictionary<string, int> Backing { get; } = new();
    //
    //   public int Get(string key)
    //   {
    //       if (_getCallback != null) return _getCallback(key);
    //       return Backing.TryGetValue(key, out var v) ? v : default;
    //   }
    //
    //   public void Set(string key, int value)
    //   {
    //       if (_setCallback != null) { _setCallback(key, value); return; }
    //       Backing[key] = value;
    //   }
    // =========================================================================

    public void Backing_DictionaryForStorage()
    {
        var stub = new Stubs.ICollection();

        // Pre-populate backing store
        stub.Indexer.Backing["existing"] = 100;

        ICollection<string, int> collection = stub;

        // Get from backing
        var val = collection["existing"]; // Returns 100

        // Set into backing
        collection["new"] = 200;
        var newVal = stub.Indexer.Backing["new"]; // 200
    }

    // =========================================================================
    // LastGetKey and LastSetEntry - Tracking
    // =========================================================================
    // DESIGN DECISION: Indexer interceptors track:
    // - LastGetKey: The key used in the most recent get
    // - LastSetEntry: A (Key, Value) tuple from the most recent set
    //
    // Note: The interface uses LastKey/LastEntry, but the GENERATOR adds
    // Get/Set prefixes to disambiguate when both get and set exist.
    //
    // GENERATOR BEHAVIOR:
    //   public string LastGetKey { get; private set; }
    //   public (string Key, int Value) LastSetEntry { get; private set; }
    // =========================================================================

    public void Tracking_LastGetKeyAndLastSetEntry()
    {
        var stub = new Stubs.ICollection();
        stub.Indexer.Backing["a"] = 1;
        stub.Indexer.Backing["b"] = 2;

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
    //
    // GENERATOR BEHAVIOR:
    //
    //   public class IndexerInterceptor
    //   {
    //       public void Get(Func<(int row, int col), double> callback) { ... }
    //       public void Set(Action<(int row, int col), double> callback) { ... }
    //
    //       public (int row, int col) LastGetKey { get; }
    //       public ((int row, int col) Key, double Value) LastSetEntry { get; }
    //   }
    //
    // DID NOT DO THIS: Flatten tuple into individual parameters for callbacks
    //
    // REJECTED PATTERN:
    //   stub.Indexer.Get((row, col) => row * 10.0 + col);  // Func<int, int, double>
    //
    // WHY NOT: Library interfaces use Func<TKey, TValue> where TKey is the
    // tuple. Flattening would create a mismatch between Get() and ThenGet()
    // callback signatures. Using the tuple key consistently avoids this.
    // =========================================================================

    public void MultiKeyIndexer_TupleKeys()
    {
        var stub = new Stubs.IMatrix();

        // Callback receives tuple key
        stub.Indexer.Get(key => key.row * 10.0 + key.col);

        IMatrix matrix = stub;
        var val = matrix[2, 3]; // Returns 23.0 (2*10 + 3)
    }

    public void MultiKeyIndexer_Backing()
    {
        var stub = new Stubs.IMatrix();

        // Backing uses tuple key
        stub.Indexer.Backing[(1, 2)] = 12.0;
        stub.Indexer.Backing[(3, 4)] = 34.0;

        IMatrix matrix = stub;
        var val = matrix[1, 2]; // Returns 12.0
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

    public void InitIndexer_Backing()
    {
        var stub = new Stubs.IInitIndexerCollection();

        // Init-only indexer still uses Backing for get
        stub.Indexer.Backing["key"] = 42;

        IInitIndexerCollection<string, int> collection = stub;
        var val = collection["key"]; // Returns 42
    }

    // =========================================================================
    // VerifyGet() and VerifySet()
    // =========================================================================
    // DESIGN DECISION: Like properties, indexers have separate verification
    // for get and set operations.
    // =========================================================================

    public void Verify_GetterAndSetter()
    {
        var stub = new Stubs.ICollection();
        stub.Indexer.Backing["test"] = 1;

        ICollection<string, int> collection = stub;

        _ = collection["test"];
        _ = collection["test"];
        collection["new"] = 42;

        stub.Indexer.VerifyGet(Called.Exactly(2));
        stub.Indexer.VerifySet(Called.Once);
    }

    // =========================================================================
    // COMMON MISTAKE: Using Get/Set with Backing
    // =========================================================================
    //
    // COMMON MISTAKE: Expecting Backing to work when Get/Set is configured
    //
    // When you set Get or Set, they OVERRIDE Backing behavior:
    //
    //   stub.Indexer.Backing["key"] = 100;
    //   stub.Indexer.Get((k) => 999);
    //   collection["key"]; // Returns 999, NOT 100!
    //
    // Backing is for simple scenarios. Get/Set are for custom behavior.
    // =========================================================================

    public void Backing_VsGet()
    {
        var stub = new Stubs.ICollection();

        // Backing alone works as expected
        stub.Indexer.Backing["key"] = 100;

        ICollection<string, int> collection = stub;
        var val1 = collection["key"]; // 100

        // But Get overrides Backing behavior
        stub.Indexer.Get((k) => 999);
        var val2 = collection["key"]; // 999 - Backing ignored!
    }
}
