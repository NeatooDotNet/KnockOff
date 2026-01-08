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
```csharp
var mock = new Mock<IUserService>();
```

**After (KnockOff):**

<!-- snippet: docs:migration-from-moq:create-knockoff-class -->
```csharp
[KnockOff]
public partial class MigUserServiceKnockOff : IMigUserService { }
```
<!-- /snippet -->

<!-- snippet: docs:migration-from-moq:instantiate-knockoff -->
```csharp
// var knockOff = new MigUserServiceKnockOff();
```
<!-- /snippet -->

### Step 2: Replace mock.Object

**Before (Moq):**
```csharp
var service = mock.Object;
DoSomething(mock.Object);
```

**After (KnockOff):**
<!-- snippet: docs:migration-from-moq:replace-mock-object -->
```csharp
// IMigUserService service = knockOff;
// // or
// DoSomething(knockOff.AsIMigUserService());
```
<!-- /snippet -->

### Step 3: Convert Setup/Returns

**Before (Moq):**
```csharp
mock.Setup(x => x.GetUser(It.IsAny<int>()))
    .Returns(new User { Id = 1, Name = "Test" });
```

**After (KnockOff):**
<!-- snippet: docs:migration-from-moq:convert-setup-returns -->
```csharp
// knockOff.GetUser.OnCall = (ko, id) =>
//     new User { Id = id, Name = "Test" };
```
<!-- /snippet -->

### Step 4: Convert Async Returns

**Before (Moq):**
```csharp
mock.Setup(x => x.GetUserAsync(It.IsAny<int>()))
    .ReturnsAsync(new User { Id = 1 });
```

**After (KnockOff):**
<!-- snippet: docs:migration-from-moq:convert-async-returns -->
```csharp
// knockOff.GetUserAsync.OnCall = (ko, id) =>
//     Task.FromResult<User?>(new User { Id = id });
```
<!-- /snippet -->

### Step 5: Convert Verification

**Before (Moq):**
```csharp
mock.Verify(x => x.Save(It.IsAny<User>()), Times.Once);
mock.Verify(x => x.Delete(It.IsAny<int>()), Times.Never);
mock.Verify(x => x.GetAll(), Times.AtLeastOnce);
mock.Verify(x => x.Update(It.IsAny<User>()), Times.Exactly(3));
```

**After (KnockOff):**
<!-- snippet: docs:migration-from-moq:convert-verification -->
```csharp
// Assert.Equal(1, knockOff.Save.CallCount);
// Assert.Equal(0, knockOff.Delete.CallCount);
// Assert.True(knockOff.GetAll.WasCalled);
// Assert.Equal(3, knockOff.Update.CallCount);
```
<!-- /snippet -->

### Step 6: Convert Callback

**Before (Moq):**
```csharp
User? captured = null;
mock.Setup(x => x.Save(It.IsAny<User>()))
    .Callback<User>(u => captured = u);
```

**After (KnockOff):**
<!-- snippet: docs:migration-from-moq:convert-callback -->
```csharp
// // Arguments are captured automatically
// var captured = knockOff.Save.LastCallArg;
//
// // Or use callback for custom logic
// knockOff.Save.OnCall = (ko, user) =>
// {
//     customList.Add(user);
// };
```
<!-- /snippet -->

### Step 7: Convert Property Setup

**Before (Moq):**
```csharp
mock.Setup(x => x.Name).Returns("Test");
mock.SetupSet(x => x.Name = It.IsAny<string>()).Verifiable();
```

**After (KnockOff):**
<!-- snippet: docs:migration-from-moq:convert-property-setup -->
```csharp
// knockOff.Name.OnGet = (ko) => "Test";
//
// // Setter tracking is automatic
// service.Name = "Value";
// Assert.Equal("Value", knockOff.Name.LastSetValue);
```
<!-- /snippet -->

## Common Patterns

### Static Returns (Simplest Migration)

**Moq:**
```csharp
mock.Setup(x => x.GetConfig()).Returns(new Config { Timeout = 30 });
```

**KnockOff Option 1 - User Method (compile-time):**

<!-- snippet: docs:migration-from-moq:static-returns-user-method -->
```csharp
[KnockOff]
public partial class MigConfigServiceKnockOff : IMigConfigService
{
    protected MigConfig GetConfig() => new MigConfig { Timeout = 30 };
}
```
<!-- /snippet -->

**KnockOff Option 2 - Callback (runtime):**
<!-- snippet: docs:migration-from-moq:static-returns-callback -->
```csharp
// knockOff.GetConfig.OnCall = (ko) => new MigConfig { Timeout = 30 };
```
<!-- /snippet -->

### Conditional Returns

**Moq:**
```csharp
mock.Setup(x => x.GetUser(1)).Returns(new User { Name = "Admin" });
mock.Setup(x => x.GetUser(2)).Returns(new User { Name = "Guest" });
mock.Setup(x => x.GetUser(It.IsAny<int>())).Returns((User?)null);
```

**KnockOff:**
<!-- snippet: docs:migration-from-moq:conditional-returns -->
```csharp
// knockOff.GetUser.OnCall = (ko, id) => id switch
// {
//     1 => new User { Name = "Admin" },
//     2 => new User { Name = "Guest" },
//     _ => null
// };
```
<!-- /snippet -->

