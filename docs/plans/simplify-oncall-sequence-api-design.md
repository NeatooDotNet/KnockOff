# Simplify OnCall Sequence API Design

**Date:** 2026-01-29
**Related Todo:** [Simplify OnCall Sequence API](../todos/simplify-oncall-sequence-api.md)
**Status:** Ready for Implementation
**Last Updated:** 2026-01-29 (Developer Approved)

---

## Overview

Eliminate `OnCallSequence` by extending the `OnCall` return type to support `ThenCall` chaining. When `ThenCall` is called, the callback configuration lazily elevates from repeating to sequence behavior.

---

## Approach

1. Create new "builder" interfaces that extend the existing tracking interfaces and add `ThenCall`
2. Change `OnCall` to return the builder interface instead of the tracking interface
3. Implement `ThenCall` to move the existing callback into a sequence list and add the new callback
4. Remove the now-redundant `OnCallSequence` entry point
5. Apply the same pattern to properties (`OnGet`/`OnSet`) and indexers

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
}

/// <summary>
/// Returned by OnCall() for methods with a single trackable parameter.
/// </summary>
public interface IMethodCallBuilder<TCallback, TArg> : IMethodTracking<TArg>
{
    IMethodSequence<TCallback> ThenCall(TCallback callback);
}

/// <summary>
/// Returned by OnCall() for methods with multiple trackable parameters.
/// </summary>
public interface IMethodCallBuilderArgs<TCallback, TArgs> : IMethodTrackingArgs<TArgs>
{
    IMethodSequence<TCallback> ThenCall(TCallback callback);
}
```

**Properties** - Four new interfaces for property get/set:

```csharp
namespace KnockOff;

/// <summary>
/// Returned by OnGet(). Supports tracking and optional sequence chaining.
/// </summary>
public interface IPropertyGetBuilder<TValue> : IPropertyGetTracking
{
    /// <summary>
    /// Elevates to sequence mode and adds another getter callback.
    /// </summary>
    IPropertyGetSequence<TValue> ThenGet(Func<TValue> callback);

    /// <summary>
    /// Elevates to sequence mode and adds a value (wrapped in callback).
    /// </summary>
    IPropertyGetSequence<TValue> ThenGet(TValue value);
}

/// <summary>
/// Returned by OnSet(). Supports tracking and optional sequence chaining.
/// </summary>
public interface IPropertySetBuilder<TValue> : IPropertySetTracking<TValue>
{
    /// <summary>
    /// Elevates to sequence mode and adds another setter callback.
    /// </summary>
    IPropertySetSequence<TValue> ThenSet(Action<TValue> callback);
}
```

**Indexers** - Four new interfaces for indexer get/set:

```csharp
namespace KnockOff;

/// <summary>
/// Returned by OnGet() on indexers. Supports tracking and optional sequence chaining.
/// </summary>
public interface IIndexerGetBuilder<TKey, TValue> : IIndexerGetTracking<TKey>
{
    /// <summary>
    /// Elevates to sequence mode and adds another getter callback.
    /// </summary>
    IIndexerGetSequence<TKey, TValue> ThenGet(Func<TKey, TValue> callback);
}

/// <summary>
/// Returned by OnSet() on indexers. Supports tracking and optional sequence chaining.
/// </summary>
public interface IIndexerSetBuilder<TKey, TValue> : IIndexerSetTracking<TKey, TValue>
{
    /// <summary>
    /// Elevates to sequence mode and adds another setter callback.
    /// </summary>
    IIndexerSetSequence<TKey, TValue> ThenSet(Action<TKey, TValue> callback);
}
```

### Lazy Elevation Logic

**Key Design Decision:** The builder IS the tracking implementation (single object), not a wrapper. This preserves object identity when elevating to sequence.

When `ThenCall` is called on the builder:

```csharp
// MethodCallBuilderImpl implements IMethodCallBuilder<TCallback> directly
// It contains tracking state (CallCount, _lastArg) - no separate MethodTrackingImpl

public IMethodSequence<TCallback> ThenCall(TCallback callback)
{
    // 1. Create sequence list if not already in sequence mode
    if (_interceptor._sequence == null)
    {
        _interceptor._sequence = new List<(TCallback, MethodCallBuilderImpl)>();

        // 2. Move THIS builder (with its tracking data) into sequence as first element
        // The builder reference held by the user remains valid
        _interceptor._sequence.Add((_interceptor._onCall!, this));

        // 3. Clear single-callback field (builder reference is now in sequence)
        _interceptor._onCall = null;
        _interceptor._sequenceIndex = 0;
    }

    // 4. Add new callback with fresh builder for its tracking
    var nextBuilder = new MethodCallBuilderImpl(_interceptor);
    _interceptor._sequence.Add((callback, nextBuilder));

    // 5. Return IMethodSequence for further chaining
    return new MethodSequenceImpl(_interceptor);
}
```

**Object Identity Preservation Example:**
```csharp
var tracking = stub.Method.OnCall(() => 1);  // tracking IS the MethodCallBuilderImpl
tracking.LastArg  // Works - returns default (not called yet)

stub.Method();  // Invokes callback, builder records call
tracking.LastArg  // Works - returns captured arg

