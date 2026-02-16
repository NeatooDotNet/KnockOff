# Single-Parameter Interceptor Tier

**Date:** 2026-02-16
**Status:** Awaiting Verification
**Last Updated:** 2026-02-16

---

## Overview

Add a dedicated interceptor tier for single-parameter methods, sitting between the existing zero-param and TTuple (1+-param) tiers. This gives users `LastArg` (singular) instead of `LastArgs` (plural) for single-parameter methods, eliminates the grammatically awkward `LastArgs` for a single value, and enables `where TArgs : struct, ITuple` on the 2+-param tier.

### Current State

| Param Count | Interceptor Family | Tracking Property | TArgs |
|---|---|---|---|
| 0 params | `*Interceptor0` (4 types) | None | None |
| 1+ params | `*Interceptor<TDelegate, TArgs, ...>` (4 types) | `LastArgs` (plural) | Raw type (1 param) or `ValueTuple` (2+ params) |

### Target State

| Param Count | Interceptor Family | Tracking Property | TArgs |
|---|---|---|---|
| 0 params | `*Interceptor0` (4 types) | None | None |
| 1 param | `*Interceptor1<TDelegate, TArg, ...>` (4 NEW types) | `LastArg` (singular) | Raw type |
| 2+ params | `*Interceptor<TDelegate, TArgs, ...>` (4 existing types) | `LastArgs` (plural) | `ValueTuple` (with `ITuple` constraint) |

**Total library types:** 12 (4 families x 3 tiers). Up from 8, far fewer than the 36 from v0.49.0.

---

## Approach

### Guiding Principles

1. **Grammatical correctness at the API surface** -- `LastArg` for one value, `LastArgs` for a tuple.
2. **Type-level enforcement** -- The 1-param tier uses `IMethodTracking<TArg>` (has `LastArg`); the 2+-param tier uses `IMethodTrackingArgs<TArgs>` (has `LastArgs`). No runtime confusion.
3. **Minimal generated code change** -- The generator picks a different interceptor type name; delegate declarations and invoke expressions stay the same.
4. **Backward-compatible internal rename** -- Users interact with interceptor types through `var`. The concrete type change from `MethodInterceptor<D,A,R>` to `MethodInterceptor1<D,A,R>` is invisible unless users spelled out the type name explicitly (extremely unlikely).

---

## Design

### 1. Four New Library Types (Single-Param Tier)

Each mirrors the structure of its TTuple counterpart but with these key differences:

| Difference | TTuple (2+ params) | Single-Param (1 param) |
|---|---|---|
| Class name suffix | None (e.g., `MethodInterceptor`) | `1` (e.g., `MethodInterceptor1`) |
| TArgs type param | `TArgs` | `TArg` |
| Tracking property (interceptor) | `LastArgs` (plural) | `LastArg` (singular) |
| Tracking property (builder) | `LastArgs` (plural) | `LastArg` (singular) |
| Builder interface | `IMethodReturnBuilder<TDelegate, TArg?>` / `IMethodCallBuilder<TDelegate, TArg?>` | Same (already uses `IMethodTracking<TArg>` which defines `LastArg`) |
| When API | `When(TArg value)`, `When(Func<TArg, bool>)` | Same |
| Invoke signature | `Invoke(bool strict, TArg arg)` | Same |

The new types are:

```
MethodInterceptor1<TDelegate, TArg, TReturn>           -- non-void sync, 1 param
VoidMethodInterceptor1<TDelegate, TArg>                  -- void sync, 1 param
AsyncMethodInterceptor1<TDelegate, TSyncDelegate, TArg, TReturn>  -- async non-void, 1 param
AsyncVoidMethodInterceptor1<TDelegate, TSyncDelegate, TArg>       -- async void, 1 param
```

#### TSyncDelegate on Async Single-Param Variants

The async TTuple types have `TSyncDelegate` to provide natural lambda parameters on simplified sync callbacks (e.g., `Return((string name) => name.Length)` instead of `Return((Func<string, int>)(name => name.Length))`). For single-param methods, the natural delegate is already `Func<TArg, TReturn>` which gives the parameter a name from the lambda. However, TSyncDelegate is still needed because:

1. **Consistency** -- The generator already emits a `SyncDelegate` for every async 1+-param method. Removing it for 1-param would add a special case to the generator.
2. **Named parameter** -- `Func<string, int>` gives the parameter name `arg`, whereas the generated `SyncDelegate` gives the original name (e.g., `(string name) => ...`). Better IntelliSense.
3. **Expression tree bridging** -- The `DelegateInvokerFactory` uses `TSyncDelegate` to build invokers. Removing it would require a different invoker path for 1-param.

**Decision:** Keep `TSyncDelegate` on async single-param variants for consistency.

#### Inner Class Naming

The single-param tier's inner classes use the same names as the TTuple tier (without the `0` suffix):

```
MethodCallBuilder       -- (not MethodCallBuilder1; parallel to TTuple's MethodCallBuilder)
MethodSequence          -- sequence chaining
WhenBuilder / WhenChain -- for non-void
VoidWhenBuilder / VoidWhenChain -- for void
```

This means the inner class names do NOT collide with the TTuple tier because they are nested inside differently-named outer classes (`MethodInterceptor1<...>` vs `MethodInterceptor<...>`).

### 2. TTuple Interceptor Changes (2+-Param Tier)

#### Add `where TArgs : struct, ITuple` Constraint

With single-param methods routed to the new tier, `TArgs` on the TTuple interceptors is ALWAYS a `ValueTuple` (2+ params). This enables adding:

```csharp
public sealed class MethodInterceptor<TDelegate, TArgs, TReturn> : IInterceptor
    where TDelegate : Delegate
    where TArgs : struct  // ITuple requires .NET 7+ or System.Runtime.CompilerServices.ITuple
```

**ITuple availability:**
- `System.Runtime.CompilerServices.ITuple` was introduced in .NET Core 2.0 and is available in `netstandard2.1`.
- The KnockOff library targets `net8.0;net9.0;net10.0` (NOT netstandard2.0). The library project CAN use ITuple.
- The source generator targets netstandard2.0 but the GENERATOR does not reference these types -- only the generated code references them. Since generated code runs in the consumer's TFM (net8.0+), ITuple is available.

**However**, adding `where TArgs : ITuple` could be a breaking change if any consumer code explicitly specifies the type parameters. In practice, consumers never spell out `MethodInterceptor<AddDelegate, (int a, int b), int>` -- they interact through `var` and the generated field type.

**Decision:** Add `where TArgs : struct` constraint (ValueTuples are structs). Defer `ITuple` constraint to a future release -- `struct` already prevents accidental misuse and does not require extra using directives in generated code.

#### Rename `LastArgs` to `LastArgs` (No Change)

The TTuple tier keeps `LastArgs` (plural). No changes needed here.

#### Remove `LastArg` Explicit Interface Implementation

The TTuple interceptors currently implement `IMethodTracking<TArgs?>.LastArg` explicitly on their `MethodCallBuilder`:

```csharp
TArgs? IMethodTracking<TArgs?>.LastArg => _lastArgs;
```

With the new tier, 1-param methods use `MethodInterceptor1` (which has a public `LastArg`). The TTuple tier now serves only 2+-param methods where `LastArg` is grammatically wrong. We have two options:

**Option A:** Keep the explicit interface implementation. It's harmless, satisfies the interface, and doesn't pollute the public API.

**Option B:** Change the builder to implement `IMethodReturnBuilderArgs<TDelegate, TArgs?>` (which extends `IMethodTrackingArgs<TArgs?>` with `LastArgs`) instead of `IMethodReturnBuilder<TDelegate, TArgs?>` (which extends `IMethodTracking<TArgs?>` with `LastArg`).

**Decision:** Option B. The generator already distinguishes builder interfaces by param count (see `UnifiedInterceptorBuilder.GetBuilderInterface()`). The TTuple builder should implement the `*Args` interface variants that expose `LastArgs`. This is the cleanest type-level separation. The required changes:

- `MethodInterceptor<...>.MethodCallBuilder` implements `IMethodReturnBuilderArgs<TDelegate, TArgs?>` instead of `IMethodReturnBuilder<TDelegate, TArgs?>`
- `VoidMethodInterceptor<...>.MethodCallBuilder` implements `IMethodCallBuilderArgs<TDelegate, TArgs?>` instead of `IMethodCallBuilder<TDelegate, TArgs?>`
- `AsyncMethodInterceptor<...>.MethodCallBuilder` implements `IMethodReturnBuilderArgs<TDelegate, TArgs?>` instead of `IMethodReturnBuilder<TDelegate, TArgs?>`
- `AsyncVoidMethodInterceptor<...>.MethodCallBuilder` implements `IMethodCallBuilderArgs<TDelegate, TArgs?>` instead of `IMethodCallBuilder<TDelegate, TArgs?>`

This replaces `LastArg` (explicit interface impl) with `LastArgs` (from `IMethodTrackingArgs<TArgs?>`). The public `LastArgs` property already exists, so it satisfies the interface implicitly.

### 3. Generator Changes

#### PreCompiledInterceptorRenderer.GetMethodInterceptorType()

This is the central method that computes the interceptor field type. It currently branches on `paramCount == 0` vs `paramCount > 0`. It needs a three-way branch:

```
paramCount == 0  -> *Interceptor0 types (unchanged)
paramCount == 1  -> *Interceptor1 types (NEW)
paramCount >= 2  -> *Interceptor types (unchanged)
```

**File:** `src/Generator/Renderer/Shared/PreCompiledInterceptorRenderer.cs`

**Method:** `GetMethodInterceptorType(UnifiedMethodInterceptorModel model, string? delegateBaseName = null)`

Current logic (line 284-331):
```csharp
if (paramCount == 0)
    return "...Interceptor0<...>";
// else (1+ params):
return "...Interceptor<TDelegate, TArgs, ...>";
```

New logic:
```csharp
if (paramCount == 0)
    return "...Interceptor0<...>";
if (paramCount == 1)
    return "...Interceptor1<TDelegate, TArg, ...>";
// else (2+ params):
return "...Interceptor<TDelegate, TArgs, ...>";
```

The type parameter names don't matter to the compiler (they're just generic arguments), but the interceptor TYPE NAME changes from `MethodInterceptor` to `MethodInterceptor1`.

#### PreCompiledInterceptorRenderer.GetOverloadInterceptorType()

Same three-way branch needed for overload compositor fields (line 774-813).

#### PreCompiledInterceptorRenderer Slot Interfaces

The slot interfaces (`IVoidOverloadSlot1<TDelegate, TArgs>`, etc.) are parameterized by TArgs. For single-param overloads, TArgs is the raw type. The slot interface binding doesn't change -- only the interceptor type it references changes from `VoidMethodInterceptor<D, TArgs>` to `VoidMethodInterceptor1<D, TArg>`.

Wait -- actually the slot interfaces bind to the INTERCEPTOR TYPE. Let me trace this more carefully.

`BuildSlotInterfaceList()` (line 1028) builds interface names like:
```csharp
$"global::KnockOff.Interceptors.IVoidOverloadSlot{voidSlot}<{overload.DelegateName}, {tArgs}>"
```

And `RenderSlotInterfacePropertyImplementations()` (line 1081) renders:
```csharp
$"{fieldType} {ifaceType}.VoidSlot{voidSlot}Interceptor => _ov{fieldIndex};"
```

Where `fieldType` is the result of `GetOverloadInterceptorType(overload)`. So the slot interfaces themselves don't need to change -- they're parameterized generically. The interceptor TYPE referenced by the field type changes, but the slot interface's generic arguments are the same delegate+TArgs.

**However**, the slot interfaces have property return types that reference the interceptor type. For example:
```csharp
public interface IVoidOverloadSlot1<TDelegate, TArgs> where TDelegate : Delegate
{
    VoidMethodInterceptor<TDelegate, TArgs> VoidSlot1Interceptor { get; }
}
```

If a single-param overload now uses `VoidMethodInterceptor1<TDelegate, TArg>`, the slot interface's property return type doesn't match. We need EITHER:
1. New slot interfaces for the single-param tier
2. A shared base type/interface that both tiers implement
3. Accept that slot interfaces don't apply to single-param overloads (fallback to generated forwarding)

**Analysis:** Slot interfaces are used for overload compositors to enable pre-compiled extension methods. The extension methods (like `Call`, `When`) need the concrete interceptor type. Since `VoidMethodInterceptor1` is a different type from `VoidMethodInterceptor`, we need separate slot interfaces.

**But wait** -- let me check if slot interfaces are actually used or if compositors just use generated forwarding methods.

Looking at the generated compositor code in `RenderOverloadCompositorClass()`, the slot interface implementations are explicit property impls, and the behavioral methods (`Call`, `Return`, `When`, etc.) are all generated forwarding methods. The slot interfaces appear to be infrastructure for potential future extension methods that haven't been implemented yet.

**Decision:** For now, add new slot interfaces for the single-param tier (`IVoidOverloadSlot1_1Param<TDelegate, TArg>`, etc.). This follows the existing pattern. Alternatively, if the extension methods are not yet in use, we can skip slot interfaces for single-param overloads and rely on the generated forwarding methods. The latter is simpler and has no functional impact.

**Revised decision:** Skip new slot interfaces for the single-param tier. The generated forwarding methods handle everything. The generator's `BuildSlotInterfaceList()` and `RenderSlotInterfacePropertyImplementations()` must skip `paramCount <= 1` overloads (not just `== 0`). The existing slot interfaces remain TTuple-only and gain `where TArgs : struct` to match the constraint on the TTuple interceptor types. Add 1-param slot interfaces in a follow-up only if extension method support is needed.

#### UnifiedInterceptorBuilder.GetBuilderInterface()

Already handles the 0/1/2+ split correctly (line 289-314). For 1-param:
```csharp
if (trackableParams.Count == 1)
    return $"global::KnockOff.IMethodReturnBuilder<{delegateType}, {param.Type}>";
```

