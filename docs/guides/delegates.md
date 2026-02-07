# Delegate Stubs

Delegate stubs allow you to test code that accepts delegates as parameters. Use the inline pattern `[KnockOff<TDelegate>]` to generate a delegate stub that tracks invocations and configures behavior.

**Important:** Only named delegate types are supported. `Func<>` and `Action<>` cannot be stubbed directly — define a named delegate instead:

<!-- snippet: delegate-func-action-not-supported -->
```cs
// Does NOT work:
// [KnockOff<Func<int, int, int>>]  // Not supported

// Define a named delegate instead:
public delegate int NamedCalculation(int a, int b);

[KnockOff<NamedCalculation>]  // Works!
public partial class NamedDelegateExample { }
```
<!-- endSnippet -->

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
// Default return value is null; LastArg tracks the argument
Assert.Null(result);
Assert.Equal("hello", stub.Interceptor.LastArg);
```
<!-- endSnippet -->

### Delegate with Multiple Parameters

<!-- snippet: delegate-stub-multi-param -->
```cs
// Access arguments via named tuple
Assert.Equal("Alice", stub.Interceptor.LastArgs!.Value.name);
Assert.Equal(30, stub.Interceptor.LastArgs!.Value.age);
```
<!-- endSnippet -->

## Configuring Callbacks

Use `stub.Interceptor.Returns(...)` (non-void delegates) or `stub.Interceptor.Execute(...)` (void delegates) to configure custom behavior when the delegate is invoked.

**Important:** Each call to `Returns`/`Execute` replaces the previous configuration. The most recent configuration wins.

### Configuration Methods

Delegates with return values support two configuration methods:

| Method | Signature | Use Case | Example |
|--------|-----------|----------|---------|
| **Returns** | `Returns(TReturn value)` | Return fixed value regardless of input | `stub.Interceptor.Returns("SUCCESS")` |
| **Returns** | `Returns(Func<...> callback)` | Compute return value from input | `stub.Interceptor.Returns((x) => x * 2)` |

Void delegates (`Action`, `Action<T>`) support only the callback method:

| Overload | Signature | Use Case | Example |
|----------|-----------|----------|---------|
| **Execute** | `Execute(Action callback)` | Execute side effects | `stub.Interceptor.Execute(() => counter++)` |

### Execute for Void Delegates

<!-- snippet: delegate-stub-oncall-void -->
```cs
// Configure side effects for void delegate
stub.Interceptor.Execute(() => notified = true);
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

### Returns with Computed Return Value

Use the callback overload to compute the return value based on input arguments. The callback receives the same parameters as the delegate and returns the result.

<!-- snippet: delegate-stub-oncall-return -->
```cs
// OnCall() - compute return value based on input
stub.Interceptor.Returns((input) => input.ToUpperInvariant());
```
<!-- endSnippet -->

**Use the callback overload when:**
- Return value depends on input arguments
- You need conditional logic or computation
- You need to capture or transform input

**Signature:** `Returns(Func<TArg1, ..., TReturn> callback)` matching the delegate signature.

### Returns with Multiple Parameters

<!-- snippet: delegate-stub-oncall-multi-param -->
```cs
// Configure with multiple parameters
stub.Interceptor.Returns((name, age) => $"{name} is {age} years old");
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

Delegate stubs support `.Verifiable()` chaining on `Returns()` and `Execute()`, just like interface and class stubs:

<!-- snippet: delegate-verifiable-pattern -->
```cs
// Mark for verification with Verifiable() chaining
stub.Interceptor.Returns((x) => x * 2).Verifiable();
stub.Interceptor.Verify(Times.Never); // Not called yet

Transform transform = stub;
var result = transform(21);

