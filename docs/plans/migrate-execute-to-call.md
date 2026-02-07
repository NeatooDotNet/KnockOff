# Singular API Naming: Return/Call/ThenReturn/ThenCall

**Date:** 2026-02-07
**Related Todo:** [Migrate Execute to Call in Void Method API](../todos/migrate-execute-to-call.md)
**Status:** Awaiting Verification
**Last Updated:** 2026-02-07 (Phases 1-4 implemented, awaiting architect verification)

---

## Overview

Three mechanical renames in a single release to achieve consistent singular naming:

1. **Returns -> Return**: Rename all `Returns`/`ThenReturns` in the non-void method API to `Return`/`ThenReturn`. This applies to entry points, sequence chaining, When chain value methods, and all builder/sequence interfaces.
2. **Execute -> Call**: Rename all `Execute`/`ThenExecute` in the void method callback API to `Call`/`ThenCall`. This reverses the v0.38.0 naming choice for void methods.
3. **Typed handler OnCall -> Return/Call**: Rename `.Of<T>().OnCall(callback)` to `.Of<T>().Return(callback)` (non-void) or `.Of<T>().Call(callback)` (void). This closes the known generator gap from v0.38.0 where typed handlers still used `OnCall`.

The final API will be:

- Non-void methods: `.Return(callback)` / `.ThenReturn(callback)` (currently `.Returns()` / `.ThenReturns()`)
- Non-void value overload: `.Return(value)` / `.ThenReturn(value)` (currently `.Returns(value)` / `.ThenReturns(value)`)
- Non-void When chain: `.When(...).Return(value)` (currently `.When(...).Returns(value)`)
- Void methods: `.Call(callback)` / `.ThenCall(callback)` (currently `.Execute()` / `.ThenExecute()`)
- Non-void generic typed handlers: `.Of<T>().Return(callback)` (currently `.Of<T>().OnCall(callback)`)
- Void generic typed handlers: `.Of<T>().Call(callback)` (currently `.Of<T>().OnCall(callback)`)

After this change, none of `Returns`, `ThenReturns`, `Execute`, `ThenExecute`, or `OnCall` should appear in the user-facing API. The API uses singular forms throughout: `Return`, `ThenReturn`, `Call`, `ThenCall`.

---

## Approach

Three renames in a single pass across three API layers:
- **Returns->Return** and **Execute->Call** are in `MethodInterceptorRenderer.cs` (shared renderer used by all 9 patterns)
- **OnCall->Return/Call** is in `FlatRenderer.cs` and `InlineRenderer.cs` (typed handler renderers -- a completely separate pipeline)
- **Interface renames** are in `src/KnockOff/` (6 interface files affected)

**Implementation order:**
1. Rename public interfaces (src/KnockOff/) -- all three renames
2. Update shared generator renderer and builder references (src/Generator/ -- MethodInterceptorRenderer, UnifiedInterceptorBuilder, ModelAdapters) -- Returns->Return and Execute->Call
3. Update typed handler renderers (src/Generator/ -- FlatRenderer, InlineRenderer) -- OnCall->Return/Call
4. Update tests -- all three renames
5. Update Design projects -- all three renames
6. Update documentation, skills, samples -- all three renames
7. Version bump and cleanup

---

## Design

### API Before/After

| Context | Before (v0.38.0) | After |
|---------|-------------------|-------|
| Non-void callback | `stub.Add.Returns((a,b) => a+b)` | `stub.Add.Return((a,b) => a+b)` |
| Non-void value | `stub.Add.Returns(42)` | `stub.Add.Return(42)` |
| Non-void sequence | `stub.Add.Returns(cb).ThenReturns(cb2)` | `stub.Add.Return(cb).ThenReturn(cb2)` |
| Non-void value sequence | `stub.Add.Returns(1,2,3)` | `stub.Add.Return(1,2,3)` |
| Non-void simplified async | `stub.GetAsync.Returns((id) => user)` | `stub.GetAsync.Return((id) => user)` |
| Non-void When chain value | `stub.Add.When(1,2).Returns(3)` | `stub.Add.When(1,2).Return(3)` |
| Non-void When terminal | `...Return(100).ThenCall(cb)` | unchanged (ThenCall already correct) |
| Void method callback | `stub.Reset.Execute(() => count++)` | `stub.Reset.Call(() => count++)` |
| Void sequence | `stub.Reset.Execute(cb1).ThenExecute(cb2)` | `stub.Reset.Call(cb1).ThenCall(cb2)` |
| Void async (simplified) | `stub.SaveAsync.Execute((d) => saved = d)` | `stub.SaveAsync.Call((d) => saved = d)` |
| Void When chain action | `stub.Process.When(1,2).Execute(cb)` | `stub.Process.When(1,2).Call(cb)` |
| Void When chain terminal | `...Execute(cb).ThenExecute(cb2)` | `...Call(cb).ThenCall(cb2)` |
| Non-void generic typed handler | `stub.GetById.Of<User>().OnCall((id) => user)` | `stub.GetById.Of<User>().Return((id) => user)` |
| Void generic typed handler | `stub.Process.Of<string>().OnCall((val) => captured = val)` | `stub.Process.Of<string>().Call((val) => captured = val)` |
| Generic overload (non-void) | `stub.Convert.Of<int,string>().OnCall((i) => i.ToString())` | `stub.Convert.Of<int,string>().Return((i) => i.ToString())` |
| Generic overload (void) | *no current void generic overload tests* | `.Call{Suffix}(callback)` if void |

### Interface Renames

**Returns -> Return (non-void):**

| Current (v0.38.0) | Target | File |
|--------------------|--------|------|
| `IMethodReturnsBuilder<TCallback>` | `IMethodReturnBuilder<TCallback>` | `IMethodReturnsBuilder.cs` -> `IMethodReturnBuilder.cs` |
| `IMethodReturnsBuilder<TCallback, TArg>` | `IMethodReturnBuilder<TCallback, TArg>` | same file |
| `IMethodReturnsBuilderArgs<TCallback, TArgs>` | `IMethodReturnBuilderArgs<TCallback, TArgs>` | same file |
| `.ThenReturns(callback)` on builders | `.ThenReturn(callback)` | same file |
| `IMethodReturnsSequence` | `IMethodReturnSequence` | `IMethodReturnsSequence.cs` -> `IMethodReturnSequence.cs` |
| `IMethodReturnsSequence<TCallback>` | `IMethodReturnSequence<TCallback>` | same file |
| `.ThenReturns(callback)` on sequences | `.ThenReturn(callback)` | same file |
| `IWhenBuilder<TDelegate, TReturn>.Returns(value)` | `IWhenBuilder<TDelegate, TReturn>.Return(value)` | `IWhenTracking.cs` |

**Execute -> Call (void):**

| Current (v0.38.0) | Target | File |
|--------------------|--------|------|
| `IMethodExecuteBuilder<TCallback>` | `IMethodCallBuilder<TCallback>` | `IMethodExecuteBuilder.cs` -> `IMethodCallBuilder.cs` |
| `IMethodExecuteBuilder<TCallback, TArg>` | `IMethodCallBuilder<TCallback, TArg>` | same file |
| `IMethodExecuteBuilderArgs<TCallback, TArgs>` | `IMethodCallBuilderArgs<TCallback, TArgs>` | same file |
| `.ThenExecute(callback)` on builders | `.ThenCall(callback)` | same file |
| `IMethodExecuteSequence` | `IMethodCallSequence` | `IMethodExecuteSequence.cs` -> `IMethodCallSequence.cs` |
| `IMethodExecuteSequence<TCallback>` | `IMethodCallSequence<TCallback>` | same file |
| `IVoidWhenChain.Execute(callback)` | `IVoidWhenChain.Call(callback)` | `IWhenTracking.cs` |
| `IVoidWhenChain.ThenExecute(callback)` (void terminal) | `IVoidWhenChain.ThenCall(callback)` | `IWhenTracking.cs` |

**Note on CA1716:** The `IVoidWhenChain.Call()` method will need `#pragma warning disable CA1716` again -- `Call` triggers the "identifiers should not match keywords" rule (it matched in the original pre-v0.38.0 code). This was removed when `Call` was renamed to `Execute`; it must be re-added.

**Note on IWhenBuilder.Return(value):** The non-void When chain `IWhenBuilder<TDelegate, TReturn>.Returns(TReturn value)` becomes `.Return(value)`. This is a value return (not a callback), but the user wants all plural `Returns` gone. The generated WhenBuilder class method and all explicit interface implementations must be renamed.

### Generator Changes -- Returns -> Return (MethodInterceptorRenderer.cs)

All changes in `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs`:

#### Entry point names

| Line | Current | Target |
|------|---------|--------|
| ~170 | `var entryPointName = model.IsVoid ? "Execute" : "Returns"` | `"Return"` |
| ~208 | `public MethodCallBuilderImpl Returns({valueStorageType} value)` | `Return(...)` |
| ~235 | `public MethodSequenceImpl Returns({valueStorageType} first, params ...)` | `Return(...)` |
| ~275 | `public MethodCallBuilderImpl Returns({simplifiedDelegateType} callback)` (simplified async) | `Return(...)` |
| ~485 | `var overloadEntryPointName = overload.IsVoid ? "Execute" : "Returns"` | `"Return"` |
| ~516 | `public MethodCallBuilderImpl_{suffix} Returns({simplifiedDelegateType} callback)` (overload async) | `Return(...)` |

#### Sequence chaining ThenReturns -> ThenReturn

| Line | Current | Target |
|------|---------|--------|
| ~1552 | `var thenChainName = isVoid ? "ThenExecute" : "ThenReturns"` | `"ThenReturn"` |
| ~1580-1591 | `ThenReturns({valueType} value)` (value wrapper, 3 async variants) | `ThenReturn(...)` |
| ~1597 | `ThenReturns(params {valueType}[] values)` | `ThenReturn(...)` |
| ~1615-1619 | `var seq = ThenReturns(values[0]); seq = seq.ThenReturns(values[i])` | `ThenReturn(...)` |
| ~1625-1640 | Simplified async `ThenReturns({simplifiedDelegateType} callback)` wrappers | `ThenReturn(...)` |
| ~1692 | Explicit interface `IMethodReturnsSequence<T>.ThenReturns(...)` | `IMethodReturnSequence<T>.ThenReturn(...)` |
| ~1720 | `var thenChainName = isVoid ? "ThenExecute" : "ThenReturns"` | `"ThenReturn"` |
| ~1723 | `global::KnockOff.IMethodReturnsSequence<{delegateType}>` | `IMethodReturnSequence<{delegateType}>` |
| ~1726 | `global::KnockOff.IMethodReturnsSequence` | `IMethodReturnSequence` |
| ~1765-1812 | Sequence class `ThenReturns(value)`, `ThenReturns(params)`, simplified async wrappers | `ThenReturn(...)` |
| ~1864 | Explicit interface `IMethodReturnsSequence<T>.ThenReturns(...)` on sequence | `IMethodReturnSequence<T>.ThenReturn(...)` |
| ~1865 | `IMethodReturnsSequence<T>.Verifiable()` | `IMethodReturnSequence<T>.Verifiable()` |

