---
name: knockoff
description: KnockOff source-generated test stubs. Use when creating interface stubs for unit tests, migrating from Moq, understanding the duality pattern (user methods vs callbacks), configuring stub behavior, verifying method calls/property access, working with callbacks for tracking and customization, or enabling strict mode.
allowed-tools: Read, Write, Edit, Glob, Grep, Bash
---

# KnockOff - Source-Generated Test Stubs

KnockOff is a Roslyn Source Generator that creates compile-time test stubs for interfaces and classes. Unlike Moq's runtime proxies, KnockOff generates debuggable C# code with strongly-typed interceptors.

**Version:** 10.21.0+

## When to Use This Skill

Use KnockOff when:
- Creating test stubs for unit tests
- You need compile-time safety over runtime flexibility
- The project has a KnockOff package reference
- Migrating from Moq (see [moq-migration.md](moq-migration.md))

## Quick Start

### 1. Create a Stub

**Standalone stub** (reusable across test files):

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

**Inline stub** (scoped to test class):

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

### 2. Configure Behavior

<!-- snippet: skill-SKILL-quick-start-usage -->
```cs
var knockOff = new SkDataServiceKnockOff(count: 100);
ISkDataService service = knockOff;

// Property - uses generated backing field
service.Name = "Test";
Assert.Equal("Test", service.Name);
Assert.Equal(1, knockOff.Name.SetCount);

// Nullable method - configure and track with OnCall
var tracking = knockOff.GetDescription.OnCall((ko, id) => null);
var description = service.GetDescription(5);
Assert.Null(description);
Assert.True(tracking.WasCalled);
Assert.Equal(5, tracking.LastArg);

// Non-nullable method - returns constructor value
Assert.Equal(100, service.GetCount());
```
<!-- endSnippet -->

### 3. Verify Calls

Tracking is automatic:

```csharp
Assert.True(knockOff.Method.WasCalled);
Assert.Equal(1, knockOff.Method.CallCount);
Assert.Equal(42, knockOff.Method.LastArg);
```

## Stub Patterns

| Pattern | Attribute | Use Case | Access |
|---------|-----------|----------|--------|
| **Standalone** | `[KnockOff]` on class | Reusable, with user methods | Direct cast to interface |
| **Inline Interface** | `[KnockOff<IService>]` on test class | Test-scoped | `new Stubs.IService()` |
| **Inline Class** | `[KnockOff<MyClass>]` on test class | Stub virtual members | `.Object` property |
| **Delegate** | `[KnockOff<Func<T>>]` on test class | Stub delegates | `new Stubs.FuncOfInt32()` |

**When to use each:**
- **Standalone:** Shared across tests, needs default behavior (user methods)
- **Inline:** Test-specific, one-off stubs
- **Class:** Stubbing unsealed classes with virtual methods
- **Delegate:** Stubbing Func<>/Action<> delegates

See [creating-stubs.md](creating-stubs.md) for detailed patterns.

## Core API Cheat Sheet

### Method Interceptors

```csharp
// Configure
var tracking = knockOff.Method.OnCall((ko, arg) => result);

// Verify
knockOff.Method.CallCount        // int: number of calls
knockOff.Method.WasCalled        // bool: CallCount > 0
knockOff.Method.LastArg          // T: last argument (single param)
knockOff.Method.LastCallArgs     // Named tuple (multi param)
knockOff.Method.Reset()          // Clear all
```

### Property Interceptors

```csharp
// Configure
knockOff.Property.Value = staticValue;           // Static value
knockOff.Property.OnGet = (ko) => dynamicValue;  // Dynamic getter
knockOff.Property.OnSet = (ko, v) => { };        // Setter callback

// Verify
knockOff.Property.GetCount       // int: getter calls
knockOff.Property.SetCount       // int: setter calls
knockOff.Property.LastSetValue   // T: last set value
```

### Indexer Interceptors

```csharp
// Configure
knockOff.Indexer.Backing[key] = value;          // Pre-populate
knockOff.Indexer.OnGet = (ko, key) => value;    // Dynamic
knockOff.Indexer.OnSet = (ko, k, v) => { };     // Callback

// Verify
knockOff.Indexer.GetCount / SetCount
knockOff.Indexer.LastGetKey / LastSetEntry
```

### Event Interceptors

```csharp
// Raise
knockOff.Event.Raise(sender, args);

// Verify
knockOff.Event.AddCount          // int: subscription count
knockOff.Event.RemoveCount       // int: unsubscription count
knockOff.Event.HasSubscribers    // bool: any subscribers
```

See [interceptor-api.md](interceptor-api.md) for complete reference.

> **⚠️ OnCall Syntax Depends on Stub Type**
>
> **Standalone stubs** (v10.21.0+):
> ```csharp
> var tracking = stub.Method.OnCall((ko, arg) => result);  // Method - returns IMethodTracking
> ```
>
> **Inline stubs**:
> ```csharp
> stub.Method.OnCall = (ko, arg) => result;  // Property assignment
> ```
>
> The examples in this file use standalone stub syntax. See [creating-stubs.md](creating-stubs.md) for inline patterns.