var seq = tracking.ThenCall(() => 2);  // Builder moves itself into sequence
tracking.LastArg  // Still works - same object, now first sequence element
```

### Return Type Changes

**Methods - Before:**
```csharp
public IMethodTracking OnCall(DelegateType callback)
public IMethodTracking<TArg> OnCall(DelegateType callback)  // single trackable param
public IMethodTrackingArgs<TArgs> OnCall(DelegateType callback)  // multi trackable params
```

**Methods - After:**
```csharp
public IMethodCallBuilder<DelegateType> OnCall(DelegateType callback)
public IMethodCallBuilder<DelegateType, TArg> OnCall(DelegateType callback)
public IMethodCallBuilderArgs<DelegateType, TArgs> OnCall(DelegateType callback)
```

**Properties - Before:**
```csharp
public IPropertyGetTracking OnGet(Func<TValue> callback)
public IPropertySetTracking<TValue> OnSet(Action<TValue> callback)
```

**Properties - After:**
```csharp
public IPropertyGetBuilder<TValue> OnGet(Func<TValue> callback)
public IPropertySetBuilder<TValue> OnSet(Action<TValue> callback)
```

**Indexers - Before:**
```csharp
public IIndexerGetTracking<TKey> OnGet(Func<TKey, TValue> callback)
public IIndexerSetTracking<TKey, TValue> OnSet(Action<TKey, TValue> callback)
```

**Indexers - After:**
```csharp
public IIndexerGetBuilder<TKey, TValue> OnGet(Func<TKey, TValue> callback)
public IIndexerSetBuilder<TKey, TValue> OnSet(Action<TKey, TValue> callback)
```

### Returns() Method Consideration

The `Returns(value)` method currently returns `IMethodTracking`. For consistency, it should also return a builder interface to support `ThenCall`:

```csharp
// Allows:
stub.Method.Returns(42).ThenCall(() => 100);  // First call returns 42, then callback
```

However, this adds complexity. Two options:

**Option A: Returns() also returns builder** (Recommended)
- Enables `Returns(42).ThenCall(() => 100)` chaining
- Consistent API - both entry points support elevation
- Slightly more complex but cleaner UX

**Option B: Returns() stays as tracking only**
- Simpler implementation
- Users must use `OnCall(() => 42).ThenCall(() => 100)` for value sequences
- Inconsistent with the "any entry point can elevate" philosophy

**Recommendation: Option A** - Returns() should return `IMethodCallBuilder` to enable full sequence chaining from any entry point.

### Removal of Old Entry Points

Delete these methods from generated code:
- `OnCallSequence()` on method interceptors
- `OnGetSequence()` on property/indexer interceptors
- `OnSetSequence()` on property/indexer interceptors

Also delete from public interfaces:
- Any `OnXxxSequence()` methods that are currently on public interfaces

---

## Edge Cases and Special Scenarios

### 1. Overloaded Methods

For methods with overloads, each overload has its own delegate type and tracking. The builder interfaces need the correct delegate type:

```csharp
// Generated per overload:
public IMethodCallBuilder<FormatDelegate_String_String> OnCall(FormatDelegate_String_String callback)
public IMethodCallBuilder<FormatDelegate_String_Boolean_String, (string input, bool uppercase)>
    OnCall(FormatDelegate_String_Boolean_String callback)
