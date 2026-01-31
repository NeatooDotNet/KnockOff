using KnockOff;
using Xunit;

namespace KnockOff.Tests;

/// <summary>
/// Tests for KnockOff's verification system:
/// - IMethodTracking.Verify() / Verify(Times) - throws VerificationException if not satisfied
/// - IMethodSequence.Verify() - throws VerificationException if sequence incomplete
/// - Stub.Verify() - verifies all Verifiable() marked items
/// - Stub.VerifyAll() - verifies all configured items were called at least once
/// </summary>
public class VerificationTests
{
    #region IMethodTracking.Verify() - Individual Tracking Verification

    [Fact]
    public void MethodTracking_Verify_SucceedsWhenCalled()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        var tracking = stub.Add.OnCall((a, b) => a + b);

        // Act
        ISequenceTestService svc = stub;
        svc.Add(1, 2);

        // Assert - Should not throw
        tracking.Verify();
    }

    [Fact]
    public void MethodTracking_Verify_ThrowsWhenNotCalled()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        var tracking = stub.Add.OnCall((a, b) => a + b);
        // Don't call the method

        // Act & Assert
        Assert.Throws<VerificationException>(() => tracking.Verify());
    }

    [Fact]
    public void MethodTracking_VerifyWithTimes_SucceedsWhenCountMatches()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        var tracking = stub.Add.OnCall((a, b) => a + b);

        // Act
        ISequenceTestService svc = stub;
        svc.Add(1, 2);
        svc.Add(2, 3);
        svc.Add(3, 4);

        // Assert - Should not throw
        tracking.Verify(Times.Exactly(3));
    }

    [Fact]
    public void MethodTracking_VerifyWithTimes_ThrowsWhenCountDoesNotMatch()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        var tracking = stub.Add.OnCall((a, b) => a + b);

        // Act
        ISequenceTestService svc = stub;
        svc.Add(1, 2);

        // Assert - Expected exactly 3 but called 1
        Assert.Throws<VerificationException>(() => tracking.Verify(Times.Exactly(3)));
    }

    [Fact]
    public void MethodTracking_Verify_WorksWithVoidMethods()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        var tracking = stub.DoWork.OnCall(() => { });

        // Act
        ISequenceTestService svc = stub;
        svc.DoWork();

        // Assert - Should not throw
        tracking.Verify();
    }

    #endregion

    #region IMethodSequence.Verify() - Sequence Completion Verification

    [Fact]
    public void MethodSequence_Verify_SucceedsWhenComplete()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        var sequence = stub.Add
            .OnCall((a, b) => 100)
            .ThenCall((a, b) => 200);

        // Act
        ISequenceTestService svc = stub;
        svc.Add(1, 2); // First callback
        svc.Add(3, 4); // Second callback

        // Assert - Should not throw
        sequence.Verify();
    }

    [Fact]
    public void MethodSequence_Verify_ThrowsWhenIncomplete()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        var sequence = stub.Add
            .OnCall((a, b) => 100)
            .ThenCall((a, b) => 200);

        // Act
        ISequenceTestService svc = stub;
        svc.Add(1, 2); // Only first callback

        // Assert
        Assert.Throws<VerificationException>(() => sequence.Verify());
    }

    [Fact]
    public void MethodSequence_Verify_ThrowsWhenNeverCalled()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        var sequence = stub.Add
            .OnCall((a, b) => 100)
            .ThenCall((a, b) => 200);

        // Act - Don't call anything

        // Assert
        Assert.Throws<VerificationException>(() => sequence.Verify());
    }

    #endregion

    #region Verifiable() - Marking for Stub.Verify()

    [Fact]
    public void Verifiable_MarksForStubVerify()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        stub.Add.OnCall((a, b) => a + b).Verifiable();
        // Don't call the method

        // Act & Assert - Stub.Verify() should fail because the verifiable wasn't called
        Assert.Throws<VerificationException>(() => stub.Verify());
    }

    [Fact]
    public void Verifiable_SucceedsWhenCalled()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        stub.Add.OnCall((a, b) => a + b).Verifiable();

        // Act
        ISequenceTestService svc = stub;
        svc.Add(1, 2);

        // Assert - Should not throw
        stub.Verify();
    }

    [Fact]
    public void Verifiable_WithTimes_VerifiesWithConstraint()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        stub.Add.OnCall((a, b) => a + b).Verifiable(Times.Exactly(2));

        // Act
        ISequenceTestService svc = stub;
        svc.Add(1, 2);
        // Only called once but expected twice

        // Assert
        Assert.Throws<VerificationException>(() => stub.Verify());
    }

    [Fact]
    public void NonVerifiable_NotCheckedByStubVerify()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        stub.Add.OnCall((a, b) => a + b); // No .Verifiable()
        // Don't call the method

        // Assert - Stub.Verify() should pass because nothing is marked verifiable
        stub.Verify(); // Should not throw
    }

    [Fact]
    public void MixedVerifiable_OnlyChecksMarkedOnes()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        stub.Add.OnCall((a, b) => a + b).Verifiable();
        stub.DoWork.OnCall(() => { }); // Not marked verifiable
        stub.GetMessage.OnCall((name) => name); // Not marked verifiable

        // Act
        ISequenceTestService svc = stub;
        svc.Add(1, 2); // Call the verifiable one
        // Don't call DoWork or GetMessage

        // Assert - Should pass because only Add is verifiable and it was called
        stub.Verify(); // Should not throw
    }

    #endregion

    #region Stub.Verify() - Verifiable Items Only

    [Fact]
    public void StubVerify_SucceedsWhenNoVerifiables()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        stub.Add.OnCall((a, b) => a + b); // No .Verifiable()
        // Don't call any methods

        // Assert - No verifiables means nothing to check
        stub.Verify(); // Should not throw
    }

    [Fact]
    public void StubVerify_SucceedsWhenAllVerifiablesSatisfied()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        stub.Add.OnCall((a, b) => a + b).Verifiable();
        stub.DoWork.OnCall(() => { }).Verifiable();
        stub.GetMessage.OnCall((name) => $"Hello {name}").Verifiable();

        // Act
        ISequenceTestService svc = stub;
        svc.Add(1, 2);
        svc.DoWork();
        svc.GetMessage("Test");

        // Assert - Should not throw
        stub.Verify();
    }

    [Fact]
    public void StubVerify_ThrowsWhenOneVerifiableFails()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        stub.Add.OnCall((a, b) => a + b).Verifiable();
        stub.DoWork.OnCall(() => { }).Verifiable();
        stub.GetMessage.OnCall((name) => $"Hello {name}").Verifiable();

        // Act - Satisfy Add and DoWork, but not GetMessage
        ISequenceTestService svc = stub;
        svc.Add(1, 2);
        svc.DoWork();
        // Don't call GetMessage

        // Assert
        Assert.Throws<VerificationException>(() => stub.Verify());
    }

    [Fact]
    public void StubVerify_ThrowsWhenAllVerifiablesFail()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        stub.Add.OnCall((a, b) => a + b).Verifiable();
        stub.DoWork.OnCall(() => { }).Verifiable();
        stub.GetMessage.OnCall((name) => $"Hello {name}").Verifiable();
        // Don't call any methods

        // Assert
        Assert.Throws<VerificationException>(() => stub.Verify());
    }

    #endregion

    #region Stub.VerifyAll() - All Configured Items

    [Fact]
    public void StubVerifyAll_SucceedsWhenNoCallbacksConfigured()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        // No callbacks configured on any method

        // Assert - Nothing configured means nothing to verify
        stub.VerifyAll(); // Should not throw
    }

    [Fact]
    public void StubVerifyAll_SucceedsWhenAllConfiguredCalled()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        stub.Add.OnCall((a, b) => a + b);
        stub.DoWork.OnCall(() => { });
        stub.GetMessage.OnCall((name) => $"Hello {name}");

        // Act
        ISequenceTestService svc = stub;
        svc.Add(1, 2);
        svc.DoWork();
        svc.GetMessage("Test");

        // Assert - Should not throw
        stub.VerifyAll();
    }

    [Fact]
    public void StubVerifyAll_ThrowsWhenOneConfiguredNotCalled()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        stub.Add.OnCall((a, b) => a + b);
        stub.DoWork.OnCall(() => { });
        stub.GetMessage.OnCall((name) => $"Hello {name}");

        // Act - Satisfy Add and DoWork, but not GetMessage
        ISequenceTestService svc = stub;
        svc.Add(1, 2);
        svc.DoWork();
        // Don't call GetMessage

        // Assert
        Assert.Throws<VerificationException>(() => stub.VerifyAll());
    }

    [Fact]
    public void StubVerifyAll_ThrowsWhenAllConfiguredNotCalled()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        stub.Add.OnCall((a, b) => a + b);
        stub.DoWork.OnCall(() => { });
        stub.GetMessage.OnCall((name) => $"Hello {name}");
        // Don't call any methods

        // Assert
        Assert.Throws<VerificationException>(() => stub.VerifyAll());
    }

    [Fact]
    public void StubVerifyAll_ExceptionContainsFailureInfo()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        stub.Add.OnCall((a, b) => a + b);
        // Don't call the method

        // Act
        var ex = Assert.Throws<VerificationException>(() => stub.VerifyAll());

        // Assert
        Assert.Contains("verification", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Reset Interaction

    [Fact]
    public void MethodTracking_Verify_AfterReset_RequiresNewCalls()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        var tracking = stub.Add.OnCall((a, b) => a + b);

        // Act - Call, verify (passes), then reset
        ISequenceTestService svc = stub;
        svc.Add(1, 2);
        tracking.Verify(); // Should pass

        stub.Add.Reset();

        // Assert - After reset, requires a new call
        Assert.Throws<VerificationException>(() => tracking.Verify());
    }

    [Fact]
    public void MethodTracking_Verify_AfterResetAndRecall_Succeeds()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        var tracking = stub.Add.OnCall((a, b) => a + b);

        // Act
        ISequenceTestService svc = stub;
        svc.Add(1, 2);
        stub.Add.Reset();
        svc.Add(1, 2); // Call again after reset

        // Assert - Should not throw
        tracking.Verify();
    }

    [Fact]
    public void Verifiable_PreservedAcrossReset()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        stub.Add.OnCall((a, b) => a + b).Verifiable();

        // Act
        ISequenceTestService svc = stub;
        svc.Add(1, 2);
        stub.Verify(); // Should pass

        stub.Add.Reset();

        // Assert - Verifiable marking should be preserved, but needs new call
        Assert.Throws<VerificationException>(() => stub.Verify());

        svc.Add(1, 2); // Call again
        stub.Verify(); // Should pass again
    }

    #endregion

    #region Overloaded Methods

    [Fact]
    public void VerifyAll_WithOverloads_SucceedsWhenAllConfiguredCalled()
    {
        // Arrange
        var stub = new OverloadTestKnockOff();
        stub.Format.OnCall((input) => input.ToUpper());
        stub.Format.OnCall(
            (OverloadTestKnockOff.FormatInterceptor.FormatDelegate_String_Boolean_String)
            ((input, uppercase) => uppercase ? input.ToUpper() : input));

        // Act
        IOverloadTestService svc = stub;
        svc.Format("hello");
        svc.Format("world", true);

        // Assert - Should not throw
        stub.VerifyAll();
    }

    [Fact]
    public void VerifyAll_WithOverloads_ThrowsWhenOneNotCalled()
    {
        // Arrange
        var stub = new OverloadTestKnockOff();
        stub.Format.OnCall((input) => input.ToUpper());
        stub.Format.OnCall(
            (OverloadTestKnockOff.FormatInterceptor.FormatDelegate_String_Boolean_String)
            ((input, uppercase) => uppercase ? input.ToUpper() : input));

        // Act - Only call one overload
        IOverloadTestService svc = stub;
        svc.Format("hello");
        // Don't call Format(string, bool)

        // Assert
        Assert.Throws<VerificationException>(() => stub.VerifyAll());
    }

    [Fact]
    public void Verify_WithOverloads_OnlyChecksVerifiable()
    {
        // Arrange
        var stub = new OverloadTestKnockOff();
        stub.Format.OnCall((input) => input.ToUpper()).Verifiable();
        stub.Format.OnCall(
            (OverloadTestKnockOff.FormatInterceptor.FormatDelegate_String_Boolean_String)
            ((input, uppercase) => uppercase ? input.ToUpper() : input));
        // Second overload not marked verifiable

        // Act
        IOverloadTestService svc = stub;
        svc.Format("hello"); // Call only the verifiable one

        // Assert - Should pass because only the single-param overload is verifiable
        stub.Verify(); // Should not throw
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Times_Validate_ExactlyZeroBehavesLikeNever()
    {
        var exactlyZero = Times.Exactly(0);
        var never = Times.Never;

        // Both should return true when not called
        Assert.True(exactlyZero.Validate(0));
        Assert.True(never.Validate(0));

        // Both should return false when called
        Assert.False(exactlyZero.Validate(1));
        Assert.False(never.Validate(1));
    }

    [Fact]
    public void VerifyAll_WithStrictMode_StillVerifiesCorrectly()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        stub.Strict = true;
        stub.Add.OnCall((a, b) => a + b);

        // Act
        ISequenceTestService svc = stub;
        svc.Add(1, 2);

        // Assert - Strict mode should not affect VerifyAll()
        stub.VerifyAll(); // Should not throw
    }

    [Fact]
    public void Verify_AfterMultipleResets_StillWorks()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        var tracking = stub.Add.OnCall((a, b) => a + b);
        ISequenceTestService svc = stub;

        // Act & Assert - Multiple reset cycles
        svc.Add(1, 2);
        tracking.Verify();

        stub.Add.Reset();
        Assert.Throws<VerificationException>(() => tracking.Verify());

        svc.Add(1, 2);
        tracking.Verify();

        stub.Add.Reset();
        Assert.Throws<VerificationException>(() => tracking.Verify());

        svc.Add(1, 2);
        tracking.Verify();
    }

    #endregion
}
