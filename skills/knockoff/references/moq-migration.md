[knockoff-usage](../SKILL.md) > [references](README.md) > moq-migration

# Migrating from Moq to KnockOff

Switching from Moq to KnockOff means moving from per-test mock setup to reusable stub classes. You gain the ability to share stubs across tests while still customizing behavior per-test—while trading Moq's runtime flexibility for source-generated, explicit stub implementations.

This guide walks you through the migration step-by-step, with side-by-side comparisons and a complete before/after example.

---

## What Changes

**Moq's approach:**
- Runtime reflection with fluent `.Setup()` API
- `Mock<T>` wrapper objects
- `.Object` property to access the instance
- `.Verify()` methods for call assertions

**KnockOff's approach:**
- Compile-time source generation with partial classes
- Direct stub classes with `[KnockOff<T>]` attribute
- Interceptor properties for configuration and verification
- Standard assertions on call tracking properties

**What stays the same:**
- You still create test doubles for interfaces and classes
- You still configure behavior and verify calls
- Your test goals and patterns remain unchanged

---

## Quick Reference

| Moq Pattern | KnockOff Equivalent |
|-------------|---------------------|
| `new Mock<IFoo>()` | `new FooStub()` with `[KnockOff<IFoo>] partial class FooStub` |
| `mock.Object` | `stub` (direct) or `stub.Object` (class stubs only) |
| `.Setup(x => x.Method()).Returns(value)` | `stub.Method.Returns(value)` |
| `.Setup(x => x.Method(arg)).Returns(val)` | `stub.Method.When(arg).Returns(val)` |
| `.Setup(x => x.Property).Returns(value)` | `stub.Property.Get(value)` |
| `.ReturnsAsync(value)` | `stub.Method.Returns(value)` (auto-wraps) |
| `.Callback(x => ...)` | Logic in `Returns`/`Execute` callback |
| `.Verify(x => x.Method(), Times.Once)` | `tracking.Verify(Times.Once)` |
| `.Verifiable()` | `.Verifiable()` then `stub.Verify()` |
| `mock.Verify()` | `stub.Verify()` |
| `It.IsAny<T>()` | Callback receives all args |
| `It.Is<T>(pred)` | `stub.Method.When(pred).Returns(val)` |
| `mock.CallBase = true` | `stub.Source(realImpl)` (interface stubs only) |

---

## Step 1: Install KnockOff

Replace the Moq package with KnockOff.

```bash
# Remove Moq:
dotnet remove package Moq

# Add KnockOff:
dotnet add package KnockOff
```

---

## Step 2: Create Stubs

Replace `Mock<T>` instances with KnockOff stub classes.

**Moq:**

<!-- snippet: moq-migration-create-stub-moq -->
```cs
// Create mock wrapper, access instance via .Object
var mock = new Mock<IMoqUserRepo>();
IMoqUserRepo repository = mock.Object;
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: moq-migration-stub-declaration -->
```cs
[KnockOff]
public partial class MoqUserRepoStub : IMoqUserRepo { }
```
<!-- endSnippet -->

<!-- snippet: moq-migration-create-stub-knockoff -->
```cs
// Stub IS the instance - no wrapper needed
var stub = new MoqUserRepoStub();
IMoqUserRepo repository = stub;
```
<!-- endSnippet -->

**Key differences:**
- Moq creates wrapper objects at runtime
- KnockOff requires a partial class declaration—the generator fills in the implementation
- You use the stub instance directly (no `.Object` property)

---

## Step 3: Configure Methods

Replace `.Setup().Returns()` with `Returns`/`Execute` property assignments.

**Moq:**

<!-- snippet: moq-migration-setup-method-moq -->
```cs
// Setup with expression tree and It.IsAny<T>() matcher
mock.Setup(x => x.GetUser(It.IsAny<int>())).Returns(testUser);
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: moq-migration-setup-method-knockoff -->
```cs
// OnCall with typed delegate - arguments available directly
stub.GetUser.Returns((id) => testUser);
```
<!-- endSnippet -->

