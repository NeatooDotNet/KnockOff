# Delegate Stubs

Delegate stubs allow you to test code that accepts delegates as parameters. Use the inline pattern `[KnockOff<TDelegate>]` to generate a delegate stub that tracks invocations and configures behavior.

## When to Use Delegate Stubs

Use delegate stubs when testing:
- **Validation rules** - Predicate callbacks that validate business logic
- **Factory functions** - Delegates that construct objects on demand
- **Event handlers** - Callbacks triggered by domain events
- **Filters and transformations** - `Func<T, TResult>` passed to query or processing logic

Delegate stubs track every invocation, capture arguments, and allow you to configure return values through callbacks.

## Basic Usage

Define a delegate type, apply `[KnockOff<TDelegate>]` to your test class, and use the generated stub.

### Void Delegate with No Parameters

<!-- snippet: delegate-stub-basic-void -->
```cs
// Create stub, convert to delegate, invoke, and verify
var stub = new BasicVoidDelegateTest.Stubs.OnComplete();
OnComplete callback = stub;
callback();
stub.Interceptor.Verify();
```
<!-- endSnippet -->

### Delegate with Return Value

<!-- snippet: delegate-stub-with-return -->
```cs
// Default return value is null; LastCallArg tracks the argument
Assert.Null(result);
Assert.Equal("hello", stub.Interceptor.LastCallArg);
```
<!-- endSnippet -->

### Delegate with Multiple Parameters

<!-- snippet: delegate-stub-multi-param -->
```cs
// Access arguments via named tuple
Assert.Equal("Alice", stub.Interceptor.LastCallArgs!.Value.name);
Assert.Equal(30, stub.Interceptor.LastCallArgs!.Value.age);
```
<!-- endSnippet -->

## Configuring Callbacks

Use `stub.Interceptor.OnCall(...)` to configure custom behavior when the delegate is invoked.

**Important:** Each call to `OnCall` replaces the previous configuration. The most recent `OnCall` wins.

### Configuration Methods

Delegates with return values support two configuration methods:

| Method | Signature | Use Case | Example |
|--------|-----------|----------|---------|
| **Returns** | `Returns(TReturn value)` | Return fixed value regardless of input | `stub.Interceptor.Returns("SUCCESS")` |
| **OnCall** | `OnCall(Func<...> callback)` | Compute return value from input | `stub.Interceptor.OnCall((x) => x * 2)` |

Void delegates (`Action`, `Action<T>`) support only the callback method:

| Overload | Signature | Use Case | Example |
|----------|-----------|----------|---------|
| **Callback** | `OnCall(Action callback)` | Execute side effects | `stub.Interceptor.OnCall(() => counter++)` |

### OnCall for Void Delegates

<!-- snippet: delegate-stub-oncall-void -->
```cs
// Configure side effects for void delegate
stub.Interceptor.OnCall(() => notified = true);
```
<!-- endSnippet -->

### Returns with Fixed Return Value

For delegates that return values, configure a fixed return value using `Returns()`. The value is returned regardless of input arguments.

<!-- snippet: delegate-stub-oncall-value -->
```cs
// Returns() - pass the return value directly (simpler syntax)
stub.Interceptor.Returns("FORMATTED");
```
<!-- endSnippet -->

**Use `Returns(value)` when:**
- Return value is constant across all invocations
- You need simpler test setup
- Input arguments don't affect the result

**Signature:** `Returns(TReturn value)` where `TReturn` is the delegate's return type.

### OnCall with Computed Return Value

Use the callback overload to compute the return value based on input arguments. The callback receives the same parameters as the delegate and returns the result.

<!-- snippet: delegate-stub-oncall-return -->
```cs
// OnCall() - compute return value based on input
stub.Interceptor.OnCall((input) => input.ToUpperInvariant());
```
<!-- endSnippet -->

**Use the callback overload when:**
- Return value depends on input arguments
- You need conditional logic or computation
- You need to capture or transform input

**Signature:** `OnCall(Func<TArg1, ..., TReturn> callback)` matching the delegate signature.

### OnCall with Multiple Parameters

<!-- snippet: delegate-stub-oncall-multi-param -->
```cs
// Configure with multiple parameters
stub.Interceptor.OnCall((name, age) => $"{name} is {age} years old");
```
<!-- endSnippet -->

## Verification

Verify delegate invocations using `stub.Interceptor.Verify()` and `Times` constraints.

### Basic Verification

<!-- snippet: delegate-stub-verification-basic -->
```cs
// Verify() passes - delegate was called at least once
stub.Interceptor.Verify();
```
<!-- endSnippet -->

### Verification with Times

<!-- snippet: delegate-stub-verification-times -->
```cs
// Verify with Times constraints
stub.Interceptor.Verify(Times.Exactly(3));
stub.Interceptor.Verify(Times.AtLeast(2));
stub.Interceptor.Verify(Times.AtMost(5));
```
<!-- endSnippet -->

### Verifiable Pattern

<!-- snippet: delegate-stub-verifiable -->
```cs
// Delegate interceptors use Verify() directly (no Verifiable() chaining)
stub.Interceptor.Verify();
```
<!-- endSnippet -->

