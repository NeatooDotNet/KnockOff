// -----------------------------------------------------------------------------
// Design.Stubs - Verification
// -----------------------------------------------------------------------------
// This file demonstrates the verification APIs:
// - Verify() on interceptors for direct verification
// - Verifiable() for deferred verification
// - stub.Verify() for all verifiable members
// - stub.VerifyAll() for all configured members
// - Called constraints
// -----------------------------------------------------------------------------

using Design.Domain.Services;
using KnockOff;

namespace Design.Stubs.Advanced;

// =============================================================================
// VERIFICATION
// =============================================================================

[KnockOff<ICalculator>]
public partial class VerificationDemo
{
    // =========================================================================
    // Direct Verification - Interceptor.Verify()
    // =========================================================================
    // DESIGN DECISION: Each interceptor has Verify() and Verify(Called) methods
    // for immediate verification. This checks the interceptor's call count.
    //
    // GENERATOR BEHAVIOR:
    //
    //   public void Verify() => Verify(Called.AtLeastOnce);
    //
    //   public void Verify(Called called)
    //   {
    //       if (!called.Validate(TotalCallCount))
    //           throw VerificationException(...);
    //   }
    // =========================================================================

    public void Verify_DirectOnInterceptor()
    {
        var stub = new Stubs.ICalculator();
        stub.Add.Return(42);

        ICalculator calc = stub;

        calc.Add(2, 3);
        calc.Add(4, 5);

        // Direct verification - throws if not met
        stub.Add.Verify(); // Passes - called at least once
        stub.Add.Verify(Called.Exactly(2)); // Passes - called exactly twice
    }

    // =========================================================================
    // Called Constraints
    // =========================================================================
    // DESIGN DECISION: Called provides static factory methods for common
    // verification patterns. All verification methods accept Called.
    //
    // Available Called:
    // - Called.Never - Must not be called
    // - Called.Once - Called exactly once
    // - Called.Exactly(n) - Called exactly n times
    // - Called.AtLeast(n) - Called n or more times
    // - Called.AtMost(n) - Called n or fewer times
    // - Called.AtLeastOnce - Called one or more times (default)
    // Note: Called.Between is not available. Use AtLeast/AtMost for range constraints.
    // =========================================================================

    public void Called_AllConstraints()
    {
        var stub = new Stubs.ICalculator();

        ICalculator calc = stub;

        // Never called
        stub.Divide.Verify(Called.Never);

        // Call once
        calc.Add(1, 2);
        stub.Add.Verify(Called.Once);
        stub.Add.Verify(Called.AtLeastOnce);
        stub.Add.Verify(Called.AtLeast(1));
        stub.Add.Verify(Called.AtMost(5));

        // Call more times
        calc.Add(3, 4);
        calc.Add(5, 6);
        stub.Add.Verify(Called.Exactly(3));
        stub.Add.Verify(Called.AtLeast(2));
        stub.Add.Verify(Called.AtMost(10));
    }

    // =========================================================================
    // Verifiable() - Mark for Deferred Verification
    // =========================================================================
    // DESIGN DECISION: Verifiable() marks a configuration for later verification
    // via stub.Verify(). This is useful for:
    // - Verifying multiple methods at once
    // - Setting up expectations before exercising code
    // - AAA pattern: Arrange includes expectations
    //
    // GENERATOR BEHAVIOR:
    //
    //   public IMethodCallBuilder<...> Verifiable()
    //   {
    //       _interceptor._isVerifiable = true;
    //       return this;
    //   }
    // =========================================================================

    public void Verifiable_MarksForDeferredVerification()
    {
        var stub = new Stubs.ICalculator();

        // Mark as verifiable during configuration
        stub.Add.Return(42).Verifiable();
        stub.Subtract.Return(10).Verifiable(Called.Exactly(2));

        ICalculator calc = stub;

        calc.Add(1, 2);
        calc.Subtract(5, 3);
        calc.Subtract(7, 2);

        // Verify all verifiable members at once
        stub.Verify(); // Checks Add (AtLeastOnce) and Subtract (Exactly(2))
    }

    // =========================================================================
    // stub.Verify() - Verify All Verifiable Members
    // =========================================================================
    // DESIGN DECISION: stub.Verify() checks all members marked with Verifiable().
    // It collects ALL failures and throws a single VerificationException with
    // all failure details.
    //
    // Members NOT marked with Verifiable() are NOT checked by stub.Verify().
    //
    // GENERATOR BEHAVIOR:
    //
    //   public void Verify()
    //   {
    //       var failures = new List<VerificationFailure>();
    //       if (Add.CheckVerification() is { } f1) failures.Add(f1);
    //       if (Subtract.CheckVerification() is { } f2) failures.Add(f2);
    //       // ...
    //       if (failures.Count > 0)
    //           throw new VerificationException(failures);
    //   }
    // =========================================================================

    public void StubVerify_OnlyChecksVerifiableMembers()
    {
        var stub = new Stubs.ICalculator();

        // Only Add is marked verifiable
        stub.Add.Return(42).Verifiable();
        stub.Subtract.Return(10); // NOT verifiable

        ICalculator calc = stub;

        calc.Add(1, 2);
        // calc.Subtract not called - but not verifiable so doesn't matter

        // This only checks Add, ignores Subtract
        stub.Verify(); // Passes
    }

