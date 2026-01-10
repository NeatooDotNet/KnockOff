using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using KnockOff.Benchmarks.Interfaces;
using KnockOff.Benchmarks.Stubs;
using Moq;

namespace KnockOff.Benchmarks.Benchmarks;

/// <summary>
/// Measures event subscription overhead.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class EventSubscriptionBenchmarks
{
    private IEventSource _moq = null!;
    private IEventSource _knockOff = null!;
    private EventSourceStub _knockOffStub = null!;
    private Mock<IEventSource> _moqMock = null!;

    [GlobalSetup]
    public void Setup()
    {
        _moqMock = new Mock<IEventSource>();
        _moq = _moqMock.Object;

        _knockOffStub = new EventSourceStub();
        _knockOff = _knockOffStub;
    }

    [Benchmark(Baseline = true)]
    public void Moq_SubscribeToEvent()
    {
        _moq.MessageReceived += (s, e) => { };
    }

    [Benchmark]
    public void KnockOff_SubscribeToEvent()
    {
        _knockOff.MessageReceived += (s, e) => { };
    }

    [Benchmark]
    public void Moq_SubscribeToActionEvent()
    {
        _moq.ValueChanged += (v) => { };
    }

    [Benchmark]
    public void KnockOff_SubscribeToActionEvent()
    {
        _knockOff.ValueChanged += (v) => { };
    }
}

/// <summary>
/// Measures event raise overhead.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class EventRaiseBenchmarks
{
    private Mock<IEventSource> _moqMock = null!;
    private EventSourceStub _knockOffStub = null!;
    private int _handlerCallCount;

    [GlobalSetup]
    public void Setup()
    {
        _moqMock = new Mock<IEventSource>();
        _moqMock.Object.MessageReceived += (s, e) => _handlerCallCount++;

        _knockOffStub = new EventSourceStub();
        ((IEventSource)_knockOffStub).MessageReceived += (s, e) => _handlerCallCount++;
    }

    [Benchmark(Baseline = true)]
    public void Moq_RaiseEvent()
    {
        _moqMock.Raise(x => x.MessageReceived += null, this, "test");
    }

    [Benchmark]
    public void KnockOff_RaiseEvent()
    {
        _knockOffStub.MessageReceived.Raise(this, "test");
    }
}

/// <summary>
/// Measures event verification overhead.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class EventVerificationBenchmarks
{
    private EventSourceStub _knockOffStub = null!;
    private Mock<IEventSource> _moqMock = null!;

    [GlobalSetup]
    public void Setup()
    {
        _moqMock = new Mock<IEventSource>();
        _moqMock.Object.MessageReceived += (s, e) => { };
        _moqMock.Object.MessageReceived += (s, e) => { };

        _knockOffStub = new EventSourceStub();
        ((IEventSource)_knockOffStub).MessageReceived += (s, e) => { };
        ((IEventSource)_knockOffStub).MessageReceived += (s, e) => { };
    }

    [Benchmark(Baseline = true)]
    public bool Moq_VerifyEventSubscribed()
    {
        // Moq doesn't have direct subscription verification
        // We can only verify raises, not subscriptions
        return true;
    }

    [Benchmark]
    public bool KnockOff_VerifyEventSubscribed()
    {
        return _knockOffStub.MessageReceived.HasSubscribers
            && _knockOffStub.MessageReceived.AddCount == 2;
    }
}
