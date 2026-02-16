using KnockOff.Interceptors;

namespace KnockOff.Tests.Interceptors;

// Test delegates for AsyncVoidMethodInterceptor<TDelegate, TArgs>
// These return Task since they represent async void methods.
delegate Task AsyncVoidProcessDelegate(int x);
delegate Task AsyncVoidExecuteDelegate(int count, string name);

/// <summary>
/// Tests for AsyncVoidMethodInterceptor&lt;TDelegate, TArgs&gt; TTuple interceptor type.
/// Verifies all behavioral features: Call overloads, When, sequences, verification, fallbacks.
/// Tests both 1-param (raw type TArgs) and 2-param (ValueTuple TArgs) cases.
/// </summary>
public class AsyncVoidMethodInterceptorTests
{
    // ========================================================================
    // Call with async delegate
    // ========================================================================

    [Fact]
    public async Task Call_WithAsyncDelegate_InvokesDelegateViaExpressionTree()
    {
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidProcessDelegate, int>("Process");
        int captured = 0;
        AsyncVoidProcessDelegate callback = async (int x) => { await Task.Yield(); captured = x; };
        interceptor.Call(callback);

        await interceptor.Invoke(false, 42);

        Assert.Equal(42, captured);
    }

    [Fact]
    public async Task Call_WithAsyncDelegate_TwoParam_InvokesDelegateViaExpressionTree()
    {
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidExecuteDelegate, (int count, string name)>("Execute");
        int capturedCount = 0;
        string capturedName = "";
        AsyncVoidExecuteDelegate callback = async (int count, string name) =>
        {
            await Task.Yield();
            capturedCount = count;
            capturedName = name;
        };
        interceptor.Call(callback);

        await interceptor.Invoke(false, (5, "test"));

        Assert.Equal(5, capturedCount);
        Assert.Equal("test", capturedName);
    }

    // ========================================================================
    // Call with simplified sync callback (Action<TArgs>)
    // ========================================================================

    [Fact]
    public async Task Call_WithSyncCallback_WrapsInTaskCompletedTask()
    {
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidProcessDelegate, int>("Process");
        int captured = 0;
        interceptor.Call((int x) => captured = x);

        await interceptor.Invoke(false, 42);

        Assert.Equal(42, captured);
    }

    [Fact]
    public async Task Call_WithSyncCallback_TwoParam_WrapsInTaskCompletedTask()
    {
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidExecuteDelegate, (int count, string name)>("Execute");
        int capturedCount = 0;
        interceptor.Call((args) => capturedCount = args.count * args.name.Length);

        await interceptor.Invoke(false, (3, "hello"));

        Assert.Equal(15, capturedCount);
    }

    // ========================================================================
    // When with exact match
    // ========================================================================

    [Fact]
    public async Task When_ExactMatch_SingleParam_InvokesCallback()
    {
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidProcessDelegate, int>("Process");
        int captured = 0;
        AsyncVoidProcessDelegate callback = async (int x) => { await Task.Yield(); captured = x; };
        interceptor.When(5).Call(callback);

        await interceptor.Invoke(false, 5);

        Assert.Equal(5, captured);
    }

    [Fact]
    public async Task When_ExactMatch_TwoParam_InvokesCallback()
    {
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidExecuteDelegate, (int count, string name)>("Execute");
        int capturedCount = 0;
        AsyncVoidExecuteDelegate callback = async (int count, string name) => { await Task.Yield(); capturedCount = count; };
        interceptor.When((3, "hello")).Call(callback);

        await interceptor.Invoke(false, (3, "hello"));

        Assert.Equal(3, capturedCount);
    }

    // ========================================================================
    // When with predicate
    // ========================================================================

