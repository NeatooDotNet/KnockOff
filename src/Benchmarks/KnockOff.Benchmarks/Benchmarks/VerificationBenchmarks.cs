using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using KnockOff.Benchmarks.Interfaces;
using KnockOff.Benchmarks.Stubs;
using Moq;

namespace KnockOff.Benchmarks.Benchmarks;

/// <summary>
/// Measures verification overhead: Verify() expression parsing vs property access.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class VerificationBenchmarks
{
    private Mock<ISimpleService> _moqSimple = null!;
    private SimpleServiceStub _knockOffSimple = null!;
    private Mock<ICalculator> _moqCalculator = null!;
    private CalculatorStub _knockOffCalculator = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Setup mocks with some calls
        _moqSimple = new Mock<ISimpleService>();
        _moqSimple.Object.DoWork();
        _moqSimple.Object.DoWork();
        _moqSimple.Object.DoWork();

        _knockOffSimple = new SimpleServiceStub();
        ((ISimpleService)_knockOffSimple).DoWork();
        ((ISimpleService)_knockOffSimple).DoWork();
        ((ISimpleService)_knockOffSimple).DoWork();

        _moqCalculator = new Mock<ICalculator>();
        _moqCalculator.Setup(x => x.Add(It.IsAny<int>(), It.IsAny<int>())).Returns(0);
        _ = _moqCalculator.Object.Add(1, 2);
        _ = _moqCalculator.Object.Add(3, 4);

        _knockOffCalculator = new CalculatorStub();
        _ = ((ICalculator)_knockOffCalculator).Add(1, 2);
        _ = ((ICalculator)_knockOffCalculator).Add(3, 4);
    }

    // Verify called (at least once)

    [Benchmark(Baseline = true)]
    public void Moq_VerifyCalled()
    {
        _moqSimple.Verify(x => x.DoWork(), Times.AtLeastOnce);
    }

    [Benchmark]
    public void KnockOff_VerifyCalled()
    {
        _ = _knockOffSimple.ISimpleService.DoWork.WasCalled;
    }

    // Verify call count

    [Benchmark]
    public void Moq_VerifyCallCount()
    {
        _moqSimple.Verify(x => x.DoWork(), Times.Exactly(3));
    }

    [Benchmark]
    public void KnockOff_VerifyCallCount()
    {
        _ = _knockOffSimple.ISimpleService.DoWork.CallCount == 3;
    }

    // Verify with argument inspection

    [Benchmark]
    public void Moq_VerifyWithArgs()
    {
        _moqCalculator.Verify(x => x.Add(1, 2), Times.Once);
    }

    [Benchmark]
    public void KnockOff_VerifyWithArgs()
    {
        var args = _knockOffCalculator.ICalculator.Add.LastCallArgs;
        _ = args?.a == 1 && args?.b == 2;
    }

    // Multiple verifications

    [Benchmark]
    public void Moq_VerifyMultiple()
    {
        _moqSimple.Verify(x => x.DoWork(), Times.AtLeastOnce);
        _moqSimple.Verify(x => x.DoWork(), Times.Exactly(3));
        _moqCalculator.Verify(x => x.Add(It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(2));
    }

    [Benchmark]
    public void KnockOff_VerifyMultiple()
    {
        _ = _knockOffSimple.ISimpleService.DoWork.WasCalled;
        _ = _knockOffSimple.ISimpleService.DoWork.CallCount == 3;
        _ = _knockOffCalculator.ICalculator.Add.CallCount == 2;
    }
}
