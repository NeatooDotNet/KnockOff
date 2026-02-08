[Home](../../README.md) / [Guides](../guides/) / Methods

# Method Interceptors

Method interceptors track calls, capture arguments, and configure return values for interface methods in your stub. Each method on the stubbed interface gets a corresponding interceptor property that provides verification and configuration capabilities.

**Key concept**: The `Returns`/`Execute` callback receives only the method's parameters—you configure behavior based on the inputs to the method being called.

---

## Configuring Method Behavior

### Void Methods

Configure void methods using `Execute` with an `Action` that matches the method parameters:

<!-- snippet: methods-oncall-void -->
```cs
// Return for void methods uses Action<...params>
stub.LogMessage.Call((message) => logged.Add(message));
```
<!-- endSnippet -->

### Methods with Return Values

Configure methods that return values using `Returns`. You have two options:

**1. Callback syntax** - Use a `Func` for dynamic values or conditional logic:

<!-- snippet: methods-oncall-return -->
```cs
// Return with return value: Func<...params, TReturn>
stub.GetUserName.Return((userId) => "TestUser");
```
<!-- endSnippet -->

**2. Value syntax** - Pass the return value directly for fixed results:

<!-- snippet: methods-oncall-value -->
```cs
// Returns - simpler syntax when you don't need callback logic
stub.GetUserName.Return("StaticUser");
```
<!-- endSnippet -->

Choose the value syntax when returning a constant, or the callback syntax when you need to inspect parameters or apply logic.

### Methods with Multiple Parameters

The callback signature includes all method parameters in the same order:

<!-- snippet: methods-oncall-multi-param -->
```cs
// All method parameters are passed to the callback in order
stub.ValidateCredentials.Return((username, password) =>
    username == "admin" && password == "secret");
```
<!-- endSnippet -->

---

## Verifying Method Calls

KnockOff provides two verification patterns:
1. **Individual tracking**: Store the object returned by `Returns`/`Execute` and call `.Verify()` on it
2. **Batch verification**: Mark interceptors with `.Verifiable()` then call `stub.Verify()` once

The builder object returned by `Returns`/`Execute` provides access to `Verify()`, `Verifiable()`, and sequencing methods.

### Using Verify()

Call `.Verify()` on the builder object returned by `Returns`/`Execute` to verify that specific method was called:

<!-- snippet: methods-verify-wascalled -->
```cs
// Mark with Verifiable(), then stub.Verify() checks all marked members
stub.Save.Call((entity) => { }).Verifiable();
```
<!-- endSnippet -->

### Verifying Call Frequency

Use `Called` to specify exact call count requirements. Available options include `Once`, `Never`, `AtLeastOnce`, and `Exactly(n)`:

<!-- snippet: methods-verify-callcount -->
```cs
// Verify exact call count (throws if different)
tracking.Verify(Called.Exactly(2));
```
<!-- endSnippet -->

### Using Verifiable()

For batch verification of multiple methods, mark each with `.Verifiable()` then call `stub.Verify()` once to check all:

<!-- snippet: methods-verify-verifiable -->
```cs
// Mark expected calls with Verifiable(), then stub.Verify() checks all
stub.Save.Call((entity) => { }).Verifiable(Called.Once);
stub.GetById.Return((id) => new User { Id = id }).Verifiable();
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

When an interface has overloaded methods, KnockOff generates a single interceptor with overloaded `Return()`/`Call()` methods. The lambda's parameter types disambiguate which overload to configure:

<!-- snippet: methods-overloads -->
```cs
// Fully-typed lambda tells KnockOff which overload to configure
stub.Find.Return(() => new List<User>());
stub.Find.Return((int id) => new User { Id = id, Name = "ById" });
stub.Find.Return((string name) => new User { Id = 1, Name = name });
```
<!-- endSnippet -->

Each overload gets its own `Return`/`Call` overload on the same interceptor property, distinguished by the delegate signature.

**Stand-Alone pattern with user methods:** Each overload gets its own virtual method in the generated base class. You can selectively override specific overloads without affecting others. See the [User Methods: Overloads](user-methods.md#overloads) section for details.

---

## Resetting Interceptors

Clear tracking state and remove callbacks using `Reset()`:

<!-- snippet: methods-reset -->
```cs
// Reset clears call count and captured arguments, but preserves callbacks
stub.ProcessData.Reset();
```
<!-- endSnippet -->

This is useful when reusing a stub instance across multiple test phases or assertions.

---

## Sequences

Use sequences when a method should behave differently across multiple calls. KnockOff provides two approaches:

1. **Params syntax** (recommended for constant values) - `Return(first, params rest)` creates a sequence in a single call
2. **Callback chaining** (for dynamic values) - Chain `ThenReturn()` or `ThenCall()` after `Return(callback)` or `Call(callback)`

### Concise Value Sequences (Params Syntax)

For constant value sequences, use the concise params syntax:

<!-- snippet: methods-sequence-params -->
```cs
// Returns(first, params rest) for value sequences
stub.GetValue.Return(1, 2, 3);
```
<!-- endSnippet -->

This matches NSubstitute's `Returns(x, y, z)` syntax for easy migration. The sequence repeats the last value after exhaustion.

### Async Methods with Params

Async methods auto-wrap values - no `Task.FromResult` needed:

<!-- snippet: methods-sequence-params-async -->
```cs
// Async methods auto-wrap values - no Task.FromResult needed
stub.GetDataAsync.Return("first", "second", "third");
```
<!-- endSnippet -->

### Mixing Callbacks with Value Params

Use `Return(callback)` for the first callback, then `ThenReturn()` with params for subsequent values:

<!-- snippet: methods-sequence-callback-then-params -->
```cs
// Return for first callback, then ThenReturn for constant values
stub.Calculate
    .Return((x, y) => x + y)
    .ThenReturn(100, 200, 300);
