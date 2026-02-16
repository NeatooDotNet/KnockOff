# TSyncDelegate Type Parameter for Async Interceptors

**Date:** 2026-02-15
**Related Todo:** [TTuple Interceptors](../todos/ttuple-interceptors.md)
**Status:** Complete (Verified 2026-02-15)
**Last Updated:** 2026-02-15

---

## Overview

Add a `TSyncDelegate` type parameter to `AsyncMethodInterceptor<TDelegate, TArgs, TReturn>` and `AsyncVoidMethodInterceptor<TDelegate, TArgs>` to restore natural parameter names on simplified sync callbacks. Currently, the simplified sync callback overload uses `Func<TArgs, TReturn>` (or `Action<TArgs>`), which for 2+ parameter methods forces users into tuple syntax like `((string input, CancellationToken ct) args) => args.input`. With TSyncDelegate, users write `(input, ct) => input` -- natural parameter names, just like the sync interceptor's TDelegate.

---

## Problem Statement

The TTuple migration (v0.51.0) collapsed arity-based interceptor types into generic TTuple types, restoring named parameters on async callbacks via TDelegate. However, TDelegate for async methods returns `Task<TReturn>` (or `Task`), so the **simplified sync callback** -- which auto-wraps in `Task.FromResult` -- cannot use TDelegate. It falls back to `Func<TArgs, TReturn>`, which for 2+ params means `Func<(string input, CancellationToken ct), string>`.

### Before (v0.50.0 arity types)

```csharp
// 1-param: natural names (no issue)
stub.FetchAsync.Return((id) => $"Fetch-{id}");

// 2-param: natural names via MethodInterceptor2's Func<T1, T2, TReturn> overload
stub.TransformAsync.Return((input, ct) => $"[{input}:ct]");
```

### After (v0.51.0 TTuple types -- current state)

```csharp
// 1-param: still natural (TArgs is raw type)
stub.FetchAsync.Return((id) => $"Fetch-{id}");

// 2-param simplified: forced tuple syntax because Return(Func<TArgs, TReturn>) where TArgs is ValueTuple
stub.TransformAsync.Return(((string input, CancellationToken ct) args) => $"[{args.input}:ct]");

// 2-param full async delegate: natural names (TDelegate has named params)
stub.TransformAsync.Return((string input, CancellationToken ct) => Task.FromResult($"[{input}:ct]"));
```

### Goal (with TSyncDelegate)

```csharp
// 2-param simplified: natural names via TSyncDelegate
stub.TransformAsync.Return((input, ct) => $"[{input}:ct]");

// Full async delegate still works identically
stub.TransformAsync.Return((string input, CancellationToken ct) => Task.FromResult($"[{input}:ct]"));
```

---

## Solution Design

### Core Idea

For every async method with 1+ parameters, the generator currently emits one delegate type (the "async delegate") that returns `Task<TReturn>` or `Task`. We add a **second delegate type** (the "sync delegate") that returns the inner type (`TReturn`) or `void`. This sync delegate becomes a new type parameter `TSyncDelegate` on the async interceptor types, replacing the `Func<TArgs, TReturn>` / `Action<TArgs>` simplified callback overloads.

### Type Parameter Changes

**AsyncMethodInterceptor:**
```
BEFORE: AsyncMethodInterceptor<TDelegate, TArgs, TReturn>
AFTER:  AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>
```

**AsyncVoidMethodInterceptor:**
```
BEFORE: AsyncVoidMethodInterceptor<TDelegate, TArgs>
AFTER:  AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs>
```

### API Changes on AsyncMethodInterceptor

```csharp
// BEFORE (current):
public MethodCallBuilder Return(TDelegate asyncCallback);           // full async
public MethodCallBuilder Return(Func<TArgs, TReturn> callback);    // simplified sync (TUPLE SYNTAX PROBLEM)
public MethodCallBuilder Return(TReturn value);                     // value

// AFTER (proposed):
public MethodCallBuilder Return(TDelegate asyncCallback);           // full async (unchanged)
public MethodCallBuilder Return(TSyncDelegate syncCallback);        // simplified sync (NATURAL PARAMS)
public MethodCallBuilder Return(TReturn value);                     // value (unchanged)
```

The `Func<TArgs, TReturn>` overloads throughout the type are replaced with `TSyncDelegate` equivalents that use the same expression tree bridging pattern as TDelegate.

### API Changes on AsyncVoidMethodInterceptor

```csharp
// BEFORE (current):
public MethodCallBuilder Call(TDelegate asyncCallback);   // full async
public MethodCallBuilder Call(Action<TArgs> callback);    // simplified sync (TUPLE SYNTAX PROBLEM)

// AFTER (proposed):
public MethodCallBuilder Call(TDelegate asyncCallback);   // full async (unchanged)
public MethodCallBuilder Call(TSyncDelegate syncCallback); // simplified sync (NATURAL PARAMS)
```

---

## Developer Concern Resolutions

### Concern 1 (FUNDAMENTAL): `ThenReturn(TReturn value)` call chain broken -- RESOLVED

**Problem:** `MethodCallBuilder.ThenReturn(TReturn value)` (line 445) internally calls `ThenReturn((_) => value)`. The lambda `(_) => value` is `Func<TArgs, TReturn>`, which resolves to the `ThenReturn(Func<TArgs, TReturn>)` overload. When that overload is replaced with `ThenReturn(TSyncDelegate)`, the lambda cannot implicitly convert to TSyncDelegate (a concrete generated delegate type). Same issue in `MethodSequence.ThenReturn(TReturn value)` (line 545).

**Resolution: Add `s_syncValueDelegate` static field, following the sync `MethodInterceptor` pattern.**

The sync `MethodInterceptor<TDelegate, TArgs, TReturn>` already solves this exact problem at line 24:
```csharp
private static readonly Func<TReturn, TDelegate> s_valueDelegate
    = DelegateInvokerFactory.BuildValueDelegate<TDelegate, TReturn>();
```

We add the equivalent for TSyncDelegate:
```csharp
private static readonly Func<TReturn, TSyncDelegate> s_syncValueDelegate
    = DelegateInvokerFactory.BuildValueDelegate<TSyncDelegate, TReturn>();
```

Then `ThenReturn(TReturn value)` routes through the TSyncDelegate overload:
```csharp
// MethodCallBuilder:
public MethodSequence ThenReturn(TReturn value) => ThenReturn(s_syncValueDelegate(value));

// MethodSequence:
public MethodSequence ThenReturn(TReturn value) => ThenReturn(s_syncValueDelegate(value));
```

