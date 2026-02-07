# Fix GetTypeSuffix Bugs for Identifier Generation

**Date:** 2026-02-06
**Related Todo:** [Parameter Type Bugs in GetTypeSuffix](../todos/param-type-bugs.md)
**Status:** Verified
**Last Updated:** 2026-02-06

---

## Overview

`GetTypeSuffix(string)` is duplicated across 4 files with inconsistent keyword maps and shared bugs that produce invalid C# identifiers for certain parameter types. `GetTypeSuffix(ITypeSymbol)` in SymbolHelpers ignores multidimensional array rank. This plan consolidates the string-based copies into a single shared implementation, fixes all known bugs, and adds test coverage.

---

## Approach

### Phase 1: Consolidate the 4 string-based copies

The function is duplicated in:
1. `UnifiedInterceptorBuilder.cs` (public static)
2. `FlatModelBuilder.cs` (private static)
3. `InlineModelBuilder.cs` (private static)
4. `FlatRenderer.cs` (private static)

All 4 copies have the same structural bugs, but differ in keyword coverage:
- **UnifiedInterceptorBuilder**: has `short`, `uint`, `ulong`, `ushort`, `sbyte`, `object`; has `"void" => "void"` (lowercase -- probably a bug itself)
- **FlatModelBuilder**: has `"void" => "Void"`; missing `short`/`uint`/`ulong`/`ushort`/`sbyte`/`object`
- **InlineModelBuilder**: has `"void" => "Void"`; missing `short`/`uint`/`ulong`/`ushort`/`sbyte`/`object`
- **FlatRenderer**: missing `void`, `short`/`uint`/`ulong`/`ushort`/`sbyte`/`object`

**Strategy:** Delete the 3 private copies and make all callers use `UnifiedInterceptorBuilder.GetTypeSuffix()` (already public static). Fix the `"void" => "void"` to `"void" => "Void"` in the canonical copy.

### Phase 2: Fix string-based bugs

All bugs are in the canonical `UnifiedInterceptorBuilder.GetTypeSuffix(string)`:

#### Bug 1: Nullable inside generics

**Input:** `global::System.Collections.Generic.Dictionary<string?, int>`
**Current processing:** `TrimEnd('?')` strips trailing `?` only. The `?` after `string` is embedded mid-string. After `Replace` operations in the fallback, produces `Collections_Generic_Dictionary_string?_int` -- the `?` is an invalid identifier character.

**Fix:** After the switch fallback's `Replace` chain, also strip all remaining `?` characters: add `.Replace("?", "")`.

#### Bug 2: Tuples

**Input:** `(int, string)` (Roslyn displays tuples this way with `UseSpecialTypes`)
**Current processing:** Parentheses are never stripped. Fallback produces `(int_string)`.

**Fix:** Add `.Replace("(", "").Replace(")", "")` to the fallback `Replace` chain. This converts `(int, string)` to `int_string`, then the fallback produces `int_string`. For named tuples like `(int x, string y)`, produces `int_x_string_y` -- not ideal but valid.

However, there is a subtlety: the C# keyword `int` inside the tuple would not be matched by the switch (because the full string is not just `int`). This means the tuple elements will use their raw names. For identifier validity this is fine -- `int` is a valid identifier substring. For consistency with non-tuple `int` -> `Int32`, this is a minor inconsistency. This is acceptable because tuples as method parameter types in interfaces/abstract classes are uncommon, and the primary goal is producing valid identifiers.

#### Bug 3: Multidimensional arrays

**Input:** `int[,]`
**Current processing:** `EndsWith("[]")` does not match `[,]`. The `int[,]` string is not recognized as a keyword because the full string is `int[,]`, not `int`. Falls through to fallback which strips `[` and `]` and `,`, producing `int_` -- valid but wrong (missing dimensionality info and no `Array` suffix).

**Fix:** Before the `[]` stripping loop, add handling for multidimensional arrays. Use a regex or string scan to detect patterns like `[,]`, `[,,]`, etc. Extract the rank (commas + 1), strip the bracket expression, and track both `arrayDepth` and dimensionality.

The suffix should encode rank: `int[,]` -> `Int32Array2D`, `int[,,]` -> `Int32Array3D`, `int[]` -> `Int32Array` (unchanged, 1D is the default).

