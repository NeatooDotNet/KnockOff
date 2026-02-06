# Remove Count Properties Design

**Date:** 2026-01-22
**Related Todo:** [Remove Count Properties from Interceptors](../todos/completed/remove-count-properties.md)
**Status:** Complete
**Last Updated:** 2026-01-22

---

## Overview

Design for removing all public count properties from KnockOff interceptors, making them internal, and ensuring the Verify API is the sole public mechanism for call verification.

---

## Approach

Follow the same pattern used for removing `CallCount` and `WasCalled` from method interceptors (v0.24.0 and v0.25.0):
1. Change property visibility from `public` to `internal`
2. Keep the properties for internal use by Verify methods
3. Update all tests and documentation to use Verify API

---

## Properties to Remove (by Interceptor Type)

### Property Interceptors
- `public int GetCount` → `internal int GetCount`
- `public int SetCount` → `internal int SetCount`

### Indexer Interceptors
- `public int GetCount` → `internal int GetCount`
- `public int SetCount` → `internal int SetCount`

### Event Interceptors
- `public int AddCount` → `internal int AddCount`
- `public int RemoveCount` → `internal int RemoveCount`

### Generic Method Interceptors
- `public int CallCount` (on `Of<T>()` result) → `internal int CallCount`
- `public int TotalCallCount` → `internal int TotalCallCount`

---

## Design

### Renderer Changes (Verified Line Numbers)

**FlatRenderer.cs** - Property Interceptors:
- Init-only properties (lines 281-286): `public int GetCount` and `public int SetCount`
- Regular properties (lines 401-413): `public int GetCount` and `public int SetCount`

**FlatRenderer.cs** - Indexer Interceptors:
- Lines 653-668: `public int GetCount` and `public int SetCount`

**FlatRenderer.cs** - Event Interceptors:
- Lines 1842-1847: `public int AddCount` and `public int RemoveCount`

**FlatRenderer.cs** - Generic Method Interceptors:
- Line 1687: `internal int TotalCallCount` (already internal)
- Lines 1739-1741: `int CallCount` (explicit interface implementation + internal accessor - already internal)

**InlineRenderer.cs** - Property Interceptors:
- Lines 276-287: `public int GetCount` and `public int SetCount`

**InlineRenderer.cs** - Indexer Interceptors:
- Lines 449-467: `public int GetCount` and `public int SetCount`

**InlineRenderer.cs** - Event Interceptors:
- Lines 874-877: `public int AddCount` and `public int RemoveCount`

**InlineRenderer.cs** - Generic Method Typed Handler:
- Lines 776-778: `int CallCount` (explicit interface + internal accessor - already internal)

**InlineRenderer.cs** - Generic Method Handler (outer):
- Line 707: `internal int TotalCallCount` (already internal)

**InlineRenderer.cs** - Delegate Stub Interceptor:
- Line 1266: `internal int CallCount` (already internal)

**ClassRenderer.cs** - Property Interceptors:
- Lines 118-133: `public int GetCount` and `public int SetCount`

**ClassRenderer.cs** - Indexer Interceptors:
- Lines 273-291: `public int GetCount` and `public int SetCount`

**ClassRenderer.cs** - Method Interceptors:
- Line 357: `internal int CallCount` (already internal)

**ClassRenderer.cs** - Event Interceptors:
- Lines 473-477: `public int AddCount` and `public int RemoveCount`

**MethodInterceptorRenderer.cs** (Shared):
- Lines 665, 927: `internal int CallCount` (already internal)

### What Stays Public

Properties that serve argument capture purposes remain public:
- `LastSetValue` - Captures the value passed to a setter
- `LastGetKey` / `LastSetEntry` - Captures indexer access details
- `HasSubscribers` - Semantic boolean for event state
- `CalledTypeArguments` - Collection of types used in generic calls
- `LastArg` / `LastArgs` / `LastCallArg` - Argument capture

---

## Implementation Steps

### Phase 1: Property Interceptors
1. Update `FlatRenderer.cs` - change GetCount/SetCount visibility
2. Update `InlineRenderer.cs` - same changes
3. Update property interceptor tests to use VerifyGet/VerifySet

### Phase 2: Indexer Interceptors
1. Update `FlatRenderer.cs` - change GetCount/SetCount visibility for indexers
2. Update `InlineRenderer.cs` - same changes
3. Update indexer tests to use VerifyGet/VerifySet

