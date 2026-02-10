namespace KnockOff.Documentation.Samples.Indexers;

// =============================================================================
// Interfaces for Indexer Samples
// =============================================================================

public interface IUserCache
{
    User? this[int userId] { get; set; }
}

public interface IConfigStore
{
    string this[string key] { get; set; }
}

public interface IProductInventory
{
    int this[string sku] { get; set; }
}

public interface IUserLookup
{
    User? this[int id] { get; }
}

public interface IMultiKeyStore
{
    string this[string key] { get; set; }
    int this[int index] { get; }
}

public interface IMatrix
{
    double this[int row, int col] { get; set; }
}

[KnockOff]
public partial class MatrixStub : IMatrix { }

// =============================================================================
// Stubs for Indexer Samples
// =============================================================================

[KnockOff]
public partial class UserCacheStub : IUserCache { }

[KnockOff]
public partial class ConfigStoreStub : IConfigStore { }

[KnockOff]
public partial class ProductInventoryStub : IProductInventory { }

[KnockOff]
public partial class UserLookupStub : IUserLookup { }

[KnockOff]
public partial class MultiKeyStoreStub : IMultiKeyStore { }

// =============================================================================
// Per-Key Returns Samples
// =============================================================================

public class PerKeyReturnsSamples
{
    [Fact]
    public void PerKey_BasicUsage()
    {
        var stub = new UserCacheStub();

        #region indexers-perkey-basic
        // Configure per-key return values
        stub.Indexer[1].Returns(new User { Id = 1, Name = "Alice" });
        stub.Indexer[2].Returns(new User { Id = 2, Name = "Bob" });
        #endregion

        IUserCache cache = stub;

        var user1 = cache[1];
        var user2 = cache[2];

        Assert.Equal("Alice", user1?.Name);
        Assert.Equal("Bob", user2?.Name);
    }

    [Fact]
    public void PerKey_MultipleEntries()
    {
        var stub = new ConfigStoreStub();

        #region indexers-perkey-multiple
        // Pre-populate multiple configuration values
        stub.Indexer["ConnectionString"].Returns("Server=localhost;Database=Test");
        stub.Indexer["ApiKey"].Returns("abc123");
        stub.Indexer["Timeout"].Returns("30");
        stub.Indexer["MaxRetries"].Returns("3");
        #endregion

        IConfigStore config = stub;

        Assert.Equal("Server=localhost;Database=Test", config["ConnectionString"]);
        Assert.Equal("abc123", config["ApiKey"]);
        Assert.Equal("30", config["Timeout"]);
        Assert.Equal("3", config["MaxRetries"]);
    }
}

// =============================================================================
// Dynamic Getter Samples
// =============================================================================

public class DynamicGetterSamples
{
    [Fact]
    public void OnGet_ComputedValue()
    {
        var stub = new ConfigStoreStub();

        #region indexers-onget-computed
        // Get computes values based on the key
        stub.Indexer.Get((key) => $"Value for {key}");
        #endregion

        IConfigStore config = stub;

        Assert.Equal("Value for ApiKey", config["ApiKey"]);
        Assert.Equal("Value for Timeout", config["Timeout"]);
    }

    [Fact]
    public void OnGet_StatefulBehavior()
    {
        var stub = new ProductInventoryStub();

        var inventory = new Dictionary<string, int>
        {
            ["SKU-001"] = 10,
            ["SKU-002"] = 5
        };

        #region indexers-onget-stateful
        // Get checks external state to determine return value
        stub.Indexer.Get((sku) => inventory.GetValueOrDefault(sku, 0));
        #endregion

        IProductInventory store = stub;

        Assert.Equal(10, store["SKU-001"]);
        Assert.Equal(5, store["SKU-002"]);
        Assert.Equal(0, store["SKU-999"]);

        inventory["SKU-001"] = 8;

        Assert.Equal(8, store["SKU-001"]);
    }
}

// =============================================================================
// Setter Interception Samples
// =============================================================================

