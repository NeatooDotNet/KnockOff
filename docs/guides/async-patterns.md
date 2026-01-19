# Async Patterns

KnockOff fully supports async methods, allowing you to configure `Task<T>` and `ValueTask<T>` return values directly from the `OnCall` callback. This guide shows common async patterns for unit testing.

---

## Task<T> Methods

For async methods returning `Task<T>`, use `Task.FromResult` to return immediate values:

<!-- snippet: async-task-result -->
```cs
[Fact]
public async Task TaskResult_ReturnedWithFromResult()
{
    var stub = new AsyncUserSvcStub();

    // Use Task.FromResult to return a value synchronously
    stub.GetUserAsync.OnCall((ko, id) =>
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
    stub.UpdateUserAsync.OnCall((ko, user) =>
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

For methods returning `ValueTask<T>`, construct the value task directly:

<!-- snippet: async-valuetask -->
```cs
[Fact]
public async Task ValueTaskResult_ReturnedDirectly()
{
    var stub = new AsyncUserSvcStub();

    // Create ValueTask directly with the value
    stub.GetCachedUserAsync.OnCall((ko, id) =>
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
    var tracking = stub.GetUserAsync.OnCall(async (ko, id) =>
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
    var tracking = stub.GetUserAsync.OnCall((ko, id) =>
        Task.FromException<User?>(new NotFoundException($"User {id} not found")));

    IAsyncUserSvc service = stub;

    // Awaiting throws the configured exception
    await Assert.ThrowsAsync<NotFoundException>(() =>
        service.GetUserAsync(999));
}
```
<!-- endSnippet -->

### Throwing Directly

Alternatively, throw exceptions directly in the `OnCall` callback:

<!-- snippet: async-throw -->
```cs
[Fact]
public async Task ThrowDirectly_InOnCallCallback()
{
    var stub = new AsyncUserSvcStub();

    // Throw exception directly in the callback
    var tracking = stub.GetUserAsync.OnCall((ko, id) =>
    {
        throw new NotFoundException($"User {id} not found");
    });

    IAsyncUserSvc service = stub;

    // Exception is thrown when the method is called
    await Assert.ThrowsAsync<NotFoundException>(() =>
        service.GetUserAsync(999));
}
```
<!-- endSnippet -->

---

## Complete Example

Here's a full test demonstrating async success and failure scenarios:

<!-- snippet: async-complete-example -->
```cs
[Fact]
public async Task AsyncService_SuccessScenario()
{
    var stub = new AsyncRepoStub();

    // Configure success case with Verifiable markers
    stub.FindAsync.OnCall((ko, id) =>
        Task.FromResult<User?>(new User { Id = id, Name = "Original" })).Verifiable();

    stub.SaveAsync.OnCall((ko, user) => Task.CompletedTask).Verifiable();

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

## Key Takeaways

- `OnCall` returns `Task<T>` or `ValueTask<T>` directly for async methods
- Use `Task.FromResult(value)` for immediate async values
- Use `Task.CompletedTask` for async void methods
- Use async lambdas to simulate delays or complex async behavior
- Use `Task.FromException<T>` or throw directly to simulate failures
- All interceptor features (call counts, argument tracking) work with async methods
