# Stub Types

KnockOff supports multiple stub patterns. Choose based on your needs.

## Overview

| Pattern | Attribute | Best For |
|---------|-----------|----------|
| Standalone | `[KnockOff]` on class implementing interface | Reusable stubs, user methods |
| Inline Interface | `[KnockOff<IService>]` on test class | Test-local stubs |
| Inline Class | `[KnockOff<MyClass>]` on test class | Stubbing virtual/abstract members |
| Delegate | `[KnockOff<Func<...>>]` on test class | Stubbing Func<>/Action<> |
| Generic Standalone | `[KnockOff]` on generic class | Reusable generic stubs |
| Open Generic Inline | `[KnockOff(typeof(IRepo<>))]` on test class | Generic stubs in tests |

## Standalone Stubs

Create a partial class implementing an interface:

```csharp
[KnockOff]
public partial class UserRepositoryStub : IUserRepository { }
```

**Usage:**
```csharp
var stub = new UserRepositoryStub();
stub.GetById.OnCall = (ko, id) => new User { Id = id };

IUserRepository repo = stub;  // Implicit conversion
```

### Adding Default Behavior

Define protected methods:

```csharp
[KnockOff]
public partial class UserRepositoryStub : IUserRepository
{
    protected User? GetById(int id) => new User { Id = id, Name = "Default" };
    protected IEnumerable<User> GetAll() => [];
}
```

### When to Use

- Stub used across multiple test files
- Default behavior needed (user methods)
- Stub complexity warrants its own file

## Inline Interface Stubs

Add `[KnockOff<T>]` to your test class:

```csharp
[KnockOff<IUserRepository>]
[KnockOff<IEmailService>]
public partial class UserServiceTests
{
    [Fact]
    public void Test()
    {
        var repoStub = new Stubs.IUserRepository();
        var emailStub = new Stubs.IEmailService();

        repoStub.GetById.OnCall = (ko, id) => new User { Id = id };
    }
}
```

**Note:** Test class must be `partial`.

### When to Use

- Stub only needed for one test class
- No custom default behavior required
- Quick, test-local stub

## Class Stubs

Stub unsealed classes with virtual/abstract members:

```csharp
public class EmailService  // Unsealed class
{
    public virtual void Send(string to, string body) { }
    public string ServerName { get; }  // Non-virtual - NOT stubbed
}

[KnockOff<EmailService>]
public partial class NotificationTests
{
    [Fact]
    public void Test()
    {
        var stub = new Stubs.EmailService();
        stub.Send.OnCall = (ko, to, body) => { };

        EmailService service = stub.Object;  // Use .Object!
    }
}
```

**Important:** Use `.Object` to get the class instance.

### Constructor Parameters

```csharp
var stub = new Stubs.EmailService("smtp.test.com", 587);
EmailService service = stub.Object;
```

### What Gets Stubbed

- `virtual` methods/properties — intercepted
- `abstract` methods/properties — intercepted
- Non-virtual members — called through `.Object` (real implementation)

### When to Use

- Testing code that depends on classes, not interfaces
- Need to override specific virtual methods
- Base class behavior needed for non-virtual members

## Delegate Stubs

Stub `Func<>`, `Action<>`, or named delegates:

```csharp
public delegate bool ValidationRule(string value);

[KnockOff<ValidationRule>]
[KnockOff<Func<int, string>>]
[KnockOff<Action<string>>]
public partial class ValidationTests
{
    [Fact]
    public void Test()
    {
        // Named delegate
        var ruleStub = new Stubs.ValidationRule();
        ruleStub.Interceptor.OnCall = (ko, value) => value.Length > 0;
        ValidationRule rule = ruleStub;  // Implicit conversion

        // Func<>
        var funcStub = new Stubs.Func();
        funcStub.Interceptor.OnCall = (ko, id) => $"Item-{id}";
        Func<int, string> func = funcStub;

        // Action<>
        var actionStub = new Stubs.Action();
        actionStub.Interceptor.OnCall = (ko, msg) => { };
        Action<string> action = actionStub;
    }
}
```

**Note:** Use `Interceptor` to access the callback.

### When to Use

- Code under test accepts `Func<>` or `Action<>` parameters
- Testing callback-based APIs
- Stubbing validation rules or factories

## Generic Standalone Stubs

Create reusable generic stubs:

```csharp
public interface IRepository<T> where T : class
{
    T? GetById(int id);
    void Save(T entity);
}

[KnockOff]
public partial class RepositoryStub<T> : IRepository<T> where T : class { }
```

**Usage:**
```csharp
var userRepo = new RepositoryStub<User>();
var orderRepo = new RepositoryStub<Order>();

userRepo.GetById.OnCall = (ko, id) => new User { Id = id };
orderRepo.GetById.OnCall = (ko, id) => new Order { Id = id };
```

### Type Parameter Arity

Stub must have same type parameters as interface:

```csharp
// Correct
[KnockOff]
public partial class CacheStub<TKey, TValue> : ICache<TKey, TValue> { }

// Error KO0008: mismatched arity
[KnockOff]
public partial class BadStub<T, TExtra> : IRepository<T> { }
```

## Open Generic Inline Stubs

Use `typeof()` with unbound generic:

```csharp
[KnockOff(typeof(IRepository<>))]
public partial class MyTests
{
    [Fact]
    public void Test()
    {
        // Generated: MyTests.Stubs.IRepository<T>
        var userRepo = new Stubs.IRepository<User>();
        var orderRepo = new Stubs.IRepository<Order>();
    }
}
```

### Closed vs Open

| Pattern | Syntax | Generated |
|---------|--------|-----------|
| Closed | `[KnockOff<IRepo<User>>]` | `Stubs.IRepoUser` (non-generic) |
| Open | `[KnockOff(typeof(IRepo<>))]` | `Stubs.IRepo<T>` (generic) |

### Multi-Parameter Generics

```csharp
[KnockOff(typeof(IKeyValueStore<,>))]
public partial class MyTests { }

var store = new Stubs.IKeyValueStore<string, int>();
```

## Nested Stubs

Stubs can be nested inside test classes:

```csharp
public partial class OrderTests  // Must be partial!
{
    [KnockOff]
    public partial class OrderRepositoryStub : IOrderRepository { }

    [Fact]
    public void Test()
    {
        var stub = new OrderRepositoryStub();
    }
}
```

**Critical:** All containing classes must be `partial`.

```csharp
// Won't compile
public class MyTests
{
    [KnockOff]
    public partial class ServiceStub : IService { }  // Error!
}

// Correct
public partial class MyTests  // partial!
{
    [KnockOff]
    public partial class ServiceStub : IService { }
}
```

## Decision Guide

| Question | Use |
|----------|-----|
| Shared across test files? | Standalone |
| Multiple interfaces in one test class? | Inline |
| Need user method defaults? | Standalone |
| Stubbing a class, not interface? | Class stub |
| Stubbing `Func<>` or `Action<>`? | Delegate stub |
| Generic interface, reusable? | Generic standalone |
| Generic interface, test-local? | Open generic inline |
| Quick, test-local stub? | Inline |

## Summary

| Stub Type | Interface Access | Notes |
|-----------|------------------|-------|
| Standalone | `stub` (implicit cast) | Define user methods for defaults |
| Inline Interface | `stub` (implicit cast) | Generated in `Stubs.` namespace |
| Class | `stub.Object` | Only virtual/abstract members stubbed |
| Delegate | `stub` (implicit cast) | Use `stub.Interceptor` for config |
| Generic | Same as standalone | Type params must match |
| Open Generic | `new Stubs.IRepo<T>()` | Generic class generated |
