# Migrating from Moq to KnockOff

Switching from Moq to KnockOff means moving from runtime reflection to compile-time source generation. You gain compile-time safety, debuggability, and performance—while trading Moq's runtime flexibility for a simpler, more explicit API.

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
| `mock.Object` | `stub` (direct instance) |
| `.Setup(x => x.Method()).Returns(value)` | `stub.IFoo_Method.OnCall = () => value` |
| `.Setup(x => x.Property).Returns(value)` | `stub.IFoo_Property.Value = value` |
| `.ReturnsAsync(value)` | `stub.IFoo_Method.OnCall = () => Task.FromResult(value)` |
| `.Callback(x => ...)` | Logic in `OnCall` delegate |
| `.Verify(x => x.Method(), Times.Once)` | `Assert.Equal(1, stub.IFoo_Method.CallCount)` |
| `It.IsAny<T>()` | Callback receives all arguments for inspection |

---

## Step 1: Install KnockOff

Replace the Moq package with KnockOff.

<!-- snippet: moq-migration-install -->
```cs
// Remove Moq:
// dotnet remove package Moq
//
// Add KnockOff:
// dotnet add package KnockOff
```
<!-- endSnippet -->

---

## Step 2: Create Stubs

Replace `Mock<T>` instances with KnockOff stub classes.

**Moq:**

<!-- snippet: moq-migration-create-stub-moq -->

**KnockOff:**

<!-- snippet: moq-migration-create-stub-knockoff -->

**Key differences:**
- Moq creates wrapper objects at runtime
- KnockOff requires a partial class declaration—the generator fills in the implementation
- You use the stub instance directly (no `.Object` property)

---

## Step 3: Configure Methods

Replace `.Setup().Returns()` with `OnCall` property assignments.

**Moq:**

<!-- snippet: moq-migration-setup-method-moq -->

**KnockOff:**

<!-- snippet: moq-migration-setup-method-knockoff -->

**Key differences:**
- Moq uses fluent setup with expression trees
- KnockOff uses direct property assignment with typed delegates
- KnockOff's callback receives actual argument values (no `It.IsAny<T>()` needed)

---

## Step 4: Configure Properties

Replace property `.Setup().Returns()` with `.Value` assignments.

**Moq:**

<!-- snippet: moq-migration-setup-property-moq -->

**KnockOff:**

<!-- snippet: moq-migration-setup-property-knockoff -->

**Key differences:**
- Moq treats properties like methods in setup
- KnockOff provides a `.Value` property on the interceptor
- KnockOff also tracks `GetCount` and `SetCount` for verification

---

## Step 5: Verify Calls

Replace `.Verify()` calls with standard assertions on interceptor properties.

**Moq:**

<!-- snippet: moq-migration-verify-moq -->

**KnockOff:**

<!-- snippet: moq-migration-verify-knockoff -->

**Key differences:**
- Moq uses `.Verify()` with Times matchers
- KnockOff exposes `CallCount` and `WasCalled` properties for direct assertions
- Use your test framework's standard assertions (xUnit, NUnit, etc.)

---

## Step 6: Async Methods

Replace `.ReturnsAsync()` with `Task.FromResult()` in `OnCall`.

**Moq:**

<!-- snippet: moq-migration-async-moq -->

**KnockOff:**

<!-- snippet: moq-migration-async-knockoff -->

**Key differences:**
- Moq provides `.ReturnsAsync()` helper
- KnockOff uses standard `Task.FromResult()` or `Task.CompletedTask`
- For exceptions: return `Task.FromException<T>(exception)`

---

## Step 7: Callbacks

Replace `.Callback()` with logic directly in `OnCall` delegates.

**Moq:**

<!-- snippet: moq-migration-callback-moq -->

**KnockOff:**

<!-- snippet: moq-migration-callback-knockoff -->

**Key differences:**
- Moq separates `.Callback()` and `.Returns()`
- KnockOff combines them in a single delegate—add logic, then return a value if needed
- You can access arguments directly by name

---

## Step 8: Argument Matching

Replace `It.IsAny<T>()` matchers with callback logic.

**Moq:**

<!-- snippet: moq-migration-arguments-moq -->

**KnockOff:**

<!-- snippet: moq-migration-arguments-knockoff -->

**Key differences:**
- Moq uses `It.IsAny<T>()` and `It.Is<T>()` for argument matching
- KnockOff callbacks receive all arguments—implement your own conditional logic
- For verification, inspect `CallHistory` to check specific argument values

---

## Complete Before/After Example

This example shows a full test class migrated from Moq to KnockOff.

### Before: Moq

<!-- snippet: moq-migration-complete-moq -->

### After: KnockOff

<!-- snippet: moq-migration-complete-knockoff -->

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

```csharp
// Wrong
[KnockOff<IUserRepository>]
class UserRepositoryStub { }

// Correct
[KnockOff<IUserRepository>]
partial class UserRepositoryStub { }
```

### Wrong `OnCall` Signature

**Problem:** Callback signature doesn't match the method parameters.

```csharp
// Wrong: GetUser(int id) expects (int) callback
stub.IUserRepository_GetUser.OnCall = () => user;

// Correct
stub.IUserRepository_GetUser.OnCall = (id) => user;
```

### Forgetting `.Object` Equivalence for Class Stubs

**Problem:** Expecting to access a wrapper object when using class stubs.

```csharp
// Moq: needed .Object
var service = new UserService(mock.Object);

// KnockOff: use stub directly
var service = new UserService(stub);
```

---

## Next Steps

- **[Getting Started Guide](../getting-started.md)** - Learn KnockOff patterns from scratch
- **[Interceptor API Reference](../api/interceptors.md)** - Deep dive into `OnCall`, `OnGet`, `OnSet`
- **[Verification Guide](../guides/verification.md)** - Advanced call tracking and verification patterns

---

**Need help?** Open an issue on [GitHub](https://github.com/neatoodotnet/KnockOff/issues) or check existing discussions.
