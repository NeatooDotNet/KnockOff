using KnockOff.Interceptors;

namespace KnockOff.Tests.Interceptors;

// Test delegates for MethodInterceptor<TDelegate, TArgs, TReturn>
delegate int ProcessIntDelegate(int x);
delegate int AddDelegate(int a, string b);

/// <summary>
/// Tests for MethodInterceptor&lt;TDelegate, TArgs, TReturn&gt; TTuple interceptor type.
/// Verifies all behavioral features: Return, When, sequences, verification, fallbacks.
/// Tests both 1-param (raw type TArgs) and 2-param (ValueTuple TArgs) cases.
/// </summary>
public class MethodInterceptorTests
{
    // ========================================================================
    // Return with value
    // ========================================================================

    [Fact]
    public void Return_WithValue_ReturnsValueOnInvoke()
    {
        var interceptor = new MethodInterceptor<ProcessIntDelegate, int, int>("Process");
        interceptor.Return(42);

        var result = interceptor.Invoke(false, 0);

        Assert.Equal(42, result);
    }

    [Fact]
    public void Return_WithValue_TwoParam_ReturnsValueOnInvoke()
    {
        var interceptor = new MethodInterceptor<AddDelegate, (int a, string b), int>("Add");
        interceptor.Return(99);

        var result = interceptor.Invoke(false, (1, "hello"));

        Assert.Equal(99, result);
    }

    // ========================================================================
    // Return with delegate callback
    // ========================================================================

    [Fact]
    public void Return_WithDelegate_CallsDelegateViaExpressionTree()
    {
        var interceptor = new MethodInterceptor<ProcessIntDelegate, int, int>("Process");
        ProcessIntDelegate callback = (int x) => x * 2;
        interceptor.Return(callback);

        var result = interceptor.Invoke(false, 21);

        Assert.Equal(42, result);
    }

    [Fact]
    public void Return_WithDelegate_TwoParam_CallsDelegateViaExpressionTree()
    {
        var interceptor = new MethodInterceptor<AddDelegate, (int a, string b), int>("Add");
        AddDelegate callback = (int a, string b) => a + b.Length;
        interceptor.Return(callback);

        var result = interceptor.Invoke(false, (10, "hello"));

        Assert.Equal(15, result);
    }

    // ========================================================================
    // When with exact match
    // ========================================================================

    [Fact]
    public void When_ExactMatch_SingleParam_ReturnsConfiguredValue()
    {
        var interceptor = new MethodInterceptor<ProcessIntDelegate, int, int>("Process");
        interceptor.When(5).Return(50);

        var result = interceptor.Invoke(false, 5);

        Assert.Equal(50, result);
    }

    [Fact]
    public void When_ExactMatch_TwoParam_ReturnsConfiguredValue()
    {
        var interceptor = new MethodInterceptor<AddDelegate, (int a, string b), int>("Add");
        interceptor.When((1, "hello")).Return(42);

        var result = interceptor.Invoke(false, (1, "hello"));

        Assert.Equal(42, result);
    }

    [Fact]
    public void When_NoMatch_ReturnsDefault()
    {
        var interceptor = new MethodInterceptor<ProcessIntDelegate, int, int>("Process");
        interceptor.When(5).Return(50);

        // First call matches, advances chain head. Second call doesn't match 5, hits unconfigured.
        interceptor.Invoke(false, 5);
        var result = interceptor.Invoke(false, 999);

        Assert.Equal(0, result); // default(int) = 0
    }

    // ========================================================================
    // When with predicate
    // ========================================================================

    [Fact]
    public void When_Predicate_SingleParam_ReturnsConfiguredValue()
    {
        var interceptor = new MethodInterceptor<ProcessIntDelegate, int, int>("Process");
        interceptor.When(x => x > 10).Return(100);

        var result = interceptor.Invoke(false, 15);

        Assert.Equal(100, result);
    }

    [Fact]
    public void When_Predicate_TwoParam_ReturnsConfiguredValue()
    {
        var interceptor = new MethodInterceptor<AddDelegate, (int a, string b), int>("Add");
        interceptor.When(args => args.a > 5 && args.b.Length > 2).Return(999);

        var result = interceptor.Invoke(false, (10, "hello"));

        Assert.Equal(999, result);
    }

    // ========================================================================
    // Sequence
    // ========================================================================

