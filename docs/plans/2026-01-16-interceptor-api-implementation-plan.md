# Interceptor API Redesign Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Redesign the interceptor API to eliminate breaking changes when interfaces evolve by using OnCall() overloads with compiler-resolved signatures instead of numeric suffixes.

**Architecture:** Methods use OnCall()/ThenCall() returning IMethodTracking/IMethodSequence. Properties stay assignment-based. Indexers use OfXxx pattern (OfInt32, OfString). Events unchanged. Times controls sequencing and verification.

**Tech Stack:** C#, Roslyn Source Generator, xUnit

---

## Phase Overview

This is a major breaking change. Implementation is divided into 6 phases:

| Phase | Description | Can Ship Independently |
|-------|-------------|----------------------|
| 1 | Foundation Types (IMethodTracking, Times, etc.) | No |
| 2 | Method Interceptors (OnCall overloads) | No |
| 3 | Sequencing (ThenCall, IMethodSequence) | No |
| 4 | Indexers (OfXxx pattern) | No |
| 5 | Verification (Verify methods) | No |
| 6 | Cleanup & Migration | Yes (breaking release) |

**Estimated scope:** Large - multiple sessions recommended

---

## Phase 1: Foundation Types

Create the core interfaces and types in the KnockOff library.

### Task 1.1: Create IMethodTracking Interface

**Files:**
- Create: `src/KnockOff/IMethodTracking.cs`
- Test: `src/Tests/KnockOffTests/MethodTrackingTests.cs`

**Step 1: Write the interface**

```csharp
// src/KnockOff/IMethodTracking.cs
namespace KnockOff;

/// <summary>
/// Tracks invocations of a method callback registration.
/// </summary>
public interface IMethodTracking
{
    /// <summary>Number of times this callback was invoked.</summary>
    int CallCount { get; }

    /// <summary>True if CallCount > 0.</summary>
    bool WasCalled { get; }

    /// <summary>Clears tracking for this registration only.</summary>
    void Reset();
}

/// <summary>
/// Tracks invocations with single argument capture.
/// </summary>
public interface IMethodTracking<TArg> : IMethodTracking
{
    /// <summary>Last argument passed to this callback.</summary>
    TArg? LastArg { get; }
}

/// <summary>
/// Tracks invocations with multiple argument capture as named tuple.
/// </summary>
public interface IMethodTrackingArgs<TArgs> : IMethodTracking
{
    /// <summary>Last arguments passed to this callback as named tuple.</summary>
    TArgs? LastArgs { get; }
}
```

**Step 2: Commit**

```bash
git add src/KnockOff/IMethodTracking.cs
git commit -m "feat: add IMethodTracking interfaces for method callback tracking"
```

---

### Task 1.2: Create Times Struct

**Files:**
- Create: `src/KnockOff/Times.cs`
- Test: `src/Tests/KnockOffTests/TimesTests.cs`

**Step 1: Write the failing test**

```csharp
// src/Tests/KnockOffTests/TimesTests.cs
using KnockOff;
using Xunit;

namespace KnockOffTests;

public class TimesTests
{
    [Fact]
    public void Once_HasCountOfOne()
    {
        var times = Times.Once;
        Assert.Equal(1, times.Count);
        Assert.False(times.IsForever);
    }

    [Fact]
    public void Twice_HasCountOfTwo()
    {
        var times = Times.Twice;
        Assert.Equal(2, times.Count);
        Assert.False(times.IsForever);
    }

    [Fact]
    public void Exactly_HasSpecifiedCount()
    {
        var times = Times.Exactly(5);
        Assert.Equal(5, times.Count);
        Assert.False(times.IsForever);
    }

    [Fact]
    public void Forever_IsMarkedAsForever()
    {
        var times = Times.Forever;
        Assert.True(times.IsForever);
    }

    [Fact]
    public void AtLeast_ForVerification()
    {
        var times = Times.AtLeast(3);
        Assert.True(times.Verify(3));
        Assert.True(times.Verify(5));
        Assert.False(times.Verify(2));
    }

    [Fact]
    public void AtMost_ForVerification()
    {
        var times = Times.AtMost(3);
        Assert.True(times.Verify(0));
        Assert.True(times.Verify(3));
        Assert.False(times.Verify(4));
    }

    [Fact]
    public void Never_ForVerification()
    {
        var times = Times.Never;
        Assert.True(times.Verify(0));
        Assert.False(times.Verify(1));
    }
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test src/Tests/KnockOffTests --filter "FullyQualifiedName~TimesTests" -v n`
Expected: FAIL - Times type doesn't exist