#### Builder/adapter interface references

`src/Generator/Builder/UnifiedInterceptorBuilder.cs` (lines ~286-293):

| Current | Target |
|---------|--------|
| `IMethodReturnsBuilder<{delegateType}>` | `IMethodReturnBuilder<{delegateType}>` |
| `IMethodReturnsBuilder<{delegateType}, {param.Type}>` | `IMethodReturnBuilder<{delegateType}, {param.Type}>` |
| `IMethodReturnsBuilderArgs<{delegateType}, {tupleType}>` | `IMethodReturnBuilderArgs<{delegateType}, {tupleType}>` |

`src/Generator/Renderer/Shared/ModelAdapters.cs` (lines ~217-224):

| Current | Target |
|---------|--------|
| `IMethodReturnsBuilder<{delegateType}>` | `IMethodReturnBuilder<{delegateType}>` |
| `IMethodReturnsBuilder<{delegateType}, {param.Type}>` | `IMethodReturnBuilder<{delegateType}, {param.Type}>` |
| `IMethodReturnsBuilderArgs<{delegateType}, {tupleType}>` | `IMethodReturnBuilderArgs<{delegateType}, {tupleType}>` |

#### Non-void When chain Returns -> Return

| Line | Current | Target |
|------|---------|--------|
| ~1996-2001 | Async `Returns({innerType} value)` on WhenBuilder | `Return(...)` |
| ~2015, ~2017 | Explicit interface `IWhenBuilder<...>.Returns(...)` (async) | `IWhenBuilder<...>.Return(...)` |
| ~2022 | Non-async `Returns({returnType} value)` on WhenBuilder | `Return(...)` |
| ~2030-2031 | Explicit interface `IWhenBuilder<...>.Returns(...)` (non-async) | `IWhenBuilder<...>.Return(...)` |

#### Internal generated field/variable names -- STAY UNCHANGED

These are private fields in generated code, not user-facing API:
- `_returnsValue` (line ~84) -- private value storage
- `_hasReturnsValue` (line ~85) -- private flag
- `_returnsValueTracking` (line ~86) -- private tracking reference
- All 18 `_returns*` references in the renderer emit private fields

Following the same principle as `_onCall` in typed handlers, internal field names stay unchanged.

### Generator Changes -- Execute -> Call (MethodInterceptorRenderer.cs)

All changes in `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs`:

#### Entry points (void-side API method names)

The `IsVoid ? "Execute" : "Returns"` ternaries at lines ~170 and ~485 are updated for BOTH sides in the Returns->Return section above. Below are the Execute-specific remaining changes:

| Line | Current | Target |
|------|---------|--------|
| ~305 | `Execute({voidDelegateType} callback)` (simplified void async, single-sig) | `Call(...)` |
| ~539 | `Execute({voidDelegateType} callback)` (simplified void async, overload) | `Call(...)` |

#### Builder/sequence ThenExecute -> ThenCall

The `isVoid ? "ThenExecute" : "ThenReturns"` ternaries at lines ~1552 and ~1720 are updated for BOTH sides (ThenReturns handled in Returns->Return section above). Below are the Execute-specific remaining changes:

| Line | Current | Target |
|------|---------|--------|
| ~1688 | Explicit interface `IMethodExecuteSequence<T>.ThenExecute(...)` | `IMethodCallSequence<T>.ThenCall(...)` |
| ~1722 | `IMethodExecuteSequence<{delegateType}>` | `IMethodCallSequence<{delegateType}>` |
| ~1725 | `IMethodExecuteSequence` | `IMethodCallSequence` |
| ~1859 | Explicit interface `IMethodExecuteSequence<T>.ThenExecute(...)` | `IMethodCallSequence<T>.ThenCall(...)` |
| ~1860 | `IMethodExecuteSequence<T>.Verifiable()` | `IMethodCallSequence<T>.Verifiable()` |

#### Void When chain Execute -> Call, ThenExecute -> ThenCall

| Line | Current | Target |
|------|---------|--------|
| ~2484 | Summary: "Execute, ThenWhen, ThenExecute" | "Call, ThenWhen, ThenCall" |
| ~2505 | `public {chainType} Execute({delegateType} callback)` | `Call(...)` |
| ~2512-13 | Explicit interface `IVoidWhenChain.Execute(...)` | `IVoidWhenChain.Call(...)` |
| ~2546-48 | `ThenExecute({delegateType} callback)` | `ThenCall(...)` |

#### Internal matcher `Execute()` -> `Call()` (both non-void and void)

**Non-void WhenMatcher classes** (lines ~1895-1957):

| Line | Current | Target |
|------|---------|--------|
| ~1883-84 | `@param` docs: "Matches/Execute signatures", "Return type for Execute method" | "Matches/Call" |
| ~1896 | `var executeParams = BuildMatchParams(parameters)` | rename variable to `callParams` |
| ~1906 | `public abstract {returnType} Execute({executeParams})` | `Call(...)` |
| ~1928 | `public override {returnType} Execute({executeParams}) => _value` | `Call(...)` |
| ~1943 | `public override {returnType} Execute({executeParams}) => _callback(...)` | `Call(...)` |
| ~1954 | `public override {returnType} Execute({executeParams}) => default!` | `Call(...)` |

**Void VoidWhenMatcher classes** (lines ~2398-2465):

| Line | Current | Target |
|------|---------|--------|
| ~2418 | `public abstract void Execute({matchParams})` | `Call(...)` |
| ~2435 | `public override void Execute({matchParams}) { Callback?.Invoke(...) }` | `Call(...)` |
| ~2450 | `public override void Execute({matchParams}) => _callback(...)` | `Call(...)` |
| ~2461 | `public override void Execute({matchParams}) { }` | `Call(...)` |

**Matcher dispatch calls** (where generated code calls `matcher.Execute()`):

| Line | Current | Target |
|------|---------|--------|
| ~1095 | `return matcher.Execute({callbackArgs})` (non-void When dispatch) | `matcher.Call(...)` |
| ~2384 | `matcher.Execute({callbackArgs})` (void When dispatch) | `matcher.Call(...)` |

#### Builder interface references (Execute side only -- Returns side in section above)

`src/Generator/Builder/UnifiedInterceptorBuilder.cs` (lines ~267-281):

| Current | Target |
|---------|--------|
| `IMethodExecuteBuilder<{delegateType}>` | `IMethodCallBuilder<{delegateType}>` |
| `IMethodExecuteBuilder<{delegateType}, {param.Type}>` | `IMethodCallBuilder<{delegateType}, {param.Type}>` |
| `IMethodExecuteBuilderArgs<{delegateType}, {tupleType}>` | `IMethodCallBuilderArgs<{delegateType}, {tupleType}>` |

`src/Generator/Renderer/Shared/ModelAdapters.cs` (lines ~200-212):

| Current | Target |
|---------|--------|
| `IMethodExecuteBuilder<{delegateType}>` | `IMethodCallBuilder<{delegateType}>` |
| `IMethodExecuteBuilder<{delegateType}, {param.Type}>` | `IMethodCallBuilder<{delegateType}, {param.Type}>` |
| `IMethodExecuteBuilderArgs<{delegateType}, {tupleType}>` | `IMethodCallBuilderArgs<{delegateType}, {tupleType}>` |

#### Error messages

| Line | Current | Target |
|------|---------|--------|
| ~849 | `"Configure via Returns or Execute."` | `"Configure via Return or Call."` |
| ~1045 | `"Configure via Returns or Execute."` | `"Configure via Return or Call."` |

#### Comments (all three renames)

| Line | Current | Target |
|------|---------|--------|
| ~12 | "Generates Returns()/Execute() entry points" | "Return()/Call()" |
| ~20 | "per-signature delegates, sequences, and Returns/Execute overloads" | "Return/Call" |
| ~77 | "Value storage for Returns(value) overload" | "Return(value)" |
| ~169 | "Returns()/Execute() - repeating callback" | "Return()/Call()" |
| ~207 | "Configures return value that repeats indefinitely. Returns builder..." | "Return builder..." |
| ~300 | "Execute(Action<...>) - simplified void callback" | "Call(Action<...>)" |
| ~474 | "Returns/Execute overloads for each unique signature" | "Return/Call" |
| ~484 | "Returns/Execute - repeating callback" | "Return/Call" |
| ~486 | "Returns builder for sequence chaining" | "Return builder..." |
| ~534 | "Execute(Action<...>) - simplified void callback" | "Call(Action<...>)" |
| ~1094 | "Execute() returns the full return type" | "Call() returns" |
| ~1220 | "(Returns, Execute, Returns(value), or When)" | "(Return, Call, Return(value), or When)" |
| ~1551 | "ThenReturns()/ThenExecute()" | "ThenReturn()/ThenCall()" |
| ~1553 | "Returns sequence for further chaining" | "Return sequence..." |
| ~1575 | "ThenReturns(value)" | "ThenReturn(value)" |
| ~1625 | "Simplified async ThenReturns" | "Simplified async ThenReturn" |
| ~1685 | "ThenReturns/ThenExecute" | "ThenReturn/ThenCall" |
| ~1754 | "ThenReturns/ThenExecute" | "ThenReturn/ThenCall" |
| ~1765 | "ThenReturns(value)" | "ThenReturn(value)" |
| ~1785 | "ThenReturns(params values)" | "ThenReturn(params values)" |
| ~1799 | "Simplified async ThenReturns" | "Simplified async ThenReturn" |
| ~1856 | "IMethodReturnsSequence/IMethodExecuteSequence" | "IMethodReturnSequence/IMethodCallSequence" |
| ~1868 | "IMethodReturnsSequence/IMethodExecuteSequence" | "IMethodReturnSequence/IMethodCallSequence" |
| ~1962 | "Returns(value) to complete the matcher" | "Return(value)" |
| ~1980 | "awaits Returns(value)" | "awaits Return(value)" |
| ~1996 | "Returns accepts the unwrapped type" | "Return accepts..." |
| ~2021 | "Non-async: Returns accepts the full return type" | "Return accepts..." |
| ~2030 | "Explicit interface implementation for IWhenBuilder.Returns" | "IWhenBuilder.Return" |
| ~2192-2209 | "Returns builder for Returns()" | "Return builder for Return()" |
| ~2221 | "Matches/Execute methods" | "Matches/Call methods" |
| ~2380 | "ThenWhen and ThenExecute" | "ThenWhen and ThenCall" |
| ~2383 | "Execute (void) and return" | "Call (void) and return" |
| ~2502 | "Execute - sets optional callback" | "Call - sets optional callback" |
| ~2546 | "ThenExecute - terminal with callback" | "ThenCall - terminal with callback" |
| ~2816 | "Returns/Execute" | "Return/Call" |

