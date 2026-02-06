# Fix User Method Detection for Custom Type Parameters

**Date:** 2026-02-05
**Related Todo:** [User Method Detection Fails for Custom Type Parameters](../todos/user-method-custom-type-detection.md)
**Status:** Complete
**Last Updated:** 2026-02-05 (architect verification complete)

---

## Overview

User method override detection fails when interface/class method parameters use custom (non-primitive) types. The detection side builds signature keys using syntax-level type names (e.g., `"User"`), while the matching side uses semantic-model fully-qualified names (e.g., `"KnockOff.Tests.User"`). These keys never match, so the generator does not recognize the user method override and generates an interceptor without user method fallback.

**Impact:** Any user method whose parameters include a custom type is silently ignored. The method appears to work (no compiler error) but the user method is never called at runtime.

---

## Approach

Change the detection side (`BuildOverrideSignatureKey` in `KnockOffGenerator.Helpers.cs`) to resolve parameter types through the semantic model instead of using `p.Type?.ToString()` (syntax text). This produces fully-qualified type names that match the keys built by the matching side (`BuildOverrideSignatureKey` in `SymbolHelpers.cs`).

**Why fix the detection side, not the matching side?**
- The matching side already uses the canonical representation (`ToDisplayString(FullyQualifiedWithNullability)`)
- Normalizing the matching side to short names would be fragile (ambiguous when two namespaces have identically-named types)
- The semantic model is already available at both call sites (`TransformClass` and `TransformStandaloneClassStub`)
- This approach handles all type forms uniformly: generics, arrays, nullables, nested types, aliases

---

## Design

### Current Architecture (Detection Side)

```
TransformClass / TransformStandaloneClassStub
  --> DetectUserOverrideMethods(classSymbol)
        --> BuildOverrideSignatureKey(MethodDeclarationSyntax method)
              --> p.Type?.ToString()  // SYNTAX: returns "User"
              --> NormalizeSyntaxType(...)  // maps System.X -> keywords
              --> produces key: "Update_(User)"
```

### Current Architecture (Matching Side)

```
FlatModelBuilder.Build / StandaloneClassModelBuilder.Build
  --> SymbolHelpers.BuildOverrideSignatureKey(methodName, parameters)
        --> p.Type  // SEMANTIC: "global::KnockOff.Tests.User"
        --> NormalizeTypeForOverrideMatching(...)  // strips global::, maps System.X -> keywords
        --> produces key: "Update_(KnockOff.Tests.User)"
```

### Proposed Fix

Pass `SemanticModel` into `DetectUserOverrideMethods`, then use it inside `BuildOverrideSignatureKey(MethodDeclarationSyntax)` to resolve each parameter type to its `ITypeSymbol` and format it using `ToDisplayString(FullyQualifiedWithNullability)`:

```
TransformClass / TransformStandaloneClassStub
  --> DetectUserOverrideMethods(classSymbol, semanticModel)  // NEW: pass SemanticModel
        --> BuildOverrideSignatureKey(MethodDeclarationSyntax method, SemanticModel semanticModel)
              --> semanticModel.GetTypeInfo(p.Type).Type  // ITypeSymbol
              --> .ToDisplayString(FullyQualifiedWithNullability)  // "global::KnockOff.Tests.User"
              --> NormalizeSyntaxType(...)  // strips global::, maps System.X -> keywords
              --> produces key: "Update_(KnockOff.Tests.User)"  // MATCHES!
```

### Key Design Details

1. **`SemanticModel.GetTypeInfo()`**: This resolves syntax-level type references to semantic-level `ITypeSymbol` objects. It handles all type forms: simple names, fully-qualified names, aliases (`using X = Ns.Type`), nullable types, generic types, array types.

2. **Fallback behavior**: If `GetTypeInfo()` returns null (e.g., type is unresolvable), fall back to `p.Type?.ToString()` with `NormalizeSyntaxType()`. This preserves current behavior for edge cases and ensures no regression for primitive types.

3. **`NormalizeSyntaxType` stays relevant**: After getting the fully-qualified display string, apply `NormalizeSyntaxType` to strip `global::` and map `System.X` to keywords. This produces keys identical to `NormalizeTypeForOverrideMatching` on the matching side. (Both functions perform the same transformations.)

