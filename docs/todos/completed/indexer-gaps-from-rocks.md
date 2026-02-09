# Indexer Gaps Identified from Rocks Testing

**Status:** Complete
**Priority:** High
**Created:** 2026-02-08
**Last Updated:** 2026-02-08

---

## Problem

Comparing KnockOff with the Rocks mocking library revealed 4 indexer-related gaps. Three are generator bugs that produce compile errors, one is a design difference.

### Gap #3: Multi-parameter indexers (BUG)

Interfaces with `this[int a, string b]` fail to generate correct stubs.

**Inline stubs (patterns 5-9):** Generator produces code but `ThenGet`/`ThenSet` nested builder classes have signature mismatches. Generated: `ThenGet(Func<int, string, int>)` (flattened params). Interface expects: `ThenGet(Func<(int a, string b), int>)` (tuple key). Results in CS0535.

**Standalone stubs (patterns 1-4):** `FlatModelBuilder` only extracts the first indexer parameter, generating `this[int]` instead of `this[int, string]`. Results in CS0539 + CS0535 - doesn't implement the interface at all.

### Gap #4: Init-only indexers (BUG)

Interfaces with `int this[int a] { get; init; }` or `int this[int a] { init; }` fail. Generator emits `set` where `init` is required. Results in CS8855: "set and init should both be init-only or neither." Note: KnockOff already handles `init` correctly for **properties** - this is specifically missing for **indexers**.

### Gap #5: Argument-specific indexer configuration (DESIGN DIFFERENCE)

Rocks allows per-key setup: `expectations.Setups[3].Gets().ReturnValue(42)` matching only key 3. KnockOff's `Indexer.Get(callback)` handles ALL keys via callback. This is a philosophical difference, not a bug. KnockOff's callback approach is more flexible but less declarative.

### Gap #17: Multi-parameter indexers with params arrays (BUG)

Interfaces with `int this[int a, params string[] b] { get; }` fail with the same root causes as Gap #3:
- Inline: ThenGet/ThenSet signature mismatch (CS0535) with key type `(int a, string[] b)`
- Standalone: Only generates single-param indexer (CS0539)

## Solution

Fix the three generator bugs (Gaps #3, #4, #17). Document Gap #5 as a design difference.

**Root causes to fix:**
1. **FlatModelBuilder** - only extracts first indexer parameter (standalone patterns)
2. **IndexerInterceptorRenderer** - ThenGet/ThenSet callback signatures don't match tuple-based library interfaces (inline patterns)
3. **Both renderers** - emit `set` instead of `init` for indexer accessors

---

## Plans

- [Fix Indexer Gaps: Multi-Param and Init-Only](completed/fix-indexer-gaps.md)

---

## Tasks

- [x] Reproduce all gaps with test interfaces and stubs
- [x] Identify root causes in generator code
- [x] Create implementation plan (architect)
- [x] Developer review
- [x] Fix Gap #4: init-only indexer support
- [x] Fix Gap #3/#17: multi-param indexer support (inline - ThenGet/ThenSet)
- [x] Fix Gap #3/#17: multi-param indexer support (standalone - FlatModelBuilder)
- [x] Verify all reproduction tests pass
- [x] Update Design projects with multi-param indexer examples

---

## Progress Log

### 2026-02-08
- Created reproduction test file: `src/Tests/KnockOffTests/IndexerGapReproductionTests.cs`
- Build produces 54 errors (18 per TFM x 3 TFMs) confirming all 4 gaps
- Identified 3 root causes in generator pipeline
- Gap #5 documented as design difference with workaround pattern in tests
- Architect created plan: `docs/plans/fix-indexer-gaps.md`
- Design.Stubs verification: 24 errors (8 unique x 3 TFMs) confirm all 3 bugs
  - Added `IInitIndexerCollection<TKey, TValue>` domain interface
  - Uncommented `[KnockOff<IMatrix>]` inline attribute
  - Added standalone stubs: `MatrixStandaloneStub`, `InitIndexerStandaloneStub`
  - Failing code left in place as acceptance criteria
- Design decision: multi-param indexer callbacks use tuple key consistently (not flattened params)
- Developer reviewed and approved plan with implementation contract
- All 3 fixes implemented (init-only, multi-param standalone, ThenGet/ThenSet)
- Two additional bugs discovered and fixed during implementation:
  - Source delegation generating `src[(row, col)]` instead of `src[row, col]`
  - Init-only source delegation generating invalid `src[key] = value`
- Architect verification: VERIFIED — 12,358 tests pass, 0 failures

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

Three generator bugs fixed:

1. **Init-only indexers (Gap #4):** Added `IsInitOnly` propagation through FlatIndexerModel, InlineInterfaceImplementation, and renderers. Generator now emits `init` instead of `set` for indexer accessors when the interface declares `init`.

2. **Multi-param standalone indexers (Gap #3/#17):** Added `ParameterSignature`, `ParameterTypes`, `KeyExpression`, `ArgumentList` fields to FlatIndexerModel. FlatModelBuilder now extracts all indexer parameters, not just the first.

3. **ThenGet/ThenSet signatures (Gap #3/#17):** Replaced `ParameterTypes` (flattened) with `KeyType` (tuple) in all `Func<>/Action<>` callback type signatures in IndexerInterceptorRenderer. This makes Get/Set/ThenGet/ThenSet all use the tuple key type consistently.

**Design decision:** Multi-param indexer callbacks use tuple key type (`Func<(int row, int col), double>`) rather than flattened params (`Func<int, int, double>`). This ensures consistency between `Get()` and `ThenGet()` signatures and matches the library interfaces.

**Known limitation documented:** Params array indexers (`this[int a, params string[] b]`) use `(int, string[])` as the Backing dictionary key type. Arrays lack value equality, so `Backing.TryGetValue` fails for different array instances with the same contents. Users should use `Get()` callbacks instead of `Backing` for params indexers.
