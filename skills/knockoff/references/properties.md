# Property Interceptor Reference

This reference covers all aspects of property interceptors in KnockOff, including static values, dynamic callbacks, sequences, verification, and reset behavior.

---

## Overview

Property interceptors are generated for every property in an interface. Each interceptor provides:

- **Get(value)** - Set a static value to return from the getter
- **Get(callback)** - Dynamic callback for computed values
- **Set(callback)** - Callback for intercepting setter calls
- **Get().ThenGet() / Set().ThenSet()** - Different behavior for successive accesses (sequences)
- **Verification methods** - For asserting on property access patterns
- **LastSetValue** - For capturing the most recent value written to a setter

---

## Setting Static Values with Get

The `Get(value)` method is the simplest way to configure a property. Call it before your test runs to return a fixed value.

<!-- snippet: properties-value-basic -->
```cs
// Set a static value for the property via the interceptor
stub.CurrentUser.Get(new User { Id = 1, Name = "Alice" });
```
<!-- endSnippet -->

Configure multiple properties at once for test fixtures:

<!-- snippet: properties-value-multiple -->
```cs
// Configure several properties before test execution
stub.UserId.Get(42);
stub.Email.Get("test@example.com");
stub.CurrentUser.Get(new User { Id = 42, Name = "Test User" });
```
<!-- endSnippet -->

**When to use Get(value):**
- Pre-populating repository stub data
- Configuring service dependencies with fixed values
- Setting up DTOs or configuration objects
- Any scenario where the value does not change during the test

---

## Dynamic Getters with Get Callbacks

Use `Get(() => value)` when a property's value should be computed at access time. The callback is invoked on every property read.

<!-- snippet: properties-onget-dynamic -->
```cs
// Get callback returns dynamic value on each access
stub.Timestamp.Get(() => DateTime.UtcNow);
```
<!-- endSnippet -->

Get callbacks can create state-dependent behavior:

<!-- snippet: properties-onget-stateful -->
```cs
// Get checks the tracked state
stub.IsReady.Get(() => isInitialized);
// Initialize method updates the tracked state
stub.Initialize.Call(() => { isInitialized = true; });
```
<!-- endSnippet -->

**Get supports both value and callback syntax:**

<!-- snippet: properties-onget-value-vs-callback -->
```cs
// VALUE: Simple syntax for static values
stub.Name.Get("StaticName");

// CALLBACK: For computed or dynamic values
stub.Age.Get(() => DateTime.Now.Year - 2000);
```
<!-- endSnippet -->

**When to use Get(callback):**
- Values that change over time (timestamps, random values)
- Computed values based on other stub state
- Simulating stateful behavior in dependencies
- Testing race conditions or timing-dependent logic

---

## Setter Interception with Set

Use `Set(callback)` to intercept property writes. This allows tracking values or validating input during tests.

<!-- snippet: properties-onset-tracking -->
```cs
// Set captures every value written to the property
var setValues = new List<string>();
stub.Name.Set((value) => setValues.Add(value));
```
<!-- endSnippet -->

Use `Set` to simulate validation logic in dependencies:

<!-- snippet: properties-onset-validation -->
```cs
// Set throws for invalid values
stub.Age.Set((value) =>
{
    if (value < 0)
        throw new ArgumentException("Age cannot be negative");
});
```
<!-- endSnippet -->

**When to use Set:**
- Tracking all values written to a property
- Simulating validation failures in dependencies
- Testing how your code handles property setter exceptions
- Verifying the sequence of property writes

---

## Verifying Property Access

Property interceptors support verification similar to method interceptors.

### Using VerifyGet

<!-- snippet: properties-verify-getcount -->
```cs
// VerifyGet checks how many times property was read
stub.Age.VerifyGet(Times.Exactly(2));
```
<!-- endSnippet -->

### Using LastSetValue

`LastSetValue` captures the most recent value written to a property:

<!-- snippet: properties-verify-lastsetvalue -->
```cs
// LastSetValue contains the most recent value
Assert.Equal("Expected", stub.Name.LastSetValue);
```
<!-- endSnippet -->

### Verification Methods

| Method | Description |
|--------|-------------|
| `VerifyGet()` | Verify property getter was called at least once (throws if not) |
| `VerifyGet(Times)` | Verify property getter was called according to Times constraint |
| `VerifySet()` | Verify property setter was called at least once (throws if not) |
| `VerifySet(Times)` | Verify property setter was called according to Times constraint |
| `Verify()` | Verify property was accessed (get or set) at least once |
| `Verify(Times)` | Verify total access count (get + set) satisfies Times constraint |

