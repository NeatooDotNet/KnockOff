[Home](../../README.md) > [Guides](.) > Stub Patterns

# Stub Patterns

KnockOff supports nine distinct patterns for creating test stubs, organized into two categories:

**Standalone Patterns** (file-based, reusable across tests):

1. **[Standalone](#standalone-pattern)** - Dedicated stub class implementing interface
2. **[Generic Standalone](#generic-standalone-pattern)** - Generic stub class with type parameters
3. **[Standalone Class](#standalone-class-pattern)** - Dedicated stub class for concrete/abstract classes
4. **[Generic Standalone Class](#generic-standalone-class-pattern)** - Generic stub class for generic base classes

**Inline Patterns** (nested within test class):

5. **[Inline Interface](#inline-interface-pattern)** - Nested stub for closed generic interface
6. **[Inline Class](#inline-class-pattern)** - Nested stub for class with virtual members
7. **[Inline Delegate](#inline-delegate-pattern)** - Nested stub for delegate types
8. **[Open Generic Interface](#open-generic-interface-pattern)** - Nested generic stub from open generic interface
9. **[Open Generic Class](#open-generic-class-pattern)** - Nested generic stub from open generic class

## Pattern Relationships

```
Standalone Patterns (file-based, reusable)
|-- 1. Standalone               - [KnockOff] class Stub : IFoo
|-- 2. Generic Standalone       - [KnockOff] class Stub<T> : IFoo<T>
|-- 3. Standalone Class         - [KnockOffBase<SomeClass>] class Stub
|-- 4. Generic Standalone Class - [KnockOffBase(typeof(ClassBase<>))] class Stub<T>

Inline Patterns (nested within test class)
|-- 5. Inline Interface        - [KnockOff<IFoo>]
|-- 6. Inline Class            - [KnockOff<SomeClass>]
|-- 7. Inline Delegate         - [KnockOff<SomeDelegate>]
|-- 8. Open Generic Interface  - [KnockOff(typeof(IFoo<>))]
|-- 9. Open Generic Class      - [KnockOff(typeof(SomeClass<>))]
```

## Quick Decision Guide

| If you need... | Use this pattern |
|----------------|------------------|
| Reusable interface stub across multiple test files | Standalone |
| Custom methods on your interface stub | Standalone |
| Reusable generic interface stub with type parameters | Generic Standalone |
| Reusable class stub across multiple test files | Standalone Class |
| Custom methods on your class stub | Standalone Class |
| Reusable generic class stub with type parameters | Generic Standalone Class |
| Quick, test-local stub | Inline Interface |
| No extra stub files | Inline Interface |
| Stub a class (not interface) for one test class | Inline Class |
| Stub a delegate type | Inline Delegate |
| Test-local stub for generic interface | Open Generic Interface |
| Test-local stub for generic class | Open Generic Class |

---

## Standalone Pattern

The Standalone pattern creates a dedicated stub class in its own file. This stub can be reused across test files and supports adding custom methods.

### When to Use

- You need the same stub in multiple test files
- You want to add helper methods or custom behavior to the stub
- You prefer explicit, discoverable stub classes in IntelliSense
- You need the cleanest instantiation syntax (`new MyStub()`)

### Basic Setup

<!-- snippet: patterns-standalone-basic -->
```cs
public interface IUserRepoStandalone
{
    User? GetById(int id);
    void Save(User user);
}

[KnockOff]
public partial class UserRepoStandaloneStub : IUserRepoStandalone { }
```
<!-- endSnippet -->

### Usage in Tests

<!-- snippet: patterns-standalone-usage -->
```cs
// Stand-Alone: instantiate like any class, configure via Verify()
var stub = new UserRepoStandaloneStub();
stub.GetById.Return((id) => new User { Id = id, Name = $"User{id}" }).Verifiable();
stub.Save.Call((user) => { }).Verifiable();
```
<!-- endSnippet -->

### Benefits

- **Reusable**: Reference the stub from any test file
- **User methods and properties**: Add custom methods and override properties directly on the stub class
- **Discoverable**: Appears in IntelliSense when browsing your test project
- **Explicit**: Clear separation between test code and stub implementation
- **Clean syntax**: Simple `new MyStub()` instantiation

### Trade-offs

- **Extra file**: Requires a dedicated .cs file for each stub
- **Partial class**: Must remember to mark the class as `partial`
- **Manual interface**: Must manually implement the interface signature

### Base Class and User Methods/Properties

The Standalone pattern generates a base class (e.g., `UserRepoStandaloneStubBase`) that your stub inherits from. This base class exposes `protected virtual` methods and properties for each interface member, allowing you to add custom stub behavior through inheritance.

Override these members using the **underscore suffix convention** (`_`) to provide default implementations:

<!-- snippet: patterns-user-methods -->
```cs
[KnockOff]
public partial class UserRepoWithUserMethodsStub : IUserRepoStandalone
{
    // Override base class method with underscore suffix
    protected override User? GetById_(int id)
    {
        return new User { Id = id, Name = "Default User" };
    }
}
```
<!-- endSnippet -->

The interceptor name remains clean (`GetById`), while your implementation uses the suffix (`GetById_`). This keeps user methods and properties separate from the generated code. See [User Methods](user-methods.md) for methods and [Properties Guide](properties.md#user-properties-standalone-patterns) for user properties.

---

## Generic Standalone Pattern

The Generic Standalone pattern creates a reusable generic stub class that can be instantiated with different type arguments across your test suite.

### When to Use

- You need a reusable stub for a generic interface (e.g., `IRepository<T>`)
- You want to use the same stub definition with different type arguments
- You need the same stub in multiple test files with various types
- You prefer clean instantiation syntax with type parameters

### Basic Setup

<!-- snippet: patterns-generic-standalone-basic -->
```cs
public interface IRepositoryGeneric<T> where T : class
{
    T? GetById(int id);
    void Save(T entity);
    IEnumerable<T> GetAll();
}

[KnockOff]
public partial class RepositoryGenericStub<T> : IRepositoryGeneric<T> where T : class { }
```
<!-- endSnippet -->

### Usage in Tests

<!-- snippet: patterns-generic-standalone-usage -->
```cs
// Generic Standalone: reusable across multiple type arguments
var userRepo = new RepositoryGenericStub<User>();
userRepo.GetById.Return((id) => new User { Id = id, Name = "Test" }).Verifiable();
userRepo.Save.Call((entity) => { }).Verifiable();

var productRepo = new RepositoryGenericStub<Product>();
productRepo.GetById.Return((id) => new Product { Id = id, Name = "Widget" }).Verifiable();
```
<!-- endSnippet -->

### Benefits

- **Single definition**: Define once, use with any type argument
- **Reusable**: Share across multiple test files
- **Type-safe**: Compiler enforces type constraints
- **Clean syntax**: `new RepositoryStub<User>()` - clear and readable
- **User methods and properties**: Supports custom helper methods and property overrides like Standalone

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
| **User methods/properties** | Yes | No |
| **Best for** | Shared generic stubs | One-time use |

---

## Standalone Class Pattern

The Standalone Class pattern creates a dedicated stub class for concrete or abstract classes in its own file. This stub can be reused across test files and supports adding custom methods.

### When to Use

- You need the same class stub in multiple test files
- You want to add helper methods or custom behavior to the stub
- The class has virtual or abstract members you want to intercept
- You prefer explicit, discoverable stub classes in IntelliSense
- You cannot or don't want to extract an interface

### Basic Setup

<!-- snippet: patterns-standalone-class-basic -->
```cs
public abstract class ServiceBaseNonGeneric
{
    public abstract string Name { get; }
    public abstract void Execute(string command);
}

[KnockOffBase<ServiceBaseNonGeneric>]
public partial class ServiceBaseStub { }
```
<!-- endSnippet -->

### Usage in Tests

<!-- snippet: patterns-standalone-class-usage -->
```cs
// Standalone Class: configure stub, use .Object for the class instance
var stub = new ServiceBaseStub();
stub.Name.Get(() => "test").Verifiable();
stub.Execute.Call((cmd) => { }).Verifiable();
ServiceBaseNonGeneric service = stub.Object;
```
<!-- endSnippet -->

### Benefits

- **Reusable**: Reference the stub from any test file
- **User methods and properties**: Add custom methods and override properties directly on the stub class
- **Discoverable**: Appears in IntelliSense when browsing your test project
- **Explicit**: Clear separation between test code and stub implementation
- **No interface needed**: Stub classes directly without creating interfaces

### Trade-offs

- **Extra file**: Requires a dedicated .cs file for each stub
- **Partial class**: Must remember to mark the class as `partial`
- **Must use .Object**: The stub is a wrapper; use `.Object` property to get the actual instance
- **Virtual/abstract only**: Only overrides members marked `virtual` or `abstract`

### Standalone Class vs Inline Class

| Aspect | Standalone Class | Inline Class |
|--------|------------------|--------------|
| **Syntax** | `[KnockOffBase<Foo>] class Stub` | `[KnockOff<Foo>]` |
| **Instantiation** | `new ServiceStub().Object` | `new Stubs.Service().Object` |
| **Reusability** | Across test files | Within one test class |
| **User methods/properties** | Yes | No |
| **Best for** | Shared class stubs | One-time use |

---

## Generic Standalone Class Pattern

The Generic Standalone Class pattern creates a reusable generic stub class for generic base classes that can be instantiated with different type arguments across your test suite.

### When to Use

- You need a reusable stub for a generic class (e.g., `RepositoryBase<T>`, `ServiceBase<T>`)
- You want to use the same stub definition with different type arguments
- You need the same stub in multiple test files with various types
- The class has virtual or abstract members you want to intercept
- You prefer clean instantiation syntax with type parameters

### Basic Setup

<!-- snippet: patterns-generic-standalone-class-basic -->
```cs
public abstract class RepositoryBase<T> where T : class
{
    public abstract T? GetItem(int id);
    public abstract void Save(T entity);
}

[KnockOffBase(typeof(RepositoryBase<>))]
public partial class RepositoryBaseStub<T> where T : class { }
```
<!-- endSnippet -->

### Usage in Tests

<!-- snippet: patterns-generic-standalone-class-usage -->
```cs
// Generic Standalone Class: reusable across multiple type arguments, uses .Object
var stub = new RepositoryBaseStub<User>();
stub.GetItem.Return((id) => new User { Id = id, Name = "Test" }).Verifiable();
stub.Save.Call((entity) => { }).Verifiable();
RepositoryBase<User> service = stub.Object;
```
<!-- endSnippet -->

### Benefits

- **Single definition**: Define once, use with any type argument
- **Reusable**: Share across multiple test files
- **Type-safe**: Compiler enforces type constraints
- **Clean syntax**: `new RepositoryStub<User>().Object` - clear and readable
- **User methods and properties**: Supports custom helper methods and property overrides like Standalone patterns
- **No interface needed**: Stub generic classes directly

### Trade-offs

- **Extra file**: Requires a dedicated .cs file for the stub
- **Partial class**: Must mark as `partial`
- **Constraints must match**: Type constraints must mirror the base class
- **Must use .Object**: The stub is a wrapper; use `.Object` property to get the actual instance
- **Virtual/abstract only**: Only overrides members marked `virtual` or `abstract`

### Generic Standalone Class vs Open Generic Class

| Aspect | Generic Standalone Class | Open Generic Class |
|--------|--------------------------|-------------------|
| **Syntax** | `[KnockOffBase(typeof(Foo<>))] class Stub<T>` | `[KnockOff(typeof(Foo<>))]` |
| **Instantiation** | `new Stub<User>().Object` | `new Stubs.Foo<User>().Object` |
| **Reusability** | Across test files | Within one test class |
| **User methods/properties** | Yes | No |
| **Best for** | Shared generic class stubs | One-time use |

---

## Inline Interface Pattern

The Inline Interface pattern generates a stub class scoped to your test class. The stub is accessed through a nested `Stubs` namespace.

### When to Use

- You need a stub only within one test class
- You don't need custom methods on the stub
- You want minimal ceremony and no extra files
- The interface is non-generic or you want a closed generic stub

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
// Inline Interface: access via Stubs namespace
var stub = new Stubs.IUserRepoInline();
stub.GetById.Return((id) => new User { Id = id, Name = "Test" }).Verifiable();
stub.Save.Call((user) => { }).Verifiable();
```
<!-- endSnippet -->

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
// Inline Class: configure stub, use .Object for the class instance
var stub = new Stubs.UserServiceClass();
stub.GetUser.Return((id) => new User { Id = id, Name = "FromStub" }).Verifiable();
UserServiceClass service = stub.Object;
```
<!-- endSnippet -->

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

<!-- snippet: patterns-inline-delegate-basic -->
```cs
// Define delegate types
public delegate bool ValidationRule(string value);
public delegate T Factory<T>();

[KnockOff<ValidationRule>]
[KnockOff<Factory<User>>]
public partial class InlineDelegateTests
{
    // The generator creates Stubs.ValidationRule and Stubs.Factory<User>
}
```
<!-- endSnippet -->

### Usage in Tests

<!-- snippet: patterns-inline-delegate-usage -->
```cs
// Inline Delegate: configure via Interceptor, implicit conversion to delegate
var ruleStub = new Stubs.ValidationRule();
ruleStub.Interceptor.Return((value) => value != "invalid");
ValidationRule rule = ruleStub;
```
<!-- endSnippet -->

### Benefits

- **Implicit conversion**: Stub converts to delegate type automatically
- **Invocation tracking**: Use `Verify()`, `LastArg`, `LastArgs`
- **Behavior configuration**: Use `Return`, `Call`, sequences, and When chains
- **Async auto-wrapping**: `Return(42)` auto-wraps for `Task<int>` delegates
- **Verification**: Use `Verify()`, `Called` constraints, and `Verifiable()` chaining
- **Strict mode**: `stub.Strict = true` throws on unconfigured invocations

### Trade-offs

- **Interceptor property**: Access tracking via `stub.Interceptor` (not named member properties)
- **Test-local only**: Cannot reuse across multiple test classes
- **Named delegates only**: Cannot stub `Func<T>` or `Action<T>` directly — define a named delegate

---

## Open Generic Interface Pattern

The Open Generic Interface pattern generates a generic stub class within your test class that can be instantiated with any type argument. Use this when you need a test-local generic interface stub without creating a separate file.

### When to Use

- You need a generic interface stub only within one test class
- You don't need custom methods on the stub
- You want to test with multiple type arguments in one test class
- You prefer inline definition over a separate file

### Basic Setup

<!-- snippet: patterns-open-generic-basic -->
```cs
[KnockOff(typeof(IServiceOpenGeneric<>))]
public partial class OpenGenericTests
{
    // The generator creates Stubs.IServiceOpenGeneric<T>
}
```
<!-- endSnippet -->

### Usage in Tests

<!-- snippet: patterns-open-generic-usage -->
```cs
// Open Generic: instantiate with any type argument
var userStub = new Stubs.IServiceOpenGeneric<User>();
userStub.GetItem.Return((id) => new User { Id = id, Name = "FromStub" }).Verifiable();

var productStub = new Stubs.IServiceOpenGeneric<Product>();
productStub.GetItem.Return((id) => new Product { Id = id, Name = "FromStub" }).Verifiable();
```
<!-- endSnippet -->

### Benefits

- **Flexible**: Use any type argument without defining separate stubs
- **No extra files**: Stub defined inline with tests
- **Type constraints**: Preserves constraints from the original generic type
- **Multiple types**: Use different type arguments in the same test class
- **Direct assignment**: Stub IS the interface implementation (no `.Object` needed)

### Trade-offs

- **Test-local only**: Cannot reuse across multiple test classes
- **No user methods**: Cannot add custom methods to the generated stub
- **typeof syntax**: Requires `typeof(IFoo<>)` with empty angle brackets
- **Stubs namespace**: Must use `Stubs.IFoo<T>` syntax

> **NOTE:** For reusable generic stubs across multiple test files, use the [Generic Standalone](#generic-standalone-pattern) pattern instead.

---

## Open Generic Class Pattern

The Open Generic Class pattern generates a generic stub class within your test class for stubbing abstract or virtual generic classes. Like the Inline Class pattern, you access the actual instance via the `.Object` property.

### When to Use

- You need to stub a generic abstract or virtual class
- You don't need custom methods on the stub
- You want to test with multiple type arguments in one test class
- You prefer inline definition over a separate file

### Basic Setup

<!-- snippet: patterns-open-generic-class-basic -->
```cs
public abstract class ServiceBaseOpenGeneric<T>
{
    public abstract T? GetItem(int id);
    public abstract void Process(T item);
}

[KnockOff(typeof(ServiceBaseOpenGeneric<>))]
public partial class OpenGenericClassTests
{
    // The generator creates Stubs.ServiceBaseOpenGeneric<T>
}
```
<!-- endSnippet -->

### Usage in Tests

<!-- snippet: patterns-open-generic-class-usage -->
```cs
// Open Generic Class: instantiate with any type argument, use .Object
var userStub = new Stubs.ServiceBaseOpenGeneric<User>();
userStub.GetItem.Return((id) => new User { Id = id, Name = "FromStub" }).Verifiable();

// IMPORTANT: .Object gives you the actual class instance
ServiceBaseOpenGeneric<User> service = userStub.Object;
var user = service.GetItem(1);

userStub.Verify();
```
<!-- endSnippet -->

### Benefits

- **Flexible**: Use any type argument without defining separate stubs
- **No extra files**: Stub defined inline with tests
- **Type constraints**: Preserves constraints from the original generic type
- **Multiple types**: Use different type arguments in the same test class
- **Class support**: Works with abstract classes, not just interfaces

### Trade-offs

- **Must use .Object**: The stub is a wrapper; use `.Object` property to get the actual instance
- **Test-local only**: Cannot reuse across multiple test classes
- **No user methods**: Cannot add custom methods to the generated stub
- **typeof syntax**: Requires `typeof(Foo<>)` with empty angle brackets
- **Virtual/abstract only**: Only overrides members marked `virtual` or `abstract`

### Key Difference from Open Generic Interface

| Aspect | Open Generic Interface | Open Generic Class |
|--------|------------------------|-------------------|
| **Syntax** | `[KnockOff(typeof(IFoo<>))]` | `[KnockOff(typeof(Foo<>))]` |
| **Instantiation** | `new Stubs.IFoo<T>()` | `new Stubs.Foo<T>().Object` |
| **Assignment** | `IFoo<T> foo = stub;` | `Foo<T> foo = stub.Object;` |
| **Best for** | Generic interfaces | Generic abstract/virtual classes |

> **NOTE:** For reusable generic stubs across multiple test files, use the [Generic Standalone](#generic-standalone-pattern) pattern instead.

---

## Pattern Comparison

| Feature | Standalone | Generic Standalone | Standalone Class | Generic Standalone Class | Inline Interface | Inline Class | Inline Delegate | Open Generic Interface | Open Generic Class |
|---------|------------|-------------------|-----------------|--------------------------|------------------|--------------|-----------------|----------------------|-------------------|
| **Reusable across test files** | Yes | Yes | Yes | Yes | No | No | No | No | No |
| **Custom user methods/properties** | Yes | Yes | Yes | Yes | No | No | No | No | No |
| **Extra file required** | Yes | Yes | Yes | Yes | No | No | No | No | No |
| **Supports interfaces** | Yes | Yes | No | No | Yes | No | No | Yes | No |
| **Supports classes** | No | No | Yes | Yes | No | Yes | No | No | Yes |
| **Supports delegates** | No | No | No | No | No | No | Yes | Yes* | No |
| **Supports generics** | No | Yes | No | Yes | Closed only | Closed only | Closed only | Yes | Yes |
| **Uses .Object property** | No | No | Yes | Yes | No | Yes | No | No | Yes |
| **Instantiation syntax** | `new MyStub()` | `new MyStub<T>()` | `new MyStub().Object` | `new MyStub<T>().Object` | `new Stubs.IFoo()` | `new Stubs.Foo().Object` | `new Stubs.Del()` | `new Stubs.IFoo<T>()` | `new Stubs.Foo<T>().Object` |
| **Best for** | Shared interface stubs | Shared generic interface stubs | Shared class stubs | Shared generic class stubs | Local interface stubs | Local class stubs | Delegate stubs | Local generic interface stubs | Local generic class stubs |

*Note: Open Generic Delegate (`[KnockOff(typeof(Factory<>))]`) behaves like Open Generic Interface (no `.Object`), as delegates are reference types that can be directly assigned.

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
    |   |-- YES --> Is it a CLASS (not interface)?
    |   |   |
    |   |   |-- YES --> Generic Standalone Class pattern
    |   |   |           [KnockOffBase(typeof(ClassBase<>))] class Stub<T>
    |   |   |           Use: new Stub<T>().Object
    |   |   |
    |   |   |-- NO --> Generic Standalone pattern
    |   |               [KnockOff] class Stub<T> : IRepo<T>
    |   |               Use: new Stub<T>()
    |   |
    |   |-- NO --> Is it a CLASS (not interface)?
    |       |
    |       |-- YES --> Open Generic Class pattern
    |       |           [KnockOff(typeof(ServiceBase<>))]
    |       |           Use: new Stubs.ServiceBase<T>().Object
    |       |
    |       |-- NO --> Do you need CUSTOM METHODS on the stub?
    |           |
    |           |-- YES --> Generic Standalone pattern
    |           |           [KnockOff] class Stub<T> : IRepo<T>
    |           |
    |           |-- NO --> Open Generic Interface pattern
    |                      [KnockOff(typeof(IRepo<>))]
    |                      Use: new Stubs.IRepo<T>()
    |
    |-- NO --> Is it a CLASS (not interface)?
        |
        |-- YES --> Do you need the stub in MULTIPLE test files?
        |   |
        |   |-- YES --> Standalone Class pattern
        |   |           [KnockOffBase<SomeClass>] class Stub
        |   |           Use: new Stub().Object
        |   |
        |   |-- NO --> Inline Class pattern
        |               [KnockOff<SomeClass>]
        |               Use: new Stubs.SomeClass().Object
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
| Interface repository stub used in 5+ test classes | Standalone |
| Interface stub with `WithAdminUser()` helper method | Standalone |
| Generic interface repository shared across tests | Generic Standalone |
| Class stub used in 5+ test classes | Standalone Class |
| Class stub with custom tracking methods | Standalone Class |
| Generic class repository shared across tests | Generic Standalone Class |
| Quick interface stub for single test class | Inline Interface |
| Stub a `DbContext` with virtual `DbSet` properties for one test | Inline Class |
| Stub an abstract base class for one test | Inline Class |
| Stub a validation rule delegate | Inline Delegate |
| Stub a factory function delegate | Inline Delegate |
| Generic interface stub for one test class | Open Generic Interface |
| `IRepository<T>` for multiple types in one test | Open Generic Interface |
| Generic abstract class stub for one test class | Open Generic Class |
| `ServiceBase<T>` with virtual methods for one test | Open Generic Class |

---

## Complete Example

This example demonstrates all nine patterns working together in a realistic test scenario.

<!-- snippet: patterns-complete-example-nine -->
```cs
// STANDALONE PATTERNS (file-based, reusable across tests)

// 1. Standalone Interface: direct instantiation, stub IS implementation
var emailStub = new EmailSvcPatternStub();
emailStub.Send.Return((to, subject, body) => true).Verifiable();
IEmailSvcPattern email = emailStub;

// 2. Generic Standalone Interface: reusable with type args
var notifierStub = new NotifierStub<User>();
notifierStub.Notify.Call((item) => { }).Verifiable();
INotifier<User> notifier = notifierStub;

// 3. Standalone Class: stub wraps instance, use .Object
var serviceBaseStub = new ServiceBaseStub();
serviceBaseStub.Name.Get(() => "TestService").Verifiable();
serviceBaseStub.Execute.Call((cmd) => { }).Verifiable();
ServiceBaseNonGeneric serviceBase = serviceBaseStub.Object;

// 4. Generic Standalone Class: reusable class stub with type args
var repoStub = new RepositoryBaseStub<Product>();
repoStub.GetItem.Return((id) => new Product { Id = id, Name = "Widget" }).Verifiable();
repoStub.Save.Call((entity) => { }).Verifiable();
RepositoryBase<Product> repo = repoStub.Object;

// INLINE PATTERNS (nested within test class)

// 5. Inline Interface: via Stubs namespace
var loggerStub = new CompleteExampleInlineHost.Stubs.ILogSvc();
loggerStub.Log.Call((msg) => { }).Verifiable();
ILogSvc logger = loggerStub;

// 6. Inline Class: use .Object for class instance
var auditStub = new CompleteExampleInlineHost.Stubs.AuditSvcBase();
auditStub.Audit.Call((action) => { }).Verifiable();
AuditSvcBase audit = auditStub.Object;

// 7. Inline Delegate: implicit conversion
var ruleStub = new InlineDelegateTests.Stubs.ValidationRule();
ruleStub.Interceptor.Return((value) => true);
ValidationRule rule = ruleStub;

// 8. Open Generic Interface: inline stub with type args
var processorStub = new CompleteExampleOpenGenericHost.Stubs.IProcessor<Order>();
processorStub.Process.Call((item) => { }).Verifiable();
IProcessor<Order> processor = processorStub;

// 9. Open Generic Class: inline stub with type args, uses .Object
var serviceStub = new CompleteExampleOpenGenericClassHost.Stubs.ServiceBase<Order>();
serviceStub.GetItem.Return((id) => new Order { Id = id }).Verifiable();
ServiceBase<Order> service = serviceStub.Object;
```
<!-- endSnippet -->

---

## Next Steps

- **[Getting Started](../getting-started.md)** - Learn basic stub creation
- **[Methods Guide](methods.md)** - Configure method behavior with Returns and Execute
- **[Properties Guide](properties.md)** - Work with property interceptors
- **[Delegates Guide](delegates.md)** - Stub delegate types for callbacks and validation
- **[Interceptor API Reference](../reference/interceptor-api.md)** - Complete API documentation

---

**UPDATED:** 2026-02-04 (Nine patterns including Standalone Class stubs)
