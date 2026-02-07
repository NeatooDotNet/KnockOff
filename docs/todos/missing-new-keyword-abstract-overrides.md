# Missing 'new' Keyword on Abstract Class Override Members

**Status:** In Progress
**Priority:** High
**Created:** 2026-02-06
**Last Updated:** 2026-02-06

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

**Reproduction:**
```csharp
[KnockOff<EventDescriptor>]
[KnockOff<PropertyDescriptor>]
public partial class CustomTypeDescriptorTests { }
```

**Generated code (WRONG):**
```csharp
public override bool Equals(object? obj) { ... }  // Missing 'new'
public override int GetHashCode() { ... }         // Missing 'new'
```

**Should generate:**
```csharp
public new bool Equals(object? obj) { ... }
public new int GetHashCode() { ... }
```

## Solution

When generating interceptor methods for abstract class stubs, detect when a method:
1. Is declared as `override` in the abstract base class
2. Overrides a member from `object` (Equals, GetHashCode, ToString)
3. Is being intercepted on the stub

In these cases, generate the interceptor method with `new` keyword instead of `override` keyword to properly hide the base implementation.

**Location:** Likely in `AbstractClassStubRenderer.cs` or method generation logic that handles abstract class interceptors.

---

## Plans

[Plans will be linked here when created]

---

## Tasks

- [ ] Identify where abstract class interceptor methods are generated
- [ ] Add detection for override-of-object-member pattern
- [ ] Emit `new` keyword instead of `override` for these cases
- [ ] Add test cases for EventDescriptor and PropertyDescriptor
- [ ] Add test case for other object overrides (ToString)
- [ ] Verify fix with dotnet/runtime System.ComponentModel.TypeConverter.Tests

---

## Progress Log

### 2026-02-06
- Discovered during migration of dotnet/runtime tests from Moq to KnockOff
- Affects System.ComponentModel.EventDescriptor and PropertyDescriptor
- 4 compilation errors total (Equals + GetHashCode for both classes)
- Blocks test execution in System.ComponentModel.TypeConverter.Tests

---

## Completion Verification

Before marking this todo as Complete, verify:

- [ ] Design project builds successfully with abstract classes that override object members
- [ ] All System.ComponentModel.TypeConverter.Tests compile without CS0108 errors
- [ ] Tests pass

**Verification results:**
- Design build: [Pending]
- Tests: [Pending]

---

## Results / Conclusions

[What was learned? What decisions were made?]
