using ExtensionOverloadPrototype.Library;

namespace ExtensionOverloadPrototype.Scenarios;

// ============================================================================
// Scenario 4: Return Type Overloads (non-void)
// Tests IMethodOverload<TDelegate, TArgs, TReturn> with Return() extension.
//
// FINDING: Same CS1061 issue. Extension methods not discovered on concrete type.
// Requires explicit casts.
// ============================================================================

// "Generated" delegates for:
// int Calculate(int value)
// int Calculate(int a, int b)
public delegate int CalcDelegate1(int value);
public delegate int CalcDelegate2(int a, int b);

// "Generated" compositor for non-void method overloads
public class CalcCompositor
    : IMethodOverload<CalcDelegate1, int, int>,
      IMethodOverload<CalcDelegate2, (int a, int b), int>
{
    object IMethodOverload<CalcDelegate1, int, int>.GetInterceptor() => "calc_interceptor1";
    object IMethodOverload<CalcDelegate2, (int a, int b), int>.GetInterceptor() => "calc_interceptor2";
}

public class Scenario4_ReturnTypeOverloads
{
    // DOES NOT COMPILE: CS1061
    // [Fact]
    // public void Return_DirectOnCompositor_DoesNotCompile()
    // {
    //     var calc = new CalcCompositor();
    //     var result = calc.Return((int value) => value * 2);
    // }

    [Fact]
    public void Return_WithCast_1Arg_ResolvesCorrectly()
    {
        var calc = new CalcCompositor();

        var asOverload1 = (IMethodOverload<CalcDelegate1, int, int>)calc;
        var result = asOverload1.Return((int value) => value * 2);

        Assert.Equal("MethodReturn:CalcDelegate1", result);
    }

    [Fact]
    public void Return_WithCast_2Args_ResolvesCorrectly()
    {
        var calc = new CalcCompositor();

        var asOverload2 = (IMethodOverload<CalcDelegate2, (int a, int b), int>)calc;
        var result = asOverload2.Return((int a, int b) => a + b);

        Assert.Equal("MethodReturn:CalcDelegate2", result);
    }

    [Fact]
    public void Call_WithCast_OnMethodOverload_ResolvesCorrectly()
    {
        var calc = new CalcCompositor();

        var asOverload1 = (IMethodOverload<CalcDelegate1, int, int>)calc;
        var result = asOverload1.Call((int value) => value * 2);

        Assert.Equal("MethodCall:CalcDelegate1", result);
    }

    [Fact]
    public void When_WithCast_SingleArg_ResolvesCorrectly()
    {
        var calc = new CalcCompositor();

        var asOverload1 = (IMethodOverload<CalcDelegate1, int, int>)calc;
        var result = asOverload1.When(42);

        Assert.Equal("MethodWhen:Int32", result);
    }

    [Fact]
    public void When_WithCast_TupleArg_ResolvesCorrectly()
    {
        var calc = new CalcCompositor();

        var asOverload2 = (IMethodOverload<CalcDelegate2, (int a, int b), int>)calc;
        var result = asOverload2.When((10, 20));

        Assert.Contains("Tuple", result);
    }
}
