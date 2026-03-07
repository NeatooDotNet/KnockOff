# Generic Type Gaps - Validation and Fix Plan

**Date:** 2026-02-08
**Related Todo:** [Generic Type Gaps](../todos/generic-type-gaps.md)
**Status:** Verified
**Last Updated:** 2026-02-09

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
| P3 Standalone Class | **Needs Implementation** | `StandaloneClassModelBuilder.BuildGenericMethodHandlerModel()` uses `genericMembers[0]` first-arity pattern -- see Bug 4 |
| P4 Generic Standalone Class | **Needs Implementation** | Same as P3 |
| P5 Inline Interface | **Needs Implementation** | CS0246: method-level type params leak into interceptor class scope -- see Bug 4 |
| P6 Inline Class | **Needs Implementation** | `ClassModelBuilder.BuildGenericMethodHandlerModel()` uses `genericMembers[0]` first-arity pattern -- see Bug 4 |
| P8 Open Generic Interface | **Needs Implementation** | Same as P5 |
| P9 Open Generic Class | **Needs Implementation** | Same as P6 |

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

**Flat pipeline:**
1. `src/Generator/Model/Flat/FlatIndexerModel.cs` -- Add `string KeyRefPrefix` field (or `RefKind KeyRefKind`)
2. `src/Generator/Builder/FlatModelBuilder.cs` lines 608-612 -- Populate the new field from `member.IndexerParameters[0].RefKind`
3. `src/Generator/Renderer/FlatRenderer.cs` line 2114 -- Use `{indexer.KeyRefPrefix}{indexer.KeyType} {indexer.KeyParamName}`
4. `src/Generator/Renderer/Shared/ModelAdapters.cs` line 308 -- `ToUnifiedIndexerModel(FlatIndexerModel)` builds `ParameterSignature` as `$"{indexer.KeyType} {indexer.KeyParamName}"` without ref prefix. Must include the new `KeyRefPrefix` so `InvokeGet`/`InvokeSet` signatures match.

**Inline pipeline:**
5. `src/Generator/Builder/InlineModelBuilder.cs` line 266 -- `keyType` tuple form `$"{p.Type} {p.Name}"` needs `GetRefKindPrefix(p.RefKind)` prefix
6. `src/Generator/Builder/InlineModelBuilder.cs` line 272 -- `paramSig` `$"{p.Type} {p.Name}"` needs `GetRefKindPrefix(p.RefKind)` prefix
7. `src/Generator/Builder/InlineModelBuilder.cs` line 792 -- `BuildIndexerImplementation` paramList needs `GetRefKindPrefix(p.RefKind)` prefix

**Class pipeline (both ClassModelBuilder and StandaloneClassModelBuilder):**
8. `src/Generator/Builder/ClassModelBuilder.cs` line 373 -- `BuildIndexerModel` keyType tuple form `$"{p.Type} {p.Name}"` needs `GetRefKindPrefix(p.RefKind)` prefix
9. `src/Generator/Builder/ClassModelBuilder.cs` line 375 -- `BuildIndexerModel` paramSig `$"{p.Type} {p.Name}"` needs `GetRefKindPrefix(p.RefKind)` prefix
10. `src/Generator/Builder/ClassModelBuilder.cs` line 447 -- `BuildImplIndexerModel` paramList `$"{p.Type} {p.Name}"` needs `GetRefKindPrefix(p.RefKind)` prefix
11. `src/Generator/Builder/StandaloneClassModelBuilder.cs` line 446 -- `BuildIndexerModel` keyType tuple form needs `GetRefKindPrefix(p.RefKind)` prefix
12. `src/Generator/Builder/StandaloneClassModelBuilder.cs` line 448 -- `BuildIndexerModel` paramSig needs `GetRefKindPrefix(p.RefKind)` prefix
13. `src/Generator/Builder/StandaloneClassModelBuilder.cs` line 521 -- `BuildImplIndexerModel` paramList needs `GetRefKindPrefix(p.RefKind)` prefix

**Shared:**
14. `src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs` -- Verify `ParameterSignature` handles `in` for `InvokeGet`/`InvokeSet`

**Verification:**

Uncomment the `Gap26InlineTest` and `Gap26StandaloneKnockOff` stubs in `src/Tests/KnockOffTests/RocksGapReproductionTests.cs` and verify they compile.

---

### Bug 3: Generic methods with out/ref params in inline and class patterns (Gaps 27/28)

**Severity:** Compilation failure -- prevents stubbing interfaces/classes with `out`/`ref` parameters on generic methods in inline and class patterns.

**Affected Patterns:** Inline patterns (P5, P6, P8, P9) AND class patterns (P3, P4). Standalone interface patterns (P1, P2) are NOT affected because `FlatModelBuilder.BuildGenericMethodHandler` correctly uses `FormatParameterWithRefKind(p)`. Class patterns (P3, P4, P6, P9) ARE affected because `ClassModelBuilder.BuildGenericMethodHandlerModel()` and `StandaloneClassModelBuilder.BuildGenericMethodHandlerModel()` have the same bug -- they build delegate params without RefKind.

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
2. `src/Generator/Builder/ClassModelBuilder.cs` line 632 -- Same fix: `delegateParams.Add($"{p.Type} {p.Name}")` needs `GetRefKindPrefix(p.RefKind)` prefix
3. `src/Generator/Builder/StandaloneClassModelBuilder.cs` line 708 -- Same fix: `delegateParams.Add($"{p.Type} {p.Name}")` needs `GetRefKindPrefix(p.RefKind)` prefix

**Verification:**

Uncomment the `Gap27InlineTest` and `Gap28InlineTest` stubs in `src/Tests/KnockOffTests/RocksGapReproductionTests.cs` and verify they compile. Additionally, verify class pattern stubs with out/ref generic methods compile correctly.

---

### Bug 4: Generic methods with mixed type arities fail (Gap 31)

**Severity:** Compilation failure -- prevents stubbing interfaces that have overloaded generic methods with different numbers of type parameters under the same name (e.g., `Run<T>()` and `Run<TIn, TOut>(TIn input)`).

**Affected Patterns:** ALL patterns that generate generic method handlers (P1, P2, P3, P4, P5, P6, P8, P9). Class patterns (P3, P4, P6, P9) ARE affected -- they DO generate `Of<T>()` handlers via `ClassModelBuilder.BuildGenericMethodHandlerModel()` and `StandaloneClassModelBuilder.BuildGenericMethodHandlerModel()`, both of which use the same `genericMembers[0]` first-arity-only pattern. The `base.Method()` fallback is only the unconfigured path; the `Of<T>()` handler is still generated and used for configured callbacks.

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

This is the most complex bug. It requires changes across all four builder/renderer pipelines (flat, inline, class, standalone class).

1. **Standalone (FlatModelBuilder + FlatRenderer):** Refactor `FlatGenericMethodHandlerModel` to support multiple type arities (or reuse the existing `FlatGenericMethodHandlerGroup` pattern). The handler interceptor class needs multiple `Of<>()` methods (e.g., `Of<T>()` and `Of<T1, T2>()`) with separate dictionaries per arity.

2. **Inline and Class pipelines (InlineModelBuilder + InlineRenderer, ClassModelBuilder + StandaloneClassModelBuilder + ClassRenderer):** All use `InlineGenericMethodHandlerModel`. The recommended approach is option (a): extend `InlineGenericMethodHandlerModel` to hold an `EquatableArray<InlineGenericTypeArityGroup>` (mirroring the `FlatGenericTypeArityGroup` pattern). Each arity group gets its own:
   - `Of<>()` method with the correct number of type parameters
   - Dictionary (`Dictionary<KeyType, object>`) keyed by the appropriate tuple of `System.Type`
   - `TypedHandler` class with delegate and tracking
   - Delegate signature matching that arity's parameter types

   The existing single-arity fields (`TypeParameterNames`, `KeyType`, `KeyConstruction`, `MethodConstraintClauses`, `TypedHandlerClassName`, `DelegateSignature`) become properties of the per-arity group record rather than the top-level model. The top-level model retains `InterceptorClassName`, `MethodName`, `StubClassName`, `InterfaceTypeParameterList`, `InterfaceConstraintClauses`, and the array of arity groups.

   The renderer (`ClassRenderer.RenderClassGenericMethodHandler`, `InlineRenderer.RenderGenericMethodHandler`) iterates over arity groups and emits one `Of<>()` method + one dictionary + one typed handler class per group.

   When there is only one arity (the common case), this reduces to the current behavior with a single-element array.

3. **Explicit interface/override implementation:** The `OfTypeAccess` expression (e.g., `.Of<TInput, TReturn>()`) already correctly includes all type params from each method. The issue is only that the handler doesn't support multiple arities.

**NOTE:** Methods with 2+ type params that all share the same arity (e.g., `Convert<TIn, TOut>` is the only generic overload of `Convert`) already work. This bug only manifests when the same method name has overloads with **different** type parameter counts.

**Files to Change:**

