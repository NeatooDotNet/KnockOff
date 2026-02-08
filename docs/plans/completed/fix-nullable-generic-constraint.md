# Fix Spurious `where T : class` on Nullable Unconstrained Generic Methods

**Date:** 2026-02-08
**Related Todo:** [Spurious class constraint bug](../todos/completed/nullable-generic-spurious-class-constraint.md)
**Status:** Complete
**Last Updated:** 2026-02-08

---

## Overview

The generator's `GetConstraintsForExplicitImpl` method incorrectly adds `where T : class` to explicit interface implementations when the method uses `T?` on an unconstrained type parameter. In C# 9+, `T?` on an unconstrained type parameter means "default value" (null for reference types, default for value types) and does NOT require a `class` constraint. Adding one causes CS8665.

The previous fix (for `T? GetCustomAttribute<T>() where T : Attribute`) correctly added `where T : class` when the type parameter has a constraint implying reference type. However, the implementation over-corrected: it checks `returnType.Contains("T?")` without verifying that the type parameter has any constraint at all.

---

## Root Cause Analysis

### The Buggy Code

Identical code exists in two files:

**`src/Generator/Builder/FlatModelBuilder.cs` lines 1003-1032:**
```csharp
private static string GetConstraintsForExplicitImpl(TypeParameterInfo[] typeParams, string returnType)
{
    var clauses = new List<string>();
    foreach (var tp in typeParams)
    {
        var constraintArray = tp.Constraints.GetArray() ?? Array.Empty<string>();

        if (constraintArray.Contains("struct")) { ... continue; }
        if (constraintArray.Contains("class")) { ... continue; }

        // BUG: This fires for ANY nullable return type, even unconstrained T?
        if (returnType.Contains($"{tp.Name}?") || returnType.EndsWith($"{tp.Name}?"))
        {
            clauses.Add($" where {tp.Name} : class");
        }
    }
    return string.Join("", clauses);
}
```

**`src/Generator/Builder/InlineModelBuilder.cs` lines 1452-1477:** (identical logic)

### Why It Is Wrong

For `TData? NullableValues<TData>(TData? data)`:
- `tp.Constraints` is **empty** (no constraints on TData)
- `returnType` is `"TData?"` which contains `"TData?"`
- The third check fires and adds `where TData : class`
- C# rejects this with CS8665 because the interface method has no such constraint

For `T? GetCustomAttribute<T>() where T : Attribute`:
- `tp.Constraints` is `["global::System.Attribute"]`
- Neither "struct" nor "class" is in constraints (correct, those checks pass through)
- `returnType` is `"T?"` which contains `"T?"`
- Adding `where T : class` is correct here because `Attribute` implies reference type

### The Missing Condition

The third check needs to verify that the type parameter has constraints that **imply reference type** before adding `where T : class`. The correct condition:

```
T has explicit "class" constraint → already handled by check 2
T has "struct" constraint → already handled by check 1
T has type constraints that are classes (e.g., Attribute) → SHOULD add "where T : class"
T has no constraints → MUST NOT add "where T : class"
T has only notnull/new()/unmanaged/interface constraints → MUST NOT add "where T : class"
```

---

## Approach: Add `IsKnownReferenceType` to `TypeParameterInfo`

### Why Model Change Is Best

The constraint strings in `TypeParameterInfo.Constraints` don't carry enough information -- you can't tell from the string `"global::System.Attribute"` whether it's a class, interface, or struct. The Roslyn `ITypeParameterSymbol` has this information at extraction time, so we should capture it then.

### Design (Revised per Developer Concern #1)

The developer correctly identified that the original proposed computation (`tp.ConstraintTypes.Any(ct => ct.IsReferenceType)`) is wrong for interface constraints. In Roslyn, `IDisposable.IsReferenceType == true` because interfaces are reference types in the CLR. However, `where T : IDisposable` does NOT constrain `T` to be a reference type -- structs can implement interfaces.

**The correct approach uses `ITypeParameterSymbol.IsReferenceType` directly.** Roslyn already performs the full semantic analysis on the type parameter itself, correctly accounting for all constraint combinations:

| Constraint | `tp.IsReferenceType` | Correct `where T : class`? |
|------------|---------------------|-----------------------------|
| No constraints | `false` | NO |
| `where T : class` | `true` | YES (but already handled by check 2) |
| `where T : Attribute` | `true` | YES |
| `where T : IDisposable` | `false` | NO |
| `where T : class, IDisposable` | `true` | YES (but already handled by check 2) |
| `where T : notnull` | `false` | NO |
| `where T : struct` | `false` | NO (already handled by check 1) |

This is simpler, more correct, and delegates the semantic analysis to Roslyn rather than reimplementing it.

**1. Extend `TypeParameterInfo` record** (`src/Generator/Models/InterfaceModels.cs`):