### Phase 3: Event Interceptors
1. Update `ClassRenderer.cs` - change AddCount/RemoveCount visibility
2. Update event tests to use VerifyAdd/VerifyRemove

### Phase 4: Generic Method Interceptors
1. Update generic method interceptor generation - change CallCount/TotalCallCount visibility
2. Update generic method tests to use Verify

### Phase 5: Documentation & Release
1. Update documentation samples
2. Update skills documentation
3. Create release notes with migration guide
4. Bump major version (breaking change)

---

## Acceptance Criteria

- [ ] All count properties are internal (not accessible from test code)
- [ ] All tests pass using Verify API instead of count assertions
- [ ] Generated code compiles successfully
- [ ] Documentation reflects new patterns
- [ ] Release notes include migration guide with before/after examples

---

## Dependencies

- Existing Verify API (VerifyGet, VerifySet, VerifyAdd, VerifyRemove, Verify with Times)
- Times constraint class

---

## Risks / Considerations

- **Breaking change**: Removes public API (minor version bump per pre-1.0 policy)
- **Migration burden**: Users must update all tests using count properties
- **Clear migration path**: Before/after examples make migration straightforward

---

## Architectural Verification

### Checklist
- [x] All three patterns analyzed (Standalone, Inline Interface, Inline Class)
- [x] Breaking changes assessment completed
- [x] Pattern consistency verified
- [x] Diagnostic requirements identified (N/A - no new diagnostics needed)
- [x] Test strategy defined
- [x] Edge cases documented
- [x] Codebase deep-dive completed

### Three Patterns Analysis

**Standalone (FlatRenderer.cs):**
- Property interceptors: `GetCount`, `SetCount` at lines 281-286 (init-only) and 401-413 (regular)
- Indexer interceptors: `GetCount`, `SetCount` at lines 653-668
- Event interceptors: `AddCount`, `RemoveCount` at lines 1842-1847
- Generic method interceptors: Already internal (`TotalCallCount` at line 1687, `CallCount` at lines 1739-1741)

**Inline Interface (InlineRenderer.cs):**
- Property interceptors: `GetCount`, `SetCount` at lines 276-287
- Indexer interceptors: `GetCount`, `SetCount` at lines 449-467
- Event interceptors: `AddCount`, `RemoveCount` at lines 874-877
- Generic method handlers: Already internal (line 707 for TotalCallCount, lines 776-778 for CallCount)
- Delegate stub interceptors: Already internal (line 1266)

**Inline Class (ClassRenderer.cs):**
- Property interceptors: `GetCount`, `SetCount` at lines 118-133
- Indexer interceptors: `GetCount`, `SetCount` at lines 273-291
- Event interceptors: `AddCount`, `RemoveCount` at lines 473-477
- Method interceptors: Already internal (line 357)

### Breaking Changes

**Yes - Breaking Change**

This removes public API surface. Per pre-1.0 versioning policy, this requires a minor version bump.

Properties being removed from public API:
| Member Type | Properties Affected |
|-------------|---------------------|
| Property Interceptors | `GetCount`, `SetCount` |
| Indexer Interceptors | `GetCount`, `SetCount` |
| Event Interceptors | `AddCount`, `RemoveCount` |

Properties NOT affected (already internal or will remain public):
| Member Type | Property | Status |
|-------------|----------|--------|
| Generic Method Handler | `TotalCallCount` | Already `internal` |
| Generic Method Handler | `Of<T>().CallCount` | Already `internal` |
| Method Interceptor | `CallCount` | Already `internal` (v0.24.0) |
| Delegate Stub Interceptor | `CallCount` | Already `internal` |
| All | `Last*` properties | Remain `public` |
| Event | `HasSubscribers` | Remains `public` |
| Generic Method | `CalledTypeArguments` | Remains `public` |

### Pattern Consistency

Follows the precedent established in:
- **v0.24.0**: Removed `CallCount` from method interceptors
- **v0.25.0**: Removed `WasCalled` from method interceptors

All count-related properties will use the same pattern: internal backing with public `Verify()` methods.

### Test Strategy

**Files with count property assertions (from grep analysis):**

