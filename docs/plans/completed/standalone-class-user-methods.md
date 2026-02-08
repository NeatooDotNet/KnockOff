# Standalone Class Pipeline: User Method Support for Methods

**Date:** 2026-02-05
**Related Todo:** [Standalone Class User Methods](../todos/standalone-class-stub-overrides.md)
**Status:** Complete
**Last Updated:** 2026-02-05

---

## Overview

Wire user method detection and rendering into the standalone class pipeline (`[KnockOffBase<T>]` -- patterns 3 and 4). The underscore-suffix user method pattern (`MethodName_()`) works in the interface pipeline (patterns 1 and 2) but was never implemented for standalone class stubs. This plan adds that support by threading the existing infrastructure through the standalone class pipeline's Transform, Builder, and Renderer.

---

## Design Decisions

Confirmed during clarification review (2026-02-05):

1. **Virtual method fallback: Option A -- user method completely replaces `base.Method()` call.** When a user defines `Initialize_()`, that IS the default behavior. The interceptor will NOT also call `base.Initialize()`. This is consistent with (a) the interface pipeline, where user methods replace Source/Strict as the fallback, and (b) standalone class property user overrides, which similarly replace the base class property value entirely.

2. **Interceptor pattern: interceptor-internal fallback.** Pass `stub` to the interceptor's `Invoke()` method. When the interceptor is unconfigured, it calls `stub.Method_()` internally. This matches the interface pipeline pattern where the generated interceptor handles the user method fallback, not the Impl class override.

3. **Base class virtual methods: generate for ALL target class methods, not just user-overridden ones.** The base class will emit `protected virtual` methods with `_` suffix for every method on the target class, regardless of whether the user overrides them. This matches the property behavior (all properties get virtual `_` accessors). It provides IntelliSense discoverability and allows users to add overrides later without regeneration.

4. **Pattern 4 verification: explicit Design.Stubs acceptance criteria required.** Pattern 4 (generic standalone class) must have its own Design.Stubs code that exercises user methods with `[KnockOffBase(typeof(RepositoryBase<>))]`. Do NOT assume it works because it shares a pipeline with pattern 3. The generic type parameter introduces additional code generation complexity.

5. **`.When()` is in scope.** `.When()` support for standalone class user method interceptors is part of this todo's deliverables. The existing `.When()` infrastructure on user method interceptors (implemented for the interface pipeline) should carry through, but it must be verified for patterns 3 and 4.

6. **Shared model: `HasUserOverride` on `InlineClassImplMethodModel`.** Add the `HasUserOverride` boolean to the existing `InlineClassImplMethodModel` record rather than creating a new model type. This record is shared between inline class and standalone class pipelines for Impl method rendering.

7. **Diagnostics: no custom KnockOff diagnostic needed.** The standard C# compiler error `CS0115` ("no suitable method found to override") is sufficient for mismatched user methods. If a user writes an incorrect override signature, the compiler catches it. No `KO` diagnostic required.

---

## Scope

### Patterns Affected

| Pattern | Affected | Notes |
|---------|----------|-------|
| Standalone (interface) | No | Already has user method support |
| Generic Standalone (interface) | No | Already has user method support |
| **Standalone Class** | **Yes** | Primary target |
| **Generic Standalone Class** | **Yes** | Primary target |
| Inline Interface | No | Does not use user methods (by design) |
| Inline Class | No | Does not use user methods (by design) |
| Inline Delegate | No | Not applicable |
| Open Generic Interface | No | Does not use user methods (by design) |
| Open Generic Class | No | Does not use user methods (by design) |

### Member Types

- **Methods**: Yes -- full user method override support with `_` suffix pattern
- **Properties**: No -- already works (verified in Design.Stubs)
- **Indexers**: No -- not applicable to user method pattern
- **Events**: No -- not applicable to user method pattern

---

## Problem Analysis

### Current State

The standalone class pipeline uses completely separate code paths from the interface pipeline:

| Component | Interface Pipeline (1,2) | Class Pipeline (3,4) |
|---|---|---|
| Transform | `TransformClass` | `TransformStandaloneClassStub` |
| Builder | `FlatModelBuilder` | `StandaloneClassModelBuilder` |
| Renderer | `FlatRenderer` | `StandaloneClassRenderer` |
| Model | `KnockOffTypeInfo.UserOverrideMethods` | `StandaloneClassStubInfo` -- **no UserOverrideMethods** |

User method detection and the `_` suffix pattern were implemented only in `FlatModelBuilder` / `FlatRenderer`. The `StandaloneClassModelBuilder` and `StandaloneClassRenderer` were never updated to support this.

### Gap Analysis

Four specific gaps exist in the standalone class pipeline:

1. **Transform layer** (`KnockOffGenerator.StandaloneClass.cs` line 171): Calls `DetectUserOverrideProperties` but NOT `DetectUserOverrideMethods`. The `StandaloneClassStubInfo` record (line 221) has `UserOverrideProperties` but NOT `UserOverrideMethods`.

2. **Builder layer** (`StandaloneClassModelBuilder.cs` lines 86-103): Builds method interceptors via `UnifiedInterceptorBuilder.BuildMethodInterceptor()` but never passes the `userMethodName` parameter. No user method detection is performed.

3. **Model layer** (`StandaloneClassGenerationUnit.cs` line 55): Has `BaseClassProperties` but no `BaseClassMethods`. No `BaseClassMethodModel` record exists. The `InlineClassImplMethodModel` (used for Impl class) has no `HasUserOverride` field.

4. **Renderer layer** (`StandaloneClassRenderer.cs`):
   - `RenderBaseClass()` (lines 167-212): Only generates virtual protected **properties** with `_` suffix. No methods.
   - `RenderImplMethodOverride()` (lines 658-730): Delegates to interceptor's `Invoke()` without checking for user method overrides or passing the stub instance. For virtual methods, uses unconfigured-count fallback to base class.

### Key Insight: Most Infrastructure Already Exists

The following shared components already support user methods:

- `DetectUserOverrideMethods()` in `KnockOffGenerator.Helpers.cs` (lines 21-53) -- syntactic detection of override methods with `_` suffix
- `UnifiedInterceptorBuilder.BuildMethodInterceptor()` already accepts `string? userMethodName = null` parameter (line 37)
- `MethodInterceptorRenderer` already handles `UserMethodFallback` option in `InterceptorRenderOptions`

The work is primarily **wiring these together** in the standalone class pipeline, plus adding the base class method generation.

---

## Design

### Architecture: Parallel the Property Pattern

The property user override pattern is already implemented in the standalone class pipeline:

