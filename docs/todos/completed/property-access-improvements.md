# Property Access Improvements

Improve the developer experience for setting and getting property values on stubs.

**Status: COMPLETE** - All tasks completed 2026-01-10.

## Summary

Investigation revealed that:
1. The `As{InterfaceName}()` methods and `*Backing` properties only existed in **stale generated files** from deleted source code
2. The current generator already uses the correct flat API pattern with `Value` on interceptors
3. No generator changes were needed - just cleanup of orphaned files

## What Was Done

### Deleted Stale Generated Files

The following orphaned files were deleted (their source classes were removed but generated files remained):

**From KnockOff.Documentation.Samples:**
- DataContextKnockOff.g.cs (source commented out)
- MiCompositeRepositoryKnockOff.g.cs
- MiDataContextKnockOff.g.cs
- MiLoggerAuditorKnockOff.g.cs
- MiLoggerNotifierKnockOff.g.cs
- MigDataContextKnockOff.g.cs
- MmDataContextKnockOff.g.cs
- SkDataContextKnockOff.g.cs
- SkExampleKnockOff.g.cs

**From KnockOffTests:**
- MultiInterfaceKnockOff.g.cs
- ListIntKnockOff.g.cs
- EnumerableIntKnockOff.g.cs
- ConflictingSignatureKnockOff.g.cs
- SharedSignatureKnockOff.g.cs
- BuildingEditStub.g.cs
- ComplexEntityStub.g.cs
- PersonEditStub.g.cs
- NeatooTests.BuildingEditKnockOff.g.cs

**From KnockOff.NeatooInterfaceTests:**
- EntityBaseServicesStub.g.cs
- EntityPropertyManagerStubForServices.g.cs
- FactorySaveStubForServices.g.cs
- PropertyInfoListStubForEntityServices.g.cs
- PropertyInfoListStubForServices.g.cs
- RuleManagerStubForEntityServices.g.cs
- RuleManagerStubForServices.g.cs
- ValidateBaseServicesStub.g.cs
- ValidatePropertyManagerStubForEntityServices.g.cs
- ValidatePropertyManagerStubForServices.g.cs

### Verification

- All 473 NeatooInterfaceTests pass (net8.0, net9.0, net10.0)
- All 405 KnockOffTests pass (net8.0)
- All 242 Documentation.Samples.Tests pass (net9.0, net10.0)
- No `=> this;` (As{Interface} methods) remain in generated code
- No `protected.*Backing { get; set; }` patterns remain in generated code

## Task List

- [x] Investigate current state of `As{InterfaceName}()` methods
- [x] Investigate current state of `*Backing` properties
- [x] Delete stale generated files (28 files total)
- [x] Verify build passes
- [x] Verify all tests pass
- [x] Confirm no generator changes needed

## Generator Dead Code (Optional Cleanup)

The following method in `KnockOffGenerator.cs` is never called and could be removed in a future cleanup:
- `GenerateInterfaceKOClass` (line ~3181) - was used for multi-interface support

This is cosmetic and doesn't affect functionality.

## Original Problem (Already Solved)

The original problem statement assumed the generator was still producing these patterns. In fact:
- KO0010 already prevents multi-interface stubs
- The generator was updated to use the flat API pattern with `Value` on interceptors
- The problematic patterns only existed in stale files from before these changes

## Related Work

The following from `docs/todos/skill-unsnippeted-code.md` can remain as-is:
- The samples now use the correct flat API pattern
- No `As{InterfaceName}()` patterns exist in the active codebase
- The `*Backing` pseudocode in docs is acceptable (illustrative, not compiled)
