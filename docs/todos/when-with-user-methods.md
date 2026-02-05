# Enable .When() API with User Methods

**Status:** In Progress
**Priority:** High
**Created:** 2026-02-04
**Last Updated:** 2026-02-04

---

## Problem

When a standalone stub defines user methods (the `_` suffix pattern), the `.When()` API is unavailable. This creates an API inconsistency that contradicts the api-consistency-matrix.md documentation, which claims `.When()` works for all 8 patterns.

This was discovered when trying to use `.When()` on a standalone stub with user methods:

```csharp
[KnockOff]
public partial class MyRepoStub(List<User> Users) : IMyRepo
{
    protected override User? GetUser_(int id)
    {
        return Users.Single(u => u.Id == id);
    }
}

// This fails to compile:
myRepoKO.GetUser.When(1).Returns(user1).Verifiable(Times.Twice);
// Error: cannot convert from 'int' to 'System.Action<...GetUserInterceptor>'
```

The user method provides default "hydrated" behavior (e.g., list lookup), but there's no way to override specific argument combinations for test scenarios.

## Solution

Extend the interceptor priority chain to support `.When()` even when user methods are defined. The priority would be:

1. When chains (parameter-specific matching) - **NEW: Available with user methods**
2. Sequences (ThenCall chain)
3. OnCall/Returns (explicit configuration)
4. **User Method (fallback behavior)** - existing
5. Smart Default (for interfaces) or Base Class (for classes)

This would allow the best of both worlds:
- User method provides default hydrated behavior
- `.When()` overrides specific argument combinations
- Full API consistency across all patterns

## Expected API:

```csharp
[KnockOff]
public partial class MyRepoStub(List<User> Users) : IMyRepo
{
    protected override User? GetUser_(int id)
    {
        // Default: lookup from list
        return Users.Single(u => u.Id == id);
    }
}

// Override specific cases:
stub.GetUser.When(99).Returns(new User { Id = 99, Name = "Special" });

// Call with id=99: returns the special user (When matched)
// Call with id=1: falls through to user method (list lookup)
```

---

## Plans

---

## Tasks

- [ ] Investigate current interceptor implementation for user methods
- [ ] Identify why `.When()` is unavailable with user methods
- [ ] Design integration of When chain with user method fallback
- [ ] Implement When chain support for user method interceptors
- [ ] Add tests covering When + user method combinations
- [ ] Update api-consistency-matrix.md to reflect accurate support
- [ ] Add documentation examples showing When + user method pattern
- [ ] Verify all three patterns (Standalone, Inline Interface, Inline Class)

---

## Progress Log

**2026-02-04**: Created todo based on discovered API inconsistency in ReadMeUseCase.cs

---

## Results / Conclusions
