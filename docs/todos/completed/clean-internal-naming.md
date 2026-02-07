# Clean Internal Naming to Match Public API

**Status:** Complete
**Priority:** Medium
**Created:** 2026-02-07
**Last Updated:** 2026-02-07
**Plan:** [clean-internal-naming.md](../plans/clean-internal-naming.md)

---

## Problem

After PR #54 (OnGet/OnSet → Get/Set public API) and PR #55 (Returns→Return, Execute→Call public API), the internal naming in the generator and generated code still uses old conventions. Since KnockOff is pre-1.0, now is the time to clean this up — no tech debt.

Three categories of stale naming remain:

1. **Generated code** — interceptor properties and private fields use old names (`OnGet`, `OnSet`, `_onCall`, `_returnsValue`)
2. **Generator internals** — model properties and builder methods use old names (`OnCallDelegateType`, `BuildOnCallDelegateType`)
3. **Error messages** — `StubException.cs` references "OnCall"

## Solution

Rename all internal identifiers to match the new public API conventions:
- `OnGet` → `Get`, `OnSet` → `Set` (in generated interceptor code)
- `_onCall` → `_call`, `_onCallTracking` → `_callTracking`, etc.
- `_returnsValue` → `_returnValue`, `_hasReturnsValue` → `_hasReturnValue`, `_returnsValueTracking` → `_returnValueTracking`
- `OnCallDelegateType` → `CallDelegateType` in models/builders
- Update error messages and comments

### Scope

- **Patterns affected:** All 9 (generated code changes affect all pipelines)
- **Member types affected:** Methods, Properties, Indexers
- **NOT in scope:** Domain code using `Execute` as business method names

## Plans

- [Clean Internal Naming to Match Public API](../plans/clean-internal-naming.md)

## Tasks

- [x] Rename generated interceptor properties `OnGet`→`Get`, `OnSet`→`Set` in FlatRenderer and InlineRenderer
- [x] Rename generated private fields (`_onCall`→`_call`, `_returnsValue`→`_returnValue`, etc.) in MethodInterceptorRenderer
- [x] Rename generated private fields (`_onGet`→`_get`, etc.) in PropertyInterceptorRenderer
- [x] Rename internal model properties (`OnCallDelegateType`→`CallDelegateType`) in models
- [x] Rename internal builder methods (`BuildOnCallDelegateType`→`BuildCallDelegateType`)
- [x] Update error messages in StubException.cs
- [x] Update generated comments referencing old names
- [x] Verify all 9 patterns compile and all tests pass

## Progress Log

## Results / Conclusions

All internal naming cleaned up to match public API conventions. 22 files modified across generator models, builders, renderers, adapters, library code, and comments. Zero stale naming patterns remain in `src/Generator/` or `src/KnockOff/`. All 3,486 tests pass per framework (net8.0/net9.0/net10.0) with zero failures. Architect independently verified.
