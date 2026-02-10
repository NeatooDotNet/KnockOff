# Indexer Configuration Guide

Indexers in KnockOff support per-key configuration, all-keys callbacks, sequences, and verification. Each interface indexer gets a corresponding interceptor on the stub.

**Quick reference:** For simple test data scenarios, use per-key `Returns`. For dynamic or computed values, use `Get` callbacks. For write validation or tracking, use `Set` callbacks.

---

## Configuration Approaches

Choose your configuration approach based on test requirements:

**Per-Key Returns (Recommended for Test Data)**
- Configure `Indexer[key].Returns(value)` for specific keys
- Use when the indexer should return known values for known keys
- Simple, readable, and covers most test scenarios
- Example: Pre-loading a cache stub with known user IDs

**Dynamic Callbacks (For Complex Scenarios)**
- Set `Indexer.Get` to compute values at access time
- Set `Indexer.Set` to intercept and validate writes
- Use when values depend on state, validation, or need computed behavior
- Example: Simulating cache misses, validation failures, or retry logic

---

## Per-Key Returns (Recommended for Test Data)

The simplest way to configure an indexer is to set per-key return values before your test runs.

<!-- snippet: indexers-perkey-basic -->
```cs
// Configure per-key return values
stub.Indexer[1].Returns(new User { Id = 1, Name = "Alice" });
stub.Indexer[2].Returns(new User { Id = 2, Name = "Bob" });
```
<!-- endSnippet -->

When the indexer is accessed via the interface, KnockOff checks per-key Returns first, then falls back to all-keys callbacks.

<!-- snippet: indexers-perkey-multiple -->
```cs
// Pre-populate multiple configuration values
stub.Indexer["ConnectionString"].Returns("Server=localhost;Database=Test");
stub.Indexer["ApiKey"].Returns("abc123");
stub.Indexer["Timeout"].Returns("30");
stub.Indexer["MaxRetries"].Returns("3");
```
<!-- endSnippet -->

**When to use per-key Returns:**
- Pre-populating repository stub data
- Configuring lookup tables or caches
- Setting up test fixtures with known data
- Any scenario where the indexer should return known values for specific keys

---

## Dynamic Getters

Use `Get` when an indexer's value should be computed at access time based on the key. This is the all-keys fallback for keys not configured with per-key Returns.

<!-- snippet: indexers-onget-computed -->
```cs
// Get computes values based on the key
stub.Indexer.Get((key) => $"Value for {key}");
```
<!-- endSnippet -->

Get receives the key as a parameter and returns the value:

<!-- snippet: indexers-onget-stateful -->
```cs
// Get checks external state to determine return value
stub.Indexer.Get((sku) => inventory.GetValueOrDefault(sku, 0));
```
<!-- endSnippet -->

**When to use Get:**
- Computed values based on the key
- Simulating cache behavior (hit vs. miss)
- Testing lazy loading or factory patterns
- State-dependent lookups

**Note:** `Get` returns `IIndexerGetTracking<TKey, TValue>` which supports verification and sequence methods.

---

## Setter Interception

Use `Set` to intercept indexer writes. This allows tracking writes or validating keys and values during tests.

<!-- snippet: indexers-onset-tracking -->
```cs
// Set intercepts writes for tracking
stub.Indexer.Set((key, value) => writtenPairs.Add((key, value)));
```
<!-- endSnippet -->

You can also use `Set` to simulate validation logic:

<!-- snippet: indexers-onset-validation -->
```cs
// Set validates and throws for invalid keys or values
stub.Indexer.Set((sku, quantity) =>
{
    if (!validSkus.Contains(sku))
        throw new ArgumentException($"Invalid SKU: {sku}");
    if (quantity < 0)
        throw new ArgumentException("Quantity cannot be negative");
});
```
<!-- endSnippet -->

**When to use Set:**
- Tracking all key-value pairs written
- Simulating validation failures
- Testing how code handles indexer setter exceptions
- Verifying the sequence of indexer writes

