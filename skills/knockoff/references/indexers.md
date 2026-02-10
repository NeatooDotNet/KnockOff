# Indexer Interceptor Reference

Indexer interceptors are generated for `this[TKey]` members on interfaces and virtual/abstract indexers on classes. They support per-key configuration, all-keys callbacks, sequences, tracking, and verification.

---

## Per-Key Builders

Access per-key builders via the interceptor's C# indexer. Each key gets its own builder with independent configuration.

```csharp
// Configure specific keys to return specific values
stub.Indexer["existing"].Returns(100);
stub.Indexer["special"].Returns(999);

ICollection<string, int> collection = stub;
var val = collection["existing"]; // 100
var val2 = collection["special"]; // 999
```

### Per-Key Builder API

| Method | Description |
|--------|-------------|
| `stub.Indexer[key].Returns(value)` | Configure return value for this key |
| `stub.Indexer[key].Get(() => value)` | Per-key getter callback (no key param — already bound) |
| `stub.Indexer[key].Set((value) => {})` | Per-key setter callback (no key param — already bound) |
| `stub.Indexer[key].Returns(v1).ThenReturns(v2)` | Per-key getter sequence |

**Per-key callbacks do NOT receive the key** — it's already bound by the indexer accessor.

---

## All-Keys Callbacks (Fallback)

All-keys callbacks handle keys not configured with per-key builders. They receive the key as a parameter.

```csharp
// Get callback receives the key
stub.Indexer.Get((key) => key.Length);

ICollection<string, int> collection = stub;
var len1 = collection["hello"]; // 5
var len2 = collection["hi"];    // 2

// Set callback receives key AND value
stub.Indexer.Set((key, value) => storage[key] = value);
collection["one"] = 1;  // storage["one"] = 1
```

---

## Per-Key with All-Keys Fallback

Per-key builders take priority over all-keys callbacks. This is the recommended pattern.

```csharp
stub.Indexer["special"].Returns(999);     // Per-key: always 999
stub.Indexer.Get((key) => key.Length);     // All-keys: fallback

ICollection<string, int> collection = stub;
var r1 = collection["special"]; // 999 (per-key wins)
var r2 = collection["hello"];   // 5 (callback fallback)
```

---

## Priority Chain

When an indexer getter is invoked, KnockOff resolves the value in this order:

1. **Per-key builder** — `stub.Indexer[key].Returns(value)` (highest)
2. **All-keys sequence** — `Get().ThenGet()` if active
3. **All-keys Get callback** — `Get((key) => value)`
4. **Source delegation** — `stub.Source(realImpl)`
5. **Strict mode check** — throws `StubException` if strict
6. **Default value** — `default(T)` (lowest)

---

## Multi-Param Indexers

For `this[int row, int col]` indexers, per-key builders use **flattened** syntax while callbacks use **tuple** syntax.

### Per-Key: Flattened Accessors

```csharp
// Flattened — natural C# indexer syntax
stub.Indexer[1, 2].Returns(12.0);
stub.Indexer[3, 4].Returns(34.0);

IMatrix matrix = stub;
var val = matrix[1, 2]; // 12.0
```

### All-Keys: Tuple Callbacks

```csharp
// Get callback receives named tuple
stub.Indexer.Get(key => key.row * 10.0 + key.col);

IMatrix matrix = stub;
var val = matrix[2, 3]; // 23.0

// Set callback receives tuple key and value
stub.Indexer.Set((key, value) => {
    // key.row, key.col, value available
});
```

**Key insight**: Per-key uses flattened `[row, col]`, callbacks use tuple `(int row, int col)`.

---

## Multi-Indexer Disambiguation

When an interface has multiple indexers distinguished by key type, C# overload resolution handles it automatically:

```csharp
// string indexer
stub.Indexer["foo"].Returns(42);

// int indexer
stub.Indexer[3].Returns(99);
```

No special syntax needed — the compiler resolves the correct overload.

---

## Init-Only Indexers

Indexers with `{ get; init; }` work identically to `{ get; set; }`. The interceptor API is unchanged.

```csharp
stub.Indexer["key"].Returns(42);

IInitIndexerCollection<string, int> collection = stub;
var val = collection["key"]; // 42
```