4. **No changes to the matching side**: `SymbolHelpers.BuildOverrideSignatureKey` and `NormalizeTypeForOverrideMatching` remain unchanged.

5. **No changes to model types**: `KnockOffTypeInfo.UserOverrideMethods` and `StandaloneClassStubInfo.UserOverrideMethods` remain `EquatableArray<string>` -- only the string values change from short-name keys to fully-qualified-name keys.

---

## Implementation Steps

### Phase 1: Fix the Generator (Single File Change)

1. **Modify `DetectUserOverrideMethods`** in `KnockOffGenerator.Helpers.cs`:
   - Add `SemanticModel semanticModel` parameter
   - Pass it to `BuildOverrideSignatureKey`

2. **Modify `BuildOverrideSignatureKey(MethodDeclarationSyntax)`** in `KnockOffGenerator.Helpers.cs`:
   - Add `SemanticModel semanticModel` parameter
   - Replace `p.Type?.ToString()` with:
     ```csharp
     var typeInfo = semanticModel.GetTypeInfo(p.Type);
     var typeSymbol = typeInfo.Type;
     var typeName = typeSymbol != null
         ? NormalizeSyntaxType(typeSymbol.ToDisplayString(SymbolHelpers.FullyQualifiedWithNullability))
         : NormalizeSyntaxType(p.Type?.ToString() ?? "object");
     ```
   - Note: `SymbolHelpers.FullyQualifiedWithNullability` is the public static field

3. **Update call sites** to pass `SemanticModel`:
   - `TransformClass` in `KnockOffGenerator.Transform.cs` (line 873): pass `context.SemanticModel`
   - `TransformStandaloneClassStub` in `KnockOffGenerator.StandaloneClass.cs` (line 177): pass `context.SemanticModel`

### Phase 2: Verify

1. Build the generator: `dotnet build src/Generator`
2. Build all test projects: `dotnet build src/KnockOff.sln`
3. Run the 5 failing custom type detection tests -- all should pass
4. Run the 3 failing Design.Tests VoidUserMethodFallback tests -- all should pass
5. Run the full test suite to verify no regressions

---

## Acceptance Criteria

- [ ] All 5 `UserMethodCustomTypeDetectionTests` pass (3 were failing)
- [ ] All 8 `VoidUserMethodFallbackTests` pass (3 were failing)
- [ ] All existing user method tests continue to pass (primitive types)
- [ ] Full test suite passes with no regressions
- [ ] Design.Stubs and Design.Tests compile successfully

---

## Dependencies

None. This is a self-contained bug fix in the generator pipeline.

---

## Risks / Considerations

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| `GetTypeInfo()` returns null for some syntax forms | Low | Medium | Fallback to `p.Type?.ToString()` preserves current behavior |
| Performance impact of semantic model lookups | Low | Low | Only called during user override detection, not for all members |
| Different `SemanticModel` for different partial declarations | Low | Medium | `DeclaringSyntaxReferences` returns refs from all syntax trees; each syntax ref has a valid tree for `SemanticModel.GetTypeInfo()` but the SemanticModel passed in may be from a different tree. Must use `compilation.GetSemanticModel(syntaxTree)` if needed -- but in practice the SemanticModel from the current context covers the entire compilation |

**Semantic Model tree mismatch**: The `SemanticModel` passed in comes from `context.SemanticModel` which is for the syntax tree containing the attribute. User override methods may be in a different partial class file (different syntax tree). `SemanticModel.GetTypeInfo()` requires the syntax node to be in the same tree. **Mitigation**: Since `DetectUserOverrideMethods` iterates `classSymbol.DeclaringSyntaxReferences`, each syntax ref may come from a different tree. We need to obtain the correct `SemanticModel` for each tree. The `GeneratorAttributeSyntaxContext` provides a single `SemanticModel` but the Roslyn incremental generator API provides `context.SemanticModel` which is tied to the target node's tree. For nodes in other trees, we can use `context.SemanticModel.Compilation.GetSemanticModel(syntaxRef.SyntaxTree)`.

**Updated approach**: Pass `Compilation` (not `SemanticModel`) into `DetectUserOverrideMethods`. Inside the loop over `DeclaringSyntaxReferences`, call `compilation.GetSemanticModel(syntaxRef.SyntaxTree)` for each partial declaration's tree.

---

## Architectural Verification

