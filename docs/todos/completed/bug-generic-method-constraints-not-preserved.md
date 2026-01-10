# Bug: Generic Method Constraints Not Preserved (FIXED)

**Status:** Completed
**Fixed in:** This session (v10.11.2)

## Problem

When generating explicit interface implementations for generic methods, the generator did not copy the type constraints from the interface method to the generated implementation.

## Solution

Updated `GetConstraintsForExplicitImpl()` in `KnockOffGenerator.cs` to:
1. Always emit `where T : class` when the original interface has a `class` constraint
2. Always emit `where T : struct` when the original interface has a `struct` constraint
3. Emit `where T : class` when the return type is `T?` (nullable type parameter) AND the type parameter has any constraint that implies it's a reference type (like `Attribute`)

This is necessary because in explicit interface implementations, only `class` and `struct` constraints are allowed (CS0460). Type constraints like `Attribute` can't be repeated, but they imply the type is a class, so we emit `class` to make `T?` work correctly as a nullable reference type.

## Reproduction

Interface:
```csharp
public interface IPropertyInfo
{
    T? GetCustomAttribute<T>() where T : Attribute;
}
```

Generated code (before fix - incorrect):
```csharp
T? global::Neatoo.IPropertyInfo.GetCustomAttribute<T>()
{
    // ...
}
```

Generated code (after fix - correct):
```csharp
T? global::Neatoo.IPropertyInfo.GetCustomAttribute<T>() where T : class
{
    // ...
}
```

## Test Cases Added

See `GenericMethodBugTests.cs`:
- `ConstrainedGeneric_WithTypeConstraint_CompilesAndWorks`
- `ConstrainedGeneric_WithTypeConstraint_CanReturnNull`
- `ConstrainedGeneric_WithClassConstraint_Works`
- `ConstrainedGeneric_MultipleTypeParams_Works`
- `ConstrainedGeneric_InterfaceConstraint_Works`

## Files Changed

- `src/Generator/KnockOffGenerator.cs` - Added `GetConstraintsForExplicitImpl()` method
- `src/Tests/KnockOffTests/GenericMethodBugTests.cs` - Added test interface and tests

## Task List (All Completed)

- [x] Find where generic method signatures are built in `KnockOffGenerator.cs`
- [x] Create smart constraint handling for explicit interface implementations
- [x] Generate constraint clause (`where T : class`) when needed for nullable returns
- [x] Handle multiple constraints correctly
- [x] Add test case for generic method with constraints
- [x] Verify fix with existing tests (406 tests pass)

## References

- Discovered in: `KnockOff.NeatooInterfaceTests` project
- Neatoo interface: `IPropertyInfo.GetCustomAttribute<T>() where T : Attribute`