### Inspection Properties

| Property | Description |
|----------|-------------|
| `LastSetValue` | The most recent value written (null/default if never set) |

---

## Using Verifiable() on Properties

Mark properties for batch verification using `Verifiable()`:

<!-- snippet: properties-verifiable -->
```cs
// Mark property as verifiable - requires access before Verify()
stub.Name.Get("test");
stub.Name.Verifiable();
stub.Age.Verifiable();
```
<!-- endSnippet -->

### Verifiable Methods

| Method | Description |
|--------|-------------|
| `Verifiable()` | Mark property (get and set) for batch verification with default constraint (AtLeastOnce) |
| `Verifiable(Times)` | Mark property (get and set) for batch verification with specific Times constraint |

---

## Property Sequences

### Get().ThenGet() for Successive Reads

Use `Get().ThenGet()` when a property should return different values on successive reads.

<!-- snippet: properties-onget-then-sequence -->
```cs
// Get().ThenGet() configures different return values for each read
stub.Name
    .Get(() => "First")
    .ThenGet(() => "Second")
    .ThenGet(() => "Third");
```
<!-- endSnippet -->

The value overload simplifies static sequences:

<!-- snippet: properties-ongetsequence-value -->
```cs
// Get with value, ThenGet elevates to sequence mode
stub.Name.Get("First")
    .ThenGet(() => "Second")
    .ThenGet(() => "Third");
```
<!-- endSnippet -->

### Set().ThenSet() for Successive Writes

Use `Set().ThenSet()` when a property should react differently to successive writes.

<!-- snippet: properties-onset-then-sequence -->
```cs
// Set().ThenSet() configures different callbacks for each write
stub.Name
    .Set((value) => { firstWriteValue = $"First: {value}"; })
    .ThenSet((value) => { secondWriteValue = $"Second: {value}"; });
```
<!-- endSnippet -->

### Verifying Sequences

Sequences support the same verification as regular callbacks:

<!-- snippet: properties-sequence-verification -->
```cs
// Sequences support verification like regular callbacks
var getSequence = stub.Name
    .Get(() => "A")
    .ThenGet(() => "B");

var setSequence = stub.Age
    .Set((v) => { })
    .ThenSet((v) => { });
```
<!-- endSnippet -->

### Sequence Exhaustion Behavior

After a sequence returns all configured values, subsequent reads **repeat the last value** by default. This matches NSubstitute's behavior.

<!-- snippet: properties-sequence-exhaustion -->
```cs
// Configure a sequence - after exhaustion, repeats last value
stub.Name.Get("first")
    .ThenGet("second")
    .ThenGet("third");
```
<!-- endSnippet -->

To return `default(T)` after exhaustion instead, chain `.ThenDefault()`:

<!-- snippet: properties-sequence-thendefault -->
```cs
// ThenDefault() returns default(T) after exhaustion instead of repeating
stub.Name.Get("first")
    .ThenGet("second")
    .ThenDefault();
```
<!-- endSnippet -->

**Note:** In strict mode (`stub.Strict = true`), accessing an exhausted sequence throws `StubException` instead of repeating the last value.

---

## Get Configuration Priority

When you call `Get` multiple times, the last call wins. This applies to both value and callback syntax.

<!-- snippet: properties-priority -->
```cs
// Last Get call wins - can upgrade from value to callback
stub.Name.Get("initial");
stub.Name.Get(() => "dynamic");
```
<!-- endSnippet -->

**Design principle:** This allows reconfiguring property behavior without explicitly clearing the previous configuration first.

---

## Resetting Property Interceptors

Calling `Reset()` on a property interceptor clears tracking state but preserves configured callbacks.

<!-- snippet: properties-reset -->
```cs
// Reset clears counts but preserves callbacks
stub.Name.Reset();

stub.Name.VerifyGet(Times.Never);
stub.Name.VerifySet(Times.Never);
```
<!-- endSnippet -->

### Reset Behavior Summary

Reset() clears:
- Tracking state (get/set counts)
- `LastSetValue`
- Sequence index (resets to beginning)
- Source delegation

Reset() preserves:
- `Get` callbacks (including sequence structure)
- `Set` callbacks (including sequence structure)
- Verifiable marking

After reset, the property retains its configured behavior but tracking counts restart at zero.

---

