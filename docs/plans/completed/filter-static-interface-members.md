# Filter Static Interface Members

**Date:** 2026-02-08
**Related Todo:** [Interface Static Virtual Members](../../todos/completed/interface-static-virtual-members.md)
**Status:** Complete
**Last Updated:** 2026-02-08

---

## Overview

Filter out `static virtual` and `static abstract` interface members in the Transform phase so they never enter the builder/renderer pipeline. This is a surgical bug fix: add `member.IsStatic` checks at the four member discovery loops in `KnockOffGenerator.Transform.cs`.

---

## Problem Statement

When a C# 11+ interface contains `static virtual` or `static abstract` members, KnockOff's source generator includes them alongside instance members. The renderer generates explicit interface implementations for these static members, which causes CS0539 compiler errors ("member in explicit interface declaration is not found among members of the interface that can be implemented").

Static interface members are not part of the instance contract. They cannot be stubbed via instance implementations. The correct behavior is to silently skip them.

---

## Root Cause Analysis

### Codebase Investigation

**Files examined:**

- `src/Generator/KnockOffGenerator.Transform.cs` -- Contains all member discovery code for interface stubs. Four separate loops iterate `GetMembers()` and include members based on type (IPropertySymbol, IMethodSymbol, IEventSymbol) without checking `IsStatic`.
- `src/Generator/KnockOffGenerator.StandaloneClass.cs` -- Standalone class stub transform. Delegates to `ExtractClassInfo()` which checks `IsVirtual || IsAbstract || IsOverride` -- static members cannot satisfy these conditions, so class stubs are NOT affected.
- `src/Generator/Models/InterfaceModels.cs` -- `InterfaceMemberInfo.FromProperty()` and `FromMethod()` factory methods. These are pure model constructors; they do not filter.
- `src/Generator/Models/EventModels.cs` -- `EventMemberInfo.FromEvent()` factory method. Same: no filtering.
- `src/Tests/KnockOffTests/InterfaceStaticVirtualTests.cs` -- Reproduction tests covering inline interface and standalone interface patterns.

### Affected Locations

There are exactly **four** member iteration loops that need the `IsStatic` filter, all in `KnockOffGenerator.Transform.cs`:

| # | Method | Line Range | Loop Purpose |
|---|--------|------------|-------------|
| 1 | `ExtractInterfaceInfo()` | 316-333 | Primary members of the target interface |
| 2 | `ExtractInterfaceInfo()` | 337-357 | Inherited members from `iface.AllInterfaces` |
| 3 | `TransformClass()` | 811-829 | Primary members of each implemented interface |
| 4 | `TransformClass()` | 833-854 | Inherited members from `iface.AllInterfaces` |

### Why Class Stubs Are Not Affected

`ExtractClassInfo()` (line 505-578) and `GetAllVirtualMembers()` (line 597-616) already implicitly filter static members because they require `property.IsVirtual || property.IsAbstract || property.IsOverride` (and similarly for methods and events). A static member cannot satisfy any of these conditions, so it never enters the class stub pipeline. No changes needed for class-related code.

### Why `IsMemberAccessible()` Is Not the Right Filter Point

`IsMemberAccessible()` (line 1162-1174) checks assembly-level accessibility. While it would be possible to add `IsStatic` filtering there, this would conflate two distinct concerns (accessibility vs. member kind). The static check belongs alongside the member-kind dispatching (`if (member is IPropertySymbol ...)`) where the intent is clear.

---

## Approach

### Fix Strategy: Guard Clause in Each Loop

Add `member.IsStatic` checks as a `continue` guard at the top of each of the four member iteration loops. This is the earliest point where we can filter, preventing static members from reaching `InterfaceMemberInfo.FromProperty()`, `InterfaceMemberInfo.FromMethod()`, or `EventMemberInfo.FromEvent()`.

### Code Change

In all four loops, add this guard immediately after the accessibility check:

```csharp
// Skip static members (static virtual/abstract in interfaces)
if (member.IsStatic)
    continue;
```

### Specific Changes

**Location 1: `ExtractInterfaceInfo()`, primary loop (line ~319)**

Before:
```csharp
foreach (var member in memberSource.GetMembers())
{
    // Skip internal members from external assemblies
    if (!IsMemberAccessible(member, knockOffAssembly))
        continue;

    if (member is IPropertySymbol property)
```

After:
```csharp
foreach (var member in memberSource.GetMembers())
{
    // Skip internal members from external assemblies
    if (!IsMemberAccessible(member, knockOffAssembly))
        continue;

    // Skip static members (static virtual/abstract in interfaces)
    if (member.IsStatic)
        continue;

    if (member is IPropertySymbol property)
```

