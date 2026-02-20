# Fix: Stub Override Splits Overload Groups in Flat Pipeline

**Date:** 2026-02-19
**Related Todo:** [Stub Override Overload Split Bug](../todos/stub-override-overload-split-bug.md)
**Status:** Awaiting Verification
**Last Updated:** 2026-02-19

---

## Overview

When a user provides a stub override for a subset of an overloaded method's signatures (partial overload coverage), the Flat pipeline splits the overload group into two separate interceptors. This produces `stub.Format` (1 overload, with stub override) and `stub.Format2` (2 overloads, without stub override) instead of a single `stub.Format` with all 3 overloads and per-signature stub override fallback.

The StandaloneClass pipeline already handles this correctly. The fix is to make the Flat pipeline follow the same approach: keep all overloads in a single group and track stub override status per-signature.

---

## Root Cause Analysis

### Pipeline Trace

**Predicate:** PASS -- candidates detected correctly.

**Transform:** PASS -- `HasStubOverride` is correctly set per method in the transform.

**Builder:** FAIL -- `FlatModelBuilder.Build()` at lines 47-69 splits methods by `HasStubOverride`:

```csharp
// Line 49-57: flatMethodGroups EXCLUDES HasStubOverride methods
var flatMethodGroups = methods
    .Where(m => !m.IsGenericMethod && !m.HasStubOverride)
    .GroupBy(m => m.InterceptorName)
    ...

// Line 61-69: flatStubOverrideGroups INCLUDES only HasStubOverride methods
var flatStubOverrideGroups = methods
    .Where(m => !m.IsGenericMethod && m.HasStubOverride)
    .GroupBy(m => m.InterceptorName)
    ...
```

Additionally, the name map builder (`AssignNamesForOverloadGroup` at lines 261-305) splits overloads with and without stub overrides into DIFFERENT interceptor names:

```csharp
if (withStubOverride.Count > 0 && withoutStubOverride.Count > 0)
{
    // Overloads WITH stub overrides use one interceptor name ("Format")
    var stubOverrideName = GetUniqueInterceptorName(methodName, usedNames);
    ...
    // Overloads WITHOUT stub overrides use a DIFFERENT name ("Format2")
    var regularName = GetUniqueInterceptorName(methodName, usedNames);
    ...
}
```

And the overload counting logic (lines 757-760) also excludes `HasStubOverride` methods:

```csharp
var interceptorNameCounts = methods
    .Where(m => !m.IsGenericMethod && !m.HasStubOverride)
    .GroupBy(m => m.InterceptorName)
    .ToDictionary(g => g.Key, g => g.Count());
```

**Renderer:** N/A -- renderer faithfully renders what the builder gives it.

### Observed Output (Partial Overload Coverage)

For `PartialOverloadStubOverrideStub` implementing `IOverloadedStubOverrideService` with only `Format_(string)` overridden:

**Generated:**
- `FormatInterceptor` -- single-signature interceptor for `Format(string)` only (with stub override fallback)
- `Format2Interceptor` -- multi-overload interceptor for `Format(string, bool)` and `Format(string, bool, int)` (no stub override)
- `stub.Format` property (FormatInterceptor)
- `stub.Format2` property (Format2Interceptor)

**Expected:**
- Single `FormatInterceptor` with 3 overloads, per-signature stub override on `Format(string)` only
- `stub.Format` property (single interceptor for all overloads)

### Why the StandaloneClass Pipeline Is Correct

`StandaloneClassModelBuilder` (lines 103-192) groups ALL methods by name regardless of stub override status. Each overload within a group carries its own `StubOverrideName` via `MethodSignatureInfo`:

```csharp
// Line 160-167: Per-signature stub override tracking
var signatures = group.Members
    .Select(m =>
    {
        var sig = ToMethodSignatureInfo(m);
        var hasStubOverride = stubOverrideMethods.Contains(
            SymbolHelpers.BuildOverrideSignatureKey(m.Name, m.Parameters));
        return hasStubOverride ? sig with { StubOverrideName = $"__StubOverride_{m.Name}" } : sig;
    })
    .ToList();
```

Then `UnifiedInterceptorBuilder.BuildMethodInterceptor` creates a single interceptor with per-signature `StubOverrideName` in each `MethodOverloadSignature`. The renderer uses this per-signature information to wire stub override fallback only for the signatures that have it.

---

## Design

### Approach: Unify All Overloads in a Single Group (Match StandaloneClass Pattern)

The fix aligns `FlatModelBuilder` with the approach already proven in `StandaloneClassModelBuilder`:

1. **Name map**: All overloads of the same method share ONE interceptor name, regardless of stub override status.
2. **Grouping**: All overloads go into `MethodGroups` (currently `flatMethodGroups`). The separate `StubOverrideGroups` concept is eliminated for method overloads.
3. **Per-signature tracking**: `HasStubOverride` remains on each `FlatMethodModel` so the renderer/adapter can set per-signature `StubOverrideName` in the `MethodOverloadSignature`.
4. **Overload counting**: Include all non-generic methods (remove the `!m.HasStubOverride` filter).

### Critical Constraint: Non-Overloaded Stub Override Methods

When a method has NO overloads and HAS a stub override, it currently goes into `StubOverrideGroups` alone. This is also correct to unify: a single-signature method with a stub override should be in `MethodGroups` with `HasStubOverride = true`. The `ModelAdapters.ToUnifiedModel` already handles this -- `BuildSingleSignatureModel` checks `first.HasStubOverride` and sets `StubOverrideName` accordingly (line 120).

