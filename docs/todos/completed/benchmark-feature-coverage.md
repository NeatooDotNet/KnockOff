# Benchmark Feature Coverage

Add benchmarks for all KnockOff features currently missing from the BenchmarkDotNet project.

## Background

The current benchmark project only tests:
- Void methods
- Methods with return values (int, string, bool, double, decimal)
- Methods with parameters
- Stand-alone stubs only

Many features tested in `KnockOffTests` have no performance benchmarks.

## Tasks

- [x] Add property interface and benchmarks (get/set/get-only/set-only)
- [x] Add async method interface and benchmarks (Task, Task<T>, ValueTask)
- [x] Add generic interface benchmarks (IRepository<T> pattern)
- [x] Add generic method benchmarks (T GetValue<T>() pattern)
- [x] Add interface inheritance benchmarks (IChild : IBase)
- [x] Add event member benchmarks
- [x] Add indexer benchmarks (this[int], this[string])
- [x] Add ref/out parameter benchmarks
- [x] Add inline stub benchmarks ([KnockOff<T>] pattern)
- [x] Add overloaded method benchmarks
- [x] Add BCL interface benchmarks (IEnumerable<T>, IDisposable)
- [x] Build and verify all benchmarks run successfully

## Implementation Details

Each feature requires:

1. **Interface** in `src/Benchmarks/KnockOff.Benchmarks/Interfaces/`
2. **Stub** in `src/Benchmarks/KnockOff.Benchmarks/Stubs/`
3. **Benchmark class** in `src/Benchmarks/KnockOff.Benchmarks/Benchmarks/`
4. **Moq comparison** where the feature is supported by Moq

### Properties

```csharp
public interface IPropertyService
{
    string Name { get; set; }
    int ReadOnlyValue { get; }
    int WriteOnlyValue { set; }
}
```

Benchmark: property get/set overhead, OnGet/OnSet callback setup.

### Async Methods

```csharp
public interface IAsyncService
{
    Task DoWorkAsync();
    Task<int> GetValueAsync();
    ValueTask<string> GetStringValueAsync();
}
```

Benchmark: async invocation overhead, callback setup for async returns.

### Generic Interfaces

```csharp
public interface IRepository<T> where T : class
{
    T? GetById(int id);
    void Save(T entity);
    IEnumerable<T> GetAll();
}
```

Benchmark: generic type resolution overhead.

### Generic Methods

```csharp
public interface IConverter
{
    T Convert<T>(object value);
    TOut Transform<TIn, TOut>(TIn input);
}
```

Benchmark: generic method dispatch overhead.

### Interface Inheritance

```csharp
public interface IBaseEntity
{
    int Id { get; }
}

public interface ITimestampedEntity : IBaseEntity
{
    DateTime CreatedAt { get; }
    DateTime? UpdatedAt { get; }
}
```

Benchmark: inherited member access overhead.

### Events

```csharp
public interface IEventSource
{
    event EventHandler<string> MessageReceived;
    event Action<int> ValueChanged;
}
```

Benchmark: event subscription/raise overhead.

### Indexers

```csharp
public interface ICache
{
    object this[string key] { get; set; }
    int this[int index] { get; }
}
```

Benchmark: indexer get/set overhead.

### Ref/Out Parameters

```csharp
public interface IParser
{
    bool TryParse(string input, out int result);
    void Increment(ref int value);
}
```

Benchmark: ref/out parameter handling overhead.

### Inline Stubs

```csharp
[KnockOff<ISimpleService>]
[KnockOff<ICalculator>]
public partial class InlineBenchmarkTests { }
```

Benchmark: inline stub creation vs stand-alone, nested class access overhead.

### Overloaded Methods

```csharp
public interface IOverloadedService
{
    void Process(int value);
    void Process(string value);
    void Process(int a, int b);
    int Calculate(int value);
    int Calculate(int a, int b);
}
```

Benchmark: overload resolution overhead (if any).

### BCL Interfaces

```csharp
public interface IDataProvider : IEnumerable<string>, IDisposable
{
    int Count { get; }
}
```

Benchmark: BCL interface implementation overhead, `IEnumerator` generation.

## Notes

- Some features (class stubbing, delegate stubbing) are inline-only and may need special handling
- Moq doesn't support all features (e.g., ref returns) - document where comparisons aren't possible
- Focus on realistic usage patterns, not synthetic edge cases
