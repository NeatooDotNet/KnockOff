# Simplify OnCall Sequence API Design (v2)

**Date:** 2026-01-30
**Related Todo:** [Simplify OnCall Sequence API v2](../todos/simplify-oncall-sequence-api-v2.md)
**Status:** Ready for Implementation
**Last Updated:** 2026-01-30 (Developer Approved)

**Note:** Re-implementation on readmeDoc branch. Adapts the completed simpleOnCallSequence design to work with the When() API already present in this branch.

**Branch Test Count:** 4910 tests (920 in KnockOffTests across 3 frameworks, 473 in NeatooInterfaceTests across 3 frameworks)

---

## Overview

Eliminate `OnCallSequence` by extending the `OnCall` return type to support `ThenCall` chaining. When `ThenCall` is called, the callback configuration lazily elevates from repeating to sequence behavior.

This is a re-implementation of work completed in the `simpleOnCallSequence` branch, now applied to the `readmeDoc` branch which already has the When() API for parameter-specific matching.

---

## Approach

1. Create new "builder" interfaces that extend the existing tracking interfaces and add `ThenCall`
2. Change `OnCall` to return the builder interface instead of the tracking interface
3. Implement `ThenCall` to move the existing callback into a sequence list and add the new callback
4. Remove the now-redundant `OnCallSequence` entry point
5. Apply the same pattern to properties (`OnGet`/`OnSet`) and indexers
6. **Preserve When() API** - The builder pattern is orthogonal to When() and must not break it

---

## Design

### New Interfaces

**Methods** - Three new interfaces to match the existing tracking interface hierarchy:

```csharp
namespace KnockOff;

/// <summary>
/// Returned by OnCall(). Supports tracking and optional sequence chaining.
/// </summary>
public interface IMethodCallBuilder<TCallback> : IMethodTracking
{
    /// <summary>
    /// Elevates to sequence mode and adds another callback.
    /// </summary>
    IMethodSequence<TCallback> ThenCall(TCallback callback);

    /// <summary>Marks for verification. Returns builder for fluent chaining.</summary>
    new IMethodCallBuilder<TCallback> Verifiable();
    new IMethodCallBuilder<TCallback> Verifiable(Times times);
}

/// <summary>
/// Returned by OnCall() for methods with a single trackable parameter.
/// </summary>
public interface IMethodCallBuilder<TCallback, TArg> : IMethodTracking<TArg>
{
    IMethodSequence<TCallback> ThenCall(TCallback callback);
    new IMethodCallBuilder<TCallback, TArg> Verifiable();
    new IMethodCallBuilder<TCallback, TArg> Verifiable(Times times);
}

/// <summary>
/// Returned by OnCall() for methods with multiple trackable parameters.
/// </summary>
public interface IMethodCallBuilderArgs<TCallback, TArgs> : IMethodTrackingArgs<TArgs>
{
    IMethodSequence<TCallback> ThenCall(TCallback callback);
    new IMethodCallBuilderArgs<TCallback, TArgs> Verifiable();
    new IMethodCallBuilderArgs<TCallback, TArgs> Verifiable(Times times);
}
```

**Properties** - Two interfaces for property get/set:

```csharp
namespace KnockOff;

/// <summary>
/// Returned by OnGet(). Supports tracking and optional sequence chaining.
/// </summary>
public interface IPropertyGetBuilder<TValue> : IPropertyGetTracking
{
    IPropertyGetSequence<TValue> ThenGet(Func<TValue> callback);
    IPropertyGetSequence<TValue> ThenGet(TValue value);
    new IPropertyGetBuilder<TValue> Verifiable();
}

/// <summary>
/// Returned by OnSet(). Supports tracking and optional sequence chaining.
/// </summary>
public interface IPropertySetBuilder<TValue> : IPropertySetTracking<TValue>
{
    IPropertySetSequence<TValue> ThenSet(Action<TValue> callback);
    new IPropertySetBuilder<TValue> Verifiable();
}
```

**Indexers** - Two interfaces for indexer get/set:

```csharp
namespace KnockOff;

/// <summary>
/// Returned by OnGet() on indexers. Supports tracking and optional sequence chaining.
/// </summary>
public interface IIndexerGetBuilder<TKey, TValue> : IIndexerGetTracking<TKey>
{
    IIndexerGetSequence<TKey, TValue> ThenGet(Func<TKey, TValue> callback);
    new IIndexerGetBuilder<TKey, TValue> Verifiable();
}

/// <summary>
/// Returned by OnSet() on indexers. Supports tracking and optional sequence chaining.
/// </summary>
public interface IIndexerSetBuilder<TKey, TValue> : IIndexerSetTracking<TKey, TValue>
{
    IIndexerSetSequence<TKey, TValue> ThenSet(Action<TKey, TValue> callback);
    new IIndexerSetBuilder<TKey, TValue> Verifiable();
}
```

### Key Design Decision: Builder IS the Tracking

The builder IS the tracking implementation (single object), not a wrapper. This preserves object identity when elevating to sequence:

```csharp
var tracking = stub.Method.OnCall(() => 1);  // tracking IS the MethodCallBuilderImpl
tracking.LastArg  // Works - returns default (not called yet)

stub.Method();  // Invokes callback, builder records call
tracking.LastArg  // Works - returns captured arg

var seq = tracking.ThenCall(() => 2);  // Builder moves itself into sequence
tracking.LastArg  // Still works - same object, now first sequence element
```

### Lazy Elevation Logic

When `ThenCall` is called on the builder:

