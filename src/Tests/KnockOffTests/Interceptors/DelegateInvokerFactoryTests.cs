using KnockOff.Interceptors;

namespace KnockOff.Tests.Interceptors;

// Test delegates for DelegateInvokerFactory
delegate int SingleParamDelegate(int x);
delegate int TwoParamDelegate(int a, string b);
delegate int ThreeParamDelegate(int a, string b, double c);
delegate int EightParamDelegate(int a, string b, double c, long d, bool e, float f, char g, byte h);

delegate void VoidSingleParamDelegate(int x);
delegate void VoidTwoParamDelegate(int a, string b);

delegate Task<int> AsyncSingleParamDelegate(int x);
delegate Task<int> AsyncTwoParamDelegate(int a, string b);

delegate Task AsyncVoidSingleParamDelegate(int x);
delegate Task AsyncVoidTwoParamDelegate(int a, string b);

delegate string? NullableParamDelegate(string? x);
delegate string StringTwoParamDelegate(string a, string b);

/// <summary>
/// Tests for DelegateInvokerFactory expression tree builders.
/// Verifies correct arg unpacking from raw types (1 param) and ValueTuples (2+ params).
/// </summary>
public class DelegateInvokerFactoryTests
{
    // ========================================================================
    // BuildInvoker — sync non-void
    // ========================================================================

    [Fact]
    public void BuildInvoker_SingleParam_InvokesDelegateWithRawType()
    {
        var invoker = DelegateInvokerFactory.BuildInvoker<SingleParamDelegate, int, int>();

        SingleParamDelegate del = (int x) => x * 2;
        var result = invoker(del, 21);

        Assert.Equal(42, result);
    }

    [Fact]
    public void BuildInvoker_TwoParam_InvokesDelegateWithValueTuple()
    {
        var invoker = DelegateInvokerFactory.BuildInvoker<TwoParamDelegate, (int a, string b), int>();

        TwoParamDelegate del = (int a, string b) => a + b.Length;
        var result = invoker(del, (10, "hello"));

        Assert.Equal(15, result);
    }

    [Fact]
    public void BuildInvoker_ThreeParam_UnpacksAllArgs()
    {
        var invoker = DelegateInvokerFactory.BuildInvoker<ThreeParamDelegate, (int a, string b, double c), int>();

        ThreeParamDelegate del = (int a, string b, double c) => a + b.Length + (int)c;
        var result = invoker(del, (10, "hi", 3.0));

        Assert.Equal(15, result);
    }

    [Fact]
    public void BuildInvoker_EightParam_MaxSupported()
    {
        var invoker = DelegateInvokerFactory.BuildInvoker<EightParamDelegate,
            (int a, string b, double c, long d, bool e, float f, char g, byte h), int>();

        EightParamDelegate del = (int a, string b, double c, long d, bool e, float f, char g, byte h) =>
            a + b.Length + (int)c + (int)d + (e ? 1 : 0) + (int)f + (int)g + h;

        var result = invoker(del, (1, "ab", 3.0, 4L, true, 5.0f, (char)6, (byte)7));

        Assert.Equal(1 + 2 + 3 + 4 + 1 + 5 + 6 + 7, result);
    }

    [Fact]
    public void BuildInvoker_ReturnsCorrectValue()
    {
        var invoker = DelegateInvokerFactory.BuildInvoker<SingleParamDelegate, int, int>();

        SingleParamDelegate del = (int x) => x + 100;
        var result = invoker(del, 5);

        Assert.Equal(105, result);
    }

    // ========================================================================
    // BuildVoidInvoker — sync void
    // ========================================================================

    [Fact]
    public void BuildVoidInvoker_SingleParam_InvokesDelegateWithRawType()
    {
        var invoker = DelegateInvokerFactory.BuildVoidInvoker<VoidSingleParamDelegate, int>();

        int captured = 0;
        VoidSingleParamDelegate del = (int x) => captured = x;
        invoker(del, 42);

        Assert.Equal(42, captured);
    }

    [Fact]
    public void BuildVoidInvoker_TwoParam_InvokesDelegateWithValueTuple()
    {
        var invoker = DelegateInvokerFactory.BuildVoidInvoker<VoidTwoParamDelegate, (int a, string b)>();

        int capturedA = 0;
        string capturedB = "";
        VoidTwoParamDelegate del = (int a, string b) => { capturedA = a; capturedB = b; };
        invoker(del, (99, "test"));

        Assert.Equal(99, capturedA);
        Assert.Equal("test", capturedB);
    }

    // ========================================================================
    // BuildAsyncInvoker — async non-void
    // ========================================================================

