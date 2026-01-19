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
- Stub class name matches the interface name without the "I" prefix (e.g., `IUserRepository` → `UserRepository`)
- Generated class is `partial` and can be extended if needed

**Use when:**
- You want minimal boilerplate
- You're testing multiple services in one test class
- You don't need to customize the stub class declaration

<!-- snippet: attr-inline-interface -->
```cs
// Inline interface pattern: [KnockOff<IInterface>] generates Stubs.IInterfaceName
[KnockOff<IAttrUserRepository>]
public partial class InlineInterfacePatternTests
{
    [Fact]
    public void InlineInterface_GeneratesStubInStubsNamespace()
    {
        // Generated stub: Stubs.IAttrUserRepository
        var stub = new Stubs.IAttrUserRepository();

        stub.GetById.OnCall((ko, id) => new User { Id = id, Name = "Inline User" });

        IAttrUserRepository repository = stub;
        var user = repository.GetById(1);

        Assert.NotNull(user);
        Assert.Equal("Inline User", user.Name);
    }
}
```
<!-- endSnippet -->

Access the generated stub through the `Stubs` namespace: `var stub = new Stubs.UserRepository();`

---

## Inline Class Pattern

Apply `[KnockOff<MyClass>]` to generate a stub for a concrete class with virtual members.

**Behavior:**
- Generates a stub class in the `Stubs` namespace that inherits from the target class
- Provides an `.Object` property returning the stub cast to the base class type
- Only virtual members can be intercepted

**Use when:**
- You need to stub a concrete class for legacy code or framework types
- You want to intercept virtual methods or properties
- You need the stub to pass type checks for the base class

<!-- snippet: attr-inline-class -->
```cs
// Inline class pattern: [KnockOff<SomeClass>] generates stub inheriting from class
[KnockOff<EmailServiceBase>]
public partial class InlineClassPatternTests
{
    [Fact]
    public void InlineClass_ProvidesObjectProperty()
    {
        // Generated stub inherits from EmailServiceBase
        var stub = new Stubs.EmailServiceBase();

        // .Object property returns the stub as the base class type
        EmailServiceBase service = stub.Object;

        // Can intercept virtual members
        stub.Send.OnCall((ko, to, subject, body) => { });

        service.Send("test@example.com", "Hello", "World");

        Assert.True(stub.Send.WasCalled);
    }
}
```
<!-- endSnippet -->

The `.Object` property lets you pass the stub wherever the base class type is expected: `IEmailSender sender = new Stubs.EmailService().Object;`

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
// Multiple inline stubs: Each attribute generates a separate stub
[KnockOff<IAttrUserRepository>]
[KnockOff<IAttrEmailService>]
[KnockOff<IAttrLogger>]
public partial class MultipleStubsPatternTests
{
    [Fact]
    public void MultipleStubs_GeneratesEachInStubsNamespace()
    {
        // Each interface gets its own stub in Stubs namespace
        var userRepo = new Stubs.IAttrUserRepository();
        var emailService = new Stubs.IAttrEmailService();
        var logger = new Stubs.IAttrLogger();

        // Configure each stub independently
        userRepo.GetById.OnCall((ko, id) => new User { Id = id, Name = "Test" });
        emailService.Send.OnCall((ko, to, subject, body) => { });
        logger.Log.OnCall((ko, message) => { });

        // Use in tests
        IAttrUserRepository repo = userRepo;
        IAttrEmailService email = emailService;
        IAttrLogger log = logger;

        repo.GetById(1);
        email.Send("a@b.com", "Subject", "Body");
        log.Log("Test message");

        Assert.True(userRepo.GetById.WasCalled);
        Assert.True(emailService.Send.WasCalled);
        Assert.True(logger.Log.WasCalled);
    }
}
```
<!-- endSnippet -->

---

## Choosing a Pattern

| Pattern | Best For | Trade-offs |
|---------|----------|------------|
| **Stand-Alone** | Full control, custom members | More boilerplate, explicit class declaration |
| **Inline Interface** | Minimal setup, multiple stubs | Less control over class declaration |
| **Inline Class** | Legacy code, virtual methods | Only intercepts virtual members, inheritance constraints |

All patterns support the same interceptor API for configuring behavior, tracking calls, and setting up callbacks.