**Properties (working):**
1. Transform: `DetectUserOverrideProperties(classSymbol)` -> `StandaloneClassStubInfo.UserOverrideProperties`
2. Builder: `userOverrideProperties.Contains(member.Name + "_")` -> `BuildImplPropertyModel(member, hasUserOverride: true)`
3. Model: `BaseClassPropertyModel` in `StandaloneClassGenerationUnit.BaseClassProperties`
4. Renderer (Base): `RenderBaseClassProperty()` generates `protected virtual T PropertyName_ { get => default!; set { } }`
5. Renderer (Impl): `RenderImplPropertyOverride()` checks `prop.HasUserOverride` and routes to `_stub.PropertyName_`

**Methods (to implement) -- same pattern:**
1. Transform: `DetectUserOverrideMethods(classSymbol)` -> `StandaloneClassStubInfo.UserOverrideMethods`
2. Builder: `userOverrideMethods.Contains(...)` -> pass `userMethodName` to `UnifiedInterceptorBuilder` + set `HasUserOverride` on `InlineClassImplMethodModel`
3. Model: `BaseClassMethodModel` in `StandaloneClassGenerationUnit.BaseClassMethods`
4. Renderer (Base): `RenderBaseClassMethod()` generates `protected virtual T MethodName_(params) => default!;`
5. Renderer (Impl): `RenderImplMethodOverride()` checks `method.HasUserOverride` and passes `_stub` to `Invoke()`

### Generated Code Pattern

**Base class (new -- methods added alongside existing properties):**
```csharp
public class StandaloneClassUserMethodStubBase
{
    // Existing: virtual protected properties
    protected virtual string Name_ => default!;

    // NEW: virtual protected methods
    /// <summary>Override to provide default implementation for ServiceBase.Execute.</summary>
    protected virtual void Execute_(string command) { }

    /// <summary>Override to provide default implementation for ServiceBase.Initialize.</summary>
    protected virtual void Initialize_() { }
}
```

**User override (user code -- currently produces CS0115):**
```csharp
public partial class StandaloneClassUserMethodStub
{
    protected override void Execute_(string command)
    {
        // User-defined default behavior
    }
}
```

**Impl class method override (with user override):**
```csharp
// Abstract method with user override -- calls Invoke with stub reference
public override void Execute(string command)
{
    if (_stub == null) return;
    _stub.Execute.Invoke(_stub.Strict, _stub, command);
}
```

**Impl class method override (without user override -- unchanged):**
```csharp
// Virtual method without user override -- current unconfigured-count pattern
public override void Initialize()
{
    if (_stub == null) { base.Initialize(); return; }
    var unconfiguredBefore = _stub.Initialize.UnconfiguredCallCount;
    _stub.Initialize.Invoke(_stub.Strict);
    if (_stub.Initialize.UnconfiguredCallCount > unconfiguredBefore)
    {
        base.Initialize();
    }
}
```

### User Method Interceptor vs Standard Interceptor

When a method has a user override, the interceptor gains user method fallback via the existing `UserMethodFallback` option in `MethodInterceptorRenderer`:

- **Standard interceptor**: Falls to Source/Strict/default when unconfigured
- **User method interceptor**: Falls to `stub.MethodName_(args)` when unconfigured

This is handled entirely by the existing `UnifiedInterceptorBuilder` + `MethodInterceptorRenderer` infrastructure. The standalone class builder just needs to pass `userMethodName` to trigger it.

### Impl Method Behavior Matrix

| Method Type | Has User Override | Impl Behavior |
|---|---|---|
| Abstract | No | Call `Invoke(_stub.Strict, args)` -- interceptor handles everything |
| Abstract | Yes | Call `Invoke(_stub.Strict, _stub, args)` -- interceptor falls to user method |
| Virtual | No | Unconfigured-count pattern: call Invoke, if unconfigured, call `base.Method()` |
| Virtual | Yes | Call `Invoke(_stub.Strict, _stub, args)` -- interceptor falls to user method (no base call) |

**Confirmed decision (Decision 1):** The user method completely replaces `base.Method()`. If the user defines `Initialize_()`, that IS the "default behavior" -- no `base.Initialize()` call occurs. This is consistent with (a) the interface pipeline where user methods replace Source/Strict as the fallback, and (b) standalone class property user overrides which similarly replace the base value entirely.

---

## Implementation Steps

### Phase 1: Transform Layer

Add user method detection to the standalone class transform.

**Files:**
- `src/Generator/KnockOffGenerator.StandaloneClass.cs`
  - Call `DetectUserOverrideMethods(classSymbol)` alongside existing `DetectUserOverrideProperties`
  - Add `UserOverrideMethods` field to `StandaloneClassStubInfo` record

**Verification:** Build succeeds, no test changes

### Phase 2: Model Layer

Add base class method model and update Impl method model.

**Files:**
- `src/Generator/Model/StandaloneClass/BaseClassMethodModel.cs` (NEW)
  - Record with: MethodName, ReturnType, Parameters, IsVoid, IsAbstract, TargetMemberDescription
  - Pattern follows `BaseClassPropertyModel`
- `src/Generator/Model/StandaloneClass/StandaloneClassGenerationUnit.cs`
  - Add `EquatableArray<BaseClassMethodModel> BaseClassMethods` field
- `src/Generator/Model/Inline/InlineClassStubModel.cs`
  - Add `bool HasUserOverride = false` to `InlineClassImplMethodModel` record (Decision 6 -- shared model, no new type)

**Verification:** Build succeeds, no test changes

### Phase 3: Builder Layer

Wire user method detection into the model builder.

**Files:**
- `src/Generator/Builder/StandaloneClassModelBuilder.cs`
  - Add `userOverrideMethods` HashSet (parallel to existing `userOverrideProperties`)
  - Build signature keys for methods and check against user override set
  - Pass `userMethodName` to `UnifiedInterceptorBuilder.BuildMethodInterceptor()` when user override exists
  - Set `HasUserOverride` on `InlineClassImplMethodModel` for methods with user overrides
  - Build `BaseClassMethods` from class members (parallel to existing `BaseClassProperties`)

**Key detail -- signature matching (Concern 1 resolution):** Extract the signature key logic into a shared helper rather than duplicating it. See "Developer Concern 1" section below for full specification.

**Key detail -- partial overload coverage (Concern 2 resolution):** When only some overloads of a method have user overrides, use a single interceptor with per-signature `UserMethodName` tracking. See "Developer Concern 2" section below for full specification. The builder changes are:
  - When calling `UnifiedInterceptorBuilder.BuildMethodInterceptor()`, pass `userMethodName` if ANY overload in the group has a user override
  - In the multi-overload path, `BuildOverloadSignature` already accepts `userMethodName` and sets it on `MethodOverloadSignature.UserMethodName` -- but the builder must pass the correct value per-signature (non-null for overloads with user overrides, null for those without)
  - Requires modifying `StandaloneClassModelBuilder.GroupMethodsByName()` to accept the `userOverrideMethods` set and pass per-overload user method names to `UnifiedInterceptorBuilder`

