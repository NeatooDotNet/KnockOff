# Return(value).ThenReturn() Sequence NRE Bug

**Status:** Complete
**Priority:** Medium
**Created:** 2026-02-07
**Last Updated:** 2026-02-07
**Plan:** Inline (see Plans section below)

---

## Problem

When a method interceptor is configured with the value-based `Return(value)` followed by `.ThenReturn(value)`, the first call in the sequence throws a `NullReferenceException`.

**Reproduction:**

```csharp
stub.GetInternalId
    .Return("first-id")        // value-based Return
    .ThenReturn("second-id");  // triggers sequence creation

service.GetDescription();      // NRE on first call
```

**Root cause:** `Return(string value)` sets `_hasReturnValue = true` and `_returnValue = value`, but sets `_call = null`. When `ThenReturn` elevates to sequence mode, it adds `_interceptor._call!` as the first sequence entry — which is null. On the first invocation, `Invoke()` calls `callback()` on that null delegate.

**Affected code path** (generated interceptor):

```csharp
// Return(string value) — sets _returnValue, nulls _call
public MethodCallBuilderImpl Return(string value)
{
    _call = null;           // ← null
    _callTracking = null;
    _hasReturnValue = true;
    _returnValue = value;
    ...
}

// ThenReturn — reads _call which is null
public MethodSequenceImpl ThenReturn(GetInternalIdDelegate callback)
{
    if (_interceptor._sequence == null)
    {
        _interceptor._sequence = new List<...>();
        _interceptor._sequence.Add((_interceptor._call!, this)); // ← null added!
        ...
    }
    ...
}
```

**Workaround:** Use callback form instead of value form:

```csharp
// Works correctly
stub.GetInternalId
    .Return(() => "first-id")
    .ThenReturn(() => "second-id");
```

## Solution

Fix `ThenReturn` in `MethodCallBuilderImpl` to handle the case where the initial configuration was `Return(value)` instead of `Return(callback)`. When `_call` is null but `_hasReturnValue` is true, the first sequence entry should wrap `_returnValue` in a lambda: `() => _interceptor._returnValue`.

---

## Plans

### Fix Plan: Sequence Elevation NRE in MethodInterceptorRenderer

**Status:** Awaiting Verification
**Date:** 2026-02-07

---

#### Root Cause Confirmation

The root cause analysis in the Problem section is correct, and the bug is broader than initially documented. The investigation revealed THREE Return/Call overloads that set `_call = null` but return `MethodCallBuilderImpl`, making ALL of them vulnerable to NRE when followed by `ThenReturn`/`ThenCall`:

1. **`Return(value)`** -- sets `_hasReturnValue = true`, `_returnValue = value`, `_call = null` (line 208-228 of `MethodInterceptorRenderer.cs`)
2. **`Return(simplifiedCallback)`** -- for `Task<T>`/`ValueTask<T>` methods, sets `_callSimplified = callback`, `_call = null` (line 275-296)
3. **`Call(simplifiedVoidCallback)`** -- for `Task`/`ValueTask` void methods, sets `_callSimplifiedVoid = callback`, `_call = null` (line 305-318)

All three return `MethodCallBuilderImpl`, whose `ThenReturn`/`ThenCall` method unconditionally reads `_interceptor._call!` during sequence elevation (lines 1563 and 1608).

Only `Return(fullCallback)` (line 172-200) and `Call(fullCallback)` set `_call = callback` (non-null), so they are NOT affected.

---

#### Single Fix Location

All nine patterns use the same shared renderer: `MethodInterceptorRenderer.RenderInterceptorClass`. Confirmed by tracing calls from each renderer:

| Renderer | Call Site |
|---|---|
| `FlatRenderer.cs` | Lines 114, 130 |
| `StandaloneClassRenderer.cs` | Line 104 |
| `InlineRenderer.cs` | Lines 163, 1318 |
| `ClassRenderer.cs` | Line 69 |

The fix is in ONE file (`src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs`) and automatically propagates to all nine patterns.

---

#### Files Examined

