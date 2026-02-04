# KnockOff Stub Patterns Reference

[Home](../../../../README.md) > [KnockOff Usage](../SKILL.md) > Stub Patterns

KnockOff supports six distinct patterns for creating test stubs, organized into two categories:

**Standalone Patterns** (file-based, reusable across tests):
1. **Standalone** - `[KnockOff] partial class Stub : IService` - Dedicated stub class implementing interface
2. **Generic Standalone** - `[KnockOff] partial class Stub<T> : IService<T>` - Generic stub class with type parameters

**Inline Patterns** (nested within test class):
3. **Inline Interface** - `[KnockOff<IService>]` - Nested stub for closed generic interface
4. **Inline Class** - `[KnockOff<ConcreteClass>]` - Nested stub for class with virtual members
5. **Inline Delegate** - `[KnockOff<DelegateType>]` - Nested stub for delegate types
6. **Open Generic** - `[KnockOff(typeof(T<>))]` - Nested generic stub from open generic type

## Pattern Relationships

```
Standalone Patterns (file-based, reusable)
|-- 1. Standalone         - [KnockOff] class Stub : IFoo
|-- 2. Generic Standalone - [KnockOff] class Stub<T> : IFoo<T>

Inline Patterns (nested within test class)
|-- 3. Inline Interface   - [KnockOff<IFoo>]
|-- 4. Inline Class       - [KnockOff<SomeClass>]
|-- 5. Inline Delegate    - [KnockOff<SomeDelegate>]
|-- 6. Open Generic       - [KnockOff(typeof(IFoo<>))]
```

---

## Quick Decision Guide

| If you need... | Use this pattern |
|----------------|------------------|
| Reusable stub across multiple test files | Standalone |
| Custom methods on your stub | Standalone |
| Reusable generic stub with type parameters | Generic Standalone |
| Quick, test-local stub | Inline Interface |
| No extra stub files | Inline Interface |
| Stub a class (not interface) | Inline Class |
| Stub a delegate type | Inline Delegate |
| Test-local stub for generic interface | Open Generic |

---

## Standalone Pattern

The Standalone pattern creates a dedicated stub class in its own file. This stub can be reused across test files and supports adding custom methods.

### When to Use

- You need the same stub in multiple test files
- You want to add helper methods or custom behavior to the stub
- You prefer explicit, discoverable stub classes in IntelliSense
- You need the cleanest instantiation syntax (`new MyStub()`)

### Basic Setup

```cs
public interface IUserRepository
{
    User? GetById(int id);
    void Save(User user);
}

[KnockOff]
public partial class UserRepositoryStub : IUserRepository { }
```

### Usage in Tests

```cs
// Standalone: instantiate like any class, configure via Verify()
var stub = new UserRepositoryStub();
stub.GetById.OnCall((id) => new User { Id = id, Name = $"User{id}" }).Verifiable();
stub.Save.OnCall((user) => { }).Verifiable();

IUserRepository repo = stub;
var user = repo.GetById(42);

stub.Verify();
```

### Benefits

- **Reusable**: Reference the stub from any test file
- **User methods**: Add custom methods directly on the stub class
- **Discoverable**: Appears in IntelliSense when browsing your test project
- **Explicit**: Clear separation between test code and stub implementation
- **Clean syntax**: Simple `new MyStub()` instantiation

### Trade-offs

- **Extra file**: Requires a dedicated .cs file for each stub
- **Partial class**: Must remember to mark the class as `partial`
- **Manual interface**: Must manually implement the interface signature

### User Methods

Override protected virtual methods with the underscore suffix convention to provide default implementations:

```cs
[KnockOff]
public partial class UserRepositoryStub : IUserRepository
{
    // Override base class method with underscore suffix
    protected override User? GetById_(int id)
    {
        return new User { Id = id, Name = "Default User" };
    }
}
```

---

## Generic Standalone Pattern

The Generic Standalone pattern creates a reusable generic stub class that can be instantiated with different type arguments across your test suite.

### When to Use

