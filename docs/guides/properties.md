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
// Set a static value for the property via the interceptor
stub.CurrentUser.OnGet(new User { Id = 1, Name = "Alice" });
```
<!-- endSnippet -->

When setting up test fixtures, you can configure multiple properties at once:

<!-- snippet: properties-value-multiple -->
```cs
// Configure several properties before test execution
stub.UserId.OnGet(42);
stub.Email.OnGet("test@example.com");
stub.CurrentUser.OnGet(new User { Id = 42, Name = "Test User" });
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
// OnGet callback returns dynamic value on each access
stub.Timestamp.OnGet(() => DateTime.UtcNow);
```
<!-- endSnippet -->

OnGet callbacks can access other interceptors on the stub to create state-dependent behavior:

<!-- snippet: properties-onget-stateful -->
```cs
// OnGet checks the tracked state
stub.IsReady.OnGet(() => isInitialized);
// Initialize method updates the tracked state
stub.Initialize.OnCall(() => { isInitialized = true; });
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
// OnSet captures every value written to the property
var setValues = new List<string>();
stub.Name.OnSet((value) => setValues.Add(value));
```
<!-- endSnippet -->

You can also use `OnSet` to simulate validation logic in dependencies:

<!-- snippet: properties-onset-validation -->
```cs
// OnSet throws for invalid values
stub.Age.OnSet((value) =>
{
    if (value < 0)
        throw new ArgumentException("Age cannot be negative");
});
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
// VerifyGet checks how many times property was read
stub.Age.VerifyGet(Times.Exactly(2));
```
<!-- endSnippet -->

<!-- snippet: properties-verify-lastsetvalue -->
```cs
// LastSetValue contains the most recent value
Assert.Equal("Expected", stub.Name.LastSetValue);
```
<!-- endSnippet -->

### Using Verifiable() on Properties

<!-- snippet: properties-verifiable -->
```cs
// Mark property as verifiable - requires access before Verify()
stub.Name.OnGet("test");
stub.Name.Verifiable();
stub.Age.Verifiable();
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

Use sequences when a property should return different values across multiple reads or react differently to multiple writes. Sequences are created by chaining `ThenGet()` or `ThenSet()` after the initial `OnGet()` or `OnSet()` call.

### Return Value Sequences

When you need a property to return different values on successive reads, use `OnGet().ThenGet()`.

**Value overloads:** Both `OnGet(value)` and `ThenGet(value)` accept static values as shorthand for callback syntax. Use whichever reads better for your scenario.

**Sequence exhaustion:** After returning all values in the sequence, subsequent reads repeat the last value. Use `ThenDefault()` to return `default(T)` after exhaustion instead.

<!-- snippet: properties-ongetsequence-value -->
```cs
// OnGet with value, ThenGet elevates to sequence mode
stub.Name.OnGet("First")
    .ThenGet(() => "Second")
    .ThenGet(() => "Third");
```
<!-- endSnippet -->

You can also mix value and callback syntax in the same sequence:

<!-- snippet: properties-onget-then-sequence -->
```cs
// OnGet().ThenGet() configures different return values for each read
stub.Name
    .OnGet(() => "First")
    .ThenGet(() => "Second")
    .ThenGet(() => "Third");
```
<!-- endSnippet -->

**When to use get sequences:**
- Testing pagination where page numbers change
- Simulating changing state over time
- Testing retry logic that checks status repeatedly
- Verifying behavior with different data on successive calls

### Sequence Exhaustion Behavior

After a sequence returns all configured values, subsequent reads **repeat the last value** by default. This matches NSubstitute's behavior and works well for most test scenarios.

<!-- snippet: properties-sequence-exhaustion -->
```cs
// Configure a sequence - after exhaustion, repeats last value
stub.Name.OnGet("first")
    .ThenGet("second")
    .ThenGet("third");
```
<!-- endSnippet -->

To return `default(T)` after exhaustion instead of repeating the last value, chain `.ThenDefault()` at the end of the sequence:

<!-- snippet: properties-sequence-thendefault -->
```cs
// ThenDefault() returns default(T) after exhaustion instead of repeating
stub.Name.OnGet("first")
    .ThenGet("second")
    .ThenDefault();
```
<!-- endSnippet -->

