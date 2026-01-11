---
name: knockoff
description: KnockOff source-generated test stubs. Use when creating interface stubs for unit tests, migrating from Moq, understanding the duality pattern (user methods vs callbacks), configuring stub behavior, verifying invocations, or working with interceptors for tracking calls.
allowed-tools: Read, Write, Edit, Glob, Grep, Bash(dotnet:*)
---

# KnockOff - Source-Generated Test Stubs

## Overview

KnockOff is a Roslyn Source Generator that creates test stubs for interfaces. Unlike Moq's runtime proxy generation, KnockOff generates compile-time code for type-safe, debuggable stubs.

### Key Differentiator: The Duality

KnockOff provides **two complementary patterns** for customizing stub behavior:

| Pattern | When | Scope | Use Case |
|---------|------|-------|----------|
| **User Methods** | Compile-time | All tests | Consistent defaults |
| **Callbacks** | Runtime | Per-test | Test-specific overrides |

snippet: skill-SKILL-duality-pattern

**Priority Order**: Callback → User method → Default

## Source Generation Mindset

**KnockOff generates all interface members automatically.** You do NOT need to:
- Worry about interface complexity
- Count how many methods an interface has
- Manually implement anything
- Assess whether an interface is "too large" or "too complex"

### The Only Question

When considering KnockOff for any interface, ask only:

> "Can I add `[KnockOff<TInterface>]` and configure the 1-3 members my test actually calls?"

The answer is almost always **yes**.

### Complex Interfaces Are Fine

```csharp
// IEditBase has 50+ members? Doesn't matter.
[KnockOff<IEditBase>]
public partial class MyTests
{
    // Generator creates everything. You configure only what you need:
    // stub.IsValid.OnGet = (ko) => true;
    // stub.IsDirty.OnGet = (ko) => false;
    // That's it. The other 48 members just work with smart defaults.
}
```

### When KnockOff Genuinely Won't Work

