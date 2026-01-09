# Standalone Stubs: Flatten Inherited Interfaces (Bug 4)

## Problem

Standalone stubs using `[KnockOff]` on a class don't generate implementations for inherited interface members:

```csharp
[KnockOff]
public partial class ListStub : IList<string> { }
// Error CS0535: 'ListStub' does not implement interface member 'ICollection<string>.Add(string)'
```

`IList<string>` inherits from `ICollection<string>`, `IEnumerable<string>`, and `IEnumerable`. The generator only implements `IList<string>` members, not the inherited ones.

## Affected Interfaces

Any interface with base interfaces:

| Interface | Inherited From |
|-----------|----------------|
| `IList<T>` | `ICollection<T>`, `IEnumerable<T>`, `IEnumerable` |
| `ICollection<T>` | `IEnumerable<T>`, `IEnumerable` |
| `IDictionary<K,V>` | `ICollection<KeyValuePair<K,V>>`, `IEnumerable<...>`, `IEnumerable` |
| `ISet<T>` | `ICollection<T>`, `IEnumerable<T>`, `IEnumerable` |
| `IEnumerable<T>` | `IEnumerable` |
| `IEnumerator<T>` | `IEnumerator`, `IDisposable` |
| `IReadOnlyList<T>` | `IReadOnlyCollection<T>`, `IEnumerable<T>`, `IEnumerable` |
| `IAsyncEnumerable<T>` | (none, but `IAsyncEnumerator<T>` inherits `IAsyncDisposable`) |

## Current Workaround

Use inline stubs instead of standalone stubs:

```csharp
// Doesn't work - standalone stub
[KnockOff]
public partial class ListStub : IList<string> { }

// Works - inline stub
[KnockOff<IList<string>>]
public partial class ListStubTests { }
```

## Root Cause Analysis

### Inline Stubs (Working)

`ExtractInterfaceInfo` (line 467) walks inherited interfaces:

```csharp
// Gets direct members
foreach (var member in iface.GetMembers()) { ... }

// ALSO gets inherited members
foreach (var baseInterface in iface.AllInterfaces)
{
    foreach (var member in baseInterface.GetMembers()) { ... }
}
```

### Standalone Stubs (Broken)

`TransformClass` (line 907) appears to walk `AllInterfaces`:

```csharp
var allInterfaces = classSymbol.AllInterfaces;  // line 958
foreach (var iface in allInterfaces)            // line 965
{
    foreach (var member in iface.GetMembers())  // line 971
    { ... }
}
```

**However**, there's a critical difference:

1. **Inline stubs**: Start with `IList<T>`, then explicitly walk `IList<T>.AllInterfaces` to get `ICollection<T>`, etc.

2. **Standalone stubs**: Use `classSymbol.AllInterfaces` which returns the interfaces the CLASS implements. For a class that declares `: IList<string>`, this might not include `IList<string>` itself in some Roslyn versions, or there may be issues with how members are collected.

**Suspected issues:**
- `classSymbol.AllInterfaces` behavior vs `interfaceSymbol.AllInterfaces`
- Member collection from interfaces not matching explicit implementation requirements
- Deduplication in `FlattenAndDeduplicateMembers` removing needed members

## Proposed Fix

### Phase 1: Diagnosis

1. Add a standalone stub for `IEnumerable<string>` to the test project
2. Examine the generated file (or compilation errors)
3. Add debug logging to `TransformClass` to see:
   - What interfaces are in `classSymbol.AllInterfaces`
   - What members are collected for each interface
   - What `FlattenAndDeduplicateMembers` returns

### Phase 2: Fix Options

#### Option A: Use Interface's AllInterfaces (Recommended)

Change standalone stub processing to match inline stub behavior:

```csharp
// Current (broken)
var allInterfaces = classSymbol.AllInterfaces;
foreach (var iface in allInterfaces)
{
    foreach (var member in iface.GetMembers()) { ... }
}

// Fixed
var directInterfaces = classSymbol.Interfaces;
foreach (var directIface in directInterfaces)
{
    // Get members from this interface
    foreach (var member in directIface.GetMembers()) { ... }

    // Get members from inherited interfaces
    foreach (var baseInterface in directIface.AllInterfaces)
    {
        foreach (var member in baseInterface.GetMembers()) { ... }
    }
}
```

#### Option B: Reuse ExtractInterfaceInfo

Standalone stubs could call `ExtractInterfaceInfo` for each declared interface:

