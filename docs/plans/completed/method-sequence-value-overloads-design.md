# Method Sequence Value Overloads Design

**Date:** 2026-02-01
**Related Todo:** [Method Sequence Value Overloads](../todos/method-sequence-value-overloads.md)
**Status:** Complete
**Last Updated:** 2026-02-01

---

## Overview

Add `ThenReturns(TValue value)` method to generated `MethodSequenceImpl` classes, enabling value-based method sequence chaining that parallels property sequence patterns.

---

## Approach

Generate a `ThenReturns(TValue value)` method directly on `MethodSequenceImpl` that wraps the value in a callback delegate. This avoids modifying the `IMethodSequence<TCallback>` interface (which is parameterized by callback type, not return type) while providing the convenience users expect.

For async methods, auto-wrap values with `Task.FromResult()` or `new ValueTask<T>()` to match the existing `Returns(value)` behavior.

---

## Design

### API Surface

**User writes:**
```csharp
// Sync method - returns string
stub.GetOptional.Returns("first").ThenReturns("second").ThenReturns("third");

// Async method - auto-wraps with Task.FromResult
stub.GetUserAsync.Returns(user1).ThenReturns(user2);

// Can mix with callbacks
stub.GetOptional.OnCall(() => "computed").ThenReturns("constant").ThenCall(() => "computed2");
```

**Generated code pattern (simplified):**
```csharp
private sealed class MethodSequenceImpl : IMethodSequence<Func<string?>>
{
    // Existing callback method
    public IMethodSequence<Func<string?>> ThenCall(Func<string?> callback) { ... }

    // NEW: Value overload - wraps value in callback
    public IMethodSequence<Func<string?>> ThenReturns(string? value) => ThenCall(() => value);
}
```

**For async methods (Task<T>):**
```csharp
private sealed class MethodSequenceImpl : IMethodSequence<Func<Task<User>>>
{
    public IMethodSequence<Func<Task<User>>> ThenCall(Func<Task<User>> callback) { ... }

    // Value overload - auto-wraps with Task.FromResult
    public IMethodSequence<Func<Task<User>>> ThenReturns(User value)
        => ThenCall(() => Task.FromResult(value));
}
```

**For async methods (ValueTask<T>):**
```csharp
private sealed class MethodSequenceImpl : IMethodSequence<Func<ValueTask<User>>>
{
    public IMethodSequence<Func<ValueTask<User>>> ThenCall(Func<ValueTask<User>> callback) { ... }

    // Value overload - auto-wraps with new ValueTask
    public IMethodSequence<Func<ValueTask<User>>> ThenReturns(User value)
        => ThenCall(() => new ValueTask<User>(value));
}
```

### What NOT to Generate

Do NOT generate `ThenReturns` for:
1. **Void methods** - No return value to provide
2. **Methods with ref/out parameters** - Callbacks needed to handle parameter mutation
3. **Task/ValueTask (non-generic)** - These are "void async" methods

### Interface Changes

**No changes to `IMethodSequence<TCallback>`** - The value overload is a generated convenience method, not an interface contract. This is the same pattern used by `PropertyGetSequenceImpl.ThenGet(TValue value)`.

### Naming Decision: `ThenReturns` vs `ThenCall`

| Option | Pros | Cons |
|--------|------|------|
| `ThenReturns(value)` | Parallels `Returns(value)`, clear intent | New name to learn |
| `ThenCall(value)` | Consistent with existing `ThenCall` | Confusing - `ThenCall` takes callback |

**Decision: `ThenReturns(value)`** - This clearly indicates a value (not callback) is being provided, and parallels the existing `Returns(value)` API.

### MethodCallBuilderImpl Changes

The builder also needs `ThenReturns(value)` for the elevation case:
```csharp
stub.GetUser.Returns(user1).ThenReturns(user2);  // Builder -> Sequence with value
```

Current `MethodCallBuilderImpl` has:
- `ThenCall(callback)` - elevates to sequence, adds callback

Need to add:
- `ThenReturns(value)` - elevates to sequence, adds value (wraps in callback)

---

## Implementation Steps

### Phase 1: Generator Changes

