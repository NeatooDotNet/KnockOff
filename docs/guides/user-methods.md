# User Methods

User methods let you define default stub behavior at compile time by writing protected methods in your stub class. They provide reusable defaults across tests while remaining overridable when needed.

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
    // Protected method matches interface method signature
    // This becomes the default behavior when no OnCall is set
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

## Priority Order

When a stub method is called, KnockOff checks for behavior in this order:

1. **OnCall** - Explicitly set callback in test
2. **User Method** - Protected method defined in stub class
3. **Source** - Delegated behavior from `Source(T)` if configured
4. **Smart Default** - Generated default return value

User methods act as compile-time defaults. You can override them with `OnCall` for specific tests without changing the user method.

<!-- snippet: user-methods-priority -->
```cs
[Fact]
public void UserMethod_ProvidesDefaultBehavior()
{
    var stub = new UserMethodsRepoStub();
    IUserMethodsRepo repository = stub;

    // User method provides default behavior automatically
    var user = repository.GetUserById(1);

    Assert.NotNull(user);
    Assert.Equal("Default User", user.Name);

    // Interceptor tracks that the method was called
    Assert.Equal(1, stub.GetUserById2.CallCount);
    Assert.Equal(1, stub.GetUserById2.LastArg);
}
```
<!-- endSnippet -->

---

## Overriding in Tests

Set `OnCall` on the interceptor to override the user method for a specific test scenario.

<!-- snippet: user-methods-override -->
```cs
[Fact]
public void UserMethod_InterceptorTracksCallsOnly()
{
    var stub = new UserMethodsRepoStub();
    IUserMethodsRepo repository = stub;

    // User method returns default value
    var isActive = repository.IsActive(42);
    Assert.True(isActive);

    // User method interceptors are tracking-only
    // They don't have OnCall - use Source delegation to override
    Assert.True(stub.IsActive2.WasCalled);
    Assert.Equal(42, stub.IsActive2.LastArg);
}
```
<!-- endSnippet -->

This lets you keep common "happy path" behavior in the user method while easily testing edge cases.

---

## Resetting to User Method

Call `Reset()` on the interceptor to clear any `OnCall` override and restore the user method as the active behavior.

<!-- snippet: user-methods-reset -->
```cs
[Fact]
public void Reset_ClearsUserMethodTracking()
{
    var stub = new UserMethodsRepoStub();
    IUserMethodsRepo repository = stub;

    // Call method
    repository.GetBalance(1);
    Assert.Equal(1, stub.GetBalance2.CallCount);

    // Reset clears tracking
    stub.GetBalance2.Reset();
    Assert.Equal(0, stub.GetBalance2.CallCount);

    // User method still works after reset
    var balance = repository.GetBalance(2);
    Assert.Equal(100.00m, balance);
    Assert.Equal(1, stub.GetBalance2.CallCount);
}
```
<!-- endSnippet -->

This is useful when reusing a stub instance across multiple test phases.

---

## Common Patterns

### Shared Test Data Setup

Define user methods that return consistent test data across all tests. Override only when a specific test needs different data.

**Use case**: Repository stubs that return standard test entities by default. Tests for empty results or edge cases override with `OnCall`.

### Default "Happy Path" Implementations

Implement the most common success scenario in user methods. Tests for error cases or alternate flows override as needed.

**Use case**: Service stubs where most tests assume operations succeed. Failure scenarios use `OnCall` to simulate errors.

---

## Complete Example

<!-- snippet: user-methods-complete-example -->
```cs
[Fact]
public void StandardUserRetrieval_UsesUserMethodDefaults()
{
    var stub = new UserMethodsRepoStub();
    IUserMethodsRepo repository = stub;

    // All user methods provide defaults automatically
    var user = repository.GetUserById(42);
    var isActive = repository.IsActive(42);
    var balance = repository.GetBalance(42);

    // User methods return expected defaults
    Assert.NotNull(user);
    Assert.Equal("Default User", user.Name);
    Assert.True(isActive);
    Assert.Equal(100.00m, balance);

    // All calls are tracked via *2 interceptors
    Assert.Equal(1, stub.GetUserById2.CallCount);
    Assert.Equal(1, stub.IsActive2.CallCount);
    Assert.Equal(1, stub.GetBalance2.CallCount);
}

[Fact]
public void MultipleCallsTrackedCorrectly()
{
    var stub = new UserMethodsRepoStub();
    IUserMethodsRepo repository = stub;

    // Make multiple calls
    repository.GetUserById(1);
    repository.GetUserById(2);
    repository.GetUserById(3);

    // All calls tracked
    Assert.Equal(3, stub.GetUserById2.CallCount);
    Assert.Equal(3, stub.GetUserById2.LastArg); // Last call was id=3
}
```
<!-- endSnippet -->

---

## Key Takeaways

- User methods only work with the Stand-Alone pattern (`[KnockOff]` on class)
- Define protected methods matching interface member signatures
- They provide compile-time defaults that can be overridden with `OnCall`
- Priority: OnCall > User Method > Source > Smart Default
- Use `Reset()` to clear OnCall and restore user method behavior
- Ideal for shared test data and common "happy path" scenarios
