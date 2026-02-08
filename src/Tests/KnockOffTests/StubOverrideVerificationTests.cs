using KnockOff;
using Xunit;

namespace KnockOff.Tests;

/// <summary>
/// Tests for verification of user-defined methods in standalone stubs.
/// Stub overrides are tracked but always "configured" (user provides implementation).
/// </summary>
public class StubOverrideVerificationTests
{
    #region Verifiable() - Marking for Stub.Verify()

    [Fact]
    public void StubOverride_Verifiable_CalledOnce_PassesVerification()
    {
        // Arrange
        var stub = new StrictModeStubOverrideStub();
        stub.GetValue.Verifiable();

        // Act
        IStrictModeStubOverrideTest service = stub;
        service.GetValue(5); // Calls the stub override

        // Assert - Should not throw
        stub.Verify();
    }

    [Fact]
    public void StubOverride_Verifiable_NotCalled_ThrowsVerificationException()
    {
        // Arrange
        var stub = new StrictModeStubOverrideStub();
        stub.GetValue.Verifiable();
        // Don't call the method

        // Act & Assert
        Assert.Throws<VerificationException>(() => stub.Verify());
    }

    [Fact]
    public void StubOverride_Verifiable_WithTimesExactly_VerifiesCount()
    {
        // Arrange
        var stub = new StrictModeStubOverrideStub();
        stub.GetValue.Verifiable(Called.Exactly(2));

        // Act - Call exactly twice
        IStrictModeStubOverrideTest service = stub;
        service.GetValue(1);
        service.GetValue(2);

        // Assert - Should not throw
        stub.Verify();
    }

    [Fact]
    public void StubOverride_Verifiable_WithTimesExactly_WrongCount_Throws()
    {
        // Arrange
        var stub = new StrictModeStubOverrideStub();
        stub.GetValue.Verifiable(Called.Exactly(2));

        // Act - Call only once (expected 2)
        IStrictModeStubOverrideTest service = stub;
        service.GetValue(1);

        // Assert
        Assert.Throws<VerificationException>(() => stub.Verify());
    }

    [Fact]
    public void StubOverride_NotMarkedVerifiable_NotCheckedByStubVerify()
    {
        // Arrange
        var stub = new StrictModeStubOverrideStub();
        // Don't mark GetValue as verifiable
        // Don't call the method either

        // Assert - Should pass because nothing is marked verifiable
        stub.Verify(); // Should not throw
    }

    #endregion

    #region Multiple Stub Overrides

    [Fact]
    public void MultipleStubOverrides_BothMarkedVerifiable_BothFail_ExceptionContainsBoth()
    {
        // Arrange
        var stub = new StrictModeStubOverrideStub();
        stub.GetValue.Verifiable();
        stub.DoSomething.Verifiable();
        // Don't call either method

        // Act
        var ex = Assert.Throws<VerificationException>(() => stub.Verify());

        // Assert - Exception should contain both failures
        Assert.True(ex.Failures.Count >= 2, $"Expected at least 2 failures, got {ex.Failures.Count}");
    }

    [Fact]
    public void MultipleStubOverrides_OnlyOneFails_ExceptionContainsOnlyFailure()
    {
        // Arrange
        var stub = new StrictModeStubOverrideStub();
        stub.GetValue.Verifiable();
        stub.DoSomething.Verifiable();

        // Act - Call GetValue but not DoSomething
        IStrictModeStubOverrideTest service = stub;
        service.GetValue(5);
        // Don't call DoSomething

        // Assert
        var ex = Assert.Throws<VerificationException>(() => stub.Verify());
        Assert.Single(ex.Failures);
        Assert.Contains("DoSomething", ex.Failures[0].Member);
    }

    #endregion

    #region VerifyAll - Stub Overrides Excluded

    [Fact]
    public void VerifyAll_StubOverrideNotCalled_DoesNotIncludeStubOverrides()
    {
        // Arrange
        var stub = new StrictModeStubOverrideStub();
        // Stub overrides are always "configured" - they should not be checked by VerifyAll
        // Don't call any methods

        // Assert - VerifyAll should pass (stub overrides excluded)
        stub.VerifyAll(); // Should not throw
    }