---

## Sequences (All-Keys)

Indexer getter sequences are **global** — they advance on ANY key access, not per-key.

### Get().ThenGet()

```csharp
stub.Indexer.Get((k) => k.Length)
    .ThenGet((k) => 100)
    .ThenGet((k) => 999);

ICollection<string, int> collection = stub;
var r1 = collection["hello"]; // 5 (first callback)
var r2 = collection["world"]; // 100 (second callback)
var r3 = collection["foo"];   // 999 (third callback)
var r4 = collection["bar"];   // 999 (repeats last)
```

### Set().ThenSet()

```csharp
stub.Indexer.Set((k, v) => log.Add($"First: {k}={v}"))
    .ThenSet((k, v) => log.Add($"Second: {k}={v}"))
    .ThenSet((k, v) => log.Add($"Final: {k}={v}"));
```

### ThenDefault()

Return `default(T)` after exhaustion instead of repeating last value:

```csharp
stub.Indexer.Get((k) => k.Length)
    .ThenGet((k) => 100)
    .ThenDefault();  // 0 after exhaustion

var r3 = collection["foo"]; // 0 (default)
```

### Sequences Are Global, Not Per-Key

```csharp
stub.Indexer.Get((k) => 1).ThenGet((k) => 2).ThenGet((k) => 3);

collection["a"]; // 1
collection["b"]; // 2 (advanced despite different key)
collection["c"]; // 3
```

For per-key behavior, use per-key `Returns` or a Get callback with its own dictionary.

---

## Tracking

| Property | Type | Description |
|----------|------|-------------|
| `LastGetKey` | `TKey?` | Key from the most recent getter call (any path) |
| `LastSetEntry` | `(TKey, TValue)?` | (Key, Value) from the most recent setter call (any path) |

```csharp
_ = collection["a"];
_ = collection["b"];
var lastKey = stub.Indexer.LastGetKey; // "b"

collection["x"] = 10;
collection["y"] = 20;
var lastEntry = stub.Indexer.LastSetEntry; // ("y", 20)
```

Tracking counts ALL accesses regardless of whether handled by per-key, callback, or default.

---

## Verification

| Method | Description |
|--------|-------------|
| `VerifyGet()` | Verify getter was called at least once |
| `VerifyGet(Called)` | Verify getter call count |
| `VerifySet()` | Verify setter was called at least once |
| `VerifySet(Called)` | Verify setter call count |
| `Verifiable()` | Mark for batch verification (AtLeastOnce) |
| `Verifiable(Called)` | Mark for batch verification with constraint |

```csharp
_ = collection["test"];
_ = collection["test"];
collection["new"] = 42;

stub.Indexer.VerifyGet(Called.Exactly(2));
stub.Indexer.VerifySet(Called.Once);
```

Verification counts include ALL access paths (per-key, callback, unconfigured).

---

## Reset

`Reset()` clears:
- Get/set counts
- `LastGetKey`, `LastSetEntry`
- Sequence index

`Reset()` preserves:
- Per-key Returns configuration
- All-keys Get/Set callbacks
- Verifiable marking

---

## API Summary

### Per-Key Builder

| Method | Returns | Description |
|--------|---------|-------------|
| `Returns(TValue)` | `PerKeyBuilder` | Set return value for this key |
| `Get(Func<TValue>)` | `PerKeyBuilder` | Getter callback for this key (no key param) |
| `Set(Action<TValue>)` | `PerKeyBuilder` | Setter callback for this key (no key param) |
| `Returns(v).ThenReturns(v2)` | `PerKeyBuilder` | Per-key sequence |

### All-Keys Configuration

| Method | Returns | Description |
|--------|---------|-------------|
| `Get(Func<TKey, TValue>)` | `IIndexerGetSequence` | All-keys getter callback |
| `Set(Action<TKey, TValue>)` | `IIndexerSetSequence` | All-keys setter callback |
| `ThenGet(Func<TKey, TValue>)` | `IIndexerGetSequence` | Add to getter sequence |
| `ThenSet(Action<TKey, TValue>)` | `IIndexerSetSequence` | Add to setter sequence |
| `ThenDefault()` | `void` | Return default(T) after exhaustion |
