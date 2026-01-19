# KnockOff

**Compile-time test stubs with zero runtime reflection**

[![NuGet](https://img.shields.io/nuget/v/KnockOff.svg)](https://www.nuget.org/packages/KnockOff/)
[![Build Status](https://github.com/NeatooDotNet/KnockOff/workflows/Build,%20Test%20&%20Publish/badge.svg)](https://github.com/NeatooDotNet/KnockOff/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

---

## The Problem

Traditional mocking frameworks like Moq and NSubstitute use runtime reflection and expression trees, making test setup opaque to IDEs and slowing down test execution. You configure behavior through fluent APIs that hide implementation details, making it harder to understand what your stubs actually do. When tests fail, you're left debugging through layers of dynamic proxies instead of reading straightforward code.

---

## The Solution

KnockOff uses Roslyn source generation to create test stubs at compile time. You define stubs as partial classes, configure behavior through interceptor properties, and get full IntelliSense support. The generated code is visible, debuggable, and contains zero reflection.

**Moq (runtime reflection):**

<!-- snippet: readme-teaser-moq -->

**KnockOff (compile-time):**

<!-- snippet: readme-teaser-knockoff -->

---

## Key Features

- **Three stub patterns**: Stand-alone classes (`[KnockOff]`), inline interface stubs (`[KnockOff<IFoo>]`), and inline class stubs (`[KnockOff<SomeClass>]`)
- **Interceptor API**: Configure behavior with `OnCall`, `OnGet`, `OnSet`, and `Value` properties
- **Smart defaults**: Methods return `default(T)`, properties auto-initialize collections
- **Source delegation**: Use `Source(T)` to delegate stub behavior to real implementations
- **Compile-time safety**: All stub configuration is type-checked by the compiler
- **Zero reflection**: Generated code contains no runtime reflection or dynamic proxies
- **Full IntelliSense**: Navigate, refactor, and debug generated code like any other class
- **Verification support**: Track call counts, inspect arguments, and assert call sequences

---

## Quick Start

### Installation

<!-- snippet: readme-quickstart-install -->
```cs
// Install KnockOff via .NET CLI:
// dotnet add package KnockOff
//
// Or via Package Manager:
// Install-Package KnockOff
```
<!-- endSnippet -->

### Create a Stub

<!-- snippet: readme-quickstart-stub -->
```cs
// 1. Define your interface
public interface IQuickStartRepo
{
    User? GetUser(int id);
}

// 2. Create a stub with [KnockOff] attribute
[KnockOff]
public partial class QuickStartRepoStub : IQuickStartRepo { }

// 3. Use the stub in tests
public class QuickStartCreateStubTests
{
    [Fact]
    public void CreateStub_IsReady()
    {
        var stub = new QuickStartRepoStub();

        // Stub is ready - implements IQuickStartRepo
        IQuickStartRepo repository = stub;
        Assert.NotNull(repository);
    }
}
```
<!-- endSnippet -->

### Configure Behavior

<!-- snippet: readme-quickstart-configure -->
```cs
[Fact]
public void ConfigureStub_WithOnCall()
{
    var stub = new QuickStartRepoStub();

    // Configure the GetUser method to return a specific user
    stub.GetUser.OnCall((ko, id) => new User { Id = id, Name = "Test User" });

    IQuickStartRepo repository = stub;
    var user = repository.GetUser(42);

    Assert.NotNull(user);
    Assert.Equal(42, user.Id);
    Assert.Equal("Test User", user.Name);
}
```
<!-- endSnippet -->

### Verify Calls

<!-- snippet: readme-quickstart-verify -->
```cs
[Fact]
public void VerifyCalls_WithTracking()
{
    var stub = new QuickStartRepoStub();
    var tracking = stub.GetUser.OnCall((ko, id) => new User { Id = id, Name = "Test" });

    IQuickStartRepo repository = stub;

    // Call the method
    var user = repository.GetUser(42);

    // Verify it was called
    Assert.True(stub.GetUser.WasCalled);
    Assert.Equal(1, stub.GetUser.CallCount);

    // Verify the argument via tracking
    Assert.Equal(42, tracking.LastArg);
}
```
<!-- endSnippet -->

---

## Documentation

- **[Getting Started](docs/getting-started.md)** - Installation and your first stub
- **[Stub Patterns](docs/guides/stub-patterns.md)** - Stand-alone, inline interface, and inline class patterns
- **[Interceptor API](docs/reference/interceptor-api.md)** - Complete reference for `OnCall`, `OnGet`, `OnSet`, and `Value`
- **[Source Delegation](docs/guides/source-delegation.md)** - Delegate stub behavior to real implementations
- **[Migration from Moq](docs/migration/from-moq.md)** - Step-by-step guide for migrating existing tests

---

## Why KnockOff?

| Feature | KnockOff | Moq | NSubstitute |
|---------|----------|-----|-------------|
| **Setup method** | Compile-time (source generator) | Runtime (reflection) | Runtime (dynamic proxy) |
| **IntelliSense support** | Full | Limited (expression trees) | Limited (fluent API) |
| **Debuggability** | Step through generated code | Debug through proxies | Debug through proxies |
| **Performance** | Zero reflection overhead | Expression compilation | Castle DynamicProxy |
| **Type safety** | Compile-time errors | Runtime exceptions | Runtime exceptions |
| **Learning curve** | Explicit interceptor API | Fluent expression API | Fluent API |
| **Generated code visibility** | Visible in IDE | Hidden | Hidden |

**When to use KnockOff:**
- You want compile-time safety and full IDE support
- You value explicit, readable test setup over terse fluent APIs
- You need to debug stub behavior without fighting dynamic proxies
- You want zero runtime reflection in your test suite

**When to use Moq/NSubstitute:**
- You prefer fluent setup APIs
- You need runtime mock configuration
- You're working with a team already invested in those frameworks

---

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.

---

## Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

- **Issues**: Report bugs or request features via [GitHub Issues](https://github.com/NeatooDotNet/KnockOff/issues)
- **Pull Requests**: Submit PRs for bug fixes, features, or documentation improvements
- **Discussions**: Join the conversation in [GitHub Discussions](https://github.com/NeatooDotNet/KnockOff/discussions)
