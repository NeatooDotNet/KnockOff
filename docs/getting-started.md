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

<!-- snippet: docs:getting-started:interface-definition -->
```csharp
public interface IEmailService
{
    void SendEmail(string to, string subject, string body);
    bool IsConnected { get; }
}
```
<!-- /snippet -->

### Step 2: Create a KnockOff Stub

Create a partial class marked with `[KnockOff]` that implements your interface:

<!-- snippet: docs:getting-started:stub-class -->
```csharp
[KnockOff]
public partial class EmailServiceKnockOff : IEmailService
{
    // That's it! The generator creates the implementation.
}
```
<!-- /snippet -->

The source generator automatically creates:
- Explicit interface implementations
- Interface-named properties (e.g., `IEmailService`) with handlers for each member
- Backing fields for properties (prefixed with interface name)
- An `AsEmailService()` helper method for explicit casting

### Step 3: Use in Tests

```csharp
[Fact]
public void NotificationService_SendsEmail_WhenUserRegisters()
{
    // Arrange
    var emailKnockOff = new EmailServiceKnockOff();
    IEmailService emailService = emailKnockOff;

    var notificationService = new NotificationService(emailService);

    // Act
    notificationService.NotifyRegistration("user@example.com");

    // Assert
    Assert.True(emailKnockOff.IEmailService.SendEmail.WasCalled);
    Assert.Equal("user@example.com", emailKnockOff.IEmailService.SendEmail.LastCallArgs?.to);
}
```

## Verification Basics

Every interface member gets a handler with tracking properties. Access handlers via the interface-named property (e.g., `knockOff.IEmailService`).

### For Methods

```csharp
// Check if called
Assert.True(knockOff.IEmailService.SendEmail.WasCalled);

// Check call count
Assert.Equal(3, knockOff.IEmailService.SendEmail.CallCount);

// Check last argument (single parameter)
Assert.Equal(42, knockOff.IUserService.GetById.LastCallArg);

// Check last arguments (multiple parameters - named tuple)
var args = knockOff.IEmailService.SendEmail.LastCallArgs;
Assert.Equal("user@example.com", args?.to);
Assert.Equal("Welcome", args?.subject);
```

### For Properties

```csharp
// Check getter calls
Assert.Equal(2, knockOff.IEmailService.IsConnected.GetCount);

// Check setter calls
Assert.Equal(1, knockOff.IUserService.Name.SetCount);
Assert.Equal("NewValue", knockOff.IUserService.Name.LastSetValue);
```

## Adding Custom Behavior

### Option 1: User-Defined Methods (Compile-Time)

Define protected methods in your stub class for consistent behavior:

<!-- snippet: docs:getting-started:user-method -->
```csharp
[KnockOff]
public partial class EmailServiceWithValidation : IEmailServiceWithValidation
{
    // This method is called when IEmailService.IsValidAddress is invoked
    protected bool IsValidAddress(string email) =>
        email.Contains("@") && email.Contains(".");
}
```
<!-- /snippet -->

### Option 2: Callbacks (Runtime)

Set callbacks for per-test behavior:

```csharp
[Fact]
public void RejectsEmail_WhenNotConnected()
{
    var knockOff = new EmailServiceKnockOff();

    // Configure property to return false
    knockOff.IEmailService.IsConnected.OnGet = (ko) => false;

    // Configure method to throw
    knockOff.IEmailService.SendEmail.OnCall = (ko, to, subject, body) =>
    {
        throw new InvalidOperationException("Not connected");
    };

    // ... test code
}
```

See [Customization Patterns](concepts/customization-patterns.md) for detailed guidance.

## Resetting State

Clear tracking and callbacks between tests or test phases:

```csharp
// Reset specific handler
knockOff.IEmailService.SendEmail.Reset();

// After reset:
Assert.Equal(0, knockOff.IEmailService.SendEmail.CallCount);
// Callbacks are also cleared
```

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
```csharp
knockOff.IUserService.GetUser.OnCall = (ko, id) => new User { Id = id, Name = "Test" };
```

Via user method (in stub class):

<!-- snippet: docs:getting-started:returning-values -->
```csharp
[KnockOff]
public partial class UserServiceKnockOff : IUserServiceSimple
{
    // Via user method (in stub class)
    protected User GetUser(int id) => new User { Id = id, Name = "Default" };
}
```
<!-- /snippet -->

### Simulating Failures

```csharp
knockOff.IRepository.SaveAsync.OnCall = (ko, entity) =>
    Task.FromException<int>(new DbException("Connection lost"));
```

