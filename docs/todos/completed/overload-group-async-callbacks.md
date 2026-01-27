# Simplified Async Callbacks for Overload Groups

**Status:** Complete
**Priority:** Low
**Created:** 2026-01-26
**Last Updated:** 2026-01-26

---

## Problem

Methods with multiple overloads (same name, different signatures) use a separate rendering path (`RenderOverloadGroupContent`) that doesn't support simplified async callbacks: `OnCall((params) => nonTaskValue)`.

This feature only works for single-signature methods currently.

Example interface:
```csharp
interface IRepository {
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByIdAsync(int id, CancellationToken ct);  // Overload
}
```

Currently must use verbose syntax:
```csharp
stub.GetByIdAsync.OnCall((int id) => Task.FromResult(user));
stub.GetByIdAsync.OnCall((int id, CancellationToken ct) => Task.FromResult(user));
```

## Solution

Extend `RenderOverloadGroupContent` to support per-signature simplified callback storage for async methods, following the patterns established in `RenderSingleSignatureContent`.

**Target API:**
```csharp
// Func<..., T> for Task<T>/ValueTask<T> - returns inner type, auto-wrapped
stub.GetByIdAsync.OnCall((int id) => user);
stub.GetByIdAsync.OnCall((int id, CancellationToken ct) => user);

// Action<...> for Task/ValueTask - void callbacks
stub.SaveAsync.OnCall((User user) => { /* side effect */ });
stub.SaveAsync.OnCall((User user, CancellationToken ct) => { /* side effect */ });
```

**Why this works:** Different `Func<>`/`Action<>` arities are distinct types, so C# compiler resolves correctly. Each returns per-signature tracking, so Verify works as expected.

**Out of scope:** Value overloads (`OnCall(value)`) - all overloads share the same return type, making the signature ambiguous. Shared-value approach was considered but rejected because Verify semantics become unclear (aggregate vs per-signature tracking, no arg capture).

---

## Plans

- [Simplified Async Callbacks for Overload Groups](../plans/overload-group-async-callbacks.md)

---

## Tasks

- [x] Add `Func<..., T>` simplified callbacks for `Task<T>`/`ValueTask<T>` overloads
- [x] Add `Action<...>` simplified callbacks for `Task`/`ValueTask` overloads
- [x] Add tests for simplified callbacks with overloaded methods (all three patterns)
- [ ] Update documentation (out of scope for this implementation)

---

## Progress Log

- 2026-01-26: Created as follow-up from async-callback-simplification feature
- 2026-01-26: Feasibility analysis with knockoff-architect. Value overloads ruled out - all overloads share same return type making `OnCall(value)` signature ambiguous. Shared-value approach considered but rejected due to Verify complexity (aggregate tracking loses per-signature arg capture). Scoped to simplified async callbacks only, which work because different `Func<>`/`Action<>` arities are distinct types.
- 2026-01-26: Architecture plan created at `docs/plans/overload-group-async-callbacks.md`. Deep-dive analysis completed. Implementation follows established single-signature pattern - purely renderer changes, no model changes needed.
- 2026-01-26: Developer review completed. Plan approved with minor implementation details noted.
- 2026-01-26: Implementation completed. All 4 phases complete. 24 new tests added. All tests pass.

---

## Results / Conclusions

**Feature successfully implemented.** Overload groups now support simplified async callbacks:

```csharp
// Before (verbose)
stub.GetByIdAsync.OnCall((int id) => Task.FromResult(user));

// After (simplified)
stub.GetByIdAsync.OnCall((int id) => user);  // Auto-wrapped in Task.FromResult
```

**Key implementation details:**
- Per-signature storage: `_onCallSimplified_{suffix}` and `_onCallSimplifiedVoid_{suffix}`
- Per-signature tracking: Follows same pattern as single-signature methods
- Mutual exclusivity: Simplified callbacks clear async delegate callbacks and vice versa
- All three patterns work: Standalone, Inline Interface, Inline Class

**Files modified:**
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` - Core implementation

**Files added:**
- `src/Tests/KnockOffTests/OverloadGroupAsyncCallbackTests.cs` - 24 comprehensive tests

