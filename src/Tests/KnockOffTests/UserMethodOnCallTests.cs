using KnockOff;
using System.Threading.Tasks;
using Xunit;

namespace KnockOff.Tests;

/// <summary>
/// Tests for OnCall/Returns on user method interceptors.
/// OnCall supersedes the user method; user method is the fallback.
/// </summary>
public class UserMethodOnCallTests
{
    #region OnCall Supersedes User Method

    [Fact]
    public void OnCall_SupersedesUserMethod_NonVoid()
    {
        // Arrange
        var stub = new StrictModeUserMethodStub();
        stub.GetValue.OnCall(x => x * 100); // Override user method (which does x * 10)

        // Act
        IStrictModeUserMethodTest service = stub;
        var result = service.GetValue(5);

        // Assert - OnCall wins over user method
        Assert.Equal(500, result); // 5 * 100, not 5 * 10
    }

    [Fact]
    public void OnCall_SupersedesUserMethod_Void()
    {
        // Arrange
        var stub = new StrictModeUserMethodStub();
        var callbackInvoked = false;
        stub.DoSomething.OnCall(() => callbackInvoked = true);

        // Act
        IStrictModeUserMethodTest service = stub;
        service.DoSomething();

        // Assert - OnCall was invoked
        Assert.True(callbackInvoked);
    }

    #endregion

    #region Returns Supersedes User Method

    [Fact]
    public void Returns_SupersedesUserMethod_NonVoid()
    {
        // Arrange
        var stub = new StrictModeUserMethodStub();
        stub.GetValue.Returns(999); // Override user method

        // Act
        IStrictModeUserMethodTest service = stub;
        var result = service.GetValue(5);

        // Assert - Returns wins over user method
        Assert.Equal(999, result); // Not 50 (user method: x * 10)
    }

    #endregion

    #region User Method Fallback

    [Fact]
    public void UserMethod_IsCalledWhenNoCallback()
    {
        // Arrange
        var stub = new StrictModeUserMethodStub();
        // No OnCall configured

        // Act
        IStrictModeUserMethodTest service = stub;
        var result = service.GetValue(5);

        // Assert - User method is called as fallback
        Assert.Equal(50, result); // User method: x * 10
    }

    [Fact]
    public void UserMethod_IsCalledWhenNoCallback_Void()
    {
        // Arrange
        var stub = new StrictModeUserMethodStub();
        // No OnCall configured

        // Act
        IStrictModeUserMethodTest service = stub;
        service.DoSomething(); // Should not throw

        // Assert - Verify it was tracked
        stub.DoSomething.Verify(Times.Once);
    }

    #endregion

    #region Reset Preserves OnCall Configuration

    [Fact]
    public void Reset_PreservesOnCallConfiguration()
    {
        // Arrange
        var stub = new StrictModeUserMethodStub();
        stub.GetValue.OnCall(x => x * 100);

        IStrictModeUserMethodTest service = stub;
        service.GetValue(5);
        stub.GetValue.Verify(Times.Once);

        // Act - Reset tracking state
        stub.GetValue.Reset();

        // Assert - OnCall configuration is preserved, tracking is cleared
        stub.GetValue.Verify(Times.Never);
        var result = service.GetValue(3);
        Assert.Equal(300, result); // Still uses OnCall (3 * 100)
    }

    [Fact]
    public void Reset_ClearsTrackingState()
    {
        // Arrange
        var stub = new StrictModeUserMethodStub();
        stub.GetValue.Returns(42);

        IStrictModeUserMethodTest service = stub;
        service.GetValue(1);
        service.GetValue(2);
        stub.GetValue.Verify(Times.Exactly(2));

        // Act
        stub.GetValue.Reset();

        // Assert - Call count is cleared
        stub.GetValue.Verify(Times.Never);
    }

    #endregion

    #region Multiple Parameters With OnCall

    [Fact]
    public void OnCall_WithMultipleParameters()
    {
        // Arrange
        var stub = new MultiParamUserMethodStub();
        stub.Calculate.OnCall((a, b) => a - b); // Override user method (which does a + b)

        // Act
        IMultiParamUserMethodService service = stub;
        var result = service.Calculate(10, 3);

        // Assert - OnCall wins (subtraction instead of addition)
        Assert.Equal(7, result); // 10 - 3, not 10 + 3
    }

