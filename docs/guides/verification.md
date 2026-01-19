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
[Fact]
public void Verifiable_MarksForBatchVerification()
{
    var stub = new RepoVerifyStub();

    // Chain .Verifiable() to mark for batch verification
    stub.GetById.OnCall((id) => new User { Id = id }).Verifiable();

    IRepoVerify repository = stub;
    repository.GetById(42);

    // Verify() checks all members marked with .Verifiable()
    stub.Verify();
}
```
<!-- endSnippet -->

### Exactly Once

Verify a method was called exactly once.

<!-- snippet: verify-times-once -->
```cs
[Fact]
public void Verify_WithTimesOnce()
{
    var stub = new RepoVerifyStub();
    var tracking = stub.Save.OnCall((user) => { });

    IRepoVerify repository = stub;
    repository.Save(new User { Id = 1 });

    // Verify exactly one call using Times.Once
    tracking.Verify(Times.Once);
}
```
<!-- endSnippet -->

### At Least N Calls

Verify a method was called a minimum number of times.

<!-- snippet: verify-times-atleast -->
```cs
[Fact]
public void Verify_WithTimesAtLeast()
{
    var stub = new RepoVerifyStub();
    var tracking = stub.Refresh.OnCall(() => { });

    IRepoVerify repository = stub;

    // Simulate multiple refreshes
    repository.Refresh();
    repository.Refresh();
    repository.Refresh();

    // Verify at least 2 calls
    tracking.Verify(Times.AtLeast(2));
}
```
<!-- endSnippet -->

### Never Called

Verify a method was never invoked.

<!-- snippet: verify-times-never -->
```cs
[Fact]
public void Verify_WithTimesNever()
{
    var stub = new RepoVerifyStub();
    var tracking = stub.Refresh.OnCall(() => { });

    IRepoVerify repository = stub;
    // Don't call Refresh

    // Verify method was never called via tracking
    tracking.Verify(Times.Never);
}
```
<!-- endSnippet -->

### All Times Matchers

The `Times` struct supports these verification modes:

- `Times.AtLeastOnce` - Default, at least one call
- `Times.Once` - Exactly one call
- `Times.Exactly(n)` - Exactly N calls
- `Times.AtLeast(n)` - At least N calls
- `Times.AtMost(n)` - At most N calls
- `Times.Between(min, max, Range)` - Between min and max calls (inclusive or exclusive)
- `Times.Never` - Zero calls

---

## Marked Verification (Recommended)

Use `.Verifiable()` to mark interceptors as requiring verification, then call `stub.Verify()` to check them all at once. This approach prevents "missing verification" bugs where you forget to check a critical call.

### Basic Marked Verification

<!-- snippet: verify-verifiable -->
```cs
[Fact]
public void Verifiable_MarksForBatchVerification()
{
    var stub = new RepoVerifyStub();

    // Chain .Verifiable() to mark for batch verification
    stub.GetById.OnCall((id) => new User { Id = id }).Verifiable();

    IRepoVerify repository = stub;
    repository.GetById(42);

    // Verify() checks all members marked with .Verifiable()
    stub.Verify();
}
```
<!-- endSnippet -->

### Verifiable with Times

You can specify `Times` constraints when marking with `.Verifiable()`.

<!-- snippet: verify-verifiable-times -->
```cs
[Fact]
public void Verifiable_WithTimesConstraint()
{
    var stub = new RepoVerifyStub();

    // Mark with Times constraint for batch verification
    stub.Refresh.OnCall(() => { }).Verifiable(Times.Exactly(2));

    IRepoVerify repository = stub;
    repository.Refresh();
    repository.Refresh();

    // Verify() respects the Times constraint
    stub.Verify();
}
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
[Fact]
public void VerifyAll_ChecksAllConfiguredMembers()
{
    var stub = new RepoVerifyStub();

    // Configure multiple members (no need to mark Verifiable)
    stub.GetById.OnCall((id) => new User { Id = id });
    stub.Save.OnCall((user) => { });

    IRepoVerify repository = stub;
    repository.GetById(1);
    repository.Save(new User { Id = 1 });

    // VerifyAll() checks all configured members were called at least once
    stub.VerifyAll();
}
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
[Fact]
public void LastArg_VerifiesSingleParameter()
{
    var stub = new RepoVerifyStub();
    var tracking = stub.GetById.OnCall((id) => new User { Id = id });

    IRepoVerify repository = stub;
    repository.GetById(42);

    // Verify the parameter value via tracking
    Assert.Equal(42, tracking.LastArg);
}
```
<!-- endSnippet -->

### Multiple Parameters (LastCallArgs)

<!-- snippet: verify-lastcallargs-tuple -->
```cs
[Fact]
public void LastArgs_VerifiesMultipleParameters()
{
    var stub = new SvcVerifyStub();
    var tracking = stub.Update.OnCall((id, name) => { });

    ISvcVerify service = stub;
    service.Update(42, "Alice");

    // Destructure the named tuple for verification
    var (id, name) = tracking.LastArgs;
    Assert.Equal(42, id);
    Assert.Equal("Alice", name);
}
```
<!-- endSnippet -->

---

## Call History Tracking

For complex scenarios requiring inspection of all calls (not just the last), use `OnCall` callbacks to capture a complete history.

<!-- snippet: verify-call-history -->
```cs
[Fact]
public void OnCall_CapturesAllCallsToList()
{
    var stub = new RepoVerifyStub();

    // Capture all calls to a list within the callback
    var calls = new List<int>();
    var tracking = stub.GetById.OnCall((id) =>
    {
        calls.Add(id);
        return new User { Id = id };
    });

    IRepoVerify repository = stub;

    repository.GetById(1);
    repository.GetById(2);
    repository.GetById(3);

    // Verify the complete call history
    Assert.Equal(new[] { 1, 2, 3 }, calls);
}
```
<!-- endSnippet -->

This pattern works for any verification need beyond "last call" inspection.

---

## Call Order Verification

To verify that methods were called in a specific sequence, track call order using shared state.

<!-- snippet: verify-call-order -->
```cs
[Fact]
public void CallOrder_VerifiedWithCounter()
{
    var stub = new RepoVerifyStub();

    var order = 0;
    var saveOrder = 0;
    var refreshOrder = 0;

    var saveTracking = stub.Save.OnCall((user) => saveOrder = ++order);
    var refreshTracking = stub.Refresh.OnCall(() => refreshOrder = ++order);

    IRepoVerify repository = stub;

    // Execute operations
    repository.Save(new User { Id = 1 });
    repository.Refresh();

    // Verify Save was called before Refresh
    Assert.True(saveOrder < refreshOrder, "Save should be called before Refresh");
}
```
<!-- endSnippet -->

This approach scales to any number of methods and supports complex ordering assertions.

---

## Cross-Interceptor Verification

Verify multiple methods were called using `.Verifiable()` and `stub.Verify()`.

<!-- snippet: verify-cross-interceptor -->
```cs
[Fact]
public void CrossInterceptor_VerifyMultipleMethodsCalled()
{
    var stub = new RepoVerifyStub();

    // Mark all methods as verifiable
    stub.GetById.OnCall((id) => new User { Id = id }).Verifiable();
    stub.Save.OnCall((user) => { }).Verifiable();
    stub.Refresh.OnCall(() => { }).Verifiable();

    IRepoVerify repository = stub;

    // Execute operations
    repository.GetById(1);
    repository.Save(new User { Id = 1 });
    repository.Refresh();

    // Single Verify() checks all marked members
    stub.Verify();
}
```
<!-- endSnippet -->

This approach is cleaner than individual assertions and catches missing verifications.

---

## Property Verification

Properties expose separate interceptors for get and set operations.

**Get verification:**
- `GetCount` - Number of times the property was read
- `WasGot` - Whether the property was read at least once

**Set verification:**
- `SetCount` - Number of times the property was written
- `WasSet` - Whether the property was written at least once
- `LastSetValue` - The most recent value assigned to the property

These follow the same patterns as method verification but distinguish between read and write operations.

---

## Complete Example

Here's a comprehensive verification scenario demonstrating the recommended patterns.

<!-- snippet: verify-complete-example -->
```cs
[Fact]
public void CompleteVerification_AllTechniques()
{
    var stub = new RepoVerifyStub();

    // Track call order
    var order = 0;
    var getOrder = 0;
    var saveOrder = 0;
    var refreshOrder = 0;

    // Track call history
    var getIdHistory = new List<int>();

    // Mark all methods as verifiable with specific constraints
    var getTracking = stub.GetById.OnCall((id) =>
    {
        getIdHistory.Add(id);
        getOrder = ++order;
        return new User { Id = id, Name = $"User{id}" };
    }).Verifiable(Times.Exactly(2));

    var saveTracking = stub.Save.OnCall((user) =>
    {
        saveOrder = ++order;
    }).Verifiable(Times.Once);

    var refreshTracking = stub.Refresh.OnCall(() =>
    {
        refreshOrder = ++order;
    }).Verifiable(Times.Once);

    IRepoVerify repository = stub;

    // Execute operations
    repository.GetById(1);
    repository.GetById(2);
    repository.Save(new User { Id = 1, Name = "Updated" });
    repository.Refresh();

    // 1. Batch verification - checks all Times constraints
    stub.Verify();

    // 2. Argument verification
    Assert.Equal(2, getTracking.LastArg); // Last call was GetById(2)

    // 3. Call history verification
    Assert.Equal(new[] { 1, 2 }, getIdHistory);

    // 4. Call order verification
    Assert.True(getOrder < saveOrder, "Get before Save");
    Assert.True(saveOrder < refreshOrder, "Save before Refresh");
}
```
<!-- endSnippet -->

This example shows the modern verification approach: mark what matters with `.Verifiable()`, verify with `stub.Verify()`, and add detailed assertions only when needed.

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
- [Interceptor API Reference](../reference/interceptor-api.md) - Complete API documentation
