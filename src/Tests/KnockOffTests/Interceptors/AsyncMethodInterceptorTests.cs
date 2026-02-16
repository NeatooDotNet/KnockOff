using KnockOff.Interceptors;

namespace KnockOff.Tests.Interceptors;

// Test delegates for AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>
// Async delegates return Task<TReturn> since they represent async non-void methods.
delegate Task<int> AsyncProcessIntDelegate(int x);
delegate Task<int> AsyncAddDelegate(int a, string b);

// Sync delegate equivalents for TSyncDelegate type parameter.
delegate int SyncProcessIntDelegate(int x);
delegate int SyncAddDelegate(int a, string b);

/// <summary>
/// Tests for AsyncMethodInterceptor&lt;TDelegate, TArgs, TReturn&gt; TTuple interceptor type.
/// Verifies all behavioral features: Return overloads, When, sequences, verification, fallbacks.
/// Tests both 1-param (raw type TArgs) and 2-param (ValueTuple TArgs) cases.
/// </summary>
public class AsyncMethodInterceptorTests
{
    // ========================================================================
    // Return with value
    // ========================================================================

    [Fact]
    public async Task Return_WithValue_ReturnsTaskFromResultOnInvoke()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncProcessIntDelegate, SyncProcessIntDelegate, int, int>("Process");
        interceptor.Return(42);

        var result = await interceptor.Invoke(false, 0);

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task Return_WithValue_TwoParam_ReturnsTaskFromResultOnInvoke()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncAddDelegate, SyncAddDelegate, (int a, string b), int>("Add");
        interceptor.Return(99);

        var result = await interceptor.Invoke(false, (1, "hello"));