```

**Impact:** Generator must select correct builder interface variant based on overload's trackable parameters.

### 2. Async Methods with Simplified Callbacks

Methods like `Task<T>` have multiple `OnCall` overloads:
- `OnCall(Func<Task<T>>)` - full callback
- `OnCall(Func<T>)` - simplified callback (auto-wrapped)

Both should return builder interfaces:

```csharp
public IMethodCallBuilder<Func<Task<T>>> OnCall(Func<Task<T>> callback)
public IMethodCallBuilder<Func<T>> OnCall(Func<T> callback)  // simplified
```

**Consideration:** When user chains `ThenCall` from simplified, should ThenCall accept simplified or full delegate?

**Recommendation:** ThenCall should match the delegate type of the initial OnCall. The sequence already stores the delegate type, so this is natural.

### 3. Void Methods

Void methods don't have a return tracking interface variant (just `IMethodTracking`), but they can still sequence:

```csharp
stub.DoWork.OnCall(() => log.Add("first")).ThenCall(() => log.Add("second"));
```

**Impact:** Builder interface works - `IMethodCallBuilder<Action>` extends `IMethodTracking`.

### 4. Methods with ref/out Parameters

Methods with `ref` or `out` parameters use custom delegate types. The builder pattern works the same:

```csharp
// TryParse(string input, out int result) uses custom delegate
public IMethodCallBuilder<TryParseDelegate> OnCall(TryParseDelegate callback)
```

### 5. Property Value Overload (OnGet(value))

Properties support `OnGet(value)` which wraps to `OnGet(() => value)`. This should also return builder:

```csharp
stub.Name.OnGet("Alice").ThenGet("Bob");  // Sequence of values
```

The `OnGet(TValue value)` overload should return `IPropertyGetBuilder<TValue>`.

### 6. Init-Only Properties

Init-only properties only generate `OnGet`/`OnGetSequence` (no OnSet). The pattern applies to the getter side only.

### 7. Existing Sequence in Progress

If user calls `OnCall` when a sequence is already configured, the current behavior clears the sequence. This should remain:

```csharp
stub.Method.OnCall(() => 1).ThenCall(() => 2);  // sequence of 2
stub.Method.OnCall(() => 100);  // Clears sequence, sets new repeating callback
```

### 8. Tracking Instance Preservation

When `ThenCall` elevates to sequence, the original tracking instance from `OnCall` must be preserved:

```csharp
var tracking = stub.Method.OnCall(() => 1);
tracking.LastArg  // Should work before and after ThenCall
var seq = tracking.ThenCall(() => 2);
tracking.LastArg  // Still works - same tracking instance, now in sequence
```

**Implementation:** The builder impl class should implement both builder interface and store the tracking. When ThenCall is called, the existing tracking moves to the sequence.

---

## Implementation Steps

### Phase 1: New Interfaces (src/KnockOff/) - Non-Breaking

1. Create `IMethodCallBuilder.cs`:
   - `IMethodCallBuilder<TCallback> : IMethodTracking`
   - `IMethodCallBuilder<TCallback, TArg> : IMethodTracking<TArg>`
   - `IMethodCallBuilderArgs<TCallback, TArgs> : IMethodTrackingArgs<TArgs>`

2. Create `IPropertyCallBuilder.cs`:
   - `IPropertyGetBuilder<TValue> : IPropertyGetTracking`
   - `IPropertySetBuilder<TValue> : IPropertySetTracking<TValue>`

3. Create `IIndexerCallBuilder.cs`:
   - `IIndexerGetBuilder<TKey, TValue> : IIndexerGetTracking<TKey>`
   - `IIndexerSetBuilder<TKey, TValue> : IIndexerSetTracking<TKey, TValue>`

**Verification Checkpoint 1:** `dotnet build src/KnockOff.sln` - should pass (additive interfaces only)

### Phase 2: Generator Changes - Methods

File: `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs`

1. **Replace MethodTrackingImpl with MethodCallBuilderImpl**:
   - `MethodCallBuilderImpl` IS the tracking (not a wrapper)
   - Implements `IMethodCallBuilder<TCallback>` directly
   - Contains tracking state: `CallCount`, `_lastArg`, `_lastArgs`
   - Implements all `IMethodTracking` members directly
   - Implements `ThenCall()` with lazy elevation logic

2. **Update OnCall methods**:
   - Change return type from `IMethodTracking` to `IMethodCallBuilder<...>`
   - Return `MethodCallBuilderImpl` (same instantiation site)

3. **Update Returns methods**:
   - Change return type to `IMethodCallBuilder<...>`
   - Return `MethodCallBuilderImpl` that can elevate value to sequence

4. **Update simplified async OnCall overloads**:
   - `OnCall(Func<T>)` for `Task<T>` methods also returns builder
   - Builder's `ThenCall` accepts same simplified delegate type

5. **Keep OnCallSequence temporarily** (remove in Phase 6)

**Verification Checkpoint 2:** `dotnet build src/KnockOff.sln` - should pass (return types widen, backward compatible)

### Phase 3: Generator Changes - Properties

File: `src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs`

1. **Replace PropertyGetTrackingImpl with PropertyGetBuilderImpl**:
   - Implements `IPropertyGetBuilder<TValue>` directly
   - Contains tracking state
   - Implements `ThenGet(Func<TValue>)` and `ThenGet(TValue)` with lazy elevation

2. **Replace PropertySetTrackingImpl with PropertySetBuilderImpl**:
   - Implements `IPropertySetBuilder<TValue>` directly
   - Implements `ThenSet()` with lazy elevation

3. **Update OnGet/OnSet and OnGet(value) return types**

4. **Keep OnGetSequence/OnSetSequence temporarily**

**Verification Checkpoint 3:** `dotnet build src/KnockOff.sln` - should pass

### Phase 4: Generator Changes - Indexers

File: `src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs`

1. **Replace IndexerGetTrackingImpl with IndexerGetBuilderImpl**
2. **Replace IndexerSetTrackingImpl with IndexerSetBuilderImpl**
3. **Update OnGet/OnSet return types**
4. **Keep OnGetSequence/OnSetSequence temporarily**

**Verification Checkpoint 4:** `dotnet build src/KnockOff.sln` - should pass

### Phase 5: Add Builder Elevation Tests

Before removing old APIs, add tests that verify new API works:

1. **New test file: `BuilderElevationTests.cs`**:
   - `OnCall_WithoutThenCall_RepeatsIndefinitely`
   - `OnCall_ThenCall_CreatesSequence`
   - `OnCall_ThenCall_PreservesTrackingInstance`
   - `Returns_ThenCall_ValueBecomesFirstSequenceElement`
   - `OnGet_ThenGet_CreatesSequence`
   - `OnSet_ThenSet_CreatesSequence`
   - `IndexerOnGet_ThenGet_CreatesSequence`

**Verification Checkpoint 5:** `dotnet test src/KnockOff.sln` - new tests should pass

### Phase 6: Remove Old APIs and Update Existing Tests

1. **Remove from generators**:
   - `OnCallSequence()` method generation
   - `OnGetSequence()` method generation
   - `OnSetSequence()` method generation

2. **Update existing sequence tests** (`SequencingTests.cs`, `SequenceValueOverloadTests.cs`):
   - Change `OnCallSequence(...).ThenCall(...)` to `OnCall(...).ThenCall(...)`
   - Change `OnGetSequence(...).ThenGet(...)` to `OnGet(...).ThenGet(...)`

**Verification Checkpoint 6:** `dotnet test src/KnockOff.sln` - all tests should pass

### Phase 7: Final Verification

1. Verify all three patterns work:
   - Standalone stubs
   - Inline interface stubs
   - Inline class stubs

2. Verify edge cases:
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
- [ ] Returns(value).ThenCall() works (if Option A implemented)
- [ ] All existing tests pass (with API updates)
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

**Mitigation:** Since KnockOff is pre-1.0, breaking changes are expected. Document in release notes with migration guide.

**Migration Guide:**
```
Before: stub.Method.OnCallSequence(() => 1).ThenCall(() => 2)
After:  stub.Method.OnCall(() => 1).ThenCall(() => 2)

