using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using KnockOff.Benchmarks.Interfaces;
using KnockOff.Benchmarks.Stubs;
using Moq;
using Rocks;

namespace KnockOff.Benchmarks.Benchmarks;

/// <summary>
/// Measures realistic unit test scenarios: create -> setup -> act -> verify.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class RealisticBenchmarks
{
    // Simple unit test pattern

    [Benchmark(Baseline = true)]
    public void Moq_SimpleTest()
    {
        // Arrange
        var mock = new Mock<ISimpleService>();
        var callCount = 0;
        mock.Setup(x => x.DoWork()).Callback(() => callCount++);

        // Act
        mock.Object.DoWork();

        // Assert
        mock.Verify(x => x.DoWork(), Times.Once);
    }

    [Benchmark]
    public void KnockOff_SimpleTest()
    {
        // Arrange
        var stub = new SimpleServiceStub();
        var callCount = 0;
        stub.DoWork.OnCall = ko => callCount++;

        // Act
        ((ISimpleService)stub).DoWork();

        // Assert
        _ = stub.DoWork.CallCount == 1;
    }

    [Benchmark]
    public void Rocks_SimpleTest()
    {
        // Arrange
        var expectations = new ISimpleServiceCreateExpectations();
        var callCount = 0;
        expectations.Methods.DoWork().Callback(() => callCount++).ExpectedCallCount(1);
        var mock = expectations.Instance();

        // Act
        mock.DoWork();

        // Assert
        expectations.Verify();
    }

    // Typical unit test with business logic

    [Benchmark]
    public void Moq_TypicalUnitTest()
    {
        // Arrange
        var mock = new Mock<IOrderService>();
        mock.Setup(x => x.GetOrder(It.IsAny<int>()))
            .Returns((int id) => new Order { Id = id, CustomerId = 1 });
        mock.Setup(x => x.ValidateOrder(It.IsAny<Order>()))
            .Returns(true);
        mock.Setup(x => x.CalculateTotal(It.IsAny<Order>()))
            .Returns(100m);

        // Act
        var sut = new OrderProcessor(mock.Object);
        sut.Process(1);

        // Assert
        mock.Verify(x => x.GetOrder(1), Times.Once);
        mock.Verify(x => x.ValidateOrder(It.IsAny<Order>()), Times.Once);
        mock.Verify(x => x.SaveOrder(It.IsAny<Order>()), Times.Once);
    }

    [Benchmark]
    public void KnockOff_TypicalUnitTest()
    {
        // Arrange
        var stub = new OrderServiceStub();
        stub.GetOrder.OnCall = (ko, id) => new Order { Id = id, CustomerId = 1 };
        stub.ValidateOrder.OnCall = (ko, _) => true;
        stub.CalculateTotal.OnCall = (ko, _) => 100m;

        // Act
        var sut = new OrderProcessor(stub);
        sut.Process(1);

        // Assert
        _ = stub.GetOrder.CallCount == 1;
        _ = stub.ValidateOrder.CallCount == 1;
        _ = stub.SaveOrder.CallCount == 1;
    }

    [Benchmark]
    public void Rocks_TypicalUnitTest()
    {
        // Arrange
        var expectations = new IOrderServiceCreateExpectations();
        expectations.Methods.GetOrder(Arg.Any<int>())
            .Callback(id => new Order { Id = id, CustomerId = 1 })
            .ExpectedCallCount(1);
        expectations.Methods.ValidateOrder(Arg.Any<Order>())
            .ReturnValue(true)
            .ExpectedCallCount(1);
        expectations.Methods.CalculateTotal(Arg.Any<Order>())
            .ReturnValue(100m)
            .ExpectedCallCount(1);
        expectations.Methods.SaveOrder(Arg.Any<Order>())
            .ExpectedCallCount(1);
        var mock = expectations.Instance();

        // Act
        var sut = new OrderProcessor(mock);
        sut.Process(1);

        // Assert
        expectations.Verify();
    }
}

/// <summary>
/// Measures test suite overhead (simulating many tests).
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class TestSuiteBenchmarks
{
    [Params(10, 50)]
    public int TestCount { get; set; }

    [Benchmark(Baseline = true)]
    public void Moq_TestSuite()
    {
        for (int i = 0; i < TestCount; i++)
        {
            // Simulate a typical test
            var mock = new Mock<IOrderService>();
            mock.Setup(x => x.GetOrder(It.IsAny<int>()))
                .Returns(new Order { Id = i });
            mock.Setup(x => x.ValidateOrder(It.IsAny<Order>()))
                .Returns(true);

            _ = mock.Object.GetOrder(i);
            _ = mock.Object.ValidateOrder(new Order());

            mock.Verify(x => x.GetOrder(i), Times.Once);
            mock.Verify(x => x.ValidateOrder(It.IsAny<Order>()), Times.Once);
        }
    }

    [Benchmark]
    public void KnockOff_TestSuite()
    {
        for (int i = 0; i < TestCount; i++)
        {
            // Simulate a typical test
            var stub = new OrderServiceStub();
            var capturedI = i;
            stub.GetOrder.OnCall = (ko, _) => new Order { Id = capturedI };
            stub.ValidateOrder.OnCall = (ko, _) => true;

            _ = ((IOrderService)stub).GetOrder(i);
            _ = ((IOrderService)stub).ValidateOrder(new Order());

            _ = stub.GetOrder.CallCount == 1;
            _ = stub.ValidateOrder.CallCount == 1;
        }
    }

    [Benchmark]
    public void Rocks_TestSuite()
    {
        for (int i = 0; i < TestCount; i++)
        {
            // Simulate a typical test
            var expectations = new IOrderServiceCreateExpectations();
            var capturedI = i;
            expectations.Methods.GetOrder(Arg.Any<int>())
                .Callback(_ => new Order { Id = capturedI })
                .ExpectedCallCount(1);
            expectations.Methods.ValidateOrder(Arg.Any<Order>())
                .ReturnValue(true)
                .ExpectedCallCount(1);
            var mock = expectations.Instance();

            _ = mock.GetOrder(i);
            _ = mock.ValidateOrder(new Order());

            expectations.Verify();
        }
    }
}
