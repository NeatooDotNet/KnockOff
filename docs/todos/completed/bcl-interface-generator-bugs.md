# BCL Interface Generator Bugs

Bugs discovered during comprehensive BCL interface testing (2025-01-09).
**Updated: 2026-01-09** - ALL BUGS FIXED

## Task List

- [x] Fix Bug 1 - deduplicate interceptor classes/properties for inherited interface members
- [x] Fix Bug 2 (CS0108) - add `new` keyword for Equals/GetHashCode/ToString interceptors
- [x] Fix Bug 3 (CS8769) - handle asymmetric nullability in properties (getter vs setter)
- [x] Fix Bug 4 - standalone stubs need to walk inherited interfaces

---

## Bug 1: IEnumerator<T> Duplicate Interceptor - FIXED

**Symptom:** Build error - duplicate `IEnumerator_CurrentInterceptor` class generated.

**Cause:** `IEnumerator<T>` inherits from `IEnumerator`, both have a `Current` property. Generator creates interceptors for both without deduplication.

**Reproduction:**
```csharp
[KnockOff<IEnumerator<string>>]
public partial class EnumeratorStub { }
// Error: Type 'IEnumerator_CurrentInterceptor' already defines a member called...
```

### Solution (Implemented 2026-01-09)

The issue was in inline stub generation - it iterated through ALL members (including inherited) without deduplication.

**Fix location:** `GenerateInlineStubs` in `KnockOffGenerator.cs`

**Changes made:**
1. Added deduplication by member name before generating handler classes
2. Pass deduplicated members to `GenerateInlineStubClass` for interceptor properties
3. Keep ALL members when generating explicit interface implementations (these need both `IEnumerator<T>.Current` AND `IEnumerator.Current`)

```csharp
// Deduplicate property/indexer members by name for interceptor class generation
// (Keep first occurrence, which is from the most-derived interface)
var processedPropertyNames = new HashSet<string>();
var deduplicatedPropertyMembers = new List<InterfaceMemberInfo>();
foreach (var member in iface.Members)
{
    if (member.IsProperty || member.IsIndexer)
    {
        if (processedPropertyNames.Add(member.Name))
        {
            deduplicatedPropertyMembers.Add(member);
        }
    }
}
```

**Regression test:** `IEnumerator<string>`, `IEnumerator<int>`, `IEnumerator` now compile and work in `BclInterfaceStubs.cs`

---

## Bug 2: CS0108 - Methods Hiding Inherited Members - FIXED

**Symptom:** Build error CS0108 treated as error due to `TreatWarningsAsErrors`.

**Affected Interfaces:**
| Interface | Member | Hides |
|-----------|--------|-------|
| `IEquatable<T>` | `Equals(T)` | `object.Equals(object)` |
| `IStructuralEquatable` | `Equals(object, IEqualityComparer)` | `object.Equals(object)` |
| `IStructuralEquatable` | `GetHashCode(IEqualityComparer)` | `object.GetHashCode()` |
| `IFormattable` | `ToString(string, IFormatProvider)` | `object.ToString()` |
| `IEqualityComparer` | `Equals(object, object)` | `object.Equals(object)` |
| `IEqualityComparer` | `GetHashCode(object)` | `object.GetHashCode()` |
| `IConvertible` | `ToString(IFormatProvider)` | `object.ToString()` |

**Reproduction:**
```csharp
[KnockOff<IEquatable<string>>]
public partial class EquatableStub { }
// Warning CS0108: 'EquatableStub.Equals' hides inherited member 'object.Equals(object)'
```

### Solution (Implemented 2026-01-09)

Added a helper method to detect interceptor names that hide `object` members and emit `new` keyword.

**Fix location:** `KnockOffGenerator.cs` - added helper and updated all interceptor property generation sites

**Changes made:**
```csharp
#region CS0108 Hiding Prevention

/// <summary>
/// Names of members inherited from object that interceptor properties could hide.
/// </summary>
private static readonly HashSet<string> ObjectMemberNames = new(StringComparer.Ordinal)
{
    "Equals",
    "GetHashCode",
    "ToString",
    "GetType"
};

/// <summary>
/// Returns "new " if the interceptor name would hide an inherited object member.
/// </summary>
private static string GetNewKeywordIfNeeded(string interceptorName) =>
    ObjectMemberNames.Contains(interceptorName) ? "new " : "";

#endregion
```

Updated 15+ locations where interceptor properties are generated to use `GetNewKeywordIfNeeded()`.

**Regression test:** `IEquatable<T>`, `IStructuralEquatable`, `IFormattable`, `IEqualityComparer`, `IConvertible` now compile and work in `BclInterfaceStubs.cs`

---

## Bug 3: CS8769 - Nullability Mismatch - FIXED

**Symptom:** Build error CS8769 - nullability of reference types in explicit interface specifier doesn't match.

**Affected Interfaces:**
| Interface | Property | Issue |
|-----------|----------|-------|
| `IDbConnection` | `ConnectionString` | Setter has `[AllowNull]` allowing nullable |
| `IDbCommand` | `CommandText` | Setter has `[AllowNull]` allowing nullable |

**Reproduction:**
```csharp
[KnockOff<IDbConnection>]
public partial class DbConnectionStub { }
// Error CS8769: Nullability mismatch in ConnectionString setter
```

### Analysis (Verified 2026-01-09)

Investigation revealed the actual issue was **opposite** to what was expected:
- `IDbConnection.ConnectionString` getter returns `string` (non-nullable)
- Setter accepts `string?` via `[AllowNull]` attribute on the value parameter

