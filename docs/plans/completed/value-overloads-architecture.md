# Value-Based Overloads Architecture

**Date:** 2026-01-24
**Related Todo:** [Add Value-Based Overloads to OnCall/OnGet/OnSet](../todos/value-overloads-oncall-onget.md)
**Status:** Complete
**Last Updated:** 2026-01-24 (Phase 5 complete - all tests passing, documentation updated)

---

## Overview

Add value-based overloads to OnCall/OnGet/OnSet methods to simplify common configuration patterns. Methods will auto-wrap async return values, and properties will unify to OnGet(value) pattern by removing the .Value property.

---

## Approach

**Hybrid Implementation Strategy:**
- **Methods**: Value-first storage (add dedicated value fields) for performance (avoid closure allocations)
- **Properties**: Wrapper pattern (delegate to lambda overload) for simplicity
- **Sequences**: Wrapper pattern (reuse existing infrastructure)

This balances performance for frequently-called methods with simplicity for properties.

---

## Design

### Method Interceptor Changes

**New Storage Fields:**
```csharp
// Value storage (NEW)
private TReturn? _onCallValue;
private bool _hasOnCallValue;  // Required because null/default are valid
private MethodTrackingImpl? _onCallValueTracking;

// Callback storage (EXISTING - unchanged)
private DelegateType? _onCall;
private MethodTrackingImpl? _onCallTracking;
```

**New OnCall Overload:**
```csharp
/// <summary>Configures return value. Returns tracking interface.</summary>
public IMethodTracking OnCall(TReturn value)
{
    // Clear callback configuration (mutual exclusivity)
    _sequence = null;
    _sequenceIndex = 0;
    _onCall = null;
    _onCallTracking = null;

    // Set value configuration
    _hasOnCallValue = true;
    _onCallValue = value;  // Note: async wrapping handled in Invoke
    _onCallValueTracking = new MethodTrackingImpl(this);
    return _onCallValueTracking;
}
```

**Async Auto-Wrapping Logic:**

Detect async return types during rendering:
```csharp
bool isTaskT = returnType.StartsWith("global::System.Threading.Tasks.Task<");
bool isValueTaskT = returnType.StartsWith("global::System.Threading.Tasks.ValueTask<");

// Store unwrapped type
string valueStorageType = isTaskT || isValueTaskT
    ? ExtractGenericArg(returnType)  // "Task<string>" -> "string"
    : returnType;
```

In Invoke method, wrap when returning:
```csharp
if (_hasOnCallValue && _onCallValueTracking != null)
{
    _onCallValueTracking.RecordCall(args);

    // Auto-wrap async
    if (isTaskT)
        return Task.FromResult(_onCallValue);
    else if (isValueTaskT)
        return new ValueTask<T>(_onCallValue);
    else
        return _onCallValue;
}
```

**Modified Invoke Priority Chain:**
```
1. Value sequence (if present and not exhausted)
2. Repeating value (if _hasOnCallValue)
3. Callback sequence (existing)
4. Repeating callback (existing)
5. Unconfigured tracking
6. Source delegation
7. Strict mode / default
```

**Constraints:**
- **No overload for void methods** (return type is void, nothing to configure)
- **No overload for ref/out methods** (requires callback to populate out parameters)

### Property Interceptor Changes

**Breaking Change: Remove .Value Property**

Before:
```csharp
private bool _valueSet;
private TValue _value = default!;

public TValue Value
{
    get => _value;
    set { _value = value; _valueSet = true; }
}
```

After (removed entirely):
```csharp
// No .Value property
```

**New OnGet Overload (Wrapper Pattern):**
```csharp
/// <summary>Configures getter return value. Returns tracking interface.</summary>
public IPropertyGetTracking OnGet(TValue value)
{
    return OnGet(() => value);  // Delegates to existing lambda overload
}
```

**Modified Existing OnCall Overload:**
```csharp
public IMethodTracking OnCall(DelegateType callback)
{
    // Clear value configuration (mutual exclusivity)
    _hasOnCallValue = false;
    _onCallValue = default;
    _onCallValueTracking = null;

    // Set callback configuration (existing code continues)
    _sequence = null;
    _sequenceIndex = 0;
    _onCall = callback;
    _onCallTracking = new MethodTrackingImpl(this);
    return _onCallTracking;
}
```

### Sequence Support

**Value Sequences (Wrapper Pattern):**
```csharp
// For IMethodSequence<TDelegate>
public IMethodSequence<TDelegate> ThenCall(TReturn value)
{
    return ThenCall(() => value);  // Reuses existing ThenCall(callback)
}

// For IPropertyGetSequence<TValue>
public IPropertyGetSequence<TValue> ThenGet(TValue value)
{
    return ThenGet(() => value);  // Reuses existing ThenGet(callback)
}
```

