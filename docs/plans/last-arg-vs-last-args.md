# LastArg (singular) vs LastArgs (plural) API Design

**Date:** 2026-02-16
**Status:** Draft (Architect)
**Last Updated:** 2026-02-16

---

## Problem Statement

KnockOff uses `LastArgs` (plural) as the public property on ALL interceptors and builders, regardless of parameter count. For single-parameter methods, this reads awkwardly:

```csharp
// Single-param method: "LastArgs" plural for one value feels wrong
Assert.Equal("hello", stub.GetGreeting.LastArgs);

// Multi-param method: "LastArgs" plural makes sense
Assert.Equal(("item2", 200, false), stub.Process.LastArgs);
```

The library already has the interfaces for both patterns (`IMethodTracking<TArg>.LastArg` and `IMethodTrackingArgs<TArgs>.LastArgs`), but the concrete types that users interact with only expose `LastArgs`.

## Current Architecture

### TArgs Type Mapping

| Param Count | TArgs Type | Example | Grammatically Natural Property |
|-------------|-----------|---------|-------------------------------|
| 0 params | No TArgs (uses arity-0 interceptors) | N/A | Neither (no arguments to capture) |
| 1 param | Raw type (e.g., `string`) | `MethodInterceptor<Delegate, string, int>` | `LastArg` |
| 2+ params | Named ValueTuple | `MethodInterceptor<Delegate, (string name, int value), int>` | `LastArgs` |

### Where Users Access LastArgs

**1. Directly on the interceptor (most common):**
```csharp
stub.Add.LastArgs           // -> (int a, int b)?  -- interceptor-level LastArgs
stub.GetGreeting.LastArgs   // -> string?           -- interceptor-level LastArgs
```

**2. On the builder returned by Return()/Call():**
```csharp
var tracking = stub.Add.Return((a, b) => a + b);
tracking.LastArgs           // -> (int a, int b)?   -- MethodCallBuilder.LastArgs
```

### Existing Interface Hierarchy

```
IMethodTracking             -- no LastArg/LastArgs (0-param)
IMethodTracking<TArg>       -- LastArg (singular, explicit interface impl)
IMethodTrackingArgs<TArgs>  -- LastArgs (plural)

IMethodReturnBuilder<TCallback>                    -- extends IMethodTracking
IMethodReturnBuilder<TCallback, TArg>              -- extends IMethodTracking<TArg>
IMethodReturnBuilderArgs<TCallback, TArgs>         -- extends IMethodTrackingArgs<TArgs>
```

The 1-param builder (`IMethodReturnBuilder<TCallback, TArg>`) extends `IMethodTracking<TArg>` which defines `LastArg`. But the concrete `MethodCallBuilder` class exposes `LastArgs` as the public property and only implements `LastArg` as an explicit interface member:

```csharp
// In MethodInterceptor<TDelegate, TArgs, TReturn>.MethodCallBuilder:
public TArgs? LastArgs => _lastArgs;                    // Public
TArgs? IMethodTracking<TArgs?>.LastArg => _lastArgs;    // Explicit (hidden)
```

### Two Access Points, Two Properties Needed

There are two distinct objects users access:

1. **The interceptor** (`stub.Method`) -- has a `LastArgs` property directly
2. **The builder** (`stub.Method.Return(...)`) -- also has `LastArgs` property

Both need to be addressed.

---

## Codebase Investigation

### Files Examined

