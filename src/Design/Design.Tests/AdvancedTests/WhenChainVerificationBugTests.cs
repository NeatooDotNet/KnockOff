// -----------------------------------------------------------------------------
// Design.Tests - When Chain Verification Bug Tests
// -----------------------------------------------------------------------------
// This file contains tests for TWO related When chain verification bugs:
//
// BUG 1 (HEAD Bug): When a When chain has a single non-terminal matcher (e.g.,
//   .When(1, 2).Return(100).Verifiable()),
// calling Stub.Verify() always throws "sequence incomplete" even though
// the matcher was successfully invoked.
//
// ROOT CAUSE: The generated verification logic checks:
//   if (head < count && !_whenChain[head].IsTerminal)
//       return VerificationFailure.SequenceIncomplete(...)
//
// But "repeat last" semantics mean HEAD never advances past the last
// non-terminal matcher. For a single-matcher chain:
//   head=0, count=1, so 0 < 1 && !false => always true => always fails.
//
// For a multi-matcher chain ending in a non-terminal matcher:
//   After consuming all matchers, HEAD stays at the last index (repeat last).
//   head=N-1, count=N, so (N-1) < N && !false => always true => always fails.
//
// The bug affects four generated code locations:
//   1. WhenChainImpl.Verify() (non-void return)
//   2. VoidWhenChainImpl.Verify() (void return)
//   3. CheckVerification() in method interceptors (Stub.Verify path)
//   4. CheckVerificationAll() in method interceptors (Stub.VerifyAll path)
//
// BUG 2 (TotalCallCount Bug): When chain invocations are not counted in
//   TotalCallCount or the per-overload local count variables.
//
// ROOT CAUSE: The When chain invoke path does matcher.CallCount++ then
//   returns early, bypassing all TotalCallCount component increments.
//   The TotalCallCount property and overload-group local counts do not
//   sum When chain matcher CallCount values. This means:
//   - stub.Method.Verify(Times) undercounts (missing When chain calls)
//   - Overload CheckVerificationAll() condition guard skips When-chain-only
//     overloads (condExpr doesn't include When chain)
//   - Overload CheckVerificationAll() local count excludes When chain calls
//
// All tests in this file are expected to FAIL until the respective bugs
// are fixed. Each test documents what SHOULD happen vs what DOES happen today.
// -----------------------------------------------------------------------------

using Design.Domain.Services;
using Design.Stubs.Advanced;
using Design.Stubs.Methods;
using KnockOff;

namespace Design.Tests.AdvancedTests;

/// <summary>
/// Tests demonstrating When chain verification bugs.
/// All tests in this class are expected to FAIL until the bugs are fixed.
/// </summary>
public class WhenChainVerificationBugTests
{
    // =========================================================================
    // Bug Scenario 1: Single non-terminal matcher with Verifiable()
    // =========================================================================
    // This is the original reproduction case from ReadMeUseCase.cs.
    //
    // WHAT SHOULD HAPPEN: After calling Add(1, 2) which matches the When(1, 2)
    // matcher, Stub.Verify() should pass because the single matcher was invoked
    // and it is the only matcher in the chain.
    //
    // WHAT ACTUALLY HAPPENS: Stub.Verify() always throws:
    //   "Add When chain: sequence incomplete - 0 of 1 callbacks invoked"
    //
    // WHY: HEAD stays at 0 due to "repeat last" semantics (single matcher =
    // last matcher, so HEAD never advances). CheckVerification() checks
    // head(0) < count(1) && !IsTerminal(false) => true => always fails.
    // =========================================================================

    [Fact]
    public void SingleMatcher_Verifiable_ShouldPassAfterMatcherInvoked()
    {
        var stub = new VerificationDemo.Stubs.ICalculator();

        // Configure a single When matcher and mark verifiable
        stub.Add.When(1, 2).Return(100).Verifiable();

        ICalculator calc = stub;

        // Invoke the method with matching arguments
        var result = calc.Add(1, 2);
        Assert.Equal(100, result);

        // BUG: This should pass because the matcher was invoked,
        // but it throws VerificationException due to "repeat last" HEAD semantics.
        stub.Verify();
    }

    // =========================================================================
    // Bug Scenario 2: Single non-terminal matcher with chain.Verify()
    // =========================================================================
    // Same bug, but exercising the WhenChain.Verify() method directly instead
    // of going through Stub.Verify()/CheckVerification().
    //
    // WHAT SHOULD HAPPEN: chain.Verify() should pass because the single matcher
    // was invoked.
    //
    // WHAT ACTUALLY HAPPENS: chain.Verify() throws:
    //   "When chain: sequence incomplete - 0 of 1 callbacks invoked"
    // =========================================================================

