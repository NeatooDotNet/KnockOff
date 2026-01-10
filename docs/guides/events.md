# Events

KnockOff supports interface events with full subscription tracking and programmatic raising.

## Basic Usage

<!-- snippet: docs:events:basic-interface -->
```csharp
public interface IGuideEventSource
{
    event EventHandler<string>? MessageReceived;
    event EventHandler? OnCompleted;
    event Action<int>? OnProgress;
    event Action<string, int>? OnData;
}

[KnockOff]
public partial class GuideEventSourceKnockOff : IGuideEventSource { }
```
<!-- /snippet -->

## Supported Delegate Types

| Delegate Type | Raise Signature |
|--------------|-----------------|
| `EventHandler` | `Raise(object? sender, EventArgs e)` |
| `EventHandler<T>` | `Raise(object? sender, T e)` |
| `Action` | `Raise()` |
| `Action<T>` | `Raise(T arg)` |
| `Action<T1, T2>` | `Raise(T1 arg1, T2 arg2)` |

## Subscription Tracking

<!-- snippet: docs:events:subscription-tracking -->
```csharp
// Initially no subscribers
        var hasSubscribers = knockOff.MessageReceived.HasSubscribers;  // false
        var addCount = knockOff.MessageReceived.AddCount;              // 0

        // Subscribe
        EventHandler<string> handler = (sender, e) => Console.WriteLine(e);
        source.MessageReceived += handler;

        hasSubscribers = knockOff.MessageReceived.HasSubscribers;  // true
        addCount = knockOff.MessageReceived.AddCount;              // 1

        // Unsubscribe
        source.MessageReceived -= handler;

        var removeCount = knockOff.MessageReceived.RemoveCount;    // 1
```
<!-- /snippet -->

## Raising Events

### EventHandler<T>

<!-- snippet: docs:events:eventhandler-raise -->
```csharp
string? received = null;
        source.MessageReceived += (sender, e) => received = e;

        // Raise always requires sender parameter
        knockOff.MessageReceived.Raise(null, "Hello");

        // received is now "Hello"
```
<!-- /snippet -->

### EventHandler (no type parameter)

<!-- snippet: docs:events:eventhandler-noargs -->
```csharp
var invoked = false;
        source.OnCompleted += (sender, e) => invoked = true;

        // Raise requires sender and EventArgs
        knockOff.OnCompleted.Raise(null, EventArgs.Empty);

        // invoked is now true
```
<!-- /snippet -->

### Action<T>

<!-- snippet: docs:events:action-raise -->
```csharp
int? progress = null;
        source.OnProgress += (value) => progress = value;

        knockOff.OnProgress.Raise(75);

        // progress is now 75
```
<!-- /snippet -->

### Action<T1, T2>

<!-- snippet: docs:events:action-multi -->
```csharp
string? name = null;
        int? value = null;
        source.OnData += (n, v) => { name = n; value = v; };

        knockOff.OnData.Raise("Temperature", 72);

        // name is "Temperature", value is 72
```
<!-- /snippet -->

## Tracking Properties

Event interceptors track subscription counts:

<!-- snippet: docs:events:tracking-properties -->
```csharp
// Subscription tracking
        var addCount = knockOff.MessageReceived.AddCount;        // Times += was called
        var removeCount = knockOff.MessageReceived.RemoveCount;  // Times -= was called
        var hasSubs = knockOff.MessageReceived.HasSubscribers;   // At least one handler attached
```
<!-- /snippet -->

## Reset

`Reset()` clears **both tracking counts and removes all handlers**:

<!-- snippet: docs:events:reset -->
```csharp
var invoked = false;
        source.MessageReceived += (s, e) => invoked = true;

        // Before reset
        var addCountBefore = knockOff.MessageReceived.AddCount;        // 1
        var hasSubsBefore = knockOff.MessageReceived.HasSubscribers;   // true

        knockOff.MessageReceived.Reset();

        // After reset - tracking cleared
        var addCountAfter = knockOff.MessageReceived.AddCount;         // 0
        var removeCountAfter = knockOff.MessageReceived.RemoveCount;   // 0
        var hasSubsAfter = knockOff.MessageReceived.HasSubscribers;    // false

        // Handlers also cleared - raise does nothing
        invoked = false;
        knockOff.MessageReceived.Raise(null, "After");
        // invoked is still false - handler was removed by Reset
```
<!-- /snippet -->

