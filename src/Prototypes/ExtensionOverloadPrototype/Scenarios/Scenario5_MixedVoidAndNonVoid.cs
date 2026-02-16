using ExtensionOverloadPrototype.Library;

namespace ExtensionOverloadPrototype.Scenarios;

// ============================================================================
// Scenario 5: Mixed Void and Non-Void Overloads
// Tests a compositor that implements BOTH IVoidOverload and IMethodOverload
// interfaces, simulating an interface with both void and returning methods.
//
// FINDING: Extension methods require explicit casts. Even with casts,
// ReturnValue() cannot be discovered on the concrete type.
// ============================================================================

// "Generated" delegates for:
// void Execute(string command)                     -> void
// int Execute(string command, int timeout)          -> int
// string Execute(string command, bool verbose)      -> string
public delegate void ExecVoidDelegate(string command);
public delegate int ExecIntDelegate(string command, int timeout);
public delegate string ExecStringDelegate(string command, bool verbose);

// "Generated" compositor mixing void and non-void overloads
public class MixedCompositor
    : IVoidOverload<ExecVoidDelegate, string>,
      IMethodOverload<ExecIntDelegate, (string command, int timeout), int>,
      IMethodOverload<ExecStringDelegate, (string command, bool verbose), string>
{
    object IVoidOverload<ExecVoidDelegate, string>.GetInterceptor() => "void_interceptor";
    object IMethodOverload<ExecIntDelegate, (string command, int timeout), int>.GetInterceptor() => "int_interceptor";
    object IMethodOverload<ExecStringDelegate, (string command, bool verbose), string>.GetInterceptor() => "string_interceptor";
}

public class Scenario5_MixedVoidAndNonVoid
{
    [Fact]
    public void Call_VoidOverload_WithCast_ResolvesCorrectly()
    {
        var compositor = new MixedCompositor();

        var asVoid = (IVoidOverload<ExecVoidDelegate, string>)compositor;
        var result = asVoid.Call((string command) => { });

        Assert.Equal("VoidCall:ExecVoidDelegate", result);
    }

    [Fact]
    public void Return_IntOverload_WithCast_ResolvesCorrectly()
    {
        var compositor = new MixedCompositor();

        var asInt = (IMethodOverload<ExecIntDelegate, (string command, int timeout), int>)compositor;
        var result = asInt.Return((string command, int timeout) => timeout);

        Assert.Equal("MethodReturn:ExecIntDelegate", result);
    }

    [Fact]
    public void Return_StringOverload_WithCast_ResolvesCorrectly()
    {
        var compositor = new MixedCompositor();

        var asStr = (IMethodOverload<ExecStringDelegate, (string command, bool verbose), string>)compositor;
        var result = asStr.Return((string command, bool verbose) => verbose ? "verbose" : "quiet");

        Assert.Equal("MethodReturn:ExecStringDelegate", result);
    }

    [Fact]
    public void When_VoidOverload_WithCast_ResolvesCorrectly()
    {
        var compositor = new MixedCompositor();

        var asVoid = (IVoidOverload<ExecVoidDelegate, string>)compositor;
        var result = asVoid.When("run");

        Assert.Equal("VoidWhen:String", result);
    }

    [Fact]
    public void ReturnValue_IntOverload_WithCast_ResolvesCorrectly()
    {
        var compositor = new MixedCompositor();

        var asInt = (IMethodOverload<ExecIntDelegate, (string command, int timeout), int>)compositor;
        var result = asInt.ReturnValue(42);

        Assert.Equal("MethodReturnValue:Int32=42", result);
    }

    [Fact]
    public void ReturnValue_StringOverload_WithCast_ResolvesCorrectly()
    {
        var compositor = new MixedCompositor();

        var asStr = (IMethodOverload<ExecStringDelegate, (string command, bool verbose), string>)compositor;
        var result = asStr.ReturnValue("done");

        Assert.Equal("MethodReturnValue:String=done", result);
    }
}
