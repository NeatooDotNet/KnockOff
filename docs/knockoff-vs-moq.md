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

<!-- pseudo:moq-basic-setup -->
```csharp
var mock = new Mock<IUserService>();
mock.Setup(x => x.GetUser(It.IsAny<int>())).Returns(new User { Id = 1 });

var service = mock.Object;
var user = service.GetUser(42);

mock.Verify(x => x.GetUser(42), Times.Once);
```
<!-- /snippet -->

**KnockOff**

<!-- snippet: knockoff-vs-moq-basic-stub -->
```cs
[KnockOff]
public partial class VsUserServiceKnockOff : IVsUserService
{
    protected VsUser GetUser(int id) => new VsUser { Id = id, Name = "Test User" };
}
```
<!-- endSnippet -->

<!-- snippet: knockoff-vs-moq-basic-stub-usage -->
```cs
// // Use in test
// var knockOff = new VsUserServiceKnockOff();
// IVsUserService service = knockOff;
//
// var user = service.GetUser(42);
//
// Assert.Equal(1, knockOff.GetUser.CallCount);
// Assert.Equal(42, knockOff.GetUser.LastCallArg);
```
<!-- endSnippet -->

### Property Mocking

**Moq**

<!-- pseudo:moq-property-mocking -->
```csharp
var mock = new Mock<IUserService>();
mock.Setup(x => x.CurrentUser).Returns(new User { Name = "Test" });
mock.SetupSet(x => x.CurrentUser = It.IsAny<User>()).Verifiable();

var user = mock.Object.CurrentUser;
mock.Object.CurrentUser = new User { Name = "New" };

mock.VerifySet(x => x.CurrentUser = It.IsAny<User>(), Times.Once);
```
<!-- /snippet -->

**KnockOff**

<!-- snippet: knockoff-vs-moq-property-mocking -->
```cs
// // Define stub - properties use auto-generated backing fields
// [KnockOff]
// public partial class UserServiceKnockOff : IUserService { }
//
// // Use in test
// var knockOff = new UserServiceKnockOff();
// IUserService service = knockOff;
//
// // Optional: customize getter behavior
// knockOff.CurrentUser.OnGet = (ko) => new User { Name = "Test" };
//
// var user = service.CurrentUser;
// service.CurrentUser = new User { Name = "New" };
//
// Assert.Equal(1, knockOff.CurrentUser.GetCount);
// Assert.Equal(1, knockOff.CurrentUser.SetCount);
// Assert.Equal("New", knockOff.CurrentUser.LastSetValue?.Name);
```
<!-- endSnippet -->

### Async Methods

**Moq**

<!-- pseudo:moq-async-methods -->
```csharp
var mock = new Mock<IRepository>();
mock.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
    .ReturnsAsync(new Entity { Id = 1 });

var entity = await mock.Object.GetByIdAsync(42);
```
<!-- /snippet -->

**KnockOff**

<!-- snippet: knockoff-vs-moq-async-stub -->
```cs
[KnockOff]
public partial class VsRepositoryKnockOff : IVsRepository
{
    protected Task<VsEntity?> GetByIdAsync(int id) =>
        Task.FromResult<VsEntity?>(new VsEntity { Id = id });
}
```
<!-- endSnippet -->

<!-- snippet: knockoff-vs-moq-async-stub-usage -->
```cs
// // Use in test
// var knockOff = new VsRepositoryKnockOff();
// var entity = await knockOff.AsIVsRepository().GetByIdAsync(42);
```
<!-- endSnippet -->

### Argument Capture

**Moq**

<!-- pseudo:moq-argument-capture -->
```csharp
Entity? captured = null;
var mock = new Mock<IRepository>();
mock.Setup(x => x.Save(It.IsAny<Entity>()))
    .Callback<Entity>(e => captured = e);

mock.Object.Save(new Entity { Id = 1 });

Assert.Equal(1, captured?.Id);
```
<!-- /snippet -->

**KnockOff**

