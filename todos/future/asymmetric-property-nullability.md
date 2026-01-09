# Asymmetric Property Nullability (CS8769)

## Problem

Some .NET interfaces have properties where the getter and setter have different nullability:

```csharp
// IDbConnection.ConnectionString definition:
string? ConnectionString { [return: MaybeNull] get; [param: DisallowNull] set; }
```

- **Getter** returns `string?` (nullable)
- **Setter** expects `string` (non-null, enforced by `[DisallowNull]`)

The generator currently captures a single type for the whole property. When it uses `string?` for both accessors, the setter violates the interface contract:

```
error CS8769: Nullability of reference types in type of parameter doesn't match
implemented member 'IDbConnection.ConnectionString'
```

## Affected Interfaces

| Interface | Property | Issue |
|-----------|----------|-------|
| `IDbConnection` | `ConnectionString` | Setter requires non-null |
| `IDbCommand` | `CommandText` | Setter requires non-null |

## Root Cause

`CreatePropertyInfo` in the generator captures one type string for the entire property:

```csharp
var propertyInfo = new InterfaceMemberInfo
{
    Type = property.Type.ToDisplayString(...),  // Single type for whole property
    // ...
};
```

For asymmetric properties, we need separate type information for:
1. Getter return type (`string?`)
2. Setter parameter type (`string`)

## Proposed Fix

### Option A: Separate Getter/Setter Types

Add separate fields to `InterfaceMemberInfo`:

```csharp
public record InterfaceMemberInfo
{
    // Existing
    public string Type { get; init; }

    // New - only populated when getter/setter nullability differs
    public string? GetterType { get; init; }
    public string? SetterType { get; init; }
}
```

In `CreatePropertyInfo`:

```csharp
string? getterType = null;
string? setterType = null;

if (property.GetMethod is { } getter && property.SetMethod is { } setter)
{
    var getterReturnType = getter.ReturnType.ToDisplayString(...);
    var setterParamType = setter.Parameters[0].Type.ToDisplayString(...);

    if (getterReturnType != setterParamType)
    {
        getterType = getterReturnType;
        setterType = setterParamType;
    }
}
```

In property generation, use `GetterType ?? Type` for getter and `SetterType ?? Type` for setter.

### Option B: Emit Diagnostic and Skip

For simplicity, detect asymmetric nullability and emit a diagnostic:

```csharp
// KO0012: Property '{0}.{1}' has asymmetric nullability. Use a user method override.
```

This documents the limitation and suggests the workaround.

## Workaround (Current)

Use inline stubs with explicit user method for these specific properties:

```csharp
[KnockOff<IDbConnection>]
public partial class DbConnectionStub
{
    private string _connectionString = "";

    protected string? IDbConnection_ConnectionString_Get() => _connectionString;
    protected void IDbConnection_ConnectionString_Set(string value) => _connectionString = value;
}
```

## Complexity

**Option A:** Medium - requires changes to `InterfaceMemberInfo` model and property generation code paths.

**Option B:** Low - just add detection and diagnostic.

## Test Cases

Once fixed, these stubs should compile without error:

```csharp
[KnockOff<IDbConnection>]
public partial class DbConnectionStubTests { }

[KnockOff<IDbCommand>]
public partial class DbCommandStubTests { }
```

## References

- `src/Tests/KnockOffTests/BclInterfaceStubs.cs:365-373` - Commented out stubs
- `todos/bcl-interface-generator-bugs.md` - Original bug tracking
