// -----------------------------------------------------------------------------
// Design.Tests - When Chain Verification Bug Tests
// -----------------------------------------------------------------------------
// BUG: When a When chain has a single non-terminal matcher (e.g.,
//   .When(1, 2).Returns(100).Verifiable()),
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
// These tests are expected to FAIL until the bug is fixed.
// Each test documents what SHOULD happen vs what DOES happen today.
// -----------------------------------------------------------------------------

using Design.Domain.Services;
using Design.Stubs.Advanced;
using KnockOff;

namespace Design.Tests.AdvancedTests;

/// <summary>
/// Tests demonstrating the When chain verification bug.
/// All tests in this class are expected to FAIL until the bug is fixed.
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
        stub.Add.When(1, 2).Returns(100).Verifiable();

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
        var chain = stub.Add.When(1, 2).Returns(100);

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
            .When(1, 2).Returns(10)
            .ThenWhen(3, 4).Returns(20);

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

        var chain = stub.Add.When(1, 2).Returns(100);

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
        stub.Add.When(1, 2).Returns(100);

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
            .When(1, 2).Returns(10)
            .ThenCall((a, b) => a + b);

        ICalculator calc = stub;

        // Consume first matcher, then fall to terminal
        Assert.Equal(10, calc.Add(1, 2));
        Assert.Equal(7, calc.Add(3, 4)); // Terminal: 3 + 4 = 7

        // This should pass because HEAD advanced to the terminal matcher.
        // IsTerminal=true causes the verification check to pass.
        chain.Verify();
    }
}
