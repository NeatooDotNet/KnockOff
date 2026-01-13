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

<!-- snippet: moq-basic-setup -->
```cs
[Fact]
public void BasicSetupAndVerification()
{
    var mock = new Mock<IMoqUserService>();
    mock.Setup(x => x.GetUser(It.IsAny<int>())).Returns(new MoqUser { Id = 1 });

    var service = mock.Object;
    var user = service.GetUser(42);

    mock.Verify(x => x.GetUser(42), Times.Once);
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

<!-- snippet: moq-property-mocking -->
```cs
[Fact]
public void PropertyMocking()
{
    var mock = new Mock<IMoqUserService>();
    mock.Setup(x => x.CurrentUser).Returns(new MoqUser { Name = "Test" });
    mock.SetupSet(x => x.CurrentUser = It.IsAny<MoqUser>()).Verifiable();

    var user = mock.Object.CurrentUser;
    mock.Object.CurrentUser = new MoqUser { Name = "New" };

    mock.VerifySet(x => x.CurrentUser = It.IsAny<MoqUser>(), Times.Once);
}
```
<!-- endSnippet -->

### Async Methods

**Moq**

<!-- snippet: moq-async-methods -->
```cs
[Fact]
public async Task AsyncMethods()
{
    var mock = new Mock<IMoqRepository>();
    mock.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
        .ReturnsAsync(new MoqEntity { Id = 1 });

    var entity = await mock.Object.GetByIdAsync(42);

    Assert.Equal(1, entity?.Id);
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

<!-- snippet: moq-argument-capture -->
```cs
[Fact]
public void ArgumentCapture()
{
    MoqEntity? captured = null;
    var mock = new Mock<IMoqRepository>();
    mock.Setup(x => x.Save(It.IsAny<MoqEntity>()))
        .Callback<MoqEntity>(e => captured = e);

    mock.Object.Save(new MoqEntity { Id = 1 });

    Assert.Equal(1, captured?.Id);
}
```
<!-- endSnippet -->

### Multiple Interface Implementation

**Moq**

<!-- snippet: moq-multiple-interfaces -->
```cs
[Fact]
public async Task MultipleInterfaces()
{
    var mock = new Mock<IMoqEmployeeRepository>();
    mock.As<IMoqUnitOfWork>()
        .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(1);

    var repo = mock.Object;
    var unitOfWork = mock.As<IMoqUnitOfWork>().Object;

    var result = await unitOfWork.SaveChangesAsync(CancellationToken.None);
    Assert.Equal(1, result);
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

<!-- snippet: moq-indexer-mocking -->
```cs
[Fact]
public void IndexerMocking()
{
    var mock = new Mock<IMoqPropertyStore>();
    mock.Setup(x => x["Name"]).Returns(new MoqPropertyInfo { Value = "Test" });
    mock.Setup(x => x["Age"]).Returns(new MoqPropertyInfo { Value = "25" });

    var name = mock.Object["Name"];

    Assert.Equal("Test", name?.Value);
}
```
<!-- endSnippet -->

<!-- snippet: knockoff-vs-moq-indexer-stub-usage -->
```cs
// // Use in test - pre-populate backing dictionary
// var knockOff = new VsPropertyStoreKnockOff();
// knockOff.Indexer.Backing["Name"] = new VsPropertyInfo { Value = "Test" };
// knockOff.Indexer.Backing["Age"] = new VsPropertyInfo { Value = "25" };
//
// IVsPropertyStore store = knockOff;
// var name = store["Name"];
//
// Assert.Equal("Name", knockOff.Indexer.LastGetKey);
```
<!-- endSnippet -->

### Event Mocking

**Moq**

<!-- snippet: moq-event-mocking -->
```cs
[Fact]
public void EventMocking()
{
    var mock = new Mock<IMoqEventSource>();

    string? receivedData = null;
    mock.Object.DataReceived += (sender, data) => receivedData = data;

    // Raise event
    mock.Raise(x => x.DataReceived += null, mock.Object, "test data");

    Assert.Equal("test data", receivedData);
}
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

<!-- snippet: moq-verification-patterns -->
```cs
[Fact]
public void VerificationPatterns()
{
    var mock = new Mock<IMoqRepository>();

    mock.Object.Save(new MoqEntity());
    mock.Object.GetAll();
    mock.Object.Update(new MoqEntity());
    mock.Object.Update(new MoqEntity());
    mock.Object.Update(new MoqEntity());

    mock.Verify(x => x.Save(It.IsAny<MoqEntity>()), Times.Once);
    mock.Verify(x => x.Delete(It.IsAny<int>()), Times.Never);
    mock.Verify(x => x.GetAll(), Times.AtLeastOnce);
    mock.Verify(x => x.Update(It.IsAny<MoqEntity>()), Times.Exactly(3));
}
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

<!-- snippet: moq-sequential-returns -->
```cs
[Fact]
public void SequentialReturns()
{
    var mock = new Mock<IMoqSequence>();
    mock.SetupSequence(x => x.GetNext())
        .Returns(1)
        .Returns(2)
        .Returns(3);

    Assert.Equal(1, mock.Object.GetNext());
    Assert.Equal(2, mock.Object.GetNext());
    Assert.Equal(3, mock.Object.GetNext());
}
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
