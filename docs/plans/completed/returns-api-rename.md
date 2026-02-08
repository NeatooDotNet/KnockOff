# Returns API Rename Design Plan

**Date:** 2026-01-27
**Related Todo:** [Returns API Rename](../todos/returns-api-rename.md)
**Status:** Complete
**Last Updated:** 2026-01-27

---

## Overview

Rename the `OnCall(value)` method to `Returns(value)` for single-signature methods to improve API clarity. The current naming `stub.Add.OnCall(10)` reads as "when argument is 10" rather than "return 10", which is confusing for users familiar with NSubstitute patterns.

---

## Approach

This is a straightforward rename operation in the code generation layer:
1. Change the generated method name from `OnCall` to `Returns` in `MethodInterceptorRenderer.cs`
2. Remove `OnCall(value)` entirely (no deprecation period - pre-1.0)
3. Keep `OnCall(callback)` unchanged - the name makes sense for callbacks
4. Update documentation and tests to use new naming

---

## Design

### API Change

**Methods - Before:**
```csharp
stub.GetUser.OnCall(user);           // Confusing - looks like argument matching
stub.GetUser.OnCall(id => user);     // Clear - callback runs on call
```

**Methods - After:**
```csharp
stub.GetUser.Returns(user);          // Clear - returns this value
stub.GetUser.OnCall(id => user);     // Unchanged - callback still makes sense
```

**Delegates - Before:**
```csharp
stub.Interceptor.OnCall("result");   // Confusing - looks like argument matching
stub.Interceptor.OnCall(x => "result"); // Clear - callback runs on call
```

**Delegates - After:**
```csharp
stub.Interceptor.Returns("result");  // Clear - returns this value
stub.Interceptor.OnCall(x => "result"); // Unchanged - callback still makes sense
```

### Files to Modify

#### Generator Files (Primary Changes)

1. **`src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs`**
   - Line ~183-184: Change `OnCall({valueStorageType} value)` to `Returns({valueStorageType} value)`
   - Update XML doc comments to reflect the new name
   - All storage fields, tracking, and internal logic remain unchanged (implementation detail)

2. **`src/Generator/Renderer/InlineRenderer.cs`**
   - Line ~1294: Change delegate value overload from `OnCall` to `Returns`

#### Test Files (Must Update)

| File | Changes |
|------|---------|
| `MethodValueOverloadTests.cs` | ~15 occurrences of `.OnCall(value)` |
| `ThreePatternValueOverloadTests.cs` | ~12 occurrences of `.OnCall(value)` |
| `SequenceValueOverloadTests.cs` | Mutual exclusivity tests reference OnCall(value) |
| `DelegateValueOverloadTests.cs` | Delegate value overload syntax (`.Interceptor.OnCall(value)`) |
| `InlineStubTests.cs` | Delegate interceptor tests |
| `OpenGenericInlineStubTests.cs` | Generic delegate tests |
| `EdgeCaseValueOverloadTests.cs` | Edge case tests |
| `AsyncCallbackSimplificationTests.cs` | Some mutual exclusivity tests |

#### Documentation Files (Must Update)

| File | Changes |
|------|---------|
| `ReadmeComparisonSamples.cs` | README comparison code samples |
| `GettingStartedSamples.cs` | Getting started examples |
| `MethodsSamples.cs` | Method documentation samples |
| `SkillSamples.cs` | Skill documentation samples |
| `ReadmeSamples.cs` | README samples |
| `UserMethodsSamples.cs` | User methods samples |
| `DelegatesSamples.cs` | Delegate documentation samples |
| `PatternsSamples.cs` | Pattern samples with delegate examples |
| `docs/guides/delegates.md` | Delegate guide documentation (line ~140) |
| `docs/guides/methods.md` | Methods guide (line ~60) |
| `docs/guides/stub-overrides.md` | User methods guide (lines ~161-162) |
| `docs/guides/source-delegation.md` | Source delegation guide (line ~180) |
| `docs/guides/stub-patterns.md` | Stub patterns guide |
| `README.md` | Comparison sections (methods AND delegates) |