```csharp
public IMethodSequence<TCallback> ThenCall(TCallback callback)
{
    if (_interceptor._sequence == null)
    {
        _interceptor._sequence = new List<(TCallback, MethodCallBuilderImpl)>();
        // Move THIS builder (with its tracking data) into sequence as first element
        _interceptor._sequence.Add((_interceptor._onCall!, this));
        _interceptor._onCall = null;
        _interceptor._sequenceIndex = 0;
    }
    // Add new callback with fresh builder for its tracking
    var nextBuilder = new MethodCallBuilderImpl(_interceptor);
    _interceptor._sequence.Add((callback, nextBuilder));
    return new MethodSequenceImpl(_interceptor);
}
```

**Important:** After the first `ThenCall()`, subsequent chaining uses `IMethodSequence<TCallback>.ThenCall()` as it does today. The builder's `ThenCall()` is only invoked once - to perform the lazy elevation. The call flow is:

```
OnCall(cb1)           → returns IMethodCallBuilder (repeating mode)
  .ThenCall(cb2)      → builder elevates to sequence, returns IMethodSequence
    .ThenCall(cb3)    → MethodSequenceImpl.ThenCall(), returns IMethodSequence
      .ThenCall(cb4)  → MethodSequenceImpl.ThenCall(), returns IMethodSequence
```

### Interaction with When() API

The When() API and builder pattern are **orthogonal**:

- `OnCall()` → repeating callback (can elevate to sequence with ThenCall)
- `When()` → parameter-specific matching chain

These are separate configuration paths. The builder pattern changes how sequences are entered, not how When() works.

**Current branch state:**
- `MethodInterceptorRenderer.cs` is 2634 lines (includes When() support)
- `WhenChainRenderer.cs` exists (833 lines)
- Uses `MethodTrackingImpl` naming (will rename to `MethodCallBuilderImpl`)

---

## Implementation Steps

### Phase 1: New Interfaces (src/KnockOff/) - Non-Breaking

1. Create `IMethodCallBuilder.cs`:
   - `IMethodCallBuilder<TCallback> : IMethodTracking` with `ThenCall()`, `Verifiable()`, `Verifiable(Times)`
   - `IMethodCallBuilder<TCallback, TArg> : IMethodTracking<TArg>` with same methods
   - `IMethodCallBuilderArgs<TCallback, TArgs> : IMethodTrackingArgs<TArgs>` with same methods

2. Create `IPropertyCallBuilder.cs`:
   - `IPropertyGetBuilder<TValue> : IPropertyGetTracking` with `ThenGet(Func)`, `ThenGet(value)`, `Verifiable()`
   - `IPropertySetBuilder<TValue> : IPropertySetTracking<TValue>` with `ThenSet(Action)`, `Verifiable()`

3. Create `IIndexerCallBuilder.cs`:
   - `IIndexerGetBuilder<TKey, TValue> : IIndexerGetTracking<TKey>` with `ThenGet(Func)`, `Verifiable()`
   - `IIndexerSetBuilder<TKey, TValue> : IIndexerSetTracking<TKey, TValue>` with `ThenSet(Action)`, `Verifiable()`

4. Update existing `IPropertySequence.cs`:
   - Add `ThenGet(TValue value)` to `IPropertyGetSequence<TValue>` interface (currently only on concrete class)

**Verification Checkpoint 1:** `dotnet build src/KnockOff.sln` - should pass (additive interfaces only)

### Phase 2: Generator Changes - Methods

**Model changes** (`src/Generator/Model/Shared/`):

1. **Rename model property** in `UnifiedMethodInterceptorModel.cs`:
   - `TrackingInterface` → `BuilderInterface`

2. **Rename model property** in `MethodOverloadSignature.cs`:
   - `TrackingInterface` → `BuilderInterface`

3. **Update builder method** in `UnifiedInterceptorBuilder.cs`:
   - Rename `GetTrackingInterface()` → `GetBuilderInterface()`
   - Add `delegateType` parameter (builder interfaces include callback type)
   - Update logic to return `IMethodCallBuilder<TCallback>`, `IMethodCallBuilder<TCallback, TArg>`, or `IMethodCallBuilderArgs<TCallback, TArgs>`

**Renderer changes** (`src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs`):

4. **Rename MethodTrackingImpl to MethodCallBuilderImpl**:
   - Builder implements `IMethodCallBuilder<TCallback>` directly
   - Contains tracking state: `CallCount`, `_lastArg`, `_lastArgs`
   - Implements all `IMethodTracking` members directly
   - Add `ThenCall()` with lazy elevation logic
   - Add `Verifiable()` overrides returning builder type

5. **Update OnCall methods**:
   - Change return type from `model.TrackingInterface` to `model.BuilderInterface`
   - Return `MethodCallBuilderImpl` (same instantiation site)

6. **Update Returns methods**:
   - Change return type to builder interface
   - Note: `Returns().ThenCall()` is deferred (known limitation)

7. **Update simplified async OnCall overloads**:
   - `OnCall(Func<T>)` for `Task<T>` methods also returns builder

8. **Keep OnCallSequence temporarily** (remove in Phase 6)

**Verification Checkpoint 2:** `dotnet build src/KnockOff.sln` - should pass

### Phase 3: Generator Changes - Properties

File: `src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs`

1. **Rename PropertyGetTrackingImpl to PropertyGetBuilderImpl**
2. **Rename PropertySetTrackingImpl to PropertySetBuilderImpl**
3. **Add ThenGet()/ThenSet() with lazy elevation**
4. **Update OnGet/OnSet return types**
5. **Keep OnGetSequence/OnSetSequence temporarily**

**Verification Checkpoint 3:** `dotnet build src/KnockOff.sln` - should pass

### Phase 4: Generator Changes - Indexers

File: `src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs`

