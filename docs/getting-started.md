# Getting Started with KnockOff

This guide walks you through creating your first KnockOff stub and using it in tests.

## Installation

Add the KnockOff NuGet package to your test project:

```bash
dotnet add package KnockOff
```

Or add directly to your `.csproj`:

```xml
<PackageReference Include="KnockOff" Version="1.0.0" />
```

## Your First KnockOff

### Step 1: Define an Interface

<!-- snippet: getting-started-interface-definition -->
```cs
public interface IEmailService
{
    void SendEmail(string to, string subject, string body);
    bool IsConnected { get; }
}
```
<!-- endSnippet -->

### Step 2: Create a KnockOff Stub

Create a partial class marked with `[KnockOff]` that implements your interface:

<!-- snippet: getting-started-stub-class -->
```cs
[KnockOff]
public partial class EmailServiceKnockOff : IEmailService
{
    // That's it! The generator creates the implementation.
}
```
<!-- endSnippet -->

The source generator automatically creates:
- Explicit interface implementations
- Interceptor properties for each member (directly on the stub class)
- Backing fields for properties
- An `AsEmailService()` helper method for explicit casting

### Step 3: Use in Tests

<!-- snippet: getting-started-step3-test -->
```cs
// [Fact]
// public void NotificationService_SendsEmail_WhenUserRegisters()
// {
//     // Arrange
//     var emailKnockOff = new EmailServiceKnockOff();
//     IEmailService emailService = emailKnockOff;
//
//     var notificationService = new NotificationService(emailService);
//
//     // Act
//     notificationService.NotifyRegistration("user@example.com");
//
//     // Assert
//     Assert.True(emailKnockOff.SendEmail.WasCalled);
//     Assert.Equal("user@example.com", emailKnockOff.SendEmail.LastCallArgs?.to);
// }
```
<!-- endSnippet -->

## Verification Basics

Every interface member gets an interceptor with tracking properties. Access interceptors directly on the stub.

### For Methods

<!-- snippet: getting-started-method-verification -->
```cs
// Check if called
// Assert.True(knockOff.SendEmail.WasCalled);
//
// Check call count
// Assert.Equal(3, knockOff.SendEmail.CallCount);
//
// Check last argument (single parameter)
// Assert.Equal(42, knockOff.GetById.LastCallArg);
//
// Check last arguments (multiple parameters - named tuple)
// var args = knockOff.SendEmail.LastCallArgs;
// Assert.Equal("user@example.com", args?.to);
// Assert.Equal("Welcome", args?.subject);
```
<!-- endSnippet -->

### For Properties

<!-- snippet: getting-started-property-verification -->
```cs
// Check getter calls
// Assert.Equal(2, knockOff.IsConnected.GetCount);
//
// Check setter calls
// Assert.Equal(1, knockOff.Name.SetCount);
// Assert.Equal("NewValue", knockOff.Name.LastSetValue);
```
<!-- endSnippet -->

## Adding Custom Behavior

### Option 1: User-Defined Methods (Compile-Time)

Define protected methods in your stub class for consistent behavior:

<!-- snippet: getting-started-user-method -->
```cs
[KnockOff]
public partial class EmailServiceWithValidation : IEmailServiceWithValidation
{
    // This method is called when IEmailService.IsValidAddress is invoked
    protected bool IsValidAddress(string email) =>
        email.Contains("@") && email.Contains(".");
}
```
<!-- endSnippet -->

### Option 2: Callbacks (Runtime)

Set callbacks for per-test behavior:

<!-- snippet: getting-started-callbacks -->
```cs
// [Fact]
// public void RejectsEmail_WhenNotConnected()
// {
//     var knockOff = new EmailServiceKnockOff();
//
//     // Configure property to return false
//     knockOff.IsConnected.OnGet = (ko) => false;
//
//     // Configure method to throw
//     knockOff.SendEmail.OnCall = (ko, to, subject, body) =>
//     {
//         throw new InvalidOperationException("Not connected");
//     };
//
//     // ... test code
// }
```
<!-- endSnippet -->

See [Customization Patterns](concepts/customization-patterns.md) for detailed guidance.

## Resetting State

Clear tracking and callbacks between tests or test phases:

<!-- snippet: getting-started-reset -->
```cs
// Reset specific handler
// knockOff.SendEmail.Reset();
//
// After reset:
// Assert.Equal(0, knockOff.SendEmail.CallCount);
// Callbacks are also cleared
```
<!-- endSnippet -->

## Viewing Generated Code

To see what the generator creates, enable output in your test project:

```xml
<PropertyGroup>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

Generated files appear in `Generated/KnockOff.Generator/KnockOff.KnockOffGenerator/`.

## Common Patterns

### Returning Values from Methods

Via callback:
<!-- snippet: getting-started-via-callback -->
```cs
// knockOff.GetUser.OnCall = (ko, id) => new User { Id = id, Name = "Test" };
```
<!-- endSnippet -->

Via user method (in stub class):

<!-- snippet: getting-started-returning-values -->
```cs
[KnockOff]
public partial class UserServiceKnockOff : IUserServiceSimple
{
    // Via user method (in stub class)
    protected User GetUser(int id) => new User { Id = id, Name = "Default" };
}
```
<!-- endSnippet -->

### Simulating Failures

<!-- snippet: getting-started-simulating-failures -->
```cs
// knockOff.SaveAsync.OnCall = (ko, entity) =>
//     Task.FromException<int>(new DbException("Connection lost"));
```
<!-- endSnippet -->

### Capturing Arguments for Later Assertions

<!-- snippet: getting-started-capturing-arguments -->
```cs
// List<string> sentEmails = new();
//
// knockOff.SendEmail.OnCall = (ko, to, subject, body) =>
// {
//     sentEmails.Add(to);
// };
//
// // ... run test ...
//
// Assert.Equal(3, sentEmails.Count);
// Assert.Contains("admin@example.com", sentEmails);
```
<!-- endSnippet -->

### Method Overloads

When an interface has method overloads, each overload gets its own interceptor with a numeric suffix:

<!-- snippet: getting-started-method-overloads -->
```cs
public interface IProcessService
{
    void Process(string data);                           // Overload 1
    void Process(string data, int priority);             // Overload 2
    void Process(string data, int priority, bool async); // Overload 3
}

