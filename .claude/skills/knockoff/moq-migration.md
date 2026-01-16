# Moq to KnockOff Migration

Step-by-step guide for migrating from Moq to KnockOff.

## Quick Reference

| Moq | KnockOff |
|-----|----------|
| `new Mock<IService>()` | `new ServiceStub()` |
| `mock.Object` | Cast to interface (implicit) |
| `.Setup(x => x.Method()).Returns(v)` | `stub.Method.OnCall = (ko) => v` |
| `.Setup(x => x.Prop).Returns(v)` | `stub.Prop.Value = v` |
| `.ReturnsAsync(v)` | `OnCall = (ko) => Task.FromResult(v)` |
| `.Callback(action)` | Logic inside `OnCall` |
| `.Verify(x, Times.Once)` | `Assert.Equal(1, stub.Method.CallCount)` |
| `.Verify(x, Times.Never)` | `Assert.Equal(0, stub.Method.CallCount)` |
| `It.IsAny<T>()` | Implicit (callback receives all args) |
| `It.Is<T>(pred)` | Check in callback body |

## Step-by-Step Migration

### Step 1: Create Stub Class

**Moq:**
```csharp
var mock = new Mock<IUserService>();
```

**KnockOff (standalone):**
```csharp
// Create once, use across files
[KnockOff]
public partial class UserServiceStub : IUserService { }

// In test
var stub = new UserServiceStub();
```

**KnockOff (inline):**
```csharp
// Scoped to test class
[KnockOff<IUserService>]
public partial class UserTests
{
    [Fact]
    public void Test()
    {
        var stub = new Stubs.IUserService();
    }
}
```

### Step 2: Replace mock.Object

**Moq:**
```csharp
var service = mock.Object;
DoSomething(mock.Object);
```

**KnockOff:**
```csharp
IUserService service = stub;  // Implicit conversion
DoSomething(stub);            // Works directly
```

### Step 3: Convert Setup/Returns

**Moq:**
```csharp
mock.Setup(x => x.GetUser(It.IsAny<int>()))
    .Returns(new User { Id = 1, Name = "Test" });
```

**KnockOff:**
```csharp
stub.GetUser.OnCall = (ko, id) => new User { Id = 1, Name = "Test" };
```

### Step 4: Convert Async Returns

**Moq:**
```csharp
mock.Setup(x => x.GetUserAsync(It.IsAny<int>()))
    .ReturnsAsync(new User { Id = 1 });
```

**KnockOff:**
```csharp
stub.GetUserAsync.OnCall = (ko, id) =>
    Task.FromResult<User?>(new User { Id = 1 });
```

### Step 5: Convert Property Setup

**Moq:**
```csharp
mock.Setup(x => x.Name).Returns("Test");
mock.SetupSet(x => x.Name = It.IsAny<string>()).Verifiable();
```

**KnockOff:**
```csharp
stub.Name.Value = "Test";  // Static value

// Or dynamic
stub.Name.OnGet = (ko) => "Test";

// Track setter
stub.Name.OnSet = (ko, value) => { /* custom logic */ };
```

### Step 6: Convert Verification

**Moq:**
```csharp
mock.Verify(x => x.Save(It.IsAny<User>()), Times.Once);
mock.Verify(x => x.Delete(It.IsAny<int>()), Times.Never);
mock.Verify(x => x.GetAll(), Times.AtLeastOnce);
mock.Verify(x => x.Update(It.IsAny<User>()), Times.Exactly(3));
```

**KnockOff:**
```csharp
Assert.Equal(1, stub.Save.CallCount);      // Times.Once
Assert.Equal(0, stub.Delete.CallCount);    // Times.Never
Assert.True(stub.GetAll.WasCalled);        // Times.AtLeastOnce
Assert.Equal(3, stub.Update.CallCount);    // Times.Exactly(3)
```

### Step 7: Convert Callbacks

**Moq:**
```csharp
User? captured = null;
mock.Setup(x => x.Save(It.IsAny<User>()))
    .Callback<User>(u => captured = u);
```

