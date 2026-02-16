using ExtensionOverloadPrototype.Library;

namespace ExtensionOverloadPrototype.Scenarios;

// ============================================================================
// Scenario 7: Edge Cases
// Tests challenging scenarios WITH explicit casts (since direct calls fail).
// ============================================================================

// ---- 7a: Same params, different return types ----
public delegate void VoidStringDelegate(string s);
public delegate int IntStringDelegate(string s);

public class SameParamsCompositor
    : IVoidOverload<VoidStringDelegate, string>,
      IMethodOverload<IntStringDelegate, string, int>
{
    object IVoidOverload<VoidStringDelegate, string>.GetInterceptor() => "void_interceptor";
    object IMethodOverload<IntStringDelegate, string, int>.GetInterceptor() => "int_interceptor";
}

// ---- 7b: Zero-parameter overload ----
public delegate void ZeroArgDelegate();
public delegate void OneArgDelegate(string s);

public readonly struct NoArgs
{
    public static readonly NoArgs Value = new();
}

public class ZeroArgCompositor
    : IVoidOverload<ZeroArgDelegate, NoArgs>,
      IVoidOverload<OneArgDelegate, string>
{
    object IVoidOverload<ZeroArgDelegate, NoArgs>.GetInterceptor() => "zero_interceptor";
    object IVoidOverload<OneArgDelegate, string>.GetInterceptor() => "one_interceptor";
}

// ---- 7c: Async mixed with sync ----
public delegate Task<int> AsyncCalcDelegate(int value);
public delegate int SyncCalcDelegate(int value);

public class AsyncMixedCompositor
    : IMethodOverload<AsyncCalcDelegate, int, Task<int>>,
      IMethodOverload<SyncCalcDelegate, int, int>
{
    object IMethodOverload<AsyncCalcDelegate, int, Task<int>>.GetInterceptor() => "async_interceptor";
    object IMethodOverload<SyncCalcDelegate, int, int>.GetInterceptor() => "sync_interceptor";
}

public class Scenario7_EdgeCases
{
    // ---- 7a: Same params, different return types ----

    [Fact]
    public void SameParams_VoidOverload_WithCast_ResolvesCorrectly()
    {
        var compositor = new SameParamsCompositor();

        IVoidOverload<VoidStringDelegate, string> asVoid = compositor;
        var result = asVoid.Call((string s) => { });

        Assert.Equal("VoidCall:VoidStringDelegate", result);
    }

    [Fact]
    public void SameParams_IntOverload_WithCast_ResolvesCorrectly()
    {
        var compositor = new SameParamsCompositor();

        IMethodOverload<IntStringDelegate, string, int> asInt = compositor;
        var result = asInt.Return((string s) => 42);

        Assert.Equal("MethodReturn:IntStringDelegate", result);
    }

    // When with same TArgs on different interface families: no ambiguity
    // because you must cast to a specific interface anyway.
    [Fact]
    public void SameParams_When_NoAmbiguity_BecauseCastRequired()
    {
        var compositor = new SameParamsCompositor();

        IVoidOverload<VoidStringDelegate, string> asVoid = compositor;
        var voidResult = asVoid.When("hello");

        IMethodOverload<IntStringDelegate, string, int> asInt = compositor;
        var intResult = asInt.When("hello");

        Assert.Equal("VoidWhen:String", voidResult);
        Assert.Equal("MethodWhen:String", intResult);
    }

    // ---- 7b: Zero-parameter overloads ----

    [Fact]
    public void ZeroArgs_Call_WithCast_ResolvesCorrectly()
    {
        var compositor = new ZeroArgCompositor();

        IVoidOverload<ZeroArgDelegate, NoArgs> asZero = compositor;
        var result = asZero.Call(() => { });

        Assert.Equal("VoidCall:ZeroArgDelegate", result);
    }

    [Fact]
    public void ZeroArgs_When_WithCast_UsesUnitType()
    {
        var compositor = new ZeroArgCompositor();

        IVoidOverload<ZeroArgDelegate, NoArgs> asZero = compositor;
        var result = asZero.When(NoArgs.Value);

        Assert.Equal("VoidWhen:NoArgs", result);
    }

    [Fact]
    public void OneArg_AfterZeroArg_WithCast_StillResolvesCorrectly()
    {
        var compositor = new ZeroArgCompositor();

        IVoidOverload<OneArgDelegate, string> asOne = compositor;
        var result = asOne.Call((string s) => { });

        Assert.Equal("VoidCall:OneArgDelegate", result);
    }

    // ---- 7c: Async mixed with sync (same TArgs, different TReturn) ----

    [Fact]
    public void AsyncMixed_Return_AsyncOverload_WithCast_ResolvesCorrectly()
    {
        var compositor = new AsyncMixedCompositor();

        IMethodOverload<AsyncCalcDelegate, int, Task<int>> asAsync = compositor;
        var result = asAsync.Return((int value) => Task.FromResult(value * 2));

        Assert.Equal("MethodReturn:AsyncCalcDelegate", result);
    }

    [Fact]
    public void AsyncMixed_Return_SyncOverload_WithCast_ResolvesCorrectly()
    {
        var compositor = new AsyncMixedCompositor();

        IMethodOverload<SyncCalcDelegate, int, int> asSync = compositor;
        var result = asSync.Return((int value) => value * 2);

        Assert.Equal("MethodReturn:SyncCalcDelegate", result);
    }

    // When with same TArgs on async vs sync: no ambiguity because cast required
    [Fact]
    public void AsyncMixed_When_NoAmbiguity_BecauseCastRequired()
    {
        var compositor = new AsyncMixedCompositor();

        IMethodOverload<AsyncCalcDelegate, int, Task<int>> asAsync = compositor;
        var asyncResult = asAsync.When(42);

        IMethodOverload<SyncCalcDelegate, int, int> asSync = compositor;
        var syncResult = asSync.When(42);

        Assert.Equal("MethodWhen:Int32", asyncResult);
        Assert.Equal("MethodWhen:Int32", syncResult);
    }

    // ---- 7d: ReturnValue with casts ----

    [Fact]
    public void ReturnValue_UniqueReturnTypes_WithCast_ResolvesCorrectly()
    {
        var compositor = new MixedCompositor();

        var asInt = (IMethodOverload<ExecIntDelegate, (string command, int timeout), int>)compositor;
        var intResult = asInt.ReturnValue(42);

        var asStr = (IMethodOverload<ExecStringDelegate, (string command, bool verbose), string>)compositor;
        var stringResult = asStr.ReturnValue("done");

        Assert.Equal("MethodReturnValue:Int32=42", intResult);
        Assert.Equal("MethodReturnValue:String=done", stringResult);
    }
}
