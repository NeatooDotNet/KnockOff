---
name: KnockOff Usage
description: This skill should be used when the user asks about "KnockOff stubs", "create a stub", "mock with KnockOff", "[KnockOff] attribute", "[KnockOff<T>] attribute", "OnCall", "OnGet", "OnSet", "setup stub behavior", "Verify calls", "Verifiable", "track method calls", "stub patterns", "Stand-Alone pattern", "Inline Interface", "Inline Class", "migrate from Moq", "KnockOff async", "interceptor API", or needs guidance on creating, configuring, or verifying KnockOff test stubs.
version: 1.0.0
---

# KnockOff Usage Guide

KnockOff is a Roslyn Source Generator that creates reusable test stubs at compile time. The key benefit: define a stub class once and share it across your entire project, with each test configuring the same stub instance differently. Unlike runtime mocking frameworks that require per-test setup, KnockOff generates explicit implementations using partial classes—enabling stub reusability while providing compile-time safety and zero reflection overhead.

## Core Concepts

**Source-generated stubs:** Mark a class with `[KnockOff]` or `[KnockOff<T>]` and the generator creates:
- Explicit interface implementations for all members
- Interceptor objects for tracking calls and configuring behavior
- Properties named after the interface for accessing interceptors

**Three patterns:** KnockOff supports three stub creation patterns. Choose based on reusability needs and target type.

**Shared stubs:** The stand-alone pattern enables defining a stub class once and using it across multiple test files. Each test creates its own instance and configures it differently—no duplicate setup code. Change default behavior in the stub class to affect all tests, or override per-test.

## The Three Patterns

### Stand-Alone Pattern

Create a dedicated stub class implementing an interface. Best for reusable stubs shared across test files.

```csharp
[KnockOff]
public partial class UserRepoStub : IUserRepo
{
    // Generator fills in implementations
    // Optionally add user methods for default behavior
}
```

**Usage:**
```csharp
var stub = new UserRepoStub();
stub.GetUser.OnCall((id) => new User { Id = id });
IUserRepo repo = stub;
```

### Inline Interface Pattern

Generate a stub scoped to the test class. Best for test-local stubs with no extra files.

```csharp
[KnockOff<IUserRepo>]
public partial class MyTests
{
    // Generator creates Stubs.IUserRepo
}
```

**Usage:**
```csharp
var stub = new Stubs.IUserRepo();
stub.GetUser.OnCall((id) => new User { Id = id });
IUserRepo repo = stub;
```

### Inline Class Pattern

Generate a stub for classes with virtual/abstract members. Best when stubbing classes without extracting interfaces.

```csharp
[KnockOff<UserServiceBase>]
public partial class MyTests
{
    // Generator creates Stubs.UserServiceBase
}
```

**Usage:**
```csharp
var stub = new Stubs.UserServiceBase();
stub.GetUser.OnCall((id) => new User { Id = id });
UserServiceBase service = stub.Object;  // Note: use .Object for class stubs
```

## Pattern Selection Guide

| Need | Pattern |
|------|---------|
| Reusable stub across files | Stand-Alone |
| Custom methods on stub | Stand-Alone |
| Quick test-local stub | Inline Interface |
| Stub a class (not interface) | Inline Class |

## Configuring Behavior

### Methods with OnCall

Configure method return values and behavior using value or callback overloads:

```csharp
// Value overload - fixed return value
stub.GetUser.OnCall(new User { Id = 42, Name = "Test" });

// Callback overload - compute based on arguments
stub.GetUser.OnCall((id) => new User { Id = id, Name = "Test" });

// Void methods - callbacks for side effects
stub.SaveUser.OnCall((user) => { /* side effects */ });

// Async methods - value auto-wraps in Task
stub.GetUserAsync.OnCall(new User { Id = 42, Name = "Alice" });

// Async with callback - return unwrapped value
stub.GetUserAsync.OnCall((id) => new User { Id = id, Name = "Dynamic" });

// Conditional logic in callbacks
stub.GetUser.OnCall((id) => id > 0 ? new User { Id = id } : null);
```

### Properties with Value, OnGet, and OnSet

```csharp
// Value property - simple backing value (most common)
stub.ConnectionString.Value = "server=localhost";

// OnGet with value - fixed return value
stub.IsConnected.OnGet(true);

// OnGet with callback - dynamic getter
stub.IsConnected.OnGet(() => DateTime.Now.Hour < 18);

// OnSet - track or validate setter calls
stub.CurrentUser.OnSet((value) => { /* handle set */ });

// Combined - both getter and setter
stub.MaxRetries.OnGet(3);
stub.MaxRetries.OnSet((value) => { /* validate */ });
```

## Verification

### Using Verifiable() and Verify()

Mark members for batch verification:

```csharp
// Setup with Verifiable()
stub.GetUser.OnCall((id) => user).Verifiable();
stub.SaveUser.OnCall((u) => { }).Verifiable();

// Act
repo.GetUser(1);
repo.SaveUser(user);

// Verify all marked members were called
stub.Verify();
```

### Using Times Constraints