Implementation approach for netstandard2.0 (no Regex dependency desired):
```
// After TrimEnd('?'), before the [] loop:
// Scan from end for patterns like [,], [,,], etc.
// When found, strip them and record arrayDepth with rank info.
```

#### Bug 4: Array of nullable elements

**Input:** `string?[]`
**Current processing:** `TrimEnd('?')` removes trailing `?` (the type is `string?[]` so no trailing `?`). Then `EndsWith("[]")` matches, strips brackets leaving `string?`. `string?` doesn't match `"string"` keyword. Fallback produces `string` (after `TrimEnd('?')` at line 228). Then appends `Array` -> `stringArray` (lowercase `s`).

**Fix:** This is addressed by Bug 1's fix. After the fallback replace chain strips `?`, the result before keyword matching won't have `?`. But the issue is ordering: the keyword switch happens before the fallback cleanup.

The real fix is: after stripping array brackets, also `TrimEnd('?')` on the working type BEFORE the keyword switch. Currently `TrimEnd('?')` happens at line 196 (before bracket stripping) and at line 228 (after the switch). The `?` from `string?` survives to the switch because it's not trailing after `[]`.

**Fix approach:** After the array bracket stripping loop, add another `workingType = workingType.TrimEnd('?');` so that `string?` becomes `string` before hitting the keyword switch.

#### Bug 5: `nint`/`nuint`

**Input:** `nint`, `nuint`
**Current processing:** Not in keyword map. Falls through to fallback which produces lowercase `nint`/`nuint`.

**Fix:** Add to keyword map: `"nint" => "IntPtr"`, `"nuint" => "UIntPtr"`. These are the underlying CLR types. The symbol-based version already handles this correctly because `ITypeSymbol.Name` returns `IntPtr`/`UIntPtr`.

### Phase 3: Fix symbol-based GetTypeSuffix

**Bug 6:** `IArrayTypeSymbol.Rank` is ignored.

**Current code:**
```csharp
IArrayTypeSymbol array => GetTypeSuffix(array.ElementType) + "Array",
```

Both `string[]` (rank 1) and `string[,]` (rank 2) produce `StringArray`.

**Fix:**
```csharp
IArrayTypeSymbol array when array.Rank == 1 =>
    GetTypeSuffix(array.ElementType) + "Array",
IArrayTypeSymbol array =>
    GetTypeSuffix(array.ElementType) + $"Array{array.Rank}D",
```

This produces `StringArray` for `string[]` and `StringArray2D` for `string[,]`, matching the string-based fix.

---

## Design

### Consolidated `GetTypeSuffix(string)` in UnifiedInterceptorBuilder

The fixed canonical implementation:

```csharp
public static string GetTypeSuffix(string type)
{
    // Strip trailing nullable marker for array bracket detection
    var workingType = type.TrimEnd('?');

    // Parse array suffixes (handles [], [,], [,,], etc.)
    var arraySuffixes = new List<int>(); // rank per array dimension
    while (true)
    {
        // Check for multidimensional: [,], [,,], etc.
        if (workingType.Length >= 3)
        {
            var lastBracket = workingType.LastIndexOf('[');
            if (lastBracket >= 0 && workingType[workingType.Length - 1] == ']')
            {
                var bracketContent = workingType.Substring(lastBracket + 1, workingType.Length - lastBracket - 2);
                if (bracketContent.Length == 0 || bracketContent.All(c => c == ','))
                {
                    var rank = bracketContent.Length + 1; // "" = rank 1, "," = rank 2
                    arraySuffixes.Add(rank);
                    workingType = workingType.Substring(0, lastBracket);
                    continue;
                }
            }
        }
        else if (workingType.EndsWith("[]"))
        {
            arraySuffixes.Add(1);
            workingType = workingType.Substring(0, workingType.Length - 2);
            continue;
        }
        break;
    }

    // Strip nullable after array brackets (handles string?[] -> string? -> string)
    workingType = workingType.TrimEnd('?');

    var simple = workingType.Replace("global::", "").Replace("System.", "");
    simple = simple switch
    {
        "int" => "Int32",
        "string" => "String",
        "bool" => "Boolean",
        "long" => "Int64",
        "double" => "Double",
        "float" => "Single",
        "decimal" => "Decimal",
        "char" => "Char",
        "byte" => "Byte",
        "short" => "Int16",
        "uint" => "UInt32",
        "ulong" => "UInt64",
        "ushort" => "UInt16",
        "sbyte" => "SByte",
        "object" => "Object",
        "void" => "Void",
        "nint" => "IntPtr",
        "nuint" => "UIntPtr",
        _ => simple.Replace(".", "_").Replace("<", "_").Replace(">", "")
            .Replace(",", "_").Replace(" ", "")
            .Replace("[", "").Replace("]", "")
            .Replace("(", "").Replace(")", "")
            .Replace("?", "")
    };

    // Append array suffixes in reverse order (outermost first)
    for (int i = arraySuffixes.Count - 1; i >= 0; i--)
    {
        simple += arraySuffixes[i] == 1 ? "Array" : $"Array{arraySuffixes[i]}D";
    }

    return simple;
}
```