// Verify the delegate was called
stub.Interceptor.Verify(Times.Once);
```
<!-- endSnippet -->

## Tracking Invocations

Delegate stubs track every invocation, providing access to the last call's arguments.

### Single Parameter Tracking

<!-- snippet: delegate-stub-lastcallarg -->
```cs
// LastArg captures the most recent argument
Assert.Equal("second", stub.Interceptor.LastArg);
```
<!-- endSnippet -->

### Multiple Parameter Tracking

<!-- snippet: delegate-stub-lastcallargs -->
```cs
// LastArgs provides named tuple access
Assert.Equal("Bob", stub.Interceptor.LastArgs!.Value.name);
Assert.Equal(25, stub.Interceptor.LastArgs!.Value.age);
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
stub.Interceptor.Returns(() => "generated value");
Factory<string> factory = stub;
```
<!-- endSnippet -->

### Open Generic Delegates

<!-- snippet: delegate-stub-open-generic -->
```cs
// Open generic: create stub with any type argument
var stringFactory = new OpenGenericDelegateTest.Stubs.Factory<string>();
stringFactory.Interceptor.Returns(() => "hello");

var intFactory = new OpenGenericDelegateTest.Stubs.Factory<int>();
intFactory.Interceptor.Returns(() => 42);
```
<!-- endSnippet -->

### Type Constraints Preserved

<!-- snippet: delegate-stub-generic-constraints -->
```cs
// ConstrainedFactory<T> requires T : new() - compiler enforces this
var productFactory = new OpenGenericDelegateTest.Stubs.ConstrainedFactory<Product>();
productFactory.Interceptor.Returns(() => new Product { Id = 1, Name = "Widget" });
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
Assert.Null(stub.Interceptor.LastArg);
Assert.Equal("TEST", format("test")); // OnCall still works
```
<!-- endSnippet -->

## Sequences

Delegate stubs support the same sequence API as interface and class stubs.

### Returns Sequences

<!-- snippet: delegate-sequences -->
```cs
// Return different values on successive calls
stub.Interceptor.Returns(10, 20, 30);
// Call 1: 10, Call 2: 20, Call 3+: 30 (repeats last)
```
<!-- endSnippet -->

### Callback Sequences

<!-- snippet: delegate-sequences-callback -->
```cs
// Callback sequences
stub.Interceptor
    .Returns((x) => x * 1)
    .ThenReturns((x) => x * 2)
    .ThenReturns((x) => x * 3);
```
<!-- endSnippet -->

### ThenReturns

<!-- snippet: delegate-sequences-thenreturns -->
```cs
// ThenReturns for fixed values after callback
stub.Interceptor
    .Returns((x) => x)
    .ThenReturns(99);
```
<!-- endSnippet -->

### ThenDefault

<!-- snippet: delegate-sequences-thendefault -->
```cs
// ThenDefault: return default(T) after exhaustion instead of repeating
stub.Interceptor
    .Returns((a, b) => 100)
    .ThenReturns((a, b) => 200)
    .ThenDefault();
// Call 1: 100, Call 2: 200, Call 3+: 0 (default(int))
```
<!-- endSnippet -->

## Async Delegate Auto-Wrapping

Async delegates (e.g., `delegate Task<int> AsyncOp(int x)`) support the same three-tier auto-wrapping as interface and class stubs:

<!-- snippet: delegate-async-auto-wrapping -->
```cs
// Tier 1: Returns takes inner type - auto-wraps in Task.FromResult
stub.Interceptor.Returns(42);
```
<!-- endSnippet -->

See [Async Patterns](async-patterns.md) for more details.

## When Chains (Parameter Matching)

Delegate interceptors support conditional parameter matching via `When()`, identical to interface and class stubs. Requires at least one parameter.

### Value Matching

<!-- snippet: delegate-when-value-matching -->
```cs
// Match specific argument values
stub.Interceptor.When(1, 2).Returns(100)
    .ThenWhen(3, 4).Returns(200)
    .ThenCall((a, b) => a + b);  // terminal fallback
```
<!-- endSnippet -->

### Predicate Matching

<!-- snippet: delegate-when-predicate-matching -->
```cs
// Match via predicate
stub.Interceptor.When((a, b) => a > 10).Returns(999);
```
<!-- endSnippet -->

<!-- snippet: delegate-when-predicate-single-param -->
```cs
// Single-parameter delegate
stub.Interceptor.When(s => s.Length > 5).Returns("LONG");
```
<!-- endSnippet -->

### Chained When

<!-- snippet: delegate-when-chained -->
```cs
stub.Interceptor
    .When("one").Returns("ONE")
    .ThenWhen("two").Returns("TWO")
    .ThenWhen(s => s.StartsWith("x")).Returns("X_PREFIX");
