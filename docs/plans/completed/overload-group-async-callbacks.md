# Simplified Async Callbacks for Overload Groups

**Date:** 2026-01-26
**Related Todo:** [Simplified Async Callbacks for Overload Groups](../todos/overload-group-async-callbacks.md)
**Status:** Complete
**Last Updated:** 2026-01-26 (Implementation Complete)

---

## Overview

Extend overload group interceptors to support simplified async callbacks:
- `Func<..., T>` for `Task<T>`/`ValueTask<T>` methods - returns inner type, auto-wrapped
- `Action<...>` for `Task`/`ValueTask` methods - void callback, auto-returns completed task

This feature already works for single-signature methods in `RenderSingleSignatureContent`. This plan extends the same pattern to `RenderOverloadGroupContent`.

---

## Approach

The implementation follows the established pattern in single-signature rendering:

1. **Per-signature storage fields** - Each overload gets its own simplified callback storage and tracking
2. **Per-signature OnCall overloads** - Each async overload gets a simplified `OnCall(Func/Action)` method
3. **Per-signature Invoke updates** - Each overload's Invoke method checks for simplified callbacks
4. **Mutual exclusivity** - Simplified callbacks clear other callback types (and vice versa)

**Why this works (from feasibility analysis):**
- Different `Func<>`/`Action<>` arities are distinct C# types
- The C# compiler resolves the correct overload based on lambda parameter count/types
- Each `OnCall` returns per-signature tracking, so Verify works correctly

**Out of scope:**
- Value overloads (`OnCall(value)`) - all overloads share the same return type, making the signature ambiguous

---

## Design

### Current Single-Signature Pattern (Reference)

From `RenderSingleSignatureContent` (lines 91-111):

```csharp
// Storage fields
private Func<int, User?>? _onCallSimplified;
private MethodTrackingImpl? _onCallSimplifiedTracking;

private Action<string>? _onCallSimplifiedVoid;
private MethodTrackingImpl? _onCallSimplifiedVoidTracking;

// OnCall method (lines 209-237)
public IMethodTracking<int> OnCall(Func<int, User?> callback)
{
    // Clear other callback types (mutual exclusivity)
    _sequence = null;
    _hasOnCallValue = false;
    _onCall = null;

    // Set simplified callback
    _onCallSimplified = callback;
    _onCallSimplifiedTracking = new MethodTrackingImpl(this);
    return _onCallSimplifiedTracking;
}

// Invoke check (lines 518-534)
if (_onCallSimplified != null && _onCallSimplifiedTracking != null)
{
    _onCallSimplifiedTracking.RecordCall(trackingArgs);
    return Task.FromResult(_onCallSimplified(callbackArgs));
}
```

### Target Overload Group Pattern

For an interface like:
```csharp
interface IRepository {
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByIdAsync(int id, CancellationToken ct);
}
```

The generated interceptor will add:

```csharp
// Per-signature simplified callback storage
private Func<int, User?>? _onCallSimplified_Int32_TaskUser;
private MethodTrackingImpl_Int32_TaskUser? _onCallSimplifiedTracking_Int32_TaskUser;

private Func<int, CancellationToken, User?>? _onCallSimplified_Int32_CancellationToken_TaskUser;
private MethodTrackingImpl_Int32_CancellationToken_TaskUser? _onCallSimplifiedTracking_Int32_CancellationToken_TaskUser;

// Simplified OnCall per signature
public IMethodTracking<int> OnCall(Func<int, User?> callback)
{
    _sequence_Int32_TaskUser = null;
    _onCall_Int32_TaskUser = null;
    _onCallSimplified_Int32_TaskUser = callback;
    _onCallSimplifiedTracking_Int32_TaskUser = new MethodTrackingImpl_Int32_TaskUser(this);
    return _onCallSimplifiedTracking_Int32_TaskUser;
}

public IMethodTrackingArgs<(int id, CancellationToken cancellationToken)> OnCall(Func<int, CancellationToken, User?> callback)
{
    _sequence_Int32_CancellationToken_TaskUser = null;
    _onCall_Int32_CancellationToken_TaskUser = null;
    _onCallSimplified_Int32_CancellationToken_TaskUser = callback;
    _onCallSimplifiedTracking_Int32_CancellationToken_TaskUser = new MethodTrackingImpl_Int32_CancellationToken_TaskUser(this);
    return _onCallSimplifiedTracking_Int32_CancellationToken_TaskUser;
}
```