| File | What Was Learned |
|---|---|
| `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` | Contains the bug. Lines 1563 and 1608 read `_call!` which is null when Return(value), Return(simplifiedCallback), or Call(simplifiedVoidCallback) was used |
| `src/Generator/Renderer/FlatRenderer.cs` | Calls shared `MethodInterceptorRenderer.RenderInterceptorClass` |
| `src/Generator/Renderer/InlineRenderer.cs` | Same -- calls shared renderer |
| `src/Generator/Renderer/StandaloneClassRenderer.cs` | Same -- calls shared renderer |
| `src/Generator/Renderer/ClassRenderer.cs` | Same -- calls shared renderer |
| `src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs` | NOT affected. Indexers use `Get(callback)` only, no `Get(value)` overload. `ThenGet` reads `_get!` which is always set by `Get(callback)` |
| `src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs` | NOT affected. Properties have `Get(value)` with `ThenGet` but use a different elevation mechanism |
| `src/Design/Design.Tests/MethodTests/MethodSequenceTests.cs` | All existing tests use `Return(callback).ThenReturn(...)` -- no test for `Return(value).ThenReturn(value)` |
| `src/Tests/KnockOffTests/SequenceValueOverloadTests.cs` | Same gap -- no test for `Return(value).ThenReturn(value)` |

---

#### Scope Table

Only method interceptors are affected. Indexers and properties are NOT affected.

| Pattern | Affected | Notes |
|---|---|---|
| Standalone | Yes | Shares `MethodInterceptorRenderer` |
| Generic Standalone | Yes | Same shared renderer |
| Standalone Class | Yes | Same shared renderer |
| Generic Standalone Class | Yes | Same shared renderer |
| Inline Interface | Yes | Same shared renderer |
| Inline Class | Yes | Same shared renderer |
| Inline Delegate | N/A | Delegates do not have method interceptors with Return(value) |
| Open Generic Interface | Yes | Same shared renderer |
| Open Generic Class | Yes | Same shared renderer |

**Member types affected:** Methods only. Properties, indexers, and events are NOT affected.

---

#### Fix Design

##### Approach

Modify the `ThenReturn`/`ThenCall` sequence elevation code in `MethodCallBuilderImpl` to handle all three cases where `_call` is null. Instead of unconditionally adding `_call!`, the generated code must check which Return/Call overload was used and create an appropriate lambda for the first sequence entry.

##### Applicability Constraints

The three null-`_call` cases apply only under specific conditions. The renderer MUST emit each branch conditionally:

| Branch | Condition for Emission | Why |
|---|---|---|
| `_hasReturnValue` | `signatureSuffix == null && canHaveValueOverload` | `_hasReturnValue` and `_returnValue` fields only exist for single-signature interceptors. Overload groups do NOT emit `Return(value)` overloads or value storage fields. |
| `_callSimplified` | Method is `Task<T>` or `ValueTask<T>` AND no ref/out params | `_callSimplified` field only exists when `isAsyncWithInnerType && !hasRefOrOut`. For overload groups, the field name is `_callSimplified_{signatureSuffix}`. |
| `_callSimplifiedVoid` | Method is `Task` or `ValueTask` (void async) AND no ref/out params | `_callSimplifiedVoid` field only exists when `isVoidAsync && !hasRefOrOut`. For overload groups, the field name is `_callSimplifiedVoid_{signatureSuffix}`. |

If none of the conditions apply (e.g., a synchronous void method with single signature), only the `_call != null` branch is needed and the bug cannot occur (because `Return(callback)` / `Call(callback)` always sets `_call` to non-null).

##### Field Name Variables Required

`RenderMethodCallBuilderImpl` already defines `callFieldName` and `callTrackingFieldName` with suffix handling (lines 1463-1464). The following additional field name variables are needed for the fix and do NOT currently exist in this method:

```csharp
// These must be added at the top of RenderMethodCallBuilderImpl alongside existing field name variables
var hasReturnValueFieldName = "_hasReturnValue";  // Only used when signatureSuffix == null
var returnValueFieldName = "_returnValue";         // Only used when signatureSuffix == null
var returnValueTrackingFieldName = "_returnValueTracking"; // Only used when signatureSuffix == null

// These need suffix-based construction for overload groups
var callSimplifiedFieldName = signatureSuffix == null ? "_callSimplified" : $"_callSimplified_{signatureSuffix}";
var callSimplifiedTrackingFieldName = signatureSuffix == null ? "_callSimplifiedTracking" : $"_callSimplifiedTracking_{signatureSuffix}";
var callSimplifiedVoidFieldName = signatureSuffix == null ? "_callSimplifiedVoid" : $"_callSimplifiedVoid_{signatureSuffix}";
var callSimplifiedVoidTrackingFieldName = signatureSuffix == null ? "_callSimplifiedVoidTracking" : $"_callSimplifiedVoidTracking_{signatureSuffix}";
```