1. `src/Generator/Builder/FlatModelBuilder.cs` -- `BuildGenericMethodHandler()`: Group overloads by type param count; generate handler supporting multiple arities
2. `src/Generator/Model/Flat/FlatGenericMethodHandlerModel.cs` -- May need to become a multi-arity model or be replaced by `FlatGenericMethodHandlerGroup`
3. `src/Generator/Renderer/FlatRenderer.cs` -- `RenderGenericMethodHandler()`: Render multiple `Of<>()` methods, one per arity
4. `src/Generator/Builder/InlineModelBuilder.cs` -- `BuildGenericMethodHandlerModel()` line 462: Same multi-arity support
5. `src/Generator/Model/Inline/InlineGenericMethodHandlerModel.cs` -- Refactor for multi-arity: add `EquatableArray<InlineGenericTypeArityGroup>`, move per-arity fields into the new record
6. `src/Generator/Renderer/InlineRenderer.cs` -- `RenderGenericMethodHandler()`, `RenderTypedHandlerClass()`: Render per-arity handlers
7. `src/Generator/Builder/ClassModelBuilder.cs` -- `BuildGenericMethodHandlerModel()` line 600: Same `genericMembers[0]` first-arity-only bug; needs multi-arity grouping
8. `src/Generator/Builder/StandaloneClassModelBuilder.cs` -- `BuildGenericMethodHandlerModel()` line 676: Same `genericMembers[0]` first-arity-only bug; needs multi-arity grouping
9. `src/Generator/Renderer/ClassRenderer.cs` -- `RenderClassGenericMethodHandler()`: Render per-arity handlers (same model change as inline)

**Verification:**

Uncomment the `Gap31InlineTest` and `Gap31StandaloneKnockOff` stubs in `src/Tests/KnockOffTests/RocksGapReproductionTests.cs` and verify they compile. Class pattern stubs with mixed-arity generic methods should also compile.

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
| Bug 2 | `in` on indexer params | CS0535/CS0539 | All | Low-Medium -- add RefKind to indexer param rendering across 4 pipelines |
| Bug 3 | out/ref on generic method delegates | CS1615 | P3, P4, P5, P6, P8, P9 | Low -- same 1-line fix in 3 model builders |
| Bug 4 | Mixed type arity generic methods | CS0305/CS0246 | P1, P2, P3, P4, P5, P6, P8, P9 | High -- multi-arity handler architecture across all 4 pipelines |

### Risk Assessment

| Bug | Risk | Rationale |
|-----|------|-----------|
| Bug 1 | Low | Rename in generated helper method, no model/builder changes |
| Bug 2 | Low-Medium | Indexer parameter rendering is well-isolated, but touches 14 locations across 4 pipelines |
| Bug 3 | Low | Same 1-line fix in 3 model builders, matching existing flat pipeline behavior |
| Bug 4 | Medium-High | Requires extending generic method handler architecture in all 4 pipelines; existing `FlatGenericMethodHandlerGroup` provides a template; inline/class pipelines share `InlineGenericMethodHandlerModel` so the model change applies to both |

---

## Implementation Plan

### Phase 1: Bug 1 - SmartDefault Type Parameter Collision (Low Risk)

**Files to change:**
1. `src/Generator/Renderer/FlatRenderer.cs` -- `RenderSmartDefaultMethod()`
2. `src/Generator/Renderer/InlineRenderer.cs` -- `RenderSmartDefaultMethod()`

**Change:** Replace `T` with `TSmartDefault` in the method declaration and body.

**Verification:** `dotnet build src/Design/Design.Stubs` should succeed for `GenericTransformServiceStub<T>` and `OpenGenericTransformServiceTest`.

### Phase 2: Bug 3 - Generic method out/ref delegate in inline and class pipelines (Low Risk)

**Files to change:**
1. `src/Generator/Builder/InlineModelBuilder.cs` line 491
2. `src/Generator/Builder/ClassModelBuilder.cs` line 632
3. `src/Generator/Builder/StandaloneClassModelBuilder.cs` line 708

**Change:** Add `GetRefKindPrefix(p.RefKind)` to delegate parameter formatting in all three `BuildGenericMethodHandlerModel` methods.

**Verification:** Uncomment `Gap27InlineTest` and `Gap28InlineTest` in `RocksGapReproductionTests.cs`, verify compilation. Also verify class pattern stubs with out/ref generic methods compile.

### Phase 3: Bug 2 - `in` modifier on indexer parameters (Low-Medium Risk)

**Files to change (14 locations across 4 pipelines):**

*Flat pipeline:*
1. `src/Generator/Model/Flat/FlatIndexerModel.cs` -- Add `KeyRefPrefix` field
2. `src/Generator/Builder/FlatModelBuilder.cs` lines 608-612 -- Populate `KeyRefPrefix`
3. `src/Generator/Renderer/FlatRenderer.cs` line 2114 -- Use `KeyRefPrefix`
4. `src/Generator/Renderer/Shared/ModelAdapters.cs` line 308 -- Include `KeyRefPrefix` in `ParameterSignature`

*Inline pipeline:*
5. `src/Generator/Builder/InlineModelBuilder.cs` line 266 -- keyType tuple form
6. `src/Generator/Builder/InlineModelBuilder.cs` line 272 -- paramSig
7. `src/Generator/Builder/InlineModelBuilder.cs` line 792 -- BuildIndexerImplementation paramList

*Class pipeline:*
8. `src/Generator/Builder/ClassModelBuilder.cs` line 373 -- BuildIndexerModel keyType tuple form
9. `src/Generator/Builder/ClassModelBuilder.cs` line 375 -- BuildIndexerModel paramSig
10. `src/Generator/Builder/ClassModelBuilder.cs` line 447 -- BuildImplIndexerModel paramList
11. `src/Generator/Builder/StandaloneClassModelBuilder.cs` line 446 -- BuildIndexerModel keyType tuple form
12. `src/Generator/Builder/StandaloneClassModelBuilder.cs` line 448 -- BuildIndexerModel paramSig
13. `src/Generator/Builder/StandaloneClassModelBuilder.cs` line 521 -- BuildImplIndexerModel paramList

*Shared:*
14. `src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs` -- Verify `ParameterSignature` handles `in`

**Verification:** Uncomment `Gap26InlineTest` and `Gap26StandaloneKnockOff` in `RocksGapReproductionTests.cs`, verify compilation.

### Phase 4: Bug 4 - Mixed type arity generic methods (Medium-High Risk)

This is the most architecturally significant change. The developer should study the existing `FlatGenericMethodHandlerGroup` + `FlatGenericTypeArityGroup` pattern used for generic stub override handlers, as it already solves the same multi-arity problem.

**Approach for standalone (FlatModelBuilder + FlatRenderer):**
- Refactor `BuildGenericMethodHandler` to produce a multi-arity-aware handler
- Either extend `FlatGenericMethodHandlerModel` to hold `EquatableArray<TypeArityInfo>` or reuse the existing `FlatGenericMethodHandlerGroup` model
- Render multiple `Of<>()` methods, each with its own dictionary (matching the pattern in `RenderGenericStubOverrideHandlerGroup`)

**Approach for inline and class pipelines (recommended -- option A):**