public class SetterInterceptionSamples
{
    [Fact]
    public void OnSet_TrackingWrites()
    {
        var stub = new ConfigStoreStub();
        var writtenPairs = new List<(string key, string value)>();

        #region indexers-onset-tracking
        // Set intercepts writes for tracking
        stub.Indexer.Set((key, value) => writtenPairs.Add((key, value)));
        #endregion

        IConfigStore config = stub;

        config["ApiKey"] = "secret123";
        config["Timeout"] = "60";
        config["MaxRetries"] = "5";

        Assert.Equal(3, writtenPairs.Count);
        Assert.Contains(("ApiKey", "secret123"), writtenPairs);
        Assert.Contains(("Timeout", "60"), writtenPairs);
        Assert.Contains(("MaxRetries", "5"), writtenPairs);
    }

    [Fact]
    public void OnSet_Validation()
    {
        var stub = new ProductInventoryStub();
        var validSkus = new HashSet<string> { "SKU-001", "SKU-002", "SKU-003" };

        #region indexers-onset-validation
        // Set validates and throws for invalid keys or values
        stub.Indexer.Set((sku, quantity) =>
        {
            if (!validSkus.Contains(sku))
                throw new ArgumentException($"Invalid SKU: {sku}");
            if (quantity < 0)
                throw new ArgumentException("Quantity cannot be negative");
        });
        #endregion

        IProductInventory inventory = stub;

        inventory["SKU-001"] = 10;

        Assert.Throws<ArgumentException>(() => inventory["INVALID-SKU"] = 5);
        Assert.Throws<ArgumentException>(() => inventory["SKU-002"] = -1);
    }
}

// =============================================================================
// Verification Samples
// =============================================================================

public class VerificationSamples
{
    [Fact]
    public void Verify_IndexerAccess()
    {
        var stub = new ConfigStoreStub();
        stub.Indexer["ApiKey"].Returns("secret");

        IConfigStore config = stub;

        _ = config["ApiKey"];
        _ = config["ApiKey"];
        config["Timeout"] = "30";

        #region indexers-verify-access
        // Verify indexer get/set call counts
        stub.Indexer.VerifyGet(Called.Exactly(2));
        stub.Indexer.VerifySet(Called.Once);
        #endregion
    }

    [Fact]
    public void CaptureLastAccess()
    {
        var stub = new ConfigStoreStub();
        stub.Indexer["First"].Returns("1");
        stub.Indexer["Second"].Returns("2");

        IConfigStore config = stub;

        _ = config["First"];
        _ = config["Second"];
        config["ApiKey"] = "secret";
        config["Timeout"] = "60";

        #region indexers-capture-last
        // LastGetKey captures the most recent getter key
        Assert.Equal("Second", stub.Indexer.LastGetKey);

        // LastSetEntry captures the most recent setter key-value pair
        Assert.Equal("Timeout", stub.Indexer.LastSetEntry!.Value.Key);
        Assert.Equal("60", stub.Indexer.LastSetEntry.Value.Value);
        #endregion
    }
}

// =============================================================================
// Sequence Samples
// =============================================================================

public class SequenceSamples
{
    [Fact]
    public void OnGet_ThenGet_DifferentValuesPerAccess()
    {
        var stub = new ConfigStoreStub();

        #region indexers-ongetsequence-basic
        // Sequence: first access returns "cached", second returns "fresh"
        stub.Indexer
            .Get((key) => "cached")
            .ThenGet((key) => "fresh");
        #endregion

        IConfigStore config = stub;

        Assert.Equal("cached", config["Data"]);
        Assert.Equal("fresh", config["Data"]);
    }

    [Fact]
    public void OnSet_ThenSet_DifferentBehaviorPerWrite()
    {
        var stub = new ProductInventoryStub();
        var attemptCount = 0;

        #region indexers-onset-then-sequence
        // Sequence: first write fails, second succeeds
        stub.Indexer
            .Set((sku, qty) => { attemptCount++; throw new InvalidOperationException("Service unavailable"); })
            .ThenSet((sku, qty) => { attemptCount++; });
        #endregion

        IProductInventory inventory = stub;

        Assert.Throws<InvalidOperationException>(() => inventory["SKU-001"] = 10);
        inventory["SKU-001"] = 10;

        Assert.Equal(2, attemptCount);
    }
}

// =============================================================================
// Multiple Overloads Sample
// =============================================================================