### Scope Table

| Pattern | Affected? | Notes |
|---------|-----------|-------|
| 1. Standalone Interface | Yes | `TransformClass` -> `DetectUserOverrideMethods` |
| 2. Generic Standalone Interface | Yes | Same pipeline as Pattern 1 |
| 3. Standalone Class | Yes | `TransformStandaloneClassStub` -> `DetectUserOverrideMethods` |
| 4. Generic Standalone Class | Yes | Same pipeline as Pattern 3 |
| 5. Inline Interface | No | No user methods (inline stubs generate entire class) |
| 6. Inline Class | No | No user methods (inline stubs generate entire class) |
| 7. Inline Delegate | No | No user methods |
| 8. Open Generic Interface | No | No user methods (inline pattern) |
| 9. Open Generic Class | No | No user methods (inline pattern) |

### Member Types Affected

| Member Type | Affected? | Notes |
|-------------|-----------|-------|
| Methods | Yes | This is the bug -- method parameter types are mismatched |
| Properties | No | Property user override detection uses name-only matching (no parameters) |
| Indexers | N/A | Indexer user overrides not supported |
| Events | N/A | Event user overrides not supported |

### Design Project Verification

**Existing failing acceptance criteria (already in place):**

1. `src/Design/Design.Stubs/UserMethods/VoidUserMethodFallback.cs` - Stub with user method overrides for `SaveOrder_(Order)` and `FormatOrder_(Order)` using custom type `Order`
   - **Status: Needs Implementation** - Compiles but user methods not detected at generation time
   - **Evidence**: `VoidUserMethodFallbackTests` Tests 1, 2, 7 fail

2. `src/Tests/KnockOffTests/UserMethodCustomTypeDetectionTests.cs` - Tests for `FindUser_(UserQuery)`, `SaveUser_(UserRecord)`, `UpdateUser_(int, UserRecord)` with custom types
   - **Status: Needs Implementation** - 3 of 5 tests fail (custom type params)
   - **Evidence**: Tests `Standalone_UserMethod_CustomTypeParam_NonVoid_IsCalledAsFallback`, `Standalone_UserMethod_CustomTypeParam_Void_IsCalledAsFallback`, `Standalone_UserMethod_MixedPrimitiveAndCustomTypeParams_IsCalledAsFallback` all fail

**Patterns 3/4 (Standalone Class) custom type coverage:**

The existing Design.Stubs for standalone class patterns (`StandaloneClassUserMethods.cs`) use `ServiceBase` and `RepositoryBase<T>` whose methods have only primitive-type parameters (`string`, `int`). These stubs do not exercise the custom type detection path. However, the fix is in `DetectUserOverrideMethods` which is shared by all standalone patterns (1-4), so fixing it for patterns 1-2 automatically fixes it for patterns 3-4. The existing primitive-type tests for patterns 3-4 verify no regression.

### Breaking Changes

**No.** The only behavioral change is that user method overrides with custom-type parameters are now correctly detected. Previously they were silently ignored. No existing working behavior changes.

### Codebase Analysis

**Files examined:**

| File | Purpose | Key Finding |
|------|---------|-------------|
| `src/Generator/KnockOffGenerator.Helpers.cs` | Detection side - `DetectUserOverrideMethods`, `BuildOverrideSignatureKey(MethodDeclarationSyntax)` | Uses `p.Type?.ToString()` (syntax text) for parameter types -- **ROOT CAUSE** |
| `src/Generator/Models/SymbolHelpers.cs` | Matching side - `BuildOverrideSignatureKey(string, EquatableArray<ParameterInfo>)`, `NormalizeTypeForOverrideMatching` | Uses `ParameterInfo.Type` (fully-qualified semantic name) |
| `src/Generator/Models/InterfaceModels.cs` | `ParameterInfo` record, `InterfaceMemberInfo.FromMethod` | `ParameterInfo.Type` populated via `p.Type.ToDisplayString(FullyQualifiedWithNullability)` |
| `src/Generator/KnockOffGenerator.Transform.cs` | `TransformClass` - call site for `DetectUserOverrideMethods` | `context.SemanticModel` available |
| `src/Generator/KnockOffGenerator.StandaloneClass.cs` | `TransformStandaloneClassStub` - call site for `DetectUserOverrideMethods` | `context.SemanticModel` available |
| `src/Generator/Builder/FlatModelBuilder.cs` | Patterns 1-2 builder - consumes `UserOverrideMethods` for `HasUserOverride` | No changes needed |
| `src/Generator/Builder/StandaloneClassModelBuilder.cs` | Patterns 3-4 builder - consumes `UserOverrideMethods` for `HasUserOverride` | No changes needed |
| `src/Tests/KnockOffTests/UserMethodCustomTypeDetectionTests.cs` | 5 tests (3 failing, 2 passing controls) | Acceptance criteria for the fix |
| `src/Design/Design.Tests/UserMethodTests/VoidUserMethodFallbackTests.cs` | 8 tests (3 failing, 5 passing) | Additional acceptance criteria |
| `src/Design/Design.Stubs/UserMethods/VoidUserMethodFallback.cs` | Design stub exercising the bug | Compiles but user methods not detected |
| `src/Design/Design.Stubs/UserMethods/UserMethodBasics.cs` | Working user methods (primitive types) | Control case -- must not regress |
| `src/Design/Design.Stubs/UserMethods/StandaloneClassUserMethods.cs` | Standalone class user methods (primitive types) | Control case -- must not regress |

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-02-05

