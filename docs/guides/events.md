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

| Delegate Type | Raise Signature | Args Tracking |
|--------------|-----------------|---------------|
| `EventHandler` | `Raise()` | `(object? sender, EventArgs e)` |
| `EventHandler<T>` | `Raise(T e)` or `Raise(sender, e)` | `(object? sender, T e)` |
| `Action` | `Raise()` | None |
| `Action<T>` | `Raise(T)` | `T` |
| `Action<T1, T2>` | `Raise(T1, T2)` | `(T1 arg1, T2 arg2)` |
| `Func<TResult>` | `RaiseAndReturn()` | None, returns `TResult` |

## Subscription Tracking

```csharp
var knockOff = new EventSourceKnockOff();
IEventSource source = knockOff;

// Initially no subscribers
Assert.False(knockOff.IEventSource.MessageReceived.HasSubscribers);
Assert.Equal(0, knockOff.IEventSource.MessageReceived.SubscribeCount);

// Subscribe
EventHandler<string> handler = (sender, e) => Console.WriteLine(e);
source.MessageReceived += handler;

Assert.True(knockOff.IEventSource.MessageReceived.HasSubscribers);
Assert.Equal(1, knockOff.IEventSource.MessageReceived.SubscribeCount);

// Unsubscribe
source.MessageReceived -= handler;

Assert.Equal(1, knockOff.IEventSource.MessageReceived.UnsubscribeCount);
```

## Raising Events

### EventHandler<T>

```csharp
var knockOff = new EventSourceKnockOff();
IEventSource source = knockOff;

string? received = null;
source.MessageReceived += (sender, e) => received = e;

// Raise with sender
knockOff.IEventSource.MessageReceived.Raise(knockOff, "Hello");

// Or raise with null sender (convenience)
knockOff.IEventSource.MessageReceived.Raise("World");

Assert.Equal("World", received);
```

### EventHandler (no type parameter)

```csharp
var invoked = false;
source.OnCompleted += (sender, e) => invoked = true;

// Raise with no arguments (uses null sender, EventArgs.Empty)
knockOff.IEventSource.OnCompleted.Raise();

Assert.True(invoked);
```

### Action<T>

```csharp
int? progress = null;
source.OnProgress += (value) => progress = value;

knockOff.IEventSource.OnProgress.Raise(75);

Assert.Equal(75, progress);
```

### Action<T1, T2>

```csharp
string? name = null;
int? value = null;
source.OnData += (n, v) => { name = n; value = v; };

knockOff.IEventSource.OnData.Raise("Temperature", 72);

Assert.Equal("Temperature", name);
Assert.Equal(72, value);
```

## Raise Tracking

Every raise is tracked with arguments:

```csharp
knockOff.IEventSource.MessageReceived.Raise("First");
knockOff.IEventSource.MessageReceived.Raise("Second");
knockOff.IEventSource.MessageReceived.Raise("Third");

// Count and boolean
Assert.Equal(3, knockOff.IEventSource.MessageReceived.RaiseCount);
Assert.True(knockOff.IEventSource.MessageReceived.WasRaised);

// Last raise arguments (tuple for EventHandler<T>)
var lastArgs = knockOff.IEventSource.MessageReceived.LastRaiseArgs;
Assert.Null(lastArgs?.sender);  // null sender from Raise(string) overload
Assert.Equal("Third", lastArgs?.e);

// All raises
var allRaises = knockOff.IEventSource.MessageReceived.AllRaises;
Assert.Equal("First", allRaises[0].e);
Assert.Equal("Second", allRaises[1].e);
Assert.Equal("Third", allRaises[2].e);
```

### Multi-Parameter Action Tracking

```csharp
knockOff.IEventSource.OnData.Raise("Temp", 72);
knockOff.IEventSource.OnData.Raise("Humidity", 45);

// Named tuple access
var last = knockOff.IEventSource.OnData.LastRaiseArgs;
Assert.Equal("Humidity", last?.arg1);
Assert.Equal(45, last?.arg2);

// All raises as tuples
var all = knockOff.IEventSource.OnData.AllRaises;
Assert.Equal(("Temp", 72), all[0]);
Assert.Equal(("Humidity", 45), all[1]);
```

## Reset vs Clear

### Reset

Clears tracking counters but **keeps handlers attached**:

```csharp
var invoked = false;
source.MessageReceived += (s, e) => invoked = true;

knockOff.IEventSource.MessageReceived.Raise("Before");
Assert.Equal(1, knockOff.IEventSource.MessageReceived.RaiseCount);

knockOff.IEventSource.MessageReceived.Reset();

// Tracking cleared
Assert.Equal(0, knockOff.IEventSource.MessageReceived.SubscribeCount);
Assert.Equal(0, knockOff.IEventSource.MessageReceived.RaiseCount);
Assert.Empty(knockOff.IEventSource.MessageReceived.AllRaises);

// But handlers still work!
invoked = false;
knockOff.IEventSource.MessageReceived.Raise("After");
Assert.True(invoked);
```

