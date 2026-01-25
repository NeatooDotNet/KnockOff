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
public partial class InlineInterfacePatternTests
{
    private void Example()
    {
        // Generated stub accessed via Stubs namespace
        var stub = new Stubs.IAttrUserRepository();

        stub.GetById.OnCall((id) => new User { Id = id, Name = "Inline User" });

        IAttrUserRepository repository = stub;
    }
}
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
public partial class InlineClassPatternTests
{
    private void Example()
    {
        // Generated stub inherits from EmailServiceBase
        var stub = new Stubs.EmailServiceBase();

        // Use .Object to get the stub as the base class type
        EmailServiceBase service = stub.Object;

        // Virtual members can be intercepted
        stub.Send.OnCall((to, subject, body) => { });
    }
}
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
public partial class MultipleStubsPatternTests
{
    private void Example()
    {
        // Each interface gets its own stub
        var userRepo = new Stubs.IAttrUserRepository();
        var emailService = new Stubs.IAttrEmailService();
        var logger = new Stubs.IAttrLogger();

        // Configure each stub independently
        userRepo.GetById.OnCall((id) => new User { Id = id, Name = "Test" });
        emailService.Send.OnCall((to, subject, body) => { });
        logger.Log.OnCall((message) => { });
    }
}
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

## See Also

- [Getting Started](../getting-started.md) - First steps with KnockOff and basic stub creation
- [Interceptor API Reference](interceptor-api.md) - Complete reference for configuring stubs
- [Methods Guide](../guides/methods.md) - Configure method behavior and callbacks
- [Properties Guide](../guides/properties.md) - Work with property interceptors
