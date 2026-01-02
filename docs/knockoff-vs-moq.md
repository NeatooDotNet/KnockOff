# KnockOff vs Moq Comparison

KnockOff provides a subset of mocking functionality focused on interface stubbing. This document compares the two approaches for supported scenarios.

> **Note**: Moq is a mature, full-featured mocking framework. KnockOff is narrowly focused on interface stubs with compile-time code generation. Choose the right tool for your needs.

## Approach Comparison

| Aspect | Moq | KnockOff |
|--------|-----|----------|
| Configuration | Runtime fluent API | Compile-time source generation |
| Type safety | Expression-based | Strongly-typed generated classes |
| Setup location | Test method only | Partial class (defaults) + callbacks (per-test) |
| Flexibility | High (dynamic proxy) | Lower (generated code) |
| Debugging | Expression trees | Standard C# code |

## The Duality: Two Ways to Customize

Unlike Moq which only offers runtime configuration, KnockOff provides **two complementary patterns**:

### Pattern 1: User-Defined Methods (Compile-Time Defaults)

```csharp
[KnockOff]
public partial class UserServiceKnockOff : IUserService
{
    // Default behavior for all tests using this stub
    protected User GetUser(int id) => new User { Id = id, Name = "Default" };
}
```

### Pattern 2: Callbacks (Runtime Overrides)

```csharp
// Override for this specific test
knockOff.Spy.GetUser.OnCall = (ko, id) => new User { Id = id, Name = "Custom" };
```

### Priority Order

1. **Callback** (if set) — takes precedence
2. **User method** (if defined) — fallback
3. **Default** — `default(T)` for methods, backing field for properties

### When to Use Each

| Scenario | Moq | KnockOff |
|----------|-----|----------|
| Same behavior across tests | Setup in shared method | User-defined method |
| Per-test behavior | Setup in test | OnCall callback |
| Static return value | `.Returns(value)` | User method or callback |
| Dynamic return | `.Returns((args) => ...)` | OnCall callback |

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
| Indexers | Yes | Yes |
| Events | Yes | Not yet |
| ref/out parameters | Yes | Not yet |
| Generic methods | Yes | Not yet |
| Setup sequences | Yes | Manual (queue in callback) |
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
// Define stub once (typically in a shared file)
[KnockOff]
public partial class UserServiceKnockOff : IUserService { }

// Use in test
var knockOff = new UserServiceKnockOff();
knockOff.Spy.GetUser.OnCall = (ko, id) => new User { Id = id };

IUserService service = knockOff;
var user = service.GetUser(42);

Assert.Equal(1, knockOff.Spy.GetUser.CallCount);
Assert.Equal(42, knockOff.Spy.GetUser.LastCallArg);
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
// Define stub
[KnockOff]
public partial class UserServiceKnockOff : IUserService { }

// Use in test
var knockOff = new UserServiceKnockOff();
knockOff.Spy.CurrentUser.OnGet = (ko) => new User { Name = "Test" };

IUserService service = knockOff;
var user = service.CurrentUser;
service.CurrentUser = new User { Name = "New" };

Assert.Equal(1, knockOff.Spy.CurrentUser.GetCount);
Assert.Equal(1, knockOff.Spy.CurrentUser.SetCount);
Assert.Equal("New", knockOff.Spy.CurrentUser.LastSetValue?.Name);
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
// Define stub
[KnockOff]
public partial class RepositoryKnockOff : IRepository { }

// Use in test
var knockOff = new RepositoryKnockOff();
knockOff.Spy.GetByIdAsync.OnCall = (ko, id) =>
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
// Define stub
[KnockOff]
public partial class RepositoryKnockOff : IRepository { }

// Use in test - no setup needed, arguments captured automatically
var knockOff = new RepositoryKnockOff();
IRepository repo = knockOff;

repo.Save(new Entity { Id = 1 });

var captured = knockOff.Spy.Save.LastCallArg;
Assert.Equal(1, captured?.Id);

// All calls are available
var allSaved = knockOff.Spy.Save.AllCalls;
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
knockOff.Spy.SaveChangesAsync.OnCall = (ko, ct) => Task.FromResult(1);

IEmployeeRepository repo = knockOff.AsEmployeeRepository();
IUnitOfWork unitOfWork = knockOff.AsUnitOfWork();
```

### Indexer Mocking

**Moq**
```csharp
var mock = new Mock<IPropertyStore>();
mock.Setup(x => x["Name"]).Returns(new PropertyInfo { Value = "Test" });
mock.Setup(x => x["Age"]).Returns(new PropertyInfo { Value = "25" });