**Verification:** Build succeeds, no test changes (model changes only, no rendering yet)

### Phase 4: Renderer Layer -- Base Class Methods

Generate virtual protected methods in the base class.

**Files:**
- `src/Generator/Renderer/StandaloneClassRenderer.cs`
  - Add `RenderBaseClassMethod()` method (parallel to existing `RenderBaseClassProperty()`)
  - Update `RenderBaseClass()` to iterate `unit.BaseClassMethods` after properties

**Generated base class method patterns:**
```csharp
// Void method with parameters
protected virtual void Execute_(string command) { }

// Void method without parameters
protected virtual void Initialize_() { }

// Non-void method with parameters
protected virtual string Process_(string input) => default!;

// Non-void method without parameters
protected virtual int GetCount_() => default!;
```

**Verification:**
- Build succeeds
- Design.Stubs `StandaloneClassUserMethods.cs` compiles (CS0115 errors resolve)

### Phase 5: Renderer Layer -- Interceptor Options and Impl Method User Override

Update interceptor rendering options and Impl method rendering to handle user method overrides. Two sub-changes:

**5a. Interceptor class rendering -- set `UserMethodFallback` on `InterceptorRenderOptions`:**
- `src/Generator/Renderer/StandaloneClassRenderer.cs`
  - In the `foreach (var method in unit.Methods)` loop (line 86-96), check if the method model has any user method name (model-level or per-overload)
  - When true, set `UserMethodFallback: true` and `StubTypeName: "{ClassName}{TypeParams}"` on `InterceptorRenderOptions`
  - See "Developer Concern 2" section for exact code pattern

**5b. Impl method rendering -- branch on `HasUserOverride`:**
- `src/Generator/Renderer/StandaloneClassRenderer.cs`
  - Update `RenderImplMethodOverride()` to check `method.HasUserOverride`
  - When `HasUserOverride` is true: pass `_stub` to `Invoke()` call and skip the unconfigured-count/base-call pattern
  - For multi-overload interceptors with partial coverage: each overload's `Invoke_` call independently includes or excludes `_stub` based on that overload's `HasUserOverride`
  - See "Developer Concern 2" section for concrete generated code examples

**Verification:**
- Build succeeds
- All existing tests pass
- Generated code for `StandaloneClassUserMethodStub` verified:
  - Execute override calls `_stub.Execute.Invoke(_stub.Strict, _stub, command)`
  - Initialize override calls `_stub.Initialize.Invoke(_stub.Strict, _stub)`
- Generated code for `RepositoryUserMethodStub<T>` verified:
  - GetDefault() (user override) calls `_stub.GetDefault.Invoke_NoParams_TNullable(_stub.Strict, _stub)`
  - GetDefault(string) (no user override) uses unconfigured-count pattern with `base.GetDefault(category)` fallback

### Phase 6: Test Coverage

Add tests for standalone class user method overrides.

**Files:**
- `src/Tests/KnockOffTests/` -- New test file for standalone class user methods
  - Test user method fallback for abstract methods
  - Test user method fallback for virtual methods
  - Test OnCall supersedes user method
  - Test Returns supersedes user method
  - Test When chains with user method fallback
  - Test Sequences with user method fallback
  - Test Verifiable on user method interceptors
  - Test generic standalone class with user methods (pattern 4)
- `src/Design/Design.Stubs/UserMethods/StandaloneClassUserMethods.cs` -- Expand with additional examples

**Verification:** All new and existing tests pass

### Phase 7: Documentation

Update documentation to reflect standalone class user method support.

**Files:**
- `docs/guides/api-consistency-matrix.md` -- Update user methods section
- `src/Design/Design.Stubs/UserMethods/StandaloneClassUserMethods.cs` -- Documentation comments

**Verification:** Documentation builds, examples compile

---

## Acceptance Criteria

### Design.Stubs Compilation (primary gate)
- [ ] `StandaloneClassUserMethods.cs` compiles without errors -- both pattern 3 and pattern 4 code

### Pattern 3: Standalone Class (`[KnockOffBase<ServiceBase>]`)
- [ ] User can override `MethodName_()` on `[KnockOffBase<T>]` stubs
- [ ] Abstract methods with user override call user method as fallback
- [ ] Virtual methods with user override call user method as fallback (no base call -- Decision 1)
- [ ] Methods without user override retain current behavior (unconfigured-count + base call)
- [ ] `.When()` chains work on user method interceptors (Decision 5 -- in scope)
- [ ] `.OnCall()` supersedes user method
- [ ] `.Returns()` supersedes user method
- [ ] Sequences work with user method fallback
- [ ] `.Verifiable()` works on user method interceptors

### Pattern 4: Generic Standalone Class (`[KnockOffBase(typeof(RepositoryBase<>))]`)
- [ ] User methods work with methods returning generic type `T?` (`GetById_`)
- [ ] User methods work with methods accepting generic type `T` parameter (`Save_`)
- [ ] Partial overload coverage works (override one overload, leave others to standard interceptor)

### Cross-cutting
- [ ] All existing tests continue to pass
- [ ] Base class generates virtual `_` methods for ALL target class methods (Decision 3)

---

## Dependencies

- None -- all required infrastructure exists in the codebase

---

## Risks / Considerations

### Risk: Signature Key Format Mismatch

The `DetectUserOverrideMethods()` helper returns method names with `_` suffix. The builder needs to match these against target class methods. If the signature key format differs between the standalone class and interface pipelines, user overrides won't be detected.

**Mitigation:** Extract shared helper method (see "Developer Concern 1" resolution). Both pipelines call the same `SymbolHelpers.BuildOverrideSignatureKey()` method, eliminating format drift. The key format is `"MethodName_(ParamType1,ParamType2,...)"`.

### Risk: Partial Overload Coverage

When only some overloads of a method have user overrides, the interceptor must correctly apply user method fallback only to overloads with overrides, not all overloads.

**Mitigation:** Use per-signature `UserMethodName` on `MethodOverloadSignature` (see "Developer Concern 2" resolution). The `MethodInterceptorRenderer` already checks `overload.UserMethodName` per-signature in `RenderOverloadInvokeMethod()`. Each Impl method override independently controls whether to pass `_stub` based on its own `HasUserOverride`.

### Risk: Virtual Method Fallback Change

For virtual methods with user overrides, the behavior changes: instead of falling back to `base.Method()`, the interceptor falls to `stub.Method_()`. This is the confirmed design (Decision 1). The user method completely replaces the base call.

**Mitigation:** Confirmed as correct by user. Consistent with interface pipeline user methods and standalone class property user overrides. No `base.Method()` call when user override exists.

### Risk: Generated Code Size

Base class gains additional virtual methods (one per stubbed method). This increases generated code size but is acceptable -- the pattern already exists for properties.

