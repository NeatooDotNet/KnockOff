# Method Sequence Value Overloads Design

**Date:** 2026-02-01
**Related Todo:** [Method Sequence Value Overloads](../todos/method-sequence-value-overloads.md)
**Status:** Under Review (Developer)
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

[Developer adds concerns/questions here during review phase]

**Status:** Not Started

**Concerns:** [List any issues found, or "None - ready for implementation"]

---

## Implementation Contract

[Developer fills before starting implementation]

**In Scope:**
- [ ] File changes listed
- [ ] Test cases listed

**Out of Scope:**
[Explicitly list what will NOT be changed]

---

## Implementation Progress

[Track progress through phases]

---

## Completion Evidence

[Required before marking complete]

- **Tests Passing:** [Output or screenshot]
- **Generated Code Sample:** [Snippet showing feature works]
- **All Checklist Items:** [Confirmed 100% complete]
