# Verification Reference

KnockOff provides three verification approaches: direct verification on interceptors, batch verification via `Verifiable()`, and `VerifyAll()` for all configured members.

---

## Called Constraints

| Constraint | Description |
|------------|-------------|
| `Called.Never` | Must not be called (0 times) |
| `Called.Once` | Exactly 1 call |
| `Called.Twice` | Exactly 2 calls |
| `Called.AtLeastOnce` | 1 or more calls |
| `Called.Exactly(n)` | Exactly n calls |
| `Called.AtLeast(n)` | n or more calls |
| `Called.AtMost(n)` | n or fewer calls |

**`Called.Between()` does NOT exist.** Use separate constraints:

```csharp
stub.Save.Verify(Called.AtLeast(1));
stub.Save.Verify(Called.AtMost(5));
```

---

## Direct Verification

Call `Verify()` directly on any interceptor for immediate checking:

```csharp
stub.Add.Verify();                  // At least once (default)
stub.Add.Verify(Called.Exactly(2)); // Exactly twice
stub.Divide.Verify(Called.Never);   // Never called
```

Throws `VerificationException` immediately if the constraint is not met.

---

## Batch Verification — Verifiable() + stub.Verify()

Mark interceptors during configuration, then verify all at once:

```csharp
// Step 1: Mark during setup
stub.GetById.Return((id) => user).Verifiable();
stub.Save.Call((u) => { }).Verifiable(Called.Exactly(2));
stub.Delete.Call((id) => { }).Verifiable(Called.Never);

// Step 2: Exercise code
repository.GetById(1);
repository.Save(user);
repository.Save(user2);

// Step 3: Verify all marked interceptors
stub.Verify();  // Checks GetById (AtLeastOnce), Save (Exactly(2)), Delete (Never)
```

`stub.Verify()` only checks members marked with `.Verifiable()`. Unconfigured or unmarked members are ignored.

---

## VerifyAll() — All Configured Members

`stub.VerifyAll()` checks ALL members that were configured (Return, Call, Get, Set, When), not just those marked Verifiable. Expects each to be called at least once.

```csharp
stub.Add.Return(42);        // Configured
stub.Subtract.Return(10);   // Configured

calc.Add(1, 2);
// calc.Subtract not called

stub.VerifyAll(); // THROWS — Subtract was configured but never called
```

---

## Per-Member Verification

### Methods

```csharp
stub.Add.Verify(Called.Once);
stub.Save.Verify(Called.AtLeast(2));
```

### Properties

```csharp
stub.Name.VerifyGet(Called.Exactly(3));  // Getter call count
stub.Name.VerifySet(Called.Once);         // Setter call count
stub.Name.Verify(Called.Exactly(4));      // Total (get + set)
```

### Indexers

```csharp
stub.Indexer.VerifyGet(Called.Exactly(2)); // All getter calls (any key)
stub.Indexer.VerifySet(Called.Once);        // All setter calls (any key)
```

### Events

```csharp
stub.Started.VerifyAdd(Called.Once);       // Subscription count
stub.Started.VerifyRemove(Called.Never);   // Unsubscription count
stub.Started.Verify();                     // Alias for VerifyAdd(AtLeastOnce)
```

### Delegates

```csharp
stub.Interceptor.Verify(Called.Exactly(3));
```

### Generic Methods

```csharp
stub.GetById.Of<User>().Verify(Called.Once);
stub.GetById.Of<Product>().Verify(Called.Never);
```

---

## Sequence Verification

Sequences have their own `Verify()` that checks if the entire sequence was consumed:

```csharp
var sequence = stub.Add.Return(1, 2, 3);
calc.Add(0, 0); // 1
calc.Add(0, 0); // 2
calc.Add(0, 0); // 3

sequence.Verify(); // Passes — all 3 consumed
```

### When Chain Verification

```csharp
var chain = stub.Add.When(1, 2).Return(10)
    .ThenWhen(3, 4).Return(20)
    .ThenCall((a, b) => 999);

calc.Add(1, 2);
calc.Add(3, 4);
calc.Add(0, 0);

chain.Verify(); // Passes — all matchers consumed
```

---

## VerificationException

When verification fails, `VerificationException` collects ALL failures and reports them together:

```csharp
stub.Add.Return(42).Verifiable();
stub.Subtract.Return(10).Verifiable();
stub.Divide.Return(5).Verifiable();

calc.Add(1, 2); // Only Add called

try { stub.Verify(); }
catch (VerificationException ex)
{
    // ex.Failures contains Subtract AND Divide failures
    // ex.Message lists all failures:
    //   "Verification failed:
    //    - Method 'Subtract' expected AtLeastOnce, was called 0 times
    //    - Method 'Divide' expected AtLeastOnce, was called 0 times"
}
```

---

## Verify() vs VerifyAll()

| Feature | `stub.Verify()` | `stub.VerifyAll()` |
|---------|-----------------|-------------------|
| Scope | Only `.Verifiable()` marked members | ALL configured members |
| Default constraint | As specified in `Verifiable(Called)` | `Called.AtLeastOnce` |
| Unconfigured members | Ignored | Ignored |
| Use case | Explicit expectations | Ensure all configs were used |

Choose based on testing philosophy:
- **Verify()**: Only verify what you explicitly mark (recommended)
- **VerifyAll()**: Catch accidentally unused configurations

---

## Verifiable() Chaining

`Verifiable()` returns the interceptor for fluent chaining:

```csharp
// Chain with Return
stub.GetUser.Return((id) => user).Verifiable();
stub.GetUser.Return((id) => user).Verifiable(Called.Exactly(2));

// Chain with Call
stub.Save.Call((u) => { }).Verifiable(Called.Once);

// Properties
stub.Name.Get("test");
stub.Name.Verifiable();

// Events
stub.Started.Verifiable();
```

---

## Reset Preserves Verifiable

`Reset()` clears tracking (counts) but preserves the Verifiable marking:

```csharp
stub.Add.Return(42).Verifiable();
calc.Add(1, 2);
stub.Verify(); // Passes

stub.Add.Reset();
// stub.Verify(); // Would FAIL — count reset to 0

calc.Add(3, 4);
stub.Verify(); // Passes again
```
