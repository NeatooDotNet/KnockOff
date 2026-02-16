using NumberedSlotPrototype.Consumer;
using NumberedSlotPrototype.Library;

namespace NumberedSlotPrototype.Tests;

/// <summary>
/// Tests that Call() and When() extension methods resolve to the correct slot
/// when the compositor implements multiple IVoidOverloadSlot interfaces.
///
/// KEY TEST: These call extension methods DIRECTLY on the compositor variable
/// without explicit casts. If this compiles and works, numbered slots solve the problem.
/// </summary>
public class VoidOverloadResolutionTests
{
    // ---- Call resolution ----

    [Fact]
    public void Call_1Arg_ResolvesToSlot1()
    {
        var compositor = new ProcessCompositor();
        var result = compositor.Call((string data) => { });
        Assert.Equal("VoidCall:ProcessDelegate1", result);
    }

    [Fact]
    public void Call_2Args_ResolvesToSlot2()
    {
        var compositor = new ProcessCompositor();
        var result = compositor.Call((string data, int priority) => { });
        Assert.Equal("VoidCall:ProcessDelegate2", result);
    }

    [Fact]
    public void Call_3Args_ResolvesToSlot3()
    {
        var compositor = new ProcessCompositor();
        var result = compositor.Call((string data, int priority, bool @async) => { });
        Assert.Equal("VoidCall:ProcessDelegate3", result);
    }

    // ---- When resolution ----

    [Fact]
    public void When_String_ResolvesToSlot1()
    {
        var compositor = new ProcessCompositor();
        var result = compositor.When("hello");
        Assert.Equal("VoidWhen:String:hello", result);
    }

    [Fact]
    public void When_Tuple2_ResolvesToSlot2()
    {
        var compositor = new ProcessCompositor();
        var result = compositor.When(("hello", 42));
        Assert.Equal("VoidWhen:ValueTuple`2:(hello, 42)", result);
    }

    [Fact]
    public void When_Tuple3_ResolvesToSlot3()
    {
        var compositor = new ProcessCompositor();
        var result = compositor.When(("hello", 42, true));
        Assert.Equal("VoidWhen:ValueTuple`3:(hello, 42, True)", result);
    }

    // ---- Verify per-slot ----

    [Fact]
    public void Verify_Slot1_ResolvesCorrectly()
    {
        var compositor = new ProcessCompositor();

        // Verify requires explicit slot selection since there's no argument to disambiguate.
        // This tests whether the compiler can resolve Verify() on a numbered slot.
        // NOTE: With no arguments, the compiler cannot infer the type parameters,
        // so Verify() likely needs a cast or explicit type arguments.
        // Let's test the cast approach since it's the minimal generated code.
        var asSlot1 = (IVoidOverloadSlot1<ProcessDelegate1, string>)compositor;
        var result = asSlot1.Verify();
        Assert.Equal("VoidVerify:ProcessDelegate1", result);
    }

    [Fact]
    public void Verify_Slot2_ResolvesCorrectly()
    {
        var compositor = new ProcessCompositor();
        var asSlot2 = (IVoidOverloadSlot2<ProcessDelegate2, (string, int)>)compositor;
        var result = asSlot2.Verify();
        Assert.Equal("VoidVerify:ProcessDelegate2", result);
    }
}
