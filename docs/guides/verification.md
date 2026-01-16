# Verification

This guide covers verifying method calls, property access, and argument capture.

## Method Verification

### Was a Method Called?

<!-- snippet: verification-was-called -->
```cs
public static void WasMethodCalled()
{
    var stub = new VfEmailServiceStub();
    IVfEmailService service = stub;
    service.SendEmail("user@test.com", "Subject", "Body");

    var deleteStub = new VfProcessorStub();

    Assert.True(stub.SendEmail.WasCalled);
    Assert.False(deleteStub.Delete.WasCalled);
}
```
<!-- endSnippet -->

### How Many Times?

<!-- snippet: verification-call-count -->
```cs
public static void HowManyTimes()
{
    var stub = new VfProcessorStub();
    IVfProcessor service = stub;

    service.Process("a");
    service.Process("b");
    service.Process("c");

    Assert.Equal(3, stub.Process.CallCount);
}
```
<!-- endSnippet -->

### Argument Capture

**Single parameter** — use `LastCallArg`:

<!-- snippet: verification-single-arg -->
```cs
public static void SingleParameterCapture()
{
    var stub = new VfRepositoryStub();
    IVfRepository service = stub;

    service.GetById(42);

    int? lastId = stub.GetById.LastCallArg;  // 42

    _ = lastId;
}
```
<!-- endSnippet -->

**Multiple parameters** — use `LastCallArgs` (named tuple):

<!-- snippet: verification-multiple-args -->
```cs
public static void MultipleParameterCapture()
{
    var stub = new VfEmailServiceStub();
    IVfEmailService service = stub;

    service.SendEmail("user@test.com", "Subject", "Body");

    var args = stub.SendEmail.LastCallArgs;
    Assert.Equal("user@test.com", args?.to);
    Assert.Equal("Subject", args?.subject);
    Assert.Equal("Body", args?.body);
}
```
<!-- endSnippet -->

**Note:** Tuple member names match the original parameter names.

### No Parameters

Methods without parameters have `WasCalled` and `CallCount` only:

<!-- snippet: verification-no-params -->
```cs
public static void NoParametersMethod()
{
    var stub = new VfProcessorStub();
    IVfProcessor service = stub;

    service.Initialize();

    Assert.True(stub.Initialize.WasCalled);
    Assert.Equal(1, stub.Initialize.CallCount);
}
```
<!-- endSnippet -->

## Property Verification

### Get/Set Counts

<!-- snippet: verification-property-counts -->
```cs
public static void PropertyGetSetCounts()
{
    var stub = new VfConnectionServiceStub();
    IVfConnectionService service = stub;

    _ = service.Name;
    _ = service.Name;
    service.Name = "First";
    service.Name = "Second";

    Assert.Equal(2, stub.Name.GetCount);
    Assert.Equal(2, stub.Name.SetCount);
    Assert.Equal("Second", stub.Name.LastSetValue);
}
```
<!-- endSnippet -->

### Get-Only Properties

Get-only properties track getter access:

<!-- snippet: verification-get-only-property -->
```cs
public static void GetOnlyPropertyTracking()
{
    var stub = new VfConnectionServiceStub();
    IVfConnectionService service = stub;

    _ = service.ConnectionString;
    _ = service.ConnectionString;

    Assert.Equal(2, stub.ConnectionString.GetCount);
}
```
<!-- endSnippet -->

### Set-Only Properties

Set-only properties track setter calls:

<!-- snippet: verification-set-only-property -->
```cs
public static void SetOnlyPropertyTracking()
{
    var stub = new VfConnectionServiceStub();
    IVfConnectionService service = stub;

    service.Output = "Line 1";
    service.Output = "Line 2";

    Assert.Equal(2, stub.Output.SetCount);
    Assert.Equal("Line 2", stub.Output.LastSetValue);
}
```
<!-- endSnippet -->

## Indexer Verification

### Access Tracking

<!-- snippet: verification-indexer -->
```cs
public static void IndexerTracking()
{
    var stub = new VfKeyValueStoreStub();
    IVfKeyValueStore store = stub;

    _ = store["Key1"];
    _ = store["Key2"];
    store["Key3"] = "Value";

    Assert.Equal(2, stub.Indexer.GetCount);
    Assert.Equal("Key2", stub.Indexer.LastGetKey);

    Assert.Equal(1, stub.Indexer.SetCount);
    Assert.Equal("Key3", stub.Indexer.LastSetEntry?.Key);
    Assert.Equal("Value", stub.Indexer.LastSetEntry?.Value);
}
```
<!-- endSnippet -->

## Event Verification

### Subscription Tracking

<!-- snippet: verification-events -->
```cs
public static void EventSubscriptionTracking()
{
    var stub = new VfEventSourceStub();
    IVfEventSource source = stub;

    EventHandler<string>? handler = (s, e) => { };
    source.DataReceived += handler;
    source.DataReceived -= handler;

    Assert.Equal(1, stub.DataReceived.AddCount);
    Assert.Equal(1, stub.DataReceived.RemoveCount);
    Assert.False(stub.DataReceived.HasSubscribers);
}
```
<!-- endSnippet -->

## Generic Method Verification

### Per-Type Tracking

