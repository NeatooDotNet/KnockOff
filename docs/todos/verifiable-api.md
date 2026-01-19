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

- [x] Update `Times` struct (remove `Forever`, keep verification-only constraints)
- [x] Update `IMethodTracking` interface (add `Verifiable()`, change `Verify()` signature)
- [x] Update `IMethodSequence` interface (remove `Times` from `ThenCall`, add `Verifiable()`)
- [x] Update `VerificationException` to support aggregate failures
- [x] Update `MethodInterceptorRenderer` for new OnCall/ThenCall signatures
- [x] Update `MethodInterceptorRenderer` for new Verify/Verifiable methods
- [x] Update property interceptor generation (add Value tracking, Verifiable)
- [x] Update user-defined method interceptors (add Verifiable with Times)
- [x] Update stub-level Verify/VerifyAll generation
- [x] Update FlatRenderer for all three patterns
- [x] Update InlineRenderer for all three patterns
- [x] Update ClassRenderer for all three patterns
- [x] Update existing tests for new API
- [x] Add new tests for Verifiable() behavior
- [ ] Update documentation and migration guide (will be done as part of a documentation rewrite)
- [x] Update event interceptor generation (add Verifiable)
- [x] Update indexer interceptor generation (add Verifiable)

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

**2026-01-18:** Core implementation completed:
- Times struct updated (removed Forever, added Validate() method)
- IMethodTracking interface updated with Verify() throwing, Verifiable() fluent methods
- IMethodSequence interface updated with Verify() throwing, Verifiable() fluent, no Times in ThenCall
- VerificationException and VerificationFailure classes created for aggregate failures
- MethodInterceptorRenderer completely rewritten with new OnCall/OnCallSequence pattern
- FlatRenderer.RenderVerifyMethods updated to use CheckVerification/CheckVerificationAll
- InlineRenderer stub-level methods updated
- ClassRenderer updated
- All existing tests updated for new API (TimesTests, SequencingTests, VerificationTests)
- All 601 tests passing

Remaining work:
- Documentation update

**2026-01-18:** Property, event, and indexer interceptor verification completed:
- Property interceptors: VerifyGet()/VerifySet()/Verify() with Times overloads, Verifiable()
- Event interceptors: VerifyAdd()/VerifyRemove()/Verify() with Times overloads, Verifiable()
- Indexer interceptors: Same pattern as properties
- All interceptors now have CheckVerification()/CheckVerificationAll() internal methods
- Container classes (IndexerContainer, GenericMethodHandler) aggregate verification
- All 601 tests passing

---

## Results / Conclusions

