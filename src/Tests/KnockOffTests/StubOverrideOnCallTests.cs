using KnockOff;
using System.Threading.Tasks;
using Xunit;

namespace KnockOff.Tests;

/// <summary>
/// Tests for OnCall/Returns on stub override interceptors.
/// OnCall supersedes the stub override; stub override is the fallback.
/// </summary>
public class StubOverrideOnCallTests
{
    #region OnCall Supersedes Stub Override

    [Fact]
    public void OnCall_SupersedesStubOverride_NonVoid()
    {
        // Arrange
        var stub = new StrictModeStubOverrideStub();
        stub.GetValue.Call(x => x * 100); // Override stub override (which does x * 10)

        // Act
        IStrictModeStubOverrideTest service = stub;
        var result = service.GetValue(5);

        // Assert - OnCall wins over stub override
        Assert.Equal(500, result); // 5 * 100, not 5 * 10
    }

    [Fact]
    public void OnCall_SupersedesStubOverride_Void()
    {
        // Arrange
        var stub = new StrictModeStubOverrideStub();
        var callbackInvoked = false;
        stub.DoSomething.Call(() => callbackInvoked = true);

        // Act
        IStrictModeStubOverrideTest service = stub;
        service.DoSomething();

        // Assert - OnCall was invoked
        Assert.True(callbackInvoked);
    }

    #endregion

    #region Returns Supersedes Stub Override

    [Fact]
    public void Returns_SupersedesStubOverride_NonVoid()
    {
        // Arrange
        var stub = new StrictModeStubOverrideStub();
        stub.GetValue.Return(999); // Override stub override

        // Act
        IStrictModeStubOverrideTest service = stub;
        var result = service.GetValue(5);

        // Assert - Returns wins over stub override
        Assert.Equal(999, result); // Not 50 (stub override: x * 10)
    }

    #endregion

    #region Stub Override Fallback

    [Fact]
    public void StubOverride_IsCalledWhenNoCallback()
    {
        // Arrange
        var stub = new StrictModeStubOverrideStub();
        // No OnCall configured

        // Act
        IStrictModeStubOverrideTest service = stub;
        var result = service.GetValue(5);

        // Assert - Stub override is called as fallback
        Assert.Equal(50, result); // Stub override: x * 10
    }

    [Fact]
    public void StubOverride_IsCalledWhenNoCallback_Void()
    {
        // Arrange
        var stub = new StrictModeStubOverrideStub();
        // No OnCall configured

        // Act
        IStrictModeStubOverrideTest service = stub;
        service.DoSomething(); // Should not throw

        // Assert - Verify it was tracked
        stub.DoSomething.Verify(Called.Once);
    }

    #endregion

    #region Reset Preserves OnCall Configuration

    [Fact]
    public void Reset_PreservesOnCallConfiguration()
    {
        // Arrange
        var stub = new StrictModeStubOverrideStub();
        stub.GetValue.Call(x => x * 100);

        IStrictModeStubOverrideTest service = stub;
        service.GetValue(5);
        stub.GetValue.Verify(Called.Once);

        // Act - Reset tracking state
        stub.GetValue.Reset();

        // Assert - OnCall configuration is preserved, tracking is cleared
        stub.GetValue.Verify(Called.Never);
        var result = service.GetValue(3);
        Assert.Equal(300, result); // Still uses OnCall (3 * 100)
    }

    [Fact]
    public void Reset_ClearsTrackingState()
    {
        // Arrange
        var stub = new StrictModeStubOverrideStub();
        stub.GetValue.Return(42);

        IStrictModeStubOverrideTest service = stub;
        service.GetValue(1);
        service.GetValue(2);
        stub.GetValue.Verify(Called.Exactly(2));

        // Act
        stub.GetValue.Reset();

        // Assert - Call count is cleared
        stub.GetValue.Verify(Called.Never);
    }

    #endregion

    #region Multiple Parameters With OnCall

    [Fact]
    public void OnCall_WithMultipleParameters()
    {
        // Arrange
        var stub = new MultiParamStubOverrideStub();
        stub.Calculate.Call((int a, int b) => a - b); // Override stub override (which does a + b)

        // Act
        IMultiParamStubOverrideService service = stub;
        var result = service.Calculate(10, 3);

        // Assert - OnCall wins (subtraction instead of addition)
        Assert.Equal(7, result); // 10 - 3, not 10 + 3
    }

