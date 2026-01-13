# Migration from Moq

This guide helps you migrate from Moq to KnockOff for interface stubbing.

## Quick Reference

| Moq | KnockOff |
|-----|----------|
| `new Mock<IService>()` | `new ServiceKnockOff()` |
| `mock.Object` | Cast to interface or `knockOff.AsIService()` |
| `.Setup(x => x.Method())` | `Method.OnCall = ...` |
| `.Returns(value)` | `OnCall = (ko) => value` |
| `.ReturnsAsync(value)` | `OnCall = (ko) => Task.FromResult(value)` |
| `.Callback(action)` | Logic inside `OnCall` |
| `.Verify(Times.Once)` | `Assert.Equal(1, Method.CallCount)` |
| `It.IsAny<T>()` | Implicit (callback receives all args) |
| `It.Is<T>(predicate)` | Check in callback body |

## Step-by-Step Migration

### Step 1: Create KnockOff Class

**Before (Moq):**

<!-- snippet: moq-create-mock -->
```cs
public void CreateMock()
{
    var mock = new Mock<IMoqMigUserService>();
    _ = mock;
}
```
<!-- endSnippet -->

<!-- snippet: migration-from-moq-instantiate-knockoff -->
```cs
// var knockOff = new MigUserServiceKnockOff();
```
<!-- endSnippet -->

### Step 2: Replace mock.Object

**Before (Moq):**

<!-- snippet: moq-mock-object -->
```cs
public void MockObject()
{
    var mock = new Mock<IMoqMigUserService>();

    var service = mock.Object;
    DoSomething(mock.Object);

    _ = service;
}

private static void DoSomething(IMoqMigUserService service) { }
```
<!-- endSnippet -->

### Step 3: Convert Setup/Returns

**Before (Moq):**

<!-- snippet: moq-setup-returns -->
```cs
public void SetupReturns()
{
    var mock = new Mock<IMoqMigUserService>();

    mock.Setup(x => x.GetUser(It.IsAny<int>()))
        .Returns(new MoqMigUser { Id = 1, Name = "Test" });
}
```
<!-- endSnippet -->

### Step 4: Convert Async Returns

**Before (Moq):**

<!-- snippet: moq-async-returns -->
```cs
public void AsyncReturns()
{
    var mock = new Mock<IMoqMigUserService>();

    mock.Setup(x => x.GetUserAsync(It.IsAny<int>()))
        .ReturnsAsync(new MoqMigUser { Id = 1 });
}
```
<!-- endSnippet -->

### Step 5: Convert Verification

**Before (Moq):**

<!-- snippet: moq-verification -->
```cs
public void Verification()
{
    var mock = new Mock<IMoqMigUserService>();

    mock.Object.Save(new MoqMigUser());
    mock.Object.GetAll();
    mock.Object.Update(new MoqMigUser());
    mock.Object.Update(new MoqMigUser());
    mock.Object.Update(new MoqMigUser());

    mock.Verify(x => x.Save(It.IsAny<MoqMigUser>()), Times.Once);
    mock.Verify(x => x.Delete(It.IsAny<int>()), Times.Never);
    mock.Verify(x => x.GetAll(), Times.AtLeastOnce);
    mock.Verify(x => x.Update(It.IsAny<MoqMigUser>()), Times.Exactly(3));
}
```
<!-- endSnippet -->

### Step 6: Convert Callback

**Before (Moq):**

<!-- snippet: moq-callback -->
```cs
public void Callback()
{
    MoqMigUser? captured = null;
    var mock = new Mock<IMoqMigUserService>();

    mock.Setup(x => x.Save(It.IsAny<MoqMigUser>()))
        .Callback<MoqMigUser>(u => captured = u);

    _ = captured;
}
```
<!-- endSnippet -->

### Step 7: Convert Property Setup

**Before (Moq):**

