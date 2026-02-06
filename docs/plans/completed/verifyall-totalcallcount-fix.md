# Fix VerifyAll TotalCallCount for When Chain Invocations

**Date:** 2026-02-05
**Related Todo:** [VerifyAll TotalCallCount Bug](../todos/completed/verifyall-totalcallcount-bug.md)
**Status:** Complete
**Last Updated:** 2026-02-05

---

## Overview

`CheckVerificationAll()` fails when only a When chain is configured because `TotalCallCount` does not include When chain invocations. The fix includes When chain matcher `CallCount` values everywhere that call counts are computed: in the `TotalCallCount` property, and in the per-overload local count variables used by overload-group verification methods. This is a consistent approach: every method invocation counts regardless of which handler path processed it.

---

## Approach

### Include When chain CallCount in all call count computations

Call counts are computed in two distinct ways depending on whether the interceptor is single-signature or an overload group:

**Single-signature interceptors** use the `TotalCallCount` property for all verification checks (`Verify(Times)`, `CheckVerification()`, `CheckVerificationAll()`).

**Overload-group interceptors** use the aggregate `TotalCallCount` property only for direct `Verify(Times)`. The `CheckVerification()` and `CheckVerificationAll()` methods compute per-overload **local count variables** that do NOT reference `TotalCallCount`. Additionally, `CheckVerificationAll()` has a per-overload **condition guard** (`condExpr`) that determines whether an overload is "configured" -- this guard also excludes When chains.

The fix adds When chain matcher `CallCount` values to **all five** count computation locations. The `matcher.CallCount++` already runs in the When chain invoke path -- it just was not included in any aggregate.

**Why this is correct:**

- `TotalCallCount` is `private` -- not public API. The previous exclusion of When chain calls was tech debt.
- Every method invocation should count regardless of which handler processed it.
- This matches the inline delegate pattern, where `RecordCall()` is called before the When chain check.
- The overload-group `IsConfigured` property (line 1285-1311) already includes When chain checks (`(_whenChain_{suffix}?.Count ?? 0) > 0`). The `condExpr` in `CheckVerificationAll()` should be consistent with this.

### All five fix locations

| # | Location | Method | Current | Fix |
|---|---|---|---|---|
| 1 | ~Line 2742 | `RenderBackwardCompatibleTrackingProperties` | Single-sig `TotalCallCount` getter sums all trackers except When chain | Add `if (_whenChain != null) foreach (var m in _whenChain) sum += m.CallCount;` |
| 2 | ~Line 2818 | `RenderOverloadBackwardCompatibleProperties` | Overload-group `TotalCallCount` expression sums per-overload trackers except When chain | Add `(_whenChain_{suffix}?.Sum(m => m.CallCount) ?? 0)` per eligible overload |
| 3 | ~Line 1325-1338 | Overload `CheckVerification()` | Local `var count` sums per-overload trackers for verifiable check | Add `(_whenChain_{suffix}?.Sum(m => m.CallCount) ?? 0)` to `countParts` per eligible overload |
| 4 | ~Line 1369-1381 | Overload `CheckVerificationAll()` condition guard | `condExpr` checks `_onCall`, `_sequence`, simplified -- NOT When chain | Add `(_whenChain_{suffix}?.Count ?? 0) > 0` to `condParts` per eligible overload (matching `IsConfigured` pattern) |
| 5 | ~Line 1386-1396 | Overload `CheckVerificationAll()` local count | Local `var count` sums per-overload trackers | Add `(_whenChain_{suffix}?.Sum(m => m.CallCount) ?? 0)` to `countParts` per eligible overload |

### Single-signature TotalCallCount computation

**Before:**
```csharp
private int TotalCallCount { get {
    var sum = _unconfiguredCallCount + (_onCallTracking?._callCount ?? 0) + ...;
    if (_sequence != null) foreach (var s in _sequence) sum += s.Tracking._callCount;
    return sum;
} }
```

**After:**
```csharp
private int TotalCallCount { get {
    var sum = _unconfiguredCallCount + (_onCallTracking?._callCount ?? 0) + ...;
    if (_sequence != null) foreach (var s in _sequence) sum += s.Tracking._callCount;
    if (_whenChain != null) foreach (var m in _whenChain) sum += m.CallCount;
    return sum;
} }
```

### Overload-group TotalCallCount computation

**Before:**
```csharp
private int TotalCallCount => _unconfiguredCallCount
    + (_onCallTracking_Suffix?._callCount ?? 0)
    + (_sequence_Suffix?.Sum(s => s.Tracking._callCount) ?? 0) + ...;
```

**After (add for each overload that can have a When chain):**
```csharp
private int TotalCallCount => _unconfiguredCallCount
    + (_onCallTracking_Suffix?._callCount ?? 0)
    + (_sequence_Suffix?.Sum(s => s.Tracking._callCount) ?? 0)
    + (_whenChain_Suffix?.Sum(m => m.CallCount) ?? 0) + ...;
```

### Overload-group CheckVerificationAll condition guard

