# Sequence Exhaustion Behavior

**Status:** Complete
**Priority:** High
**Created:** 2026-02-01
**Last Updated:** 2026-02-01

---

## Problem

When a sequence is exhausted (all configured returns consumed), KnockOff currently returns `default(T)` in non-strict mode. This differs from NSubstitute, which repeats the last value forever.

Current behavior:
```csharp
stub.Method.OnCall(() => 1).ThenReturns(2).ThenReturns(3);
// Returns: 1, 2, 3, default, default, default...
```

Users expect NSubstitute-like behavior where the last value repeats.

## Solution

Change default exhaustion behavior to repeat the last configured value. Add `ThenDefault()` as an explicit escape hatch for tests that need to verify exact call counts.

New behavior:
```csharp
// Default: repeat last value
stub.Method.OnCall(() => 1).ThenReturns(2).ThenReturns(3);
// Returns: 1, 2, 3, 3, 3, 3...

// Explicit default termination
stub.Method.OnCall(() => 1).ThenReturns(2).ThenDefault();
// Returns: 1, 2, default, default, default...

// Strict mode unchanged - throws on exhaustion
stub.Strict = true;
stub.Method.OnCall(() => 1).ThenReturns(2);
// Returns: 1, 2, then throws SequenceExhausted
```

**Breaking Change:** Existing code that relied on sequences returning `default(T)` after exhaustion will now get the last value repeated instead.

---

## Plans

- [Sequence Exhaustion Behavior Design](../plans/sequence-exhaustion-behavior-design.md)

---

## Tasks

- [x] Add `_repeatLastValue` field to generated sequence classes (default: `true`)
- [x] Add `ThenDefault()` method that sets `_repeatLastValue = false`
- [x] Modify `Invoke` sequence execution to repeat last callback when exhausted
- [x] Apply to all member types: Methods, Properties, Indexers
- [x] Update existing exhaustion tests to expect new behavior
- [x] Add new tests for `ThenDefault()` functionality
- [x] Update Design.Stubs documentation

---

## Progress Log

**2026-02-01:** Created todo. User requested NSubstitute-like default behavior with `ThenDefault()` escape hatch.

**2026-02-01:** Implementation complete across 6 phases:
- Phase 0: Added `ThenDefault()` to 5 library interfaces
- Phase 1: Method sequence generator (MethodInterceptorRenderer.cs)
- Phase 2: Property sequence generator (PropertyInterceptorRenderer.cs)
- Phase 3: Indexer sequence generator (IndexerInterceptorRenderer.cs)
- Phase 4: Updated tests (968-969 tests passing)
- Phase 5: Updated Design.Stubs documentation

---

## Results / Conclusions

**Completed 2026-02-01**

Successfully changed sequence exhaustion behavior to match NSubstitute:

| Mode | Default | After `ThenDefault()` |
|------|---------|----------------------|
| Non-strict | Repeat last callback | Return `default(T)` |
| Strict | Throw `SequenceExhausted` | Throw `SequenceExhausted` |

**Breaking Change:** Existing code that relied on `default(T)` after exhaustion now gets the last value repeated. Migration: add `.ThenDefault()` to affected sequences.

**Files modified:**
- `src/KnockOff/IMethodSequence.cs`, `IPropertySequence.cs`, `IIndexerSequence.cs`
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs`
- `src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs`
- `src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs`
- Design.Stubs and Design.Tests documentation