**Key differences:**
- Moq uses fluent setup with expression trees
- KnockOff uses direct property assignment with typed delegates
- KnockOff's callback receives actual argument values (no `It.IsAny<T>()` needed)

---

## Step 4: Configure Properties

Replace property `.Setup().Returns()` with `Get()` calls.

**Moq:**

<!-- snippet: moq-migration-setup-property-moq -->
```cs
// Properties use same Setup/Returns pattern as methods
mock.Setup(x => x.ConnectionString).Returns("server=localhost");
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: moq-migration-setup-property-knockoff -->
```cs
// Get configures property getter return value
stub.ConnectionString.Get("server=localhost");
```
<!-- endSnippet -->

**Key differences:**
- Moq treats properties like methods in setup
- KnockOff provides `Get()` and `Set()` methods on the property interceptor
- KnockOff also provides `VerifyGet()` and `VerifySet()` for separate getter/setter verification

---

## Step 5: Verify Calls

Replace Moq's `.Verify()` calls with KnockOff's `.Verify()` or `.Verifiable()` API.

**Moq:**

<!-- snippet: moq-migration-verify-moq -->
```cs
// Verify with expression tree and Times constraint
mock.Verify(x => x.SaveUser(It.IsAny<User>()), Moq.Times.Once());
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: moq-migration-verify-knockoff -->
```cs
// Mark as verifiable during setup, then verify all at once
stub.SaveUser.Execute((user) => { }).Verifiable();
```
<!-- endSnippet -->

**Key differences:**
- Moq uses `mock.Verify(expression, times)` with expression trees
- KnockOff has three verification approaches:
  - `tracking.Verify(times)` on the object returned by `Returns`/`Execute`
  - `stub.Method.Verify(times)` directly on the interceptor property
  - `.Verifiable()` + `stub.Verify()` for batch verification
- Both support the same `Times` matchers (Once, AtLeastOnce, Exactly, etc.)

---

## Step 6: Async Methods

Replace `.ReturnsAsync()` with value overloads (auto-wrapped) or callbacks with `Task.FromResult()`.

**Moq:**

<!-- snippet: moq-migration-async-moq -->
```cs
// ReturnsAsync helper wraps value in Task
mock.Setup(x => x.GetUserAsync(It.IsAny<int>())).ReturnsAsync(testUser);
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: moq-migration-async-knockoff -->
```cs
// Use Task.FromResult to wrap the return value
stub.GetUserAsync.Returns((id) => Task.FromResult<User?>(testUser));
```
<!-- endSnippet -->

**Key differences:**
- Moq provides `.ReturnsAsync()` helper
- KnockOff `Returns(value)` and simplified `Returns(callback)` auto-wrap in `Task.FromResult()`
- Return the unwrapped type from callbacks - KnockOff handles the Task wrapping
- Only use explicit `Task.FromResult()` when your callback needs actual async operations
- For exceptions: return `Task.FromException<T>(exception)`

---

## Step 7: Callbacks

Replace `.Callback()` with logic directly in `Returns`/`Execute` callbacks.

**Moq:**

<!-- snippet: moq-migration-callback-moq -->
```cs
// Callback is separate from Returns
mock.Setup(x => x.SaveUser(It.IsAny<User>()))
    .Callback<User>(u => savedUsers.Add(u));
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: moq-migration-callback-knockoff -->
```cs
// Logic goes directly in OnCall delegate
stub.SaveUser.Execute((user) => savedUsers.Add(user));
```
<!-- endSnippet -->

**Key differences:**
- Moq separates `.Callback()` and `.Returns()`
- KnockOff combines them in a single delegate—add logic, then return a value if needed
- You can access arguments directly by name

---

## Step 8: Argument Matching

