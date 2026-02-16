using NumberedSlotPrototype.Consumer;
using NumberedSlotPrototype.Library;

namespace NumberedSlotPrototype.Tests;

/// <summary>
/// Tests a compositor with only one overload slot.
/// Verifies the pattern works when there's no overloading at all.
/// </summary>
public class SingleOverloadTests
{
    [Fact]
    public void Return_SingleSlot_ResolvesCorrectly()
    {
        var compositor = new SingleOverloadCompositor();
        var result = compositor.Return((string url) => $"response from {url}");
        Assert.Equal("MethodReturn:FetchDelegate", result);
    }

    [Fact]
    public void When_SingleSlot_ResolvesCorrectly()
    {
        var compositor = new SingleOverloadCompositor();
        var result = compositor.When("https://example.com");
        Assert.Equal("MethodWhen:String:https://example.com", result);
    }

    [Fact]
    public void Verify_SingleSlot_ResolvesCorrectly()
    {
        var compositor = new SingleOverloadCompositor();
        var asSlot1 = (IMethodOverloadSlot1<FetchDelegate, string, string>)compositor;
        var result = asSlot1.Verify();
        Assert.Equal("MethodVerify:FetchDelegate", result);
    }
}
