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
        var tracking1 = stub.Format.Return((string input) => input.ToUpper());
        var tracking2 = stub.Format.Return((string input, bool uppercase) => uppercase ? input.ToUpper() : input);
        var tracking3 = stub.Format.Return((string input, int maxLength) => input.Substring(0, Math.Min(input.Length, maxLength)));

        IMethodOverloadService svc = stub;

        Assert.Equal("HELLO", svc.Format("hello"));
        Assert.Equal("world", svc.Format("world", false));
        Assert.Equal("hel", svc.Format("hello", 3));

        tracking1.Verify(Called.Once);
        tracking2.Verify(Called.Once);
        tracking3.Verify(Called.Once);
    }

    [Fact]
    public void OnCall_EachOverload_TracksIndependently()
    {
        var stub = new MethodOverloadServiceKnockOff();

        var tracking1 = stub.Format.Return((string input) => "1");
        var tracking2 = stub.Format.Return((string input, bool uppercase) => "2");

        IMethodOverloadService svc = stub;

        svc.Format("a");
        svc.Format("b");
        svc.Format("c", true);

        tracking1.Verify(Called.Exactly(2));
        Assert.Equal("b", tracking1.LastArg);

        tracking2.Verify(Called.Once);
        Assert.Equal(("c", true), tracking2.LastArgs);
    }
}