<!-- snippet: knockoff-vs-moq-argument-capture -->
```cs
// // Define stub - arguments are captured automatically
// [KnockOff]
// public partial class RepositoryKnockOff : IRepository
// {
//     protected void Save(Entity entity) { /* optional logic */ }
// }
//
// // Use in test - no callback setup needed
// var knockOff = new RepositoryKnockOff();
// IRepository repo = knockOff;
//
// repo.Save(new Entity { Id = 1 });
//
// var captured = knockOff.Save.LastCallArg;
// Assert.Equal(1, captured?.Id);
```
<!-- endSnippet -->

### Multiple Interface Implementation

**Moq**

<!-- pseudo:moq-multiple-interfaces -->
```csharp
var mock = new Mock<IEmployeeRepository>();
mock.As<IUnitOfWork>()
    .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
    .ReturnsAsync(1);

var repo = mock.Object;
var unitOfWork = mock.As<IUnitOfWork>().Object;
```
<!-- /snippet -->

**KnockOff**

<!-- snippet: knockoff-vs-moq-multiple-interfaces -->
```cs
[KnockOff]
public partial class VsEmployeeRepoKnockOff : IVsEmployeeRepository
{
}

[KnockOff]
public partial class VsUnitOfWorkKnockOff : IVsUnitOfWork
{
    protected Task<int> SaveChangesAsync(CancellationToken ct) => Task.FromResult(1);
}
```
<!-- endSnippet -->

<!-- snippet: knockoff-vs-moq-multiple-interfaces-usage -->
```cs
// // Use in tests - separate stubs for each interface
// var repoKnockOff = new VsEmployeeRepoKnockOff();
// IVsEmployeeRepository repo = repoKnockOff.AsIVsEmployeeRepository();
//
// var uowKnockOff = new VsUnitOfWorkKnockOff();
// IVsUnitOfWork unitOfWork = uowKnockOff.AsIVsUnitOfWork();
```
<!-- endSnippet -->

### Indexer Mocking

**Moq**

<!-- pseudo:moq-indexer-mocking -->
```csharp
var mock = new Mock<IPropertyStore>();
mock.Setup(x => x["Name"]).Returns(new PropertyInfo { Value = "Test" });
mock.Setup(x => x["Age"]).Returns(new PropertyInfo { Value = "25" });

var name = mock.Object["Name"];
```
<!-- /snippet -->

**KnockOff**

<!-- snippet: knockoff-vs-moq-indexer-stub -->
```cs
[KnockOff]
public partial class VsPropertyStoreKnockOff : IVsPropertyStore { }
```
<!-- endSnippet -->

<!-- snippet: knockoff-vs-moq-indexer-stub-usage -->
```cs
// // Use in test - pre-populate backing dictionary
// var knockOff = new VsPropertyStoreKnockOff();
// knockOff.StringIndexerBacking["Name"] = new VsPropertyInfo { Value = "Test" };
// knockOff.StringIndexerBacking["Age"] = new VsPropertyInfo { Value = "25" };
//
// IVsPropertyStore store = knockOff;
// var name = store["Name"];
//
// Assert.Equal("Name", knockOff.StringIndexer.LastGetKey);
```
<!-- endSnippet -->

### Event Mocking

**Moq**

<!-- pseudo:moq-event-mocking -->
```csharp
var mock = new Mock<IEventSource>();

// Raise event
mock.Raise(x => x.DataReceived += null, "test data");

// Verify subscription (requires Callback setup)
bool subscribed = false;
mock.SetupAdd(x => x.DataReceived += It.IsAny<EventHandler<string>>())
    .Callback(() => subscribed = true);
```
<!-- /snippet -->

**KnockOff**

<!-- snippet: knockoff-vs-moq-event-stub -->
```cs
[KnockOff]
public partial class VsEventSourceKnockOff : IVsEventSource { }
```
<!-- endSnippet -->

<!-- snippet: knockoff-vs-moq-event-stub-usage -->
```cs
// // Use in test
// var knockOff = new VsEventSourceKnockOff();
// IVsEventSource source = knockOff;
//
// string? receivedData = null;
//
// // Subscribe (tracked automatically)
// source.DataReceived += (sender, data) => receivedData = data;
//
// Assert.True(knockOff.DataReceived.HasSubscribers);
// Assert.Equal(1, knockOff.DataReceived.AddCount);
//
// // Raise event (requires sender parameter for EventHandler<T>)
// knockOff.DataReceived.Raise(null, "test data");
//
// Assert.Equal("test data", receivedData);
```
<!-- endSnippet -->

