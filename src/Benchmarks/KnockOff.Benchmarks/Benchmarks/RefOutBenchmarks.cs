using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using KnockOff.Benchmarks.Interfaces;
using KnockOff.Benchmarks.Stubs;
using Moq;

namespace KnockOff.Benchmarks.Benchmarks;

/// <summary>
/// Measures ref/out parameter overhead.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class RefOutInvocationBenchmarks
{
    private IParser _moq = null!;
    private IParser _knockOff = null!;

    [GlobalSetup]
    public void Setup()
    {
        var mock = new Mock<IParser>();
        mock.Setup(x => x.TryParse("42", out It.Ref<int>.IsAny))
            .Returns((string input, out int result) =>
            {
                result = 42;
                return true;
            });
        mock.Setup(x => x.Increment(ref It.Ref<int>.IsAny))
            .Callback((ref int value) => value++);
        _moq = mock.Object;

        var stub = new ParserStub();
        stub.TryParse.OnCall((ParserStub ko, string input, out int result) =>
        {
            result = 42;
            return true;
        });
        stub.Increment.OnCall((ParserStub ko, ref int value) => value++);
        _knockOff = stub;
    }

    [Benchmark(Baseline = true)]
    public bool Moq_TryParseOut()
    {
        return _moq.TryParse("42", out _);
    }

    [Benchmark]
    public bool KnockOff_TryParseOut()
    {
        return _knockOff.TryParse("42", out _);
    }

    [Benchmark]
    public int Moq_IncrementRef()
    {
        int value = 0;
        _moq.Increment(ref value);
        return value;
    }

    [Benchmark]
    public int KnockOff_IncrementRef()
    {
        int value = 0;
        _knockOff.Increment(ref value);
        return value;
    }
}

/// <summary>
/// Measures ref/out parameter setup overhead.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class RefOutSetupBenchmarks
{
    [Benchmark(Baseline = true)]
    public Mock<IParser> Moq_SetupRefOut()
    {
        var mock = new Mock<IParser>();
        mock.Setup(x => x.TryParse("42", out It.Ref<int>.IsAny))
            .Returns((string input, out int result) =>
            {
                result = 42;
                return true;
            });
        mock.Setup(x => x.Increment(ref It.Ref<int>.IsAny))
            .Callback((ref int value) => value++);
        return mock;
    }

    [Benchmark]
    public ParserStub KnockOff_SetupRefOut()
    {
        var stub = new ParserStub();
        stub.TryParse.OnCall((ParserStub ko, string input, out int result) =>
        {
            result = 42;
            return true;
        });
        stub.Increment.OnCall((ParserStub ko, ref int value) => value++);
        return stub;
    }
}