This returns the correct interface. No change needed.

#### Other Generator Files

**ComputeTArgsType()** -- No change. Still returns raw type for 1 param, tuple for 2+.

**FormatInvokeArgs()** -- No change. Still returns `, paramName` for 1 param, `, (a, b)` for 2+.

**BuildDelegateDeclaration()** -- No change. Delegates are the same regardless of interceptor tier.

**BuildSyncDelegateDeclaration()** -- No change.

**GetMethodSourceFallbackExpression()** -- No change. The source delegation expression is the same regardless of interceptor type.

**GetStubOverrideFallbackExpression()** -- No change.

**GetMethodSmartDefaultFactory()** -- No change.

### 4. When() API

No changes. For single-param methods:
- `When(value)` -- passes raw value, same as today
- `When(predicate)` -- `Func<TArg, bool>`, same as today

For 2+-param methods:
- `When((a, b))` -- tuple literal, same as today
- `When(args => args.a == ...)` -- `Func<TArgs, bool>`, same as today

### 5. Interface Hierarchy Summary

After the change, the complete hierarchy for return builders:

```
0 params:  MethodInterceptor0<TReturn>.MethodCallBuilder0
           implements IMethodReturnBuilder<Func<TReturn>>
           extends    IMethodTracking  (no LastArg/LastArgs)

1 param:   MethodInterceptor1<TDelegate, TArg, TReturn>.MethodCallBuilder
           implements IMethodReturnBuilder<TDelegate, TArg?>
           extends    IMethodTracking<TArg?>  (has LastArg)

2+ params: MethodInterceptor<TDelegate, TArgs, TReturn>.MethodCallBuilder
           implements IMethodReturnBuilderArgs<TDelegate, TArgs?>
           extends    IMethodTrackingArgs<TArgs?>  (has LastArgs)
```

And for call builders (void methods):

```
0 params:  VoidMethodInterceptor0.MethodCallBuilder0
           implements IMethodCallBuilder<Action>
           extends    IMethodTracking  (no LastArg/LastArgs)

1 param:   VoidMethodInterceptor1<TDelegate, TArg>.MethodCallBuilder
           implements IMethodCallBuilder<TDelegate, TArg?>
           extends    IMethodTracking<TArg?>  (has LastArg)

2+ params: VoidMethodInterceptor<TDelegate, TArgs>.MethodCallBuilder
           implements IMethodCallBuilderArgs<TDelegate, TArgs?>
           extends    IMethodTrackingArgs<TArgs?>  (has LastArgs)
```

### 6. Patterns Affected

All nine patterns use pre-compiled interceptors for non-ref/out, non->8-param methods. All need the generator to emit the right type.

| Pipeline | Transform | Builder | Renderer | Change Required |
|---|---|---|---|---|
| Standalone interface (1,2) | `TransformClass` | `FlatModelBuilder` | `FlatRenderer` | `GetMethodInterceptorType()` |
| Standalone class (3,4) | `TransformStandaloneClass` | `StandaloneClassModelBuilder` | `StandaloneClassRenderer` | `GetMethodInterceptorType()` |
| Inline interface/class (5,6) | `TransformInlineStubClass` | `InlineModelBuilder` | `InlineRenderer` | `GetMethodInterceptorType()` |
| Open generic (7,8,9) | Various | Various | `InlineRenderer` | `GetMethodInterceptorType()` |

The change is centralized in `PreCompiledInterceptorRenderer.GetMethodInterceptorType()` and `GetOverloadInterceptorType()`. All renderers call through these methods. No per-renderer changes needed.

---

## Breaking Changes Assessment

### For Users (Test Code)

| What Changes | Breaking? | Risk |
|---|---|---|
| Interceptor concrete type name | No -- users use `var` | Extremely low (only breaks explicit type annotations) |
| `LastArgs` on 1-param interceptors | Yes -- becomes `LastArg` | Moderate (users with single-param `LastArgs` must change) |
| `LastArgs` on 1-param builders | Yes -- becomes `LastArg` | Moderate (same) |
| `When()` API | No -- unchanged | None |
| `Return()`/`Call()` API | No -- unchanged | None |
| Sequence API | No -- unchanged | None |
| Verification API | No -- unchanged | None |

**The `LastArgs` -> `LastArg` rename on single-param methods is the only user-visible breaking change.** This is the explicit goal of the feature -- fixing the grammar. Users updating to this version need to change `stub.Method.LastArgs` to `stub.Method.LastArg` for single-param methods.

**Mitigation:** None. Clean break only. `LastArg` on single-param interceptors, `LastArgs` on 2+-param interceptors. No deprecated `LastArgs` alias on the 1-param tier. If existing code uses `LastArgs` on a single-param method, it will produce a compile error. Users update to `LastArg`.

### For the Library (Internal)

The TTuple interceptors' `MethodCallBuilder` changes from `IMethodReturnBuilder<TDelegate, TArgs?>` to `IMethodReturnBuilderArgs<TDelegate, TArgs?>`. This is internal to the library.

---

## Implementation Steps

### Phase 1: Library Types (src/KnockOff/)

1. **Create 4 new interceptor files** by copying from TTuple counterparts and modifying:
   - `src/KnockOff/Interceptors/MethodInterceptor1.cs`
   - `src/KnockOff/Interceptors/VoidMethodInterceptor1.cs`
   - `src/KnockOff/Interceptors/AsyncMethodInterceptor1.cs`
   - `src/KnockOff/Interceptors/AsyncVoidMethodInterceptor1.cs`

   Changes from TTuple originals:
   - Class name: `*Interceptor1<...>` instead of `*Interceptor<...>`
   - Type parameter: `TArg` instead of `TArgs`
   - Property: `LastArg` (singular) instead of `LastArgs` (plural)
   - Field: `_lastArg` instead of `_lastArgs`, `_unconfiguredLastArg` instead of `_unconfiguredLastArgs`
   - Builder interface: `IMethodReturnBuilder<TDelegate, TArg?>` (same as today -- this interface already uses `LastArg`)
   - Builder: public `LastArg` property (no explicit interface impl needed)

2. **Modify 4 existing TTuple interceptor files** to:
   - Change `MethodCallBuilder` from `IMethodReturnBuilder<TDelegate, TArgs?>` to `IMethodReturnBuilderArgs<TDelegate, TArgs?>`
   - Change `MethodCallBuilder` from `IMethodCallBuilder<TDelegate, TArgs?>` to `IMethodCallBuilderArgs<TDelegate, TArgs?>`
   - Remove `TArgs? IMethodTracking<TArgs?>.LastArg => _lastArgs;` explicit interface impl
   - Add `where TArgs : struct` constraint on the outer class
   - Rename internal members from `_lastArgs` to `_lastArgs` (no rename needed, just the public property stays `LastArgs`)
   - Add explicit `IMethodTrackingArgs<TArgs?>.LastArgs => _lastArgs;` if needed by the interface, OR keep the existing public `LastArgs` property which implicitly satisfies it

