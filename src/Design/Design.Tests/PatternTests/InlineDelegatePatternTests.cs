// -----------------------------------------------------------------------------
// Design.Tests - Inline Delegate Pattern Tests
// -----------------------------------------------------------------------------

using Design.Domain.Delegates;
using Design.Stubs.StubPatterns;

namespace Design.Tests.PatternTests;

/// <summary>
/// Tests for the inline delegate pattern: [KnockOff{T}] where T is a delegate type.
/// </summary>
public class InlineDelegatePatternTests
{
    [Fact]
    public void InlineDelegate_GeneratesStubClass()
    {
        var stub = new InlineDelegateExample.Stubs.ArithmeticOperation();

        Assert.NotNull(stub);
    }

    [Fact]
    public void InlineDelegate_ImplicitConversionToDelegate()
    {
        var stub = new InlineDelegateExample.Stubs.ArithmeticOperation();
        stub.Interceptor.Returns(42);

        ArithmeticOperation operation = stub;

        Assert.NotNull(operation);
    }

    [Fact]
    public void InlineDelegate_Returns_ConfiguresConstantReturnValue()
    {
        var stub = new InlineDelegateExample.Stubs.ArithmeticOperation();
        stub.Interceptor.Returns(100);

        ArithmeticOperation operation = stub;
        var result = operation(5, 10);

        Assert.Equal(100, result);
    }

    [Fact]
    public void InlineDelegate_OnCall_ConfiguresCallback()
    {
        var stub = new InlineDelegateExample.Stubs.ArithmeticOperation();
        stub.Interceptor.OnCall((a, b) => a + b);

        ArithmeticOperation operation = stub;
        var result = operation(5, 10);

        Assert.Equal(15, result);
    }

    [Fact]
    public void InlineDelegate_LastCallArgs_TracksArguments()
    {
        var stub = new InlineDelegateExample.Stubs.ArithmeticOperation();
        stub.Interceptor.Returns(0);

        ArithmeticOperation operation = stub;
        operation(7, 3);

        var args = stub.Interceptor.LastCallArgs;

        Assert.NotNull(args);
        Assert.Equal(7, args.Value.a);
        Assert.Equal(3, args.Value.b);
    }

    [Fact]
    public void InlineDelegate_VoidDelegate_OnCallOnly()
    {
        var stub = new InlineDelegateExample.Stubs.LogAction();
        string? capturedMessage = null;
        stub.Interceptor.OnCall(msg => capturedMessage = msg);

        LogAction logger = stub;
        logger("Hello, World!");

        Assert.Equal("Hello, World!", capturedMessage);
    }

    [Fact]
    public void InlineDelegate_VoidDelegate_LastCallArg()
    {
        var stub = new InlineDelegateExample.Stubs.LogAction();

        LogAction logger = stub;
        logger("Test message");

        Assert.Equal("Test message", stub.Interceptor.LastCallArg);
    }
}
