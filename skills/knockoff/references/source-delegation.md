# Source Delegation Reference

Source delegation lets you delegate unconfigured calls to a real implementation. This enables partial stubbing — configure specific methods while the rest fall through to the real object.

---

## Basic Usage

```csharp
var stub = new Stubs.ICalculator();
var realCalculator = new RealCalculator();

stub.Source(realCalculator);

ICalculator calc = stub;

// No methods configured — all delegate to source
calc.Add(2, 3);      // Returns 5 (from real implementation)
calc.Subtract(10, 4); // Returns 6 (from real implementation)
```

---

## Partial Stubbing

Configure specific methods while delegating the rest:

```csharp
var stub = new Stubs.ICalculator();
stub.Source(new RealCalculator());

// Override just one method
stub.Add.Return(999);

ICalculator calc = stub;
calc.Add(2, 3);      // 999 (stub configuration wins)
calc.Subtract(10, 4); // 6 (delegates to source)
```

---

## Priority Chain

Source delegation sits below configuration but above defaults:

1. **When chains** (highest)
2. **Sequences**
3. **Return / Call** configuration
4. **Stub overrides** (standalone patterns only)
5. **Source delegation**
6. **Default value** (lowest) / StubException in strict mode

```csharp
stub.Source(realCalculator);
stub.Divide.When(10, 2).Return(5);

calc.Divide(10, 2);  // 5 (When chain matched)
calc.Divide(20, 4);  // 5 (falls to source — real implementation)
```

---

## Source(null) — Remove Delegation

Pass `null` to remove the source:

```csharp
stub.Source(realCalculator);
calc.Add(2, 3); // 5 (from source)

stub.Source(null);
calc.Add(2, 3); // 0 (default — no source, no configuration)
```

---

## Interface Hierarchy Support

When stubbing an interface that extends other interfaces, KnockOff generates **separate `Source()` overloads** for each interface in the hierarchy.

```csharp
// Given: IStore : IReadableStore
// IReadableStore has: GetById, Count
// IStore adds: Save, Delete

var stub = new Stubs.IStore();
```

### Source(IStore) — Full Delegation

```csharp
var fullImpl = new InMemoryStore(); // implements IStore
stub.Source(fullImpl);

// ALL methods delegate
store.GetById(1);      // delegates to fullImpl
store.Count;           // delegates to fullImpl
store.Save(1, "val");  // delegates to fullImpl
store.Delete(1);       // delegates to fullImpl
```

### Source(IReadableStore) — Partial Delegation

```csharp
var readOnly = new ReadOnlyStore(); // implements IReadableStore only
stub.Source(readOnly);

// Only IReadableStore members delegate
store.GetById(1); // delegates to readOnly
store.Count;      // delegates to readOnly

// IStore-only members are NOT delegated — returns defaults
store.Save(1, "val"); // no-op (void default)
store.Delete(1);      // no-op (void default)
```

### Partial Source + Configuration

Combine partial delegation with explicit configuration:

```csharp
stub.Source(readOnlySource);  // Delegates reads

// Explicitly configure writes
var saved = new Dictionary<int, string>();
stub.Save.Call((id, value) => saved[id] = value);
stub.Delete.Call((id) => saved.Remove(id));

store.GetById(1);          // From readOnlySource
store.Save(99, "new");     // Goes to saved dictionary
```

---

## Reset Clears Source

`Reset()` on an interceptor clears its source reference:

```csharp
stub.Source(realCalculator);
calc.Add(2, 3); // 5 (from source)

stub.Add.Reset();
calc.Add(2, 3); // 0 (default — source cleared)

// Re-establish if needed
stub.Source(realCalculator);
```

---

## Interface Stubs Only

Source delegation is **only available for interface stubs**. Class stubs (`[KnockOffBase<T>]`) do not have a `Source()` method because:

- The stub IS-A the class (inheritance, not delegation)
- Non-virtual members come from the base class directly
- Only virtual/abstract members are interceptable

---

## Quick Reference

| Task | Code |
|------|------|
| Set source | `stub.Source(implementation)` |
| Remove source | `stub.Source(null)` |
| Partial stub | `stub.Source(impl)` then `stub.Method.Return(value)` |
| Hierarchy partial | `stub.Source(baseInterfaceImpl)` |
| Reset clears source | `stub.Method.Reset()` removes source for that member |
