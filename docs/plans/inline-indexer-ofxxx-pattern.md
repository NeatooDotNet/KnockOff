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
    EquatableArray<InlineIndexerModel> Indexers);
```

**Note:** No `TypeParameterList` or `ConstraintClauses` on the group - these are on the individual `InlineIndexerModel` records. The container class is not generic; the contained interceptors carry any generics.

#### Modify: `InlineIndexerModel`

Add `KeyTypeFriendlyName` field:

```csharp
// Add to InlineIndexerModel record
/// <summary>Friendly name for the key type (e.g., "Int32", "String") for OfXxx pattern.</summary>
string KeyTypeFriendlyName
```

**Note:** `BaseName` is NOT needed on `InlineIndexerModel` - for inline stubs it is always "Indexer". The `BaseName` is stored in `InlineIndexerGroup` instead.

#### Modify: `InlineInterfaceStubModel`

Add `IndexerGroups` field:

```csharp
// Add to InlineInterfaceStubModel record
/// <summary>Indexer groups for container generation.</summary>
EquatableArray<InlineIndexerGroup> IndexerGroups,
```

### Builder Changes

#### `InlineModelBuilder.cs`

1. Add `GetKeyTypeFriendlyName` helper method (reuse `GetTypeSuffix` pattern from `FlatModelBuilder.cs` lines 1203-1221)
2. Update `BuildIndexerModel` to compute `KeyTypeFriendlyName`
3. Add `GroupIndexers` method to create `InlineIndexerGroup` records
4. Update `BuildInterfaceStub` to build indexer groups
5. Update `BuildInterceptorProperties` to:
   - For single-indexer groups: emit direct `Indexer` property (type = interceptor class)
   - For multi-indexer groups: emit `Indexer` property (type = container class)
6. Build indexer access map for implementations (see Access Map Usage below)

**KeyTypeFriendlyName computation** - reuse the existing `GetTypeSuffix` method pattern from `FlatModelBuilder.cs`:

```csharp
private static string GetTypeSuffix(string type)
{
    var simple = type.Replace("global::", "").Replace("System.", "");
    simple = simple switch
    {
        "int" => "Int32",
        "string" => "String",
        "bool" => "Boolean",
        "long" => "Int64",
        "double" => "Double",
        "float" => "Single",
        "decimal" => "Decimal",
        "char" => "Char",
        "byte" => "Byte",
        "void" => "Void",
        _ => simple.Replace(".", "_").Replace("<", "_").Replace(">", "").Replace(",", "_").Replace(" ", "")
    };
    return simple.TrimEnd('?');
}
```

#### Access Map Usage

The access map determines the correct interceptor access path for implementations:

| Scenario | InterceptorName | Access Path |
|----------|-----------------|-------------|
| Single indexer | `IndexerString` | `Indexer` |
| Multi indexer (string) | `IndexerString` | `Indexer.OfString` |
| Multi indexer (int) | `IndexerInt32` | `Indexer.OfInt32` |

Build the map in `InlineModelBuilder` (same pattern as `FlatModelBuilder.cs` lines 1711-1730):

```csharp
private static Dictionary<string, string> BuildIndexerAccessMap(IEnumerable<InlineIndexerGroup> groups)
{
    var map = new Dictionary<string, string>();

    foreach (var group in groups)
    {
        if (group.Indexers.Count == 1)
        {
            // Single indexer - direct access
            var indexer = group.Indexers.GetArray()![0];
            map[indexer.IndexerName] = group.BaseName;
        }
        else
        {
            // Multiple indexers - container with OfXxx pattern
            foreach (var indexer in group.Indexers)
            {
                map[indexer.IndexerName] = $"{group.BaseName}.Of{indexer.KeyTypeFriendlyName}";
            }
        }
    }

    return map;
}
```

Pass the access map to implementations via `InlineInterfaceImplementation.InterceptorName` field.

#### InterceptorProperties Generation Changes

Update `BuildInterceptorProperties` to handle containers:

```csharp
// For indexers, use groups instead of individual members
foreach (var group in indexerGroups)
{
    if (group.Indexers.Count == 1)
    {
        // Single indexer - direct property
        var indexer = group.Indexers.GetArray()![0];
        properties.Add(new InlineInterceptorPropertyModel(
            PropertyName: group.BaseName,
            InterceptorTypeName: indexer.InterceptorClassName,
            NeedsNewKeyword: group.NeedsNewKeyword,
            Description: $"Interceptor for indexer."));
    }
    else
    {
        // Multiple indexers - container property
        properties.Add(new InlineInterceptorPropertyModel(
            PropertyName: group.BaseName,
            InterceptorTypeName: group.ContainerClassName,
            NeedsNewKeyword: group.NeedsNewKeyword,
            Description: $"Container for indexer interceptors. Access via .OfXxx."));
    }
}
```

### Renderer Changes

#### `InlineRenderer.cs`

1. **Add** `RenderIndexerContainerClass` method (adapt from `FlatRenderer.cs` lines 571-636)
2. **Add** `BuildIndexerAccessMap` helper method
3. **Modify** `RenderInterfaceStub` to:
   - Render container classes for multi-indexer groups (after individual interceptors)
   - Interceptor properties already handled by model (via `InlineInterceptorPropertyModel`)
4. **Modify** `RenderIndexerImplementation` to:
   - Accept access map as parameter
   - Use access map to get correct path (e.g., `Indexer.OfString` vs `Indexer`)
5. **Modify** `RenderInlineVerifyMethods` for containers (aggregate from contained interceptors)
6. **Modify** `RenderSourceMethods` to route through container (see Source(T) Delegation below)

**Note:** The FlatRenderer implementation loop for indexers is at lines 145-146, not 1200-1250. Line 2376 is where `RenderIndexerImplementation` is defined.

#### Verify Methods Updates

Update `RenderInlineVerifyMethods` to aggregate verification from containers:

```csharp
// For multi-indexer groups, aggregate from container
w.Line($"if ({group.BaseName}.CheckVerification() is {{ }} {group.BaseName}Failure) failures.Add({group.BaseName}Failure);");
```

For `VerifyAll`:
```csharp
w.Line($"if ({group.BaseName}.CheckVerificationAll() is {{ }} {group.BaseName}Failure) failures.Add({group.BaseName}Failure);");
```

#### Source(T) Delegation

For multi-indexer groups, `Source(T)` must set `_source` on each contained interceptor:

```csharp
// In Source(T) method generation
// For single indexer: Indexer._source = source;
// For multi indexer:
Indexer.OfString._source = source;
Indexer.OfInt32._source = source;
```

The access map built in the builder should be used to generate the correct paths.

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
        if (OfString.CheckVerification() is { } failureString) return failureString;
        if (OfInt32.CheckVerification() is { } failureInt32) return failureInt32;
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
            if (Indexer.OfString._source is { } src) return src[key];
            // ...
        }
    }
}
```

