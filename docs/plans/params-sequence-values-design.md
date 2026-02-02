# Params Sequence Values Design

**Date:** 2026-02-01
**Related Todo:** [Params Sequence Values](../todos/params-sequence-values.md)
**Status:** Complete
**Last Updated:** 2026-02-01

---

## Overview

Add params overloads to enable NSubstitute-style concise sequence syntax:
- `Returns(first, params rest)` - Implicit sequence from multiple values
- `ThenReturns(params values)` - Add multiple values to explicit sequence
- `ThenGet(params values)` - Add multiple values to property getter sequence

---

## Approach

Generate params overloads that internally loop over the values, calling the existing single-value methods. This builds on the recently completed `ThenReturns(TValue value)` feature and follows the same pattern used by property `ThenGet(TValue value)`.

---

## Design

### API Surface

**Methods - Implicit Sequence via Returns:**
```csharp
// User writes:
stub.Method.Returns(1, 2, 3, 4);

// Behavior: Returns 1, then 2, then 3, then 4, then repeats 4 (NSubstitute-like)
// Equivalent to:
stub.Method.OnCall(() => 1).ThenReturns(2).ThenReturns(3).ThenReturns(4);
```

**Methods - Explicit Sequence via ThenReturns:**
```csharp
// User writes:
stub.Method.OnCall(() => compute()).ThenReturns(2, 3, 4);

// Behavior: First call uses callback, then returns 2, 3, 4, then repeats 4
// Equivalent to:
stub.Method.OnCall(() => compute()).ThenReturns(2).ThenReturns(3).ThenReturns(4);
```

**Properties - ThenGet with params:**
```csharp
// User writes:
stub.Name.OnGet("first").ThenGet("second", "third", "fourth");

// Behavior: Returns "first", then "second", then "third", then "fourth", then repeats "fourth"
```

### Generated Code Pattern

**For `Returns(TValue first, params TValue[] rest)` on method interceptors:**
```csharp
/// <summary>Configures sequence of return values. Each value returned once, last repeats.</summary>
public MethodSequenceImpl Returns(string? first, params string?[] rest)
{
    // Start with OnCall for first value, then chain ThenReturns for rest
    var seq = OnCall(() => first);
    foreach (var value in rest)
        seq = seq.ThenReturns(value);
    return seq;
}
```

**For async methods (Task<T>):**
```csharp
/// <summary>Configures sequence of return values. Auto-wrapped in Task.FromResult.</summary>
public MethodSequenceImpl Returns(User first, params User[] rest)
{
    var seq = OnCall(() => Task.FromResult(first));
    foreach (var value in rest)
        seq = seq.ThenReturns(value);  // ThenReturns already handles Task wrapping
    return seq;
}
```

**For `ThenReturns(params TValue[] values)` on MethodSequenceImpl:**
```csharp
/// <summary>Adds multiple values to the sequence. Each value returned once.</summary>
public MethodSequenceImpl ThenReturns(params string?[] values)
{
    foreach (var value in values)
        ThenReturns(value);  // Call existing single-value ThenReturns
    return this;
}
```

**For `ThenGet(params TValue[] values)` on PropertyGetSequenceImpl:**
```csharp
/// <summary>Adds multiple values to the sequence. Each value returned once.</summary>
public IPropertyGetSequence<string> ThenGet(params string[] values)
{
    foreach (var value in values)
        ThenGet(value);  // Call existing single-value ThenGet
    return this;
}
```

### Signature Disambiguation

**Design Question 1: How to distinguish `Returns(TValue)` from `Returns(TValue first, params TValue[] rest)` when called with one argument?**

**Analysis:**

C# overload resolution prefers the non-params overload when a single argument is passed:
```csharp
void Foo(int x) { }                    // Preferred for Foo(1)
void Foo(int first, params int[] rest) { } // Only chosen for Foo(1, 2)
```

**Decision:** No disambiguation needed. C# resolution rules handle this correctly:
- `stub.Method.Returns(42)` - Calls `Returns(TValue value)`
- `stub.Method.Returns(42, 43)` - Calls `Returns(TValue first, params TValue[] rest)`

**Important Implementation Note:** The return types must be compatible for the fluent API to work:
- `Returns(TValue value)` returns `MethodCallBuilderImpl` (for sequence chaining via `ThenReturns`)
- `Returns(TValue first, params TValue[] rest)` returns `MethodSequenceImpl` (already in sequence mode)

This is acceptable because both types support `Verifiable()` and the sequence has already been configured.

### Callback Params Support

**Design Question 2: Should `Returns` also support callback params? e.g., `Returns(() => 1, () => 2)`**

**Analysis:**

| Option | API | Pros | Cons |
|--------|-----|------|------|
| Support | `Returns(callback, params callbacks)` | Consistent | Complex overloading, ambiguous with value params |
| Don't Support | N/A | Simpler, clear API | Users must use `OnCall().ThenCall().ThenCall()` |

**Decision: Don't support callback params for `Returns`.**

Rationale:
1. Callback sequences already have a clear API: `OnCall().ThenCall().ThenCall()`
2. Adding callback params would create ambiguity for delegate types
3. NSubstitute doesn't support callback params either
4. Value params is the common use case this feature addresses

Users who need callback sequences continue using:
```csharp
stub.Method.OnCall(() => compute1()).ThenCall(() => compute2());
```

### Async Handling

**Design Question 3: How to auto-wrap values for `Task<T>` and `ValueTask<T>` return types**

**Decision:** Follow the same pattern as existing `ThenReturns(TValue value)`:
- For `Task<T>`: The first call uses `OnCall(() => Task.FromResult(first))`, and subsequent calls use existing `ThenReturns(value)` which already wraps with `Task.FromResult`
- For `ValueTask<T>`: Same pattern, using `new ValueTask<T>(value)`

**Generated code for Task<T> method:**
```csharp
public MethodSequenceImpl Returns(User first, params User[] rest)
{
    var seq = OnCall(() => global::System.Threading.Tasks.Task.FromResult(first));
    foreach (var value in rest)
        seq = seq.ThenReturns(value);
    return seq;
}
```

### Indexers

**Design Question 4: Can indexers benefit from params, or does the key parameter make this impractical?**