### My Understanding of This Plan

**Core Change:** Fix user method override detection for methods with custom (non-primitive) type parameters by using the semantic model to resolve parameter types instead of syntax-level `p.Type?.ToString()`.

**User-Facing API:** No API change. User methods with custom type parameters that are currently silently ignored will start working as designed. This is a bug fix.

**Internal Changes:** Modify `DetectUserOverrideMethods` and `BuildOverrideSignatureKey(MethodDeclarationSyntax)` in `KnockOffGenerator.Helpers.cs` to accept `Compilation`, obtain a `SemanticModel` per syntax tree, and resolve parameter types via `GetTypeInfo()`. Update two call sites.

**Patterns Affected:** Standalone patterns 1-4 only (shared `DetectUserOverrideMethods`). Inline patterns 5-9 do not have user methods.

### Codebase Investigation

**Files Examined:**
- `src/Generator/KnockOffGenerator.Helpers.cs` -- Confirmed root cause: line 83 uses `p.Type?.ToString()` (syntax text). `DetectUserOverrideMethods` iterates `DeclaringSyntaxReferences`.
- `src/Generator/Models/SymbolHelpers.cs` -- Confirmed matching side: line 348 uses `p.Type` (fully-qualified semantic name) and `NormalizeTypeForOverrideMatching`.
- `src/Generator/Models/InterfaceModels.cs` -- Confirmed `ParameterInfo.Type` populated via `p.Type.ToDisplayString(SymbolHelpers.FullyQualifiedWithNullability)` at line 228.
- `src/Generator/KnockOffGenerator.Transform.cs` -- Confirmed call site at line 873: `DetectUserOverrideMethods(classSymbol)`.
- `src/Generator/KnockOffGenerator.StandaloneClass.cs` -- Confirmed call site at line 177: `DetectUserOverrideMethods(classSymbol)`.
- `src/Generator/Builder/FlatModelBuilder.cs` -- Confirmed builder consumes `UserOverrideMethods` via `SymbolHelpers.BuildOverrideSignatureKey`.
- `src/Generator/Builder/StandaloneClassModelBuilder.cs` -- Same consumption pattern.
- `src/Tests/KnockOffTests/UserMethodCustomTypeDetectionTests.cs` -- 5 tests, 3 failing.
- `src/Design/Design.Tests/UserMethodTests/VoidUserMethodFallbackTests.cs` -- 8 tests, 3 failing.
- `src/Design/Design.Stubs/UserMethods/VoidUserMethodFallback.cs` -- Stub with custom-type user methods not detected.
- `src/Design/Design.Stubs/UserMethods/UserMethodBasics.cs` -- Working primitive-type user methods (control case).
- `src/Design/Design.Stubs/UserMethods/StandaloneClassUserMethods.cs` -- Standalone class user methods, primitive types only.

**Searches Performed:**
- `BuildOverrideSignatureKey` -- found 2 definitions and usage in both builders
- `HasUserOverride|UserOverrideMethods` in Builder/ -- confirmed both builders use same lookup pattern
- `UseSpecialTypes` -- confirmed both `FullyQualifiedWithNullability` definitions include this flag, so `ToDisplayString` already produces C# keywords for built-in types (no regression risk for primitive types)
- `context.SemanticModel` -- confirmed available at both call sites