**For single-indexer interfaces:**
Keep current behavior - direct `Indexer` property without container.

---

## Implementation Steps

### Phase 1: Model Changes (Checkpoint: Build succeeds)

1. **Create** `src/Generator/Model/Inline/InlineIndexerGroup.cs`
   - Simple record with: `BaseName`, `ContainerClassName`, `NeedsNewKeyword`, `Indexers`
   - No type parameters on the group

2. **Modify** `src/Generator/Model/Inline/InlineIndexerModel.cs`
   - Add `KeyTypeFriendlyName` parameter

3. **Modify** `src/Generator/Model/Inline/InlineInterfaceStubModel.cs`
   - Add `IndexerGroups` parameter

**Verification:** `dotnet build src/Generator`

### Phase 2A: Builder - Indexer Grouping (Checkpoint: Build succeeds)

4. **Modify** `src/Generator/Builder/InlineModelBuilder.cs`
   - Add `GetTypeSuffix` helper method (copy from FlatModelBuilder)
   - Update `BuildIndexerModel` to compute `KeyTypeFriendlyName`
   - Add `GroupIndexers` method to create `InlineIndexerGroup` records
   - Update `BuildInterfaceStub` to call `GroupIndexers` and pass to model

**Verification:** `dotnet build src/Generator`

### Phase 2B: Builder - Access Map and Properties (Checkpoint: Build succeeds)

5. **Modify** `src/Generator/Builder/InlineModelBuilder.cs`
   - Add `BuildIndexerAccessMap` helper method
   - Update `BuildInterceptorProperties` to emit container property for multi-indexer groups
   - Update implementation building to use access map for `InterceptorName`

**Verification:** `dotnet build src/Generator`

### Phase 3A: Renderer - Container Class (Checkpoint: Build succeeds)

6. **Modify** `src/Generator/Renderer/InlineRenderer.cs`
   - Add `RenderIndexerContainerClass` method (adapt from FlatRenderer lines 571-636)
   - Call from `RenderInterfaceStub` for groups with multiple indexers

**Verification:** `dotnet build src/Generator`

### Phase 3B: Renderer - Implementation Updates (Checkpoint: Build and basic tests pass)

7. **Modify** `src/Generator/Renderer/InlineRenderer.cs`
   - Update `RenderIndexerImplementation` to use access map for interceptor path
   - Pass access map through rendering pipeline

**Verification:** `dotnet build && dotnet test src/Tests/KnockOffTests --filter "FullyQualifiedName~Indexer"`

### Phase 3C: Renderer - Verification and Source (Checkpoint: All tests pass)

8. **Modify** `src/Generator/Renderer/InlineRenderer.cs`
   - Update `RenderInlineVerifyMethods` to aggregate from containers
   - Update `RenderSourceMethods` to route through container paths

**Verification:** `dotnet test src/Tests/KnockOffTests`

### Phase 4: Testing

9. **Create** `src/Tests/KnockOffTests/InlineMultiIndexerTests.cs` (if not exists)
   - Test multi-indexer `stub.Indexer.OfString` API
   - Test multi-indexer `stub.Indexer.OfInt32` API
   - Test container Reset() method
   - Test container verification aggregation
   - Test Source(T) through container

10. **Verify** single indexer behavior unchanged
    - Test direct `stub.Indexer.OnGet` access (no OfXxx)

