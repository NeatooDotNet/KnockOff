# Add Verifiable Support for User-Defined Methods

**Status:** Complete
**Priority:** Medium
**Created:** 2026-01-30
**Last Updated:** 2026-01-31

---

## Problem

User-defined methods in stand-alone stubs have `.Verifiable()` methods that exist for API compatibility but don't actually track verifiable state. The methods just return `this`:

```csharp
public global::KnockOff.IMethodTracking Verifiable() => this;
public global::KnockOff.IMethodTracking Verifiable(global::KnockOff.Times times) => this;
```

There's no `_isVerifiable` field and no `IsVerifiable` property. This means:
- You can call `.Verify()` directly on a user-defined method interceptor
- But calling `.Verifiable()` and then `stub.Verify()` won't include that method in aggregate verification

Regular interface methods (via shared `MethodInterceptorRenderer`) have proper Verifiable support, but user-defined methods (via `RenderUserMethodInterceptorClass` in FlatRenderer.cs) do not.

## Solution

Add proper `_isVerifiable` state tracking to user-defined method interceptors:
1. Add `_isVerifiable` field and `_verifiableTimes` field
2. Update `Verifiable()` methods to set state instead of just returning `this`
3. Add `IsVerifiable` property
4. Add `GetVerificationFailure()` method for aggregate verification
5. Include user-defined methods in `Stub.Verify()` aggregation

---

## Plans

- [User-Defined Method Verifiable Implementation](../plans/user-method-verifiable-implementation.md)

---

## Tasks

- [x] Add `_isVerifiable` and `_verifiableTimes` fields to user method interceptor
- [x] Update `Verifiable()` to set `_isVerifiable = true`
- [x] Update `Verifiable(Times)` to set both fields
- [x] Add `CheckVerification()` method (internal, for Stub.Verify() aggregation)
- [x] Update `Stub.Verify()` to include user-defined method interceptors
- [x] Add tests for user-defined method Verifiable behavior

**Note:** Generic method handlers (for generic interface methods with user implementations) are out of scope - they use `GenericMethodHandler`, not the user method interceptor pattern.

---

## Progress Log

**2026-01-31**: Architect completed plan review and verification. Plan is now "Under Review (Developer)".

**2026-01-31**: Developer review completed. **Concerns Raised:**
- Critical gap: `RenderVerifyMethods` is only called when `unit.MethodGroups.Count > 0`. Stubs with ONLY user methods (no regular interface methods) would not get `Verify()`/`VerifyAll()` methods at all. The plan adds user methods to a loop inside a method that is never called for such stubs.
- Plan needs architect to address the guard condition at line 2114 of `FlatRenderer.cs`.
- See plan for full review details.

**2026-01-31**: Architect addressed critical concern #1 (conditional rendering gap):
- Investigated guard condition at line 2114 in `FlatRenderer.cs`
- Confirmed evidence: `StrictModeUserMethodStub.g.cs` has user methods and a property but NO `Verify()`/`VerifyAll()` methods
- Added "Code Change #4: Expand Guard Condition" to the plan with explicit code to check all verifiable member types
- Updated Implementation Steps, Acceptance Criteria, and Test Scenarios to include guard condition fix
- Plan status updated to "Under Review (Developer)" for re-review

---

## Results / Conclusions

**Implementation completed:** 2026-01-31

### Summary

Successfully implemented Verifiable support for user-defined methods in standalone stubs. User method interceptors now properly track verifiable state and participate in `Stub.Verify()` aggregation.

### Changes Made

1. **FlatRenderer.cs - RenderUserMethodInterceptorClass**:
   - Added `_isVerifiable` and `_verifiableTimes` fields
   - Added `CheckVerification()` method for Stub.Verify() aggregation
   - Updated `Verifiable()` methods to set state instead of just returning `this`

2. **FlatRenderer.cs - RenderStandardMembers**:
   - Expanded guard condition to call `RenderVerifyMethods` for stubs with user methods, properties, or events (not just methods)
   - Note: Indexers excluded due to a separate bug with container accessor paths

3. **FlatRenderer.cs - RenderVerifyMethods**:
   - Added user method interceptors to `Verify()` aggregation loop
   - User methods intentionally NOT added to `VerifyAll()` (they are always "configured")

4. **UserMethodVerificationTests.cs** (new file):
   - 18 tests covering all scenarios for user method verification

### Key Decisions

- **User methods excluded from VerifyAll()**: User-defined methods are always "configured" (the user provides the implementation), so including them in VerifyAll() would require every user method to be called. Only `Stub.Verify()` includes user methods that have been explicitly marked with `.Verifiable()`.

- **Indexers excluded from guard condition expansion**: Discovered a latent bug where indexer container accessor paths are incorrect in RenderVerifyMethods. Created TODO comment; requires separate fix.

### Test Results

All 18 new tests pass across net8.0, net9.0, and net10.0. Full test suite (955-956 tests) passes with no regressions.
