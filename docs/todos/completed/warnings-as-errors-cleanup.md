# Warnings as Errors Cleanup

**Status:** Complete
**Priority:** Medium
**Created:** 2026-03-23
**Last Updated:** 2026-03-23


---

## Problem

Many `NoWarn` suppressions were added across the solution instead of actually fixing the underlying issues. These suppressions hide real problems and prevent the codebase from maintaining high code quality. The generated code also needs to be able to compile with warnings as errors enabled.

## Solution

Work towards having warnings as errors fully enabled across the solution:
1. Remove as many `NoWarn` entries as possible from `Directory.Build.props` and individual `.csproj` files by fixing the underlying code issues
2. Ensure generated code can compile with warnings as errors enabled (consumers shouldn't need to suppress warnings caused by KnockOff-generated code)
3. Audit and remove `#pragma warning disable` directives where the underlying issue can be fixed instead

## Discovered References

- `src/Directory.Build.props` — `TreatWarningsAsErrors` is already `True`, but 10 codes are suppressed via `NoWarn`: `CA1861;CA1865;CA1510;IDE0021;IDE0022;IDE0023;IDE1006;CA1050;CA1822;MSB3277`
- `src/Prototype/Directory.Build.props` — `TreatWarningsAsErrors` set to `false`
- `src/Tests/PackageTest/PackageTest.csproj` — `TreatWarningsAsErrors` set to `false`
- 10 `.csproj` files have additional per-project `NoWarn` entries
- 56 `.cs` files contain `#pragma warning disable` directives

## Plans

- [Warnings as Errors Cleanup](../plans/warnings-as-errors-cleanup.md)

## Tasks

- [x] Create todo (Step 1)
- [x] Architect comprehension check (Step 2)
- [x] Business requirements review (Step 3)
- [x] Architect plan creation & design (Step 4)
- [x] Developer review (Step 5)
- [x] Implementation (Step 7)
- [x] Verification (Step 8)
- [x] Documentation (Step 9 — N/A, no behavioral or user-facing doc changes)
- [x] Completion (Step 10)

## Clarifications

**Q1: Should generated-code warnings be prioritized over internal test project suppressions?**
A: Yes, prioritize generated-code warnings — they affect every consumer.

**Q2: Should Prototype and PackageTest be brought under the same discipline?**
A: No, leave them as-is — they're experimental.

**Q3: Library runtime code (`src/KnockOff/Interceptors/`) has intentional API design pragmas (CA1034, CA1051, CA1002). Preference?**
A: List them all, evaluate what it would take to un-suppress each one, and if a suppression must stay, it needs documented justification.

**Q4: Should the goal be zero suppressions or only justified ones?**
A: Only suppressions with clear documented justification.

**Q5: Is `SYSLIB0050` in generated code a known necessity?**
A: Nothing is a known necessity — investigate all of them.

## Requirements Review

**Reviewer:** knockoff-requirements-reviewer
**Reviewed:** 2026-03-23
**Verdict:** APPROVED

### Governing Constraints Checked

1. **Interceptor-as-Property Principle** -- NOT AT RISK. This todo does not propose changing interceptor APIs. However, the architect must be warned: the library interceptor files (`src/KnockOff/Interceptors/`) suppress CA1034 (nested types), CA1051 (visible instance fields), CA1002 (generic lists), CA1062 (argument validation), and CA1716 (keyword identifiers). Fixing CA1034/CA1051/CA1002 would require restructuring interceptor base classes (e.g., making fields private, un-nesting types, replacing `List<>` with `IList<>`) which would break the interceptor-as-property architecture and all generated code that inherits from these base classes. These suppressions are likely to require "documented justification" rather than removal.

2. **API Consistency Principle** -- NOT AT RISK. No API changes proposed. Warning cleanup is internal code quality work.

3. **Nine Patterns** -- All four pipelines emit `#pragma warning disable` directives in generated code (SYSLIB0050 in FlatRenderer, InlineRenderer, StandaloneClassRenderer; CS8601/CS8765/CS8763/CS8618/CS8603 in ClassRenderer, StandaloneClassRenderer, and shared renderers). Changes to generated pragmas must be verified across all four renderer pipelines per the Pipeline Verification Rule.

4. **Four Member Types** -- Not directly affected. Warning suppressions are not member-type-specific.

5. **Pipeline Verification Rule** -- Relevant. Generator-emitted pragmas exist in: `FlatRenderer` (patterns 1,2), `StandaloneClassRenderer` (patterns 3,4), `InlineRenderer` (patterns 5-9), `ClassRenderer` (shared by inline/standalone class), and shared renderers (`MethodInterceptorRenderer`, `PropertyInterceptorRenderer`, `IndexerInterceptorRenderer`). Any pragma removal in one renderer must be checked against all renderers that emit the same pattern.

6. **Design Projects as Source of Truth** -- Design.Stubs and Design.Tests have their own suppressions (CA1707, CA2007, CA1030, CA1052, CA1062, CA1044). Changes to these must not alter the compilability of Design projects, which serve as the API source of truth.

### Behavioral Contracts Found

No behavioral contracts are directly affected. This work changes compilation warnings, not runtime behavior. However, the following implicit contracts exist:

- **Generated code must compile under warnings-as-errors in consumer projects.** The generator already emits pragmas (SYSLIB0050, CS8765, CS8618, CS8763, CS8601, CS8603, CS8769) to ensure this. Removing any of these without fixing the underlying generated code pattern would break compilation for consumers with `TreatWarningsAsErrors=true` -- which is every project under `src/Directory.Build.props`.

- **Library interceptor base classes are the public API.** Files in `src/KnockOff/Interceptors/` define the runtime types that generated interceptor classes inherit from. Their public field/type structure (suppressed by CA1034, CA1051, CA1002) is load-bearing. Generated code directly accesses these fields and nested types. Changing visibility or structure is a breaking change.

### Gaps Identified

None. This todo does not introduce new features requiring pattern or member type coverage.

### Contradictions Found

None. No governing constraint violations.

### Risk Areas for Architect

**HIGH RISK -- Library interceptor suppressions (CA1034, CA1051, CA1002):**
These protect the interceptor architecture. The generated code inherits from `MethodInterceptorRuntime`, `PropertyGetInterceptorBase`, `PropertySetInterceptorBase`, `IndexerGetSetInterceptorBase`, etc. and accesses their `public` fields directly. Fixing CA1051 (making fields non-public) or CA1034 (un-nesting types) would require changes to every generated interceptor class across all four pipelines. This would be a massive cross-cutting change, not a simple warning fix.

**MEDIUM RISK -- Generator-emitted pragmas (SYSLIB0050, CS8601, CS8765, CS8618, CS8763, CS8603):**
These exist because the generated code must handle cases the C# compiler cannot prove safe (nullability mismatches on overrides, `[DoesNotReturn]` methods that actually return, serialization APIs, ref-return backing fields). The architect should investigate whether better code generation patterns could eliminate the need for each pragma, but some may be inherent to the source generation approach and require documented justification.

**LOW RISK -- Solution-wide NoWarn and test project suppressions:**
The 10 solution-wide codes (CA1861, CA1865, CA1510, IDE0021-IDE0023, IDE1006, CA1050, CA1822, MSB3277) and per-project test suppressions (CA1707, xUnit1051, CS4014, IDE0044, etc.) are code style rules with no API impact. These are safe to evaluate independently.

### Recommendations for Architect

1. **Categorize suppressions by risk tier before planning work.** Separate into: (a) safe to remove by fixing code, (b) requires generated code pattern changes, (c) requires library API restructuring (likely keep with justification).

2. **Library interceptor pragmas will almost certainly need documented justification.** The CA1034/CA1051/CA1002 suppressions in `src/KnockOff/Interceptors/` are structural to the interceptor-as-property architecture. Evaluate the cost, but expect most to remain as justified.

3. **Generator-emitted pragmas need per-pipeline verification.** If a pragma is removed from generated output in one renderer, verify all four renderers (FlatRenderer, StandaloneClassRenderer, InlineRenderer/ClassRenderer, shared renderers) and confirm generated code still compiles under warnings-as-errors for all nine patterns.

4. **Design project suppressions must preserve compilability.** `src/Design/Design.Stubs/` and `src/Design/Design.Tests/` are the API source of truth. Any suppression removal there must be followed by a build verification of both Design projects.

5. **No API changes should be needed.** If the architect discovers that removing a suppression requires changing a public API signature in the library or generated code, that is a scope escalation that needs separate approval -- it would affect consumers.

## Progress Log

- **2026-03-23:** Todo created from user request to clean up warning suppressions and enable warnings as errors across the solution
- **2026-03-23:** Architect plan created at docs/plans/warnings-as-errors-cleanup.md. Comprehensive suppression catalog completed covering all NoWarn entries, renderer-emitted pragmas, library runtime pragmas, and test/design project suppressions. Four-phase implementation plan defined.

## Results / Conclusions

**All 4 phases complete. Architect verified. Requirements satisfied.**

### Before → After

| Metric | Before | After |
|--------|--------|-------|
| Directory.Build.props NoWarn entries | 10 | 1 (MSB3277, justified) |
| Test project .csproj NoWarn codes | ~60 total | ~6 total (xUnit1051 + justified per-project) |
| CA1062 pragmas in interceptors | 4 | 1 (justified) |
| SYSLIB0050 in MethodInterceptorRenderer inline pairs | 6 | 0 (redundant with file-level) |

### Key Findings

1. **SYSLIB0050 is NOT cargo-culted** — it's required because KnockOff generates stubs for `ISerializable` whose method signatures reference obsolete types. Retained with justification.
2. **9 of 10 solution-wide NoWarn entries removed** — most produced zero warnings because code was already compliant.
3. **CA1062 pragmas partially eliminated** — replaced with `ArgumentNullException.ThrowIfNull` null checks in `InterceptorExtensions.cs` and `PropertyGetInterceptorBase.cs`.
4. **Test projects switched to $(NoWarn) inheritance** — prevents future duplication of suppressions.
5. **Every remaining suppression now has documented justification.**
