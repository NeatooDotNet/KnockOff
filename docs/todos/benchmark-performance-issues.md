# Benchmark Performance Issues

Analysis from benchmark run on 2026-01-05.

## Task List

- [ ] Fix NA results in InvocationBenchmarks (List.Add allocation in hot path)
- [ ] Implement lazy handler initialization for large interfaces
- [ ] Implement lazy List allocation for call tracking
- [ ] Consider optional call tracking mode (`[KnockOff(TrackCalls = false)]`)
- [ ] Re-run benchmarks to verify fixes

---

## Issue 1: NA Results in InvocationBenchmarks

### Affected Benchmarks

| Benchmark | Method Called |
|-----------|---------------|
| `KnockOff_InvokeWithReturn` | `_knockOffCalculator.Add(1, 2)` |
| `KnockOff_InvokeWithArgs` | `_knockOffMedium.Method2(42)` |
| `KnockOff_InvokeWithStringArg` | `_knockOffMedium.Method3("test")` |
| `KnockOff_InvokeWithMultipleArgs` | `_knockOffMedium.Method4(42, "test")` |

### Root Cause

Every invocation calls `RecordCall()` which does `_calls.Add(...)`. Over millions of benchmark iterations:

```csharp
// From MediumServiceStub.g.cs
public void RecordCall(int param) => _calls.Add(param);
```

This causes:
1. **Unbounded List growth** - millions of items added during benchmark runs
2. **GC pressure** - constant allocations trigger garbage collection
3. **Memory explosion** - each iteration adds to the list, never cleared between runs

BenchmarkDotNet marks these as NA when it can't get stable measurements due to allocation in the hot path.

### Why `KnockOff_InvokeVoid` Works

`ISimpleService.DoWork()` is a zero-parameter void method - its handler only increments `CallCount` (an int), no List allocation:

```csharp
public void RecordCall() => CallCount++;  // No allocation
```

### Fix Options

1. **Use array with index** instead of List for call tracking (fixed capacity, no resize)
2. **Make call tracking opt-in** - don't record by default, only when needed
3. **Ring buffer** - fixed-size circular buffer that overwrites old entries
4. **Benchmark-specific:** Reset stubs between iterations

---

## Issue 2: Large Interface Memory Usage (130% of Moq)

### Benchmark Results

| Stub | KnockOff | Moq | Ratio |
|------|----------|-----|-------|
| Large (50 methods) | 2680 B | 2064 B | 130% |

### Memory Breakdown

The `ILargeService` has 50 methods. From `LargeServiceStub.g.cs`:

| Component | Count | Size Each | Total |
|-----------|-------|-----------|-------|
| Stub object | 1 | ~24 B | 24 B |
| ILargeServiceSpy | 1 | 8 B (ref) + 50x8 B (handler refs) | 408 B |
| Handler objects | 50 | ~32 B base | 1600 B |
| List<T> in handlers | 20 | ~56 B (empty List) | 1120 B |
| **Total** | | | **~3150 B** |

### Root Cause

- **Every handler is eagerly allocated** at construction
- **Every parameterized method allocates a List** upfront for call tracking

Moq uses lazy creation - only allocates when methods are actually invoked/verified.

### Fix Options

#### 1. Lazy Handler Initialization

```csharp
// Before (eager)
public ILargeService_VoidMethod01Handler VoidMethod01 { get; } = new();

// After (lazy)
private ILargeService_VoidMethod01Handler? _voidMethod01;
public ILargeService_VoidMethod01Handler VoidMethod01 =>
    _voidMethod01 ??= new();
```

#### 2. Lazy List Allocation

```csharp
// Before (eager)
private readonly List<int> _calls = new();
public void RecordCall(int p) => _calls.Add(p);
public int CallCount => _calls.Count;

// After (lazy)
private List<int>? _calls;
public void RecordCall(int p) => (_calls ??= new()).Add(p);
public int CallCount => _calls?.Count ?? 0;
```

#### 3. Optional Call Tracking

Add attribute option for pure stubs that don't need verification:

```csharp
[KnockOff(TrackCalls = false)]
public partial class FastStub : IService { }
```

---

## Performance Summary (Where KnockOff Excels)

Despite the issues above, KnockOff delivers excellent performance in most scenarios:

| Scenario | Moq | KnockOff | Speedup |
|----------|-----|----------|---------|
| Creation (simple) | 726 ns | 6.3 ns | **115x** |
| Creation (bulk 1000) | 760 us | 5.7 us | **132x** |
| Void invocation | 213 ns | 0.57 ns | **370x** |
| Verification | 423 ns | 0.014 ns | **29,365x** |
| Typical unit test | 71 us | 69 ns | **1,030x** |
| Test suite (50) | 7.4 ms | 3.1 us | **2,413x** |

The fixes above will improve the remaining edge cases without affecting these wins.

---

## Files to Modify

- `src/Generator/Emitters/MethodHandlerEmitter.cs` - Lazy List allocation
- `src/Generator/Emitters/SpyClassEmitter.cs` - Lazy handler initialization
- `src/KnockOff/KnockOffAttribute.cs` - Add TrackCalls property (optional)