### What Changes

#### 1. `FlatModelBuilder.AssignNamesForOverloadGroup` (lines 261-305)

**Remove the split.** All overloads get the same interceptor name regardless of stub override status.

**Before:**
```csharp
if (withStubOverride.Count > 0 && withoutStubOverride.Count > 0)
{
    // Split into two names
    var stubOverrideName = GetUniqueInterceptorName(methodName, usedNames);
    var regularName = GetUniqueInterceptorName(methodName, usedNames);
}
```

**After:**
```csharp
// All overloads share a single interceptor name
var finalName = GetUniqueInterceptorName(methodName, usedNames);
usedNames.Add(finalName);
foreach (var overload in overloads)
{
    var key = GetMemberKey(overload);
    nameMap[key] = finalName;
}
```

The entire method simplifies to the existing "all same" branch (lines 294-304).

#### 2. `FlatModelBuilder.Build` -- Grouping (lines 47-69)

**Merge into a single group set.** Remove the separate `flatStubOverrideGroups`. All non-generic methods go into `flatMethodGroups`.

**Before:**
```csharp
var flatMethodGroups = methods
    .Where(m => !m.IsGenericMethod && !m.HasStubOverride)
    .GroupBy(m => m.InterceptorName)
    ...

var flatStubOverrideGroups = methods
    .Where(m => !m.IsGenericMethod && m.HasStubOverride)
    .GroupBy(m => m.InterceptorName)
    ...
```

**After:**
```csharp
var flatMethodGroups = methods
    .Where(m => !m.IsGenericMethod)
    .GroupBy(m => m.InterceptorName)
    ...

// flatStubOverrideGroups removed entirely
```

#### 3. `FlatModelBuilder.Build` -- Overload Counting (lines 757-760)

**Remove the `!m.HasStubOverride` filter.**

**Before:**
```csharp
var interceptorNameCounts = methods
    .Where(m => !m.IsGenericMethod && !m.HasStubOverride)
    ...
```

**After:**
```csharp
var interceptorNameCounts = methods
    .Where(m => !m.IsGenericMethod)
    ...
```

#### 4. `FlatGenerationUnit` Model

**Remove `StubOverrideGroups` field.** It becomes empty/unused. To avoid breaking the record signature during implementation, the developer can either:
- (A) Remove the field entirely and update all references, or
- (B) Keep the field but always pass `EquatableArray<FlatMethodGroup>.Empty` and remove downstream usage.

Option (A) is cleaner and preferred.

#### 5. `ModelAdapters.ToUnifiedModel` for `FlatMethodGroup`

This already handles `HasStubOverride` correctly per-method in both `BuildSingleSignatureModel` (line 120) and `BuildMultiOverloadModel` (lines 197-198, 216, 239). No changes needed here.

#### 6. `FlatRenderer` -- Remove `StubOverrideGroups` Usage

All places in `FlatRenderer.cs` that iterate over `unit.StubOverrideGroups` must be updated to rely on the unified `MethodGroups`. The key touch points:

- **Line 143-156**: Rendering stub override interceptor classes. Now handled by `MethodGroups` -- the `ModelAdapters.ToUnifiedModel` already detects `HasStubOverride` and passes appropriate options. The renderer must set `StubOverrideFallback = true` and `StubTypeName` when any method in the group has `HasStubOverride = true`.

- **Line 196-199**: `multiOverloadStubOverrideInterceptors` set. **Remove entirely.** After unification, stub override methods live in the same `MethodGroups` as regular methods, so `multiOverloadInterceptors` (line 190-193) already covers them. Pass `multiOverloadInterceptors` to `RenderStubOverrideImplementation` instead. See "Change 8" below for the signature update.

- **Line 1592-1593**: Verify methods check. Remove `StubOverrideGroups.Count > 0` -- these methods are now in `MethodGroups`.

- **Line 1616-1619**: Stub override interceptor names for Verify. Remove -- these are now part of the `methodInterceptorNames` from `MethodGroups`.

- **Line 1946-1958**: Fallback wiring for stub override pre-compiled interceptors. Move this logic into the main method groups loop.

- **Line 2201-2204**: `RenderStubOverrideImplementation` check. This stays, but the method is now called from the unified method implementation renderer. The `HasStubOverride` flag on each `FlatMethodModel` still controls whether `RenderStubOverrideImplementation` is used for that specific method.

#### 7. `FlatRenderer.RenderInterceptorClass` for Mixed Groups

When rendering interceptor classes from `MethodGroups`, the renderer must detect whether ANY method in the group has `HasStubOverride = true`. If so, render the interceptor with `StubOverrideFallback: true` and `StubTypeName` set. This is the key change in the renderer.

**Before (line 129-140):**
```csharp
foreach (var group in unit.MethodGroups)
{
    // Always renders without stub override fallback
    var options = new InterceptorRenderOptions(
        BaseIndent: 0,
        IncludeStrictParameter: true,
        StrictAccessExpression: "strict");
    MethodInterceptorRenderer.RenderInterceptorClass(w, unifiedModel, options);
}
```

