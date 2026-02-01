# User-Defined Method Verifiable Implementation

**Date:** 2026-01-31
**Related Todo:** [Add Verifiable Support for User-Defined Methods](../todos/user-method-verifiable.md)
**Status:** Complete
**Last Updated:** 2026-01-31

---

## Overview

Fix user-defined method interceptors to properly track verifiable state and participate in `Stub.Verify()` aggregation.

**Problem**: User-defined methods have `Verifiable()` methods that just return `this` without tracking state. They are also missing from `Stub.Verify()` aggregation.

**Solution**: Add `_isVerifiable` and `_verifiableTimes` fields, update `Verifiable()` methods to set state, add `CheckVerification()` method, and include user methods in `Stub.Verify()`.

---

## Approach

Follow the existing pattern used by regular method interceptors in `MethodInterceptorRenderer`:
1. Add verifiable state fields
2. Update `Verifiable()` methods to set state
3. Add `CheckVerification()` method for aggregate verification
4. Include user methods in `Stub.Verify()` aggregation loop

User methods should NOT be included in `VerifyAll()` because they are always "configured" (user provides implementation), so including them would require every user method to be called.

---

## Design

### Files to Modify

**Primary: `src/Generator/Renderer/FlatRenderer.cs`**

Three locations need changes:

1. **`RenderUserMethodInterceptorClass`** (lines 1548-1661)
   - Add `_isVerifiable` and `_verifiableTimes` fields after line 1569
   - Add `CheckVerification()` method after `Reset()` (after line 1618)
   - Update `Verifiable()` methods (lines 1636-1658) to set state instead of just returning `this`

2. **Guard condition at line 2114** (CRITICAL - discovered during developer review)
   - The current guard `if (unit.MethodGroups.Count > 0)` is too restrictive
   - `RenderVerifyMethods` is never called for stubs with only user methods, properties, indexers, or events
   - Must expand to include all verifiable member types

3. **`RenderVerifyMethods`** (lines 2126-2232)
   - Add user method interceptors to `Verify()` aggregation (after line 2188)
   - Note: Do NOT add to `VerifyAll()` - user methods are always "configured"

### Code Changes

#### 1. Add Verifiable State Fields (after line 1569)

```csharp
// Verifiable state
w.Line("private bool _isVerifiable;");
w.Line("private global::KnockOff.Times? _verifiableTimes;");
w.Line();
```

#### 2. Add CheckVerification Method (after Reset method, ~line 1618)

```csharp
w.Line("/// <summary>Checks verification for Stub.Verify().</summary>");
w.Line($"internal global::KnockOff.VerificationFailure? CheckVerification()");
using (w.Braces())
{
    w.Line("if (!_isVerifiable) return null;");
    w.Line("var times = _verifiableTimes ?? global::KnockOff.Times.AtLeastOnce;");
    w.Line($"if (!times.Validate(CallCount)) return new global::KnockOff.VerificationFailure(\"{method.MethodName}\", times, CallCount);");
    w.Line("return null;");
}
w.Line();
```

#### 3. Update Verifiable Methods (replace lines 1636-1658)

**For base interface (isBaseInterface = true):**
```csharp
w.Line("/// <summary>Marks for verification by Stub.Verify().</summary>");
w.Line("public global::KnockOff.IMethodTracking Verifiable()");
using (w.Braces())
{
    w.Line("_isVerifiable = true;");
    w.Line("_verifiableTimes = null;");
    w.Line("return this;");
}
w.Line();

w.Line("/// <summary>Marks for verification by Stub.Verify() with Times constraint.</summary>");
w.Line("public global::KnockOff.IMethodTracking Verifiable(global::KnockOff.Times times)");
using (w.Braces())
{
    w.Line("_isVerifiable = true;");
    w.Line("_verifiableTimes = times;");
    w.Line("return this;");
}
```

**For typed interface (isBaseInterface = false):**
```csharp
w.Line("/// <summary>Marks for verification by Stub.Verify().</summary>");
w.Line($"public {trackingInterface} Verifiable()");
using (w.Braces())
{
    w.Line("_isVerifiable = true;");
    w.Line("_verifiableTimes = null;");
    w.Line("return this;");
}
w.Line();

w.Line("/// <summary>Marks for verification by Stub.Verify() with Times constraint.</summary>");
w.Line($"public {trackingInterface} Verifiable(global::KnockOff.Times times)");
using (w.Braces())
{
    w.Line("_isVerifiable = true;");
    w.Line("_verifiableTimes = times;");
    w.Line("return this;");
}
w.Line();

// Explicit interface implementations
w.Line("global::KnockOff.IMethodTracking global::KnockOff.IMethodTracking.Verifiable() => Verifiable();");
w.Line("global::KnockOff.IMethodTracking global::KnockOff.IMethodTracking.Verifiable(global::KnockOff.Times times) => Verifiable(times);");
```