### File Changes

**Primary file:** `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs`

1. **Add storage fields per overload** (in `RenderOverloadGroupContent`, ~lines 343-365)
   - Add `_onCallSimplified_{suffix}` field for Task<T>/ValueTask<T> overloads
   - Add `_onCallSimplifiedTracking_{suffix}` field
   - Add `_onCallSimplifiedVoid_{suffix}` field for Task/ValueTask overloads
   - Add `_onCallSimplifiedVoidTracking_{suffix}` field

2. **Add OnCall methods per overload** (in `RenderOverloadGroupContent`, ~lines 374-408)
   - For each async overload with inner type: add `OnCall(Func<...>)` method
   - For each void async overload: add `OnCall(Action<...>)` method
   - Each clears other callback types and sets simplified storage

3. **Update Invoke methods** (in `RenderOverloadInvokeMethod`, ~lines 614-715)
   - Add check for `_onCallSimplified_{suffix}` (Task<T>/ValueTask<T>)
   - Add check for `_onCallSimplifiedVoid_{suffix}` (Task/ValueTask)
   - Auto-wrap return values appropriately

4. **Update Reset method** (in `RenderResetMethod`, ~lines 721-770)
   - Reset simplified tracking per overload

5. **Update IsConfigured check** (in `RenderInternalVerificationMembers`, ~lines 772-861)
   - Include simplified callbacks in IsConfigured expression

6. **Update backward-compatible properties** (in `RenderOverloadBackwardCompatibleProperties`, ~lines 1312-1323)
   - Include simplified tracking in TotalCallCount

### Helper Methods to Add/Extend

The existing helper methods can be reused:
- `GetAsyncTypeInfo()` - extracts inner type from Task<T>/ValueTask<T>
- `GetVoidAsyncInfo()` - detects Task/ValueTask (no generic)
- `BuildSimplifiedDelegateType()` - builds Func<params, innerType>
- `BuildSimplifiedVoidDelegateType()` - builds Action<params>

---

## Implementation Steps

### Phase 1: Storage and OnCall Methods
1. Add per-signature simplified callback storage fields in `RenderOverloadGroupContent`
2. Add per-signature simplified OnCall methods with mutual exclusivity
3. Verify compilation passes

### Phase 2: Invoke Method Updates
4. Update `RenderOverloadInvokeMethod` to check simplified callbacks
5. Add proper Task.FromResult / new ValueTask<T> wrapping
6. Verify basic tests pass

### Phase 3: Tracking and Verification
7. Update Reset method to handle simplified tracking per overload
8. Update IsConfigured to include simplified callbacks
9. Update TotalCallCount aggregation

### Phase 4: Testing
10. Add comprehensive tests (see Test Strategy section)
11. Verify all three patterns work (Standalone, Inline Interface, Inline Class)

---

## Acceptance Criteria

- [ ] `stub.AsyncMethod.OnCall((params) => innerValue)` works for Task<T> overloads
- [ ] `stub.AsyncMethod.OnCall((params) => innerValue)` works for ValueTask<T> overloads
- [ ] `stub.AsyncMethod.OnCall((params) => { })` works for Task overloads
- [ ] `stub.AsyncMethod.OnCall((params) => { })` works for ValueTask overloads
- [ ] Each simplified callback returns per-signature tracking
- [ ] Verify() works correctly with per-signature tracking
- [ ] LastArg/LastArgs capture works correctly
- [ ] Mutual exclusivity: simplified clears async callback, async clears simplified
- [ ] All three patterns work: Standalone, Inline Interface, Inline Class
- [ ] No breaking changes to existing APIs
- [ ] All existing tests pass

---

## Dependencies

- Existing simplified callback implementation for single-signature methods (already complete)
- Helper methods: `GetAsyncTypeInfo`, `GetVoidAsyncInfo`, `BuildSimplifiedDelegateType`, `BuildSimplifiedVoidDelegateType`

---