11. **Run** full test suite to verify no regressions

**Verification:** `dotnet test`

### Phase 5: Documentation

12. **Update** todo status to complete Phase 3
13. **Update** inline stub documentation (if exists)

---

## File Changes Summary

| File | Change Type | Description |
|------|-------------|-------------|
| `src/Generator/Model/Inline/InlineIndexerGroup.cs` | Create | New model record |
| `src/Generator/Model/Inline/InlineIndexerModel.cs` | Modify | Add `KeyTypeFriendlyName` |
| `src/Generator/Model/Inline/InlineInterfaceStubModel.cs` | Modify | Add `IndexerGroups` |
| `src/Generator/Builder/InlineModelBuilder.cs` | Modify | Add grouping logic, access map |
| `src/Generator/Renderer/InlineRenderer.cs` | Modify | Add container rendering |
| `src/Tests/KnockOffTests/InlineMultiIndexerTests.cs` | Create/Modify | Test OfXxx API |

---

## Test Cases

### Must-Have Tests

1. **Single indexer keeps direct access**
   ```csharp
   // Interface with one indexer
   stub.Indexer.OnGet = (key) => "value";  // NOT stub.Indexer.OfString
   ```

2. **Multi-indexer uses OfXxx pattern**
   ```csharp
   stub.Indexer.OfString.OnGet = (key) => "value";
   stub.Indexer.OfInt32.OnGet = (index) => 42;
   ```

3. **Source(T) through container**
   ```csharp
   stub.Source(realImplementation);
   // Verify Indexer.OfString._source and Indexer.OfInt32._source are set
   ```

4. **Container Reset() aggregates**
   ```csharp
   stub.Indexer.Reset();  // Should reset all contained interceptors
   ```

5. **Container verification aggregates**
   ```csharp
   stub.Indexer.OfString.Verifiable();
   stub.Verify();  // Should check OfString
   ```

6. **Inherited interface indexers**
   ```csharp
   // IChild : IParent where both have indexers
   // All indexers should be grouped correctly
   ```

7. **Open generic interface with indexers**
   ```csharp
   [KnockOff<IGenericRepo<>>]
   // Container should work with generic interceptors
   ```

### Edge Case Tests

8. **Tuple key types** (multi-parameter indexers)
   ```csharp
   // this[string key, int index] -> KeyTypeFriendlyName = "String_Int32"
   stub.Indexer.OfString_Int32.OnGet = (key, index) => "value";
   ```

9. **Same key type from different interfaces**
   ```csharp
   // IFoo : IBar where both have this[string]
   // Should deduplicate by KeyTypeFriendlyName
   ```

---

## Acceptance Criteria

- [ ] Inline stubs with multiple indexers expose `Indexer.OfXxx` pattern
- [ ] Inline stubs with single indexer keep direct `Indexer` property
- [ ] All existing indexer tests pass
- [ ] New `InlineMultiIndexerTests.cs` tests pass with OfXxx API
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

**Decision**: Keep single-indexer as direct property (`stub.Indexer.OnGet`) matching standalone behavior. The container pattern only activates when there are multiple indexer key types.

### Tuple Key Types

Multi-parameter indexers like `this[string key, int index]` will have `KeyTypeFriendlyName` computed by `GetTypeSuffix`. The tuple type `(string, int)` will be converted to something like `ValueTuple_String_Int32`. Verify this produces valid C# identifiers.

**Risk Mitigation**: The existing `GetTypeSuffix` method handles angle brackets and commas by replacing with underscores, which should work for tuples.

### Interface Inheritance

When `IChild : IParent` both have indexers with the same key type, they should share the same container slot (deduplicated by `KeyTypeFriendlyName`). This matches the standalone behavior.

**Risk Mitigation**: Group by `BaseName` first, then deduplicate within the group by `KeyTypeFriendlyName`.

### Open Generic Interface Indexers

For `[KnockOff<IRepo<>>]` with indexers, the individual interceptor classes carry the type parameters:

```csharp
public class IndexerContainer
{
    public IRepo_IndexerInterceptor<T> OfT { get; } = new();
}
```

The container itself is NOT generic. This matches the standalone pattern.

### Verification Message Consistency

Ensure verification failure messages match between standalone and inline:
- Standalone: `"Indexer"` (generic name)
- Inline: Should use specific names (`IndexerString`, `IndexerInt32`) for clarity in multi-indexer scenarios.

**Decision**: Keep specific names in failure messages for better debugging.

---

## Reference Implementation

The standalone pattern implementation in `FlatRenderer.cs` serves as the authoritative reference:

- `RenderIndexerContainerClass` (lines 571-636)
- `BuildIndexerAccessMap` (lines 171-194)
- `RenderIndexerImplementation` (lines 2376-2398) - note: called in loop at lines 145-146
- `RenderInterceptorProperties` handling for containers (lines 122-126)

The builder implementation in `FlatModelBuilder.cs`:
- Indexer grouping (lines 52-58)
- Access map for Source(T) (lines 1710-1755)
- `GetTypeSuffix` (lines 1203-1221)
