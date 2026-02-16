using ExtensionOverloadPrototype.Library;

namespace ExtensionOverloadPrototype.Scenarios;

// ============================================================================
// Scenario 1: Basic Overload Resolution
// Tests whether the compiler can resolve Call() to the correct overload
// based on delegate signature when multiple IVoidOverload interfaces are implemented.
//
// FINDING: Extension methods on generic interfaces DO NOT resolve automatically
// when the compiler needs to infer TDelegate and TArgs from the concrete type's
// interface implementations. The compiler gives CS1061 (method not found).
//
// This file tests workarounds that DO compile.
// ============================================================================

// "Generated" delegates for a method with 3 overloads:
// void Process(string data)
// void Process(string data, int priority)
// void Process(string data, int priority, bool flag)
public delegate void ProcessDelegate1(string data);
public delegate void ProcessDelegate2(string data, int priority);
public delegate void ProcessDelegate3(string data, int priority, bool flag);

// "Generated" compositor implementing multiple IVoidOverload interfaces
public class ProcessCompositor
    : IVoidOverload<ProcessDelegate1, string>,
      IVoidOverload<ProcessDelegate2, (string data, int priority)>,
      IVoidOverload<ProcessDelegate3, (string data, int priority, bool flag)>
{
    object IVoidOverload<ProcessDelegate1, string>.GetInterceptor() => "interceptor1";
    object IVoidOverload<ProcessDelegate2, (string data, int priority)>.GetInterceptor() => "interceptor2";
    object IVoidOverload<ProcessDelegate3, (string data, int priority, bool flag)>.GetInterceptor() => "interceptor3";
}

public class Scenario1_BasicOverloadResolution
{
    // -------------------------------------------------------------------
    // DIRECT CALLS ON COMPOSITOR: ALL FAIL WITH CS1061
    // The compiler cannot traverse from ProcessCompositor to
    // IVoidOverload<ProcessDelegate1, string> to find the extension method.
    // -------------------------------------------------------------------

    // DOES NOT COMPILE: CS1061
    // [Fact]
    // public void Call_DirectOnCompositor_DoesNotCompile()
    // {
    //     var compositor = new ProcessCompositor();
    //     var result = compositor.Call((string data) => { });
    // }

    // -------------------------------------------------------------------
    // WORKAROUND 1: Explicit cast to specific interface
    // This works but defeats the purpose - users must know the interface.
    // -------------------------------------------------------------------

    [Fact]
    public void Call_WithExplicitCast_1Arg_ResolvesCorrectly()
    {
        var compositor = new ProcessCompositor();

        var asOverload1 = (IVoidOverload<ProcessDelegate1, string>)compositor;
        var result = asOverload1.Call((string data) => { });

        Assert.Equal("VoidCall:ProcessDelegate1", result);
    }

    [Fact]
    public void Call_WithExplicitCast_2Args_ResolvesCorrectly()
    {
        var compositor = new ProcessCompositor();

        var asOverload2 = (IVoidOverload<ProcessDelegate2, (string data, int priority)>)compositor;
        var result = asOverload2.Call((string data, int priority) => { });

        Assert.Equal("VoidCall:ProcessDelegate2", result);
    }

    [Fact]
    public void Call_WithExplicitCast_3Args_ResolvesCorrectly()
    {
        var compositor = new ProcessCompositor();

        var asOverload3 = (IVoidOverload<ProcessDelegate3, (string data, int priority, bool flag)>)compositor;
        var result = asOverload3.Call((string data, int priority, bool flag) => { });

        Assert.Equal("VoidCall:ProcessDelegate3", result);
    }

    // -------------------------------------------------------------------
    // WORKAROUND 2: Explicit generic type arguments
    // This works but is extremely verbose and ugly.
    // -------------------------------------------------------------------

    [Fact]
    public void Call_WithExplicitTypeArgs_ResolvesCorrectly()
    {
        var compositor = new ProcessCompositor();

        var result = VoidOverloadExtensions
            .Call<ProcessDelegate2, (string, int)>(compositor, (data, priority) => { });

        Assert.Equal("VoidCall:ProcessDelegate2", result);
    }

    // -------------------------------------------------------------------
    // WORKAROUND 3: Delegate variable with explicit type
    // This works - the compiler knows the delegate type from the variable.
    // But users still need to know the delegate type name.
    // -------------------------------------------------------------------

    [Fact]
    public void Call_WithDelegateVariable_RequiresExplicitCast()
    {
        var compositor = new ProcessCompositor();

        // Even with a typed delegate variable, the extension still cannot be found
        // because the compiler cannot infer the interface from the compositor type.
        // So we must still cast.
        ProcessDelegate2 callback = (data, priority) => { };
        var asOverload2 = (IVoidOverload<ProcessDelegate2, (string data, int priority)>)compositor;
        var result = asOverload2.Call(callback);

        Assert.Equal("VoidCall:ProcessDelegate2", result);
    }
}
