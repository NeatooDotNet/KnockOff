using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using KnockOff;
using KnockOff.Benchmarks.Interfaces;
using KnockOff.Benchmarks.Stubs;
using Moq;

namespace KnockOff.Benchmarks.Benchmarks;

/// <summary>
/// Measures overloaded method invocation overhead.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class OverloadedMethodInvocationBenchmarks
{
    private IOverloadedService _moq = null!;
    private IOverloadedService _knockOff = null!;

    [GlobalSetup]
    public void Setup()
    {
        var mock = new Mock<IOverloadedService>();
        mock.Setup(x => x.Calculate(It.IsAny<int>())).Returns((int v) => v * 2);
        mock.Setup(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>())).Returns((int a, int b) => a + b);
        _moq = mock.Object;

        var stub = new OverloadedServiceStub();
        // Overloaded methods use single interceptor with overloaded OnCall methods
        stub.Calculate.OnCall((OverloadedServiceStub ko, int v) => v * 2);
        stub.Calculate.OnCall((OverloadedServiceStub ko, int a, int b) => a + b);
        _knockOff = stub;
    }

    // Void overloads

    [Benchmark(Baseline = true)]
    public void Moq_InvokeOverloadedVoid_Int() => _moq.Process(42);

    [Benchmark]
    public void KnockOff_InvokeOverloadedVoid_Int() => _knockOff.Process(42);

    [Benchmark]
    public void Moq_InvokeOverloadedVoid_String() => _moq.Process("test");

    [Benchmark]
    public void KnockOff_InvokeOverloadedVoid_String() => _knockOff.Process("test");

    [Benchmark]
    public void Moq_InvokeOverloadedVoid_TwoArgs() => _moq.Process(1, 2);

    [Benchmark]
    public void KnockOff_InvokeOverloadedVoid_TwoArgs() => _knockOff.Process(1, 2);

    // Return value overloads

    [Benchmark]
    public int Moq_InvokeOverloadedReturn_OneArg() => _moq.Calculate(21);

    [Benchmark]
    public int KnockOff_InvokeOverloadedReturn_OneArg() => _knockOff.Calculate(21);

    [Benchmark]
    public int Moq_InvokeOverloadedReturn_TwoArgs() => _moq.Calculate(1, 2);

    [Benchmark]
    public int KnockOff_InvokeOverloadedReturn_TwoArgs() => _knockOff.Calculate(1, 2);
}

/// <summary>
/// Measures overloaded method setup overhead.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class OverloadedMethodSetupBenchmarks
{
    [Benchmark(Baseline = true)]
    public Mock<IOverloadedService> Moq_SetupOverloadedMethods()
    {
        var mock = new Mock<IOverloadedService>();
        mock.Setup(x => x.Process(It.IsAny<int>()));
        mock.Setup(x => x.Process(It.IsAny<string>()));
        mock.Setup(x => x.Process(It.IsAny<int>(), It.IsAny<int>()));
        mock.Setup(x => x.Calculate(It.IsAny<int>())).Returns((int v) => v * 2);
        mock.Setup(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>())).Returns((int a, int b) => a + b);
        return mock;
    }

    [Benchmark]
    public OverloadedServiceStub KnockOff_SetupOverloadedMethods()
    {
        var stub = new OverloadedServiceStub();
        // Overloaded methods use single interceptor with overloaded OnCall methods
        stub.Process.OnCall((OverloadedServiceStub ko, int v) => { });
        stub.Process.OnCall((OverloadedServiceStub ko, string v) => { });
        stub.Process.OnCall((OverloadedServiceStub ko, int a, int b) => { });
        stub.Calculate.OnCall((OverloadedServiceStub ko, int v) => v * 2);
        stub.Calculate.OnCall((OverloadedServiceStub ko, int a, int b) => a + b);
        return stub;
    }
}

/// <summary>
/// Measures overloaded method verification overhead.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class OverloadedMethodVerificationBenchmarks
{
    private Mock<IOverloadedService> _moqMock = null!;
    private OverloadedServiceStub _knockOffStub = null!;
    private IMethodTracking<int> _processIntTracking = null!;
    private IMethodTracking<string> _processStringTracking = null!;
    private IMethodTrackingArgs<(int? a, int? b)> _processTwoArgsTracking = null!;

    [GlobalSetup]
    public void Setup()
    {
        _moqMock = new Mock<IOverloadedService>();
        _moqMock.Object.Process(42);
        _moqMock.Object.Process("test");
        _moqMock.Object.Process(1, 2);

        _knockOffStub = new OverloadedServiceStub();
        // Set up callbacks to get tracking objects
        _processIntTracking = _knockOffStub.Process.OnCall((OverloadedServiceStub ko, int v) => { });
        _processStringTracking = _knockOffStub.Process.OnCall((OverloadedServiceStub ko, string v) => { });
        _processTwoArgsTracking = _knockOffStub.Process.OnCall((OverloadedServiceStub ko, int a, int b) => { });

        ((IOverloadedService)_knockOffStub).Process(42);
        ((IOverloadedService)_knockOffStub).Process("test");
        ((IOverloadedService)_knockOffStub).Process(1, 2);
    }

    [Benchmark(Baseline = true)]
    public void Moq_VerifyOverloadedCalls()
    {
        _moqMock.Verify(x => x.Process(42), Moq.Times.Once);
        _moqMock.Verify(x => x.Process("test"), Moq.Times.Once);
        _moqMock.Verify(x => x.Process(1, 2), Moq.Times.Once);
    }

    [Benchmark]
    public bool KnockOff_VerifyOverloadedCalls()
    {
        return _processIntTracking.WasCalled && _processIntTracking.LastArg == 42
            && _processStringTracking.WasCalled && _processStringTracking.LastArg == "test"
            && _processTwoArgsTracking.WasCalled && _processTwoArgsTracking.LastArgs == (1, 2);
    }
}
