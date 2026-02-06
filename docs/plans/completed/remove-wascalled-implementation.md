# Remove WasCalled Implementation Plan

**Date:** 2026-01-22
**Related Todo:** [Remove WasCalled](../../todos/completed/remove-wascalled.md)
**Status:** Complete
**Last Updated:** 2026-01-22 (Implementation complete)

---

## Overview

Remove the `WasCalled` computed property from all interceptors and tracking objects. Add `Verify()` and `Verify(Times)` methods directly on method interceptors for consistency with property interceptors.

**Key Correction:** Indexer interceptors already have `Verify()`, `VerifyGet()`, and `VerifySet()` methods in both FlatRenderer and InlineRenderer (verified in codebase review). No indexer changes needed.

This is a **breaking change** that requires major version bump.

---

## Approach

1. **Interface First**: Remove `WasCalled` from `IMethodTracking` interfaces - this makes the breaking change explicit
2. **Generator Core**: Update shared `MethodInterceptorRenderer.cs` - affects both inline and flat patterns
3. **Pattern-Specific**: Update `FlatRenderer.cs`, `InlineRenderer.cs`, `ClassRenderer.cs`
4. **Documentation**: Update guides, remove incorrect WasGot/WasSet references

**Note:** Indexers already have verification methods - no changes needed there (corrected from initial plan).

---

## Design

### Current State

**WasCalled exists at two levels:**

1. **Interceptor Level** (aggregate across all call sources):
   ```csharp
   // MethodInterceptorRenderer.cs:929
   public bool WasCalled => CallCount > 0;
   ```

2. **MethodTrackingImpl Level** (per-callback tracking):
   ```csharp
   // MethodInterceptorRenderer.cs:664
   public bool WasCalled => CallCount > 0;
   ```

**Properties already have direct Verify():**
```csharp
// Property interceptors (existing)
public void Verify() => Verify(Times.AtLeastOnce);
public void Verify(Times times) { ... }
public void VerifyGet() => VerifyGet(Times.AtLeastOnce);
public void VerifySet() => VerifySet(Times.AtLeastOnce);
```

**Methods only have Verify() on tracking objects:**
```csharp
// MethodTrackingImpl (existing)
public void Verify() => Verify(Times.AtLeastOnce);
public void Verify(Times times) { ... }
// But NOT on interceptor directly
```

### Target State

**Remove WasCalled from:**
- `IMethodTracking.WasCalled` (interface)
- `MethodTrackingImpl.WasCalled` (implementation)
- Interceptor-level `WasCalled` (backward-compat properties)
- Generic method handler `WasCalled` (aggregate)

**Add Verify() to method interceptors:**
```csharp
public sealed class GetByIdInterceptor
{
    // Existing: CallCount, OnCall(), Invoke(), Reset(), etc.

    // NEW: Direct verification
    public void Verify() => Verify(global::KnockOff.Times.AtLeastOnce);

    public void Verify(global::KnockOff.Times times)
    {
        if (!times.Validate(CallCount))
            throw new global::KnockOff.VerificationException(
                new global::KnockOff.VerificationFailure("GetById", times, CallCount));
    }
}
```

**Indexer interceptors (NO CHANGES NEEDED):**
```csharp
// FlatRenderer.cs lines 718-764 and InlineRenderer.cs lines 532-576
// Already have: Verify(), Verify(Times), VerifyGet(), VerifyGet(Times), VerifySet(), VerifySet(Times)
// Verified in codebase - these methods exist and work correctly
```

### File Changes Map

| File | Remove | Add |
|------|--------|-----|
| `IMethodTracking.cs` | `WasCalled` property (line 9) + update XML docs (lines 47, 70) | - |
| `MethodInterceptorRenderer.cs` | `WasCalled` lines 664, 929, 969 | `Verify()` methods on interceptors |
| `FlatRenderer.cs` | `WasCalled` lines 231, 908, 1057, 1297, 1440, 1531, 1576, 1703, 1763 | `Verify()` on generic handlers |
| `InlineRenderer.cs` | `WasCalled` lines 61, 710, 787, 1263 | `Verify()` on generic handlers, delegate interceptor |
| `ClassRenderer.cs` | `WasCalled` line 360 | - (already has Verify() at lines 417-426) |
| `verification.md` | WasGot/WasSet section (lines 386-395) | Migration section |