### Clear

Clears **both tracking and handlers**:

```csharp
var invoked = false;
source.MessageReceived += (s, e) => invoked = true;

knockOff.IEventSource.MessageReceived.Raise("Before");
Assert.True(invoked);

knockOff.IEventSource.MessageReceived.Clear();

// Tracking cleared
Assert.Equal(0, knockOff.IEventSource.MessageReceived.RaiseCount);
Assert.False(knockOff.IEventSource.MessageReceived.HasSubscribers);

// Handlers also cleared - raise does nothing
invoked = false;
knockOff.IEventSource.MessageReceived.Raise("After");
Assert.False(invoked);  // Handler was cleared
```

## No Subscribers Safe

Raising an event with no subscribers does not throw:

```csharp
var knockOff = new EventSourceKnockOff();

// No exception, just tracked
knockOff.IEventSource.MessageReceived.Raise("No one listening");

Assert.True(knockOff.IEventSource.MessageReceived.WasRaised);
Assert.Equal(1, knockOff.IEventSource.MessageReceived.RaiseCount);
```

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
    Assert.True(knockOff.IGuideDataService.DataChanged.HasSubscribers);
    Assert.Equal(1, knockOff.IGuideDataService.DataChanged.SubscribeCount);
}

[Fact]
public void ViewModel_UpdatesOnDataChanged()
{
    var knockOff = new GuideDataServiceKnockOff();
    var viewModel = new MyViewModel(knockOff.AsGuideDataService());

    // Simulate data change
    knockOff.IGuideDataService.DataChanged.Raise(new DataChangedEventArgs { NewValue = 42 });

    Assert.Equal(42, viewModel.CurrentValue);
}
```

### Testing Event Unsubscription

```csharp
[Fact]
public void ViewModel_Dispose_UnsubscribesFromEvents()
{
    var knockOff = new DataServiceKnockOff();
    var viewModel = new MyViewModel(knockOff.AsDataService());

    Assert.Equal(1, knockOff.IDataService.DataChanged.SubscribeCount);

    viewModel.Dispose();

    Assert.Equal(1, knockOff.IDataService.DataChanged.UnsubscribeCount);
}
```

### Progress Reporting

<!-- snippet: docs:events:progress-reporting -->
```csharp
[KnockOff]
public partial class GuideDownloaderKnockOff : IGuideDownloader { }
```
<!-- /snippet -->

```csharp
[Fact]
public async Task Downloader_ReportsProgress()
{
    var knockOff = new GuideDownloaderKnockOff();
    var progressValues = new List<int>();

    knockOff.AsGuideDownloader().ProgressChanged += (p) => progressValues.Add(p);

    // Simulate download progress
    knockOff.IGuideDownloader.ProgressChanged.Raise(0);
    knockOff.IGuideDownloader.ProgressChanged.Raise(25);
    knockOff.IGuideDownloader.ProgressChanged.Raise(50);
    knockOff.IGuideDownloader.ProgressChanged.Raise(75);
    knockOff.IGuideDownloader.ProgressChanged.Raise(100);

    Assert.Equal([0, 25, 50, 75, 100], progressValues);
    Assert.Equal(5, knockOff.IGuideDownloader.ProgressChanged.RaiseCount);
}
```

### Multiple Subscribers

```csharp
[Fact]
public void Event_MultipleSubscribers_AllReceiveEvent()
{
    var knockOff = new EventSourceKnockOff();
    IEventSource source = knockOff;

    var received = new List<string>();
    source.MessageReceived += (s, e) => received.Add($"Handler1: {e}");
    source.MessageReceived += (s, e) => received.Add($"Handler2: {e}");
    source.MessageReceived += (s, e) => received.Add($"Handler3: {e}");

    Assert.Equal(3, knockOff.IEventSource.MessageReceived.SubscribeCount);

    knockOff.IEventSource.MessageReceived.Raise("Test");

    Assert.Equal(3, received.Count);
    Assert.Contains("Handler1: Test", received);
    Assert.Contains("Handler2: Test", received);
    Assert.Contains("Handler3: Test", received);
}
```

## Handler API Reference

| Property/Method | Type | Description |
|----------------|------|-------------|
| `SubscribeCount` | `int` | Times handlers were added |
| `UnsubscribeCount` | `int` | Times handlers were removed |
| `HasSubscribers` | `bool` | True if any handler attached |
| `RaiseCount` | `int` | Times event was raised |
| `WasRaised` | `bool` | True if raised at least once |
| `LastRaiseArgs` | `T?` | Arguments from most recent raise |
| `AllRaises` | `IReadOnlyList<T>` | All recorded raise arguments |
| `Raise(...)` | `void` | Raises event and records args |
| `Reset()` | `void` | Clears tracking, keeps handlers |
| `Clear()` | `void` | Clears tracking and handlers |