**Design.Stubs Verification:**
- Pattern 1/2: Architect provided failing code at `VoidUserMethodFallback.cs` with `SaveOrder_(Order)` and `FormatOrder_(Order)`. Confirmed code exists and compiles but user methods not detected. Failing tests confirmed.
- Pattern 3/4: Architect correctly notes existing stubs only use primitive types. Justification that fix is in shared code (`DetectUserOverrideMethods`) verified correct.
- Patterns 5-9: Correctly marked as not affected.

**Discrepancies Found:**
- Minor wording: Plan Phase 2 step 3 says "Run the 5 failing custom type detection tests" but only 3 of 5 are failing. Not blocking.

### Key Technical Verification

**NormalizeSyntaxType vs NormalizeTypeForOverrideMatching:** Both functions are functionally identical (strip `global::`, map `System.X` to keywords). After the fix, `ToDisplayString(FullyQualifiedWithNullability)` output goes through `NormalizeSyntaxType` on the detection side, producing identical keys to `NormalizeTypeForOverrideMatching` on the matching side.

**UseSpecialTypes flag:** Both `FullyQualifiedWithNullability` definitions include `UseSpecialTypes`, so `ToDisplayString` already produces `"int"` not `"System.Int32"`. The System.X keyword mappings in both normalize functions become dead code for `ToDisplayString` output but cause no harm. No regression risk for primitive types.

**Compilation vs SemanticModel:** The plan correctly identifies that `context.SemanticModel` is tied to one syntax tree, but `DeclaringSyntaxReferences` may span multiple trees. Using `compilation.GetSemanticModel(syntaxRef.SyntaxTree)` is the correct approach.

### Devil's Advocate Analysis

**Edge cases NOT explicitly covered:**
1. Nullable value types (e.g., `int?`) -- verified: both paths produce `"int?"` after normalization. No issue.
2. Array types (e.g., `User[]`) -- verified: `ToDisplayString` produces `"global::Ns.User[]"`, normalization strips `global::`, matching side produces same. No issue.
3. Type aliases (e.g., `using U = Ns.User`) -- `GetTypeInfo()` resolves aliases to their underlying type. Improvement over current behavior.

**Ways this could break existing functionality:**
1. None identified. For primitive types, `ToDisplayString` with `UseSpecialTypes` produces the same keyword as `p.Type?.ToString()`. For custom types, the fix produces matching keys instead of mismatched ones.

**Ways users could misunderstand the API:**
1. Not applicable -- this is a bug fix, not an API change.

### Verdict

This plan is exceptionally clear because it is a well-scoped bug fix with a precisely identified root cause, minimal change surface (one function signature, one function body, two call sites), specific acceptance criteria (6 named failing tests), and no architectural changes. The root cause analysis is accurate -- I verified every file and confirmed the syntax-vs-semantic mismatch. The `Compilation` approach correctly handles partial class declarations across multiple syntax trees.

---

## Implementation Contract

**Created:** 2026-02-05
**Approved by:** knockoff-developer

### Design Project Acceptance Criteria

- [x] `src/Design/Design.Tests/UserMethodTests/VoidUserMethodFallbackTests.cs:52` - `VoidMethod_CustomType_UserMethodShouldBeCalledAsFallback`: Now passes
- [x] `src/Design/Design.Tests/UserMethodTests/VoidUserMethodFallbackTests.cs:78` - `NonVoidMethod_CustomType_UserMethodShouldBeCalledAsFallback`: Now passes
- [x] `src/Design/Design.Tests/UserMethodTests/VoidUserMethodFallbackTests.cs:205` - `VoidMethod_CustomType_MultipleCalls_UserMethodCalledEachTime`: Now passes
- [x] `src/Tests/KnockOffTests/UserMethodCustomTypeDetectionTests.cs:38` - `Standalone_UserMethod_CustomTypeParam_NonVoid_IsCalledAsFallback`: Now passes
- [x] `src/Tests/KnockOffTests/UserMethodCustomTypeDetectionTests.cs:67` - `Standalone_UserMethod_CustomTypeParam_Void_IsCalledAsFallback`: Now passes
- [x] `src/Tests/KnockOffTests/UserMethodCustomTypeDetectionTests.cs:96` - `Standalone_UserMethod_MixedPrimitiveAndCustomTypeParams_IsCalledAsFallback`: Now passes