    [Fact]
    public void Returns_WithMultipleParameters()
    {
        // Arrange
        var stub = new MultiParamUserMethodStub();
        stub.Calculate.Returns(999);

        // Act
        IMultiParamUserMethodService service = stub;
        var result = service.Calculate(10, 3);

        // Assert - Returns constant value
        Assert.Equal(999, result);
    }

    #endregion

    #region Tracking Still Works With OnCall

    [Fact]
    public void LastArg_TracksWithOnCallConfigured()
    {
        // Arrange
        var stub = new StrictModeUserMethodStub();
        stub.GetValue.OnCall(x => x * 100);

        // Act
        IStrictModeUserMethodTest service = stub;
        service.GetValue(7);

        // Assert - LastArg still tracks even with OnCall
        Assert.Equal(7, stub.GetValue.LastArg);
    }

    [Fact]
    public void Verifiable_WorksWithOnCall()
    {
        // Arrange
        var stub = new StrictModeUserMethodStub();
        stub.GetValue.OnCall(x => 42).Verifiable();

        // Act - Don't call the method

        // Assert - Verifiable still works
        Assert.Throws<VerificationException>(() => stub.Verify());
    }

    [Fact]
    public void Verify_WorksWithOnCall()
    {
        // Arrange
        var stub = new StrictModeUserMethodStub();
        stub.GetValue.OnCall(x => x);

        // Act
        IStrictModeUserMethodTest service = stub;
        service.GetValue(1);
        service.GetValue(2);

        // Assert
        stub.GetValue.Verify(Times.Exactly(2));
    }

    #endregion

    #region Async User Methods With OnCall

    [Fact]
    public async Task OnCall_SupersedesAsyncUserMethod()
    {
        // Arrange
        var stub = new AsyncUserMethodTestStub();
        stub.ProcessAsync.OnCall(input => Task.FromResult($"[OnCall: {input}]"));

        // Act
        IAsyncUserMethodTestService service = stub;
        var result = await service.ProcessAsync("test");

        // Assert - OnCall wins over user method
        Assert.Equal("[OnCall: test]", result);
    }

    [Fact]
    public async Task Returns_SupersedesAsyncUserMethod_WithAutoWrap()
    {
        // Arrange
        var stub = new AsyncUserMethodTestStub();
        stub.ProcessAsync.Returns("constant value"); // Returns auto-wraps in Task.FromResult

        // Act
        IAsyncUserMethodTestService service = stub;
        var result = await service.ProcessAsync("ignored");

        // Assert
        Assert.Equal("constant value", result);
    }

    [Fact]
    public async Task AsyncUserMethod_FallbackWhenNoCallback()
    {
        // Arrange
        var stub = new AsyncUserMethodTestStub();
        // No OnCall configured

        // Act
        IAsyncUserMethodTestService service = stub;
        var result = await service.ProcessAsync("hello");

        // Assert - User method is called (returns "[Async: hello]")
        Assert.Equal("[Async: hello]", result);
    }

    [Fact]
    public async Task ValueTask_Returns_WithAutoWrap()
    {
        // Arrange
        var stub = new AsyncUserMethodTestStub();
        stub.ComputeAsync.Returns(42); // Returns auto-wraps in new ValueTask<int>()

        // Act
        IAsyncUserMethodTestService service = stub;
        var result = await service.ComputeAsync(999);

        // Assert
        Assert.Equal(42, result);
    }

    #endregion
}

#region Test Interfaces and Stubs

/// <summary>Interface for multi-parameter user method tests.</summary>
public interface IMultiParamUserMethodService
{
    int Calculate(int a, int b);
}

/// <summary>Stub with multi-parameter user method.</summary>
[KnockOff]
public partial class MultiParamUserMethodStub : IMultiParamUserMethodService
{
}

public partial class MultiParamUserMethodStub
{
    protected override int Calculate_(int a, int b)
    {
        return a + b; // Default: addition
    }
}

/// <summary>Interface for async user method tests.</summary>
public interface IAsyncUserMethodTestService
{
    Task<string> ProcessAsync(string input);
    ValueTask<int> ComputeAsync(int value);
}

/// <summary>Stub with async user methods.</summary>
[KnockOff]
public partial class AsyncUserMethodTestStub : IAsyncUserMethodTestService
{
}

public partial class AsyncUserMethodTestStub
{
    protected override async Task<string> ProcessAsync_(string input)
    {
        await Task.Yield();
        return $"[Async: {input}]";
    }

    protected override async ValueTask<int> ComputeAsync_(int value)
    {
        await Task.Yield();
        return value * 2;
    }
}

#endregion
