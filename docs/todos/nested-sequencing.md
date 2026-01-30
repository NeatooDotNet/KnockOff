# Nested Sequencing Support

**Status:** Not Started
**Priority:** Medium
**Created:** 2026-01-29
**Last Updated:** 2026-01-29

---

## Problem

KnockOff needs to support nested/chained callback sequencing similar to NSubstitute's `Callback` builder pattern.

## Solution

Implement a fluent API for sequenced callbacks that allows chaining multiple behaviors.

---

## Plans

---

## Tasks

- [ ] Design the callback builder API
- [ ] Implement sequencing support
- [ ] Add tests for all chaining methods

---

## Progress Log

---

## Results / Conclusions

---

## Reference

NSubstitute syntax we want to support something similar to:

```csharp
sub.When(x => x.Something())
    .Do(Callback.First(x => calls.Add("1"))
        .Then(x => calls.Add("2"))
        .ThenKeepDoing(x => calls.Add("+"))
        .AndAlways(x => counter++));
```

Key methods in the chain:
- `First()` - First call behavior
- `Then()` - Subsequent call behavior (one-time)
- `ThenKeepDoing()` - All remaining calls
- `AndAlways()` - Runs on every call in addition to the sequenced behavior
