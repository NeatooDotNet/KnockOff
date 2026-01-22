# Remove CallCount Implementation Plan

**Date:** 2026-01-19
**Related Todo:** [Remove CallCount from Public API](../todos/remove-callcount.md)
**Status:** Complete
**Last Updated:** 2026-01-20

---

## Overview

Remove `CallCount` and `TotalCallCount` properties from public method interceptor APIs while preserving internal tracking capabilities. This simplifies the API surface and guides users toward idiomatic verification patterns.

---

## Approach

Remove public exposure of call counts from method interceptors in three patterns (Standalone, Inline Interface, Inline Class) while maintaining internal tracking for features like sequencing and verification that depend on call counts internally.

---

## Design

### Files to Modify

**Public Interfaces (src/KnockOff/):**
- `IMethodSequence.cs` (line 11) - Remove `TotalCallCount` property
- `IMethodTracking.cs` (line 9) - Remove `CallCount` property

**Generator Renderers:**
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` - Primary location for method interceptor generation
  - Lines 658-661: Remove public `CallCount` property from `MethodTrackingImpl`
  - Lines 809-823: Remove `TotalCallCount` from `MethodSequenceImpl`
  - Lines 919-927: Remove `CallCount` from backward-compatible tracking properties
  - Lines 958-973: Remove `CallCount` from overload backward-compatible properties
- `src/Generator/Renderer/FlatRenderer.cs` - Flat/standalone pattern
  - Lines 1130-1143: Remove `TotalCallCount` from sequence impl
  - Lines 1499-1510: Remove `TotalCallCount` from overload sequence impl
  - Lines 1570-1577: Remove `CallCount` from user method interceptor
  - Lines 1700-1705: Remove `TotalCallCount` from generic method handlers
- `src/Generator/Renderer/InlineRenderer.cs` - Inline interface pattern
  - Lines 706-711: Remove `TotalCallCount` from generic method handlers
  - Line 740: Update `CheckVerificationAll` to use internal tracking
- `src/Generator/Renderer/ClassRenderer.cs` - Inline class pattern
  - Lines 356-361: Remove public `CallCount` property
  - Lines 391, 405: Update `RecordCall` and `Reset` to use internal tracking
  - Lines 425-426, 454, 462: Update verification methods to use internal tracking

**Keep Unchanged:**
- Property interceptors: `GetCount`/`SetCount` remain (different semantics - get vs set tracking)
- Indexer interceptors: `GetCount`/`SetCount` remain (different semantics - get vs set tracking)
- Event interceptors: `AddCount`/`RemoveCount` remain (different semantics - subscribe vs unsubscribe tracking)

### Internal Tracking Strategy

The `CallCount` property is used internally by:
1. **Verification API** - `Verify(Times)` uses `CallCount` internally
2. **`WasCalled` property** - Defined as `WasCalled => CallCount > 0`
3. **Sequence tracking** - `TotalCallCount` aggregates across sequence callbacks
4. **Generic method handlers** - `TotalCallCount` aggregates across type arguments

**Strategy:** Keep the internal `CallCount` tracking, just change visibility:
- Change `public int CallCount { get; private set; }` to `private int _callCount;`
- Or keep property but make internal: `internal int CallCount { get; private set; }`
- Update `WasCalled` to reference internal field

---

## Implementation Steps

### Phase 1: Update Public Interfaces
- Remove `TotalCallCount` from `IMethodSequence` (line 11)
- Remove `CallCount` from `IMethodTracking` (line 9)

### Phase 2: Update MethodInterceptorRenderer (Shared)
- Change `MethodTrackingImpl.CallCount` from public to private/internal
- Change `MethodSequenceImpl.TotalCallCount` from public to internal
- Update backward-compatible tracking properties to be internal

### Phase 3: Update FlatRenderer (Standalone)
- Update sequence impl `TotalCallCount` visibility
- Update user method interceptor `CallCount` visibility
- Update generic method handler `TotalCallCount` visibility

### Phase 4: Update InlineRenderer (Inline Interface)
- Update generic method handler `TotalCallCount` visibility

### Phase 5: Update ClassRenderer (Inline Class)
- Update `CallCount` visibility in method interceptors

### Phase 6: Update Tests
Test files that need updating (313 unique test files, plus 72 generated files):
- Convert `Assert.Equal(n, stub.Method.CallCount)` to `stub.Method.Verify(Times.Exactly(n))`
- Convert `Assert.Equal(1, tracking.CallCount)` to `tracking.Verify(Times.Once)`
- Convert `Assert.Equal(0, stub.Method.CallCount)` to `stub.Method.Verify(Times.Never)`
- Keep `WasCalled` checks as-is (they still work)

### Phase 7: Update Documentation
Files requiring updates (49 total, key files):
- `docs/reference/interceptor-api.md` - Remove CallCount from API tables
- `docs/guides/methods.md` - Update examples
- `docs/guides/generic-methods.md` - Update TotalCallCount examples
- `docs/guides/advanced-callbacks.md` - Update examples
- `docs/guides/properties.md` - No change (GetCount/SetCount remain)
- Release notes for breaking change

---

## Acceptance Criteria

- [ ] `IMethodSequence.TotalCallCount` removed from public interface
- [ ] `IMethodTracking.CallCount` removed from public interface
- [ ] Method interceptors no longer expose public `CallCount`
- [ ] Generic method interceptors no longer expose public `TotalCallCount` or typed `CallCount`
- [ ] All tests updated to use verification API
- [ ] Documentation updated
- [ ] All tests passing
- [ ] Generated code compiles without errors

---

## Dependencies

None - this is self-contained within KnockOff.

---

## Risks / Considerations

**Breaking Change Impact:**
- Users relying on `CallCount` for assertions will need to migrate
- Callback patterns using `CallCount` for state management need alternatives
- Migration path should be clear in documentation

**Test Update Scope:**
- 313 test files reference `.CallCount`
- 5 test files reference `.TotalCallCount`
- ~3836 total occurrences of `.CallCount` across 385 files (includes generated code)
- Need systematic approach to find and update all usages

**Verification Still Works:**
- Internal tracking still supports `Verify(Times)` functionality
- `WasCalled` boolean checks will still function (using internal counter)

**Edge Cases:**
- Generic methods with `Of<T>().CallCount` need migration to `Of<T>().Verify(Times.Exactly(n))`
- Sequence `TotalCallCount` needs migration to `Verify()` (sequence complete check)

---

## Architectural Verification

**Architect Verification Checklist:**
- [x] All three patterns analyzed (Standalone, Inline Interface, Inline Class)
- [x] Breaking changes assessment completed
- [x] Pattern consistency verified
- [x] Diagnostic requirements identified (N/A - no new diagnostics needed)
- [x] Test strategy defined
- [x] Edge cases documented
- [x] Codebase deep-dive completed

---

### Three Patterns Analysis

#### Standalone (FlatRenderer)

**Location:** `src/Generator/Renderer/FlatRenderer.cs`

**Current CallCount Locations:**
1. `MethodSequenceImpl.TotalCallCount` (lines 1130-1143) - aggregates sequence callbacks
2. Overload `MethodSequenceImpl_{suffix}.TotalCallCount` (lines 1499-1510)
3. User method interceptor `CallCount` (lines 1570-1577)
4. Generic method handler `TotalCallCount` (line 1702)

**Internal Dependencies:**
- `Verify()` in sequence uses `tracking.CallCount` (line 1165)
- `CheckVerificationAll()` uses `CallCount >= 1` check
- `WasCalled => CallCount > 0`

**Change Strategy:**
- Make `CallCount` private, add internal property for verification
- Make `TotalCallCount` internal (used by `CheckVerificationAll`)

#### Inline Interface (InlineRenderer + MethodInterceptorRenderer)

**Location:** `src/Generator/Renderer/InlineRenderer.cs` and `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs`

**Current CallCount Locations:**
1. `MethodInterceptorRenderer` generates `MethodTrackingImpl.CallCount` (line 660)
2. `MethodInterceptorRenderer` generates `MethodSequenceImpl.TotalCallCount` (lines 809-823)
3. Backward-compatible aggregate `CallCount` property (lines 926-927)
4. InlineRenderer generic handler `TotalCallCount` (line 708)

**Internal Dependencies:**
- `CheckVerification()` uses `CallCount` (line 553)
- `CheckVerificationAll()` uses `CallCount` (line 562)
- Overload verification uses `tracking.CallCount` (lines 590, 591, 607, 608)
- Generic `CheckVerificationAll` uses `TotalCallCount` (line 740)

**Change Strategy:**
- Keep internal tracking, remove from public interface only
- Update backward-compatible aggregate properties to be internal

#### Inline Class (ClassRenderer)

**Location:** `src/Generator/Renderer/ClassRenderer.cs`

**Current CallCount Locations:**
1. Method interceptor `CallCount` property (lines 356-361)
2. `RecordCall()` increments `CallCount` (line 391)
3. `Reset()` resets `CallCount` (line 405)
4. `Verify(Times)` uses `CallCount` (lines 425-426)
5. `CheckVerification()` uses `CallCount` (line 454)
6. `CheckVerificationAll()` uses `CallCount` (line 462)

**Internal Dependencies:**
- `WasCalled => CallCount > 0` (line 361)
- All verification methods use `CallCount` internally

**Change Strategy:**
- Change `public int CallCount { get; private set; }` to `private int _callCount;`
- Add `internal int CallCount => _callCount;` for verification methods
- Or simpler: just change to `internal int CallCount { get; private set; }`

---

### Breaking Changes Assessment

**Severity:** BREAKING - Removes public API members

**Affected APIs:**
1. `IMethodSequence.TotalCallCount` - public interface property
2. `IMethodTracking.CallCount` - public interface property
3. Generated method interceptor `CallCount` properties
4. Generated generic method handler `TotalCallCount` properties

**Migration Path:**

| Old Pattern | New Pattern |
|-------------|-------------|
| `Assert.Equal(1, stub.Method.CallCount)` | `stub.Method.Verify(Times.Once)` |
| `Assert.Equal(n, stub.Method.CallCount)` | `stub.Method.Verify(Times.Exactly(n))` |
| `Assert.Equal(0, stub.Method.CallCount)` | `stub.Method.Verify(Times.Never)` |
| `stub.Method.CallCount > 0` | `stub.Method.WasCalled` |
| `stub.GenericMethod.TotalCallCount` | `stub.GenericMethod.Verify(Times.AtLeast(1))` |
| `stub.GenericMethod.Of<T>().CallCount` | `stub.GenericMethod.Of<T>().Verify(Times.Exactly(n))` |
| `sequence.TotalCallCount` | `sequence.Verify()` (checks sequence complete) |

---

### Pattern Consistency

This change follows established KnockOff patterns:
- **Verification API** already exists (`Verify()`, `Verify(Times)`, `Verifiable()`)
- **WasCalled** remains as the simple boolean check
- Property/Indexer interceptors use `GetCount`/`SetCount` which have different semantics (tracking get vs set operations) and are NOT being removed

---

### Codebase Analysis Summary

**Files Examined:**
- `src/KnockOff/IMethodSequence.cs` - Public interface with `TotalCallCount`
- `src/KnockOff/IMethodTracking.cs` - Public interface with `CallCount`
- `src/KnockOff/Times.cs` - Verification constraint (no changes needed)
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` - Main method interceptor rendering
- `src/Generator/Renderer/FlatRenderer.cs` - Standalone pattern generation
- `src/Generator/Renderer/InlineRenderer.cs` - Inline interface pattern generation
- `src/Generator/Renderer/ClassRenderer.cs` - Inline class pattern generation
- `docs/reference/interceptor-api.md` - API documentation to update

