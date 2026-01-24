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
[Fact]
public void BasicVoid_DelegateStub()
{
    // Create the delegate stub
    var stub = new BasicVoidDelegateTest.Stubs.OnComplete();

    // Convert to delegate and invoke
    OnComplete callback = stub;
    callback();

    // Verify the delegate was called
    stub.Interceptor.Verify();
}
```
<!-- endSnippet -->

### Delegate with Return Value

<!-- snippet: delegate-stub-with-return -->
```cs
[Fact]
public void WithReturn_TracksArgAndReturnsDefault()
{
    // Create a stub for a delegate with return value
    var stub = new DelegateStubTests.Stubs.Formatter();

    // Invoke through the delegate
    Formatter format = stub;
    var result = format("hello");

    // Default return value is null for reference types
    Assert.Null(result);

    // Track last argument
    Assert.Equal("hello", stub.Interceptor.LastCallArg);
}
```
<!-- endSnippet -->

### Delegate with Multiple Parameters

<!-- snippet: delegate-stub-multi-param -->
```cs
[Fact]
public void MultiParam_TracksLastCallArgs()
{
    // Create a stub for a multi-parameter delegate
    var stub = new DelegateStubTests.Stubs.MessageBuilder();

    // Invoke through the delegate
    MessageBuilder builder = stub;
    builder("Alice", 30);

    // Access arguments via named tuple
    Assert.NotNull(stub.Interceptor.LastCallArgs);
    Assert.Equal("Alice", stub.Interceptor.LastCallArgs.Value.name);
    Assert.Equal(30, stub.Interceptor.LastCallArgs.Value.age);
}
```
<!-- endSnippet -->

## Configuring Callbacks

Use `stub.Interceptor.OnCall(...)` to configure custom behavior when the delegate is invoked.

### OnCall for Void Delegates

<!-- snippet: delegate-stub-oncall-void -->
```cs
[Fact]
public void OnCallVoid_ExecutesCustomLogic()
{
    var stub = new DelegateStubTests.Stubs.NotifyCallback();

    // Configure side effects for void delegate
    var notified = false;
    stub.Interceptor.OnCall(() => notified = true);

    // Invoke through the delegate
    NotifyCallback callback = stub;
    callback();

    // Verify side effect occurred
    Assert.True(notified);
}
```
<!-- endSnippet -->

### OnCall for Return Delegates

<!-- snippet: delegate-stub-oncall-return -->
```cs
[Fact]
public void OnCallReturn_ReturnsComputedValue()
{
    var stub = new DelegateStubTests.Stubs.Formatter();

    // Configure to return computed value based on input
    stub.Interceptor.OnCall((input) => input.ToUpperInvariant());

    // Invoke through the delegate
    Formatter format = stub;
    var result = format("hello");

    // Verify computed return value
    Assert.Equal("HELLO", result);
}
```
<!-- endSnippet -->

### OnCall with Multiple Parameters

<!-- snippet: delegate-stub-oncall-multi-param -->
```cs
[Fact]
public void OnCallMultiParam_ComputesFromAllParams()
{
    var stub = new DelegateStubTests.Stubs.MessageBuilder();

    // Configure with multiple parameters
    stub.Interceptor.OnCall((name, age) => $"{name} is {age} years old");

    // Invoke through the delegate
    MessageBuilder builder = stub;
    var result = builder("Bob", 25);

    // Verify computed result
    Assert.Equal("Bob is 25 years old", result);
}
```
<!-- endSnippet -->

## Verification

Verify delegate invocations using `stub.Interceptor.Verify()` and `Times` constraints.

### Basic Verification

<!-- snippet: delegate-stub-verification-basic -->
```cs
[Fact]
public void Verify_ThrowsIfNeverCalled()
{
    var stub = new DelegateStubTests.Stubs.NotifyCallback();
    stub.Interceptor.OnCall(() => { });

    // Invoke through the delegate
    NotifyCallback callback = stub;
    callback();

    // Verify() passes - delegate was called at least once
    stub.Interceptor.Verify();
}
```
<!-- endSnippet -->

### Verification with Times

<!-- snippet: delegate-stub-verification-times -->
```cs
[Fact]
public void Verify_WithTimesConstraints()
{
    var stub = new DelegateStubTests.Stubs.NotifyCallback();
    stub.Interceptor.OnCall(() => { });

    NotifyCallback callback = stub;

    // Call exactly 3 times
    callback();
    callback();
    callback();

    // Verify with Times constraints
    stub.Interceptor.Verify(Times.Exactly(3));
    stub.Interceptor.Verify(Times.AtLeast(2));
    stub.Interceptor.Verify(Times.AtMost(5));
}
```
<!-- endSnippet -->

### Verifiable Pattern

<!-- snippet: delegate-stub-verifiable -->
```cs
[Fact]
public void Verifiable_VerifyAfterOnCall()
{
    var stub = new DelegateStubTests.Stubs.Formatter();

    // Configure the delegate behavior
    stub.Interceptor.OnCall((input) => input.ToUpperInvariant());

    Formatter format = stub;
    format("test");

    // Verify delegate was called
    // Note: Delegate interceptors use Verify() directly (no Verifiable() chaining)
    stub.Interceptor.Verify();
}
```
<!-- endSnippet -->

## Tracking Invocations

Delegate stubs track every invocation, providing access to the last call's arguments.

### Single Parameter Tracking

<!-- snippet: delegate-stub-lastcallarg -->
```cs
[Fact]
public void LastCallArg_SingleParameter()
{
    var stub = new DelegateStubTests.Stubs.Formatter();
    stub.Interceptor.OnCall((input) => input);

    Formatter format = stub;
    format("first");
    format("second");

    // LastCallArg captures the most recent argument
    Assert.Equal("second", stub.Interceptor.LastCallArg);
}
```
<!-- endSnippet -->

### Multiple Parameter Tracking

<!-- snippet: delegate-stub-lastcallargs -->
```cs
[Fact]
public void LastCallArgs_MultipleParameters()
{
    var stub = new DelegateStubTests.Stubs.MessageBuilder();
    stub.Interceptor.OnCall((name, age) => $"{name}: {age}");

    MessageBuilder builder = stub;
    builder("Alice", 30);
    builder("Bob", 25);

    // LastCallArgs provides named tuple access
    var args = stub.Interceptor.LastCallArgs;
    Assert.NotNull(args);
    Assert.Equal("Bob", args.Value.name);
    Assert.Equal(25, args.Value.age);
}
```
<!-- endSnippet -->

### Call Count

<!-- snippet: delegate-stub-callcount -->
```cs
[Fact]
public void CallCount_VerifyWithTimes()
{
    var stub = new DelegateStubTests.Stubs.NotifyCallback();
    stub.Interceptor.OnCall(() => { });

    NotifyCallback callback = stub;
    callback();
    callback();
    callback();

    // Verify invocation count using Times constraints
    // Note: Use Verify(Times.Exactly(n)) instead of CallCount property
    stub.Interceptor.Verify(Times.Exactly(3));
}
```
<!-- endSnippet -->

## Open Generic Delegates

KnockOff supports closed generic delegates using standard generic attribute syntax and open generic delegates using `typeof()`.

### Closed Generic Delegates

<!-- snippet: delegate-stub-closed-generic -->
```cs
[Fact]
public void ClosedGeneric_FullySpecifiedTypeArgs()
{
    // Closed generic: type arguments specified at stub definition
    var stub = new DelegateStubTests.Stubs.Factory();
    stub.Interceptor.OnCall(() => "generated value");

    // Use as Factory<string>
    Factory<string> factory = stub;
    var result = factory();

    Assert.Equal("generated value", result);
    stub.Interceptor.Verify();
}
```
<!-- endSnippet -->

### Open Generic Delegates

<!-- snippet: delegate-stub-open-generic -->
```cs
[Fact]
public void OpenGeneric_ReuseWithAnyTypeArg()
{
    // Open generic: create stub with any type argument
    var stringFactory = new OpenGenericDelegateTest.Stubs.Factory<string>();
    stringFactory.Interceptor.OnCall(() => "hello");

    var intFactory = new OpenGenericDelegateTest.Stubs.Factory<int>();
    intFactory.Interceptor.OnCall(() => 42);

    // Each stub instance is independent
    Factory<string> sf = stringFactory;
    Factory<int> intf = intFactory;

    Assert.Equal("hello", sf());
    Assert.Equal(42, intf());
}
```
<!-- endSnippet -->

### Type Constraints Preserved

<!-- snippet: delegate-stub-generic-constraints -->
```cs
[Fact]
public void GenericConstraints_PreservedAtCompileTime()
{
    // ConstrainedFactory<T> requires T : new()
    // Compiler enforces this when creating the stub
    var productFactory = new OpenGenericDelegateTest.Stubs.ConstrainedFactory<Product>();
    productFactory.Interceptor.OnCall(() => new Product { Id = 1, Name = "Widget" });

    ConstrainedFactory<Product> factory = productFactory;
    var product = factory();

    Assert.Equal("Widget", product.Name);

    // This would NOT compile because string has no parameterless constructor:
    // var invalidFactory = new OpenGenericDelegateTest.Stubs.ConstrainedFactory<string>();
}
```
<!-- endSnippet -->

## Reset Behavior

Use `stub.Interceptor.Reset()` to clear tracking state while preserving configuration.

### Reset Clears Tracking

<!-- snippet: delegate-stub-reset -->
```cs
[Fact]
public void Reset_ClearsTrackingPreservesConfiguration()
{
    var stub = new DelegateStubTests.Stubs.Formatter();
    stub.Interceptor.OnCall((input) => input.ToUpperInvariant());

    Formatter format = stub;
    format("hello");
    format("world");

    // Before reset: verify calls were tracked
    stub.Interceptor.Verify(Times.Exactly(2));
    Assert.Equal("world", stub.Interceptor.LastCallArg);

    // Reset clears tracking state
    stub.Interceptor.Reset();

    // After reset: tracking cleared
    stub.Interceptor.Verify(Times.Never);
    Assert.Null(stub.Interceptor.LastCallArg);

    // Configuration preserved: OnCall still works
    var result = format("test");
    Assert.Equal("TEST", result);
}
```
<!-- endSnippet -->

## Implicit Conversion

Delegate stubs implicitly convert to the delegate type, allowing seamless substitution.

### Direct Assignment

<!-- snippet: delegate-stub-implicit-conversion -->
```cs
[Fact]
public void ImplicitConversion_DirectAssignment()
{
    var stub = new DelegateStubTests.Stubs.Formatter();
    stub.Interceptor.OnCall((input) => input.ToUpperInvariant());

    // Implicit conversion - no cast required
    Formatter format = stub;

    // Use the delegate
    var result = format("hello");
    Assert.Equal("HELLO", result);
}
```
<!-- endSnippet -->

### Method Parameters

<!-- snippet: delegate-stub-method-parameter -->
```cs
[Fact]
public void MethodParameter_SeamlessSubstitution()
{
    var stub = new DelegateStubTests.Stubs.Formatter();
    stub.Interceptor.OnCall((input) => $"[{input}]");

    // Pass stub directly to method expecting Formatter
    var result = ProcessWithFormatter(stub);

    Assert.Equal("[test]", result);
    stub.Interceptor.Verify();
}

