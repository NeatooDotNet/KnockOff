using KnockOff.Interceptors;

namespace KnockOff.Tests.Interceptors;

// Test delegates for VoidMethodInterceptor<TDelegate, TArgs>
delegate void VoidProcessDelegate(int x);
delegate void VoidExecuteDelegate(int count, string name);

/// <summary>
/// Tests for VoidMethodInterceptor&lt;TDelegate, TArgs&gt; TTuple interceptor type.
/// Verifies all behavioral features: Call, When, sequences, verification, fallbacks.
/// Tests both 1-param (raw type TArgs) and 2-param (ValueTuple TArgs) cases.
/// </summary>
public class VoidMethodInterceptorTests
{
    // ========================================================================
    // Call with delegate
    // ========================================================================

    [Fact]
    public void Call_WithDelegate_InvokesDelegateViaExpressionTree()
    {
        var interceptor = new VoidMethodInterceptor<VoidProcessDelegate, int>("Process");
        int captured = 0;
        VoidProcessDelegate callback = (int x) => captured = x;
        interceptor.Call(callback);

        interceptor.Invoke(false, 42);

        Assert.Equal(42, captured);
    }

    [Fact]
    public void Call_WithDelegate_TwoParam_InvokesDelegateViaExpressionTree()
    {
        var interceptor = new VoidMethodInterceptor<VoidExecuteDelegate, (int count, string name)>("Execute");
        int capturedCount = 0;
        string capturedName = "";
        VoidExecuteDelegate callback = (int count, string name) => { capturedCount = count; capturedName = name; };
        interceptor.Call(callback);

        interceptor.Invoke(false, (5, "test"));

        Assert.Equal(5, capturedCount);
        Assert.Equal("test", capturedName);
    }

    // ========================================================================
    // When with exact match
    // ========================================================================

    [Fact]
    public void When_ExactMatch_SingleParam_InvokesCallback()
    {
        var interceptor = new VoidMethodInterceptor<VoidProcessDelegate, int>("Process");
        int captured = 0;
        VoidProcessDelegate callback = (int x) => captured = x;
        interceptor.When(5).Call(callback);

        interceptor.Invoke(false, 5);

        Assert.Equal(5, captured);
    }

    [Fact]
    public void When_ExactMatch_TwoParam_InvokesCallback()
    {
        var interceptor = new VoidMethodInterceptor<VoidExecuteDelegate, (int count, string name)>("Execute");
        int capturedCount = 0;
        VoidExecuteDelegate callback = (int count, string name) => capturedCount = count;
        interceptor.When((3, "hello")).Call(callback);

        interceptor.Invoke(false, (3, "hello"));

        Assert.Equal(3, capturedCount);
    }

    // ========================================================================
    // When with predicate
    // ========================================================================

    [Fact]
    public void When_Predicate_SingleParam_InvokesCallback()
    {
        var interceptor = new VoidMethodInterceptor<VoidProcessDelegate, int>("Process");
        int captured = 0;
        VoidProcessDelegate callback = (int x) => captured = x;
        interceptor.When(x => x > 10).Call(callback);

        interceptor.Invoke(false, 15);

        Assert.Equal(15, captured);
    }

    [Fact]
    public void When_Predicate_TwoParam_InvokesCallback()
    {
        var interceptor = new VoidMethodInterceptor<VoidExecuteDelegate, (int count, string name)>("Execute");
        int capturedCount = 0;
        VoidExecuteDelegate callback = (int count, string name) => capturedCount = count;
        interceptor.When(args => args.count > 5 && args.name.Length > 2).Call(callback);

        interceptor.Invoke(false, (10, "hello"));

        Assert.Equal(10, capturedCount);
    }

    // ========================================================================
    // Sequence
    // ========================================================================

    [Fact]
    public void Call_ThenCall_SequencesCallbacks()
    {
        var interceptor = new VoidMethodInterceptor<VoidProcessDelegate, int>("Process");
        var calls = new List<int>();
        VoidProcessDelegate first = (int x) => calls.Add(x * 1);
        VoidProcessDelegate second = (int x) => calls.Add(x * 10);
        interceptor.Call(first).ThenCall(second);

        interceptor.Invoke(false, 1);
        interceptor.Invoke(false, 2);

        Assert.Equal(new[] { 1, 20 }, calls);
    }

