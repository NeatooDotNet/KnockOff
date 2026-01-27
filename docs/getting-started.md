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
// <PackageReference Include="KnockOff" Version="10.23.0" />
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

## Understanding OnCall

The `OnCall` method is the core API for configuring stub behavior. It provides two syntaxes: value-based for simple cases and callback-based for dynamic behavior.

### OnCall with Values - Simple Return Values

When your method needs to return a fixed value, use the value overload. KnockOff generates an `OnCall(TReturn value)` overload for all methods that return values:

<!-- snippet: getting-started-value-overloads -->
```cs
[Fact]
public void GetById_ValueOverload_SimplerSyntax()
{
    var stub = new UserRepoStub();

    // Value overload - pass the return value directly
    stub.GetById.OnCall(new User { Id = 1, Name = "Alice" });

    // Callback syntax - use when you need argument-based logic
    stub.GetById.OnCall((id) => new User { Id = id, Name = "Dynamic" });

    IUserRepo repository = stub;
    var user = repository.GetById(1);

    Assert.Equal("Dynamic", user!.Name);
}
```
<!-- endSnippet -->

**Key benefits of OnCall(value)**:
- Simpler syntax when you don't need dynamic logic
- Still returns a tracking object for verification
- Works with async methods (auto-wraps in Task.FromResult)

### OnCall with Callbacks - Dynamic Behavior

When you need to compute values based on arguments, perform side effects, or implement conditional logic, use the callback overload:

```cs
// Use VALUE when returning a fixed result
stub.GetById.OnCall(new User { Id = 1, Name = "Alice" });

// Use CALLBACK when you need:
// - Dynamic values based on arguments
// - Side effects
// - Conditional logic
stub.GetById.OnCall((id) => id > 100 ? adminUser : regularUser);

// Both return tracking objects for verification
```

**When to use callbacks**:
- Computing return values based on input arguments
- Implementing conditional logic
- Tracking or validating argument values
- Performing side effects (like updating test state)

### Properties - OnGet/OnSet

Properties use `OnGet` for getters and `OnSet` for setters. Both support value and callback overloads:

<!-- snippet: getting-started-property-configuration -->
```cs
[Fact]
public void Property_OnGetAndOnSet()
{
    var stub = new UserConfigStub();

    // OnGet - configure what the getter returns
    stub.CurrentUser.OnGet(new User { Id = 1, Name = "Alice" });

    // OnSet - track or validate setter calls
    User? capturedUser = null;
    stub.CurrentUser.OnSet((user) => capturedUser = user);

    IUserConfig config = stub;

    // Reading uses OnGet
    var user = config.CurrentUser;
    Assert.Equal("Alice", user!.Name);

    // Writing uses OnSet
    config.CurrentUser = new User { Id = 2, Name = "Bob" };
    Assert.Equal("Bob", capturedUser!.Name);
}
```
<!-- endSnippet -->

### Async Methods - Auto-Wrapping

For async methods returning `Task<T>` or `ValueTask<T>`, KnockOff automatically handles the async wrapping - both for value overloads and callbacks.

#### Value Overload (Simplest)

<!-- snippet: getting-started-async-wrapping -->
```cs
[Fact]
public async Task AsyncMethod_ValueAutoWrapped()
{
    var stub = new AsyncUserRepoStub();

    // Value overload - KnockOff wraps in Task.FromResult automatically
    stub.GetUserAsync.OnCall(new User { Id = 1, Name = "Alice" });

    IAsyncUserRepo repository = stub;
    var user = await repository.GetUserAsync(1);

    Assert.Equal("Alice", user!.Name);
}
```
<!-- endSnippet -->

#### Simplified Callbacks

When you need callback logic but don't need actual async operations, return the inner type directly - KnockOff auto-wraps the result:

<!-- snippet: async-task-simplified-callback -->
```cs
[Fact]
public async Task TaskResult_SimplifiedCallback_AutoWraps()
{
    var stub = new AsyncUserSvcStub();

    // SIMPLIFIED CALLBACK: Return the unwrapped type, auto-wrapped in Task.FromResult
    // This combines the simplicity of value overloads with callback flexibility
    stub.GetUserAsync.OnCall((id) => new User { Id = id, Name = "Alice" }).Verifiable();

    IAsyncUserSvc service = stub;
    var user = await service.GetUserAsync(42);

    Assert.NotNull(user);
    Assert.Equal("Alice", user.Name);
    stub.Verify();
}
```
<!-- endSnippet -->

#### Void Async Methods

For `Task` or `ValueTask` methods (no return value), use `Action` callbacks - KnockOff auto-returns `Task.CompletedTask`:

<!-- snippet: async-task-simplified-void -->
```cs
[Fact]
public async Task TaskVoid_SimplifiedCallback_AutoReturnsCompletedTask()
{
    var stub = new AsyncUserSvcStub();

    var updatedUsers = new List<User>();

    // SIMPLIFIED VOID CALLBACK: Just use Action, Task.CompletedTask is auto-returned
    stub.UpdateUserAsync.OnCall((user) => updatedUsers.Add(user)).Verifiable();

    IAsyncUserSvc service = stub;
    await service.UpdateUserAsync(new User { Id = 1, Name = "Bob" });

    Assert.Single(updatedUsers);
    stub.Verify();
}
```
<!-- endSnippet -->

You don't need to manually wrap values in `Task.FromResult` or return `Task.CompletedTask` - KnockOff handles this for you.

### Decision Guide: Value vs Callback

| Syntax | Use When | Example |
|--------|----------|---------|
| `.OnCall(value)` | Returning a fixed value | `stub.GetStatus.OnCall("OK")` |
| `.OnCall(callback)` | Computing values from arguments | `stub.GetUser.OnCall((id) => users[id])` |
| `.OnCall(callback)` | Conditional logic | `stub.IsValid.OnCall((x) => x > 0)` |
| `.OnCall(callback)` | Side effects or tracking | `stub.Save.OnCall((u) => saved.Add(u))` |

**Important**: Both syntaxes return tracking objects, so you can verify calls regardless of which you use.

## Understanding Generated Code

### Where to Find Generated Files

KnockOff outputs generated code to your project's `Generated/` folder at the project root. You can view these files in your IDE:

- **Visual Studio**: Expand Dependencies → Analyzers → KnockOff.SourceGenerator
- **Rider**: Navigate to the Generated folder in the project structure
- **File System**: Look in the `Generated/` folder within your test project directory

Generated files are committed to source control so you can track changes in diffs and PRs.

### What Gets Generated

For each stub, KnockOff generates:

1. **Explicit interface implementations** - Every interface member is implemented explicitly
2. **Interceptor classes** - Per-member classes that track calls, arguments, and return values
3. **Interceptor properties** - Public properties for each interface member (e.g., `GetById`, `SaveUser`) that expose the interceptor API

The generated code is readable C# that mirrors your interface structure. You can review it in the `Generated/` folder to understand how KnockOff implements your stub.

## Next Steps

Now that you've created your first stubs, explore more features:

- **[Stub Patterns](guides/stub-patterns.md)** - Learn about all three stub patterns (Stand-Alone, Inline Interface, Inline Class)
- **[Methods](guides/methods.md)** - Configure method behavior with OnCall, track arguments, handle async methods
- **[Properties](guides/properties.md)** - Use OnGet/OnSet for properties, track access, configure backing values
- **[Interceptor API Reference](reference/interceptor-api.md)** - Complete reference for the interceptor API

---

**UPDATED:** 2026-01-27
