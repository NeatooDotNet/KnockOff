[Home](../../README.md) / [Guides](../guides/) / Stub Overrides

# Stub Overrides

Stub overrides let you define default stub behavior at compile time by writing protected override methods in your stub class. Tests can override these defaults using `Return(callback)` or `Call(callback)` when needed.

**Availability**: Stub overrides work with all four **Standalone patterns**: `[KnockOff]` on a class implementing an interface (patterns 1, 2) and `[KnockOffBase<T>]` / `[KnockOffBase(typeof(T<>))]` on class stubs (patterns 3, 4). They are not available in Inline patterns.

---

## Defining Stub Overrides

KnockOff generates a base class with virtual methods for each interface member. Override these methods in your partial stub class to provide default behavior.

<!-- snippet: stub-overrides-basic -->
```cs
[KnockOff]
public partial class StubOverrideRepoStub : IStubOverrideRepo { }

// Stub overrides provide default behavior
public partial class StubOverrideRepoStub
{
    // Protected override method with underscore suffix
    // This is the fallback when no Return is configured
    protected override User? GetUserById_(int id)
    {
        return new User { Id = id, Name = "Default User" };
    }

    protected override bool IsActive_(int userId)
    {
        return true; // Default: users are active
    }

    protected override decimal GetBalance_(int userId)
    {
        return 100.00m; // Default test balance
    }
}
```
<!-- endSnippet -->

**The underscore suffix convention:**
- KnockOff generates virtual methods with a `_` suffix (e.g., `GetUserById_`)
- You override these methods using `protected override MethodName_(...)`
- The compiler enforces signature correctness: typos or wrong parameters cause "no suitable method to override" errors

---

## How Stub Overrides Work

KnockOff generates a base class (e.g., `StubOverrideRepoStubBase`) with virtual methods for each interface member:

<!-- snippet: stub-overrides-generated-base -->
```cs
// Generated base class (you don't write this -- KnockOff generates it):
//
//   public partial class StubOverrideRepoStubBase
//   {
//       protected virtual User? GetUserById_(int id) => default!;
//       protected virtual bool IsActive_(int userId) => default!;
//       protected virtual decimal GetBalance_(int userId) => default!;
//   }
//
// Your override in the partial class:
//   protected override User? GetUserById_(int id) => new User { Id = id };
```
<!-- endSnippet -->

When you override a virtual method:

1. The generated interface implementation checks for `Return`/`Call` configuration first
2. If `Return`/`Call` is configured, it supersedes the stub override
3. If no `Return`/`Call` is configured, your override is called as the fallback

This provides compile-time safety: if you typo the method name or get the signature wrong, the compiler reports "no suitable method to override" instead of silently ignoring your code.

<!-- snippet: stub-overrides-fallback -->
```cs
// No Return configured - stub override provides behavior
var user = repository.GetUserById(1);

// Verify the call was tracked
stub.GetUserById.Verify(Called.Once);
```
<!-- endSnippet -->

**Note**: Interceptor properties use clean names (`stub.GetUserById`), not the underscore suffix. The suffix only appears on the overridable method.

---

## Overriding with Return/Call

Use `Return(callback)` or `Call(callback)` to override the stub override for specific tests. The callback supersedes the stub override.

<!-- snippet: stub-overrides-oncall -->
```cs
// Return supersedes the stub override
stub.GetUserById.Return(id => new User { Id = id, Name = "Overridden" });

var user = repository.GetUserById(42);
Assert.Equal("Overridden", user!.Name);
```
<!-- endSnippet -->

For constant return values, use `Return()`:

<!-- snippet: stub-overrides-returns -->
```cs
// Return() for constant values
stub.GetBalance.Return(500.00m);

var balance = repository.GetBalance(1);
Assert.Equal(500.00m, balance);
```
<!-- endSnippet -->

For async methods, `Return()` auto-wraps the value in `Task.FromResult`:

<!-- snippet: stub-overrides-async-returns -->
```cs
// Returns auto-wraps value in Task.FromResult for async methods
stub.GetUserByIdAsync.Return(new User { Id = 99, Name = "Test User" });

var user = await repository.GetUserByIdAsync(99);
Assert.Equal("Test User", user!.Name);
```
<!-- endSnippet -->

---

## Tracking and Verification

Stub override interceptors provide full tracking capabilities. Tracking works the same whether using the stub override or a `Return`/`Call` override.

