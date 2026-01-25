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
// Override GetById for test data
stub.GetById.OnCall((id) => new User { Id = id, Name = "Test User" });

IRepository repository = stub;

// GetById uses OnCall override
var testUser = repository.GetById(1);
Assert.NotNull(testUser);
Assert.Equal("Test User", testUser.Name);

// Save delegates to source (no OnCall configured)
repository.Save(new User { Id = 2, Name = "New User" });
Assert.NotNull(realRepo.GetById(2));
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

1. **OnCall callback** - Highest priority, set via `stub.Method.OnCall(...)`
2. **User method** - Detected methods you define in the stub class
3. **Source delegation** - Set via `stub.Source(realImplementation)`
4. **Smart default** - Lowest priority, KnockOff's built-in return value generation

The first match wins. This means you can set a source for baseline behavior, override specific members with user methods, and further customize with `OnCall` when needed.

<!-- snippet: source-priority -->
```cs
// Source returns 1 for active user (when no OnCall is set)
var fromSource = repository.GetPriority(new User { Id = 1, IsActive = true });
Assert.Equal(1, fromSource);

// OnCall overrides source
stub.GetPriority.OnCall((user) => 42);
var fromOnCall = repository.GetPriority(new User { Id = 1, IsActive = true });
Assert.Equal(42, fromOnCall);
```
<!-- endSnippet -->

Understanding priority order is crucial for predictable stub behavior when mixing delegation patterns.

---

## Complete Example

Here's a complete scenario demonstrating source delegation for a decorator pattern test:

<!-- snippet: source-complete-example -->
```cs
// Override Read for specific test scenario
stub.Read.OnCall((filename) =>
    filename == "config.txt" ? "Test Config" : null);

IDataSource dataSource = stub;

// OnCall handles config.txt
var config = dataSource.Read("config.txt");
Assert.Equal("Test Config", config);

// OnCall returned null for data.txt, but source is NOT consulted
// once OnCall is configured - it takes full control
var data = dataSource.Read("data.txt");
Assert.Null(data);

// Write delegates entirely to source (no OnCall configured)
dataSource.Write("output.txt", "New Data");
Assert.Equal("New Data", realDataSource.Read("output.txt"));
```
<!-- endSnippet -->

This example demonstrates source delegation's strength: you get real implementation behavior for most operations while overriding specific members needed for test scenarios.

---

**Next Steps:**
- [Advanced Callbacks Guide](advanced-callbacks.md) - Using `OnCall` to configure complex stub behavior
- [User Methods Guide](user-methods.md) - Defining methods in your stub class for custom behavior
- [Verification Patterns](verification.md) - Assert on stub interactions and call tracking
