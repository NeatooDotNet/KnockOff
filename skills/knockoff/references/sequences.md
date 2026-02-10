# Sequences Reference

Sequences configure different return values or behaviors for successive calls. KnockOff supports sequences for methods, properties, and indexers with NSubstitute-compatible exhaustion behavior.

---

## Core Behavior

- After all values/callbacks are consumed, the **last one repeats** indefinitely (NSubstitute-like)
- Use `ThenDefault()` to return `default(T)` after exhaustion instead
- In strict mode, exhausted sequences throw `StubException.SequenceExhausted`

---

## Method Sequences

### Params Syntax (Preferred)

```csharp
// NSubstitute-style concise syntax
stub.Add.Return(1, 2, 3);

calc.Add(0, 0); // 1
calc.Add(0, 0); // 2
calc.Add(0, 0); // 3
calc.Add(0, 0); // 3 (repeats last)
```

### Single Value vs Params

C# overload resolution distinguishes them:

```csharp
stub.Method.Return(42);       // Single value — repeats forever (no sequence)
stub.Method.Return(1, 2, 3);  // Params — sequence, last repeats after exhaustion
```

### Callback Sequences

```csharp
stub.Add
    .Return((a, b) => a + b)     // First: computed
    .ThenReturn((a, b) => a * b) // Second: computed
    .ThenReturn(999);            // Third+: constant
```

### Callback + Params

```csharp
stub.Add.Return((a, b) => a + b).ThenReturn(100, 200, 300);

calc.Add(1, 2); // 3 (computed)
calc.Add(0, 0); // 100
calc.Add(0, 0); // 200
calc.Add(0, 0); // 300
calc.Add(0, 0); // 300 (repeats)
```

### Value-Based Sequences

```csharp
stub.Add.Return(1).ThenReturn(2).ThenReturn(3);
// Equivalent to: stub.Add.Return(1, 2, 3);
```

### ThenDefault()

```csharp
stub.Add.Return((a, b) => 1).ThenReturn((a, b) => 999).ThenDefault();

calc.Add(0, 0); // 1
calc.Add(0, 0); // 999
calc.Add(0, 0); // 0 (default)
```

### Async Auto-Wrapping in Sequences

Params values auto-wrap for `Task<T>` and `ValueTask<T>`:

```csharp
stub.GetDataAsync.Return("first", "second", "third");

var r1 = await service.GetDataAsync(1); // "first"
var r2 = await service.GetDataAsync(2); // "second"
var r3 = await service.GetDataAsync(3); // "third"
var r4 = await service.GetDataAsync(4); // "third" (repeats)
```

### Void Method Sequences

```csharp
stub.Reset
    .Call(() => log.Add("First"))
    .ThenCall(() => log.Add("Second"))
    .ThenCall(() => log.Add("Subsequent"));

calc.Reset(); // "First"
calc.Reset(); // "Second"
calc.Reset(); // "Subsequent"
calc.Reset(); // "Subsequent" (repeats last)
```

---

## Property Sequences

### Getter Sequences — Get().ThenGet()

```csharp
stub.Name
    .Get("First")
    .ThenGet("Second")
    .ThenGet("Third");

service.Name; // "First"
service.Name; // "Second"
service.Name; // "Third"
service.Name; // "Third" (repeats)
```

With callbacks:

```csharp
stub.Name
    .Get(() => "First")
    .ThenGet(() => "Second")
    .ThenGet(() => "Third");
```

### Setter Sequences — Set().ThenSet()

```csharp
stub.Name
    .Set((v) => firstWrite = v)
    .ThenSet((v) => secondWrite = v);

service.Name = "A"; // firstWrite = "A"
service.Name = "B"; // secondWrite = "B"
service.Name = "C"; // secondWrite = "C" (repeats last)
```

### Property ThenDefault()

```csharp
stub.Name.Get("first").ThenGet("second").ThenDefault();

service.Name; // "first"
service.Name; // "second"
service.Name; // null (default)
```

---

## Indexer Sequences

Indexer sequences are **global** — they advance on ANY key access, not per-key.

### All-Keys Getter Sequences