1. **Rename IndexerGetTrackingImpl to IndexerGetBuilderImpl**
2. **Rename IndexerSetTrackingImpl to IndexerSetBuilderImpl**
3. **Add ThenGet()/ThenSet() with lazy elevation**
4. **Update OnGet/OnSet return types**
5. **Keep OnGetSequence/OnSetSequence temporarily**

**Verification Checkpoint 4:** `dotnet build src/KnockOff.sln` - should pass

### Phase 5: Add Builder Elevation Tests

Create `src/Tests/KnockOffTests/BuilderElevationTests.cs`:

1. `OnCall_WithoutThenCall_RepeatsIndefinitely`
2. `OnCall_ThenCall_CreatesSequence`
3. `OnCall_ThenCall_PreservesTrackingInstance`
4. `Returns_ThenCall_ValueBecomesFirstSequenceElement` (SKIP - deferred)
5. `OnGet_ThenGet_CreatesSequence`
6. `OnGet_ThenGetValue_CreatesSequence`
7. `OnSet_ThenSet_CreatesSequence`
8. `IndexerOnGet_ThenGet_CreatesSequence`
9. `IndexerOnSet_ThenSet_CreatesSequence`
10. `OnCall_WithoutThenCall_TrackingWorks`
11. `OnCall_MultipleThenCall_CreatesLongerSequence`
12. `OnCall_ThenCall_ExhaustedInStrictMode_Throws`
13. `OnCall_ThenCall_ExhaustedInNonStrictMode_ReturnsDefault`
14. `OnCall_AfterSequence_ClearsSequence`

**Verification Checkpoint 5:** `dotnet test src/KnockOff.sln` - new tests pass

### Phase 6: Remove Old APIs and Update Existing Tests

1. **Remove from generators**:
   - `OnCallSequence()` method generation
   - `OnGetSequence()` method generation
   - `OnSetSequence()` method generation

2. **Update existing sequence tests**:
   - Change `OnCallSequence(...).ThenCall(...)` to `OnCall(...).ThenCall(...)`
   - Change `OnGetSequence(...).ThenGet(...)` to `OnGet(...).ThenGet(...)`
   - Change `OnSetSequence(...).ThenSet(...)` to `OnSet(...).ThenSet(...)`

**Verification Checkpoint 6:** `dotnet test src/KnockOff.sln` - all tests pass

### Phase 7: Final Verification

1. Verify all three patterns work:
   - Standalone stubs (KnockOffAttribute on class)
   - Inline interface stubs (KnockOff<IInterface>)
   - Inline class stubs (KnockOff<ConcreteClass>)

2. Verify When() API still works correctly:
   - Run `WhenChainTests.cs`
   - Confirm no regressions

3. Verify edge cases:
   - Overloaded methods
   - Async simplified callbacks
   - ref/out parameters
   - Multi-key indexers

**Verification Checkpoint 7:** Full test suite green

---

## Acceptance Criteria

- [ ] `OnCall((a,b) => x).ThenCall((a,b) => y)` works as a sequence
- [ ] `OnCall((a,b) => x)` without `ThenCall` still repeats
- [ ] All existing `IMethodTracking` functionality works through builder interface
- [ ] `OnCallSequence` is removed
- [ ] Same pattern works for properties (`OnGet/OnSet`) and indexers
- [ ] All existing tests pass (with API updates)
- [ ] When() API tests pass (no regressions)
- [ ] Generated code compiles without warnings
- [ ] Tracking instance is preserved across elevation

---

## Dependencies

None - this is an internal API change.

---

## Risks / Considerations

### Breaking Change

This is a **breaking change** for anyone currently using:
- `OnCallSequence()`
- `OnGetSequence()`
- `OnSetSequence()`

**Migration Guide:**
```
Before: stub.Method.OnCallSequence(() => 1).ThenCall(() => 2)
After:  stub.Method.OnCall(() => 1).ThenCall(() => 2)

Before: stub.Property.OnGetSequence("a").ThenGet(() => "b")
After:  stub.Property.OnGet("a").ThenGet("b")
```

### Known Limitation

`Returns(value).ThenCall()` is not implemented. Users must use `OnCall(() => value).ThenCall()` for value-to-sequence chaining.

### When() API Preservation

The When() API must continue to work. The builder pattern only affects how sequences are configured, not parameter-specific matching.

---

## Architectural Verification

### Three Patterns Analysis

| Pattern | Applies | Impact | Notes |
|---------|---------|--------|-------|
| **Standalone** | Yes | High | Method/property/indexer interceptors all affected. Uses shared renderers. |
| **Inline Interface** | Yes | High | Same interceptor generation via `MethodInterceptorRenderer.cs`, etc. |
| **Inline Class** | Yes | High | Same interceptor generation - virtual/abstract members get same interceptors. |
| **Inline Delegate** | N/A | None | Delegates have `Invoke()` only - no `OnCall`/sequence pattern. |

### Four Member Types Analysis

| Member Type | Applies | Files Affected |
|-------------|---------|----------------|
| **Methods** | Yes | `MethodInterceptorRenderer.cs` (2634 lines) - Primary focus |
| **Properties** | Yes | `PropertyInterceptorRenderer.cs` - OnGet/OnSet sequences |
| **Indexers** | Yes | `IndexerInterceptorRenderer.cs` - OnGet/OnSet sequences |
| **Events** | N/A | Events do not use sequence pattern |

### Breaking Changes Assessment

**Breaking Change:** YES - removes three public API methods

Affected APIs:
1. `OnCallSequence()` - removed from method interceptors
2. `OnGetSequence()` - removed from property/indexer interceptors
3. `OnSetSequence()` - removed from property/indexer interceptors

**Impact Analysis:**
- Pre-1.0 library - breaking changes acceptable per versioning policy
- Migration path is simple 1:1 replacement
- Original implementation in `simpleOnCallSequence` branch had 838 passing tests, current branch has 920

