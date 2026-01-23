# Stub Patterns

KnockOff supports three patterns for creating test stubs. Each pattern solves different testing scenarios with varying trade-offs in reusability, ceremony, and capabilities.

## Quick Decision Guide

| If you need... | Use this pattern |
|----------------|------------------|
| Reusable stub across multiple test files | Stand-Alone / Flat |
| Custom methods on your stub | Stand-Alone / Flat |
| Quick, test-local stub | Inline Interface |
| No extra stub files | Inline Interface |
| Stub a class (not interface) | Inline Class |

---

## Stand-Alone / Flat Pattern

The Stand-Alone pattern creates a dedicated stub class in its own file. This stub can be reused across test files and supports adding custom methods.

### When to Use

- You need the same stub in multiple test files
- You want to add helper methods or custom behavior to the stub
- You prefer explicit, discoverable stub classes in IntelliSense

### Basic Setup

<!-- snippet: patterns-standalone-basic -->
```cs
public interface IUserRepoStandalone
{
    User? GetById(int id);
    void Save(User user);
}

[KnockOff]
public partial class UserRepoStandaloneStub : IUserRepoStandalone
{
    // Optionally add user methods for default behavior
    protected User? GetById(int id) => new User { Id = id, Name = $"User{id}" };
}
```
<!-- endSnippet -->

### Usage in Tests

<!-- snippet: patterns-standalone-usage -->
```cs
[Fact]
public void StandaloneStub_CanBeConfiguredAndVerified()
{
    // Arrange - instantiate the reusable stub
    var stub = new UserRepoStandaloneStub();

    // Configure void method via OnCall and mark verifiable
    stub.Save.OnCall((user) => { }).Verifiable();

    // Act - cast to interface for use
    IUserRepoStandalone repository = stub;
    var user = repository.GetById(42);
    repository.Save(user!);

    // Assert - verify via Verify()
    Assert.NotNull(user);
    stub.Verify();
    // User methods get a numbered interceptor (GetById2) for tracking
    // This allows verification without blocking the user method implementation
    stub.GetById2.Verify(Times.Once);
}
```
<!-- endSnippet -->

### Benefits

- **Reusable**: Reference the stub from any test file
- **User methods**: Add custom methods directly on the stub class
- **Discoverable**: Appears in IntelliSense when browsing your test project
- **Explicit**: Clear separation between test code and stub implementation

### Trade-offs

- **Extra file**: Requires a dedicated .cs file for each stub
- **Partial class**: Must remember to mark the class as `partial`
- **Manual interface**: Must manually implement the interface signature

---

## Inline Interface Pattern

The Inline Interface pattern generates a stub class scoped to your test class. The stub is accessed through a nested `Stubs` namespace.

### When to Use

- You need a stub only within one test class
- You don't need custom methods on the stub
- You want minimal ceremony and no extra files

### Basic Setup

<!-- snippet: patterns-inline-interface-basic -->
```cs
[KnockOff<IUserRepoInline>]
public partial class InlineInterfaceTests
{
    // The generator creates Stubs.IUserRepoInline
}
```
<!-- endSnippet -->

### Usage in Tests

<!-- snippet: patterns-inline-interface-usage -->
```cs
[Fact]
public void InlineInterfaceStub_GeneratedInStubsNamespace()
{
    // Arrange - use generated Stubs.InterfaceName class
    var stub = new Stubs.IUserRepoInline();

    // Configure behavior and mark verifiable
    stub.GetById.OnCall((id) => new User { Id = id, Name = "Test" }).Verifiable();
    stub.Save.OnCall((user) => { }).Verifiable();

    // Act
    IUserRepoInline repository = stub;
    var user = repository.GetById(1);
    repository.Save(user!);

    // Assert
    Assert.NotNull(user);
    Assert.Equal("Test", user.Name);
    stub.Verify();
}
```
<!-- endSnippet -->

### Benefits

- **Scoped**: Stub exists only for this test class, reducing namespace pollution
- **Less ceremony**: No separate file, no manual interface implementation
- **Automatic**: Stub class generated from interface definition

### Trade-offs

- **No user methods**: Cannot add custom methods to the generated stub
- **Stubs namespace**: Must use `Stubs.IFoo` syntax to instantiate
- **Test-local only**: Cannot reuse across multiple test classes

---

## Inline Class Pattern

The Inline Class pattern generates a stub for abstract or virtual class members. This allows stubbing classes without extracting interfaces.

### When to Use

- You need to stub a class (not an interface)
- The class has `virtual` or `abstract` members you want to intercept
- You cannot or don't want to extract an interface

### Basic Setup

