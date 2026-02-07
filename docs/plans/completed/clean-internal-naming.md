# Clean Internal Naming to Match Public API

**Date:** 2026-02-07
**Related Todo:** [Clean Internal Naming to Match Public API](../todos/clean-internal-naming.md)
**Status:** Complete
**Last Updated:** 2026-02-07

---

## Overview

After PR #54 (OnGet/OnSet -> Get/Set) and PR #55 (Returns->Return, Execute->Call), internal naming in the generator and generated code still uses old conventions. This plan covers a mechanical rename of all stale internal identifiers to match the new public API.

---

## Approach

This is a find-and-replace operation across three layers:
1. **Generated code** (renderer output) -- private fields and public properties in the generated interceptor classes
2. **Generator internals** (models and builders) -- C# identifiers in the generator source
3. **Error messages** (library code) -- user-facing text in StubException

The rename is purely internal. No public API changes. The compiler will catch any mismatches -- if the build passes, the rename is correct.

---

## Design

### Naming Conventions

The following renames align internal names with the public API:

#### Category 1: Generated Property Interceptor Code (Properties and Indexers)

Generated public property names `OnGet`/`OnSet` on interceptor classes become `Get`/`Set` to match the new public API entry point methods.

| Old Name | New Name | Where Generated |
|----------|----------|-----------------|
| `public ... OnGet { get; set; }` | `public ... Get { get; set; }` | FlatRenderer (property), InlineRenderer (property) |
| `public ... OnSet { get; set; }` | `public ... Set { get; set; }` | FlatRenderer (property), InlineRenderer (property) |
| `public ... OnGet` (with backing field) | `public ... Get` (with backing field) | InlineRenderer (indexer), FlatRenderer (indexer) |
| `public ... OnSet` (with backing field) | `public ... Set` (with backing field) | InlineRenderer (indexer), FlatRenderer (indexer) |
| `private ... _onGet` | `private ... _get` | InlineRenderer (indexer) |
| `private ... _onSet` | `private ... _set` | InlineRenderer (indexer) |
| `_onGet` references in generated logic | `_get` | PropertyInterceptorRenderer, IndexerInterceptorRenderer |
| `_onGet != null` | `_get != null` | InlineRenderer (IsConfigured), FlatRenderer (IsConfigured) |
| `_onSet != null` | `_set != null` | InlineRenderer (IsConfigured), FlatRenderer (IsConfigured) |
| `_onGetTracking` | `_getTracking` | PropertyInterceptorRenderer, IndexerInterceptorRenderer |
| `_onSetTracking` | `_setTracking` | PropertyInterceptorRenderer, IndexerInterceptorRenderer |
| `OnGet` / `OnSet` in generated comments | `Get` / `Set` | All renderers |

#### Category 2: Generated Method Interceptor Code

| Old Name | New Name | Where Generated |
|----------|----------|-----------------|
| `_onCall` | `_call` | MethodInterceptorRenderer, FlatRenderer (generic handlers), InlineRenderer (generic handlers) |
| `_onCallTracking` | `_callTracking` | MethodInterceptorRenderer |
| `_onCallSimplified` | `_callSimplified` | MethodInterceptorRenderer |
| `_onCallSimplifiedTracking` | `_callSimplifiedTracking` | MethodInterceptorRenderer |
| `_onCallSimplifiedVoid` | `_callSimplifiedVoid` | MethodInterceptorRenderer |
| `_onCallSimplifiedVoidTracking` | `_callSimplifiedVoidTracking` | MethodInterceptorRenderer |
| `_onCall_{suffix}` | `_call_{suffix}` | MethodInterceptorRenderer (overloads) |
| `_onCallTracking_{suffix}` | `_callTracking_{suffix}` | MethodInterceptorRenderer (overloads) |
| `_onCallSimplified_{suffix}` | `_callSimplified_{suffix}` | MethodInterceptorRenderer (overloads) |
| `_onCallSimplifiedTracking_{suffix}` | `_callSimplifiedTracking_{suffix}` | MethodInterceptorRenderer (overloads) |
| `_onCallSimplifiedVoid_{suffix}` | `_callSimplifiedVoid_{suffix}` | MethodInterceptorRenderer (overloads) |
| `_onCallSimplifiedVoidTracking_{suffix}` | `_callSimplifiedVoidTracking_{suffix}` | MethodInterceptorRenderer (overloads) |
| `_returnsValue` | `_returnValue` | MethodInterceptorRenderer |
| `_hasReturnsValue` | `_hasReturnValue` | MethodInterceptorRenderer |
| `_returnsValueTracking` | `_returnValueTracking` | MethodInterceptorRenderer |
| `onCallCallback` (local variable) | `callCallback` | FlatRenderer, InlineRenderer |

#### Category 3: Generator Internal Model Properties

| Old Name | New Name | File |
|----------|----------|------|
| `OnCallDelegateType` | `CallDelegateType` | `Model/Flat/FlatMethodModel.cs` (line 24) |
| `OnCallDelegateType` | `CallDelegateType` | `Model/Shared/UnifiedMethodInterceptorModel.cs` (line 39) |
| `OnCallArgs` | `CallArgs` | `Model/Inline/InlineInterfaceImplementation.cs` (line 40) |
| `OnCallArgumentList` | `CallArgumentList` | `Model/Inline/InlineClassStubModel.cs` (line 261) |
| `OnCallType` | `CallType` | `Model/Inline/InlineDelegateStubModel.cs` (line 36) |

#### Category 4: Generator Internal Builder Methods

