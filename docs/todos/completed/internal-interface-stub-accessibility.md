# Internal Interface Stub Accessibility

**Status:** Complete
**Priority:** High
**Created:** 2026-03-07
**Last Updated:** 2026-03-07

**Plan:** [Internal Interface Stub Accessibility](../plans/completed/internal-interface-stub-accessibility.md)
**Requirements Review:** 2026-03-07

---

## Problem

When stubbing an interface that has internal accessibility (or internal methods), the generated stub class is always emitted as `public`. This causes a compilation error because a public class can't implement an internal interface. This happens even when the stub is in the same assembly as the interface.

The bug was discovered with an inline stub (`[KnockOff<IInternalInterface>]`), but all 9 patterns need to be checked for the same issue.

## Solution

KnockOff should emit stub classes with accessibility that matches or is compatible with the target interface. When the target interface is internal, the generated stub class must also be internal.

Check all 9 patterns for this bug — each has a separate code pipeline and may need independent fixes.

---

## Clarifications

Architect comprehension check completed 2026-03-07 — no questions, ready to proceed.

Architect confirmed four hardcoded `public class` locations across all renderers: `InlineRenderer.cs:247`, `ClassRenderer.cs:118`, `FlatRenderer.cs:268`, `StandaloneClassRenderer.cs:273`. For standalone patterns, the generated `Base` helper class also needs matching accessibility.

---

## Requirements Review

**Reviewer:** knockoff-requirements-reviewer
**Reviewed:** 2026-03-07
**Verdict:** APPROVED

### Relevant Requirements Found

**Governing Constraints (CLAUDE.md):**

1. **Interceptor-as-Property Principle** — Not affected. This change modifies the accessibility modifier on the generated stub class declaration, not the interceptor API. `stub.Method` remains a property returning an interceptor object.

2. **API Consistency Principle** — Directly relevant. The fix must be applied consistently across all applicable patterns. The current bug (hardcoded `public class`) affects all patterns, and the fix must address all pipelines equally. The resulting API surface (how users interact with stubs) does not change — only the generated class's accessibility modifier changes.

3. **Nine Patterns** — All nine patterns are in scope. Each pipeline has a separate hardcoded `public class` location:
   - **Patterns 1-2 (Standalone Interface):** `FlatRenderer.cs:268` emits `public class {ClassName}Base`. The user-declared stub class controls its own accessibility via the partial class declaration, but the generated `Base` helper class is hardcoded to `public`.
   - **Patterns 3-4 (Standalone Class):** `StandaloneClassRenderer.cs:273` emits `public class {ClassName}Base`. Same issue as Patterns 1-2.
   - **Patterns 5, 8 (Inline Interface, Open Generic Interface):** `InlineRenderer.cs:247` emits `public class {StubClassName}`. This is the fully generated stub class inside the nested `Stubs` container.
   - **Patterns 6, 9 (Inline Class, Open Generic Class):** `ClassRenderer.cs:118` emits `public class {StubClassName}`. This is the wrapper stub class inside the nested `Stubs` container.
   - **Pattern 7 (Inline Delegate):** Delegates have no accessibility constraint from the target type in the same way, but should be checked for consistency.

4. **Four Member Types** — Not directly affected. This change is about the stub class declaration, not about individual member (method/property/indexer/event) interception. All four member types continue to work identically regardless of the stub class's accessibility.

5. **Pipeline Verification Rule** — Critically relevant. Four separate pipelines each have independent hardcoded `public class` locations. Per the Pipeline Verification Rule, fixing one pipeline does NOT fix the others. Each must be independently modified and verified:
   - `FlatRenderer` (Patterns 1, 2)
   - `StandaloneClassRenderer` (Patterns 3, 4)
   - `InlineRenderer` (Patterns 5, 7, 8)
   - `ClassRenderer` (Patterns 6, 9)

