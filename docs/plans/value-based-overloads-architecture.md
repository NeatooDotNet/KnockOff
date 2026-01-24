# Value-Based Overloads Architecture

**Date:** 2026-01-24
**Related Todo:** [Value-Based Overloads for OnCall/OnGet/OnSet Methods](../todos/value-overloads-for-oncall.md)
**Status:** Draft (Architect)
**Last Updated:** 2026-01-24

---

## Overview

Design the architecture for adding value-based overloads to OnCall/OnGet/OnSet methods, enabling simpler syntax for returning fixed values in unit tests.

---

## Core Design Decision: Renderer-Only Approach

After analyzing the KnockOff pipeline architecture, the cleanest design is a **renderer-only approach**. The key insight is:

> The model describes *what* the interceptor needs (return type, parameters). The renderer decides *how* to implement it. Value-based overloads are an implementation detail - they don't change what the interceptor needs to do.

**Rationale:**
1. **Models describe capability, not implementation** - `UnifiedMethodInterceptorModel` already has `ReturnType`, `IsVoid`, and `OnCallDelegateType`. This is sufficient to generate value-based overloads.
2. **No new information needed** - A value overload for `T GetById(int id)` just needs the return type (`T`). The model already has this.
3. **Separation of concerns** - Adding "should I generate value overloads?" to models mixes rendering concerns into domain modeling.
4. **Equatability preserved** - No model changes means no equatability concerns.

---

## Architecture Approach

### Layer Responsibilities

| Layer | Current Responsibility | Value Overloads Impact |
|-------|----------------------|----------------------|
| **Model** | Describes interceptor shape (types, names, signatures) | NO CHANGE |
| **Builder** | Transforms Roslyn symbols to models | NO CHANGE |
| **Renderer** | Generates C# code from models | ADD value overloads here |

### Why This Works

The renderer already has all the information it needs:

```csharp
// From UnifiedMethodInterceptorModel:
model.ReturnType       // "User?" - the type to return
model.IsVoid           // false - so we can have value overload
model.OnCallDelegateType // Func<int, User?> - for callback version

// Renderer can derive:
// - Value overload signature: OnCall(User? returnValue)
// - Storage field: private User? _returnValue;
// - Invoke logic: if (_returnValue != null) return _returnValue;
```

---

## Detailed Design

### 1. Method Interceptor Value Overloads

#### API Surface

```csharp
// Current callback API (unchanged)
stub.GetById.OnCall((id) => expectedUser);
stub.GetById.OnCallSequence((id) => user1).ThenCall((id) => user2);

// New value API
stub.GetById.OnCall(expectedUser);
stub.GetById.OnCallSequence(user1).ThenReturn(user2);
```

#### Generated Code Pattern

```csharp
public sealed class GetByIdInterceptor
{
    // Existing callback storage (unchanged)
    private GetByIdDelegate? _onCall;
    private MethodTrackingImpl? _onCallTracking;

    // NEW: Value storage
    private bool _hasReturnValue;
    private User? _returnValue;
    private MethodTrackingImpl? _returnValueTracking;

    // Existing callback OnCall (unchanged)
    public IMethodTracking<int> OnCall(GetByIdDelegate callback)
    {
        ClearReturnValue();
        _onCall = callback;
        _onCallTracking = new MethodTrackingImpl(this);
        return _onCallTracking;
    }

    // NEW: Value OnCall overload
    public IMethodTracking<int> OnCall(User? returnValue)
    {
        ClearCallback();
        _hasReturnValue = true;
        _returnValue = returnValue;
        _returnValueTracking = new MethodTrackingImpl(this);
        return _returnValueTracking;
    }

    private void ClearCallback()
    {
        _onCall = null;
        _onCallTracking = null;
        _sequence = null;
        _sequenceIndex = 0;
    }

    private void ClearReturnValue()
    {
        _hasReturnValue = false;
        _returnValue = default;
        _returnValueTracking = null;
    }

    internal User? Invoke(int id)
    {
        // Check callback first (existing logic)
        if (_onCall != null && _onCallTracking != null)
        {
            _onCallTracking.RecordCall(id);
            return _onCall(id);
        }

        // Check return value (NEW)
        if (_hasReturnValue && _returnValueTracking != null)
        {
            _returnValueTracking.RecordCall(id);
            return _returnValue;
        }

        // ... existing unconfigured handling
    }
}
```

### 2. Why `_hasReturnValue` Boolean Flag?

The pattern uses a boolean flag rather than checking `_returnValue != null` because:

1. **Null is a valid return value** - `stub.GetById.OnCall(null)` should work
2. **Default values are valid** - `stub.GetCount.OnCall(0)` should work
3. **Consistent with callback pattern** - We check `_onCall != null` for callbacks

### 3. Property Interceptor Value Overloads

Properties already have `Value` which serves a similar purpose. The question is whether to add `OnGet(value)`:

#### Current API