    [Fact]
    public void Return_Sequence_ReturnsValuesInOrder()
    {
        var interceptor = new MethodInterceptor<ProcessIntDelegate, int, int>("Process");
        interceptor.Return(1, 2, 3);

        var r1 = interceptor.Invoke(false, 0);
        var r2 = interceptor.Invoke(false, 0);
        var r3 = interceptor.Invoke(false, 0);

        Assert.Equal(1, r1);
        Assert.Equal(2, r2);
        Assert.Equal(3, r3);
    }

    [Fact]
    public void Return_Sequence_RepeatsLastValueByDefault()
    {
        var interceptor = new MethodInterceptor<ProcessIntDelegate, int, int>("Process");
        interceptor.Return(1, 2);

        interceptor.Invoke(false, 0); // 1
        interceptor.Invoke(false, 0); // 2
        var r3 = interceptor.Invoke(false, 0); // repeats 2

        Assert.Equal(2, r3);
    }

    [Fact]
    public void Return_Sequence_ThenDefault_ReturnsDefaultAfterExhaustion()
    {
        var interceptor = new MethodInterceptor<ProcessIntDelegate, int, int>("Process");
        interceptor.Return(1, 2).ThenDefault();

        interceptor.Invoke(false, 0); // 1
        interceptor.Invoke(false, 0); // 2
        var r3 = interceptor.Invoke(false, 0); // default after exhaustion

        Assert.Equal(0, r3); // default(int) = 0
    }

    [Fact]
    public void Return_ThenReturn_BuildsSequence()
    {
        var interceptor = new MethodInterceptor<ProcessIntDelegate, int, int>("Process");
        ProcessIntDelegate first = (int x) => x;
        ProcessIntDelegate second = (int x) => x * 10;
        interceptor.Return(first).ThenReturn(second);

        var r1 = interceptor.Invoke(false, 5);
        var r2 = interceptor.Invoke(false, 3);

        Assert.Equal(5, r1);
        Assert.Equal(30, r2);
    }

    // ========================================================================
    // LastArgs
    // ========================================================================

    [Fact]
    public void LastArgs_SingleParam_RecordsArgsAfterInvoke()
    {
        var interceptor = new MethodInterceptor<ProcessIntDelegate, int, int>("Process");
        interceptor.Return(0);

        interceptor.Invoke(false, 42);

        Assert.Equal(42, interceptor.LastArgs);
    }

    [Fact]
    public void LastArgs_TwoParam_RecordsTupleArgsAfterInvoke()
    {
        var interceptor = new MethodInterceptor<AddDelegate, (int a, string b), int>("Add");
        interceptor.Return(0);

        interceptor.Invoke(false, (10, "world"));

        Assert.Equal((10, "world"), interceptor.LastArgs);
    }

    [Fact]
    public void LastArgs_Unconfigured_RecordsArgsForUnconfiguredCalls()
    {
        var interceptor = new MethodInterceptor<ProcessIntDelegate, int, int>("Process");

        interceptor.Invoke(false, 99);

        Assert.Equal(99, interceptor.LastArgs);
    }

    // ========================================================================
    // SetFallback
    // ========================================================================

    [Fact]
    public void SetFallback_InvokeUsesFallbackWhenUnconfigured()
    {
        var interceptor = new MethodInterceptor<ProcessIntDelegate, int, int>("Process");
        ProcessIntDelegate fallback = (int x) => x + 100;
        interceptor.SetFallback(fallback);

        var result = interceptor.Invoke(false, 5);

        Assert.Equal(105, result);
    }

    [Fact]
    public void SetFallback_TwoParam_InvokeUsesFallback()
    {
        var interceptor = new MethodInterceptor<AddDelegate, (int a, string b), int>("Add");
        AddDelegate fallback = (int a, string b) => a * b.Length;
        interceptor.SetFallback(fallback);

        var result = interceptor.Invoke(false, (3, "hello"));

        Assert.Equal(15, result);
    }

    // ========================================================================
    // SetSourceFallback
    // ========================================================================

    [Fact]
    public void SetSourceFallback_InvokesSourceFallbackWhenUnconfigured()
    {
        var interceptor = new MethodInterceptor<ProcessIntDelegate, int, int>("Process");
        ProcessIntDelegate sourceFallback = (int x) => x * 3;
        interceptor.SetSourceFallback(sourceFallback);

        var result = interceptor.Invoke(false, 7);

        Assert.Equal(21, result);
    }

