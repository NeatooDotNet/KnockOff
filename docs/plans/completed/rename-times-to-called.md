# Rename `Times` to `Called`

**Date:** 2026-02-07
**Related Todo:** [Times Namespace Collision with Moq](../todos/times-namespace-collision-moq.md)
**Status:** In Progress
**Last Updated:** 2026-02-07

---

## Overview

Rename the `KnockOff.Times` struct to `KnockOff.Called` throughout the entire codebase: library, source generator, tests, design projects, documentation, and skills. This eliminates CS0104 ambiguous reference errors when a test file imports both `KnockOff` and `Moq`.

### Before

```csharp
stub.Method.Verify(Times.Once);       // CS0104 if Moq is also imported
stub.Method.Verify(Times.Exactly(2));
```

### After

```csharp
stub.Method.Verify(Called.Once);       // No collision with Moq.Times
stub.Method.Verify(Called.Exactly(2));
```

---

## Approach

This is a mechanical rename with no design ambiguity. Every occurrence of `Times` as the KnockOff type (not the English word in comments/docs where it means "number of times") must be replaced with `Called`. The internal `TimesKind` enum stays private and has no public API surface, but should also be renamed to `CalledKind` for consistency.

The rename touches six categories of files:
1. **Library** (`src/KnockOff/`) - The `Times` struct definition and all interfaces that reference it
2. **Generator** (`src/Generator/`) - All renderer code that emits `global::KnockOff.Times` in generated C#
3. **Tests** (`src/Tests/`) - All test files that use `Times.*` in assertions
4. **Design** (`src/Design/`) - Design.Stubs and Design.Tests references
5. **Documentation** (`docs/`) - Guides, reference docs, migration guides, release notes
6. **Skills** (`skills/knockoff/`) - Skill file and reference files

---

## Breaking Change Assessment

**This is a breaking API change.** The `Times` struct is part of KnockOff's public API:
- Users write `Times.Once`, `Times.Never`, etc. in test code
- The type appears in interface signatures (`Verify(Times times)`, `Verifiable(Times times)`)
- Generated code references `global::KnockOff.Times`

**Mitigation:** KnockOff is pre-1.0, so breaking changes are expected. Per project convention, this bumps the minor version: 0.40.0 -> 0.41.0.

**Migration for existing users:** Find-and-replace `Times.` with `Called.` in test files (scoped to KnockOff usage). The compiler will catch any missed references because `Times` will no longer exist in the `KnockOff` namespace.

---

## File Inventory

### Category 1: Library (`src/KnockOff/`)

These files define or reference the `Times` type in KnockOff's public API.

| # | File | Change Description |
|---|------|-------------------|
| 1 | `src/KnockOff/Times.cs` | Rename file to `Called.cs`. Rename struct `Times` to `Called`, enum `TimesKind` to `CalledKind`, update all self-references. |
| 2 | `src/KnockOff/IMethodTracking.cs` | Replace `Times` parameter types and XML doc references |
| 3 | `src/KnockOff/IMethodCallBuilder.cs` | Replace `Times` parameter types and XML doc references |
| 4 | `src/KnockOff/IMethodReturnBuilder.cs` | Replace `Times` parameter types and XML doc references |
| 5 | `src/KnockOff/IPropertyTracking.cs` | Replace `Times` parameter types and XML doc references |
| 6 | `src/KnockOff/IPropertyCallBuilder.cs` | Replace `Times` XML doc references |
| 7 | `src/KnockOff/IIndexerTracking.cs` | Replace `Times` parameter types and XML doc references |
| 8 | `src/KnockOff/IIndexerCallBuilder.cs` | Replace `Times` XML doc references |
| 9 | `src/KnockOff/IWhenTracking.cs` | Replace `Times` parameter type on `Verify(Times times)` |
| 10 | `src/KnockOff/VerificationException.cs` | Replace `Times Expected` property type, constructor parameter, and `Times.Exactly()` call |

**Total: 10 files**

### Category 2: Generator (`src/Generator/`)

These files emit `global::KnockOff.Times` in generated C# code. Every string literal containing `KnockOff.Times` must become `KnockOff.Called`. Comments mentioning "Times" in the context of the type should also be updated.

| # | File | Approx. Occurrences |
|---|------|-------------------|
| 1 | `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` | ~30 |
| 2 | `src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs` | ~30 |
| 3 | `src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs` | ~25 |
| 4 | `src/Generator/Renderer/FlatRenderer.cs` | ~45 |
| 5 | `src/Generator/Renderer/InlineRenderer.cs` | ~50 |
| 6 | `src/Generator/Renderer/ClassRenderer.cs` | ~15 |
| 7 | `src/Generator/Renderer/StandaloneClassRenderer.cs` | ~15 |

**Total: 7 files, ~210 string literal replacements**

**Important:** The replacement in generator files is a string literal replacement: `KnockOff.Times` -> `KnockOff.Called` within interpolated strings and string constants. The word "Times" in XML doc summaries within generated code (e.g., "Verifies call count satisfies the Times constraint") should also be updated to "Called".

### Category 3: Tests (`src/Tests/`)