Note: `_hasReturnValue`, `_returnValue`, and `_returnValueTracking` do NOT need suffix variants because they only exist for single-signature interceptors (`signatureSuffix == null`).

##### Code Change: Location 1 -- `ThenReturn(callback)` elevation (line 1557-1567)

Current generated code:
```csharp
if (_interceptor._sequence == null)
{
    _interceptor._sequence = new List<(DelegateType Callback, MethodCallBuilderImpl Tracking)>();
    _interceptor._sequence.Add((_interceptor._call!, this));  // BUG: _call is null
    _interceptor._call = null;
    _interceptor._callTracking = null;
    _interceptor._sequenceIndex = 0;
}
```

Fixed generated code (example for a `Task<string> GetDataAsync(int id)` method):
```csharp
if (_interceptor._sequence == null)
{
    _interceptor._sequence = new List<(GetDataAsyncDelegate Callback, MethodCallBuilderImpl Tracking)>();
    if (_interceptor._call != null)
    {
        _interceptor._sequence.Add((_interceptor._call, this));
    }
    else if (_interceptor._hasReturnValue)
    {
        // Return(value) was used -- wrap _returnValue in a lambda
        var capturedValue = _interceptor._returnValue;
        _interceptor._sequence.Add(((id) => global::System.Threading.Tasks.Task.FromResult(capturedValue), this));
        _interceptor._hasReturnValue = false;
        _interceptor._returnValue = default!;
        _interceptor._returnValueTracking = null;
    }
    else if (_interceptor._callSimplified != null)
    {
        // Return(simplifiedCallback) was used -- wrap in full delegate
        var captured = _interceptor._callSimplified;
        _interceptor._sequence.Add(((id) => global::System.Threading.Tasks.Task.FromResult(captured(id)), this));
        _interceptor._callSimplified = null;
        _interceptor._callSimplifiedTracking = null;
    }
    _interceptor._call = null;
    _interceptor._callTracking = null;
    _interceptor._sequenceIndex = 0;
}
```

For a `Task DoSomethingAsync(int id)` (void Task) method, the `_callSimplifiedVoid` branch would appear instead of `_callSimplified`:
```csharp
    else if (_interceptor._callSimplifiedVoid != null)
    {
        // Call(simplifiedVoidCallback) was used -- wrap in full delegate
        var captured = _interceptor._callSimplifiedVoid;
        _interceptor._sequence.Add(((id) => { captured(id); return global::System.Threading.Tasks.Task.CompletedTask; }, this));
        _interceptor._callSimplifiedVoid = null;
        _interceptor._callSimplifiedVoidTracking = null;
    }
```

For a `ValueTask DoSomethingAsync(int id)` (void ValueTask) method:
```csharp
    else if (_interceptor._callSimplifiedVoid != null)
    {
        var captured = _interceptor._callSimplifiedVoid;
        _interceptor._sequence.Add(((id) => { captured(id); return default; }, this));
        _interceptor._callSimplifiedVoid = null;
        _interceptor._callSimplifiedVoidTracking = null;
    }
```

##### Lambda Parameter Forwarding

The lambdas in the fix MUST forward actual method parameters, not a generic placeholder. Use existing helpers:

- **`BuildLambdaParams(parameters)`** -- produces the lambda parameter list (e.g., `id` for 1 param, `a, b` for 2 params, empty for 0 params). Uses `p.EscapedName`.
- **`BuildCallbackArgs(parameters)`** -- produces the callback invocation args (e.g., `id` for 1 param, `ref a, out b` for ref/out params). Uses `p.RefPrefix + p.EscapedName`.
- **`BuildDiscardLambdaPrefix(parameterCount)`** -- produces discard params for value capture (e.g., `(_)` for 1 param, `(_, _)` for 2). Used for the `_hasReturnValue` branch since the value doesn't use params.

