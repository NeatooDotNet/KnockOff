# Base Class Approach for User Methods

**Status:** In Progress
**Priority:** High
**Created:** 2026-02-02
**Last Updated:** 2026-02-02

---

## Problem

The current user methods feature has two significant issues:

1. **The '2' postfix is ugly**: When a user defines a protected method like `GetUserById(int id)`, the tracker property becomes `GetUserById2` because the name `GetUserById` is reserved by the user's method. This naming is confusing and aesthetically poor.

2. **Signature changes are silent**: If the interface method signature changes (e.g., `GetById(int id)` → `GetById(string id)`), the user's protected method no longer matches. The generator silently stops recognizing it as a user method, creates a regular interceptor, and the orphaned protected method is never called. **There is no compile-time error.**

## Solution

Generate a base class with virtual protected methods that users must override. This provides:

1. **Clean tracker names**: Trackers use the method name directly (`stub.GetById`), no '2' suffix needed
2. **Compile-time signature enforcement**: If user's override doesn't match, compiler error: "no suitable method to override"
3. **IntelliSense discovery**: Users see available methods to override when typing in the stub class

### Key Insight

In C#, a **property** and a **method with parameters** can have the same name when the property is on a derived class hiding an inherited method. But actually... they can coexist naturally because they're different member kinds distinguished by call syntax:
- `GetById` (no parens) → property
- `GetById(id)` (with parens) → method

**CORRECTION**: This doesn't work. C# does not allow a property and method with the same name in the same class, even with different signatures. The solution is to suffix the base class virtual methods with `_`:

```csharp
// Generated base class
public class RepoStubBase {
    protected virtual Task<Order> GetById_(int id) { throw new NotImplementedException(); }
}

// User writes override
protected override Task<Order> GetById_(int id) => ...;

// Generated partial - clean tracker names
public GetByIdInterceptor GetById { get; }  // No suffix!
```

---

## Plans

- [Base Class User Methods Design](../plans/base-class-user-methods-design.md)

---

## Tasks

- [x] Analyze overload handling with base class approach
- [x] Analyze generic method handling with base class approach
- [x] Determine if properties should be supported (currently methods only)
- [ ] Design the generated base class structure
- [ ] Handle edge case: user already has a base class (block or error)
- [ ] Handle source generator timing for override detection
- [ ] Implementation planning

---

## Progress Log

### 2026-02-02 - Initial Exploration

Explored the base class approach through conversation. Key findings:

1. **Current implementation**: User methods are detected by matching protected method signatures to interface methods. Name collision causes '2' suffix on trackers.

2. **Base class approach viable**: Generator creates `{ClassName}Base` with virtual methods. Users write `protected override`. Signature mismatches cause compile errors.

3. **Naming conflict resolution**: Property and method can't share names in C#. Solution: suffix base class methods with `_` (e.g., `GetById_`). Tracker properties keep clean names (`GetById`).

4. **Properties not currently supported**: `GetUserDefinedMethods()` explicitly filters with `!member.IsProperty`. Only methods are user-definable today.

### 2026-02-02 - Overloads, Generics, and Properties Analysis

**Overloads:** Work naturally. Each overload becomes a separate virtual method in base class (`Format_(string)`, `Format_(string, FormatOptions)`, etc.). Users can override any subset. Non-overridden overloads use interceptor.

**Generic methods:** Recommend EXCLUDING from base class pattern. Current `.Of<T>()` pattern is already good for type-specific configuration. User override would be a single method for all type arguments, losing the per-type flexibility.

**Properties:** Currently NOT supported (code explicitly filters `!member.IsProperty`). The base class pattern could work for properties, but defer to Phase 2. Methods are higher priority and more common use case.

---

## Results / Conclusions

