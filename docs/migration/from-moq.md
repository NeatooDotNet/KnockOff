# Migrating from Moq to KnockOff

Switching from Moq to KnockOff means moving from per-test mock setup to reusable stub classes. You gain the ability to share stubs across tests while still customizing behavior per-test—while trading Moq's runtime flexibility for source-generated, explicit stub implementations.

This guide walks you through the migration step-by-step, with side-by-side comparisons and a complete before/after example.

---

## What Changes

**Moq's approach:**
- Runtime reflection with fluent `.Setup()` API
- `Mock<T>` wrapper objects
- `.Object` property to access the instance
- `.Verify()` methods for call assertions

**KnockOff's approach:**
- Compile-time source generation with partial classes
- Direct stub classes with `[KnockOff<T>]` attribute
- Interceptor properties for configuration and verification
- Standard assertions on call tracking properties

**What stays the same:**
- You still create test doubles for interfaces and classes
- You still configure behavior and verify calls
- Your test goals and patterns remain unchanged

---

## Quick Reference

| Moq Pattern | KnockOff Equivalent |
|-------------|---------------------|
| `new Mock<IFoo>()` | `new FooStub()` with `[KnockOff] partial class FooStub : IFoo` |
| `mock.Object` | `stub` (direct instance) |
| `.Setup(x => x.Method()).Returns(value)` | `stub.Method.OnCall(() => value)` |
| `.Setup(x => x.Property).Returns(value)` | `stub.Property.OnGet(value)` |
| `.ReturnsAsync(value)` | `stub.Method.OnCall(() => Task.FromResult(value))` |
| `.Callback(x => ...)` | Logic in `OnCall` delegate |
| `.Verify(x => x.Method(), Times.Once)` | `var t = stub.Method.OnCall(...); t.Verify(Times.Once)` |
| `.Verifiable()` | `stub.Method.OnCall(...).Verifiable()` |
| `mock.Verify()` | `stub.Verify()` (checks all `.Verifiable()` calls) |
| `It.IsAny<T>()` | Callback receives all arguments for inspection |

---

## Step 1: Install KnockOff

Replace the Moq package with KnockOff.

```bash
# Remove Moq:
dotnet remove package Moq

# Add KnockOff:
dotnet add package KnockOff
```

---

## Step 2: Create Stubs

Replace `Mock<T>` instances with KnockOff stub classes.

**Moq:**

<!-- snippet: moq-migration-create-stub-moq -->
```cs
[Fact]
public void CreateStub_MoqApproach()
{
    var mock = new Mock<IMoqUserRepo>();
    IMoqUserRepo repository = mock.Object;

    Assert.NotNull(repository);
}
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: moq-migration-create-stub-knockoff -->
```cs
[Fact]
public void CreateStub_KnockOffApproach()
{
    var stub = new MoqUserRepoStub();
    IMoqUserRepo repository = stub;

    Assert.NotNull(repository);
}
```
<!-- endSnippet -->

**Key differences:**
- Moq wraps test doubles in `Mock<T>` objects
- KnockOff uses partial class declarations with source generation
- You use the stub instance directly (no `.Object` property)

**Stub class declaration:**

To enable the above code, declare a partial stub class with the `[KnockOff]` attribute. The source generator creates the implementation.

<!-- snippet: moq-migration-stub-declaration -->
```cs
[KnockOff]
public partial class MoqUserRepoStub : IMoqUserRepo { }
```
<!-- endSnippet -->

Place this declaration in your test file. The generator fills in the explicit interface implementations and interceptor properties.

**Alternative:** You can also use the inline pattern `[KnockOff<IFoo>] partial class FooStub` without implementing the interface—both patterns work the same way. This guide uses the standalone pattern for consistency with Moq's explicit interface usage.

---

## Step 3: Configure Methods

Replace `.Setup().Returns()` with `OnCall` property assignments.

**Moq:**

<!-- snippet: moq-migration-setup-method-moq -->
```cs
[Fact]
public void SetupMethod_MoqApproach()
{
    var mock = new Mock<IMoqUserRepo>();
    var testUser = new User { Id = 42, Name = "Alice" };

    mock.Setup(x => x.GetUser(It.IsAny<int>())).Returns(testUser);

    IMoqUserRepo repository = mock.Object;
    var user = repository.GetUser(42);

    Assert.NotNull(user);
    Assert.Equal("Alice", user.Name);
}
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: moq-migration-setup-method-knockoff -->
```cs
[Fact]
public void SetupMethod_KnockOffApproach()
{
    var stub = new MoqUserRepoStub();
    var testUser = new User { Id = 42, Name = "Alice" };

    stub.GetUser.OnCall((id) => testUser);

    IMoqUserRepo repository = stub;
    var user = repository.GetUser(42);

    Assert.NotNull(user);
    Assert.Equal("Alice", user.Name);
}
```
<!-- endSnippet -->

