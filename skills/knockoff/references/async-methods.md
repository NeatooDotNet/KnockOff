# Async Methods Reference

KnockOff provides three-tier auto-wrapping for async methods returning `Task<T>` or `ValueTask<T>`. You configure with unwrapped values and KnockOff wraps them automatically.

---

## Three-Tier Auto-Wrapping

For an async method like `Task<string> FetchAsync(int id)`:

### Tier 1: Value — Auto-Wraps

```csharp
stub.FetchAsync.Return("value");
// Internally: Task.FromResult("value")

var result = await service.FetchAsync(1); // "value"
```

### Tier 2: Simplified Callback — Auto-Wraps

```csharp
stub.FetchAsync.Return((id) => $"Fetch-{id}");
// Internally: Task.FromResult(callback(id))

var result = await service.FetchAsync(42); // "Fetch-42"
```

### Tier 3: Full Callback — Direct

```csharp
stub.FetchAsync.Return((int id) => Task.FromResult($"Full-{id}"));
// Used as-is — for custom async behavior

var result = await service.FetchAsync(99); // "Full-99"
```

**Rule of thumb:** Use Tier 1 or 2 for simple returns. Use Tier 3 when you need actual async behavior (delays, cancellation tokens, etc.).

---

## Void Async Methods (Task, ValueTask)

Methods returning `Task` or `ValueTask` (no result) use `Call()`:

```csharp
stub.ExecuteAsync.Call((command) => { /* side effect */ });

await service.ExecuteAsync("test"); // Callback invoked
```

Unconfigured void async methods return `Task.CompletedTask` or `default(ValueTask)`.

---

## Sequences with Auto-Wrapping

Params values auto-wrap for async methods:

```csharp
stub.GetDataAsync.Return("first", "second", "third");

var r1 = await service.GetDataAsync(1); // "first"
var r2 = await service.GetDataAsync(2); // "second"
var r3 = await service.GetDataAsync(3); // "third"
var r4 = await service.GetDataAsync(4); // "third" (repeats)
```

Callback sequences also work:

```csharp
stub.FetchAsync.Return((id) => $"First-{id}")
    .ThenReturn((id) => $"Second-{id}")
    .ThenReturn("constant");
```

---

## When Chains with Auto-Wrapping

When chain `Return(value)` auto-wraps for async methods:

```csharp
stub.GetDataAsync.When(1).Return("Item 1");
stub.GetDataAsync.When(2).Return("Item 2");
stub.GetDataAsync.When((id) => id > 100).Return("Bulk item");

var r = await service.GetDataAsync(1); // "Item 1"
```

---

## Async Delegates

Async delegates (e.g., `delegate Task<int> AsyncOperation(int x)`) support the same three-tier pattern:

```csharp
var stub = new Stubs.AsyncOperation();

// Tier 1: auto-wraps int -> Task<int>
stub.Interceptor.Return(42);

// Tier 2: simplified callback, auto-wrapped
stub.Interceptor.Return((int x) => x * 2);

// Tier 3: full delegate
stub.Interceptor.Return((int x) => Task.FromResult(x * 2));

AsyncOperation op = stub;
var result = await op(10); // depends on tier used
```

Sequences on async delegates also auto-wrap:

```csharp
stub.Interceptor.Return(10, 20);

var r1 = await op(0); // 10
var r2 = await op(0); // 20
var r3 = await op(0); // 20 (repeats)
```

---

## All 9 Patterns

Async auto-wrapping works identically across all 9 patterns:

| Pattern | Access |
|---------|--------|
| 1. Standalone | `stub.FetchAsync.Return("value")` |
| 2. Generic Standalone | `stub.GetByIdAsync.Return("value")` |
| 3. Standalone Class | `stub.FetchAsync.Return("value")` → `stub.Object` |
| 4. Generic Standalone Class | `stub.GetByIdAsync.Return("value")` → `stub.Object` |
| 5. Inline Interface | `stub.FetchAsync.Return("value")` |
| 6. Inline Class | `stub.FetchAsync.Return("value")` → `stub.Object` |
| 7. Inline Delegate | `stub.Interceptor.Return(42)` |
| 8. Open Generic Interface | `stub.GetByIdAsync.Return("value")` |
| 9. Open Generic Class | `stub.GetByIdAsync.Return("value")` → `stub.Object` |

---

## Verification

Async methods verify the same way as sync methods:

```csharp
await service.FetchAsync(1);
await service.FetchAsync(2);

stub.FetchAsync.Verify(Called.Exactly(2));
stub.FetchAsync.LastArg; // 2 (last argument)
```

---

## Async Stub Overrides

Standalone stubs can define async stub overrides:

```csharp
protected override async Task<string> ProcessAsync_(string input)
{
    await Task.Delay(1);
    return $"[Async: {input}]";
}

protected override async ValueTask<int> ComputeAsync_(int value)
{
    await Task.Yield();
    return value * 2;
}
```

`Return()` supersedes async stub overrides per-test, same as sync methods.

---

## Quick Reference

| Task | Code |
|------|------|
| Return value (auto-wrap) | `stub.Method.Return("value")` |
| Return callback (auto-wrap) | `stub.Method.Return((args) => result)` |
| Return full async | `stub.Method.Return((args) => Task.FromResult(result))` |
| Void async callback | `stub.Method.Call((args) => { })` |
| Sequence (auto-wrap) | `stub.Method.Return("a", "b", "c")` |
| When chain (auto-wrap) | `stub.Method.When(arg).Return("value")` |
| Verify | `stub.Method.Verify(Called.Once)` |