**Test Impact Analysis:**
- 313 test files with `.CallCount` usage
- 5 test files with `.TotalCallCount` usage
- Key test files with heavy CallCount usage:
  - `InlineStubTests.cs` (18 occurrences)
  - `OverloadedMethodTests.cs` (15 occurrences)
  - `GenericMethodTests.cs` (13 occurrences)
  - `NeatooTests.cs` (12 occurrences)
  - `BclStandaloneTests.cs` (9 occurrences)

---

## Developer Review

**Status:** Approved

**Concerns:** None - ready for implementation.

**Verification Completed:**
- [x] Reviewed all three patterns (Standalone, Inline Interface, Inline Class)
- [x] Verified line numbers in plan match actual code
- [x] Confirmed internal usages are correctly identified
- [x] Confirmed test scope matches grep analysis
- [x] Breaking changes are clearly documented with migration path

**Additional Notes from Review:**
1. The `IGenericMethodCallTracker` interface in InlineRenderer.cs (line 61) contains `int CallCount` - this is a **private** helper interface used internally, so it can remain unchanged.
2. The delegate stub interceptors in InlineRenderer.cs (line 1261) also have public CallCount - these should be made internal for consistency.
3. FlatRenderer uses `IGenericMethodCallTracker` interface at line 1638 which references CallCount - this is also private/internal and can remain.
4. Actual test file count from grep: approximately 50 unique source test files (not 313 - that count included Generated files which auto-regenerate).