    [Fact]
    public async Task When_Predicate_SingleParam_InvokesCallback()
    {
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidProcessDelegate, int>("Process");
        int captured = 0;
        AsyncVoidProcessDelegate callback = async (int x) => { await Task.Yield(); captured = x; };
        interceptor.When(x => x > 10).Call(callback);

        await interceptor.Invoke(false, 15);

        Assert.Equal(15, captured);
    }

    [Fact]
    public async Task When_Predicate_TwoParam_InvokesCallback()
    {
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidExecuteDelegate, (int count, string name)>("Execute");
        int capturedCount = 0;
        AsyncVoidExecuteDelegate callback = async (int count, string name) => { await Task.Yield(); capturedCount = count; };
        interceptor.When(args => args.count > 5 && args.name.Length > 2).Call(callback);

        await interceptor.Invoke(false, (10, "hello"));

        Assert.Equal(10, capturedCount);
    }

    // ========================================================================
    // When with sync callback
    // ========================================================================

    [Fact]
    public async Task When_SyncCallback_SingleParam_InvokesCallback()
    {
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidProcessDelegate, int>("Process");
        int captured = 0;
        interceptor.When(5).Call((int x) => captured = x);

        await interceptor.Invoke(false, 5);

        Assert.Equal(5, captured);
    }

    // ========================================================================
    // Sequence
    // ========================================================================

    [Fact]
    public async Task Call_ThenCall_SequencesAsyncCallbacks()
    {
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidProcessDelegate, int>("Process");
        var calls = new List<int>();
        AsyncVoidProcessDelegate first = async (int x) => { await Task.Yield(); calls.Add(x * 1); };
        AsyncVoidProcessDelegate second = async (int x) => { await Task.Yield(); calls.Add(x * 10); };
        interceptor.Call(first).ThenCall(second);

        await interceptor.Invoke(false, 1);
        await interceptor.Invoke(false, 2);

        Assert.Equal(new[] { 1, 20 }, calls);
    }

    [Fact]
    public async Task Call_ThenCall_WithSyncCallback_SequencesCallbacks()
    {
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidProcessDelegate, int>("Process");
        var calls = new List<int>();
        AsyncVoidProcessDelegate first = async (int x) => { await Task.Yield(); calls.Add(x); };
        interceptor.Call(first).ThenCall((int x) => calls.Add(x * 10));

        await interceptor.Invoke(false, 1);
        await interceptor.Invoke(false, 2);

        Assert.Equal(new[] { 1, 20 }, calls);
    }

    [Fact]
    public async Task Sequence_RepeatsLastCallbackByDefault()
    {
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidProcessDelegate, int>("Process");
        var calls = new List<int>();
        AsyncVoidProcessDelegate first = async (int x) => { await Task.Yield(); calls.Add(1); };
        AsyncVoidProcessDelegate second = async (int x) => { await Task.Yield(); calls.Add(2); };
        interceptor.Call(first).ThenCall(second);

        await interceptor.Invoke(false, 0); // 1
        await interceptor.Invoke(false, 0); // 2
        await interceptor.Invoke(false, 0); // repeats 2

        Assert.Equal(new[] { 1, 2, 2 }, calls);
    }

    [Fact]
    public async Task Sequence_ThenDefault_StopsRepeating()
    {
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidProcessDelegate, int>("Process");
        var calls = new List<int>();
        AsyncVoidProcessDelegate first = async (int x) => { await Task.Yield(); calls.Add(1); };
        AsyncVoidProcessDelegate second = async (int x) => { await Task.Yield(); calls.Add(2); };
        interceptor.Call(first).ThenCall(second).ThenDefault();

        await interceptor.Invoke(false, 0); // 1
        await interceptor.Invoke(false, 0); // 2
        await interceptor.Invoke(false, 0); // exhausted, ThenDefault - no call

        Assert.Equal(new[] { 1, 2 }, calls);
    }

    // ========================================================================
    // LastArgs
    // ========================================================================