**Note:** In strict mode (`stub.Strict = true`), accessing an exhausted sequence throws `StubException` instead of repeating the last value.

### Setter Sequences

When you need different behavior for successive property writes, use `OnSet().ThenSet()`:

<!-- snippet: properties-onset-then-sequence -->
```cs
// OnSet().ThenSet() configures different callbacks for each write
stub.Name
    .OnSet((value) => { firstWriteValue = $"First: {value}"; })
    .ThenSet((value) => { secondWriteValue = $"Second: {value}"; });
```
<!-- endSnippet -->

**When to use set sequences:**
- Testing validation that changes over time
- Simulating connection state changes
- Testing error recovery (first write fails, second succeeds)

### Sequence vs. Single Callbacks

| Use Case | Use This | Why |
|----------|----------|-----|
| Property always returns same value | `OnGet(value)` or `OnGet(() => value)` | Simple, no sequence needed |
| Property returns different values per read | `OnGet(() => first).ThenGet(() => second)` | Different values on successive reads |
| Property setter should validate differently | `OnSet((v) => validate1(v)).ThenSet((v) => validate2(v))` | Different behavior per write |
| Property behavior changes based on test state | `OnGet(() => computedValue)` | Callback computes on each access |

### Sequence Tracking

Both `OnGet().ThenGet()` and `OnSet().ThenSet()` return sequence-specific interfaces that support chaining and verification:

**OnGet returns IPropertyGetSequence\<T\>:**
- Use `.ThenGet(() => value)` to chain additional get behaviors
- Each callback in the sequence is called once in order
- After exhausting the sequence, behavior depends on strict mode (see Troubleshooting)

**OnSet returns IPropertySetSequence\<T\>:**
- Use `.ThenSet((value) => { })` to chain additional set behaviors
- Each callback in the sequence is called once in order
- After exhausting the sequence, subsequent writes do nothing (non-strict) or throw (strict mode)

<!-- snippet: properties-sequence-verification -->
```cs
// Sequences support verification like regular callbacks
var getSequence = stub.Name
    .OnGet(() => "A")
    .ThenGet(() => "B");

var setSequence = stub.Age
    .OnSet((v) => { })
    .ThenSet((v) => { });
```
<!-- endSnippet -->

---

## OnGet Priority (Value vs Callback)

When you call `OnGet` multiple times, the last call wins. You can start with a static value using `OnGet(value)` and later upgrade to a dynamic callback using `OnGet(() => callback)`.

<!-- snippet: properties-priority -->
```cs
// Last OnGet call wins - can upgrade from value to callback
stub.Name.OnGet("initial");
stub.Name.OnGet(() => "dynamic");
```
<!-- endSnippet -->

**Design principle:** This allows upgrading from simple static value configuration to dynamic callback behavior without removing the previous assignment first.

---

## Resetting Properties

Calling `Reset()` on a property interceptor clears tracking state but preserves the configured callbacks.

<!-- snippet: properties-reset -->
```cs
// Reset clears counts but preserves callbacks
stub.Name.Reset();

stub.Name.VerifyGet(Times.Never);
stub.Name.VerifySet(Times.Never);
```
<!-- endSnippet -->

**Note on Reset behavior:** Reset() clears call counts, `LastSetValue`, and resets sequence positions to the start, but **preserves** all configured callbacks (`OnGet`, `OnSet`, sequences). The callbacks remain configured and will execute again on subsequent property access.

---

## User Properties (Standalone Patterns)

When using standalone patterns, you can define **user properties** by overriding the generated base class properties with an underscore suffix. This provides reusable default implementations that work across all tests.

### When to Use

- **Reusable defaults** - Define once, use across all tests
- **Computed values** - When property values require logic
- **State management** - When you need instance state with backing fields
- **Constructor injection** - Pass data at stub construction time

### Basic Setup

Override protected virtual properties using the underscore suffix convention:

```csharp
public interface IMyRepo
{
    int Count { get; }
    string Name { get; set; }
}

[KnockOff]
public partial class MyRepoStub(List<User> users) : IMyRepo
{
    // Get-only property: return computed value
    protected override int Count_ => users.Count;

    // Get/set property: use backing field
    private string _name = "";
    protected override string Name_
    {
        get => _name;
        set => _name = value;
    }
}
```

