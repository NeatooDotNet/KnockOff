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

**How it works**: When you set a source, KnockOff's interceptors check for configuration first. If no `OnCall` callback or `OnCall` value is configured for a member, the interceptor forwards the call to the source implementation.

---

## Partial Delegation

You can override specific methods while delegating the rest to the source. Set the source for baseline behavior, then use `OnCall` (callback or value) to customize specific members:

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
- You need full control over all stub behavior (use pure stubbing with `OnCall`)
- Testing in complete isolation with no real dependencies
- The source implementation has side effects you want to avoid (database calls, external APIs, etc.)

---

## Priority Order

KnockOff's interceptors evaluate member calls in this priority order:

1. **Sequence (OnCallSequence)** - Active sequence steps run first
2. **OnCall value** - Direct return value, set via `stub.Method.OnCall(value)`
3. **OnCall callback** - Callback, set via `stub.Method.OnCall((args) => result)`
4. **Source delegation** - Set via `stub.Source(realImplementation)`
5. **Smart default** - Lowest priority, KnockOff's built-in return value generation

The first match wins. This means you can set a source for baseline behavior and selectively override specific members with `OnCall` when needed.

**Important**: `OnCall` (both callback and value overloads) take complete control once configured. If `OnCall` is set, the source is never consulted for that member, even if the callback returns `null` or a default value.

**Note**: The Flat/Stand-Alone pattern (`[KnockOff] partial class Stub : IInterface`) also supports user-defined methods, which execute between OnCall and Source in the priority chain. See the [User Methods Guide](user-methods.md) for details on this pattern-specific feature.

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

You can use either the callback overload (`OnCall(callback)`) or the value overload (`OnCall(value)`) to override the source. The value overload is simpler when you need a fixed return value:

```csharp
// Value overload - simpler for fixed values
stub.GetPriority.OnCall(99);

// Callback overload - use when you need logic or side effects
stub.GetPriority.OnCall((user) => user.IsActive ? 1 : 0);
```

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

## OnCall API Reference

The `OnCall` method has two overloads for configuring method behavior:

### OnCall(callback)
Pass a delegate that matches the method signature. Use when you need:
- Dynamic values based on arguments
- Conditional logic
- Side effects

```csharp
stub.GetById.OnCall((id) => new User { Id = id, Name = $"User{id}" });
```

### OnCall(value)
Pass a direct return value. Use when you need:
- Fixed return values
- Simpler syntax without callback boilerplate

```csharp
stub.GetById.OnCall(new User { Id = 1, Name = "Fixed User" });
```

Both overloads return `IMethodTracking<T>` for verification and tracking.

---

**Next Steps:**
- [Method Interceptors Guide](methods.md) - Complete guide to `OnCall` callback and value overloads
- [User Methods Guide](user-methods.md) - Defining methods in your stub class for custom behavior
- [Verification Patterns](verification.md) - Assert on stub interactions and call tracking

---

**UPDATED:** 2026-01-25
