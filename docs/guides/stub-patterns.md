# Stub Patterns

KnockOff supports three patterns for creating stubs. Choose based on your needs.

## Pattern Comparison

| Pattern | Best For | Stub Location |
|---------|----------|---------------|
| **Standalone** | Reusable stubs shared across files | Own class file |
| **Inline** | Test-class-scoped stubs | Nested in test class |
| **Delegate** | Stubbing `Func<>`, `Action<>`, delegates | Nested in test class |

## Standalone Stubs

Create a partial class implementing an interface with `[KnockOff]`:

<!-- snippet: stub-patterns-standalone-basic -->
```cs
[KnockOff]
public partial class SpUserRepositoryStub : ISpUserRepository { }
```
<!-- endSnippet -->

**Usage:**

<!-- snippet: stub-patterns-standalone-usage -->
```cs
public static void StandaloneUsage()
{
    var stub = new SpUserRepositoryStub();
    stub.GetById.OnCall = (ko, id) => new SpUser { Id = id };

    ISpUserRepository repo = stub;
    // or
    // var repo2 = stub.AsISpUserRepository(); // Alternative helper

    _ = repo;
}
```
<!-- endSnippet -->

### Adding Default Behavior

Define protected methods for consistent defaults:

<!-- snippet: stub-patterns-standalone-with-defaults -->
```cs
[KnockOff]
public partial class SpUserRepositoryWithDefaultsStub : ISpUserRepository
{
    protected SpUser? GetById(int id) => new SpUser { Id = id, Name = $"User-{id}" };
    protected IEnumerable<SpUser> GetAll() => [];
}
```
<!-- endSnippet -->

### When to Use

- Stub used across multiple test files
- Default behavior needed (user methods)
- Stub complexity warrants its own file

## Inline Stubs

Add `[KnockOff<T>]` to your test class:

<!-- snippet: stub-patterns-inline-basic -->
```cs
[KnockOff<ISpUserRepository>]
[KnockOff<ISpEmailService>]
public partial class SpUserServiceTests
{
    public void Test()
    {
        var repoStub = new Stubs.ISpUserRepository();
        var emailStub = new Stubs.ISpEmailService();

        repoStub.GetById.OnCall = (ko, id) => new SpUser { Id = id };

        // var service = new UserService(repoStub, emailStub);
        _ = (repoStub, emailStub);
    }
}
```
<!-- endSnippet -->

**Note:** The test class must be `partial`.

### Multiple Interfaces

Apply multiple `[KnockOff<T>]` attributes:

<!-- snippet: stub-patterns-inline-multiple -->
```cs
[KnockOff<ISpOrderRepository>]
[KnockOff<ISpPaymentService>]
[KnockOff<ISpNotificationService>]
public partial class SpOrderProcessorTests
{
    // Each generates a nested stub class in Stubs namespace
}
```
<!-- endSnippet -->

### When to Use

- Stub only needed for one test class
- No custom default behavior required
- Multiple unrelated interfaces needed

## Class Stubs

Stub unsealed classes with virtual/abstract members:

<!-- snippet: stub-patterns-class-stub -->
```cs
[KnockOff<SpEmailServiceClass>]
public partial class SpNotificationTests
{
    public void Test()
    {
        var stub = new Stubs.SpEmailServiceClass();

        // Configure virtual methods
        stub.Send.OnCall = (ko, to, body) => { };

        // Use .Object to get the class instance
        SpEmailServiceClass service = stub.Object;

        // var notifier = new Notifier(service);
        _ = service;
    }
}
```
<!-- endSnippet -->

**Important:** Access the class instance via `.Object`.

### Constructor Parameters

Pass constructor arguments when creating the stub:

<!-- snippet: stub-patterns-class-constructor -->
```cs
[KnockOff<SpEmailServiceClass>]
public partial class SpEmailServiceConstructorTests
{
    public void Test()
    {
        var stub = new Stubs.SpEmailServiceClass("smtp.test.com", 587);
        SpEmailServiceClass service = stub.Object;

        _ = service;
    }
}
```
<!-- endSnippet -->

### What Gets Stubbed

- `virtual` methods and properties — intercepted
- `abstract` methods and properties — intercepted
- Non-virtual members — called through `.Object` (real implementation)

### When to Use

- Testing code that depends on classes, not interfaces
- Need to override specific virtual methods
- Base class behavior needed for non-virtual members

## Delegate Stubs

Stub `Func<>`, `Action<>`, or named delegates:

<!-- snippet: stub-patterns-delegate -->
```cs
[KnockOff<SpValidationRule>]
[KnockOff<Func<int, string>>]
[KnockOff<Action<string>>]
public partial class SpValidationTests
{
    public void Test()
    {
        // Named delegate
        var ruleStub = new Stubs.SpValidationRule();
        ruleStub.Interceptor.OnCall = (ko, value) => value.Length > 0;
        SpValidationRule rule = ruleStub;

        // Func<>
        var funcStub = new Stubs.Func();
        funcStub.Interceptor.OnCall = (ko, id) => $"Item-{id}";
        Func<int, string> func = funcStub;

        // Action<>
        var actionStub = new Stubs.Action();
        actionStub.Interceptor.OnCall = (ko, msg) => { /* captured */ };
        Action<string> action = actionStub;

        _ = (rule, func, action);
    }
}
```
<!-- endSnippet -->

**Note:** Delegate stubs use `Interceptor` to access the callback, not direct property names.

### Implicit Conversion

Delegate stubs convert implicitly to their target type:

<!-- snippet: stub-patterns-delegate-implicit -->
```cs
[KnockOff<SpValidationRule>]
public partial class SpImplicitConversionTests
{
    public void Test()
    {
        var stub = new Stubs.SpValidationRule();
        SpValidationRule rule = stub;  // Implicit conversion

        var result = rule("test");  // Invokes through interceptor
        Assert.True(stub.Interceptor.WasCalled);

        _ = result;
    }
}
```
<!-- endSnippet -->

### When to Use

- Code under test accepts `Func<>` or `Action<>` parameters
- Testing callback-based APIs
- Stubbing validation rules or factories

## Nested Stubs

Stubs can be nested inside test classes:

<!-- snippet: stub-patterns-nested -->
```cs
public partial class SpOrderTests  // Must be partial!
{
    [KnockOff]
    public partial class OrderRepositoryStub : ISpOrderRepository { }

    public void Test()
    {
        var stub = new OrderRepositoryStub();
        _ = stub;
    }
}
```
<!-- endSnippet -->

**Important:** All containing classes must be `partial`.

## Decision Guide

| Question | Pattern |
|----------|---------|
| Shared across test files? | Standalone |
| Multiple interfaces in one test class? | Inline |
| Need user method defaults? | Standalone |
| Stubbing a class, not interface? | Class stub (inline) |
| Stubbing `Func<>` or `Action<>`? | Delegate stub |
| Quick, test-local stub? | Inline |
