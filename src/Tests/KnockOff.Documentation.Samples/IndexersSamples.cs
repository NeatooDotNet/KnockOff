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
// Backing Dictionary Samples
// =============================================================================

public class BackingDictionarySamples
{
    #region indexers-backing-basic
    [Fact]
    public void Backing_BasicUsage()
    {
        var stub = new UserCacheStub();

        // Populate the backing dictionary with test data
        stub.Indexer.Backing[1] = new User { Id = 1, Name = "Alice" };
        stub.Indexer.Backing[2] = new User { Id = 2, Name = "Bob" };

        IUserCache cache = stub;

        // Access via the interface returns values from backing dictionary
        var user1 = cache[1];
        var user2 = cache[2];

        Assert.Equal("Alice", user1?.Name);
        Assert.Equal("Bob", user2?.Name);
    }
    #endregion

    #region indexers-backing-multiple
    [Fact]
    public void Backing_MultipleEntries()
    {
        var stub = new ConfigStoreStub();

        // Pre-populate multiple configuration values
        stub.Indexer.Backing["ConnectionString"] = "Server=localhost;Database=Test";
        stub.Indexer.Backing["ApiKey"] = "abc123";
        stub.Indexer.Backing["Timeout"] = "30";
        stub.Indexer.Backing["MaxRetries"] = "3";

        IConfigStore config = stub;

        // All keys are accessible via the interface indexer
        Assert.Equal("Server=localhost;Database=Test", config["ConnectionString"]);
        Assert.Equal("abc123", config["ApiKey"]);
        Assert.Equal("30", config["Timeout"]);
        Assert.Equal("3", config["MaxRetries"]);
    }
    #endregion
}

// =============================================================================
// Dynamic Getter Samples
// =============================================================================

public class DynamicGetterSamples
{
    #region indexers-onget-computed
    [Fact]
    public void OnGet_ComputedValue()
    {
        var stub = new ConfigStoreStub();

        // OnGet computes values based on the key
        stub.Indexer.OnGet((key) => $"Value for {key}");

        IConfigStore config = stub;

        // Each access computes a fresh value from the key
        Assert.Equal("Value for ApiKey", config["ApiKey"]);
        Assert.Equal("Value for Timeout", config["Timeout"]);
    }
    #endregion

    #region indexers-onget-stateful
    [Fact]
    public void OnGet_StatefulBehavior()
    {
        var stub = new ProductInventoryStub();

        // Track inventory state with local variable
        var inventory = new Dictionary<string, int>
        {
            ["SKU-001"] = 10,
            ["SKU-002"] = 5
        };

        // OnGet checks the tracked state to determine return value
        stub.Indexer.OnGet((sku) => inventory.GetValueOrDefault(sku, 0));

        IProductInventory store = stub;

        // Indexer behavior depends on test state
        Assert.Equal(10, store["SKU-001"]);
        Assert.Equal(5, store["SKU-002"]);
        Assert.Equal(0, store["SKU-999"]); // Not in inventory

        // Modify state
        inventory["SKU-001"] = 8;

        // Indexer reflects updated state
        Assert.Equal(8, store["SKU-001"]);
    }
    #endregion
}

// =============================================================================
// Setter Interception Samples
// =============================================================================

public class SetterInterceptionSamples
{
    #region indexers-onset-tracking
    [Fact]
    public void OnSet_TrackingWrites()
    {
        var stub = new ConfigStoreStub();

        // Track all key-value pairs written to the indexer
        var writtenPairs = new List<(string key, string value)>();
        stub.Indexer.OnSet((key, value) =>
        {
            writtenPairs.Add((key, value));
        });

        IConfigStore config = stub;

        config["ApiKey"] = "secret123";
        config["Timeout"] = "60";
        config["MaxRetries"] = "5";

        // All writes are tracked for verification
        Assert.Equal(3, writtenPairs.Count);
        Assert.Contains(("ApiKey", "secret123"), writtenPairs);
        Assert.Contains(("Timeout", "60"), writtenPairs);
        Assert.Contains(("MaxRetries", "5"), writtenPairs);
    }
    #endregion

    #region indexers-onset-validation
    [Fact]
    public void OnSet_Validation()
    {
        var stub = new ProductInventoryStub();

        // OnSet validates keys and throws for invalid ones
        var validSkus = new HashSet<string> { "SKU-001", "SKU-002", "SKU-003" };
        stub.Indexer.OnSet((sku, quantity) =>
        {
            if (!validSkus.Contains(sku))
                throw new ArgumentException($"Invalid SKU: {sku}");
            if (quantity < 0)
                throw new ArgumentException("Quantity cannot be negative");
        });

        IProductInventory inventory = stub;

        // Valid key and value works
        inventory["SKU-001"] = 10;

        // Invalid key throws
        Assert.Throws<ArgumentException>(() => inventory["INVALID-SKU"] = 5);

        // Invalid value throws
        Assert.Throws<ArgumentException>(() => inventory["SKU-002"] = -1);
    }
    #endregion
}

