# Migration from Moq

This guide helps you migrate from Moq to KnockOff for interface stubbing.

## Quick Reference

| Moq | KnockOff |
|-----|----------|
| `new Mock<IService>()` | `new ServiceKnockOff()` |
| `mock.Object` | Cast to interface or `knockOff.AsService()` |
| `.Setup(x => x.Method())` | `IInterface.Method.OnCall = ...` |
| `.Returns(value)` | `OnCall = (ko) => value` |
| `.ReturnsAsync(value)` | `OnCall = (ko) => Task.FromResult(value)` |
| `.Callback(action)` | Logic inside `OnCall` |
| `.Verify(Times.Once)` | `Assert.Equal(1, IInterface.Method.CallCount)` |
| `It.IsAny<T>()` | Implicit (callback receives all args) |
| `It.Is<T>(predicate)` | Check in callback body |

## Step-by-Step Migration

### Step 1: Create KnockOff Class

**Before (Moq):**
```csharp
var mock = new Mock<IUserService>();
```

**After (KnockOff):**
```csharp
// Create once, typically in test project
[KnockOff]
public partial class UserServiceKnockOff : IUserService { }

// In test
var knockOff = new UserServiceKnockOff();
```

### Step 2: Replace mock.Object

**Before (Moq):**
```csharp
var service = mock.Object;
DoSomething(mock.Object);
```

**After (KnockOff):**
```csharp
IUserService service = knockOff;
// or
DoSomething(knockOff.AsUserService());
```

### Step 3: Convert Setup/Returns

**Before (Moq):**
```csharp
mock.Setup(x => x.GetUser(It.IsAny<int>()))
    .Returns(new User { Id = 1, Name = "Test" });
```

**After (KnockOff):**
```csharp
knockOff.IUserService.GetUser.OnCall = (ko, id) =>
    new User { Id = id, Name = "Test" };
```

### Step 4: Convert Async Returns

**Before (Moq):**
```csharp
mock.Setup(x => x.GetUserAsync(It.IsAny<int>()))
    .ReturnsAsync(new User { Id = 1 });
```

**After (KnockOff):**
```csharp
knockOff.IUserService.GetUserAsync.OnCall = (ko, id) =>
    Task.FromResult<User?>(new User { Id = id });
```

### Step 5: Convert Verification

**Before (Moq):**
```csharp
mock.Verify(x => x.Save(It.IsAny<User>()), Times.Once);
mock.Verify(x => x.Delete(It.IsAny<int>()), Times.Never);
mock.Verify(x => x.GetAll(), Times.AtLeastOnce);
mock.Verify(x => x.Update(It.IsAny<User>()), Times.Exactly(3));
```

**After (KnockOff):**
```csharp
Assert.Equal(1, knockOff.IUserService.Save.CallCount);
Assert.Equal(0, knockOff.IUserService.Delete.CallCount);
Assert.True(knockOff.IUserService.GetAll.WasCalled);
Assert.Equal(3, knockOff.IUserService.Update.CallCount);
```

### Step 6: Convert Callback

**Before (Moq):**
```csharp
User? captured = null;
mock.Setup(x => x.Save(It.IsAny<User>()))
    .Callback<User>(u => captured = u);
```

**After (KnockOff):**
```csharp
// Arguments are captured automatically
var captured = knockOff.IUserService.Save.LastCallArg;

// Or use callback for custom logic
knockOff.IUserService.Save.OnCall = (ko, user) =>
{
    customList.Add(user);
};
```

### Step 7: Convert Property Setup

**Before (Moq):**
```csharp
mock.Setup(x => x.Name).Returns("Test");
mock.SetupSet(x => x.Name = It.IsAny<string>()).Verifiable();
```

**After (KnockOff):**
```csharp
knockOff.IUserService.Name.OnGet = (ko) => "Test";

// Setter tracking is automatic
service.Name = "Value";
Assert.Equal("Value", knockOff.IUserService.Name.LastSetValue);
```

## Common Patterns

### Static Returns (Simplest Migration)

**Moq:**
```csharp
mock.Setup(x => x.GetConfig()).Returns(new Config { Timeout = 30 });
```