1. **Modify `RenderMethodSequenceImpl` in `MethodInterceptorRenderer.cs`:**
   - After rendering `ThenCall(callback)`, check if method can have value overload
   - Render `ThenReturns(TValue value)` that delegates to `ThenCall(() => value)` or async-wrapped version

2. **Modify `RenderMethodCallBuilderImpl` in `MethodInterceptorRenderer.cs`:**
   - After rendering `ThenCall(callback)`, render `ThenReturns(value)` that:
     - Elevates to sequence mode (same as ThenCall)
     - Wraps value in appropriate callback (sync or async)
     - Returns sequence

### Phase 2: Tests

3. **Add tests in `SequenceValueOverloadTests.cs`:**
   - `Returns_ThenReturns_ReturnsSequence`
   - `OnCall_ThenReturns_MixedSequence`
   - `AsyncMethod_ThenReturns_AutoWraps`
   - `ValueTaskMethod_ThenReturns_AutoWraps`
   - `ThenReturns_TracksCorrectly`
   - `ThenReturns_SequenceExhaustion`

### Phase 3: Documentation

4. **Update `src/Design/Design.Stubs/Methods/MethodSequences.cs`:**
   - Remove "REJECTED PATTERN" note about `ThenReturns`
   - Add example showing `Returns().ThenReturns()` pattern
   - Document mixing callbacks and values in sequences

---

## Acceptance Criteria

- [ ] `stub.Method.Returns(v1).ThenReturns(v2)` works for sync methods
- [ ] `stub.Method.OnCall(cb).ThenReturns(v)` works (mixed)
- [ ] `stub.AsyncMethod.Returns(v1).ThenReturns(v2)` auto-wraps Task.FromResult
- [ ] `stub.ValueTaskMethod.Returns(v1).ThenReturns(v2)` auto-wraps new ValueTask
- [ ] Sequence exhaustion works same as callback sequences
- [ ] Verification works same as callback sequences
- [ ] All four patterns (Standalone, Inline Interface, Inline Class, Delegate) supported
- [ ] No `ThenReturns` generated for void/ref/out methods

---

## Dependencies

- Existing `Returns(value)` implementation for async wrapping logic
- Existing `ThenCall(callback)` implementation in `MethodSequenceImpl`
- `PropertyGetSequenceImpl.ThenGet(value)` as reference pattern

---

## Risks / Considerations

1. **Overload resolution with null**: For `ThenReturns(null)` on nullable reference types, may need explicit cast or the compiler might be ambiguous with `ThenCall`. Test this case.

2. **Generic methods**: For generic methods like `T GetValue<T>()`, the value type depends on the type argument. Ensure generated code handles this.

3. **Method overloads**: For overloaded methods, each overload's `MethodSequenceImpl_{suffix}` gets its own `ThenReturns` appropriate to its return type.

---

## Architectural Verification

**All verification items completed.**

### Three Patterns Analysis

**Standalone Pattern:**
- `MethodInterceptorRenderer.RenderMethodSequenceImpl()` generates `MethodSequenceImpl` for standalone stubs
- Single-signature: Uses `model.ReturnType` to determine value type for `ThenReturns`
- Overload groups: Each `MethodSequenceImpl_{suffix}` uses `overload.ReturnType`
- Changes apply automatically via shared renderer

**Inline Interface Pattern:**
- Same `MethodInterceptorRenderer` renders interceptors for inline interface stubs
- `UnifiedMethodInterceptorModel` provides `ReturnType` for single-signature methods
- `MethodOverloadSignature` provides `ReturnType` for overloads
- No separate code paths - same generator handles both

**Inline Class Pattern:**
- Inline class stubs also use `MethodInterceptorRenderer` for method interceptors
- Virtual/abstract methods get interceptors with same sequence support
- No special handling needed

**Inline Delegate Pattern:**
- Delegate stubs generate single-method interceptors
- Same `MethodInterceptorRenderer` applies
- `ThenReturns` generated based on delegate return type

### Breaking Changes

**No** - This is purely additive:
- `IMethodSequence<TCallback>` interface unchanged
- `ThenReturns` is a generated method on implementation class
- Existing `ThenCall(callback)` behavior unchanged
- No existing tests affected

### Pattern Consistency

This design follows the established pattern from property sequences:

