# README Rewrite: Fake Repository Value Proposition

**Date:** 2026-03-20
**Related Todo:** [readme-fake-repository-motivation](../todos/readme-fake-repository-motivation.md)
**Status:** Verified
**Last Updated:** 2026-03-20 (requirements verification passed)

---

## Overview

Replace the README's lead narrative from "I wanted to reuse my mocks" to "mocking frameworks are the wrong tool for fakes." Create a new sample demonstrating a richer fake repository pattern (Add/GetById/Delete/GetAll) that better illustrates the split-abstraction problem and KnockOff's solution. Rewrite the opening README sections while preserving the existing comparison tables and feature sections.

---

## Business Requirements Context

**Source:** [Todo Requirements Review](../todos/readme-fake-repository-motivation.md#requirements-review)

### Relevant Existing Requirements

#### Behavioral Contracts

- **Interceptor-as-Property Principle** -- Not affected. No API changes. The sample will use `stub.Method` as a property returning an interceptor object.
- **API Consistency Principle** -- Not affected. The sample uses existing standalone stub features (stub overrides, Return/Call/Verify, When chains).
- **Nine Patterns** -- The sample targets Pattern 1 (Standalone Interface) only. Appropriate because stub overrides require standalone patterns (1-4) and the fake repository pattern specifically relies on standalone stubs owning state.
- **Four Member Types** -- Methods only. Properties, indexers, and events are not relevant to the fake repository demonstration.

#### Existing Samples

- `ReadMeUseCase.cs` (lines 11-49) -- Already has `IMyRepo` (GetUser/Update), `MyRepoManualStub`, and `MyRepoStub(List<User> Users)` with stub overrides. Uses `#region` markers referenced by README snippets.
- `ReusableStubsSamples.cs` (lines 9-73) -- Has `IOrderRepository` (GetOrder/SaveOrder/GetTotal) with `OrderRepoStub(List<Order> orders)`. Different namespace (`ReusableStubs`).
- `ReadmeComparisonSamples.cs` -- Contains comparison examples (Moq/NSubstitute/KnockOff) for methods, properties, events, delegates, indexers. All use `#region` markers referenced by README.
- `ReadmeSamples.cs` -- Contains QuickStart examples and a standalone ReadmeUserRepoStub.

#### MarkdownSnippets

- The README uses `<!-- snippet: region-name -->` / `<!-- endSnippet -->` markers sourced from `#region` / `#endregion` in sample files.
- All C# code in documentation must come from compiled, tested sample code.
- After changes: run `dotnet mdsnippets` to sync.

### Gaps

None. This is a documentation/sample-only change using existing, working generator features.

### Contradictions

None.

### Recommendations for Architect

1. Decide whether to extend `ReadMeUseCase.cs` or create a new file. The reviewer recommends avoiding duplication of the same pattern across multiple files.
2. The richer interface (Add/GetById/Delete/GetAll) should better demonstrate the split-abstraction problem than the current GetUser/Update pair.
3. Primary constructor style (`MyRepoStub(List<T> backing)`) is confirmed working.
4. Plan for `dotnet mdsnippets` verification after implementation.

---

## Business Rules (Testable Assertions)

Since this is a documentation/sample task, "business rules" are the requirements that the sample code must demonstrate and the README must communicate. Each rule maps to a concrete behavior the sample exercises.

1. WHEN a standalone stub is defined with `[KnockOff]`, a `List<T>` primary constructor parameter, and stub override methods, THEN the stub compiles and acts as a complete IRepository implementation backed by the list. -- Source: Existing behavioral contract (ReadMeUseCase.cs lines 36-49)

2. WHEN a stub override method (e.g., `GetById_`) is defined, THEN it is called as fallback behavior when no Return/Call/When is configured on the interceptor. -- Source: Design.Stubs StubOverrideBasics.cs priority chain

3. WHEN `Return()` or `Call()` is configured on an interceptor that has a stub override, THEN the Return/Call configuration takes priority over the stub override for that test. -- Source: Design.Stubs StubOverrideBasics.cs priority chain

4. WHEN a stub's backing list is mutated through the stub override method (e.g., `Add_` appends to the list), THEN the mutation is visible through other stub override methods (e.g., `GetAll_` returns the updated list) because they share the same `List<T>` instance. -- Source: NEW (this is the core demonstration of unified state)

5. WHEN the NSubstitute equivalent is constructed, THEN it requires two separate objects (`List<T>` + `Substitute.For<IRepo>()`) wired together via lambda callbacks, demonstrating the split-abstraction problem. -- Source: NEW (this is the counter-example)

6. WHEN the manual fake class equivalent is constructed, THEN it requires implementing every interface member by hand, demonstrating the boilerplate problem KnockOff eliminates. -- Source: NEW (this is the second counter-example)

7. WHEN the KnockOff stub is constructed, THEN it can be passed directly to any consumer expecting `IRepository` (no `.Object` needed for interface stubs), AND it retains full mock capabilities (Verify, When, Return). -- Source: Existing behavioral contract (ReadMeUseCase.cs tests)

8. WHEN all sample code is wrapped in `#region` / `#endregion` markers, THEN `dotnet mdsnippets` produces matching `<!-- snippet: -->` blocks in README.md. -- Source: MarkdownSnippets integration requirement

### Test Scenarios

| # | Scenario | Inputs / State | Rule(s) | Expected Result |
|---|----------|---------------|---------|-----------------|
| 1 | Stub with backing list provides Add/GetById | Create stub with empty list, Add an entity, then GetById | Rules 1, 4 | GetById returns the entity that was added through the stub |
| 2 | Stub override fallback | Call GetAll on stub with pre-populated list, no Return configured | Rule 2 | Returns all entities from the backing list |
| 3 | Return overrides stub override for one test | Configure Return on GetById, call through interface | Rule 3 | Return value wins over stub override |
| 4 | Verify works on stub with overrides | Call Delete through interface, then Verify | Rule 7 | Verify(Called.Once) passes |
| 5 | NSubstitute comparison compiles and runs | Construct NSubstitute mock wired to a List | Rule 5 | Works but requires two objects + lambda wiring |
| 6 | Manual fake comparison compiles and runs | Hand-written fake class implementing all 4 methods | Rule 6 | Works but requires all members implemented manually |
| 7 | Samples produce valid snippets | Regions wrap all sample code | Rule 8 | `dotnet mdsnippets` succeeds without errors |

---

## Approach

### Decision: Extend ReadMeUseCase.cs vs. New File

**Decision: Replace the content of `ReadMeUseCase.cs`.**

Rationale:
- The existing file already serves as the README's lead sample. Its regions (`readme-knockoff-stub`, `readme-nsub-shared-mock`, etc.) are referenced by the README.
- Creating a separate file would require the README to reference regions from two different files for the same narrative, which is confusing.
- The existing `IMyRepo` (GetUser/Update) is too simple to demonstrate the split-abstraction problem well. Replacing it with a richer interface (Add/GetById/Delete/GetAll) serves the narrative better.
- The existing `UserDomainModel` consumer can be simplified or replaced -- the point of the sample is the repository pattern, not a domain model.

### New Interface Design

The new interface should be rich enough to show the split-abstraction problem clearly but simple enough for a README lead:

```csharp
public interface IEntityRepository<T> where T : class
{
    void Add(T entity);
    T? GetById(int id);
    List<T> GetAll();
    bool Delete(int id);
}
```

Wait -- a generic interface complicates the README lead. Better to use a concrete `IUserRepository` with `User` entities:

```csharp
public interface IUserRepository
{
    void Add(User user);
    User? GetById(int id);
    List<User> GetAll();
    bool Delete(int id);
}
```

Four methods. `Add` and `Delete` are void/bool (side effects on the list). `GetById` and `GetAll` are query methods. This gives enough surface to show:
- The split-abstraction problem (NSubstitute version needs 4 lambda callbacks wired to the same list)
- The manual fake boilerplate (implementing all 4 methods)
- KnockOff's clean solution (stub overrides + backing list)
- Per-test override (Return on GetById for one test)
- Verification (Verify on Add/Delete)

### Existing Snippet Migration

The README currently references these snippets from ReadMeUseCase.cs:
- `readme-manual-stub-interface` -- Will be replaced with new `IUserRepository`
- `readme-manual-stub-desired` -- Will be replaced with new manual fake
- `readme-knockoff-stub` -- Will be replaced with new `UserRepositoryStub`
- `readme-nsub-shared-mock` -- Will be replaced with new NSubstitute example
- `readme-knockoff-fetch-test` -- Will be replaced with new test showing Add + GetById
- `readme-knockoff-oncall-test` -- Will be replaced with new test showing per-test override

All existing region names will be replaced with new ones. The old regions will be removed. The README markdown changes (Step 9) will reference the new region names.

### Type Conflict Resolution

The `ReadMeUseCase.cs` file is in namespace `KnockOff.Documentation.Samples.Readme`. It uses the `User` type from the root `KnockOff.Documentation.Samples` namespace (via `SharedTypes.cs`). The new sample will continue to use the same `User` type from SharedTypes.cs -- no new entity types needed.

However, the new `IUserRepository` interface name could conflict with similar interfaces in other sample files. Current interfaces in the project:
- `IMyRepo` (ReadMeUseCase.cs, Readme namespace) -- being replaced
- `IReadmeUserRepo` (ReadmeSamples.cs, Readme namespace) -- stays, different purpose (QuickStart)
- `IUserRepo` (ReadmeComparisonSamples.cs, CompareComparisons namespace) -- stays, different namespace
- `IOrderRepository` (ReusableStubsSamples.cs, ReusableStubs namespace) -- stays, different namespace

The name `IUserRepository` in the `Readme` namespace is safe -- no conflicts.

---

## Design

### File: `src/Tests/KnockOff.Documentation.Samples/ReadMeUseCase.cs`

Complete rewrite. New contents:

**Namespace:** `KnockOff.Documentation.Samples.Readme` (unchanged)

**Section 1: Interface Definition** (region: `readme-repo-interface`)
```csharp
public interface IUserRepository
{
    void Add(User user);
    User? GetById(int id);
    List<User> GetAll();
    bool Delete(int id);
}
```

**Section 2: The Split-Abstraction Problem -- NSubstitute** (region: `readme-nsub-split-abstraction`)
Show the NSubstitute version requiring `List<User>` + `Substitute.For<IUserRepository>()` as two separate objects wired together via 4 lambda callbacks. This is the "before" that demonstrates the problem.

**Section 3: The Manual Fake Solution** (region: `readme-manual-fake`)
Show a hand-written `ManualUserRepositoryFake` implementing all 4 methods. This works but is boilerplate. Include a comment noting that every new interface member requires manual implementation.

**Section 4: The KnockOff Solution** (region: `readme-knockoff-fake`)
Show the `[KnockOff]` standalone stub with primary constructor `(List<User> users)` and stub overrides for all 4 methods. This is the centerpiece -- same backing store, same behavior, but also a full mock.

**Section 5: Test -- Add and Query** (region: `readme-fake-add-and-query`)
Test that adds users via the interface, then queries them. Demonstrates that the stub owns its state -- mutations through `Add` are visible through `GetAll` and `GetById`.

**Section 6: Test -- Verify Calls** (region: `readme-fake-verify`)
Test that calls Delete and then uses `stub.Delete.Verify(Called.Once)`. Demonstrates that the stub is still a full mock.

**Section 7: Test -- Per-Test Override** (region: `readme-fake-per-test-override`)
Test that uses `stub.GetById.Return(...)` to override the stub's default behavior for a specific test. Demonstrates that Return/Call still works on top of stub overrides.

### Region Names (New)

All new regions use `readme-` prefix for README snippet integration:

| Region Name | Content | README Section |
|---|---|---|
| `readme-repo-interface` | `IUserRepository` interface | Lead narrative |
| `readme-nsub-split-abstraction` | NSubstitute + List wiring | "The Split-Abstraction Problem" |
| `readme-manual-fake` | Hand-written fake class | "The Manual Solution" |
| `readme-knockoff-fake` | KnockOff standalone stub | "The KnockOff Solution" |
| `readme-fake-add-and-query` | Test: add + query | Usage example |
| `readme-fake-verify` | Test: verify calls | Mock capabilities |
| `readme-fake-per-test-override` | Test: Return override | Per-test customization |

### Region Names (Removed)

These regions from the current `ReadMeUseCase.cs` will be removed:

| Region Name | Replaced By |
|---|---|
| `readme-manual-stub-interface` | `readme-repo-interface` |
| `readme-manual-stub-desired` | `readme-manual-fake` |
| `readme-knockoff-stub` | `readme-knockoff-fake` |
| `readme-nsub-shared-mock` | `readme-nsub-split-abstraction` |
| `readme-knockoff-fetch-test` | `readme-fake-add-and-query` |
| `readme-knockoff-oncall-test` | `readme-fake-per-test-override` |

### Classes Removed

These classes from the current `ReadMeUseCase.cs` will be removed:
- `IMyRepo` -- replaced by `IUserRepository`
- `MyRepoManualStub` -- replaced by `ManualUserRepositoryFake`
- `MyRepoStub` -- replaced by `UserRepositoryStub`
- `UserDomainModel` -- removed (unnecessary for the fake repository narrative)
- `UserDomainModelTests` -- replaced by new test class

### README Changes (Step 9 Documentation Deliverable)

The README sections to be rewritten are the opening through "So I Created KnockOff." The sections from "What Sets KnockOff Apart" onward remain unchanged. Specific README changes:

1. **Opening paragraph** -- Replace "reusable stub classes" framing with "fakes vs mocks" framing
2. **"KnockOff Stub" section** -- Replace with "The KnockOff Solution" showing the new stub
3. **"Why I Wrote KnockOff" section** -- Replace with "The Split-Abstraction Problem" showing the NSubstitute example and manual fake
4. **"So I Created KnockOff" section** -- Replace with usage examples (add-and-query, verify, per-test override)
5. **Everything from "What Sets KnockOff Apart" onward** -- Unchanged

The README changes reference new snippet names. Old snippet names are removed from both the sample file and the README.

---

## Implementation Steps

Implementation covers source code changes only. README markdown changes are a Step 9 documentation deliverable.

1. **Rewrite `ReadMeUseCase.cs`** -- Replace the entire file with the new content:
   - New `IUserRepository` interface (4 methods: Add, GetById, GetAll, Delete)
   - NSubstitute split-abstraction example (List + Substitute wired via lambdas)
   - Manual fake class implementing all 4 methods
   - KnockOff `UserRepositoryStub` with primary constructor and stub overrides
   - Three test methods exercising the scenarios
   - All code wrapped in `#region` / `#endregion` markers

2. **Build and test** -- Verify the sample project compiles and all tests pass:
   ```bash
   dotnet build src/Tests/KnockOff.Documentation.Samples/
   dotnet test src/Tests/KnockOff.Documentation.Samples/
   ```

3. **Verify no broken snippet references** -- The old region names are removed. The README still references them until Step 9, but `dotnet mdsnippets` will show warnings/errors for missing snippets. This is expected and acceptable until the README is updated in Step 9.

---

## Acceptance Criteria

- [ ] `ReadMeUseCase.cs` contains the new `IUserRepository` interface with Add/GetById/GetAll/Delete
- [ ] `ReadMeUseCase.cs` contains NSubstitute split-abstraction example demonstrating two-object problem
- [ ] `ReadMeUseCase.cs` contains manual fake class demonstrating boilerplate problem
- [ ] `ReadMeUseCase.cs` contains KnockOff `UserRepositoryStub` with `List<User>` backing store and stub overrides
- [ ] Three test methods: add-and-query, verify, per-test override -- all pass
- [ ] All `#region` markers use `readme-` prefix with kebab-case names
- [ ] `dotnet build src/Tests/KnockOff.Documentation.Samples/` succeeds
- [ ] `dotnet test src/Tests/KnockOff.Documentation.Samples/` -- all tests pass (including unchanged tests in other files)
- [ ] No new types conflict with types in other sample files (namespace isolation)

---

## Dependencies

- NSubstitute package (already referenced by the Documentation.Samples project)
- `User` type from `SharedTypes.cs` (already available in the project)

---

## Risks / Considerations

1. **Old snippet references break README until Step 9** -- After the sample file is rewritten, `dotnet mdsnippets` will report missing snippets for the old region names still referenced in README.md. This is expected. The README will be updated in Step 9 to reference the new region names. The developer should NOT modify README.md during implementation.

2. **Type name collision** -- The new `IUserRepository` name must not conflict with interfaces in other sample files. Verified: no `IUserRepository` exists in any other namespace in the project.

3. **NSubstitute import** -- The existing file already imports NSubstitute. The new file continues to use it for the comparison example.

4. **Removing `UserDomainModel`** -- The existing `ReadMeUseCase.cs` has a `UserDomainModel` class that is not used by any other file. Removing it is safe. Verified: `UserDomainModel` is only referenced within `ReadMeUseCase.cs`.

---

## Architectural Verification

**Scope Table:**

This is a documentation/sample-only change. No generator, library, or API changes.

| Concern | Status | Notes |
|---|---|---|
| Generator changes | None | No pipeline changes |
| Library changes | None | No runtime library changes |
| Pattern coverage | Pattern 1 only | Appropriate for this sample |
| Member types | Methods only | Appropriate for repository pattern |

**Verification Evidence:**

- Standalone stub with primary constructor + List backing store: Already compiling in `ReadMeUseCase.cs` lines 36-49
- Stub overrides with custom type parameters: Fixed 2026-02-05, confirmed by 14 unit tests
- Priority chain (Return > stub override): Confirmed in Design.Stubs/StubOverrides/StubOverrideBasics.cs

**Breaking Changes:** No

**Codebase Analysis:**

Files examined:
- `src/Tests/KnockOff.Documentation.Samples/ReadMeUseCase.cs` -- Current lead sample, being replaced
- `src/Tests/KnockOff.Documentation.Samples/ReadmeSamples.cs` -- QuickStart samples, not affected
- `src/Tests/KnockOff.Documentation.Samples/ReadmeComparisonSamples.cs` -- Comparison tables, not affected
- `src/Tests/KnockOff.Documentation.Samples/ReusableStubsSamples.cs` -- Reusable stubs guide, not affected (different namespace)
- `src/Tests/KnockOff.Documentation.Samples/StubOverrideSamples.cs` -- Stub override guide samples, not affected
- `src/Tests/KnockOff.Documentation.Samples/SharedTypes.cs` -- Shared `User` type, used by new sample
- `README.md` -- Current README structure, sections to be rewritten in Step 9
- `.claude/rules/documentation-samples.md` -- Sample coding standards
- `.claude/rules/documentation-snippets.md` -- MarkdownSnippets integration rules
- `.config/dotnet-tools.json` -- mdsnippets tool version

---

## Agent Phasing

| Phase | Agent Type | Fresh Agent? | Rationale | Dependencies |
|-------|-----------|-------------|-----------|--------------|
| Phase 1: Sample code | developer | Yes | Single file rewrite, small scope | None |

**Parallelizable phases:** None -- single phase.

**Notes:** This is a small, focused change (one file rewrite). A single developer agent phase is sufficient. The README markdown rewrite is a Step 9 documentation deliverable, not an implementation phase.

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-03-20

### Assertion Trace Verification

| Rule # | Implementation Path (method/condition) | Expected Result | Matches Rule? | Notes |
|--------|---------------------------------------|-----------------|---------------|-------|
| 1 | `UserRepositoryStub(List<User> users)` with `[KnockOff]` attribute. Generator produces base class with virtual `Add_`, `GetById_`, `GetAll_`, `Delete_`. User overrides all four. Generated interface implementation calls interceptor which falls back to stub override. | Stub compiles and acts as complete IRepository backed by list | Yes | Same pattern as existing `MyRepoStub(List<User> Users)` at ReadMeUseCase.cs:37-48. Confirmed compiling. |
| 2 | Test calls `GetAll` on stub with pre-populated list, no Return configured. Generated priority chain: When (none) > Sequences (none) > Return/Call (none) > falls to `GetAll_()` stub override. | Returns all entities from backing list | Yes | Priority chain confirmed in StubOverrideBasics.cs:600-605. |
| 3 | Test configures `stub.GetById.Return(specificUser)`. Priority chain: When (none) > Sequences (none) > Return/Call (found) > returns configured value. `GetById_` stub override never called. | Return value wins over stub override | Yes | Confirmed in StubOverrideBasics.cs:197-238. Existing test at ReadMeUseCase.cs:149-167 demonstrates same pattern with `When().Return()`. |
| 4 | `Add_(User user)` calls `users.Add(user)` on the primary constructor's `List<User>`. `GetAll_()` and `GetById_(int id)` read from the same `users` reference. All stub overrides share the same `users` instance. | Mutations visible across stub override methods | Yes | Standard C# reference semantics on `List<T>`. |
| 5 | NSubstitute example creates `Substitute.For<IUserRepository>()` and a separate `List<User>`, then wires each of 4 methods via `.Returns(callInfo => ...)` or `.When(x => ...).Do(...)`. Two separate objects requiring manual wiring. | Two-object split abstraction demonstrated | Yes | Plan describes but does not show full code. Developer writes the wiring. Existing NSubstitute example at ReadMeUseCase.cs:91-104 demonstrates pattern with 2 methods. |
| 6 | `ManualUserRepositoryFake(List<User> users)` implements `IUserRepository` with hand-written `Add`, `GetById`, `GetAll`, `Delete`. All 4 methods manually implemented. | Boilerplate problem demonstrated | Yes | Straightforward hand-written class. |
| 7 | Test creates `var stub = new UserRepositoryStub(new List<User>())`. Pattern 1 standalone stubs implement the interface directly -- no `.Object` needed. `stub.Delete.Verify(Called.Once)` and `stub.GetById.Return(...)` work on the same object. | Stub is directly usable as IUserRepository AND has full mock capabilities | Yes | Confirmed by existing `MyRepoStub` usage at ReadMeUseCase.cs:112. |
| 8 | Seven new regions with `readme-` prefix and kebab-case names. `dotnet mdsnippets` processes them after Step 9 README update. | Snippets sync to README | Yes | Old snippets will show warnings until Step 9. Acknowledged in Risk 1. |

### Concerns

**Concern 1 (Minor): Type Name Overlap with Existing Files**

The plan's Type Conflict Resolution section (lines 154-160) lists `IMyRepo`, `IReadmeUserRepo`, `IUserRepo`, and `IOrderRepository` as the interfaces it checked. It missed two existing `IUserRepository` interfaces:
- `SkillPatternsSamples.cs` -- `IUserRepository` in `KnockOff.Documentation.Samples.SkillPatterns` namespace (GetById/Save)
- `AdvancedCallbacksSamples.cs` -- `IUserRepository` in `KnockOff.Documentation.Samples.AdvancedCallbacks` namespace (FindById)

Additionally, the proposed stub name `UserRepositoryStub` already exists in `AdvancedCallbacksSamples.cs` (AdvancedCallbacks namespace).

**Impact:** No compiler conflict because the namespaces differ (`Readme` vs `SkillPatterns`/`AdvancedCallbacks`). However, three files will all define `IUserRepository` with different method signatures, which is confusing for anyone reading the codebase.

**Resolution:** This is NOT blocking. The namespace isolation is correct. The developer should be aware of the overlap but the plan's chosen names are acceptable. The architect's analysis was incomplete but the conclusion (safe names) happens to be correct for different reasons than stated.

**Concern 2 (Informational): Removed Region Table Inaccuracy**

The plan's "Region Names (Removed)" table (lines 216-227) lists `readme-manual-stub-interface` and `readme-manual-stub-desired` as snippets being "replaced." However, the README does NOT reference these snippets -- they exist only as regions in the `.cs` file. The replacement mapping implies a README dependency that doesn't exist. The README only references 4 snippets from this file: `readme-knockoff-stub`, `readme-nsub-shared-mock`, `readme-knockoff-fetch-test`, `readme-knockoff-oncall-test`.

**Impact:** None on implementation. The Step 9 documentation work needs to reference only the 4 actual README snippets, not 6.

### Why This Plan Is Approved Despite Concerns

Both concerns are non-blocking:
1. The type name overlap is a cosmetic issue in a documentation samples project. Namespace isolation prevents any compiler error.
2. The region table inaccuracy doesn't affect implementation -- the developer is rewriting the entire file regardless.

The plan is otherwise clear and implementable. The assertions trace correctly through the proposed design. All 7 test scenarios have clear expected results that match the proposed implementation. The scope is small (one file rewrite), the risk is low (documentation samples only), and the existing codebase evidence confirms all claimed features work.

---

## Implementation Contract

**Created:** 2026-03-20
**Approved by:** knockoff-developer

### Verification Acceptance Criteria

- All 8 business rule assertions verified (see Assertion Trace Verification table)
- `dotnet build src/Tests/KnockOff.Documentation.Samples/` succeeds
- `dotnet test src/Tests/KnockOff.Documentation.Samples/` -- ALL tests pass (not just new ones)

### Test Scenario Mapping

| Scenario # | Test Method | Notes |
|------------|-------------|-------|
| 1 | Add-and-query test (`readme-fake-add-and-query` region) | Create stub with empty list, Add user, GetById returns it, GetAll includes it |
| 2 | (Covered by Scenario 1) | GetAll with pre-populated list exercises stub override fallback |
| 3 | Per-test override test (`readme-fake-per-test-override` region) | Configure `stub.GetById.Return(...)`, verify Return wins |
| 4 | Verify test (`readme-fake-verify` region) | Call Delete, then `stub.Delete.Verify(Called.Once)` |
| 5 | (Compilation-only) | NSubstitute example compiles and is exercised by implicit test coverage or explicit test |
| 6 | (Compilation-only) | Manual fake compiles |
| 7 | (Post-Step-9 deliverable) | `dotnet mdsnippets` succeeds after README update |

### In Scope

- [x] Rewrite `src/Tests/KnockOff.Documentation.Samples/ReadMeUseCase.cs` with:
  - [x] `IUserRepository` interface (Add/GetById/GetAll/Delete)
  - [x] NSubstitute split-abstraction example (List + Substitute wired via lambdas)
  - [x] `ManualUserRepositoryFake` hand-written class implementing all 4 methods
  - [x] `ReadmeUserRepositoryStub` with `[KnockOff]`, primary constructor `(List<User> users)`, and 4 stub overrides (renamed from `UserRepositoryStub` to avoid generator hint name collision with `AdvancedCallbacksSamples.cs`)
  - [x] Test: add-and-query (region `readme-fake-add-and-query`)
  - [x] Test: verify calls (region `readme-fake-verify`)
  - [x] Test: per-test override (region `readme-fake-per-test-override`)
  - [x] All sections wrapped in `#region` / `#endregion` with `readme-` prefix kebab-case names
- [x] Remove all old types: `IMyRepo`, `MyRepoManualStub`, `MyRepoStub`, `UserDomainModel`, `UserDomainModelTests`
- [x] Verify build: `dotnet build src/Tests/KnockOff.Documentation.Samples/`
- [x] Verify tests: `dotnet test src/Tests/KnockOff.Documentation.Samples/` -- ALL tests pass
- [x] Checkpoint: run full test suite after rewrite

### Out of Scope

- README.md changes (Step 9 documentation deliverable)
- `dotnet mdsnippets` sync (Step 9 -- old snippet references will show warnings until then)
- Skill file updates (not affected by this change)
- Changes to other sample files (`ReadmeSamples.cs`, `ReadmeComparisonSamples.cs`, etc.)

### Verification Gates

1. After file rewrite: `dotnet build src/Tests/KnockOff.Documentation.Samples/` succeeds
2. After file rewrite: `dotnet test src/Tests/KnockOff.Documentation.Samples/` -- ALL tests pass (including unchanged tests in other files)
3. Final: Verify no test count regression (existing tests in other files still present and passing)

### Stop Conditions

If any occur, STOP and report:
- Out-of-scope test failure (test in another file starts failing)
- Build failure in the Documentation.Samples project
- Generated stub does not compile (indicates generator issue, not sample issue)
- Any of the 4 stub override methods fail to generate correctly

---

## Implementation Progress

**Started:** 2026-03-20
**Developer:** knockoff-developer

### Milestone 1: File Rewrite

Rewrote `ReadMeUseCase.cs` with all planned content. Encountered two issues during implementation:

1. **Generator hint name collision:** The plan's stub name `UserRepositoryStub` collided with an existing `UserRepositoryStub` in `AdvancedCallbacksSamples.cs` (different namespace but same class name). The Roslyn generator uses class name, not fully qualified name, for generated file hint names, causing `CS8785: The hintName 'UserRepositoryStub.Base.g.cs' must be unique`. Renamed to `ReadmeUserRepositoryStub` following the project's existing naming convention (e.g., `CompareUserRepoStub`, `ReadmeUserRepoStub`). The developer review (Concern 1) identified the type name overlap but assessed it as "NOT blocking" -- it turned out to be blocking at the generator level.

2. **Interceptor-as-property compilation:** On the concrete stub type, `stub.Add(...)` resolves to the interceptor property, not the interface method. Tests needed `IUserRepository repo = stub;` to call methods through the interface while using `stub.` for interceptor access (Verify, Return). This follows the existing pattern in `ReusableStubsSamples.cs` (line 171).

### Verification Gates

1. Build succeeds: `dotnet build src/Tests/KnockOff.Documentation.Samples/` -- 0 warnings, 0 errors
2. All tests pass: `dotnet test src/Tests/KnockOff.Documentation.Samples/` -- 701 passed, 0 failed, 0 skipped (all 3 target frameworks)
3. No test count regression: 701 tests per framework (3 new tests added, 6 old tests removed = net -3; total confirms no out-of-scope test loss)

---

## Completion Evidence

**Reported:** 2026-03-20

- **Tests Passing:**
  - net8.0: 701 passed, 0 failed, 0 skipped (146 ms)
  - net9.0: 701 passed, 0 failed, 0 skipped (154 ms)
  - net10.0: 701 passed, 0 failed, 0 skipped (408 ms)
- **Verification Resources Pass:** N/A (no Design.Stubs changes)
- **All Contract Items:** Confirmed 100% complete

### Deviation from Plan

The stub class was renamed from `UserRepositoryStub` to `ReadmeUserRepositoryStub` to avoid a Roslyn generator hint name collision with the existing `UserRepositoryStub` in `AdvancedCallbacksSamples.cs`. This is a naming-only change; the behavior, API surface, and demonstration value are identical. The Step 9 README documentation deliverable should use `ReadmeUserRepositoryStub` in snippets.

### New Regions

| Region Name | Content |
|---|---|
| `readme-repo-interface` | `IUserRepository` with Add/GetById/GetAll/Delete |
| `readme-nsub-split-abstraction` | NSubstitute factory method with 4 lambda callbacks |
| `readme-manual-fake` | `ManualUserRepositoryFake` implementing all 4 methods |
| `readme-knockoff-fake` | `ReadmeUserRepositoryStub` with `[KnockOff]` and 4 stub overrides |
| `readme-fake-add-and-query` | Test: add users, then GetById/GetAll |
| `readme-fake-verify` | Test: Delete + `stub.Delete.Verify(Called.Once)` |
| `readme-fake-per-test-override` | Test: `stub.GetById.Return(...)` overrides stub behavior |

### Removed Types

- `IMyRepo` -- replaced by `IUserRepository`
- `MyRepoManualStub` -- replaced by `ManualUserRepositoryFake`
- `MyRepoStub` -- replaced by `ReadmeUserRepositoryStub`
- `UserDomainModel` -- removed (unnecessary for fake repository narrative)
- `UserDomainModelTests` -- replaced by `FakeRepositoryTests`

### Test Scenarios Verified

| # | Scenario | Result |
|---|----------|--------|
| 1 | Stub with backing list provides Add/GetById/GetAll | Pass (AddAndQuery test) |
| 2 | Stub override fallback (GetAll with pre-populated list) | Pass (covered by AddAndQuery test) |
| 3 | Return overrides stub override for one test | Pass (PerTestOverride test) |
| 4 | Verify works on stub with overrides | Pass (VerifyCalls test) |
| 5 | NSubstitute comparison compiles | Pass (build succeeds) |
| 6 | Manual fake comparison compiles | Pass (build succeeds) |
| 7 | Samples produce valid snippets | Deferred to Step 9 (README update needed first) |

---

## Documentation

**Agent:** [documentation agent name]
**Completed:** [date]

### Expected Deliverables

- [ ] README.md -- Rewrite opening sections (tagline through "So I Created KnockOff") to frame the fake repository value proposition. Replace old snippet references with new region names. Keep "What Sets KnockOff Apart" and everything after unchanged.
- [ ] Run `dotnet mdsnippets` to sync README with new sample regions
- [ ] Skill updates: No (skill docs are independent of README narrative)
- [ ] Sample updates: Done in implementation phase (ReadMeUseCase.cs)

### Files Updated

---

## Architect Verification

**Verified:** 2026-03-20
**Verdict:** VERIFIED

### Independent Test Results

All builds and tests run independently by the architect. Zero failures.

| Project | Framework | Passed | Failed | Skipped |
|---------|-----------|--------|--------|---------|
| KnockOff.Documentation.Samples | net8.0 | 701 | 0 | 0 |
| KnockOff.Documentation.Samples | net9.0 | 701 | 0 | 0 |
| KnockOff.Documentation.Samples | net10.0 | 701 | 0 | 0 |
| KnockOffTests | net8.0 | 1533 | 0 | 4 skipped |
| KnockOffTests | net9.0 | 1532 | 0 | 4 skipped |
| KnockOffTests | net10.0 | 1533 | 0 | 4 skipped |
| KnockOffTests.AssemblyStrict | all 3 | 14 each | 0 | 0 |
| KnockOff.NeatooInterfaceTests | all 3 | 473 each | 0 | 0 |

Build: `dotnet build src/KnockOff.sln` -- 0 warnings, 0 errors.

### Design Match

| Acceptance Criterion | Status | Evidence |
|---|---|---|
| IUserRepository with Add/GetById/GetAll/Delete | Matches plan | Lines 10-16, all 4 methods present |
| NSubstitute split-abstraction example | Matches plan | Lines 23-51, factory method with 4 lambda callbacks wiring to separate List |
| Manual fake class | Matches plan | Lines 57-73, ManualUserRepositoryFake with primary constructor, all 4 methods |
| KnockOff stub with List backing store and stub overrides | Matches plan | Lines 79-95, ReadmeUserRepositoryStub with [KnockOff], 4 stub overrides |
| Test: add-and-query | Matches plan | Lines 103-122, adds users via interface, queries by id and gets all |
| Test: verify calls | Matches plan | Lines 124-141, deletes via interface, stub.Delete.Verify(Called.Once) |
| Test: per-test override | Matches plan | Lines 143-157, stub.GetById.Return(specialUser) overrides stub behavior |
| All regions use readme- prefix kebab-case | Matches plan | 7 regions: readme-repo-interface, readme-nsub-split-abstraction, readme-manual-fake, readme-knockoff-fake, readme-fake-add-and-query, readme-fake-verify, readme-fake-per-test-override |
| No type conflicts | Matches plan | Build succeeds, namespace isolation confirmed |
| Old types removed | Matches plan | IMyRepo, MyRepoManualStub, MyRepoStub, UserDomainModel, UserDomainModelTests -- zero matches in file |

### Deviation Acknowledged

Stub renamed from `UserRepositoryStub` to `ReadmeUserRepositoryStub` to avoid Roslyn generator hint name collision with existing `UserRepositoryStub` in `AdvancedCallbacksSamples.cs`. The developer review (Concern 1) identified the type name overlap but assessed it as non-blocking at the C# level. The developer discovered it was blocking at the generator hint name level during implementation. The rename is appropriate and follows existing naming conventions (e.g., `CompareUserRepoStub`, `ReadmeUserRepoStub`).

### Issues Found

None.

---

## Requirements Verification

**Reviewer:** knockoff-requirements-reviewer
**Verified:** 2026-03-20
**Verdict:** REQUIREMENTS SATISFIED

### Requirements Compliance

| Requirement | Status | Evidence |
|-------------|--------|----------|
| Interceptor-as-property | Satisfied | `stub.Delete.Verify(Called.Once)` (line 139) and `stub.GetById.Return(specialUser)` (line 152) both access interceptors as properties. Interface method calls go through `IUserRepository repo = stub;` then `repo.Method()`, correctly separating interceptor access from interface invocation. |
| API consistency (Pattern 1) | Satisfied | No new API surface introduced. Uses only existing APIs: `[KnockOff]` attribute, `protected override` with underscore suffix, `Return()`, `Verify()`, `Called.Once`. All confirmed in api-consistency-matrix.md and Design.Stubs/StubOverrides/StubOverrideBasics.cs. |
| Nine patterns scope | Satisfied | Sample targets Pattern 1 (Standalone Interface) only. This is appropriate because stub overrides require standalone patterns (1-4), and the fake repository pattern relies on standalone stubs owning state. No other patterns need coverage for this narrative sample. |
| Four member types scope | Satisfied | Methods only (Add, GetById, GetAll, Delete). Appropriate for the repository pattern demonstration. Properties, indexers, and events are not relevant to this narrative. |
| Pipeline verification rule | Satisfied | No generator or pipeline changes. This is a documentation sample file only. |
| Design projects as source of truth | Satisfied | No Design project files modified. The only Design project reference to this file is a historical comment in Design.Tests/AdvancedTests/WhenChainVerificationBugTests.cs (line 61: "original reproduction case from ReadMeUseCase.cs") -- not a code dependency. |
| MarkdownSnippets (#region markers) | Satisfied | Seven regions with `readme-` prefix and kebab-case names: `readme-repo-interface` (line 9), `readme-nsub-split-abstraction` (line 25), `readme-manual-fake` (line 57), `readme-knockoff-fake` (line 79), `readme-fake-add-and-query` (line 106), `readme-fake-verify` (line 127), `readme-fake-per-test-override` (line 146). |
| No type conflicts across namespaces | Satisfied | `IUserRepository` in `Readme` namespace does not conflict with `IUserRepository` in `SkillPatterns` or `AdvancedCallbacks` namespaces (different namespaces, confirmed by build). Stub renamed to `ReadmeUserRepositoryStub` to avoid Roslyn generator hint name collision with `UserRepositoryStub` in `AdvancedCallbacks`. |
| Existing tests pass | Satisfied | Architect independently verified: 701 passed per framework in Documentation.Samples; 1533/1532/1533 passed in KnockOffTests; 14 each in AssemblyStrict; 473 each in NeatooInterfaceTests. Zero failures. |
| Stub override priority chain (Design.Stubs contract) | Satisfied | PerTestOverride test (line 143-157) demonstrates `stub.GetById.Return(specialUser)` taking priority over `GetById_` stub override. Consistent with priority chain documented in Design.Stubs/StubOverrides/StubOverrideBasics.cs (When > Sequences > Return/Call > Stub Override). |
| Old types fully removed | Satisfied | Zero matches for `IMyRepo`, `MyRepoManualStub`, `MyRepoStub`, `UserDomainModel`, `UserDomainModelTests` in `src/`. README still references old snippet names -- expected and documented as Step 9 deliverable. |

### Unintended Side Effects

None. This change is entirely contained within a single documentation sample file (`ReadMeUseCase.cs`). No shared code, generated code structure, library base classes, interceptor API signatures, or pipeline code was modified. The only files affected outside `ReadMeUseCase.cs` are the Roslyn-generated files for the new `ReadmeUserRepositoryStub` (excluded from git, verified by build success). The README still references old snippet names from the removed regions, but this is expected and acknowledged in the plan as a Step 9 documentation deliverable.

### Issues Found

None.