**Key differences:**
- Moq uses fluent setup with expression trees
- KnockOff uses direct property assignment with typed delegates
- KnockOff's callback receives actual argument values (no `It.IsAny<T>()` needed)

---

## Step 4: Configure Properties

Replace property `.Setup().Returns()` with `.Value` assignments.

**Moq:**

<!-- snippet: moq-migration-setup-property-moq -->
```cs
[Fact]
public void SetupProperty_MoqApproach()
{
    var mock = new Mock<IMoqUserRepo>();

    mock.Setup(x => x.ConnectionString).Returns("server=localhost");

    IMoqUserRepo repository = mock.Object;
    var connStr = repository.ConnectionString;

    Assert.Equal("server=localhost", connStr);
}
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: moq-migration-setup-property-knockoff -->
```cs
[Fact]
public void SetupProperty_KnockOffApproach()
{
    var stub = new MoqUserRepoStub();

    stub.ConnectionString.OnGet("server=localhost");

    IMoqUserRepo repository = stub;
    var connStr = repository.ConnectionString;

    Assert.Equal("server=localhost", connStr);
}
```
<!-- endSnippet -->

**Key differences:**
- Moq treats properties like methods in setup
- KnockOff provides `.OnGet()` and `.OnSet()` methods on the property interceptor
- KnockOff also provides `.VerifyGet()` and `.VerifySet()` for granular verification

---

## Step 5: Verify Calls

Replace Moq's `.Verify()` calls with KnockOff's `.Verify()` or `.Verifiable()` API.

**Moq:**

<!-- snippet: moq-migration-verify-moq -->
```cs
[Fact]
public void VerifyCalls_MoqApproach()
{
    var mock = new Mock<IMoqUserRepo>();

    IMoqUserRepo repository = mock.Object;
    repository.SaveUser(new User { Id = 1, Name = "Bob" });

    mock.Verify(x => x.SaveUser(It.IsAny<User>()), Moq.Times.Once());
}
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: moq-migration-verify-knockoff -->
```cs
[Fact]
public void VerifyCalls_KnockOffApproach()
{
    var stub = new MoqUserRepoStub();

    // Mark method as verifiable during setup
    stub.SaveUser.OnCall((user) => { }).Verifiable();

    IMoqUserRepo repository = stub;
    repository.SaveUser(new User { Id = 1, Name = "Bob" });

    // Verify() checks all members marked with .Verifiable()
    stub.Verify();

    // Or verify with Times constraint directly on tracking
    // stub.SaveUser.Verify(Times.Once);
}
```
<!-- endSnippet -->

**Key differences:**
- Moq uses `mock.Verify(expression, times)` with expression trees
- KnockOff uses `tracking.Verify(times)` on the object returned by `OnCall`
- KnockOff also supports `.Verifiable()` + `stub.Verify()` for batch verification
- Both support the same `Times` matchers (Once, AtLeastOnce, Exactly, etc.)

---

## Step 6: Async Methods

Replace `.ReturnsAsync()` with `Task.FromResult()` in `OnCall`.

**Moq:**