## Tracking Invocations

Delegate stubs track every invocation, providing access to the last call's arguments.

### Single Parameter Tracking

<!-- snippet: delegate-stub-lastcallarg -->
```cs
// LastCallArg captures the most recent argument
Assert.Equal("second", stub.Interceptor.LastCallArg);
```
<!-- endSnippet -->

### Multiple Parameter Tracking

<!-- snippet: delegate-stub-lastcallargs -->
```cs
// LastCallArgs provides named tuple access
Assert.Equal("Bob", stub.Interceptor.LastCallArgs!.Value.name);
Assert.Equal(25, stub.Interceptor.LastCallArgs!.Value.age);
```
<!-- endSnippet -->

### Call Count

<!-- snippet: delegate-stub-callcount -->
```cs
// Verify invocation count using Times constraints
stub.Interceptor.Verify(Times.Exactly(3));
```
<!-- endSnippet -->

## Open Generic Delegates

KnockOff supports closed generic delegates using standard generic attribute syntax and open generic delegates using `typeof()`.

> **NOTE:** Open generic delegate stubs use the Open Generic pattern (`[KnockOff(typeof(Delegate<>))]`). For details on when to choose this pattern versus defining a Generic Standalone stub, see [Stub Patterns - Open Generic](stub-patterns.md#open-generic-pattern).

### Closed Generic Delegates

<!-- snippet: delegate-stub-closed-generic -->
```cs
// Closed generic: type arguments specified at stub definition
var stub = new DelegateStubTests.Stubs.Factory();
stub.Interceptor.OnCall(() => "generated value");
Factory<string> factory = stub;
```
<!-- endSnippet -->

### Open Generic Delegates

<!-- snippet: delegate-stub-open-generic -->
```cs
// Open generic: create stub with any type argument
var stringFactory = new OpenGenericDelegateTest.Stubs.Factory<string>();
stringFactory.Interceptor.OnCall(() => "hello");

var intFactory = new OpenGenericDelegateTest.Stubs.Factory<int>();
intFactory.Interceptor.OnCall(() => 42);
```
<!-- endSnippet -->

### Type Constraints Preserved

<!-- snippet: delegate-stub-generic-constraints -->
```cs
// ConstrainedFactory<T> requires T : new() - compiler enforces this
var productFactory = new OpenGenericDelegateTest.Stubs.ConstrainedFactory<Product>();
productFactory.Interceptor.OnCall(() => new Product { Id = 1, Name = "Widget" });
```
<!-- endSnippet -->

## Reset Behavior

Use `stub.Interceptor.Reset()` to clear tracking state while preserving configuration.

### Reset Clears Tracking

<!-- snippet: delegate-stub-reset -->
```cs
// Reset clears tracking state but preserves configuration
stub.Interceptor.Reset();

stub.Interceptor.Verify(Times.Never);
Assert.Null(stub.Interceptor.LastCallArg);
Assert.Equal("TEST", format("test")); // OnCall still works
```
<!-- endSnippet -->

## Implicit Conversion

Delegate stubs implicitly convert to the delegate type, allowing seamless substitution.

### Direct Assignment

<!-- snippet: delegate-stub-implicit-conversion -->
```cs
// Implicit conversion - no cast required
Formatter format = stub;
var result = format("hello");
```
<!-- endSnippet -->

### Method Parameters

<!-- snippet: delegate-stub-method-parameter -->
```cs
// Pass stub directly to method expecting Formatter
var result = ProcessWithFormatter(stub);
```
<!-- endSnippet -->

## Real-World Examples

### Validation Rule Stub

<!-- snippet: delegate-stub-validation-rule -->
```cs
// Configure validation: "admin" is taken, others are available
stub.Interceptor.OnCall((value) => value != "admin");
```
<!-- endSnippet -->

### Factory Function Stub

<!-- snippet: delegate-stub-factory -->
```cs
// Configure factory to return test instance
stub.Interceptor.OnCall(() => testProduct);
Factory<Product> factory = stub;
```
<!-- endSnippet -->

### Event Callback Stub

<!-- snippet: delegate-stub-event-callback -->
```cs
// Track received events
stub.Interceptor.OnCall((evt) => receivedEvent = evt);
```
<!-- endSnippet -->

## Complete Example

This example demonstrates delegate stubs in a realistic validation scenario.

<!-- snippet: delegate-stub-complete-example -->
```cs
// Configure format rule: must be at least 3 characters
formatStub.Interceptor.OnCall((value) => value.Length >= 3);

// Configure uniqueness rule: "admin" and "root" are taken
uniqueStub.Interceptor.OnCall((value) => value != "admin" && value != "root");

// Create validator with stubbed rules
var validator = new UsernameValidator(uniqueStub, formatStub);
```
<!-- endSnippet -->

---

## Next Steps

- **[Stub Patterns](stub-patterns.md)** - Learn about Stand-Alone, Inline Interface, and Inline Class patterns
- **[Methods Guide](methods.md)** - Configure method behavior with OnCall
- **[Interceptor API Reference](../reference/interceptor-api.md)** - Complete API documentation

---

**UPDATED:** 2026-01-25