// =============================================================================
// Verification Samples
// =============================================================================

public class VerificationSamples
{
    #region indexers-verify-access
    [Fact]
    public void Verify_IndexerAccess()
    {
        var stub = new ConfigStoreStub();
        stub.Indexer.Backing["ApiKey"] = "secret";

        IConfigStore config = stub;

        // Access the indexer
        _ = config["ApiKey"];
        _ = config["ApiKey"];
        config["Timeout"] = "30";

        // VerifyGet checks getter was called expected number of times
        stub.Indexer.VerifyGet(Times.Exactly(2));

        // VerifySet checks setter was called expected number of times
        stub.Indexer.VerifySet(Times.Once);
    }
    #endregion

    #region indexers-capture-last
    [Fact]
    public void CaptureLastAccess()
    {
        var stub = new ConfigStoreStub();
        stub.Indexer.Backing["First"] = "1";
        stub.Indexer.Backing["Second"] = "2";

        IConfigStore config = stub;

        // Read multiple keys
        _ = config["First"];
        _ = config["Second"];

        // LastGetKey captures the most recent getter key
        Assert.Equal("Second", stub.Indexer.LastGetKey);

        // Write to the indexer
        config["ApiKey"] = "secret";
        config["Timeout"] = "60";

        // LastSetEntry captures the most recent setter key-value pair
        Assert.NotNull(stub.Indexer.LastSetEntry);
        Assert.Equal("Timeout", stub.Indexer.LastSetEntry.Value.Key);
        Assert.Equal("60", stub.Indexer.LastSetEntry.Value.Value);
    }
    #endregion
}

// =============================================================================
// Sequence Samples
// =============================================================================

public class SequenceSamples
{
    #region indexers-ongetsequence-basic
    [Fact]
    public void OnGet_ThenGet_DifferentValuesPerAccess()
    {
        var stub = new ConfigStoreStub();

        // Configure sequence: first access returns "cached", second returns "fresh"
        stub.Indexer
            .OnGet((key) => "cached")
            .ThenGet((key) => "fresh");

        IConfigStore config = stub;

        // First access returns first value
        Assert.Equal("cached", config["Data"]);

        // Second access returns second value
        Assert.Equal("fresh", config["Data"]);
    }
    #endregion

    #region indexers-onset-then-sequence
    [Fact]
    public void OnSet_ThenSet_DifferentBehaviorPerWrite()
    {
        var stub = new ProductInventoryStub();

        var attemptCount = 0;

        // First write fails validation, second write succeeds
        stub.Indexer
            .OnSet((sku, qty) =>
            {
                attemptCount++;
                throw new InvalidOperationException("Service unavailable");
            })
            .ThenSet((sku, qty) =>
            {
                attemptCount++;
                // Second attempt succeeds
            });

        IProductInventory inventory = stub;

        // First write throws
        Assert.Throws<InvalidOperationException>(() => inventory["SKU-001"] = 10);

        // Second write succeeds
        inventory["SKU-001"] = 10;

        Assert.Equal(2, attemptCount);
    }
    #endregion
}

// =============================================================================
// Multiple Overloads Sample
// =============================================================================

public class MultipleOverloadsSamples
{
    #region indexers-multiple-overloads
    [Fact]
    public void MultipleIndexerOverloads()
    {
        var stub = new MultiKeyStoreStub();

        // Each indexer overload has its own interceptor
        // String key indexer: stub.Indexer.OfString
        // Int key indexer: stub.Indexer.OfInt32

        // Configure string indexer
        stub.Indexer.OfString.Backing["name"] = "Alice";
        stub.Indexer.OfString.Backing["email"] = "alice@example.com";

        // Configure int indexer
        stub.Indexer.OfInt32.Backing[0] = 100;
        stub.Indexer.OfInt32.Backing[1] = 200;

        IMultiKeyStore store = stub;

        // Access string indexer
        Assert.Equal("Alice", store["name"]);
        Assert.Equal("alice@example.com", store["email"]);

        // Access int indexer
        Assert.Equal(100, store[0]);
        Assert.Equal(200, store[1]);

        // Each overload tracks independently
        stub.Indexer.OfString.VerifyGet(Times.Exactly(2));
        stub.Indexer.OfInt32.VerifyGet(Times.Exactly(2));
    }
    #endregion
}

// =============================================================================
// Priority Sample
// =============================================================================