## User Methods (Duality Pattern)

Define **compile-time defaults** in the stub class:

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

Override **per-test** with callbacks:

```csharp
var knockOff = new SkServiceKnockOff();

// Uses user method by default
service.GetValue(5);  // Returns 10

// Override for this test
knockOff.GetValue2.OnCall((ko, id) => id * 100);
service.GetValue(5);  // Returns 500
```

**Priority:** `OnCall` callback > User method > Smart default

See [creating-stubs.md](creating-stubs.md) for user method patterns.

## Common Patterns

### Conditional Returns

```csharp
knockOff.GetUser.OnCall((ko, id) => id switch
{
    1 => new User { Name = "Admin" },
    2 => new User { Name = "Guest" },
    _ => null
});
```

### Throwing Exceptions

```csharp
knockOff.Connect.OnCall((ko) => throw new TimeoutException());
```

### Sequential Returns

```csharp
var values = new Queue<int>([1, 2, 3]);
knockOff.GetNext.OnCall((ko) => values.Dequeue());
```

### Async Methods

```csharp
knockOff.GetByIdAsync.OnCall((ko, id) =>
    Task.FromResult<User?>(new User { Id = id }));
```

See [interceptor-api.md](interceptor-api.md) for more patterns.

## Overloaded Methods

Methods with overloads get numeric suffixes:

<!-- snippet: skill-SKILL-pattern-overloads -->
```cs
[KnockOff]
public partial class SkOverloadedServiceKnockOff : ISkOverloadedService { }
```
<!-- endSnippet -->

```csharp
public interface IProcessor
{
    void Process(string data);        // knockOff.Process1
    void Process(string data, int n); // knockOff.Process2
}

knockOff.Process1.OnCall((ko, data) => { });
knockOff.Process2.OnCall((ko, data, n) => { });
```

**Note:** Methods without overloads have **no suffix**.

See [advanced.md](advanced.md) for overload details.

## Common Mistakes

### 1. Forgetting `partial`

```csharp
// WRONG
[KnockOff]
public class UserStub : IUser { }

// CORRECT
[KnockOff]
public partial class UserStub : IUser { }
```

### 2. Wrong Callback Signature

```csharp
// WRONG - missing ko parameter
knockOff.GetUser.OnCall((id) => new User());

// CORRECT - first param is always the stub instance
knockOff.GetUser.OnCall((ko, id) => new User());
```

### 3. Forgetting `.Object` for Class Stubs

```csharp
[KnockOff<EmailService>]
public partial class Tests
{
    [Fact]
    public void Test()
    {
        var stub = new Stubs.EmailService();

        // WRONG
        var service = new MyService(stub);

        // CORRECT - use .Object for class instance
        var service = new MyService(stub.Object);
    }
}
```

### 4. Using Moq Syntax

```csharp
// WRONG - Moq syntax
knockOff.Setup(x => x.GetUser(It.IsAny<int>())).Returns(user);

// CORRECT - KnockOff syntax
knockOff.GetUser.OnCall((ko, id) => user);
```

See [troubleshooting.md](troubleshooting.md) for debugging.

## Smart Defaults

When no callback is set, methods return sensible defaults:

| Return Type | Default |
|-------------|---------|
| Value types (`int`, `bool`) | `default(T)` (0, false) |
| Nullable refs (`string?`) | `null` |
| `List<T>`, `IList<T>`, `IEnumerable<T>` | `new List<T>()` |
| `Dictionary<K,V>`, `IDictionary<K,V>` | `new Dictionary<K,V>()` |
| Types with `new()` | `new T()` |
| Non-nullable without ctor | Throws `InvalidOperationException` |

This means many stubs "just work" without configuration.

## Additional Files

- **[creating-stubs.md](creating-stubs.md)** - All stub patterns, user methods, duality pattern
- **[interceptor-api.md](interceptor-api.md)** - Complete interceptor API reference
- **[troubleshooting.md](troubleshooting.md)** - Common issues, diagnostics, debugging
- **[strict-mode.md](strict-mode.md)** - Strict mode patterns
- **[moq-migration.md](moq-migration.md)** - Comprehensive Moq → KnockOff migration guide
- **[advanced.md](advanced.md)** - Generics, overloads, out/ref parameters, delegates

## Next Steps

1. **Creating your first stub?** → Read [creating-stubs.md](creating-stubs.md)
2. **Customizing behavior?** → See user methods and callbacks in [creating-stubs.md](creating-stubs.md)
3. **Debugging issues?** → Check [troubleshooting.md](troubleshooting.md)
4. **Migrating from Moq?** → Follow [moq-migration.md](moq-migration.md)
5. **Advanced scenarios?** → See [advanced.md](advanced.md)
