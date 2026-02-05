# Stand-Alone Class Stubs

**Status:** Complete
**Priority:** Medium
**Created:** 2026-02-03
**Completed:** 2026-02-04

---

## Problem

Currently, stand-alone stubs only support interfaces:

```csharp
[KnockOff]
public partial class RepoStub : IRepository { }
```

Users cannot create stand-alone stubs for classes with virtual/abstract methods. The inline pattern `[KnockOff<ConcreteClass>]` generates a nested stub, which doesn't allow user-defined methods or custom constructors.

## Solution

Introduced two new patterns for stand-alone class stubs:

1. **Pattern 3: Standalone Class** - `[KnockOffBase<Foo>]` for closed generic or non-generic classes
2. **Pattern 4: Generic Standalone Class** - `[KnockOffBase(typeof(Foo<>))]` for open generic classes

These patterns follow the inline class stub architecture (wrapper + nested Impl) enabling user-defined methods and custom constructors.

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
- [x] Create design plan
- [x] Implement KnockOffBaseAttribute and KnockOffBaseAttribute<T>
- [x] Add predicate and pipeline for [KnockOffBase] detection
- [x] Create StandaloneClassModelBuilder
- [x] Create StandaloneClassRenderer
- [x] Add tests for Pattern 3 and Pattern 4
- [x] Update documentation (Design.Stubs, API reference, patterns guide)

---

## Progress Log

### 2026-02-03 - Todo Created

Initial analysis todo created based on user request.

### 2026-02-04 - Design Decisions Made

User decisions for the feature:

1. **Syntax**: New attributes `[KnockOffBase<T>]` and `[KnockOffBase(typeof(T<>))]`
2. **Interfaces**: Users CAN add interfaces to their standalone class stub
3. **Member selection**: Match inline class behavior (abstract=default/strict, virtual=base fallback)
4. **Interceptor naming**: Clean names - use composition pattern like inline class stubs
5. **Constructors**: Forward all accessible constructors to base
6. **`.Object` property**: Required - returns nested Impl instance as target type
7. **Generic support**: Yes - `[KnockOffBase(typeof(T<>))]` for generic standalone class stubs

### 2026-02-04 - Design Revised (Architect)

Developer review identified critical name collision issue. Solution: **Composition pattern** (same as inline class stubs):
- User's partial class = wrapper (holds interceptors with clean names)
- Generated nested `Impl` class = inherits from target, delegates to wrapper
- No inheritance between wrapper and Impl = no name collision

### 2026-02-04 - Implementation Complete

All phases implemented:
- Phase 1: `KnockOffBaseAttribute<T>` and `KnockOffBaseAttribute` created
- Phase 2: Pipeline 4 and 5 added to generator
- Phase 3: `StandaloneClassGenerationUnit` and `StandaloneClassModelBuilder` created
- Phase 4: `StandaloneClassRenderer` created
- Phase 5: 52 tests added (StandaloneClassStubTests + GenericStandaloneClassStubTests)
- Phase 6: Documentation updated (AllPatterns.cs, stub-patterns.md, patterns.md, CLAUDE.md)

### 2026-02-04 - Additional Tests Added

Architect review identified minor test gaps. Added 7 tests for:
- Stub-level Verify/VerifyAll
- Sequence behavior (ThenCall, repeats last)
- Verifiable() marking

---

## Results / Conclusions

Feature complete. KnockOff now has 9 patterns instead of 7:

| # | Pattern | Attribute |
|---|---------|-----------|
| 1 | Standalone | `[KnockOff]` on class : interface |
| 2 | Generic Standalone | `[KnockOff]` on class<T> : interface<T> |
| 3 | **Standalone Class** | `[KnockOffBase<T>]` |
| 4 | **Generic Standalone Class** | `[KnockOffBase(typeof(T<>))]` |
| 5 | Inline Interface | `[KnockOff<IInterface>]` |
| 6 | Inline Class | `[KnockOff<ConcreteClass>]` |
| 7 | Inline Delegate | `[KnockOff<DelegateType>]` |
| 8 | Open Generic Interface | `[KnockOff(typeof(IFoo<>))]` |
| 9 | Open Generic Class | `[KnockOff(typeof(Foo<>))]` |

All tests pass (1090+ across net8.0/net9.0/net10.0).
