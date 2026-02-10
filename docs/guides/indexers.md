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

## Per-Key Verification

You can verify that a specific key was accessed a specific number of times, rather than checking total indexer access counts.

### Per-Key VerifyGet

<!-- snippet: indexers-perkey-verify-get -->
```cs
// Verify a specific key was read a specific number of times
stub.Indexer["ApiKey"].VerifyGet(Called.Exactly(2));
stub.Indexer["Timeout"].VerifyGet(Called.Once);
```
<!-- endSnippet -->

### Per-Key VerifySet

Per-key set verification requires a per-key `.Set()` callback to be configured so the per-key builder tracks set calls.

<!-- snippet: indexers-perkey-verify-set -->
```cs
// Verify a specific key was written a specific number of times
stub.Indexer["ApiKey"].VerifySet(Called.Once);
stub.Indexer["Timeout"].VerifySet(Called.Exactly(2));
```
<!-- endSnippet -->

**Per-key verification vs. all-keys verification:**
- `stub.Indexer.VerifyGet(Called.Exactly(3))` - verifies total get count across all keys
- `stub.Indexer["ApiKey"].VerifyGet(Called.Exactly(2))` - verifies get count for a specific key only

---

## Predicate-Based Key Matching

Use `When(predicate)` to match keys by condition rather than exact value. This is useful when you want to configure behavior for a group of keys that share a pattern.

### Basic Predicate Matching

<!-- snippet: indexers-when-predicate -->
```cs
// When(predicate) matches keys by condition
stub.Indexer.When(key => key.StartsWith("prefix_", StringComparison.Ordinal)).Returns(99);
```
<!-- endSnippet -->

### Combining Per-Key and When Predicate

Per-key exact match always takes priority over When predicate matching:

<!-- snippet: indexers-when-with-perkey -->
```cs
// Per-key exact match takes priority over When predicate
stub.Indexer["exact"].Returns(100);
stub.Indexer.When(key => key.Length > 3).Returns(42);
```
<!-- endSnippet -->

In this example, `stub["exact"]` returns 100 (per-key wins), while `stub["hello"]` returns 42 (When predicate matches).

### When with Set Callback

Getter and setter When chains are independent. Use `.Set()` on the When builder to intercept writes for matching keys:

<!-- snippet: indexers-when-set-callback -->
```cs
// When(predicate).Set() intercepts writes for matching keys
stub.Indexer.When(key => key.StartsWith("temp_", StringComparison.Ordinal)).Set((key, value) =>
{
    captured.Add((key, value));
});
```
<!-- endSnippet -->

### When Chains with ThenWhen

Chain multiple predicates using `ThenWhen`. Each matcher advances after matching once; the last matcher repeats:

<!-- snippet: indexers-when-chain -->
```cs
// Chain multiple predicates with ThenWhen -- each matcher advances once
stub.Indexer
    .When(key => key.StartsWith("a", StringComparison.Ordinal)).Returns(1)
    .ThenWhen(key => key.StartsWith("b", StringComparison.Ordinal)).Returns(2);
```
<!-- endSnippet -->

**Priority order for get operations:**
1. Per-key exact match (highest priority)
2. When predicate match
3. All-keys sequence / Get callback
4. Source delegation
5. Strict mode / default value (lowest priority)

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
2. When predicate match
3. All-keys sequence (`Get().ThenGet()`)
4. All-keys Get callback
5. Source delegation
6. Strict mode check
7. Default value (lowest priority)

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
| Keys matching a pattern return same value | `When(predicate)` | `stub.Indexer.When(k => k.StartsWith("prefix_")).Returns(99);` |
| Per-key exact with pattern fallback | Per-key + When | `stub.Indexer["x"].Returns(1); stub.Indexer.When(k => k.Length > 3).Returns(42);` |
| Indexer computes values from keys | `Get` | `stub.Cache.Get((id) => LoadById(id));` |
| Indexer returns different values per access | `Get().ThenGet()` | `stub.Data.Get((k) => v1).ThenGet((k) => v2);` |
| Track all writes to indexer | `Set` | `stub.Store.Set((k, v) => log.Add((k, v)));` |
| Track writes for matching keys only | `When(predicate).Set()` | `stub.Indexer.When(k => k.StartsWith("temp_")).Set((k, v) => ...);` |
| Simulate validation in indexer | `Set` | `stub.Config.Set((k, v) => Validate(k));` |
| Indexer validation changes per write | `Set().ThenSet()` | `stub.Db.Set((k, v) => Fail()).ThenSet((k, v) => Ok());` |
| Verify indexer was accessed | Verification | `stub.Indexer.VerifyGet(Called.Once);` |
| Verify specific key was accessed | Per-key verification | `stub.Indexer["key"].VerifyGet(Called.Once);` |
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
2. **Use When(predicate) for pattern matching** - Match groups of keys by condition with `When(k => predicate).Returns(value)`
3. **Use Get for computed values** - Key-dependent or state-dependent returns as fallback for unconfigured keys
4. **Use Set for tracking** - When you need to verify writes or simulate validation
5. **Use sequences for changing behavior** - `Get().ThenGet()` / `Set().ThenSet()` when values or behavior differ across calls
6. **Per-key > When > Get callback** - Per-key exact match wins over When predicate, which wins over all-keys callbacks
7. **Verify per-key or all-keys** - `stub.Indexer["key"].VerifyGet()` for specific keys, `stub.Indexer.VerifyGet()` for totals
8. **Reset() preserves per-key Returns and callbacks** - Clears tracking state but not configuration
9. **Multiple overloads** - Each indexer signature resolves via C# indexer overloads on the interceptor

---

**Next Steps:**
- [Property Configuration Guide](properties.md) - Configure property behavior and callbacks
- [Method Configuration Guide](methods.md) - Configure method behavior and callbacks
- [Verification Patterns](verification.md) - Assert on stub interactions
- [Interceptor API Reference](../reference/interceptor-api.md) - Complete interceptor API documentation

---

**UPDATED:** 2026-02-09