**KnockOff Option 1 - User Method (compile-time):**
```csharp
[KnockOff]
public partial class ConfigServiceKnockOff : IConfigService
{
    protected Config GetConfig() => new Config { Timeout = 30 };
}
```

**KnockOff Option 2 - Callback (runtime):**
```csharp
knockOff.IConfigService.GetConfig.OnCall = (ko) => new Config { Timeout = 30 };
```

### Conditional Returns

**Moq:**
```csharp
mock.Setup(x => x.GetUser(1)).Returns(new User { Name = "Admin" });
mock.Setup(x => x.GetUser(2)).Returns(new User { Name = "Guest" });
mock.Setup(x => x.GetUser(It.IsAny<int>())).Returns((User?)null);
```

**KnockOff:**
```csharp
knockOff.IUserService.GetUser.OnCall = (ko, id) => id switch
{
    1 => new User { Name = "Admin" },
    2 => new User { Name = "Guest" },
    _ => null
};
```

### Throwing Exceptions

**Moq:**
```csharp
mock.Setup(x => x.Connect()).Throws(new TimeoutException());
```

**KnockOff:**
```csharp
knockOff.IConnection.Connect.OnCall = (ko) =>
    throw new TimeoutException();
```

### Sequential Returns

**Moq:**
```csharp
mock.SetupSequence(x => x.GetNext())
    .Returns(1)
    .Returns(2)
    .Returns(3);
```

**KnockOff:**
```csharp
var values = new Queue<int>([1, 2, 3]);
knockOff.ISequence.GetNext.OnCall = (ko) => values.Dequeue();
```

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
```csharp
[KnockOff]
public partial class DataContextKnockOff : IRepository, IUnitOfWork { }

var knockOff = new DataContextKnockOff();
knockOff.IUnitOfWork.SaveChangesAsync.OnCall = (ko, ct) => Task.FromResult(1);

IRepository repo = knockOff.AsRepository();
IUnitOfWork uow = knockOff.AsUnitOfWork();
```

### Argument Matching

**Moq:**
```csharp
mock.Setup(x => x.Log(It.Is<string>(s => s.Contains("error"))))
    .Callback<string>(s => errors.Add(s));
```

**KnockOff:**
```csharp
knockOff.ILogger.Log.OnCall = (ko, message) =>
{
    if (message.Contains("error"))
        errors.Add(message);
};
```

## Feature Comparison

| Feature | Moq | KnockOff |
|---------|-----|----------|
| Runtime configuration | Yes | Yes (callbacks) |
| Compile-time configuration | No | Yes (user methods) |
| Type-safe setup | Expression-based | Strongly-typed handlers |
| Argument capture | Via Callback | Automatic (AllCalls) |
| Call counting | Verify(Times.X) | CallCount property |
| Strict mode | Yes | No |
| VerifyNoOtherCalls | Yes | No |
| Events | Yes | Yes |
| ref/out | Yes | Not yet |
| Generic methods | Yes | Not yet |

## Tips for Migration

### Start with Simple Tests

Begin with tests that only verify calls:
```csharp
// These translate directly
Assert.True(knockOff.IService.Method.WasCalled);
Assert.Equal(expectedCount, knockOff.IService.Method.CallCount);
```

### Use User Methods for Shared Stubs

If multiple tests use the same mock setup, move it to a user method:
```csharp
[KnockOff]
public partial class RepositoryKnockOff : IRepository
{
    protected User? GetById(int id) => TestData.Users.FirstOrDefault(u => u.Id == id);
}
```

### Keep Complex Logic in Callbacks

For test-specific behavior, use callbacks:
```csharp
knockOff.ISaveService.Save.OnCall = (ko, entity) =>
{
    entity.Id = nextId++;
    savedEntities.Add(entity);
};
```

### Leverage Automatic Tracking

Unlike Moq where you need Callback to capture args, KnockOff tracks automatically:
```csharp
// No setup needed - just call the method
service.Process("data");

// Args are captured
Assert.Equal("data", knockOff.IProcessor.Process.LastCallArg);
```

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