| Old Name | New Name | File |
|----------|----------|------|
| `BuildOnCallDelegateType` | `BuildCallDelegateType` | `Builder/UnifiedInterceptorBuilder.cs` (line 365) |
| `onCallDelegateType` (local var) | `callDelegateType` | `Builder/UnifiedInterceptorBuilder.cs` (line 49) |
| `onCallDelegateType` (local var) | `callDelegateType` | `Builder/FlatModelBuilder.cs` (lines 806, 820, 824, 829, 853, 949, 970) |
| `onCallArgs` (local var) | `callArgs` | `Builder/ClassModelBuilder.cs` (lines 413, 427) |
| `onCallArgs` (local var) | `callArgs` | `Builder/StandaloneClassModelBuilder.cs` (lines 469, 483) |
| `onCallArgs` (local var) | `callArgs` | `Renderer/FlatRenderer.cs` (lines 2230, 2237, 2244, 2251) |
| `OnCallArgs:` (named argument) | `CallArgs:` | `Builder/InlineModelBuilder.cs` (lines 755, 811, 901, 980, 1043, 1081) |
| `OnCallArgumentList:` (named argument) | `CallArgumentList:` | `Builder/ClassModelBuilder.cs` (line 427) |
| `OnCallArgumentList:` (named argument) | `CallArgumentList:` | `Builder/StandaloneClassModelBuilder.cs` (line 483) |
| `OnCallType:` (named argument) | `CallType:` | `Builder/InlineModelBuilder.cs` (line 1172) |
| `onCallType` (local var) | `callType` | `Renderer/Shared/ModelAdapters.cs` (lines 335, 349) |
| `OnCallDelegateType:` (named argument) | `CallDelegateType:` | `Builder/UnifiedInterceptorBuilder.cs` (lines 63, 90) |
| `OnCallDelegateType:` (named argument) | `CallDelegateType:` | `Renderer/Shared/ModelAdapters.cs` (lines 80, 154, 349) |
| `.OnCallDelegateType` (property access) | `.CallDelegateType` | `Renderer/Shared/ModelAdapters.cs` (line 67) |
| `.OnCallDelegateType` (property access) | `.CallDelegateType` | `Renderer/Shared/MethodInterceptorRenderer.cs` (line 54) |
| `.OnCallType` (property access) | `.CallType` | `Renderer/Shared/ModelAdapters.cs` (lines 335, 349) |
| `.OnCallArgs` (property access) | `.CallArgs` | `Renderer/InlineRenderer.cs` (lines 1262, 1266) |

#### Category 5: Comments and Documentation Strings

| Old Reference | New Reference | Files |
|---------------|---------------|-------|
| `"OnCall"` in comments | `"Call"` | Various model/builder xml-doc comments |
| `"OnGet"` / `"OnSet"` in generated xml-doc | `"Get"` / `"Set"` | FlatRenderer, InlineRenderer |
| `"Configure OnCall before invoking."` | `"Configure Call before invoking."` | `src/KnockOff/StubException.cs` (line 31) |
| xml-doc comments referencing OnCall | Updated to Call | UnifiedMethodInterceptorModel, MethodOverloadSignature, InlineClassStubModel, InlineMethodModel, InlineDelegateStubModel, FlatGenericMethodHandlerGroup, FlatMethodGroup, ClassModelBuilder, StandaloneClassModelBuilder, InlineModelBuilder, UnifiedInterceptorBuilder |

### What Does NOT Change

- `RaiseReturnsValue` in event models -- this describes whether the delegate returns a value, not the `Returns()` API
- Public API method names (`Get()`, `Set()`, `Return()`, `Call()`) -- already renamed in PR #54 and #55
- Domain method names like `Execute()` that appear as user-defined methods being stubbed
- External interface/type names (`IMethodTracking`, `IPropertyGetBuilder`, etc.)

---

## Implementation Steps

### Phase 1: Model Property Renames (5 files)

Rename record properties. These will cascade compiler errors to all usage sites.

1. `src/Generator/Model/Flat/FlatMethodModel.cs`: `OnCallDelegateType` -> `CallDelegateType`
2. `src/Generator/Model/Shared/UnifiedMethodInterceptorModel.cs`: `OnCallDelegateType` -> `CallDelegateType` + update comments
3. `src/Generator/Model/Inline/InlineInterfaceImplementation.cs`: `OnCallArgs` -> `CallArgs`
4. `src/Generator/Model/Inline/InlineClassStubModel.cs`: `OnCallArgumentList` -> `CallArgumentList` + update comment
5. `src/Generator/Model/Inline/InlineDelegateStubModel.cs`: `OnCallType` -> `CallType` + update comment

**Checkpoint:** `dotnet build src/Generator/Generator.csproj` will fail with errors at all usage sites -- that is expected and confirms all references are found.

### Phase 2: Builder Method and Variable Renames (5 files)

Fix the compiler errors from Phase 1 and rename builder methods/locals.

1. `src/Generator/Builder/UnifiedInterceptorBuilder.cs`: `BuildOnCallDelegateType` -> `BuildCallDelegateType`, rename local vars, update named args and comment
2. `src/Generator/Builder/FlatModelBuilder.cs`: rename local vars (`onCallDelegateType` -> `callDelegateType`), update named args
3. `src/Generator/Builder/InlineModelBuilder.cs`: update named args (`OnCallArgs:` -> `CallArgs:`, `OnCallType:` -> `CallType:`)
4. `src/Generator/Builder/ClassModelBuilder.cs`: rename local vars (`onCallArgs` -> `callArgs`), update named args
5. `src/Generator/Builder/StandaloneClassModelBuilder.cs`: rename local vars (`onCallArgs` -> `callArgs`), update named args

