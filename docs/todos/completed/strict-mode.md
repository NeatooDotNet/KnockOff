# Strict Mode Feature

## Problem

KnockOff stubs currently return `default` when no `OnCall` callback is configured. This is "lenient" behavior - tests pass silently even when methods are called unexpectedly.

Some users prefer "strict" behavior where unconfigured method calls throw an exception, catching unexpected interactions.

## Proposed Solution

Add a `Strict` option that throws `StubException` when a method is called without an `OnCall` configured.

### Implementation Options

#### Option 1: Attribute Only (Compile-Time)

```csharp
[KnockOff(Strict = true)]
public partial class UserServiceStub : IUserService { }

[KnockOff<IUserService>(Strict = true)]
public partial class MyTests { }
```

**Generated code:**
```csharp
User IUserService.GetUser(int id)
{
    GetUser.RecordCall(id);
    if (GetUser.OnCall is { } onCall) return onCall(this, id);
    throw StubException.NotConfigured("IUserService", "GetUser");
}
```

**Pros:**
- No runtime overhead (no field, no check)
- Clear intent at declaration site

**Cons:**
- Can't mix strict/lenient per test
- Requires separate stub class for strict vs lenient

#### Option 2: Constructor Parameter (Runtime)

```csharp
var stub = new Stubs.IUserService(strict: true);
var lenientStub = new Stubs.IUserService(); // defaults to false
```

**Generated code:**
```csharp
public class IUserService : global::IUserService
{
    private readonly bool _strict;

    public IUserService(bool strict = false)
    {
        _strict = strict;
    }

    User global::IUserService.GetUser(int id)
    {
        GetUser.RecordCall(id);
        if (GetUser.OnCall is { } onCall) return onCall(this, id);
        if (_strict) throw StubException.NotConfigured("IUserService", "GetUser");
        return default!;
    }
}
```

**Pros:**
- Flexible per-test decision
- Same stub class works for both modes

**Cons:**
- Minor runtime overhead (field + branch)
- Slightly more generated code

#### Option 3: Both

Support both attribute-level default and constructor override:

```csharp
[KnockOff(Strict = true)]  // Default to strict
public partial class UserServiceStub : IUserService { }

// In tests:
var strict = new UserServiceStub();           // strict (from attribute)
var lenient = new UserServiceStub(strict: false);  // override to lenient
```

**Pros:**
- Maximum flexibility
- Teams can set project-wide defaults via attribute

**Cons:**
- Most complex implementation
- Two ways to configure same thing

## Recommendation

**Option 3 (Both)** - they're the same implementation.

The attribute just sets the constructor's default value:
```csharp
// [KnockOff] or [KnockOff(Strict = false)]
public IUserService(bool strict = false)

// [KnockOff(Strict = true)]
public IUserService(bool strict = true)
```

Same field, same runtime check. Attribute-level is free once constructor exists.

## Implementation Tasks

- [x] Create `KnockOff.StubException` class
- [x] Add `Strict` property to `KnockOffAttribute` and `KnockOffAttribute<T>`
- [x] Generate `Strict` property for standalone stubs (public, settable, uses attribute default)
- [x] Generate `Strict` property + constructor parameter for inline stubs
- [x] Update method implementations to check strict before returning default
- [x] Update property getter/setter implementations similarly
- [x] Update void method implementations to throw in strict mode
- [x] Add tests for strict mode behavior (17 tests in StrictModeTests.cs)
- [x] Update documentation with strict mode usage
- [x] Update knockoff skill (SKILL.md) with strict mode section
- [x] Update moq-migration.md skill with MockBehavior.Strict mapping
- [x] Update README: remove "strict mode" from Limitations vs Moq section

### v10.17.0 - Extension Method Enhancement

- [x] Create `IKnockOffStub` interface with `Strict` property
- [x] Create `StubExtensions.Strict<T>()` extension method for fluent API
- [x] Update generator to implement `IKnockOffStub` on all stub types (standalone, inline interface, inline class, inline delegate)
- [x] Change inline stubs from readonly `_strict` field to settable `Strict` property (for extension method compatibility)
- [x] Add `Strict` property to class and delegate stubs (no-op for now, enables future strict mode support)
- [x] Add 7 tests for extension method and interface implementation
- [x] Update documentation with `.Strict()` extension method usage

## Implementation Notes

**All stubs** implement `IKnockOffStub` interface and have a settable `Strict` property:

```csharp
// Standalone stub
var stub = new UserServiceStub().Strict(); // fluent API
// or
var stub = new UserServiceStub { Strict = true }; // property initializer

// Inline stub - constructor parameter (backwards compatible) or extension method
var stub = new Stubs.IUserService(strict: true); // constructor
var stub = new Stubs.IUserService().Strict(); // extension method
```

The `IKnockOffStub` interface enables the `.Strict()` extension method to work with any KnockOff stub type.

## Resolved Decisions

Following Moq's `MockBehavior.Strict` patterns:

1. **Exception type**: `KnockOff.StubException` with factory method `NotConfigured(interfaceName, memberName)`
2. **Void methods**: Yes, throw if no `OnCall` configured
3. **Properties**: Both getters and setters throw if not configured
4. **Message format**: `"{Interface}.{Member} invocation failed with strict behavior. Configure OnCall before invoking."`

## Related

- Moq's `MockBehavior.Strict` for comparison
- Current default behavior in generated code (returns `default!`)