3. **Propagate `where TArgs : struct` to slot interfaces and extension methods** (32 interfaces + 128 extension methods):
   - `src/KnockOff/Interceptors/Slots/IVoidOverloadSlots.cs` -- Add `where TArgs : struct` to all 8 interfaces
   - `src/KnockOff/Interceptors/Slots/IMethodOverloadSlots.cs` -- Add `where TArgs : struct` to all 8 interfaces
   - `src/KnockOff/Interceptors/Slots/IAsyncVoidOverloadSlots.cs` -- Add `where TArgs : struct` to all 8 interfaces
   - `src/KnockOff/Interceptors/Slots/IAsyncMethodOverloadSlots.cs` -- Add `where TArgs : struct` to all 8 interfaces
   - `src/KnockOff/Interceptors/Slots/VoidSlotExtensions.cs` -- Add `where TArgs : struct` to all 24 methods
   - `src/KnockOff/Interceptors/Slots/MethodSlotExtensions.cs` -- Add `where TArgs : struct` to all 32 methods
   - `src/KnockOff/Interceptors/Slots/AsyncVoidSlotExtensions.cs` -- Add `where TArgs : struct` to all 32 methods
   - `src/KnockOff/Interceptors/Slots/AsyncMethodSlotExtensions.cs` -- Add `where TArgs : struct` to all 40 methods

   **Note:** No new slot interfaces for the 1-param tier. Slot interfaces remain TTuple-only (2+-param). This is safe because the generator skips slot interface generation for 1-param overloads (see step 7).

### Phase 2: Generator Changes (src/Generator/)

4. **Update `PreCompiledInterceptorRenderer.GetMethodInterceptorType()`** to emit `*Interceptor1` for 1-param methods.

5. **Update `PreCompiledInterceptorRenderer.GetOverloadInterceptorType()`** to emit `*Interceptor1` for 1-param overloads.

6. **Update `PreCompiledInterceptorRenderer.GetCallBuilderType()`** and **`GetWhenBuilderType()`** to handle the 1-param tier's inner class names (they use unsuffixed names like `MethodCallBuilder`, same as TTuple, so this may need no change).

7. **Update slot interface construction** -- `BuildSlotInterfaceList()` (line 1041) and `RenderSlotInterfacePropertyImplementations()` (line 1092) currently skip `paramCount == 0`. Change both to skip `paramCount <= 1`, since 1-param overloads use `*Interceptor1` types which have no corresponding slot interface. The generated forwarding methods on the compositor handle 1-param overloads (same as 0-param today).

### Phase 3: Tests and Design Projects

8. **Update Design.Stubs** to demonstrate `LastArg` on single-param methods.

9. **Update tests** that assert on `LastArgs` for single-param methods -- change to `LastArg`.

10. **Add new tests** verifying the 1-param tier works correctly across all patterns.

### Phase 4: Documentation

11. **Update release notes** documenting the `LastArgs` -> `LastArg` breaking change for single-param methods.

---

## Acceptance Criteria

- All 9 patterns generate `*Interceptor1` types for single-param methods
- `stub.Method.LastArg` works for single-param methods (singular)
- `stub.Method.LastArgs` works for 2+-param methods (plural)
- Zero-param methods unchanged (no `LastArg`/`LastArgs`)
- All existing tests pass (with `LastArgs` -> `LastArg` updates for single-param)
- Design projects compile and demonstrate the new API
- `when TArgs : struct` constraint on TTuple interceptors
- TTuple builders implement `*Args` interface variants

---

## Files to Create

| File | Description |
|---|---|
| `src/KnockOff/Interceptors/MethodInterceptor1.cs` | Non-void sync, 1-param interceptor |
| `src/KnockOff/Interceptors/VoidMethodInterceptor1.cs` | Void sync, 1-param interceptor |
| `src/KnockOff/Interceptors/AsyncMethodInterceptor1.cs` | Async non-void, 1-param interceptor |
| `src/KnockOff/Interceptors/AsyncVoidMethodInterceptor1.cs` | Async void, 1-param interceptor |

## Files to Modify

| File | Change |
|---|---|
| `src/KnockOff/Interceptors/MethodInterceptor.cs` | `where TArgs : struct`, builder -> `IMethodReturnBuilderArgs`, remove `LastArg` explicit impl |
| `src/KnockOff/Interceptors/VoidMethodInterceptor.cs` | `where TArgs : struct`, builder -> `IMethodCallBuilderArgs`, remove `LastArg` explicit impl |
| `src/KnockOff/Interceptors/AsyncMethodInterceptor.cs` | `where TArgs : struct`, builder -> `IMethodReturnBuilderArgs`, remove `LastArg` explicit impl |
| `src/KnockOff/Interceptors/AsyncVoidMethodInterceptor.cs` | `where TArgs : struct`, builder -> `IMethodCallBuilderArgs`, remove `LastArg` explicit impl |
| `src/KnockOff/Interceptors/Slots/IVoidOverloadSlots.cs` | Add `where TArgs : struct` to all 8 interfaces |
| `src/KnockOff/Interceptors/Slots/IMethodOverloadSlots.cs` | Add `where TArgs : struct` to all 8 interfaces |
| `src/KnockOff/Interceptors/Slots/IAsyncVoidOverloadSlots.cs` | Add `where TArgs : struct` to all 8 interfaces |
| `src/KnockOff/Interceptors/Slots/IAsyncMethodOverloadSlots.cs` | Add `where TArgs : struct` to all 8 interfaces |
| `src/KnockOff/Interceptors/Slots/VoidSlotExtensions.cs` | Add `where TArgs : struct` to all 24 extension methods |
| `src/KnockOff/Interceptors/Slots/MethodSlotExtensions.cs` | Add `where TArgs : struct` to all 32 extension methods |
| `src/KnockOff/Interceptors/Slots/AsyncVoidSlotExtensions.cs` | Add `where TArgs : struct` to all 32 extension methods |
| `src/KnockOff/Interceptors/Slots/AsyncMethodSlotExtensions.cs` | Add `where TArgs : struct` to all 40 extension methods |
| `src/Generator/Renderer/Shared/PreCompiledInterceptorRenderer.cs` | `GetMethodInterceptorType()`, `GetOverloadInterceptorType()` -- add 1-param branch; `BuildSlotInterfaceList()` and `RenderSlotInterfacePropertyImplementations()` -- skip `paramCount <= 1` |

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| `LastArgs` -> `LastArg` breaks user tests | High | Low | Clean break -- compile errors guide users to `LastArg`; release notes document the change |
| `where TArgs : struct` breaks consumer code | Very Low | Low | Consumers never spell out generic args |
| Expression tree invokers need changes for `*Interceptor1` | Very Low | Low | Same `DelegateInvokerFactory` -- TArg is just a type parameter name |
| Overload compositors with mix of 1-param and 2+-param overloads | Low | Medium | Test thoroughly; both interceptor types used in same compositor |
| 4 new files increase maintenance burden | Certain | Low | Mechanical copies with search-replace; structure identical to TTuple types |

---

## Open Questions