**Step 3: Write the Times struct**

```csharp
// src/KnockOff/Times.cs
namespace KnockOff;

/// <summary>
/// Specifies how many times a callback should be used (sequencing)
/// or how many times it should have been called (verification).
/// </summary>
public readonly struct Times
{
    private readonly int _count;
    private readonly TimesKind _kind;

    private enum TimesKind
    {
        Exactly,
        Forever,
        AtLeast,
        AtMost,
        Never
    }

    private Times(int count, TimesKind kind)
    {
        _count = count;
        _kind = kind;
    }

    /// <summary>Use once, then advance to next callback.</summary>
    public static Times Once => new(1, TimesKind.Exactly);

    /// <summary>Use twice, then advance to next callback.</summary>
    public static Times Twice => new(2, TimesKind.Exactly);

    /// <summary>Use exactly n times, then advance to next callback.</summary>
    public static Times Exactly(int count) => new(count, TimesKind.Exactly);

    /// <summary>Repeat indefinitely (never advance).</summary>
    public static Times Forever => new(0, TimesKind.Forever);

    /// <summary>Verification: must be called at least n times.</summary>
    public static Times AtLeast(int count) => new(count, TimesKind.AtLeast);

    /// <summary>Verification: must be called at most n times.</summary>
    public static Times AtMost(int count) => new(count, TimesKind.AtMost);

    /// <summary>Verification: must never be called.</summary>
    public static Times Never => new(0, TimesKind.Never);

    /// <summary>The count for Exactly/Once/Twice.</summary>
    public int Count => _count;

    /// <summary>True if this represents Forever (indefinite repeat).</summary>
    public bool IsForever => _kind == TimesKind.Forever;

    /// <summary>True if this is for verification (AtLeast/AtMost/Never).</summary>
    public bool IsVerification => _kind is TimesKind.AtLeast or TimesKind.AtMost or TimesKind.Never;

    /// <summary>Verify if actual call count satisfies this constraint.</summary>
    public bool Verify(int actualCount) => _kind switch
    {
        TimesKind.Exactly => actualCount == _count,
        TimesKind.Forever => true,
        TimesKind.AtLeast => actualCount >= _count,
        TimesKind.AtMost => actualCount <= _count,
        TimesKind.Never => actualCount == 0,
        _ => false
    };
}
```

**Step 4: Run test to verify it passes**

Run: `dotnet test src/Tests/KnockOffTests --filter "FullyQualifiedName~TimesTests" -v n`
Expected: PASS

**Step 5: Commit**

```bash
git add src/KnockOff/Times.cs src/Tests/KnockOffTests/TimesTests.cs
git commit -m "feat: add Times struct for sequencing and verification"
```

---

### Task 1.3: Create IMethodSequence Interface

**Files:**
- Create: `src/KnockOff/IMethodSequence.cs`

**Step 1: Write the interface**

```csharp
// src/KnockOff/IMethodSequence.cs
namespace KnockOff;

/// <summary>
/// Represents a sequence of method callbacks with Times constraints.
/// Returned by OnCall(callback, Times) to enable ThenCall chaining.
/// </summary>
public interface IMethodSequence
{
    /// <summary>Total calls across all callbacks in sequence.</summary>
    int TotalCallCount { get; }

    /// <summary>Verify all Times constraints in the sequence were satisfied.</summary>
    bool Verify();

    /// <summary>Reset all tracking in the sequence.</summary>
    void Reset();
}

/// <summary>
/// Typed sequence that enables ThenCall chaining for specific signatures.
/// </summary>
public interface IMethodSequence<TCallback> : IMethodSequence
{
    /// <summary>Add another callback to the sequence.</summary>
    IMethodSequence<TCallback> ThenCall(TCallback callback, Times times);
}
```

**Step 2: Commit**

