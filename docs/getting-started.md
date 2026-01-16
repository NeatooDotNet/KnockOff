# Getting Started

This guide walks you through creating your first KnockOff stub and using it in tests.

## Installation

```bash
dotnet add package KnockOff
```

Or add to your `.csproj`:

```xml
<PackageReference Include="KnockOff" Version="10.0.0" />
```

## Your First Stub

### 1. Define an Interface

<!-- snippet: getting-started-interface-definition -->
```cs
public interface IEmailService
{
    void SendEmail(string to, string subject, string body);
    bool IsConnected { get; }
}
```
<!-- endSnippet -->

### 2. Create a Stub

Create a partial class with `[KnockOff]` that implements your interface:

<!-- snippet: getting-started-stub-class -->
```cs
[KnockOff]
public partial class EmailServiceKnockOff : IEmailService
{
    // That's it! The generator creates the implementation.
}
```
<!-- endSnippet -->

The generator creates:
- Interface implementations for all members
- Interceptor properties for tracking and callbacks
- Backing fields for properties

### 3. Use in Tests

```csharp
[Fact]
public void NotificationService_SendsEmail_WhenUserRegisters()
{
    // Arrange
    var stub = new EmailServiceKnockOff();
    var service = new NotificationService(stub);

    // Act
    service.NotifyRegistration("user@example.com");

    // Assert
    Assert.True(stub.SendEmail.WasCalled);
    Assert.Equal("user@example.com", stub.SendEmail.LastCallArgs?.to);
}
```

## Configuring Behavior

### Return Values

Set `OnCall` to return values:

```csharp
stub.GetUser.OnCall = (ko, id) => new User { Id = id, Name = "Test" };
```

### Properties

Set `Value` for simple property returns:

```csharp
stub.IsConnected.Value = true;
stub.Name.Value = "TestService";
```

Or use `OnGet` for dynamic behavior:

```csharp
stub.CurrentTime.OnGet = (ko) => DateTime.UtcNow;
```

### Throwing Exceptions

```csharp
stub.SaveAsync.OnCall = (ko, entity) =>
    throw new InvalidOperationException("Connection lost");
```

## Verification

### Methods

```csharp
Assert.True(stub.SendEmail.WasCalled);
Assert.Equal(3, stub.SendEmail.CallCount);
Assert.Equal("user@example.com", stub.SendEmail.LastCallArgs?.to);
```

### Properties

```csharp
Assert.Equal(2, stub.IsConnected.GetCount);
Assert.Equal(1, stub.Name.SetCount);
Assert.Equal("NewValue", stub.Name.LastSetValue);
```

## Resetting State

Clear tracking and callbacks:

```csharp
stub.SendEmail.Reset();  // CallCount = 0, OnCall = null
```

## Viewing Generated Code

To see what the generator creates:

```xml
<PropertyGroup>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

Generated files appear in `Generated/KnockOff.Generator/KnockOff.KnockOffGenerator/`.

## Next Steps

- [Why KnockOff?](why-knockoff/) — Understand the design philosophy
- [Stub Patterns](guides/stub-patterns.md) — Standalone vs inline vs delegate stubs
- [Methods Guide](guides/methods.md) — Async, overloads, generics
- [Properties Guide](guides/properties.md) — Properties, indexers, init/required
- [Verification Guide](guides/verification.md) — Tracking and assertions
- [KnockOff vs Moq](knockoff-vs-moq.md) — Detailed comparison