1. ~~**Deprecated `LastArgs` alias duration**~~ **Resolved.** No deprecated alias. Clean break only.
2. **Slot interfaces for 1-param overloads** -- Defer or implement now? Current analysis suggests deferring has no functional impact. See Concern 2 and 3 resolutions below.
3. **Should the TTuple tier also add a public `LastArg` (returning tuple) for cross-tier consistency?** The analysis in `last-arg-vs-last-args.md` recommended this (Option C), but the new tier approach makes it unnecessary. The TTuple tier should only have `LastArgs`.

---

## Codebase Investigation Summary

### Files Examined

| File | What Was Learned |
|---|---|
| `src/KnockOff/Interceptors/MethodInterceptor0.cs` | Zero-param tier: no LastArg/LastArgs, `MethodCallBuilder0 : IMethodReturnBuilder<Func<TReturn>>`. Template for understanding tier structure. |
| `src/KnockOff/Interceptors/MethodInterceptor.cs` | TTuple tier: `LastArgs` property on interceptor and builder. Builder implements `IMethodReturnBuilder<TDelegate, TArgs?>` with explicit `IMethodTracking<TArgs?>.LastArg`. This will be the template for `MethodInterceptor1`. |
| `src/KnockOff/Interceptors/VoidMethodInterceptor.cs` | Void TTuple: same pattern. Builder implements `IMethodCallBuilder<TDelegate, TArgs?>`. |
| `src/KnockOff/Interceptors/VoidMethodInterceptor0.cs` | Void zero-param: no arg tracking. |
| `src/KnockOff/Interceptors/AsyncMethodInterceptor.cs` | Async TTuple: has `TSyncDelegate`. Builder implements `IMethodReturnBuilder<TDelegate, TArgs?>`. |
| `src/KnockOff/Interceptors/AsyncMethodInterceptor0.cs` | Async zero-param: no arg tracking, no TSyncDelegate. |
| `src/KnockOff/Interceptors/AsyncVoidMethodInterceptor.cs` | Async void TTuple: has `TSyncDelegate`. Builder implements `IMethodCallBuilder<TDelegate, TArgs?>`. |
| `src/KnockOff/Interceptors/AsyncVoidMethodInterceptor0.cs` | Async void zero-param: no arg tracking, no TSyncDelegate. |
| `src/KnockOff/Interceptors/DelegateInvokerFactory.cs` | Expression tree bridging. `BuildArgExpressions()` handles 1-param (raw pass-through) vs 2+ params (ValueTuple field access). No changes needed -- works with any interceptor type. |
| `src/KnockOff/IMethodTracking.cs` | `IMethodTracking<TArg>` has `LastArg` (singular). `IMethodTrackingArgs<TArgs>` has `LastArgs` (plural). Already designed for the split. |
| `src/KnockOff/IMethodReturnBuilder.cs` | Three variants: 0-arg, 1-arg (`TArg`), multi-arg (`TArgs`). The 1-arg variant extends `IMethodTracking<TArg>`. The multi-arg variant extends `IMethodTrackingArgs<TArgs>`. |
| `src/KnockOff/IMethodCallBuilder.cs` | Mirrors ReturnBuilder: three variants with same inheritance. |
| `src/Generator/Builder/UnifiedInterceptorBuilder.cs` | `GetBuilderInterface()` already distinguishes 0, 1, and 2+ trackable params. `GetLastArgType()` returns the type for 1-param. `GetLastArgsType()` returns tuple for 2+. No changes needed. |
| `src/Generator/Renderer/Shared/PreCompiledInterceptorRenderer.cs` | `GetMethodInterceptorType()` and `GetOverloadInterceptorType()` are the central decision points. Currently binary (0 vs 1+). Need to become ternary (0 vs 1 vs 2+). |
| `src/Generator/Model/Shared/UnifiedMethodInterceptorModel.cs` | Model already has `LastArgType` (1-param) and `LastArgsType` (2+-param). No model changes needed. |
| `docs/plans/last-arg-vs-last-args.md` | Previous analysis explored adding `LastArg` as alias. Recommended Option C (both properties always). This plan supersedes that with a cleaner type-level separation. |
| `docs/todos/ttuple-interceptors.md` | Context for the TTuple migration that created the current architecture. |
| `src/KnockOff/Interceptors/Slots/IVoidOverloadSlots.cs` | 8 slot interfaces, each with `VoidMethodInterceptor<TDelegate, TArgs>` property. Currently has `where TDelegate : Delegate` only -- no `where TArgs : struct`. |
| `src/KnockOff/Interceptors/Slots/IMethodOverloadSlots.cs` | 8 slot interfaces with `MethodInterceptor<TDelegate, TArgs, TReturn>` property. Same constraint pattern. |
| `src/KnockOff/Interceptors/Slots/IAsyncMethodOverloadSlots.cs` | 8 slot interfaces with `AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>` property. |
| `src/KnockOff/Interceptors/Slots/IAsyncVoidOverloadSlots.cs` | 8 slot interfaces with `AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs>` property. |
| `src/KnockOff/Interceptors/Slots/VoidSlotExtensions.cs` | Extension methods (Call/When) per slot. Each method references `VoidMethodInterceptor<TDelegate, TArgs>` return types. |
| `src/KnockOff/Interceptors/Slots/MethodSlotExtensions.cs` | Extension methods (Return/When) per slot. References `MethodInterceptor<TDelegate, TArgs, TReturn>`. |
| `src/KnockOff/Interceptors/Slots/AsyncMethodSlotExtensions.cs` | Extension methods per slot. References `AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>`. |
| `src/KnockOff/Interceptors/Slots/AsyncVoidSlotExtensions.cs` | Extension methods per slot. References `AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs>`. |

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-02-16 (second review)

### Concern Resolutions

#### Concern 1: Plan still says "include deprecated alias" -- RESOLVED

All references to deprecated `LastArgs` alias on the 1-param tier have been removed from the plan. The decision is now: **clean break only**. `LastArg` on single-param interceptors, `LastArgs` on 2+-param interceptors. No `[Obsolete]` alias. Code that uses `LastArgs` on a single-param method will get a compile error, and users update to `LastArg`.

Specific changes made to this plan:
- Breaking Changes section: Mitigation changed from deprecated alias to "clean break"
- Risk Assessment table: Mitigation column updated
- Open Question 1: Marked as resolved
- Phase 4: Changed from "remove deprecated alias" to "documentation"

#### Concern 2: Slot interfaces need `where TArgs : struct` propagated -- RESOLVED

**Investigation findings:**

The slot interfaces (`IVoidOverloadSlot1<TDelegate, TArgs>`, `IMethodOverloadSlot1<TDelegate, TArgs, TReturn>`, etc.) and their extension methods reference the TTuple interceptor types as return types. For example:

```csharp
// IVoidOverloadSlots.cs
public interface IVoidOverloadSlot1<TDelegate, TArgs> where TDelegate : Delegate
{
    VoidMethodInterceptor<TDelegate, TArgs> VoidSlot1Interceptor { get; }
}
```

