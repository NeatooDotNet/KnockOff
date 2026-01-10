using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using KnockOff.Benchmarks.Interfaces;
using KnockOff.Benchmarks.Stubs;
using Moq;

namespace KnockOff.Benchmarks.Benchmarks;

/// <summary>
/// Measures object creation overhead: proxy generation vs new().
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class CreationBenchmarks
{
    // Simple interface (1 method)

    [Benchmark(Baseline = true)]
    public object Moq_CreateSimple() => new Mock<ISimpleService>().Object;

    [Benchmark]
    public object KnockOff_CreateSimple() => new SimpleServiceStub();

    [Benchmark]
    public object Rocks_CreateSimple() => new ISimpleServiceMakeExpectations().Instance();

    // Calculator interface (5 methods with return values)

    [Benchmark]
    public object Moq_CreateCalculator() => new Mock<ICalculator>().Object;

    [Benchmark]
    public object KnockOff_CreateCalculator() => new CalculatorStub();

    [Benchmark]
    public object Rocks_CreateCalculator() => new ICalculatorMakeExpectations().Instance();

    // Medium interface (10 methods)

    [Benchmark]
    public object Moq_CreateMedium() => new Mock<IMediumService>().Object;

    [Benchmark]
    public object KnockOff_CreateMedium() => new MediumServiceStub();

    [Benchmark]
    public object Rocks_CreateMedium() => new IMediumServiceMakeExpectations().Instance();

    // Large interface (50 methods)

    [Benchmark]
    public object Moq_CreateLarge() => new Mock<ILargeService>().Object;

    [Benchmark]
    public object KnockOff_CreateLarge() => new LargeServiceStub();

    [Benchmark]
    public object Rocks_CreateLarge() => new ILargeServiceMakeExpectations().Instance();

    // Realistic order service

    [Benchmark]
    public object Moq_CreateOrderService() => new Mock<IOrderService>().Object;

    [Benchmark]
    public object KnockOff_CreateOrderService() => new OrderServiceStub();

    [Benchmark]
    public object Rocks_CreateOrderService() => new IOrderServiceMakeExpectations().Instance();
}

/// <summary>
/// Measures cumulative creation overhead for many instances.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class BulkCreationBenchmarks
{
    [Params(100, 1000)]
    public int Count { get; set; }

    [Benchmark(Baseline = true)]
    public void Moq_CreateMany()
    {
        for (int i = 0; i < Count; i++)
        {
            _ = new Mock<ISimpleService>().Object;
        }
    }

    [Benchmark]
    public void KnockOff_CreateMany()
    {
        for (int i = 0; i < Count; i++)
        {
            _ = new SimpleServiceStub();
        }
    }

    [Benchmark]
    public void Rocks_CreateMany()
    {
        for (int i = 0; i < Count; i++)
        {
            _ = new ISimpleServiceMakeExpectations().Instance();
        }
    }
}