<!-- snippet: moq-migration-async-moq -->
```cs
[Fact]
public async Task AsyncMethod_MoqApproach()
{
    var mock = new Mock<IMoqUserRepo>();
    var testUser = new User { Id = 42, Name = "Alice" };

    mock.Setup(x => x.GetUserAsync(It.IsAny<int>())).ReturnsAsync(testUser);

    IMoqUserRepo repository = mock.Object;
    var user = await repository.GetUserAsync(42);

    Assert.NotNull(user);
    Assert.Equal("Alice", user.Name);
}
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: moq-migration-async-knockoff -->
```cs
[Fact]
public async Task AsyncMethod_KnockOffApproach()
{
    var stub = new MoqUserRepoStub();
    var testUser = new User { Id = 42, Name = "Alice" };

    stub.GetUserAsync.OnCall((id) => Task.FromResult<User?>(testUser));

    IMoqUserRepo repository = stub;
    var user = await repository.GetUserAsync(42);

    Assert.NotNull(user);
    Assert.Equal("Alice", user.Name);
}
```
<!-- endSnippet -->

**Key differences:**
- Moq provides `.ReturnsAsync()` helper
- KnockOff uses standard `Task.FromResult()` or `Task.CompletedTask`
- For exceptions: return `Task.FromException<T>(exception)`

---

## Step 7: Callbacks

Replace `.Callback()` with logic directly in `OnCall` delegates.

**Moq:**

<!-- snippet: moq-migration-callback-moq -->
```cs
[Fact]
public void Callback_MoqApproach()
{
    var mock = new Mock<IMoqUserRepo>();
    var savedUsers = new List<User>();

    mock.Setup(x => x.SaveUser(It.IsAny<User>()))
        .Callback<User>(u => savedUsers.Add(u));

    IMoqUserRepo repository = mock.Object;
    repository.SaveUser(new User { Id = 1, Name = "Alice" });
    repository.SaveUser(new User { Id = 2, Name = "Bob" });

    Assert.Equal(2, savedUsers.Count);
    Assert.Equal("Alice", savedUsers[0].Name);
    Assert.Equal("Bob", savedUsers[1].Name);
}
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: moq-migration-callback-knockoff -->
```cs
[Fact]
public void Callback_KnockOffApproach()
{
    var stub = new MoqUserRepoStub();
    var savedUsers = new List<User>();

    stub.SaveUser.OnCall((user) =>
    {
        savedUsers.Add(user);
    });

    IMoqUserRepo repository = stub;
    repository.SaveUser(new User { Id = 1, Name = "Alice" });
    repository.SaveUser(new User { Id = 2, Name = "Bob" });

    Assert.Equal(2, savedUsers.Count);
    Assert.Equal("Alice", savedUsers[0].Name);
    Assert.Equal("Bob", savedUsers[1].Name);
}
```
<!-- endSnippet -->

**Key differences:**
- Moq separates `.Callback()` and `.Returns()`
- KnockOff combines them in a single delegate—add logic, then return a value if needed
- You can access arguments directly by name

---

## Step 8: Argument Matching

Replace `It.IsAny<T>()` matchers with callback logic.

**Moq:**

<!-- snippet: moq-migration-arguments-moq -->
```cs
[Fact]
public void ArgumentMatching_MoqApproach()
{
    var mock = new Mock<IMoqUserRepo>();

    mock.Setup(x => x.GetUser(It.Is<int>(id => id > 0)))
        .Returns<int>(id => new User { Id = id, Name = "Valid User" });

    IMoqUserRepo repository = mock.Object;

    var validUser = repository.GetUser(1);
    var invalidUser = repository.GetUser(-1);

    Assert.NotNull(validUser);
    Assert.Null(invalidUser);
}
```
<!-- endSnippet -->

**KnockOff:**

<!-- snippet: moq-migration-arguments-knockoff -->
```cs
[Fact]
public void ArgumentMatching_KnockOffApproach()
{
    var stub = new MoqUserRepoStub();

    stub.GetUser.OnCall((id) =>
        id > 0 ? new User { Id = id, Name = "Valid User" } : null);

    IMoqUserRepo repository = stub;

    var validUser = repository.GetUser(1);
    var invalidUser = repository.GetUser(-1);

    Assert.NotNull(validUser);
    Assert.Null(invalidUser);
}
```
<!-- endSnippet -->

**Key differences:**
- Moq uses `It.IsAny<T>()` and `It.Is<T>()` for argument matching
- KnockOff callbacks receive all arguments—implement your own conditional logic
- For verification, inspect `CallHistory` to check specific argument values

---

## Complete Before/After Example

This example shows a full test class migrated from Moq to KnockOff.

### Before: Moq

