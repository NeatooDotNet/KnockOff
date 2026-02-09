# Generic Type Gaps - Validation and Fix Plan

**Date:** 2026-02-08
**Related Todo:** [Generic Type Gaps](../todos/generic-type-gaps.md)
**Status:** Under Review (Developer)
**Last Updated:** 2026-02-08

---

## Overview

User feedback indicates KnockOff may not support more than one generic type parameter. This plan validates comprehensive generic type support across all applicable patterns by: (1) documenting what currently works with evidence, (2) identifying what is missing, and (3) proposing implementation phases to fill gaps.

Additionally, Rocks library testing revealed four concrete bugs (Gaps 26, 27/28, 31) that must be fixed. These are incorporated into the implementation plan alongside the originally discovered SmartDefault collision (Bug 1).

---

## Approach

### Validation Strategy

For each feature in the scope matrix, verification follows this order:
1. **Search existing tests** in `src/Tests/KnockOffTests/` for coverage
2. **Search Design projects** for compilation evidence
3. **Write minimal Design.Stubs code** exercising the feature if no coverage exists
4. **Build** to verify compilation
5. **Mark result** in scope table: Verified, Needs Implementation, or Needs Investigation

### Pipeline Reference

| Patterns | Transform | Builder | Renderer |
|---|---|---|---|
| P1,P2: `[KnockOff]` interface | `TransformClass` | `FlatModelBuilder` | `FlatRenderer` |
| P3,P4: `[KnockOffBase<T>]` class | `TransformStandaloneClassStub` | `StandaloneClassModelBuilder` | `StandaloneClassRenderer` |
| P5,P6: Inline interface/class | `TransformInlineStubClass` | `InlineModelBuilder` | `InlineRenderer` |
| P7,P8,P9: Open generic | `TransformInlineStubClass` | `InlineModelBuilder` | `InlineRenderer` |

---

## Current State Analysis

### Evidence of Existing Multi-Type-Parameter Support

#### P2: Generic Standalone (Multi-type-param interface)

**Existing coverage - VERIFIED:**
- `GenericStandaloneStubTests.cs:270-282`: `IGenericKeyValueStore<TKey, TValue>` with `GenericKeyValueStoreStub<TKey, TValue>` -- two type params, no constraints. Tests compile and pass.
- `GenericStandaloneEdgeCaseTests.cs:246-254`: `INestedDictService<TKey, TValue> where TKey : notnull` with `NestedDictStub<TKey, TValue> where TKey : notnull` -- two type params, one constraint. Tests compile and pass.

#### P4: Generic Standalone Class (Multi-type-param class)

**VERIFIED via Design.Stubs compilation.** `CacheStub<TKey, TValue>` using `[KnockOffBase(typeof(CacheBase<,>))]` compiles successfully. Generates correct interceptors for `Get`, `Set`, `ContainsKey`, `Name`, and `Transform<TResult>` (method-level type param with `Of<TResult>()` pattern).

