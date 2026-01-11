# Best Practices

This guide consolidates best practices for using KnockOff effectively. These patterns help you write maintainable, readable tests.

## Embrace Source Generation

**KnockOff generates all interface members automatically.** You don't need to:
- Worry about interface complexity
- Count how many methods an interface has
- Manually implement anything
- Assess whether an interface is "too large"

### The Only Question

When considering KnockOff for any interface, ask:

> "Can I add `[KnockOff<TInterface>]` and configure the 1-3 members my test actually calls?"

The answer is almost always **yes**.

### Complex Interfaces Are Fine

```csharp
// IEditBase has 50+ members? Doesn't matter.
[KnockOff<IEditBase>]
public partial class MyTests
{
    // Generator creates everything. Configure only what you need:
    // stub.IsValid.OnGet = (ko) => true;
    // stub.IsDirty.OnGet = (ko) => false;
    // The other 48 members work with smart defaults.
}
```

### Anti-Pattern: Manual Test Doubles

**Never** create hand-written test doubles when KnockOff would work:

```csharp
// WRONG - Don't do this
public class FakeEditBase : IEditBase
{
    public bool IsValid => true;
    public bool IsDirty => false;
    // ... 48 more manual implementations
}
```

This defeats the purpose of having a source generator. Use `[KnockOff<IEditBase>]` instead.

## Stub Minimalism

**Only stub what the test needs.** Don't implement every interface member—let smart defaults handle the rest.

<!-- snippet: docs:best-practices:stub-minimalism -->
```csharp
[KnockOff]
public partial class BpUserServiceKnockOff : IBpUserService
{
    // Only define methods needing custom behavior
    protected User? GetUser(int id) => new User { Id = id };
    // GetCount() returns 0, GetUsers() returns empty list, etc.
}
```
<!-- /snippet -->

```csharp
// BAD - over-stubbing
[KnockOff]
public partial class UserServiceKnockOff : IUserService
{
    protected User? GetUser(int id) => new User { Id = id };
    protected int GetCount() => 0;  // Unnecessary - smart default does this
    protected List<User> GetUsers() => new();  // Unnecessary
    protected void Save(User u) { }  // Unnecessary - void methods work by default
}
```

## Choose the Right Pattern

### The Duality: User Methods vs Callbacks

KnockOff provides two complementary patterns:

| Pattern | When Defined | Scope | Use Case |
|---------|--------------|-------|----------|
| **User Methods** | Compile-time | All tests using this stub | Consistent default behavior |
| **Callbacks** | Runtime | Per-test | Test-specific overrides |

**Priority Order**: Callback &rarr; User method &rarr; Smart default

### When to Use User Methods

- Same behavior needed across all tests
- Shared test fixtures
- Simple, deterministic returns
- No access to test-specific state

<!-- snippet: docs:best-practices:user-methods -->
```csharp
[KnockOff]
public partial class BpRepositoryKnockOff : IBpRepository
{
    // Default for ALL tests using this stub
    protected User? GetById(int id) => new User { Id = id, Name = "Test User" };
}
```
<!-- /snippet -->

### When to Use Callbacks

- Different behavior per test
- Dynamic returns based on arguments
- Side effects (capturing, validation)
- Accessing stub state
- Temporarily overriding user methods

<!-- snippet: docs:best-practices:callbacks-test -->
```csharp
public class BpCallbacksExample
{
    public void ReturnsAdmin_WhenIdIs1()
    {
        var knockOff = new BpCallbackRepoKnockOff();
        IBpCallbackRepo repo = knockOff;

        // Override just for this test
        knockOff.GetById.OnCall = (ko, id) => id == 1
            ? new User { Id = 1, Name = "Admin" }
            : null;

        // Use through interface
        var admin = repo.GetById(1);
        var other = repo.GetById(2);
    }
}
```
<!-- /snippet -->

## Property Configuration

### Prefer `Value` Over `OnGet` for Static Data

For simple, static test data, use `Value` instead of `OnGet`:

