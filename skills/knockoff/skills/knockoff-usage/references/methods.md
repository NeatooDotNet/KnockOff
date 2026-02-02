# Method Interceptors Reference

Method interceptors track calls, capture arguments, and configure return values for interface methods in your stub. Each method on the stubbed interface gets a corresponding interceptor property that provides verification and configuration capabilities.

---

## Configuring Method Behavior

### Void Methods

Configure void methods using `OnCall` with an `Action`:

<!-- snippet: methods-oncall-void -->
```cs
// OnCall for void methods uses Action<...params>
stub.LogMessage.OnCall((message) => logged.Add(message));
```
<!-- endSnippet -->

### Methods with Return Values

#### Using a Callback

Configure methods that return values using `OnCall` with a `Func`:

<!-- snippet: methods-oncall-return -->
```cs
// OnCall with return value: Func<...params, TReturn>
stub.GetUserName.OnCall((userId) => "TestUser");
```
<!-- endSnippet -->

#### Using a Fixed Value

For simple scenarios where the return value does not depend on arguments, use the value overload:

<!-- snippet: methods-oncall-value -->
```cs
// Returns - simpler syntax when you don't need callback logic
stub.GetUserName.Returns("StaticUser");
```
<!-- endSnippet -->

#### When to Use Value, Params Sequence, or Callback

<!-- snippet: methods-oncall-value-vs-callback -->
```cs
// Use VALUE when returning a fixed result:
stub.GetUserName.Returns("Alice");

// Use CALLBACK when you need:
// - Dynamic values based on arguments
// - Side effects
// - Conditional logic
stub.GetUserName.OnCall((userId) => userId > 100 ? "Admin" : "User");

// Both return tracking objects for verification
```
<!-- endSnippet -->

**Params sequences** (NSubstitute-style):

```cs
// Use PARAMS SEQUENCE for multiple constant values:
// Returns 1, then 2, then 3, then repeats 3
stub.Calculate.Returns(1, 2, 3);

// Mix callbacks with params for complex sequences:
// First call uses callback, then returns 100, 200, 300
stub.Calculate.OnCall((x) => x * 2).ThenReturns(100, 200, 300);
```

| Scenario | Recommended Syntax |
|----------|-------------------|
| Fixed value (always same) | `Returns(value)` |
| Constant sequence | `Returns(first, second, ...)` |
| Dynamic based on args | `OnCall((args) => computed)` |
| Callback then constants | `OnCall(cb).ThenReturns(x, y, z)` |

### Methods with Multiple Parameters

Methods with multiple parameters include all parameters in the callback:

<!-- snippet: methods-oncall-multi-param -->
```cs
// All method parameters are passed to the callback in order
stub.ValidateCredentials.OnCall((username, password) =>
    username == "admin" && password == "secret");
```
<!-- endSnippet -->

---

## Verifying Method Calls

### Using Verify()

Call `.Verify()` on the tracking object returned by `OnCall`:

<!-- snippet: methods-verify-wascalled -->
```cs
// Mark with Verifiable(), then stub.Verify() checks all marked members
stub.Save.OnCall((entity) => { }).Verifiable();
```
<!-- endSnippet -->

### Verifying Call Frequency with Times

Use `Times` to specify exact call count requirements:

<!-- snippet: methods-verify-callcount -->
```cs
// Verify exact call count (throws if different)
tracking.Verify(Times.Exactly(2));
```
<!-- endSnippet -->

**Available Times constraints:**

| Constraint | Description |
|------------|-------------|
| `Times.Never` | Method must not be called |
| `Times.Once` | Method must be called exactly once |
| `Times.AtLeastOnce` | Method must be called one or more times |
| `Times.Exactly(n)` | Method must be called exactly n times |

### Using Verifiable() for Batch Verification

For batch verification of multiple methods, use `.Verifiable()` then call `stub.Verify()`:

<!-- snippet: methods-verify-verifiable -->
```cs
// Mark expected calls with Verifiable(), then stub.Verify() checks all
stub.Save.OnCall((entity) => { }).Verifiable(Times.Once);
stub.GetById.OnCall((id) => new User { Id = id }).Verifiable();
```
<!-- endSnippet -->

**Key difference:**
- `tracking.Verify()` - Verifies a single method
- `stub.Verify()` - Verifies all methods marked with `.Verifiable()`

---

## Capturing Arguments

### Single Parameter Methods

Access the last call's argument using `LastArg`:

<!-- snippet: methods-capture-single -->
```cs
// LastArg captures the most recent call's argument
int capturedId = tracking.LastArg;
```
<!-- endSnippet -->

### Multiple Parameter Methods

Access arguments using the `LastArgs` named tuple:

<!-- snippet: methods-capture-multiple -->
```cs
// LastArgs is a named tuple with all parameters
var (username, password) = tracking.LastArgs;
```
<!-- endSnippet -->

---

## Async Methods

KnockOff provides simplified syntax for async methods, eliminating verbose `Task.FromResult()` and `Task.CompletedTask` wrappers.

### Task<T> and ValueTask<T> Methods

For methods returning `Task<T>` or `ValueTask<T>`, you have three options:

<!-- snippet: async-task-value-overload -->
```cs
// KnockOff auto-wraps the value in Task.FromResult
stub.GetUserAsync.Returns(new User { Id = 42, Name = "Alice" });
```
<!-- endSnippet -->

<!-- snippet: async-task-simplified-callback -->
```cs
// OnCall() with unwrapped return type - auto-wrapped in Task.FromResult
stub.GetUserAsync.OnCall((id) => new User { Id = id, Name = "Alice" }).Verifiable();
```
<!-- endSnippet -->

<!-- snippet: async-task-result -->
```cs
// Use Task.FromResult when you need parameter-based return values
stub.GetUserAsync.OnCall((id) =>
    Task.FromResult<User?>(new User { Id = id, Name = "Alice" })).Verifiable();
```
<!-- endSnippet -->

### Void Async Methods (Task/ValueTask)

For methods returning `Task` or `ValueTask` (no result), use `Action` callbacks - KnockOff auto-returns the completed task:

<!-- snippet: async-task-simplified-void -->
```cs
// Action callback for void async - Task.CompletedTask auto-returned
stub.UpdateUserAsync.OnCall((user) => updatedUsers.Add(user)).Verifiable();
```
<!-- endSnippet -->

### Async Callback Syntax Decision Guide

| Return Type | Simplest Syntax | When to Use Full Syntax |
|-------------|-----------------|------------------------|
| `Task<T>` | `OnCall((args) => value)` | When callback needs actual async operations |
| `ValueTask<T>` | `OnCall((args) => value)` | When callback needs actual async operations |
| `Task` | `OnCall((args) => { action(); })` | When callback needs to return a specific Task |
| `ValueTask` | `OnCall((args) => { action(); })` | When callback needs to return a specific ValueTask |

---

## Method Sequences

When a method should return different values on successive calls, use sequences. KnockOff supports NSubstitute-style concise syntax for creating value sequences.

### Concise Value Sequences (NSubstitute-style)

Use `Returns(first, ...rest)` to create a sequence from multiple values in a single call:

```cs
// Returns 1 on first call, 2 on second, 3 on third and all subsequent calls
stub.Calculate.Returns(1, 2, 3);

ICalculator calc = stub;
var r1 = calc.Calculate(0);  // 1
var r2 = calc.Calculate(0);  // 2
var r3 = calc.Calculate(0);  // 3
var r4 = calc.Calculate(0);  // 3 (repeats last value)
```

This matches NSubstitute's syntax for easier migration:
- NSubstitute: `substitute.Method().Returns(1, 2, 3);`
- KnockOff: `stub.Method.Returns(1, 2, 3);`

### Single Value vs Params Sequence

C# overload resolution distinguishes between single values and sequences:

```cs
// Single value - repeats indefinitely (no sequence)
stub.Calculate.Returns(42);

// Params sequence - progresses through values, repeats last
stub.Calculate.Returns(1, 2, 3);
```

### Mixing Callbacks with Params

Use `OnCall()` for the first value when you need callback logic, then `ThenReturns(params)` for subsequent constant values:

```cs
// First call: compute dynamically
// Then: return 100, 200, 300 in sequence
stub.Add.OnCall((a, b) => a + b).ThenReturns(100, 200, 300);

ICalculator calc = stub;
var r1 = calc.Add(1, 2);  // 3 (computed: 1 + 2)
var r2 = calc.Add(0, 0);  // 100
var r3 = calc.Add(0, 0);  // 200
var r4 = calc.Add(0, 0);  // 300
var r5 = calc.Add(0, 0);  // 300 (repeats last)
```

### Async Methods with Params Sequences

For `Task<T>` and `ValueTask<T>` methods, params values are auto-wrapped - no `Task.FromResult()` needed:

```cs
// Async values auto-wrapped - no Task.FromResult needed
stub.GetDataAsync.Returns("first", "second", "third");

IDataService service = stub;
var r1 = await service.GetDataAsync(1);  // "first"
var r2 = await service.GetDataAsync(2);  // "second"
var r3 = await service.GetDataAsync(3);  // "third"
var r4 = await service.GetDataAsync(4);  // "third" (repeats)
```

### Sequence Exhaustion Behavior

