# Remove Count Properties from Public API

**Status:** In Progress
**Priority:** High
**Created:** 2026-01-22
**Last Updated:** 2026-01-22

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
- [ ] Change count properties from `internal` to `private` fields in MethodInterceptorRenderer.cs
- [ ] Change count properties from `internal` to `private` fields in FlatRenderer.cs
- [ ] Change count properties from `internal` to `private` fields in InlineRenderer.cs
- [ ] Change count properties from `internal` to `private` fields in ClassRenderer.cs
- [ ] Ensure `Verify()` methods still work with private count tracking
- [ ] Update KnockOffTests to use Verify API instead of count properties
- [ ] Update KnockOff.NeatooInterfaceTests to use Verify API
- [ ] Update KnockOff.Documentation.Samples to use Verify API
- [ ] Update PackageTest to use Verify API
- [ ] Update KnockOffSandbox to use Verify API
- [ ] Regenerate all stub files
- [ ] Run all tests to verify nothing breaks

---

## Progress Log

**2026-01-22**: Completed architectural analysis. Identified all renderer locations emitting count properties:
- `MethodInterceptorRenderer.cs`: Method interceptor CallCount (aggregate and per-tracking)
- `FlatRenderer.cs`: Property, indexer, event count properties
- `InlineRenderer.cs`: Property, indexer, event, delegate, generic method count properties
- `ClassRenderer.cs`: Property, indexer, method, event count properties

Identified ~65 test usages across 5 test projects that need migration. Created comprehensive plan with implementation steps and migration patterns.

---

## Results / Conclusions