When we add `where TArgs : struct` to `VoidMethodInterceptor<TDelegate, TArgs>`, any type that references `VoidMethodInterceptor<TDelegate, TArgs>` as a closed generic (where `TArgs` is a type parameter of the referencing type) must also constrain its own `TArgs` with `where TArgs : struct`. Otherwise the compiler will report:

```
CS0314: The type 'TArgs' cannot be used as type parameter 'TArgs' in the generic type...
There is no boxing conversion from 'TArgs' to 'System.ValueType'.
```

**Full inventory of files requiring `where TArgs : struct`:**

| File | Count of interfaces/methods | Change |
|---|---|---|
| `src/KnockOff/Interceptors/Slots/IVoidOverloadSlots.cs` | 8 interfaces | Add `where TArgs : struct` |
| `src/KnockOff/Interceptors/Slots/IMethodOverloadSlots.cs` | 8 interfaces | Add `where TArgs : struct` |
| `src/KnockOff/Interceptors/Slots/IAsyncVoidOverloadSlots.cs` | 8 interfaces | Add `where TArgs : struct` |
| `src/KnockOff/Interceptors/Slots/IAsyncMethodOverloadSlots.cs` | 8 interfaces | Add `where TArgs : struct` |
| `src/KnockOff/Interceptors/Slots/VoidSlotExtensions.cs` | 24 methods (3 per slot x 8) | Add `where TArgs : struct` |
| `src/KnockOff/Interceptors/Slots/MethodSlotExtensions.cs` | 32 methods (4 per slot x 8) | Add `where TArgs : struct` |
| `src/KnockOff/Interceptors/Slots/AsyncVoidSlotExtensions.cs` | 32 methods (4 per slot x 8) | Add `where TArgs : struct` |
| `src/KnockOff/Interceptors/Slots/AsyncMethodSlotExtensions.cs` | 40 methods (5 per slot x 8) | Add `where TArgs : struct` |
| **Total** | **32 interfaces + 128 extension methods** | |

These are all mechanical search-and-replace changes: append `, struct` (or a new `where TArgs : struct` line) to each generic constraint.

**Updated Files to Modify table** (added in Phase 1):

| File | Change |
|---|---|
| `src/KnockOff/Interceptors/Slots/IVoidOverloadSlots.cs` | Add `where TArgs : struct` to all 8 interfaces |
| `src/KnockOff/Interceptors/Slots/IMethodOverloadSlots.cs` | Add `where TArgs : struct` to all 8 interfaces |
| `src/KnockOff/Interceptors/Slots/IAsyncVoidOverloadSlots.cs` | Add `where TArgs : struct` to all 8 interfaces |
| `src/KnockOff/Interceptors/Slots/IAsyncMethodOverloadSlots.cs` | Add `where TArgs : struct` to all 8 interfaces |
| `src/KnockOff/Interceptors/Slots/VoidSlotExtensions.cs` | Add `where TArgs : struct` to all 24 extension methods |
| `src/KnockOff/Interceptors/Slots/MethodSlotExtensions.cs` | Add `where TArgs : struct` to all 32 extension methods |
| `src/KnockOff/Interceptors/Slots/AsyncVoidSlotExtensions.cs` | Add `where TArgs : struct` to all 32 extension methods |
| `src/KnockOff/Interceptors/Slots/AsyncMethodSlotExtensions.cs` | Add `where TArgs : struct` to all 40 extension methods |

**Implementation note:** These changes are safe and mechanical. The slot interfaces are only ever instantiated with 2+-param overloads (see Concern 3), so `TArgs` is always a `ValueTuple`, which is a struct. Adding the constraint simply makes explicit what was already true at runtime.

#### Concern 3: `BuildSlotInterfaceList()` must skip 1-param overloads -- RESOLVED

**Investigation findings:**

`BuildSlotInterfaceList()` at line 1028 of `PreCompiledInterceptorRenderer.cs` currently skips only `paramCount == 0`:

```csharp
if (overload.Parameters.Count == 0) continue; // Zero-param overloads don't use slots
```

With this change, 1-param overloads will use `*Interceptor1` types (e.g., `VoidMethodInterceptor1<TDelegate, TArg>`), but the slot interfaces only reference the TTuple types (`VoidMethodInterceptor<TDelegate, TArgs>`). The return type of the slot property would not match the field type, causing a compile error.

**Required changes:**

1. **`BuildSlotInterfaceList()`** (line 1041): Change `== 0` to `<= 1`:
   ```csharp
   if (overload.Parameters.Count <= 1) continue; // 0-param and 1-param overloads don't use slots
   ```

2. **`RenderSlotInterfacePropertyImplementations()`** (line 1092): Same change:
   ```csharp
   if (overload.Parameters.Count <= 1) continue;
   ```

3. **`RenderOverloadCompositorClass()`** -- the delegate declaration loop (line 839) currently skips `paramCount == 0`:
   ```csharp
   if (overload.Parameters.Count > 0)
   ```
   This does NOT need to change. 1-param overloads still have delegates. The change is only to slot interface construction.

**Impact:** 1-param overloads in compositors will lack slot interfaces, which means the pre-compiled extension methods in `VoidSlotExtensions`, `MethodSlotExtensions`, etc. will not apply to 1-param overloads. Instead, the generated forwarding methods (Return/Call/When) on the compositor class will handle 1-param overloads, just as they handle 0-param overloads today. This is functionally equivalent -- the forwarding methods are the primary API surface.

#### Concern 4: Test update scope is ~80-120+ assertions -- RESOLVED

**Investigation findings (actual counts from codebase):**

- `.LastArgs` occurrences across `src/Tests/`: **177 occurrences across 57 files**
- `.LastArg` occurrences across `src/Tests/`: **31 occurrences across 15 files** (these are existing 1-param tests already using `LastArg` via the explicit interface impl -- they are correct and do not need changes)
- `.LastArgs` occurrences across `src/Design/`: **20 occurrences across 10 files**

Not all 177 `.LastArgs` occurrences need changing -- only those that reference single-param methods. Methods with 2+ params keep `LastArgs`. A rough estimate:

- **Single-param `LastArgs` in tests**: ~80-100 assertions (the majority of methods in tests are 1-param)
- **Single-param `LastArgs` in Design projects**: ~10-12 assertions
- **Total estimated changes**: ~90-120 lines

**Implementation strategy:**

1. Complete Phase 1 (library types) and Phase 2 (generator changes)
2. Build the solution -- the compiler will flag every `LastArgs` usage that now fails (because the interceptor type changed from `*Interceptor<D, TArgs, R>` to `*Interceptor1<D, TArg, R>` which has `LastArg` not `LastArgs`)
3. For each compiler error, change `LastArgs` to `LastArg`
4. Build again -- repeat until zero errors
5. Run all tests to verify

The compiler is the definitive guide. No manual search-and-replace guesswork needed. This is mechanical but high-volume work.

