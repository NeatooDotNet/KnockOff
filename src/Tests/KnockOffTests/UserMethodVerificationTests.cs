using KnockOff;
using Xunit;

namespace KnockOff.Tests;

/// <summary>
/// Tests for verification of user-defined methods in standalone stubs.
/// User methods are tracked but always "configured" (user provides implementation).
/// </summary>
public class UserMethodVerificationTests
{
    #region Verifiable() - Marking for Stub.Verify()

    [Fact]
    public void UserMethod_Verifiable_CalledOnce_PassesVerification()
    {
        // Arrange
        var stub = new StrictModeUserMethodStub();
        stub.GetValue.Verifiable();

        // Act
        IStrictModeUserMethodTest service = stub;
        service.GetValue(5); // Calls the user method

        // Assert - Should not throw
        stub.Verify();
    }

    [Fact]
    public void UserMethod_Verifiable_NotCalled_ThrowsVerificationException()
    {
        // Arrange
        var stub = new StrictModeUserMethodStub();
        stub.GetValue.Verifiable();
        // Don't call the method

        // Act & Assert
        Assert.Throws<VerificationException>(() => stub.Verify());
    }

    [Fact]
    public void UserMethod_Verifiable_WithTimesExactly_VerifiesCount()
    {
        // Arrange
        var stub = new StrictModeUserMethodStub();
        stub.GetValue.Verifiable(Times.Exactly(2));

        // Act - Call exactly twice
        IStrictModeUserMethodTest service = stub;
        service.GetValue(1);
        service.GetValue(2);

        // Assert - Should not throw
        stub.Verify();
    }

    [Fact]
    public void UserMethod_Verifiable_WithTimesExactly_WrongCount_Throws()
    {
        // Arrange
        var stub = new StrictModeUserMethodStub();
        stub.GetValue.Verifiable(Times.Exactly(2));

        // Act - Call only once (expected 2)
        IStrictModeUserMethodTest service = stub;
        service.GetValue(1);

        // Assert
        Assert.Throws<VerificationException>(() => stub.Verify());
    }

    [Fact]
    public void UserMethod_NotMarkedVerifiable_NotCheckedByStubVerify()
    {
        // Arrange
        var stub = new StrictModeUserMethodStub();
        // Don't mark GetValue as verifiable
        // Don't call the method either

        // Assert - Should pass because nothing is marked verifiable
        stub.Verify(); // Should not throw
    }

    #endregion

    #region Multiple User Methods

    [Fact]
    public void MultipleUserMethods_BothMarkedVerifiable_BothFail_ExceptionContainsBoth()
    {
        // Arrange
        var stub = new StrictModeUserMethodStub();
        stub.GetValue.Verifiable();
        stub.DoSomething.Verifiable();
        // Don't call either method

        // Act
        var ex = Assert.Throws<VerificationException>(() => stub.Verify());

        // Assert - Exception should contain both failures
        Assert.True(ex.Failures.Count >= 2, $"Expected at least 2 failures, got {ex.Failures.Count}");
    }

    [Fact]
    public void MultipleUserMethods_OnlyOneFails_ExceptionContainsOnlyFailure()
    {
        // Arrange
        var stub = new StrictModeUserMethodStub();
        stub.GetValue.Verifiable();
        stub.DoSomething.Verifiable();

        // Act - Call GetValue but not DoSomething
        IStrictModeUserMethodTest service = stub;
        service.GetValue(5);
        // Don't call DoSomething

        // Assert
        var ex = Assert.Throws<VerificationException>(() => stub.Verify());
        Assert.Single(ex.Failures);
        Assert.Contains("DoSomething", ex.Failures[0].Member);
    }

    #endregion

    #region VerifyAll - User Methods Excluded

    [Fact]
    public void VerifyAll_UserMethodNotCalled_DoesNotIncludeUserMethods()
    {
        // Arrange
        var stub = new StrictModeUserMethodStub();
        // User methods are always "configured" - they should not be checked by VerifyAll
        // Don't call any methods

        // Assert - VerifyAll should pass (user methods excluded)
        stub.VerifyAll(); // Should not throw
    }

