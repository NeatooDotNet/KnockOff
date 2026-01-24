# Remove Count Properties from Public API

**Date:** 2026-01-22
**Related Todo:** [Remove Count Properties from Public API](../todos/remove-count-properties-from-api.md)
**Status:** Ready for Implementation
**Last Updated:** 2026-01-22

---

## Overview

Remove `CallCount`, `GetCount`, `SetCount`, `AddCount`, and `RemoveCount` properties from the generated interceptor classes' public/internal API. These properties are currently marked `internal` but remain accessible in test projects because generated code lives in the same assembly as tests. The goal is to hide these implementation details entirely, forcing users to use the `Verify()` API.

---

## Problem Analysis

### Current State

Count properties are exposed as `internal` properties in interceptor classes:

```csharp
// Current - internal but accessible from test code in same assembly
internal int CallCount { get; private set; }
internal int GetCount { get; private set; }
internal int SetCount { get; private set; }
internal int AddCount { get; private set; }
internal int RemoveCount { get; private set; }
```

### Why `internal` Doesn't Work

1. KnockOff generates stub code **inside the user's assembly** (test project)
2. `internal` restricts access from **other assemblies**, not within the same assembly
3. Test code lives in the same assembly as the generated stubs
4. Therefore, `internal` provides no encapsulation from test code

### Desired State

```csharp
// Desired - truly private, only accessible via Verify() API
private int _callCount;
private int _getCount;
private int _setCount;
private int _addCount;
private int _removeCount;

// Public API remains unchanged
public void Verify() => Verify(Times.AtLeastOnce);
public void Verify(Times times) { ... }
public void VerifyGet(Times times) { ... }
public void VerifySet(Times times) { ... }
```

---

## Approach

### Strategy

1. **Change property accessibility**: Convert `internal int XxxCount { get; private set; }` to `private int _xxxCount;`
2. **Update internal references**: All internal code that reads counts must use the private field
3. **Preserve Verify API**: The Verify methods continue to work exactly as before
4. **Update aggregate count calculations**: Internal properties like `CallCount` in method interceptors aggregate from multiple sources - these become private fields with internal computation

### Non-Breaking

- **Verify API unchanged**: `stub.Method.Verify()`, `stub.Method.Verify(Times.Once)` work identically
- **VerifyGet/VerifySet unchanged**: `stub.Property.VerifyGet(Times.Once)` works identically
- **LastCallArg/LastArgs unchanged**: These remain public
- **WasCalled convenience stays**: If present, remains public

### Breaking (Intentional)

- **Count properties removed**: `stub.Method.CallCount` no longer compiles
- **Direct count assertions fail**: `Assert.Equal(1, stub.Method.CallCount)` must change to `stub.Method.Verify(Times.Once)`

---

## Design

### Affected Interceptor Types

| Interceptor Type | Properties to Remove | Renderers Affected |
|-----------------|---------------------|-------------------|
| Method | `CallCount` | `MethodInterceptorRenderer.cs` |
| Property | `GetCount`, `SetCount` | `FlatRenderer.cs`, `InlineRenderer.cs`, `ClassRenderer.cs` |
| Indexer | `GetCount`, `SetCount` | `FlatRenderer.cs`, `InlineRenderer.cs`, `ClassRenderer.cs` |
| Event | `AddCount`, `RemoveCount` | `FlatRenderer.cs`, `InlineRenderer.cs`, `ClassRenderer.cs` |
| Generic Method | `CallCount` (in typed handler) | `FlatRenderer.cs`, `InlineRenderer.cs` |
| Delegate | `CallCount` | `InlineRenderer.cs` |

### Pattern-by-Pattern Analysis

#### Stand-Alone (Flat) Pattern

**File:** `src/Generator/Renderer/FlatRenderer.cs`

Properties interceptors:
- Line 282-286: `internal int GetCount { get; private set; }`
- Line 413-414: `internal int SetCount { get; private set; }`

Indexer interceptors:
- Line 653-654: `internal int GetCount { get; private set; }`
- Line 668-669: `internal int SetCount { get; private set; }`

Event interceptors:
- Line 875-876: `internal int AddCount { get; private set; }`
- Line 879-880: `internal int RemoveCount { get; private set; }`

**File:** `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs`

Method interceptors:
- Line 665: `internal int CallCount { get; private set; }` (in MethodTrackingImpl)
- Line 927: `internal int CallCount { get { ... } }` (aggregate in interceptor class)
- Line 963: `internal int CallCount => ...` (overload groups)

