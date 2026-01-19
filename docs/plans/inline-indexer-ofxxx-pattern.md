# Inline Indexer OfXxx Pattern Implementation

**Date:** 2026-01-19
**Related Todo:** [Inline Interceptor API Unification](../todos/inline-interceptor-api-unification.md)
**Status:** Draft
**Last Updated:** 2026-01-19

---

## Overview

Implement the OfXxx pattern for inline stubs with multiple indexers to achieve API consistency with standalone stubs. Currently, inline stubs expose individual indexer properties (`IndexerString`, `IndexerInt32`), while standalone stubs use an `IndexerContainer` class with `OfXxx` properties (`Indexer.OfString`, `Indexer.OfInt32`).

---

## Problem Statement

**Current inline stub API (inconsistent):**
```csharp
var stub = new InlineIndexerTestClass.Stubs.IMultiIndexerService();
stub.IndexerString.OnGet = (key) => "value";  // Direct property
stub.IndexerInt32.OnGet = (index) => 42;      // Direct property
```

**Target API (matches standalone):**
```csharp
var stub = new InlineIndexerTestClass.Stubs.IMultiIndexerService();
stub.Indexer.OfString.OnGet = (key) => "value";  // IndexerContainer
stub.Indexer.OfInt32.OnGet = (index) => 42;      // IndexerContainer
```

This inconsistency causes:
1. Breaking API changes when adding indexer overloads to an interface
2. Different mental models for inline vs. standalone stubs
3. Harder migration between stub patterns

---

## Approach

Replicate the standalone indexer grouping pattern in inline stubs:

1. **Model Changes**: Add `InlineIndexerGroup` record and `IndexerGroups` field to `InlineInterfaceStubModel`
2. **Builder Changes**: Group indexers by base name in `InlineModelBuilder`, similar to `FlatModelBuilder`
3. **Renderer Changes**: Generate `IndexerContainer` class for multi-indexer groups, update interceptor properties and implementations

The standalone pattern (in `FlatRenderer.cs`) serves as the reference implementation.

---

## Design

### Model Changes

#### New Model: `InlineIndexerGroup`

```csharp
// src/Generator/Model/Inline/InlineIndexerGroup.cs
internal sealed record InlineIndexerGroup(
    /// <summary>Base name for the group (e.g., "Indexer").</summary>
    string BaseName,
    /// <summary>Name of the container class (e.g., "IndexerContainer").</summary>
    string ContainerClassName,
    /// <summary>Whether this group needs the 'new' keyword.</summary>
    bool NeedsNewKeyword,
    /// <summary>All indexers in this group.</summary>
    EquatableArray<InlineIndexerModel> Indexers,
    /// <summary>Type parameter list for open generic interfaces.</summary>
    string TypeParameterList,
    /// <summary>Constraint clauses for type parameters.</summary>
    string ConstraintClauses);
```

#### Modify: `InlineIndexerModel`

Add `KeyTypeFriendlyName` field (matching `FlatIndexerModel`):

```csharp
// Add to InlineIndexerModel record
/// <summary>Friendly name for the key type (e.g., "Int32", "String") for OfXxx pattern.</summary>
string KeyTypeFriendlyName,
/// <summary>Base name for indexer grouping (e.g., "Indexer").</summary>
string BaseName
```

#### Modify: `InlineInterfaceStubModel`

Add `IndexerGroups` field:

```csharp
// Add to InlineInterfaceStubModel record
/// <summary>Indexer groups for container generation.</summary>
EquatableArray<InlineIndexerGroup> IndexerGroups,
```

### Builder Changes

#### `InlineModelBuilder.cs`

1. Update `BuildIndexerModel` to compute `KeyTypeFriendlyName` and `BaseName`
2. Add `GroupIndexers` method (similar to `FlatModelBuilder`)
3. Update `BuildInterfaceStub` to create indexer groups
4. Update `BuildInterceptorProperties` to emit container property instead of individual indexer properties
5. Update `BuildImplementations` to route through container

