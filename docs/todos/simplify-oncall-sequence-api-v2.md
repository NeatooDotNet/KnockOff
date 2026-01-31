# Simplify OnCall Sequence API (v2)

**Status:** In Progress
**Priority:** Medium
**Created:** 2026-01-30
**Last Updated:** 2026-01-30 (Developer approved, ready for implementation)

**Note:** Re-implementation on readmeDoc branch which already has When() API. Based on completed work in simpleOnCallSequence branch.

**Architect Review:** Complete - plan enhanced with full architectural verification, When() API integration analysis, and risk assessment. Developer concerns addressed 2026-01-30.

**Developer Review:** Approved - Implementation contract created with 7-phase checklist and verification gates.

---

## Problem

The current API requires two separate entry points for configuring method callbacks:

```csharp
// Repeating callback
stub.Method.OnCall((a, b) => a + b);

// Sequence - different entry point
stub.Method.OnCallSequence((a, b) => 1).ThenCall((a, b) => 2);
```

This creates unnecessary cognitive load. Users must learn two methods (`OnCall` and `OnCallSequence`) when a single entry point with optional chaining would be more intuitive.

## Solution

Eliminate `OnCallSequence` by having `OnCall` return a builder interface that supports `ThenCall`. The callback "elevates" to sequence behavior lazily when `ThenCall` is called:

```csharp
// Repeating callback - unchanged behavior
stub.Method.OnCall((a, b) => a + b);

// Sequence - same entry point, just chain ThenCall
stub.Method.OnCall((a, b) => 1).ThenCall((a, b) => 2);
```

---

## Plans

- [Simplify OnCall Sequence API Design v2](../plans/simplify-oncall-sequence-api-design-v2.md)

---

## Tasks

- [ ] Create new builder interfaces in `src/KnockOff/`
- [ ] Update MethodInterceptorRenderer to use builder pattern
- [ ] Update PropertyInterceptorRenderer to use builder pattern
- [ ] Update IndexerInterceptorRenderer to use builder pattern
- [ ] Add builder elevation tests
- [ ] Remove old OnCallSequence/OnGetSequence/OnSetSequence entry points
- [ ] Update existing sequence tests to use new API
- [ ] Verify all three stub patterns work
- [ ] Verify When() API still works correctly

---

## Progress Log

### 2026-01-30
- Created todo and plan for re-implementation on readmeDoc branch
- Previous implementation in simpleOnCallSequence branch had 838 tests passing
- Current branch has When() API which must be preserved
- Architect enhanced plan with full verification checklist
- Developer raised 4 clarification concerns
- Architect addressed all concerns:
  1. Add `ThenGet(TValue value)` to both builder and sequence interfaces
  2. Rename `TrackingInterface` → `BuilderInterface` in models; update builder method
  3. Confirmed builder `ThenCall()` only called once for elevation
  4. Added covariant `Verifiable()` overrides to all builder interfaces
- Developer re-reviewed architect's responses - all concerns adequately addressed
- One minor typo corrected in plan (`IPropertySetBuilder<TValue, TValue>` to `<TValue>`)
- Implementation contract created with 7-phase checklist
- Plan status updated to "Ready for Implementation"

---

## Results / Conclusions