## User Properties (Stand-Alone Pattern)

When you define a **user property** (override a virtual property with underscore suffix in a Stand-Alone stub), the interceptor uses a clean name (e.g., `Count`, not `Count2`). These interceptors support `Get()` and `Set()` to override the user property per-test.

### Why Use User Properties?

User properties provide several advantages over per-test configuration:

1. **Reusable defaults** - Define once, use across all tests
2. **Computed values** - When you need logic to compute property values
3. **State management** - When you need instance state with backing fields
4. **IDE support** - Full IntelliSense, refactoring, debugging
5. **Compile-time safety** - Signature errors caught by compiler

### Defining User Properties

Override protected virtual properties with the underscore suffix convention to provide default implementations:

<!-- snippet: user-properties-interface-and-stub -->
```cs
public interface ISkillUserSvc
{
    int Count { get; }
    string Name { get; set; }
    string Setting { set; }
}

[KnockOff]
public partial class SkillUserSvcStub : ISkillUserSvc { }

public partial class SkillUserSvcStub
{
    private int _count;
    private string _name = "";
    private string _setting = "";

    // Get-only property override
    protected override int Count_ => _count;

    // Get/set property override
    protected override string Name_
    {
        get => _name;
        set => _name = value;
    }

    // Set-only property override
    protected override string Setting_
    {
        set => _setting = value;
    }

    // Public methods for test setup
    public void SetCount(int value) => _count = value;
}
```
<!-- endSnippet -->

### Using Stubs with User Properties

<!-- snippet: user-properties-basic-usage -->
```cs
var stub = new SkillUserSvcStub();
stub.SetCount(42);

ISkillUserSvc service = stub;

// Get-only property uses your protected override Count_
var count = service.Count;  // 42

// Get/set property uses your protected override Name_
service.Name = "Test";
var name = service.Name;    // "Test"

// Set-only property uses your protected override Setting_
service.Setting = "value";
```
<!-- endSnippet -->

### Get/Set Supersede User Properties

User properties provide shareable defaults. Use `Get()` or `Set()` to override per-test:

<!-- snippet: user-properties-onget-onset-override -->
```cs
var stub = new SkillUserSvcStub();
stub.SetCount(42);

ISkillUserSvc service = stub;

// Default: user property is called
var defaultValue = service.Count;  // 42

// Get supersedes the user property for this test
stub.Count.Get(999);
var overrideValue = service.Count;  // 999

// Set supersedes the user property for this test
var capturedValue = "";
stub.Name.Set(v => capturedValue = $"Captured: {v}");
service.Name = "Test";
// capturedValue == "Captured: Test"
// The user override's backing field was NOT updated
```
<!-- endSnippet -->

### Tracking Works Through User Properties

User property interceptors provide full tracking even when using the user override:

<!-- snippet: user-properties-tracking -->
```cs
var stub = new SkillUserSvcStub();
stub.SetCount(100);

ISkillUserSvc service = stub;

_ = service.Count;
_ = service.Count;
_ = service.Count;

stub.Count.VerifyGet(Times.Exactly(3));
stub.Name.VerifySet(Times.Never);
```
<!-- endSnippet -->

### Reset Preserves Get/Set Configuration

`Reset()` clears tracking state but preserves the Get/Set configuration (matching regular interceptor semantics):

<!-- snippet: user-properties-reset -->
```cs
var stub = new SkillUserSvcStub();
stub.Count.Get(100);  // Override user property

ISkillUserSvc service = stub;
_ = service.Count;
stub.Count.VerifyGet(Times.Once);

// Reset clears tracking but preserves Get
stub.Count.Reset();
stub.Count.VerifyGet(Times.Never);

var value = service.Count;  // 100 (Get still active)
```
<!-- endSnippet -->

### Strict Mode Bypassed for User Properties

User overrides bypass strict mode because they ARE the configuration:

<!-- snippet: user-properties-strict-mode -->
```cs
// [KnockOff(Strict = true)]
// public partial class StrictSkillUserSvcStub : ISkillUserSvc { }
//
// public partial class StrictSkillUserSvcStub
// {
//     protected override int Count_ => 10;  // This IS configured
// }

// Usage:
var stub = new StrictSkillUserSvcStub();
ISkillUserSvc service = stub;

var count = service.Count;  // 10 (no exception - user override is configured)
```
<!-- endSnippet -->

### Supported Patterns

User properties apply to all four standalone patterns:

| Pattern | Attribute | User Property Support |
|---------|-----------|----------------------|
| Standalone | `[KnockOff] class Stub : IService` | Yes |
| Generic Standalone | `[KnockOff] class Stub<T> : IService<T>` | Yes |
| Standalone Class | `[KnockOffBase<SomeClass>] class Stub` | Yes |
| Generic Standalone Class | `[KnockOffBase(typeof(ClassBase<>))] class Stub<T>` | Yes |
| Inline patterns (5-9) | `[KnockOff<...>]` | No (entire class generated) |

### Priority Order

When multiple configurations exist, the priority is:

1. **Get/Set** - Per-test override (highest priority)
2. **User property** - Shared default from protected override
3. **Smart default** - Returns `default(T)` or throws in strict mode

---

## Complete Example

This example demonstrates all property configuration approaches in a realistic test scenario:

<!-- snippet: properties-complete-example -->
```cs
// Get with static value: Fixed test data
stub.CurrentUser.Get(new User { Id = 1, Name = "Alice" });

// Get with callback: State-dependent behavior
stub.IsConnected.Get(() => isConnected);

// Set: Track all values written
var connectionStrings = new List<string>();
stub.ConnectionString.Set((value) => connectionStrings.Add(value));

// Method callback updates the tracked state
stub.Connect.Call(() => { isConnected = true; });
```
<!-- endSnippet -->

---

## Decision Guide

Choose your configuration approach based on the test scenario:

| Scenario | Use This | Example |
|----------|----------|---------|
| Property should return fixed test data | `Get(value)` | `stub.UserId.Get(42);` |
| Property should return current time/random value | `Get(callback)` | `stub.Now.Get(() => DateTime.UtcNow);` |
| Property depends on other stub state | `Get(callback)` | `stub.IsReady.Get(() => isInitialized);` |
| Property should return different values on successive reads | `Get().ThenGet()` | `stub.Name.Get("A").ThenGet("B");` |
| Track all values written to property | `Set` | `stub.Name.Set((v) => list.Add(v));` |
| Simulate validation in dependency | `Set` | `stub.Age.Set((v) => Validate(v));` |
| Property should react differently to successive writes | `Set().ThenSet()` | `stub.Name.Set(cb1).ThenSet(cb2);` |
| Verify property was accessed N times | `VerifyGet` | `stub.UserId.VerifyGet(Times.Exactly(2));` |
| Verify last value written | `LastSetValue` | `Assert.Equal("x", stub.Name.LastSetValue);` |

---

## API Summary

### Configuration Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `Get(T value)` | `IPropertyGetSequence<T>` | Configure getter to return static value. Chain with `.ThenGet()` for sequences. |
| `Get(Func<T> callback)` | `IPropertyGetSequence<T>` | Configure getter with dynamic callback. Chain with `.ThenGet()` for sequences. |
| `Set(Action<T> callback)` | `IPropertySetSequence<T>` | Configure setter callback. Chain with `.ThenSet()` for sequences. |

### Verification Methods

| Method | Description |
|--------|-------------|
| `Verify()` | Verify property was accessed (get or set) at least once |
| `Verify(Times)` | Verify total access count satisfies Times constraint |
| `VerifyGet()` | Verify property getter was called at least once |
| `VerifyGet(Times)` | Verify property getter was called according to Times constraint |
| `VerifySet()` | Verify property setter was called at least once |
| `VerifySet(Times)` | Verify property setter was called according to Times constraint |
| `Verifiable()` | Mark property for batch verification (AtLeastOnce) |
| `Verifiable(Times)` | Mark property for batch verification with specific constraint |

### Inspection Properties

| Property | Type | Description |
|----------|------|-------------|
| `LastSetValue` | `T?` | The value from the most recent setter call (null/default if never set) |

### Utility Methods

| Method | Description |
|--------|-------------|
| `Reset()` | Clears tracking state (get/set counts, LastSetValue, sequence index, source delegation). Preserves Get/Set callbacks and verifiable marking. |

### Sequence Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `ThenGet(T value)` | `IPropertyGetSequence<T>` | Add static value to getter sequence |
| `ThenGet(Func<T> callback)` | `IPropertyGetSequence<T>` | Add callback to getter sequence |
| `ThenSet(Action<T> callback)` | `IPropertySetSequence<T>` | Add callback to setter sequence |
| `ThenDefault()` | `void` | Terminate sequence with default(T) after exhaustion instead of repeating last value |

---

**UPDATED:** 2026-02-06
