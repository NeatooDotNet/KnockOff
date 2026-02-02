[Home](../../README.md) > [Guides](.) > Stub Patterns

# Stub Patterns

KnockOff supports three fundamental patterns for creating test stubs: Stand-Alone, Inline Interface, and Inline Class. Each pattern solves different testing scenarios with varying trade-offs in reusability, ceremony, and capabilities.

The Inline Interface pattern also supports **delegate types**, allowing you to stub validation rules, factories, and callbacks. This specialized use case is covered in the Inline Delegate section below.

## Quick Decision Guide

| If you need... | Use this pattern |
|----------------|------------------|
| Reusable stub across multiple test files | Stand-Alone / Flat |
| Custom methods on your stub | Stand-Alone / Flat |
| Quick, test-local stub | Inline Interface |
| No extra stub files | Inline Interface |
| Stub a class (not interface) | Inline Class |
| Stub a delegate type | Inline Delegate |

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
public partial class UserRepoStandaloneStub : IUserRepoStandalone { }
```
<!-- endSnippet -->

### Usage in Tests

<!-- snippet: patterns-standalone-usage -->
```cs
// Stand-Alone: instantiate like any class, configure via Verify()
var stub = new UserRepoStandaloneStub();
stub.GetById.OnCall((id) => new User { Id = id, Name = $"User{id}" }).Verifiable();
stub.Save.OnCall((user) => { }).Verifiable();
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
// Inline Interface: access via Stubs namespace
var stub = new Stubs.IUserRepoInline();
stub.GetById.OnCall((id) => new User { Id = id, Name = "Test" }).Verifiable();
stub.Save.OnCall((user) => { }).Verifiable();
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
// Inline Class: configure stub, use .Object for the class instance
var stub = new Stubs.UserServiceClass();
stub.GetUser.OnCall((id) => new User { Id = id, Name = "FromStub" }).Verifiable();
UserServiceClass service = stub.Object;
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

## Inline Delegate Pattern (Specialized Use Case)

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
ruleStub.Interceptor.OnCall((value) => value != "invalid");
ValidationRule rule = ruleStub;
```
<!-- endSnippet -->

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

## Pattern Comparison

**Note**: Inline Delegate is a specialized use case of the Inline Interface pattern for delegate types.

| Feature | Stand-Alone | Inline Interface | Inline Class | Inline Delegate |
|---------|-------------|------------------|--------------|-----------------|
| **Reusable across test files** | Yes | No | No | No |
| **Custom user methods** | Yes | No | No | No |
| **Extra file required** | Yes | No | No | No |
| **Supports interfaces** | Yes | Yes | No | No |
| **Supports classes** | No | No | Yes | No |
| **Supports delegates** | No | No | No | Yes |
| **IntelliSense visible** | Yes | Within test class | Within test class | Within test class |
| **Instantiation syntax** | `new MyStub()` | `new Stubs.IFoo()` | `new Stubs.Foo().Object` | `new Stubs.DelegateName()` |
| **Best for** | Shared stubs | Local stubs | Class stubs | Delegate stubs |

---

## Choosing a Pattern

Follow this decision tree to select the appropriate pattern:

1. **Do you need to stub a delegate type?**
   - Yes → **Inline Delegate** pattern (specialized use of Inline Interface)
   - No → Continue to step 2

2. **Do you need to stub a class (not an interface)?**
   - Yes → **Inline Class** pattern
   - No → Continue to step 3

3. **Do you need the stub in multiple test files?**
   - Yes → **Stand-Alone** pattern
   - No → Continue to step 4

4. **Do you need custom methods on the stub?**
   - Yes → **Stand-Alone** pattern
   - No → **Inline Interface** pattern

**The three fundamental patterns** (Stand-Alone, Inline Interface, Inline Class) cover all architectural scenarios. Inline Delegate is a specialized application of Inline Interface for delegate types.

### Examples by Scenario

| Scenario | Recommended Pattern |
|----------|---------------------|
| Repository stub used in 5+ test classes | Stand-Alone |
| Stub with `WithAdminUser()` helper method | Stand-Alone |
| Quick stub for single test class | Inline Interface |
| Stub a `DbContext` with virtual `DbSet` properties | Inline Class |
| Stub an abstract base class | Inline Class |
| Stub a validation rule delegate | Inline Delegate |
| Stub a factory function delegate | Inline Delegate |
| Stub an event handler delegate | Inline Delegate |

---

## Complete Example

This example demonstrates all three fundamental patterns (Stand-Alone, Inline Interface, Inline Class) working together in a realistic test scenario.

<!-- snippet: patterns-complete-example -->
```cs
// Stand-Alone: direct instantiation
var emailStub = new EmailSvcPatternStub();
emailStub.Send.OnCall((to, subject, body) => true).Verifiable();

// Inline Interface: via Stubs namespace
var loggerStub = new Stubs.ILogSvc();
loggerStub.Log.OnCall((msg) => { }).Verifiable();

// Inline Class: use .Object for class instance
var auditStub = new Stubs.AuditSvcBase();
auditStub.Audit.OnCall((action) => { }).Verifiable();
AuditSvcBase audit = auditStub.Object;
```
<!-- endSnippet -->

---

## Next Steps

- **[Getting Started](../getting-started.md)** - Learn basic stub creation
- **[Methods Guide](methods.md)** - Configure method behavior with OnCall
- **[Properties Guide](properties.md)** - Work with property interceptors
- **[Delegates Guide](delegates.md)** - Stub delegate types for callbacks and validation
- **[Interceptor API Reference](../reference/interceptor-api.md)** - Complete API documentation

---

**UPDATED:** 2026-01-25
