# Benchmark: Moq vs KnockOff

**Goal**: Measure if source generation provides real performance benefits over runtime mocking.

## Status

- [x] Create benchmark project
- [x] Define interfaces at varying complexity
- [x] Create KnockOff stubs for each interface
- [x] Implement creation benchmarks
- [x] Implement invocation benchmarks
- [x] Implement setup benchmarks
- [x] Implement verification benchmarks
- [x] Implement realistic scenario benchmarks
- [ ] Run benchmarks and analyze results
- [ ] Document findings

---

## Why Source Generation Should Be Faster

**Moq's Runtime Overhead:**
1. **Proxy generation** - Castle.Core's DynamicProxy creates types at runtime
2. **Expression tree parsing** - `Setup(x => x.Method())` compiles expressions
3. **Interception chain** - Every call goes through interceptor pipeline
4. **Reflection** - Runtime type inspection for interface members

**KnockOff's Advantages:**
1. **Pre-compiled code** - Stubs are generated at compile time
2. **Direct calls** - No interception, just regular method calls
3. **Direct assignment** - `stub.Spy.Method.OnCall = ...` vs expression parsing
4. **Direct verification** - `stub.Spy.Method.CallCount` vs `Verify()` parsing

---

## What to Benchmark

### Category 1: Object Creation

Tests proxy generation overhead vs `new Stub()`

| Scenario | Why It Matters |
|----------|----------------|
| Simple interface (1 method) | Baseline overhead |
| Medium interface (10 methods) | Typical real-world size |
| Large interface (50 methods) | Stress test proxy generation |
| Many instances (1000x simple) | Cumulative overhead in large test suites |

### Category 2: Method Invocation

Tests interception overhead vs direct calls

| Scenario | Why It Matters |
|----------|----------------|
| Void method, no args | Purest interception overhead |
| Method with return value | Return value handling |
| Method with primitive args | Argument capture overhead |
| Method with complex args | Object handling in interceptors |
| Tight loop (10,000 calls) | Cumulative invocation overhead |

### Category 3: Setup/Configuration

Tests expression parsing vs direct assignment

| Scenario | Why It Matters |
|----------|----------------|
| Setup single return value | Basic setup cost |
| Setup with callback | Delegate assignment |
| Setup 10 methods | Multiple setups per mock |
| Setup with argument capture | `Callback<T>` vs direct |

### Category 4: Verification

Tests `Verify()` expression parsing vs property access

| Scenario | Why It Matters |
|----------|----------------|
| Verify called once | Basic verification |
| Verify call count | Count comparison |
| Verify with arg inspection | Argument verification |
| Multiple verifications | Cumulative in assertions |

### Category 5: Realistic Scenarios

Tests complete unit test patterns

| Scenario | Why It Matters |
|----------|----------------|
| Simple unit test | Create -> Setup -> Act -> Verify |
| Test with 3 dependencies | Multiple mocks per test |
| Test with 10 interactions | Many calls and verifications |
| "Test class" (20 tests) | Suite-level overhead |

### Category 6: Memory Allocation

Track with `[MemoryDiagnoser]`

- Allocations per stub creation
- Allocations per method call
- Allocations per setup/verify
- Total allocations for realistic test

---

## Project Structure

```
src/Benchmarks/
└── KnockOff.Benchmarks/
    ├── KnockOff.Benchmarks.csproj
    ├── Program.cs
    │
    ├── Interfaces/
    │   ├── ISimpleService.cs           # 1 void method
    │   ├── ICalculator.cs              # Methods with returns
    │   ├── IMediumService.cs           # 10 methods
    │   ├── ILargeService.cs            # 50 methods
    │   └── IOrderService.cs            # Realistic: typical business interface
    │
    ├── Stubs/                          # KnockOff implementations
    │   ├── SimpleServiceStub.cs
    │   ├── CalculatorStub.cs
    │   ├── MediumServiceStub.cs
    │   ├── LargeServiceStub.cs
    │   └── OrderServiceStub.cs
    │
    ├── Benchmarks/
    │   ├── CreationBenchmarks.cs
    │   ├── InvocationBenchmarks.cs
    │   ├── SetupBenchmarks.cs
    │   ├── VerificationBenchmarks.cs
    │   ├── RealisticBenchmarks.cs
    │   └── MemoryBenchmarks.cs
    │
    └── Generated/                      # Source-generated output
```