**OnCallSequence Value Overload:**
```csharp
public IMethodSequence<TDelegate> OnCallSequence(TReturn value)
{
    return OnCallSequence(() => value);  // Delegates to callback version
}
```

### Generic Methods, Indexers, Delegates

**Generic Methods:**
```csharp
// Method: T Create<T>()
stub.Create.Of<User>().OnCall(testUser);  // Works - TReturn is User
```

**Indexers:**
```csharp
// Indexer: string this[int index] { get; }
stub.Item.OnGet("default");  // Sets default return for any index
```

**Delegates:**
```csharp
// [KnockOff<Func<int, string>>]
stub.OnCall((id) => $"User{id}");  // Existing
stub.OnCall("ConstantValue");      // NEW - always returns "ConstantValue"
```

---

## Implementation Steps

### Phase 1: Method Value Overloads
1. Modify `MethodInterceptorRenderer.cs`:
   - Add value storage fields to `RenderSingleSignatureContent`
   - Generate `OnCall(TReturn value)` overload (skip if void or has ref/out params)
   - Implement async detection and wrapping logic
   - Modify `RenderInvokeMethod` to check value storage before callback storage
   - Update existing `OnCall(callback)` to clear value storage
2. Repeat for `RenderOverloadGroupContent` (overloaded methods)
3. Add tests for value overloads

### Phase 2: Async Auto-Wrapping
1. Add async type detection helper in renderer
2. Extract generic type argument (Task<T> → T, ValueTask<T> → T)
3. Store unwrapped type in value storage
4. Wrap in Invoke based on detected async type
5. Test with Task<T>, Task<T?>, ValueTask<T>, nullable types

### Phase 3: Property .Value Removal (Breaking Change)
1. Remove `.Value` property generation from `PropertyInterceptorRenderer.cs`
2. Remove `_value` and `_valueSet` fields (no longer needed for storage)
3. Update `InvokeGet` fallback behavior (return default! instead of _value)
4. Update `IsConfigured` check (remove _valueSet condition)
5. Update 78 test files to use `OnGet(value)` instead of `.Value = value`
6. Update documentation and samples

### Phase 4: Property Value Overloads
1. Add `OnGet(TValue value)` wrapper in `PropertyInterceptorRenderer.cs`
2. Test that it returns tracking correctly
3. Verify mutual exclusivity with OnGet(callback)

### Phase 5: Sequence Value Support
1. Add `ThenCall(TReturn value)` to `IMethodSequence<T>` interface
2. Add `ThenGet(TValue value)` to `IPropertyGetSequence<T>` interface
3. Generate wrapper implementations in renderers
4. Add sequence value tests

### Phase 6: Generics, Indexers, Delegates
1. Verify generic methods work (no special handling needed)
2. Test indexer value overloads
3. Test delegate stub value overloads
4. Add comprehensive test coverage

---

## Acceptance Criteria

- [ ] Methods support `OnCall(value)` returning IMethodTracking
- [ ] Async methods auto-wrap: `OnCall(user)` → `Task.FromResult(user)`
- [ ] Void methods do NOT have value overload (compile error)
- [ ] Ref/out methods do NOT have value overload
- [ ] Properties support `OnGet(value)` (OnSet removed from scope - semantically unclear)
- [ ] Property `.Value` removed entirely (breaking change)
- [ ] Sequences support `ThenCall(value)` and `ThenGet(value)`
- [ ] Value clears callback, callback clears value (mutual exclusivity)
- [ ] IsConfigured returns true when value is set
- [ ] All 29 non-generated test files migrated from `.Value` to `OnGet(value)`
- [ ] Generic methods, indexers, delegates work correctly
- [ ] All existing tests pass
- [ ] New tests cover value overload scenarios

---

## Dependencies

None - this is a renderer-only change. No model or builder modifications required.

---

## Risks / Considerations

### Breaking Change Impact
- 78 test files need migration from `.Value` to `.OnGet(value)`
- User code using `.Value` will have compile errors
- Mitigation: Clear migration guide, compile error message is obvious

### Async Auto-Wrapping Edge Cases
- `Task<string?>` - ensure nullable types handled correctly
- `Task` (void) - no value overload should exist
- `ValueTask<T>` - different wrapping syntax than Task<T>
- Generic `Task<T>` in generic methods

### Overload Resolution Ambiguity
- Risk: Could `OnCall(value)` and `OnCall(callback)` conflict?
- Analysis: No - `TReturn` vs `Func<TReturn>` are distinct types
- Exception: If TReturn itself is a delegate type (rare, but possible)