**Checkpoint:** `dotnet build src/Generator/Generator.csproj` should succeed.

### Phase 3: Renderer Renames - Generated Code Strings (5 files)

These are string literals in the renderers that become part of the generated C# code.

1. `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs`: `_onCall` -> `_call`, `_onCallTracking` -> `_callTracking`, `_onCallSimplified` -> `_callSimplified`, `_onCallSimplifiedTracking` -> `_callSimplifiedTracking`, `_onCallSimplifiedVoid` -> `_callSimplifiedVoid`, `_onCallSimplifiedVoidTracking` -> `_callSimplifiedVoidTracking`, `_returnsValue` -> `_returnValue`, `_hasReturnsValue` -> `_hasReturnValue`, `_returnsValueTracking` -> `_returnValueTracking` (plus all `_{suffix}` variants)
2. `src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs`: `_onGet` -> `_get`, `_onGetTracking` -> `_getTracking`, `_onSet` -> `_set`, `_onSetTracking` -> `_setTracking`
3. `src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs`: `_onGet` -> `_get`, `_onGetTracking` -> `_getTracking`, `_onSet` -> `_set`, `_onSetTracking` -> `_setTracking`
4. `src/Generator/Renderer/FlatRenderer.cs`:
   - Property `OnGet` -> `Get`, `OnSet` -> `Set` (public property names)
   - Indexer `OnGet` -> `Get`, `OnSet` -> `Set` (public property names)
   - Generic handler `_onCall` -> `_call` (all occurrences)
   - Local var `onCallCallback` -> `callCallback`, `onCallArgs` -> `callArgs`
   - Update generated comments referencing OnGet/OnSet
5. `src/Generator/Renderer/InlineRenderer.cs`:
   - Property `OnGet` -> `Get`, `OnSet` -> `Set` (public property names)
   - Indexer `OnGet` -> `Get`, `OnSet` -> `Set` (public property names), `_onGet` -> `_get`, `_onSet` -> `_set`
   - Generic handler `_onCall` -> `_call` (all occurrences)
   - Local var `onCallCallback` -> `callCallback`
   - Update generated comments referencing OnGet/OnSet

### Phase 4: Adapter and Library Code (2 files)

1. `src/Generator/Renderer/Shared/ModelAdapters.cs`: Update `.OnCallDelegateType` -> `.CallDelegateType`, `.OnCallType` -> `.CallType`, local var `onCallType` -> `callType`, named args
2. `src/KnockOff/StubException.cs`: `"Configure OnCall before invoking."` -> `"Configure the method before invoking."`

**Note on StubException:** Rather than "Configure Call before invoking" (which reads oddly), use the more natural phrasing "Configure the method before invoking."

### Phase 5: Comment-Only Updates (6+ files)

Update xml-doc comments that reference old naming:
1. `src/Generator/Model/Shared/UnifiedMethodInterceptorModel.cs`: "OnCall" in multiple doc comments
2. `src/Generator/Model/Shared/MethodOverloadSignature.cs`: "OnCall" in doc comment
3. `src/Generator/Model/Inline/InlineMethodModel.cs`: "OnCall" in doc comment
4. `src/Generator/Model/Inline/InlineClassStubModel.cs`: "OnCall" in doc comment
5. `src/Generator/Model/Flat/FlatGenericMethodHandlerGroup.cs`: "OnCall" in doc comment
6. `src/Generator/Model/Flat/FlatMethodGroup.cs`: "OnCall" in doc comment
7. `src/Generator/Builder/ClassModelBuilder.cs`: "OnCall" in doc comment
8. `src/Generator/Builder/StandaloneClassModelBuilder.cs`: "OnCall" in doc comment
9. `src/Generator/Builder/InlineModelBuilder.cs`: "OnCall" in doc comment

### Phase 6: Build and Test

1. `dotnet build src/KnockOff.sln`
2. `dotnet test src/KnockOff.sln`
3. `dotnet build src/Design/Design.Stubs`
4. `dotnet test src/Design/Design.Tests`

---

## Acceptance Criteria

- [ ] All generated property interceptors use `Get`/`Set` instead of `OnGet`/`OnSet` as public property names
- [ ] All generated method interceptors use `_call`/`_callTracking` instead of `_onCall`/`_onCallTracking`
- [ ] All generated method interceptors use `_returnValue`/`_hasReturnValue`/`_returnValueTracking` instead of `_returnsValue`/`_hasReturnsValue`/`_returnsValueTracking`
- [ ] All generated property/indexer interceptors use `_get`/`_getTracking`/`_set`/`_setTracking` instead of `_onGet`/`_onGetTracking`/`_onSet`/`_onSetTracking`
- [ ] All model properties renamed from `OnCall*` prefix to `Call*` prefix
- [ ] Builder method `BuildOnCallDelegateType` renamed to `BuildCallDelegateType`
- [ ] StubException error message updated
- [ ] All xml-doc comments updated
- [ ] All 9 patterns build successfully
- [ ] All tests pass
- [ ] Design.Stubs compiles
- [ ] Design.Tests pass

---

## Dependencies

- Depends on: PR #54 and PR #55 being merged (both are merged)
- No external dependencies

---

## Risks / Considerations

