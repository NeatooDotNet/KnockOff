# Remove ko Parameter from OnCall/Get/Set Callbacks

**Date:** 2026-01-19
**Related Todo:** [Remove ko Parameter](../todos/remove-ko-parameter.md)
**Status:** Draft
**Last Updated:** 2026-01-19 (Updated with review corrections)

---

## Overview

Remove the redundant `ko` parameter from all generated delegate signatures for OnCall, Get, and Set callbacks. The `ko` parameter passes a reference to the stub instance, but users already have access to the stub through local variables. Removing this parameter simplifies the API and reduces "noise" in callback signatures.

---

## Approach

**Option A: Remove ko parameter entirely (Recommended)**

The `ko` parameter is redundant because:
1. Users always have the stub as a local variable (e.g., `var stub = new FooStub();`)
2. Closures can capture the stub if inter-callback access is needed
3. The parameter adds visual noise to every callback signature

**Before:**
```csharp
stub.GetUser.OnCall((ko, id) => new User { Id = id });
stub.IsActive.Get((ko) => true);
stub.Name.Set((ko, value) => { });
```

**After:**
```csharp
stub.GetUser.OnCall((id) => new User { Id = id });
stub.IsActive.Get(() => true);
stub.Name.Set((value) => { });
```

---

## Design

### Affected Components

The change affects renderers, builders, and model builders:

| File | What Changes |
|------|--------------|
| `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` | Remove `ko` from Invoke method params and callback args |
| `src/Generator/Renderer/FlatRenderer.cs` | Remove `ko` from property/indexer Get/Set delegate types and implementation invocations; update legacy `RenderInvokeMethod` |
| `src/Generator/Renderer/InlineRenderer.cs` | Remove `ko` from property/indexer Get/Set delegate types, implementation invocations, and delegate stub OnCall types |
| `src/Generator/Builder/UnifiedInterceptorBuilder.cs` | Remove `ko` from delegate signature construction |
| `src/Generator/Builder/InlineModelBuilder.cs` | Remove `this,` from `OnCallArgs` construction for indexers and generic methods |

### Model Changes Required

While most model types do not encode `ko` directly, `InlineModelBuilder.cs` constructs `OnCallArgs` values that include `this,` prefix. These need to be updated:
- Line 762 (indexer): `OnCallArgs: $"this, {argList}"` - remove `this,`
- Line 934 (generic method): `OnCallArgs: member.Parameters.Count > 0 ? $"this, {argList}" : "this"` - remove `this,`

---

## Implementation Steps

### Phase 1: Generator Changes (Builders)

#### Step 1.1: Update `UnifiedInterceptorBuilder.cs`

**File:** `src/Generator/Builder/UnifiedInterceptorBuilder.cs`

**Current code at lines 262-282 (`BuildOnCallDelegateType`):**
```csharp
public static string BuildOnCallDelegateType(
    string methodName,
    MethodSignatureInfo sig,
    string ownerClassName,
    string ownerTypeParameters)
{
    if (NeedsCustomDelegate(sig))
    {
        return $"{methodName}Delegate?";
    }

    var ownerWithParams = string.IsNullOrEmpty(ownerTypeParameters)
        ? ownerClassName
        : $"{ownerClassName}{ownerTypeParameters}";

    if (sig.Parameters.Count == 0)
        return $"global::System.Action<{ownerWithParams}>?";  // <-- Remove ownerWithParams

    var paramTypes = string.Join(", ", sig.Parameters.Select(p => p.Type));
    return $"global::System.Action<{ownerWithParams}, {paramTypes}>?";  // <-- Remove ownerWithParams
}
```

**Change to:**
```csharp
public static string BuildOnCallDelegateType(
    string methodName,
    MethodSignatureInfo sig,
    string ownerClassName,
    string ownerTypeParameters)
{
    if (NeedsCustomDelegate(sig))
    {
        return $"{methodName}Delegate?";
    }

    if (sig.Parameters.Count == 0)
        return "global::System.Action?";

    var paramTypes = string.Join(", ", sig.Parameters.Select(p => p.Type));
    return $"global::System.Action<{paramTypes}>?";
}
```