Replace `It.IsAny<T>()` matchers with callback logic.

**Moq:**

<!-- snippet: moq-migration-arguments-moq -->
```cs
// It.Is<T>() for conditional matching, Returns<T> to access args
mock.Setup(x => x.GetUser(It.Is<int>(id => id > 0)))
    .Returns<int>(id => new User { Id = id, Name = "Valid User" });
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: moq-migration-arguments-knockoff -->
```cs
// Arguments available directly - use standard C# conditionals
stub.GetUser.Returns((id) =>
    id > 0 ? new User { Id = id, Name = "Valid User" } : null);
```
<!-- endSnippet -->

**Key differences:**
- Moq uses `It.IsAny<T>()` and `It.Is<T>()` for argument matching
- KnockOff callbacks receive all arguments—implement your own conditional logic
- For verification, inspect `CallHistory` to check specific argument values

---

## Complete Before/After Example

This example shows a full test class migrated from Moq to KnockOff.

### Before: Moq

<!-- snippet: moq-migration-complete-moq -->
```cs
// Setup with expression tree
_mockRepo.Setup(x => x.GetUserAsync(1)).ReturnsAsync(user);

var result = await _service.GetUserAsync(1);

// Verify with expression tree and Times
_mockRepo.Verify(x => x.GetUserAsync(1), Moq.Times.Once());
```
<!-- endSnippet -->

### After: KnockOff

<!-- snippet: moq-migration-complete-knockoff -->
```cs
// OnCall with Verifiable marks for batch verification
_stub.GetUserAsync.Returns((id) => Task.FromResult<User?>(user)).Verifiable();

var result = await _service.GetUserAsync(1);

// stub.Verify() checks all .Verifiable() members
_stub.Verify();
```
<!-- endSnippet -->

**What changed:**
- Added stub class declaration with `[KnockOff<IUserRepository>]`
- Replaced `Mock<T>` with stub instance
- Replaced `.Setup()` with interceptor property assignments
- Replaced `.Verify()` with direct assertions
- Removed `.Object` property accesses

**What stayed the same:**
- Test logic and assertions
- Test structure and organization
- Coverage and test goals

---

## Common Gotchas

### Forgetting the `partial` Keyword

**Problem:** Stub class isn't marked `partial`, causing duplicate member errors.

<!-- snippet: moq-migration-gotcha-partial-wrong -->
```cs
// Wrong
[KnockOff<IMoqUserRepo>]
class MoqUserRepoStubWrong { }
```
<!-- endSnippet -->

<!-- snippet: moq-migration-gotcha-partial-correct -->
```cs
// Correct
[KnockOff<IMoqUserRepo>]
partial class MoqUserRepoStubCorrect { }
```
<!-- endSnippet -->

### Wrong Callback Signature

**Problem:** Callback signature doesn't match the method parameters.

<!-- snippet: moq-migration-gotcha-signature-wrong -->
```cs
// Wrong: GetUser(int id) expects (int) callback
// stub.GetUser.OnCall(() => user);  // Compile error
```
<!-- endSnippet -->

<!-- snippet: moq-migration-gotcha-signature-correct -->
```cs
// Correct
stub.GetUser.Returns((id) => user);
```
<!-- endSnippet -->

### Forgetting `.Object` Equivalence

**Problem:** Trying to use `.Object` like in Moq when it doesn't exist.

<!-- snippet: moq-migration-gotcha-object-moq -->
```cs
// Moq: needed .Object
var mock = new Mock<IMoqUserRepo>();
var moqService = new UserServiceMigration(mock.Object);
```
<!-- endSnippet -->

<!-- snippet: moq-migration-gotcha-object-knockoff -->
```cs
// KnockOff: use stub directly
var stub = new MoqUserRepoStub();
var knockoffService = new UserServiceMigration(stub);
```
<!-- endSnippet -->

### Async Methods - Both Overloads Auto-Wrap

**Note:** KnockOff auto-wraps both Returns and simplified callbacks for async methods:

<!-- snippet: moq-migration-gotcha-async-autowrap -->
```cs
// Returns - auto-wraps in Task.FromResult
stub.GetUserAsync.Returns(user);