#### 4. Expand Guard Condition (line 2114) - CRITICAL FIX

**Current code (broken):**
```csharp
// Verify and VerifyAll methods (only if there are method interceptors)
if (unit.MethodGroups.Count > 0)
{
    RenderVerifyMethods(w, unit);
}
```

**Replace with:**
```csharp
// Verify and VerifyAll methods (if there are any verifiable members)
// Must check all member types: methods, user methods, properties, indexers, events
var hasUserMethods = unit.Methods.GetArray()?.Any(m => !m.IsGenericMethod && m.UserMethodCall != null) == true;
if (unit.MethodGroups.Count > 0
    || hasUserMethods
    || unit.Properties.Count > 0
    || unit.Indexers.Count > 0
    || unit.Events.Count > 0)
{
    RenderVerifyMethods(w, unit);
}
```

**Why this matters:**
- The current guard skips `RenderVerifyMethods` entirely when `MethodGroups.Count == 0`
- Stubs with only user methods, properties, indexers, or events would not have `Verify()` or `VerifyAll()` methods
- Evidence: `StrictModeUserMethodStub.g.cs` has user methods and a property but NO `Verify()`/`VerifyAll()` methods

#### 5. Add User Methods to Verify() Aggregation (after line 2188)

```csharp
// Check verifiable user-defined method interceptors
foreach (var name in userMethodInterceptorNames)
{
    w.Line($"if ({name}.CheckVerification() is {{ }} {name.ToLowerInvariant()}Failure) failures.Add({name.ToLowerInvariant()}Failure);");
}
```

---

## Implementation Steps