6. **Design Projects as Source of Truth** — No existing Design.Stubs code demonstrates internal interfaces or internal classes. All Design.Stubs types use `public` accessibility. The documentation samples (`TroubleshootingSamples.cs:554-570`) show a commented-out example of stubbing an internal interface with `InternalsVisibleTo`, but it uses `public partial class InternalServiceStub : IInternalService` — the cross-assembly scenario where `InternalsVisibleTo` makes this valid.

**Behavioral Contracts (Design.Tests):**

No existing Design.Tests exercise internal interface or class stubs. There are no behavioral contracts at risk of being broken by this change. The change adds new capability (correct accessibility on generated stubs) without modifying any existing generated code for public types.

**API Consistency Matrix (`docs/guides/api-consistency-matrix.md`):**

The matrix documents features across all 8 interface/class patterns. None of the 12 documented features (instantiation, methods, properties, indexers, events, sequences, when chains, verification, strict mode, reset, stub overrides, async auto-wrapping) are affected by this change. The matrix does not currently document accessibility behavior as a feature dimension. It may be worth adding a note after implementation.

**Related Documentation:**

- `docs/guides/protected-methods.md:7` states "Interface stubs have no access modifiers" — this refers to interface members (which are implicitly public), not to the interface type's accessibility. This is compatible with the proposed fix.
- `src/Tests/KnockOff.Documentation.Samples/TroubleshootingSamples.cs:554-570` — Documents the `InternalsVisibleTo` pattern for cross-assembly internal interface stubbing. This documentation is compatible with the fix but describes a different scenario (cross-assembly vs. same-assembly).

### Gaps

1. **No Design.Stubs coverage for internal types.** There are zero existing stub declarations using `internal` accessibility in `src/Design/Design.Stubs/` or `src/Design/Design.Tests/`. The architect must establish new Design.Stubs patterns demonstrating internal interface and internal class stubs.

2. **No test coverage for accessibility.** There are zero tests in `src/Tests/KnockOffTests/` that verify generated stub class accessibility. The architect should specify tests that verify correct accessibility for both `public` and `internal` target types.

3. **No coverage for `protected internal`, `private protected`, or other accessibility combinations.** The todo focuses on `internal`, but the architect should consider whether other accessibility levels (particularly `protected internal` on class members) need attention. Note: interfaces themselves can only be `public` or `internal` (or nested with additional options), but classes can have `protected`, `protected internal`, and `private protected`.

4. **No coverage for nested internal types.** An interface could be `public` but nested inside an `internal` class, making it effectively internal. The `ContainingTypeInfo` already captures accessibility of containing types (used by renderers at `FlatRenderer.cs:42-44`, `InlineRenderer.cs:40-42`, `StandaloneClassRenderer.cs:45-47`), but the generated stub class inside the `Stubs` container does not account for this.

5. **Inline Delegate (Pattern 7) not mentioned.** The todo and clarifications identify four hardcoded locations but do not explicitly address whether delegate accessibility needs the same fix. The architect should verify.

### Contradictions

None found. The proposed fix does not contradict any governing constraint or behavioral contract.

- Interceptor-as-property is preserved (no API change).
- API consistency is maintained (same fix applied across all pipelines).
- No existing Design.Tests would fail (no accessibility-related tests exist).
- No shared code (library base classes, `UnifiedInterceptorBuilder`) is affected — the change is purely in renderers.

### Recommendations for Architect

1. **All four pipelines need independent fixes.** Per the Pipeline Verification Rule, each renderer must be modified separately:
   - `FlatRenderer.cs:268` — Change `public class {ClassName}Base` to use the target type's accessibility
   - `StandaloneClassRenderer.cs:273` — Change `public class {ClassName}Base` to use the target type's accessibility
   - `InlineRenderer.cs:247` — Change `public class {StubClassName}` to use the target type's accessibility
   - `ClassRenderer.cs:118` — Change `public class {StubClassName}` to use the target type's accessibility

2. **Model changes needed.** The current models do not carry the target type's accessibility:
   - `InlineInterfaceStubModel` — no accessibility field
   - `InlineClassStubModel` — no accessibility field
   - `FlatGenerationUnit` — no target type accessibility field
   - `StandaloneClassGenerationUnit` — no target type accessibility field

   The Transform phase already resolves `DeclaredAccessibility` for containing types (`KnockOffGenerator.Transform.cs:1201-1208`). The same approach should be used for the target interface/class type, and the resolved accessibility should flow through the builder into the generation unit model.