By default, sequences repeat the last value after exhaustion (matching NSubstitute):

```cs
stub.GetValue.Returns(1, 2);

var r1 = calc.GetValue();  // 1
var r2 = calc.GetValue();  // 2
var r3 = calc.GetValue();  // 2 (repeats last)
var r4 = calc.GetValue();  // 2 (still repeats)
```

For different exhaustion behaviors:

| Behavior | How to Configure |
|----------|-----------------|
| Repeat last value (default) | `Returns(1, 2, 3)` or `OnCall(...).ThenReturns(...)` |
| Return default(T) | `OnCall(...).ThenReturns(...).ThenDefault()` |
| Throw exception | Set `stub.Strict = true` |

### Sequence Verification

Params sequences support verification to ensure all values were consumed:

```cs
var sequence = stub.Calculate.Returns(1, 2, 3);

ICalculator calc = stub;
calc.Calculate(0);  // 1
calc.Calculate(0);  // 2
calc.Calculate(0);  // 3

sequence.Verify();  // Passes - all 3 values used
```

---

## Handling Overloaded Methods

When an interface has overloaded methods, KnockOff distinguishes them by the callback signature. The fully-typed lambda tells KnockOff which overload to configure:

<!-- snippet: methods-overloads -->
```cs
// Fully-typed lambda tells KnockOff which overload to configure
stub.Find.OnCall(() => new List<User>());
stub.Find.OnCall((int id) => new User { Id = id, Name = "ById" });
stub.Find.OnCall((string name) => new User { Id = 1, Name = name });
```
<!-- endSnippet -->

**Important:** The callback signature determines which overload is configured. Use explicit types in lambdas when parameter types are ambiguous.

---

## Resetting Interceptors

Clear tracking state and remove callbacks using `Reset()`:

<!-- snippet: methods-reset -->
```cs
// Reset clears call count, captured arguments, and callbacks
stub.ProcessData.Reset();
```
<!-- endSnippet -->

**Use cases for Reset():**
- Reusing a stub instance across multiple test phases
- Testing a sequence of interactions where counts should restart
- Isolating assertions between test setup and execution phases

---

## Complete Example

This example demonstrates a realistic test using method configuration, execution, and verification:

<!-- snippet: methods-complete-example -->
```cs
// Configure stub with tracking
var getTracking = stub.GetUser.OnCall((id) => id == 1 ? testUser : null).Verifiable();
var saveTracking = stub.SaveUser.OnCall((user) => { }).Verifiable();
```
<!-- endSnippet -->

---

## Quick Reference

| Task | Code |
|------|------|
| Configure void method | `stub.Method.OnCall((args) => { })` |
| Configure method with callback | `stub.Method.OnCall((args) => returnValue)` |
| Configure method with value | `stub.Method.Returns(fixedValue)` |
| Create value sequence (NSubstitute-style) | `stub.Method.Returns(1, 2, 3)` |
| Mix callback with value sequence | `stub.Method.OnCall(cb).ThenReturns(x, y, z)` |
| Configure async Task<T> (simplified) | `stub.AsyncMethod.Returns(value)` |
| Configure async Task<T> sequence | `stub.AsyncMethod.Returns(v1, v2, v3)` |
| Configure async Task (void, simplified) | `stub.AsyncMethod.OnCall((args) => { action(); })` |
| Verify method was called | `tracking.Verify()` |
| Verify call count | `tracking.Verify(Times.Exactly(n))` |
| Mark for batch verify | `stub.Method.OnCall(...).Verifiable()` |
| Batch verify all | `stub.Verify()` |
| Get last single arg | `tracking.LastArg` |
| Get last multiple args | `tracking.LastArgs` (named tuple) |
| Reset interceptor | `stub.Method.Reset()` |

---

## Key Takeaways

- **OnCall signature**: Callback receives only the method parameters
- **Value vs Callback**: Use `Returns(value)` for fixed returns, `OnCall(callback)` for dynamic logic
- **Params sequences**: Use `Returns(1, 2, 3)` for concise value sequences (matches NSubstitute)
- **Sequence exhaustion**: Last value repeats after exhaustion (NSubstitute-like behavior)
- **Async auto-wrapping**: Params values auto-wrap for `Task<T>` and `ValueTask<T>` - no `Task.FromResult()` needed
- **Verification**: Use `tracking.Verify(Times)` for single methods or `.Verifiable()` + `stub.Verify()` for batch
- **Arguments**: `LastArg` for single parameters, `LastArgs` tuple for multiple
- **Overloads**: Distinguished by callback parameter types - use explicit types in lambdas
- **Reset**: Clears call counts and tracking state

---

**UPDATED:** 2026-02-02
