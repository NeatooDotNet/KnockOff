# When Chain Verification Bug

**Status:** Complete
**Priority:** High
**Created:** 2026-02-05
**Last Updated:** 2026-02-05

---

## Problem

When a When chain has a single non-terminal matcher (e.g., `.When(1).Returns(user1).Verifiable()`), calling `Stub.Verify()` always throws `VerificationException: sequence incomplete - 0 of 1 callbacks invoked`, even though the matcher was successfully invoked.

The root cause is in the generated `CheckVerification()` and `WhenChainImpl.Verify()` methods. The verification logic checks:

```csharp
if (head < count && !_whenChain[head].IsTerminal)
    return VerificationFailure.SequenceIncomplete(...)
```

But due to "repeat last" semantics, HEAD never advances past the last non-terminal matcher. For a single-matcher chain: head=0, count=1, so `0 < 1 && !false` is always true, and verification always fails.

**Discovered in:** `UpdateTest_KnockOff_OnCall` test in `ReadMeUseCase.cs`

**Reproduction:**

```csharp
myRepoKO.GetUser.When(1).Returns(user1).Verifiable();
// ... call GetUser(1) multiple times ...
myRepoKO.Verify(); // Throws: "GetUser When chain: sequence incomplete - 0 of 1 callbacks invoked"
```

## Solution

Fix the When chain verification condition in the generator to account for "repeat last" semantics. The check should consider a chain "consumed" when HEAD is at the last position and that matcher has been called at least once (or when HEAD has reached a terminal matcher or advanced past the chain).

**Affected generator locations (10 total across 3 files):**

1. `WhenChainRenderer.cs` - `RenderWhenChainImpl()` Verify (non-void)
2. `WhenChainRenderer.cs` - `RenderVoidWhenChainImpl()` Verify (void)
3. `MethodInterceptorRenderer.cs` - `CheckVerification()` single-signature
4. `MethodInterceptorRenderer.cs` - `CheckVerificationAll()` single-signature
5. `MethodInterceptorRenderer.cs` - `CheckVerification()` multi-overload
6. `MethodInterceptorRenderer.cs` - `CheckVerificationAll()` multi-overload
7. `MethodInterceptorRenderer.cs` - Private `RenderWhenChainImpl()` Verify (non-void)
8. `MethodInterceptorRenderer.cs` - Private `RenderVoidWhenChainImpl()` Verify (void)
9. `InlineRenderer.cs` - Delegate WhenChain Verify (non-void)
10. `InlineRenderer.cs` - Delegate VoidWhenChain Verify (void)

---

## Plans

- [When Chain Verification Bug Fix](../plans/when-chain-verification-fix.md)

---

## Tasks

- [x] Architect confirms bug and creates failing design tests
- [x] Create implementation plan
- [x] Fix generator verification logic
- [x] Verify all existing tests still pass
- [x] Verify `UpdateTest_KnockOff_OnCall` passes

---

## Progress Log

**2026-02-05**: Created todo. Bug discovered while investigating `UpdateTest_KnockOff_OnCall` failure. Root cause traced to When chain verification logic in generator output.

**2026-02-05 (architect investigation)**: Bug confirmed via code trace and live test execution. The `UpdateTest_KnockOff_OnCall` test in `ReadMeUseCase.cs` fails with: `"GetUser When chain: sequence incomplete - 0 of 1 callbacks invoked"`. Root cause verified in four generator locations. Created 7 failing design tests in `src/Design/Design.Tests/AdvancedTests/WhenChainVerificationBugTests.cs` covering: single matcher with Verifiable(), single matcher with chain.Verify(), multiple non-terminal matchers, repeat-last semantics, VerifyAll(), plus a passing contrast test for terminal chains. Note: Design.Tests project has pre-existing compilation errors in other files (LastCallArgs, LastCallArg, OnCall_NoParams_T) that prevent running the full test suite, but the new test file compiles cleanly (no errors referencing WhenChainVerificationBugTests).

**2026-02-05 (implementation plan)**: Created implementation plan at `docs/plans/when-chain-verification-fix.md`. Deep codebase analysis revealed 10 affected locations (not 4 as originally estimated) across 3 files: WhenChainRenderer.cs (2), MethodInterceptorRenderer.cs (6), InlineRenderer.cs (2). The fix is to append `&& ...CallCount == 0` to the verification condition at each location, leveraging the existing `CallCount` property on matcher classes. All nine KnockOff patterns are affected equally.

---

## Results / Conclusions

**Fixed.** Appended `&& ...CallCount == 0` to the When chain verification condition at all 10 locations across 3 generator files (`WhenChainRenderer.cs`, `MethodInterceptorRenderer.cs`, `InlineRenderer.cs`). The fix correctly accounts for "repeat last" semantics — when HEAD is at the last non-terminal matcher and it has been invoked (`CallCount > 0`), the chain is considered "consumed" and verification passes.

**5 previously-failing tests now pass**, including the original reproduction case `UpdateTest_KnockOff_OnCall`. No regressions introduced.

**Related issue discovered:** `SingleMatcher_VerifyAll_ShouldPassAfterMatcherInvoked` still fails because `TotalCallCount` doesn't count When chain invocations. This is a separate bug — `VerifyAll()` fails at the `TotalCallCount >= 1` check before reaching the When chain condition. Tracked as a new todo.
