# Rename OnGet/OnSet to Get/Set - Design Plan

**Date:** 2026-02-07
**Related Todo:** [Rename OnGet/OnSet to Get/Set](../todos/rename-onget-onset-to-get-set.md)
**Status:** Complete
**Last Updated:** 2026-02-07

---

## Overview

Rename all `OnGet`/`OnSet` property and indexer configuration methods to `Get`/`Set`, removing the "On" prefix to align with the method API that already uses `Returns`/`Call` (no "On" prefix). This is a breaking public API change affecting the generated interceptor code, the library interfaces (doc comments only), Design projects, tests, documentation, and skills.

---

## Open Questions - Architect Answers

### Q1: Should GetSequence/SetSequence entry points be removed?

**Answer: There are no `OnGetSequence`/`OnSetSequence` methods to remove.** This is a non-issue.

After thorough codebase analysis, `OnGetSequence()` and `OnSetSequence()` do not exist as actual methods. They only appear in doc comments on `IPropertySequence.cs` and `IIndexerSequence.cs` (e.g., "Returned by OnGetSequence() to enable ThenGet chaining"). These comments are inaccurate -- sequences are entered via `OnGet().ThenGet()` chaining, which matches the method API pattern of `Returns().ThenReturns()`.

**Action:** Update the doc comments to say "Returned by Get() and ThenGet() chaining" instead of "Returned by OnGetSequence()". No entry points need removal.

### Q2: Are there other On-prefixed APIs that should be renamed?

**Answer: Yes, but they belong to separate todos.**

The following `On`-prefixed APIs exist:

1. **`OnCall` on generic method typed handlers** (FlatRenderer.cs line 1201, InlineRenderer.cs line 818) -- These are part of the generic method / event handler subsystem. They use `OnCall` because the typed handler pattern was never migrated to `Returns`/`Call`. This is explicitly covered by the `migrate-execute-to-call.md` todo and is OUT OF SCOPE for this plan.

2. **`OnGet`/`OnSet` as public properties** in dead code (FlatRenderer.cs lines 671/685, InlineRenderer.cs lines 304/316) -- These are the OLD property interceptor renderers (`RenderPropertyInterceptorClass` in FlatRenderer and InlineRenderer). These methods are **never called** -- all renderers now use the shared `PropertyInterceptorRenderer` and `IndexerInterceptorRenderer`. This dead code should be cleaned up but is OUT OF SCOPE for this rename.

3. **`HasOnGet`/`HasOnSet` internal properties** on generated interceptors (PropertyInterceptorRenderer.cs lines 322/358) -- These are internal properties used by the user override pattern. They should be renamed to `HasGet`/`HasSet` as part of this plan since they appear in generated code.

**Summary:** Only property/indexer `OnGet`/`OnSet` methods and `HasOnGet`/`HasOnSet` are in scope. Generic handler `OnCall` is a separate todo.

### Q3: Should internal generated field/method names change?

**Answer: Only the `HasOnGet`/`HasOnSet` internal properties should change. Internal fields like `_onGet`/`_onSet` should NOT change.**

Reasoning:
- `_onGet`, `_onSet`, `_onGetTracking`, `_onSetTracking` are **private fields** in the generated interceptor class. Users never see or interact with them. Renaming them adds scope with zero user benefit.
- `InvokeGet()`/`InvokeSet()` -- These are **internal methods** named for what they do (invoke the getter/setter), not for the public API method. They should stay as-is.
- `InvokeGetCallback()`/`InvokeSetCallback()` -- Same reasoning. Stay as-is.
- `HasOnGet`/`HasOnSet` -- These ARE visible in generated code (used in comments and by the user override pattern). Rename to `HasGet`/`HasSet` for consistency.

### Q4: Do VerifyGet/VerifySet need changes?

