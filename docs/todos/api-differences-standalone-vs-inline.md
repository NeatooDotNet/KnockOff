# API Differences: Standalone vs Inline Stubs

## Status: In Progress

This document captures API inconsistencies discovered when testing KnockOff with Neatoo interfaces.

## Summary

When creating comprehensive tests for Neatoo interfaces using both standalone (`[KnockOff]`) and inline (`[KnockOff<T>]`) stubs, significant API differences were discovered that could confuse users.

## Source of `.Value` Expectation

**Where did `.Value` come from?**

The `.Value` property API for property interceptors is documented in:

1. **`docs/guides/inline-stubs.md`** (lines 126-127, 156):
   ```csharp
   // propStub.Name.Value = "TestProp";
   // propStub.Value.Value = 42;

   // stub.PropertyName.Value          // T - backing value
   ```

2. **Inline Stubs Interceptor API table** (lines 167-169):
   > | Property (get-only) | `GetCount`, `Value`, `OnGet`, `Reset()` |

However, the **skill documentation** (`~/.claude/skills/knockoff/interceptor-api.md`) does NOT include `.Value`:
```
| `GetCount` | `int` | Number of getter invocations |
| `SetCount` | `int` | Number of setter invocations |
| `LastSetValue` | `T?` | Last value passed to setter |
| `OnGet` | `Func<TKnockOff, T>?` | Getter callback |
| `OnSet` | `Action<TKnockOff, T>?` | Setter callback |
```

**Conclusion**: The inline stubs documentation added `.Value` as a convenience property, but:
1. The skill documentation was never updated to include it
2. Standalone stubs weren't updated to generate it

This is a **documentation/implementation gap** where inline stubs have a feature that standalone stubs lack.

## Findings

### 1. Property Interceptor: `Value` vs `OnGet` Only

**Inline stubs** generate property interceptors with both:
- `Value` property (for setting return value)
- `OnGet` callback (for dynamic behavior)

**Standalone stubs** generate property interceptors with only:
- `OnGet` callback (no `Value` property)

**Impact**: Code written for inline stubs won't compile with standalone stubs.

```csharp
// Works with inline stubs
Stubs.IValidateBase.IsBusy.Value = true;

// Standalone stubs require callback pattern
stub.IValidateBase.IsBusy.OnGet = () => true;
```

**Recommendation**: Either:
- Add `Value` property to standalone stub interceptors for consistency
- Or update inline stubs documentation to remove `Value` and unify on `OnGet`

### 2. Duplicate Indexer Members for Inherited Interfaces

When an interface inherits from another interface with the same indexer signature, inline stubs generate duplicate members.

**Example**: `IEntityBase : IValidateBase` where both have `this[string]`:
- Generates `IEntityBase_StringIndexer`
- Also generates `IValidateBase_StringIndexer`
- These conflict when both are in same stub class

**Impact**: Cannot use inline stubs for `IEntityBase` due to duplicate member errors.

**Workaround**: Use standalone stubs for interfaces with inherited indexers.

**Recommendation**: Detect inherited members with same signature and generate single interceptor.

### 3. Method Interceptor Naming Inconsistency

The naming pattern for interceptors differs slightly between standalone and inline:

| Pattern | Standalone | Inline |
|---------|-----------|--------|
| Container | `I{Interface}Interceptors` | `I{Interface}` |
| Member access | `stub.IUserService.GetUser` | `Stubs.IUserService.GetUser` |

This is expected given the architectural difference but should be documented.

### 4. Meta Properties Interceptor Structure

For interfaces with many properties (like `IValidateMetaProperties`), the generated interceptor class may not include all expected members when using standalone pattern.

**Observed**: `RunRules` not found on `IValidateMetaPropertiesInterceptors`

**Root cause**: Needs investigation - may be method vs property detection issue.

### 5. Delegate Stub Return Value for Async Delegates

Async delegate stubs (like `NeatooPropertyChanged` which returns `Task`) return `default!` (null) when no `OnCall` callback is set. This causes `NullReferenceException` when awaiting.

**Workaround**: Always set `OnCall` callback before invoking async delegate stubs:
```csharp
stub.Interceptor.OnCall = (s, args) => Task.CompletedTask;
await del(new NeatooPropertyChangedEventArgs("Prop", this));
```

**Recommendation**: Generator should return `Task.CompletedTask` for async delegates by default.

### 6. IBase Removed in Neatoo 10.6.0

`IBase` interface was removed from Neatoo. The `Parent` property is now on `IValidateBase`.

**Migration**: Update tests to use `stub.IValidateBase.Parent` instead of `stub.IBase.Parent`.

### 7. Method Overload Interceptor Naming

When interfaces have method overloads (like `WaitForTasks()` and `WaitForTasks(CancellationToken)`), interceptors are suffixed with numbers:
- `WaitForTasks1` - first overload (no args)
- `WaitForTasks2` - second overload (with CancellationToken)

Same pattern applies to `RunRules1`, `RunRules2`.

## Tasks

- [ ] Add `Value` property to standalone stub property interceptors
- [ ] Fix duplicate indexer generation for inherited interfaces
- [x] Investigate missing `RunRules` on meta properties interceptor - **RESOLVED**: Named `RunRules1`/`RunRules2` due to overloads
- [x] Verify delegate stub parameter mapping - **RESOLVED**: Works correctly, just returns null for async
- [ ] Generator: Return `Task.CompletedTask` for async delegate stubs by default
- [ ] Add documentation explaining pattern differences
- [ ] Consider unifying API where possible

## Test Coverage Completed

- [x] Standalone stubs for IEntityBase (EntityBaseStub)
- [x] Standalone stubs for IValidateBase (ValidateBaseStub)
- [x] Inline stubs for IValidateBase
- [x] ~~Inline stubs for IBase~~ - IBase removed in Neatoo 10.6.0
- [x] Delegate stubs for NeatooPropertyChanged
- [x] Custom interfaces extending Neatoo types (IBuildingEdit, IPersonEdit)
- [x] Multiple interface inline stubs
- [x] Nested class stubs

## Related Files

- `src/Tests/KnockOffTests/NeatooTests.cs` - Comprehensive Neatoo tests
- `src/Generator/KnockOffGenerator.cs` - Generator code
- `src/Directory.Packages.props` - Updated to Neatoo 10.6.0