- Evidence: `src/Design/Design.Stubs/StubPatterns/GenericTypeGapsVerification.cs:30`
- Generated: `src/Design/Design.Stubs/Generated/KnockOff.Generator/KnockOff.KnockOffGenerator/CacheStub`2.g.cs`

#### P8: Open Generic Interface (Multi-type-param)

**Existing coverage - VERIFIED:**
- `OpenGenericInlineStubTests.cs:300-304`: `IKeyValueStore<TKey, TValue>` used with `[KnockOff(typeof(IKeyValueStore<,>))]`. Tests compile and pass.

#### P9: Open Generic Class (Multi-type-param)

**Existing coverage - VERIFIED:**
- `OpenGenericInlineStubTests.cs:204-210`: `OGCache<TKey, TValue> where TKey : notnull where TValue : new()` used with `[KnockOff(typeof(OGCache<,>))]`. Tests compile and pass with multiple constraints.
- **VERIFIED via Design.Stubs compilation:** `OpenGenericCacheTest` using `[KnockOff(typeof(CacheBase<,>))]` also compiles, including method-level `Transform<TResult>` with `Of<TResult>()` pattern.
  - Evidence: `src/Design/Design.Stubs/StubPatterns/GenericTypeGapsVerification.cs:61`

#### Method-Level Generic Type Parameters

**Existing coverage - VERIFIED:**
- `GenericMethodTests.cs:244-289`: `Convert<TIn, TOut>` -- multiple method-level type params. Tests compile and pass.
- `GenericConstraintCoverageTests.cs:140-152`: `ConvertWithConstraint<TBase, TResult> where TResult : TBase` -- cross-referencing constraint between method type params. Tests compile and pass.
- `GenericMethodBugTests.cs:357`: `TOut Convert<TIn, TOut>(TIn input) where TOut : new()` -- multi-type method with constraint.

#### Where Clause Coverage

**Existing coverage - VERIFIED:**
- Single constraint: `where T : class` -- `GenericStandaloneStubTests.cs:263`, `GenericStandaloneEdgeCaseTests.cs:290`
- `where T : struct` -- `GenericConstraintCoverageTests.cs:221`, `GenericStandaloneEdgeCaseTests.cs:301`
- `where T : notnull` -- `GenericConstraintCoverageTests.cs:224`
- `where T : new()` -- `GenericStandaloneEdgeCaseTests.cs:312`
- Multiple constraints `where T : class, IEntity` -- `GenericStandaloneEdgeCaseTests.cs:290`
- Method-level where clause: `where T : class, new()` -- `GenericConstraintCoverageTests.cs:227`
- Method-level struct: `where T : struct` -- `GenericConstraintCoverageTests.cs:221`
- Cross-referencing: `where TResult : TBase` -- `GenericConstraintCoverageTests.cs:239`
- Multiple interface + class: `where T : class, IComparable, IConvertible` -- `GenericConstraintCoverageTests.cs:230`
- Type constrained by class: `where T : Attribute` (implies class) -- `GenericMethodBugTests.cs:404`
- Self-referential: `where T : IComparable<T>` -- `GenericConstraintCoverageTests.cs:233`

#### Generic Delegates

**Existing coverage - VERIFIED:**
- `Design.Domain/Delegates/Delegates.cs:29`: `Factory<T>` -- single type param delegate
- `OpenGenericInlineStubTests.cs:150`: `OGConverter<TIn, TOut, TResult> where TResult : class` -- three type params with constraint
- `OpenGenericInlineStubTests.cs:152-153`: `[KnockOff(typeof(OGConverter<,,>))]` -- open generic multi-param delegate. Tests compile and pass.

### Generator Infrastructure Analysis

The generator properly handles multi-type parameters at every pipeline stage:

1. **Transform layer** (`SymbolHelpers.ExtractTypeParameters`): Extracts type parameter names and constraints from `ITypeParameterSymbol`. Handles `class`, `struct`, `unmanaged`, `notnull`, interface constraints, class constraints, and `new()`. All constraints are stored as strings in `TypeParameterInfo.Constraints`.

2. **Builder layer** (`FlatModelBuilder.BuildTypeParameters`, `InlineModelBuilder`): Converts `TypeParameterInfo` to `TypeParameterModel(Name, Constraints)`. Constraint strings are joined with `", "` and emitted as `where T : constraint1, constraint2`.

3. **Renderer layer** (`FormatTypeParameterList`, `FormatTypeConstraints`): Emits `<T1, T2>` and `where T1 : ... where T2 : ...` syntax. `ReplaceUnboundGeneric` handles replacing `<,>` with `<T1, T2>` for open generics.

4. **Method-level type parameters**: `InterfaceMemberInfo.FromMethod` extracts `IsGenericMethod` and `TypeParameters` for each method. `GetConstraintsForExplicitImpl` handles the explicit impl constraint reduction (only class/struct for CS0460). `HasUnconstrainedNullableTypeParams` handles the T? edge case.

---

## Scope Table

### Feature A: Multi-type-param interface

| Pattern | Status | Evidence |
|---|---|---|
| P1 Standalone (closed) | Verified | Implicit -- closing `IFoo<string, int>` is standard usage |
| P2 Generic Standalone (open) | Verified | `GenericKeyValueStoreStub<TKey, TValue>` in `GenericStandaloneStubTests.cs:280` |
| P5 Inline Interface (closed) | Verified | `ICollection<string, int>` pattern supported in `Design.Stubs/IndexerBasics.cs` |
| P8 Open Generic Interface | Verified | `IKeyValueStore<TKey, TValue>` in `OpenGenericInlineStubTests.cs:300` |

### Feature B: Multi-type-param class

| Pattern | Status | Evidence |
|---|---|---|
| P3 Standalone Class (closed) | N/A | Closed generic class target compiles if class exists |
| P4 Generic Standalone Class (open) | **Verified (new code)** | `CacheStub<TKey, TValue>` in `Design.Stubs/StubPatterns/GenericTypeGapsVerification.cs:30` -- compiles |
| P6 Inline Class (closed) | N/A | Closed generic class target compiles if class exists |
| P9 Open Generic Class | Verified | `OGCache<TKey, TValue>` in `OpenGenericInlineStubTests.cs:204` + new `OpenGenericCacheTest` in `GenericTypeGapsVerification.cs:61` |

### Feature C: Methods using class type params

| Pattern | Status | Evidence |
|---|---|---|
| P1 Standalone | Verified | `IGenericService<T>.GetById(int) -> T?` in `AllPatterns.cs` |
| P2 Generic Standalone | Verified | `GenericServiceStub<T> : IGenericService<T>` in `AllPatterns.cs:197` |
| P3 Standalone Class | Verified | Uses class type params in virtual methods |
| P4 Generic Standalone Class | Verified | `ClassRepositoryBase<T>.GetById(int) -> T?` in `GenericStandaloneClassStubTests.cs:269` |
| P5 Inline Interface | Verified | Closed generic interfaces work by definition |
| P6 Inline Class | Verified | Closed generic class stubs work |
| P8 Open Generic Interface | Verified | `IOGRepository<T>.GetById` tests pass |
| P9 Open Generic Class | Verified | `OGRepository<T>.GetById` tests pass |

### Feature D: Methods with own type params (single)

| Pattern | Status | Evidence |
|---|---|---|
| P1 Standalone | Verified | `GenericMethodServiceKnockOff : IGenericMethodService` tests pass |
| P2 Generic Standalone | **Needs Implementation** | CS0693: `SmartDefault<T>` collides with outer `<T>` -- see Bug 1 below |
| P3 Standalone Class | Verified | `GenericMethodBase.Convert<T>` via class-stub-generic-method-support plan |
| P4 Generic Standalone Class | Verified | `GenericMethodRepositoryBase<TEntity>.ConvertEntity<TResult>` -- compiles (no `SmartDefault` in class pipeline) |
| P5 Inline Interface | Verified | `GenericConstraintInlineTests` covers method-level type params |
| P6 Inline Class | Verified | `GenericMethodBase` inline stubs tested |
| P8 Open Generic Interface | **Needs Implementation** | CS0693: `SmartDefault<T>` collides with outer `<T>` -- see Bug 1 below |
| P9 Open Generic Class | **Verified (new code)** | `OpenGenericCacheTest` with `CacheBase<TKey, TValue>.Transform<TResult>` -- compiles (class pipeline, no `SmartDefault`) |

### Feature E: Methods with multiple own type params

| Pattern | Status | Evidence |
|---|---|---|
| P1 Standalone | Verified | `Convert<TIn, TOut>` in `GenericMethodTests.cs:244` -- works when all overloads of a method name have the same type arity |
| P2 Generic Standalone | **Needs Implementation** | CS0693: `SmartDefault<T>` collides with outer `<T>` -- see Bug 1 below |
| P3 Standalone Class | Verified | `GenericMethodBase.Transform<TInput, TResult>` in Design.Domain |
| P4 Generic Standalone Class | **Verified (new code)** | `CacheStub<TKey, TValue>` with `Transform<TResult>` -- compiles (no `SmartDefault` in class pipeline) |
| P5 Inline Interface | **Needs Implementation for mixed arities** | Single-arity works, but `Run<T>()` + `Run<TIn, TOut>(TIn)` fails -- see Bug 4 |
| P6 Inline Class | Same as P5 |
| P8 Open Generic Interface | **Needs Implementation** | CS0693 + mixed arity issues |
| P9 Open Generic Class | **Verified (new code)** | `OpenGenericCacheTest` with `Transform<TResult>` -- compiles |

### Feature E-2: Methods with mixed type arities (same name, different type param counts)

This is a new sub-feature identified via Gap 31. Example: `Run<TReturn>()` and `Run<TInput, TReturn>(TInput input)`.

| Pattern | Status | Evidence |
|---|---|---|
| P1 Standalone | **Needs Implementation** | CS0305: handler only has `Of<TReturn>()` but impl calls `Of<TInput, TReturn>()` -- see Bug 4 |
| P2 Generic Standalone | **Needs Implementation** | Same as P1 + Bug 1 |
| P5 Inline Interface | **Needs Implementation** | CS0246: method-level type params leak into interceptor class scope -- see Bug 4 |
| P8 Open Generic Interface | **Needs Implementation** | Same as P5 |

### Feature F: Where clauses on class/interface type params

| Pattern | Status | Evidence |
|---|---|---|
| P1 Standalone (Must handle) | Verified | Closing generic satisfies constraints at instantiation |
| P2 Generic Standalone (Must propagate) | Verified | `NestedDictStub<TKey, TValue> where TKey : notnull` in `GenericStandaloneEdgeCaseTests.cs:252` |
| P3 Standalone Class (Must handle) | Verified | Closing generic satisfies constraints |
| P4 Generic Standalone Class (Must propagate) | Verified | `ConstrainedClassRepoStub<T> where T : class, new()` in `GenericStandaloneClassStubTests.cs:309` + new `CacheStub<TKey, TValue> where TKey : notnull` |
| P5 Inline Interface (Must handle) | Verified | Closing generic satisfies constraints |
| P6 Inline Class (Must handle) | Verified | Closing generic satisfies constraints |
| P8 Open Generic Interface (Must propagate) | Verified | `IClassRepository<T> where T : class` in `OpenGenericInlineStubTests.cs:282` |
| P9 Open Generic Class (Must propagate) | Verified | `OGCache<TKey, TValue> where TKey : notnull where TValue : new()` in `OpenGenericInlineStubTests.cs:204` |

### Feature G: Where clauses on method type params

| Pattern | Status | Evidence |
|---|---|---|
| P1 Standalone | Verified | `IConstrainedGenericService` and constraint coverage tests |
| P2 Generic Standalone | **Needs Implementation** | Blocked by Bug 1 (SmartDefault collision) -- constraints on method type params are correctly generated, but build fails due to SmartDefault |
| P3 Standalone Class | Verified | `GenericMethodBase.Transform<TInput, TResult> where TInput : class where TResult : new()` |
| P4 Generic Standalone Class | Verified | `GenericMethodRepositoryBase<TEntity>` has method constraints |
| P5 Inline Interface | Verified | Constraint coverage inline tests pass |
| P6 Inline Class | Verified | `GenericMethodBase` inline stubs tested |
| P8 Open Generic Interface | **Needs Implementation** | Blocked by Bug 1 (SmartDefault collision) |
| P9 Open Generic Class | **Verified (new code)** | `OpenGenericCacheTest` with `CacheBase<TKey, TValue>.Transform<TResult>` -- compiles |

### Feature H: Multiple where clauses on methods (all patterns)

Same as Feature G coverage. All patterns that support method-level type params handle multiple where clauses via the same `GetConstraintClauses` and `GetConstraintsForExplicitImpl` code paths. The constraint extraction in `SymbolHelpers.GetTypeParameterConstraints` handles all constraint types.

### Feature I: Generic delegates with multiple type params

| Pattern | Status | Evidence |
|---|---|---|
| P7 Inline Delegate (closed) | Verified | `Factory<T>` in Design.Domain, compiles |
| P7 Open Generic Delegate | Verified | `OGConverter<TIn, TOut, TResult> where TResult : class` in `OpenGenericInlineStubTests.cs:150` |

---

## Bugs Discovered

### Bug 1: SmartDefault<T> Type Parameter Name Collision (CS0693)

**Severity:** Compilation failure -- prevents usage of generic stubs with generic methods when the outer class has a type parameter named `T`.

**Affected Patterns:** P2 (FlatRenderer) and P8 (InlineRenderer) -- any pattern where the generated stub class is generic AND the target interface has generic methods.

**Not Affected:** P4/P9 (StandaloneClassRenderer, ClassRenderer) -- class stubs don't emit `SmartDefault` because they use `base.Method()` fallback. Also not affected when type parameter names don't include `T` (e.g., `TKey`, `TValue`, `TEntity`).

**Root Cause:**

Both `FlatRenderer.RenderSmartDefaultMethod()` (line 1913) and `InlineRenderer.RenderSmartDefaultMethod()` (line 1060) emit a helper method with a hardcoded type parameter name `T`:

```csharp
private static T SmartDefault<T>(string methodName)
```

When the outer class already has a type parameter named `T` (e.g., `GenericTransformServiceStub<T>`), this creates a CS0693 warning that is treated as an error (TreatWarningsAsErrors is enabled).

**Reproduction:**

```csharp
public interface IGenericTransformService<T> where T : class
{
    TResult Convert<TResult>(T input) where TResult : new();
}

