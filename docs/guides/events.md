# Events

KnockOff supports interface events with full subscription tracking and programmatic raising.

## Basic Usage

<!-- snippet: events-basic-interface -->
```cs
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
<!-- endSnippet -->

## Supported Delegate Types

| Delegate Type | Raise Signature |
|--------------|-----------------|
| `EventHandler` | `Raise(object? sender, EventArgs e)` |
| `EventHandler<T>` | `Raise(object? sender, T e)` |
| `Action` | `Raise()` |
| `Action<T>` | `Raise(T arg)` |
| `Action<T1, T2>` | `Raise(T1 arg1, T2 arg2)` |

## Subscription Tracking

<!-- snippet: events-subscription-tracking -->
```cs
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
<!-- endSnippet -->

## Raising Events

### EventHandler<T>

<!-- snippet: events-eventhandler-raise -->
```cs
string? received = null;
source.MessageReceived += (sender, e) => received = e;

// Raise always requires sender parameter
knockOff.MessageReceived.Raise(null, "Hello");

// received is now "Hello"
```
<!-- endSnippet -->

### EventHandler (no type parameter)

<!-- snippet: events-eventhandler-noargs -->
```cs
var invoked = false;
source.OnCompleted += (sender, e) => invoked = true;

// Raise requires sender and EventArgs
knockOff.OnCompleted.Raise(null, EventArgs.Empty);

// invoked is now true
```
<!-- endSnippet -->

### Action<T>

<!-- snippet: events-action-raise -->
```cs
int? progress = null;
source.OnProgress += (value) => progress = value;

knockOff.OnProgress.Raise(75);

// progress is now 75
```
<!-- endSnippet -->

### Action<T1, T2>

<!-- snippet: events-action-multi -->
```cs
string? name = null;
int? value = null;
source.OnData += (n, v) => { name = n; value = v; };

knockOff.OnData.Raise("Temperature", 72);

// name is "Temperature", value is 72
```
<!-- endSnippet -->

## Tracking Properties

Event interceptors track subscription counts:

<!-- snippet: events-tracking-properties -->
```cs
// Subscription tracking
var addCount = knockOff.MessageReceived.AddCount;        // Times += was called
var removeCount = knockOff.MessageReceived.RemoveCount;  // Times -= was called
var hasSubs = knockOff.MessageReceived.HasSubscribers;   // At least one handler attached
```
<!-- endSnippet -->

## Reset

`Reset()` clears **both tracking counts and removes all handlers**:

<!-- snippet: events-reset -->
```cs
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
<!-- endSnippet -->

## No Subscribers Safe

Raising an event with no subscribers does not throw:

<!-- snippet: events-no-subscribers -->
```cs
var knockOff = new GuideEventSourceKnockOff();

// No exception - safe to raise with no handlers
knockOff.MessageReceived.Raise(null, "No one listening");
```
<!-- endSnippet -->

## Common Patterns

### Testing Event Handlers

<!-- snippet: events-testing-viewmodel -->
```cs
[KnockOff]
public partial class GuideDataServiceKnockOff : IGuideDataService { }
```
<!-- endSnippet -->

<!-- pseudo:viewmodel-event-tests -->
```csharp
[Fact]
public void ViewModel_SubscribesToDataService_Events()
{
    var knockOff = new GuideDataServiceKnockOff();
    var viewModel = new MyViewModel(knockOff.AsIGuideDataService());

    // ViewModel should have subscribed
    Assert.True(knockOff.DataChanged.HasSubscribers);
    Assert.Equal(1, knockOff.DataChanged.AddCount);
}

[Fact]
public void ViewModel_UpdatesOnDataChanged()
{
    var knockOff = new GuideDataServiceKnockOff();
    var viewModel = new MyViewModel(knockOff.AsIGuideDataService());

    // Simulate data change
    knockOff.DataChanged.Raise(null, new DataChangedEventArgs { NewValue = 42 });

    Assert.Equal(42, viewModel.CurrentValue);
}
```
<!-- /snippet -->

### Testing Event Unsubscription

<!-- pseudo:viewmodel-unsubscribe-test -->
```csharp
[Fact]
public void ViewModel_Dispose_UnsubscribesFromEvents()
{
    var knockOff = new GuideDataServiceKnockOff();
    var viewModel = new MyViewModel(knockOff.AsIGuideDataService());

    Assert.Equal(1, knockOff.DataChanged.AddCount);

    viewModel.Dispose();

    Assert.Equal(1, knockOff.DataChanged.RemoveCount);
}
```
<!-- /snippet -->

### Progress Reporting

<!-- snippet: events-progress-reporting -->
```cs
[KnockOff]
public partial class GuideDownloaderKnockOff : IGuideDownloader { }
```
<!-- endSnippet -->

<!-- snippet: events-progress-example -->
```cs
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
<!-- endSnippet -->

### Multiple Subscribers

<!-- snippet: events-multiple-subscribers -->
```cs
var received = new List<string>();
source.MessageReceived += (s, e) => received.Add($"Handler1: {e}");
source.MessageReceived += (s, e) => received.Add($"Handler2: {e}");
source.MessageReceived += (s, e) => received.Add($"Handler3: {e}");

var addCount = knockOff.MessageReceived.AddCount;  // 3

knockOff.MessageReceived.Raise(null, "Test");

// received contains ["Handler1: Test", "Handler2: Test", "Handler3: Test"]
```
<!-- endSnippet -->

## Interceptor API Reference

| Property/Method | Type | Description |
|----------------|------|-------------|
| `AddCount` | `int` | Times handlers were added (+=) |
| `RemoveCount` | `int` | Times handlers were removed (-=) |
| `HasSubscribers` | `bool` | True if any handler is attached |
| `Raise(...)` | `void` | Raises the event to all handlers |
| `Reset()` | `void` | Clears counts AND removes all handlers |