**Current code at lines 287-306 (`BuildCustomDelegateSignature`):**
```csharp
public static string? BuildCustomDelegateSignature(...)
{
    ...
    var delegateParamList = BuildDelegateParamList(ownerWithParams, sig.Parameters);
    ...
}
```

**And lines 308-316 (`BuildDelegateParamList`):**
```csharp
private static string BuildDelegateParamList(string ownerClassName, EquatableArray<ParameterModel> parameters)
{
    var parts = new List<string> { $"{ownerClassName} ko" };  // <-- Remove this line
    foreach (var p in parameters)
    {
        parts.Add($"{p.RefPrefix}{p.Type} {p.EscapedName}");
    }
    return string.Join(", ", parts);
}
```

**Change to:**
```csharp
private static string BuildDelegateParamList(EquatableArray<ParameterModel> parameters)
{
    var parts = new List<string>();
    foreach (var p in parameters)
    {
        parts.Add($"{p.RefPrefix}{p.Type} {p.EscapedName}");
    }
    return string.Join(", ", parts);
}
```

Also update call sites that pass `ownerWithParams` to `BuildDelegateParamList`.

#### Step 1.2: Update overload delegate generation

In `BuildOverloadSignature` (lines 112-143), update the delegate signature generation:

**Current:**
```csharp
var delegateParamList = BuildDelegateParamList(ownerWithParams, sig.Parameters);
```

**Change to:**
```csharp
var delegateParamList = BuildDelegateParamList(sig.Parameters);
```

#### Step 1.3: Update `InlineModelBuilder.cs`

**File:** `src/Generator/Builder/InlineModelBuilder.cs`

**1. Update indexer `OnCallArgs` (line 762):**

**Current:**
```csharp
OnCallArgs: $"this, {argList}",
```

**Change to:**
```csharp
OnCallArgs: argList,
```

**2. Update generic method `OnCallArgs` (line 934):**

**Current:**
```csharp
OnCallArgs: member.Parameters.Count > 0 ? $"this, {argList}" : "this",
```

**Change to:**
```csharp
OnCallArgs: argList,
```

---

### Phase 2: Generator Changes (Renderer - Methods)

#### Step 2.1: Update `MethodInterceptorRenderer.cs`

**File:** `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs`

**1. Update `BuildInvokeParams` (lines 891-900):**

**Current:**
```csharp
private static string BuildInvokeParams(string ownerClassName, EquatableArray<ParameterModel> parameters, bool includeStrict)
{
    var parts = new List<string> { $"{ownerClassName} ko" };  // <-- Remove ko
    if (includeStrict)
        parts.Add("bool strict");
    foreach (var p in parameters)
    {
        parts.Add($"{p.RefPrefix}{p.Type} {p.EscapedName}");
    }
    return string.Join(", ", parts);
}
```

**Change to:**
```csharp
private static string BuildInvokeParams(EquatableArray<ParameterModel> parameters, bool includeStrict)
{
    var parts = new List<string>();
    if (includeStrict)
        parts.Add("bool strict");
    foreach (var p in parameters)
    {
        parts.Add($"{p.RefPrefix}{p.Type} {p.EscapedName}");
    }
    return string.Join(", ", parts);
}
```

**2. Update `BuildCallbackArgs` (lines 903-911):**

**Current:**
```csharp
private static string BuildCallbackArgs(EquatableArray<ParameterModel> parameters)
{
    var parts = new List<string> { "ko" };  // <-- Remove ko
    foreach (var p in parameters)
    {
        parts.Add($"{p.RefPrefix}{p.EscapedName}");
    }
    return string.Join(", ", parts);
}
```

**Change to:**
```csharp
private static string BuildCallbackArgs(EquatableArray<ParameterModel> parameters)
{
    var parts = new List<string>();
    foreach (var p in parameters)
    {
        parts.Add($"{p.RefPrefix}{p.EscapedName}");
    }
    return string.Join(", ", parts);
}
```

**3. Update call sites that pass `ownerWithParams` to `BuildInvokeParams`:**

- Line 279: `RenderInvokeMethod` - remove `ownerWithParams` argument
- Line 392: `RenderOverloadInvokeMethod` - remove `ownerWithParams` argument

