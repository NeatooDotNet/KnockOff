# Value and Simplified Callbacks for Overload Groups

**Status:** Not Started
**Priority:** Low
**Created:** 2026-01-26
**Last Updated:** 2026-01-26

---

## Problem

Methods with multiple overloads (same name, different signatures) use a separate rendering path (`RenderOverloadGroupContent`) that doesn't support:
1. Value overloads: `OnCall(value)`
2. Simplified async callbacks: `OnCall((params) => nonTaskValue)`

These features only work for single-signature methods currently.

Example interface:
```csharp
interface IRepository {
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByIdAsync(int id, CancellationToken ct);  // Overload
}
```

Currently must use verbose syntax:
```csharp
stub.GetByIdAsync.OnCall((int id) => Task.FromResult(user));
stub.GetByIdAsync.OnCall((int id, CancellationToken ct) => Task.FromResult(user));
```

## Solution

Extend `RenderOverloadGroupContent` to support:
1. Per-signature value storage (like single-signature methods)
2. Per-signature simplified callback storage for async methods

This follows the same patterns already established in `RenderSingleSignatureContent`.

---

## Plans

---

## Tasks

- [ ] Add value overload support to overload groups
- [ ] Add simplified async callback support to overload groups
- [ ] Add tests for both features with overloaded methods
- [ ] Update documentation

---

## Progress Log

- 2026-01-26: Created as follow-up from async-callback-simplification feature

---

## Results / Conclusions

