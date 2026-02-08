[Home](../../README.md) / [Guides](../guides/) / Events

# Working with Events

KnockOff generates event interceptors that let you raise events from your test stubs and verify subscription behavior. Event interceptors support `EventHandler`, `EventHandler<T>`, `Action`, `Action<T>`, and custom delegate types.

---

## Raising Events

### EventHandler and EventHandler&lt;T&gt;

For standard event handler delegates, use the `Raise(sender, args)` method with two parameters.

<!-- snippet: events-raise-eventhandler -->
```cs
// Raise EventHandler<T> event with sender and args
stub.DataReceived.Raise(stub, new DataEventArgs { Data = "Test Data" });
```
<!-- endSnippet -->

### Action and Action&lt;T&gt;

For Action-based events, use the `Raise(arg)` method with a single parameter matching the action's argument type.

<!-- snippet: events-raise-action -->
```cs
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
// Check if any handlers are subscribed
var hasHandlers = stub.OnCompleted.HasSubscribers;
```
<!-- endSnippet -->

### Verifying Subscribe Operations

Use `VerifyAdd` to verify how many times handlers have been subscribed to the event.

<!-- snippet: events-verify-addcount -->
```cs
// Verify how many times handlers were subscribed
stub.OnCompleted.VerifyAdd(Called.Exactly(2));
```
<!-- endSnippet -->

---

## Verifying Unsubscriptions

Use `VerifyRemove` to verify how many times handlers have been unsubscribed from the event.

<!-- snippet: events-verify-unsubscribe -->
```cs
// Verify how many times handlers were unsubscribed
stub.OnCompleted.VerifyRemove(Called.Once);
```
<!-- endSnippet -->

---

## Batch Verification with Verifiable

Mark event interceptors with `Verifiable()` to include them in batch verification via `stub.Verify()`. This allows verifying multiple members at once instead of calling individual `Verify()` methods.

<!-- snippet: events-verifiable -->
```cs
// Mark event for batch verification (expects at least one add/remove)
stub.OnCompleted.Verifiable();
```
<!-- endSnippet -->

**What Verifiable tracks**: The total event access count (add + remove operations combined). If you need to verify subscriptions and unsubscriptions separately, use `VerifyAdd(Called)` or `VerifyRemove(Called)` directly instead.

**Default behavior**: Calling `.Verifiable()` without arguments marks the event to be verified with `Called.AtLeastOnce` when `stub.Verify()` is called.

---

## Resetting Events

The `Reset()` method clears subscription counts and removes all active subscribers. Use this to reset both tracking state and event handlers between test phases.

<!-- snippet: events-reset -->
```cs
// Clear all tracking counts and remove all subscribers
stub.OnCompleted.Reset();
```
<!-- endSnippet -->

**Important**: After calling `Reset()`, both the tracking counters and active subscribers are cleared, so `HasSubscribers` will return `false`.

---

## Complete Example

This example demonstrates the full event interceptor workflow: subscribing handlers, raising events, verifying counts, and checking subscription state.

<!-- snippet: events-complete-example -->
```cs
// Subscribe through the interface
publisher.DataReceived += handler;
stub.DataReceived.VerifyAdd(Called.Once);

// Raise the event from the stub
stub.DataReceived.Raise(stub, new DataEventArgs { Data = "Test" });

// Unsubscribe and verify
publisher.DataReceived -= handler;
stub.DataReceived.VerifyRemove(Called.Once);
```
<!-- endSnippet -->

---

## Next Steps

- Learn about [method interceptors](methods.md) for verifying method calls
- Explore [property interceptors](properties.md) for property access tracking
- Review [interceptor API reference](../reference/interceptor-api.md) for all available members

---

**UPDATED:** 2026-01-25
