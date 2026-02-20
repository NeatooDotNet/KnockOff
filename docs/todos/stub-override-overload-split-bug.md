# Bug: Stub Override Splits Overload Groups in Flat Pipeline

**Status:** In Progress
**Priority:** High
**Date:** 2026-02-19
**Last Updated:** 2026-02-19

---

## Plans

- [Fix: Stub Override Splits Overload Groups in Flat Pipeline](../plans/stub-override-overload-split-bug.md)

---

## Summary

When a user provides a stub override for one overload of an overloaded method, the Flat pipeline incorrectly splits the overload group into two separate groups. This breaks delegate generation, overload numbering, and tracking handles.

## Reproduction

```csharp
public interface IMethodOverloadService
{
    string Format(string input);
    string Format(string input, bool uppercase);
    string Format(string input, int maxLength);
}

[KnockOff]
public partial class MethodOverloadServiceKnockOff : IMethodOverloadService
{
    // User provides stub override for ONE overload
    protected override string Format_(string input)
    {
        return base.Format_(input);
    }
}
```

**Expected:** All three `Format` overloads remain in the same overload group. The stub override is wired as fallback for the matching signature only.

**Actual:** `Format(string input)` (with stub override) is split into `flatStubOverrideGroups`, while `Format(string input, bool uppercase)` and `Format(string input, int maxLength)` go into `flatMethodGroups`. This causes:
- `FormatDelegate` does not take 2 arguments (CS1593)
- `FormatImpl` does not contain `LastArgs` (CS1061)

## Root Cause

`FlatModelBuilder.cs` lines 49-69 filter methods by `HasStubOverride` into separate groups:
- `flatMethodGroups` excludes `HasStubOverride` methods (line 50)
- `flatStubOverrideGroups` includes only `HasStubOverride` methods (line 62)

The overload-counting logic (line 757-760) also excludes `HasStubOverride` methods, so the remaining 2 overloads may not be recognized as an overload group correctly.

Additionally, the name map builder (`AssignNamesForOverloadGroup`) splits overloads with and without stub overrides into DIFFERENT interceptor names, assigning "Format" to the stub override overload and "Format2" to the remaining overloads.

## Investigation Results

### Affected Pipelines

- **Flat pipeline** (`FlatModelBuilder` / `FlatRenderer`) -- confirmed broken
- **StandaloneClass pipeline** -- NOT affected. `StandaloneClassModelBuilder` groups all overloads together with per-signature `StubOverrideName` via `MethodSignatureInfo`.
- **Inline pipeline** -- NOT affected. Inline stubs do not support stub overrides.

### Evidence

Confirmed by examining generated code for `PartialOverloadStubOverrideStub` in Design.Stubs:
- Generates `FormatInterceptor` (1 overload) and `Format2Interceptor` (2 overloads) instead of a single interceptor
- User sees `stub.Format` and `stub.Format2` instead of unified `stub.Format`

Contrast with `OverloadedStubOverrideStub` where ALL overloads have stub overrides -- this correctly generates a single `FormatInterceptor` with 3 overloads because the name map assigns one name when all overloads are in the same group.

## Scope

- [x] All 9 patterns verified -- only Flat pipeline (patterns 1-2) is affected
- [x] Methods only -- properties/indexers/events don't have overloads in this context
- [x] StandaloneClass pipeline has the correct approach (per-signature stub override tracking)