**After:**
```csharp
foreach (var group in unit.MethodGroups)
{
    var hasAnyStubOverride = group.Methods.Any(m => m.HasStubOverride);
    var options = new InterceptorRenderOptions(
        BaseIndent: 0,
        IncludeStrictParameter: true,
        StrictAccessExpression: "strict",
        StubOverrideFallback: hasAnyStubOverride,
        StubTypeName: hasAnyStubOverride ? classNameWithTypeParams : null);
    MethodInterceptorRenderer.RenderInterceptorClass(w, unifiedModel, options);
}
```

#### 8. `FlatRenderer.RenderMethodImplementation` and `RenderStubOverrideImplementation` -- Replace `multiOverloadStubOverrideInterceptors` with `multiOverloadInterceptors`

The method implementation already checks `method.HasStubOverride` and calls `RenderStubOverrideImplementation` (line 2201). This handles per-method routing correctly.

**The key insight from developer review:** After removing `StubOverrideGroups`, the `multiOverloadStubOverrideInterceptors` set (computed at lines 196-199 from `unit.StubOverrideGroups`) becomes empty. But `RenderStubOverrideImplementation` (line 2239) uses this set to determine whether `Invoke` needs a suffix. The solution is straightforward: **remove `multiOverloadStubOverrideInterceptors` entirely** and pass `multiOverloadInterceptors` instead.

After unification, stub override methods and regular methods share the same `MethodGroups`. The `multiOverloadInterceptors` set (lines 190-193) is already computed from `unit.MethodGroups` and correctly identifies interceptors that need suffixed `Invoke` calls. A stub override method in a multi-overload group needs the same suffix logic as a regular method in the same group.

**Changes:**

1. **Remove** the `multiOverloadStubOverrideInterceptors` variable entirely (lines 196-199).

2. **Update `RenderMethodImplementation` signature** (line 2177) -- remove the `multiOverloadStubOverrideInterceptors` parameter:

   **Before:**
   ```csharp
   private static void RenderMethodImplementation(
       CodeWriter w,
       FlatMethodModel method,
       HashSet<string> multiOverloadInterceptors,
       HashSet<string> multiOverloadStubOverrideInterceptors,
       HashSet<string> multiOverloadGenericStubOverrideInterceptors,
       EquatableArray<FlatGenericMethodHandlerGroup> genericStubOverrideHandlerGroups,
       Dictionary<string, string> preCompiledInterceptors)
   ```

   **After:**
   ```csharp
   private static void RenderMethodImplementation(
       CodeWriter w,
       FlatMethodModel method,
       HashSet<string> multiOverloadInterceptors,
       HashSet<string> multiOverloadGenericStubOverrideInterceptors,
       EquatableArray<FlatGenericMethodHandlerGroup> genericStubOverrideHandlerGroups,
       Dictionary<string, string> preCompiledInterceptors)
   ```

3. **Update the call to `RenderStubOverrideImplementation`** (line 2203) -- pass `multiOverloadInterceptors`:

   **Before:**
   ```csharp
   if (method.HasStubOverride)
   {
       RenderStubOverrideImplementation(w, method, multiOverloadStubOverrideInterceptors);
       return;
   }
   ```

   **After:**
   ```csharp
   if (method.HasStubOverride)
   {
       RenderStubOverrideImplementation(w, method, multiOverloadInterceptors);
       return;
   }
   ```

4. **Update `RenderStubOverrideImplementation` signature** (line 2239) -- rename parameter for clarity:

   **Before:**
   ```csharp
   private static void RenderStubOverrideImplementation(CodeWriter w, FlatMethodModel method, HashSet<string> multiOverloadStubOverrideInterceptors)
   ```

   **After:**
   ```csharp
   private static void RenderStubOverrideImplementation(CodeWriter w, FlatMethodModel method, HashSet<string> multiOverloadInterceptors)
   ```

   The body already uses the correct pattern (`multiOverload*.Contains(method.InterceptorName)`), so the logic is unchanged -- only the variable name changes.

5. **Update the call site** at line 235 that passes both sets:

   **Before:**
   ```csharp
   RenderMethodImplementation(w, method, multiOverloadInterceptors, multiOverloadStubOverrideInterceptors, multiOverloadGenericStubOverrideInterceptors, unit.GenericStubOverrideHandlerGroups, preCompiledInterceptors);
   ```

   **After:**
   ```csharp
   RenderMethodImplementation(w, method, multiOverloadInterceptors, multiOverloadGenericStubOverrideInterceptors, unit.GenericStubOverrideHandlerGroups, preCompiledInterceptors);
   ```

**For the unified approach:**
- A method with `HasStubOverride` in a multi-overload group needs `Invoke_{suffix}` with the stub override `this` argument.
- A method without `HasStubOverride` in the same group needs regular `Invoke_{suffix}` without `this`.
- Both use the SAME `multiOverloadInterceptors` set to determine whether a suffix is needed.

---

## Affected Pipelines

| Pipeline | Has Bug | Fix Needed |
|---|---|---|
| Flat (Standalone Interface, patterns 1-2) | Yes | Fix `FlatModelBuilder` + `FlatRenderer` |
| StandaloneClass (patterns 3-4) | No | Already correct |
| Inline Interface (pattern 5) | No | No stub override support |
| Inline Class (pattern 6) | No | No stub override support |
| Inline Delegate (pattern 7) | No | No stub override support |
| Open Generic Interface (pattern 8) | No | No stub override support |
| Open Generic Class (pattern 9) | No | No stub override support |

---

## Architectural Verification

### Scope Table