    [Fact]
    public void Sequence_RepeatsLastCallbackByDefault()
    {
        var interceptor = new VoidMethodInterceptor<VoidProcessDelegate, int>("Process");
        var calls = new List<int>();
        VoidProcessDelegate first = (int x) => calls.Add(1);
        VoidProcessDelegate second = (int x) => calls.Add(2);
        interceptor.Call(first).ThenCall(second);

        interceptor.Invoke(false, 0); // 1
        interceptor.Invoke(false, 0); // 2
        interceptor.Invoke(false, 0); // repeats 2

        Assert.Equal(new[] { 1, 2, 2 }, calls);
    }

    [Fact]
    public void Sequence_ThenDefault_StopsRepeating()
    {
        var interceptor = new VoidMethodInterceptor<VoidProcessDelegate, int>("Process");
        var calls = new List<int>();
        VoidProcessDelegate first = (int x) => calls.Add(1);
        VoidProcessDelegate second = (int x) => calls.Add(2);
        interceptor.Call(first).ThenCall(second).ThenDefault();

        interceptor.Invoke(false, 0); // 1
        interceptor.Invoke(false, 0); // 2
        interceptor.Invoke(false, 0); // exhausted, ThenDefault - no call

        Assert.Equal(new[] { 1, 2 }, calls);
    }

    // ========================================================================
    // LastArgs
    // ========================================================================

    [Fact]
    public void LastArgs_SingleParam_RecordsArgsAfterInvoke()
    {
        var interceptor = new VoidMethodInterceptor<VoidProcessDelegate, int>("Process");
        VoidProcessDelegate callback = (int x) => { };
        interceptor.Call(callback);

        interceptor.Invoke(false, 42);

        Assert.Equal(42, interceptor.LastArgs);
    }

    [Fact]
    public void LastArgs_TwoParam_RecordsTupleArgsAfterInvoke()
    {
        var interceptor = new VoidMethodInterceptor<VoidExecuteDelegate, (int count, string name)>("Execute");
        VoidExecuteDelegate callback = (int count, string name) => { };
        interceptor.Call(callback);

        interceptor.Invoke(false, (10, "world"));

        Assert.Equal((10, "world"), interceptor.LastArgs);
    }

    [Fact]
    public void LastArgs_Unconfigured_RecordsArgsForUnconfiguredCalls()
    {
        var interceptor = new VoidMethodInterceptor<VoidProcessDelegate, int>("Process");

        interceptor.Invoke(false, 99);

        Assert.Equal(99, interceptor.LastArgs);
    }

    // ========================================================================
    // SetFallback / SetSourceFallback
    // ========================================================================

    [Fact]
    public void SetFallback_InvokeUsesFallbackWhenUnconfigured()
    {
        var interceptor = new VoidMethodInterceptor<VoidProcessDelegate, int>("Process");
        int captured = 0;
        VoidProcessDelegate fallback = (int x) => captured = x + 100;
        interceptor.SetFallback(fallback);

        interceptor.Invoke(false, 5);

        Assert.Equal(105, captured);
    }

    [Fact]
    public void SetSourceFallback_InvokesSourceFallbackWhenUnconfigured()
    {
        var interceptor = new VoidMethodInterceptor<VoidProcessDelegate, int>("Process");
        int captured = 0;
        VoidProcessDelegate sourceFallback = (int x) => captured = x * 3;
        interceptor.SetSourceFallback(sourceFallback);

        interceptor.Invoke(false, 7);

        Assert.Equal(21, captured);
    }

    [Fact]
    public void SetFallback_TakesPrecedenceOverSourceFallback()
    {
        var interceptor = new VoidMethodInterceptor<VoidProcessDelegate, int>("Process");
        int captured = 0;
        VoidProcessDelegate fallback = (int x) => captured = 100;
        VoidProcessDelegate sourceFallback = (int x) => captured = 200;
        interceptor.SetFallback(fallback);
        interceptor.SetSourceFallback(sourceFallback);

        interceptor.Invoke(false, 0);

        Assert.Equal(100, captured);
    }