<!-- snippet: moq-property-setup -->
```cs
public void PropertySetup()
{
    var mock = new Mock<IMoqMigUserService>();

    mock.Setup(x => x.Name).Returns("Test");
    mock.SetupSet(x => x.Name = It.IsAny<string>()).Verifiable();
}
```
<!-- endSnippet -->

## Common Patterns

### Static Returns (Simplest Migration)

**Moq:**

<!-- snippet: moq-static-returns -->
```cs
public void StaticReturns()
{
    var mock = new Mock<IMoqMigConfigService>();

    mock.Setup(x => x.GetConfig()).Returns(new MoqMigConfig { Timeout = 30 });
}
```
<!-- endSnippet -->

**KnockOff Option 2 - Callback (runtime):**
<!-- snippet: migration-from-moq-static-returns-callback -->
```cs
// knockOff.GetConfig.OnCall = (ko) => new MigConfig { Timeout = 30 };
```
<!-- endSnippet -->

### Conditional Returns

**Moq:**

<!-- snippet: moq-conditional-returns -->
```cs
public void ConditionalReturns()
{
    var mock = new Mock<IMoqMigUserService>();

    mock.Setup(x => x.GetUser(1)).Returns(new MoqMigUser { Name = "Admin" });
    mock.Setup(x => x.GetUser(2)).Returns(new MoqMigUser { Name = "Guest" });
    mock.Setup(x => x.GetUser(It.IsAny<int>())).Returns((MoqMigUser?)null);
}
```
<!-- endSnippet -->

### Throwing Exceptions

**Moq:**

<!-- snippet: moq-throwing-exceptions -->
```cs
public void ThrowingExceptions()
{
    var mock = new Mock<IMoqMigConnection>();

    mock.Setup(x => x.Connect()).Throws(new TimeoutException());
}
```
<!-- endSnippet -->

<!-- snippet: migration-from-moq-throwing-exceptions-usage -->
```cs
// knockOff.Connect.OnCall = (ko) =>
//     throw new TimeoutException();
```
<!-- endSnippet -->

### Sequential Returns

**Moq:**

<!-- snippet: moq-setup-sequence -->
```cs
public void SetupSequence()
{
    var mock = new Mock<IMoqMigSequence>();

    mock.SetupSequence(x => x.GetNext())
        .Returns(1)
        .Returns(2)
        .Returns(3);
}
```
<!-- endSnippet -->

<!-- snippet: migration-from-moq-sequential-returns-usage -->
```cs
// var values = new Queue<int>([1, 2, 3]);
// knockOff.GetNext.OnCall = (ko) => values.Dequeue();
```
<!-- endSnippet -->

### Multiple Interfaces

**Moq:**

<!-- snippet: moq-multiple-interfaces-as -->
```cs
public void MultipleInterfacesAs()
{
    var mock = new Mock<IMoqMigRepository>();
    mock.As<IMoqMigUnitOfWork>()
        .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(1);

    var repo = mock.Object;
    var uow = mock.As<IMoqMigUnitOfWork>().Object;

    _ = (repo, uow);
}
```
<!-- endSnippet -->

<!-- snippet: migration-from-moq-multiple-interfaces-usage -->
```cs
// // Separate stubs for each interface
// var repoKnockOff = new MigRepositoryKnockOff();
// IMigRepository repo = repoKnockOff.AsIMigRepository();
//
// var uowKnockOff = new MigUnitOfWorkKnockOff();
// uowKnockOff.SaveChangesAsync.OnCall = (ko, ct) => Task.FromResult(1);
// IMigUnitOfWork uow = uowKnockOff.AsIMigUnitOfWork();
```
<!-- endSnippet -->

### Argument Matching

**Moq:**

<!-- snippet: moq-argument-matching -->
```cs
public void ArgumentMatching()
{
    var errors = new List<string>();
    var mock = new Mock<IMoqMigLogger>();

    mock.Setup(x => x.Log(It.Is<string>(s => s.Contains("error"))))
        .Callback<string>(s => errors.Add(s));
}
```
<!-- endSnippet -->

