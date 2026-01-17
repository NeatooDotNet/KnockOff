---
skill: knockoff
topic: creating-stubs
audience: beginner
---

# Creating KnockOff Stubs

This guide covers all stub patterns, user methods, and the duality pattern.

## Stub Patterns Overview

| Pattern | Syntax | Generated | When to Use |
|---------|--------|-----------|-------------|
| **Standalone** | `[KnockOff]` on class | Stub class with interceptors | Shared across tests, needs defaults |
| **Inline Interface** | `[KnockOff<IService>]` | Nested `Stubs.IService` | Test-local, one-off |
| **Inline Class** | `[KnockOff<MyClass>]` | Nested `Stubs.MyClass` | Stub virtual/abstract members |
| **Delegate** | `[KnockOff<Func<T>>]` | Nested `Stubs.FuncOfT` | Stub Func/Action |

## Standalone Stubs

Use for stubs shared across multiple test files or when you need user method defaults.

### Basic Syntax

<!-- snippet: skill-SKILL-quick-start-stub -->
```cs
[KnockOff]
public partial class SkDataServiceKnockOff : ISkDataService
{
    private readonly int _count;

    public SkDataServiceKnockOff(int count = 42) => _count = count;

    // Define behavior for non-nullable method
    protected int GetCount() => _count;

    // GetDescription not defined - returns null by default
}
```
<!-- endSnippet -->

**Key points:**
- Must be `partial`
- Implement exactly one interface
- Can have constructors, fields, properties
- Can define protected user methods

### With User Methods

<!-- snippet: skill-SKILL-customization-user-method -->
```cs
[KnockOff]
public partial class SkRepoKnockOff : ISkRepoService
{
    protected SkUser? GetById(int id) => new SkUser { Id = id };
    protected Task<SkUser?> GetByIdAsync(int id) => Task.FromResult<SkUser?>(new SkUser { Id = id });
}
```
<!-- endSnippet -->

**User method rules:**
- Must be `protected`
- Match interface method signature exactly (name, params, return type)
- Provide compile-time defaults for all tests

### Usage

```csharp
var knockOff = new SkDataServiceKnockOff(count: 100);
ISkDataService service = knockOff;  // Implicit cast

// Access interface members
var result = service.GetCount();  // Returns 100

// Access interceptors for verification/customization
Assert.Equal(1, knockOff.GetCount2.CallCount);
```

### When to Use Standalone

- ✅ Stub used across multiple test files
- ✅ Need default behavior (user methods)
- ✅ Want named stub class (better IntelliSense)
- ✅ Complex setup logic in constructor

## Inline Interface Stubs

Use for test-local stubs that don't need shared defaults.

### Basic Syntax

<!-- snippet: skill-SKILL-inline-stub-pattern -->
```cs
[KnockOff<ISkInlineUserService>]
[KnockOff<ISkInlineLogger>]
public partial class SkInlineUserServiceTests
{
    // Generates: Stubs.ISkInlineUserService, Stubs.ISkInlineLogger
}
```
<!-- endSnippet -->

**Generated code:**
```csharp
public partial class SkInlineUserServiceTests
{
    public static class Stubs
    {
        public partial class ISkInlineUserService : global::ISkInlineUserService { }
        public partial class ISkInlineLogger : global::ISkInlineLogger { }
    }
}
```

### Usage

```csharp
[Fact]
public void Test()
{
    var userService = new Stubs.ISkInlineUserService();
    var logger = new Stubs.ISkInlineLogger();

    userService.GetUser.OnCall = (ko, id) => new SkUser { Id = id };
    logger.Log.OnCall = (ko, message) => { };

    // Use as interface
    ISkInlineUserService service = userService;
    service.GetUser(42);
}
```

### Partial Properties (C# 13+)

<!-- snippet: skill-SKILL-partial-properties -->
```cs
[KnockOff<ISkInlineUserService>]
public partial class SkPartialPropertyTests
{
    public partial Stubs.ISkInlineUserService UserStub { get; }  // Auto-instantiated
}
```
<!-- endSnippet -->

**Benefit:** Stub is auto-instantiated, no `new` needed in tests.

### When to Use Inline Interface

- ✅ Stub used in single test class
- ✅ Don't need default behavior (one-off setups)
- ✅ Want less boilerplate

## Inline Class Stubs

Use for stubbing unsealed classes with virtual/abstract members.

### Basic Syntax

<!-- snippet: skill-SKILL-class-stubs-class -->
```cs
public class SkEmailService
{
    public virtual void Send(string to, string subject, string body)
        => Console.WriteLine($"Sending to {to}");

    public virtual string ServerName { get; set; } = "default";
}
```
<!-- endSnippet -->

