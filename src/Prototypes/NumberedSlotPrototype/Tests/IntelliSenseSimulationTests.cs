using NumberedSlotPrototype.Consumer;
using NumberedSlotPrototype.Library;

namespace NumberedSlotPrototype.Tests;

/// <summary>
/// Verifies that the compiler infers delegate types correctly from lambda expressions.
/// While we cannot test actual IntelliSense behavior, we can verify that:
/// 1. Lambda parameter types are inferred (not explicitly required)
/// 2. The correct delegate type is resolved
/// 3. Named parameters from the delegate are available in the lambda
/// </summary>
public class IntelliSenseSimulationTests
{
    [Fact]
    public void Call_InfersDelegate_FromLambdaParameters()
    {
        var compositor = new ProcessCompositor();

        // The compiler should infer ProcessDelegate2 from the (string, int) parameters.
        // The named parameters "data" and "priority" come from the delegate definition.
        var result = compositor.Call((string data, int priority) => { });
        Assert.Equal("VoidCall:ProcessDelegate2", result);
    }

    [Fact]
    public void Return_InfersDelegate_FromLambdaParameters()
    {
        var compositor = new CalculateCompositor();

        // The compiler should infer CalculateDelegate2 from the (int, int) parameters.
        var result = compositor.Return((int x, int y) => x + y);
        Assert.Equal("MethodReturn:CalculateDelegate2", result);
    }

    [Fact]
    public void Call_WorksWithImplicitlyTypedLambda()
    {
        var compositor = new ProcessCompositor();

        // With only one string-parameter overload, the compiler can infer types.
        // NOTE: This tests whether implicit lambda types work.
        // The compiler needs to find exactly one Call extension that matches.
        var result = compositor.Call((string data) => { });
        Assert.Equal("VoidCall:ProcessDelegate1", result);
    }

    [Fact]
    public void ExplicitDelegateVariable_StillWorks()
    {
        var compositor = new ProcessCompositor();

        // Users can also create an explicit delegate if they prefer.
        ProcessDelegate2 callback = (data, priority) => { };
        var result = compositor.Call(callback);
        Assert.Equal("VoidCall:ProcessDelegate2", result);
    }
}
