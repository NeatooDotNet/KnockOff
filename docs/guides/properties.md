# Property Configuration Guide

Properties in KnockOff can be configured two ways: **static values** for test data or **dynamic callbacks** for computed/stateful behavior. Choose the approach that matches your test scenario.

---

## Configuration Approaches

**Static Value (Recommended for Test Data)**
- Use `Property.OnGet(value)` before running test
- Syntactic sugar for `OnGet(() => value)` - creates a callback that returns the value
- Use when the property should return a fixed value
- Simple, readable, and covers most test scenarios

**Dynamic Callbacks (For Complex Scenarios)**
- Use `Property.OnGet(() => callback)` to compute values at runtime
- Use `Property.OnSet((value) => callback)` to intercept and validate writes
- Use when values depend on state, time, or other factors

---

## Static Values (Recommended for Test Data)

The simplest way to configure a property is to use `OnGet(value)` with a static value before your test runs. This is ideal for pre-populating dependencies with test data.

**Note:** `OnGet(value)` is syntactic sugar that internally creates `OnGet(() => value)`. Both forms return the same tracking interface and work identically.

<!-- snippet: properties-value-basic -->
```cs
[Fact]
public void Value_SetsPropertyReturnValue()
{
    var stub = new UserConfigPropsStub();

    // Set a static value for the property via the interceptor
    stub.CurrentUser.OnGet(new User { Id = 1, Name = "Alice" });

    IUserConfigProps config = stub;
    var user = config.CurrentUser;

    Assert.NotNull(user);
    Assert.Equal("Alice", user.Name);
}
```
<!-- endSnippet -->

When setting up test fixtures, you can configure multiple properties at once:

<!-- snippet: properties-value-multiple -->
```cs
[Fact]
public void Value_ConfigureMultipleProperties()
{
    var stub = new UserConfigPropsStub();

    // Configure several properties before test execution
    stub.UserId.OnGet(42);
    stub.Email.OnGet("test@example.com");
    stub.CurrentUser.OnGet(new User { Id = 42, Name = "Test User" });

    IUserConfigProps config = stub;

    Assert.Equal(42, config.UserId);
    Assert.Equal("test@example.com", config.Email);
    Assert.NotNull(config.CurrentUser);
}
```
<!-- endSnippet -->

**When to use OnGet with static values:**
- Pre-populating repository stub data
- Configuring service dependencies with fixed values
- Setting up DTOs or configuration objects
- Any scenario where the value doesn't change during the test

---

## Dynamic Getters

Use `OnGet(() => callback)` when a property's value should be computed at access time. The callback is invoked each time the property is accessed.

<!-- snippet: properties-onget-dynamic -->
```cs
[Fact]
public void OnGet_ReturnsComputedValue()
{
    var stub = new TimeProviderPropsStub();

    // OnGet callback returns dynamic value on each access
    stub.Timestamp.OnGet(() => DateTime.UtcNow);

    ITimeProviderProps timeProvider = stub;

    var time1 = timeProvider.Timestamp;
    Thread.Sleep(10);
    var time2 = timeProvider.Timestamp;

    // Each access returns current time
    Assert.True(time2 >= time1);
}
```
<!-- endSnippet -->

OnGet callbacks can access other interceptors on the stub to create state-dependent behavior:

<!-- snippet: properties-onget-stateful -->
```cs
[Fact]
public void OnGet_DependsOnOtherInterceptorState()
{
    var stub = new ServiceWithInitPropsStub();

    // Track initialization state with local variable
    var isInitialized = false;

    // OnGet checks the tracked state
    stub.IsReady.OnGet(() => isInitialized);
    var initTracking = stub.Initialize.OnCall(() => { isInitialized = true; });

    IServiceWithInitProps service = stub;

    // Initially false (Initialize not called)
    Assert.False(service.IsReady);

    // After Initialize, becomes true
    service.Initialize();
    Assert.True(service.IsReady);
}
```
<!-- endSnippet -->

**When to use OnGet:**
- Values that change over time (timestamps, random values)
- Computed values based on other stub state
- Simulating stateful behavior in dependencies
- Testing race conditions or timing-dependent logic

---

## Setter Interception

Use `OnSet` to intercept property writes. This allows tracking values or validating input during tests.

<!-- snippet: properties-onset-tracking -->
```cs
[Fact]
public void OnSet_TracksAllWrittenValues()
{
    var stub = new ConfigPropsStub();

    var setValues = new List<string>();
    stub.Name.OnSet((value) => setValues.Add(value));

    IConfigProps config = stub;

    config.Name = "First";
    config.Name = "Second";
    config.Name = "Third";

    Assert.Equal(3, setValues.Count);
    Assert.Equal(new[] { "First", "Second", "Third" }, setValues);
}
```
<!-- endSnippet -->