| Pattern | Affected | Notes |
|---|---|---|
| Standalone (1) | Yes | Primary bug location |
| Generic Standalone (2) | Yes | Same pipeline |
| Standalone Class (3) | No | Already correct |
| Generic Standalone Class (4) | No | Already correct |
| Inline Interface (5) | No | No stub overrides |
| Inline Class (6) | No | No stub overrides |
| Inline Delegate (7) | No | No stub overrides |
| Open Generic Interface (8) | No | No stub overrides |
| Open Generic Class (9) | No | No stub overrides |

### Design Project Verification

- `PartialOverloadStubOverrideStub` in `src/Design/Design.Stubs/StubOverrides/StubOverrideBasics.cs` -- currently compiles but generates split interceptors (`Format` + `Format2`). After the fix, it must generate a single `Format` interceptor with 3 overloads and per-signature stub override on `Format(string)` only.
- `OverloadedStubOverrideStub` -- all overloads have stub overrides. Currently works correctly (single interceptor). Must continue to work after the fix.
- `BasicStubOverrideStub` -- no overloads. Must continue to work.
- `MixedStubOverrideStub` -- different methods with/without stub overrides (not overloads of the same method). Must continue to work.

### Breaking Changes

**No user-facing breaking changes.** The fix changes generated code (interceptor names, class structure), but:
- Users who had partial overload coverage were getting `Format` and `Format2` as separate interceptors. After the fix, they get a single `Format` with all overloads. This is a behavior fix, not a breaking change.
- Users who had ALL overloads covered by stub overrides see no change.
- Users who had NO stub overrides see no change.

### Codebase Analysis

Files examined:
- `src/Generator/Builder/FlatModelBuilder.cs` -- root cause (name splitting + group splitting)
- `src/Generator/Builder/StandaloneClassModelBuilder.cs` -- correct reference implementation
- `src/Generator/Builder/InlineModelBuilder.cs` -- confirmed no stub override support
- `src/Generator/Builder/ClassModelBuilder.cs` -- confirmed no stub override support
- `src/Generator/Renderer/FlatRenderer.cs` -- downstream consumer of split groups
- `src/Generator/Renderer/Shared/ModelAdapters.cs` -- adapter already handles per-method HasStubOverride
- `src/Generator/Renderer/StandaloneClassRenderer.cs` -- correct reference for stub override rendering
- `src/Generator/Model/Flat/FlatGenerationUnit.cs` -- model contains `StubOverrideGroups` field
- `src/Design/Design.Stubs/StubOverrides/StubOverrideBasics.cs` -- Design source of truth
- `src/Design/Design.Domain/Services/IStubOverrideService.cs` -- interface definitions
- `src/Tests/KnockOffTests/BaseClassStubOverrideTests.cs` -- in-scope tests using `stub.Format2` (buggy behavior)
- Generated code: `PartialOverloadStubOverrideStub.g.cs`, `OverloadedStubOverrideStub.g.cs`

---

## Implementation Steps

### Phase 1: Fix FlatModelBuilder Name Assignment

1. Simplify `AssignNamesForOverloadGroup` to always assign a single name to all overloads.
2. Remove the `withStubOverride` / `withoutStubOverride` split logic.
3. The method becomes a simple "assign one name to all overloads" regardless of stub override status.

### Phase 2: Fix FlatModelBuilder Grouping

1. Remove `flatStubOverrideGroups` computation (lines 61-69).
2. Change `flatMethodGroups` to include ALL non-generic methods (remove `!m.HasStubOverride` filter).
3. Remove `!m.HasStubOverride` from overload counting (lines 757-760).
4. Remove `StubOverrideGroups` from `FlatGenerationUnit` model.
5. Remove `GenericStubOverrideHandlerGroups` if now empty (it was already returning empty).

### Phase 3: Fix FlatRenderer

1. Remove all iterations over `unit.StubOverrideGroups`.
2. In the `MethodGroups` rendering loop, detect `HasStubOverride` on any method and set `StubOverrideFallback` option.
3. **Remove `multiOverloadStubOverrideInterceptors` entirely** (lines 196-199). Pass `multiOverloadInterceptors` to `RenderStubOverrideImplementation` instead. Update `RenderMethodImplementation` and `RenderStubOverrideImplementation` signatures to remove the old parameter.
4. Update Verify/VerifyAll methods to use only `MethodGroups`.
5. Update Source provider to use only `MethodGroups`.
6. Update constructor/fallback wiring to handle mixed groups.

### Phase 4: Update In-Scope Tests

The following tests in `src/Tests/KnockOffTests/BaseClassStubOverrideTests.cs` reference `stub.Format2`, which is the buggy split-interceptor behavior. These tests are **in-scope** (they test the exact feature being fixed) and must be updated to use the unified `stub.Format` interceptor:

1. **`Overload_OnCall_OnNonOverriddenOverload_Works`** (line ~465):
   - **Before:** `stub.Format2.Call((string input, bool uppercase) => uppercase ? input.ToUpper() : input);`
   - **After:** `stub.Format.Call((string input, bool uppercase) => uppercase ? input.ToUpper() : input);`
   - The two-parameter lambda `(string input, bool uppercase) =>` disambiguates to the `Format(string, bool)` overload via the Call API's overload resolution.
   - Update comments to remove references to "Format2" and "separate interceptor".

