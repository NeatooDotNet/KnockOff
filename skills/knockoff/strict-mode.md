---
skill: knockoff
topic: strict-mode
audience: intermediate
---

# Strict Mode

Strict mode makes stubs throw `StubException` when unconfigured members are accessed.

## Quick Start

Enable strict mode with the `.Strict()` extension method:

```csharp
var knockOff = new UserServiceStub().Strict();
IUserService service = knockOff;

// Throws StubException - Method not configured
service.GetUser(42);  // StubException: Method GetUser was called but not configured
```

## When to Use Strict Mode

**Use strict mode when:**
- ✅ You want explicit failures for unconfigured methods
- ✅ Testing that only expected methods are called
- ✅ Catching accidental usage of stubbed methods

**Don't use strict mode when:**
- ❌ You want smart defaults (empty lists, null, etc.)
- ❌ Testing happy path with minimal setup
- ❌ Many methods need default behavior

## Enabling Strict Mode

### Option 1: Extension Method (Recommended)

```csharp
var knockOff = new UserServiceStub().Strict();
```

**Benefits:**
- Works with any stub (standalone or inline)
- Clear and explicit
- Can be toggled per-test

### Option 2: Attribute (Standalone Stubs)

```csharp
[KnockOff(Strict = true)]
public partial class StrictUserServiceStub : IUserService { }
```

**Benefits:**
- Strict by default for all uses
- No need to call `.Strict()` in tests

### Option 3: Constructor Parameter (Inline Stubs)

```csharp
[KnockOff<IUserService>]
public partial class Tests
{
    [Fact]
    public void Test()
    {
        var knockOff = new Stubs.IUserService(strict: true);
    }
}
```

## Behavior

### Unconfigured Methods

```csharp
var knockOff = new UserServiceStub().Strict();

// Throws: Method GetUser not configured
service.GetUser(42);
```

**Fix:** Configure with OnCall or user method:

```csharp
knockOff.GetUser.OnCall((ko, id) => new User { Id = id });
service.GetUser(42);  // OK
```

### Unconfigured Properties

```csharp
var knockOff = new UserServiceStub().Strict();

// Throws: Property Name getter not configured
var name = service.Name;
```

**Fix:** Set Value or OnGet:

```csharp
knockOff.Name.Value = "Test";
var name = service.Name;  // OK
```

### Configured Members Work Normally

```csharp
var knockOff = new UserServiceStub().Strict();

knockOff.GetUser.OnCall((ko, id) => new User { Id = id });
knockOff.Name.Value = "Test";

service.GetUser(42);  // ✅ OK - configured
service.Name;         // ✅ OK - configured
service.Delete(1);    // ❌ Throws - not configured
```

## User Methods in Strict Mode

User methods count as "configured":

```csharp
[KnockOff(Strict = true)]
public partial class StrictUserServiceStub : IUserService
{
    protected User? GetUser(int id) => new User { Id = id };
}

var knockOff = new StrictUserServiceStub();
service.GetUser(42);  // ✅ OK - user method exists
service.Delete(1);    // ❌ Throws - no user method or callback
```

## StubException

When strict mode throws, the exception message includes:
- Member name
- Member type (method, property getter/setter)
- Guidance on how to fix

```
StubException: Method 'GetUser' on interface 'IUserService' was called but not configured.
Configure with: knockOff.GetUser.OnCall = (ko, id) => ...
Or define user method: protected User? GetUser(int id) => ...
```

## Strict Mode vs Moq Strict

| Aspect | Moq Strict | KnockOff Strict |
|--------|------------|-----------------|
| **Default** | `MockBehavior.Strict` | Extension method `.Strict()` |
| **Unconfigured** | Throws `MockException` | Throws `StubException` |
| **Verification** | `.Verify()` required | Automatic tracking |
| **Setup** | `Setup()` for all | OnCall or user method |

**Key difference:** KnockOff strict mode is opt-in per stub, Moq strict is global for the mock.

## Examples

### Example 1: Testing Specific Call

```csharp
[Fact]
public void Should_Only_Call_GetUser()
{
    var knockOff = new UserServiceStub().Strict();
    knockOff.GetUser.OnCall((ko, id) => new User { Id = id });

    var service = new MyService(knockOff);
    service.LoadUser(42);

    // If LoadUser calls Delete(), test fails with StubException
    Assert.Equal(1, knockOff.GetUser.CallCount);
}
```

### Example 2: Mixed Mode

```csharp
// Some stubs strict, others not
var userService = new UserServiceStub().Strict();  // Strict
var logger = new LoggerStub();  // Not strict

userService.GetUser.OnCall((ko, id) => new User { Id = id });
// logger methods can be called without configuration
```

### Example 3: Attribute Default

```csharp
[KnockOff(Strict = true)]
public partial class StrictRepoStub : IRepository
{
    // All uses of this stub are strict by default
    protected User? GetById(int id) => new User { Id = id };
}

// In test
var knockOff = new StrictRepoStub();  // Already strict
service.GetById(42);  // OK - user method
service.Save(user);   // Throws - not configured
```

## Toggling Strict Mode

Strict mode cannot be disabled once enabled (immutable):

```csharp
var knockOff = new UserServiceStub().Strict();
// No .NotStrict() method - strict is permanent for this instance
```

**Workaround:** Create a new instance without `.Strict()`.

## When Strict Mode Helps

### Catch Unexpected Calls

```csharp
[Fact]
public void Should_Not_Call_Delete_On_ReadOnly_Operation()
{
    var knockOff = new UserServiceStub().Strict();
    knockOff.GetUser.OnCall((ko, id) => new User { Id = id });

    var service = new MyService(knockOff);
    service.DisplayUser(42);  // Read-only operation

    // If DisplayUser accidentally calls Delete(), test fails
}
```

### Explicit Dependencies

```csharp
[Fact]
public void Test_With_Explicit_Dependencies()
{
    // Strict mode forces you to configure what you need
    var knockOff = new UserServiceStub().Strict();
    knockOff.GetUser.OnCall((ko, id) => new User { Id = id });
    knockOff.IsConnected.Value = true;

    // Clear which methods the test depends on
}
```

## Summary

**Quick reference:**

| Need | Use |
|------|-----|
| Enable strict | `.Strict()` extension |
| Standalone default | `[KnockOff(Strict = true)]` |
| Inline default | `new Stubs.IService(strict: true)` |
| User methods | Count as configured |
| Error type | `StubException` |

**Next:**
- [creating-stubs.md](creating-stubs.md) - Stub patterns
- [troubleshooting.md](troubleshooting.md) - Debugging
