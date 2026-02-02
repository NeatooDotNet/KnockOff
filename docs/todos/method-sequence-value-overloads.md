# Method Sequence Value Overloads

**Status:** In Progress
**Priority:** Medium
**Created:** 2026-02-01
**Last Updated:** 2026-02-01

---

## Problem

Method sequences currently only support callback-based chaining:
```csharp
stub.GetUser.OnCall(() => user1).ThenCall(() => user2).ThenCall(() => user3);
```

This is verbose when returning constant values. Property sequences already support value overloads:
```csharp
stub.Name.OnGet("first").ThenGet("second").ThenGet("third");
```

Users expect the same convenience for method sequences:
```csharp
stub.GetUser.Returns(user1).ThenReturns(user2).ThenReturns(user3);
```

The previous "value-based overloads" work explicitly deferred this feature, noting it IS feasible through generated methods.

## Solution

Add `ThenReturns(TValue value)` to the generated `MethodSequenceImpl` class that wraps the value in a callback. This follows the same pattern as `ThenGet(TValue value)` on `PropertyGetSequenceImpl`.

For async methods (`Task<T>`, `ValueTask<T>`), auto-wrap with `Task.FromResult(value)` or `new ValueTask<T>(value)` - matching the existing `Returns(value)` behavior.

**API:**
- `ThenReturns(TValue value)` - adds a value to return in sequence
- Does NOT modify `IMethodSequence<TCallback>` interface (generated method only)

---

## Plans

- [Method Sequence Value Overloads Design](../plans/method-sequence-value-overloads-design.md)

---

## Tasks

- [ ] Update `IMethodSequence` interface documentation (note generated value overload)
- [ ] Modify `MethodSequenceImpl` generation to add `ThenReturns(TValue value)` overload
- [ ] Handle async wrapping for Task<T> and ValueTask<T> return types
- [ ] Add tests for all four patterns (Standalone, Inline Interface, Inline Class, Delegate)
- [ ] Update Design.Stubs/Methods/MethodSequences.cs to document the new pattern
- [ ] Verify sequence exhaustion behavior works correctly with value overloads

---

## Progress Log

**2026-02-01:** Architect completed design and verification. Plan ready for developer review.

---

## Results / Conclusions
