# Getting Started with KnockOff

KnockOff is a Roslyn Source Generator that creates unit test stubs at compile time. Unlike runtime mocking frameworks, KnockOff generates explicit implementations you can read, debug, and configure using partial classes.

## Prerequisites

- .NET 8.0 SDK or later
- A test framework (xUnit, NUnit, MSTest)
- Your favorite C# IDE (Visual Studio, Rider, VS Code)

## Installation

Add the KnockOff package to your test project using the .NET CLI:

```bash
dotnet add package KnockOff
```

Or add directly to your `.csproj` file:

<!-- snippet: getting-started-install -->
```cs
// Add KnockOff to your test project via CLI:
// dotnet add package KnockOff

// Or add to your .csproj:
// <PackageReference Include="KnockOff" Version="0.36.0" />
```
<!-- endSnippet -->

## Your First Stub - Stand-Alone Pattern

The stand-alone pattern uses the `[KnockOff]` attribute on a partial class that implements your test interface.

**Note**: The examples below use a simple `User` class (with `Id` and `Name` properties) for demonstration purposes. In your tests, substitute your own domain types.

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
- Interceptor classes for tracking calls and configuring behavior
- Public interceptor properties for each interface member (e.g., `GetById`, `SaveUser`)

### Use the Stub in Tests

Configure and verify stub behavior through the generated interceptors:

<!-- snippet: getting-started-standalone-use -->
```cs
// Configure behavior and mark for verification
stub.SaveUser.Return((user) => true).Verifiable();

// Call the method through the interface
IUserRepo repository = stub;
repository.SaveUser(new User { Id = 1, Name = "Alice" });

// Verify() checks all .Verifiable() members were called
stub.Verify();
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
// Call returns a tracking object for argument access
var tracking = stub.Send.Call((to, subject, body) => { }).Verifiable();

IEmailSvc emailService = stub;
emailService.Send("user@example.com", "Welcome", "Hello!");

// Access captured arguments from the tracking object
var args = tracking.LastArgs!.Value;
```
<!-- endSnippet -->

## Understanding Method Configuration

KnockOff provides two methods for configuring stub behavior: `Return(value/callback)` for non-void methods and `Call(callback)` for void methods.

### Return - Simple Return Values

When your method needs to return a fixed value, use `Return()`. KnockOff generates a `Return(TReturn value)` method for all methods that return values:

<!-- snippet: getting-started-value-overloads -->
```cs
// Return() - simple fixed value
stub.GetById.Return(new User { Id = 1, Name = "Alice" });

// Return() - dynamic value based on arguments
stub.GetById.Return((id) => new User { Id = id, Name = "Dynamic" });
```
<!-- endSnippet -->

**Key benefits of Return(value)**:
- Simpler syntax when you don't need dynamic logic
- Still returns a tracking object for verification
- Works with async methods (auto-wraps in Task.FromResult)

### Return(callback) - Dynamic Behavior

When you need to compute values based on arguments or implement conditional logic, use the callback overload of Return:

<!-- snippet: getting-started-oncall-dynamic -->
```cs
// Use Return for fixed values
stub.GetById.Return(new User { Id = 1, Name = "Alice" });

// Use Return for dynamic values, side effects, or conditional logic
stub.GetById.Return((id) => new User { Id = id, Name = id > 100 ? "Admin" : "Regular" });

// Both return tracking objects for verification
```
<!-- endSnippet -->

**When to use callbacks**:
- Computing return values based on input arguments
- Implementing conditional logic
- Tracking or validating argument values
- Performing side effects (like updating test state)

### Properties - Get/Set

Properties use `Get` for getters and `Set` for setters. Both support value and callback overloads:

<!-- snippet: getting-started-property-configuration -->
```cs
// Get - configure the getter return value
stub.CurrentUser.Get(new User { Id = 1, Name = "Alice" });

// Set - capture or validate setter calls
stub.CurrentUser.Set((user) => capturedUser = user);
```
<!-- endSnippet -->

### Async Methods - Auto-Wrapping

For async methods returning `Task<T>` or `ValueTask<T>`, KnockOff automatically handles the async wrapping - both for value overloads and callbacks.

#### Value Overload (Simplest)

<!-- snippet: getting-started-async-wrapping -->
```cs
// Return() auto-wraps in Task.FromResult - no manual wrapping needed
stub.GetUserAsync.Return(new User { Id = 1, Name = "Alice" });
```
<!-- endSnippet -->

#### Simplified Callbacks

When you need callback logic but don't need actual async operations, return the inner type directly - KnockOff auto-wraps the result:

<!-- snippet: async-task-simplified-callback -->
```cs
// Return() with unwrapped return type - auto-wrapped in Task.FromResult
stub.GetUserAsync.Return((id) => new User { Id = id, Name = "Alice" }).Verifiable();
```
<!-- endSnippet -->

#### Void Async Methods

For `Task` or `ValueTask` methods (no return value), use `Action` callbacks - KnockOff auto-returns `Task.CompletedTask`:

<!-- snippet: async-task-simplified-void -->
```cs
// Action callback for void async - Task.CompletedTask auto-returned
stub.UpdateUserAsync.Call((user) => updatedUsers.Add(user)).Verifiable();
```
<!-- endSnippet -->

You don't need to manually wrap values in `Task.FromResult` or return `Task.CompletedTask` - KnockOff handles this for you.

### Decision Guide: Return vs Call

| Syntax | Use When | Example |
|--------|----------|---------|
| `.Return(value)` | Returning a fixed value | `stub.GetStatus.Return("OK")` |
| `.Return(callback)` | Computing values from arguments | `stub.GetUser.Return((id) => users[id])` |
| `.Return(callback)` | Conditional logic | `stub.IsValid.Return((x) => x > 0)` |
| `.Call(callback)` | Side effects or tracking (void methods) | `stub.Save.Call((u) => saved.Add(u))` |

**Important**: Both syntaxes return tracking objects, so you can verify calls regardless of which you use.

## Understanding Generated Code

### Where to Find Generated Files

KnockOff outputs generated code to your project's `Generated/` folder at the project root. You can view these files in your IDE:

- **Visual Studio**: Expand Dependencies → Analyzers → KnockOff.SourceGenerator
- **Rider**: Navigate to the Generated folder in the project structure
- **File System**: Look in the `Generated/` folder within your test project directory

Generated files are excluded from source control by default (the `Generated/` folder is in `.gitignore`). You can inspect them locally during development to understand how KnockOff implements your stubs.

### What Gets Generated

For each stub, KnockOff generates:

1. **Explicit interface implementations** - Every interface member is implemented explicitly
2. **Interceptor classes** - Per-member classes that track calls, arguments, and return values
3. **Interceptor properties** - Public properties for each interface member (e.g., `GetById`, `SaveUser`) that expose the interceptor API

The generated code is readable C# that mirrors your interface structure. You can review it in the `Generated/` folder to understand how KnockOff implements your stub.

## Next Steps

Now that you've created your first stubs, explore more features:

- **[Stub Patterns](guides/stub-patterns.md)** - Learn about all nine stub patterns (Standalone, Generic Standalone, Standalone Class, Generic Standalone Class, Inline Interface, Inline Class, Inline Delegate, Open Generic Interface, Open Generic Class)
- **[Methods](guides/methods.md)** - Configure method behavior with Return/Call, track arguments, handle async methods
- **[Properties](guides/properties.md)** - Use Get/Set for properties, track access, configure backing values
- **[Interceptor API Reference](reference/interceptor-api.md)** - Complete reference for the interceptor API

---

**UPDATED:** 2026-01-27