`s_syncValueDelegate(value)` builds a TSyncDelegate that ignores all parameters and returns the fixed value, exactly like the sync interceptor's `s_valueDelegate`. The existing `DelegateInvokerFactory.BuildValueDelegate<TDelegate, TReturn>()` is fully generic and already handles this -- no factory changes needed.

### Concern 2 (FUNDAMENTAL): Same pattern in AsyncVoidMethodInterceptor -- CONFIRMED NO ISSUE

**Problem:** Developer asked whether AsyncVoidMethodInterceptor has internal call chains that pass a lambda to the `Action<TArgs>` overload.

**Resolution:** Investigated all methods in `AsyncVoidMethodInterceptor.cs`. There is no `ThenReturn(TReturn value)` equivalent (it is a void interceptor). All `ThenCall(Action<TArgs>)` and `Call(Action<TArgs>)` overloads are called directly from user code, never from internal routing. No `ThenCall(TReturn value)` or convenience method exists that would create a lambda and call the Action overload. **No additional changes needed for AsyncVoidMethodInterceptor beyond what the plan already specifies.**

### Concern 3 (MODERATE): Compositor async void simplified callback not addressed in 6e -- RESOLVED

**Problem:** Section 6e only showed the async non-void compositor change (replacing `Func<TArgs, TReturn>` with sync delegate). The async void case at `PreCompiledInterceptorRenderer.cs` lines 1059-1067 emits:
```csharp
var simplifiedType = $"global::System.Action<{tArgs}>";
w.Line($"public {builderReturnType} Call({simplifiedType} callback) => _ov{overloadIndex}.Call(callback);");
```
This also needs to change to use the sync delegate.

**Resolution:** Section 6e updated below to cover both cases:
- Async non-void: `Func<TArgs, TReturn>` becomes sync delegate name
- Async void: `Action<TArgs>` becomes sync delegate name

Both follow the same pattern: replace the generic `Func`/`Action` type with the overload's specific sync delegate name. See updated section 6e.

### Concern 4 (LOW): ClassRenderer.cs hedged as "if applicable" -- RESOLVED

**Problem:** Plan says "ClassRenderer.cs -- Emit sync delegate declarations (if needed)." Should be definitive.

**Resolution:** Confirmed: `ClassRenderer.cs` line 99 calls `BuildDelegateDeclaration` in the same pattern as FlatRenderer, StandaloneClassRenderer, and InlineRenderer. ClassRenderer.cs **definitely** needs sync delegate emission. Updated the Files to Modify table and Phase 2 step 10 to remove the hedge.

### Concern 5 (LOW): No BuildValueDelegate equivalent for TSyncDelegate -- RESOLVED

**Problem:** Related to concern 1. If choosing the `s_syncValueDelegate` approach, need `BuildValueDelegate<TSyncDelegate, TReturn>()`.

**Resolution:** This is already handled by concern 1's resolution. The existing `DelegateInvokerFactory.BuildValueDelegate<TDelegate, TReturn>()` (line 63 of DelegateInvokerFactory.cs) is fully generic -- it works for any delegate type parameter. We simply call it as `BuildValueDelegate<TSyncDelegate, TReturn>()` and store the result in `s_syncValueDelegate`. No changes to DelegateInvokerFactory.cs are needed.

### Concern 6 (LOW): `Return(TReturn first, params TReturn[] rest)` also broken -- RESOLVED

**Problem:** `AsyncMethodInterceptor.Return(TReturn first, params TReturn[] rest)` (lines 236-243) calls `builder.ThenReturn(first)` and `builder.ThenReturn(rest[0])`, which route through `ThenReturn(TReturn value)`, which calls `ThenReturn((_) => value)` -- same breakage as concern 1.

**Resolution:** Fixed by concern 1's resolution. Once `ThenReturn(TReturn value)` is changed to call `ThenReturn(s_syncValueDelegate(value))`, it routes to `ThenReturn(TSyncDelegate)` instead of the removed `ThenReturn(Func<TArgs, TReturn>)`. The `Return(TReturn first, params TReturn[] rest)` method itself does not need changes -- its `Return(first)` call goes to `Return(TReturn value)` which is a direct value path (stores `_returnValue`, no callback routing), and all subsequent `ThenReturn` calls are now correctly routed via `s_syncValueDelegate`.

---

## Detailed Changes

### 1. Generated Delegate Types

Currently, for an async method like `Task<string> TransformAsync(string input, CancellationToken ct)`, the generator emits:

```csharp
public delegate Task<string> TransformAsyncDelegate(string input, CancellationToken ct);
```

After this change, it will emit two delegates:

```csharp
public delegate Task<string> TransformAsyncDelegate(string input, CancellationToken ct);
public delegate string TransformAsyncSyncDelegate(string input, CancellationToken ct);
```

For async void methods like `Task SaveAsync(string data, bool overwrite)`:

```csharp
public delegate Task SaveAsyncDelegate(string data, bool overwrite);
public delegate void SaveAsyncSyncDelegate(string data, bool overwrite);
```

**Naming convention:** `{MethodName}SyncDelegate`.

**When generated:** Only for async methods with 1+ parameters. Zero-param async methods use `AsyncMethodInterceptor0` / `AsyncVoidMethodInterceptor0` which are unaffected. Sync methods already have natural params via TDelegate -- no change needed.

**1-param async methods:** TSyncDelegate is still beneficial even though TArgs is a raw type (no tuple). The current `Func<TArgs, TReturn>` overload works, but `TSyncDelegate` gives IntelliSense the actual parameter name (e.g., `id` instead of a generic `arg`). Also, switching to TSyncDelegate makes the API uniform across all param counts.

### 2. AsyncMethodInterceptor Library Changes

File: `src/KnockOff/Interceptors/AsyncMethodInterceptor.cs`

#### Class signature

```csharp
// BEFORE:
public sealed class AsyncMethodInterceptor<TDelegate, TArgs, TReturn> : IInterceptor
    where TDelegate : Delegate

// AFTER:
public sealed class AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn> : IInterceptor
    where TDelegate : Delegate
    where TSyncDelegate : Delegate
```

#### New static fields

```csharp
// Existing:
private static readonly Func<TDelegate, TArgs, Task<TReturn>> s_asyncInvoker
    = DelegateInvokerFactory.BuildAsyncInvoker<TDelegate, TArgs, TReturn>();

// NEW -- sync invoker for TSyncDelegate callback bridging:
private static readonly Func<TSyncDelegate, TArgs, TReturn> s_syncInvoker
    = DelegateInvokerFactory.BuildInvoker<TSyncDelegate, TArgs, TReturn>();

// NEW -- sync value delegate factory for ThenReturn(TReturn value) routing:
private static readonly Func<TReturn, TSyncDelegate> s_syncValueDelegate
    = DelegateInvokerFactory.BuildValueDelegate<TSyncDelegate, TReturn>();
```

