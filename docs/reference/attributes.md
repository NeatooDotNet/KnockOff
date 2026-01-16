# Attributes Reference

KnockOff provides two attributes for creating stubs.

## [KnockOff] (Standalone)

Marks a class for standalone stub generation. The class must implement an interface.

### Usage

<!-- snippet: attr-standalone-basic -->
```cs
[KnockOff]
public partial class AttrUserRepositoryStub : IAttrUserRepository { }
```
<!-- endSnippet -->

### Requirements

| Requirement | Description |
|-------------|-------------|
| `partial` keyword | Required for generator to add code |
| Implements interface | Must implement exactly one interface |
| Not `abstract` | Must be instantiable |
| Not `static` | Must be instance class |

### Valid Examples

<!-- snippet: attr-standalone-valid -->
```cs
// Basic usage
[KnockOff]
public partial class AttrServiceStub : IAttrService { }

// Generic interface (with concrete type)
[KnockOff]
public partial class AttrUserRepoStub : IAttrRepository<AttrUser> { }

// Internal visibility
[KnockOff]
internal partial class AttrInternalServiceStub : IAttrService { }

// Nested in test class
public partial class AttrMyTests
{
    [KnockOff]
    public partial class NestedStub : IAttrService { }
}
```
<!-- endSnippet -->

### Invalid Examples

<!-- invalid:attr-standalone-invalid -->
```csharp
// Missing partial - COMPILE ERROR
[KnockOff]
public class ServiceStub : IService { }

// Multiple interfaces - KO0010 ERROR
[KnockOff]
public partial class MultiStub : IService, IDisposable { }

// No interface - no members generated
[KnockOff]
public partial class EmptyStub { }
```
<!-- /snippet -->

## [KnockOff<T>] (Inline)

Generates a nested stub class inside the annotated class. Best for test-class-scoped stubs.

### Usage

<!-- snippet: attr-inline-usage -->
```cs
[KnockOff<IAttrUserRepository>]
[KnockOff<IAttrEmailService>]
public partial class AttrUserServiceTests
{
    public void Test()
    {
        var repoStub = new Stubs.IAttrUserRepository();
        var emailStub = new Stubs.IAttrEmailService();

        _ = (repoStub, emailStub);
    }
}
```
<!-- endSnippet -->

### Requirements

| Requirement | Description |
|-------------|-------------|
| `partial` keyword | Required on the containing class |
| Type argument | Must be an interface, class, or delegate |

### What Gets Generated

For each `[KnockOff<T>]` attribute, a nested `Stubs` class is generated containing:

<!-- pseudo:attr-generated-stubs -->
```csharp
public partial class UserServiceTests
{
    public static partial class Stubs
    {
        public partial class IUserRepository : global::IUserRepository { /* ... */ }
        public partial class IEmailService : global::IEmailService { /* ... */ }
    }
}
```
<!-- /snippet -->

### Interface Stubs

<!-- snippet: attr-inline-interface -->
```cs
[KnockOff<IAttrUserRepository>]
public partial class AttrInterfaceTests
{
    public void Test()
    {
        var stub = new Stubs.IAttrUserRepository();
        stub.GetById.OnCall = (ko, id) => new AttrUser { Id = id };

        IAttrUserRepository repo = stub;  // Implicit conversion

        _ = repo;
    }
}
```
<!-- endSnippet -->

### Class Stubs

For classes (must be unsealed with virtual/abstract members):

<!-- snippet: attr-inline-class -->
```cs
[KnockOff<AttrEmailServiceClass>]
public partial class AttrClassTests
{
    public void Test()
    {
        var stub = new Stubs.AttrEmailServiceClass("smtp.test.com", 587);
        stub.Send.OnCall = (ko, to, body) => { };

        AttrEmailServiceClass service = stub.Object;  // Use .Object for class instance

        _ = service;
    }
}
```
<!-- endSnippet -->

### Delegate Stubs

For `Func<>`, `Action<>`, or named delegates:

<!-- snippet: attr-inline-delegate -->
```cs
[KnockOff<Func<int, string>>]
[KnockOff<AttrValidationRule>]  // Named delegate
public partial class AttrDelegateTests
{
    public void Test()
    {
        var funcStub = new Stubs.Func();
        funcStub.Interceptor.OnCall = (ko, id) => $"Item-{id}";

        Func<int, string> func = funcStub;  // Implicit conversion

        _ = func;
    }
}
```
<!-- endSnippet -->

### Stub Naming

| Type Argument | Generated Stub Name |
|---------------|---------------------|
| `IUserRepository` | `Stubs.IUserRepository` |
| `EmailService` | `Stubs.EmailService` |
| `Func<int, string>` | `Stubs.Func_int_string` |
| `Action<string>` | `Stubs.Action_string` |

## Namespace

Both attributes are in the `KnockOff` namespace:

<!-- pseudo:attr-namespace-using -->
```csharp
using KnockOff;
```
<!-- /snippet -->

Or fully qualified:

<!-- snippet: attr-namespace-qualified -->
```cs
[KnockOff.KnockOff]
public partial class AttrFullyQualifiedStub : IAttrService { }

[KnockOff.KnockOff<IAttrService>]
public partial class AttrFullyQualifiedTests { }
```
<!-- endSnippet -->

