using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using KnockOff.Benchmarks.Interfaces;
using KnockOff.Benchmarks.Stubs;
using Moq;

namespace KnockOff.Benchmarks.Benchmarks;

/// <summary>
/// Measures setup/configuration overhead: expression parsing vs direct assignment.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class SetupBenchmarks
{
    // Setup single return value

    [Benchmark(Baseline = true)]
    public Mock<ICalculator> Moq_SetupSingleReturn()
    {
        var mock = new Mock<ICalculator>();
        mock.Setup(x => x.Add(It.IsAny<int>(), It.IsAny<int>())).Returns(42);
        return mock;
    }

    [Benchmark]
    public CalculatorStub KnockOff_SetupSingleReturn()
    {
        var stub = new CalculatorStub();
        stub.ICalculator.Add.OnCall = (ko, a, b) => 42;
        return stub;
    }

    // Setup with callback

    [Benchmark]
    public Mock<ICalculator> Moq_SetupWithCallback()
    {
        var mock = new Mock<ICalculator>();
        mock.Setup(x => x.Add(It.IsAny<int>(), It.IsAny<int>()))
            .Returns((int a, int b) => a + b);
        return mock;
    }

    [Benchmark]
    public CalculatorStub KnockOff_SetupWithCallback()
    {
        var stub = new CalculatorStub();
        stub.ICalculator.Add.OnCall = (ko, a, b) => a + b;
        return stub;
    }

    // Setup multiple methods

    [Benchmark]
    public Mock<ICalculator> Moq_SetupMultiple()
    {
        var mock = new Mock<ICalculator>();
        mock.Setup(x => x.Add(It.IsAny<int>(), It.IsAny<int>())).Returns(1);
        mock.Setup(x => x.Subtract(It.IsAny<int>(), It.IsAny<int>())).Returns(2);
        mock.Setup(x => x.Multiply(It.IsAny<int>(), It.IsAny<int>())).Returns(3);
        mock.Setup(x => x.Divide(It.IsAny<double>(), It.IsAny<double>())).Returns(4.0);
        mock.Setup(x => x.Square(It.IsAny<int>())).Returns(5);
        return mock;
    }

    [Benchmark]
    public CalculatorStub KnockOff_SetupMultiple()
    {
        var stub = new CalculatorStub();
        stub.ICalculator.Add.OnCall = (ko, a, b) => 1;
        stub.ICalculator.Subtract.OnCall = (ko, a, b) => 2;
        stub.ICalculator.Multiply.OnCall = (ko, a, b) => 3;
        stub.ICalculator.Divide.OnCall = (ko, a, b) => 4.0;
        stub.ICalculator.Square.OnCall = (ko, x) => 5;
        return stub;
    }
}

/// <summary>
/// Measures setup overhead for void methods (callbacks only).
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class VoidSetupBenchmarks
{
    [Benchmark(Baseline = true)]
    public Mock<ISimpleService> Moq_SetupVoidCallback()
    {
        var callCount = 0;
        var mock = new Mock<ISimpleService>();
        mock.Setup(x => x.DoWork()).Callback(() => callCount++);
        return mock;
    }

    [Benchmark]
    public SimpleServiceStub KnockOff_SetupVoidCallback()
    {
        var callCount = 0;
        var stub = new SimpleServiceStub();
        stub.ISimpleService.DoWork.OnCall = ko => callCount++;
        return stub;
    }
}
