# User Methods: Compile-Time Defaults

KnockOff allows you to define **user methods** in your stub classes to provide default behavior. This is a key differentiator from Moq, which requires runtime setup for every behavior.

## How It Works

Define protected methods in your stub class matching the interface signature. The generator detects them and calls them automatically.

<!-- snippet: user-methods-stub -->
```cs
[KnockOff]
public partial class DuUserRepositoryStub : IDuUserRepository
{
    // Default: return a test user for any ID
    protected DuUser? GetById(int id) => new DuUser { Id = id, Name = $"User-{id}" };

    // Save: no user method = requires OnCall setup or will use default void behavior
}
```
<!-- endSnippet -->

Now every test using `DuUserRepositoryStub` gets this default behavior automatically:

<!-- snippet: user-methods-usage -->
```cs
public static void UserMethodUsage()
{
    var stub = new DuUserRepositoryStub();
    IDuUserRepository repo = stub;

    // User method provides default behavior automatically
    var user = repo.GetById(42);

    Assert.Equal(42, user?.Id);
    Assert.Equal("User-42", user?.Name);
}
```
<!-- endSnippet -->

## Full Tracking Support

User methods get the same tracking capabilities as generated methods:

<!-- snippet: user-methods-tracking -->
```cs
public static void UserMethodWithTracking()
{
    var stub = new DuUserRepositoryStub();
    IDuUserRepository repo = stub;

    repo.GetById(42);
    repo.GetById(99);

    // User methods still get full tracking
    Assert.Equal(2, stub.GetById2.CallCount);
    Assert.Equal(99, stub.GetById2.LastArg);
}
```
<!-- endSnippet -->

## When to Use User Methods

| Scenario | User Method? |
|----------|--------------|
| Same behavior in all tests | ✓ Yes |
| Shared stub across test files | ✓ Yes |
| Want compile-time guarantees | ✓ Yes |
| Different behavior per test | Use `OnCall` on methods without user implementations |

## Example: Repository with Defaults

<!-- snippet: user-methods-order-stub -->
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

<!-- snippet: user-methods-order-defaults -->
```cs
public static void OrderRepositoryDefaults()
{
    var stub = new DuOrderRepositoryStub();
    IDuOrderRepository repo = stub;

    // GetById returns a pending order by default
    var order = repo.GetById(123);
    Assert.Equal("Pending", order?.Status);

    // GetByCustomer returns empty by default
    var orders = repo.GetByCustomer(1);
    Assert.Empty(orders);
}
```
<!-- endSnippet -->

## Why This Matters

1. **DRY defaults** — Define once, use everywhere
2. **Focused tests** — Tests only show what's different
3. **Compile-time safety** — Errors caught before runtime
4. **Clear intent** — User method = "normal" behavior for this stub

## Next

- [Compile-Time Safety](compile-time-safety.md) — Catch errors before runtime
- [Readability](readability.md) — Less ceremony than Moq
