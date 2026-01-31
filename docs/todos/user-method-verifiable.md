# Add Verifiable Support for User-Defined Methods

**Status:** Not Started
**Priority:** Medium
**Created:** 2026-01-30
**Last Updated:** 2026-01-30

---

## Problem

User-defined methods in stand-alone stubs have `.Verifiable()` methods that exist for API compatibility but don't actually track verifiable state. The methods just return `this`:

```csharp
public global::KnockOff.IMethodTracking Verifiable() => this;
public global::KnockOff.IMethodTracking Verifiable(global::KnockOff.Times times) => this;
```

There's no `_isVerifiable` field and no `IsVerifiable` property. This means:
- You can call `.Verify()` directly on a user-defined method interceptor
- But calling `.Verifiable()` and then `stub.Verify()` won't include that method in aggregate verification

Regular interface methods (via shared `MethodInterceptorRenderer`) have proper Verifiable support, but user-defined methods (via `RenderUserMethodInterceptorClass` in FlatRenderer.cs) do not.

## Solution

Add proper `_isVerifiable` state tracking to user-defined method interceptors:
1. Add `_isVerifiable` field and `_verifiableTimes` field
2. Update `Verifiable()` methods to set state instead of just returning `this`
3. Add `IsVerifiable` property
4. Add `GetVerificationFailure()` method for aggregate verification
5. Include user-defined methods in `Stub.Verify()` aggregation

---

## Plans

---

## Tasks

- [ ] Add `_isVerifiable` and `_verifiableTimes` fields to user method interceptor
- [ ] Update `Verifiable()` to set `_isVerifiable = true`
- [ ] Update `Verifiable(Times)` to set both fields
- [ ] Add `IsVerifiable` property
- [ ] Add `GetVerificationFailure()` method
- [ ] Update `Stub.Verify()` to include user-defined method interceptors
- [ ] Add tests for user-defined method Verifiable behavior
- [ ] Verify generic method handlers also have proper support

---

## Progress Log

---

## Results / Conclusions