[KnockOff]
public partial class ProcessServiceKnockOff : IProcessService { }

// Each overload has its own interceptor (1-based numbering)
// knockOff.Process1.CallCount;  // Calls to Process(string)
// knockOff.Process2.CallCount;  // Calls to Process(string, int)
// knockOff.Process3.CallCount;  // Calls to Process(string, int, bool)
//
// Identify exactly which overload was called
// Assert.True(knockOff.Process2.WasCalled);
// Assert.False(knockOff.Process1.WasCalled);
//
// Simple callbacks - no delegate casting needed
// knockOff.Process1.OnCall = (ko, data) => { };
// knockOff.Process2.OnCall = (ko, data, priority) => { };
// knockOff.Process3.OnCall = (ko, data, priority, async) => { };
//
// Proper types - no nullable wrappers
// var args = knockOff.Process3.LastCallArgs;
// Assert.Equal("test", args.Value.data);
// Assert.Equal(5, args.Value.priority);  // int, not int?
// Assert.True(args.Value.async);
```
<!-- endSnippet -->

Methods without overloads don't get a suffix:
<!-- snippet: getting-started-single-method-suffix -->
```cs
// knockOff.SendEmail.CallCount;  // Single method - no suffix
```
<!-- endSnippet -->

### Single Interface Constraint

Standalone KnockOff stubs implement **one interface** (plus its inheritance chain). If you need multiple unrelated interfaces, create separate stubs:

<!-- snippet: getting-started-single-interface -->
```cs
// Single interface - this is the standard pattern
[KnockOff]
public partial class SingleRepositoryKnockOff : IRepository { }

[KnockOff]
public partial class SingleUnitOfWorkKnockOff : IUnitOfWork { }

// Interface inheritance is fine - IEntity is a single interface
[KnockOff]
public partial class EntityKnockOff : IEntity { }

// Multiple unrelated interfaces - not supported
// This emits diagnostic KO0010
// [KnockOff]
// public partial class DataContextKnockOff : IRepository, IUnitOfWork { }
```
<!-- endSnippet -->

For multiple unrelated interfaces, use [inline stubs](guides/inline-stubs.md) instead:

<!-- snippet: getting-started-inline-stubs-example -->
```cs
[KnockOff<IRepository>]
[KnockOff<IUnitOfWork>]
public partial class InlineStubsExampleTests
{
    // [Fact]
    // public void Test()
    // {
    //     var repo = new Stubs.IRepository();
    //     var uow = new Stubs.IUnitOfWork();
    //     // ...
    // }
}
```
<!-- endSnippet -->

### Nested Classes

KnockOff stubs can be nested inside test classes, which is a common pattern for organizing test fixtures:

<!-- snippet: getting-started-nested-classes -->
```cs
public partial class UserServiceTests  // Must be partial!
{
    [KnockOff]
    public partial class UserRepositoryKnockOff : IUserRepository
    {
    }

    // [Fact]
    // public void GetUser_ReturnsUser()
    // {
    //     var knockOff = new UserRepositoryKnockOff();
    //     // ... test code
    // }
}
```
<!-- endSnippet -->

**Important:** When nesting a `[KnockOff]` class inside another class, all containing classes must also be marked `partial`. This is a C# requirement—the generator produces a partial class that must merge with your nested class declaration.

<!-- snippet: getting-started-nested-partial-error -->
```cs
// Won't compile - containing class not partial
// public class MyBadTests
// {
//     [KnockOff]
//     public partial class ServiceKnockOff : IService { }
// }
```
<!-- endSnippet -->

<!-- snippet: getting-started-nested-partial-correct -->
```cs
// Correct - containing class is partial
// public partial class MyGoodTests
// {
//     [KnockOff]
//     public partial class ServiceKnockOff : IService { }
// }
```
<!-- endSnippet -->

This works for any nesting depth—just ensure every class in the hierarchy is `partial`.

## Next Steps

- [Best Practices](guides/best-practices.md) — Guidelines for effective KnockOff usage
- [Customization Patterns](concepts/customization-patterns.md) — Deep dive into user methods vs callbacks
- [Interceptor API Reference](reference/interceptor-api.md) — Complete API for tracking and callbacks
- [KnockOff vs Moq](knockoff-vs-moq.md) — Comparison with Moq patterns
- [Migration from Moq](migration-from-moq.md) — Step-by-step migration guide

## Guides

- [Best Practices](guides/best-practices.md)
- [Properties](guides/properties.md)
- [Methods](guides/methods.md)
- [Async Methods](guides/async-methods.md)
- [Generic Interfaces](guides/generics.md)
- [Multiple Interfaces](guides/multiple-interfaces.md)
- [Interface Inheritance](guides/interface-inheritance.md)
- [Indexers](guides/indexers.md)
- [Events](guides/events.md)
- [Inline Stubs](guides/inline-stubs.md)
- [Delegates](guides/delegates.md)