The `s_syncValueDelegate` factory creates a TSyncDelegate that ignores all parameters and returns a fixed value. This is the same pattern used by the sync `MethodInterceptor`'s `s_valueDelegate` (line 24 of MethodInterceptor.cs). It enables `ThenReturn(TReturn value)` to route through `ThenReturn(TSyncDelegate)` instead of the removed `ThenReturn(Func<TArgs, TReturn>)`.

#### Return overload replacement

Replace `Return(Func<TArgs, TReturn> callback)` with:

```csharp
public MethodCallBuilder Return(TSyncDelegate syncCallback)
{
    var builder = new MethodCallBuilder(this);
    _sequence = null; _sequenceIndex = 0;
    _isVerifiable = false; _verifiableTimes = null;
    _hasReturnValue = false; _returnValue = default!; _returnValueTracking = null;
    _call = (args) => Task.FromResult(s_syncInvoker(syncCallback, args));
    _callTracking = builder;
    return builder;
}
```

#### MethodCallBuilder.ThenReturn overload replacement

Replace `ThenReturn(Func<TArgs, TReturn> callback)` with:

```csharp
public MethodSequence ThenReturn(TSyncDelegate syncCallback)
{
    ElevateToSequence();
    var nextBuilder = new MethodCallBuilder(_interceptor);
    _interceptor._sequence!.Add(((args) => Task.FromResult(s_syncInvoker(syncCallback, args)), nextBuilder));
    return new MethodSequence(_interceptor);
}
```

#### MethodCallBuilder.ThenReturn(TReturn value) fix

Change internal routing from `Func<TArgs, TReturn>` to TSyncDelegate via `s_syncValueDelegate`:

```csharp
// BEFORE (broken -- lambda cannot convert to TSyncDelegate):
public MethodSequence ThenReturn(TReturn value)
{
    return ThenReturn((_) => value);  // routes to removed Func<TArgs, TReturn> overload
}

// AFTER (routes through TSyncDelegate overload via value delegate factory):
public MethodSequence ThenReturn(TReturn value)
{
    return ThenReturn(s_syncValueDelegate(value));  // routes to ThenReturn(TSyncDelegate)
}
```

This follows the exact pattern from the sync `MethodInterceptor` (line 445: `return ThenReturn(s_valueDelegate(value))`).

#### MethodSequence.ThenReturn overload replacement

Replace `ThenReturn(Func<TArgs, TReturn> callback)` with:

```csharp
public MethodSequence ThenReturn(TSyncDelegate syncCallback)
{
    var tracking = new MethodCallBuilder(_interceptor);
    _interceptor._sequence!.Add(((args) => Task.FromResult(s_syncInvoker(syncCallback, args)), tracking));
    return this;
}
```

#### MethodSequence.ThenReturn(TReturn value) fix

Same fix as MethodCallBuilder:

```csharp
// BEFORE (broken):
public MethodSequence ThenReturn(TReturn value)
{
    return ThenReturn((_) => value);
}

// AFTER:
public MethodSequence ThenReturn(TReturn value)
{
    return ThenReturn(s_syncValueDelegate(value));
}
```

#### WhenChain.ThenCall overload replacement

Replace `ThenCall(Func<TArgs, TReturn> callback)` with:

```csharp
public WhenChain ThenCall(TSyncDelegate syncCallback)
{
    _interceptor._whenChain ??= new List<WhenMatcherBase>();
    _interceptor._whenChain.Add(new WhenMatcherCall((args) => Task.FromResult(s_syncInvoker(syncCallback, args))));
    return this;
}
```

#### Inner class type parameter updates

All inner classes (`MethodCallBuilder`, `MethodSequence`, `WhenBuilder`, `WhenChain`) reference the parent interceptor type. Their `_interceptor` field type changes to `AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>`. These are inner classes, so they automatically pick up the enclosing type's type parameters -- no explicit changes needed beyond the class signature.

### 3. AsyncVoidMethodInterceptor Library Changes

File: `src/KnockOff/Interceptors/AsyncVoidMethodInterceptor.cs`

#### Class signature

```csharp
// BEFORE:
public sealed class AsyncVoidMethodInterceptor<TDelegate, TArgs> : IInterceptor
    where TDelegate : Delegate

// AFTER:
public sealed class AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs> : IInterceptor
    where TDelegate : Delegate
    where TSyncDelegate : Delegate
```

#### New static invoker

```csharp
// Existing:
private static readonly Func<TDelegate, TArgs, Task> s_asyncVoidInvoker
    = DelegateInvokerFactory.BuildAsyncVoidInvoker<TDelegate, TArgs>();

// NEW:
private static readonly Action<TSyncDelegate, TArgs> s_syncVoidInvoker
    = DelegateInvokerFactory.BuildVoidInvoker<TSyncDelegate, TArgs>();
```

#### Call overload replacement

Replace `Call(Action<TArgs> callback)` with:

```csharp
public MethodCallBuilder Call(TSyncDelegate syncCallback)
{
    var builder = new MethodCallBuilder(this);
    _sequence = null; _sequenceIndex = 0;
    _isVerifiable = false; _verifiableTimes = null;
    _call = (args) => { s_syncVoidInvoker(syncCallback, args); return Task.CompletedTask; };
    _callTracking = builder;
    return builder;
}
```

#### MethodCallBuilder.ThenCall overload replacement

Replace `ThenCall(Action<TArgs> callback)` with:

```csharp
public MethodSequence ThenCall(TSyncDelegate syncCallback)
{
    ElevateToSequence();
    var nextBuilder = new MethodCallBuilder(_interceptor);
    Func<TArgs, Task> wrapped = (args) => { s_syncVoidInvoker(syncCallback, args); return Task.CompletedTask; };
    _interceptor._sequence!.Add((wrapped, nextBuilder));
    return new MethodSequence(_interceptor);
}
```

#### MethodSequence.ThenCall overload replacement

Replace `ThenCall(Action<TArgs> callback)` with:

```csharp
public MethodSequence ThenCall(TSyncDelegate syncCallback)
{
    var tracking = new MethodCallBuilder(_interceptor);
    Func<TArgs, Task> wrapped = (args) => { s_syncVoidInvoker(syncCallback, args); return Task.CompletedTask; };
    _interceptor._sequence!.Add((wrapped, tracking));
    return this;
}
```

#### VoidWhenBuilder.Call overload replacement

Replace `Call(Action<TArgs> callback)` with:

```csharp
public VoidWhenChain Call(TSyncDelegate syncCallback)
{
    ((VoidWhenMatcherPredicate)_interceptor._whenChain![_matcherIndex])
        .SetCallback((args) => { s_syncVoidInvoker(syncCallback, args); return Task.CompletedTask; });
    return new VoidWhenChain(_interceptor, _matcherIndex);
}
```

#### VoidWhenBuilder.ThenCall and VoidWhenChain.ThenCall overload replacements

Same pattern: replace `Action<TArgs>` with `TSyncDelegate`, use `s_syncVoidInvoker` bridging.

### 4. DelegateInvokerFactory Changes

File: `src/KnockOff/Interceptors/DelegateInvokerFactory.cs`

**No changes required.** The existing `BuildInvoker<TDelegate, TArgs, TReturn>()` and `BuildVoidInvoker<TDelegate, TArgs>()` methods already handle arbitrary delegate types. TSyncDelegate will use these same methods -- they just need to be called with `TSyncDelegate` as the delegate type parameter instead of `TDelegate`. The expression tree building logic is fully generic.

### 5. Slot Interface Changes

Files in `src/KnockOff/Interceptors/Slots/`

#### IAsyncMethodOverloadSlots.cs

Each slot interface gains TSyncDelegate:

```csharp
// BEFORE:
public interface IAsyncMethodOverloadSlot1<TDelegate, TArgs, TReturn>
    where TDelegate : Delegate
{
    AsyncMethodInterceptor<TDelegate, TArgs, TReturn> AsyncMethodSlot1Interceptor { get; }
}

// AFTER:
public interface IAsyncMethodOverloadSlot1<TDelegate, TSyncDelegate, TArgs, TReturn>
    where TDelegate : Delegate
    where TSyncDelegate : Delegate
{
    AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn> AsyncMethodSlot1Interceptor { get; }
}
```

Repeat for slots 2-8.

#### IAsyncVoidOverloadSlots.cs

Each slot interface gains TSyncDelegate:

```csharp
// BEFORE:
public interface IAsyncVoidOverloadSlot1<TDelegate, TArgs>
    where TDelegate : Delegate
{
    AsyncVoidMethodInterceptor<TDelegate, TArgs> AsyncVoidSlot1Interceptor { get; }
}

// AFTER:
public interface IAsyncVoidOverloadSlot1<TDelegate, TSyncDelegate, TArgs>
    where TDelegate : Delegate
    where TSyncDelegate : Delegate
{
    AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs> AsyncVoidSlot1Interceptor { get; }
}
```

Repeat for slots 2-8.

#### AsyncMethodSlotExtensions.cs

Extension methods gain TSyncDelegate type parameter. The `Return(Func<TArgs, TReturn>)` overload becomes `Return(TSyncDelegate)`:

```csharp
// BEFORE:
public static AsyncMethodInterceptor<TDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TArgs, TReturn>(
    this IAsyncMethodOverloadSlot1<TDelegate, TArgs, TReturn> self, Func<TArgs, TReturn> callback)
    where TDelegate : Delegate
    => self.AsyncMethodSlot1Interceptor.Return(callback);

// AFTER:
public static AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>.MethodCallBuilder Return<TDelegate, TSyncDelegate, TArgs, TReturn>(
    this IAsyncMethodOverloadSlot1<TDelegate, TSyncDelegate, TArgs, TReturn> self, TSyncDelegate callback)
    where TDelegate : Delegate
    where TSyncDelegate : Delegate
    => self.AsyncMethodSlot1Interceptor.Return(callback);
```

All slot extension methods for async types gain the TSyncDelegate parameter and updated return types.

#### AsyncVoidSlotExtensions.cs

Extension methods gain TSyncDelegate type parameter. The `Call(Action<TArgs>)` overload becomes `Call(TSyncDelegate)`:

```csharp
// BEFORE:
public static AsyncVoidMethodInterceptor<TDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TArgs>(
    this IAsyncVoidOverloadSlot1<TDelegate, TArgs> self, Action<TArgs> callback)
    where TDelegate : Delegate
    => self.AsyncVoidSlot1Interceptor.Call(callback);

// AFTER:
public static AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs>.MethodCallBuilder Call<TDelegate, TSyncDelegate, TArgs>(
    this IAsyncVoidOverloadSlot1<TDelegate, TSyncDelegate, TArgs> self, TSyncDelegate callback)
    where TDelegate : Delegate
    where TSyncDelegate : Delegate
    => self.AsyncVoidSlot1Interceptor.Call(callback);
```

### 6. Generator / Renderer Changes

#### PreCompiledInterceptorRenderer.cs (primary file)

This is the central file that computes interceptor field types and emits delegate declarations.

**6a. New method: `BuildSyncDelegateDeclaration`**

Add a companion to `BuildDelegateDeclaration` that generates the sync version of an async delegate:

```csharp
public static string? BuildSyncDelegateDeclaration(string methodName, IEnumerable<ParameterModel> parameters, string returnType, bool isVoid, string? delegateBaseName = null)
{
    // Only applicable to async methods with 1+ params
    var paramList = parameters.ToList();
    if (paramList.Count == 0) return null;

    var (innerType, isAsyncTaskT, isAsyncValueTaskT) = GetAsyncTypeInfo(returnType);
    var (isVoidTask, isVoidValueTask) = GetVoidAsyncInfo(returnType);
    var isAsync = isAsyncTaskT || isAsyncValueTaskT || isVoidTask || isVoidValueTask;

    if (!isAsync) return null; // Sync methods don't need a sync delegate

    var syncDelegateName = ComputeSyncDelegateTypeName(delegateBaseName ?? methodName);
    var paramDecls = string.Join(", ", paramList.Select(p => $"{p.RefPrefix}{p.Type} {p.EscapedName}"));

    string syncReturnType;
    if (isVoid || isVoidTask || isVoidValueTask)
        syncReturnType = "void";
    else
        syncReturnType = innerType; // The inner type (e.g., string for Task<string>)

    return $"public delegate {syncReturnType} {syncDelegateName}({paramDecls});";
}

public static string ComputeSyncDelegateTypeName(string methodName)
{
    return $"{methodName}SyncDelegate";
}
```

**6b. Update `GetMethodInterceptorType` for async types**

Add TSyncDelegate as the second type parameter:

```csharp
// BEFORE (async non-void, 1+ params):
return $"global::KnockOff.Interceptors.AsyncMethodInterceptor<{delegateType}, {tArgs}, {innerType}>";

// AFTER:
var syncDelegateType = ComputeSyncDelegateTypeName(nameForDelegate);
return $"global::KnockOff.Interceptors.AsyncMethodInterceptor<{delegateType}, {syncDelegateType}, {tArgs}, {innerType}>";
```