**Mitigation:** None needed. Properties already use this pattern without issues.

### Note: Source(T) Behavior

Same as the interface pipeline: user method stubs have empty `Source(T)` method bodies. The user method IS the fallback.

---

## Developer Concern Resolutions

### Developer Concern 1: Signature Key Matching -- Extract Shared Helper

**Problem:** `FlatModelBuilder.BuildOverrideSignatureKeyFromMember(InterfaceMemberInfo)` builds signature keys for matching against `DetectUserOverrideMethods()` output. The standalone class pipeline uses `ClassMemberInfo`, not `InterfaceMemberInfo`. The plan originally said "follow the same pattern" without specifying whether to extract or duplicate.

**Resolution: Extract into `SymbolHelpers` as a shared static method.**

Both `InterfaceMemberInfo` and `ClassMemberInfo` have the same relevant fields for signature key building:
- `Name` (string)
- `Parameters` (`EquatableArray<ParameterInfo>`)
- Each `ParameterInfo` has `Type` (string) and `RefKind` (`RefKind`)

The logic does not depend on any other fields of either type. Extract to:

```csharp
// In SymbolHelpers (or a new OverrideSignatureHelper static class)
internal static string BuildOverrideSignatureKey(string methodName, EquatableArray<ParameterInfo> parameters)
{
    var paramArray = parameters.GetArray() ?? Array.Empty<ParameterInfo>();
    var paramParts = paramArray.Select(p =>
    {
        var prefix = p.RefKind switch
        {
            RefKind.Ref => "ref ",
            RefKind.Out => "out ",
            RefKind.In => "in ",
            RefKind.RefReadOnlyParameter => "ref readonly ",
            _ => ""
        };
        return prefix + NormalizeTypeForOverrideMatching(p.Type);
    });
    return $"{methodName}_({string.Join(",", paramParts)})";
}
```

Also extract `NormalizeTypeForOverrideMatching(string type)` to the same location. Currently this method exists only in `FlatModelBuilder` (lines 1577-1605). The `NormalizeSyntaxType` in `KnockOffGenerator.Helpers.cs` (lines 134-162) has an identical switch table but operates on syntax strings. Both must produce compatible output -- keeping them in different files is fine since they serve different pipeline stages (syntax vs semantic), but `NormalizeTypeForOverrideMatching` must be shared because both `FlatModelBuilder` and `StandaloneClassModelBuilder` call it.

**Call sites after extraction:**
- `FlatModelBuilder.BuildOverrideSignatureKeyFromMember(InterfaceMemberInfo member)` becomes: `SymbolHelpers.BuildOverrideSignatureKey(member.Name, member.Parameters)` (or inline the call)
- `FlatModelBuilder.HasMatchingUserOverride(InterfaceMemberInfo member, ...)` uses the shared method
- `StandaloneClassModelBuilder` calls `SymbolHelpers.BuildOverrideSignatureKey(member.Name, member.Parameters)` on `ClassMemberInfo`

**Compatibility guarantee:** `NormalizeTypeForOverrideMatching` normalizes **semantic model types** (from `IParameterSymbol.Type.ToDisplayString()`). `NormalizeSyntaxType` normalizes **syntax types** (from user source code). Both map `global::System.String` -> `string`, `System.Int32` -> `int`, etc. The switch tables are identical. They converge on the same output for any given type, ensuring `DetectUserOverrideMethods()` output (syntax-based keys) matches `BuildOverrideSignatureKey()` output (semantic-based keys).

**Files changed:**
- `src/Generator/SymbolHelpers.cs` (or new file `src/Generator/OverrideSignatureHelper.cs`) -- add shared methods
- `src/Generator/Builder/FlatModelBuilder.cs` -- delegate to shared method, remove private duplicate
- `src/Generator/Builder/StandaloneClassModelBuilder.cs` -- call shared method

---

### Developer Concern 2: Partial Overload Coverage -- Single Interceptor Strategy

**Problem:** When only SOME overloads of a method have user overrides (e.g., `GetDefault_()` overridden but `GetDefault_(string)` not), the plan did not specify how the standalone class pipeline handles this. The flat pipeline splits overloads with user overrides into separate interceptor groups via `AssignNamesForOverloadGroup()`. The standalone class pipeline groups all overloads together via `GroupMethodsByName()`.

**Resolution: Use single interceptor with per-signature `UserMethodName` tracking. Do NOT split overloads into separate interceptors.**

The standalone class pipeline differs from the flat pipeline in a key way: the flat pipeline needs separate interceptor properties on the stub class for user-override vs non-user-override overloads because the explicit interface implementation must reference a specific interceptor property. The standalone class pipeline does not have this constraint -- all overloads already route through a single interceptor.

The existing multi-overload infrastructure in `UnifiedInterceptorBuilder` and `MethodInterceptorRenderer` already supports per-signature user methods:

1. **`UnifiedInterceptorBuilder.BuildOverloadSignature()`** (line 141-171) accepts `string? userMethodName = null` and sets `MethodOverloadSignature.UserMethodName`.

2. **`MethodInterceptorRenderer.RenderOverloadInvokeMethod()`** (lines 857-1019) checks `options.UserMethodFallback && !string.IsNullOrEmpty(overload.UserMethodName)` (line 1004). This is per-signature: overloads with `UserMethodName = null` skip user method fallback; overloads with `UserMethodName = "GetDefault_"` use it.

3. **`InterceptorRenderOptions.UserMethodFallback`** is set at the interceptor level (not per-overload). It must be `true` if ANY overload has a user override. The per-signature `UserMethodName` (null vs non-null) controls which signatures actually use fallback.

**Builder changes required:**

The current `StandaloneClassModelBuilder` call to `UnifiedInterceptorBuilder.BuildMethodInterceptor()` (line 97) passes overloads but not `userMethodName`. The change is:

```csharp
// Current:
var methodModel = UnifiedInterceptorBuilder.BuildMethodInterceptor(
    interceptorClassName: interceptorClassName,
    methodName: group.MethodName,
    declaringInterface: "",
    ownerClassName: ownerClassName,
    ownerTypeParameters: "",
    overloads: signatures);

// New: pass userMethodName when any overload has user override
var anyHasUserOverride = group.Members.Any(m =>
    userOverrideMethods.Contains(SymbolHelpers.BuildOverrideSignatureKey(m.Name, m.Parameters)));
var userMethodName = anyHasUserOverride ? $"{group.MethodName}_" : null;

var methodModel = UnifiedInterceptorBuilder.BuildMethodInterceptor(
    interceptorClassName: interceptorClassName,
    methodName: group.MethodName,
    declaringInterface: "",
    ownerClassName: ownerClassName,
    ownerTypeParameters: "",
    overloads: signatures,
    userMethodName: userMethodName);
```