#### Inline Interface Pattern

**File:** `src/Generator/Renderer/InlineRenderer.cs`

Property interceptors:
- Line 276-278: `internal int GetCount { get; private set; }`
- Line 285-287: `internal int SetCount { get; private set; }`

Indexer interceptors:
- Line 448-450: `internal int GetCount { get; private set; }`
- Line 467-469: `internal int SetCount { get; private set; }`

Event interceptors:
- Line 874-876: `internal int AddCount { get; private set; }`
- Line 878-880: `internal int RemoveCount { get; private set; }`

Generic method handlers:
- Line 707: `internal int TotalCallCount => ...`
- Line 777-778: `int IGenericMethodCallTracker.CallCount => _callCount;` / `internal int CallCount => _callCount;`

Delegate interceptors:
- Line 1266: `internal int CallCount { get; private set; }`

#### Inline Class Pattern

**File:** `src/Generator/Renderer/ClassRenderer.cs`

Property interceptors:
- Line 118-119: `internal int GetCount { get; private set; }`
- Line 134-135: `internal int SetCount { get; private set; }`

Indexer interceptors:
- Line 279-280: `internal int GetCount { get; private set; }`
- Line 302-303: `internal int SetCount { get; private set; }`

Method interceptors:
- Line 459-460: `internal int CallCount { get; private set; }`

Event interceptors:
- Line 576-577: `internal int AddCount { get; private set; }`
- Line 579-580: `internal int RemoveCount { get; private set; }`

### Internal Usages That Must Continue Working

The following internal code uses count properties and must be updated to use private fields:

1. **CheckVerification()** - Uses counts to validate against Times constraint
2. **CheckVerificationAll()** - Uses counts to check at-least-once
3. **Verify(Times)** - Uses counts for validation
4. **Aggregate CallCount** - In method interceptors, computes total from multiple sources
5. **LastCallArg/LastCallArgs getters** - Check if count > 0 to determine which value to return
6. **Sequence tracking** - Uses TotalCallCount to check sequence completion

### Generated Code Example

**Before:**
```csharp
public sealed class IService_GetNameInterceptor
{
    internal int CallCount { get; private set; }

    public void Verify(Times times)
    {
        if (!times.Validate(CallCount))
            throw new VerificationException(...);
    }
}
```

**After:**
```csharp
public sealed class IService_GetNameInterceptor
{
    private int _callCount;

    public void Verify(Times times)
    {
        if (!times.Validate(_callCount))
            throw new VerificationException(...);
    }
}
```

### Special Cases

#### Method Interceptors with Aggregate Counts

The current aggregate `CallCount` property in method interceptors computes from multiple sources:
- `_unconfiguredCallCount`
- `_onCallTracking?.CallCount`
- Sequence tracking counts

This becomes a private computed expression used only by Verify methods.

**Before:**
```csharp
internal int CallCount { get {
    var sum = _unconfiguredCallCount + (_onCallTracking?.CallCount ?? 0);
    if (_sequence != null) foreach (var s in _sequence) sum += s.Tracking.CallCount;
    return sum;
} }
```

**After:**
```csharp
private int ComputeTotalCallCount()
{
    var sum = _unconfiguredCallCount + (_onCallTracking?.CallCount ?? 0);
    if (_sequence != null) foreach (var s in _sequence) sum += s.Tracking.CallCount;
    return sum;
}
```

Note: The nested `MethodTrackingImpl.CallCount` is accessed internally by the parent interceptor, so it can remain `internal` or we can add an accessor method.

#### IGenericMethodCallTracker Interface

For generic method handlers, there's a private interface `IGenericMethodCallTracker` with `CallCount` property used for LINQ aggregation:

```csharp
private interface IGenericMethodCallTracker { int CallCount { get; } }
```

This is internal implementation detail that can remain since it's private to the generated code.

---

## Implementation Steps

### Phase 1: Renderer Changes

1. **MethodInterceptorRenderer.cs**
   - Change `internal int CallCount` in MethodTrackingImpl to private field `_callCount`
   - Change aggregate `CallCount` property to private method `ComputeTotalCallCount()`
   - Update all references to use private field/method
   - Update overload group handling similarly

2. **FlatRenderer.cs**
   - Change property `GetCount`/`SetCount` to private fields
   - Change indexer `GetCount`/`SetCount` to private fields
   - Change event `AddCount`/`RemoveCount` to private fields
   - Update Verify methods to use private fields

