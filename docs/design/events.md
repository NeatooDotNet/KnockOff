# KnockOff Event Support Design

## Overview

Add support for interface events to KnockOff. Events are the primary gap preventing KnockOff from being used with Neatoo's `PropertyChanged`/`NeatooPropertyChanged` patterns.

## Use Cases

### Primary: Raise Events from Tests
```csharp
public interface INotifier
{
    event EventHandler<string> MessageReceived;
}

[KnockOff]
public partial class NotifierKnockOff : INotifier { }

[Fact]
public void Test_EventHandler_ReceivesMessage()
{
    var knockOff = new NotifierKnockOff();
    INotifier notifier = knockOff;

    var received = new List<string>();
    notifier.MessageReceived += (sender, msg) => received.Add(msg);

    // Raise the event from test code
    knockOff.Spy.MessageReceived.Raise(knockOff, "Hello");

    Assert.Single(received);
    Assert.Equal("Hello", received[0]);
}
```

### Secondary: Verify Subscription Tracking
```csharp
[Fact]
public void Test_EventSubscription_IsTracked()
{
    var knockOff = new NotifierKnockOff();
    INotifier notifier = knockOff;

    Assert.False(knockOff.Spy.MessageReceived.HasSubscribers);

    notifier.MessageReceived += (s, e) => { };

    Assert.True(knockOff.Spy.MessageReceived.HasSubscribers);
    Assert.Equal(1, knockOff.Spy.MessageReceived.SubscribeCount);
}
```

## Design

### Event Handler Class Pattern

For each event, generate a handler class following existing patterns:

```csharp
/// <summary>Tracks and configures behavior for MessageReceived.</summary>
public sealed class MessageReceivedHandler
{
    private EventHandler<string>? _handler;
    private readonly List<(object? sender, string e)> _raises = new();

    // === Subscription Tracking ===

    /// <summary>Number of times handlers were added.</summary>
    public int SubscribeCount { get; private set; }

    /// <summary>Number of times handlers were removed.</summary>
    public int UnsubscribeCount { get; private set; }

    /// <summary>True if at least one handler is subscribed.</summary>
    public bool HasSubscribers => _handler != null;

    // === Raise Tracking ===

    /// <summary>Number of times the event was raised.</summary>
    public int RaiseCount => _raises.Count;

    /// <summary>True if the event was raised at least once.</summary>
    public bool WasRaised => _raises.Count > 0;

    /// <summary>Arguments from the most recent raise.</summary>
    public (object? sender, string e)? LastRaiseArgs => _raises.Count > 0 ? _raises[^1] : null;

    /// <summary>All recorded raise invocations with their arguments.</summary>
    public IReadOnlyList<(object? sender, string e)> AllRaises => _raises;

    // === Add/Remove ===

    /// <summary>Adds a handler (called by generated add accessor).</summary>
    internal void Add(EventHandler<string> handler)
    {
        _handler += handler;
        SubscribeCount++;
    }

    /// <summary>Removes a handler (called by generated remove accessor).</summary>
    internal void Remove(EventHandler<string> handler)
    {
        _handler -= handler;
        UnsubscribeCount++;
    }

    // === Raise ===

    /// <summary>Raises the event with the specified sender and args.</summary>
    public void Raise(object? sender, string e)
    {
        _raises.Add((sender, e));
        _handler?.Invoke(sender, e);
    }

    /// <summary>Raises the event with null sender.</summary>
    public void Raise(string e) => Raise(null, e);

    // === Reset ===

    /// <summary>Resets all tracking (subscriptions and raises).</summary>
    public void Reset()
    {
        SubscribeCount = 0;
        UnsubscribeCount = 0;
        _raises.Clear();
    }

    /// <summary>Clears all handlers and resets tracking.</summary>
    public void Clear()
    {
        _handler = null;
        Reset();
    }
}
```

### Explicit Interface Implementation

```csharp
event EventHandler<string> INotifier.MessageReceived
{
    add => Spy.MessageReceived.Add(value);
    remove => Spy.MessageReceived.Remove(value);
}
```

### Spy Class Property

```csharp
public sealed class NotifierKnockOffSpy
{
    public MessageReceivedHandler MessageReceived { get; } = new();
}
```

## Delegate Type Variations

C# events support various delegate types. Each requires different `Raise` signatures and tracking tuple shapes.

