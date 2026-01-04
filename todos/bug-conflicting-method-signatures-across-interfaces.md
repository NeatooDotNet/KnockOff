# Bug: Conflicting Method Signatures Across Multiple Interfaces

**Status:** ✅ FIXED
**Severity:** Medium
**Discovered:** 2026-01-03
**Fixed:** 2026-01-03

## Summary

When a KnockOff stub implements multiple interfaces that have methods with the **same name but different parameter types**, the generator used to produce invalid code.

## Fix Applied

**Interface-Scoped Spy Handlers** - Each interface now gets its own spy class with its own handlers:

```csharp
// Old (broken): knockOff.Spy.Process  -- single handler for all interfaces
// New (fixed): knockOff.IMiStringProcessor.Process  -- per-interface handlers
//              knockOff.IMiIntProcessor.Process
```

### Breaking Change

This fix changes the public API:
- `Spy.Method` → `IInterface.Method`
- The `Spy` property is removed entirely
- Each interface has its own spy property (e.g., `knockOff.IEmailService`)

### Implementation Details

1. Removed flat `Spy` class
2. Generate `IInterfaceSpy` class per interface
3. Each interface spy contains handlers only for that interface's members
4. Backing fields are now prefixed with interface name (e.g., `ILogger_NameBacking`)
5. Collision detection: If interface has member named same as interface, spy property gets `_` suffix

## Example (Now Works)

```csharp
public interface IDataProvider
{
    string GetData(int id);
}

public interface IKeyLookup
{
    int GetData(string key);  // Same name, different signature
}

[KnockOff]
public partial class DataKnockOff : IDataProvider, IKeyLookup { }

// Usage:
knockOff.IDataProvider.GetData.OnCall((ko, id) => $"Data-{id}");
knockOff.IKeyLookup.GetData.OnCall((ko, key) => key.Length);

// Each interface tracked separately
Assert.Equal(1, knockOff.IDataProvider.GetData.CallCount);
Assert.Equal(2, knockOff.IKeyLookup.GetData.CallCount);
```

## Test Coverage

- `ConflictingSignatureTests.cs` - Tests for this specific scenario
- `SpyPropertyCollisionTests.cs` - Tests for naming collision edge case
