# Indexer API Redesign

**Status:** Complete
**Priority:** High
**Created:** 2026-02-09
**Last Updated:** 2026-02-09

---

## Problem

The current indexer API has several usability issues identified from developer feedback and Rocks comparison:

1. **`Backing` dictionary is verbose** — `stub.Indexer.Backing[3] = 42` instead of something simpler
2. **`OfXxx` pattern for multiple indexers** — When an interface has `this[int]` and `this[string]`, users must write `stub.Indexer.OfInt32` and `stub.Indexer.OfString`. Not discoverable.
3. **No per-key configuration** — Rocks allows `Setups[3].Gets().ReturnValue(42)` to match only key 3. KnockOff's `Indexer.Get(callback)` handles ALL keys. Users must write conditional callbacks as workaround.
4. **Tuple keys for multi-param indexers** — `stub.Indexer.Get(((int row, int col) key) => key.row + key.col)` received pushback from developers. Unnatural compared to the interface declaration.

## Solution

Redesign the indexer API with three key changes:

### 1. Flattened indexer accessor (callbacks keep tuples)

Multi-param indexers use flattened C# indexer on the interceptor class, but callbacks use tuple TKey via library interfaces:
```csharp
// Interface: double this[int row, int col] { get; set; }
stub.Indexer[1, 2].Returns(3.14);                                    // flattened indexer accessor
stub.Indexer.Get(((int row, int col) key) => key.row + key.col);    // tuple callback (unchanged)
```

### 2. Indexer on the interceptor (replaces Backing and OfXxx)

The interceptor class exposes indexer overloads matching each interface indexer. C# overload resolution disambiguates by key type — no OfXxx needed:
```csharp
// Interface has this[int] and this[string]
stub.Indexer[3].Returns(42);           // int indexer — resolved by key type
stub.Indexer["foo"].Returns("bar");    // string indexer — resolved by key type
```

### 3. Per-key configuration via indexer

`stub.Indexer[key]` returns a per-key builder enabling Rocks-style per-key setup:
```csharp
stub.Indexer[3].Returns(42);              // fixed value for key 3
stub.Indexer[3].Get(() => ComputeIt());   // callback for key 3
stub.Indexer.Get((int key) => key * 10);  // fallback for all other keys
```

### 4. Callback overloads for all-keys configuration

Get/Set callbacks are overloaded by delegate type. The compiler resolves which indexer based on the callback signature:
```csharp
stub.Indexer.Get((int key) => key * 10);            // int indexer
stub.Indexer.Get((string key) => key.ToUpper());     // string indexer
stub.Indexer.Set((int key, int value) => { });       // int setter
```

### Design principles

- **C# indexer overloads require unique parameter types** — key types are always unique per interface, so the type system handles all disambiguation
- **Flattened indexer accessor matches the interface** — `stub.Indexer[1, 2]` not `stub.Indexer[(1, 2)]`
- **Callbacks keep tuple TKey** — library interfaces remain unchanged; callbacks use named tuples for ergonomics
- **Per-key config > Backing dictionary** — explicit per-key setup is clearer than pre-populating a dictionary

### Priority chain

1. Per-key config (`stub.Indexer[3].Returns(42)`) — exact match
2. All-keys callback (`stub.Indexer.Get(...)`) — fallback
3. Source delegation (if configured)
4. Strict mode throws / return default

---

## Plans

- [Indexer API Redesign Plan](../plans/indexer-api-redesign.md)

---

## Tasks

- [x] Create implementation plan (architect)
- [x] Developer review
- [x] Implement new indexer API
- [x] Update Design projects with new API examples
- [x] Update existing tests
- [x] Update documentation

---

## Progress Log

### 2026-02-09
- Brainstormed new API design with user
- Key decisions: flattened params (no tuples), indexer on interceptor (replaces Backing + OfXxx), per-key builders via indexer
- Created todo
- Architect created implementation plan at `docs/plans/indexer-api-redesign.md`
- Plan covers: per-key builder design, multi-indexer overloads, flattened params, priority chain, all 9 patterns, 7 implementation phases
- Developer review raised 9 concerns (2 blocking, 4 design concerns, 3 minor)
- Blocking: (1) No Design.Stubs verification evidence, (2) Unresolved multi-param library interface design
- Key concerns: ref-return multi-indexer _refReturnBacking, IsConfigured semantics with lazy builders, open questions unresolved
- Architect addressed all 9 concerns with user clarification: multi-param flattening applies ONLY to indexer accessor, not callbacks
- Key decisions: library interfaces unchanged, tuple TKey for callbacks, Returns/ThenReturns naming intentional, IsConfigured checks config state not builder existence
- Failing acceptance criteria written in `src/Design/Design.Stubs/Indexers/IndexerRedesignAcceptance.cs` (57 compile errors)
- All 4 open questions resolved (no per-key tracking in V1, underscore-separated InvokeGet suffix, no ThenDefault on per-key, independent sequence tracking)
- Plan status set to "Under Review (Developer)" for second pass
- Developer second review: APPROVED. All 9 concerns satisfactorily addressed.
- Implementation contract created with 7 phases, 12 Design.Stubs acceptance criteria, explicit verification gates and stop conditions
- Plan status set to "Ready for Implementation"
- Developer implemented all 7 phases (model changes, renderer rewrite, builder updates, renderer updates, test updates, design updates, documentation)
- Architect verification: VERIFIED — all tests pass (1410/1411 KnockOffTests, 599 Documentation.Samples, 473 NeatooInterfaceTests, 356 Design.Tests, 14 AssemblyStrict) x 3 TFMs, 0 failures
- All 12 Design.Stubs acceptance criteria compile
- Zero Backing/OfXxx references remain in generated code

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] Design project builds successfully
- [x] Design project tests pass

**Verification results:**
- Design build: 0 errors (verified by architect)
- Design tests: 356 tests pass x 3 TFMs (verified by architect)

---

## Results / Conclusions

Major indexer API redesign completed:

1. **Backing dictionary removed** — replaced with per-key builders: `stub.Indexer[key].Returns(value)`. Cleaner, more explicit API.

2. **OfXxx pattern removed** — multi-indexer interfaces now use C# indexer overloads on the interceptor. The compiler resolves by key type. No more `.OfInt32` / `.OfString`.

3. **Per-key configuration added** — `stub.Indexer[3].Returns(42)` for fixed values, `.Get(() => ...)` for per-key callbacks, `.ThenReturns()` for per-key sequences. Priority: per-key > all-keys callback > source delegation > strict/default.

4. **Multi-param indexers flattened** — `stub.Indexer[1, 2]` instead of `stub.Indexer[(1, 2)]`. Only the indexer accessor is flattened; callbacks and library interfaces keep tuple TKey.

5. **Library interfaces unchanged** — `IIndexerGetBuilder<TKey, TValue>`, sequences, and tracking interfaces all preserved. Tuple TKey works for multi-param callbacks.

6. **`Returns`/`ThenReturns` naming** — intentional divergence from method API's `Return`/`ThenReturn`. Per-key Returns is declarative ("this key returns this value"), matching Moq/NSubstitute convention.

**Breaking changes:** All existing indexer usage (`Backing`, `OfXxx`) must migrate to new API. Migration patterns documented in the plan.
