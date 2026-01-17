# KnockOff

C# test stub source generator that catches configuration errors at compile time instead of runtime.

[![NuGet](https://img.shields.io/nuget/v/KnockOff.svg)](https://www.nuget.org/packages/KnockOff)
[![Build Status](https://github.com/NeatooDotNet/KnockOff/workflows/build/badge.svg)](https://github.com/NeatooDotNet/KnockOff/actions)

## Why KnockOff

**Compile-time safety** - Misspell a method name, forget to configure a dependency, or mismatch a type? Your code won't compile. No runtime surprises.

**Source generation** - Stubs are generated as regular C# code during compilation. Full IntelliSense support, no reflection overhead.

**Explicit syntax** - Configure behavior with callbacks and properties instead of fluent chains. Every interaction is visible and searchable.

**Verification built-in** - Call counts, argument capture, and call tracking are always available. No separate verification API to remember.

## KnockOff vs Moq

| Feature | Moq | KnockOff |
|---------|-----|----------|
| **When errors occur** | Runtime | **Compile-time** |
| **How it works** | Reflection + dynamic proxies | **Source generation** |
| **Setup syntax** | Fluent API with magic strings | **Callbacks + properties** |
| **Type safety** | `It.IsAny<T>()`, loose matching | **Fully typed, compiler-enforced** |
| **IntelliSense** | Limited (runtime types) | **Full support (generated code)** |
| **Verification** | Separate `Verify()` calls | **Always-on tracking** |

Both frameworks create test stubs. KnockOff trades runtime flexibility for compile-time guarantees.

## Installation

<!-- snippet: readme-installation -->
```cs
dotnet add package KnockOff
```
<sup><a href='/src/Tests/KnockOff.Documentation.Samples/ReadMe/ReadMeSamples.cs#L25-L27' title='Snippet source file'>snippet source</a> | <a href='#snippet-readme-installation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Quick Start

### Standalone Stub

Create a reusable stub by marking a partial class with `[KnockOff]`:

<!-- snippet: readme-quick-example-standalone -->
```cs
[KnockOff]
public partial class UserServiceStub : IUserService { }

// Usage in test:
// var stub = new UserServiceStub();
// stub.GetUser.OnCall((ko, id) => new User { Id = id, Name = "Test User" });
//
// IUserService service = stub;  // Implicit conversion
// var user = service.GetUser(42);
//
// Assert.Equal(42, stub.GetUser.LastArg);
// Assert.True(stub.GetUser.WasCalled);
```
<sup><a href='/src/Tests/KnockOff.Documentation.Samples/ReadMe/ReadMeSamples.cs#L40-L52' title='Snippet source file'>snippet source</a> | <a href='#snippet-readme-quick-example-standalone' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Inline Stub

Create test-local stubs with `[KnockOff<T>]` on your test class:

<!-- snippet: readme-quick-example-inline -->
```cs
[KnockOff<IEmailService>]
public partial class NotificationTests
{
	// [Fact]
	// public void SendsWelcomeEmail_WhenUserRegisters()
	// {
	//     var emailStub = new Stubs.IEmailService();
	//     emailStub.SendEmail.OnCall((ko, to, subject, body) => { });
	//
	//     IEmailService emailService = emailStub;
	//     var notificationService = new NotificationService(emailService);
	//
	//     notificationService.NotifyRegistration("user@example.com");
	//
	//     Assert.True(emailStub.SendEmail.WasCalled);
	//     Assert.Equal("user@example.com", emailStub.SendEmail.LastCallArgs?.to);
	// }
}
```
<sup><a href='/src/Tests/KnockOff.Documentation.Samples/ReadMe/ReadMeSamples.cs#L67-L85' title='Snippet source file'>snippet source</a> | <a href='#snippet-readme-quick-example-inline' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Verification

Every stub tracks calls automatically:

<!-- snippet: readme-quick-example-verification -->
```cs
// Check if method was called
// Assert.True(stub.SaveUser.WasCalled);
//
// Check call count
// Assert.Equal(3, stub.SaveUser.CallCount);
//
// Capture arguments (single parameter)
// Assert.Equal(42, stub.GetUser.LastArg);
//
// Capture arguments (multiple parameters - named tuple)
// var args = stub.SendEmail.LastCallArgs;
// Assert.Equal("user@example.com", args?.to);
// Assert.Equal("Welcome!", args?.subject);
```
<sup><a href='/src/Tests/KnockOff.Documentation.Samples/ReadMe/ReadMeSamples.cs#L92-L105' title='Snippet source file'>snippet source</a> | <a href='#snippet-readme-quick-example-verification' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Documentation

- **[Getting Started](docs/getting-started.md)** - Your first stub in 5 minutes
- **[Guides](docs/guides/)** - Stub patterns, methods, properties, events, generics, verification, strict mode
- **[KnockOff vs Moq](docs/knockoff-vs-moq.md)** - Detailed comparison and when to choose KnockOff
- **[Reference](docs/reference/)** - Complete API reference, attributes, diagnostics

## When NOT to Use KnockOff

- **You need runtime stub generation** - KnockOff generates stubs at compile time. If you need to create stubs dynamically at runtime (e.g., proxying unknown types), use Moq.
- **You prefer fluent setup syntax** - KnockOff uses callbacks and properties instead of `Setup().Returns()` chains. If you strongly prefer fluent syntax, stick with Moq.
- **Your build process can't run source generators** - Some legacy build systems don't support Roslyn source generators.

## License

MIT License - see [LICENSE](LICENSE) for details.

## Complete Example

Here's a complete working test showing all three patterns:

```csharp
using Xunit;

public interface IUserService
{
    User? GetUser(int id);
    void SaveUser(User user);
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

// Standalone stub - reusable across tests
[KnockOff]
public partial class UserServiceStub : IUserService { }

// Inline stub - scoped to test class
[KnockOff<IEmailService>]
public partial class UserServiceTests
{
    [Fact]
    public void GetUser_ReturnsConfiguredUser()
    {
        var stub = new UserServiceStub();
        stub.GetUser.OnCall((ko, id) => new User { Id = id, Name = "Test" });

        IUserService service = stub;
        var user = service.GetUser(42);

        Assert.NotNull(user);
        Assert.Equal(42, user.Id);
        Assert.Equal(42, stub.GetUser.LastArg);
        Assert.True(stub.GetUser.WasCalled);
    }

    [Fact]
    public void SaveUser_TracksCallCount()
    {
        var stub = new UserServiceStub();
        IUserService service = stub;

        service.SaveUser(new User { Id = 1 });
        service.SaveUser(new User { Id = 2 });
        service.SaveUser(new User { Id = 3 });

        Assert.Equal(3, stub.SaveUser.CallCount);
    }
}
```