**Before:**
```csharp
// condParts does NOT include When chain
var condParts = new List<string>
{
    $"_onCall_{suffix} != null",
    $"(_sequence_{suffix}?.Count ?? 0) > 0"
};
// ... simplified callbacks ...
```

**After (add for eligible overloads):**
```csharp
var condParts = new List<string>
{
    $"_onCall_{suffix} != null",
    $"(_sequence_{suffix}?.Count ?? 0) > 0"
};
// ... simplified callbacks ...
// When chain configured check (matching IsConfigured property pattern)
if (canHaveWhenChainForOverload || canHaveVoidWhenChainForOverload)
    condParts.Add($"(_whenChain_{suffix}?.Count ?? 0) > 0");
```

### Overload-group local count (both CheckVerification and CheckVerificationAll)

**Before:**
```csharp
var countParts = new List<string>
{
    $"(_onCallTracking_{suffix}?._callCount ?? 0)",
    $"(_sequence_{suffix}?.Sum(s => s.Tracking._callCount) ?? 0)"
};
// ... simplified tracking ...
```

**After (add for eligible overloads):**
```csharp
var countParts = new List<string>
{
    $"(_onCallTracking_{suffix}?._callCount ?? 0)",
    $"(_sequence_{suffix}?.Sum(s => s.Tracking._callCount) ?? 0)"
};
// ... simplified tracking ...
// When chain call counts
if (canHaveWhenChainForOverload || canHaveVoidWhenChainForOverload)
    countParts.Add($"(_whenChain_{suffix}?.Sum(m => m.CallCount) ?? 0)");
```

### Test update

The existing test `Verify_WhenChainCallsNotInTotalCount` in `src/Tests/KnockOffTests/UserMethodWhenTests.cs` (line 283) must be updated to reflect the corrected behavior: When chain calls ARE counted. The assertion changes from `Times.Once` to `Times.Exactly(2)`.

---

## Design

### Affected Code Locations

All changes are in a single generator file plus one test update.

#### File 1: `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs`

| # | Location | Method | Fix Description |
|---|---|---|---|
| 1 | ~Line 2728-2742 | `RenderBackwardCompatibleTrackingProperties()` | Add `bool hasWhenChain` parameter; append When chain sum to TotalCallCount getter |
| 2 | ~Line 160 | Call site for above | Pass `hasWhenChain: canHaveWhenChain \|\| canHaveVoidWhenChain` |
| 3 | ~Line 2795-2818 | `RenderOverloadBackwardCompatibleProperties()` | Add per-overload When chain sum to TotalCallCount expression |
| 4 | ~Line 1325-1338 | Overload `CheckVerification()` local count | Add When chain count to `countParts` for eligible overloads |
| 5 | ~Line 1369-1381 | Overload `CheckVerificationAll()` condition guard | Add When chain to `condParts` for eligible overloads |
| 6 | ~Line 1386-1396 | Overload `CheckVerificationAll()` local count | Add When chain count to `countParts` for eligible overloads |

**When chain eligibility check (same pattern used throughout the file):**
```csharp
var hasRefOrOut = HasRefOrOutParameters(overload.Parameters);
var canHaveWhenChainForOverload = !overload.IsVoid && overload.Parameters.Count > 0 && !hasRefOrOut;
var canHaveVoidWhenChainForOverload = overload.IsVoid && overload.Parameters.Count > 0 && !hasRefOrOut;
```

Note: For locations 4, 5, and 6, the `hasRefOrOut` local variable and the `canHaveWhenChain*` checks already exist nearby in the same `foreach` loop body (at ~line 1400-1402 for `CheckVerificationAll` and ~line 1342-1344 for `CheckVerification`). The developer should either reuse those existing variables by hoisting the computation earlier in the loop, or compute them earlier where the `countParts` are built.

#### File 2: `src/Tests/KnockOffTests/UserMethodWhenTests.cs`

| Location | Description |
|---|---|
| Line 283-297 | Rename test, change assertion from `Times.Once` to `Times.Exactly(2)`, update comments |

#### Files NOT affected:
- **Single-sig `CheckVerificationAll()`** (line 1252-1274): Uses `TotalCallCount`. Fix at location 1 flows through.
- **Single-sig `CheckVerification()`** (line 1222-1249): Uses `TotalCallCount` (line 1233). Fix at location 1 flows through.
- **`Verify(Times)` on interceptor** (line 2836): Uses `TotalCallCount`. Fix at locations 1/3 flows through.
- **`InlineRenderer.cs` delegate stubs**: NOT buggy. `RecordCall()` before When chain check.
- **`InlineRenderer.cs` generic method handlers**: No When chain support.
- **Property/Indexer/Event interceptors**: When chains are method-only.
- **`FlatRenderer.cs`, `StandaloneClassRenderer.cs`, `ClassRenderer.cs`**: Use shared `MethodInterceptorRenderer.RenderInterceptorClass`.

### Pipeline Coverage

