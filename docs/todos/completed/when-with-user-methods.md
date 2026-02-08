# Enable .When() API with Stub Overrides

**Status:** Complete
**Priority:** High
**Created:** 2026-02-04
**Last Updated:** 2026-02-05
**Completed:** 2026-02-05

---

## Problem

When a standalone stub defines stub overrides (the `_` suffix pattern), the `.When()` API is unavailable. This creates an API inconsistency that contradicts the api-consistency-matrix.md documentation, which claims `.When()` works for all 8 patterns.

This was discovered when trying to use `.When()` on a standalone stub with stub overrides:

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
myRepoKO.GetUser.When(1).Returns(user1).Verifiable(Called.Twice);
// Error: cannot convert from 'int' to 'System.Action<...GetUserInterceptor>'
```

The stub override provides default "hydrated" behavior (e.g., list lookup), but there's no way to override specific argument combinations for test scenarios.

## Solution

Extend the interceptor priority chain to support `.When()` even when stub overrides are defined. The priority would be:

1. When chains (parameter-specific matching) - **NEW: Available with stub overrides**
2. Sequences (ThenCall chain)
3. OnCall/Returns (explicit configuration)
4. **Stub Override (fallback behavior)** - existing
5. Smart Default (for interfaces) or Base Class (for classes)

This would allow the best of both worlds:
- Stub override provides default hydrated behavior
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
// Call with id=1: falls through to stub override (list lookup)
```

---

## Plans

- [Enable .When() API with Stub Overrides - Implementation Plan](../plans/when-with-user-methods.md)

---

## Tasks

- [x] Investigate current interceptor implementation for stub overrides
- [x] Identify why `.When()` is unavailable with stub overrides
- [x] Design integration of When chain with stub override fallback
- [x] Implement When chain support for stub override interceptors
- [x] Add tests covering When + stub override combinations
- [x] Update api-consistency-matrix.md to reflect accurate support
- [x] Add documentation examples showing When + stub override pattern
- [x] Verify all standalone patterns (Standalone, Generic Standalone, Standalone Class, Generic Standalone Class)

---

## Progress Log

**2026-02-04**: Created todo based on discovered API inconsistency in ReadMeUseCase.cs

**2026-02-05**: Architectural analysis completed by knockoff-architect:
- Root cause identified: Stub override interceptors use simplified `RenderUserMethodInterceptorClass()` which lacks When chain, Sequence, and verification infrastructure
- Scope confirmed: All standalone patterns with stub overrides; methods only (matching inline pattern)
- Design approach: Extend `MethodInterceptorRenderer` to handle stub override fallback instead of maintaining separate renderer
- Priority chain defined: When -> Sequences -> OnCall/Returns -> Stub Override (stub override replaces Source/Strict as final fallback)
- Created implementation plan with 6 phases and comprehensive test strategy

---

## Results / Conclusions

**2026-02-05**: Implementation complete.

### Summary

Stub override interceptors now have full `.When()` API support, matching the capabilities of inline stubs. The implementation unified the renderer path so stub override interceptors use `MethodInterceptorRenderer` with a new `UserMethodFallback` option.

### Key Changes

1. **Model Layer**: Added `UserMethodName` property to `UnifiedMethodInterceptorModel` and `InterceptorRenderOptions` now includes `UserMethodFallback` and `StubTypeName`

2. **Renderer Layer**: `FlatRenderer` now routes stub override groups through the unified `MethodInterceptorRenderer` instead of the simplified `RenderUserMethodInterceptorClass()`

3. **Generated Code**: Stub override interceptors now have:
   - `_whenChain` field and `When()` methods
   - `Invoke()` method with priority chain: When > Sequences > OnCall/Returns > Stub Override
   - Full `WhenBuilder`, `WhenMatcher`, `MethodCallBuilderImpl` nested classes
   - Interface implementations call `Interceptor.Invoke(Strict, this, args)`

### Tests Added

18 new tests in `UserMethodWhenTests.cs` covering:
- Basic value and predicate When matching
- ThenWhen/ThenCall chaining
- Void and async methods
- Sequences with stub override fallback
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
// id=1: Falls through to stub override (list lookup)
```
