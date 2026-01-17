using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using KnockOff.Benchmarks.Interfaces;
using KnockOff.Benchmarks.Stubs;
using Moq;

namespace KnockOff.Benchmarks.Benchmarks;

/// <summary>
/// Compares inline stub pattern vs stand-alone stub pattern.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class InlineVsStandaloneCreationBenchmarks
{
    [Benchmark(Baseline = true)]
    public object Moq_CreateSimple() => new Mock<ISimpleService>().Object;

    [Benchmark]
    public object StandAlone_CreateSimple() => new SimpleServiceStub();

    [Benchmark]
    public object Inline_CreateSimple() => new InlineStubs.Stubs.ISimpleService();
}

/// <summary>
/// Measures inline stub invocation overhead.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class InlineStubInvocationBenchmarks
{
    private ISimpleService _moq = null!;
    private ISimpleService _standAlone = null!;
    private ISimpleService _inline = null!;
    private ICalculator _moqCalc = null!;
    private ICalculator _standAloneCalc = null!;
    private ICalculator _inlineCalc = null!;

    [GlobalSetup]
    public void Setup()
    {
        _moq = new Mock<ISimpleService>().Object;
        _standAlone = new SimpleServiceStub();
        _inline = new InlineStubs.Stubs.ISimpleService();

        var moqCalc = new Mock<ICalculator>();
        moqCalc.Setup(x => x.Add(It.IsAny<int>(), It.IsAny<int>())).Returns((int a, int b) => a + b);
        _moqCalc = moqCalc.Object;

        var standAloneCalc = new CalculatorStub();
        standAloneCalc.Add.OnCall((ko, a, b) => a + b);
        _standAloneCalc = standAloneCalc;

        var inlineCalc = new InlineStubs.Stubs.ICalculator();
        inlineCalc.Add.OnCall((stub, a, b) => a + b);
        _inlineCalc = inlineCalc;
    }

    [Benchmark(Baseline = true)]
    public void Moq_InvokeVoid() => _moq.DoWork();

    [Benchmark]
    public void StandAlone_InvokeVoid() => _standAlone.DoWork();

    [Benchmark]
    public void Inline_InvokeVoid() => _inline.DoWork();

    [Benchmark]
    public int Moq_InvokeWithReturn() => _moqCalc.Add(1, 2);

    [Benchmark]
    public int StandAlone_InvokeWithReturn() => _standAloneCalc.Add(1, 2);

    [Benchmark]
    public int Inline_InvokeWithReturn() => _inlineCalc.Add(1, 2);
}

/// <summary>
/// Measures inline stub setup overhead.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class InlineStubSetupBenchmarks
{
    [Benchmark(Baseline = true)]
    public Mock<ICalculator> Moq_SetupCalculator()
    {
        var mock = new Mock<ICalculator>();
        mock.Setup(x => x.Add(It.IsAny<int>(), It.IsAny<int>())).Returns((int a, int b) => a + b);
        mock.Setup(x => x.Subtract(It.IsAny<int>(), It.IsAny<int>())).Returns((int a, int b) => a - b);
        return mock;
    }

    [Benchmark]
    public CalculatorStub StandAlone_SetupCalculator()
    {
        var stub = new CalculatorStub();
        stub.Add.OnCall((ko, a, b) => a + b);
        stub.Subtract.OnCall((ko, a, b) => a - b);
        return stub;
    }

    [Benchmark]
    public InlineStubs.Stubs.ICalculator Inline_SetupCalculator()
    {
        var stub = new InlineStubs.Stubs.ICalculator();
        stub.Add.OnCall((s, a, b) => a + b);
        stub.Subtract.OnCall((s, a, b) => a - b);
        return stub;
    }
}
