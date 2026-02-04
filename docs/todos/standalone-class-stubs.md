# Stand-Alone Class Stubs

**Status:** Analysis Needed
**Priority:** Medium
**Created:** 2026-02-03
**Last Updated:** 2026-02-03

---

## Problem

Currently, stand-alone stubs only support interfaces:

```csharp
[KnockOff]
public partial class RepoStub : IRepository { }
```

Users cannot create stand-alone stubs for classes with virtual/abstract methods. The inline pattern `[KnockOff<ConcreteClass>]` generates a nested stub, which doesn't allow user-defined methods or custom constructors.

## Proposed Feature

Allow stand-alone stubs to target classes with virtual methods using the attribute syntax:

```csharp
public abstract class TestSubject
{
    public virtual int Calculate(int x) => x * 2;
    public abstract string GetName();
}

[KnockOff<TestSubject>()]
public partial class TestSubjectStub
{
    // User can add custom constructors, fields, user methods, etc.
}
```

### Generated Code (Conceptual)

```csharp
// Generated base class inherits from target class
public class TestSubjectStubBase : TestSubject
{
    // Override virtual methods to add interception
    public override int Calculate(int x)
    {
        Calculate_.RecordCall(x);
        if (Calculate_.Callback is { } callback) return callback(x);
        // Call base implementation as fallback
        return base.Calculate(x);
    }

    // Abstract methods must be implemented
    public override string GetName()
    {
        GetName_.RecordCall();
        if (GetName_.Callback is { } callback) return callback();
        if (Strict) throw StubException.NotConfigured(...);
        return default!;
    }
}

// User's partial class inherits from generated base
public partial class TestSubjectStub : TestSubjectStubBase, IKnockOffStub
{
    public bool Strict { get; set; }

    // Clean interceptor names (no collision with base class methods)
    public CalculateInterceptor Calculate_ { get; } = new();
    public GetNameInterceptor GetName_ { get; } = new();
}
```

### Key Differences from Interface Stand-Alone

| Aspect | Interface Stand-Alone | Class Stand-Alone (Proposed) |
|--------|----------------------|------------------------------|
| Attribute | `[KnockOff]` | `[KnockOff<TargetClass>()]` |
| Inheritance | Stub → Base → Interface | Stub → Base → TargetClass |
| Members stubbed | All interface members | Virtual/abstract members only |
| Base call | N/A | `base.Method()` available as fallback |
| Interceptor naming | `Method` (clean) | `Method_` (underscore suffix to avoid collision) |

---

## Analysis Questions

### 1. Attribute Syntax
- Use `[KnockOff<TestSubject>()]` to distinguish from interface stand-alone?
- Or detect automatically based on whether target is class vs interface?
- How does this interact with existing inline class pattern `[KnockOff<ConcreteClass>]`?

### 2. Member Selection
- Only virtual/abstract methods?
- Virtual properties?
- What about `new` methods that hide base methods?
- Non-virtual methods - ignore or error?

### 3. Interceptor Naming
- Use `Method_` suffix to avoid collision with inherited `Method`?
- Or use different naming convention?
- How does this interact with user method overrides (which also use `_` suffix)?

### 4. Constructor Handling
- Target class may have required constructor parameters
- Generated base class needs to call base constructor
- How does user provide constructor arguments?

### 5. Base Call Behavior
- Virtual methods: call `base.Method()` as fallback (like source delegation)?
- Abstract methods: no base call possible, use Strict/default pattern?
- Should base call be configurable?

### 6. Sealed Classes
- Sealed classes cannot be inherited - emit diagnostic?
- Sealed methods in non-sealed classes - skip or error?

### 7. Pattern Conflicts
- What if class also implements interfaces?
- Should both class members AND interface members be stubbed?
- How to handle diamond inheritance scenarios?

---

## Plans

*(None yet - analysis phase)*

---

## Tasks

- [ ] Analyze attribute syntax options and conflicts with existing patterns
- [ ] Analyze member selection rules (virtual, abstract, properties, etc.)
- [ ] Analyze interceptor naming to avoid collisions
- [ ] Analyze constructor parameter handling
- [ ] Analyze base call behavior options
- [ ] Determine diagnostic requirements (sealed class, etc.)
- [ ] Prototype feasibility in generator
- [ ] Create design plan if analysis is favorable

---

## Progress Log

### 2026-02-03 - Todo Created

Initial analysis todo created based on user request. Key insight from user:

> The stub base class could inherit from the class with virtual methods. Like inline stubs, the class with virtual methods would be defined in the KnockOff attribute.

This would enable stand-alone stubs for classes, giving users the ability to:
- Add custom constructors
- Add user-defined override methods
- Have full control over stub instantiation
- Use the same OnCall/Returns/Verify API

---

## Results / Conclusions

*(Pending analysis)*
