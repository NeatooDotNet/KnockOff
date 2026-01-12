using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using KnockOff.Benchmarks.Interfaces;
using KnockOff.Benchmarks.Stubs;
using Moq;

namespace KnockOff.Benchmarks.Benchmarks;

/// <summary>
/// Measures interface inheritance overhead.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class InheritanceCreationBenchmarks
{
    [Benchmark(Baseline = true)]
    public object Moq_CreateInheritedInterface() => new Mock<ITimestampedEntity>().Object;

    [Benchmark]
    public object KnockOff_CreateInheritedInterface() => new TimestampedEntityStub();

    [Benchmark]
    public object Moq_CreateBaseInterface() => new Mock<IBaseEntity>().Object;

    [Benchmark]
    public object KnockOff_CreateBaseInterface() => new BaseEntityStub();
}

/// <summary>
/// Measures inherited member access overhead.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class InheritanceInvocationBenchmarks
{
    private ITimestampedEntity _moq = null!;
    private ITimestampedEntity _knockOff = null!;
    private readonly DateTime _testDate = new(2024, 1, 1);

    [GlobalSetup]
    public void Setup()
    {
        var mock = new Mock<ITimestampedEntity>();
        mock.Setup(x => x.Id).Returns(1);
        mock.Setup(x => x.CreatedAt).Returns(_testDate);
        mock.Setup(x => x.UpdatedAt).Returns(_testDate);
        _moq = mock.Object;

        var stub = new TimestampedEntityStub();
        stub.Id.Value = 1;
        stub.CreatedAt.Value = _testDate;
        stub.UpdatedAt.Value = _testDate;
        _knockOff = stub;
    }

    // Base interface member
    [Benchmark(Baseline = true)]
    public int Moq_AccessInheritedMember() => _moq.Id;

    [Benchmark]
    public int KnockOff_AccessInheritedMember() => _knockOff.Id;

    // Derived interface member
    [Benchmark]
    public DateTime Moq_AccessDerivedMember() => _moq.CreatedAt;

    [Benchmark]
    public DateTime KnockOff_AccessDerivedMember() => _knockOff.CreatedAt;

    // Nullable derived member
    [Benchmark]
    public DateTime? Moq_AccessNullableMember() => _moq.UpdatedAt;

    [Benchmark]
    public DateTime? KnockOff_AccessNullableMember() => _knockOff.UpdatedAt;
}

/// <summary>
/// Measures inherited interface setup overhead.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class InheritanceSetupBenchmarks
{
    private readonly DateTime _testDate = new(2024, 1, 1);

    [Benchmark(Baseline = true)]
    public Mock<ITimestampedEntity> Moq_SetupInheritedInterface()
    {
        var mock = new Mock<ITimestampedEntity>();
        mock.Setup(x => x.Id).Returns(1);
        mock.Setup(x => x.CreatedAt).Returns(_testDate);
        mock.Setup(x => x.UpdatedAt).Returns(_testDate);
        return mock;
    }

    [Benchmark]
    public TimestampedEntityStub KnockOff_SetupInheritedInterface()
    {
        var stub = new TimestampedEntityStub();
        stub.Id.Value = 1;
        stub.CreatedAt.Value = _testDate;
        stub.UpdatedAt.Value = _testDate;
        return stub;
    }
}