```bash
git add src/KnockOff/IMethodSequence.cs
git commit -m "feat: add IMethodSequence interface for callback sequencing"
```

---

### Task 1.4: Create VerificationException

**Files:**
- Create: `src/KnockOff/VerificationException.cs`

**Step 1: Write the exception**

```csharp
// src/KnockOff/VerificationException.cs
namespace KnockOff;

/// <summary>
/// Thrown when verification fails (Times constraints not satisfied).
/// </summary>
public class VerificationException : Exception
{
    public VerificationException(string message) : base(message) { }

    public VerificationException(string member, Times expected, int actual)
        : base($"Verification failed for '{member}': expected {FormatTimes(expected)}, actual {actual} calls")
    {
        Member = member;
        Expected = expected;
        Actual = actual;
    }

    public string? Member { get; }
    public Times? Expected { get; }
    public int? Actual { get; }

    private static string FormatTimes(Times times)
    {
        if (times.IsForever) return "any number of calls";
        return $"{times.Count} calls";
    }
}
```

**Step 2: Commit**

```bash
git add src/KnockOff/VerificationException.cs
git commit -m "feat: add VerificationException for verification failures"
```

---

## Phase 2: Method Interceptor Generation

Update the generator to emit OnCall() methods instead of OnCall property assignment.

### Task 2.1: Create Method Interceptor Template

**Files:**
- Modify: `src/Generator/Renderer/FlatRenderer.cs`
- Modify: `src/Generator/Model/Flat/FlatMethodModel.cs`

**Step 1: Understand current generation**

Read the current `RenderMethodInterceptorClass` method in `FlatRenderer.cs` to understand the current pattern.

**Step 2: Design new interceptor class**

The new interceptor class for a method `int Add(int a, int b)` should look like:

```csharp
public sealed class AddInterceptor
{
    private readonly List<(Func<StubClass, int, int, int> Callback, Times Times, MethodTrackingImpl Tracking)> _sequence = new();
    private int _sequenceIndex;
    private readonly bool _strict;

    public AddInterceptor(bool strict) => _strict = strict;

    // OnCall without Times - returns IMethodTracking, repeats forever
    public IMethodTrackingArgs<(int a, int b)> OnCall(Func<StubClass, int, int, int> callback)
    {
        var tracking = new MethodTrackingImpl();
        _sequence.Clear();
        _sequence.Add((callback, Times.Forever, tracking));
        _sequenceIndex = 0;
        return tracking;
    }

    // OnCall with Times - returns IMethodSequence, enables ThenCall
    public IMethodSequence<Func<StubClass, int, int, int>> OnCall(Func<StubClass, int, int, int> callback, Times times)
    {
        var tracking = new MethodTrackingImpl();
        _sequence.Clear();
        _sequence.Add((callback, times, tracking));
        _sequenceIndex = 0;
        return new MethodSequenceImpl(this, tracking);
    }

    // Internal: called by generated explicit interface implementation
    internal int Invoke(StubClass ko, int a, int b)
    {
        if (_sequence.Count == 0)
        {
            if (_strict) throw new StubException("No callback configured for Add");
            return default;
        }

        var (callback, times, tracking) = _sequence[_sequenceIndex];
        tracking.RecordCall((a, b));

        // Advance sequence if times exhausted (and not Forever)
        if (!times.IsForever && tracking.CallCount >= times.Count)
        {
            if (_sequenceIndex < _sequence.Count - 1)
                _sequenceIndex++;
            else if (tracking.CallCount > times.Count)
                throw new StubException("Sequence exhausted for Add");
        }

        return callback(ko, a, b);
    }

    public void Reset()
    {
        foreach (var (_, _, tracking) in _sequence)
            tracking.Reset();
        _sequenceIndex = 0;
    }

    // Nested tracking implementation
    private sealed class MethodTrackingImpl : IMethodTrackingArgs<(int a, int b)>
    {
        private (int a, int b)? _lastArgs;
        public int CallCount { get; private set; }
        public bool WasCalled => CallCount > 0;
        public (int a, int b)? LastArgs => _lastArgs;

        public void RecordCall((int a, int b) args)
        {
            CallCount++;
            _lastArgs = args;
        }

        public void Reset()
        {
            CallCount = 0;
            _lastArgs = default;
        }
    }

    // Nested sequence implementation
    private sealed class MethodSequenceImpl : IMethodSequence<Func<StubClass, int, int, int>>
    {
        private readonly AddInterceptor _interceptor;

        public MethodSequenceImpl(AddInterceptor interceptor, MethodTrackingImpl firstTracking)
        {
            _interceptor = interceptor;
        }

        public int TotalCallCount => _interceptor._sequence.Sum(s => s.Tracking.CallCount);

        public IMethodSequence<Func<StubClass, int, int, int>> ThenCall(
            Func<StubClass, int, int, int> callback, Times times)
        {
            var tracking = new MethodTrackingImpl();
            _interceptor._sequence.Add((callback, times, tracking));
            return this;
        }

        public bool Verify()
        {
            foreach (var (_, times, tracking) in _interceptor._sequence)
            {
                if (!times.Verify(tracking.CallCount))
                    return false;
            }
            return true;
        }

        public void Reset() => _interceptor.Reset();
    }
}
```