---

### Phase 3: Generator Changes (Renderer - Properties)

#### Step 3.1: Update `FlatRenderer.cs`

**File:** `src/Generator/Renderer/FlatRenderer.cs`

**1. Update property OnGet delegate type (around line 407):**

**Current:**
```csharp
w.Line($"public global::System.Func<{className}, {prop.ReturnType}>? OnGet {{ get; set; }}");
```

**Change to:**
```csharp
w.Line($"public global::System.Func<{prop.ReturnType}>? OnGet {{ get; set; }}");
```

**2. Update property OnSet delegate type (around line 422):**

**Current:**
```csharp
w.Line($"public global::System.Action<{className}, {prop.ReturnType}>? OnSet {{ get; set; }}");
```

**Change to:**
```csharp
w.Line($"public global::System.Action<{prop.ReturnType}>? OnSet {{ get; set; }}");
```

**3. Update property implementation (OnGet callback invocation, line 2333):**

**Current:**
```csharp
w.Line($"get {{ {prop.InterceptorName}.RecordGet(); if ({prop.InterceptorName}.OnGet is {{ }} onGet) return onGet(this); ...
```

**Change to:**
```csharp
w.Line($"get {{ {prop.InterceptorName}.RecordGet(); if ({prop.InterceptorName}.OnGet is {{ }} onGet) return onGet(); ...
```

**4. Update property implementation (OnSet callback invocation, line 2340):**

**Current:**
```csharp
w.Line($"set {{ {prop.InterceptorName}.RecordSet(value); if ({prop.InterceptorName}.OnSet is {{ }} onSet) {{ onSet(this, value); return; }} ...
```

**Change to:**
```csharp
w.Line($"set {{ {prop.InterceptorName}.RecordSet(value); if ({prop.InterceptorName}.OnSet is {{ }} onSet) {{ onSet(value); return; }} ...
```

**5. Update indexer OnGet delegate type (line 663):**

**Current:**
```csharp
w.Line($"public global::System.Func<{className}, {indexer.KeyType}, {indexer.ReturnType}>? OnGet {{ get; set; }}");
```

**Change to:**
```csharp
w.Line($"public global::System.Func<{indexer.KeyType}, {indexer.ReturnType}>? OnGet {{ get; set; }}");
```

**6. Update indexer OnSet delegate type (line 678):**

**Current:**
```csharp
w.Line($"public global::System.Action<{className}, {indexer.KeyType}, {indexer.ReturnType}>? OnSet {{ get; set; }}");
```

**Change to:**
```csharp
w.Line($"public global::System.Action<{indexer.KeyType}, {indexer.ReturnType}>? OnSet {{ get; set; }}");
```

**7. Update indexer implementation (OnGet callback invocation, line 2389):**

**Current:**
```csharp
w.Line($"get {{ {accessExpr}.RecordGet({indexer.KeyParamName}); if ({accessExpr}.OnGet is {{ }} onGet) return onGet(this, {indexer.KeyParamName}); ...
```

**Change to:**
```csharp
w.Line($"get {{ {accessExpr}.RecordGet({indexer.KeyParamName}); if ({accessExpr}.OnGet is {{ }} onGet) return onGet({indexer.KeyParamName}); ...
```

**8. Update indexer implementation (OnSet callback invocation, line 2394):**

**Current:**
```csharp
w.Line($"set {{ {accessExpr}.RecordSet({indexer.KeyParamName}, value); if ({accessExpr}.OnSet is {{ }} onSet) {{ onSet(this, {indexer.KeyParamName}, value); return; }} ...
```

**Change to:**
```csharp
w.Line($"set {{ {accessExpr}.RecordSet({indexer.KeyParamName}, value); if ({accessExpr}.OnSet is {{ }} onSet) {{ onSet({indexer.KeyParamName}, value); return; }} ...
```

#### Step 3.2: Update `InlineRenderer.cs`

**File:** `src/Generator/Renderer/InlineRenderer.cs`

Apply the same changes as FlatRenderer:

**1. Property interceptor class (around lines 271-284):**