Before: stub.Property.OnGetSequence("a").ThenGet(() => "b")
After:  stub.Property.OnGet("a").ThenGet("b")
```

### Return Type Change

The return type of `OnCall` changes from `IMethodTracking` to `IMethodCallBuilder<T>`. However, `IMethodCallBuilder<T> : IMethodTracking`, so:
- Existing code that uses `OnCall()` without storing the result: **works**
- Existing code that stores result as `IMethodTracking`: **works** (builder extends tracking)
- Existing code that stores result as `var`: **works**

### Interface Proliferation

Adding 8+ new interfaces. This is acceptable because:
- They're direct extensions of existing interfaces
- Users rarely need to reference these types directly
- The simplified API justifies the internal complexity

### Complexity in Generated Code

The `MethodCallBuilderImpl` class needs to:
- Implement all tracking interface members (delegating to tracking impl)
- Have ThenCall with lazy elevation logic
- Access interceptor fields

This adds complexity to the renderer, but it's manageable.

---

## Architectural Verification

**Three Patterns Analysis:**
- Standalone: Applies - method/property/indexer interceptors all affected
- Inline Interface: Applies - same interceptor generation via shared renderers
- Inline Class: Applies - same interceptor generation via shared renderers
- Inline Delegate: N/A - delegates don't use OnCall/OnCallSequence pattern

**Breaking Changes:** Yes - removes `OnCallSequence`, `OnGetSequence`, `OnSetSequence` entry points. Migration is straightforward (change method name, same arguments).

**Pattern Consistency:** Follows existing pattern of interfaces extending each other (`IMethodTracking<T> : IMethodTracking`). The builder pattern adds one level of inheritance.

**Diagnostic Requirements:** None - this is a runtime API change, not a compile-time diagnostic change.

**Test Strategy:**
1. Update existing sequence tests to use new API
2. Add elevation behavior tests
3. Verify all three stub patterns work
4. Test edge cases (overloads, async, ref/out)

**Codebase Deep-Dive (Files Examined):**
- `src/KnockOff/IMethodSequence.cs` - existing sequence interface (lines 1-43)
- `src/KnockOff/IMethodTracking.cs` - tracking interface hierarchy (lines 1-83)
- `src/KnockOff/IPropertyTracking.cs` - property tracking interfaces (lines 1-82)
- `src/KnockOff/IPropertySequence.cs` - property sequence interfaces (lines 1-61)
- `src/KnockOff/IIndexerTracking.cs` - indexer tracking interfaces (lines 1-81)
- `src/KnockOff/IIndexerSequence.cs` - indexer sequence interfaces (lines 1-61)
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` - OnCall/OnCallSequence generation (lines 1-1565)
- `src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs` - property sequence generation (lines 1-1077)
- `src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs` - indexer sequence generation (lines 1-808)
- `src/Tests/KnockOffTests/SequencingTests.cs` - existing sequence tests (lines 1-414)
- `src/Tests/KnockOffTests/SequenceValueOverloadTests.cs` - property sequence tests (lines 1-304)

**Key Architectural Insights:**
1. The sequence storage (`_sequence` field) is already separate from `_onCall` - lazy elevation just moves data from one to the other
2. `MethodTrackingImpl` is a nested class inside the interceptor - builder can also be nested
3. Overloads have per-overload storage with `_signatureSuffix` naming - builder must match this pattern
4. Async simplified callbacks are separate storage fields - ThenCall from simplified should continue the same delegate pattern
5. Property/Indexer patterns mirror method pattern closely - same elevation strategy works

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-01-29
**Architect Response:** 2026-01-29
**Developer Approval:** 2026-01-29

### Why This Plan Is Approved

All six concerns have been adequately addressed by the architect:

1. **Builder vs Tracking Relationship** - Clarified as single-object design. `MethodCallBuilderImpl` IS the tracking (not a wrapper). Object identity preserved during elevation.

2. **Verifiable() Return Type** - Confirmed as terminal operation. Users chain `ThenCall` before `Verifiable()`, not after. No interface change needed.

3. **Async Simplified Callbacks** - Each `OnCall` overload returns its own builder type with matching `ThenCall` signature. Consistent with existing pattern.