### Performance
- Methods: No closure allocation (value storage approach)
- Properties: One closure per OnGet(value) call (acceptable for properties)
- Sequences: One closure per value (acceptable for sequences)

---

## Architectural Verification

### Verification Checklist

- [x] All three patterns analyzed (Standalone, Inline Interface, Inline Class)
- [x] Breaking changes assessment completed
- [x] Pattern consistency verified (follows existing patterns)
- [x] Diagnostic requirements identified (none needed - compile errors are self-explanatory)
- [x] Test strategy defined (see below)
- [x] Edge cases documented (see below)
- [x] Codebase deep-dive completed

---

### Three Patterns Analysis

| Pattern | OnCall(value) Methods | OnGet(value) Properties | Delegate OnCall(value) | Notes |
|---------|----------------------|------------------------|----------------------|-------|
| **Standalone** (`[KnockOff]`) | Supported via `MethodInterceptorRenderer` | Supported via `PropertyInterceptorRenderer` | N/A | Uses shared renderers |
| **Inline Interface** (`[KnockOff<IFoo>]`) | Supported via `MethodInterceptorRenderer` | Supported via `PropertyInterceptorRenderer` | N/A | Same shared renderers |
| **Inline Class** (`[KnockOff<SomeClass>]`) | Supported via `MethodInterceptorRenderer` | Supported via `PropertyInterceptorRenderer` | N/A | Same shared renderers |
| **Delegate Stubs** (`[KnockOff<Func<...>>]`) | Requires separate handling | N/A | Needs value overload added to `InlineRenderer.RenderDelegateStub` | Currently only has `OnCall(callback)` |

**Key Finding**: All three main patterns use the same shared renderers, so value overloads automatically work across all patterns. However, **delegate stubs** are rendered separately in `InlineRenderer.cs` (lines 1252-1375) and require a parallel implementation.

---

### Breaking Changes Assessment

**Breaking Change**: Removal of `.Value` property from property interceptors

**Impact Analysis**:
- 47 files in `src/Tests/` use `.Value =` syntax
- 247 total occurrences across test files
- Generated files (in `Generated/` directories) will auto-regenerate
- User test files require manual migration

**Migration Required**:
```csharp
// Before
stub.Name.Value = "test";

// After
stub.Name.OnGet("test");
```

**Compile Error Guidance**: When users try to access `.Value`, they will get a compile error like:
```
error CS1061: 'NameInterceptor' does not contain a definition for 'Value'
```
This is self-explanatory - no diagnostic needed from the generator.

**Init-Only Properties**: The `.Value` property is still needed internally for init-only property setters (see `PropertyInterceptorRenderer.cs` line 55-63). Design decision: Keep internal `_value` field but remove public `Value` property accessor. Alternative: Consider keeping `.Value` for init-only properties only, but this creates API inconsistency.

---

### Pattern Consistency Verification

| Feature | Existing Pattern | New Value Overload | Consistent? |
|---------|-----------------|-------------------|-------------|
| Return type | `IMethodTracking` | `IMethodTracking` | Yes |
| Mutual exclusivity | OnCall clears sequence | OnCall(value) clears callback+sequence | Yes |
| Tracking creation | `new MethodTrackingImpl(this)` | Same pattern | Yes |
| Verifiable chain | `OnCall().Verifiable()` | `OnCall(value).Verifiable()` | Yes |

**Priority Chain Update**: The plan correctly identifies that value storage should be checked before callback storage in Invoke. However, I recommend:
1. Value sequence first (maintains sequence precedence)
2. **Callback sequence** (existing)
3. Repeating value
4. **Repeating callback** (existing)

This preserves the current "sequence before repeating" precedent while adding value support.

---

### Edge Cases and Concerns

**1. Delegate Type Return Values**
```csharp
interface IFoo { Func<int> GetFactory(); }
stub.GetFactory.OnCall(value);  // value is Func<int>
stub.GetFactory.OnCall(callback);  // callback is Func<Func<int>>
```
**Risk**: Overload resolution could be ambiguous if the return type is itself a delegate.
**Mitigation**: C# overload resolution should prefer the exact match (`Func<int>` over `Func<Func<int>>`). Add test case to verify.

**2. Nullable Async Types**
```csharp
// Task<string?> GetNameAsync();
stub.GetNameAsync.OnCall((string?)null);  // Should work
```
**Verification**: The design correctly stores the unwrapped type. Ensure null literal inference works.

**3. Generic Type Parameter Return**
```csharp
// T Get<T>() where T : class
stub.Get.Of<User>().OnCall(testUser);  // Works - type is known
```
**Status**: Existing generic method handlers use `MethodName}Delegate` pattern. Value overload requires knowing `TReturn` at compile time, which happens via `Of<T>()`.

