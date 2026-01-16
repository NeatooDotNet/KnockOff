# The Duality Pattern

KnockOff provides **two complementary ways** to customize stub behavior. This is a key differentiator from Moq, which only offers runtime configuration.

## The Two Patterns

| Pattern | When Defined | Scope | Best For |
|---------|--------------|-------|----------|
| **User Methods** | Compile-time | All tests using this stub | Consistent defaults |
| **Callbacks** | Runtime | Single test | Test-specific overrides |

## User Methods: Compile-Time Defaults

Define protected methods in your stub class. The generator detects them and calls them as the default behavior.

<!-- snippet: duality-user-repository-stub -->
```cs
[KnockOff]
public partial class DuUserRepositoryStub : IDuUserRepository
{
    // Default: return a test user for any ID
    protected DuUser? GetById(int id) => new DuUser { Id = id, Name = $"User-{id}" };

    // Save: no user method = default behavior (void returns nothing)
}
```
<!-- endSnippet -->

Now every test using `DuUserRepositoryStub` gets this default behavior automatically:

<!-- snippet: duality-user-method-usage -->
```cs
public static void UserMethodUsage()
{
    var stub = new DuUserRepositoryStub();
    IDuUserRepository repo = stub;

    var user = repo.GetById(42);

    Assert.Equal(42, user?.Id);
    Assert.Equal("User-42", user?.Name);
}
```
<!-- endSnippet -->

### When to Use User Methods

- Behavior is the same across many tests
- Stub is shared between test classes
- You want compile-time guarantees

## Callbacks: Runtime Overrides

Set `OnCall`, `OnGet`, or `OnSet` for per-test behavior. Callbacks take precedence over user methods.

<!-- snippet: duality-callback-override -->
```cs
public static void CallbackOverride()
{
    var stub = new DuUserRepositoryStub();

    // Override the user method for just this test
    stub.GetById2.OnCall = (ko, id) => null;

    var user = stub.Object.GetById(999);

    Assert.Null(user);
}
```
<!-- endSnippet -->

### When to Use Callbacks

- Behavior differs between tests
- You need dynamic return values based on arguments
- You're testing error conditions
- You need access to test-local state

## Priority Order

When an interface member is invoked:

<!-- pseudo:duality-priority-order -->
```
1. CALLBACK (if set)
   └─ OnCall for methods, OnGet/OnSet for properties
   └─ If callback exists → use it, stop

2. USER METHOD (if defined)
   └─ Protected method matching interface signature
   └─ If user method exists → call it, stop

3. DEFAULT
   └─ Methods: return default(T)
   └─ Properties: return backing field value
```
<!-- /snippet -->

## Combining Both Patterns

The real power comes from using both together:

<!-- snippet: duality-order-repository-stub -->
```cs
[KnockOff]
public partial class DuOrderRepositoryStub : IDuOrderRepository
{
    // Default: most tests need an order to exist
    protected DuOrder? GetById(int id) => new DuOrder
    {
        Id = id,
        CustomerId = 1,
        Status = "Pending"
    };

    // Default: return empty list (no orders for customer)
    protected IEnumerable<DuOrder> GetByCustomer(int customerId) => [];
}
```
<!-- endSnippet -->

Tests use the defaults, but override when needed:

<!-- snippet: duality-combining-not-found -->
```cs
public static void CombiningNotFound()
{
    var stub = new DuOrderRepositoryStub();
    stub.GetById2.OnCall = (ko, id) => null;  // Override for this test

    // var processor = new OrderProcessor(stub);
    // var result = processor.Process(orderId: 999);
    // Assert.False(result);
}
```
<!-- endSnippet -->

<!-- snippet: duality-combining-multiple-orders -->
```cs
public static void CombiningMultipleOrders()
{
    var stub = new DuOrderRepositoryStub();
    stub.GetByCustomer2.OnCall = (ko, customerId) => new[]
    {
        new DuOrder { Id = 1, CustomerId = customerId },
        new DuOrder { Id = 2, CustomerId = customerId }
    };

    // var service = new CustomerService(stub);
    // var orders = service.GetOrderHistory(customerId: 42);
    // Assert.Equal(2, orders.Count());
}
```
<!-- endSnippet -->

## Reset Returns to Defaults

`Reset()` clears callbacks and tracking, returning to user method behavior:

<!-- snippet: duality-reset-behavior -->
```cs
public static void ResetBehavior()
{
    var stub = new DuUserRepositoryStub();

    // Override default
    stub.GetById2.OnCall = (ko, id) => null;
    var user1 = stub.Object.GetById(1);  // null

    // Reset
    stub.GetById2.Reset();
    var user2 = stub.Object.GetById(1);  // User { Id = 1, Name = "User-1" }

    _ = (user1, user2);
}
```
<!-- endSnippet -->

## Decision Guide

| Question | Answer |
|----------|--------|
| Same behavior in all tests? | → User method |
| Different per test? | → Callback |
| Shared stub across files? | → User method for defaults |
| Testing error conditions? | → Callback |
| Just need a simple return? | → Either works |

## Why This Matters

1. **DRY defaults** — Define once, use everywhere
2. **Focused tests** — Tests only show what's different
3. **Flexible overrides** — Easy to change behavior per test
4. **Clear intent** — User method = "normal", callback = "special case"

## Next

- [Compile-Time Safety](compile-time-safety.md) — Catch errors before runtime
- [Readability](readability.md) — Less ceremony than Moq
