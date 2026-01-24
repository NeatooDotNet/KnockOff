# Unify OnCall/OnGet/OnSet Signatures to Methods

**Status:** Complete
**Priority:** High
**Created:** 2026-01-23
**Last Updated:** 2026-01-23

---

## Problem

User feedback indicates the current mix of method syntax (`OnCall()`) and property assignment syntax (`OnGet =`, `OnSet =`) is confusing. Methods use method calls returning tracking objects, while properties and indexers use settable properties with no return values.

**Current inconsistent API:**
```csharp
// Methods - method syntax with tracking return
stub.Method.OnCall(() => result).Verifiable();

// Properties - property assignment syntax (no return)
stub.Property.OnGet = () => value;
stub.Property.OnSet = (val) => { };

// Indexers - property assignment syntax (no return)
stub.Indexer.OnGet = (key) => value;
stub.Indexer.OnSet = (key, val) => { };
```

## Solution

Unify all member types to use method-based callback registration that returns tracking interfaces, enabling consistent fluent verification patterns and sequence support.

**Unified API:**
```csharp
// Methods - existing (no change)
stub.Method.OnCall(() => result).Verifiable();
stub.Method.OnCallSequence(() => first).ThenCall(() => second);

// Properties - NEW method syntax
stub.Property.OnGet(() => value).Verifiable();
stub.Property.OnSet(val => { }).Verifiable();
stub.Property.OnGetSequence(() => first).ThenGet(() => second);
stub.Property.OnSetSequence(val => { }).ThenSet(val => { });

// Indexers - NEW method syntax
stub.Indexer.OnGet(key => value).Verifiable();
stub.Indexer.OnSet((key, val) => { }).Verifiable();
stub.Indexer.OnGetSequence(key => first).ThenGet(key => second);
stub.Indexer.OnSetSequence((k, v) => { }).ThenSet((k, v) => { });
```

---

## Plans

- [Unify OnCall/OnGet/OnSet to Method Signatures](../../plans/completed/unify-oncall-onget-onset-signatures.md)

---

## Tasks

- [x] Phase 1: Create public tracking/sequence interfaces
- [x] Phase 2: Create unified models for property/indexer interceptors
- [x] Phase 3: Create shared PropertyInterceptorRenderer
- [x] Phase 4: Create shared IndexerInterceptorRenderer
- [x] Phase 5: Integrate into FlatRenderer and InlineRenderer
- [x] Phase 6: Update explicit interface implementations
- [x] Phase 7: Migrate tests
- [x] Phase 8: Update documentation
- [x] Phase 9: Update skills documentation

---

## Progress Log

- 2026-01-23: Initial discovery and architecture design complete. User approved Clean Architecture approach with full unification, tracking returns, and sequence support.

---

## Results / Conclusions

Successfully unified all callback configuration to use method syntax:

**New Files Created (8):**
- `src/KnockOff/IPropertyTracking.cs`, `IPropertySequence.cs`
- `src/KnockOff/IIndexerTracking.cs`, `IIndexerSequence.cs`
- `src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs`
- `src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs`
- `src/Generator/Model/Shared/UnifiedPropertyInterceptorModel.cs`
- `src/Generator/Model/Shared/UnifiedIndexerInterceptorModel.cs`

**Key Decisions:**
- `IPropertyGetTracking` has no `LastValue` (getter callbacks are deterministic)
- Init-only properties only have `OnGet()`/`OnGetSequence()`, no setter methods
- Renamed `RecordGet`/`RecordSet` to `InvokeGet`/`InvokeSet` for consistency
- Kept `VerifyGet()`/`VerifySet()` convenience methods on interceptors

**Test Results:** All 1,250+ tests pass across 3 projects and 3 frameworks.

**Breaking Change:** All `stub.Property.OnGet = callback` must become `stub.Property.OnGet(callback)`. Clean break acceptable for pre-1.0.