    [Fact]
    public void SingleMatcher_ChainVerify_ShouldPassAfterMatcherInvoked()
    {
        var stub = new VerificationDemo.Stubs.ICalculator();

        // Configure a single When matcher
        var chain = stub.Add.When(1, 2).Return(100);

        ICalculator calc = stub;

        // Invoke the method with matching arguments
        var result = calc.Add(1, 2);
        Assert.Equal(100, result);

        // BUG: This should pass because the matcher was invoked,
        // but it throws VerificationException.
        chain.Verify();
    }

    // =========================================================================
    // Bug Scenario 3: Multiple non-terminal matchers, all consumed
    // =========================================================================
    // When a chain has multiple non-terminal matchers (e.g., When().ThenWhen())
    // and all are consumed, HEAD ends up pointing at the last non-terminal matcher
    // because "repeat last" prevents advancing past it.
    //
    // WHAT SHOULD HAPPEN: After invoking all matchers in sequence, chain.Verify()
    // should pass because the chain was fully consumed.
    //
    // WHAT ACTUALLY HAPPENS: chain.Verify() throws because HEAD is at the last
    // non-terminal matcher position (head = count - 1), and the check
    // (head < count && !IsTerminal) evaluates to true.
    // =========================================================================

    [Fact]
    public void MultipleNonTerminalMatchers_AllConsumed_ChainVerifyShouldPass()
    {
        var stub = new VerificationDemo.Stubs.ICalculator();

        // Chain with two non-terminal matchers (no terminal at end)
        var chain = stub.Add
            .When(1, 2).Return(10)
            .ThenWhen(3, 4).Return(20);

        ICalculator calc = stub;

        // Consume both matchers in sequence
        Assert.Equal(10, calc.Add(1, 2));
        Assert.Equal(20, calc.Add(3, 4));

        // BUG: This should pass because all matchers were consumed,
        // but it throws because HEAD stays at the last non-terminal matcher.
        chain.Verify();
    }

    // =========================================================================
    // Bug Scenario 4: Single non-terminal matcher invoked multiple times
    // (repeat last semantics)
    // =========================================================================
    // The "repeat last" behavior means a single non-terminal matcher repeats
    // indefinitely. After calling it multiple times, verification should still
    // pass because the matcher WAS invoked.
    //
    // WHAT SHOULD HAPPEN: chain.Verify() should pass because the matcher was
    // invoked (multiple times due to repeat-last).
    //
    // WHAT ACTUALLY HAPPENS: chain.Verify() throws because HEAD=0, count=1.
    // =========================================================================

    [Fact]
    public void SingleMatcher_InvokedMultipleTimes_ChainVerifyShouldPass()
    {
        var stub = new VerificationDemo.Stubs.ICalculator();

        var chain = stub.Add.When(1, 2).Return(100);

        ICalculator calc = stub;

        // Call multiple times (repeat-last behavior)
        Assert.Equal(100, calc.Add(1, 2));
        Assert.Equal(100, calc.Add(1, 2));
        Assert.Equal(100, calc.Add(1, 2));

        // BUG: Should pass - matcher was invoked 3 times.
        chain.Verify();
    }

    // =========================================================================
    // Bug Scenario 5: VerifyAll() also affected
    // =========================================================================
    // CheckVerificationAll() has the same buggy condition as CheckVerification().
    // When a When chain is configured and all matchers are consumed,
    // VerifyAll() incorrectly reports the chain as incomplete.
    //
    // WHAT SHOULD HAPPEN: VerifyAll() should pass because the method was called
    // and the When chain was fully consumed.
    //
    // WHAT ACTUALLY HAPPENS: VerifyAll() throws due to the same
    // head < count && !IsTerminal condition.
    // =========================================================================

    [Fact]
    public void SingleMatcher_VerifyAll_ShouldPassAfterMatcherInvoked()
    {
        var stub = new VerificationDemo.Stubs.ICalculator();

        // Configure a When chain (making it "configured" for VerifyAll)
        stub.Add.When(1, 2).Return(100);

        ICalculator calc = stub;

        // Invoke with matching arguments
        calc.Add(1, 2);

        // BUG: This should pass because the method was called and the When chain
        // matcher was invoked, but VerifyAll() fails on the When chain check.
        stub.VerifyAll();
    }

    // =========================================================================
    // Contrast: Terminal chain (ThenCall) does NOT have this bug
    // =========================================================================
    // This test demonstrates that chains ending with a terminal matcher (ThenCall
    // or ThenNone) verify correctly, because HEAD advances past the last
    // non-terminal to the terminal, and the check finds IsTerminal=true.
    //
    // This test should PASS even before the fix.
    // =========================================================================

