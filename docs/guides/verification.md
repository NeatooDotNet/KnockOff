# Verification Guide

After configuring stub behavior, you need to verify that your code under test interacted with the stub correctly. KnockOff provides properties and patterns for verifying calls, arguments, state changes, and call order.

---

## What You Can Verify

KnockOff enables verification of:

- **Calls** - Whether a method or property was invoked
- **Arguments** - What values were passed to methods
- **State** - Property get/set operations and final values
- **Order** - The sequence of calls across multiple methods

All verification uses standard assertion libraries (xUnit, NUnit, MSTest). KnockOff exposes properties you inspect—it doesn't provide its own assertion API.

---

## Basic Call Verification

### WasCalled

The simplest verification checks whether a method or property was invoked.

<!-- snippet: verify-wascalled -->
```cs
[Fact]
public void WasCalled_VerifiesMethodInvoked()
{
    var stub = new RepoVerifyStub();
    var tracking = stub.GetById.OnCall((ko, id) => new User { Id = id });

    IRepoVerify repository = stub;
    repository.GetById(42);

    // WasCalled is true if invoked at least once
    Assert.True(tracking.WasCalled);
}
```
<!-- endSnippet -->

### Call Count (Exact)

For precise verification, check the exact number of calls.

<!-- snippet: verify-callcount-exact -->
```cs
[Fact]
public void CallCount_VerifiesExactNumber()
{
    var stub = new RepoVerifyStub();
    var tracking = stub.Save.OnCall((ko, user) => { });

    IRepoVerify repository = stub;
    repository.Save(new User { Id = 1 });

    // Verify exactly one call via tracking
    Assert.Equal(1, tracking.CallCount);
}
```
<!-- endSnippet -->

### Call Count (Range)

For flexible verification, assert on call count ranges.

<!-- snippet: verify-callcount-range -->
```cs
[Fact]
public void CallCount_VerifiesRange()
{
    var stub = new RepoVerifyStub();
    var tracking = stub.Refresh.OnCall((ko) => { });

    IRepoVerify repository = stub;

    // Simulate multiple refreshes
    repository.Refresh();
    repository.Refresh();
    repository.Refresh();

    // Verify at least 2 calls
    Assert.True(tracking.CallCount >= 2);
}
```
<!-- endSnippet -->

---

## Argument Verification

### Single Parameter (LastCallArg)

For methods with one parameter, use `LastCallArg` to inspect the most recent argument.

<!-- snippet: verify-lastcallarg -->
```cs
[Fact]
public void LastArg_VerifiesSingleParameter()
{
    var stub = new RepoVerifyStub();
    var tracking = stub.GetById.OnCall((ko, id) => new User { Id = id });

    IRepoVerify repository = stub;
    repository.GetById(42);

    // Verify the parameter value via tracking
    Assert.Equal(42, tracking.LastArg);
}
```
<!-- endSnippet -->

### Multiple Parameters (LastCallArgs)

For methods with multiple parameters, `LastCallArgs` returns a named tuple with each parameter accessible by name.

<!-- snippet: verify-lastcallargs-tuple -->
```cs
[Fact]
public void LastArgs_VerifiesMultipleParameters()
{
    var stub = new SvcVerifyStub();
    var tracking = stub.Update.OnCall((ko, id, name) => { });

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
    var tracking = stub.GetById.OnCall((ko, id) =>
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

    var saveTracking = stub.Save.OnCall((ko, user) => saveOrder = ++order);
    var refreshTracking = stub.Refresh.OnCall((ko) => refreshOrder = ++order);

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

You can verify relationships between different stub members by comparing their interceptor state.

<!-- snippet: verify-cross-interceptor -->
```cs
[Fact]
public void CrossInterceptor_VerifyMultipleMethodsCalled()
{
    var stub = new RepoVerifyStub();

    var getTracking = stub.GetById.OnCall((ko, id) => new User { Id = id });
    var saveTracking = stub.Save.OnCall((ko, user) => { });
    var refreshTracking = stub.Refresh.OnCall((ko) => { });

    IRepoVerify repository = stub;

    // Execute operations
    repository.GetById(1);
    repository.Save(new User { Id = 1 });
    repository.Refresh();

    // Verify all methods were called
    Assert.True(getTracking.WasCalled);
    Assert.True(saveTracking.WasCalled);
    Assert.True(refreshTracking.WasCalled);

    // Verify total interactions
    Assert.Equal(1, getTracking.CallCount);
    Assert.Equal(1, saveTracking.CallCount);
    Assert.Equal(1, refreshTracking.CallCount);
}
```
<!-- endSnippet -->

Combine this with call order tracking for comprehensive verification of method interactions.

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

Here's a comprehensive verification scenario combining multiple techniques.

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

    var getTracking = stub.GetById.OnCall((ko, id) =>
    {
        getIdHistory.Add(id);
        getOrder = ++order;
        return new User { Id = id, Name = $"User{id}" };
    });

    var saveTracking = stub.Save.OnCall((ko, user) =>
    {
        saveOrder = ++order;
    });

    var refreshTracking = stub.Refresh.OnCall((ko) =>
    {
        refreshOrder = ++order;
    });

    IRepoVerify repository = stub;

    // Execute operations
    repository.GetById(1);
    repository.GetById(2);
    repository.Save(new User { Id = 1, Name = "Updated" });
    repository.Refresh();

    // 1. Basic call verification
    Assert.True(getTracking.WasCalled);
    Assert.True(saveTracking.WasCalled);
    Assert.True(refreshTracking.WasCalled);

    // 2. Call count verification
    Assert.Equal(2, getTracking.CallCount);
    Assert.Equal(1, saveTracking.CallCount);
    Assert.Equal(1, refreshTracking.CallCount);

    // 3. Argument verification
    Assert.Equal(2, getTracking.LastArg); // Last call was GetById(2)

    // 4. Call history verification
    Assert.Equal(new[] { 1, 2 }, getIdHistory);

    // 5. Call order verification
    Assert.True(getOrder < saveOrder, "Get before Save");
    Assert.True(saveOrder < refreshOrder, "Save before Refresh");
}
```
<!-- endSnippet -->

This example demonstrates how to combine verification techniques for thorough test coverage.

---

## Best Practices

**Use the simplest verification that proves correctness.** Start with `WasCalled`, only add `CallCount` or argument checks if needed.

**Prefer built-in properties over callbacks.** Use `LastCallArg` and `CallCount` instead of capturing state in `OnCall` unless you need call history.

**Verify intent, not implementation details.** Test that the right methods were called with the right data, not the exact number of times internal helpers ran.

**Combine verification techniques.** Real tests often need to verify both "was it called" and "with what arguments" and "in what order."

**Keep assertions focused.** One logical verification per test makes failures easier to diagnose.

---

## See Also

- [Methods Guide](methods.md) - Configure method behavior and callbacks
- [Properties Guide](properties.md) - Work with property interceptors
- [Interceptor API Reference](../reference/interceptor-api.md) - Complete API documentation
