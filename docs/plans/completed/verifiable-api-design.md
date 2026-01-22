# Verifiable() API Design

**Date:** 2026-01-18
**Related Todo:** [Verifiable API Enhancement](../todos/completed/verifiable-api.md)
**Status:** Complete
**Completed:** 2026-01-22
**Last Updated:** 2026-01-18 (Developer review fixes: DD8 correction, IsConfigured fix, Reset() clarification, VerifyAll() constraints, GetTypedHandlers() fix, VerificationFailure fix, _isVerifiable clarification, stub-level example code consistency)

---

## Overview

Redesign KnockOff's verification API to separate sequencing from verification, add `.Verifiable()` marking, and align with Moq's patterns. This is a breaking change that will require a major version bump.

---

## Design Decisions

### 1. Separate Sequencing from Verification

**Current:** `Times` is used in `OnCall()` to control both how many times a callback runs AND verification.

**New:** `Times` is only used at verification time. Sequencing is pure - each callback in a sequence runs exactly once.

### 2. Two OnCall Methods - Preserve IMethodTracking Access

**Critical:** Keep `OnCall()` returning `IMethodTracking` for simple cases (preserves `LastArg`/`LastArgs` access). Add `OnCallSequence()` for sequence building.

- `OnCall(callback)` → Returns `IMethodTracking` (callback repeats indefinitely)
- `OnCallSequence(callback)` → Returns `IMethodSequence` (for `ThenCall` chaining)

### 3. Add Verifiable() Marking

Add `.Verifiable()` method to:
- `IMethodTracking` - mark a callback registration
- `IMethodSequence` - mark an entire sequence
- Property interceptors - mark for value/callback access verification
- User-defined method interceptors - mark with optional `Times` constraint

### 4. Verify() Throws Exceptions

All `Verify()` methods throw `VerificationException` instead of returning `bool`. This matches Moq's behavior.

### 5. Method Overloads for Default Times

Use method overloads instead of default parameter:
- `Verify()` - defaults to `Times.AtLeastOnce`
- `Verify(Times times)` - explicit constraint

Exception: `IMethodSequence.Verify()` has no `Times` parameter (verifies sequence exhausted).

### 6. Sequence Verification Semantics

`IMethodSequence.Verify()` verifies the sequence was **exhausted** - all callbacks in the sequence were invoked exactly once each. This is equivalent to "sequence completed."

### 6a. Exhausted Sequence Behavior

When a sequence is exhausted (all callbacks used) but the method is called again:

- **If `Strict = true`:** Throw `StubException.SequenceExhausted`
- **If `Strict = false`:** Return default value (no exception)

This matches the existing strict mode semantics for unconfigured calls.

### 7. Stub.Verify() vs Stub.VerifyAll()

- `Stub.Verify()` - Only checks items marked with `.Verifiable()`
- `Stub.VerifyAll()` - Checks everything that was **stubbed/configured** for the test (NOT all interface members)

### 7a. Definition of "Configured"

An interceptor is **configured** when:
- **Methods:** `_onCall != null || (_sequence?.Count ?? 0) > 0` (has OnCall/OnCallSequence registered)
- **Properties:** Value has been set OR OnGet/OnSet callback registered
- **Events:** Has handlers subscribed

`Reset()` clears counts but **NOT** configuration. A method with `OnCall` registered remains configured after `Reset()`.

**What VerifyAll() checks:**
- All method interceptors that have `OnCall` or `OnCallSequence` configured
- All property interceptors that have `Value` set or `OnGet`/`OnSet` configured
- All indexer interceptors that have been accessed or configured
- All event interceptors that have handlers subscribed
- All user-defined method interceptors (always included - they are implementations)

**What VerifyAll() does NOT check:**
- Interface members that were never configured or accessed
- The stub does not enumerate all interface members - only what was actually stubbed

**VerifyAll() verification constraints:**
- **Methods:** `CallCount >= 1` (called at least once)
- **Properties:** `GetCount + SetCount >= 1` (accessed at least once)
- **Events:** `AddCount >= 1` (subscribed at least once)

### 8. Unified Property Tracking

Property interceptors track **interface access** - `GetCount` and `SetCount` count access through the interface implementation.

**Clarification: Where counts increment (DD8 correction):**
- **`.Value` property** increments counts - this is where tracking happens
- **Interface implementation** delegates to `.Value` - it's a simple pass-through

```csharp
// Interface implementation - just delegates
string IService.Name => _nameInterceptor.Value;

// Value property - this is where tracking happens
public string Value
{
    get { _getCount++; return _onGet?.Invoke(_stub) ?? _value; }
    set { _setCount++; _value = value; _valueSet = true; }
}
```