var name = mock.Object["Name"];
```

**KnockOff**
```csharp
// Define stub
[KnockOff]
public partial class PropertyStoreKnockOff : IPropertyStore { }

// Use in test
var knockOff = new PropertyStoreKnockOff();

// Option 1: Pre-populate backing dictionary
knockOff.StringIndexerBacking["Name"] = new PropertyInfo { Value = "Test" };
knockOff.StringIndexerBacking["Age"] = new PropertyInfo { Value = "25" };

// Option 2: Use callback for dynamic behavior
knockOff.Spy.StringIndexer.OnGet = (ko, key) => key switch
{
    "Name" => new PropertyInfo { Value = "Test" },
    "Age" => new PropertyInfo { Value = "25" },
    _ => null
};

IPropertyStore store = knockOff;
var name = store["Name"];

Assert.Equal("Name", knockOff.Spy.StringIndexer.LastGetKey);
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
// Define stub
[KnockOff]
public partial class RepositoryKnockOff : IRepository { }

// Verify in test
Assert.Equal(1, knockOff.Spy.Save.CallCount);      // Times.Once
Assert.Equal(0, knockOff.Spy.Delete.CallCount);    // Times.Never
Assert.True(knockOff.Spy.GetAll.WasCalled);        // Times.AtLeastOnce
Assert.Equal(3, knockOff.Spy.Update.CallCount);    // Times.Exactly(3)
```

### Static Return via User Method

KnockOff supports defining behavior in the partial class for consistent returns:

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

### Sequential Returns

**Moq**
```csharp
mock.SetupSequence(x => x.GetNext())
    .Returns(1)
    .Returns(2)
    .Returns(3);
```

**KnockOff**
```csharp
// Define stub
[KnockOff]
public partial class SequenceKnockOff : ISequence { }

// Use in test
var knockOff = new SequenceKnockOff();
var returnValues = new Queue<int>([1, 2, 3]);
knockOff.Spy.GetNext.OnCall = (ko) => returnValues.Dequeue();
```

### Reset and Reuse

**KnockOff** (no Moq equivalent - Moq requires new mock)
```csharp
// Define stub
[KnockOff]
public partial class UserServiceKnockOff : IUserService { }

// Use in test
var knockOff = new UserServiceKnockOff();
IUserService service = knockOff;

knockOff.Spy.GetUser.OnCall = (ko, id) => new User { Name = "First" };
var user1 = service.GetUser(1);

knockOff.Spy.GetUser.Reset(); // Clears callback and tracking

// Now falls back to user method or default
var user2 = service.GetUser(2);
Assert.Equal(0, knockOff.Spy.GetUser.CallCount); // Reset cleared count
```

## When to Use Each

### Consider Moq When

- You need features KnockOff doesn't support (events, ref/out, strict mode)
- Dynamic setup per-test is strongly preferred
- `VerifyNoOtherCalls` is needed
- Your team is already experienced with Moq

### Consider KnockOff When

- Stubs are reused across many tests (define defaults in partial class)
- You prefer compile-time errors over runtime failures
- Debugging generated code is easier than expression trees
- Your test dependencies are primarily interfaces
- You want strongly-typed argument tracking with named tuple access
- You want the layered customization of user methods + callbacks

## Migration Path

For teams migrating from Moq to KnockOff:

1. **Start with verification-only tests** - Tests that only check `WasCalled`/`CallCount` migrate directly
2. **Add `OnCall` for returns** - Replace `.Setup().Returns()` with `OnCall` callbacks
3. **Use partial class for stable stubs** - Move common setups to protected methods
4. **Defer unsupported features** - Keep Moq for tests using events or ref/out

## But Wait, I Just Want Moq-Style Per-Test Setup

If you prefer Moq's per-test configuration style, KnockOff supports that too. Just define an empty stub and use callbacks:

```csharp
// Define once - no behavior, just the stub
[KnockOff]
public partial class UserServiceKnockOff : IUserService { }

// Configure per-test, just like Moq
[Fact]
public void Test_WithMoqStyle()
{
    var knockOff = new UserServiceKnockOff();

    // This feels like Moq's Setup().Returns()
    knockOff.Spy.GetUser.OnCall = (ko, id) => new User { Id = id, Name = "Mocked" };
    knockOff.Spy.Name.OnGet = (ko) => "Test Name";

    IUserService service = knockOff;
    var user = service.GetUser(42);

    Assert.Equal("Mocked", user.Name);
    Assert.Equal(1, knockOff.Spy.GetUser.CallCount);
}
```

The difference from Moq: callbacks are simple delegate assignments, not expression trees. You get IntelliSense, compile-time checking, and easier debugging.
