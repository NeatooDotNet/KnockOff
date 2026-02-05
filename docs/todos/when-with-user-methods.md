# Enable .When() API with User Methods

**Status:** Complete
**Priority:** High
**Created:** 2026-02-04
**Last Updated:** 2026-02-05
**Completed:** 2026-02-05

---

## Problem

When a standalone stub defines user methods (the `_` suffix pattern), the `.When()` API is unavailable. This creates an API inconsistency that contradicts the api-consistency-matrix.md documentation, which claims `.When()` works for all 8 patterns.

This was discovered when trying to use `.When()` on a standalone stub with user methods:

```csharp
[KnockOff]
public partial class MyRepoStub(List<User> Users) : IMyRepo
{
    protected override User? GetUser_(int id)
    {
        return Users.Single(u => u.Id == id);
    }
}

// This fails to compile:
myRepoKO.GetUser.When(1).Returns(user1).Verifiable(Times.Twice);
// Error: cannot convert from 'int' to 'System.Action<...GetUserInterceptor>'
```

The user method provides default "hydrated" behavior (e.g., list lookup), but there's no way to override specific argument combinations for test scenarios.

## Solution

Extend the interceptor priority chain to support `.When()` even when user methods are defined. The priority would be:

1. When chains (parameter-specific matching) - **NEW: Available with user methods**
2. Sequences (ThenCall chain)
3. OnCall/Returns (explicit configuration)
4. **User Method (fallback behavior)** - existing
5. Smart Default (for interfaces) or Base Class (for classes)

This would allow the best of both worlds:
- User method provides default hydrated behavior
- `.When()` overrides specific argument combinations
- Full API consistency across all patterns

## Expected API:

```csharp
[KnockOff]
public partial class MyRepoStub(List<User> Users) : IMyRepo
{
    protected override User? GetUser_(int id)
    {
        // Default: lookup from list
        return Users.Single(u => u.Id == id);
    }
}

// Override specific cases:
stub.GetUser.When(99).Returns(new User { Id = 99, Name = "Special" });

// Call with id=99: returns the special user (When matched)
// Call with id=1: falls through to user method (list lookup)
```

---

## Plans

- [Enable .When() API with User Methods - Implementation Plan](../plans/when-with-user-methods.md)

---

## Tasks

- [x] Investigate current interceptor implementation for user methods
- [x] Identify why `.When()` is unavailable with user methods
- [x] Design integration of When chain with user method fallback
- [x] Implement When chain support for user method interceptors
- [x] Add tests covering When + user method combinations
- [x] Update api-consistency-matrix.md to reflect accurate support
- [x] Add documentation examples showing When + user method pattern
- [x] Verify all standalone patterns (Standalone, Generic Standalone, Standalone Class, Generic Standalone Class)

---

## Progress Log

**2026-02-04**: Created todo based on discovered API inconsistency in ReadMeUseCase.cs

**2026-02-05**: Architectural analysis completed by knockoff-architect:
- Root cause identified: User method interceptors use simplified `RenderUserMethodInterceptorClass()` which lacks When chain, Sequence, and verification infrastructure
- Scope confirmed: All standalone patterns with user methods; methods only (matching inline pattern)
- Design approach: Extend `MethodInterceptorRenderer` to handle user method fallback instead of maintaining separate renderer
- Priority chain defined: When -> Sequences -> OnCall/Returns -> User Method (user method replaces Source/Strict as final fallback)
- Created implementation plan with 6 phases and comprehensive test strategy

---

## Results / Conclusions

**2026-02-05**: Implementation complete.

### Summary

User method interceptors now have full `.When()` API support, matching the capabilities of inline stubs. The implementation unified the renderer path so user method interceptors use `MethodInterceptorRenderer` with a new `UserMethodFallback` option.

### Key Changes

1. **Model Layer**: Added `UserMethodName` property to `UnifiedMethodInterceptorModel` and `InterceptorRenderOptions` now includes `UserMethodFallback` and `StubTypeName`

2. **Renderer Layer**: `FlatRenderer` now routes user method groups through the unified `MethodInterceptorRenderer` instead of the simplified `RenderUserMethodInterceptorClass()`

3. **Generated Code**: User method interceptors now have:
   - `_whenChain` field and `When()` methods
   - `Invoke()` method with priority chain: When > Sequences > OnCall/Returns > User Method
   - Full `WhenBuilder`, `WhenMatcher`, `MethodCallBuilderImpl` nested classes
   - Interface implementations call `Interceptor.Invoke(Strict, this, args)`

### Tests Added

18 new tests in `UserMethodWhenTests.cs` covering:
- Basic value and predicate When matching
- ThenWhen/ThenCall chaining
- Void and async methods
- Sequences with user method fallback
- Verification with When chains
- Mixed When + OnCall scenarios
- Multi-parameter When matching

### API Example

```csharp
[KnockOff]
public partial class MyRepoStub(List<User> Users) : IMyRepo
{
    protected override User? GetUser_(int id) => Users.Single(u => u.Id == id);
}

// Override specific cases with When:
stub.GetUser.When(99).Returns(new User { Id = 99, Name = "Special" });

// id=99: Returns special user (When matched)
// id=1: Falls through to user method (list lookup)
```