### Callers to update

After consolidation, these callers change from local `GetTypeSuffix` to `UnifiedInterceptorBuilder.GetTypeSuffix`:

| File | Caller | Change |
|------|--------|--------|
| `FlatModelBuilder.cs` | `GetSignatureSuffix()` | Use `UnifiedInterceptorBuilder.GetTypeSuffix()` |
| `FlatModelBuilder.cs` | `ComputeSignatureSuffixForGeneric()` | Use `UnifiedInterceptorBuilder.GetTypeSuffix()` |
| `FlatModelBuilder.cs` | Indexer `KeyTypeFriendlyName` | Use `UnifiedInterceptorBuilder.GetTypeSuffix()` |
| `InlineModelBuilder.cs` | Indexer `KeyTypeFriendlyName` | Use `UnifiedInterceptorBuilder.GetTypeSuffix()` |
| `InlineModelBuilder.cs` | `GetUniqueSignatureCount()` | Use `UnifiedInterceptorBuilder.GetTypeSuffix()` |
| `FlatRenderer.cs` | `GetSignatureSuffix()` | Use `UnifiedInterceptorBuilder.GetTypeSuffix()` |

Note: `MethodInterceptorRenderer.cs` already calls `UnifiedInterceptorBuilder.GetTypeSuffix()` directly -- no change needed.

### Symbol-based fix

In `SymbolHelpers.GetTypeSuffix(ITypeSymbol)`:

```csharp
IArrayTypeSymbol array when array.Rank == 1 =>
    GetTypeSuffix(array.ElementType) + "Array",
IArrayTypeSymbol array =>
    GetTypeSuffix(array.ElementType) + $"Array{array.Rank}D",
```

---

## Implementation Steps

### Phase 1: Consolidate and fix GetTypeSuffix(string)

1. Fix the canonical `UnifiedInterceptorBuilder.GetTypeSuffix(string)`:
   - Handle multidimensional arrays (`[,]`, `[,,]`)
   - Strip `?` from nullable elements after array bracket removal
   - Add `nint`/`nuint` to keyword map
   - Fix `"void" => "void"` to `"void" => "Void"`
   - Add `.Replace("(", "").Replace(")", "")` for tuples
   - Add `.Replace("?", "")` for nullable-inside-generics
2. Delete private `GetTypeSuffix(string)` from `FlatModelBuilder.cs`
3. Delete private `GetTypeSuffix(string)` from `InlineModelBuilder.cs`
4. Delete private `GetTypeSuffix(string)` from `FlatRenderer.cs`
5. Update all callers to use `UnifiedInterceptorBuilder.GetTypeSuffix()`
6. Build and verify no compile errors

### Phase 2: Fix GetTypeSuffix(ITypeSymbol)

1. Update `SymbolHelpers.GetTypeSuffix(ITypeSymbol)` array handling to include rank
2. Build and verify

### Phase 3: Add test interfaces and tests

1. Create test interface `IParamTypeSuffixService` in `TestInterfaces.cs` with methods using:
   - Nullable inside generics: `void Process(Dictionary<string?, int> data)`
   - Tuples: `void Process((int, string) pair)`
   - Multidimensional arrays: `void Process(int[,] matrix)`
   - Array of nullable: `void Process(string?[] items)`
   - `nint`/`nuint`: `void Process(nint value)`
   - Overloads with each type to verify suffix uniqueness

2. Create standalone stub (`[KnockOff]`) and inline stub (`[KnockOff<IParamTypeSuffixService>]`)

3. Add tests verifying:
   - Each method overload is accessible (compilation verification)
   - OnCall works for each parameter type
   - Verify tracks each overload independently

