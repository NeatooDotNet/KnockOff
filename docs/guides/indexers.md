# Indexer Configuration Guide

Indexers in KnockOff work similarly to properties but with key-based access. Each interface indexer gets a corresponding interceptor that maintains a backing dictionary, tracks access, and supports custom get/set callbacks.

**Quick reference:** For simple test data scenarios, use the `Backing` dictionary. For dynamic or computed values, use `OnGet` callbacks. For write validation or tracking, use `OnSet` callbacks.

---

## Configuration Approaches

Choose your configuration approach based on test requirements:

**Backing Dictionary (Recommended for Test Data)**
- Populate `Indexer.Backing` with test data before running test
- Use when the indexer should behave like a standard dictionary
- Simple, readable, and covers most test scenarios
- Example: Pre-loading a cache stub with known user IDs

**Dynamic Callbacks (For Complex Scenarios)**
- Set `Indexer.OnGet` to compute values at access time
- Set `Indexer.OnSet` to intercept and validate writes
- Use when values depend on state, validation, or need computed behavior
- Example: Simulating cache misses, validation failures, or retry logic

---

## Backing Dictionary (Recommended for Test Data)

The simplest way to configure an indexer is to populate the backing dictionary before your test runs.

<!-- snippet: indexers-backing-basic -->
```cs
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
```
<!-- endSnippet -->