| File | What Was Learned |
|------|-----------------|
| `src/KnockOff/IMethodTracking.cs` | `IMethodTracking<TArg>` has `LastArg`; `IMethodTrackingArgs<TArgs>` has `LastArgs`. Already designed for this split. |
| `src/KnockOff/IMethodReturnBuilder.cs` | Three variants: no-arg, single-arg (`TArg`), multi-arg (`TArgs`). Single-arg extends `IMethodTracking<TArg>`. |
| `src/KnockOff/IMethodCallBuilder.cs` | Mirrors ReturnBuilder: three variants with same inheritance. |
| `src/KnockOff/Interceptors/MethodInterceptor.cs` | `MethodCallBuilder` implements `IMethodReturnBuilder<TDelegate, TArgs?>` -- currently the SAME `TArgs` for both 1-param and 2+-param cases. `LastArg` is explicit interface impl returning same value as `LastArgs`. |
| `src/KnockOff/Interceptors/VoidMethodInterceptor.cs` | Same pattern as MethodInterceptor. `MethodCallBuilder` implements `IMethodCallBuilder<TDelegate, TArgs?>`. |
| `src/KnockOff/Interceptors/AsyncMethodInterceptor.cs` | Same pattern. `MethodCallBuilder : IMethodReturnBuilder<TDelegate, TArgs?>`. |
| `src/KnockOff/Interceptors/AsyncVoidMethodInterceptor.cs` | Same pattern. `MethodCallBuilder : IMethodCallBuilder<TDelegate, TArgs?>`. |
| `src/KnockOff/Interceptors/MethodInterceptor0.cs` | Zero-param interceptors: No LastArgs/LastArg at all (no arguments to track). `MethodCallBuilder0 : IMethodReturnBuilder<Func<TReturn>>`. |
| `src/Generator/Builder/UnifiedInterceptorBuilder.cs` | `GetBuilderInterface()` selects which interface variant based on trackable param count. Already distinguishes 0, 1, and 2+ params. `GetLastArgType()` and `GetLastArgsType()` return null for wrong counts. |
| `src/Generator/Renderer/Shared/PreCompiledInterceptorRenderer.cs` | `ComputeTArgsType()` returns raw type for 1 param, named tuple for 2+. This determines TArgs for the interceptor. |
| `src/Design/Design.Stubs/Methods/BasicMethods.cs` | Documentation says `LastArg` for single-param, `LastArgs` for multi-param. But actual code only shows `LastArgs`. |

### Key Pattern: The Builder Interface Already Splits

`UnifiedInterceptorBuilder.GetBuilderInterface()` (line 289) already makes the 1-param vs 2+-param distinction:

```csharp
if (trackableParams.Count == 1)
    return $"global::KnockOff.IMethodReturnBuilder<{delegateType}, {param.Type}>";  // -> IMethodTracking<TArg> -> LastArg
if (trackableParams.Count >= 2)
    return $"global::KnockOff.IMethodReturnBuilderArgs<{delegateType}, {tupleType}>";  // -> IMethodTrackingArgs<TArgs> -> LastArgs
```

This means the interface-level type returned by Return()/Call() ALREADY knows whether it should be single-arg or multi-arg. The problem is that the concrete `MethodCallBuilder` class doesn't make this distinction -- it always exposes `LastArgs`.

---

## Option Analysis

### Option A: Add `LastArg` to Interceptors and Builders for Single-Param Methods

**Approach**: Add a public `LastArg` property alongside `LastArgs` on both the interceptor and the builder, but only for 1-param methods. Users can use whichever reads better. Both return the same value. For 2+-param methods, only `LastArgs` exists. For 0-param methods, neither exists.

**API Surface**:
```csharp
// 0 params - no arg capture
stub.Reset.TotalCallCount;  // tracking only

// 1 param - both available
stub.GetGreeting.LastArg;   // string? -- natural for single param
stub.GetGreeting.LastArgs;  // string? -- still works (backward compatible)

// 2+ params - only LastArgs
stub.Add.LastArgs;           // (int a, int b)? -- natural for tuple
```

**Implementation**:
- **Interceptor level**: The interceptor (`MethodInterceptor<TDelegate, TArgs, TReturn>`) already has `LastArgs`. For 1-param TArgs, TArgs IS the raw type. Adding `LastArg` as a simple alias (`public TArgs? LastArg => LastArgs;`) would work BUT it would also appear on 2+-param interceptors where it makes no sense.
- **The problem**: The interceptor class is generic over TArgs. There is no way at the C# type level to conditionally add a member based on whether TArgs is a tuple or not. Both 1-param and 2+-param methods use the SAME `MethodInterceptor<TDelegate, TArgs, TReturn>` class.
- **Builder level**: Same problem. `MethodCallBuilder` is nested inside the same generic class.