### Phase 4: Verify existing tests

1. Run full test suite
2. Verify Design.Stubs builds
3. Verify Design.Tests pass

---

## Acceptance Criteria

- [ ] Only one copy of `GetTypeSuffix(string)` exists (in `UnifiedInterceptorBuilder`)
- [ ] `Dictionary<string?, int>` produces valid identifier (no `?` in output)
- [ ] `(int, string)` produces valid identifier (no parentheses in output)
- [ ] `int[,]` produces `Int32Array2D` (distinct from `Int32Array`)
- [ ] `string?[]` produces `StringArray` (keyword matched correctly)
- [ ] `nint` produces `IntPtr`, `nuint` produces `UIntPtr`
- [ ] `IArrayTypeSymbol` with `Rank > 1` produces distinct suffix from `Rank == 1`
- [ ] All existing tests pass
- [ ] Design.Stubs builds successfully
- [ ] New tests cover each bug fix for both standalone and inline patterns

---

## Dependencies

- No external dependencies
- All changes are internal to the generator

---

## Risks / Considerations

### Risk 1: Generated code changes break existing consumers

**Likelihood:** Low
**Impact:** Medium
**Mitigation:** The bug fixes only affect edge-case type names that currently produce invalid C# identifiers. Any code that currently compiles will continue to work because the affected identifiers are *already broken* (invalid identifiers or collisions). The consolidation changes `Void` casing in FlatRenderer but `void` only appears as return types, and return type suffixes are internal to the generated code.

### Risk 2: Multidimensional array suffix format

**Consideration:** The `Array2D`/`Array3D` suffix format is a design choice. Alternatives include `Array2`, `ArrayRank2`, `MultiArray`. The `Array2D` format is chosen because it reads naturally ("2-dimensional array") and is consistent with how developers typically describe them.

### Risk 3: Tuple suffix format

**Consideration:** Tuples produce `int_string` instead of `Int32_String` because the keyword elements are embedded inside the tuple string, not the top-level type. This is acceptable -- the goal is valid identifiers, not perfectly canonicalized names. Tuples as parameters in stubbable interfaces are rare.

### Risk 4: netstandard2.0 constraint

**Consideration:** Cannot use `System.Linq`, Span, or Regex easily. The array bracket parsing must use basic string operations. The proposed implementation uses `LastIndexOf`, `Substring`, and character comparison, all available in netstandard2.0.