| Property Sequence | Method Sequence (Proposed) |
|------------------|---------------------------|
| `IPropertyGetSequence<TValue>.ThenGet(Func<TValue>)` | `IMethodSequence<TCallback>.ThenCall(TCallback)` |
| `PropertyGetSequenceImpl.ThenGet(TValue value)` (generated) | `MethodSequenceImpl.ThenReturns(TValue value)` (generated) |
| Wraps as `() => value` | Wraps as `() => value` or async-wrapped |

The naming difference (`ThenGet` vs `ThenReturns`) reflects the API naming on the interceptor:
- Properties: `OnGet(value)` -> `ThenGet(value)`
- Methods: `Returns(value)` -> `ThenReturns(value)`

### Diagnostic Requirements

No new diagnostics needed. Invalid cases (void, ref/out) simply don't get `ThenReturns` generated.

### Test Strategy

1. **Basic functionality:** Value sequences work for sync methods
2. **Async wrapping:** Task<T> and ValueTask<T> auto-wrap correctly
3. **Mixed sequences:** Callbacks and values can be mixed
4. **Edge cases:** Null values, sequence exhaustion, verification
5. **All patterns:** Verify generated code for standalone, inline interface, inline class

### Edge Cases

1. **Null values:** `ThenReturns((string?)null)` - cast may be needed if ambiguous
2. **Generic methods:** `T GetValue<T>()` - `ThenReturns` uses `T` as value type
3. **Overloaded methods:** Each overload's sequence gets appropriate `ThenReturns`
4. **Method with parameters:** `int Add(int a, int b)` - `ThenReturns(5)` ignores parameters (same as `ThenCall((_,_) => 5)`)

### Codebase Analysis

**Files Examined:**

1. **`src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs`** (lines 1466-1550)
   - `RenderMethodSequenceImpl` generates `MethodSequenceImpl` class
   - Currently only renders `ThenCall(callback)`
   - Need to add `ThenReturns(value)` after `ThenCall`
   - Must determine return type and async wrapping needs

2. **`src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs`** (lines 925-982)
   - `RenderPropertyGetSequenceImpl` shows reference pattern
   - Generates both `ThenGet(Func<TValue>)` and `ThenGet(TValue value)`
   - Value overload: `ThenGet({valueType} value) => ThenGet(() => value);`

3. **`src/Generator/Model/Shared/UnifiedMethodInterceptorModel.cs`**
   - Has `ReturnType` (string), `IsVoid` (bool) for single-signature
   - Has `Overloads` array with `MethodOverloadSignature` for overload groups

4. **`src/Generator/Model/Shared/MethodOverloadSignature.cs`**
   - Has `ReturnType` (string), `IsVoid` (bool) per overload

5. **`src/KnockOff/IMethodSequence.cs`**
   - Interface has `ThenCall(TCallback callback)` only
   - No value overload (by design - callback type != return type)

6. **`src/KnockOff/IPropertySequence.cs`**
   - Interface has both `ThenGet(Func<TValue>)` and `ThenGet(TValue)`
   - Property callback type matches value type, so interface can declare both

7. **Helper methods in `MethodInterceptorRenderer.cs`:**
   - `GetAsyncTypeInfo(returnType)` - extracts inner type from Task<T>/ValueTask<T>
   - `GetVoidAsyncInfo(returnType)` - detects Task/ValueTask (non-generic)
   - These will be reused for determining async wrapping

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-02-01

### My Understanding of This Plan

**Core Change:** Add `ThenReturns(TValue value)` method to generated `MethodSequenceImpl` and `MethodCallBuilderImpl` classes to enable value-based method sequence chaining.

**User-Facing API:** `stub.Method.Returns(v1).ThenReturns(v2)` and `stub.Method.OnCall(cb).ThenReturns(v)`

**Internal Changes:** Modify `RenderMethodSequenceImpl` and `RenderMethodCallBuilderImpl` in `MethodInterceptorRenderer.cs`

**Patterns Affected:** All four (Standalone, Inline Interface, Inline Class, Delegate) - same renderer

### Codebase Investigation

