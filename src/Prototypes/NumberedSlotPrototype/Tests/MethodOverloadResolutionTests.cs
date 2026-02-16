using NumberedSlotPrototype.Consumer;
using NumberedSlotPrototype.Library;

namespace NumberedSlotPrototype.Tests;

/// <summary>
/// Tests that Return() and When() extension methods resolve to the correct slot
/// for non-void method overloads.
/// </summary>
public class MethodOverloadResolutionTests
{
    // ---- Return resolution ----

    [Fact]
    public void Return_1Arg_ResolvesToSlot1()
    {
        var compositor = new CalculateCompositor();
        var result = compositor.Return((int x) => x * 2);
        Assert.Equal("MethodReturn:CalculateDelegate1", result);
    }

    [Fact]
    public void Return_2Args_ResolvesToSlot2()
    {
        var compositor = new CalculateCompositor();
        var result = compositor.Return((int x, int y) => x + y);
        Assert.Equal("MethodReturn:CalculateDelegate2", result);
    }

    // ---- When resolution ----

    [Fact]
    public void When_SingleInt_ResolvesToSlot1()
    {
        var compositor = new CalculateCompositor();
        var result = compositor.When(5);
        Assert.Equal("MethodWhen:Int32:5", result);
    }

    [Fact]
    public void When_Tuple2Ints_ResolvesToSlot2()
    {
        var compositor = new CalculateCompositor();
        var result = compositor.When((3, 7));
        Assert.Equal("MethodWhen:ValueTuple`2:(3, 7)", result);
    }

    // ---- Verify per-slot ----

    [Fact]
    public void Verify_Slot1_ResolvesCorrectly()
    {
        var compositor = new CalculateCompositor();
        var asSlot1 = (IMethodOverloadSlot1<CalculateDelegate1, int, int>)compositor;
        var result = asSlot1.Verify();
        Assert.Equal("MethodVerify:CalculateDelegate1", result);
    }

    [Fact]
    public void Verify_Slot2_ResolvesCorrectly()
    {
        var compositor = new CalculateCompositor();
        var asSlot2 = (IMethodOverloadSlot2<CalculateDelegate2, (int, int), int>)compositor;
        var result = asSlot2.Verify();
        Assert.Equal("MethodVerify:CalculateDelegate2", result);
    }
}
