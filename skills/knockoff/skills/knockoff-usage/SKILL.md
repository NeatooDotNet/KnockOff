---
name: KnockOff Usage
description: This skill should be used when the user asks about "KnockOff stubs", "create a stub", "mock with KnockOff", "[KnockOff] attribute", "[KnockOff<T>] attribute", "OnCall", "OnGet", "OnSet", "setup stub behavior", "Verify calls", "Verifiable", "VerifyAll", "track method calls", "stub patterns", "Stand-Alone pattern", "Inline Interface", "Inline Class", "Inline Delegate", "stub a delegate", "migrate from Moq", "KnockOff async", "interceptor API", "Strict mode", "Strict()", "assembly-wide strict", "[assembly: KnockOffStrict]", "ThenCall", "ThenGet", "ThenSet", ".Of<T>()", "generic method interceptor", "Source() delegation", or needs guidance on creating, configuring, or verifying KnockOff test stubs.
version: 1.0.0
---

[← Back to Skills](../) | [Commands](../../commands/) | [References](references/)

# KnockOff Usage Guide

KnockOff is a Roslyn Source Generator that creates reusable test stubs at compile time. The key benefit: define a stub class once and share it across your entire project, with each test configuring the same stub instance differently. Unlike runtime mocking frameworks that require per-test setup, KnockOff generates explicit implementations using partial classes—enabling stub reusability while providing compile-time safety and zero reflection overhead.

## Core Concepts

**Source-generated stubs:** Mark a class with `[KnockOff]` or `[KnockOff<T>]` and the generator creates:
- Explicit interface implementations for all members
- Interceptor classes for tracking calls and configuring behavior
- Public interceptor properties for each interface member (named after the member: `GetById`, `SaveUser`, etc.)

**Four patterns:** KnockOff supports four stub creation patterns. Choose based on reusability needs and target type.

**Shared stubs:** The stand-alone pattern enables defining a stub class once and using it across multiple test files. Each test creates its own instance and configures it differently—no duplicate setup code. Change default behavior in the stub class to affect all tests, or override per-test.

## The Four Patterns

### Stand-Alone Pattern

Create a dedicated stub class implementing an interface. Best for reusable stubs shared across test files.

<!-- snippet: skill-standalone-pattern-define -->
```cs
public interface ISkillUserRepo
{
    User? GetById(int id);
    void Save(User user);
}

[KnockOff]
public partial class SkillUserRepoStub : ISkillUserRepo { }
```
<!-- endSnippet -->

**Usage:**
<!-- snippet: skill-standalone-pattern-usage -->
```cs
[Fact]
public void StandaloneStub_ConfigureAndVerify()
{
    // Create the stub
    var stub = new SkillUserRepoStub();

    // Configure behavior
    stub.GetById.OnCall((id) => new User { Id = id, Name = "Alice" }).Verifiable();
    stub.Save.OnCall((user) => { }).Verifiable();

    // Use as interface
    ISkillUserRepo repo = stub;
    var user = repo.GetById(42);
    repo.Save(user!);

    // Verify calls
    stub.Verify();
}
```
<!-- endSnippet -->

### Inline Interface Pattern

Generate a stub scoped to the test class. Best for test-local stubs with no extra files.

<!-- snippet: skill-inline-interface-pattern-define -->
```cs
[KnockOff<ISkillEmailSvc>]
public partial class SkillEmailSvcTests
{
    // Generator creates Stubs.ISkillEmailSvc inside this class
}
```
<!-- endSnippet -->

**Usage:**
<!-- snippet: skill-inline-interface-pattern-usage -->
```cs
[Fact]
public void InlineInterfaceStub_ConfigureAndVerify()
{
    // Create stub from nested Stubs class
    var stub = new Stubs.ISkillEmailSvc();

    // Configure behavior
    stub.Send.OnCall((to, subject) => true).Verifiable();

    // Use as interface
    ISkillEmailSvc email = stub;
    var result = email.Send("test@example.com", "Hello");

    // Verify
    Assert.True(result);
    stub.Verify();
}
```
<!-- endSnippet -->

### Inline Class Pattern

Generate a stub for classes with virtual/abstract members. Best when stubbing classes without extracting interfaces.

