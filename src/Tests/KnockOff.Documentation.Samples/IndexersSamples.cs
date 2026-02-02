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
    [Fact]
    public void Backing_BasicUsage()
    {
        var stub = new UserCacheStub();

        #region indexers-backing-basic
        // Populate the backing dictionary with test data
        stub.Indexer.Backing[1] = new User { Id = 1, Name = "Alice" };
        stub.Indexer.Backing[2] = new User { Id = 2, Name = "Bob" };
        #endregion

        IUserCache cache = stub;

        var user1 = cache[1];
        var user2 = cache[2];

        Assert.Equal("Alice", user1?.Name);
        Assert.Equal("Bob", user2?.Name);
    }

    [Fact]
    public void Backing_MultipleEntries()
    {
        var stub = new ConfigStoreStub();

        #region indexers-backing-multiple
        // Pre-populate multiple configuration values
        stub.Indexer.Backing["ConnectionString"] = "Server=localhost;Database=Test";
        stub.Indexer.Backing["ApiKey"] = "abc123";
        stub.Indexer.Backing["Timeout"] = "30";
        stub.Indexer.Backing["MaxRetries"] = "3";
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
        // OnGet computes values based on the key
        stub.Indexer.OnGet((key) => $"Value for {key}");
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
        // OnGet checks external state to determine return value
        stub.Indexer.OnGet((sku) => inventory.GetValueOrDefault(sku, 0));
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
        // OnSet intercepts writes for tracking
        stub.Indexer.OnSet((key, value) => writtenPairs.Add((key, value)));
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
        // OnSet validates and throws for invalid keys or values
        stub.Indexer.OnSet((sku, quantity) =>
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
        stub.Indexer.Backing["ApiKey"] = "secret";

        IConfigStore config = stub;

        _ = config["ApiKey"];
        _ = config["ApiKey"];
        config["Timeout"] = "30";

        #region indexers-verify-access
        // Verify indexer get/set call counts
        stub.Indexer.VerifyGet(Times.Exactly(2));
        stub.Indexer.VerifySet(Times.Once);
        #endregion
    }

    [Fact]
    public void CaptureLastAccess()
    {
        var stub = new ConfigStoreStub();
        stub.Indexer.Backing["First"] = "1";
        stub.Indexer.Backing["Second"] = "2";

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
            .OnGet((key) => "cached")
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
            .OnSet((sku, qty) => { attemptCount++; throw new InvalidOperationException("Service unavailable"); })
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
        // Each overload has its own interceptor: Indexer.OfString, Indexer.OfInt32
        stub.Indexer.OfString.Backing["name"] = "Alice";
        stub.Indexer.OfInt32.Backing[0] = 100;
        #endregion

        IMultiKeyStore store = stub;

        Assert.Equal("Alice", store["name"]);
        Assert.Equal(100, store[0]);

        stub.Indexer.OfString.VerifyGet(Times.Once);
        stub.Indexer.OfInt32.VerifyGet(Times.Once);
    }
}

// =============================================================================
// Priority Sample
// =============================================================================

public class PrioritySamples
{
    [Fact]
    public void OnGet_TakesPrecedenceOverBacking()
    {
        var stub = new ConfigStoreStub();

        #region indexers-priority
        // OnGet takes precedence over Backing dictionary
        stub.Indexer.Backing["ApiKey"] = "from-backing";
        stub.Indexer.OnGet((key) => "from-callback");
        #endregion

        IConfigStore config = stub;

        Assert.Equal("from-callback", config["ApiKey"]);
    }

    [Fact]
    public void OnSet_WithBackingUpdate()
    {
        var stub = new ConfigStoreStub();
        var validationLog = new List<string>();

        #region indexers-onset-with-backing
        // OnSet must manually update Backing if reads should reflect writes
        stub.Indexer.OnSet((key, value) =>
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Value cannot be empty");
            validationLog.Add(key);
            stub.Indexer.Backing[key] = value;
        });
        #endregion

        IConfigStore config = stub;

        config["ApiKey"] = "secret123";

        Assert.Single(validationLog);
        Assert.Equal("secret123", stub.Indexer.Backing["ApiKey"]);
    }
}

// =============================================================================
// Reset Sample
// =============================================================================

public class ResetSamples
{
    [Fact]
    public void Reset_ClearsTrackingPreservesBacking()
    {
        var stub = new ConfigStoreStub();

        stub.Indexer.Backing["ApiKey"] = "secret";
        stub.Indexer.Backing["Timeout"] = "30";

        IConfigStore config = stub;

        _ = config["ApiKey"];
        config["NewKey"] = "value";

        stub.Indexer.VerifyGet(Times.Once);
        stub.Indexer.VerifySet(Times.Once);

        #region indexers-reset
        // Reset clears tracking but preserves Backing and callbacks
        stub.Indexer.Reset();
        #endregion

        stub.Indexer.VerifyGet(Times.Never);
        stub.Indexer.VerifySet(Times.Never);
        Assert.Null(stub.Indexer.LastGetKey);
        Assert.Null(stub.Indexer.LastSetEntry);

        Assert.Equal("secret", config["ApiKey"]);
        Assert.True(stub.Indexer.Backing.ContainsKey("Timeout"));
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
        // 1. Backing: Pre-populate test data
        stub.Indexer.Backing[1] = new User { Id = 1, Name = "Alice", Email = "alice@example.com" };

        // 2. OnGet: Compute values dynamically
        stub.Indexer.OnGet((id) => id == 999
            ? new User { Id = 999, Name = "Dynamic User", Email = "dynamic@example.com" }
            : null);

        // 3. OnSet: Track writes
        stub.Indexer.OnSet((id, user) => cacheUpdates.Add((id, user)));
        #endregion

        IUserCache cache = stub;

        var alice = cache[1];
        Assert.Null(alice); // OnGet takes precedence, returns null for id=1

        var dynamicUser = cache[999];
        Assert.Equal("Dynamic User", dynamicUser?.Name);

        Assert.Null(cache[404]);

        stub.Indexer.VerifyGet(Times.AtLeast(3));
        Assert.Equal(404, stub.Indexer.LastGetKey);

        cache[3] = new User { Id = 3, Name = "Charlie" };
        cache[4] = new User { Id = 4, Name = "Diana" };

        Assert.Equal(2, cacheUpdates.Count);
        stub.Indexer.VerifySet(Times.Exactly(2));

        stub.Indexer.Reset();
        stub.Indexer.VerifyGet(Times.Never);
        stub.Indexer.VerifySet(Times.Never);
    }
}
