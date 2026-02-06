# KnockOff

## Why I wrote KnockOff

I found many times I wanted to reuse my mocks. 
Especially in my integration test library where I may even register my mocks.
So, I found myself either copying my mock definitions or creating shared methods like this:


```csharp
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

But I found these methods quite unreadable and inflexible.

What I really wanted a shared stub:

```csharp
public class MyRepoStub(List<User> Users) : IMyRepo
{
    public User? GetUser(int id)
    {
        return Users.Single(u => u.Id == id);
    }

    public void Update_(User user)
    {
        Assert.Contains(user, Users);
    }
}
```

But that meant:

- Forced to implement all of the methods of the interface
- I didn't have any nice to haves like .Verify()

**So I created KnockOff**

For stand alone stubs KnockOff:

- Automatically implements all of the interface members
- Provides features like Verification, Returns and When
- Allows per-test configuration

With KnockOff the stub looks like:

``` csharp
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

And your test looks like

``` csharp
    [Fact]
    public void FetchTest_KnockOff()
    {
        var myRepoKO = new MyRepoStub([new User { Id = 1 }, new User { Id = 2 }]);
        var userDomainModel = new UserDomainModel(myRepoKO);

        Assert.True(userDomainModel.Fetch(1));

        // I have Verify on my Stub!
        myRepoKO.GetUser.Verify(Times.Once);
    }
```

And you don't actually loose the ability to configure per-test!

``` csharp
    [Fact]
    public void UpdateTest_KnockOff_OnCall()
    {
        var user = new User { Id = 1 };
        var myRepoKO = new MyRepoStub([user]);
        var userDomainModel = new UserDomainModel(myRepoKO);

        // OnCall overrides the stub methods
        myRepoKO.GetUser.OnCall(id => user).Verifiable();
        myRepoKO.Update.OnCall(u => Assert.Same(u, user)).Verifiable();

        userDomainModel.Fetch(1);
        userDomainModel.Update();

        myRepoKO.Verify();
    }
```


It also uses source generation so:

- No more `Arg.Any<>()`. No more `It.IsAny<>()`. Just write C#
- If the method signature changes you get a compile error
- There's a small performance gain but honestly it's negligible

**Now I have my stubs and mocks in one!**

Plus this is just the start. With source generation I think many more ideas are possible.
I've added a number of patterns (create a link).
And new features like Source (create a link).

**What other ideas do you have?**

## AI

With my ideas and guidance Claude Code has written the entirety of this library. 
What started as a curiosity has shown me the value of AI.
These are ideas I've had for years. I would not have been able to actually execute with AI.
Even more so in about a month!


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

### Any-Value Matching

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
| **Sequence** | `factory(Arg.Any<int>()).Returns(1, 2, 3);` | `stub.Interceptor.Returns(1, 2, 3);` |
| **Async** | `asyncOp(1).Returns(42);` | `stub.Interceptor.Returns(42);` (auto-wraps) |
| **Match values** | *(per-parameter Arg.Is)* | `stub.Interceptor.When(42).Returns("found");` |
| **Verify** | `factory.Received()(42);` | `stub.Interceptor.Verify();` |
| **Verify count** | `factory.Received(3)(Arg.Any<int>());` | `stub.Interceptor.Verify(Times.Exactly(3));` |
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
| **Delegates** | `Interceptor.Returns` / `OnCall` / `When` / `Verify` | Setup on substitute |
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