## No Subscribers Safe

Raising an event with no subscribers does not throw:

<!-- snippet: docs:events:no-subscribers -->
```csharp
var knockOff = new GuideEventSourceKnockOff();

        // No exception - safe to raise with no handlers
        knockOff.MessageReceived.Raise(null, "No one listening");
```
<!-- /snippet -->

## Common Patterns

### Testing Event Handlers

<!-- snippet: docs:events:testing-viewmodel -->
```csharp
[KnockOff]
public partial class GuideDataServiceKnockOff : IGuideDataService { }
```
<!-- /snippet -->

```csharp
[Fact]
public void ViewModel_SubscribesToDataService_Events()
{
    var knockOff = new GuideDataServiceKnockOff();
    var viewModel = new MyViewModel(knockOff.AsGuideDataService());

    // ViewModel should have subscribed
    Assert.True(knockOff.DataChanged.HasSubscribers);
    Assert.Equal(1, knockOff.DataChanged.AddCount);
}

[Fact]
public void ViewModel_UpdatesOnDataChanged()
{
    var knockOff = new GuideDataServiceKnockOff();
    var viewModel = new MyViewModel(knockOff.AsGuideDataService());

    // Simulate data change
    knockOff.DataChanged.Raise(null, new DataChangedEventArgs { NewValue = 42 });

    Assert.Equal(42, viewModel.CurrentValue);
}
```

### Testing Event Unsubscription

```csharp
[Fact]
public void ViewModel_Dispose_UnsubscribesFromEvents()
{
    var knockOff = new GuideDataServiceKnockOff();
    var viewModel = new MyViewModel(knockOff.AsGuideDataService());

    Assert.Equal(1, knockOff.DataChanged.AddCount);

    viewModel.Dispose();

    Assert.Equal(1, knockOff.DataChanged.RemoveCount);
}
```

### Progress Reporting

<!-- snippet: docs:events:progress-reporting -->
```csharp
[KnockOff]
public partial class GuideDownloaderKnockOff : IGuideDownloader { }
```
<!-- /snippet -->

<!-- snippet: docs:events:progress-example -->
```csharp
var progressValues = new List<int>();
        ((IGuideDownloader)knockOff).ProgressChanged += (p) => progressValues.Add(p);

        // Simulate download progress
        knockOff.ProgressChanged.Raise(0);
        knockOff.ProgressChanged.Raise(25);
        knockOff.ProgressChanged.Raise(50);
        knockOff.ProgressChanged.Raise(75);
        knockOff.ProgressChanged.Raise(100);

        // progressValues is [0, 25, 50, 75, 100]
```
<!-- /snippet -->

### Multiple Subscribers

<!-- snippet: docs:events:multiple-subscribers -->
```csharp
var received = new List<string>();
        source.MessageReceived += (s, e) => received.Add($"Handler1: {e}");
        source.MessageReceived += (s, e) => received.Add($"Handler2: {e}");
        source.MessageReceived += (s, e) => received.Add($"Handler3: {e}");

        var addCount = knockOff.MessageReceived.AddCount;  // 3

        knockOff.MessageReceived.Raise(null, "Test");

        // received contains ["Handler1: Test", "Handler2: Test", "Handler3: Test"]
```
<!-- /snippet -->

## Interceptor API Reference

| Property/Method | Type | Description |
|----------------|------|-------------|
| `AddCount` | `int` | Times handlers were added (+=) |
| `RemoveCount` | `int` | Times handlers were removed (-=) |
| `HasSubscribers` | `bool` | True if any handler is attached |
| `Raise(...)` | `void` | Raises the event to all handlers |
| `Reset()` | `void` | Clears counts AND removes all handlers |