### In Scope

- [x] `src/Generator/KnockOffGenerator.Helpers.cs`: Add `Compilation` parameter to `DetectUserOverrideMethods`; inside the loop, call `compilation.GetSemanticModel(syntaxRef.SyntaxTree)` for each partial declaration
- [x] `src/Generator/KnockOffGenerator.Helpers.cs`: Add `SemanticModel` parameter to `BuildOverrideSignatureKey(MethodDeclarationSyntax)`; replace `p.Type?.ToString()` with `semanticModel.GetTypeInfo(p.Type).Type?.ToDisplayString(FullyQualifiedWithNullability)` with fallback to `p.Type?.ToString()` (used the in-scope `FullyQualifiedWithNullability` from `KnockOffGenerator.cs` rather than `SymbolHelpers.FullyQualifiedWithNullability` -- identical definitions)
- [x] `src/Generator/KnockOffGenerator.Transform.cs`: Pass `context.SemanticModel.Compilation` to `DetectUserOverrideMethods` call at line 873
- [x] `src/Generator/KnockOffGenerator.StandaloneClass.cs`: Pass `context.SemanticModel.Compilation` to `DetectUserOverrideMethods` call at line 177
- [x] Checkpoint: `dotnet build src/Generator` succeeds (0 warnings, 0 errors)
- [x] Checkpoint: `dotnet build src/KnockOff.sln` succeeds (0 warnings, 0 errors)
- [x] Checkpoint: All 6 previously-failing tests now pass
- [x] Checkpoint: Full test suite -- see Completion Evidence for 2 test failures (both pre-existing on `readme` branch, confirmed by stash-and-test)

### Out of Scope

- Adding custom-type user method tests for standalone class patterns 3-4 (existing primitive-type tests verify no regression; fix is in shared code)
- Changes to matching side (`SymbolHelpers.BuildOverrideSignatureKey`, `NormalizeTypeForOverrideMatching`)
- Changes to any builder or renderer
- Changes to model types (`KnockOffTypeInfo`, `StandaloneClassStubInfo`)

### Verification Gates

1. After code change: `dotnet build src/Generator` succeeds
2. After code change: `dotnet build src/KnockOff.sln` succeeds
3. After code change: All 6 previously-failing tests now pass
4. Final: Full test suite passes with zero failures; `dotnet build src/Design/Design.Stubs` succeeds

### Stop Conditions

If any occur, STOP and report:
- Out-of-scope test failure
- `GetTypeInfo()` returns null for the test case types (would indicate the approach needs refinement)
- Any compilation error in Design.Stubs after the fix

---

## Implementation Progress

**Started:** 2026-02-05
**Developer:** knockoff-developer

**Phase 1:** Fix Generator
- [x] Add `Compilation` parameter to `DetectUserOverrideMethods`
- [x] Add `SemanticModel` parameter to `BuildOverrideSignatureKey(MethodDeclarationSyntax)`
- [x] Use `compilation.GetSemanticModel(syntaxTree)` for each partial declaration
- [x] Use `semanticModel.GetTypeInfo(p.Type).Type.ToDisplayString(...)` with fallback
- [x] Update call sites in Transform.cs and StandaloneClass.cs
- [x] **Verification**: Build succeeds, all in-scope tests pass (see Completion Evidence)

---

## Completion Evidence

**Reported:** 2026-02-05

### Files Modified

1. `src/Generator/KnockOffGenerator.Helpers.cs` -- Added `Compilation compilation` parameter to `DetectUserOverrideMethods`; inside the loop over `DeclaringSyntaxReferences`, calls `compilation.GetSemanticModel(syntaxRef.SyntaxTree)` for each partial declaration. Added `SemanticModel semanticModel` parameter to `BuildOverrideSignatureKey(MethodDeclarationSyntax)` and replaced `p.Type?.ToString()` with `semanticModel.GetTypeInfo(p.Type).Type?.ToDisplayString(FullyQualifiedWithNullability)` with fallback.
2. `src/Generator/KnockOffGenerator.Transform.cs` -- Updated call at line 873: `DetectUserOverrideMethods(classSymbol, context.SemanticModel.Compilation)`
3. `src/Generator/KnockOffGenerator.StandaloneClass.cs` -- Updated call at line 177: `DetectUserOverrideMethods(classSymbol, context.SemanticModel.Compilation)`

