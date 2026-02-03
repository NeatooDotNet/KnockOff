[Guides](../docs/guides) > Method Interceptors

# Method Interceptors

Method interceptors track calls, capture arguments, and configure return values for interface methods in your stub. Each method on the stubbed interface gets a corresponding interceptor property that provides verification and configuration capabilities.

**Key concept**: The `OnCall` callback receives only the method's parameters—you configure behavior based on the inputs to the method being called.

---

## Configuring Method Behavior

### Void Methods

Configure void methods using `OnCall` with an `Action` that matches the method parameters:

<!-- snippet: methods-oncall-void -->
```cs
// OnCall for void methods uses Action<...params>
stub.LogMessage.OnCall((message) => logged.Add(message));
```
<!-- endSnippet -->

### Methods with Return Values

Configure methods that return values using `OnCall`. You have two options:

**1. Callback syntax** - Use a `Func` for dynamic values or conditional logic:

<!-- snippet: methods-oncall-return -->
```cs
// OnCall with return value: Func<...params, TReturn>
stub.GetUserName.OnCall((userId) => "TestUser");
```
<!-- endSnippet -->

**2. Value syntax** - Pass the return value directly for fixed results:

<!-- snippet: methods-oncall-value -->
```cs
// Returns - simpler syntax when you don't need callback logic
stub.GetUserName.Returns("StaticUser");
```
<!-- endSnippet -->

Choose the value syntax when returning a constant, or the callback syntax when you need to inspect parameters or apply logic.

### Methods with Multiple Parameters

The callback signature includes all method parameters in the same order:

<!-- snippet: methods-oncall-multi-param -->
```cs
// All method parameters are passed to the callback in order
stub.ValidateCredentials.OnCall((username, password) =>
    username == "admin" && password == "secret");
```
<!-- endSnippet -->

---

## Verifying Method Calls

KnockOff provides two verification patterns:
1. **Individual tracking**: Store the object returned by `OnCall` and call `.Verify()` on it
2. **Batch verification**: Mark interceptors with `.Verifiable()` then call `stub.Verify()` once

The tracking object returned by `OnCall` provides access to `Verify()`, `LastArg`/`LastArgs`, and call count information.

### Using Verify()

Call `.Verify()` on the tracking object returned by `OnCall` to verify that specific method was called:

<!-- snippet: methods-verify-wascalled -->
```cs
// Mark with Verifiable(), then stub.Verify() checks all marked members
stub.Save.OnCall((entity) => { }).Verifiable();
```
<!-- endSnippet -->

### Verifying Call Frequency

Use `Times` to specify exact call count requirements. Available options include `Once`, `Never`, `AtLeastOnce`, and `Exactly(n)`:

<!-- snippet: methods-verify-callcount -->
```cs
// Verify exact call count (throws if different)
tracking.Verify(Times.Exactly(2));
```
<!-- endSnippet -->

### Using Verifiable()

For batch verification of multiple methods, mark each with `.Verifiable()` then call `stub.Verify()` once to check all:

<!-- snippet: methods-verify-verifiable -->
```cs
// Mark expected calls with Verifiable(), then stub.Verify() checks all
stub.Save.OnCall((entity) => { }).Verifiable(Times.Once);
stub.GetById.OnCall((id) => new User { Id = id }).Verifiable();
```
<!-- endSnippet -->

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

## Overloaded Methods

When an interface has overloaded methods, KnockOff generates numbered suffixes for each overload:

<!-- snippet: methods-overloads -->
```cs
// Fully-typed lambda tells KnockOff which overload to configure
stub.Find.OnCall(() => new List<User>());
stub.Find.OnCall((int id) => new User { Id = id, Name = "ById" });
stub.Find.OnCall((string name) => new User { Id = 1, Name = name });
```
<!-- endSnippet -->

Overloads are numbered in the order they appear in the interface definition.

