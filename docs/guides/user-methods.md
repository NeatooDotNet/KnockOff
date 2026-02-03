[Home](../../README.md) / [Guides](../guides/) / User Methods

# User Methods

User methods let you define default stub behavior at compile time by writing protected methods in your stub class. Tests can override these defaults using `OnCall()` when needed.

**Availability**: User methods work only with the **Stand-Alone pattern** (`[KnockOff]` on a class implementing an interface). They are not available in Inline Interface or Inline Class patterns.

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
    // Protected method matches interface member signature
    // This is the fallback when no OnCall is configured
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

1. KnockOff generates an explicit interface implementation
2. When called, the interceptor checks for `OnCall` configuration first
3. If `OnCall` is configured, it supersedes the user method
4. If no `OnCall` is configured, the user method is called as the fallback

This allows you to define sensible defaults while still being able to override them in specific tests.

<!-- snippet: user-methods-fallback -->
```cs
// No OnCall configured - user method provides behavior
var user = repository.GetUserById(1);

// Verify the call was tracked
stub.GetUserById2.Verify(Times.Once);
```
<!-- endSnippet -->

---

## Overriding with OnCall

Use `OnCall()` to override the user method for specific tests. The callback supersedes the user method.

<!-- snippet: user-methods-oncall -->
```cs
// OnCall supersedes the user method
stub.GetUserById2.OnCall(id => new User { Id = id, Name = "Overridden" });

var user = repository.GetUserById(42);
Assert.Equal("Overridden", user!.Name);
```
<!-- endSnippet -->

For constant return values, use `Returns()`:

<!-- snippet: user-methods-returns -->
```cs
// Returns() for constant values
stub.GetBalance2.Returns(500.00m);

var balance = repository.GetBalance(1);
Assert.Equal(500.00m, balance);
```
<!-- endSnippet -->

For async methods, `Returns()` auto-wraps the value in `Task.FromResult`:

<!-- snippet: user-methods-async-returns -->
```cs
// Returns auto-wraps value in Task.FromResult for async methods
stub.GetUserByIdAsync2.Returns(new User { Id = 99, Name = "Test User" });

var user = await repository.GetUserByIdAsync(99);
Assert.Equal("Test User", user!.Name);
```
<!-- endSnippet -->

---

## Tracking and Verification

User method interceptors provide full tracking capabilities. Tracking works the same whether using the user method or an `OnCall` override.

<!-- snippet: user-methods-tracking -->
```cs
stub.IsActive2.Returns(false);
repository.IsActive(42);

// Tracking works whether using OnCall or user method
stub.IsActive2.Verify(Times.Once);
Assert.Equal(42, stub.IsActive2.LastArg);
```
<!-- endSnippet -->

User method interceptors have the same tracking API as regular interceptors: `Verify()`, `LastArg`, and `Reset()`.

---

## Resetting Call Tracking

Call `Reset()` to clear call count and argument tracking. The `OnCall` configuration is preserved.

<!-- snippet: user-methods-reset -->
```cs
// Reset clears tracking state but preserves OnCall configuration
stub.GetBalance2.Reset();
stub.GetBalance2.Verify(Times.Never);
```
<!-- endSnippet -->

This is useful when reusing a stub instance across multiple test phases or when you need to verify calls made during a specific portion of the test.

---

## Shareable Stub Pattern

User methods enable a powerful pattern: define sensible defaults in a base stub class, then override specific methods in tests that need different behavior.

<!-- snippet: user-methods-shareable-base -->
```cs
[KnockOff]
public partial class NotificationServiceStub : INotificationService { }

public partial class NotificationServiceStub
{
    // Default: emails succeed
    protected bool SendEmail(string to, string subject) => true;

    // Default: no pending notifications
    protected int GetPendingCount() => 0;
}
```
<!-- endSnippet -->

Most tests use the defaults:

<!-- snippet: user-methods-shareable-default -->
```cs
// Most tests use the defaults
var sent = service.SendEmail("user@test.com", "Welcome");
Assert.True(sent); // Default behavior: success
```
<!-- endSnippet -->

Specific tests override when needed:

<!-- snippet: user-methods-shareable-override -->
```cs
// Specific test overrides to simulate failure
stub.SendEmail2.Returns(false);

var sent = service.SendEmail("user@test.com", "Welcome");
Assert.False(sent); // OnCall supersedes user method
```
<!-- endSnippet -->

This pattern keeps test code DRY while maintaining flexibility for edge cases.

---

## Complete Example

<!-- snippet: user-methods-complete-example -->
```cs
// User method provides default; OnCall can override
var user = repository.GetUserById(42);
stub.GetUserById2.Verify(Times.Once);

// Override for next call
stub.GetUserById2.OnCall(id => new User { Id = id, Name = "Custom" });
var customUser = repository.GetUserById(99);
Assert.Equal("Custom", customUser!.Name);
```
<!-- endSnippet -->

---

## Key Takeaways

- User methods only work with the Stand-Alone pattern (`[KnockOff]` on class)
- Define protected methods matching interface member signatures
- User methods are the fallback when no `OnCall` is configured
- `OnCall()` supersedes the user method when configured
- `Returns()` provides constant values (auto-wraps for async methods)
- `Reset()` clears tracking but preserves `OnCall` configuration
- Ideal for the shareable stub pattern: defaults in base class, overrides in tests

Next: [Source Delegation](source-delegation.md) for partial stubbing patterns where you want to delegate to a real implementation.

---

**UPDATED:** 2026-02-02