public class MultipleOverloadsSamples
{
    [Fact]
    public void MultipleIndexerOverloads()
    {
        var stub = new MultiKeyStoreStub();

        #region indexers-multiple-overloads
        // C# indexer overloads resolve by key type -- no OfXxx needed
        stub.Indexer["name"].Returns("Alice");
        stub.Indexer[0].Returns(100);
        #endregion

        IMultiKeyStore store = stub;

        Assert.Equal("Alice", store["name"]);
        Assert.Equal(100, store[0]);

        stub.Indexer.VerifyGet(Called.Exactly(2));
    }
}

// =============================================================================
// Priority Sample
// =============================================================================

public class PrioritySamples
{
    [Fact]
    public void PerKey_TakesPrecedenceOverGetCallback()
    {
        var stub = new ConfigStoreStub();

        #region indexers-priority
        // Per-key Returns takes precedence over all-keys Get callback
        stub.Indexer["ApiKey"].Returns("from-per-key");
        stub.Indexer.Get((key) => "from-callback");
        #endregion

        IConfigStore config = stub;

        Assert.Equal("from-per-key", config["ApiKey"]);
        Assert.Equal("from-callback", config["Other"]);
    }

    [Fact]
    public void PerKeyWithFallback()
    {
        var stub = new ConfigStoreStub();

        #region indexers-perkey-with-fallback
        // Per-key for specific keys, Get callback as fallback for others
        stub.Indexer["ApiKey"].Returns("secret123");
        stub.Indexer.Get((key) => $"default-{key}");
        #endregion

        IConfigStore config = stub;

        Assert.Equal("secret123", config["ApiKey"]);
        Assert.Equal("default-Timeout", config["Timeout"]);
    }
}

// =============================================================================
// Reset Sample
// =============================================================================

public class ResetSamples
{
    [Fact]
    public void Reset_ClearsTrackingPreservesPerKeyAndCallbacks()
    {
        var stub = new ConfigStoreStub();

        stub.Indexer["ApiKey"].Returns("secret");
        stub.Indexer["Timeout"].Returns("30");

        IConfigStore config = stub;

        _ = config["ApiKey"];
        config["NewKey"] = "value";

        stub.Indexer.VerifyGet(Called.Once);
        stub.Indexer.VerifySet(Called.Once);

        #region indexers-reset
        // Reset clears tracking but preserves per-key Returns and callbacks
        stub.Indexer.Reset();
        #endregion

        stub.Indexer.VerifyGet(Called.Never);
        stub.Indexer.VerifySet(Called.Never);
        Assert.Null(stub.Indexer.LastGetKey);
        Assert.Null(stub.Indexer.LastSetEntry);

        // Per-key Returns still works after reset
        Assert.Equal("secret", config["ApiKey"]);
    }
}

// =============================================================================
// Complete Example
// =============================================================================

public class CompleteExampleTests
{
    [Fact]
    public void CompleteIndexerExample_AllPatterns()
    {
        var stub = new UserCacheStub();
        var cacheUpdates = new List<(int id, User? user)>();

        #region indexers-complete-example
        // 1. Per-key Returns: Pre-configure specific keys
        stub.Indexer[1].Returns(new User { Id = 1, Name = "Alice", Email = "alice@example.com" });

        // 2. Get: Dynamic fallback for unconfigured keys
        stub.Indexer.Get((id) => id == 999
            ? new User { Id = 999, Name = "Dynamic User", Email = "dynamic@example.com" }
            : null);

        // 3. Set: Track writes
        stub.Indexer.Set((id, user) => cacheUpdates.Add((id, user)));
        #endregion

        IUserCache cache = stub;

        // Per-key Returns wins for key 1
        var alice = cache[1];
        Assert.Equal("Alice", alice?.Name);

        // Get callback handles key 999
        var dynamicUser = cache[999];
        Assert.Equal("Dynamic User", dynamicUser?.Name);

        // Get callback returns null for unknown keys
        Assert.Null(cache[404]);

        stub.Indexer.VerifyGet(Called.AtLeast(3));
        Assert.Equal(404, stub.Indexer.LastGetKey);

        cache[3] = new User { Id = 3, Name = "Charlie" };
        cache[4] = new User { Id = 4, Name = "Diana" };

        Assert.Equal(2, cacheUpdates.Count);
        stub.Indexer.VerifySet(Called.Exactly(2));

        stub.Indexer.Reset();
        stub.Indexer.VerifyGet(Called.Never);
        stub.Indexer.VerifySet(Called.Never);
    }
}

// =============================================================================
// Indexer Reference Samples (for indexers.md)
// =============================================================================

