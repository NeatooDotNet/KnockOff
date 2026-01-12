using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using KnockOff.Benchmarks.Interfaces;
using KnockOff.Benchmarks.Stubs;
using Moq;

namespace KnockOff.Benchmarks.Benchmarks;

/// <summary>
/// Measures property get/set overhead.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class PropertyBenchmarks
{
    private IPropertyService _moq = null!;
    private IPropertyService _knockOff = null!;
    private PropertyServiceStub _knockOffStub = null!;

    [GlobalSetup]
    public void Setup()
    {
        var mock = new Mock<IPropertyService>();
        mock.SetupProperty(x => x.Name, "Test");
        mock.Setup(x => x.ReadOnlyValue).Returns(42);
        _moq = mock.Object;

        _knockOffStub = new PropertyServiceStub();
        _knockOffStub.Name.Value = "Test";
        _knockOffStub.ReadOnlyValue.Value = 42;
        _knockOff = _knockOffStub;
    }

    // Property get

    [Benchmark(Baseline = true)]
    public string Moq_PropertyGet() => _moq.Name;

    [Benchmark]
    public string KnockOff_PropertyGet() => _knockOff.Name;

    // Property set

    [Benchmark]
    public void Moq_PropertySet() => _moq.Name = "NewValue";

    [Benchmark]
    public void KnockOff_PropertySet() => _knockOff.Name = "NewValue";

    // Read-only property

    [Benchmark]
    public int Moq_ReadOnlyPropertyGet() => _moq.ReadOnlyValue;

    [Benchmark]
    public int KnockOff_ReadOnlyPropertyGet() => _knockOff.ReadOnlyValue;

    // Write-only property

    [Benchmark]
    public void Moq_WriteOnlyPropertySet() => _moq.WriteOnlyValue = 123;

    [Benchmark]
    public void KnockOff_WriteOnlyPropertySet() => _knockOff.WriteOnlyValue = 123;
}

/// <summary>
/// Measures property setup overhead.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class PropertySetupBenchmarks
{
    [Benchmark(Baseline = true)]
    public Mock<IPropertyService> Moq_SetupProperty()
    {
        var mock = new Mock<IPropertyService>();
        mock.SetupProperty(x => x.Name, "Test");
        return mock;
    }

    [Benchmark]
    public PropertyServiceStub KnockOff_SetupProperty()
    {
        var stub = new PropertyServiceStub();
        stub.Name.Value = "Test";
        return stub;
    }

    [Benchmark]
    public Mock<IPropertyService> Moq_SetupReadOnlyProperty()
    {
        var mock = new Mock<IPropertyService>();
        mock.Setup(x => x.ReadOnlyValue).Returns(42);
        return mock;
    }

    [Benchmark]
    public PropertyServiceStub KnockOff_SetupReadOnlyProperty()
    {
        var stub = new PropertyServiceStub();
        stub.ReadOnlyValue.Value = 42;
        return stub;
    }
}
