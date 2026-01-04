# Customization Patterns: The Duality

KnockOff provides **two complementary patterns** for customizing stub behavior. This is a key differentiator from traditional mocking frameworks like Moq, which only offer runtime configuration.

## Overview

| Pattern | When Defined | Scope | Use Case |
|---------|--------------|-------|----------|
| User-Defined Methods | Compile-time | All tests using the stub | Consistent default behavior |
| Callbacks | Runtime | Per-test | Test-specific overrides |

## Pattern 1: User-Defined Methods

Define protected methods in your stub class that match interface method signatures. The generator detects these at compile time and calls them as the default behavior.

### Basic Example

```csharp
public interface IUserService
{
    User GetUser(int id);
    int CalculateScore(string name, int baseScore);
}

[KnockOff]
public partial class UserServiceKnockOff : IUserService
{
    // Generator detects this and calls it for IUserService.GetUser
    protected User GetUser(int id) => new User { Id = id, Name = "Default User" };

    // Multi-parameter methods work the same way
    protected int CalculateScore(string name, int baseScore) => baseScore * 2;
}
```

### Rules for User Methods

1. **Must be `protected`** — The generator looks for protected methods
2. **Must match signature** — Same name, parameter types, and return type as the interface method
3. **Only for methods** — Properties and indexers use backing fields, not user methods

### Async Methods

User methods work with async return types:

```csharp
public interface IRepository
{
    Task<User?> GetByIdAsync(int id);
    ValueTask<int> CountAsync();
}

[KnockOff]
public partial class RepositoryKnockOff : IRepository
{
    protected Task<User?> GetByIdAsync(int id) =>
        Task.FromResult<User?>(new User { Id = id });

    protected ValueTask<int> CountAsync() =>
        new ValueTask<int>(42);
}
```

### When to Use User Methods

- **Consistent behavior** — Same return value across all tests
- **Shared test fixtures** — Stub class used by multiple test classes
- **Simple defaults** — Static or deterministic returns
- **No test-specific logic** — Doesn't need access to test state

## Pattern 2: Callbacks

Set delegates on the interface spy handlers at runtime. Callbacks provide per-test customization and take precedence over user methods.

### Method Callbacks (`OnCall`)

```csharp
var knockOff = new UserServiceKnockOff();

// Void method
knockOff.IUserService.DoSomething.OnCall = (ko) =>
{
    // Custom logic for this test
};

// Method with return value
knockOff.IUserService.GetUser.OnCall = (ko, id) => new User { Id = id, Name = "Mocked" };

// Method with multiple parameters (individual params)
knockOff.IUserService.Calculate.OnCall = (ko, name, value, flag) =>
{
    return flag ? value * 2 : value;
};
```

### Property Callbacks (`OnGet` / `OnSet`)

```csharp
// Getter callback
knockOff.IUserService.Name.OnGet = (ko) => "Dynamic Value";

// Setter callback
knockOff.IUserService.Name.OnSet = (ko, value) =>
{
    // Custom logic when property is set
    // Note: When OnSet is set, value does NOT go to backing field
};
```

### Indexer Callbacks

```csharp
// Getter with key parameter
knockOff.IPropertyStore.StringIndexer.OnGet = (ko, key) => key switch
{
    "Name" => new PropertyInfo { Value = "Test" },
    "Age" => new PropertyInfo { Value = "25" },
    _ => null
};

// Setter with key and value parameters
knockOff.IPropertyStore.StringIndexer.OnSet = (ko, key, value) =>
{
    // Custom logic
    // Note: When OnSet is set, value does NOT go to backing dictionary
};
```

### Callback Signatures

Each method gets a generated delegate type. The callback signature varies by member type:

| Member Type | Callback | Signature |
|-------------|----------|-----------|
| Void method (no params) | `OnCall =` | `Action<TKnockOff>` |
| Void method (params) | `OnCall =` | `Action<TKnockOff, TArg1, TArg2, ...>` |
| Return method (no params) | `OnCall =` | `Func<TKnockOff, TReturn>` |
| Return method (params) | `OnCall =` | `Func<TKnockOff, TArg1, ..., TReturn>` |
| Property getter | `OnGet =` | `Func<TKnockOff, TProperty>` |
| Property setter | `OnSet =` | `Action<TKnockOff, TProperty>` |
| Indexer getter | `OnGet =` | `Func<TKnockOff, TKey, TValue>` |
| Indexer setter | `OnSet =` | `Action<TKnockOff, TKey, TValue>` |

### Accessing KnockOff Instance

All callbacks receive the KnockOff instance (`ko`) as the first parameter. This allows:

```csharp
knockOff.IUserService.GetUser.OnCall = (ko, id) =>
{
    // Access other handlers
    if (ko.IUserService.IsInitialized.WasCalled)
        return new User { Id = id, Name = "Initialized" };

    // Access backing fields
    return new User { Id = id, Name = ko.IUserService_NameBacking };
};
```

### When to Use Callbacks

- **Per-test behavior** — Different return values for different tests
- **Dynamic returns** — Return value depends on arguments
- **Side effects** — Need to capture or validate during the call
- **Access to Spy state** — Check if other methods were called
- **Override user method** — Temporarily change behavior for one test

## Priority Order

When an interface member is invoked, KnockOff checks in this order:

```
┌─────────────────────────────────────────────────────────┐
│  1. CALLBACK (if set)                                   │
│     • OnCall for methods                                │
│     • OnGet/OnSet for properties                        │
│     • OnGet/OnSet for indexers                          │
│     → If callback exists, use it and stop               │
├─────────────────────────────────────────────────────────┤
│  2. USER METHOD (if defined, methods only)              │
│     • Protected method matching interface signature     │
│     → If user method exists, call it and stop           │
├─────────────────────────────────────────────────────────┤
│  3. DEFAULT BEHAVIOR                                    │
│     • Properties: return backing field value            │
│     • Methods: return default(T)                        │
│     • Indexers: check backing dictionary, then default  │
└─────────────────────────────────────────────────────────┘
```

### Example: Priority in Action

```csharp
[KnockOff]
public partial class ServiceKnockOff : IService
{
    // User method returns input * 2
    protected int Calculate(int input) => input * 2;
}

// Test
var knockOff = new ServiceKnockOff();
IService service = knockOff;

// No callback set → uses user method
var result1 = service.Calculate(5);  // Returns 10 (5 * 2)

// Set callback → overrides user method
knockOff.IService.Calculate.OnCall = (ko, input) => input * 100;
var result2 = service.Calculate(5);  // Returns 500 (callback)

// Reset clears callback → back to user method
knockOff.IService.Calculate.Reset();
var result3 = service.Calculate(5);  // Returns 10 (user method)
```

## Reset Behavior

The `Reset()` method clears:
- Call tracking (`CallCount`, `AllCalls`, etc.)
- Callbacks (`OnCall`, `OnGet`, `OnSet`)

It does **NOT** clear:
- Backing fields for properties
- Backing dictionaries for indexers

```csharp
// Set up state
knockOff.IUserService.GetUser.OnCall = (ko, id) => new User { Name = "Callback" };
service.GetUser(1);
service.GetUser(2);

Assert.Equal(2, knockOff.IUserService.GetUser.CallCount);

// Reset
knockOff.IUserService.GetUser.Reset();

Assert.Equal(0, knockOff.IUserService.GetUser.CallCount);  // Tracking cleared
Assert.Null(knockOff.IUserService.GetUser.OnCall);  // Callback cleared

// Now uses user method (or default if no user method)
var user = service.GetUser(3);
```

## Combining Both Patterns

The patterns work together for layered customization:

```csharp
[KnockOff]
public partial class RepositoryKnockOff : IRepository
{
    // Default: return null (not found)
    protected User? GetById(int id) => null;
}

// Test 1: Uses default (null)
var knockOff = new RepositoryKnockOff();
Assert.Null(knockOff.AsRepository().GetById(999));

// Test 2: Override for specific IDs
knockOff.IRepository.GetById.OnCall = (ko, id) => id switch
{
    1 => new User { Id = 1, Name = "Admin" },
    2 => new User { Id = 2, Name = "Guest" },
    _ => null  // Fall through to "not found"
};

Assert.Equal("Admin", knockOff.AsRepository().GetById(1)?.Name);
Assert.Null(knockOff.AsRepository().GetById(999));  // Still null

// Test 3: Reset and use different callback
knockOff.IRepository.GetById.Reset();
knockOff.IRepository.GetById.OnCall = (ko, id) =>
    new User { Id = id, Name = $"User-{id}" };

Assert.Equal("User-999", knockOff.AsRepository().GetById(999)?.Name);
```

## Decision Guide

| Question | Answer → Pattern |
|----------|------------------|
| Same behavior in all tests? | User method |
| Different per test? | Callback |
| Need to capture call info? | Callback (but tracking always available) |
| Shared across test classes? | User method |
| Complex conditional logic? | Callback |
| Just return a static value? | Either (user method is simpler) |
| Need to verify side effects? | Callback with assertions |

## Summary

- **User methods** = compile-time defaults baked into the stub class
- **Callbacks** = runtime overrides set per-test
- **Callbacks take precedence** over user methods
- **Reset()** clears callbacks and tracking, returning to user method behavior
- Use both patterns together for flexible, maintainable test stubs
