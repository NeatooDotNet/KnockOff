# Remove WasCalled and Add Direct Verify() to Interceptors

**Status:** Complete
**Priority:** High
**Created:** 2026-01-22
**Completed:** 2026-01-22
**Last Updated:** 2026-01-22 (Implementation complete)

---

## Problem

The `WasCalled` property is a redundant computed boolean (`CallCount > 0`) that duplicates functionality provided by the `Verify()` method. The API is inconsistent: property interceptors have `Verify()`, `VerifyGet()`, `VerifySet()` methods directly available, but method interceptors only expose `Verify()` through the `IMethodTracking` object returned by `OnCall()`.

Additionally, documentation references `WasGot` and `WasSet` properties for properties that don't actually exist in the generated code.

## Solution

Remove `WasCalled` from all locations and add `Verify()`/`Verify(Times)` methods directly on method interceptors for consistency with property interceptors. Also add verification methods to indexer interceptors which currently lack them.

**Breaking change:** Tests using `WasCalled` will fail and require migration to use `Verify()` or check `CallCount` directly.

---

## Plans

- [Remove WasCalled Implementation Plan](../../plans/completed/remove-wascalled-implementation.md)

---

## Tasks

### Phase 1: Core API Changes
- [x] Remove `WasCalled` property from `IMethodTracking` interface
- [x] Remove `WasCalled` property from `IMethodTracking<TArg>` interface
- [x] Remove `WasCalled` property from `IMethodTrackingArgs<TArgs>` interface

### Phase 2: Generator - Method Interceptors
- [x] Remove `WasCalled` from `MethodInterceptorRenderer.cs` (MethodTrackingImpl)
- [x] Remove `WasCalled` from backward-compat tracking properties (single-signature)
- [x] Remove `WasCalled` from backward-compat tracking properties (overload groups)
- [x] Add `Verify()` and `Verify(Times)` to method interceptors (single-signature)
- [x] Add `Verify()` and `Verify(Times)` to method interceptors (overload groups)
- [x] Update `LastCallArg`/`LastCallArgs` to use `CallCount > 0` instead of `WasCalled`

### Phase 3: Generator - Pattern-Specific Files
- [x] Remove `WasCalled` from `FlatRenderer.cs` (generic handlers, tracking impl)
- [x] Remove `WasCalled` from `InlineRenderer.cs` (generic handlers, delegates)
- [x] Remove `WasCalled` from `ClassRenderer.cs` (method interceptors)
- [x] Add `Verify()` methods to flat method interceptors
- [x] Add `Verify()` methods to class stub method interceptors
- [x] Add `Verify()` methods to delegate interceptors

### Phase 4: Indexer Verification
- [x] ~~Add `Verify()`, `VerifyGet()`, `VerifySet()` to flat indexer interceptors~~ (Already exist - verified by architect)
- [x] ~~Add `Verify()`, `VerifyGet()`, `VerifySet()` to inline indexer interceptors~~ (Already exist - verified by architect)

### Phase 5: Documentation
- [x] Update `docs/guides/verification.md` - remove WasGot/WasSet references
- [x] Add migration guide section
- [x] Update API examples to use `Verify()` instead of `WasCalled`

### Phase 6: Version & Release
- [x] Bump major version in `Directory.Build.props` (10.24.0 -> 11.0.0)
- [x] Create release notes

### Phase 9: Test Migration
- [x] Migrate ~226 WasCalled usages across ~38 test files
- [x] Pattern: `Assert.True(x.WasCalled)` -> `x.Verify()`
- [x] Pattern: `Assert.False(x.WasCalled)` -> `x.Verify(Times.Never)`
- [x] Handle tracking objects from OnCall()
- [x] Update comments mentioning WasCalled
- [x] All tests pass (607 tests)

---

## Progress Log

### 2026-01-22
- Created todo and plan
- Analyzed codebase: found `WasCalled` in 9 locations across 5 generator files
- Confirmed `WasGot`/`WasSet` don't exist in generated code (docs are wrong)
- Confirmed properties already have `Verify()` methods - methods should match
- **CORRECTED:** Indexers already have verification methods (FlatRenderer lines 711-764, InlineRenderer lines 532-576)
- **Architect Review Complete:**
  - Verified all three patterns (Standalone, Inline Interface, Inline Class)
  - Identified 641 test usages that will break (intentional)
  - ClassRenderer already has Verify() methods - only WasCalled removal needed
  - Internal tracking checks (3 locations) use WasCalled - must change to CallCount > 0
  - Plan status updated to "Under Review (Developer)"
- **Developer Review Complete:**
  - Verified all 22 WasCalled locations across 5 files
  - Found 2 additional internal usages (MethodInterceptorRenderer lines 937, 946)
  - Confirmed delegate interceptors need Verify() methods added (correction to architect's table)
  - Created detailed 8-phase implementation contract with 30+ checklist items
  - Plan status updated to "Ready for Implementation"
- **Phase 9 Added (Test Migration):**
  - User clarified: "let tests fail" was about external backwards compatibility, not repo's own tests
  - Added Phase 9 to plan for migrating ~226 WasCalled usages across ~36 test files
  - Documented migration patterns and edge cases
  - Created per-file checklist for systematic migration

---

## Results / Conclusions

### Implementation Complete (2026-01-22)

**Summary:** Successfully removed `WasCalled` property from all interceptors and tracking objects, and added `Verify()` / `Verify(Times)` methods directly on method interceptors for API consistency with property interceptors.

**Breaking Change:** This is a major version bump (10.24.0 -> 11.0.0). All existing code using `WasCalled` will fail to compile and must be migrated.

**Migration Path:**
- `stub.Method.WasCalled` -> `stub.Method.Verify()` (throws if not called)
- `Assert.True(stub.Method.WasCalled)` -> `stub.Method.Verify()`
- `Assert.False(stub.Method.WasCalled)` -> `stub.Method.Verify(Times.Never)`

**Final Verification:**
- Build: **Build succeeded. 0 Warning(s) 0 Error(s)**
- Tests: **Test Run Successful. Total tests: 607 Passed: 607**
- Grep: No remaining `.WasCalled` API usage in tests or benchmarks

**Key Files Changed:**
- `src/KnockOff/IMethodTracking.cs` - Interface property removed
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` - Core changes
- `src/Generator/Renderer/FlatRenderer.cs` - Standalone stub changes
- `src/Generator/Renderer/InlineRenderer.cs` - Inline stub changes
- `src/Generator/Renderer/ClassRenderer.cs` - Class stub changes
- `docs/guides/verification.md` - Documentation updated
- 38 test files migrated in src/Tests/