| # | File | Description |
|---|------|-------------|
| 1 | `src/Tests/KnockOffTests/TimesTests.cs` | **Rename file to `CalledTests.cs`**. Rename class. Replace all `Times.` with `Called.` |
| 2 | `src/Tests/KnockOffTests/VerificationTests.cs` | Replace `Times.*` usage |
| 3 | `src/Tests/KnockOffTests/WhenChainTests.cs` | Replace `Times.*` usage |
| 4 | `src/Tests/KnockOffTests/BugRegressionTests.cs` | Replace `Times.*` usage |
| 5 | `src/Tests/KnockOffTests/StrictModeTests.cs` | Replace `Times.*` usage |
| 6 | `src/Tests/KnockOffTests/BasicTests.cs` | Replace `Times.*` usage |
| 7 | `src/Tests/KnockOffTests/InlineStubTests.cs` | Replace `Times.*` usage |
| 8 | `src/Tests/KnockOffTests/StandaloneClassStubTests.cs` | Replace `Times.*` usage |
| 9 | `src/Tests/KnockOffTests/StandaloneClassUserMethodTests.cs` | Replace `Times.*` usage |
| 10 | `src/Tests/KnockOffTests/GenericStandaloneStubTests.cs` | Replace `Times.*` usage |
| 11 | `src/Tests/KnockOffTests/GenericStandaloneClassStubTests.cs` | Replace `Times.*` usage |
| 12 | `src/Tests/KnockOffTests/GenericStandaloneEdgeCaseTests.cs` | Replace `Times.*` usage |
| 13 | `src/Tests/KnockOffTests/OpenGenericInlineStubTests.cs` | Replace `Times.*` usage |
| 14 | `src/Tests/KnockOffTests/IndexerTests.cs` | Replace `Times.*` usage |
| 15 | `src/Tests/KnockOffTests/ClassIndexerVerificationTests.cs` | Replace `Times.*` usage |
| 16 | `src/Tests/KnockOffTests/InlineMultiIndexerTests.cs` | Replace `Times.*` usage |
| 17 | `src/Tests/KnockOffTests/EventTests.cs` | Replace `Times.*` usage |
| 18 | `src/Tests/KnockOffTests/CallbackTests.cs` | Replace `Times.*` usage |
| 19 | `src/Tests/KnockOffTests/AsyncMethodTests.cs` | Replace `Times.*` usage |
| 20 | `src/Tests/KnockOffTests/AsyncCallbackSimplificationTests.cs` | Replace `Times.*` usage |
| 21 | `src/Tests/KnockOffTests/MethodOverloadTests.cs` | Replace `Times.*` usage |
| 22 | `src/Tests/KnockOffTests/MethodValueOverloadTests.cs` | Replace `Times.*` usage |
| 23 | `src/Tests/KnockOffTests/OverloadedMethodTests.cs` | Replace `Times.*` usage |
| 24 | `src/Tests/KnockOffTests/OverloadGroupAsyncCallbackTests.cs` | Replace `Times.*` usage |
| 25 | `src/Tests/KnockOffTests/SequencingTests.cs` | Replace `Times.*` usage |
| 26 | `src/Tests/KnockOffTests/SequenceValueOverloadTests.cs` | Replace `Times.*` usage |
| 27 | `src/Tests/KnockOffTests/ThreePatternValueOverloadTests.cs` | Replace `Times.*` usage |
| 28 | `src/Tests/KnockOffTests/PropertyValueOverloadTests.cs` | Replace `Times.*` usage |
| 29 | `src/Tests/KnockOffTests/DelegateValueOverloadTests.cs` | Replace `Times.*` usage |
| 30 | `src/Tests/KnockOffTests/EdgeCaseValueOverloadTests.cs` | Replace `Times.*` usage |
| 31 | `src/Tests/KnockOffTests/ParamTypeSuffixTests.cs` | Replace `Times.*` usage |
| 32 | `src/Tests/KnockOffTests/RefParameterTests.cs` | Replace `Times.*` usage |
| 33 | `src/Tests/KnockOffTests/OutParameterTests.cs` | Replace `Times.*` usage |
| 34 | `src/Tests/KnockOffTests/GenericMethodTests.cs` | Replace `Times.*` usage |
| 35 | `src/Tests/KnockOffTests/GenericMethodBugTests.cs` | Replace `Times.*` usage |
| 36 | `src/Tests/KnockOffTests/GenericInheritanceTypeMismatchBugTests.cs` | Replace `Times.*` usage |
| 37 | `src/Tests/KnockOffTests/ReturnTypeMismatchBugTests.cs` | Replace `Times.*` usage |
| 38 | `src/Tests/KnockOffTests/UserMethodWhenTests.cs` | Replace `Times.*` usage |
| 39 | `src/Tests/KnockOffTests/UserMethodOnCallTests.cs` | Replace `Times.*` usage |
| 40 | `src/Tests/KnockOffTests/UserMethodCustomTypeDetectionTests.cs` | Replace `Times.*` usage |
| 41 | `src/Tests/KnockOffTests/UserMethodVerificationTests.cs` | Replace `Times.*` usage |
| 42 | `src/Tests/KnockOffTests/BuilderElevationTests.cs` | Replace `Times.*` usage |
| 43 | `src/Tests/KnockOffTests/KOPropertyCollisionTests.cs` | Replace `Times.*` usage |
| 44 | `src/Tests/KnockOffTests/BclInterfaceTests.cs` | Replace `Times.*` usage |
| 45 | `src/Tests/KnockOffTests/BclStandaloneTests.cs` | Replace `Times.*` usage |
| 46 | `src/Tests/KnockOffTests/NeatooTests.cs` | Replace `Times.*` usage |
| 47 | `src/Tests/KnockOffTests/ArrayParamOverloadTests.cs` | Replace `Times.*` usage |
| 48 | `src/Tests/KnockOffTests/BaseClassUserMethodTests.cs` | Replace `Times.*` usage |
| 49 | `src/Tests/KnockOffTests/InitPropertyTests.cs` | Replace `Times.*` usage |
| 50 | `src/Tests/KnockOffTests/InterfaceInheritanceTests.cs` | Replace `Times.*` usage |
| 51 | `src/Tests/KnockOff.NeatooInterfaceTests/ValidationRules/IRuleTests.cs` | Replace `Times.*` usage |
| 52 | `src/Tests/KnockOff.NeatooInterfaceTests/ValidationRules/ITriggerPropertyTests.cs` | Replace `Times.*` usage |
| 53 | `src/Tests/KnockOff.NeatooInterfaceTests/ValidationRules/IRuleManagerTests.cs` | Replace `Times.*` usage |
| 54 | `src/Tests/KnockOff.NeatooInterfaceTests/ValidationRules/IRuleMessagesTests.cs` | Replace `Times.*` usage |
| 55 | `src/Tests/KnockOff.NeatooInterfaceTests/ValidationRules/IRuleOfTTests.cs` | Replace `Times.*` usage |
| 56 | `src/Tests/KnockOff.NeatooInterfaceTests/ValidationRules/IRuleMessageTests.cs` | Replace `Times.*` usage |
| 57 | `src/Tests/KnockOff.NeatooInterfaceTests/Properties/IValidatePropertyTests.cs` | Replace `Times.*` usage |
| 58 | `src/Tests/KnockOff.NeatooInterfaceTests/Properties/IEntityPropertyTests.cs` | Replace `Times.*` usage |
| 59 | `src/Tests/KnockOff.NeatooInterfaceTests/Properties/IPropertyInfoTests.cs` | Replace `Times.*` usage |
| 60 | `src/Tests/KnockOff.NeatooInterfaceTests/PropertyManagers/IEntityPropertyManagerTests.cs` | Replace `Times.*` usage |
| 61 | `src/Tests/KnockOff.NeatooInterfaceTests/PropertyManagers/IValidatePropertyManagerTests.cs` | Replace `Times.*` usage |
| 62 | `src/Tests/KnockOff.NeatooInterfaceTests/Notifications/INotifyNeatooPropertyChangedTests.cs` | Replace `Times.*` usage |
| 63 | `src/Tests/KnockOff.NeatooInterfaceTests/Collections/IEntityListBaseTests.cs` | Replace `Times.*` usage |
| 64 | `src/Tests/KnockOff.NeatooInterfaceTests/Collections/IValidateListBaseTests.cs` | Replace `Times.*` usage |
| 65 | `src/Tests/KnockOff.NeatooInterfaceTests/MetaProperties/IValidateMetaPropertiesTests.cs` | Replace `Times.*` usage |
| 66 | `src/Tests/KnockOff.NeatooInterfaceTests/MetaProperties/IEntityMetaPropertiesTests.cs` | Replace `Times.*` usage |
| 67 | `src/Tests/KnockOff.NeatooInterfaceTests/BuiltInRules/IRequiredRuleTests.cs` | Replace `Times.*` usage |
| 68 | `src/Tests/KnockOff.Documentation.Samples/VerificationSamples.cs` | Replace `Times.*` usage |
| 69 | `src/Tests/KnockOff.Documentation.Samples/SkillContentSamples.cs` | Replace `Times.*` usage |
| 70 | `src/Tests/KnockOff.Documentation.Samples/SkillSamples.cs` | Replace `Times.*` usage |
| 71 | `src/Tests/KnockOff.Documentation.Samples/SkillReadmeSamples.cs` | Replace `Times.*` usage |
| 72 | `src/Tests/KnockOff.Documentation.Samples/SkillPatternsSamples.cs` | Replace `Times.*` usage |
| 73 | `src/Tests/KnockOff.Documentation.Samples/MoqMigrationSamples.cs` | Replace `Times.*` usage |
| 74 | `src/Tests/KnockOff.Documentation.Samples/NSubstituteMigrationSamples.cs` | Replace `Times.*` usage |
| 75 | `src/Tests/KnockOff.Documentation.Samples/ReadmeComparisonSamples.cs` | Replace `Times.*` usage |
| 76 | `src/Tests/KnockOff.Documentation.Samples/ReadMeUseCase.cs` | Replace `Times.*` usage |
| 77 | `src/Tests/KnockOff.Documentation.Samples/WhenApiSamples.cs` | Replace `Times.*` usage |
| 78 | `src/Tests/KnockOff.Documentation.Samples/UserMethodsSamples.cs` | Replace `Times.*` usage |
| 79 | `src/Tests/KnockOff.Documentation.Samples/TroubleshootingSamples.cs` | Replace `Times.*` usage |
| 80 | `src/Tests/KnockOff.Documentation.Samples/PropertiesSamples.cs` | Replace `Times.*` usage |
| 81 | `src/Tests/KnockOff.Documentation.Samples/PatternsSamples.cs` | Replace `Times.*` usage |
| 82 | `src/Tests/KnockOff.Documentation.Samples/InterceptorApiSamples.cs` | Replace `Times.*` usage |
| 83 | `src/Tests/KnockOff.Documentation.Samples/MethodsSamples.cs` | Replace `Times.*` usage |
| 84 | `src/Tests/KnockOff.Documentation.Samples/GenericMethodsSamples.cs` | Replace `Times.*` usage |
| 85 | `src/Tests/KnockOff.Documentation.Samples/ApiConsistencyMatrixSamples.cs` | Replace `Times.*` usage |
| 86 | `src/Tests/KnockOff.Documentation.Samples/DelegatesSamples.cs` | Replace `Times.*` usage |
| 87 | `src/Tests/KnockOff.Documentation.Samples/IndexersSamples.cs` | Replace `Times.*` usage |
| 88 | `src/Tests/KnockOff.Documentation.Samples/EventsSamples.cs` | Replace `Times.*` usage |
| 89 | `src/Tests/KnockOffSandbox/Program.cs` | Replace `Times.*` usage |
| 90 | `src/Tests/PackageTest/Program.cs` | Replace `Times.*` usage |