<!-- snippet: skill-inline-class-pattern-define -->
```cs
public class SkillDataSvc
{
    public virtual string? GetData(int id) => null;
    public virtual bool IsConnected { get; set; }
}

[KnockOff<SkillDataSvc>]
public partial class SkillDataSvcTests
{
    // Generator creates Stubs.SkillDataSvc inside this class
}
```
<!-- endSnippet -->

**Usage:**
<!-- snippet: skill-inline-class-pattern-usage -->
```cs
[Fact]
public void InlineClassStub_UseObjectProperty()
{
    // Create stub from nested Stubs class
    var stub = new Stubs.SkillDataSvc();

    // Configure behavior
    stub.GetData.OnCall((id) => $"Data-{id}").Verifiable();

    // Use .Object to get the actual class instance
    SkillDataSvc service = stub.Object;
    var data = service.GetData(42);

    // Verify
    Assert.Equal("Data-42", data);
    stub.Verify();
}
```
<!-- endSnippet -->

## Pattern Selection Guide

| Need | Pattern |
|------|---------|
| Reusable stub across files | Stand-Alone |
| Custom methods on stub | Stand-Alone |
| Quick test-local stub | Inline Interface |
| Stub a class (not interface) | Inline Class |
| Stub a delegate type | Inline Delegate |

## Strict Mode

Enable strict mode to catch unexpected method calls. Unconfigured methods throw `StubException` instead of returning default values.

```cs
// Per-stub via attribute
[KnockOff(Strict = true)]
public partial class StrictUserRepoStub : IUserRepo { }

// Per-stub via fluent API
var stub = new UserRepoStub().Strict();

// Assembly-wide default
[assembly: KnockOffStrict]
```

See **`references/strict-mode.md`** for precedence rules, opt-out mechanisms, and detailed examples.

## Configuring Behavior

### Methods with OnCall

Configure method return values and behavior using value or callback overloads:

<!-- snippet: skill-method-oncall-examples -->
```cs
// VALUE syntax - for fixed return values
stub.GetValue.Returns("default-value");

// CALLBACK syntax - for dynamic values based on arguments
stub.GetValue.OnCall((key) => key == "debug" ? "true" : "false");

// Void methods use Action callback
stub.SetValue.OnCall((key, value) => { /* track or validate */ });
```
<!-- endSnippet -->

### Properties with OnGet and OnSet

<!-- snippet: skill-property-configuration-examples -->
```cs
// OnGet with value - simplest syntax
stub.Timeout.OnGet(30);

// OnGet with callback - for computed values
stub.ApiKey.OnGet(() => Environment.GetEnvironmentVariable("API_KEY") ?? "test-key");

// OnSet - intercept property writes
stub.ApiKey.OnSet((value) => { /* validate or track */ });
```
<!-- endSnippet -->

## Verification

### Using Verifiable() and Verify()

Mark members for batch verification:

<!-- snippet: skill-verifiable-batch -->
```cs
// Mark methods as verifiable
stub.Log.OnCall((msg) => { }).Verifiable();
stub.LogError.OnCall((msg) => { }).Verifiable();

ISkillLogger logger = stub;
logger.Log("Starting");
logger.LogError("Oops");

// Single Verify() checks all marked members
stub.Verify();
```
<!-- endSnippet -->

### Verify() vs VerifyAll()

- **`Verify()`** - Checks only members marked with `.Verifiable()`
- **`VerifyAll()`** - Checks ALL configured members (any member with `OnCall`, `OnGet`, etc.)

Use `Verify()` when you want explicit control over which members to verify. Use `VerifyAll()` when you want to ensure every configured member was actually called.

### Using Times Constraints

<!-- snippet: skill-times-constraints -->
```cs
// Verify specific call counts
var tracking = stub.Log.OnCall((msg) => { });

ISkillLogger logger = stub;
logger.Log("First");
logger.Log("Second");

tracking.Verify(Times.Exactly(2));  // Exactly 2 calls
tracking.Verify(Times.AtLeast(1));  // At least 1 call
// Times.Once, Times.Never, Times.AtMost(n) also available
```
<!-- endSnippet -->

### Accessing Call Arguments

<!-- snippet: skill-accessing-arguments -->
```cs
var tracking = stub.Notify.OnCall((userId, message) => { });

ISkillNotifier notifier = stub;
notifier.Notify(42, "Hello");

// Access arguments from tracking object
var (userId, message) = tracking.LastArgs;
Assert.Equal(42, userId);
Assert.Equal("Hello", message);
```
<!-- endSnippet -->

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

### Source Delegation

