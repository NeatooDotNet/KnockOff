# Interface Stub Tests for KnockOff Robustness

## Goal

Create a comprehensive **regression test suite** for the most commonly used .NET interfaces. All KnockOff features (generics, inheritance, events, async, etc.) are already implemented—these tests validate correctness and prevent regressions.

Priorities are based on [BigQuery analysis of GitHub C# code](common-dotnet-interfaces-research.md), ordered by real-world usage frequency.

Each interface gets:
1. **Inline stub** - stub class defined within the test class
2. **Standalone stub** - stub class in a separate file
3. **Tests** - verify stub behavior and interceptor functionality

## Task List

### Priority 1: Lifecycle (1,177 implementations)

- [ ] IDisposable
  - [ ] Inline stub with Dispose tracking
  - [ ] Standalone stub
  - [ ] Tests: Dispose called, multiple dispose calls, using statement
- [ ] IAsyncDisposable
  - [ ] Inline stub with DisposeAsync tracking
  - [ ] Standalone stub
  - [ ] Tests: DisposeAsync called, await using statement

### Priority 2: Comparison/Equality (866+ implementations)

- [ ] IEquatable&lt;T&gt;
  - [ ] Inline stub with Equals tracking
  - [ ] Standalone stub
  - [ ] Tests: equality comparison, null handling, self-referential generic
- [ ] IComparer&lt;T&gt; (334 implementations)
  - [ ] Inline stub with Compare tracking
  - [ ] Standalone stub
  - [ ] Tests: custom sorting, OrderBy compatibility
- [ ] IComparable&lt;T&gt; (320 implementations)
  - [ ] Inline stub with typed CompareTo tracking
  - [ ] Standalone stub
  - [ ] Tests: typed sorting, List.Sort compatibility
- [ ] IEqualityComparer&lt;T&gt; (297 implementations)
  - [ ] Inline stub with Equals/GetHashCode tracking
  - [ ] Standalone stub
  - [ ] Tests: Dictionary/HashSet compatibility

### Priority 3: Collections - Generic (491+ implementations)

- [ ] IEnumerable&lt;T&gt;
  - [ ] Inline stub with GetEnumerator tracking
  - [ ] Standalone stub
  - [ ] Tests: foreach, LINQ queries, covariant type parameter
- [ ] IEnumerator&lt;T&gt; (432 implementations)
  - [ ] Inline stub with typed Current tracking
  - [ ] Standalone stub
  - [ ] Tests: typed iteration, multiple interface inheritance (IEnumerator, IDisposable)
- [ ] IList&lt;T&gt; (179 implementations)
  - [ ] Inline stub with typed indexer tracking
  - [ ] Standalone stub
  - [ ] Tests: typed index access, IndexOf, interface inheritance chain
- [ ] ICollection&lt;T&gt; (166 implementations)
  - [ ] Inline stub with typed Add/Remove/Contains tracking
  - [ ] Standalone stub
  - [ ] Tests: typed operations, IsReadOnly behavior
- [ ] IDictionary&lt;TKey, TValue&gt; (127 implementations)
  - [ ] Inline stub with typed key/value tracking
  - [ ] Standalone stub
  - [ ] Tests: typed operations, TryGetValue, two type parameters

### Priority 4: Notifications/MVVM (230+ implementations)

- [ ] INotifyPropertyChanged
  - [ ] Inline stub with event tracking
  - [ ] Standalone stub
  - [ ] Tests: event subscription, PropertyChanged firing
- [ ] ICommand (140 implementations)
  - [ ] Inline stub with Execute/CanExecute tracking
  - [ ] Standalone stub
  - [ ] Tests: command execution, CanExecuteChanged event
- [ ] INotifyPropertyChanging (37 implementations)
  - [ ] Inline stub with event tracking
  - [ ] Standalone stub
  - [ ] Tests: event subscription, PropertyChanging firing

### Priority 5: Read-Only Collections

- [ ] IReadOnlyList&lt;T&gt;
  - [ ] Inline stub with indexer tracking
  - [ ] Standalone stub
  - [ ] Tests: index access, enumeration, covariant type parameter
- [ ] IReadOnlyCollection&lt;T&gt;
  - [ ] Inline stub with Count tracking
  - [ ] Standalone stub
  - [ ] Tests: enumeration, count access
- [ ] IReadOnlyDictionary&lt;TKey, TValue&gt;
  - [ ] Inline stub with key lookup tracking
  - [ ] Standalone stub
  - [ ] Tests: TryGetValue, Keys/Values properties

### Priority 6: DI/Logging (68+ implementations)

- [ ] ILogger
  - [ ] Inline stub with Log tracking
  - [ ] Standalone stub
  - [ ] Tests: log levels, IsEnabled
  - **Package:** Microsoft.Extensions.Logging.Abstractions
- [ ] ILogger&lt;T&gt;
  - [ ] Inline stub (inherits ILogger)
  - [ ] Standalone stub
  - [ ] Tests: category name, scopes
  - **Package:** Microsoft.Extensions.Logging.Abstractions
- [ ] IServiceProvider (36 implementations)
  - [ ] Inline stub with GetService tracking
  - [ ] Standalone stub
  - [ ] Tests: service resolution, null returns
  - **Package:** Microsoft.Extensions.DependencyInjection.Abstractions

### Priority 7: Async/Streaming

- [ ] IAsyncEnumerable&lt;T&gt;
  - [ ] Inline stub with GetAsyncEnumerator tracking
  - [ ] Standalone stub
  - [ ] Tests: await foreach, cancellation token
- [ ] IAsyncEnumerator&lt;T&gt;
  - [ ] Inline stub with MoveNextAsync/Current tracking
  - [ ] Standalone stub
  - [ ] Tests: async iteration, disposal
- [ ] IObservable&lt;T&gt; (13 implementations)
  - [ ] Inline stub with Subscribe tracking
  - [ ] Standalone stub
  - [ ] Tests: subscription, IDisposable return
- [ ] IObserver&lt;T&gt; (17 implementations)
  - [ ] Inline stub with OnNext/OnError/OnCompleted tracking
  - [ ] Standalone stub
  - [ ] Tests: notification sequence

### Priority 8: Formatting

- [ ] IFormattable (26 implementations)
  - [ ] Inline stub with ToString tracking
  - [ ] Standalone stub
  - [ ] Tests: format string, format provider
- [ ] IFormatProvider
  - [ ] Inline stub with GetFormat tracking
  - [ ] Standalone stub
  - [ ] Tests: culture-specific formatting

### Priority 9: Data Access

- [ ] IDbConnection
  - [ ] Inline stub with Open/Close/CreateCommand tracking
  - [ ] Standalone stub
  - [ ] Tests: connection lifecycle
  - **Package:** System.Data.Common
- [ ] IDbCommand
  - [ ] Inline stub with ExecuteReader/ExecuteNonQuery tracking
  - [ ] Standalone stub
  - [ ] Tests: command execution
  - **Package:** System.Data.Common
- [ ] IDataReader
  - [ ] Inline stub with Read/GetValue tracking
  - [ ] Standalone stub
  - [ ] Tests: row iteration, field access
  - **Package:** System.Data.Common

### Priority 10: Edge Cases

Custom test interfaces designed to stress the generator. Some validate correct behavior, others document expected limitations.

#### Must Work Correctly

- [ ] Member named `Value`
  - [ ] Interface with property named `Value` - tests naming conflict resolution
  - [ ] Tests: interceptor still accessible, no compilation errors
- [ ] Member named `OnCall`, `CallCount`, `WasCalled`
  - [ ] Interface with members matching interceptor API names
  - [ ] Tests: no naming collisions in generated code
- [ ] C# keywords as member names
  - [ ] Interface with `@class`, `@event`, `@object` members
  - [ ] Tests: proper escaping in generated code
- [ ] Covariant type parameter (`out T`)
  - [ ] Custom `IProducer<out T>` interface
  - [ ] Tests: variance preserved, assignable to base type
- [ ] Contravariant type parameter (`in T`)
  - [ ] Custom `IConsumer<in T>` interface
  - [ ] Tests: variance preserved, assignable from derived type
- [ ] Tuple return types
  - [ ] Method returning `(int x, string y)`
  - [ ] Tests: tuple tracking in LastCallArgs, OnCall receives tuple
- [ ] Nullable reference types
  - [ ] Properties and methods with `string?`, `T?` annotations
  - [ ] Tests: nullability preserved in generated code
- [ ] Empty marker interface
  - [ ] `interface IMarker { }` with no members
  - [ ] Tests: stub compiles, no interceptors generated
- [ ] Multiple type parameters
  - [ ] `interface IConverter<TIn, TOut>` with method `TOut Convert(TIn input)`
  - [ ] Tests: both type parameters handled correctly

#### Documented Limitations (By Design)

- [ ] `new` keyword shadowing
  - [ ] `interface IChild : IParent { new void Method(); }`
  - [ ] **Behavior:** Only top-level member is intercepted; shadowed member ignored
  - [ ] Tests: verify documented behavior, no runtime errors
- [ ] Default interface implementations (C# 8+)
  - [ ] `interface IFoo { void Bar() => Console.WriteLine("default"); }`
  - [ ] **Behavior:** Default implementation not inherited; stub provides own implementation
  - [ ] Tests: verify documented behavior
- [ ] Explicit interface implementation in base
  - [ ] **Behavior:** Document how KnockOff handles this scenario
  - [ ] Tests: verify documented behavior

---

### Deprioritized: Legacy Interfaces

Per research recommendations, these legacy interfaces have low usage and limited value:

- [ ] ICloneable (114 implementations) - Legacy pattern, not recommended
- [ ] ISerializable (52 implementations) - Legacy serialization
- [ ] IXmlSerializable (24 implementations) - Legacy XML serialization
- [ ] IConvertible - Rarely implemented directly
- [ ] Non-generic collections (IEnumerable, IList, ICollection, IDictionary) - Prefer generic versions

---

## Test File Structure

All interface regression tests go in a dedicated project:

```
src/Tests/KnockOff.Tests.RegressionTests/
├── KnockOff.Tests.RegressionTests.csproj
├── Lifecycle/
│   ├── IDisposableStubTests.cs
│   └── IAsyncDisposableStubTests.cs
├── Comparison/
│   ├── IEquatableStubTests.cs
│   ├── IComparerStubTests.cs
│   ├── IComparableStubTests.cs
│   └── IEqualityComparerStubTests.cs
├── Collections/
│   ├── IEnumerableStubTests.cs
│   ├── IEnumeratorStubTests.cs
│   ├── IListStubTests.cs
│   ├── ICollectionStubTests.cs
│   ├── IDictionaryStubTests.cs
│   └── ...
├── ReadOnlyCollections/
│   ├── IReadOnlyListStubTests.cs
│   ├── IReadOnlyCollectionStubTests.cs
│   └── IReadOnlyDictionaryStubTests.cs
├── Notifications/
│   ├── INotifyPropertyChangedStubTests.cs
│   ├── INotifyPropertyChangingStubTests.cs
│   └── ICommandStubTests.cs
├── Logging/
│   ├── ILoggerStubTests.cs
│   └── ILoggerOfTStubTests.cs
├── DependencyInjection/
│   └── IServiceProviderStubTests.cs
├── Async/
│   ├── IAsyncEnumerableStubTests.cs
│   ├── IAsyncEnumeratorStubTests.cs
│   ├── IObservableStubTests.cs
│   └── IObserverStubTests.cs
├── Formatting/
│   ├── IFormattableStubTests.cs
│   └── IFormatProviderStubTests.cs
├── DataAccess/
│   ├── IDbConnectionStubTests.cs
│   ├── IDbCommandStubTests.cs
│   └── IDataReaderStubTests.cs
├── EdgeCases/
│   ├── NamingConflictTests.cs        # Value, OnCall, CallCount as member names
│   ├── KeywordEscapingTests.cs       # @class, @event, @object
│   ├── VarianceTests.cs              # out T, in T
│   ├── TupleReturnTests.cs           # (int x, string y)
│   ├── NullableTests.cs              # string?, T?
│   ├── MarkerInterfaceTests.cs       # empty interfaces
│   ├── MultiTypeParameterTests.cs    # IConverter<TIn, TOut>
│   └── DocumentedLimitationsTests.cs # new shadowing, DIMs
└── Standalone/
    ├── DisposableStub.cs
    ├── EnumerableStub.cs
    ├── LoggerStub.cs
    └── ...
```

---

## Test Pattern Template

### Inline Stub Example

```csharp
public class IDisposableStubTests
{
    // Inline stub definition
    [KnockOff]
    public partial class DisposableStub : IDisposable
    {
        // User can add custom behavior here
    }

    [Fact]
    public void Dispose_TracksCall()
    {
        var stub = new DisposableStub();

        ((IDisposable)stub).Dispose();

        Assert.True(stub.IDisposable.Dispose.WasCalled);
        Assert.Equal(1, stub.IDisposable.Dispose.CallCount);
    }

    [Fact]
    public void Dispose_CallbackInvoked()
    {
        var stub = new DisposableStub();
        var disposed = false;
        stub.IDisposable.Dispose.OnCall = () => disposed = true;

        ((IDisposable)stub).Dispose();

        Assert.True(disposed);
    }

    [Fact]
    public void Dispose_WorksWithUsingStatement()
    {
        var stub = new DisposableStub();

        using (stub as IDisposable) { }

        Assert.True(stub.IDisposable.Dispose.WasCalled);
    }
}
```

### Standalone Stub Example

```csharp
// In separate file: Standalone/DisposableStub.cs
[KnockOff]
public partial class StandaloneDisposableStub : IDisposable
{
    // Standalone stub for reuse across tests
}
```

---

## Acceptance Criteria

For each interface, tests must verify:

1. **Interceptor tracking**
   - CallCount increments correctly
   - WasCalled returns true after invocation
   - LastCallArgs captures parameters (if any)

2. **Callback invocation**
   - OnCall delegate is invoked
   - Callback can modify behavior/return value

3. **Reset functionality**
   - Reset() clears call tracking
   - Reset() doesn't affect callbacks

4. **Real-world usage**
   - Works with language constructs (using, foreach, await)
   - Compatible with LINQ and framework APIs
   - Handles edge cases (null, empty, exceptions)

---

## Implementation Order

Start with Priority 1-3 (most commonly used), then proceed through remaining priorities. Each interface should be fully tested before moving to the next.

**Estimated scope**:
- BCL interfaces: ~45 interfaces × 2 stub types × ~4 tests = ~360 test cases
- Edge cases: ~12 scenarios × ~3 tests = ~36 test cases
- **Total: ~396 test cases**

**Project**: `KnockOff.Tests.RegressionTests` (includes all external package dependencies)
