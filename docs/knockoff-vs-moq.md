# KnockOff vs Moq Comparison

KnockOff provides a subset of mocking functionality focused on interface stubbing. This document compares the two approaches for supported scenarios.

> **Note**: Moq is a mature, full-featured mocking framework. KnockOff is narrowly focused on interface stubs with compile-time code generation. Choose the right tool for your needs.

## Approach Comparison

| Aspect | Moq | KnockOff |
|--------|-----|----------|
| Configuration | Runtime fluent API | Compile-time source generation |
| Type safety | Expression-based | Strongly-typed generated classes |
| Setup location | Test method | Partial class + callbacks |
| Flexibility | High (dynamic proxy) | Lower (generated code) |
| Debugging | Expression trees | Standard C# code |

## Feature Support Matrix

| Feature | Moq | KnockOff |
|---------|-----|----------|
| Properties (get/set) | Yes | Yes |
| Void methods | Yes | Yes |
| Methods with return values | Yes | Yes |
| Async methods (Task, ValueTask) | Yes | Yes |
| Generic interfaces | Yes | Yes |
| Multiple interfaces | Yes | Yes |
| Interface inheritance | Yes | Yes |
| Call verification | Yes | Yes |
| Argument capture | Yes | Yes |
| Dynamic return values | Yes | Yes (OnCall) |
| Indexers | Yes | Not yet |
| Events | Yes | Not yet |
| ref/out parameters | Yes | Not yet |
| Generic methods | Yes | Not yet |
| Setup sequences | Yes | Not yet |
| Strict/Loose modes | Yes | No |

## Side-by-Side Examples

### Basic Setup and Verification

**Moq**
```csharp
var mock = new Mock<IUserService>();
mock.Setup(x => x.GetUser(It.IsAny<int>())).Returns(new User { Id = 1 });

var service = mock.Object;
var user = service.GetUser(42);

mock.Verify(x => x.GetUser(42), Times.Once);
```

**KnockOff**
```csharp
var knockOff = new UserServiceKnockOff();
knockOff.ExecutionInfo.GetUser.OnCall = (ko, id) => new User { Id = id };

IUserService service = knockOff;
var user = service.GetUser(42);

Assert.Equal(1, knockOff.ExecutionInfo.GetUser.CallCount);
Assert.Equal(42, knockOff.ExecutionInfo.GetUser.LastCallArg);
```

### Property Mocking

**Moq**
```csharp
var mock = new Mock<IUserService>();
mock.Setup(x => x.CurrentUser).Returns(new User { Name = "Test" });
mock.SetupSet(x => x.CurrentUser = It.IsAny<User>()).Verifiable();

var user = mock.Object.CurrentUser;
mock.Object.CurrentUser = new User { Name = "New" };

mock.VerifySet(x => x.CurrentUser = It.IsAny<User>(), Times.Once);
```

**KnockOff**
```csharp
var knockOff = new UserServiceKnockOff();
knockOff.ExecutionInfo.CurrentUser.OnGet = (ko) => new User { Name = "Test" };

var user = knockOff.AsUserService().CurrentUser;
knockOff.AsUserService().CurrentUser = new User { Name = "New" };

Assert.Equal(1, knockOff.ExecutionInfo.CurrentUser.GetCount);
Assert.Equal(1, knockOff.ExecutionInfo.CurrentUser.SetCount);
Assert.Equal("New", knockOff.ExecutionInfo.CurrentUser.LastSetValue?.Name);
```

### Async Methods

**Moq**
```csharp
var mock = new Mock<IRepository>();
mock.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
    .ReturnsAsync(new Entity { Id = 1 });

var entity = await mock.Object.GetByIdAsync(42);
```

**KnockOff**
```csharp
var knockOff = new RepositoryKnockOff();
knockOff.ExecutionInfo.GetByIdAsync.OnCall = (ko, id) =>
    Task.FromResult<Entity?>(new Entity { Id = id });

var entity = await knockOff.AsRepository().GetByIdAsync(42);
```

### Argument Capture

**Moq**
```csharp
Entity? captured = null;
var mock = new Mock<IRepository>();
mock.Setup(x => x.Save(It.IsAny<Entity>()))
    .Callback<Entity>(e => captured = e);

mock.Object.Save(new Entity { Id = 1 });

Assert.Equal(1, captured?.Id);
```