3. **Standalone patterns (1-4): Two classes need matching accessibility.** The user's partial class declaration controls its own modifier, but the generated `{ClassName}Base` helper class is emitted by the renderer with hardcoded `public`. The `Base` class must match the accessibility of the user's stub class. Since the user can declare the stub as `internal partial class MyStub : IInternalFoo`, the `Base` class must also be `internal`.

4. **Inline patterns (5-9): The stub class is nested.** Since inline stubs are nested inside the user's `Stubs` container (which is itself nested inside the user's test class), C# allows nested types to have any accessibility regardless of the outer type. However, if the target type is `internal` and the stub class is `public`, the stub class declaration `public class IFoo : global::Namespace.IInternalFoo` will fail because a `public` class cannot inherit from an `internal` type. The fix is to emit the stub class with `internal` accessibility when the target type is `internal`.

5. **Existing documentation compatibility.** The `TroubleshootingSamples.cs:554-570` documents using `public partial class InternalServiceStub : IInternalService` with `InternalsVisibleTo`. After the fix, the standalone patterns should still support `public` stubs for internal interfaces when `InternalsVisibleTo` is used (the user explicitly chose `public`). The fix should derive the generated stub class accessibility from the user's declaration (standalone) or from the target type (inline), not force `internal` in all cases.

6. **Add Design.Stubs coverage.** Create at least one Design.Stubs example per pattern group demonstrating an internal interface/class stub to establish the behavioral contract.

7. **No shared code (library, interceptor base classes) changes required.** The interceptor base classes (`MethodInterceptorRuntime`, `PropertyGetSetInterceptor`, etc.) are unaffected. The change is purely in the renderer layer, which controls the emitted C# text.

---

## Plans

- [Internal Interface Stub Accessibility](../plans/completed/internal-interface-stub-accessibility.md)

---

## Tasks

- [x] Architect comprehension check
- [x] Business requirements review (APPROVED 2026-03-07)
- [x] Architect plan creation & design
- [x] Developer review (APPROVED 2026-03-07)
- [x] Implementation (28/28 checklist items, 8,114 tests pass)
- [x] Verification (Architect: VERIFIED, Requirements: SATISFIED)
- [x] Documentation (requirements docs, skill, release notes)

---

## Progress Log

### 2026-03-07
- Todo created from user-reported bug
- Initial investigation confirmed: `InlineRenderer.cs:247` hardcodes `public class` for inline stubs
- All 9 patterns need checking — each uses a separate pipeline (see Pipeline Verification Rule in CLAUDE.md)
- Architect comprehension check: ready to proceed, no questions
- Requirements review: APPROVED, no contradictions. 5 gaps identified (no existing coverage for internal types)
- Architect plan created: `docs/plans/internal-interface-stub-accessibility.md` -- 10 business rules, 11 test scenarios, 5 implementation steps across all 4 pipelines
- Developer review: APPROVED -- all 10 assertion traces verified, implementation contract created with 28 checklist items

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] All builds pass
- [x] All tests pass

**Verification results:**
- Build: 0 errors, 0 warnings (all TFMs)
- Tests: 8,114 passed, 0 failed, 12 skipped (pre-existing)

---

## Results / Conclusions

Fixed bug where generated stub classes were always emitted as `public`, causing CS0060 compilation errors when stubbing internal interfaces/classes/delegates. All 4 pipelines (FlatRenderer, StandaloneClassRenderer, InlineRenderer, ClassRenderer) independently fixed. 20 files modified: 10 model fields added, 5 transform resolution points, 5 builder plumbing changes, 5 renderer fixes. Design.Stubs acceptance criteria established for Patterns 1, 3, 5, 6, 7, 8. All defaults are `"public"` for backward compatibility — zero impact on existing stubs. Released as v0.55.0.