// Simplified callback - also auto-wraps (return unwrapped type)
stub.GetUserAsync.Returns((id) => user);

// Only use Task.FromResult when callback needs actual async operations
stub.GetUserAsync.Returns(async (id) =>
{
    await Task.Delay(1); // Some actual async work
    return user;
});
```
<!-- endSnippet -->

### Property Configuration

**Problem:** Forgetting properties use `Get()` and `Set()`, not `Returns()`/`Execute()`.

<!-- snippet: moq-migration-gotcha-property-wrong -->
```cs
// Wrong: OnCall is for methods
// stub.ConnectionString.OnCall(() => "connection");  // Compile error
```
<!-- endSnippet -->

<!-- snippet: moq-migration-gotcha-property-correct -->
```cs
// Correct: use Get for property getters
stub.ConnectionString.Get("connection");

// For setters, use Set
stub.ConnectionString.Set((value) => { /* handle set */ });
```
<!-- endSnippet -->

### Void Methods Need Empty Delegate Body

**Problem:** Forgetting that void methods still need a delegate body.

<!-- snippet: moq-migration-gotcha-void-wrong -->
```cs
// Wrong: no delegate body
// stub.SaveUser.OnCall();  // Compile error
```
<!-- endSnippet -->

<!-- snippet: moq-migration-gotcha-void-correct -->
```cs
// Correct
stub.SaveUser.Execute((user) => { });
```
<!-- endSnippet -->

---

## Times Matcher Reference

KnockOff supports these `Times` matchers:

| Matcher | Description |
|---------|-------------|
| `Times.Never` | Method was never called |
| `Times.Once` | Method was called exactly once |
| `Times.AtLeastOnce` | Method was called one or more times |
| `Times.AtLeast(n)` | Method was called at least n times |
| `Times.AtMost(n)` | Method was called at most n times |
| `Times.Exactly(n)` | Method was called exactly n times |

**Note:** Unlike Moq, KnockOff does NOT have `Times.Between()`. Use separate `AtLeast` and `AtMost` checks instead.

**Example:**

<!-- snippet: moq-migration-times-example -->
```cs
// Moq
mock.Verify(x => x.SaveUser(It.IsAny<User>()), Moq.Times.Exactly(3));

// KnockOff
stub.SaveUser.Verify(Times.Exactly(3));

// For range verification (no Times.Between in KnockOff):
stub.SaveUser.Verify(Times.AtLeast(1));
stub.SaveUser.Verify(Times.AtMost(5));
```
<!-- endSnippet -->

---

## Migration Checklist

Use this checklist when migrating a test file from Moq to KnockOff:

- [ ] Add KnockOff NuGet package
- [ ] Remove Moq NuGet package
- [ ] Create stub class declarations with `[KnockOff<T>]` attribute
- [ ] Ensure stub classes are marked `partial`
- [ ] Replace `Mock<T>` field declarations with stub types
- [ ] Remove `.Object` property accesses
- [ ] Convert method `.Setup().Returns()` to `.Returns(callback)` or `.Execute(callback)`
- [ ] Convert property setups to `.Get()` and `.Set()`
- [ ] Convert `.ReturnsAsync()` to `Task.FromResult()`
- [ ] Move `.Callback()` logic into `Returns`/`Execute` callbacks
- [ ] Replace `It.IsAny<T>()` with callback parameter inspection
- [ ] Convert `.Verify()` calls to `tracking.Verify()`, `stub.Method.Verify()`, or batch `stub.Verify()`
- [ ] Update `using` statements (remove Moq, add KnockOff namespace if needed)

---

**UPDATED:** 2026-02-07
