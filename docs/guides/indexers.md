# Indexer Configuration Guide

Indexers in KnockOff work similarly to properties but with key-based access. Each interface indexer gets a corresponding interceptor that maintains a backing dictionary, tracks access, and supports custom get/set callbacks.

---

## Configuration Approaches

**Backing Dictionary (Recommended for Test Data)**
- Populate `Indexer.Backing` with test data before running test
- Use when the indexer should behave like a standard dictionary
- Simple, readable, and covers most test scenarios

**Dynamic Callbacks (For Complex Scenarios)**
- Set `Indexer.OnGet` to compute values at access time
- Set `Indexer.OnSet` to intercept and validate writes
- Use when values depend on state, validation, or need computed behavior

---

## Backing Dictionary (Recommended for Test Data)

The simplest way to configure an indexer is to populate the backing dictionary before your test runs.

<!-- snippet: indexers-backing-basic -->
<!--
Demonstrate: Populating indexer backing dictionary with test data
Show: Indexer.Backing[key] = value, then access via interface
Result: Indexer returns values from backing dictionary
-->
<!-- endSnippet -->

When the indexer is accessed via the interface, KnockOff uses the backing dictionary by default:
- **Get**: Returns `Backing[key]` (throws `KeyNotFoundException` if key doesn't exist)
- **Set**: Stores to `Backing[key]`

<!-- snippet: indexers-backing-multiple -->
<!--
Demonstrate: Pre-populating multiple entries in backing dictionary
Show: Multiple Backing[key] = value assignments
Result: All keys accessible via interface indexer
-->
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
<!--
Demonstrate: Indexer getter that computes values based on key
Show: OnGet((key) => computedValue) method syntax
Result: Each key access computes a fresh value
-->
<!-- endSnippet -->

OnGet receives the key as a parameter and returns the value:

<!-- snippet: indexers-onget-stateful -->
<!--
Demonstrate: Indexer getter that depends on other stub state
Show: OnGet accessing tracked state to determine return value
Result: Indexer behavior changes based on test execution
-->
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
<!--
Demonstrate: OnSet tracking all written key-value pairs
Show: OnSet((key, value) => { }) method syntax capturing to list
Result: All writes tracked for verification
-->
<!-- endSnippet -->

You can also use `OnSet` to simulate validation logic:

<!-- snippet: indexers-onset-validation -->
<!--
Demonstrate: OnSet simulating validation that throws for invalid keys
Show: OnSet checking key validity and throwing exceptions
Result: Invalid keys cause exceptions during indexer writes
-->
<!-- endSnippet -->

**When to use OnSet:**
- Tracking all key-value pairs written
- Simulating validation failures
- Testing how code handles indexer setter exceptions
- Verifying the sequence of indexer writes

**Note:** `OnSet` returns `IIndexerSetTracking<TKey, TValue>` which supports verification and sequence methods.

---

## Verifying Indexer Access

Indexer interceptors support verification similar to properties.

### Using Verify() on Indexers

<!-- snippet: indexers-verify-access -->
<!--
Demonstrate: Verifying indexer was accessed
Show: VerifyGet(Times), VerifySet(Times) on Indexer interceptor
Result: Verification passes if access counts match expectations
-->
<!-- endSnippet -->

### Capturing Last Access

<!-- snippet: indexers-capture-last -->
<!--
Demonstrate: Capturing the last accessed key and set entry
Show: LastGetKey, LastSetEntry properties
Result: Access to most recently used key and value
-->
<!-- endSnippet -->

**Available verification methods:**
- `VerifyGet(Times)` - Verify indexer getter was called
- `VerifySet(Times)` - Verify indexer setter was called

**Available inspection properties:**
- `LastGetKey` - The key from the most recent getter call (nullable)
- `LastSetEntry` - Tuple of (key, value) from the most recent setter call (nullable)
- `Backing` - The backing dictionary (modifiable)

---

## Sequence Behavior

Use sequences when an indexer should return different values for the same key across multiple reads, or react differently to multiple writes.

### Get Sequences (OnGetSequence)

When you need an indexer to return different values on successive reads of the same key:

<!-- snippet: indexers-ongetsequence-basic -->
<!--
Demonstrate: Indexer returning different values on successive reads
Show: OnGetSequence((key) => first).ThenGet((key) => second)
Result: First access returns first value, second access returns second value
-->
<!-- endSnippet -->

**When to use OnGetSequence:**
- Testing cache invalidation (first hit returns cached, second returns fresh)
- Simulating retry logic with changing data
- Testing eventual consistency scenarios

### Set Sequences (OnSetSequence)

When you need different behavior for successive indexer writes:

<!-- snippet: indexers-onsetsequence-basic -->
<!--
Demonstrate: Indexer reacting differently to successive writes
Show: OnSetSequence((k, v) => reject).ThenSet((k, v) => accept)
Result: First write fails validation, second write succeeds
-->
<!-- endSnippet -->

**When to use OnSetSequence:**
- Testing validation that changes over time
- Simulating connection failures then recovery
- Testing retry logic with different outcomes

### Sequence vs. Single Callbacks

| Use Case | Use This | Why |
|----------|----------|-----|
| Indexer uses backing dictionary | `Backing[key] = value` | Simple, standard dictionary behavior |
| Indexer computes values from keys | `OnGet((key) => computed)` | Key-based computation |
| Indexer returns different values per access | `OnGetSequence((k) => v1).ThenGet((k) => v2)` | Different values on successive reads |
| Indexer validates writes | `OnSet((k, v) => Validate(k, v))` | Custom validation logic |
| Indexer validation changes per write | `OnSetSequence((k, v) => check1).ThenSet((k, v) => check2)` | Different behavior per write |

---

## Multiple Indexer Overloads

When an interface has multiple indexer overloads (different key types), KnockOff generates separate interceptor properties:

<!-- snippet: indexers-multiple-overloads -->
<!--
Demonstrate: Interface with multiple indexer signatures
Show: Indexer, Indexer_1, Indexer_2 properties on stub
Result: Each overload has its own interceptor with independent backing dictionary
-->
<!-- endSnippet -->

Overloads are numbered in the order they appear in the interface definition:
- First indexer: `stub.Indexer`
- Second indexer: `stub.Indexer_1`
- Third indexer: `stub.Indexer_2`

Each overload maintains its own backing dictionary and configuration.

---

## OnGet/OnSet vs. Backing Priority

When both `Backing` and `OnGet`/`OnSet` are configured:
- **OnGet takes precedence**: Callback return value is used instead of `Backing[key]`
- **OnSet takes precedence**: Callback is invoked, `Backing` is NOT updated automatically

<!-- snippet: indexers-priority -->
<!--
Demonstrate: OnGet/OnSet taking precedence over Backing
Show: Populate Backing, then set OnGet - callback wins
Result: OnGet callback value returned instead of Backing value
-->
<!-- endSnippet -->

**Design principle:** Callbacks override default backing dictionary behavior. If you want both, your callback must explicitly update `Backing`:

```csharp
stub.Indexer.OnSet((key, value) => {
    Validate(key, value);
    stub.Indexer.Backing[key] = value;  // Manually update Backing
});
```

---

## Resetting Indexers

Calling `Reset()` on an indexer interceptor clears all counters and callbacks but **preserves the Backing dictionary**.

<!-- snippet: indexers-reset -->
<!--
Demonstrate: Reset() clearing tracking but preserving Backing
Show: Reset(), then verify counts are zero but Backing data remains
Result: Tracking cleared, test data preserved
-->
<!-- endSnippet -->

**Note on Reset behavior:** Reset() clears tracking counters, `LastGetKey`, `LastSetEntry`, `OnGet`, and `OnSet`. The `Backing` dictionary is preserved to maintain test data configuration between verification phases.

---

## Decision Guide

Choose your configuration approach based on the test scenario:

| Scenario | Use This | Example |
|----------|----------|---------|
| Indexer should return fixed test data | `Backing` | `stub.Indexer.Backing[1] = user1;` |
| Indexer computes values from keys | `OnGet` | `stub.Cache.OnGet((id) => LoadById(id));` |
| Indexer returns different values per access | `OnGetSequence` | `stub.Data.OnGetSequence((k) => v1).ThenGet((k) => v2);` |
| Track all writes to indexer | `OnSet` | `stub.Store.OnSet((k, v) => log.Add((k, v)));` |
| Simulate validation in indexer | `OnSet` | `stub.Config.OnSet((k, v) => Validate(k));` |
| Indexer validation changes per write | `OnSetSequence` | `stub.Db.OnSetSequence((k, v) => Fail()).ThenSet((k, v) => Ok());` |
| Verify indexer was accessed | Verification | `stub.Indexer.VerifyGet(Times.Once);` |
| Verify last key written | Verification | `Assert.Equal(42, stub.Indexer.LastGetKey);` |

---

## Complete Example

This example demonstrates all indexer configuration approaches in a realistic test scenario.

<!-- snippet: indexers-complete-example -->
<!--
Demonstrate: Complete indexer usage in a test
Show: Backing, OnGet, OnSet, verification, and sequences
Result: Comprehensive example showing all configuration patterns
-->
<!-- endSnippet -->

---

## Key Takeaways

1. **Start with Backing** - It covers most scenarios and behaves like a standard dictionary
2. **Use OnGet for computed values** - Key-dependent or state-dependent returns
3. **Use OnSet for tracking** - When you need to verify writes or simulate validation
4. **Use sequences for changing behavior** - OnGetSequence/OnSetSequence when values or behavior differ across calls
5. **OnGet/OnSet override Backing** - Callbacks take precedence over dictionary lookups
6. **Reset() preserves Backing** - Clears execution state but not test data
7. **Verify access patterns** - Use `VerifyGet()` and `VerifySet()` like property verification
8. **Multiple overloads** - Each indexer signature gets its own interceptor (Indexer, Indexer_1, etc.)

---

**Next Steps:**
- [Property Configuration Guide](properties.md) - Configure property behavior and callbacks
- [Method Configuration Guide](methods.md) - Configure method behavior and callbacks
- [Verification Patterns](verification.md) - Assert on stub interactions
- [Interceptor API Reference](../reference/interceptor-api.md) - Complete interceptor API documentation
