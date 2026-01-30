# Parameter-Specific Matching (When API)

**Status:** In Progress
**Priority:** High
**Created:** 2026-01-27
**Last Updated:** 2026-01-28

---

## Problem

KnockOff's current `OnCall()` API applies to ALL invocations regardless of parameter values. Users cannot configure different return values for different argument combinations like NSubstitute's `calc.Add(1, 2).Returns(3)`.

Current workaround requires conditional logic in callbacks:
```csharp
stub.Add.OnCall((a, b) => (a, b) switch {
    (1, 2) => 3,
    (5, 7) => 12,
    _ => 0
});
```

## Solution

Add a fluent `When()` API for parameter-specific matching with sequential consumption:

```csharp
// Single When (repeats when matched)
stub.Add.When(1, 2).Returns(3);

// Chain with ThenWhen (last repeats when matched)
stub.Add
    .When(1, 2).Returns(3)
    .ThenWhen(5, 7).Returns(12)
    .ThenWhen((a, b) => a > 10).Returns(100);

// Chain with ThenCall (unconditional callback, repeats)
stub.Add
    .When(1, 2).Returns(3)
    .ThenCall((a, b) => a + b);

// Chain with ThenNone (explicit close, exhausts)
stub.Add
    .When(1, 2).Returns(3)
    .ThenWhen(5, 7).Returns(12)
    .ThenNone();

// Fallback coexists
stub.Add.OnCall((a, b) => 0);  // Used when When doesn't match or exhausted
```

### Architecture Decision: Sequential HEAD Matching

**Chosen approach:** When chain with HEAD pointer. Only HEAD is checked per call. When matched, HEAD advances (except last ThenWhen which repeats).

**Last matcher behavior:**
- Last `ThenWhen`: repeats when matched, falls through when not
- `ThenCall`: unconditional callback, repeats forever (terminal)
- `ThenNone`: explicit exhaustion, always falls through (terminal)

**Invoke priority chain:**
1. **When chain (NEW - HEAD match check)**
2. Sequence (existing)
3. Returns value (existing)
4. OnCall callback (existing)
5. Source delegation (existing)
6. Strict mode check (existing)
7. Default (existing)

**When chains coexist with Sequence/Returns/OnCall** - When is checked first, rest serves as fallback.

---

## Prerequisites

- [Returns API Rename](./returns-api-rename.md) - establishes `Returns()` naming before `When().Returns()` is added

---

## Plans

- [Parameter-Specific Matching Design](../plans/parameter-specific-matching-design.md) - Complete architecture with When/ThenWhen/ThenCall/ThenNone API

---

## Requirements

### Core Features
- [x] Fluent API: `stub.Method.When(...).Returns(...)`
- [x] Exact value matching: `When(1, 2)`
- [x] Predicate matching: `When((a, b) => a > 0 && b < 10)` (single Func, no Arg class)
- [x] Sequential HEAD matching (only current matcher checked)
- [x] Last ThenWhen repeats when matched, falls through when not
- [x] `ThenCall(callback)` - unconditional terminal that repeats
- [x] `ThenNone()` - explicit exhaustion terminal
- [x] Fallback chain: When → Sequence → Returns/OnCall → Strict → Default
- [x] Methods only (properties/indexers later)
- [x] No `Arg` class - dropped in favor of predicate overload

### Tracking & Verification
- [x] Shared `IWhenTracking` for entire chain
- [x] `ITracking` base interface shared with `IMethodTracking`
- [x] `Verify()` - chain reached terminal state (no Times overload)
- [x] `Reset()` - reset HEAD to first matcher
- [x] `Verifiable()` for batch verification

### Deferred
- [ ] Void method support
- [ ] Property/Indexer support
- [ ] Overload group support (Phase 8)

---

## Tasks

### Phase 1: Public Interfaces
- [ ] Create `src/KnockOff/ITracking.cs` with base interface
- [ ] Modify `IMethodTracking` to extend `ITracking`
- [ ] Create `src/KnockOff/IWhenTracking.cs` with `IWhenTracking`, `IWhenChain<T>`, `IWhenBuilder<T>`

### Phase 2-3: Matcher & Builder Classes
- [ ] Add `WhenMatcher` abstract base class generation
- [ ] Add `WhenMatcherValue`, `WhenMatcherCall`, `WhenMatcherNone` implementations
- [ ] Add `WhenBuilderImpl` nested class generation
- [ ] Add `WhenChainImpl` nested class generation

### Phase 4-5: Storage & Entry Point
- [ ] Add `_whenChain` and `_whenChainHead` fields to interceptors
- [ ] Add `BuildWhenPredicateType()` helper to `UnifiedInterceptorBuilder`
- [ ] Generate `When()` value and predicate overloads
- [ ] Update `Reset()` to clear When chain state

### Phase 6-7: Priority & Verification
- [ ] Add When chain check at TOP of `Invoke()` priority chain
- [ ] Implement HEAD matching with last-matcher repeat logic
- [ ] Update `IsConfigured` and verification methods

### Phase 8-9: Overloads & Testing
- [ ] Replicate for overload group interceptors
- [ ] Add comprehensive tests
- [ ] Add documentation samples

---

## Progress Log

**2026-01-27:** Initial feature exploration and architecture design. Identified two approaches (Matcher List vs Composite Callback). Selected Matcher List approach for first-match-wins support. Compared with NSubstitute's Arg features - identified core features to include and features to defer (Arg.Do, Arg.Invoke, post-hoc verification). Added prerequisite: Returns API Rename todo - establishes `Returns()` naming before `When().Returns()` is added.

**2026-01-28:** Comprehensive design session. Key decisions:
- Dropped `Arg` class entirely - use `When(Func<T1, T2, bool>)` predicate overload instead
- Changed from first-match-wins to sequential HEAD matching (only current matcher checked)
- Added `ThenWhen()` for chaining conditions
- Added `ThenCall(callback)` for unconditional terminal that repeats
- Added `ThenNone()` for explicit exhaustion terminal
- Last `ThenWhen` in chain repeats when matched (not consumed)
- Created `ITracking` base interface shared by `IMethodTracking` and `IWhenTracking`
- `IWhenTracking.Verify()` checks terminal state (no Times overload)
- Shared tracking for entire When chain
- When chains coexist with Sequence/Returns/OnCall as fallback
- Created detailed plan: `docs/plans/parameter-specific-matching-design.md`

---

## Results / Conclusions