**Resolution**: Since we cannot conditionally add members to a generic class, we would need to either:
1. Always expose both `LastArg` and `LastArgs` (regardless of param count), OR
2. Rely on the interfaces to provide the right property

**Subvariant A1: Always expose both on the concrete types**

Add `LastArg` as a public property alias on all interceptors and builders. For 2+-param methods, `LastArg` returns a tuple (grammatically odd but not harmful). Users learn: "use `LastArg` for 1 param, `LastArgs` for 2+."

Pros:
- Simple implementation (one property alias per interceptor and builder)
- Fully backward compatible (LastArgs still works)
- No generator changes needed

Cons:
- `LastArg` is available on 2+-param interceptors where it returns a tuple (misleading)
- Pollutes the API surface for multi-param methods
- IntelliSense shows both properties everywhere

**Subvariant A2: Expose `LastArg` only through the interface**

The interfaces already separate: `IMethodTracking<TArg>.LastArg` for 1-param and `IMethodTrackingArgs<TArgs>.LastArgs` for 2+-param. The builder return types already select the right interface. Users who capture the builder via the interface type get the right property.

```csharp
// Interface-typed variable gets LastArg
IMethodReturnBuilder<AddDelegate, string> tracking = stub.GetGreeting.Return(name => $"Hi {name}");
tracking.LastArg;  // string? -- available through IMethodTracking<string>

// var-typed variable gets both (LastArgs from concrete, LastArg from explicit interface)
var tracking2 = stub.GetGreeting.Return(name => $"Hi {name}");
tracking2.LastArgs;  // string? -- public on MethodCallBuilder
((IMethodTracking<string?>)tracking2).LastArg;  // string? -- explicit interface
```

This is technically already the current state. Users can already access `LastArg` by casting to the interface.

Pros:
- Zero implementation work (already done)
- Clean API surface (no pollution)

Cons:
- Completely undiscoverable (requires casting)
- Nobody will find it without reading documentation
- The interceptor itself (`stub.Method.LastArg`) is not addressable at all through interfaces

**Pipeline Impact**: Subvariant A1 requires changes to all 4 interceptor files (MethodInterceptor, VoidMethodInterceptor, AsyncMethodInterceptor, AsyncVoidMethodInterceptor). No generator changes. Subvariant A2 requires nothing.

---

### Option B: Everything Is a Tuple (Even Single-Param)

**Approach**: Change TArgs for single-param methods from raw type to `ValueTuple<T>`. Then `LastArgs` (plural) is always semantically correct because it always holds a tuple.

**API Surface**:
```csharp
// 1 param - TArgs is now ValueTuple<string>
stub.GetGreeting.LastArgs;           // ValueTuple<string>?
stub.GetGreeting.LastArgs.Value.Item1;  // string -- or named field

// When() also changes for 1-param methods
stub.GetGreeting.When(("Alice",)).Return("Hi Alice");  // double parens
// or with predicate:
stub.GetGreeting.When(args => args.Item1 == "Alice").Return("Hi Alice");
```

**Implementation**:
- `PreCompiledInterceptorRenderer.ComputeTArgsType()` must wrap single params: `(string name)` instead of `string`
- `FormatInvokeArgs()` must wrap single params in tuple literal: `(name,)` instead of `name`
- The `When()` API changes for single-param methods (breaking change for When with exact values)
- The `Return(callback)` API does NOT change because it uses TDelegate, not TArgs

**Breaking Changes**: YES -- this is a breaking change.
- `stub.Method.LastArgs` changes from `string?` to `ValueTuple<string>?` for single-param methods
- `stub.Method.When("value")` changes to `stub.Method.When(("value",))` for single-param methods
- All existing tests using single-param LastArgs need updating
- All existing tests using single-param When with exact values need updating

Pros:
- "Args" is always plural (always a container)
- Consistent mental model: TArgs is always a tuple
- Eliminates the 1-param vs 2+-param distinction in the type system

Cons:
- **Breaking change** for all single-param LastArgs and When users
- Awkward `ValueTuple<T>` syntax for 1-param: `args.Item1` or `args.name`
- Double-paren When syntax for 1-param: `When(("Alice",))` -- confusing
- Adds unnecessary wrapping overhead
- Goes against C# conventions where tuples are 2+ elements