## Risks / Considerations

1. **Complexity in overload resolution** - The C# compiler handles this, but we must ensure delegate types are distinct. Risk is low because different parameter counts/types create different Func<>/Action<> types.

2. **Field naming collisions** - Using `_onCallSimplified_{SignatureSuffix}` pattern ensures uniqueness since SignatureSuffix is already unique per overload.

3. **Tracking interface consistency** - Simplified callbacks should return the same tracking interface as the async delegate version for that signature.

---

## Architectural Verification

**Three Patterns Analysis:**
- **Standalone:** Works via `MethodInterceptorRenderer.RenderOverloadGroupContent`, which is the shared rendering path
- **Inline Interface:** Same renderer, accessed via `[KnockOff<IInterface>]` pattern
- **Inline Class:** Same renderer, accessed via `[KnockOff]` on class implementing interface

All three patterns use `MethodInterceptorRenderer`, so changes apply universally.

**Breaking Changes:** No. This is purely additive - new OnCall overloads for simplified callbacks.

**Pattern Consistency:**
- Follows exact same pattern as single-signature simplified callbacks
- Uses same helper methods (`GetAsyncTypeInfo`, `BuildSimplifiedDelegateType`, etc.)
- Same mutual exclusivity semantics
- Same tracking interface returns

**Codebase Analysis:**
Files examined:
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` - Primary implementation file
- `src/Generator/Model/Shared/MethodOverloadSignature.cs` - Overload signature model
- `src/Generator/Model/Shared/UnifiedMethodInterceptorModel.cs` - Unified model
- `src/Generator/Builder/UnifiedInterceptorBuilder.cs` - Model building logic
- `src/Tests/KnockOffTests/AsyncCallbackSimplificationTests.cs` - Existing simplified tests
- `src/Tests/KnockOffTests/OverloadedMethodTests.cs` - Existing overload tests
- `src/Tests/KnockOffTests/TestInterfaces.cs` - Test interface definitions

Key patterns found:
- Single-signature simplified callbacks: lines 91-260 in renderer
- Overload group rendering: lines 324-436 in renderer
- Overload Invoke: lines 614-715 in renderer
- Helper methods for async detection: lines 1155-1213 in renderer

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-01-26

### My Understanding of This Plan

**Core Change:** Add simplified async callbacks (`Func<..., T>` for `Task<T>`/`ValueTask<T>`, `Action<...>` for `Task`/`ValueTask`) to overload group interceptors, extending the pattern already implemented for single-signature methods.

**User-Facing API:** Users can write `stub.GetByIdAsync.OnCall((int id) => user)` instead of `stub.GetByIdAsync.OnCall((int id) => Task.FromResult(user))` for methods with multiple overloads.

**Internal Changes:** Renderer changes only - adding storage fields, OnCall methods, and Invoke logic for simplified callbacks per-overload signature.

**Patterns Affected:** All three (Standalone, Inline Interface, Inline Class) since they share `MethodInterceptorRenderer`.

### Codebase Investigation

**Files Examined:**
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` - Confirmed single-signature pattern (lines 91-260) and overload group pattern (lines 324-436). Plan accurately describes both.
- `src/Tests/KnockOffTests/AsyncCallbackSimplificationTests.cs` - Comprehensive tests for single-signature simplified callbacks including mutual exclusivity. Good test pattern reference.
- `src/Tests/KnockOffTests/OverloadedMethodTests.cs` - Overload testing patterns but no async simplified callback tests (expected - that's what we're adding).
- `src/Generator/Model/Shared/MethodOverloadSignature.cs` - Contains ReturnType which is sufficient for async detection via existing helpers.
- `src/Generator/Builder/UnifiedInterceptorBuilder.cs` - Model building passes ReturnType through; helper methods exist.
- `src/Tests/KnockOffTests/TestInterfaces.cs` - `IOverloadedService` has async overloads (`GetByIdAsync` with int id and int id + CancellationToken).

**Discrepancies Found:** None significant. Plan accurately reflects codebase structure.

### Review Checklist

**Completeness:**
- [x] All three patterns addressed (shared `MethodInterceptorRenderer`)
- [x] Null/empty/default handling (follows existing nullable storage pattern)
- [x] Mutual exclusivity documented
- [x] Tracking interface consistency (per-signature tracking)