**Location 2: `ExtractInterfaceInfo()`, inherited loop (line ~341)**

Same pattern: add `if (member.IsStatic) continue;` after the `IsMemberAccessible` check.

**Location 3: `TransformClass()`, primary loop (line ~813)**

Same pattern: add `if (member.IsStatic) continue;` after the `IsMemberAccessible` check.

**Location 4: `TransformClass()`, inherited loop (line ~838)**

Same pattern: add `if (member.IsStatic) continue;` after the `IsMemberAccessible` check.

---

## Scope

### Patterns Affected

Static virtual/abstract members only exist on interfaces. Only interface-based patterns are affected:

| Pattern | Affected | Notes |
|---------|----------|-------|
| Standalone (1) | Yes | `TransformClass()` loops 3+4 |
| Generic Standalone (2) | Yes | Same code path as pattern 1 |
| Standalone Class (3) | No | Virtual/abstract filter already excludes static |
| Generic Standalone Class (4) | No | Same as pattern 3 |
| Inline Interface (5) | Yes | `ExtractInterfaceInfo()` loops 1+2 |
| Inline Class (6) | No | Virtual/abstract filter already excludes static |
| Inline Delegate (7) | No | Delegates do not have static members |
| Open Generic Interface (8) | Yes | Uses `ExtractInterfaceInfo()` |
| Open Generic Class (9) | No | Virtual/abstract filter already excludes static |

### Member Types Affected

All three interface member types need the filter:
- **Methods** (`IMethodSymbol`) -- `static virtual string StaticLift() => "Lift";`
- **Properties** (`IPropertySymbol`) -- `static virtual string? StaticPush { get; set; }`
- **Events** (`IEventSymbol`) -- `static virtual event EventHandler? StaticEvent;`

`member.IsStatic` works uniformly for all three because `ISymbol.IsStatic` is defined on the base `ISymbol` type. A single check before the type-dispatching `if` handles all member types.

### Breaking Changes

**No.** This fix only removes erroneously generated code. Any interface that currently has static virtual/abstract members fails to compile. After the fix, the generator will simply skip those members, and the stub will compile correctly. No existing working code is affected.

---

## Test Strategy

### Existing Tests (in `InterfaceStaticVirtualTests.cs`)

The reproduction tests are already written and are currently causing build failures:

1. `InlineStub_InstanceMethod_Works` -- Verifies instance method still works when interface has static members
2. `InlineStub_InstanceProperty_Works` -- Verifies instance property getter
3. `InlineStub_InstancePropertySetter_Works` -- Verifies instance property setter
4. `StandaloneStub_InstanceMethod_Works` -- Verifies standalone pattern with mixed members
5. `StandaloneStub_InstanceProperty_Works` -- Verifies standalone property

The `IOnlyStaticVirtuals` interface (with `OnlyStaticVirtualInlineTest` stub) verifies the edge case where ALL members are static -- the stub should compile but have no interceptors.

### No Additional Tests Needed

The existing tests cover the two affected pipelines (inline interface and standalone interface). They verify both the positive case (instance members still work) and the edge case (interface with only static members). Static events could be tested but are a rare edge case covered by the same `member.IsStatic` guard.

---

## Design Project Verification

This fix is a pipeline bug fix. Static virtual members are a C# 11+ feature. The Design projects target interfaces that don't currently use `static virtual` members, so there is no existing Design.Stubs code to verify.

No new Design.Stubs code is needed because the fix is a filter (removing erroneously included members), not a new feature. The compilation of the test project (`KnockOffTests`) IS the acceptance criteria. When the four `IsStatic` guards are added, the 18 CS0539 errors disappear and the 5 tests pass.

---

## Implementation Steps

1. Add `if (member.IsStatic) continue;` to the four member iteration loops in `KnockOffGenerator.Transform.cs`
2. Build `src/KnockOff.sln` -- verify 0 errors
3. Run tests -- verify `InterfaceStaticVirtualTests` all pass
4. Verify no other tests regressed

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Missing a loop location | Low | High | All four locations identified and verified by grep |
| Filtering legitimate members | None | N/A | `IsStatic` is only true for static members; instance members are unaffected |
| Breaking existing stubs | None | N/A | No existing stub has static virtual members (they all fail to compile) |

---

## Architectural Verification

- [x] All nine patterns analyzed
- [x] Breaking changes assessment: None
- [x] Pattern consistency verified: Fix applies uniformly via `ISymbol.IsStatic`
- [x] Diagnostic requirements: None needed (silent skip is correct behavior)
- [x] Test strategy defined: Existing tests are sufficient
- [x] Edge cases documented: All-static interface, inherited static members
- [x] Codebase deep-dive completed

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-02-08