- **Low risk:** This is a mechanical rename. The compiler catches all mismatches.
- **String matching care:** The `_onCall` pattern appears both as standalone (`_onCall`) and with suffixes (`_onCall_{suffix}`), as well as in names like `_onCallSimplified`. Each variant must be renamed correctly, but since the rename is `_onCall` -> `_call`, `_onCallSimplified` -> `_callSimplified`, etc., a systematic approach works.
- **`RaiseReturnsValue` is NOT renamed:** This boolean describes whether an event delegate returns a value, not the `Returns()` API. It stays as-is.
- **Generated code is not in git:** Changes to renderers affect generated output. Tests will verify correctness.
- **No public API changes:** Users don't interact with generated private fields or internal model properties directly. The rename is invisible to consumers.

---

## Architectural Verification

**Scope Table:**

This is a purely internal naming cleanup. All 9 patterns are affected because the shared renderers (MethodInterceptorRenderer, PropertyInterceptorRenderer, IndexerInterceptorRenderer) generate code for all patterns.

| Pattern | Affected | Reason |
|---------|----------|--------|
| Standalone | Yes | Uses FlatRenderer + shared renderers |
| Generic Standalone | Yes | Uses FlatRenderer + shared renderers |
| Standalone Class | Yes | Uses StandaloneClassRenderer + shared renderers |
| Generic Standalone Class | Yes | Uses StandaloneClassRenderer + shared renderers |
| Inline Interface | Yes | Uses InlineRenderer + shared renderers |
| Inline Class | Yes | Uses InlineRenderer (ClassRenderer) + shared renderers |
| Inline Delegate | Yes | Uses InlineRenderer + ModelAdapters -> shared renderers |
| Open Generic Interface | Yes | Uses InlineRenderer + shared renderers |
| Open Generic Class | Yes | Uses InlineRenderer + shared renderers |

**Design Project Verification:**

No Design.Stubs changes are needed. This is an internal naming cleanup -- users never reference the generated private field names (`_onCall`, `_onGet`, etc.) or the generator model properties. Design.Stubs exercises the public API (`Get()`, `Set()`, `Return()`, `Call()`) which is unchanged.

The verification is: `dotnet build src/Design/Design.Stubs` and `dotnet test src/Design/Design.Tests` must pass after all renames.

**Breaking Changes:** No -- all renames are internal to the generator or internal to generated interceptor classes. No public API surface changes.

**Codebase Analysis:**

Files examined:
- `src/Generator/Model/Flat/FlatMethodModel.cs` -- has `OnCallDelegateType`
- `src/Generator/Model/Shared/UnifiedMethodInterceptorModel.cs` -- has `OnCallDelegateType` + comments
- `src/Generator/Model/Shared/MethodOverloadSignature.cs` -- comments only
- `src/Generator/Model/Inline/InlineInterfaceImplementation.cs` -- has `OnCallArgs`
- `src/Generator/Model/Inline/InlineClassStubModel.cs` -- has `OnCallArgumentList` + comment
- `src/Generator/Model/Inline/InlineDelegateStubModel.cs` -- has `OnCallType` + comment
- `src/Generator/Model/Inline/InlineMethodModel.cs` -- comment only
- `src/Generator/Model/Flat/FlatGenericMethodHandlerGroup.cs` -- comment only
- `src/Generator/Model/Flat/FlatMethodGroup.cs` -- comment only
- `src/Generator/Model/Flat/FlatEventModel.cs` -- `RaiseReturnsValue` stays (not related)
- `src/Generator/Builder/UnifiedInterceptorBuilder.cs` -- has `BuildOnCallDelegateType`, local vars, named args
- `src/Generator/Builder/FlatModelBuilder.cs` -- local vars, named args
- `src/Generator/Builder/InlineModelBuilder.cs` -- named args, comments
- `src/Generator/Builder/ClassModelBuilder.cs` -- local vars, named args, comment
- `src/Generator/Builder/StandaloneClassModelBuilder.cs` -- local vars, named args, comment
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` -- extensive `_onCall*`, `_returnsValue*` in generated strings
- `src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs` -- `_onGet*`, `_onSet*` in generated strings
- `src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs` -- `_onGet*`, `_onSet*` in generated strings
- `src/Generator/Renderer/Shared/ModelAdapters.cs` -- `.OnCallDelegateType`, `.OnCallType`, local vars
- `src/Generator/Renderer/FlatRenderer.cs` -- `OnGet`/`OnSet` public property names, `_onCall` in generic handlers, comments
- `src/Generator/Renderer/InlineRenderer.cs` -- `OnGet`/`OnSet` public property names, `_onGet`/`_onSet` in indexers, `_onCall` in generic handlers, comments
- `src/Generator/Renderer/ClassRenderer.cs` -- clean (uses shared renderers)
- `src/Generator/Renderer/StandaloneClassRenderer.cs` -- clean (uses shared renderers)
- `src/KnockOff/StubException.cs` -- "Configure OnCall before invoking."

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-02-07

### Why This Plan Is Exceptionally Clear

This plan is a mechanical find-and-replace rename with no behavioral changes. The compiler will catch all code mismatches (model property renames cascade errors through builders and renderers). The rename tables are verified correct against the actual codebase. No public API surface changes, no new features, no edge cases. The risk profile is the lowest possible for a source generator change.

### Review Summary

- Files examined: 20+ source files across models, builders, renderers, adapters, library, tests, and design projects
- Questions checked: All applicable items from the review checklist
- Devil's advocate items: 4 generated, all addressed below

### Codebase Investigation

**Files Examined:**
- `src/Generator/Model/Flat/FlatMethodModel.cs` -- Confirmed `OnCallDelegateType` at line 24
- `src/Generator/Model/Shared/UnifiedMethodInterceptorModel.cs` -- Confirmed `OnCallDelegateType` at line 39
- `src/Generator/Model/Inline/InlineInterfaceImplementation.cs` -- Confirmed `OnCallArgs` at line 40
- `src/Generator/Model/Inline/InlineClassStubModel.cs` -- Confirmed `OnCallArgumentList` at line 261
- `src/Generator/Model/Inline/InlineDelegateStubModel.cs` -- Confirmed `OnCallType` at line 36
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` -- Confirmed `_onCall*`, `_returnsValue*` patterns
- `src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs` -- Confirmed `_onGet*`/`_onSet*` (27 occurrences)
- `src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs` -- Confirmed `_onGet*`/`_onSet*` (18 occurrences)
- `src/Generator/Renderer/FlatRenderer.cs` -- Confirmed `OnGet`/`OnSet` public properties, `_onCall` in generic handlers
- `src/Generator/Renderer/InlineRenderer.cs` -- Confirmed `OnGet`/`OnSet` properties, `_onGet`/`_onSet` backing fields
- `src/Generator/Renderer/Shared/ModelAdapters.cs` -- Confirmed `OnCallDelegateType`, `OnCallType` references
- `src/Generator/Builder/UnifiedInterceptorBuilder.cs` -- Confirmed `BuildOnCallDelegateType`, local vars
- `src/Generator/Builder/FlatModelBuilder.cs` -- Confirmed `onCallDelegateType` local vars
- `src/Generator/Builder/InlineModelBuilder.cs` -- Confirmed named args, `onCallType` local var at lines 1123-1132
- `src/Generator/Builder/ClassModelBuilder.cs` -- Confirmed `onCallArgs` local var
- `src/Generator/Builder/StandaloneClassModelBuilder.cs` -- Confirmed `onCallArgs` local var
- `src/KnockOff/StubException.cs` -- Confirmed `"Configure OnCall before invoking."` at line 31

