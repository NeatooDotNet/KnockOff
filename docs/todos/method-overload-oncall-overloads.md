# Method Interceptor Redesign: OnCall() Returns Tracking Stub

**Status:** Design Complete
**Priority:** High
**Created:** 2026-01-16

---

## Problem

Current approach uses numeric suffixes for method overloads:

```csharp
// Before: single method
void DoSomething();
// API: stub.DoSomething.CallCount ✓

// After: add overload
void DoSomething();
void DoSomething(int x);
// API becomes: stub.DoSomething0.CallCount, stub.DoSomething1.CallCount
// BREAKS: all existing code using stub.DoSomething
```

This creates cascading compile errors whenever an interface adds an overload. Unacceptable for library evolution.

---

## Solution

**OnCall() returns an IMethodStub for tracking.** Overload resolution happens via compiler based on callback signature.

### Core API

```csharp
// OnCall returns a stub for tracking
IMethodStub send = stub.AddMethod.OnCall((a, b) => a + b);

send.CallCount;   // How many times this callback was invoked
send.LastArgs;    // Last arguments when this callback ran
```

### Overload Resolution via Compiler

```csharp
void Process(int a, int b);
void Process(string s);

// Compiler picks OnCall overload based on callback signature
stub.Process.OnCall((int a, int b) => { });   // First overload
stub.Process.OnCall((string s) => { });       // Second overload
```

No numeric suffixes. Interface can evolve without breaking existing code.

### Sequencing Support

Multiple OnCall() registrations create a sequence:

```csharp
IMethodStub first = stub.Method.OnCall((a, b) => a + b);   // Call 1
IMethodStub rest = stub.Method.OnCall((a, b) => a - b);    // Calls 2+
```

Last registration repeats forever (even with Strict=true).

### Times Support

Limit how many times a callback is used:

```csharp
IMethodStub first = stub.Method.OnCall((a, b) => a + b, Times.Two);  // Calls 1-2
IMethodStub rest = stub.Method.OnCall((a, b) => a * b);              // Calls 3+
```

If last registration has Times and is exhausted, subsequent calls fail:

```csharp
stub.Method.OnCall((a, b) => a + b, Times.Two);  // Calls 1-2
stub.Method.OnCall((a, b) => a - b, Times.Two);  // Calls 3-4, call 5+ fails
```

### CallerArgumentExpression for Same-Expression Detection

Same expression text returns same stub:

```csharp
var send1 = stub.Method.OnCall((a, b) => a + b);
var send2 = stub.Method.OnCall((a, b) => a + b);  // Same text
send1 == send2;  // True
```

Enables inline verification without holding reference:

```csharp
stub.Method.OnCall((a, b) => a + b);  // Setup

// ... test code ...

Assert.Equal(3, stub.Method.OnCall((a, b) => a + b).CallCount);  // Same stub
```

### Default Behavior (No OnCall)

- **Strict=false:** Return default value
- **Strict=true:** Throw exception

---

## Behavior Summary

| Scenario | Behavior |
|----------|----------|
| No OnCall, Strict=false | Return default |
| No OnCall, Strict=true | Throw |
| Single OnCall (no Times) | Repeat forever |
| Single OnCall with Times.N | Use N times, then fail |
| Multiple OnCall, last has no Times | Last repeats forever |
| Multiple OnCall, last has Times.N | Last uses N times, then fail |

---

## IMethodTracking Interface

```csharp
public interface IMethodTracking
{
    int CallCount { get; }
    bool WasCalled { get; }
    void Reset();  // Clears this registration's tracking only
}

public interface IMethodTracking<TArg> : IMethodTracking
{
    TArg? LastArg { get; }
}

public interface IMethodTracking<TArgs> : IMethodTracking  // For tuple args
{
    TArgs? LastArgs { get; }
}
```

## Reset Behavior

**Per-registration Reset:**
```csharp
var first = stub.Method.OnCall((a, b) => a + b);
var second = stub.Method.OnCall((a, b) => a - b);

first.Reset();   // Clears first.CallCount, first.LastArgs only
```