But this passes the SAME `userMethodName` to ALL overloads. For partial coverage, we need per-signature differentiation.

**Solution:** Modify the call to `UnifiedInterceptorBuilder.BuildMethodInterceptor()` to support per-signature user method names. Two options:

**Option A (Preferred): Extend `MethodSignatureInfo` with optional `UserMethodName`.**

Add `string? UserMethodName = null` to `MethodSignatureInfo`. Then `UnifiedInterceptorBuilder.BuildOverloadSignature()` can read it from the signature instead of using the blanket `userMethodName` parameter for all overloads.

```csharp
// In StandaloneClassModelBuilder, when building signatures:
var signatures = group.Members
    .Select(m => {
        var sig = ToMethodSignatureInfo(m);
        var hasUserOverride = userOverrideMethods.Contains(
            SymbolHelpers.BuildOverrideSignatureKey(m.Name, m.Parameters));
        return sig with { UserMethodName = hasUserOverride ? $"{m.Name}_" : null };
    })
    .ToList();
```

Then in `UnifiedInterceptorBuilder.BuildOverloadSignature()`, read `sig.UserMethodName` instead of the blanket parameter:

```csharp
// Current (line 170):
UserMethodName: userMethodName);

// New:
UserMethodName: sig.UserMethodName ?? userMethodName);
```

This is backward-compatible: `FlatModelBuilder` does not set `UserMethodName` on `MethodSignatureInfo`, so it falls back to the blanket parameter (existing behavior preserved).

**Option B (Alternative): Pass a dictionary of per-signature user method names.**

Add an optional `Dictionary<string, string?>` parameter mapping signature keys to user method names. More complex, less clean.

**Recommendation: Option A.** Extending `MethodSignatureInfo` is minimal, backward-compatible, and self-documenting.

**Renderer changes required:**

In `StandaloneClassRenderer.cs`, method interceptor rendering (line 86-96) currently creates `InterceptorRenderOptions` without `UserMethodFallback`:

```csharp
// Current:
var options = new InterceptorRenderOptions(
    BaseIndent: baseIndent,
    IncludeStrictParameter: true,
    StrictAccessExpression: "strict",
    InterceptorTypeParameters: typeParamList,
    InterceptorConstraints: constraintClauses);
```

Change to set `UserMethodFallback` and `StubTypeName` when the interceptor model has a user method:

```csharp
// New:
var hasUserMethod = !string.IsNullOrEmpty(method.UserMethodName) ||
    method.Overloads.Any(o => !string.IsNullOrEmpty(o.UserMethodName));
var options = new InterceptorRenderOptions(
    BaseIndent: baseIndent,
    IncludeStrictParameter: true,
    StrictAccessExpression: "strict",
    InterceptorTypeParameters: typeParamList,
    InterceptorConstraints: constraintClauses,
    UserMethodFallback: hasUserMethod,
    StubTypeName: hasUserMethod ? $"{unit.ClassName}{typeParamList}" : null);
```

**Impl method rendering changes:**

In `StandaloneClassRenderer.RenderImplMethodOverride()` (lines 658-730), when `HasUserOverride` is true, the Impl method must pass `_stub` to `Invoke()` and skip the unconfigured-count/base-call pattern. The generated code for a method with user override becomes:

```csharp
// HasUserOverride = true:
public override void Execute(string command)
{
    if (_stub == null) return;
    _stub.Execute.Invoke(_stub.Strict, _stub, command);
}
```

vs current pattern for virtual methods without user override:

```csharp
// HasUserOverride = false, virtual:
public override void Initialize()
{
    if (_stub == null) { base.Initialize(); return; }
    var unconfiguredBefore = _stub.Initialize.UnconfiguredCallCount;
    _stub.Initialize.Invoke(_stub.Strict);
    if (_stub.Initialize.UnconfiguredCallCount > unconfiguredBefore)
    {
        base.Initialize();
    }
}
```

The `HasUserOverride` field controls this branching. For multi-overload interceptors with partial coverage, each overload's `Invoke_` suffix call includes or excludes `_stub` based on that specific overload's `HasUserOverride` value.

**Concrete example with Pattern 4 acceptance criteria (`GetDefault` with partial overload coverage):**

```
Target class:
  T? GetDefault()          -- user overrides GetDefault_()
  T? GetDefault(string)    -- no user override

Single interceptor: GetDefaultInterceptor
  Overloads[0]: SignatureSuffix="NoParams_TNullable", UserMethodName="GetDefault_"
  Overloads[1]: SignatureSuffix="String_TNullable", UserMethodName=null

Impl rendering:
  // GetDefault() -- HasUserOverride=true
  public override T? GetDefault()
  {
      if (_stub == null) return default;
      return _stub.GetDefault.Invoke_NoParams_TNullable(_stub.Strict, _stub);
  }

  // GetDefault(string) -- HasUserOverride=false, virtual
  public override T? GetDefault(string category)
  {
      if (_stub == null) return base.GetDefault(category);
      var unconfiguredBefore = _stub.GetDefault.UnconfiguredCallCount;
      var result = _stub.GetDefault.Invoke_String_TNullable(_stub.Strict, category);
      if (_stub.GetDefault.UnconfiguredCallCount > unconfiguredBefore)
      {
          return base.GetDefault(category);
      }
      return result;
  }
```

**Why not split into separate interceptors (like FlatModelBuilder)?**

The flat pipeline splits because it needs distinct interceptor *properties* on the stub class -- the explicit interface implementation calls `this.MethodName.Invoke(...)` and the interceptor property name determines the API surface. Splitting gives `stub.GetDefault` (user method interceptor) and `stub.GetDefault2` (regular interceptor), which maps to the separate `FlatMethodGroup`/`UserMethodGroup` rendering.

The standalone class pipeline does not have this constraint. The Impl class calls `_stub.GetDefault.Invoke_SUFFIX(...)` with per-overload suffixes. A single interceptor with per-signature `UserMethodName` keeps the API clean: `stub.GetDefault` covers all overloads.

**Files changed:**
- `src/Generator/Builder/UnifiedInterceptorBuilder.cs` -- `MethodSignatureInfo` gains `string? UserMethodName = null`; `BuildOverloadSignature` uses `sig.UserMethodName ?? userMethodName`
- `src/Generator/Builder/StandaloneClassModelBuilder.cs` -- per-overload user method detection when building signatures
- `src/Generator/Renderer/StandaloneClassRenderer.cs` -- `InterceptorRenderOptions` set `UserMethodFallback`/`StubTypeName` when model has user methods; `RenderImplMethodOverride` branches on `HasUserOverride`
- `src/Generator/Model/Inline/InlineClassStubModel.cs` -- `InlineClassImplMethodModel` gains `bool HasUserOverride = false`

---

## Architectural Verification

### Nine Patterns Analysis

