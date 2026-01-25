# KnockOff

**Reusable test stubs that work across your entire project**

[![NuGet](https://img.shields.io/nuget/v/KnockOff.svg)](https://www.nuget.org/packages/KnockOff/)
[![Build Status](https://github.com/NeatooDotNet/KnockOff/workflows/Build,%20Test%20&%20Publish/badge.svg)](https://github.com/NeatooDotNet/KnockOff/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

---

## The Problem

Testing with mocks means repeating setup code in every test file. When multiple tests need the same stub behavior, you duplicate configuration or build complex test fixtures. Change that behavior? Update every test that uses it. Runtime mocking frameworks make sharing stubs across your project difficult.

---

## The Solution

KnockOff generates reusable stub classes you define once and share across your entire project. Each test configures the same stub instance differently—no duplicate setup code. Change default behavior in one place, or override per-test. The same stub class works in every test file that needs it.

**Moq (runtime reflection):**

<!-- snippet: readme-teaser-moq -->
```cs
[Fact]
public void Moq_RuntimeSetup()
{
    var mock = new Mock<IReadmeUserRepo>();
    mock.Setup(x => x.GetUser(It.IsAny<int>()))
        .Returns(new User { Id = 42, Name = "Test User" });

    IReadmeUserRepo repository = mock.Object;
    var user = repository.GetUser(42);

    Assert.NotNull(user);
    Assert.Equal("Test User", user.Name);
}
```
<!-- endSnippet -->

**KnockOff (source generation):**

<!-- snippet: readme-teaser-knockoff -->
```cs
[Fact]
public void KnockOff_CompileTimeSetup()
{
    var stub = new ReadmeUserRepoStub();
    stub.GetUser.OnCall((id) => new User { Id = id, Name = "Test User" });

    IReadmeUserRepo repository = stub;
    var user = repository.GetUser(42);

    Assert.NotNull(user);
    Assert.Equal("Test User", user.Name);
}
```
<!-- endSnippet -->

---

## Key Features

- **Shared stubs** - Define once, use across entire project with per-test customization
- **Source generation** - Zero reflection overhead, compile-time safety
- **Explicit configuration** - Clear interceptor API (`OnCall`, `OnGet`, `OnSet`, `Value`)
- **Verification** - Track call counts, arguments, and validate expected calls
- **Source delegation** - Delegate stub behavior to real implementations
- **Three stub patterns** - Stand-alone, inline interface, and inline class

---

## Quick Start

### Create a Stub

<!-- snippet: readme-quickstart-stub -->
```cs
public interface IQuickStartRepo
{
    User? GetUser(int id);
}

[KnockOff]
public partial class QuickStartRepoStub : IQuickStartRepo { }

public class QuickStartCreateStubTests
{
    [Fact]
    public void CreateStub_IsReady()
    {
        var stub = new QuickStartRepoStub();

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

    stub.GetUser.OnCall((id) => new User { Id = id, Name = "Test User" });

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
public void VerifyCalls_WithVerifiable()
{
    var stub = new QuickStartRepoStub();
    stub.GetUser.OnCall((id) => new User { Id = id, Name = "Test" }).Verifiable();

    IQuickStartRepo repository = stub;

    var user = repository.GetUser(42);

    // Verify() checks all members marked with .Verifiable()
    stub.Verify();
}
```
<!-- endSnippet -->

---

## Installation

Install via NuGet Package Manager:

```bash
dotnet add package KnockOff
```

Or via Package Manager Console:

```powershell
Install-Package KnockOff
```

See the [Getting Started Guide](docs/getting-started.md) for detailed setup instructions.

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
- Multiple test files need the same stub with different configurations per-test
- You want to eliminate duplicate setup code across your test suite
- You prefer explicit stub classes you can share and customize
- Changing shared behavior in one place matters to you

**When to use Moq/NSubstitute:**
- You prefer fluent setup APIs and are comfortable with per-test configuration
- Your tests rarely reuse the same stub across multiple files
- You're working with a team already invested in those frameworks

---

## Documentation

- **[Getting Started](docs/getting-started.md)** - Installation and your first stub
- **[Stub Patterns](docs/guides/stub-patterns.md)** - Stand-alone, inline interface, and inline class patterns
- **[Interceptor API](docs/reference/interceptor-api.md)** - Complete reference for `OnCall`, `OnGet`, `OnSet`, and `Value`
- **[Source Delegation](docs/guides/source-delegation.md)** - Delegate stub behavior to real implementations
- **[Migration from Moq](docs/migration/from-moq.md)** - Step-by-step guide for migrating existing tests
- **[Migration from NSubstitute](docs/migration/from-nsubstitute.md)** - Honest comparison and migration guide

---

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.

---

## Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

- **Issues**: Report bugs or request features via [GitHub Issues](https://github.com/NeatooDotNet/KnockOff/issues)
- **Pull Requests**: Submit PRs for bug fixes, features, or documentation improvements
- **Discussions**: Join the conversation in [GitHub Discussions](https://github.com/NeatooDotNet/KnockOff/discussions)