**Migration Guide:**
```csharp
// Methods
Before: stub.Method.OnCallSequence(() => 1).ThenCall(() => 2)
After:  stub.Method.OnCall(() => 1).ThenCall(() => 2)

// Properties (callback)
Before: stub.Property.OnGetSequence(() => "a").ThenGet(() => "b")
After:  stub.Property.OnGet(() => "a").ThenGet(() => "b")

// Properties (value)
Before: stub.Property.OnGetSequence("a").ThenGet("b")
After:  stub.Property.OnGet("a").ThenGet("b")

// Indexers
Before: stub.Item.OnGetSequence(k => v1).ThenGet(k => v2)
After:  stub.Item.OnGet(k => v1).ThenGet(k => v2)
```

### Pattern Consistency Check

**Interface Inheritance Pattern:** Follows KnockOff conventions
- `IMethodCallBuilder<TCallback> : IMethodTracking` - extends existing interface
- `IMethodCallBuilder<TCallback, TArg> : IMethodTracking<TArg>` - variant with LastArg
- `IMethodCallBuilderArgs<TCallback, TArgs> : IMethodTrackingArgs<TArgs>` - variant with LastArgs

**Naming Convention:** Consistent
- Builder suffix for types that enable fluent chaining
- `ThenX` for sequence continuation (matches `ThenWhen` in When API)

**Return Type Pattern:** Follows established pattern
- `OnCall()` returns builder (enables `.ThenCall()` or use as tracking)
- `ThenCall()` returns sequence (for continued chaining)
- Same pattern used for properties/indexers

### When() API Integration Analysis

**Orthogonality Verified:** The builder pattern and When() API are independent:

| Configuration Path | Entry Point | Behavior |
|-------------------|-------------|----------|
| Repeating callback | `OnCall(cb)` | Callback runs every invocation |
| Sequence | `OnCall(cb).ThenCall(cb2)` | Callbacks run once in order |
| When chain | `When(args).Returns(val)` | Parameter-specific matching |

**Priority Order (unchanged):**
1. When chain (if parameters match HEAD matcher)
2. Sequence (if not exhausted)
3. Returns value (if configured)
4. OnCall callback (if configured)
5. Source delegation (if Source() was called)
6. Strict mode check
7. Default value

**When() Code Path:** The When chain invoke check happens at the TOP of `Invoke()` method:
```csharp
// From MethodInterceptorRenderer.cs line 674-681
if (canHaveWhenChain)
{
    RenderWhenChainInvokeCheck(w, model.Parameters, model.ReturnType, null);
}
```
This is unaffected by OnCall/sequence changes.

### Codebase Deep-Dive

**Files Examined:**
- `/home/keithvoels/neatoodotnet/KnockOff/src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` - 2634 lines, contains all method interceptor generation including When() support
- `/home/keithvoels/neatoodotnet/KnockOff/src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs` - Property interceptor generation
- `/home/keithvoels/neatoodotnet/KnockOff/src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs` - Indexer interceptor generation
- `/home/keithvoels/neatoodotnet/KnockOff/src/KnockOff/IMethodTracking.cs` - Existing tracking interfaces
- `/home/keithvoels/neatoodotnet/KnockOff/src/KnockOff/IMethodSequence.cs` - Existing sequence interfaces
- `/home/keithvoels/neatoodotnet/KnockOff/src/KnockOff/IWhenTracking.cs` - When API interfaces
- `/home/keithvoels/neatoodotnet/KnockOff/src/KnockOff/IPropertyTracking.cs` - Property tracking interfaces
- `/home/keithvoels/neatoodotnet/KnockOff/src/KnockOff/IPropertySequence.cs` - Property sequence interfaces
- `/home/keithvoels/neatoodotnet/KnockOff/src/Tests/KnockOffTests/WhenChainTests.cs` - When API tests
- `/home/keithvoels/neatoodotnet/KnockOff/src/Tests/KnockOffTests/SequencingTests.cs` - Current sequence tests

**Key Observations:**
1. `MethodTrackingImpl` is currently a nested private class in generated interceptors (lines 1358-1438)
2. `MethodSequenceImpl` handles ThenCall chaining (lines 1517-1567+)
3. Storage pattern: `_onCall` for repeating callback, `_sequence` for sequence list
4. The naming rename from `MethodTrackingImpl` to `MethodCallBuilderImpl` will affect generated code but not public API

**Current Storage Structure (to be modified):**
```csharp
// OnCall storage - single repeating callback
private {delegateType}? _onCall;
private MethodTrackingImpl? _onCallTracking;

// Sequence storage - list of callbacks
private List<({delegateType} Callback, MethodTrackingImpl Tracking)>? _sequence;
private int _sequenceIndex;
```

**After modification:**
- `MethodTrackingImpl` becomes `MethodCallBuilderImpl` (implements builder interface)
- Same storage structure, builder adds `ThenCall()` that converts to sequence

### Diagnostic Requirements

**No new diagnostics needed:**
- Compile-time errors for invalid usage are already provided by the type system
- `ThenCall()` only available on builder return type
- Sequence-related runtime errors (exhaustion) already handled via `StubException.SequenceExhausted()`

### Test Strategy

1. **Phase 5 tests (new):** Builder elevation behavior tests
   - Verify lazy elevation logic
   - Verify tracking instance preservation
   - Verify builder-to-sequence conversion

2. **Phase 6 tests (updated):** Existing sequence tests migrated
   - Tests in `SequencingTests.cs` (7 files use OnCallSequence)
   - All `OnCallSequence` → `OnCall`
   - All `OnGetSequence` → `OnGet`
   - All `OnSetSequence` → `OnSet`

