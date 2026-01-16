using Xunit;
using KnockOff;

namespace KnockOffTests;

public interface IMethodOverloadService
{
    string Format(string input);
    string Format(string input, bool uppercase);
    string Format(string input, int maxLength);
}

[KnockOff]
public partial class MethodOverloadServiceKnockOff : IMethodOverloadService
{
}

public class MethodOverloadTests
{
    [Fact]
    public void OnCall_DifferentOverloads_CompilerResolvesCorrectly()
    {
        var stub = new MethodOverloadServiceKnockOff();

        // Compiler resolves based on lambda parameter types (explicit types needed for disambiguation)
        var tracking1 = stub.Format.OnCall((MethodOverloadServiceKnockOff ko, string input) => input.ToUpper());
        var tracking2 = stub.Format.OnCall((MethodOverloadServiceKnockOff ko, string input, bool uppercase) => uppercase ? input.ToUpper() : input);
        var tracking3 = stub.Format.OnCall((MethodOverloadServiceKnockOff ko, string input, int maxLength) => input.Substring(0, Math.Min(input.Length, maxLength)));

        IMethodOverloadService svc = stub;

        Assert.Equal("HELLO", svc.Format("hello"));
        Assert.Equal("world", svc.Format("world", false));
        Assert.Equal("hel", svc.Format("hello", 3));

        Assert.Equal(1, tracking1.CallCount);
        Assert.Equal(1, tracking2.CallCount);
        Assert.Equal(1, tracking3.CallCount);
    }

    [Fact]
    public void OnCall_EachOverload_TracksIndependently()
    {
        var stub = new MethodOverloadServiceKnockOff();

        var tracking1 = stub.Format.OnCall((MethodOverloadServiceKnockOff ko, string input) => "1");
        var tracking2 = stub.Format.OnCall((MethodOverloadServiceKnockOff ko, string input, bool uppercase) => "2");

        IMethodOverloadService svc = stub;

        svc.Format("a");
        svc.Format("b");
        svc.Format("c", true);

        Assert.Equal(2, tracking1.CallCount);
        Assert.Equal("b", tracking1.LastArg);

        Assert.Equal(1, tracking2.CallCount);
        Assert.Equal(("c", true), tracking2.LastArgs);
    }
}