### Build Results

- `dotnet build src/Generator` -- 0 warnings, 0 errors
- `dotnet build src/KnockOff.sln` -- 0 warnings, 0 errors
- `dotnet build src/Design/Design.Stubs` -- 0 warnings, 0 errors

### Previously-Failing Tests: All 6 Now Pass

**UserMethodCustomTypeDetectionTests (5/5 pass, 3 were previously failing):**
- `Standalone_UserMethod_CustomTypeParam_NonVoid_IsCalledAsFallback` -- PASS (was FAIL)
- `Standalone_UserMethod_CustomTypeParam_Void_IsCalledAsFallback` -- PASS (was FAIL)
- `Standalone_UserMethod_MixedPrimitiveAndCustomTypeParams_IsCalledAsFallback` -- PASS (was FAIL)
- `Standalone_UserMethod_PrimitiveParam_IsCalledAsFallback` -- PASS (was PASS)
- `Standalone_UserMethod_CustomTypeParam_OnCallSupersedesUserMethod` -- PASS (was PASS)

**VoidUserMethodFallbackTests (8/8 pass, 3 were previously failing):**
- All 8 tests pass across net8.0, net9.0, net10.0

### Full Test Suite Results

| Project | net8.0 | net9.0 | net10.0 |
|---------|--------|--------|---------|
| KnockOffTests | 1158 pass, 0 fail | 1159 pass, 0 fail | 1159 pass, 0 fail |
| KnockOffTests.AssemblyStrict | 14 pass, 0 fail | 14 pass, 0 fail | 14 pass, 0 fail |
| KnockOff.NeatooInterfaceTests | 473 pass, 0 fail | 473 pass, 0 fail | 473 pass, 0 fail |
| KnockOff.Documentation.Samples | 456 pass, **1 fail** | 456 pass, **1 fail** | 456 pass, **1 fail** |
| Design.Tests | 225 pass, **1 fail** | 225 pass, **1 fail** | 225 pass, **1 fail** |

### Test Failures Reported (2 total, both pre-existing on `readme` branch)

**Failure 1:** `KnockOff.Documentation.Samples.Readme.UserDomainModelTests.UpdateTest_KnockOff`
- Error: "Verification failed: GetUser: expected Once, actual 2 calls"
- This test is part of the in-progress `readme` branch work. Confirmed failing before my changes by stashing and testing the baseline. Not related to user method type detection.

**Failure 2:** `Design.Tests.AdvancedTests.WhenChainVerificationBugTests.SingleMatcher_VerifyAll_ShouldPassAfterMatcherInvoked`
- Error: "Verification failed: Add: expected AtLeastOnce, actual 0 calls"
- This is the known When chain verification bug tracked at `docs/todos/when-chain-verification-bug.md`. Added in commit `52eac84` as a known-failing test. Not related to this fix.

### Summary

- **Design Projects Compile:** Yes (Design.Stubs builds with 0 warnings, 0 errors)
- **All Contract Items:** Confirmed 100% complete
- **Documentation Updated:** N/A (no documentation changes needed for a bug fix)

---

## Architect Verification

**Verified:** 2026-02-05
**Verdict:** VERIFIED

### Independent Build Results

| Project | Result |
|---------|--------|
| `src/Generator/Generator.csproj` | 0 warnings, 0 errors |
| `src/KnockOff/KnockOff.csproj` | 0 warnings, 0 errors |
| `src/Design/Design.Stubs` | 0 warnings, 0 errors |
| `src/Tests/KnockOff.Documentation.Samples` | 0 warnings, 0 errors |
| Full solution (`src/KnockOff.sln`) | 0 warnings, 0 errors |

### Independent Test Results

| Project | net8.0 | net9.0 | net10.0 |
|---------|--------|--------|---------|
| KnockOffTests | 1158 pass, 0 fail | 1159 pass, 0 fail | 1159 pass, 0 fail |
| KnockOffTests.AssemblyStrict | 14 pass, 0 fail | 14 pass, 0 fail | 14 pass, 0 fail |
| KnockOff.NeatooInterfaceTests | 473 pass, 0 fail | 473 pass, 0 fail | 473 pass, 0 fail |
| KnockOff.Documentation.Samples | 456 pass, 1 fail | 456 pass, 1 fail | 456 pass, 1 fail |
| Design.Tests | 225 pass, 1 fail | 225 pass, 1 fail | 225 pass, 1 fail |

