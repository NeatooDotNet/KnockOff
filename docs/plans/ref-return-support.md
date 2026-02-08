# Ref Return Support Design

**Date:** 2026-02-08
**Related Todo:** [Add Ref Return Support to Generator](../todos/ref-return-support.md)
**Status:** Draft (Architect)
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

**Unified models** (shared by all renderers):

`UnifiedMethodInterceptorModel` -- add:
```csharp
bool ReturnsByRef = false,
bool ReturnsByRefReadonly = false
```

`UnifiedPropertyInterceptorModel` -- add:
```csharp
bool ReturnsByRef = false,
bool ReturnsByRefReadonly = false
```

`UnifiedIndexerInterceptorModel` -- add:
```csharp
bool ReturnsByRef = false,
bool ReturnsByRefReadonly = false
```

**Flat models:**

`FlatPropertyModel` -- add:
```csharp
bool ReturnsByRef = false,
bool ReturnsByRefReadonly = false
```

`FlatMethodModel` -- add:
```csharp
bool ReturnsByRef = false,
bool ReturnsByRefReadonly = false
```

`FlatIndexerModel` -- add:
```csharp
bool ReturnsByRef = false,
bool ReturnsByRefReadonly = false
```

**Inline models:**

`InlineInterfaceImplementation` -- add:
```csharp
bool ReturnsByRef = false,
bool ReturnsByRefReadonly = false
```

`InlinePropertyModel`, `InlineMethodModel`, `InlineIndexerModel` -- add:
```csharp
bool ReturnsByRef = false,
bool ReturnsByRefReadonly = false
```

**Standalone class models:**

`BaseClassMethodModel`, `BaseClassPropertyModel` -- add same fields if they exist (need to verify).

**Convenience property:** Add a computed property on each model:
```csharp
bool IsRefReturn => ReturnsByRef || ReturnsByRefReadonly;
string RefReturnPrefix => ReturnsByRef ? "ref " : ReturnsByRefReadonly ? "ref readonly " : "";
```

### Layer 3: Builders (Set the Flags)

Each builder reads the flags from the transform model and sets them on the generation model.

**`FlatModelBuilder`**: When building `FlatMethodModel`, `FlatPropertyModel`, `FlatIndexerModel` from `InterfaceMemberInfo`, propagate `ReturnsByRef` / `ReturnsByRefReadonly`.

**`InlineModelBuilder`**: When building `InlineInterfaceImplementation`, `InlineMethodModel`, `InlinePropertyModel`, `InlineIndexerModel`, propagate the flags.

**`ClassModelBuilder`** and **`StandaloneClassModelBuilder`**: When building from `ClassMemberInfo`, propagate the flags.

**`UnifiedInterceptorBuilder`**: When building `UnifiedMethodInterceptorModel`, `UnifiedPropertyInterceptorModel`, `UnifiedIndexerInterceptorModel`, propagate the flags.

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
2. Generate `InvokeRef(...)` instead of `Invoke(...)` -- same priority chain logic, but writes to `_refReturnBacking` instead of returning
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

For class stubs (patterns 3, 4, 6, 9), the Impl class overrides virtual/abstract members:

```csharp
// In Impl nested class:
public override ref int GetValueRef()
{
    _stub.GetValueRef.InvokeRef(_stub.Strict);
    return ref _stub.GetValueRef._refReturnBacking;
}
```

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
            // Same priority chain as Invoke():
            // sequence > return value > callback > source > strict > default
            // But writes to _refReturnBacking instead of returning
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
| 3. Standalone Class | Yes | Yes | Yes | Override in Impl class |
| 4. Generic Standalone Class | Yes | Yes | Yes | Same pipeline as Standalone Class |
| 5. Inline Interface | Yes | Yes | Yes | Primary focus |
| 6. Inline Class | Yes | Yes | Yes | Override in Impl class |
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
- `src/Generator/Model/Flat/FlatMethodModel.cs` -- Add flags
- `src/Generator/Model/Flat/FlatPropertyModel.cs` -- Add flags
- `src/Generator/Model/Flat/FlatIndexerModel.cs` -- Add flags
- `src/Generator/Model/Inline/InlineInterfaceImplementation.cs` -- Add flags
- `src/Generator/Model/Inline/InlineMethodModel.cs` -- Add flags
- `src/Generator/Model/Inline/InlinePropertyModel.cs` -- Add flags
- `src/Generator/Model/Inline/InlineIndexerModel.cs` -- Add flags
- `src/Generator/Model/StandaloneClass/BaseClassMethodModel.cs` -- Add flags (if applicable)
- `src/Generator/Model/StandaloneClass/BaseClassPropertyModel.cs` -- Add flags (if applicable)