For the `_hasReturnValue` branch: Use `BuildDiscardLambdaPrefix` since the captured value ignores parameters.
For the `_callSimplified` and `_callSimplifiedVoid` branches: Use `BuildLambdaParams` for the lambda params and `BuildCallbackArgs` for calling the captured delegate.

Example for `Task<string> GetDataAsync(int id, string name)`:
```csharp
// _hasReturnValue branch:
var discardPrefix = BuildDiscardLambdaPrefix(2); // "(_, _)"
// Emits: (_, _) => global::System.Threading.Tasks.Task.FromResult(capturedValue)

// _callSimplified branch:
var lambdaParams = BuildLambdaParams(parameters); // "id, name"
var callbackArgs = BuildCallbackArgs(parameters); // "id, name"
// Emits: (id, name) => global::System.Threading.Tasks.Task.FromResult(captured(id, name))
```

##### Code Change: Location 2 -- `ThenReturn(params values)` empty-array elevation (line 1600-1612)

The identical `_call!` pattern appears in the `ThenReturn(params values)` method when `values.Length == 0`. The exact same fix logic applies. This code path elevates to sequence mode without adding new values, so the branching is identical to Location 1.

##### Renderer Code Change

In `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs`, method `RenderMethodCallBuilderImpl`, replace the sequence elevation blocks at both locations.

The renderer needs to extract a helper method (or inline at both sites) that:

1. Always emits the `_call != null` branch (this is the non-buggy path that handles `Return(callback)` / `Call(callback)`)
2. Conditionally emits the `_hasReturnValue` branch ONLY when `signatureSuffix == null && canHaveValueOverload` (i.e., single-signature, non-void, no ref/out)
3. Conditionally emits the `_callSimplified` branch ONLY when `isAsyncWithInnerType && !hasRefOrOut` (i.e., `Task<T>` or `ValueTask<T>` without ref/out)
4. Conditionally emits the `_callSimplifiedVoid` branch ONLY when `isVoidAsync && !hasRefOrOut` (i.e., `Task` or `ValueTask` void without ref/out)

`canHaveValueOverload` is NOT currently a parameter of `RenderMethodCallBuilderImpl`. It can be derived within the method: `var canHaveValueOverload = !isVoid && !hasRefOrOut && signatureSuffix == null;` -- the `signatureSuffix == null` check ensures we never reference `_hasReturnValue` in overload groups where that field does not exist.

Alternatively, compute it as: non-void AND no ref/out AND single-signature. The `signatureSuffix == null` check is the distinguishing factor since overload groups do not emit `Return(value)` overloads.

The async type info can be derived from `returnType` using existing helpers:
```csharp
var (valueType, isTaskT, isValueTaskT) = GetAsyncTypeInfo(returnType);
var isAsyncWithInnerType = isTaskT || isValueTaskT;
var (isVoidTask, isVoidValueTask) = GetVoidAsyncInfo(returnType);
var isVoidAsync = isVoidTask || isVoidValueTask;
```

These calls already exist in the parent scope or are cheap to re-derive.

##### Async Wrapping Reference Table

The wrapping must match what the existing `Invoke` method does for each case:

| Return Type | `_hasReturnValue` wrapping | `_callSimplified` wrapping | `_callSimplifiedVoid` wrapping |
|---|---|---|---|
| `T` (sync) | `(discardParams) => capturedValue` | N/A | N/A |
| `Task<T>` | `(discardParams) => Task.FromResult(capturedValue)` | `(lambdaParams) => Task.FromResult(captured(callbackArgs))` | N/A |
| `ValueTask<T>` | `(discardParams) => new ValueTask<T>(capturedValue)` | `(lambdaParams) => new ValueTask<T>(captured(callbackArgs))` | N/A |
| `Task` | N/A (void, no value overload) | N/A | `(lambdaParams) => { captured(callbackArgs); return Task.CompletedTask; }` |
| `ValueTask` | N/A (void, no value overload) | N/A | `(lambdaParams) => { captured(callbackArgs); return default; }` |

Key: `Task.CompletedTask` for void `Task`, `default` (which is `default(ValueTask)`) for void `ValueTask`. This matches the existing Invoke method at lines 754-757 and 960-962.