2. **`Overload_MixedConfiguration_EachOverloadIndependent`** (line ~480):
   - **Before:** `stub.Format2.Call((string input, bool uppercase) => "ONCALL:" + (uppercase ? input.ToUpper() : input));`
   - **After:** `stub.Format.Call((string input, bool uppercase) => "ONCALL:" + (uppercase ? input.ToUpper() : input));`
   - Same disambiguation approach. The test's assertions remain identical -- one overload uses stub override, the other uses Call.
   - Update comments to remove references to "Format2" and "each overload now has its own interceptor".

3. **`Overload_NoStubOverride_StrictModeThrows`** (line ~499):
   - No API changes needed -- this test does not reference `stub.Format2`.
   - The test logic remains valid: `Format("hello")` uses stub override (does not throw), `Format("hello", true)` has no stub override and should throw in strict mode.
   - Update comment on line 502 (typo: missing `//`) if noticed.

4. **`Overload_OnCall_SupersedesStubOverride`** (line ~449):
   - Currently uses `stub.Format.Call(input => "ONCALL:" + input)` -- this already works with the unified interceptor since the single-parameter lambda disambiguates to `Format(string)`.
   - No changes needed.

5. **`Overload_NoStubOverride_ThrowsWithoutOnCall`** (line ~435):
   - Currently calls `service.Format("hello", true)` directly without configuring an interceptor.
   - No changes needed -- the behavior is the same with the unified interceptor.

### Phase 4b: Add 3-Overload Partial Stub Override Regression Test

The existing tests in `BaseClassStubOverrideTests.cs` use a 2-overload interface (`IOverloadedStubOverrideService` defined locally with `Format(string)` and `Format(string, bool)`). The Design.Stubs `PartialOverloadStubOverrideStub` uses the 3-overload `IOverloadedStubOverrideService` from Design.Domain (with `Format(string)`, `Format(string, bool)`, `Format(string, bool, int)`), but no compiled test exercises this 3-overload partial stub override scenario.

**New interface and stub types** (add to test supporting types region):

```csharp
/// <summary>Interface with 3 overloaded Format methods for 3-overload partial stub override regression test.</summary>
public interface IThreeOverloadStubOverrideService
{
    string Format(string input);
    string Format(string input, bool uppercase);
    string Format(string input, bool uppercase, int maxLength);
}

/// <summary>Stub with partial stub override on only 1 of 3 overloads.</summary>
[KnockOff]
public partial class ThreeOverloadPartialStubOverrideStub : IThreeOverloadStubOverrideService
{
}

public partial class ThreeOverloadPartialStubOverrideStub
{
    // Stub override for the first overload ONLY
    protected override string Format_(string input)
    {
        return "USER:" + input;
    }

    // NO override for Format_(string input, bool uppercase)
    // NO override for Format_(string input, bool uppercase, int maxLength)
}
```

**New test methods** (add to region 7 - OverloadedStubOverrideTests, or a new region 8):

1. **`ThreeOverload_SingleInterceptor_AllOverloadsAccessible`**:
   Verifies all 3 overloads are accessible via a single `stub.Format` interceptor (no `stub.Format2` or `stub.Format3`). The overridden overload uses the stub override fallback; the two non-overridden overloads use the regular interceptor path.

   ```csharp
   [Fact]
   public void ThreeOverload_SingleInterceptor_AllOverloadsAccessible()
   {
       // Arrange - stub override on Format(string) only
       var stub = new ThreeOverloadPartialStubOverrideStub();
       IThreeOverloadStubOverrideService service = stub;

       // Act - call the overridden overload (uses stub override)
       var result = service.Format("hello");

       // Assert - stub override was called
       Assert.Equal("USER:hello", result);

       // Assert - all 3 overloads are tracked through a SINGLE interceptor
       stub.Format.Verify(Called.Once);
   }
   ```

2. **`ThreeOverload_NonOverriddenOverloads_ThrowWithoutCall`**:
   Verifies the two non-overridden overloads follow the regular interceptor path (throw `InvalidOperationException` when no Call/Return configured).

   ```csharp
   [Fact]
   public void ThreeOverload_NonOverriddenOverloads_ThrowWithoutCall()
   {
       // Arrange
       var stub = new ThreeOverloadPartialStubOverrideStub();
       IThreeOverloadStubOverrideService service = stub;

       // Act & Assert - non-overridden 2-param overload throws
       Assert.Throws<InvalidOperationException>(() => service.Format("hello", true));

       // Act & Assert - non-overridden 3-param overload throws
       Assert.Throws<InvalidOperationException>(() => service.Format("hello", true, 5));
   }
   ```

3. **`ThreeOverload_OverloadDisambiguation_ViaLambdaParameterTypes`**:
   Verifies overload disambiguation works correctly via lambda parameter types on the single `stub.Format` interceptor. All 3 overloads configured through the same interceptor property, each disambiguated by their lambda signature.

   ```csharp
   [Fact]
   public void ThreeOverload_OverloadDisambiguation_ViaLambdaParameterTypes()
   {
       // Arrange
       var stub = new ThreeOverloadPartialStubOverrideStub();
       // Configure non-overridden overloads via Call with disambiguating lambdas
       stub.Format.Call((string input, bool uppercase) =>
           uppercase ? input.ToUpper() : input);
       stub.Format.Call((string input, bool uppercase, int maxLength) =>
           (uppercase ? input.ToUpper() : input).Substring(0, Math.Min(input.Length, maxLength)));
       IThreeOverloadStubOverrideService service = stub;

       // Act
       var r1 = service.Format("hello");              // Uses stub override
       var r2 = service.Format("hello", true);         // Uses Call (2-param lambda)
       var r3 = service.Format("hello world", true, 5);// Uses Call (3-param lambda)

       // Assert
       Assert.Equal("USER:hello", r1);                 // Stub override
       Assert.Equal("HELLO", r2);                      // Call via 2-param
       Assert.Equal("HELLO", r3);                      // Call via 3-param (truncated)

       // All 3 tracked through single interceptor
       stub.Format.Verify(Called.Exactly(3));
   }
   ```

