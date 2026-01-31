// -----------------------------------------------------------------------------
// Design.Tests - Verification Tests
// -----------------------------------------------------------------------------

using Design.Domain.Services;
using Design.Stubs.Advanced;
using KnockOff;

namespace Design.Tests.AdvancedTests;

/// <summary>
/// Tests for verification: Verify(), Verifiable(), stub.Verify(), stub.VerifyAll().
/// </summary>
public class VerificationTests
{
    [Fact]
    public void Verify_ThrowsWhenNotCalled()
    {
        var stub = new VerificationDemo.Stubs.ICalculator();
        stub.Add.Returns(0);

        Assert.Throws<VerificationException>(() => stub.Add.Verify());
    }

    [Fact]
    public void Verify_PassesWhenCalled()
    {
        var stub = new VerificationDemo.Stubs.ICalculator();

        ICalculator calc = stub;
        calc.Add(1, 2);

        stub.Add.Verify(); // Should not throw
    }

    [Fact]
    public void Verify_Never_FailsWhenCalled()
    {
        var stub = new VerificationDemo.Stubs.ICalculator();

        ICalculator calc = stub;
        calc.Add(1, 2);

        Assert.Throws<VerificationException>(() => stub.Add.Verify(Times.Never));
    }

    [Fact]
    public void Verify_Never_PassesWhenNotCalled()
    {
        var stub = new VerificationDemo.Stubs.ICalculator();

        stub.Add.Verify(Times.Never); // Should not throw
    }

    [Fact]
    public void Verify_Exactly_ChecksCount()
    {
        var stub = new VerificationDemo.Stubs.ICalculator();

        ICalculator calc = stub;
        calc.Add(1, 2);
        calc.Add(3, 4);

        stub.Add.Verify(Times.Exactly(2));
    }

    [Fact]
    public void Verify_AtLeast_ChecksMinimum()
    {
        var stub = new VerificationDemo.Stubs.ICalculator();

        ICalculator calc = stub;
        calc.Add(1, 2);
        calc.Add(3, 4);
        calc.Add(5, 6);

        stub.Add.Verify(Times.AtLeast(2));
    }

    [Fact]
    public void Verify_AtMost_ChecksMaximum()
    {
        var stub = new VerificationDemo.Stubs.ICalculator();

        ICalculator calc = stub;
        calc.Add(1, 2);
        calc.Add(3, 4);

        stub.Add.Verify(Times.AtMost(5));
    }

    [Fact]
    public void Verifiable_MarksForDeferredVerification()
    {
        var stub = new VerificationDemo.Stubs.ICalculator();

        stub.Add.Returns(42).Verifiable();

        ICalculator calc = stub;
        calc.Add(1, 2);

        stub.Verify(); // Checks verifiable members
    }

    [Fact]
    public void Verifiable_WithTimes()
    {
        var stub = new VerificationDemo.Stubs.ICalculator();

        stub.Add.Returns(42).Verifiable(Times.Exactly(2));

        ICalculator calc = stub;
        calc.Add(1, 2);

        Assert.Throws<VerificationException>(() => stub.Verify()); // Only 1 call

        calc.Add(3, 4);

        stub.Verify(); // Now 2 calls - passes
    }

    [Fact]
    public void StubVerify_OnlyChecksVerifiableMembers()
    {
        var stub = new VerificationDemo.Stubs.ICalculator();

        stub.Add.Returns(42).Verifiable();
        stub.Subtract.Returns(10); // Not verifiable

        ICalculator calc = stub;
        calc.Add(1, 2);
        // calc.Subtract not called

        stub.Verify(); // Only checks Add, passes
    }

    [Fact]
    public void StubVerifyAll_ChecksAllConfiguredMembers()
    {
        var stub = new VerificationDemo.Stubs.ICalculator();

        stub.Add.Returns(42);
        stub.Subtract.Returns(10);

        ICalculator calc = stub;
        calc.Add(1, 2);
        // Subtract configured but not called

        Assert.Throws<VerificationException>(() => stub.VerifyAll());
    }

    [Fact]
    public void VerificationException_CollectsAllFailures()
    {
        var stub = new VerificationDemo.Stubs.ICalculator();

        stub.Add.Returns(42).Verifiable();
        stub.Subtract.Returns(10).Verifiable();

        ICalculator calc = stub;
        // Neither called

        var ex = Assert.Throws<VerificationException>(() => stub.Verify());

        // Exception message should mention both failures
        Assert.Contains("Add", ex.Message, StringComparison.Ordinal);
        Assert.Contains("Subtract", ex.Message, StringComparison.Ordinal);
    }
}