3. **Phase 7 verification:**
   - Full test suite: 4910 tests across all frameworks
   - Specific When() API verification: `WhenChainTests.cs` must pass unchanged

### Edge Cases

| Edge Case | Expected Behavior | Test Coverage |
|-----------|------------------|---------------|
| `OnCall().ThenCall().ThenCall()` | Three-element sequence | BuilderElevationTests |
| `OnCall()` without `ThenCall()` | Repeats indefinitely | Existing tests |
| Overloaded methods | Per-signature builders | Existing overload tests |
| `async Task<T>` simplified OnCall | Returns builder | AsyncCallbackSimplificationTests |
| `ref`/`out` parameters | Still uses callback delegate | Existing ref/out tests |
| Multi-key indexers | Builder for full key type | IndexerTests |
| Generic methods | Type params on builder | Existing generic tests |
| Builder after sequence consumed | Previous tracking preserved | New test needed |

### Architectural Verification Checklist

- [x] All three patterns analyzed (Standalone, Inline Interface, Inline Class)
- [x] All four member types analyzed (Methods, Properties, Indexers, Events)
- [x] Breaking changes assessment completed
- [x] Pattern consistency verified
- [x] Diagnostic requirements identified (none needed)
- [x] Test strategy defined
- [x] Edge cases documented
- [x] Codebase deep-dive completed (files listed above)
- [x] When() API integration verified as orthogonal

### Risks Identified

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| When() API regression | Low | High | Run WhenChainTests.cs at each checkpoint; When code path is unchanged |
| Tracking instance corruption | Low | Medium | Test preserves object identity across elevation |
| Overload signature collision | Low | Medium | Per-signature builder classes already isolated |
| Simplified async callbacks | Low | Medium | Test Task<T> and ValueTask<T> overloads specifically |

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-01-30
**Reviewed by:** knockoff-developer

### Re-Review (2026-01-30)

All four concerns adequately addressed by architect. One minor typo noted at line 772: `IPropertySetBuilder<TValue, TValue>` should be `IPropertySetBuilder<TValue>` (single type parameter). This is a documentation typo, not a design flaw.

### Original Concerns (All Resolved)

1. **[Clarity]: `ThenGet(TValue value)` overload placement** - **ADDRESSED**
   - Details: The plan's `IPropertyGetBuilder<TValue>` interface (lines 79-83) specifies both `ThenGet(Func<TValue> callback)` and `ThenGet(TValue value)`. However, the current `IPropertyGetSequence` interface only has the callback version - the value overload is generated on the concrete class only (see `PropertyInterceptorRenderer.cs:974-976`).
   - Question: Should `ThenGet(TValue value)` be added to the `IPropertyGetBuilder` interface, or should it remain concrete-class-only for consistency with current pattern?
   - Suggestion: Add to interface for better discoverability and fluent typing.
   - **Resolution:** Accepted. Add to both builder interface AND existing `IPropertyGetSequence` interface.

2. **[Implementation]: Model changes not specified** - **ADDRESSED**
   - Details: The plan discusses changing `OnCall` return types but doesn't mention that `UnifiedMethodInterceptorModel.TrackingInterface` property is used throughout the renderer to determine the return type. This property would need to change from `IMethodTracking<TArg>` to `IMethodCallBuilder<TCallback, TArg>`.
   - Question: Should the model property be renamed (e.g., `BuilderInterface`) and updated to return the new type, or should a new property be added?
   - Suggestion: Add clarification to Phase 2 about which model properties need updating.
   - **Resolution:** Rename to `BuilderInterface`. Phase 2 updated with detailed model changes.

3. **[Implementation]: Lazy elevation logic for multiple `ThenCall()`** - **ADDRESSED**
   - Details: The pseudo-code at lines 136-149 shows the first `ThenCall()` elevating from repeating to sequence mode. It correctly shows adding a new callback, but the pattern `_interceptor._sequence.Add((callback, nextBuilder))` would fail if `ThenCall()` is called on the MethodSequenceImpl (not the builder). The plan states `ThenCall()` returns `IMethodSequence<TCallback>` - so subsequent `ThenCall()` calls go through `MethodSequenceImpl.ThenCall()`, not the builder's `ThenCall()`.
   - Question: Is the intent that the builder's `ThenCall()` is only called once (to elevate), and subsequent `ThenCall()` calls go through `MethodSequenceImpl`? If so, this is consistent with current behavior. Please confirm.
   - Suggestion: Add a sentence clarifying this flow: "After the first `ThenCall()`, subsequent chaining uses `IMethodSequence<TCallback>.ThenCall()` as it does today."
   - **Resolution:** Confirmed. Clarifying call flow diagram added to design section.

4. **[Clarity]: `Verifiable()` return type on builder** - **ADDRESSED**
   - Details: `IMethodTracking.Verifiable()` returns `IMethodTracking`. The new builder interface extends `IMethodTracking`. When user calls `stub.Method.OnCall(cb).Verifiable()`, the return type will be `IMethodTracking`, not `IMethodCallBuilder`, which loses the `ThenCall()` capability.
   - Question: Should `IMethodCallBuilder` override `Verifiable()` to return `IMethodCallBuilder` (similar to how `IMethodTracking<TArg>` overrides it)? This would allow `OnCall(cb).Verifiable().ThenCall(cb2)`.
   - Suggestion: Yes, add `new IMethodCallBuilder<TCallback> Verifiable();` and `new IMethodCallBuilder<TCallback> Verifiable(Times times);` to the builder interface.
   - **Resolution:** Accepted. All builder interfaces now include covariant `Verifiable()` overrides.

### What Looks Good

- Comprehensive 7-phase approach with verification checkpoints
- Clear breaking change documentation and migration guide
- Thorough When() API orthogonality analysis
- Edge case table with test coverage mapping
- Risk assessment with likelihood/impact/mitigation
- All four patterns and member types analyzed

