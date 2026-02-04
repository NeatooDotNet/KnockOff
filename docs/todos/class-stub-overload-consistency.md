# Class Stub Overload API Consistency

**Status:** Open
**Priority:** Medium
**Created:** 2026-02-03
**Last Updated:** 2026-02-03

---

## Problem

Class-based stubs generate numbered interceptors for overloaded methods (e.g., `GetDefault1`, `GetDefault2`) instead of a single interceptor with multiple `OnCall` overloads like interface stubs.

This creates an API inconsistency between stub patterns:

```csharp
// Interface stub - single interceptor, C# overload resolution
stub.Format.OnCall((item) => ...);                    // 1 param
stub.Format.OnCall((item, uppercase) => ...);         // 2 params

// Class stub - numbered interceptors (INCONSISTENT)
stub.GetDefault1.OnCall(() => ...);                   // no params
stub.GetDefault2.OnCall((filter) => ...);             // 1 param
```

**Affected patterns:**
- Inline Class (`[KnockOff<ConcreteClass>]`)
- Open Generic Class (`[KnockOff(typeof(RepositoryBase<>))]`)

**Patterns that work correctly:**
- Standalone (interface)
- Generic Standalone (interface)
- Inline Interface
- Open Generic Interface

## Solution

Update the generator to produce a single interceptor with multiple `OnCall` overloads for class stubs, matching the interface stub pattern.

The class stub for `RepositoryBase<T>` with overloaded `GetDefault()` methods should generate:

```csharp
// Expected: single interceptor with overloaded OnCall
stub.GetDefault.OnCall(() => ...);                    // T? GetDefault()
stub.GetDefault.OnCall((filter) => ...);              // T? GetDefault(string filter)
```

---

## Plans

---

## Tasks

- [ ] Investigate generator code that handles class stub method generation
- [ ] Identify why class stubs use numbered interceptors while interfaces use overloads
- [ ] Design fix to unify the overload API across all patterns
- [ ] Update generator to produce single interceptor with `OnCall` overloads for class stubs
- [ ] Update existing tests to use new API
- [ ] Add explicit tests for class stub overload consistency

---

## Progress Log

### 2026-02-03: Issue discovered during open generic investigation

While testing how the overload API works with open generic patterns, discovered that:

1. `[KnockOff(typeof(IGenericFormatter<>))]` (interface) generates single `Format` interceptor with multiple `OnCall` overloads
2. `[KnockOff(typeof(RepositoryBase<>))]` (class) generates `GetDefault1` and `GetDefault2` separate interceptors

This is the first time class stubs with overloaded methods have been tested - existing inline class example (`ServiceBase`) has no overloaded methods.

**Files created during investigation:**
- `src/Design/Design.Domain/Abstractions/RepositoryBase.cs` - generic abstract class with overloaded `GetDefault()` methods
- `src/Design/Design.Tests/GenericOverloadTests/OpenGenericOverloadTests.cs` - tests demonstrating the issue

### 2026-02-03: Confirmed inline class pattern also affected

Tested `[KnockOff<RepositoryBase<TestEntity>>]` (inline class with closed generic) and confirmed it also generates numbered interceptors (`GetDefault1Interceptor`, `GetDefault2Interceptor`).

**Conclusion:** Both class-based patterns (inline class and open generic class) are affected. The issue is in the generator's class stub handling, not specific to open generics.

---

## Results / Conclusions