- You need a reusable stub for a generic interface (e.g., `IRepository<T>`)
- You want to use the same stub definition with different type arguments
- You need the same stub in multiple test files with various types
- You prefer clean instantiation syntax with type parameters

### Basic Setup

```cs
public interface IRepository<T> where T : class
{
    T? GetById(int id);
    void Save(T entity);
    IEnumerable<T> GetAll();
}

[KnockOff]
public partial class RepositoryStub<T> : IRepository<T> where T : class { }
```

### Usage in Tests

```cs
// Generic Standalone: reusable across multiple type arguments
var userRepo = new RepositoryStub<User>();
userRepo.GetById.OnCall((id) => new User { Id = id, Name = "Test" }).Verifiable();
userRepo.Save.OnCall((entity) => { }).Verifiable();

var productRepo = new RepositoryStub<Product>();
productRepo.GetById.OnCall((id) => new Product { Id = id, Name = "Widget" }).Verifiable();
```

### Benefits

- **Single definition**: Define once, use with any type argument
- **Reusable**: Share across multiple test files
- **Type-safe**: Compiler enforces type constraints
- **Clean syntax**: `new RepositoryStub<User>()` - clear and readable
- **User methods**: Supports custom helper methods like Standalone

### Trade-offs

- **Extra file**: Requires a dedicated .cs file for the stub
- **Partial class**: Must mark as `partial`
- **Constraints must match**: Type constraints must mirror the interface

### Generic Standalone vs Open Generic

| Aspect | Generic Standalone | Open Generic |
|--------|-------------------|--------------|
| **Syntax** | `[KnockOff] class Stub<T> : IFoo<T>` | `[KnockOff(typeof(IFoo<>))]` |
| **Instantiation** | `new Stub<User>()` | `new Stubs.IFoo<User>()` |
| **Reusability** | Across test files | Within one test class |
| **User methods** | Yes | No |
| **Best for** | Shared generic stubs | One-time use |

---

## Inline Interface Pattern

The Inline Interface pattern generates a stub class scoped to your test class. The stub is accessed through a nested `Stubs` namespace.

### When to Use

- You need a stub only within one test class
- You don't need custom methods on the stub
- You want minimal ceremony and no extra files
- The interface is non-generic or you want a closed generic stub

### Basic Setup

```cs
[KnockOff<IEmailService>]
public partial class EmailServiceTests
{
    // The generator creates Stubs.IEmailService
}
```

### Usage in Tests

```cs
// Inline Interface: access via Stubs namespace
var stub = new Stubs.IEmailService();
stub.Send.OnCall((to, subject) => true).Verifiable();

IEmailService email = stub;
email.Send("test@example.com", "Hello");

stub.Verify();
```

### Benefits

- **Scoped**: Stub exists only for this test class, reducing namespace pollution
- **Less ceremony**: No separate file, no manual interface implementation
- **Automatic**: Stub class generated from interface definition
- **Co-located**: Stub definition and usage in same file

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
- You're testing code that depends on a concrete class

### Basic Setup

```cs
// Target class with virtual members
public class UserService
{
    public virtual User? GetUser(int id) => null;
    public virtual void SaveUser(User user) { }
    public virtual bool IsConnected { get; set; }
}

[KnockOff<UserService>]
public partial class UserServiceTests
{
    // The generator creates Stubs.UserService
}
```

### Usage in Tests

```cs
// Inline Class: configure stub, use .Object for the class instance
var stub = new Stubs.UserService();
stub.GetUser.OnCall((id) => new User { Id = id, Name = "FromStub" }).Verifiable();

UserService service = stub.Object;  // Use .Object!
var user = service.GetUser(42);

stub.Verify();
```

### Benefits

- **Stub classes**: Works with classes, not just interfaces
- **No interface extraction**: Avoids creating interfaces just for testing
- **Virtual members**: Intercepts any `virtual` or `abstract` members
- **Inheritance**: Properly inherits from the target class

### Trade-offs

