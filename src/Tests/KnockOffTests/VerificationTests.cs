using KnockOff;
using Xunit;

namespace KnockOff.Tests;

/// <summary>
/// Tests for KnockOff's three-level verification system:
/// Level 2 - Method interceptor Verify()
/// Level 3 - Stub-level Verify() and VerifyAll()
///
/// Level 1 (Times.Verify()) is tested in TimesTests.cs.
/// </summary>
public class VerificationTests
{
    #region Level 2: Method Interceptor Verify() - Basic Verification

    [Fact]
    public void MethodInterceptor_Verify_ReturnsTrue_WhenNoCallbacksConfigured()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        // No OnCall configured

        // Assert
        Assert.True(stub.Add.Verify());
    }

    [Fact]
    public void MethodInterceptor_Verify_ReturnsTrue_WhenForeverConstraintCalled()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        stub.Add.OnCall((ko, a, b) => a + b); // Implicit Times.Forever

        // Act
        ISequenceTestService svc = stub;
        svc.Add(1, 2);

        // Assert
        Assert.True(stub.Add.Verify());
    }

    [Fact]
    public void MethodInterceptor_Verify_ReturnsFalse_WhenForeverConstraintNotCalled()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        stub.Add.OnCall((ko, a, b) => a + b); // Implicit Times.Forever
        // Don't call the method

        // Assert
        Assert.False(stub.Add.Verify());
    }

    #endregion

    #region Level 2: Method Interceptor Verify() - Exact Count Verification

    [Fact]
    public void MethodInterceptor_Verify_ReturnsTrue_WhenOnceConstraintSatisfied()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        stub.Add.OnCall((ko, a, b) => a + b, Times.Once);

        // Act
        ISequenceTestService svc = stub;
        svc.Add(1, 2);

        // Assert
        Assert.True(stub.Add.Verify());
    }

    [Fact]
    public void MethodInterceptor_Verify_ReturnsFalse_WhenOnceConstraintNotCalled()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        stub.Add.OnCall((ko, a, b) => a + b, Times.Once);
        // Don't call the method

        // Assert
        Assert.False(stub.Add.Verify());
    }

    [Fact]
    public void MethodInterceptor_Verify_ReturnsTrue_WhenExactlyNConstraintSatisfied()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        stub.Add.OnCall((ko, a, b) => a + b, Times.Exactly(3));

        // Act
        ISequenceTestService svc = stub;
        svc.Add(1, 2);
        svc.Add(2, 3);
        svc.Add(3, 4);

        // Assert
        Assert.True(stub.Add.Verify());
    }

    #endregion

    #region Level 2: Method Interceptor Verify() - Sequence Verification

    [Fact]
    public void MethodInterceptor_Verify_ReturnsTrue_WhenAllSequenceConstraintsSatisfied()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        stub.Add
            .OnCall((ko, a, b) => 100, Times.Once)
            .ThenCall((ko, a, b) => 200, Times.Once);

        // Act
        ISequenceTestService svc = stub;
        svc.Add(1, 2); // First callback
        svc.Add(3, 4); // Second callback

        // Assert
        Assert.True(stub.Add.Verify());
    }

    [Fact]
    public void MethodInterceptor_Verify_ReturnsFalse_WhenFirstConstraintNotSatisfied()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        stub.Add
            .OnCall((ko, a, b) => 100, Times.Twice)
            .ThenCall((ko, a, b) => 200, Times.Once);

        // Act
        ISequenceTestService svc = stub;
        svc.Add(1, 2); // Only called once, but Twice was expected

        // Assert
        Assert.False(stub.Add.Verify());
    }

    [Fact]
    public void MethodInterceptor_Verify_ReturnsFalse_WhenLastConstraintNotSatisfied()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        stub.Add
            .OnCall((ko, a, b) => 100, Times.Once)
            .ThenCall((ko, a, b) => 200, Times.Once);

        // Act
        ISequenceTestService svc = stub;
        svc.Add(1, 2); // Only first callback called

        // Assert
        Assert.False(stub.Add.Verify());
    }

    #endregion

    #region Level 2: Method Interceptor Verify() - Void Methods

    [Fact]
    public void MethodInterceptor_Verify_WorksWithVoidMethods()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        stub.DoWork.OnCall(ko => { }, Times.Once);

        // Act
        ISequenceTestService svc = stub;
        svc.DoWork();

        // Assert
        Assert.True(stub.DoWork.Verify());
    }

    #endregion

    #region Level 2: Method Interceptor Verify() - Reset Interaction

    [Fact]
    public void MethodInterceptor_Verify_ReturnsFalse_AfterReset_WhenConstraintNoLongerSatisfied()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        stub.Add.OnCall((ko, a, b) => a + b, Times.Once);

        // Act - Call, verify (true), then reset
        ISequenceTestService svc = stub;
        svc.Add(1, 2);
        Assert.True(stub.Add.Verify()); // Satisfied before reset

        stub.Add.Reset();

        // Assert - After reset, constraint is no longer satisfied
        Assert.False(stub.Add.Verify());
    }

    [Fact]
    public void MethodInterceptor_Verify_ReturnsTrue_AfterReset_WhenRecalled()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        stub.Add.OnCall((ko, a, b) => a + b, Times.Once);

        // Act
        ISequenceTestService svc = stub;
        svc.Add(1, 2);
        stub.Add.Reset();
        svc.Add(1, 2); // Call again after reset

        // Assert
        Assert.True(stub.Add.Verify());
    }

    #endregion

    #region Level 3: Stub Verify() - Returns Bool

    [Fact]
    public void StubVerify_ReturnsTrue_WhenNoMethodsConfigured()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        // No callbacks configured on any method

        // Assert
        Assert.True(stub.Verify());
    }

    [Fact]
    public void StubVerify_ReturnsTrue_WhenAllMethodsSatisfied()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        stub.Add.OnCall((ko, a, b) => a + b, Times.Once);
        stub.DoWork.OnCall(ko => { }, Times.Once);
        stub.GetMessage.OnCall((ko, name) => $"Hello {name}", Times.Once);

        // Act
        ISequenceTestService svc = stub;
        svc.Add(1, 2);
        svc.DoWork();
        svc.GetMessage("Test");

        // Assert
        Assert.True(stub.Verify());
    }

    [Fact]
    public void StubVerify_ReturnsFalse_WhenOneMethodFails()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        stub.Add.OnCall((ko, a, b) => a + b, Times.Once);
        stub.DoWork.OnCall(ko => { }, Times.Once);
        stub.GetMessage.OnCall((ko, name) => $"Hello {name}", Times.Once);

        // Act - Satisfy Add and DoWork, but not GetMessage
        ISequenceTestService svc = stub;
        svc.Add(1, 2);
        svc.DoWork();
        // Don't call GetMessage

        // Assert
        Assert.False(stub.Verify());
    }

    [Fact]
    public void StubVerify_ReturnsFalse_WhenAllMethodsFail()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        stub.Add.OnCall((ko, a, b) => a + b, Times.Once);
        stub.DoWork.OnCall(ko => { }, Times.Once);
        stub.GetMessage.OnCall((ko, name) => $"Hello {name}", Times.Once);
        // Don't call any methods

        // Assert
        Assert.False(stub.Verify());
    }

    [Fact]
    public void StubVerify_ReturnsTrue_WhenMixedConfiguredAndUnconfigured()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        stub.Add.OnCall((ko, a, b) => a + b, Times.Once);
        // DoWork and GetMessage not configured

        // Act
        ISequenceTestService svc = stub;
        svc.Add(1, 2);

        // Assert - Unconfigured methods should pass verification
        Assert.True(stub.Verify());
    }

    #endregion

    #region Level 3: Stub VerifyAll() - Throws Exception

    [Fact]
    public void StubVerifyAll_DoesNotThrow_WhenAllSatisfied()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        stub.Add.OnCall((ko, a, b) => a + b, Times.Once);

        // Act
        ISequenceTestService svc = stub;
        svc.Add(1, 2);

        // Assert - Should not throw
        stub.VerifyAll();
    }

    [Fact]
    public void StubVerifyAll_ThrowsVerificationException_WhenFails()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        stub.Add.OnCall((ko, a, b) => a + b, Times.Once);
        // Don't call the method

        // Act & Assert
        Assert.Throws<VerificationException>(() => stub.VerifyAll());
    }

    [Fact]
    public void StubVerifyAll_ExceptionMessage_ContainsExpectedText()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        stub.Add.OnCall((ko, a, b) => a + b, Times.Once);
        // Don't call the method

        // Act
        var ex = Assert.Throws<VerificationException>(() => stub.VerifyAll());

        // Assert
        Assert.Contains("verification", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Level 3: Overloaded Methods

    [Fact]
    public void StubVerify_ReturnsTrue_WithOverloadedMethods_AllOverloadsSatisfied()
    {
        // Arrange
        var stub = new OverloadTestKnockOff();
        stub.Format.OnCall((ko, input) => input.ToUpper(), Times.Once);
        stub.Format.OnCall(
            (OverloadTestKnockOff.FormatInterceptor.FormatDelegate_String_Boolean_String)
            ((ko, input, uppercase) => uppercase ? input.ToUpper() : input),
            Times.Once);
        stub.Format.OnCall(
            (OverloadTestKnockOff.FormatInterceptor.FormatDelegate_String_Int32_String)
            ((ko, input, maxLength) => input.Substring(0, Math.Min(input.Length, maxLength))),
            Times.Once);

        // Act
        IOverloadTestService svc = stub;
        svc.Format("hello");
        svc.Format("world", true);
        svc.Format("testing", 4);

        // Assert
        Assert.True(stub.Verify());
    }

    [Fact]
    public void StubVerify_ReturnsFalse_WithOverloadedMethods_OneOverloadNotSatisfied()
    {
        // Arrange
        var stub = new OverloadTestKnockOff();
        stub.Format.OnCall((ko, input) => input.ToUpper(), Times.Once);
        stub.Format.OnCall(
            (OverloadTestKnockOff.FormatInterceptor.FormatDelegate_String_Boolean_String)
            ((ko, input, uppercase) => uppercase ? input.ToUpper() : input),
            Times.Once);
        stub.Format.OnCall(
            (OverloadTestKnockOff.FormatInterceptor.FormatDelegate_String_Int32_String)
            ((ko, input, maxLength) => input.Substring(0, Math.Min(input.Length, maxLength))),
            Times.Once);

        // Act - Satisfy only two overloads
        IOverloadTestService svc = stub;
        svc.Format("hello");
        svc.Format("world", true);
        // Don't call Format(string, int)

        // Assert
        Assert.False(stub.Verify());
    }

    [Fact]
    public void StubVerify_ReturnsTrue_WithOverloadedMethods_SomeUnconfigured()
    {
        // Arrange
        var stub = new OverloadTestKnockOff();
        stub.Format.OnCall((ko, input) => input.ToUpper(), Times.Once);
        // Other overloads not configured

        // Act
        IOverloadTestService svc = stub;
        svc.Format("hello");

        // Assert - Unconfigured overloads should pass verification
        Assert.True(stub.Verify());
    }

    #endregion

    #region Edge Cases and Boundary Conditions

    [Fact]
    public void Verify_BeforeAnyCalls_WithForeverConstraint_ReturnsFalse()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        stub.Add.OnCall((ko, a, b) => a + b); // Implicit Times.Forever
        // Don't call the method

        // Assert - Forever requires at least one call
        Assert.False(stub.Add.Verify());
    }

    [Fact]
    public void Verify_BeforeAnyCalls_WithNoConstraints_ReturnsTrue()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        // No callbacks configured

        // Assert - No constraints means verification passes
        Assert.True(stub.Verify());
    }

    [Fact]
    public void Verify_WithExactlyZero_BehavesLikeNever()
    {
        // Times.Exactly(0) should behave the same as Times.Never
        var exactlyZero = Times.Exactly(0);
        var never = Times.Never;

        // Both should return true when not called
        Assert.True(exactlyZero.Verify(0));
        Assert.True(never.Verify(0));

        // Both should return false when called
        Assert.False(exactlyZero.Verify(1));
        Assert.False(never.Verify(1));
    }

    [Fact]
    public void Verify_AfterMultipleResets_StillWorks()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        stub.Add.OnCall((ko, a, b) => a + b, Times.Once);
        ISequenceTestService svc = stub;

        // Act & Assert - Multiple reset cycles
        svc.Add(1, 2);
        Assert.True(stub.Add.Verify());

        stub.Add.Reset();
        Assert.False(stub.Add.Verify());

        svc.Add(1, 2);
        Assert.True(stub.Add.Verify());

        stub.Add.Reset();
        Assert.False(stub.Add.Verify());

        svc.Add(1, 2);
        Assert.True(stub.Add.Verify());
    }

    [Fact]
    public void StubVerify_WithStrictMode_StillVerifiesCorrectly()
    {
        // Arrange
        var stub = new SequenceTestKnockOff();
        stub.Strict = true;
        stub.Add.OnCall((ko, a, b) => a + b, Times.Once);

        // Act
        ISequenceTestService svc = stub;
        svc.Add(1, 2);

        // Assert - Strict mode should not affect Verify()
        Assert.True(stub.Verify());
    }

    #endregion
}