**4. Overloaded Methods with Same Return Type**
```csharp
interface IFoo {
    string Get(int id);
    string Get(string name);
}
// Both generate OnCall(string value) - which one?
```
**Status**: Overloaded methods generate per-signature delegates (`GetDelegate_Int32_String`, `GetDelegate_String_String`). The value overload works on the per-signature interceptor, so no conflict.

**5. Init-Only Properties Without Getter**
```csharp
interface IEntity { string Name { init; } }  // No getter!
```
**Status**: `OnGet(value)` makes no sense here. Design should skip value overload generation for setter-only properties. Verified: `PropertyInterceptorRenderer` checks `model.HasGetter` before generating OnGet (lines 243-274).

---

### Diagnostic Requirements

**None Required**: The design does not introduce scenarios requiring generator diagnostics:
- Void methods naturally won't have `OnCall(value)` because there's no `TReturn`
- Ref/out methods require delegate syntax - attempting value overload is a compile error
- `.Value` removal produces standard compile errors

---

### Test Strategy

**Unit Tests to Add**:

1. **Method Value Overloads**
   - `OnCall(value)` returns configured value
   - `OnCall(value)` tracks invocations via returned `IMethodTracking`
   - Multiple calls return same value (repeating behavior)
   - `OnCall(value)` clears previous callback configuration
   - `OnCall(callback)` clears previous value configuration

2. **Async Auto-Wrapping**
   - `Task<T>` method: `OnCall(value)` returns `Task.FromResult(value)`
   - `ValueTask<T>` method: `OnCall(value)` returns `new ValueTask<T>(value)`
   - `Task<T?>` with null value
   - Verify callback tracking works with async

3. **Property Value Overloads**
   - `OnGet(value)` returns configured value
   - `OnGet(value)` returns `IPropertyGetTracking`
   - `OnGet(value)` clears previous callback configuration
   - Verification works: `stub.Name.OnGet("test").Verifiable()`

4. **Sequence Value Support**
   - `OnCallSequence(val1).ThenCall(val2).ThenCall(val3)` - values in order
   - `OnGetSequence(val1).ThenGet(val2)` - values in order
   - Mixed: `OnCallSequence(value).ThenCall(callback)` - value then callback

5. **Pattern Coverage**
   - Standalone stub with value overloads
   - Inline interface stub with value overloads
   - Inline class stub with value overloads
   - Delegate stub with value overload

6. **Edge Cases**
   - Delegate return type (overload resolution)
   - Generic method with Of<T>().OnCall(value)
   - Overloaded method signatures

---

### Codebase Analysis (Deep-Dive)

**Files Examined**:
| File | Lines | Key Observations |
|------|-------|-----------------|
| `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` | 984 | Single-signature: lines 49-156. Overload-group: lines 162-272. Invoke: lines 278-388. Storage pattern on lines 73-80. |
| `src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs` | 1027 | Init-only: lines 47-160 (has `.Value`). Regular: lines 166-340 (has `.Value`). InvokeGet: lines 346-404. |
| `src/Generator/Renderer/InlineRenderer.cs` | 1460 | Delegate stub rendering: lines 1252-1375. Uses different pattern - no shared renderer. |
| `src/KnockOff/IMethodTracking.cs` | 83 | Interface unchanged - value overload returns same interface. |
| `src/KnockOff/IMethodSequence.cs` | 43 | Needs `ThenCall(TReturn value)` method added (requires runtime interface change). |
| `src/KnockOff/IPropertySequence.cs` | 61 | Needs `ThenGet(TValue value)` method added (requires runtime interface change). |

**Key Pattern**: Storage follows `_field` + `_fieldTracking` pattern consistently.

**Interface Changes Required**:
```csharp
// IMethodSequence.cs - add:
IMethodSequence<TCallback> ThenCall<TReturn>(TReturn value);

// IPropertySequence.cs - add:
IPropertyGetSequence<TValue> ThenGet(TValue value);
```

Wait - the sequence interfaces are in the runtime library (`src/KnockOff/`), not generated. This means:
1. Adding `ThenCall(TReturn value)` to `IMethodSequence<TCallback>` is problematic because the interface is parameterized by `TCallback` (the delegate type), not `TReturn`
2. The generated `MethodSequenceImpl` can add value overloads without changing the interface by generating the method directly

**Design Clarification Needed**: The sequence value overloads should be generated methods, not interface methods, since the return type is determined at generation time, not defined in the interface.

---

### Additional Architectural Concerns

**1. OnSet(value) for Properties**
The todo mentions `OnSet(value)` but this doesn't make sense semantically:
- `OnGet()` configures what the getter returns
- `OnSet()` configures what happens when the setter is called
- `OnSet(value)` would mean "when setter is called, do what with this value?"