- **Standalone (interface)**: Not affected -- already has user method support
- **Generic Standalone (interface)**: Not affected -- already has user method support
- **Standalone Class**: Primary target -- gains user method support for methods
- **Generic Standalone Class**: Primary target -- gains user method support for methods
- **Inline Interface**: Not affected -- cannot have user methods (by design)
- **Inline Class**: Not affected -- cannot have user methods (by design)
- **Inline Delegate**: Not affected -- cannot have user methods
- **Open Generic Interface**: Not affected -- cannot have user methods
- **Open Generic Class**: Not affected -- cannot have user methods

### Design.Stubs Compilation Verification

| Pattern + Feature | Status | Evidence |
|---|---|---|
| Standalone Class + User Methods (Pattern 3) | **Needs Implementation** | CS0115 at `StandaloneClassUserMethods.cs:55,60` -- base class does not generate virtual methods |
| Generic Standalone Class + User Methods (Pattern 4) | **Needs Implementation** | CS0115 at `StandaloneClassUserMethods.cs:98,106,115` -- generic base class does not generate virtual methods |
| Standalone (interface) + User Methods | Verified (existing) | `UserMethodBasics.cs` compiles, `BasicUserMethodStub.g.cs` generates correctly |
| Standalone Class + User Properties | Verified (existing) | `StubOverridePropertyBasics.cs:512-556` compiles, `ConfigStub.Base.g.cs` generates virtual properties |
| Generic Standalone Class + User Properties (Pattern 4) | Verified (existing) | `StubOverridePropertyBasics.cs:612-651` compiles, `CacheStubOverridePropertyStub.Base.g.cs` generates virtual properties |

**Failing Design.Stubs code (acceptance criteria):**

Pattern 3 (`StandaloneClassUserMethodStub` at `StandaloneClassUserMethods.cs`):
- Error: `CS0115: 'StandaloneClassUserMethodStub.Execute_(string)': no suitable method found to override` (line 55)
- Error: `CS0115: 'StandaloneClassUserMethodStub.Initialize_()': no suitable method found to override` (line 60)

Pattern 4 (`RepositoryUserMethodStub<T>` at `StandaloneClassUserMethods.cs`):
- Error: `CS0115: 'RepositoryUserMethodStub<T>.GetById_(int)': no suitable method found to override` (line 98)
- Error: `CS0115: 'RepositoryUserMethodStub<T>.Save_(T)': no suitable method found to override` (line 106)
- Error: `CS0115: 'RepositoryUserMethodStub<T>.GetDefault_()': no suitable method found to override` (line 115)

Pattern 4 exercises three additional concerns beyond pattern 3:
- Methods returning generic type `T?` (`GetById_`)
- Methods with generic type parameter `T` (`Save_`)
- Partial overload coverage (`GetDefault_()` overridden, `GetDefault_(string)` not overridden)

After implementation, this entire file must compile without errors.

### Breaking Changes

No. This is purely additive:
- Existing standalone class stubs without user method overrides generate identical code
- New virtual methods in the base class have default implementations (`{ }` for void, `=> default!` for non-void)
- No API changes to interceptor classes (user method support already built into `UnifiedInterceptorBuilder`)

### Pattern Consistency

The implementation follows the exact same pattern used for properties in the standalone class pipeline and for methods in the interface pipeline. No new patterns are introduced.

### Diagnostic Requirements

None needed (Decision 7). The feature is purely additive. `CS0115` is sufficient:
- Before implementation: user writes `protected override void MethodName_()`, gets CS0115 because base class lacks the virtual method
- After implementation: valid overrides compile; mismatched signatures still get CS0115
- No custom `KO` diagnostic required

### Codebase Deep-Dive

Files examined:
- `src/Generator/KnockOffGenerator.StandaloneClass.cs` -- Transform: missing `DetectUserOverrideMethods` call, `StandaloneClassStubInfo` missing `UserOverrideMethods`
- `src/Generator/KnockOffGenerator.Helpers.cs` -- `DetectUserOverrideMethods` helper already exists (lines 21-53)
- `src/Generator/Builder/StandaloneClassModelBuilder.cs` -- Builder: no user method detection, no `userMethodName` passed to `UnifiedInterceptorBuilder`
- `src/Generator/Builder/UnifiedInterceptorBuilder.cs` -- Already accepts `userMethodName` parameter (line 37)
- `src/Generator/Builder/FlatModelBuilder.cs` -- Working implementation for interface pipeline (lines 26-69)
- `src/Generator/Model/StandaloneClass/StandaloneClassGenerationUnit.cs` -- Missing `BaseClassMethods`
- `src/Generator/Model/StandaloneClass/BaseClassPropertyModel.cs` -- Template for `BaseClassMethodModel`
- `src/Generator/Model/Inline/InlineClassStubModel.cs` -- `InlineClassImplMethodModel` missing `HasUserOverride`
- `src/Generator/Renderer/StandaloneClassRenderer.cs` -- `RenderBaseClass()` only generates properties, `RenderImplMethodOverride()` does not handle user overrides
- `src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` -- Already handles `UserMethodFallback` option
- `src/Generator/Renderer/FlatRenderer.cs` -- Working implementation for interface pipeline user methods
- `src/Design/Design.Stubs/UserMethods/UserMethodBasics.cs` -- Working interface pipeline user method examples
- `src/Design/Design.Stubs/StubOverrideProperties/StubOverridePropertyBasics.cs` -- Working standalone class user property examples
- `src/Design/Design.Stubs/StubPatterns/AllPatterns.cs` -- Existing standalone class stub without user methods
- `src/Design/Design.Stubs/Generated/KnockOff.Generator/KnockOff.KnockOffGenerator/StandaloneServiceStub.Base.g.cs` -- Generated base class: only virtual properties, no methods
- `src/Design/Design.Stubs/Generated/KnockOff.Generator/KnockOff.KnockOffGenerator/StandaloneServiceStub.g.cs` -- Generated Impl: methods delegate to interceptor without user method support
- `src/Design/Design.Stubs/Generated/KnockOff.Generator/KnockOff.KnockOffGenerator/BasicUserMethodStub.g.cs` -- Interface pipeline: user method interceptor with `Invoke(Strict, this, args)` pattern
- `src/Design/Design.Domain/Abstractions/ServiceBase.cs` -- Target class: abstract Execute, virtual Initialize
- `src/Design/Design.Domain/Abstractions/ProcessorBase.cs` -- Target class: multiple overloads for testing

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-02-05

**Previous Concerns (now resolved):**
1. **Signature key matching for `ClassMemberInfo` is underspecified** -- RESOLVED in "Developer Concern 1" section. Extract `BuildOverrideSignatureKey` and `NormalizeTypeForOverrideMatching` into shared `SymbolHelpers` method. Both pipelines call the same code. Verified: `FlatModelBuilder` private method at line 1553 and `NormalizeTypeForOverrideMatching` at line 1577 are extractable. `InterfaceMemberInfo` and `ClassMemberInfo` share `Name` and `Parameters` fields.

