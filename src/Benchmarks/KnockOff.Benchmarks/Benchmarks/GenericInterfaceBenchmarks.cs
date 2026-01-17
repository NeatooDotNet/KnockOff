using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using KnockOff.Benchmarks.Interfaces;
using KnockOff.Benchmarks.Stubs;
using Moq;

namespace KnockOff.Benchmarks.Benchmarks;

/// <summary>
/// Measures generic interface overhead.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class GenericInterfaceCreationBenchmarks
{
    [Benchmark(Baseline = true)]
    public object Moq_CreateGenericRepository() => new Mock<IRepository<Entity>>().Object;

    [Benchmark]
    public object KnockOff_CreateGenericRepository() => new EntityRepositoryStub();
}

/// <summary>
/// Measures generic interface invocation overhead.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class GenericInterfaceInvocationBenchmarks
{
    private IRepository<Entity> _moq = null!;
    private IRepository<Entity> _knockOff = null!;
    private Entity _testEntity = null!;

    [GlobalSetup]
    public void Setup()
    {
        _testEntity = new Entity { Id = 1, Name = "Test" };

        var mock = new Mock<IRepository<Entity>>();
        mock.Setup(x => x.GetById(It.IsAny<int>())).Returns(_testEntity);
        mock.Setup(x => x.GetAll()).Returns(new[] { _testEntity });
        _moq = mock.Object;

        var stub = new EntityRepositoryStub();
        stub.GetById.OnCall((ko, id) => _testEntity);
        stub.GetAll.OnCall((ko) => new[] { _testEntity });
        _knockOff = stub;
    }

    [Benchmark(Baseline = true)]
    public Entity? Moq_GetById() => _moq.GetById(1);

    [Benchmark]
    public Entity? KnockOff_GetById() => _knockOff.GetById(1);

    [Benchmark]
    public void Moq_Save() => _moq.Save(_testEntity);

    [Benchmark]
    public void KnockOff_Save() => _knockOff.Save(_testEntity);

    [Benchmark]
    public List<Entity> Moq_GetAll() => _moq.GetAll().ToList();

    [Benchmark]
    public List<Entity> KnockOff_GetAll() => _knockOff.GetAll().ToList();
}

/// <summary>
/// Measures generic interface setup overhead.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class GenericInterfaceSetupBenchmarks
{
    private readonly Entity _testEntity = new() { Id = 1, Name = "Test" };

    [Benchmark(Baseline = true)]
    public Mock<IRepository<Entity>> Moq_SetupGenericRepository()
    {
        var mock = new Mock<IRepository<Entity>>();
        mock.Setup(x => x.GetById(It.IsAny<int>())).Returns(_testEntity);
        mock.Setup(x => x.GetAll()).Returns(new[] { _testEntity });
        return mock;
    }

    [Benchmark]
    public EntityRepositoryStub KnockOff_SetupGenericRepository()
    {
        var stub = new EntityRepositoryStub();
        stub.GetById.OnCall((ko, id) => _testEntity);
        stub.GetAll.OnCall((ko) => new[] { _testEntity });
        return stub;
    }
}
