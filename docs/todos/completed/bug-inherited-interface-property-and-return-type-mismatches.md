# Bug: Inherited Interface Property and Return Type Mismatches

**Status:** Fixed
**Discovered in:** KnockOff.NeatooInterfaceTests, KnockOffTests
**Related to:** bug-inherited-interface-method-parameter-types (fixed)
**Related errors:** CS1503, CS0030, CS0266

## Problem

Similar to the fixed bug for method parameter types, the generator had issues when:
1. A generic interface inherits from a non-generic interface with **properties** of different types
2. A generic interface inherits from a non-generic interface with **methods that have different return types**

## Pattern 1: Properties with Different Types

**Example:** `IProperty<T>` inheriting from `IProperty`

```csharp
public interface IProperty
{
    object Value { get; set; }
}

public interface IProperty<T> : IProperty
{
    new T Value { get; set; }
}
```

**Fix:** Added property delegation support for both inline and standalone stubs:
- `FindPropertyDelegationTarget()` - Finds typed counterpart property in a single interface
- `FindPropertyDelegationTargetInInterfaces()` - Searches across all implemented interfaces
- `GenerateInlineStubPropertyDelegationImplementation()` - Generates delegation for inline stubs
- `GenerateFlatPropertyDelegationImplementation()` - Generates delegation for standalone stubs

The base property now delegates to the typed property:
```csharp
object IProperty.Value
{
    get => ((IProperty<string>)this).Value;
    set => ((IProperty<string>)this).Value = (string)value;
}
```

## Pattern 2: Methods with Different Return Types

**Example:** `IEnumerable<T>` inheriting from `IEnumerable`

```csharp
public interface IEnumerable
{
    IEnumerator GetEnumerator();
}

public interface IEnumerable<T> : IEnumerable
{
    new IEnumerator<T> GetEnumerator();
}
```

**Fix:** Extended method delegation to handle return type mismatches for standalone stubs:
- `FindDelegationTargetInInterfaces()` now checks return types
- Only delegates from non-generic to generic return types (e.g., `IEnumerator` to `IEnumerator<T>`)
- Rejects delegation when return types are incompatible (e.g., `void` to `bool`)

The base method now delegates to the typed method:
```csharp
IEnumerator IEnumerable.GetEnumerator()
{
    return ((IEnumerable<KeyValuePair<string, string>>)this).GetEnumerator();
}
```

## Additional Edge Cases Fixed

- **Generic vs non-generic methods**: Generic methods (`Process<T>(T)`) no longer incorrectly delegate to non-generic methods (`Process(string)`)
- **Independent overloads**: Methods with same name but different specific types (`Process(string)` vs `Process(int)`) are correctly identified as independent overloads
- **Void/non-void mismatches**: Methods like `ISet<T>.Add(T) -> bool` no longer delegate to `ICollection<T>.Add(T) -> void`

## Implementation Details

### Key Functions Added/Modified in KnockOffGenerator.cs

1. **`FindPropertyDelegationTarget()`** - For inline stubs, finds typed property counterpart
2. **`GenerateInlineStubPropertyDelegationImplementation()`** - Generates inline stub property delegation
3. **`FindPropertyDelegationTargetInInterfaces()`** - For standalone stubs, searches all interfaces
4. **`FindDelegationTargetInInterfaces()`** - For standalone stubs, searches all interfaces for method delegation
5. **`GenerateFlatPropertyDelegationImplementation()`** - Generates standalone stub property delegation
6. **`GenerateFlatMethodDelegationImplementation()`** - Generates standalone stub method delegation

### Delegation Direction Rules

- Only delegate from base (less specific) to derived (more specific)
- `object` type -> specific type (delegation valid)
- Non-generic return -> generic return (delegation valid)
- Generic -> non-generic (no delegation)
- Both specific but different (no delegation - independent overloads)
- Void/non-void mismatch (no delegation)

## Task List

- [x] Fix Pattern 1: Property type delegation
  - [x] Add `FindPropertyDelegationTarget()` helper
  - [x] Add `GenerateInlineStubPropertyDelegationImplementation()`
  - [x] Modify `GenerateInlineStubClass()` to check properties for delegation
  - [x] Add `FindPropertyDelegationTargetInInterfaces()` for standalone stubs
  - [x] Add `GenerateFlatPropertyDelegationImplementation()` for standalone stubs
  - [x] Test with IProperty<string> stubs

- [x] Fix Pattern 2: Return type delegation
  - [x] Extend `FindDelegationTargetInInterfaces()` to check return types
  - [x] Add `GenerateFlatMethodDelegationImplementation()`
  - [x] Handle edge cases (generic methods, independent overloads, void mismatches)
  - [x] Test with IEnumerable<T> stubs

- [x] Verify all KnockOffTests compile
- [x] Run full KnockOffTests test suite (405-406 tests passing)
- [x] Verify NeatooInterfaceTests generated code compiles (test code has unrelated errors)