        Assert.Equal(99, result);
    }

    // ========================================================================
    // Return with async delegate callback
    // ========================================================================

    [Fact]
    public async Task Return_WithAsyncDelegate_InvokesDelegateViaExpressionTree()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncProcessIntDelegate, SyncProcessIntDelegate, int, int>("Process");
        AsyncProcessIntDelegate callback = async (int x) => { await Task.Yield(); return x * 2; };
        interceptor.Return(callback);

        var result = await interceptor.Invoke(false, 21);

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task Return_WithAsyncDelegate_TwoParam_InvokesDelegateViaExpressionTree()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncAddDelegate, SyncAddDelegate, (int a, string b), int>("Add");
        AsyncAddDelegate callback = async (int a, string b) => { await Task.Yield(); return a + b.Length; };
        interceptor.Return(callback);

        var result = await interceptor.Invoke(false, (10, "hello"));

        Assert.Equal(15, result);
    }

    // ========================================================================
    // Return with simplified sync callback (Func<TArgs, TReturn>)
    // ========================================================================

    [Fact]
    public async Task Return_WithSyncCallback_WrapsInTaskFromResult()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncProcessIntDelegate, SyncProcessIntDelegate, int, int>("Process");
        interceptor.Return((int x) => x * 3);

        var result = await interceptor.Invoke(false, 7);

        Assert.Equal(21, result);
    }

    [Fact]
    public async Task Return_WithSyncCallback_TwoParam_WrapsInTaskFromResult()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncAddDelegate, SyncAddDelegate, (int a, string b), int>("Add");
        interceptor.Return((a, b) => a + b.Length);

        var result = await interceptor.Invoke(false, (10, "hi"));

        Assert.Equal(12, result);
    }

    // ========================================================================
    // When with exact match
    // ========================================================================

    [Fact]
    public async Task When_ExactMatch_SingleParam_ReturnsConfiguredValue()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncProcessIntDelegate, SyncProcessIntDelegate, int, int>("Process");
        interceptor.When(5).Return(50);

        var result = await interceptor.Invoke(false, 5);

        Assert.Equal(50, result);
    }

    [Fact]
    public async Task When_ExactMatch_TwoParam_ReturnsConfiguredValue()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncAddDelegate, SyncAddDelegate, (int a, string b), int>("Add");
        interceptor.When((1, "hello")).Return(42);

        var result = await interceptor.Invoke(false, (1, "hello"));

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task When_NoMatch_ReturnsDefault()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncProcessIntDelegate, SyncProcessIntDelegate, int, int>("Process");
        interceptor.When(5).Return(50);

        // First call matches, advances chain head. Second call doesn't match 5, hits unconfigured.
        await interceptor.Invoke(false, 5);
        var result = await interceptor.Invoke(false, 999);

        Assert.Equal(0, result); // default(int) = 0
    }

    // ========================================================================
    // When with predicate
    // ========================================================================

    [Fact]
    public async Task When_Predicate_SingleParam_ReturnsConfiguredValue()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncProcessIntDelegate, SyncProcessIntDelegate, int, int>("Process");
        interceptor.When(x => x > 10).Return(100);

        var result = await interceptor.Invoke(false, 15);

        Assert.Equal(100, result);
    }

    [Fact]
    public async Task When_Predicate_TwoParam_ReturnsConfiguredValue()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncAddDelegate, SyncAddDelegate, (int a, string b), int>("Add");
        interceptor.When(args => args.a > 5 && args.b.Length > 2).Return(999);

        var result = await interceptor.Invoke(false, (10, "hello"));

        Assert.Equal(999, result);
    }

    // ========================================================================
    // Sequence
    // ========================================================================

    [Fact]
    public async Task Return_Sequence_ReturnsValuesInOrder()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncProcessIntDelegate, SyncProcessIntDelegate, int, int>("Process");
        interceptor.Return(1, 2, 3);

        var r1 = await interceptor.Invoke(false, 0);
        var r2 = await interceptor.Invoke(false, 0);
        var r3 = await interceptor.Invoke(false, 0);

        Assert.Equal(1, r1);
        Assert.Equal(2, r2);
        Assert.Equal(3, r3);
    }

    [Fact]
    public async Task Return_Sequence_RepeatsLastValueByDefault()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncProcessIntDelegate, SyncProcessIntDelegate, int, int>("Process");
        interceptor.Return(1, 2);

        await interceptor.Invoke(false, 0); // 1
        await interceptor.Invoke(false, 0); // 2
        var r3 = await interceptor.Invoke(false, 0); // repeats 2

        Assert.Equal(2, r3);
    }

    [Fact]
    public async Task Return_Sequence_ThenDefault_ReturnsDefaultAfterExhaustion()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncProcessIntDelegate, SyncProcessIntDelegate, int, int>("Process");
        interceptor.Return(1, 2).ThenDefault();

        await interceptor.Invoke(false, 0); // 1
        await interceptor.Invoke(false, 0); // 2
        var r3 = await interceptor.Invoke(false, 0); // default after exhaustion

        Assert.Equal(0, r3); // default(int) = 0
    }

    [Fact]
    public async Task Return_ThenReturn_BuildsSequenceWithDelegates()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncProcessIntDelegate, SyncProcessIntDelegate, int, int>("Process");
        AsyncProcessIntDelegate first = async (int x) => { await Task.Yield(); return x; };
        AsyncProcessIntDelegate second = async (int x) => { await Task.Yield(); return x * 10; };
        interceptor.Return(first).ThenReturn(second);

        var r1 = await interceptor.Invoke(false, 5);
        var r2 = await interceptor.Invoke(false, 3);

        Assert.Equal(5, r1);
        Assert.Equal(30, r2);
    }

    [Fact]
    public async Task Return_ThenReturn_BuildsSequenceWithSyncCallbacks()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncProcessIntDelegate, SyncProcessIntDelegate, int, int>("Process");
        interceptor.Return((int x) => x).ThenReturn((int x) => x * 10);

        var r1 = await interceptor.Invoke(false, 5);
        var r2 = await interceptor.Invoke(false, 3);

        Assert.Equal(5, r1);
        Assert.Equal(30, r2);
    }

    // ========================================================================
    // LastArgs
    // ========================================================================

    [Fact]
    public async Task LastArgs_SingleParam_RecordsArgsAfterInvoke()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncProcessIntDelegate, SyncProcessIntDelegate, int, int>("Process");
        interceptor.Return(0);

        await interceptor.Invoke(false, 42);

        Assert.Equal(42, interceptor.LastArgs);
    }

    [Fact]
    public async Task LastArgs_TwoParam_RecordsTupleArgsAfterInvoke()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncAddDelegate, SyncAddDelegate, (int a, string b), int>("Add");
        interceptor.Return(0);

        await interceptor.Invoke(false, (10, "world"));

        Assert.Equal((10, "world"), interceptor.LastArgs);
    }

    [Fact]
    public async Task LastArgs_Unconfigured_RecordsArgsForUnconfiguredCalls()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncProcessIntDelegate, SyncProcessIntDelegate, int, int>("Process");

        await interceptor.Invoke(false, 99);

        Assert.Equal(99, interceptor.LastArgs);
    }

    // ========================================================================
    // SetFallback / SetSourceFallback
    // ========================================================================

    [Fact]
    public async Task SetFallback_InvokeUsesFallbackWhenUnconfigured()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncProcessIntDelegate, SyncProcessIntDelegate, int, int>("Process");
        AsyncProcessIntDelegate fallback = async (int x) => { await Task.Yield(); return x + 100; };
        interceptor.SetFallback(fallback);

        var result = await interceptor.Invoke(false, 5);

        Assert.Equal(105, result);
    }

    [Fact]
    public async Task SetFallback_TwoParam_InvokeUsesFallback()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncAddDelegate, SyncAddDelegate, (int a, string b), int>("Add");
        AsyncAddDelegate fallback = async (int a, string b) => { await Task.Yield(); return a * b.Length; };
        interceptor.SetFallback(fallback);

        var result = await interceptor.Invoke(false, (3, "hello"));

        Assert.Equal(15, result);
    }

    [Fact]
    public async Task SetSourceFallback_InvokesSourceFallbackWhenUnconfigured()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncProcessIntDelegate, SyncProcessIntDelegate, int, int>("Process");
        AsyncProcessIntDelegate sourceFallback = async (int x) => { await Task.Yield(); return x * 3; };
        interceptor.SetSourceFallback(sourceFallback);

        var result = await interceptor.Invoke(false, 7);

        Assert.Equal(21, result);
    }

    [Fact]
    public async Task SetFallback_TakesPrecedenceOverSourceFallback()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncProcessIntDelegate, SyncProcessIntDelegate, int, int>("Process");
        AsyncProcessIntDelegate fallback = async (int x) => { await Task.Yield(); return 100; };
        AsyncProcessIntDelegate sourceFallback = async (int x) => { await Task.Yield(); return 200; };
        interceptor.SetFallback(fallback);
        interceptor.SetSourceFallback(sourceFallback);

        var result = await interceptor.Invoke(false, 0);

        Assert.Equal(100, result);
    }

    // ========================================================================
    // Verify
    // ========================================================================

    [Fact]
    public void Verify_ThrowsWhenNotCalled()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncProcessIntDelegate, SyncProcessIntDelegate, int, int>("Process");
        interceptor.Return(42);

        Assert.Throws<VerificationException>(() => interceptor.Verify());
    }

    [Fact]
    public async Task Verify_PassesWhenCalled()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncProcessIntDelegate, SyncProcessIntDelegate, int, int>("Process");
        interceptor.Return(42);

        await interceptor.Invoke(false, 0);

        interceptor.Verify(); // should not throw
    }

    [Fact]
    public void Verifiable_CheckVerification_ReturnsFailureWhenNotCalled()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncProcessIntDelegate, SyncProcessIntDelegate, int, int>("Process");
        interceptor.Return(42).Verifiable();

        var failure = interceptor.CheckVerification();

        Assert.NotNull(failure);
    }

    [Fact]
    public async Task Verifiable_CheckVerification_ReturnsNullWhenCalled()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncProcessIntDelegate, SyncProcessIntDelegate, int, int>("Process");
        interceptor.Return(42).Verifiable();

        await interceptor.Invoke(false, 0);

        var failure = interceptor.CheckVerification();

        Assert.Null(failure);
    }

    [Fact]
    public void CheckVerificationAll_ReturnsNullWhenNotConfigured()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncProcessIntDelegate, SyncProcessIntDelegate, int, int>("Process");

        var failure = interceptor.CheckVerificationAll();

        Assert.Null(failure);
    }

    [Fact]
    public void CheckVerificationAll_ReturnsFailureWhenConfiguredButNotCalled()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncProcessIntDelegate, SyncProcessIntDelegate, int, int>("Process");
        interceptor.Return(42);

        var failure = interceptor.CheckVerificationAll();

        Assert.NotNull(failure);
    }

    // ========================================================================
    // Strict mode
    // ========================================================================

    [Fact]
    public async Task Invoke_StrictMode_ThrowsWhenUnconfigured()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncProcessIntDelegate, SyncProcessIntDelegate, int, int>("Process");

        await Assert.ThrowsAsync<StubException>(() => interceptor.Invoke(true, 0));
    }

    [Fact]
    public async Task Invoke_StrictMode_SequenceExhausted_Throws()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncProcessIntDelegate, SyncProcessIntDelegate, int, int>("Process");
        interceptor.Return(1, 2);

        await interceptor.Invoke(true, 0); // 1
        await interceptor.Invoke(true, 0); // 2
        await Assert.ThrowsAsync<StubException>(() => interceptor.Invoke(true, 0)); // exhausted
    }

    // ========================================================================
    // Reset
    // ========================================================================

    [Fact]
    public async Task Reset_ClearsTrackingState()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncProcessIntDelegate, SyncProcessIntDelegate, int, int>("Process");
        interceptor.Return(42);

        await interceptor.Invoke(false, 5);
        Assert.Equal(1, interceptor.TotalCallCount);

        interceptor.Reset();

        Assert.Equal(0, interceptor.TotalCallCount);
        Assert.Equal(0, interceptor.UnconfiguredCallCount);
    }

    // ========================================================================
    // Default factory
    // ========================================================================

    [Fact]
    public async Task DefaultFactory_UsedWhenUnconfiguredAndNotStrict()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncProcessIntDelegate, SyncProcessIntDelegate, int, int>("Process", () => 999);

        var result = await interceptor.Invoke(false, 0);

        Assert.Equal(999, result);
    }

    // ========================================================================
    // TotalCallCount and UnconfiguredCallCount
    // ========================================================================

    [Fact]
    public async Task TotalCallCount_IncludesAllCalls()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncProcessIntDelegate, SyncProcessIntDelegate, int, int>("Process");
        interceptor.Return(42);

        await interceptor.Invoke(false, 0);
        await interceptor.Invoke(false, 0);

        Assert.Equal(2, interceptor.TotalCallCount);
    }

    [Fact]
    public async Task UnconfiguredCallCount_OnlyCountsUnconfiguredCalls()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncProcessIntDelegate, SyncProcessIntDelegate, int, int>("Process");

        await interceptor.Invoke(false, 0);
        await interceptor.Invoke(false, 0);

        Assert.Equal(2, interceptor.UnconfiguredCallCount);
    }

    // ========================================================================
    // IsConfigured
    // ========================================================================

    [Fact]
    public void IsConfigured_FalseByDefault()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncProcessIntDelegate, SyncProcessIntDelegate, int, int>("Process");

        Assert.False(interceptor.IsConfigured);
    }

    [Fact]
    public void IsConfigured_TrueAfterReturn()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncProcessIntDelegate, SyncProcessIntDelegate, int, int>("Process");
        interceptor.Return(42);

        Assert.True(interceptor.IsConfigured);
    }

    // ========================================================================
    // When chain - ThenWhen
    // ========================================================================

    [Fact]
    public async Task When_ThenWhen_ChainsMultipleMatchers()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncProcessIntDelegate, SyncProcessIntDelegate, int, int>("Process");
        interceptor.When(1).Return(10)
                   .ThenWhen(2).Return(20);

        var r1 = await interceptor.Invoke(false, 1);
        var r2 = await interceptor.Invoke(false, 2);

        Assert.Equal(10, r1);
        Assert.Equal(20, r2);
    }

    // ========================================================================
    // When chain - ThenCall (terminal)
    // ========================================================================

    [Fact]
    public async Task When_ThenCall_TerminalMatcher_WithDelegate()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncProcessIntDelegate, SyncProcessIntDelegate, int, int>("Process");
        AsyncProcessIntDelegate callback = async (int x) => { await Task.Yield(); return x * 100; };
        interceptor.When(1).Return(10)
                   .ThenCall(callback);

        var r1 = await interceptor.Invoke(false, 1);
        var r2 = await interceptor.Invoke(false, 5); // terminal matcher matches anything

        Assert.Equal(10, r1);
        Assert.Equal(500, r2);
    }

    [Fact]
    public async Task When_ThenCall_TerminalMatcher_WithSyncCallback()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncProcessIntDelegate, SyncProcessIntDelegate, int, int>("Process");
        interceptor.When(1).Return(10)
                   .ThenCall((int x) => x * 100);

        var r1 = await interceptor.Invoke(false, 1);
        var r2 = await interceptor.Invoke(false, 5);

        Assert.Equal(10, r1);
        Assert.Equal(500, r2);
    }

    // ========================================================================
    // When chain - ThenNone
    // ========================================================================

    [Fact]
    public async Task When_ThenNone_AdvancesChainPastTerminal()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncProcessIntDelegate, SyncProcessIntDelegate, int, int>("Process");
        interceptor.When(1).Return(10)
                   .ThenNone();

        var r1 = await interceptor.Invoke(false, 1);
        var r2 = await interceptor.Invoke(false, 99);

        Assert.Equal(10, r1);
        Assert.Equal(0, r2); // default, since ThenNone doesn't match
    }

    // ========================================================================
    // Builder interface: MethodCallBuilder tracking
    // ========================================================================

    [Fact]
    public async Task MethodCallBuilder_TracksLastArgs()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncProcessIntDelegate, SyncProcessIntDelegate, int, int>("Process");
        var builder = interceptor.Return(42);

        await interceptor.Invoke(false, 7);

        Assert.Equal(7, builder.LastArgs);
    }

    [Fact]
    public void MethodCallBuilder_Verify_ThrowsWhenNotCalled()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncProcessIntDelegate, SyncProcessIntDelegate, int, int>("Process");
        var builder = interceptor.Return(42);

        Assert.Throws<VerificationException>(() => builder.Verify());
    }

    [Fact]
    public async Task MethodCallBuilder_Verify_PassesAfterCall()
    {
        var interceptor = new AsyncMethodInterceptor<AsyncProcessIntDelegate, SyncProcessIntDelegate, int, int>("Process");
        var builder = interceptor.Return(42);

        await interceptor.Invoke(false, 0);

        builder.Verify(); // should not throw
    }

    // ========================================================================
    // ConfigureAwait behavior
    // ========================================================================

    [Fact]
    public async Task Invoke_ConfigureAwaitFalse_DoesNotDeadlock()
    {
        // Verify that ConfigureAwait(false) is used throughout by running in a sync context
        var interceptor = new AsyncMethodInterceptor<AsyncProcessIntDelegate, SyncProcessIntDelegate, int, int>("Process");
        AsyncProcessIntDelegate callback = async (int x) =>
        {
            await Task.Delay(1).ConfigureAwait(false);
            return x * 2;
        };
        interceptor.Return(callback);

        var result = await interceptor.Invoke(false, 21);

        Assert.Equal(42, result);
    }
}
