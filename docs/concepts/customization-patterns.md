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

<!-- snippet: customization-patterns-user-method-basic -->
```cs
public interface IPatternUserService
{
    PatternUser GetUser(int id);
    int CalculateScore(string name, int baseScore);
}

[KnockOff]
public partial class PatternUserServiceKnockOff : IPatternUserService
{
    // Generator detects this and calls it for IPatternUserService.GetUser
    protected PatternUser GetUser(int id) => new PatternUser { Id = id, Name = "Default User" };

    // Multi-parameter methods work the same way
    protected int CalculateScore(string name, int baseScore) => baseScore * 2;
}
```
<!-- endSnippet -->

### Rules for User Methods

1. **Must be `protected`** — The generator looks for protected methods
2. **Must match signature** — Same name, parameter types, and return type as the interface method
3. **Only for methods** — Properties and indexers use backing fields, not user methods

### Async Methods

User methods work with async return types:

<!-- snippet: customization-patterns-user-method-async -->
```cs
public interface IPatternRepository
{
    Task<PatternUser?> GetByIdAsync(int id);
    ValueTask<int> CountAsync();
}

[KnockOff]
public partial class PatternRepositoryKnockOff : IPatternRepository
{
    protected Task<PatternUser?> GetByIdAsync(int id) =>
        Task.FromResult<PatternUser?>(new PatternUser { Id = id });

    protected ValueTask<int> CountAsync() =>
        new ValueTask<int>(42);
}
```
<!-- endSnippet -->

### When to Use User Methods

- **Consistent behavior** — Same return value across all tests
- **Shared test fixtures** — Stub class used by multiple test classes
- **Simple defaults** — Static or deterministic returns
- **No test-specific logic** — Doesn't need access to test state

## Pattern 2: Callbacks

Set delegates on the interface handlers at runtime. Callbacks provide per-test customization and take precedence over user methods.

### Method Callbacks (`OnCall`)

<!-- snippet: customization-patterns-callback-method -->
```cs
// Void method
knockOff.DoSomething.OnCall = (ko) =>
{
    // Custom logic for this test
};

// Method with return value
knockOff.GetUser.OnCall = (ko, id) => new PatternUser { Id = id, Name = "Mocked" };

// Method with multiple parameters (individual params)
knockOff.Calculate.OnCall = (ko, name, value, flag) =>
{
    return flag ? value * 2 : value;
};
```
<!-- endSnippet -->

### Property Callbacks (`OnGet` / `OnSet`)

<!-- snippet: customization-patterns-callback-property -->
```cs
// Getter callback
knockOff.Name.OnGet = (ko) => "Dynamic Value";

// Setter callback
knockOff.Name.OnSet = (ko, value) =>
{
    // Custom logic when property is set
    // Note: When OnSet is set, value does NOT go to backing field
};
```
<!-- endSnippet -->

### Indexer Callbacks

<!-- snippet: customization-patterns-callback-indexer -->
```cs
// Getter with key parameter
knockOff.Indexer.OnGet = (ko, key) => key switch
{
    "Name" => new PatternPropertyInfo { Value = "Test" },
    "Age" => new PatternPropertyInfo { Value = "25" },
    _ => null
};

// Setter with key and value parameters
knockOff.Indexer.OnSet = (ko, key, value) =>
{
    // Custom logic
    // Note: When OnSet is set, value does NOT go to backing dictionary
};
```
<!-- endSnippet -->

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

<!-- pseudo:callback-ko-access -->
```csharp
knockOff.GetUser.OnCall = (ko, id) =>
{
    // Access other interceptors
    if (ko.IsInitialized.WasCalled)
        return new User { Id = id, Name = "Initialized" };

    // Access backing fields
    return new User { Id = id, Name = ko.NameBacking };
};
```
<!-- /snippet -->

### When to Use Callbacks

- **Per-test behavior** — Different return values for different tests
- **Dynamic returns** — Return value depends on arguments
- **Side effects** — Need to capture or validate during the call
- **Access to handler state** — Check if other methods were called
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

<!-- snippet: customization-patterns-priority-example -->
```cs
public interface IPatternService
{
    int Calculate(int input);
}

[KnockOff]
public partial class PatternServiceKnockOff : IPatternService
{
    // User method returns input * 2
    protected int Calculate(int input) => input * 2;
}
```
<!-- endSnippet -->

<!-- pseudo:priority-example-usage -->
```csharp
// Test
var knockOff = new PatternServiceKnockOff();
IPatternService service = knockOff;

// No callback set → uses user method
var result1 = service.Calculate(5);  // Returns 10 (5 * 2)

// Set callback → overrides user method
knockOff.Calculate.OnCall = (ko, input) => input * 100;
var result2 = service.Calculate(5);  // Returns 500 (callback)

// Reset clears callback → back to user method
knockOff.Calculate.Reset();
var result3 = service.Calculate(5);  // Returns 10 (user method)
```
<!-- /snippet -->

## Reset Behavior

The `Reset()` method clears:
- Call tracking (`CallCount`, `LastCallArg`/`LastCallArgs`)
- Callbacks (`OnCall`, `OnGet`, `OnSet`)

It does **NOT** clear:
- Backing fields for properties
- Backing dictionaries for indexers

<!-- pseudo:reset-behavior -->
```csharp
// Set up state
knockOff.GetUser.OnCall = (ko, id) => new User { Name = "Callback" };
service.GetUser(1);
service.GetUser(2);

Assert.Equal(2, knockOff.GetUser.CallCount);

// Reset
knockOff.GetUser.Reset();

Assert.Equal(0, knockOff.GetUser.CallCount);  // Tracking cleared
Assert.Null(knockOff.GetUser.OnCall);  // Callback cleared

// Now uses user method (or default if no user method)
var user = service.GetUser(3);
```
<!-- /snippet -->

## Combining Both Patterns

The patterns work together for layered customization:

<!-- snippet: customization-patterns-combining-patterns -->
```cs
public interface IPatternCombinedRepository
{
    PatternUser? GetById(int id);
}

[KnockOff]
public partial class PatternCombinedRepositoryKnockOff : IPatternCombinedRepository
{
    // Default: return null (not found)
    protected PatternUser? GetById(int id) => null;
}
```
<!-- endSnippet -->

<!-- pseudo:combining-patterns-usage -->
```csharp
// Test 1: Uses default (null)
var knockOff = new PatternCombinedRepositoryKnockOff();
Assert.Null(knockOff.AsIPatternCombinedRepository().GetById(999));

// Test 2: Override for specific IDs
knockOff.GetById.OnCall = (ko, id) => id switch
{
    1 => new PatternUser { Id = 1, Name = "Admin" },
    2 => new PatternUser { Id = 2, Name = "Guest" },
    _ => null  // Fall through to "not found"
};

Assert.Equal("Admin", knockOff.AsIPatternCombinedRepository().GetById(1)?.Name);
Assert.Null(knockOff.AsIPatternCombinedRepository().GetById(999));  // Still null

// Test 3: Reset and use different callback
knockOff.GetById.Reset();
knockOff.GetById.OnCall = (ko, id) =>
    new PatternUser { Id = id, Name = $"User-{id}" };

Assert.Equal("User-999", knockOff.AsIPatternCombinedRepository().GetById(999)?.Name);
```
<!-- /snippet -->

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
