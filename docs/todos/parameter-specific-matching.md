# Parameter-Specific Matching (When API)

**Status:** In Progress
**Priority:** High
**Created:** 2026-01-27
**Last Updated:** 2026-01-27

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

Add a fluent `When()` API for parameter-specific matching:

```csharp
stub.Add.When(1, 2).Returns(3);
stub.Add.When(5, 7).Returns(12);
stub.Add.OnCall((a, b) => 0);  // Fallback
```

### Architecture Decision: Matcher List (Option A)

**Chosen approach:** Matchers stored in `List<MatcherEntry>`, checked in registration order (first-match-wins).

**Why not Composite Callback (Option B):** Option B would wrap When() around OnCall() internally, but only supports last-wins semantics. Can't have multiple specific matchers coexist with a fallback.

**Invoke priority chain:**
1. Sequence (existing)
2. **Matchers (NEW - first match wins)**
3. Returns value (existing, renamed from OnCall value)
4. OnCall callback (existing)
5. Source delegation (existing)
6. Strict mode check (existing)
7. Default (existing)

**Matchers coexist with Returns/OnCall** - matchers are checked first, Returns/OnCall serves as fallback for non-matching calls.

---

## Prerequisites

- [Returns API Rename](./returns-api-rename.md) - establishes `Returns()` naming before `When().Returns()` is added

---

## Plans

- [Parameter-Specific Matching Design](../plans/parameter-specific-matching-design.md)

---

## Requirements

### Core Features
- [x] Fluent API: `stub.Method.When(...).Returns(...)`
- [x] Exact value matching: `When(1, 2)`
- [x] Predicate matching: `When(a => a > 0, b => b < 10)`
- [x] `Arg.Any<T>()` - match any value
- [x] `Arg.Is<T>(predicate)` - match by predicate
- [x] First-match-wins (registration order)
- [x] Fallback chain: Matchers → Returns/OnCall → Strict → Default
- [x] Methods only (properties/indexers later)

### Tracking & Verification
- [ ] Returns `IMethodTracking` from `Returns()` for verification
- [ ] `LastArgs` access on matcher tracking
- [ ] `Verify(Times)` on matcher tracking
- [ ] `Verifiable()` for batch verification

### Additional Details
(User to provide)

---

## Tasks

- [ ] Create `Arg` static class with `Any<T>()`, `Is<T>(predicate)`
- [ ] Update `UnifiedMethodInterceptorModel` with matcher support flag
- [ ] Update `UnifiedInterceptorBuilder` to populate matcher fields
- [ ] Add `When()` method generation to `MethodInterceptorRenderer`
- [ ] Add `WhenBuilder` nested class generation
- [ ] Add matcher storage (`List<MatcherEntry>`) to interceptors
- [ ] Update `Invoke()` to check matchers before OnCall
- [ ] Update `Reset()` to clear matchers
- [ ] Update verification to include matcher tracking
- [ ] Add tests for parameter matching
- [ ] Add documentation samples

---

## Progress Log

**2026-01-27:** Initial feature exploration and architecture design. Identified two approaches (Matcher List vs Composite Callback). Selected Matcher List approach for first-match-wins support. Compared with NSubstitute's Arg features - identified core features to include and features to defer (Arg.Do, Arg.Invoke, post-hoc verification). Added prerequisite: Returns API Rename todo - establishes `Returns()` naming before `When().Returns()` is added.

---

## Results / Conclusions

