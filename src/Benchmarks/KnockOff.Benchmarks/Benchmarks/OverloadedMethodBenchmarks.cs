using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
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
        stub.Calculate1.OnCall = (ko, v) => v * 2;
        stub.Calculate2.OnCall = (ko, a, b) => a + b;
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
        stub.Process1.OnCall = (ko, v) => { };
        stub.Process2.OnCall = (ko, v) => { };
        stub.Process3.OnCall = (ko, a, b) => { };
        stub.Calculate1.OnCall = (ko, v) => v * 2;
        stub.Calculate2.OnCall = (ko, a, b) => a + b;
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

    [GlobalSetup]
    public void Setup()
    {
        _moqMock = new Mock<IOverloadedService>();
        _moqMock.Object.Process(42);
        _moqMock.Object.Process("test");
        _moqMock.Object.Process(1, 2);

        _knockOffStub = new OverloadedServiceStub();
        ((IOverloadedService)_knockOffStub).Process(42);
        ((IOverloadedService)_knockOffStub).Process("test");
        ((IOverloadedService)_knockOffStub).Process(1, 2);
    }

    [Benchmark(Baseline = true)]
    public void Moq_VerifyOverloadedCalls()
    {
        _moqMock.Verify(x => x.Process(42), Times.Once);
        _moqMock.Verify(x => x.Process("test"), Times.Once);
        _moqMock.Verify(x => x.Process(1, 2), Times.Once);
    }

    [Benchmark]
    public bool KnockOff_VerifyOverloadedCalls()
    {
        return _knockOffStub.Process1.WasCalled && _knockOffStub.Process1.LastCallArg == 42
            && _knockOffStub.Process2.WasCalled && _knockOffStub.Process2.LastCallArg == "test"
            && _knockOffStub.Process3.WasCalled && _knockOffStub.Process3.LastCallArgs == (1, 2);
    }
}