**Correctness:**
- [x] Generated code examples look correct and follow existing patterns
- [x] Implementation consistent with single-signature pattern
- [x] Renderer-only changes are appropriate (no model/builder changes needed)
- [x] No breaking changes

**Clarity:**
- [x] Implementation steps are clear and phased appropriately
- [x] Test strategy is specific with test interface defined
- [x] Edge cases (ref/out) handled by reusing existing helper checks

### Minor Additions Needed During Implementation

During review I identified that the following items are implied by the plan but should be explicit during implementation:

1. **CheckVerification/CheckVerificationAll count calculation** - Lines 836 and 853 compute `count` from only `_onCallTracking_{suffix}` and `_sequence_{suffix}`. Must add simplified tracking to count calculation.

2. **IsConfigured expression for overloads** - Line 821-822 checks only `_onCall_{suffix}` and `_sequence_{suffix}`. Must include `_onCallSimplified_{suffix}` and `_onCallSimplifiedVoid_{suffix}`.

These are covered by plan items 5 and 6 but the specifics should be noted during implementation.

### Why This Plan Is Ready for Implementation

This plan is well-structured and follows the established single-signature pattern exactly. The architect's analysis is thorough:
- Codebase deep-dive completed with specific line references
- Helper methods already exist and can be reused
- Test strategy is comprehensive with specific test interface definition
- Phased implementation with verification checkpoints

The minor additions noted above are implementation details that fall naturally from the pattern - they don't require architectural decisions.

### Recommendation

**Approved for implementation.** The plan is clear, follows established patterns, and the identified details are straightforward to address during implementation.

---

## Implementation Contract

**Created:** 2026-01-26
**Approved by:** knockoff-developer

### In Scope

**Phase 1: Storage and OnCall Methods**
- [ ] `MethodInterceptorRenderer.cs` (~line 343-365): Add per-overload simplified storage fields in `RenderOverloadGroupContent`
  - `_onCallSimplified_{suffix}` for Task<T>/ValueTask<T> overloads
  - `_onCallSimplifiedTracking_{suffix}` for Task<T>/ValueTask<T> overloads
  - `_onCallSimplifiedVoid_{suffix}` for Task/ValueTask overloads
  - `_onCallSimplifiedVoidTracking_{suffix}` for Task/ValueTask overloads
- [ ] `MethodInterceptorRenderer.cs` (~line 374-408): Add per-overload simplified OnCall methods
  - `OnCall(Func<..., T>)` for each async overload with inner type
  - `OnCall(Action<...>)` for each void async overload
  - Mutual exclusivity: clear `_onCall_{suffix}` and `_sequence_{suffix}` when setting simplified
- [ ] **Checkpoint**: Build compiles, tests still pass

**Phase 2: Invoke Method Updates**
- [ ] `MethodInterceptorRenderer.cs` (~line 614-715): Update `RenderOverloadInvokeMethod`
  - Add check for `_onCallSimplified_{suffix}` (after sequence, before default)
  - Add check for `_onCallSimplifiedVoid_{suffix}`
  - Use `Task.FromResult` / `new ValueTask<T>` wrapping appropriately
- [ ] **Checkpoint**: Basic simplified callback invocation works

**Phase 3: Tracking and Verification**
- [ ] `MethodInterceptorRenderer.cs` (~line 721-770): Update `RenderResetMethod` for overloads
  - Reset `_onCallSimplifiedTracking_{suffix}` per overload
  - Reset `_onCallSimplifiedVoidTracking_{suffix}` per overload
- [ ] `MethodInterceptorRenderer.cs` (~line 812-860): Update `RenderInternalVerificationMembers` for overloads
  - Add simplified callbacks to `IsConfigured` expression (line 821)
  - Add simplified tracking to `count` in `CheckVerification` (line 836)
  - Add simplified tracking to `count` in `CheckVerificationAll` (line 853)
- [ ] `MethodInterceptorRenderer.cs` (~line 1312-1323): Update `RenderOverloadBackwardCompatibleProperties`
  - Add simplified tracking to `TotalCallCount` sum expression
