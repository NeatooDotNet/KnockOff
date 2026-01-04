# Smart Default Return Values

## Goal

Make KnockOff stubs more forgiving by returning sensible defaults instead of throwing `InvalidOperationException` when methods/properties aren't explicitly configured.

## Decision Tree

| Type Category | Example | Default Value |
|---------------|---------|---------------|
| Value type | `int`, `bool`, `DateTime` | `default` |
| Nullable reference | `MyClass?`, `string?` | `default!` (null) |
| Non-nullable with `new()` | `List<T>`, `MyClass` | `new T()` |
| Non-nullable without `new()` | `string`, abstract class | **throw** (no safe option) |

## Implementation Tasks

### Phase 1: Model Changes

- [x] Add `DefaultValueStrategy` enum to `KnockOffGenerator.cs`:
  ```csharp
  internal enum DefaultValueStrategy
  {
      Default,           // Use default/default! (value types, nullable refs)
      NewInstance,       // Use new T() (has parameterless ctor)
      ThrowException     // No safe default available
  }
  ```

- [x] Add `DefaultValueStrategy` field to `InterfaceMemberInfo` record

- [x] Add helper method to determine strategy:
  ```csharp
  private static DefaultValueStrategy GetDefaultValueStrategy(ITypeSymbol type)
  {
      // Value types: always use default
      if (type.IsValueType)
          return DefaultValueStrategy.Default;

      // Nullable reference types: use default (null is valid)
      if (type.NullableAnnotation == NullableAnnotation.Annotated)
          return DefaultValueStrategy.Default;

      // Non-nullable reference: check for parameterless ctor
      if (type is INamedTypeSymbol named &&
          !named.IsAbstract &&
          named.TypeKind == TypeKind.Class)
      {
          var hasCtor = named.Constructors.Any(c =>
              c.Parameters.Length == 0 &&
              c.DeclaredAccessibility >= Accessibility.Public);

          if (hasCtor)
              return DefaultValueStrategy.NewInstance;
      }

      // No safe default available
      return DefaultValueStrategy.ThrowException;
  }
  ```

### Phase 2: Transform Changes

- [x] Update `CreatePropertyInfo` to call `GetDefaultValueStrategy(property.Type)`
- [x] Update `CreateMethodInfo` to call `GetDefaultValueStrategy` on return type
  - Handle `Task<T>` / `ValueTask<T>` - check inner type's strategy

### Phase 3: Code Generation Changes

Update all locations that currently throw or return default:

- [x] **Method generation** (~lines 2208-2242):
  - Value type / nullable: `return default!;`
  - NewInstance: `return new T();`
  - ThrowException: keep existing throw

- [x] **Task<T> / ValueTask<T>** (~lines 2213-2234):
  - Value type / nullable: `Task.FromResult(default!)`
  - NewInstance: `Task.FromResult(new T())`
  - ThrowException: keep existing throw

- [x] **Indexer getters** (~lines 2063-2070, 2370-2377):
  - Same logic as methods

- [x] **Interface-scoped method generation** (~lines 2520-2565):
  - Same logic as above

### Phase 4: Tests

- [x] Add test interface with various return types:
  ```csharp
  public interface ISmartDefaultsService
  {
      // Value types - return default
      int GetInt();
      bool GetBool();
      DateTime GetDateTime();

      // Nullable refs - return default (null)
      string? GetNullableString();
      TestEntity? GetNullableEntity();

      // Non-nullable with parameterless ctor - return new T()
      List<string> GetList();
      Dictionary<string, int> GetDictionary();
      TestEntity GetEntity();

      // Non-nullable without parameterless ctor - throw
      string GetString();
      IDisposable GetDisposable();

      // Task<T> variants - same strategy for inner type
      Task<int> GetIntAsync();
      Task<List<string>> GetListAsync();
      Task<string> GetStringAsync();

      // Properties
      int Count { get; }
      List<string> Items { get; }
  }
  ```

- [x] Verify each scenario returns expected default/new instance/throws (18 tests)

### Phase 5: Collection Interface Mapping

For interface return types that are well-known collection interfaces, map to concrete implementations:

| Interface | Concrete Type |
|-----------|---------------|
| `IEnumerable<T>` | `List<T>` |
| `ICollection<T>` | `List<T>` |
| `IList<T>` | `List<T>` |
| `IReadOnlyList<T>` | `List<T>` |
| `IReadOnlyCollection<T>` | `List<T>` |
| `IDictionary<K,V>` | `Dictionary<K,V>` |
| `IReadOnlyDictionary<K,V>` | `Dictionary<K,V>` |
| `ISet<T>` | `HashSet<T>` |

- [x] Add `ConcreteTypeForNew` field to `InterfaceMemberInfo` record
- [x] Create `GetCollectionInterfaceMapping` helper method
- [x] Update `GetDefaultValueStrategy` to return concrete type for collection interfaces
- [x] Update code generation to use `ConcreteTypeForNew ?? ReturnType`
- [x] Add tests for `IList<T>` (19 tests now passing)

## Files to Modify

1. `src/Generator/KnockOffGenerator.cs`
   - Add `DefaultValueStrategy` enum (~line 2641, near other enums)
   - Add `GetDefaultValueStrategy` helper method
   - Update `InterfaceMemberInfo` record (~line 2612)
   - Update `CreatePropertyInfo` (~line 222)
   - Update `CreateMethodInfo` (~line 272)
   - Update method code generation (~lines 2208-2242, 2520-2565)
   - Update indexer code generation (~lines 2063-2070, 2370-2377)

2. `src/Tests/KnockOff.GeneratorTests/` - Add smart defaults tests

## Edge Cases to Consider

- **Arrays (`T[]`)**: No parameterless ctor, would throw. Could special-case to `Array.Empty<T>()` but that's optimization for later.
- **Interfaces as return type**: Can't instantiate, must throw.
- **Abstract classes**: Can't instantiate, must throw.
- **Structs with parameterless ctor**: Already handled by `IsValueType` check.
- **Generic type parameters**: If constrained with `new()`, could detect, but complex. Defer.

## Notes

- The `IsNullable` field on `InterfaceMemberInfo` is still useful for nullable annotation in generated code
- `DefaultValueStrategy` is about what to do when no callback/user method is provided
- These are orthogonal concerns that work together