4. **`ThreeOverload_MixedConfiguration_StubOverrideAndCallCoexist`**:
   Verifies that Call supersedes the stub override on the overridden overload, while the other overloads also use Call, all through the single `stub.Format` interceptor.

   ```csharp
   [Fact]
   public void ThreeOverload_MixedConfiguration_StubOverrideAndCallCoexist()
   {
       // Arrange - override all 3 overloads via Call (supersedes stub override on first)
       var stub = new ThreeOverloadPartialStubOverrideStub();
       stub.Format.Call(input => "CALL:" + input);                                    // Supersedes stub override
       stub.Format.Call((string input, bool uppercase) => "CALL2:" + input);          // Regular call
       stub.Format.Call((string input, bool uppercase, int maxLength) => "CALL3:" + input); // Regular call
       IThreeOverloadStubOverrideService service = stub;

       // Act
       var r1 = service.Format("hello");
       var r2 = service.Format("hello", true);
       var r3 = service.Format("hello", true, 5);

       // Assert - Call supersedes stub override for all overloads
       Assert.Equal("CALL:hello", r1);
       Assert.Equal("CALL2:hello", r2);
       Assert.Equal("CALL3:hello", r3);
   }
   ```

5. **`ThreeOverload_StrictMode_OverriddenOverloadDoesNotThrow`**:
   Verifies that in strict mode, the overridden overload (with stub override) does NOT throw, while the non-overridden overloads DO throw `StubException`.

   ```csharp
   [Fact]
   public void ThreeOverload_StrictMode_OverriddenOverloadDoesNotThrow()
   {
       // Arrange
       var stub = new ThreeOverloadPartialStubOverrideStub().Strict();
       IThreeOverloadStubOverrideService service = stub;

       // Act & Assert - overridden overload does NOT throw (stub override IS configuration)
       var result = service.Format("hello");
       Assert.Equal("USER:hello", result);

       // Act & Assert - non-overridden overloads SHOULD throw in strict mode
       Assert.Throws<StubException>(() => service.Format("hello", true));
       Assert.Throws<StubException>(() => service.Format("hello", true, 5));
   }
   ```

**Why these tests matter:**
- The existing tests only prove the fix works for 2 overloads. With 3 overloads, the old bug would have produced `stub.Format` (1 overload with stub override) and `stub.Format2` (2 overloads without). This is the exact scenario from `PartialOverloadStubOverrideStub` in Design.Stubs that originally exposed the bug.
- These tests guarantee the fix correctly handles the case where a single stub override method must coexist with MULTIPLE non-overridden overloads in a unified interceptor.
- Lambda parameter type disambiguation across 3 signatures (1-param, 2-param, 3-param) is a stronger test than 2 signatures.

### Phase 5: Verify

1. Build Design.Stubs -- must compile.
2. Run all tests.
3. Inspect generated code for `PartialOverloadStubOverrideStub` -- must have single `Format` interceptor with 3 overloads.
4. Verify `OverloadedStubOverrideStub` still generates correctly (all overloads in one interceptor).
5. Verify `BasicStubOverrideStub` still generates correctly (single method with stub override).
6. Verify `MixedStubOverrideStub` still generates correctly (different methods, not overloads).

---

## Acceptance Criteria

1. `PartialOverloadStubOverrideStub` generates a single `FormatInterceptor` with 3 overloads:
   - `Format(string)` with stub override fallback
   - `Format(string, bool)` without stub override
   - `Format(string, bool, int)` without stub override
2. `stub.Format` is the only interceptor property for Format (no `stub.Format2`).
3. Tests in `BaseClassStubOverrideTests.cs` that referenced `stub.Format2` are updated to use `stub.Format` with overload-disambiguating lambdas, and pass.
4. All existing tests pass (including non-stub-override tests).
5. Design.Stubs and Design.Tests compile and pass.
6. `multiOverloadStubOverrideInterceptors` variable is removed from `FlatRenderer.cs`; `multiOverloadInterceptors` is used for both regular and stub override methods.
7. New 3-overload partial stub override regression tests pass:
   - `ThreeOverload_SingleInterceptor_AllOverloadsAccessible` -- single `stub.Format` interceptor covers all 3 overloads
   - `ThreeOverload_NonOverriddenOverloads_ThrowWithoutCall` -- non-overridden overloads throw without configuration
   - `ThreeOverload_OverloadDisambiguation_ViaLambdaParameterTypes` -- all 3 overloads configured through single interceptor via lambda signature
   - `ThreeOverload_MixedConfiguration_StubOverrideAndCallCoexist` -- Call supersedes stub override while coexisting with non-overridden overloads
   - `ThreeOverload_StrictMode_OverriddenOverloadDoesNotThrow` -- strict mode respects per-overload stub override status

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Removing `StubOverrideGroups` breaks renderer paths | Medium | Medium | Careful audit of all `StubOverrideGroups` references; renderer already has `HasStubOverride` per-method |
| Pre-compiled interceptor fallback wiring broken | Low | Medium | Pre-compiled interceptors check `HasStubOverride` per-method, not per-group |
| Source provider exclusion logic affected | Low | Low | Source providers already skip stub override methods individually |
| VerifyAll behavior changes | Low | Low | VerifyAll iterates method interceptors; unified groups are still method interceptors |