- [ ] **Checkpoint**: Verify/VerifyAll tests pass

**Phase 4: Testing**
- [ ] Create test file or extend existing: Add `IAsyncOverloadService` test interface
  - Task<T> overloads (e.g., `GetByIdAsync(int id)`, `GetByIdAsync(int id, CancellationToken ct)`)
  - ValueTask<T> overloads
  - Task (void) overloads
  - ValueTask (void) overloads
- [ ] Add test: Task<T> simplified callback returns correct value
- [ ] Add test: ValueTask<T> simplified callback returns correct value
- [ ] Add test: Task void callback executes and returns CompletedTask
- [ ] Add test: ValueTask void callback executes and returns default
- [ ] Add test: Multiple overloads - configure different callbacks, verify correct invocation
- [ ] Add test: Tracking per signature (LastArg, LastArgs, CallCount)
- [ ] Add test: Verify(Times) works per signature
- [ ] Add test: Mutual exclusivity (simplified clears async callback, async clears simplified)
- [ ] Add test: All three patterns (Standalone, Inline Interface, Inline Class)
- [ ] **Checkpoint**: All tests pass (`dotnet test`)

### Explicitly Out of Scope

- Value overloads (`OnCall(value)`) for overload groups - signatures are ambiguous (all overloads share same return type)
- Model changes (`MethodOverloadSignature`, `UnifiedMethodInterceptorModel`) - not needed, return type already available
- Builder changes (`UnifiedInterceptorBuilder`) - not needed
- Documentation updates - separate todo if needed

### Verification Gates

1. **After Phase 1**: Build compiles, existing tests pass (no regressions)
2. **After Phase 2**: New simplified callbacks can be invoked and return correct values
3. **After Phase 3**: Verify/VerifyAll work correctly with simplified callbacks
4. **Final**: All new tests pass, `dotnet test` green, no existing test regressions

### Stop Conditions

If any of these occur, STOP and report:
- Out-of-scope test fails unexpectedly
- Architectural contradiction discovered (e.g., model needs changes after all)
- Generated code does not compile
- Type ambiguity issues with simplified delegate resolution

---

## Implementation Progress

**Phase 1:** Storage and OnCall Methods
- [x] Add per-signature simplified callback storage fields
- [x] Add per-signature simplified OnCall methods
- [x] **Verification**: Build compiles successfully, all 774 tests pass

**Phase 2:** Invoke Method Updates
- [x] Update RenderOverloadInvokeMethod for simplified callbacks
- [x] Add Task.FromResult / ValueTask wrapping
- [x] **Verification**: Build compiles, all tests pass

**Phase 3:** Tracking and Verification
- [x] Update Reset method
- [x] Update IsConfigured and TotalCallCount
- [x] **Verification**: All tests pass

**Phase 4:** Testing
- [x] Add comprehensive test coverage (24 new tests)
- [x] Verify all three patterns (Standalone, Inline Interface, Inline Class)
- [x] **Verification**: All tests pass - 798 tests (net9.0/net10.0), 797 tests (net8.0)

---

## Test Strategy

### Test Interface
Add to existing test interfaces or create new:
```csharp
public interface IAsyncOverloadService
{
    // Task<T> overloads
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByIdAsync(int id, CancellationToken ct);

    // ValueTask<T> overloads
    ValueTask<string> GetCachedAsync(string key);
    ValueTask<string> GetCachedAsync(string key, bool refresh);

    // Task (void) overloads
    Task SaveAsync(User user);
    Task SaveAsync(User user, CancellationToken ct);

    // ValueTask (void) overloads
    ValueTask LogAsync(string message);
    ValueTask LogAsync(string message, int level);
}
```

### Test Cases

1. **Basic functionality:**
   - Task<T> simplified callback returns correct value
   - ValueTask<T> simplified callback returns correct value
   - Task void callback executes and returns CompletedTask
   - ValueTask void callback executes and returns default

2. **Multiple overloads:**
   - Configure different simplified callbacks for each overload
   - Call each overload, verify correct callback invoked

3. **Tracking:**
   - LastArg captured correctly per signature
   - LastArgs captured correctly per signature
   - CallCount tracked per signature
   - Verify(Times) works per signature

