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
// Note: [KnockOff<IMatrix>] is commented out due to a known limitation with
// multi-key indexers (tuple key types). See Multi-Key Indexers section below.
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
    // Multi-Key Indexers (Tuple Keys) - KNOWN LIMITATION
    // =========================================================================
    // DESIGN DECISION: Multi-key indexers like this[int row, int col] use
    // tuple keys internally. The IndexerContainer uses (TKey1, TKey2) as TKey.
    //
    // KNOWN LIMITATION: Multi-key indexers have a
    // generator bug where ThenGet/ThenSet sequence methods are not correctly
    // generated for tuple key types.
    //
    // EXPECTED GENERATOR BEHAVIOR (when fixed):
    //
    //   public class IndexerInterceptor : IndexerContainer<(int row, int col), double>
    //   {
    //       public void Get(Func<int, int, double> callback) { ... }
    //       public void Set(Action<int, int, double> callback) { ... }
    //
    //       public (int row, int col) LastGetKey { get; }
    //       public ((int row, int col) Key, double Value) LastSetEntry { get; }
    //   }
    //
    // The callbacks should flatten the tuple for natural syntax.
    //
    // WORKAROUND: Use a wrapper type instead of multi-key indexer, or use
    // a single-key indexer with a custom key type.
    // =========================================================================

    // Multi-key indexer example commented out due to generator limitation
    // public void MultiKeyIndexer_TupleKeys()
    // {
    //     var stub = new Stubs.IMatrix();
    //     stub.Indexer.Get((row, col) => row * 10.0 + col);
    //     IMatrix matrix = stub;
    //     var val = matrix[2, 3]; // Returns 23.0 (2*10 + 3)
    // }

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

        stub.Indexer.VerifyGet(Times.Exactly(2));
        stub.Indexer.VerifySet(Times.Once);
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