Single increment point, no double-counting risk. Both test setup (`stub.Name.Value = "Test"`) and interface access (`((IService)stub).Name`) go through the same `.Value` property.

### 9. Aggregate Verification Failures

`VerificationException` collects all failures and throws a single aggregate exception with details for each failure.

### 10. Verifiable() Placement Rules

Where `.Verifiable()` can be called depends on whether methods are user-defined or generated:

| Stub Type | Method Type | Verifiable() Location |
|-----------|-------------|----------------------|
| **Inline** | All generated | `OnCall().Verifiable()` or `OnCallSequence().Verifiable()` |
| **Stand-Alone** | Generated | `OnCall().Verifiable()` or `OnCallSequence().Verifiable()` |
| **Stand-Alone** | User-defined | Interceptor has `Verifiable()` directly (call in constructor) |
| **Mixed overloads** | Both | Use whichever applies to that specific overload |

**Rationale:** User-defined methods have no `OnCall()` to chain from, so they need `Verifiable()` on the interceptor itself.

### 11. Generic Methods - Stub.Verify() Checks All Instantiations

When `stub.Verify()` is called, it verifies ALL type instantiations that were marked `.Verifiable()`:

```csharp
stub.Get.Of<User>().OnCall((id) => user).Verifiable();
stub.Get.Of<Order>().OnCall((id) => order).Verifiable();
stub.Verify();  // Checks BOTH User and Order instantiations
```

Note: There is no `stub.GenericMethod.Verify()`. Verification is done via:
- `IMethodTracking.Verify()` / `IMethodSequence.Verify()` on the return from `OnCall()`/`OnCallSequence()`
- `stub.Verify()` for all `.Verifiable()` items
- `stub.VerifyAll()` for all configured items

### 11a. Generic Method Stub-Level Verification

For `stub.Verify()` and `stub.VerifyAll()` to check generic methods, the interceptor exposes an internal method to iterate over its typed handler dictionary:

```csharp
// In generated generic method interceptor
internal IEnumerable<(Type type, bool isVerifiable, bool isConfigured, Func<VerificationFailure?> checkVerification, Func<VerificationFailure?> checkVerificationAll)> GetTypedHandlers()
{
    foreach (var kvp in _handlers)
    {
        var handler = kvp.Value;
        yield return (kvp.Key, handler.IsVerifiable, handler.IsConfigured, handler.CheckVerification, handler.CheckVerificationAll);
    }
}
```

The stub-level `Verify()` iterates this and calls `checkVerification` to check all instantiations marked verifiable. `VerifyAll()` iterates and calls `checkVerificationAll` to check all configured instantiations.

### 12. Reset() Preserves Verifiable Marking

`Reset()` clears call counts but **preserves** both `_isVerifiable` and `_verifiableTimes` marking. These are configuration state, not tracking state.

A new `OnCall()`/`OnCallSequence()` **clears** the verifiable state (new configuration replaces old).

### 13. OnCall() Clears Verifiable State

When a new `OnCall()` or `OnCallSequence()` is registered, it clears any previous `_isVerifiable` flag. The rationale is that `Verifiable()` is tied to a specific configuration:

```csharp
stub.Method.OnCall(cb1).Verifiable();  // Marked verifiable
stub.Method.OnCall(cb2);               // New config - clears verifiable
stub.Verify();  // cb2 is NOT verifiable
```

**Clarification:** `_isVerifiable` is per-interceptor (not per-tracking-instance). Reconfiguring with `OnCall()` or `OnCallSequence()` clears the verifiable state. This means:
- Configure first, then mark verifiable at end
- Reconfiguring mid-test clears verifiable state (new config = new verification expectations)

**Note:** For generated methods (no user implementation), `Verifiable()` is always accessed via `OnCall().Verifiable()`. See DD20 for user-defined method handling.

### 14. Indexer Interceptors Follow Property Pattern

Indexer interceptors support the same verification API as property interceptors:
- Unified `GetCount`/`SetCount` tracking
- `Verifiable()` and `Verifiable(Times)` methods
- `Verify()` / `Verify(Times)` / `VerifyGet()` / `VerifyGet(Times)` / `VerifySet()` / `VerifySet(Times)`

### 15. Generic Method Verifiable State Location

For generic methods, `_isVerifiable` lives on the **typed handler** (`CreateTypedHandler<T>`), not on the interceptor class. This allows per-instantiation marking:

```csharp
stub.Deserialize.Of<User>().OnCall((json) => user).Verifiable();
stub.Deserialize.Of<Order>().OnCall((json) => order);  // Not verifiable
stub.Verify();  // Only checks User instantiation
```

### 16. Stub Name in Verification Error Messages

Include the stub type name in verification failure messages for clarity when using multiple stubs:

```
Verification failed with 2 error(s):
  - UserRepoStub.GetUser: expected AtLeastOnce, actual 0 calls
  - UserRepoStub.Save: expected Once, actual 3 calls
```

### 17. Event Interceptor Verification

Event interceptors support verification similar to property interceptors:

**Tracking:**
- `AddCount` - Number of times handlers were added via `+=`
- `RemoveCount` - Number of times handlers were removed via `-=`
- `HandlerCount` - Current number of subscribed handlers

**Verification Methods:**
- `Verify()` - Throws if event was never subscribed to (AddCount == 0)
- `Verify(Times times)` - Throws if AddCount doesn't match constraint
- `VerifyAdd()` / `VerifyAdd(Times)` - Verify add operations
- `VerifyRemove()` / `VerifyRemove(Times)` - Verify remove operations
- `Verifiable()` / `Verifiable(Times)` - Mark for Stub.Verify()

**Example:**
```csharp
stub.DataChanged.Verifiable();
sut.Subscribe();
stub.Verify();  // Throws if DataChanged wasn't subscribed
```

### 18. Times.ToString() Override

Add readable `ToString()` for error messages:

```csharp
public override string ToString() => _kind switch
{
    TimesKind.Exactly => _count == 1 ? "Once" : _count == 2 ? "Twice" : $"Exactly({_count})",
    TimesKind.AtLeast => _count == 1 ? "AtLeastOnce" : $"AtLeast({_count})",
    TimesKind.AtMost => $"AtMost({_count})",
    TimesKind.Never => "Never",
    _ => "Unknown"
};
```

### 19. Verifiable() Returns Fluent Type

`Verifiable()` methods return the object for fluent chaining (matches Moq pattern):

```csharp
// IMethodTracking
IMethodTracking Verifiable();
IMethodTracking Verifiable(Times times);

// IMethodSequence
IMethodSequence Verifiable();

// Property/Indexer/Event interceptors
TInterceptor Verifiable();
TInterceptor Verifiable(Times times);
```

**Example:**
```csharp
stub.GetUser.OnCall(cb).Verifiable().Reset();  // Chain continues
```

### 20. Verifiable() on Interceptors - User-Defined vs Generated Methods

`Verifiable()` **exists on all method interceptors**, but behavior differs:

**User-defined methods (stand-alone stubs):**
- `stub.Save.Verifiable()` works - marks for verification
- Used when there's a user implementation backing the method

**Generated methods (no user implementation):**
- `stub.Method.Verifiable()` throws `InvalidOperationException`:
  ```
  "Use OnCall().Verifiable() for generated methods. Verifiable() on the interceptor is only valid for user-defined methods."
  ```
- Must use `stub.Method.OnCall(cb).Verifiable()` instead

**Implementation:** The interceptor tracks whether it has a user-defined backing method. `Verifiable()` checks this flag and throws if false.

```csharp
// Generated method interceptor
private readonly bool _hasUserDefinedMethod;

public MethodInterceptor Verifiable()
{
    if (!_hasUserDefinedMethod)
        throw new InvalidOperationException(
            "Use OnCall().Verifiable() for generated methods. " +
            "Verifiable() on the interceptor is only valid for user-defined methods.");
    _isVerifiable = true;
    return this;
}
```

This provides a clear runtime error guiding users to the correct pattern.

### 21. Typed Tracking Interfaces Include Verifiable()

The typed interfaces `IMethodTracking<TArg>` and `IMethodTrackingArgs<TArgs>` must also include `Verifiable()` methods returning their own type for proper fluent chaining:

```csharp
public interface IMethodTracking<TArg> : IMethodTracking
{
    TArg LastArg { get; }
    new IMethodTracking<TArg> Verifiable();
    new IMethodTracking<TArg> Verifiable(Times times);
}

public interface IMethodTrackingArgs<TArgs> : IMethodTracking
{
    TArgs LastArgs { get; }
    new IMethodTrackingArgs<TArgs> Verifiable();
    new IMethodTrackingArgs<TArgs> Verifiable(Times times);
}
```

This enables:
```csharp
var tracking = stub.GetUser.OnCall((id) => user).Verifiable();
var lastId = tracking.LastArg;  // Still has access to LastArg
```

### 22. Source() and Verifiable() are Independent

`Source()` and verification (`Verifiable()`, `Verify()`, `VerifyAll()`) are completely independent concerns:

- **Source()** = Test setup - "What do I need for my test to succeed?"
- **Verify()** = Test assertion - "What am I actually testing?"

Setting `Source()` has no effect on verification. Only explicitly marked `.Verifiable()` items are checked:

```csharp
stub.Source(realImplementation);  // Provides fallback behavior
stub.GetUser.OnCall((id) => user).Verifiable();  // This is what we're testing

// Later...
stub.Verify();  // Only checks the OnCall().Verifiable() - Source() is ignored
```

