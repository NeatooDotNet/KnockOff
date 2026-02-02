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
// Populate the backing dictionary with test data
stub.Indexer.Backing[1] = new User { Id = 1, Name = "Alice" };
stub.Indexer.Backing[2] = new User { Id = 2, Name = "Bob" };
```
<!-- endSnippet -->

When the indexer is accessed via the interface, KnockOff uses the backing dictionary by default:
- **Get**: Returns `Backing[key]` (throws `KeyNotFoundException` if key doesn't exist)
- **Set**: Stores to `Backing[key]`

<!-- snippet: indexers-backing-multiple -->
```cs
// Pre-populate multiple configuration values
stub.Indexer.Backing["ConnectionString"] = "Server=localhost;Database=Test";
stub.Indexer.Backing["ApiKey"] = "abc123";
stub.Indexer.Backing["Timeout"] = "30";
stub.Indexer.Backing["MaxRetries"] = "3";
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
// OnGet computes values based on the key
stub.Indexer.OnGet((key) => $"Value for {key}");
```
<!-- endSnippet -->

OnGet receives the key as a parameter and returns the value:

<!-- snippet: indexers-onget-stateful -->
```cs
// OnGet checks external state to determine return value
stub.Indexer.OnGet((sku) => inventory.GetValueOrDefault(sku, 0));
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
// OnSet intercepts writes for tracking
stub.Indexer.OnSet((key, value) => writtenPairs.Add((key, value)));
```
<!-- endSnippet -->

You can also use `OnSet` to simulate validation logic:

<!-- snippet: indexers-onset-validation -->
```cs
// OnSet validates and throws for invalid keys or values
stub.Indexer.OnSet((sku, quantity) =>
{
    if (!validSkus.Contains(sku))
        throw new ArgumentException($"Invalid SKU: {sku}");
    if (quantity < 0)
        throw new ArgumentException("Quantity cannot be negative");
});
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
// Verify indexer get/set call counts
stub.Indexer.VerifyGet(Times.Exactly(2));
stub.Indexer.VerifySet(Times.Once);
```
<!-- endSnippet -->

### Capturing Last Access

<!-- snippet: indexers-capture-last -->
```cs
// LastGetKey captures the most recent getter key
Assert.Equal("Second", stub.Indexer.LastGetKey);

// LastSetEntry captures the most recent setter key-value pair
Assert.Equal("Timeout", stub.Indexer.LastSetEntry!.Value.Key);
Assert.Equal("60", stub.Indexer.LastSetEntry.Value.Value);
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
// Sequence: first access returns "cached", second returns "fresh"
stub.Indexer
    .OnGet((key) => "cached")
    .ThenGet((key) => "fresh");
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
// Sequence: first write fails, second succeeds
stub.Indexer
    .OnSet((sku, qty) => { attemptCount++; throw new InvalidOperationException("Service unavailable"); })
    .ThenSet((sku, qty) => { attemptCount++; });
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
// Each overload has its own interceptor: Indexer.OfString, Indexer.OfInt32
stub.Indexer.OfString.Backing["name"] = "Alice";
stub.Indexer.OfInt32.Backing[0] = 100;
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
// OnGet takes precedence over Backing dictionary
stub.Indexer.Backing["ApiKey"] = "from-backing";
stub.Indexer.OnGet((key) => "from-callback");
```
<!-- endSnippet -->

**Design principle:** Callbacks override default backing dictionary behavior. If you need to both execute custom logic AND update the backing dictionary, your callback must explicitly write to `Backing`:

<!-- snippet: indexers-onset-with-backing -->
```cs
// OnSet must manually update Backing if reads should reflect writes
stub.Indexer.OnSet((key, value) =>
{
    if (string.IsNullOrWhiteSpace(value))
        throw new ArgumentException("Value cannot be empty");
    validationLog.Add(key);
    stub.Indexer.Backing[key] = value;
});
```
<!-- endSnippet -->

---

## Resetting Indexers

Calling `Reset()` on an indexer interceptor clears all counters and callbacks but **preserves the Backing dictionary**.

<!-- snippet: indexers-reset -->
```cs
// Reset clears tracking but preserves Backing and callbacks
stub.Indexer.Reset();
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
// 1. Backing: Pre-populate test data
stub.Indexer.Backing[1] = new User { Id = 1, Name = "Alice", Email = "alice@example.com" };

// 2. OnGet: Compute values dynamically
stub.Indexer.OnGet((id) => id == 999
    ? new User { Id = 999, Name = "Dynamic User", Email = "dynamic@example.com" }
    : null);

// 3. OnSet: Track writes
stub.Indexer.OnSet((id, user) => cacheUpdates.Add((id, user)));
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