Same for async void:
```csharp
// BEFORE:
return $"global::KnockOff.Interceptors.AsyncVoidMethodInterceptor<{delegateType}, {tArgs}>";

// AFTER:
var syncDelegateType = ComputeSyncDelegateTypeName(nameForDelegate);
return $"global::KnockOff.Interceptors.AsyncVoidMethodInterceptor<{delegateType}, {syncDelegateType}, {tArgs}>";
```

**6c. Update `GetOverloadInterceptorType` for async types**

Same pattern as above but using the overload's delegate name.

**6d. Update compositor rendering**

In `BuildSlotInterfaceList`, `RenderSlotInterfacePropertyImplementations`, and `RenderOverloadReturnMethod`, the async method slot interfaces gain the TSyncDelegate parameter. The simplified callback overload on compositors changes from `Func<TArgs, TReturn>` to the sync delegate type.

**6e. Update compositor simplified callback emission**

In `RenderOverloadReturnMethod`, replace BOTH the async non-void and async void simplified callback emissions:

**Async non-void** (lines 1101-1108):
```csharp
// BEFORE:
var tArgs = ComputeTArgsType(overload.Parameters);
var simplifiedType = $"global::System.Func<{tArgs}, {innerType}>";
w.Line($"public {builderReturnType} Return({simplifiedType} callback) => _ov{overloadIndex}.Return(callback);");

// AFTER:
var syncDelegateName = ComputeSyncDelegateTypeName(overload.DelegateName);
w.Line($"public {builderReturnType} Return({syncDelegateName} callback) => _ov{overloadIndex}.Return(callback);");
```

**Async void** (lines 1059-1067):
```csharp
// BEFORE:
if (isVoidAsync && paramCount > 0)
{
    var tArgs = ComputeTArgsType(overload.Parameters);
    var simplifiedType = $"global::System.Action<{tArgs}>";
    w.Line($"public {builderReturnType} Call({simplifiedType} callback) => _ov{overloadIndex}.Call(callback);");
}

// AFTER:
if (isVoidAsync && paramCount > 0)
{
    var syncDelegateName = ComputeSyncDelegateTypeName(overload.DelegateName);
    w.Line($"public {builderReturnType} Call({syncDelegateName} callback) => _ov{overloadIndex}.Call(callback);");
}
```

Note: `ComputeSyncDelegateTypeName` derives the sync delegate name from the overload's `DelegateName` (which is already uniquely suffixed for overloads). The overload's `DelegateName` might be `TransformAsync_String_CancellationTokenDelegate`, so the sync version would be `TransformAsync_String_CancellationTokenSyncDelegate`. Alternatively, we can compute it from the base delegate name (strip `Delegate`, add `SyncDelegate`) -- the developer should verify the naming convention produces correct results for overload compositors. The key point: the sync delegate for an overload's compositor method must match the sync delegate type emitted alongside that overload's async delegate declaration.

#### All Four Renderers (emit delegate declarations)

Each renderer that currently emits delegate declarations via `BuildDelegateDeclaration` must also emit the sync delegate via `BuildSyncDelegateDeclaration` for async methods:

- `FlatRenderer.cs` -- standalone interface stubs (patterns 1, 2)
- `StandaloneClassRenderer.cs` -- standalone class stubs (patterns 3, 4)
- `InlineRenderer.cs` -- inline and open generic stubs (patterns 5, 6, 7, 8, 9)
- `ClassRenderer.cs` -- class stubs (patterns 3, 4 if separate)

The change in each is minimal: wherever `BuildDelegateDeclaration` is called for a method, also call `BuildSyncDelegateDeclaration` and emit the result if non-null.

### 7. Model Changes

#### MethodOverloadSignature

If the `MethodOverloadSignature` model already contains delegate naming information, add a `SyncDelegateName` property computed the same way as `DelegateName` but with the `SyncDelegate` suffix. Alternatively, the renderer can compute it on the fly using `ComputeSyncDelegateTypeName`.

No deep model changes are needed since the sync delegate is a pure rendering concern.

### 8. Test Migration

Tests that currently use the `Func<TArgs, TReturn>` tuple syntax for async simplified callbacks will be updated to use natural parameter syntax:

```csharp
// BEFORE:
stub.TransformAsync.Return(((string input, CancellationToken ct) args) => $"[{args.input}:ct]");

// AFTER:
stub.TransformAsync.Return((input, ct) => $"[{input}:ct]");
```

Similarly for `Action<TArgs>` on async void methods:

```csharp
// BEFORE:
stub.ProcessAsync.Call(((string data, bool flag) args) => { /* use args.data */ });

// AFTER:
stub.ProcessAsync.Call((data, flag) => { /* use data directly */ });
```

**Files to update:**
- `src/Design/Design.Stubs/Methods/MethodOverloads.cs` (the `AsyncOverloads_WithAndWithoutCancellation` method)
- All test files in `src/Tests/KnockOffTests/` that use tuple syntax for async simplified callbacks
- Documentation sample files in `src/Tests/KnockOff.Documentation.Samples/`

---

## Edge Cases

### Zero-parameter async methods

`AsyncMethodInterceptor0<TReturn>` and `AsyncVoidMethodInterceptor0` are **not affected**. They have no TDelegate, no TArgs, and no TSyncDelegate. Their simplified callbacks (`Func<TReturn>` and `Action`) work fine without named parameters since there are no parameters.

### One-parameter async methods

Even though `Func<TArgs, TReturn>` works acceptably for 1 param (no tuple), TSyncDelegate still provides value:
- IntelliSense shows the actual parameter name (e.g., `id`) instead of the generic type parameter name
- The API is uniform across all param counts -- users always see the same overload pattern regardless of param count

TSyncDelegate IS applied to 1-param methods. The generated field type becomes:
```csharp
AsyncMethodInterceptor<FetchAsyncDelegate, FetchAsyncSyncDelegate, int, string>
```

### Generic compositors (open generic patterns 8, 9)

For generic compositors where delegates are nested inside the class (because they reference type parameters), the sync delegate must also be emitted inside the class. This follows the same pattern as the existing async delegate -- if `BuildDelegateDeclaration` emits inside the class for generic compositors, `BuildSyncDelegateDeclaration` must too.

### Overload disambiguation

For compositor overload methods, the compiler currently disambiguates between the TDelegate overload and the `Func<TArgs, TReturn>` overload based on return type (the delegate returns `Task<TReturn>`, the Func returns `TReturn`). With TSyncDelegate, disambiguation still works because:

- `Return(TDelegate)` -- delegate returns `Task<TReturn>`
- `Return(TSyncDelegate)` -- delegate returns `TReturn`
- `Return(TReturn value)` -- not a delegate at all

The C# compiler resolves these by matching the lambda's return type against the delegate signatures.