    [Fact]
    public async Task LastArgs_SingleParam_RecordsArgsAfterInvoke()
    {
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidProcessDelegate, int>("Process");
        AsyncVoidProcessDelegate callback = async (int x) => { await Task.Yield(); };
        interceptor.Call(callback);

        await interceptor.Invoke(false, 42);

        Assert.Equal(42, interceptor.LastArgs);
    }

    [Fact]
    public async Task LastArgs_TwoParam_RecordsTupleArgsAfterInvoke()
    {
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidExecuteDelegate, (int count, string name)>("Execute");
        AsyncVoidExecuteDelegate callback = async (int count, string name) => { await Task.Yield(); };
        interceptor.Call(callback);

        await interceptor.Invoke(false, (10, "world"));

        Assert.Equal((10, "world"), interceptor.LastArgs);
    }

    [Fact]
    public async Task LastArgs_Unconfigured_RecordsArgsForUnconfiguredCalls()
    {
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidProcessDelegate, int>("Process");

        await interceptor.Invoke(false, 99);

        Assert.Equal(99, interceptor.LastArgs);
    }

    // ========================================================================
    // SetFallback / SetSourceFallback
    // ========================================================================

    [Fact]
    public async Task SetFallback_InvokeUsesFallbackWhenUnconfigured()
    {
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidProcessDelegate, int>("Process");
        int captured = 0;
        AsyncVoidProcessDelegate fallback = async (int x) => { await Task.Yield(); captured = x + 100; };
        interceptor.SetFallback(fallback);

        await interceptor.Invoke(false, 5);

        Assert.Equal(105, captured);
    }

    [Fact]
    public async Task SetSourceFallback_InvokesSourceFallbackWhenUnconfigured()
    {
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidProcessDelegate, int>("Process");
        int captured = 0;
        AsyncVoidProcessDelegate sourceFallback = async (int x) => { await Task.Yield(); captured = x * 3; };
        interceptor.SetSourceFallback(sourceFallback);

        await interceptor.Invoke(false, 7);

        Assert.Equal(21, captured);
    }

    [Fact]
    public async Task SetFallback_TakesPrecedenceOverSourceFallback()
    {
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidProcessDelegate, int>("Process");
        int captured = 0;
        AsyncVoidProcessDelegate fallback = async (int x) => { await Task.Yield(); captured = 100; };
        AsyncVoidProcessDelegate sourceFallback = async (int x) => { await Task.Yield(); captured = 200; };
        interceptor.SetFallback(fallback);
        interceptor.SetSourceFallback(sourceFallback);

        await interceptor.Invoke(false, 0);

        Assert.Equal(100, captured);
    }

    [Fact]
    public async Task SetFallback_TwoParam_InvokeUsesFallback()
    {
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidExecuteDelegate, (int count, string name)>("Execute");
        int capturedCount = 0;
        AsyncVoidExecuteDelegate fallback = async (int count, string name) => { await Task.Yield(); capturedCount = count * name.Length; };
        interceptor.SetFallback(fallback);

        await interceptor.Invoke(false, (3, "hello"));

        Assert.Equal(15, capturedCount);
    }

    // ========================================================================
    // Verify
    // ========================================================================

    [Fact]
    public void Verify_ThrowsWhenNotCalled()
    {
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidProcessDelegate, int>("Process");
        AsyncVoidProcessDelegate callback = async (int x) => { await Task.Yield(); };
        interceptor.Call(callback);

        Assert.Throws<VerificationException>(() => interceptor.Verify());
    }

    [Fact]
    public async Task Verify_PassesWhenCalled()
    {
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidProcessDelegate, int>("Process");
        AsyncVoidProcessDelegate callback = async (int x) => { await Task.Yield(); };
        interceptor.Call(callback);

        await interceptor.Invoke(false, 0);

        interceptor.Verify(); // should not throw
    }