**Files Examined:**
- `/home/keithvoels/neatoodotnet/KnockOff/src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` - Confirmed `RenderMethodSequenceImpl` (lines 1466-1550) and `RenderMethodCallBuilderImpl` (lines 1295-1460) are the target functions. Helper methods `GetAsyncTypeInfo` and `GetVoidAsyncInfo` exist and can be reused.
- `/home/keithvoels/neatoodotnet/KnockOff/src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs` - Confirmed reference pattern at lines 951-953: `ThenGet({valueType} value) => ThenGet(() => value);`
- `/home/keithvoels/neatoodotnet/KnockOff/src/KnockOff/IMethodSequence.cs` - Confirmed interface has only `ThenCall`, no value overload (by design)
- `/home/keithvoels/neatoodotnet/KnockOff/src/KnockOff/IMethodCallBuilder.cs` - Confirmed builder interfaces have `ThenCall` returning `IMethodSequence<TCallback>`
- `/home/keithvoels/neatoodotnet/KnockOff/src/Tests/KnockOffTests/SequenceValueOverloadTests.cs` - Existing test file for property sequence values; good place to add method sequence value tests
- `/home/keithvoels/neatoodotnet/KnockOff/src/Design/Design.Stubs/Methods/MethodSequences.cs` - Contains "REJECTED PATTERN" at lines 101-106 that will be updated

**Searches Performed:**
- Searched for `ThenReturns|ThenCall` - found 29 files using these patterns, confirmed no existing `ThenReturns` on method sequences
- Searched for `RenderMethodCallBuilderImpl` - found it's called for both single-signature (line 311) and overloads (line 549)
- Searched for `HasRefOrOutParameters` - confirmed it's used throughout to exclude ref/out methods from value overloads

**Discrepancies Found:** None - plan accurately reflects codebase structure

### Structured Question Checklist

**Completeness Questions:**
- [x] All four patterns addressed (Standalone, Inline Interface, Inline Class, Delegate) - Yes, all use same `MethodInterceptorRenderer`
- [x] What happens with null/empty/default values - Plan mentions null case in Risks section
- [x] What happens with generic type parameters - Plan addresses generic methods in Edge Cases
- [x] What happens with nested types or inherited members - N/A, method interceptors handle this
- [x] Interaction with existing features - Sequences integrate with existing OnCall/ThenCall, verification, When chains

**Correctness Questions:**
- [x] Do generated code examples compile - Yes, follows existing pattern from property sequences
- [x] Is implementation consistent with existing patterns - Yes, mirrors `PropertyGetSequenceImpl.ThenGet(value)`
- [x] Are model/builder/renderer responsibilities correct - Yes, renderer generates, no model changes needed
- [x] Breaking changes migration path - N/A, purely additive

**Clarity Questions:**
- [x] Can I implement without clarifying questions - Yes
- [x] Ambiguous requirements - None found
- [x] Edge cases explicitly handled - Yes, void/ref/out exclusions documented
- [x] Test strategy specific enough - Yes, test names provided

**Risk Questions:**
- [x] What could go wrong - Overload resolution with null (documented in plan)
- [x] Which existing tests might fail - None, purely additive
- [x] Performance implications - None, trivial wrapper method
- [x] Backward compatibility concerns - None, additive change

### Devil's Advocate Analysis

**Edge cases NOT explicitly covered:**
1. **Methods returning `IAsyncEnumerable<T>`** - Not covered but acceptable; this is a separate async pattern from Task/ValueTask and likely out of scope
2. **Nullable value types** - `int?` return type should work but not explicitly tested in plan

**Ways this could break existing functionality:**
1. No concerns - purely additive, existing `ThenCall` unchanged

**Ways users could misunderstand the API:**
1. Already addressed in plan - `ThenReturns` vs `ThenCall` naming makes intent clear

### Why This Plan Is Exceptionally Clear

This plan is well-structured and complete because:
1. The design directly mirrors an existing proven pattern (`PropertyGetSequenceImpl.ThenGet(value)`)
2. All helper methods needed already exist (`GetAsyncTypeInfo`, `GetVoidAsyncInfo`, `HasRefOrOutParameters`)
3. The exclusion criteria (void, ref/out, void-async) match existing `Returns(value)` logic
4. The two target functions are clearly identified with exact line numbers
5. Test cases are enumerated with descriptive names
6. Edge cases are explicitly documented

### Review Summary

