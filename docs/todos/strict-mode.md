# Strict Mode Feature

## Problem

KnockOff stubs currently return `default` when no `OnCall` callback is configured. This is "lenient" behavior - tests pass silently even when methods are called unexpectedly.

Some users prefer "strict" behavior where unconfigured method calls throw an exception, catching unexpected interactions.

## Proposed Solution

Add a `Strict` option that throws `InvalidOperationException` when a method is called without an `OnCall` configured.

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
    throw new InvalidOperationException("No implementation provided for IUserService.GetUser");
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
        if (_strict) throw new InvalidOperationException("No implementation provided for IUserService.GetUser");
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

- [ ] Add `strict` constructor parameter to generated stub classes
- [ ] Add `_strict` backing field
- [ ] Update method implementations to check `_strict` before returning default
- [ ] Update property getter implementations similarly
- [ ] Add tests for strict mode behavior
- [ ] Update documentation with strict mode usage
- [ ] Update README: remove "strict mode" from Limitations vs Moq section
- [ ] Consider: Should `void` methods also throw in strict mode?

## Open Questions

1. **Exception type**: `InvalidOperationException` or custom `StubNotConfiguredException`?
2. **Void methods**: Should they throw too, or only methods with return values?
3. **Properties**: Apply to getters? Setters? Both?
4. **Message format**: `"No implementation provided for {Interface}.{Member}"` or simpler?

## Related

- Moq's `MockBehavior.Strict` for comparison
- Current default behavior in generated code (returns `default!`)