- **Must use .Object**: The stub is a wrapper; use `.Object` property to get the actual instance
- **Virtual/abstract only**: Only overrides members marked `virtual` or `abstract`
- **No user methods**: Cannot add custom methods like Standalone pattern
- **Class limitations**: Subject to any sealed/non-virtual restrictions

---

## Inline Delegate Pattern

The Inline Delegate pattern is a specialized use of the Inline Interface pattern for delegate types. It generates a stub for delegates, allowing you to test code that accepts delegates as parameters, such as validation rules, factories, or callbacks.

### When to Use

- You need to stub a delegate type
- You want to track delegate invocations
- You need to configure delegate behavior in tests
- You are testing validation rules, factories, or event handlers

### Basic Setup

```cs
// Define delegate types
public delegate bool ValidationRule(string value);
public delegate T Factory<T>();

[KnockOff<ValidationRule>]
[KnockOff<Factory<User>>]
public partial class DelegateTests
{
    // The generator creates Stubs.ValidationRule and Stubs.Factory
}
```

### Usage in Tests

```cs
// Inline Delegate: configure via Interceptor, implicit conversion to delegate
var ruleStub = new Stubs.ValidationRule();
ruleStub.Interceptor.OnCall((value) => value != "invalid");

ValidationRule rule = ruleStub;  // Implicit conversion
bool isValid = rule("test");

ruleStub.Interceptor.Verify(Times.Once);
```

### Benefits

- **Implicit conversion**: Stub converts to delegate type automatically
- **Invocation tracking**: Use `Verify()`, `LastCallArg`, `LastCallArgs`
- **Behavior configuration**: Use `OnCall` to define custom logic
- **Verification**: Use `Verify()` and `Times` constraints

### Trade-offs

- **Interceptor property**: Access tracking via `stub.Interceptor` (not direct properties)
- **Test-local only**: Cannot reuse across multiple test classes
- **Named delegates only**: Cannot stub inline `Func<T>` or `Action<T>` directly

---

## Open Generic Pattern

The Open Generic pattern generates a generic stub class within your test class that can be instantiated with any type argument. Use this when you need a test-local generic stub without creating a separate file.

### When to Use

- You need a generic stub only within one test class
- You don't need custom methods on the stub
- You want to test with multiple type arguments in one test class
- You prefer inline definition over a separate file

### Basic Setup

```cs
public interface IService<T>
{
    T? GetItem(int id);
    void Process(T item);
}

[KnockOff(typeof(IService<>))]
public partial class OpenGenericTests
{
    // The generator creates Stubs.IService<T>
}
```

### Usage in Tests

```cs
// Open Generic: instantiate with any type argument
var userStub = new Stubs.IService<User>();
userStub.GetItem.OnCall((id) => new User { Id = id, Name = "FromStub" }).Verifiable();

var productStub = new Stubs.IService<Product>();
productStub.GetItem.OnCall((id) => new Product { Id = id, Name = "FromStub" }).Verifiable();

IService<User> userService = userStub;
var user = userService.GetItem(1);

userStub.Verify();
```

### Benefits

- **Flexible**: Use any type argument without defining separate stubs
- **No extra files**: Stub defined inline with tests
- **Type constraints**: Preserves constraints from the original generic type
- **Multiple types**: Use different type arguments in the same test class

### Trade-offs

- **Test-local only**: Cannot reuse across multiple test classes
- **No user methods**: Cannot add custom methods to the generated stub
- **typeof syntax**: Requires `typeof(IFoo<>)` with empty angle brackets
- **Stubs namespace**: Must use `Stubs.IFoo<T>` syntax

> **NOTE:** For reusable generic stubs across multiple test files, use the Generic Standalone pattern instead.

---

## Pattern Comparison

