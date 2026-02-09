# Interface Static Virtual Members

**Status:** Complete
**Priority:** High
**Created:** 2026-02-08
**Last Updated:** 2026-02-08

---

## Results / Conclusions

Fixed by adding `if (member.IsStatic) continue;` at 4 locations in `KnockOffGenerator.Transform.cs`. The generator now skips static virtual/abstract interface members during the Transform phase, preventing them from entering the builder/renderer pipeline.

- 4 code changes in 1 file (`KnockOffGenerator.Transform.cs`)
- 5 new tests in `InterfaceStaticVirtualTests.cs` (inline + standalone patterns)
- All 2396+ tests pass across all 3 TFMs, 0 regressions

---

## Problem

KnockOff's source generator does not filter out `static virtual` or `static abstract` interface members. When an interface contains these members (C# 11+), the generator attempts to create instance implementations for them, producing compiler errors.

Reported via comparison with [Rocks InterfaceStaticVirtualTests](https://github.com/keithdv/Rocks/blob/main/src/Rocks.Analysis.IntegrationTests/InterfaceStaticVirtualTests.cs) — `IHaveStaticVirtuals` cannot be stubbed.

### Root Cause

The member discovery code in `ExtractInterfaceInfo()` (Transform.cs) does not check `IMethodSymbol.IsStatic` or `IPropertySymbol.IsStatic`. Static virtual/abstract members pass all existing filters (`MethodKind.Ordinary`, accessibility) and are included in the generated stub, where they cause compiler errors because:
- Instance methods cannot implement static interface members
- The generated interceptor pattern doesn't apply to static members

### Affected Pipelines

No pipeline filters static members:

| Pipeline | Transform | Builder | Renderer |
|---|---|---|---|
| Standalone interface (1,2) | TransformClass | FlatModelBuilder | FlatRenderer |
| Standalone class (3,4) | TransformStandaloneClass | StandaloneClassModelBuilder | StandaloneClassRenderer |
| Inline interface/class (5,6) | TransformInlineStubClass | InlineModelBuilder | InlineRenderer |
| Open generic (7,8,9) | Various | Various | InlineRenderer |

## Solution

Filter out static members during the `ExtractInterfaceInfo()` phase (Transform.cs) so they never enter the pipeline. Static virtual/abstract members cannot be stubbed — they are implementation details of the type, not part of the instance contract.

## Plans

- [Filter Static Interface Members](../../plans/completed/filter-static-interface-members.md)

## Tasks

- [x] Reproduce the bug in KnockOffTests
- [x] Create implementation plan

## Progress Log

- 2026-02-08: Created todo. Reproduction test added in `InterfaceStaticVirtualTests.cs`.