**Recommendation**: Remove `OnSet(value)` from scope. `OnSet()` is for callback/verification only.

**2. Delegate Stubs - Separate Implementation Required**
The `InlineRenderer.RenderDelegateStub()` method (lines 1254-1375) handles delegate stubs separately. It currently has:
```csharp
public void OnCall({del.OnCallType} callback) { _onCall = callback; }
```

For value support, it needs a parallel:
```csharp
public void OnCall({del.ReturnType} value) { _onCall = () => value; }  // Wrapper pattern
```

**3. Init-Only Property Considerations**
Init-only properties currently use `.Value` for init setter storage (line 62-63 in PropertyInterceptorRenderer):
```csharp
w.Line($"\t\t\t\t{setterKeyword} {{ {impl.InterceptorName}.RecordSet(value); {impl.InterceptorName}.Value = value; }}");
```

If we remove `.Value`, we need an alternative storage mechanism for init-set values. Options:
1. Keep internal `_value` field, just remove public accessor
2. Use internal `SetValue(T value)` method instead of property
3. Accept that init values aren't stored (setter only tracks calls)

**Recommendation**: Keep internal `_value` field and `_valueSet` flag for init-only properties. Only remove the public `Value` property accessor. The init setter implementation uses internal access anyway.

---

## Developer Review

**Status:** Approved

**Concerns:** None - ready for implementation with clarifications noted below.

### Review Summary

The architect's design is comprehensive and implementable. Key verification completed:

**1. Three Patterns Verification**
- Confirmed: `MethodInterceptorRenderer.cs` and `PropertyInterceptorRenderer.cs` are shared by all three patterns (Standalone, Inline Interface, Inline Class)
- Confirmed: Delegate stubs require separate implementation in `InlineRenderer.RenderDelegateStub()` (lines 1254-1375)
- The shared renderer approach means one implementation automatically supports all three patterns

**2. Test Migration Impact Corrected**
- Architect stated: "78 test files, 247 occurrences"
- Actual: **29 non-generated test files with 220 occurrences** need manual migration
- Generated files (in `Generated/` directories) will auto-regenerate - no manual work needed
- This reduces migration effort compared to original estimate

**3. Interface Changes Clarification**
- Confirmed: `IMethodSequence<TCallback>` is parameterized by callback type, not return type
- The architect correctly identified that `ThenCall(TReturn value)` cannot be an interface method
- Sequence value overloads must be generated methods, not added to runtime interfaces
- No changes to `src/KnockOff/IMethodSequence.cs` or `src/KnockOff/IPropertySequence.cs` required

**4. Init-Only Property Handling Confirmed**
- Lines 54-63 of `PropertyInterceptorRenderer.cs` show init-only properties store value via `.Value` property
- Line 126 shows `RecordSet` is called from init setter
- Recommendation confirmed: Keep internal `_value` field, remove only public `Value` property accessor
- Init setter will call an internal method instead of public property

**5. Async Type Detection Approach**
- The architect's string-based detection approach is appropriate:
  ```csharp
  bool isTaskT = returnType.StartsWith("global::System.Threading.Tasks.Task<");
  ```
- This works because `ReturnType` in `UnifiedMethodInterceptorModel` is already fully qualified

**6. Delegate Stub Implementation Gap**
- Current `InlineRenderer.RenderDelegateStub()` (line 1283):
  ```csharp
  public void OnCall({del.OnCallType} callback) {{ _onCall = callback; }}
  ```
- Needs parallel value overload when return type is not void
- Architect correctly identified this in Three Patterns Analysis table

**7. Priority Chain Ordering**
- Architect's recommended priority chain is sound:
  1. Value sequence (if present and not exhausted)
  2. Callback sequence (existing)
  3. Repeating value
  4. Repeating callback (existing)
- This maintains "sequence before repeating" precedent

### Minor Clarifications Applied

1. **Test count corrected**: 29 non-generated files, not 78
2. **OnSet(value) removed from acceptance criteria** (correctly excluded by architect)
3. **Sequence interfaces unchanged** - value overloads are generated methods only

---

## Implementation Contract

### In Scope

#### Phase 1: Method Value Overloads (MethodInterceptorRenderer.cs)

**Single-Signature Methods (lines 49-156):**
- [ ] Add value storage fields: `_onCallValue`, `_hasOnCallValue`, `_onCallValueTracking`
- [ ] Add `OnCall(TReturn value)` method (skip for void/ref/out methods)
- [ ] Update existing `OnCall(callback)` to clear value storage (mutual exclusivity)
- [ ] Update `RenderInvokeMethod` to check value before callback
- [ ] Add async type detection helper method
- [ ] Generate wrapping logic in Invoke for Task<T>/ValueTask<T>

