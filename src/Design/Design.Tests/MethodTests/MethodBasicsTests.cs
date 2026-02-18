// -----------------------------------------------------------------------------
// Design.Tests - Method Basics Tests
// -----------------------------------------------------------------------------

using Design.Stubs.Methods;
using KnockOff;

namespace Design.Tests.MethodTests;

/// <summary>
/// Tests for basic method stubbing: Return, Call, argument capture.
/// </summary>
public class MethodBasicsTests
{
    [Fact]
    public void Returns_ConfiguresConstantReturnValue()
    {
        var stub = new BasicMethodsDemo.Stubs.ICalculator();
        stub.Add.Return(42);

        Design.Domain.Services.ICalculator calc = stub;

        Assert.Equal(42, calc.Add(1, 2));
        Assert.Equal(42, calc.Add(100, 200));
    }

    [Fact]
    public void OnCall_ConfiguresCallback()
    {
        var stub = new BasicMethodsDemo.Stubs.ICalculator();
        stub.Add.Call(args => args.a + args.b);

        Design.Domain.Services.ICalculator calc = stub;

        Assert.Equal(3, calc.Add(1, 2));
        Assert.Equal(300, calc.Add(100, 200));
    }

    [Fact]
    public void LastCallArgs_CapturesArguments()
    {
        var stub = new BasicMethodsDemo.Stubs.ICalculator();

        Design.Domain.Services.ICalculator calc = stub;
        calc.Add(5, 10);

        Assert.Equal((5, 10), stub.Add.LastArgs);
    }

    [Fact]
    public void LastCallArgs_UpdatesOnEachCall()
    {
        var stub = new BasicMethodsDemo.Stubs.ICalculator();

        Design.Domain.Services.ICalculator calc = stub;
        calc.Add(1, 2);
        calc.Add(3, 4);

        Assert.Equal((3, 4), stub.Add.LastArgs);
    }

    [Fact]
    public void VoidMethod_OnCallWithAction()
    {
        var stub = new BasicMethodsDemo.Stubs.ICalculator();
        var resetCalled = false;
        stub.Reset.Call(() => resetCalled = true);

        Design.Domain.Services.ICalculator calc = stub;
        calc.Reset();

        Assert.True(resetCalled);
    }

    [Fact]
    public void Verify_ThrowsWhenNotCalled()
    {
        var stub = new BasicMethodsDemo.Stubs.ICalculator();
        stub.Add.Return(0);

        Assert.Throws<VerificationException>(() => stub.Add.Verify());
    }

    [Fact]
    public void Verify_PassesWhenCalled()
    {
        var stub = new BasicMethodsDemo.Stubs.ICalculator();
        stub.Add.Return(0);

        Design.Domain.Services.ICalculator calc = stub;
        calc.Add(1, 2);

        stub.Add.Verify(); // Should not throw
    }

    [Fact]
    public void Verify_WithTimes_ChecksCallCount()
    {
        var stub = new BasicMethodsDemo.Stubs.ICalculator();

        Design.Domain.Services.ICalculator calc = stub;
        calc.Add(1, 2);
        calc.Add(3, 4);

        stub.Add.Verify(Called.Exactly(2));
    }

    [Fact]
    public void Reset_ClearsTracking()
    {
        var stub = new BasicMethodsDemo.Stubs.ICalculator();
        stub.Add.Return(42);

        Design.Domain.Services.ICalculator calc = stub;
        calc.Add(1, 2);

        stub.Add.Reset();

        // Configuration preserved
        Assert.Equal(42, calc.Add(3, 4));

        // But call count reset - verify fails
        Assert.Throws<VerificationException>(() => stub.Add.Verify(Called.Exactly(2)));
    }
}
