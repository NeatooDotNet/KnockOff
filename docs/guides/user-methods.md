[Home](../../README.md) / [Guides](../guides/) / User Methods

# User Methods

User methods let you define default stub behavior at compile time by writing protected methods in your stub class. They provide reusable defaults across tests while remaining testable through tracking interceptors.

**Availability**: User methods work only with the **Stand-Alone pattern** (`[KnockOff]` on a class implementing an interface). They are not available in Inline Interface or Inline Class patterns.

**Important**: User method interceptors are tracking-only. They do not have `OnCall` for configuring behavior. The protected method implementation is the behavior.

---

## Defining User Methods

Add a protected method to your `[KnockOff]` stub class. The method signature must match the interface member you want to intercept.

<!-- snippet: user-methods-basic -->
```cs
[KnockOff]
public partial class UserMethodsRepoStub : IUserMethodsRepo { }

// User methods provide default behavior
public partial class UserMethodsRepoStub
{
    // Protected method matches interface method signature
    // This becomes the behavior (user method interceptors have no OnCall)
    protected User? GetUserById(int id)
    {
        return new User { Id = id, Name = "Default User" };
    }

    protected bool IsActive(int userId)
    {
        return true; // Default: users are active
    }

    protected decimal GetBalance(int userId)
    {
        return 100.00m; // Default test balance
    }
}
```
<!-- endSnippet -->

**Signature matching rules:**
- Method name must exactly match the interface member name
- Return type must match
- Parameter types must match (parameter names can differ)
- Must be `protected` (not private, public, or internal)

---

## How User Methods Work

When you define a protected method matching an interface member signature:

1. KnockOff generates an explicit interface implementation that calls your protected method
2. The interceptor tracks calls but does not provide `OnCall` configuration
3. Your protected method implementation provides the behavior
4. Tests verify calls through the interceptor using `Verify()` and `LastArg`

User methods provide permanent compile-time behavior. If you need runtime-configurable behavior, use a regular stub without user methods (see [When You Need Runtime-Configurable Behavior](#when-you-need-runtime-configurable-behavior) below).

<!-- snippet: user-methods-priority -->
```cs
// User method provides default behavior automatically
var user = repository.GetUserById(1);

// Verify the call was tracked (user method interceptors end with "2")
stub.GetUserById2.Verify(Times.Once);
```
<!-- endSnippet -->

---

## Tracking and Verification

User method interceptors provide call tracking without behavior configuration. Use them to verify the user method was called with expected arguments.

<!-- snippet: user-methods-override -->
```cs
// User method interceptors are tracking-only (no OnCall available)
stub.IsActive2.Verify(Times.Once);
Assert.Equal(42, stub.IsActive2.LastArg);
```
<!-- endSnippet -->

User method interceptors have the same verification API as regular interceptors: `Verify()`, `LastArg`, and `Reset()`. They omit `OnCall` because the protected method defines the behavior.

---

## Resetting Call Tracking

Call `Reset()` on the interceptor to clear call count and argument tracking. The user method behavior remains unchanged since it's defined in your protected method.

<!-- snippet: user-methods-reset -->
```cs
// Reset clears call count and argument tracking
stub.GetBalance2.Reset();
stub.GetBalance2.Verify(Times.Never);
```
<!-- endSnippet -->

This is useful when reusing a stub instance across multiple test phases or when you need to verify calls made during a specific portion of the test.

---

## When You Need Runtime-Configurable Behavior

User method interceptors do not have `OnCall` because the protected method defines the behavior at compile time. If you need runtime-configurable behavior for specific tests, use a regular stub without user methods:

<!-- snippet: user-methods-source-override -->
```cs
// For runtime-configurable behavior, use a regular stub with OnCall
stub.GetUserById.OnCall((id) => new User { Id = id, Name = "Overridden" });
stub.IsActive.Returns(true);
stub.GetBalance.Returns(999.99m);
```
<!-- endSnippet -->

User methods provide permanent compile-time behavior. If a test requires different behavior, create a separate stub class without user methods and use `OnCall` to configure it. See the [Methods guide](methods.md) for `OnCall` patterns.

---

## Common Patterns

### Shared Test Data Setup

Define user methods that return consistent test data across all tests. Tests verify behavior using the tracking interceptors.

**Use case**: Repository stubs that return standard test entities by default. Tests verify the stub was called correctly and check the returned data.

### Default "Happy Path" Implementations

Implement the most common success scenario in user methods. Tests verify the happy path executes correctly.

**Use case**: Service stubs where most tests assume operations succeed. Tests can verify success paths without configuring callbacks for every method.

---

## Complete Example

<!-- snippet: user-methods-complete-example -->
```cs
// User methods provide defaults; interceptors track calls
var user = repository.GetUserById(42);
var isActive = repository.IsActive(42);

// Verify with *2 interceptors (user method tracking)
stub.GetUserById2.Verify(Times.Once);
stub.IsActive2.Verify(Times.Once);
```
<!-- endSnippet -->

---

## Key Takeaways

- User methods only work with the Stand-Alone pattern (`[KnockOff]` on class)
- Define protected methods matching interface member signatures
- They provide compile-time behavior that cannot be changed with `OnCall`
- Interceptors provide tracking only: `Verify()`, `LastArg`, `Reset()`
- No `OnCall` available on user method interceptors
- If you need runtime-configurable behavior, use a regular stub without user methods
- Ideal for shared test data and common "happy path" scenarios where behavior is constant

Next: [Source Delegation](source-delegation.md) for partial stubbing patterns where you want to delegate to a real implementation.

---

**UPDATED:** 2026-01-25
