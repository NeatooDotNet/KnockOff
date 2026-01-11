# Generic Type API Cleanup

## Status: Complete

## Goal

Unify and simplify the public API for generic type handling across KnockOff features. Remove inconsistencies by using type suffixes for collision scenarios (e.g., `IListString` instead of `IList`).

## Current State

| Feature | Current Pattern | Issue |
|---------|-----------------|-------|
| Indexers | `StringIndexer`, `Int32Indexer` | Type embedded in name always |
| Inline generic stubs | `Stubs.IList` (type stripped) | Collision if same interface with different type args |
| Generic methods | `.Of<T>()` | Good - keep as-is |
| Interceptor classes | `Interface_MemberInterceptor` | Good - keep as-is (internal) |

## Proposed Design

### Indexers

**Single indexer (no collision):**
```csharp
knockOff.Indexer.OnGet = ...
knockOff.Indexer.Backing["key"] = value;
```

**Multiple key types (collision) - type suffix on property name:**
```csharp
knockOff.IndexerString.OnGet = ...
knockOff.IndexerString.Backing["key"] = value;

knockOff.IndexerInt.OnGet = ...
knockOff.IndexerInt.Backing[0] = value;
```

**Note:** `Backing` is a property on the interceptor, keeping everything accessible from one place.

### Inline Generic Stubs

**Single type argument (no collision):**
```csharp
[KnockOff<IList<string>>]
public partial class Tests
{
    public partial Stubs.IList List { get; }
}

var stub = new Stubs.IList();
```

**Multiple type arguments (collision) - type suffix on class name:**
```csharp
[KnockOff<IList<string>>]
[KnockOff<IList<int>>]
public partial class Tests
{
    public partial Stubs.IListString StringList { get; }
    public partial Stubs.IListInt IntList { get; }
}

var stringStub = new Stubs.IListString();
var intStub = new Stubs.IListInt();
```

**No factory methods** - just direct instantiation with type-suffixed class names.

### Type Name Suffixes

For collision cases, append type names as suffix (concatenated PascalCase, no separators):

| Generic Type | Stub Class Name |
|--------------|-----------------|
| `IList<string>` | `Stubs.IListString` |
| `IList<int>` | `Stubs.IListInt` |
| `IList<User>` | `Stubs.IListUser` |
| `IDictionary<string, int>` | `Stubs.IDictionaryStringInt` |
| `IDictionary<string, List<int>>` | `Stubs.IDictionaryStringListInt` |
| `IEnumerable<IFoo>` | `Stubs.IEnumerableIFoo` |
| `IList<string[]>` | `Stubs.IListStringArray` |
| `IList<int?>` | `Stubs.IListNullableInt32` |
| `IList<(string, int)>` | `Stubs.IListValueTupleStringInt32` |
| `IDictionary<string, List<Dictionary<int, User>>>` | `Stubs.IDictionaryStringListDictionaryInt32User` |

**Rule:** Concatenate all type argument names in PascalCase without separators.

**Edge case rules:**
- **Arrays**: Append `Array` → `StringArray`
- **Nullable value types**: Prefix with `Nullable` → `NullableInt32` (matches CLR naming)
- **Tuples**: Use `ValueTuple` + types → `ValueTupleStringInt32`
- **Deeply nested**: Accept long names (use stand-alone stub if too unwieldy)

## Tasks

### Phase 1: Design Decisions
- [x] Decide on type name formatting rules for complex types → Concatenate PascalCase suffix
- [x] Decide if `Backing` property moves inside interceptor → Yes (`Indexer.Backing`)
- [x] Decide if partial properties need special handling for collision case → Type-suffixed class names
- [x] Factory methods vs type suffixes → Type suffixes (simpler, supports partial properties)
- [x] Write design doc with examples for all edge cases

### Phase 2: Indexer Refactoring
- [x] Detect single vs multiple indexers in generator
- [x] Generate `Indexer` property when single indexer
- [x] Generate `IndexerString`, `IndexerInt` etc. when multiple (type suffix)
- [x] Move `Backing` property inside interceptor (`Indexer.Backing`)
- [x] Update all indexer tests
- [x] Update indexer documentation

### Phase 3: Inline Generic Stub Refactoring
- [x] Detect collision: same generic interface with different type args
- [x] Generate simple `Stubs.IList` class when no collision
- [x] Generate `Stubs.IListString`, `Stubs.IListInt` etc. when collision (type suffix)
- [x] Ensure partial properties work with type-suffixed class names
- [x] Update all inline generic stub tests
- [x] Update inline stubs documentation

### Phase 4: Documentation
- [x] Update `docs/guides/indexers.md`
- [x] Update `docs/guides/inline-stubs.md`
- [x] Update `docs/guides/generics.md` (no changes needed - focuses on explicit pattern)
- [x] Add migration notes for breaking changes (included in indexers.md)
- [x] Update skill documentation (verified via extract-snippets)

### Phase 5: Validation
- [x] Run full test suite (3137 tests pass across net8.0/net9.0/net10.0)
- [x] Test with Neatoo interfaces (473 interface tests pass)
- [x] Verify no regressions in BCL interface stubs

## Breaking Changes

This is a **breaking change** for:
1. Indexer access: `StringIndexer` → `Indexer` (single) or `IndexerString` (collision)
2. Backing dictionary: `StringIndexerBacking` → `Indexer.Backing` or `IndexerString.Backing`
3. Inline generic stubs with collision: now use type-suffixed class names

**Migration path:**
- Minor version bump (breaking changes already communicated to users)
- Clear migration guide in release notes

## Related Files

**Generator:**
- `src/Generator/KnockOffGenerator.GenerateFlat.cs` - Indexer generation (stand-alone stubs)
- `src/Generator/KnockOffGenerator.GenerateInline.cs` - Inline stub generation

**Tests:**
- `src/Tests/KnockOffTests/IndexerTests.cs`
- `src/Tests/KnockOffTests/InlineStubTests.cs`
- `src/Tests/KnockOff.Documentation.Samples/Guides/IndexersSamples.cs`
- `src/Tests/KnockOff.Documentation.Samples/Guides/InlineStubsSamples.cs`

**Docs:**
- `docs/guides/indexers.md`
- `docs/guides/inline-stubs.md`
- `docs/guides/generics.md`

## Design Decisions (Resolved)

1. **Nested generics**: Concatenate without separators → `IListListString`
2. **Multi-type-param generics**: Concatenate → `IDictionaryStringInt`
3. **Backing dictionaries**: Property on interceptor → `Indexer.Backing`
4. **Version**: Minor version bump acceptable (breaking changes communicated)
5. **Factory methods vs type suffixes**: Type suffixes (simpler, supports partial properties)
6. **Partial properties**: Work with type-suffixed class names → `Stubs.IListString`