### Throwing Exceptions

**Moq:**
```csharp
mock.Setup(x => x.Connect()).Throws(new TimeoutException());
```

**KnockOff:**

<!-- snippet: docs:migration-from-moq:throwing-exceptions -->
```csharp
[KnockOff]
public partial class MigConnectionKnockOff : IMigConnection { }
```
<!-- /snippet -->

<!-- snippet: docs:migration-from-moq:throwing-exceptions-usage -->
```csharp
// knockOff.Connect.OnCall = (ko) =>
//     throw new TimeoutException();
```
<!-- /snippet -->

### Sequential Returns

**Moq:**
```csharp
mock.SetupSequence(x => x.GetNext())
    .Returns(1)
    .Returns(2)
    .Returns(3);
```

**KnockOff:**

<!-- snippet: docs:migration-from-moq:sequential-returns -->
```csharp
[KnockOff]
public partial class MigSequenceKnockOff : IMigSequence { }
```
<!-- /snippet -->

<!-- snippet: docs:migration-from-moq:sequential-returns-usage -->
```csharp
// var values = new Queue<int>([1, 2, 3]);
// knockOff.GetNext.OnCall = (ko) => values.Dequeue();
```
<!-- /snippet -->

### Multiple Interfaces

**Moq:**
```csharp
var mock = new Mock<IRepository>();
mock.As<IUnitOfWork>()
    .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
    .ReturnsAsync(1);

var repo = mock.Object;
var uow = mock.As<IUnitOfWork>().Object;
```

**KnockOff:**

<!-- snippet: docs:migration-from-moq:multiple-interfaces -->
```csharp
[KnockOff]
public partial class MigRepositoryKnockOff : IMigRepository { }

[KnockOff]
public partial class MigUnitOfWorkKnockOff : IMigUnitOfWork { }
```
<!-- /snippet -->

<!-- snippet: docs:migration-from-moq:multiple-interfaces-usage -->
```csharp
// // Separate stubs for each interface
// var repoKnockOff = new MigRepositoryKnockOff();
// IMigRepository repo = repoKnockOff.AsIMigRepository();
//
// var uowKnockOff = new MigUnitOfWorkKnockOff();
// uowKnockOff.SaveChangesAsync.OnCall = (ko, ct) => Task.FromResult(1);
// IMigUnitOfWork uow = uowKnockOff.AsIMigUnitOfWork();
```
<!-- /snippet -->

### Argument Matching

**Moq:**
```csharp
mock.Setup(x => x.Log(It.Is<string>(s => s.Contains("error"))))
    .Callback<string>(s => errors.Add(s));
```

**KnockOff:**

<!-- snippet: docs:migration-from-moq:argument-matching -->
```csharp
[KnockOff]
public partial class MigLoggerKnockOff : IMigLogger { }
```
<!-- /snippet -->

<!-- snippet: docs:migration-from-moq:argument-matching-usage -->
```csharp
// knockOff.Log.OnCall = (ko, message) =>
// {
//     if (message.Contains("error"))
//         errors.Add(message);
// };
```
<!-- /snippet -->

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
<!-- snippet: docs:migration-from-moq:simple-verification -->
```csharp
// // These translate directly
// Assert.True(knockOff.Method.WasCalled);
// Assert.Equal(expectedCount, knockOff.Method.CallCount);
```
<!-- /snippet -->

### Use User Methods for Shared Stubs

If multiple tests use the same mock setup, move it to a user method:

<!-- snippet: docs:migration-from-moq:shared-stubs -->
```csharp
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
<!-- /snippet -->

### Keep Complex Logic in Callbacks

For test-specific behavior, use callbacks:

<!-- snippet: docs:migration-from-moq:complex-callbacks -->
```csharp
[KnockOff]
public partial class MigSaveServiceKnockOff : IMigSaveService { }
```
<!-- /snippet -->

<!-- snippet: docs:migration-from-moq:complex-callbacks-usage -->
```csharp
// knockOff.Save.OnCall = (ko, entity) =>
// {
//     entity.Id = nextId++;
//     savedEntities.Add(entity);
// };
```
<!-- /snippet -->

### Leverage Automatic Tracking

Unlike Moq where you need Callback to capture args, KnockOff tracks automatically:

<!-- snippet: docs:migration-from-moq:automatic-tracking -->
```csharp
[KnockOff]
public partial class MigProcessorKnockOff : IMigProcessor { }
```
<!-- /snippet -->

<!-- snippet: docs:migration-from-moq:automatic-tracking-usage -->
```csharp
// // No setup needed - just call the method
// service.Process("data");
//
// // Args are captured
// Assert.Equal("data", knockOff.Process.LastCallArg);
```
<!-- /snippet -->

## What to Keep in Moq

Some features aren't in KnockOff yet. Keep using Moq for:

- **ref/out parameters** — By-reference parameters
- **Generic methods** — Methods with type parameters
- **Strict mode** — Throwing on unconfigured calls
- **VerifyNoOtherCalls** — Ensuring no unexpected calls

## Gradual Migration

You can use both in the same project:

```csharp
// New tests use KnockOff
var userKnockOff = new UserServiceKnockOff();

// Legacy tests keep Moq (until migrated)
var orderMock = new Mock<IOrderService>();
```

Migrate incrementally as you touch tests.
