# KnockOff

**No more `Arg.Any<>()`. No more `It.IsAny<>()`. Just write C#.**

[![NuGet](https://img.shields.io/nuget/v/KnockOff.svg)](https://www.nuget.org/packages/KnockOff/)
[![Build Status](https://github.com/NeatooDotNet/KnockOff/workflows/Build,%20Test%20&%20Publish/badge.svg)](https://github.com/NeatooDotNet/KnockOff/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

---

## The Difference

**NSubstitute:**
```csharp
var repo = Substitute.For<IUserRepo>();
repo.GetUser(Arg.Is<int>(id => id > 0)).Returns(x => new User { Id = x.Arg<int>() });
```

**KnockOff:**
```csharp
var stub = new UserRepoStub();
stub.GetUser.OnCall((id) => id > 0 ? new User { Id = id } : null);
```

No `Arg.Is<>()`. No `x.Arg<int>()`. The parameter is just `id`.

---

## Method Overload Resolution

**The Problem:** When an interface has overloaded methods with the same parameter count but different types:

<!-- snippet: readme-method-overload-interface -->
```cs
public interface IFormatter
{
    string Format(string input, bool uppercase);
    string Format(string input, int maxLength);
}
```
<!-- endSnippet -->

**NSubstitute** - Must use `Arg.Any<T>()` to match any value:
```csharp
var formatter = Substitute.For<IFormatter>();

// Arg.Any<T>() required - compiler needs the types to resolve overload
formatter.Format(Arg.Any<string>(), Arg.Any<bool>()).Returns("bool overload");
formatter.Format(Arg.Any<string>(), Arg.Any<int>()).Returns("int overload");

// Specific value matching - literals work when all args are specific
formatter.Format("test", true).Returns("UPPERCASE");
formatter.Format("test", 10).Returns("truncated");

// To use argument values, extract from CallInfo:
formatter.Format(Arg.Any<string>(), Arg.Any<bool>())
    .Returns(x => x.ArgAt<bool>(1) ? x.ArgAt<string>(0).ToUpper() : x.ArgAt<string>(0));
```

**KnockOff** - Just write C# with typed parameters:

<!-- snippet: readme-method-overload-knockoff -->
```cs
[Fact]
public void OnCall_WithExplicitParameterTypes_ResolvesOverload()
{
    var stub = new FormatterStub();

    // Explicit parameter types resolve the overload - standard C# syntax
    stub.Format.OnCall((string input, bool uppercase) => "bool overload");
    stub.Format.OnCall((string input, int maxLength) => "int overload");

    IFormatter formatter = stub;

    // Each overload is correctly configured
    Assert.Equal("bool overload", formatter.Format("test", true));
    Assert.Equal("int overload", formatter.Format("test", 10));
}
```
<!-- endSnippet -->

<!-- snippet: readme-method-overload-when -->
```cs
[Fact]
public void When_WithLiteralValues_ResolvesOverload()
{
    var stub = new FormatterStub();

    // Specific value matching - parameter types resolve the overload
    stub.Format.When("test", true).Returns("UPPERCASE");
    stub.Format.When("test", 10).Returns("truncated");

    IFormatter formatter = stub;

    Assert.Equal("UPPERCASE", formatter.Format("test", true));
    Assert.Equal("truncated", formatter.Format("test", 10));
}
```
<!-- endSnippet -->

<!-- snippet: readme-method-overload-argument-access -->
```cs
[Fact]
public void OnCall_ArgumentsDirectlyAvailable_WithNamesAndTypes()
{
    var stub = new FormatterStub();

    // Arguments are directly available with names and types:
    stub.Format.OnCall((string input, bool uppercase) => uppercase ? input.ToUpper() : input);

    IFormatter formatter = stub;

    Assert.Equal("HELLO", formatter.Format("hello", true));
    Assert.Equal("hello", formatter.Format("hello", false));
}
```
<!-- endSnippet -->

**The Difference:**
- NSubstitute: `Arg.Any<bool>()` + `x.ArgAt<bool>(1)` to match any value and access arguments
- KnockOff: `(string input, bool uppercase)` - standard C# lambda with named, typed parameters

---

## Unique Feature: Source Delegation

Delegate to a real implementation, override only what you need:

```csharp
var realRepo = new SqlUserRepository(connectionString);
var stub = new UserRepoStub();

stub.Source(realRepo);  // ALL methods delegate to real implementation

// Override just the method you're testing
stub.GetUser.OnCall((id) => new User { Id = id, Name = "Test User" });

IUserRepo repo = stub;
repo.Save(user);     // Calls real SqlUserRepository.Save()
repo.GetUser(1);     // Returns test data
```

No other mocking framework has this. Perfect for integration tests, decorator patterns, and partial mocking without complexity.

---

## Side-by-Side Comparisons

### Methods

| Task | NSubstitute | KnockOff |
|------|-------------|----------|
| **Return value** | `calc.Add(1, 2).Returns(3);` | `stub.Add.Returns(3);` |
| **Any argument** | `calc.Add(Arg.Any<int>(), Arg.Any<int>()).Returns(10);` | `stub.Add.Returns(10);` |
| **Match values** | `calc.Add(1, 2).Returns(100);` | `stub.Add.When(1, 2).Returns(100);` |
| **Conditional** | `calc.Add(Arg.Any<int>(), Arg.Any<int>()).Returns(x => ...);` | `stub.Add.OnCall((a, b) => a > 0 ? a + b : 0);` |
| **Throw** | `calc.Add(Arg.Any<int>(), Arg.Any<int>()).Throws<Exception>();` | `stub.Add.OnCall((a, b) => throw new Exception());` |
| **Callback** | `calc.Add(Arg.Any<int>(), Arg.Any<int>()).Returns(3).AndDoes(x => ...);` | `stub.Add.OnCall((a, b) => { log.Add(a); return 3; });` |
| **Sequence** | `calc.Add(1, 2).Returns(1, 2, 3);` | `stub.Add.Returns(1, 2, 3);` |
| **Async** | `repo.GetUserAsync(1).Returns(user);` | `stub.GetUserAsync.Returns(user);` |
| **Verify called** | `calc.Received().Add(1, 2);` | `stub.Add.Verify();` |
| **Verify count** | `calc.Received(3).Add(Arg.Any<int>(), Arg.Any<int>());` | `stub.Add.Verify(Times.Exactly(3));` |

### Argument Matching

```csharp
// NSubstitute - Arg.Is<T> per parameter (permanent matchers)
calc.Add(Arg.Is<int>(a => a > 0), Arg.Any<int>()).Returns(100);

// KnockOff - OnCall with conditional (permanent, matches all calls)
stub.Add.OnCall((a, b) => a > 0 ? 100 : 0);

// KnockOff - When() for sequential matching (first match returns 100, then falls through)
stub.Add.When((a, b) => a > 0).Returns(100).ThenCall((a, b) => a + b);

// Multiple specific values
calc.Add(1, 2).Returns(100);
calc.Add(3, 4).Returns(200);

stub.Add.When(1, 2).Returns(100);
stub.Add.When(3, 4).Returns(200);
```

**Note:** NSubstitute's matchers are permanent—they match all qualifying calls. KnockOff's `When()` is sequential—matchers are consumed in order. Use `OnCall()` with conditionals for permanent matching behavior.

### Argument Capture

```csharp
// NSubstitute - requires Arg.Do in setup
int capturedA = 0, capturedB = 0;
calc.Add(Arg.Do<int>(x => capturedA = x), Arg.Do<int>(x => capturedB = x));
calc.Add(1, 2);

// KnockOff - built-in, no pre-setup
var tracking = stub.Add.OnCall((a, b) => a + b);
calc.Add(1, 2);
var (a, b) = tracking.LastArgs;  // Named tuple: a = 1, b = 2
```

### Properties

| Task | NSubstitute | KnockOff |
|------|-------------|----------|
| **Setup getter** | `calc.Mode.Returns("Scientific");` | `stub.Mode.OnGet("Scientific");` |
| **Setup setter** | `calc.When(x => x.Mode = Arg.Any<string>()).Do(x => ...);` | `stub.Mode.OnSet((v) => captured = v);` |
| **Verify getter** | `_ = calc.Received().Mode;` | `stub.Mode.VerifyGet();` |
| **Verify setter** | `calc.Received().Mode = "Scientific";` | `stub.Mode.VerifySet();` |
| **Verify count** | `_ = calc.Received(3).Mode;` | `stub.Mode.VerifyGet(Times.Exactly(3));` |
| **Capture value** | `calc.When(x => x.Mode = Arg.Do<string>(v => ...)).Do(...);` | `stub.Mode.LastSetValue` (built-in) |

### Events

| Task | NSubstitute | KnockOff |
|------|-------------|----------|
| **Raise event** | `calc.PoweringUp += Raise.Event();` | `stub.PoweringUp.Raise(stub, EventArgs.Empty);` |
| **Raise with args** | `calc.PoweringUp += Raise.EventWith(sender, args);` | `stub.PoweringUp.Raise(sender, args);` |
| **Verify subscription** | *(not available)* | `stub.PoweringUp.VerifyAdd(Times.Once);` |
| **Verify unsubscription** | *(not available)* | `stub.PoweringUp.VerifyRemove(Times.Once);` |
| **Check subscribers** | *(not available)* | `stub.PoweringUp.HasSubscribers` |

### Delegates

| Task | NSubstitute | KnockOff |
|------|-------------|----------|
| **Setup** | `factory(Arg.Any<int>()).Returns("result");` | `stub.Interceptor.Returns("result");` |
| **With logic** | `factory(Arg.Is<int>(x => x > 0)).Returns(x => $"val: {x.Arg<int>()}");` | `stub.Interceptor.OnCall((x) => $"val: {x}");` |
| **Verify** | `factory.Received()(42);` | `stub.Interceptor.Verify();` |
| **Capture** | *(manual with Arg.Do)* | `stub.Interceptor.LastCallArg` (built-in) |

### Indexers

| Task | NSubstitute | KnockOff |
|------|-------------|----------|
| **Setup getter** | `dict["key"].Returns(42);` | `stub.Indexer.Backing["key"] = 42;` |
| **Dynamic getter** | `dict[Arg.Any<string>()].Returns(0);` | `stub.Indexer.OnGet((key) => 0);` |
| **Verify getter** | `_ = dict.Received()["key"];` | `stub.Indexer.VerifyGet();` |
| **Verify setter** | `dict.Received()["key"] = 42;` | `stub.Indexer.VerifySet();` |
| **Capture** | *(manual with When/Do)* | `stub.Indexer.LastSetEntry` |

---

## Feature Parity

KnockOff covers the features NSubstitute users expect:

| Feature | KnockOff | NSubstitute |
|---------|----------|-------------|
| **Returns** | `Returns(value)` | `.Returns(value)` |
| **Returns with logic** | `OnCall((args) => value)` | `.Returns(x => value)` |
| **Argument matching** | `When(args).Returns(value)` | `Arg.Is<T>()` per parameter |
| **Sequences** | `Returns(v1, v2, v3)` | `.Returns(v1, v2, v3)` |
| **Callbacks** | Built into `OnCall` | `.AndDoes(callback)` |
| **Throws** | `OnCall(() => throw ...)` | `.Throws<T>()` |
| **Async methods** | Auto-wrapped | Auto-wrapped |
| **Properties** | `OnGet` / `OnSet` | `.Returns` / assignment |
| **Indexers** | `Indexer.OnGet` / `OnSet` / `Backing` | Assignment |
| **Events** | `Raise()` / `VerifyAdd` / `VerifyRemove` | `Raise.Event()` |
| **Delegates** | `Interceptor.OnCall` | Setup on substitute |
| **Verification** | `.Verify(Times)` | `.Received(n)` |
| **Batch verification** | `.Verifiable()` + `stub.Verify()` | Individual `.Received()` calls |
| **Strict mode** | `[KnockOff(Strict=true)]` | Configure substitute |

---

## What KnockOff Does Better

| Feature | Why It's Better |
|---------|-----------------|
| **Parameter matching** | `When((a, b) => a > 0)` matches all params at once vs `Arg.Is<>` per param |
| **Named tuple capture** | `var (a, b) = tracking.LastArgs` vs manual `Arg.Do<>` setup |
| **Source delegation** | Delegate to real implementation, override specific methods |
| **Event verification** | `VerifyAdd()` / `VerifyRemove()` / `HasSubscribers` |
| **Explicit Get/Set verify** | `VerifyGet(Times)` / `VerifySet(Times)` |
| **Built-in capture** | `LastArg`, `LastArgs`, `LastSetValue`, `LastSetEntry` |
| **Reusable stub classes** | Define once, customize per-test |

---

## Quick Start

### Install

```bash
dotnet add package KnockOff
```

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

### Configure and Verify

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

## Three Stub Patterns

**Standalone** - Reusable across your project:
```csharp
[KnockOff]
public partial class UserRepoStub : IUserRepo { }
```

**Inline Interface** - Test-local stubs:
```csharp
[KnockOff<IUserRepo>]
public partial class MyTests
{
    [Fact]
    public void Test()
    {
        var stub = new Stubs.IUserRepo();
    }
}
```

**Inline Class** - Stub virtual members:
```csharp
[KnockOff<MyService>]
public partial class MyTests
{
    [Fact]
    public void Test()
    {
        var stub = new Stubs.MyService();
        IMyService service = stub.Object;
    }
}
```

---

## Documentation

- **[Getting Started](docs/getting-started.md)** - Installation and first stub
- **[Stub Patterns](docs/guides/stub-patterns.md)** - Standalone, inline interface, inline class
- **[Interceptor API](docs/reference/interceptor-api.md)** - Complete `OnCall`, `OnGet`, `OnSet` reference
- **[Source Delegation](docs/guides/source-delegation.md)** - Delegate to real implementations
- **[Migration from Moq](docs/migration/from-moq.md)** - Step-by-step migration guide
- **[Migration from NSubstitute](docs/migration/from-nsubstitute.md)** - Comparison and migration guide

---

## License

MIT License. See [LICENSE](LICENSE) for details.

---

## Contributing

Contributions welcome! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

- **Issues**: [GitHub Issues](https://github.com/NeatooDotNet/KnockOff/issues)
- **Pull Requests**: Bug fixes, features, documentation
- **Discussions**: [GitHub Discussions](https://github.com/NeatooDotNet/KnockOff/discussions)
