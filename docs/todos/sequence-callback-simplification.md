# Sequence Callback Simplification (ThenCall)

**Status:** Not Started
**Priority:** Low
**Created:** 2026-01-26
**Last Updated:** 2026-01-26

---

## Problem

After implementing async callback simplification for `OnCall`, the same pattern should apply to sequence methods (`ThenCall`).

Currently:
```csharp
stub.GetUserAsync.OnCallSequence((id) => Task.FromResult(user1))
    .ThenCall((id) => Task.FromResult(user2));
```

Desired:
```csharp
stub.GetUserAsync.OnCallSequence((id) => user1)
    .ThenCall((id) => user2);
```

## Solution

Apply the same simplified callback pattern to `IMethodSequence.ThenCall()` that was implemented for `OnCall`.

---

## Plans

---

## Tasks

- [ ] Design ThenCall overload generation
- [ ] Update MethodSequenceImpl rendering
- [ ] Add tests
- [ ] Update documentation

---

## Progress Log

- 2026-01-26: Created as follow-up from async-callback-simplification feature

---

## Results / Conclusions

