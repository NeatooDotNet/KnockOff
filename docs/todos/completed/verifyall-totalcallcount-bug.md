# VerifyAll TotalCallCount Does Not Count When Chain Invocations

**Status:** Complete
**Priority:** High
**Created:** 2026-02-05
**Last Updated:** 2026-02-05
**Plan:** [Fix VerifyAll TotalCallCount for When Chain Invocations](../plans/completed/verifyall-totalcallcount-fix.md)

---

## Problem

`VerifyAll()` fails when only a When chain is configured (no OnCall, ReturnsValue, or Sequence) because `TotalCallCount` does not include When chain invocations. The `CheckVerificationAll()` method checks `TotalCallCount >= 1` before checking the When chain condition, so it reports "expected AtLeastOnce, actual 0 calls" even though the When chain was successfully invoked.

**Root cause:** When chain matcher invocation increments `CallCount` on the matcher but does NOT increment the interceptor's `TotalCallCount`. The `TotalCallCount` is only incremented in the non-When-chain invoke path.

**Reproduction:**

```csharp
stub.Add.When(1, 2).Returns(3).Verifiable();
stub.Add(1, 2); // When chain invoked, CallCount=1, but TotalCallCount=0
stub.VerifyAll(); // Throws: "Add: expected AtLeastOnce, actual 0 calls"
```

**Discovered in:** `SingleMatcher_VerifyAll_ShouldPassAfterMatcherInvoked` test in `WhenChainVerificationBugTests.cs` during the When chain verification bug fix.

**Affects:** All 9 patterns that support When chains and VerifyAll.

## Solution

Fix the generated code to increment `TotalCallCount` when a When chain matcher is invoked, OR change `CheckVerificationAll()` to consider When chain `CallCount` when evaluating the minimum call count check.

---

## Plans

- [Fix VerifyAll TotalCallCount for When Chain Invocations](../plans/completed/verifyall-totalcallcount-fix.md)

---

## Tasks

- [x] Architect investigates and creates implementation plan
- [x] Create failing design tests as acceptance criteria for overload-path bug
- [x] Fix generator logic
- [x] Verify all existing tests still pass
- [x] Verify `SingleMatcher_VerifyAll_ShouldPassAfterMatcherInvoked` passes
- [x] Verify all 4 new Bug 2 design tests pass

---

## Progress Log

**2026-02-05**: Created todo. Bug discovered during the When chain verification bug fix (see `docs/todos/completed/when-chain-verification-bug.md`). The design test `SingleMatcher_VerifyAll_ShouldPassAfterMatcherInvoked` in `src/Design/Design.Tests/AdvancedTests/WhenChainVerificationBugTests.cs` fails with `"Add: expected AtLeastOnce, actual 0 calls"`. The When chain verification condition fix (CallCount == 0) is correct but unreachable because `CheckVerificationAll()` fails earlier at the `TotalCallCount >= 1` check.

**2026-02-05**: Architect investigation complete. Created plan at `docs/plans/verifyall-totalcallcount-fix.md`. Initial approach was to compute "effective call count" locally in `CheckVerificationAll()` to avoid modifying `TotalCallCount` (due to existing test `Verify_WhenChainCallsNotInTotalCount`).

**2026-02-05**: Plan revised per user feedback. The previous approach was preserving tech debt, not intentional design. `TotalCallCount` was once public API, so the test protected that contract. But `TotalCallCount` is now `private`, so there is no compatibility concern. Revised approach: include When chain matcher `CallCount` values in the `TotalCallCount` property getter. This is simpler (change the property, not the consumers), cleaner (TotalCallCount reflects reality), and consistent (matches how the inline delegate path already works). The test `Verify_WhenChainCallsNotInTotalCount` will be updated to reflect the corrected behavior.

**2026-02-05**: Developer review raised concern: overload-group `CheckVerification()` and `CheckVerificationAll()` compute local per-overload count variables that do NOT reference `TotalCallCount`. Fixing `TotalCallCount` alone would not cover overloaded methods. Plan revised (v3) to address all five count computation locations: (1) single-sig TotalCallCount, (2) overload-group TotalCallCount, (3) overload CheckVerification local count, (4) overload CheckVerificationAll condition guard, (5) overload CheckVerificationAll local count. The condition guard fix also aligns with the existing `IsConfigured` property pattern. Plan returned to Draft (Architect) for developer re-review.

**2026-02-05**: Created 4 failing design tests as acceptance criteria for the TotalCallCount bug (Bug 2). Tests added to `WhenChainVerificationBugTests.cs` covering: (1) single-sig Verify(Times) missing When chain count, (2) overload-group Verify(Times) missing When chain count, (3) overload-group VerifyAll local count missing When chain count, (4) void overload-group Verify(Times) missing When chain count. All 4 tests compile and fail with expected error messages. Tests use `ICalculator` (single-sig, VerificationDemo) and `IFormatter` (overload-group, MethodOverloadsDemo). Plan acceptance criteria updated.

---

## Results / Conclusions

**Fixed.** When chain matcher `CallCount` values are now included in all call count computations (TotalCallCount property + overload-group local counts). 6 locations changed in `MethodInterceptorRenderer.cs`, 1 test updated. All 5 acceptance criteria tests pass. Architect verified independently on 2026-02-05.