### Typed Handler Generator Changes (FlatRenderer.cs and InlineRenderer.cs)

Typed handlers for generic methods use a completely separate rendering pipeline from `MethodInterceptorRenderer`. They are rendered directly by `FlatRenderer.cs` (standalone patterns) and `InlineRenderer.cs` (inline patterns). The current code emits `OnCall` for all typed handlers regardless of void/non-void. This needs to be split using singular naming.

**Key finding:** `IsVoid` is available on all relevant handler models:
- `FlatGenericMethodHandlerModel.IsVoid` (single-signature)
- `FlatGenericTypeArityGroup.IsVoid` (arity group level)
- `FlatGenericSignatureGroup.IsVoid` (per-signature in overload groups)
- `InlineGenericMethodHandlerModel.IsVoid` (inline single-signature)

#### FlatRenderer.cs changes

**Single-signature typed handler** (line ~1201):
```
Current:  public global::KnockOff.IMethodTracking OnCall({handler.MethodName}Delegate callback) { _onCall = callback; return this; }
Non-void: public global::KnockOff.IMethodTracking Return({handler.MethodName}Delegate callback) { _onCall = callback; return this; }
Void:     public global::KnockOff.IMethodTracking Call({handler.MethodName}Delegate callback) { _onCall = callback; return this; }
```
Use `handler.IsVoid` to select the method name.

**Overload group typed handler** (line ~1413):
```
Current:  public global::KnockOff.IMethodTracking OnCall{sig.SignatureSuffix}({sig.DelegateName} callback) { _onCall{sig.SignatureSuffix} = callback; return this; }
Non-void: public global::KnockOff.IMethodTracking Return{sig.SignatureSuffix}(...)
Void:     public global::KnockOff.IMethodTracking Call{sig.SignatureSuffix}(...)
```
Use `sig.IsVoid` to select the method name.

**Error messages:**
- Line ~1932: `"Set the handler's OnCall."` -> `"Set the handler's Return or Call."`
- Line ~2244: `"Use {interceptorAccess}.OnCall(callback)."` -> `"Use {interceptorAccess}.Return(callback)."` or `"Use {interceptorAccess}.Call(callback)."` depending on `method.IsVoid`

**Comments** (lines ~1199, ~1409, ~2141, ~2148, ~2178, ~2209): Update `OnCall` references to `Return/Call`.

**Internal field names stay unchanged:**
- `_onCall` field (line ~1176) -- private, not user-facing
- `_onCall{sig.SignatureSuffix}` fields (line ~1382) -- private, not user-facing
- `Callback` property (line ~1206) -- internal, not user-facing

#### InlineRenderer.cs changes

**Single-signature typed handler** (line ~818):
```
Current:  public global::KnockOff.IMethodTracking OnCall({handler.MethodName}Delegate callback) { _onCall = callback; return this; }
Non-void: public global::KnockOff.IMethodTracking Return(...)
Void:     public global::KnockOff.IMethodTracking Call(...)
```
Use `handler.IsVoid` to select the method name.

**Error message** (line ~1080): `"Set the handler's OnCall."` -> `"Set the handler's Return or Call."`

**Comments** (lines ~734, ~735, ~816, ~845, ~846): Update `OnCall` references.

**Internal field names stay unchanged:** `_onCall` (line ~793), `Callback` property (line ~823).

#### Internal model property names -- out of scope

`OnCallDelegateType` on `UnifiedMethodInterceptorModel`, `FlatMethodModel`, `InlineDelegateStubModel`, and related properties like `OnCallArgs`, `OnCallArgumentList` in builder/model code are internal property names that do not appear in generated output. Renaming these would be a cosmetic refactor that adds risk without user-facing benefit. They are explicitly out of scope.

### Out of Scope

#### StandaloneClassRenderer.cs `Execute_()` forwarder

**NOT in scope.** The comments at lines ~157 and ~538 of `StandaloneClassRenderer.cs` mention `Execute_()` -- this refers to a user method forwarder where the domain class (`ServiceBase`) happens to have a method named `Execute(string command)`. The generated user method override is named `Execute_()` following the `MethodName_()` naming convention. This has nothing to do with the void callback API. The `Execute_` name is derived from the *member name on the target type*, not from the `.Execute()` API.

**Verification:** `src/Design/Design.Domain/Abstractions/ServiceBase.cs` line 46: `public abstract void Execute(string command)`. The user method stubs at `Design.Stubs/UserMethods/UserMethodBasics.cs` lines 77, 93 override `Execute_()`. This is the domain method name, not the API.

#### Non-void When chain ThenCall

The `ThenCall` on `IWhenChain<TDelegate, TReturn>` stays as-is. This is the non-void When chain terminal -- it was already named `ThenCall` and the rename only affects the void side.

---

## Pipeline Analysis

### Execute -> Call (MethodInterceptorRenderer -- shared by all 9 patterns)

All nine patterns route through `MethodInterceptorRenderer.RenderInterceptorClass()`:

| Pattern | Execute->Call? | Shared Renderer? |
|---------|----------------|-------------------|
| 1. Standalone | Yes | `FlatRenderer` -> `MethodInterceptorRenderer` |
| 2. Generic Standalone | Yes | `FlatRenderer` -> `MethodInterceptorRenderer` |
| 3. Standalone Class | Yes | `StandaloneClassRenderer` -> `MethodInterceptorRenderer` |
| 4. Generic Standalone Class | Yes | `StandaloneClassRenderer` -> `MethodInterceptorRenderer` |
| 5. Inline Interface | Yes | `InlineRenderer` -> `MethodInterceptorRenderer` |
| 6. Inline Class | Yes | `InlineRenderer` -> `MethodInterceptorRenderer` |
| 7. Inline Delegate | Yes | `InlineRenderer` -> `MethodInterceptorRenderer` |
| 8. Open Generic Interface | Yes | `InlineRenderer` -> `MethodInterceptorRenderer` |
| 9. Open Generic Class | Yes | `InlineRenderer` -> `MethodInterceptorRenderer` |

A single change to `MethodInterceptorRenderer` propagates to all nine patterns.

### OnCall -> Returns/Call (Typed handler renderers -- SEPARATE pipeline)

Typed handlers for generic methods are rendered by `FlatRenderer.cs` (standalone patterns 1-4) and `InlineRenderer.cs` (inline/open generic patterns 5-9). These are **completely separate code paths** from `MethodInterceptorRenderer` and must be updated independently.