**KnockOff:**
```csharp
User? captured = null;
stub.Save.OnCall = (ko, user) =>
{
    captured = user;
};

// Or use automatic tracking
// captured = stub.Save.LastCallArg;
```

## Common Patterns

### Conditional Returns

**Moq:**
```csharp
mock.Setup(x => x.GetUser(1)).Returns(new User { Name = "Admin" });
mock.Setup(x => x.GetUser(2)).Returns(new User { Name = "Guest" });
mock.Setup(x => x.GetUser(It.IsAny<int>())).Returns((User?)null);
```

**KnockOff:**
```csharp
stub.GetUser.OnCall = (ko, id) => id switch
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
stub.Connect.OnCall = (ko) => throw new TimeoutException();
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
stub.GetNext.OnCall = (ko) => values.Dequeue();
```

### Argument Matching

**Moq:**
```csharp
var errors = new List<string>();
mock.Setup(x => x.Log(It.Is<string>(s => s.Contains("error"))))
    .Callback<string>(s => errors.Add(s));
```

**KnockOff:**
```csharp
var errors = new List<string>();
stub.Log.OnCall = (ko, message) =>
{
    if (message.Contains("error"))
        errors.Add(message);
};
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
// Create separate stubs for each interface
[KnockOff]
public partial class RepositoryStub : IRepository { }

[KnockOff]
public partial class UnitOfWorkStub : IUnitOfWork { }

var repoStub = new RepositoryStub();
var uowStub = new UnitOfWorkStub();
uowStub.SaveChangesAsync.OnCall = (ko, ct) => Task.FromResult(1);
```

## KnockOff-Only Features

### User Methods (Compile-Time Defaults)

Define shared behavior in the stub class:

```csharp
[KnockOff]
public partial class UserRepositoryStub : IUserRepository
{
    // Used by default unless OnCall is set
    protected User? GetById(int id) => new User { Id = id, Name = "Default" };
}
```

### Automatic Argument Tracking

No explicit callback needed:

```csharp
service.Save(new User { Id = 42 });

// Args captured automatically
Assert.Equal(42, stub.Save.LastCallArg?.Id);

// Multi-param uses named tuple
service.Log("error", "Failed");
Assert.Equal("error", stub.Log.LastCallArgs?.level);
Assert.Equal("Failed", stub.Log.LastCallArgs?.message);
```

### Property Value Backing

```csharp
stub.Name.Value = "PresetValue";
Assert.Equal("PresetValue", service.Name);

// Track access
_ = service.Name;
_ = service.Name;
Assert.Equal(2, stub.Name.GetCount);
```

## Migration Tips

### Start Simple

Begin with verification-only tests:
```csharp
// These translate directly
Assert.True(stub.Method.WasCalled);
Assert.Equal(expectedCount, stub.Method.CallCount);
```

### Use User Methods for Shared Behavior

If multiple tests use the same mock setup, move to user method:

```csharp
// Instead of repeating this in every test:
mock.Setup(x => x.GetById(It.IsAny<int>())).Returns(testUser);

// Define once in stub class:
[KnockOff]
public partial class UserRepoStub : IUserRepository
{
    protected User? GetById(int id) => TestUsers.FirstOrDefault(u => u.Id == id);
}
```

### Leverage Automatic Tracking

Remove Callback boilerplate:

```csharp
// Moq - explicit capture
User? captured = null;
mock.Setup(x => x.Save(It.IsAny<User>())).Callback<User>(u => captured = u);

// KnockOff - automatic
service.Save(user);
var captured = stub.Save.LastCallArg;
```

## Features Not in KnockOff

Keep using Moq for these (can coexist in same project):

| Feature | Notes |
|---------|-------|
| `VerifyNoOtherCalls` | Not supported |
| `mock.Protected()` | Not supported |
| Loose/Strict modes | KnockOff supports strict via `[KnockOff(Strict = true)]` |

## Gradual Migration

Both can coexist in the same project:

```csharp
// New tests use KnockOff
var userStub = new UserServiceStub();

// Legacy tests keep Moq until migrated
var orderMock = new Mock<IOrderService>();
```

Migrate incrementally as you touch tests.