    [Fact]
    public async Task BuildAsyncInvoker_SingleParam_InvokesDelegateReturningTask()
    {
        var invoker = DelegateInvokerFactory.BuildAsyncInvoker<AsyncSingleParamDelegate, int, int>();

        AsyncSingleParamDelegate del = async (int x) =>
        {
            await Task.Yield();
            return x * 3;
        };
        var result = await invoker(del, 14);

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task BuildAsyncInvoker_TwoParam_InvokesDelegateWithValueTuple()
    {
        var invoker = DelegateInvokerFactory.BuildAsyncInvoker<AsyncTwoParamDelegate, (int a, string b), int>();

        AsyncTwoParamDelegate del = async (int a, string b) =>
        {
            await Task.Yield();
            return a + b.Length;
        };
        var result = await invoker(del, (10, "hello"));

        Assert.Equal(15, result);
    }

    [Fact]
    public async Task BuildAsyncInvoker_SyncDelegateReturningTaskFromResult()
    {
        var invoker = DelegateInvokerFactory.BuildAsyncInvoker<AsyncSingleParamDelegate, int, int>();

        AsyncSingleParamDelegate del = (int x) => Task.FromResult(x + 1);
        var result = await invoker(del, 41);

        Assert.Equal(42, result);
    }

    // ========================================================================
    // BuildAsyncVoidInvoker — async void (returns Task)
    // ========================================================================

    [Fact]
    public async Task BuildAsyncVoidInvoker_SingleParam_InvokesDelegateReturningTask()
    {
        var invoker = DelegateInvokerFactory.BuildAsyncVoidInvoker<AsyncVoidSingleParamDelegate, int>();

        int captured = 0;
        AsyncVoidSingleParamDelegate del = async (int x) =>
        {
            await Task.Yield();
            captured = x;
        };
        await invoker(del, 42);

        Assert.Equal(42, captured);
    }

    [Fact]
    public async Task BuildAsyncVoidInvoker_TwoParam_InvokesDelegateWithValueTuple()
    {
        var invoker = DelegateInvokerFactory.BuildAsyncVoidInvoker<AsyncVoidTwoParamDelegate, (int a, string b)>();

        int capturedA = 0;
        string capturedB = "";
        AsyncVoidTwoParamDelegate del = async (int a, string b) =>
        {
            await Task.Yield();
            capturedA = a;
            capturedB = b;
        };
        await invoker(del, (99, "async"));

        Assert.Equal(99, capturedA);
        Assert.Equal("async", capturedB);
    }

    // ========================================================================
    // BuildValueDelegate — creates TDelegate that returns fixed value
    // ========================================================================

    [Fact]
    public void BuildValueDelegate_SingleParam_CreatesDelegateThatReturnsFixedValue()
    {
        var factory = DelegateInvokerFactory.BuildValueDelegate<SingleParamDelegate, int>();

        SingleParamDelegate del = factory(42);

        Assert.Equal(42, del(0));
        Assert.Equal(42, del(999));
        Assert.Equal(42, del(-1));
    }

    [Fact]
    public void BuildValueDelegate_TwoParam_CreatesDelegateThatIgnoresAllArgs()
    {
        var factory = DelegateInvokerFactory.BuildValueDelegate<TwoParamDelegate, int>();

        TwoParamDelegate del = factory(100);

        Assert.Equal(100, del(0, "anything"));
        Assert.Equal(100, del(999, "ignored"));
    }

    [Fact]
    public void BuildValueDelegate_DifferentValues_CreateDistinctDelegates()
    {
        var factory = DelegateInvokerFactory.BuildValueDelegate<SingleParamDelegate, int>();

        SingleParamDelegate del1 = factory(10);
        SingleParamDelegate del2 = factory(20);

        Assert.Equal(10, del1(0));
        Assert.Equal(20, del2(0));
    }

    // ========================================================================
    // Edge cases
    // ========================================================================

    [Fact]
    public void BuildInvoker_NullableStringParam_HandlesNull()
    {
        var invoker = DelegateInvokerFactory.BuildInvoker<NullableParamDelegate, string?, string?>();

        NullableParamDelegate del = (string? x) => x?.ToUpper();
        var result = invoker(del, null);

        Assert.Null(result);
    }

    [Fact]
    public void BuildInvoker_NullableStringParam_HandlesNonNull()
    {
        var invoker = DelegateInvokerFactory.BuildInvoker<NullableParamDelegate, string?, string?>();

        NullableParamDelegate del = (string? x) => x?.ToUpper();
        var result = invoker(del, "hello");

        Assert.Equal("HELLO", result);
    }

    [Fact]
    public void BuildInvoker_ReferenceTypeParams_WithValueTuple()
    {
        var invoker = DelegateInvokerFactory.BuildInvoker<StringTwoParamDelegate, (string a, string b), string>();

        StringTwoParamDelegate del = (string a, string b) => a + b;
        var result = invoker(del, ("hello", " world"));

        Assert.Equal("hello world", result);
    }

    [Fact]
    public void BuildValueDelegate_NullValue_CreatesDelegateThatReturnsNull()
    {
        var factory = DelegateInvokerFactory.BuildValueDelegate<NullableParamDelegate, string?>();

        NullableParamDelegate del = factory(null);

        Assert.Null(del("anything"));
    }

    [Fact]
    public void BuildValueDelegate_StringValue_CreatesDelegateThatReturnsString()
    {
        var factory = DelegateInvokerFactory.BuildValueDelegate<NullableParamDelegate, string?>();

        NullableParamDelegate del = factory("fixed");

        Assert.Equal("fixed", del("ignored"));
        Assert.Equal("fixed", del(null));
    }

    // ========================================================================
    // Invoker consistency: same expression tree for sync and async
    // ========================================================================

    [Fact]
    public async Task BuildAsyncInvoker_EightParam_MaxSupported()
    {
        var invoker = DelegateInvokerFactory.BuildAsyncInvoker<EightParamAsyncDelegate,
            (int a, string b, double c, long d, bool e, float f, char g, byte h), int>();

        EightParamAsyncDelegate del = async (int a, string b, double c, long d, bool e, float f, char g, byte h) =>
        {
            await Task.Yield();
            return a + b.Length + (int)c + (int)d + (e ? 1 : 0) + (int)f + (int)g + h;
        };

        var result = await invoker(del, (1, "ab", 3.0, 4L, true, 5.0f, (char)6, (byte)7));

        Assert.Equal(1 + 2 + 3 + 4 + 1 + 5 + 6 + 7, result);
    }
}

// Additional delegate for the 8-param async test
delegate Task<int> EightParamAsyncDelegate(int a, string b, double c, long d, bool e, float f, char g, byte h);