**KnockOff**
```csharp
var knockOff = new RepositoryKnockOff();
IRepository repo = knockOff;

repo.Save(new Entity { Id = 1 });

var captured = knockOff.ExecutionInfo.Save.LastCallArg;
Assert.Equal(1, captured?.Id);

// All calls are available
var allSaved = knockOff.ExecutionInfo.Save.AllCalls;
```

### Multiple Interface Implementation

**Moq**
```csharp
var mock = new Mock<IEmployeeRepository>();
mock.As<IUnitOfWork>()
    .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
    .ReturnsAsync(1);

var repo = mock.Object;
var unitOfWork = mock.As<IUnitOfWork>().Object;
```

**KnockOff**
```csharp
// Define once
[KnockOff]
public partial class EmployeeRepoKnockOff : IEmployeeRepository, IUnitOfWork { }

// Use in tests
var knockOff = new EmployeeRepoKnockOff();
knockOff.ExecutionInfo.SaveChangesAsync.OnCall = (ko, ct) => Task.FromResult(1);

IEmployeeRepository repo = knockOff.AsEmployeeRepository();
IUnitOfWork unitOfWork = knockOff.AsUnitOfWork();
```

### Verification Patterns

**Moq**
```csharp
mock.Verify(x => x.Save(It.IsAny<Entity>()), Times.Once);
mock.Verify(x => x.Delete(It.IsAny<int>()), Times.Never);
mock.Verify(x => x.GetAll(), Times.AtLeastOnce);
mock.Verify(x => x.Update(It.IsAny<Entity>()), Times.Exactly(3));
```

**KnockOff**
```csharp
Assert.Equal(1, knockOff.ExecutionInfo.Save.CallCount);      // Times.Once
Assert.Equal(0, knockOff.ExecutionInfo.Delete.CallCount);    // Times.Never
Assert.True(knockOff.ExecutionInfo.GetAll.WasCalled);        // Times.AtLeastOnce
Assert.Equal(3, knockOff.ExecutionInfo.Update.CallCount);    // Times.Exactly(3)
```

### Static Return via User Method

KnockOff supports defining behavior in the partial class for static returns:

**KnockOff**
```csharp
[KnockOff]
public partial class UserServiceKnockOff : IUserService
{
    // Generator calls this when IUserService.GetUser is invoked
    protected User GetUser(int id) => new User { Id = id, Name = "Test User" };
}

// Test usage - no callback setup needed
var knockOff = new UserServiceKnockOff();
var user = knockOff.AsUserService().GetUser(42);

Assert.Equal(42, user.Id);
```

## When to Use Each

### Consider Moq When

- You need features KnockOff doesn't support (events, indexers, ref/out, sequences)
- Dynamic setup per-test is preferred over partial class definitions
- Strict mock mode or `VerifyNoOtherCalls` is needed
- Your team is already experienced with Moq

### Consider KnockOff When

- Stubs are reused across many tests (define once in partial class)
- You prefer compile-time errors over runtime failures
- Debugging generated code is easier for your team than expression trees
- Your test dependencies are primarily interfaces
- You want strongly-typed argument tracking with named tuple access

## Generated Code Structure

KnockOff generates explicit interface implementations with tracking:

```csharp
// Generated code (simplified)
partial class UserServiceKnockOff
{
    public UserServiceKnockOffExecutionInfo ExecutionInfo { get; } = new();

    // Generated ExecutionDetails for each member
    public sealed class GetUserExecutionDetails
    {
        public int CallCount => _calls.Count;
        public bool WasCalled => _calls.Count > 0;
        public int? LastCallArg => ...;
        public IReadOnlyList<int> AllCalls => _calls;
        public Func<UserServiceKnockOff, int, User>? OnCall { get; set; }
        public void Reset() { ... }
    }

    // Explicit implementation delegates to user method or callback
    User IUserService.GetUser(int id)
    {
        ExecutionInfo.GetUser.RecordCall(id);
        if (ExecutionInfo.GetUser.OnCall is { } callback)
            return callback(this, id);
        return GetUser(id); // User-defined method
    }
}
```

## Migration Path

For teams migrating from Moq to KnockOff:

1. **Start with verification-only tests** - Tests that only check `WasCalled`/`CallCount` migrate directly
2. **Add `OnCall` for returns** - Replace `.Setup().Returns()` with `OnCall` callbacks
3. **Use partial class for stable stubs** - Move common setups to `protected` methods
4. **Defer unsupported features** - Keep Moq for tests using events, indexers, or ref/out