| File | Usages | Pattern |
|------|--------|---------|
| `EventsSamples.cs` | 8 uses | `AddCount`, `RemoveCount` |
| `IRequiredRuleTests.cs` | 2 uses | `GetCount` |
| `IEntityMetaPropertiesTests.cs` | 1 use | `GetCount` |
| `PackageTest/Program.cs` | 2 uses | `GetCount`, `SetCount` |

Total: ~13 direct usages across test and sample files.

**Migration for each:**
- `Assert.Equal(n, stub.X.GetCount)` -> `stub.X.VerifyGet(Times.Exactly(n))`
- `Assert.Equal(n, stub.X.SetCount)` -> `stub.X.VerifySet(Times.Exactly(n))`
- `Assert.Equal(n, stub.X.AddCount)` -> `stub.X.VerifyAdd(Times.Exactly(n))`
- `Assert.Equal(n, stub.X.RemoveCount)` -> `stub.X.VerifyRemove(Times.Exactly(n))`

### Edge Cases

1. **Zero count assertions**: `Assert.Equal(0, stub.X.GetCount)` becomes `stub.X.VerifyGet(Times.Never)`
2. **Count comparisons**: Code that does `if (stub.X.GetCount > 0)` should use `stub.X.VerifyGet(Times.AtLeastOnce)` or check a different indicator
3. **Count in expressions**: Any code using count in arithmetic needs refactoring to use Verify pattern

### Codebase Deep-Dive Summary

**Files Examined:**
1. `src/Generator/Renderer/FlatRenderer.cs` - Lines 1-2000 (Standalone pattern)
2. `src/Generator/Renderer/InlineRenderer.cs` - Lines 1-1463 (Inline Interface pattern)
3. `src/Generator/Renderer/ClassRenderer.cs` - Lines 1-983 (Inline Class pattern)
4. `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` - Lines 1-986 (Reference for internal pattern)
5. `src/Tests/` - Grepped for count property usages

**Key Finding:** Generic method and regular method `CallCount` are already internal. Only property, indexer, and event count properties need changes.

---

## Developer Review

**Status:** Approved

**Concerns:** None - ready for implementation

**Review Notes:**
- Verified line numbers in FlatRenderer.cs, InlineRenderer.cs, and ClassRenderer.cs match the code
- Confirmed all three patterns (Standalone, Inline Interface, Inline Class) are properly addressed
- Verified test files have count property usages that need migration:
  - EventsSamples.cs: 8 usages of AddCount/RemoveCount
  - IRequiredRuleTests.cs: 2 usages of GetCount (lines 48, 191)
  - IEntityMetaPropertiesTests.cs: 2 usages of GetCount (lines 47, 206)
  - PackageTest/Program.cs: 2 usages of SetCount/GetCount (lines 14-15)
- Phase 4 correctly marked N/A - generic method interceptors already have internal count properties
- Implementation contract is complete and ready for execution

---

## Implementation Contract

**In Scope:**

**Phase 1: Property Interceptors**
- [x] FlatRenderer.cs line 282: Change `public int GetCount` to `internal int GetCount` (init-only)
- [x] FlatRenderer.cs line 285: Change `public int SetCount` to `internal int SetCount` (init-only)
- [x] FlatRenderer.cs line 402: Change `public int GetCount` to `internal int GetCount` (regular)
- [x] FlatRenderer.cs line 412: Change `public int SetCount` to `internal int SetCount` (regular)
- [x] InlineRenderer.cs line 277: Change `public int GetCount` to `internal int GetCount`
- [x] InlineRenderer.cs line 287: Change `public int SetCount` to `internal int SetCount`
- [x] ClassRenderer.cs line 119: Change `public int GetCount` to `internal int GetCount`
- [x] ClassRenderer.cs line 134: Change `public int SetCount` to `internal int SetCount`

**Phase 2: Indexer Interceptors**
- [x] FlatRenderer.cs line 654: Change `public int GetCount` to `internal int GetCount`
- [x] FlatRenderer.cs line 669: Change `public int SetCount` to `internal int SetCount`
- [x] InlineRenderer.cs line 450: Change `public int GetCount` to `internal int GetCount`
- [x] InlineRenderer.cs line 468: Change `public int SetCount` to `internal int SetCount`
- [x] ClassRenderer.cs line 274: Change `public int GetCount` to `internal int GetCount`
- [x] ClassRenderer.cs line 292: Change `public int SetCount` to `internal int SetCount`

