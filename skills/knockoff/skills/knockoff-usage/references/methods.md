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

<!-- snippet: methods-params-sequence-intro -->
```cs
// Use PARAMS SEQUENCE for multiple constant values:
// Returns 1, then 2, then 3, then repeats 3
stub.GetValue.Returns(1, 2, 3);

// Mix callbacks with params for complex sequences:
// First call uses callback, then returns 100, 200, 300
addStub.Calculate.OnCall((x, y) => x + y).ThenReturns(100, 200, 300);
```
<!-- endSnippet -->

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

<!-- snippet: methods-sequence-nsub-style -->
```cs
// Returns 1 on first call, 2 on second, 3 on third and all subsequent calls
stub.GetValue.Returns(1, 2, 3);

IValueSvc calc = stub;
var r1 = calc.GetValue();  // 1
var r2 = calc.GetValue();  // 2
var r3 = calc.GetValue();  // 3
var r4 = calc.GetValue();  // 3 (repeats last value)
```
<!-- endSnippet -->

This matches NSubstitute's syntax for easier migration:
- NSubstitute: `substitute.Method().Returns(1, 2, 3);`
- KnockOff: `stub.Method.Returns(1, 2, 3);`

### Single Value vs Params Sequence

C# overload resolution distinguishes between single values and sequences:

<!-- snippet: methods-single-vs-params -->
```cs
// Single value - repeats indefinitely (no sequence)
singleStub.GetValue.Returns(42);

// Params sequence - progresses through values, repeats last
paramsStub.GetValue.Returns(1, 2, 3);
```
<!-- endSnippet -->

### Mixing Callbacks with Params

Use `OnCall()` for the first value when you need callback logic, then `ThenReturns(params)` for subsequent constant values:

<!-- snippet: methods-sequence-callback-then-params-full -->
```cs
// First call: compute dynamically
// Then: return 100, 200, 300 in sequence
stub.Calculate.OnCall((a, b) => a + b).ThenReturns(100, 200, 300);

ICalculatorSvc calc = stub;
var r1 = calc.Calculate(1, 2);  // 3 (computed: 1 + 2)
var r2 = calc.Calculate(0, 0);  // 100
var r3 = calc.Calculate(0, 0);  // 200
var r4 = calc.Calculate(0, 0);  // 300
var r5 = calc.Calculate(0, 0);  // 300 (repeats last)
```
<!-- endSnippet -->

### Async Methods with Params Sequences

For `Task<T>` and `ValueTask<T>` methods, params values are auto-wrapped - no `Task.FromResult()` needed:

<!-- snippet: methods-sequence-params-async-full -->
```cs
// Async values auto-wrapped - no Task.FromResult needed
stub.GetDataAsync.Returns("first", "second", "third");

IDataSvc service = stub;
var r1 = await service.GetDataAsync(1);  // "first"
var r2 = await service.GetDataAsync(2);  // "second"
var r3 = await service.GetDataAsync(3);  // "third"
var r4 = await service.GetDataAsync(4);  // "third" (repeats)
```
<!-- endSnippet -->

### Sequence Exhaustion Behavior

By default, sequences repeat the last value after exhaustion (matching NSubstitute):

<!-- snippet: methods-sequence-exhaustion-params -->
```cs
stub.GetValue.Returns(1, 2);

IValueSvc calc = stub;
var r1 = calc.GetValue();  // 1
var r2 = calc.GetValue();  // 2
var r3 = calc.GetValue();  // 2 (repeats last)
var r4 = calc.GetValue();  // 2 (still repeats)
```
<!-- endSnippet -->

For different exhaustion behaviors:

| Behavior | How to Configure |
|----------|-----------------|
| Repeat last value (default) | `Returns(1, 2, 3)` or `OnCall(...).ThenReturns(...)` |
| Return default(T) | `OnCall(...).ThenReturns(...).ThenDefault()` |
| Throw exception | Set `stub.Strict = true` |

### Sequence Verification

Params sequences support verification to ensure all values were consumed:

<!-- snippet: methods-sequence-params-verify -->
```cs
var sequence = stub.GetValue.Returns(1, 2, 3);

IValueSvc calc = stub;
calc.GetValue();  // 1
calc.GetValue();  // 2
calc.GetValue();  // 3

sequence.Verify();  // Passes - all 3 values used
```
<!-- endSnippet -->

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

**Note:** User method interceptors have different reset semantics. See the User Method Interceptors section below.

---

## User Method Interceptors (Stand-Alone Pattern)

When you define a **user method** (override a virtual method with underscore suffix in a Stand-Alone stub), the interceptor uses a clean name (e.g., `GetById`, not `GetById2`). These interceptors support `OnCall()` and `Returns()` to override the user method.

### OnCall Supersedes User Method

<!-- snippet: user-methods-standalone-example -->
```cs
[KnockOff]
public partial class SkillRepoStub : ISkillRepo { }

public partial class SkillRepoStub
{
    // Override virtual method with underscore suffix - compiler enforces signature!
    protected override User? GetById_(int id) => new User { Id = id, Name = "Default" };
}
```
<!-- endSnippet -->

<!-- snippet: user-methods-standalone-usage -->
```cs
// Usage:
var stub = new SkillRepoStub();
ISkillRepo repo = stub;

// Without OnCall: user method provides behavior
var user1 = repo.GetById(1);  // Name = "Default"

// With OnCall: callback supersedes user method (clean interceptor name)
stub.GetById.OnCall(id => new User { Id = id, Name = "Override" });
var user2 = repo.GetById(2);  // Name = "Override"
```
<!-- endSnippet -->

### Returns for Constant Values

`Returns()` provides constant values. For async methods (`Task<T>`, `ValueTask<T>`), the value is auto-wrapped:

```cs
stub.GetById.Returns(new User { Id = 99, Name = "Fixed" });
stub.GetUserAsync.Returns(new User { Id = 1 });  // Auto-wrapped in Task.FromResult
```

### Full Tracking Support

User method interceptors provide full tracking even when using `OnCall`:

<!-- snippet: user-methods-tracking-with-oncall -->
```cs
stub.GetById.OnCall(id => new User { Id = id });
repo.GetById(42);

stub.GetById.Verify(Times.Once);
Assert.Equal(42, stub.GetById.LastArg);
```
<!-- endSnippet -->

### Reset Preserves OnCall Configuration

Unlike regular method interceptors, user method interceptors preserve `OnCall` configuration across `Reset()`:

<!-- snippet: user-methods-reset-preserves-oncall -->
```cs
stub.GetById.OnCall(id => new User { Id = id });
repo.GetById(1);
stub.GetById.Verify(Times.Once);

stub.GetById.Reset();
stub.GetById.Verify(Times.Never);  // Tracking cleared

repo.GetById(2);  // Still uses OnCall callback (not reset to user method)
```
<!-- endSnippet -->

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
| Override user method | `stub.Method.OnCall((args) => returnValue)` |
| Override user method (constant) | `stub.Method.Returns(value)` |
| Override async user method | `stub.AsyncMethod.Returns(value)` (auto-wraps) |

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
- **User methods**: Override virtual methods (with underscore suffix) in Stand-Alone stubs for default behavior
- **User method override**: Use `stub.Method.OnCall()` or `stub.Method.Returns()` to supersede user method
- **User method reset**: `Reset()` preserves OnCall configuration (different from regular interceptors)

---

**UPDATED:** 2026-02-02