You can also use `OnSet` to simulate validation logic in dependencies:

<!-- snippet: properties-onset-validation -->
```cs
[Fact]
public void OnSet_SimulatesValidation()
{
    var stub = new ConfigPropsStub();

    // OnSet throws for invalid values
    stub.Age.OnSet((value) =>
    {
        if (value < 0)
            throw new ArgumentException("Age cannot be negative");
    });

    IConfigProps config = stub;

    // Valid value works
    config.Age = 25;

    // Invalid value throws
    Assert.Throws<ArgumentException>(() => config.Age = -1);
}
```
<!-- endSnippet -->

**When to use OnSet:**
- Tracking all values written to a property
- Simulating validation failures in dependencies
- Testing how your code handles property setter exceptions
- Verifying the sequence of property writes

---

## Verifying Property Access

Property interceptors support verification similar to methods.

### Using Verify() on Properties

<!-- snippet: properties-verify-getcount -->
```cs
[Fact]
public void VerifyGet_TracksPropertyReads()
{
    var stub = new ConfigPropsStub();
    stub.Age.OnGet(42);

    IConfigProps service = stub;

    _ = service.Age;
    _ = service.Age;

    // VerifyGet checks how many times property was read
    stub.Age.VerifyGet(Times.Exactly(2));
}
```
<!-- endSnippet -->

<!-- snippet: properties-verify-lastsetvalue -->
```cs
[Fact]
public void LastSetValue_CapturesLastWrittenValue()
{
    var stub = new ConfigPropsStub();

    IConfigProps service = stub;

    service.Name = "First";
    service.Name = "Second";
    service.Name = "Expected";

    // LastSetValue contains the most recent value
    Assert.Equal("Expected", stub.Name.LastSetValue);
}
```
<!-- endSnippet -->

### Using Verifiable() on Properties

<!-- snippet: properties-verifiable -->
```cs
[Fact]
public void Verifiable_MarksPropertyForVerification()
{
    var stub = new ConfigPropsStub();

    // Mark property as verifiable
    stub.Name.OnGet("test");
    stub.Name.Verifiable();
    stub.Age.Verifiable();

    IConfigProps service = stub;
    _ = service.Name;
    service.Age = 42;

    // Verify individually (standalone stubs verify at interceptor level)
    stub.Name.Verify();
    stub.Age.Verify();
}
```
<!-- endSnippet -->

**Available verification methods:**
- `VerifyGet(Times)` - Verify property getter was called
- `VerifySet(Times)` - Verify property setter was called
- `Verifiable()` - Mark property for batch verification (verifies both getter and setter)
- `Verifiable(Times)` - Mark property for batch verification with specific Times constraint

**Available inspection properties:**
- `LastSetValue` - The most recent value written to the property setter

---

## Sequence Behavior

Use sequences when a property should return different values across multiple reads or react differently to multiple writes. Sequences are configured with `OnGetSequence` and `OnSetSequence`, which return tracking objects that support `ThenGet` and `ThenSet` chaining.

### Return Value Sequences (OnGetSequence)

When you need a property to return different values on successive reads, use `OnGetSequence`.

**Value overloads:** Both `OnGetSequence(value)` and `ThenGet(value)` accept static values as shorthand for callback syntax. Use whichever reads better for your scenario.

<!-- snippet: properties-ongetsequence-value -->
```cs
[Fact]
public void OnGetSequence_ValueSyntax()
{
    var stub = new ConfigPropsStub();

    // OnGetSequence with value - simpler syntax for static values
    // Note: First value uses OnGetSequence(value), chain uses callback syntax
    stub.Name.OnGetSequence("First")
        .ThenGet(() => "Second")
        .ThenGet(() => "Third");

    IConfigProps config = stub;

    Assert.Equal("First", config.Name);
    Assert.Equal("Second", config.Name);
    Assert.Equal("Third", config.Name);
}
```
<!-- endSnippet -->

You can also mix value and callback syntax in the same sequence:

<!-- snippet: properties-ongetsequence-basic -->
```cs
[Fact]
public void OnGetSequence_ReturnsDifferentValuesOnSuccessiveReads()
{
    var stub = new ConfigPropsStub();

    // OnGetSequence configures different return values for each read
    stub.Name
        .OnGetSequence(() => "First")
        .ThenGet(() => "Second")
        .ThenGet(() => "Third");

    IConfigProps config = stub;

    // Each read returns the next value in the sequence
    Assert.Equal("First", config.Name);
    Assert.Equal("Second", config.Name);
    Assert.Equal("Third", config.Name);
}
```
<!-- endSnippet -->

