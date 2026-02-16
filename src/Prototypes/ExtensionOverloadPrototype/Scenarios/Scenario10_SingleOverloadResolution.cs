using ExtensionOverloadPrototype.Library;

namespace ExtensionOverloadPrototype.Scenarios.SingleOverload;

// ============================================================================
// Scenario 10: Single Overload Resolution
//
// CRITICAL FINDING: When a compositor implements exactly ONE instantiation of
// a generic interface (e.g., one IVoidOverload<TDelegate, TArgs>), the compiler
// CAN discover and resolve extension methods directly on the concrete type.
//
// This includes:
// - Explicit lambdas: compositor.Call((string data) => { })
// - IMPLICIT lambdas: compositor.Call((data) => { })
// - Method groups: compositor.Call(HandleString)
// - When matching: compositor.When("hello")
// - Predicates: compositor.WhenPredicate((string data) => data == "test")
//
// The failure only occurs with MULTIPLE instantiations of the same generic
// interface (e.g., two IVoidOverload<...> interfaces). The compiler cannot
// decide which instantiation to use for type inference in that case.
//
// IMPLICATION: If each compositor only wraps ONE overload, extension methods
// work perfectly. But the whole point of the compositor pattern is to have
// MULTIPLE overloads in one class, so this finding does not save the approach.
// ============================================================================

public delegate void SingleDelegate(string data);

public class SingleOverloadCompositor
    : IVoidOverload<SingleDelegate, string>
{
    object IVoidOverload<SingleDelegate, string>.GetInterceptor() => "single_interceptor";
}

public delegate int SingleReturnDelegate(int value);

public class SingleReturnCompositor
    : IMethodOverload<SingleReturnDelegate, int, int>
{
    object IMethodOverload<SingleReturnDelegate, int, int>.GetInterceptor() => "return_interceptor";
}

public class Scenario10_SingleOverloadResolution
{
    // -------------------------------------------------------------------
    // ALL of these compile and resolve correctly - no casts needed!
    // -------------------------------------------------------------------

    [Fact]
    public void Call_ExplicitLambda_WorksDirectly()
    {
        var compositor = new SingleOverloadCompositor();

        // No cast needed - compiler finds the extension method
        var result = compositor.Call((string data) => { });

        Assert.Equal("VoidCall:SingleDelegate", result);
    }

    [Fact]
    public void Call_ImplicitLambda_WorksDirectly()
    {
        var compositor = new SingleOverloadCompositor();

        // Even implicit lambda works! Compiler infers data is string
        var result = compositor.Call((data) => { });

        Assert.Equal("VoidCall:SingleDelegate", result);
    }

    [Fact]
    public void When_WorksDirectly()
    {
        var compositor = new SingleOverloadCompositor();

        var result = compositor.When("hello");

        Assert.Equal("VoidWhen:String", result);
    }

    [Fact]
    public void WhenPredicate_WorksDirectly()
    {
        var compositor = new SingleOverloadCompositor();

        var result = compositor.WhenPredicate((string data) => data == "hello");

        Assert.Equal("VoidWhenPredicate:String", result);
    }

    [Fact]
    public void WhenPredicate_ImplicitLambda_WorksDirectly()
    {
        var compositor = new SingleOverloadCompositor();

        // Implicit lambda in predicate also works
        var result = compositor.WhenPredicate((data) => data == "hello");

        Assert.Equal("VoidWhenPredicate:String", result);
    }

    private static void HandleString(string data) { }

    [Fact]
    public void MethodGroup_WorksDirectly()
    {
        var compositor = new SingleOverloadCompositor();

        var result = compositor.Call(HandleString);

        Assert.Equal("VoidCall:SingleDelegate", result);
    }

    // -------------------------------------------------------------------
    // IMethodOverload single overload also works
    // -------------------------------------------------------------------

    [Fact]
    public void Return_ExplicitLambda_WorksDirectly()
    {
        var compositor = new SingleReturnCompositor();

        var result = compositor.Return((int value) => value * 2);

        Assert.Equal("MethodReturn:SingleReturnDelegate", result);
    }

    [Fact]
    public void Return_ImplicitLambda_WorksDirectly()
    {
        var compositor = new SingleReturnCompositor();

        var result = compositor.Return((value) => value * 2);

        Assert.Equal("MethodReturn:SingleReturnDelegate", result);
    }

    [Fact]
    public void ReturnValue_WorksDirectly()
    {
        var compositor = new SingleReturnCompositor();

        var result = compositor.ReturnValue(42);

        Assert.Equal("MethodReturnValue:Int32=42", result);
    }

    [Fact]
    public void When_OnMethodOverload_WorksDirectly()
    {
        var compositor = new SingleReturnCompositor();

        var result = compositor.When(42);

        Assert.Equal("MethodWhen:Int32", result);
    }
}