**Note:** `Set` returns `IIndexerSetTracking<TKey, TValue>` which supports verification and sequence methods.

---

## Verifying Indexer Access

Indexer interceptors support verification and tracking similar to properties and methods.

<!-- snippet: indexers-verify-access -->
```cs
// Verify indexer get/set call counts
stub.Indexer.VerifyGet(Called.Exactly(2));
stub.Indexer.VerifySet(Called.Once);
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
- `VerifyGet(Called)` - Verify indexer getter was called specified number of times
- `VerifySet(Called)` - Verify indexer setter was called specified number of times

**Inspection properties:**
- `LastGetKey` - The key from the most recent getter call (null if never accessed)
- `LastSetEntry` - Nullable KeyValuePair of the most recent setter call (null if never set)

---

## Sequence Behavior

Use sequences when an indexer should return different values for the same key across multiple reads, or react differently to multiple writes.

### Get Sequences

When you need an indexer to return different values on successive reads of the same key, use `Get().ThenGet()`:

<!-- snippet: indexers-ongetsequence-basic -->
```cs
// Sequence: first access returns "cached", second returns "fresh"
stub.Indexer
    .Get((key) => "cached")
    .ThenGet((key) => "fresh");
```
<!-- endSnippet -->

**When to use get sequences:**
- Testing cache invalidation (first hit returns cached, second returns fresh)
- Simulating retry logic with changing data
- Testing eventual consistency scenarios

### Set Sequences

When you need different behavior for successive indexer writes, use `Set().ThenSet()`:

<!-- snippet: indexers-onset-then-sequence -->
```cs
// Sequence: first write fails, second succeeds
stub.Indexer
    .Set((sku, qty) => { attemptCount++; throw new InvalidOperationException("Service unavailable"); })
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
| Fixed values for specific keys | `Indexer[key].Returns(value)` | Simple per-key configuration |
| Indexer computes values from keys | `Get((key) => computed)` | Key-based computation |
| Indexer returns different values per access | `Get((k) => v1).ThenGet((k) => v2)` | Different values on successive reads |
| Indexer validates writes | `Set((k, v) => Validate(k, v))` | Custom validation logic |
| Indexer validation changes per write | `Set((k, v) => check1).ThenSet((k, v) => check2)` | Different behavior per write |

---

## Multiple Indexer Overloads

When an interface has multiple indexer overloads (different key types), KnockOff generates C# indexer overloads on the interceptor class:

<!-- snippet: indexers-multiple-overloads -->
```cs
// C# indexer overloads resolve by key type -- no OfXxx needed
stub.Indexer["name"].Returns("Alice");
stub.Indexer[0].Returns(100);
```
<!-- endSnippet -->

Overloads resolve naturally via C# indexer overload resolution -- access `stub.Indexer[stringKey]` or `stub.Indexer[intKey]` and the compiler selects the correct overload.

For tracking, multi-indexer stubs provide type-suffixed properties: `LastStringGetKey`, `LastInt32GetKey`, etc.

---

## Per-Key vs. All-Keys Priority

When both per-key Returns and all-keys Get callback are configured:
- **Per-key Returns takes precedence** for keys configured with `Indexer[key].Returns(value)`
- **All-keys Get callback** handles keys not configured with per-key Returns

<!-- snippet: indexers-priority -->
```cs
// Per-key Returns takes precedence over all-keys Get callback
stub.Indexer["ApiKey"].Returns("from-per-key");
stub.Indexer.Get((key) => "from-callback");
```
<!-- endSnippet -->

The full priority chain for indexer get operations:
1. Per-key builder (highest priority)
2. All-keys sequence (`Get().ThenGet()`)
3. All-keys Get callback
4. Source delegation
5. Strict mode check
6. Default value (lowest priority)

---

## Per-Key with Fallback Pattern

A common pattern is to use per-key Returns for specific keys and a Get callback as a fallback:

<!-- snippet: indexers-perkey-with-fallback -->
```cs
// Per-key for specific keys, Get callback as fallback for others
stub.Indexer["ApiKey"].Returns("secret123");
stub.Indexer.Get((key) => $"default-{key}");
```
<!-- endSnippet -->

---

## Resetting Indexers

Calling `Reset()` on an indexer interceptor clears all counters but **preserves per-key Returns and callbacks**.

<!-- snippet: indexers-reset -->
```cs
// Reset clears tracking but preserves per-key Returns and callbacks
stub.Indexer.Reset();
```
<!-- endSnippet -->

**Reset behavior:** Calling `Reset()` clears all tracking counters, `LastGetKey`, `LastSetEntry`, and resets sequence position to the beginning. However, per-key Returns, callbacks (`Get`, `Set`), and sequence configurations are all preserved. This allows you to verify behavior, reset tracking state, and re-run the same test scenario without reconfiguring.

---

## Decision Guide

Choose your configuration approach based on the test scenario:

| Scenario | Use This | Example |
|----------|----------|---------|
| Indexer should return fixed test data | Per-key Returns | `stub.Indexer[1].Returns(user1);` |
| Specific keys with fallback for others | Per-key + Get | `stub.Indexer["x"].Returns(1); stub.Indexer.Get(k => 0);` |
| Indexer computes values from keys | `Get` | `stub.Cache.Get((id) => LoadById(id));` |
| Indexer returns different values per access | `Get().ThenGet()` | `stub.Data.Get((k) => v1).ThenGet((k) => v2);` |
| Track all writes to indexer | `Set` | `stub.Store.Set((k, v) => log.Add((k, v)));` |
| Simulate validation in indexer | `Set` | `stub.Config.Set((k, v) => Validate(k));` |
| Indexer validation changes per write | `Set().ThenSet()` | `stub.Db.Set((k, v) => Fail()).ThenSet((k, v) => Ok());` |
| Verify indexer was accessed | Verification | `stub.Indexer.VerifyGet(Called.Once);` |
| Verify last key written | Verification | `Assert.Equal(42, stub.Indexer.LastGetKey);` |

---

## Complete Example

This example demonstrates all indexer configuration approaches in a realistic test scenario.

<!-- snippet: indexers-complete-example -->
```cs
// 1. Per-key Returns: Pre-configure specific keys
stub.Indexer[1].Returns(new User { Id = 1, Name = "Alice", Email = "alice@example.com" });

// 2. Get: Dynamic fallback for unconfigured keys
stub.Indexer.Get((id) => id == 999
    ? new User { Id = 999, Name = "Dynamic User", Email = "dynamic@example.com" }
    : null);

// 3. Set: Track writes
stub.Indexer.Set((id, user) => cacheUpdates.Add((id, user)));
```
<!-- endSnippet -->

---

## Key Takeaways

1. **Start with per-key Returns** - It covers most scenarios with simple `Indexer[key].Returns(value)` syntax
2. **Use Get for computed values** - Key-dependent or state-dependent returns as fallback for unconfigured keys
3. **Use Set for tracking** - When you need to verify writes or simulate validation
4. **Use sequences for changing behavior** - `Get().ThenGet()` / `Set().ThenSet()` when values or behavior differ across calls
5. **Per-key Returns takes priority** - Per-key configuration always wins over all-keys Get callbacks
6. **Reset() preserves per-key Returns and callbacks** - Clears tracking state but not configuration
7. **Verify access patterns** - Use `VerifyGet()` and `VerifySet()` like property verification
8. **Multiple overloads** - Each indexer signature resolves via C# indexer overloads on the interceptor

---

**Next Steps:**
- [Property Configuration Guide](properties.md) - Configure property behavior and callbacks
- [Method Configuration Guide](methods.md) - Configure method behavior and callbacks
- [Verification Patterns](verification.md) - Assert on stub interactions
- [Interceptor API Reference](../reference/interceptor-api.md) - Complete interceptor API documentation

---

**UPDATED:** 2026-02-09
