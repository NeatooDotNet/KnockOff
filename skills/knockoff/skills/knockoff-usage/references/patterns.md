# KnockOff Stub Patterns Reference

[Home](../../../../README.md) > [Documentation](../../../../docs/README.md) > [Guides](../../../../docs/guides/README.md) > Stub Patterns

KnockOff supports four patterns for creating test stubs. Each pattern solves different testing scenarios with varying trade-offs in reusability, ceremony, and capabilities.

---

## Quick Decision Guide

| If you need... | Use this pattern |
|----------------|------------------|
| Reusable stub across multiple test files | Stand-Alone |
| Custom methods on your stub | Stand-Alone |
| Quick, test-local stub | Inline Interface |
| No extra stub files | Inline Interface |
| Stub a class (not interface) | Inline Class |
| Stub a delegate | Inline Delegate |

---

## Stand-Alone Pattern

The Stand-Alone pattern creates a dedicated stub class in its own file. This stub can be reused across test files and supports adding custom methods.

### When to Use

- You need the same stub in multiple test files
- You want to add helper methods or custom behavior to the stub
- You prefer explicit, discoverable stub classes in IntelliSense

### Declaration

Create a partial class with the `[KnockOff]` attribute that implements the interface you want to stub:

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

The source generator produces:
- A base class with virtual protected methods (e.g., `GetById_`) for user method overrides
- Explicit interface implementations for all members
- Interceptor properties named after each interface member (e.g., `Save`, `GetById`)

### Usage in Tests

<!-- snippet: patterns-standalone-usage -->
```cs
// Stand-Alone: instantiate like any class, configure via Verify()
var stub = new UserRepoStandaloneStub();
stub.GetById.OnCall((id) => new User { Id = id, Name = $"User{id}" }).Verifiable();
stub.Save.OnCall((user) => { }).Verifiable();
```
<!-- endSnippet -->

### Key Points

- **Instantiation**: `new MyStub()`
- **Interface access**: Cast to interface type or assign to interface variable
- **Verification**: Call `stub.Verify()` or individual interceptor `.Verify()`
- **User methods**: Override virtual methods (with underscore suffix, e.g., `GetById_`) to provide default behavior
- **Clean interceptor names**: The interceptor uses the clean name (e.g., `GetById`) regardless of whether a user override exists
- **OnCall supersedes user method**: Use `stub.GetById.OnCall(...)` to override the user method for specific tests
- **User method as fallback**: When no `OnCall` is configured, the user override is called

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

### Declaration

Apply `[KnockOff<IInterface>]` to your test class:

<!-- snippet: patterns-inline-interface-basic -->
```cs
[KnockOff<IUserRepoInline>]
public partial class InlineInterfaceTests
{
    // The generator creates Stubs.IUserRepoInline
}
```
<!-- endSnippet -->

The source generator produces a nested `Stubs` class containing the stub implementation.

### Usage in Tests

<!-- snippet: patterns-inline-interface-usage -->
```cs
// Inline Interface: access via Stubs namespace
var stub = new Stubs.IUserRepoInline();
stub.GetById.OnCall((id) => new User { Id = id, Name = "Test" }).Verifiable();
stub.Save.OnCall((user) => { }).Verifiable();
```
<!-- endSnippet -->

### Key Points

- **Instantiation**: `new Stubs.IInterfaceName()`
- **Interface access**: Cast to interface type or assign to interface variable
- **Verification**: Call `stub.Verify()` or individual interceptor `.Verify()`
- **Scoping**: Stub only exists within the test class where it is declared

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

### Declaration

Apply `[KnockOff<ClassName>]` to your test class, targeting a class with virtual or abstract members:

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

The source generator produces:
- A wrapper class in the nested `Stubs` namespace
- A derived class that overrides all virtual/abstract members
- An `Object` property to access the actual class instance

### Usage in Tests

<!-- snippet: patterns-inline-class-usage -->
```cs
// Inline Class: configure stub, use .Object for the class instance
var stub = new Stubs.UserServiceClass();
stub.GetUser.OnCall((id) => new User { Id = id, Name = "FromStub" }).Verifiable();
UserServiceClass service = stub.Object;
```
<!-- endSnippet -->

### Key Points

- **Instantiation**: `new Stubs.ClassName()`
- **Class access**: Use the `.Object` property to get the actual class instance
- **Verification**: Call `stub.Verify()` or individual interceptor `.Verify()`
- **Virtual/abstract only**: Only members marked `virtual` or `abstract` can be intercepted

### Benefits

- **Stub classes**: Works with classes, not just interfaces
- **No interface extraction**: Avoids creating interfaces just for testing
- **Virtual members**: Intercepts any `virtual` or `abstract` members

### Trade-offs