<!-- snippet: moq-migration-complete-moq -->
```cs
private readonly Mock<IMoqUserRepo> _mockRepo;
private readonly UserServiceMigration _service;

public CompleteMoqTests()
{
    _mockRepo = new Mock<IMoqUserRepo>();
    _service = new UserServiceMigration(_mockRepo.Object);
}

[Fact]
public async Task GetUser_ReturnsUser()
{
    var user = new User { Id = 1, Name = "Alice" };
    _mockRepo.Setup(x => x.GetUserAsync(1)).ReturnsAsync(user);

    var result = await _service.GetUserAsync(1);

    Assert.Equal("Alice", result?.Name);
    _mockRepo.Verify(x => x.GetUserAsync(1), Moq.Times.Once());
}

[Fact]
public void SaveUser_CallsRepository()
{
    User? savedUser = null;
    _mockRepo.Setup(x => x.SaveUser(It.IsAny<User>()))
        .Callback<User>(u => savedUser = u);

    _service.SaveUser(new User { Id = 1, Name = "Bob" });

    Assert.NotNull(savedUser);
    Assert.Equal("Bob", savedUser?.Name);
    _mockRepo.Verify(x => x.SaveUser(It.IsAny<User>()), Moq.Times.Once());
}
```
<!-- endSnippet -->

### After: KnockOff

<!-- snippet: moq-migration-complete-knockoff -->
```cs
private readonly MoqUserRepoStub _stub;
private readonly UserServiceMigration _service;

public CompleteKnockOffTests()
{
    _stub = new MoqUserRepoStub();
    _service = new UserServiceMigration(_stub);
}

[Fact]
public async Task GetUser_ReturnsUser()
{
    var user = new User { Id = 1, Name = "Alice" };
    // Similar to Moq: Setup + Verifiable
    _stub.GetUserAsync.OnCall((id) => Task.FromResult<User?>(user)).Verifiable();

    var result = await _service.GetUserAsync(1);

    Assert.Equal("Alice", result?.Name);
    // Similar to Moq: mock.Verify() -> stub.Verify()
    _stub.Verify();
}

[Fact]
public void SaveUser_CallsRepository()
{
    User? savedUser = null;
    var tracking = _stub.SaveUser.OnCall((user) =>
    {
        savedUser = user;
    }).Verifiable();

    _service.SaveUser(new User { Id = 1, Name = "Bob" });

    Assert.NotNull(savedUser);
    Assert.Equal("Bob", savedUser?.Name);
    // Similar to Moq: mock.Verify(x => x.SaveUser(...), Times.Once())
    tracking.Verify(Times.Once);
}
```
<!-- endSnippet -->

**What changed:**
- Added stub class declaration with `[KnockOff]`
- Replaced `Mock<T>` with stub instance
- Replaced `.Setup()` with interceptor property assignments
- Replaced `.Verify()` with `stub.Verify()` and tracking object verification
- Removed `.Object` property accesses

**What stayed the same:**
- Test logic and assertions
- Test structure and organization
- Coverage and test goals

---

## Common Gotchas

### Forgetting the `partial` Keyword

**Problem:** Stub class isn't marked `partial`, causing duplicate member errors.

```csharp
// Wrong
[KnockOff]
class UserRepositoryStub : IUserRepository { }

// Correct
[KnockOff]
partial class UserRepositoryStub : IUserRepository { }
```

### Wrong `OnCall` Signature

**Problem:** Callback signature doesn't match the method parameters.

```csharp
// Wrong: GetUser(int id) expects (int) callback
stub.GetUser.OnCall(() => user);

// Correct
stub.GetUser.OnCall((id) => user);
```

### Forgetting `.Object` Equivalence for Class Stubs

**Problem:** Expecting to access a wrapper object when using class stubs.

```csharp
// Moq: needed .Object
var service = new UserService(mock.Object);

// KnockOff: use stub directly
var service = new UserService(stub);
```

---

## Next Steps

- **[Getting Started Guide](../getting-started.md)** - Learn KnockOff patterns from scratch
- **[Interceptor API Reference](../reference/interceptor-api.md)** - Deep dive into `OnCall`, `OnGet`, `OnSet`
- **[Verification Guide](../guides/verification.md)** - Advanced call tracking and verification patterns
- **[Methods Guide](../guides/methods.md)** - Configure method behavior and callbacks
- **[Properties Guide](../guides/properties.md)** - Work with property interceptors

---

**Need help?** Open an issue on [GitHub](https://github.com/neatoodotnet/KnockOff/issues) or check existing discussions.