| Pattern | Has generic typed handlers? | Typed handler renderer |
|---------|-----------------------------|------------------------|
| 1. Standalone | Yes (if interface has generic methods) | `FlatRenderer.cs` |
| 2. Generic Standalone | Yes | `FlatRenderer.cs` |
| 3. Standalone Class | Yes (if base class has generic virtual methods) | `FlatRenderer.cs` (via StandaloneClassRenderer) |
| 4. Generic Standalone Class | Yes | `FlatRenderer.cs` (via StandaloneClassRenderer) |
| 5. Inline Interface | Yes (if interface has generic methods) | `InlineRenderer.cs` |
| 6. Inline Class | Yes (if class has generic virtual methods) | `InlineRenderer.cs` |
| 7. Inline Delegate | No (delegates don't have generic methods) | N/A |
| 8. Open Generic Interface | Yes | `InlineRenderer.cs` |
| 9. Open Generic Class | Yes | `InlineRenderer.cs` |

Both renderers must be updated. The `IsVoid` property is available on all handler models to determine the void/non-void split.

---

## Implementation Steps

### Phase 1: Interface Renames (src/KnockOff/)

**Returns -> Return (non-void):**

1. Rename `IMethodReturnsBuilder.cs` to `IMethodReturnBuilder.cs`
   - Rename `IMethodReturnsBuilder<TCallback>` -> `IMethodReturnBuilder<TCallback>`
   - Rename `IMethodReturnsBuilder<TCallback, TArg>` -> `IMethodReturnBuilder<TCallback, TArg>`
   - Rename `IMethodReturnsBuilderArgs<TCallback, TArgs>` -> `IMethodReturnBuilderArgs<TCallback, TArgs>`
   - Update all `ThenReturns` -> `ThenReturn` on those interfaces
   - Update XML doc comments: "Returns(callback)" -> "Return(callback)", "ThenReturns" -> "ThenReturn"

2. Rename `IMethodReturnsSequence.cs` to `IMethodReturnSequence.cs`
   - Rename `IMethodReturnsSequence` -> `IMethodReturnSequence`
   - Rename `IMethodReturnsSequence<TCallback>` -> `IMethodReturnSequence<TCallback>`
   - Update `ThenReturns` -> `ThenReturn`
   - Update XML doc comments

3. Update `IWhenTracking.cs` -- non-void side
   - Rename `IWhenBuilder<TDelegate, TReturn>.Returns(TReturn value)` -> `.Return(TReturn value)`
   - Update `IWhenBuilder` summary: "awaits Returns()" -> "awaits Return()"
   - Update `IWhenChain` summary: reference to `IWhenBuilder.Returns` -> `IWhenBuilder.Return`

**Execute -> Call (void):**

4. Rename `IMethodExecuteBuilder.cs` to `IMethodCallBuilder.cs`
   - Rename `IMethodExecuteBuilder<TCallback>` -> `IMethodCallBuilder<TCallback>`
   - Rename `IMethodExecuteBuilder<TCallback, TArg>` -> `IMethodCallBuilder<TCallback, TArg>`
   - Rename `IMethodExecuteBuilderArgs<TCallback, TArgs>` -> `IMethodCallBuilderArgs<TCallback, TArgs>`
   - Update all `ThenExecute` -> `ThenCall` on those interfaces
   - Update XML doc comments

5. Rename `IMethodExecuteSequence.cs` to `IMethodCallSequence.cs`
   - Rename `IMethodExecuteSequence` -> `IMethodCallSequence`
   - Rename `IMethodExecuteSequence<TCallback>` -> `IMethodCallSequence<TCallback>`
   - Update `ThenExecute` -> `ThenCall`
   - Update XML doc comments

6. Update `IWhenTracking.cs` -- void side
   - Rename `IVoidWhenChain.Execute(callback)` -> `IVoidWhenChain.Call(callback)`
   - Rename `IVoidWhenChain.ThenExecute(callback)` -> `IVoidWhenChain.ThenCall(callback)`
   - Re-add `#pragma warning disable CA1716` / `#pragma warning restore CA1716` around `Call()` (needed because `Call` triggers the CA1716 rule)
   - Update XML doc comments

**Checkpoint 1:** `dotnet build src/KnockOff/KnockOff.csproj` passes

### Phase 2: Generator Changes

**Phase 2a: Builder interface references (both Returns and Execute sides)**

1. `src/Generator/Builder/UnifiedInterceptorBuilder.cs` -- update `GetBuilderInterface()`:
   - `IMethodReturnsBuilder` -> `IMethodReturnBuilder`, `IMethodReturnsBuilderArgs` -> `IMethodReturnBuilderArgs`
   - `IMethodExecuteBuilder` -> `IMethodCallBuilder`, `IMethodExecuteBuilderArgs` -> `IMethodCallBuilderArgs`
2. `src/Generator/Renderer/Shared/ModelAdapters.cs` -- same changes in `GetBuilderInterface()`

**Phase 2b: MethodInterceptorRenderer.cs -- entry points (both sides)**

1. Change `model.IsVoid ? "Execute" : "Returns"` to `model.IsVoid ? "Call" : "Return"` (line ~170)
2. Change `overload.IsVoid ? "Execute" : "Returns"` to `overload.IsVoid ? "Call" : "Return"` (line ~485)
3. Change simplified void async `Execute(` to `Call(` (lines ~305, ~539)
4. Change non-void `Returns({valueStorageType} value)` to `Return(...)` (line ~208)
5. Change non-void `Returns({valueStorageType} first, params ...)` to `Return(...)` (line ~235)
6. Change non-void simplified async `Returns({simplifiedDelegateType} callback)` to `Return(...)` (lines ~275, ~516)
7. Update all internal calls from `Returns(` to `Return(` and `ThenReturns(` to `ThenReturn(` in generated code (e.g., lines ~242-263 where `var builder = Returns(...)` and `builder.ThenReturns(...)` are emitted)

**Phase 2c: MethodInterceptorRenderer.cs -- builder/sequence chaining (both sides)**

1. Change `isVoid ? "ThenExecute" : "ThenReturns"` to `isVoid ? "ThenCall" : "ThenReturn"` (lines ~1552, ~1720)
2. Update all `ThenReturns(` generated method names to `ThenReturn(` (value wrappers at ~1580-1591, ~1597, ~1615-1619, ~1625-1640, ~1765-1812)
3. Update explicit interface implementations: `IMethodReturnsSequence` -> `IMethodReturnSequence`, `ThenReturns` -> `ThenReturn` (lines ~1692, ~1723, ~1726, ~1864, ~1865)
4. Update explicit interface implementations: `IMethodExecuteSequence` -> `IMethodCallSequence`, `ThenExecute` -> `ThenCall` (lines ~1688, ~1722, ~1725, ~1859, ~1860)

**Phase 2d: MethodInterceptorRenderer.cs -- non-void When chain Returns -> Return**

1. Rename `Returns({innerType} value)` on WhenBuilder to `Return(...)` (line ~2001, async variant)
2. Rename `Returns({returnType} value)` on WhenBuilder to `Return(...)` (line ~2022, non-async variant)
3. Update explicit interface implementations `IWhenBuilder<...>.Returns(...)` to `IWhenBuilder<...>.Return(...)` (lines ~2015, ~2017, ~2031)

**Phase 2e: MethodInterceptorRenderer.cs -- void When chain**

1. Rename generated `Execute()` to `Call()` on `VoidWhenChainImpl` (line ~2505)
2. Rename `ThenExecute()` to `ThenCall()` (line ~2548)
3. Update explicit interface implementation `IVoidWhenChain.Execute` -> `IVoidWhenChain.Call` (line ~2513)

**Phase 2f: MethodInterceptorRenderer.cs -- internal matcher Execute -> Call**

1. Non-void `WhenMatcher` abstract base and implementations: rename `Execute()` method to `Call()` (lines ~1906, ~1928, ~1943, ~1954)
2. Void `VoidWhenMatcher` abstract base and implementations: rename `Execute()` method to `Call()` (lines ~2418, ~2435, ~2450, ~2461)
3. Matcher dispatch calls: `matcher.Execute(` -> `matcher.Call(` (lines ~1095, ~2384)
4. Rename local variable `executeParams` -> `callParams` (line ~1896)

**Phase 2g: Error messages and comments**

1. Error messages: "Configure via Returns or Execute" -> "Configure via Return or Call" (lines ~849, ~1045)
2. Update all comments listed in the Design section above (~35 comment edits across all three renames)

**Phase 2h: Typed handler renderers -- OnCall -> Return/Call**

This is a **separate pipeline** from MethodInterceptorRenderer. Changes in FlatRenderer.cs and InlineRenderer.cs.

1. `FlatRenderer.cs` single-signature typed handler (line ~1201):
   - Change `OnCall(` to `handler.IsVoid ? "Call" : "Return"` + `(`
   - Use `handler.IsVoid` to select method name
2. `FlatRenderer.cs` overload group typed handler (line ~1413):
   - Change `OnCall{sig.SignatureSuffix}(` to `(sig.IsVoid ? "Call" : "Return") + sig.SignatureSuffix + (`
   - Use `sig.IsVoid` to select method name
3. `FlatRenderer.cs` error messages:
   - Line ~1932: `"Set the handler's OnCall."` -> `"Set the handler's Return or Call."`
   - Line ~2244: `"Use {interceptorAccess}.OnCall(callback)."` -> conditional on `method.IsVoid`: `"Use {interceptorAccess}.Call(callback)."` or `"Use {interceptorAccess}.Return(callback)."`
4. `FlatRenderer.cs` comments (lines ~1199, ~1409, ~2141, ~2148, ~2178, ~2209): Update `OnCall` -> `Return/Call`
5. `InlineRenderer.cs` single-signature typed handler (line ~818):
   - Change `OnCall(` to `handler.IsVoid ? "Call" : "Return"` + `(`
   - Use `handler.IsVoid` to select method name
6. `InlineRenderer.cs` error message (line ~1080): `"Set the handler's OnCall."` -> `"Set the handler's Return or Call."`
7. `InlineRenderer.cs` comments (lines ~734, ~735, ~816, ~845, ~846): Update `OnCall` -> `Return/Call`

**Internal field/property names stay unchanged:**
- `_onCall` field -- private, not user-facing
- `_onCall{sig.SignatureSuffix}` fields -- private, not user-facing
- `Callback` property -- internal, not user-facing

**Checkpoint 2:** `dotnet build src/KnockOff.sln` passes

### Phase 3: Test Updates

Mechanical find-and-replace across all test files for ALL THREE renames:

**Returns -> Return transformation rules (largest surface area):**
- `.Returns(` on non-void method interceptors -> `.Return(`
- `.ThenReturns(` on non-void sequences -> `.ThenReturn(`
- `.When(...).Returns(value)` on non-void When chains -> `.When(...).Return(value)`
- Comments referencing "Returns" in the context of non-void callbacks -> update

**Execute -> Call transformation rules:**
- `.Execute(` on void method interceptors -> `.Call(`
- `.ThenExecute(` on void sequences/When chains -> `.ThenCall(`
- Comments referencing "Execute" in the context of void callbacks -> update

**OnCall -> Return/Call transformation rules (typed handlers):**
- `.Of<T>().OnCall(` on non-void generic methods -> `.Of<T>().Return(`
- `.Of<T>().OnCall(` on void generic methods -> `.Of<T>().Call(`
- `.OnCall{Suffix}(` on overload groups -> `.Return{Suffix}(` or `.Call{Suffix}(` based on void/non-void

**Returns -> Return files affected** (from grep audit -- largest scope):
- `src/Tests/KnockOffTests/` -- 42 files, 600 occurrences of `.Returns(`/`.ThenReturns(`
- `src/Tests/KnockOff.NeatooInterfaceTests/` -- 14 files, 51 occurrences
- Total: ~651 occurrences across ~56 files

**Execute -> Call files affected** (from grep audit):
- `src/Tests/KnockOffTests/` -- 32 files, 229 occurrences of `.Execute(`
- `src/Tests/KnockOff.NeatooInterfaceTests/` -- 4 files, 5 occurrences

**OnCall -> Return/Call files affected** (from grep audit):
- `src/Tests/KnockOffTests/` -- 3 files, 24 occurrences of `.OnCall(`
- `src/Tests/KnockOff.NeatooInterfaceTests/` -- 2 files, 4 occurrences
- Total: 55 `.OnCall(` occurrences across 9 files (including Documentation.Samples, handled in Phase 5)

**Important distinctions:**
- `.Execute(` that refers to calling the domain method `ServiceBase.Execute("command")` is NOT renamed
- `.Returns(` on Moq code examples (if any exist in test infrastructure) is NOT renamed -- only KnockOff API calls
- All typed handler `.OnCall(` in current tests are non-void, so they become `.Return(` (singular). If any void generic typed handler tests exist, those become `.Call(`

**Checkpoint 3:** `dotnet test src/KnockOff.sln` -- all tests pass (except Design projects, handled in Phase 4)

### Phase 4: Design Project Updates

All three renames apply.

**Design.Stubs -- Returns -> Return:**
- Update all `.Returns(` to `.Return(`, `.ThenReturns(` to `.ThenReturn(` across 29 files, ~445 occurrences

**Design.Stubs -- Execute -> Call:**
- Update all `.Execute(` (void callback API) to `.Call(`, `.ThenExecute(` to `.ThenCall(`

**Design.Stubs -- OnCall -> Return/Call:**
- Update all `.Of<T>().OnCall(` to `.Of<T>().Return(` (non-void) or `.Of<T>().Call(` (void)
- `src/Design/Design.Stubs/UserMethods/UserMethodBasics.cs` has 2 `.OnCall(` occurrences (both non-void -> `.Return(`)

**Critical distinction in Design.Stubs:**
- `stub.Execute.Execute(cmd => ...)` becomes `stub.Execute.Call(cmd => ...)` -- the first `Execute` is the interceptor property name (from `ServiceBase.Execute(string)` domain method), the second was the void callback API
- `stub.Execute_(string command)` user method override stays unchanged (domain method name)

**Design.Tests** -- same transformation rules for all three renames.

**Checkpoint 4:** `dotnet build src/Design/Design.Stubs` AND `dotnet test src/Design/Design.Tests` pass

### Phase 5: Documentation, Skills, Samples

**All three renames apply here:** Returns->Return, Execute->Call, AND OnCall->Return/Call.

**Skills -- Returns -> Return** (6 files, 259 occurrences):
- All 6 skill files reference `Returns`/`ThenReturns` extensively -- all become `Return`/`ThenReturn`

**Skills -- Execute -> Call** (~6 files):
- `skills/knockoff/SKILL.md` -- ~20+ lines referencing `Execute` in void callback context
- `skills/knockoff/references/api-reference.md` -- `Execute` method docs, `IMethodExecuteBuilder`, `IMethodExecuteSequence`
- `skills/knockoff/references/methods.md` -- void method `Execute` examples
- `skills/knockoff/references/moq-migration.md` -- migration table entries
- `skills/knockoff/references/patterns.md` -- pattern examples
- `skills/knockoff/references/properties.md` -- void method examples near property discussion

**Skills -- OnCall -> Returns/Call** (4 files, 25 occurrences):
- `skills/knockoff/SKILL.md` -- 4 occurrences
- `skills/knockoff/references/api-reference.md` -- 8 occurrences (typed handler API docs)
- `skills/knockoff/references/methods.md` -- 6 occurrences (generic method OnCall examples)
- `skills/knockoff/references/moq-migration.md` -- 7 occurrences

**Documentation guides -- Returns -> Return:**
- All ~20 active guide/reference/migration files reference `Returns`/`ThenReturns` -- all become `Return`/`ThenReturn`
- `IMethodReturnsBuilder`/`IMethodReturnsSequence` interface references in API docs -> `IMethodReturnBuilder`/`IMethodReturnSequence`

**Documentation guides -- Execute -> Call** (~20 files):
- `docs/guides/methods.md`, `docs/guides/async-patterns.md`, `docs/guides/delegates.md`, `docs/guides/verification.md`, `docs/guides/parameter-matching.md`, `docs/guides/api-consistency-matrix.md`, `docs/guides/user-methods.md`, `docs/guides/advanced-callbacks.md`, `docs/guides/source-delegation.md`, `docs/guides/stub-patterns.md`
- `docs/reference/interceptor-api.md`, `docs/reference/smart-defaults.md`
- `docs/getting-started.md`, `docs/troubleshooting.md`, `docs/comparison.md`, `docs/type-safety.md`
- `docs/migration/from-moq.md`, `docs/migration/from-nsubstitute.md`
- `README.md`

**Documentation guides -- OnCall -> Returns/Call** (key files):
- `docs/guides/generic-methods.md` -- 18 occurrences (primary typed handler docs, extensive rewrite needed: `OnCall` -> `Returns` throughout, section titles, explanatory text)
- `docs/guides/methods.md` -- 3 occurrences in comments
- `docs/guides/user-methods.md` -- 8 occurrences (OnCall supersedes user method)
- `docs/guides/api-consistency-matrix.md` -- 3 occurrences
- `docs/guides/delegates.md` -- 2 occurrences
- `docs/guides/parameter-matching.md` -- 1 occurrence
- `docs/reference/interceptor-api.md` -- 3 occurrences (typed handler examples)
- `docs/reference/smart-defaults.md` -- 2 occurrences
- `docs/migration/from-moq.md` -- 10 occurrences
- `docs/migration/from-nsubstitute.md` -- 7 occurrences

**Important note on doc OnCall references:** Many `OnCall` references in docs are within `<!-- snippet: -->` markers and will be auto-updated when the corresponding Documentation.Samples code is updated and `dotnet mdsnippets` is run. Some are in prose text outside snippets and must be manually edited. The `docs/guides/generic-methods.md` file has extensive prose about `OnCall` that needs manual updating (section titles like "OnCall Signature and Return Value", explanatory text about `OnCall`).

**MarkdownSnippet sample files -- Execute -> Call:**
- Update `.Execute(` to `.Call(` and `.ThenExecute(` to `.ThenCall(` in `src/Tests/KnockOff.Documentation.Samples/` files

**MarkdownSnippet sample files -- Returns -> Return:**
- ~468 occurrences of `.Returns(`/`.ThenReturns(` across 29 sample files -- all become `.Return(`/`.ThenReturn(`

**MarkdownSnippet sample files -- OnCall -> Return/Call:**
- `GenericMethodsSamples.cs` -- 18 occurrences of `.OnCall(` (all non-void -> `.Return(`)
- `InterceptorApiSamples.cs` -- 3 occurrences
- `MoqMigrationSamples.cs` -- 3 occurrences
- `SkillContentSamples.cs` -- 3 occurrences
- Plus many other files with `OnCall` in comments and test names (cosmetic from pre-v0.38.0 era, but should be updated for consistency)

**Run `dotnet mdsnippets` to sync after all sample file updates.**

**Checkpoint 5:** `dotnet mdsnippets` succeeds, spot-check docs

### Phase 6: Cleanup

1. Version bump: `0.38.0` -> `0.39.0` in `Directory.Build.props` (FileVersion, PackageVersion, PackageReleaseNotes)
2. Create release notes at `docs/release-notes/v0.39.0.md`
3. Update prior plan `docs/plans/unify-returns-execute-design.md` PackageReleaseNotes reference if applicable
4. Final: `dotnet test src/KnockOff.sln` -- all tests pass

---

## Acceptance Criteria

**Returns -> Return (non-void):**
- [ ] `IMethodReturnsBuilder` does not exist -- replaced by `IMethodReturnBuilder`
- [ ] `IMethodReturnsSequence` does not exist -- replaced by `IMethodReturnSequence`
- [ ] Non-void methods expose `.Return(callback)` and `.ThenReturn(callback)`, not `.Returns()`/`.ThenReturns()`
- [ ] Non-void value overloads expose `.Return(value)`, not `.Returns(value)`
- [ ] Non-void When chain uses `.When(...).Return(value)`, not `.When(...).Returns(value)`
- [ ] Generated non-void WhenBuilder class uses `Return()` method name, not `Returns()`
- [ ] Explicit interface implementations reference `IWhenBuilder<...>.Return(...)`, not `IWhenBuilder<...>.Returns(...)`

**Execute -> Call (void):**
- [ ] `IMethodExecuteBuilder` does not exist -- replaced by `IMethodCallBuilder`
- [ ] `IMethodExecuteSequence` does not exist -- replaced by `IMethodCallSequence`
- [ ] Void methods expose `.Call(callback)` and `.ThenCall(callback)`, not `.Execute()`/`.ThenExecute()`
- [ ] `IVoidWhenChain` uses `.Call()` and `.ThenCall()`, not `.Execute()`/`.ThenExecute()`
- [ ] Internal generated matcher classes use `.Call()` method, not `.Execute()`
- [ ] Error messages say "Configure via Return or Call", not "Returns or Execute"

**OnCall -> Return/Call (typed handlers):**
- [ ] Non-void generic typed handlers expose `.Of<T>().Return(callback)`, not `.OnCall()`
- [ ] Void generic typed handlers expose `.Of<T>().Call(callback)`, not `.OnCall()`
- [ ] Overload group typed handlers use `Return{Suffix}` / `Call{Suffix}`, not `OnCall{Suffix}`
- [ ] Typed handler error messages reference "Return or Call", not "OnCall"
- [ ] `OnCall` does not appear in any user-facing generated API

**Unchanged:**
- [ ] Non-void When chain `ThenCall` is unchanged (was already `ThenCall`)
- [ ] `StandaloneClassRenderer.cs` `Execute_()` forwarder is unchanged (domain method name)
- [ ] Internal `_onCall` field names, `OnCallDelegateType` model properties are unchanged (not user-facing)
- [ ] Internal `_returnsValue`, `_hasReturnsValue`, `_returnsValueTracking` generated field names are unchanged (private, not user-facing)

**Verification:**
- [ ] All nine patterns generate correct API (all three renames applied to both regular methods and generic typed handlers)
- [ ] All tests pass
- [ ] Design projects compile and tests pass
- [ ] Skills, docs, and samples updated (all three renames: `Returns`, `Execute`, and `OnCall` references)
- [ ] None of `Returns`, `ThenReturns`, `Execute`, `ThenExecute`, or `OnCall` appear in user-facing generated API
- [ ] Version bumped

---

## Dependencies

- Depends on v0.38.0 being complete (it is -- merged to main)
- No other dependencies

---

## Risks / Considerations

### Breaking Change

This renames ALL method API from v0.38.0. Any consumers who adopted `Returns`/`ThenReturns`, `Execute`/`ThenExecute`, or `OnCall` must update. Mitigation: pre-1.0 software, breaking changes expected.

### Returns -> Return is the Largest Surface Area

The Returns->Return rename touches the most code (~600 test occurrences, ~468 sample occurrences, ~445 Design occurrences, ~259 skill occurrences). Risk: mechanical find-and-replace may catch non-KnockOff `.Returns(` calls (e.g., if Moq code exists in any test infrastructure). Mitigation: all occurrences have been audited; the test projects use only KnockOff stubs, so `.Returns(` is unambiguous. Review diffs to confirm no false positives.

### IWhenBuilder.Return(value) is a Value Method, Not a Callback

Unlike `.Return(callback)` on method interceptors (which takes a delegate), `IWhenBuilder.Return(value)` takes a plain value. Both use the name `Return` after this rename. This is intentional (the user wants all plural `Returns` removed), but the dual meaning of `Return` (callback vs. value) may need a note in docs. Mitigation: the When chain `Return` always follows `.When(...)` so context makes it clear.

### CA1716 Suppression Re-added

`IVoidWhenChain.Call()` triggers CA1716 ("identifiers should not match keywords"). The `#pragma` suppression that was removed in v0.38.0 must be re-added. This is the same suppression that existed before v0.38.0.

### Confusion with Domain Method Named "Execute"

The `ServiceBase.Execute(string command)` domain method creates interceptor properties named `stub.Execute` in Design projects. After this rename, the user writes `stub.Execute.Call(cmd => ...)` -- the `.Execute` is the interceptor property (from the domain method), and `.Call()` is the void callback API. This is correct behavior but may look confusing. It is actually LESS confusing than the v0.38.0 version which had `stub.Execute.Execute(cmd => ...)`.

### Typed Handler Void/Non-Void Split

The typed handler rename (OnCall->Return/Call) introduces a behavioral split that didn't exist before. Previously, `OnCall` was used for both void and non-void generic typed handlers. Now the method name depends on `IsVoid`. Risk: if any handler model has an incorrect `IsVoid` value, the wrong method name will be generated. Mitigation: `IsVoid` is already used correctly throughout the pipeline for regular methods, and the same model properties are reused for typed handlers.

### Stale OnCall References in Documentation.Samples Comments

Many Documentation.Samples files have `OnCall` in comments and test method names from the pre-v0.38.0 era (e.g., `VoidMethod_ConfiguredWithOnCall`, `OnCall_SupersedesUserMethod`). While these don't affect compilation, they create confusion because the actual API calls will use `Return`/`Call` after this rename. This plan should update these cosmetic references for consistency, but it's low-risk if some are missed -- they don't affect functionality.

---

## Architectural Verification

### Scope Table

| Pattern | Returns->Return (non-void) | Execute->Call (void) | OnCall->Return/Call (typed handlers) | Properties? | Indexers? | Events? |
|---------|---------------------------|---------------------|--------------------------------------|-------------|-----------|---------|
| 1. Standalone | Yes (via MethodInterceptorRenderer) | Yes (via MethodInterceptorRenderer) | Yes if has generic methods (via FlatRenderer) | No | No | No |
| 2. Generic Standalone | Yes (via MethodInterceptorRenderer) | Yes (via MethodInterceptorRenderer) | Yes (via FlatRenderer) | No | No | No |
| 3. Standalone Class | Yes (via MethodInterceptorRenderer) | Yes (via MethodInterceptorRenderer) | Yes if has generic virtual methods (via FlatRenderer) | No | No | No |
| 4. Generic Standalone Class | Yes (via MethodInterceptorRenderer) | Yes (via MethodInterceptorRenderer) | Yes (via FlatRenderer) | No | No | No |
| 5. Inline Interface | Yes (via MethodInterceptorRenderer) | Yes (via MethodInterceptorRenderer) | Yes if has generic methods (via InlineRenderer) | No | No | No |
| 6. Inline Class | Yes (via MethodInterceptorRenderer) | Yes (via MethodInterceptorRenderer) | Yes if has generic virtual methods (via InlineRenderer) | No | No | No |
| 7. Inline Delegate | Yes (via MethodInterceptorRenderer) | Yes (via MethodInterceptorRenderer) | No (delegates have no generic methods) | No | No | No |
| 8. Open Generic Interface | Yes (via MethodInterceptorRenderer) | Yes (via MethodInterceptorRenderer) | Yes (via InlineRenderer) | No | No | No |
| 9. Open Generic Class | Yes (via MethodInterceptorRenderer) | Yes (via MethodInterceptorRenderer) | Yes (via InlineRenderer) | No | No | No |

### Pipeline Verification

**Returns -> Return AND Execute -> Call (MethodInterceptorRenderer):**
All nine patterns share `MethodInterceptorRenderer`. Verified by tracing:
- `FlatRenderer.cs` calls `MethodInterceptorRenderer.RenderInterceptorClass()`
- `StandaloneClassRenderer.cs` calls `MethodInterceptorRenderer.RenderInterceptorClass()`
- `InlineRenderer.cs` calls `MethodInterceptorRenderer.RenderInterceptorClass()`
- `ClassRenderer.cs` calls `MethodInterceptorRenderer.RenderInterceptorClass()`

A single change to `MethodInterceptorRenderer` propagates to all nine patterns. Both Returns->Return and Execute->Call are handled in this shared renderer.

**OnCall -> Return/Call (Typed handler renderers):**
Typed handlers are rendered by separate code in `FlatRenderer.cs` and `InlineRenderer.cs`. Verified by tracing:
- `FlatRenderer.cs` lines ~1150-1450 render standalone typed handlers (single-sig and overload groups)
- `InlineRenderer.cs` lines ~780-870 render inline typed handlers (single-sig only)
- Both emit `OnCall` as a string literal that must be split to `Return`/`Call` based on `IsVoid`
- `IsVoid` is available on `FlatGenericMethodHandlerModel`, `FlatGenericSignatureGroup`, and `InlineGenericMethodHandlerModel`

### Breaking Changes

**Yes.** Three breaking changes:
1. Non-void method API renames `Returns`->`Return`, `ThenReturns`->`ThenReturn` (interfaces, generated code, When chain value method)
2. Void method API renames `Execute`->`Call`, `ThenExecute`->`ThenCall`
3. Generic typed handler API renames `OnCall`->`Return` (non-void) / `Call` (void)

Pre-1.0 software. All three changes are in the same version bump (0.38.0 -> 0.39.0).

### Design Project Verification

Deferred to Phase 4. Design projects will be updated after generator changes and verified by compilation at Checkpoint 4. This is a mechanical rename in a shared renderer layer (same justification as unify-returns-execute-design plan).

### Codebase Deep-Dive (Files Examined)

**Generator files -- Returns -> Return:**
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` (~2800+ lines) -- all `Returns`/`ThenReturns` references audited. Found: entry point ternaries (lines ~170, ~485 -- both void and non-void sides), value overload `Returns(value)` (line ~208), params overload (line ~235), simplified async (lines ~275, ~516), sequence `ThenReturns` (lines ~1552, ~1580-1640, ~1720, ~1765-1812), explicit interface implementations `IMethodReturnsSequence.ThenReturns` (lines ~1692, ~1723, ~1726, ~1864, ~1865), WhenBuilder `Returns(value)` (lines ~1996-2001, ~2015, ~2017, ~2022, ~2031), error messages (lines ~849, ~1045), ~15 comments referencing `Returns`/`ThenReturns`
- `src/Generator/Builder/UnifiedInterceptorBuilder.cs` -- `GetBuilderInterface()` at lines ~286-293, 3 `IMethodReturnsBuilder` references
- `src/Generator/Renderer/Shared/ModelAdapters.cs` -- `GetBuilderInterface()` at lines ~217-224, 3 `IMethodReturnsBuilder` references
- Internal generated fields `_returnsValue`, `_hasReturnsValue`, `_returnsValueTracking` (18 references across MethodInterceptorRenderer.cs) -- all private fields, confirmed out of scope

**Interface files -- Returns -> Return:**
- `src/KnockOff/IMethodReturnsBuilder.cs` -- 3 builder interfaces (`IMethodReturnsBuilder<TCallback>`, `IMethodReturnsBuilder<TCallback, TArg>`, `IMethodReturnsBuilderArgs<TCallback, TArgs>`) with `ThenReturns` methods -- all to rename
- `src/KnockOff/IMethodReturnsSequence.cs` -- 2 interfaces (`IMethodReturnsSequence`, `IMethodReturnsSequence<TCallback>`) with `ThenReturns` -- all to rename
- `src/KnockOff/IWhenTracking.cs` line 62 -- `IWhenBuilder<TDelegate, TReturn>.Returns(TReturn value)` -- to rename to `.Return(value)`

**Audit results (grep "Returns"/"ThenReturns" in user-facing contexts):**
- `src/KnockOff/` -- 16 lines across 3 files (IMethodReturnsBuilder.cs, IMethodReturnsSequence.cs, IWhenTracking.cs)
- `src/Generator/` -- ~100+ lines across 3 files (MethodInterceptorRenderer.cs, UnifiedInterceptorBuilder.cs, ModelAdapters.cs)
- `src/Tests/KnockOffTests/` -- 42 files, 600 occurrences of `.Returns(`/`.ThenReturns(`
- `src/Tests/KnockOff.Documentation.Samples/` -- 29 files, 468 occurrences
- `src/Tests/KnockOff.NeatooInterfaceTests/` -- 14 files, 51 occurrences
- `src/Design/` -- 29 files, 445 occurrences
- `skills/knockoff/` -- 6 files, 259 occurrences

**Generator files -- Execute -> Call:**
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` (~2800+ lines) -- all `Execute` references audited. Found: entry point names (lines ~170, ~305, ~485, ~539), builder/sequence ThenExecute (lines ~1552, ~1688, ~1720, ~1722, ~1725, ~1859, ~1860), void When chain Execute/ThenExecute (lines ~2484, ~2505, ~2513, ~2548), internal matchers (lines ~1895-1957 non-void, ~2398-2465 void), matcher dispatch (lines ~1095, ~2384), error messages (lines ~849, ~1045), ~25 comments
- `src/Generator/Builder/UnifiedInterceptorBuilder.cs` -- `GetBuilderInterface()` at lines ~267-281, 3 `IMethodExecuteBuilder` references
- `src/Generator/Renderer/Shared/ModelAdapters.cs` -- `GetBuilderInterface()` at lines ~200-212, 3 `IMethodExecuteBuilder` references
- `src/Generator/Renderer/StandaloneClassRenderer.cs` -- lines ~157, ~538 mention `Execute_()` -- confirmed OUT OF SCOPE (user method forwarder, domain method name)

**Generator files -- OnCall -> Returns/Call (typed handlers):**
- `src/Generator/Renderer/FlatRenderer.cs` -- 10 `OnCall` references audited: single-sig method (line ~1201), overload method (line ~1413), error messages (lines ~1932, ~2244), comments (lines ~1199, ~1409, ~2141, ~2148, ~2178, ~2209). Internal `_onCall` field (line ~1176) and `Callback` property (line ~1206) stay unchanged.
- `src/Generator/Renderer/InlineRenderer.cs` -- 7 `OnCall` references audited: single-sig method (line ~818), error message (line ~1080), comments (lines ~734, ~735, ~816, ~845, ~846). Internal `_onCall` field (line ~793) stays unchanged.
- `src/Generator/Model/Flat/FlatGenericMethodHandlerModel.cs` -- `IsVoid` property confirmed at line ~22
- `src/Generator/Model/Flat/FlatGenericMethodHandlerGroup.cs` -- `FlatGenericTypeArityGroup.IsVoid` at line ~44, `FlatGenericSignatureGroup.IsVoid` at line ~65
- `src/Generator/Model/Inline/InlineGenericMethodHandlerModel.cs` -- `IsVoid` property confirmed at line ~19
- `src/Generator/Renderer/Shared/ModelAdapters.cs` -- `OnCallDelegateType` property access at lines ~67, ~80, ~154, ~349 -- internal model property NOT emitted to generated code, confirmed out of scope

**Interface files:**
- `src/KnockOff/IMethodExecuteBuilder.cs` -- 3 builder interfaces to rename
- `src/KnockOff/IMethodExecuteSequence.cs` -- 2 sequence interfaces to rename
- `src/KnockOff/IWhenTracking.cs` -- `IVoidWhenChain.Execute()` and `.ThenExecute()` to rename
- `src/KnockOff/IMethodReturnsBuilder.cs` -- confirmed unchanged
- `src/KnockOff/IMethodReturnsSequence.cs` -- confirmed unchanged
- `src/KnockOff/IMethodSequence.cs` -- confirmed unchanged (no Execute references)

**Design domain:**
- `src/Design/Design.Domain/Abstractions/ServiceBase.cs` line 46 -- `public abstract void Execute(string command)` confirms `Execute_()` user method is domain-derived
- `src/Design/Design.Domain/Abstractions/EventServiceBase.cs` line 27 -- same pattern
- `src/Design/Design.Domain/Services/IUserMethodService.cs` lines 36, 94 -- `Execute` and `ExecuteAsync` domain methods

**Audit results (grep "Execute"):**
- `src/KnockOff/` -- 26 lines across 3 files (all in scope)
- `src/Generator/` -- ~55 lines across 3 files (all in scope except StandaloneClassRenderer comments)
- `src/Design/` -- ~150+ lines across ~25 files (mix of API calls and domain method references)
- `src/Tests/` -- ~200+ lines across ~40+ files (mix of API calls and domain method references)
- `docs/` -- ~200+ lines across ~20+ files
- `skills/` -- ~100+ lines across ~6 files

**Audit results (grep "OnCall"):**
- `src/Generator/Renderer/FlatRenderer.cs` -- 10 occurrences (all in scope for typed handler rename)
- `src/Generator/Renderer/InlineRenderer.cs` -- 7 occurrences (all in scope for typed handler rename)
- `src/Generator/Renderer/Shared/ModelAdapters.cs` -- 4 `OnCallDelegateType` property accesses (out of scope -- internal model property)
- `src/Tests/KnockOffTests/` -- 24 occurrences across 3 files (GenericMethodTests, GenericMethodBugTests, InlineStubTests)
- `src/Tests/KnockOff.Documentation.Samples/` -- ~130+ occurrences across ~20 files (mix of typed handler API calls and cosmetic comments/names from pre-v0.38.0)
- `src/Tests/KnockOff.NeatooInterfaceTests/` -- 4 occurrences across 2 files
- `src/Design/Design.Stubs/` -- 2 occurrences in 1 file (UserMethodBasics.cs)
- `skills/knockoff/` -- 25 occurrences across 4 files
- `docs/` -- ~1900+ occurrences across ~150 files (vast majority in completed todos/plans and old release notes -- only active guides/references need updating)

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-02-07

### Why This Plan Is Exceptionally Clear

This plan is a mechanical rename with no new functionality, no behavioral changes, and no ambiguous edge cases. The architect performed an exhaustive line-by-line audit of every affected file, verified line references against the actual codebase, correctly identified three separate code pipelines (shared MethodInterceptorRenderer, FlatRenderer typed handlers, InlineRenderer typed handlers), and explicitly called out what is in scope vs out of scope. The plan correctly handles all of the following:

- All 9 patterns covered via shared MethodInterceptorRenderer
- Typed handler void/non-void split using existing IsVoid model properties
- Domain method `ServiceBase.Execute()` correctly excluded
- Internal generated field names (`_returnsValue`, `_onCall`) correctly excluded
- `IWhenChain.ThenCall` correctly identified as already using the target name
- Generated code internal calls (`var builder = Returns(...)`, `builder.ThenReturns(...)`) identified for update
- CA1716 suppression need identified
- PackageReleaseNotes update included in cleanup

### Review Summary

- Files examined: 12 source files (6 interface files, 3 generator renderer files, 2 builder/adapter files, 3 model files)
- Questions checked: 16 of 16
- Devil's advocate items: 7 generated across 3 categories, all already addressed or acknowledged in the plan
- Grep verification: `.Returns(` (543 occurrences/42 files), `.Execute(` (229/32), `.OnCall(` (3 files in KnockOffTests, 2 in NeatooInterfaceTests) -- all match plan claims

### Codebase Investigation Details

**Generator verification (MethodInterceptorRenderer.cs):**
- Line 170: `model.IsVoid ? "Execute" : "Returns"` -- confirmed
- Line 208: `public MethodCallBuilderImpl Returns({valueStorageType} value)` -- confirmed
- Line 235: `public MethodSequenceImpl Returns({valueStorageType} first, params ...)` -- confirmed
- Lines 242-263: Internal generated `var builder = Returns(...)` and `builder.ThenReturns(...)` calls -- confirmed
- Line 485: `overload.IsVoid ? "Execute" : "Returns"` -- confirmed
- Lines 1552, 1720: `isVoid ? "ThenExecute" : "ThenReturns"` ternaries -- confirmed
- Lines 1688, 1692: Explicit interface impls for `IMethodExecuteSequence.ThenExecute` / `IMethodReturnsSequence.ThenReturns` -- confirmed
- Lines 1722-1726: Sequence interface references -- confirmed
- Lines 1859-1865: Sequence class explicit interface impls -- confirmed
- Lines 2001, 2022: WhenBuilder `Returns(value)` methods -- confirmed
- Lines 2015, 2017, 2031: Explicit interface `IWhenBuilder.Returns` -- confirmed
- Lines 2505, 2513: VoidWhenChain `Execute()` and explicit interface impl -- confirmed
- Line 2548: `ThenExecute()` -- confirmed
- Lines 1906, 1928, 1943, 1954: Non-void WhenMatcher `Execute()` methods -- confirmed
- Lines 2418, 2435, 2450, 2461: Void VoidWhenMatcher `Execute()` methods -- confirmed
- Lines 1095, 2384: Matcher dispatch `matcher.Execute()` calls -- confirmed
- Lines 849, 1045: Error messages "Configure via Returns or Execute" -- confirmed

**Typed handler verification:**
- FlatRenderer.cs line 1201: `OnCall(` on single-sig handler -- confirmed
- FlatRenderer.cs line 1413: `OnCall{sig.SignatureSuffix}(` on overload handler -- confirmed
- FlatRenderer.cs line 1932: Error message "Set the handler's OnCall" -- confirmed
- FlatRenderer.cs line 2244: Error message "Use {interceptorAccess}.OnCall(callback)" -- confirmed
- InlineRenderer.cs line 818: `OnCall(` on single-sig handler -- confirmed
- InlineRenderer.cs line 1080: Error message "Set the handler's OnCall" -- confirmed
- `IsVoid` available on: `FlatGenericMethodHandlerModel` (line 22), `FlatGenericTypeArityGroup` (line 44), `FlatGenericSignatureGroup` (line 65), `InlineGenericMethodHandlerModel` (line 19) -- all confirmed

**Interface verification:**
- `IMethodReturnsBuilder.cs`: 3 interfaces, `ThenReturns` methods -- confirmed
- `IMethodReturnsSequence.cs`: 2 interfaces, `ThenReturns` -- confirmed
- `IMethodExecuteBuilder.cs`: 3 interfaces, `ThenExecute` -- confirmed
- `IMethodExecuteSequence.cs`: 2 interfaces, `ThenExecute` -- confirmed
- `IWhenTracking.cs` line 62: `IWhenBuilder.Returns(TReturn value)` -- confirmed
- `IWhenTracking.cs` line 83: `IVoidWhenChain.Execute(TDelegate)` -- confirmed
- `IWhenTracking.cs` line 90: `IVoidWhenChain.ThenExecute(TDelegate)` -- confirmed
- `IWhenTracking.cs` line 34: `IWhenChain.ThenCall(TDelegate)` -- confirmed unchanged
- `IWhenTracking.cs` line 18: `<see cref="IWhenBuilder{TDelegate, TReturn}.Returns"/>` -- confirmed needs update
- `IWhenTracking.cs` line 51: `<see cref="Returns"/>` -- confirmed needs update

**Builder/adapter verification:**
- `UnifiedInterceptorBuilder.cs` lines 274, 278, 281, 286, 290, 293: `IMethodExecuteBuilder`/`IMethodReturnsBuilder` references -- confirmed
- `ModelAdapters.cs` lines 205, 209, 212, 217, 221, 224: Same references -- confirmed

**Design.Stubs verification:**
- Design project verification deferred to Phase 4 -- acceptable for a mechanical rename (no new functionality, no failing stubs needed)
- `ServiceBase.Execute(string command)` confirmed at Design.Domain line 46 -- domain method, out of scope

---

## Implementation Contract

**Created:** 2026-02-07
**Approved by:** knockoff-developer

### In Scope

**Phase 1: Interface Renames (src/KnockOff/)**
- [x] Rename `IMethodReturnsBuilder.cs` to `IMethodReturnBuilder.cs` (3 interfaces + ThenReturns -> ThenReturn + XML docs)
- [x] Rename `IMethodReturnsSequence.cs` to `IMethodReturnSequence.cs` (2 interfaces + ThenReturns -> ThenReturn + XML docs)
- [x] Rename `IMethodExecuteBuilder.cs` to `IMethodCallBuilder.cs` (3 interfaces + ThenExecute -> ThenCall + XML docs)
- [x] Rename `IMethodExecuteSequence.cs` to `IMethodCallSequence.cs` (2 interfaces + ThenExecute -> ThenCall + XML docs)
- [x] Update `IWhenTracking.cs`: `IWhenBuilder.Returns()` -> `.Return()`, `IVoidWhenChain.Execute()` -> `.Call()`, `IVoidWhenChain.ThenExecute()` -> `.ThenCall()`, XML doc see-crefs, add `#pragma warning disable/restore CA1716` around `Call()` AND `Return()`
- [x] **Checkpoint 1:** `dotnet build src/KnockOff/KnockOff.csproj` passes

**Phase 2: Generator Changes (src/Generator/)**
- [x] Phase 2a: Update `UnifiedInterceptorBuilder.cs` and `ModelAdapters.cs` builder interface references (IMethodReturnsBuilder -> IMethodReturnBuilder, IMethodExecuteBuilder -> IMethodCallBuilder)
- [x] Phase 2b: MethodInterceptorRenderer.cs entry points -- `IsVoid ? "Execute" : "Returns"` -> `"Call" : "Return"`, value overloads, simplified async, params method internal calls
- [x] Phase 2c: MethodInterceptorRenderer.cs builder/sequence chaining -- ThenReturns/ThenExecute ternaries, explicit interface impls for IMethodReturnSequence/IMethodCallSequence
- [x] Phase 2d: MethodInterceptorRenderer.cs non-void When chain -- WhenBuilder `Returns()` -> `Return()`, explicit interface `IWhenBuilder.Returns` -> `.Return`
- [x] Phase 2e: MethodInterceptorRenderer.cs void When chain -- VoidWhenChainImpl `Execute()` -> `Call()`, `ThenExecute()` -> `ThenCall()`, explicit interface impl
- [x] Phase 2f: MethodInterceptorRenderer.cs internal matchers -- WhenMatcher/VoidWhenMatcher `Execute()` -> `Call()`, matcher dispatch calls, `executeParams` -> `callParams`
- [x] Phase 2g: MethodInterceptorRenderer.cs error messages ("Configure via Returns or Execute" -> "Return or Call")
- [x] Phase 2h: MethodInterceptorRenderer.cs comments (~35 comment updates)
- [x] Phase 2i: FlatRenderer.cs typed handlers -- `OnCall` -> `handler.IsVoid ? "Call" : "Return"` (single-sig line ~1201, overload line ~1413), error messages (lines ~1932, ~2244), comments
- [x] Phase 2j: InlineRenderer.cs typed handlers -- `OnCall` -> `handler.IsVoid ? "Call" : "Return"` (line ~818), error message (line ~1080), comments
- [x] **Checkpoint 2:** `dotnet build src/KnockOff.sln` passes

**Phase 3: Test Updates**
- [x] Rename `.Returns(` -> `.Return(` and `.ThenReturns(` -> `.ThenReturn(` across all KnockOffTests files (~543+ occurrences)
- [x] Rename `.Execute(` -> `.Call(` and `.ThenExecute(` -> `.ThenCall(` across all KnockOffTests files (~229 occurrences), distinguishing domain `Execute` from API `Execute`
- [x] Rename `.OnCall(` -> `.Return(` (non-void) / `.Call(` (void) in GenericMethodTests, GenericMethodBugTests, InlineStubTests
- [x] Update KnockOff.NeatooInterfaceTests (same three transformations)
- [x] Update KnockOffTests.AssemblyStrict (`.Returns(` -> `.Return(`)
- [x] Update KnockOffSandbox (`.Execute(` -> `.Call(` for void API methods)
- [x] **Checkpoint 3:** `dotnet test src/KnockOff.sln` passes (excluding Documentation.Samples)

**Phase 4: Design Project Updates**
- [x] Update Design.Stubs: `.Returns(` -> `.Return(`, `.ThenReturns(` -> `.ThenReturn(`, `.Execute(` -> `.Call(` (API only, not domain), `.OnCall(` -> `.Return(`/`.Call(`
- [x] Update Design.Tests: same transformations
- [x] **Checkpoint 4:** `dotnet build src/Design/Design.Stubs` AND `dotnet test src/Design/Design.Tests` pass

**Phase 5: Documentation, Skills, Samples**
- [ ] Update Documentation.Samples files (all three renames: Returns/Execute/OnCall)
- [ ] Update skills/knockoff/ files (all three renames)
- [ ] Update docs/guides/, docs/reference/, docs/migration/ (all three renames)
- [ ] Update README.md
- [ ] Run `dotnet mdsnippets`
- [ ] **Checkpoint 5:** `dotnet mdsnippets` succeeds

**Phase 6: Cleanup**
- [ ] Version bump: 0.38.0 -> 0.39.0 in Directory.Build.props (FileVersion, PackageVersion, PackageReleaseNotes)
- [ ] Create release notes at docs/release-notes/v0.39.0.md
- [ ] **Final Checkpoint:** `dotnet test src/KnockOff.sln` -- all tests pass

### Explicitly Out of Scope

- `StandaloneClassRenderer.cs` `Execute_()` forwarder -- domain method name, not API
- Internal generated field names: `_returnsValue`, `_hasReturnsValue`, `_returnsValueTracking`, `_onCall`, `_onCall{suffix}`, `Callback` properties
- Internal model property names: `OnCallDelegateType`, `OnCallArgs`, `OnCallArgumentList`
- `IWhenChain.ThenCall` -- already uses correct name
- Renaming completed plans/todos in docs/plans/completed/ or docs/todos/completed/ -- historical artifacts

### Verification Gates

1. **After Phase 1:** `dotnet build src/KnockOff/KnockOff.csproj` passes. No `Returns`, `ThenReturns`, `Execute`, `ThenExecute` in the 6 interface files (except XML doc prose where the word "returns" is used in its English sense, e.g., "Returns this for fluent chaining").
2. **After Phase 2:** `dotnet build src/KnockOff.sln` passes. Generator emits `Return`/`Call`/`ThenReturn`/`ThenCall` instead of `Returns`/`Execute`/`ThenReturns`/`ThenExecute`. Typed handlers emit `Return`/`Call` instead of `OnCall`.
3. **After Phase 3:** `dotnet test src/KnockOff.sln` passes (excluding Design project tests if Design not yet updated).
4. **After Phase 4:** `dotnet build src/Design/Design.Stubs` AND `dotnet test src/Design/Design.Tests` pass.
5. **After Phase 5:** `dotnet mdsnippets` succeeds.
6. **Final:** ALL tests pass across entire solution. None of `Returns(`, `ThenReturns(`, `Execute(` (API context), `ThenExecute(`, `OnCall(` appear in user-facing generated code or public interfaces.

### Stop Conditions

If any of these occur, STOP and report:
- Out-of-scope test fails unexpectedly
- Architectural contradiction discovered (e.g., a code path where IsVoid is incorrect)
- Generated code does not compile after Phase 2
- Domain method `Execute` references accidentally renamed
- CA1716 causes build failure that cannot be resolved with `#pragma` suppression

---

## Implementation Progress

**Started:** 2026-02-07
**Developer:** knockoff-developer

### Phase 1: Interface Renames -- COMPLETE

- Created `IMethodReturnBuilder.cs` (replacing IMethodReturnsBuilder.cs)
- Created `IMethodReturnSequence.cs` (replacing IMethodReturnsSequence.cs)
- Created `IMethodCallBuilder.cs` (replacing IMethodExecuteBuilder.cs)
- Created `IMethodCallSequence.cs` (replacing IMethodExecuteSequence.cs)
- Deleted old files: IMethodReturnsBuilder.cs, IMethodReturnsSequence.cs, IMethodExecuteBuilder.cs, IMethodExecuteSequence.cs
- Updated IWhenTracking.cs with all renames and CA1716 suppressions
- **Discovery:** CA1716 also triggered on `IWhenBuilder.Return(TReturn)` (not just `Call()`). Added `#pragma warning disable CA1716` around both methods.
- **Checkpoint 1:** PASS

### Phase 2: Generator Changes -- COMPLETE

- Phase 2a: Updated UnifiedInterceptorBuilder.cs and ModelAdapters.cs builder interface references
- Phase 2b-2h: Extensive edits to MethodInterceptorRenderer.cs (~35+ locations): entry points, value overloads, simplified async, builder/sequence chaining, When chains (both void and non-void), matchers, error messages, comments
- Phase 2i: FlatRenderer.cs typed handlers updated (single-sig and overload group) with `handler.IsVoid ? "Call" : "Return"` logic
- Phase 2j: InlineRenderer.cs typed handlers updated with same logic
- **Checkpoint 2:** PASS

### Phase 3: Test Updates -- COMPLETE

- Bulk renamed `.ThenReturns(` -> `.ThenReturn(`, `.ThenExecute(` -> `.ThenCall(`, `.Returns(` -> `.Return(`, `.Execute(` -> `.Call(` across all test files
- Bulk renamed `.OnCall(` -> `.Return(` across all test files
- Fixed domain method false positives:
  - `stub.Object.Call(` -> `stub.Object.Execute(` in StandaloneClassUserMethodTests.cs, InlineStubTests.cs, StandaloneClassStubTests.cs
  - `rule.Call(` -> `rule.Execute(` in InlineStubBugTests.cs, GenericInheritanceTypeMismatchBugTests.cs
  - `service.Call(` -> `service.Execute(` in UserMethodWhenTests.cs
  - `service.Execute("hello")` restored in StandaloneClassStubTests.cs helper method
- Fixed void typed handler false positives: `Process.Of<>().Return(` -> `.Call(` and `Transfer.Of<>().Return(` -> `.Call(` and `SaveEntity.Of<>().Return(` -> `.Call(` in GenericMethodTests.cs
- Additional projects missed by initial sed: KnockOffTests.AssemblyStrict (`.Returns(` -> `.Return(`) and KnockOffSandbox (`.Execute(` -> `.Call(` for void API methods)
- **Checkpoint 3:** PASS -- KnockOffTests: 1185/1185/1184 passed (net10/net9/net8), AssemblyStrict: 14/14/14 passed, NeatooInterfaceTests: 473/473/473 passed

### Phase 4: Design Project Updates -- COMPLETE

- Applied same bulk renames to Design.Stubs and Design.Tests (excluding obj/ and Generated/ directories)
- Fixed domain method false positive: `service.Call("test")` -> `service.Execute("test")` in UserMethodBasics.cs
- Renamed 2 remaining `.OnCall(` to `.Return(` in UserMethodBasics.cs (both on non-void generic methods)
- **Checkpoint 4:** PASS -- Design.Stubs: Build succeeded (0 warnings, 0 errors). Design.Tests: 259/259/259 passed (net10/net9/net8)

### Observations

- No out-of-scope test failures encountered
- No architectural contradictions discovered
- No stop conditions triggered

---

## Completion Evidence

### Test Results Summary

| Project | net8.0 | net9.0 | net10.0 |
|---------|--------|--------|---------|
| KnockOffTests | 1184 passed | 1185 passed | 1185 passed |
| KnockOffTests.AssemblyStrict | 14 passed | 14 passed | 14 passed |
| KnockOff.NeatooInterfaceTests | 473 passed | 473 passed | 473 passed |
| Design.Tests | 259 passed | 259 passed | 259 passed |
| **Total** | **1930** | **1931** | **1931** |

All tests pass with 0 failures across all target frameworks.

### Build Results

- `dotnet build src/KnockOff/KnockOff.csproj` -- PASS (Phase 1 checkpoint)
- `dotnet build src/KnockOff.sln` -- PASS (Phase 2 checkpoint, excluding Documentation.Samples which is Phase 5)
- `dotnet build src/Design/Design.Stubs` -- PASS (Phase 4 checkpoint)
- `dotnet test src/Design/Design.Tests` -- PASS (Phase 4 checkpoint)

### Documentation.Samples Status

Documentation.Samples has build errors because it still uses the old API names (`.Returns(`, `.Execute(`, `.OnCall(`). This is expected and explicitly deferred to Phase 5, which is handled by separate agents.

### Remaining "OnCall" References in Design.Tests Comments

Design.Tests files contain ~60+ "OnCall" references in comments and test method names (e.g., `OnCall_ConfiguresCallback`). These are cosmetic and do not affect compilation or test execution. They should be updated in Phase 5 or a follow-up pass.

### All Contract Items Confirmed Complete

- [x] Phase 1: Interface renames (4 new files, 4 deleted, 1 updated)
- [x] Phase 2: Generator changes (3 renderer files, 2 builder files)
- [x] Phase 3: Test updates (all 4 test projects)
- [x] Phase 4: Design project updates (Design.Stubs and Design.Tests)
