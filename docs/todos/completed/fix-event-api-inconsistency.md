# Fix Event API Inconsistency Between Patterns

**Status:** Complete
**Priority:** High
**Created:** 2026-02-06
**Last Updated:** 2026-02-06

---

## Problem

The event interceptor API differs between standalone and inline patterns. This violates the API Consistency Principle ("All patterns should provide identical APIs except for intentional variations").

**Standalone pattern** (e.g., `EventServiceStub`):
- Bare event names: `stub.Started`
- Has `Raise()` method: `stub.Started.Raise(sender, args)`
- Has `HasSubscribers` property: `stub.Started.HasSubscribers`
- Private `_handler` field (not exposed)

**Inline pattern** (e.g., `EventPatternsDemo.Stubs.IEventSource`):
- `Interceptor` suffix: `stub.StartedInterceptor`
- No `Raise()` method — uses `Handler?.Invoke()`: `stub.StartedInterceptor.Handler?.Invoke(sender, args)`
- No `HasSubscribers` property — uses `Handler != null`
- Public `Handler` property

**User-reported issue:** A user expected `stub.Changed.Raise(...)` (as documented in the skill) but had to use `stub.ChangedInterceptor.Handler?.Invoke(...)` because they were using an inline stub.

## Solution

Update the inline event interceptor generator to match the standalone pattern:

1. Generate `Raise()` method on inline event interceptors (all event types: EventHandler, EventHandler<T>, Action, Action<T...>)
2. Generate `HasSubscribers` property on inline event interceptors
3. Use bare event names (e.g., `Started` not `StartedInterceptor`) for the property on the stub class
4. Remove public `Handler` property (make backing field private like standalone)
5. Update Design project to use `Raise()` API
6. Update Design tests accordingly

---

## Plans

- [Event API Consistency Design](../plans/completed/event-api-consistency-design.md)

---

## Tasks

- [x] Architect: Analyze inline event renderer and create implementation plan
- [x] Developer: Review plan (approved with Implementation Contract)
- [x] Developer: Implement generator changes (Phases 1-3: helper, models, builders, renderers, naming)
- [x] Developer: Fix tests (Phase 4: 9 test files updated)
- [x] Update Design.Stubs event documentation to use Raise() API (Phase 5)
- [x] Update Design.Tests event tests to use Raise() API (Phase 5)
- [x] Update documentation and skill files (Phases 6-7)
- [x] Architect: Verify implementation — VERIFIED (7,427 tests, 0 failures)

---

## Progress Log

### 2026-02-06
- User reported: skill says `.Raise()` exists but they had to use `.ChangedInterceptor.Handler?.Invoke()`
- Investigation revealed API inconsistency between standalone and inline patterns
- Standalone has `Raise()`, `HasSubscribers`, bare names — inline has `Handler?.Invoke()`, `Handler != null`, `Interceptor` suffix
- Created todo to track the fix
- Architect analysis complete: inconsistency affects 3 renderers, 3 builders, 2 models
- Clarifying questions answered: Option A (bring all up to match FlatRenderer), fix naming, all breaking changes approved
- Plan created: `docs/plans/event-api-consistency-design.md`
- Developer review: 3 concerns raised (test coverage, EscapeIdentifier dependency, acceptance criteria scope)
- Plan revised: all 3 concerns addressed, acceptance criteria expanded to cover Patterns 3, 5, and 8
- New domain types created: `EventServiceBase` (abstract class), `IGenericEventSource<T>` (generic interface)
- Plan status: Under Review (Developer) -- ready for re-review
- Developer re-review: all 3 concerns resolved, plan approved, Implementation Contract created
- Plan status: Ready for Implementation

### 2026-02-06 (Implementation)
- Phase 1 complete: Created `EventBuilderHelpers.cs`, updated 2 models, 3 builders. Generator compiles.
- Phase 2 complete: Rewrote event rendering in InlineRenderer, ClassRenderer, StandaloneClassRenderer. Added `_handler`, `HasSubscribers`, `Raise()`. Generator compiles.
- Phase 3 complete: Dropped `Interceptor` suffix from inline event property names. Generator compiles.
- Phase 4 complete: Updated 9 test files. Full solution: 6,650 tests, 0 failures.
- Phase 5 complete: Rewrote `EventPatterns.cs` (removed history comments) and `EventBasicsTests.cs`. Design.Stubs compiles (acceptance criteria met). Design.Tests: 259 x 3 = 777 tests, 0 failures.
- Phase 6 complete: `events.md` already correct. Removed outdated `Interceptor` suffix note from `api-consistency-matrix.md`.
- Phase 7 complete: All 4 skill files already use correct API. No changes needed.
- Plan status: Awaiting Verification

---

## Completion Verification

- [x] Design project builds successfully
- [x] Design project tests pass (777 tests)
- [x] Full solution builds (0 errors)
- [x] Full solution tests pass (6,650 tests)
- [x] Architect independently verified all results

---

## Results / Conclusions

The event API inconsistency was caused by the inline, class, and standalone class renderers being implemented separately from the flat renderer without carrying over the `Raise()`, `HasSubscribers`, and private `_handler` patterns. The fix unified all four renderer pipelines by extracting `GetRaiseMethodInfo` into a shared `EventBuilderHelpers` class, adding Raise-related fields to both event models, and rewriting the event interceptor rendering in three renderers to match the flat renderer's pattern. The `Interceptor` suffix on inline event property names was also removed for naming consistency. All breaking changes were approved (pre-1.0). Design project documentation was updated to remove incorrect "DID NOT DO THIS" comments and document the unified pattern as canonical.
