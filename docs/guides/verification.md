# Verification Guide

After configuring stub behavior, you need to verify that your code under test interacted with the stub correctly. KnockOff provides a fluent verification API inspired by Moq, plus lower-level properties for custom assertions.

---

## Quick Start

KnockOff offers three verification approaches:

1. **Direct verification** - Call `.Verify()` on individual interceptors
2. **Marked verification** - Use `.Verifiable()` to mark expected calls, then `stub.Verify()`
3. **Verify all** - Call `stub.VerifyAll()` to check everything configured

**Recommended:** Use `.Verifiable()` + `stub.Verify()` for most tests. It's explicit, readable, and catches missing verifications.

---

## What You Can Verify

KnockOff enables verification of:

- **Calls** - Whether a method or property was invoked
- **Call frequency** - Exactly once, at least N times, never, etc.
- **Arguments** - What values were passed to methods
- **State** - Property get/set operations and final values
- **Order** - The sequence of calls across multiple methods

---

## Direct Verification

Call `.Verify()` directly on interceptors returned by `OnCall`. This approach is concise when you only need to verify one or two calls.

### At Least Once (Default)

The simplest verification checks whether a method was invoked at least once.

<!-- snippet: verify-verifiable -->
```cs
// Mark for batch verification, then verify all marked members
stub.GetById.OnCall((id) => new User { Id = id }).Verifiable();

IRepoVerify repository = stub;
repository.GetById(42);

stub.Verify();
```
<!-- endSnippet -->

### Exactly Once

Verify a method was called exactly once.

<!-- snippet: verify-times-once -->
```cs
// Verify exactly one call
tracking.Verify(Times.Once);
```
<!-- endSnippet -->

### At Least N Calls

Verify a method was called a minimum number of times.

<!-- snippet: verify-times-atleast -->
```cs
// Verify at least N calls
tracking.Verify(Times.AtLeast(2));
```
<!-- endSnippet -->

### Never Called

Verify a method was never invoked.

<!-- snippet: verify-times-never -->
```cs
// Verify method was never called
tracking.Verify(Times.Never);
```
<!-- endSnippet -->

### All Times Matchers

The `Times` struct supports these verification modes:

- `Times.AtLeastOnce` - Default, at least one call
- `Times.Once` - Exactly one call
- `Times.Twice` - Exactly two calls
- `Times.Exactly(n)` - Exactly N calls
- `Times.AtLeast(n)` - At least N calls
- `Times.AtMost(n)` - At most N calls
- `Times.Never` - Zero calls

**Note:** For most scenarios, use `Times.Once`, `Times.AtLeast(n)`, or `Times.Never`.

---

## Marked Verification (Recommended)

Use `.Verifiable()` to mark interceptors as requiring verification, then call `stub.Verify()` to check them all at once. This approach prevents "missing verification" bugs where you forget to check a critical call.

### Basic Marked Verification

The `verify-verifiable` example in the Direct Verification section demonstrates this pattern. Chain `.Verifiable()` on the interceptor returned by `OnCall`, then call `stub.Verify()` to check all marked members at once.

### Verifiable with Times

You can specify `Times` constraints when marking with `.Verifiable()`.

<!-- snippet: verify-verifiable-times -->
```cs
// Mark with Times constraint for batch verification
stub.Refresh.OnCall(() => { }).Verifiable(Times.Exactly(2));
```
<!-- endSnippet -->

### When to Use Marked Verification

**Prefer `.Verifiable()` + `stub.Verify()` when:**
- You have multiple method calls to verify
- You want to ensure you don't forget verification
- You want verification failures to clearly list what wasn't called

**Use direct `.Verify()` when:**
- You only need to check one or two calls
- The verification logic is complex (argument inspection, etc.)

---

## Verify All

Call `stub.VerifyAll()` to check every interceptor that has `OnCall` or `Value` configured, regardless of whether it was marked `.Verifiable()`.

<!-- snippet: verify-verifyall -->
```cs
// VerifyAll checks all configured members were called at least once
stub.VerifyAll();
```
<!-- endSnippet -->

**Use `VerifyAll()` when:**
- You want strict verification that everything configured was used
- You're testing integration scenarios where all dependencies should be touched

**Warning:** `VerifyAll()` can be brittle. If you configure a callback for optional behavior, `VerifyAll()` will fail if it's not called.

---

## Argument Verification

For argument inspection, use `LastCallArg` or `LastCallArgs` from the tracking object returned by `OnCall`.

### Single Parameter (LastCallArg)

<!-- snippet: verify-lastcallarg -->
```cs
// LastArg contains the most recent argument value
Assert.Equal(42, tracking.LastArg);
```
<!-- endSnippet -->

### Multiple Parameters (LastCallArgs)

<!-- snippet: verify-lastcallargs-tuple -->
```cs
// LastArgs is a named tuple for multi-parameter methods
var (id, name) = tracking.LastArgs;
Assert.Equal(42, id);
Assert.Equal("Alice", name);
```
<!-- endSnippet -->

---

## Call Count Inspection

For scenarios where you need to track call counts for custom logic, capture the count in your callback.

<!-- snippet: verify-callcount-tracking -->
```cs
// Track call count in the callback for custom assertions
var saveCount = 0;
stub.Save.OnCall((user) => { saveCount++; });
```
<!-- endSnippet -->

**When to use tracked counts:**
- Custom verification logic that doesn't fit `Times` matchers
- Relative comparisons between multiple methods
- Debugging to understand call patterns

