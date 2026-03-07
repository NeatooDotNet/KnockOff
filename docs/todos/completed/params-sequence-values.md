# Params Sequence Values

**Status:** In Progress
**Priority:** Medium
**Created:** 2026-02-01
**Last Updated:** 2026-02-01

---

## Problem

Method sequences require chaining multiple `ThenReturns()` calls when returning multiple constant values:

```csharp
stub.Method.OnCall(() => 1).ThenReturns(2).ThenReturns(3).ThenReturns(4);
```

NSubstitute offers a more concise params-based API:
```csharp
sub.Method().Returns(1, 2, 3, 4);
```

Users expect similar convenience in KnockOff.

## Solution

Add params overloads to support multiple values in a single call:

**Option A: Params on Returns (implicit sequence)**
```csharp
stub.Method.Returns(1, 2, 3, 4);  // Creates sequence implicitly
```

**Option B: Params on ThenReturns (explicit sequence)**
```csharp
stub.Method.OnCall(() => 1).ThenReturns(2, 3, 4);
```

**Decision: Both.** They serve different use cases:
- `Returns(x, y, z)` - Simple constant sequences
- `OnCall().ThenReturns(y, z)` - When first value needs callback access to arguments

### Scope

**Patterns:** All four (Standalone, Inline Interface, Inline Class, Delegate)

**Members:**
- Methods: `Returns(first, params rest)`, `ThenReturns(params values)`
- Properties: `ThenGet(params values)` (Returns already takes single value)
- Indexers: N/A (getters require key parameter in callback)

**Async handling:** Auto-wrap with `Task.FromResult()` / `new ValueTask<T>()` for `Task<T>` and `ValueTask<T>` return types.

---

## Plans

- [Params Sequence Values Design](../plans/params-sequence-values-design.md)

---

## Tasks

- [ ] Design params overload signatures
- [ ] Generator changes for `Returns(TValue first, params TValue[] rest)`
- [ ] Generator changes for `ThenReturns(params TValue[] values)`
- [ ] Generator changes for `ThenGet(params TValue[] values)` on properties
- [ ] Handle async wrapping for Task<T> and ValueTask<T>
- [ ] Add tests for all patterns
- [ ] Update Design.Stubs documentation

---

## Progress Log

**2026-02-01:** Created todo based on discussion about NSubstitute's `Returns(x, y, z)` API. Decided to implement both `Returns(first, params rest)` for implicit sequences and `ThenReturns(params values)` for explicit sequences.

**2026-02-01:** Architect completed comprehensive design plan. Addressed all design questions:
- Signature disambiguation: C# overload resolution handles single vs params naturally
- Callback params: Not supported (clear alternative exists with OnCall().ThenCall())
- Async handling: Reuse existing ThenReturns wrapping pattern
- Indexers: Excluded (key parameter would be ignored)

---

## Results / Conclusions
