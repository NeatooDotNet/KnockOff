using NumberedSlotPrototype.Consumer;
using NumberedSlotPrototype.Library;

namespace NumberedSlotPrototype.Tests;

/// <summary>
/// Tests a compositor that has both void and non-void overload slots.
/// Verifies that IVoidOverloadSlot and IMethodOverloadSlot can coexist.
/// </summary>
public class MixedFamilyTests
{
    [Fact]
    public void Call_VoidOverload_ResolvesCorrectly()
    {
        var compositor = new MixedCompositor();
        var result = compositor.Call((string command) => { });
        Assert.Equal("VoidCall:ExecuteVoidDelegate", result);
    }

    [Fact]
    public void Return_MethodOverload_ResolvesCorrectly()
    {
        var compositor = new MixedCompositor();
        var result = compositor.Return((string command, int timeout) => timeout);
        Assert.Equal("MethodReturn:ExecuteIntDelegate", result);
    }

    [Fact]
    public void When_VoidSlot_ResolvesCorrectly()
    {
        var compositor = new MixedCompositor();

        // When("hello") should resolve to the void slot (TArgs = string)
        var result = compositor.When("hello");
        Assert.Equal("VoidWhen:String:hello", result);
    }

    [Fact]
    public void When_MethodSlot_ResolvesCorrectly()
    {
        var compositor = new MixedCompositor();

        // When(("command", 30)) should resolve to the method slot (TArgs = (string, int))
        var result = compositor.When(("command", 30));
        Assert.Equal("MethodWhen:ValueTuple`2:(command, 30)", result);
    }

    [Fact]
    public void Verify_VoidSlot_WithCast()
    {
        var compositor = new MixedCompositor();
        var asVoid = (IVoidOverloadSlot1<ExecuteVoidDelegate, string>)compositor;
        var result = asVoid.Verify();
        Assert.Equal("VoidVerify:ExecuteVoidDelegate", result);
    }

    [Fact]
    public void Verify_MethodSlot_WithCast()
    {
        var compositor = new MixedCompositor();
        var asMethod = (IMethodOverloadSlot1<ExecuteIntDelegate, (string, int), int>)compositor;
        var result = asMethod.Verify();
        Assert.Equal("MethodVerify:ExecuteIntDelegate", result);
    }
}