<!-- snippet: skill-SKILL-class-stubs -->
```cs
[KnockOff<SkEmailService>]
public partial class SkEmailServiceTests
{
    // Generates: Stubs.SkEmailService
}
```
<!-- endSnippet -->

### Usage - IMPORTANT: Use `.Object`

```csharp
[Fact]
public void Test()
{
    var stub = new Stubs.SkEmailService();

    stub.Send.OnCall = (ko, to, subject, body) => { };
    stub.ServerName.Value = "test.smtp.com";

    // CRITICAL: Use .Object to get the class instance
    SkEmailService service = stub.Object;

    service.Send("test@example.com", "Subject", "Body");
    Assert.Equal(1, stub.Send.CallCount);
}
```

**Why `.Object`?**
- `stub` is the wrapper with interceptors
- `stub.Object` is the actual class instance

### Constructor Parameters

<!-- snippet: skill-SKILL-class-constructor -->
```cs
[KnockOff<SkRepository>]
public partial class SkConstructorTests
{
    // Generates: Stubs.SkRepository
}
```
<!-- endSnippet -->

**Usage:**
```csharp
var stub = new Stubs.SkRepository("connection-string");
stub.Object.ConnectionString;  // "connection-string"
```

### Abstract Classes

<!-- snippet: skill-SKILL-abstract-classes -->
```cs
[KnockOff<SkBaseRepository>]
public partial class SkAbstractTests
{
    // Generates: Stubs.SkBaseRepository
}
```
<!-- endSnippet -->

Abstract members are stubbed like virtual members.

### Non-Virtual Members

<!-- snippet: skill-SKILL-non-virtual-members -->
```cs
[KnockOff<SkNonVirtualService>]
public partial class SkNonVirtualTests
{
    // Generates: Stubs.SkNonVirtualService
}
```
<!-- endSnippet -->

**Important:** Only virtual/abstract members get interceptors. Non-virtual members call base implementation.

```csharp
var stub = new Stubs.SkNonVirtualService();

// Virtual - stubbed
stub.VirtualProperty.Value = "Stubbed";

// Non-virtual - calls base
stub.Object.NonVirtualProperty;  // Returns "Original"
```

### When to Use Class Stubs

- ✅ Need to stub unsealed class
- ✅ Class has virtual/abstract members
- ⚠️ Remember to use `.Object`!

## Delegate Stubs

Use for stubbing Func<>/Action<> delegates.

### Basic Syntax

<!-- snippet: skill-SKILL-delegate-stubs -->
```cs
[KnockOff<SkIsUniqueRule>]
[KnockOff<SkUserFactory>]
public partial class SkValidationTests
{
    // Generates: Stubs.SkIsUniqueRule, Stubs.SkUserFactory
}
```
<!-- endSnippet -->

### Usage

```csharp
[Fact]
public void Test()
{
    var rule = new Stubs.SkIsUniqueRule();
    rule.Invoke.OnCall = (ko, value) => value != "taken";

    SkIsUniqueRule ruleDelegate = rule;
    bool isUnique = ruleDelegate("newValue");  // true

    Assert.Equal(1, rule.Invoke.CallCount);
}
```

**Common delegate types:**
- `Func<T>` → `Stubs.FuncOfT`
- `Func<T1, TResult>` → `Stubs.FuncOfT1AndTResult`
- `Action<T>` → `Stubs.ActionOfT`

## The Duality Pattern

KnockOff supports **two ways** to configure behavior:

### 1. User Methods (Compile-Time Defaults)

Define in the stub class - used by default:

<!-- snippet: skill-SKILL-duality-pattern -->
```cs
// Pattern 1: User method (compile-time default)
[KnockOff]
public partial class SkServiceKnockOff : ISkService
{
    protected int GetValue(int id) => id * 2;  // Default for all tests
}
```
<!-- endSnippet -->

### 2. Callbacks (Runtime Overrides)

Set per-test - overrides user method:

```csharp
var knockOff = new SkServiceKnockOff();

// No callback → uses user method
service.GetValue(5);  // Returns 10

// Set callback → overrides user method
knockOff.GetValue2.OnCall((ko, id) => id * 100);
service.GetValue(5);  // Returns 500

// Reset → back to user method
knockOff.GetValue2.Reset();
service.GetValue(5);  // Returns 10
```

### Priority Order