**Additional test-adjacent changes:**
- Tests that assert on `LastArgs` for interceptor UNIT tests (e.g., `MethodInterceptorTests.cs`, `VoidMethodInterceptorTests.cs`) -- these test the library types directly, so 1-param test cases will need `LastArg`
- Tests that use `stub.Method.LastArgs` for generated stubs -- the generated property name is determined by the interceptor type, so these also need updating for 1-param methods

#### Concern 5: No Design.Stubs compilation evidence -- RESOLVED

**Acceptance criteria for Design.Stubs:**

After implementation, the following must compile and pass in `src/Design/`:

1. **Single-param method with `LastArg`** -- at least one Design.Stubs file must demonstrate:
   ```csharp
   stub.MethodName.Return(value);
   target.MethodName(arg);
   Assert.Equal(arg, stub.MethodName.LastArg);   // singular
   ```

2. **Multi-param method with `LastArgs`** -- existing Design.Stubs code must continue to work:
   ```csharp
   stub.MethodName.Return(value);
   target.MethodName(a, b);
   Assert.Equal((a, b), stub.MethodName.LastArgs); // plural, tuple
   ```

3. **Zero-param method** -- no `LastArg`/`LastArgs` (unchanged)

4. **All patterns where applicable:**
   - Standalone: single-param `LastArg` works
   - Inline Interface: single-param `LastArg` works
   - Inline Class: single-param `LastArg` works
   - Standalone Class: single-param `LastArg` works

5. **Builder `LastArg`** -- `stub.Method.Return(callback).LastArg` returns the single arg

6. **Overload compositors** -- an overloaded method with both a 1-param and 2-param overload generates correctly (1-param uses `*Interceptor1`, 2-param uses `*Interceptor`)

**Verification gate:** `dotnet build src/Design/Design.Stubs && dotnet test src/Design/Design.Tests`

**Note:** Design.Stubs code that currently uses `LastArgs` on single-param methods will need updating to `LastArg` as part of this implementation. The developer should update these as compiler errors guide them.

#### Concern 6: Compositor `RenderOverloadInvokeMethod` confirmation -- RESOLVED

**Investigation findings:**

The compositor's `RenderOverloadInvokeMethod` in `PreCompiledInterceptorRenderer.cs` (line 1303) generates invoke methods that forward to the inner interceptor field. For a 1-param overload:

```csharp
// Generated code for 1-param:
internal TReturn Invoke_String(bool strict, string name) => _ov1.Invoke(strict, name);
```

The method calls `_ov{N}.Invoke(strict, {arg})`. The field type (`_ov1`) is determined by `GetOverloadInterceptorType()`. After the change:

- **Before:** `_ov1` is `VoidMethodInterceptor<D, string>` -- `Invoke(bool strict, string args)`
- **After:** `_ov1` is `VoidMethodInterceptor1<D, string>` -- `Invoke(bool strict, string arg)`

Both have the same `Invoke` signature shape: `Invoke(bool strict, TArg arg)`. The parameter TYPE is the same (the raw type, e.g., `string`). Only the parameter NAME differs (`args` vs `arg`), which does not affect call-site compatibility since the compositor calls by position, not by name.

The compositor invoke routing code (lines 1319-1336) handles the 1-param case specifically:

```csharp
else if (overload.Parameters.Count == 1)
{
    var p = overload.Parameters.First();
    paramDecls = (options.IncludeStrictParameter ? ", " : "") + $"{p.Type} {p.EscapedName}";
    invokeArgs = $", {p.EscapedName}";
}
```

This passes the raw parameter value to `_ovN.Invoke()`, which works identically for both `*Interceptor<D, TArgs>` and `*Interceptor1<D, TArg>` because the `Invoke` method signature is the same for 1-param types.

**Confirmed:** No changes needed to `RenderOverloadInvokeMethod`. The invoke routing is unaffected by the interceptor type change.

### Second Review: Approval

**Why This Plan Is Approved:**

All six concerns from the first review have been thoroughly addressed with concrete code references, exact file/line citations, and verified counts. The plan now covers:

1. Clean break (no deprecated alias) -- explicitly stated, no contradictions remain
2. Slot interface cascading -- 32 interfaces + 128 extension methods inventoried with exact file paths and counts (all verified against the actual codebase)
3. Generator slot skipping -- `BuildSlotInterfaceList()` and `RenderSlotInterfacePropertyImplementations()` changes specified with before/after code
4. Test scope -- ~90-120 estimated changes acknowledged, compiler-driven strategy documented
5. Design.Stubs acceptance criteria -- 6 specific criteria defined
6. Compositor invoke routing -- confirmed unaffected with code trace

**Remaining items that are acceptable implementation-time decisions:**
- The exact explicit interface implementation lines that change when TTuple builders switch from `IMethodReturnBuilder<TDelegate, TArgs?>` to `IMethodReturnBuilderArgs<TDelegate, TArgs?>` (the compiler will guide this)
- Whether `IMethodTrackingArgs<TArgs?>.LastArgs` is satisfied implicitly by the existing public `LastArgs` property or needs an explicit impl (the compiler will determine this)

**Review summary:**
- Files examined: 20+ source files across interceptors, interfaces, slots, renderer, builder
- Extension method counts verified: VoidSlotExtensions (24), MethodSlotExtensions (32), AsyncVoidSlotExtensions (32), AsyncMethodSlotExtensions (40)
- Slot interface counts verified: 4 files x 8 interfaces = 32
- All 9 patterns addressed via centralized `GetMethodInterceptorType()` / `GetOverloadInterceptorType()`
- All 4 member types: methods covered; properties/indexers/events are unaffected (they use different interceptor types)

---

## Implementation Contract

**Created:** 2026-02-16
**Approved by:** knockoff-developer

### Design.Stubs Acceptance Criteria

After implementation, these must compile and pass:

1. Single-param method `LastArg` on standalone pattern (Design.Stubs)
2. Single-param method `LastArg` on inline interface pattern (Design.Stubs)
3. Multi-param method `LastArgs` continues working (existing Design.Stubs code)
4. Builder `LastArg` -- `stub.Method.Return(callback).LastArg` works for single-param
5. Mixed overload compositor with 1-param and 2+-param overloads generates correctly
6. `dotnet build src/Design/Design.Stubs` succeeds
7. `dotnet test src/Design/Design.Tests` passes

### In Scope

#### Phase 1: Library Types (src/KnockOff/)