**Prefer `.Verify(Times)` when possible** - it provides clearer error messages and aligns with the verification pattern.

---

## Call History Tracking

For complex scenarios requiring inspection of all calls (not just the last), use `OnCall` callbacks to capture a complete history.

<!-- snippet: verify-call-history -->
```cs
// Capture all calls to a list for history inspection
var calls = new List<int>();
stub.GetById.OnCall((id) =>
{
    calls.Add(id);
    return new User { Id = id };
});
```
<!-- endSnippet -->

This pattern works for any verification need beyond "last call" inspection.

---

## Call Order Verification

To verify that methods were called in a specific sequence, track call order using shared state.

<!-- snippet: verify-call-order -->
```cs
// Track call order with a shared counter
var order = 0;
var saveOrder = 0;
var refreshOrder = 0;

stub.Save.OnCall((user) => saveOrder = ++order);
stub.Refresh.OnCall(() => refreshOrder = ++order);
```
<!-- endSnippet -->

This approach scales to any number of methods and supports complex ordering assertions.

---

## Cross-Interceptor Verification

Verify multiple methods were called using `.Verifiable()` and `stub.Verify()`.

<!-- snippet: verify-cross-interceptor -->
```cs
// Mark multiple methods as verifiable
stub.GetById.OnCall((id) => new User { Id = id }).Verifiable();
stub.Save.OnCall((user) => { }).Verifiable();
stub.Refresh.OnCall(() => { }).Verifiable();
```
<!-- endSnippet -->

This approach is cleaner than individual assertions and catches missing verifications.

---

## Property Verification

Properties expose separate verification methods for get and set operations.

**Get verification:**
- `VerifyGet()` / `VerifyGet(Times)` - Verify get access count

**Set verification:**
- `LastSetValue` - The most recent value assigned to the property
- `VerifySet()` / `VerifySet(Times)` - Verify set access count

**Combined verification:**
- `Verify()` / `Verify(Times)` - Verify total access count (get + set)

### Property Get Verification

Verify that a property was read the expected number of times.

<!-- snippet: verify-property-get -->
```cs
// VerifyGet checks how many times property was read
stub.MaxRetries.VerifyGet(Times.Exactly(2));
```
<!-- endSnippet -->

### Property Set Verification

Verify that a property was written and inspect the assigned value.

<!-- snippet: verify-property-set -->
```cs
// VerifySet checks property was written
stub.Timeout.VerifySet(Times.Once);

// LastSetValue contains the assigned value
Assert.Equal(30, stub.Timeout.LastSetValue);
```
<!-- endSnippet -->

### Property Combined Verification

Verify total property access across both get and set operations.

<!-- snippet: verify-property-combined -->
```cs
// Verify checks combined get + set count (2 gets + 2 sets = 4)
stub.MaxRetries.Verify(Times.Exactly(4));
```
<!-- endSnippet -->

---

## Complete Example

Here's a comprehensive verification scenario demonstrating the recommended patterns.

<!-- snippet: verify-complete-example -->
```cs
// 1. Batch verification - checks all Times constraints
stub.Verify();

// 2. Argument verification via tracking
Assert.Equal(2, getTracking.LastArg);

// 3. Call history verification
Assert.Equal(new[] { 1, 2 }, getIdHistory);

// 4. Call order verification
Assert.True(getOrder < saveOrder, "Get before Save");
```
<!-- endSnippet -->

This example shows the modern verification approach: mark what matters with `.Verifiable()`, verify with `stub.Verify()`, and add detailed assertions only when needed.

---

## When Verification Fails

When verification fails, KnockOff throws an exception with a clear message indicating what went wrong.

**Common failure scenarios:**

- **Not called enough times**: "Expected method GetById to be called Times.Once, but was called 0 times"
- **Called too many times**: "Expected method Save to be called Times.Once, but was called 2 times"
- **Missing `.Verifiable()` calls**: If you call `stub.Verify()` but nothing was marked `.Verifiable()`, no verification occurs
- **VerifyAll with uncalled members**: "Expected method Refresh to be called at least once, but was never called"

**Debugging tips:**

1. Check that you're calling the stub through the interface (not directly on the stub class)
2. Verify callbacks are configured before calling the method under test
3. Use `.Verifiable()` to explicitly mark what matters
4. Track call counts in your callbacks for debugging complex scenarios

---

## Best Practices

**Prefer `.Verifiable()` + `stub.Verify()` over manual assertions.** This prevents forgetting to verify critical calls and makes test intent explicit.

**Use `Times` constraints to be precise.** `Times.Once` is better than `Times.AtLeastOnce` when you know the exact expected behavior.

**Verify intent, not implementation details.** Test that the right methods were called with the right data, not the exact number of times internal helpers ran.

**Combine with argument verification when needed.** Use `.Verify()` for call frequency, then inspect `LastCallArg` for argument values.

**Keep assertions focused.** One logical verification per test makes failures easier to diagnose.

---

## See Also

- [Methods Guide](methods.md) - Configure method behavior and callbacks
- [Properties Guide](properties.md) - Work with property interceptors
- [Events Guide](events.md) - Raise and verify events
- [Delegates Guide](delegates.md) - Configure and verify delegate invocations
- [Interceptor API Reference](../reference/interceptor-api.md) - Complete API documentation

---

**UPDATED:** 2026-01-25