<!-- snippet: skill-SKILL-priority-order -->
```cs
var knockOff = new SkPriorityServiceKnockOff();
ISkPriorityService service = knockOff;
// No callback -> uses user method: service.Calculate(5) returns 10
// Callback -> overrides: knockOff.Calculate2.OnCall((ko, x) => x * 100);
// Reset -> back to user method: knockOff.Calculate2.Reset();
```
<!-- endSnippet -->

**Priority:**
1. **OnCall callback** (if set) - highest priority
2. **User method** (if defined) - fallback
3. **Smart default** - last resort

### When to Use Each

**User methods:**
- ✅ Shared behavior across all tests
- ✅ Realistic test data
- ✅ Reduce test setup boilerplate

**Callbacks:**
- ✅ Test-specific behavior
- ✅ Edge cases
- ✅ Override defaults

## Smart Defaults

When no callback or user method is set, KnockOff returns sensible defaults:

<!-- snippet: skill-SKILL-smart-defaults -->
```cs
[KnockOff]
public partial class SkSmartDefaultKnockOff : ISkSmartDefaultService { }
```
<!-- endSnippet -->

| Return Type | Default Value | Example |
|-------------|---------------|---------|
| `int`, `bool`, `decimal` | `default(T)` | `0`, `false`, `0m` |
| `string?` | `null` | `null` |
| `List<T>` | `new List<T>()` | Empty list |
| `IList<T>`, `IEnumerable<T>` | `new List<T>()` | Empty list |
| `Dictionary<K,V>` | `new Dictionary<K,V>()` | Empty dict |
| `IDictionary<K,V>` | `new Dictionary<K,V>()` | Empty dict |
| `ISet<T>` | `new HashSet<T>()` | Empty set |
| Types with `new()` | `new T()` | New instance |
| Non-nullable without ctor | Throws | `InvalidOperationException` |

**Example:**
```csharp
var knockOff = new SkSmartDefaultKnockOff();
ISkSmartDefaultService service = knockOff;

service.GetCount();       // Returns 0
service.GetItems();       // Returns new List<string>()
service.GetOptional();    // Returns null
service.GetDisposable();  // Throws InvalidOperationException
```

## Backing Storage

### Properties

<!-- snippet: skill-SKILL-backing-properties -->
```cs
[KnockOff]
public partial class SkBackingServiceKnockOff : ISkBackingService { }
```
<!-- endSnippet -->

Properties have automatic backing fields:

```csharp
knockOff.Name.Value = "Test";
Assert.Equal("Test", service.Name);

// Set via interface
service.Name = "Updated";
Assert.Equal("Updated", knockOff.Name.Value);
```

### Indexers

<!-- snippet: skill-SKILL-backing-indexers -->
```cs
[KnockOff]
public partial class SkBackingPropertyStoreKnockOff : ISkBackingPropertyStore { }
```
<!-- endSnippet -->

Indexers have a `Backing` dictionary:

```csharp
knockOff.Indexer.Backing["key1"] = "value1";
knockOff.Indexer.Backing["key2"] = "value2";

Assert.Equal("value1", store["key1"]);
```

**Reset behavior:**
- `Reset()` clears tracking counts
- `Reset()` does NOT clear `Value` (properties) or `Backing` (indexers)

## Stub Minimalism

<!-- snippet: skill-SKILL-stub-minimalism -->
```cs
// GOOD - minimal stub, most methods just work with smart defaults
[KnockOff]
public partial class SkMinimalServiceKnockOff : ISkMinimalService
{
    // Only define methods needing custom behavior
    protected SkUser? GetUser(int id) => new SkUser { Id = id };
    // GetCount returns 0, GetUsers() returns new List<SkUser>(), etc.
}
```
<!-- endSnippet -->

**Principle:** Only configure what needs custom behavior. Let smart defaults handle the rest.

## Nested Stubs

Stubs can be nested in test classes:

<!-- snippet: skill-SKILL-pattern-nested -->
```cs
public partial class SkUserServiceTests  // Must be partial!
{
    [KnockOff]
    public partial class SkRepoNestedKnockOff : ISkRepository { }
}
```
<!-- endSnippet -->

**Requirements:**
- Parent class must be `partial`
- Stub class must be `partial`

## Summary

**Quick decision guide:**

| Scenario | Pattern |
|----------|---------|
| Shared across tests | Standalone |
| Test-local, one-off | Inline interface |
| Stub a class | Inline class (use `.Object`) |
| Stub Func/Action | Inline delegate |
| Need defaults | User methods (standalone) |
| Per-test config | Callbacks (any pattern) |

**Next:**
- [interceptor-api.md](interceptor-api.md) - Complete API reference
- [troubleshooting.md](troubleshooting.md) - Common issues
- [advanced.md](advanced.md) - Generics, overloads, out/ref