| Pipeline | Renderer | Has When Chain | Fix Location |
|---|---|---|---|
| Standalone Interface (1,2) | `FlatRenderer` -> shared `MethodInterceptorRenderer` | Yes (methods with params) | Locations 1-6 |
| Standalone Class (3,4) | `StandaloneClassRenderer` -> shared `MethodInterceptorRenderer` | Yes (methods with params) | Locations 1-6 |
| Inline Interface/Class (5,6) | shared `MethodInterceptorRenderer` for methods | Yes (methods with params) | Locations 1-6 |
| Inline Delegate (7) | `InlineRenderer` delegate code | Yes but NOT buggy | N/A |
| Open Generic Interface/Class (8,9) | `InlineRenderer` generic handler + shared `MethodInterceptorRenderer` for non-generic methods | Non-generic: Yes | Locations 1-6 |

### Edge Cases

1. **Only When chain configured, When chain invoked (single-sig)**: `TotalCallCount` now includes When chain `CallCount`. `CheckVerificationAll` passes.
2. **Only When chain configured, When chain invoked (overload-group)**: `condExpr` now true (location 5). Local `count` now includes When chain `CallCount` (location 6). Passes.
3. **Only When chain configured, NOT invoked**: `TotalCallCount`/count = 0. Correctly fails.
4. **Both When chain and OnCall configured, only When chain invoked (single-sig)**: `TotalCallCount` includes When chain. Passes.
5. **Both When chain and OnCall configured, only When chain invoked (overload-group)**: `condExpr` true (was already true via `_onCall`). Local `count` now includes When chain (location 6). Passes.
6. **When chain configured but no match (fell through to other handler)**: Matcher `CallCount` = 0. Other handler's tracking incremented. Correct.
7. **No When chain configured**: `_whenChain` is null. Null checks skip the sum. No behavior change.
8. **Reset()**: Already clears When chain `CallCount` values. After reset, all counts correctly return to 0.

---

## Implementation Steps

1. **Modify `RenderBackwardCompatibleTrackingProperties`** (~line 2728): Add `bool hasWhenChain = false` parameter. When true, append `if (_whenChain != null) foreach (var m in _whenChain) sum += m.CallCount;` before `return sum;`.

2. **Update call site** (~line 160): Pass `hasWhenChain: canHaveWhenChain || canHaveVoidWhenChain`.

3. **Modify `RenderOverloadBackwardCompatibleProperties`** (~line 2795): For each overload, check When chain eligibility. If eligible, add `(_whenChain_{suffix}?.Sum(m => m.CallCount) ?? 0)` to `sumParts`.

4. **Modify overload-group `CheckVerification()` local count** (~line 1325): For each overload in the verification loop, check When chain eligibility. If eligible, add `(_whenChain_{suffix}?.Sum(m => m.CallCount) ?? 0)` to `countParts`. Hoist the `hasRefOrOut` / `canHaveWhenChain*` computation from later in the loop body (~line 1342) to before the `countParts` construction.

5. **Modify overload-group `CheckVerificationAll()` condition guard** (~line 1369): For each overload in the verification loop, check When chain eligibility. If eligible, add `(_whenChain_{suffix}?.Count ?? 0) > 0` to `condParts`. This matches the existing `IsConfigured` property pattern (line 1306-1307).

6. **Modify overload-group `CheckVerificationAll()` local count** (~line 1386): Same as step 4 -- add When chain count to `countParts` for eligible overloads. Reuse the `hasRefOrOut` / `canHaveWhenChain*` variables from step 5.

7. **Update test** in `src/Tests/KnockOffTests/UserMethodWhenTests.cs`: Rename and update assertion.

8. **Rebuild and test**: Rebuild Design.Stubs, verify generated code, run all tests.

---

## Acceptance Criteria

- [ ] `SingleMatcher_VerifyAll_ShouldPassAfterMatcherInvoked` passes (existing Bug 1 test, pattern 5)
- [ ] `SingleSig_VerifyTimes_ShouldCountWhenChainCalls` passes (Bug 2 test, single-sig TotalCallCount)
- [ ] `Overload_VerifyTimes_ShouldCountWhenChainCalls` passes (Bug 2 test, overload-group TotalCallCount)
- [ ] `Overload_VerifyAll_ShouldCountWhenChainCalls` passes (Bug 2 test, overload-group CheckVerificationAll local count)
- [ ] `VoidOverload_VerifyTimes_ShouldCountWhenChainCalls` passes (Bug 2 test, void overload-group TotalCallCount)
- [ ] Updated test `Verify_WhenChainCallsIncludedInTotalCount` passes with `Times.Exactly(2)` (KnockOffTests)
- [ ] All existing tests pass
- [ ] Design.Stubs compiles
- [ ] Design.Tests: all tests pass (Bug 1 HEAD tests already pass; Bug 2 TotalCallCount tests must pass after fix)

---

## Dependencies

