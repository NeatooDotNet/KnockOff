# Interceptor API Redesign

**Created:** 2026-01-16
**Status:** Design Complete

## Problem

The current interceptor API uses numeric suffixes for method overloads (`Method0`, `Method1`). This causes cascading compile errors when interfaces evolve:

```csharp
// Before: single method
void DoSomething();
// API: stub.DoSomething.CallCount ✓

// After: add overload
void DoSomething();
void DoSomething(int x);
// API becomes: stub.DoSomething0.CallCount, stub.DoSomething1.CallCount
// BREAKS all existing code
```

## Solution Summary

| Member Type | Design | Collision Handling |
|-------------|--------|-------------------|
| Methods | OnCall() overloads return IMethodTracking | Compiler resolves by signature |
| Properties | Simple assignment-based (unchanged) | N/A - no overloads |
| Indexers | OfXxx pattern (OfInt32, OfString) | Stable key-type naming |
| Events | Keep current design | N/A - no overloads |
| Generic Methods | .Of<T>() then OnCall() | Same as methods |

---

## Methods

### Core API

OnCall() returns IMethodTracking. Compiler resolves overloads by callback signature:

```csharp
// Register callback, get tracking back
IMethodTracking tracking = stub.Add.OnCall((a, b) => a + b);

tracking.CallCount;
tracking.LastArgs;
tracking.WasCalled;
```

### Overload Resolution

```csharp
void Process(int a, int b);
void Process(string s);

// Compiler picks based on callback signature
stub.Process.OnCall((int a, int b) => { });   // First overload
stub.Process.OnCall((string s) => { });       // Second overload
```

### Sequencing

Multiple OnCall() registrations create a sequence:

```csharp
var first = stub.Method.OnCall((a, b) => a + b);   // Call 1
var rest = stub.Method.OnCall((a, b) => a - b);    // Calls 2+
```

Last registration repeats forever unless Times specified.

### Times Support

```csharp
var first = stub.Method.OnCall((a, b) => a + b, Times.Two);  // Calls 1-2
var rest = stub.Method.OnCall((a, b) => a * b);              // Calls 3+
```

If last registration has Times and is exhausted, subsequent calls fail.

### CallerArgumentExpression

Same expression text returns same IMethodTracking:

```csharp
var t1 = stub.Method.OnCall((a, b) => a + b);
var t2 = stub.Method.OnCall((a, b) => a + b);  // Same text
t1 == t2;  // True
```

### Default Behavior (No OnCall)

- **Strict=false:** Return default value
- **Strict=true:** Throw exception

### Reset

```csharp
tracking.Reset();      // Clears this registration's tracking only
stub.Method.Reset();   // Clears all tracking, resets sequence position, keeps registrations
```

Full reset = create new stub instance.

---

## User-Defined Members

When user defines a method, `stub.Member` IS the tracking (no OnCall):

```csharp
[KnockOff]
public partial class CalculatorStub : ICalculator
{
    public int Add(int a, int b) => a + b;  // User-defined
}

// stub.Add IS IMethodTracking
stub.Add.CallCount;
stub.Add.LastArgs;
stub.Add.WasCalled;
// No OnCall() - implementation already defined
```

**Type change is deliberate** when adding/removing user methods. Compile errors force acknowledgment of semantic shift.

---

## Properties

Properties don't have overloads. Keep simple assignment-based design:

```csharp
stub.Name.Value = "test";              // Backing value
stub.Name.OnGet = (ko) => "computed";  // Callback (assignment)
stub.Name.OnSet = (ko, v) => { };      // Setter callback

// Tracking directly on interceptor
stub.Name.GetCount;
stub.Name.SetCount;
stub.Name.LastSetValue;
```

---

## Indexers

Use OfXxx pattern for stable naming (always, even for single indexer):

```csharp
// Int indexer
stub.Indexer.OfInt32.Backing[0] = "preset";
stub.Indexer.OfInt32.OnGet = (ko, i) => items[i];
stub.Indexer.OfInt32.OnSet = (ko, i, v) => { };
stub.Indexer.OfInt32.GetCount;
stub.Indexer.OfInt32.LastGetKey;

// Add string indexer later - OfInt32 unchanged!
stub.Indexer.OfString.Backing["key"] = "value";
stub.Indexer.OfString.OnGet = (ko, k) => dict[k];
```