**Note:** ClassRenderer.cs already has `Verify()` and `Verify(Times)` methods - only need to remove `WasCalled`.

---

## Implementation Steps

### Phase 1: Core Interface Changes

**File: `src/KnockOff/IMethodTracking.cs`**

1. Remove line 9: `bool WasCalled { get; }` from `IMethodTracking`
2. Update XML docs in all three interfaces

### Phase 2: Shared Method Interceptor Renderer

**File: `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs`**

1. **RenderMethodTrackingImpl** (lines 621-779):
   - Remove line 664: `w.Line("public bool WasCalled => CallCount > 0;");`
   - Keep `internal int CallCount { get; private set; }` (line 659)
   - Keep `Verify()` methods (lines 710-721)

2. **RenderBackwardCompatibleTrackingProperties** (lines 917-949):
   - Remove line 929: `w.Line("public bool WasCalled => CallCount > 0;");`
   - Update line 937: Change `_onCallTracking?.WasCalled == true` to `_onCallTracking?.CallCount > 0`
   - Update line 946: Same change for `LastCallArgs`

3. **RenderOverloadBackwardCompatibleProperties** (lines 955-971):
   - Remove line 969: `w.Line("public bool WasCalled => CallCount > 0;");`

4. **Add new method `RenderInterceptorVerifyMethods`:**
   ```csharp
   private static void RenderInterceptorVerifyMethods(CodeWriter w, string methodName)
   {
       w.Line("/// <summary>Verifies method was called at least once. Throws VerificationException if not.</summary>");
       w.Line("public void Verify() => Verify(global::KnockOff.Times.AtLeastOnce);");
       w.Line();

       w.Line("/// <summary>Verifies call count satisfies the Times constraint. Throws VerificationException if not.</summary>");
       w.Line("public void Verify(global::KnockOff.Times times)");
       using (w.Braces())
       {
           w.Line("if (!times.Validate(CallCount))");
           w.Line($"\tthrow new global::KnockOff.VerificationException(new global::KnockOff.VerificationFailure(\"{methodName}\", times, CallCount));");
       }
       w.Line();
   }
   ```

5. Call `RenderInterceptorVerifyMethods` from:
   - `RenderSingleSignatureContent` (after line 104)
   - `RenderOverloadGroupContent` (after line 204)

### Phase 3: Flat Stubs Renderer

**File: `src/Generator/Renderer/FlatRenderer.cs`**

1. **IGenericMethodCallTracker** (line 231):
   - Remove `bool WasCalled { get; }` from interface

2. **Method interceptor** (around line 1057):
   - Remove `WasCalled` property
   - Add `Verify()` methods

3. **Overload group** (around line 1440):
   - Remove `WasCalled` property
   - Add `Verify()` methods

4. **MethodTrackingImpl** (around line 1576):
   - Remove `WasCalled` property

5. **Generic method handler** (around lines 1703, 1763):
   - Remove `WasCalled` aggregate
   - Add `Verify()` methods

6. **Internal tracking checks** (lines 908, 1297, 1531):
   - Change `tracking.WasCalled` to `tracking.CallCount > 0`

7. **Generic method handler** (lines 1699-1703):
   - Remove aggregate `WasCalled` property
   - Add `Verify()` methods to aggregate handler for consistency

**Note:** Indexer interceptors already have all verification methods (lines 711-764) - no changes needed.

### Phase 4: Inline Stubs Renderer

**File: `src/Generator/Renderer/InlineRenderer.cs`**

1. **IGenericMethodCallTracker** (line 61):
   - Remove `bool WasCalled { get; }` from interface

2. **Generic method handler** (lines 710, 787):
   - Remove `WasCalled` properties

3. **Delegate interceptor** (line 1263):
   - Remove `WasCalled` property
   - Add `Verify()` methods

4. **Generic method handler** (lines 706-710):
   - Remove aggregate `WasCalled` property
   - Add `Verify()` methods to aggregate handler for consistency

**Note:** Indexer interceptors already have all verification methods (lines 532-576) - no changes needed.