Rare cases where KnockOff cannot be used:
- Sealed classes (can't inherit)
- Delegates with `ref`/`out` parameters
- Types requiring complex constructor logic that can't be stubbed

**If uncertain: TRY IT FIRST.** Add the attribute, build, see if it compiles. If it fails, THEN ask for clarification - don't abandon KnockOff preemptively.

### Anti-Pattern: Manual Test Doubles

**NEVER** create hand-written test doubles like this when KnockOff would work:

```csharp
// ❌ WRONG - Don't do this
public class FakeEditBase : IEditBase
{
    public bool IsValid => true;
    public bool IsDirty => false;
    // ... 48 more manual implementations
}
```

This defeats the purpose of having a source generator.

## Installation

```bash
dotnet add package KnockOff
```

## Quick Start

### 1. Create KnockOff Stub

snippet: skill-SKILL-quick-start-interface

snippet: skill-SKILL-quick-start-stub

### 2. Use in Tests

snippet: skill-SKILL-quick-start-usage

## Stub Patterns

KnockOff supports multiple stub patterns:

| Pattern | Attribute | Target | Use Case |
|---------|-----------|--------|----------|
| **Explicit** | `[KnockOff]` on class implementing interface | Interface | Reusable stubs, user methods |
| **Generic Standalone** | `[KnockOff]` on generic class implementing generic interface | Interface | Reusable generic stubs |
| **Inline Interface** | `[KnockOff<TInterface>]` on test class | Interface | Test-local interface stubs |
| **Inline Class** | `[KnockOff<TClass>]` on test class | Class | Test-local class stubs |
| **Inline Delegate** | `[KnockOff<TDelegate>]` on test class | Delegate | Test-local delegate stubs |

### Inline Stubs

Generate stubs inside test classes using `[KnockOff<TInterface>]`:

snippet: skill-SKILL-inline-stub-pattern

In test:

snippet: skill-SKILL-inline-stub-usage

#### Partial Properties (C# 13+)

snippet: skill-SKILL-partial-properties

### Generic Standalone Stubs

Create reusable generic stubs that work with any type argument:

```csharp
public interface IRepository<T> where T : class
{
    T? GetById(int id);
    void Save(T entity);
}

// Generic standalone stub - reusable with any type
[KnockOff]
public partial class RepositoryStub<T> : IRepository<T> where T : class { }
```

Use the same stub class with different type arguments:

```csharp
// Same stub class, different types
var userRepo = new RepositoryStub<User>();
var orderRepo = new RepositoryStub<Order>();

// Configure each independently
userRepo.GetById.OnCall = (ko, id) => new User { Id = id };
orderRepo.GetById.OnCall = (ko, id) => new Order { Id = id };

// Tracking works per instance
userRepo.Save.CallCount;  // tracks User saves
orderRepo.Save.CallCount; // tracks Order saves
```

**Type Parameter Arity:** The stub class must have the **same number of type parameters** as the interface:

```csharp
// Correct: matching arity
[KnockOff]
public partial class CacheStub<TKey, TValue> : ICache<TKey, TValue> { }

// Error KO0008: mismatched arity (2 vs 1)
[KnockOff]
public partial class BadStub<T, TExtra> : IRepository<T> { }
```

### Delegate Stubs

Stub named delegate types using `[KnockOff<TDelegate>]`:

snippet: skill-SKILL-delegate-stubs

In test:

snippet: skill-SKILL-delegate-stubs-usage

**Note:** Delegates with `ref`/`out` parameters cannot be stubbed (Func<>/Action<> limitation).

### Class Stubs

Stub virtual/abstract class members using `[KnockOff<TClass>]`:

snippet: skill-SKILL-class-stubs-class

snippet: skill-SKILL-class-stubs

In test:
```csharp
var stub = new SkEmailServiceTests.Stubs.SkEmailService();
stub.Send.OnCall = (ko, to, subject, body) => Console.WriteLine($"STUBBED: {to}");

// Use .Object to get the EmailService instance
SkEmailService service = stub.Object;
service.Send("test@example.com", "Hello", "World");

Assert.True(stub.Send.WasCalled);
Assert.Equal("test@example.com", stub.Send.LastCallArgs?.to);
```

#### Unified API

Class stubs use the **same API** as interface stubs for interceptors:

| Aspect | Interface Stubs | Class Stubs |
|--------|-----------------|-------------|
| Get typed instance | `stub` (direct) | `stub.Object` |
| Interceptor access | `stub.Member` | `stub.Member` (unified!) |
| Base behavior | N/A | Calls base class when no callback |

#### Constructor Parameters

snippet: skill-SKILL-class-constructor

```csharp
var stub = new SkConstructorTests.Stubs.SkRepository("Server=test");
Assert.Equal("Server=test", stub.Object.ConnectionString);
```

#### Abstract Classes

Abstract members return defaults unless configured:

snippet: skill-SKILL-abstract-classes

```csharp
var stub = new SkAbstractTests.Stubs.SkBaseRepository();
Assert.Null(stub.Object.ConnectionString);  // default(string)
stub.ConnectionString.OnGet = (ko) => "Server=test";
Assert.Equal("Server=test", stub.Object.ConnectionString);
```

#### Non-Virtual Members

Non-virtual members are NOT intercepted. Access through `.Object`:

snippet: skill-SKILL-non-virtual-members

```csharp
stub.Object.NonVirtualProperty = "Direct";
stub.Object.NonVirtualMethod();  // Calls base class directly
```

## Accessing Interceptors

Each interface member gets its own interceptor for tracking and configuration:

snippet: skill-SKILL-interface-access

### Multiple Interfaces

When implementing multiple interfaces, each has a separate property:

snippet: skill-SKILL-multiple-interfaces

### Accessing as Interface Type

snippet: skill-SKILL-interface-class-access

See [Moq Migration](moq-migration.md) for detailed interface access patterns.

## OnCall API

**Callbacks use property assignment** with `OnCall =`:

snippet: skill-SKILL-oncall-patterns

**Out/Ref parameters** - use explicit delegate type:

snippet: skill-SKILL-oncall-out-ref

## Smart Default Return Values

KnockOff returns sensible defaults for unconfigured methods instead of throwing:

| Return Type | Default Value | Example |
|-------------|---------------|---------|
| Value types | `default` | `int` → `0`, `bool` → `false` |
| Nullable refs | `null` | `string?` → `null` |
| Types with `new()` | `new T()` | `List<T>` → empty list |
| Collection interfaces | concrete type | `IList<T>` → `new List<T>()` |
| Other non-nullable | throws | `string`, `IDisposable` |

snippet: skill-SKILL-smart-defaults

**Collection Interface Mapping:**

| Interface | Concrete Type |
|-----------|---------------|
| `IEnumerable<T>`, `ICollection<T>`, `IList<T>` | `List<T>` |
| `IReadOnlyList<T>`, `IReadOnlyCollection<T>` | `List<T>` |
| `IDictionary<K,V>`, `IReadOnlyDictionary<K,V>` | `Dictionary<K,V>` |
| `ISet<T>` | `HashSet<T>` |

## Stub Minimalism

**Only stub what the test needs.** Don't implement every interface member.

snippet: skill-SKILL-stub-minimalism

## Interceptor Types

| Member Type | Tracking | Callbacks |
|-------------|----------|-----------|
| Method | `CallCount`, `WasCalled`, `LastCallArg`/`LastCallArgs` | `OnCall` |
| Property | `GetCount`, `SetCount`, `LastSetValue` | `OnGet`, `OnSet` |
| Indexer | `GetCount`, `SetCount`, `LastGetKey`, `LastSetEntry` | `OnGet`, `OnSet` |
| Event | `SubscribeCount`, `UnsubscribeCount`, `RaiseCount`, `WasRaised`, `LastRaiseArgs`, `AllRaises` | `Raise()`, `Reset()`, `Clear()` |

### Reset

```csharp
knockOff.GetUser.Reset();  // Clears tracking AND callbacks
// After reset: CallCount=0, OnCall=null
// Falls back to user method or default
```

## Customization Patterns

### User Methods (Compile-Time)

Define protected methods matching interface signatures:

snippet: skill-SKILL-customization-user-method

Rules:
- Must be `protected`
- Must match method signature exactly
- Only works for methods (not properties/indexers)

### Callbacks (Runtime)

#### Method Callbacks

snippet: skill-SKILL-customization-callbacks-method

#### Properties

**Use `Value` for static values (recommended):**

snippet: skill-SKILL-property-value-pattern

**Use `OnGet`/`OnSet` for dynamic behavior:**

snippet: skill-SKILL-customization-callbacks-property

**Decision guide:**
| Scenario | Use |
|----------|-----|
| Static test data | `Value` |
| Different value each call | `OnGet` |
| Depends on stub state | `OnGet` |
| Capture/validate on set | `OnSet` |

#### Indexer Callbacks

snippet: skill-SKILL-customization-callbacks-indexer

### Priority Order

```
1. Callback (if set) → takes precedence
2. User method (if defined) → fallback for methods
3. Smart default:
   - Properties: backing field (initialized via smart defaults)
   - Methods: smart default (value types→default, new()→new T(), etc.)
   - Indexers: backing dictionary, then smart default
   - Void methods: execute silently
```

## Verification Patterns

### Call Tracking

snippet: skill-SKILL-verification-call-tracking

### Property Tracking

snippet: skill-SKILL-verification-property-tracking

### Indexer Tracking

snippet: skill-SKILL-verification-indexer-tracking

## Backing Storage

### Properties

Properties use `interceptor.Value` for storage:

```csharp
[KnockOff]
public partial class SkBackingServiceKnockOff : ISkBackingService { }

// Direct access to backing value via interceptor
knockOff.Name.Value = "Pre-populated value";

// Without OnGet, getter returns interceptor.Value
Assert.Equal("Pre-populated value", service.Name);
```

### Indexers

snippet: skill-SKILL-backing-indexers

**Important**: `Reset()` does NOT clear backing storage.

## Supported Features

| Feature | Status |
|---------|--------|
| Explicit stubs (`[KnockOff]` on interface impl) | Supported |
| Generic standalone stubs (`Stub<T> : IRepo<T>`) | Supported |
| Inline interface stubs (`[KnockOff<TInterface>]`) | Supported |
| Inline class stubs (`[KnockOff<TClass>]`) | Supported |
| Delegate stubs (`[KnockOff<TDelegate>]`) | Supported |
| Partial property auto-instantiation (C# 13+) | Supported |
| Properties (get/set, get-only, set-only) | Supported |
| Void methods | Supported |
| Methods with return values | Supported |
| Methods with parameters | Supported |
| Method overloads (separate interceptors) | Supported |
| Out parameters | Supported |
| Ref parameters | Supported |
| Async methods (Task, Task<T>, ValueTask, ValueTask<T>) | Supported |
| Generic interfaces (concrete types) | Supported |
| Generic methods (via `.Of<T>()` pattern) | Supported |
| Multiple interfaces | Supported |
| Interface inheritance | Supported |
| Indexers | Supported |
| Events | Supported |
| Nested classes | Supported |
| User method detection | Supported |
| OnCall/OnGet/OnSet callbacks | Supported |
| Named tuple argument tracking | Supported |

## Common Patterns

### Conditional Returns

snippet: skill-SKILL-pattern-conditional

### Throwing Exceptions

snippet: skill-SKILL-pattern-exceptions

### Sequential Returns

snippet: skill-SKILL-pattern-sequential

### Async Methods

snippet: skill-SKILL-pattern-async

### Events

snippet: skill-SKILL-pattern-events

### Generic Methods

Generic methods use the `.Of<T>()` pattern for type-specific configuration:

snippet: skill-SKILL-pattern-generics

### Method Overloads

When an interface has overloaded methods, each overload gets its own interceptor with a **numeric suffix** (1-based):

snippet: skill-SKILL-pattern-overloads

Methods without overloads don't get a suffix:
```csharp
knockOff.SendEmail.CallCount;  // Single method - no suffix
```

### Nested Classes

KnockOff stubs can be nested inside test classes:

snippet: skill-SKILL-pattern-nested

**Critical:** All containing classes must be `partial`. This is a C# requirement—the generator produces partial class wrappers that must merge with your declarations.

```csharp
// ❌ Won't compile
public class MyTests
{
    [KnockOff]
    public partial class ServiceKnockOff : IService { }
}

// ✅ Correct
public partial class MyTests
{
    [KnockOff]
    public partial class ServiceKnockOff : IService { }
}
```

Works at any nesting depth—just ensure every class in the hierarchy is `partial`.

### Out Parameters

Methods with `out` parameters are fully supported. Out parameters are outputs, not inputs, so they're excluded from tracking but included in callbacks.

snippet: skill-SKILL-pattern-out-params

### Ref Parameters

Methods with `ref` parameters track the **input value** (before any callback modification).

snippet: skill-SKILL-pattern-ref-params

## Best Practices

### Embrace Source Generation

- **Complex interfaces are fine** — KnockOff generates everything; configure only what you test
- **Try it first** — If uncertain whether KnockOff works, add the attribute and build
- **Never create manual test doubles** — Defeats the purpose of source generation

### Stub Minimalism

Only configure members your test actually calls:

```csharp
// GOOD - minimal, relies on smart defaults
[KnockOff]
public partial class UserServiceKnockOff : IUserService
{
    protected User? GetUser(int id) => new User { Id = id };
    // Other 10 methods use smart defaults
}
```

### Choose the Right Pattern

| Need | Use |
|------|-----|
| Same behavior across all tests | User methods |
| Per-test customization | Callbacks |
| Static property value | `Value` property |
| Dynamic/computed value | `OnGet` callback |
| Interface used in multiple test classes | Stand-alone stub |
| One-off stub for single test class | Inline stub |

### Common Pitfalls

| Pitfall | Solution |
|---------|----------|
| Nested class won't compile | Make ALL containing classes `partial` |
| Out/ref callback won't compile | Use explicit delegate type: `(Handler.Delegate)((ko, ...) => ...)` |
| Reset didn't clear property value | `Reset()` clears tracking/callbacks only—set `Value = default` explicitly |
| Can't find overload interceptor | Overloads get numeric suffixes: `Method1`, `Method2` |
| Same inline stub in multiple classes | Use stand-alone stub to reduce generated code |

For comprehensive guidance, see [Best Practices](docs/guides/best-practices.md).

## Moq Migration Quick Reference

| Moq | KnockOff |
|-----|----------|
| `new Mock<IService>()` | `new ServiceKnockOff()` |
| `mock.Object` | `IService svc = stub;` (implicit) or `stub.Object` (class stubs) |
| `.Setup(x => x.Property).Returns(v)` | `stub.Property.Value = v` (static) or `stub.Property.OnGet = ...` (dynamic) |
| `.Setup(x => x.Method())` | `stub.Method.OnCall = (ko, ...) => ...` |
| `.Returns(value)` | `OnCall = (ko) => value` |
| `.ReturnsAsync(value)` | `OnCall = (ko) => Task.FromResult(value)` |
| `.Callback(action)` | Logic inside `OnCall` callback |
| `.Verify(Times.Once)` | `Assert.Equal(1, IService.Method.CallCount)` |
| `It.IsAny<T>()` | Implicit (callback receives all args) |
| `It.Is<T>(pred)` | Check in callback body |

## Additional Resources

For detailed guidance, see:
- [Best Practices](docs/guides/best-practices.md) - Consolidated best practices guide
- [Customization Patterns](customization-patterns.md) - Deep dive on user methods vs callbacks
- [Interceptor API Reference](interceptor-api.md) - Complete API for all interceptor types
- [Moq Migration](moq-migration.md) - Step-by-step migration patterns
- [Version Migrations](migrations.md) - Breaking changes and upgrade guides

## Skill Sync Status

All code examples in this skill are sourced from compiled, tested samples in the KnockOff repository.

| Repository | Samples Location | Sync Script |
|------------|------------------|-------------|
| [KnockOff](https://github.com/yourusername/KnockOff) | `src/Tests/KnockOff.Documentation.Samples/Skills/` | `scripts/extract-snippets.ps1` |

To update skill files after modifying samples:
```powershell
.\scripts\extract-snippets.ps1 -Update
```