### Priority Order

When multiple configurations exist:

1. **OnGet/OnSet** - Per-test override (highest priority)
2. **User property** - Shared default from protected override
3. **Smart default** - Returns `default(T)` or throws in strict mode

```csharp
var stub = new MyRepoStub(new List<User> { new(), new() });
IMyRepo repo = stub;

// User property provides the default
var count = repo.Count;  // 2

// OnGet supersedes user property for this test
stub.Count.OnGet(999);
count = repo.Count;  // 999
```

### Tracking Works

User property interceptors provide full tracking even when using the user override:

```csharp
var stub = new MyRepoStub(users);
IMyRepo repo = stub;

_ = repo.Count;
_ = repo.Count;

stub.Count.VerifyGet(Times.Exactly(2));
```

### Strict Mode Behavior

User properties bypass strict mode because they ARE configured:

```csharp
[KnockOff(Strict = true)]
public partial class StrictRepoStub : IMyRepo
{
    protected override int Count_ => 10;  // This IS configured - no exception
}
```

### Supported Patterns

User properties work with all four standalone patterns:

| Pattern | User Property Support |
|---------|----------------------|
| Standalone | Yes |
| Generic Standalone | Yes |
| Standalone Class | Yes |
| Generic Standalone Class | Yes |
| Inline patterns (5-9) | No (entire class generated) |

---

## Decision Guide

Choose your configuration approach based on the test scenario:

| Scenario | Use This | Example |
|----------|----------|---------|
| Property should return fixed test data | `OnGet(value)` | `stub.UserId.OnGet(42);` |
| Property should return current time/random value | `OnGet(callback)` | `stub.Now.OnGet(() => DateTime.UtcNow);` |
| Property depends on other stub state | `OnGet(callback)` | `stub.IsReady.OnGet(() => isInitialized);` |
| Property returns different values per read | `OnGet().ThenGet()` | `stub.Status.OnGet("Pending").ThenGet("Complete");` |
| Track all values written to property | `OnSet` | `stub.Name.OnSet((v) => list.Add(v));` |
| Simulate validation in dependency | `OnSet` | `stub.Age.OnSet((v) => Validate(v));` |
| Setter behavior changes across writes | `OnSet().ThenSet()` | `stub.Config.OnSet((v) => Reject(v)).ThenSet((v) => Accept(v));` |
| Verify property was accessed N times | Verification | `stub.UserId.VerifyGet(Times.Exactly(2));` |
| Verify last value written | Verification | `Assert.Equal("x", stub.Name.LastSetValue);` |

---

## Complete Example

This example demonstrates all property configuration approaches in a realistic test scenario.

<!-- snippet: properties-complete-example -->
```cs
// OnGet with static value: Fixed test data
stub.CurrentUser.OnGet(new User { Id = 1, Name = "Alice" });

// OnGet with callback: State-dependent behavior
stub.IsConnected.OnGet(() => isConnected);

// OnSet: Track all values written
var connectionStrings = new List<string>();
stub.ConnectionString.OnSet((value) => connectionStrings.Add(value));

// Method callback updates the tracked state
stub.Connect.OnCall(() => { isConnected = true; });
```
<!-- endSnippet -->

---

## Key Takeaways

1. **Start with OnGet(value)** - Static value syntax covers most scenarios and keeps tests simple
2. **Use OnGet(callback) for computed values** - Time-dependent or state-dependent returns
3. **Use OnSet for tracking** - When you need to verify writes or simulate validation
4. **Use sequences for changing behavior** - `OnGet().ThenGet()` / `OnSet().ThenSet()` when values or behavior differ across calls
5. **Last OnGet wins** - You can upgrade from static values to dynamic callbacks by calling OnGet again
6. **Reset() preserves configuration** - Clears call counts and sequence position, but preserves OnGet/OnSet callbacks
7. **Verify access patterns** - Use `VerifyGet()` and `VerifySet()` like method verification

---

**Next Steps:**
- [Method Configuration Guide](methods.md) - Configure method behavior and callbacks
- [Verification Patterns](verification.md) - Assert on stub interactions
- [Interceptor API Reference](../reference/interceptor-api.md) - Complete interceptor API documentation

---

**UPDATED:** 2026-02-02
