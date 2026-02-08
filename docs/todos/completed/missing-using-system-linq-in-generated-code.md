# Missing 'using System.Linq;' in Generated Interceptor Code

**Status:** Complete
**Priority:** High
**Created:** 2026-02-06
**Last Updated:** 2026-02-07

---

## Problem

When generating unified interceptors for methods with overloads, KnockOff generates a `TotalCallCount` property that uses `.Sum()` LINQ extension method but does not include `using System.Linq;` in the generated file, causing compilation errors:

```csharp
error CS1061: 'List<(...)>' does not contain a definition for 'Sum' and no accessible extension method 'Sum' accepting a first argument of type 'List<(...)>' could be found (are you missing a using directive or an assembly reference?)
```

This occurs for any interface method with multiple overloads because the aggregate verification pattern sums call counts across all overloads.

## Solution

Always emit `using System.Linq;` in all three renderers to ensure generated code is self-contained, regardless of whether the consumer project has `ImplicitUsings` enabled.

---

## Plans

No formal plan — bug fix applied directly.

---

## Tasks

- [x] Identify where using directives are generated for stub files
- [x] Change InlineRenderer to always emit `using System.Linq;` (was conditional on HasGenericMethods)
- [x] Change FlatRenderer to always emit `using System.Linq;` (was conditional on HasGenericMethods)
- [x] Add `using System.Linq;` to StandaloneClassRenderer (had none)
- [x] Add regression test exercising .Sum() via overloaded method verification

---

## Progress Log

### 2026-02-06
- Discovered during migration of dotnet/runtime tests from Moq to KnockOff

### 2026-02-07
- Fixed InlineRenderer.cs, FlatRenderer.cs (conditional → unconditional)
- Fixed StandaloneClassRenderer.cs (added missing `using System.Linq;`)
- Regression test in BugRegressionTests.cs exercises .Sum() path
- Note: test project has ImplicitUsings enabled so can't reproduce compile error directly
- All 6,316 tests pass across net8.0/net9.0/net10.0

---

## Results / Conclusions

The `using System.Linq;` was only emitted when `HasGenericMethods` was true, and StandaloneClassRenderer never emitted it at all. Changed all renderers to always include it, ensuring generated code works for consumers without ImplicitUsings. Fix applied in PR #57.