- Files examined: 6
- Questions checked: 16 of 16
- Devil's advocate items: 3 generated, 1 already addressed in plan, 2 acceptable as out-of-scope

### Concerns

None - ready for implementation

---

## Implementation Contract

**Created:** 2026-02-01
**Approved by:** knockoff-developer

### In Scope

**Phase 1: Generator Changes** - COMPLETE
- [x] `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs`:
  - [x] Modify `RenderMethodSequenceImpl` to add `ThenReturns(TValue value)` method
  - [x] Add check: only generate if `!isVoid && !hasRefOrOut`
  - [x] Handle sync case: `ThenReturns({valueType} value) => ThenCall({discards} => value);`
  - [x] Handle Task<T> case: `ThenReturns({innerType} value) => ThenCall({discards} => Task.FromResult(value));`
  - [x] Handle ValueTask<T> case: `ThenReturns({innerType} value) => ThenCall({discards} => new ValueTask<{innerType}>(value));`
  - [x] Modify `RenderMethodCallBuilderImpl` to add `ThenReturns(TValue value)` method
  - [x] Same async wrapping logic as above
  - [x] **Checkpoint: Build solution, verify no compile errors** - All 12 test projects built successfully

**Phase 2: Tests** - COMPLETE
- [x] `src/Tests/KnockOffTests/SequenceValueOverloadTests.cs`:
  - [x] Add `OnCall_ThenReturns_ReturnsSequence` test
  - [x] Add `OnCall_ThenReturns_MixedSequence` test
  - [x] Add `AsyncMethod_ThenReturns_AutoWraps` test
  - [x] Add `ValueTaskMethod_ThenReturns_AutoWraps` test
  - [x] Add `ThenReturns_TracksCorrectly` test (verify sequence.Verify() works)
  - [x] Add `ThenReturns_SequenceExhaustion_ReturnsDefaultInNonStrictMode` test
  - [x] Add `ThenReturns_SequenceExhaustion_ThrowsInStrictMode` test
  - [x] Add `ThenReturns_NullValue_WorksCorrectly` test
  - [x] **Checkpoint: Run all tests, all pass** - 27 sequence tests pass

**Phase 3: Documentation** - COMPLETE
- [x] `src/Design/Design.Stubs/Methods/MethodSequences.cs`:
  - [x] Remove "REJECTED PATTERN" notes
  - [x] Add `ThenReturns_CreatesSequenceOfValues()` example showing value sequences
  - [x] Add `MixedSequence_CallbacksAndValues()` example showing mixed sequences
- [x] `src/Design/Design.Tests/MethodTests/MethodSequenceTests.cs`:
  - [x] Add `ThenReturns_CreatesValueSequence` test
  - [x] Add `ThenReturns_MixedWithThenCall` test
  - [x] Add `ThenReturns_Verify_Works` test
  - [x] **Checkpoint: Build Design projects, run Design tests** - 118 Design tests pass

### Explicitly Out of Scope

- **IAsyncEnumerable<T> support** - Different async pattern, future enhancement
- **Interface changes** - `IMethodSequence<TCallback>` unchanged; value overload is generated only
- **IMethodCallBuilder interface changes** - Same reasoning; generated method only
- **Indexer sequences** - Indexers don't have method sequences (different API)
- **Event sequences** - Events don't have method sequences

### Verification Gates

1. **After Phase 1:** Solution builds without errors. Generated code for a test stub shows `ThenReturns` method on `MethodSequenceImpl` and `MethodCallBuilderImpl`.

2. **After Phase 2:** All existing tests pass. New tests pass. Test coverage includes sync, Task<T>, ValueTask<T>, null values, mixed sequences, and sequence exhaustion.

3. **Final:** All tests pass. Design project builds and tests pass. No regressions.

### Stop Conditions

If any of these occur, STOP and report:
- Out-of-scope test fails unexpectedly
- Architectural contradiction discovered (e.g., delegate type structure prevents implementation)
- Generated code does not compile for any pattern

---

## Implementation Progress

**Started:** 2026-02-01
**Developer:** knockoff-developer

### Phase 1: Generator Changes - COMPLETE

**Changes made to `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs`:**