    [Fact]
    public void SetFallback_TwoParam_InvokeUsesFallback()
    {
        var interceptor = new VoidMethodInterceptor<VoidExecuteDelegate, (int count, string name)>("Execute");
        int capturedCount = 0;
        VoidExecuteDelegate fallback = (int count, string name) => capturedCount = count * name.Length;
        interceptor.SetFallback(fallback);

        interceptor.Invoke(false, (3, "hello"));

        Assert.Equal(15, capturedCount);
    }

    // ========================================================================
    // Verify
    // ========================================================================

    [Fact]
    public void Verify_ThrowsWhenNotCalled()
    {
        var interceptor = new VoidMethodInterceptor<VoidProcessDelegate, int>("Process");
        VoidProcessDelegate callback = (int x) => { };
        interceptor.Call(callback);

        Assert.Throws<VerificationException>(() => interceptor.Verify());
    }

    [Fact]
    public void Verify_PassesWhenCalled()
    {
        var interceptor = new VoidMethodInterceptor<VoidProcessDelegate, int>("Process");
        VoidProcessDelegate callback = (int x) => { };
        interceptor.Call(callback);

        interceptor.Invoke(false, 0);

        interceptor.Verify(); // should not throw
    }

    [Fact]
    public void Verifiable_CheckVerification_ReturnsFailureWhenNotCalled()
    {
        var interceptor = new VoidMethodInterceptor<VoidProcessDelegate, int>("Process");
        VoidProcessDelegate callback = (int x) => { };
        interceptor.Call(callback).Verifiable();

        var failure = interceptor.CheckVerification();

        Assert.NotNull(failure);
    }

    [Fact]
    public void Verifiable_CheckVerification_ReturnsNullWhenCalled()
    {
        var interceptor = new VoidMethodInterceptor<VoidProcessDelegate, int>("Process");
        VoidProcessDelegate callback = (int x) => { };
        interceptor.Call(callback).Verifiable();

        interceptor.Invoke(false, 0);

        var failure = interceptor.CheckVerification();

        Assert.Null(failure);
    }

    [Fact]
    public void CheckVerificationAll_ReturnsNullWhenNotConfigured()
    {
        var interceptor = new VoidMethodInterceptor<VoidProcessDelegate, int>("Process");

        var failure = interceptor.CheckVerificationAll();

        Assert.Null(failure);
    }

    [Fact]
    public void CheckVerificationAll_ReturnsFailureWhenConfiguredButNotCalled()
    {
        var interceptor = new VoidMethodInterceptor<VoidProcessDelegate, int>("Process");
        VoidProcessDelegate callback = (int x) => { };
        interceptor.Call(callback);

        var failure = interceptor.CheckVerificationAll();

        Assert.NotNull(failure);
    }

    // ========================================================================
    // Strict mode
    // ========================================================================

    [Fact]
    public void Invoke_StrictMode_ThrowsWhenUnconfigured()
    {
        var interceptor = new VoidMethodInterceptor<VoidProcessDelegate, int>("Process");

        Assert.Throws<StubException>(() => interceptor.Invoke(true, 0));
    }

    [Fact]
    public void Invoke_StrictMode_SequenceExhausted_Throws()
    {
        var interceptor = new VoidMethodInterceptor<VoidProcessDelegate, int>("Process");
        VoidProcessDelegate first = (int x) => { };
        VoidProcessDelegate second = (int x) => { };
        interceptor.Call(first).ThenCall(second);

        interceptor.Invoke(true, 0); // first
        interceptor.Invoke(true, 0); // second
        Assert.Throws<StubException>(() => interceptor.Invoke(true, 0)); // exhausted
    }

    // ========================================================================
    // Reset
    // ========================================================================

    [Fact]
    public void Reset_ClearsTrackingState()
    {
        var interceptor = new VoidMethodInterceptor<VoidProcessDelegate, int>("Process");
        VoidProcessDelegate callback = (int x) => { };
        interceptor.Call(callback);

        interceptor.Invoke(false, 5);
        Assert.Equal(1, interceptor.TotalCallCount);

        interceptor.Reset();

        Assert.Equal(0, interceptor.TotalCallCount);
        Assert.Equal(0, interceptor.UnconfiguredCallCount);
    }

    // ========================================================================
    // TotalCallCount and UnconfiguredCallCount
    // ========================================================================

