# Class Stub Generator Bugs: TypeDescriptionProvider

**Status:** Not Started
**Priority:** High
**Created:** 2026-02-07
**Last Updated:** 2026-02-07

---

## Problem

`[KnockOff<TypeDescriptionProvider>]` generates code that does not compile. Two separate issues:

### Bug 1: CS0115 — Override of non-overridable method `RegisterType`

The generator attempts to override `RegisterType()`, but there is no suitable virtual/abstract method to override on `TypeDescriptionProvider`.

```
error CS0115: 'Stubs.TypeDescriptionProvider.RegisterType(Type)': no suitable method found to override
```

`TypeDescriptionProvider.RegisterType(Type)` is likely a static or non-virtual method that the generator incorrectly treats as overridable.

### Bug 2: CS0507 — Access modifier change on `protected internal` member

The generator changes the access modifier when overriding `GetExtenderProviders`:

```
error CS0507: cannot change access modifiers when overriding 'protected internal' inherited member 'TypeDescriptionProvider.GetExtenderProviders(object)'
```

The generator likely emits `protected override` instead of `protected internal override`.

### Reproduction

```csharp
[KnockOff<TypeDescriptionProvider>]
public partial class MyTests
{
    // Generated Stubs.TypeDescriptionProvider fails to compile
}
```

### Impact

Found during dotnet/runtime migration of `TypeDescriptionProviderTests.cs`. Without the class stub, 14+ tests that mock `TypeDescriptionProvider` as a parent cannot be migrated from Moq to KnockOff. This includes:
- `CreateInstance_InvokeWithParent_ReturnsExpected`
- `GetTypeDescriptor_InvokeWithParent_*` (multiple overloads)
- `GetExtendedTypeDescriptor_InvokeWithParent_ReturnsExpected`
- `GetFullComponentName_InvokeWithParent_ReturnsExpected`
- `GetReflectionType_InvokeWithParent_*` (multiple overloads)
- `GetRuntimeType_Invoke_ReturnsExpected`
- `IsSupportedType_InvokeWithParent_ReturnsExpected`

---

## Plans

---

## Tasks

- [ ] Reproduce with minimal test case
- [ ] Fix generator to skip non-virtual/static methods (Bug 1)
- [ ] Fix generator to preserve `protected internal` access modifier (Bug 2)
- [ ] Add regression tests for both bugs
- [ ] Re-test with dotnet/runtime TypeDescriptionProviderTests.cs

---

## Progress Log

- 2026-02-07: Found during dotnet/runtime migration. Two generator bugs prevent `[KnockOff<TypeDescriptionProvider>]` from compiling.

---

## Results / Conclusions