**Total: 90 files**

### Category 4: Design Projects (`src/Design/`)

| # | File | Description |
|---|------|-------------|
| 1 | `src/Design/Design.Stubs/Methods/BasicMethods.cs` | Replace `Times.*` usage |
| 2 | `src/Design/Design.Stubs/Methods/MethodOverloads.cs` | Replace `Times.*` usage |
| 3 | `src/Design/Design.Stubs/Methods/WhenMatching.cs` | Replace `Times.*` usage |
| 4 | `src/Design/Design.Stubs/Properties/PropertyBasics.cs` | Replace `Times.*` usage |
| 5 | `src/Design/Design.Stubs/Indexers/IndexerBasics.cs` | Replace `Times.*` usage |
| 6 | `src/Design/Design.Stubs/Events/EventPatterns.cs` | Replace `Times.*` usage |
| 7 | `src/Design/Design.Stubs/Events/EventApiConsistencyVerification.cs` | Replace `Times.*` usage |
| 8 | `src/Design/Design.Stubs/Advanced/Verification.cs` | Replace `Times.*` usage |
| 9 | `src/Design/Design.Stubs/Advanced/DelegateStubs.cs` | Replace `Times.*` usage |
| 10 | `src/Design/Design.Stubs/StubPatterns/AllPatterns.cs` | Replace `Times.*` usage |
| 11 | `src/Design/Design.Stubs/UserMethods/UserMethodBasics.cs` | Replace `Times.*` usage |
| 12 | `src/Design/Design.Stubs/UserProperties/UserPropertyBasics.cs` | Replace `Times.*` usage |
| 13 | `src/Design/Design.Tests/MethodTests/MethodBasicsTests.cs` | Replace `Times.*` usage |
| 14 | `src/Design/Design.Tests/MethodTests/MethodOverloadTests.cs` | Replace `Times.*` usage |
| 15 | `src/Design/Design.Tests/PropertyTests/PropertyBasicsTests.cs` | Replace `Times.*` usage |
| 16 | `src/Design/Design.Tests/IndexerTests/IndexerBasicsTests.cs` | Replace `Times.*` usage |
| 17 | `src/Design/Design.Tests/EventTests/EventBasicsTests.cs` | Replace `Times.*` usage |
| 18 | `src/Design/Design.Tests/AdvancedTests/VerificationTests.cs` | Replace `Times.*` usage |
| 19 | `src/Design/Design.Tests/AdvancedTests/DelegateStubTests.cs` | Replace `Times.*` usage |
| 20 | `src/Design/Design.Tests/UserMethodTests/VoidUserMethodFallbackTests.cs` | Replace `Times.*` usage |
| 21 | `src/Design/Design.Tests/UserPropertyTests/UserPropertyBasicsTests.cs` | Replace `Times.*` usage |
| 22 | `src/Design/Design.Tests/AdvancedTests/WhenChainVerificationBugTests.cs` | Replace `Times.*` usage |
| 23 | `src/Design/Design.Tests/GenericOverloadTests/OpenGenericOverloadTests.cs` | Replace `Times.*` usage |
| 24 | `src/Design/Design.Tests/GenericOverloadTests/GenericStandaloneOverloadTests.cs` | Replace `Times.*` usage |
| 25 | `src/Design/Design.Tests/GenericOverloadTests/InlineClassOverloadTests.cs` | Replace `Times.*` usage |

