# Remove CallCount from Public API

**Status:** In Progress
**Priority:** Medium
**Created:** 2026-01-19
**Last Updated:** 2026-01-19

---

## Problem

The `CallCount` property on method interceptors is redundant now that KnockOff has proper verification APIs:

- `WasCalled` - boolean check for at least one call
- `Verify()` / `Verify(Times)` - throws on verification failure
- `Verifiable()` / `Verifiable(Times)` - batch verification support

Exposing `CallCount` encourages low-level assertions like `Assert.Equal(2, stub.Method.CallCount)` instead of the more expressive `stub.Method.Verify(Times.Exactly(2))`.

Removing `CallCount` from the public API will:
1. Simplify the interceptor API surface
2. Guide users toward idiomatic verification patterns
3. Present a cleaner public interface

## Solution

Remove `CallCount` from all **public APIs** while keeping it available internally where needed.

**Remove from public API:**
- Method interceptors - remove public `CallCount` property
- Generic method interceptors - remove public `TotalCallCount` and typed `CallCount`
- `IMethodSequence.TotalCallCount` - remove from public interface
- `IMethodTracking.CallCount` - remove from public interface

**Keep for internal use:**
- Internal tracking can still use call counts for implementation (e.g., sequence tracking)
- Make internal fields/properties private or internal as needed

**Keep unchanged (different semantics):**
- `PropertyInterceptor.GetCount` / `SetCount` - separate get/set tracking
- `IndexerInterceptor.GetCount` / `SetCount` - separate get/set tracking
- `EventInterceptor.AddCount` / `RemoveCount` - subscription tracking

## Breaking Changes

This is a breaking change. Users currently using:
- `stub.Method.CallCount` → use `stub.Method.Verify(Times.Exactly(n))`
- `stub.GenericMethod.TotalCallCount` → use `stub.GenericMethod.Verify(Times.Exactly(n))`
- `stub.GenericMethod.Of<T>().CallCount` → use `stub.GenericMethod.Of<T>().Verify(Times.Exactly(n))`

Callback patterns using `CallCount` for state (e.g., `stub.Connect.CallCount > 0`) will need alternative approaches - document these in migration guide.

---

## Plans

- [Remove CallCount Implementation Plan](../plans/remove-callcount-implementation.md)

---

## Tasks

- [ ] Identify all public interfaces exposing CallCount/TotalCallCount
- [ ] Update IMethodSequence interface to remove TotalCallCount
- [ ] Update IMethodTracking interface to remove CallCount
- [ ] Update generator to remove CallCount from method interceptor public API
- [ ] Update generator to remove TotalCallCount from generic method interceptors
- [ ] Update all tests using CallCount to use Verify(Times) instead
- [ ] Address callback-based patterns that used CallCount for state
- [ ] Update documentation (interceptor-api.md, guides)
- [ ] Update samples in Documentation.Samples
- [ ] Add migration guidance in from-moq.md for this breaking change

---

## Progress Log

2026-01-19: knockoff-architect reviewed initial design and raised concerns:
- CallCount is used in callback patterns for state-dependent behavior (not just verification)
- IMethodSequence.TotalCallCount is a public interface - breaking change
- 362 files reference CallCount

Decision: Proceed with removal from public API. Internal use can remain. Breaking change accepted.

---

## Results / Conclusions