<!-- snippet: docs:best-practices:value-usage -->
```csharp
public class BpValueUsageExample
{
    public void ConfigureWithValue()
    {
        var knockOff = new BpPropertyServiceKnockOff();

        // GOOD - simple and clear
        knockOff.Name.Value = "John Doe";
        knockOff.IsActive.Value = true;
    }
}
```
<!-- /snippet -->

```csharp
// UNNECESSARY COMPLEXITY
knockOff.Name.OnGet = (ko) => "John Doe";
knockOff.IsActive.OnGet = (ko) => true;
```

### Use `OnGet` for Dynamic Values

Only use `OnGet` when you need computed or dynamic behavior:

<!-- snippet: docs:best-practices:dynamic-onget -->
```csharp
public class BpDynamicOnGetExample
{
    public void ConfigureWithOnGet()
    {
        var knockOff = new BpDynamicServiceKnockOff();
        IBpDynamicService service = knockOff;

        // Different value each call
        var counter = 0;
        knockOff.RequestId.OnGet = (ko) => $"REQ-{++counter}";

        // Depends on stub state
        knockOff.IsReady.OnGet = (ko) => ko.Initialize.WasCalled;
    }
}
```
<!-- /snippet -->

### Decision Guide

| Scenario | Use |
|----------|-----|
| Static test data | `Value` |
| Pre-populate before test | `Value` |
| Different value each call | `OnGet` |
| Depends on other stub state | `OnGet` |
| Throw on access | `OnGet` |

## Understand Reset Behavior

`Reset()` clears tracking and callbacks, but **NOT** backing storage:

<!-- snippet: docs:best-practices:reset-behavior -->
```csharp
public class BpResetBehaviorExample
{
    public void ResetPreservesValue()
    {
        var knockOff = new BpResetServiceKnockOff();
        IBpResetService service = knockOff;

        knockOff.Name.Value = "John";
        service.Name = "Jane";  // SetCount = 1

        knockOff.Name.Reset();

        // After reset:
        // Assert.Equal(0, knockOff.Name.SetCount);  // Tracking cleared
        // Assert.Null(knockOff.Name.OnGet);         // Callback cleared
        // Assert.Equal("Jane", knockOff.Name.Value); // Value preserved!
    }
}
```
<!-- /snippet -->

If you need to clear backing storage, set it explicitly:

```csharp
knockOff.Name.Reset();
knockOff.Name.Value = default;  // Clear backing value
```

## Handle Out/Ref Parameters Correctly

Methods with `out` or `ref` parameters require explicit delegate types:

```csharp
// WRONG - won't compile
knockOff.TryParse.OnCall = (ko, input, out result) => { ... };
```

<!-- snippet: docs:best-practices:out-param-correct -->
```csharp
public class BpOutParamExample
{
    public void ConfigureOutParam()
    {
        var knockOff = new BpParserKnockOff();

        // CORRECT - explicit delegate type
        knockOff.TryParse.OnCall =
            (BpParserKnockOff.TryParseInterceptor.TryParseDelegate)((BpParserKnockOff ko, string input, out int result) =>
            {
                return int.TryParse(input, out result);
            });
    }
}
```
<!-- /snippet -->