### Why This Plan Is Exceptionally Clear

This plan is a surgical bug fix with a precisely scoped change: four identical two-line additions to a single file. The root cause, fix locations, and acceptance criteria are all unambiguous. The fix targets the earliest possible point in the pipeline (Transform phase) and uses a well-defined, non-nullable boolean property (`ISymbol.IsStatic`) that uniformly handles methods, properties, and events. The existing reproduction tests already serve as acceptance criteria -- when the fix is applied, the 18 CS0539 errors disappear and the 5 tests pass.

### Review Summary

- **Files examined:** `KnockOffGenerator.Transform.cs` (lines 300-370, 505-616, 790-860, 1155-1185), `KnockOffGenerator.StandaloneClass.cs` (lines 1-50), `InterfaceStaticVirtualTests.cs` (all), `InterfaceModels.cs` (referenced), `EventModels.cs` (referenced)
- **Questions checked:** 16 of 16
- **Devil's advocate items:** 3 generated (static abstract test coverage, static event test coverage, inherited static virtual members), all already addressed by the uniform `IsStatic` guard

### Codebase Investigation

**Files Examined:**
- `src/Generator/KnockOffGenerator.Transform.cs` - Confirmed 5 `GetMembers()` call sites (lines 316, 340, 605, 811, 836). Four are in interface loops needing the fix; one (line 605) is in `GetAllVirtualMembers()` for class pipeline, already safe.
- `src/Generator/KnockOffGenerator.StandaloneClass.cs` - Confirmed it delegates to `ExtractClassInfo()`. Not affected.
- `src/Tests/KnockOffTests/InterfaceStaticVirtualTests.cs` - 5 test methods, 2 test interfaces, 3 stub definitions. Build output confirms 18 CS0539 errors (6 unique errors x 3 TFMs).

**Searches Performed:**
- `.GetMembers()` in Transform.cs - 5 sites, 4 need fix
- `IsStatic` in Generator directory - only 1 existing usage (line 422, class-level check)
- `ExtractInterfaceInfo` usages - called from line 132 (inline + open generic patterns)
- `static virtual event` in tests - no coverage (non-blocking, same guard applies)

**Design.Stubs Verification:**
- N/A -- bug fix removing erroneously generated code. No Design.Stubs code uses `static virtual` members. The architect's reasoning is sound: the test project compilation is the acceptance criteria.

**Discrepancies Found:**
- Plan line numbers are approximate (~319 vs actual 316, ~341 vs 340, ~813 vs 811, ~838 vs 836). The "~" prefix acknowledges this. No material discrepancy.

### Observations (Non-Blocking)

1. The test interfaces do not include a `static abstract` variant or a `static virtual event`. These are covered by the same `IsStatic` guard, so no test gap exists from a correctness standpoint, but if the architect wants to add them for completeness, they could be added in a future pass.

---

## Implementation Contract

**Created:** 2026-02-08
**Approved by:** knockoff-developer

### Acceptance Criteria

When the four `IsStatic` guards are added:
- `dotnet build src/KnockOff.sln` succeeds with 0 errors
- All 5 tests in `InterfaceStaticVirtualTests` pass
- The `OnlyStaticVirtualInlineTest` stub compiles (edge case: all-static interface)
- No other tests regress

### In Scope

- [ ] Add `if (member.IsStatic) continue;` after `IsMemberAccessible` check in `ExtractInterfaceInfo()` primary loop (line ~319 in `KnockOffGenerator.Transform.cs`)
- [ ] Add `if (member.IsStatic) continue;` after `IsMemberAccessible` check in `ExtractInterfaceInfo()` inherited loop (line ~343 in `KnockOffGenerator.Transform.cs`)
- [ ] Add `if (member.IsStatic) continue;` after `IsMemberAccessible` check in `TransformClass()` primary loop (line ~814 in `KnockOffGenerator.Transform.cs`)
- [ ] Add `if (member.IsStatic) continue;` after `IsMemberAccessible` check in `TransformClass()` inherited loop (line ~839 in `KnockOffGenerator.Transform.cs`)
- [ ] Checkpoint: Build `src/KnockOff.sln` -- verify 0 errors
- [ ] Checkpoint: Run full test suite -- verify `InterfaceStaticVirtualTests` pass and no regressions

### Explicitly Out of Scope

- Design.Stubs additions -- not needed for a filter bug fix
- Additional test coverage for `static abstract` or `static virtual event` -- same guard covers all member types
- Class pipeline changes -- `ExtractClassInfo()` already filters via virtual/abstract/override requirements
- Documentation updates -- this is a bug fix with no user-facing API change
- Open generic interface test -- pattern 8 shares the same `ExtractInterfaceInfo()` code path as pattern 5; the inline tests cover both