**Rationale:** Source provides default/fallback behavior (making tests work). Verifiable marks what you explicitly want to verify was called (what you're testing). These serve different purposes and don't need to interact.

---

## Interface Changes

### IMethodTracking

```csharp
/// <summary>
/// Tracks invocations of a method callback registration.
/// </summary>
public interface IMethodTracking
{
    /// <summary>Number of times this callback was invoked.</summary>
    int CallCount { get; }

    /// <summary>True if CallCount > 0.</summary>
    bool WasCalled { get; }

    /// <summary>Clears tracking state for this registration.</summary>
    void Reset();

    /// <summary>
    /// Verifies the callback was invoked at least once.
    /// Throws VerificationException if not satisfied.
    /// </summary>
    void Verify();

    /// <summary>
    /// Verifies the callback was invoked according to the Times constraint.
    /// Throws VerificationException if not satisfied.
    /// </summary>
    /// <param name="times">The Times constraint to verify against.</param>
    void Verify(Times times);

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify().
    /// Returns this for fluent chaining.
    /// </summary>
    IMethodTracking Verifiable();

    /// <summary>
    /// Marks this callback registration for verification by Stub.Verify()
    /// with a specific Times constraint. Returns this for fluent chaining.
    /// </summary>
    /// <param name="times">The Times constraint to verify against.</param>
    IMethodTracking Verifiable(Times times);
}

/// <summary>
/// Tracks invocations with single argument capture.
/// </summary>
public interface IMethodTracking<TArg> : IMethodTracking
{
    /// <summary>Last argument passed to this callback. Default if never called (check WasCalled).</summary>
    TArg LastArg { get; }

    /// <summary>Marks for verification. Returns this for fluent chaining with LastArg access.</summary>
    new IMethodTracking<TArg> Verifiable();

    /// <summary>Marks for verification with Times constraint. Returns this for fluent chaining.</summary>
    new IMethodTracking<TArg> Verifiable(Times times);
}

/// <summary>
/// Tracks invocations with multiple argument capture as named tuple.
/// </summary>
public interface IMethodTrackingArgs<TArgs> : IMethodTracking
{
    /// <summary>Last arguments passed as named tuple. Default if never called (check WasCalled).</summary>
    TArgs LastArgs { get; }

    /// <summary>Marks for verification. Returns this for fluent chaining with LastArgs access.</summary>
    new IMethodTrackingArgs<TArgs> Verifiable();

    /// <summary>Marks for verification with Times constraint. Returns this for fluent chaining.</summary>
    new IMethodTrackingArgs<TArgs> Verifiable(Times times);
}
```

### IMethodSequence

```csharp
/// <summary>
/// Represents a sequence of method callbacks.
/// Returned by OnCall() to enable ThenCall chaining.
/// </summary>
public interface IMethodSequence
{
    /// <summary>Total calls across all callbacks in sequence.</summary>
    int TotalCallCount { get; }

    /// <summary>
    /// Verifies the entire sequence was executed (all callbacks invoked).
    /// Throws VerificationException if sequence incomplete.
    /// </summary>
    void Verify();

    /// <summary>Reset all tracking in the sequence.</summary>
    void Reset();

    /// <summary>
    /// Marks this sequence for verification by Stub.Verify().
    /// The sequence must complete (all callbacks invoked) to pass.
    /// Returns this for fluent chaining.
    /// </summary>
    IMethodSequence Verifiable();
}

/// <summary>
/// Typed sequence that enables ThenCall chaining.
/// </summary>
public interface IMethodSequence<TCallback> : IMethodSequence
{
    /// <summary>Marks this sequence for verification. Returns this for fluent chaining.</summary>
    new IMethodSequence<TCallback> Verifiable();
    /// <summary>
    /// Adds another callback to the sequence.
    /// Each callback in the sequence is invoked exactly once.
    /// </summary>
    IMethodSequence<TCallback> ThenCall(TCallback callback);
}
```

### Times Struct Changes

Remove `Times.Forever` (no longer needed for sequencing).

Keep:
- `Times.Once` - exactly 1 call
- `Times.Twice` - exactly 2 calls
- `Times.Exactly(n)` - exactly n calls
- `Times.AtLeast(n)` - at least n calls
- `Times.AtLeastOnce` - at least 1 call
- `Times.AtMost(n)` - at most n calls
- `Times.Never` - exactly 0 calls

### VerificationException

```csharp
/// <summary>
/// Thrown when verification fails. Contains all failures when multiple exist.
/// </summary>
public class VerificationException : Exception
{
    /// <summary>All verification failures that occurred.</summary>
    public IReadOnlyList<VerificationFailure> Failures { get; }

    // ... constructors
}

/// <summary>
/// Details about a single verification failure.
/// </summary>
public class VerificationFailure
{
    /// <summary>The member that failed verification.</summary>
    public string Member { get; }

    /// <summary>The expected Times constraint.</summary>
    public Times Expected { get; }

    /// <summary>The actual call count.</summary>
    public int Actual { get; }

    /// <summary>Human-readable failure message.</summary>
    public string Message { get; }

    // Standard constructor for Times-based failures
    public VerificationFailure(string member, Times expected, int actual)
    {
        Member = member;
        Expected = expected;
        Actual = actual;
        Message = $"{member}: expected {expected}, actual {actual} calls";
    }

    // Constructor for sequence failures
    public VerificationFailure(string member, int sequenceLength, int completedCount)
    {
        Member = member;
        Expected = Times.Exactly(sequenceLength);
        Actual = completedCount;
        Message = $"{member}: sequence incomplete - {completedCount} of {sequenceLength} callbacks invoked";
    }
}

```

The `Message` property and `ToString()` will include a formatted summary of all failures (including stub type name):
```
Verification failed with 2 error(s):
  - UserRepoStub.GetUser: expected AtLeastOnce, actual 0 calls
  - UserRepoStub.Save: expected Once, actual 3 calls
```

**Note:** This is a breaking change from the previous `VerificationException` which had `Member`, `Expected`, `Actual` properties directly. Those are removed in favor of the `Failures` collection.

### Internal Verification Interface

Generated interceptors implement an internal interface pattern for stub-level verification. This is NOT a public interface - it's a code generation pattern.

**Required members on each generated interceptor:**

```csharp
// Method interceptor (generated or user-defined)
internal bool IsVerifiable => _isVerifiable;
internal bool IsConfigured => _onCall != null || (_sequence?.Count ?? 0) > 0;
internal VerificationFailure? CheckVerification()
{
    if (!_isVerifiable) return null;
    var times = _verifiableTimes ?? Times.AtLeastOnce;
    return times.Validate(CallCount) ? null : new VerificationFailure(MemberName, times, CallCount);
}
internal VerificationFailure? CheckVerificationAll()
{
    if (!IsConfigured) return null;
    return Times.AtLeastOnce.Validate(CallCount) ? null : new VerificationFailure(MemberName, Times.AtLeastOnce, CallCount);
}

// Property interceptor
internal bool IsVerifiable => _isVerifiable;
internal bool IsConfigured => _valueSet || _onGet != null || _onSet != null;
internal VerificationFailure? CheckVerification() { /* similar pattern */ }
internal VerificationFailure? CheckVerificationAll()
{
    if (!IsConfigured) return null;
    // Properties: GetCount + SetCount >= 1 (accessed at least once)
    var totalAccess = GetCount + SetCount;
    return totalAccess >= 1 ? null : new VerificationFailure(MemberName, Times.AtLeastOnce, totalAccess);
}

// Event interceptor
internal bool IsVerifiable => _isVerifiable;
internal bool IsConfigured => _handlers.Count > 0;
internal VerificationFailure? CheckVerification() { /* similar pattern */ }
internal VerificationFailure? CheckVerificationAll()
{
    if (!IsConfigured) return null;
    // Events: AddCount >= 1 (subscribed at least once)
    return AddCount >= 1 ? null : new VerificationFailure(MemberName, Times.AtLeastOnce, AddCount);
}
```

**Stub-level methods use these:**
```csharp
public void Verify()
{
    var failures = new List<VerificationFailure>();
    if (GetUser.IsVerifiable && GetUser.CheckVerification() is { } f1) failures.Add(f1);
    if (Name.IsVerifiable && Name.CheckVerification() is { } f2) failures.Add(f2);
    // ... etc
    if (failures.Count > 0) throw new VerificationException(failures);
}

public void VerifyAll()
{
    var failures = new List<VerificationFailure>();
    if (GetUser.IsConfigured && GetUser.CheckVerificationAll() is { } f1) failures.Add(f1);
    if (Name.IsConfigured && Name.CheckVerificationAll() is { } f2) failures.Add(f2);
    // ... etc
    if (failures.Count > 0) throw new VerificationException(failures);
}
```

---

## Generated Code Changes

### Method Interceptor

**OnCall methods - keep both patterns:**
```csharp
// Current
public IMethodTracking<T> OnCall(Delegate callback);
public IMethodSequence<Delegate> OnCall(Delegate callback, Times times);

// New - two methods, no Times
/// <summary>
/// Configures callback that repeats indefinitely. Returns tracking for LastArg access.
/// </summary>
public IMethodTracking<T> OnCall(Delegate callback);

/// <summary>
/// Starts a callback sequence. Returns sequence for ThenCall chaining.
/// Each callback in sequence runs exactly once.
/// </summary>
public IMethodSequence<Delegate> OnCallSequence(Delegate callback);
```

**ThenCall signature change:**
```csharp
// Current
IMethodSequence<Delegate> ThenCall(Delegate callback, Times times);

// New - no Times (each callback runs once)
IMethodSequence<Delegate> ThenCall(Delegate callback);
```

**Invoke method - Sequence vs OnCall storage:**

The interceptor maintains separate storage for the two patterns:
- `_onCall` - Single callback that repeats indefinitely (from `OnCall()`)
- `_sequence` - List of callbacks that each run once (from `OnCallSequence()`)

```csharp
private Func<TStub, TArgs, TReturn>? _onCall;        // Repeating callback
private List<Func<TStub, TArgs, TReturn>>? _sequence; // One-shot sequence
private int _sequenceIndex;

internal TReturn Invoke(TStub stub, TArgs args)
{
    CallCount++;

    // Sequence takes priority if present and not exhausted
    if (_sequence != null && _sequenceIndex < _sequence.Count)
    {
        return _sequence[_sequenceIndex++].Invoke(stub, args);
    }

    // Fall back to repeating OnCall callback
    if (_onCall != null)
    {
        return _onCall.Invoke(stub, args);
    }

    // No configuration - strict mode check or default
    if (stub.Strict)
        throw new StubException($"No callback configured for {MemberName}");
    return default!;
}
```

When `OnCallSequence()` is called, it clears `_onCall` and initializes `_sequence`. When `OnCall()` is called, it clears `_sequence` and sets `_onCall`.

**Add Verifiable tracking (fluent):**
```csharp
private bool _isVerifiable;
private Times? _verifiableTimes;  // null means AtLeastOnce

/// <summary>Marks for verification with AtLeastOnce constraint. Returns this for fluent chaining.</summary>
public IMethodTracking Verifiable() { _isVerifiable = true; return this; }

/// <summary>Marks for verification with specific Times constraint. Returns this for fluent chaining.</summary>
public IMethodTracking Verifiable(Times times) { _isVerifiable = true; _verifiableTimes = times; return this; }

// Used by stub-level Verify()
internal bool IsVerifiable => _isVerifiable;
internal Times VerifiableTimes => _verifiableTimes ?? Times.AtLeastOnce;
```

### Property Interceptor

**Unified tracking - `.Value` is where counts increment (DD8 correction):**
```csharp
private int _getCount;
private int _setCount;
private string _value = default!;
private bool _valueSet;
private readonly TStub _stub;

/// <summary>Total gets (both test setup and interface access).</summary>
public int GetCount => _getCount;

/// <summary>Total sets (both test setup and interface access).</summary>
public int SetCount => _setCount;

// Value property - this is where tracking happens
public string Value
{
    get { _getCount++; return _onGet?.Invoke(_stub) ?? _value; }
    set { _setCount++; _value = value; _valueSet = true; }
}

// Interface implementation - just delegates
string IService.Name => _nameInterceptor.Value;
```

**Note:** `_valueSet` tracks whether configuration was provided (for `IsConfigured` check in VerifyAll). Single increment point in `.Value` ensures no double-counting risk.

**Add Verifiable (fluent):**
```csharp
private bool _isVerifiable;
private Times? _verifiableTimes;

public MyPropertyInterceptor Verifiable() { _isVerifiable = true; return this; }
public MyPropertyInterceptor Verifiable(Times times) { _isVerifiable = true; _verifiableTimes = times; return this; }
```

**Property Verification Semantics:**
- `property.Verify()` - Throws if `GetCount + SetCount == 0` (never accessed)
- `property.Verify(Times.Once)` - Throws if `GetCount + SetCount != 1`
- `property.VerifyGet()` / `property.VerifyGet(Times)` - Verify getter access only
- `property.VerifySet()` / `property.VerifySet(Times)` - Verify setter access only
- Unified counting means no distinction between OnGet/OnSet callbacks and Value access

### User-Defined Method Interceptor

**Add Verifiable with Times (fluent):**
```csharp
private bool _isVerifiable;
private Times _verifiableTimes = Times.AtLeastOnce;

/// <summary>Marks for verification with AtLeastOnce constraint. Returns this for fluent chaining.</summary>
public SaveInterceptor Verifiable() { _isVerifiable = true; return this; }

/// <summary>Marks for verification with specific Times constraint. Returns this for fluent chaining.</summary>
public SaveInterceptor Verifiable(Times times) { _isVerifiable = true; _verifiableTimes = times; return this; }
```

### Stub-Level Methods

```csharp
/// <summary>
/// Verifies all members marked with .Verifiable() were invoked as expected.
/// Throws VerificationException with all failures if any fail.
/// </summary>
public void Verify()
{
    var failures = new List<VerificationFailure>();

    // Check verifiable method interceptors
    if (GetUser.IsVerifiable && GetUser.CheckVerification() is { } f1)
        failures.Add(f1);

    // Check verifiable property interceptors
    if (Name.IsVerifiable && Name.CheckVerification() is { } f2)
        failures.Add(f2);

    // ... etc

    if (failures.Count > 0)
        throw new VerificationException(failures);
}

/// <summary>
/// Verifies ALL configured members were invoked at least once.
/// Includes all interceptors with OnCall/OnCallSequence, Value set, or callbacks registered.
/// Throws VerificationException with all failures if any fail.
/// </summary>
public void VerifyAll()
{
    var failures = new List<VerificationFailure>();

    // Check ALL configured method interceptors
    if (GetUser.IsConfigured && GetUser.CheckVerificationAll() is { } f1)
        failures.Add(f1);

    // Check ALL configured property interceptors (including Value access)
    if (Name.IsConfigured && Name.CheckVerificationAll() is { } f2)
        failures.Add(f2);

    // Check ALL user-defined method interceptors (always configured)
    if (Save.CheckVerificationAll() is { } f3)
        failures.Add(f3);

    // ... etc

    if (failures.Count > 0)
        throw new VerificationException(failures);
}
```

---

## Usage Examples

### Basic Setup and Verification

```csharp
// Setup - OnCall() repeats indefinitely, returns IMethodTracking
var tracking = stub.GetUser.OnCall((id) => user);

// Execute
var result = sut.GetUserById(1);

// Access tracking info
Assert.Equal(1, tracking.LastArg);  // LastArg available!

// Verify with Times at verification time
tracking.Verify(Times.Once);
```

### Using Verifiable Pattern

```csharp
// Mark as verifiable - works with both OnCall and OnCallSequence
stub.GetUser.OnCall((id) => user).Verifiable();
stub.Save.OnCall((entity) => entity).Verifiable();

// Execute
sut.DoWork();

// Verify only .Verifiable() items
stub.Verify();  // Throws if GetUser or Save not called
```

### Sequencing

```csharp
// OnCallSequence() for ThenCall chaining - each callback runs once
stub.GetUser
    .OnCallSequence(((id) => user1)
    .ThenCall(((id) => user2)
    .ThenCall(((id) => user3)
    .Verifiable();

// First call returns user1, second user2, third user3
sut.ProcessUsers();

// Verify sequence completed (all 3 callbacks invoked)
stub.Verify();
```

### Property Verification

```csharp
stub.ConnectionString.Value = "Server=localhost";
stub.ConnectionString.Verifiable();

sut.Connect();

stub.Verify();  // Throws if ConnectionString.Value not read
```

### User-Defined Methods

```csharp
[KnockOff<IUserRepo>]
partial class UserRepoStub
{
    public UserRepoStub()
    {
        // Mark user-defined method as must be called exactly twice
        Save.Verifiable(Times.Exactly(2));
    }

    protected User Save(UserRepoStub ko, User entity) => entity;
}

// In test
var stub = new UserRepoStub();
sut.SaveUsers(users);
stub.Verify();  // Throws if Save not called exactly twice
```

### VerifyAll

```csharp
// Configure stub
stub.GetUser.OnCall((id) => user);
stub.Name.Value = "Test";

// Execute
sut.DoWork();

// Verify everything
stub.VerifyAll();  // Checks GetUser, Name (Value access), all user methods
```

---

## Migration Guide

### Breaking Changes

1. **Sequencing uses OnCallSequence() instead of OnCall(cb, Times):**
   ```csharp
   // Before
   stub.Method.OnCall(cb, Times.Once).ThenCall(cb2, Times.Forever);

   // After - use OnCallSequence for chaining
   stub.Method.OnCallSequence(cb).ThenCall(cb2);

   // Simple repeating callback - OnCall() unchanged
   stub.Method.OnCall(cb);  // Repeats indefinitely, returns IMethodTracking
   ```

2. **ThenCall no longer takes Times:**
   ```csharp
   // Before
   stub.Method.OnCall(cb, Times.Once).ThenCall(cb2, Times.Twice);

   // After - each callback runs once, repeat if needed
   stub.Method.OnCallSequence(cb).ThenCall(cb2).ThenCall(cb2);
   ```

3. **Verify() no longer returns bool:**
   ```csharp
   // Before
   Assert.True(tracking.Verify(Times.Once));

   // After
   tracking.Verify(Times.Once);  // Throws on failure
   ```

4. **Stub.Verify() only checks Verifiable items:**
   ```csharp
   // Before - checked all OnCall configurations
   stub.Verify();

   // After - only checks .Verifiable() items
   stub.Method.OnCall(cb).Verifiable();
   stub.Verify();

   // Or use VerifyAll() for previous behavior
   stub.VerifyAll();
   ```

5. **Times.Forever removed:**
   ```csharp
   // Before
   stub.Method.OnCall(cb, Times.Forever);

   // After - just use OnCall (repeats indefinitely by default)
   stub.Method.OnCall(cb);
   ```

---

## Implementation Steps

1. Update `Times` struct:
   - Remove `Forever`
   - Add `ToString()` override for readable error messages
2. Update `IMethodTracking` interface:
   - Change `Verify()` to return `void` and throw
   - Add `Verify()` overload (no params, defaults to AtLeastOnce)
   - Add `Verifiable()` and `Verifiable(Times)` returning `IMethodTracking` (fluent)
2a. Update typed tracking interfaces:
   - `IMethodTracking<TArg>` - Add `Verifiable()` / `Verifiable(Times)` returning `IMethodTracking<TArg>`
   - `IMethodTrackingArgs<TArgs>` - Add `Verifiable()` / `Verifiable(Times)` returning `IMethodTrackingArgs<TArgs>`
3. Update `IMethodSequence` interface:
   - Change `Verify()` to return `void` and throw
   - Remove `Times` from `ThenCall()`
   - Add `Verifiable()` returning `IMethodSequence` (fluent)
4. Create `VerificationFailure` class with both constructors setting `Message` property
5. Update `VerificationException` for aggregation (BREAKING: remove Member/Expected/Actual)
6. Update `MethodInterceptorRenderer`:
   - Keep `OnCall()` returning `IMethodTracking` (repeats indefinitely)
   - Add `OnCallSequence()` returning `IMethodSequence`
   - Remove `Times` from `ThenCall`
   - Add `Verifiable()` and `Verifiable(Times)` returning `this` (fluent)
   - Add `_isVerifiable` and `_verifiableTimes` fields
   - Change `Verify()` to throw
7. Update property interceptor generation:
   - `.Value` increments counts, interface implementation delegates to `.Value` (DD8)
   - Track Value access in getter/setter
   - Add `Verifiable()` and `Verifiable(Times)` returning `this` (fluent)
   - Add `VerifyGet()`/`VerifyGet(Times)`/`VerifySet()`/`VerifySet(Times)`
   - Change `Verify()` to throw
8. Update event interceptor generation:
   - Add `AddCount`, `RemoveCount`, `HandlerCount` tracking
   - Add `Verify()`/`Verify(Times)` for subscription verification
   - Add `VerifyAdd()`/`VerifyAdd(Times)`/`VerifyRemove()`/`VerifyRemove(Times)`
   - Add `Verifiable()`/`Verifiable(Times)` returning `this` (fluent)
9. Update user-defined method interceptors:
   - Add `Verifiable()` and `Verifiable(Times)` returning `this` (fluent)
   - Change `Verify()` to throw
10. Update stub-level generation:
    - `Verify()` checks only `.Verifiable()` items
    - `VerifyAll()` checks everything that was stubbed/configured
    - Both aggregate failures and throw single exception
    - `Source()` is independent - doesn't affect verification (DD22)
11. Update all three renderers (Flat, Inline, Class)
12. Update existing tests for new API
13. Add new tests for:
    - `Verifiable()` behavior (including fluent chaining)
    - `OnCallSequence()` + `ThenCall()` sequencing
    - Aggregate `VerificationException`
    - Property `Value` tracking (unified counts)
    - Event subscription verification
    - Generic method verification (all instantiations)
    - `Times.ToString()` output
    - `OnCall().Verifiable()` then `OnCall()` again clears verifiable (DD13)
    - Property `Value` access interleaved with `OnGet` callbacks
    - `VerifyAll()` with mixed configured/unconfigured members
    - Generic method with multiple type instantiations (some verifiable, some not)
    - Indexer interceptor verification
    - `Reset()` preserves `Verifiable` marking (DD12)
    - Aggregate exception formatting with multiple failures
    - Stand-alone stub with user-defined methods marked `Verifiable()`
    - Verifiable on void methods
    - Verifiable with ref/out parameters
    - Verifiable with nullable return types
    - `Source()` with `Verifiable()` - only marked items verified, source ignored (DD22)
    - Typed tracking interfaces (`IMethodTracking<TArg>`, `IMethodTrackingArgs<TArgs>`) fluent chaining
14. Update documentation and migration guide

---

## Risks / Considerations

1. **Breaking Change:** This is a major API change requiring version bump
2. **Migration Effort:** Existing tests using `Times` in `OnCall` need updating
3. **Sequence Repetition:** Users wanting callbacks used multiple times must duplicate `ThenCall`
4. **Value Tracking Overhead:** Tracking every `.Value` access adds minimal runtime cost