[KnockOff]
public partial class GenericTransformServiceStub<T> : IGenericTransformService<T> where T : class { }
```

Build error: `CS0693: Type parameter 'T' has the same name as the type parameter from outer type 'GenericTransformServiceStub<T>'`

**Fix:**

Rename the `SmartDefault<T>` method's type parameter to a name that cannot collide with user-defined type parameters. Recommended: `SmartDefault<TSmartDefault>`.

**Files to Change:**

1. `src/Generator/Renderer/FlatRenderer.cs` -- `RenderSmartDefaultMethod()` (line ~1910-1934): Change `SmartDefault<T>` to `SmartDefault<TSmartDefault>` and update all internal references (`typeof(T)`, `(T)ctor.Invoke(null)`)
2. `src/Generator/Renderer/InlineRenderer.cs` -- `RenderSmartDefaultMethod()` (line ~1057-1081): Same change

**Verification Code:**

The failing Design.Stubs code is left in place at:
- `src/Design/Design.Stubs/StubPatterns/GenericTypeGapsVerification.cs:42` (P2: `GenericTransformServiceStub<T>`)
- `src/Design/Design.Stubs/StubPatterns/GenericTypeGapsVerification.cs:53` (P8: `OpenGenericTransformServiceTest`)

After the fix, these should compile without errors.

---

### Bug 2: `in` modifier stripped from indexer parameters (Gap 26)

**Severity:** Compilation failure -- prevents stubbing interfaces with `in` parameters on indexers.

**Affected Patterns:** All patterns that support indexers (P1, P2, P5, P6, P8, P9, P3, P4). The bug is in both the flat and inline pipelines.

**Error Codes:** CS0535 (does not implement interface member), CS0539 (member not found in interface)

**Reproduction:**

```csharp
public interface IGap26_InParameter
{
    int this[in int a] { get; }
}
```

Generated code: `int IGap26_InParameter.this[int a]` -- missing `in` modifier.

**Root Cause:**

The `in` modifier (`RefKind.In`) is stored in `ParameterInfo.RefKind` for indexer parameters, but is dropped at multiple points:

1. **Inline pipeline (`InlineModelBuilder.BuildIndexerImplementation`, line 792):**
   ```csharp
   var paramList = string.Join(", ", member.IndexerParameters.Select(p => $"{p.Type} {p.Name}"));
   ```
   This constructs `ParameterDeclarations` without consulting `p.RefKind`. Should use `$"{GetRefKindPrefix(p.RefKind)}{p.Type} {p.Name}"`.

2. **Flat pipeline (`FlatModelBuilder.BuildIndexerModels`, line 608-612 + `FlatRenderer.RenderIndexerImplementation`, line 2114):**
   The `FlatIndexerModel` only stores `KeyType` and `KeyParamName` as strings -- it does not store `RefKind` at all. The renderer then emits `this[{indexer.KeyType} {indexer.KeyParamName}]` without any modifier.

3. **Note:** Regular method parameters correctly use `FormatParameter(p)` which includes `GetRefKindPrefix(p.RefKind)`. This is an indexer-only bug because indexer parameter handling has a separate code path.

**Files to Change:**

1. `src/Generator/Builder/InlineModelBuilder.cs` line 792 -- Add `GetRefKindPrefix(p.RefKind)` to indexer parameter formatting
2. `src/Generator/Model/Flat/FlatIndexerModel.cs` -- Add `string KeyRefPrefix` field (or `RefKind KeyRefKind`)
3. `src/Generator/Builder/FlatModelBuilder.cs` lines 608-612 -- Populate the new field from `member.IndexerParameters[0].RefKind`
4. `src/Generator/Renderer/FlatRenderer.cs` line 2114 -- Use `{indexer.KeyRefPrefix}{indexer.KeyType} {indexer.KeyParamName}`
5. `src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs` -- May need `ParameterSignature` to include `in` for `InvokeGet`/`InvokeSet`
6. **Class pipelines** (`StandaloneClassRenderer`, `ClassRenderer`) -- Also check `RenderImplIndexerOverride` at lines using `indexer.ParameterDeclarations`

**Verification:**

Uncomment the `Gap26InlineTest` and `Gap26StandaloneKnockOff` stubs in `src/Tests/KnockOffTests/RocksGapReproductionTests.cs` and verify they compile.

---

### Bug 3: Generic methods with out/ref params in inline pattern (Gaps 27/28)

**Severity:** Compilation failure -- prevents stubbing interfaces with `out`/`ref` parameters on generic methods in inline pattern.

**Affected Patterns:** Inline patterns only (P5, P6, P8, P9). Standalone patterns (P1, P2) are NOT affected because `FlatModelBuilder.BuildGenericMethodHandler` correctly uses `FormatParameterWithRefKind(p)`.

**Error Code:** CS1615 (Argument may not be passed with the 'ref'/'out' keyword)

**Reproduction:**

```csharp
public interface IGap27_OutParameter
{
    void OutArgumentsWithGenerics<T1, T2>(T1 a, out T2 b);
}

