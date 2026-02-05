# Stand-Alone Class Stubs

**Status:** In Progress
**Priority:** Medium
**Created:** 2026-02-03
**Last Updated:** 2026-02-04

---

## Problem

Currently, stand-alone stubs only support interfaces:

```csharp
[KnockOff]
public partial class RepoStub : IRepository { }
```

Users cannot create stand-alone stubs for classes with virtual/abstract methods. The inline pattern `[KnockOff<ConcreteClass>]` generates a nested stub, which doesn't allow user-defined methods or custom constructors.

## Solution

Introduce two new patterns for stand-alone class stubs:

1. **Pattern 8: Standalone Class** - `[KnockOffBase<Foo>]` for closed generic or non-generic classes
2. **Pattern 9: Generic Standalone Class** - `[KnockOffBase(typeof(Foo<>))]` for open generic classes

These patterns follow the inline class stub architecture (wrapper + Impl separation) but in a standalone file context, enabling user-defined methods and custom constructors.

---

## Plans

- [Standalone Class Stubs Design](../plans/standalone-class-stubs-design.md)

---

## Tasks

- [x] Analyze attribute syntax options and conflicts with existing patterns
- [x] Analyze member selection rules (virtual, abstract, properties, etc.)
- [x] Analyze interceptor naming to avoid collisions
- [x] Analyze constructor parameter handling
- [x] Analyze base call behavior options
- [x] Determine diagnostic requirements (sealed class, etc.)
- [ ] Create design plan
- [ ] Implement KnockOffBaseAttribute and KnockOffBaseAttribute<T>
- [ ] Add predicate and pipeline for [KnockOffBase] detection
- [ ] Create StandaloneClassModelBuilder
- [ ] Create StandaloneClassRenderer
- [ ] Add tests for Pattern 8 and Pattern 9
- [ ] Update documentation (Design.Stubs, API reference, patterns guide)

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

### 2026-02-04 - Design Decisions Made

User decisions for the feature:

1. **Syntax**: New attributes `[KnockOffBase<T>]` and `[KnockOffBase(typeof(T<>))]`
2. **Interfaces**: Users CAN add interfaces to their standalone class stub
3. **Member selection**: Match inline class behavior (abstract=default/strict, virtual=base fallback)
4. **Interceptor naming**: Clean names - use wrapper/base class separation like inline class stubs
5. **Constructors**: Forward all accessible constructors to base
6. **`.Object` property**: Include for API consistency (returns `this`)
7. **Source() delegation**: Omit (match inline class behavior)
8. **Generic support**: Yes - `[KnockOffBase(typeof(T<>))]` for generic standalone class stubs

### 2026-02-04 - Design Revised (Architect)

Developer review identified CRITICAL issues with initial design:

1. **Name Collision**: Original design had interceptor properties and overrides in same class - C# compilation error
2. **Private Field Access**: `private bool _strict` inaccessible from derived class
3. **Inherited Virtual Members**: Missing guidance on base class virtual members

**Solution**: Inverted wrapper/Impl pattern:
- User's partial class = wrapper (holds interceptors with clean names)
- Generated `*Impl` base class = inherits from target, delegates UP to wrapper
- No name collision because interceptors and overrides on different types

This follows the inline class stub pattern but inverted (wrapper above Impl, not below).

---

### 2026-02-04 - Developer Re-Review: Concerns Raised

Developer re-reviewed the revised plan. While 3 of 4 original concerns were addressed (protected fields, diagnostic numbering, inherited members), the **name collision issue is NOT resolved**.

**The Problem:**
When `ServiceStub` inherits from `ServiceStubImpl`, it inherits `override string Name`. Then the generated partial adds `ServiceStub_NameInterceptor Name { get; }`. These are different types with the same property name - C# compilation error.

**Inline stubs avoid this** because Impl is NESTED inside the wrapper, not inherited. For standalone, the inverted architecture (wrapper inherits from Impl) causes collisions.

**Question for Architect:** How to give interceptor properties clean names when the wrapper inherits override properties with those same names?

Sent back to architect for resolution.

---

## Results / Conclusions

*(Awaiting architect response to name collision concern)*