```csharp
internal sealed record TypeParameterInfo(
    string Name,
    EquatableArray<string> Constraints,
    bool IsKnownReferenceType = false) : IEquatable<TypeParameterInfo>;
```

`IsKnownReferenceType` is set directly from `ITypeParameterSymbol.IsReferenceType`.

Using a default parameter value of `false` means all existing creation sites continue to work without modification. Only the sites that extract from Roslyn symbols need updating.

**2. Set `IsKnownReferenceType` at extraction sites:**

There are 3 sites where `TypeParameterInfo` is created from Roslyn symbols:

a. `src/Generator/Models/InterfaceModels.cs` line 249 -- `InterfaceMemberInfo.FromMethod()`
b. `src/Generator/Models/ClassModels.cs` line 187 -- `ClassMemberInfo.FromMethod()`
c. `src/Generator/Models/SymbolHelpers.cs` line 413 -- `ExtractTypeParameters()`

At each site, simply read the existing Roslyn property:
```csharp
new TypeParameterInfo(
    tp.Name,
    new EquatableArray<string>(SymbolHelpers.GetTypeParameterConstraints(tp).ToArray()),
    tp.IsReferenceType)
```

No manual constraint analysis needed -- Roslyn has already done it.

**3. Fix `GetConstraintsForExplicitImpl` in both builders:**

Replace the third check with:

```csharp
// If type parameter is known to be a reference type (e.g., where T : Attribute, where T : class)
// and the return type uses T?, we need "where T : class" for explicit impl.
// NOTE: Interface-only constraints (e.g., where T : IDisposable) do NOT make T a reference type.
if (tp.IsKnownReferenceType
    && (returnType.Contains($"{tp.Name}?") || returnType.EndsWith($"{tp.Name}?")))
{
    clauses.Add($" where {tp.Name} : class");
}
```

This change is in:
- `src/Generator/Builder/FlatModelBuilder.cs` (standalone patterns 1, 2)
- `src/Generator/Builder/InlineModelBuilder.cs` (inline patterns 5, 7, 8)

---

## Affected Pipelines

| Pipeline | Builder | Has `GetConstraintsForExplicitImpl`? | Affected? |
|----------|---------|--------------------------------------|-----------|
| Standalone (1, 2) | `FlatModelBuilder` | YES (line 1003) | **YES** |
| Standalone Class (3, 4) | `StandaloneClassModelBuilder` | NO -- uses `GetConstraintClauses` with explicit empty string for overrides | No |
| Inline Interface (5) | `InlineModelBuilder` | YES (line 1452) | **YES** |
| Inline Class (6) | `ClassModelBuilder` | NO -- uses `GetConstraintClauses` | No |
| Inline Delegate (7) | N/A | N/A | No |
| Open Generic Interface (8) | `InlineModelBuilder` | YES (shares code) | **YES** |
| Open Generic Class (9) | `ClassModelBuilder` | NO | No |

Class stubs (patterns 3, 4, 6, 9) use method `override` rather than explicit interface implementation, and C# inherits constraints from the base automatically. These pipelines already correctly emit empty constraint clauses for overrides.

---

## Pattern Analysis (Nine Patterns)

| Pattern | Applicable? | Affected by Bug? | Notes |
|---------|-------------|-------------------|-------|
| 1. Standalone | YES -- uses explicit impl | **YES** | FlatModelBuilder |
| 2. Generic Standalone | YES -- uses explicit impl | **YES** | FlatModelBuilder |
| 3. Standalone Class | No -- uses override | No | Constraints inherited from base |
| 4. Generic Standalone Class | No -- uses override | No | Constraints inherited from base |
| 5. Inline Interface | YES -- uses explicit impl | **YES** | InlineModelBuilder |
| 6. Inline Class | No -- uses override | No | Constraints inherited from base |
| 7. Inline Delegate | N/A | No | Delegates don't have generic methods |
| 8. Open Generic Interface | YES -- uses explicit impl | **YES** | InlineModelBuilder |
| 9. Open Generic Class | No -- uses override | No | Constraints inherited from base |

---

## Design.Stubs Verification

### Files Created (Updated per Developer Concern #2)