### 1. EventHandler (standard pattern)
```csharp
event EventHandler MyEvent;
```
**Tracking:** `(object? sender, EventArgs e)` tuple
```csharp
public (object? sender, EventArgs e)? LastRaiseArgs { get; }
public IReadOnlyList<(object? sender, EventArgs e)> AllRaises { get; }

public void Raise(object? sender, EventArgs e);
public void Raise(); // Convenience: null sender, EventArgs.Empty
```

### 2. EventHandler<TEventArgs> (generic pattern)
```csharp
event EventHandler<string> MessageReceived;
event EventHandler<PropertyChangedEventArgs> PropertyChanged;
```
**Tracking:** `(object? sender, TEventArgs e)` tuple
```csharp
public (object? sender, TEventArgs e)? LastRaiseArgs { get; }
public IReadOnlyList<(object? sender, TEventArgs e)> AllRaises { get; }

public void Raise(object? sender, TEventArgs e);
public void Raise(TEventArgs e); // Convenience: null sender
```

### 3. Action delegates
```csharp
event Action OnComplete;
event Action<int> OnProgress;
event Action<string, int> OnData;
```
**Tracking:** Single value for one param, tuple for multiple
```csharp
// Action - no args to track
public int RaiseCount { get; }
public void Raise();

// Action<T> - single value (no tuple needed)
public T? LastRaiseArgs { get; }
public IReadOnlyList<T> AllRaises { get; }
public void Raise(T arg);

// Action<T1, T2> - tuple
public (T1, T2)? LastRaiseArgs { get; }
public IReadOnlyList<(T1, T2)> AllRaises { get; }
public void Raise(T1 arg1, T2 arg2);
```

### 4. Async delegates (Neatoo pattern)
```csharp
public delegate Task AsyncEventHandler<TArgs>(TArgs args);
event AsyncEventHandler<PropertyChangedEventArgs> NeatooPropertyChanged;
```
**Tracking:** Same as sync, but `RaiseAsync` instead of `Raise`
```csharp
public TArgs? LastRaiseArgs { get; }
public IReadOnlyList<TArgs> AllRaises { get; }

/// <summary>Raises the event and awaits all handlers sequentially.</summary>
public async Task RaiseAsync(TArgs args)
{
    _raises.Add(args);
    if (_handler == null) return;

    // Multicast delegates need special handling for async
    foreach (var handler in _handler.GetInvocationList())
    {
        await ((AsyncEventHandler<TArgs>)handler)(args);
    }
}
```

### 5. Func delegates (rare for events)
```csharp
event Func<int, bool> OnValidate;
```
**Tracking:** Args only (return value from last handler)
```csharp
public int? LastRaiseArgs { get; }
public IReadOnlyList<int> AllRaises { get; }

// Returns result of last handler (multicast behavior)
public bool Raise(int arg);
```

## Model Changes

### New: EventMemberInfo Record
```csharp
internal sealed record EventMemberInfo(
    string Name,
    string DelegateTypeName,           // e.g., "EventHandler<string>"
    string FullDelegateTypeName,       // Fully qualified
    EventDelegateKind DelegateKind,    // EventHandler, EventHandlerOfT, Action, Func, Custom
    EquatableArray<ParameterInfo> DelegateParameters,  // Parameters of Invoke method
    string? ReturnTypeName,            // For Func delegates; null for void/Action
    bool IsAsync                       // True if delegate returns Task/ValueTask
) : IEquatable<EventMemberInfo>;

internal enum EventDelegateKind
{
    EventHandler,           // System.EventHandler
    EventHandlerOfT,        // System.EventHandler<TEventArgs>
    Action,                 // System.Action or Action<T...>
    Func,                   // System.Func<..., TResult>
    Custom                  // Custom delegate type
}
```

### Updated: InterfaceInfo
```csharp
internal sealed record InterfaceInfo(
    string FullName,
    string SimpleName,
    EquatableArray<InterfaceMemberInfo> Members,
    EquatableArray<EventMemberInfo> Events  // NEW
) : IEquatable<InterfaceInfo>;
```

## Generator Changes

### 1. Transform: Extract Events
```csharp
foreach (var member in iface.GetMembers())
{
    if (member is IEventSymbol eventSymbol)
    {
        events.Add(CreateEventInfo(eventSymbol));
    }
    // ... existing property/method handling
}
```

