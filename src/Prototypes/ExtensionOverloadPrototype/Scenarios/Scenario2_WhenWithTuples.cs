using ExtensionOverloadPrototype.Library;

namespace ExtensionOverloadPrototype.Scenarios;

// ============================================================================
// Scenario 2: When Resolution with Tuples
// Tests whether When() resolves correctly by TArgs type.
//
// FINDING: Same as Scenario 1. Extension methods cannot be discovered on the
// compositor type. Explicit casting to the interface is required.
// ============================================================================

public class Scenario2_WhenWithTuples
{
    // DOES NOT COMPILE: CS1061
    // [Fact]
    // public void When_SingleArg_DirectOnCompositor()
    // {
    //     var compositor = new ProcessCompositor();
    //     var result = compositor.When("hello");
    // }

    [Fact]
    public void When_SingleArg_WithExplicitCast_ResolvesCorrectly()
    {
        var compositor = new ProcessCompositor();

        var asOverload1 = (IVoidOverload<ProcessDelegate1, string>)compositor;
        var result = asOverload1.When("hello");

        Assert.Equal("VoidWhen:String", result);
    }

    [Fact]
    public void When_TwoArgs_WithExplicitCast_ResolvesViaTuple()
    {
        var compositor = new ProcessCompositor();

        var asOverload2 = (IVoidOverload<ProcessDelegate2, (string data, int priority)>)compositor;
        var result = asOverload2.When(("hello", 5));

        Assert.Contains("Tuple", result);
    }

    [Fact]
    public void When_ThreeArgs_WithExplicitCast_ResolvesViaTuple()
    {
        var compositor = new ProcessCompositor();

        var asOverload3 = (IVoidOverload<ProcessDelegate3, (string data, int priority, bool flag)>)compositor;
        var result = asOverload3.When(("hello", 5, true));

        Assert.Contains("Tuple", result);
    }
}
