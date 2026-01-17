using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using KnockOff.Benchmarks.Interfaces;
using KnockOff.Benchmarks.Stubs;
using Moq;

namespace KnockOff.Benchmarks.Benchmarks;

/// <summary>
/// Measures generic method invocation overhead.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class GenericMethodInvocationBenchmarks
{
    private IConverter _moq = null!;
    private IConverter _knockOff = null!;

    [GlobalSetup]
    public void Setup()
    {
        var mock = new Mock<IConverter>();
        mock.Setup(x => x.Convert<int>(It.IsAny<object>())).Returns(42);
        mock.Setup(x => x.Convert<string>(It.IsAny<object>())).Returns("test");
        mock.Setup(x => x.Transform<int, string>(It.IsAny<int>())).Returns("converted");
        _moq = mock.Object;

        var stub = new ConverterStub();
        stub.Convert.Of<int>().OnCall((ko, v) => 42);
        stub.Convert.Of<string>().OnCall((ko, v) => "test");
        stub.Transform.Of<int, string>().OnCall((ko, v) => "converted");
        _knockOff = stub;
    }

    [Benchmark(Baseline = true)]
    public int Moq_GenericMethodInt() => _moq.Convert<int>("42");

    [Benchmark]
    public int KnockOff_GenericMethodInt() => _knockOff.Convert<int>("42");

    [Benchmark]
    public string Moq_GenericMethodString() => _moq.Convert<string>(42);

    [Benchmark]
    public string KnockOff_GenericMethodString() => _knockOff.Convert<string>(42);

    [Benchmark]
    public string Moq_GenericMethodTwoTypeParams() => _moq.Transform<int, string>(42);

    [Benchmark]
    public string KnockOff_GenericMethodTwoTypeParams() => _knockOff.Transform<int, string>(42);
}

/// <summary>
/// Measures generic method setup overhead.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class GenericMethodSetupBenchmarks
{
    [Benchmark(Baseline = true)]
    public Mock<IConverter> Moq_SetupGenericMethods()
    {
        var mock = new Mock<IConverter>();
        mock.Setup(x => x.Convert<int>(It.IsAny<object>())).Returns(42);
        mock.Setup(x => x.Convert<string>(It.IsAny<object>())).Returns("test");
        return mock;
    }

    [Benchmark]
    public ConverterStub KnockOff_SetupGenericMethods()
    {
        var stub = new ConverterStub();
        stub.Convert.Of<int>().OnCall((ko, v) => 42);
        stub.Convert.Of<string>().OnCall((ko, v) => "test");
        return stub;
    }
}