**When to use OnGetSequence:**
- Testing pagination where page numbers change
- Simulating changing state over time
- Testing retry logic that checks status repeatedly
- Verifying behavior with different data on successive calls

### Setter Sequences (OnSetSequence)

When you need different behavior for successive property writes, use `OnSetSequence`:

<!-- snippet: properties-onsetsequence-basic -->
```cs
[Fact]
public void OnSetSequence_ReactsDifferentlyToSuccessiveWrites()
{
    var stub = new ConfigPropsStub();

    var firstWriteValue = "";
    var secondWriteValue = "";

    // OnSetSequence configures different callbacks for each write
    stub.Name
        .OnSetSequence((value) => { firstWriteValue = $"First: {value}"; })
        .ThenSet((value) => { secondWriteValue = $"Second: {value}"; });

    IConfigProps config = stub;

    // First write triggers first callback
    config.Name = "Alpha";
    Assert.Equal("First: Alpha", firstWriteValue);
    Assert.Equal("", secondWriteValue);

    // Second write triggers second callback
    config.Name = "Beta";
    Assert.Equal("Second: Beta", secondWriteValue);
}
```
<!-- endSnippet -->

**When to use OnSetSequence:**
- Testing validation that changes over time
- Simulating connection state changes
- Testing error recovery (first write fails, second succeeds)

### Sequence vs. Single Callbacks

| Use Case | Use This | Why |
|----------|----------|-----|
| Property always returns same value | `OnGet(value)` or `OnGet(() => value)` | Simple, no sequence needed |
| Property returns different values per read | `OnGetSequence(() => first).ThenGet(() => second)` | Different values on successive reads |
| Property setter should validate differently | `OnSetSequence((v) => validate1(v)).ThenSet((v) => validate2(v))` | Different behavior per write |
| Property behavior changes based on test state | `OnGet(() => computedValue)` | Callback computes on each access |

### Sequence Tracking

Both `OnGetSequence` and `OnSetSequence` return sequence-specific interfaces that support chaining and verification:

**OnGetSequence returns IPropertyGetSequence\<T\>:**
- Use `.ThenGet(() => value)` to chain additional get behaviors
- Each callback in the sequence is called once in order
- After exhausting the sequence, behavior depends on strict mode (see Troubleshooting)

**OnSetSequence returns IPropertySetSequence\<T\>:**
- Use `.ThenSet((value) => { })` to chain additional set behaviors
- Each callback in the sequence is called once in order
- After exhausting the sequence, subsequent writes do nothing (non-strict) or throw (strict mode)

<!-- snippet: properties-sequence-verification -->
```cs
[Fact]
public void Sequence_VerifiesLikeRegularCallbacks()
{
    var stub = new ConfigPropsStub();

    // Configure sequences
    var getSequence = stub.Name
        .OnGetSequence(() => "A")
        .ThenGet(() => "B");

    var setSequence = stub.Age
        .OnSetSequence((v) => { })
        .ThenSet((v) => { });

    IConfigProps config = stub;

    // Access properties
    _ = config.Name;
    _ = config.Name;
    config.Age = 1;
    config.Age = 2;

    // Verify sequence was fully consumed
    getSequence.Verify();
    setSequence.Verify();

    // VerifyGet/VerifySet work the same with sequences
    stub.Name.VerifyGet(Times.Exactly(2));
    stub.Age.VerifySet(Times.Exactly(2));
}
```
<!-- endSnippet -->

---

## OnGet Priority (Value vs Callback)

When you call `OnGet` multiple times, the last call wins. You can start with a static value using `OnGet(value)` and later upgrade to a dynamic callback using `OnGet(() => callback)`.

<!-- snippet: properties-priority -->
```cs
[Fact]
public void OnGet_TakesPrecedenceOverValue()
{
    var stub = new ConfigPropsStub();

    // Set a static value
    stub.Name.OnGet("initial");

    // Then set OnGet - it takes precedence
    stub.Name.OnGet(() => "dynamic");

    IConfigProps config = stub;

    // Callback syntax takes precedence (last call wins)
    Assert.Equal("dynamic", config.Name);
}
```
<!-- endSnippet -->

**Design principle:** This allows upgrading from simple static value configuration to dynamic callback behavior without removing the previous assignment first.

---

## Resetting Properties

Calling `Reset()` on a property interceptor clears tracking state but preserves the configured callbacks.

