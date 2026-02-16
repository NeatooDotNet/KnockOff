using ExtensionOverloadPrototype.Library;

namespace ExtensionOverloadPrototype.IsolatedTest;

public delegate void TestDelegate1(string data);
public delegate void TestDelegate2(string data, int priority);
public delegate void TestDelegate3(string data, int priority, bool flag);

// Only ONE interface implementation
public class SingleInterfaceCompositor
    : IVoidOverload<TestDelegate1, string>
{
    object IVoidOverload<TestDelegate1, string>.GetInterceptor() => "ov1";
}

// TWO interface implementations
public class TwoInterfaceCompositor
    : IVoidOverload<TestDelegate1, string>,
      IVoidOverload<TestDelegate2, (string data, int priority)>
{
    object IVoidOverload<TestDelegate1, string>.GetInterceptor() => "ov1";
    object IVoidOverload<TestDelegate2, (string data, int priority)>.GetInterceptor() => "ov2";
}

public class Scenario11_ActualCompileTest
{
    [Fact]
    public void SingleInterface_Call_Works()
    {
        var compositor = new SingleInterfaceCompositor();
        var result = compositor.Call((string data) => { });
        Assert.Equal("VoidCall:TestDelegate1", result);
    }

    [Fact]
    public void SingleInterface_When_Works()
    {
        var compositor = new SingleInterfaceCompositor();
        var result = compositor.When("hello");
        Assert.Equal("VoidWhen:String", result);
    }

    [Fact]
    public void TwoInterfaces_Call_1Arg()
    {
        var compositor = new TwoInterfaceCompositor();
        var result = compositor.Call((string data) => { });
        Assert.Equal("VoidCall:TestDelegate1", result);
    }
}
