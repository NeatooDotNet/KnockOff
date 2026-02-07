# Async Patterns

KnockOff provides three configuration tiers for async methods (`Task<T>`, `ValueTask<T>`), each with increasing control. The first two tiers auto-wrap return values so you never write `Task.FromResult` for simple cases.

**See also:**
- [Method Interceptors](methods.md) - Core `Returns` and `Execute` patterns
- [API Consistency Matrix](api-consistency-matrix.md#feature-12-async-method-auto-wrapping) - Cross-pattern async support
- [Verification Guide](verification.md) - Details on `Verifiable()` and `stub.Verify()`

---

## The Three-Tier Async API

For an async method like `Task<string> GetDataAsync(int id)`:

| Tier | API | Accepts | Auto-wraps? |
|------|-----|---------|-------------|
| 1 | `Return(value)` | `string` | Yes -- `Task.FromResult(value)` |
| 2 | `Return((id) => value)` | `Func<int, string>` | Yes -- `Task.FromResult(value)` |
| 3 | `Return((id) => Task.FromResult(value))` | `Func<int, Task<string>>` | No -- you provide the Task |

All three tiers work identically across all 9 stub patterns (1–9), including delegate stubs.

---

## Task<T> Methods

### Tier 1: Return (Recommended for Constants)

`Return(unwrappedValue)` auto-wraps the value in `Task.FromResult`:

```csharp
// Given: Task<string?> GetDataAsync(int id)
stub.GetDataAsync.Returns("hello");

IDataService svc = stub;
var result = await svc.GetDataAsync(1); // "hello"
```

No `Task.FromResult` needed. This is the simplest syntax when the return value is constant.

### Tier 2: Simplified Callback (Recommended for Dynamic Values)

`Return(Func<..., T>)` receives typed arguments and returns the unwrapped type. KnockOff auto-wraps the result:

```csharp
// Callback returns string, not Task<string> — auto-wrapped
stub.GetDataAsync.Returns((id) => $"Data-{id}");

IDataService svc = stub;
var result = await svc.GetDataAsync(42); // "Data-42"
```

Use this when the return value depends on the arguments but you don't need async behavior in the callback itself.

### Tier 3: Full Delegate (For Async Callbacks)

`Return(Func<..., Task<T>>)` gives full control -- you construct the Task yourself:

```csharp
// Callback returns Task<string?> directly
stub.GetDataAsync.Returns((int id) => Task.FromResult<string?>($"Full-{id}"));

IDataService svc = stub;
var result = await svc.GetDataAsync(99); // "Full-99"
```

Use this when you need `async`/`await` inside the callback (e.g., simulating delays) or when returning faulted tasks.

---

## Void Async Methods (Task Return)

For methods returning `Task` with no value, `Execute` accepts an `Action`:

```csharp
// Given: Task SaveDataAsync(string data)
string? savedData = null;
stub.SaveDataAsync.Execute((data) => savedData = data);

IDataService svc = stub;
await svc.SaveDataAsync("important data");
// savedData == "important data"
```

The generated interceptor returns `Task.CompletedTask` automatically.

---

## ValueTask<T> Methods

The same three tiers apply to `ValueTask<T>`:

```csharp
// Tier 1: Returns — auto-wraps in new ValueTask<T>(value)
stub.GetCachedAsync.Returns(cachedUser);

// Tier 2: Simplified callback — returns T, auto-wrapped
stub.GetCachedAsync.Returns((id) => new User { Id = id });

// Tier 3: Full delegate — returns ValueTask<T> directly
stub.GetCachedAsync.Returns((id) => new ValueTask<User?>(new User { Id = id }));
```

---

## Delegate Stubs — Full Auto-Wrapping (Pattern 7)

Delegate stubs (Pattern 7) support the same three-tier async API as all other patterns. After MethodInterceptorRenderer reuse, async delegates like `delegate Task<int> AsyncOperation(int x)` get auto-wrapping:

```csharp
// Given: delegate Task<int> AsyncOperation(int x)

// Tier 1: Returns takes the inner type — auto-wraps in Task.FromResult
stub.Interceptor.Returns(42);

// Tier 2: Simplified callback — returns int, auto-wrapped
stub.Interceptor.Returns((int x) => x * 2);

// Tier 3: Full delegate — returns Task<int> directly
stub.Interceptor.Returns((int x) => Task.FromResult(x * 2));
```

**See also:** [Delegate Stubs Guide](delegates.md)

---

## Sequences with Async Methods

Async methods support params-style sequences with auto-wrapping:

```csharp
// Returns multiple values — each auto-wrapped in Task.FromResult
stub.GetDataAsync.Returns("first", "second", "third");
// Call 1: "first", Call 2: "second", Call 3+: "third" (repeats last)

// Callback sequences also work
stub.GetDataAsync
    .Returns((id) => "initial")
    .ThenReturns((id) => "updated");
```

---

## Simulating Delays

Use async lambdas with the Tier 3 API to simulate asynchronous delays:

```csharp
stub.GetDataAsync.Returns(async (id) =>
{
    await Task.Delay(50);
    return $"Delayed-{id}";
});
```

---

## Simulating Failures

### Using Task.FromException

Return a faulted task using `Task.FromException<T>`:

```csharp
stub.GetDataAsync.Returns((id) =>
    Task.FromException<string?>(new NotFoundException($"Item {id} not found")));
```

### Throwing Directly

Throw exceptions directly in the callback. The exception is thrown when the method is awaited:

```csharp
stub.GetDataAsync.Returns((int id) =>
    throw new NotFoundException($"Item {id} not found"));
```

When throwing directly in a simplified callback (Tier 2), you may need to specify the parameter type explicitly to disambiguate overloads.

---

## Choosing Your Tier

| Scenario | Recommended Tier | Example |
|----------|-----------------|---------|
| Constant return value | Tier 1: `Returns` | `stub.Method.Returns("value")` |
| Value depends on args | Tier 2: Simplified callback | `stub.Method.Returns((id) => ...)` |
| Need async/await in callback | Tier 3: Full delegate | `stub.Method.Returns(async (id) => ...)` |
| Simulating failures | Tier 3: Full delegate | `stub.Method.Returns((id) => Task.FromException<T>(...))` |
| Delegate stubs | Same 3 tiers | `stub.Interceptor.Returns(42)` (auto-wraps) |

---

## Key Takeaways

- **Three tiers** for async methods: `Return(T)`, `Return(Func<..., T>)`, `Return(Func<..., Task<T>>)`
- **Tiers 1 and 2 auto-wrap** -- you work with the unwrapped type, KnockOff handles `Task.FromResult`
- **Tier 3 gives full control** -- use for async lambdas, delays, and faulted tasks
- **Void async methods** use `Call(Action<...>)` -- `Task.CompletedTask` is returned automatically
- **ValueTask<T>** follows the same three tiers with `ValueTask` wrapping
- **All 9 patterns** (including Pattern 7 delegates) support identical async APIs
- **All interceptor features** (verification, argument capture, sequences, When chains) work with async methods

---

**UPDATED:** 2026-02-05