**Stand-Alone pattern with user methods:** Each overload gets its own virtual method in the generated base class. You can selectively override specific overloads without affecting others. See the [User Methods: Overloads](user-methods.md#overloads) section for details.

---

## Resetting Interceptors

Clear tracking state and remove callbacks using `Reset()`:

<!-- snippet: methods-reset -->
```cs
// Reset clears call count, captured arguments, and callbacks
stub.ProcessData.Reset();
```
<!-- endSnippet -->

This is useful when reusing a stub instance across multiple test phases or assertions.

---

## Sequences

Use sequences when a method should behave differently across multiple calls. KnockOff provides two approaches:

1. **Params syntax** (recommended for constant values) - `Returns(first, params rest)` creates a sequence in a single call
2. **Callback chaining** (for dynamic values) - Chain `ThenCall()` or `ThenReturns()` after `OnCall()`

### Concise Value Sequences (Params Syntax)

For constant value sequences, use the concise params syntax:

<!-- snippet: methods-sequence-params -->
```cs
// Returns(first, params rest) for value sequences
stub.GetValue.Returns(1, 2, 3);
```
<!-- endSnippet -->

This matches NSubstitute's `Returns(x, y, z)` syntax for easy migration. The sequence repeats the last value after exhaustion.

### Async Methods with Params

Async methods auto-wrap values - no `Task.FromResult` needed:

<!-- snippet: methods-sequence-params-async -->
```cs
// Async methods auto-wrap values - no Task.FromResult needed
stub.GetDataAsync.Returns("first", "second", "third");
```
<!-- endSnippet -->

### Mixing Callbacks with Value Params

Use `OnCall()` for the first callback, then `ThenReturns()` with params for subsequent values:

<!-- snippet: methods-sequence-callback-then-params -->
```cs
// OnCall for first callback, then ThenReturns for constant values
stub.Calculate
    .OnCall((x, y) => x + y)
    .ThenReturns(100, 200, 300);
```
<!-- endSnippet -->

### Callback Sequences

For callback sequences or mixed sequences with dynamic values, chain `ThenCall()` after `OnCall()`:

<!-- snippet: methods-sequence-basic -->
```cs
// Chain ThenCall() for callback sequences
stub.GetStatus
    .OnCall(() => "Pending")
    .ThenCall(() => "Processing")
    .ThenCall(() => "Complete");
```
<!-- endSnippet -->

Each callback in the sequence is invoked exactly once in order.

### Void Method Sequences

Sequences work with void methods using `Action` callbacks:

<!-- snippet: methods-sequence-void -->
```cs
// Void method sequences use Action callbacks
stub.Notify
    .OnCall((msg) => calls.Add("first"))
    .ThenCall((msg) => calls.Add("second"))
    .ThenCall((msg) => calls.Add("third"));
```
<!-- endSnippet -->

### Return Method Sequences

Sequences with return values use `Func` callbacks:

<!-- snippet: methods-sequence-return -->
```cs
// Return method sequences use Func callbacks
stub.Calculate
    .OnCall((x, y) => x + y)
    .ThenCall((x, y) => x * y)
    .ThenCall((x, y) => x - y);
```
<!-- endSnippet -->

The callback signature matches the method signature, just like `OnCall()`.

### Sequence Exhaustion

After the sequence is exhausted (all callbacks consumed), subsequent calls **repeat the last value** by default. This matches NSubstitute's behavior for easier migration and more forgiving tests.

<!-- snippet: methods-sequence-exhaustion -->
```cs
// After exhaustion: repeats last value (NSubstitute behavior)
stub.GetValue.OnCall(() => 1).ThenCall(() => 2).ThenCall(() => 3);
```
<!-- endSnippet -->

### Returning Default After Exhaustion

Use `ThenDefault()` when you want the sequence to return `default(T)` after exhaustion instead of repeating the last value:

<!-- snippet: methods-sequence-then-default -->
```cs
// ThenDefault() returns default(T) after exhaustion instead of repeating
stub.GetValue.OnCall(() => 1).ThenCall(() => 2).ThenDefault();
```
<!-- endSnippet -->

### Strict Mode Sequence Exhaustion

In strict mode, exhausted sequences throw `StubException.SequenceExhausted` regardless of `ThenDefault()`:

<!-- snippet: methods-sequence-strict -->
```cs
// Strict mode throws on sequence exhaustion
stub.Strict = true;
stub.GetValue.OnCall(() => 1).ThenCall(() => 2);
```
<!-- endSnippet -->

### Mixing Fixed Values and Dynamic Callbacks

You can mix fixed values and dynamic callbacks in the same sequence using `OnCall()`:

<!-- snippet: methods-sequence-mixed -->
```cs
// Mix fixed values with dynamic callbacks
stub.GetStatus
    .OnCall(() => "Initial")
    .ThenCall(() => DateTime.Now.ToString("HH:mm:ss"))
    .ThenCall(() => "Final");
```
<!-- endSnippet -->

**Note:** Use `OnCall(() => value)` to include fixed values in a sequence chain.

### Sequence Verification

Sequences can be verified like any other callback configuration:

<!-- snippet: methods-sequence-verification -->
```cs
// Verify sequence was called the expected number of times
sequence.Verify();
```
<!-- endSnippet -->

### Combining Sequences With Verification

<!-- snippet: methods-sequence-with-times -->
```cs
// Mark sequence for batch verification via stub.Verify()
stub.Process
    .OnCall(() => { })
    .ThenCall(() => { })
    .Verifiable();
```
<!-- endSnippet -->

---

## Complete Example

This example demonstrates method configuration, argument capturing, and verification in a realistic scenario. The example assumes a `UserService` class that depends on `ICompleteUserRepo`:

<!-- snippet: methods-complete-example -->
```cs
// Configure stub with tracking
var getTracking = stub.GetUser.OnCall((id) => id == 1 ? testUser : null).Verifiable();
var saveTracking = stub.SaveUser.OnCall((user) => { }).Verifiable();
```
<!-- endSnippet -->

---

## Key Takeaways

- **Configuration options**: Use `OnCall(callback)` for dynamic values or `Returns(value)` for fixed return values
- **OnCall signature**: Callback matches method signature—receives only the method parameters
- **Verification patterns**: Individual tracking with `tracking.Verify(Times)` or batch verification with `.Verifiable()` then `stub.Verify()`
- **Times options**: `Once`, `Never`, `AtLeastOnce`, `Exactly(n)`
- **Argument capture**: `LastArg` for single parameters, `LastArgs` tuple for multiple
- **Overloads**: Configure using fully-typed lambda to distinguish which overload
- **Sequences**: Use `Returns(1, 2, 3)` for constant value sequences (NSubstitute-style); use `ThenCall()` chaining for callback sequences
- **Async auto-wrapping**: Async methods auto-wrap params values - no `Task.FromResult` needed
- **ThenDefault()**: Opt-in to returning `default(T)` after sequence exhaustion instead of repeating
- **Reset**: Clears call count, captured arguments, and removes callbacks

Next: [Property Interceptors](properties.md) for get/set tracking and configuration.

**See also:**
- [Parameter Matching Guide](parameter-matching.md) - Use `When()` to match specific argument values

---

**UPDATED:** 2026-02-03
