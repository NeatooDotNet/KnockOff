# Async Patterns

KnockOff fully supports async methods, allowing you to configure `Task<T>` and `ValueTask<T>` return values using the `OnCall` API. You can use either the **value overload** (recommended for simple values) or the **callback overload** (for dynamic logic). This guide shows common async patterns for unit testing.

**See also:**
- [Method Interceptors](methods.md) - Core `OnCall` callback patterns
- [Verification Guide](verification.md) - Details on `Verifiable()` and `stub.Verify()`

---

## Task<T> Methods

### Value Overload (Recommended)

For simple async values, KnockOff auto-wraps values in `Task.FromResult`:

<!-- snippet: async-task-value-overload -->
```cs
[Fact]
public async Task TaskResult_ValueOverload_AutoWraps()
{
    var stub = new AsyncUserSvcStub();

    // VALUE OVERLOAD: KnockOff auto-wraps the value in Task.FromResult
    // This is the simplest syntax for returning async values
    stub.GetUserAsync.OnCall(new User { Id = 42, Name = "Alice" });

    IAsyncUserSvc service = stub;
    var user = await service.GetUserAsync(42);

    Assert.NotNull(user);
    Assert.Equal("Alice", user.Name);
}
```
<!-- endSnippet -->

This is the simplest syntax when you don't need dynamic logic based on parameters.

### Callback Overload

For async methods where you need dynamic logic or parameter-based values, use `Task.FromResult` in the callback:

<!-- snippet: async-task-result -->
```cs
[Fact]
public async Task TaskResult_ReturnedWithFromResult()
{
    var stub = new AsyncUserSvcStub();

    // FULL CALLBACK: Use Task.FromResult when you need async operations in the callback
    stub.GetUserAsync.OnCall((id) =>
        Task.FromResult<User?>(new User { Id = id, Name = "Alice" })).Verifiable();

    IAsyncUserSvc service = stub;
    var user = await service.GetUserAsync(42);

    Assert.NotNull(user);
    Assert.Equal("Alice", user.Name);
    stub.Verify();
}
```
<!-- endSnippet -->

For async methods with no return value (`Task` instead of `Task<T>`), use `Task.CompletedTask`:

<!-- snippet: async-task-void -->
```cs
[Fact]
public async Task TaskVoid_ReturnedWithCompletedTask()
{
    var stub = new AsyncUserSvcStub();

    var updatedUsers = new List<User>();

    // Use Task.CompletedTask for async void methods
    stub.UpdateUserAsync.OnCall((user) =>
    {
        updatedUsers.Add(user);
        return Task.CompletedTask;
    }).Verifiable();

    IAsyncUserSvc service = stub;
    await service.UpdateUserAsync(new User { Id = 1, Name = "Bob" });

    Assert.Single(updatedUsers);
    stub.Verify();
}
```
<!-- endSnippet -->

---

## ValueTask<T> Methods

### Value Overload (Recommended)

For simple ValueTask values, KnockOff auto-wraps values in `ValueTask<T>`:

<!-- snippet: async-valuetask-value-overload -->
```cs
[Fact]
public async Task ValueTaskResult_ValueOverload_AutoWraps()
{
    var stub = new AsyncUserSvcStub();

    // VALUE OVERLOAD: KnockOff auto-wraps the value in new ValueTask<T>(value)
    // This is the simplest syntax for returning async values
    stub.GetCachedUserAsync.OnCall(new User { Id = 42, Name = "Cached" });

    IAsyncUserSvc service = stub;
    var user = await service.GetCachedUserAsync(42);

    Assert.NotNull(user);
    Assert.Equal("Cached", user.Name);
}
```
<!-- endSnippet -->

This is the simplest syntax when you don't need dynamic logic based on parameters.

### Callback Overload

For ValueTask methods where you need dynamic logic, construct the value task in the callback:

<!-- snippet: async-valuetask -->
```cs
[Fact]
public async Task ValueTaskResult_ReturnedDirectly()
{
    var stub = new AsyncUserSvcStub();

    // FULL CALLBACK: Create ValueTask directly when you need async operations
    stub.GetCachedUserAsync.OnCall((id) =>
        new ValueTask<User?>(new User { Id = id, Name = "Cached" })).Verifiable();

    IAsyncUserSvc service = stub;
    var user = await service.GetCachedUserAsync(42);

    Assert.NotNull(user);
    Assert.Equal("Cached", user.Name);
    stub.Verify();
}
```
<!-- endSnippet -->

---

## Simulating Delays

Use async lambdas in `OnCall` to simulate asynchronous delays:

