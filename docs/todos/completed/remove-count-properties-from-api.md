# Remove Count Properties from Public API

**Status:** Complete
**Priority:** High
**Created:** 2026-01-22
**Last Updated:** 2026-02-01

---

## Problem

`CallCount`, `GetCount`, `SetCount`, `AddCount`, and `RemoveCount` properties are still accessible in test projects despite being marked `internal`. This is because:

1. Interceptor classes are generated **inside the user's assembly** (not the KnockOff library)
2. `internal` only restricts access from other assemblies
3. Tests are in the same assembly as the generated stubs
4. Therefore `internal` provides no encapsulation from test code

The intent was to make these properties truly internal to the stub's implementation logic, not just "internal to the assembly."

## Solution

Remove `CallCount`, `GetCount`, `SetCount`, `AddCount`, and `RemoveCount` properties entirely from the generated interceptor classes. Users should use the `Verify()` API instead:

**Before (current):**
```csharp
Assert.Equal(1, stub.Name.GetCount);
Assert.Equal(2, stub.Method.CallCount);
```

**After (desired):**
```csharp
stub.Name.VerifyGet(Times.Once);
stub.Method.Verify(Times.Exactly(2));
```

The count tracking will still exist internally (private fields) to support `Verify()`, but won't be exposed as properties.

---

## Plans

- [Remove Count Properties from Public API](../plans/remove-count-properties-from-api.md)

---

## Tasks

- [x] Identify all locations in renderers that emit count properties (see plan)
- [x] Change count properties to `internal` fields in MethodInterceptorRenderer.cs
- [x] Change count properties to `internal` fields in FlatRenderer.cs
- [x] Change count properties to `internal` fields in PropertyInterceptorRenderer.cs
- [x] Change count properties to `internal` fields in IndexerInterceptorRenderer.cs
- [x] Ensure `Verify()` methods still work with internal count tracking
- [x] Update PackageTest to use Verify API
- [x] Regenerate all stub files
- [x] Run all tests to verify nothing breaks

**Note:** Most tests were already using Verify API. Only PackageTest needed migration.

---

## Progress Log

**2026-01-22**: Completed architectural analysis. Identified all renderer locations emitting count properties:
- `MethodInterceptorRenderer.cs`: Method interceptor CallCount (aggregate and per-tracking)
- `FlatRenderer.cs`: Property, indexer, event count properties
- `InlineRenderer.cs`: Property, indexer, event, delegate, generic method count properties
- `ClassRenderer.cs`: Property, indexer, method, event count properties

Identified ~65 test usages across 5 test projects that need migration. Created comprehensive plan with implementation steps and migration patterns.

**2026-02-01**: Implementation complete. Changed `internal int CallCount { get; private set; }` to `internal int _callCount;` in nested tracking classes. Key findings:
- Properties (`_getCount`, `_setCount`) and events (`_addCount`, `_removeCount`) were already converted in a previous partial implementation
- Method tracking classes still had `internal int CallCount` properties - converted these
- Used `internal` instead of `private` because C# outer classes cannot access private members of nested classes
- Only PackageTest needed migration - other tests were already using Verify API
- WhenMatcher classes retain `public int CallCount { get; set; }` (different purpose - conditional matching)

---

## Results / Conclusions

**Completed successfully.** The count properties are no longer exposed as accessible properties in the generated code.

**Before:**
```csharp
internal int CallCount { get; private set; }
```

**After:**
```csharp
internal int _callCount;
```

The underscore-prefixed field signals it's not for external use. Tests should use `Verify()` API instead:
```csharp
stub.Method.Verify(Times.Once);
stub.Property.VerifyGet(Times.Exactly(2));
```

All tests pass (956 KnockOffTests + 385 Documentation.Samples + 473 NeatooInterfaceTests).