---

## Edge Cases

1. **All overloads have stub overrides** -- `OverloadedStubOverrideStub`. Must generate single interceptor with all per-signature stub override names. Already works today; must not regress.

2. **No overloads, single method with stub override** -- `BasicStubOverrideStub`. Must stay as single-signature interceptor with stub override fallback. Already handled by `ModelAdapters.BuildSingleSignatureModel`.

3. **Mixed methods (not overloads)** -- `MixedStubOverrideStub` has different method names, some with stub overrides. These are separate interceptors (different names), not overloads. No change needed.

4. **Generic methods with overloads** -- Generic methods are excluded from stub override support by design. No change.

5. **Pre-compiled interceptors with stub overrides** -- Flat pipeline can use pre-compiled interceptors for methods. When a method has a stub override, `canUsePreCompiled` is gated by `HasPrimaryConstructor` (line 87). The fallback wiring in `RenderConstructorIfNeeded` iterates `StubOverrideGroups` -- this must be updated to iterate `MethodGroups` and check `HasStubOverride` per-method.

---

## Developer Review Resolutions (Revision 2)

### Concern 1: `multiOverloadStubOverrideInterceptors` resolution

**Raised by:** Developer
**Concern:** After removing `StubOverrideGroups`, the `multiOverloadStubOverrideInterceptors` set (FlatRenderer.cs lines 196-199) will be empty. `RenderStubOverrideImplementation` (line 2239) uses this set to determine whether `Invoke` needs a suffix.
**Proposed fix (developer):** Remove `multiOverloadStubOverrideInterceptors` entirely and pass `multiOverloadInterceptors` instead.
**Resolution:** Accepted. This is correct. After unification, stub override methods are in the same `MethodGroups` as regular methods, so `multiOverloadInterceptors` already covers them. Plan updated: Design change 8 now specifies removing the variable entirely, updating `RenderMethodImplementation`'s signature, and passing `multiOverloadInterceptors` to `RenderStubOverrideImplementation`. Implementation Phase 3 step 3 updated accordingly.

### Concern 2: Tests referencing `stub.Format2` must be updated

**Raised by:** Developer
**Concern:** Tests at `BaseClassStubOverrideTests.cs` lines 465-514 explicitly use `stub.Format2` (the buggy split-interceptor behavior). These are in-scope tests that must be updated.
**Resolution:** Accepted. These tests directly test the feature being fixed, so they are in-scope. Plan updated: Added Phase 4 (Update In-Scope Tests) with specific changes for each affected test. The key change is replacing `stub.Format2.Call((string input, bool uppercase) => ...)` with `stub.Format.Call((string input, bool uppercase) => ...)` -- the two-parameter lambda disambiguates to the correct overload via the Call API. Acceptance criteria updated to include test updates and `multiOverloadStubOverrideInterceptors` removal.

---

## Developer Review Resolutions (Revision 3)

### Concern 3: Missing 3-overload partial stub override regression test

**Raised by:** Developer
**Concern:** The existing tests in `BaseClassStubOverrideTests.cs` only exercise a 2-overload interface (`IOverloadedStubOverrideService` defined locally with `Format(string)` and `Format(string, bool)`). The `PartialOverloadStubOverrideStub` in Design.Stubs uses the 3-overload `IOverloadedStubOverrideService` from Design.Domain (with `Format(string)`, `Format(string, bool)`, `Format(string, bool, int)`), but no compiled test exercises this 3-overload scenario. A 3-overload test would be a stronger regression test because the original bug would produce `stub.Format` (1 overload) and `stub.Format2` (2 overloads) -- the "1 vs. many" split that is the core of this bug.
**Resolution:** Accepted. Plan updated: Added Phase 4b (Add 3-Overload Partial Stub Override Regression Test) with a new `IThreeOverloadStubOverrideService` interface, `ThreeOverloadPartialStubOverrideStub` stub, and 5 new regression test methods covering: single-interceptor accessibility for all 3 overloads, non-overridden overload throw behavior, overload disambiguation via lambda parameter types, mixed Call/stub-override configuration, and strict mode per-overload behavior. Acceptance criteria updated to include all 5 tests.

---

## Implementation Progress

**Started:** 2026-02-19

### Phase 1: Fix FlatModelBuilder Name Assignment
- [x] Simplified `AssignNamesForOverloadGroup` to always assign a single name to all overloads
- [x] Removed the `withStubOverride` / `withoutStubOverride` split logic

### Phase 2: Fix FlatModelBuilder Grouping
- [x] Removed `!m.HasStubOverride` filter from `flatMethodGroups` (line 50)
- [x] Removed `flatStubOverrideGroups` computation entirely (lines 59-69)
- [x] Removed `StubOverrideGroups` field from `FlatGenerationUnit` record
- [x] Updated `FlatModelBuilder.Build()` constructor call to remove `StubOverrideGroups` parameter
- [x] Removed `!m.HasStubOverride` from overload counting (lines 757-760)
- [x] **Checkpoint:** `dotnet build src/Generator/Generator.csproj` succeeds (with expected downstream errors in FlatRenderer)