**Searches Performed:**
- `_onCall|_onGet|_onSet` across `src/Generator` -- 5 renderer files (matches plan)
- `OnCallDelegateType|OnCallArgs|OnCallType|BuildOnCallDelegateType` across `src/Generator` -- 13 files (matches plan)
- `_onCall|_onGet|_onSet|_returnsValue` across all `src/` -- found 3 additional files outside generator (comment-only)
- `.OnGet =|.OnSet =` across all `src/` -- 0 matches (no user code accesses generated properties)
- `RaiseReturnsValue` across `src/Generator` -- 13 occurrences, all event-related, correctly excluded

**Design.Stubs Verification:**
N/A -- internal naming cleanup with no Design.Stubs code changes needed. Architect correctly specifies verification as `dotnet build src/Design/Design.Stubs` + `dotnet test src/Design/Design.Tests` post-rename.

**Discrepancies Found (minor, non-blocking):**

1. **Missing local variable in InlineModelBuilder.cs**: Plan's Category 4 lists `onCallType` local var only for `ModelAdapters.cs` (lines 335, 349) but it also exists in `InlineModelBuilder.cs` (lines 1123, 1126, 1132). The compiler will catch this since the `OnCallType:` named arg rename at line 1172 is listed and the local feeds into it.

2. **Missing local variables in MethodInterceptorRenderer.cs**: `onCallFieldName` and `onCallTrackingFieldName` at lines 1463-1464 construct strings `"_onCall"` and `"_onCallTracking"`. Both the string values and local variable names should be renamed. The string values are covered by Phase 3 step 1's `_onCall` -> `_call` rename, but the local variable names (`onCallFieldName` -> `callFieldName`, `onCallTrackingFieldName` -> `callTrackingFieldName`) are not explicitly listed.

3. **Comment references in test/design files not listed**: Three files outside the generator have comments referencing old names:
   - `src/Tests/KnockOffTests/InlineStubTests.cs` lines 421, 482 -- `_onCall` in comments
   - `src/Design/Design.Tests/AdvancedTests/WhenChainVerificationBugTests.cs` -- `_onCallTracking`, `_returnsValue`, `_returnsValueTracking`, `_onCall` in comments
   - `src/Design/Design.Stubs/Advanced/DelegateStubs.cs` lines 70, 93, 146 -- `_onCall` in comments
   - `src/Design/Design.Tests/AdvancedTests/WhenChainVerificationBugTests.cs` lines 342, 396, 434 -- `"Configure OnCall"` in comments

4. **Comment at FlatModelBuilder.cs line 801**: `// OnCall delegate` not listed in Phase 5.

**Assessment:** All discrepancies are minor. Items 1-2 will be caught by the compiler or naturally found during rename. Items 3-4 are cosmetic comment updates that won't break anything if missed but should be included for thoroughness. The implementer should grep broadly after completing the plan phases.

### Devil's Advocate Analysis

**Edge cases NOT explicitly covered:**
1. The `onCallType` local variable in `InlineModelBuilder.cs` -- addressed above, compiler catches it
2. Comment-only references in test files -- non-breaking, cosmetic
3. String interpolation within generated code (e.g., `$"_onCall_{signatureSuffix}"`) -- verified all are plain string concatenation or interpolation, no hidden patterns

**Ways this could break existing functionality:**
1. If `OnGet`/`OnSet` generated public properties are accessed directly by any code -- verified with grep: no code accesses them directly

**Ways users could misunderstand the API:**
1. N/A -- no user-facing changes. Internal only.

---

## Implementation Contract

**Created:** 2026-02-07
**Approved by:** knockoff-developer

### Design Project Acceptance Criteria

N/A -- No design project code changes needed. Verification is build + test pass.

### In Scope