**Total: 25 files**

### Category 5: Documentation (`docs/`)

Documentation files that reference `Times` as the KnockOff type (not just the English word). Each file must be carefully reviewed -- only KnockOff-specific `Times` references should change, not general English usage like "number of times".

| # | File | Description |
|---|------|-------------|
| 1 | `docs/guides/verification.md` | Replace `Times.*` code samples and type references |
| 2 | `docs/guides/methods.md` | Replace `Times.*` code samples |
| 3 | `docs/guides/properties.md` | Replace `Times.*` code samples |
| 4 | `docs/guides/indexers.md` | Replace `Times.*` code samples |
| 5 | `docs/guides/events.md` | Replace `Times.*` code samples |
| 6 | `docs/guides/delegates.md` | Replace `Times.*` code samples |
| 7 | `docs/guides/generic-methods.md` | Replace `Times.*` code samples |
| 8 | `docs/guides/parameter-matching.md` | Replace `Times.*` code samples |
| 9 | `docs/guides/stub-patterns.md` | Replace `Times.*` code samples |
| 10 | `docs/guides/stub-overrides.md` | Replace `Times.*` code samples |
| 11 | `docs/guides/api-consistency-matrix.md` | Replace `Times` in API tables |
| 12 | `docs/reference/interceptor-api.md` | Replace `Times` parameter references |
| 13 | `docs/migration/from-moq.md` | Replace KnockOff `Times` references (keep `Moq.Times` references as-is) |
| 14 | `docs/migration/from-nsubstitute.md` | Replace `Times.*` code samples |
| 15 | `docs/comparison.md` | Replace `Times.*` code samples |
| 16 | `docs/troubleshooting.md` | Replace `Times.*` code samples |

**Note:** Completed plans and todos in `docs/plans/completed/` and `docs/todos/completed/` should NOT be updated -- they are historical records. Active plans and todos that reference `Times` only need updating if they contain forward-looking API references.

**Total: ~16 active documentation files** (exact count depends on review of which references are KnockOff-specific vs. English usage)

### Category 6: Skills (`skills/knockoff/`)

| # | File | Description |
|---|------|-------------|
| 1 | `skills/knockoff/SKILL.md` | Replace all `Times.*` code samples and the "Times Constraints" section heading |
| 2 | `skills/knockoff/references/api-reference.md` | Replace "Times Constraint Reference" section and all `Times.*` code samples |
| 3 | `skills/knockoff/references/methods.md` | Replace `Times.*` code samples and reference table |
| 4 | `skills/knockoff/references/properties.md` | Replace `Times.*` code samples and API tables |
| 5 | `skills/knockoff/references/patterns.md` | Replace `Times.*` code samples |
| 6 | `skills/knockoff/references/moq-migration.md` | Replace KnockOff `Times` references (keep `Moq.Times` references as-is) |

**Total: 6 files**

### Summary

| Category | Files | Notes |
|----------|-------|-------|
| Library | 10 | Struct definition + 9 interface/exception files |
| Generator | 7 | ~210 string literal replacements in renderer code |
| Tests | 90 | Mechanical `Times.` -> `Called.` replacement |
| Design | 25 | Same mechanical replacement |
| Documentation | ~16 | Requires careful review (English "times" vs. KnockOff `Times`) |
| Skills | 6 | Same as documentation |
| **Total** | **~154** | |

---

## Implementation Steps

### Phase 1: Library Rename (Foundation)

Everything depends on this. Rename the type at its source.

1. Rename `src/KnockOff/Times.cs` to `src/KnockOff/Called.cs`
2. In `Called.cs`:
   - Rename struct `Times` to `Called`
   - Rename enum `TimesKind` to `CalledKind`
   - Update all self-references (constructor, static factory methods, operators, IEquatable)
   - Update XML doc comments
3. Update all 9 interface/exception files in `src/KnockOff/`:
   - Replace `Times` parameter types with `Called`
   - Replace `Times.` references in XML doc comments with `Called.`