The C# compiler enforces that explicit interface implementations match the nullability attributes exactly, but `[param: AllowNull]` on a setter in explicit implementation doesn't satisfy this requirement.

### Solution (Implemented 2026-01-09)

Since C# doesn't properly propagate `[AllowNull]`/`[DisallowNull]` via `[param:]` syntax on explicit interface implementations, the solution is to suppress the CS8769 warning for these specific properties:

**Fix location:** `KnockOffGenerator.cs`

**Changes made:**
1. Added `SetterHasAllowNull` and `SetterHasDisallowNull` fields to `InterfaceMemberInfo`
2. `CreatePropertyInfo` detects these attributes on setter parameters
3. Property generation emits `#pragma warning disable CS8769` before the setter and `#pragma warning restore CS8769` after

```csharp
// Generated code for IDbConnection.ConnectionString:
string global::System.Data.IDbConnection.ConnectionString
{
    get { ... }
#pragma warning disable CS8769
    set { ... }
#pragma warning restore CS8769
}
```

**Regression test:** `IDbConnection`, `IDbCommand` now compile and work in `BclInterfaceStubs.cs`

---

## Bug 4: Standalone Stubs Don't Flatten Inherited Interfaces - FIXED

**Symptom:** Standalone stubs for interfaces with inheritance chains fail to compile - missing interface implementations.

**Affected Pattern:** Any interface with base interfaces when using standalone `[KnockOff]`:
- `IList<T>` (inherits ICollection<T>, IEnumerable<T>, IEnumerable)
- `ICollection<T>` (inherits IEnumerable<T>, IEnumerable)
- `IDictionary<K,V>` (inherits ICollection<KeyValuePair<K,V>>, IEnumerable<...>, IEnumerable)
- `ISet<T>` (inherits ICollection<T>, IEnumerable<T>, IEnumerable)
- `IEnumerable<T>` (inherits IEnumerable)

**Reproduction:**
```csharp
[KnockOff]
public partial class ListStub : IList<string> { }
// Error: 'ListStub' does not implement interface member 'ICollection<string>.Add(string)'
```

**Workaround:** Use inline stubs instead:
```csharp
[KnockOff<IList<string>>]
public partial class ListStubTests { }
// Works - inline stubs flatten the inheritance chain
```

### Solution (Implemented 2026-01-09)

The fix required two changes:

**1. TransformClass - Walk inherited interfaces**

Added nested loop in `TransformClass` to collect members from inherited interfaces:

```csharp
foreach (var iface in allInterfaces)
{
    // ... collect direct members ...

    // Also get inherited interface members (Bug 4 fix)
    foreach (var baseInterface in iface.AllInterfaces)
    {
        var baseIfaceFullName = baseInterface.ToDisplayString();
        foreach (var member in baseInterface.GetMembers())
        {
            // ... add to members list with correct DeclaringInterfaceFullName ...
        }
    }
}
```

**2. GenerateKnockOff - Generate all explicit implementations**

Changed explicit interface implementation generation to iterate over ALL interfaces and members (not FlatMembers) to ensure both `IEnumerable<T>.GetEnumerator()` AND `IEnumerable.GetEnumerator()` are generated:

```csharp
// Step 7: Generate explicit interface implementations for ALL members from ALL interfaces
var generatedImplementations = new HashSet<string>();
foreach (var iface in typeInfo.Interfaces)
{
    foreach (var member in iface.Members)
    {
        var implKey = $"{member.DeclaringInterfaceFullName}.{member.Name}(...)";
        if (!generatedImplementations.Add(implKey))
            continue; // Skip duplicates

        // Generate implementation...
    }
}
```

**Regression tests:** `IEnumerable<string>`, `ICollection<string>`, `IList<string>`, `IDictionary<string, int>`, `ISet<string>`, `IReadOnlyList<string>` now compile and work in `BclStandaloneStubs.cs`

---

## Test Files

These bugs were discovered while creating:
- `src/Tests/KnockOffTests/BclInterfaceStubs.cs` - problematic interfaces commented out with explanations
- `src/Tests/KnockOffTests/BclStandaloneStubs.cs` - collection interfaces removed due to Bug 4

---

## Summary (2026-01-09)

| Bug | Status | Resolution |
|-----|--------|------------|
| Bug 1 (duplicate interceptors) | ✅ FIXED | Deduplicate members in inline stub generation |
| Bug 2 (CS0108 hiding) | ✅ FIXED | Add `new` keyword for object member names |
| Bug 3 (nullability) | ✅ FIXED | Suppress CS8769 with pragma for asymmetric nullability |
| Bug 4 (standalone inheritance) | ✅ FIXED | Walk inherited interfaces + generate all explicit implementations |

---

## Regression Tests Added

**Bug 1:** `IEnumerator`, `IEnumerator<string>`, `IEnumerator<int>` in BclInterfaceStubs.cs
**Bug 2:** `IEquatable<T>`, `IStructuralEquatable`, `IFormattable`, `IEqualityComparer`, `IConvertible` in BclInterfaceStubs.cs
**Bug 4:** `IEnumerable<string>`, `ICollection<string>`, `IList<string>`, `IDictionary<string, int>`, `ISet<string>`, `IReadOnlyList<string>` in BclStandaloneStubs.cs + tests in BclStandaloneTests.cs

---

## Remaining Work

None - all bugs fixed!