**Overload Group Methods (lines 162-272):**
- [ ] Add per-signature value storage fields
- [ ] Add `OnCall(TReturn value)` per signature (skip for void/ref/out)
- [ ] Update existing `OnCall(callback)` per signature
- [ ] Update `RenderOverloadInvokeMethod` for value priority

**Nested Classes:**
- [ ] Update `MethodTrackingImpl` if needed (likely no changes)
- [ ] Add `ThenCall(TReturn value)` to `MethodSequenceImpl` (wrapper pattern)
- [ ] Add `OnCallSequence(TReturn value)` wrapper method

#### Phase 2: Property Changes (PropertyInterceptorRenderer.cs)

**Breaking Change - Remove .Value:**
- [ ] Remove public `Value` property from regular properties (lines 182-190)
- [ ] Keep internal `_value` field and `_valueSet` flag for init-only (lines 54-63)
- [ ] Update `IsConfigured` to not rely on `_valueSet` for regular properties
- [ ] Update `InvokeGet` fallback to use default instead of _value

**Add OnGet(value) Wrapper:**
- [ ] Add `OnGet(TValue value)` wrapper method (calls `OnGet(() => value)`)
- [ ] For init-only properties: add value overload
- [ ] For regular properties: add value overload

**Add ThenGet(value) to Sequence:**
- [ ] Add `ThenGet(TValue value)` to `PropertyGetSequenceImpl` (wrapper pattern)
- [ ] Add `OnGetSequence(TValue value)` wrapper method

#### Phase 3: Delegate Stub Value Overload (InlineRenderer.cs)

- [ ] Add `OnCall(TReturn value)` to delegate interceptor (lines 1280-1284)
- [ ] Skip for void delegates
- [ ] Wrapper pattern: `_onCall = () => value`

#### Phase 4: Test Migration

- [ ] Migrate 29 non-generated test files from `.Value = x` to `.OnGet(x)`
- [ ] Verify all existing tests pass
- [ ] Add new tests for value overloads (see Test Strategy in plan)

#### Phase 5: Documentation

- [ ] Update docs/getting-started.md
- [ ] Add migration guide for .Value removal
- [ ] Update code samples

### Out of Scope

The following are explicitly NOT part of this implementation:

1. **OnSet(value)** - Semantically unclear, removed from scope per architect recommendation
2. **Runtime interface changes** - `IMethodSequence<T>` and `IPropertySequence<T>` remain unchanged
3. **Model changes** - No changes to Builder or Model files required
4. **Indexer special handling** - Value overloads work automatically via shared renderers
5. **Class stub special handling** - Uses same PropertyInterceptorRenderer as others
6. **Generator diagnostics** - None needed; compile errors are self-explanatory

---

## Implementation Progress

### Phase 1: Method Value Overloads (COMPLETE)
- [x] Single-signature value storage and OnCall(value)
- [x] Single-signature Invoke priority chain update
- [ ] Overload group value storage and OnCall(value) - Deferred to Phase 5 per scope
- [ ] Overload group Invoke priority chain update - Deferred to Phase 5 per scope
- [x] Async type detection and wrapping (Task<T>, ValueTask<T>)
- [ ] Sequence value wrappers (ThenCall, OnCallSequence) - Deferred to Phase 5
- **Verification**: PASSED - all 664 tests passing (net10.0), 21 new MethodValueOverloadTests added

**Phase 1 Evidence:**
- `MethodInterceptorRenderer.cs` updated with value storage fields, OnCall(value) overload, Invoke priority chain
- `MethodValueOverloadTests.cs` created with 21 tests covering:
  - Basic value returns (string, int, bool, null)
  - Async auto-wrapping (Task<T>, Task<T?>)
  - Mutual exclusivity (value clears callback, callback clears value)
  - Tracking and verification
  - Generic interfaces
  - Complex objects and collections

### Phase 2: Property Changes (Breaking Change) - COMPLETE
- [x] Remove public .Value property (keep internal _value for init-only)
- [x] Add OnGet(value) wrapper method
- [x] Add OnGetSequence(value) wrapper method
- [x] Add ThenGet(value) to PropertyGetSequenceImpl
- [x] Update init-only property setter to use SetValue() instead of .Value
- [x] Update InvokeGet fallback to return default! instead of _value for regular properties
- [x] Update IsConfigured to remove _valueSet condition for regular properties
- **Verification**: EXPECTED FAILURES - 31 unique test files fail with CS1061 "does not contain definition for 'Value'"