### Internal Field Renames

For code maintainability and consistency, internal field names should also be renamed:

| Old Name | New Name |
|----------|----------|
| `_onCallValue` | `_returnsValue` |
| `_hasOnCallValue` | `_hasReturnsValue` |
| `_onCallValueTracking` | `_returnsValueTracking` |

### Unchanged Components

- `OnCall(callback)` - remains for all callback-based configuration
- `OnCallSequence()` - remains for sequence chaining
- All tracking interfaces and behavior

---

## Architectural Verification

### Three Patterns Analysis

| Pattern | Impact | Notes |
|---------|--------|-------|
| **Standalone** | Rename applies | `stub.GetUser.OnCall(value)` becomes `stub.GetUser.Returns(value)` |
| **Inline Interface** | Rename applies | Same as standalone - uses shared `MethodInterceptorRenderer` |
| **Inline Class** | N/A | Does not have value overloads for methods (only callback syntax) |

### Breaking Changes

**Yes** - This is a breaking API change. `OnCall(value)` will no longer compile.

**Mitigation**: Pre-1.0, breaking changes are acceptable. No deprecation period needed.

### Pattern Consistency

The rename maintains consistency:
- Properties already use `OnGet(value)` - clear "getter returns value" semantics
- `Returns(value)` follows the same pattern - "method returns value"
- `OnCall(callback)` remains for callbacks - "on call, execute this callback"

### Codebase Analysis

**Files Examined:**

| File | Purpose |
|------|---------|
| `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` | Primary rendering logic for method interceptors |
| `src/Generator/Model/Shared/UnifiedMethodInterceptorModel.cs` | Model definition - no changes needed |
| `src/Tests/KnockOffTests/MethodValueOverloadTests.cs` | Test patterns for value overloads |
| `src/Tests/KnockOffTests/ThreePatternValueOverloadTests.cs` | Three-pattern verification tests |
| `src/Tests/KnockOff.Documentation.Samples/ReadmeComparisonSamples.cs` | Documentation samples |

**Key Discovery:** The `OnCall(value)` method is generated in one primary location (`MethodInterceptorRenderer.cs` line ~184) with a secondary location for delegate stubs in `InlineRenderer.cs` (line ~1294). Internal storage uses `_onCallValue`, `_hasOnCallValue`, and `_onCallValueTracking` field names - these should also be renamed for consistency.

### Architect Verification Checklist

- [x] All three patterns analyzed (Standalone, Inline Interface, Inline Class)
- [x] Breaking changes assessment completed (Yes - breaking, pre-1.0 acceptable)
- [x] Pattern consistency verified (follows OnGet/OnSet naming convention)
- [x] Diagnostic requirements identified (N/A - compile-time name change)
- [x] Test strategy defined (phase-based: generator, tests, docs)
- [x] Edge cases documented (delegate stubs, internal field renames)
- [x] Codebase deep-dive completed (files examined above)

**Additional Files Examined During Re-Review:**
- `src/Generator/Renderer/ClassRenderer.cs` - Confirmed Inline Class pattern does NOT use shared renderer, only has `OnCall(callback)` at line 152
- `docs/guides/methods.md`, `docs/guides/stub-overrides.md`, `docs/guides/source-delegation.md` - Found additional value overload usage

---

## Developer Review

**Status:** Approved

**Concerns:** None - plan was clear and complete. Implementation proceeded smoothly.

---

## Implementation Contract

**In Scope:**
- [x] Rename `OnCall(TValue value)` to `Returns(TValue value)` in `MethodInterceptorRenderer.cs`
- [x] Rename `OnCall(TValue value)` to `Returns(TValue value)` in `InlineRenderer.cs` (delegate value overload)
- [x] Rename internal field names: `_onCallValue` → `_returnsValue`, `_hasOnCallValue` → `_hasReturnsValue`, `_onCallValueTracking` → `_returnsValueTracking`
- [x] Update XML documentation comments for the renamed method
- [x] Update all test files that use `.OnCall(value)` syntax
- [x] Update all documentation sample files
- [x] Update README.md comparison sections

