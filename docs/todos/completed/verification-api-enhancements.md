# Verification API Enhancements

**Status:** Complete
**Priority:** High
**Created:** 2026-01-18
**Last Updated:** 2026-01-18

---

## Problem

Feedback from developers reviewing KnockOff: "it'd be nice to have .Verify()". The existing verification system was confusing:

1. `IMethodTracking` (returned by `OnCall(callback)`) had no `Verify()` method
2. Property interceptors had no verification capability
3. The relationship between Times constraints and verification wasn't clear
4. Users couldn't easily verify that callbacks were invoked

## Solution

Added `Verify(Times times = default)` method to `IMethodTracking` interface, with default behavior of "at least once". Added `Verify()` to property interceptors to check configured OnGet/OnSet were called.

---

## Plans

- [Verification API Enhancement Design](../plans/completed/verification-api-enhancement.md)

---

## Tasks

- [x] Add `Times.AtLeastOnce` convenience property
- [x] Add `Verify(Times times = default)` to `IMethodTracking` interface
- [x] Add `Verify(Times)` to generated `MethodTrackingImpl` classes (shared renderer)
- [x] Add `Verify()` to property interceptors in FlatRenderer
- [x] Add `Verify()` to property interceptors in InlineRenderer
- [x] Add `Verify(Times)` to TypedHandlerClass in FlatRenderer (generic methods)
- [x] Add `Verify(Times)` to TypedHandlerClass in InlineRenderer (generic methods)
- [x] Add `Verify(Times)` to ClassRenderer method interceptors
- [x] Add `Verify(Times)` to UserMethodInterceptorClass in FlatRenderer
- [x] Add `Verify(Times)` to GroupMethodTrackingImpl in FlatRenderer (overload groups)
- [x] Fix `Verify()` in GroupMethodSequenceImpl to handle Forever correctly
- [x] Build and test all changes

---

## Progress Log

**2026-01-18**: Implemented Approach 1 (Minimal Extension) after exploring 3 design options with user. Added Verify methods across all three stub patterns (Stand-Alone, Inline Interface, Inline Class). Code review caught missing implementations in overload group paths - fixed those. All 594 tests pass.

---

## Results / Conclusions

### What Was Implemented

**Runtime Types:**
- `Times.AtLeastOnce` - Convenience property equivalent to `AtLeast(1)`
- `IMethodTracking.Verify(Times times = default)` - Defaults to AtLeastOnce

**Generated Code:**
- `MethodTrackingImpl.Verify(Times)` in all renderers
- Property interceptor `Verify()` - checks OnGet/OnSet were called if configured
- TypedHandlerClass `Verify(Times)` for generic methods
- UserMethodInterceptorClass `Verify(Times)` for user-defined methods

### API Usage

```csharp
// Method tracking verification
var tracking = stub.GetById.OnCall((ko, id) => user);
Assert.True(tracking.Verify());              // Defaults to AtLeastOnce
Assert.True(tracking.Verify(Times.Once));    // Exactly once

// Property verification
stub.Name.OnGet = (ko) => "Test";
Assert.True(stub.Name.Verify());  // Checks OnGet was called

// Existing APIs unchanged
Assert.True(stub.GetById.Verify());  // Interceptor-level
stub.VerifyAll();                    // Throws if any fail
```

### Key Design Decisions

1. **Default to AtLeastOnce** - Most common verification use case
2. **Property Verify vacuous pass** - Only verifies callbacks that were configured
3. **Consistent across patterns** - Works identically for all 3 stub patterns
4. **Non-breaking** - All changes are additive
