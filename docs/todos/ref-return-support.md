# Add Ref Return Support to Generator

**Status:** In Progress
**Priority:** Medium
**Created:** 2026-02-08
**Last Updated:** 2026-02-08

---

## Problem

KnockOff supports ref/out **parameters** fully, but ref **returns** (`ref int GetValue()`, `ref readonly int GetValue()`) are completely broken. The generator silently strips the `ref`/`ref readonly` modifier from return types, producing non-compiling stubs.

This affects three member types:
- **Methods**: `ref int GetValueRef()` generates as `int GetValueRef()`
- **Properties**: `ref int Value { get; }` generates as `int Value { get; }`
- **Indexers**: `ref int this[int index] { get; }` generates as `int this[int index] { get; }`

### Root Cause

The generator's model stores `ReturnType` as a plain string with no ref metadata. `ReturnsByRef` and `ReturnsByRefReadonly` from Roslyn's `IMethodSymbol`/`IPropertySymbol` are never captured in the transform layer. Since the information is lost upstream, all downstream pipelines (FlatModelBuilder, InlineModelBuilder, StandaloneClassModelBuilder) are affected identically.

### Compilation Errors

Three distinct error patterns (120 total errors across 3 TFMs from exploratory tests):

| Error | Member Type | Description |
|-------|-------------|-------------|
| CS0539 | Methods, Properties, Indexers | Explicit interface member not found (signature mismatch due to missing `ref`) |
| CS0535 | Methods, Indexers | Interface member not implemented (follow-on from CS0539) |
| CS8152 | Properties | Cannot implement — return by reference mismatch |

### What Works

Normal members on the same interface as ref return members generate correctly. The ref return bug does not corrupt adjacent normal member generation. A mixed interface with both normal and ref return members will fail to compile, but only because of the ref return members.

## Solution

1. Capture `ReturnsByRef` and `ReturnsByRefReadonly` from Roslyn symbols during the transform phase
2. Store ref return metadata in the generator's model (method model, property model, indexer model)
3. Emit `ref` / `ref readonly` prefix on return types in:
   - Explicit interface implementations
   - Interceptor delegates
   - User method signatures (standalone stubs)
4. Handle the fundamental challenge: ref returns require storage backing (can't return `ref` to a local variable), which may require a different interceptor pattern than value returns

### Design Challenges

- **Storage**: `ref int` returns need a field/array to reference. The current interceptor pattern returns computed values from delegates — these can't be returned by ref.
- **Callbacks**: `delegate ref int GetValueRefDelegate()` requires the callback itself to return a ref, which limits what callers can do.
- **Smart defaults**: Default returns for ref return members need backing storage.
- **Potential approach**: May need to store values in fields and return refs to those fields, rather than using the delegate-invoke pattern used for normal returns.

---

## Plans

- [Ref Return Support Design](../plans/ref-return-support.md)

---

## Tasks

- [x] Add exploratory ref return interfaces to TestInterfaces.cs (4 interfaces, 4 standalone stubs)
- [x] Create RefReturnTests.cs with inline stubs and comprehensive test class
- [x] Build and document compilation failures (120 errors, 3 error patterns confirmed)
- [x] Design ref return interceptor pattern (architect)
- [ ] Implement ref return support in transform layer (capture ReturnsByRef/ReturnsByRefReadonly)
- [ ] Implement ref return support in model (add ref return metadata)
- [ ] Implement ref return support in builders and renderers (all pipelines)
- [ ] Get exploratory tests compiling and passing
- [ ] Add Design project examples for ref returns
- [ ] Update documentation/skills

---

## Progress Log

- 2026-02-08: Created exploratory tests in `RefReturnTests.cs` and interfaces in `TestInterfaces.cs`. Confirmed 120 compilation errors across all 4 interfaces, both standalone and inline patterns. Root cause: generator strips ref/ref readonly from return types at the transform layer. Normal members adjacent to ref return members are unaffected.
- 2026-02-08: Architecture plan created at `docs/plans/ref-return-support.md`. Selected "backing field in interceptor" approach: interceptors get `_refReturnBacking` field and `InvokeRef`/`InvokeRefGet` methods. User-facing API (Return, Call, sequences, verification) stays identical to non-ref methods.

---

## Results / Conclusions