**Answer: No.** `VerifyGet()`, `VerifySet()`, `LastSetValue`, `LastGetKey`, `LastSetEntry` -- none of these have an "On" prefix. They stay as-is. Confirmed by examining the library interfaces (`IPropertyTracking.cs`, `IIndexerTracking.cs`, `IPropertyCallBuilder.cs`, `IIndexerCallBuilder.cs`).

---

## Approach

This is a straightforward rename across the source generator and consumer code. The changes are mechanical -- no new functionality, no behavioral changes, no model changes.

### Pipeline Analysis

Per CLAUDE.md's Pipeline Verification Rule, I traced which pipelines are affected:

| Pipeline | Renderer | Has OnGet/OnSet? | Action |
|----------|----------|-------------------|--------|
| Standalone Interface (1,2) | `FlatRenderer` -> `PropertyInterceptorRenderer`, `IndexerInterceptorRenderer` | Yes (methods) | Rename in shared renderer |
| Standalone Class (3,4) | `StandaloneClassRenderer` -> `PropertyInterceptorRenderer`, `IndexerInterceptorRenderer` | Yes (methods + HasOnGet/HasOnSet) | Rename in shared renderer + StandaloneClassRenderer |
| Inline Interface (5) | `InlineRenderer` -> `PropertyInterceptorRenderer`, `IndexerInterceptorRenderer` | Yes (methods) | Rename in shared renderer |
| Inline Class (6) | `ClassRenderer` -> `PropertyInterceptorRenderer`, `IndexerInterceptorRenderer` | Yes (methods) | Rename in shared renderer |
| Inline Delegate (7) | N/A for properties | N/A | No changes needed |
| Open Generic Interface (8) | `InlineRenderer` -> shared renderers | Yes (methods) | Same as inline interface |
| Open Generic Class (9) | `InlineRenderer` -> shared renderers | Yes (methods) | Same as inline class |

**Key insight:** Because all pipelines now delegate to the shared `PropertyInterceptorRenderer` and `IndexerInterceptorRenderer`, the generator changes are concentrated in just 2 files plus `StandaloneClassRenderer` (for `HasOnGet`/`HasOnSet` references) and `FlatRenderer` (for `HasOnGet`/`HasOnSet` references).

---

## Design

### Rename Mapping

**Public API methods (generated code):**

| Current | New |
|---------|-----|
| `stub.Name.OnGet("value")` | `stub.Name.Get("value")` |
| `stub.Name.OnGet(() => value)` | `stub.Name.Get(() => value)` |
| `stub.Name.OnSet(v => ...)` | `stub.Name.Set(v => ...)` |
| `stub.Indexer.OnGet((k) => ...)` | `stub.Indexer.Get((k) => ...)` |
| `stub.Indexer.OnSet((k, v) => ...)` | `stub.Indexer.Set((k, v) => ...)` |

**Internal generated properties (user override pattern):**

| Current | New |
|---------|-----|
| `HasOnGet` | `HasGet` |
| `HasOnSet` | `HasSet` |

**Unchanged (no "On" prefix, or internal-only):**

| Name | Reason |
|------|--------|
| `_onGet`, `_onSet`, `_onGetTracking`, `_onSetTracking` | Private fields, not user-facing |
| `InvokeGet()`, `InvokeSet()` | Named for action, not API |
| `InvokeGetCallback()`, `InvokeSetCallback()` | Named for action, not API |
| `VerifyGet()`, `VerifySet()` | No "On" prefix |
| `ThenGet()`, `ThenSet()` | No "On" prefix |
| `ThenDefault()` | No "On" prefix |
| `LastSetValue`, `LastGetKey`, `LastSetEntry` | No "On" prefix |
| `RecordGet()`, `RecordSet()` | Internal tracking, no "On" prefix |

### Files Requiring Changes

**Generator (src/Generator/):**