**Phase 1: Model Property Renames (5 files)**
- [ ] `src/Generator/Model/Flat/FlatMethodModel.cs`: `OnCallDelegateType` -> `CallDelegateType`
- [ ] `src/Generator/Model/Shared/UnifiedMethodInterceptorModel.cs`: `OnCallDelegateType` -> `CallDelegateType` + update comments
- [ ] `src/Generator/Model/Inline/InlineInterfaceImplementation.cs`: `OnCallArgs` -> `CallArgs`
- [ ] `src/Generator/Model/Inline/InlineClassStubModel.cs`: `OnCallArgumentList` -> `CallArgumentList` + update comments
- [ ] `src/Generator/Model/Inline/InlineDelegateStubModel.cs`: `OnCallType` -> `CallType` + update comment
- [ ] **Checkpoint:** `dotnet build src/Generator/Generator.csproj` expected to FAIL (cascading errors confirm all references)

**Phase 2: Builder Method and Variable Renames (5 files)**
- [ ] `src/Generator/Builder/UnifiedInterceptorBuilder.cs`: `BuildOnCallDelegateType` -> `BuildCallDelegateType`, local vars, named args, comment
- [ ] `src/Generator/Builder/FlatModelBuilder.cs`: local vars `onCallDelegateType` -> `callDelegateType`, comment at line 801
- [ ] `src/Generator/Builder/InlineModelBuilder.cs`: named args, local var `onCallType` -> `callType` (lines 1123-1132), comment at line 1122
- [ ] `src/Generator/Builder/ClassModelBuilder.cs`: local vars `onCallArgs` -> `callArgs`, named args
- [ ] `src/Generator/Builder/StandaloneClassModelBuilder.cs`: local vars `onCallArgs` -> `callArgs`, named args
- [ ] **Checkpoint:** `dotnet build src/Generator/Generator.csproj` passes

**Phase 3: Renderer Renames - Generated Code Strings (5 files)**
- [ ] `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs`: All `_onCall*` -> `_call*`, `_returnsValue*` -> `_returnValue*`, local vars `onCallFieldName` -> `callFieldName`, `onCallTrackingFieldName` -> `callTrackingFieldName`
- [ ] `src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs`: All `_onGet*` -> `_get*`, `_onSet*` -> `_set*`
- [ ] `src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs`: All `_onGet*` -> `_get*`, `_onSet*` -> `_set*`
- [ ] `src/Generator/Renderer/FlatRenderer.cs`: Property/indexer `OnGet` -> `Get`, `OnSet` -> `Set`, `_onCall` -> `_call`, local vars `onCallCallback` -> `callCallback`, `onCallArgs` -> `callArgs`, update comments
- [ ] `src/Generator/Renderer/InlineRenderer.cs`: Property/indexer `OnGet` -> `Get`, `OnSet` -> `Set`, `_onGet` -> `_get`, `_onSet` -> `_set`, `_onCall` -> `_call`, local var `onCallCallback` -> `callCallback`, update comments

**Phase 4: Adapter and Library Code (2 files)**
- [ ] `src/Generator/Renderer/Shared/ModelAdapters.cs`: `.OnCallDelegateType` -> `.CallDelegateType`, `.OnCallType` -> `.CallType`, local var `onCallType` -> `callType`, named args
- [ ] `src/KnockOff/StubException.cs`: `"Configure OnCall before invoking."` -> `"Configure the method before invoking."`
- [ ] **Checkpoint:** `dotnet build src/KnockOff.sln` passes

**Phase 5: Comment-Only Updates (9+ files)**
- [ ] Generator model/builder xml-doc comments (9 files listed in plan)
- [ ] `src/Tests/KnockOffTests/InlineStubTests.cs`: Update `_onCall` references in comments (lines 421, 482)
- [ ] `src/Design/Design.Tests/AdvancedTests/WhenChainVerificationBugTests.cs`: Update `_onCallTracking`, `_returnsValue*`, `Configure OnCall` references in comments
- [ ] `src/Design/Design.Stubs/Advanced/DelegateStubs.cs`: Update `_onCall` references in comments (lines 70, 93, 146)

**Phase 6: Build and Test**
- [ ] `dotnet build src/KnockOff.sln`
- [ ] `dotnet test src/KnockOff.sln`
- [ ] `dotnet build src/Design/Design.Stubs`
- [ ] `dotnet test src/Design/Design.Tests`
- [ ] Final grep sweep: `OnCall|_onCall|_onGet|_onSet|OnGet|OnSet|_returnsValue|_hasReturnsValue` across `src/Generator` should return 0 results (excluding `RaiseReturnsValue`)

### Out of Scope

- `RaiseReturnsValue` in event models -- describes event delegate return type, not the `Returns()` API
- Any public API changes -- already completed in PRs #54 and #55
- Domain method names like `Execute()` in user code
- Test method names containing `OnGet`/`OnSet`/`OnCall` -- these are descriptive test names, not API references; renaming them is a separate cleanup
- Renaming `RecordCall` -- this is a distinct concept from the `Call()` API method

### Verification Gates

1. After Phase 1: `dotnet build src/Generator/Generator.csproj` FAILS with cascading errors (expected)
2. After Phase 2: `dotnet build src/Generator/Generator.csproj` passes
3. After Phase 4: `dotnet build src/KnockOff.sln` passes
4. Final: All tests pass, Design projects compile and pass, grep sweep clean

### Stop Conditions

If any occur, STOP and report:
- Out-of-scope test failure
- Unexpected compile errors in test projects (suggests a generated name was public-facing)
- Any test referencing `OnGet`/`OnSet`/`OnCall` as actual API calls (not just test method names)

