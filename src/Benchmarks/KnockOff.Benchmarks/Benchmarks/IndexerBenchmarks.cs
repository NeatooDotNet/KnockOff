using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using KnockOff.Benchmarks.Interfaces;
using KnockOff.Benchmarks.Stubs;
using Moq;

namespace KnockOff.Benchmarks.Benchmarks;

/// <summary>
/// Measures indexer get/set overhead.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class IndexerInvocationBenchmarks
{
    private ICache _moq = null!;
    private ICache _knockOff = null!;

    [GlobalSetup]
    public void Setup()
    {
        var mock = new Mock<ICache>();
        mock.Setup(x => x["key"]).Returns("value");
        mock.Setup(x => x[0]).Returns(42);
        _moq = mock.Object;

        var stub = new CacheStub();
        stub.IndexerString.OnGet = (ko, key) => "value";
        stub.IndexerInt32.OnGet = (ko, index) => 42;
        _knockOff = stub;
    }

    [Benchmark(Baseline = true)]
    public object Moq_StringIndexerGet() => _moq["key"];

    [Benchmark]
    public object KnockOff_StringIndexerGet() => _knockOff["key"];

    [Benchmark]
    public void Moq_StringIndexerSet() => _moq["key"] = "newValue";

    [Benchmark]
    public void KnockOff_StringIndexerSet() => _knockOff["key"] = "newValue";

    [Benchmark]
    public int Moq_IntIndexerGet() => _moq[0];

    [Benchmark]
    public int KnockOff_IntIndexerGet() => _knockOff[0];
}

/// <summary>
/// Measures indexer setup overhead.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class IndexerSetupBenchmarks
{
    [Benchmark(Baseline = true)]
    public Mock<ICache> Moq_SetupIndexers()
    {
        var mock = new Mock<ICache>();
        mock.Setup(x => x["key"]).Returns("value");
        mock.Setup(x => x[0]).Returns(42);
        return mock;
    }

    [Benchmark]
    public CacheStub KnockOff_SetupIndexers()
    {
        var stub = new CacheStub();
        stub.IndexerString.OnGet = (ko, key) => "value";
        stub.IndexerInt32.OnGet = (ko, index) => 42;
        return stub;
    }
}

/// <summary>
/// Measures indexer verification overhead.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class IndexerVerificationBenchmarks
{
    private ICache _moq = null!;
    private CacheStub _knockOffStub = null!;
    private Mock<ICache> _moqMock = null!;

    [GlobalSetup]
    public void Setup()
    {
        _moqMock = new Mock<ICache>();
        _moqMock.Setup(x => x["key"]).Returns("value");
        _moq = _moqMock.Object;

        _knockOffStub = new CacheStub();
        _knockOffStub.IndexerString.OnGet = (ko, key) => "value";

        // Trigger accesses
        _ = _moq["key"];
        _ = ((ICache)_knockOffStub)["key"];
    }

    [Benchmark(Baseline = true)]
    public void Moq_VerifyIndexerAccess()
    {
        _moqMock.Verify(x => x["key"], Times.AtLeastOnce);
    }

    [Benchmark]
    public bool KnockOff_VerifyIndexerAccess()
    {
        return _knockOffStub.IndexerString.GetCount > 0
            && _knockOffStub.IndexerString.LastGetKey == "key";
    }
}