    [Fact]
    public void TotalCallCount_IncludesAllCalls()
    {
        var interceptor = new VoidMethodInterceptor<VoidProcessDelegate, int>("Process");
        VoidProcessDelegate callback = (int x) => { };
        interceptor.Call(callback);

        interceptor.Invoke(false, 0);
        interceptor.Invoke(false, 0);

        Assert.Equal(2, interceptor.TotalCallCount);
    }

    [Fact]
    public void UnconfiguredCallCount_OnlyCountsUnconfiguredCalls()
    {
        var interceptor = new VoidMethodInterceptor<VoidProcessDelegate, int>("Process");

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
        var interceptor = new VoidMethodInterceptor<VoidProcessDelegate, int>("Process");

        Assert.False(interceptor.IsConfigured);
    }

    [Fact]
    public void IsConfigured_TrueAfterCall()
    {
        var interceptor = new VoidMethodInterceptor<VoidProcessDelegate, int>("Process");
        VoidProcessDelegate callback = (int x) => { };
        interceptor.Call(callback);

        Assert.True(interceptor.IsConfigured);
    }

    // ========================================================================
    // When chain - ThenWhen
    // ========================================================================

    [Fact]
    public void When_ThenWhen_ChainsMultipleMatchers()
    {
        var interceptor = new VoidMethodInterceptor<VoidProcessDelegate, int>("Process");
        var calls = new List<int>();
        VoidProcessDelegate cb1 = (int x) => calls.Add(10);
        VoidProcessDelegate cb2 = (int x) => calls.Add(20);
        interceptor.When(1).Call(cb1)
                   .ThenWhen(2).Call(cb2);

        interceptor.Invoke(false, 1);
        interceptor.Invoke(false, 2);

        Assert.Equal(new[] { 10, 20 }, calls);
    }

    // ========================================================================
    // When chain - ThenCall (terminal)
    // ========================================================================

    [Fact]
    public void When_ThenCall_TerminalMatcher()
    {
        var interceptor = new VoidMethodInterceptor<VoidProcessDelegate, int>("Process");
        var calls = new List<int>();
        VoidProcessDelegate cb1 = (int x) => calls.Add(10);
        VoidProcessDelegate terminal = (int x) => calls.Add(x * 100);
        interceptor.When(1).Call(cb1)
                   .ThenCall(terminal);

        interceptor.Invoke(false, 1);
        interceptor.Invoke(false, 5); // terminal matches anything

        Assert.Equal(new[] { 10, 500 }, calls);
    }

    // ========================================================================
    // When chain - ThenNone
    // ========================================================================

    [Fact]
    public void When_ThenNone_AdvancesChainPastTerminal()
    {
        var interceptor = new VoidMethodInterceptor<VoidProcessDelegate, int>("Process");
        var calls = new List<int>();
        VoidProcessDelegate cb1 = (int x) => calls.Add(10);
        interceptor.When(1).Call(cb1)
                   .ThenNone();

        interceptor.Invoke(false, 1);
        interceptor.Invoke(false, 99); // ThenNone is terminal-never-matches, so chain head advances

        Assert.Equal(new[] { 10 }, calls);
    }

    // ========================================================================
    // Builder interface: MethodCallBuilder tracking
    // ========================================================================

    [Fact]
    public void MethodCallBuilder_TracksLastArgs()
    {
        var interceptor = new VoidMethodInterceptor<VoidProcessDelegate, int>("Process");
        VoidProcessDelegate callback = (int x) => { };
        var builder = interceptor.Call(callback);

        interceptor.Invoke(false, 7);

        Assert.Equal(7, builder.LastArgs);
    }

    [Fact]
    public void MethodCallBuilder_Verify_ThrowsWhenNotCalled()
    {
        var interceptor = new VoidMethodInterceptor<VoidProcessDelegate, int>("Process");
        VoidProcessDelegate callback = (int x) => { };
        var builder = interceptor.Call(callback);

        Assert.Throws<VerificationException>(() => builder.Verify());
    }

    [Fact]
    public void MethodCallBuilder_Verify_PassesAfterCall()
    {
        var interceptor = new VoidMethodInterceptor<VoidProcessDelegate, int>("Process");
        VoidProcessDelegate callback = (int x) => { };
        var builder = interceptor.Call(callback);

        interceptor.Invoke(false, 0);

        builder.Verify(); // should not throw
    }
}