### 2. CreateEventInfo Method
```csharp
private static EventMemberInfo CreateEventInfo(IEventSymbol eventSymbol)
{
    var delegateType = (INamedTypeSymbol)eventSymbol.Type;
    var invokeMethod = delegateType.DelegateInvokeMethod!;

    var delegateKind = ClassifyDelegateKind(delegateType);
    var isAsync = IsAsyncDelegate(invokeMethod);

    var parameters = invokeMethod.Parameters
        .Select(p => new ParameterInfo(p.Name, p.Type.ToDisplayString(FullyQualifiedWithNullability)))
        .ToArray();

    var returnType = invokeMethod.ReturnsVoid ? null
        : invokeMethod.ReturnType.ToDisplayString(FullyQualifiedWithNullability);

    return new EventMemberInfo(
        Name: eventSymbol.Name,
        DelegateTypeName: delegateType.Name,
        FullDelegateTypeName: delegateType.ToDisplayString(FullyQualifiedWithNullability),
        DelegateKind: delegateKind,
        DelegateParameters: new EquatableArray<ParameterInfo>(parameters),
        ReturnTypeName: returnType,
        IsAsync: isAsync);
}
```

### 3. Generate Event Handler Class
```csharp
private static void GenerateEventHandlerClass(
    StringBuilder sb,
    EventMemberInfo evt,
    string className)
{
    sb.AppendLine($"\t/// <summary>Tracks and raises {evt.Name}.</summary>");
    sb.AppendLine($"\tpublic sealed class {evt.Name}Handler");
    sb.AppendLine("\t{");

    // Private handler field
    sb.AppendLine($"\t\tprivate {evt.FullDelegateTypeName}? _handler;");
    sb.AppendLine();

    // Tracking properties
    sb.AppendLine("\t\t/// <summary>Number of times handlers were added.</summary>");
    sb.AppendLine("\t\tpublic int SubscribeCount { get; private set; }");
    sb.AppendLine();
    sb.AppendLine("\t\t/// <summary>Number of times handlers were removed.</summary>");
    sb.AppendLine("\t\tpublic int UnsubscribeCount { get; private set; }");
    sb.AppendLine();
    sb.AppendLine("\t\t/// <summary>True if at least one handler is subscribed.</summary>");
    sb.AppendLine("\t\tpublic bool HasSubscribers => _handler != null;");
    sb.AppendLine();

    // Add/Remove methods
    sb.AppendLine("\t\t/// <summary>Adds a handler.</summary>");
    sb.AppendLine($"\t\tinternal void Add({evt.FullDelegateTypeName} handler)");
    sb.AppendLine("\t\t{");
    sb.AppendLine("\t\t\t_handler += handler;");
    sb.AppendLine("\t\t\tSubscribeCount++;");
    sb.AppendLine("\t\t}");
    sb.AppendLine();

    sb.AppendLine("\t\t/// <summary>Removes a handler.</summary>");
    sb.AppendLine($"\t\tinternal void Remove({evt.FullDelegateTypeName} handler)");
    sb.AppendLine("\t\t{");
    sb.AppendLine("\t\t\t_handler -= handler;");
    sb.AppendLine("\t\t\tUnsubscribeCount++;");
    sb.AppendLine("\t\t}");
    sb.AppendLine();

    // Raise method(s) - varies by delegate kind
    GenerateRaiseMethods(sb, evt, className);

    // Reset and Clear
    sb.AppendLine("\t\t/// <summary>Resets tracking counters.</summary>");
    sb.AppendLine("\t\tpublic void Reset() { SubscribeCount = 0; UnsubscribeCount = 0; }");
    sb.AppendLine();
    sb.AppendLine("\t\t/// <summary>Clears all handlers and resets tracking.</summary>");
    sb.AppendLine("\t\tpublic void Clear() { _handler = null; Reset(); }");

    sb.AppendLine("\t}");
    sb.AppendLine();
}
```