public class IndexerRefPerKeyTests
{
    [Fact]
    public void Indexers_PerKeyBasic()
    {
        var stub = new ConfigStoreStub();

        #region indexers-ref-perkey
        // Configure specific keys to return specific values
        stub.Indexer["existing"].Returns("100");
        stub.Indexer["special"].Returns("999");

        IConfigStore collection = stub;
        var val = collection["existing"]; // "100"
        var val2 = collection["special"]; // "999"
        #endregion

        Assert.Equal("100", val);
        Assert.Equal("999", val2);
    }

    [Fact]
    public void Indexers_AllKeysCallback()
    {
        var stub = new ConfigStoreStub();

        #region indexers-ref-allkeys-get
        // Get callback receives the key
        stub.Indexer.Get((key) => key.Length.ToString());

        IConfigStore collection = stub;
        var len1 = collection["hello"]; // "5"
        var len2 = collection["hi"];    // "2"
        #endregion

        Assert.Equal("5", len1);
        Assert.Equal("2", len2);
    }

    [Fact]
    public void Indexers_AllKeysSet()
    {
        var stub = new ConfigStoreStub();
        var storage = new Dictionary<string, string>();

        #region indexers-ref-allkeys-set
        // Set callback receives key AND value
        stub.Indexer.Set((key, value) => storage[key] = value);
        #endregion

        IConfigStore collection = stub;
        collection["one"] = "1";

        Assert.Equal("1", storage["one"]);
    }

    [Fact]
    public void Indexers_PerKeyWithFallback()
    {
        var stub = new ConfigStoreStub();

        #region indexers-ref-perkey-fallback
        stub.Indexer["special"].Returns("999");     // Per-key: always "999"
        stub.Indexer.Get((key) => key.Length.ToString());     // All-keys: fallback

        IConfigStore collection = stub;
        var r1 = collection["special"]; // "999" (per-key wins)
        var r2 = collection["hello"];   // "5" (callback fallback)
        #endregion

        Assert.Equal("999", r1);
        Assert.Equal("5", r2);
    }
}

public class IndexerRefMultiParamTests
{
    [Fact]
    public void Indexers_MultiParam_PerKey()
    {
        var stub = new MatrixStub();

        #region indexers-ref-multi-perkey
        // Flattened -- natural C# indexer syntax
        stub.Indexer[1, 2].Returns(12.0);
        stub.Indexer[3, 4].Returns(34.0);

        IMatrix matrix = stub;
        var val = matrix[1, 2]; // 12.0
        #endregion

        Assert.Equal(12.0, val);
    }

    [Fact]
    public void Indexers_MultiParam_AllKeysCallback()
    {
        var stub = new MatrixStub();

        #region indexers-ref-multi-allkeys
        // Get callback receives named tuple
        stub.Indexer.Get(key => key.row * 10.0 + key.col);

        IMatrix matrix = stub;
        var val = matrix[2, 3]; // 23.0
        #endregion

        Assert.Equal(23.0, val);
    }
}

public class IndexerRefTrackingTests
{
    [Fact]
    public void Indexers_Tracking()
    {
        var stub = new ConfigStoreStub();
        stub.Indexer.Get((k) => "value");

        IConfigStore collection = stub;

        #region indexers-ref-tracking
        _ = collection["a"];
        _ = collection["b"];
        var lastKey = stub.Indexer.LastGetKey; // "b"

        collection["x"] = "10";
        collection["y"] = "20";
        var lastEntry = stub.Indexer.LastSetEntry; // ("y", "20")
        #endregion

        Assert.Equal("b", lastKey);
        Assert.Equal("y", lastEntry!.Value.Key);
    }
}

public class IndexerRefThenDefaultTests
{
    [Fact]
    public void Indexers_ThenDefault()
    {
        var stub = new ConfigStoreStub();

        #region indexers-ref-thendefault
        stub.Indexer.Get((k) => k.Length.ToString())
            .ThenGet((k) => "100")
            .ThenDefault();  // null after exhaustion

        IConfigStore collection = stub;
        var r1 = collection["hello"]; // "5"
        var r2 = collection["world"]; // "100"
        var r3 = collection["foo"];   // null (default)
        #endregion

        Assert.Equal("5", r1);
        Assert.Equal("100", r2);
        Assert.Null(r3);
    }
}