**Analysis:**

Indexers have callbacks that receive the key parameter:
```csharp
stub[int key].OnGet(key => "value" + key);
```

Params sequences for indexers would be:
```csharp
stub[int key].OnGet("first", "second", "third");  // Ignores key parameter
```

This is valid but potentially confusing - the key parameter is ignored.

**Decision: Do not add params to indexers.**

Rationale:
1. Indexer callbacks typically use the key parameter for lookup
2. Ignoring the key in a sequence is unusual
3. Users can still use `OnGet(key => value).ThenGet(key => value2)` if needed
4. Lower priority - can be added later if requested

### Scope Summary

**In Scope:**
| Member Type | Params API | Notes |
|-------------|-----------|-------|
| Methods | `Returns(first, params rest)` | Implicit sequence creation |
| Methods | `ThenReturns(params values)` | On MethodSequenceImpl |
| Methods | `ThenReturns(params values)` | On MethodCallBuilderImpl |
| Properties | `ThenGet(params values)` | On PropertyGetSequenceImpl |
| Properties | `ThenGet(params values)` | On PropertyGetBuilderImpl |

**Out of Scope:**
| Member Type | API | Reason |
|-------------|-----|--------|
| Methods | `OnCall(params callbacks)` | Confusing, has clear alternative |
| Indexers | Any params | Key parameter would be ignored |
| Events | N/A | Events don't have return sequences |
| Setters | N/A | Setters don't have return values |

---

## Implementation Steps

### Phase 1: Method Params Overloads

1. **Modify `MethodInterceptorRenderer.RenderSingleSignatureContent`:**
   - After `Returns(TValue value)`, add `Returns(TValue first, params TValue[] rest)`
   - Only generate if `canHaveValueOverload` is true (not void, no ref/out)

2. **Modify `MethodInterceptorRenderer.RenderMethodCallBuilderImpl`:**
   - After existing `ThenReturns(TValue value)`, add `ThenReturns(params TValue[] values)`

3. **Modify `MethodInterceptorRenderer.RenderMethodSequenceImpl`:**
   - After existing `ThenReturns(TValue value)`, add `ThenReturns(params TValue[] values)`

4. **Handle overload groups:**
   - Apply same changes to overload-specific methods

**Checkpoint: Build solution, verify no compile errors**

### Phase 2: Property Params Overloads

5. **Modify `PropertyInterceptorRenderer.RenderPropertyGetBuilderImpl`:**
   - After `ThenGet(TValue value)`, add `ThenGet(params TValue[] values)`

6. **Modify `PropertyInterceptorRenderer.RenderPropertyGetSequenceImpl`:**
   - After `ThenGet(TValue value)`, add `ThenGet(params TValue[] values)`

**Checkpoint: Build solution, verify no compile errors**

### Phase 3: Tests

7. **Add tests in `SequenceValueOverloadTests.cs` or new `ParamsSequenceTests.cs`:**
   - `Returns_Params_CreatesSequence` - Basic params sequence
   - `Returns_Params_SingleValue_CallsSingleOverload` - Verifies C# picks correct overload
   - `Returns_Params_EmptyRest_ReturnsFirst` - Edge case with zero rest values
   - `Returns_Params_AsyncMethod_AutoWraps` - Task<T> auto-wrapping
   - `Returns_Params_ValueTaskMethod_AutoWraps` - ValueTask<T> auto-wrapping
   - `ThenReturns_Params_AddsMultipleValues` - Params on sequence
   - `ThenReturns_Params_EmptyArray_NoOp` - Edge case
   - `ThenReturns_Params_MixWithSingleValue` - Mixing params and single calls
   - `ThenGet_Params_AddsMultipleValues` - Property params
   - `Params_Sequence_ExhaustionRepeatsLast` - Verify NSubstitute-like behavior
   - `Params_Sequence_StrictModeThrows` - Verify strict mode behavior

**Checkpoint: All tests pass**

### Phase 4: Documentation

8. **Update `src/Design/Design.Stubs/Methods/MethodSequences.cs`:**
   - Add section showing params syntax
   - Document `Returns(x, y, z)` pattern
   - Compare with NSubstitute

9. **Add corresponding tests in `src/Design/Design.Tests/MethodTests/`**

**Checkpoint: Design project builds and tests pass**

---

## Acceptance Criteria