3. **InlineRenderer.cs**
   - Same changes as FlatRenderer for property, indexer, event interceptors
   - Change delegate interceptor `CallCount` to private field
   - Update generic method handler TotalCallCount to private

4. **ClassRenderer.cs**
   - Same changes for property, indexer, method, event interceptors

### Phase 2: Test Migration

Migrate tests from count assertions to Verify API:

| Project | Files | Estimated Changes |
|---------|-------|-------------------|
| KnockOffTests | BasicTests.cs | 4 usages |
| KnockOff.NeatooInterfaceTests | Multiple files | ~50 usages |
| KnockOff.Documentation.Samples | AdvancedCallbacksSamples.cs | 2 usages |
| KnockOffSandbox | Program.cs | 4 usages |
| PackageTest | Program.cs | 1 usage |

**Migration pattern:**
```csharp
// Before
Assert.Equal(1, stub.Name.SetCount);

// After
stub.Name.VerifySet(Times.Once);
```

**For callback-based assertions:**
```csharp
// Before - callback uses CallCount
stub.IsConnected.OnGet(() => stub.Connect.CallCount > 0);

// After - use WasCalled or track state differently
var connected = false;
stub.Connect.OnCall(_ => { connected = true; return Task.CompletedTask; });
stub.IsConnected.OnGet(() => connected);
```

### Phase 3: Regenerate and Verify

1. Regenerate all stub files in Generated/ directories
2. Run full test suite
3. Verify no compilation errors from removed count properties
4. Verify all tests pass with Verify API

---

## Acceptance Criteria

- [ ] All `CallCount`, `GetCount`, `SetCount`, `AddCount`, `RemoveCount` properties removed from public/internal API
- [ ] Count tracking continues to work internally (private fields)
- [ ] `Verify()` and `Verify(Times)` methods work correctly
- [ ] `VerifyGet(Times)` and `VerifySet(Times)` work correctly
- [ ] `VerifyAdd(Times)` and `VerifyRemove(Times)` work correctly for events
- [ ] All three patterns (Stand-Alone, Inline Interface, Inline Class) updated
- [ ] All tests migrated to use Verify API
- [ ] All tests pass
- [ ] Generated files regenerated and committed

---

## Dependencies

- No external dependencies
- No changes to KnockOff library APIs (only generator output changes)

---

## Risks / Considerations

### Breaking Change

This is an intentional breaking change. Users currently using count properties will get compilation errors.

**Mitigation:**
- This is pre-1.0 software where breaking changes are expected
- The Verify API is more expressive and provides better error messages
- Migration is straightforward (count assertions -> Verify calls)

### Callback-Based Count Access

Some tests use count properties inside callbacks to determine behavior:
```csharp
stub.IsConnected.OnGet = () => stub.Connect.CallCount > 0;
```

**Mitigation:**
- Use local state tracking instead
- Document the pattern change in migration guidance

### Internal Implementation Complexity

Method interceptors have complex aggregate count calculations spanning multiple tracking objects.

**Mitigation:**
- Keep the computation logic, just make it private
- Add private method for aggregate calculation
- Ensure nested tracking classes can still report counts to parent

---

## Architectural Verification

### All Three Patterns Analyzed

- [x] **Stand-Alone/Flat**: Analyzed in `FlatRenderer.cs` - all interceptor types covered
- [x] **Inline Interface**: Analyzed in `InlineRenderer.cs` - all interceptor types covered
- [x] **Inline Class**: Analyzed in `ClassRenderer.cs` - all interceptor types covered

### Breaking Changes Assessment

| Change | Impact | Migration |
|--------|--------|-----------|
| Remove `CallCount` | High - commonly used | Use `Verify(Times.Exactly(n))` |
| Remove `GetCount` | Medium | Use `VerifyGet(Times.Exactly(n))` |
| Remove `SetCount` | Medium | Use `VerifySet(Times.Exactly(n))` |
| Remove `AddCount` | Low - rarely used directly | Use `VerifyAdd(Times.Exactly(n))` |
| Remove `RemoveCount` | Low - rarely used directly | Use `VerifyRemove(Times.Exactly(n))` |

### Pattern Consistency Verified

All three patterns will be updated consistently:
- Same private field naming (`_callCount`, `_getCount`, etc.)
- Same Verify method behavior
- Same internal computation patterns

### Diagnostic Requirements

No new diagnostics needed - this is a generated code change, not a compile-time check.

### Test Strategy Defined

