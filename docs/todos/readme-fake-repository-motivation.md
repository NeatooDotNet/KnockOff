# README Rewrite: Fake Repository Value Proposition

**Status:** In Progress
**Priority:** High
**Created:** 2026-03-20
**Last Updated:** 2026-03-20 (architect plan creation)

---

## Problem

The current README leads with "I wanted to reuse my mocks." That's true but misses the deeper motivation.

The real problem: when you need a fake repository for integration tests (full DI, no database), mocking frameworks force a split abstraction. You end up with two properties in your test fixture — `List<MyEntity>` (the backing store) and `Mock<IMyRepo>` (the mock) — wired together via callbacks. Assertions hit the list but there's no visible connection to the mock. Modifying mock behavior requires thinking about list state separately. They're conceptually one thing split across two objects.

Both Moq and NSubstitute have this limitation. The "real" answer is to hand-roll a fake class that implements the interface and owns its list. But that means implementing every interface member manually — boilerplate.

KnockOff solves this: a standalone stub IS a real class. It owns its `List<T>`, implements `IMyRepo`, and still has Verify/When/Return. One object. No split abstraction. No boilerplate.

## Solution

1. Create a new sample in `Documentation.Samples` that demonstrates the fake repository pattern — a standalone stub with `List<T>` backing store, stub overrides for Add/GetById/Delete, and a test showing DI registration and assertion on the stub's own state.
2. Replace the README's lead narrative and "Why I Wrote KnockOff" section to frame this as the primary value proposition: "Mocking frameworks are the wrong tool for fakes. KnockOff bridges the gap."
3. Keep the existing comparison tables and feature sections — just reframe the opening.

---

## Clarifications

---

## Requirements Review

**Reviewer:** knockoff-requirements-reviewer
**Reviewed:** 2026-03-20
**Verdict:** APPROVED

### Relevant Requirements Found

**Governing Constraints from CLAUDE.md:**

1. **Interceptor-as-Property Principle** -- Not affected. This todo proposes no API changes. The proposed sample will use `stub.Method` as a property returning an interceptor object, consistent with all existing samples.

2. **API Consistency Principle** -- Not affected. No new API surface is proposed. The sample will use existing standalone stub features (stub overrides, `Return`/`Call`/`Verify`, `When` chains) that already work consistently across all applicable patterns.

3. **Nine Patterns** -- The proposed sample targets Pattern 1 (Standalone Interface) only. This is appropriate because: (a) stub overrides are only available on standalone patterns (1-4), and (b) the fake repository pattern specifically relies on standalone stubs owning state via constructor parameters. No other patterns need coverage for this narrative sample.

4. **Four Member Types** -- The proposed sample uses methods only (Add/GetById/Delete). This is sufficient for a README motivation sample. Properties, indexers, and events are not relevant to the fake repository pattern being demonstrated.

5. **Pipeline Verification Rule** -- Not affected. No generator or pipeline changes are proposed.

6. **Design Projects as Source of Truth** -- No conflicts. The proposed sample pattern is already demonstrated in Design.Stubs.

**Behavioral Contracts (Design.Stubs and Design.Tests):**

- **Standalone stub with constructor parameters and List backing store:** Already demonstrated and compiling in `src/Tests/KnockOff.Documentation.Samples/ReadMeUseCase.cs` (lines 36-49). `MyRepoStub(List<User> Users)` uses primary constructor to inject test data and provides stub overrides for `GetUser_` and `Update_`. All tests in that file pass.

- **Same pattern with `OrderRepoStub(List<Order> orders)`:** Demonstrated in `src/Tests/KnockOff.Documentation.Samples/ReusableStubsSamples.cs` (lines 61-73). Stub overrides for `GetOrder_` and `GetTotal_` with a `List<Order>` backing store.

- **Stub override with custom type parameters:** The custom-type detection bug was fixed on 2026-02-05 (plan: `docs/plans/completed/user-method-type-detection-fix.md`, todo: `docs/todos/completed/user-method-custom-type-detection.md`). Confirmed by 14 unit tests in `src/Tests/KnockOffTests/StubOverrideCustomTypeDetectionTests.cs`. The proposed sample's `Add(Entity)`, `Delete(Entity)` stub overrides will work correctly because custom type parameters are now properly resolved through the semantic model.

- **Stub override priority chain (confirmed in Design.Stubs):** `src/Design/Design.Stubs/StubOverrides/StubOverrideBasics.cs` documents the priority: When chains > Sequences > Return/Call > Stub Override > Source > Smart default. The proposed sample's claim that "Return/Call overrides stub methods per-test" is consistent with this contract.