4. **Multi-Key Indexers** - Verified in codebase: TKey becomes tuple type `(int x, int y)`, callbacks use expanded parameters. Single interface handles both cases.

5. **Verification Checkpoints** - Phases restructured with 7 explicit checkpoints. Each phase verifies build/test before proceeding.

6. **Returns().ThenCall() Semantics** - First call returns value, subsequent calls use ThenCall callbacks. Value converted to callback on elevation.

### Codebase Verification

**Files I Examined:**
- `src/KnockOff/IMethodTracking.cs` (lines 1-84) - Confirmed interface hierarchy matches plan
- `src/KnockOff/IMethodSequence.cs` (lines 1-44) - Confirmed `ThenCall(TCallback)` signature
- `src/KnockOff/IIndexerTracking.cs` (lines 1-82) - Confirmed single TKey parameter handles tuples
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` (lines 1-350) - Verified `MethodTrackingImpl` nested class pattern
- `src/Generator/Builder/InlineModelBuilder.cs` (lines 259-281) - Confirmed multi-key indexer tuple handling
- `src/Tests/KnockOffTests/SequencingTests.cs` (lines 1-100) - Confirmed test patterns to update

**Key Observations:**
- Current `MethodTrackingImpl` is already a nested class with tracking state - builder design mirrors this
- Multi-key indexers use named tuples for `TKey` (e.g., `(int x, int y)`) - single interface sufficient
- Sequence list already stores `(Callback, Tracking)` tuples - builder fits naturally
- 7 phase structure with checkpoints prevents incomplete states

### Original Review Details

### My Understanding of This Plan

**Core Change:** Eliminate `OnCallSequence`, `OnGetSequence`, `OnSetSequence` entry points by having `OnCall`, `OnGet`, and `OnSet` return builder interfaces that support `ThenCall`/`ThenGet`/`ThenSet` chaining. The builder lazily elevates to sequence mode when chaining is used.

**User-Facing API:**
- `stub.Method.OnCall(() => 1).ThenCall(() => 2)` instead of `stub.Method.OnCallSequence(() => 1).ThenCall(() => 2)`
- Same pattern for properties and indexers
- Optionally: `stub.Method.Returns(42).ThenCall(() => 100)`

**Internal Changes:**
- Add 8+ new builder interfaces in `src/KnockOff/`
- Modify `MethodInterceptorRenderer`, `PropertyInterceptorRenderer`, `IndexerInterceptorRenderer` to return builder interfaces
- Add nested `*BuilderImpl` classes to generated code
- Remove `OnCallSequence`, `OnGetSequence`, `OnSetSequence` method generation

**Patterns Affected:** Standalone, Inline Interface, Inline Class (all use shared renderers)

### Codebase Investigation

**Files Examined:**
- `src/KnockOff/IMethodTracking.cs` - Confirmed tracking interface hierarchy
- `src/KnockOff/IMethodSequence.cs` - Confirmed `IMethodSequence<TCallback>` has `ThenCall(TCallback)` method
- `src/KnockOff/IPropertyTracking.cs` - Confirmed `IPropertyGetTracking` and `IPropertySetTracking<TValue>` are separate (no common base)
- `src/KnockOff/IPropertySequence.cs` - Confirmed `ThenGet(Func<TValue>)` only (no value overload on interface)
- `src/KnockOff/IIndexerTracking.cs` - Confirmed indexer tracking with key type parameters
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` (lines 1-500) - Confirmed patterns for OnCall, OnCallSequence, Returns
- `src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs` (lines 1-200) - Confirmed property pattern
- `src/Generator/Renderer/InlineRenderer.cs` (lines 1280-1330) - Confirmed inline delegate stubs have `Returns(value)` but no sequence
- `src/Tests/KnockOffTests/SequencingTests.cs` - 17 tests using `OnCallSequence`
- `src/Tests/KnockOffTests/SequenceValueOverloadTests.cs` - 15+ tests, notes that `ThenGet(value)` is concrete-only

**Searches Performed:**
- "OnCallSequence|OnGetSequence|OnSetSequence" - 10 test files using these APIs
- "Returns(" - Found in renderer; Returns() currently returns IMethodTracking
- "ThenGet(" - PropertyInterceptorRenderer has `ThenGet(value)` wrapper on concrete class
- "Verifiable()" - Returns tracking interface type (not builder)

### Concerns and Resolutions

**Concern 1: Builder vs Tracking Object Relationship Unclear** - RESOLVED

The lazy elevation logic example shows creating a new `MethodTrackingImpl` on elevation:
```csharp
var firstTracking = _interceptor._onCallTracking ?? new MethodTrackingImpl(_interceptor);
_interceptor._sequence.Add((_interceptor._onCall!, firstTracking));
```

But the "Tracking Instance Preservation" section says the original tracking must be preserved. These seem contradictory.

**Question:** Should the builder BE the tracking implementation (single object) or WRAP it (composition)? The implementation will differ significantly based on this choice.

**Suggestion:** Clarify whether:
- Builder IS tracking (implements all tracking methods directly)
- Builder WRAPS tracking (delegates to internal MethodTrackingImpl)
- Builder BECOMES tracking on elevation (object identity preserved)

**ARCHITECT RESPONSE:**

The builder IS the tracking implementation (single object). Specifically:

1. **Single Object Design**: `MethodCallBuilderImpl` implements both `IMethodCallBuilder<TCallback>` (which extends `IMethodTracking`) directly. It does NOT wrap a separate `MethodTrackingImpl`.

2. **Object Identity Preserved**: When `ThenCall` is called, the builder object that was returned by `OnCall` remains valid. The builder moves its own tracking data into the sequence:

```csharp
private sealed class MethodCallBuilderImpl : IMethodCallBuilder<TCallback>
{
    private readonly MethodInterceptor _interceptor;

    // Tracking state lives IN the builder
    internal int CallCount { get; private set; }
    private TArg _lastArg = default!;

    // IMethodTracking implementation
    public TArg LastArg => _lastArg;
    public void Reset() { CallCount = 0; _lastArg = default!; }
    public void Verify() => Verify(Times.AtLeastOnce);
    public void Verify(Times times) { /* implementation */ }
    public IMethodTracking Verifiable() { /* implementation */ return this; }

    public IMethodSequence<TCallback> ThenCall(TCallback callback)
    {
        if (_interceptor._sequence == null)
        {
            _interceptor._sequence = new List<(TCallback, MethodCallBuilderImpl)>();
            // Move THIS builder (with its tracking) into sequence as first element
            _interceptor._sequence.Add((_interceptor._onCall!, this));
            _interceptor._onCall = null;
            // Note: _onCallTracking stays pointing to this builder
        }
        // Add new callback with fresh builder for its tracking
        var nextBuilder = new MethodCallBuilderImpl(_interceptor);
        _interceptor._sequence.Add((callback, nextBuilder));
        return new MethodSequenceImpl(_interceptor);
    }
}
```

3. **Why This Works**: The user's reference `var tracking = stub.Method.OnCall(() => 1)` holds the builder. When `tracking.ThenCall(() => 2)` is called, the builder moves itself into the sequence. `tracking.LastArg` still works because the builder object is the same - it's now the first element in the sequence but the same instance.

---

**Concern 2: Verifiable() Return Type Breaking Fluent Chain** - NOT A CONCERN (User Guidance)

Current interface:
```csharp
public interface IMethodTracking { IMethodTracking Verifiable(); }
```

For fluent chaining to work (`OnCall().Verifiable().ThenCall()`), `Verifiable()` must return the builder. But inherited `Verifiable()` returns `IMethodTracking`, which doesn't have `ThenCall()`.

**Impact:** This pattern won't compile:
```csharp
stub.Method.OnCall(() => 1).Verifiable().ThenCall(() => 2); // Compile error
```

**USER GUIDANCE:** `Verifiable()` is terminal to the fluent chain. Users do not need to chain `ThenCall` after `Verifiable()`. This is not a concern.

**ARCHITECT CONFIRMATION:** Agreed. The typical usage patterns are:
- `stub.Method.OnCall(() => 1).ThenCall(() => 2).Verifiable()` - sequence then verify
- `stub.Method.OnCall(() => 1).Verifiable()` - single callback with verify

No builder override of `Verifiable()` is needed.

---

**Concern 3: Async Simplified Callbacks** - RESOLVED

For `Task<T>` methods, there are simplified `OnCall(Func<T>)` overloads (auto-wrapped in `Task.FromResult`). The plan mentions this in edge cases but doesn't specify:
- Does simplified OnCall also return a builder?
- What delegate type does ThenCall accept from simplified builder?

**ARCHITECT RESPONSE:**

Yes, simplified `OnCall(Func<T>)` also returns a builder. The design:

1. **Separate Builder Per Entry Point Type**: Each `OnCall` overload returns its own builder type:
   - `OnCall(Func<Task<T>>)` returns `IMethodCallBuilder<Func<Task<T>>>`
   - `OnCall(Func<T>)` returns `IMethodCallBuilder<Func<T>>` (simplified)

2. **ThenCall Accepts Same Delegate Type**: When chaining from simplified, `ThenCall` accepts the simplified delegate:
   ```csharp
   stub.GetValueAsync.OnCall(() => 42).ThenCall(() => 100);  // Both Func<int>
   ```

3. **Sequence Storage Consistency**: The sequence stores the delegate type that was used to enter. If user starts with simplified callback, the entire sequence uses simplified callbacks.

4. **Cannot Mix Delegate Types**: This is consistent with current behavior - you cannot start with `OnCallSequence(Func<Task<T>>)` and then `ThenCall(Func<T>)`.

**Interface definition:**
```csharp
// For Task<T> returning methods, generator produces:
public IMethodCallBuilder<Func<Task<T>>> OnCall(Func<Task<T>> callback);
public IMethodCallBuilder<Func<T>> OnCall(Func<T> callback);  // simplified
```

---

**Concern 4: Multi-Key Indexers** - RESOLVED

The plan shows `IIndexerGetBuilder<TKey, TValue>` for single-key indexers. What about indexers with multiple keys like `this[int x, int y]`? Current code handles multi-key indexers differently.

**Question:** Is single-key sufficient, or does the design need `IIndexerGetBuilderArgs<TArgs, TValue>` variant?

**ARCHITECT RESPONSE:**

**Single generic interface is sufficient.** Examining the codebase:

