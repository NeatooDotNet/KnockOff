# Bug: Missing global:: Prefix in Fully Qualified Names

## Problem

When generating stubs for interfaces in namespaces that conflict with type names in scope, the generated code fails to compile because fully qualified names are not prefixed with `global::`.

## Reproduction

Given:
- A domain class `DomainModel.Person`
- An interface `Person.Ef.IPersonDbContext` (in the `Person.Ef` namespace)
- A test class in `DomainModel.Tests.UnitTests`

When generating `[KnockOff<IPersonDbContext>]`, the generator produces:

```csharp
// Generated code references Person.Ef.IPersonDbContext
// But Person resolves to DomainModel.Person (the class), not the Person namespace
```

Error:
```
error CS0426: The type name 'Ef' does not exist in the type 'Person'
error CS0538: 'Person.Ef.IPersonDbContext' in explicit interface declaration is not an interface
```

## Expected Behavior

The generator should use `global::` prefix for all fully qualified type names:

```csharp
global::Person.Ef.IPersonDbContext  // Unambiguous reference to namespace
```

## Workaround

- Rename the conflicting class or namespace
- Keep using Moq for affected interfaces

## Task List

- [x] Update generator to prefix all FQN type references with `global::`
- [x] Add test case for namespace/type name collision
- [x] Verify fix doesn't break existing generated code

## Fix Applied

Updated `KnockOffGenerator.cs` to use `FullyQualifiedWithNullability` format (which includes `global::` prefix) for all type references that appear in generated code:

1. `ExtractInterfaceInfo()` - interface and base interface full names
2. `ExtractDelegateInfo()` - delegate full name, return type, and parameter types
3. `ExtractClassInfo()` - class full name
4. `TransformInlineStub()` - interface and base interface full names
5. `FlattenAndDeduplicateMembers()` - interface inheritance lookup (for consistency)

## Test Case

Files:
- `src/Tests/KnockOffTests/NamespaceCollisionTests.cs` - Test class
- `src/Tests/KnockOffTests/NamespaceCollisionStubs.cs` - Stubs in `DomainModel.Tests` namespace
- `src/Tests/KnockOffTests/NamespaceCollisionTypes.cs` - `DomainModel.Person` class and `Person.Ef.IPersonDbContext` interface

## Verification

- All 1852 tests pass across net8.0, net9.0, and net10.0
- Generated code now uses `global::Person.Ef.IPersonDbContext` instead of `Person.Ef.IPersonDbContext`