2. **Partial overload coverage interceptor splitting is not described** -- RESOLVED in "Developer Concern 2" section. Use single interceptor with per-signature `UserMethodName` tracking via `MethodOverloadSignature.UserMethodName`. Verified: `BuildOverloadSignature()` at line 141 already accepts `userMethodName`. `RenderOverloadInvokeMethod()` at line 864 and 1004 already checks per-signature `overload.UserMethodName`. Option A (extend `MethodSignatureInfo`) is clean and backward-compatible.

**Codebase verification completed.** All plan claims confirmed against actual source files. Design.Stubs acceptance criteria verified (5 CS0115 errors at expected locations).

---

## Implementation Contract

**Created:** 2026-02-05
**Approved by:** knockoff-developer

### Design.Stubs Acceptance Criteria

These are the failing Design.Stubs files left by the architect. Implementation is done when they all compile.

- [ ] `src/Design/Design.Stubs/UserMethods/StandaloneClassUserMethods.cs:55` - Pattern 3: CS0115 `StandaloneClassUserMethodStub.Execute_(string)` must compile
- [ ] `src/Design/Design.Stubs/UserMethods/StandaloneClassUserMethods.cs:60` - Pattern 3: CS0115 `StandaloneClassUserMethodStub.Initialize_()` must compile
- [ ] `src/Design/Design.Stubs/UserMethods/StandaloneClassUserMethods.cs:98` - Pattern 4: CS0115 `RepositoryUserMethodStub<T>.GetById_(int)` must compile
- [ ] `src/Design/Design.Stubs/UserMethods/StandaloneClassUserMethods.cs:106` - Pattern 4: CS0115 `RepositoryUserMethodStub<T>.Save_(T)` must compile
- [ ] `src/Design/Design.Stubs/UserMethods/StandaloneClassUserMethods.cs:115` - Pattern 4: CS0115 `RepositoryUserMethodStub<T>.GetDefault_()` must compile

### In Scope

**Phase 1: Transform Layer**
- [ ] Add `DetectUserOverrideMethods(classSymbol)` call in `KnockOffGenerator.StandaloneClass.cs`
- [ ] Add `UserOverrideMethods` field to `StandaloneClassStubInfo` record
- [ ] Pass `UserOverrideMethods` to early return points (where `UserOverrideProperties: default` is used)
- [ ] **Checkpoint: `dotnet build src/KnockOff.sln` succeeds, no test changes**

**Phase 2: Model Layer**
- [ ] Create `BaseClassMethodModel.cs` in `src/Generator/Model/StandaloneClass/` (parallel to `BaseClassPropertyModel`)
- [ ] Add `EquatableArray<BaseClassMethodModel> BaseClassMethods` to `StandaloneClassGenerationUnit`
- [ ] Add `bool HasUserOverride = false` to `InlineClassImplMethodModel` record
- [ ] **Checkpoint: `dotnet build src/KnockOff.sln` succeeds, no test changes**

**Phase 3: Builder Layer**
- [ ] Extract `BuildOverrideSignatureKey(string, EquatableArray<ParameterInfo>)` and `NormalizeTypeForOverrideMatching` to `SymbolHelpers.cs`
- [ ] Update `FlatModelBuilder` to delegate to shared method (remove private duplicate)
- [ ] Add `string? UserMethodName = null` to `MethodSignatureInfo` record
- [ ] Update `UnifiedInterceptorBuilder.BuildOverloadSignature()` to use `sig.UserMethodName ?? userMethodName`
- [ ] Add `userOverrideMethods` HashSet in `StandaloneClassModelBuilder.Build()`
- [ ] Build per-signature user method names when creating `MethodSignatureInfo` for each overload
- [ ] Pass `userMethodName` to `UnifiedInterceptorBuilder.BuildMethodInterceptor()` when any overload has user override
- [ ] Set `HasUserOverride` on `InlineClassImplMethodModel` for methods with user overrides
- [ ] Build `BaseClassMethods` collection from class members
- [ ] **Checkpoint: `dotnet build src/KnockOff.sln` succeeds, all existing tests pass**

**Phase 4: Renderer -- Base Class Methods**
- [ ] Add `RenderBaseClassMethod()` method to `StandaloneClassRenderer`
- [ ] Update `RenderBaseClass()` to iterate `unit.BaseClassMethods` after properties
- [ ] **Checkpoint: `dotnet build src/Design/Design.Stubs` succeeds (CS0115 errors resolve)**

**Phase 5: Renderer -- Interceptor Options and Impl Method User Override**
- [ ] Set `UserMethodFallback` and `StubTypeName` on `InterceptorRenderOptions` when method has user method
- [ ] Update `RenderImplMethodOverride()` to branch on `HasUserOverride` -- pass `_stub` to `Invoke()`, skip unconfigured-count/base-call pattern
- [ ] **Checkpoint: `dotnet build src/KnockOff.sln` succeeds, all existing tests pass**
- [ ] **Checkpoint: Verify generated code for `StandaloneClassUserMethodStub` and `RepositoryUserMethodStub<T>` matches expected patterns**

**Phase 6: Test Coverage**
- [x] Test user method fallback for abstract methods (pattern 3)
- [x] Test user method fallback for virtual methods (pattern 3)
- [x] Test OnCall supersedes user method
- [x] Test Returns supersedes user method
- [x] Test When chains with user method fallback
- [x] Test Sequences with user method fallback
- [x] Test Verifiable on user method interceptors
- [x] Test generic standalone class with user methods (pattern 4)
- [x] Test partial overload coverage (pattern 4 -- GetDefault_ overridden, GetDefault_(string) not)
- [x] **Checkpoint: All new and existing tests pass**

**Phase 7: Documentation**
- [ ] Update `docs/guides/api-consistency-matrix.md` -- user methods section
- [ ] Expand `StandaloneClassUserMethods.cs` with documentation comments
- [ ] **Checkpoint: `dotnet build src/Design/Design.Stubs` succeeds**

### Explicitly Out of Scope

- Inline class user methods (by design, inline stubs cannot have user methods)
- User method support for indexers or events
- Custom KnockOff diagnostics (CS0115 is sufficient)
- Async user method testing (existing async interceptor paths handle this, but dedicated async target class methods are not in the domain classes; can be a follow-up)

### Verification Gates

1. After Phase 1-2: `dotnet build src/KnockOff.sln` succeeds, no test regressions
2. After Phase 3: `dotnet build src/KnockOff.sln` succeeds, all existing tests pass
3. After Phase 4: `dotnet build src/Design/Design.Stubs` succeeds (primary acceptance gate -- all 5 CS0115 errors resolve)
4. After Phase 5: All existing tests pass, generated code matches expected patterns
5. Final: All new tests pass, all existing tests pass, `dotnet build src/Design/Design.Stubs` succeeds

