---
name: knockoff
description: KnockOff source-generated test stubs. Use when creating interface stubs for unit tests, migrating from Moq to KnockOff, understanding the interceptor API (OnCall, OnGet, OnSet, Value), configuring stub behavior, verifying method calls/property access, or working with callbacks for tracking and customization.
allowed-tools: Read, Write, Edit, Glob, Grep, Bash(dotnet:*)
---

# KnockOff - Source-Generated Test Stubs

KnockOff is a Roslyn Source Generator that creates compile-time test stubs for interfaces, classes, and delegates. Unlike Moq's runtime proxies, KnockOff generates debuggable C# code.

## When to Use KnockOff

**Use KnockOff when:**
- The project has a `KnockOff` package reference
- Writing unit tests that need test doubles
- User asks to stub, mock, or fake an interface/class

**Do NOT use when:**
- Project uses Moq and user hasn't asked to migrate
- User explicitly requests Moq patterns

## Quick Start

### 1. Create a Stub

```csharp
// Standalone stub (reusable across files)
[KnockOff]
public partial class UserRepositoryStub : IUserRepository { }

// Inline stub (scoped to test class)
[KnockOff<IUserRepository>]
public partial class UserTests { }
```

### 2. Configure Behavior

```csharp
var stub = new UserRepositoryStub();

// Methods - use OnCall
stub.GetById.OnCall = (ko, id) => new User { Id = id };

// Properties - use Value (static) or OnGet (dynamic)
stub.IsConnected.Value = true;
stub.CurrentTime.OnGet = (ko) => DateTime.UtcNow;
```

### 3. Verify Calls

```csharp
Assert.True(stub.GetById.WasCalled);
Assert.Equal(1, stub.GetById.CallCount);
Assert.Equal(42, stub.GetById.LastCallArg);
```

## Stub Patterns

| Pattern | Attribute | Use Case |
|---------|-----------|----------|
| Standalone | `[KnockOff]` on class implementing interface | Reusable stubs with user methods |
| Inline Interface | `[KnockOff<IService>]` on test class | Test-local stubs |
| Inline Class | `[KnockOff<MyClass>]` on test class | Stub virtual/abstract class members |
| Delegate | `[KnockOff<Func<int,bool>>]` on test class | Stub Func<>/Action<> |

**Inline stub usage:**
```csharp
[KnockOff<IUserRepository>]
public partial class MyTests
{
    [Fact]
    public void Test()
    {
        var stub = new Stubs.IUserRepository();
        stub.GetById.OnCall = (ko, id) => new User { Id = id };
        // ...
    }
}
```

**Class stub - use `.Object`:**
```csharp
[KnockOff<EmailService>]
public partial class MyTests
{
    [Fact]
    public void Test()
    {
        var stub = new Stubs.EmailService();
        stub.Send.OnCall = (ko, to, body) => { };
        EmailService service = stub.Object;  // .Object for class instance
    }
}
```

## Core API

### Method Interceptors

```csharp
stub.MethodName.OnCall = (ko, arg1, arg2) => returnValue;  // Set behavior
stub.MethodName.CallCount      // int: number of calls
stub.MethodName.WasCalled      // bool: CallCount > 0
stub.MethodName.LastCallArg    // T: last argument (single param)
stub.MethodName.LastCallArgs   // named tuple: (arg1, arg2) (multi param)
stub.MethodName.Reset()        // Clear tracking and callbacks
```

### Property Interceptors

```csharp
stub.PropertyName.Value = value;          // Static return value (preferred)
stub.PropertyName.OnGet = (ko) => value;  // Dynamic getter
stub.PropertyName.OnSet = (ko, v) => { }; // Setter callback
stub.PropertyName.GetCount                // int: getter calls
stub.PropertyName.SetCount                // int: setter calls
stub.PropertyName.LastSetValue            // T: last set value
stub.PropertyName.Reset()                 // Clear tracking (NOT Value)
```