### ValueTask wrapping

No change to how ValueTask wrapping works. The interceptor still operates on Task/Task<TReturn> internally. The generated code wraps `new ValueTask(...)` or `new ValueTask<T>(...)` around the interceptor's Invoke call, exactly as today.

### Stub overrides (SetFallback)

`SetFallback(TDelegate?)` is unchanged. The fallback delegate is always the full async version. TSyncDelegate is only for the simplified sync callback API surface. No changes to `SetFallback`, `SetSourceFallback`, or the renderer's fallback expression generation.

### Sequence ThenReturn with sync delegate

The `ThenReturn(TSyncDelegate)` overload wraps in `Task.FromResult(s_syncInvoker(syncCallback, args))`. `ThenReturn(TDelegate)` remains unchanged. `ThenReturn(TReturn value)` is updated to route through `ThenReturn(s_syncValueDelegate(value))` -- the `s_syncValueDelegate` factory creates a TSyncDelegate that ignores all parameters and returns the fixed value. This follows the same pattern as the sync `MethodInterceptor`'s `s_valueDelegate`.

### Return(TReturn first, params TReturn[] rest) internal routing

This method calls `Return(first)` (routes to the value path, no callback) then `builder.ThenReturn(first)` and `seq.ThenReturn(rest[i])`. With the `s_syncValueDelegate` fix to `ThenReturn(TReturn value)`, all these calls correctly route through TSyncDelegate. No changes needed to the `Return(TReturn first, params TReturn[] rest)` method itself.

### ElevateToSequence internal storage

`ElevateToSequence()` in `MethodCallBuilder` creates a `Func<TArgs, Task<TReturn>>` lambda from the return value path: `(_) => Task.FromResult(capturedValue)`. This is internal storage (the sequence list stores `Func<TArgs, Task<TReturn>>` entries), not a public API call that routes through an overload. No change needed.

---

## Patterns Affected

| Pattern | TSyncDelegate Applies | Notes |
|---|---|---|
| 1. Standalone | Yes, for 1+ param async methods | FlatRenderer emits sync delegate |
| 2. Generic Standalone | Yes | Same as Pattern 1 |
| 3. Standalone Class | Yes | StandaloneClassRenderer emits sync delegate |
| 4. Generic Standalone Class | Yes | Same as Pattern 3 |
| 5. Inline Interface | Yes | InlineRenderer emits sync delegate |
| 6. Inline Class | Yes | InlineRenderer emits sync delegate |
| 7. Inline Delegate | Only if the delegate itself is async | Depends on delegate signature |
| 8. Open Generic Interface | Yes | InlineRenderer, delegates inside class |
| 9. Open Generic Class | Yes | InlineRenderer, delegates inside class |

---

## Implementation Phases

### Phase 1: Library Type Changes

1. Add `TSyncDelegate` type parameter to `AsyncMethodInterceptor<TDelegate, TSyncDelegate, TArgs, TReturn>`
2. Add static sync invoker field using `DelegateInvokerFactory.BuildInvoker<TSyncDelegate, TArgs, TReturn>()`
3. Add static sync value delegate factory using `DelegateInvokerFactory.BuildValueDelegate<TSyncDelegate, TReturn>()`
4. Replace all `Func<TArgs, TReturn>` overloads with `TSyncDelegate` overloads using `s_syncInvoker`
5. Fix `ThenReturn(TReturn value)` methods to route through `s_syncValueDelegate` (both MethodCallBuilder and MethodSequence)
6. Add `TSyncDelegate` type parameter to `AsyncVoidMethodInterceptor<TDelegate, TSyncDelegate, TArgs>`
7. Add static sync void invoker field using `DelegateInvokerFactory.BuildVoidInvoker<TSyncDelegate, TArgs>()`
8. Replace all `Action<TArgs>` overloads with `TSyncDelegate` overloads using `s_syncVoidInvoker`
9. Update all 8 async method slot interfaces to include TSyncDelegate
10. Update all 8 async void slot interfaces to include TSyncDelegate
11. Update `AsyncMethodSlotExtensions` -- all Return/When overloads gain TSyncDelegate
12. Update `AsyncVoidSlotExtensions` -- all Call/When overloads gain TSyncDelegate

**Verification gate:** Library project compiles.

### Phase 2: Generator / Renderer Changes

1. Add `ComputeSyncDelegateTypeName` and `BuildSyncDelegateDeclaration` to `PreCompiledInterceptorRenderer`
2. Update `GetMethodInterceptorType` to include TSyncDelegate in async field types
3. Update `GetOverloadInterceptorType` to include TSyncDelegate in async overload field types
4. Update `BuildSlotInterfaceList` for async slot interfaces
5. Update `RenderSlotInterfacePropertyImplementations` for async slots
6. Update `RenderOverloadReturnMethod` to use sync delegate instead of `Func<TArgs, TReturn>` / `Action<TArgs>`
7. Update `FlatRenderer` to emit sync delegate declarations alongside async delegates
8. Update `StandaloneClassRenderer` to emit sync delegate declarations
9. Update `InlineRenderer` to emit sync delegate declarations
10. Update `ClassRenderer` to emit sync delegate declarations (confirmed needed: line 99 calls BuildDelegateDeclaration)

**Verification gate:** Generator project compiles. Run `dotnet build src/Design/Design.Stubs` to verify generated code compiles.

### Phase 3: Test Migration

1. Update `src/Design/Design.Stubs/Methods/MethodOverloads.cs` -- replace tuple syntax with natural params
2. Update `src/Design/Design.Stubs/Methods/AsyncConsistency.cs` if affected
3. Search all test files for `Func<.*TArgs.*TReturn>` and `Action<.*TArgs>` patterns on async interceptors
4. Update `src/Tests/KnockOffTests/` test files that use tuple syntax
5. Update `src/Tests/KnockOff.Documentation.Samples/` files that use tuple syntax

**Verification gate:** Full test suite passes -- `dotnet test src/KnockOff.sln`.

### Phase 4: Design Project Verification

1. Verify Design.Stubs builds with all 3 TFMs
2. Verify Design.Tests pass
3. Verify IntelliSense shows named parameters on simplified sync callbacks
4. Update any Design.Stubs examples that demonstrate the new natural syntax