**Out of Scope:**
- `OnCall(callback)` method - remains unchanged
- `OnCallSequence()` method - remains unchanged
- Model classes - no changes needed
- Builder classes - no changes needed

---

## Implementation Steps

### Phase 1: Generator Changes
1. Modify `MethodInterceptorRenderer.cs` to generate `Returns()` instead of `OnCall(value)`
2. Rename internal fields: `_onCallValue` → `_returnsValue`, `_hasOnCallValue` → `_hasReturnsValue`, `_onCallValueTracking` → `_returnsValueTracking`
3. Modify `InlineRenderer.cs` delegate value overload
4. Run tests to confirm all tests fail (expected - API changed)

### Phase 2: Test Updates
1. Update all test files to use `.Returns(value)` syntax
2. Verify all tests pass
3. Verify three patterns work correctly

### Phase 3: Documentation Updates
1. Update documentation sample files
2. Update README.md comparison section
3. Verify MarkdownSnippets sync works

---

## Acceptance Criteria

- [x] `stub.Method.Returns(value)` compiles and returns the configured value
- [x] `stub.Method.OnCall(callback)` still works unchanged
- [x] `stub.Method.OnCall(value)` no longer compiles (intentional breaking change)
- [x] All three patterns work with new syntax
- [x] All tests pass
- [x] README comparison reflects new syntax

---

## Dependencies

None - this is a self-contained rename operation.

---

## Risks / Considerations

1. **Breaking Change Risk**: Users upgrading will get compile errors. Mitigation: clear release notes, pre-1.0 phase allows breaking changes.

2. **Documentation Sync**: Must ensure all documentation samples are updated before merge. Mitigation: samples are compiled and tested.

3. **Search/Replace Accuracy**: `.OnCall(value)` vs `.OnCall(callback)` must be distinguished. Mitigation: callback versions have lambda syntax `=>` or delegate type, value versions pass simple expressions.

---

## Implementation Progress

**Phase 1: Generator Changes** - Complete
- Modified `MethodInterceptorRenderer.cs` to generate `Returns()` instead of `OnCall(value)`
- Renamed 25 internal field occurrences across 3 fields
- Modified `InlineRenderer.cs` delegate value overload

**Phase 2: Test Updates** - Complete
- Updated all test files (MethodValueOverloadTests, DelegateValueOverloadTests, ThreePatternValueOverloadTests, EdgeCaseValueOverloadTests, SequenceValueOverloadTests, AsyncCallbackSimplificationTests)
- Updated all documentation sample files

**Phase 3: Documentation Updates** - Complete
- Updated README.md comparison tables
- Updated docs/guides/methods.md, delegates.md, troubleshooting.md, getting-started.md, reference/interceptor-api.md
- Regenerated MarkdownSnippets

---

## Completion Evidence

**Tests Passing:** All 4513 tests pass across net8.0, net9.0, net10.0
```
Passed!  - Failed:     0, Passed:   824, Skipped:     0, Total:   824 - KnockOffTests.dll (net10.0)
Passed!  - Failed:     0, Passed:   338, Skipped:     0, Total:   338 - KnockOff.Documentation.Samples.dll (net10.0)
Passed!  - Failed:     0, Passed:   473, Skipped:     0, Total:   473 - KnockOff.NeatooInterfaceTests.dll (net10.0)
Passed!  - Failed:     0, Passed:    14, Skipped:     0, Total:    14 - KnockOffTests.AssemblyStrict.dll (net10.0)
```

**Generated Code Sample:**
```csharp
// Generated method signature (from MethodInterceptorRenderer.cs)
public IMethodTracking<TReturn> Returns(TReturn value)
{
    _sequence = null;
    _sequenceIndex = 0;
    _isVerifiable = false;
    _verifiableTimes = null;
    _onCall = null;
    _onCallTracking = null;
    _hasReturnsValue = true;
    _returnsValue = value;
    _returnsValueTracking = new MethodTrackingImpl(this);
    return _returnsValueTracking;
}
```

**All Checklist Items:** Confirmed 100% complete