### 4. Generate Raise Methods
```csharp
private static void GenerateRaiseMethods(StringBuilder sb, EventMemberInfo evt, string className)
{
    var paramList = string.Join(", ", evt.DelegateParameters.Select(p => $"{p.Type} {p.Name}"));
    var argList = string.Join(", ", evt.DelegateParameters.Select(p => p.Name));

    if (evt.IsAsync)
    {
        // Async: iterate through invocation list
        sb.AppendLine($"\t\t/// <summary>Raises the event and awaits all handlers sequentially.</summary>");
        sb.AppendLine($"\t\tpublic async global::System.Threading.Tasks.Task RaiseAsync({paramList})");
        sb.AppendLine("\t\t{");
        sb.AppendLine("\t\t\tif (_handler == null) return;");
        sb.AppendLine($"\t\t\tforeach (var h in _handler.GetInvocationList())");
        sb.AppendLine($"\t\t\t\tawait (({evt.FullDelegateTypeName})h)({argList});");
        sb.AppendLine("\t\t}");
    }
    else if (evt.ReturnTypeName != null)
    {
        // Func: return result of invocation
        sb.AppendLine($"\t\t/// <summary>Raises the event and returns the result.</summary>");
        sb.AppendLine($"\t\tpublic {evt.ReturnTypeName} Raise({paramList})");
        sb.AppendLine($"\t\t\t=> _handler?.Invoke({argList}) ?? default!;");
    }
    else
    {
        // Action/EventHandler: simple invocation
        sb.AppendLine($"\t\t/// <summary>Raises the event.</summary>");
        sb.AppendLine($"\t\tpublic void Raise({paramList}) => _handler?.Invoke({argList});");
    }
    sb.AppendLine();

    // Convenience overloads for EventHandler patterns
    if (evt.DelegateKind == EventDelegateKind.EventHandler)
    {
        sb.AppendLine("\t\t/// <summary>Raises the event with null sender and empty args.</summary>");
        sb.AppendLine("\t\tpublic void Raise() => Raise(null, global::System.EventArgs.Empty);");
        sb.AppendLine();
    }
    else if (evt.DelegateKind == EventDelegateKind.EventHandlerOfT)
    {
        // Extract TEventArgs from parameters (second parameter after sender)
        var eventArgsParam = evt.DelegateParameters.GetArray()![1];
        sb.AppendLine($"\t\t/// <summary>Raises the event with null sender.</summary>");
        sb.AppendLine($"\t\tpublic void Raise({eventArgsParam.Type} e) => Raise(null, e);");
        sb.AppendLine();
    }
}
```

### 5. Generate Event Implementation
```csharp
private static void GenerateEventImplementation(
    StringBuilder sb,
    string interfaceName,
    EventMemberInfo evt)
{
    sb.AppendLine($"\tevent {evt.FullDelegateTypeName} {interfaceName}.{evt.Name}");
    sb.AppendLine("\t{");
    sb.AppendLine($"\t\tadd => Spy.{evt.Name}.Add(value);");
    sb.AppendLine($"\t\tremove => Spy.{evt.Name}.Remove(value);");
    sb.AppendLine("\t}");
    sb.AppendLine();
}
```

## Implementation Phases

### Phase 1: Basic EventHandler Support
- [ ] Add `EventMemberInfo` record
- [ ] Update `InterfaceInfo` to include events
- [ ] Implement `CreateEventInfo` for `IEventSymbol`
- [ ] Classify `EventHandler` and `EventHandler<T>` delegate kinds
- [ ] Generate event handler class for `EventHandler<T>`
- [ ] Generate explicit interface event implementation
- [ ] Add event handlers to Spy class
- [ ] Write unit tests for `EventHandler<T>` events

### Phase 2: Action Delegate Events
- [ ] Classify `Action`, `Action<T>`, `Action<T1,T2>`, etc.
- [ ] Generate appropriate `Raise` methods
- [ ] Write unit tests for Action-based events

### Phase 3: Async Delegate Events
- [ ] Detect Task/ValueTask return types
- [ ] Generate `RaiseAsync` with invocation list iteration
- [ ] Write unit tests for async events (Neatoo pattern)

### Phase 4: Func and Custom Delegates
- [ ] Classify `Func<..., TResult>` delegates
- [ ] Handle custom delegate types
- [ ] Write unit tests for edge cases

## Test Cases

### Basic EventHandler<T>
```csharp
public interface IBasicEvents
{
    event EventHandler<string> MessageReceived;
    event EventHandler OnComplete;
}

[Fact]
public void EventHandlerOfT_RaiseWithSender()
{
    var ko = new BasicEventsKnockOff();
    object? capturedSender = null;
    string? capturedArg = null;

    ((IBasicEvents)ko).MessageReceived += (s, e) => { capturedSender = s; capturedArg = e; };

    ko.Spy.MessageReceived.Raise(ko, "test");

    // Verify handler received args
    Assert.Same(ko, capturedSender);
    Assert.Equal("test", capturedArg);

    // Verify raise was tracked
    Assert.Equal(1, ko.Spy.MessageReceived.RaiseCount);
    Assert.True(ko.Spy.MessageReceived.WasRaised);
    Assert.Equal((ko, "test"), ko.Spy.MessageReceived.LastRaiseArgs);
}

[Fact]
public void EventHandlerOfT_TracksMultipleRaises()
{
    var ko = new BasicEventsKnockOff();

    ko.Spy.MessageReceived.Raise(ko, "first");
    ko.Spy.MessageReceived.Raise(ko, "second");
    ko.Spy.MessageReceived.Raise(null, "third");

    Assert.Equal(3, ko.Spy.MessageReceived.RaiseCount);
    Assert.Equal((null, "third"), ko.Spy.MessageReceived.LastRaiseArgs);

    // AllRaises gives full history
    Assert.Equal(3, ko.Spy.MessageReceived.AllRaises.Count);
    Assert.Equal("first", ko.Spy.MessageReceived.AllRaises[0].e);
    Assert.Equal("second", ko.Spy.MessageReceived.AllRaises[1].e);
}
```