**Step 3: Update FlatMethodModel to support new pattern**

Add properties needed for new generation pattern.

**Step 4: Update RenderMethodInterceptorClass**

Modify to generate the new interceptor pattern.

**Note:** This task is complex. Break into sub-steps during execution:
1. First get OnCall(callback) working (no Times)
2. Then add OnCall(callback, Times)
3. Then add ThenCall

---

### Task 2.2: Update Explicit Interface Implementation

**Files:**
- Modify: `src/Generator/Renderer/FlatRenderer.cs`

The generated explicit interface implementation should call `interceptor.Invoke()`:

```csharp
int ICalculator.Add(int a, int b) => Add.Invoke(this, a, b);
```

---

### Task 2.3: Handle User-Defined Methods

**Files:**
- Modify: `src/Generator/Builder/FlatModelBuilder.cs`

When user defines a method, generate tracking-only interceptor (no OnCall):

```csharp
public sealed class AddInterceptor : IMethodTrackingArgs<(int a, int b)>
{
    private (int a, int b)? _lastArgs;
    public int CallCount { get; private set; }
    public bool WasCalled => CallCount > 0;
    public (int a, int b)? LastArgs => _lastArgs;

    internal void RecordCall((int a, int b) args)
    {
        CallCount++;
        _lastArgs = args;
    }

    public void Reset()
    {
        CallCount = 0;
        _lastArgs = default;
    }
}
```

The user's method is used directly, interceptor just tracks.

---

## Phase 3: Sequencing (ThenCall)

Already designed in Phase 2. Verify it works with tests.

### Task 3.1: Write Sequencing Tests

**Files:**
- Create: `src/Tests/KnockOffTests/SequencingTests.cs`

**Step 1: Write tests**

```csharp
// src/Tests/KnockOffTests/SequencingTests.cs
public class SequencingTests
{
    [Fact]
    public void OnCall_WithoutTimes_RepeatsForever()
    {
        var stub = new CalculatorStub();
        var tracking = stub.Add.OnCall((ko, a, b) => a + b);

        ICalculator calc = stub;
        Assert.Equal(3, calc.Add(1, 2));
        Assert.Equal(3, calc.Add(1, 2));
        Assert.Equal(3, calc.Add(1, 2));

        Assert.Equal(3, tracking.CallCount);
    }

    [Fact]
    public void OnCall_WithTimes_AdvancesToNext()
    {
        var stub = new CalculatorStub();
        stub.Add
            .OnCall((ko, a, b) => 100, Times.Once)
            .ThenCall((ko, a, b) => 200, Times.Forever);

        ICalculator calc = stub;
        Assert.Equal(100, calc.Add(1, 2));  // First call
        Assert.Equal(200, calc.Add(1, 2));  // Second call
        Assert.Equal(200, calc.Add(1, 2));  // Third call (repeats)
    }

    [Fact]
    public void ExhaustedSequence_Throws()
    {
        var stub = new CalculatorStub();
        stub.Add
            .OnCall((ko, a, b) => 100, Times.Once)
            .ThenCall((ko, a, b) => 200, Times.Once);

        ICalculator calc = stub;
        calc.Add(1, 2);  // First
        calc.Add(1, 2);  // Second

        Assert.Throws<StubException>(() => calc.Add(1, 2));  // Third - exhausted
    }
}
```

