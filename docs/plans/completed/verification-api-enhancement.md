# Verification API Enhancement Design

**Date:** 2026-01-18
**Related Todo:** [Verification API Enhancements](../todos/completed/verification-api-enhancements.md)
**Status:** Complete
**Last Updated:** 2026-01-18

---

## Overview

Design for adding `Verify(Times)` support to `IMethodTracking` and property interceptors, making verification more discoverable and consistent across all KnockOff stub patterns.

---

## Approach

**Approach 1 (Minimal Extension)** was chosen over more complex alternatives:

1. **Approach 1: Minimal Extension** ✓ Selected
   - Add `Verify(Times times = default)` to `IMethodTracking`
   - Add `Verify()` to property interceptors
   - Zero breaking changes, simple implementation

2. **Approach 2: Comprehensive Verification** (Rejected)
   - Would add `VerifyGet(Times)` and `VerifySet(Times)` separately
   - Over-engineered for the use case

3. **Approach 3: Fluent Verification Builder** (Rejected)
   - Would add `Expect().CalledOnce().Verify()` fluent API
   - Too complex, potential confusion with two verification styles

---

## Design

### Times Struct Enhancement

```csharp
public readonly struct Times
{
    // Existing: Once, Twice, Exactly(n), Forever, AtLeast(n), AtMost(n), Never

    // NEW: Convenience for default verification
    public static Times AtLeastOnce => new(1, TimesKind.AtLeast);
}
```

### IMethodTracking Interface

```csharp
public interface IMethodTracking
{
    int CallCount { get; }
    bool WasCalled { get; }
    void Reset();

    // NEW: Defaults to AtLeastOnce
    bool Verify(Times times = default);
}
```

### Generated MethodTrackingImpl

```csharp
public bool Verify(global::KnockOff.Times times = default) =>
    (times == default ? global::KnockOff.Times.AtLeastOnce : times).Verify(CallCount);
```

### Generated Property Interceptor

```csharp
public bool Verify() =>
    (OnGet == null || GetCount > 0) &&
    (OnSet == null || SetCount > 0);
```

---

## Implementation Steps

1. ✓ Add `Times.AtLeastOnce` property to `Times.cs`
2. ✓ Add `Verify(Times)` method signature to `IMethodTracking.cs`
3. ✓ Update `MethodInterceptorRenderer.cs` to generate `Verify` in `MethodTrackingImpl`
4. ✓ Update `FlatRenderer.cs`:
   - Init-only property interceptors: `Verify() => true`
   - Regular property interceptors: Check OnGet/OnSet
   - `RenderMethodTrackingImpl`: Add Verify
   - `RenderGroupMethodTrackingImpl`: Add Verify
   - `RenderGroupMethodSequenceImpl`: Fix Forever handling
   - `RenderUserMethodInterceptorClass`: Add Verify
   - `RenderTypedHandlerClass`: Add Verify
5. ✓ Update `InlineRenderer.cs`:
   - Property interceptors: Add Verify
   - `RenderTypedHandlerClass`: Add Verify
6. ✓ Update `ClassRenderer.cs`:
   - Method interceptors: Add Verify

---

## Acceptance Criteria

- [x] `tracking.Verify()` returns true if called at least once
- [x] `tracking.Verify(Times.Once)` checks exactly once
- [x] Property `Verify()` checks configured OnGet/OnSet were called
- [x] Works for all 3 patterns (Stand-Alone, Inline Interface, Inline Class)
- [x] All existing tests pass
- [x] No breaking changes

---

## Dependencies

- None - all changes are additive

---

## Risks / Considerations

### Semantic Difference: Interceptor vs Tracking Verify

There are two `Verify()` methods with slightly different semantics:

1. **Interceptor-level `Verify()`** (no parameters)
   - Verifies **all** callbacks in the sequence
   - For `Forever`, infers "at least once"

2. **Tracking-level `Verify(Times)`** (with optional parameter)
   - Verifies **this specific callback**
   - Defaults to `AtLeastOnce`

The difference is subtle but documented in XML comments. `Times.Forever.Verify(callCount)` always returns true, but the interceptor-level `Verify()` treats Forever as "at least once".

### Property Verify Vacuous Pass

Property `Verify()` returns true if:
- OnGet is null (not configured), OR GetCount > 0
- AND OnSet is null (not configured), OR SetCount > 0

This means unconfigured callbacks pass vacuously, which aligns with "verify what you configured" principle.