**Interceptor-level Reset:**
```csharp
stub.Method.Reset();
// - Clears tracking on ALL IMethodTracking instances
// - Resets sequence position back to first registration
// - Keeps registrations (callbacks still configured)
```

**Full reset:** Create a new stub instance.

---

## Tasks

- [ ] Design IMethodStub interface hierarchy
- [ ] Design Times enum/struct
- [ ] Update generator to emit OnCall() overloads (one per method signature)
- [ ] Implement sequencing logic
- [ ] Implement CallerArgumentExpression tracking
- [ ] Remove numeric suffix generation
- [ ] Update tests
- [ ] Update documentation

---

## Progress Log

### 2026-01-16
- Created todo
- Initial design: numeric suffixes
- Redesigned: OnCall() returns IMethodTracking, compiler resolves overloads
- Added sequencing support (like Moq SetupSequence)
- Added Times support
- Added CallerArgumentExpression for same-expression detection
- Confirmed: last registration repeats forever unless Times specified
- Renamed IMethodStub → IMethodTracking (clearer purpose)
- Decided: per-registration tracking only, no aggregate TotalCallCount
- Decided Reset behavior: per-registration clears own tracking; interceptor-level clears all + resets sequence position; full reset = new instance
- User-defined members: stub.Member IS IMethodTracking (direct access), no OnCall()
- Generated members: stub.Member only provides OnCall(), tracking via returned IMethodTracking
- Type change when adding/removing user methods is deliberate breaking change (good)
- Generic methods: same pattern with .Of<T>(), dropped aggregate tracking (TotalCallCount, CalledTypeArguments)
- Multiple interfaces: same signature = one interceptor; different signatures = diagnostic error
- Properties: keep simple assignment-based design, no OnGet() returning tracking
- Indexers: use `OfXxx` pattern (OfInt32, OfString) for stable naming, always use even for single indexer
- Events: keep current simple design, no changes needed
- Times: OnCall(cb) = IMethodTracking (repeats forever), OnCall(cb, Times) = IMethodSequence (enables ThenCall)
- ThenCall for fluent sequencing
- Times.Forever for indefinite repeat, exhausted sequence throws
- Dropped CallerArgumentExpression (just hold onto tracker)
- Verify() at tracking, sequence, and stub levels

---

## Times Design (Finalized)

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

**For verification (on Verify()):**
- `Times.AtLeast(n)` - at least n calls
- `Times.AtMost(n)` - at most n calls
- `Times.Never` - never called

### Verification

```csharp
// Per-tracking (single callback)
tracking.Verify();  // Just checks it was callable (no Times = no constraint)

// Per-sequence
sequence.Verify();  // Checks all Times constraints met

// Whole stub
stub.Verify();  // All methods meet expectations
```

### Dropped

- **CallerArgumentExpression** - unnecessary complexity, just hold onto the tracker

---

## Decisions Made

- **IMethodTracking** (not IMethodStub) - clearer naming
- **Per-registration tracking** - each `IMethodTracking` tracks its own calls
- **No aggregate tracking** - no `TotalCallCount` across registrations
- **Reset behavior:**
  - `tracking.Reset()` - clears that registration's tracking only
  - `stub.Method.Reset()` - clears all tracking, resets sequence position, keeps registrations
  - Full reset = new stub instance

### User-Defined vs Generated Members

The type of `stub.Member` differs based on whether user defined it:

**User-defined method exists:**
```csharp
public int Add(int a, int b) => a + b;

// stub.Add IS IMethodTracking (direct access)
stub.Add.CallCount;
stub.Add.LastArgs;
stub.Add.WasCalled;
// No OnCall() - implementation already defined
```

**No user-defined method:**
```csharp
// stub.Add only provides OnCall()
var tracking = stub.Add.OnCall((a, b) => a + b);
tracking.CallCount;
tracking.LastArgs;

// No direct stub.Add.CallCount
```

**Same pattern for properties:**
- User-defined property → Direct tracking (GetCount, SetCount, LastSetValue)
- Generated property → OnGet/OnSet/Value methods

**This is a deliberate breaking change** when adding/removing user methods. The compile error forces acknowledgment of the semantic shift (compile-time vs runtime implementation).

### Generic Methods

Same pattern with `.Of<T>()` to select type first:

```csharp
// Register and get tracking
var tracking = stub.Deserialize.Of<User>().OnCall((json) => new User());
tracking.CallCount;
tracking.LastArg;
```

**Overload resolution still via compiler:**
```csharp
T Deserialize<T>(string json);
T Deserialize<T>(string json, JsonOptions options);

stub.Deserialize.Of<User>().OnCall((json) => new User());         // First overload
stub.Deserialize.Of<User>().OnCall((json, opts) => new User());   // Second overload
```

**Dropped:** Aggregate tracking across type arguments (`TotalCallCount`, `CalledTypeArguments`). Per-registration tracking only.

### Multiple Interfaces (Stand-Alone Stubs)

**Same signature across interfaces:** One interceptor handles both. No special handling needed.

```csharp
interface IHasCount { int Count { get; } }
interface IAlsoHasCount { int Count { get; } }

[KnockOff]
class Stub : IHasCount, IAlsoHasCount { }

// One interceptor satisfies both
stub.Count.OnGet((ko) => 5);
```

**Different signatures (name collision):** Diagnostic error.

```csharp
interface IFoo { string Name { get; } }
interface IBar { int Name { get; } }  // Different return type

[KnockOff]
class Stub : IFoo, IBar { }  // ✗ KO0XX: Member 'Name' has conflicting signatures
```

**Resolution options:**
1. Use separate stubs (one per interface)
2. Provide user-defined explicit implementations for conflicting members

**Rationale:** A real class implementing `IList<Order>` and `IList<User>` doesn't make semantic sense. Keep generator simple, handle edge cases with diagnostics.

### Properties

Properties don't have overloads, so no collision problem. Keep simple assignment-based design:

```csharp
stub.Name.Value = "test";              // Backing value
stub.Name.OnGet = (ko) => "computed";  // Callback (assignment)
stub.Name.OnSet = (ko, v) => { };      // Setter callback

// Tracking directly on interceptor
stub.Name.GetCount;
stub.Name.SetCount;
stub.Name.LastSetValue;
```

No OnGet() method returning tracking. Properties stay simple.

### Indexers

Indexers CAN have overloads (different key types), but use `OfXxx` pattern for stable naming:

```csharp
// Always use OfXxx, even with single indexer
stub.Indexer.OfInt32.Backing[0] = "preset";
stub.Indexer.OfInt32.OnGet = (ko, i) => items[i];
stub.Indexer.OfInt32.OnSet = (ko, i, v) => { };
stub.Indexer.OfInt32.GetCount;
stub.Indexer.OfInt32.LastGetKey;

// Add string indexer later - OfInt32 unchanged!
stub.Indexer.OfString.Backing["key"] = "value";
stub.Indexer.OfString.OnGet = (ko, k) => dict[k];
```

**Key insight:** `OfXxx` name based on key type, not count. Adding indexers doesn't break existing code.

Each `OfXxx` is property-like:
- Backing dictionary
- OnGet/OnSet (assignment, not method)
- Direct tracking (GetCount, SetCount, LastGetKey, LastSetEntry)

### Events

Events are simple - no overloads, no callbacks needed. Keep current design:

```csharp
// Tracking
stub.DataReceived.AddCount;        // Times += called
stub.DataReceived.RemoveCount;     // Times -= called
stub.DataReceived.HasSubscribers;  // Any handlers attached?

// Raise
stub.DataReceived.Raise(sender, EventArgs.Empty);

// Reset clears counts AND removes handlers
stub.DataReceived.Reset();
```

**Multi-interface collision:** Same rules as properties - same delegate type = one interceptor; different delegate type = diagnostic error.

---

## Results / Conclusions

Design complete. Formal design document: `docs/plans/2026-01-16-interceptor-api-redesign.md`

**Key decisions:**
- Methods: OnCall() overloads return IMethodTracking, compiler resolves
- Properties: Keep simple assignment-based design
- Indexers: OfXxx pattern for stable naming
- Events: Keep current design
- User-defined members: Direct tracking, no callbacks
- Multiple interfaces: Diagnostic error for conflicting signatures

**This is a major breaking change** but eliminates cascading breaks from interface evolution.
