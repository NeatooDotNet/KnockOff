// -----------------------------------------------------------------------------
// Design.Tests - Delegate Stub Tests
// -----------------------------------------------------------------------------

using Design.Domain.Delegates;
using Design.Stubs.Advanced;
using KnockOff;

namespace Design.Tests.AdvancedTests;

/// <summary>
/// Tests for delegate stubs.
/// </summary>
public class DelegateStubTests
{
    [Fact]
    public void DelegateStub_ImplicitConversion()
    {
        var stub = new DelegateStubsDemo.Stubs.ArithmeticOperation();
        stub.Interceptor.Return(42);

        ArithmeticOperation operation = stub;

        Assert.NotNull(operation);
    }

    [Fact]
    public void Returns_ConfiguresConstantReturnValue()
    {
        var stub = new DelegateStubsDemo.Stubs.ArithmeticOperation();
        stub.Interceptor.Return(100);

        ArithmeticOperation operation = stub;

        Assert.Equal(100, operation(1, 2));
        Assert.Equal(100, operation(10, 20));
    }

    [Fact]
    public void OnCall_ConfiguresCallback()
    {
        var stub = new DelegateStubsDemo.Stubs.ArithmeticOperation();
        stub.Interceptor.Return((a, b) => a + b);

        ArithmeticOperation operation = stub;

        Assert.Equal(3, operation(1, 2));
        Assert.Equal(30, operation(10, 20));
    }

    [Fact]
    public void LastCallArgs_TracksMultipleArguments()
    {
        var stub = new DelegateStubsDemo.Stubs.ArithmeticOperation();
        stub.Interceptor.Return(0);

        ArithmeticOperation operation = stub;
        operation(5, 10);

        var args = stub.Interceptor.LastArgs;

        Assert.NotNull(args);
        Assert.Equal(5, args.Value.a);
        Assert.Equal(10, args.Value.b);
    }

    [Fact]
    public void VoidDelegate_OnCall()
    {
        var stub = new DelegateStubsDemo.Stubs.LogAction();
        var messages = new List<string>();
        stub.Interceptor.Call(msg => messages.Add(msg));

        LogAction logger = stub;
        logger("Hello");
        logger("World");

        Assert.Equal(["Hello", "World"], messages);
    }

    [Fact]
    public void VoidDelegate_LastCallArg()
    {
        var stub = new DelegateStubsDemo.Stubs.LogAction();

        LogAction logger = stub;
        logger("First");
        logger("Second");

        Assert.Equal("Second", stub.Interceptor.LastArg);
    }

    [Fact]
    public void NoArgDelegate_CallTracking()
    {
        var stub = new DelegateStubsDemo.Stubs.SimpleAction();
        var callCount = 0;
        stub.Interceptor.Call(() => callCount++);

        SimpleAction action = stub;
        action();
        action();
        action();

        Assert.Equal(3, callCount);
        stub.Interceptor.Verify(Times.Exactly(3));
    }

    [Fact]
    public void GenericDelegate_ClosedGeneric()
    {
        var stub = new DelegateStubsDemo.Stubs.Factory();
        stub.Interceptor.Return("Created item");

        Factory<string> factory = stub;

        Assert.Equal("Created item", factory());
    }

    [Fact]
    public void When_ConditionalBehavior()
    {
        var stub = new DelegateStubsDemo.Stubs.ArithmeticOperation();
        stub.Interceptor.When(1, 2).Return(100)
            .ThenWhen(3, 4).Return(200)
            .ThenCall((a, b) => a + b);

        ArithmeticOperation operation = stub;

        Assert.Equal(100, operation(1, 2));
        Assert.Equal(200, operation(3, 4));
        Assert.Equal(11, operation(5, 6)); // Falls to ThenCall
    }

    [Fact]
    public void Verify_ChecksCallCount()
    {
        var stub = new DelegateStubsDemo.Stubs.ArithmeticOperation();
        stub.Interceptor.Return(0);

        ArithmeticOperation operation = stub;
        operation(1, 2);
        operation(3, 4);

        stub.Interceptor.Verify(Times.Exactly(2));
    }

    [Fact]
    public void Reset_ClearsTracking()
    {
        var stub = new DelegateStubsDemo.Stubs.ArithmeticOperation();
        stub.Interceptor.Return(42);

        ArithmeticOperation operation = stub;
        operation(1, 2);

        stub.Interceptor.Reset();

        // Configuration preserved
        Assert.Equal(42, operation(3, 4));

        // But tracking reset
        stub.Interceptor.Verify(Times.Once);
    }
}