4. **Verification gate:** `dotnet build src/KnockOff/KnockOff.csproj` must succeed

### Phase 2: Generator Rename

Update all string literals in renderer code that emit `KnockOff.Times` in generated C#.

1. In all 7 renderer files, replace string literal `KnockOff.Times` with `KnockOff.Called`
2. Update any XML doc comments in generated code that reference "Times" as a type name
3. **Verification gate:** `dotnet build src/Generator/KnockOff.Generator.csproj` must succeed

### Phase 3: Tests Rename

Mechanical replacement across all test files.

1. Rename `TimesTests.cs` to `CalledTests.cs`, rename the class
2. In all 90 test files, replace `Times.` with `Called.` (scoped to KnockOff usage)
3. **Verification gate:** `dotnet build src/KnockOff.sln` and `dotnet test src/KnockOff.sln` must succeed with all tests passing

### Phase 4: Design Projects Rename

1. In all 25 Design files, replace `Times.` with `Called.`
2. **Verification gate:** `dotnet build src/Design/Design.Stubs` and `dotnet test src/Design/Design.Tests` must succeed

### Phase 5: Documentation and Skills Rename

1. Update all ~16 active documentation files
2. Update all 6 skill files
3. **Care required:** In migration docs (`from-moq.md`, `moq-migration.md`), `Moq.Times` references must NOT be changed -- only KnockOff `Times` references
4. Add a new section to migration docs explaining the rename

### Phase 6: Version Bump and Release Notes

1. Bump version in `src/Directory.Build.props`: 0.40.0 -> 0.41.0
2. Update `PackageReleaseNotes` in `Directory.Build.props`
3. Create `docs/release-notes/v0.41.0.md` documenting the breaking change

---

## Acceptance Criteria

1. The type `KnockOff.Times` no longer exists anywhere in the codebase
2. The type `KnockOff.Called` exists with identical API surface (same static members, same behavior)
3. `dotnet build src/KnockOff.sln` succeeds with zero errors and zero warnings
4. `dotnet test src/KnockOff.sln` succeeds with all tests passing
5. `dotnet build src/Design/Design.Stubs` succeeds
6. `dotnet test src/Design/Design.Tests` succeeds with all tests passing
7. No file in `src/` or `skills/` contains `KnockOff.Times` (verified by grep)
8. All `Moq.Times` references in migration docs are preserved unchanged
9. Version bumped to 0.41.0
10. Release notes created

---

## Risks

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Missed `Times` reference causes compile error | Low | Low | Compiler will catch all type references; grep for stragglers |
| Accidentally rename `Moq.Times` in migration docs | Medium | Low | Careful review of migration docs; developer should verify Moq references preserved |
| Documentation "times" (English) vs. `Times` (type) confusion | Medium | Low | Only rename when `Times` appears as a code reference (backticks, code blocks, type names), not as English prose |
| Breaking change surprises downstream users | Low | Medium | Pre-1.0 expectation; release notes document the change clearly |

---

## Architectural Verification

This rename does not affect any architectural patterns. It is a pure mechanical rename of a public type name. All nine patterns use `Times` identically (through `Verify(Times)` and `Verifiable(Times)` on interceptors), so all nine patterns will be affected equally by the rename.

**Scope Table:**

| Pattern | Affected | Notes |
|---------|----------|-------|
| Standalone | Yes | FlatRenderer emits `global::KnockOff.Times` |
| Generic Standalone | Yes | Same FlatRenderer pipeline |
| Standalone Class | Yes | StandaloneClassRenderer emits `global::KnockOff.Times` |
| Generic Standalone Class | Yes | Same StandaloneClassRenderer pipeline |
| Inline Interface | Yes | InlineRenderer emits `global::KnockOff.Times` |
| Inline Class | Yes | Same InlineRenderer pipeline |
| Inline Delegate | Yes | Same InlineRenderer pipeline |
| Open Generic Interface | Yes | Same InlineRenderer pipeline |
| Open Generic Class | Yes | Same InlineRenderer pipeline |

**Design.Stubs Verification:** Not applicable for this plan. This is a mechanical rename -- the compiler is the verification. After Phase 3, all Design.Stubs code will compile with `Called` instead of `Times`, and that IS the verification.

**Breaking Changes:** Yes. `KnockOff.Times` -> `KnockOff.Called`. Requires minor version bump (0.40.0 -> 0.41.0).

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-02-07

### Codebase Investigation

**Files Examined:**
- `src/KnockOff/Times.cs` - Struct with `TimesKind` private enum, 7 static members, `IEquatable<Times>`, operators
- `src/KnockOff/IMethodTracking.cs` - `Verify(Times)` and `Verifiable(Times)` on 3 interfaces
- `src/KnockOff/IMethodCallBuilder.cs` - `Verifiable(Times)` on 3 interfaces
- `src/KnockOff/IMethodReturnBuilder.cs` - `Verifiable(Times)` on 3 interfaces
- `src/KnockOff/IPropertyTracking.cs` - `Verify(Times)` and `Verifiable(Times)` on 2 interfaces
- `src/KnockOff/IPropertyCallBuilder.cs` - `Times.AtLeastOnce` in XML doc only
- `src/KnockOff/IIndexerTracking.cs` - `Verify(Times)` and `Verifiable(Times)` on 2 interfaces
- `src/KnockOff/IIndexerCallBuilder.cs` - `Times.AtLeastOnce` in XML doc only
- `src/KnockOff/IWhenTracking.cs` - `Verify(Times)` on `IVoidWhenChain<TDelegate>`, `Times` in XML doc
- `src/KnockOff/VerificationException.cs` - `VerificationFailure` class with `Times Expected` property, `Times.Exactly()` call
- `src/Tests/KnockOffTests/TimesTests.cs` - Dedicated test class for `Times` struct
- All 7 generator renderer files examined via grep

