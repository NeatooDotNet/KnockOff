# Verifiable() API Enhancement

**Status:** Complete
**Priority:** High
**Created:** 2026-01-18
**Completed:** 2026-01-22

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

- [Verifiable API Design](../plans/completed/verifiable-api-design.md)

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
- [x] Update ClassRenderer for methods, properties, events
- [x] **Update ClassRenderer indexer interceptors (add Verifiable, VerifyGet, VerifySet, CheckVerification)**
- [x] Update existing tests for new API
- [x] Add new tests for Verifiable() behavior
- [x] ~~Update documentation and migration guide~~ (deferred - will be done outside this todo)
- [x] Update event interceptor generation (add Verifiable)
- [x] Update FlatRenderer indexer interceptors (add Verifiable)
- [x] Update InlineRenderer indexer interceptors (add Verifiable)
- [x] Add tests for ClassRenderer indexer verification API

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

**2026-01-22:** Code review discovered incomplete implementation:
- ClassRenderer.RenderIndexerInterceptorClass (lines 263-342) is **missing the verification API**
- FlatRenderer and InlineRenderer have full indexer verification API, ClassRenderer does not
- Previous progress log entry incorrectly claimed indexer interceptors were complete
- Architect reviewed DD14 in verifiable-api-design.md - design is still valid
- Tasks updated to reflect actual state; remaining work identified

**Missing from ClassRenderer indexer interceptors (per DD14):**
1. `_isVerifiable`, `_verifiableTimes`, `_configured` fields
2. `Verifiable()`, `Verifiable(Times)` fluent methods
3. `Verify()`, `Verify(Times)` throwing methods
4. `VerifyGet()`, `VerifyGet(Times)` (conditional on HasGetter)
5. `VerifySet()`, `VerifySet(Times)` (conditional on HasSetter)
6. `IsVerifiable`, `IsConfigured` internal properties
7. `CheckVerification()`, `CheckVerificationAll()` internal methods
8. OnGet/OnSet need backing fields with configuration tracking

**Reference implementations:** FlatRenderer lines 709-820, InlineRenderer lines 441-606

**2026-01-22:** ClassRenderer indexer verification API implemented:
- Updated `RenderIndexerInterceptorClass` (lines 263-420) with full verification API
- Added `_isVerifiable`, `_verifiableTimes`, `_configured` fields
- Converted `OnGet`/`OnSet` to backing fields (`_onGet`/`_onSet`) with configuration tracking in setters
- Added `Verifiable()` and `Verifiable(Times)` fluent methods returning the interceptor type
- Added `Verify()` and `Verify(Times)` methods that throw VerificationException
- Added `VerifyGet()`/`VerifyGet(Times)` (conditional on HasGetter)
- Added `VerifySet()`/`VerifySet(Times)` (conditional on HasSetter)
- Added `IsVerifiable` and `IsConfigured` internal properties
- Added `CheckVerification()` and `CheckVerificationAll()` internal methods
- Updated Reset() comment to indicate verifiable marking is preserved
- Build succeeded with 0 errors, 0 warnings
- All tests pass (607 net8.0, 608 net9.0/net10.0, plus 134 Documentation.Samples and 473 NeatooInterfaceTests per target)

**2026-01-22:** Added tests for ClassRenderer indexer verification API:
- Created `ClassIndexerVerificationTests.cs` with 35 comprehensive tests
- Created test classes `IndexedCacheService` (get/set indexer) and `ReadOnlyIndexedService` (get-only indexer)
- Tests cover all verification API methods:
  - `Verifiable()` and `Verifiable(Times)` - marking for stub-level verification
  - `VerifyGet()` and `VerifyGet(Times)` - getter access verification
  - `VerifySet()` and `VerifySet(Times)` - setter access verification
  - `Verify()` and `Verify(Times)` - total access verification
  - `stub.Verify()` - stub-level verifiable indexer integration
  - `stub.VerifyAll()` - stub-level configured indexer integration
  - `Reset()` - preserves verifiable marking
- All 35 tests pass on net8.0, net9.0, net10.0
- Full test suite passes: 642 tests (net8.0), 643 tests (net9.0/net10.0)

---

## Results / Conclusions

### What Was Implemented

The Verifiable() API redesign is complete across all three stub patterns (Stand-Alone, Inline Interface, Inline Class):

**Core API Changes:**
- `Times.Forever` removed; `Times` now verification-only
- `IMethodTracking.Verify()` throws `VerificationException` instead of returning bool
- `IMethodTracking.Verifiable()` and `Verifiable(Times)` for marking
- `IMethodSequence` updated with pure sequencing (no Times in ThenCall)
- `VerificationException` aggregates all failures

**Interceptor Verification API (all member types):**
- Methods: `Verify()`, `Verify(Times)`, `Verifiable()`, `Verifiable(Times)`
- Properties: Above plus `VerifyGet()`, `VerifySet()` with Times overloads
- Indexers: Same as properties
- Events: `VerifyAdd()`, `VerifyRemove()` with Times overloads

**Stub-Level Verification:**
- `stub.Verify()` - checks only `.Verifiable()` marked items
- `stub.VerifyAll()` - checks all configured items

### Breaking Changes

- `Times.Forever` removed
- `Verify()` methods throw instead of returning bool
- `VerificationException` uses `Failures` collection (removed `Member`, `Expected`, `Actual` properties)

### Test Coverage

- 642+ tests across all frameworks
- 35 dedicated tests for ClassRenderer indexer verification
- All three stub patterns fully tested

### Notes

- Documentation update deferred to separate documentation rewrite effort
- ClassRenderer indexer verification gap discovered 2026-01-22 and fixed same day