```csharp
stub.Indexer.Get((k) => k.Length)
    .ThenGet((k) => 100)
    .ThenGet((k) => 999);

collection["hello"]; // 5 (first callback)
collection["world"]; // 100 (second callback)
collection["foo"];   // 999 (third)
collection["bar"];   // 999 (repeats)
```

### All-Keys Setter Sequences

```csharp
stub.Indexer.Set((k, v) => log.Add($"First: {k}={v}"))
    .ThenSet((k, v) => log.Add($"Final: {k}={v}"));
```

### Per-Key Sequences

```csharp
stub.Indexer["key"].Returns(1).ThenReturns(2).ThenReturns(3);

collection["key"]; // 1
collection["key"]; // 2
collection["key"]; // 3
collection["key"]; // 3 (repeats)
```

### Global vs Per-Key

All-keys sequences are shared across all keys:

```csharp
stub.Indexer.Get((k) => 1).ThenGet((k) => 2).ThenGet((k) => 3);

collection["a"]; // 1
collection["b"]; // 2 (advanced despite different key!)
collection["c"]; // 3
```

For per-key behavior, use per-key `Returns` or a Get callback with its own dictionary.

---

## Sequence Exhaustion

| Behavior | How to Configure |
|----------|-----------------|
| Repeat last (default) | `Return(1, 2, 3)` |
| Return default(T) | `Return(1, 2).ThenDefault()` |
| Throw exception | `stub.Strict = true` + sequence |

### Strict Mode Throws on Exhaustion

```csharp
stub.Strict = true;
stub.Add.Return((a, b) => 100).ThenReturn((a, b) => 200);

calc.Add(0, 0); // 100
calc.Add(0, 0); // 200
calc.Add(0, 0); // Throws StubException.SequenceExhausted
```

---

## Sequence Verification

Sequences support `Verify()` to check if fully consumed:

```csharp
var sequence = stub.Add.Return(1, 2, 3);

calc.Add(0, 0); // 1
calc.Add(0, 0); // 2
calc.Add(0, 0); // 3

sequence.Verify(); // Passes — all 3 consumed
```

---

## Interaction with When Chains

When chains have **higher priority** than sequences. When matches don't advance the sequence:

```csharp
stub.Add.Return((a, b) => 1).ThenReturn((a, b) => 2);
stub.Add.When(99, 99).Return(9999);

calc.Add(0, 0);   // 1 (sequence)
calc.Add(99, 99);  // 9999 (When match — doesn't advance sequence)
calc.Add(0, 0);   // 2 (sequence advances)
calc.Add(99, 99);  // 9999 (When still matches)
```

---

## Reset and Sequences

`Reset()` clears the sequence index (resets to beginning) but preserves the sequence structure:

```csharp
stub.Add.Return(1, 2, 3);
calc.Add(0, 0); // 1
calc.Add(0, 0); // 2

stub.Add.Reset();

calc.Add(0, 0); // 1 (restarted from beginning)
```

---

## Method Summary

### Method Sequences

| Method | Description |
|--------|-------------|
| `Return(first, params rest)` | Concise value sequence |
| `Return(cb).ThenReturn(cb)` | Callback sequence |
| `Return(cb).ThenReturn(value)` | Mix callbacks and values |
| `Return(cb).ThenReturn(params values)` | Callback then multiple values |
| `ThenDefault()` | Return default(T) after exhaustion |

### Property Sequences

| Method | Description |
|--------|-------------|
| `Get(v).ThenGet(v)` | Getter value sequence |
| `Get(cb).ThenGet(cb)` | Getter callback sequence |
| `Set(cb).ThenSet(cb)` | Setter callback sequence |
| `ThenDefault()` | Return default(T) after exhaustion |

### Indexer Sequences

| Method | Description |
|--------|-------------|
| `Get(cb).ThenGet(cb)` | All-keys getter sequence (global) |
| `Set(cb).ThenSet(cb)` | All-keys setter sequence (global) |
| `[key].Returns(v).ThenReturns(v)` | Per-key sequence |
| `ThenDefault()` | Return default(T) after exhaustion |