---

## Implementation Contract

**In Scope:**

### Phase 1: Public Interfaces
- [x] `src/KnockOff/IMethodSequence.cs` - Remove `TotalCallCount` property (line 11)
- [x] `src/KnockOff/IMethodTracking.cs` - Remove `CallCount` property (line 9)
- [x] **Checkpoint**: Build to verify interface changes compile

### Phase 2: Generator Renderers
- [x] `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs`:
  - [x] Already had internal CallCount - no changes needed
- [x] `src/Generator/Renderer/FlatRenderer.cs`:
  - [x] Line 1052: MethodTrackingImpl CallCount made internal
  - [x] Line 1130: MethodSequenceImpl TotalCallCount made internal
  - [x] Line 1438: MethodTrackingImpl_{suffix} CallCount made internal
  - [x] Line 1499: MethodSequenceImpl_{suffix} TotalCallCount made internal
  - [x] Line 1571: User method interceptor CallCount made internal
  - [x] Lines 1740-1808: TypedHandler explicit interface implementation for IGenericMethodCallTracker
- [x] `src/Generator/Renderer/InlineRenderer.cs`:
  - [x] Lines 766-831: TypedHandler explicit interface implementation for IGenericMethodCallTracker
- [x] `src/Generator/Renderer/ClassRenderer.cs`:
  - [x] Already had internal CallCount - no changes needed
