using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using KnockOff.Benchmarks.Interfaces;
using KnockOff.Benchmarks.Stubs;
using Moq;

namespace KnockOff.Benchmarks.Benchmarks;

/// <summary>
/// Measures method invocation overhead: interception vs direct calls.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class InvocationBenchmarks
{
    private ISimpleService _moqSimple = null!;
    private ISimpleService _knockOffSimple = null!;
    private ICalculator _moqCalculator = null!;
    private ICalculator _knockOffCalculator = null!;
    private IMediumService _moqMedium = null!;
    private IMediumService _knockOffMedium = null!;

    [GlobalSetup]
    public void Setup()
    {
        _moqSimple = new Mock<ISimpleService>().Object;
        _knockOffSimple = new SimpleServiceStub();

        var moqCalc = new Mock<ICalculator>();
        moqCalc.Setup(x => x.Add(It.IsAny<int>(), It.IsAny<int>())).Returns(0);
        _moqCalculator = moqCalc.Object;

        var knockOffCalc = new CalculatorStub();
        knockOffCalc.Add.OnCall = (ko, a, b) => a + b;
        _knockOffCalculator = knockOffCalc;

        _moqMedium = new Mock<IMediumService>().Object;
        _knockOffMedium = new MediumServiceStub();
    }

    // Void method, no args - purest interception overhead

    [Benchmark(Baseline = true)]
    public void Moq_InvokeVoid() => _moqSimple.DoWork();

    [Benchmark]
    public void KnockOff_InvokeVoid() => _knockOffSimple.DoWork();

    // Method with return value

    [Benchmark]
    public int Moq_InvokeWithReturn() => _moqCalculator.Add(1, 2);

    [Benchmark]
    public int KnockOff_InvokeWithReturn() => _knockOffCalculator.Add(1, 2);

    // Method with primitive args

    [Benchmark]
    public void Moq_InvokeWithArgs() => _moqMedium.Method2(42);

    [Benchmark]
    public void KnockOff_InvokeWithArgs() => _knockOffMedium.Method2(42);

    // Method with string arg

    [Benchmark]
    public void Moq_InvokeWithStringArg() => _moqMedium.Method3("test");

    [Benchmark]
    public void KnockOff_InvokeWithStringArg() => _knockOffMedium.Method3("test");

    // Method with multiple args

    [Benchmark]
    public void Moq_InvokeWithMultipleArgs() => _moqMedium.Method4(42, "test");

    [Benchmark]
    public void KnockOff_InvokeWithMultipleArgs() => _knockOffMedium.Method4(42, "test");
}

/// <summary>
/// Measures cumulative invocation overhead in tight loops.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class BulkInvocationBenchmarks
{
    private ISimpleService _moqSimple = null!;
    private ISimpleService _knockOffSimple = null!;

    [Params(1000, 10000)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _moqSimple = new Mock<ISimpleService>().Object;
        _knockOffSimple = new SimpleServiceStub();
    }

    [Benchmark(Baseline = true)]
    public void Moq_InvokeMany()
    {
        for (int i = 0; i < Count; i++)
        {
            _moqSimple.DoWork();
        }
    }

    [Benchmark]
    public void KnockOff_InvokeMany()
    {
        for (int i = 0; i < Count; i++)
        {
            _knockOffSimple.DoWork();
        }
    }
}