Use `Source()` to delegate unconfigured calls to a real implementation. Configured members (via `OnCall`) take priority over source delegation.

```cs
var stub = new DataStoreStub();
var realStore = new InMemoryDataStore();

// Delegate unconfigured members to real implementation
stub.Source(realStore);

// Override specific members while delegating the rest
stub.Get.OnCall((id) => "test value");

IDataStore store = stub;
store.Add("item");     // Delegates to realStore
store.Get(0);          // Returns "test value" (OnCall configured)
```

## Common Gotchas

### Missing `partial` Keyword

**Problem:** Stub class not marked `partial` causes duplicate member errors.

<!-- snippet: skill-gotcha-missing-partial -->
```cs
// CORRECT: Include 'partial' keyword
[KnockOff]
public partial class SkillFooStub : ISkillFoo { }
```
<!-- endSnippet -->

### Wrong OnCall Signature

**Problem:** Callback signature doesn't match method parameters.

<!-- snippet: skill-gotcha-wrong-signature -->
```cs
// CORRECT: Callback signature matches method parameters exactly
stub.Process.OnCall((int id, string name) => { /* ... */ });
```
<!-- endSnippet -->

### Forgetting .Object for Class Stubs

**Problem:** Using inline class stub directly instead of `.Object`.

<!-- snippet: skill-gotcha-missing-object -->
```cs
// CORRECT: Use .Object for inline class stubs
SkillAbstractBase service = stub.Object;
_ = service.GetName();
```
<!-- endSnippet -->

### Simplified Async Callbacks

**KnockOff auto-wraps async callbacks.** No need for `Task.FromResult()` or `Task.CompletedTask`:

```cs
// Value overload - auto-wrapped
stub.GetUserAsync.OnCall(new User { Id = 1, Name = "Alice" });

// Simplified callback - return inner type, auto-wrapped in Task.FromResult
stub.GetUserAsync.OnCall((id) => new User { Id = id, Name = "Alice" });

// Void async - use Action, Task.CompletedTask auto-returned
stub.SaveUserAsync.OnCall((user) => ValidateUser(user));
```

See **`references/methods.md`** for complete async method documentation.

## Moq Migration Quick Reference

| Moq | KnockOff |
|-----|----------|
| `new Mock<IFoo>()` | `new FooStub()` or `new Stubs.IFoo()` |
| `mock.Object` | `stub` (direct) or `stub.Object` (class stubs) |
| `.Setup(x => x.Method()).Returns(val)` | `stub.Method.OnCall(val)` or `stub.Method.OnCall(() => val)` |
| `.Setup(x => x.Prop).Returns(val)` | `stub.Prop.OnGet(val)` |
| `.ReturnsAsync(val)` | `stub.Method.OnCall(val)` (auto-wraps) |
| `.Callback(x => ...)` | Logic inside OnCall callback |
| `.Verify(x => x.Method(), Times.Once)` | `tracking.Verify(Times.Once)` |
| `.Verifiable()` + `mock.Verify()` | `.Verifiable()` + `stub.Verify()` |
| `It.IsAny<T>()` | Callback receives all args (always) |

## Reference Documentation

For detailed documentation, consult the reference files in `references/`:

- **`references/patterns.md`** - Complete guide to all four stub patterns with examples
- **`references/methods.md`** - Method interceptor configuration, verification, and argument capture
- **`references/properties.md`** - Property interceptors with OnGet, OnSet, and sequences
- **`references/api-reference.md`** - Complete interceptor API (methods, properties, indexers, events, generics)
- **`references/strict-mode.md`** - Strict mode configuration, assembly-wide defaults, and precedence
- **`references/moq-migration.md`** - Step-by-step Moq to KnockOff migration guide

## Troubleshooting

**Generator not running:**
- Ensure `[KnockOff]` or `[KnockOff<T>]` attribute is present
- Check class is marked `partial`
- Rebuild the project
- Check for analyzer errors in build output

**Interceptor property not found:**
- Generated properties are named after each interface member (e.g., `GetById`, `SaveUser`)
- For stand-alone stubs, interceptor properties are directly on the stub class
- For inline stubs, access through the generated `Stubs` nested class
- Check Generated/ folder for actual generated code

**Type mismatch in OnCall:**
- Ensure callback parameters match interface method signature exactly
- For generic methods, specify type arguments explicitly

---

**UPDATED:** 2026-01-27