1. `Renderer/Shared/PropertyInterceptorRenderer.cs` -- Rename public method `OnGet(` to `Get(`, `OnSet(` to `Set(`, `HasOnGet` to `HasGet`, `HasOnSet` to `HasSet`. Update all doc comments referencing `OnGet`/`OnSet`.
2. `Renderer/Shared/IndexerInterceptorRenderer.cs` -- Same pattern for indexer `OnGet`/`OnSet` methods and doc comments.
3. `Renderer/StandaloneClassRenderer.cs` -- Update `HasOnGet`/`HasOnSet` references in user override pattern.
4. `Renderer/FlatRenderer.cs` -- Update `HasOnGet`/`HasOnSet` references in user override pattern. Also update doc comments in dead code (or leave dead code as-is).

**Library (src/KnockOff/):**

5. `IPropertyCallBuilder.cs` -- Update doc comments ("Returned by OnGet()" -> "Returned by Get()")
6. `IPropertySequence.cs` -- Update doc comments ("Returned by OnGetSequence()" -> "Returned by Get().ThenGet()")
7. `IIndexerCallBuilder.cs` -- Update doc comments ("Returned by OnGet()" -> "Returned by Get()")
8. `IIndexerSequence.cs` -- Update doc comments ("Returned by OnSetSequence()" -> "Returned by Set().ThenSet()")
9. `IPropertyTracking.cs` -- Update doc comments ("Returned by OnGet()" -> "Returned by Get()")
10. `IIndexerTracking.cs` -- Update doc comments ("Returned by OnGet()" -> "Returned by Get()")

**Design (src/Design/):**

11. `Design.Stubs/Properties/PropertyBasics.cs` -- Rename all `OnGet`/`OnSet` calls to `Get`/`Set`, update comments
12. `Design.Stubs/Properties/PropertySequences.cs` -- Same
13. `Design.Stubs/Indexers/IndexerBasics.cs` -- Same
14. `Design.Stubs/Indexers/IndexerSequences.cs` -- Same
15. `Design.Stubs/StubOverrideProperties/StubOverridePropertyBasics.cs` -- Same
16. `Design.Stubs/StubPatterns/AllPatterns.cs` -- Same
17. `Design.Tests/PropertyTests/PropertyBasicsTests.cs` -- Same
18. `Design.Tests/PropertyTests/PropertySequenceTests.cs` -- Same
19. `Design.Tests/IndexerTests/IndexerBasicsTests.cs` -- Same
20. `Design.Tests/IndexerTests/IndexerSequenceTests.cs` -- Same
21. `Design.Tests/StubOverridePropertyTests/StubOverridePropertyBasicsTests.cs` -- Same
22. `Design.Tests/AdvancedTests/VerificationTests.cs` -- Check for OnGet/OnSet
23. `Design.Tests/AdvancedTests/StrictModeTests.cs` -- Check for OnGet/OnSet
24. `Design.Tests/PatternTests/*.cs` -- Check for OnGet/OnSet
25. `Design.Tests/GenericOverloadTests/*.cs` -- Check for OnGet/OnSet

**Tests (src/Tests/):**

26. 421 occurrences of `.OnGet`/`.OnSet` across 56 test files -- all must be renamed

**Benchmarks (src/Benchmarks/):**

27. Benchmark files referencing `OnGet`/`OnSet` -- rename

**Documentation (docs/):**

28. ~319 occurrences across 52 doc files -- rename. Most are in guides, migration docs, release notes, and completed plans/todos.

**Skills (skills/):**

29. ~57 occurrences across 5 skill files -- rename

---

## Implementation Steps

### Phase 1: Generator Changes (Core)
1. Rename `OnGet(` to `Get(` and `OnSet(` to `Set(` in `PropertyInterceptorRenderer.cs`
2. Rename `HasOnGet` to `HasGet` and `HasOnSet` to `HasSet` in `PropertyInterceptorRenderer.cs`
3. Rename `OnGet(` to `Get(` and `OnSet(` to `Set(` in `IndexerInterceptorRenderer.cs`
4. Update `HasOnGet`/`HasOnSet` references in `StandaloneClassRenderer.cs`
5. Update `HasOnGet`/`HasOnSet` references in `FlatRenderer.cs`
6. Update doc comments in all generator files that mention `OnGet`/`OnSet`
7. **Verification gate:** `dotnet build src/Generator/Generator.csproj` succeeds

