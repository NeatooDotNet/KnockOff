# KnockOff vs Moq Comparison

KnockOff provides a subset of mocking functionality focused on interface stubbing. This document compares the two approaches for supported scenarios.

> **Note**: Moq is a mature, full-featured mocking framework. KnockOff is narrowly focused on interface stubs with compile-time code generation. Choose the right tool for your needs.

## Approach Comparison

| Aspect | Moq | KnockOff |
|--------|-----|----------|
| Configuration | Runtime fluent API | Compile-time partial class |
| Type safety | Expression-based | Strongly-typed generated classes |
| Setup location | Test method | Partial class (reusable across tests) |
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
| Argument capture | Yes | Yes (automatic) |
| Indexers | Yes | Yes |
| Events | Yes | Yes |
| ref/out parameters | Yes | Not yet |
| Generic methods | Yes | Not yet |
| Setup sequences | Yes | Manual |
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

<!-- snippet: docs:knockoff-vs-moq:basic-stub -->
```csharp
[KnockOff]
public partial class VsUserServiceKnockOff : IVsUserService
{
    protected VsUser GetUser(int id) => new VsUser { Id = id, Name = "Test User" };
}
```
<!-- /snippet -->

```csharp
// Use in test
var knockOff = new VsUserServiceKnockOff();
IVsUserService service = knockOff;

var user = service.GetUser(42);

Assert.Equal(1, knockOff.IVsUserService.GetUser.CallCount);
Assert.Equal(42, knockOff.IVsUserService.GetUser.LastCallArg);
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
// Define stub - properties use auto-generated backing fields
[KnockOff]
public partial class UserServiceKnockOff : IUserService { }

// Use in test
var knockOff = new UserServiceKnockOff();
IUserService service = knockOff;

// Optional: customize getter behavior
knockOff.IUserService.CurrentUser.OnGet = (ko) => new User { Name = "Test" };

var user = service.CurrentUser;
service.CurrentUser = new User { Name = "New" };

Assert.Equal(1, knockOff.IUserService.CurrentUser.GetCount);
Assert.Equal(1, knockOff.IUserService.CurrentUser.SetCount);
Assert.Equal("New", knockOff.IUserService.CurrentUser.LastSetValue?.Name);
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

<!-- snippet: docs:knockoff-vs-moq:async-stub -->
```csharp
[KnockOff]
public partial class VsRepositoryKnockOff : IVsRepository
{
    protected Task<VsEntity?> GetByIdAsync(int id) =>
        Task.FromResult<VsEntity?>(new VsEntity { Id = id });
}
```
<!-- /snippet -->

```csharp
// Use in test
var knockOff = new VsRepositoryKnockOff();
var entity = await knockOff.AsVsRepository().GetByIdAsync(42);
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
// Define stub - arguments are captured automatically
[KnockOff]
public partial class RepositoryKnockOff : IRepository
{
    protected void Save(Entity entity) { /* optional logic */ }
}

// Use in test - no callback setup needed
var knockOff = new RepositoryKnockOff();
IRepository repo = knockOff;

repo.Save(new Entity { Id = 1 });

var captured = knockOff.IRepository.Save.LastCallArg;
Assert.Equal(1, captured?.Id);

// All calls are available
var allSaved = knockOff.IRepository.Save.AllCalls;
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

<!-- snippet: docs:knockoff-vs-moq:multiple-interfaces -->
```csharp
[KnockOff]
public partial class VsEmployeeRepoKnockOff : IVsEmployeeRepository, IVsUnitOfWork
{
    protected Task<int> SaveChangesAsync(CancellationToken ct) => Task.FromResult(1);
}
```
<!-- /snippet -->

```csharp
// Use in tests
var knockOff = new VsEmployeeRepoKnockOff();
IVsEmployeeRepository repo = knockOff.AsVsEmployeeRepository();
IVsUnitOfWork unitOfWork = knockOff.AsVsUnitOfWork();
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

<!-- snippet: docs:knockoff-vs-moq:indexer-stub -->
```csharp
[KnockOff]
public partial class VsPropertyStoreKnockOff : IVsPropertyStore { }
```
<!-- /snippet -->

```csharp
// Use in test - pre-populate backing dictionary
var knockOff = new VsPropertyStoreKnockOff();
knockOff.IVsPropertyStore_StringIndexerBacking["Name"] = new VsPropertyInfo { Value = "Test" };
knockOff.IVsPropertyStore_StringIndexerBacking["Age"] = new VsPropertyInfo { Value = "25" };

IVsPropertyStore store = knockOff;
var name = store["Name"];

Assert.Equal("Name", knockOff.IVsPropertyStore.StringIndexer.LastGetKey);
```

### Event Mocking

**Moq**
```csharp
var mock = new Mock<IEventSource>();

// Raise event
mock.Raise(x => x.DataReceived += null, "test data");

// Verify subscription (requires Callback setup)
bool subscribed = false;
mock.SetupAdd(x => x.DataReceived += It.IsAny<EventHandler<string>>())
    .Callback(() => subscribed = true);