<!-- snippet: stub-overrides-tracking -->
```cs
stub.IsActive.Return(false);
repository.IsActive(42);

// Tracking works whether using Return or stub override
stub.IsActive.Verify(Called.Once);
Assert.Equal(42, stub.IsActive.LastArg);
```
<!-- endSnippet -->

Stub override interceptors have the same tracking API as regular interceptors: `Verify()`, `LastArg`, and `Reset()`.

---

## Resetting Call Tracking

Call `Reset()` to clear call count and argument tracking. The `Return`/`Call` configuration is preserved.

<!-- snippet: stub-overrides-reset -->
```cs
// Reset clears tracking state but preserves Return configuration
stub.GetBalance.Reset();
stub.GetBalance.Verify(Called.Never);
```
<!-- endSnippet -->

This is useful when reusing a stub instance across multiple test phases or when you need to verify calls made during a specific portion of the test.

---

## Shareable Stub Pattern

Stub overrides enable a powerful pattern: define sensible defaults in a stub class, then override specific methods in tests that need different behavior.

<!-- snippet: stub-overrides-shareable-base -->
```cs
[KnockOff]
public partial class NotificationServiceStub : INotificationService { }

public partial class NotificationServiceStub
{
    // Default: emails succeed
    protected override bool SendEmail_(string to, string subject) => true;

    // Default: no pending notifications
    protected override int GetPendingCount_() => 0;
}
```
<!-- endSnippet -->

Most tests use the defaults:

<!-- snippet: stub-overrides-shareable-default -->
```cs
// Most tests use the defaults
var sent = service.SendEmail("user@test.com", "Welcome");
Assert.True(sent); // Default behavior: success
```
<!-- endSnippet -->

Specific tests override when needed:

<!-- snippet: stub-overrides-shareable-override -->
```cs
// Specific test overrides to simulate failure
stub.SendEmail.Return(false);

var sent = service.SendEmail("user@test.com", "Welcome");
Assert.False(sent); // Return supersedes stub override
```
<!-- endSnippet -->

This pattern keeps test code DRY while maintaining flexibility for edge cases.

---

## Complete Example

<!-- snippet: stub-overrides-complete-example -->
```cs
// Stub override provides default; Return can override
var user = repository.GetUserById(42);
stub.GetUserById.Verify(Called.Once);

// Override for next call
stub.GetUserById.Return(id => new User { Id = id, Name = "Custom" });
var customUser = repository.GetUserById(99);
Assert.Equal("Custom", customUser!.Name);
```
<!-- endSnippet -->

---

## Overloads

Stub overrides work naturally with method overloads. Each overload gets its own virtual method in the generated base class, and you can override any subset of them.

<!-- snippet: stub-overrides-overloads -->
```cs
public partial class StubOverrideFormatterStub
{
    // Override only the overloads you need
    protected override string Format_(string input) => input.ToUpperInvariant();

    // Override other overloads with custom logic
    protected override string Format_(string input, bool uppercase)
        => uppercase ? input.ToUpperInvariant() : input.ToLowerInvariant();
}
```
<!-- endSnippet -->

Overriding one overload does not affect others. The non-overridden overloads work exactly like regular methods: configure them with `Return(callback)` or `Return(value)`, or leave them to return defaults.

---

## Restrictions

**No user-defined base classes**: Standalone stubs cannot have a user-defined base class because KnockOff generates the base class. If you add `: MyBaseClass` to a `[KnockOff]` stub, you will get diagnostic **KO0200** ("Standalone stubs cannot have user-defined base classes").

**Generic methods excluded**: Generic methods (e.g., `T Create<T>()`) are not included in the base class pattern. Use `stub.Create.Of<T>().Return(...)` to configure them instead.

---

## Key Takeaways

- Stub overrides work with all four Standalone patterns (`[KnockOff]` and `[KnockOffBase<T>]`)
- Override virtual methods with underscore suffix (e.g., `protected override string Method_(...)`)
- Interceptor properties use clean names (`stub.Method`), not the underscore suffix
- Compile-time safety: signature mismatches cause "no suitable method to override" errors
- Stub overrides are the fallback when no `Return`/`Call` is configured
- `Return(callback)`/`Call(callback)` supersedes the stub override when configured
- Overloads can be selectively overridden (overriding one does not affect others)
- `Reset()` clears tracking but preserves `Return`/`Call` configuration
- Ideal for the shareable stub pattern: defaults in stub, overrides in specific tests

Next: [Source Delegation](source-delegation.md) for partial stubbing patterns where you want to delegate to a real implementation.

---

**UPDATED:** 2026-02-06
