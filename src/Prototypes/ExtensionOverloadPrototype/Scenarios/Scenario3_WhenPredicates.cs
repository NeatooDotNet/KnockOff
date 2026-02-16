using ExtensionOverloadPrototype.Library;

namespace ExtensionOverloadPrototype.Scenarios;

// ============================================================================
// Scenario 3: When Predicate Resolution
// Tests whether WhenPredicate() resolves with explicit casts.
//
// FINDING: Same fundamental issue. Extension methods require explicit
// interface casting. Once cast, predicates work correctly.
// ============================================================================

public class Scenario3_WhenPredicates
{
    [Fact]
    public void WhenPredicate_SingleArg_WithCast_ResolvesCorrectly()
    {
        var compositor = new ProcessCompositor();

        var asOverload1 = (IVoidOverload<ProcessDelegate1, string>)compositor;
        var result = asOverload1.WhenPredicate((string data) => data == "hello");

        Assert.Equal("VoidWhenPredicate:String", result);
    }

    [Fact]
    public void WhenPredicate_TwoArgs_WithCast_ResolvesViaTuple()
    {
        var compositor = new ProcessCompositor();

        var asOverload2 = (IVoidOverload<ProcessDelegate2, (string data, int priority)>)compositor;
        var result = asOverload2.WhenPredicate(((string data, int priority) args) => args.data == "hello");

        Assert.Contains("Tuple", result);
    }

    [Fact]
    public void WhenPredicate_ThreeArgs_WithCast_ResolvesViaTuple()
    {
        var compositor = new ProcessCompositor();

        var asOverload3 = (IVoidOverload<ProcessDelegate3, (string data, int priority, bool flag)>)compositor;
        var result = asOverload3.WhenPredicate(((string data, int priority, bool flag) args) => args.data == "hello");

        Assert.Contains("Tuple", result);
    }
}
