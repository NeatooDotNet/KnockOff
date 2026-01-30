# Simplify OnCall Sequence API

**Status:** In Progress
**Priority:** Medium
**Created:** 2026-01-29
**Last Updated:** 2026-01-29

**Note:** Plan approved by developer. Implementation contract created with 7 phases and verification checkpoints. Ready for implementation.

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

Eliminate `OnCallSequence` by having `OnCall` return an interface that supports `ThenCall`. The callback "elevates" to sequence behavior lazily when `ThenCall` is called:

```csharp
// Repeating callback - unchanged behavior
stub.Method.OnCall((a, b) => a + b);

// Sequence - same entry point, just chain ThenCall
stub.Method.OnCall((a, b) => 1).ThenCall((a, b) => 2);
```

---

## Plans

- [Simplify OnCall Sequence API Design](../plans/simplify-oncall-sequence-api-design.md)

---

## Tasks

- [ ] Create new `IMethodCallBuilder` interfaces (3 variants)
- [ ] Update generator to change `OnCall` return type
- [ ] Implement lazy elevation in `ThenCall`
- [ ] Apply same pattern to properties (OnGet/OnSet)
- [ ] Apply same pattern to indexers
- [ ] Remove `OnCallSequence`, `OnGetSequence`, `OnSetSequence` entry points
- [ ] Update/add tests
- [ ] Verify all existing sequence tests still pass

---

## Progress Log

---

## Results / Conclusions
