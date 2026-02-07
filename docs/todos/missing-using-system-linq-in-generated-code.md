# Missing 'using System.Linq;' in Generated Interceptor Code

**Status:** In Progress
**Priority:** High
**Created:** 2026-02-06
**Last Updated:** 2026-02-06

---

## Problem

When generating unified interceptors for methods with overloads, KnockOff generates a `TotalCallCount` property that uses `.Sum()` LINQ extension method but does not include `using System.Linq;` in the generated file, causing compilation errors:

```csharp
error CS1061: 'List<(...)>' does not contain a definition for 'Sum' and no accessible extension method 'Sum' accepting a first argument of type 'List<(...)>' could be found (are you missing a using directive or an assembly reference?)
```

**Generated code pattern:**
```csharp
// File: CustomTypeDescriptorTests.Stubs.g.cs
// Missing: using System.Linq;

private int TotalCallCount =>
    _unconfiguredCallCount +
    (_onCallTracking_NoParams?._callCount ?? 0) +
    (_sequence_NoParams?.Sum(s => s.Tracking._callCount) ?? 0) +  // ERROR: Sum not found
    (_whenChain_NoParams?.Sum(m => m.CallCount) ?? 0) +            // ERROR: Sum not found
    // ... repeats for each overload
```

This occurs for any interface method with multiple overloads because the aggregate verification pattern sums call counts across all overloads.

**Reproduction:**
```csharp
public interface ICustomTypeDescriptor
{
    EventDescriptorCollection GetEvents();
    EventDescriptorCollection GetEvents(Attribute[] attributes);
}

[KnockOff<ICustomTypeDescriptor>]
public partial class Tests { }
```

Results in 18 compilation errors in the generated code (multiple `.Sum()` calls per overload group).

## Solution

Add `using System.Linq;` to the using directives in generated stub files when the unified interceptor pattern is used (i.e., when any method has overloads).

**Location:** Likely in `UnifiedInterceptorBuilder.cs` or the file-level using directive generation logic in `StubFileRenderer.cs` / `MethodInterceptorRenderer.cs`.

**Alternatives considered:**
1. Use explicit `Enumerable.Sum()` calls instead of extension syntax
2. Implement manual summation with foreach loops (no Linq dependency)

**Recommended:** Add the using directive - simplest and most consistent with existing code patterns.

---

## Plans

[Plans will be linked here when created]

---

## Tasks

- [ ] Identify where using directives are generated for stub files
- [ ] Add conditional logic: if unified interceptor is used, include `using System.Linq;`
- [ ] Add test case for interface with method overloads
- [ ] Verify generated code includes the using directive
- [ ] Verify fix with dotnet/runtime System.ComponentModel.TypeConverter.Tests
- [ ] Consider: should Linq be always included, or only when needed?

---

## Progress Log

### 2026-02-06
- Discovered during migration of dotnet/runtime tests from Moq to KnockOff
- Affects ICustomTypeDescriptor.GetEvents and GetProperties (both have 2 overloads)
- 18 compilation errors total across multiple overload groups
- Blocks test execution in System.ComponentModel.TypeConverter.Tests
- Only occurs in KnockOff 0.37.0 (the version that fixed the overload generation bug)

---

## Completion Verification

Before marking this todo as Complete, verify:

- [ ] Design project with overloaded methods builds successfully
- [ ] All System.ComponentModel.TypeConverter.Tests compile without CS1061 errors
- [ ] Generated code includes `using System.Linq;` when needed
- [ ] Tests pass

**Verification results:**
- Design build: [Pending]
- Generated code inspection: [Pending]
- Tests: [Pending]

---

## Results / Conclusions

[What was learned? What decisions were made?]
