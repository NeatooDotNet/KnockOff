using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using KnockOff.Benchmarks.Interfaces;
using KnockOff.Benchmarks.Stubs;
using Moq;

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
        stub.ISimpleService.DoWork.OnCall = ko => callCount++;

        // Act
        ((ISimpleService)stub).DoWork();

        // Assert
        _ = stub.ISimpleService.DoWork.CallCount == 1;
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
        stub.IOrderService.GetOrder.OnCall = (ko, id) => new Order { Id = id, CustomerId = 1 };
        stub.IOrderService.ValidateOrder.OnCall = (ko, _) => true;
        stub.IOrderService.CalculateTotal.OnCall = (ko, _) => 100m;

        // Act
        var sut = new OrderProcessor(stub);
        sut.Process(1);

        // Assert
        _ = stub.IOrderService.GetOrder.CallCount == 1;
        _ = stub.IOrderService.ValidateOrder.CallCount == 1;
        _ = stub.IOrderService.SaveOrder.CallCount == 1;
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
            stub.IOrderService.GetOrder.OnCall = (ko, _) => new Order { Id = capturedI };
            stub.IOrderService.ValidateOrder.OnCall = (ko, _) => true;

            _ = ((IOrderService)stub).GetOrder(i);
            _ = ((IOrderService)stub).ValidateOrder(new Order());

            _ = stub.IOrderService.GetOrder.CallCount == 1;
            _ = stub.IOrderService.ValidateOrder.CallCount == 1;
        }
    }
}
