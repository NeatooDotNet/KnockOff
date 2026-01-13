# Generic Standalone Stubs Implementation Plan

## Goal

Support generic standalone stubs like:
```csharp
[KnockOff]
public partial class RepositoryStub<T> : IRepository<T> where T : class { }

// Usage:
var stub = new RepositoryStub<User>();
stub.GetById.OnCall = (ko, id) => new User { Id = id };
```

## Status: COMPLETE

All phases are done including documentation and skill updates. Generic standalone stubs are now fully supported.

**Completed:**
- Phases 1-7: Core implementation and testing
- Phase 8: Documentation (generics.md, README.md, diagnostics.md)
- Phase 9: Skill update (SKILL.md, migrations.md)

## Task List

### Phase 1: Data Model Updates

- [x] Add `TypeParameters` field to `KnockOffTypeInfo` in `Models/CommonModels.cs`
  - Type: `EquatableArray<TypeParameterInfo>`
  - Include constraints from `SymbolHelpers.GetTypeParameterConstraints()`

- [x] Add helper method `FormatTypeParameterList()` to `SymbolHelpers.cs`
  - Input: `EquatableArray<TypeParameterInfo>`
  - Output: `<T, U>` format string (empty if no type params)

- [x] Add helper method `FormatTypeConstraints()` to `SymbolHelpers.cs`
  - Input: `EquatableArray<TypeParameterInfo>`
  - Output: `where T : class where U : new()` format string

- [x] Add helper method `ExtractTypeParameters()` to `SymbolHelpers.cs`
  - Input: `IEnumerable<ITypeParameterSymbol>`
  - Output: `EquatableArray<TypeParameterInfo>`

### Phase 2: Predicate Updates

- [x] Modify `IsCandidateClass()` in `KnockOffGenerator.cs:244`
  - Removed the generic class rejection
  - Classes with type parameters now pass validation

- [x] Add type parameter arity validation for standalone stubs
  - Added `KO0008` diagnostic error on mismatch
  - Message: "Generic standalone stub '{0}' has {1} type parameter(s) but interface '{2}' has {3}. Type parameter count must match exactly."
  - No stub generated if arity mismatch

### Phase 3: Transform Updates

- [x] Update `TransformClass()` in `KnockOffGenerator.Transform.cs`
  - Extract type parameters from `classSymbol.TypeParameters`
  - Populate `TypeParameters` field on `KnockOffTypeInfo`
  - Uses `SymbolHelpers.ExtractTypeParameters()`

### Phase 4: Generation Updates

#### 4.1 Class Declaration
- [x] Update class signature generation in `GenerateKnockOff()`
  - Now generates: `partial class {ClassName}{TypeParams} {Constraints}`
  - Example: `partial class RepositoryStub<T> where T : class`

#### 4.2 Delegate Signatures
- [x] Update delegate generation to use class type parameters
  - `ko` parameter type now includes type arguments
  - Example: `public delegate T? GetByIdDelegate(RepositoryStub<T> ko, int id);`

#### 4.3 Interceptor Container Type References
- [x] Updated all places that reference the stub class type
  - All `Func<>` and `Action<>` callbacks include type parameters
  - Constructor parameters and field types are correct

#### 4.4 File Naming
- [x] Update hint name to sanitize generic class names
  - **Decision: Use arity suffix**
  - Example: `RepositoryStub`1.g.cs` for `RepositoryStub<T>`
  - Example: `DictionaryStub`2.g.cs` for `DictionaryStub<TKey, TValue>`

### Phase 5: Interface Name Handling

- [x] Interface-named properties automatically use type parameter names for open generics
  - Uses flat API (v10.9+) where interceptors are directly on the stub class
  - Example: `stub.GetById.OnCall` works for `IGenericRepository<T>.GetById`

### Phase 6: Testing

- [x] Create basic generic standalone stub test
  - `GenericRepositoryStub<T> : IGenericRepository<T>`

- [x] Test type parameter constraints are preserved
  - `ConstrainedRepositoryStub<T> : IConstrainedRepository<T> where T : class`

- [x] Test multiple type parameters
  - `GenericKeyValueStoreStub<TKey, TValue> : IGenericKeyValueStore<TKey, TValue>`

- [x] Test OnCall delegate signatures work correctly
  - Callback receives correctly typed `ko` parameter
  - Return type matches type parameter

- [x] Test instantiation with different type arguments
  - `new GenericRepositoryStub<User>()`
  - `new GenericRepositoryStub<TestEntity>()`

