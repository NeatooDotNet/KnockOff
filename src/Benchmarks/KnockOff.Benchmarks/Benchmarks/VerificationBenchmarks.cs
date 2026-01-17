using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using KnockOff;
using KnockOff.Benchmarks.Interfaces;
using KnockOff.Benchmarks.Stubs;
using Moq;
using Rocks;

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
    private ISimpleServiceCreateExpectations _rocksSimple = null!;
    private Mock<ICalculator> _moqCalculator = null!;
    private CalculatorStub _knockOffCalculator = null!;
    private ICalculatorCreateExpectations _rocksCalculator = null!;
    private IMethodTracking _knockOffSimpleTracking = null!;
    private IMethodTrackingArgs<(int? a, int? b)> _knockOffCalculatorTracking = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Setup mocks with some calls
        _moqSimple = new Mock<ISimpleService>();
        _moqSimple.Object.DoWork();
        _moqSimple.Object.DoWork();
        _moqSimple.Object.DoWork();

        _knockOffSimple = new SimpleServiceStub();
        _knockOffSimpleTracking = _knockOffSimple.DoWork.OnCall(ko => { });
        ((ISimpleService)_knockOffSimple).DoWork();
        ((ISimpleService)_knockOffSimple).DoWork();
        ((ISimpleService)_knockOffSimple).DoWork();

        _rocksSimple = new ISimpleServiceCreateExpectations();
        _rocksSimple.Methods.DoWork().ExpectedCallCount(3);
        var rocksSimpleInstance = _rocksSimple.Instance();
        rocksSimpleInstance.DoWork();
        rocksSimpleInstance.DoWork();
        rocksSimpleInstance.DoWork();

        _moqCalculator = new Mock<ICalculator>();
        _moqCalculator.Setup(x => x.Add(It.IsAny<int>(), It.IsAny<int>())).Returns(0);
        _ = _moqCalculator.Object.Add(1, 2);
        _ = _moqCalculator.Object.Add(3, 4);

        _knockOffCalculator = new CalculatorStub();
        _knockOffCalculatorTracking = _knockOffCalculator.Add.OnCall((ko, a, b) => 0);
        _ = ((ICalculator)_knockOffCalculator).Add(1, 2);
        _ = ((ICalculator)_knockOffCalculator).Add(3, 4);

        _rocksCalculator = new ICalculatorCreateExpectations();
        _rocksCalculator.Methods.Add(Arg.Any<int>(), Arg.Any<int>()).ReturnValue(0).ExpectedCallCount(2);
        var rocksCalcInstance = _rocksCalculator.Instance();
        _ = rocksCalcInstance.Add(1, 2);
        _ = rocksCalcInstance.Add(3, 4);
    }

    // Verify called (at least once)

    [Benchmark(Baseline = true)]
    public void Moq_VerifyCalled()
    {
        _moqSimple.Verify(x => x.DoWork(), Moq.Times.AtLeastOnce());
    }

    [Benchmark]
    public void KnockOff_VerifyCalled()
    {
        _ = _knockOffSimpleTracking.WasCalled;
    }

    [Benchmark]
    public void Rocks_VerifyCalled()
    {
        _rocksSimple.Verify();
    }

    // Verify call count

    [Benchmark]
    public void Moq_VerifyCallCount()
    {
        _moqSimple.Verify(x => x.DoWork(), Moq.Times.Exactly(3));
    }

    [Benchmark]
    public void KnockOff_VerifyCallCount()
    {
        _ = _knockOffSimpleTracking.CallCount == 3;
    }

    [Benchmark]
    public void Rocks_VerifyCallCount()
    {
        _rocksSimple.Verify();
    }

    // Verify with argument inspection

    [Benchmark]
    public void Moq_VerifyWithArgs()
    {
        _moqCalculator.Verify(x => x.Add(1, 2), Moq.Times.Once());
    }

    [Benchmark]
    public void KnockOff_VerifyWithArgs()
    {
        var args = _knockOffCalculatorTracking.LastArgs;
        _ = args.a == 1 && args.b == 2;
    }

    [Benchmark]
    public void Rocks_VerifyWithArgs()
    {
        _rocksCalculator.Verify();
    }

    // Multiple verifications

    [Benchmark]
    public void Moq_VerifyMultiple()
    {
        _moqSimple.Verify(x => x.DoWork(), Moq.Times.AtLeastOnce());
        _moqSimple.Verify(x => x.DoWork(), Moq.Times.Exactly(3));
        _moqCalculator.Verify(x => x.Add(It.IsAny<int>(), It.IsAny<int>()), Moq.Times.Exactly(2));
    }

    [Benchmark]
    public void KnockOff_VerifyMultiple()
    {
        _ = _knockOffSimpleTracking.WasCalled;
        _ = _knockOffSimpleTracking.CallCount == 3;
        _ = _knockOffCalculatorTracking.CallCount == 2;
    }

    [Benchmark]
    public void Rocks_VerifyMultiple()
    {
        _rocksSimple.Verify();
        _rocksCalculator.Verify();
    }
}
