# Clean Internal Naming to Match Public API

**Status:** In Progress
**Priority:** Medium
**Created:** 2026-02-07
**Last Updated:** 2026-02-07

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

_(to be linked)_

## Tasks

- [ ] Rename generated interceptor properties `OnGet`→`Get`, `OnSet`→`Set` in FlatRenderer and InlineRenderer
- [ ] Rename generated private fields (`_onCall`→`_call`, `_returnsValue`→`_returnValue`, etc.) in MethodInterceptorRenderer
- [ ] Rename generated private fields (`_onGet`→`_get`, etc.) in PropertyInterceptorRenderer
- [ ] Rename internal model properties (`OnCallDelegateType`→`CallDelegateType`) in models
- [ ] Rename internal builder methods (`BuildOnCallDelegateType`→`BuildCallDelegateType`)
- [ ] Update error messages in StubException.cs
- [ ] Update generated comments referencing old names
- [ ] Verify all 9 patterns compile and all tests pass

## Progress Log

## Results / Conclusions