**Tracking differences:**
- `out` parameters: NOT tracked (they're outputs)
- `ref` parameters: Tracked with INPUT value (before modification)

## Nested Classes Require Partial Containers

When nesting `[KnockOff]` classes, **all containing classes must be `partial`**:

```csharp
// WRONG - won't compile
public class MyTests
{
    [KnockOff]
    public partial class ServiceKnockOff : IService { }
}
```

<!-- snippet: docs:best-practices:partial-container -->
```csharp
public partial class BpMyTests  // <-- partial required
{
    [KnockOff]
    public partial class BpServiceKnockOff : IBpService { }
}
```
<!-- /snippet -->

This applies at any nesting depth.

## Method Overloads Get Numeric Suffixes

When an interface has overloaded methods, each overload gets its own interceptor:

<!-- snippet: docs:best-practices:method-overloads -->
```csharp
public interface IBpProcessor
{
    void Process(string data);
    void Process(string data, int priority);
}

[KnockOff]
public partial class BpProcessorKnockOff : IBpProcessor { }

public class BpOverloadsExample
{
    public void AccessOverloads()
    {
        var knockOff = new BpProcessorKnockOff();

        // Generated interceptors:
        // knockOff.Process1.CallCount;  // Process(string)
        // knockOff.Process2.CallCount;  // Process(string, int)
    }
}
```
<!-- /snippet -->

Methods without overloads don't get suffixes.

## Stand-Alone vs Inline Stubs

### Prefer Stand-Alone for Shared Interfaces

**If you stub the same interface in multiple test classes, use a stand-alone stub.**

Each `[KnockOff<IFoo>]` attribute generates a complete stub class. Using inline stubs for the same interface across many test classes duplicates all that generated code—interceptors, backing fields, implementations—in every class.

```csharp
// INEFFICIENT - duplicates generated code in every test class
[KnockOff<IUserRepository>]  // Generates Stubs.IUserRepository here
public partial class UserServiceTests { }

[KnockOff<IUserRepository>]  // Generates identical Stubs.IUserRepository here too
public partial class OrderServiceTests { }

[KnockOff<IUserRepository>]  // And again here...
public partial class NotificationTests { }
```

<!-- snippet: docs:best-practices:standalone-stub -->
```csharp
[KnockOff]
public partial class BpUserRepositoryKnockOff : IBpUserRepository
{
    protected User? GetById(int id) => new User { Id = id };
}

// All test classes use the same stub
public class BpUserServiceTests
{
    private readonly BpUserRepositoryKnockOff _repoKnockOff = new();
}
```
<!-- /snippet -->

Benefits of stand-alone stubs:
- **Less generated code** — compiled once, shared everywhere
- **Faster builds** — less code to compile
- **User methods** — define shared default behavior (inline stubs can't do this)

### When to Use Each

| Scenario | Use |
|----------|-----|
| Interface used in multiple test classes | Stand-alone stub |
| Need user methods for default behavior | Stand-alone stub |
| One-off stub for a single test class | Inline stub |
| Need multiple unrelated interfaces in one class | Inline stubs |

### Stand-Alone Stubs

Best for reusable stubs with user methods. See the example above in [Prefer Stand-Alone for Shared Interfaces](#prefer-stand-alone-for-shared-interfaces).

**Constraint:** One interface per class (plus inheritance chain).

### Inline Stubs

Best for test-local stubs or when you need multiple interfaces:

<!-- snippet: docs:best-practices:inline-stubs -->
```csharp
[KnockOff<IBpInlineUserService>]
[KnockOff<IBpInlineLogger>]
public partial class BpInlineTests
{
    public void Test()
    {
        var userStub = new Stubs.IBpInlineUserService();
        var loggerStub = new Stubs.IBpInlineLogger();
        // Configure via callbacks only
    }
}
```
<!-- /snippet -->

## When KnockOff Won't Work

Rare cases where KnockOff cannot be used:
- Sealed classes (can't inherit)
- Delegates with `ref`/`out` parameters (Func<>/Action<> limitation)
- Types requiring complex constructor logic that can't be stubbed

**If uncertain: TRY IT FIRST.** Add the attribute, build, see if it compiles. If it fails, then investigate—don't abandon KnockOff preemptively.

## Summary

| Practice | Do | Don't |
|----------|-----|-------|
| Interface complexity | Use KnockOff for any interface | Create manual test doubles |
| Stub configuration | Configure only tested members | Over-implement every member |
| Static property values | Use `Value` | Use `OnGet` for static data |
| Shared behavior | Use user methods | Duplicate callback setup |
| Per-test behavior | Use callbacks | Modify user methods |
| Shared interfaces | Use stand-alone stubs | Duplicate inline stubs across classes |
| Container classes | Mark as `partial` | Forget `partial` keyword |
| Complex features | Try it first | Assume it won't work |
