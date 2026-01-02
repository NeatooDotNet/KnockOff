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

```csharp
public interface IEmailService
{
    void SendEmail(string to, string subject, string body);
    bool IsConnected { get; }
}
```

### Step 2: Create a KnockOff Stub

Create a partial class marked with `[KnockOff]` that implements your interface:

```csharp
using KnockOff;

[KnockOff]
public partial class EmailServiceKnockOff : IEmailService
{
    // That's it! The generator creates the implementation.
}
```

The source generator automatically creates:
- Explicit interface implementations
- A `Spy` property with handlers for each member
- Backing fields for properties
- An `AsEmailService()` helper method

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
    Assert.True(emailKnockOff.Spy.SendEmail.WasCalled);
    Assert.Equal("user@example.com", emailKnockOff.Spy.SendEmail.LastCallArgs?.to);
}
```

## Verification Basics

Every interface member gets a handler in the `Spy` class with tracking properties:

### For Methods

```csharp
// Check if called
Assert.True(knockOff.Spy.SendEmail.WasCalled);

// Check call count
Assert.Equal(3, knockOff.Spy.SendEmail.CallCount);

// Check last argument (single parameter)
Assert.Equal(42, knockOff.Spy.GetById.LastCallArg);

// Check last arguments (multiple parameters - named tuple)
var args = knockOff.Spy.SendEmail.LastCallArgs;
Assert.Equal("user@example.com", args?.to);
Assert.Equal("Welcome", args?.subject);

// Check all calls
Assert.Equal(3, knockOff.Spy.SendEmail.AllCalls.Count);
Assert.Equal("first@example.com", knockOff.Spy.SendEmail.AllCalls[0].to);
```

### For Properties

```csharp
// Check getter calls
Assert.Equal(2, knockOff.Spy.IsConnected.GetCount);

// Check setter calls
Assert.Equal(1, knockOff.Spy.Name.SetCount);
Assert.Equal("NewValue", knockOff.Spy.Name.LastSetValue);
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
    knockOff.Spy.IsConnected.OnGet = (ko) => false;

    // Configure method to throw
    knockOff.Spy.SendEmail.OnCall = (ko, args) =>
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
knockOff.Spy.SendEmail.Reset();

// After reset:
Assert.Equal(0, knockOff.Spy.SendEmail.CallCount);
Assert.Null(knockOff.Spy.SendEmail.OnCall);
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
knockOff.Spy.GetUser.OnCall = (ko, id) => new User { Id = id, Name = "Test" };

// Via user method (in stub class)
protected User GetUser(int id) => new User { Id = id, Name = "Default" };
```

### Simulating Failures

```csharp
knockOff.Spy.SaveAsync.OnCall = (ko, entity) =>
    Task.FromException<int>(new DbException("Connection lost"));
```

### Capturing Arguments for Later Assertions

```csharp
List<string> sentEmails = new();

knockOff.Spy.SendEmail.OnCall = (ko, args) =>
{
    sentEmails.Add(args.to);
};

// ... run test ...

Assert.Equal(3, sentEmails.Count);
Assert.Contains("admin@example.com", sentEmails);
```

### Multiple Interfaces

```csharp
[KnockOff]
public partial class DataContextKnockOff : IRepository, IUnitOfWork
{
}

// Use either interface
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
- [Async Methods](guides/async-methods.md)
- [Generic Interfaces](guides/generics.md)
- [Multiple Interfaces](guides/multiple-interfaces.md)
- [Interface Inheritance](guides/interface-inheritance.md)
- [Indexers](guides/indexers.md)
- [Events](guides/events.md)