```

**KnockOff**

<!-- snippet: docs:knockoff-vs-moq:event-stub -->
```csharp
[KnockOff]
public partial class VsEventSourceKnockOff : IVsEventSource { }
```
<!-- /snippet -->

```csharp
// Use in test
var knockOff = new VsEventSourceKnockOff();
IVsEventSource source = knockOff;

// Subscribe (tracked automatically)
source.DataReceived += (sender, data) => Console.WriteLine(data);

Assert.True(knockOff.IVsEventSource.DataReceived.HasSubscribers);
Assert.Equal(1, knockOff.IVsEventSource.DataReceived.SubscribeCount);

// Raise event
knockOff.IVsEventSource.DataReceived.Raise("test data");

Assert.True(knockOff.IVsEventSource.DataReceived.WasRaised);
Assert.Equal("test data", knockOff.IVsEventSource.DataReceived.LastRaiseArgs?.e);
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

<!-- snippet: docs:knockoff-vs-moq:verification-patterns -->
```csharp
[KnockOff]
public partial class VsVerificationRepositoryKnockOff : IVsVerificationRepository { }
```
<!-- /snippet -->

```csharp
// Verify in test
Assert.Equal(1, knockOff.IVsVerificationRepository.Save.CallCount);      // Times.Once
Assert.Equal(0, knockOff.IVsVerificationRepository.Delete.CallCount);    // Times.Never
Assert.True(knockOff.IVsVerificationRepository.GetAll.WasCalled);        // Times.AtLeastOnce
Assert.Equal(3, knockOff.IVsVerificationRepository.Update.CallCount);    // Times.Exactly(3)
```

## Dynamic Behavior with Callbacks

For scenarios requiring per-test dynamic behavior, KnockOff provides `OnCall` callbacks:

### Sequential Returns

**Moq**
```csharp
mock.SetupSequence(x => x.GetNext())
    .Returns(1)
    .Returns(2)
    .Returns(3);
```

**KnockOff**

<!-- snippet: docs:knockoff-vs-moq:sequential-returns -->
```csharp
[KnockOff]
public partial class VsSequenceKnockOff : IVsSequence { }
```
<!-- /snippet -->

```csharp
var knockOff = new VsSequenceKnockOff();
var returnValues = new Queue<int>([1, 2, 3]);
knockOff.IVsSequence.GetNext.OnCall = (ko) => returnValues.Dequeue();
```

### Per-Test Overrides

When the stub's default behavior isn't right for a specific test:

<!-- snippet: docs:knockoff-vs-moq:per-test-override -->
```csharp
[KnockOff]
public partial class VsOverrideServiceKnockOff : IVsOverrideService
{
    // Default behavior for most tests
    protected VsUser GetUser(int id) => new VsUser { Id = id, Name = "Default" };
}
```
<!-- /snippet -->

```csharp
[Fact]
public void Test_WithSpecialCase()
{
    var knockOff = new VsOverrideServiceKnockOff();

    // Override just for this test
    knockOff.IVsOverrideService.GetUser.OnCall = (ko, id) => new VsUser { Id = id, Name = "Special" };

    var user = knockOff.AsVsOverrideService().GetUser(42);
    Assert.Equal("Special", user.Name);
}
```

### Reset and Reuse

**KnockOff** (no Moq equivalent - Moq requires new mock)
```csharp
var knockOff = new UserServiceKnockOff();
IUserService service = knockOff;

knockOff.IUserService.GetUser.OnCall = (ko, id) => new User { Name = "First" };
var user1 = service.GetUser(1);

knockOff.IUserService.GetUser.Reset(); // Clears callback and tracking

// Now falls back to user method or default
var user2 = service.GetUser(2);
Assert.Equal(0, knockOff.IUserService.GetUser.CallCount); // Reset cleared count
```

## Callback Priority

When both a user method and callback are defined, the callback takes precedence:

1. **Callback** (if set) — takes precedence
2. **User method** (if defined) — fallback
3. **Default** — `default(T)` for methods, backing field for properties

## When to Use Each

### Consider Moq When

- You need features KnockOff doesn't support (ref/out, generic methods, strict mode)
- Dynamic setup per-test is strongly preferred
- `VerifyNoOtherCalls` is needed
- Your team is already experienced with Moq

### Consider KnockOff When

- Stubs are reused across many tests (define defaults in partial class)
- You prefer compile-time errors over runtime failures
- Debugging generated code is easier than expression trees
- Your test dependencies are primarily interfaces
- You want strongly-typed argument tracking with named tuple access

## Migration Path

For teams migrating from Moq to KnockOff:

1. **Start with verification-only tests** - Tests that only check `WasCalled`/`CallCount` migrate directly
2. **Create stub classes** - Define `[KnockOff]` partial classes for each interface
3. **Add user methods for stable behavior** - Move common `.Returns()` setups to protected methods
4. **Use `OnCall` for dynamic cases** - Sequential returns, test-specific overrides
5. **Defer unsupported features** - Keep Moq for tests using ref/out or generic methods