```csharp
var directInterfaces = classSymbol.Interfaces;
foreach (var iface in directInterfaces)
{
    var interfaceInfo = ExtractInterfaceInfo(iface, knockOffAssembly);
    interfaceInfos.Add(interfaceInfo);
}
```

This reuses proven code but may need adjustment for how interfaces are grouped.

#### Option C: Emit Diagnostic (Minimal)

Detect interfaces with inheritance and emit a diagnostic:

```csharp
// KO0011: Interface '{0}' has base interfaces. Use inline stubs [KnockOff<{0}>] instead.
```

This documents the limitation without fixing it.

### Phase 3: Implementation (Option A or B)

1. Update `TransformClass` to walk inherited interfaces properly
2. Ensure `FlattenAndDeduplicateMembers` preserves all needed members
3. Update `GenerateKnockOff` if interface grouping changes

### Phase 4: Testing

Add standalone stubs for interfaces with inheritance:

```csharp
// In BclStandaloneStubs.cs
[KnockOff]
public partial class EnumerableStringKnockOff : IEnumerable<string> { }

[KnockOff]
public partial class ListStringKnockOff : IList<string> { }

[KnockOff]
public partial class CollectionStringKnockOff : ICollection<string> { }

[KnockOff]
public partial class DictionaryStringIntKnockOff : IDictionary<string, int> { }
```

Add tests verifying:
- All interface members are implemented
- Interceptors work for inherited members
- No duplicate interceptor classes

## Task List

- [x] Reproduce bug with minimal test case
- [x] Add diagnostic logging to understand current behavior
- [x] Determine root cause (AllInterfaces behavior or deduplication)
- [x] Implement fix (Option A, B, or C) - Used combined approach
- [x] Add standalone stubs for collection interfaces to BclStandaloneStubs.cs
- [x] Add tests for inherited interface members in BclStandaloneTests.cs
- [x] Update documentation in BclStandaloneStubs.cs (remove "Bug 4" warning)
- [x] Update todos/bcl-interface-generator-bugs.md to mark Bug 4 as fixed

**COMPLETED: 2026-01-09**

## Phase 5: Feature Parity Testing (Standalone vs Inline)

Once the inherited interfaces bug is fixed, ensure all shared features are thoroughly tested for **both** stub patterns.

### Current Test Coverage Analysis

| Feature | Standalone Tests | Inline Tests | Notes |
|---------|-----------------|--------------|-------|
| Properties (get/set) | `BasicTests.cs` | `InlineStubTests.cs` | Both covered |
| Void methods | `BasicTests.cs` | `InlineStubTests.cs` | Both covered |
| Methods with return values | `BasicTests.cs` | `InlineStubTests.cs` | Both covered |
| CallCount/WasCalled/LastCallArg | `BasicTests.cs` | `InlineStubTests.cs` | Both covered |
| Reset | `BasicTests.cs` | `InlineStubTests.cs` | Both covered |
| OnCall callbacks | `CallbackTests.cs` | `InlineStubTests.cs` | Both covered |
| OnGet/OnSet callbacks | `CallbackTests.cs` | `InlineStubTests.cs` | Both covered |
| Async methods (Task/ValueTask) | `AsyncMethodTests.cs` | **MISSING** | Standalone only |
| Generic interfaces | `GenericInterfaceTests.cs` | **MISSING** | Standalone only |
| Generic methods (Of<T>) | `GenericMethodTests.cs` | `InlineStubTests.cs` | Both covered |
| Events | `EventTests.cs` | **MISSING** | Standalone only |
| Indexers | `IndexerTests.cs` | **MISSING** | Standalone only |
| Ref parameters | `RefParameterTests.cs` | **MISSING** | Standalone only |
| Out parameters | `OutParameterTests.cs` | **MISSING** | Standalone only |
| Interface inheritance | `InterfaceInheritanceTests.cs` | **MISSING** | Standalone only |
| Overloaded methods | `OverloadedMethodTests.cs` | **MISSING** | Standalone only |
| Smart defaults | `SmartDefaultsTests.cs` | **MISSING** | Standalone only |
| Multiple interfaces | N/A | `InlineStubTests.cs` | Inline-specific |
| Delegate stubs | N/A | `InlineStubTests.cs` | Inline-only feature |
| Class stubs | N/A | `InlineStubTests.cs` | Inline-only feature |
| Partial properties | N/A | `InlineStubTests.cs` | Inline-only feature |

### Task List for Feature Parity