### Stop Conditions

If any of these occur, STOP and report:
- Out-of-scope test starts failing
- `InlineClassImplMethodModel.HasUserOverride` change causes regression in `ClassRenderer` (inline class pipeline)
- Signature key format mismatch between `DetectUserOverrideMethods` syntax-based keys and `BuildOverrideSignatureKey` semantic-based keys
- Generated code for stubs without user overrides changes (must be identical to current output)
- Architectural contradiction discovered between standalone class and interface pipeline user method behavior

---

## Implementation Progress

**Started:** 2026-02-05

### Phase 1: Transform Layer -- Add User Method Detection
- [x] Added `UserOverrideMethods` field to `StandaloneClassStubInfo` record
- [x] Added `DetectUserOverrideMethods(classSymbol)` call in `TransformStandaloneClassStub`
- [x] Passed `UserOverrideMethods: default` to all early return paths
- **Verification**: `dotnet build src/KnockOff.sln` succeeded

### Phase 2: Model Layer -- Add BaseClassMethodModel and HasUserOverride
- [x] Created `BaseClassMethodModel.cs` record
- [x] Added `BaseClassMethods` to `StandaloneClassGenerationUnit`
- [x] Added `HasUserOverride = false` to `InlineClassImplMethodModel` record
- **Verification**: `dotnet build src/KnockOff.sln` succeeded

### Phase 3: Builder Layer -- Wire User Method Detection
- [x] Extracted shared `BuildOverrideSignatureKey` and `NormalizeTypeForOverrideMatching` to `SymbolHelpers.cs`
- [x] Updated `FlatModelBuilder` to delegate to shared methods
- [x] Added `UserMethodName` to `MethodSignatureInfo` record
- [x] Set per-signature `UserMethodName` in `StandaloneClassModelBuilder` for overloads with user overrides
- [x] Set `HasUserOverride` on `InlineClassImplMethodModel` for methods with user overrides
- [x] Built `BaseClassMethods` collection from class members
- **Verification**: `dotnet build src/KnockOff.sln` succeeded, all existing tests pass

### Phase 4: Renderer -- Base Class Methods
- [x] Added `RenderBaseClassMethod()` to `StandaloneClassRenderer`
- [x] Updated `RenderBaseClass()` to iterate `BaseClassMethods`
- **Verification**: `dotnet build src/Design/Design.Stubs` succeeded (all 5 CS0115 errors resolved)

### Phase 5: Renderer -- Interceptor Options and Impl Method User Override
- [x] Updated method interceptor rendering to set `UserMethodFallback` and `StubTypeName` on `InterceptorRenderOptions` when model has user methods
- [x] Updated `RenderImplMethodOverride()` to branch on `HasUserOverride` -- pass `_stub` to `Invoke()`, skip unconfigured-count/base-call pattern
- [x] **Architectural fix**: Standalone class interceptors are top-level (not nested in stub), so they cannot access `protected virtual` methods on the base class. Added internal forwarding methods (e.g., `internal void __UserMethod_Execute(string command) => Execute_(command)`) on the partial stub class. Builder uses `__UserMethod_{Name}` convention for `UserMethodName`.
- [x] Fixed `BuildOverloadSignature()` in `UnifiedInterceptorBuilder`: removed `sig.UserMethodName ?? userMethodName` coalescing that incorrectly gave non-overridden overloads a user method name in partial overload coverage scenarios
- **Verification**: `dotnet build src/Design/Design.Stubs` succeeded, all existing tests pass, generated code matches expected patterns

### Verification Summary (Phases 1-5)

| Check | Result |
|-------|--------|
| `dotnet build src/KnockOff.sln` | PASS |
| `dotnet build src/Design/Design.Stubs` | PASS (all CS0115 errors resolved) |
| KnockOffTests | 1114-1115 pass (net8/9/10) |
| NeatooInterfaceTests | 473 pass (net8/9/10) |
| AssemblyStrict | 14 pass (net8/9/10) |
| Documentation.Samples | 456/457 pass (1 pre-existing failure: `UpdateTest_KnockOff`) |
| Stubs without user overrides unchanged | Verified -- no `__UserMethod_` in `StandaloneServiceStub.g.cs` |
| Interceptors match expected patterns | Verified -- `stub` param on user methods only |
| Impl methods branch correctly | Verified -- `HasUserOverride` routes to `_stub` pattern |
| Partial overload coverage | Verified -- `GetDefault()` has user override, `GetDefault(string)` uses standard pattern |

### Phase 6: Test Coverage
- [x] Test user method fallback for abstract methods (pattern 3) -- 3 tests
- [x] Test user method fallback for virtual methods (pattern 3) -- 2 tests
- [x] Test OnCall supersedes user method -- 2 tests (void + non-void)
- [x] Test Returns supersedes user method -- 1 test
- [x] Test When chains with user method fallback -- 5 tests (match, no-match, ThenWhen chain, void, priority over OnCall)
- [x] Test Sequences with user method fallback -- 3 tests (OnCall sequence, Returns sequence, void sequence)
- [x] Test Verifiable on user method interceptors -- 7 tests (called/not-called, Times.Exactly, void, individual verify, LastArg)
- [x] Test generic standalone class with user methods (pattern 4) -- 4 tests (return T?, void with T param, OnCall supersedes, Returns supersedes, Verifiable)
- [x] Test partial overload coverage (pattern 4) -- 4 tests (overridden uses user method, non-overridden delegates to base, OnCall on non-overridden, each overload independent)
- [x] Additional: Strict mode interaction -- 2 tests (user method bypasses strict, no user override throws)
- [x] Additional: Reset and ResetInterceptors -- 2 tests
- **Verification**: All 39 new tests pass, all existing tests pass (net8.0: 1153, net9.0: 1154, net10.0: 1154)

### Phase 7: Documentation
- [x] Updated `docs/guides/api-consistency-matrix.md` -- user methods section expanded for standalone class patterns (3, 4)
- [x] Updated `docs/guides/stub-overrides.md` -- availability note corrected to include all four standalone patterns
- [x] Expanded `src/Design/Design.Stubs/UserMethods/StandaloneClassUserMethods.cs` with documentation comments (replaced acceptance criteria language with working-feature documentation)
- **Verification**: `dotnet build src/Design/Design.Stubs` succeeded (0 warnings, 0 errors)

---

## Completion Evidence

- **All Phases Complete:** Phases 1-7 implemented
- **Tests Passing:** 39 new tests, all existing tests pass (net8.0: 1153, net9.0: 1154, net10.0: 1154)
- **Design.Stubs Compile:** Yes (0 warnings, 0 errors)
- **All Contract Items:** Confirmed complete
