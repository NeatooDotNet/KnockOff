# Delegate Stub Reference

Delegate stubs are created with `[KnockOff<DelegateType>]`. They generate a stub class with an `Interceptor` property for configuration and verification. The stub implicitly converts to the delegate type.

---

## Named Delegates Only

KnockOff requires **named delegate types**. `Func<>` and `Action<>` are NOT supported.

```csharp
// WRONG: Func<>/Action<> are not distinct types
// [KnockOff<Func<int, int, int>>]  // Does NOT work

// RIGHT: Define a named delegate
public delegate int ArithmeticOperation(int a, int b);
[KnockOff<ArithmeticOperation>]
public partial class MyTests { }
```

---

## Basic Usage

```csharp
var stub = new Stubs.ArithmeticOperation();

// Configure via Interceptor
stub.Interceptor.Return(42);

// Implicit conversion to delegate type
ArithmeticOperation operation = stub;
var result = operation(2, 3); // 42
```

**The stub must be converted to the delegate type before invocation.** The Interceptor is for configuration, not direct invocation.

---

## Configuration

### Return(value) — Constant Value

```csharp
stub.Interceptor.Return(100);

ArithmeticOperation op = stub;
op(1, 2);  // 100
op(10, 20); // 100
```

### Return(callback) — Dynamic Behavior

```csharp
stub.Interceptor.Return((a, b) => a + b);

ArithmeticOperation op = stub;
op(2, 3);  // 5
op(10, 20); // 30
```

### Call(callback) — Void Delegates

```csharp
// delegate void LogAction(string message);
var stub = new Stubs.LogAction();
var logged = new List<string>();

stub.Interceptor.Call(msg => logged.Add(msg));

LogAction logger = stub;
logger("Hello");  // logged: ["Hello"]
```

---

## Sequences

```csharp
// Params syntax (NSubstitute-style)
stub.Interceptor.Return(10, 20, 30);

ArithmeticOperation op = stub;
op(0, 0); // 10
op(0, 0); // 20
op(0, 0); // 30
op(0, 0); // 30 (repeats last)

// Callback sequences
stub.Interceptor.Return((a, b) => a + b)
    .ThenReturn((a, b) => a * b)
    .ThenReturn(999);
```

---

## When Chains

### Value Matching

```csharp
stub.Interceptor.When(1, 2).Return(100)
    .ThenWhen(3, 4).Return(200)
    .ThenCall((a, b) => a + b);  // Terminal fallback

ArithmeticOperation op = stub;
op(1, 2); // 100
op(3, 4); // 200
op(5, 6); // 11 (fallback)
```

### Predicate Matching (Void Delegates)

```csharp
// delegate void LogAction(string message);
stub.Interceptor.When(msg => msg.StartsWith("IMPORTANT:"))
    .Call(msg => important.Add(msg))
    .ThenWhen(msg => true)
    .Call(msg => normal.Add(msg));
```

---

## Argument Tracking

| Delegate Params | Property | Type |
|----------------|----------|------|
| Single parameter | `LastArg` | `T` |
| Multiple parameters | `LastArgs` | Named tuple `(T1 a, T2 b)` |
| No parameters | — | No tracking property |

```csharp
// Multi-param: LastArgs
stub.Interceptor.Return(0);
ArithmeticOperation op = stub;
op(5, 10);
var args = stub.Interceptor.LastArgs; // (5, 10)

// Single-param: LastArg
// delegate void LogAction(string message);
LogAction logger = logStub;
logger("Test");
var arg = logStub.Interceptor.LastArg; // "Test"
```

---

## Verification

```csharp
stub.Interceptor.Return(0);
ArithmeticOperation op = stub;

op(1, 2);
op(3, 4);

stub.Interceptor.Verify();              // At least once
stub.Interceptor.Verify(Called.Exactly(2)); // Exactly twice

// Verifiable for batch
stub.Interceptor.Return(0).Verifiable();
// ... later ...
stub.Verify();
```

---

## Generic Delegates

Closed generic delegates use the **simple name** in the Stubs namespace:

```csharp
// delegate T Factory<T>();
// [KnockOff<Factory<string>>]

var stub = new Stubs.Factory();  // NOT Stubs.Factory<string>
stub.Interceptor.Return("Created item");

Factory<string> factory = stub;
var item = factory(); // "Created item"
```

---

## Async Delegates

Async delegates (`Task<T>`, `ValueTask<T>`) support auto-wrapping:

```csharp
// delegate Task<int> AsyncOperation(int x);
var stub = new Stubs.AsyncOperation();

// Tier 1: Value — auto-wraps in Task.FromResult
stub.Interceptor.Return(42);

// Tier 2: Simplified callback — auto-wrapped
stub.Interceptor.Return((int x) => x * 2);

// Tier 3: Full callback — direct
stub.Interceptor.Return((int x) => Task.FromResult(x * 2));
```

---

## Strict Mode

```csharp
stub.Strict = true;

ArithmeticOperation op = stub;
// op(1, 2); // Throws StubException — not configured
```

---

## Reset

`Reset()` clears:
- Call counts
- `LastArg` / `LastArgs`
- Sequence index, When chain position

`Reset()` preserves:
- `Return` / `Call` callbacks
- Sequence structure, When chain structure
- Verifiable marking

```csharp
stub.Interceptor.Return(42);
op(1, 2);
stub.Interceptor.Reset();

op(3, 4);  // Still returns 42 (config preserved)
stub.Interceptor.Verify(Called.Once); // Only 1 call after reset
```

---

## Quick Reference

| Task | Code |
|------|------|
| Create stub | `var stub = new Stubs.MyDelegate();` |
| Configure return | `stub.Interceptor.Return(value)` |
| Configure callback | `stub.Interceptor.Return((args) => result)` |
| Configure void | `stub.Interceptor.Call((args) => { })` |
| Value sequence | `stub.Interceptor.Return(1, 2, 3)` |
| When matching | `stub.Interceptor.When(args).Return(value)` |
| Convert to delegate | `MyDelegate del = stub;` |
| Check last args | `stub.Interceptor.LastArgs` |
| Verify calls | `stub.Interceptor.Verify(Called.Once)` |
| Strict mode | `stub.Strict = true` |
| Reset | `stub.Interceptor.Reset()` |
