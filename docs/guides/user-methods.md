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

User methods provide permanent compile-time behavior. To override them, use `Source()` delegation (see the [Source Delegation guide](source-delegation.md)).

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

    // Interceptor tracks that the method was called - verify with Times
    stub.GetUserById2.Verify(Times.Once);
    Assert.Equal(1, stub.GetUserById2.LastArg);
}
```
<!-- endSnippet -->

---

## Tracking and Verification

User method interceptors provide call tracking without behavior configuration. Use them to verify the user method was called with expected arguments.

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
    stub.IsActive2.Verify(Times.Once);
    Assert.Equal(42, stub.IsActive2.LastArg);
}
```
<!-- endSnippet -->

User method interceptors have the same verification API as regular interceptors: `Verify()`, `LastArg`, and `Reset()`. They omit `OnCall` because the protected method defines the behavior.

---

## Resetting Call Tracking

Call `Reset()` on the interceptor to clear call count and argument tracking. The user method behavior remains unchanged since it's defined in your protected method.

<!-- snippet: user-methods-reset -->
```cs
[Fact]
public void Reset_ClearsUserMethodTracking()
{
    var stub = new UserMethodsRepoStub();
    IUserMethodsRepo repository = stub;

    // Call method
    repository.GetBalance(1);
    stub.GetBalance2.Verify(Times.Once);

    // Reset clears tracking
    stub.GetBalance2.Reset();
    stub.GetBalance2.Verify(Times.Never);

    // User method still works after reset
    var balance = repository.GetBalance(2);
    Assert.Equal(100.00m, balance);
    stub.GetBalance2.Verify(Times.Once);
}
```
<!-- endSnippet -->

This is useful when reusing a stub instance across multiple test phases or when you need to verify calls made during a specific portion of the test.

---

## Overriding User Method Behavior

User method interceptors do not have `OnCall`. To override user method behavior in specific tests, use `Source()` delegation.

<!-- snippet: user-methods-source-override -->
```cs
[Fact]
public void WhenOverrideNeeded_UseRegularStubWithOnCall()
{
    // Use a stub WITHOUT user methods when you need OnCall
    var stub = new OverridableRepoStub();

    // Configure specific behavior with OnCall
    stub.GetUserById.OnCall((id) => new User { Id = id, Name = "Overridden" });
    stub.IsActive.OnCall(true);
    stub.GetBalance.OnCall(999.99m);

    IUserMethodsRepo repository = stub;

    // OnCall provides the behavior
    var user = repository.GetUserById(1);
    Assert.Equal("Overridden", user!.Name);
    Assert.True(repository.IsActive(1));
    Assert.Equal(999.99m, repository.GetBalance(1));

    // Still get full verification
    stub.GetUserById.Verify(Times.Once);
}
```
<!-- endSnippet -->

The `Source()` method allows you to delegate to a different implementation for the entire interface. See the [Source Delegation guide](source-delegation.md) for details.

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

    // All calls are tracked via *2 interceptors - verify with Times
    stub.GetUserById2.Verify(Times.Once);
    stub.IsActive2.Verify(Times.Once);
    stub.GetBalance2.Verify(Times.Once);
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

    // Verify call count using Times
    stub.GetUserById2.Verify(Times.Exactly(3));
    Assert.Equal(3, stub.GetUserById2.LastArg); // Last call was id=3
}
```
<!-- endSnippet -->

---

## Key Takeaways

- User methods only work with the Stand-Alone pattern (`[KnockOff]` on class)
- Define protected methods matching interface member signatures
- They provide compile-time behavior that cannot be changed with `OnCall`
- Interceptors provide tracking only: `Verify()`, `LastArg`, `Reset()`
- No `OnCall` available on user method interceptors
- To override behavior, use `Source()` delegation (see [Source Delegation guide](source-delegation.md))
- Ideal for shared test data and common "happy path" scenarios where behavior is constant