##### Implementation Notes

- The `_hasReturnValue` branch must capture the value and clear the return-value state (`_hasReturnValue`, `_returnValue`, `_returnValueTracking`) to prevent the Invoke method from using the return-value path instead of the sequence path
- For async methods, the lambda wrapping must match the existing pattern: `Task.FromResult(value)` for Task<T>, `new ValueTask<T>(value)` for ValueTask<T>
- The `_callSimplified` branch must apply the same Task/ValueTask wrapping as the existing Invoke method does for simplified callbacks
- The `_callSimplifiedVoid` branch must apply `Task.CompletedTask` for void `Task` or `return default` for void `ValueTask` (matching lines 754-757 of the Invoke method)
- Since the three null-`_call` cases are mutually exclusive in the generated interceptor (each Return/Call overload clears the others), only one `else if` branch can match at runtime

---

#### Developer Review

**Status:** Concerns Raised (5 items) -- All Addressed
**Reviewed:** 2026-02-07

**Concern 1: Pseudocode parameter handling is vague** -- ADDRESSED. Added "Lambda Parameter Forwarding" section specifying exactly which helpers to use: `BuildLambdaParams` for lambda params, `BuildCallbackArgs` for callback invocation, `BuildDiscardLambdaPrefix` for value-capture lambdas. Added concrete example for `Task<string> GetDataAsync(int id, string name)`.

**Concern 2: Missing field name variables** -- ADDRESSED. Added "Field Name Variables Required" section listing all new variables needed: `callSimplifiedFieldName`, `callSimplifiedTrackingFieldName`, `callSimplifiedVoidFieldName`, `callSimplifiedVoidTrackingFieldName` with suffix-based construction (`_callSimplified_{signatureSuffix}` for overload groups). Noted that `_hasReturnValue`/`_returnValue`/`_returnValueTracking` do NOT need suffix variants.

**Concern 3: `_hasReturnValue` doesn't exist for overload groups** -- ADDRESSED. Added "Applicability Constraints" table. The `_hasReturnValue` branch MUST only emit when `signatureSuffix == null && canHaveValueOverload`. The `_callSimplified` branch MUST only emit when `isAsyncWithInnerType && !hasRefOrOut`. The `_callSimplifiedVoid` branch MUST only emit when `isVoidAsync && !hasRefOrOut`. Updated Implementation Phases to derive `canHaveValueOverload` within the method.

**Concern 4: ValueTask void wrapping incomplete** -- ADDRESSED. Added "Async Wrapping Reference Table" covering all 5 return type variants. Explicitly calls out `Task.CompletedTask` for void `Task` and `return default` (i.e., `default(ValueTask)`) for void `ValueTask`, matching the existing Invoke method at lines 754-757 and 960-962. Updated pseudocode examples to show both void Task and void ValueTask cases.

**Concern 5: Test coverage gap** -- ADDRESSED. Changed simplified callback tests from "Optional" to "Required" with rationale that these are affected bug vectors. Added 3 required tests: `AsyncSimplified_ThenReturnCallback_ReturnsSequence` (test 9), `VoidTaskSimplifiedCall_ThenCall_ReturnsSequence` (test 10), `VoidValueTaskSimplifiedCall_ThenCall_ReturnsSequence` (test 11). Updated Tasks section to match.

---

#### Breaking Changes

**None.** This fix changes generated code behavior from "NRE crash" to "correct behavior." There is no API change. Code that uses `Return(callback).ThenReturn(...)` continues to work identically.

---

#### Test Strategy

##### Regression Tests for `Return(value).ThenReturn(value)`

Add tests to `src/Tests/KnockOffTests/SequenceValueOverloadTests.cs`:

1. **`ReturnValue_ThenReturnValue_ReturnsSequence`** -- `Return("first").ThenReturn("second").ThenReturn("third")` returns values in order
2. **`ReturnValue_ThenReturnCallback_ReturnsSequence`** -- `Return("first").ThenReturn(() => "second")` mixes value start with callback continuation
3. **`ReturnValue_ThenReturnValue_RepeatsLastAfterExhaustion`** -- Verifies repeat-last behavior
4. **`ReturnValue_ThenReturnValue_StrictModeThrows`** -- Verifies strict mode throws on exhaustion
5. **`ReturnValue_ThenReturnValue_Verification`** -- Verifies `sequence.Verify()` works
6. **`ReturnValue_ThenReturnValue_ThenDefault`** -- Verifies ThenDefault works after value-based sequence
7. **`AsyncReturnValue_ThenReturnValue_AutoWraps`** -- For `Task<T>` methods: `Return("first").ThenReturn("second")`
8. **`ValueTaskReturnValue_ThenReturnValue_AutoWraps`** -- For `ValueTask<T>` methods

##### Required: Tests for simplified callback cases

These are affected bug vectors (the `_callSimplified` and `_callSimplifiedVoid` paths set `_call = null` the same way `Return(value)` does) and MUST have test coverage.

9. **`AsyncSimplified_ThenReturnCallback_ReturnsSequence`** -- For `Task<T>` methods: `Return(id => value).ThenReturn(id => Task.FromResult(value2))` -- verifies `_callSimplified` elevation
10. **`VoidTaskSimplifiedCall_ThenCall_ReturnsSequence`** -- For `Task` void methods: `Call(id => { }).ThenCall(id => Task.CompletedTask)` -- verifies `_callSimplifiedVoid` elevation
11. **`VoidValueTaskSimplifiedCall_ThenCall_ReturnsSequence`** -- For `ValueTask` void methods: `Call(id => { }).ThenCall(id => default(ValueTask))` -- verifies `_callSimplifiedVoid` elevation with `ValueTask`

##### Design.Tests Regression Test

Add a test to `src/Design/Design.Tests/MethodTests/MethodSequenceTests.cs`:

```csharp
[Fact]
public void ReturnValue_ThenReturnValue_ReturnsSequence()
{
    var stub = new MethodSequencesDemo.Stubs.ICalculator();

    // Value-based Return followed by value-based ThenReturn
    // This was previously an NRE bug
    stub.Add.Return(1).ThenReturn(2).ThenReturn(3);

    ICalculator calc = stub;

    Assert.Equal(1, calc.Add(0, 0));
    Assert.Equal(2, calc.Add(0, 0));
    Assert.Equal(3, calc.Add(0, 0));
    Assert.Equal(3, calc.Add(0, 0)); // Repeats last
}
```

##### Design.Stubs Example

Add an example to `src/Design/Design.Stubs/Methods/MethodSequences.cs` showing the value-based sequence pattern:

```csharp
public void ReturnValue_ThenReturnValue_Sequence()
{
    var stub = new Stubs.ICalculator();

    // Value-based sequence: Return(value).ThenReturn(value)
    stub.Add.Return(1).ThenReturn(2).ThenReturn(3);

    ICalculator calc = stub;

    var r1 = calc.Add(0, 0); // 1
    var r2 = calc.Add(0, 0); // 2
    var r3 = calc.Add(0, 0); // 3
    var r4 = calc.Add(0, 0); // 3 (repeats last)
}
```

---

#### Edge Cases

1. **`Return(value).ThenReturn(params values)`** -- Same fix applies; the ThenReturn(params) calls ThenReturn(singleValue) which calls ThenReturn(callback), so the elevation happens at the first ThenReturn(callback) call
2. **`Return(value).ThenReturn(callback).ThenReturn(value)`** -- After the first ThenReturn fixes the elevation, subsequent ThenReturn calls operate on `MethodSequenceImpl` which does NOT have this bug (it uses a different path that directly adds to the sequence without reading `_call`)
3. **Null return values** -- `Return((string?)null).ThenReturn("second")` must work; the lambda should capture and return null
4. **Reset after value-based sequence** -- `Reset()` clears all state; a subsequent `Return(value).ThenReturn(value)` must work correctly again
5. **Re-configuration** -- Calling `Return(value)` after a sequence was already set up replaces the sequence (existing behavior via `_sequence = null` in `Return(value)`)

---

#### Implementation Phases

