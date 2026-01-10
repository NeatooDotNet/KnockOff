using System.Collections;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using KnockOff.Benchmarks.Interfaces;
using KnockOff.Benchmarks.Stubs;
using Moq;

namespace KnockOff.Benchmarks.Benchmarks;

/// <summary>
/// Measures BCL interface implementation overhead.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class BclInterfaceCreationBenchmarks
{
    [Benchmark(Baseline = true)]
    public object Moq_CreateBclInterface() => new Mock<IDataProvider>().Object;

    [Benchmark]
    public object KnockOff_CreateBclInterface() => new DataProviderStub();
}

/// <summary>
/// Measures BCL interface invocation overhead.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class BclInterfaceInvocationBenchmarks
{
    private IDataProvider _moq = null!;
    private IDataProvider _knockOff = null!;
    private static readonly string[] TestData = ["a", "b", "c"];

    [GlobalSetup]
    public void Setup()
    {
        var mock = new Mock<IDataProvider>();
        mock.Setup(x => x.Count).Returns(3);
        mock.Setup(x => x.GetEnumerator()).Returns(TestData.AsEnumerable().GetEnumerator());
        _moq = mock.Object;

        var stub = new DataProviderStub();
        stub.Count.OnGet = (ko) => 3;
        stub.GetEnumerator.OnCall = (ko) => TestData.AsEnumerable().GetEnumerator();
        _knockOff = stub;
    }

    [Benchmark(Baseline = true)]
    public int Moq_AccessProperty() => _moq.Count;

    [Benchmark]
    public int KnockOff_AccessProperty() => _knockOff.Count;

    [Benchmark]
    public IEnumerator<string> Moq_GetEnumerator() => _moq.GetEnumerator();

    [Benchmark]
    public IEnumerator<string> KnockOff_GetEnumerator() => _knockOff.GetEnumerator();

    [Benchmark]
    public void Moq_Dispose() => _moq.Dispose();

    [Benchmark]
    public void KnockOff_Dispose() => _knockOff.Dispose();
}

/// <summary>
/// Measures BCL interface enumeration overhead.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class BclInterfaceEnumerationBenchmarks
{
    private IDataProvider _moq = null!;
    private IDataProvider _knockOff = null!;
    private static readonly string[] TestData = ["a", "b", "c", "d", "e"];

    [GlobalSetup]
    public void Setup()
    {
        var mock = new Mock<IDataProvider>();
        mock.Setup(x => x.GetEnumerator()).Returns(() => TestData.AsEnumerable().GetEnumerator());
        mock.As<IEnumerable>().Setup(x => x.GetEnumerator()).Returns(() => TestData.GetEnumerator());
        _moq = mock.Object;

        var stub = new DataProviderStub();
        // Non-generic GetEnumerator delegates to the generic one
        stub.GetEnumerator.OnCall = (ko) => TestData.AsEnumerable().GetEnumerator();
        _knockOff = stub;
    }

    [Benchmark(Baseline = true)]
    public int Moq_Enumerate()
    {
        int count = 0;
        foreach (var item in _moq)
        {
            count++;
        }
        return count;
    }

    [Benchmark]
    public int KnockOff_Enumerate()
    {
        int count = 0;
        foreach (var item in _knockOff)
        {
            count++;
        }
        return count;
    }
}

/// <summary>
/// Measures BCL interface setup overhead.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class BclInterfaceSetupBenchmarks
{
    private static readonly string[] TestData = ["a", "b", "c"];

    [Benchmark(Baseline = true)]
    public Mock<IDataProvider> Moq_SetupBclInterface()
    {
        var mock = new Mock<IDataProvider>();
        mock.Setup(x => x.Count).Returns(3);
        mock.Setup(x => x.GetEnumerator()).Returns(TestData.AsEnumerable().GetEnumerator());
        mock.As<IEnumerable>().Setup(x => x.GetEnumerator()).Returns(TestData.GetEnumerator());
        return mock;
    }

    [Benchmark]
    public DataProviderStub KnockOff_SetupBclInterface()
    {
        var stub = new DataProviderStub();
        stub.Count.OnGet = (ko) => 3;
        // Non-generic GetEnumerator delegates to the generic one
        stub.GetEnumerator.OnCall = (ko) => TestData.AsEnumerable().GetEnumerator();
        return stub;
    }
}