    [Fact]
    public void Verifiable_CheckVerification_ReturnsFailureWhenNotCalled()
    {
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidProcessDelegate, int>("Process");
        AsyncVoidProcessDelegate callback = async (int x) => { await Task.Yield(); };
        interceptor.Call(callback).Verifiable();

        var failure = interceptor.CheckVerification();

        Assert.NotNull(failure);
    }

    [Fact]
    public async Task Verifiable_CheckVerification_ReturnsNullWhenCalled()
    {
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidProcessDelegate, int>("Process");
        AsyncVoidProcessDelegate callback = async (int x) => { await Task.Yield(); };
        interceptor.Call(callback).Verifiable();

        await interceptor.Invoke(false, 0);

        var failure = interceptor.CheckVerification();

        Assert.Null(failure);
    }

    [Fact]
    public void CheckVerificationAll_ReturnsNullWhenNotConfigured()
    {
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidProcessDelegate, int>("Process");

        var failure = interceptor.CheckVerificationAll();

        Assert.Null(failure);
    }

    [Fact]
    public void CheckVerificationAll_ReturnsFailureWhenConfiguredButNotCalled()
    {
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidProcessDelegate, int>("Process");
        AsyncVoidProcessDelegate callback = async (int x) => { await Task.Yield(); };
        interceptor.Call(callback);

        var failure = interceptor.CheckVerificationAll();

        Assert.NotNull(failure);
    }

    // ========================================================================
    // Strict mode
    // ========================================================================

    [Fact]
    public async Task Invoke_StrictMode_ThrowsWhenUnconfigured()
    {
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidProcessDelegate, int>("Process");

        await Assert.ThrowsAsync<StubException>(() => interceptor.Invoke(true, 0));
    }

    [Fact]
    public async Task Invoke_StrictMode_SequenceExhausted_Throws()
    {
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidProcessDelegate, int>("Process");
        AsyncVoidProcessDelegate first = async (int x) => { await Task.Yield(); };
        AsyncVoidProcessDelegate second = async (int x) => { await Task.Yield(); };
        interceptor.Call(first).ThenCall(second);

        await interceptor.Invoke(true, 0); // first
        await interceptor.Invoke(true, 0); // second
        await Assert.ThrowsAsync<StubException>(() => interceptor.Invoke(true, 0)); // exhausted
    }

    // ========================================================================
    // Reset
    // ========================================================================

    [Fact]
    public async Task Reset_ClearsTrackingState()
    {
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidProcessDelegate, int>("Process");
        AsyncVoidProcessDelegate callback = async (int x) => { await Task.Yield(); };
        interceptor.Call(callback);

        await interceptor.Invoke(false, 5);
        Assert.Equal(1, interceptor.TotalCallCount);

        interceptor.Reset();

        Assert.Equal(0, interceptor.TotalCallCount);
        Assert.Equal(0, interceptor.UnconfiguredCallCount);
    }

    // ========================================================================
    // TotalCallCount and UnconfiguredCallCount
    // ========================================================================

    [Fact]
    public async Task TotalCallCount_IncludesAllCalls()
    {
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidProcessDelegate, int>("Process");
        AsyncVoidProcessDelegate callback = async (int x) => { await Task.Yield(); };
        interceptor.Call(callback);

        await interceptor.Invoke(false, 0);
        await interceptor.Invoke(false, 0);

        Assert.Equal(2, interceptor.TotalCallCount);
    }

    [Fact]
    public async Task UnconfiguredCallCount_OnlyCountsUnconfiguredCalls()
    {
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidProcessDelegate, int>("Process");

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
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidProcessDelegate, int>("Process");

        Assert.False(interceptor.IsConfigured);
    }

    [Fact]
    public void IsConfigured_TrueAfterCall()
    {
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidProcessDelegate, int>("Process");
        AsyncVoidProcessDelegate callback = async (int x) => { await Task.Yield(); };
        interceptor.Call(callback);

        Assert.True(interceptor.IsConfigured);
    }

    // ========================================================================
    // When chain - ThenWhen
    // ========================================================================

