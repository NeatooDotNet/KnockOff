# Simplified Exception Throwing API

**Status:** Not Started
**Priority:** Low
**Created:** 2026-01-26
**Last Updated:** 2026-01-26

---

## Problem

Throw-only lambdas create overload ambiguity because `throw` expressions have no return type:

```csharp
// Ambiguous - compiler can't choose between Func<int, User?> and Func<int, Task<User?>>
stub.GetUserAsync.OnCall((id) => throw new NotFoundException());
```

Users must currently cast to an explicit delegate type:
```csharp
stub.GetUserAsync.OnCall((Func<int, Task<User?>>)((id) => throw new NotFoundException()));
```

This is verbose and unintuitive.

## Solution Options

### Option 1: `OnCall(Exception)` overload

```csharp
stub.GetUserAsync.OnCall(new NotFoundException());
// Internally: throws the exception on every call
```

Pros:
- Simple, mirrors `OnCall(value)` pattern
- No ambiguity

Cons:
- Same exception instance thrown every time (stack trace reuse issues?)
- Can't vary exception based on parameters

### Option 2: `OnCall(Func<TParams..., Exception>)` overload

```csharp
stub.GetUserAsync.OnCall((id) => new NotFoundException($"User {id} not found"));
// Internally: calls func, throws returned exception
```

Pros:
- Can vary exception based on parameters
- Fresh exception instance each call

Cons:
- Still potential ambiguity with other Func overloads?

### Option 3: `.Throws()` extension method

```csharp
stub.GetUserAsync.Throws(new NotFoundException());
stub.GetUserAsync.Throws((id) => new NotFoundException($"User {id} not found"));
```

Pros:
- Clear intent - separate from OnCall
- No ambiguity
- Follows Moq pattern (familiar to users)

Cons:
- New API surface
- Need to decide return type (IMethodTracking for verification?)

### Option 4: `OnCallThrows()` method

```csharp
stub.GetUserAsync.OnCallThrows(new NotFoundException());
stub.GetUserAsync.OnCallThrows((id) => new NotFoundException($"User {id}"));
```

Pros:
- Consistent with OnCall naming
- Clear intent

Cons:
- More verbose than `.Throws()`

---

## Plans

---

## Tasks

- [ ] Evaluate options and choose approach
- [ ] Design API surface
- [ ] Implement
- [ ] Add tests
- [ ] Update documentation

---

## Progress Log

- 2026-01-26: Created as follow-up from async-callback-simplification implementation (edge case discovered)

---

## Results / Conclusions

