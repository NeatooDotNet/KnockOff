# Property `new` narrowing/widening across interface hierarchy produces uncompilable stubs

**Type:** Bug-Exposes-Fallacy
**Status:** Complete
**Priority:** High
**Created:** 2026-04-20
**Last Updated:** 2026-04-20 (Requirements Review added)

---

## Problem

When an interface hierarchy uses the C# `new` modifier to shadow a property with a different accessor set, the generator produces uncompilable stubs.

Repro (in `src/Design/Design.Domain/Entities/IInterfaceNarrow.cs` and `src/Design/Design.Stubs/Properties/NarrowingPropertyRepro.cs`):

```csharp
public interface IInterfaceWide { int Prop { get; set; } }
public interface IInterfaceNarrow : IInterfaceWide { new int Prop { get; } }

[KnockOff<IInterfaceNarrow>]              // Inline — pattern 5
public partial class NarrowingInlineStub { }

[KnockOff]
public partial class NarrowingStandaloneStub : IInterfaceNarrow { }  // pattern 1
```

Both produce:

```
error CS1061: 'PropertyGetInterceptor<int>' does not contain a definition for 'InvokeSet'
```

The interceptor property is typed from the first-encountered declaration (`IInterfaceNarrow.Prop`, get-only), but the generator still emits `IInterfaceWide.Prop` with a setter that calls `InvokeSet` on the get-only interceptor.

## Solution

When multiple shadowed property declarations exist for the same interceptor name, compute the **union** of accessors for the interceptor's type (so `InvokeGet` and `InvokeSet` are both available whenever any shadowed declaration requires them). Explicit interface implementations continue to use each declaring interface's own accessor set.

---

## Skipped Steps

_(none yet)_

---

## Plans

- [Property new narrowing fix](../plans/property-new-narrowing-bug.md)

---

## Requirements Review

**Reviewer:** knockoff-requirements-reviewer
**Reviewed:** 2026-04-20
**Verdict:** APPROVED

### Relevant Requirements Found

**Governing constraints (CLAUDE.md):**
- **Interceptor-as-Property Principle** — `stub.Prop` must remain a property returning one interceptor object. The plan explicitly preserves this (see Design Decision 1: rejects splitting into `Prop_Narrow`/`Prop_Wide` interceptors).
- **API Consistency Principle** — All applicable patterns should offer identical APIs. The plan addresses patterns 1, 2, 5, 7, 8 (both affected interface pipelines) and explicitly defers class patterns 3, 4, 6, 9. See Contradictions below.
- **Pipeline Verification Rule** — Plan correctly identifies the two distinct pipelines (Inline via `InlineModelBuilder`, Flat via `FlatModelBuilder`+`FlatRenderer`) and proposes a targeted fix in each. It does NOT rely on "same code path" hand-waving — it reads each pipeline independently and confirms the bug in each.
- **Four Member Types** — Plan addresses properties only. Indexers, methods, events explicitly considered (methods/events unaffected; indexer shadowing deferred with rationale).

**Behavioral contracts (Design projects):**
- `src/Design/Design.Domain/Entities/IInterfaceNarrow.cs` — already contains the `IInterfaceWide`/`IInterfaceNarrow` shadow hierarchy. Pre-existing Design-level repro.
- `src/Design/Design.Stubs/Properties/NarrowingPropertyRepro.cs` — already contains `NarrowingInlineStub`, `WideInlineStub`, `NarrowingStandaloneStub`. These are the repros that currently fail to compile; they form the behavioral contract the fix must satisfy.
- `src/Design/Design.Tests/PropertyTests/PropertyBasicsTests.cs` and `PropertySequenceTests.cs` — non-shadowed property contracts. Regression scenario 7 in the plan protects these.
- No existing Design.Stubs or Design.Tests demonstrate shadowed-property routing behavior (Get via narrow face, Set via wide face, VerifySet across shadow). This is a gap the plan fills via scenarios 3–6.

**FlatRenderer existing widest-accessor logic:**
- `FlatRenderer.cs:315-348` already does widest-accessor selection — but only for the `protected virtual` **stub-override base class property**, NOT for the interceptor type at `FlatRenderer.cs:79-99`. The plan correctly identifies this as a separate, non-overlapping code path. The fix pattern (widest accessor wins) is consistent with the precedent already established in the codebase.

**api-consistency-matrix.md:**
- Feature 3 (Property Interception) claims `Get`, `Set`, `VerifyGet`, `VerifySet`, `LastSetValue` are 100% consistent across all 8 interface/class patterns. For shadowed properties on patterns 1, 2, 5, 7, 8 today, this claim is false (stubs do not compile). The fix restores the matrix's stated consistency for these 5 patterns.
- Matrix needs no structural update from this fix, but patterns 3, 4, 6, 9 remain silently non-compliant for shadowed properties until the deferred class-patterns work lands.

### Gaps