    // =========================================================================
    // stub.VerifyAll() - Verify All Configured Members
    // =========================================================================
    // DESIGN DECISION: stub.VerifyAll() checks all CONFIGURED members, not just
    // those marked Verifiable(). If you configured a method (Returns, Execute,
    // When), VerifyAll expects it to be called at least once.
    //
    // This is useful for ensuring all stub configurations were actually used.
    //
    // GENERATOR BEHAVIOR:
    //
    //   public void VerifyAll()
    //   {
    //       var failures = new List<VerificationFailure>();
    //       if (Add.CheckVerificationAll() is { } f1) failures.Add(f1);
    //       // ... CheckVerificationAll returns failure if configured but not called
    //       if (failures.Count > 0)
    //           throw new VerificationException(failures);
    //   }
    // =========================================================================

    public void StubVerifyAll_ChecksAllConfigured()
    {
        var stub = new Stubs.ICalculator();

        stub.Add.Return(42); // Configured
        stub.Subtract.Return(10); // Configured but not verifiable

        ICalculator calc = stub;

        calc.Add(1, 2);
        // calc.Subtract not called

        // This would fail because Subtract was configured but never called
        // stub.VerifyAll(); // Throws!
    }

    // =========================================================================
    // VerificationException - Multiple Failures
    // =========================================================================
    // DESIGN DECISION: VerificationException collects multiple failures and
    // reports them all at once. This gives you a complete picture of what
    // failed rather than failing fast on the first issue.
    //
    // The exception message lists all failed verifications:
    //   "Verification failed:
    //    - Method 'Add' expected AtLeastOnce, was called 0 times
    //    - Method 'Subtract' expected Exactly(2), was called 1 time"
    // =========================================================================

    public void VerificationException_CollectsAllFailures()
    {
        var stub = new Stubs.ICalculator();

        stub.Add.Return(42).Verifiable();
        stub.Subtract.Return(10).Verifiable();
        stub.Divide.Return(5).Verifiable();

        ICalculator calc = stub;

        // Only call Add
        calc.Add(1, 2);

        try
        {
            stub.Verify();
        }
        catch (VerificationException ex)
        {
            // ex.Failures contains both Subtract and Divide failures
            // ex.Message describes all failures
            _ = ex.Message;
        }
    }

    // =========================================================================
    // Sequence Verification
    // =========================================================================
    // DESIGN DECISION: Method sequences have their own Verify() that checks
    // if the entire sequence was consumed.
    // =========================================================================

    public void Sequence_Verification()
    {
        var stub = new Stubs.ICalculator();

        var sequence = stub.Add.Return((a, b) => a + b)
            .ThenReturn((a, b) => a * b)
            .ThenReturn((a, b) => 999);

        ICalculator calc = stub;

        calc.Add(1, 2); // First callback
        calc.Add(3, 4); // Second callback

        // Sequence not complete - only 2 of 3 consumed
        // sequence.Verify(); // Would throw - incomplete

        calc.Add(5, 6); // Third callback

        // Now complete
        sequence.Verify(); // Passes - all 3 consumed
    }

    // =========================================================================
    // When Chain Verification
    // =========================================================================
    // DESIGN DECISION: When chains can be verified similarly to sequences.
    // The Verify() on a When chain checks if all matchers were consumed.
    // =========================================================================

    public void WhenChain_Verification()
    {
        var stub = new Stubs.ICalculator();

        var chain = stub.Add.When(1, 2).Return(10)
            .ThenWhen(3, 4).Return(20)
            .ThenCall((a, b) => 999); // Terminal

        ICalculator calc = stub;

        calc.Add(1, 2); // Matches first
        calc.Add(3, 4); // Matches second
        calc.Add(0, 0); // Falls to terminal

        // Verify chain was fully consumed
        chain.Verify(); // Passes
    }

    // =========================================================================
    // Verifiable() with Called
    // =========================================================================
    // DESIGN DECISION: Verifiable(Called) sets both the verifiable flag AND
    // the expected Called constraint. This is checked by stub.Verify().
    // =========================================================================

    public void Verifiable_WithCalled()
    {
        var stub = new Stubs.ICalculator();

        // Must be called exactly twice
        stub.Add.Return(42).Verifiable(Called.Exactly(2));

        ICalculator calc = stub;

        calc.Add(1, 2);
        calc.Add(3, 4);

        stub.Verify(); // Passes - called exactly twice
    }

    // =========================================================================
    // COMMON MISTAKE: Verify vs VerifyAll Confusion
    // =========================================================================
    //
    // COMMON MISTAKE: Using stub.Verify() expecting it to check all methods
    //
    // stub.Verify() ONLY checks members marked with .Verifiable()
    // stub.VerifyAll() checks ALL configured members
    //
    // Choose based on your testing philosophy:
    // - Verify(): Explicit expectations (only what you marked)
    // - VerifyAll(): All configurations must be used
    // =========================================================================

    // =========================================================================
    // DESIGN DECISION: Reset() Does Not Clear Verifiable Flag
    // =========================================================================
    // Reset() clears tracking state (call counts) but preserves configuration,
    // including the Verifiable() marking. This allows re-running tests with
    // the same expectations.
    // =========================================================================

    public void Reset_PreservesVerifiable()
    {
        var stub = new Stubs.ICalculator();

        stub.Add.Return(42).Verifiable();

        ICalculator calc = stub;

        calc.Add(1, 2);
        stub.Verify(); // Passes

        // Reset clears call count
        stub.Add.Reset();

        // Verifiable marking is preserved
        // stub.Verify(); // Would fail - call count is 0 again

        calc.Add(3, 4);
        stub.Verify(); // Passes again
    }
}