- **Verify/Verifiable on stub override methods (confirmed in Design.Stubs):** `src/Design/Design.Stubs/StubOverrides/StubOverrideBasics.cs` (lines 141-153) confirms `stub.Method.Verify(Called.X)` works on methods with stub overrides. The proposed sample's use of verification on the stub is consistent.

- **API consistency matrix:** `docs/guides/api-consistency-matrix.md` Feature 11 (Stub Overrides) confirms standalone patterns 1-4 support stub overrides. The proposed sample uses Pattern 1, which is confirmed working.

**Related Guide Documentation:**

- `docs/guides/stub-overrides.md` -- Documents the stub override pattern, availability (standalone patterns 1-4), and underscore suffix convention.
- `docs/guides/reusable-stubs.md` -- Documents the reusable stub pattern with constructor parameters.

### Gaps

None. The proposed sample uses only features that are already demonstrated in Design.Stubs and covered by existing behavioral contracts. No new patterns, member types, or API surfaces are introduced.

### Contradictions

None found. The proposed work is documentation/sample-only with no generator, library, or API changes.

**Note on narrative claims:** The todo's Problem section states "mocking frameworks force a split abstraction" and "KnockOff solves this: a standalone stub IS a real class." These claims are accurate and consistent with KnockOff's actual behavior -- standalone stubs are real partial classes that can own state, accept constructor parameters, and provide stub override methods, while also exposing full interceptor APIs (Verify/When/Return). The existing `ReadMeUseCase.cs` sample already demonstrates this exact capability.

### Recommendations for Architect

1. **Existing samples already cover this pattern.** The `ReadMeUseCase.cs` file already has `MyRepoStub(List<User> Users)` with stub overrides -- the exact pattern this todo proposes. The new sample should either (a) extend or replace the existing `ReadMeUseCase.cs` with the Add/GetById/Delete interface, or (b) create a separate sample file. Avoid duplicating the same pattern across multiple sample files.

2. **Custom constructor chaining requirement.** Per the stub-overrides reference, custom constructors on standalone stubs must chain to `this()`. The proposed sample uses a primary constructor (`MyRepoStub(List<T> backing)`), which the compiler handles correctly. The architect should confirm this is the preferred style for the sample.

3. **Design.Stubs divergence (informational, not blocking).** The files `src/Design/Design.Stubs/StubOverrides/VoidStubOverrideFallback.cs` and `src/Design/Design.Tests/StubOverrideTests/VoidStubOverrideFallbackTests.cs` still contain "BUG" comments describing the custom-type detection issue as if it is active (e.g., "ACTUAL: This method is never called because the generator does not recognize it"). The bug was fixed on 2026-02-05 and these tests now pass, but the comments have not been updated to reflect the fix. This divergence is not related to the current todo but should be reconciled in a separate cleanup task to keep Design projects accurate as source of truth.

4. **MarkdownSnippets integration.** Per `.claude/rules/documentation-samples.md`, all sample code must use `#region` / `#endregion` markers with descriptive kebab-case names, and corresponding `<!-- snippet: region-name -->` references in the README markdown. The architect should plan for `dotnet mdsnippets` verification after implementation.

5. **No new pipelines or generators affected.** This is a documentation-only change. No builder, renderer, or model changes are needed.

---

## Plans

- [README Rewrite: Fake Repository Value Proposition](../plans/readme-fake-repository-motivation.md)

---

## Tasks

- [x] Architect comprehension check (Step 2)
- [x] Business requirements review (Step 3)
- [x] Architect plan creation & design (Step 4)
- [ ] Developer review (Step 5)
- [ ] Implementation (Step 7)
- [ ] Verification (Step 8)
- [ ] Documentation (Step 9)
- [ ] Completion (Step 10)

---

## Progress Log

### 2026-03-20
- Created todo from conversation about fake repository motivation
- Confirmed the custom-type stub override detection bug was already fixed (completed 2026-02-05)
- Existing README samples are in `src/Tests/KnockOff.Documentation.Samples/ReadMeUseCase.cs`, `ReadmeSamples.cs`, `ReadmeComparisonSamples.cs`
- The README already uses `MyRepoStub` as lead example but frames motivation as "reuse mocks" rather than "fakes vs mocks"
- Requirements review completed (APPROVED) -- no contradictions, no gaps
- Architect plan created at `docs/plans/readme-fake-repository-motivation.md`
- Decision: replace `ReadMeUseCase.cs` content (not create a new file) with richer `IUserRepository` (Add/GetById/GetAll/Delete)

---

## Completion Verification

Before marking this todo as Complete, verify:

- [ ] All builds pass
- [ ] All tests pass

**Verification results:**
- Build: [Pending]
- Tests: [Pending]

---

## Results / Conclusions