---

## Phase 4: Indexers (OfXxx Pattern)

### Task 4.1: Update Indexer Interceptor Generation

**Files:**
- Modify: `src/Generator/Renderer/FlatRenderer.cs`
- Modify: `src/Generator/Model/Flat/FlatIndexerModel.cs`

Change from:
```csharp
stub.Indexer.GetCount           // Single indexer
stub.IndexerInt32.GetCount      // Multiple indexers
```

To:
```csharp
stub.Indexer.OfInt32.GetCount   // Always use OfXxx
stub.Indexer.OfString.GetCount
```

**Step 1: Update FlatIndexerModel**

Add `KeyTypeName` property for OfXxx naming.

**Step 2: Generate container class**

```csharp
public sealed class IndexerContainer
{
    public Int32IndexerInterceptor OfInt32 { get; } = new();
    public StringIndexerInterceptor OfString { get; } = new();
}

// Property on stub
public IndexerContainer Indexer { get; } = new();
```

**Step 3: Each OfXxx interceptor stays property-like**

```csharp
public sealed class Int32IndexerInterceptor
{
    public Dictionary<int, string> Backing { get; } = new();
    public Func<StubClass, int, string>? OnGet { get; set; }
    public Action<StubClass, int, string>? OnSet { get; set; }
    public int GetCount { get; private set; }
    public int SetCount { get; private set; }
    public int? LastGetKey { get; private set; }
    public (int Key, string Value)? LastSetEntry { get; private set; }
    // ...
}
```

---

## Phase 5: Verification

### Task 5.1: Add Verify to IMethodTracking

Already in interface. Implement in generated code.

### Task 5.2: Add stub.Verify()

**Files:**
- Modify generator to add `Verify()` method to stub class

```csharp
public bool Verify()
{
    // Check all sequences
    // Return false if any fail
    return true;
}

public void VerifyAll()
{
    if (!Verify())
        throw new VerificationException("One or more verifications failed");
}
```

---

## Phase 6: Cleanup & Migration

### Task 6.1: Remove Numeric Suffixes

**Files:**
- Modify: `src/Generator/Builder/FlatModelBuilder.cs`

Remove code that adds `0`, `1`, `2` suffixes to overloaded methods.

### Task 6.2: Add Diagnostic for Conflicting Signatures

**Files:**
- Modify: `src/Generator/KnockOffGenerator.cs`

When multiple interfaces have same-name members with different signatures:

```csharp
// Emit diagnostic
context.ReportDiagnostic(Diagnostic.Create(
    new DiagnosticDescriptor(
        "KO0100",
        "Conflicting member signatures",
        "Member '{0}' has conflicting signatures across interfaces {1}",
        "KnockOff",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true),
    location,
    memberName,
    string.Join(", ", conflictingInterfaces)));
```

### Task 6.3: Update All Tests

Mass update of existing tests to new API.

### Task 6.4: Update Documentation

- Update `docs/guides/methods.md`
- Update `docs/guides/properties.md`
- Update `docs/reference/interceptor-api.md`
- Update `docs/getting-started.md`
- Update `docs/migration-from-moq.md`

---

## Testing Strategy

Each phase should have tests before moving on:

1. **Phase 1:** Unit tests for Times, interfaces compile
2. **Phase 2:** Integration tests for OnCall returning tracking
3. **Phase 3:** Sequencing tests with ThenCall
4. **Phase 4:** Indexer OfXxx tests
5. **Phase 5:** Verification tests
6. **Phase 6:** Full regression suite passes

---

## Migration Notes

This is a **major breaking change**. Document migration path:

| Old API | New API |
|---------|---------|
| `stub.Method.OnCall = (a, b) => ...` | `stub.Method.OnCall((a, b) => ...)` |
| `stub.Method.CallCount` | `tracking.CallCount` |
| `stub.Method0.CallCount` | `tracking.CallCount` (compiler resolves) |
| `stub.Indexer.GetCount` | `stub.Indexer.OfInt32.GetCount` |

Properties and events unchanged.