When the indexer is accessed via the interface, KnockOff uses the backing dictionary by default:
- **Get**: Returns `Backing[key]` (throws `KeyNotFoundException` if key doesn't exist)
- **Set**: Stores to `Backing[key]`

<!-- snippet: indexers-backing-multiple -->
```cs
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
```
<!-- endSnippet -->

**When to use Backing:**
- Pre-populating repository stub data
- Configuring lookup tables or caches
- Setting up test fixtures with known data
- Any scenario where the indexer should behave like a dictionary

---

## Dynamic Getters

Use `OnGet` when an indexer's value should be computed at access time based on the key.

<!-- snippet: indexers-onget-computed -->
```cs
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
```
<!-- endSnippet -->

OnGet receives the key as a parameter and returns the value:

<!-- snippet: indexers-onget-stateful -->
```cs
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
```
<!-- endSnippet -->

**When to use OnGet:**
- Computed values based on the key
- Simulating cache behavior (hit vs. miss)
- Testing lazy loading or factory patterns
- State-dependent lookups

**Note:** `OnGet` returns `IIndexerGetTracking<TKey, TValue>` which supports verification and sequence methods.

---

## Setter Interception

Use `OnSet` to intercept indexer writes. This allows tracking writes or validating keys and values during tests.

<!-- snippet: indexers-onset-tracking -->
```cs
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
```
<!-- endSnippet -->

You can also use `OnSet` to simulate validation logic:

<!-- snippet: indexers-onset-validation -->
```cs
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
```
<!-- endSnippet -->

**When to use OnSet:**
- Tracking all key-value pairs written
- Simulating validation failures
- Testing how code handles indexer setter exceptions
- Verifying the sequence of indexer writes

**Note:** `OnSet` returns `IIndexerSetTracking<TKey, TValue>` which supports verification and sequence methods.

---

## Verifying Indexer Access

Indexer interceptors support verification and tracking similar to properties and methods.

<!-- snippet: indexers-verify-access -->
```cs
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
```
<!-- endSnippet -->

### Capturing Last Access

<!-- snippet: indexers-capture-last -->
```cs
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
```
<!-- endSnippet -->

**Verification methods:**
- `VerifyGet(Times)` - Verify indexer getter was called specified number of times
- `VerifySet(Times)` - Verify indexer setter was called specified number of times

**Inspection properties:**
- `LastGetKey` - The key from the most recent getter call (null if never accessed)
- `LastSetEntry` - Nullable KeyValuePair of the most recent setter call (null if never set)
- `Backing` - The backing dictionary (read/write access for test setup)

---

## Sequence Behavior

Use sequences when an indexer should return different values for the same key across multiple reads, or react differently to multiple writes.

### Get Sequences

When you need an indexer to return different values on successive reads of the same key, use `OnGet().ThenGet()`:

<!-- snippet: indexers-ongetsequence-basic -->
```cs
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
```
<!-- endSnippet -->

**When to use get sequences:**
- Testing cache invalidation (first hit returns cached, second returns fresh)
- Simulating retry logic with changing data
- Testing eventual consistency scenarios

### Set Sequences

When you need different behavior for successive indexer writes, use `OnSet().ThenSet()`:

<!-- snippet: indexers-onset-then-sequence -->
```cs
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
```
<!-- endSnippet -->

**When to use set sequences:**
- Testing validation that changes over time
- Simulating connection failures then recovery
- Testing retry logic with different outcomes

### Sequence vs. Single Callbacks

| Use Case | Use This | Why |
|----------|----------|-----|
| Indexer uses backing dictionary | `Backing[key] = value` | Simple, standard dictionary behavior |
| Indexer computes values from keys | `OnGet((key) => computed)` | Key-based computation |
| Indexer returns different values per access | `OnGet((k) => v1).ThenGet((k) => v2)` | Different values on successive reads |
| Indexer validates writes | `OnSet((k, v) => Validate(k, v))` | Custom validation logic |
| Indexer validation changes per write | `OnSet((k, v) => check1).ThenSet((k, v) => check2)` | Different behavior per write |

---

## Multiple Indexer Overloads

When an interface has multiple indexer overloads (different key types), KnockOff generates separate interceptor properties:

<!-- snippet: indexers-multiple-overloads -->
```cs
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
```
<!-- endSnippet -->

Overloads use type-based naming in the order they appear in the interface definition:
- String key indexer: `stub.Indexer.OfString`
- Int32 key indexer: `stub.Indexer.OfInt32`

Each overload maintains its own backing dictionary and configuration.

---

## OnGet/OnSet vs. Backing Priority

When both `Backing` and `OnGet`/`OnSet` are configured:
- **OnGet takes precedence**: Callback return value is used instead of `Backing[key]`
- **OnSet takes precedence**: Callback is invoked, `Backing` is NOT updated automatically

<!-- snippet: indexers-priority -->
```cs
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
```
<!-- endSnippet -->

**Design principle:** Callbacks override default backing dictionary behavior. If you need to both execute custom logic AND update the backing dictionary, your callback must explicitly write to `Backing`:

<!-- snippet: indexers-onset-with-backing -->
```cs
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
```
<!-- endSnippet -->

---

## Resetting Indexers

Calling `Reset()` on an indexer interceptor clears all counters and callbacks but **preserves the Backing dictionary**.

<!-- snippet: indexers-reset -->
```cs
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
```
<!-- endSnippet -->

**Reset behavior:** Calling `Reset()` clears all tracking counters, `LastGetKey`, `LastSetEntry`, and resets sequence position to the beginning. However, callbacks (`OnGet`, `OnSet`), sequence configurations, and the `Backing` dictionary are all preserved. This allows you to verify behavior, reset tracking state, and re-run the same test scenario without reconfiguring callbacks.

---

## Decision Guide

Choose your configuration approach based on the test scenario:

| Scenario | Use This | Example |
|----------|----------|---------|
| Indexer should return fixed test data | `Backing` | `stub.Indexer.Backing[1] = user1;` |
| Indexer computes values from keys | `OnGet` | `stub.Cache.OnGet((id) => LoadById(id));` |
| Indexer returns different values per access | `OnGet().ThenGet()` | `stub.Data.OnGet((k) => v1).ThenGet((k) => v2);` |
| Track all writes to indexer | `OnSet` | `stub.Store.OnSet((k, v) => log.Add((k, v)));` |
| Simulate validation in indexer | `OnSet` | `stub.Config.OnSet((k, v) => Validate(k));` |
| Indexer validation changes per write | `OnSet().ThenSet()` | `stub.Db.OnSet((k, v) => Fail()).ThenSet((k, v) => Ok());` |
| Verify indexer was accessed | Verification | `stub.Indexer.VerifyGet(Times.Once);` |
| Verify last key written | Verification | `Assert.Equal(42, stub.Indexer.LastGetKey);` |

---

## Complete Example

This example demonstrates all indexer configuration approaches in a realistic test scenario.

<!-- snippet: indexers-complete-example -->
```cs
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
```
<!-- endSnippet -->

---

## Key Takeaways

1. **Start with Backing** - It covers most scenarios and behaves like a standard dictionary
2. **Use OnGet for computed values** - Key-dependent or state-dependent returns
3. **Use OnSet for tracking** - When you need to verify writes or simulate validation
4. **Use sequences for changing behavior** - `OnGet().ThenGet()` / `OnSet().ThenSet()` when values or behavior differ across calls
5. **OnGet/OnSet override Backing** - Callbacks take precedence over dictionary lookups
6. **Reset() preserves Backing and callbacks** - Clears tracking state but not configuration
7. **Verify access patterns** - Use `VerifyGet()` and `VerifySet()` like property verification
8. **Multiple overloads** - Each indexer signature gets its own interceptor (OfString, OfInt32, etc.)

---

**Next Steps:**
- [Property Configuration Guide](properties.md) - Configure property behavior and callbacks
- [Method Configuration Guide](methods.md) - Configure method behavior and callbacks
- [Verification Patterns](verification.md) - Assert on stub interactions
- [Interceptor API Reference](../reference/interceptor-api.md) - Complete interceptor API documentation

---

**UPDATED:** 2026-01-25