- The When chain verification HEAD bug (Bug 1) has already been fixed (see `docs/todos/completed/when-chain-verification-bug.md`). Bug 1 tests (scenarios 1-4) now PASS.
- This plan fixes Bug 2 (TotalCallCount) which causes 5 test failures: the 4 new Bug 2 tests plus `SingleMatcher_VerifyAll_ShouldPassAfterMatcherInvoked`.
- The Bug 2 tests are independent of the HEAD bug because they use `Verify(Times)` (which checks TotalCallCount directly) and `VerifyAll` (where Block 1 returns before Block 2's HEAD check runs).

---

## Risks / Considerations

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Overload group missing When chain eligibility check | Low | Medium | Use same `canHaveWhenChain*` pattern that already exists in the same loop body |
| Variable hoisting in verification loop | Low | Low | The `hasRefOrOut` / `canHaveWhenChain*` variables are already computed later in the same loop; just move them earlier |
| Inline delegate double-counting | None | N/A | Delegates use `RecordCall()` before When chain check, not the shared renderer |

---

## Architectural Verification

**Scope Table:**

| Pattern | Has When Chain | Affected | Fix Path |
|---|---|---|---|
| 1. Standalone | Yes (methods w/ params) | Yes | Shared MethodInterceptorRenderer (all 6 locations) |
| 2. Generic Standalone | Yes (methods w/ params) | Yes | Shared MethodInterceptorRenderer (all 6 locations) |
| 3. Standalone Class | Yes (methods w/ params) | Yes | Shared MethodInterceptorRenderer (all 6 locations) |
| 4. Generic Standalone Class | Yes (methods w/ params) | Yes | Shared MethodInterceptorRenderer (all 6 locations) |
| 5. Inline Interface | Yes (methods w/ params) | Yes | Shared MethodInterceptorRenderer (all 6 locations) |
| 6. Inline Class | Yes (methods w/ params) | Yes | Shared MethodInterceptorRenderer (all 6 locations) |
| 7. Inline Delegate | Yes, but NOT buggy | No | RecordCall before When chain check |
| 8. Open Generic Interface | Non-generic methods only | Yes (for non-generic methods) | Shared MethodInterceptorRenderer (all 6 locations) |
| 9. Open Generic Class | Non-generic methods only | Yes (for non-generic methods) | Shared MethodInterceptorRenderer (all 6 locations) |

**Design Project Verification:**

- Failing test (Bug 1 + TotalCallCount): `src/Design/Design.Tests/AdvancedTests/WhenChainVerificationBugTests.cs:194` - `SingleMatcher_VerifyAll_ShouldPassAfterMatcherInvoked`
  - Current error: `KnockOff.VerificationException : Verification failed: Add: expected AtLeastOnce, actual 0 calls`
  - Evidence: Test runs on pattern 5 (Inline Interface via `VerificationDemo` with `[KnockOff<ICalculator>]`)
  - Must pass after TotalCallCount fix (single-sig CheckVerificationAll uses TotalCallCount)

- Failing test (Bug 2, single-sig TotalCallCount): `src/Design/Design.Tests/AdvancedTests/WhenChainVerificationBugTests.cs` - `SingleSig_VerifyTimes_ShouldCountWhenChainCalls`
  - Current error: `Add: expected Twice, actual 1 calls`
  - Evidence: ICalculator (single-sig), When chain + Returns, Verify(Times.Exactly(2))
  - Exercises fix location 1 (single-sig TotalCallCount getter)

- Failing test (Bug 2, overload TotalCallCount): `src/Design/Design.Tests/AdvancedTests/WhenChainVerificationBugTests.cs` - `Overload_VerifyTimes_ShouldCountWhenChainCalls`
  - Current error: `Format: expected Twice, actual 1 calls`
  - Evidence: IFormatter (overload-group), When chain + OnCall on Format(string) overload, Verify(Times.Exactly(2))
  - Exercises fix location 3 (overload-group TotalCallCount expression)

- Failing test (Bug 2, overload VerifyAll count): `src/Design/Design.Tests/AdvancedTests/WhenChainVerificationBugTests.cs` - `Overload_VerifyAll_ShouldCountWhenChainCalls`
  - Current error: `Format: expected AtLeastOnce, actual 0 calls`
  - Evidence: IFormatter (overload-group), When chain + OnCall, only When chain invoked, VerifyAll()
  - Exercises fix locations 5 (condExpr) and 6 (overload CheckVerificationAll local count)

- Failing test (Bug 2, void overload TotalCallCount): `src/Design/Design.Tests/AdvancedTests/WhenChainVerificationBugTests.cs` - `VoidOverload_VerifyTimes_ShouldCountWhenChainCalls`
  - Current error: `Log: expected Twice, actual 1 calls`
  - Evidence: IFormatter (void overload-group), void When chain + OnCall on Log(string) overload, Verify(Times.Exactly(2))
  - Exercises fix location 3 for void overloads

- Existing test to update: `src/Tests/KnockOffTests/UserMethodWhenTests.cs:283` - `Verify_WhenChainCallsNotInTotalCount`
  - Currently asserts `Verify(Times.Once)` -- will fail after fix because TotalCallCount will be 2
  - Must be updated to `Verify(Times.Exactly(2))` and renamed

**Breaking Changes:** No. `TotalCallCount` is `private`. `CheckVerificationAll` and `CheckVerification` are `internal`. The only user-visible change is that `VerifyAll()` and `Verify()`/`Verify(Times)` correctly count When chain invocations.

**Codebase Analysis:**

Files examined:
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs`:
  - `RenderBackwardCompatibleTrackingProperties()` (line 2728-2743) -- single-sig TotalCallCount definition
  - Call site (line 160-162) -- passes no `hasWhenChain` currently
  - `canHaveWhenChain` / `canHaveVoidWhenChain` computed at line 120-121
  - `RenderOverloadBackwardCompatibleProperties()` (line 2795-2819) -- overload-group TotalCallCount expression
  - `CheckVerification()` single-sig (line 1222-1249) -- uses `TotalCallCount` at line 1233
  - `CheckVerificationAll()` single-sig (line 1252-1274) -- uses `TotalCallCount` at line 1258
  - `CheckVerification()` overload-group (line 1314-1359) -- local `countParts` at line 1325-1338, When chain check at 1342-1355
  - `CheckVerificationAll()` overload-group (line 1362-1417) -- `condParts` at 1369-1381, local `countParts` at 1386-1396, When chain check at 1400-1413
  - `IsConfigured` overload-group property (line 1285-1311) -- already includes When chain at line 1306-1307 (pattern to follow for `condExpr`)
  - `RenderWhenChainInvokeCheck()` (line 1061-1103) -- `matcher.CallCount++` at line 1080
  - `RenderVoidWhenChainInvokeCheck()` (line 2276-2318) -- `matcher.CallCount++` at line 2294
  - `RenderInvokeMethod()` (line 631-760+) -- When chain returns early before other tracking
  - `Verify(Times)` on interceptor (line 2836) -- uses `TotalCallCount`
- `src/Generator/Renderer/InlineRenderer.cs`:
  - Delegate Invoke (line 1411-1470) -- `RecordCall()` at line 1416 before When chain check (NOT buggy)
- `src/Generator/Renderer/FlatRenderer.cs` -- uses shared `MethodInterceptorRenderer.RenderInterceptorClass` (line 116, 132)
- `src/Generator/Renderer/StandaloneClassRenderer.cs` -- uses shared renderer
- `src/Generator/Renderer/ClassRenderer.cs` -- uses shared renderer (line 69)
- `src/Generator/Model/Shared/MethodOverloadSignature.cs` -- has `Parameters`, `IsVoid`, `ReturnType` for eligibility check
- `src/Tests/KnockOffTests/UserMethodWhenTests.cs` -- line 283, test to update
- `src/Design/Design.Tests/AdvancedTests/WhenChainVerificationBugTests.cs` -- line 194, failing test

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-02-05 (v2 review), 2026-02-05 (v3 re-review)

### My Understanding of This Plan

**Core Change:** Include When chain matcher `CallCount` values in all five call-count computation locations so that `VerifyAll()` and `Verify(Times)` correctly count When chain invocations for both single-signature and overload-group interceptors.
**User-Facing API:** No API changes. `VerifyAll()` will now correctly pass when a When chain is the only configuration and has been invoked. `Verify(Times)` will count When chain invocations.
**Internal Changes:** Six modifications to `MethodInterceptorRenderer.cs` (5 count locations + 1 call site), plus one test update.
**Patterns Affected:** All patterns that use the shared `MethodInterceptorRenderer` (1-6, 8-9 for non-generic methods). Pattern 7 (delegate) is not affected.

### Codebase Investigation

**Files Examined (v3 re-review):**
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` lines 100-163 - Confirmed `canHaveWhenChain` (line 120) and `canHaveVoidWhenChain` (line 121) booleans exist. Confirmed call site at line 160-162 does NOT currently pass a `hasWhenChain` parameter.
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` lines 2728-2742 - Confirmed `RenderBackwardCompatibleTrackingProperties` signature has no `hasWhenChain` parameter. `TotalCallCount` at line 2742 sums everything except When chain.
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` lines 2795-2819 - Confirmed overload `TotalCallCount` expression at line 2818 does NOT include When chain sum.
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` lines 1314-1359 - Confirmed overload `CheckVerification()` local `countParts` (line 1325-1338) does NOT include When chain. `canHaveWhenChain*` variables at lines 1342-1344 need hoisting.
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` lines 1362-1417 - Confirmed overload `CheckVerificationAll()`: `condParts` (line 1369-1381) does NOT include When chain. `countParts` (line 1386-1396) does NOT include When chain. `canHaveWhenChain*` variables at lines 1400-1402 need hoisting.
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` lines 1285-1311 - Confirmed `IsConfigured` property includes When chain at lines 1306-1307 (pattern for `condExpr` fix).
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` lines 1252-1274 - Confirmed single-sig `CheckVerificationAll()` uses `TotalCallCount` at line 1258. Fix flows through.
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` lines 1222-1249 - Confirmed single-sig `CheckVerification()` uses `TotalCallCount` at line 1233. Fix flows through.
- `src/Tests/KnockOffTests/UserMethodWhenTests.cs` lines 282-297 - Confirmed test exists at line 283, asserts `Times.Once`.
- `src/Design/Design.Tests/AdvancedTests/WhenChainVerificationBugTests.cs` - All 5 failing tests confirmed present and failing with expected errors.

**Searches Performed:**
- Searched for `hasWhenChain` in MethodInterceptorRenderer.cs -- found at lines 343, 349, 1109, 1141, 1198, 1215, 1226, 1236, 1261. Exists on `RenderResetMethod` and `RenderInternalVerificationMembers` but NOT on `RenderBackwardCompatibleTrackingProperties`.
- Searched for `RenderOverloadBackwardCompatibleProperties` -- call site at line 465, definition at line 2795.
- Searched for `TotalCallCount` -- all references accounted for.

**Design.Stubs Verification (all 5 failing tests confirmed by running them):**
- `SingleMatcher_VerifyAll_ShouldPassAfterMatcherInvoked` -- `"Add: expected AtLeastOnce, actual 0 calls"` (confirmed)
- `SingleSig_VerifyTimes_ShouldCountWhenChainCalls` -- `"Add: expected Twice, actual 1 calls"` (confirmed)
- `Overload_VerifyTimes_ShouldCountWhenChainCalls` -- `"Format: expected Twice, actual 1 calls"` (confirmed)
- `Overload_VerifyAll_ShouldCountWhenChainCalls` -- `"Format: expected AtLeastOnce, actual 0 calls"` (confirmed)
- `VoidOverload_VerifyTimes_ShouldCountWhenChainCalls` -- `"Log: expected Twice, actual 1 calls"` (confirmed)

**Previous Concern Resolution:**
- v2 concern (overload-path local counts not using TotalCallCount) is fully addressed in v3 with fix locations 4, 5, and 6. All cited line numbers verified accurate.

### What Looks Good

- All six fix locations verified against actual source code with accurate line numbers.
- Five failing acceptance tests compile and fail with expected error messages.
- The `condExpr` fix (location 5) correctly follows the `IsConfigured` property pattern at lines 1306-1307.
- Variable hoisting guidance is practical and leaves appropriate flexibility.
- Pipeline coverage analysis is correct: all non-delegate patterns route through the shared renderer.
- Edge cases well-considered (no-param methods, ref/out, delegates, Reset).
- Test update is straightforward and correctly identified.

### Recommendation

**Approved. Proceed to implementation.**

---

## Implementation Contract

**Created:** 2026-02-05
**Approved by:** knockoff-developer

### Design Project Acceptance Criteria

These are the failing Design.Tests that must pass after implementation. All 5 confirmed failing as of 2026-02-05.

- [ ] `SingleMatcher_VerifyAll_ShouldPassAfterMatcherInvoked` -- `WhenChainVerificationBugTests.cs:209` -- single-sig VerifyAll, pattern 5 (exercises fix location 1 via TotalCallCount)
- [ ] `SingleSig_VerifyTimes_ShouldCountWhenChainCalls` -- `WhenChainVerificationBugTests.cs:292` -- single-sig Verify(Times), pattern 5 (exercises fix location 1)
- [ ] `Overload_VerifyTimes_ShouldCountWhenChainCalls` -- `WhenChainVerificationBugTests.cs:336` -- overload Verify(Times), pattern 5 via IFormatter (exercises fix location 3)
- [ ] `Overload_VerifyAll_ShouldCountWhenChainCalls` -- `WhenChainVerificationBugTests.cs:389` -- overload VerifyAll, pattern 5 via IFormatter (exercises fix locations 5 and 6)
- [ ] `VoidOverload_VerifyTimes_ShouldCountWhenChainCalls` -- `WhenChainVerificationBugTests.cs:427` -- void overload Verify(Times), pattern 5 via IFormatter (exercises fix location 3 for void)

### In Scope

All changes in a single file (`src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs`) plus one test update:

- [ ] **Location 1** (~line 2728): Add `bool hasWhenChain = false` parameter to `RenderBackwardCompatibleTrackingProperties`. When true, append `if (_whenChain != null) foreach (var m in _whenChain) sum += m.CallCount;` before `return sum;` in the TotalCallCount getter.
- [ ] **Location 2** (~line 160): Update call site to pass `hasWhenChain: canHaveWhenChain || canHaveVoidWhenChain`.
- [ ] **Location 3** (~line 2795-2818): In `RenderOverloadBackwardCompatibleProperties`, for each overload, check When chain eligibility. If eligible, add `(_whenChain_{suffix}?.Sum(m => m.CallCount) ?? 0)` to `sumParts`.
- [ ] **Location 4** (~line 1325-1338): In overload `CheckVerification()`, hoist `hasRefOrOut` / `canHaveWhenChain*` computation from lines 1342-1344 to before `countParts` construction. If eligible, add `(_whenChain_{suffix}?.Sum(m => m.CallCount) ?? 0)` to `countParts`.
- [ ] **Location 5** (~line 1369-1381): In overload `CheckVerificationAll()`, hoist `hasRefOrOut` / `canHaveWhenChain*` computation from lines 1400-1402 to before `condParts` construction. If eligible, add `(_whenChain_{suffix}?.Count ?? 0) > 0` to `condParts` (matching `IsConfigured` pattern at lines 1306-1307).
- [ ] **Location 6** (~line 1386-1396): In overload `CheckVerificationAll()`, if eligible (using variables from location 5), add `(_whenChain_{suffix}?.Sum(m => m.CallCount) ?? 0)` to `countParts`.
- [ ] **Test update**: `src/Tests/KnockOffTests/UserMethodWhenTests.cs` line 283: Rename `Verify_WhenChainCallsNotInTotalCount` to `Verify_WhenChainCallsIncludedInTotalCount`, change assertion from `Times.Once` to `Times.Exactly(2)`, update comments.
- [ ] **Checkpoint**: Rebuild Design.Stubs, verify generated TotalCallCount includes When chain sum
- [ ] **Checkpoint**: Run all tests (Design.Tests + KnockOffTests)

### Explicitly Out of Scope

- Single-sig `CheckVerificationAll()` (line 1252-1274) -- uses `TotalCallCount`, fix at location 1 flows through
- Single-sig `CheckVerification()` (line 1222-1249) -- uses `TotalCallCount` at line 1233, fix flows through
- `Verify(Times)` on interceptor (line 2836) -- uses `TotalCallCount`, fix flows through
- When chain invoke path (`matcher.CallCount++` at lines 1080, 2294) -- no changes needed
- Inline delegate stubs (`InlineRenderer.cs`) -- not buggy, `RecordCall()` before When chain check
- When chain HEAD verification condition (`CallCount == 0` check) -- tracked in separate todo
- `FlatRenderer.cs`, `StandaloneClassRenderer.cs`, `ClassRenderer.cs` -- use shared renderer, no direct changes
- Adding new tests beyond the 5 acceptance criteria tests

### Verification Gates

1. **After locations 1-3**: Rebuild generator, rebuild Design.Stubs. Verify generated `TotalCallCount` in `VerificationDemo.Stubs.g.cs` includes `_whenChain` sum. Verify `MethodOverloadsDemo.Stubs.g.cs` overload TotalCallCount includes `_whenChain_{suffix}` sum.
2. **After locations 4-6**: Rebuild generator, rebuild Design.Stubs. Verify generated `CheckVerification()` and `CheckVerificationAll()` in `MethodOverloadsDemo.Stubs.g.cs` include When chain in `countParts` and `condParts`.
3. **After test update**: Run `UserMethodWhenTests.Verify_WhenChainCallsIncludedInTotalCount` -- must pass with `Times.Exactly(2)`.
4. **Final**: All 5 acceptance criteria tests pass. All existing tests pass. `dotnet build src/Design/Design.Stubs` succeeds.

### Stop Conditions

If any of these occur, STOP and report:
- Out-of-scope test fails (any test other than the 5 acceptance criteria tests and `Verify_WhenChainCallsNotInTotalCount`)
- Generated code does not compile
- Architectural contradiction discovered (e.g., When chain count is already included somewhere causing double-counting)

---

## Implementation Progress

**Started:** 2026-02-05
**Developer:** knockoff-developer

**Phase 1:** Modify TotalCallCount and verification count computations
- [x] Location 1: Added `bool hasWhenChain = false` parameter to `RenderBackwardCompatibleTrackingProperties` (line 2735). When true, appends `if (_whenChain != null) foreach (var m in _whenChain) sum += m.CallCount;` before `return sum;` in the TotalCallCount getter.
- [x] Location 2: Updated call site (line 163) to pass `hasWhenChain: canHaveWhenChain || canHaveVoidWhenChain`.
- [x] Location 3: Added per-overload When chain sum `(_whenChain_{suffix}?.Sum(m => m.CallCount) ?? 0)` to `sumParts` in `RenderOverloadBackwardCompatibleProperties` (lines 2817-2820).
- [x] Location 4: Added When chain count to overload `CheckVerification()` `countParts` (lines 1338-1342). Computed `canHaveWhenChainForOverload` / `canHaveVoidWhenChainForOverload` using existing `hasRefOrOut` variable. Renamed the later When chain variables at line 1348-1350 to `*ForWhen` to avoid scope conflict.
- [x] Location 5: Added `(_whenChain_{suffix}?.Count ?? 0) > 0` to `condParts` in overload `CheckVerificationAll()` (lines 1390-1393), matching the `IsConfigured` property pattern.
- [x] Location 6: Added `(_whenChain_{suffix}?.Sum(m => m.CallCount) ?? 0)` to `countParts` in overload `CheckVerificationAll()` (lines 1403-1404), reusing the variables from Location 5.
- [x] Updated test: Renamed `Verify_WhenChainCallsNotInTotalCount` to `Verify_WhenChainCallsIncludedInTotalCount`, changed assertion from `Times.Once` to `Times.Exactly(2)`, updated comments.
- [x] **Verification**: Rebuilt Design.Stubs (success), verified generated TotalCallCount includes When chain sum, ran all tests.

**Verification Gate 1 (After locations 1-3):** Generated `VerificationDemo.Stubs.g.cs` line 44 shows `if (_whenChain != null) foreach (var m in _whenChain) sum += m.CallCount;` in single-sig TotalCallCount. Generated `MethodOverloadsDemo.Stubs.g.cs` line 73 shows `(_whenChain_{suffix}?.Sum(m => m.CallCount) ?? 0)` per eligible overload in overload-group TotalCallCount. Methods without parameters (e.g., Reset) correctly exclude When chain sum.

**Verification Gate 2 (After locations 4-6):** Generated `MethodOverloadsDemo.Stubs.g.cs` line 452 shows `condExpr` includes `(_whenChain_String_String?.Count ?? 0) > 0`. Line 454 shows `countParts` includes `(_whenChain_String_String?.Sum(m => m.CallCount) ?? 0)`. Line 407 shows `CheckVerification()` countParts include When chain sum.

**Verification Gate 3 (Test update):** `Verify_WhenChainCallsIncludedInTotalCount` passes with `Times.Exactly(2)` across all 3 target frameworks.

**Verification Gate 4 (Final):** All 5 acceptance criteria tests pass. All existing tests pass (see Completion Evidence).

---

## Completion Evidence

**Reported:** 2026-02-05

- **Tests Passing:**
  - KnockOffTests: 3476 passed, 0 failed (1159 + 1159 + 1158 across net8.0/net9.0/net10.0)
  - Design.Tests: 690 passed, 0 failed (230 x 3 frameworks)
  - Documentation.Samples: 456 passed, 1 failed (across each framework)
    - The 1 failure is `UpdateTest_KnockOff` in `ReadMeUseCase.cs` -- this is a pre-existing failure on the `readme` branch (verified by stashing my changes and retesting: without my changes, BOTH `UpdateTest_KnockOff` AND `UpdateTest_KnockOff_OnCall` fail; WITH my changes, only `UpdateTest_KnockOff` fails -- my fix actually resolved one of the two pre-existing failures). This failure is unrelated to the TotalCallCount bug fix; the test calls `GetUser` twice but asserts `Times.Once`.
  - NeatooInterfaceTests: 14 passed, 0 failed (x3 frameworks)
  - AssemblyStrict: 473 passed, 0 failed (x3 frameworks)
- **Design Projects Compile:** Yes. `dotnet build src/Design/Design.Stubs` succeeds on all 3 frameworks.
- **All Contract Items:** Confirmed 100% complete. All 7 in-scope items checked off.
- **Documentation Updated:** N/A (no documentation changes in scope)

---

## Architect Verification

**Verified:** 2026-02-05
**Verdict:** VERIFIED

**Independent test results:**
- Design.Stubs: Build succeeded (0 warnings, 0 errors, all 3 frameworks)
- Design.Tests: 690 passed, 0 failed (230 x 3 frameworks)
- Generator: Build succeeded (0 warnings, 0 errors)
- KnockOff Library: Build succeeded (0 warnings, 0 errors, all 3 frameworks)
- Documentation.Samples: Build succeeded; 456 passed, 1 failed per framework (3 x 1 failure)
  - Failure: `UpdateTest_KnockOff` -- pre-existing on `readme` branch, confirmed by stashing developer changes and retesting (same failure without changes). The test calls `GetUser` twice but asserts `Times.Once`. Not related to When chain or TotalCallCount.
- KnockOffTests: 3476 passed, 0 failed (1159 + 1159 + 1158 across net8.0/net9.0/net10.0)
- NeatooInterfaceTests: 1419 passed, 0 failed (473 x 3 frameworks)
- AssemblyStrict: 42 passed, 0 failed (14 x 3 frameworks)
- **All 5 acceptance criteria tests:** 15 passed, 0 failed (5 x 3 frameworks)
- **Updated test** `Verify_WhenChainCallsIncludedInTotalCount`: 3 passed, 0 failed (1 x 3 frameworks)

**Design match:** Yes. All 6 fix locations in `MethodInterceptorRenderer.cs` match the plan:
- Location 1 (single-sig TotalCallCount): Added `bool hasWhenChain = false` parameter, appends When chain sum to getter. Matches.
- Location 2 (call site): Passes `hasWhenChain: canHaveWhenChain || canHaveVoidWhenChain`. Matches.
- Location 3 (overload TotalCallCount): Adds per-overload When chain sum to `sumParts` with eligibility check. Matches.
- Location 4 (overload CheckVerification count): Adds When chain count to `countParts`. Developer renamed later variables to `*ForWhen` instead of hoisting (equivalent approach). Matches.
- Location 5 (overload CheckVerificationAll condExpr): Adds `(_whenChain_{suffix}?.Count ?? 0) > 0` to `condParts`, matching `IsConfigured` pattern. Matches.
- Location 6 (overload CheckVerificationAll count): Adds When chain count to `countParts`, reuses variables from Location 5. Matches.
- Test update: Renamed, assertion changed from `Times.Once` to `Times.Exactly(2)`, comments updated. Matches.

**Issues found:** None. The diff also includes the previously completed When chain HEAD verification bug fix (`CallCount == 0` checks in 6 locations), which is expected since both fixes are uncommitted on this branch.