- No existing Design.Stubs coverage for shadowed properties in class patterns (3, 4, 6, 9). Plan defers; see Contradictions for whether deferral is acceptable.
- No existing Design.Stubs coverage for shadowed indexers (`new this[int]` with differing accessors). Plan defers; architect should confirm a follow-up todo is filed.
- No existing Design.Tests for shadowed-property routing (`Get` via narrow face, `Set` via wide face, `VerifySet` across faces). Plan's scenarios 3–6 fill this gap.
- No existing test asserts regression-identity of generated output for non-shadowed interfaces. Plan step 8 (diff vs pre-fix snapshot) is the right mechanism but has no infrastructure today — architect should confirm how the snapshot check will be performed.

### Contradictions

**None that rise to VETO level.** Notes:

- **Deferral of class patterns (3, 4, 6, 9)** — technically a partial violation of the API Consistency Principle (same feature should work across all applicable patterns). However:
  - The class pipeline (`StandaloneClassModelBuilder`, `ClassRenderer`) is genuinely a different code path with its own `new`-shadowing semantics (class members with `new` require `override` chains that interface shadowing doesn't).
  - Interface-level shadowing is a legitimate scope boundary — users of interface patterns get a working fix now rather than waiting for a larger cross-pipeline refactor.
  - Plan explicitly acknowledges the deferral and calls it out in Deferred Scope.
  - **Requirement:** A follow-up todo MUST be filed for class-pattern shadowed-property support before the matrix can advertise 100% consistency for Feature 3. Architect: confirm this is captured.

- **Deferral of shadowed indexers** — acceptable because no repro exists and the dedup key differs. Follow-up todo recommended if a repro surfaces.

### Recommendations for Architect

1. **Proceed with the two-site fix as designed.** The diagnosis is accurate, the fix is minimal, and the interceptor-as-property principle is preserved.

2. **File follow-up todos before closing this one:**
   - Class-pattern shadowed properties (patterns 3, 4, 6, 9) — required to restore full matrix consistency for Feature 3.
   - Shadowed indexers — lower priority, file a tracking todo.

3. **Verify the "regression check" (step 8) has a real mechanism.** The plan says "compare generated output to a prior-build snapshot" but there is no existing snapshot harness. Either (a) use the Design.Stubs build succeeding as proxy (non-shadowed stubs still compile and Design.Tests still pass), or (b) introduce a concrete snapshot comparison. Do not ship with a vague "regression check" that doesn't actually run.

4. **Verify `InterfaceMemberInfo` and `FlatPropertyModel` are records** (implementation step 1). If either is not, the `with` expressions must be replaced with explicit constructor calls — the plan acknowledges this.

5. **Expand `NarrowingPropertyRepro.cs` to cover the open-generic case (scenario 8, pattern 8: `[KnockOff(typeof(IShadowed<>))]`).** The current repro file covers patterns 1 and 5 only. Per the Pipeline Verification Rule, pattern 8 must be tested because it uses `InlineRenderer` via a different transform path, not assumed to share behavior with pattern 5.

6. **Ensure new Design.Tests for scenarios 3–6 verify behavior, not just compilation.** Compile-clean is necessary but not sufficient — runtime routing (Get via narrow face, Set via wide face, `LastSetValue`, `VerifySet`) must be asserted with actual calls.

7. **Add `DESIGN DECISION` comments in `NarrowingPropertyRepro.cs`** explaining that the shared interceptor's accessor set is the union of shadowed declarations. This is the kind of non-obvious behavior that needs to be documented in Design.Stubs per the source-of-truth rule.

---

## Graded Review

### 2026-04-20 (re-review)
**Reviewer:** code-reviewer
**Overall Grade:** A

| Category | Grade |
|----------|-------|
| Requirements Coverage | A |
| Test Coverage | A |
| Design Alignment | A |
| Code Quality | A |
| Framework Correctness | A |
| Build & Test Health | A |
| Scope Discipline | A |

Build: 0 warnings, 0 errors. Tests: 0 failed across entire solution (Design.Tests 380 × 3 TFMs, KnockOffTests 1515-1516 × 3, Analysis 1336 × 3, NeatooInterface 473 × 3, Documentation.Samples 701 × 3, AssemblyStrict 14 × 3).

### 2026-04-20 (initial)
**Overall Grade:** B — gaps in Test Coverage (no standalone-widening runtime tests), Design Alignment & Scope Discipline (Fix #3 source-fallback extension implemented but not documented in plan). Both addressed; see re-review above.

---

## Progress Log

### 2026-04-20
- Reproduced bug in Design.Stubs (inline and standalone patterns both fail).
- Traced root cause: Inline pipeline dedups property members keeping first (`InlineModelBuilder.cs:120-123`); Flat pipeline picks the first property's `HasGetter/HasSetter` when rendering the interceptor class (`FlatRenderer.cs:79-99`).
- Drafted plan.