1. Update all existing tests using count properties to use Verify API
2. Verify that Verify API correctly reports failures
3. Verify that Verify API passes for correct counts
4. Test edge cases (zero calls, multiple calls, sequence exhaustion)

### Edge Cases Documented

1. **Zero calls**: `Verify(Times.Never)` should pass, `Verify(Times.Once)` should fail
2. **Callbacks accessing counts**: Must be refactored to use local state
3. **Aggregate counts**: Method interceptors with overloads must correctly sum across all overloads
4. **Sequence tracking**: Sequence completion verification continues to work

### Codebase Deep-Dive Completed

**Files Examined:**

Renderers:
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` (986 lines) - Contains method interceptor generation
- `src/Generator/Renderer/FlatRenderer.cs` (partial, ~700 lines examined) - Stand-alone stub generation
- `src/Generator/Renderer/InlineRenderer.cs` (1462 lines) - Inline interface/delegate stub generation
- `src/Generator/Renderer/ClassRenderer.cs` (1086 lines) - Inline class stub generation

Tests using count properties (from grep):
- `KnockOffTests/BasicTests.cs` - 4 usages
- `KnockOff.NeatooInterfaceTests/Collections/*.cs` - ~15 usages
- `KnockOff.NeatooInterfaceTests/PropertyManagers/*.cs` - ~12 usages
- `KnockOff.NeatooInterfaceTests/ValidationRules/*.cs` - ~10 usages
- `KnockOff.NeatooInterfaceTests/Properties/*.cs` - ~8 usages
- `KnockOff.NeatooInterfaceTests/Notifications/*.cs` - ~8 usages
- `KnockOff.NeatooInterfaceTests/MetaProperties/*.cs` - ~5 usages
- `KnockOff.Documentation.Samples/AdvancedCallbacksSamples.cs` - 2 usages
- `KnockOffSandbox/Program.cs` - 4 usages
- `PackageTest/Program.cs` - 1 usage

---

## Developer Review

**Reviewed By:** knockoff-developer
**Review Date:** 2026-01-22
**Review Status:** APPROVED - No concerns found

### Verification of Architectural Analysis

I have reviewed the architectural plan against the actual codebase and verified:

1. **Line number accuracy**: All referenced line numbers in the renderers are accurate
2. **Pattern coverage**: All three patterns (Stand-Alone, Inline Interface, Inline Class) are properly analyzed
3. **Internal usages**: The plan correctly identifies all internal usages of count properties:
   - `CheckVerification()` - uses counts for Times validation
   - `CheckVerificationAll()` - uses counts for at-least-once validation
   - `Verify(Times)` methods - uses counts for validation
   - Aggregate `CallCount` computation in method interceptors
   - `RecordCall()` methods - increment counts (will use private fields)
   - Sequence tracking - uses counts for completion verification

4. **Test migration scope**: Confirmed ~65 test usages across the identified projects

### Additional Observations

1. **IGenericMethodCallTracker interface** (InlineRenderer.cs line 61, FlatRenderer.cs lines 228-231):
   - This is a PRIVATE interface used internally for LINQ aggregation
   - The `CallCount` property in this interface is NOT part of the public API
   - It is accessed only by generated code within the same class
   - **Decision**: Keep this interface unchanged - it's internal implementation detail

2. **TotalCallCount in sequence/generic handlers**:
   - Properties like `TotalCallCount` are marked `internal` and used for aggregate computation
   - These should also become private for consistency
   - **Decision**: Convert to private fields/methods

3. **MethodTrackingImpl.CallCount** (MethodInterceptorRenderer.cs line 665):
   - This is accessed by the parent interceptor for aggregate computation
   - Since both are in the same generated class, making it private still works
   - **Decision**: Convert to private field

---

## Implementation Contract

### Pre-Implementation Checklist

- [x] Architectural verification checklist is complete
- [x] All three patterns (Stand-Alone, Inline Interface, Inline Class) are addressed
- [x] Breaking changes are assessed (intentional breaking change)
- [x] Test strategy is defined
- [x] No gaps or missing considerations
- [x] Design is implementable without major architectural changes

### Phase 1: MethodInterceptorRenderer.cs Changes

**File:** `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs`

- [ ] **Line 665**: Change `internal int CallCount { get; private set; }` to `private int _callCount;`
- [ ] **Line 689**: Update `RecordCall()` to use `_callCount++` instead of `CallCount++`
- [ ] **Line 694**: Update `RecordCall(...)` to use `_callCount++`
- [ ] **Line 698**: Update `RecordCall(...)` to use `_callCount++`
- [ ] **Line 705**: Update `Reset()` to use `_callCount = 0`
- [ ] **Line 707**: Update `Reset()` to use `_callCount = 0`
- [ ] **Line 709**: Update `Reset()` to use `_callCount = 0`
- [ ] **Line 722**: Update `Verify(Times)` to use `_callCount`
- [ ] **Line 812**: Update sequence `TotalCallCount` to private field
- [ ] **Line 927**: Change aggregate `CallCount` property to private method `GetTotalCallCount()`
- [ ] **Line 963**: Update overload group `CallCount` to private method
- [ ] Update `RenderInterceptorVerifyMethods()` (line 976-982) to use private method
- [ ] Update `RenderInternalVerificationMembers()` (line 559, 568, 596-597, 613) to use private method/field
- [ ] Update `RenderBackwardCompatibleTrackingProperties()` to remove `CallCount` property (it's aggregate)

**Checkpoint**: Run `dotnet build src/KnockOff.sln` - should fail (expected - tests use removed properties)

### Phase 2: FlatRenderer.cs Changes

**File:** `src/Generator/Renderer/FlatRenderer.cs`

Property interceptors:
- [ ] **Line 282**: Change `internal int GetCount { get; private set; }` to `private int _getCount;`
- [ ] **Line 294**: Update `RecordGet()` to use `_getCount++`
- [ ] **Line 401-402**: Same changes in regular property interceptor content
- [ ] **Line 439**: Update `RecordGet()` to use `_getCount++`
- [ ] **Line 413-414**: Change `internal int SetCount { get; private set; }` to `private int _setCount;`
- [ ] **Line 446**: Update `RecordSet()` to use `_setCount++`
- [ ] Update all `Verify*` methods to use private fields
- [ ] Update `CheckVerification()` and `CheckVerificationAll()` to use private fields
- [ ] Update `Reset()` methods to use private fields

Indexer interceptors:
- [ ] **Line 653-654**: Change `GetCount` to private field
- [ ] **Line 668-669**: Change `SetCount` to private field
- [ ] Update `RecordGet()`, `RecordSet()`, `Reset()`, `Verify*` methods

Event interceptors:
- [ ] **Line 875-876**: Change `AddCount` to private field
- [ ] **Line 879-880**: Change `RemoveCount` to private field
- [ ] Update `RecordAdd()`, `RecordRemove()`, `Reset()`, `Verify*` methods

**Checkpoint**: Run `dotnet build src/KnockOff.sln` - should fail (expected)

### Phase 3: InlineRenderer.cs Changes

**File:** `src/Generator/Renderer/InlineRenderer.cs`

Property interceptors:
- [ ] **Line 276-278**: Change `GetCount` to private field
- [ ] **Line 285-287**: Change `SetCount` to private field
- [ ] Update all related methods

Indexer interceptors:
- [ ] **Line 448-450**: Change `GetCount` to private field
- [ ] **Line 467-469**: Change `SetCount` to private field
- [ ] Update all related methods

Event interceptors:
- [ ] **Line 874-876**: Change `AddCount` to private field
- [ ] **Line 878-880**: Change `RemoveCount` to private field
- [ ] Update all related methods

Generic method handlers:
- [ ] **Line 707**: Change `TotalCallCount` to private
- [ ] **Line 777-778**: Keep `IGenericMethodCallTracker.CallCount` (private interface) but change public `CallCount` property to private field

Delegate interceptors:
- [ ] **Line 1266**: Change `CallCount` to private field
- [ ] Update all related methods

**Checkpoint**: Run `dotnet build src/KnockOff.sln` - should fail (expected)

### Phase 4: ClassRenderer.cs Changes

**File:** `src/Generator/Renderer/ClassRenderer.cs`

Property interceptors:
- [ ] **Line 118-119**: Change `GetCount` to private field
- [ ] **Line 134-135**: Change `SetCount` to private field
- [ ] Update all related methods

Indexer interceptors:
- [ ] **Line 279-280**: Change `GetCount` to private field
- [ ] **Line 302-303**: Change `SetCount` to private field
- [ ] Update all related methods

Method interceptors:
- [ ] **Line 459-460**: Change `CallCount` to private field
- [ ] Update all related methods

Event interceptors:
- [ ] **Line 576-577**: Change `AddCount` to private field
- [ ] **Line 579-580**: Change `RemoveCount` to private field
- [ ] Update all related methods

**Checkpoint**: Run `dotnet build src/KnockOff.sln` - should fail (expected - tests still use old API)

### Phase 5: Test Migration - KnockOffTests

**File changes:**

- [ ] `BasicTests.cs`: 4 usages -> convert to `Verify*` API
- [ ] `CallbackTests.cs`: 3 usages -> convert to `Verify*` API
- [ ] `EventTests.cs`: 9 usages -> convert to `Verify*` API
- [ ] `BclInterfaceTests.cs`: ~30 usages -> convert to `Verify*` API
- [ ] `BclStandaloneTests.cs`: ~6 usages -> convert to `Verify*` API
- [ ] `ClassIndexerVerificationTests.cs`: 4 usages -> convert to `Verify*` API

**Checkpoint**: Run `dotnet test src/Tests/KnockOffTests` - should pass

### Phase 6: Test Migration - KnockOff.NeatooInterfaceTests

- [ ] `Collections/IValidateListBaseTests.cs`: ~8 usages
- [ ] `Collections/IEntityListBaseTests.cs`: ~6 usages
- [ ] `ValidationRules/ITriggerPropertyTests.cs`: ~7 usages
- [ ] `ValidationRules/IRuleTests.cs`: ~6 usages
- [ ] `ValidationRules/IRuleOfTTests.cs`: 1 usage
- [ ] `ValidationRules/IRuleMessageTests.cs`: 2 usages
- [ ] `ValidationRules/IRuleMessagesTests.cs`: 1 usage
- [ ] `ValidationRules/IRuleManagerTests.cs`: ~6 usages

**Checkpoint**: Run `dotnet test src/Tests/KnockOff.NeatooInterfaceTests` - should pass

### Phase 7: Test Migration - Other Projects

- [ ] `KnockOff.Documentation.Samples/AdvancedCallbacksSamples.cs`: 2 usages (callback-based)
- [ ] `KnockOff.Documentation.Samples/PropertiesSamples.cs`: 2 usages (callback-based)
- [ ] `KnockOffSandbox/Program.cs`: 4 usages
- [ ] `PackageTest/Program.cs`: 1 usage

**Note for callback-based usages**: These use `CallCount` inside callbacks to determine behavior:
```csharp
stub.IsConnected.OnGet(() => stub.Connect.CallCount > 0);
```

**Migration approach**: Use local state tracking:
```csharp
var connected = false;
stub.Connect.OnCall(_ => { connected = true; return Task.CompletedTask; });
stub.IsConnected.OnGet(() => connected);
```

**Checkpoint**: Run `dotnet test src/Tests/KnockOff.Documentation.Samples.Tests` - should pass

### Phase 8: Regenerate and Final Verification

- [ ] Regenerate all stub files in `Generated/` directories
- [ ] Run full test suite: `dotnet test src/KnockOff.sln`
- [ ] Verify no compilation errors
- [ ] Verify all tests pass

**Final Checkpoint**: All tests green, all generated files committed

---

## Success Criteria

1. [ ] No `CallCount`, `GetCount`, `SetCount`, `AddCount`, `RemoveCount` properties in generated public/internal API
2. [ ] Count tracking works internally via private fields
3. [ ] All `Verify()`, `Verify(Times)`, `VerifyGet()`, `VerifySet()`, `VerifyAdd()`, `VerifyRemove()` methods work correctly
4. [ ] All three patterns updated consistently
5. [ ] All ~65 test usages migrated to Verify API
6. [ ] All tests pass
7. [ ] Generated files regenerated and committed

---

## Out of Scope

The following are explicitly NOT part of this implementation:

1. Changes to the `IMethodTracking` interface in the KnockOff library
2. Changes to the `Times` class
3. Changes to the `VerificationException` class
4. Any new public API additions
5. Documentation updates (separate todo)

---

## Risks Mitigated

| Risk | Mitigation |
|------|------------|
| Test regression | Checkpoint verification after each phase |
| Missing internal usages | Comprehensive grep search completed |
| Callback-based count access | Identified 4 instances, migration pattern documented |
| Breaking user code | Intentional breaking change, pre-1.0 software |

---

## Evidence Requirements

Before marking complete, provide:

1. Screenshot/output of `dotnet test src/KnockOff.sln` showing all tests passing
2. Code snippet showing a generated interceptor with private count fields
3. Code snippet showing a migrated test using Verify API
4. Confirmation that Generated/ files have been regenerated