### Phase 2: Library Interface Doc Comments
1. Update doc comments in `IPropertyCallBuilder.cs`, `IPropertySequence.cs`, `IPropertyTracking.cs`
2. Update doc comments in `IIndexerCallBuilder.cs`, `IIndexerSequence.cs`, `IIndexerTracking.cs`
3. **Verification gate:** `dotnet build src/KnockOff/KnockOff.csproj` succeeds

### Phase 3: Design Projects
1. Rename all `.OnGet(`/`.OnSet(` to `.Get(`/`.Set(` in Design.Stubs files
2. Rename all `.OnGet(`/`.OnSet(` to `.Get(`/`.Set(` in Design.Tests files
3. Update Design file comments referencing OnGet/OnSet
4. **Verification gate:** `dotnet build src/Design/Design.Stubs` and `dotnet test src/Design/Design.Tests` both pass

### Phase 4: Test Projects
1. Rename all `.OnGet(`/`.OnSet(` to `.Get(`/`.Set(` across all test files (56 files, 421 occurrences)
2. Update test comments referencing OnGet/OnSet
3. **Verification gate:** `dotnet test src/KnockOff.sln` -- all tests pass

### Phase 5: Documentation, Skills, and Benchmarks
1. Rename in all docs files (~319 occurrences, 52 files)
2. Rename in skills files (~57 occurrences, 5 files)
3. Rename in benchmark files
4. Run `dotnet mdsnippets` if applicable
5. **Verification gate:** Documentation builds, no broken links

### Phase 6: Version Bump
1. Bump minor version in `Directory.Build.props` (breaking change, pre-1.0 convention)
2. Update `PackageReleaseNotes` in `Directory.Build.props`
3. Create release notes file

---

## Acceptance Criteria

- [ ] All `OnGet(` public method calls in generated code are renamed to `Get(`
- [ ] All `OnSet(` public method calls in generated code are renamed to `Set(`
- [ ] `HasOnGet`/`HasOnSet` internal properties renamed to `HasGet`/`HasSet`
- [ ] All library interface doc comments updated (no mention of `OnGet`/`OnSet`)
- [ ] Design.Stubs builds with new API
- [ ] Design.Tests pass with new API
- [ ] All test projects pass (421 occurrences updated)
- [ ] Documentation updated
- [ ] Skills updated
- [ ] Version bumped
- [ ] No `OnGet` or `OnSet` references remain in active code (completed plans/todos/release notes exempted)

---

## Dependencies

- None. This change is independent and can proceed immediately.
- Note: The `migrate-execute-to-call.md` todo is a parallel rename effort for void method APIs. These two changes are independent but could be combined into a single version bump if done in sequence.

---

## Risks / Considerations

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Missing an OnGet/OnSet reference | Low | Low | Grep-based sweep after implementation, build gate catches generated code |
| Dead code in FlatRenderer/InlineRenderer | Low | Low | Out of scope; dead code uses OnGet/OnSet as property names, not methods. Can be cleaned up separately. |
| Benchmark compilation | Low | Low | Benchmarks use OnGet/OnSet -- must be updated |
| Completed plan/todo references | None | None | Historical documents are exempted from rename |

---

## Architectural Verification

### Scope Table

This rename affects all patterns that have properties or indexers.