**Phase 1: Fix the renderer** (single file change)
- Add field name variables to `RenderMethodCallBuilderImpl`: `callSimplifiedFieldName`, `callSimplifiedTrackingFieldName`, `callSimplifiedVoidFieldName`, `callSimplifiedVoidTrackingFieldName` (suffix-based for overload group support)
- Note: `hasReturnValueFieldName`, `returnValueFieldName`, `returnValueTrackingFieldName` do NOT need suffix variants (they only exist when `signatureSuffix == null`)
- Derive `canHaveValueOverload` within the method: `!isVoid && !hasRefOrOut && signatureSuffix == null`
- Derive async info: call `GetAsyncTypeInfo(returnType)` and `GetVoidAsyncInfo(returnType)`
- Update Location 1 (line 1563, `ThenReturn(callback)` elevation): Replace unconditional `_call!` with conditional branching
- Update Location 2 (line 1608, `ThenReturn(params values)` empty-array elevation): Same fix
- Use `BuildDiscardLambdaPrefix(parameterCount)` for value-capture lambdas, `BuildLambdaParams(parameters)` + `BuildCallbackArgs(parameters)` for callback-forwarding lambdas
- Emit `_hasReturnValue` branch ONLY when `signatureSuffix == null && canHaveValueOverload`
- Emit `_callSimplified` branch ONLY when `isAsyncWithInnerType && !hasRefOrOut`
- Emit `_callSimplifiedVoid` branch ONLY when `isVoidAsync && !hasRefOrOut`; use `Task.CompletedTask` for void Task, `return default` for void ValueTask

**Phase 2: Add regression tests**
- Add tests to `SequenceValueOverloadTests.cs` for `Return(value).ThenReturn(value)` (tests 1-8 in test strategy)
- Add required tests for `Return(simplifiedCallback).ThenReturn(callback)` (test 9)
- Add required tests for `Call(simplifiedVoidCallback).ThenCall(callback)` for both Task and ValueTask (tests 10-11)
- Add test and example to Design project

**Phase 3: Verify**
- `dotnet build src/KnockOff.sln`
- `dotnet test src/KnockOff.sln`
- `dotnet build src/Design/Design.Stubs`
- `dotnet test src/Design/Design.Tests`

---

#### Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Fix introduces wrong lambda wrapping for async methods | Medium | High | Test Task<T>, ValueTask<T>, void Task, void ValueTask scenarios explicitly. Verify wrapping matches Invoke method (lines 700-757) |
| Fix breaks existing callback-based ThenReturn | Low | High | Existing tests cover this heavily; the `_call != null` branch is the first check and preserves current behavior |
| Fix references non-existent fields in overload groups | Medium | High | Conditional emission gated on `signatureSuffix == null` for `_hasReturnValue`; gated on `isAsyncWithInnerType`/`isVoidAsync` for simplified fields. Compile will catch. |
| Fix uses wrong parameter names in lambdas | Medium | High | Use `BuildLambdaParams`/`BuildCallbackArgs` helpers (not ad-hoc parameter construction). These helpers already handle EscapedName, RefPrefix, etc. |
| Generated code becomes too complex | Low | Low | The branching logic adds ~15 lines per elevation site; acceptable complexity |

---

## Tasks

- [x] Identify the generator code that emits `ThenReturn` for method interceptors
- [x] Confirm root cause and identify all affected Return/Call overloads
- [x] Add field name variables (`callSimplifiedFieldName`, `callSimplifiedVoidFieldName`, etc.) to `RenderMethodCallBuilderImpl` with suffix-based construction
- [x] Fix sequence elevation in `MethodInterceptorRenderer.cs` Location 1 (line 1563 -- `ThenReturn(callback)`)
- [x] Fix sequence elevation in `MethodInterceptorRenderer.cs` Location 2 (line 1608 -- `ThenReturn(params values)` empty array)
- [x] Handle `_hasReturnValue` case -- ONLY emit when `signatureSuffix == null && canHaveValueOverload`
- [x] Handle `_callSimplified` case -- ONLY emit when method is `Task<T>`/`ValueTask<T>` AND no ref/out
- [x] Handle `_callSimplifiedVoid` case -- ONLY emit when method is `Task`/`ValueTask` AND no ref/out; use `Task.CompletedTask` for Task, `return default` for ValueTask
- [x] Use `BuildLambdaParams`/`BuildCallbackArgs` for parameter forwarding (NOT generic placeholders)
- [x] Use `BuildDiscardLambdaPrefix` for `_hasReturnValue` branch (value ignores params)
- [x] Add regression tests to `SequenceValueOverloadTests.cs` for Return(value).ThenReturn(value)
- [x] Add regression test for async Return(value).ThenReturn(value) (Task<T> and ValueTask<T>)
- [x] Add regression tests for `Return(simplifiedCallback).ThenReturn(callback)` (Task<T>/ValueTask<T>)
- [x] Add regression test for `Call(simplifiedVoidCallback).ThenReturn(callback)` (Task and ValueTask)
- [x] Add Design.Tests regression test using `Return(value).ThenReturn(value)`
- [x] Add Design.Stubs example showing value-based sequence pattern
- [x] Verify all existing tests still pass
- [x] Verify Design projects build and test

