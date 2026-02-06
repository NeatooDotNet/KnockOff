# KnockOff

A .NET mocking library that lets you define reusable stub classes — with full mocking capabilities built in. Powered by Roslyn source generation.

Define your test double once. Reuse it across your test project. Customize it per-test with OnCall, Returns, Verify, and When chains. No more copying mock setups between tests or maintaining shared factory methods full of `Arg.Any<>()`.

Claude Code was used to write this library. Skip to more [AI discussion](#ai).

[![NuGet](https://img.shields.io/nuget/v/KnockOff.svg)](https://www.nuget.org/packages/KnockOff/)
[![Build Status](https://github.com/NeatooDotNet/KnockOff/workflows/Build,%20Test%20&%20Publish/badge.svg)](https://github.com/NeatooDotNet/KnockOff/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)


## KnockOff Stub

Here's what a KnockOff stub looks like. There are [9 patterns](docs/guides/stub-patterns.md) total, but this is where it starts:

<!-- snippet: readme-knockoff-stub -->
```cs
[KnockOff]
public partial class MyRepoStub(List<User> Users) : IMyRepo
{
    protected override User? GetUser_(int id)
    {
        return Users.Single(u => u.Id == id);
    }

    protected override void Update_(User user)
    {
        Assert.Contains(user, Users);
    }
}
```
<!-- endSnippet -->

- **`[KnockOff]` + `partial class`** — KnockOff generates a base class that implements every member of `IMyRepo`. Your stub is a real class — define it once, reuse it across your entire test project. Pass it around, register it in DI, share it between test fixtures.
- **Constructor parameters** — `List<User> Users` is a primary constructor. Test data flows in naturally, just like any other C# class.
- **Overrides are optional** — `GetUser_` and `Update_` override the generated defaults. Only override what you need — everything else still works with [OnCall](docs/guides/methods.md), [Returns](docs/reference/interceptor-api.md), or [When chains](docs/guides/parameter-matching.md).
- **Compile-time safety** — If `IMyRepo` adds a method, the generated base class updates automatically. If a signature changes, your overrides produce compile errors — not silent runtime failures.

This stub is also a full mock. It has [Verify](docs/guides/verification.md), [Strict mode](docs/guides/strict-mode.md), [Async](docs/guides/async-patterns.md), and [Source Delegation](docs/guides/source-delegation.md) — all on the same reusable class.


## Why I Wrote KnockOff

I often wanted to reuse my mocks.
Especially in my integration test library where I may even register my mocks.
I found myself either copying my mock definitions code or creating shared methods like this:

**NSubstitute:**
<!-- snippet: readme-nsub-shared-mock -->
```cs
public static IMyRepo NSubstituteMock(List<User> users)
{
    var myRepoMock = Substitute.For<IMyRepo>();

    // Setup: configure GetUser to look up from the list based on id
    myRepoMock.GetUser(Arg.Any<int>())
        .Returns(callInfo => users.SingleOrDefault(u => u.Id == callInfo.Arg<int>()));

    // Setup: configure Update to assert user exists in list
    myRepoMock.When(x => x.Update(Arg.Any<User>()))
        .Do(callInfo => Assert.Contains(callInfo.Arg<User>(), users));

    return myRepoMock;
}
```
<!-- endSnippet -->

**Moq:**
```cs
public static Mock<IMyRepo> MoqMock(List<User> users)
{
    var mock = new Mock<IMyRepo>();

    mock.Setup(x => x.GetUser(It.IsAny<int>()))
        .Returns<int>(id => users.SingleOrDefault(u => u.Id == id));

    mock.Setup(x => x.Update(It.IsAny<User>()))
        .Callback<User>(u => Assert.Contains(u, users));

    return mock;
}
```

But I find that hard to read and unintuitive. Also, my shared methods accumulated extra parameters for variations across different tests.


## So I Created KnockOff

You can create a stub to implement [interfaces](docs/guides/stub-patterns.md) or non-sealed [classes](docs/guides/stub-patterns.md) with virtual methods.
Yet, you can still customize the stub per test.
All while having the features you would expect with a full mocking library.

With the stub above, your tests are:

<!-- snippet: readme-knockoff-fetch-test -->
```cs
var myRepoKO = new MyRepoStub([new User { Id = 1 }, new User { Id = 2 }]);
var userDomainModel = new UserDomainModel(myRepoKO);

Assert.True(userDomainModel.Fetch(1));

// I have Verify on my Stub!
myRepoKO.GetUser.Verify(Times.Once);
```
<!-- endSnippet -->

Need different behavior for a specific test? Override with OnCall:

<!-- snippet: readme-knockoff-oncall-test -->
```cs
var user1 = new User { Id = 1 }; // Ignored do to per-test configuration
var myRepoKO = new MyRepoStub([user1]);
var userDomainModel = new UserDomainModel(myRepoKO);

var user2 = new User { Id = 2 };

// When and OnCall overrides the stub methods
myRepoKO.GetUser.When(2).Returns(user2).Verifiable();
myRepoKO.Update.OnCall(u => Assert.Same(u, user2)).Verifiable();

userDomainModel.Fetch(2);
userDomainModel.Update();

myRepoKO.Verify();
```
<!-- endSnippet -->

**Now I have my stubs and mocks in one!**

---

## What KnockOff Does Better

| Feature | Why It's Better |
|---------|-----------------|
| **Reusable stub classes** | Define once, customize per-test. No more copying mock setups or shared factory methods. |
| **Source delegation** | Delegate to a real implementation, override only specific methods. [No equivalent in Moq or NSubstitute.](docs/guides/source-delegation.md) |
| **Compile-time safety** | Interface changes break your stubs at compile time, not at runtime 10 minutes into a test run. |
| **Parameter matching** | `OnCall((a, b) => a > 0 ? 100 : 0)` — standard C# conditionals instead of `Arg.Is<>` or `It.Is<>` per parameter. |
| **Built-in capture** | `LastArg`, `LastArgs`, `LastSetValue`, `LastSetEntry` — no manual `Arg.Do<>` or `Callback<>` setup. |
| **Event verification** | `VerifyAdd()` / `VerifyRemove()` / `HasSubscribers` — not available in Moq or NSubstitute. |
| **Explicit Get/Set verify** | `VerifyGet(Times)` / `VerifySet(Times)` for properties and indexers. |

---

## When KnockOff Shines

**Reusable test doubles** — Define a stub class once with constructor parameters for test data. Use it across hundreds of tests without copying setup code.

**Integration tests with DI** — Your stub is a real class. Register it in a DI container, pass it through constructors, use it anywhere a real implementation would go.

**Partial real implementations** — Use [Source Delegation](docs/guides/source-delegation.md) to delegate to a real implementation and override only the methods you're testing. Perfect for integration tests and decorator patterns.

**Large interfaces** — Stub only the methods you care about. Everything else auto-implements with defaults or can be configured with a single `Returns` call.

**Stubbing concrete classes** — Override virtual methods on non-sealed classes. KnockOff generates the stub with `.Object` to access the typed instance.

---

## Unique Feature: Source Delegation

`stub.Source(realImplementation)` delegates unconfigured method calls to a real object. Configured methods (OnCall, Returns, When) still take priority.

**The key: you don't need a complete implementation.** KnockOff generates a separate `Source()` overload for each interface in the hierarchy. Pass an object that implements any parent interface — only the matching methods get delegated.

For example, stub an interface that extends `IList<string>`:

<!-- snippet: source-hierarchy-interface -->
```cs
public interface IStepList : IList<string>
{
    void AddRange(IEnumerable<string> items);
}
```
<!-- endSnippet -->

Now pass a `List<string>` — it implements `IList<string>` but not `IStepList`:

<!-- snippet: source-hierarchy-partial -->
```cs
var realList = new List<string> { "step1", "step2", "step3" };

// List<string> doesn't implement IStepList, but it does implement IList<string>
// KnockOff delegates IList/ICollection/IEnumerable members to the real list
stub.Source(realList);

IStepList list = stub;

// These work — delegated to List<string>
Assert.Equal(3, list.Count);          // ICollection<T>.Count
Assert.Equal("step1", list[0]);       // IList<T> indexer
var items = new List<string>();
foreach (var item in list)            // IEnumerable<T>
{
    items.Add(item);
}
Assert.Equal(new[] { "step1", "step2", "step3" }, items);

// AddRange is NOT delegated — it's on IStepList, which List<string> doesn't implement
// Configure it explicitly, or it returns the smart default
stub.AddRange.OnCall((newItems) =>
{
    foreach (var newItem in newItems)
    {
        list.Add(newItem);
    }
});
```
<!-- endSnippet -->

Not available in Moq or NSubstitute. See the [full Source Delegation guide](docs/guides/source-delegation.md) for hierarchy details, priority order, and complete examples.

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

## The Difference

**Moq:**
```cs
mock.Setup(x => x.GetUser(It.Is<int>(id => id > 0)))
    .Returns<int>(id => new User { Id = id });
```

**NSubstitute:**
<!-- snippet: readme-hero-nsub -->
```cs
var repo = Substitute.For<IUserRepo>();
repo.GetUser(Arg.Is<int>(id => id > 0)).Returns(x => new User { Id = x.Arg<int>() });
```
<!-- endSnippet -->

**KnockOff:**
<!-- snippet: readme-hero-knockoff -->
```cs
var stub = new CompareUserRepoStub();
stub.GetUser.OnCall((id) => id > 0 ? new User { Id = id } : null);
```
<!-- endSnippet -->

No `It.Is<>()`. No `Arg.Is<>()`. No `x.Arg<int>()`. The parameter is just `id`.

---

## Side-by-Side Comparisons

### Methods

| Task | Moq | NSubstitute | KnockOff |
|------|-----|-------------|----------|
| **Return value** | `mock.Setup(x => x.Add(1, 2)).Returns(3);` | `calc.Add(1, 2).Returns(3);` | `stub.Add.Returns(3);` |
| **Any argument** | `mock.Setup(x => x.Add(It.IsAny<int>(), It.IsAny<int>())).Returns(10);` | `calc.Add(Arg.Any<int>(), Arg.Any<int>()).Returns(10);` | `stub.Add.Returns(10);` |
| **Match values** | `mock.Setup(x => x.Add(1, 2)).Returns(100);` | `calc.Add(1, 2).Returns(100);` | `stub.Add.When(1, 2).Returns(100);` |
| **Conditional** | `mock.Setup(x => x.Add(It.IsAny<int>(), It.IsAny<int>())).Returns<int, int>((a, b) => a > 0 ? a + b : 0);` | `calc.Add(Arg.Any<int>(), Arg.Any<int>()).Returns(x => ...);` | `stub.Add.OnCall((a, b) => a > 0 ? a + b : 0);` |
| **Throw** | `mock.Setup(x => x.Add(It.IsAny<int>(), It.IsAny<int>())).Throws<Exception>();` | `calc.Add(Arg.Any<int>(), Arg.Any<int>()).Throws<Exception>();` | `stub.Add.OnCall((a, b) => throw new Exception());` |
| **Callback** | `mock.Setup(x => x.Add(It.IsAny<int>(), It.IsAny<int>())).Returns(3).Callback<int, int>((a, b) => log.Add(a));` | `calc.Add(Arg.Any<int>(), Arg.Any<int>()).Returns(3).AndDoes(x => ...);` | `stub.Add.OnCall((a, b) => { log.Add(a); return 3; });` |
| **Sequence** | `mock.SetupSequence(x => x.Add(1, 2)).Returns(1).Returns(2).Returns(3);` | `calc.Add(1, 2).Returns(1, 2, 3);` | `stub.Add.Returns(1, 2, 3);` |
| **Async** | `mock.Setup(x => x.GetUserAsync(1)).ReturnsAsync(user);` | `repo.GetUserAsync(1).Returns(user);` | `stub.GetUserAsync.Returns(user);` |
| **Verify called** | `mock.Verify(x => x.Add(1, 2));` | `calc.Received().Add(1, 2);` | `stub.Add.Verify();` |
| **Verify count** | `mock.Verify(x => x.Add(It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(3));` | `calc.Received(3).Add(Arg.Any<int>(), Arg.Any<int>());` | `stub.Add.Verify(Times.Exactly(3));` |

### Argument Matching

**Moq:**
```cs
// Moq - It.Is<T> per parameter
mock.Setup(x => x.Add(It.Is<int>(a => a > 0), It.IsAny<int>())).Returns(100);
```

**NSubstitute:**
<!-- snippet: readme-argmatch-nsub-matchers -->
```cs
// NSubstitute - Arg.Is<T> per parameter (permanent matchers)
calc.Add(Arg.Is<int>(a => a > 0), Arg.Any<int>()).Returns(100);
```
<!-- endSnippet -->

**KnockOff:**
<!-- snippet: readme-argmatch-knockoff-oncall -->
```cs
// KnockOff - OnCall with conditional (permanent, matches all calls)
stub.Add.OnCall((a, b) => a > 0 ? 100 : 0);
```
<!-- endSnippet -->

<!-- snippet: readme-argmatch-knockoff-when -->
```cs
// KnockOff - When() for sequential matching (first match returns 100, then falls through)
stub.Add.When((a, b) => a > 0).Returns(100).ThenCall((a, b) => a + b);
```
<!-- endSnippet -->

**Multiple specific values:**

**Moq:**
```cs
mock.Setup(x => x.Add(1, 2)).Returns(100);
mock.Setup(x => x.Add(3, 4)).Returns(200);
```

<!-- snippet: readme-argmatch-nsub-specific -->
```cs
// Multiple specific values
calc.Add(1, 2).Returns(100);
calc.Add(3, 4).Returns(200);
```
<!-- endSnippet -->

<!-- snippet: readme-argmatch-knockoff-specific -->
```cs
stub.Add.When(1, 2).Returns(100);
stub.Add.When(3, 4).Returns(200);
```
<!-- endSnippet -->

**Note:** Moq and NSubstitute matchers are permanent — they match all qualifying calls. KnockOff's `When()` is sequential — matchers are consumed in order. Use `OnCall()` with conditionals for permanent matching behavior.

### Argument Capture

**Moq:**
```cs
// Moq - requires Callback setup
int capturedA = 0, capturedB = 0;
mock.Setup(x => x.Add(It.IsAny<int>(), It.IsAny<int>()))
    .Callback<int, int>((a, b) => { capturedA = a; capturedB = b; });
mock.Object.Add(1, 2);
```

**NSubstitute:**
<!-- snippet: readme-argcapture-nsub -->
```cs
// NSubstitute - requires Arg.Do in setup
int capturedA = 0, capturedB = 0;
calc.Add(Arg.Do<int>(x => capturedA = x), Arg.Do<int>(x => capturedB = x));
calc.Add(1, 2);
```
<!-- endSnippet -->

**KnockOff:**
<!-- snippet: readme-argcapture-knockoff -->
```cs
// KnockOff - built-in, no pre-setup
var tracking = stub.Add.OnCall((a, b) => a + b);
ICalculator calc = stub;
calc.Add(1, 2);
var (a, b) = tracking.LastArgs;  // Named tuple: a = 1, b = 2
```
<!-- endSnippet -->

For full comparisons of properties, events, delegates, and indexers, see the [complete comparison guide](docs/comparison.md).

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

### Any-Value Matching

**Moq:**
```cs
// It.IsAny<T>() required - compiler needs the types to resolve overload
mock.Setup(x => x.Format(It.IsAny<string>(), It.IsAny<bool>())).Returns("bool overload");
mock.Setup(x => x.Format(It.IsAny<string>(), It.IsAny<int>())).Returns("int overload");
```

**NSubstitute:**
<!-- snippet: readme-nsubstitute-any-value -->
```cs
// Arg.Any<T>() required - compiler needs the types to resolve overload
formatter.Format(Arg.Any<string>(), Arg.Any<bool>()).Returns("bool overload");
formatter.Format(Arg.Any<string>(), Arg.Any<int>()).Returns("int overload");
```
<!-- endSnippet -->

**KnockOff:**
<!-- snippet: readme-knockoff-any-value -->
```cs
// Explicit parameter types resolve the overload - standard C# syntax
stub.Format.OnCall((string input, bool uppercase) => "bool overload");
stub.Format.OnCall((string input, int maxLength) => "int overload");
```
<!-- endSnippet -->

### Specific-Value Matching

**NSubstitute:**
<!-- snippet: readme-nsubstitute-specific-value -->
```cs
// Specific value matching - literals work when all args are specific
formatter.Format("test", true).Returns("UPPERCASE");
formatter.Format("test", 10).Returns("truncated");
```
<!-- endSnippet -->

**KnockOff:**
<!-- snippet: readme-knockoff-specific-value -->
```cs
// Specific value matching - parameter types resolve the overload
stub.Format.When("test", true).Returns("UPPERCASE");
stub.Format.When("test", 10).Returns("truncated");
```
<!-- endSnippet -->

### Argument Access

**Moq:**
```cs
// To use argument values, extract via Returns<T1, T2>:
mock.Setup(x => x.Format(It.IsAny<string>(), It.IsAny<bool>()))
    .Returns<string, bool>((input, uppercase) => uppercase ? input.ToUpper() : input);
```

**NSubstitute:**
<!-- snippet: readme-nsubstitute-argument-access -->
```cs
// To use argument values, extract from CallInfo:
formatter.Format(Arg.Any<string>(), Arg.Any<bool>())
    .Returns(x => x.ArgAt<bool>(1) ? x.ArgAt<string>(0).ToUpper() : x.ArgAt<string>(0));
```
<!-- endSnippet -->

**KnockOff:**
<!-- snippet: readme-knockoff-argument-access -->
```cs
// Arguments are directly available with names and types:
stub.Format.OnCall((string input, bool uppercase) => uppercase ? input.ToUpper() : input);
```
<!-- endSnippet -->

**The Difference:**
- Moq: `It.IsAny<bool>()` + `.Returns<string, bool>((input, uppercase) => ...)` to match any value and access arguments
- NSubstitute: `Arg.Any<bool>()` + `x.ArgAt<bool>(1)` to match any value and access arguments
- KnockOff: `(string input, bool uppercase)` - standard C# lambda with named, typed parameters

---

## Three Stub Patterns

KnockOff supports [9 patterns](docs/guides/stub-patterns.md) total. Here are the three most common:

**Standalone** - Reusable across your project:
<!-- snippet: readme-pattern-standalone -->
```cs
[KnockOff]
public partial class ReadmeStandaloneStub : IUserRepo { }
```
<!-- endSnippet -->

**Inline Interface** - Test-local stubs:
<!-- snippet: readme-pattern-inline-interface -->
```cs
[Fact]
public void InlineInterface_Pattern()
{
    var stub = new Stubs.IUserRepo();
    stub.GetUser.OnCall((id) => new User { Id = id });

    IUserRepo repo = stub;
    Assert.NotNull(repo.GetUser(1));
}
```
<!-- endSnippet -->

**Inline Class** - Stub virtual members:
<!-- snippet: readme-pattern-inline-class -->
```cs
[Fact]
public void InlineClass_Pattern()
{
    var stub = new Stubs.MyService();
    stub.GetUser.OnCall((id) => new User { Id = id });

    MyService service = stub.Object;
    Assert.NotNull(service.GetUser(1));
}
```
<!-- endSnippet -->

---

## Roslyn Source Generation

KnockOff uses Roslyn source generation, which means:

- No more `Arg.Any<>()`. No more `It.IsAny<>()`. Just write C#
- If the method signature changes you get a compile error
- There's a small performance gain but honestly it's negligible

Source generation opens doors beyond traditional mocking — I've already added [9 patterns](docs/guides/stub-patterns.md) and features like [Source Delegation](docs/guides/source-delegation.md), with more ideas to come.

**What other ideas do you have?** Open a [discussion](https://github.com/NeatooDotNet/KnockOff/discussions).


## AI

This is an idea I've had for years but never took the time to implement. With my ideas and guidance, Claude Code has written the entirety of this library — the Roslyn source generator, the runtime library, the tests, and the documentation.

Source generation turned out to be a great fit for AI code generation. The work is highly patterned: analyze an interface, generate code for each member, handle edge cases across 9 patterns and 4 member types. That's exactly the kind of systematic, repetitive-but-varied work where AI excels. I designed the API and patterns; Claude Code implemented them across every combination.

### Claude Code Skill

KnockOff includes a [Claude Code skill](skills/knockoff/) that teaches Claude how to use the library. Copy the `skills/knockoff/` directory into your project and Claude Code will know how to create stubs, configure behavior, write tests with KnockOff, and migrate from Moq — without you explaining the API.

The skill includes slash commands:
- **`/knockoff:create-stub`** — Create a new stub class with the pattern of your choice
- **`/knockoff:migrate-from-moq`** — Convert existing Moq tests to KnockOff
- **`/knockoff:troubleshoot`** — Diagnose and fix common KnockOff issues

---

## Documentation

- **[Getting Started](docs/getting-started.md)** - Installation and first stub
- **[Stub Patterns](docs/guides/stub-patterns.md)** - Standalone, inline interface, inline class
- **[Interceptor API](docs/reference/interceptor-api.md)** - Complete `OnCall`, `OnGet`, `OnSet` reference
- **[Source Delegation](docs/guides/source-delegation.md)** - Delegate to real implementations
- **[Full Comparison Guide](docs/comparison.md)** - Properties, events, delegates, indexers vs Moq and NSubstitute
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