    [Fact]
    public void TerminalChain_Verify_PassesCorrectly()
    {
        var stub = new VerificationDemo.Stubs.ICalculator();

        // Chain ending with terminal matcher (ThenCall)
        var chain = stub.Add
            .When(1, 2).Return(10)
            .ThenCall((a, b) => a + b);

        ICalculator calc = stub;

        // Consume first matcher, then fall to terminal
        Assert.Equal(10, calc.Add(1, 2));
        Assert.Equal(7, calc.Add(3, 4)); // Terminal: 3 + 4 = 7

        // This should pass because HEAD advanced to the terminal matcher.
        // IsTerminal=true causes the verification check to pass.
        chain.Verify();
    }

    // =========================================================================
    // BUG 2: TotalCallCount does not include When chain invocations
    // =========================================================================
    // The following tests demonstrate that When chain calls are not counted in
    // TotalCallCount or the per-overload local count variables used for
    // verification.
    //
    // NOTE: These tests use Verify(Times) on the interceptor, which checks
    // TotalCallCount. The HEAD bug (Bug 1) does NOT affect Verify(Times) --
    // it only affects chain.Verify() and CheckVerification/CheckVerificationAll
    // When chain HEAD checks. So these tests cleanly isolate the TotalCallCount
    // bug without interference from the HEAD bug.
    //
    // Related plan: docs/plans/verifyall-totalcallcount-fix.md
    // =========================================================================

    // =========================================================================
    // Bug 2, Scenario 1: Single-sig Verify(Times) excludes When chain calls
    // =========================================================================
    // ICalculator.Add is a non-overloaded method (single-signature interceptor).
    // TotalCallCount sums _unconfiguredCallCount, _onCallTracking, _returnsValue,
    // and _sequence counts -- but NOT When chain matcher CallCount values.
    //
    // WHAT SHOULD HAPPEN: After calling Add(1,2) via When chain and Add(5,6)
    // via Returns fallback, Verify(Times.Exactly(2)) should pass (2 total calls).
    //
    // WHAT ACTUALLY HAPPENS: TotalCallCount = 1 (only the Returns call counted).
    // Verify(Times.Exactly(2)) throws "expected Exactly(2), actual 1 calls".
    //
    // WHY: The When chain invoke path does matcher.CallCount++ and returns early,
    // bypassing _returnsValueTracking.RecordCall(). The TotalCallCount getter
    // never sums the When chain matcher CallCount values.
    // =========================================================================

    [Fact]
    public void SingleSig_VerifyTimes_ShouldCountWhenChainCalls()
    {
        var stub = new VerificationDemo.Stubs.ICalculator();

        // Configure When chain (non-terminal, specific value matching)
        stub.Add.When(1, 2).Return(100);
        // Configure Returns as fallback for non-matching args
        stub.Add.Return(0);

        ICalculator calc = stub;

        // Call 1: When chain matches (1, 2) -> matcher.CallCount++
        var r1 = calc.Add(1, 2);
        Assert.Equal(100, r1);

        // Call 2: When chain doesn't match (5, 6), falls through to Returns
        // -> _returnsValueTracking._callCount++
        var r2 = calc.Add(5, 6);
        Assert.Equal(0, r2);

        // BUG: TotalCallCount = 1 (only Returns call counted).
        // Should be 2 (When chain + Returns).
        stub.Add.Verify(Times.Exactly(2));
    }

    // =========================================================================
    // Bug 2, Scenario 2: Overload Verify(Times) excludes When chain calls
    // =========================================================================
    // IFormatter.Format is an overloaded method (overload-group interceptor).
    // The overload-group TotalCallCount expression sums per-overload tracking
    // counts -- but NOT When chain matcher CallCount values.
    //
    // WHAT SHOULD HAPPEN: After calling Format("special") via When chain and
    // Format("other") via OnCall, Verify(Times.Exactly(2)) should pass.
    //
    // WHAT ACTUALLY HAPPENS: TotalCallCount = 1 (only the OnCall path counted).
    // Verify(Times.Exactly(2)) throws "expected Exactly(2), actual 1 calls".
    //
    // WHY: Same root cause as scenario 1, but in the overload-group code path.
    // The overload-group TotalCallCount expression does not include
    // (_whenChain_{suffix}?.Sum(m => m.CallCount) ?? 0).
    // =========================================================================