| Pattern | Has Properties? | Has Indexers? | Renderer | Status |
|---------|----------------|---------------|----------|--------|
| 1. Standalone Interface | Yes | Yes | Shared `PropertyInterceptorRenderer` + `IndexerInterceptorRenderer` | Affected |
| 2. Generic Standalone Interface | Yes | Yes | Same shared renderers | Affected |
| 3. Standalone Class | Yes | Yes | Same shared renderers + `StandaloneClassRenderer` | Affected |
| 4. Generic Standalone Class | Yes | Yes | Same | Affected |
| 5. Inline Interface | Yes | Yes | Same shared renderers | Affected |
| 6. Inline Class | Yes | Yes | `ClassRenderer` -> shared renderers | Affected |
| 7. Inline Delegate | No | No | N/A | Not affected |
| 8. Open Generic Interface | Yes | Yes | Same shared renderers | Affected |
| 9. Open Generic Class | Yes | Yes | Same shared renderers | Affected |

### Design Project Verification

All Design.Stubs code currently uses `OnGet`/`OnSet`. Once the generator is updated (Phase 1), these will fail to compile. The Design.Stubs files themselves serve as the acceptance criteria -- after renaming calls from `OnGet` to `Get` and `OnSet` to `Set`, they must compile.

**Existing Design.Stubs files that exercise property/indexer APIs:**

- `Design.Stubs/Properties/PropertyBasics.cs` -- Exercises `OnGet(value)`, `OnGet(callback)`, `OnSet(callback)` on inline interface pattern. **Will need: `.OnGet(` -> `.Get(`, `.OnSet(` -> `.Set(`**
- `Design.Stubs/Properties/PropertySequences.cs` -- Exercises `OnGet().ThenGet()`, `OnSet().ThenSet()`. **Will need: `.OnGet(` -> `.Get(`, `.OnSet(` -> `.Set(`**
- `Design.Stubs/Indexers/IndexerBasics.cs` -- Exercises `OnGet(callback)`, `OnSet(callback)` on indexers. **Will need same rename.**
- `Design.Stubs/Indexers/IndexerSequences.cs` -- Exercises `OnGet().ThenGet()`, `OnSet().ThenSet()` on indexers. **Will need same rename.**
- `Design.Stubs/StubOverrideProperties/StubOverridePropertyBasics.cs` -- Exercises user override property pattern with `OnGet`/`OnSet`. **Will need same rename.**
- `Design.Stubs/StubPatterns/AllPatterns.cs` -- Multi-pattern exercise. **Will need same rename.**

**Verification approach:** Since this is a mechanical rename (no new features), the existing Design.Stubs code + renamed API calls IS the acceptance criteria. No new failing code needs to be written -- the rename itself is the test.

**Breaking Changes:** Yes -- this is a breaking public API change. All consumers must update `OnGet`/`OnSet` calls to `Get`/`Set`. Per pre-1.0 convention, bump minor version.

### Codebase Analysis

**Files examined:**

