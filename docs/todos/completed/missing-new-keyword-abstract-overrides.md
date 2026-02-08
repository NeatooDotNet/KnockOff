# Missing 'new' Keyword on Abstract Class Override Members

**Status:** Complete
**Priority:** High
**Created:** 2026-02-06
**Last Updated:** 2026-02-07

---

## Problem

When generating stubs for abstract classes that override `Equals(object)` and `GetHashCode()` from `object`, KnockOff generates these methods without the `new` keyword, causing CS0108 compiler warnings/errors:

```csharp
error CS0108: 'CustomTypeDescriptorTests.Stubs.EventDescriptor.Equals' hides inherited member 'object.Equals(object?)'. Use the new keyword if hiding was intended.
error CS0108: 'CustomTypeDescriptorTests.Stubs.EventDescriptor.GetHashCode' hides inherited member 'object.GetHashCode()'. Use the new keyword if hiding was intended.
```

This occurs when stubbing abstract classes like `System.ComponentModel.EventDescriptor` and `System.ComponentModel.PropertyDescriptor` that declare:
- `public override bool Equals(object obj)`
- `public override int GetHashCode()`

## Solution

When generating interceptor methods for abstract class stubs, detect when a method:
1. Is declared as `override` in the abstract base class
2. Overrides a member from `object` (Equals, GetHashCode, ToString)
3. Is being intercepted on the stub

In these cases, generate the interceptor method with `new` keyword instead of `override` keyword to properly hide the base implementation.

---

## Plans

No formal plan — bug fix applied directly.

---

## Tasks

- [x] Identify where abstract class interceptor methods are generated
- [x] Add `NeedsNewKeyword()` helper to ClassModelBuilder and StandaloneClassModelBuilder
- [x] Fix ClassRenderer and StandaloneClassRenderer to emit `new` when NeedsNewKeyword is true
- [x] Add regression tests for all 4 class stub patterns (3, 4, 6, 9)

---

## Progress Log

### 2026-02-06
- Discovered during migration of dotnet/runtime tests from Moq to KnockOff
- Affects System.ComponentModel.EventDescriptor and PropertyDescriptor

### 2026-02-07
- Fixed builders: ClassModelBuilder.cs, StandaloneClassModelBuilder.cs (NeedsNewKeyword: false → NeedsNewKeyword(memberName))
- Fixed renderers: ClassRenderer.cs, StandaloneClassRenderer.cs (added `new` keyword emission)
- Created BugRegressionTests.cs with 4 tests covering all class stub patterns
- All 6,316 tests pass across net8.0/net9.0/net10.0

---

## Results / Conclusions

The bug had two parts: builders hardcoded `NeedsNewKeyword: false` for all class members, and renderers didn't use the `NeedsNewKeyword` flag at all. The InlineRenderer (for interface stubs) already had the correct pattern — ClassRenderer and StandaloneClassRenderer were missing it. Fix applied in PR #57.
