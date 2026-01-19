# Source Delegation

**Source delegation** allows you to configure a KnockOff stub to forward method calls and property access to a real implementation. This is useful for partial stubbing scenarios where you want default behavior from an actual implementation but need to override specific members for testing.

Use `Source(T)` when you're testing decorators, wrappers, or need integration-style tests with mostly real dependencies but a few test-controlled overrides.

---

## Basic Source Delegation

Configure a stub to delegate to a real implementation by calling `Source(realImplementation)` on the stub:

<!-- snippet: source-basic -->
```cs
[Fact]
public void Source_DelegatesToRealImplementation()
{
    var stub = new DataStoreStub();
    var realStore = new InMemoryDataStore();

    // Configure stub to delegate to real implementation
    stub.Source(realStore);

    IDataStore store = stub;

    // All calls delegate to the real implementation
    store.Add("first");
    store.Add("second");

    Assert.Equal(2, store.Count);
    Assert.Equal("first", store.Get(0));
    Assert.Equal("second", store.Get(1));
}
```
<!-- endSnippet -->

**How it works**: When you set a source, KnockOff's interceptors check for callbacks first. If no `OnCall` or user method is configured for a member, the interceptor forwards the call to the source implementation.

---

## Partial Delegation

You can override specific methods while delegating the rest to the source. Set the source for baseline behavior, then use `OnCall` to customize specific members:

<!-- snippet: source-partial-override -->
```cs
[Fact]
public void Source_PartialOverrideWithOnCall()
{
    var stub = new SourceRepoStub();
    var realRepo = new SimpleRepository();

    // Seed real repo with data
    realRepo.Save(new User { Id = 1, Name = "Real User" });

    // Delegate to real implementation
    stub.Source(realRepo);

    // Override specific method for testing
    stub.GetById.OnCall((ko, id) =>
        id == 999 ? new User { Id = 999, Name = "Test User" } : null);

    IRepository repository = stub;

    // OnCall overrides source for id 999
    var testUser = repository.GetById(999);
    Assert.NotNull(testUser);
    Assert.Equal("Test User", testUser.Name);

    // Source still used when OnCall returns null (fallback)
    // Note: In this case OnCall handles all ids, so source is bypassed
}
```
<!-- endSnippet -->

This pattern is ideal when you want real behavior for most operations but need controlled test data for specific scenarios.

---

## Interface Hierarchies

When delegating to an implementation of a derived interface (like `IList<T>` which implements `IEnumerable<T>`), the source applies to all levels of the hierarchy:

<!-- snippet: source-hierarchy -->
```cs
[Fact]
public void Source_AppliesAcrossInterfaceHierarchy()
{
    var stub = new DataStoreStub();
    var realStore = new InMemoryDataStore();

    // Add items to real store
    realStore.Add("item1");
    realStore.Add("item2");

    // Delegate to real implementation
    stub.Source(realStore);

    IDataStore store = stub;

    // All interface methods delegate to source
    Assert.Equal(2, store.Count);
    Assert.Equal("item1", store.Get(0));
}
```
<!-- endSnippet -->

KnockOff's interceptors respect the inheritance chain—setting a source on a derived interface stub automatically delegates base interface members as well.

---

## Clearing Source

Remove source delegation by setting it to `null`:

<!-- snippet: source-clear -->
```cs
[Fact]
public void Source_CanBeClearedWithNull()
{
    var stub = new DataStoreStub();
    var realStore = new InMemoryDataStore();

    realStore.Add("item");
    stub.Source(realStore);

    IDataStore store = stub;

    // Source is active
    Assert.Equal(1, store.Count);

    // Clear source
    stub.Source(null);

    // Now smart defaults are used (Count returns 0)
    Assert.Equal(0, store.Count);
}
```
<!-- endSnippet -->

This is useful when you need source delegation for test setup but want to verify stub behavior independently later in the test.

---

## When to Use Source

**Use `Source(T)` when:**
- Testing decorator patterns where you wrap a real implementation
- Building integration tests that use mostly real dependencies with a few test overrides
- You need baseline behavior from a real class but want to intercept specific methods
- Partially stubbing complex interfaces where manually configuring every member is impractical

**Do NOT use `Source(T)` when:**
- You need full control over all stub behavior (use pure stubbing with `OnCall` or user methods)
- Testing in complete isolation with no real dependencies
- The source implementation has side effects you want to avoid (database calls, external APIs, etc.)

---

## Priority Order

KnockOff's interceptors evaluate member calls in this priority order:

1. **OnCall callback** - Highest priority, set via `stub.Interceptors.Method.OnCall(...)`
2. **User method** - Detected methods you define in the stub class
3. **Source delegation** - Set via `stub.Source(realImplementation)`
4. **Smart default** - Lowest priority, KnockOff's built-in return value generation

The first match wins. This means you can set a source for baseline behavior, override specific members with user methods, and further customize with `OnCall` when needed.

<!-- snippet: source-priority -->
```cs
[Fact]
public void Priority_OnCallBeatsSourceBeatsSmartDefault()
{
    var stub = new SourceRepoStub();
    var realRepo = new SimpleRepository();

    realRepo.Save(new User { Id = 1, Name = "Source", IsActive = true });

    // Set source (returns priority 1 for active users)
    stub.Source(realRepo);

    IRepository repository = stub;

    // Source returns 1 for active user
    var fromSource = repository.GetPriority(new User { Id = 1, IsActive = true });
    Assert.Equal(1, fromSource);

    // OnCall overrides source
    stub.GetPriority.OnCall((ko, user) => 42);
    var fromOnCall = repository.GetPriority(new User { Id = 1, IsActive = true });
    Assert.Equal(42, fromOnCall);
}
```
<!-- endSnippet -->

Understanding priority order is crucial for predictable stub behavior when mixing delegation patterns.

---

## Complete Example

Here's a complete scenario testing a caching decorator using source delegation:

<!-- snippet: source-complete-example -->
```cs
[Fact]
public void CachingDecorator_UsesSourceForBaseline()
{
    var stub = new CachingSourceRepoStub();
    var realRepo = new RealRepository();

    // Use real repository as baseline
    stub.Source(realRepo);

    // Track calls to verify caching behavior
    var callCount = 0;
    stub.GetUser.OnCall((ko, id) =>
    {
        callCount++;
        // Delegate to source
        return realRepo.GetUser(id);
    });

    ICachingRepository repository = stub;

    // First call
    var user1 = repository.GetUser(1);
    Assert.NotNull(user1);
    Assert.Equal(1, callCount);

    // Second call with same id
    var user2 = repository.GetUser(1);
    Assert.NotNull(user2);
    Assert.Equal(2, callCount); // Not cached - stub doesn't cache

    // Verify real data came through
    Assert.Equal("User1", user1.Name);
    Assert.Equal("User1", user2.Name);
}
```
<!-- endSnippet -->

This example demonstrates source delegation's strength: you get real repository behavior for most operations while controlling specific scenarios (cache miss/hit) needed for decorator testing.

---

## Related Guides

- [Callbacks](callbacks.md) - Using `OnCall` to configure stub behavior
- [User Methods](user-methods.md) - Defining methods in your stub class
- [Smart Defaults](smart-defaults.md) - Understanding KnockOff's default return values

## See Also

- [API Reference: Source(T)](../api/stub-methods.md#source) - Complete API documentation