    [Fact]
    public void SetFallback_TakesPrecedenceOverSourceFallback()
    {
        var interceptor = new MethodInterceptor<ProcessIntDelegate, int, int>("Process");
        ProcessIntDelegate fallback = (int x) => 100;
        ProcessIntDelegate sourceFallback = (int x) => 200;
        interceptor.SetFallback(fallback);
        interceptor.SetSourceFallback(sourceFallback);

        var result = interceptor.Invoke(false, 0);

        Assert.Equal(100, result);
    }

    // ========================================================================
    // Verify
    // ========================================================================

    [Fact]
    public void Verify_ThrowsWhenNotCalled()
    {
        var interceptor = new MethodInterceptor<ProcessIntDelegate, int, int>("Process");
        interceptor.Return(42);

        Assert.Throws<VerificationException>(() => interceptor.Verify());
    }

    [Fact]
    public void Verify_PassesWhenCalled()
    {
        var interceptor = new MethodInterceptor<ProcessIntDelegate, int, int>("Process");
        interceptor.Return(42);

        interceptor.Invoke(false, 0);

        interceptor.Verify(); // should not throw
    }

    [Fact]
    public void Verifiable_CheckVerification_ReturnsFailureWhenNotCalled()
    {
        var interceptor = new MethodInterceptor<ProcessIntDelegate, int, int>("Process");
        interceptor.Return(42).Verifiable();

        var failure = interceptor.CheckVerification();

        Assert.NotNull(failure);
    }

    [Fact]
    public void Verifiable_CheckVerification_ReturnsNullWhenCalled()
    {
        var interceptor = new MethodInterceptor<ProcessIntDelegate, int, int>("Process");
        interceptor.Return(42).Verifiable();

        interceptor.Invoke(false, 0);

        var failure = interceptor.CheckVerification();

        Assert.Null(failure);
    }

    [Fact]
    public void CheckVerificationAll_ReturnsNullWhenNotConfigured()
    {
        var interceptor = new MethodInterceptor<ProcessIntDelegate, int, int>("Process");

        var failure = interceptor.CheckVerificationAll();

        Assert.Null(failure);
    }

    [Fact]
    public void CheckVerificationAll_ReturnsFailureWhenConfiguredButNotCalled()
    {
        var interceptor = new MethodInterceptor<ProcessIntDelegate, int, int>("Process");
        interceptor.Return(42);

        var failure = interceptor.CheckVerificationAll();

        Assert.NotNull(failure);
    }

    // ========================================================================
    // Strict mode
    // ========================================================================

    [Fact]
    public void Invoke_StrictMode_ThrowsWhenUnconfigured()
    {
        var interceptor = new MethodInterceptor<ProcessIntDelegate, int, int>("Process");

        Assert.Throws<StubException>(() => interceptor.Invoke(true, 0));
    }

    [Fact]
    public void Invoke_StrictMode_SequenceExhausted_Throws()
    {
        var interceptor = new MethodInterceptor<ProcessIntDelegate, int, int>("Process");
        interceptor.Return(1, 2);

        interceptor.Invoke(true, 0); // 1
        interceptor.Invoke(true, 0); // 2
        Assert.Throws<StubException>(() => interceptor.Invoke(true, 0)); // exhausted
    }

    // ========================================================================
    // Reset
    // ========================================================================

    [Fact]
    public void Reset_ClearsTrackingState()
    {
        var interceptor = new MethodInterceptor<ProcessIntDelegate, int, int>("Process");
        interceptor.Return(42);

        interceptor.Invoke(false, 5);
        Assert.Equal(1, interceptor.TotalCallCount);

        interceptor.Reset();

        Assert.Equal(0, interceptor.TotalCallCount);
        Assert.Equal(0, interceptor.UnconfiguredCallCount);
    }

    // ========================================================================
    // Default factory
    // ========================================================================

    [Fact]
    public void DefaultFactory_UsedWhenUnconfiguredAndNotStrict()
    {
        var interceptor = new MethodInterceptor<ProcessIntDelegate, int, int>("Process", () => 999);

        var result = interceptor.Invoke(false, 0);

        Assert.Equal(999, result);
    }

