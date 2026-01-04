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
- Interface spy properties (e.g., `IEmailService`) with handlers for each member
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

Every interface member gets a handler in its interface spy class with tracking properties. Access handlers via the interface spy property (e.g., `knockOff.IEmailService`).

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

// Check all calls
Assert.Equal(3, knockOff.IEmailService.SendEmail.AllCalls.Count);
Assert.Equal("first@example.com", knockOff.IEmailService.SendEmail.AllCalls[0].to);
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

```csharp
[KnockOff]
public partial class EmailServiceKnockOff : IEmailService
{
    // This method is called when IEmailService.IsValidAddress is invoked
    protected bool IsValidAddress(string email) =>
        email.Contains("@") && email.Contains(".");
}
```

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

```csharp
// Via callback
knockOff.IUserService.GetUser.OnCall = (ko, id) => new User { Id = id, Name = "Test" };

// Via user method (in stub class)
protected User GetUser(int id) => new User { Id = id, Name = "Default" };
```

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

Each interface gets its own spy property with separate tracking:

```csharp
[KnockOff]
public partial class DataContextKnockOff : IRepository, IUnitOfWork
{
}

// Access via interface spy properties
Assert.True(knockOff.IRepository.Save.WasCalled);
Assert.True(knockOff.IUnitOfWork.Commit.WasCalled);

// Use AsXxx() for explicit casting
IRepository repo = knockOff.AsRepository();
IUnitOfWork uow = knockOff.AsUnitOfWork();

// Or cast directly
IRepository repo = knockOff;
```

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