---

## Progress Log

### 2026-02-07
- Discovered bug while writing protected method behavior tests
- Root cause identified: `Return(value)` nulls `_call`, `ThenReturn` reads `_call!`
- Documented workaround: use callback form `Return(() => "value")`
- Filed this todo

### 2026-02-07 (Implementation)
- Phase 1: Fixed `MethodInterceptorRenderer.cs` -- extracted `EmitSequenceElevation` helper method
  - Added field name variables for `_callSimplified*` and `_callSimplifiedVoid*` with suffix support
  - Derived `canHaveValueOverload`, `elevationIsAsyncWithInnerType`, `elevationIsVoidAsync` conditions
  - Replaced unconditional `_call!` at both locations with conditional branching
  - Branch 1: `_call != null` (existing callback path, always emitted)
  - Branch 2: `_hasReturnValue` (value path, emitted when `canHaveValueOverload`)
  - Branch 3: `_callSimplified != null` (simplified Task<T>/ValueTask<T> callback, emitted when `isAsyncWithInnerType && !hasRefOrOut`)
  - Branch 4: `_callSimplifiedVoid != null` (simplified void Task/ValueTask callback, emitted when `isVoidAsync && !hasRefOrOut`)
  - Checkpoint: All 1241/1242/1242 existing tests pass (net8/net9/net10)
- Phase 2: Added 11 regression tests to `SequenceValueOverloadTests.cs`
  - Tests 1-6: Return(value).ThenReturn(value) for sync methods (sequence, mixed, exhaustion, strict, verify, ThenDefault)
  - Tests 7-8: Return(value).ThenReturn(value) for async methods (Task<T>, ValueTask<T>)
  - Test 9: Return(simplifiedCallback).ThenReturn(fullCallback) for Task<T>
  - Tests 10-11: Call(simplifiedVoidCallback).ThenReturn(fullCallback) for void Task and void ValueTask
  - Note: Tests 10-11 use `.ThenReturn` not `.ThenCall` because `Task`/`ValueTask` returning methods have `IsVoid=false`
  - Added Design.Tests regression test and Design.Stubs example
- Phase 3: Full verification passed

---

## Completion Evidence

### Test Results

**KnockOff.sln (full solution):**
- KnockOffTests.dll (net8.0): 1252 passed, 0 failed
- KnockOffTests.dll (net9.0): 1253 passed, 0 failed
- KnockOffTests.dll (net10.0): 1253 passed, 0 failed
- KnockOffTests.AssemblyStrict.dll: 14 passed x 3 TFMs
- KnockOff.Documentation.Samples.dll: 571 passed x 3 TFMs
- KnockOff.NeatooInterfaceTests.dll: 473 passed x 3 TFMs

**Design.Stubs:** Builds successfully (0 warnings, 0 errors) across all 3 TFMs
**Design.Tests:** 301 passed, 0 failed across all 3 TFMs

### Files Modified

1. `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` -- Added `EmitSequenceElevation` helper, replaced buggy `_call!` at both elevation sites
2. `src/Tests/KnockOffTests/SequenceValueOverloadTests.cs` -- Added 11 regression tests
3. `src/Design/Design.Tests/MethodTests/MethodSequenceTests.cs` -- Added 1 regression test
4. `src/Design/Design.Stubs/Methods/MethodSequences.cs` -- Added value-based sequence example

### Contract Items: All Confirmed Complete

---

## Results / Conclusions