### Phase 5: Class Stubs Renderer

**File: `src/Generator/Renderer/ClassRenderer.cs`**

1. **Method interceptor** (line 360):
   - Remove `WasCalled` property
   - Add `Verify()` methods (after line 410)

### Phase 6: Documentation

**File: `docs/guides/verification.md`**

1. Remove lines 388-395 (WasGot/WasSet section that doesn't match reality)
2. Add migration section:
   ```markdown
   ## Migration from WasCalled

   **Before (v0.x):**
   ```csharp
   Assert.True(stub.GetById.WasCalled);
   ```

   **After (v11.x):**
   ```csharp
   stub.GetById.Verify();  // Throws if not called
   // Or for manual assertion:
   Assert.True(stub.GetById.CallCount > 0);
   ```
   ```

3. Update examples to show direct interceptor verification

---

## Acceptance Criteria

- [x] `IMethodTracking.WasCalled` property is removed from all interfaces
- [x] `WasCalled` is removed from all generated code locations
- [x] Method interceptors have `Verify()` and `Verify(Times)` methods (add to MethodInterceptorRenderer.cs)
- [x] Generic method aggregate handlers have `Verify()` methods (FlatRenderer.cs, InlineRenderer.cs)
- [x] All three patterns work: Stand-Alone, Inline Interface, Inline Class
- [x] Generated code compiles without errors
- [x] **All tests pass** - ~226 WasCalled usages migrated across ~38 test files
- [x] Documentation updated with migration guide (remove WasGot/WasSet references)
- [x] Major version bumped in `Directory.Build.props`

**Note:** Indexer interceptors already have verification methods - no acceptance criteria needed.

---

## Dependencies

- Roslyn source generator infrastructure (no changes)
- `KnockOff.Times` struct (existing, no changes)
- `KnockOff.VerificationException` (existing, no changes)
- `KnockOff.VerificationFailure` (existing, no changes)

---

## Risks / Considerations

### Breaking Change
- **Impact:** High - tests using `WasCalled` will fail
- **Mitigation:** Clear migration path (WasCalled → Verify())
- **Decision:** Acceptable per requirements

### Test Migration Scope
- ~226 test usages across ~36 files require migration (Phase 9)
- Migration is mechanical: `WasCalled` -> `Verify()` or `Verify(Times.Never)`
- Edge case: local variables named `*WasCalled` are NOT API usage (leave unchanged)
- Edge case: comments mentioning WasCalled need text updates

### Indexer Verification (CORRECTED)
- **Indexers already have Verify() methods** (verified in codebase)
- FlatRenderer.cs lines 711-764: `Verify()`, `VerifyGet()`, `VerifySet()` all present
- InlineRenderer.cs lines 532-576: Same methods present
- No indexer changes needed - this was a documentation error in the initial plan

### CallCount Visibility
- `CallCount` is `internal` but used in verification
- Keep internal - users should use `Verify()` not inspect counts directly
- If needed for advanced scenarios, users can access in generated code context

### Documentation Accuracy
- Current docs mention WasGot/WasSet which don't exist
- Must fix documentation to match reality

---

## Architectural Verification

### Three Patterns Analysis

**Standalone (`[KnockOff]` on class):**
- WasCalled removed from: FlatRenderer.cs (lines 231, 908, 1057, 1297, 1440, 1531, 1576, 1703, 1763)
- Verify() added to method interceptors via MethodInterceptorRenderer.cs (shared)
- Generic method handlers need aggregate Verify() methods added
- Indexers: Already have Verify() methods (lines 711-764) - no changes needed

**Inline Interface (`[KnockOff<IFoo>]`):**
- WasCalled removed from: InlineRenderer.cs (lines 61, 710, 787, 1263)
- Method interceptors use MethodInterceptorRenderer.cs (shared) - gets Verify() automatically
- Generic method handlers need aggregate Verify() methods added
- Indexers: Already have Verify() methods (lines 532-576) - no changes needed

**Inline Class (`[KnockOff<MyClass>]`):**
- WasCalled removed from: ClassRenderer.cs (line 360)
- ClassRenderer already has Verify() methods (lines 417-426) - only need WasCalled removal
- No indexer support in class stubs (classes don't typically expose indexers)

### Breaking Changes Assessment

**Impact:** HIGH - 641 occurrences of `.WasCalled` across 141 files in repo

**Breaking Changes:**
1. `IMethodTracking.WasCalled` - removed from public interface
2. `IMethodTracking<TArg>.WasCalled` - inherited removal
3. `IMethodTrackingArgs<TArgs>.WasCalled` - inherited removal
4. Interceptor-level `WasCalled` properties - removed from all generated code

**Migration Path:**
- `stub.Method.WasCalled` -> `stub.Method.Verify()` (throws) or `stub.Method.CallCount > 0` (boolean)
- `tracking.WasCalled` -> `tracking.Verify()` or `tracking.CallCount > 0` (note: CallCount is internal on tracking)

**Backward Compatibility:** None - this is a clean break requiring major version bump

### Pattern Consistency Check

| Interceptor Type | Verify() | Verify(Times) | VerifyGet() | VerifySet() | Status |
|-----------------|----------|---------------|-------------|-------------|--------|
| Properties | Yes | Yes | Yes | Yes | Existing |
| Indexers | Yes | Yes | Yes | Yes | Existing |
| Methods (single) | **ADD** | **ADD** | N/A | N/A | Needs work |
| Methods (overload) | **ADD** | **ADD** | N/A | N/A | Needs work |
| Generic handlers | **ADD** | **ADD** | N/A | N/A | Needs work |
| Delegates | Yes | Yes | N/A | N/A | Existing |
| Class methods | Yes | Yes | N/A | N/A | Existing |

### Diagnostic Requirements

No new diagnostics needed. WasCalled removal will cause compile errors (method not found), which is the correct user experience for a breaking change.

### Test Strategy

1. **Verify removal:** Regenerate all stubs, confirm no `WasCalled` in generated code
2. **Verify Verify():** Test that `stub.Method.Verify()` throws when not called
3. **Verify Verify(Times):** Test various Times constraints on interceptors
4. **All patterns:** One test per pattern (Standalone, Inline Interface, Inline Class)
5. **Edge cases:** Generic methods, overloaded methods, sequence tracking

### Edge Cases Documented

1. **Generic method aggregate handlers:** Need Verify() that aggregates across all typed handlers
2. **Overload groups:** Verify() should check aggregate CallCount across all overloads
3. **Sequence tracking:** MethodTrackingImpl.WasCalled removed but Verify() remains for per-callback checks
4. **Internal usage:** `tracking.WasCalled` used internally in FlatRenderer (lines 908, 1297, 1531) - change to `CallCount > 0`
5. **IGenericMethodCallTracker interface:** Remove `WasCalled` from both FlatRenderer (line 231) and InlineRenderer (line 61)

### Codebase Deep-Dive

**Files Examined:**
- `src/KnockOff/IMethodTracking.cs` - Interface with WasCalled (line 9), XML docs referencing it (lines 47, 70)
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` - Shared renderer, WasCalled at lines 664, 929, 969
- `src/Generator/Renderer/FlatRenderer.cs` - Standalone stubs, WasCalled at lines 231, 908, 1057, 1297, 1440, 1531, 1576, 1703, 1763
- `src/Generator/Renderer/InlineRenderer.cs` - Inline stubs, WasCalled at lines 61, 710, 787, 1263
- `src/Generator/Renderer/ClassRenderer.cs` - Class stubs, WasCalled at line 360, Verify() already at lines 417-426
- `docs/guides/verification.md` - WasGot/WasSet references at lines 386-395 (incorrect, don't exist)

**Key Findings:**
1. Indexers already have full verification API - plan incorrectly stated they lacked it
2. ClassRenderer already has Verify() methods - only needs WasCalled removal
3. 641 test usages will break - this is expected and intentional
4. Internal tracking checks use `tracking.WasCalled` - must change to `CallCount > 0`

### Verification Checklist

- [x] All three patterns analyzed (Standalone, Inline Interface, Inline Class)
- [x] Breaking changes assessment completed
- [x] Pattern consistency verified
- [x] Diagnostic requirements identified (none needed)
- [x] Test strategy defined
- [x] Edge cases documented
- [x] Codebase deep-dive completed (files listed above)

---

## Developer Review

**Status:** Approved

**Architect Notes for Developer:**
1. Plan has been corrected - indexers already have Verify() methods, no changes needed there
2. ClassRenderer already has Verify() methods - only remove WasCalled
3. Key additions needed: Verify() on method interceptors (MethodInterceptorRenderer.cs) and generic handlers
4. Internal tracking checks (3 locations) must change `tracking.WasCalled` to `tracking.CallCount > 0`
5. Test impact: 641 usages across 141 files will break (intentional)

**Developer Verification (2026-01-22):**

Verified all WasCalled locations against codebase:

| File | Line | Verified | Notes |
|------|------|----------|-------|
| `IMethodTracking.cs` | 9 | Yes | Interface property |
| `IMethodTracking.cs` | 47 | Yes | XML doc "check WasCalled" |
| `IMethodTracking.cs` | 70 | Yes | XML doc "check WasCalled" |
| `MethodInterceptorRenderer.cs` | 664 | Yes | MethodTrackingImpl.WasCalled |
| `MethodInterceptorRenderer.cs` | 929 | Yes | Backward-compat aggregate WasCalled |
| `MethodInterceptorRenderer.cs` | 937 | Yes | `_onCallTracking?.WasCalled == true` (internal) |
| `MethodInterceptorRenderer.cs` | 946 | Yes | `_onCallTracking?.WasCalled == true` (internal) |
| `MethodInterceptorRenderer.cs` | 969 | Yes | Overload aggregate WasCalled |
| `FlatRenderer.cs` | 231 | Yes | IGenericMethodCallTracker interface |
| `FlatRenderer.cs` | 908 | Yes | `tracking.WasCalled` (internal verification) |
| `FlatRenderer.cs` | 1057 | Yes | MethodTrackingImpl WasCalled |
| `FlatRenderer.cs` | 1297 | Yes | `tracking.WasCalled` (internal verification) |
| `FlatRenderer.cs` | 1440 | Yes | Overload group WasCalled |
| `FlatRenderer.cs` | 1531 | Yes | `tracking.WasCalled` (internal verification) |
| `FlatRenderer.cs` | 1576 | Yes | GroupMethodTrackingImpl WasCalled |
| `FlatRenderer.cs` | 1703 | Yes | Generic handler aggregate WasCalled |
| `FlatRenderer.cs` | 1763 | Yes | TypedHandler WasCalled |
| `InlineRenderer.cs` | 61 | Yes | IGenericMethodCallTracker interface |
| `InlineRenderer.cs` | 710 | Yes | Generic handler aggregate WasCalled |
| `InlineRenderer.cs` | 787 | Yes | TypedHandler WasCalled |
| `InlineRenderer.cs` | 1263 | Yes | Delegate interceptor WasCalled |
| `ClassRenderer.cs` | 360 | Yes | Method interceptor WasCalled |

**Additional Findings:**
1. MethodInterceptorRenderer lines 937 and 946 also use WasCalled internally in `LastCallArg`/`LastCallArgs` getters - must update to `CallCount > 0`
2. Delegate interceptors in InlineRenderer do NOT have Verify() methods - must add them (architect noted "Existing" in pattern table but this is incorrect)
3. ClassRenderer already has Verify() at lines 417-426 - confirmed, only need WasCalled removal

**Concerns:** None - ready for implementation

**Correction to Pattern Consistency Table:**
- Delegates are listed as having Verify() methods, but InlineRenderer delegate interceptor (lines 1252-1312) does NOT have Verify(). This must be added.

---

## Implementation Contract

### Phase 1: Core Interface Changes
**File: `src/KnockOff/IMethodTracking.cs`**
- [x] 1.1 Remove line 9: `bool WasCalled { get; }`
- [x] 1.2 Update XML doc line 47: Remove "(check WasCalled)" from `IMethodTracking<TArg>.LastArg` doc
- [x] 1.3 Update XML doc line 70: Remove "(check WasCalled)" from `IMethodTrackingArgs<TArgs>.LastArgs` doc
- [x] **Checkpoint:** Build solution - expect compile errors in generated code (WasCalled not found)

### Phase 2: Shared Method Interceptor Renderer
**File: `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs`**
- [x] 2.1 Remove line 664: `w.Line("public bool WasCalled => CallCount > 0;");` in RenderMethodTrackingImpl
- [x] 2.2 Remove line 929: `w.Line("public bool WasCalled => CallCount > 0;");` in RenderBackwardCompatibleTrackingProperties
- [x] 2.3 Update line 937: Change `_onCallTracking?.WasCalled == true` to `(_onCallTracking?.CallCount ?? 0) > 0`
- [x] 2.4 Update line 946: Change `_onCallTracking?.WasCalled == true` to `(_onCallTracking?.CallCount ?? 0) > 0`
- [x] 2.5 Remove line 969: `w.Line("public bool WasCalled => CallCount > 0;");` in RenderOverloadBackwardCompatibleProperties
- [x] 2.6 Add `RenderInterceptorVerifyMethods` helper method (after line 949)
- [x] 2.7 Call `RenderInterceptorVerifyMethods` in `RenderSingleSignatureContent` (after backward-compat properties, before OnCall)
- [x] 2.8 Call `RenderInterceptorVerifyMethods` in `RenderOverloadGroupContent` (after backward-compat properties, before OnCall overloads)
- [x] **Checkpoint:** Build solution - check for remaining WasCalled errors

### Phase 3: Flat Stubs Renderer
**File: `src/Generator/Renderer/FlatRenderer.cs`**
- [x] 3.1 Remove line 231: `w.Line("bool WasCalled { get; }");` from IGenericMethodCallTracker
- [x] 3.2 Update line 908: Change `tracking.WasCalled` to `tracking.CallCount > 0`
- [x] 3.3 Remove lines 1055-1057: WasCalled property section
- [x] 3.4 Update line 1297: Change `tracking.WasCalled` to `tracking.CallCount > 0`
- [x] 3.5 Remove line 1440: `w.Line("public bool WasCalled => CallCount > 0;");`
- [x] 3.6 Update line 1531: Change `tracking.WasCalled` to `tracking.CallCount > 0`
- [x] 3.7 Remove lines 1574-1576: WasCalled property section
- [x] 3.8 Remove line 1703: aggregate WasCalled property from generic handler
- [x] 3.9 Add Verify() methods to generic handler aggregate (after TotalCallCount)
- [x] 3.10 Remove lines 1761-1763: WasCalled from TypedHandler
- [x] 3.11 Add Verify() methods to TypedHandler (implement IMethodTracking.Verify)
- [x] **Checkpoint:** Build solution - verify FlatRenderer changes compile

### Phase 4: Inline Stubs Renderer
**File: `src/Generator/Renderer/InlineRenderer.cs`**
- [x] 4.1 Remove `bool WasCalled { get; }` from IGenericMethodCallTracker (line 61)
- [x] 4.2 Remove line 710: aggregate WasCalled property from generic handler
- [x] 4.3 Add Verify() methods to generic handler aggregate
- [x] 4.4 Remove line 787: WasCalled from TypedHandler
- [x] 4.5 Add Verify() methods to TypedHandler (implement IMethodTracking.Verify)
- [x] 4.6 Remove line 1263: WasCalled from delegate interceptor
- [x] 4.7 Add Verify() and Verify(Times) to delegate interceptor (after Reset method)
- [x] **Checkpoint:** Build solution - verify InlineRenderer changes compile

### Phase 5: Class Stubs Renderer
**File: `src/Generator/Renderer/ClassRenderer.cs`**
- [x] 5.1 Remove lines 356-360: WasCalled property section (ClassRenderer already has Verify() at 417-426)
- [x] **Checkpoint:** Build solution - verify ClassRenderer changes compile

### Phase 6: Documentation
**File: `docs/guides/verification.md`**
- [x] 6.1 Remove lines 386-395: Incorrect WasGot/WasSet documentation
- [x] 6.2 Add migration section explaining WasCalled removal
- [x] 6.3 Update any other WasCalled references in the document

### Phase 7: Version Bump
**File: `Directory.Build.props`**
- [x] 7.1 Bump major version (0.24.0 -> 11.0.0)

### Phase 8: Final Generator Verification
- [x] 8.1 Run `dotnet build` on entire solution (expect test compile errors)
- [x] 8.2 Verify no WasCalled in Generated/ folders (search generated files)
- [x] 8.3 Verify Verify() methods exist on method interceptors in generated code
- [x] 8.4 Create release notes in `docs/release-notes/`

### Phase 9: Test Migration

**Scope:** Migrate all ~226 `WasCalled` usages across ~36 test files in `src/Tests/`

**Test Folders Requiring Migration:**
- `src/Tests/KnockOffTests/` - Core tests (~100 usages across 18 files)
- `src/Tests/KnockOff.NeatooInterfaceTests/` - Neatoo interface tests (~100 usages across 16 files)
- `src/Tests/KnockOff.Documentation.Samples/` - Documentation samples (~4 usages across 2 files)

**Migration Patterns:**

| Original Pattern | Replacement | Notes |
|-----------------|-------------|-------|
| `Assert.True(stub.Method.WasCalled)` | `stub.Method.Verify()` | Direct interceptor - throws if not called |
| `Assert.False(stub.Method.WasCalled)` | `stub.Method.Verify(Times.Never)` | Verifies zero calls |
| `Assert.True(tracking.WasCalled)` | `tracking.Verify()` | OnCall tracking object |
| `Assert.False(tracking.WasCalled)` | `tracking.Verify(Times.Never)` | OnCall tracking - zero calls |
| Comment mentioning WasCalled | Update comment text | e.g., "Reset clears CallCount, WasCalled" |

**Edge Cases to Watch:**

1. **Comments mentioning WasCalled** - Update comment text (e.g., `MethodsSamples.cs:281`)
2. **Local variables named `*WasCalled`** - These are user variables, NOT API usage (e.g., `InterceptorApiSamples.cs:147` has `setWasCalled` local variable - leave unchanged)
3. **Tracking objects from OnCall()** - Use `tracking.Verify()` or `tracking.Verify(Times.Never)`
4. **Multiple tracking objects** - Migrate each independently (e.g., `OverloadedMethodTests.cs:256-258`)

**Checklist by Test Project:**

**KnockOffTests/ (18 files):**
- [x] 9.1 `BclStandaloneTests.cs` (21 usages)
- [x] 9.2 `CallbackTests.cs` (4 usages)
- [x] 9.3 `GenericInheritanceTypeMismatchBugTests.cs` (2 usages)
- [x] 9.4 `GenericInterfaceTests.cs` (2 usages)
- [x] 9.5 `GenericMethodBugTests.cs` (2 usages)
- [x] 9.6 `GenericMethodTests.cs` (7 usages)
- [x] 9.7 `GenericStandaloneStubTests.cs` (2 usages)
- [x] 9.8 `InlineStubBugTests.cs` (4 usages)
- [x] 9.9 `InlineStubTests.cs` (21 usages)
- [x] 9.10 `KOPropertyCollisionTests.cs` (1 usage)
- [x] 9.11 `NamespaceCollisionTests.cs` (2 usages)
- [x] 9.12 `NeatooTests.cs` (25 usages)
- [x] 9.13 `OpenGenericInlineStubTests.cs` (2 usages)
- [x] 9.14 `OutParameterTests.cs` (2 usages)
- [x] 9.15 `OverloadedMethodTests.cs` (5 usages)
- [x] 9.16 `RefParameterTests.cs` (3 usages)
- [x] 9.17 `ReturnTypeMismatchBugTests.cs` (1 usage)
- [x] 9.18 `SequencingTests.cs` (1 usage)

**KnockOff.NeatooInterfaceTests/ (16 files):**
- [x] 9.19 `BuiltInRules/IRequiredRuleTests.cs` (2 usages)
- [x] 9.20 `BuiltInRules/OtherBuiltInRuleTests.cs` (3 usages)
- [x] 9.21 `Collections/IEntityListBaseTests.cs` (8 usages)
- [x] 9.22 `Collections/IValidateListBaseTests.cs` (11 usages)
- [x] 9.23 `MetaProperties/IValidateMetaPropertiesTests.cs` (8 usages)
- [x] 9.24 `Notifications/INotifyNeatooPropertyChangedTests.cs` (2 usages)
- [x] 9.25 `Properties/IEntityPropertyTests.cs` (6 usages)
- [x] 9.26 `Properties/IPropertyInfoTests.cs` (10 usages)
- [x] 9.27 `Properties/IValidatePropertyTests.cs` (7 usages)
- [x] 9.28 `PropertyManagers/IEntityPropertyManagerTests.cs` (10 usages)
- [x] 9.29 `PropertyManagers/IValidatePropertyManagerTests.cs` (11 usages)
- [x] 9.30 `ValidationRules/IRuleManagerTests.cs` (12 usages)
- [x] 9.31 `ValidationRules/IRuleMessagesTests.cs` (12 usages)
- [x] 9.32 `ValidationRules/IRuleOfTTests.cs` (4 usages)
- [x] 9.33 `ValidationRules/IRuleTests.cs` (4 usages)
- [x] 9.34 `ValidationRules/ITriggerPropertyTests.cs` (6 usages)

**KnockOff.Documentation.Samples/ (2 files):**
- [x] 9.35 `MethodsSamples.cs` (2 usages + 1 comment)
- [x] 9.36 `TroubleshootingSamples.cs` (1 usage)

### Phase 10: Final Verification
- [x] 10.1 Run `dotnet build` on entire solution - must succeed
- [x] 10.2 Run `dotnet test` on entire solution - all tests must pass
- [x] 10.3 Verify no remaining WasCalled API usage in test files (grep confirmation)
- [x] 10.4 Finalize release notes

**Out of Scope:**
- Indexer interceptors (already have Verify() methods - verified)
- Property interceptors (already have Verify() methods)
- ClassRenderer Verify() methods (already exist at lines 417-426)
- Event interceptors (no WasCalled, no changes needed)
- User-defined local variables named `*WasCalled` (not API usage)

---

## Implementation Progress

### 2026-01-22: Phase 1-8 Complete
- Removed `WasCalled` from all interfaces and generator renderers
- Added `Verify()` and `Verify(Times)` methods to all method interceptors
- Updated documentation with migration guide
- Bumped version to 11.0.0
- All generator changes verified - no WasCalled in generated code

### 2026-01-22: Phase 9 Complete
- Migrated ~226 WasCalled usages across 38 test files
- Migration patterns applied:
  - `Assert.True(stub.Method.WasCalled)` -> `stub.Method.Verify()`
  - `Assert.False(stub.Method.WasCalled)` -> `stub.Method.Verify(Times.Never)`
  - `Assert.True(tracking.WasCalled)` -> `tracking.Verify()`
- All three test projects migrated:
  - KnockOffTests (18 files)
  - KnockOff.NeatooInterfaceTests (16 files)
  - KnockOff.Documentation.Samples (2 files)

### 2026-01-22: Phase 10 Complete
- Final build verification: **Build succeeded. 0 Warning(s) 0 Error(s)**
- Final test verification: **Test Run Successful. Total tests: 607 Passed: 607**
- Grep confirmation: No remaining `.WasCalled` usages in Tests or Benchmarks folders

---

## Completion Evidence

### Build Output
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### Test Output
```
Test Run Successful.
Total tests: 607
     Passed: 607
 Total time: 4.2815 Seconds
```

### Generated Code Sample (Method Interceptor with Verify)
```csharp
public sealed class GetByIdInterceptor
{
    public int CallCount { get; private set; }

    /// <summary>Verifies method was called at least once.</summary>
    public void Verify() => Verify(global::KnockOff.Times.AtLeastOnce);

    /// <summary>Verifies call count satisfies the Times constraint.</summary>
    public void Verify(global::KnockOff.Times times)
    {
        if (!times.Validate(CallCount))
            throw new global::KnockOff.VerificationException(
                new global::KnockOff.VerificationFailure("GetById", times, CallCount));
    }

    // ... rest of interceptor
}
```

### Grep Verification
- No `.WasCalled` pattern found in `src/Tests/` folder
- No `.WasCalled` pattern found in `src/Benchmarks/` folder
- Confirmed API removal is complete

### All Checklist Items: 100% Complete
- All 10 phases completed
- All 48+ individual checklist items marked as done
- All acceptance criteria met