```csharp
stub.Name.Value = "Alice";  // Static value
stub.Name.OnGet(() => "Alice");  // Callback (functionally equivalent)
```

#### Proposed API Addition

```csharp
stub.Name.OnGet("Alice");  // Value-based, cleaner for simple cases
stub.Name.OnGetSequence("first").ThenGet("second").ThenGet("third");
```

#### Generated Code Pattern

```csharp
public sealed class NameInterceptor
{
    // Existing
    private Func<string>? _onGet;
    private PropertyGetTrackingImpl? _onGetTracking;

    // NEW: Value-based getter storage
    private bool _hasGetValue;
    private string _getValue = default!;
    private PropertyGetTrackingImpl? _getValueTracking;

    // Existing callback OnGet
    public IPropertyGetTracking OnGet(Func<string> callback) { ... }

    // NEW: Value OnGet
    public IPropertyGetTracking OnGet(string value)
    {
        ClearGetCallback();
        _hasGetValue = true;
        _getValue = value;
        _getValueTracking = new PropertyGetTrackingImpl(this);
        return _getValueTracking;
    }

    internal string InvokeGet()
    {
        // Check callback
        if (_onGet != null && _onGetTracking != null)
        {
            _onGetTracking.RecordCall();
            return _onGet();
        }

        // Check return value
        if (_hasGetValue && _getValueTracking != null)
        {
            _getValueTracking.RecordCall();
            return _getValue;
        }

        // Fall back to Value property
        return _value;
    }
}
```

### 4. Sequence Value Overloads

#### Method Sequences

```csharp
// Current: all callbacks
stub.GetById.OnCallSequence((id) => user1).ThenCall((id) => user2);

// Proposed: mix and match
stub.GetById.OnCallSequence(user1).ThenReturn(user2);
stub.GetById.OnCallSequence((id) => user1).ThenReturn(user2);
stub.GetById.OnCallSequence(user1).ThenCall((id) => user2);
```

#### Implementation Strategy

The sequence stores a list of `(TCallback? Callback, TValue? Value, bool IsValue, Tracking)` tuples. At invoke time, it checks `IsValue` to decide how to produce the result.

Alternatively, a cleaner approach: always store a callback, and value overloads wrap the value in a lambda:

```csharp
public IMethodSequence<GetByIdDelegate> ThenReturn(User? value)
{
    return ThenCall((id) => value);  // Wrap value as callback
}
```

This approach:
- Reuses existing callback infrastructure
- No sequence storage changes needed
- Slight overhead (closure allocation) - acceptable for test code

### 5. Async Auto-Wrapping

For methods returning `Task<T>` or `ValueTask<T>`, value overloads should accept `T` and auto-wrap:

```csharp
// Interface: Task<User> GetByIdAsync(int id);

// Both should work:
stub.GetByIdAsync.OnCall(user);           // Accepts User, wraps in Task.FromResult
stub.GetByIdAsync.OnCall(Task.FromResult(user));  // Accepts Task<User> directly
```

#### Where to Handle This

**Option A: Renderer generates both overloads**

```csharp
// Generated for Task<User> return type
public IMethodTracking<int> OnCall(User returnValue)
{
    _hasReturnValue = true;
    _returnValue = Task.FromResult(returnValue);  // Renderer wraps it
    ...
}

public IMethodTracking<int> OnCall(Task<User> returnValue)
{
    _hasReturnValue = true;
    _returnValue = returnValue;
    ...
}
```

**Option B: Single overload, unwrap at invoke time**

Not possible - we need to know the type at configuration time.

**Recommendation: Option A** - Generate both overloads for async methods. The renderer already knows if `ReturnType` is `Task<T>` or `ValueTask<T>`.

---

## Architectural Verification

### Three Patterns Analysis

| Pattern | Impact | Notes |
|---------|--------|-------|
| **Standalone** | Full support | OnCall(value) works identically |
| **Inline Interface** | Full support | Same generated code |
| **Inline Class** | Full support | Same generated code |

All three patterns use the shared `MethodInterceptorRenderer` and `PropertyInterceptorRenderer`, so changes propagate automatically.

### Breaking Changes Assessment

**NO BREAKING CHANGES**

- All existing APIs remain unchanged
- Value overloads are additive
- Sequence changes (`ThenReturn`) are additive
- No interface changes to public tracking interfaces

### Pattern Consistency Check

| Existing Pattern | New Pattern | Consistent? |
|-----------------|-------------|-------------|
| `OnCall(callback)` | `OnCall(value)` | Yes - same method name, different signature |
| `ThenCall(callback)` | `ThenReturn(value)` | Yes - different name clarifies intent |
| `OnGet(callback)` | `OnGet(value)` | Yes - same pattern as methods |
| `Value = x` | `OnGet(x)` | Complementary - OnGet is dynamic, Value is static fallback |

### Diagnostic Requirements

