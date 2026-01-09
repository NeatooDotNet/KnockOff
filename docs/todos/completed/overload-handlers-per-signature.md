# Plan: Separate Handlers Per Method Overload

## Problem

The current unified handler approach for method overloads has major issues:

### 1. Can't Tell Which Overload Was Called
```csharp
public interface IService
{
    void Process(string data);
    void Process(string data, int priority);
    void Process(string data, int priority, bool async);
}

// Current: All calls go to same handler
knockOff.IService.Process.CallCount;  // 3 - but which overloads?
knockOff.IService.Process.LastCallArgs;  // (string, int?, bool?) - was it 1, 2, or 3 params?
```

You have to inspect which tuple fields are null to guess which overload was called. Unreliable.

### 2. Type Conflicts with Same-Name Different-Type Parameters
```csharp
void Process(string input);
void Process(int input);  // BUG: Can't store both in List<string>
```

### 3. Complex Delegate Casting
```csharp
// Current: Must cast to specific delegate type
knockOff.IService.Process.OnCall((ServiceKnockOff.IServiceSpy.ProcessHandler.ProcessDelegate2)(
    (ko, data, priority, async) => { ... }));
```

Ugly and error-prone.

## Solution

**Treat each overload as a completely separate handler**, just like we did with multiple interfaces.

### New API Design

```csharp
public interface IService
{
    void Process(string data);                           // Overload 0
    void Process(string data, int priority);             // Overload 1
    void Process(string data, int priority, bool async); // Overload 2
}

// New: Each overload has its own handler
knockOff.IService.Process0.CallCount;      // Calls to Process(string)
knockOff.IService.Process1.CallCount;      // Calls to Process(string, int)
knockOff.IService.Process2.CallCount;      // Calls to Process(string, int, bool)

// Clear argument tracking - no nullable tuples
knockOff.IService.Process0.LastCallArg;    // string (single param)
knockOff.IService.Process1.LastCallArgs;   // (string data, int priority)
knockOff.IService.Process2.LastCallArgs;   // (string data, int priority, bool async)

// Simple callbacks - no delegate casting
knockOff.IService.Process0.OnCall((ko, data) => { ... });
knockOff.IService.Process1.OnCall((ko, data, priority) => { ... });
knockOff.IService.Process2.OnCall((ko, data, priority, async) => { ... });
```

### Benefits

1. **Clear Tracking**: Know exactly which overload was called
2. **Proper Types**: No nullable wrappers, no type conflicts
3. **Simple Callbacks**: No delegate casting needed
4. **Consistent Pattern**: Same as interface-scoped handlers

### Generated Code Structure

```csharp
// Handler for Process(string data)
public sealed class IService_Process0Handler
{
    public delegate void ProcessDelegate(ServiceKnockOff ko, string data);

    private ProcessDelegate? _onCall;
    private readonly List<string> _calls = new();

    public int CallCount => _calls.Count;
    public bool WasCalled => _calls.Count > 0;
    public string? LastCallArg => _calls.Count > 0 ? _calls[^1] : null;
    public IReadOnlyList<string> AllCalls => _calls;

    public void OnCall(ProcessDelegate callback) => _onCall = callback;
    internal ProcessDelegate? GetCallback() => _onCall;
    public void RecordCall(string data) => _calls.Add(data);
    public void Reset() { _calls.Clear(); _onCall = null; }
}

// Handler for Process(string data, int priority)
public sealed class IService_Process1Handler
{
    public delegate void ProcessDelegate(ServiceKnockOff ko, string data, int priority);

    private ProcessDelegate? _onCall;
    private readonly List<(string data, int priority)> _calls = new();

    public int CallCount => _calls.Count;
    public bool WasCalled => _calls.Count > 0;
    public (string data, int priority)? LastCallArgs => _calls.Count > 0 ? _calls[^1] : null;
    public IReadOnlyList<(string data, int priority)> AllCalls => _calls;

    public void OnCall(ProcessDelegate callback) => _onCall = callback;
    // ... etc
}

// Spy class
public sealed class IServiceSpy
{
    public IService_Process0Handler Process0 { get; } = new();
    public IService_Process1Handler Process1 { get; } = new();
    public IService_Process2Handler Process2 { get; } = new();
}
```

### Naming Options

**Option A: Numeric suffix** (simple, consistent)
```csharp
knockOff.IService.Process0
knockOff.IService.Process1
knockOff.IService.Process2
```

**Option B: Param-based suffix** (more descriptive)
```csharp
knockOff.IService.Process_String
knockOff.IService.Process_String_Int
knockOff.IService.Process_String_Int_Bool
```

**Option C: Nested property** (grouped)
```csharp
knockOff.IService.Process.Overload0
knockOff.IService.Process.Overload1
// But loses ability to have Process.CallCount for total
```

**Recommendation: Option A** - Simple, predictable, no naming conflicts with type names.

### Non-Overloaded Methods

Methods without overloads don't get a suffix:
```csharp
void Save(Entity e);  // Only one signature

knockOff.IService.Save.CallCount;  // No suffix needed
```

### Breaking Changes

This is a significant breaking change:

| Old API | New API |
|---------|---------|
| `Process.CallCount` | `Process0.CallCount` (if overloaded) |
| `Process.LastCallArgs?.priority` | `Process1.LastCallArgs.priority` |
| `Process.OnCall((Delegate1)(...))`  | `Process1.OnCall((ko, ...) => ...)` |
| `Process.AllCalls[0].data` | `Process0.AllCalls[0]` or `Process1.AllCalls[0].data` |

## Implementation Checklist

**Status: COMPLETE**

### Generator Changes
- [x] Detect when method has overloads vs single signature
- [x] Generate separate handler class per overload (with numeric suffix)
- [x] Remove unified handler logic (no more delegate types per overload)
- [x] Remove nullable tuple generation
- [x] Update spy class to have `Method0`, `Method1` properties for overloads
- [x] Keep `Method` (no suffix) for non-overloaded methods

### Handler Changes
- [x] Each handler is independent - no shared state
- [x] Simple delegate type (just one per handler)
- [x] Proper types (no nullable wrappers for required params)
- [x] Single `OnCall` method (no overloads)

### Implementation Generation
- [x] Each overload implementation calls its specific handler
- [x] `GetCallback()` returns single callback type

### Tests
- [x] Update OverloadedMethodTests.cs for new API
- [x] Add test: verify can distinguish which overload was called
- [x] Add test: same-name different-type params now works (bug is fixed by new design)
- [x] Remove delegate casting from all tests

### Documentation
- [x] Update getting-started.md
- [x] Add migration notes for overload handling
