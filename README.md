# KnockOff

**Reusable test stubs that work across your entire project**

[![NuGet](https://img.shields.io/nuget/v/KnockOff.svg)](https://www.nuget.org/packages/KnockOff/)
[![Build Status](https://github.com/NeatooDotNet/KnockOff/workflows/Build,%20Test%20&%20Publish/badge.svg)](https://github.com/NeatooDotNet/KnockOff/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

---

## The Problem

Creating test doubles is tedious when every test file needs its own configuration. Runtime mocking frameworks require setup code in each test, making it hard to share stubs across your project. When you need the same stub behavior in multiple tests, you end up duplicating setup logic or creating complex test fixtures. Changing shared behavior means updating code in multiple places.

---

## The Solution

KnockOff uses Roslyn source generation to create reusable stub classes that live in your test project. Define a stub once, then use it across all your tests—each test can configure behavior differently using the same stub instance. Change default behavior in one place, or override it per-test when needed. Share stubs across test files while keeping test-specific customization simple.

**Moq (runtime reflection):**

<!-- snippet: readme-teaser-moq -->

**KnockOff (compile-time):**

<!-- snippet: readme-teaser-knockoff -->

---

## Key Features

- **Shared stubs**: Define once, reuse across all tests—each test customizes behavior as needed
- **Per-test configuration**: Override default stub behavior in individual tests without affecting others
- **Three stub patterns**: Stand-alone classes (`[KnockOff]`), inline interface stubs (`[KnockOff<IFoo>]`), and inline class stubs (`[KnockOff<SomeClass>]`)
- **Interceptor API**: Configure behavior with `OnCall`, `OnGet`, `OnSet`, and `Value` properties
- **Smart defaults**: Methods return `default(T)`, properties auto-initialize collections
- **Source delegation**: Use `Source(T)` to delegate stub behavior to real implementations
- **Zero reflection**: Generated code contains no runtime reflection
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
| **Stub reusability** | Define once, share across project | Per-test setup required | Per-test setup required |
| **Default behavior** | Configured in stub class | Repeated in each test | Repeated in each test |
| **Per-test override** | Simple interceptor assignment | Full re-setup needed | Full re-setup needed |
| **Setup method** | Source generation | Runtime reflection | Runtime dynamic proxy |
| **Performance** | Zero reflection overhead | Expression compilation | Dynamic proxy overhead |
| **Learning curve** | Explicit interceptor API | Fluent expression API | Fluent API |
| **Generated code** | Visible in project | Hidden | Hidden |

**When to use KnockOff:**
- You need the same stub in multiple test files with different configurations
- You want to define default stub behavior once and override it per-test
- You value explicit, discoverable stub classes over per-test mock setup
- You want to eliminate duplicate stub configuration across your test suite

**When to use Moq/NSubstitute:**
- You prefer fluent setup APIs
- Your tests rarely share stub implementations
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
