# Working with Events

KnockOff generates event interceptors that let you raise events from your test stubs and verify subscription behavior. Event interceptors support `EventHandler`, `EventHandler<T>`, `Action`, `Action<T>`, and custom delegate types.

---

## Raising Events

### EventHandler and EventHandler&lt;T&gt;

For standard event handler delegates, use the `Raise(sender, args)` method with two parameters.

<!-- snippet: events-raise-eventhandler -->
```cs
// Subscribe to the event through the interface
IEventPub publisher = stub;
publisher.DataReceived += (sender, args) =>
{
    receivedArgs = args;
};

// Raise the event using the interceptor
var eventArgs = new DataEventArgs { Data = "Test Data" };
stub.DataReceived.Raise(stub, eventArgs);
```
<!-- endSnippet -->

### Action and Action&lt;T&gt;

For Action-based events, use the `Raise(arg)` method with a single parameter matching the action's argument type.

<!-- snippet: events-raise-action -->
```cs
IEventPub publisher = stub;
publisher.StatusChanged += status => receivedStatus = status;

// Raise Action<T> event with single argument
stub.StatusChanged.Raise("Connected");
```
<!-- endSnippet -->

---

## Verifying Subscriptions

### Checking for Subscribers

Use `HasSubscribers` to verify whether any handlers are currently subscribed to an event.

<!-- snippet: events-verify-subscribe -->
```cs
// Initially no subscribers
Assert.False(stub.OnCompleted.HasSubscribers);

// Subscribe a handler
subscriber.OnCompleted += (sender, args) => { };

// Now has subscribers
Assert.True(stub.OnCompleted.HasSubscribers);
```
<!-- endSnippet -->

### Counting Add Operations

Use `AddCount` to track how many times handlers have been added to the event.

<!-- snippet: events-verify-addcount -->
```cs
subscriber.OnCompleted += (sender, args) => { };
subscriber.OnCompleted += (sender, args) => { };

// VerifyAdd tracks subscribe operations
stub.OnCompleted.VerifyAdd(Times.Exactly(2));
```
<!-- endSnippet -->

---

## Verifying Unsubscriptions

Use `RemoveCount` to verify how many times handlers have been unsubscribed from the event.

<!-- snippet: events-verify-unsubscribe -->
```cs
subscriber.OnCompleted += handler;
subscriber.OnCompleted -= handler;

// VerifyRemove tracks unsubscribe operations
stub.OnCompleted.VerifyRemove(Times.Once);
```
<!-- endSnippet -->

---

## Resetting Events

The `Reset()` method clears subscription counts and removes all active subscribers. Use this to reset both tracking state and event handlers between test phases.

<!-- snippet: events-reset -->
```cs
// Reset clears counts and subscribers
stub.OnCompleted.Reset();

// Counts are cleared - verify add was never called after reset
stub.OnCompleted.VerifyAdd(Times.Never);

// Subscribers are also cleared
Assert.False(stub.OnCompleted.HasSubscribers);
```
<!-- endSnippet -->

**Important**: After calling `Reset()`, both the tracking counters and subscribers are cleared, so `HasSubscribers` will return `false`.

---

## Complete Example

This example demonstrates the full event interceptor workflow: subscribing handlers, raising events, verifying counts, and checking subscription state.

<!-- snippet: events-complete-example -->
```cs
var stub = new EventPubStub();

DataEventArgs? receivedArgs = null;
int raiseCount = 0;

EventHandler<DataEventArgs> handler = (sender, args) =>
{
    receivedArgs = args;
    raiseCount++;
};

IEventPub publisher = stub;

// Subscribe and verify
publisher.DataReceived += handler;
stub.DataReceived.VerifyAdd(Times.Once);
Assert.True(stub.DataReceived.HasSubscribers);

// Raise the event
var eventArgs = new DataEventArgs { Data = "Test" };
stub.DataReceived.Raise(stub, eventArgs);
Assert.Equal(1, raiseCount);
Assert.Equal("Test", receivedArgs?.Data);

// Unsubscribe and verify
publisher.DataReceived -= handler;
stub.DataReceived.VerifyRemove(Times.Once);
Assert.False(stub.DataReceived.HasSubscribers);
```
<!-- endSnippet -->

---

## Next Steps

- Learn about [method interceptors](methods.md) for verifying method calls
- Explore [property interceptors](properties.md) for property access tracking
- Review [interceptor API reference](../reference/interceptor-api.md) for all available members