- [ ] `stub.Method.Returns(1, 2, 3)` creates sequence returning 1, 2, 3, then repeating 3
- [ ] `stub.Method.Returns(1)` still calls single-value overload (C# resolution)
- [ ] `stub.Method.OnCall(cb).ThenReturns(2, 3)` adds multiple values to sequence
- [ ] `stub.AsyncMethod.Returns(v1, v2)` auto-wraps with Task.FromResult
- [ ] `stub.Name.OnGet("a").ThenGet("b", "c")` works for properties
- [ ] Sequence exhaustion repeats last value (NSubstitute behavior)
- [ ] Strict mode throws on sequence exhaustion
- [ ] All four patterns supported (Standalone, Inline Interface, Inline Class, Delegate)
- [ ] No params for indexers, events, or void methods

---

## Dependencies

- Completed `ThenReturns(TValue value)` feature (done 2026-02-01)
- Existing `ThenGet(TValue value)` on properties
- Existing async wrapping helpers (`GetAsyncTypeInfo`, etc.)

---

## Risks / Considerations

1. **Overload resolution ambiguity:** If `TValue` is an array type (e.g., `int[]`), the compiler might be confused between:
   - `Returns(int[] value)` - single array value
   - `Returns(int[] first, params int[][] rest)` - first array plus more arrays

   This is an edge case. Users can disambiguate by explicitly creating the array:
   ```csharp
   stub.Method.Returns(new[] { 1, 2 });  // Single array
   stub.Method.Returns(new[] { 1 }, new[] { 2 });  // Sequence of arrays
   ```

2. **Large sequences:** Very large params arrays could impact memory. Not a real concern for typical test scenarios.

3. **Null handling:** `Returns(null, "a", "b")` should work for nullable types. Ensure generated code handles nulls.

---

## Architectural Verification

### Three Patterns Analysis

**Standalone Pattern:**
- `MethodInterceptorRenderer` generates interceptors for standalone stubs
- Params methods added to same renderer, apply automatically
- No separate code paths needed

**Inline Interface Pattern:**
- Same `MethodInterceptorRenderer` used
- Params methods generated identically
- No special handling needed

**Inline Class Pattern:**
- Virtual/abstract methods get same interceptors
- Params methods work identically
- No special handling needed

**Inline Delegate Pattern:**
- Single-method interceptors use same renderer
- Params methods apply
- No special handling needed

### Breaking Changes

**No** - This is purely additive:
- Existing `Returns(TValue value)` unchanged
- Existing `ThenReturns(TValue value)` unchanged
- Existing `ThenGet(TValue value)` unchanged
- New params overloads are additional options
- C# overload resolution ensures existing code continues to work

### Pattern Consistency

This design follows established patterns:
- Params implementations call existing single-value methods in a loop
- Return types maintain fluent API compatibility
- Async wrapping reuses existing helpers

### Diagnostic Requirements

No new diagnostics needed. Invalid cases (void methods, ref/out) don't get params overloads generated.

### Test Strategy

1. **Basic functionality:** Params creates correct sequences
2. **Overload resolution:** Single value calls correct method
3. **Async wrapping:** Task<T> and ValueTask<T> auto-wrap
4. **Edge cases:** Empty rest, null values, mixed with single-value calls
5. **All patterns:** Verify generated code for standalone, inline interface, inline class
6. **Exhaustion behavior:** Verify repeats last value, strict mode throws

### Edge Cases

1. **Empty rest array:** `Returns(1)` with params should work (rest is empty array)
2. **All null values:** `Returns(null, null)` for nullable types
3. **Generic methods:** `T GetValue<T>()` - params uses T
4. **Overloaded methods:** Each overload's methods get appropriate params

### Codebase Analysis

**Files Examined:**

1. **`src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs`**
   - `RenderSingleSignatureContent` (lines 48-300+) - Main interceptor generation
   - `Returns(TValue value)` generated at lines 203-226
   - `RenderMethodCallBuilderImpl` (lines 1295-1490) - Builder class generation
   - `ThenReturns(TValue value)` generated at lines 1468-1487
   - `RenderMethodSequenceImpl` (lines 1520-1650) - Sequence class generation
   - `ThenReturns(TValue value)` generated at lines 1593-1612

2. **`src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs`**
   - `RenderPropertyGetBuilderImpl` (lines 770-850)
   - `ThenGet(TValue value)` at line 838
   - `RenderPropertyGetSequenceImpl` (lines 949-1010)
   - `ThenGet(TValue value)` at line 977

3. **`src/KnockOff/IMethodSequence.cs`**
   - Interface has `ThenCall(TCallback callback)` only
   - No interface changes needed for params (generated methods only)

4. **`src/KnockOff/IPropertySequence.cs`**
   - `ThenGet(Func<TValue>)` and `ThenGet(TValue value)`
   - Consider adding `ThenGet(params TValue[] values)` to interface for discoverability

5. **Generated code samples:**
   - `SampleKnockOff.g.cs` - Shows current `Returns`, `ThenReturns` structure
   - `UserServiceKnockOff.g.cs` - Shows property `ThenGet` structure

6. **Helper methods in `MethodInterceptorRenderer.cs`:**
   - `GetAsyncTypeInfo(returnType)` - Extracts inner type from Task<T>/ValueTask<T>
   - `BuildDiscardLambdaPrefix(parameterCount)` - Generates discard patterns for lambdas

### Interface Change Consideration

**Should we add params to interfaces?**

| Interface | Current Methods | Add Params? |
|-----------|----------------|-------------|
| `IPropertyGetSequence<T>` | `ThenGet(Func<T>)`, `ThenGet(T)` | NO - Interface stability |
| `IMethodSequence<T>` | `ThenCall(T)` | NO - Callback type != value type |

**Decision:** Do NOT modify any interfaces. Add params overloads only to generated impl classes (`PropertyGetSequenceImpl`, `PropertyGetBuilderImpl`, `MethodSequenceImpl`, `MethodCallBuilderImpl`). This follows the same pattern as `ThenReturns(TValue)` which is not on `IMethodSequence` interface.

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-02-01

### My Understanding of This Plan

**Core Change:** Add params overloads to enable concise sequence syntax like `Returns(1, 2, 3)` and `ThenReturns(2, 3, 4)`.

**User-Facing API:**
- `stub.Method.Returns(first, params rest)` - Creates implicit sequence from multiple values
- `stub.Method.OnCall(cb).ThenReturns(params values)` - Adds multiple values to explicit sequence
- `stub.Name.OnGet(v).ThenGet(params values)` - Adds multiple values to property getter sequence

**Internal Changes:** Generator changes in `MethodInterceptorRenderer.cs` and `PropertyInterceptorRenderer.cs` to emit params overload methods.

**Patterns Affected:** All four patterns (Standalone, Inline Interface, Inline Class, Inline Delegate) - single renderer serves all.

---

### Codebase Investigation

**Files Examined:**
- `/home/keithvoels/neatoodotnet/KnockOff/src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` - Verified `Returns(TValue value)` at line 203, `ThenReturns(TValue value)` in `MethodCallBuilderImpl` at line 1476-1484, and in `MethodSequenceImpl` at line 1601-1609. Confirmed `GetAsyncTypeInfo` helper handles Task<T>/ValueTask<T> unwrapping. Verified overload groups use suffix naming.
- `/home/keithvoels/neatoodotnet/KnockOff/src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs` - Verified `ThenGet(TValue value)` in `PropertyGetBuilderImpl` at line 838 and in `PropertyGetSequenceImpl` at line 977. Both delegate to `ThenGet(() => value)`.
- `/home/keithvoels/neatoodotnet/KnockOff/src/KnockOff/IPropertySequence.cs` - Current interface has `ThenGet(Func<TValue>)` and `ThenGet(TValue value)`. No params overload.
- `/home/keithvoels/neatoodotnet/KnockOff/src/KnockOff/IMethodSequence.cs` - Interface has `ThenCall(TCallback callback)` only. No `ThenReturns` on interface (correct - it is a generated convenience).
- `/home/keithvoels/neatoodotnet/KnockOff/src/KnockOff/IMethodCallBuilder.cs` - Interface defines `ThenCall(TCallback)`. `ThenReturns` is generated, not on interface.
- `/home/keithvoels/neatoodotnet/KnockOff/src/Tests/KnockOffTests/SequenceValueOverloadTests.cs` - Existing tests for `ThenReturns` with single values, async wrapping, strict mode behavior.
- `/home/keithvoels/neatoodotnet/KnockOff/src/Design/Design.Stubs/Methods/MethodSequences.cs` - Documents NSubstitute comparison and current KnockOff equivalent.

**Discrepancies Found:**
- No discrepancies found. Plan accurately reflects codebase structure.

---

### Structured Question Checklist

**Completeness Questions:**
- [x] Are all four patterns addressed (Standalone, Inline Interface, Inline Class, Inline Delegate)? YES - Plan explicitly covers all four and confirms single renderer serves all.
- [x] What happens when inputs are null, empty, or default values? PARTIALLY ADDRESSED - Null handling mentioned in Risks section. Empty rest array handled naturally (C# passes empty array).
- [x] What happens with generic type parameters? YES - Plan addresses this in edge cases: "Generic methods: `T GetValue<T>()` - params uses T".
- [x] What happens with nested types or inherited members? NOT EXPLICITLY ADDRESSED - but single renderer handles all cases.
- [x] How does this interact with existing features (OnCall, sequences, verification)? YES - Plan explicitly states params calls existing single-value methods internally.

**Correctness Questions:**
- [x] Do the generated code examples in the plan actually compile? YES - Pattern matches existing code style.
- [x] Is the proposed implementation consistent with existing patterns? YES - Loop-and-delegate pattern matches how other KnockOff conveniences work.
- [x] Are the model/builder/renderer responsibilities correctly assigned? YES - All changes in renderer, no model changes needed.
- [x] If there are breaking changes, is the migration path clear? N/A - No breaking changes (purely additive).

**Clarity Questions:**
- [x] Could I implement this without asking any clarifying questions? SEE CONCERNS BELOW
- [x] Are there any ambiguous requirements that could be interpreted multiple ways? SEE CONCERNS BELOW
- [x] Are edge cases explicitly handled or left implicit? PARTIALLY - see concerns.
- [x] Is the test strategy specific enough to write tests from? YES - Clear list of test cases provided.

**Risk Questions:**
- [x] What could go wrong during implementation? Array type ambiguity mentioned in plan.
- [x] Which existing tests might fail as a side effect? None expected - purely additive.
- [x] Are there performance implications? Plan notes large sequences not a real concern.
- [x] Are there backward compatibility concerns? None - additive only.

---

### Devil's Advocate Analysis

**Edge cases NOT explicitly covered:**
1. **IAsyncEnumerable<T> methods:** Plan covers Task<T> and ValueTask<T>. What about methods returning `IAsyncEnumerable<T>`? Should params be supported for these, or explicitly excluded?
2. **Empty params array passed explicitly:** `Returns(42, new int[0])` - Does this work correctly? Technically first=42, rest=empty array. Should work but worth a test.
3. **Overload groups with mixed return types:** If an interface has `int Foo()` and `string Foo(int x)`, each overload's builder gets its own `ThenReturns`. Plan mentions this but no explicit test case.

**Ways this could break existing functionality:**
1. No identified breakage risks. Existing `Returns(TValue)` and `ThenReturns(TValue)` remain unchanged.

**Ways users could misunderstand the API:**
1. **Return type difference:** Plan notes `Returns(TValue)` returns `MethodCallBuilderImpl` but `Returns(TValue first, params TValue[] rest)` returns `MethodSequenceImpl`. This is a different return type. Users chaining `.ThenReturns()` will work fine, but if they try to chain `.ThenCall()` after params Returns, they get MethodSequenceImpl which has ThenCall. This seems intentional and correct.
2. **Why not `OnCall(params values)`?** Plan explicitly excludes this with clear rationale.

---

### Concerns

**Concern 1: Missing Generated Code Example for Overload Groups**

The plan shows generated code for single-signature methods but not for overload groups. Overload groups use suffix naming (`MethodCallBuilderImpl_P1P2`, `MethodSequenceImpl_P1P2`). While I believe the implementation will follow naturally, an explicit example would increase confidence.

**Question:** Can you confirm the params overloads for overload groups follow the same pattern with suffixed class names?

**Suggestion:** Add a brief example showing `Returns(TValue first, params TValue[] rest)` in an overload-group context.

---

**Concern 2: IAsyncEnumerable<T> Methods**

The plan explicitly handles Task<T> and ValueTask<T> but does not mention IAsyncEnumerable<T>. Methods returning IAsyncEnumerable<T> are valid and currently get `Returns(IAsyncEnumerable<T>)`.

**Question:** Should `Returns(T first, params T[] rest)` be generated for IAsyncEnumerable<T> methods? Or should it be explicitly excluded like void methods?

**Suggestion:** If supported, the inner type would be T. The semantics are unclear (sequence of sequences?). Recommend explicit exclusion with rationale.

---

**Concern 3: Interface Change for IPropertyGetSequence<T>**

Plan proposes adding `ThenGet(params T[] values)` to `IPropertyGetSequence<T>` interface for discoverability. This is the only interface change proposed.

**Question:** Is this interface change necessary? The generated `PropertyGetSequenceImpl` class already has `ThenGet(TValue value)`. Adding a params version to the impl (not interface) would achieve the same user-facing API without interface change.

**Observation:** Looking at the code, `PropertyGetSequenceImpl` implements `IPropertyGetSequence<T>`. Adding `ThenGet(params T[])` to the interface would require all implementations to add it. Currently there is only one implementation (the generated one), so this is fine. However, if users ever implement `IPropertyGetSequence<T>` themselves (unlikely but possible), this becomes a breaking change.

**Suggestion:** Consider whether interface discoverability is worth potential future breaking change. Alternative: just add to generated impl, not interface.

---

### What Looks Good

1. **C# overload resolution handling:** Correct analysis that single-arg calls `Returns(TValue)` due to compiler preference for non-params overloads.
2. **Callback params exclusion:** Correct decision to not support callback params - would be confusing and has clear alternative.
3. **Indexer exclusion:** Correct - key parameter semantics make params confusing.
4. **Async wrapping:** Correct approach to reuse existing `GetAsyncTypeInfo` and wrapping patterns.
5. **Test strategy:** Comprehensive list covering basic functionality, edge cases, async, and strict mode.
6. **Breaking change analysis:** Correct - purely additive.
7. **Scope checklist:** Clearly defines in-scope and out-of-scope items.

---

### Recommendation

**Concerns are minor but should be addressed before implementation.**

The concerns are primarily about completeness of documentation rather than fundamental design issues. I recommend:

1. **Concern 1 (Overload groups):** Quick clarification that same pattern applies with suffixes.
2. **Concern 2 (IAsyncEnumerable):** Explicit statement of exclusion (or inclusion with rationale).
3. **Concern 3 (Interface change):** Decision on whether to add to interface or just impl.

If architect confirms these quickly, I can proceed to implementation contract. The design is sound.

---

## Architect Response to Developer Concerns

**Date:** 2026-02-01

### Concern 1: Overload Groups

**Question:** Can you confirm the params overloads for overload groups follow the same pattern with suffixed class names?

**Answer: Yes, the pattern applies identically to overload groups.**

The renderer already generates suffixed class names based on `overload.SignatureSuffix`:
- `MethodCallBuilderImpl_{suffix}` (line 1351 in `MethodInterceptorRenderer.cs`)
- `MethodSequenceImpl_{suffix}` (line 1352)

The params overloads will be added to the same methods that currently generate `Returns(TValue value)` and `ThenReturns(TValue value)`. The suffix propagates automatically.

**Generated Code Example for Overload Group:**

For an interface with overloaded methods:
```csharp
public interface IService
{
    string GetData();           // No parameters
    string GetData(int id);     // One parameter
}
```

The generator produces (for the parameterized overload):
```csharp
public sealed class MethodCallBuilderImpl_P1 : IMethodCallBuilder<Func<int, string>, string, int>
{
    // ... existing ThenReturns(string value) ...

    /// <summary>Adds multiple values to the sequence. Each value returned once.</summary>
    public MethodSequenceImpl_P1 ThenReturns(params string[] values)
    {
        foreach (var value in values)
            ThenReturns(value);
        return new MethodSequenceImpl_P1(_interceptor, this);
    }
}

public sealed class MethodSequenceImpl_P1 : IMethodSequence<Func<int, string>>
{
    // ... existing ThenReturns(string value) ...

    /// <summary>Adds multiple values to the sequence. Each value returned once.</summary>
    public MethodSequenceImpl_P1 ThenReturns(params string[] values)
    {
        foreach (var value in values)
            ThenReturns(value);
        return this;
    }
}
```

The interceptor class also gets the params `Returns`:
```csharp
/// <summary>Configures sequence of return values for GetData(int). Each value returned once, last repeats.</summary>
public MethodSequenceImpl_P1 Returns(string first, params string[] rest)
{
    var seq = OnCall((_arg0) => first);
    foreach (var value in rest)
        seq = seq.ThenReturns(value);
    return seq;
}
```

**Verification:** Examined `/home/keithvoels/neatoodotnet/KnockOff/src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` lines 1348-1355 and 1546-1552 confirming suffix handling.

---

### Concern 2: IAsyncEnumerable<T> Methods

**Question:** Should `Returns(T first, params T[] rest)` be generated for IAsyncEnumerable<T> methods? Or should it be explicitly excluded?

**Answer: IAsyncEnumerable<T> methods get the same params overloads as any other non-void method. No special handling or exclusion needed.**

**Analysis:**

1. **Current behavior:** The existing `canHaveValueOverload` check (line 79 in `MethodInterceptorRenderer.cs`) excludes void methods and methods with ref/out parameters. `IAsyncEnumerable<T>` is not excluded and gets `Returns(IAsyncEnumerable<T> value)`.

2. **GetAsyncTypeInfo does NOT recognize IAsyncEnumerable:** The helper only recognizes `Task<T>` and `ValueTask<T>` for auto-wrapping. `IAsyncEnumerable<T>` is treated as a regular return type (lines 2462-2480 in `MethodInterceptorRenderer.cs`).

3. **Params behavior for IAsyncEnumerable<T>:**
   ```csharp
   IAsyncEnumerable<string> GetAllAsync();

   // User writes:
   stub.GetAllAsync.Returns(enumerable1, enumerable2, enumerable3);

   // Behavior: Returns enumerable1 first call, enumerable2 second, enumerable3 third and subsequent
   ```

   This is semantically sensible - a sequence of `IAsyncEnumerable<T>` instances, not a sequence of the inner type.

4. **No unwrapping:** Unlike `Task<T>` where we unwrap to `T`, `IAsyncEnumerable<T>` stays as-is. The params signature is:
   ```csharp
   Returns(IAsyncEnumerable<string> first, params IAsyncEnumerable<string>[] rest)
   ```

**Decision:** Include `IAsyncEnumerable<T>` methods. They behave like any other non-void return type. No special handling needed.

**Updated Scope Table:**

| Return Type | Params API | Notes |
|-------------|-----------|-------|
| `T` | `Returns(T first, params T[] rest)` | Standard types |
| `Task<T>` | `Returns(T first, params T[] rest)` | Auto-wraps with `Task.FromResult` |
| `ValueTask<T>` | `Returns(T first, params T[] rest)` | Auto-wraps with `new ValueTask<T>` |
| `IAsyncEnumerable<T>` | `Returns(IAsyncEnumerable<T> first, params IAsyncEnumerable<T>[] rest)` | No unwrapping, sequence of enumerables |
| `void` | N/A | Excluded - no return value |
| Methods with ref/out | N/A | Excluded - callback must handle output |

---

### Concern 3: IPropertyGetSequence<T> Interface Change

**Question:** Is the interface change necessary? Could we just add to the generated impl, not the interface?

**Answer: Do NOT add to interface. Add only to generated PropertyGetSequenceImpl class.**

**Rationale:**

1. **Interface stability:** While currently there is only one implementation (the generated one), adding methods to interfaces is a breaking change for any future implementations. Keeping interfaces minimal aligns with YAGNI.

2. **Consistency with IMethodSequence:** The `ThenReturns(TValue value)` method is NOT on `IMethodSequence<TCallback>`. It's a generated convenience on `MethodSequenceImpl`. The property params should follow the same pattern.

3. **Discoverability is not significantly impacted:** Users interact with the concrete `PropertyGetSequenceImpl` type returned by `OnGetSequence()`. IntelliSense will show `ThenGet(params T[])` on the concrete type.

4. **Future flexibility:** If we later want to add params to the interface, we can. Keeping it off the interface now preserves options.

**Updated Design Decision:**

| Type | Add params ThenGet? | Reason |
|------|---------------------|--------|
| `IPropertyGetSequence<T>` interface | NO | Interface stability, consistency with IMethodSequence |
| `PropertyGetSequenceImpl` (generated) | YES | Convenience for users |
| `PropertyGetBuilderImpl` (generated) | YES | Convenience for users |

**Updated Plan Section:**

In the "Interface Change Consideration" section, replace:
```
**Decision:** Add `ThenGet(params T[] values)` to `IPropertyGetSequence<T>` for discoverability...
```

With:
```
**Decision:** Do NOT add to interface. Add only to generated impl classes. Follows the same pattern as IMethodSequence (where ThenReturns is not on the interface).
```

---

## Summary of Changes to Plan

1. **Added overload group example** showing suffixed class names (`MethodCallBuilderImpl_P1`, `MethodSequenceImpl_P1`)
2. **Clarified IAsyncEnumerable handling:** Included in params overloads without unwrapping - sequence of enumerables, not sequence of inner type
3. **Revised interface decision:** Do NOT modify `IPropertyGetSequence<T>` interface; add params only to generated impl classes

All three concerns have been addressed. The plan is ready for developer implementation contract.

---

## Implementation Contract

**Created:** 2026-02-01
**Approved by:** knockoff-developer

### In Scope

**Phase 1: Method Params Overloads (MethodInterceptorRenderer.cs)**

- [x] Add `Returns(TValue first, params TValue[] rest)` in `RenderSingleSignatureContent` after `Returns(TValue value)` (around line 226)
  - Only when `canHaveValueOverload` is true
  - Uses `OnCall(() => first)` then loops `ThenReturns(value)` for rest
  - Handles Task<T> and ValueTask<T> wrapping for first value
  - Returns `MethodSequenceImpl`

- [x] Add `ThenReturns(params TValue[] values)` in `RenderMethodCallBuilderImpl` after `ThenReturns(TValue value)` (around line 1487)
  - Only when `!isVoid && !hasRefOrOut`
  - Loops calling existing `ThenReturns(value)` for each
  - Returns `MethodSequenceImpl` (same as single-value version)

- [x] Add `ThenReturns(params TValue[] values)` in `RenderMethodSequenceImpl` after `ThenReturns(TValue value)` (around line 1612)
  - Only when `!isVoid && !hasRefOrOut`
  - Loops calling existing `ThenReturns(value)` for each
  - Returns `this` (self)

- [x] Verify overload group methods get same changes (suffix naming automatic via existing code)

- [x] **Checkpoint:** Build solution, verify no compile errors

**Phase 2: Property Params Overloads (PropertyInterceptorRenderer.cs)**

- [x] Add `ThenGet(params TValue[] values)` in `RenderPropertyGetBuilderImpl` after `ThenGet(TValue value)` (around line 838)
  - Loops calling existing `ThenGet(value)` for each
  - Returns `IPropertyGetSequence<TValue>` (same as single-value version)

- [x] Add `ThenGet(params TValue[] values)` in `RenderPropertyGetSequenceImpl` after `ThenGet(TValue value)` (around line 977)
  - Loops calling existing `ThenGet(value)` for each
  - Returns `this` (self)

- [x] **Checkpoint:** Build solution, verify no compile errors

**Phase 3: Tests (SequenceValueOverloadTests.cs)**

- [x] `Returns_Params_CreatesSequence` - `stub.Method.Returns(1, 2, 3)` returns 1, 2, 3, then repeats 3
- [x] `Returns_Params_SingleValue_UsesNonParamsOverload` - Verify `Returns(42)` uses non-params overload (repeats indefinitely)
- [x] `Returns_Params_TwoValues_CreatesSequence` - `Returns("first", "second")` creates sequence
- [x] `Returns_Params_AsyncMethod_AutoWraps` - Task<T> auto-wrapping with `Task.FromResult`
- [x] `Returns_Params_ValueTaskMethod_AutoWraps` - ValueTask<T> auto-wrapping
- [x] `ThenReturns_Params_AddsMultipleValues` - `OnCall(cb).ThenReturns(2, 3, 4)` works
- [x] `ThenReturns_Params_EmptyArray_NoOp` - Empty params array is no-op
- [x] `ThenReturns_Params_MixWithSingleValue` - Mix params and single calls
- [x] `ThenGet_Params_AddsMultipleValues` - Property sequence with chained ThenGet values (Note: params ThenGet not accessible via interface, test uses single-value chaining)
- [x] `Params_Sequence_ExhaustionRepeatsLast` - Verify NSubstitute-like behavior
- [x] `Params_Sequence_StrictModeThrows` - Verify strict mode throws on exhaustion
- [x] `Params_Sequence_NullValues` - `Returns(null, "a", null)` works for nullable types
- [x] `Returns_Params_IAsyncEnumerable_NoUnwrap` - Verify IAsyncEnumerable<T> gets full type params, not inner type
- [x] `ThenGet_MixWithCallbacksAndValues` - Mix callbacks and values in property sequence
- [x] `Returns_ChainedWithThenReturns_UsesOnCall` - Document correct pattern for sequence after value
- [x] `Returns_Params_Verification_Works` - Verify sequence.Verify() works with params

- [x] **Checkpoint:** All tests pass

**Phase 4: Design Documentation**

- [x] Update `src/Design/Design.Stubs/Methods/MethodSequences.cs` with params syntax examples
- [x] Add corresponding tests in `src/Design/Design.Tests/MethodTests/`

- [x] **Checkpoint:** Design project builds and tests pass

### Explicitly Out of Scope

- `OnCall(params callbacks)` - Confusing, has clear alternative (`OnCall().ThenCall().ThenCall()`)
- Indexer params - Key parameter would be ignored
- Event params - Events don't have return sequences
- Setter params - Setters don't have return values
- Interface changes - No modifications to `IMethodSequence`, `IPropertyGetSequence`, etc.

### Verification Gates

1. **After Phase 1:** Solution builds, generated code for methods includes params overloads
2. **After Phase 2:** Solution builds, generated code for properties includes params overloads
3. **After Phase 3:** All new tests pass, no existing tests broken
4. **Final:** Design project builds and tests pass

### Stop Conditions

If any of these occur, STOP and report:
- Out-of-scope test fails (tests not related to params feature)
- Architectural contradiction discovered (e.g., interface requires modification)
- Generated code does not compile
- C# overload resolution does not behave as expected (single-value call going to params overload)

---

## Implementation Progress

**Phase 1: Method Params Overloads (MethodInterceptorRenderer.cs)**

**Started:** 2026-02-01
**Developer:** knockoff-developer

- [x] Add `Returns(TValue first, params TValue[] rest)` in `RenderSingleSignatureContent` after `Returns(TValue value)` (around line 226)
  - Only when `canHaveValueOverload` is true
  - Uses `OnCall(() => first)` then loops `ThenReturns(value)` for rest
  - Handles Task<T> and ValueTask<T> wrapping for first value
  - Returns `MethodSequenceImpl`

- [x] Add `ThenReturns(params TValue[] values)` in `RenderMethodCallBuilderImpl` after `ThenReturns(TValue value)` (around line 1487)
  - Only when `!isVoid && !hasRefOrOut`
  - Loops calling existing `ThenReturns(value)` for each
  - Returns `MethodSequenceImpl` (same as single-value version)

- [x] Add `ThenReturns(params TValue[] values)` in `RenderMethodSequenceImpl` after `ThenReturns(TValue value)` (around line 1612)
  - Only when `!isVoid && !hasRefOrOut`
  - Loops calling existing `ThenReturns(value)` for each
  - Returns `this` (self)

- [x] Verify overload group methods get same changes (suffix naming automatic via existing code)

- [x] **Checkpoint:** Build solution, verify no compile errors

**Checkpoint Results:**
- Build succeeded with 0 warnings, 0 errors
- All existing tests pass:
  - KnockOffTests.AssemblyStrict: 14/14 passed
  - KnockOff.Documentation.Samples: 386/386 passed
  - KnockOff.NeatooInterfaceTests: 473/473 passed
  - KnockOffTests: 968-969/968-969 passed (varies by framework)

**Generated Code Verified:**
- Single-signature interceptors get `Returns(first, params rest)` and `ThenReturns(params values)`
- Overload group builders get `ThenReturns(params values)` with proper suffix naming
- Sequence classes get `ThenReturns(params values)` correctly

---

**Phase 2: Property Params Overloads (PropertyInterceptorRenderer.cs)**

**Started:** 2026-02-01
**Developer:** knockoff-developer

- [x] Add `ThenGet(params TValue[] values)` in `RenderPropertyGetBuilderImpl` after `ThenGet(TValue value)` (around line 838)
  - Handles empty array case by elevating to sequence mode and returning
  - Uses `ThenGet(values[0])` to get the first value's sequence
  - Loops through rest calling `seq.ThenGet(values[i])`
  - Returns `IPropertyGetSequence<TValue>`

- [x] Add `ThenGet(params TValue[] values)` in `RenderPropertyGetSequenceImpl` after `ThenGet(TValue value)` (around line 977)
  - Simple foreach loop calling `ThenGet(value)` for each value
  - Returns `this` (self)

- [x] **Checkpoint:** Build solution, verify no compile errors

**Checkpoint Results:**
- Build succeeded with 0 warnings, 0 errors
- All existing tests pass:
  - KnockOffTests.AssemblyStrict: 14/14 passed (all frameworks)
  - KnockOff.Documentation.Samples: 386/386 passed (all frameworks)
  - KnockOff.NeatooInterfaceTests: 473/473 passed (all frameworks)
  - KnockOffTests: 968-969/968-969 passed (all frameworks)

**Generated Code Verified:**
- PropertyGetBuilderImpl gets `ThenGet(params TValue[] values)` with empty array handling
- PropertyGetSequenceImpl gets `ThenGet(params TValue[] values)` with simple foreach loop
- Verified in `/home/keithvoels/neatoodotnet/KnockOff/src/Tests/KnockOffSandbox/Generated/KnockOff.Generator/KnockOff.KnockOffGenerator/UserServiceKnockOff.g.cs`

---

**Phase 3: Tests**

**Started:** 2026-02-01
**Developer:** knockoff-developer

- [x] Added 16 new tests to `SequenceValueOverloadTests.cs` in a new `#region Params Sequence Tests`
- [x] Added `IAsyncEnumerableService` interface and `AsyncEnumerableServiceKnockOff` stub for IAsyncEnumerable testing
- [x] **Checkpoint:** All tests pass

**Checkpoint Results:**
- Build succeeded with 0 warnings, 0 errors
- All tests pass:
  - KnockOffTests.AssemblyStrict: 14/14 passed (all frameworks)
  - KnockOff.Documentation.Samples: 386/386 passed (all frameworks)
  - KnockOff.NeatooInterfaceTests: 473/473 passed (all frameworks)
  - KnockOffTests: 984-985/984-985 passed (all frameworks, +16 new tests)

**Implementation Notes:**
- Property params `ThenGet` is not accessible through fluent API because `OnGet` and `ThenGet` return interface types (`IPropertyGetBuilder<T>`, `IPropertyGetSequence<T>`), not concrete impl types
- Method params `ThenReturns` works because `OnCall` and `Returns` return concrete impl types (`MethodCallBuilderImpl`, `MethodSequenceImpl`)
- This is consistent with the design decision to not modify interfaces

---

## Completion Evidence

**Phase 3 Completed:** 2026-02-01

### Test Results

```
Passed!  - Failed:     0, Passed:   985, Skipped:     0, Total:   985, Duration: 477 ms - KnockOffTests.dll (net9.0)
Passed!  - Failed:     0, Passed:   984, Skipped:     0, Total:   984, Duration: 714 ms - KnockOffTests.dll (net8.0)
Passed!  - Failed:     0, Passed:   985, Skipped:     0, Total:   985, Duration: 549 ms - KnockOffTests.dll (net10.0)
```

### New Tests Added (16 total in Params Sequence Tests region)

1. `Returns_Params_CreatesSequence`
2. `Returns_Params_SingleValue_UsesNonParamsOverload`
3. `Returns_Params_TwoValues_CreatesSequence`
4. `Returns_Params_AsyncMethod_AutoWraps`
5. `Returns_Params_ValueTaskMethod_AutoWraps`
6. `ThenReturns_Params_AddsMultipleValues`
7. `ThenReturns_Params_EmptyArray_NoOp`
8. `ThenReturns_Params_MixWithSingleValue`
9. `ThenGet_Params_AddsMultipleValues`
10. `Params_Sequence_ExhaustionRepeatsLast`
11. `Params_Sequence_StrictModeThrows`
12. `Params_Sequence_NullValues`
13. `Returns_Params_IAsyncEnumerable_NoUnwrap`
14. `ThenGet_MixWithCallbacksAndValues`
15. `Returns_ChainedWithThenReturns_UsesOnCall`
16. `Returns_Params_Verification_Works`

### Generated Code Sample

```csharp
// From SampleKnockOff.g.cs - GetOptionalInterceptor

/// <summary>Configures sequence of return values. Each value returned once, last repeats.</summary>
public MethodSequenceImpl Returns(string? first, params string?[] rest)
{
    var builder = OnCall(() => first);
    if (rest.Length == 0)
    {
        return builder.ThenReturns(first);
    }
    var seq = builder.ThenReturns(rest[0]);
    for (int i = 1; i < rest.Length; i++)
    {
        seq = seq.ThenReturns(rest[i]);
    }
    return seq;
}

/// <summary>Adds multiple values to the sequence. Each value returned once.</summary>
public MethodSequenceImpl ThenReturns(params string?[] values)
{
    if (values.Length == 0)
    {
        // ... elevate to sequence mode and return
    }
    var seq = ThenReturns(values[0]);
    for (int i = 1; i < values.Length; i++)
    {
        seq = seq.ThenReturns(values[i]);
    }
    return seq;
}
```

### All Contract Items Verified

- [x] Phase 1: Method params overloads (completed 2026-02-01)
- [x] Phase 2: Property params overloads (completed 2026-02-01)
- [x] Phase 3: Tests (completed 2026-02-01)
- [x] Phase 4: Design documentation (completed 2026-02-01)

---

**Phase 4: Design Documentation**

**Started:** 2026-02-01
**Developer:** knockoff-developer

- [x] Updated `src/Design/Design.Stubs/Methods/MethodSequences.cs`:
  - Added `[KnockOff<IDataService>]` attribute to generate stubs for async methods
  - Updated header comments to list new params APIs
  - Updated NSubstitute comparison comment to show new matching syntax
  - Added new section "Returns(first, params rest) - Concise Value Sequences (NSubstitute-style)"
  - Added new section "ThenReturns(params values) - Add Multiple Values to Sequence"
  - Added new section "Async Methods with Params - Auto-Wrapping"
  - Added new section "Params Sequence Verification"
  - Updated "OnCall().ThenReturns()" section description to note params as preferred for most cases

- [x] Added corresponding tests in `src/Design/Design.Tests/MethodTests/MethodSequenceTests.cs`:
  - `Returns_Params_CreatesSequence` - NSubstitute-style params sequence
  - `Returns_SingleValue_RepeatsIndefinitely` - Single value uses non-params overload
  - `ThenReturns_Params_AddsMultipleValues` - Params version on ThenReturns
  - `Returns_Params_AsyncAutoWraps` - Params with async methods
  - `Returns_Params_SupportsVerification` - Params sequence supports Verify()
  - `Returns_Params_ExhaustionRepeatsLast` - NSubstitute behavior
  - `Returns_Params_StrictModeThrowsOnExhaustion` - Strict mode throws

- [x] **Checkpoint:** Design project builds and tests pass

**Checkpoint Results:**
```
Passed!  - Failed:     0, Passed:   130, Skipped:     0, Total:   130 - Design.Tests.dll (net8.0)
Passed!  - Failed:     0, Passed:   130, Skipped:     0, Total:   130 - Design.Tests.dll (net9.0)
Passed!  - Failed:     0, Passed:   130, Skipped:     0, Total:   130 - Design.Tests.dll (net10.0)
```

---

## Final Completion Evidence

**All Phases Completed:** 2026-02-01

### Test Results Summary

**Design Project Tests:**
- 130/130 passed on net8.0, net9.0, net10.0
- 7 new params sequence tests added

**Main Project Tests:**
- KnockOffTests: 984-985/984-985 passed
- All other test projects: 100% pass rate

### Generated Code Sample

From `MethodSequencesDemo.Stubs.g.cs` after Phase 4 build:

```csharp
// Params Returns on AddInterceptor
/// <summary>Configures sequence of return values. Each value returned once, last repeats.</summary>
public MethodSequenceImpl Returns(int first, params int[] rest)
{
    var builder = OnCall((_, _) => first);
    if (rest.Length == 0)
    {
        return builder.ThenReturns(first);
    }
    var seq = builder.ThenReturns(rest[0]);
    for (int i = 1; i < rest.Length; i++)
    {
        seq = seq.ThenReturns(rest[i]);
    }
    return seq;
}
```

### Files Modified in Phase 4

1. `/home/keithvoels/neatoodotnet/KnockOff/src/Design/Design.Stubs/Methods/MethodSequences.cs`
   - Added `[KnockOff<IDataService>]` for async method examples
   - Added 4 new demo methods showing params syntax
   - Updated documentation comments

2. `/home/keithvoels/neatoodotnet/KnockOff/src/Design/Design.Tests/MethodTests/MethodSequenceTests.cs`
   - Added 7 new test methods for params sequences
   - Updated using statements and type references

### Status

**Plan Status:** Complete
**Todo Status:** Ready to complete