### Phase 3: Fix FlatRenderer
- [x] Merged `StubOverrideGroups` rendering loop into `MethodGroups` loop with per-group `hasAnyStubOverride` detection
- [x] Removed `multiOverloadStubOverrideInterceptors` variable entirely
- [x] Updated `RenderMethodImplementation` signature to remove `multiOverloadStubOverrideInterceptors` parameter
- [x] Updated `RenderStubOverrideImplementation` to accept `multiOverloadInterceptors` instead
- [x] Updated call site to pass `multiOverloadInterceptors` instead
- [x] Removed `|| unit.StubOverrideGroups.Count > 0` from Verify methods check
- [x] Removed `stubOverrideInterceptorNames` computation and its Verify loop (now covered by `methodInterceptorNames`)
- [x] Updated `RenderConstructorIfNeeded` to iterate `unit.MethodGroups` with per-method `HasStubOverride` check
- [x] **Checkpoint:** `dotnet build src/KnockOff.sln` succeeds (with expected test compile errors)

### Phase 4: Update In-Scope Tests
- [x] Updated `Overload_OnCall_OnNonOverriddenOverload_Works` -- `stub.Format2` to `stub.Format`
- [x] Updated `Overload_MixedConfiguration_EachOverloadIndependent` -- `stub.Format2` to `stub.Format`
- [x] Updated `Overloaded_When_OnNonStubOverrideOverload` in `StubOverrideWhenTests.cs` -- `stub.Format2` to `stub.Format`
- [x] Updated `Overloaded_When_BothOverloadsIndependent` in `StubOverrideWhenTests.cs` -- `stub.Format2` to `stub.Format`
- [x] Fixed `Return` to `Call` for multi-overload disambiguation in When test fallbacks

### Phase 4b: Add 3-Overload Regression Tests
- [x] Added `IThreeOverloadStubOverrideService` interface (3 overloads)
- [x] Added `ThreeOverloadPartialStubOverrideStub` stub with partial stub override on 1 of 3 overloads
- [x] Added `ThreeOverload_SingleInterceptor_AllOverloadsAccessible` test
- [x] Added `ThreeOverload_NonOverriddenOverloads_ThrowWithoutCall` test
- [x] Added `ThreeOverload_OverloadDisambiguation_ViaLambdaParameterTypes` test
- [x] Added `ThreeOverload_MixedConfiguration_StubOverrideAndCallCoexist` test
- [x] Added `ThreeOverload_StrictMode_OverriddenOverloadDoesNotThrow` test
- [x] **Checkpoint:** all tests pass

### Phase 5: Verify
- [x] `dotnet build src/Design/Design.Stubs` succeeds
- [x] All test projects pass (zero failures)
- [x] `PartialOverloadStubOverrideStub` generates single `FormatInterceptor` with 3 overloads
- [x] `OverloadedStubOverrideStub` still generates single interceptor (no regression)
- [x] `BasicStubOverrideStub` still generates correctly (no regression)
- [x] `MixedStubOverrideStub` still generates correctly (no regression)

---

## Completion Evidence

### Test Results

**KnockOffTests** (all 3 target frameworks):
- net10.0: 1515 passed, 4 skipped, 0 failed
- net9.0: 1515 passed, 4 skipped, 0 failed
- net8.0: 1514 passed, 4 skipped, 0 failed

**Design.Tests** (all 3 target frameworks):
- net10.0: 370 passed, 0 failed
- net9.0: 370 passed, 0 failed
- net8.0: 370 passed, 0 failed

**KnockOffTests.AssemblyStrict**: 14 passed per framework, 0 failed
**KnockOff.NeatooInterfaceTests**: 473 passed per framework, 0 failed
**KnockOff.Documentation.Samples**: 701 passed per framework, 0 failed

### Design Projects Compile
- `dotnet build src/Design/Design.Stubs` -- succeeds (all 3 target frameworks)

### Generated Code Verification
- `PartialOverloadStubOverrideStub.g.cs` -- single `FormatInterceptor` with `FormatImpl`, `FormatImpl2`, `FormatImpl3` (3 overloads). No `Format2Interceptor`. Single `stub.Format` property.
- `OverloadedStubOverrideStub.g.cs` (test project) -- single `FormatInterceptor`. No `Format2`. No regression.
- `BasicStubOverrideStub.g.cs` -- 4 separate interceptors for 4 methods. No regression.
- `MixedStubOverrideStub.g.cs` -- separate interceptors per method name. No regression.

### All Contract Items Confirmed Complete
- `multiOverloadStubOverrideInterceptors` variable removed from `FlatRenderer.cs`
- `StubOverrideGroups` field removed from `FlatGenerationUnit`
- All tests using `stub.Format2` updated to use unified `stub.Format`
- 5 new 3-overload regression tests added and passing

### Additional Discovery: StubOverrideWhenTests
The plan identified `BaseClassStubOverrideTests.cs` tests referencing `stub.Format2`, but `StubOverrideWhenTests.cs` (lines 449-469) also referenced `stub.Format2`. These are in-scope tests (they directly test the overloaded stub override feature) and were updated accordingly. The `Return` calls on multi-overload interceptors were changed to `Call` with lambda disambiguation since bare `Return` is not available on multi-overload interceptors.
