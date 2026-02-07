# Parameter Type Bugs in GetTypeSuffix

**Status:** Complete
**Priority:** High
**Created:** 2026-02-06
**Last Updated:** 2026-02-06

---

## Problem

`GetTypeSuffix(string)` (duplicated in 4 files) and `GetTypeSuffix(ITypeSymbol)` produce invalid or colliding C# identifiers for certain parameter types. The recent `string[]` fix (v0.37.0) addressed one case, but the same class of bug exists for other type constructs.

### Confirmed Bugs

**String-based `GetTypeSuffix` (UnifiedInterceptorBuilder + 3 copies in FlatModelBuilder, InlineModelBuilder, FlatRenderer):**

1. **Nullable types inside generics** — `Dictionary<string?, int>` produces `Collections_Generic_Dictionary_string?_int` — the `?` after `string` is embedded mid-identifier (invalid C#). `TrimEnd('?')` only removes trailing `?`.

2. **Tuples** — `(int, string)` produces `(int_string)` — parentheses are never stripped (invalid C#). Roslyn with `UseSpecialTypes` displays tuples as `(int, string)`, not `ValueTuple<int, string>`.

3. **Multidimensional arrays** — `int[,]` doesn't match `EndsWith("[]")` so the keyword `int` is never recognized. Falls through to fallback producing `int_` instead of `Int32Array2D` or similar.

**Symbol-based `GetTypeSuffix(ITypeSymbol)` in SymbolHelpers.cs:**

4. **Multidimensional arrays** — `IArrayTypeSymbol.Rank` is ignored. Both `string[]` and `string[,]` produce `"StringArray"` → suffix collision if both appear as overloads.

### Minor Issues

5. **Array of nullable elements** — `string?[]` → strips `[]` → leaves `string?` → doesn't match `"string"` keyword → fallback produces lowercase `stringArray` instead of `StringArray`.

6. **`nint`/`nuint`** — String-based version has no keyword mapping. Produces lowercase `nint` instead of `IntPtr` or `NativeInt`. (Symbol-based is fine — `Name` returns `IntPtr`.)

## Solution

Fix both `GetTypeSuffix` implementations to handle these type constructs correctly. Add comprehensive test interfaces exercising each problematic type, verify across applicable patterns.

### Affected Code

**String-based (4 copies):**
- `src/Generator/Builder/UnifiedInterceptorBuilder.cs` — `GetTypeSuffix(string)`
- `src/Generator/Builder/FlatModelBuilder.cs` — `GetTypeSuffix(string)`
- `src/Generator/Builder/InlineModelBuilder.cs` — `GetTypeSuffix(string)`
- `src/Generator/Renderer/FlatRenderer.cs` — `GetTypeSuffix(string)`

**Symbol-based:**
- `src/Generator/Models/SymbolHelpers.cs` — `GetTypeSuffix(ITypeSymbol)`

---

## Plans

- [Fix GetTypeSuffix Bugs for Identifier Generation](../plans/param-type-suffix-fixes.md)

---

## Tasks

- [x] Fix nullable-inside-generics (`?` mid-identifier)
- [x] Fix tuple parentheses handling
- [x] Fix multidimensional array handling (both string and symbol versions)
- [x] Fix array-of-nullable keyword matching (`string?[]`)
- [x] Add `nint`/`nuint` to keyword map
- [x] Add test interfaces for each bug category
- [x] Add tests exercising each fix across applicable patterns
- [x] Verify existing tests still pass

---

## Progress Log

### 2026-02-06
- Analyzed `GetTypeSuffix(string)` and `GetTypeSuffix(ITypeSymbol)` implementations
- Traced each problematic type through the string-based processing pipeline
- Identified 4 confirmed bugs and 2 minor issues
- Researched real-world parameter types from dotnet/aspnetcore for test inspiration
- Created todo
- Architect created plan, developer reviewed and approved
- Implementation completed: consolidated 4 copies into 1, fixed all 6 bugs, added 16 tests
- Architect verified: all 3,571 tests pass per framework, zero failures

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] Design project builds successfully
- [x] Design project tests pass

**Verification results:**
- Design build: Pass (all TFMs)
- Design tests: 259 passed per TFM, 0 failed

---

## Results / Conclusions

- Consolidated 4 duplicate copies of `GetTypeSuffix(string)` into a single canonical implementation in `UnifiedInterceptorBuilder`
- Fixed 6 bugs: nullable-inside-generics, tuple parentheses, multidimensional arrays (string and symbol), array-of-nullable keyword matching, `nint`/`nuint` mapping
- Also fixed discovered inconsistency: `void` was mapped to lowercase `"void"` in UnifiedInterceptorBuilder (now `"Void"`)
- Also fixed: 3 copies had incomplete keyword maps (missing `short`, `uint`, `ulong`, `ushort`, `sbyte`, `object`)
- Added 16 tests covering all bug categories for standalone (Pattern 1) and inline (Pattern 5) patterns
- Used `List<string?>` instead of `Dictionary<string?, int>` for nullable-inside-generics testing since `string?` violates Dictionary's `notnull` TKey constraint
