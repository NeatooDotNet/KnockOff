# Remove Duplicate Call/Return on Task/ValueTask Methods

**Status:** Complete
**Priority:** Medium
**Created:** 2026-02-07
**Last Updated:** 2026-02-08

---

## Problem

The generator produces **both** `Return` and `Call` entry points on `Task`/`ValueTask` methods (without `<T>`). For example, `Task SaveAsync(T entity)` generates:

- `Return(SaveAsyncDelegate callback)` — because `IsVoid` is false (return type is `Task`)
- `Call(Action<T> callback)` — because the renderer detects `Task` as a void-like async type

Having both is confusing. The user sees two different names for configuring the same method.

### Current behavior (confusing)

```csharp
// Task SaveAsync(T entity) — generates BOTH:
stub.SaveAsync.Return((entity) => Task.CompletedTask);  // full delegate
stub.SaveAsync.Call((entity) => { });                   // simplified void callback
```

### Expected behavior (consistent)

```csharp
// Task SaveAsync(T entity) — Return only, with simplification:
stub.SaveAsync.Return((entity) => Task.CompletedTask);  // full delegate
stub.SaveAsync.Return((entity) => { });                 // simplified (auto Task.CompletedTask)
stub.SaveAsync.Return(Task.CompletedTask);              // value form
```

This follows the same unwrapping pattern as `Task<T>`:

```csharp
// Task<string> GetAsync(int id) — Return with unwrap:
stub.GetAsync.Return((id) => Task.FromResult("value")); // full delegate
stub.GetAsync.Return((id) => "value");                  // simplified (auto Task.FromResult)
stub.GetAsync.Return("value");                          // value form
```

---

## Solution

Rename the existing `Call(Action<...>)` overload to `Return(Action<...>)` on `Task`/`ValueTask` methods. This eliminates the mixed naming — everything is `Return` for non-void methods.

---

## Plans

- [Rename Call(Action) to Return(Action) on Task/ValueTask Methods](../plans/completed/rename-task-call-to-return.md)

---

## Tasks

- [x] Rename `Call(Action<...>)` to `Return(Action<...>)` for Task/ValueTask methods in the renderer
- [x] Update affected tests
- [x] Verify all tests pass and Design projects compile

---

## Progress Log

### 2026-02-07
- Discovered issue: Task/ValueTask methods generate both `Return` and `Call` entry points
- Initially explored full Action vs Function nomenclature rework (IsAction concept, library changes, When chain renaming)
- After discussion, realized the core issue is simpler: just eliminate the duplicate by renaming `Call` to `Return` on Task/ValueTask methods
- This follows the same unwrapping pattern already used for Task<T> simplified callbacks
- Scrapped original plan, rewriting todo with focused scope

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] Design project builds successfully
- [x] Design project tests pass
- [x] All KnockOffTests pass

**Verification results:**
- Design build: Pass (0 errors, 0 warnings)
- Design tests: Pass (301 tests × 3 frameworks)
- KnockOffTests: Pass (3,758+ tests × 3 frameworks)

---

## Results / Conclusions

Renamed `Call(Action<...>)` to `Return(Action<...>)` on Task/ValueTask methods in the renderer. Two functional line changes in `MethodInterceptorRenderer.cs` (lines 305 and 539), plus 6 comment updates, 9 test/design files, 5 documentation files. All non-void methods now consistently use `Return` as their entry point. The simplified void callback follows the same unwrapping pattern as `Task<T>` simplified callbacks.