**Current:**
```csharp
w.Line($"public global::System.Func<{prop.StubClassName}, {prop.ReturnType}>? OnGet {{ get; set; }}");
...
w.Line($"public global::System.Action<{prop.StubClassName}, {prop.ReturnType}>? OnSet {{ get; set; }}");
```

**Change to:**
```csharp
w.Line($"public global::System.Func<{prop.ReturnType}>? OnGet {{ get; set; }}");
...
w.Line($"public global::System.Action<{prop.ReturnType}>? OnSet {{ get; set; }}");
```

**2. Indexer interceptor class (around lines 447-467):**

**Current:**
```csharp
w.Line($"public global::System.Func<{indexer.StubClassName}, {indexer.ParameterTypes}, {indexer.ReturnType}>? OnGet");
...
w.Line($"public global::System.Action<{indexer.StubClassName}, {indexer.ParameterTypes}, {indexer.ReturnType}>? OnSet");
```

**Change to:**
```csharp
w.Line($"public global::System.Func<{indexer.ParameterTypes}, {indexer.ReturnType}>? OnGet");
...
w.Line($"public global::System.Action<{indexer.ParameterTypes}, {indexer.ReturnType}>? OnSet");
```

**3. Update property/indexer implementation invocations:**

Change `onGet(this)` to `onGet()` and `onSet(this, value)` to `onSet(value)`.

---

### Phase 4: Generator Changes (Renderer - Delegate Stubs)

#### Step 4.1: Update `InlineModelBuilder.cs` OnCallType construction

**File:** `src/Generator/Builder/InlineModelBuilder.cs`

The `OnCallType` for delegate stubs is constructed at lines 1080-1092. This includes the stub class reference as the first type parameter.

**Current (lines 1080-1092):**
```csharp
string onCallType;
if (del.IsVoid)
{
    onCallType = del.Parameters.Count == 0
        ? $"global::System.Action<{stubClassRef}>"
        : $"global::System.Action<{stubClassRef}, {string.Join(", ", del.Parameters.Select(p => p.Type))}>";
}
else
{
    onCallType = del.Parameters.Count == 0
        ? $"global::System.Func<{stubClassRef}, {del.ReturnType}>"
        : $"global::System.Func<{stubClassRef}, {string.Join(", ", del.Parameters.Select(p => p.Type))}, {del.ReturnType}>";
}
```

**Change to:**
```csharp
string onCallType;
if (del.IsVoid)
{
    onCallType = del.Parameters.Count == 0
        ? "global::System.Action"
        : $"global::System.Action<{string.Join(", ", del.Parameters.Select(p => p.Type))}>";
}
else
{
    onCallType = del.Parameters.Count == 0
        ? $"global::System.Func<{del.ReturnType}>"
        : $"global::System.Func<{string.Join(", ", del.Parameters.Select(p => p.Type))}, {del.ReturnType}>";
}
```

#### Step 4.2: Update `InlineRenderer.cs` delegate stub Invoke

**File:** `src/Generator/Renderer/InlineRenderer.cs`

**1. Update void delegate Invoke (lines 1270-1271):**

**Current:**
```csharp
var onCallArgs = del.Parameters.Count > 0 ? $"this, {del.InvokeArgumentList}" : "this";
w.Line($"\t\t\t\tif (Interceptor.OnCall is {{ }} onCall) onCall({onCallArgs});");
```

**Change to:**
```csharp
var onCallArgs = del.InvokeArgumentList;
w.Line($"\t\t\t\tif (Interceptor.OnCall is {{ }} onCall) onCall({onCallArgs});");
```

**2. Update non-void delegate Invoke (lines 1275-1276):**

**Current:**
```csharp
var onCallArgs = del.Parameters.Count > 0 ? $"this, {del.InvokeArgumentList}" : "this";
w.Line($"\t\t\t\tif (Interceptor.OnCall is {{ }} onCall) return onCall({onCallArgs});");
```

**Change to:**
```csharp
var onCallArgs = del.InvokeArgumentList;
w.Line($"\t\t\t\tif (Interceptor.OnCall is {{ }} onCall) return onCall({onCallArgs});");
```

