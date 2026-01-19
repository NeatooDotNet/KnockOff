# Verifiable() API Enhancement

**Status:** In Progress
**Priority:** High
**Created:** 2026-01-18
**Last Updated:** 2026-01-18

---

## Problem

KnockOff's current verification API conflates two concerns:
1. **Sequencing** - How many times to use a callback before advancing (`Times.Once`, `Times.Twice`)
2. **Verification** - How many times a method should have been called (`Times.AtLeastOnce`, `Times.Never`)

This differs from Moq's pattern where `Times` is only used at verification time, not during setup. Additionally, KnockOff lacks Moq's `.Verifiable()` pattern for marking specific setups as "must be verified."

## Solution

Redesign the verification API to:
1. Remove `Times` from `OnCall`/`ThenCall` - sequencing becomes pure (each callback runs once)
2. Add `.Verifiable()` method to mark interceptors for `Stub.Verify()`
3. Change `Verify()` methods to throw exceptions (like Moq) instead of returning bool
4. Add `Times` parameter to `Verify()` methods for explicit constraint checking
5. Make `Stub.Verify()` only check `.Verifiable()` items, `Stub.VerifyAll()` checks everything
6. Track `.Value` property access on property interceptors
7. Aggregate all verification failures into a single exception

This aligns KnockOff more closely with Moq's verification patterns while maintaining KnockOff's compile-time, explicit interceptor approach.

---

## Plans

- [Verifiable API Design](../plans/verifiable-api-design.md)

---

## Tasks

- [ ] Update `Times` struct (remove `Forever`, keep verification-only constraints)
- [ ] Update `IMethodTracking` interface (add `Verifiable()`, change `Verify()` signature)
- [ ] Update `IMethodSequence` interface (remove `Times` from `ThenCall`, add `Verifiable()`)
- [ ] Update `VerificationException` to support aggregate failures
- [ ] Update `MethodInterceptorRenderer` for new OnCall/ThenCall signatures
- [ ] Update `MethodInterceptorRenderer` for new Verify/Verifiable methods
- [ ] Update property interceptor generation (add Value tracking, Verifiable)
- [ ] Update user-defined method interceptors (add Verifiable with Times)
- [ ] Update stub-level Verify/VerifyAll generation
- [ ] Update FlatRenderer for all three patterns
- [ ] Update InlineRenderer for all three patterns
- [ ] Update ClassRenderer for all three patterns
- [ ] Update existing tests for new API
- [ ] Add new tests for Verifiable() behavior
- [ ] Update documentation and migration guide

---

## Progress Log

**2026-01-18:** Initial design discussion. Decided on:
- Remove Times from OnCall/ThenCall (pure sequencing)
- Add Verifiable() to IMethodTracking, IMethodSequence, property interceptors
- All Verify() methods throw exceptions (like Moq)
- Use method overloads: Verify() defaults to AtLeastOnce, Verify(Times) for explicit
- Stub.Verify() checks only .Verifiable() items
- Stub.VerifyAll() checks everything including user-defined methods and Value access
- VerificationException aggregates all failures

**2026-01-18:** Architect review completed. Key refinements:
- Keep OnCall() returning IMethodTracking (preserves LastArg access)
- Add OnCallSequence() for sequence building with ThenCall
- Sequence verification = "all callbacks invoked exactly once" (exhausted)
- Verifiable() placement rules: on tracking/sequence for generated methods, on interceptor for user-defined
- Generic methods: Verify() checks ALL type instantiations
- Reset() preserves Verifiable marking

**2026-01-18:** Second architect review - final clarifications:
- VerificationException: BREAKING CHANGE - remove Member/Expected/Actual, use Failures collection, summary in Message/ToString()
- Exhausted sequence: Strict=true throws, Strict=false returns default
- Property tracking simplified: GetCount = OnGet + Value.Get, SetCount = OnSet + Value.Set (unified, no separate ValueGetCount/ValueSetCount)

**2026-01-18:** Third architect review - additional refinements:
- Added `Verifiable(Times)` to IMethodTracking (matches Moq 4.20+ pattern)
- Added `VerifyGet()` / `VerifyGet(Times)` / `VerifySet()` / `VerifySet(Times)` for property verification
- Indexer interceptors follow property pattern
- Clarified OnCall() clears `_isVerifiable` (new config replaces old)
- Clarified generic method tracking: `_isVerifiable` lives on typed handler (per-instantiation)
- Added stub type name to verification error messages

---

## Results / Conclusions