### Verification Patterns

**Moq**

<!-- pseudo:moq-verification-patterns -->
```csharp
mock.Verify(x => x.Save(It.IsAny<Entity>()), Times.Once);
mock.Verify(x => x.Delete(It.IsAny<int>()), Times.Never);
mock.Verify(x => x.GetAll(), Times.AtLeastOnce);
mock.Verify(x => x.Update(It.IsAny<Entity>()), Times.Exactly(3));
```
<!-- /snippet -->

**KnockOff**

<!-- snippet: knockoff-vs-moq-verification-patterns -->
```cs
[KnockOff]
public partial class VsVerificationRepositoryKnockOff : IVsVerificationRepository { }
```
<!-- endSnippet -->

<!-- snippet: knockoff-vs-moq-verification-patterns-usage -->
```cs
// // Verify in test
// Assert.Equal(1, knockOff.Save.CallCount);      // Times.Once
// Assert.Equal(0, knockOff.Delete.CallCount);    // Times.Never
// Assert.True(knockOff.GetAll.WasCalled);        // Times.AtLeastOnce
// Assert.Equal(3, knockOff.Update.CallCount);    // Times.Exactly(3)
```
<!-- endSnippet -->

## Dynamic Behavior with Callbacks

For scenarios requiring per-test dynamic behavior, KnockOff provides `OnCall` callbacks:

### Sequential Returns

**Moq**

<!-- pseudo:moq-sequential-returns -->
```csharp
mock.SetupSequence(x => x.GetNext())
    .Returns(1)
    .Returns(2)
    .Returns(3);
```
<!-- /snippet -->

**KnockOff**

<!-- snippet: knockoff-vs-moq-sequential-returns -->
```cs
[KnockOff]
public partial class VsSequenceKnockOff : IVsSequence { }
```
<!-- endSnippet -->

<!-- snippet: knockoff-vs-moq-sequential-returns-usage -->
```cs
// var knockOff = new VsSequenceKnockOff();
// var returnValues = new Queue<int>([1, 2, 3]);
// knockOff.GetNext.OnCall = (ko) => returnValues.Dequeue();
```
<!-- endSnippet -->

### Per-Test Overrides

When the stub's default behavior isn't right for a specific test:

<!-- snippet: knockoff-vs-moq-per-test-override -->
```cs
[KnockOff]
public partial class VsOverrideServiceKnockOff : IVsOverrideService
{
    // Default behavior for most tests
    protected VsUser GetUser(int id) => new VsUser { Id = id, Name = "Default" };
}
```
<!-- endSnippet -->

<!-- snippet: knockoff-vs-moq-per-test-override-usage -->
```cs
// [Fact]
// public void Test_WithSpecialCase()
// {
//     var knockOff = new VsOverrideServiceKnockOff();
//
//     // Override just for this test
//     knockOff.GetUser.OnCall = (ko, id) => new VsUser { Id = id, Name = "Special" };
//
//     var user = knockOff.AsIVsOverrideService().GetUser(42);
//     Assert.Equal("Special", user.Name);
// }
```
<!-- endSnippet -->

### Reset and Reuse

**KnockOff** (no Moq equivalent - Moq requires new mock)

<!-- snippet: knockoff-vs-moq-reset-and-reuse -->
```cs
// var knockOff = new UserServiceKnockOff();
// IUserService service = knockOff;
//
// knockOff.GetUser.OnCall = (ko, id) => new User { Name = "First" };
// var user1 = service.GetUser(1);
//
// knockOff.GetUser.Reset(); // Clears callback and tracking
//
// // Now falls back to user method or default
// var user2 = service.GetUser(2);
// Assert.Equal(0, knockOff.GetUser.CallCount); // Reset cleared count
```
<!-- endSnippet -->

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