```csharp
// From InlineModelBuilder.cs lines 259-271:
var keyType = member.IndexerParameters.Count == 1
    ? member.IndexerParameters.GetArray()![0].Type
    : $"({string.Join(", ", member.IndexerParameters.Select(p => $"{p.Type} {p.Name}"))})";

var keyExpr = member.IndexerParameters.Count == 1
    ? member.IndexerParameters.GetArray()![0].Name
    : $"({string.Join(", ", member.IndexerParameters.Select(p => p.Name))})";
```

For multi-key indexers like `this[int x, int y]`:
- `KeyType` becomes a named tuple: `(int x, int y)`
- Callbacks use expanded parameters: `Func<int, int, TValue>`

The builder interface handles this naturally:
```csharp
// For this[int x, int y] -> TValue:
public interface IIndexerGetBuilder<TKey, TValue> : IIndexerGetTracking<TKey>
{
    IIndexerGetSequence<TKey, TValue> ThenGet(Func<int, int, TValue> callback);
}
```

Where `TKey` is `(int x, int y)` tuple but callbacks expand to individual parameters. The existing pattern in `IIndexerGetSequence<TKey, TValue>` already works this way - the generated `ThenGet` method accepts expanded parameters, not the tuple.

**No new interface variant needed.**

---

**Concern 5: No Verification Checkpoints** - RESOLVED

Phases 1-4 change interfaces and renderers. Phase 5 updates tests. If Phase 2 changes return types but builder classes aren't complete, generated code won't compile. Tests can't run until all phases complete.

**ARCHITECT RESPONSE:**

Restructured phases for earlier verification. See updated Implementation Steps section below.

**Key Changes:**
1. Phase 1-2: Add interfaces (non-breaking) + verify build
2. Phase 3: Methods - add builders, change return types, verify compile
3. Phase 4: Properties - same pattern, verify compile
4. Phase 5: Indexers - same pattern, verify compile
5. Phase 6: Remove old APIs + update tests
6. Phase 7: Final verification

Each phase now has explicit verification checkpoint.

---

**Concern 6: Returns().ThenCall() Semantics** - RESOLVED

When user writes `Returns(42).ThenCall(() => 100)`, what does first call return? 42 or 100?

**ARCHITECT RESPONSE:**

**First call returns 42, second call returns 100.** The semantics are:

```csharp
stub.Method.Returns(42).ThenCall(() => 100);

// Call 1: returns 42 (from Returns value, now first sequence element)
// Call 2: returns 100 (from ThenCall callback)
// Call 3+: StubException.SequenceExhausted (if strict) or returns default
```

When `ThenCall` is invoked:
1. The `Returns(42)` value is converted to a callback: `() => 42`
2. This callback becomes the first sequence element
3. The new callback `() => 100` becomes the second sequence element

**Implementation detail:**
```csharp
// In builder returned by Returns(value):
public IMethodSequence<TCallback> ThenCall(TCallback callback)
{
    if (_interceptor._sequence == null)
    {
        _interceptor._sequence = new List<(TCallback, MethodCallBuilderImpl)>();
        // Convert stored value to callback and add as first element
        TCallback valueCallback = () => _interceptor._returnsValue;
        _interceptor._sequence.Add((valueCallback, this));
        _interceptor._hasReturnsValue = false;
    }
    var nextBuilder = new MethodCallBuilderImpl(_interceptor);
    _interceptor._sequence.Add((callback, nextBuilder));
    return new MethodSequenceImpl(_interceptor);
}
```

This is consistent with the "lazy elevation" philosophy - `Returns(42)` alone repeats indefinitely, but `Returns(42).ThenCall(...)` converts to a sequence where 42 is returned exactly once.

---

### What Looks Good

1. Interface inheritance design - Builder extends tracking, backward compatible
2. Lazy elevation concept - Clean mental model
3. Edge cases coverage - Overloads, async, ref/out, init-only, reset
4. Migration guide - Clear before/after examples
5. Breaking change awareness - Properly documented
6. Codebase analysis - Correct files examined

### Recommendation

All concerns addressed. Ready for implementation contract.

---

## Implementation Contract

**Created:** 2026-01-29
**Approved by:** knockoff-developer

### In Scope

**Phase 1: New Interfaces (src/KnockOff/)**
- [ ] Create `IMethodCallBuilder.cs` with 3 interfaces:
  - `IMethodCallBuilder<TCallback> : IMethodTracking`
  - `IMethodCallBuilder<TCallback, TArg> : IMethodTracking<TArg>`
  - `IMethodCallBuilderArgs<TCallback, TArgs> : IMethodTrackingArgs<TArgs>`
- [ ] Create `IPropertyCallBuilder.cs` with 2 interfaces:
  - `IPropertyGetBuilder<TValue> : IPropertyGetTracking`
  - `IPropertySetBuilder<TValue> : IPropertySetTracking<TValue>`
- [ ] Create `IIndexerCallBuilder.cs` with 2 interfaces:
  - `IIndexerGetBuilder<TKey, TValue> : IIndexerGetTracking<TKey>`
  - `IIndexerSetBuilder<TKey, TValue> : IIndexerSetTracking<TKey, TValue>`
- [ ] **Checkpoint 1:** `dotnet build src/KnockOff.sln` passes