<!-- snippet: async-delay -->
```cs
[Fact]
public async Task AsyncDelay_SimulatesLatency()
{
    var stub = new AsyncUserSvcStub();

    // Use async lambda to simulate delay
    var tracking = stub.GetUserAsync.OnCall(async (id) =>
    {
        await Task.Delay(50); // Simulate network latency
        return new User { Id = id, Name = "Delayed" };
    });

    IAsyncUserSvc service = stub;

    var sw = System.Diagnostics.Stopwatch.StartNew();
    var user = await service.GetUserAsync(1);
    sw.Stop();

    Assert.NotNull(user);
    Assert.True(sw.ElapsedMilliseconds >= 40); // Account for timing variance
}
```
<!-- endSnippet -->

---

## Simulating Failures

### Using Task.FromException

Return a faulted task using `Task.FromException<T>`:

<!-- snippet: async-exception -->
```cs
[Fact]
public async Task TaskFromException_ReturnsFaultedTask()
{
    var stub = new AsyncUserSvcStub();

    // Return a faulted task using Task.FromException
    var tracking = stub.GetUserAsync.OnCall((id) =>
        Task.FromException<User?>(new NotFoundException($"User {id} not found")));

    IAsyncUserSvc service = stub;

    // Awaiting throws the configured exception
    await Assert.ThrowsAsync<NotFoundException>(() =>
        service.GetUserAsync(999));
}
```
<!-- endSnippet -->

### Throwing Directly

Alternatively, throw exceptions directly in the `OnCall` callback. Both approaches produce the same result - the exception is thrown when the method is awaited:

<!-- snippet: async-throw -->
```cs
[Fact]
public async Task ThrowDirectly_InOnCallCallback()
{
    var stub = new AsyncUserSvcStub();

    // Throw exception directly in the callback
    // Note: When only throwing (no return value), use explicit delegate type to disambiguate overloads
    stub.GetUserAsync.OnCall((AsyncUserSvcStub.GetUserAsyncInterceptor.GetUserAsyncDelegate)(id =>
        throw new NotFoundException($"User {id} not found")));

    IAsyncUserSvc service = stub;

    // Exception is thrown when the method is called
    await Assert.ThrowsAsync<NotFoundException>(() =>
        service.GetUserAsync(999));
}
```
<!-- endSnippet -->

---

## Complete Example

This example demonstrates stubbing an async repository for testing a service layer. It combines multiple async method stubs with verification:

<!-- snippet: async-complete-example -->
```cs
[Fact]
public async Task AsyncService_SuccessScenario()
{
    var stub = new AsyncRepoStub();

    // Configure success case with Verifiable markers
    stub.FindAsync.OnCall((id) =>
        Task.FromResult<User?>(new User { Id = id, Name = "Original" })).Verifiable();

    stub.SaveAsync.OnCall((user) => Task.CompletedTask).Verifiable();

    var manager = new UserManager(stub);

    // Act
    var result = await manager.UpdateUserNameAsync(42, "Updated");

    // Assert
    Assert.True(result);
    stub.Verify(); // Verifies both FindAsync and SaveAsync were called
}
```
<!-- endSnippet -->

---

## Choosing Your Approach

**For simple, static values:**
- Use the value overload: `stub.GetUserAsync.OnCall(someUser)`
- KnockOff auto-wraps in `Task.FromResult` (Task) or `new ValueTask<T>(value)` (ValueTask)

**For dynamic logic or parameter-based values:**
- Task: Use callback with `Task.FromResult`: `stub.GetUserAsync.OnCall((id) => Task.FromResult(...))`
- ValueTask: Use callback with `new ValueTask<T>(value)`: `stub.GetCachedAsync.OnCall((id) => new ValueTask<T>(...))`

**For simulating async behavior:**
- Use async lambda: `stub.GetUserAsync.OnCall(async (id) => { await Task.Delay(...); return ...; })`

**For simulating failures:**
- Use `Task.FromException<T>(exception)` or throw directly in callback

---

## Key Takeaways

- **Value overload (recommended)**: Use `OnCall(value)` for simple, static async values
  - Task<T> automatically wraps in `Task.FromResult(value)`
  - ValueTask<T> automatically wraps in `new ValueTask<T>(value)`
- **Callback overload**: Use when you need dynamic logic based on parameters
  - Task<T>: Return `Task.FromResult(value)`
  - ValueTask<T>: Return `new ValueTask<T>(value)`
  - Task (void): Return `Task.CompletedTask`
- **Async lambdas**: Use to simulate delays or complex async behavior
- **Simulating failures**: Use `Task.FromException<T>` or throw directly in callback
- **All interceptor features** (call counts, argument tracking, verification) work with async methods

---

**UPDATED:** 2026-01-25