### Codebase Investigation Summary

**Files Examined:**
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` - Confirmed structure at lines 1358-1510
- `src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs` - Verified `OnGetSequence` at line 102
- `src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs` - Verified `OnGetSequence` at line 122
- `src/KnockOff/IMethodTracking.cs` - Existing tracking interface hierarchy
- `src/KnockOff/IPropertySequence.cs` - Has `ThenGet(Func)` but no `ThenGet(value)` on interface
- `src/Tests/KnockOffTests/SequencingTests.cs` - 12 tests use `OnCallSequence`
- `src/Tests/KnockOffTests/WhenChainTests.cs` - When() API tests including sequence priority

**Searches Performed:**
- `OnCallSequence|OnGetSequence|OnSetSequence` - 11 test files use these APIs
- `ThenGet(.*value)` - Only exists on concrete generated class, not interface

### Recommendation

Send back to architect to address the four concerns before implementation. These are clarifications needed, not fundamental design issues. The overall approach is sound.

---

## Architect Response to Concerns

**Date:** 2026-01-30
**Responded by:** knockoff-architect

### Concern 1 Response: `ThenGet(TValue value)` overload placement

**Decision:** Add `ThenGet(TValue value)` to the `IPropertyGetBuilder<TValue>` interface.

**Rationale:**
- The current `IPropertyGetSequence` interface (see `/home/keithvoels/neatoodotnet/KnockOff/src/KnockOff/IPropertySequence.cs`) only has `ThenGet(Func<TValue> callback)` - the value overload is convenience sugar on the concrete class
- For the new builder pattern, adding `ThenGet(TValue value)` to the interface improves discoverability and maintains fluent typing
- This is consistent with the goal of simplifying the API - users should see both options in IntelliSense when chaining

**Updated Interface Design:**
```csharp
public interface IPropertyGetBuilder<TValue> : IPropertyGetTracking
{
    IPropertyGetSequence<TValue> ThenGet(Func<TValue> callback);
    IPropertyGetSequence<TValue> ThenGet(TValue value);
}
```

**Note:** Also add `ThenGet(TValue value)` to `IPropertyGetSequence<TValue>` for consistency (currently only on concrete class at `PropertyInterceptorRenderer.cs:974-976`). This is a minor additive change to the existing interface.

---

### Concern 2 Response: Model changes not specified

**Decision:** Rename `TrackingInterface` property to `BuilderInterface` and update `GetTrackingInterface()` to `GetBuilderInterface()`.

**Rationale:**
- The `TrackingInterface` property in `UnifiedMethodInterceptorModel` (line 49 of `/home/keithvoels/neatoodotnet/KnockOff/src/Generator/Model/Shared/UnifiedMethodInterceptorModel.cs`) currently returns types like `IMethodTracking<TArg>`
- This property is used throughout `MethodInterceptorRenderer.cs` (lines 167, 203, 233, 263, 347, 473, 502, 525, 612) to determine `OnCall` return types
- Renaming to `BuilderInterface` clarifies its new purpose
- The `GetTrackingInterface()` method in `UnifiedInterceptorBuilder.cs` (line 195) must be updated to return builder types

**Implementation Details:**

**Model change** (`UnifiedMethodInterceptorModel.cs`):
```csharp
// Rename from:
string TrackingInterface,
// To:
string BuilderInterface,
```

**Builder change** (`UnifiedInterceptorBuilder.cs`):
```csharp
// Rename method:
public static string GetBuilderInterface(
    EquatableArray<ParameterModel> trackableParams,
    string delegateType)
{
    if (trackableParams.Count == 0)
        return $"global::KnockOff.IMethodCallBuilder<{delegateType}>";
    if (trackableParams.Count == 1)
    {
        var param = trackableParams.GetArray()![0];
        return $"global::KnockOff.IMethodCallBuilder<{delegateType}, {param.Type}>";
    }
    var tupleType = GetLastArgsType(trackableParams);
    return $"global::KnockOff.IMethodCallBuilderArgs<{delegateType}, {tupleType}>";
}
```

**Note:** The method now requires `delegateType` as a parameter since builder interfaces include the callback type.

**Also affects** `MethodOverloadSignature` model - same property rename and builder method updates.

---

### Concern 3 Response: Lazy elevation logic for multiple `ThenCall()`

**Confirmed:** Yes, the intent is exactly as the developer described.

**Clarification:**
1. The builder's `ThenCall()` is called **once** to elevate from repeating to sequence mode
2. That first `ThenCall()` returns `IMethodSequence<TCallback>`
3. Subsequent `ThenCall()` calls chain through `MethodSequenceImpl.ThenCall()` (existing implementation at lines 1556-1564 of `MethodInterceptorRenderer.cs`)

**Call Flow:**
```
OnCall(cb1)           → returns IMethodCallBuilder (repeating mode)
  .ThenCall(cb2)      → builder elevates to sequence, returns IMethodSequence
    .ThenCall(cb3)    → MethodSequenceImpl.ThenCall(), returns IMethodSequence
      .ThenCall(cb4)  → MethodSequenceImpl.ThenCall(), returns IMethodSequence
