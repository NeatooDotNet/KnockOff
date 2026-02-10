# When Chains Reference

When chains provide parameter-specific matching. When the method is called with matching arguments, the configured return value is used instead of the default Return/Call behavior. When chains are the **highest priority** in the resolution chain.

---

## Value Equality Matching

Match specific argument values using equality:

```csharp
stub.Add.When(1, 2).Return(100);
stub.Add.When(5, 5).Return(500);

ICalculator calc = stub;
calc.Add(1, 2);  // 100 (matched)
calc.Add(5, 5);  // 500 (matched)
calc.Add(3, 4);  // falls to Return/default
```

### Single-Parameter Methods

```csharp
stub.GetUser.When(42).Return(adminUser);
stub.GetUser.When(1).Return(regularUser);
```

---

## Predicate Matching

Match using a predicate function for complex conditions:

```csharp
// Range check
stub.Add.When((a, b) => a > 0 && b > 0).Return(42);

// Pattern matching
stub.GetData.When((id) => id < 0).Return("not found");

// Null handling (use predicate, not When(null))
stub.GetUser.When((s) => s == null).Return(defaultUser);
```

---

## ThenWhen() Chaining

Chain multiple matchers as a logical group:

```csharp
stub.Add
    .When(1, 1).Return(1)
    .ThenWhen(2, 2).Return(2)
    .ThenWhen(3, 3).Return(3);

calc.Add(1, 1); // 1
calc.Add(2, 2); // 2
calc.Add(3, 3); // 3
calc.Add(4, 4); // falls to Return/default
```

Mix value and predicate matchers in the same chain:

```csharp
stub.GetUser
    .When(42).Return(adminUser)
    .ThenWhen(id => id > 100).Return(premiumUser)
    .ThenWhen(id => id > 0).Return(regularUser);
```

---

## Order Matters — First Match Wins

Matchers are checked in the order added. Put specific matchers **before** broad ones:

```csharp
// WRONG ORDER: broad predicate catches everything
stub.Add.When((a, b) => a > 0).Return(100);  // Added first
stub.Add.When(5, 5).Return(500);              // Added second
calc.Add(5, 5); // Returns 100! (first match wins)

// RIGHT ORDER: specific first, broad second
stub.Add.When(5, 5).Return(500);              // Specific first
stub.Add.When((a, b) => a > 0).Return(100);  // Broad second
calc.Add(5, 5); // Returns 500 (specific wins)
```

---

## When + Return(value) Only

When chains use `Return(value)` — there is **no** `Return(callback)` on When chains.

```csharp
// This works:
stub.Add.When(10, 10).Return(100);

// This does NOT exist:
// stub.Add.When(10, 10).Return((a, b) => a * b);  // No Return(callback) on When

// For dynamic behavior on all calls, use Return(callback) without When:
stub.Add.Return((a, b) => a * b);
```

---

## Void Methods — Call Instead of Return

Void methods use `Call(callback)` instead of `Return(value)`:

```csharp
stub.Log.When("error").Call((msg) => errors.Add(msg));
stub.Log.When(msg => msg.StartsWith("WARN")).Call((msg) => warnings.Add(msg));
```

### ThenCall() Terminal Fallback

Use `.ThenCall()` as a terminal fallback for non-void When chains:

```csharp
stub.Add
    .When(1, 2).Return(100)
    .ThenWhen(3, 4).Return(200)
    .ThenCall((a, b) => a + b);  // Fallback for unmatched

calc.Add(1, 2); // 100
calc.Add(3, 4); // 200
calc.Add(5, 6); // 11 (fallback computes)
```

---

## Async Methods

When chains work identically with async methods. `Return(value)` auto-wraps:

```csharp
stub.GetDataAsync.When(1).Return("Item 1");     // Auto-wrapped in Task.FromResult
stub.GetDataAsync.When(2).Return("Item 2");
stub.GetDataAsync.When((id) => id > 100).Return("Bulk item");

var r = await service.GetDataAsync(1); // "Item 1"
```

---

## Delegate When Chains

Delegates use the same When API via `stub.Interceptor`:

```csharp
stub.Interceptor.When(1, 2).Return(100)
    .ThenWhen(3, 4).Return(200)
    .ThenCall((a, b) => a + b);
```

---

## Verification

### Verifiable() on When Chains

Mark When chains for batch verification:

```csharp
stub.Add.When(1, 2).Return(100).Verifiable();
stub.Add.When(5, 5).Return(500).Verifiable();
// ... exercise code ...
stub.Verify(); // Checks all Verifiable() items
```

### When Chain Verify()

When chains have their own `Verify()` for checking consumption:

```csharp
var chain = stub.Add.When(1, 2).Return(10)
    .ThenWhen(3, 4).Return(20)
    .ThenCall((a, b) => 999);

calc.Add(1, 2); // First matcher
calc.Add(3, 4); // Second matcher
calc.Add(0, 0); // Terminal

chain.Verify(); // Passes — all matchers consumed
```

---

## Priority in Resolution Chain

When chains are checked **first** — before all other configuration:

1. **When chains** (highest)
2. Sequences
3. Return / Call
4. Stub overrides
5. Source delegation
6. Default value (lowest)

When a When chain is configured, Return/Call becomes the fallback for unmatched calls:

```csharp
stub.Add.Return(0);                     // Default for unmatched
stub.Add.When(1, 2).Return(100);        // Specific match

calc.Add(1, 2); // 100 (When matched)
calc.Add(3, 4); // 0 (fell to Return)
```

---

## Known Bug

`When()` currently **accumulates** like `ThenWhen()` instead of replacing the chain. Calling `When()` again adds to the existing chain rather than starting a new one. See `docs/todos/when-entry-point-should-clear-chain.md`.
