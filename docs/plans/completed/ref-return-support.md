# Ref Return Support Design

**Date:** 2026-02-08
**Related Todo:** [Add Ref Return Support to Generator](../todos/ref-return-support.md)
**Status:** Verified
**Last Updated:** 2026-02-08

---

## Overview

KnockOff silently strips `ref` and `ref readonly` modifiers from return types on methods, properties, and indexers. The root cause is that `ReturnsByRef` / `ReturnsByRefReadonly` from Roslyn's `IMethodSymbol` / `IPropertySymbol` are never captured in the transform layer. This plan designs support for ref return members across all applicable pipelines.

### Scope

**Member types affected:** Methods, Properties, Indexers (all three support ref returns in C#).

**Patterns affected:** All nine patterns are affected in principle, but ref returns on class members (virtual/abstract) are rare in practice. The design covers all patterns uniformly.

**Events:** Not affected. Events cannot have ref return types.

---

## The Fundamental Design Challenge

Ref returns require returning a reference to a **stable storage location**. The current interceptor pattern computes return values from delegates:

```csharp
// Current pattern - works for value returns:
int IFoo.GetValue() {
    return GetValueInterceptor.Invoke(Strict);  // returns a value
}

// Cannot work for ref returns:
ref int IFoo.GetValue() {
    return ref GetValueInterceptor.Invoke(Strict);  // ERROR: can't return ref to temporary
}
```

A `ref` return must reference a field, array element, or other stable storage. This means interceptors for ref return members need a fundamentally different internal pattern: they must store the computed value in a field and return a reference to that field.

### Design Options Considered

#### Option A: Backing Field in Interceptor (Recommended)

The interceptor stores the computed value in a backing field and the explicit interface implementation returns a ref to that field.

```csharp
// Generated interceptor has a backing field:
public sealed class GetValueRefInterceptor
{
    internal int _refReturnBacking;   // stable storage for ref return

    // Same API as today: Return(value), Return(callback), sequences, etc.
    // InvokeRef writes to _refReturnBacking and returns void
    internal void InvokeRef(bool strict)
    {
        // Same priority chain as Invoke(), but writes to _refReturnBacking
        // instead of returning
        if (_hasReturnValue) { _refReturnBacking = _returnValue; return; }
        if (_call != null) { _refReturnBacking = _call(); return; }
        // ... strict/default fallback
        _refReturnBacking = default;
    }
}

// Explicit interface implementation:
ref int IFoo.GetValueRef()
{
    GetValueRef.InvokeRef(Strict);
    return ref GetValueRef._refReturnBacking;
}
```

**Pros:**
- User-facing API (Return, Call, sequences, verification) stays identical to non-ref methods
- Minimal model changes (add a boolean flag, change Invoke to InvokeRef)
- The backing field is naturally part of the interceptor (scoped, not polluting the stub class)
- Works for all patterns uniformly

**Cons:**
- Returning ref to a field that gets overwritten on next call means users can't hold two ref results simultaneously (this is acceptable for stubs -- the same limitation applies to most real implementations)
- `ref readonly` returns ref to a non-readonly field (safe because the caller sees it as readonly via the interface signature)

#### Option B: Delegate Returns Ref

Make the user-provided callback itself return `ref`:

```csharp
public delegate ref int GetValueRefDelegate();
stub.GetValueRef.Return(ref () => ref someField);
```

**Rejected because:**
- Requires a completely different user API (users must manage their own backing storage)
- `ref` returning lambdas are syntactically awkward and limited (can't capture locals)
- Breaks API consistency -- ref return members would have a fundamentally different usage pattern
- `Func<T>` / custom delegates can't express ref returns, requiring special delegate types

#### Option C: No Interceptor, Just Backing Field

Generate a simple backing field without the interceptor infrastructure:

```csharp
private int _getValueRefBacking;
ref int IFoo.GetValueRef() => ref _getValueRefBacking;
```

**Rejected because:**
- No Return()/Call() API, no sequences, no verification
- Makes ref return members second-class citizens
- Users would have no way to configure behavior

### Selected Approach: Option A (Backing Field in Interceptor)

The interceptor maintains the same user-facing API (Return, Call, sequences, verification) but uses a different internal invocation path. Instead of `return Invoke()`, the implementation calls `InvokeRef()` which writes to a backing field, then returns ref to that field.

For `ref readonly` returns: the implementation signature uses `ref readonly`, but the backing field is a regular mutable field. This is safe because:
1. The caller receives `ref readonly` and cannot write through it
2. The interceptor needs to write to it (that is the whole point)
3. This matches what real implementations typically do

---

## Approach

### Layer 1: Transform (Capture Ref Return Metadata)

Add two boolean fields to the `InterfaceMemberInfo` and `ClassMemberInfo` records:

```csharp
// In InterfaceMemberInfo:
bool ReturnsByRef = false,
bool ReturnsByRefReadonly = false

// In ClassMemberInfo:
bool ReturnsByRef = false,
bool ReturnsByRefReadonly = false
```

**`InterfaceMemberInfo.FromProperty`** changes:
```csharp
// Capture from IPropertySymbol:
var returnsByRef = property.ReturnsByRef;
var returnsByRefReadonly = property.ReturnsByRefReadonly;
```

**`InterfaceMemberInfo.FromMethod`** changes:
```csharp
// Capture from IMethodSymbol:
var returnsByRef = method.ReturnsByRef;
var returnsByRefReadonly = method.ReturnsByRefReadonly;
```

**`ClassMemberInfo.FromProperty`** and **`ClassMemberInfo.FromMethod`** get the same treatment.

These are the only transform-layer changes needed. The existing `ReturnType` string (e.g., `"int"`) stays as-is -- the `ref` / `ref readonly` prefix is a separate concern applied at render time.

### Layer 2: Model (Propagate Flag Through Pipeline)

The ref return flags must flow from the transform records through the builder to the models used by renderers.

**Complete model inventory** (21 model types that need `ReturnsByRef` / `ReturnsByRefReadonly`):

#### Unified models (shared by all renderers):

1. `UnifiedMethodInterceptorModel` (`src/Generator/Model/Shared/UnifiedMethodInterceptorModel.cs`)
2. `UnifiedPropertyInterceptorModel` (`src/Generator/Model/Shared/UnifiedPropertyInterceptorModel.cs`)
3. `UnifiedIndexerInterceptorModel` (`src/Generator/Model/Shared/UnifiedIndexerInterceptorModel.cs`)
4. `MethodOverloadSignature` (`src/Generator/Model/Shared/MethodOverloadSignature.cs`) -- for mixed ref/non-ref overload groups, each signature needs its own ref return flag

#### Flat models:

5. `FlatMethodModel` (`src/Generator/Model/Flat/FlatMethodModel.cs`)
6. `FlatPropertyModel` (`src/Generator/Model/Flat/FlatPropertyModel.cs`)
7. `FlatIndexerModel` (`src/Generator/Model/Flat/FlatIndexerModel.cs`)

#### Inline interface models:

8. `InlineInterfaceImplementation` (`src/Generator/Model/Inline/InlineInterfaceImplementation.cs`)
9. `InlineMethodModel` (`src/Generator/Model/Inline/InlineMethodModel.cs`)
10. `InlinePropertyModel` (`src/Generator/Model/Inline/InlinePropertyModel.cs`)
11. `InlineIndexerModel` (`src/Generator/Model/Inline/InlineIndexerModel.cs`)

#### Inline class models (InlineClassStubModel.cs):

12. `InlineClassPropertyModel` (line 68) -- interceptor property model for class stubs
13. `InlineClassIndexerModel` (line 93) -- interceptor indexer model for class stubs
14. `InlineClassImplMethodModel` (line 239) -- Impl class method override model
15. `InlineClassImplPropertyModel` (line 184) -- Impl class property override model
16. `InlineClassImplIndexerModel` (line 210) -- Impl class indexer override model

#### Standalone class models:

17. `BaseClassMethodModel` (`src/Generator/Model/StandaloneClass/BaseClassMethodModel.cs`)
18. `BaseClassPropertyModel` (`src/Generator/Model/StandaloneClass/BaseClassPropertyModel.cs`)

#### Adapter layer:

19. `ModelAdapters.ToUnifiedModel` (FlatMethodGroup -> UnifiedMethodInterceptorModel) -- must propagate ref return flags
20. `ModelAdapters.ToUnifiedPropertyModel` (FlatPropertyModel/InlinePropertyModel -> UnifiedPropertyInterceptorModel) -- must propagate ref return flags
21. `ModelAdapters.ToUnifiedIndexerModel` (FlatIndexerModel/InlineIndexerModel -> UnifiedIndexerInterceptorModel) -- must propagate ref return flags

**Convenience property:** Add a computed property on each model:
```csharp
bool IsRefReturn => ReturnsByRef || ReturnsByRefReadonly;
string RefReturnPrefix => ReturnsByRef ? "ref " : ReturnsByRefReadonly ? "ref readonly " : "";
```

**Note on `InlineClassMethodModel`:** This model (line 122 of `InlineClassStubModel.cs`) is used for class stub method interceptors. However, methods in class stubs use `UnifiedMethodInterceptorModel` directly (see `InlineClassStubModel.Methods` field). The ref return flags flow through `UnifiedMethodInterceptorModel`, so `InlineClassMethodModel` does not need separate flags -- it is not used for method interceptor rendering.

### Layer 3: Builders (Set the Flags)

Each builder reads the flags from the transform model and sets them on the generation model.

**`FlatModelBuilder`**: When building `FlatMethodModel`, `FlatPropertyModel`, `FlatIndexerModel` from `InterfaceMemberInfo`, propagate `ReturnsByRef` / `ReturnsByRefReadonly`.

**`InlineModelBuilder`**: When building `InlineInterfaceImplementation`, `InlineMethodModel`, `InlinePropertyModel`, `InlineIndexerModel`, and the class stub models (`InlineClassPropertyModel`, `InlineClassIndexerModel`, `InlineClassImplMethodModel`, `InlineClassImplPropertyModel`, `InlineClassImplIndexerModel`), propagate the flags.

**`ClassModelBuilder`** and **`StandaloneClassModelBuilder`**: When building from `ClassMemberInfo`, propagate the flags to all class stub models.

**`UnifiedInterceptorBuilder`**: When building `UnifiedMethodInterceptorModel`, `UnifiedPropertyInterceptorModel`, `UnifiedIndexerInterceptorModel`, propagate the flags.

**`ModelAdapters`**: When converting flat/inline models to unified models in `ToUnifiedModel`, `ToUnifiedPropertyModel`, `ToUnifiedIndexerModel`, propagate the flags. For `BuildMultiOverloadModel`, each `MethodOverloadSignature` must get the ref return flags from the corresponding `FlatMethodModel`.

### Layer 4: Renderers (Emit Correct Code)

This is where the substantive logic changes are.

#### 4a: Explicit Interface Implementations

**Methods** -- In `FlatRenderer.RenderMethodImplementation` and `InlineRenderer.RenderNonGenericMethodImplementation`:

Current:
```csharp
w.Line($"{method.ReturnType} {method.DeclaringInterface}.{method.MethodName}(...)");
// ...
w.Line($"return {method.InterceptorName}.Invoke{suffix}({invokeArgs});");
```

New (when `IsRefReturn`):
```csharp
w.Line($"{method.RefReturnPrefix}{method.ReturnType} {method.DeclaringInterface}.{method.MethodName}(...)");
// ...
w.Line($"{method.InterceptorName}.InvokeRef{suffix}({invokeArgs});");
w.Line($"return ref {method.InterceptorName}._refReturnBacking;");
```

**Properties** -- In `FlatRenderer.RenderPropertyImplementation` and `InlineRenderer.RenderPropertyImplementation`:

Current:
```csharp
w.Line($"{prop.ReturnType} {prop.DeclaringInterface}.{prop.MemberName}");
// get => InterceptorName.InvokeGet(Strict);
```

New (when `IsRefReturn`):
```csharp
w.Line($"{prop.RefReturnPrefix}{prop.ReturnType} {prop.DeclaringInterface}.{prop.MemberName}");
// get { InterceptorName.InvokeRefGet(Strict); return ref InterceptorName._refReturnBacking; }
```

Note: Ref return properties are always get-only (C# constraint: `ref T Prop { get; }` -- no setter allowed).

**Indexers** -- Same pattern as properties but with key parameters:

```csharp
w.Line($"{indexer.RefReturnPrefix}{indexer.ReturnType} {indexer.DeclaringInterface}.this[...]");
// get { InterceptorName.InvokeRefGet(Strict, key); return ref InterceptorName._refReturnBacking; }
```

#### 4b: Interceptor Classes

**MethodInterceptorRenderer** -- When `model.IsRefReturn`:

1. Add backing field: `internal {ReturnType} _refReturnBacking;`
2. Generate `InvokeRef(...)` instead of `Invoke(...)` -- a simplified version of the Invoke priority chain that writes to `_refReturnBacking` instead of returning, and **skips all async-related branches** (see Concern 2 Resolution below)
3. Return(value), Return(callback), sequences, When chains, verification -- all work as before. The difference is only in how the value reaches the caller.
4. The `Return(value)` overload stores the value; `InvokeRef` writes it to `_refReturnBacking`
5. The `Return(callback)` overload stores the delegate; `InvokeRef` invokes it and writes result to `_refReturnBacking`

**PropertyInterceptorRenderer** -- When `model.IsRefReturn`:

1. Add backing field: `internal {ValueType} _refReturnBacking;`
2. Generate `InvokeRefGet(...)` -- same as `InvokeGet` but writes to `_refReturnBacking` instead of returning
3. Get() callback API unchanged

**IndexerInterceptorRenderer** -- When `model.IsRefReturn`:

1. Add backing field: `internal {ValueType} _refReturnBacking;`
2. Generate `InvokeRefGet(...)` -- same as `InvokeGet` but writes to `_refReturnBacking` instead of returning

#### 4c: Class Stubs (override pattern)

For class stubs (patterns 3, 4, 6, 9), the Impl class overrides virtual/abstract members. The pattern differs between abstract and virtual members for ref returns (see Concern 3 Resolution below).

**Abstract ref return methods:**
```csharp
public override ref int GetValueRef()
{
    if (_stub == null) { _defaultRefBacking = default!; return ref _defaultRefBacking; }
    _stub.GetValueRef.InvokeRef(_stub.Strict);
    return ref _stub.GetValueRef._refReturnBacking;
}
```

**Virtual ref return methods (IsConfigured-first pattern):**
```csharp
public override ref int GetValueRef()
{
    if (_stub == null) return ref base.GetValueRef();
    if (_stub.GetValueRef.IsConfigured)
    {
        _stub.GetValueRef.InvokeRef(_stub.Strict);
        return ref _stub.GetValueRef._refReturnBacking;
    }
    _stub.GetValueRef.InvokeRef(_stub.Strict); // tracks unconfigured call
    return ref base.GetValueRef();
}
```

**Abstract ref return properties:**
```csharp
public override ref int Value
{
    get
    {
        if (_stub == null) { _defaultRefBacking = default!; return ref _defaultRefBacking; }
        _stub.Value.InvokeRefGet(_stub.Strict);
        return ref _stub.Value._refReturnBacking;
    }
}
```

**Virtual ref return properties (IsConfigured-first pattern):**
```csharp
public override ref int Value
{
    get
    {
        if (_stub == null) return ref base.Value;
        if (_stub.Value.IsConfigured)
        {
            _stub.Value.InvokeRefGet(_stub.Strict);
            return ref _stub.Value._refReturnBacking;
        }
        _stub.Value.InvokeRefGet(_stub.Strict); // tracks unconfigured call
        return ref base.Value;
    }
}
```

**Indexers** follow the same pattern as properties.

**The `_defaultRefBacking` field:** For abstract members in the `_stub == null` path (calls during base constructor), we need a stable storage location. Add a single `private T _defaultRefBacking;` field per ref-return-type to the Impl class. This is only used during the brief window when `_stub` is null.

---

## Concern Resolutions

### Concern 1 Resolution: Complete Model Inventory

**Resolution:** The plan's Layer 2 section has been updated with a complete inventory of all 21 model types that need ref return flags. This includes:

- 3 InlineClassImpl* models (ImplMethodModel, ImplPropertyModel, ImplIndexerModel) -- used by ClassRenderer and StandaloneClassRenderer for override signatures
- 2 InlineClass* models (InlineClassPropertyModel, InlineClassIndexerModel) -- used for class stub interceptor rendering
- ModelAdapters.cs -- 3 adapter methods that must propagate flags when converting flat/inline models to unified models
- MethodOverloadSignature -- needs per-signature ref return flags for mixed overload groups

**Note on `InlineClassMethodModel`:** This record exists in `InlineClassStubModel.cs` (line 122) but is NOT used for method interceptor rendering. Class stub methods use `UnifiedMethodInterceptorModel` directly (the `InlineClassStubModel.Methods` field is `EquatableArray<UnifiedMethodInterceptorModel>`). Therefore `InlineClassMethodModel` does not need ref return flags, but `InlineClassImplMethodModel` does (it controls the override signature in the Impl class).

### Concern 2 Resolution: InvokeRef Skips Async Branches

**Resolution:** `InvokeRef` is a **simplified** version of `Invoke` that explicitly skips all async-related steps.

The full Invoke method in `MethodInterceptorRenderer.RenderInvokeMethod` has 13 steps. Here is the mapping for InvokeRef:

| Step | Invoke | InvokeRef | Rationale |
|------|--------|-----------|-----------|
| 1. Out param initialization | Yes | Yes | Ref return + out params are orthogonal |
| 2. When chain check (value) | Yes | **Yes** (modified) | Writes to `_refReturnBacking` instead of `return` |
| 3. When chain check (void) | Yes | No | Ref return methods are never void |
| 4. Sequence check | Yes | **Yes** (modified) | Writes callback result to `_refReturnBacking` |
| 5. Return value check | Yes | **Yes** (modified) | Writes `_returnValue` to `_refReturnBacking`. **No Task/ValueTask wrapping.** |
| 6. Repeating callback check | Yes | **Yes** (modified) | Writes callback result to `_refReturnBacking` |
| 7. Simplified callback (Task/ValueTask) | Yes | **SKIP** | C# prohibits async ref returns |
| 8. Simplified void callback (Task/ValueTask) | Yes | **SKIP** | C# prohibits async ref returns |
| 9. Unconfigured tracking | Yes | Yes | Identical |
| 10. Sequence exhaustion | Yes | **Yes** (modified) | Same logic but writes to `_refReturnBacking` instead of `return` |
| 11. User method fallback | Yes | **Yes** (modified) | Writes user method result to `_refReturnBacking` |
| 12. Source delegation | Yes | **Yes** (modified) | Copies source result to `_refReturnBacking` (lossy ref redirection, acceptable for stubs) |
| 13. Strict mode / default | Yes | **Yes** (modified) | Writes `default` to `_refReturnBacking` |

**Key simplifications:**
- Steps 7-8 are completely eliminated (async handling impossible for ref returns)
- Step 5 never wraps in Task.FromResult/ValueTask -- direct assignment to `_refReturnBacking`
- All `return value;` statements become `_refReturnBacking = value; return;`
- All `return callback(args);` become `_refReturnBacking = callback(args); return;`
- The method signature is `internal void InvokeRef(...)` instead of `internal T Invoke(...)`

**For PropertyInterceptorRenderer (`InvokeRefGet`):** Same simplification applies. The property priority chain has no async branches, so InvokeRefGet is a more direct transformation -- every `return value;` becomes `_refReturnBacking = value; return;`.

**For IndexerInterceptorRenderer (`InvokeRefGet`):** Same as property -- no async branches. Direct transformation.

### Concern 3 Resolution: Virtual Ref Return Override Pattern

**Resolution:** Use the **IsConfigured-first pattern** for virtual ref return overrides in class stubs.

**Problem:** The current virtual override pattern stores `Invoke()` result in a local, then conditionally falls back to `base.Method()`:

```csharp
// Current pattern (non-ref virtual methods):
var result = _stub.Handler.Invoke(args);
if (_stub.Handler.UnconfiguredCallCount > unconfiguredBefore)
    return base.Method(args);
return result;
```

This cannot work for ref returns because `ref` locals cannot be conditionally reassigned between two different storage locations.

**Solution:** Check `IsConfigured` before calling `InvokeRef`, similar to how virtual property overrides already work:

```csharp
// Ref return virtual method pattern:
if (_stub == null) return ref base.GetValueRef();
if (_stub.GetValueRef.IsConfigured)
{
    _stub.GetValueRef.InvokeRef(_stub.Strict);
    return ref _stub.GetValueRef._refReturnBacking;
}
// Not configured: still call InvokeRef for tracking, then fall back to base
_stub.GetValueRef.InvokeRef(_stub.Strict);
return ref base.GetValueRef();
```

**Why this is consistent with existing patterns:**

The virtual property override already uses this exact pattern (see `ClassRenderer.RenderImplPropertyOverride` line 699 and `StandaloneClassRenderer.RenderImplPropertyOverride` line 708):

```csharp
// Existing virtual property override:
if (_stub == null) return base.PropertyName;
if (_stub.PropertyName.IsConfigured) return _stub.PropertyName.InvokeGet(_stub.Strict);
_stub.PropertyName.InvokeGet(_stub.Strict); // tracks unconfigured
return base.PropertyName;
```

The ref return version follows the same structure but uses `InvokeRef` + `return ref backing` for the configured path and `return ref base.Member()` for the unconfigured path.

**For abstract members:** No fallback is needed. Abstract members always use `InvokeRef` directly:

```csharp
if (_stub == null) { _defaultRefBacking = default!; return ref _defaultRefBacking; }
_stub.GetValueRef.InvokeRef(_stub.Strict);
return ref _stub.GetValueRef._refReturnBacking;
```

The `_defaultRefBacking` field provides stable storage for the brief `_stub == null` window during base constructor execution.

**For user method overrides + ref return:** Deferred to a follow-up. User method overrides would need to return `ref T`, which does not compose well with the generated base class pattern. The `HasUserOverride` path in `StandaloneClassRenderer.RenderImplMethodOverride` is not applicable to ref return methods.

---

## Generated Code Examples

### Standalone Interface Pattern (Pattern 1)

Given:
```csharp
public interface IRefService { ref int GetValueRef(); }

[KnockOff]
public partial class RefServiceKnockOff : IRefService { }
```

Generated:
```csharp
public partial class RefServiceKnockOff : IRefService
{
    public GetValueRefInterceptor GetValueRef { get; } = new();

    ref int IRefService.GetValueRef()
    {
        GetValueRef.InvokeRef(Strict);
        return ref GetValueRef._refReturnBacking;
    }

    public sealed class GetValueRefInterceptor
    {
        internal int _refReturnBacking;

        // ... Return(), Call(), sequences, verification ...
        // (same API as non-ref methods)

        internal void InvokeRef(bool strict)
        {
            // Priority chain (simplified -- no async branches):
            // When chain > sequence > return value > callback > unconfigured tracking >
            // sequence exhaustion > user method > source > strict > default
            //
            // All steps write to _refReturnBacking instead of returning
            if (_hasReturnValue && _returnValueTracking != null)
            {
                _returnValueTracking.RecordCall();
                _refReturnBacking = _returnValue;
                return;
            }
            if (_call != null && _callTracking != null)
            {
                _callTracking.RecordCall();
                _refReturnBacking = _call();
                return;
            }
            _unconfiguredCallCount++;
            if (strict) throw StubException.NotConfigured("", "GetValueRef");
            _refReturnBacking = default;
        }
    }
}
```

### Inline Interface Pattern (Pattern 5)

Given:
```csharp
[KnockOff<IRefService>]
public partial class MyTest { }
```

Generated stub class:
```csharp
public class IRefService : global::IRefService, IKnockOffStub
{
    public GetValueRefInterceptor GetValueRef { get; } = new();

    ref int global::IRefService.GetValueRef()
    {
        GetValueRef.InvokeRef(Strict);
        return ref GetValueRef._refReturnBacking;
    }

    // GetValueRefInterceptor: same as standalone version
}
```

### Class Stub Pattern (Patterns 3, 6) -- Abstract Method

Given:
```csharp
public abstract class RefServiceBase
{
    public abstract ref int GetValueRef();
}

[KnockOff<RefServiceBase>]  // or [KnockOffBase<RefServiceBase>]
public partial class MyTest { }
```

Generated Impl class override:
```csharp
private class RefServiceBase_Generated : global::RefServiceBase
{
    private int _defaultRefBacking; // for _stub == null path

    public override ref int GetValueRef()
    {
        if (_stub == null) { _defaultRefBacking = default!; return ref _defaultRefBacking; }
        _stub.GetValueRef.InvokeRef(_stub.Strict);
        return ref _stub.GetValueRef._refReturnBacking;
    }
}
```

### Class Stub Pattern -- Virtual Method

Given:
```csharp
public abstract class MixedBase
{
    private int _backing = 42;
    public virtual ref int GetValueRef() => ref _backing;
}
```

Generated Impl class override:
```csharp
public override ref int GetValueRef()
{
    if (_stub == null) return ref base.GetValueRef();
    if (_stub.GetValueRef.IsConfigured)
    {
        _stub.GetValueRef.InvokeRef(_stub.Strict);
        return ref _stub.GetValueRef._refReturnBacking;
    }
    _stub.GetValueRef.InvokeRef(_stub.Strict); // tracks unconfigured
    return ref base.GetValueRef();
}
```

### Ref Return Property

Given:
```csharp
public interface IRefProps { ref int Value { get; } }
```

Generated:
```csharp
ref int IRefProps.Value
{
    get
    {
        Value.InvokeRefGet(Strict);
        return ref Value._refReturnBacking;
    }
}

public sealed class ValueInterceptor
{
    internal int _refReturnBacking;

    // Get() API unchanged

    internal void InvokeRefGet(bool strict)
    {
        // Same priority chain as InvokeGet, but writes to _refReturnBacking
    }
}
```

### Ref Readonly Return

Given:
```csharp
public interface IRefReadonly { ref readonly int Value { get; } }
```

Generated:
```csharp
ref readonly int IRefReadonly.Value
{
    get
    {
        Value.InvokeRefGet(Strict);
        return ref Value._refReturnBacking;
    }
}
```

The `ref readonly` in the return type means the *caller* cannot write through it. The backing field itself is mutable (the interceptor writes to it). The C# compiler is fine with `return ref field` inside a `ref readonly` getter -- it restricts the caller, not the implementation.

---

## Scope Table

| Pattern | Methods | Properties | Indexers | Notes |
|---------|---------|------------|----------|-------|
| 1. Standalone | Yes | Yes | Yes | Primary focus |
| 2. Generic Standalone | Yes | Yes | Yes | Same pipeline as Standalone |
| 3. Standalone Class | Yes | Yes | Yes | Override in Impl class (IsConfigured-first pattern for virtual) |
| 4. Generic Standalone Class | Yes | Yes | Yes | Same pipeline as Standalone Class |
| 5. Inline Interface | Yes | Yes | Yes | Primary focus |
| 6. Inline Class | Yes | Yes | Yes | Override in Impl class (IsConfigured-first pattern for virtual) |
| 7. Inline Delegate | N/A | N/A | N/A | Delegates cannot have ref returns |
| 8. Open Generic Interface | Yes | Yes | Yes | Same pipeline as Inline Interface |
| 9. Open Generic Class | Yes | Yes | Yes | Same pipeline as Inline Class |

### C# Language Constraints on Ref Returns

- Ref return properties are always get-only (no setter)
- Ref return indexers are always get-only (no setter)
- Ref return methods can have any parameter types (including ref/out params)
- `async` methods cannot return by ref
- Iterator methods cannot return by ref
- These constraints are enforced by the C# compiler; the generator does not need to validate them

---

## Implementation Phases

### Phase 1: Transform + Model

**Goal:** Capture ref return metadata from Roslyn symbols and propagate through all models.

Files to modify:
- `src/Generator/Models/InterfaceModels.cs` -- Add `ReturnsByRef`, `ReturnsByRefReadonly` to `InterfaceMemberInfo`
- `src/Generator/Models/ClassModels.cs` -- Add same to `ClassMemberInfo`
- `src/Generator/Model/Shared/UnifiedMethodInterceptorModel.cs` -- Add flags
- `src/Generator/Model/Shared/UnifiedPropertyInterceptorModel.cs` -- Add flags
- `src/Generator/Model/Shared/UnifiedIndexerInterceptorModel.cs` -- Add flags
- `src/Generator/Model/Shared/MethodOverloadSignature.cs` -- Add flags per signature
- `src/Generator/Model/Flat/FlatMethodModel.cs` -- Add flags
- `src/Generator/Model/Flat/FlatPropertyModel.cs` -- Add flags
- `src/Generator/Model/Flat/FlatIndexerModel.cs` -- Add flags
- `src/Generator/Model/Inline/InlineInterfaceImplementation.cs` -- Add flags
- `src/Generator/Model/Inline/InlineMethodModel.cs` -- Add flags
- `src/Generator/Model/Inline/InlinePropertyModel.cs` -- Add flags
- `src/Generator/Model/Inline/InlineIndexerModel.cs` -- Add flags
- `src/Generator/Model/Inline/InlineClassStubModel.cs` -- Add flags to `InlineClassPropertyModel`, `InlineClassIndexerModel`, `InlineClassImplMethodModel`, `InlineClassImplPropertyModel`, `InlineClassImplIndexerModel`
- `src/Generator/Model/StandaloneClass/BaseClassMethodModel.cs` -- Add flags
- `src/Generator/Model/StandaloneClass/BaseClassPropertyModel.cs` -- Add flags

**Verification gate:** All existing tests still pass. Flags default to false.

### Phase 2: Builders + Adapters

**Goal:** Wire the flags from transform models to generation models in all builders and adapters.

Files to modify:
- `src/Generator/Builder/FlatModelBuilder.cs`
- `src/Generator/Builder/InlineModelBuilder.cs`
- `src/Generator/Builder/ClassModelBuilder.cs`
- `src/Generator/Builder/StandaloneClassModelBuilder.cs`
- `src/Generator/Builder/UnifiedInterceptorBuilder.cs`
- `src/Generator/Renderer/Shared/ModelAdapters.cs` -- Propagate flags in `ToUnifiedModel`, `ToUnifiedPropertyModel`, `ToUnifiedIndexerModel`, and `BuildMultiOverloadModel` (per-signature flags)

**Verification gate:** All existing tests still pass. Ref return flags are now populated (can verify with debugger or diagnostic).

### Phase 3: Interceptor Renderers

**Goal:** Generate `_refReturnBacking` field and `InvokeRef` / `InvokeRefGet` methods.

Files to modify:
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs`
- `src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs`
- `src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs`

**Approach:** When the model has `IsRefReturn`:
1. Emit `internal {type} _refReturnBacking;` field
2. For methods: Emit `InvokeRef()` -- simplified Invoke that skips async branches (steps 7-8), writes to `_refReturnBacking` instead of returning
3. For properties: Emit `InvokeRefGet()` -- direct transformation of InvokeGet, writing to `_refReturnBacking`
4. For indexers: Emit `InvokeRefGet()` -- same as property version with key parameters

**Verification gate:** All existing tests still pass (non-ref members unaffected). Ref return interceptors now generate correct code.

### Phase 4: Implementation Renderers

**Goal:** Generate correct explicit interface implementations and class overrides for ref return members.

Files to modify:
- `src/Generator/Renderer/FlatRenderer.cs` -- Method, property, indexer implementations
- `src/Generator/Renderer/InlineRenderer.cs` -- Method, property, indexer implementations
- `src/Generator/Renderer/ClassRenderer.cs` -- Class stub overrides (abstract: direct InvokeRef; virtual: IsConfigured-first pattern)
- `src/Generator/Renderer/StandaloneClassRenderer.cs` -- Standalone class stub overrides (same patterns as ClassRenderer)

**Approach:**
1. Prefix return type with `ref ` or `ref readonly ` in the member signature
2. For interface methods: call `InvokeRef()`, then `return ref interceptor._refReturnBacking`
3. For interface properties/indexers: call `InvokeRefGet()`, then `return ref interceptor._refReturnBacking`
4. For class stub abstract overrides: null check with `_defaultRefBacking`, then `InvokeRef` + `return ref _refReturnBacking`
5. For class stub virtual overrides: null check -> `return ref base.Member()`; IsConfigured check -> `InvokeRef` + `return ref _refReturnBacking`; else -> `InvokeRef` (tracking) + `return ref base.Member()`

**Verification gate:** Exploratory tests compile. 120 compilation errors resolved.

### Phase 5: Tests and Design Projects

**Goal:** Get all exploratory tests passing. Verify Design.Stubs compile. Verify Design.Tests pass.

Files:
- `src/Tests/KnockOffTests/RefReturnTests.cs` -- Verify all tests pass (class stub tests included)
- `src/Tests/KnockOffTests/TestInterfaces.cs` -- Ref return base class + class stub declarations (already added by architect)
- `src/Design/Design.Domain/Services/IRefReturnService.cs` -- Ref return interfaces (already added by architect)
- `src/Design/Design.Domain/Abstractions/RefReturnBase.cs` -- Ref return abstract class (already added by architect)
- `src/Design/Design.Stubs/Methods/RefReturns.cs` -- Ref return stub declarations (already added by architect)
- `src/Design/Design.Tests/MethodTests/RefReturnTests.cs` -- Ref return tests (already added by architect)

---

## Acceptance Criteria

1. All 120+ compilation errors from exploratory tests are resolved
2. All existing tests continue to pass
3. Ref return methods support: Return(value), Return(callback), Call(callback), sequences (ThenReturn/ThenCall), When chains, verification (Verify/Called)
4. Ref return properties support: Get(callback), Get(value), sequences (ThenGet), VerifyGet
5. Ref return indexers support: Get(callback), Get(value), sequences (ThenGet), VerifyGet
6. Mixed interfaces (normal + ref return members) compile and work correctly
7. Ref readonly returns emit `ref readonly` in the member signature
8. Class stub overrides (abstract and virtual) for ref return members compile and work correctly
9. Design.Stubs compile with ref return examples
10. Design.Tests pass for ref return examples

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Backing field overwritten between calls | Medium | Low | Acceptable for stubs -- mirrors real-world behavior. Document in comments. |
| `ref readonly` returning ref to mutable field | Low | Low | Valid C# -- the readonly constraint is on the caller, not the implementation. |
| Async methods with ref return | None | None | C# compiler prevents this -- no action needed. InvokeRef skips async branches. |
| Generic methods with ref return | Low | Medium | InvokeRef pattern works the same for generic handlers. Verify with test. |
| User method override + ref return (standalone stubs) | Medium | Medium | Deferred to follow-up. User override methods would need to return ref. |
| Overload groups with mixed ref/non-ref returns | Low | Low | Each overload's MethodOverloadSignature carries its own ref return flag. |
| `_defaultRefBacking` field type in Impl class | Low | Low | One field per unique ref-return type. May need multiple fields if multiple abstract ref return members have different types. |

---

## Edge Cases

1. **Mixed interfaces:** An interface with both `int GetValue()` and `ref int GetValueRef()` -- both members generate correctly, each with its own interceptor and invocation pattern.

2. **ref return + ref/out parameters:** `ref int Process(ref int a, out string b)` -- the ref return uses `InvokeRef` with backing field; the ref/out parameters use the existing custom delegate pattern. These are orthogonal.

3. **Overload groups with ref return:** If a method name has overloads where some return by ref and some don't, each overload's `MethodOverloadSignature` carries its own ref return flags. This determines whether that overload uses `Invoke` or `InvokeRef` suffix and whether the backing field is generated.

4. **Source(T) delegation with ref return:** When `_source` is set and the source method returns by ref, we cannot chain the ref through (the source's ref points to its own storage). The interceptor must call the source, copy the value to `_refReturnBacking`, and return ref to `_refReturnBacking`. This is slightly lossy (the ref no longer points to the original storage) but acceptable for stubs.

5. **Default values for ref return:** When no callback is configured, `_refReturnBacking = default` is correct for value types. For reference types, `_refReturnBacking = default!` works.

6. **User method override + ref return:** For standalone stubs with user method overrides (e.g., `protected override ref int GetValueRef_()`), the user method would need to return by ref. This may not be practical since the override mechanism uses regular return types. **Recommendation: defer user method override support for ref return methods to a follow-up. Document this as a known limitation.**

7. **Virtual ref return override with `_stub == null`:** During base constructor execution, `_stub` is null. For virtual members, we return `ref base.Member()` which is safe (base is fully constructed at this point). For abstract members, we use `_defaultRefBacking` as stable storage.

8. **Multiple abstract ref return members with different types:** The Impl class may need multiple `_defaultRefBacking` fields, one per unique type. The renderer should generate `_defaultRefBacking_{MemberName}` to avoid naming conflicts.

---

## Codebase Analysis

### Files Examined

| File | Purpose | Key Findings |
|------|---------|--------------|
| `src/Generator/KnockOffGenerator.Transform.cs` | Transform layer | `InterfaceMemberInfo.FromMethod` and `FromProperty` are called here. Neither captures `ReturnsByRef` / `ReturnsByRefReadonly`. |
| `src/Generator/Models/InterfaceModels.cs` | Transform models | `InterfaceMemberInfo` record -- needs two new boolean fields. `FromMethod` and `FromProperty` factory methods need to read the Roslyn symbols. |
| `src/Generator/Models/ClassModels.cs` | Class transform models | `ClassMemberInfo` record -- same changes needed. |
| `src/Generator/Model/Shared/UnifiedMethodInterceptorModel.cs` | Unified method model | Needs `ReturnsByRef` / `ReturnsByRefReadonly` fields. |
| `src/Generator/Model/Shared/UnifiedPropertyInterceptorModel.cs` | Unified property model | Same. |
| `src/Generator/Model/Shared/UnifiedIndexerInterceptorModel.cs` | Unified indexer model | Same. |
| `src/Generator/Model/Shared/MethodOverloadSignature.cs` | Overload signatures | Needs per-signature ref return flags for mixed ref/non-ref overload groups. |
| `src/Generator/Model/Flat/*.cs` | Flat pipeline models | `FlatMethodModel`, `FlatPropertyModel`, `FlatIndexerModel` -- all need flags. |
| `src/Generator/Model/Inline/*.cs` | Inline pipeline models | `InlineInterfaceImplementation`, `InlineMethodModel`, `InlinePropertyModel`, `InlineIndexerModel` -- all need flags. |
| `src/Generator/Model/Inline/InlineClassStubModel.cs` | Class stub models | Contains `InlineClassPropertyModel`, `InlineClassIndexerModel`, `InlineClassImplMethodModel`, `InlineClassImplPropertyModel`, `InlineClassImplIndexerModel` -- all need flags except `InlineClassMethodModel` (methods use `UnifiedMethodInterceptorModel` directly). |
| `src/Generator/Renderer/FlatRenderer.cs` | Flat renderer | Lines 1995, 2104, 2159 -- explicit implementations use `ReturnType` directly. Need `RefReturnPrefix`. |
| `src/Generator/Renderer/InlineRenderer.cs` | Inline renderer | Lines 1126, 1168, 1213 -- same pattern, needs `RefReturnPrefix`. |
| `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` | Method interceptor | Line 650: `Invoke()` returns the value. Need alternative `InvokeRef()` that writes to backing field. 13 steps analyzed; steps 7-8 (async) skipped for InvokeRef. |
| `src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs` | Property interceptor | Line 401: `InvokeGet()` returns the value. Need `InvokeRefGet()`. No async branches -- direct transformation. |
| `src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs` | Indexer interceptor | Line 187: `InvokeGet()` returns the value. Need `InvokeRefGet()`. No async branches. |
| `src/Generator/Renderer/ClassRenderer.cs` | Class renderer | Lines 675-788: Property override uses IsConfigured pattern. Lines 733-788: Indexer override uses IsConfigured pattern. Lines 790-894: Method override uses UnconfiguredCallCount pattern. For ref returns, method overrides switch to IsConfigured-first pattern (consistent with properties). |
| `src/Generator/Renderer/StandaloneClassRenderer.cs` | Standalone class renderer | Lines 668-763: Property overrides. Lines 765-813: Indexer overrides. Lines 815-914: Method overrides. Same patterns as ClassRenderer. |
| `src/Generator/Renderer/Shared/ModelAdapters.cs` | Model adapters | Converts flat/inline models to unified models. Must propagate ref return flags in `ToUnifiedModel`, `ToUnifiedPropertyModel`, `ToUnifiedIndexerModel`. |
| `src/Tests/KnockOffTests/TestInterfaces.cs` | Test interfaces | Lines 529-592: Four ref return interfaces + four standalone stubs. |
| `src/Tests/KnockOffTests/RefReturnTests.cs` | Exploratory tests | 4 inline stubs + comprehensive test class covering standalone and inline patterns for methods, properties, indexers, and mixed interfaces. |
| `src/Design/Design.Stubs/Methods/RefOutParameters.cs` | Design stubs | Ref/out parameters are supported. No ref returns yet. |

---

## Architectural Verification

### Design Project Verification

Ref return Design project code has been added by the architect as failing acceptance criteria:

- `src/Design/Design.Domain/Services/IRefReturnService.cs` -- Ref return interfaces
- `src/Design/Design.Domain/Abstractions/RefReturnBase.cs` -- Ref return abstract class for class stubs
- `src/Design/Design.Stubs/Methods/RefReturns.cs` -- Stub declarations for all applicable patterns
- `src/Design/Design.Tests/MethodTests/RefReturnTests.cs` -- Tests exercising ref return stubs

**Status:** These files will not compile until the generator implements ref return support. The compiler errors serve as acceptance criteria.

### Test Project Verification

Ref return test code has been added by the architect:

- `src/Tests/KnockOffTests/TestInterfaces.cs` -- `RefReturnServiceBase` abstract class + `RefReturnServiceBaseKnockOff` standalone class stub
- `src/Tests/KnockOffTests/RefReturnTests.cs` -- Class stub tests for inline and standalone patterns

**Status:** These tests will not compile until the generator implements ref return support. The compiler errors serve as acceptance criteria.

### Breaking Changes

**None.** All changes are additive:
- New boolean fields default to `false` on existing models
- New `InvokeRef` / `InvokeRefGet` methods are only generated for ref return members
- Existing non-ref members are completely unaffected

### Pattern Consistency

The design maintains API consistency: ref return members expose the same interceptor API (Return, Call, sequences, verification) as non-ref members. The only difference is internal (how the value reaches the caller). Users should not need to learn any new concepts.

### Diagnostic Requirements

No new diagnostics are needed. The C# compiler already enforces ref return constraints (no async, no iterators, get-only for properties/indexers). If a user declares an interface with invalid ref return signatures, the compiler catches it before the generator runs.

### Test Strategy

1. **Existing exploratory tests** (`RefReturnTests.cs`) cover standalone and inline patterns for methods, properties, indexers, mixed interfaces, **and class stubs**
2. **Design.Stubs** examples cover all applicable patterns
3. **Design.Tests** verify the stubs work correctly
4. **Existing test suite** must continue to pass (regression check)
5. **Edge case tests** for: sequences on ref return methods, verification on ref return properties, mixed ref/non-ref interfaces

---

## Open Questions

1. **User method overrides + ref return:** Deferred to follow-up. User method overrides would need to return `ref T`, which does not compose well with the generated base class pattern.

2. **Source(T) delegation:** When delegating to a source object, the source's ref return points to its own storage. Copying the value to the interceptor's backing field means the ref is "redirected." This is acceptable for stubs.

3. **`_defaultRefBacking` naming for multiple types:** If an Impl class has abstract ref return members of different types, it needs multiple `_defaultRefBacking` fields. Use `_defaultRefBacking_{MemberName}` to disambiguate.

---

## Architectural Verification Checklist

- [x] All nine patterns analyzed
- [x] Design.Stubs code added as acceptance criteria (will fail until implemented)
- [x] KnockOffTests code added as acceptance criteria (will fail until implemented)
- [x] Breaking changes assessment completed (None)
- [x] Pattern consistency verified (Same API across all patterns)
- [x] Diagnostic requirements identified (None needed)
- [x] Test strategy defined
- [x] Edge cases documented (including virtual override pattern and _defaultRefBacking)
- [x] Codebase deep-dive completed
- [x] Complete model inventory documented (21 types)
- [x] InvokeRef step-by-step mapping documented (13 steps, 2 skipped)
- [x] Virtual ref return override pattern designed (IsConfigured-first)
- [x] Developer concerns 1-3 addressed

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-02-08

### Re-Review Summary

All three original concerns have been addressed. Approval granted with implementation contract below.

### My Understanding of This Plan

**Core Change:** Add ref return support (ref T, ref readonly T) for methods, properties, and indexers across all KnockOff patterns. The interceptor pattern changes: instead of `return Invoke()`, ref return members use `InvokeRef()` which writes to a backing field, then `return ref _refReturnBacking`.

**User-Facing API:** Unchanged. Users still use Return(value), Call(callback), sequences, and verification. The difference is entirely internal to how values reach the caller.

**Internal Changes:** (1) Capture ReturnsByRef/ReturnsByRefReadonly in transform models, (2) propagate through all pipeline models, (3) generate InvokeRef/InvokeRefGet methods in interceptors with backing fields, (4) emit ref/ref readonly prefixed return types in explicit implementations and class overrides.

**Patterns Affected:** All nine (with Inline Delegate being N/A since delegates cannot have ref returns).

### Concern 1 Resolution Verification: Complete Model Inventory (BLOCKING)

**Verdict: Adequately resolved.**

The plan now lists 21 model types in Layer 2 (section "Complete model inventory"). I verified against the codebase:

- `InlineClassImplMethodModel` (line 239 of InlineClassStubModel.cs) -- confirmed, needs `ReturnsByRef`/`ReturnsByRefReadonly` for override signature rendering
- `InlineClassImplPropertyModel` (line 184) -- confirmed, needs flags for property override signature
- `InlineClassImplIndexerModel` (line 210) -- confirmed, needs flags for indexer override signature
- `InlineClassPropertyModel` (line 68) -- confirmed, needs flags for interceptor rendering
- `InlineClassIndexerModel` (line 93) -- confirmed, needs flags for interceptor rendering
- `ModelAdapters.cs` methods -- confirmed, `ToUnifiedModel`, `ToUnifiedPropertyModel`, `ToUnifiedIndexerModel` must propagate
- `MethodOverloadSignature` -- confirmed, needs per-signature flags for mixed overload groups
- The note about `InlineClassMethodModel` (line 122) NOT needing flags is correct: `InlineClassStubModel.Methods` is `EquatableArray<UnifiedMethodInterceptorModel>`, so method interceptors flow through unified models directly

### Concern 2 Resolution Verification: InvokeRef Step Mapping (MODERATE)

**Verdict: Adequately resolved.**

The plan now provides a 13-step mapping table showing exactly which Invoke steps apply to InvokeRef. I verified:

- Steps 7-8 (simplified callback for Task/ValueTask) are correctly marked SKIP -- C# prohibits `async ref` returns
- Step 5 (return value check) correctly notes no Task/ValueTask wrapping -- direct assignment to `_refReturnBacking`
- All `return value;` statements become `_refReturnBacking = value; return;`
- The InvokeRef signature is `internal void InvokeRef(...)` instead of `internal T Invoke(...)`
- Property/Indexer InvokeRefGet is a simpler transformation since properties have no async branches

### Concern 3 Resolution Verification: Virtual Ref Return Override Pattern (MODERATE)

**Verdict: Adequately resolved.**

The plan adopts the IsConfigured-first pattern for virtual ref return overrides. I verified this is consistent with the existing codebase:

- `ClassRenderer.cs` line 697-702: Virtual property override already uses `if (IsConfigured) return InvokeGet(); ... return base.Prop;`
- The plan's virtual ref return method pattern follows the same structure: check `_stub == null`, check `IsConfigured`, then unconditional fallback
- For abstract members, the `_defaultRefBacking` field provides stable storage during the `_stub == null` window (base constructor)
- The plan correctly defers user method override + ref return to a follow-up (documented in Open Questions and Edge Cases)

### Codebase Investigation

**Files Examined:**
- `src/Generator/Models/InterfaceModels.cs` -- `InterfaceMemberInfo` record confirmed: no `ReturnsByRef`/`ReturnsByRefReadonly` fields exist. `FromProperty` and `FromMethod` factory methods do not read these Roslyn properties. This is the root cause.
- `src/Generator/Models/ClassModels.cs` -- `ClassMemberInfo` record confirmed: same gap. `FromProperty` and `FromMethod` do not capture ref return metadata.
- `src/Generator/Model/Shared/UnifiedMethodInterceptorModel.cs` -- 67 lines, no ref return fields
- `src/Generator/Model/Shared/UnifiedPropertyInterceptorModel.cs` -- 51 lines, no ref return fields
- `src/Generator/Model/Shared/UnifiedIndexerInterceptorModel.cs` -- 65 lines, no ref return fields
- `src/Generator/Model/Shared/MethodOverloadSignature.cs` -- 41 lines, no ref return fields
- `src/Generator/Model/Flat/FlatMethodModel.cs` -- 45 lines, no ref return fields
- `src/Generator/Model/Flat/FlatPropertyModel.cs` -- 31 lines, no ref return fields
- `src/Generator/Model/Flat/FlatIndexerModel.cs` -- 26 lines, no ref return fields
- `src/Generator/Model/Inline/InlineInterfaceImplementation.cs` -- 92 lines, no ref return fields
- `src/Generator/Model/Inline/InlineMethodModel.cs` -- 34 lines, no ref return fields
- `src/Generator/Model/Inline/InlinePropertyModel.cs` -- 32 lines, no ref return fields
- `src/Generator/Model/Inline/InlineIndexerModel.cs` -- 42 lines, no ref return fields
- `src/Generator/Model/Inline/InlineClassStubModel.cs` -- Full file: `InlineClassPropertyModel`, `InlineClassIndexerModel`, `InlineClassImplMethodModel`, `InlineClassImplPropertyModel`, `InlineClassImplIndexerModel` -- none have ref return fields
- `src/Generator/Model/StandaloneClass/BaseClassMethodModel.cs` -- 23 lines, no ref return fields
- `src/Generator/Model/StandaloneClass/BaseClassPropertyModel.cs` -- 20 lines, no ref return fields
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` (line 640+) -- Confirmed the `Invoke()` method structure with 13 steps
- `src/Generator/Renderer/ClassRenderer.cs` (lines 690-894) -- Confirmed virtual property uses IsConfigured pattern, virtual method uses UnconfiguredCallCount pattern
- `src/Generator/Renderer/Shared/ModelAdapters.cs` -- Confirmed adapter methods that must propagate flags

**Design.Stubs Verification:**
- `src/Design/Design.Domain/Services/IRefReturnService.cs` -- Present, has ref return methods, properties, indexer, and mixed normal members
- `src/Design/Design.Domain/Abstractions/RefReturnBase.cs` -- Present, has abstract and virtual ref return members for class stub patterns
- `src/Design/Design.Stubs/Methods/RefReturns.cs` -- Present, declares stubs for Pattern 1 (standalone), Pattern 3 (standalone class), Pattern 5 (inline interface), Pattern 6 (inline class). Uses Return(), Get(), Verify() APIs. These will fail to compile until generator implements ref return support.
- `src/Design/Design.Tests/MethodTests/RefReturnTests.cs` -- Present, 30 tests covering standalone methods/properties/indexers, inline methods, standalone class abstract/virtual, inline class abstract/virtual

**Test Project Verification:**
- `src/Tests/KnockOffTests/TestInterfaces.cs` (lines 529-630) -- 4 ref return interfaces + `RefReturnServiceBase` abstract class + `RefReturnServiceBaseKnockOff` standalone class stub
- `src/Tests/KnockOffTests/RefReturnTests.cs` (636 lines) -- Comprehensive tests: standalone interface (methods, properties, indexers, mixed), inline interface (methods, properties, indexers, mixed), standalone class stub (abstract/virtual methods, properties, indexers, mixed), inline class stub (abstract/virtual methods, properties, indexers, mixed)

**Discrepancies Found:**
- None. The plan accurately reflects the current codebase state.

### Why This Plan Is Approved

1. All three original blocking/moderate concerns are genuinely resolved with concrete code patterns and codebase references
2. The model inventory is complete (21 types verified against actual source files)
3. The InvokeRef step mapping is thorough and correctly identifies which steps are skipped
4. The IsConfigured-first pattern for virtual overrides is consistent with existing property override patterns already in the codebase
5. Design.Stubs and Design.Tests provide compilation acceptance criteria for all applicable patterns
6. KnockOffTests provide comprehensive test coverage for both standalone and inline patterns, including class stubs
7. Edge cases are documented (mixed interfaces, ref+out params, overload groups, Source delegation, _defaultRefBacking naming)
8. Out-of-scope items are clearly identified (user method overrides + ref return, documentation/skill updates)

### Devil's Advocate Analysis

**Edge cases NOT explicitly covered (but acceptable):**
1. Generic ref return method on an open generic interface (e.g., `ref T GetRef<T>()`) -- the plan's approach handles this through existing generic method pipelines; the ref return flag is orthogonal to generics
2. Multiple abstract ref return members with the same type but different names sharing `_defaultRefBacking` -- edge case 8 handles this with `_defaultRefBacking_{MemberName}` naming

**Ways this could break existing functionality:**
1. None identified. All changes are additive (new boolean fields default to false). Existing non-ref members are unaffected.

**Ways users could misunderstand the API:**
1. Users might try `stub.GetValueRef.Return(ref someVar)` expecting the ref to chain through -- but the plan correctly notes this is not possible and not needed (the backing field pattern is transparent)

---

## Implementation Contract

**Created:** 2026-02-08
**Approved by:** knockoff-developer

### Design.Stubs Acceptance Criteria

These are the failing Design.Stubs/Design.Tests files. Implementation is done when they all compile and tests pass.

- [ ] `src/Design/Design.Stubs/Methods/RefReturns.cs` -- Must compile after implementation (currently fails: generated stubs missing ref return signatures)
- [ ] `src/Design/Design.Tests/MethodTests/RefReturnTests.cs` -- Must compile and all 30 tests must pass
- [ ] `src/Tests/KnockOffTests/RefReturnTests.cs` -- Must compile and all 50+ tests must pass
- [ ] `dotnet build src/Design/Design.Stubs` -- Must succeed
- [ ] `dotnet test src/Design/Design.Tests` -- Must pass (all tests including ref return tests)

### In Scope

#### Phase 1: Transform + Model (add flags, wire nothing yet)

- [ ] Add `ReturnsByRef` and `ReturnsByRefReadonly` boolean fields to `InterfaceMemberInfo` record in `src/Generator/Models/InterfaceModels.cs`
- [ ] Update `InterfaceMemberInfo.FromProperty` to capture `property.ReturnsByRef` and `property.ReturnsByRefReadonly`
- [ ] Update `InterfaceMemberInfo.FromMethod` to capture `method.ReturnsByRef` and `method.ReturnsByRefReadonly`
- [ ] Add `ReturnsByRef` and `ReturnsByRefReadonly` boolean fields to `ClassMemberInfo` record in `src/Generator/Models/ClassModels.cs`
- [ ] Update `ClassMemberInfo.FromProperty` to capture ref return flags
- [ ] Update `ClassMemberInfo.FromMethod` to capture ref return flags
- [ ] Add `ReturnsByRef` / `ReturnsByRefReadonly` to `UnifiedMethodInterceptorModel` in `src/Generator/Model/Shared/UnifiedMethodInterceptorModel.cs`
- [ ] Add `ReturnsByRef` / `ReturnsByRefReadonly` to `UnifiedPropertyInterceptorModel` in `src/Generator/Model/Shared/UnifiedPropertyInterceptorModel.cs`
- [ ] Add `ReturnsByRef` / `ReturnsByRefReadonly` to `UnifiedIndexerInterceptorModel` in `src/Generator/Model/Shared/UnifiedIndexerInterceptorModel.cs`
- [ ] Add `ReturnsByRef` / `ReturnsByRefReadonly` to `MethodOverloadSignature` in `src/Generator/Model/Shared/MethodOverloadSignature.cs`
- [ ] Add `ReturnsByRef` / `ReturnsByRefReadonly` to `FlatMethodModel` in `src/Generator/Model/Flat/FlatMethodModel.cs`
- [ ] Add `ReturnsByRef` / `ReturnsByRefReadonly` to `FlatPropertyModel` in `src/Generator/Model/Flat/FlatPropertyModel.cs`
- [ ] Add `ReturnsByRef` / `ReturnsByRefReadonly` to `FlatIndexerModel` in `src/Generator/Model/Flat/FlatIndexerModel.cs`
- [ ] Add `ReturnsByRef` / `ReturnsByRefReadonly` to `InlineInterfaceImplementation` in `src/Generator/Model/Inline/InlineInterfaceImplementation.cs`
- [ ] Add `ReturnsByRef` / `ReturnsByRefReadonly` to `InlineMethodModel` in `src/Generator/Model/Inline/InlineMethodModel.cs`
- [ ] Add `ReturnsByRef` / `ReturnsByRefReadonly` to `InlinePropertyModel` in `src/Generator/Model/Inline/InlinePropertyModel.cs`
- [ ] Add `ReturnsByRef` / `ReturnsByRefReadonly` to `InlineIndexerModel` in `src/Generator/Model/Inline/InlineIndexerModel.cs`
- [ ] Add `ReturnsByRef` / `ReturnsByRefReadonly` to `InlineClassPropertyModel` in `src/Generator/Model/Inline/InlineClassStubModel.cs`
- [ ] Add `ReturnsByRef` / `ReturnsByRefReadonly` to `InlineClassIndexerModel` in `src/Generator/Model/Inline/InlineClassStubModel.cs`
- [ ] Add `ReturnsByRef` / `ReturnsByRefReadonly` to `InlineClassImplMethodModel` in `src/Generator/Model/Inline/InlineClassStubModel.cs`
- [ ] Add `ReturnsByRef` / `ReturnsByRefReadonly` to `InlineClassImplPropertyModel` in `src/Generator/Model/Inline/InlineClassStubModel.cs`
- [ ] Add `ReturnsByRef` / `ReturnsByRefReadonly` to `InlineClassImplIndexerModel` in `src/Generator/Model/Inline/InlineClassStubModel.cs`
- [ ] Add `ReturnsByRef` / `ReturnsByRefReadonly` to `BaseClassMethodModel` in `src/Generator/Model/StandaloneClass/BaseClassMethodModel.cs`
- [ ] Add `ReturnsByRef` / `ReturnsByRefReadonly` to `BaseClassPropertyModel` in `src/Generator/Model/StandaloneClass/BaseClassPropertyModel.cs`
- [ ] **Checkpoint: `dotnet build src/KnockOff.sln` succeeds. All existing tests pass.**

#### Phase 2: Builders + Adapters (wire the flags through pipelines)

- [ ] Update `FlatModelBuilder` to propagate ref return flags from `InterfaceMemberInfo` to `FlatMethodModel`, `FlatPropertyModel`, `FlatIndexerModel`
- [ ] Update `InlineModelBuilder` to propagate ref return flags to `InlineInterfaceImplementation`, `InlineMethodModel`, `InlinePropertyModel`, `InlineIndexerModel`, and the class stub models (`InlineClassPropertyModel`, `InlineClassIndexerModel`, `InlineClassImplMethodModel`, `InlineClassImplPropertyModel`, `InlineClassImplIndexerModel`)
- [ ] Update `ClassModelBuilder` to propagate from `ClassMemberInfo` to class stub models
- [ ] Update `StandaloneClassModelBuilder` to propagate from `ClassMemberInfo` to `BaseClassMethodModel`, `BaseClassPropertyModel`, and class stub models
- [ ] Update `UnifiedInterceptorBuilder` to propagate to `UnifiedMethodInterceptorModel`, `UnifiedPropertyInterceptorModel`, `UnifiedIndexerInterceptorModel`
- [ ] Update `ModelAdapters.ToUnifiedModel` to propagate flags from `FlatMethodGroup` to `UnifiedMethodInterceptorModel`
- [ ] Update `ModelAdapters.ToUnifiedPropertyModel` to propagate flags
- [ ] Update `ModelAdapters.ToUnifiedIndexerModel` to propagate flags
- [ ] Update `ModelAdapters.BuildMultiOverloadModel` to propagate per-signature flags to `MethodOverloadSignature`
- [ ] **Checkpoint: `dotnet build src/KnockOff.sln` succeeds. All existing tests pass.**

#### Phase 3: Interceptor Renderers (generate InvokeRef/InvokeRefGet + backing field)

- [x] Update `MethodInterceptorRenderer` (`src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs`):
  - Generate `internal {type} _refReturnBacking;` field when `IsRefReturn` (with CS8618 pragma)
  - Generate `InvokeRef(...)` method: simplified Invoke that skips async branches (steps 7-8), writes to `_refReturnBacking` instead of returning
  - Generate per-overload `_refReturnBacking_{suffix}` fields and `InvokeRef_{suffix}` methods
- [x] Update `PropertyInterceptorRenderer` (`src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs`):
  - Generate `internal {type} _refReturnBacking;` field when `IsRefReturn` (with CS8618 pragma)
  - Generate `InvokeRefGet(...)` method: writes to `_refReturnBacking` instead of returning
- [x] Update `IndexerInterceptorRenderer` (`src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs`):
  - Generate `internal {type} _refReturnBacking;` field when `IsRefReturn` (with CS8618 pragma)
  - Generate `InvokeRefGet(...)` method with key parameters
- [x] **Checkpoint: `dotnet build src/KnockOff.sln` succeeds. All existing tests pass.**

#### Phase 4: Implementation Renderers (explicit interface implementations + class overrides)

- [x] Update `FlatRenderer` (`src/Generator/Renderer/FlatRenderer.cs`):
  - Method implementation: prefix return type with `ref`/`ref readonly`, use `InvokeRef` + `return ref _refReturnBacking`
  - Property implementation: prefix return type, use `InvokeRefGet` + `return ref _refReturnBacking`
  - Indexer implementation: prefix return type, use `InvokeRefGet` + `return ref _refReturnBacking`
- [x] Update `InlineRenderer` (`src/Generator/Renderer/InlineRenderer.cs`):
  - Same changes as FlatRenderer for explicit interface implementations
- [x] Update `ClassRenderer` (`src/Generator/Renderer/ClassRenderer.cs`):
  - Abstract ref return overrides: null check with `_defaultRefBacking`, then `InvokeRef` + `return ref _refReturnBacking`
  - Virtual ref return overrides: IsConfigured-first pattern (null check -> base; IsConfigured -> InvokeRef + backing; else -> InvokeRef tracking + base)
  - Add `_defaultRefBacking_{MemberName}` field generation for abstract ref return members
  - Fix `ToUnifiedPropertyModel` and `ToUnifiedIndexerModel` to pass `ReturnsByRef`/`ReturnsByRefReadonly` flags
- [x] Update `StandaloneClassRenderer` (`src/Generator/Renderer/StandaloneClassRenderer.cs`):
  - Same patterns as ClassRenderer for class stub overrides
  - Fix `ToUnifiedPropertyModel` and `ToUnifiedIndexerModel` to pass `ReturnsByRef`/`ReturnsByRefReadonly` flags
- [x] **Checkpoint: Ref return tests in KnockOffTests compile and pass. Design.Stubs compile. Design.Tests pass.**

#### Phase 5: Final verification

- [x] Run full test suite: `dotnet test src/Tests/KnockOffTests/`
- [x] Run Design tests: `dotnet test src/Design/Design.Tests/`
- [x] Build Design.Stubs: `dotnet build src/Design/Design.Stubs/`
- [x] Verify all compilation errors from exploratory tests are resolved
- [x] Verify no regressions in existing tests

### Explicitly Out of Scope

- User method overrides + ref return (standalone stubs with `protected override ref int Method_()`) -- deferred to follow-up per plan's Open Questions
- Documentation updates (skill files, guides) -- user confirmed this is a minor edge case, no doc updates needed
- Generic standalone patterns (2, 4) and open generic patterns (7, 8, 9) do not need separate test code because they share pipelines with patterns 1, 3, 5, 6 respectively. The flag propagation is uniform.

### Verification Gates

1. **After Phase 1:** `dotnet build src/KnockOff.sln` succeeds. All existing tests pass. New boolean fields default to `false` -- zero behavioral change.
2. **After Phase 2:** `dotnet build src/KnockOff.sln` succeeds. All existing tests pass. Flags are now populated in models but not yet consumed by renderers.
3. **After Phase 3:** `dotnet build src/KnockOff.sln` succeeds. All existing tests pass. Interceptor classes for ref return members now have `_refReturnBacking` and `InvokeRef`/`InvokeRefGet` methods, but implementation renderers still emit wrong signatures.
4. **After Phase 4:** This is the big phase. Ref return tests compile and pass. Design.Stubs compile. Design.Tests pass. All existing tests continue to pass.
5. **Final (Phase 5):** Full test suite green across all TFMs. Design projects build and test cleanly. All Design.Stubs acceptance criteria compile.

### Stop Conditions

If any of these occur, STOP and report:
- An out-of-scope test that was passing before starts failing
- Architectural contradiction discovered (e.g., a renderer path that cannot be extended for ref returns without breaking non-ref members)
- Generated code does not compile for a pattern that the plan says should work
- The `InvokeRef` method needs to handle a step that was marked as SKIP in the plan's mapping table
- The `_defaultRefBacking` pattern causes issues with generic type parameters or nullable reference types

---

## Implementation Progress

### Phase 1: Model Flags (Complete)
Added `ReturnsByRef` and `ReturnsByRefReadonly` boolean flags to all 21 model types. Added `IsRefReturn` and `RefReturnPrefix` convenience properties.

### Phase 2: Builders and Adapters (Complete)
All builders and adapters propagate ref return flags from Roslyn symbols through the model pipeline.

### Phase 3: Interceptor Renderers (Complete)
- `MethodInterceptorRenderer`: Added `_refReturnBacking` field, `InvokeRef()` method, per-overload `_refReturnBacking_{suffix}` fields and `InvokeRef_{suffix}()` methods, and `RenderWhenChainInvokeRefCheck` helper.
- `PropertyInterceptorRenderer`: Added `_refReturnBacking` field and `InvokeRefGet()` method.
- `IndexerInterceptorRenderer`: Added `_refReturnBacking` field and `InvokeRefGet()` method with key parameters.
- All `_refReturnBacking` fields include `#pragma warning disable CS8618` for non-nullable reference types.

### Phase 4: Implementation Renderers (Complete)
- `FlatRenderer`: Added `RefReturnPrefix` to method/property/indexer signatures, added ref return branches using `InvokeRef`/`InvokeRefGet` + `return ref _refReturnBacking`.
- `InlineRenderer`: Same pattern as FlatRenderer.
- `ClassRenderer`: Added `_defaultRefBacking_{MemberName}` fields for abstract ref return members. Added ref return branches for abstract (direct InvokeRef) and virtual (IsConfigured-first pattern) overrides. Fixed `ToUnifiedPropertyModel` and `ToUnifiedIndexerModel` to pass `ReturnsByRef`/`ReturnsByRefReadonly`.
- `StandaloneClassRenderer`: Same patterns as ClassRenderer.

### Additional Fixes
- Fixed 4 pre-existing test lines in `RefReturnTests.cs` (lines 147, 157, 323, 333) that used incorrect multi-indexer API (`Indexer.VerifyGet` -> `Indexer.OfInt32.VerifyGet`, `IndexerString` -> `Indexer.OfString`).
- Added `#pragma warning disable CA1859` in `Design.Tests/MethodTests/RefReturnTests.cs` for intentional interface-typed variables.

### Status: Awaiting Verification

---

## Completion Evidence

### Build Results
- `dotnet build src/KnockOff.sln`: **0 warnings, 0 errors** (all TFMs: net8.0, net9.0, net10.0)
- `dotnet build src/Design/Design.Stubs/Design.Stubs.csproj`: **0 warnings, 0 errors** (all TFMs)

### Test Results
- **KnockOffTests**: net8.0: 1304 passed / net9.0: 1305 passed / net10.0: 1305 passed. **0 failures.**
- **Design.Tests**: net8.0: 356 passed / net9.0: 356 passed / net10.0: 356 passed. **0 failures.**

### Files Modified
**Renderers (Phase 3 - Interceptors):**
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` - InvokeRef, _refReturnBacking
- `src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs` - InvokeRefGet, _refReturnBacking
- `src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs` - InvokeRefGet, _refReturnBacking

**Renderers (Phase 4 - Implementations):**
- `src/Generator/Renderer/FlatRenderer.cs` - ref return method/property/indexer implementations
- `src/Generator/Renderer/InlineRenderer.cs` - ref return method/property/indexer implementations
- `src/Generator/Renderer/ClassRenderer.cs` - ref return overrides + _defaultRefBacking + adapter fixes
- `src/Generator/Renderer/StandaloneClassRenderer.cs` - ref return overrides + _defaultRefBacking + adapter fixes

**Tests (bug fixes):**
- `src/Tests/KnockOffTests/RefReturnTests.cs` - Fixed 4 lines with incorrect multi-indexer API
- `src/Design/Design.Tests/MethodTests/RefReturnTests.cs` - CA1859 suppression

---

## Architect Verification

**Verified:** 2026-02-08
**Verdict:** VERIFIED

### Independent Build Results
- `dotnet build src/KnockOff.sln`: 0 warnings, 0 errors (all TFMs: net8.0, net9.0, net10.0)
- `dotnet build src/Design/Design.Stubs/`: 0 warnings, 0 errors (all TFMs)

### Independent Test Results
- **KnockOffTests**: net8.0: 1304 passed / net9.0: 1305 passed / net10.0: 1305 passed. **0 failures.**
- **Design.Tests**: net8.0: 356 passed / net9.0: 356 passed / net10.0: 356 passed. **0 failures.**

### 1304 vs 1305 Discrepancy (net8.0 vs net9.0+)
The difference is `KnockOff.Tests.InlineStubTests.PartialProperty_AutoInstantiated` which only exists on net9.0+ because partial properties are a C# 13 / .NET 9 feature. This is a pre-existing framework-level difference, not related to the ref return implementation.

### Design Match Verification

**Interceptor backing fields:** Confirmed. `_refReturnBacking` fields are generated in `MethodInterceptorRenderer`, `PropertyInterceptorRenderer`, and `IndexerInterceptorRenderer` with CS8618 pragma suppression.

**InvokeRef / InvokeRefGet methods:** Confirmed. All three interceptor renderers generate the ref-specific invoke methods. The `InvokeRef` method in `MethodInterceptorRenderer` correctly skips async branches (steps 7-8) with explicit comment, and all `return value;` statements become `_refReturnBacking = value; return;`.

**Explicit interface implementations:** Confirmed. `FlatRenderer`, `InlineRenderer` both use `RefReturnPrefix` on return type signatures and emit `InvokeRef`/`InvokeRefGet` + `return ref _refReturnBacking` for ref return members.

**Class stub overrides (IsConfigured-first pattern for virtual):** Confirmed in `ClassRenderer` and `StandaloneClassRenderer`:
- Abstract: `_stub == null` -> `_defaultRefBacking = default!; return ref _defaultRefBacking;` then `InvokeRef` + `return ref _refReturnBacking`
- Virtual: `_stub == null` -> `return ref base.Member()` then `IsConfigured` check -> `InvokeRef` + `return ref _refReturnBacking` else `InvokeRef` (tracking) + `return ref base.Member()`

**`_defaultRefBacking_{MemberName}` fields:** Confirmed. Generated per abstract ref return member (methods, properties, indexers) in both `ClassRenderer` (line 608-629) and `StandaloneClassRenderer` (line 608-626).

**Model flags:** Confirmed across all 14 model files in `src/Generator/Model/`, both transform models (`InterfaceModels.cs`, `ClassModels.cs`, `MethodModels.cs`), all 5 builders, and all adapter methods in `ModelAdapters.cs`.

**Per-overload ref return support:** Confirmed. `MethodOverloadSignature` carries `ReturnsByRef`/`ReturnsByRefReadonly` flags with `IsRefReturn` and `RefReturnPrefix` computed properties.

### Generated Code Spot-Check
- `ClassRenderer.cs` lines 870-910: Virtual method ref return override matches plan's IsConfigured-first pattern exactly
- `ClassRenderer.cs` lines 700-732: Property ref return override matches plan (abstract and virtual paths)
- `ClassRenderer.cs` lines 608-629: `_defaultRefBacking_{MemberName}` field generation for abstract members
- `FlatRenderer.cs` lines 2190-2195: Method implementation uses `InvokeRef` + `return ref _refReturnBacking`
- `InlineRenderer.cs` lines 1243-1248: Same pattern as FlatRenderer
- `MethodInterceptorRenderer.cs` lines 1141-1284: Full `InvokeRef` method with all priority chain steps, async branches correctly skipped

### Test Coverage Assessment
- **KnockOffTests/RefReturnTests.cs**: 52 tests covering standalone interface (methods, properties, indexers, mixed, sequences, verification), inline interface (same), standalone class (abstract/virtual methods, properties, indexers, mixed), inline class (abstract/virtual methods, properties, indexers)
- **Design.Tests/MethodTests/RefReturnTests.cs**: 24 tests covering standalone interface, inline interface, standalone class (abstract/virtual), and inline class (abstract/virtual) patterns for methods, properties, and indexers