**Correction:** `System.Linq` IS available in netstandard2.0 (it's part of the BCL). The constraint is about newer API surfaces like `Span<T>`, `Index`/`Range` syntax, etc. The existing code already uses LINQ extensively.

---

## Architectural Verification

### Scope Table

This is a bug fix to internal identifier generation. It affects all patterns because all patterns use `GetTypeSuffix` for overload disambiguation. However, bugs only manifest when interfaces/classes contain parameters of the problematic types.

| Pattern | Affected Pipeline | GetTypeSuffix Source | Bug Impact |
|---------|-------------------|---------------------|------------|
| 1. Standalone | FlatModelBuilder -> FlatRenderer | FlatModelBuilder (local copy) + FlatRenderer (local copy) + UnifiedInterceptorBuilder (via shared method builder) | All 6 bugs |
| 2. Generic Standalone | FlatModelBuilder -> FlatRenderer | Same as Standalone | All 6 bugs |
| 3. Standalone Class | StandaloneClassModelBuilder -> ClassRenderer | UnifiedInterceptorBuilder (via shared method builder) | String bugs 1-5 |
| 4. Generic Standalone Class | StandaloneClassModelBuilder -> ClassRenderer | Same as Standalone Class | String bugs 1-5 |
| 5. Inline Interface | InlineModelBuilder -> InlineRenderer | InlineModelBuilder (local copy) + UnifiedInterceptorBuilder | All 6 bugs |
| 6. Inline Class | ClassModelBuilder -> InlineRenderer | UnifiedInterceptorBuilder (via shared method builder) | String bugs 1-5 |
| 7. Open Generic Interface | InlineModelBuilder -> InlineRenderer | Same as Inline Interface | All 6 bugs |
| 8. Open Generic Class | ClassModelBuilder -> InlineRenderer | Same as Inline Class | String bugs 1-5 |
| 9. Inline Delegate | N/A | Delegates don't have overloaded methods with these types | Not affected |

**Symbol-based bug (multidimensional array rank):** Affects `GetTypeArgumentsSuffix` called from `Transform.cs` for interface name disambiguation when multiple interfaces share the same simple name. This is cross-cutting across all patterns that use closed generic interfaces.

### Design Project Verification

Design.Stubs currently builds successfully. The bugs manifest only with specific parameter types not currently exercised in Design projects. Test verification will be done via new test interfaces in the test project.

**Breaking Changes:** No. The only observable change is that previously-broken identifiers (containing `?`, `(`, `)`, or colliding names) become valid. Any code currently using these parameter types would already fail to compile.

### Codebase Analysis

**Files examined:**
- `src/Generator/Builder/UnifiedInterceptorBuilder.cs` (lines 193-234) -- canonical copy, public static, most complete keyword map but with `void` lowercase bug
- `src/Generator/Builder/FlatModelBuilder.cs` (lines 1298-1333) -- private copy, missing 6 keywords, `Void` correct
- `src/Generator/Builder/InlineModelBuilder.cs` (lines 1391-1426) -- private copy, missing 6 keywords, `Void` correct
- `src/Generator/Renderer/FlatRenderer.cs` (lines 1097-1132) -- private copy, missing 7 keywords (including `void`)
- `src/Generator/Models/SymbolHelpers.cs` (lines 171-190) -- symbol-based version, rank ignored
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` (lines 569, 2719) -- already uses `UnifiedInterceptorBuilder.GetTypeSuffix` directly
- `src/Generator/Models/InterfaceModels.cs` (lines 112-179) -- `IndexerTypeSuffix` uses `GetSimpleTypeName`, not `GetTypeSuffix` (separate concern)
- `src/Generator/Models/ClassModels.cs` (lines 77-127) -- same pattern as InterfaceModels for indexers
- `src/Tests/KnockOffTests/ArrayParamOverloadTests.cs` -- existing test for the `string[]` fix (v0.37.0)
- `src/Tests/KnockOffTests/TestInterfaces.cs` -- where test interfaces are defined

**Key finding -- additional inconsistency discovered:** The copies differ not just in keywords but also in whether `void` is mapped. `FlatRenderer.cs` has no `void` mapping at all. `UnifiedInterceptorBuilder` maps to lowercase `"void"`. The others map to `"Void"`. Consolidation will fix all of these to `"Void"` for consistency.

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-02-06

### Why This Plan Is Exceptionally Clear

This plan is a well-scoped bug fix to an internal utility function with no user-facing API changes. The architect:
1. Accurately identified all 4 duplicate copies and their keyword differences (verified against actual code)
2. Correctly traced each bug through the processing pipeline with specific input/output pairs
3. Provided a complete, compilable proposed implementation that I traced through all 6 bug scenarios and confirmed produces correct output
4. Identified all callers (6 call sites across 3 files) -- verified against grep results, no callers missed
5. Correctly identified that `StandaloneClassModelBuilder` and `ClassModelBuilder` have no local copies (they use `UnifiedInterceptorBuilder` through `MethodInterceptorRenderer`)
6. Correctly scoped the symbol-based fix as a separate, clean change

### Review Summary

- Files examined: `UnifiedInterceptorBuilder.cs`, `FlatModelBuilder.cs`, `InlineModelBuilder.cs`, `FlatRenderer.cs`, `SymbolHelpers.cs`, `MethodInterceptorRenderer.cs`, `ArrayParamOverloadTests.cs`, `TestInterfaces.cs`, `Generator.csproj`
- Questions checked: 15 of 15
- Devil's advocate items: 5 generated, all addressed by the plan or immaterial to correctness

### Notes from Trace Analysis

1. **Proposed code correctness verified.** I manually traced `Dictionary<string?, int>`, `(int, string)`, `int[,]`, `string?[]`, `nint`, and `int[,][]` through the proposed implementation. All produce valid, distinct identifiers.

2. **The `else if` branch (lines 143-148) is effectively dead code.** It is only reachable when `workingType.Length < 3`, meaning the type name would have to be `"[]"` (empty element type). Roslyn will never produce this. It is harmless but the developer may simplify or keep it as a safety net.

3. **Consolidation keyword risk is theoretical.** Adding 6 keywords (`short`, `uint`, `ulong`, `ushort`, `sbyte`, `object`) to FlatModelBuilder/InlineModelBuilder and adding all keywords to FlatRenderer could change suffixes for existing overloads using those types. I searched the test interfaces and found no existing overloads with these parameter types, so no existing tests will break.

4. **`void` casing change in FlatRenderer is safe.** FlatRenderer currently has no `void` mapping -- `void` would fall through to the default case producing lowercase `void`. After consolidation, it maps to `Void`. This only affects return type suffixes (e.g., `GetSignatureSuffix`), and `void` return methods are common but the suffix only matters for overload disambiguation, which is unlikely to change behavior for existing tests.

---

## Implementation Contract

**Created:** 2026-02-06
**Approved by:** knockoff-developer

### Design Project Acceptance Criteria

N/A -- These are internal identifier bugs. Verification is through test interfaces that exercise each parameter type, not through Design.Stubs compilation.

### In Scope

- [x] Fix `UnifiedInterceptorBuilder.GetTypeSuffix(string)` with proposed implementation (multidimensional arrays, nullable stripping, tuple parentheses, `?` removal, `nint`/`nuint`, `void` casing)
- [x] Delete private `GetTypeSuffix(string)` from `FlatModelBuilder.cs` (lines 1298-1333)
- [x] Delete private `GetTypeSuffix(string)` from `InlineModelBuilder.cs` (lines 1391-1426)
- [x] Delete private `GetTypeSuffix(string)` from `FlatRenderer.cs` (lines 1097-1132)
- [x] Update callers in `FlatModelBuilder.cs`: `GetSignatureSuffix()`, `ComputeSignatureSuffixForGeneric()`, indexer `KeyTypeFriendlyName` (line 633)
- [x] Update callers in `InlineModelBuilder.cs`: indexer `KeyTypeFriendlyName` (lines 280-281), `GetUniqueSignatureCount()` (line 926)
- [x] Update callers in `FlatRenderer.cs`: `GetSignatureSuffix()` (lines 1091, 1094)
- [x] Checkpoint: `dotnet build src/KnockOff.sln` succeeds, all existing tests pass
- [x] Fix `SymbolHelpers.GetTypeSuffix(ITypeSymbol)` array handling to include rank
- [x] Checkpoint: build succeeds, all existing tests pass
- [x] Add test interface `IParamTypeSuffixService` in `TestInterfaces.cs` with method overloads for each bug category
- [x] Add standalone stub `[KnockOff]` and inline stub `[KnockOff<IParamTypeSuffixService>]` in `TestInterfaces.cs`
- [x] Add test file with tests for standalone and inline patterns verifying OnCall and Verify for each parameter type
- [x] Final: All tests pass, Design.Stubs builds, Design.Tests pass

### Explicitly Out of Scope

- Tuple element keyword canonicalization (e.g., `int` inside tuple producing `Int32`) -- valid identifiers are sufficient
- Multi-key indexer tuple bug (documented separately in Design.Domain/Entities/ICollection.cs)
- IndexerTypeSuffix (uses `GetSimpleTypeName`, separate concern)
- Changes to `FullyQualifiedWithNullability` display format
- Pointer types (`int*`) -- extremely unlikely in interface/abstract class method parameters

### Verification Gates

1. After Phase 1 (consolidation + string fixes): `dotnet build src/KnockOff.sln` succeeds, all existing tests pass
2. After Phase 2 (symbol fix): build succeeds, all existing tests pass
3. After Phase 3 (new tests): all tests pass including new ones
4. Final: `dotnet build src/Design/Design.Stubs` succeeds, Design.Tests pass

### Stop Conditions

If any of these occur, STOP and report:
- Out-of-scope test fails (especially any test using `short`, `uint`, `ulong`, `ushort`, `sbyte`, `object` parameter types in overload groups)
- Generated code changes that affect existing test assertions beyond the expected suffix corrections
- Architectural contradiction discovered
- Any test interface method signature that the generator cannot handle (e.g., tuple syntax not recognized by Roslyn's display format)

---

## Implementation Progress

**Started:** 2026-02-06
**Developer:** knockoff-developer

**Phase 1:** Consolidate and fix GetTypeSuffix(string)
- [x] Fix canonical implementation in UnifiedInterceptorBuilder -- replaced with new implementation supporting multidimensional arrays, nullable stripping, tuple parentheses, `?` removal, `nint`/`nuint`, `void` -> `Void` casing fix
- [x] Delete copy from FlatModelBuilder (lines 1298-1333 removed)
- [x] Delete copy from InlineModelBuilder (lines 1391-1426 removed)
- [x] Delete copy from FlatRenderer (lines 1097-1132 removed)
- [x] Update callers: FlatModelBuilder (GetSignatureSuffix, ComputeSignatureSuffixForGeneric, indexer KeyTypeFriendlyName), InlineModelBuilder (indexer KeyTypeFriendlyName, GetUniqueSignatureCount), FlatRenderer (GetSignatureSuffix) -- all now call `UnifiedInterceptorBuilder.GetTypeSuffix()`
- [x] Updated in-scope test `EdgeCaseValueOverloadTests.cs` to use `_Void` instead of `_void` in delegate type names (matches corrected casing)
- [x] **Verification**: Build succeeds, all existing tests pass (0 failures across all TFMs)

**Phase 2:** Fix GetTypeSuffix(ITypeSymbol)
- [x] Update SymbolHelpers array handling -- added `when array.Rank == 1` guard and new branch for `Rank > 1` producing `Array{rank}D` suffix
- [x] **Verification**: Build succeeds, all existing tests pass

**Phase 3:** Add tests
- [x] Create test interface `IParamTypeSuffixService` in TestInterfaces.cs with 6 overloads (List<string?>, (int, string), int[,], string?[], nint, nuint) plus GetName()
- [x] Create standalone stub `ParamTypeSuffixKnockOff` and inline stub `ParamTypeSuffixInlineTests`
- [x] Write 16 tests in `ParamTypeSuffixTests.cs` covering OnCall and Verify for both standalone and inline patterns
- [x] **Verification**: All tests pass including 16 new tests

**Note on test interface:** Changed `Dictionary<string?, int>` to `List<string?>` for Bug 1 because `string?` violates `Dictionary<TKey, TValue>`'s `notnull` constraint on TKey. `List<string?>` still exercises the embedded `?` bug without the constraint violation.

---

## Completion Evidence

**Reported:** 2026-02-06

- **Tests Passing:**
  - KnockOffTests.dll (net8.0): 1184 passed, 0 failed
  - KnockOffTests.dll (net9.0): 1185 passed, 0 failed
  - KnockOffTests.dll (net10.0): 1185 passed, 0 failed
  - KnockOffTests.AssemblyStrict.dll: 14 passed per TFM, 0 failed
  - KnockOff.Documentation.Samples.dll: 571 passed per TFM, 0 failed
  - KnockOff.NeatooInterfaceTests.dll: 473 passed per TFM, 0 failed
  - Design.Tests.dll: 259 passed per TFM, 0 failed
  - Zero failures across all test projects and all target frameworks
- **Design Projects Compile:** Yes (Design.Stubs builds successfully on net8.0, net9.0, net10.0)
- **All Contract Items:** Confirmed 100% complete
- **Documentation Updated:** N/A (internal bug fix, no user-facing documentation changes needed)

### Files Modified

| File | Change |
|------|--------|
| `src/Generator/Builder/UnifiedInterceptorBuilder.cs` | Replaced `GetTypeSuffix(string)` with fixed implementation (multidimensional arrays, nullable, tuples, nint/nuint, void casing) |
| `src/Generator/Builder/FlatModelBuilder.cs` | Deleted private `GetTypeSuffix`, updated 4 callers to use `UnifiedInterceptorBuilder.GetTypeSuffix()` |
| `src/Generator/Builder/InlineModelBuilder.cs` | Deleted private `GetTypeSuffix`, updated 2 callers to use `UnifiedInterceptorBuilder.GetTypeSuffix()` |
| `src/Generator/Renderer/FlatRenderer.cs` | Deleted private `GetTypeSuffix`, updated 2 callers to use `UnifiedInterceptorBuilder.GetTypeSuffix()`, added `using KnockOff.Builder;` |
| `src/Generator/Models/SymbolHelpers.cs` | Added `Rank == 1` guard and `Array{rank}D` branch for multidimensional arrays |
| `src/Tests/KnockOffTests/TestInterfaces.cs` | Added `IParamTypeSuffixService` interface, `ParamTypeSuffixKnockOff` standalone stub, `ParamTypeSuffixInlineTests` inline stub |
| `src/Tests/KnockOffTests/ParamTypeSuffixTests.cs` | New file: 16 tests covering all bug categories for standalone and inline patterns |
| `src/Tests/KnockOffTests/EdgeCaseValueOverloadTests.cs` | Updated delegate type references from `_void` to `_Void` (in-scope: matches corrected casing) |

### Generated Code Samples

The generator now produces valid identifiers for all problematic parameter types:

- `List<string?>` -> `ProcessDelegate_Collections_Generic_List_string_Void` (no `?` in identifier)
- `(int, string)` -> `ProcessDelegate_int_string_Void` (no parentheses)
- `int[,]` -> `ProcessDelegate_Int32Array2D_Void` (distinct from `Int32Array`)
- `string?[]` -> `ProcessDelegate_StringArray_Void` (keyword matched correctly)
- `nint` -> `ProcessDelegate_IntPtr_Void` (mapped to CLR type name)
- `nuint` -> `ProcessDelegate_UIntPtr_Void` (mapped to CLR type name)

---

## Architect Verification

**Verified:** 2026-02-06
**Verdict:** VERIFIED

### Independent Test Results

All builds and tests executed independently by the architect. Results match the developer's reported numbers exactly.

**Build:**
- `dotnet build src/KnockOff.sln` -- 0 warnings, 0 errors
- `dotnet build src/Design/Design.Stubs` -- 0 warnings, 0 errors (all 3 TFMs)

**Tests:**
- KnockOffTests.dll (net8.0): 1184 passed, 0 failed
- KnockOffTests.dll (net9.0): 1185 passed, 0 failed
- KnockOffTests.dll (net10.0): 1185 passed, 0 failed
- KnockOffTests.AssemblyStrict.dll: 14 passed per TFM, 0 failed
- KnockOff.Documentation.Samples.dll: 571 passed per TFM, 0 failed
- KnockOff.NeatooInterfaceTests.dll: 473 passed per TFM, 0 failed
- Design.Tests.dll: 259 passed per TFM, 0 failed
- **Zero failures across all test projects and all target frameworks**

### Design Match

The implementation matches the original plan with one minor improvement:

1. **Canonical `GetTypeSuffix(string)` in `UnifiedInterceptorBuilder.cs`** -- Matches plan. All 18 keywords present (including `nint`/`nuint`). All 6 bug fixes applied: multidimensional array parsing, nullable stripping after bracket removal, tuple parenthesis stripping, embedded `?` removal in fallback, `nint`/`nuint` keywords, and `void` -> `Void` casing fix. Minor improvement: condition changed from `workingType.Length >= 3` (plan) to `workingType.Length >= 2` (implementation), which correctly eliminates the dead-code `else if` branch identified during developer review.

2. **Three private copies deleted** -- Confirmed: `grep` for `private static string GetTypeSuffix` in `src/Generator/` returns zero results. No copy remains in `FlatModelBuilder.cs`, `InlineModelBuilder.cs`, or `FlatRenderer.cs`.

3. **All 6 callers updated** -- Confirmed all reference `UnifiedInterceptorBuilder.GetTypeSuffix()`:
   - `FlatModelBuilder.cs`: lines 633, 1157, 1288, 1292
   - `InlineModelBuilder.cs`: lines 280, 281, 926
   - `FlatRenderer.cs`: lines 1092, 1095 (with `using KnockOff.Builder;` added at line 6)

4. **Symbol-based fix in `SymbolHelpers.cs`** -- Matches plan exactly: `when array.Rank == 1` guard for 1D arrays, separate branch producing `Array{rank}D` for multidimensional arrays.

5. **New tests** -- 16 tests in `ParamTypeSuffixTests.cs` cover all 6 bug categories across standalone (Pattern 1) and inline (Pattern 5) patterns. Tests verify OnCall routing and Verify tracking for each overloaded parameter type.

6. **In-scope test update** -- `EdgeCaseValueOverloadTests.cs` delegate type references changed from `_void` to `_Void` to match corrected casing. This is the expected and correct consequence of the `void` -> `Void` fix.

7. **Test interface** -- `IParamTypeSuffixService` uses `List<string?>` instead of `Dictionary<string?, int>` (plan noted `Dictionary` but developer correctly substituted to avoid `notnull` constraint on `TKey`). This was documented in the implementation progress.

### Issues Found

None.