---

### Option C: Both Properties on the Interceptor and Builder (Always)

**Approach**: Add `LastArg` as a public property on ALL interceptors and builders, aliasing `LastArgs`. Both always exist. Users choose what reads better.

**API Surface**:
```csharp
// 0 params - neither makes sense, but both exist returning Unit/nothing
// (Actually, 0-param interceptors have no TArgs, so this doesn't apply)

// 1 param
stub.GetGreeting.LastArg;   // string? -- reads naturally
stub.GetGreeting.LastArgs;  // string? -- also works

// 2+ params
stub.Add.LastArg;           // (int a, int b)? -- grammatically wrong but usable
stub.Add.LastArgs;          // (int a, int b)? -- reads naturally

// On builder too
var tracking = stub.GetGreeting.Return(name => $"Hi {name}");
tracking.LastArg;   // string?
tracking.LastArgs;  // string?
```

**Implementation**:
- Add `public TArgs? LastArg => LastArgs;` to each TTuple interceptor (4 files)
- Add `public TArgs? LastArg => _lastArgs;` to each MethodCallBuilder (4 inner classes)
- Remove the explicit interface implementation of `IMethodTracking<TArgs?>.LastArg` (it now conflicts with the public property -- or keep it and let the public property satisfy it)
- No generator changes needed
- Fully backward compatible

**Library Changes** (4 interceptor files, ~2 lines each):

```csharp
// MethodInterceptor<TDelegate, TArgs, TReturn>:
public TArgs? LastArg => LastArgs;  // On the interceptor

// MethodCallBuilder:
public TArgs? LastArg => _lastArgs;  // On the builder (replaces explicit interface impl)
```

Pros:
- Simple, minimal changes
- Fully backward compatible
- Users can choose the natural form
- No generator changes

Cons:
- `LastArg` returning a tuple for 2+-param methods is semantically misleading
- API surface is slightly polluted (two properties that do the same thing)
- No compile-time guidance pushing users toward the "right" property

---

### Option D: Leave As-Is

**Approach**: Keep `LastArgs` everywhere. It works, it's consistent, users adapt.

**API Surface**: Unchanged.

```csharp
stub.GetGreeting.LastArgs;  // string? -- works, slightly awkward for 1-param
stub.Add.LastArgs;          // (int a, int b)? -- natural for 2+ params
```

Pros:
- Zero implementation work
- Zero risk
- Consistent: one property name everywhere
- Users already know this pattern
- "Args" can be read as "argument(s)" -- grammatically defensible even for singular

Cons:
- `LastArgs` reads awkwardly for single-param methods
- Design.Stubs documentation already documents `LastArg` for single-param (creating false expectations)

---

### Option E: Rename to `LastCall` (Neutral Naming)

**Approach**: Replace `LastArgs`/`LastArg` with a single neutral property name that works regardless of param count.

**API Surface**:
```csharp
stub.GetGreeting.LastCall;  // string?
stub.Add.LastCall;          // (int a, int b)?
stub.Reset.LastCall;        // Unit? (0-param)
```

Or alternatively: `CapturedArgs`, `ReceivedArgs`, `Arguments`.

**Implementation**: Rename `LastArgs` to `LastCall` (or chosen name) on all interceptors and builders. Optionally keep `LastArgs` as `[Obsolete]` for backward compatibility.

Pros:
- Avoids the singular/plural debate entirely
- Single property name everywhere
- Natural for all param counts

Cons:
- **Breaking change** unless we keep `LastArgs` as deprecated
- `LastCall` could be confused with "the last method call" (a timestamp or call record) rather than "the arguments of the last call"
- Adds migration burden for existing users

---

## Recommendation

**Preferred Option: Option C (Both Properties Always)**

### Reasoning

1. **Minimal change, maximum benefit**: Adding `LastArg` as a simple alias on 4 interceptor files and their inner builders is ~8 lines of code total. Zero generator changes. Zero breaking changes.