```
<!-- endSnippet -->

### Callback Sequences

For callback sequences or mixed sequences with dynamic values, chain `ThenReturn()` after `Return(callback)`:

<!-- snippet: methods-sequence-basic -->
```cs
// Chain ThenCall() for callback sequences
stub.GetStatus
    .Return(() => "Pending")
    .ThenReturn(() => "Processing")
    .ThenReturn(() => "Complete");
```
<!-- endSnippet -->

Each callback in the sequence is invoked exactly once in order.

### Void Method Sequences

Sequences work with void methods using `Action` callbacks:

<!-- snippet: methods-sequence-void -->
```cs
// Void method sequences use Action callbacks
stub.Notify
    .Call((msg) => calls.Add("first"))
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
    .Return((x, y) => x + y)
    .ThenReturn((x, y) => x * y)
    .ThenReturn((x, y) => x - y);
```
<!-- endSnippet -->

The callback signature matches the method signature, just like `Return(callback)` and `Call(callback)`.

### Sequence Exhaustion

After the sequence is exhausted (all callbacks consumed), subsequent calls **repeat the last value** by default. This matches NSubstitute's behavior for easier migration and more forgiving tests.

<!-- snippet: methods-sequence-exhaustion -->
```cs
// After exhaustion: repeats last value (NSubstitute behavior)
stub.GetValue.Return(() => 1).ThenReturn(() => 2).ThenReturn(() => 3);
```
<!-- endSnippet -->

### Returning Default After Exhaustion

Use `ThenDefault()` when you want the sequence to return `default(T)` after exhaustion instead of repeating the last value:

<!-- snippet: methods-sequence-then-default -->
```cs
// ThenDefault() returns default(T) after exhaustion instead of repeating
stub.GetValue.Return(() => 1).ThenReturn(() => 2).ThenDefault();
```
<!-- endSnippet -->

### Strict Mode Sequence Exhaustion

In strict mode, exhausted sequences throw `StubException.SequenceExhausted` regardless of `ThenDefault()`:

<!-- snippet: methods-sequence-strict -->
```cs
// Strict mode throws on sequence exhaustion
stub.Strict = true;
stub.GetValue.Return(() => 1).ThenReturn(() => 2);
```
<!-- endSnippet -->

### Mixing Fixed Values and Dynamic Callbacks

You can mix fixed values and dynamic callbacks in the same sequence using `Return(callback)`:

<!-- snippet: methods-sequence-mixed -->
```cs
// Mix fixed values with dynamic callbacks
stub.GetStatus
    .Return(() => "Initial")
    .ThenReturn(() => DateTime.Now.ToString("HH:mm:ss"))
    .ThenReturn(() => "Final");
```
<!-- endSnippet -->

**Note:** Use `Return(() => value)` to include fixed values in a sequence chain.

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
    .Call(() => { })
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
var getTracking = stub.GetUser.Return((id) => id == 1 ? testUser : null).Verifiable();
var saveTracking = stub.SaveUser.Call((user) => { }).Verifiable();
```
<!-- endSnippet -->

---

## Key Takeaways

- **Configuration options**: Use `Return(callback)` for dynamic return values, `Call(callback)` for void methods, or `Return(value)` for fixed return values
- **Callback signature**: Callback matches method signature—receives only the method parameters
- **Verification patterns**: Individual tracking with `tracking.Verify(Called)` or batch verification with `.Verifiable()` then `stub.Verify()`
- **Called options**: `Once`, `Never`, `AtLeastOnce`, `Exactly(n)`
- **Argument capture**: `LastArg` for single parameters, `LastArgs` tuple for multiple
- **Overloads**: Configure using fully-typed lambda to distinguish which overload
- **Sequences**: Use `Return(1, 2, 3)` for constant value sequences (NSubstitute-style); use `ThenReturn()`/`ThenCall()` chaining for callback sequences
- **Async auto-wrapping**: Async methods auto-wrap params values - no `Task.FromResult` needed
- **ThenDefault()**: Opt-in to returning `default(T)` after sequence exhaustion instead of repeating
- **Reset**: Clears call count, captured arguments, and sequence position, but preserves callbacks

Next: [Property Interceptors](properties.md) for get/set tracking and configuration.

**See also:**
- [Parameter Matching Guide](parameter-matching.md) - Use `When()` to match specific argument values

---

**UPDATED:** 2026-02-03
