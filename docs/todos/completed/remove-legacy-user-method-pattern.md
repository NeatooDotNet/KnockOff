# Remove Legacy User Method Pattern

**Status:** Complete
**Priority:** High
**Created:** 2026-02-03
**Completed:** 2026-02-03

---

## Problem

The base class user methods feature was implemented as a **breaking change** that should have replaced the old signature-matching pattern. However:

1. **The old detection logic was never removed** - The generator still supports both patterns:
   - NEW: `protected override GetUserById_(int id)` (base class pattern)
   - OLD: `protected GetUserById(int id)` (signature matching)

2. **Documentation.Samples was overlooked** - The Implementation Contract listed files to migrate but completely missed `KnockOff.Documentation.Samples/UserMethodsSamples.cs`, which is the source for public markdown documentation via mdsnippets.

3. **Documentation is internally inconsistent** - The markdown prose in `docs/guides/user-methods.md` describes the new pattern (override + underscore), but the code samples show the old pattern (plain protected methods).

4. **mdsnippets was never run** - After updating samples, mdsnippets should verify the sync works.

## Solution

1. Remove the legacy user method detection logic from the generator
2. Migrate all samples in `KnockOff.Documentation.Samples/UserMethodsSamples.cs` to the new pattern
3. Run mdsnippets to sync the updated samples to markdown
4. Verify documentation is now consistent

---

## Plans

- [Remove Legacy User Method Pattern Plan](../plans/remove-legacy-user-method-pattern.md)

---

## Tasks

- [x] Enumerate ALL files containing user method samples
- [x] Remove legacy signature-matching detection from generator
- [x] Migrate `Documentation.Samples/UserMethodsSamples.cs` to new pattern
- [x] Run mdsnippets and verify sync
- [x] Verify all tests pass
- [x] Verify documentation is consistent

---

## Progress Log

**2026-02-03:** Implementation complete. All three phases executed successfully:
- Phase 1: Migrated 6 test files to new `protected override MethodName_()` pattern
- Phase 2: Removed legacy code from 7 generator files
- Phase 3: Ran mdsnippets, verified documentation shows correct pattern

---

## Results / Conclusions

**Successfully removed legacy user method pattern.** The generator now only supports the new base class pattern (`protected override MethodName_()`).

**Files modified:**

Generator files:
- `KnockOffGenerator.Helpers.cs` - Removed `GetUserDefinedMethods()` and `GetMethodSignature()`
- `KnockOffGenerator.Transform.cs` - Removed `UserMethods` parameter
- `CommonModels.cs` - Removed `UserMethods` from `KnockOffTypeInfo`
- `MethodModels.cs` - Removed `UserMethodInfo` record
- `FlatMethodModel.cs` - Removed `UserMethodCall` property
- `FlatModelBuilder.cs` - Removed `FindUserMethod()` and legacy code paths
- `FlatRenderer.cs` - Removed `RenderUserMethodImplementation()` and legacy conditionals

Test files migrated:
- `TestInterfaces.cs`
- `BasicTests.cs`, `AsyncMethodTests.cs`, `CallbackTests.cs`
- `KnockOffSandbox/Program.cs`
- `PackageTest/Program.cs`
- `UserMethodsSamples.cs`
- `GenericMethodBugTests.cs` (updated to use OnCall instead of legacy user methods)

**All tests pass. Documentation is now consistent with the new pattern.**