1. Added `parameterCount` parameter to `RenderMethodSequenceImpl` and `RenderMethodCallBuilderImpl` functions
2. Added `BuildDiscardLambdaPrefix` helper method to generate correct discard patterns:
   - 0 params: `()`
   - 1 param: `(_)`
   - 2 params: `(_, _)`
3. Added `ThenReturns(TValue value)` method generation in both:
   - `RenderMethodSequenceImpl` (after ThenCall)
   - `RenderMethodCallBuilderImpl` (after ThenCall)
4. Correctly handles sync, Task<T>, and ValueTask<T> return types
5. Correctly excludes void methods and methods with ref/out parameters

**Verification:**
- Solution builds without errors (0 warnings, 0 errors)
- All 956 tests pass on net10.0, 956 on net9.0, 955 on net8.0
- Generated code verified to show correct `ThenReturns` methods with proper discard patterns

### Phase 2: Tests - COMPLETE

**Changes made to `src/Tests/KnockOffTests/SequenceValueOverloadTests.cs`:**

1. Added 8 new tests for `ThenReturns` functionality:
   - `OnCall_ThenReturns_ReturnsSequence` - Basic value sequence
   - `OnCall_ThenReturns_MixedSequence` - Mix callbacks and values
   - `AsyncMethod_ThenReturns_AutoWraps` - Task<T> auto-wrapping
   - `ValueTaskMethod_ThenReturns_AutoWraps` - ValueTask<T> auto-wrapping
   - `ThenReturns_TracksCorrectly` - Verification works
   - `ThenReturns_SequenceExhaustion_ReturnsDefaultInNonStrictMode` - Exhaustion behavior
   - `ThenReturns_SequenceExhaustion_ThrowsInStrictMode` - Strict mode exhaustion
   - `ThenReturns_NullValue_WorksCorrectly` - Null value handling

2. Added `IValueTaskMethodService` interface and `ValueTaskMethodKnockOff` stub for ValueTask testing

**Bug Fix During Phase 2:**

Fixed double-counting bug in `TotalCallCount` when elevating to sequence mode:
- Added `_onCallTracking = null` in `ThenCall` elevation logic
- Without this fix, the same tracking object was counted twice (once via `_onCallTracking` field, once in sequence)

### Phase 3: Documentation - COMPLETE

**Changes made to `src/Design/Design.Stubs/Methods/MethodSequences.cs`:**

1. Removed "REJECTED PATTERN" documentation for `Returns().ThenReturns()`
2. Added `ThenReturns_CreatesSequenceOfValues()` example
3. Added `MixedSequence_CallbacksAndValues()` example
4. Updated header to mention `OnCall().ThenReturns()` pattern

**Changes made to `src/Design/Design.Tests/MethodTests/MethodSequenceTests.cs`:**

1. Added `ThenReturns_CreatesValueSequence` test
2. Added `ThenReturns_MixedWithThenCall` test
3. Added `ThenReturns_Verify_Works` test

---

## Completion Evidence

**Completed:** 2026-02-01

### Test Results

All tests pass across all target frameworks:

```
KnockOffTests.dll:
- net8.0:  963 passed
- net9.0:  964 passed
- net10.0: 964 passed

Design.Tests.dll:
- net8.0:  118 passed
- net9.0:  118 passed
- net10.0: 118 passed

SequenceValueOverloadTests: 27 tests pass (including 8 new ThenReturns tests)
```

### Generated Code Sample

From `SampleKnockOff.g.cs` - GetOptional interceptor:

```csharp
public sealed class MethodSequenceImpl : global::KnockOff.IMethodSequence<GetOptionalDelegate>
{
    // Adds a value to the sequence. The value is returned exactly once.
    public MethodSequenceImpl ThenReturns(string? value) => ThenCall(() => value);

    // ... other methods
}

public sealed class MethodCallBuilderImpl : global::KnockOff.IMethodCallBuilder<GetOptionalDelegate>
{
    // Elevates to sequence mode and adds a value. Returns sequence for further chaining.
    public MethodSequenceImpl ThenReturns(string? value) => ThenCall(() => value);

    // ... other methods
}
```

### All Checklist Items

- [x] Phase 1: Generator Changes - COMPLETE
- [x] Phase 2: Tests - COMPLETE
- [x] Phase 3: Documentation - COMPLETE
- [x] All verification gates passed
- [x] No stop conditions triggered