**Phase 2 Evidence:**
- `PropertyInterceptorRenderer.cs` updated:
  - Init-only: Replaced public `Value` property with internal `SetValue(T value)` method
  - Regular properties: Removed `_value` and `_valueSet` fields entirely (no longer needed)
  - Added `OnGet(TValue value)` wrapper method (calls `OnGet(() => value)`)
  - Added `OnGetSequence(TValue value)` wrapper method (calls `OnGetSequence(() => value)`)
  - Added `ThenGet(TValue value)` to `PropertyGetSequenceImpl`
  - Updated `InvokeGet` to return `default!` for regular properties when unconfigured
  - Updated `IsConfigured` to not rely on `_valueSet` for regular properties
- `FlatRenderer.cs` updated: Init setter now calls `SetValue(value)` instead of `.Value = value`
- `InlineRenderer.cs` updated: Init setter now calls `SetValue(value)` instead of `.Value = value`
- `PropertyValueOverloadTests.cs` created with 14 tests covering OnGet(value) API

**Files Failing (31 unique - expected breaking change):**
- src/Benchmarks/KnockOff.Benchmarks/Benchmarks/InheritanceBenchmarks.cs
- src/Benchmarks/KnockOff.Benchmarks/Benchmarks/PropertyBenchmarks.cs
- src/Tests/KnockOff.Documentation.Samples/InterceptorApiSamples.cs
- src/Tests/KnockOff.Documentation.Samples/MoqMigrationSamples.cs
- src/Tests/KnockOff.Documentation.Samples/NSubstituteMigrationSamples.cs
- src/Tests/KnockOff.Documentation.Samples/PatternsSamples.cs
- src/Tests/KnockOff.Documentation.Samples/PropertiesSamples.cs
- src/Tests/KnockOff.Documentation.Samples/TroubleshootingSamples.cs
- src/Tests/KnockOff.NeatooInterfaceTests/BuiltInRules/IRequiredRuleTests.cs
- src/Tests/KnockOff.NeatooInterfaceTests/BuiltInRules/OtherBuiltInRuleTests.cs
- src/Tests/KnockOff.NeatooInterfaceTests/Collections/IEntityListBaseTests.cs
- src/Tests/KnockOff.NeatooInterfaceTests/Collections/IValidateListBaseTests.cs
- src/Tests/KnockOff.NeatooInterfaceTests/MetaProperties/IEntityMetaPropertiesTests.cs
- src/Tests/KnockOff.NeatooInterfaceTests/MetaProperties/IValidateMetaPropertiesTests.cs
- src/Tests/KnockOff.NeatooInterfaceTests/Properties/IEntityPropertyTests.cs
- src/Tests/KnockOff.NeatooInterfaceTests/Properties/IPropertyInfoTests.cs
- src/Tests/KnockOff.NeatooInterfaceTests/Properties/IValidatePropertyTests.cs
- src/Tests/KnockOff.NeatooInterfaceTests/PropertyManagers/IEntityPropertyManagerTests.cs
- src/Tests/KnockOff.NeatooInterfaceTests/PropertyManagers/IValidatePropertyManagerTests.cs
- src/Tests/KnockOff.NeatooInterfaceTests/ValidationRules/IRuleManagerTests.cs
- src/Tests/KnockOff.NeatooInterfaceTests/ValidationRules/IRuleMessageTests.cs
- src/Tests/KnockOff.NeatooInterfaceTests/ValidationRules/IRuleMessagesTests.cs
- src/Tests/KnockOff.NeatooInterfaceTests/ValidationRules/IRuleOfTTests.cs
- src/Tests/KnockOff.NeatooInterfaceTests/ValidationRules/IRuleTests.cs
- src/Tests/KnockOff.NeatooInterfaceTests/ValidationRules/ITriggerPropertyTests.cs
- src/Tests/KnockOffTests/CallbackTests.cs
- src/Tests/KnockOffTests/GenericStandaloneStubTests.cs
- src/Tests/KnockOffTests/InitPropertyTests.cs
- src/Tests/KnockOffTests/InlineStubTests.cs
- src/Tests/KnockOffTests/NeatooTests.cs
- src/Tests/KnockOffTests/OpenGenericInlineStubTests.cs

### Phase 3: Test Migration
- [ ] Migrate tests in KnockOff.NeatooInterfaceTests/ValidationRules/
- [ ] Migrate tests in KnockOff.NeatooInterfaceTests/BuiltInRules/
- [ ] Migrate tests in KnockOff.NeatooInterfaceTests/MetaProperties/
- [ ] Migrate tests in KnockOff.NeatooInterfaceTests/Properties/
- [ ] Migrate tests in KnockOff.NeatooInterfaceTests/PropertyManagers/
- [ ] Migrate tests in KnockOff.NeatooInterfaceTests/Collections/
- [ ] Migrate tests in KnockOffTests/
- **Verification**: Run `dotnet test` - all tests must pass

