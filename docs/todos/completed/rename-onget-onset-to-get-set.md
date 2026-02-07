# Rename OnGet/OnSet to Get/Set in Property and Indexer APIs

**Status:** Complete
**Priority:** High
**Created:** 2026-02-07
**Last Updated:** 2026-02-07

---

## Problem

KnockOff's property and indexer configuration methods use `OnGet`/`OnSet` prefixes that feel unnecessarily verbose. The "On" prefix was already dropped from the method API (`OnCall` → `Returns`/`Call`), so properties and indexers should follow the same pattern for consistency.

Current API:
```csharp
stub.Name.OnGet("John");
stub.Name.OnSet(v => captured = v);
stub.Name.OnGet(() => "First").ThenGet(() => "Second");
```

Target API:
```csharp
stub.Name.Get("John");
stub.Name.Set(v => captured = v);
stub.Name.Get(() => "First").ThenGet(() => "Second");
```

This also applies to indexer APIs (`OnGet`/`OnSet`).

**Note:** A broader API decision was made to use **singular imperative** verb forms across the entire API (`Return`/`Call`/`Get`/`Set` — not `Returns`/`Calls`/`Gets`/`Sets`). The `Returns` → `Return` rename is tracked as a separate todo.

## Solution

Rename all `On`-prefixed property and indexer configuration methods:

| Current | Target |
|---------|--------|
| `OnGet(value)` | `Get(value)` |
| `OnGet(callback)` | `Get(callback)` |
| `OnSet(callback)` | `Set(callback)` |
| `OnGetSequence(...)` | `GetSequence(...)` or remove (architect to evaluate) |
| `OnSetSequence(...)` | `SetSequence(...)` or remove (architect to evaluate) |

`ThenGet()`, `ThenSet()`, and `ThenDefault()` have no "On" prefix and stay as-is.

### Open Questions for Architect

1. **Sequence entry points:** The method API removed `OnCallSequence()` — sequences start via `Returns().ThenReturns()` chaining. Should `GetSequence()`/`SetSequence()` also be removed in favor of `Get().ThenGet()` chaining? Or keep them as separate entry points?
2. **Associated APIs:** Are there any other `On`-prefixed APIs remaining (events, delegates, etc.) that should be renamed for consistency?
3. **Internal generated code:** Should internal field names (`_onGet`, `_onSet`) and method names (`InvokeGet`, `InvokeSet`) also change, or just the public API?
4. **Verification APIs:** `VerifyGet()`/`VerifySet()` have no "On" prefix — confirm no changes needed.

### Affected Files (from API inventory)

**Public Library (src/KnockOff/):**
- `IPropertyCallBuilder.cs` — `OnGet`/`OnSet` mentioned in doc comments
- `IPropertySequence.cs` — sequence interfaces
- `IIndexerCallBuilder.cs` — indexer versions
- `IIndexerSequence.cs` — indexer sequence interfaces

**Generator (src/Generator/):**
- `PropertyInterceptorRenderer.cs` — generates `OnGet()`/`OnSet()`/`OnGetSequence()`/`OnSetSequence()` methods
- `IndexerInterceptorRenderer.cs` — generates indexer versions
- Field names: `_onGet`, `_onSet`, `_getSequence`, `_setSequence`

**Design Projects (src/Design/):**
- `Design.Stubs/Properties/PropertyBasics.cs`
- `Design.Stubs/Properties/PropertySequences.cs`
- `Design.Stubs/Indexers/IndexerBasics.cs`
- `Design.Stubs/Indexers/IndexerSequences.cs`
- All corresponding Design.Tests files

**Docs, Skills, Samples** — all references to OnGet/OnSet

---

## Plans

- [Rename OnGet/OnSet to Get/Set - Design Plan](../plans/rename-onget-onset-design.md)

---

## Tasks

- [x] Architect explores codebase, evaluates open questions, creates plan
- [x] Developer reviews plan and creates implementation contract
- [x] Implement: Rename public API methods in generator renderers
- [x] Implement: Update library interfaces and doc comments
- [x] Implement: Update Design.Stubs and Design.Tests
- [x] Implement: Update test projects
- [x] Implement: Update docs, skills, samples
- [ ] Version bump (breaking change — deferred to bundle with other API renames)

---

## Progress Log

### 2026-02-07
- Created todo
- Full API inventory completed — identified all property and indexer `On`-prefixed APIs
- Scope: properties and indexers only (method API handled by separate todos)
- Open questions documented for architect evaluation
- Invoking architect for plan creation
- Architect completed codebase analysis and created plan at `docs/plans/rename-onget-onset-design.md`
- Answered all 4 open questions (no sequence entry points exist, HasOnGet/HasOnSet rename in scope, OnCall out of scope, VerifyGet/VerifySet unchanged)
- Developer reviewed and approved plan, created implementation contract
- **API verb form decision:** All APIs should use singular imperative (`Return`/`Call`/`Get`/`Set`), not plural third-person (`Returns`/`Calls`/`Gets`/`Sets`). `Returns` → `Return` tracked as separate todo.
- Plan status: Ready for Implementation
- Implementation completed across 124 files in 5 parallel phases
- All 7,699 tests pass across net8.0/net9.0/net10.0
- Zero `.OnGet(`/`.OnSet(` references remain in active source code
- PR #54 merged to main

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] Design project builds successfully
- [x] Design project tests pass

**Verification results:**
- Design build: 0 errors, 0 warnings
- Design tests: 259 passed, 0 failed (net8.0/net9.0/net10.0)
- Full solution: 7,699 passed, 0 failed

---

## Results / Conclusions

Successfully renamed all `OnGet`/`OnSet` property and indexer configuration methods to `Get`/`Set` across the entire codebase. Also renamed `HasOnGet`/`HasOnSet` internal properties to `HasGet`/`HasSet`. The rename was mechanical — no behavioral changes, no new functionality. All 8 applicable patterns (Inline Delegate excluded) are updated via shared renderers.

Key decisions made during this work:
- `OnGetSequence`/`OnSetSequence` don't exist as methods (only stale doc comments) — corrected comments
- Private fields (`_onGet`, `_onSet`) left unchanged — not user-facing
- `VerifyGet`/`VerifySet` unchanged — no "On" prefix
- Broader API verb form decision: all verbs should be singular imperative (`Return`/`Call`/`Get`/`Set`). `Returns` → `Return` tracked as separate todo.