| Feature | Standalone | Generic Standalone | Inline Interface | Inline Class | Inline Delegate | Open Generic |
|---------|------------|-------------------|------------------|--------------|-----------------|--------------|
| **Reusable across test files** | Yes | Yes | No | No | No | No |
| **Custom user methods** | Yes | Yes | No | No | No | No |
| **Extra file required** | Yes | Yes | No | No | No | No |
| **Supports interfaces** | Yes | Yes | Yes | No | No | Yes |
| **Supports classes** | No | No | No | Yes | No | Yes |
| **Supports delegates** | No | No | No | No | Yes | Yes |
| **Supports generics** | No | Yes | Closed only | Closed only | Closed only | Yes |
| **Instantiation syntax** | `new MyStub()` | `new MyStub<T>()` | `new Stubs.IFoo()` | `new Stubs.Foo().Object` | `new Stubs.Del()` | `new Stubs.IFoo<T>()` |
| **Best for** | Shared stubs | Shared generic stubs | Local stubs | Class stubs | Delegate stubs | Local generic stubs |

---

## Choosing a Pattern

Follow this decision tree to select the appropriate pattern:

```
Is it a DELEGATE type?
|-- YES --> Inline Delegate pattern
|           [KnockOff<ValidationRule>]
|
|-- NO --> Is it a GENERIC interface/class?
    |
    |-- YES --> Do you need the stub in MULTIPLE test files?
    |   |
    |   |-- YES --> Generic Standalone pattern
    |   |           [KnockOff] class Stub<T> : IRepo<T>
    |   |
    |   |-- NO --> Open Generic pattern
    |              [KnockOff(typeof(IRepo<>))]
    |
    |-- NO --> Is it a CLASS (not interface)?
        |
        |-- YES --> Inline Class pattern
        |           [KnockOff<SomeClass>]
        |
        |-- NO --> Do you need the stub in MULTIPLE test files?
            |
            |-- YES --> Standalone pattern
            |           [KnockOff] class Stub : IFoo
            |
            |-- NO --> Do you need CUSTOM METHODS on the stub?
                |
                |-- YES --> Standalone pattern
                |           [KnockOff] class Stub : IFoo
                |
                |-- NO --> Inline Interface pattern
                           [KnockOff<IFoo>]
```

### Examples by Scenario

| Scenario | Recommended Pattern |
|----------|---------------------|
| Repository stub used in 5+ test classes | Standalone |
| Stub with `WithAdminUser()` helper method | Standalone |
| Generic repository shared across tests | Generic Standalone |
| Quick stub for single test class | Inline Interface |
| Stub a `DbContext` with virtual `DbSet` properties | Inline Class |
| Stub an abstract base class | Inline Class |
| Stub a validation rule delegate | Inline Delegate |
| Stub a factory function delegate | Inline Delegate |
| Generic service stub for one test class | Open Generic |
| `IRepository<T>` for multiple types in one test | Open Generic |

---

## Complete Example

This example demonstrates all six patterns working together:

```cs
// 1. Standalone: direct instantiation
var emailStub = new EmailServiceStub();
emailStub.Send.OnCall((to, subject, body) => true).Verifiable();
IEmailService email = emailStub;

// 2. Generic Standalone: reusable with type args
var notifierStub = new NotifierStub<User>();
notifierStub.Notify.OnCall((item) => { }).Verifiable();
INotifier<User> notifier = notifierStub;

// 3. Inline Interface: via Stubs namespace
var loggerStub = new Stubs.ILogger();
loggerStub.Log.OnCall((msg) => { }).Verifiable();
ILogger logger = loggerStub;

// 4. Inline Class: use .Object for class instance
var auditStub = new Stubs.AuditService();
auditStub.Audit.OnCall((action) => { }).Verifiable();
AuditService audit = auditStub.Object;

// 5. Inline Delegate: implicit conversion
var ruleStub = new Stubs.ValidationRule();
ruleStub.Interceptor.OnCall((value) => true);
ValidationRule rule = ruleStub;

// 6. Open Generic: inline stub with type args
var processorStub = new Stubs.IProcessor<Order>();
processorStub.Process.OnCall((item) => { }).Verifiable();
IProcessor<Order> processor = processorStub;
```

---

**UPDATED:** 2026-02-03
