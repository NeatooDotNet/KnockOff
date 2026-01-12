using KnockOff.Benchmarks.Benchmarks;
using KnockOff.Benchmarks.Interfaces;
using Rocks;

// Register interfaces for Rocks source generation - Create for strict mocks with verification
[assembly: Rock(typeof(ISimpleService), BuildType.Create)]
[assembly: Rock(typeof(ICalculator), BuildType.Create)]
[assembly: Rock(typeof(IMediumService), BuildType.Create)]
[assembly: Rock(typeof(ILargeService), BuildType.Create)]
[assembly: Rock(typeof(IOrderService), BuildType.Create)]

// Register Make types for loose mocks (no verification needed)
[assembly: Rock(typeof(ISimpleService), BuildType.Make)]
[assembly: Rock(typeof(ICalculator), BuildType.Make)]
[assembly: Rock(typeof(IMediumService), BuildType.Make)]
[assembly: Rock(typeof(ILargeService), BuildType.Make)]
[assembly: Rock(typeof(IOrderService), BuildType.Make)]

// Framework Comparison Benchmark interfaces (matches README Scenario 2: Cached Repository)
[assembly: Rock(typeof(IFcBenchProductRepository), BuildType.Create)]
[assembly: Rock(typeof(IFcBenchCacheService), BuildType.Create)]
[assembly: Rock(typeof(IFcBenchLogger), BuildType.Create)]
