# Source Delegation

**Source delegation** allows you to configure a KnockOff stub to forward method calls and property access to a real implementation. This is useful for partial stubbing scenarios where you want default behavior from an actual implementation but need to override specific members for testing.

Use `Source(T)` when you're testing decorators, wrappers, or need integration-style tests with mostly real dependencies but a few test-controlled overrides.

**Availability**: Source delegation is available for **interface stubs** only (Standalone and Inline patterns). Class stubs (`[KnockOffBase<T>]`) delegate to the base class directly and do not support `Source()`.

---

## Basic Source Delegation

Configure a stub to delegate to a real implementation by calling `Source(realImplementation)` on the stub:

<!-- snippet: source-basic -->
```cs
// Configure stub to delegate to real implementation
stub.Source(realStore);
```
<!-- endSnippet -->

**How it works**: When you set a source, KnockOff's interceptors check for configuration first. If no `OnCall` callback or `Returns` value is configured for a member, the interceptor forwards the call to the source implementation.

---

## Partial Delegation

You can override specific methods while delegating the rest to the source. Set the source for baseline behavior, then use `OnCall` (callback or value) to customize specific members:

<!-- snippet: source-partial-override -->
```cs
// Override specific member while source handles the rest
stub.GetById.OnCall((id) => new User { Id = id, Name = "Test User" });
```
<!-- endSnippet -->

This pattern is ideal when you want real behavior for most operations but need controlled test data for specific scenarios.

---

## Interface Hierarchies

When delegating to an implementation of a derived interface (like `IList<T>` which implements `IEnumerable<T>`), the source applies to all levels of the hierarchy:

<!-- snippet: source-hierarchy -->
```cs
// Source applies to all interface hierarchy levels
stub.Source(realStore);
```
<!-- endSnippet -->

KnockOff's interceptors respect the inheritance chain--setting a source on a derived interface stub automatically delegates base interface members as well.

---

## Clearing Source

Remove source delegation by setting it to `null`:

<!-- snippet: source-clear -->
```cs
// Clear source to revert to smart defaults
stub.Source(null);
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

1. **When chains** - Conditional matches run first (configured via `.When(...).Returns(...)`)
2. **Sequence callbacks** - Active sequence steps (configured via `OnCall().ThenCall()`)
3. **OnCall/Returns** - Callback or value, set via `stub.Method.OnCall(...)` or `stub.Method.Returns(...)`
4. **User methods** - Protected override methods with `_` suffix (Standalone patterns only)
5. **Source delegation** - Set via `stub.Source(realImplementation)`
6. **Smart default** - Lowest priority, KnockOff's built-in return value generation

The first match wins. This means you can set a source for baseline behavior and selectively override specific members with `OnCall` when needed.

**Important**: `OnCall` (both callback and value overloads) take complete control once configured. If `OnCall` is set, the source is never consulted for that member, even if the callback returns `null` or a default value.

**Note**: The Stand-Alone pattern (`[KnockOff] partial class Stub : IInterface`) also supports user methods--`protected override` methods with an underscore suffix (e.g., `protected override User GetById_(int id)`)--that execute between OnCall and Source in the priority chain. See the [User Methods Guide](user-methods.md) for details on this pattern-specific feature.

<!-- snippet: source-priority -->
```cs
// OnCall takes precedence over source
stub.GetPriority.OnCall((user) => 42);
```
<!-- endSnippet -->

You can use either the callback overload (`OnCall(callback)`) or the value overload (`Returns(value)`) to override the source:

<!-- snippet: source-oncall-value-vs-callback -->
```cs
// Value overload - simpler for fixed values
stub.GetPriority.Returns(99);

// Callback overload - use when you need logic or side effects
stub.GetPriority.OnCall((user) => user.IsActive ? 1 : 0);
```
<!-- endSnippet -->

Understanding priority order is crucial for predictable stub behavior when mixing delegation patterns.

---

## Complete Example

Here's a complete scenario demonstrating source delegation for a decorator pattern test:

<!-- snippet: source-complete-example -->
```cs
// OnCall takes full control - source not consulted even for non-matches
stub.Read.OnCall((filename) =>
    filename == "config.txt" ? "Test Config" : null);
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

<!-- snippet: source-oncall-api-callback -->
```cs
stub.GetById.OnCall((id) => new User { Id = id, Name = $"User{id}" });
```
<!-- endSnippet -->

### Returns(value)
Pass a direct return value. Use when you need:
- Fixed return values
- Simpler syntax without callback boilerplate

<!-- snippet: source-oncall-api-value -->
```cs
stub.GetById.Returns(new User { Id = 1, Name = "Fixed User" });
```
<!-- endSnippet -->

Both overloads return `IMethodTracking<T>` for verification and tracking.

---

**Next Steps:**
- [Method Interceptors Guide](methods.md) - Complete guide to `OnCall` callback and value overloads
- [User Methods Guide](user-methods.md) - Defining methods in your stub class for custom behavior
- [Verification Patterns](verification.md) - Assert on stub interactions and call tracking

---

**UPDATED:** 2026-02-06