- `src/KnockOff/IPropertyCallBuilder.cs` -- Contains `IPropertyGetBuilder<TValue>` and `IPropertySetBuilder<TValue>`. These are returned by the generated `OnGet()`/`OnSet()` methods. The interface names themselves do NOT contain "On" and stay as-is.
- `src/KnockOff/IPropertySequence.cs` -- Contains `IPropertyGetSequence<TValue>` and `IPropertySetSequence<TValue>`. Doc comments reference "OnGetSequence()" which doesn't exist. Fix comments.
- `src/KnockOff/IPropertyTracking.cs` -- Contains `IPropertyGetTracking` and `IPropertySetTracking<TValue>`. Doc comments reference "OnGet()" and "OnSet()". Fix comments.
- `src/KnockOff/IIndexerCallBuilder.cs` -- Same pattern as property call builder for indexers.
- `src/KnockOff/IIndexerSequence.cs` -- Same pattern as property sequence for indexers.
- `src/KnockOff/IIndexerTracking.cs` -- Same pattern as property tracking for indexers.
- `src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs` -- 1,232 lines. Generates `OnGet(callback)`, `OnGet(value)`, `OnSet(callback)` methods and nested builder/sequence classes. Contains `HasOnGet`/`HasOnSet` for user override pattern. This is the PRIMARY file for changes.
- `src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs` -- 850 lines. Same pattern for indexers. Contains `OnGet(callback)`, `OnSet(callback)`. No value overload (indexers don't have `OnGet(value)`).
- `src/Generator/Renderer/StandaloneClassRenderer.cs` -- References `HasOnGet`/`HasOnSet` in user override rendering (lines 678, 724).
- `src/Generator/Renderer/FlatRenderer.cs` -- References `HasOnGet`/`HasOnSet` in user override rendering (lines 2042, 2058). Also contains dead code with `OnGet`/`OnSet` as public properties (lines 671, 685) -- not called by any pipeline.
- `src/Generator/Renderer/InlineRenderer.cs` -- Contains dead code `RenderPropertyInterceptorClass` (line 284) and `RenderIndexerInterceptorClass` (line 455) with `OnGet`/`OnSet` as public properties -- not called.
- `src/Generator/Renderer/ClassRenderer.cs` -- Delegates to shared renderers. No direct OnGet/OnSet references.
- `src/Design/Design.Stubs/Properties/PropertyBasics.cs` -- 11 occurrences of OnGet/OnSet
- `src/Design/Design.Stubs/Properties/PropertySequences.cs` -- 6 occurrences
- `src/Design/Design.Stubs/Indexers/IndexerBasics.cs` -- 6 occurrences
- `src/Design/Design.Stubs/Indexers/IndexerSequences.cs` -- 5 occurrences
- `src/Design/Design.Tests/` -- Multiple test files with OnGet/OnSet calls
- `src/Tests/` -- 421 occurrences across 56 files
- `docs/` -- 319 occurrences across 52 files
- `skills/` -- 57 occurrences across 5 files
- `src/Benchmarks/` -- OnGet/OnSet calls in benchmark files

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-02-07

### Why This Plan Is Exceptionally Clear

This plan is a mechanical rename with no behavioral changes. The architect performed thorough codebase analysis, correctly identified all pipelines (shared renderers, standalone class renderer, flat renderer), verified the dead code claim (InlineRenderer/FlatRenderer old `RenderPropertyInterceptorClass` methods have no callers), and correctly scoped `HasOnGet`/`HasOnSet` to only PropertyInterceptorRenderer + FlatRenderer + StandaloneClassRenderer (indexers use `IsConfigured`/`InvokeGet`/`InvokeSet` without `HasOnGet`/`HasOnSet`).

### Review Summary

- Files examined: PropertyInterceptorRenderer.cs, IndexerInterceptorRenderer.cs, StandaloneClassRenderer.cs, FlatRenderer.cs, InlineRenderer.cs, ClassRenderer.cs, UnifiedPropertyInterceptorModel.cs, UnifiedIndexerInterceptorModel.cs, IPropertyCallBuilder.cs, IPropertySequence.cs, IPropertyTracking.cs, IIndexerCallBuilder.cs, IIndexerSequence.cs, IIndexerTracking.cs, Design.Stubs/Properties/PropertyBasics.cs, Design.Domain/Entities/IEntity.cs
- Questions checked: 17 of 17
- Devil's advocate items: 4 generated, all minor (see Observations below)

### Observations (Non-Blocking)

1. **Occurrence counts are undercounted.** Plan says ~319 doc occurrences (52 files) and ~57 skill occurrences (5 files). Actual: ~530 doc occurrences (37 non-completed files) and ~150 skill occurrences (5 files). This does not affect implementation since the developer will use grep-based sweeps.

2. **Design.Domain files not listed.** `IEntity.cs`, `ICollection.cs`, `IStubOverridePropertyService.cs`, `IUserMethodService.cs` have OnGet/OnSet in doc comments. Covered implicitly by Phase 3 ("Design project updates") but not enumerated.

3. **Model doc comments not listed.** `UnifiedPropertyInterceptorModel.cs` and `UnifiedIndexerInterceptorModel.cs` have OnGet/OnSet in doc comments (line 10 each). Covered implicitly by Phase 1 step 6 ("Update doc comments in all generator files").

4. **Release notes.** Plan correctly exempts these as historical documents. 74 occurrences across 11 release note files will NOT be renamed.

---

## Implementation Contract

**Created:** 2026-02-07
**Approved by:** knockoff-developer

### Design Project Acceptance Criteria

No new failing code -- this is a mechanical rename. The acceptance criteria is:
1. Rename `.OnGet(` to `.Get(` and `.OnSet(` to `.Set(` in all Design.Stubs and Design.Tests files
2. Rename `OnGet`/`OnSet` in all Design.Domain doc comments
3. `dotnet build src/Design/Design.Stubs` succeeds
4. `dotnet test src/Design/Design.Tests` passes

### In Scope

- [ ] Phase 1: Generator changes
  - [ ] Rename `OnGet(`/`OnSet(` to `Get(`/`Set(` in `PropertyInterceptorRenderer.cs`
  - [ ] Rename `HasOnGet`/`HasOnSet` to `HasGet`/`HasSet` in `PropertyInterceptorRenderer.cs`
  - [ ] Rename `OnGet(`/`OnSet(` to `Get(`/`Set(` in `IndexerInterceptorRenderer.cs`
  - [ ] Update `HasOnGet`/`HasOnSet` references in `StandaloneClassRenderer.cs` (lines 678, 724) and comments (lines 673, 685, 713, 731)
  - [ ] Update `HasOnGet`/`HasOnSet` references in `FlatRenderer.cs` (lines 2042, 2058) and comments (lines 1989, 2028, 2041, 2057)
  - [ ] Update doc comments in PropertyInterceptorRenderer.cs, IndexerInterceptorRenderer.cs, FlatRenderer.cs, StandaloneClassRenderer.cs
  - [ ] Update doc comments in `UnifiedPropertyInterceptorModel.cs` and `UnifiedIndexerInterceptorModel.cs`
  - [ ] **Checkpoint**: `dotnet build src/Generator/Generator.csproj` succeeds
- [ ] Phase 2: Library doc comment updates
  - [ ] Update 6 interface files: IPropertyCallBuilder.cs, IPropertySequence.cs, IPropertyTracking.cs, IIndexerCallBuilder.cs, IIndexerSequence.cs, IIndexerTracking.cs
  - [ ] **Checkpoint**: `dotnet build src/KnockOff/KnockOff.csproj` succeeds
- [ ] Phase 3: Design project updates
  - [ ] Rename in Design.Stubs files (Properties, Indexers, StubOverrideProperties, StubPatterns)
  - [ ] Rename in Design.Tests files (PropertyTests, IndexerTests, StubOverridePropertyTests, PatternTests, GenericOverloadTests, AdvancedTests)
  - [ ] Update Design.Domain doc comments (IEntity.cs, ICollection.cs, IStubOverridePropertyService.cs, IUserMethodService.cs)
  - [ ] **Checkpoint**: `dotnet build src/Design/Design.Stubs` and `dotnet test src/Design/Design.Tests` pass
- [ ] Phase 4: Test project updates
  - [ ] Rename across all test files in src/Tests/ (grep-based sweep)
  - [ ] **Checkpoint**: `dotnet test src/KnockOff.sln` -- all tests pass
- [ ] Phase 5: Documentation, skills, benchmarks
  - [ ] Rename in active docs files (guides, migration, reference, troubleshooting, getting-started, comparison)
  - [ ] Rename in active todos and plans (excluding completed/)
  - [ ] Rename in skills files (5 files)
  - [ ] Rename in benchmark files (4 files)
  - [ ] Do NOT rename in completed/ directories or release-notes/ (historical)
  - [ ] **Checkpoint**: Documentation.Samples project builds
- [ ] Phase 6: Version bump
  - [ ] Bump minor version in Directory.Build.props
  - [ ] Create release notes file

### Explicitly Out of Scope

- Generic method `OnCall` on typed handlers (separate todo: `migrate-execute-to-call.md`)
- Dead code in FlatRenderer (lines 516-903) and InlineRenderer (lines 284-453) -- old property/indexer renderers never called
- Private field names (`_onGet`, `_onSet`, `_onGetTracking`, `_onSetTracking`)
- Internal method names (`InvokeGet`, `InvokeSet`, `InvokeGetCallback`, `InvokeSetCallback`)
- Completed plans/todos in `docs/*/completed/` directories (historical references)
- Release notes in `docs/release-notes/` (historical references)

### Verification Gates

1. After Phase 1: `dotnet build src/Generator/Generator.csproj` succeeds
2. After Phase 2: `dotnet build src/KnockOff/KnockOff.csproj` succeeds
3. After Phase 3: `dotnet build src/Design/Design.Stubs` succeeds and `dotnet test src/Design/Design.Tests` passes
4. After Phase 4: `dotnet test src/KnockOff.sln` -- all tests pass
5. After Phase 5: Documentation.Samples project builds
6. Final: Full grep sweep for remaining `\.OnGet\b` and `\.OnSet\b` in non-exempted code/docs

### Stop Conditions

If any occur, STOP and report:
- Out-of-scope test failure
- OnGet/OnSet reference found in an unexpected location (new pipeline, new file not listed)
- Any behavioral change detected (this should be purely mechanical)

---

## Implementation Progress

**Started:** [date]
**Developer:** [agent name]

**Phase 1:** Generator Changes
- [ ] Rename OnGet/OnSet in PropertyInterceptorRenderer.cs
- [ ] Rename HasOnGet/HasOnSet in PropertyInterceptorRenderer.cs
- [ ] Rename OnGet/OnSet in IndexerInterceptorRenderer.cs
- [ ] Update HasOnGet/HasOnSet references in StandaloneClassRenderer.cs
- [ ] Update HasOnGet/HasOnSet references in FlatRenderer.cs
- [ ] Update doc comments in generator files
- [ ] **Verification**: Generator builds

**Phase 2:** Library Doc Comments
- [ ] Update 6 interface files
- [ ] **Verification**: Library builds

**Phase 3:** Design Projects
- [ ] Rename in Design.Stubs files
- [ ] Rename in Design.Tests files
- [ ] **Verification**: Design builds and tests pass

**Phase 4:** Test Projects
- [ ] Rename across 56 test files (421 occurrences)
- [ ] **Verification**: All tests pass

**Phase 5:** Docs, Skills, Benchmarks
- [ ] Rename in docs (~319 occurrences)
- [ ] Rename in skills (~57 occurrences)
- [ ] Rename in benchmarks
- [ ] **Verification**: Samples build

**Phase 6:** Version Bump
- [ ] Bump version in Directory.Build.props
- [ ] Create release notes

---

## Completion Evidence

[Developer fills this section, then sets status to "Awaiting Verification" and STOPS.]

**Reported:** [date]

- **Tests Passing:** [Output or summary -- report ALL failures, do not classify any as "pre-existing"]
- **Design Projects Compile:** [Yes/No/N/A]
- **All Contract Items:** [Confirmed 100% complete]
- **Documentation Updated:** [Yes/No/N/A]

---

## Architect Verification

[Architect fills this section after independently verifying the developer's work.]

**Verified:** [date]
**Verdict:** VERIFIED | SENT BACK

**Independent test results:**
- Design.Stubs: [Build result]
- Design.Tests: [X passed, Y failed]
- Production code: [Build result]
- Documentation.Samples: [Build result]
- All tests: [X passed, Y failed]

**Design match:** [Does the implementation match the original plan?]

**Issues found:** [List any issues, or "None"]