- [ ] Create `src/KnockOff/Interceptors/MethodInterceptor1.cs` (copy from MethodInterceptor.cs, rename: class `MethodInterceptor1`, `TArg` instead of `TArgs`, `LastArg` instead of `LastArgs`, `_lastArg`/`_unconfiguredLastArg` fields, builder implements `IMethodReturnBuilder<TDelegate, TArg?>`)
- [ ] Create `src/KnockOff/Interceptors/VoidMethodInterceptor1.cs` (same pattern, builder implements `IMethodCallBuilder<TDelegate, TArg?>`)
- [ ] Create `src/KnockOff/Interceptors/AsyncMethodInterceptor1.cs` (same pattern with `TSyncDelegate`)
- [ ] Create `src/KnockOff/Interceptors/AsyncVoidMethodInterceptor1.cs` (same pattern with `TSyncDelegate`)
- [ ] Modify `src/KnockOff/Interceptors/MethodInterceptor.cs`: add `where TArgs : struct`, change builder from `IMethodReturnBuilder<TDelegate, TArgs?>` to `IMethodReturnBuilderArgs<TDelegate, TArgs?>`, remove `IMethodTracking<TArgs?>.LastArg` explicit impl, update all explicit interface impl lines from `IMethodTracking<TArgs?>` to `IMethodTrackingArgs<TArgs?>` and `IMethodReturnBuilder<TDelegate, TArgs?>` to `IMethodReturnBuilderArgs<TDelegate, TArgs?>`
- [ ] Modify `src/KnockOff/Interceptors/VoidMethodInterceptor.cs`: same changes (IMethodCallBuilder -> IMethodCallBuilderArgs)
- [ ] Modify `src/KnockOff/Interceptors/AsyncMethodInterceptor.cs`: same changes
- [ ] Modify `src/KnockOff/Interceptors/AsyncVoidMethodInterceptor.cs`: same changes
- [ ] Add `where TArgs : struct` to all 8 interfaces in `src/KnockOff/Interceptors/Slots/IVoidOverloadSlots.cs`
- [ ] Add `where TArgs : struct` to all 8 interfaces in `src/KnockOff/Interceptors/Slots/IMethodOverloadSlots.cs`
- [ ] Add `where TArgs : struct` to all 8 interfaces in `src/KnockOff/Interceptors/Slots/IAsyncVoidOverloadSlots.cs`
- [ ] Add `where TArgs : struct` to all 8 interfaces in `src/KnockOff/Interceptors/Slots/IAsyncMethodOverloadSlots.cs`
- [ ] Add `where TArgs : struct` to all 24 methods in `src/KnockOff/Interceptors/Slots/VoidSlotExtensions.cs`
- [ ] Add `where TArgs : struct` to all 32 methods in `src/KnockOff/Interceptors/Slots/MethodSlotExtensions.cs`
- [ ] Add `where TArgs : struct` to all 32 methods in `src/KnockOff/Interceptors/Slots/AsyncVoidSlotExtensions.cs`
- [ ] Add `where TArgs : struct` to all 40 methods in `src/KnockOff/Interceptors/Slots/AsyncMethodSlotExtensions.cs`
- [ ] **Checkpoint:** `dotnet build src/KnockOff/KnockOff.csproj` succeeds

#### Phase 2: Generator Changes (src/Generator/)

- [ ] Update `PreCompiledInterceptorRenderer.GetMethodInterceptorType()` -- add `paramCount == 1` branch returning `*Interceptor1` types for all 4 families (void, sync, async void, async)
- [ ] Update `PreCompiledInterceptorRenderer.GetOverloadInterceptorType()` -- same 3-way branch
- [ ] Update `BuildSlotInterfaceList()` -- change `== 0` to `<= 1` at line 1041
- [ ] Update `RenderSlotInterfacePropertyImplementations()` -- change `== 0` to `<= 1` at line 1092
- [ ] Verify `GetCallBuilderType()` and `GetWhenBuilderType()` need no changes (they derive from `GetOverloadInterceptorType()` which now returns the correct type)
- [ ] **Checkpoint:** `dotnet build src/Generator/Generator.csproj` succeeds

#### Phase 3: Tests and Design Projects

- [x] Build full solution (`dotnet build src/KnockOff.sln`) -- collect all `LastArgs` compile errors on single-param methods
- [x] Update `src/Design/` files: change single-param `LastArgs` to `LastArg` (compiler-guided)
- [x] Update `src/Tests/` files: change single-param `LastArgs` to `LastArg` (compiler-guided, ~107 lines via targeted sed + manual fixes)
- [x] Fix nullable tuple deconstruction: add `.Value` for multi-param `LastArgs` tuple deconstruction (9 locations)
- [x] Fix `SlotExtensionTests.cs`: change single-param (`string`) TArgs to 2+-param tuples for `where TArgs : struct` constraint
- [x] Fix `InterfaceContractTests.cs`: update `IMethodCallBuilder<>` to `IMethodCallBuilderArgs<>` and `IMethodTracking<>` to `IMethodTrackingArgs<>` for multi-param methods
- [x] Fix benchmarks: update `IMethodTracking<(int,int)>` to `IMethodTrackingArgs<(int,int)?>` in VerificationBenchmarks and OverloadedMethodBenchmarks
- [x] Fix Design.Stubs: 3 single-param `LastArgs` to `LastArg`, 1 nullable tuple deconstruction `.Value`
- [x] Fix Design.Tests: 2 single-param `LastArgs` to `LastArg`, 1 nullable tuple deconstruction `.Value`
- [x] **Checkpoint:** `dotnet build src/KnockOff.sln` succeeds (zero errors)
- [x] **Checkpoint:** `dotnet test src/KnockOff.sln` -- all tests pass (0 failures)
- [x] **Checkpoint:** `dotnet build src/Design/Design.Stubs` succeeds (zero errors)
- [x] **Checkpoint:** `dotnet test src/Design/Design.Tests` -- all tests pass (0 failures)

### Explicitly Out of Scope

- New slot interfaces for the 1-param tier (deferred -- generated forwarding methods handle it)
- `ITuple` constraint on TTuple tier (deferred -- `struct` is sufficient)
- Release notes / documentation updates (separate phase)
- Changes to `DelegateInvokerFactory`, `UnifiedInterceptorBuilder`, or any model types
- Changes to `ComputeTArgsType()`, `FormatInvokeArgs()`, `BuildDelegateDeclaration()`, `BuildSyncDelegateDeclaration()`, `GetMethodSourceFallbackExpression()`, `GetStubOverrideFallbackExpression()`, `GetMethodSmartDefaultFactory()`

### Verification Gates

1. **After Phase 1:** `dotnet build src/KnockOff/KnockOff.csproj` succeeds. The 4 new interceptor files compile. The 4 modified TTuple interceptors compile with `where TArgs : struct` and `*BuilderArgs` interfaces. All 8 slot interface files and 4 slot extension files compile with `where TArgs : struct`.
2. **After Phase 2:** `dotnet build src/Generator/Generator.csproj` succeeds. The generator produces `*Interceptor1` types for 1-param methods.
3. **After Phase 3:** `dotnet build src/KnockOff.sln` and `dotnet test src/KnockOff.sln` succeed. `dotnet build src/Design/Design.Stubs` and `dotnet test src/Design/Design.Tests` succeed. All Design.Stubs acceptance criteria verified.

### Stop Conditions

If any of these occur, STOP and report:
- Out-of-scope test fails (a test that was passing before AND is not related to `LastArgs`/`LastArg` renaming)
- Architectural contradiction discovered (e.g., a pipeline that does NOT flow through `GetMethodInterceptorType()`)
- Generated code does not compile after Phase 2+3 changes
- `DelegateInvokerFactory` expression trees fail at runtime for `*Interceptor1` types (unexpected -- but stop if it happens)
