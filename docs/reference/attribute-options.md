[Home](../../README.md) > [Reference](./README.md) > Attribute Options

# Attribute Options

The `[KnockOff]` attribute supports three distinct patterns for creating test stubs. Each pattern has specific requirements and generates different output to suit various testing scenarios.

---

## Overview

KnockOff provides three attribute patterns:

1. **Stand-Alone Pattern** - Apply `[KnockOff]` to a partial class that implements an interface
2. **Inline Interface Pattern** - Apply `[KnockOff<IService>]` to generate a stub for an interface
3. **Inline Class Pattern** - Apply `[KnockOff<MyClass>]` to generate a stub for a concrete class

You can use multiple attributes on the same test class to generate stubs for different types.

---

## Stand-Alone Pattern

Apply `[KnockOff]` directly to a partial class that implements one or more interfaces.

**Requirements:**
- Class must be declared as `partial`
- Class must implement at least one interface
- Generated code provides explicit interface implementations

**Use when:**
- You want full control over the stub class definition
- You need to implement custom members or properties alongside the stub
- You prefer a traditional class-based approach

<!-- snippet: attr-standalone -->
```cs
// Stand-alone pattern: [KnockOff] on a partial class implementing an interface
[KnockOff]
public partial class AttrUserRepositoryStub : IAttrUserRepository { }
```
<!-- endSnippet -->

The source generator produces explicit interface implementations for all members, plus interceptor properties for test verification.

**Base class generation**: KnockOff generates a base class (e.g., `AttrUserRepositoryStubBase`) for standalone stubs. This base class contains virtual methods for each interface member that you can override to provide default stub behavior. Because KnockOff generates the base class, standalone stubs cannot have user-defined base classes (diagnostic **KO0200**).

See [User Methods](../guides/user-methods.md) for details on overriding virtual methods to define default behavior.

---

## Inline Interface Pattern

Apply `[KnockOff<IService>]` to any test class to generate a stub implementing the specified interface.

**Behavior:**
- Generates a stub class in the `Stubs` namespace nested under your test class
- Stub class name matches the interface name (the "I" prefix is preserved: `IUserRepository` → `Stubs.IUserRepository`)
- Generated class is `partial` and can be extended if needed

**Use when:**
- You want minimal boilerplate
- You're testing multiple services in one test class
- You don't need to customize the stub class declaration

<!-- snippet: attr-inline-interface -->
```cs
// Inline interface pattern: [KnockOff<IInterface>] generates stub in Stubs namespace
[KnockOff<IAttrUserRepository>]
public partial class InlineInterfacePatternTests { }

// Generated stub accessed via Stubs namespace
// var stub = new InlineInterfacePatternTests.Stubs.IAttrUserRepository();
```
<!-- endSnippet -->

Access the generated stub through the `Stubs` namespace nested under your test class.

---

## Inline Class Pattern

Apply `[KnockOff<MyClass>]` to generate a stub for a concrete class with virtual members.

**Behavior:**
- Generates a stub class in the `Stubs` namespace that inherits from the target class
- Provides an `.Object` property returning the stub cast to the base class type (use this to pass the stub where the base class type is expected)
- Only virtual members can be intercepted (non-virtual members call through to the base implementation)

**Use when:**
- You need to stub a concrete class for legacy code or framework types
- You want to intercept virtual methods or properties
- You need the stub to pass type checks for the base class

<!-- snippet: attr-inline-class -->
```cs
// Inline class pattern: [KnockOff<ConcreteClass>] generates stub inheriting from class
[KnockOff<EmailServiceBase>]
public partial class InlineClassPatternTests { }

// Generated stub inherits from EmailServiceBase
// var stub = new InlineClassPatternTests.Stubs.EmailServiceBase();
// EmailServiceBase service = stub.Object;  // Cast to base class type
```
<!-- endSnippet -->

**Note:** The `.Object` property is essential when working with code that requires the base class type. For example, if a constructor expects `EmailServiceBase`, you would pass `stub.Object` rather than `stub` directly.

---

## Multiple Stubs

Apply multiple `[KnockOff<T>]` attributes to generate stubs for several types in a single test class.

**Behavior:**
- Each attribute generates an independent stub in the `Stubs` namespace
- No conflicts between generated stubs
- Allows organizing related stubs in one test class

**Use when:**
- Testing interactions between multiple dependencies
- Organizing integration tests with several collaborators
- Reducing test class proliferation