**Searches Performed:**
- `\bTimes\b` across entire `src/` - found all file matches
- `\bTimes\b` across `docs/` - found 72 files total (mostly completed plans/todos)
- `\bTimes\b` across `skills/` - found 6 files (matches plan)
- `KnockOff\.Times` in generator - 176 occurrences across 7 files (plan said ~210, actual is 176)
- `\bTimes\b` in generator (includes variable names) - 245 occurrences across 7 files
- `\bTimes\b` in README.md - 2 occurrences (lines 107 and 146)
- `\bTimes\b` in `.claude/agents/` - 1 file: `test-coverage-analyzer.md`
- `\bTimesKind\b` across `src/` - only in `src/KnockOff/Times.cs` (confirmed private)
- `_verifiableTimes` in generator - used extensively as emitted field name in generated code
- Active plans referencing `Times`: `design-source-of-truth-plan.md`, `unify-returns-execute-design.md`, `when-with-stub-overrides.md`, `user-method-verifiable-implementation.md`, `parameter-specific-matching-design.md`
- Active todos referencing `Times`: `skill-documentation-gaps.md`, `when-with-stub-overrides.md`

**Design.Stubs Verification:** Not applicable. This is a mechanical rename -- the compiler verifies correctness. The architect's justification for skipping Design.Stubs evidence is reasonable.

**Discrepancies Found:**

1. **README.md missing from inventory.** The `README.md` at the project root contains 2 `Times` references (line 107: `Times.Once` in code sample, line 146: `VerifyGet(Times)` in feature table). This file is not listed in any category.

2. **Generator occurrence count inaccurate.** The plan says "~210 string literal replacements". The actual count of `KnockOff.Times` occurrences (the string literals that need changing) is 176. The broader `\bTimes\b` count is 245, but 69 of those are variable/parameter names like `_verifiableTimes`, `verifiableTimesBody`, `verifiableTimesFieldName`, and `times` (local variables) that are internal implementation details.

3. **Active plans and todos not listed.** Several active (non-completed) plans and todos reference `Times` as a KnockOff API type in forward-looking code samples. The plan says completed docs should not be updated (correct), but does not list the active plans/todos that should be updated:
   - `docs/plans/design-source-of-truth-plan.md` - ~30 references to `Times` in API checklists and file references
   - `docs/plans/unify-returns-execute-design.md` - 7 references to `Verifiable(Times times)` in interface designs
   - `docs/plans/when-with-stub-overrides.md` - `Verify(Times)` in design code
   - `docs/plans/parameter-specific-matching-design.md` - `Verify(Times)` references
   - `docs/plans/user-method-verifiable-implementation.md` - `Verifiable(Times)` and `_verifiableTimes` in generated code samples
   - `docs/todos/skill-documentation-gaps.md` - `Times` references
   - `docs/todos/when-with-stub-overrides.md` - `Times` references

4. **`.claude/agents/test-coverage-analyzer.md` not listed.** This agent file contains `Times` in a coverage checklist description.

5. **Variable name decision not explicit.** The plan mentions renaming `KnockOff.Times` string literals in generators but does not address:
   - **Emitted field name `_verifiableTimes`** in generated code -- this is an internal/private field in generated classes. Should it be renamed to `_verifiableCalled`? The current name makes semantic sense as "the verifiable times constraint" but could be confusing after rename. Recommend leaving as-is since it is private implementation detail and `times` as an English word ("number of times") still makes sense.
   - **Generator source variable names** like `verifiableTimesBody`, `verifiableTimesFieldName`, `verifiableTimesField` in `MethodInterceptorRenderer.cs`, `PropertyInterceptorRenderer.cs`, `IndexerInterceptorRenderer.cs` -- these are internal C# variable names in the generator source, not emitted code. Recommend leaving as-is for the same reason.
   - **Parameter names `times`** in library interfaces (e.g., `Verify(Called times)`) -- the parameter name `times` could be renamed to `called` for consistency, or left as `times` since "times" is valid English for "number of times". Recommend renaming to `called` for API consistency.

### Structured Question Checklist

**Completeness:**
- [x] All nine patterns addressed -- yes, scope table is correct, all use the same `Times` type
- [x] Null/empty/default -- N/A for a rename
- [x] Generic type parameters -- N/A
- [x] Nested types/inherited members -- N/A
- [x] Interaction with existing features -- N/A, pure rename

**Correctness:**
- [x] Generated code examples compile -- yes, the before/after examples are trivially correct
- [x] Consistent with existing patterns -- yes
- [x] Model/builder/renderer responsibilities -- N/A
- [x] Breaking changes migration path -- clear (find-and-replace `Times.` with `Called.`)

