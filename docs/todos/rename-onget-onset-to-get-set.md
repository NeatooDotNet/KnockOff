# Rename OnGet/OnSet to Get/Set in Property and Indexer APIs

**Status:** In Progress
**Priority:** High
**Created:** 2026-02-07
**Last Updated:** 2026-02-07 (plan created, developer approved)

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

- [ ] Architect explores codebase, evaluates open questions, creates plan
- [ ] Developer reviews plan and creates implementation contract
- [ ] Implement: Rename public API methods in generator renderers
- [ ] Implement: Update library interfaces and doc comments
- [ ] Implement: Update Design.Stubs and Design.Tests
- [ ] Implement: Update test projects
- [ ] Implement: Update docs, skills, samples
- [ ] Version bump (breaking change — minor per pre-1.0 convention)

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

---

## Completion Verification

Before marking this todo as Complete, verify:

- [ ] Design project builds successfully
- [ ] Design project tests pass

**Verification results:**
- Design build: [Pending]
- Design tests: [Pending]

---

## Results / Conclusions