### Targeted Test Verification

All 5 UserMethodCustomTypeDetectionTests pass (3 were previously failing):
- `Standalone_UserMethod_CustomTypeParam_NonVoid_IsCalledAsFallback` -- PASS (was FAIL)
- `Standalone_UserMethod_CustomTypeParam_Void_IsCalledAsFallback` -- PASS (was FAIL)
- `Standalone_UserMethod_MixedPrimitiveAndCustomTypeParams_IsCalledAsFallback` -- PASS (was FAIL)
- `Standalone_UserMethod_PrimitiveParam_IsCalledAsFallback` -- PASS (was PASS, no regression)
- `Standalone_UserMethod_CustomTypeParam_OnCallSupersedesUserMethod` -- PASS (was PASS, no regression)

All 8 VoidUserMethodFallbackTests pass (3 were previously failing, confirmed via Design.Tests 225 pass total).

### Test Failures Assessment

Two test failures observed, both confirmed pre-existing on `readme` branch via stash-and-test:

1. `KnockOff.Documentation.Samples.Readme.UserDomainModelTests.UpdateTest_KnockOff`
   - Error: "Verification failed: GetUser: expected Once, actual 2 calls"
   - Pre-existing: Confirmed. Fails on baseline without developer's changes. Part of in-progress `readme` branch work (modified `ReadMeUseCase.cs` visible in `git status` at conversation start).

2. `Design.Tests.AdvancedTests.WhenChainVerificationBugTests.SingleMatcher_VerifyAll_ShouldPassAfterMatcherInvoked`
   - Error: "Verification failed: Add: expected AtLeastOnce, actual 0 calls"
   - Pre-existing: Confirmed. Fails on baseline (5 of 6 WhenChainVerificationBugTests fail on baseline; developer's When chain fix resolved 4 of 5, this one remains as a separate bug tracked at `docs/todos/verifyall-totalcallcount-bug.md`).

### Design Match Verification

The implementation exactly matches the original plan:

1. **`DetectUserOverrideMethods` signature**: Takes `Compilation compilation` parameter (Helpers.cs line 22). MATCHES plan.
2. **Semantic model per syntax tree**: Inside the `DeclaringSyntaxReferences` loop, calls `compilation.GetSemanticModel(syntaxRef.SyntaxTree)` (Helpers.cs line 36). MATCHES plan's updated approach for partial class declarations across multiple files.
3. **`BuildOverrideSignatureKey` signature**: Takes `SemanticModel semanticModel` parameter (Helpers.cs line 68). MATCHES plan.
4. **Type resolution**: Uses `semanticModel.GetTypeInfo(p.Type).Type?.ToDisplayString(FullyQualifiedWithNullability)` with fallback to `p.Type.ToString()` (Helpers.cs lines 97-101). MATCHES plan.
5. **Call site in Transform.cs**: Passes `context.SemanticModel.Compilation` (line 873). MATCHES plan.
6. **Call site in StandaloneClass.cs**: Passes `context.SemanticModel.Compilation` (line 177). MATCHES plan.
7. **No Compilation/SemanticModel in models**: Grep of `src/Generator/Model/` for `Compilation|SemanticModel` returns zero matches. Equatable model graph is clean. MATCHES plan constraint.
8. **No changes to matching side**: `SymbolHelpers.BuildOverrideSignatureKey` and `NormalizeTypeForOverrideMatching` are unmodified. MATCHES plan.
9. **No changes to builders or renderers** (for user method detection). MATCHES plan.

### Scope Note

The working tree contains additional changes beyond this bug fix: a When chain verification fix (`InlineRenderer.cs`, `MethodInterceptorRenderer.cs`, `WhenChainRenderer.cs`) and associated todo/plan files. These are out of scope for this verification but appear to be a separate, correctly-implemented fix that resolved 4 additional failing tests.

### Issues Found

None related to the user method custom type detection fix. The implementation is correct, complete, and matches the plan exactly.