```
<!-- endSnippet -->

### Void Delegate When Chains

Void delegates use `.Execute()` instead of `.Returns()`:

<!-- snippet: delegate-when-void-chains -->
```cs
stub.Interceptor
    .When(1, 2).Execute((a, b) => calls.Add("first"))
    .ThenWhen(3, 4).Execute((a, b) => calls.Add("second"));
```
<!-- endSnippet -->

### ThenNone (Exhaust Matching)

<!-- snippet: delegate-when-thennone -->
```cs
// After "one" is matched, subsequent calls fall through to default behavior
stub.Interceptor.When("one").Returns("ONE").ThenNone();
```
<!-- endSnippet -->

See [Parameter Matching Guide](parameter-matching.md) for more details.

## Strict Mode

Delegate stubs have a `Strict` property. When `true`, unconfigured invocations throw `StubException.NotConfigured` instead of returning `default(T)`.

<!-- snippet: delegate-strict-mode -->
```cs
var stub = new DelegateStubTests.Stubs.Calculate();
stub.Strict = true;

Calculate calc = stub;
Assert.Throws<StubException>(() => calc(1, 2)); // Throws StubException.NotConfigured
```
<!-- endSnippet -->

In strict mode, exhausted sequences throw `StubException.SequenceExhausted`:

<!-- snippet: delegate-strict-mode-sequences -->
```cs
stub.Strict = true;
stub.Interceptor.Returns(10, 20);

Calculate op = stub;
Assert.Equal(10, op(0, 0)); // first value
Assert.Equal(20, op(0, 0)); // second value
Assert.Throws<StubException>(() => op(0, 0)); // Throws StubException.SequenceExhausted
```
<!-- endSnippet -->

## Configuration Mutual Exclusivity

`Returns()` and `Execute()` are mutually exclusive. Configuring one clears the other:

<!-- snippet: delegate-config-mutual-exclusivity -->
```cs
stub.Interceptor.Returns(42);
stub.Interceptor.Returns((a, b) => a + b); // Clears Returns(42)
```
<!-- endSnippet -->

## Priority Resolution Order

When a delegate is invoked, KnockOff checks configurations in this priority order:

1. **When chains** (highest) — parameter-specific matching
2. **Sequences** — `Returns().ThenReturns()` / `Execute().ThenExecute()` sequence callbacks
3. **Returns value** — `Returns(value)` repeating constant
4. **Returns callback** — `Returns(delegate)` repeating callback
5. **Simplified callback** — `Returns(simplified)` for async delegates
6. **Strict mode check** — throws `StubException.NotConfigured` if strict
7. **Smart default** — `default(T)` for value types, `null` for reference types

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
stub.Interceptor.Returns((value) => value != "admin");
```
<!-- endSnippet -->

### Factory Function Stub

<!-- snippet: delegate-stub-factory -->
```cs
// Configure factory to return test instance
stub.Interceptor.Returns(() => testProduct);
Factory<Product> factory = stub;
```
<!-- endSnippet -->

### Event Callback Stub

<!-- snippet: delegate-stub-event-callback -->
```cs
// Track received events
stub.Interceptor.Execute((evt) => receivedEvent = evt);
```
<!-- endSnippet -->

## Complete Example

This example demonstrates delegate stubs in a realistic validation scenario.

<!-- snippet: delegate-stub-complete-example -->
```cs
// Configure format rule: must be at least 3 characters
formatStub.Interceptor.Returns((value) => value.Length >= 3);

// Configure uniqueness rule: "admin" and "root" are taken
uniqueStub.Interceptor.Returns((value) => value != "admin" && value != "root");

// Create validator with stubbed rules
var validator = new UsernameValidator(uniqueStub, formatStub);
```
<!-- endSnippet -->

---

## Next Steps

- **[Stub Patterns](stub-patterns.md)** - Learn about all nine patterns including Inline Delegate
- **[Methods Guide](methods.md)** - Configure method behavior with Returns/Execute
- **[Parameter Matching](parameter-matching.md)** - When chains and conditional behavior
- **[Async Patterns](async-patterns.md)** - Async auto-wrapping details
- **[Verification Guide](verification.md)** - Verify delegate invocations
- **[Interceptor API Reference](../reference/interceptor-api.md)** - Complete API documentation

---

**UPDATED:** 2026-02-05