<!-- snippet: migration-from-moq-argument-matching-usage -->
```cs
// knockOff.Log.OnCall = (ko, message) =>
// {
//     if (message.Contains("error"))
//         errors.Add(message);
// };
```
<!-- endSnippet -->

## Feature Comparison

| Feature | Moq | KnockOff |
|---------|-----|----------|
| Runtime configuration | Yes | Yes (callbacks) |
| Compile-time configuration | No | Yes (user methods) |
| Type-safe setup | Expression-based | Strongly-typed handlers |
| Argument capture | Via Callback | Automatic (LastCallArg/LastCallArgs) |
| Call counting | Verify(Times.X) | CallCount property |
| Strict mode | Yes | No |
| VerifyNoOtherCalls | Yes | No |
| Events | Yes | Yes |
| ref/out | Yes | Not yet |
| Generic methods | Yes | Not yet |

## Tips for Migration

### Start with Simple Tests

Begin with tests that only verify calls:
<!-- snippet: migration-from-moq-simple-verification -->
```cs
// // These translate directly
// Assert.True(knockOff.Method.WasCalled);
// Assert.Equal(expectedCount, knockOff.Method.CallCount);
```
<!-- endSnippet -->

### Use User Methods for Shared Stubs

If multiple tests use the same mock setup, move it to a user method:

<!-- snippet: migration-from-moq-shared-stubs -->
```cs
[KnockOff]
public partial class MigSharedRepositoryKnockOff : IMigSharedRepository
{
    // Test data shared across tests
    private static readonly List<MigUser> TestUsers = new()
    {
        new MigUser { Id = 1, Name = "Admin" },
        new MigUser { Id = 2, Name = "Guest" }
    };

    protected MigUser? GetById(int id) => TestUsers.FirstOrDefault(u => u.Id == id);
}
```
<!-- endSnippet -->

### Keep Complex Logic in Callbacks

For test-specific behavior, use callbacks:

<!-- snippet: migration-from-moq-complex-callbacks -->
```cs
[KnockOff]
public partial class MigSaveServiceKnockOff : IMigSaveService { }
```
<!-- endSnippet -->

<!-- snippet: migration-from-moq-complex-callbacks-usage -->
```cs
// knockOff.Save.OnCall = (ko, entity) =>
// {
//     entity.Id = nextId++;
//     savedEntities.Add(entity);
// };
```
<!-- endSnippet -->

### Leverage Automatic Tracking

Unlike Moq where you need Callback to capture args, KnockOff tracks automatically:

<!-- snippet: migration-from-moq-automatic-tracking -->
```cs
[KnockOff]
public partial class MigProcessorKnockOff : IMigProcessor { }
```
<!-- endSnippet -->

<!-- snippet: migration-from-moq-automatic-tracking-usage -->
```cs
// // No setup needed - just call the method
// service.Process("data");
//
// // Args are captured
// Assert.Equal("data", knockOff.Process.LastCallArg);
```
<!-- endSnippet -->

## What to Keep in Moq

Some features aren't in KnockOff yet. Keep using Moq for:

- **ref/out parameters** — By-reference parameters
- **Generic methods** — Methods with type parameters
- **Strict mode** — Throwing on unconfigured calls
- **VerifyNoOtherCalls** — Ensuring no unexpected calls

## Gradual Migration

You can use both in the same project:

<!-- snippet: gradual-migration -->
```cs
public void GradualMigration()
{
    // New tests use KnockOff
    var userKnockOff = new MigUserServiceKnockOff();

    // Legacy tests keep Moq (until migrated)
    var orderMock = new Mock<IMoqMigOrderService>();

    _ = (userKnockOff, orderMock);
}
```
<!-- endSnippet -->

Migrate incrementally as you touch tests.