Note: For zero-parameter delegates, `onCallArgs` will be empty string, resulting in `onCall()`.

---

### Phase 5: Generator Changes (Renderer - Generic Methods)

#### Step 5.1: Update generic method handler delegate signatures

**File:** `src/Generator/Renderer/InlineRenderer.cs` and `src/Generator/Renderer/FlatRenderer.cs`

Update the generic method handler delegate signatures to remove the stub parameter from OnCall delegates in:
- `RenderTypedHandlerClass`
- `RenderGenericMethodHandler`

---

### Phase 6: Generator Changes (Legacy FlatRenderer Patterns)

#### Step 6.1: Update `RenderInvokeMethod` in FlatRenderer.cs

**File:** `src/Generator/Renderer/FlatRenderer.cs`

The legacy `RenderInvokeMethod` (lines 951-1017) uses a different pattern than the unified interceptor and needs separate updates.

**1. Update invoke parameter list (lines 954-956):**

**Current:**
```csharp
var invokeParams = method.Parameters.Count > 0
    ? $"{className} ko, bool strict, " + string.Join(", ", method.Parameters.Select(p => $"{p.RefPrefix}{p.Type} {p.EscapedName}"))
    : $"{className} ko, bool strict";
```

**Change to:**
```csharp
var invokeParams = method.Parameters.Count > 0
    ? "bool strict, " + string.Join(", ", method.Parameters.Select(p => $"{p.RefPrefix}{p.Type} {p.EscapedName}"))
    : "bool strict";
```

**2. Update callback args (lines 1007-1009):**

**Current:**
```csharp
var callbackArgs = method.Parameters.Count > 0
    ? "ko, " + string.Join(", ", method.Parameters.Select(p => $"{p.RefPrefix}{p.EscapedName}"))
    : "ko";
```

**Change to:**
```csharp
var callbackArgs = method.Parameters.Count > 0
    ? string.Join(", ", method.Parameters.Select(p => $"{p.RefPrefix}{p.EscapedName}"))
    : "";
```

**3. Update callback invocations (lines 1011-1014):**

**Current:**
```csharp
if (method.IsVoid)
    w.Line($"callback({callbackArgs});");
else
    w.Line($"return callback({callbackArgs});");
```

No change needed here - the `callbackArgs` variable will be correctly empty or contain just the method parameters.

---

### Phase 7: Update Implementation Invocation Sites

#### Step 7.1: Update FlatRenderer implementations

**File:** `src/Generator/Renderer/FlatRenderer.cs`

Update all explicit interface implementation methods that call interceptor Invoke methods to remove the `this` argument:

**Property implementations (already covered in Phase 3):**
- Change `onGet(this)` to `onGet()`
- Change `onSet(this, value)` to `onSet(value)`

**Method implementations:**
- Change `Invoke(this, ...)` to `Invoke(...)`
- Change `Invoke_Suffix(this, ...)` to `Invoke_Suffix(...)`

#### Step 7.2: Update InlineRenderer implementations

**File:** `src/Generator/Renderer/InlineRenderer.cs`

Same changes as FlatRenderer for:
- `RenderPropertyImplementation`
- `RenderIndexerImplementation`
- `RenderNonGenericMethodImplementation`
- `RenderGenericMethodImplementation`

---

### Phase 8: Test Updates

#### Step 8.1: Find all test files using ko parameter

Run grep to find all test usages:
```bash
grep -r "(ko[,)]" src/Tests/
```

#### Step 8.2: Update test callback signatures

Update all test files that use OnCall, Get, Set callbacks to remove the `ko` parameter:

**Before:**
```csharp
stub.Method.OnCall((arg1, arg2) => { });
stub.Property.OnGet = () => value;
stub.Property.OnSet = ((val) => { };
```

**After:**
```csharp
stub.Method.OnCall((arg1, arg2) => { });
stub.Property.OnGet = () => value;
stub.Property.OnSet = (val) => { };
```

---

### Phase 9: Documentation Updates

#### Step 9.1: Update reference documentation

**Files:**
- `docs/reference/interceptor-api.md`
- `docs/reference/attribute-options.md`

Update all code examples showing callback signatures.

