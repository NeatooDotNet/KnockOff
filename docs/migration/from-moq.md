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
| `new Mock<IFoo>()` | `new FooStub()` with `[KnockOff] partial class FooStub : IFoo` |
| `mock.Object` | `stub` (direct instance) |
| `.Setup(x => x.Method()).Returns(value)` | `stub.Method.Return((args) => value)` |
| `.Setup(x => x.Property).Returns(value)` | `stub.Property.OnGet(value)` |
| `.ReturnsAsync(value)` | `stub.Method.Return(value)` (auto-wraps) or `Return((args) => Task.FromResult(value))` |
| `.Callback(x => ...)` | Logic in `Return` delegate |
| `.Verify(x => x.Method(), Times.Once)` | `var t = stub.Method.Return(...); t.Verify(Times.Once)` |
| `.Verifiable()` | `stub.Method.Return(...).Verifiable()` |
| `mock.Verify()` | `stub.Verify()` (checks all `.Verifiable()` calls) |
| `It.IsAny<T>()` | Callback receives all arguments for inspection |

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

<!-- snippet: moq-migration-create-stub-knockoff -->
```cs
// Stub IS the instance - no wrapper needed
var stub = new MoqUserRepoStub();
IMoqUserRepo repository = stub;
```
<!-- endSnippet -->

**Key differences:**
- Moq wraps test doubles in `Mock<T>` objects
- KnockOff uses partial class declarations with source generation
- You use the stub instance directly (no `.Object` property)

**Stub class declaration:**

To enable the above code, declare a partial stub class with the `[KnockOff]` attribute. The source generator creates the implementation.

<!-- snippet: moq-migration-stub-declaration -->
```cs
[KnockOff]
public partial class MoqUserRepoStub : IMoqUserRepo { }
```
<!-- endSnippet -->

Place this declaration in your test file. The generator fills in the explicit interface implementations and interceptor properties.

**Alternative patterns:** KnockOff also supports inline patterns that don't require implementing the interface explicitly:
- `[KnockOff<IFoo>] partial class FooStub` - inline interface pattern
- `[KnockOff<FooClass>] partial class FooStub` - inline class pattern

This guide uses the standalone pattern (`[KnockOff]` with explicit interface implementation) for consistency with Moq's style, but all patterns are functionally equivalent.

---

## Step 3: Configure Methods

Replace `.Setup().Returns()` with `Return`/`Call` property assignments.

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
// Return with typed delegate - arguments available directly
stub.GetUser.Return((id) => testUser);
```
<!-- endSnippet -->

**Key differences:**
- Moq uses fluent setup with expression trees
- KnockOff uses direct property assignment with typed delegates
- KnockOff's callback receives actual argument values (no `It.IsAny<T>()` needed)

---

## Step 4: Configure Properties

Replace property `.Setup().Returns()` with `.OnGet()` calls.

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
// OnGet configures property getter return value
stub.ConnectionString.OnGet("server=localhost");
```
<!-- endSnippet -->

**Key differences:**
- Moq treats properties like methods in setup
- KnockOff provides `.OnGet()` and `.OnSet()` methods on the property interceptor
- KnockOff also provides `.VerifyGet()` and `.VerifySet()` for granular verification

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
stub.SaveUser.Call((user) => { }).Verifiable();
```
<!-- endSnippet -->

**Key differences:**
- Moq uses `mock.Verify(expression, times)` with expression trees
- KnockOff uses `.Verifiable()` + `stub.Verify()` for batch verification
- KnockOff also supports direct verification: `stub.SaveUser.Verify(Times.Once)`
- Both support the same `Times` matchers (Once, AtLeastOnce, Exactly, etc.)

---

## Step 6: Async Methods

Replace `.ReturnsAsync()` with `Return()` (auto-wrapped).

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
stub.GetUserAsync.Return((id) => Task.FromResult<User?>(testUser));
```
<!-- endSnippet -->

**Three async configuration options (simplest first):**

<!-- snippet: moq-gotcha-async-options -->
```cs
// 1. Return() -- auto-wraps in Task.FromResult (recommended for fixed values)
stub.GetUserAsync.Return(testUser);

// 2. Return() simplified -- callback returns unwrapped type, auto-wrapped
stub.GetUserAsync.Return((id) => new User { Id = id });

// 3. Return() full -- callback returns Task<T> directly
stub.GetUserAsync.Return((id) => Task.FromResult<User?>(testUser));
```
<!-- endSnippet -->

**Key differences:**
- Moq provides `.ReturnsAsync()` helper
- KnockOff auto-wraps with `Return(value)` -- identical convenience to Moq's `.ReturnsAsync()`
- `Return()` also supports simplified callbacks that auto-wrap
- For full control: `Return()` with explicit `Task.FromResult()`
- For exceptions: `Return(() => throw new Exception())` (auto-wraps the throw too)

---

## Step 7: Callbacks

Replace `.Callback()` with logic directly in `Return`/`Call` delegates.

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
// Logic goes directly in Return delegate
stub.SaveUser.Call((user) => savedUsers.Add(user));
```
<!-- endSnippet -->

**Key differences:**
- Moq separates `.Callback()` and `.Returns()`
- KnockOff combines them in a single delegate -- add logic, then return a value if needed
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
stub.GetUser.Return((id) =>
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
// Return with Verifiable marks for batch verification
_stub.GetUserAsync.Return((id) => Task.FromResult<User?>(user)).Verifiable();

var result = await _service.GetUserAsync(1);

// stub.Verify() checks all .Verifiable() members
_stub.Verify();
```
<!-- endSnippet -->

**What changed:**
- Added stub class declaration with `[KnockOff]`
- Replaced `Mock<T>` with stub instance
- Replaced `.Setup()` with interceptor property assignments
- Replaced `.Verify()` with `stub.Verify()` and tracking object verification
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

### Wrong `Return`/`Call` Signature

**Problem:** Callback signature doesn't match the method parameters.

<!-- snippet: moq-migration-gotcha-signature-wrong -->
```cs
// Wrong: GetUser(int id) expects (int) callback
// stub.GetUser.Return(() => user);  // Compile error
```
<!-- endSnippet -->

<!-- snippet: moq-migration-gotcha-signature-correct -->
```cs
// Correct
stub.GetUser.Return((id) => user);
```
<!-- endSnippet -->

### No `.Object` Property Needed

**Key difference:** Moq uses `mock.Object` to get the instance, KnockOff stubs are the instance.

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

**Note:** KnockOff stubs do have an `.Object` property for compatibility, but it just returns `this` - you rarely need it.

---

## Next Steps

- **[Getting Started Guide](../getting-started.md)** - Learn KnockOff patterns from scratch
- **[Interceptor API Reference](../reference/interceptor-api.md)** - Deep dive into `Return`, `Call`, `OnGet`, `OnSet`
- **[Verification Guide](../guides/verification.md)** - Advanced call tracking and verification patterns
- **[Methods Guide](../guides/methods.md)** - Configure method behavior and callbacks
- **[Properties Guide](../guides/properties.md)** - Work with property interceptors

---

**Need help?** Open an issue on [GitHub](https://github.com/neatoodotnet/KnockOff/issues) or check existing discussions.

---

**UPDATED:** 2026-02-05
