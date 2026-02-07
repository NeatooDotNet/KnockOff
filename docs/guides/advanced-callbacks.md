[Guides](../README.md#guides) > Advanced Callback Patterns

# Advanced Callback Patterns

When simple `Returns`/`Execute` configuration isn't enough, callbacks give you complete control over stub behavior. This guide covers practical patterns for sequential returns, conditional logic, exceptions, state tracking, and side effects.

## When You Need Advanced Callbacks

Use these patterns when your tests need to simulate:
- Dependencies that return different values on successive calls
- Behavior that varies based on input arguments
- Methods that throw exceptions under certain conditions
- Stateful dependencies where one method affects another
- Side effects like logging, notifications, or external system calls

Most examples use local variables captured by closures—this is C#'s natural way to maintain state between callback invocations. The callback "remembers" variables from its surrounding scope, letting you track counters, queues, or any other state across multiple calls.

---

## Sequential Returns

### Using a Queue

Return different values on successive calls by maintaining a queue:

<!-- snippet: advanced-sequential-queue -->
```cs
// Queue of results: first succeeds, second fails
var results = new Queue<bool>(new[] { true, false });
stub.Send.Returns((to, message) => results.Dequeue());
```
<!-- endSnippet -->

### Using a Counter

Control behavior based on call count using a simple counter:

<!-- snippet: advanced-sequential-counter -->
```cs
// Counter tracks call count for conditional behavior
var attempts = 0;
stub.Attempt.Returns(() =>
{
    attempts++;
    return attempts > 3; // Succeed on 4th attempt
});
```
<!-- endSnippet -->

---

## Conditional Returns

Return different values based on method arguments using pattern matching:

<!-- snippet: advanced-conditional-switch -->
```cs
// Pattern matching for argument-based return values
stub.FindById.Returns((id) => id switch
{
    1 => new User { Id = 1, Name = "Admin", Email = "admin@test.com" },
    2 => new User { Id = 2, Name = "User", Email = "user@test.com" },
    _ => null
});
```
<!-- endSnippet -->

---

## Throwing Exceptions

Simulate error conditions by throwing exceptions from callbacks:

<!-- snippet: advanced-exception -->
```cs
// Throw exceptions based on argument conditions
stub.Charge.Execute((amount) =>
{
    if (amount > 1000)
        throw new PaymentException("Insufficient funds");
});
```
<!-- endSnippet -->

---

## State-Dependent Behavior

### Property Depends on Method Call

Use interceptor state to make one member's behavior depend on another:

<!-- snippet: advanced-state-property -->
```cs
// Shared state between property and method
var isConnected = false;
stub.IsConnected.OnGet(() => isConnected);
stub.Connect.Execute(() => { isConnected = true; });
```
<!-- endSnippet -->

### Method Throws If Not Initialized

Enforce ordering requirements by checking state in callbacks:

<!-- snippet: advanced-state-method -->
```cs
// Enforce method ordering with shared state
var isInitialized = false;
stub.Initialize.Execute(() => { isInitialized = true; });
stub.Query.Returns((sql) =>
{
    if (!isInitialized)
        throw new InvalidOperationException("Must call Initialize() first");
    return "result";
});
```
<!-- endSnippet -->

---

## Side Effects

Callbacks can perform actions beyond returning values. Use this to simulate dependencies that trigger notifications, log events, or modify external state:

<!-- snippet: advanced-side-effects -->
```cs
// Callbacks can track state and perform side effects
stub.PlaceOrder.Returns((order) =>
{
    placedOrders.Add(order);
    notifications.Add($"Order {nextOrderId} placed for user {order.UserId}");
    return nextOrderId++;
});
```
<!-- endSnippet -->

---

## Complete Example: Simulating a Stateful Cache

This example combines multiple patterns to create a realistic cache simulation with expiration, capacity limits, and hit/miss tracking:

<!-- snippet: advanced-complete-example -->
```cs
// Get: Check expiration, track hits/misses
stub.Get.Returns((key) =>
{
    if (cache.TryGetValue(key, out var entry))
    {
        if ((DateTime.UtcNow - entry.Added).TotalSeconds < expirationSeconds)
        {
            hits++;
            return entry.Value;
        }
        cache.Remove(key); // Expired
    }
    misses++;
    return null;
});

// Set: Enforce capacity, evict oldest if needed
stub.Set.Execute((key, value) =>
{
    if (cache.Count >= maxCapacity && !cache.ContainsKey(key))
    {
        var oldest = cache.OrderBy(kvp => kvp.Value.Added).First();
        cache.Remove(oldest.Key);
    }
    cache[key] = (value, DateTime.UtcNow);
});

// Clear: Reset everything
stub.Clear.Execute(() =>
{
    cache.Clear();
    hits = 0;
    misses = 0;
});

// Stats: Return current counts
stub.Stats.OnGet(() => new CacheStats { Hits = hits, Misses = misses });
```
<!-- endSnippet -->

---

## Key Takeaways

- **Callbacks have full control**: Any logic you can write in C# works in a callback
- **Closures capture context**: Use local variables and collections to maintain state across calls—the callback captures these variables from the surrounding scope, allowing you to track state between calls
- **Think like the dependency**: Model realistic behavior, not just happy paths
- **Keep tests readable**: Complex callbacks might indicate your test is doing too much—consider splitting it

These patterns let you simulate sophisticated dependency behavior without needing heavyweight mocking frameworks or test doubles.

---

**UPDATED:** 2026-01-25