**Verification gate:** Design projects build and test successfully.

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Breaking change: existing `Func<TArgs, TReturn>` code stops compiling | High (intentional) | Low | TSyncDelegate is a direct replacement; migration is mechanical. The old `Func` syntax was a pain point users want to eliminate. |
| Overload ambiguity between TDelegate and TSyncDelegate | Low | Medium | Delegates have different return types (Task<T> vs T). C# compiler resolves unambiguously. |
| Generic compositor delegate scoping issues | Low | Medium | Same scoping rules as existing async delegates -- known pattern. |
| Expression tree compilation overhead | Very Low | Very Low | `s_syncInvoker` is compiled once per closed generic type combo (same as existing `s_asyncInvoker`). |
| Increased library type surface (more type params) | N/A | Low | One additional type parameter per async interceptor. Acceptable complexity increase for the ergonomic improvement. |
| Internal call chain breakage (ThenReturn(TReturn) routing) | High (caught) | High | Resolved: `s_syncValueDelegate` factory routes value overloads through TSyncDelegate, following proven MethodInterceptor pattern. |
| Compositor sync delegate naming for overloads | Low | Low | `ComputeSyncDelegateTypeName` must use the overload's `DelegateName` as base, not the method name, to produce unique sync delegate names per overload signature. |

---

## Open Questions

None. All developer concerns have been resolved (see "Developer Concern Resolutions" section above). The design follows directly from the established TDelegate pattern. TSyncDelegate is essentially "apply the same approach to the sync callback that we already applied to the async callback." The `s_syncValueDelegate` factory resolves the internal call chain issue identified by the developer, using the exact same pattern already proven in the sync `MethodInterceptor`.

---

## Breaking Changes

**Yes -- the simplified sync callback API signature changes.**

Before:
```csharp
// These overloads existed on AsyncMethodInterceptor<TDelegate, TArgs, TReturn>:
public MethodCallBuilder Return(Func<TArgs, TReturn> callback);

// And on AsyncVoidMethodInterceptor<TDelegate, TArgs>:
public MethodCallBuilder Call(Action<TArgs> callback);
```

After:
```csharp
// Replaced with:
public MethodCallBuilder Return(TSyncDelegate syncCallback);
public MethodCallBuilder Call(TSyncDelegate syncCallback);
```

**Migration is mechanical and improves ergonomics.** Any code using the old `Func<TArgs, TReturn>` / `Action<TArgs>` overloads must switch to using the sync delegate's natural parameter syntax. Since the sync delegate has the same parameter types, lambdas with natural parameter names (the desired usage) work without any explicit types. Code that explicitly typed the `Func<TArgs, TReturn>` will need to be updated.

**The generated field type changes too**, adding a type parameter. This is only visible if users were writing code that explicitly references the interceptor type by name (unlikely -- they typically use `var`).

---

## Files to Modify

### Library (`src/KnockOff/Interceptors/`)

| File | Change |
|---|---|
| `AsyncMethodInterceptor.cs` | Add TSyncDelegate type param, `s_syncInvoker`, `s_syncValueDelegate`, replace Func overloads, fix ThenReturn(TReturn) routing |
| `AsyncVoidMethodInterceptor.cs` | Add TSyncDelegate type param, static invoker, replace Action overloads |
| `Slots/IAsyncMethodOverloadSlots.cs` | Add TSyncDelegate to all 8 slot interfaces |
| `Slots/IAsyncVoidOverloadSlots.cs` | Add TSyncDelegate to all 8 slot interfaces |
| `Slots/AsyncMethodSlotExtensions.cs` | Update all extension methods for TSyncDelegate |
| `Slots/AsyncVoidSlotExtensions.cs` | Update all extension methods for TSyncDelegate |

### Generator (`src/Generator/Renderer/`)

| File | Change |
|---|---|
| `Shared/PreCompiledInterceptorRenderer.cs` | Add sync delegate methods, update type computation, update compositor rendering |
| `FlatRenderer.cs` | Emit sync delegate declarations |
| `StandaloneClassRenderer.cs` | Emit sync delegate declarations |
| `InlineRenderer.cs` | Emit sync delegate declarations |
| `ClassRenderer.cs` | Emit sync delegate declarations (confirmed: line 99 calls BuildDelegateDeclaration) |

### Tests / Design

| File | Change |
|---|---|
| `src/Design/Design.Stubs/Methods/MethodOverloads.cs` | Replace tuple syntax with natural params |
| `src/Design/Design.Stubs/Methods/AsyncConsistency.cs` | Verify/update examples |
| Multiple test files in `src/Tests/KnockOffTests/` | Replace tuple syntax |
| Multiple files in `src/Tests/KnockOff.Documentation.Samples/` | Replace tuple syntax |

---

## Architectural Verification

**Scope Table:**

| Pattern | TSyncDelegate | Notes |
|---|---|---|
| 1. Standalone | Yes | FlatRenderer + FlatModelBuilder pipeline |
| 2. Generic Standalone | Yes | Same pipeline as Pattern 1 |
| 3. Standalone Class | Yes | StandaloneClassRenderer pipeline |
| 4. Generic Standalone Class | Yes | Same pipeline as Pattern 3 |
| 5. Inline Interface | Yes | InlineRenderer pipeline |
| 6. Inline Class | Yes | InlineRenderer pipeline |
| 7. Inline Delegate | Conditional | Only if delegate is async |
| 8. Open Generic Interface | Yes | InlineRenderer, delegates inside class |
| 9. Open Generic Class | Yes | InlineRenderer, delegates inside class |

**Breaking Changes:** Yes -- `Func<TArgs, TReturn>` and `Action<TArgs>` overloads replaced with TSyncDelegate. This is an intentional ergonomic improvement.

**Codebase Analysis:**

Files examined:
- `src/KnockOff/Interceptors/AsyncMethodInterceptor.cs` -- current 3-param TTuple async interceptor
- `src/KnockOff/Interceptors/AsyncVoidMethodInterceptor.cs` -- current 2-param TTuple async void interceptor
- `src/KnockOff/Interceptors/MethodInterceptor.cs` -- sync equivalent (reference for TDelegate pattern)
- `src/KnockOff/Interceptors/VoidMethodInterceptor.cs` -- sync void equivalent
- `src/KnockOff/Interceptors/DelegateInvokerFactory.cs` -- expression tree bridging (already supports the needed patterns)
- `src/KnockOff/Interceptors/AsyncMethodInterceptor0.cs` -- zero-param async (not affected)
- `src/KnockOff/Interceptors/Slots/IAsyncMethodOverloadSlots.cs` -- 8 slot interfaces
- `src/KnockOff/Interceptors/Slots/IAsyncVoidOverloadSlots.cs` -- 8 slot interfaces
- `src/KnockOff/Interceptors/Slots/AsyncMethodSlotExtensions.cs` -- slot extension methods
- `src/KnockOff/Interceptors/Slots/AsyncVoidSlotExtensions.cs` -- slot extension methods
- `src/Generator/Renderer/Shared/PreCompiledInterceptorRenderer.cs` -- central renderer
- `src/Design/Design.Stubs/Methods/AsyncConsistency.cs` -- async design examples
- `src/Design/Design.Stubs/Methods/MethodOverloads.cs` -- overload design examples (shows the tuple syntax problem)
- `src/Design/Design.Stubs/Generated/.../AsyncServiceStub.g.cs` -- generated output example
- `src/Design/Design.Stubs/Generated/.../MethodOverloadsDemo.Stubs.g.cs` -- generated compositor output
- `docs/todos/ttuple-interceptors.md` -- parent todo

