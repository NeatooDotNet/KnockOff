using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using KnockOff.Benchmarks.Interfaces;
using KnockOff.Benchmarks.Stubs;
using Moq;

namespace KnockOff.Benchmarks.Benchmarks;

/// <summary>
/// Measures async method invocation overhead.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class AsyncInvocationBenchmarks
{
    private IAsyncService _moq = null!;
    private IAsyncService _knockOff = null!;

    [GlobalSetup]
    public void Setup()
    {
        var mock = new Mock<IAsyncService>();
        mock.Setup(x => x.DoWorkAsync()).Returns(Task.CompletedTask);
        mock.Setup(x => x.GetValueAsync()).ReturnsAsync(42);
        mock.Setup(x => x.GetStringValueAsync()).Returns(new ValueTask<string>("test"));
        _moq = mock.Object;

        var stub = new AsyncServiceStub();
        stub.DoWorkAsync.OnCall = (ko) => Task.CompletedTask;
        stub.GetValueAsync.OnCall = (ko) => Task.FromResult(42);
        stub.GetStringValueAsync.OnCall = (ko) => new ValueTask<string>("test");
        _knockOff = stub;
    }

    // Task (void async)

    [Benchmark(Baseline = true)]
    public Task Moq_InvokeTaskVoid() => _moq.DoWorkAsync();

    [Benchmark]
    public Task KnockOff_InvokeTaskVoid() => _knockOff.DoWorkAsync();

    // Task<T>

    [Benchmark]
    public Task<int> Moq_InvokeTaskWithResult() => _moq.GetValueAsync();

    [Benchmark]
    public Task<int> KnockOff_InvokeTaskWithResult() => _knockOff.GetValueAsync();

    // ValueTask<T>

    [Benchmark]
    public ValueTask<string> Moq_InvokeValueTask() => _moq.GetStringValueAsync();

    [Benchmark]
    public ValueTask<string> KnockOff_InvokeValueTask() => _knockOff.GetStringValueAsync();
}

/// <summary>
/// Measures async method setup overhead.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class AsyncSetupBenchmarks
{
    [Benchmark(Baseline = true)]
    public Mock<IAsyncService> Moq_SetupAsyncMethods()
    {
        var mock = new Mock<IAsyncService>();
        mock.Setup(x => x.DoWorkAsync()).Returns(Task.CompletedTask);
        mock.Setup(x => x.GetValueAsync()).ReturnsAsync(42);
        mock.Setup(x => x.GetStringValueAsync()).Returns(new ValueTask<string>("test"));
        return mock;
    }

    [Benchmark]
    public AsyncServiceStub KnockOff_SetupAsyncMethods()
    {
        var stub = new AsyncServiceStub();
        stub.DoWorkAsync.OnCall = (ko) => Task.CompletedTask;
        stub.GetValueAsync.OnCall = (ko) => Task.FromResult(42);
        stub.GetStringValueAsync.OnCall = (ko) => new ValueTask<string>("test");
        return stub;
    }
}