**`src/Design/Design.Domain/Services/INullableGenericService.cs`** -- Interface with:
- `TData? NullableValues<TData>(TData? data)` -- unconstrained nullable (bug case)
- `T? NullableReturn<T>()` -- unconstrained nullable (bug case)
- `T? InterfaceConstrainedReturn<T>() where T : IDisposable` -- interface-only constraint (bug case, added per Concern #2)
- `T? ConstrainedNullableReturn<T>() where T : Attribute` -- class constraint (regression case, must keep working)

**`src/Design/Design.Stubs/Methods/NullableGenericMethods.cs`** -- Stubs for:
- Pattern 1 (Standalone): `NullableGenericServiceStub : INullableGenericService`
- Pattern 5 (Inline Interface): `[KnockOff<INullableGenericService>] NullableGenericInlineTests`

### Current Compilation Status

`dotnet build src/Design/Design.Stubs` produces **CS8665** errors on 3 methods (x 2 patterns x 3 target frameworks = 18 errors):
- `NullableValues<TData>` -- spurious `where TData : class` (unconstrained)
- `NullableReturn<T>` -- spurious `where T : class` (unconstrained)
- `InterfaceConstrainedReturn<T>` -- spurious `where T : class` (interface-only constraint)

**`ConstrainedNullableReturn<T>() where T : Attribute` compiles successfully** -- confirming the existing fix works for the class-constrained case.

### Acceptance Criteria

The Design.Stubs code is the acceptance criteria. When the fix is correct:
1. `dotnet build src/Design/Design.Stubs` succeeds with zero errors
2. All four methods compile:
   - `NullableValues<TData>` -- no constraint emitted
   - `NullableReturn<T>` -- no constraint emitted
   - `InterfaceConstrainedReturn<T>` -- no constraint emitted
   - `ConstrainedNullableReturn<T>` -- `where T : class` emitted (regression guard)
3. `dotnet test src/Design/Design.Tests` continues to pass
4. `dotnet test src/KnockOff.sln` continues to pass (all existing tests)

---

## Implementation Steps

### Phase 1: Model Change

1. Add `IsKnownReferenceType` parameter (default `false`) to `TypeParameterInfo` record in `src/Generator/Models/InterfaceModels.cs`
2. Update the 3 creation sites to pass `tp.IsReferenceType`:
   - `InterfaceMemberInfo.FromMethod()` in `InterfaceModels.cs`
   - `ClassMemberInfo.FromMethod()` in `ClassModels.cs`
   - `SymbolHelpers.ExtractTypeParameters()` in `SymbolHelpers.cs`

### Phase 2: Fix Builders

3. Fix `GetConstraintsForExplicitImpl` in `src/Generator/Builder/FlatModelBuilder.cs` -- guard the third check with `tp.IsKnownReferenceType`
4. Fix `GetConstraintsForExplicitImpl` in `src/Generator/Builder/InlineModelBuilder.cs` -- same change

### Phase 3: Verify

5. `dotnet build src/Design/Design.Stubs` -- should now succeed (all 4 methods compile)
6. `dotnet test src/KnockOff.sln` -- all existing tests pass (including constraint bug tests)
7. `dotnet test src/Design/Design.Tests` -- all Design tests pass

### Phase 4: Add Test Cases

8. Add test interface with unconstrained nullable generic methods to `src/Tests/KnockOffTests/TestInterfaces.cs`
9. Add standalone and inline stubs for the test interface
10. Add tests in `src/Tests/KnockOffTests/GenericMethodBugTests.cs` exercising:
    - Unconstrained nullable return works (no CS8665)
    - Unconstrained nullable parameter works
    - Interface-only constraint does NOT get `where T : class`
    - Class constraint (Attribute) still gets `where T : class` (regression test)

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Model change breaks equatability/caching | Low | High | Default parameter value ensures backward compatibility; record auto-generates equality |
| Constrained case regresses | Low | High | `ConstrainedNullableReturn<T>() where T : Attribute` in Design.Stubs verifies this |
| Interface-only constraints incorrectly get `where T : class` | None | High | Using `tp.IsReferenceType` (not `ct.IsReferenceType`) avoids this; `InterfaceConstrainedReturn<T>() where T : IDisposable` in Design.Stubs verifies |
| Type parameter constrained by another type parameter (`where T : U`) | Low | Low | Roslyn's `IsReferenceType` handles this transitively; unlikely to appear in practice |

---

## Codebase Files Examined

| File | Purpose |
|------|---------|
| `src/Generator/Models/InterfaceModels.cs` | `TypeParameterInfo` record definition; `FromMethod()` extraction |
| `src/Generator/Models/ClassModels.cs` | `ClassMemberInfo.FromMethod()` extraction |
| `src/Generator/Models/SymbolHelpers.cs` | `GetTypeParameterConstraints()`, `ExtractTypeParameters()` |
| `src/Generator/Builder/FlatModelBuilder.cs` | `GetConstraintsForExplicitImpl()` -- bug location |
| `src/Generator/Builder/InlineModelBuilder.cs` | `GetConstraintsForExplicitImpl()` -- bug location |
| `src/Generator/Builder/StandaloneClassModelBuilder.cs` | Confirmed NOT affected (uses override, not explicit impl) |
| `src/Generator/Builder/ClassModelBuilder.cs` | Confirmed NOT affected (uses override, not explicit impl) |
| `src/Tests/KnockOffTests/GenericMethodBugTests.cs` | Existing constraint tests |
| `src/Tests/KnockOffTests/TestInterfaces.cs` | `IConstrainedGenericMethod` interface (previous fix) |
| Generated `.g.cs` files | Confirmed spurious `where TData : class` in output |

---

## Architectural Verification

- [x] All nine patterns analyzed
- [x] Design.Stubs compilation verification (failing code in place as acceptance criteria)
- [x] Breaking changes assessment: None -- default parameter value is backward compatible
- [x] Pattern consistency verified: Fix applies to all pipelines that use explicit impl
- [x] Diagnostic requirements: None needed -- this is a silent fix
- [x] Test strategy defined (Phase 4)
- [x] Edge cases documented:
  - Unconstrained `T?` -- must NOT get `where T : class`
  - Interface-only constraint (`where T : IDisposable`) -- must NOT get `where T : class`
  - Class constraint (`where T : Attribute`) -- MUST get `where T : class`
  - Explicit `class` constraint -- already handled by check 2 (unchanged)
- [x] Developer Concern #1 addressed: Use `tp.IsReferenceType` instead of `ct.IsReferenceType`
- [x] Developer Concern #2 addressed: Added `InterfaceConstrainedReturn<T>() where T : IDisposable` to Design.Stubs
- [x] Codebase deep-dive completed

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-02-08 (Re-review)

### Why This Plan Is Approved

Both concerns from the initial review have been addressed:

1. **Concern #1 (CRITICAL) -- RESOLVED.** The `IsKnownReferenceType` computation now uses `ITypeParameterSymbol.IsReferenceType` directly instead of the incorrect `tp.ConstraintTypes.Any(ct => ct.IsReferenceType)`. The truth table on lines 88-97 correctly documents the behavior for all constraint combinations. The field was renamed from `ImpliesReferenceType` to `IsKnownReferenceType` which better reflects the semantics.

2. **Concern #2 (MINOR) -- RESOLVED.** The `InterfaceConstrainedReturn<T>() where T : IDisposable` method was added to `INullableGenericService`. I confirmed it currently produces CS8665 on build (18 total errors: 3 bug-case methods x 2 patterns x 3 frameworks). The regression guard (`ConstrainedNullableReturn<T>() where T : Attribute`) continues to compile.

### Re-Review Verification

**Design.Stubs re-verified:**
- `src/Design/Design.Domain/Services/INullableGenericService.cs` -- Confirmed 4 methods: `NullableValues<TData>`, `NullableReturn<T>`, `InterfaceConstrainedReturn<T>`, `ConstrainedNullableReturn<T>`
- `src/Design/Design.Stubs/Methods/NullableGenericMethods.cs` -- Confirmed Pattern 1 (standalone) and Pattern 5 (inline) stubs
- Build output: 18 CS8665 errors on the 3 bug-case methods; `ConstrainedNullableReturn` compiles (not in error list)

**Revised approach verified:**
- The extraction code `tp.IsReferenceType` is available on `ITypeParameterSymbol` at all 3 extraction sites (confirmed `tp` is `ITypeParameterSymbol` in `method.TypeParameters` LINQ selections)
- The builder fix simply adds `tp.IsKnownReferenceType &&` before the existing nullable return type check -- minimal change, maximal correctness
- The truth table covers: unconstrained, class, struct, class-type (Attribute), interface-only (IDisposable), class+interface, notnull

**Checklist from initial review -- all items now resolved:**
- [x] All nine patterns addressed
- [x] Extraction sites verified (3 sites)
- [x] Builder fix in 2 files
- [x] Design.Stubs cover all 4 edge cases
- [x] Regression guard in place
- [x] Interface constraint edge case covered
- [x] `tp.IsReferenceType` is the correct Roslyn property for this purpose

### Previous Review (Initial -- Concerns Raised)

The initial review identified two concerns. Both have been resolved by the architect. The full initial review is preserved in the "Architect Response" section below for audit trail.

---

## Architect Response to Developer Concerns (2026-02-08)

### Concern #1: ACCEPTED -- `ImpliesReferenceType` computation corrected

The developer is correct on all points. My original risk assessment claim that "IsReferenceType on interfaces is false" was factually wrong. In Roslyn, interfaces ARE reference types (`IDisposable.IsReferenceType == true`), which means `tp.ConstraintTypes.Any(ct => ct.IsReferenceType)` would incorrectly return `true` for interface-only constraints like `where T : IDisposable`.

The developer's suggested fix -- using `ITypeParameterSymbol.IsReferenceType` directly on the type parameter itself -- is the correct approach. I confirmed the Roslyn semantics:

- For an **unconstrained** type parameter: `tp.IsReferenceType == false`, `tp.IsValueType == false`
- For `where T : class`: `tp.IsReferenceType == true`
- For `where T : Attribute` (class constraint): `tp.IsReferenceType == true`
- For `where T : IDisposable` (interface constraint): `tp.IsReferenceType == false`
- For `where T : class, IDisposable`: `tp.IsReferenceType == true` (from the explicit `class` constraint)

Sources: [Roslyn ITypeSymbol.IsReferenceType](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.itypesymbol.isreferencetype?view=roslyn-dotnet-4.9.0), [Roslyn source](https://github.com/dotnet/roslyn/blob/main/src/Compilers/Core/Portable/Symbols/ITypeSymbol.cs)

**Changes made to the plan:**
1. Renamed field from `ImpliesReferenceType` to `IsKnownReferenceType` for clarity
2. Changed computation from `tp.HasReferenceTypeConstraint || tp.ConstraintTypes.Any(ct => ct.IsReferenceType)` to simply `tp.IsReferenceType`
3. Updated the Approach section with a truth table showing all constraint cases
4. Corrected the risk assessment table to remove the incorrect interface claim
5. Updated the Roslyn semantics section throughout

### Concern #2: ACCEPTED -- Interface constraint test case added

Added `T? InterfaceConstrainedReturn<T>() where T : IDisposable` to the `INullableGenericService` interface in Design.Domain. Confirmed that it currently produces CS8665 (proving the original approach would have missed this case). Both standalone and inline stubs exercise this method.

The Design.Stubs now cover 4 test cases across 2 patterns:
- **Must NOT get `where T : class`**: `NullableValues<TData>`, `NullableReturn<T>`, `InterfaceConstrainedReturn<T>`
- **MUST get `where T : class`**: `ConstrainedNullableReturn<T>`

---

## Implementation Contract

**Created:** 2026-02-08
**Approved by:** knockoff-developer

### Design.Stubs Acceptance Criteria

These are the failing Design.Stubs that must compile after implementation:

- [ ] `src/Design/Design.Stubs/Methods/NullableGenericMethods.cs:23` -- Pattern 1 (Standalone): `NullableGenericServiceStub : INullableGenericService` -- CS8665 on `NullableValues<TData>`, `NullableReturn<T>`, `InterfaceConstrainedReturn<T>`
- [ ] `src/Design/Design.Stubs/Methods/NullableGenericMethods.cs:31` -- Pattern 5 (Inline): `[KnockOff<INullableGenericService>] NullableGenericInlineTests` -- CS8665 on same 3 methods
- [ ] `ConstrainedNullableReturn<T>() where T : Attribute` -- Must continue to compile (regression guard)

### In Scope

**Phase 1: Model Change**
- [ ] Add `IsKnownReferenceType` parameter (default `false`) to `TypeParameterInfo` record in `src/Generator/Models/InterfaceModels.cs`
- [ ] Update extraction site 1: `InterfaceMemberInfo.FromMethod()` in `src/Generator/Models/InterfaceModels.cs` -- pass `tp.IsReferenceType`
- [ ] Update extraction site 2: `ClassMemberInfo.FromMethod()` in `src/Generator/Models/ClassModels.cs` -- pass `tp.IsReferenceType`
- [ ] Update extraction site 3: `SymbolHelpers.ExtractTypeParameters()` in `src/Generator/Models/SymbolHelpers.cs` -- pass `tp.IsReferenceType`

**Phase 2: Fix Builders**
- [ ] Fix `GetConstraintsForExplicitImpl` in `src/Generator/Builder/FlatModelBuilder.cs` -- guard third check with `tp.IsKnownReferenceType`
- [ ] Fix `GetConstraintsForExplicitImpl` in `src/Generator/Builder/InlineModelBuilder.cs` -- same change

**Phase 3: Verify**
- [ ] **Checkpoint:** `dotnet build src/Design/Design.Stubs` succeeds with zero errors
- [ ] **Checkpoint:** `dotnet test src/KnockOff.sln` -- all existing tests pass
- [ ] **Checkpoint:** `dotnet test src/Design/Design.Tests` -- all Design tests pass

**Phase 4: Add Test Cases**
- [ ] Add test interface with unconstrained nullable generic methods to `src/Tests/KnockOffTests/TestInterfaces.cs` (or inline in `GenericMethodBugTests.cs`)
- [ ] Add standalone and inline stubs for the test interface
- [ ] Add tests in `src/Tests/KnockOffTests/GenericMethodBugTests.cs`:
  - Unconstrained nullable return works (no CS8665)
  - Unconstrained nullable parameter works
  - Interface-only constraint does NOT get `where T : class`
  - Class constraint (Attribute) still gets `where T : class` (regression)
- [ ] **Checkpoint:** Full `dotnet test src/KnockOff.sln` passes

### Explicitly Out of Scope

- Class stub pipelines (patterns 3, 4, 6, 9) -- confirmed not affected; use `override` not explicit impl
- Inline delegate (pattern 7) -- delegates don't have generic methods
- Changes to `GetConstraintClauses` method -- this method is for non-explicit-impl contexts and is correct as-is
- Open generic pattern 8 test in Design.Stubs -- while pattern 8 shares `InlineModelBuilder` code, adding an open generic interface just for this bug is over-engineering; the inline pattern 5 test covers the same code path

### Verification Gates

1. **After Phase 2:** `dotnet build src/Design/Design.Stubs` must succeed with zero errors. If it does not, STOP -- the fix is incorrect.
2. **After Phase 3:** Full `dotnet test src/KnockOff.sln` and `dotnet test src/Design/Design.Tests` must pass. If any test fails, categorize as in-scope or out-of-scope.
3. **Final (after Phase 4):** Full `dotnet test src/KnockOff.sln` must pass with all new tests included.

### Stop Conditions

If any of these occur, STOP and report:
- Out-of-scope test fails after the model or builder change
- `ConstrainedNullableReturn<T>() where T : Attribute` stops compiling (regression)
- Any existing `GenericMethodBugTests` test fails
- `tp.IsReferenceType` does not return expected values at any extraction site (architectural contradiction)

---

## Implementation Progress

**Started:** 2026-02-08
**Developer:** knockoff-developer
**Status:** Complete -- Awaiting Verification

### Phase 1: Model Change -- COMPLETE

- [x] Added `IsKnownReferenceType` parameter (default `false`) to `TypeParameterInfo` record in `src/Generator/Models/InterfaceModels.cs`
- [x] Updated extraction site 1: `InterfaceMemberInfo.FromMethod()` -- passes `tp.IsReferenceType`
- [x] Updated extraction site 2: `ClassMemberInfo.FromMethod()` -- passes `tp.IsReferenceType`
- [x] Updated extraction site 3: `SymbolHelpers.ExtractTypeParameters()` -- passes `tp.IsReferenceType`

### Phase 2: Fix Builders -- COMPLETE

- [x] Fixed `GetConstraintsForExplicitImpl` in `FlatModelBuilder.cs` -- guarded third check with `tp.IsKnownReferenceType`
- [x] Fixed `GetConstraintsForExplicitImpl` in `InlineModelBuilder.cs` -- same change

### Phase 2.5: Additional Fix (Discovered During Implementation) -- COMPLETE

The original plan's Phase 2 fix was necessary but insufficient. After removing the spurious `where T : class` constraint, the generated code still used `T?` in explicit implementation signatures. Without a `class` constraint, the C# compiler interprets `T?` as `Nullable<T>` (CS0453) rather than a nullable annotation. This required three additional changes per affected builder:

1. **Type stripping:** Added `StripUnconstrainedNullableAnnotations()` helper that replaces `T?` with `T` for unconstrained type parameters in the explicit implementation's return type and parameter declarations.
2. **Nullable pragma:** Added `NeedsNullableDisable` field to `FlatMethodModel` and `InlineInterfaceImplementation` models. When true, the renderers emit `#nullable disable` before and `#nullable restore` after the explicit implementation method. This prevents CS8769 (nullability mismatch between `T` in impl and `T?` in interface).
3. **Detection:** Added `HasUnconstrainedNullableTypeParams()` helper that checks whether any unconstrained type parameter appears with `?` in the return type or parameters.

Files additionally modified:
- `src/Generator/Model/Flat/FlatMethodModel.cs` -- added `NeedsNullableDisable` field
- `src/Generator/Model/Inline/InlineInterfaceImplementation.cs` -- added `NeedsNullableDisable` field
- `src/Generator/Renderer/FlatRenderer.cs` -- emits `#nullable disable/restore` around affected explicit implementations
- `src/Generator/Renderer/InlineRenderer.cs` -- same

### Incidental Fixes (Discovered During Implementation)

Two pre-existing bugs were exposed by the new Design.Stubs test interface and fixed as part of this work:

1. **`typeof(T)` hardcoded in generic method handler (CS0246):** In `FlatModelBuilder.BuildGenericMethodHandler()`, the `KeyConstruction` for single-type-parameter methods was hardcoded as `"typeof(T)"` instead of using the actual type parameter name. This caused CS0246 when the type parameter was named `TData`. Fixed: changed to `$"typeof({typeParams[0].Name})"`.

2. **CA1052 on empty stub classes:** The `NullableGenericInlineTests` partial class had no user-defined members and was flagged by CA1052 (static holder type). Fixed by adding CA1052 to `Design.Stubs.csproj` NoWarn (consistent with other KnockOff stub classes that are empty partial classes by design).

### Phase 3: Verify -- COMPLETE

- [x] `dotnet build src/Design/Design.Stubs` succeeds with zero errors across net8.0, net9.0, net10.0
- [x] `dotnet test src/KnockOff.sln` -- all tests pass (7,196 total across all frameworks)
- [x] Generated code verified correct:
  - `NullableValues<TData>`: `#nullable disable`, `TData` (stripped), no constraint, `#nullable restore`
  - `NullableReturn<T>`: `#nullable disable`, `T` (stripped), no constraint, `#nullable restore`
  - `InterfaceConstrainedReturn<T>`: `#nullable disable`, `T` (stripped), no constraint, `#nullable restore`
  - `ConstrainedNullableReturn<T>`: `T?` (preserved), `where T : class` (correct regression guard)

### Phase 4: Add Test Cases -- COMPLETE

- [x] Added `INullableGenericServiceForTests` interface to `GenericMethodBugTests.cs` with 4 methods
- [x] Added `NullableGenericServiceKnockOff` stub class (Pattern 1 standalone)
- [x] Added 8 test methods in `GenericMethodBugTests.cs` (Bug 4 region):
  - `UnconstrainedNullable_ReturnType_CompilesAndWorks` -- verifies configured return works
  - `UnconstrainedNullable_ReturnType_CanReturnNull` -- verifies null return works
  - `UnconstrainedNullable_WithParameter_CompilesAndWorks` -- verifies parameter passing works
  - `UnconstrainedNullable_WithValueType_Works` -- verifies value types work (int)
  - `InterfaceConstrainedNullable_NoSpuriousClassConstraint` -- verifies IDisposable constraint
  - `ClassConstrainedNullable_RegressionStillWorks` -- regression guard for Attribute constraint
  - `ClassConstrainedNullable_CanReturnNull` -- verifies nullable return for constrained case
  - `UnconstrainedNullable_Verification_Works` -- verifies call tracking works
- [x] Full `dotnet test src/KnockOff.sln` passes with all new tests included

### Test Results

```
KnockOffTests.AssemblyStrict: 14 passed x 3 frameworks = 42
KnockOff.Documentation.Samples: 599 passed x 3 frameworks = 1,797
KnockOff.NeatooInterfaceTests: 473 passed x 3 frameworks = 1,419
KnockOffTests: 1313/1312/1313 (net9/net8/net10) = 3,938

Total: ~7,196 tests, all passing, zero failures
```

### Design.Stubs Build Results

```
Design.Stubs -> net8.0\Design.Stubs.dll
Design.Stubs -> net9.0\Design.Stubs.dll
Design.Stubs -> net10.0\Design.Stubs.dll
Build succeeded. 0 Warning(s), 0 Error(s)
```

---

## Completion Evidence

### All Files Modified

| File | Change |
|------|--------|
| `src/Generator/Models/InterfaceModels.cs` | Added `IsKnownReferenceType` to `TypeParameterInfo`; updated extraction site 1 |
| `src/Generator/Models/ClassModels.cs` | Updated extraction site 2 |
| `src/Generator/Models/SymbolHelpers.cs` | Updated extraction site 3 |
| `src/Generator/Builder/FlatModelBuilder.cs` | Guarded constraint check; added stripping + detection helpers; fixed `typeof(T)` bug |
| `src/Generator/Builder/InlineModelBuilder.cs` | Guarded constraint check; added stripping + detection helpers |
| `src/Generator/Model/Flat/FlatMethodModel.cs` | Added `NeedsNullableDisable` field |
| `src/Generator/Model/Inline/InlineInterfaceImplementation.cs` | Added `NeedsNullableDisable` field |
| `src/Generator/Renderer/FlatRenderer.cs` | Emits `#nullable disable/restore` for affected methods |
| `src/Generator/Renderer/InlineRenderer.cs` | Emits `#nullable disable/restore` for affected methods |
| `src/Tests/KnockOffTests/GenericMethodBugTests.cs` | Added interface, stub, and 8 test methods |
| `src/Design/Design.Stubs/Design.Stubs.csproj` | Added CA1052 to NoWarn |

### Generated Code Sample (Pattern 1 -- Standalone)

The fix produces correct generated code for all 4 test cases:

```csharp
// Unconstrained -- #nullable disable, no constraint, T stripped
#nullable disable
TData global::Design.Domain.Services.INullableGenericService.NullableValues<TData>(TData data)
{
    NullableValues.Of<TData>().RecordCall();
    if (NullableValues.Of<TData>().Callback is { } callback)
        return callback(data);
    if (Strict) throw global::KnockOff.StubException.NotConfigured("INullableGenericService", "NullableValues");
    return default!;
}
#nullable restore

// Interface constraint (IDisposable) -- also #nullable disable, no where T : class
#nullable disable
T global::Design.Domain.Services.INullableGenericService.InterfaceConstrainedReturn<T>()
{
    InterfaceConstrainedReturn.Of<T>().RecordCall();
    ...
}
#nullable restore

// Class constraint (Attribute) -- T? preserved, where T : class emitted (regression guard)
T? global::Design.Domain.Services.INullableGenericService.ConstrainedNullableReturn<T>() where T : class
{
    ConstrainedNullableReturn.Of<T>().RecordCall();
    ...
}
```

---

## Architect Verification

**Verified:** 2026-02-08
**Verdict:** VERIFIED

### Independent Test Results

All builds and tests executed independently by the architect. Zero failures.

| Project | Framework | Result |
|---------|-----------|--------|
| KnockOffTests | net8.0 | 1312 passed, 0 failed |
| KnockOffTests | net9.0 | 1313 passed, 0 failed |
| KnockOffTests | net10.0 | 1313 passed, 0 failed |
| KnockOff.Documentation.Samples | net8.0 | 599 passed, 0 failed |
| KnockOff.Documentation.Samples | net9.0 | 599 passed, 0 failed |
| KnockOff.Documentation.Samples | net10.0 | 599 passed, 0 failed |
| KnockOff.NeatooInterfaceTests | net8.0 | 473 passed, 0 failed |
| KnockOff.NeatooInterfaceTests | net9.0 | 473 passed, 0 failed |
| KnockOff.NeatooInterfaceTests | net10.0 | 473 passed, 0 failed |
| KnockOffTests.AssemblyStrict | net8.0 | 14 passed, 0 failed |
| KnockOffTests.AssemblyStrict | net9.0 | 14 passed, 0 failed |
| KnockOffTests.AssemblyStrict | net10.0 | 14 passed, 0 failed |
| Design.Tests | net8.0 | 356 passed, 0 failed |
| Design.Tests | net9.0 | 356 passed, 0 failed |
| Design.Tests | net10.0 | 356 passed, 0 failed |
| **Design.Stubs** | **all 3** | **Build succeeded, 0 errors** |

**Total: ~8,264 tests, zero failures.**

### Design Match

The implementation matches the plan's design with one well-justified extension:

- **Phase 1 (Model Change):** Matches plan. `IsKnownReferenceType` added to `TypeParameterInfo` with default `false`, populated from `tp.IsReferenceType` at all 3 extraction sites.
- **Phase 2 (Builder Fix):** Matches plan. Third check in `GetConstraintsForExplicitImpl` guarded with `tp.IsKnownReferenceType` in both `FlatModelBuilder` and `InlineModelBuilder`.
- **Phase 2.5 (Additional Fix):** Not in original plan but architecturally sound (see assessment below). Strips `T?` to `T` for unconstrained type parameters and wraps explicit implementations with `#nullable disable` / `#nullable restore`.
- **Phase 4 (Tests):** Exceeds plan. 8 tests added covering all 4 constraint cases plus value type, null return, and verification scenarios.

### Generated Code Spot-Check

**Pattern 1 (Standalone) -- `NullableGenericServiceStub.g.cs`:**
- `NullableValues<TData>`: `#nullable disable`, `TData` (stripped), no constraint, `#nullable restore` -- CORRECT
- `NullableReturn<T>`: `#nullable disable`, `T` (stripped), no constraint, `#nullable restore` -- CORRECT
- `InterfaceConstrainedReturn<T>`: `#nullable disable`, `T` (stripped), no constraint, `#nullable restore` -- CORRECT
- `ConstrainedNullableReturn<T>`: `T?` preserved, `where T : class` emitted, no pragma -- CORRECT

**Pattern 5 (Inline Interface) -- `NullableGenericInlineTests.Stubs.g.cs`:**
- Same 4 methods verified with identical patterns to Pattern 1 -- CORRECT
- Inline interceptors correctly preserve `T?` in delegate signatures (not stripped) -- CORRECT

### Assessment of Additional Fix (Phase 2.5)

The `#nullable disable` / `#nullable restore` pragma approach is **architecturally sound** for the following reasons:

1. **Correctness:** In C# 9+, `T?` on an unconstrained type parameter is a nullable annotation hint that means "default value". In an explicit interface implementation, without `where T : class`, the compiler interprets `T?` as `Nullable<T>` (CS0453). Stripping `T?` to `T` and disabling the nullable context is the standard C# workaround for this language limitation.

2. **Scoping:** The pragmas are tightly scoped to individual method bodies only. They do NOT disable nullable analysis for the entire file or class. This minimizes the nullable safety surface area reduction.

3. **Semantics preserved:** The stripped `T` in the explicit implementation still correctly matches the interface's `T?` because in `#nullable disable` context, `T` is equivalent to the interface's unconstrained `T?`. The runtime behavior is identical.

4. **No leakage:** The `#nullable restore` immediately follows the closing brace, restoring full nullable analysis for subsequent methods. I verified this in both generated files.

5. **Consistent across pipelines:** Both `FlatRenderer` and `InlineRenderer` emit the pragmas identically, including in the delegation rendering path in `FlatRenderer`.

### Assessment of Incidental Fix (typeof(T) bug)

The `typeof(T)` hardcoding fix in `FlatModelBuilder.BuildGenericMethodHandler()` is correct. Line 1131 now reads `$"typeof({typeParams[0].Name})"` instead of the hardcoded `"typeof(T)"`. This was a pre-existing bug exposed by the new `TData` type parameter name in the test interface.

### Test Coverage Assessment

The 8 new tests in `GenericMethodBugTests.cs` provide thorough coverage:
- Unconstrained nullable return (string and int) with configured values
- Unconstrained nullable return with null
- Unconstrained nullable with parameter passing
- Interface-only constraint (`IDisposable`) without spurious `where T : class`
- Class-implying constraint (`Attribute`) regression guard (value and null)
- Call tracking and verification with unconstrained nullable