    [Fact]
    public void VerifyAll_ConfiguredPropertyNotCalled_Fails()
    {
        // Arrange
        var stub = new StrictModeUserMethodStub();
        stub.Name.Get("test"); // Configure the property
        // Don't access the property

        // Assert - VerifyAll should fail because configured property not accessed
        Assert.Throws<VerificationException>(() => stub.VerifyAll());
    }

    [Fact]
    public void VerifyAll_UserMethodNotCalled_ConfiguredPropertyCalled_Passes()
    {
        // Arrange
        var stub = new StrictModeUserMethodStub();
        stub.Name.Get("test"); // Configure the property

        // Act - Access the property but not the user method
        IStrictModeUserMethodTest service = stub;
        _ = service.Name;
        // Don't call GetValue or DoSomething

        // Assert - VerifyAll should pass (property accessed, user methods ignored)
        stub.VerifyAll();
    }

    #endregion

    #region Void User Methods

    [Fact]
    public void VoidUserMethod_Verifiable_CalledOnce_PassesVerification()
    {
        // Arrange
        var stub = new StrictModeUserMethodStub();
        stub.DoSomething.Verifiable();

        // Act
        IStrictModeUserMethodTest service = stub;
        service.DoSomething();

        // Assert - Should not throw
        stub.Verify();
    }

    [Fact]
    public void VoidUserMethod_Verifiable_NotCalled_ThrowsVerificationException()
    {
        // Arrange
        var stub = new StrictModeUserMethodStub();
        stub.DoSomething.Verifiable();
        // Don't call the method

        // Act & Assert
        Assert.Throws<VerificationException>(() => stub.Verify());
    }

    #endregion

    #region Reset Behavior

    [Fact]
    public void UserMethod_VerifiablePreservedAfterReset()
    {
        // Arrange
        var stub = new StrictModeUserMethodStub();
        stub.GetValue.Verifiable();

        // Act - Call, verify passes, reset, verify fails (call count reset but verifiable preserved)
        IStrictModeUserMethodTest service = stub;
        service.GetValue(5);
        stub.Verify(); // Should pass

        stub.GetValue.Reset();

        // Assert - Verifiable marking preserved, but needs new call
        Assert.Throws<VerificationException>(() => stub.Verify());
    }

    [Fact]
    public void UserMethod_AfterResetAndRecall_PassesVerification()
    {
        // Arrange
        var stub = new StrictModeUserMethodStub();
        stub.GetValue.Verifiable();

        // Act
        IStrictModeUserMethodTest service = stub;
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
    public void UserMethod_IndividualVerify_StillWorks()
    {
        // Arrange
        var stub = new StrictModeUserMethodStub();

        // Act
        IStrictModeUserMethodTest service = stub;
        service.GetValue(5);

        // Assert - Individual Verify() on the interceptor should work
        stub.GetValue.Verify(); // Should not throw
        stub.GetValue.Verify(Times.Once);
    }

    [Fact]
    public void UserMethod_IndividualVerify_NotCalled_Throws()
    {
        // Arrange
        var stub = new StrictModeUserMethodStub();
        // Don't call the method

        // Act & Assert
        Assert.Throws<VerificationException>(() => stub.GetValue.Verify());
    }

    #endregion

    #region Guard Condition Regression Test

    [Fact]
    public void StubWithOnlyUserMethodsAndProperty_HasVerifyMethods()
    {
        // This test verifies the guard condition fix - stubs with only user methods
        // and properties (no regular method interceptors) should still have Verify/VerifyAll

        // Arrange
        var stub = new StrictModeUserMethodStub();

        // These methods should exist and be callable
        stub.Verify();
        stub.VerifyAll();

        // If we get here without compile errors, the test passes
    }

    [Fact]
    public void StubWithOnlyUserMethodsAndProperty_VerifyWithMarkedVerifiable_Works()
    {
        // Arrange
        var stub = new StrictModeUserMethodStub();
        stub.GetValue.Verifiable();

        // Act
        IStrictModeUserMethodTest service = stub;
        service.GetValue(42);

        // Assert
        stub.Verify(); // Should work and not throw
    }

    #endregion
}