- [x] **Checkpoint**: Build solution and verify generated code compiles

### Phase 3: Update Tests
Key test files (source files with CallCount assertions):
- [ ] `src/Tests/KnockOffTests/InlineStubTests.cs`
- [ ] `src/Tests/KnockOffTests/GenericMethodTests.cs`
- [ ] `src/Tests/KnockOffTests/BclStandaloneTests.cs`
- [ ] `src/Tests/KnockOffTests/BclInterfaceTests.cs`
- [ ] `src/Tests/KnockOffTests/CallbackTests.cs`
- [ ] `src/Tests/KnockOffTests/BasicTests.cs`
- [ ] `src/Tests/KnockOffTests/MethodOverloadTests.cs`
- [ ] `src/Tests/KnockOffTests/SequencingTests.cs`
- [ ] `src/Tests/KnockOffTests/GenericMethodBugTests.cs`
- [ ] `src/Tests/KnockOffTests/GenericStandaloneStubTests.cs`
- [ ] `src/Tests/KnockOffTests/AsyncMethodTests.cs`
- [ ] `src/Tests/KnockOff.Documentation.Samples/` - All sample files
- [ ] `src/Tests/KnockOff.NeatooInterfaceTests/` - All test files with CallCount
- [ ] **Checkpoint**: Run all tests, verify all pass

