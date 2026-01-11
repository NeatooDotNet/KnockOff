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

<!-- pseudo:moq-create-mock -->
```csharp
var mock = new Mock<IUserService>();
```
<!-- /snippet -->

**After (KnockOff):**

<!-- snippet: migration-from-moq-create-knockoff-class -->
```cs
[KnockOff]
public partial class MigUserServiceKnockOff : IMigUserService { }
```
<!-- endSnippet -->

<!-- snippet: migration-from-moq-instantiate-knockoff -->
```cs
// var knockOff = new MigUserServiceKnockOff();
```
<!-- endSnippet -->

### Step 2: Replace mock.Object

**Before (Moq):**

<!-- pseudo:moq-mock-object -->
```csharp
var service = mock.Object;
DoSomething(mock.Object);
```
<!-- /snippet -->

**After (KnockOff):**
<!-- snippet: migration-from-moq-replace-mock-object -->
```cs
// IMigUserService service = knockOff;
// // or
// DoSomething(knockOff.AsIMigUserService());
```
<!-- endSnippet -->

### Step 3: Convert Setup/Returns

**Before (Moq):**

<!-- pseudo:moq-setup-returns -->
```csharp
mock.Setup(x => x.GetUser(It.IsAny<int>()))
    .Returns(new User { Id = 1, Name = "Test" });
```
<!-- /snippet -->

**After (KnockOff):**
<!-- snippet: migration-from-moq-convert-setup-returns -->
```cs
// knockOff.GetUser.OnCall = (ko, id) =>
//     new User { Id = id, Name = "Test" };
```
<!-- endSnippet -->

### Step 4: Convert Async Returns

**Before (Moq):**

<!-- pseudo:moq-async-returns -->
```csharp
mock.Setup(x => x.GetUserAsync(It.IsAny<int>()))
    .ReturnsAsync(new User { Id = 1 });
```
<!-- /snippet -->

**After (KnockOff):**
<!-- snippet: migration-from-moq-convert-async-returns -->
```cs
// knockOff.GetUserAsync.OnCall = (ko, id) =>
//     Task.FromResult<User?>(new User { Id = id });
```
<!-- endSnippet -->

### Step 5: Convert Verification

**Before (Moq):**

<!-- pseudo:moq-verification -->
```csharp
mock.Verify(x => x.Save(It.IsAny<User>()), Times.Once);
mock.Verify(x => x.Delete(It.IsAny<int>()), Times.Never);
mock.Verify(x => x.GetAll(), Times.AtLeastOnce);
mock.Verify(x => x.Update(It.IsAny<User>()), Times.Exactly(3));
```
<!-- /snippet -->

**After (KnockOff):**
<!-- snippet: migration-from-moq-convert-verification -->
```cs
// Assert.Equal(1, knockOff.Save.CallCount);
// Assert.Equal(0, knockOff.Delete.CallCount);
// Assert.True(knockOff.GetAll.WasCalled);
// Assert.Equal(3, knockOff.Update.CallCount);
```
<!-- endSnippet -->

### Step 6: Convert Callback

**Before (Moq):**

<!-- pseudo:moq-callback -->
```csharp
User? captured = null;
mock.Setup(x => x.Save(It.IsAny<User>()))
    .Callback<User>(u => captured = u);
```
<!-- /snippet -->

**After (KnockOff):**
<!-- snippet: migration-from-moq-convert-callback -->
```cs
// // Arguments are captured automatically
// var captured = knockOff.Save.LastCallArg;
//
// // Or use callback for custom logic
// knockOff.Save.OnCall = (ko, user) =>
// {
//     customList.Add(user);
// };
```
<!-- endSnippet -->

### Step 7: Convert Property Setup

**Before (Moq):**

<!-- pseudo:moq-property-setup -->
```csharp
mock.Setup(x => x.Name).Returns("Test");
mock.SetupSet(x => x.Name = It.IsAny<string>()).Verifiable();
```
<!-- /snippet -->

**After (KnockOff):**
<!-- snippet: migration-from-moq-convert-property-setup -->
```cs
// knockOff.Name.OnGet = (ko) => "Test";
//
// // Setter tracking is automatic
// service.Name = "Value";
// Assert.Equal("Value", knockOff.Name.LastSetValue);
```
<!-- endSnippet -->

## Common Patterns

### Static Returns (Simplest Migration)

**Moq:**

<!-- pseudo:moq-static-returns -->
```csharp
mock.Setup(x => x.GetConfig()).Returns(new Config { Timeout = 30 });
```
<!-- /snippet -->

**KnockOff Option 1 - User Method (compile-time):**

<!-- snippet: migration-from-moq-static-returns-user-method -->
```cs
[KnockOff]
public partial class MigConfigServiceKnockOff : IMigConfigService
{
    protected MigConfig GetConfig() => new MigConfig { Timeout = 30 };
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

<!-- pseudo:moq-conditional-returns -->
```csharp
mock.Setup(x => x.GetUser(1)).Returns(new User { Name = "Admin" });
mock.Setup(x => x.GetUser(2)).Returns(new User { Name = "Guest" });
mock.Setup(x => x.GetUser(It.IsAny<int>())).Returns((User?)null);
```
<!-- /snippet -->

**KnockOff:**
<!-- snippet: migration-from-moq-conditional-returns -->
```cs
// knockOff.GetUser.OnCall = (ko, id) => id switch
// {
//     1 => new User { Name = "Admin" },
//     2 => new User { Name = "Guest" },
//     _ => null
// };
```
<!-- endSnippet -->

### Throwing Exceptions

**Moq:**

<!-- pseudo:moq-throwing-exceptions -->
```csharp
mock.Setup(x => x.Connect()).Throws(new TimeoutException());
```
<!-- /snippet -->

**KnockOff:**

<!-- snippet: migration-from-moq-throwing-exceptions -->
```cs
[KnockOff]
public partial class MigConnectionKnockOff : IMigConnection { }
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

<!-- pseudo:moq-setup-sequence -->
```csharp
mock.SetupSequence(x => x.GetNext())
    .Returns(1)
    .Returns(2)
    .Returns(3);
```
<!-- /snippet -->

**KnockOff:**

<!-- snippet: migration-from-moq-sequential-returns -->
```cs
[KnockOff]
public partial class MigSequenceKnockOff : IMigSequence { }
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

<!-- pseudo:moq-multiple-interfaces-as -->
```csharp
var mock = new Mock<IRepository>();
mock.As<IUnitOfWork>()
    .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
    .ReturnsAsync(1);

var repo = mock.Object;
var uow = mock.As<IUnitOfWork>().Object;
```
<!-- /snippet -->

**KnockOff:**

<!-- snippet: migration-from-moq-multiple-interfaces -->
```cs
[KnockOff]
public partial class MigRepositoryKnockOff : IMigRepository { }

[KnockOff]
public partial class MigUnitOfWorkKnockOff : IMigUnitOfWork { }
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

<!-- pseudo:moq-argument-matching -->
```csharp
mock.Setup(x => x.Log(It.Is<string>(s => s.Contains("error"))))
    .Callback<string>(s => errors.Add(s));
```
<!-- /snippet -->

**KnockOff:**

<!-- snippet: migration-from-moq-argument-matching -->
```cs
[KnockOff]
public partial class MigLoggerKnockOff : IMigLogger { }
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

<!-- pseudo:gradual-migration -->
```csharp
// New tests use KnockOff
var userKnockOff = new UserServiceKnockOff();

// Legacy tests keep Moq (until migrated)
var orderMock = new Mock<IOrderService>();
```
<!-- /snippet -->

Migrate incrementally as you touch tests.
