# API Differences: Standalone vs Inline Stubs

## Status: Complete

This document captured API inconsistencies discovered when testing KnockOff with Neatoo interfaces. All issues have been resolved.

## Summary

When creating comprehensive tests for Neatoo interfaces using both standalone (`[KnockOff]`) and inline (`[KnockOff<T>]`) stubs, several API differences were discovered. These have been fixed or documented.

## Resolved Issues

### 1. Property Interceptor `Value` Property

**Issue**: Concern that standalone stubs might not have `Value` property.

**Resolution**: Both inline and standalone stubs generate `Value` property in interceptors. Verified by checking generated code - standalone stubs correctly use `Interceptor.Value`. Removed dead code that used legacy `{PropertyName}Backing` pattern.

### 2. Duplicate Indexer Members for Inherited Interfaces

**Issue**: When `IEntityBase : IValidateBase` and both have `this[string]`, inline stubs generated duplicate interceptor classes.

**Resolution**: The deduplication logic was already working correctly. Stale generated files from before the fix were cleaned up. Added test case `InlineEntityContainer` to verify the fix works.

### 3. Async Delegate Default Return Value

**Issue**: Async delegate stubs returned `default!` (null) causing `NullReferenceException` when awaited.

**Resolution**: Fixed in `KnockOffGenerator.GenerateInline.cs` to use `GetDefaultForType()` which returns:
- `Task.CompletedTask` for `Task`
- `default` (completed struct) for `ValueTask`
- `Task.FromResult<T>(default!)` for `Task<T>`
- `default` for `ValueTask<T>`

### 4. Documentation Updates

**Resolution**: Updated `docs/guides/inline-stubs.md` comparison table to note inline-only features (class stubbing, delegate stubbing).

### 5. Method Overload Naming (Documented)

**Behavior**: Method overloads get numeric suffixes (`RunRules1`, `RunRules2`). This is expected and documented.

### 6. Naming Pattern Differences (Expected)

**Behavior**: Standalone uses `stub.MethodName`, inline uses `Stubs.IInterface.MethodName`. This is an architectural difference, not a bug.

## Tasks

- [x] Add `Value` property to standalone stub property interceptors - **VERIFIED**: Already present, removed dead code
- [x] Fix duplicate indexer generation for inherited interfaces - **VERIFIED**: Already fixed, cleaned up stale files
- [x] Investigate missing `RunRules` on meta properties interceptor - **RESOLVED**: Named `RunRules1`/`RunRules2` due to overloads
- [x] Verify delegate stub parameter mapping - **RESOLVED**: Works correctly
- [x] Generator: Return `Task.CompletedTask` for async delegate stubs by default - **FIXED**
- [x] Add documentation explaining pattern differences - **DONE**: Updated inline-stubs.md
- [x] Consider unifying API where possible - **DONE**: APIs now unified

## Test Coverage

- [x] Standalone stubs for IEntityBase (EntityBaseStub)
- [x] Standalone stubs for IValidateBase (ValidateBaseStub)
- [x] Inline stubs for IValidateBase
- [x] Inline stubs for IEntityBase (InlineEntityContainer) - **NEW**: Verifies duplicate indexer fix
- [x] Delegate stubs for NeatooPropertyChanged
- [x] Custom interfaces extending Neatoo types
- [x] Multiple interface inline stubs
- [x] Nested class stubs

## Changes Made

1. `src/Generator/KnockOffGenerator.GenerateInline.cs` - Fixed async delegate return value
2. `src/Generator/KnockOffGenerator.GenerateFlat.cs` - Removed dead code (legacy backing field methods)
3. `src/Tests/KnockOffTests/NeatooTests.cs` - Added `InlineEntityContainer` test, removed outdated workaround comment
4. `docs/guides/inline-stubs.md` - Added class/delegate stubbing to comparison table
5. Deleted stale generated files that had no source