    [Fact]
    public void VerifyAll_ConfiguredPropertyNotCalled_Fails()
    {
        // Arrange
        var stub = new StrictModeStubOverrideStub();
        stub.Name.Get("test"); // Configure the property
        // Don't access the property

        // Assert - VerifyAll should fail because configured property not accessed
        Assert.Throws<VerificationException>(() => stub.VerifyAll());
    }

    [Fact]
    public void VerifyAll_StubOverrideNotCalled_ConfiguredPropertyCalled_Passes()
    {
        // Arrange
        var stub = new StrictModeStubOverrideStub();
        stub.Name.Get("test"); // Configure the property

        // Act - Access the property but not the stub override
        IStrictModeStubOverrideTest service = stub;
        _ = service.Name;
        // Don't call GetValue or DoSomething

        // Assert - VerifyAll should pass (property accessed, stub overrides ignored)
        stub.VerifyAll();
    }

    #endregion

    #region Void Stub Overrides

    [Fact]
    public void VoidStubOverride_Verifiable_CalledOnce_PassesVerification()
    {
        // Arrange
        var stub = new StrictModeStubOverrideStub();
        stub.DoSomething.Verifiable();

        // Act
        IStrictModeStubOverrideTest service = stub;
        service.DoSomething();

        // Assert - Should not throw
        stub.Verify();
    }

    [Fact]
    public void VoidStubOverride_Verifiable_NotCalled_ThrowsVerificationException()
    {
        // Arrange
        var stub = new StrictModeStubOverrideStub();
        stub.DoSomething.Verifiable();
        // Don't call the method

        // Act & Assert
        Assert.Throws<VerificationException>(() => stub.Verify());
    }

    #endregion

    #region Reset Behavior

    [Fact]
    public void StubOverride_VerifiablePreservedAfterReset()
    {
        // Arrange
        var stub = new StrictModeStubOverrideStub();
        stub.GetValue.Verifiable();

        // Act - Call, verify passes, reset, verify fails (call count reset but verifiable preserved)
        IStrictModeStubOverrideTest service = stub;
        service.GetValue(5);
        stub.Verify(); // Should pass

        stub.GetValue.Reset();

        // Assert - Verifiable marking preserved, but needs new call
        Assert.Throws<VerificationException>(() => stub.Verify());
    }

    [Fact]
    public void StubOverride_AfterResetAndRecall_PassesVerification()
    {
        // Arrange
        var stub = new StrictModeStubOverrideStub();
        stub.GetValue.Verifiable();

        // Act
        IStrictModeStubOverrideTest service = stub;
        service.GetValue(5);
        stub.Verify(); // Should pass

        stub.GetValue.Reset();
        service.GetValue(10); // Call again after reset

        // Assert - Should pass again
        stub.Verify();
    }

    #endregion

    #region Individual Verify Still Works

    [Fact]
    public void StubOverride_IndividualVerify_StillWorks()
    {
        // Arrange
        var stub = new StrictModeStubOverrideStub();

        // Act
        IStrictModeStubOverrideTest service = stub;
        service.GetValue(5);

        // Assert - Individual Verify() on the interceptor should work
        stub.GetValue.Verify(); // Should not throw
        stub.GetValue.Verify(Called.Once);
    }

    [Fact]
    public void StubOverride_IndividualVerify_NotCalled_Throws()
    {
        // Arrange
        var stub = new StrictModeStubOverrideStub();
        // Don't call the method

        // Act & Assert
        Assert.Throws<VerificationException>(() => stub.GetValue.Verify());
    }

    #endregion

    #region Guard Condition Regression Test

    [Fact]
    public void StubWithOnlyStubOverridesAndProperty_HasVerifyMethods()
    {
        // This test verifies the guard condition fix - stubs with only stub overrides
        // and properties (no regular method interceptors) should still have Verify/VerifyAll

        // Arrange
        var stub = new StrictModeStubOverrideStub();

        // These methods should exist and be callable
        stub.Verify();
        stub.VerifyAll();

        // If we get here without compile errors, the test passes
    }

    [Fact]
    public void StubWithOnlyStubOverridesAndProperty_VerifyWithMarkedVerifiable_Works()
    {
        // Arrange
        var stub = new StrictModeStubOverrideStub();
        stub.GetValue.Verifiable();

        // Act
        IStrictModeStubOverrideTest service = stub;
        service.GetValue(42);

        // Assert
        stub.Verify(); // Should work and not throw
    }

    #endregion
}
