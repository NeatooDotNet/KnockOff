# Indexer Post-Review Fixes

**Status:** Complete
**Priority:** High
**Created:** 2026-02-09
**Last Updated:** 2026-02-09

---

## Problem

After the indexer API redesign (PR #71), Moq skeptic and NSubstitute advocate reviews identified 5 issues:

1. **Stale documentation** — `docs/comparison.md` and `docs/reference/interceptor-api.md` still reference the removed `Backing` API
2. **Missing unit tests** — Several acceptance criteria (AC-2, AC-3, AC-4, AC-6, AC-7, AC-8, AC-13) lack isolated unit tests
3. **Per-key verification gap** — `PerKeyBuilder` tracks `_getCallCount`/`_setCallCount` internally but doesn't expose verification
4. **Predicate-based key matching** — No way to match keys by condition (e.g., "all keys starting with prefix_"). Moq has `It.Is<T>()`, NSubstitute has `Arg.Is<T>()`. KnockOff has `When()` for methods but not indexers.

## Solution

1. Fix stale `Backing` references in both doc files — update to new per-key API
2. Add unit tests for missing acceptance criteria coverage
3. Expose `Verify()` / `VerifyGet()` / `VerifySet()` on `PerKeyBuilder` so users can verify specific key access counts
4. Add `When(predicate)` overload to indexer interceptor — `stub.Indexer.When(k => k.StartsWith("prefix_")).Returns(99)` — slotting into the priority chain above all-keys callback but below exact per-key match

### Priority Chain (updated)

1. Per-key exact match (`stub.Indexer["key"].Returns(...)`)
2. When predicate match (`stub.Indexer.When(k => ...).Returns(...)`)
3. All-keys sequence (if active)
4. All-keys callback (`stub.Indexer.Get(...)`)
5. Source delegation
6. Strict mode / default

---

## Plans

- [Indexer Post-Review Fixes Plan](../plans/completed/indexer-post-review-fixes.md)

---

## Tasks

- [x] Fix stale `Backing` in `docs/comparison.md`
- [x] Fix stale `Backing` in `docs/reference/interceptor-api.md` (6 references)
- [x] Add unit tests for AC-2 (per-key Get callback)
- [x] Add unit tests for AC-3 (per-key Set callback)
- [x] Add unit tests for AC-4 (per-key sequences)
- [x] Add unit tests for AC-6 (per-key with all-keys fallback)
- [x] Add unit tests for AC-7/AC-8 (multi-param indexers)
- [x] Add unit tests for AC-13 (all-keys sequences)
- [x] Expose per-key verification on PerKeyBuilder
- [x] Add When(predicate) overload on indexer interceptor

---

## Progress Log

### 2026-02-09
- Created todo from Moq skeptic and NSubstitute advocate review findings
- Both reviews approved the overall indexer API design
- Key insight: `When()` API already exists for methods, natural extension to indexers
- Architect created plan with 4 phases, 11 acceptance criteria
- Developer raised 3 concerns (separate get/set chains, missing When+Set AC, chain advancement)
- Architect resolved all 3, added AC-WHEN-4 and AC-WHEN-5
- Developer approved, created implementation contract
- All 4 phases implemented: 28 new tests, 11 acceptance criteria compile
- Architect independently verified: 5,098 tests pass, 0 failures

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] Design project builds successfully
- [x] Design project tests pass

**Verification results:**
- Design build: 0 errors, 0 warnings
- Design tests: 356 tests passed per framework, 0 failures

---

## Results / Conclusions

All 5 review issues resolved:
- Stale `Backing` references removed from 2 doc files
- 28 new unit tests added covering all missing acceptance criteria
- Per-key verification exposed via `VerifyGet(Called)`/`VerifySet(Called)` on PerKeyBuilder
- `When(predicate)` added to indexer interceptor with separate get/set chains, matching method When pattern
- Priority chain: per-key exact > When predicate > all-keys sequence > all-keys callback > source > strict > default