    [Fact]
    public void Overload_VerifyTimes_ShouldCountWhenChainCalls()
    {
        var stub = new MethodOverloadsDemo.Stubs.IFormatter();

        // Configure When chain (non-terminal, specific value matching)
        stub.Format.When("special").Return("SPECIAL");
        // Configure OnCall on the same overload as fallback
        stub.Format.Return((string input) => "default");

        IFormatter formatter = stub;

        // Call 1: When chain matches "special" -> matcher.CallCount++
        var r1 = formatter.Format("special");
        Assert.Equal("SPECIAL", r1);

        // Call 2: When chain doesn't match "other", falls through to OnCall
        // -> _onCallTracking._callCount++
        var r2 = formatter.Format("other");
        Assert.Equal("default", r2);

        // BUG: TotalCallCount = 1 (only OnCall path counted).
        // Should be 2 (When chain + OnCall).
        stub.Format.Verify(Times.Exactly(2));
    }

    // =========================================================================
    // Bug 2, Scenario 3: Overload VerifyAll local count excludes When chain
    // =========================================================================
    // In overload-group CheckVerificationAll(), each overload has TWO blocks:
    //   Block 1: condExpr (is configured?) + count check (was called?)
    //   Block 2: When chain HEAD check
    //
    // Block 1's condExpr checks _onCall and _sequence but NOT _whenChain.
    // Block 1's local count sums _onCallTracking and _sequence but NOT When
    // chain matcher CallCount values.
    //
    // This test configures a When chain AND an OnCall on the same overload,
    // then invokes ONLY via the When chain path. This ensures condExpr passes
    // (OnCall is configured) so Block 1 runs, but the count check fails
    // because the count doesn't include When chain CallCount.
    //
    // WHAT SHOULD HAPPEN: VerifyAll() should pass because the overload was
    // configured (OnCall + When chain) and was invoked (via When chain).
    //
    // WHAT ACTUALLY HAPPENS: VerifyAll() throws
    //   "Format: expected AtLeastOnce, actual 0 calls"
    // because the local count = _onCallTracking._callCount (0) + _sequence (0)
    // and does not include When chain matcher CallCount (1).
    //
    // WHY: The per-overload local count in CheckVerificationAll() does not
    // include (_whenChain_{suffix}?.Sum(m => m.CallCount) ?? 0).
    // =========================================================================

    [Fact]
    public void Overload_VerifyAll_ShouldCountWhenChainCalls()
    {
        var stub = new MethodOverloadsDemo.Stubs.IFormatter();

        // Configure When chain (non-terminal, specific value matching)
        stub.Format.When("special").Return("SPECIAL");
        // Configure OnCall on the same overload (makes condExpr true in Block 1,
        // isolating the count bug from the condExpr bug)
        stub.Format.Return((string input) => "default");

        IFormatter formatter = stub;

        // Invoke ONLY via When chain (OnCall never runs because "special" matches)
        var r = formatter.Format("special");
        Assert.Equal("SPECIAL", r);

        // BUG: VerifyAll fails with "Format: expected AtLeastOnce, actual 0"
        // because the overload count (0) doesn't include When chain CallCount (1).
        // Block 1 runs (condExpr true: OnCall configured), count = 0, fails.
        // Block 2 (HEAD check) never runs because Block 1 returned early.
        stub.VerifyAll();
    }

    // =========================================================================
    // Bug 2, Scenario 4: Void overload Verify(Times) excludes When chain calls
    // =========================================================================
    // Same as scenario 2 but for void method overloads (IFormatter.Log).
    // Void When chains use VoidWhenMatcher with matcher.CallCount++, which is
    // equally excluded from the overload-group TotalCallCount expression.
    //
    // WHAT SHOULD HAPPEN: After calling Log("special") via void When chain and
    // Log("other") via OnCall, Verify(Times.Exactly(2)) should pass.
    //
    // WHAT ACTUALLY HAPPENS: TotalCallCount = 1 (only the OnCall path counted).
    // =========================================================================

    [Fact]
    public void VoidOverload_VerifyTimes_ShouldCountWhenChainCalls()
    {
        var stub = new MethodOverloadsDemo.Stubs.IFormatter();
        var callLog = new List<string>();

        // Configure void When chain (non-terminal, specific value matching)
        stub.Log.When("special").Call(msg => callLog.Add($"when:{msg}"));
        // Configure OnCall on the same overload as fallback
        stub.Log.Call((string msg) => callLog.Add($"oncall:{msg}"));

        IFormatter formatter = stub;

        // Call 1: Void When chain matches "special" -> matcher.CallCount++
        formatter.Log("special");
        Assert.Contains("when:special", callLog);

        // Call 2: When chain doesn't match "other", falls through to OnCall
        // -> _onCallTracking._callCount++
        formatter.Log("other");
        Assert.Contains("oncall:other", callLog);

        // BUG: TotalCallCount = 1 (only OnCall path counted).
        // Should be 2 (When chain + OnCall).
        stub.Log.Verify(Times.Exactly(2));
    }
}