<!-- snippet: patterns-inline-class-basic -->
```cs
// Target class with virtual members
public class UserServiceClass
{
    public virtual User? GetUser(int id) => null;
    public virtual void SaveUser(User user) { }
    public virtual bool IsConnected { get; set; }
}

[KnockOff<UserServiceClass>]
public partial class InlineClassTests
{
    // The generator creates Stubs.UserServiceClass
}
```
<!-- endSnippet -->

### Usage in Tests

<!-- snippet: patterns-inline-class-usage -->
```cs
[Fact]
public void InlineClassStub_UsesObjectProperty()
{
    // Arrange - create wrapper stub
    var stub = new Stubs.UserServiceClass();

    // Configure virtual member behavior and mark verifiable
    stub.GetUser.OnCall((id) => new User { Id = id, Name = "FromStub" }).Verifiable();

    // Act - use .Object to get the actual class instance
    UserServiceClass service = stub.Object;
    var user = service.GetUser(42);

    // Assert
    Assert.NotNull(user);
    Assert.Equal("FromStub", user.Name);
    stub.Verify();
}
```
<!-- endSnippet -->

### Benefits

- **Stub classes**: Works with classes, not just interfaces
- **No interface extraction**: Avoids creating interfaces just for testing
- **Virtual members**: Intercepts any `virtual` or `abstract` members

### Trade-offs

- **Must use .Object**: The stub is a wrapper; use `.Object` property to get the actual instance
- **Virtual/abstract only**: Only overrides members marked `virtual` or `abstract`
- **No user methods**: Cannot add custom methods like Stand-Alone pattern

---

## Pattern Comparison

| Feature | Stand-Alone | Inline Interface | Inline Class |
|---------|-------------|------------------|--------------|
| **Reusable across test files** | Yes | No | No |
| **Custom user methods** | Yes | No | No |
| **Extra file required** | Yes | No | No |
| **Supports interfaces** | Yes | Yes | No |
| **Supports classes** | No | No | Yes |
| **IntelliSense visible** | Yes | Within test class | Within test class |
| **Instantiation syntax** | `new MyStub()` | `new Stubs.IFoo()` | `new Stubs.Foo().Object` |
| **Best for** | Shared stubs | Local stubs | Class stubs |

---

## Choosing a Pattern

Follow this decision tree:

1. **Do you need to stub a class (not an interface)?**
   - Yes → **Inline Class** pattern
   - No → Continue to step 2

2. **Do you need the stub in multiple test files?**
   - Yes → **Stand-Alone** pattern
   - No → Continue to step 3

3. **Do you need custom methods on the stub?**
   - Yes → **Stand-Alone** pattern
   - No → **Inline Interface** pattern

### Examples by Scenario

| Scenario | Recommended Pattern |
|----------|---------------------|
| Repository stub used in 5+ test classes | Stand-Alone |
| Stub with `WithAdminUser()` helper method | Stand-Alone |
| Quick stub for single test class | Inline Interface |
| Stub a `DbContext` with virtual `DbSet` properties | Inline Class |
| Stub an abstract base class | Inline Class |

---

## Complete Example

This example demonstrates all three patterns in a realistic test scenario.

<!-- snippet: patterns-complete-example -->
```cs
[KnockOff<ILogSvc>]
[KnockOff<AuditSvcBase>]
public partial class PatternComparisonTests
{
    [Fact]
    public void AllThreePatterns_WorkTogether()
    {
        // Stand-Alone: Reusable email stub
        var emailStub = new EmailSvcPatternStub();
        emailStub.Send.OnCall((to, subject, body) => true).Verifiable();
        emailStub.IsConfigured.Value = true;

        // Inline Interface: Test-local logger stub
        var loggerStub = new Stubs.ILogSvc();
        var logMessages = new List<string>();
        var logTracking = loggerStub.Log.OnCall((msg) => logMessages.Add(msg)).Verifiable(Times.Exactly(2));

        // Inline Class: Stub for abstract base class
        var auditStub = new Stubs.AuditSvcBase();
        auditStub.Audit.OnCall((action) => { }).Verifiable();

        // Act - simulate integration scenario
        IEmailSvcPattern email = emailStub;
        ILogSvc logger = loggerStub;
        AuditSvcBase audit = auditStub.Object;

        logger.Log("Starting operation");
        var sent = email.Send("user@test.com", "Hello", "World");
        audit.Audit("email_sent");
        logger.Log("Operation complete");

        // Assert - each pattern provides Verify()
        Assert.True(sent);
        emailStub.Verify();
        loggerStub.Verify();
        auditStub.Verify();
        Assert.Contains("Starting operation", logMessages);
    }
}
```
<!-- endSnippet -->

---

## Next Steps

- **[Getting Started](../getting-started.md)** - Learn basic stub creation
- **[Methods Guide](methods.md)** - Configure method behavior with OnCall
- **[Properties Guide](properties.md)** - Work with property interceptors
- **[Interceptor API Reference](../reference/interceptor-api.md)** - Complete API documentation