[KnockOff<IGap27_OutParameter>]
public partial class Gap27InlineTest { }
```

The generated delegate has `void OutArgumentsWithGenericsDelegate(T1 a, T2 b)` (no `out`), but the explicit implementation calls `callCallback(a, out b)` which fails because the delegate doesn't accept `out`.

**Root Cause:**

In `InlineModelBuilder.BuildGenericMethodHandlerModel()` at line 491, the delegate signature is built without ref/out modifiers:

```csharp
foreach (var p in allParams)
{
    delegateParams.Add($"{p.Type} {p.Name}");  // BUG: Missing GetRefKindPrefix(p.RefKind)
}
```

Compare with `FlatModelBuilder.BuildGenericMethodHandler()` at line 1169 which correctly uses:
```csharp
delegateParams.Add(FormatParameterWithRefKind(p));
```

**Fix:**

Change line 491 in `InlineModelBuilder.cs` from:
```csharp
delegateParams.Add($"{p.Type} {p.Name}");
```
to:
```csharp
delegateParams.Add($"{GetRefKindPrefix(p.RefKind)}{p.Type} {p.Name}");
```

**Files to Change:**

1. `src/Generator/Builder/InlineModelBuilder.cs` line 491 -- Add `GetRefKindPrefix(p.RefKind)` to delegate parameter formatting

**Verification:**

Uncomment the `Gap27InlineTest` and `Gap28InlineTest` stubs in `src/Tests/KnockOffTests/RocksGapReproductionTests.cs` and verify they compile.

---

### Bug 4: Generic methods with mixed type arities fail (Gap 31)

**Severity:** Compilation failure -- prevents stubbing interfaces that have overloaded generic methods with different numbers of type parameters under the same name (e.g., `Run<T>()` and `Run<TIn, TOut>(TIn input)`).

**Affected Patterns:** ALL patterns (P1, P2, P5, P6, P8, P9). Class patterns (P3, P4, P9) are not affected because they use `base.Method()` fallback and don't generate generic method handlers.

**Error Codes:**
- Standalone: CS0305 (`Of<TReturn>()` requires 1 type argument but is invoked with 2)
- Inline: CS0246 (type parameter name like `TInput` used in interceptor class scope where it doesn't exist)

**Reproduction:**

```csharp
public interface IGap31_GenericMethods
{
    void Sprint<TReturn>();
    TReturn Run<TReturn>() where TReturn : new();
    TReturn Run<TInput, TReturn>(TInput input) where TReturn : new();
}
```

When methods with the same name have different numbers of type parameters, the generator only creates a single `Of<>()` method matching the first overload found.

**Root Cause:**

Both `FlatModelBuilder.BuildGenericMethodHandler()` and `InlineModelBuilder.BuildGenericMethodHandlerModel()` take the first generic overload's type parameters for the entire handler:

```csharp
var genericOverload = group.Overloads.First(o => o.IsGenericMethod);
var typeParams = genericOverload.TypeParameters.GetArray()!;
```

If `Run<TReturn>()` is the first overload, the handler gets `Of<TReturn>()`. When the explicit implementation for `Run<TInput, TReturn>(TInput input)` tries to call `Run.Of<TInput, TReturn>()`, it fails because only `Of<TReturn>()` (1 type param) exists.

**Existing infrastructure to leverage:**

The flat pipeline already has `FlatGenericMethodHandlerGroup` with `TypeArityGroups` (used for generic stub override handlers). This supports multiple `Of<>()` methods with different arities on the same interceptor. The same pattern needs to be applied to regular generic method handlers.

However, the inline pipeline has no equivalent -- `InlineGenericMethodHandlerModel` only supports a single set of type parameters.

**Scope of fix:**

This is the most complex bug. It requires:

1. **Standalone (FlatModelBuilder + FlatRenderer):** Refactor `FlatGenericMethodHandlerModel` to support multiple type arities (or reuse the existing `FlatGenericMethodHandlerGroup` pattern). The handler interceptor class needs multiple `Of<>()` methods (e.g., `Of<T>()` and `Of<T1, T2>()`) with separate dictionaries per arity.

2. **Inline (InlineModelBuilder + InlineRenderer):** Either:
   - (a) Extend `InlineGenericMethodHandlerModel` to support multiple type arities, or
   - (b) Generate multiple handler classes per method name (one per arity)

3. **Explicit interface implementation:** The `OfTypeAccess` expression (e.g., `.Of<TInput, TReturn>()`) already correctly includes all type params from each method. The issue is only that the handler doesn't support multiple arities.

**NOTE:** Methods with 2+ type params that all share the same arity (e.g., `Convert<TIn, TOut>` is the only generic overload of `Convert`) already work. This bug only manifests when the same method name has overloads with **different** type parameter counts.

**Files to Change:**

1. `src/Generator/Builder/FlatModelBuilder.cs` -- `BuildGenericMethodHandler()`: Group overloads by type param count; generate handler supporting multiple arities
2. `src/Generator/Model/Flat/FlatGenericMethodHandlerModel.cs` -- May need to become a multi-arity model or be replaced by `FlatGenericMethodHandlerGroup`
3. `src/Generator/Renderer/FlatRenderer.cs` -- `RenderGenericMethodHandler()`: Render multiple `Of<>()` methods, one per arity
4. `src/Generator/Builder/InlineModelBuilder.cs` -- `BuildGenericMethodHandlerModel()`: Same multi-arity support
5. `src/Generator/Model/Inline/InlineGenericMethodHandlerModel.cs` -- Extend or refactor for multi-arity
6. `src/Generator/Renderer/InlineRenderer.cs` -- `RenderGenericMethodHandler()`, `RenderTypedHandlerClass()`: Render per-arity handlers

**Verification:**

Uncomment the `Gap31InlineTest` and `Gap31StandaloneKnockOff` stubs in `src/Tests/KnockOffTests/RocksGapReproductionTests.cs` and verify they compile.

---

## Summary of Findings

### What Works (Verified)

The overwhelming majority of generic type features already work. The generator infrastructure properly handles:

1. **Multi-type-param interfaces and classes** across all patterns (P1-P9) -- including the newly verified P4 two-param case
2. **Where clauses** on both class-level and method-level type params, including all constraint types
3. **Constraint propagation** in open generic patterns (P2, P4, P8, P9)
4. **Generic delegates** with multiple type params and constraints (P7)
5. **Method-level type params** including multiple type params (`<TIn, TOut>`) when all overloads share the same arity
6. **Class-level + method-level type param interaction** on class stubs (P4, P9)
7. **Non-generic out/ref params** in all patterns (both standalone and inline)
8. **Regular `in` params on methods** -- only indexer params are affected

### What Needs Implementation

| Bug | Description | Severity | Patterns | Effort |
|-----|-------------|----------|----------|--------|
| Bug 1 | SmartDefault<T> collision | CS0693 | P2, P8 | Low -- rename in 2 files |
| Bug 2 | `in` on indexer params | CS0535/CS0539 | All | Low-Medium -- add RefKind to indexer param rendering |
| Bug 3 | out/ref on generic methods (inline) | CS1615 | P5, P6, P8, P9 | Low -- 1-line fix in InlineModelBuilder |
| Bug 4 | Mixed type arity generic methods | CS0305/CS0246 | P1, P2, P5, P8 | High -- multi-arity handler architecture |

### Risk Assessment

| Bug | Risk | Rationale |
|-----|------|-----------|
| Bug 1 | Low | Rename in generated helper method, no model/builder changes |
| Bug 2 | Low-Medium | Indexer parameter rendering is well-isolated, but touches 4+ files across pipelines |
| Bug 3 | Low | One-line fix, matching existing flat pipeline behavior |
| Bug 4 | Medium-High | Requires extending generic method handler architecture in both pipelines; existing `FlatGenericMethodHandlerGroup` provides a template but the inline pipeline needs new design |

---

## Implementation Plan

### Phase 1: Bug 1 - SmartDefault Type Parameter Collision (Low Risk)

**Files to change:**
1. `src/Generator/Renderer/FlatRenderer.cs` -- `RenderSmartDefaultMethod()`
2. `src/Generator/Renderer/InlineRenderer.cs` -- `RenderSmartDefaultMethod()`

**Change:** Replace `T` with `TSmartDefault` in the method declaration and body.

**Verification:** `dotnet build src/Design/Design.Stubs` should succeed for `GenericTransformServiceStub<T>` and `OpenGenericTransformServiceTest`.

### Phase 2: Bug 3 - Generic method out/ref delegate in inline (Low Risk)

**Files to change:**
1. `src/Generator/Builder/InlineModelBuilder.cs` line 491

**Change:** Add `GetRefKindPrefix(p.RefKind)` to delegate parameter formatting.

**Verification:** Uncomment `Gap27InlineTest` and `Gap28InlineTest` in `RocksGapReproductionTests.cs`, verify compilation.

### Phase 3: Bug 2 - `in` modifier on indexer parameters (Low-Medium Risk)

**Files to change:**
1. `src/Generator/Builder/InlineModelBuilder.cs` line 792
2. `src/Generator/Model/Flat/FlatIndexerModel.cs`
3. `src/Generator/Builder/FlatModelBuilder.cs` lines 608-612
4. `src/Generator/Renderer/FlatRenderer.cs` line 2114
5. `src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs` (verify `ParameterSignature` handles `in`)
6. Class pipeline files (verify `RenderImplIndexerOverride` handles `in`)

**Verification:** Uncomment `Gap26InlineTest` and `Gap26StandaloneKnockOff` in `RocksGapReproductionTests.cs`, verify compilation.

### Phase 4: Bug 4 - Mixed type arity generic methods (Medium-High Risk)

This is the most architecturally significant change. The developer should study the existing `FlatGenericMethodHandlerGroup` + `FlatGenericTypeArityGroup` pattern used for generic stub override handlers, as it already solves the same multi-arity problem.

**Approach for standalone (FlatModelBuilder + FlatRenderer):**
- Refactor `BuildGenericMethodHandler` to produce a multi-arity-aware handler
- Either extend `FlatGenericMethodHandlerModel` to hold `EquatableArray<TypeArityInfo>` or reuse the existing `FlatGenericMethodHandlerGroup` model
- Render multiple `Of<>()` methods, each with its own dictionary (matching the pattern in `RenderGenericStubOverrideHandlerGroup`)

**Approach for inline (InlineModelBuilder + InlineRenderer):**
- Extend `InlineGenericMethodHandlerModel` with a list of type arity groups
- Render multiple `Of<>()` methods and typed handler classes per arity

**Verification:** Uncomment `Gap31InlineTest` and `Gap31StandaloneKnockOff` in `RocksGapReproductionTests.cs`, verify compilation.

### Phase 5: Tests (Additive)

Write focused tests for all fixed bugs:
1. **Bug 1 tests**: P2 + method-level generics, P8 + method-level generics
2. **Bug 2 tests**: `in` modifier on indexer params in standalone and inline
3. **Bug 3 tests**: Generic methods with out/ref in inline -- verify callback invocation works
4. **Bug 4 tests**: Mixed arity generic methods -- verify `Of<T>()` and `Of<T1, T2>()` both work

### Phase 6: Documentation Update

Update the generic-type-gaps todo with completion status and add release notes.

---

## Architectural Verification

### Design Project Verification

| Item | Status | File Path |
|---|---|---|
| P4: `CacheStub<TKey, TValue>` two type params | **Verified (compiles)** | `src/Design/Design.Stubs/StubPatterns/GenericTypeGapsVerification.cs:30` |
| P9: `OpenGenericCacheTest` with `CacheBase<,>` | **Verified (compiles)** | `src/Design/Design.Stubs/StubPatterns/GenericTypeGapsVerification.cs:61` |
| P2: `GenericTransformServiceStub<T>` + generic methods | **Needs Implementation (Bug 1)** | `src/Design/Design.Stubs/StubPatterns/GenericTypeGapsVerification.cs:42` -- CS0693 |
| P8: `OpenGenericTransformServiceTest` + generic methods | **Needs Implementation (Bug 1)** | `src/Design/Design.Stubs/StubPatterns/GenericTypeGapsVerification.cs:53` -- CS0693 |
| Gap 26: `in` indexer params | **Needs Implementation (Bug 2)** | `src/Tests/KnockOffTests/RocksGapReproductionTests.cs` -- commented out stubs |
| Gap 27/28: Generic out/ref inline | **Needs Implementation (Bug 3)** | `src/Tests/KnockOffTests/RocksGapReproductionTests.cs` -- commented out stubs |
| Gap 31: Mixed arity generics | **Needs Implementation (Bug 4)** | `src/Tests/KnockOffTests/RocksGapReproductionTests.cs` -- commented out stubs |

**New Design.Domain types created:**
- `src/Design/Design.Domain/Abstractions/CacheBase.cs` -- Multi-type-param abstract class with method-level generics
- `src/Design/Design.Domain/Services/IGenericTransformService.cs` -- Generic interface with method-level type params

**Existing reproduction tests:** `src/Tests/KnockOffTests/RocksGapReproductionTests.cs` -- contains commented-out stubs for all bugs. Uncomment after fixes to verify compilation.

### Breaking Changes

**Minimal.** Bug 1 renames a private helper method's type parameter in generated code. Bugs 2/3/4 fix compilation failures, so they only add support that didn't exist before. No existing working behavior is changed.

### Nine-Pattern Analysis

| Pattern | Status | Notes |
|---|---|---|
| P1 Standalone | **Needs fix (Bug 4 only)** | Mixed arity generic methods fail |
| P2 Generic Standalone | **Needs fix (Bugs 1, 4)** | SmartDefault collision + mixed arity |
| P3 Standalone Class | All verified | No SmartDefault, no generic method handlers |
| P4 Generic Standalone Class | **Verified (new code)** | Two-type-param `CacheBase<TKey, TValue>` compiles correctly |
| P5 Inline Interface | **Needs fix (Bugs 2, 3, 4)** | `in` indexer, out/ref generic, mixed arity |
| P6 Inline Class | **Needs fix (Bugs 2, 3)** | `in` indexer, out/ref generic |
| P7 Inline Delegate | All verified | Multi-type-param delegates work |
| P8 Open Generic Interface | **Needs fix (Bugs 1, 2, 3, 4)** | All four bugs apply |
| P9 Open Generic Class | **Needs fix (Bug 2 only)** | `in` indexer params; no SmartDefault in class pipeline |

### Codebase Analysis

Files examined during this analysis:

**Generator pipeline (Bug 1 -- SmartDefault):**
- `src/Generator/Renderer/FlatRenderer.cs` -- `RenderSmartDefaultMethod()` (line ~1910-1934) -- **bug location**
- `src/Generator/Renderer/InlineRenderer.cs` -- `RenderSmartDefaultMethod()` (line ~1057-1081) -- **bug location**

**Generator pipeline (Bug 2 -- indexer `in` params):**
- `src/Generator/Models/InterfaceModels.cs` -- `ParameterInfo(Name, Type, RefKind)` -- stores RefKind.In correctly
- `src/Generator/Builder/InlineModelBuilder.cs` line 792 -- `$"{p.Type} {p.Name}"` drops `in` **bug location**
- `src/Generator/Builder/FlatModelBuilder.cs` lines 608-612 -- extracts `KeyType`/`KeyParamName` without RefKind **bug location**
- `src/Generator/Model/Flat/FlatIndexerModel.cs` -- no field for key parameter RefKind
- `src/Generator/Renderer/FlatRenderer.cs` line 2114 -- `this[{indexer.KeyType} {indexer.KeyParamName}]` **bug location**
- `src/Generator/Renderer/InlineRenderer.cs` line 1177 -- `this[{impl.ParameterDeclarations}]` (uses model data from InlineModelBuilder)
- `src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs` -- `ParameterSignature`, `InvokeGet`, `InvokeSet` methods
- `src/Generator/Renderer/StandaloneClassRenderer.cs` line 822 -- `this[{indexer.ParameterDeclarations}]` (class pipeline)
- `src/Generator/Renderer/ClassRenderer.cs` line 790 -- `this[{indexer.ParameterDeclarations}]` (class pipeline)

**Generator pipeline (Bug 3 -- generic method out/ref delegate):**
- `src/Generator/Builder/InlineModelBuilder.cs` line 491 -- `$"{p.Type} {p.Name}"` missing RefKind **bug location**
- `src/Generator/Builder/FlatModelBuilder.cs` line 1169 -- `FormatParameterWithRefKind(p)` -- correct, not affected
- `src/Generator/Builder/FlatModelBuilder.cs` line 1618 -- `FormatParameterWithRefKind` definition

**Generator pipeline (Bug 4 -- mixed type arity):**
- `src/Generator/Builder/FlatModelBuilder.cs` line 1120 -- `group.Overloads.First(o => o.IsGenericMethod)` takes only first arity **bug location**
- `src/Generator/Builder/InlineModelBuilder.cs` line 462 -- same pattern **bug location**
- `src/Generator/Model/Flat/FlatGenericMethodHandlerModel.cs` -- single `TypeParameterNames` per handler
- `src/Generator/Model/Flat/FlatGenericMethodHandlerGroup.cs` -- multi-arity model (existing, for stub overrides)
- `src/Generator/Renderer/FlatRenderer.cs` -- `RenderGenericMethodHandler` (single arity) vs `RenderGenericStubOverrideHandlerGroup` (multi-arity template)
- `src/Generator/Model/Inline/InlineGenericMethodHandlerModel.cs` -- single `TypeParameterNames` per handler
- `src/Generator/Renderer/InlineRenderer.cs` -- `RenderGenericMethodHandler`, `RenderTypedHandlerClass`

**Tests:**
- `src/Tests/KnockOffTests/RocksGapReproductionTests.cs` -- Reproduction tests for Gaps 25-31
- `src/Tests/KnockOffTests/GenericMethodTests.cs` -- Existing multi-type-param tests (same arity)
- `src/Tests/KnockOffTests/TestInterfaces.cs` -- `IGenericMethodService` with `Convert<TIn, TOut>`

### Test Strategy

1. **Compilation verification** (all phases): Uncomment reproduction stubs in `RocksGapReproductionTests.cs`
2. **Runtime behavior tests** (Phase 5): New tests exercising Return/Call/Verify on each fixed combination
3. **Regression safety**: No existing tests will be modified. All new tests are additive.
4. **Existing test baseline**: `dotnet test src/KnockOff.sln` must pass before and after all changes

### Edge Cases

1. **SmartDefault name collision**: Only manifests when outer class type param is literally `T`. Names like `TKey` don't collide.
2. **`in` on multi-param indexers**: `this[in int a, string b]` -- `in` applies per-parameter, not to the whole indexer.
3. **out/ref on generic methods in standalone**: Already works via `FormatParameterWithRefKind`. Bug is inline-only.
4. **Mixed arity with constraints**: `Run<T>() where T : new()` and `Run<TIn, TOut>(TIn input) where TOut : class` -- each arity needs its own constraint clauses on the typed handler.
5. **Single method with 2+ type params (no arity conflict)**: Already works (e.g., `Convert<TIn, TOut>`). Bug 4 only affects mixed arities.
6. **Method with void + non-void overloads at different arities**: e.g., `void Sprint<T>()` and `TReturn Run<TInput, TReturn>(TInput input)` -- the handler's return type and delegate differ per arity.

---

## Developer Review

**Status:** Not Started
**Reviewed:** --

**Concerns:** --

---

## Implementation Contract

*(To be created after developer review)*

---

## Implementation Progress

*(To be filled during implementation)*

---

## Completion Evidence

*(To be filled after implementation)*