```csharp
var tracking = stub.GetUser.OnCall((id) => user).Verifiable();

// After acting...
tracking.Verify(Times.Once);           // Called exactly once
tracking.Verify(Times.Twice);          // Called exactly twice
tracking.Verify(Times.Exactly(3));     // Called exactly N times
tracking.Verify(Times.AtLeastOnce);    // Called one or more times
tracking.Verify(Times.AtLeast(3));     // Called N or more times
tracking.Verify(Times.AtMost(5));      // Called N or fewer times
tracking.Verify(Times.Never);          // Never called
```

### Accessing Call Arguments

```csharp
var tracking = stub.SaveUser.OnCall((user) => { }).Verifiable();

// After acting...
var lastArgs = tracking.LastArgs;
Assert.Equal("Alice", lastArgs.user.Name);
```

## Best Practices

### Choose Value vs Callback Syntax

**Use value overloads when:**
- Returning a fixed value that never changes
- No logic needed based on arguments
- Keeping test setup concise

**Use callback overloads when:**
- Computing values based on method arguments
- Implementing conditional logic
- Tracking calls or performing side effects
- Need access to all method parameters

### Stub Reusability Strategy

**Stand-alone pattern for:**
- Stubs used across multiple test classes
- Default behavior shared by many tests
- Custom helper methods on the stub (e.g., `ConfigureForHappyPath()`)

**Inline pattern for:**
- Test-local stubs used only in one test class
- Quick prototyping or exploratory testing
- No need for cross-file sharing

### Verification Patterns

**Batch verification with `Verify()`:**
- Mark multiple members with `.Verifiable()`
- Call `stub.Verify()` once at the end
- Best when verifying many calls together

**Individual verification with `Times`:**
- Store tracking object from `OnCall().Verifiable()`
- Call `tracking.Verify(Times.X)` for specific constraints
- Best when different members have different expectations

## Common Gotchas

### Missing `partial` Keyword

**Problem:** Stub class not marked `partial` causes duplicate member errors.

```csharp
// Wrong
[KnockOff]
class UserRepoStub : IUserRepo { }

// Correct
[KnockOff]
partial class UserRepoStub : IUserRepo { }
```

### Wrong OnCall Signature

**Problem:** Callback signature doesn't match method parameters.

```csharp
// Wrong - GetUser(int id) expects (int) callback
stub.GetUser.OnCall(() => user);

// Correct
stub.GetUser.OnCall((id) => user);
```

### Forgetting .Object for Class Stubs

**Problem:** Using inline class stub directly instead of `.Object`.

```csharp
// Wrong - stub is a wrapper
var service = new UserService(stub);

// Correct - use .Object to get actual instance
var service = new UserService(stub.Object);
```

### Using Task.FromResult in Async Callbacks

**No longer required:** KnockOff auto-wraps values for async methods.

```csharp
// Value overload - auto-wrapped (preferred)
stub.GetUserAsync.OnCall(user);

// Callback - auto-wrapped (preferred)
stub.GetUserAsync.OnCall((id) => user);

// Explicit Task.FromResult - still works but unnecessary
stub.GetUserAsync.OnCall((id) => Task.FromResult(user));
```

## Moq Migration Quick Reference

| Moq | KnockOff |
|-----|----------|
| `new Mock<IFoo>()` | `new FooStub()` or `new Stubs.IFoo()` |
| `mock.Object` | `stub` (direct) or `stub.Object` (class stubs) |
| `.Setup(x => x.Method()).Returns(val)` | `stub.Method.OnCall(val)` or `stub.Method.OnCall(() => val)` |
| `.Setup(x => x.Prop).Returns(val)` | `stub.Prop.Value = val` or `stub.Prop.OnGet(val)` |
| `.ReturnsAsync(val)` | `stub.Method.OnCall(val)` (auto-wraps) |
| `.Callback(x => ...)` | Logic inside OnCall callback |
| `.Verify(x => x.Method(), Times.Once)` | `tracking.Verify(Times.Once)` |
| `.Verifiable()` + `mock.Verify()` | `.Verifiable()` + `stub.Verify()` |
| `It.IsAny<T>()` | Callback receives all args (always) |

## Reference Documentation

For detailed documentation, consult the reference files in `references/`:

- **`references/patterns.md`** - Complete guide to all three stub patterns with examples
- **`references/methods.md`** - Method interceptor configuration, verification, and argument capture
- **`references/properties.md`** - Property interceptors with Value, OnGet, OnSet
- **`references/api-reference.md`** - Complete interceptor API (methods, properties, indexers, events, generics)
- **`references/moq-migration.md`** - Step-by-step Moq to KnockOff migration guide

## Troubleshooting

**Generator not running:**
- Ensure `[KnockOff]` or `[KnockOff<T>]` attribute is present
- Check class is marked `partial`
- Rebuild the project
- Check for analyzer errors in build output

**Interceptor property not found:**
- Generated properties are named after the interface (e.g., `IUserRepo`)
- For multiple interfaces, each gets its own property
- Check Generated/ folder for actual generated code

**Type mismatch in OnCall:**
- Ensure callback parameters match interface method signature exactly
- For generic methods, specify type arguments explicitly