**Key computation for `KeyTypeFriendlyName`:**
```csharp
private static string GetKeyTypeFriendlyName(string keyType)
{
    // Handle simple types
    if (keyType == "int" || keyType == "global::System.Int32") return "Int32";
    if (keyType == "string" || keyType == "global::System.String") return "String";
    // ... etc for other common types

    // Extract simple name from global:: qualified type
    var lastDot = keyType.LastIndexOf('.');
    return lastDot >= 0 ? keyType.Substring(lastDot + 1) : keyType;
}
```

### Renderer Changes

#### `InlineRenderer.cs`

1. **Add** `RenderIndexerContainerClass` method (copy pattern from `FlatRenderer.RenderIndexerContainerClass`)
2. **Modify** `RenderInterfaceStub` to:
   - Render container classes for multi-indexer groups
   - Emit single `Indexer` property of container type (for multi-indexer groups)
   - Keep individual properties for single-indexer groups
3. **Modify** `RenderIndexerImplementation` to:
   - Route through `Indexer.OfXxx` for multi-indexer groups
   - Use `indexerAccessMap` pattern from `FlatRenderer`
4. **Modify** `RenderInlineVerifyMethods` to aggregate verification from container
5. **Modify** `RenderSourceMethods` to route through container

### Generated Code Pattern

**For multi-indexer groups:**
```csharp
// Container class (same pattern as standalone)
public sealed class IndexerContainer
{
    public IMultiIndexerService_IndexerStringInterceptor OfString { get; } = new();
    public IMultiIndexerService_IndexerInt32Interceptor OfInt32 { get; } = new();

    public void Reset()
    {
        OfString.Reset();
        OfInt32.Reset();
    }

    internal bool IsVerifiable => false;
    internal bool IsConfigured => OfString.IsConfigured || OfInt32.IsConfigured;

    internal VerificationFailure? CheckVerification()
    {
        if (OfString.CheckVerification() is { } f1) return f1;
        if (OfInt32.CheckVerification() is { } f2) return f2;
        return null;
    }

    internal VerificationFailure? CheckVerificationAll() { /* same pattern */ }
}

// Stub class
public class IMultiIndexerService : global::KnockOff.Tests.IMultiIndexerService
{
    /// <summary>Interceptor for indexer. Access via .OfXxx.</summary>
    public IndexerContainer Indexer { get; } = new();

    // Explicit implementation routes through container
    string IMultiIndexerService.this[string key]
    {
        get
        {
            Indexer.OfString.RecordGet(key);
            if (Indexer.OfString.OnGet is { } onGet) return onGet(key);
            // ...
        }
    }
}
```

**For single-indexer interfaces:**
Keep current behavior - direct `Indexer` property without container.

---

## Implementation Steps

### Phase 1: Model Changes

1. **Create** `src/Generator/Model/Inline/InlineIndexerGroup.cs`
   - Copy pattern from `FlatIndexerGroup.cs`
   - Add `TypeParameterList` and `ConstraintClauses` for open generics

2. **Modify** `src/Generator/Model/Inline/InlineIndexerModel.cs`
   - Add `KeyTypeFriendlyName` parameter
   - Add `BaseName` parameter

3. **Modify** `src/Generator/Model/Inline/InlineInterfaceStubModel.cs`
   - Add `IndexerGroups` parameter

### Phase 2: Builder Changes

4. **Modify** `src/Generator/Builder/InlineModelBuilder.cs`
   - Add `GetKeyTypeFriendlyName` helper method
   - Update `BuildIndexerModel` to compute `KeyTypeFriendlyName` and `BaseName`
   - Add `GroupIndexers` method
   - Update `BuildInterfaceStub` to build indexer groups
   - Update `BuildInterceptorProperties` for container pattern
   - Build indexer access map for implementations

### Phase 3: Renderer Changes