**Phase 2: Generator Changes - Methods**
- [ ] Modify `MethodInterceptorRenderer.cs`:
  - Replace `MethodTrackingImpl` with `MethodCallBuilderImpl`
  - Builder implements tracking interface directly (single object)
  - Add `ThenCall()` with lazy elevation logic
  - Change `OnCall()` return type to builder interface
  - Change `Returns()` return type to builder interface (for chaining)
  - Update simplified async `OnCall()` overloads to return builders
- [ ] Keep `OnCallSequence()` temporarily (for backward compat during transition)
- [ ] **Checkpoint 2:** `dotnet build src/KnockOff.sln` passes

**Phase 3: Generator Changes - Properties**
- [ ] Modify `PropertyInterceptorRenderer.cs`:
  - Replace `PropertyGetTrackingImpl` with `PropertyGetBuilderImpl`
  - Replace `PropertySetTrackingImpl` with `PropertySetBuilderImpl`
  - Add `ThenGet()` with lazy elevation (callback and value overloads)
  - Add `ThenSet()` with lazy elevation
  - Change `OnGet()` / `OnSet()` return types to builder interfaces
- [ ] Keep `OnGetSequence()` / `OnSetSequence()` temporarily
- [ ] **Checkpoint 3:** `dotnet build src/KnockOff.sln` passes

**Phase 4: Generator Changes - Indexers**
- [ ] Modify `IndexerInterceptorRenderer.cs`:
  - Replace `IndexerGetTrackingImpl` with `IndexerGetBuilderImpl`
  - Replace `IndexerSetTrackingImpl` with `IndexerSetBuilderImpl`
  - Add `ThenGet()` / `ThenSet()` with lazy elevation
  - Change `OnGet()` / `OnSet()` return types to builder interfaces
- [ ] Keep `OnGetSequence()` / `OnSetSequence()` temporarily
- [ ] **Checkpoint 4:** `dotnet build src/KnockOff.sln` passes

**Phase 5: Add Builder Elevation Tests**
- [ ] Create new test file `BuilderElevationTests.cs`:
  - `OnCall_WithoutThenCall_RepeatsIndefinitely`
  - `OnCall_ThenCall_CreatesSequence`
  - `OnCall_ThenCall_PreservesTrackingInstance`
  - `Returns_ThenCall_ValueBecomesFirstSequenceElement`
  - `OnGet_ThenGet_CreatesSequence`
  - `OnSet_ThenSet_CreatesSequence`
  - `IndexerOnGet_ThenGet_CreatesSequence`
- [ ] **Checkpoint 5:** `dotnet test src/KnockOff.sln` - new tests pass

**Phase 6: Remove Old APIs and Update Existing Tests**
- [ ] Remove from generators:
  - `OnCallSequence()` method generation
  - `OnGetSequence()` method generation
  - `OnSetSequence()` method generation
- [ ] Update `SequencingTests.cs`:
  - Change `OnCallSequence(...).ThenCall(...)` to `OnCall(...).ThenCall(...)`
- [ ] Update `SequenceValueOverloadTests.cs`:
  - Change `OnGetSequence(...).ThenGet(...)` to `OnGet(...).ThenGet(...)`
- [ ] Update any other test files using old sequence APIs
- [ ] **Checkpoint 6:** `dotnet test src/KnockOff.sln` - all tests pass

**Phase 7: Final Verification**
- [ ] Verify all three patterns work:
  - Standalone stubs (KnockOffAttribute on class)
  - Inline interface stubs (KnockOff<IInterface>)
  - Inline class stubs (KnockOff<ConcreteClass>)
- [ ] Verify edge cases:
  - Overloaded methods
  - Async simplified callbacks (Func<T> for Task<T>)
  - ref/out parameters
  - Multi-key indexers
- [ ] **Checkpoint 7:** Full test suite green, no warnings

### Explicitly Out of Scope

- **Inline delegate stubs** - Do not have OnCall/OnCallSequence pattern (use Returns only)
- **Events** - Not affected by this change (no sequence support)
- **Breaking the Verifiable() chain** - `Verifiable()` remains terminal
- **Mixing delegate types** - Cannot start with `OnCall(Func<Task<T>>)` then `ThenCall(Func<T>)`
- **New sequence functionality** - Only API simplification, no new behavior

### Verification Gates

1. **After Phase 1:** New interfaces exist, solution builds, no existing tests affected
2. **After Phase 2:** Methods use builder pattern, generated code compiles
3. **After Phase 3:** Properties use builder pattern, generated code compiles
4. **After Phase 4:** Indexers use builder pattern, generated code compiles
5. **After Phase 5:** New elevation tests pass, demonstrating ThenCall works from OnCall
6. **After Phase 6:** Old APIs removed, all existing tests pass with updated syntax
7. **After Phase 7:** All patterns work, edge cases verified, full test suite green

### Stop Conditions

If any of these occur, STOP and report:
- Out-of-scope test starts failing unexpectedly
- Object identity not preserved after ThenCall (tracking.LastArg fails)
- Generated code does not compile at any checkpoint
- Sequence behavior differs from current OnCallSequence behavior
- Architectural contradiction discovered during implementation

---

## Implementation Progress

---

## Completion Evidence