### Indexer Interceptors

```csharp
stub.Indexer.Backing[key] = value;           // Pre-populate backing dictionary
stub.Indexer.OnGet = (ko, key) => value;     // Dynamic getter
stub.Indexer.OnSet = (ko, key, v) => { };    // Setter callback
stub.Indexer.GetCount / SetCount             // Access counts
stub.Indexer.LastGetKey / LastSetEntry       // Last access info
```

### Event Interceptors

```csharp
stub.EventName.Raise(sender, args);  // Raise the event
stub.EventName.AddCount              // int: subscription count
stub.EventName.RemoveCount           // int: unsubscription count
stub.EventName.HasSubscribers        // bool: any subscribers
stub.EventName.Reset()               // Clear counts AND handlers
```

## Common Mistakes

### 1. Forgetting `partial`
```csharp
// WRONG
[KnockOff]
public class UserStub : IUser { }

// CORRECT
[KnockOff]
public partial class UserStub : IUser { }
```

### 2. Wrong callback signature (missing `ko` parameter)
```csharp
// WRONG
stub.GetUser.OnCall = (id) => new User();

// CORRECT - first param is always the stub instance
stub.GetUser.OnCall = (ko, id) => new User();
```

### 3. Forgetting `.Object` for class stubs
```csharp
// WRONG - stub is the wrapper
var service = new MyService(stub);

// CORRECT - use .Object for the class instance
var service = new MyService(stub.Object);
```

### 4. Using Moq syntax
```csharp
// WRONG - Moq syntax
stub.Setup(x => x.GetUser(It.IsAny<int>())).Returns(user);

// CORRECT - KnockOff syntax
stub.GetUser.OnCall = (ko, id) => user;
```

## Overloaded Methods

Methods with overloads get numeric suffixes:

```csharp
public interface IProcessor
{
    void Process(string data);        // stub.Process1
    void Process(string data, int n); // stub.Process2
}
```

Methods without overloads have no suffix.

## Generic Methods

Use `.Of<T>()` for type-specific configuration:

```csharp
stub.Deserialize.Of<User>().OnCall = (ko, json) => new User();
stub.Deserialize.Of<Order>().OnCall = (ko, json) => new Order();

Assert.Equal(2, stub.Deserialize.Of<User>().CallCount);
Assert.Equal(5, stub.Deserialize.TotalCallCount);  // All types combined
```

## User-Defined Methods (Compile-Time Defaults)

Define protected methods for shared behavior:

```csharp
[KnockOff]
public partial class UserRepositoryStub : IUserRepository
{
    // Called by default when GetById is invoked
    protected User? GetById(int id) => new User { Id = id, Name = "Default" };
}
```

**Priority:** Callback > User method > Default

## Moq Migration Quick Reference

| Moq | KnockOff |
|-----|----------|
| `new Mock<IService>()` | `new ServiceStub()` |
| `mock.Object` | Cast to interface (implicit) or `stub.Object` (class) |
| `.Setup(x => x.Prop).Returns(v)` | `stub.Prop.Value = v` |
| `.Setup(x => x.Method())` | `stub.Method.OnCall = (ko) => ...` |
| `.ReturnsAsync(v)` | `OnCall = (ko) => Task.FromResult(v)` |
| `.Callback(action)` | Logic inside `OnCall` |
| `.Verify(Times.Once)` | `Assert.Equal(1, stub.Method.CallCount)` |
| `It.IsAny<T>()` | Implicit (callback receives all args) |

See [moq-migration.md](moq-migration.md) for detailed migration patterns.

## Additional Reference

- [api-reference.md](api-reference.md) - Complete interceptor API
- [moq-migration.md](moq-migration.md) - Step-by-step Moq migration
- [patterns.md](patterns.md) - Async, sequential returns, exceptions
- [stub-types.md](stub-types.md) - Standalone vs inline vs class vs delegate