    // ========================================================================
    // TotalCallCount and UnconfiguredCallCount
    // ========================================================================

    [Fact]
    public void TotalCallCount_IncludesAllCalls()
    {
        var interceptor = new MethodInterceptor<ProcessIntDelegate, int, int>("Process");
        interceptor.Return(42);

        interceptor.Invoke(false, 0);
        interceptor.Invoke(false, 0);

        Assert.Equal(2, interceptor.TotalCallCount);
    }

    [Fact]
    public void UnconfiguredCallCount_OnlyCountsUnconfiguredCalls()
    {
        var interceptor = new MethodInterceptor<ProcessIntDelegate, int, int>("Process");

        interceptor.Invoke(false, 0);
        interceptor.Invoke(false, 0);

        Assert.Equal(2, interceptor.UnconfiguredCallCount);
    }

    // ========================================================================
    // IsConfigured
    // ========================================================================

    [Fact]
    public void IsConfigured_FalseByDefault()
    {
        var interceptor = new MethodInterceptor<ProcessIntDelegate, int, int>("Process");

        Assert.False(interceptor.IsConfigured);
    }

    [Fact]
    public void IsConfigured_TrueAfterReturn()
    {
        var interceptor = new MethodInterceptor<ProcessIntDelegate, int, int>("Process");
        interceptor.Return(42);

        Assert.True(interceptor.IsConfigured);
    }

    // ========================================================================
    // When chain - ThenWhen
    // ========================================================================

    [Fact]
    public void When_ThenWhen_ChainsMultipleMatchers()
    {
        var interceptor = new MethodInterceptor<ProcessIntDelegate, int, int>("Process");
        interceptor.When(1).Return(10)
                   .ThenWhen(2).Return(20);

        var r1 = interceptor.Invoke(false, 1);
        var r2 = interceptor.Invoke(false, 2);

        Assert.Equal(10, r1);
        Assert.Equal(20, r2);
    }

    // ========================================================================
    // When chain - ThenCall (terminal)
    // ========================================================================

    [Fact]
    public void When_ThenCall_TerminalMatcher()
    {
        var interceptor = new MethodInterceptor<ProcessIntDelegate, int, int>("Process");
        ProcessIntDelegate callback = (int x) => x * 100;
        interceptor.When(1).Return(10)
                   .ThenCall(callback);

        var r1 = interceptor.Invoke(false, 1);
        var r2 = interceptor.Invoke(false, 5); // terminal matcher matches anything

        Assert.Equal(10, r1);
        Assert.Equal(500, r2);
    }

    // ========================================================================
    // When chain - ThenNone
    // ========================================================================

    [Fact]
    public void When_ThenNone_AdvancesChainPastTerminal()
    {
        var interceptor = new MethodInterceptor<ProcessIntDelegate, int, int>("Process");
        interceptor.When(1).Return(10)
                   .ThenNone();

        var r1 = interceptor.Invoke(false, 1);
        // After consuming first matcher, ThenNone is terminal-never-matches,
        // so chain head advances past it on next non-matching call
        var r2 = interceptor.Invoke(false, 99);

        Assert.Equal(10, r1);
        Assert.Equal(0, r2); // default, since ThenNone doesn't match
    }

    // ========================================================================
    // Builder interface: MethodCallBuilder tracking
    // ========================================================================

    [Fact]
    public void MethodCallBuilder_TracksLastArgs()
    {
        var interceptor = new MethodInterceptor<ProcessIntDelegate, int, int>("Process");
        var builder = interceptor.Return(42);

        interceptor.Invoke(false, 7);

        Assert.Equal(7, builder.LastArgs);
    }

    [Fact]
    public void MethodCallBuilder_Verify_ThrowsWhenNotCalled()
    {
        var interceptor = new MethodInterceptor<ProcessIntDelegate, int, int>("Process");
        var builder = interceptor.Return(42);

        Assert.Throws<VerificationException>(() => builder.Verify());
    }

    [Fact]
    public void MethodCallBuilder_Verify_PassesAfterCall()
    {
        var interceptor = new MethodInterceptor<ProcessIntDelegate, int, int>("Process");
        var builder = interceptor.Return(42);

        interceptor.Invoke(false, 0);

        builder.Verify(); // should not throw
    }
}