---

## Implementation Progress

**Started:** 2026-02-07
**Developer:** knockoff-developer

### Phase 1: Model Property Renames (5 files) - COMPLETE

All 5 model files renamed:
- `FlatMethodModel.cs`: `OnCallDelegateType` -> `CallDelegateType`
- `UnifiedMethodInterceptorModel.cs`: `OnCallDelegateType` -> `CallDelegateType` + XML doc
- `InlineInterfaceImplementation.cs`: `OnCallArgs` -> `CallArgs` + XML doc
- `InlineClassStubModel.cs`: `OnCallArgumentList` -> `CallArgumentList` + XML doc
- `InlineDelegateStubModel.cs`: `OnCallType` -> `CallType` + XML doc

Checkpoint: Build failed with 23 expected cascading errors in ModelAdapters.cs, MethodInterceptorRenderer.cs, InlineRenderer.cs.

### Phase 2: Builder Method and Variable Renames (5 files) - COMPLETE

All 5 builder files renamed:
- `UnifiedInterceptorBuilder.cs`: Method `BuildOnCallDelegateType` -> `BuildCallDelegateType`, local vars, named args, comment
- `FlatModelBuilder.cs`: local vars `onCallDelegateType` -> `callDelegateType`, named args, comment
- `InlineModelBuilder.cs`: named args `OnCallArgs:` -> `CallArgs:` (6 locations), `OnCallType:` -> `CallType:`, local var `onCallType` -> `callType`, comment
- `ClassModelBuilder.cs`: local vars `onCallArgs` -> `callArgs`, named arg `OnCallArgumentList:` -> `CallArgumentList:`
- `StandaloneClassModelBuilder.cs`: local vars `onCallArgs` -> `callArgs`, named arg `OnCallArgumentList:` -> `CallArgumentList:`

Also fixed cascading references (pulled forward from Phase 4):
- `ModelAdapters.cs`: `.OnCallDelegateType` -> `.CallDelegateType` (3), `.OnCallType` -> `.CallType` (2), local var, named args
- `MethodInterceptorRenderer.cs`: `.OnCallDelegateType` -> `.CallDelegateType`
- `InlineRenderer.cs`: `.OnCallArgs` -> `.CallArgs` (2)

Checkpoint: `dotnet build src/Generator/Generator.csproj` passed.

### Phase 3: Renderer Renames - Method Interceptor (1 file) - COMPLETE

`MethodInterceptorRenderer.cs` -- all generated string literals renamed (in order, longest-first to prevent partial matches):
- `_onCallSimplifiedVoidTracking` -> `_callSimplifiedVoidTracking`
- `_onCallSimplifiedVoid` -> `_callSimplifiedVoid`
- `_onCallSimplifiedTracking` -> `_callSimplifiedTracking`
- `_onCallSimplified` -> `_callSimplified`
- `_onCallTracking` -> `_callTracking`
- `_onCall` -> `_call`
- `_hasReturnsValue` -> `_hasReturnValue`
- `_returnsValueTracking` -> `_returnValueTracking`
- `_returnsValue` -> `_returnValue`
- `onCallFieldName` -> `callFieldName`
- `onCallTrackingFieldName` -> `callTrackingFieldName`

### Phase 3b: Renderer Renames - Property/Indexer/Flat/Inline (4 files) - COMPLETE

- `PropertyInterceptorRenderer.cs`: `_onGetTracking` -> `_getTracking`, `_onGet` -> `_get`, `_onSetTracking` -> `_setTracking`, `_onSet` -> `_set`
- `IndexerInterceptorRenderer.cs`: Same pattern as PropertyInterceptorRenderer
- `FlatRenderer.cs`: `onCallCallback` -> `callCallback`, `onCallArgs` -> `callArgs`, `_onCall` -> `_call`, `OnGet` -> `Get`, `OnSet` -> `Set`
- `InlineRenderer.cs`: `onCallCallback` -> `callCallback`, `_onCall` -> `_call`, `_onGet` -> `_get`, `_onSet` -> `_set`, `OnGet` -> `Get`, `OnSet` -> `Set`

### Phase 4: Library Code (1 file) - COMPLETE

- `StubException.cs`: `"Configure OnCall before invoking."` -> `"Configure the method before invoking."`

Note: ModelAdapters.cs was handled in Phase 2 to fix cascading errors.

### Phase 5: Comments and XML Docs - COMPLETE

Updated XML doc comments in 9 generator files:
- `UnifiedMethodInterceptorModel.cs`, `MethodOverloadSignature.cs`, `InlineMethodModel.cs`, `InlineClassStubModel.cs`, `FlatGenericMethodHandlerGroup.cs`, `FlatMethodGroup.cs`, `ClassModelBuilder.cs`, `StandaloneClassModelBuilder.cs`, `InlineModelBuilder.cs`

Updated comments in test/design files:
- `InlineStubTests.cs`: 2 comment blocks updated
- `WhenChainVerificationBugTests.cs`: Multiple `_onCallTracking`, `_returnsValue`, `Configure OnCall` references updated
- `DelegateStubs.cs`: 3 comment references updated

### Phase 6: Final Verification - COMPLETE

All builds and tests passed. Grep sweep clean (see Completion Evidence).

---

## Completion Evidence

**Reported:** 2026-02-07

### Build Results

- `dotnet build src/KnockOff.sln` -- 0 errors, 0 warnings
- `dotnet build src/Design/Design.Stubs` -- 0 errors, 0 warnings