    [Fact]
    public async Task When_ThenWhen_ChainsMultipleMatchers()
    {
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidProcessDelegate, int>("Process");
        var calls = new List<int>();
        AsyncVoidProcessDelegate cb1 = async (int x) => { await Task.Yield(); calls.Add(10); };
        AsyncVoidProcessDelegate cb2 = async (int x) => { await Task.Yield(); calls.Add(20); };
        interceptor.When(1).Call(cb1)
                   .ThenWhen(2).Call(cb2);

        await interceptor.Invoke(false, 1);
        await interceptor.Invoke(false, 2);

        Assert.Equal(new[] { 10, 20 }, calls);
    }

    // ========================================================================
    // When chain - ThenCall (terminal)
    // ========================================================================

    [Fact]
    public async Task When_ThenCall_TerminalMatcher()
    {
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidProcessDelegate, int>("Process");
        var calls = new List<int>();
        AsyncVoidProcessDelegate cb1 = async (int x) => { await Task.Yield(); calls.Add(10); };
        AsyncVoidProcessDelegate terminal = async (int x) => { await Task.Yield(); calls.Add(x * 100); };
        interceptor.When(1).Call(cb1)
                   .ThenCall(terminal);

        await interceptor.Invoke(false, 1);
        await interceptor.Invoke(false, 5); // terminal matches anything

        Assert.Equal(new[] { 10, 500 }, calls);
    }

    // ========================================================================
    // When chain - ThenNone
    // ========================================================================

    [Fact]
    public async Task When_ThenNone_AdvancesChainPastTerminal()
    {
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidProcessDelegate, int>("Process");
        var calls = new List<int>();
        AsyncVoidProcessDelegate cb1 = async (int x) => { await Task.Yield(); calls.Add(10); };
        interceptor.When(1).Call(cb1)
                   .ThenNone();

        await interceptor.Invoke(false, 1);
        await interceptor.Invoke(false, 99); // ThenNone is terminal-never-matches, so chain head advances

        Assert.Equal(new[] { 10 }, calls);
    }

    // ========================================================================
    // Builder interface: MethodCallBuilder tracking
    // ========================================================================

    [Fact]
    public async Task MethodCallBuilder_TracksLastArgs()
    {
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidProcessDelegate, int>("Process");
        AsyncVoidProcessDelegate callback = async (int x) => { await Task.Yield(); };
        var builder = interceptor.Call(callback);

        await interceptor.Invoke(false, 7);

        Assert.Equal(7, builder.LastArgs);
    }

    [Fact]
    public void MethodCallBuilder_Verify_ThrowsWhenNotCalled()
    {
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidProcessDelegate, int>("Process");
        AsyncVoidProcessDelegate callback = async (int x) => { await Task.Yield(); };
        var builder = interceptor.Call(callback);

        Assert.Throws<VerificationException>(() => builder.Verify());
    }

    [Fact]
    public async Task MethodCallBuilder_Verify_PassesAfterCall()
    {
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidProcessDelegate, int>("Process");
        AsyncVoidProcessDelegate callback = async (int x) => { await Task.Yield(); };
        var builder = interceptor.Call(callback);

        await interceptor.Invoke(false, 0);

        builder.Verify(); // should not throw
    }

    // ========================================================================
    // ConfigureAwait behavior
    // ========================================================================

    [Fact]
    public async Task Invoke_ConfigureAwaitFalse_DoesNotDeadlock()
    {
        var interceptor = new AsyncVoidMethodInterceptor<AsyncVoidProcessDelegate, int>("Process");
        int captured = 0;
        AsyncVoidProcessDelegate callback = async (int x) =>
        {
            await Task.Delay(1).ConfigureAwait(false);
            captured = x;
        };
        interceptor.Call(callback);

        await interceptor.Invoke(false, 42);

        Assert.Equal(42, captured);
    }
}