**Verification gate:** All existing tests still pass. Flags default to false.

### Phase 2: Builders

**Goal:** Wire the flags from transform models to generation models in all builders.

Files to modify:
- `src/Generator/Builder/FlatModelBuilder.cs`
- `src/Generator/Builder/InlineModelBuilder.cs`
- `src/Generator/Builder/ClassModelBuilder.cs`
- `src/Generator/Builder/StandaloneClassModelBuilder.cs`
- `src/Generator/Builder/UnifiedInterceptorBuilder.cs`

**Verification gate:** All existing tests still pass. Ref return flags are now populated (can verify with debugger or diagnostic).

### Phase 3: Interceptor Renderers

**Goal:** Generate `_refReturnBacking` field and `InvokeRef` / `InvokeRefGet` methods.

Files to modify:
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs`
- `src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs`
- `src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs`

**Approach:** When the model has `IsRefReturn`:
1. Emit `internal {type} _refReturnBacking;` field
2. Emit `InvokeRef()` (methods) or `InvokeRefGet()` (properties/indexers) that mirrors the existing Invoke/InvokeGet logic but writes to `_refReturnBacking` instead of returning

**Verification gate:** All existing tests still pass (non-ref members unaffected). Ref return interceptors now generate correct code.

### Phase 4: Implementation Renderers

**Goal:** Generate correct explicit interface implementations and class overrides for ref return members.

Files to modify:
- `src/Generator/Renderer/FlatRenderer.cs` -- Method, property, indexer implementations
- `src/Generator/Renderer/InlineRenderer.cs` -- Method, property, indexer implementations
- `src/Generator/Renderer/ClassRenderer.cs` -- Class stub overrides
- `src/Generator/Renderer/StandaloneClassRenderer.cs` -- Standalone class stub overrides

**Approach:**
1. Prefix return type with `ref ` or `ref readonly ` in the member signature
2. For methods: call `InvokeRef()`, then `return ref interceptor._refReturnBacking`
3. For properties/indexers: call `InvokeRefGet()`, then `return ref interceptor._refReturnBacking`

**Verification gate:** Exploratory tests compile. 120 compilation errors resolved.

### Phase 5: Tests and Design Projects

**Goal:** Get all exploratory tests passing. Add Design.Stubs examples.

Files to modify:
- `src/Tests/KnockOffTests/RefReturnTests.cs` -- Verify all tests pass
- `src/Design/Design.Stubs/Methods/` -- Add ref return method examples
- `src/Design/Design.Stubs/Properties/` -- Add ref return property examples
- `src/Design/Design.Stubs/Indexers/` -- Add ref return indexer examples

---

## Acceptance Criteria

1. All 120 compilation errors from exploratory tests are resolved
2. All existing tests continue to pass
3. Ref return methods support: Return(value), Return(callback), Call(callback), sequences (ThenReturn/ThenCall), When chains, verification (Verify/Called)
4. Ref return properties support: Get(callback), Get(value), sequences (ThenGet), VerifyGet
5. Ref return indexers support: Get(callback), Get(value), sequences (ThenGet), VerifyGet
6. Mixed interfaces (normal + ref return members) compile and work correctly
7. Ref readonly returns emit `ref readonly` in the member signature

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Backing field overwritten between calls | Medium | Low | Acceptable for stubs -- mirrors real-world behavior. Document in comments. |
| `ref readonly` returning ref to mutable field | Low | Low | Valid C# -- the readonly constraint is on the caller, not the implementation. |
| Async methods with ref return | None | None | C# compiler prevents this -- no action needed. |
| Generic methods with ref return | Low | Medium | InvokeRef pattern works the same for generic handlers. Verify with test. |
| User method override + ref return (standalone stubs) | Medium | Medium | User override methods would need to return ref. May need special handling or deferral. |
| Overload groups with mixed ref/non-ref returns | Low | Low | Each overload gets its own InvokeRef/Invoke suffix. |

---

## Edge Cases

1. **Mixed interfaces:** An interface with both `int GetValue()` and `ref int GetValueRef()` -- both members generate correctly, each with its own interceptor and invocation pattern.

2. **ref return + ref/out parameters:** `ref int Process(ref int a, out string b)` -- the ref return uses `InvokeRef` with backing field; the ref/out parameters use the existing custom delegate pattern. These are orthogonal.

3. **Overload groups with ref return:** If a method name has overloads where some return by ref and some don't, each overload gets its own `Invoke` or `InvokeRef` suffix. The backing field is shared per interceptor.

4. **Source(T) delegation with ref return:** When `_source` is set and the source method returns by ref, we cannot chain the ref through (the source's ref points to its own storage). The interceptor must call the source, copy the value to `_refReturnBacking`, and return ref to `_refReturnBacking`. This is slightly lossy (the ref no longer points to the original storage) but acceptable for stubs.

5. **Default values for ref return:** When no callback is configured, `_refReturnBacking = default` is correct for value types. For reference types, `_refReturnBacking = default!` works.

6. **User method override + ref return:** For standalone stubs with user method overrides (e.g., `protected override ref int GetValueRef_()`), the user method would need to return by ref. This may not be practical since the override mechanism uses regular return types. **Recommendation: defer user method override support for ref return methods to a follow-up. Document this as a known limitation.**

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
| `src/Generator/Model/Flat/*.cs` | Flat pipeline models | `FlatMethodModel`, `FlatPropertyModel`, `FlatIndexerModel` -- all need flags. |
| `src/Generator/Model/Inline/*.cs` | Inline pipeline models | `InlineInterfaceImplementation`, `InlineMethodModel`, `InlinePropertyModel`, `InlineIndexerModel` -- all need flags. |
| `src/Generator/Renderer/FlatRenderer.cs` | Flat renderer | Lines 1995, 2104, 2159 -- explicit implementations use `ReturnType` directly. Need `RefReturnPrefix`. |
| `src/Generator/Renderer/InlineRenderer.cs` | Inline renderer | Lines 1126, 1168, 1213 -- same pattern, needs `RefReturnPrefix`. |
| `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` | Method interceptor | Line 650: `Invoke()` returns the value. Need alternative `InvokeRef()` that writes to backing field. |
| `src/Generator/Renderer/Shared/PropertyInterceptorRenderer.cs` | Property interceptor | Line 401: `InvokeGet()` returns the value. Need `InvokeRefGet()`. |
| `src/Generator/Renderer/Shared/IndexerInterceptorRenderer.cs` | Indexer interceptor | Line 187: `InvokeGet()` returns the value. Need `InvokeRefGet()`. |
| `src/Tests/KnockOffTests/TestInterfaces.cs` | Test interfaces | Lines 529-592: Four ref return interfaces + four standalone stubs. |
| `src/Tests/KnockOffTests/RefReturnTests.cs` | Exploratory tests | 4 inline stubs + comprehensive test class covering standalone and inline patterns for methods, properties, indexers, and mixed interfaces. |
| `src/Design/Design.Stubs/Methods/RefOutParameters.cs` | Design stubs | Ref/out parameters are supported. No ref returns yet. |

---

## Architectural Verification

### Design Project Verification

No ref return examples exist in `src/Design/Design.Stubs/` currently. Design.Stubs code will be added in Phase 5 as acceptance criteria.

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

1. **Existing exploratory tests** (`RefReturnTests.cs`) cover standalone and inline patterns for methods, properties, indexers, and mixed interfaces
2. **Design.Stubs** examples will be added for all three member types
3. **Existing test suite** must continue to pass (regression check)
4. **Edge case tests** for: sequences on ref return methods, verification on ref return properties, mixed ref/non-ref interfaces

---

## Open Questions

1. **User method overrides + ref return:** Should standalone stubs with user method overrides support ref return methods, or should this be deferred? The user's `protected override` method would need to return `ref T`, which may not compose well with the base class pattern. **Recommendation: defer to follow-up.**

2. **Source(T) delegation:** When delegating to a source object, the source's ref return points to its own storage. Copying the value to the interceptor's backing field means the ref is "redirected." Is this acceptable? **Recommendation: yes, for stubs this is fine.**

---

## Architectural Verification Checklist

- [x] All nine patterns analyzed
- [ ] Design.Stubs compilation verification for every pattern+feature claim (Phase 5)
- [x] Breaking changes assessment completed (None)
- [x] Pattern consistency verified (Same API across all patterns)
- [x] Diagnostic requirements identified (None needed)
- [x] Test strategy defined
- [x] Edge cases documented
- [x] Codebase deep-dive completed

---

## Developer Review

**Status:** Not Started
**Reviewed:** -

**Concerns:** -

---

## Implementation Contract

*To be created after developer review.*

---

## Implementation Progress

*To be filled during implementation.*

---

## Completion Evidence

*To be filled after implementation.*