### Test Results

**KnockOffTests:**
- net8.0: 1184 passed, 0 failed
- net9.0: 1185 passed, 0 failed
- net10.0: 1185 passed, 0 failed

**Documentation.Samples:**
- net8.0: 571 passed, 0 failed
- net9.0: 571 passed, 0 failed
- net10.0: 571 passed, 0 failed

**NeatooInterfaceTests:**
- net8.0: 473 passed, 0 failed
- net9.0: 473 passed, 0 failed
- net10.0: 473 passed, 0 failed

**AssemblyStrictTests:**
- net8.0: 14 passed, 0 failed
- net9.0: 14 passed, 0 failed
- net10.0: 14 passed, 0 failed

**Design.Tests:**
- net8.0: 259 passed, 0 failed
- net9.0: 259 passed, 0 failed
- net10.0: 259 passed, 0 failed

### Grep Sweep Results

Searched `src/Generator` for: `OnCall|_onCall|_onGet|_onSet|OnGet|OnSet|_returnsValue|_hasReturnsValue`
- Result: **0 matches** (excluding `RaiseReturnsValue` which is correctly preserved)

Searched `src/KnockOff` for: `OnCall|_onCall`
- Result: **0 matches**

Verified preserved exceptions:
- `RaiseReturnsValue`: 13 occurrences across event-related files (correct, out of scope)
- `RecordCall`: Present in builders (correct, out of scope)

### Contract Checklist Summary

- **All Phase 1 items:** Complete (5 model files)
- **All Phase 2 items:** Complete (5 builder files + 3 adapter/renderer fix-ups)
- **All Phase 3 items:** Complete (5 renderer files)
- **All Phase 4 items:** Complete (1 library file; ModelAdapters handled in Phase 2)
- **All Phase 5 items:** Complete (9 generator comment files + 3 test/design comment files)
- **All Phase 6 items:** Complete (builds, tests, grep sweep all clean)
- **Design Projects Compile:** Yes
- **All Contract Items:** Confirmed 100% complete
- **Documentation Updated:** N/A (internal naming only, no user-facing docs affected)

---

## Architect Verification

**Verified:** 2026-02-07
**Verdict:** VERIFIED

### Independent Build Results

- `dotnet build src/KnockOff.sln`: 0 errors, 0 warnings
- `dotnet build src/Design/Design.Stubs`: 0 errors, 0 warnings

### Independent Test Results

| Project | net8.0 | net9.0 | net10.0 |
|---------|--------|--------|---------|
| KnockOffTests | 1184 passed, 0 failed | 1185 passed, 0 failed | 1185 passed, 0 failed |
| Documentation.Samples | 571 passed, 0 failed | 571 passed, 0 failed | 571 passed, 0 failed |
| NeatooInterfaceTests | 473 passed, 0 failed | 473 passed, 0 failed | 473 passed, 0 failed |
| AssemblyStrict | 14 passed, 0 failed | 14 passed, 0 failed | 14 passed, 0 failed |
| Design.Tests | 259 passed, 0 failed | 259 passed, 0 failed | 259 passed, 0 failed |

**Total: Zero failures across all projects and target frameworks.**

### Grep Sweep Results (Independent)

Searched `src/Generator/` and `src/KnockOff/` for all stale patterns listed in the verification instructions:

| Pattern | src/Generator | src/KnockOff |
|---------|---------------|--------------|
| `OnCallDelegateType` | 0 matches | 0 matches |
| `OnCallArgs` | 0 matches | 0 matches |
| `OnCallArgumentList` | 0 matches | 0 matches |
| `OnCallType` | 0 matches | 0 matches |
| `BuildOnCallDelegateType` | 0 matches | 0 matches |
| `_onCall` | 0 matches | 0 matches |
| `_returnsValue` | 0 matches | 0 matches |
| `_hasReturnsValue` | 0 matches | 0 matches |
| `_returnsValueTracking` | 0 matches | 0 matches |
| `_onGet` / `_onSet` / `_onGetTracking` / `_onSetTracking` | 0 matches | 0 matches |
| `"OnGet"` / `"OnSet"` | 0 matches | 0 matches |
| `onCallCallback` / `onCallType` | 0 matches | 0 matches |

### Design Match Spot-Checks

Verified the following rename table entries against actual files:

- `FlatMethodModel.cs` line 24: `CallDelegateType` -- matches plan
- `UnifiedMethodInterceptorModel.cs` line 39: `CallDelegateType` -- matches plan
- `InlineInterfaceImplementation.cs` line 40: `CallArgs` -- matches plan
- `InlineClassStubModel.cs` line 261: `CallArgumentList` -- matches plan
- `InlineDelegateStubModel.cs` line 36: `CallType` -- matches plan
- `UnifiedInterceptorBuilder.cs`: `BuildCallDelegateType` method and `callDelegateType` local var -- matches plan
- `StubException.cs` line 31: `"Configure the method before invoking."` -- matches plan
- `MethodInterceptorRenderer.cs`: `_call`, `_callTracking`, `_callSimplified`, `_callSimplifiedVoid`, `_returnValue`, `_hasReturnValue`, `_returnValueTracking`, `callFieldName`, `callTrackingFieldName` -- all match plan
- `PropertyInterceptorRenderer.cs`: `_get`, `_getTracking`, `_set`, `_setTracking` -- all match plan
- `FlatRenderer.cs`: `callCallback`, `callArgs` local vars -- match plan
- `RaiseReturnsValue`: 13 occurrences across 12 event-related files -- correctly preserved (out of scope)

### Issues Found

None.