**Clarity:**
- [x] Could implement without clarifying questions -- mostly yes, see concerns above
- [x] Ambiguous requirements -- the variable name question (concern #5)
- [x] Edge cases -- the `Moq.Times` preservation in migration docs is called out
- [x] Test strategy -- clear (all existing tests pass after rename)

**Risk:**
- [x] What could go wrong -- accidental Moq.Times rename, missed references
- [x] Existing tests failing -- expected, all tests are in-scope for this change
- [x] Performance -- N/A
- [x] Backward compatibility -- breaking change, documented

### Devil's Advocate Analysis

**Edge cases NOT explicitly covered:**
1. The `TimesTests.ToString()` test asserts string values like `"Once"`, `"Twice"`, `"Exactly(5)"`. These are the output of `Called.ToString()` after rename. The current output does NOT include the word "Times" anywhere -- it outputs the member name only (e.g., "Once", not "Times.Once"). So ToString() output does not need changing. This is correct but not stated in the plan.
2. The `_verifiableTimes` field name appears in generated code as a private field. If a user somehow depends on this internal name (via reflection in tests), renaming would break them. Very unlikely, and the plan correctly does not rename it.
3. The `VerificationFailure.Message` format string is `"{member}: expected {expected}, actual {actual} calls"` where `{expected}` calls `Times.ToString()`. After rename, this would call `Called.ToString()` which produces the same output. No issue.

**Ways this could break existing functionality:**
1. If any test file has a `using Times = SomeOtherType;` alias, the rename would miss it. Extremely unlikely given this is the KnockOff repo.

**Ways users could misunderstand the API:**
1. The parameter name `times` in `Verify(Called times)` reads oddly. "Verify called times" does not parse naturally. Consider `Verify(Called called)` or `Verify(Called constraint)`.

### What Looks Good

- The file inventory for Categories 1-4 (Library, Generator, Tests, Design) is accurate and complete
- The phase ordering is correct -- library first, then generator, then everything else
- The breaking change assessment and migration guidance are clear
- The Moq.Times preservation warning is well-placed
- Scope table correctly identifies all 9 patterns as affected
- The decision to rename `TimesKind` to `CalledKind` for internal consistency is good
- Verification gates at each phase are appropriate

### Why This Plan Is Approved Despite Concerns

The concerns are all additive (missing files to include in the rename) or clarification requests (variable naming decisions), not architectural or correctness issues. The core plan is sound and complete for the critical code paths. The missing files (README, active plans/todos, agent file) are easy to add to the implementation contract. The variable naming question is a style decision that can be resolved in the contract.

### Decisions Made for Implementation Contract

Based on review, the following decisions are incorporated:
- **README.md** added to file inventory (Phase 5)
- **Active plans/todos** added to file inventory (Phase 5) -- only forward-looking API code samples, not historical references
- **`.claude/agents/test-coverage-analyzer.md`** added to file inventory (Phase 5)
- **Emitted `_verifiableTimes` field name** -- leave as-is (private implementation detail, English "times" still meaningful)
- **Generator source variable names** (`verifiableTimesBody`, etc.) -- leave as-is (internal, not emitted)
- **Parameter names `times`** in library interfaces -- rename to `called` for API consistency (e.g., `Verify(Called called)`)
- **Generator `Times` in XML doc comments** within generated code (e.g., "Verifies call count satisfies the Times constraint") -- update to "Called"

---

## Implementation Contract

**Created:** 2026-02-07
**Approved by:** knockoff-developer

### Acceptance Criteria

The compiler is the acceptance test. After all phases, these must hold:
- `dotnet build src/KnockOff.sln` succeeds (zero errors, zero warnings)
- `dotnet test src/KnockOff.sln` passes all tests
- `dotnet build src/Design/Design.Stubs` succeeds
- `dotnet test src/Design/Design.Tests` passes all tests
- `grep -r "KnockOff\.Times" src/ skills/` returns zero matches (excluding `Moq.Times`)
- `grep -r "\bTimes\b" src/KnockOff/` returns zero matches (type fully renamed)

### In Scope

#### Phase 1: Library Rename (10 files)
- [ ] Rename `src/KnockOff/Times.cs` to `src/KnockOff/Called.cs` (git mv)
- [ ] In `Called.cs`: rename struct `Times` -> `Called`, enum `TimesKind` -> `CalledKind`, update all self-references including `IEquatable<Called>`, operators, static members
- [ ] In `Called.cs`: update XML doc comments
- [ ] Update `IMethodTracking.cs` -- replace `Times` type references and parameter names (`times` -> `called`)
- [ ] Update `IMethodCallBuilder.cs` -- replace `Times` type references and parameter names
- [ ] Update `IMethodReturnBuilder.cs` -- replace `Times` type references and parameter names
- [ ] Update `IPropertyTracking.cs` -- replace `Times` type references and parameter names
- [ ] Update `IPropertyCallBuilder.cs` -- replace `Times` in XML doc comments
- [ ] Update `IIndexerTracking.cs` -- replace `Times` type references and parameter names
- [ ] Update `IIndexerCallBuilder.cs` -- replace `Times` in XML doc comments
- [ ] Update `IWhenTracking.cs` -- replace `Times` type reference and parameter name on `IVoidWhenChain<TDelegate>.Verify()`
- [ ] Update `VerificationException.cs` -- replace `Times` type on `VerificationFailure.Expected`, constructor parameters, `Times.Exactly()` call
- [ ] **Checkpoint:** `dotnet build src/KnockOff/KnockOff.csproj` succeeds

#### Phase 2: Generator Rename (7 files)
- [ ] In all 7 renderer files: replace string literal `KnockOff.Times` with `KnockOff.Called` (176 occurrences)
- [ ] In all 7 renderer files: update XML doc comment strings referencing "Times" as a type name to "Called"
- [ ] Leave `_verifiableTimes` emitted field name as-is (private implementation detail)
- [ ] Leave generator source variable names (`verifiableTimesBody`, etc.) as-is
- [ ] **Checkpoint:** `dotnet build src/Generator/KnockOff.Generator.csproj` succeeds

#### Phase 3: Tests Rename (90 files)
- [ ] Rename `TimesTests.cs` to `CalledTests.cs` (git mv), rename class `TimesTests` -> `CalledTests`
- [ ] In all 90 test files: replace `Times.` with `Called.` (KnockOff usage only, preserve `Moq.Times` in MoqMigrationSamples.cs)
- [ ] **Checkpoint:** `dotnet build src/KnockOff.sln` succeeds
- [ ] **Checkpoint:** `dotnet test src/KnockOff.sln` -- all tests pass

#### Phase 4: Design Projects Rename (25 files)
- [ ] In all 25 Design files: replace `Times.` with `Called.`
- [ ] **Checkpoint:** `dotnet build src/Design/Design.Stubs` succeeds
- [ ] **Checkpoint:** `dotnet test src/Design/Design.Tests` -- all tests pass

#### Phase 5: Documentation, Skills, README, Active Plans/Todos, Agents
- [ ] Update `README.md` (2 occurrences: line 107 code sample, line 146 feature table)
- [ ] Update 16 active documentation files (see plan inventory)
- [ ] Update 6 skill files (see plan inventory)
- [ ] In migration docs (`from-moq.md`, `moq-migration.md`): preserve all `Moq.Times` references, only change KnockOff `Times`
- [ ] Add migration note to `from-moq.md` explaining the rename
- [ ] Update active plans with forward-looking API references:
  - `docs/plans/design-source-of-truth-plan.md`
  - `docs/plans/unify-returns-execute-design.md`
  - `docs/plans/when-with-stub-overrides.md`
  - `docs/plans/parameter-specific-matching-design.md`
  - `docs/plans/user-method-verifiable-implementation.md`
- [ ] Update active todos with forward-looking API references:
  - `docs/todos/skill-documentation-gaps.md`
  - `docs/todos/when-with-stub-overrides.md`
- [ ] Update `.claude/agents/test-coverage-analyzer.md` (1 occurrence)
- [ ] **DO NOT** update completed plans/todos in `docs/plans/completed/` or `docs/todos/completed/`
- [ ] **DO NOT** update historical release notes in `docs/release-notes/`

#### Phase 6: Version Bump and Release Notes
- [ ] Bump version in `src/Directory.Build.props`: 0.40.0 -> 0.41.0
- [ ] Update `PackageReleaseNotes` in `Directory.Build.props`
- [ ] Create `docs/release-notes/v0.41.0.md` documenting the breaking change

#### Final Verification
- [ ] `dotnet build src/KnockOff.sln` -- zero errors, zero warnings
- [ ] `dotnet test src/KnockOff.sln` -- all tests pass
- [ ] `dotnet build src/Design/Design.Stubs` -- succeeds
- [ ] `dotnet test src/Design/Design.Tests` -- all tests pass
- [ ] Grep verification: no `KnockOff.Times` in `src/` or `skills/` (excluding Moq references)
- [ ] Grep verification: no `\bTimes\b` as a type reference in `src/KnockOff/`

### Explicitly Out of Scope

- **Completed plans and todos** in `docs/plans/completed/` and `docs/todos/completed/` -- historical records
- **Historical release notes** in `docs/release-notes/` -- historical records
- **Emitted field name `_verifiableTimes`** -- private implementation detail, "times" is valid English
- **Generator source variable names** (`verifiableTimesBody`, `verifiableTimesFieldName`, `verifiableTimesField`) -- internal, not emitted
- **MEMORY.md** -- the reference to `Times.Once` in the project memory is historical context

### Verification Gates

1. After Phase 1: `dotnet build src/KnockOff/KnockOff.csproj` succeeds
2. After Phase 2: `dotnet build src/Generator/KnockOff.Generator.csproj` succeeds
3. After Phase 3: `dotnet build src/KnockOff.sln` and `dotnet test src/KnockOff.sln` succeed
4. After Phase 4: `dotnet build src/Design/Design.Stubs` and `dotnet test src/Design/Design.Tests` succeed
5. Final: All builds pass, all tests pass, grep verification clean

### Stop Conditions

If any of these occur, STOP and report:
- Out-of-scope test fails (test that was passing before AND is not related to `Times` rename)
- Generator-emitted code does not compile (indicates missed string literal)
- `Moq.Times` reference accidentally modified in migration docs

### Fresh Agent Strategy

Given the mechanical nature and large file count (~154 files), the following agent allocation is recommended:

- **Phases 1-2** (Library + Generator, 17 files): Single agent. These are the foundation -- the library defines the type and the generator emits it. Must be done together.
- **Phase 3** (Tests, 90 files): Fresh agent. Mechanical `Times.` -> `Called.` replacement across many files. Independent of Phase 4-5 context. Benefits from clean context window.
- **Phase 4** (Design, 25 files): Can be done by the Phase 3 agent or a fresh agent. Same mechanical replacement.
- **Phase 5** (Docs/Skills/README/Plans/Todos/Agents, ~30 files): Fresh agent recommended. Requires careful judgment about English "times" vs. KnockOff `Times`, especially in migration docs. Different skill set from mechanical code replacement.
- **Phase 6** (Version bump, 2 files): Any agent, trivial.

---

## Implementation Progress

**Started:** 2026-02-07

### Phase 1: Library Rename -- COMPLETE

- [x] Renamed `src/KnockOff/Times.cs` to `src/KnockOff/Called.cs` (git mv)
- [x] In `Called.cs`: renamed struct `Times` -> `Called`, enum `TimesKind` -> `CalledKind`, updated all self-references
- [x] Updated XML doc comments in `Called.cs`
- [x] Updated `IMethodTracking.cs` -- replaced `Times` type references and parameter names (`times` -> `called`)
- [x] Updated `IMethodCallBuilder.cs` -- replaced `Times` type references and parameter names
- [x] Updated `IMethodReturnBuilder.cs` -- replaced `Times` type references and parameter names
- [x] Updated `IPropertyTracking.cs` -- replaced `Times` type references and parameter names
- [x] Updated `IPropertyCallBuilder.cs` -- replaced `Times` in XML doc comments
- [x] Updated `IIndexerTracking.cs` -- replaced `Times` type references and parameter names
- [x] Updated `IIndexerCallBuilder.cs` -- replaced `Times` in XML doc comments
- [x] Updated `IWhenTracking.cs` -- replaced `Times` type reference and parameter name
- [x] Updated `VerificationException.cs` -- replaced `Times` type on `VerificationFailure.Expected`, constructor parameters, `Called.Exactly()` call
- [x] **Verification gate:** `dotnet build src/KnockOff/KnockOff.csproj` -- PASSED (0 warnings, 0 errors, all 3 TFMs)
- [x] Grep verification: zero `\bTimes\b` matches in `src/KnockOff/`

### Phase 2: Generator Rename -- COMPLETE

- [x] Replaced all `KnockOff.Times` string literals with `KnockOff.Called` in all 7 renderer files
- [x] Replaced all "Times constraint" XML doc comment strings with "Called constraint" in generated code (65 occurrences across 7 files)
- [x] Left `_verifiableTimes` emitted field name as-is (private implementation detail, per contract)
- [x] Left generator source variable names (`verifiableTimesBody`, `verifiableTimesFieldName`, `verifiableTimesField`) as-is (per contract)
- [x] **Verification gate:** `dotnet build src/Generator/Generator.csproj` -- PASSED (0 warnings, 0 errors)
- [x] **Verification gate:** `dotnet build src/KnockOff/KnockOff.csproj` -- PASSED (0 warnings, 0 errors, all 3 TFMs)
- [x] Grep verification: zero `KnockOff.Times` matches in `src/Generator/`

### Phases 3-6: Pending (Fresh agents)

---

## Completion Evidence

_To be filled after implementation._