1. Modify `RenderUserMethodInterceptorClass` to add verifiable state fields
2. Add `CheckVerification()` method to user method interceptor class
3. Update `Verifiable()` methods to actually set state
4. **Expand guard condition at line 2114** (CRITICAL - address developer concern #1)
5. Modify `RenderVerifyMethods` to include user methods in `Verify()` aggregation
6. Add tests for user method Verifiable behavior
7. Run all tests to verify no regressions

---

## Acceptance Criteria

- [ ] User method interceptors have `_isVerifiable` and `_verifiableTimes` fields
- [ ] `Verifiable()` sets `_isVerifiable = true` and `_verifiableTimes = null`
- [ ] `Verifiable(Times)` sets both fields appropriately
- [ ] `CheckVerification()` returns failure if verifiable and not satisfied
- [ ] **Guard condition expanded** - `RenderVerifyMethods` called for stubs with any verifiable member type
- [ ] `Stub.Verify()` includes user method interceptors
- [ ] `Stub.VerifyAll()` does NOT include user method interceptors
- [ ] Tests verify all scenarios
- [ ] **New test**: Stub with ONLY user methods has working `Verify()`/`VerifyAll()` methods

---

## Dependencies

None - this is a self-contained change to the source generator.

---

## Risks / Considerations

1. **Breaking change?** No - this is fixing a bug. The API exists but doesn't work correctly.

2. **Reset behavior**: `Reset()` should NOT clear `_isVerifiable` (matching regular method interceptor behavior). Current code only resets CallCount and LastArg/LastArgs, which is correct.

3. **Generic user methods**: User-defined methods cannot be generic (filtered out by `!m.IsGenericMethod`), so no special handling needed.

4. **Method name in error message**: Use `method.MethodName` to show the actual method name, not the interceptor class name.

---

## Architectural Verification

### Verification Checklist

- [x] All three patterns analyzed (Standalone, Inline Interface, Inline Class)
- [x] Breaking changes assessment completed
- [x] Pattern consistency verified
- [x] Diagnostic requirements identified (N/A - no new diagnostics needed)
- [x] Test strategy defined
- [x] Edge cases documented
- [x] Codebase deep-dive completed (files examined documented below)

### Three Patterns Analysis

| Pattern | Applies? | Notes |
|---------|----------|-------|
| **Standalone** | YES | User-defined methods only exist in standalone stubs where users can add `protected` methods to their partial class |
| **Inline Interface** | NO | No partial class = no user methods possible |
| **Inline Class** | NO | No partial class = no user methods possible |
| **Inline Delegate** | NO | No partial class = no user methods possible |

**Conclusion:** This feature applies ONLY to the Standalone pattern. The `RenderUserMethodInterceptorClass` method in `FlatRenderer.cs` is only used for standalone stubs.

### Breaking Changes Assessment

**No breaking changes.** This is a bug fix where:
- The `Verifiable()` API already exists on user method interceptors
- Users can already call `stub.UserMethod.Verifiable()` - it just doesn't work correctly
- After this fix, the API will function as documented

### Pattern Consistency

The implementation follows the exact pattern from regular method interceptors in `MethodInterceptorRenderer.cs`:

| Feature | Regular Methods | User Methods (Current) | User Methods (After Fix) |
|---------|----------------|------------------------|-------------------------|
| `_isVerifiable` field | Yes (line 137) | No | Yes |
| `_verifiableTimes` field | Yes (line 138) | No | Yes |
| `Verifiable()` sets state | Yes | No (returns `this`) | Yes |
| `CheckVerification()` | Yes (line 1091) | No | Yes |
| In `Stub.Verify()` | Yes | No | Yes |
| In `Stub.VerifyAll()` | Yes (if configured) | No | No (intentional) |

### Codebase Analysis

**Files Examined:**

1. **`/home/keithvoels/neatoodotnet/KnockOff/src/Generator/Renderer/FlatRenderer.cs`**
   - Lines 1548-1661: `RenderUserMethodInterceptorClass` method
   - Lines 2126-2232: `RenderVerifyMethods` method
   - Lines 2135-2139: `userMethodInterceptorNames` collection (unused currently)
   - Line 1569: `CallCount` property (where fields should be added after)
   - Lines 1636-1658: Current `Verifiable()` implementations (to be replaced)

2. **`/home/keithvoels/neatoodotnet/KnockOff/src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs`**
   - Lines 137-138: `_isVerifiable` and `_verifiableTimes` field generation pattern
   - Lines 1091-1118: `CheckVerification()` method pattern
   - Lines 1307-1308: Verifiable field naming convention

3. **`/home/keithvoels/neatoodotnet/KnockOff/src/Tests/KnockOffTests/Generated/KnockOff.Generator/KnockOff.KnockOffGenerator/SampleKnockOff.g.cs`**
   - Lines 1370-1404: Generated `GetValue2Interceptor` (user method interceptor)
   - Lines 1397-1403: Current broken `Verifiable()` implementation
   - Lines 1427-1439: `Verify()` method - does NOT include `GetValue2`
   - Lines 1441-1453: `VerifyAll()` method - correctly excludes `GetValue2`

4. **`/home/keithvoels/neatoodotnet/KnockOff/src/Tests/KnockOffTests/TestInterfaces.cs`**
   - Line 17: Example user method `protected int GetValue(int input)`
   - Lines 377-386: Additional user method examples in `StrictModeUserMethodStub`

5. **`/home/keithvoels/neatoodotnet/KnockOff/src/Tests/KnockOffTests/VerificationTests.cs`**
   - Complete verification test patterns to follow for new tests

### Edge Cases Identified

1. **Multiple user methods on same stub**: Each should be independently verifiable and independently fail in `Stub.Verify()`

2. **User methods with different parameter counts**: The interceptor class name varies (e.g., `GetValue2Interceptor`), but the `method.MethodName` gives the actual name for error messages

3. **Reset behavior**: `Reset()` should NOT clear `_isVerifiable` - verified this matches regular method behavior. The current user method `Reset()` only clears `CallCount` and `_lastArg`, which is correct.

4. **Interaction with individual Verify()**: User methods already have `Verify()` and `Verify(Times)` methods that throw on failure. The new `CheckVerification()` is for aggregate verification via `Stub.Verify()` only.

5. **Generic user methods**: Not applicable - user-defined methods for generic interface methods use `GenericMethodHandler`, not `RenderUserMethodInterceptorClass`. Verified by filter: `!m.IsGenericMethod && m.UserMethodCall != null`

### Diagnostic Requirements

No new diagnostics needed. This is a runtime behavior fix, not a compile-time validation change.

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-01-31
**Reviewer:** knockoff-developer

### Re-review Summary

All concerns from the initial review have been addressed by the architect.

**Verified:**
1. Guard condition fix (Code Change #4) correctly expands the check to include all verifiable member types
2. Evidence confirmed: `StrictModeUserMethodStub.g.cs` has NO `Verify()`/`VerifyAll()` methods despite having user methods and a property
3. The `userMethodInterceptorNames` collection is already extracted at lines 2135-2139 but not used - plan correctly adds it to the aggregation loop

### Previous Concerns - Resolution Status

1. **[Critical]: Conditional Rendering Gap** - RESOLVED
   - Architect added Code Change #4 with explicit guard condition expansion
   - Evidence verified against actual generated code

2. **[Minor]: Variable Naming Collision Risk** - ACCEPTED
   - Follows existing pattern; collisions rare in practice

3. **[Clarification]: CallCount vs TotalCallCount** - CONFIRMED
   - Correct: user methods have no sequences, so `CallCount` equals total

### Minor Refinement (Non-blocking)

The guard condition could be simplified since `EquatableArray<T>` implements `IEnumerable<T>`:
```csharp
// Proposed (works but verbose):
var hasUserMethods = unit.Methods.GetArray()?.Any(m => !m.IsGenericMethod && m.UserMethodCall != null) == true;

// Alternative (simpler):
var hasUserMethods = unit.Methods.Any(m => !m.IsGenericMethod && m.UserMethodCall != null);
```
Either approach is correct. Developer may use the simpler form during implementation.

### Codebase Investigation

**Files Examined:**
- `FlatRenderer.cs` (lines 2100-2232) - Guard condition and RenderVerifyMethods confirmed
- `StrictModeUserMethodStub.g.cs` (full file, 531 lines) - Missing Verify methods confirmed
- `FlatGenerationUnit.cs` - Confirmed `unit.Methods` type is `EquatableArray<FlatMethodModel>`
- `EquatableArray.cs` - Confirmed implements `IEnumerable<T>`, LINQ available directly

### Recommendation

**APPROVED.** Ready for implementation. Implementation Contract updated below.

---

## Implementation Contract

**Created:** 2026-01-31
**Approved by:** knockoff-developer

### In Scope

**File: `/home/keithvoels/neatoodotnet/KnockOff/src/Generator/Renderer/FlatRenderer.cs`**

- [ ] Line ~1569: Add `_isVerifiable` and `_verifiableTimes` fields after CallCount in `RenderUserMethodInterceptorClass`
- [ ] Line ~1618: Add `CheckVerification()` method after `Reset()` in `RenderUserMethodInterceptorClass`
- [ ] Lines 1636-1658: Replace `Verifiable()` methods to set state instead of just returning `this`
- [ ] Line 2114: Expand guard condition to check all verifiable member types (CRITICAL)
- [ ] Line ~2188: Add user methods to `Verify()` aggregation loop in `RenderVerifyMethods`

**File: `/home/keithvoels/neatoodotnet/KnockOff/src/Tests/KnockOffTests/UserMethodVerificationTests.cs`** (NEW)

- [ ] Test: User method marked Verifiable, called - passes
- [ ] Test: User method marked Verifiable, NOT called - throws
- [ ] Test: User method marked Verifiable(Times.Exactly(2)) - verifies count
- [ ] Test: Multiple user methods both fail - exception contains both
- [ ] Test: User method NOT marked Verifiable - not checked
- [ ] Test: VerifyAll does NOT include user methods
- [ ] Test: Stub with ONLY user methods has working Verify() (guard condition regression test)

### Explicitly Out of Scope

- `VerifyAll()` changes for user methods (intentionally excluded - user methods are always "configured")
- Regular method interceptors (already working correctly)
- Inline patterns (user methods only exist in Standalone pattern)
- New diagnostics (not needed - this is a runtime behavior fix)

### Verification Gates

1. **After Phase 1** (RenderUserMethodInterceptorClass changes):
   - Build succeeds
   - Generated user method interceptors have `_isVerifiable`, `_verifiableTimes` fields
   - Generated user method interceptors have `CheckVerification()` method
   - Generated `Verifiable()` methods set state

2. **After Phase 2** (Guard condition fix):
   - Build succeeds
   - `StrictModeUserMethodStub.g.cs` now has `Verify()` and `VerifyAll()` methods

3. **After Phase 3** (Verify aggregation):
   - Build succeeds
   - User method interceptors included in `Verify()` loop in generated code

4. **After Phase 4** (Tests):
   - All new tests pass
   - All existing tests pass
   - No regressions

### Stop Conditions

If any of these occur, STOP and report:
- Out-of-scope test fails unexpectedly
- Architectural contradiction discovered (e.g., user methods need to be in VerifyAll after all)
- Generated code does not compile
- Pattern inconsistency with regular method interceptors discovered

---

## Implementation Progress

**Phase 1:** Modify RenderUserMethodInterceptorClass
- [x] Add `_isVerifiable` and `_verifiableTimes` fields
- [x] Add `CheckVerification()` method
- [x] Update `Verifiable()` methods
- [x] **Verification**: Build passes

**Phase 2:** Expand Guard Condition (CRITICAL - addresses developer concern #1)
- [x] Modify guard at line 2114 to check all verifiable member types
- [x] **Verification**: Build passes and `StrictModeUserMethodStub.g.cs` now has `Verify()`/`VerifyAll()` methods
- Note: Indexers excluded due to separate bug with container accessor paths

**Phase 3:** Modify RenderVerifyMethods
- [x] Add user methods to `Verify()` aggregation loop
- [x] **Verification**: Build passes

**Phase 4:** Add Tests
- [x] User method marked Verifiable, called → passes
- [x] User method marked Verifiable, NOT called → throws
- [x] User method marked Verifiable(Times.Exactly(2)) → verifies count
- [x] Multiple user methods both fail → exception contains both
- [x] User method NOT marked Verifiable → not checked
- [x] VerifyAll does NOT include user methods
- [x] **Stub with ONLY user methods** → `Verify()` works correctly (guard condition test)
- [x] **Verification**: All tests pass (18/18 new tests, full suite passes)

---

## Completion Evidence

- **Tests Passing:** 18/18 new tests pass across net8.0, net9.0, net10.0. Full suite: 955-956 tests pass.
- **Generated Code Sample:** See below
- **All Checklist Items:** Confirmed complete

### Generated Code Sample (StrictModeUserMethodStub.g.cs)

User method interceptor with verifiable state:
```csharp
public sealed class GetValue2Interceptor : global::KnockOff.IMethodTracking<int>
{
    private int _lastArg = default!;
    internal int CallCount { get; private set; }
    private bool _isVerifiable;
    private global::KnockOff.Times? _verifiableTimes;

    // ... RecordCall, Reset methods ...

    internal global::KnockOff.VerificationFailure? CheckVerification()
    {
        if (!_isVerifiable) return null;
        var times = _verifiableTimes ?? global::KnockOff.Times.AtLeastOnce;
        if (!times.Validate(CallCount)) return new global::KnockOff.VerificationFailure("GetValue", times, CallCount);
        return null;
    }

    public global::KnockOff.IMethodTracking<int> Verifiable()
    {
        _isVerifiable = true;
        _verifiableTimes = null;
        return this;
    }

    public global::KnockOff.IMethodTracking<int> Verifiable(global::KnockOff.Times times)
    {
        _isVerifiable = true;
        _verifiableTimes = times;
        return this;
    }
}
```

Stub.Verify() now includes user method interceptors:
```csharp
public void Verify()
{
    var failures = new global::System.Collections.Generic.List<global::KnockOff.VerificationFailure>();

    if (Name.CheckVerification() is { } nameFailure) failures.Add(nameFailure);
    if (GetValue2.CheckVerification() is { } getvalue2Failure) failures.Add(getvalue2Failure);
    if (DoSomething2.CheckVerification() is { } dosomething2Failure) failures.Add(dosomething2Failure);

    if (failures.Count > 0)
        throw new global::KnockOff.VerificationException(failures);
}
```

### Implementation Notes

**Indexers excluded from guard condition expansion:** Discovered a latent bug where indexer verification references use incorrect accessor paths (e.g., `IndexerString` instead of `Indexer.OfString`). Added TODO comment; requires separate fix.

---

## Test Scenarios

1. **User method marked Verifiable, called** → `Stub.Verify()` passes
2. **User method marked Verifiable, NOT called** → `Stub.Verify()` throws
3. **User method marked Verifiable(Times.Exactly(2))** → Verifies exact count
4. **Multiple user methods, both fail** → Exception contains both failures
5. **User method NOT marked Verifiable** → NOT checked by `Stub.Verify()`
6. **User method Reset** → Preserves verifiable marking, resets call count
7. **VerifyAll** → Does NOT include user methods (intentional)
8. **Stub with ONLY user methods** → `Stub.Verify()` method exists and works correctly (guard condition regression test)