**Design Project Verification:** Deferred to implementation. The plan defines the sync delegate pattern; the developer will verify compilation via Design.Stubs builds.

---

## Implementation Progress

**Started:** 2026-02-15
**Developer:** knockoff-developer
**Current Status:** Awaiting Verification

### Phase 1: Library Type Changes -- COMPLETE

All library changes implemented as specified. Build verified: `dotnet build src/KnockOff/KnockOff.csproj` passed with 0 errors, 0 warnings across all 3 TFMs (net8.0, net9.0, net10.0).

### Phase 2: Generator / Renderer Changes -- COMPLETE

All renderer changes implemented as specified. `dotnet build src/Generator/Generator.csproj` passed. `dotnet build src/Design/Design.Stubs` passed with 0 errors, 0 warnings across all 3 TFMs.

### Phase 3: Test Migration -- COMPLETE

All test files updated to use natural parameter syntax. Full test suite passed.

### Phase 4: Design Project Verification -- COMPLETE

`dotnet build src/Design && dotnet test src/Design` passed: 370 tests passed, 0 failures across all 3 TFMs.

---

## Completion Evidence

### Build Results

**Full solution build:** `dotnet build src/KnockOff.sln` -- 0 errors, 0 warnings

**Design projects build:** `dotnet build src/Design` -- 0 errors, 0 warnings (all 3 TFMs)

### Test Results

**Full test suite:** `dotnet test src/KnockOff.sln`

| Project | net8.0 | net9.0 | net10.0 |
|---|---|---|---|
| KnockOffTests | 1717 passed, 4 skipped | 1718 passed, 4 skipped | 1718 passed, 4 skipped |
| KnockOff.NeatooInterfaceTests | 473 passed | 473 passed | 473 passed |
| KnockOff.Documentation.Samples | 691 passed | 691 passed | 691 passed |
| KnockOffTests.AssemblyStrict | 14 passed | 14 passed | 14 passed |
| NumberedSlotPrototype | -- | 33 passed | -- |
| **Total** | **2895 passed, 4 skipped** | **2929 passed, 4 skipped** | **2896 passed, 4 skipped** |

**0 failures.** The 4 skipped tests are pre-existing BugRegressionTests (PropertySetBuilder/GetBuilder/IndexerSetBuilder/GetBuilder Verifiable CalledConstraint tests) unrelated to this change.

**Design tests:** `dotnet test src/Design` -- 370 passed per TFM (1110 total), 0 failures.

### Files Modified

**Library (Phase 1):**
- `/home/keithvoels/KnockOff/src/KnockOff/Interceptors/AsyncMethodInterceptor.cs` -- Added TSyncDelegate (4th type param), s_syncInvoker, s_syncValueDelegate, replaced all Func<TArgs, TReturn> with TSyncDelegate
- `/home/keithvoels/KnockOff/src/KnockOff/Interceptors/AsyncVoidMethodInterceptor.cs` -- Added TSyncDelegate (3rd type param), s_syncVoidInvoker, replaced all Action<TArgs> with TSyncDelegate
- `/home/keithvoels/KnockOff/src/KnockOff/Interceptors/Slots/IAsyncMethodOverloadSlots.cs` -- All 8 interfaces updated
- `/home/keithvoels/KnockOff/src/KnockOff/Interceptors/Slots/IAsyncVoidOverloadSlots.cs` -- All 8 interfaces updated
- `/home/keithvoels/KnockOff/src/KnockOff/Interceptors/Slots/AsyncMethodSlotExtensions.cs` -- All extension methods updated
- `/home/keithvoels/KnockOff/src/KnockOff/Interceptors/Slots/AsyncVoidSlotExtensions.cs` -- All extension methods updated

**Generator (Phase 2):**
- `/home/keithvoels/KnockOff/src/Generator/Renderer/Shared/PreCompiledInterceptorRenderer.cs` -- Added ComputeSyncDelegateTypeName, BuildSyncDelegateDeclaration, BuildOverloadSyncDelegateDeclaration, ComputeOverloadSyncDelegateName; updated GetMethodInterceptorType, GetOverloadInterceptorType, BuildSlotInterfaceList, RenderSlotInterfacePropertyImplementations, RenderOverloadReturnMethod, RenderOverloadCompositorClass
- `/home/keithvoels/KnockOff/src/Generator/Renderer/FlatRenderer.cs` -- Emits sync delegate declarations alongside async delegates
- `/home/keithvoels/KnockOff/src/Generator/Renderer/ClassRenderer.cs` -- Emits sync delegate declarations
- `/home/keithvoels/KnockOff/src/Generator/Renderer/InlineRenderer.cs` -- Emits sync delegate declarations
- `/home/keithvoels/KnockOff/src/Generator/Renderer/StandaloneClassRenderer.cs` -- Emits sync delegate declarations

**Tests/Design (Phase 3):**
- `/home/keithvoels/KnockOff/src/Design/Design.Stubs/Methods/MethodOverloads.cs` -- Replaced tuple syntax with natural params
- `/home/keithvoels/KnockOff/src/Design/Design.Tests/MethodTests/MethodOverloadTests.cs` -- Replaced tuple syntax with natural params
- `/home/keithvoels/KnockOff/src/Tests/KnockOffTests/Interceptors/AsyncMethodInterceptorTests.cs` -- Added sync delegates, updated type params, natural params
- `/home/keithvoels/KnockOff/src/Tests/KnockOffTests/Interceptors/AsyncVoidMethodInterceptorTests.cs` -- Added sync delegates, updated type params, natural params
- `/home/keithvoels/KnockOff/src/Tests/KnockOffTests/Interceptors/SlotExtensionTests.cs` -- Added sync delegates, updated async compositor/slot types
- `/home/keithvoels/KnockOff/src/Tests/KnockOffTests/OverloadGroupAsyncCallbackTests.cs` -- Replaced tuple syntax with natural params
- `/home/keithvoels/KnockOff/src/Tests/KnockOffTests/AsyncCallbackSimplificationTests.cs` -- Replaced tuple syntax with natural params
- `/home/keithvoels/KnockOff/src/Tests/KnockOff.Documentation.Samples/AsyncSamples.cs` -- Updated throw-in-callback to use sync delegate cast