public class PrioritySamples
{
    #region indexers-priority
    [Fact]
    public void OnGet_TakesPrecedenceOverBacking()
    {
        var stub = new ConfigStoreStub();

        // First, populate the backing dictionary
        stub.Indexer.Backing["ApiKey"] = "from-backing";

        // Then set OnGet - it takes precedence over Backing
        stub.Indexer.OnGet((key) => "from-callback");

        IConfigStore config = stub;

        // OnGet callback value is returned, not backing value
        Assert.Equal("from-callback", config["ApiKey"]);
    }
    #endregion

    #region indexers-onset-with-backing
    [Fact]
    public void OnSet_WithBackingUpdate()
    {
        var stub = new ConfigStoreStub();

        var validationLog = new List<string>();
        stub.Indexer.OnSet((key, value) =>
        {
            // Custom validation
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Value cannot be empty");

            validationLog.Add(key);

            // Manually update backing so subsequent reads work
            stub.Indexer.Backing[key] = value;
        });

        IConfigStore config = stub;

        config["ApiKey"] = "secret123";

        // Validation was called
        Assert.Single(validationLog);

        // Backing was updated manually
        Assert.Equal("secret123", stub.Indexer.Backing["ApiKey"]);
    }
    #endregion
}

// =============================================================================
// Reset Sample
// =============================================================================

public class ResetSamples
{
    #region indexers-reset
    [Fact]
    public void Reset_ClearsTrackingPreservesBacking()
    {
        var stub = new ConfigStoreStub();

        // Setup: populate backing and access indexer
        stub.Indexer.Backing["ApiKey"] = "secret";
        stub.Indexer.Backing["Timeout"] = "30";

        IConfigStore config = stub;

        _ = config["ApiKey"];
        config["NewKey"] = "value";

        // Verify access counts before reset
        stub.Indexer.VerifyGet(Times.Once);
        stub.Indexer.VerifySet(Times.Once);
        Assert.Equal("ApiKey", stub.Indexer.LastGetKey);
        Assert.NotNull(stub.Indexer.LastSetEntry);

        // Reset clears tracking state
        stub.Indexer.Reset();

        // Tracking counts are cleared
        stub.Indexer.VerifyGet(Times.Never);
        stub.Indexer.VerifySet(Times.Never);
        Assert.Null(stub.Indexer.LastGetKey);
        Assert.Null(stub.Indexer.LastSetEntry);

        // Backing data is preserved
        Assert.Equal("secret", config["ApiKey"]);
        Assert.True(stub.Indexer.Backing.ContainsKey("Timeout"));
    }
    #endregion
}

// =============================================================================
// Complete Example
// =============================================================================

public class CompleteExampleTests
{
    #region indexers-complete-example
    [Fact]
    public void CompleteIndexerExample_AllPatterns()
    {
        // Scenario: Testing a cache service that loads users by ID

        var stub = new UserCacheStub();

        // 1. Backing Dictionary: Pre-populate known test data
        stub.Indexer.Backing[1] = new User { Id = 1, Name = "Alice", Email = "alice@example.com" };
        stub.Indexer.Backing[2] = new User { Id = 2, Name = "Bob", Email = "bob@example.com" };

        IUserCache cache = stub;

        // Basic access via backing dictionary
        var alice = cache[1];
        Assert.Equal("Alice", alice?.Name);

        // 2. OnGet: Configure dynamic behavior for cache misses
        stub.Indexer.OnGet((id) =>
        {
            // Simulate loading from database for unknown IDs
            if (id == 999)
                return new User { Id = 999, Name = "Dynamic User", Email = "dynamic@example.com" };
            return null;
        });

        var dynamicUser = cache[999];
        Assert.Equal("Dynamic User", dynamicUser?.Name);

        // Unknown IDs return null
        Assert.Null(cache[404]);

        // 3. Verification: Check access patterns
        stub.Indexer.VerifyGet(Times.AtLeast(3));
        Assert.Equal(404, stub.Indexer.LastGetKey);

        // 4. OnSet: Track cache updates
        var cacheUpdates = new List<(int id, User? user)>();
        stub.Indexer.OnSet((id, user) =>
        {
            cacheUpdates.Add((id, user));
        });

        cache[3] = new User { Id = 3, Name = "Charlie" };
        cache[4] = new User { Id = 4, Name = "Diana" };

        Assert.Equal(2, cacheUpdates.Count);
        stub.Indexer.VerifySet(Times.Exactly(2));

        // 5. Reset for next test phase
        stub.Indexer.Reset();
        stub.Indexer.VerifyGet(Times.Never);
        stub.Indexer.VerifySet(Times.Never);

        // Backing data still available after reset
        Assert.True(stub.Indexer.Backing.ContainsKey(1));
    }
    #endregion
}