    [Fact]
    public void Returns_WithMultipleParameters()
    {
        // Arrange
        var stub = new MultiParamStubOverrideStub();
        stub.Calculate.Return(999);

        // Act
        IMultiParamStubOverrideService service = stub;
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
        var stub = new StrictModeStubOverrideStub();
        stub.GetValue.Call(x => x * 100);

        // Act
        IStrictModeStubOverrideTest service = stub;
        service.GetValue(7);

        // Assert - LastArg still tracks even with OnCall
        Assert.Equal(7, stub.GetValue.LastArg);
    }

    [Fact]
    public void Verifiable_WorksWithOnCall()
    {
        // Arrange
        var stub = new StrictModeStubOverrideStub();
        stub.GetValue.Call(x => 42).Verifiable();

        // Act - Don't call the method

        // Assert - Verifiable still works
        Assert.Throws<VerificationException>(() => stub.Verify());
    }

    [Fact]
    public void Verify_WorksWithOnCall()
    {
        // Arrange
        var stub = new StrictModeStubOverrideStub();
        stub.GetValue.Call(x => x);

        // Act
        IStrictModeStubOverrideTest service = stub;
        service.GetValue(1);
        service.GetValue(2);

        // Assert
        stub.GetValue.Verify(Called.Exactly(2));
    }

    #endregion

    #region Async Stub Overrides With OnCall

    [Fact]
    public async Task OnCall_SupersedesAsyncStubOverride()
    {
        // Arrange
        var stub = new AsyncStubOverrideTestStub();
        stub.ProcessAsync.Call(input => Task.FromResult($"[OnCall: {input}]"));

        // Act
        IAsyncStubOverrideTestService service = stub;
        var result = await service.ProcessAsync("test");

        // Assert - OnCall wins over stub override
        Assert.Equal("[OnCall: test]", result);
    }

    [Fact]
    public async Task Returns_SupersedesAsyncStubOverride_WithAutoWrap()
    {
        // Arrange
        var stub = new AsyncStubOverrideTestStub();
        stub.ProcessAsync.Return("constant value"); // Returns auto-wraps in Task.FromResult

        // Act
        IAsyncStubOverrideTestService service = stub;
        var result = await service.ProcessAsync("ignored");

        // Assert
        Assert.Equal("constant value", result);
    }

    [Fact]
    public async Task AsyncStubOverride_FallbackWhenNoCallback()
    {
        // Arrange
        var stub = new AsyncStubOverrideTestStub();
        // No OnCall configured

        // Act
        IAsyncStubOverrideTestService service = stub;
        var result = await service.ProcessAsync("hello");

        // Assert - Stub override is called (returns "[Async: hello]")
        Assert.Equal("[Async: hello]", result);
    }

    [Fact]
    public async Task ValueTask_Returns_WithAutoWrap()
    {
        // Arrange
        var stub = new AsyncStubOverrideTestStub();
        stub.ComputeAsync.Return(42); // Returns auto-wraps in new ValueTask<int>()

        // Act
        IAsyncStubOverrideTestService service = stub;
        var result = await service.ComputeAsync(999);

        // Assert
        Assert.Equal(42, result);
    }

    #endregion
}

#region Test Interfaces and Stubs

/// <summary>Interface for multi-parameter stub override tests.</summary>
public interface IMultiParamStubOverrideService
{
    int Calculate(int a, int b);
}

/// <summary>Stub with multi-parameter stub override.</summary>
[KnockOff]
public partial class MultiParamStubOverrideStub : IMultiParamStubOverrideService
{
}

public partial class MultiParamStubOverrideStub
{
    protected override int Calculate_(int a, int b)
    {
        return a + b; // Default: addition
    }
}

/// <summary>Interface for async stub override tests.</summary>
public interface IAsyncStubOverrideTestService
{
    Task<string> ProcessAsync(string input);
    ValueTask<int> ComputeAsync(int value);
}

/// <summary>Stub with async stub overrides.</summary>
[KnockOff]
public partial class AsyncStubOverrideTestStub : IAsyncStubOverrideTestService
{
}

public partial class AsyncStubOverrideTestStub
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
