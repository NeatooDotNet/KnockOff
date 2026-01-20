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