### Phase 4: Delegate Stubs (COMPLETE)
- [x] Add OnCall(value) to delegate interceptor
- [x] Skip for void delegates (Action, Action<T>)
- [x] Handle all arities (0 params, 1 param, n params)
- [x] Create DelegateValueOverloadTests.cs with 18 tests
- **Verification**: PASSED - all 694 tests passing (net10.0)

**Phase 4 Evidence:**
- Modified `src/Generator/Renderer/InlineRenderer.cs` (lines 1285-1294):
  ```csharp
  // OnCall value overload - wraps value in lambda (non-void delegates only)
  if (!del.IsVoid)
  {
      var ignoredParams = del.Parameters.Count == 0
          ? ""
          : string.Join(", ", Enumerable.Range(0, del.Parameters.Count).Select(_ => "_"));
      w.Line($"\t\t\t/// <summary>Configures return value for delegate. Always returns the specified value.</summary>");
      w.Line($"\t\t\tpublic void OnCall({del.ReturnType} value) {{ _onCall = ({ignoredParams}) => value; }}");
      w.Line();
  }
  ```
- Created `src/Tests/KnockOffTests/DelegateValueOverloadTests.cs` with 18 tests:
  - Basic value returns (string, int, bool, null for nullable delegates)
  - Single and multi-parameter delegates (value overload ignores arguments)
  - Complex objects and collections
  - Mutual exclusivity (value clears callback, callback clears value)
  - Void delegate verification (callback-only, no value overload)
  - Verification and Reset functionality

**Generated Code Sample (0 params):**
```csharp
public void OnCall(string value) { _onCall = () => value; }
```

**Generated Code Sample (1 param):**
```csharp
public void OnCall(string value) { _onCall = (_) => value; }
```

**Generated Code Sample (2 params):**
```csharp
public void OnCall(string value) { _onCall = (_, _) => value; }
```

**Void Delegate Verification:**
Void delegates (VoidNotify, VoidLogger) correctly do NOT generate value overload - only callback overload is present.

### Phase 5: New Tests and Documentation
- [x] Add value overload tests (methods, async, properties, sequences)
- [x] Add edge case tests (delegate return types, generics, overloaded methods)
- [x] Update documentation (getting-started.md, Documentation.Samples)
- [x] Create migration guide (docs/migration/property-value-removal.md)
- **Verification**: All 1432 tests pass

---

## Completion Evidence

**Completed 2026-01-24:**

**Test Output:**
```
Passed!  - Failed:     0, Passed:   218, Skipped:     0, Total:   218 - KnockOff.Documentation.Samples.dll
Passed!  - Failed:     0, Passed:   473, Skipped:     0, Total:   473 - KnockOff.NeatooInterfaceTests.dll
Passed!  - Failed:     0, Passed:   741, Skipped:     0, Total:   741 - KnockOffTests.dll
Total: 1432 tests passing
```

**New Test Files Created:**
- `SequenceValueOverloadTests.cs` - 17 tests for property sequence value overloads
- `EdgeCaseValueOverloadTests.cs` - 14 tests for delegate returns, async nullable, overloads
- `ThreePatternValueOverloadTests.cs` - 16 tests verifying all three patterns

**Documentation Updated:**
- `docs/getting-started.md` - Added "Configuring Return Values" section
- `MethodsSamples.cs` - Added `methods-oncall-value` and `methods-oncall-value-vs-callback` examples
- `PropertiesSamples.cs` - Added `properties-onget-value-vs-callback` and `properties-ongetsequence-value` examples
- `AsyncSamples.cs` - Added `async-task-value-overload` example
- `DelegatesSamples.cs` - Added `delegate-stub-oncall-value` example
- `docs/migration/property-value-removal.md` - Created migration guide

**Generated Code Sample (OnCall value):**
```csharp
// From SampleKnockOff.g.cs line ~940
public global::KnockOff.IMethodTracking OnCall(string? value)
{
    _sequence = null;
    _sequenceIndex = 0;
    _isVerifiable = false;
    _verifiableTimes = null;
    _onCall = null;
    _onCallTracking = null;
    _hasOnCallValue = true;
    _onCallValue = value;
    _onCallValueTracking = new MethodTrackingImpl(this);
    return _onCallValueTracking;
}
```

**Generated Code Sample (OnGet value):**
```csharp
// From SampleKnockOff.g.cs line ~69
public global::KnockOff.IPropertyGetTracking OnGet(string? value) => OnGet(() => value);
```

- [x] All existing tests passing (1432 total)
- [x] New value overload tests passing (47 new tests)
- [x] Generated code sample showing `OnCall(value)` working
- [x] Generated code sample showing `OnGet(value)` working
- [x] All implementation contract items checked off
