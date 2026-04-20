# Property `new` narrowing fix — union accessors on shared interceptors

**Date:** 2026-04-20
**Related Todo:** [property-new-narrowing-bug](../todos/property-new-narrowing-bug.md)
**Status:** Complete
**Last Updated:** 2026-04-20 (requirements documented)

---

## Overview

Fix a generator bug where shadowed property declarations across an interface hierarchy (using the C# `new` modifier) produce uncompilable stubs. The interceptor's accessor set must be the union of accessors across all shadowed declarations that share a name.

---

## Current Behavior Map

**Pipeline 1 — Inline interfaces (patterns 5, 7, 8):**
- `src/Generator/Builder/InlineModelBuilder.cs:102-126` — deduplicates property members by name, **keeping the first** encountered.
- `src/Generator/Builder/InlineModelBuilder.cs:235-263` (`BuildPropertyModel`) — copies `HasGetter`/`HasSetter` straight from the one dedup'd member.
- `BuildImplementations` then iterates **every** interface member independently to emit explicit interface implementations — implementations use each member's own accessor set.
- Result: when Narrow (get-only) is first and Wide (get/set) is second, the interceptor is typed `PropertyGetInterceptor<T>` but an `IInterfaceWide.Prop` setter is still emitted, calling `.InvokeSet(...)` which doesn't exist.

**Pipeline 2 — Flat (patterns 1, 2):**
- `src/Generator/Builder/FlatModelBuilder.cs:270-339` (`BuildPropertyModels`) — emits one `FlatPropertyModel` **per (interface, name)** pair. All models for the same name share the same `InterceptorName` via `nameMap`.
- `src/Generator/Renderer/FlatRenderer.cs:79-99` — iterates `unit.Properties`; the first to reach an unseen `InterceptorClassName` wins and drives the pre-compiled interceptor type via `ModelAdapters.ToUnifiedPropertyModel(prop)` → `GetPropertyInterceptorType(unifiedModel)`.
- **There is unrelated widest-accessor logic** at `FlatRenderer.cs:319-348`, but that only chooses which **stub-override base class** `protected virtual` property to emit — it does NOT touch interceptor-type selection. The interceptor bug is present in Flat too (confirmed by `NarrowingStandaloneStub.g.cs` which types `Prop` as `PropertyGetInterceptor<int>` while emitting an `IInterfaceWide.Prop` setter calling `InvokeSet`).

**Invariant (preserved):**
- Explicit interface implementations **must** match the accessor set declared on each interface. C# enforces this — we cannot "collapse" two shadowed declarations into one implementation.
- `stub.Prop` must remain a property exposing an interceptor (interceptor-as-property principle).
- Narrow/wide `InvokeGet(Strict)` and `InvokeSet(Strict, value)` routing continues through the single shared interceptor.

---

## Out of Scope / Invariants

- Class-based stub patterns (3, 4, 6, 9) — `new` narrowing through a class hierarchy is structurally different and is out of scope for this fix. Deferred.
- Indexers with shadowed declarations — deferred (different dedup key: key-type signature; additional renderer code path).
- Methods / events — not affected by this specific bug (methods group by name but interceptor dispatch is per-signature).
- Verification API for the narrow interface's hidden setter — when the stub is accessed through `IInterfaceNarrow`, the hidden setter is not reachable; `stub.Prop.VerifySet(...)` continues to verify any set that happened via `IInterfaceWide`. No API change.
- Source(T) delegation behavior for shadowed properties — ~~already wires per-interface source fallbacks; we don't change this.~~ **REVISED during implementation:** the same per-face-vs-union fallacy applies here. See Design Decisions entry for Fix #3.
- The `Object` property type — already chooses the declared type of the generic argument (`IInterfaceNarrow`).

---

## Fallacy

- **What we believed:** Within one interface-based stub, every declaration of a property name has the same accessor set, so picking the first declaration's `HasGetter`/`HasSetter` is safe for the single shared interceptor.
- **What is actually true:** C# interface hierarchies can `new`-shadow a property with a narrower or wider accessor set (and a derived-type narrowing or widening). The shared interceptor must expose the union of accessors required across all declarations that share its name, because each explicit interface implementation routes through that one interceptor.
- **Downstream consequences:**
  - Interceptor-type selection for a shared-name interceptor must consult *all* shadowed declarations, not just the first.
  - Explicit interface implementations still need their own per-declaration accessor set (unchanged).
  - Any future dedup-by-name logic (e.g., for indexer shadowing, which is technically legal via `new`) has the same latent bug.

---

## Approach

Make the interceptor a function of the **union** of shadowed declarations, not of the first.

Two small, local changes — one per pipeline — both at the moment the interceptor's accessor set is read:

1. **Inline pipeline — at the Builder level.** Group shadowed property members by name, produce a synthesized `InterfaceMemberInfo` whose `HasGetter`/`HasSetter` are the union, and feed that into `BuildPropertyModel`. `BuildImplementations` keeps iterating raw members (unchanged).
2. **Flat pipeline — at the Renderer level.** Group `unit.Properties` by `InterceptorClassName`, pick a representative with the union accessor set, and render the interceptor class / pre-compiled type from that representative. `RenderExplicitImplementations` / base-class rendering continues to use each model's own accessors (unchanged).

No model-shape changes. No renderer refactors. No new generator diagnostics.

---

## Design

### Fix #1 — `InlineModelBuilder.cs`

Change the dedup block (currently `InlineModelBuilder.cs:102-126`) from "keep first by name" to "merge accessors across all members sharing the name":

```csharp
// Properties: group by name, pick a primary member, union HasGetter/HasSetter across shadowed declarations.
var propertyMembersByName = new Dictionary<string, InterfaceMemberInfo>();
// Indexers dedup unchanged — see Deferred Scope for shadowed-indexer handling.
var processedIndexerKeys = new HashSet<string>();
var deduplicatedPropertyMembers = new List<InterfaceMemberInfo>();
foreach (var member in iface.Members)
{
    if (!(member.IsProperty || member.IsIndexer)) continue;

    if (member.IsIndexer)
    {
        var indexerKey = string.Join(",", member.IndexerParameters.Select(p => p.Type));
        if (processedIndexerKeys.Add(indexerKey))
            deduplicatedPropertyMembers.Add(member);
        continue;
    }

    if (propertyMembersByName.TryGetValue(member.Name, out var existing))
    {
        propertyMembersByName[member.Name] = existing with
        {
            HasGetter = existing.HasGetter || member.HasGetter,
            HasSetter = existing.HasSetter || member.HasSetter,
            // IsInitOnly stays with the primary (first seen) — a later shadow cannot widen to init-only-settable beyond what C# already allows here.
        };
    }
    else
    {
        propertyMembersByName[member.Name] = member;
    }
}
deduplicatedPropertyMembers.AddRange(propertyMembersByName.Values);
```

`BuildImplementations` is untouched; it walks raw `iface.Members` and emits each declaration's own accessors.

**`InterfaceMemberInfo` must be a record with `with`-compatible `HasGetter`/`HasSetter` init setters.** Verify in implementation; if it's not, convert to a record or build a new instance.

### Fix #2 — `FlatRenderer.cs`

Before the interceptor-rendering loop at `FlatRenderer.cs:79`, pick a widest-accessor representative per `InterceptorClassName`:

```csharp
// Before rendering interceptor classes: pick the widest-accessor representative per interceptor name.
// (Shadowed `new` properties across an interface hierarchy may share one interceptor.)
var propertyRepresentatives = unit.Properties
    .Where(p => p.DelegationTarget == null)
    .GroupBy(p => p.InterceptorClassName)
    .Select(g =>
    {
        var hasGetter = g.Any(p => p.HasGetter);
        var hasSetter = g.Any(p => p.HasSetter);
        var first = g.First();
        return first with { HasGetter = hasGetter, HasSetter = hasSetter };
    })
    .ToList();

foreach (var prop in propertyRepresentatives)
{
    if (renderedInterceptorClasses.Add(prop.InterceptorClassName))
    {
        var unifiedModel = ModelAdapters.ToUnifiedPropertyModel(prop);
        // ... existing code, unchanged
    }
}
```

The existing per-member iteration for emitting explicit interface implementations is untouched.

**If `FlatPropertyModel` is not a record, fall back to constructing a new instance with overridden accessors.**

### Why not a single shared helper?

The two pipelines have different model types (`InterfaceMemberInfo` vs `FlatPropertyModel`) and different shapes (Builder-level raw members vs Renderer-level grouped models). A shared helper would be more plumbing than the two 10-line blocks above.

### What this does NOT change

- `InlineRenderer` — no changes. It consumes `InlinePropertyModel` which now carries union accessors.
- Explicit interface implementations — unchanged. They iterate raw per-interface members.
- `Source<T>(...)` fallbacks — unchanged.
- Interceptor class internals (`_getCount`, `_setCount`, `LastSetValue`) — union accessors mean both `_getCount` and `_setCount` / `LastSetValue` fields are rendered, matching what both shadowed implementations need.

---

## Business Rules (Testable Assertions)

1. WHEN a stub exposes an interface with `new`-shadowed properties and any shadowed declaration has a setter, THEN the stub's `Prop` interceptor SUPPORTS `InvokeSet` (compiles, and `stub.Prop.Set(callback)` is callable) — Source: NEW (direct consequence of the interceptor-as-property principle).
2. WHEN a stub exposes an interface with `new`-shadowed properties and any shadowed declaration has a getter, THEN the stub's `Prop` interceptor SUPPORTS `InvokeGet` — Source: NEW (symmetric).
3. WHEN the stub is used as `IInterfaceNarrow` and `Prop` is read, THEN the shared interceptor's getter callback is invoked — Source: existing inline-routing rule.
4. WHEN the stub is used as `IInterfaceWide` and `Prop` is written, THEN the shared interceptor's setter callback is invoked AND `stub.Prop.LastSetValue` reflects the value — Source: existing inline-routing rule.
5. WHEN `VerifySet(Called.Once)` is called on a stub whose hidden-set property was set exactly once via `IInterfaceWide`, THEN verification passes — Source: existing verification rule.
6. WHEN an interface has no shadowing (single declaration per property name), THEN generated code is byte-for-byte identical to pre-fix generated code — Source: NEW (regression constraint).
7. WHEN pattern 1 (`[KnockOff]`), pattern 5 (`[KnockOff<I>]`), pattern 7/8 (open generics) expose the shadowed hierarchy, THEN each produces a compiling stub with identical routing semantics (Rules 1–5 hold) — Source: API Consistency Principle (CLAUDE.md).

### Test Scenarios

| # | Scenario | Inputs / State | Rule(s) | Expected Result |
|---|----------|----------------|---------|-----------------|
| 1 | Inline stub, narrow-first hierarchy | `[KnockOff<IInterfaceNarrow>]`, Narrow is `new int Prop { get; }`, Wide is `int Prop { get; set; }` | 1, 2 | Generated code compiles. `stub.Prop` is `PropertyGetSetInterceptor<int>`. |
| 2 | Standalone stub, same hierarchy | `[KnockOff] partial class S : IInterfaceNarrow` | 1, 2, 7 | Generated code compiles. `stub.Prop` supports `Set` and `Get`. |
| 3 | Get routing via narrow face | Configure `stub.Prop.Get(42)`, cast to `IInterfaceNarrow`, read `Prop` | 3 | Reads `42`. |
| 4 | Set routing via wide face | Cast to `IInterfaceWide`, assign `Prop = 7` | 4 | `stub.Prop.LastSetValue == 7`. |
| 5 | VerifySet via wide face | Assign once, `stub.Prop.VerifySet(Called.Once)` | 5 | Passes. |
| 6 | Widening shadow (opposite direction) | Wide is `get`-only, Narrow adds `new int Prop { get; set; }` | 1, 2 | Compiles. Interceptor supports both. |
| 7 | Non-shadowed control | Existing `IEntity` stub (`Id`, `Description`, etc.) | 6 | No generated-code diff vs pre-fix. |
| 8 | Open-generic interface | `[KnockOff(typeof(IShadowed<>))]` over a generic narrow/wide pair | 7 | Compiles, routes correctly. |
| 9 | Generic standalone (pattern 2) | `[KnockOff] partial class S<T> : IShadowed<T>` where `IShadowed<T>` shadows a base with differing accessors | 1, 2, 7 | Compiles, routes correctly. |

---

## Domain Model Behavioral Design

_N/A — generator-only change; no runtime domain model._

---

## Design Decisions

### 2026-04-20
- **Decision:** Union accessors for the shared interceptor; keep per-declaration accessors for explicit interface implementations.
- **Alternative considered:** Emit **separate interceptor objects** per shadowed declaration (`stub.Prop_Narrow`, `stub.Prop_Wide`).
- **Reason:** Rejected — violates interceptor-as-property, breaks user intuition (one property name = one interceptor), and forces API consumers to know about shadowing. Union is transparent and preserves all existing APIs.

### 2026-04-20
- **Decision:** Fix at two locations (`InlineModelBuilder` and `FlatRenderer`), not one shared helper.
- **Alternative considered:** Extract a `WidestAccessorPicker` shared between pipelines.
- **Reason:** Different model types (`InterfaceMemberInfo` vs `FlatPropertyModel`) and different pipeline stages. Shared helper would be more code than the two 10-line fixes.

### 2026-04-20 — Fix #3 (discovered during implementation)
- **Decision:** Extend the fix to source-fallback emission (`SourceMemberMapping`, `PreCompiledInterceptorRenderer.GetPropertySourceFallbackExpression`, `FlatRenderer.cs:1792-1805`, `InlineRenderer.cs:1515-1519`). The interceptor holds the UNION of accessors; a source face (`Source(IInterfaceNarrow)`) may have a narrower accessor set than the interceptor.
- **Fallacy surface:** Same per-face-vs-union fallacy as Fixes #1 and #2. The original plan's Out-of-Scope line "Source(T) delegation — we don't change this" was written before the union interceptor existed; once the interceptor's overload becomes the 2-arg `SetSourceFallback(Func?, Action?)`, a 1-arg call no longer compiles.
- **Approach:** Added `SourceHasGetter`/`SourceHasSetter` (nullable) to `SourceMemberMapping` (per-source-face flags for delegate payload) and `interceptorHasGetter`/`interceptorHasSetter` (nullable) to `GetPropertySourceFallbackExpression` (for overload selection). When the interceptor is union but the source face is narrower, emit `SetSourceFallback(getterOrNull, setterOrNull)` that matches the union interceptor's overload.
- **Alternative considered:** Split into a follow-up todo. Rejected — the bug surface is the same fallacy and would leave the feature half-fixed (stubs compile but `Source(IInterfaceNarrow)` fails to compile).

### 2026-04-20
- **Decision:** Scope limited to interface-based patterns (1, 2, 5, 7, 8). Class patterns and indexers deferred.
- **Alternative considered:** Fix all pipelines in one todo.
- **Reason:** Class hierarchies hit `new` through a different code path (`StandaloneClassModelBuilder`, `ClassRenderer`). Indexer shadowing uses a different dedup key. Each is a separate piece of work with its own repros.

---

## Skills

- `skills/knockoff/` — KnockOff stub patterns, interceptor surface, pipelines
- `.claude/rules/design-source-of-truth.md` — Design.Stubs is authoritative
- `.claude/rules/production-code.md` — Keep Design projects in sync with generator changes

---

## Implementation Steps

1. Verify `InterfaceMemberInfo` supports `with` (is a record). If not, adapt the fix to construct a new instance. _(tiny investigation, no user-visible change)_
2. Apply Fix #1 to `InlineModelBuilder.cs` (property dedup → union accessors).
3. Build `Design.Stubs` for net8/net9/net10 — inline repro (`NarrowingInlineStub`) must compile.
4. Apply Fix #2 to `FlatRenderer.cs` (interceptor representative picker).
5. Build again — standalone repro (`NarrowingStandaloneStub`) must compile.
6. Extend Design.Stubs repro file to exercise all test scenarios (routing, VerifySet, widening direction, open-generic pattern 8, generic standalone pattern 2). Add `DESIGN DECISION` comments explaining that the shared interceptor's accessor set is the union of shadowed declarations.
7. Add Design.Tests assertions covering scenarios 3–6 with **runtime routing assertions** (not just compilation): configure Get/Set via one face, read/write via the other, assert `LastSetValue` and `VerifySet` reflect actual invocations.
8. Regression check (operational, not snapshot-based): Design.Stubs builds clean across net8/net9/net10, and pre-existing Design.Tests (`PropertyBasicsTests`, `PropertySequenceTests`) pass unchanged. No snapshot-comparison infrastructure is introduced — the proxy is "non-shadowed tests continue to pass."
9. Run full solution build + test (`src/KnockOff.sln`) — no regressions.
10. Before marking complete: file follow-up todos for (a) class-pattern shadowed properties (patterns 3, 4, 6, 9) and (b) shadowed indexers. Link both from this plan's Deferred Scope.

---

## Acceptance Criteria

- [ ] `[KnockOff<IInterfaceNarrow>]` and `[KnockOff] partial class … : IInterfaceNarrow` both produce compiling stubs.
- [ ] `stub.Prop.Get(...)` and `stub.Prop.Set(...)` are both callable when any shadowed declaration has the corresponding accessor.
- [ ] Routing works through both `IInterfaceNarrow` (getter) and `IInterfaceWide` (setter).
- [ ] `VerifyGet` / `VerifySet` / `LastSetValue` behave identically to non-shadowed properties.
- [ ] No change to generated code for non-shadowed interfaces (regression check).
- [ ] Design.Tests pass on net8, net9, net10.
- [ ] `src/KnockOff.sln` builds clean (TreatWarningsAsErrors) and all tests pass.

---

## Deferred Scope

- Class-based stub patterns (3, 4, 6, 9) with `new` narrowing through a class hierarchy — separate pipelines (`StandaloneClassModelBuilder`, `ClassRenderer`); file a follow-up if a repro surfaces. — 2026-04-20 — different pipelines.
- Indexers with shadowed declarations (`new this[int]` with different accessors across interfaces) — different dedup key (key-type signature); no repro reported. — 2026-04-20 — scope.
- Events with shadowed declarations — not known to be affected; no repro. — 2026-04-20 — scope.
- Generator diagnostic warning for ambiguous shadowing — potentially useful but out of scope; the fix silently merges. — 2026-04-20 — scope.

---

## Dependencies

- None external. Internal only: `InlineModelBuilder.cs`, `FlatRenderer.cs`, plus Design.Stubs and Design.Tests additions.

---

## Risks / Considerations

- **`with` on `InterfaceMemberInfo`** — if it is a class, Fix #1 must build a new instance. Implementation step 1 verifies.
- **Source fallback ordering** — `Source(IInterfaceNarrow)` currently nulls the setter fallback; `Source(IInterfaceWide)` currently nulls the getter fallback. Neither changes with this fix (verify in review).
- **`IsInitOnly` on a shadowed property** — unlikely (init-only is used on write-capable members), but worth a sanity check: if Wide has `init` and Narrow has `get`-only, the union must keep `init` semantics intact. Test scenario 6 covers.
- **Covariant return types** — unrelated; C# interface covariance is a different mechanism and is not touched.

---

## Documentation

**Completed:** 2026-04-20
**Documenter:** knockoff-requirements-documenter

### Files Updated

- `docs/guides/api-consistency-matrix.md` — Added a "Shadowed Properties (C# `new` modifier)" subsection under Feature 3 (Property Interception) documenting union-accessor semantics, listing supported interface patterns (1, 2, 5, 8) and flagging the class-pattern gap (3, 4, 6, 9) with a link to the follow-up todo. Amended the Summary row for Property Interception to note the class-pattern carve-out.
- `docs/guides/properties.md` — Added a "Shadowed Properties (C# `new` modifier)" section describing union-accessor behavior, routing rules across faces, and the per-face source-fallback rule. Links to the deferred follow-up todos.

### Design Project Consistency

- `src/Design/Design.Domain/Entities/IInterfaceNarrow.cs` — confirmed. Contains `IInterfaceWide`/`IInterfaceNarrow` (narrow-first), `IGetOnly`/`IGetSetFromGetOnly` (widening), and `IShadowedBase<T>`/`IShadowed<T>` (open generic).
- `src/Design/Design.Stubs/Properties/NarrowingPropertyRepro.cs` — confirmed. Covers patterns 1, 2, 5, 8 with narrow-first, widening, open-generic, and generic-standalone stubs. `DESIGN DECISION` header explains union-accessor semantics and the rejected `Prop_Narrow`/`Prop_Wide` alternative.
- `src/Design/Design.Tests/PropertyTests/NarrowingPropertyTests.cs` — confirmed. Ten runtime routing tests cover scenarios 3–6 and 8–9 from the plan (Get via narrow face, Set via wide face, VerifyGet/VerifySet counts, LastSetValue, widening direction, open-generic pattern 8, generic standalone pattern 2).

### Developer Deliverables

- [ ] `src/Tests/KnockOff.Documentation.Samples/` — the new "Shadowed Properties" sections in `api-consistency-matrix.md` and `properties.md` currently use an inline illustrative code block (not a `<!-- snippet: -->` marker). Per `.claude/rules/documentation-snippets.md`, C# code blocks must be sourced from Documentation.Samples. Add a `shadowed-properties-*` region to Documentation.Samples exercising the narrow-first hierarchy and swap the inline block for snippet markers, then run `dotnet mdsnippets`.

### Discrepancies Found

None. All assertions in the plan's Business Rules are backed by Design.Stubs code (`NarrowingPropertyRepro.cs`) and Design.Tests (`NarrowingPropertyTests.cs`).