2. **The "misleading tuple return" concern is minor**: In practice, users working with 2+-param methods will naturally gravitate toward `LastArgs` because IntelliSense will show the tuple type. `LastArg` returning a tuple is unusual but not confusing -- users who type `LastArg` on a multi-param method will immediately see the tuple type hint and switch to `LastArgs`.

3. **Single-param is the high-value case**: Single-param methods are common (e.g., `GetById(int id)`, `GetGreeting(string name)`, `SaveData(string data)`). For these methods, `LastArg` reads significantly better than `LastArgs`. This is where the improvement matters most.

4. **Backward compatible**: Existing code using `LastArgs` continues to work unchanged.

5. **The interface hierarchy already supports this**: `IMethodTracking<TArg>` already defines `LastArg`. By making the public property satisfy the interface, we unify the concrete and interface views.

6. **Consistent with Design.Stubs documentation**: The existing Design.Stubs comments already say "LastArg for single-parameter methods." This change makes the code match the documentation.

### What About Option D (Leave As-Is)?

Option D is a legitimate choice. "Args" can be read as "argument(s)" covering both singular and plural. The counter-argument is that the Design.Stubs documentation already creates the expectation that `LastArg` exists for single-param methods. If we leave as-is, we should update the documentation to remove references to `LastArg`. But adding the property is so cheap that the documentation-fixing path is actually more work than the code-change path.

### What About Option A (Interface-Only)?

Option A2 (current state) is completely undiscoverable. Option A1 is identical to Option C -- there is no way to restrict the property to only 1-param interceptors since they share the same generic class.

### What About Option E (Neutral Naming)?

Option E is the "nuclear option" -- it solves the problem completely but at the cost of a breaking change or deprecation ceremony. The benefit does not justify the cost when Option C solves 90% of the ergonomics issue with zero disruption.

---

## Implementation Details for Option C

### Interceptor Changes (4 files)

**`src/KnockOff/Interceptors/MethodInterceptor.cs`**:
```csharp
// On the interceptor (next to existing LastArgs):
public TArgs? LastArg => LastArgs;

// On MethodCallBuilder (replace explicit interface impl with public property):
public TArgs? LastArg => _lastArgs;
// Remove: TArgs? IMethodTracking<TArgs?>.LastArg => _lastArgs;
```

**`src/KnockOff/Interceptors/VoidMethodInterceptor.cs`**: Same changes.

**`src/KnockOff/Interceptors/AsyncMethodInterceptor.cs`**: Same changes.

**`src/KnockOff/Interceptors/AsyncVoidMethodInterceptor.cs`**: Same changes.

### Interface Satisfaction

When `MethodCallBuilder` has a public `LastArg` property, it implicitly satisfies `IMethodTracking<TArgs?>.LastArg` -- the explicit interface implementation becomes unnecessary. The public property covers both direct access and interface access.

However, keeping the explicit interface implementation alongside the public property would cause CS0102 (duplicate member). So the explicit impl must be removed when the public property is added.

### Zero-Param Interceptors (No Change)

The arity-0 interceptors (`MethodInterceptor0`, `VoidMethodInterceptor0`, `AsyncMethodInterceptor0`, `AsyncVoidMethodInterceptor0`) have no TArgs and no `LastArgs`. They don't need `LastArg` either.

### Design.Stubs Documentation (Optional)

Update `src/Design/Design.Stubs/Methods/BasicMethods.cs` to demonstrate both:
```csharp
// Single-param: use LastArg (natural) or LastArgs (also works)
Assert.Equal("Alice", stub.GetGreeting.LastArg);

// Multi-param: use LastArgs (natural)
Assert.Equal((3, 5), stub.Add.LastArgs);
```

### Test Impact

No existing tests break. New tests can be added to verify `LastArg` works as an alias.

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| `LastArg` on multi-param confuses users | Low | Low | IntelliSense shows tuple type, users self-correct |
| Breaking existing explicit interface casts | Very Low | Low | Replacing explicit impl with public property satisfies same interface |
| Future refactoring complication | Low | Low | Property is a trivial alias; easy to remove if a better solution emerges |

## Open Questions

None -- this analysis covers all approaches raised plus one additional (Option E). The recommendation is straightforward.