```

**Added to plan design section (line 149):**
> After the first `ThenCall()`, subsequent chaining uses `IMethodSequence<TCallback>.ThenCall()` as it does today. The builder's `ThenCall()` is only invoked once - to perform the lazy elevation.

---

### Concern 4 Response: `Verifiable()` return type on builder

**Decision:** Yes, add covariant `Verifiable()` overrides to all builder interfaces.

**Rationale:**
- The current `IMethodTracking` hierarchy demonstrates this pattern - `IMethodTracking<TArg>.Verifiable()` returns `IMethodTracking<TArg>` not `IMethodTracking` (see lines 52, 59, 75, 82 of `/home/keithvoels/neatoodotnet/KnockOff/src/KnockOff/IMethodTracking.cs`)
- Without this, fluent chains like `OnCall(cb).Verifiable().ThenCall(cb2)` would fail
- This is a standard C# interface pattern for preserving fluent return types

**Updated Interface Designs:**

```csharp
// Methods
public interface IMethodCallBuilder<TCallback> : IMethodTracking
{
    IMethodSequence<TCallback> ThenCall(TCallback callback);
    new IMethodCallBuilder<TCallback> Verifiable();
    new IMethodCallBuilder<TCallback> Verifiable(Times times);
}

public interface IMethodCallBuilder<TCallback, TArg> : IMethodTracking<TArg>
{
    IMethodSequence<TCallback> ThenCall(TCallback callback);
    new IMethodCallBuilder<TCallback, TArg> Verifiable();
    new IMethodCallBuilder<TCallback, TArg> Verifiable(Times times);
}

public interface IMethodCallBuilderArgs<TCallback, TArgs> : IMethodTrackingArgs<TArgs>
{
    IMethodSequence<TCallback> ThenCall(TCallback callback);
    new IMethodCallBuilderArgs<TCallback, TArgs> Verifiable();
    new IMethodCallBuilderArgs<TCallback, TArgs> Verifiable(Times times);
}

// Properties
public interface IPropertyGetBuilder<TValue> : IPropertyGetTracking
{
    IPropertyGetSequence<TValue> ThenGet(Func<TValue> callback);
    IPropertyGetSequence<TValue> ThenGet(TValue value);
    new IPropertyGetBuilder<TValue> Verifiable();
}

public interface IPropertySetBuilder<TValue> : IPropertySetTracking<TValue>
{
    IPropertySetSequence<TValue> ThenSet(Action<TValue> callback);
    new IPropertySetBuilder<TValue> Verifiable();  // Fixed: was incorrectly <TValue, TValue>
}

// Indexers
public interface IIndexerGetBuilder<TKey, TValue> : IIndexerGetTracking<TKey>
{
    IIndexerGetSequence<TKey, TValue> ThenGet(Func<TKey, TValue> callback);
    new IIndexerGetBuilder<TKey, TValue> Verifiable();
}