private static string ProcessWithFormatter(Formatter formatter)
{
    return formatter("test");
}
```
<!-- endSnippet -->

## Real-World Examples

### Validation Rule Stub

<!-- snippet: delegate-stub-validation-rule -->
```cs
[Fact]
public void ValidationRule_StubValidationPredicate()
{
    var stub = new DelegateStubTests.Stubs.IsUniqueRule();

    // Configure validation: "admin" is taken, others are available
    stub.Interceptor.OnCall((value) => value != "admin");

    IsUniqueRule isUnique = stub;

    // Test validation logic
    Assert.False(isUnique("admin"));  // taken
    Assert.True(isUnique("newuser")); // available

    // Verify both checks were performed
    stub.Interceptor.Verify(Times.Exactly(2));
}
```
<!-- endSnippet -->

### Factory Function Stub

<!-- snippet: delegate-stub-factory -->
```cs
[Fact]
public void Factory_StubObjectCreation()
{
    // Use separate test class to avoid naming collision with Factory<string>
    var stub = new ProductFactoryTest.Stubs.Factory();
    var testProduct = new Product { Id = 42, Name = "Test Widget", Price = 9.99m };

    // Configure factory to return test instance
    stub.Interceptor.OnCall(() => testProduct);

    Factory<Product> factory = stub;

    // Test code that uses the factory
    var product = factory();

    Assert.Same(testProduct, product);
    Assert.Equal("Test Widget", product.Name);
    stub.Interceptor.Verify();
}
```
<!-- endSnippet -->

### Event Callback Stub

<!-- snippet: delegate-stub-event-callback -->
```cs
[Fact]
public void EventCallback_VerifyEventRaised()
{
    var stub = new DelegateStubTests.Stubs.EventCallback();

    // Track received events
    DomainEvent? receivedEvent = null;
    stub.Interceptor.OnCall((evt) => receivedEvent = evt);

    EventCallback handler = stub;

    // Simulate event being raised
    var testEvent = new DomainEvent
    {
        EventType = "UserCreated",
        Payload = new { UserId = 1, Name = "Alice" }
    };
    handler(testEvent);

    // Verify callback received correct event
    stub.Interceptor.Verify();
    Assert.NotNull(receivedEvent);
    Assert.Equal("UserCreated", receivedEvent.EventType);
}
```
<!-- endSnippet -->

## Complete Example

This example demonstrates delegate stubs in a realistic validation scenario.

<!-- snippet: delegate-stub-complete-example -->
```cs
[Fact]
public void CompleteExample_ValidationWithMultipleRules()
{
    // Arrange - create delegate stubs
    var uniqueStub = new DelegateStubTests.Stubs.IsUniqueRule();
    var formatStub = new DelegateStubTests.Stubs.IsValidFormatRule();

    // Configure rules:
    // - Format: must be at least 3 characters
    // - Unique: "admin" and "root" are taken
    formatStub.Interceptor.OnCall((value) => value.Length >= 3);
    uniqueStub.Interceptor.OnCall((value) => value != "admin" && value != "root");

    // Create validator with stubbed rules
    var validator = new UsernameValidator(uniqueStub, formatStub);

    // Act & Assert - test various scenarios
    var (valid1, error1) = validator.Validate("ab");
    Assert.False(valid1);
    Assert.Equal("Invalid format", error1);

    var (valid2, error2) = validator.Validate("admin");
    Assert.False(valid2);
    Assert.Equal("Username already taken", error2);

    var (valid3, error3) = validator.Validate("alice");
    Assert.True(valid3);
    Assert.Null(error3);

    // Verify rules were invoked
    formatStub.Interceptor.Verify(Times.Exactly(3)); // All 3 usernames checked
    uniqueStub.Interceptor.Verify(Times.Exactly(2)); // Only valid formats check uniqueness
}
```
<!-- endSnippet -->

---

## Next Steps

- **[Stub Patterns](stub-patterns.md)** - Learn about Stand-Alone, Inline Interface, and Inline Class patterns
- **[Methods Guide](methods.md)** - Configure method behavior with OnCall
- **[Interceptor API Reference](../reference/interceptor-api.md)** - Complete API documentation