Extend `InlineGenericMethodHandlerModel` to hold an `EquatableArray<InlineGenericTypeArityGroup>`. Create a new record `InlineGenericTypeArityGroup` (mirroring `FlatGenericTypeArityGroup`) with these fields:
- `TypeParameterNames` (e.g., "T" or "TIn, TOut")
- `TypeParameterCount` (1, 2, etc.)
- `KeyType` (e.g., "global::System.Type" or "(global::System.Type, global::System.Type)")
- `KeyConstruction` (e.g., "typeof(T)" or "(typeof(TIn), typeof(TOut))")
- `ConstraintClauses` (where clauses for this arity)
- `TypedHandlerClassName` (e.g., "RunTypedHandler" or "RunTypedHandler2")
- `DelegateSignature` (delegate for this arity's overloads)
- `IsVoid`, `ReturnType`
- `NonGenericParameters`, `LastCallArgType`, `LastCallArgsType`

Move per-arity fields out of `InlineGenericMethodHandlerModel` and into the new record. The top-level model keeps: `InterceptorClassName`, `MethodName`, `StubClassName`, `InterfaceTypeParameterList`, `InterfaceConstraintClauses`, and `EquatableArray<InlineGenericTypeArityGroup> ArityGroups`.

Renderers iterate over arity groups:
- `ClassRenderer.RenderClassGenericMethodHandler()`: Emit one dictionary + one `Of<>()` method + one typed handler class per arity group
- `InlineRenderer.RenderGenericMethodHandler()`: Same pattern

Builders group generic overloads by type parameter count:
- `InlineModelBuilder.BuildGenericMethodHandlerModel()`: Group by `TypeParameters.Count`, create one `InlineGenericTypeArityGroup` per group
- `ClassModelBuilder.BuildGenericMethodHandlerModel()`: Same grouping
- `StandaloneClassModelBuilder.BuildGenericMethodHandlerModel()`: Same grouping

When there is only one arity (the common case), this reduces to the current behavior with a single-element array. No existing generated code changes for single-arity cases.

**Files to change:** See Bug 4 "Files to Change" section above (9 files).

**Verification:** Uncomment `Gap31InlineTest` and `Gap31StandaloneKnockOff` in `RocksGapReproductionTests.cs`, verify compilation. Also verify class pattern stubs with mixed-arity generic methods compile.

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
| P3 Standalone Class | **Needs fix (Bugs 2, 3, 4)** | `in` indexer, out/ref generic delegate, mixed arity |
| P4 Generic Standalone Class | **Needs fix (Bugs 2, 3, 4)** | Two-type-param `CacheBase<TKey, TValue>` compiles for single-arity, but out/ref generic delegate and mixed arity bugs apply |
| P5 Inline Interface | **Needs fix (Bugs 2, 3, 4)** | `in` indexer, out/ref generic, mixed arity |
| P6 Inline Class | **Needs fix (Bugs 2, 3, 4)** | `in` indexer, out/ref generic, mixed arity |
| P7 Inline Delegate | All verified | Multi-type-param delegates work |
| P8 Open Generic Interface | **Needs fix (Bugs 1, 2, 3, 4)** | All four bugs apply |
| P9 Open Generic Class | **Needs fix (Bugs 2, 3, 4)** | `in` indexer, out/ref generic delegate, mixed arity |

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

**Generator pipeline (Bug 2 -- indexer `in` params, class pipeline additions):**
- `src/Generator/Builder/ClassModelBuilder.cs` lines 373, 375 -- `BuildIndexerModel` keyType and paramSig missing RefKind **bug location**
- `src/Generator/Builder/ClassModelBuilder.cs` line 447 -- `BuildImplIndexerModel` paramList missing RefKind **bug location**
- `src/Generator/Builder/StandaloneClassModelBuilder.cs` lines 446, 448 -- `BuildIndexerModel` keyType and paramSig missing RefKind **bug location**
- `src/Generator/Builder/StandaloneClassModelBuilder.cs` line 521 -- `BuildImplIndexerModel` paramList missing RefKind **bug location**
- `src/Generator/Builder/InlineModelBuilder.cs` lines 266, 272 -- `BuildInlineIndexerModel` keyType and paramSig missing RefKind **bug location**
- `src/Generator/Renderer/Shared/ModelAdapters.cs` line 308 -- `ParameterSignature` missing RefKind in flat indexer adapter **bug location**

**Generator pipeline (Bug 3 -- generic method out/ref delegate):**
- `src/Generator/Builder/InlineModelBuilder.cs` line 491 -- `$"{p.Type} {p.Name}"` missing RefKind **bug location**
- `src/Generator/Builder/ClassModelBuilder.cs` line 632 -- `$"{p.Type} {p.Name}"` missing RefKind **bug location**
- `src/Generator/Builder/StandaloneClassModelBuilder.cs` line 708 -- `$"{p.Type} {p.Name}"` missing RefKind **bug location**
- `src/Generator/Builder/FlatModelBuilder.cs` line 1169 -- `FormatParameterWithRefKind(p)` -- correct, not affected
- `src/Generator/Builder/FlatModelBuilder.cs` line 1618 -- `FormatParameterWithRefKind` definition

**Generator pipeline (Bug 4 -- mixed type arity):**
- `src/Generator/Builder/FlatModelBuilder.cs` line 1120 -- `group.Overloads.First(o => o.IsGenericMethod)` takes only first arity **bug location**
- `src/Generator/Builder/InlineModelBuilder.cs` line 462 -- same pattern **bug location**
- `src/Generator/Builder/ClassModelBuilder.cs` line 600 -- `genericMembers[0]` takes only first arity **bug location**
- `src/Generator/Builder/StandaloneClassModelBuilder.cs` line 676 -- `genericMembers[0]` takes only first arity **bug location**
- `src/Generator/Model/Flat/FlatGenericMethodHandlerModel.cs` -- single `TypeParameterNames` per handler
- `src/Generator/Model/Flat/FlatGenericMethodHandlerGroup.cs` -- multi-arity model (existing, for stub overrides)
- `src/Generator/Renderer/FlatRenderer.cs` -- `RenderGenericMethodHandler` (single arity) vs `RenderGenericStubOverrideHandlerGroup` (multi-arity template)
- `src/Generator/Model/Inline/InlineGenericMethodHandlerModel.cs` -- single `TypeParameterNames` per handler
- `src/Generator/Renderer/InlineRenderer.cs` -- `RenderGenericMethodHandler`, `RenderTypedHandlerClass`
- `src/Generator/Renderer/ClassRenderer.cs` -- `RenderClassGenericMethodHandler` (single arity, same model as inline)

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
2. **`in` on multi-param indexers**: `this[in int a, string b]` -- `in` applies per-parameter, not to the whole indexer. All indexer parameter formatting must be per-parameter, not just the first.
3. **out/ref on generic methods in standalone**: Already works via `FormatParameterWithRefKind`. Bug 3 affects inline and class pipelines only.
4. **Mixed arity with constraints**: `Run<T>() where T : new()` and `Run<TIn, TOut>(TIn input) where TOut : class` -- each arity needs its own constraint clauses on the typed handler.
5. **Single method with 2+ type params (no arity conflict)**: Already works (e.g., `Convert<TIn, TOut>`). Bug 4 only affects mixed arities.
6. **Method with void + non-void overloads at different arities**: e.g., `void Sprint<T>()` and `TReturn Run<TInput, TReturn>(TInput input)` -- the handler's return type and delegate differ per arity.
7. **Bug 3 + Bug 4 compound**: Mixed-arity generic methods that also have `out`/`ref` params. The multi-arity handler's delegate must include RefKind for each arity's parameters. Since Bug 3 is fixed before Bug 4 in the phase ordering, the fix will naturally carry forward.
8. **Class pattern base.Method() vs. Of<T>() interaction**: Class stubs generate BOTH a `base.Method()` fallback AND an `Of<T>()` handler. The generated override checks `Of<T>()` first, falls back to `base.Method()` if unconfigured. Both paths must work correctly for mixed arities.

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-02-08 (re-reviewed after architect addressed all 5 concerns)

### My Understanding of This Plan

**Core Change:** Fix 4 bugs related to generic type handling in the source generator: SmartDefault type collision, `in` on indexer params, out/ref on generic method delegates, and mixed-arity generic method support.

**User-Facing API:** No API changes. These are compilation fixes -- currently broken patterns will compile and work after the fixes.

**Internal Changes:** Renderer changes (SmartDefault rename), model builder changes (indexer/delegate parameter formatting, multi-arity handler architecture), and model additions (multi-arity support in handler models).

**Patterns Affected:** All 9 patterns, with different bugs affecting different subsets.

### Codebase Investigation

**Files Examined:**
- `src/Generator/Renderer/FlatRenderer.cs:1910-1934` -- Confirmed `SmartDefault<T>` hardcoded type param (Bug 1)
- `src/Generator/Renderer/InlineRenderer.cs:1057-1081` -- Confirmed `SmartDefault<T>` hardcoded type param (Bug 1)
- `src/Generator/Builder/InlineModelBuilder.cs:491` -- Confirmed missing `GetRefKindPrefix` in delegate params (Bug 3)
- `src/Generator/Builder/InlineModelBuilder.cs:272,792` -- Confirmed missing `GetRefKindPrefix` in indexer params (Bug 2)
- `src/Generator/Builder/FlatModelBuilder.cs:608-612` -- Confirmed `KeyType`/`KeyParamName` extracted without RefKind (Bug 2)
- `src/Generator/Builder/FlatModelBuilder.cs:1120` -- Confirmed `First(o => o.IsGenericMethod)` takes only first arity (Bug 4)
- `src/Generator/Builder/InlineModelBuilder.cs:462` -- Confirmed same first-arity-only pattern (Bug 4)
- `src/Generator/Renderer/Shared/ModelAdapters.cs:308` -- Confirmed `ParameterSignature` built without RefKind for flat indexer
- `src/Generator/Model/Flat/FlatIndexerModel.cs` -- No field for key parameter RefKind
- `src/Generator/Model/Flat/FlatGenericMethodHandlerModel.cs` -- Single `TypeParameterNames` per handler
- `src/Generator/Model/Flat/FlatGenericMethodHandlerGroup.cs` -- Multi-arity model exists (template for Bug 4 fix)
- `src/Generator/Model/Inline/InlineGenericMethodHandlerModel.cs` -- Single `TypeParameterNames` per handler
- `src/Generator/Builder/ClassModelBuilder.cs:600,632` -- Has same first-arity and missing-RefKind bugs
- `src/Generator/Builder/StandaloneClassModelBuilder.cs:676,708` -- Has same first-arity and missing-RefKind bugs
- `src/Generator/Renderer/ClassRenderer.cs:1067` -- Class stubs DO use `Of<T>()` pattern
- `src/Generator/Renderer/StandaloneClassRenderer.cs:117` -- Uses `ClassRenderer.RenderClassGenericMethodHandler`
- `src/Design/Design.Stubs/StubPatterns/GenericTypeGapsVerification.cs` -- Confirmed 4 stubs exist (2 compile, 2 fail)
- `src/Design/Design.Domain/Abstractions/CacheBase.cs` -- Confirmed two-type-param abstract class
- `src/Design/Design.Domain/Services/IGenericTransformService.cs` -- Confirmed generic interface with method-level type params
- `src/Tests/KnockOffTests/RocksGapReproductionTests.cs` -- Confirmed reproduction tests with commented-out stubs

**Searches Performed:**
- Searched `ParameterSignature` across generator: found 6 build sites, flat indexer adapter at `ModelAdapters.cs:308` missing RefKind
- Searched `BuildImplIndexerModel` in class model builders: both `ClassModelBuilder:447` and `StandaloneClassModelBuilder:521` build `paramList` without RefKind
- Searched `RenderGenericMethodHandler` in class renderers: both `ClassRenderer` and `StandaloneClassRenderer` render generic method handlers using `InlineGenericMethodHandlerModel`
- Searched `BuildGenericMethodHandlerModel` in class model builders: both have the same `genericMembers[0]` first-arity pattern

**Design.Stubs Verification:**
- P4: `CacheStub<TKey, TValue>` at `GenericTypeGapsVerification.cs:30` -- confirmed compiles (Verified)
- P2: `GenericTransformServiceStub<T>` at `GenericTypeGapsVerification.cs:42` -- confirmed fails with CS0693 (Needs Implementation)
- P8: `OpenGenericTransformServiceTest` at `GenericTypeGapsVerification.cs:53` -- confirmed fails with CS0693 (Needs Implementation)
- P9: `OpenGenericCacheTest` at `GenericTypeGapsVerification.cs:61` -- confirmed compiles (Verified)
- Gap reproduction tests at `RocksGapReproductionTests.cs` -- confirmed commented-out stubs for Bugs 2/3/4

**Discrepancies Found:**
- Plan says class patterns (P3, P4, P6, P9) are not affected by Bug 3 or Bug 4. **Code shows otherwise.** See Concern 1 below.

### Concerns

#### 1. Bug 3 and Bug 4 Scope Undercount -- Class Patterns ARE Affected

**Category:** Correctness / Completeness

**Details:** The plan states for Bug 3: "Affected Patterns: Inline patterns only (P5, P6, P8, P9)." For Bug 4: "Class patterns (P3, P4, P9) are not affected because they use `base.Method()` fallback and don't generate generic method handlers."

This is incorrect. I verified that:
- `ClassModelBuilder.BuildGenericMethodHandlerModel()` (line 632) has `delegateParams.Add($"{p.Type} {p.Name}")` -- same Bug 3 issue
- `StandaloneClassModelBuilder.BuildGenericMethodHandlerModel()` (line 708) has same Bug 3 issue
- Both class model builders use `genericMembers[0]` to take the first generic member's type parameters (ClassModelBuilder:600, StandaloneClassModelBuilder:676) -- same Bug 4 issue
- Both class renderers call `ClassRenderer.RenderClassGenericMethodHandler()` which renders `Of<T>()` handlers, and the method impl uses `.Of<TypeParams>()` access (ClassRenderer:1067)

The plan's "base.Method() fallback" claim is only relevant for the unconfigured path. The `Of<T>()` handler IS still generated and used for configured callbacks. So if a class has virtual/abstract methods with mixed-arity generics or out/ref on generic methods, those bugs would manifest.

**Question:** Should the plan update Bug 3 affected patterns to include P3, P4 and the "Files to Change" to include `ClassModelBuilder.cs` and `StandaloneClassModelBuilder.cs`? Same for Bug 4?

**Suggestion:** Add `ClassModelBuilder.cs:632` and `StandaloneClassModelBuilder.cs:708` to Bug 3's "Files to Change". Add `ClassModelBuilder.cs:600` and `StandaloneClassModelBuilder.cs:676` to Bug 4's "Files to Change". Update the nine-pattern analysis to show P3/P4 need fixes for Bugs 3 and 4 (not just Bug 2).

#### 2. Bug 2 -- `in` on Indexer Params in Class Pipeline Model Builders Also Affected

**Category:** Completeness

**Details:** The plan mentions "Class pipelines (StandaloneClassRenderer, ClassRenderer) -- Also check `RenderImplIndexerOverride` at lines using `indexer.ParameterDeclarations`" for Bug 2 but does not trace back to the source of `ParameterDeclarations` in class model builders.

I found that `BuildImplIndexerModel` in both `StandaloneClassModelBuilder.cs:521` and `ClassModelBuilder.cs:447` builds `paramList` as `$"{p.Type} {p.Name}"` without RefKind, which feeds into `ParameterDeclarations`. The `ParameterSignature` at `StandaloneClassRenderer.cs:1084` and `ClassRenderer.cs:1169` then uses this `ParameterDeclarations` value for the interceptor model. So the `in` modifier is dropped at the class model builder level too.

**Question:** Should the "Files to Change" for Bug 2 explicitly list `StandaloneClassModelBuilder.cs:521` and `ClassModelBuilder.cs:447`? The plan's note about checking class pipelines is good but should be elevated from a "Note" to a required change.

**Suggestion:** Add `StandaloneClassModelBuilder.cs:521 (BuildImplIndexerModel)` and `ClassModelBuilder.cs:447 (BuildImplIndexerModel)` to Bug 2's "Files to Change" list. Also add the `InlineModelBuilder.cs:266,272` (indexer model builder, both keyType tuple form and paramSig) which are also missing RefKind.

#### 3. Bug 2 -- ModelAdapters.cs:308 Also Needs Fix

**Category:** Completeness

**Details:** The plan identifies FlatIndexerModel as needing a `KeyRefPrefix` field and the renderer needing to use it. However, it doesn't mention that `ModelAdapters.ToUnifiedIndexerModel(FlatIndexerModel)` at line 308 builds `ParameterSignature` as `$"{indexer.KeyType} {indexer.KeyParamName}"` without any ref prefix. This adapter feeds the unified `IndexerInterceptorRenderer` which uses `ParameterSignature` for `InvokeGet`/`InvokeSet` method signatures. If the adapter doesn't include the `in` modifier, the interceptor's invoke methods will have the wrong signature.

**Question:** Should `ModelAdapters.cs:308` be added to the "Files to Change" for Bug 2?

**Suggestion:** Update `ModelAdapters.ToUnifiedIndexerModel(FlatIndexerModel)` to include the new `KeyRefPrefix` field in the `ParameterSignature` construction.

#### 4. Bug 4 -- Inline Pipeline Architecture Needs More Specificity

**Category:** Clarity / Implementability

**Details:** For Bug 4, the plan says the inline pipeline approach should either "(a) Extend `InlineGenericMethodHandlerModel` to support multiple type arities, or (b) Generate multiple handler classes per method name." This is vague enough to cause implementation indecision.

The existing `FlatGenericMethodHandlerGroup` / `FlatGenericTypeArityGroup` pattern is well-proven. The plan should specify which option is recommended for the inline pipeline, and how the model changes would work. For instance:
- Will `InlineGenericMethodHandlerModel` be replaced with something like `InlineGenericMethodHandlerGroup` containing multiple arity entries?
- Will each arity group have its own typed handler class name, dictionary, etc.?
- How will the renderer emit multiple `Of<>()` methods on the same interceptor class?

Currently, there is only one interceptor class per generic method group in inline. The multi-arity fix needs to put multiple `Of<>()` methods on that single interceptor class (matching the flat pattern).

**Question:** Can the plan explicitly recommend option (a) with a description of the model changes, rather than leaving both options open?

**Suggestion:** Recommend extending `InlineGenericMethodHandlerModel` to hold an `EquatableArray<InlineGenericTypeArityGroup>` (similar to `FlatGenericTypeArityGroup`). Each arity group gets its own `Of<>()` method, dictionary, typed handler class, and delegate. The existing single-arity fields on the model become a single-element array.

#### 5. Bug 4 -- Feature E-2 Scope Table Missing P3, P4, P6, P9

**Category:** Completeness

**Details:** The Feature E-2 scope table ("Methods with mixed type arities") only lists P1, P2, P5, P8. Based on my codebase investigation, P3 (`StandaloneClassModelBuilder`), P4 (same), P6 (`ClassModelBuilder`), and P9 (same) also build `InlineGenericMethodHandlerModel` using `genericMembers[0]` and would fail with mixed arities.

**Question:** Should P3, P4, P6, P9 be added to the Feature E-2 scope table with "Needs Implementation"?

### What Looks Good

- Bug 1 (SmartDefault collision) root cause analysis is precise and the fix is correct -- rename to `TSmartDefault` in 2 files
- Bug 2 root cause for the flat and inline interface pipelines is accurately identified with correct line numbers
- Bug 3 root cause for `InlineModelBuilder.cs:491` is exactly right and the one-line fix is correct
- Bug 4 root cause analysis (first-arity-only pattern) is correct and the proposal to reuse `FlatGenericMethodHandlerGroup` as a template is sound
- The Current State Analysis is thorough with extensive test evidence
- Design.Stubs verification files exist and demonstrate both passing and failing cases
- The reproduction tests at `RocksGapReproductionTests.cs` are well-structured with clear documentation
- The phasing (Bugs 1 -> 3 -> 2 -> 4) is in correct dependency order (simplest first, most complex last)
- The plan correctly identifies that Bug 1 does NOT affect class patterns (no SmartDefault in class pipeline)

### Devil's Advocate Analysis

**Edge cases NOT explicitly covered:**
1. What if an indexer has BOTH `in` and `ref readonly` parameters (e.g., `this[in int a, ref readonly string b]`)? The fix needs to handle per-parameter RefKind, not just the first parameter.
2. What if mixed-arity generic methods also have `out`/`ref` params? Bug 3 and Bug 4 could compound -- the multi-arity handler's delegate must include RefKind for each arity's parameters.
3. What about `params` arrays on generic methods? Does `RefKind.None` vs `params` interaction need consideration?

**Ways this could break existing functionality:**
1. Renaming `SmartDefault<T>` to `SmartDefault<TSmartDefault>` could theoretically break if any existing generated code references the type parameter `T` inside `SmartDefault` (e.g., `typeof(T)`). All internal references must be updated. The plan does mention this but the developer should verify by searching for all occurrences of `typeof(T)` and `(T)` cast within `RenderSmartDefaultMethod`.

**Ways users could misunderstand the API:**
1. Bug 4 introduces multiple `Of<>()` methods on the same interceptor. Users need to know which overload to call. The existing pattern (same name, different type arity) should be natural C# overload resolution, so this is likely fine.

### Recommendation

Send back to architect to address concerns 1-5 before implementation. The core issues are:
- **Concerns 1, 2, 3, 5**: The plan undercounts affected files and patterns. Class model builders (`ClassModelBuilder`, `StandaloneClassModelBuilder`) need the same fixes as inline model builders for Bugs 2, 3, and 4. The `ModelAdapters.cs` needs updating for Bug 2.
- **Concern 4**: Bug 4's inline pipeline approach needs more specificity to avoid implementation ambiguity.

---

## Architect Response to Developer Concerns

**Date:** 2026-02-08

All five concerns independently verified and confirmed. The plan has been updated throughout to address each one.

### Concern 1 Resolution: Bug 3 and Bug 4 class patterns -- CONFIRMED and FIXED

The developer is correct. My original claim that "class patterns use `base.Method()` fallback and don't generate generic method handlers" was factually incorrect. I verified:

- `ClassModelBuilder.BuildGenericMethodHandlerModel()` at line 632: `delegateParams.Add($"{p.Type} {p.Name}")` -- same Bug 3 pattern as `InlineModelBuilder.cs:491`
- `StandaloneClassModelBuilder.BuildGenericMethodHandlerModel()` at line 708: identical Bug 3 pattern
- `ClassModelBuilder` line 600: `genericMembers[0]` -- same Bug 4 first-arity pattern
- `StandaloneClassModelBuilder` line 676: `genericMembers[0]` -- same Bug 4 first-arity pattern
- `ClassRenderer.RenderClassGenericMethodHandler()` at line 314: renders `Of<T>()` handlers with dictionaries, confirming the handlers ARE generated and used

The `base.Method()` fallback is only the unconfigured path. The `Of<T>()` handler is still generated and used for configured callbacks via `ClassRenderer.RenderClassGenericMethodHandler()` (confirmed at line 343).

**Changes made:**
- Bug 3: Added P3, P4 to affected patterns. Added `ClassModelBuilder.cs:632` and `StandaloneClassModelBuilder.cs:708` to Files to Change.
- Bug 4: Updated affected patterns to include P3, P4, P6, P9. Added `ClassModelBuilder.cs:600`, `StandaloneClassModelBuilder.cs:676`, and `ClassRenderer.cs` to Files to Change.
- Nine-Pattern Analysis: P3, P4, P9 now show Bugs 2, 3, 4. P6 now shows Bugs 2, 3, 4.
- Summary table: Bug 3 patterns updated to P3, P4, P5, P6, P8, P9. Bug 4 patterns updated to P1, P2, P3, P4, P5, P6, P8, P9.

### Concern 2 Resolution: Bug 2 class pipeline model builders -- CONFIRMED and FIXED

The developer is correct. I verified:
- `ClassModelBuilder.BuildImplIndexerModel()` at line 447: `$"{p.Type} {p.Name}"` without RefKind
- `StandaloneClassModelBuilder.BuildImplIndexerModel()` at line 521: identical pattern
- `ClassModelBuilder.BuildIndexerModel()` at lines 373, 375: keyType tuple form and paramSig without RefKind
- `StandaloneClassModelBuilder.BuildIndexerModel()` at lines 446, 448: identical pattern
- `InlineModelBuilder.BuildInlineIndexerModel()` at lines 266, 272: keyType tuple form and paramSig without RefKind

**Changes made:** Bug 2 "Files to Change" expanded from 6 items (with a vague "check class pipelines" note) to 14 explicitly enumerated locations across all 4 pipelines. The class pipeline locations are now listed as required changes, not optional checks.

### Concern 3 Resolution: ModelAdapters.cs:308 -- CONFIRMED and FIXED

The developer is correct. I verified at `ModelAdapters.cs` line 308:
```csharp
ParameterSignature: $"{indexer.KeyType} {indexer.KeyParamName}",
```
This feeds into `IndexerInterceptorRenderer` for `InvokeGet`/`InvokeSet` method signatures. Without the ref prefix, the interceptor's invoke methods would have the wrong signature even after the other flat pipeline fixes.

**Changes made:** Added `ModelAdapters.cs` line 308 as item 4 in Bug 2's flat pipeline Files to Change.

### Concern 4 Resolution: Bug 4 inline pipeline architecture -- COMMITTED to option A

The developer is correct that leaving both options open creates implementation ambiguity. I have committed to **option (a)**: extend `InlineGenericMethodHandlerModel` with an `EquatableArray<InlineGenericTypeArityGroup>`.

The plan now specifies:
- New record `InlineGenericTypeArityGroup` mirroring `FlatGenericTypeArityGroup`
- Which fields move from the top-level model to the per-arity record
- Which fields stay on the top-level model
- How builders group overloads by type parameter count
- How renderers iterate over arity groups
- That single-arity (common case) becomes a single-element array with no behavioral change

See the updated Bug 4 "Scope of fix" and Phase 4 sections for the full specification.

### Concern 5 Resolution: Feature E-2 scope table -- CONFIRMED and FIXED

The developer is correct. I verified that `ClassModelBuilder.BuildGenericMethodHandlerModel()` at line 600 and `StandaloneClassModelBuilder.BuildGenericMethodHandlerModel()` at line 676 both use the same `genericMembers[0]` first-arity pattern.

**Changes made:** Added P3, P4, P6, P9 to Feature E-2 scope table with "Needs Implementation" status and evidence referencing the specific builder locations.

---

## Implementation Contract

**Created:** 2026-02-08
**Approved by:** knockoff-developer

### Design.Stubs Acceptance Criteria

These are the failing Design.Stubs files left by the architect. Implementation is done when they all compile.

- [ ] `src/Design/Design.Stubs/StubPatterns/GenericTypeGapsVerification.cs:42` - P2: `GenericTransformServiceStub<T>` CS0693 -> Must compile after Bug 1 fix
- [ ] `src/Design/Design.Stubs/StubPatterns/GenericTypeGapsVerification.cs:53` - P8: `OpenGenericTransformServiceTest` CS0693 -> Must compile after Bug 1 fix
- [ ] `src/Tests/KnockOffTests/RocksGapReproductionTests.cs` - Gap26 stubs (uncomment) -> Must compile after Bug 2 fix
- [ ] `src/Tests/KnockOffTests/RocksGapReproductionTests.cs` - Gap27/28 stubs (uncomment) -> Must compile after Bug 3 fix
- [ ] `src/Tests/KnockOffTests/RocksGapReproductionTests.cs` - Gap31 stubs (uncomment) -> Must compile after Bug 4 fix

### In Scope

**Phase 1: Bug 1 - SmartDefault Type Parameter Collision (Low Risk)**

- [ ] `src/Generator/Renderer/FlatRenderer.cs` - `RenderSmartDefaultMethod()`: Rename `T` to `TSmartDefault` in method declaration, `typeof(T)`, and `(T)` cast
- [ ] `src/Generator/Renderer/InlineRenderer.cs` - `RenderSmartDefaultMethod()`: Same rename
- [ ] **Checkpoint:** `dotnet build src/Design/Design.Stubs` - GenericTypeGapsVerification.cs:42 and :53 must compile

**Phase 2: Bug 3 - Generic Method out/ref Delegate (Low Risk)**

- [ ] `src/Generator/Builder/InlineModelBuilder.cs` line 491 - Add `GetRefKindPrefix(p.RefKind)` to delegate param formatting
- [ ] `src/Generator/Builder/ClassModelBuilder.cs` line 632 - Same fix
- [ ] `src/Generator/Builder/StandaloneClassModelBuilder.cs` line 708 - Same fix
- [ ] Uncomment `Gap27InlineTest` and `Gap28InlineTest` in `RocksGapReproductionTests.cs`
- [ ] **Checkpoint:** `dotnet build src/Tests/KnockOffTests` must compile. `dotnet test src/KnockOff.sln` - all tests pass

**Phase 3: Bug 2 - `in` Modifier on Indexer Parameters (Low-Medium Risk)**

Flat pipeline:
- [ ] `src/Generator/Model/Flat/FlatIndexerModel.cs` - Add `KeyRefPrefix` field (or similar)
- [ ] `src/Generator/Builder/FlatModelBuilder.cs` lines 608-612 - Populate `KeyRefPrefix` from `member.IndexerParameters[0].RefKind`
- [ ] `src/Generator/Renderer/FlatRenderer.cs` line 2114 - Use `{indexer.KeyRefPrefix}{indexer.KeyType}` in indexer declaration
- [ ] `src/Generator/Renderer/Shared/ModelAdapters.cs` line 308 - Include `KeyRefPrefix` in `ParameterSignature`

Inline pipeline:
- [ ] `src/Generator/Builder/InlineModelBuilder.cs` line 266 - Add `GetRefKindPrefix(p.RefKind)` to keyType tuple form
- [ ] `src/Generator/Builder/InlineModelBuilder.cs` line 272 - Add `GetRefKindPrefix(p.RefKind)` to paramSig
- [ ] `src/Generator/Builder/InlineModelBuilder.cs` line 792 - Add `GetRefKindPrefix(p.RefKind)` to BuildIndexerImplementation paramList

Class pipeline:
- [ ] `src/Generator/Builder/ClassModelBuilder.cs` line 373 - Add `GetRefKindPrefix(p.RefKind)` to BuildIndexerModel keyType tuple form
- [ ] `src/Generator/Builder/ClassModelBuilder.cs` line 375 - Add `GetRefKindPrefix(p.RefKind)` to BuildIndexerModel paramSig
- [ ] `src/Generator/Builder/ClassModelBuilder.cs` line 447 - Add `GetRefKindPrefix(p.RefKind)` to BuildImplIndexerModel paramList
- [ ] `src/Generator/Builder/StandaloneClassModelBuilder.cs` line 446 - Add `GetRefKindPrefix(p.RefKind)` to BuildIndexerModel keyType tuple form
- [ ] `src/Generator/Builder/StandaloneClassModelBuilder.cs` line 448 - Add `GetRefKindPrefix(p.RefKind)` to BuildIndexerModel paramSig
- [ ] `src/Generator/Builder/StandaloneClassModelBuilder.cs` line 521 - Add `GetRefKindPrefix(p.RefKind)` to BuildImplIndexerModel paramList

Shared:
- [ ] `src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs` - Verify `ParameterSignature` handles `in` correctly in `InvokeGet`/`InvokeSet`

Verification:
- [ ] Uncomment `Gap26InlineTest` and `Gap26StandaloneKnockOff` in `RocksGapReproductionTests.cs`
- [ ] **Checkpoint:** `dotnet build src/Tests/KnockOffTests` must compile. `dotnet test src/KnockOff.sln` - all tests pass

**Phase 4: Bug 4 - Mixed Type Arity Generic Methods (Medium-High Risk)**

Model changes:
- [ ] Create `src/Generator/Model/Inline/InlineGenericTypeArityGroup.cs` record mirroring `FlatGenericTypeArityGroup` with: `TypeParameterNames`, `TypeParameterCount`, `KeyType`, `KeyConstruction`, `ConstraintClauses`, `TypedHandlerClassName`, `DelegateSignature`, `IsVoid`, `ReturnType`, `NonGenericParameters`, `LastCallArgType`, `LastCallArgsType`
- [ ] Refactor `src/Generator/Model/Inline/InlineGenericMethodHandlerModel.cs` - Move per-arity fields to `InlineGenericTypeArityGroup`, add `EquatableArray<InlineGenericTypeArityGroup> ArityGroups`, keep top-level: `InterceptorClassName`, `MethodName`, `StubClassName`, `InterfaceTypeParameterList`, `InterfaceConstraintClauses`
- [ ] Refactor `src/Generator/Model/Flat/FlatGenericMethodHandlerModel.cs` - Either extend with `EquatableArray<FlatGenericTypeArityGroup>` or replace with `FlatGenericMethodHandlerGroup` (study the existing pattern)

Builder changes:
- [ ] `src/Generator/Builder/FlatModelBuilder.cs` `BuildGenericMethodHandler()` - Group overloads by type parameter count, produce multi-arity handler
- [ ] `src/Generator/Builder/InlineModelBuilder.cs` `BuildGenericMethodHandlerModel()` - Same multi-arity grouping
- [ ] `src/Generator/Builder/ClassModelBuilder.cs` `BuildGenericMethodHandlerModel()` - Same multi-arity grouping
- [ ] `src/Generator/Builder/StandaloneClassModelBuilder.cs` `BuildGenericMethodHandlerModel()` - Same multi-arity grouping

Renderer changes:
- [ ] `src/Generator/Renderer/FlatRenderer.cs` `RenderGenericMethodHandler()` - Render multiple `Of<>()` methods per arity (follow `RenderGenericStubOverrideHandlerGroup` pattern)
- [ ] `src/Generator/Renderer/InlineRenderer.cs` `RenderGenericMethodHandler()` + `RenderTypedHandlerClass()` - Iterate over arity groups
- [ ] `src/Generator/Renderer/ClassRenderer.cs` `RenderClassGenericMethodHandler()` - Iterate over arity groups

Verification:
- [ ] Uncomment `Gap31InlineTest` and `Gap31StandaloneKnockOff` in `RocksGapReproductionTests.cs`
- [ ] **Checkpoint:** `dotnet build src/Tests/KnockOffTests` must compile. `dotnet test src/KnockOff.sln` - all tests pass

**Phase 5: Tests (Additive)**

- [ ] Bug 1 tests: P2 generic standalone + method-level generics (verify `Of<T>()` works)
- [ ] Bug 1 tests: P8 open generic interface + method-level generics
- [ ] Bug 2 tests: `in` modifier on indexer params in standalone and inline
- [ ] Bug 3 tests: Generic methods with out/ref in inline - verify callback invocation
- [ ] Bug 4 tests: Mixed arity generic methods - verify both `Of<T>()` and `Of<T1, T2>()` work
- [ ] Bug 4 tests: Class patterns with mixed arity generic methods
- [ ] **Checkpoint:** `dotnet test src/KnockOff.sln` - all tests pass

**Phase 6: Documentation**

- [ ] Update `docs/todos/generic-type-gaps.md` with progress
- [ ] Prepare release notes draft

### Explicitly Out of Scope

- Gap 29 (all interfaces in namespace) - Separate investigation needed, different root cause
- Any new features - This is purely fixing existing broken scenarios
- P7 (Inline Delegate) - Already works, no bugs affect it
- Modifying the user-facing API (no new attributes, no new library methods)
- Performance optimization of generated code

### Verification Gates

1. **After Phase 1:** `dotnet build src/Design/Design.Stubs` succeeds (GenericTypeGapsVerification.cs compiles fully). Existing tests still pass.
2. **After Phase 2:** Gap27/28 stubs uncommented and compile. All existing tests pass.
3. **After Phase 3:** Gap26 stubs uncommented and compile. All existing tests pass.
4. **After Phase 4:** Gap31 stubs uncommented and compile. All existing tests pass. Single-arity generic methods still work (no regression in `GenericMethodTests`, `GenericConstraintCoverageTests`, `GenericMethodBugTests`).
5. **Final:** `dotnet test src/KnockOff.sln` - all tests pass across all target frameworks. `dotnet build src/Design/Design.Stubs` succeeds. All Design.Stubs acceptance criteria compile.

### Stop Conditions

If any of these occur, STOP and report:
- An out-of-scope test starts failing (any test not directly related to Bugs 1-4)
- Architectural contradiction discovered (e.g., the multi-arity model change breaks an assumption in the renderer)
- Generated code does not compile for patterns that previously worked
- Phase 4 model refactoring breaks the single-arity common case (existing `GenericMethodTests` fail)
- Any existing `FlatGenericMethodHandlerGroup` / stub override handler behavior regresses

---

## Implementation Progress

**Started:** 2026-02-08

### Phase 1: Bug 1 - SmartDefault Type Parameter Collision

- [x] `src/Generator/Renderer/FlatRenderer.cs` - `RenderSmartDefaultMethod()`: Renamed `T` to `TSmartDefault` in method declaration, return type, `typeof()`, comment, and cast
- [x] `src/Generator/Renderer/InlineRenderer.cs` - `RenderSmartDefaultMethod()`: Same rename
- [x] **Checkpoint:** `dotnet build src/KnockOff.sln` - 0 warnings, 0 errors
- [x] **Checkpoint:** `dotnet test src/Tests/KnockOffTests/KnockOffTests.csproj` - net8.0: 1347 passed, net9.0: 1348 passed, net10.0: 1348 passed, 0 failures
- [x] **Checkpoint:** `dotnet build src/Design/Design.Stubs` - 0 warnings, 0 errors
- [x] **Verification:** GenericTypeGapsVerification.cs:42 (`GenericTransformServiceStub<T>`) compiles - CONFIRMED
- [x] **Verification:** GenericTypeGapsVerification.cs:53 (`OpenGenericTransformServiceTest`) compiles - CONFIRMED
- [x] **Verification:** Generated code at `GenericTransformServiceStub\`1.g.cs` shows `SmartDefault<TSmartDefault>` (no longer `SmartDefault<T>`) - CONFIRMED

### Phase 2: Bug 3 - Generic Method out/ref Delegate Parameters

- [x] `src/Generator/Builder/InlineModelBuilder.cs` line 491: Changed `$"{p.Type} {p.Name}"` to `$"{GetRefKindPrefix(p.RefKind)}{p.Type} {p.Name}"` in delegate param formatting
- [x] `src/Generator/Builder/ClassModelBuilder.cs` line 632: Same fix
- [x] `src/Generator/Builder/StandaloneClassModelBuilder.cs` line 708: Same fix
- [x] Uncommented `Gap27InlineTest` (`[KnockOff<IGap27_OutParameter>]`) in `RocksGapReproductionTests.cs`
- [x] Uncommented `Gap28InlineTest` (`[KnockOff<IGap28_RefParameter>]`) in `RocksGapReproductionTests.cs`
- [x] **Checkpoint:** `dotnet build src/KnockOff.sln` - 0 warnings, 0 errors
- [x] **Checkpoint:** `dotnet build src/Tests/KnockOffTests` - 0 warnings, 0 errors (Gap 27/28 stubs compile)
- [x] **Checkpoint:** `dotnet test src/Tests/KnockOffTests/KnockOffTests.csproj` - net8.0: 1347 passed, net9.0: 1348 passed, net10.0: 1348 passed, 0 failures

### Phase 3: Bug 2 - `in` Modifier on Indexer Parameters

Flat pipeline:
- [x] `src/Generator/Model/Flat/FlatIndexerModel.cs` - Added `string KeyRefPrefix` field to record
- [x] `src/Generator/Builder/FlatModelBuilder.cs` lines 608-616 - Added `keyRefPrefix` variable from `GetRefKindPrefix(member.IndexerParameters.GetArray()![0].RefKind)` and passed to `FlatIndexerModel` constructor
- [x] `src/Generator/Renderer/FlatRenderer.cs` line 2114 - Changed `this[{indexer.KeyType} {indexer.KeyParamName}]` to `this[{indexer.KeyRefPrefix}{indexer.KeyType} {indexer.KeyParamName}]`
- [x] `src/Generator/Renderer/Shared/ModelAdapters.cs` line 308 - Changed `ParameterSignature: $"{indexer.KeyType} {indexer.KeyParamName}"` to include `{indexer.KeyRefPrefix}`

Inline pipeline:
- [x] `src/Generator/Builder/InlineModelBuilder.cs` line 272 - Added `GetRefKindPrefix(p.RefKind)` to paramSig
- [x] `src/Generator/Builder/InlineModelBuilder.cs` line 792 - Added `GetRefKindPrefix(p.RefKind)` to BuildIndexerImplementation paramList
- [x] Note: Line 266 keyType tuple form intentionally NOT changed -- keyType is used as a C# type (dictionary key, generic arg) and must not include `in`/`ref`/`out` modifiers

Class pipeline:
- [x] `src/Generator/Builder/ClassModelBuilder.cs` line 375 - Added `GetRefKindPrefix(p.RefKind)` to BuildIndexerModel paramSig
- [x] `src/Generator/Builder/ClassModelBuilder.cs` line 447 - Added `GetRefKindPrefix(p.RefKind)` to BuildImplIndexerModel paramList
- [x] Note: Line 373 keyType tuple form intentionally NOT changed (same reason as inline)
- [x] `src/Generator/Builder/StandaloneClassModelBuilder.cs` line 448 - Added `GetRefKindPrefix(p.RefKind)` to BuildIndexerModel paramSig
- [x] `src/Generator/Builder/StandaloneClassModelBuilder.cs` line 521 - Added `GetRefKindPrefix(p.RefKind)` to BuildImplIndexerModel paramList
- [x] Note: Line 446 keyType tuple form intentionally NOT changed (same reason)

Class pipeline adapters (additional fix discovered during implementation):
- [x] `src/Generator/Renderer/ClassRenderer.cs` line 1153 - Fixed paramTypes extraction to use `parts[parts.Length - 2]` instead of `parts[0]` to handle ref kind prefixes (e.g., `in int a` splits to `["in", "int", "a"]`, need `parts[1]` = `int` not `parts[0]` = `in`)
- [x] `src/Generator/Renderer/StandaloneClassRenderer.cs` line 1068 - Same fix

Method interceptor (additional fix discovered during implementation):
- [x] `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` line 3125 - Fixed `BuildCallbackArgs` to NOT include `in`/`ref readonly` prefix at call sites when passing to callbacks (Action/Func delegates don't accept `in`). Only `ref` and `out` are valid at call sites for delegates. This was a pre-existing bug exposed by the `InArgument(in int a)` method in `IGap26_InParameter`.

Shared:
- [x] `src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs` - Verified: `ParameterSignature` correctly flows through to `InvokeGet`/`InvokeSet` method signatures. `KeyExpression` (used at call sites inside InvokeGet/InvokeSet) does NOT include `in`, so callback calls like `_get(a)` are correct.

Verification:
- [x] Uncommented `Gap26InlineTest` (`[KnockOff<IGap26_InParameter>]`) in `RocksGapReproductionTests.cs`
- [x] Uncommented `Gap26StandaloneKnockOff` (`[KnockOff] : IGap26_InParameter`) in `RocksGapReproductionTests.cs`
- [x] **Verification:** Generated code `Gap26InlineTest.Stubs.g.cs` line 764: `int global::KnockOff.Tests.IGap26_InParameter.this[in int a]` - CONFIRMED
- [x] **Verification:** Generated code `Gap26StandaloneKnockOff.g.cs` line 793: `int global::KnockOff.Tests.IGap26_InParameter.this[in int a]` - CONFIRMED
- [x] **Verification:** InvokeGet signature: `internal int InvokeGet(bool strict, in int a)` - CONFIRMED
- [x] **Checkpoint:** `dotnet build src/KnockOff.sln` - 0 warnings, 0 errors
- [x] **Checkpoint:** `dotnet build src/Tests/KnockOffTests` - 0 warnings, 0 errors (Gap 26 stubs compile)
- [x] **Checkpoint:** `dotnet test src/Tests/KnockOffTests/KnockOffTests.csproj` - net8.0: 1347 passed, net9.0: 1348 passed, net10.0: 1348 passed, 0 failures

### Phase 4: Bug 4 - Mixed Type Arity Generic Methods

Model layer:
- [x] `src/Generator/Model/Inline/InlineGenericTypeArityGroup.cs` - NEW FILE: Record holding per-arity data (TypeParameterNames, TypeParameterCount, KeyType, KeyConstruction, ConstraintClauses, TypedHandlerClassName, DelegateSignature, IsVoid, ReturnType, NonGenericParameters, LastCallArgType, LastCallArgsType) for inline/class pipelines
- [x] `src/Generator/Model/Inline/InlineGenericMethodHandlerModel.cs` - REWRITTEN: Moved all per-arity fields into `EquatableArray<InlineGenericTypeArityGroup> ArityGroups`. Top-level retains only: InterceptorClassName, MethodName, StubClassName, InterfaceTypeParameterList, InterfaceConstraintClauses
- [x] `src/Generator/Model/Flat/FlatGenericMethodHandlerModel.cs` - REWRITTEN: Created `FlatGenericMethodArityGroup` record with per-arity fields. Model retains: InterceptorName, InterceptorClassName, MethodName, NeedsNewKeyword, and `EquatableArray<FlatGenericMethodArityGroup> ArityGroups`

Builder layer (all 4 builders):
- [x] `src/Generator/Builder/FlatModelBuilder.cs` - `BuildGenericMethodHandler()`: Groups generic overloads by `TypeParameters.Count`, creates one `FlatGenericMethodArityGroup` per unique arity. TypedHandlerClassName uses count suffix for arities > 1 (e.g., `RunTypedHandler2`)
- [x] `src/Generator/Builder/InlineModelBuilder.cs` - `BuildGenericMethodHandlerModel()`: Same grouping pattern using `MethodOverloadInfo.TypeParameters.Count`, creates `InlineGenericTypeArityGroup` per arity
- [x] `src/Generator/Builder/ClassModelBuilder.cs` - `BuildGenericMethodHandlerModel()`: Groups `ClassMemberInfo` by `TypeParameters.Count`, creates `InlineGenericTypeArityGroup` per arity
- [x] `src/Generator/Builder/StandaloneClassModelBuilder.cs` - `BuildGenericMethodHandlerModel()`: Same as ClassModelBuilder but with different `stubClassRef` (no `Stubs.` prefix)

Renderer layer (all 3 renderers):
- [x] `src/Generator/Renderer/FlatRenderer.cs` - `RenderGenericMethodHandler()`: Iterates over `ArityGroups` for dictionaries, `Of<>()` methods, Reset, TotalCallCount, CalledTypeArguments, IsConfigured. Uses `isMultiArity` flag and `GetDictSuffix()` local function for correct dictionary naming (suffix only when multiple arity groups exist). `RenderTypedHandlerClass()` updated to take `FlatGenericMethodArityGroup` parameter.
- [x] `src/Generator/Renderer/InlineRenderer.cs` - `RenderGenericMethodHandler()`: Same multi-arity iteration pattern. Renamed `RenderTypedHandlerClass` to `RenderInlineTypedHandlerClass` to take `InlineGenericTypeArityGroup` parameter. Uses same `isMultiArity`/`GetDictSuffix` pattern.
- [x] `src/Generator/Renderer/ClassRenderer.cs` - `RenderClassGenericMethodHandler()`: Same multi-arity iteration with `isMultiArity`/`GetDictSuffix` pattern. `RenderClassTypedHandlerClass()` updated to take `InlineGenericTypeArityGroup` parameter.

Tests:
- [x] Uncommented `Gap31InlineTest` (`[KnockOff<IGap31_GenericMethods>]`) in `RocksGapReproductionTests.cs`
- [x] Uncommented `Gap31StandaloneKnockOff` (`[KnockOff] : IGap31_GenericMethods`) in `RocksGapReproductionTests.cs`
- [x] Added `Gap31_MixedArity_InlineCompiles` test - verifies inline stub compiles
- [x] Added `Gap31_MixedArity_StandaloneCompiles` test - verifies standalone stub compiles
- [x] Added `Gap31_MixedArity_InlineCanConfigureBothArities` test - configures `Run.Of<int>().Return(() => 42)` and `Run.Of<string, int>().Return((input) => 99)`, verifies both work
- [x] Added `Gap31_MixedArity_StandaloneCanConfigureBothArities` test - same verification for standalone pattern

Verification:
- [x] **Checkpoint:** `dotnet build src/KnockOff.sln` - 0 warnings, 0 errors
- [x] **Checkpoint:** `dotnet build src/Design/Design.Stubs` - 0 warnings, 0 errors
- [x] **Checkpoint:** `dotnet test src/Tests/KnockOffTests/KnockOffTests.csproj` - net8.0: 1351 passed, net9.0: 1352 passed, net10.0: 1352 passed, 0 failures
- [x] **Checkpoint:** `dotnet test src/Tests/KnockOffTests.AssemblyStrict` - 14 passed per framework, 0 failures
- [x] **Checkpoint:** `dotnet test src/Tests/KnockOff.Documentation.Samples` - 599 passed per framework, 0 failures
- [x] **Checkpoint:** `dotnet test src/Tests/KnockOff.NeatooInterfaceTests` - 473 passed per framework, 0 failures
- [x] No regressions in `GenericMethodTests`, `GenericConstraintCoverageTests`, or `GenericMethodBugTests`

Key design decision - dictionary suffix logic:
- Suffix is applied ONLY when multiple arity groups exist for the same method name (e.g., `_typedHandlers_1` and `_typedHandlers_2`)
- When only one arity group exists (even if it has multiple type parameters), no suffix is used (`_typedHandlers`)
- This avoids breaking all existing single-arity generic methods which reference `_typedHandlers` without suffix

---

## Completion Evidence

### Full Test Results (All Phases Complete)

| Project | net8.0 | net9.0 | net10.0 |
|---------|--------|--------|---------|
| KnockOffTests | 1351 passed, 0 failed | 1352 passed, 0 failed | 1352 passed, 0 failed |
| AssemblyStrict | 14 passed, 0 failed | 14 passed, 0 failed | 14 passed, 0 failed |
| Documentation.Samples | 599 passed, 0 failed | 599 passed, 0 failed | 599 passed, 0 failed |
| NeatooInterfaceTests | 473 passed, 0 failed | 473 passed, 0 failed | 473 passed, 0 failed |

### Design.Stubs Build
`dotnet build src/Design/Design.Stubs` - 0 warnings, 0 errors (all 3 target frameworks)

### Full Solution Build
`dotnet build src/KnockOff.sln` - 0 warnings, 0 errors

### Bugs Fixed (All 4 Phases)

1. **Phase 1 - Bug 1 (SmartDefault collision):** Renamed `T` to `TSmartDefault` in `SmartDefault<T>()` method to avoid collision with interface type parameters named `T`. Affects FlatRenderer and InlineRenderer.

2. **Phase 2 - Bug 3 (out/ref on generic delegates):** Added `GetRefKindPrefix()` to delegate parameter formatting in InlineModelBuilder, ClassModelBuilder, and StandaloneClassModelBuilder. Gap 27 (out) and Gap 28 (ref) stubs now compile.

3. **Phase 3 - Bug 2 (in modifier on indexers):** Added ref kind prefix to indexer parameter signatures across all 4 pipelines. Discovered and fixed additional issues: paramTypes extraction with ref prefixes in ClassRenderer/StandaloneClassRenderer, and callback argument handling for `in` parameters in MethodInterceptorRenderer. Gap 26 stubs now compile.

4. **Phase 4 - Bug 4 (mixed type arity):** Restructured generic method handler models to support multiple type parameter arities per method name. Created `InlineGenericTypeArityGroup` and `FlatGenericMethodArityGroup` records. Updated all 4 builders to group by type parameter count and all 3 renderers to iterate over arity groups with correct dictionary naming. Gap 31 stubs now compile and new tests verify both arities work.

---

## Architect Verification

**Verified:** 2026-02-09
**Verdict:** VERIFIED

### Independent Build Results

| Project | Result |
|---------|--------|
| `dotnet build src/KnockOff.sln` | 0 warnings, 0 errors |
| `dotnet build src/Design/Design.Stubs` | 0 warnings, 0 errors |

### Independent Test Results

| Project | net8.0 | net9.0 | net10.0 |
|---------|--------|--------|---------|
| KnockOffTests | 1351 passed, 0 failed | 1352 passed, 0 failed | 1352 passed, 0 failed |
| AssemblyStrict | 14 passed, 0 failed | 14 passed, 0 failed | 14 passed, 0 failed |
| Documentation.Samples | 599 passed, 0 failed | 599 passed, 0 failed | 599 passed, 0 failed |
| NeatooInterfaceTests | 473 passed, 0 failed | 473 passed, 0 failed | 473 passed, 0 failed |
| Design.Tests | 356 passed, 0 failed | 356 passed, 0 failed | 356 passed, 0 failed |

Zero failures across all projects and all target frameworks.

### Design Match Verification

**Bug 1 (SmartDefault collision):** Matches plan. Both `FlatRenderer.cs` and `InlineRenderer.cs` emit `SmartDefault<TSmartDefault>`. Generated code for `GenericTransformServiceStub<T>` (P2) and `OpenGenericTransformServiceTest` (P8) in Design.Stubs now compiles successfully.

**Bug 2 (Gap 26, `in` indexer params):** Matches plan. Generated code for both standalone (`Gap26StandaloneKnockOff.g.cs:793`) and inline (`Gap26InlineTest.Stubs.g.cs:764`) correctly emits `this[in int a]`. The developer also discovered and fixed additional issues (paramTypes extraction in ClassRenderer/StandaloneClassRenderer, and callback argument handling for `in` in MethodInterceptorRenderer) that were not in the original plan but are correct enhancements.

**Bug 3 (Gaps 27/28, out/ref on generic delegates):** Matches plan. Generated delegate for Gap 27 inline stub correctly includes `out T2 b` in the delegate signature. All three model builders (InlineModelBuilder, ClassModelBuilder, StandaloneClassModelBuilder) now include `GetRefKindPrefix(p.RefKind)` in delegate parameter formatting.

**Bug 4 (Gap 31, mixed arity):** Matches plan. Generated code for both standalone and inline stubs now has multiple `Of<>()` methods per interceptor class: `Of<TReturn>()` (arity 1) and `Of<TInput, TReturn>()` (arity 2), with separate `RunTypedHandler` and `RunTypedHandler2` typed handler classes. Tests confirm both arities can be configured and invoked at runtime. The `InlineGenericTypeArityGroup` and `FlatGenericMethodArityGroup` records were created as specified.

### Generated Code Spot-Check

| File | What was verified |
|------|-------------------|
| `Gap26StandaloneKnockOff.g.cs` | `this[in int a]` at line 793 |
| `Gap26InlineTest.Stubs.g.cs` | `this[in int a]` at line 764 |
| `Gap27InlineTest.Stubs.g.cs` | `out T2 b` in delegate and explicit impl |
| `Gap31StandaloneKnockOff.g.cs` | `Of<TReturn>()` and `Of<TInput, TReturn>()` methods |
| `Gap31InlineTest.Stubs.g.cs` | Same dual-arity `Of<>()` methods |
| `GenericTransformServiceStub'1.g.cs` | `SmartDefault<TSmartDefault>` (no collision) |
| `OpenGenericTransformServiceTest.Stubs.g.cs` | `SmartDefault<TSmartDefault>` (no collision) |

### RocksGapReproductionTests.cs Verification

All four gap stubs are uncommented and compiling:
- Gap 26: `Gap26InlineTest` and `Gap26StandaloneKnockOff` -- both uncommented, both compile
- Gap 27: `Gap27InlineTest` -- uncommented, compiles
- Gap 28: `Gap28InlineTest` -- uncommented, compiles
- Gap 31: `Gap31InlineTest` and `Gap31StandaloneKnockOff` -- both uncommented, both compile

Runtime tests verify behavior:
- `Gap31_MixedArity_InlineCanConfigureBothArities` -- configures and invokes both `Of<int>()` and `Of<string, int>()` successfully
- `Gap31_MixedArity_StandaloneCanConfigureBothArities` -- same for standalone pattern

### Design.Stubs Compilation

`GenericTypeGapsVerification.cs` compiles fully with all 4 stub declarations:
- P4: `CacheStub<TKey, TValue>` (line 30) -- compiles
- P2: `GenericTransformServiceStub<T>` (line 42) -- compiles (was CS0693, now fixed)
- P8: `OpenGenericTransformServiceTest` (line 53) -- compiles (was CS0693, now fixed)
- P9: `OpenGenericCacheTest` (line 61) -- compiles
