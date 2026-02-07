# Array Parameter Types Produce Invalid Generated Identifiers

**Status:** Complete
**Priority:** High
**Created:** 2026-02-06
**Last Updated:** 2026-02-06

---

## Problem

When an interface has method overloads where one parameter is an array type (e.g., `string[]`), KnockOff generates C# identifiers containing `[]` characters, which are invalid. This causes hundreds of compilation errors in the generated code.

Reported against v0.36.0. Reproduction at `C:\Users\KeithVoels\.claude\projects\c--src-neatoodotnet-runtime\scratchpad\KnockOffBugRepro\KnockOffMethodOverloadBug\`.

### Reproduction

```csharp
public interface ITestInterface
{
    MyCollection GetItems();
    MyCollection GetItems(string[] filters);
}

[KnockOff<ITestInterface>]
public partial class BugReproTests { }
```

### Errors

- `CS0102`: Duplicate member `_KnockOffMethodOverloadBug_MyCollection` (the `[]` in identifiers causes the parser to misinterpret field names)
- `CS0246`: Missing delegate type `GetItemsDelegate_NoParams_KnockOffMethodOverloadBug_MyCollection` (because the "matching" delegate has `[]` in its name)
- `CS0111`: Duplicate `Verify()` and `Reset()` methods (same parser misinterpretation)

### Impact

Blocks stubbing any interface with array parameters in overloaded methods, including `System.ComponentModel.ICustomTypeDescriptor` in dotnet/runtime.

## Root Cause

`GetTypeSuffix(string type)` does not handle `[]` array brackets. When `string[]` is passed, it falls through the switch and gets embedded verbatim into identifiers like `GetItemsDelegate_string[]_KnockOff_Tests_MyCollection` — invalid C#.

There is already a correct implementation in `SymbolHelpers.GetTypeSuffix(ITypeSymbol)` that handles `IArrayTypeSymbol` by recursing on the element type and appending `"Array"`. But the four string-based copies of `GetTypeSuffix` lack this handling.

## Solution

Add array bracket handling to all four string-based `GetTypeSuffix` methods:
- `UnifiedInterceptorBuilder.GetTypeSuffix`
- `FlatModelBuilder.GetTypeSuffix`
- `InlineModelBuilder.GetTypeSuffix`
- `FlatRenderer.GetTypeSuffix`

Strip `[]` suffixes before the switch so that element types get proper normalization (e.g., `string` → `String`), then append `"Array"` per stripped pair. Also add `[` and `]` to the default case replacements for multi-dimensional arrays.

Result: `string[]` → `StringArray`, `int[][]` → `Int32ArrayArray`, `int[,]` → handled by bracket replacement.

---

## Plans

- [Fix Array Type Handling in GetTypeSuffix(string)](../plans/array-type-suffix-fix.md)

---

## Tasks

- [x] Fix `GetTypeSuffix` in all four locations
- [x] Add repro interface to test project
- [x] Verify build succeeds with inline pattern (Pattern 5)
- [x] Verify build succeeds with standalone pattern (Pattern 1)
- [x] Add tests exercising the overload (OnCall, Verify) — 10 new tests
- [x] Run full test suite — no regressions (3500+ tests pass)

---

## Progress Log

### 2026-02-06
- Reproduced bug: added `IParamlessOverloadService` to TestInterfaces.cs, confirmed 888 errors
- Identified root cause: `GetTypeSuffix(string)` doesn't strip `[]` from array types
- Found five copies of `GetTypeSuffix` — four string-based (all affected) plus one `ITypeSymbol`-based (already correct)
- Reverted premature fix to follow proper workflow
- Architect created plan, developer reviewed and approved
- Developer implemented fix in all four copies, added Design.Domain interface, Design.Stubs, 10 tests
- Architect independently verified: all builds pass, all 3500+ tests pass, implementation matches design

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] Design project builds successfully
- [x] Design project tests pass

**Verification results:**
- Design build: Pass (0 warnings, 0 errors across net8.0/net9.0/net10.0)
- Design tests: Pass (259 tests across all frameworks)

---

## Results / Conclusions

Root cause was `GetTypeSuffix(string)` not handling `[]` array brackets — they were embedded verbatim into C# identifiers. Fixed by stripping `[]` suffixes before the type-name switch (so element types get proper normalization like `string` → `String`), then appending `"Array"` per stripped pair. Also added `[`/`]` to the default case character replacements for multi-dimensional arrays.

Four copies fixed, 10 new tests added, Design.Domain interface and Design.Stubs created for ongoing compilation verification.