4. **Mutual exclusivity:**
   - Simplified callback clears async delegate callback
   - Async delegate callback clears simplified callback
   - Sequence clears simplified callback
   - Simplified callback clears sequence

5. **Three patterns:**
   - Standalone: `[KnockOff] class Stub : IInterface`
   - Inline Interface: `[KnockOff<IInterface>] class Container`
   - Inline Class: Same as inline interface, different access pattern

---

## Completion Evidence

**Completed:** 2026-01-26

### Tests Passing

```
Passed!  - Failed:     0, Passed:   798, Skipped:     0, Total:   798 - KnockOffTests.dll (net9.0)
Passed!  - Failed:     0, Passed:   798, Skipped:     0, Total:   798 - KnockOffTests.dll (net10.0)
Passed!  - Failed:     0, Passed:   797, Skipped:     0, Total:   797 - KnockOffTests.dll (net8.0)
Passed!  - Failed:     0, Passed:   473, Skipped:     0, Total:   473 - KnockOff.NeatooInterfaceTests.dll
Passed!  - Failed:     0, Passed:   285, Skipped:     0, Total:   285 - KnockOff.Documentation.Samples.dll
```

24 new tests added in `OverloadGroupAsyncCallbackTests.cs` covering:
- Task<T> simplified callbacks with 1, 2, 3 parameters
- ValueTask<T> simplified callbacks
- Task (void) simplified callbacks
- ValueTask (void) simplified callbacks
- Tracking (LastArg, LastArgs, Verify)
- Mutual exclusivity
- Reset behavior
- All three patterns (Standalone, Inline Interface, Inline Class)

### Generated Code Sample

Example of generated simplified callback OnCall for overload group:

```csharp
// For interface: Task<User?> GetByIdAsync(int id) / Task<User?> GetByIdAsync(int id, CancellationToken ct)

// Storage fields (per signature)
private Func<int, User?>? _onCallSimplified_Int32_TaskUser;
private MethodTrackingImpl_Int32_TaskUser? _onCallSimplifiedTracking_Int32_TaskUser;

private Func<int, CancellationToken, User?>? _onCallSimplified_Int32_CancellationToken_TaskUser;
private MethodTrackingImpl_Int32_CancellationToken_TaskUser? _onCallSimplifiedTracking_Int32_CancellationToken_TaskUser;

// Simplified OnCall (returns inner type, auto-wrapped)
public IMethodTracking<int> OnCall(Func<int, User?> callback)
{
    // Clear other callback types (mutual exclusivity)
    _onCall_Int32_TaskUser = null;
    _sequence_Int32_TaskUser = null;
    // Set simplified callback
    _onCallSimplified_Int32_TaskUser = callback;
    _onCallSimplifiedTracking_Int32_TaskUser = new MethodTrackingImpl_Int32_TaskUser(this);
    return _onCallSimplifiedTracking_Int32_TaskUser;
}

// In Invoke method
if (_onCallSimplified_Int32_TaskUser != null && _onCallSimplifiedTracking_Int32_TaskUser != null)
{
    _onCallSimplifiedTracking_Int32_TaskUser.RecordCall(id);
    return Task.FromResult(_onCallSimplified_Int32_TaskUser(id));
}
```

### All Checklist Items

**Implementation Contract - All items complete:**

Phase 1: Storage and OnCall Methods - COMPLETE
- [x] Added per-overload simplified callback storage fields
- [x] Added per-overload simplified OnCall methods with mutual exclusivity

Phase 2: Invoke Method Updates - COMPLETE
- [x] Updated `RenderOverloadInvokeMethod` for simplified callbacks
- [x] Added `Task.FromResult` / `new ValueTask<T>` wrapping

Phase 3: Tracking and Verification - COMPLETE
- [x] Updated Reset method for simplified tracking per overload
- [x] Updated IsConfigured expression for overloads
- [x] Updated CheckVerification count calculation
- [x] Updated CheckVerificationAll condition and count
- [x] Updated TotalCallCount aggregation

Phase 4: Testing - COMPLETE
- [x] Created `IAsyncOverloadService` test interface
- [x] Added 24 comprehensive tests
- [x] Verified all three patterns work