### Capturing Arguments for Later Assertions

```csharp
List<string> sentEmails = new();

knockOff.IEmailService.SendEmail.OnCall = (ko, to, subject, body) =>
{
    sentEmails.Add(to);
};

// ... run test ...

Assert.Equal(3, sentEmails.Count);
Assert.Contains("admin@example.com", sentEmails);
```

### Method Overloads

When an interface has method overloads, each overload gets its own handler with a numeric suffix:

```csharp
public interface IProcessService
{
    void Process(string data);                           // Overload 1
    void Process(string data, int priority);             // Overload 2
    void Process(string data, int priority, bool async); // Overload 3
}

[KnockOff]
public partial class ProcessServiceKnockOff : IProcessService { }

// Each overload has its own handler (1-based numbering)
knockOff.IProcessService.Process1.CallCount;  // Calls to Process(string)
knockOff.IProcessService.Process2.CallCount;  // Calls to Process(string, int)
knockOff.IProcessService.Process3.CallCount;  // Calls to Process(string, int, bool)

// Identify exactly which overload was called
Assert.True(knockOff.IProcessService.Process2.WasCalled);
Assert.False(knockOff.IProcessService.Process1.WasCalled);

// Simple callbacks - no delegate casting needed
knockOff.IProcessService.Process1.OnCall = (ko, data) => { };
knockOff.IProcessService.Process2.OnCall = (ko, data, priority) => { };
knockOff.IProcessService.Process3.OnCall = (ko, data, priority, async) => { };

// Proper types - no nullable wrappers
var args = knockOff.IProcessService.Process3.LastCallArgs;
Assert.Equal("test", args.Value.data);
Assert.Equal(5, args.Value.priority);  // int, not int?
Assert.True(args.Value.async);
```

Methods without overloads don't get a suffix:
```csharp
knockOff.IEmailService.SendEmail.CallCount;  // Single method - no suffix
```

### Multiple Interfaces

Each interface gets its own property with separate tracking:

<!-- snippet: docs:getting-started:multiple-interfaces -->
```csharp
[KnockOff]
public partial class DataContextKnockOff : IRepository, IUnitOfWork
{
}
```
<!-- /snippet -->

```csharp
// Access via interface-named properties
Assert.True(knockOff.IRepository.Save.WasCalled);
Assert.True(knockOff.IUnitOfWork.Commit.WasCalled);

// Use AsXxx() for explicit casting
IRepository repo = knockOff.AsRepository();
IUnitOfWork uow = knockOff.AsUnitOfWork();

// Or cast directly
IRepository repo = knockOff;
```

### Nested Classes

KnockOff stubs can be nested inside test classes, which is a common pattern for organizing test fixtures:

```csharp
public partial class UserServiceTests  // Must be partial!
{
    [KnockOff]
    public partial class UserRepositoryKnockOff : IUserRepository
    {
    }

    [Fact]
    public void GetUser_ReturnsUser()
    {
        var knockOff = new UserRepositoryKnockOff();
        // ... test code
    }
}
```

**Important:** When nesting a `[KnockOff]` class inside another class, all containing classes must also be marked `partial`. This is a C# requirement—the generator produces a partial class that must merge with your nested class declaration.

```csharp
// ❌ Won't compile - containing class not partial
public class MyTests
{
    [KnockOff]
    public partial class ServiceKnockOff : IService { }
}

// ✅ Correct - containing class is partial
public partial class MyTests
{
    [KnockOff]
    public partial class ServiceKnockOff : IService { }
}
```

This works for any nesting depth—just ensure every class in the hierarchy is `partial`.

## Next Steps

- [Customization Patterns](concepts/customization-patterns.md) — Deep dive into user methods vs callbacks
- [Handler API Reference](reference/handler-api.md) — Complete API for tracking and callbacks
- [KnockOff vs Moq](knockoff-vs-moq.md) — Comparison with Moq patterns
- [Migration from Moq](migration-from-moq.md) — Step-by-step migration guide

## Guides by Member Type

- [Properties](guides/properties.md)
- [Methods](guides/methods.md)
- [Method Overloads](guides/method-overloads.md)
- [Async Methods](guides/async-methods.md)
- [Generic Interfaces](guides/generics.md)
- [Multiple Interfaces](guides/multiple-interfaces.md)
- [Interface Inheritance](guides/interface-inheritance.md)
- [Indexers](guides/indexers.md)
- [Events](guides/events.md)
