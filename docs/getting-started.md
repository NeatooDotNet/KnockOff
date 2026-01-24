# Getting Started with KnockOff

KnockOff is a Roslyn Source Generator that creates unit test stubs at compile time. Unlike runtime mocking frameworks, KnockOff generates explicit implementations you can read, debug, and configure using partial classes.

## Prerequisites

- .NET 8.0 SDK or later
- A test framework (xUnit, NUnit, MSTest)
- Your favorite C# IDE (Visual Studio, Rider, VS Code)

## Installation

Add the KnockOff package to your test project:

<!-- snippet: getting-started-install -->
```cs
// Add KnockOff to your test project via CLI:
// dotnet add package KnockOff

// Or add to your .csproj:
// <PackageReference Include="KnockOff" Version="10.23.0" />
```
<!-- endSnippet -->

## Your First Stub - Stand-Alone Pattern

The stand-alone pattern uses the `[KnockOff]` attribute on a partial class that implements your test interface.

### Define the Stub

First, define a test interface and create a partial class that implements it:

<!-- snippet: getting-started-standalone-define -->
```cs
// Define the interface you want to stub
public interface IUserRepo
{
    User? GetById(int id);
    bool SaveUser(User user);
}

// Create a partial class with [KnockOff] attribute
[KnockOff]
public partial class UserRepoStub : IUserRepo
{
    // No implementations needed - the generator creates them
}
```
<!-- endSnippet -->

When you build, KnockOff generates:
- Explicit interface implementations for all members
- Interceptor objects for tracking calls and configuring behavior
- Properties named after your interface (e.g., `IUserRepository`) for accessing interceptors

### Use the Stub in Tests

Configure and verify stub behavior through the generated interceptors:

<!-- snippet: getting-started-standalone-use -->
```cs
[Fact]
public void SaveUser_WhenCalled_TracksInvocation()
{
    // Arrange - create the stub
    var stub = new UserRepoStub();

    // Configure method behavior using OnCall
    // Chain .Verifiable() to mark for batch verification
    stub.SaveUser.OnCall((user) => true).Verifiable();

    // Act - use through the interface
    IUserRepo repository = stub;
    var result = repository.SaveUser(new User { Id = 1, Name = "Alice" });

    // Assert - Verify() checks all members marked with .Verifiable()
    Assert.True(result);
    stub.Verify();
}
```
<!-- endSnippet -->

## Your First Stub - Inline Pattern

The inline pattern generates the entire stub class for you using `[KnockOff<TInterface>]`.

### Define the Stub

Mark your test class with the inline attribute:

<!-- snippet: getting-started-inline-define -->
```cs
// Add [KnockOff<T>] attribute to your test class
[KnockOff<IEmailSvc>]
public partial class InlineStubTests
{
    // The source generator creates Stubs.IEmailSvc for you
}
```
<!-- endSnippet -->

KnockOff generates a nested `Stubs` class containing your stub implementation.

### Use the Stub in Tests

Instantiate and configure the generated stub:

<!-- snippet: getting-started-inline-use -->
```cs
[Fact]
public void Send_WhenCalled_TracksMessage()
{
    // Arrange - instantiate the generated stub
    var stub = new Stubs.IEmailSvc();

    // Configure behavior and mark as verifiable
    // OnCall returns a tracking object for argument access
    var tracking = stub.Send.OnCall((to, subject, body) => { }).Verifiable();

    // Act - use through the interface
    IEmailSvc emailService = stub;
    emailService.Send("user@example.com", "Welcome", "Hello!");

    // Assert - Verify() checks method was called
    stub.Verify();
    // Access last arguments from tracking
    var args = tracking.LastArgs;
    Assert.Equal("user@example.com", args.to);
}
```
<!-- endSnippet -->

## Configuring Return Values

### Value Overloads - Simple Syntax

For methods and properties that return values, KnockOff provides convenient value overloads that let you specify the return value directly:

```csharp
// Instead of writing callbacks for simple returns:
stub.GetById.OnCall((id) => new User { Id = id, Name = "Alice" });

// You can use the value overload:
stub.GetById.OnCall(new User { Id = 1, Name = "Alice" });
```

### Properties - OnGet/OnSet

Configure property behavior with `OnGet` for getters and `OnSet` for setters:

```csharp
// Return a fixed value from the getter
stub.Name.OnGet("Alice");

// Track setter calls
stub.Name.OnSet((value) => Console.WriteLine($"Name set to: {value}"));
```

### Async Methods - Auto-Wrapping

For async methods returning `Task<T>` or `ValueTask<T>`, KnockOff automatically wraps your value:

```csharp
// Value is automatically wrapped in Task.FromResult
stub.GetUserAsync.OnCall(new User { Name = "Alice" });

// Equivalent to:
stub.GetUserAsync.OnCall(() => Task.FromResult(new User { Name = "Alice" }));
```

### When to Use Callbacks vs Values

| Syntax | Use When |
|--------|----------|
| `.OnCall(value)` | Returning a fixed value |
| `.OnCall(callback)` | Computing values based on arguments, side effects, or tracking |
| `.OnGet(value)` | Property returns a fixed value |
| `.OnGet(callback)` | Property computes value dynamically |

Both syntaxes return tracking objects for call verification.

## Understanding Generated Code

### Where to Find Generated Files

KnockOff outputs generated code to your project's `Generated/` folder. You can view these files in your IDE:

- **Visual Studio**: Expand Dependencies → Analyzers → KnockOff.SourceGenerator
- **Rider**: Navigate to the Generated folder in the project structure
- **File System**: `obj/{Configuration}/{TargetFramework}/generated/KnockOff.SourceGenerator/`

Generated files are also committed to source control (in the `Generated/` folder) so you can track changes in diffs and PRs.

### What Gets Generated

For each stub, KnockOff generates:

1. **Explicit interface implementations** - Every interface member is implemented explicitly
2. **Interceptor classes** - Per-member classes that track calls, arguments, and return values
3. **Container properties** - Interface-named properties that provide access to interceptors (e.g., `IUserRepository`)

The generated code is readable C# that mirrors your interface structure. You can review it in the `Generated/` folder to understand how KnockOff implements your stub.

## Next Steps

Now that you've created your first stubs, explore more features:

- **[Stub Patterns](guides/stub-patterns.md)** - Learn about all three stub patterns (Stand-Alone, Inline Interface, Inline Class)
- **[Methods](guides/methods.md)** - Configure method behavior with OnCall, track arguments, handle async methods
- **[Properties](guides/properties.md)** - Use OnGet/OnSet for properties, track access, configure backing values
- **[Interceptor API Reference](reference/interceptor-api.md)** - Complete reference for the interceptor API
