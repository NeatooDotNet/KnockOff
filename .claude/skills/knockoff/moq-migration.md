# Moq Migration

Step-by-step patterns for migrating from Moq to KnockOff.

## Quick Reference

| Moq | KnockOff |
|-----|----------|
| `new Mock<IService>()` | `new ServiceKnockOff()` |
| `mock.Object` | `IService svc = stub;` (implicit) or `stub.Object` (class stubs) |
| `.Setup(x => x.Prop).Returns(v)` | `stub.Prop.Value = v` (static) or `stub.Prop.OnGet = ...` (dynamic) |
| `.Setup(x => x.M())` | `stub.M.OnCall = (ko, ...) => ...` |
| `.Returns(v)` | `OnCall = (ko) => v` |
| `.ReturnsAsync(v)` | `OnCall = (ko) => Task.FromResult(v)` |
| `.Callback(a)` | Logic inside `OnCall` callback |
| `.Verify(Times.Once)` | `Assert.Equal(1, IService.M.CallCount)` |
| `It.IsAny<T>()` | Implicit |
| `It.Is<T>(p)` | Check in callback |

## Migration Steps

### Step 1: Create KnockOff Class

snippet: skill-moq-migration-step1-create

### Step 2: Replace mock.Object

**Interface stubs** use implicit conversion (they implement the interface directly):

snippet: skill-moq-migration-step2-object

**Class stubs** use `.Object` property (similar to Moq):

snippet: skill-moq-migration-class-stub-object-usage

**Interface access** works via implicit conversion:

snippet: skill-moq-migration-interface-access-usage

### Step 3: Convert Setup/Returns

snippet: skill-moq-migration-step3-setup

### Step 4: Convert ReturnsAsync

snippet: skill-moq-migration-step4-async

### Step 5: Convert Verify

snippet: skill-moq-migration-step5-verify

### Step 6: Convert Callback

snippet: skill-moq-migration-step6-callback

## Common Patterns

### Static Returns

snippet: skill-moq-migration-static-returns

### Conditional Returns

snippet: skill-moq-migration-conditional-returns

### Throwing Exceptions

snippet: skill-moq-migration-throwing-exceptions

### Sequential Returns

snippet: skill-moq-migration-sequential-returns

### Property Setup

snippet: skill-moq-migration-setup-property

**Use `Value` for static values (recommended):**
```csharp
// Moq
mock.Setup(x => x.Name).Returns("Test");

// KnockOff - use Value for static data
knockOff.Name.Value = "Test";
```

**Use `OnGet` for dynamic values:**
snippet: skill-moq-migration-property-setup

**Setter tracking is automatic:**
```csharp
service.Name = "NewValue";
Assert.Equal("NewValue", knockOff.Name.LastSetValue);
Assert.Equal(1, knockOff.Name.SetCount);
```

### SetupProperty (Tracked Properties)

Moq's `SetupProperty` creates a property that tracks get/set values. KnockOff interceptor's `Value` property provides the same behavior:

```csharp
// Moq - property with get/set tracking
mock.SetupProperty(x => x.Active, true);  // Initial value
service.Active = false;                    // Tracks the set
Assert.False(service.Active);              // Returns last set value

// KnockOff - interceptor.Value works identically
knockOff.Active.Value = true;             // Initial value
service.Active = false;                    // Sets interceptor.Value
Assert.False(service.Active);              // Returns interceptor.Value
Assert.Equal(1, knockOff.Active.SetCount); // Tracking works
```

**Naming Convention:** For every property `Foo` on the interface, access its backing storage via `knockOff.Foo.Value`:

```csharp
knockOff.NewDate.Value = DateTime.Today;    // DateTime
knockOff.VisitId.Value = 42L;               // long
knockOff.VisitLabel.Value = "Test";         // string
knockOff.PreviousVisitDate.Value = null;    // Nullable works identically
```

### Multiple Interfaces

snippet: skill-moq-migration-multiple-interfaces

### Interface Inheritance

KnockOff automatically implements ALL inherited interface members:

```csharp
// Moq - inherits base interface automatically
var mock = new Mock<IEmployeeService>();  // IEmployeeService : IEntityBase
mock.Setup(x => x.Id).Returns(42);        // Base interface member
mock.Setup(x => x.Name).Returns("John");  // Derived interface member
```

KnockOff - same behavior, all members accessible directly:

snippet: skill-moq-migration-interface-inheritance-callbacks

The `Value` property works for all inherited members via the interceptor:
```csharp
knockOff.Id.Value = 42;                   // Sets base interface property
knockOff.Name.Value = "John";             // Sets derived interface property
knockOff.Department.Value = "Engineering";
```

This works regardless of inheritance depth or whether the base interface comes from external packages (e.g., Neatoo's `IEntityBase`).

### Argument Matching

snippet: skill-moq-migration-argument-matching

### Method Overloads

snippet: skill-moq-migration-method-overloads

### Out Parameters

snippet: skill-moq-migration-out-params

### Ref Parameters

snippet: skill-moq-migration-ref-params

### Strict Mode (MockBehavior.Strict)

Moq's strict mode throws when unconfigured methods are called:

<!-- pseudo:moq-migration-strict-moq -->
```csharp
// Moq - strict mode via constructor
var mock = new Mock<IUserService>(MockBehavior.Strict);
mock.Setup(x => x.GetUser(1)).Returns(new User());

// Unconfigured call throws MockException
mock.Object.GetUser(2);  // Throws!
```
<!-- /snippet -->

KnockOff supports the same behavior:

<!-- pseudo:moq-migration-strict-knockoff -->
```csharp
// Extension method (recommended) - works with any stub type
var stub = new UserServiceKnockOff().Strict();
var stub = new Stubs.IUserService().Strict();

// Property setter
var stub = new UserServiceKnockOff();
stub.Strict = true;

// Constructor parameter (inline stubs only)
var stub = new Stubs.IUserService(strict: true);

// Attribute default
[KnockOff(Strict = true)]
public partial class UserServiceKnockOff : IUserService { }
```
<!-- /snippet -->

Unconfigured calls throw `StubException`:

<!-- pseudo:moq-migration-strict-throws -->
```csharp
var stub = new UserServiceKnockOff().Strict();
// stub.GetUser.OnCall not set
stub.Object.GetUser(1);  // Throws StubException!
```
<!-- /snippet -->

## Features Comparison

| Feature | Moq | KnockOff |
|---------|-----|----------|
| Runtime config | Supported | Supported (callbacks) |
| Compile-time config | Not supported | Supported (user methods) |
| Type-safe setup | Expression-based | Strongly-typed delegates |
| Argument capture | Via Callback | Automatic tracking |
| Call counting | Verify(Times.X) | CallCount property |
| Out parameters | Supported | Supported |
| Ref parameters | Supported | Supported |
| Events | Supported | Supported (with Raise/tracking) |
| Generic methods | Supported | Supported (via `.Of<T>()` pattern) |
| Strict mode | Supported | Supported |
| VerifyNoOtherCalls | Supported | Not supported |

## Keep Using Moq For

- `VerifyNoOtherCalls` verification

## Gradual Migration

Use both in same project:

```csharp
// New tests: KnockOff
var userKnockOff = new UserServiceKnockOff();

// Legacy tests: Keep Moq until migrated
var orderMock = new Mock<IOrderService>();
```