<!-- snippet: properties-reset -->
```cs
[Fact]
public void Reset_ClearsCountsButPreservesValue()
{
    var stub = new ConfigPropsStub();

    stub.Name.OnGet("test");

    IConfigProps config = stub;

    // Access property to increment counts
    _ = config.Name;
    config.Name = "updated";

    stub.Name.VerifyGet(Times.AtLeastOnce);
    stub.Name.VerifySet(Times.AtLeastOnce);

    // Reset clears counts and callbacks
    stub.Name.Reset();

    stub.Name.VerifyGet(Times.Never);
    stub.Name.VerifySet(Times.Never);
    // Note: Reset clears tracking counters and all configured callbacks
}
```
<!-- endSnippet -->

**Note on Reset behavior:** Reset() clears call counts, `LastSetValue`, and resets sequence positions to the start, but **preserves** all configured callbacks (`OnGet`, `OnSet`, sequences). The callbacks remain configured and will execute again on subsequent property access.

---

## Decision Guide

Choose your configuration approach based on the test scenario:

| Scenario | Use This | Example |
|----------|----------|---------|
| Property should return fixed test data | `OnGet(value)` | `stub.UserId.OnGet(42);` |
| Property should return current time/random value | `OnGet(callback)` | `stub.Now.OnGet(() => DateTime.UtcNow);` |
| Property depends on other stub state | `OnGet(callback)` | `stub.IsReady.OnGet(() => isInitialized);` |
| Property returns different values per read | `OnGetSequence` | `stub.Status.OnGetSequence(() => "Pending").ThenGet(() => "Complete");` |
| Track all values written to property | `OnSet` | `stub.Name.OnSet((v) => list.Add(v));` |
| Simulate validation in dependency | `OnSet` | `stub.Age.OnSet((v) => Validate(v));` |
| Setter behavior changes across writes | `OnSetSequence` | `stub.Config.OnSetSequence((v) => Reject(v)).ThenSet((v) => Accept(v));` |
| Verify property was accessed N times | Verification | `stub.UserId.VerifyGet(Times.Exactly(2));` |
| Verify last value written | Verification | `Assert.Equal("x", stub.Name.LastSetValue);` |

---

## Complete Example

This example demonstrates all property configuration approaches in a realistic test scenario.

<!-- snippet: properties-complete-example -->
```cs
[Fact]
public void CompletePropertyExample_AllConfigurationApproaches()
{
    var stub = new UserConfigCompleteStub();

    // Track connection state with local variable
    var isConnected = false;

    // OnGet with static value: Fixed test data
    stub.CurrentUser.OnGet(new User { Id = 1, Name = "Alice" });

    // OnGet: State-dependent behavior using tracked state
    stub.IsConnected.OnGet(() => isConnected);

    // OnSet: Track all values written
    var connectionStrings = new List<string>();
    stub.ConnectionString.OnSet((value) => connectionStrings.Add(value));

    // Configure the Connect method to update state
    var connectTracking = stub.Connect.OnCall(() => { isConnected = true; });

    IUserConfigComplete service = stub;

    // Test execution
    var user = service.CurrentUser;            // Read CurrentUser
    Assert.False(service.IsConnected);         // Not connected yet

    service.Connect();                          // Call Connect
    Assert.True(service.IsConnected);          // Now connected

    service.ConnectionString = "Server=test";  // Write ConnectionString

    // Verification
    stub.CurrentUser.VerifyGet(Times.Once);
    Assert.True(service.IsConnected);
    Assert.Single(connectionStrings);
    Assert.Equal("Server=test", stub.ConnectionString.LastSetValue);
}
```
<!-- endSnippet -->

---

## Key Takeaways

1. **Start with OnGet(value)** - Static value syntax covers most scenarios and keeps tests simple
2. **Use OnGet(callback) for computed values** - Time-dependent or state-dependent returns
3. **Use OnSet for tracking** - When you need to verify writes or simulate validation
4. **Use sequences for changing behavior** - OnGetSequence/OnSetSequence when values or behavior differ across calls
5. **Last OnGet wins** - You can upgrade from static values to dynamic callbacks by calling OnGet again
6. **Reset() preserves configuration** - Clears call counts and sequence position, but preserves OnGet/OnSet callbacks
7. **Verify access patterns** - Use `VerifyGet()` and `VerifySet()` like method verification

---

**Next Steps:**
- [Method Configuration Guide](methods.md) - Configure method behavior and callbacks
- [Verification Patterns](verification.md) - Assert on stub interactions
- [Interceptor API Reference](../reference/interceptor-api.md) - Complete interceptor API documentation

---

**UPDATED:** 2026-01-25
