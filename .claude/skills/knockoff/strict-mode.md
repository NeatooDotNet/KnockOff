# Strict Mode

By default, KnockOff stubs return smart defaults for unconfigured methods. **Strict mode** throws `StubException` instead, catching unexpected interactions.

## Enabling Strict Mode

### Extension Method (Recommended)

Use the fluent `.Strict()` extension method:

<!-- pseudo:strict-mode-extension -->
```csharp
// Standalone stub
var stub = new UserServiceKnockOff().Strict();

// Inline stub
var stub = new Stubs.IUserService().Strict();

// Works with any stub type - returns same instance for chaining
```
<!-- /snippet -->

### Standalone Stubs

<!-- pseudo:strict-mode-standalone -->
```csharp
var stub = new UserServiceKnockOff().Strict();

IUserService service = stub;
// service.GetUser(1);  // Would throw StubException - no OnCall configured!
```
<!-- /snippet -->

### Inline Stubs

Use constructor parameter or extension method:

<!-- pseudo:strict-mode-inline -->
```csharp
// Constructor parameter
var stub = new Stubs.IUserService(strict: true);

// Or extension method
var stub = new Stubs.IUserService().Strict();

stub.GetUser.OnCall = (ko, id) => new User { Id = id };  // Configure what you expect

IUserService service = stub;
_ = service.GetUser(1);     // OK - OnCall configured
// service.DeleteUser(1);  // Would throw StubException - no OnCall configured!
```
<!-- /snippet -->

## Attribute Defaults

Set project-wide defaults via the attribute:

### Standalone

<!-- pseudo:strict-mode-attribute-standalone -->
```csharp
// All instances default to strict
[KnockOff(Strict = true)]
public partial class StrictUserServiceKnockOff : IUserService { }

// Override per-instance if needed
var stub = new StrictUserServiceKnockOff();
stub.Strict = false;  // This instance is lenient
```
<!-- /snippet -->

### Inline

<!-- pseudo:strict-mode-attribute-inline -->
```csharp
[KnockOff<IUserService>(Strict = true)]
public partial class MyTests { }

// Default is strict, but can override:
var lenient = new Stubs.IUserService(strict: false);
```
<!-- /snippet -->

## What Throws in Strict Mode

| Member Type | Throws When |
|-------------|-------------|
| Methods | No `OnCall` callback configured |
| Properties (get) | No `OnGet` callback configured |
| Properties (set) | No `OnSet` callback configured |
| Indexers | No `OnGet`/`OnSet` configured |
| Void methods | No `OnCall` callback configured |

**Note:** Init-only properties don't throw in strict mode (they're set via object initializers).

## When to Use Strict Mode

| Scenario | Recommendation |
|----------|----------------|
| Testing specific interactions | Strict - catches unexpected calls |
| Complex integrations | Strict - ensures all interactions are explicit |
| Quick prototyping | Lenient - smart defaults reduce setup |
| Legacy code testing | Lenient - may have many incidental calls |

## Moq Migration

Moq's `MockBehavior.Strict` maps directly to KnockOff's strict mode:

<!-- pseudo:strict-mode-moq-migration -->
```csharp
// Moq equivalent:
// var mock = new Mock<IUserService>(MockBehavior.Strict);
// mock.Setup(x => x.GetUser(1)).Returns(new User());

// KnockOff - standalone
var stub = new UserServiceKnockOff().Strict();
stub.GetUser.OnCall = (ko, id) => new User();

// KnockOff - inline
var stub = new Stubs.IUserService().Strict();
stub.GetUser.OnCall = (ko, id) => new User();
```
<!-- /snippet -->

See [Moq Migration](moq-migration.md) for complete migration patterns.