---

## Benchmark Implementation Examples

### Creation Benchmarks

```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class CreationBenchmarks
{
    [Benchmark(Baseline = true)]
    public object Moq_CreateSimple() => new Mock<ISimpleService>().Object;

    [Benchmark]
    public object KnockOff_CreateSimple() => new SimpleServiceStub();
}
```

### Invocation Benchmarks

```csharp
[MemoryDiagnoser]
public class InvocationBenchmarks
{
    private ISimpleService _moqService;
    private ISimpleService _knockOffService;

    [GlobalSetup]
    public void Setup()
    {
        _moqService = new Mock<ISimpleService>().Object;
        _knockOffService = new SimpleServiceStub();
    }

    [Benchmark(Baseline = true)]
    public void Moq_InvokeVoid() => _moqService.DoWork();

    [Benchmark]
    public void KnockOff_InvokeVoid() => _knockOffService.DoWork();
}
```

### Realistic Benchmarks

```csharp
[MemoryDiagnoser]
public class RealisticBenchmarks
{
    [Benchmark(Baseline = true)]
    public void Moq_TypicalUnitTest()
    {
        // Arrange
        var mock = new Mock<IOrderService>();
        mock.Setup(x => x.GetOrder(It.IsAny<int>()))
            .Returns(new Order { Id = 1 });

        // Act
        var sut = new OrderProcessor(mock.Object);
        sut.Process(1);

        // Assert
        mock.Verify(x => x.GetOrder(1), Times.Once);
        mock.Verify(x => x.SaveOrder(It.IsAny<Order>()), Times.Once);
    }

    [Benchmark]
    public void KnockOff_TypicalUnitTest()
    {
        // Arrange
        var stub = new OrderServiceStub();
        stub.Spy.GetOrder.OnCall = id => new Order { Id = id };

        // Act
        var sut = new OrderProcessor(stub);
        sut.Process(1);

        // Assert
        Debug.Assert(stub.Spy.GetOrder.CallCount == 1);
        Debug.Assert(stub.Spy.SaveOrder.CallCount == 1);
    }
}
```

---

## Fairness Considerations

| Aspect | Approach |
|--------|----------|
| **Equivalent functionality** | Compare only features both support |
| **Moq configuration** | Use `MockBehavior.Loose` (default) |
| **Argument matching** | Skip `It.IsAny<T>()` comparisons (KnockOff doesn't have this) |
| **Cold vs warm** | Run enough iterations that JIT is warmed up |
| **Same runtime** | Both run on same .NET version |

---

## Expected Results Hypothesis

| Category | Expected Winner | Magnitude |
|----------|-----------------|-----------|
| Object creation | KnockOff | 10-100x faster |
| Method invocation | KnockOff | 2-10x faster |
| Setup/configuration | KnockOff | 5-50x faster |
| Verification | KnockOff | 5-20x faster |
| Memory allocation | KnockOff | 5-50x less |
| Realistic test | KnockOff | 3-10x faster |

---

## Commands

```bash
# Run benchmarks (must be Release mode)
dotnet run -c Release --project src/Benchmarks/KnockOff.Benchmarks

# Run specific benchmark class
dotnet run -c Release --project src/Benchmarks/KnockOff.Benchmarks -- --filter *Creation*

# Quick test run (fewer iterations)
dotnet run -c Release --project src/Benchmarks/KnockOff.Benchmarks -- --job short
```