<!-- snippet: attr-multiple -->
```cs
// Multiple attributes generate independent stubs in the Stubs namespace
[KnockOff<IAttrUserRepository>]
[KnockOff<IAttrEmailService>]
[KnockOff<IAttrLogger>]
public partial class MultipleStubsPatternTests { }

// Each interface gets its own stub in the Stubs namespace
// var userRepo = new MultipleStubsPatternTests.Stubs.IAttrUserRepository();
// var emailService = new MultipleStubsPatternTests.Stubs.IAttrEmailService();
// var logger = new MultipleStubsPatternTests.Stubs.IAttrLogger();
```
<!-- endSnippet -->

---

## Choosing a Pattern

| Pattern | Best For | Trade-offs |
|---------|----------|------------|
| **Stand-Alone** | Full control, custom members, traditional OOP style | More boilerplate, explicit class declaration |
| **Inline Interface** | Minimal setup, multiple stubs, rapid testing | Less control over class declaration |
| **Inline Class** | Legacy code, framework types, virtual methods | Only intercepts virtual members, inheritance constraints |

**Decision Guide:**

- **Start with Inline Interface** for most scenarios - it provides the fastest setup and least boilerplate
- **Use Stand-Alone** when you need to add custom properties, methods, or fields to your stub class
- **Use Inline Class** only when stubbing concrete classes (legacy code or framework types you can't modify)
- **Mix patterns** freely within a test project based on each stub's specific needs

All patterns support the same interceptor API for configuring behavior, tracking calls, and setting up callbacks.

---

## Strict Mode

Strict mode controls how stubs handle unconfigured members. When enabled, unconfigured method calls throw `StubException` instead of returning default values. This helps catch unexpected interactions during tests by failing fast.

### Per-Stub Strict Mode

Set strict mode for individual stubs via attribute property or constructor parameter:

```csharp
// Set via attribute property (default for all instances of this stub)
[KnockOff(Strict = true)]
public partial class UserRepoStub : IUserRepository { }

// Or via generic attribute
[KnockOff<IUserRepository>(Strict = true)]
public partial class MyTests { }

// Override per instance via constructor (inline stubs only)
var stub = new Stubs.IUserRepository(strict: true);

// Override at runtime
stub.Strict = false;  // Disable
stub.Strict();        // Enable via fluent API
```

### Assembly-Wide Strict Mode

Apply `[assembly: KnockOffStrict]` to make all stubs in an assembly default to strict mode:

```csharp
// In AssemblyInfo.cs or any file in your test project
[assembly: KnockOffStrict]
```

With this attribute, all stubs in the assembly throw `StubException` for unconfigured calls unless explicitly opted out.

### Opting Out of Assembly Strict Mode

Individual stubs can opt out of the assembly default:

```csharp
[assembly: KnockOffStrict]

// All stubs default to strict mode
[KnockOff<IUserService>]
public partial class UserTests { }

// Opt out via attribute property
[KnockOff<ILegacyService>(Strict = false)]
public partial class LegacyTests { }

// Opt out via constructor (inline stubs only)
var stub = new UserTests.Stubs.IUserService(strict: false);

// Opt out at runtime
stub.Strict = false;
```

### Precedence

Strict mode settings are resolved in this order (highest to lowest):

1. **Runtime:** `stub.Strict = false` or `stub.Strict()`
2. **Constructor:** `new Stubs.IService(strict: false)` (inline stubs only)
3. **Attribute:** `[KnockOff(Strict = false)]` or `[KnockOff<T>(Strict = false)]`
4. **Assembly:** `[assembly: KnockOffStrict]`
5. **Default:** `false` (non-strict)

### When to Use Strict Mode

**Use assembly-wide strict mode when:**
- You want strict behavior as the default for your entire test project
- You want to enforce explicit stub configuration as a coding standard
- You prefer opting out of strict mode rather than opting in

**Use per-stub strict mode when:**
- Only certain tests require strict verification
- You're migrating an existing test project incrementally
- Different tests have different strictness requirements

**Benefits of strict mode:**
- Catches missing configurations during test development
- Ensures all stub interactions are intentional
- Prevents tests from passing due to default return values

---

## See Also

- [Getting Started](../getting-started.md) - First steps with KnockOff and basic stub creation
- [Interceptor API Reference](interceptor-api.md) - Complete reference for configuring stubs
- [Methods Guide](../guides/methods.md) - Configure method behavior and callbacks
- [Properties Guide](../guides/properties.md) - Work with property interceptors

---

**UPDATED:** 2026-02-03