- [ ] **Async methods**: Add `InlineAsyncMethodTests.cs` or add inline tests to `AsyncMethodTests.cs`
- [ ] **Generic interfaces**: Add inline tests for `IRepository<T>` pattern
- [ ] **Events**: Add inline tests for event subscription/raising
- [ ] **Indexers**: Add inline tests for indexer get/set tracking
- [ ] **Ref parameters**: Add inline tests for ref parameter callbacks
- [ ] **Out parameters**: Add inline tests for out parameter callbacks
- [ ] **Interface inheritance**: Add inline tests for inherited interface members
- [ ] **Overloaded methods**: Add inline tests for overload disambiguation
- [ ] **Smart defaults**: Add inline tests for Task<T>/property/non-instantiable defaults

### Recommended Test Structure

For each feature, tests should follow this pattern:

```csharp
// Option A: Separate test classes
public class AsyncMethodTests        // Standalone stubs
public class InlineAsyncMethodTests  // Inline stubs

// Option B: Combined test class with regions
public class AsyncMethodTests
{
    #region Standalone Stubs
    // Tests using [KnockOff] on class implementing interface
    #endregion

    #region Inline Stubs
    // Tests using [KnockOff<T>] attribute
    #endregion
}
```

**Recommendation**: Use Option B (combined classes) for features that have identical behavior. This makes it easier to verify parity and catch regressions. Use Option A only for features with significant behavioral differences.

### Test File Templates

#### InlineAsyncMethodTests (example)

```csharp
// In InlineStubTests.cs or separate file

[KnockOff<IAsyncService>]
public partial class InlineAsyncTest { }

public class InlineAsyncMethodTests
{
    [Fact]
    public async Task InlineStub_AsyncMethod_Task_ReturnsCompletedTask()
    {
        var stub = new InlineAsyncTest.Stubs.IAsyncService();
        IAsyncService service = stub;

        await service.DoWorkAsync();

        Assert.True(stub.DoWorkAsync.WasCalled);
    }

    [Fact]
    public async Task InlineStub_AsyncMethod_TaskOfT_OnCallReturnsValue()
    {
        var stub = new InlineAsyncTest.Stubs.IAsyncService();
        stub.GetValueAsync.OnCall = (ko, x) => Task.FromResult(x * 10);
        IAsyncService service = stub;

        var result = await service.GetValueAsync(5);

        Assert.Equal(50, result);
    }
}
```

### Verification Checklist

When adding inline tests for each feature, verify:

1. **API shape**: Interceptor properties exist on `Stubs.IInterface`
2. **Tracking**: CallCount, WasCalled, LastCallArg work identically
3. **Callbacks**: OnCall/OnGet/OnSet delegates have same signature
4. **Reset**: Reset() clears all state
5. **Smart defaults**: Default return values match standalone behavior

## Complexity

**Medium** - Requires understanding Roslyn's interface inheritance model and ensuring the fix doesn't break existing standalone stubs.

**Phase 5 Complexity: Low-Medium** - Mostly mechanical work adding test coverage, but important for ensuring no regressions.

## Files to Modify

- `src/Generator/KnockOffGenerator.cs` - `TransformClass` method
- `src/Tests/KnockOffTests/BclStandaloneStubs.cs` - Add collection interface stubs
- `src/Tests/KnockOffTests/BclStandaloneTests.cs` - Add tests

### Phase 5 Files to Add/Modify

- `src/Tests/KnockOffTests/InlineStubTests.cs` - Add async, event, indexer, ref/out tests
- OR create separate files:
  - `src/Tests/KnockOffTests/InlineAsyncMethodTests.cs`
  - `src/Tests/KnockOffTests/InlineEventTests.cs`
  - `src/Tests/KnockOffTests/InlineIndexerTests.cs`
  - `src/Tests/KnockOffTests/InlineRefParameterTests.cs`
  - `src/Tests/KnockOffTests/InlineOutParameterTests.cs`
  - `src/Tests/KnockOffTests/InlineGenericInterfaceTests.cs`
  - `src/Tests/KnockOffTests/InlineOverloadedMethodTests.cs`
  - `src/Tests/KnockOffTests/InlineSmartDefaultsTests.cs`

## References

- `src/Generator/KnockOffGenerator.cs:467` - `ExtractInterfaceInfo` (working implementation)
- `src/Generator/KnockOffGenerator.cs:907` - `TransformClass` (broken implementation)
- `src/Tests/KnockOffTests/BclStandaloneStubs.cs:95-97` - Bug 4 workaround comment
- `todos/bcl-interface-generator-bugs.md` - Original bug tracking