#### Step 9.2: Update guide documentation

**Files:**
- `docs/getting-started.md`
- `docs/guides/methods.md`
- `docs/guides/properties.md`
- `docs/guides/advanced-callbacks.md`
- `docs/guides/async-patterns.md`
- `docs/guides/generic-methods.md`
- `docs/guides/source-delegation.md`
- `docs/guides/stub-patterns.md`
- `docs/guides/verification.md`

#### Step 9.3: Update migration guide

**File:** `docs/migration/from-moq.md`

Update all callback examples.

#### Step 9.4: Update README

**File:** `README.md`

Update any callback examples in the main README.

---

## Acceptance Criteria

- [ ] All delegate signatures no longer include `ko` parameter
- [ ] All Invoke methods no longer take `ko` as first parameter
- [ ] All Get callbacks are `Func<TReturn>` (no stub param)
- [ ] All Set callbacks are `Action<TValue>` (no stub param)
- [ ] All indexer Get callbacks are `Func<TKey, TReturn>` (no stub param)
- [ ] All indexer Set callbacks are `Action<TKey, TValue>` (no stub param)
- [ ] All delegate stub OnCall callbacks remove stub param
- [ ] InlineModelBuilder `OnCallArgs` no longer includes `this,` prefix
- [ ] Legacy FlatRenderer `RenderInvokeMethod` updated
- [ ] All test files updated with new callback signatures
- [ ] All documentation updated with new callback signatures
- [ ] All existing tests pass
- [ ] Works for all 3 patterns: Stand-Alone, Inline Interface, Inline Class
- [ ] Works for delegate stubs (`[KnockOff<Func<...>>]`)
- [ ] Generated code compiles without warnings

---

## Dependencies

None - this is a self-contained API simplification change.

---

## Risks / Considerations

### Breaking Change

This is a **breaking change** that affects all existing KnockOff users who have written OnCall, Get, or Set callbacks. Every callback will need to have its first parameter removed.

**Mitigation:**
- Compile-time errors will guide users to fix callbacks (delegate signature mismatch)
- Error messages will be clear: "cannot convert lambda expression"
- Add a "Migration" section to release notes

### Loss of Stub Access in Callbacks

Users who genuinely need stub access within a callback will need to use closure:

**Before:**
```csharp
stub.Method.OnCall((id) => ko.OtherMethod.CallCount > 0 ? value1 : value2);
```

**After:**
```csharp
stub.Method.OnCall((id) => stub.OtherMethod.CallCount > 0 ? value1 : value2);
```

This is actually **more explicit** and **clearer** since `stub` is a known local variable.

### Indexer Signature Impact

Indexers have more complex signatures. After the change:
- `Get` becomes `Func<TKey, TReturn>`
- `Set` becomes `Action<TKey, TValue>`

These are still standard delegate types that are easy to understand.

### Generated Code Readability

The generated code becomes simpler and more standard:
- Delegates use standard `Func<>` and `Action<>` where possible
- Method invocations have fewer parameters
- Custom delegates have cleaner signatures

---

## Migration Guide Content

Add to release notes:

### Breaking Change: Callback Signatures Simplified

The `ko` (stub instance) parameter has been removed from all callback signatures. This simplifies callback definitions since you already have access to the stub through local variables.

**OnCall for methods:**
```csharp
// Before (v1.x)
stub.GetUser.OnCall((id) => new User { Id = id });

// After (v2.0)
stub.GetUser.OnCall((id) => new User { Id = id });
```

**Get for properties:**
```csharp
// Before (v1.x)
stub.IsActive.Get((ko) => true);

// After (v2.0)
stub.IsActive.Get(() => true);
```

**Set for properties:**
```csharp
// Before (v1.x)
stub.Name.Set((ko, value) => Console.WriteLine(value));

// After (v2.0)
stub.Name.Set((value) => Console.WriteLine(value));
```

**Accessing stub within callbacks:**

If you need to access the stub instance within a callback, use closure:

```csharp
var stub = new MyServiceStub();
stub.GetData.OnCall((key) => {
    // Access stub via closure
    return stub.Cache.WasCalled ? cachedValue : fetchedValue;
});
```