### Action Events
```csharp
public interface IActionEvents
{
    event Action OnVoid;
    event Action<int> OnProgress;
    event Action<string, int> OnData;
}

[Fact]
public void ActionEvent_RaiseWithArgs()
{
    var ko = new ActionEventsKnockOff();
    int? progress = null;

    ((IActionEvents)ko).OnProgress += p => progress = p;

    ko.Spy.OnProgress.Raise(50);
    ko.Spy.OnProgress.Raise(75);

    // Handler received last value
    Assert.Equal(75, progress);

    // All raises tracked
    Assert.Equal(2, ko.Spy.OnProgress.RaiseCount);
    Assert.Equal(75, ko.Spy.OnProgress.LastRaiseArgs);
    Assert.Equal([50, 75], ko.Spy.OnProgress.AllRaises);
}

[Fact]
public void ActionEvent_MultipleParams_TracksAsTuple()
{
    var ko = new ActionEventsKnockOff();

    ko.Spy.OnData.Raise("test", 42);

    Assert.Equal(1, ko.Spy.OnData.RaiseCount);
    Assert.Equal(("test", 42), ko.Spy.OnData.LastRaiseArgs);
}
```

### Async Events (Neatoo Pattern)
```csharp
public delegate Task AsyncPropertyHandler(string propertyName);

public interface IAsyncEvents
{
    event AsyncPropertyHandler PropertyChanged;
}

[Fact]
public async Task AsyncEvent_RaiseAwaitsAllHandlers()
{
    var ko = new AsyncEventsKnockOff();
    var order = new List<int>();

    ((IAsyncEvents)ko).PropertyChanged += async (name) =>
    {
        await Task.Delay(10);
        order.Add(1);
    };
    ((IAsyncEvents)ko).PropertyChanged += async (name) =>
    {
        order.Add(2);
    };

    await ko.Spy.PropertyChanged.RaiseAsync("Test");

    // Sequential execution: first handler completes before second starts
    Assert.Equal([1, 2], order);
}
```

### Subscription Tracking
```csharp
[Fact]
public void EventSubscription_TracksAddAndRemove()
{
    var ko = new BasicEventsKnockOff();
    EventHandler<string> handler = (s, e) => { };

    Assert.Equal(0, ko.Spy.MessageReceived.SubscribeCount);
    Assert.False(ko.Spy.MessageReceived.HasSubscribers);

    ((IBasicEvents)ko).MessageReceived += handler;

    Assert.Equal(1, ko.Spy.MessageReceived.SubscribeCount);
    Assert.True(ko.Spy.MessageReceived.HasSubscribers);

    ((IBasicEvents)ko).MessageReceived -= handler;

    Assert.Equal(1, ko.Spy.MessageReceived.UnsubscribeCount);
    Assert.False(ko.Spy.MessageReceived.HasSubscribers);
}
```

## Documentation Updates

After implementation, update:
- [ ] README.md - Add events to feature table
- [ ] docs/knockoff-vs-moq.md - Add event comparison section
- [ ] docs/getting-started.md - Add event example

## Open Questions

1. **Thread safety**: Event add/remove can race with Raise. For test stubs, this is usually not a concern. Document that KnockOff stubs are not thread-safe.

2. **Clear vs Reset**:
   - `Reset()` - Only clears tracking counters, leaves handlers attached
   - `Clear()` - Clears handlers AND tracking
   - Is this distinction useful, or should we just have one method?

3. **Parallel async raising**: Should we offer `RaiseParallelAsync` that uses `Task.WhenAll`? Probably overkill for v1.

4. **Return value for multicast Func events**: Multicast delegates only return the last handler's result. Document this limitation.
