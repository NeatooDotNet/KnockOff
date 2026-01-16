# KnockOff

Source-generated test stubs with readable syntax. Less ceremony than Moq.

## Why KnockOff?

**Readable tests.** KnockOff replaces Moq's lambda-heavy syntax with direct property access. No more `Setup(x => x.Method(It.IsAny<int>()))` — just `stub.Method.OnCall = ...`.

**Two ways to customize.** Define default behavior in your stub class (compile-time), then override per-test with callbacks (runtime). This "duality pattern" keeps tests focused on what's different, not what's common.

**Compile-time safety.** When interfaces change, KnockOff stubs fail to compile. No more green builds with tests that explode at runtime.

## Quick Comparison

| Task | Moq | KnockOff |
|------|-----|----------|
| Return a value | `mock.Setup(x => x.GetUser(It.IsAny<int>())).Returns(user)` | `stub.GetUser.OnCall = (ko, id) => user` |
| Verify called once | `mock.Verify(x => x.Save(It.IsAny<Order>()), Times.Once)` | `Assert.Equal(1, stub.Save.CallCount)` |
| Check last argument | `Callback<Order>(o => captured = o)` then assert | `Assert.Equal(expected, stub.Save.LastCallArg)` |
| Property value | `mock.Setup(x => x.Name).Returns("Test")` | `stub.Name.Value = "Test"` |

## Installation

<!-- pseudo:readme-installation -->
```bash
dotnet add package KnockOff
```
<!-- /snippet -->

## Your First Stub

**1. Define an interface:**

<!-- snippet: readme-first-interface -->
```cs
public interface IUserRepository
{
    RmUser? GetById(int id);
    void Save(RmUser user);
}
```
<!-- endSnippet -->

**2. Create a stub:**

<!-- snippet: readme-first-stub -->
```cs
[KnockOff]
public partial class UserRepositoryStub : IUserRepository { }
```
<!-- endSnippet -->

**3. Use in tests:**

<!-- snippet: readme-first-test -->
```cs
public class FirstStubTests
{
    public void UpdatesUserName()
    {
        // Arrange
        var stub = new UserRepositoryStub();
        stub.GetById.OnCall = (ko, id) => new RmUser { Id = id, Name = "Original" };

        var service = new RmUserService(stub);

        // Act
        service.Rename(userId: 1, newName: "Updated");

        // Assert
        Assert.Equal(1, stub.Save.CallCount);
        Assert.Equal("Updated", stub.Save.LastCallArg?.Name);
    }
}
```
<!-- endSnippet -->

## Side-by-Side: Moq vs KnockOff

**Moq:**

<!-- snippet: readme-side-by-side-moq -->
```cs
[Fact]
public void OrderProcessor_ProcessesValidOrder_Moq()
{
    // Arrange
    var mock = new Mock<IOrderService>();
    mock.Setup(x => x.GetOrder(It.IsAny<int>()))
        .Returns((int id) => new Order { Id = id, CustomerId = 1 });
    mock.Setup(x => x.ValidateOrder(It.IsAny<Order>())).Returns(true);
    mock.Setup(x => x.CalculateTotal(It.IsAny<Order>())).Returns(100m);

    var sut = new OrderProcessor(mock.Object);

    // Act
    sut.Process(1);

    // Assert
    mock.Verify(x => x.GetOrder(1), Times.Once);
    mock.Verify(x => x.ValidateOrder(It.IsAny<Order>()), Times.Once);
    mock.Verify(x => x.SaveOrder(It.IsAny<Order>()), Times.Once);
}
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: readme-side-by-side-knockoff -->
```cs
[Fact]
public void OrderProcessor_ProcessesValidOrder_KnockOff()
{
    // Arrange
    var stub = new OrderServiceStub();
    stub.GetOrder.OnCall = (ko, id) => new Order { Id = id, CustomerId = 1 };
    stub.ValidateOrder.OnCall = (ko, _) => true;
    stub.CalculateTotal.OnCall = (ko, _) => 100m;

    var sut = new OrderProcessor(stub);

    // Act
    sut.Process(1);

    // Assert
    Assert.Equal(1, stub.GetOrder.CallCount);
    Assert.Equal(1, stub.ValidateOrder.CallCount);
    Assert.Equal(1, stub.SaveOrder.CallCount);
}
```
<!-- endSnippet -->

## Features

- Properties (get/set, get-only, init-only, required)
- Methods (void, return values, async, overloads, generics)
- Generic interfaces and methods
- Interface inheritance
- Indexers and events
- Delegates (`Func<>`, `Action<>`, named delegates)
- ref/out parameters
- Named tuple argument tracking (`stub.Method.LastCallArgs?.email`)

## Three Stub Patterns

**Standalone stub** — reusable across test files:

<!-- pseudo:readme-pattern-standalone -->
```csharp
[KnockOff]
public partial class UserRepositoryStub : IUserRepository { }
```
<!-- /snippet -->

**Inline stub** — scoped to a test class:

<!-- pseudo:readme-pattern-inline -->
```csharp
[KnockOff<IUserRepository>]
public partial class UserTests
{
    [Fact]
    public void Test()
    {
        var stub = new Stubs.IUserRepository();
        // ...
    }
}
```
<!-- /snippet -->

**Delegate stub** — for callbacks and rules:

<!-- pseudo:readme-pattern-delegate -->
```csharp
[KnockOff<Func<int, bool>>]
public partial class ValidationTests { }
```
<!-- /snippet -->

## Documentation

- [Getting Started](docs/getting-started.md) — Installation and first steps
- [Why KnockOff?](docs/why-knockoff/) — Readability, duality pattern, compile-time safety
- [Guides](docs/guides/) — Stub patterns, methods, properties, events, verification
- [KnockOff vs Moq](docs/knockoff-vs-moq.md) — Detailed comparison
- [Migration from Moq](docs/migration-from-moq.md) — Step-by-step guide
- [API Reference](docs/reference/) — Interceptor API, attributes, diagnostics
- [For AI Assistants](docs/for-ai-assistants.md) — Code generation guidelines

## Limitations

KnockOff doesn't support argument matchers (`It.IsAny<T>`), `SetupSequence`, `VerifyNoOtherCalls`, strict mode, or `MockRepository`. See [KnockOff vs Moq](docs/knockoff-vs-moq.md) for details and workarounds.

## License

MIT