- **Must use .Object**: The stub is a wrapper; use `.Object` property to get the actual instance
- **Virtual/abstract only**: Only overrides members marked `virtual` or `abstract`
- **No user methods**: Cannot add custom methods like Stand-Alone pattern

---

## Inline Delegate Pattern

The Inline Delegate pattern generates a stub for delegate types. This allows stubbing callbacks and factory patterns without creating wrapper interfaces.

### When to Use

- You need to stub a delegate type
- You want to track delegate invocations
- You need to configure delegate behavior dynamically in tests

### Declaration

Apply `[KnockOff<DelegateType>]` to your test class, targeting a delegate type:

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

The source generator produces:
- A wrapper class in the nested `Stubs` namespace
- An `Interceptor` property for configuring behavior and tracking calls
- Implicit conversion operator to the delegate type

### Usage in Tests

<!-- snippet: patterns-inline-delegate-usage -->
```cs
// Inline Delegate: configure via Interceptor, implicit conversion to delegate
var ruleStub = new Stubs.ValidationRule();
ruleStub.Interceptor.OnCall((value) => value != "invalid");
ValidationRule rule = ruleStub;
```
<!-- endSnippet -->

### Key Points

- **Instantiation**: `new Stubs.DelegateType()`
- **Delegate access**: Implicit conversion to delegate type
- **Configuration**: Use `stub.Interceptor.OnCall(...)` to configure behavior
- **Verification**: Call `stub.Interceptor.Verify()` to verify invocations
- **Argument tracking**: Access `stub.Interceptor.LastCallArg` for the last argument passed

### Benefits

- **Stub delegates**: Works with any delegate type, including `Func<T>` and `Action<T>`
- **Invocation tracking**: Tracks all delegate invocations automatically
- **Flexible configuration**: Configure behavior dynamically per test

### Trade-offs

- **Interceptor syntax**: Must use `stub.Interceptor` to access configuration (not directly on stub)
- **No user methods**: Cannot add custom methods like Stand-Alone pattern
- **Test-local only**: Cannot reuse across multiple test classes

---

## Pattern Comparison

| Feature | Stand-Alone | Inline Interface | Inline Class | Inline Delegate |
|---------|-------------|------------------|--------------|-----------------|
| **Reusable across test files** | Yes | No | No | No |
| **Custom user methods** | Yes | No | No | No |
| **Extra file required** | Yes | No | No | No |
| **Supports interfaces** | Yes | Yes | No | No |
| **Supports classes** | No | No | Yes | No |
| **Supports delegates** | No | No | No | Yes |
| **IntelliSense visible** | Yes | Within test class | Within test class | Within test class |
| **Instantiation syntax** | `new MyStub()` | `new Stubs.IFoo()` | `new Stubs.Foo()` | `new Stubs.DelegateType()` |
| **Access pattern** | Cast to interface | Cast to interface | Use `.Object` property | Implicit conversion |
| **Configuration** | `stub.Member.OnCall(...)` | `stub.Member.OnCall(...)` | `stub.Member.OnCall(...)` | `stub.Interceptor.OnCall(...)` |
| **Best for** | Shared stubs | Local stubs | Class stubs | Delegate stubs |

---

## Decision Tree

Follow this decision tree to choose the right pattern:

1. **Do you need to stub a delegate type?**
   - Yes: Use **Inline Delegate** pattern
   - No: Continue to step 2

2. **Do you need to stub a class (not an interface)?**
   - Yes: Use **Inline Class** pattern
   - No: Continue to step 3

3. **Do you need the stub in multiple test files?**
   - Yes: Use **Stand-Alone** pattern
   - No: Continue to step 4

4. **Do you need custom methods on the stub?**
   - Yes: Use **Stand-Alone** pattern
   - No: Use **Inline Interface** pattern

### Scenario Recommendations

| Scenario | Recommended Pattern |
|----------|---------------------|
| Repository stub used in 5+ test classes | Stand-Alone |
| Stub with `WithAdminUser()` helper method | Stand-Alone |
| Quick stub for single test class | Inline Interface |
| Stub a `DbContext` with virtual `DbSet` properties | Inline Class |
| Stub an abstract base class | Inline Class |
| Stub a validation callback or factory delegate | Inline Delegate |

---

## Complete Example: All Patterns Together

This example demonstrates using all patterns in a realistic test scenario:

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

### Pattern Usage Summary in the Example

| Stub | Pattern | Reason |
|------|---------|--------|
| `EmailSvcPatternStub` | Stand-Alone | Reusable across test files, could have helper methods |
| `Stubs.ILogSvc` | Inline Interface | Only needed in this test class |
| `Stubs.AuditSvcBase` | Inline Class | Stubbing an abstract class, not an interface |

---

**UPDATED:** 2026-01-26