5. **Modify** `src/Generator/Renderer/InlineRenderer.cs`
   - Add `RenderIndexerContainerClass` method
   - Add `BuildIndexerAccessMap` helper method
   - Update `RenderInterfaceStub` to render containers
   - Update interceptor property generation
   - Update `RenderIndexerImplementation` to use access map
   - Update `RenderInlineVerifyMethods` for containers
   - Update `RenderSourceMethods` for containers

### Phase 4: Testing

6. **Modify** `src/Tests/KnockOffTests/InlineMultiIndexerTests.cs`
   - Update test API calls from `stub.IndexerString` to `stub.Indexer.OfString`
   - Update test API calls from `stub.IndexerInt32` to `stub.Indexer.OfInt32`
   - Add test for container Reset() method
   - Add test for container verification aggregation

7. **Run** full test suite to verify no regressions

### Phase 5: Documentation

8. **Update** inline stub documentation (if exists)
9. **Update** todo status to complete Phase 3

---

## File Changes Summary

| File | Change Type | Description |
|------|-------------|-------------|
| `src/Generator/Model/Inline/InlineIndexerGroup.cs` | Create | New model record |
| `src/Generator/Model/Inline/InlineIndexerModel.cs` | Modify | Add `KeyTypeFriendlyName`, `BaseName` |
| `src/Generator/Model/Inline/InlineInterfaceStubModel.cs` | Modify | Add `IndexerGroups` |
| `src/Generator/Builder/InlineModelBuilder.cs` | Modify | Add grouping logic |
| `src/Generator/Renderer/InlineRenderer.cs` | Modify | Add container rendering |
| `src/Tests/KnockOffTests/InlineMultiIndexerTests.cs` | Modify | Update test API |

---

## Acceptance Criteria

- [ ] Inline stubs with multiple indexers expose `Indexer.OfXxx` pattern
- [ ] Inline stubs with single indexer keep direct `Indexer` property
- [ ] All existing indexer tests pass
- [ ] `InlineMultiIndexerTests.cs` passes with OfXxx API
- [ ] Container Reset() aggregates to all child interceptors
- [ ] Container verification aggregates correctly
- [ ] Source(T) delegation works through container
- [ ] API is identical between inline and standalone for multi-indexer interfaces

---

## Dependencies

- None - all changes are internal to the generator

---

## Risks / Considerations

### Breaking Change

This is a **breaking API change** for any inline stubs with multiple indexers. Users must update:

| Old API | New API |
|---------|---------|
| `stub.IndexerString.OnGet` | `stub.Indexer.OfString.OnGet` |
| `stub.IndexerInt32.OnGet` | `stub.Indexer.OfInt32.OnGet` |

**Mitigation**: This is expected as part of the larger interceptor API unification effort.

### Single vs Multi Indexer Behavior

Decision: Should single-indexer interfaces also use the container pattern for consistency?

**Recommendation**: No. Keep single-indexer as direct property (`stub.Indexer.OnGet`) matching standalone behavior. The container pattern only activates when there are multiple indexer key types.

### Generic Interface Indexers

Open generic interfaces with indexers need container classes that carry type parameters:

```csharp
public class IndexerContainer<T>
{
    public IGenericInterface_IndexerTInterceptor<T> OfT { get; } = new();
}
```

This is already handled by passing `TypeParameterList` and `ConstraintClauses` to the model.

### Verification Message Consistency

Ensure verification failure messages match between standalone and inline:
- Standalone: `"Indexer"` (generic name)
- Inline: `"IndexerString"` (specific name) vs. `"Indexer"` through container

**Decision**: Keep specific names (`IndexerString`, `IndexerInt32`) for clarity in multi-indexer scenarios.

---

## Reference Implementation

The standalone pattern implementation in `FlatRenderer.cs` serves as the authoritative reference:

- `RenderIndexerContainerClass` (lines 571-636)
- `BuildIndexerAccessMap` (lines 171-194)
- `RenderIndexerImplementation` (lines 1200-1250)
- `RenderInterceptorProperties` handling for containers
