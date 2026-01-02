# Events

KnockOff supports interface events with full subscription tracking and programmatic raising.

## Basic Usage

```csharp
public interface IEventSource
{
    event EventHandler<string> MessageReceived;
    event EventHandler OnCompleted;
    event Action<int> OnProgress;
    event Action<string, int> OnData;
}

[KnockOff]
public partial class EventSourceKnockOff : IEventSource { }
```

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
Assert.False(knockOff.Spy.MessageReceived.HasSubscribers);
Assert.Equal(0, knockOff.Spy.MessageReceived.SubscribeCount);

// Subscribe
EventHandler<string> handler = (sender, e) => Console.WriteLine(e);
source.MessageReceived += handler;

Assert.True(knockOff.Spy.MessageReceived.HasSubscribers);
Assert.Equal(1, knockOff.Spy.MessageReceived.SubscribeCount);

// Unsubscribe
source.MessageReceived -= handler;

Assert.Equal(1, knockOff.Spy.MessageReceived.UnsubscribeCount);
```

## Raising Events

### EventHandler<T>

```csharp
var knockOff = new EventSourceKnockOff();
IEventSource source = knockOff;

string? received = null;
source.MessageReceived += (sender, e) => received = e;

// Raise with sender
knockOff.Spy.MessageReceived.Raise(knockOff, "Hello");

// Or raise with null sender (convenience)
knockOff.Spy.MessageReceived.Raise("World");

Assert.Equal("World", received);
```

### EventHandler (no type parameter)

```csharp
var invoked = false;
source.OnCompleted += (sender, e) => invoked = true;

// Raise with no arguments (uses null sender, EventArgs.Empty)
knockOff.Spy.OnCompleted.Raise();

Assert.True(invoked);
```

### Action<T>

```csharp
int? progress = null;
source.OnProgress += (value) => progress = value;

knockOff.Spy.OnProgress.Raise(75);

Assert.Equal(75, progress);
```

### Action<T1, T2>

```csharp
string? name = null;
int? value = null;
source.OnData += (n, v) => { name = n; value = v; };

knockOff.Spy.OnData.Raise("Temperature", 72);

Assert.Equal("Temperature", name);
Assert.Equal(72, value);
```

## Raise Tracking

Every raise is tracked with arguments:

```csharp
knockOff.Spy.MessageReceived.Raise("First");
knockOff.Spy.MessageReceived.Raise("Second");
knockOff.Spy.MessageReceived.Raise("Third");

// Count and boolean
Assert.Equal(3, knockOff.Spy.MessageReceived.RaiseCount);
Assert.True(knockOff.Spy.MessageReceived.WasRaised);

// Last raise arguments (tuple for EventHandler<T>)
var lastArgs = knockOff.Spy.MessageReceived.LastRaiseArgs;
Assert.Null(lastArgs?.sender);  // null sender from Raise(string) overload
Assert.Equal("Third", lastArgs?.e);

// All raises
var allRaises = knockOff.Spy.MessageReceived.AllRaises;
Assert.Equal("First", allRaises[0].e);
Assert.Equal("Second", allRaises[1].e);
Assert.Equal("Third", allRaises[2].e);
```

### Multi-Parameter Action Tracking

```csharp
knockOff.Spy.OnData.Raise("Temp", 72);
knockOff.Spy.OnData.Raise("Humidity", 45);

// Named tuple access
var last = knockOff.Spy.OnData.LastRaiseArgs;
Assert.Equal("Humidity", last?.arg1);
Assert.Equal(45, last?.arg2);

// All raises as tuples
var all = knockOff.Spy.OnData.AllRaises;
Assert.Equal(("Temp", 72), all[0]);
Assert.Equal(("Humidity", 45), all[1]);
```

## Reset vs Clear

### Reset

Clears tracking counters but **keeps handlers attached**:

```csharp
var invoked = false;
source.MessageReceived += (s, e) => invoked = true;

knockOff.Spy.MessageReceived.Raise("Before");
Assert.Equal(1, knockOff.Spy.MessageReceived.RaiseCount);

knockOff.Spy.MessageReceived.Reset();

// Tracking cleared
Assert.Equal(0, knockOff.Spy.MessageReceived.SubscribeCount);
Assert.Equal(0, knockOff.Spy.MessageReceived.RaiseCount);
Assert.Empty(knockOff.Spy.MessageReceived.AllRaises);

// But handlers still work!
invoked = false;
knockOff.Spy.MessageReceived.Raise("After");
Assert.True(invoked);
```

### Clear

Clears **both tracking and handlers**:

```csharp
var invoked = false;
source.MessageReceived += (s, e) => invoked = true;

knockOff.Spy.MessageReceived.Raise("Before");
Assert.True(invoked);

knockOff.Spy.MessageReceived.Clear();

// Tracking cleared
Assert.Equal(0, knockOff.Spy.MessageReceived.RaiseCount);
Assert.False(knockOff.Spy.MessageReceived.HasSubscribers);

// Handlers also cleared - raise does nothing
invoked = false;
knockOff.Spy.MessageReceived.Raise("After");
Assert.False(invoked);  // Handler was cleared
```

## No Subscribers Safe

Raising an event with no subscribers does not throw:

```csharp
var knockOff = new EventSourceKnockOff();

// No exception, just tracked
knockOff.Spy.MessageReceived.Raise("No one listening");

Assert.True(knockOff.Spy.MessageReceived.WasRaised);
Assert.Equal(1, knockOff.Spy.MessageReceived.RaiseCount);
```

## Common Patterns

### Testing Event Handlers

```csharp
[Fact]
public void ViewModel_SubscribesToDataService_Events()
{
    var knockOff = new DataServiceKnockOff();
    var viewModel = new MyViewModel(knockOff.AsDataService());

    // ViewModel should have subscribed
    Assert.True(knockOff.Spy.DataChanged.HasSubscribers);
    Assert.Equal(1, knockOff.Spy.DataChanged.SubscribeCount);
}

[Fact]
public void ViewModel_UpdatesOnDataChanged()
{
    var knockOff = new DataServiceKnockOff();
    var viewModel = new MyViewModel(knockOff.AsDataService());

    // Simulate data change
    knockOff.Spy.DataChanged.Raise(new DataChangedEventArgs { NewValue = 42 });

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

    Assert.Equal(1, knockOff.Spy.DataChanged.SubscribeCount);

    viewModel.Dispose();

    Assert.Equal(1, knockOff.Spy.DataChanged.UnsubscribeCount);
}
```

### Progress Reporting

```csharp
[Fact]
public async Task Downloader_ReportsProgress()
{
    var knockOff = new DownloaderKnockOff();
    var progressValues = new List<int>();

    knockOff.AsDownloader().ProgressChanged += (p) => progressValues.Add(p);

    // Simulate download progress
    knockOff.Spy.ProgressChanged.Raise(0);
    knockOff.Spy.ProgressChanged.Raise(25);
    knockOff.Spy.ProgressChanged.Raise(50);
    knockOff.Spy.ProgressChanged.Raise(75);
    knockOff.Spy.ProgressChanged.Raise(100);

    Assert.Equal([0, 25, 50, 75, 100], progressValues);
    Assert.Equal(5, knockOff.Spy.ProgressChanged.RaiseCount);
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

    Assert.Equal(3, knockOff.Spy.MessageReceived.SubscribeCount);

    knockOff.Spy.MessageReceived.Raise("Test");

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
