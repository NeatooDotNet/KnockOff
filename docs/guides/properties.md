# Property Configuration Guide

Properties in KnockOff can be configured two ways: **static values** for test data or **dynamic callbacks** for computed/stateful behavior. Choose the approach that matches your test scenario.

---

## Configuration Approaches

**Static Value (Recommended for Test Data)**
- Set `Property.Value` before running test
- Use when the property should return a fixed value
- Simple, readable, and covers most test scenarios

**Dynamic Callbacks (For Complex Scenarios)**
- Set `Property.OnGet` to compute values at runtime
- Set `Property.OnSet` to intercept and validate writes
- Use when values depend on state, time, or other factors

---

## Static Values (Recommended for Test Data)

The simplest way to configure a property is to assign a value before your test runs. This is ideal for pre-populating dependencies with test data.

<!-- snippet: properties-value-basic -->
```cs
[Fact]
public void Value_SetsPropertyReturnValue()
{
    var stub = new UserConfigPropsStub();

    // Set a static value for the property via the interceptor
    stub.CurrentUser.Value = new User { Id = 1, Name = "Alice" };

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
    stub.UserId.Value = 42;
    stub.Email.Value = "test@example.com";
    stub.CurrentUser.Value = new User { Id = 42, Name = "Test User" };

    IUserConfigProps config = stub;

    Assert.Equal(42, config.UserId);
    Assert.Equal("test@example.com", config.Email);
    Assert.NotNull(config.CurrentUser);
}
```
<!-- endSnippet -->

**When to use Value:**
- Pre-populating repository stub data
- Configuring service dependencies with fixed values
- Setting up DTOs or configuration objects
- Any scenario where the value doesn't change during the test

---

## Dynamic Getters

Use `OnGet` when a property's value should be computed at access time. The callback receives the stub instance as a parameter.

<!-- snippet: properties-onget-dynamic -->
```cs
[Fact]
public void OnGet_ReturnsComputedValue()
{
    var stub = new TimeProviderPropsStub();

    // OnGet callback returns dynamic value on each access
    stub.Timestamp.OnGet = (ko) => DateTime.UtcNow;

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

    // OnGet checks if Initialize() was called via interceptor CallCount
    stub.IsReady.OnGet = (ko) => stub.Initialize.CallCount > 0;
    var initTracking = stub.Initialize.OnCall((ko) => { });

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
    stub.Name.OnSet = (ko, value) => setValues.Add(value);

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
    stub.Age.OnSet = (ko, value) =>
    {
        if (value < 0)
            throw new ArgumentException("Age cannot be negative");
    };

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

All property interceptors track access counts and the last value set.

<!-- snippet: properties-verify-getcount -->
```cs
[Fact]
public void GetCount_TracksPropertyReads()
{
    var stub = new ConfigPropsStub();
    stub.Age.Value = 42;

    IConfigProps service = stub;

    _ = service.Age;
    _ = service.Age;

    // GetCount tracks how many times property was read
    Assert.Equal(2, stub.Age.GetCount);
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

**Available verification properties:**
- `GetCount` - Number of times property was read
- `SetCount` - Number of times property was written
- `LastSetValue` - The most recent value written (null if never set)

---

## Value vs OnGet Priority

When both `Value` and `OnGet` are configured, `OnGet` takes precedence. Setting `OnGet` replaces any previously set `Value`.

<!-- snippet: properties-priority -->
```cs
[Fact]
public void OnGet_TakesPrecedenceOverValue()
{
    var stub = new ConfigPropsStub();

    // Set a static value
    stub.Name.Value = "initial";

    // Then set OnGet - it takes precedence
    stub.Name.OnGet = (ko) => "dynamic";

    IConfigProps config = stub;

    // OnGet wins over Value
    Assert.Equal("dynamic", config.Name);
}
```
<!-- endSnippet -->

**Design principle:** This allows upgrading from simple Value configuration to dynamic OnGet behavior without removing the Value assignment first.

---

## Resetting Properties

Calling `Reset()` on a property interceptor clears all counters and callbacks but **preserves the Value**.

<!-- snippet: properties-reset -->
```cs
[Fact]
public void Reset_ClearsCountsButPreservesValue()
{
    var stub = new ConfigPropsStub();

    stub.Name.Value = "test";

    IConfigProps config = stub;

    // Access property to increment counts
    _ = config.Name;
    config.Name = "updated";

    Assert.True(stub.Name.GetCount > 0);
    Assert.True(stub.Name.SetCount > 0);

    // Reset clears counts and callbacks
    stub.Name.Reset();

    Assert.Equal(0, stub.Name.GetCount);
    Assert.Equal(0, stub.Name.SetCount);
    // Note: Reset also clears Value, OnGet, OnSet
}
```
<!-- endSnippet -->

**Why Value is preserved:** Value represents test data configuration, not test execution state. Resetting execution state (counts, callbacks) shouldn't require reconfiguring test data.

---

## Decision Guide

Choose your configuration approach based on the test scenario:

| Scenario | Use This | Example |
|----------|----------|---------|
| Property should return fixed test data | `Value` | `stub.UserId.Value = 42;` |
| Property should return current time/random value | `OnGet` | `stub.Now.OnGet = (ko) => DateTime.UtcNow;` |
| Property depends on other stub state | `OnGet` | `stub.IsReady.OnGet = (ko) => ko.Init.WasCalled;` |
| Track all values written to property | `OnSet` | `stub.Name.OnSet = (ko, v) => list.Add(v);` |
| Simulate validation in dependency | `OnSet` | `stub.Age.OnSet = (ko, v) => Validate(v);` |
| Verify property was accessed N times | Verification | `Assert.Equal(2, stub.UserId.GetCount);` |
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

    // Value: Static test data
    stub.CurrentUser.Value = new User { Id = 1, Name = "Alice" };

    // OnGet: State-dependent behavior
    stub.IsConnected.OnGet = (ko) => stub.Connect.CallCount > 0;

    // OnSet: Track all values written
    var connectionStrings = new List<string>();
    stub.ConnectionString.OnSet = (ko, value) => connectionStrings.Add(value);

    // Configure the Connect method
    var connectTracking = stub.Connect.OnCall((ko) => { });

    IUserConfigComplete service = stub;

    // Test execution
    var user = service.CurrentUser;            // Read CurrentUser
    Assert.False(service.IsConnected);         // Not connected yet

    service.Connect();                          // Call Connect
    Assert.True(service.IsConnected);          // Now connected

    service.ConnectionString = "Server=test";  // Write ConnectionString

    // Verification
    Assert.Equal(1, stub.CurrentUser.GetCount);
    Assert.True(service.IsConnected);
    Assert.Single(connectionStrings);
    Assert.Equal("Server=test", stub.ConnectionString.LastSetValue);
}
```
<!-- endSnippet -->

---

## Key Takeaways

1. **Start with Value** - It covers most scenarios and keeps tests simple
2. **Use OnGet for computed values** - Time-dependent or state-dependent returns
3. **Use OnSet for tracking** - When you need to verify writes or simulate validation
4. **OnGet replaces Value** - You can upgrade from static to dynamic without conflicts
5. **Reset() preserves Value** - Clears execution state but not test data configuration
6. **Verify access patterns** - GetCount, SetCount, and LastSetValue provide test assertions

---

**Next Steps:**
- [Method Configuration Guide](methods.md) - Configure method behavior and callbacks
- [Verification Patterns](verification.md) - Assert on stub interactions
- [Interceptor API Reference](../reference/interceptor-api.md) - Complete interceptor API documentation