- [x] Verify generated files compile and output to `Generated/` folder
  - Files are at `Generated/KnockOff.Generator/KnockOff.KnockOffGenerator/*.g.cs`

All 14 new tests pass on .NET 8.0, 9.0, and 10.0.

### Phase 7: Edge Cases & Validation

- [x] Enforce exact type parameter count match between class and interface
  - Class `Stub<T>` implementing `IRepo<T>` → valid
  - Class `Stub<T, TExtra>` implementing `IRepo<T>` → KO0008 error, no stub generated
  - Class `Stub<T>` implementing `IRepo<T, U>` → KO0008 error, no stub generated

- [x] Handle nested type parameters (e.g., `IRepository<List<T>>`)
  - Works correctly: `INestedTypeParamService<T>` with `List<T> GetItems()` generates proper code
  - `Dictionary<TKey, TValue>` return types work when constraints match (e.g., `TKey : notnull`)
  - `Task<T?>` return types work for async methods

- [x] Handle variance (`in`/`out` modifiers on interface type params)
  - Covariance (`out T`): `ICovariantService<out T>` works correctly
  - Contravariance (`in T`): `IContravariantService<in T>` works correctly
  - Variance is on the interface, not the stub class - no special handling needed

- [x] Constraint mismatch handling
  - Class constraints must satisfy interface constraints (enforced by C# compiler)
  - No additional diagnostic needed - compiler already produces CS0314/CS0311 errors
  - Multiple constraints (`where T : class, IEntity`) preserved correctly

- [x] Test with nullable reference types enabled/disabled
  - Nullable return types (`T?`) work correctly
  - Non-nullable parameters work correctly
  - NRT annotations preserved in generated code

13 new edge case tests added in `GenericStandaloneEdgeCaseTests.cs`.

### Phase 8: Documentation

- [x] Update `docs/guides/generics.md` with generic standalone stub examples
- [x] Add code samples to `KnockOff.Documentation.Samples`
- [x] Update README.md to show generic standalone stub support
- [x] Document `KO0008` diagnostic in `docs/diagnostics.md` (created full diagnostics reference)

### Phase 9: Skill Update

- [x] Update `/knockoff` skill to document generic standalone stub support
- [x] Add examples showing `[KnockOff] partial class Stub<T> : IRepo<T>` pattern
- [x] Document the type parameter arity constraint and `KO0008` diagnostic

Updated files:
- `~/.claude/skills/knockoff/SKILL.md` - Added Generic Standalone Stubs section and feature table entry
- `~/.claude/skills/knockoff/migrations.md` - Added v10.14.0 section with feature and KO0008 diagnostic

## Key Files Modified

| File | Changes |
|------|---------|
| `src/Generator/Models/CommonModels.cs` | Added `TypeParameters` to `KnockOffTypeInfo` |
| `src/Generator/Models/SymbolHelpers.cs` | Added `FormatTypeParameterList()`, `FormatTypeConstraints()`, `ExtractTypeParameters()` |
| `src/Generator/KnockOffGenerator.cs` | Updated predicate, file naming, class declaration, delegate signatures |
| `src/Generator/KnockOffGenerator.Transform.cs` | Extract type parameters, arity validation with KO0008 |
| `src/Generator/KnockOffGenerator.GenerateInline.cs` | Added KO0008 to diagnostic switch |
| `src/Tests/KnockOffTests/GenericStandaloneStubTests.cs` | New test file with 14 tests |
| `src/Tests/KnockOffTests/GenericStandaloneEdgeCaseTests.cs` | Edge case tests (13 tests) |

## Design Decisions Made

1. **File naming convention for generic stubs**: Arity suffix
   - `RepositoryStub`1.g.cs` (arity-based)
   - Consistent with CLR naming, handles multiple type params well

2. **Interface property naming with open generics**: Uses flat API
   - With v10.9+ flat API, interceptors are directly on the stub
   - `stub.GetById` instead of `stub.IRepository_T.GetById`

3. **Constraint mismatch handling**: Not implemented yet
   - Currently, class constraints must satisfy interface constraints at compile time
   - Future: Could add diagnostic for constraint mismatches

## Notes

- Existing `TypeParameterInfo` record was reused (already has name + constraints)
- `SymbolHelpers.GetTypeParameterConstraints()` already existed and works
- All existing tests continue to pass (420+ tests on each .NET version)