<!-- snippet: verification-generic-per-type -->
```cs
public static void GenericMethodPerType()
{
    var stub = new VfSerializerStub();
    IVfSerializer service = stub;

    service.Deserialize<VfUser>("{}");
    service.Deserialize<VfUser>("{}");
    service.Deserialize<VfOrder>("{}");

    Assert.Equal(2, stub.Deserialize.Of<VfUser>().CallCount);
    Assert.Equal(1, stub.Deserialize.Of<VfOrder>().CallCount);
}
```
<!-- endSnippet -->

### Aggregate Tracking

<!-- snippet: verification-generic-aggregate -->
```cs
public static void GenericMethodAggregate()
{
    var stub = new VfSerializerStub();
    IVfSerializer service = stub;

    service.Deserialize<VfUser>("{}");
    service.Deserialize<VfUser>("{}");
    service.Deserialize<VfOrder>("{}");

    Assert.Equal(3, stub.Deserialize.TotalCallCount);
    Assert.True(stub.Deserialize.WasCalled);
    Assert.Equal(2, stub.Deserialize.CalledTypeArguments.Count);
}
```
<!-- endSnippet -->

## Reset

Clear all tracking data:

<!-- snippet: verification-reset -->
```cs
public static void ResetTracking()
{
    var processStub = new VfProcessorStub();
    var nameStub = new VfConnectionServiceStub();
    var eventStub = new VfEventSourceStub();
    var serializerStub = new VfSerializerStub();

    // Method
    processStub.Process.Reset();
    Assert.Equal(0, processStub.Process.CallCount);
    Assert.False(processStub.Process.WasCalled);

    // Property
    nameStub.Name.Reset();
    Assert.Equal(0, nameStub.Name.GetCount);
    Assert.Equal(0, nameStub.Name.SetCount);

    // Event
    eventStub.DataReceived.Reset();
    Assert.Equal(0, eventStub.DataReceived.AddCount);
    Assert.False(eventStub.DataReceived.HasSubscribers);

    // Generic method (single type)
    serializerStub.Deserialize.Of<VfUser>().Reset();

    // Generic method (all types)
    serializerStub.Deserialize.Reset();
}
```
<!-- endSnippet -->

**Note:** `Reset()` clears tracking AND callbacks/OnGet/OnSet. The backing `Value` is preserved for properties.

## Cross-Interceptor Verification

Verify behavior depends on other stub state:

<!-- snippet: verification-cross-interceptor -->
```cs
public static void CrossInterceptorVerification()
{
    var stub = new VfProcessorStub();
    IVfProcessor service = stub;

    stub.Process.OnCall = (ko, value) =>
    {
        if (!ko.Initialize.WasCalled)
            throw new InvalidOperationException("Not initialized");
    };

    // This throws because Initialize wasn't called first
    Assert.Throws<InvalidOperationException>(() => service.Process("test"));

    // Now initialize and try again
    service.Initialize();
    service.Process("test");  // Succeeds
}
```
<!-- endSnippet -->

## Verification Patterns

### Verify Exact Call Count

<!-- snippet: verification-exact-count -->
```cs
public static void VerifyExactCallCount()
{
    var stub = new VfRepositoryStub();
    IVfRepository service = stub;
    service.Save(new VfUser());

    Assert.Equal(1, stub.Save.CallCount);  // Called exactly once
}
```
<!-- endSnippet -->

### Verify Never Called

<!-- snippet: verification-never-called -->
```cs
public static void VerifyNeverCalled()
{
    var stub = new VfProcessorStub();

    Assert.False(stub.Delete.WasCalled);
    Assert.Equal(0, stub.Delete.CallCount);
}
```
<!-- endSnippet -->

### Verify Call Order

Track order with callbacks:

<!-- snippet: verification-call-order -->
```cs
public static void VerifyCallOrder()
{
    var stub = new VfProcessorStub();
    IVfProcessor service = stub;

    var callOrder = new List<string>();

    stub.Initialize.OnCall = (ko) => callOrder.Add("Initialize");
    stub.Process.OnCall = (ko, value) => callOrder.Add("Process");
    stub.Cleanup.OnCall = (ko) => callOrder.Add("Cleanup");

    service.Initialize();
    service.Process("test");
    service.Cleanup();

    Assert.Equal(["Initialize", "Process", "Cleanup"], callOrder);
}
```
<!-- endSnippet -->

### Verify All Arguments (History)

For full call history, capture in a callback:

<!-- snippet: verification-history -->
```cs
public static void VerifyAllArgumentsHistory()
{
    var stub = new VfEmailServiceStub();
    IVfEmailService service = stub;

    var allCalls = new List<(string to, string subject, string body)>();

    stub.SendEmail.OnCall = (ko, to, subject, body) =>
    {
        allCalls.Add((to, subject, body));
    };

    service.SendEmail("a@test.com", "S1", "B1");
    service.SendEmail("b@test.com", "S2", "B2");

    Assert.Equal(2, allCalls.Count);
    Assert.Equal("a@test.com", allCalls[0].to);
    Assert.Equal("b@test.com", allCalls[1].to);
}
```
<!-- endSnippet -->

## Quick Reference

| Member Type | Tracking Properties |
|-------------|---------------------|
| **Method** | `WasCalled`, `CallCount`, `LastCallArg`/`LastCallArgs` |
| **Property** | `GetCount`, `SetCount`, `LastSetValue` |
| **Indexer** | `GetCount`, `SetCount`, `LastGetKey`, `LastSetEntry` |
| **Event** | `AddCount`, `RemoveCount`, `HasSubscribers` |
| **Generic** | `Of<T>().CallCount`, `TotalCallCount`, `CalledTypeArguments` |