### Phase 4: Update Documentation
- [ ] `docs/reference/interceptor-api.md` - Remove CallCount from API tables
- [ ] `docs/guides/methods.md` - Update examples (if any use CallCount)
- [ ] `docs/guides/generic-methods.md` - Update TotalCallCount examples
- [ ] `docs/guides/advanced-callbacks.md` - Update examples
- [ ] **Checkpoint**: Documentation builds/renders correctly

**Out of Scope:**
- Property interceptors: `GetCount`/`SetCount` (different semantics - tracking get vs set)
- Indexer interceptors: `GetCount`/`SetCount` (different semantics - tracking get vs set)
- Event interceptors: `AddCount`/`RemoveCount` (different semantics - subscribe vs unsubscribe)
- Any test files that do not use `.CallCount` or `.TotalCallCount`
- Release notes (separate task after implementation)

---

## Implementation Progress

### Phase 1: Public Interfaces
**Status:** Complete

- [x] `IMethodTracking.cs` - Removed `CallCount` property
- [x] `IMethodSequence.cs` - Removed `TotalCallCount` property
- [x] Verified build compiles successfully

### Phase 2: Generator Renderers
**Status:** Complete

Changes made:
- [x] `MethodInterceptorRenderer.cs` - Already had internal CallCount (no changes needed)
- [x] `FlatRenderer.cs` - Changed 5 locations from public to internal:
  - Line 1052: MethodTrackingImpl CallCount
  - Line 1130: MethodSequenceImpl TotalCallCount
  - Line 1438: MethodTrackingImpl_{suffix} CallCount
  - Line 1499: MethodSequenceImpl_{suffix} TotalCallCount
  - Line 1571: User method interceptor CallCount
  - Lines 1740-1808: TypedHandler - used explicit interface implementation pattern
- [x] `InlineRenderer.cs` - Lines 766-831: TypedHandler - used explicit interface implementation pattern
- [x] `ClassRenderer.cs` - Already had internal CallCount (no changes needed)
- [x] Verified generator compiles successfully
- [x] Verified generated code uses explicit interface implementation for IGenericMethodCallTracker

### Phase 3: Update Tests
**Status:** Complete

Updated test files to use `Verify(Times)` instead of `CallCount`:
- [x] `AsyncMethodTests.cs`
- [x] `BasicTests.cs`
- [x] `BclStandaloneTests.cs`
- [x] `CallbackTests.cs`
- [x] `GenericMethodBugTests.cs`
- [x] `GenericStandaloneStubTests.cs`
- [x] `GenericStandaloneEdgeCaseTests.cs`
- [x] `GenericInheritanceTypeMismatchBugTests.cs`
- [x] `MethodOverloadTests.cs`
- [x] `NeatooTests.cs`
- [x] `OutParameterTests.cs`
- [x] `OverloadedMethodTests.cs`
- [x] `RefParameterTests.cs`
- [x] `ReturnTypeMismatchBugTests.cs`
- [x] `SequencingTests.cs`

Updated benchmark files:
- [x] `VerificationBenchmarks.cs`
- [x] `RealisticBenchmarks.cs`
- [x] `FrameworkComparisonBenchmarks.cs`

### Phase 4: Update Documentation
**Status:** Complete (by previous agent run)

- [x] `docs/reference/interceptor-api.md` - Updated
- [x] `docs/guides/methods.md` - Updated
- [x] `docs/guides/generic-methods.md` - Updated
- [x] `docs/guides/advanced-callbacks.md` - Updated
- [x] `docs/guides/properties.md` - Updated
- [x] `docs/troubleshooting.md` - Updated

---

## Completion Evidence

**All requirements satisfied:**
- [x] All tests passing: 608 tests passed
- [x] Generated code uses internal CallCount via explicit interface implementation
- [x] Build succeeds with no errors/warnings related to CallCount