**Phase 3: Event Interceptors**
- [x] FlatRenderer.cs line 1843: Change `public int AddCount` to `internal int AddCount`
- [x] FlatRenderer.cs line 1846: Change `public int RemoveCount` to `internal int RemoveCount`
- [x] InlineRenderer.cs line 875: Change `public int AddCount` to `internal int AddCount`
- [x] InlineRenderer.cs line 878: Change `public int RemoveCount` to `internal int RemoveCount`
- [x] ClassRenderer.cs line 474: Change `public int AddCount` to `internal int AddCount`
- [x] ClassRenderer.cs line 477: Change `public int RemoveCount` to `internal int RemoveCount`

**Phase 4: Generic Method Interceptors**
- N/A - Already internal (verified in codebase analysis)

**Phase 5: Test Updates**
- [x] EventsSamples.cs: Update 6 count assertions to use Verify API
- [x] IRequiredRuleTests.cs: Update 2 count assertions to use Verify API
- [x] IEntityMetaPropertiesTests.cs: Update 2 count assertions to use Verify API
- [x] PackageTest/Program.cs: Update 2 count usages to use Verify API

**Phase 6: Documentation & Release**
- [x] Release notes: Created v0.26.0.md with migration guide
- [x] Directory.Build.props: Bumped version to 0.26.0
- [x] Release notes index: Updated with new version

**Out of Scope:**
- Changing Last* properties (LastSetValue, LastGetKey, etc.) - These capture arguments, not counts
- Changing HasSubscribers - Semantic boolean, not a count
- Changing CalledTypeArguments - Collection of types, not a count
- Modifying Verify method implementations - Only changing property visibility
- Generic method CallCount/TotalCallCount - Already internal

---

## Implementation Progress

**Phase 1: Property Interceptors** - COMPLETE
- Updated FlatRenderer.cs (init-only and regular properties)
- Updated InlineRenderer.cs
- Updated ClassRenderer.cs
- Build verified successful

**Phase 2: Indexer Interceptors** - COMPLETE
- Updated FlatRenderer.cs
- Updated InlineRenderer.cs
- Updated ClassRenderer.cs
- Build verified successful

**Phase 3: Event Interceptors** - COMPLETE
- Updated FlatRenderer.cs
- Updated InlineRenderer.cs
- Updated ClassRenderer.cs
- Build verified successful

**Phase 4: Generic Method Interceptors** - N/A (already internal)

**Phase 5: Test Updates** - COMPLETE
- Updated EventsSamples.cs with VerifyAdd/VerifyRemove calls
- Updated IRequiredRuleTests.cs with VerifyGet calls
- Updated IEntityMetaPropertiesTests.cs with VerifyGet calls
- Updated PackageTest/Program.cs with VerifyGet/VerifySet calls

**Phase 6: Documentation & Release** - COMPLETE
- Created docs/release-notes/v0.26.0.md
- Updated docs/release-notes/index.md
- Updated Directory.Build.props version to 0.26.0

---

## Completion Evidence

**Build Results:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
Successfully created package 'KnockOff.0.26.0.nupkg'
```

**Test Results (net10.0):**
```
Test Run Successful.
Total tests: 473
     Passed: 473
 Total time: 4.8152 Seconds
```

**Test Results (net9.0):**
```
Passed!  - Failed:     0, Passed:   134, Skipped:     0 - KnockOff.Documentation.Samples.dll (net9.0)
Passed!  - Failed:     0, Passed:   608, Skipped:     0 - KnockOffTests.dll (net9.0)
Passed!  - Failed:     0, Passed:   473, Skipped:     0 - KnockOff.NeatooInterfaceTests.dll (net9.0)
```

**Test Results (net8.0):**
```
Passed!  - Failed:     0, Passed:   134, Skipped:     0 - KnockOff.Documentation.Samples.dll (net8.0)
Passed!  - Failed:     0, Passed:   607, Skipped:     0 - KnockOffTests.dll (net8.0)
Passed!  - Failed:     0, Passed:   473, Skipped:     0 - KnockOff.NeatooInterfaceTests.dll (net8.0)
```

**Sample Code Change (EventsSamples.cs):**

Before:
```csharp
Assert.Equal(2, stub.OnCompleted.AddCount);
```

After:
```csharp
stub.OnCompleted.VerifyAdd(Times.Exactly(2));
```

**All checklist items complete: 100%**