Each OfXxx is property-like: backing dictionary, assignment callbacks, direct tracking.

---

## Events

Keep current design - no overloads, no callbacks needed:

```csharp
stub.DataReceived.AddCount;
stub.DataReceived.RemoveCount;
stub.DataReceived.HasSubscribers;
stub.DataReceived.Raise(sender, EventArgs.Empty);
stub.DataReceived.Reset();
```

---

## Generic Methods

Same pattern with .Of<T>() to select type first:

```csharp
var tracking = stub.Deserialize.Of<User>().OnCall((json) => new User());
tracking.CallCount;
tracking.LastArg;
```

**Dropped:** Aggregate tracking (TotalCallCount, CalledTypeArguments). Per-registration only.

---

## Multiple Interfaces (Stand-Alone Stubs)

**Same signature:** One interceptor handles both interfaces.

```csharp
interface IHasCount { int Count { get; } }
interface IAlsoHasCount { int Count { get; } }

[KnockOff]
class Stub : IHasCount, IAlsoHasCount { }
// One Count interceptor satisfies both
```

**Different signatures:** Diagnostic error.

```csharp
interface IFoo { string Name { get; } }
interface IBar { int Name { get; } }

[KnockOff]
class Stub : IFoo, IBar { }  // KO0XX: Conflicting signatures
```

---

## IMethodTracking Interface

```csharp
public interface IMethodTracking
{
    int CallCount { get; }
    bool WasCalled { get; }
    void Reset();
}

public interface IMethodTracking<TArg> : IMethodTracking
{
    TArg? LastArg { get; }
}

public interface IMethodTracking<TArgs> : IMethodTracking
{
    TArgs? LastArgs { get; }  // Named tuple for multiple params
}
```

---

## Times and Sequencing

### Two Modes Based on Times Parameter

**Without Times = single callback, repeats forever:**
```csharp
var tracking = stub.Method.OnCall((a, b) => a + b);
// Returns IMethodTracking
// No ThenCall available
// Repeats forever
```

**With Times = sequencing enabled:**
```csharp
var sequence = stub.Method
    .OnCall((a, b) => a + b, Times.Once)       // Call 1
    .ThenCall((a, b) => a - b, Times.Twice)    // Calls 2-3
    .ThenCall((a, b) => a * b, Times.Forever); // Calls 4+ forever

// Returns IMethodSequence
// ThenCall available (also requires Times)
```

### Exhausted Sequence = Throws

```csharp
var sequence = stub.Method
    .OnCall((a, b) => a + b, Times.Once)
    .ThenCall((a, b) => a - b, Times.Twice);
    // No Times.Forever at end

// Calls 1-3: work
// Call 4: throws (like Strict=true, no definition)
```

### Return Types

| Call | Returns | ThenCall? | Behavior |
|------|---------|-----------|----------|
| `OnCall(cb)` | `IMethodTracking` | No | Repeats forever |
| `OnCall(cb, Times)` | `IMethodSequence` | Yes | Sequencing + verification |

### Times Values

**For sequencing:**
- `Times.Once` - use once, then advance
- `Times.Twice` - use twice, then advance
- `Times.Exactly(n)` - use n times, then advance
- `Times.Forever` - repeat indefinitely (no advance)

**For verification:**
- `Times.AtLeast(n)` - at least n calls
- `Times.AtMost(n)` - at most n calls
- `Times.Never` - never called

### Verification

```csharp
// Per-tracking (no constraint if no Times)
tracking.Verify();

// Per-sequence (checks all Times constraints)
sequence.Verify();

// Whole stub (all methods)
stub.Verify();
```

---

## Dropped Features

- **CallerArgumentExpression** - unnecessary complexity, just hold onto the tracker
- **Aggregate tracking** - no TotalCallCount across registrations or type arguments

---

## Migration Impact

This is a **major breaking change**. All method interceptor usage changes:

| Old API | New API |
|---------|---------|
| `stub.Method.OnCall = (a, b) => ...` | `stub.Method.OnCall((a, b) => ...)` |
| `stub.Method.CallCount` | `tracking.CallCount` (from OnCall return) |
| `stub.Method0.CallCount` (overloads) | `tracking.CallCount` (compiler resolves) |
| `stub.Indexer.GetCount` | `stub.Indexer.OfInt32.GetCount` |

Properties and events unchanged.