### Verification Gates

1. After adding all 4 guards: `dotnet build src/KnockOff.sln` must succeed with 0 errors (currently 18 CS0539 errors)
2. Final: `dotnet test src/KnockOff.sln` must pass all tests including the 5 `InterfaceStaticVirtualTests`

### Stop Conditions

If any of these occur, STOP and report:
- Any out-of-scope test starts failing after the change
- A `GetMembers()` loop is discovered that was not identified in this plan
- The `IsStatic` check filters instance members (should never happen, but verify via tests)

---

## Implementation Progress

**Started:** 2026-02-08

**Phase 1: Add IsStatic guards (4 locations)**
- [x] Location 1: `ExtractInterfaceInfo()` primary loop (line 319-320 after edit) -- added `if (member.IsStatic) continue;`
- [x] Location 2: `ExtractInterfaceInfo()` inherited loop (line 346-347 after edit) -- added `if (member.IsStatic) continue;`
- [x] Location 3: `TransformClass()` primary loop (line 820-821 after edit) -- added `if (member.IsStatic) continue;`
- [x] Location 4: `TransformClass()` inherited loop (line 847-848 after edit) -- added `if (member.IsStatic) continue;`
- [x] **Verification Gate 1**: `dotnet build src/KnockOff.sln` -- 0 errors, 0 warnings (was 18 CS0539 errors before fix)

**Phase 2: Test fix**
- [x] Fixed in-scope test `StandaloneStub_InstanceMethod_Works` -- added `stub.InstanceLift.Return("Lifted")` and assertion. Standalone stubs require explicit return configuration for non-void methods (unlike inline stubs which default to `default`). This is expected standalone behavior, not a bug.
- [x] **Verification Gate 2**: `dotnet test src/KnockOff.sln` -- all tests pass, zero failures

---

## Completion Evidence

- **Build**: `dotnet build src/KnockOff.sln` -- 0 errors, 0 warnings
- **Tests Passing:**
  - KnockOffTests: 1310 passed (net9.0, net10.0), 1309 passed (net8.0), 0 failed
  - KnockOff.NeatooInterfaceTests: 473 passed (all 3 TFMs), 0 failed
  - KnockOff.Documentation.Samples: 599 passed (all 3 TFMs), 0 failed
  - KnockOffTests.AssemblyStrict: 14 passed (all 3 TFMs), 0 failed
  - **Total: 0 failures across all test projects and target frameworks**
- **Design Projects Compile:** N/A (bug fix, no Design.Stubs changes)
- **All Contract Items:** Confirmed complete
  - [x] 4 IsStatic guards added in `KnockOffGenerator.Transform.cs`
  - [x] Build succeeds with 0 errors
  - [x] All InterfaceStaticVirtualTests pass (5 tests)
  - [x] OnlyStaticVirtualInlineTest compiles (all-static interface edge case)
  - [x] No regressions in any test project
- **Files Modified:**
  - `src/Generator/KnockOffGenerator.Transform.cs` -- 4 guard clauses added (8 new lines total)
  - `src/Tests/KnockOffTests/InterfaceStaticVirtualTests.cs` -- Fixed `StandaloneStub_InstanceMethod_Works` test to configure return value (in-scope test fix)

---

## Architect Verification

**Verified:** 2026-02-08
**Verdict:** VERIFIED

### Independent Test Results
- KnockOffTests: 1310 passed (net9.0, net10.0), 1309 passed (net8.0), 0 failed
- KnockOff.NeatooInterfaceTests: 473 passed (all 3 TFMs), 0 failed
- KnockOff.Documentation.Samples: 599 passed (all 3 TFMs), 0 failed
- KnockOffTests.AssemblyStrict: 14 passed (all 3 TFMs), 0 failed
- Total: 0 failures across all test projects and target frameworks

### Design Match
- 4 `IsStatic` guards in `KnockOffGenerator.Transform.cs`: Matches plan
- Guard placement (after `IsMemberAccessible`, before type dispatch): Matches plan
- Comment text and code pattern: Matches plan
- No changes to class pipeline (`ExtractClassInfo`/`GetAllVirtualMembers`): Matches plan rationale

### Generated Code Spot-Check
- `InterfaceStaticVirtualTests.cs`: Inline and standalone stubs compile, verifying the generator no longer emits static member implementations
- `OnlyStaticVirtualInlineTest`: Edge case (all-static interface) compiles, confirming the stub generates correctly with zero interceptors

### Test Coverage Verification
- Inline interface pattern: 3 tests (method, property get, property set)
- Standalone interface pattern: 2 tests (method, property get)
- All-static edge case: Compilation-only (no runtime assertions needed)