public interface IIndexerSetBuilder<TKey, TValue> : IIndexerSetTracking<TKey, TValue>
{
    IIndexerSetSequence<TKey, TValue> ThenSet(Action<TKey, TValue> callback);
    new IIndexerSetBuilder<TKey, TValue> Verifiable();
}
```

**Renderer Implementation:** The `MethodCallBuilderImpl` class must implement both the typed `Verifiable()` returning `IMethodCallBuilder<TCallback>` and explicit interface implementations for base types. This mirrors the existing pattern in `RenderMethodTrackingImpl()` at lines 1461-1508.

---

### Summary of Changes to Plan

1. **Phase 1 interfaces updated** - Added `Verifiable()` overrides to all builder interfaces; added `ThenGet(TValue value)` to property builder
2. **Phase 2 clarified** - Added model property rename (`TrackingInterface` → `BuilderInterface`) and method signature change (`GetBuilderInterface` now requires delegate type)
3. **Design section clarified** - Added explicit statement about builder `ThenCall()` only being called once for elevation
4. **Phase 1 expanded** - Note to also add `ThenGet(TValue value)` to existing `IPropertyGetSequence<TValue>` interface

All concerns addressed. The design is now implementation-ready.

---

## Implementation Contract

**Created:** 2026-01-30
**Approved by:** knockoff-developer

### In Scope

**Phase 1: New Interfaces (src/KnockOff/) - Non-Breaking**
- [ ] Create `src/KnockOff/IMethodCallBuilder.cs`:
  - `IMethodCallBuilder<TCallback> : IMethodTracking`
  - `IMethodCallBuilder<TCallback, TArg> : IMethodTracking<TArg>`
  - `IMethodCallBuilderArgs<TCallback, TArgs> : IMethodTrackingArgs<TArgs>`
  - All include `ThenCall()` and covariant `Verifiable()` overrides
- [ ] Create `src/KnockOff/IPropertyCallBuilder.cs`:
  - `IPropertyGetBuilder<TValue> : IPropertyGetTracking`
  - `IPropertySetBuilder<TValue> : IPropertySetTracking<TValue>`
  - All include `ThenGet()`/`ThenSet()` and covariant `Verifiable()` overrides
- [ ] Create `src/KnockOff/IIndexerCallBuilder.cs`:
  - `IIndexerGetBuilder<TKey, TValue> : IIndexerGetTracking<TKey>`
  - `IIndexerSetBuilder<TKey, TValue> : IIndexerSetTracking<TKey, TValue>`
  - All include `ThenGet()`/`ThenSet()` and covariant `Verifiable()` overrides
- [ ] Update `src/KnockOff/IPropertySequence.cs`:
  - Add `ThenGet(TValue value)` to `IPropertyGetSequence<TValue>` interface
- [ ] **Checkpoint 1:** `dotnet build src/KnockOff.sln` passes

**Phase 2: Generator Changes - Methods**
- [ ] Rename model property in `src/Generator/Model/Shared/UnifiedMethodInterceptorModel.cs`:
  - `TrackingInterface` to `BuilderInterface`
- [ ] Rename model property in `src/Generator/Model/Shared/MethodOverloadSignature.cs`:
  - `TrackingInterface` to `BuilderInterface`
- [ ] Update `src/Generator/Builder/UnifiedInterceptorBuilder.cs`:
  - Rename `GetTrackingInterface()` to `GetBuilderInterface()`
  - Add `delegateType` parameter
  - Update return types to builder interfaces
- [ ] Update `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs`:
  - Rename `MethodTrackingImpl` to `MethodCallBuilderImpl`
  - Implement builder interfaces with `ThenCall()` and covariant `Verifiable()`
  - Update `OnCall` return types to builder interface
  - Update `Returns` return types to builder interface
- [ ] **Checkpoint 2:** `dotnet build src/KnockOff.sln` passes

**Phase 3: Generator Changes - Properties**
- [ ] Update `src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs`:
  - Rename `PropertyGetTrackingImpl` to `PropertyGetBuilderImpl`
  - Rename `PropertySetTrackingImpl` to `PropertySetBuilderImpl`
  - Add `ThenGet()`/`ThenSet()` with lazy elevation
  - Update `OnGet`/`OnSet` return types to builder interfaces
- [ ] **Checkpoint 3:** `dotnet build src/KnockOff.sln` passes

**Phase 4: Generator Changes - Indexers**
- [ ] Update `src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs`:
  - Rename `IndexerGetTrackingImpl` to `IndexerGetBuilderImpl`
  - Rename `IndexerSetTrackingImpl` to `IndexerSetBuilderImpl`
  - Add `ThenGet()`/`ThenSet()` with lazy elevation
  - Update `OnGet`/`OnSet` return types to builder interfaces
- [ ] **Checkpoint 4:** `dotnet build src/KnockOff.sln` passes

**Phase 5: Add Builder Elevation Tests**
- [ ] Create `src/Tests/KnockOffTests/BuilderElevationTests.cs` with:
  - `OnCall_WithoutThenCall_RepeatsIndefinitely`
  - `OnCall_ThenCall_CreatesSequence`
  - `OnCall_ThenCall_PreservesTrackingInstance`
  - `OnGet_ThenGet_CreatesSequence`
  - `OnGet_ThenGetValue_CreatesSequence`
  - `OnSet_ThenSet_CreatesSequence`
  - `IndexerOnGet_ThenGet_CreatesSequence`
  - `IndexerOnSet_ThenSet_CreatesSequence`
  - `OnCall_WithoutThenCall_TrackingWorks`
  - `OnCall_MultipleThenCall_CreatesLongerSequence`
  - `OnCall_ThenCall_ExhaustedInStrictMode_Throws`
  - `OnCall_ThenCall_ExhaustedInNonStrictMode_ReturnsDefault`
  - `OnCall_AfterSequence_ClearsSequence`
- [ ] **Checkpoint 5:** `dotnet test src/KnockOff.sln` - new tests pass

**Phase 6: Remove Old APIs and Update Existing Tests**
- [ ] Remove from `MethodInterceptorRenderer.cs`: `OnCallSequence()` generation
- [ ] Remove from `PropertyInterceptorRenderer.cs`: `OnGetSequence()`/`OnSetSequence()` generation
- [ ] Remove from `IndexerInterceptorRenderer.cs`: `OnGetSequence()`/`OnSetSequence()` generation
- [ ] Update tests in (11 files):
  - `src/Tests/KnockOff.Documentation.Samples/IndexersSamples.cs`
  - `src/Tests/KnockOff.Documentation.Samples/PropertiesSamples.cs`
  - `src/Tests/KnockOff.Documentation.Samples/ReadmeSamples.cs`
  - `src/Tests/KnockOff.Documentation.Samples/TroubleshootingSamples.cs`
  - `src/Tests/KnockOffTests/AsyncCallbackSimplificationTests.cs`
  - `src/Tests/KnockOffTests/MethodValueOverloadTests.cs`
  - `src/Tests/KnockOffTests/PropertyValueOverloadTests.cs`
  - `src/Tests/KnockOffTests/SequenceValueOverloadTests.cs`
  - `src/Tests/KnockOffTests/SequencingTests.cs`
  - `src/Tests/KnockOffTests/VerificationTests.cs`
  - `src/Tests/KnockOffTests/WhenChainTests.cs`
- [ ] **Checkpoint 6:** `dotnet test src/KnockOff.sln` - all tests pass

**Phase 7: Final Verification**
- [ ] Verify all three patterns work (Standalone, Inline Interface, Inline Class)
- [ ] Verify When() API tests pass unchanged
- [ ] Verify edge cases: overloads, async callbacks, ref/out, multi-key indexers
- [ ] **Checkpoint 7:** Full test suite green (4910 tests expected)

### Explicitly Out of Scope

- `Returns(value).ThenCall()` - marked as deferred limitation
- Events - do not use sequence pattern
- Inline Delegate pattern - has `Invoke()` only, no `OnCall`/sequence
- Any changes to When() API behavior (must remain orthogonal)
- New diagnostics - none needed per plan

### Verification Gates

1. **After Phase 1:** All new interfaces compile, no test changes needed yet
2. **After Phase 2:** Methods use builder pattern, tests still pass (OnCallSequence still exists)
3. **After Phase 3:** Properties use builder pattern, tests still pass
4. **After Phase 4:** Indexers use builder pattern, tests still pass
5. **After Phase 5:** New elevation tests pass, demonstrating builder behavior
6. **After Phase 6:** Old APIs removed, all tests migrated and passing
7. **Final:** Full test suite green, generated code compiles without warnings

### Stop Conditions

If any of these occur, STOP and report:
- Out-of-scope test fails (not one of the 11 files listed for API migration)
- When() API tests fail unexpectedly
- Architectural contradiction discovered (e.g., builder pattern incompatible with existing feature)
- Generated code does not compile
- Test count decreases unexpectedly

---

## Implementation Progress

[To be filled during implementation]

---

## Completion Evidence

[To be filled before marking complete]