No new diagnostics needed. Invalid usage (e.g., `OnCall(void)` for void methods) will be caught by C# compiler as no matching overload.

### Test Strategy

1. **Unit tests per interceptor type:**
   - Method interceptor value overloads
   - Property interceptor value overloads (OnGet)
   - Indexer interceptor value overloads (if applicable)

2. **Async tests:**
   - `Task<T>` return types with `T` values
   - `ValueTask<T>` return types with `T` values

3. **Sequence tests:**
   - `ThenReturn(value)` chaining
   - Mixed callback/value sequences

4. **Edge cases:**
   - Null values
   - Default values (0, false, etc.)
   - Reference equality for returned objects

### Edge Cases Documented

1. **Void methods** - No value overload (nothing to return)
2. **Out parameters** - Value overload only configures return value, out params still need initialization
3. **Ref parameters** - Same as out parameters
4. **Overloaded methods** - Each overload gets its own value storage (existing pattern)
5. **Generic methods** - Value overload per type argument (existing `Of<T>()` pattern)

---

## Codebase Analysis

### Files Examined

| File | Purpose | Modification Needed |
|------|---------|-------------------|
| `src/Generator/Model/Shared/UnifiedMethodInterceptorModel.cs` | Method interceptor model | NO |
| `src/Generator/Model/Shared/UnifiedPropertyInterceptorModel.cs` | Property interceptor model | NO |
| `src/Generator/Builder/UnifiedInterceptorBuilder.cs` | Builds models from Roslyn | NO |
| `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` | Renders method interceptors | YES - add value overloads |
| `src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs` | Renders property interceptors | YES - add value overloads |
| `src/KnockOff/IMethodSequence.cs` | Sequence interface | YES - add ThenReturn |
| `src/KnockOff/IPropertySequence.cs` | Property sequence interface | YES - add ThenGet(value) |

### Patterns Found

1. **Clear-on-configure pattern**: `OnCall(callback)` clears sequences, `OnCallSequence(callback)` clears OnCall. Value overloads should follow same pattern.

2. **Tracking separation**: Each configuration mode (callback, sequence, value) has its own tracking instance. This is correct for accurate call counting.

3. **Priority in Invoke**: Sequence > OnCall > Source > Strict > Default. Value overload should be checked same place as OnCall.

---

## Implementation Phases

### Phase 1: Foundation (Low Risk)

1. Add value storage fields to renderer output
2. Add `OnCall(value)` for non-void methods
3. Add `OnGet(value)` for property getters
4. Update `Invoke` methods to check value storage

### Phase 2: Sequences (Medium Risk)

1. Add `IMethodSequence.ThenReturn(TValue)` to interface
2. Implement `ThenReturn` as wrapper around `ThenCall`
3. Add `IPropertyGetSequence.ThenGet(TValue)` to interface
4. Implement `ThenGet(value)` for properties

### Phase 3: Async Auto-Wrapping (Higher Complexity)

1. Detect `Task<T>` and `ValueTask<T>` return types in renderer
2. Generate unwrapped value overloads for async methods
3. Generate both wrapped and unwrapped overloads

---

## Alternative Approaches Considered

### Alternative 1: Model-Based Value Source Abstraction

**Approach:** Add a `ValueSourceKind` enum to models (Callback, Value, Sequence) and let renderer branch on it.

**Rejected because:**
- Mixes rendering concerns into domain models
- Requires model changes and equatability updates
- More complex than necessary

### Alternative 2: Interface Changes with Value Members

**Approach:** Add `IMethodTracking.Value` property and `IMethodSequence.ThenValue()` to public interfaces.

**Rejected because:**
- Breaking changes to public interfaces
- Users would need to update to new package version
- Confusing API surface (both callback and value on same interface)

### Alternative 3: Returns() Method Instead of OnCall Overload

**Approach:** Add new method `Returns(value)` separate from `OnCall`.

**Rejected because:**
- Inconsistent with existing `OnGet`, `OnSet` pattern
- More methods to learn
- `OnCall(value)` is more intuitive - "on call, return this"

---

## Decision Summary

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Model changes? | NO | Models have sufficient information |
| Where to implement? | Renderer | Separation of concerns |
| Value storage mechanism | `_hasReturnValue` + `_returnValue` | Supports null values |
| Async handling | Renderer generates both overloads | Compile-time type safety |
| Sequence values | Wrap in callback | Reuses existing infrastructure |

---

## Developer Review

**Status:** Not Started

**Concerns:** [Developer will add concerns here]

---

## Implementation Contract

[Developer fills before starting implementation]

**In Scope:**
- [ ] TBD by developer

**Out of Scope:**
- [ ] TBD by developer

---

## Implementation Progress

[Developer fills during implementation]

---

## Completion Evidence

[Required before marking complete]

- **Tests Passing:** [Output or screenshot]
- **Generated Code Sample:** [Snippet showing feature works]
- **All Checklist Items:** [Confirmed 100% complete]
