# Class Stub Overload Consistency Design

**Date:** 2026-02-03
**Related Todo:** [Class Stub Overload API Consistency](../todos/class-stub-overload-consistency.md)
**Status:** Complete
**Last Updated:** 2026-02-03

---

## Overview

Unify the overload API for class-based stubs (Inline Class and Open Generic Class) to match the interface stub pattern. Replace numbered interceptors (`GetDefault1`, `GetDefault2`) with a single interceptor that has multiple `OnCall` overloads.

---

## Approach

Modify `ClassModelBuilder.cs` to leverage `UnifiedInterceptorBuilder.BuildMethodInterceptor()` for method interceptor generation, matching the interface stub approach in `InlineModelBuilder.cs`.

**Key insight:** Interface stubs already have the correct pattern. The fix is to make class stubs follow the same code path.

---

## Design

### Current Architecture (Class Stubs)

```
ClassModelBuilder.GroupMethodsByName()
    └── Returns simple groups without compatibility checks

ClassModelBuilder.Build()
    └── Iterates groups, creates numbered handlers for overloads
        ├── GetDefault1 → InlineClassMethodModel
        └── GetDefault2 → InlineClassMethodModel
```

### Target Architecture (Matches Interface Stubs)

```
ClassModelBuilder.GroupMethodsByName()
    └── Returns groups WITH compatibility checks (same as InlineModelBuilder)

ClassModelBuilder.Build()
    └── Uses UnifiedInterceptorBuilder.BuildMethodInterceptor()
        ├── Compatible overloads → Single interceptor with multiple OnCall
        └── Incompatible overloads → Numbered interceptors (fallback)
```

### File Changes

#### 1. `src/Generator/Builder/ClassModelBuilder.cs`

**Changes:**
- Replace `GroupMethodsByName()` with compatible-aware version from `InlineModelBuilder`
- Replace `BuildMethodModel()` to use `UnifiedInterceptorBuilder.BuildMethodInterceptor()`
- Update method loop to handle `UnifiedMethodInterceptorModel` instead of creating numbered models
- Update `BuildImplMethodModel()` to generate signature-based `Invoke` calls

**Current grouping (lines 404-430):**
```csharp
// Groups methods but does NOT check compatibility
var tempGroups = new Dictionary<string, List<ClassMemberInfo>>();
foreach (var method in methods) { ... }
```

**New grouping (based on InlineModelBuilder lines 1237-1288):**
```csharp
// Groups methods AND checks compatibility
// Incompatible → numbered groups
// Compatible → single group with combined params
```

#### 2. `src/Generator/Model/Inline/InlineClassStubModel.cs`

**Changes:**
- Replace `EquatableArray<InlineClassMethodModel> Methods` with `EquatableArray<UnifiedMethodInterceptorModel> Methods`
- This enables the model to carry overload information

#### 3. `src/Generator/Renderer/ClassRenderer.cs` (or equivalent)

**Changes:**
- Update method interceptor rendering to handle multi-overload case
- Generate `OnCall` overloads per signature (delegate per signature, invoke per signature)
- Generate signature-suffixed `Invoke_XXX` methods

#### 4. Impl Class Generation

**Current pattern (numbered):**
```csharp
public override T? GetDefault()
{
    if (_stub.GetDefault1.IsConfigured) {
        var result = _stub.GetDefault1.Invoke(_stub.Strict, out var handled);
        if (handled) return result;
    }
    return base.GetDefault();
}
```

**New pattern (signature-based):**
```csharp
public override T? GetDefault()
{
    return _stub.GetDefault.Invoke_NoParams_TNullable(_stub.Strict);
}

public override T? GetDefault(string filter)
{
    return _stub.GetDefault.Invoke_String_TNullable(_stub.Strict, filter);
}
```

### Compatibility Rules (from InlineModelBuilder)

Methods can share an interceptor when:
1. Parameter names with same types across overloads (or new parameters)
2. Return types match OR parameters are identical (BCL pattern like `IEnumerable`)

Methods get numbered interceptors when:
1. Same parameter name has different types across overloads
2. Different return types with different parameter sets

**Example - Compatible (single interceptor):**
```csharp
T? GetDefault()           // 0 params
T? GetDefault(string f)   // 1 param, no overlap
```

**Example - Incompatible (numbered interceptors):**
```csharp
string Process(int value)   // returns string
bool Process(string text)   // returns bool, different param type
// → Process1, Process2
```

---

## Implementation Steps

### Phase 1: Model Updates

1. Update `InlineClassStubModel` to use `UnifiedMethodInterceptorModel` for methods
2. Create or update any supporting types needed for class stub overload signatures
3. Ensure `MethodOverloadSignature` works for class stubs (may need adjustments for virtual method context)

### Phase 2: Builder Updates

1. Port `AreAllOverloadsCompatible()` and `AreMethodsCompatibleForSharedInterceptor()` from `InlineModelBuilder` to `ClassModelBuilder` (or extract to shared helper)
2. Update `GroupMethodsByName()` to use compatibility checks
3. Replace `BuildMethodModel()` to call `UnifiedInterceptorBuilder.BuildMethodInterceptor()`
4. Update method iteration in `Build()` to handle groups vs individual methods

### Phase 3: Renderer Updates

1. Update class method interceptor rendering to handle `UnifiedMethodInterceptorModel.Overloads`
2. Generate per-signature delegates, fields, OnCall methods, and Invoke methods
3. Update Impl class method overrides to use signature-based Invoke

### Phase 4: Test Updates

1. Update `OpenGenericClassOverloadTests.cs` to use new API (`stub.GetDefault` instead of `stub.GetDefault1`/`GetDefault2`)
2. Add explicit Inline Class overload tests
3. Add incompatibility edge case tests (verify numbered fallback still works)

---

## Acceptance Criteria

- [ ] Class stubs with compatible overloads generate single interceptor with multiple `OnCall` overloads
- [ ] Class stubs with incompatible overloads still generate numbered interceptors (fallback)
- [ ] API matches interface stubs: `stub.Method.OnCall((params) => ...)` not `stub.Method1.OnCall(...)`
- [ ] All six patterns continue to work
- [ ] Existing tests pass (after API updates)
- [ ] New tests explicitly cover both Inline Class and Open Generic Class patterns

---

## Dependencies

- `UnifiedInterceptorBuilder` already handles the complex overload logic
- `MethodOverloadSignature` model exists and is used by interface stubs
- Renderer patterns for multi-overload interceptors exist in interface stub rendering

---

## Risks / Considerations

### Breaking Change

This is a full breaking change for class stubs with overloaded methods:
- `stub.Method1` / `stub.Method2` → `stub.Method` with overloaded `OnCall`
- Users must update their test code

**Mitigation:** Document in release notes, provide migration guide.

### Compatibility Fallback

When methods are incompatible (different return types, conflicting parameter types), numbered interceptors are still generated. This is consistent with interface stub behavior.

### Edge Case: Abstract vs Virtual

Class stubs handle both abstract and virtual methods. Ensure the `Invoke` pattern correctly calls `base.Method()` for virtual methods that fall through.

---

## Architectural Verification

### Six Patterns Analysis

| Pattern | Impact | Notes |
|---------|--------|-------|
| Standalone | N/A | Interface-based, already works correctly |
| Generic Standalone | N/A | Interface-based, already works correctly |
| Inline Interface | N/A | Already works correctly |
| Inline Class | **FIX** | Will use unified overload pattern |
| Inline Delegate | N/A | No overloads in delegate pattern |
| Open Generic | **FIX** | Shares code path with Inline Class |

### Breaking Changes

**Yes** - Full breaking change as approved by user:
- Numbered interceptors (`GetDefault1`, `GetDefault2`) removed for compatible overloads
- Users must update to `stub.Method.OnCall(...)` with appropriate lambda signatures

### Pattern Consistency

**Follows existing patterns:**
- Uses `UnifiedInterceptorBuilder` already proven for interface stubs
- Same compatibility rules as interface stubs
- Same `Invoke_XXX` signature suffix pattern
- Same `OnCall` overload resolution via C# lambda parameter types

### Diagnostic Requirements

No new diagnostics needed. Existing diagnostics for class stubs remain unchanged.

### Test Strategy

1. Update `OpenGenericClassOverloadTests.OpenGenericClass_GetDefault_Overloads()` to new API
2. Add `InlineClassOverloadTests` for Inline Class pattern
3. Add edge case tests:
   - Compatible overloads (different param counts)
   - Compatible overloads (different param names)
   - Incompatible overloads (different return types) → verify numbered fallback
   - Incompatible overloads (same param name, different type) → verify numbered fallback

### Edge Cases

1. **Multiple overload groups:** Class has `GetDefault()` (compatible) and `Process()` (incompatible) - handle independently
2. **Generic type parameters:** Ensure `T?` return types work with signature suffix generation
3. **Abstract methods:** No `base.Method()` call available - must throw if unconfigured in strict mode
4. **Virtual methods with default impl:** Can fall through to `base.Method()` if unconfigured

### Codebase Analysis

**Files examined:**
- `/home/keithvoels/neatoodotnet/KnockOff/src/Generator/Builder/ClassModelBuilder.cs` - Current class stub builder (root cause)
- `/home/keithvoels/neatoodotnet/KnockOff/src/Generator/Builder/InlineModelBuilder.cs` - Interface stub builder (target pattern)
- `/home/keithvoels/neatoodotnet/KnockOff/src/Generator/Builder/UnifiedInterceptorBuilder.cs` - Shared overload logic
- `/home/keithvoels/neatoodotnet/KnockOff/src/Design/Design.Stubs/Methods/MethodOverloads.cs` - API design documentation
- `/home/keithvoels/neatoodotnet/KnockOff/src/Design/Design.Tests/GenericOverloadTests/OpenGenericOverloadTests.cs` - Existing tests showing issue
- Generated code for both interface and class stubs (compared patterns)

**Patterns found:**
- Interface stubs group overloads into `MethodGroupInfo`, check compatibility, build `UnifiedMethodInterceptorModel`
- Class stubs bypass this and create numbered `InlineClassMethodModel` directly
- Fix: Make class stubs follow the interface stub pattern by using the unified builder

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-02-03
**Re-reviewed:** 2026-02-03

### My Understanding of This Plan

**Core Change:** Modify class stub generation to produce single interceptors with multiple `OnCall` overloads (matching interface stubs), instead of numbered interceptors (`GetDefault1`, `GetDefault2`).

**User-Facing API:** `stub.GetDefault.OnCall(() => ...)` and `stub.GetDefault.OnCall((filter) => ...)` instead of `stub.GetDefault1.OnCall(...)` and `stub.GetDefault2.OnCall(...)`.

**Internal Changes:**
1. Update `ClassModelBuilder` to use `UnifiedInterceptorBuilder.BuildMethodInterceptor()`
2. Change `InlineClassStubModel.Methods` from `EquatableArray<InlineClassMethodModel>` to `EquatableArray<UnifiedMethodInterceptorModel>`
3. Update `ClassRenderer` to use `MethodInterceptorRenderer` for method rendering
4. Update Impl class generation to call signature-based `Invoke_XXX` methods

**Patterns Affected:** Inline Class and Open Generic Class only (4 patterns unaffected).

### Codebase Investigation

**Files Examined:**
- `/home/keithvoels/neatoodotnet/KnockOff/src/Generator/Builder/ClassModelBuilder.cs` - Confirmed current numbered handler pattern at lines 85-100, `GroupMethodsByName` lacks compatibility checks
- `/home/keithvoels/neatoodotnet/KnockOff/src/Generator/Builder/InlineModelBuilder.cs` (lines 1200-1400) - Target pattern for compatibility checking
- `/home/keithvoels/neatoodotnet/KnockOff/src/Generator/Builder/UnifiedInterceptorBuilder.cs` - Confirmed `BuildMethodInterceptor()` API
- `/home/keithvoels/neatoodotnet/KnockOff/src/Generator/Renderer/ClassRenderer.cs` - Has ~300 lines of method rendering that duplicates `MethodInterceptorRenderer`
- `/home/keithvoels/neatoodotnet/KnockOff/src/Generator/Renderer/Shared/MethodInterceptorRenderer.cs` - Has `RenderOverloadInvokeMethod()` for multi-overload pattern
- `/home/keithvoels/neatoodotnet/KnockOff/src/Generator/Model/Inline/InlineClassStubModel.cs` - `InlineClassMethodModel` lacks Overloads collection

**Searches Performed:**
- `MethodInterceptorRenderer` - used by FlatRenderer and InlineRenderer, NOT ClassRenderer
- `BuildMethodInterceptor` - InlineModelBuilder calls it (lines 363, 383), ClassModelBuilder does not

**Discrepancies Found:**
1. Plan says "ClassRenderer (or equivalent)" but doesn't acknowledge ClassRenderer has its own ~300-line method rendering
2. Ripple effect on `InlineClassImplMethodModel` not fully detailed

### Concerns

#### Concern 1: Missing `MethodSignatureInfo` Construction Details

**Category:** Implementation Gap

`UnifiedInterceptorBuilder.BuildMethodInterceptor()` requires `IReadOnlyList<MethodSignatureInfo>`. Interface stubs construct this from `InterfaceMemberInfo`. Class stubs use `ClassMemberInfo`. The plan doesn't specify the conversion.

**Question:** How should `ClassMemberInfo` be converted to `MethodSignatureInfo`? Should a helper method be created?

#### Concern 2: `InlineClassImplMethodModel` Needs Signature Suffix

**Category:** Missing Model Change

For multi-overload interceptors, Impl class methods need to call `Invoke_NoParams_TNullable` or `Invoke_String_TNullable`. `InlineClassImplMethodModel` has no field for signature suffix.

**Question:** Should `InlineClassImplMethodModel` be extended with a `SignatureSuffix` field?

#### Concern 3: ClassRenderer Method Rendering Replacement Not Detailed

**Category:** Scope Underestimate

`ClassRenderer.cs` contains ~300 lines of method interceptor rendering (`RenderMethodInterceptorClass`, `RenderWhenMatcherClasses`, etc.) that duplicates `MethodInterceptorRenderer`.

**Question:** Is the intent to delete ClassRenderer's method rendering and use `MethodInterceptorRenderer.RenderInterceptorClass()` instead?

**Suggestion:** Add explicit statement: "Replace `RenderMethodInterceptorClass` and related methods in ClassRenderer.cs with call to `MethodInterceptorRenderer.RenderInterceptorClass()`."

#### Concern 4: `InlineInterceptorPropertyModel` for Overload Groups

**Category:** Missing Detail

Currently one `InlineInterceptorPropertyModel` is created per method. For overload groups, there should be ONE per group.

**Question:** With overload groups, do we generate one `InlineInterceptorPropertyModel` per group?

#### Concern 5: Compatibility Check Methods Location

**Category:** Architectural Ambiguity

Plan says "Port or extract" compatibility methods. These operate on different input types (`InterfaceMemberInfo` vs `ClassMemberInfo`).

**Question:** Should we (A) create parallel methods for `ClassMemberInfo`, (B) create generic helpers, or (C) convert to common intermediate type?

### What Looks Good

- Clear identification of root cause
- Correct target architecture (uses existing UnifiedInterceptorBuilder)
- Proper handling of incompatible overloads (numbered fallback)
- Test strategy covers both compatible and incompatible cases
- Breaking change properly acknowledged

### Recommendation

~~Send back to architect to address concerns before implementation.~~

**UPDATE 2026-02-03:** Architect has addressed all concerns. See "Architect Responses" below.

### Re-Review Summary (2026-02-03)

All 5 concerns have been adequately addressed:

1. **MethodSignatureInfo Construction** - Full `ToMethodSignatureInfo()` helper with complete code
2. **InlineClassImplMethodModel Signature Suffix** - `InvokeSuffix` field added with builder/renderer code
3. **ClassRenderer Replacement** - Confirmed: 7 methods (~250 lines) to remove, use shared renderer
4. **InterceptorPropertyModel for Groups** - Clear before/after code showing one per group
5. **Compatibility Check Location** - Parallel implementation with full method code

**Verdict:** Plan is now complete and ready for implementation.

---

## Architect Responses to Developer Concerns

### Response to Concern 1: `MethodSignatureInfo` Construction

**Decision:** Create a helper method in `ClassModelBuilder` to convert `ClassMemberInfo` to `MethodSignatureInfo`.

**Implementation:**

```csharp
// In ClassModelBuilder.cs
private static MethodSignatureInfo ToMethodSignatureInfo(ClassMemberInfo member, string returnType)
{
    var parameters = member.Parameters
        .Select(p => new ParameterModel(
            Name: p.Name,
            EscapedName: EscapeIdentifier(p.Name),
            Type: p.Type,
            NullableType: MakeNullable(p.Type),
            RefKind: p.RefKind,
            RefPrefix: GetRefKindPrefix(p.RefKind)))
        .ToEquatableArray();

    var trackableParams = UnifiedInterceptorBuilder.GetTrackableParameters(parameters);
    var hasRefOrOut = parameters.Any(p => p.RefKind == RefKind.Ref || p.RefKind == RefKind.Out);
    var isVoid = returnType == "void";
    var defaultExpr = isVoid ? "" : "default!";

    return new MethodSignatureInfo(
        Parameters: parameters,
        TrackableParameters: trackableParams,
        ParameterDeclarations: UnifiedInterceptorBuilder.BuildParameterDeclarations(parameters),
        ReturnType: returnType,
        IsVoid: isVoid,
        HasRefOrOutParams: hasRefOrOut,
        DefaultExpression: defaultExpr,
        ThrowsOnDefault: false);
}
```

**Usage in Build():**

```csharp
foreach (var group in methodGroups.Values)
{
    var signatures = group.Members
        .Select(m => ToMethodSignatureInfo(m, group.ReturnType))
        .ToList();

    var interceptorModel = UnifiedInterceptorBuilder.BuildMethodInterceptor(
        interceptorClassName: $"{stubClassName}_{group.Name}Interceptor",
        methodName: group.Name,
        declaringInterface: "",  // Class stubs don't have declaring interface
        ownerClassName: $"Stubs.{stubClassName}{typeParamList}",
        ownerTypeParameters: "",
        overloads: signatures);

    methods.Add(interceptorModel);
}
```

### Response to Concern 2: `InlineClassImplMethodModel` Signature Suffix

**Decision:** Add `InvokeSuffix` field to `InlineClassImplMethodModel`.

**Model Change:**

```csharp
// In InlineClassStubModel.cs
internal sealed record InlineClassImplMethodModel(
    string HandlerName,           // Interceptor property name (e.g., "GetDefault")
    string MethodName,            // Actual method name (same for overloads)
    string ReturnType,
    string AccessModifier,
    bool IsVoid,
    bool IsTask,
    bool IsValueTask,
    bool IsAbstract,
    string ParameterDeclarations,
    string ArgumentList,
    string InputArgumentList,
    string OnCallArgumentList,
    string InvokeSuffix);         // NEW: e.g., "_NoParams_TNullable" or "_String_TNullable"
```

**Builder Change in `ClassModelBuilder.BuildImplMethodModel()`:**

```csharp
private static InlineClassImplMethodModel BuildImplMethodModel(
    ClassMemberInfo member,
    string handlerName,
    string invokeSuffix)  // NEW parameter
{
    // ... existing code ...

    return new InlineClassImplMethodModel(
        HandlerName: handlerName,
        MethodName: member.Name,
        // ... other fields ...
        InvokeSuffix: invokeSuffix);  // NEW
}
```

**Renderer Change in `ClassRenderer.RenderImplMethodOverride()`:**

```csharp
// For single-signature interceptors (Overloads.Count == 0):
var invokeArgs = "_stub.Strict, out var handled, " + method.InputArgumentList;
w.Line($"var result = _stub.{method.HandlerName}.Invoke({invokeArgs});");

// For multi-overload interceptors:
var invokeArgs = "_stub.Strict, " + method.InputArgumentList;
w.Line($"return _stub.{method.HandlerName}.Invoke{method.InvokeSuffix}({invokeArgs});");
```

### Response to Concern 3: ClassRenderer Method Rendering

**Decision:** Replace ClassRenderer's method interceptor rendering with calls to `MethodInterceptorRenderer.RenderInterceptorClass()`.

**Rationale:** ClassRenderer currently has ~300 lines duplicating MethodInterceptorRenderer. Using the shared renderer:
1. Eliminates code duplication
2. Ensures feature parity (sequences, When chains, verification)
3. Future improvements apply to all patterns

**Implementation:**

```csharp
// In ClassRenderer.Render(), replace:
foreach (var method in cls.Methods)
{
    RenderMethodInterceptorClass(w, method, cls.StubClassName, indent);
}

// With:
foreach (var method in cls.Methods)
{
    var options = new InterceptorRenderOptions(
        BaseIndent: 2,
        IncludeStrictParameter: true,
        StrictAccessExpression: "strict",
        InterceptorTypeParameters: cls.TypeParameterList,
        InterceptorConstraints: cls.ConstraintClauses);
    w.SetIndent(2);
    MethodInterceptorRenderer.RenderInterceptorClass(w, method, options);
}
```

**Methods to Remove from ClassRenderer:**
- `RenderMethodInterceptorClass()`
- `RenderMethodInterceptorInvoke()`
- `RenderWhenMatcherClasses()`
- `RenderWhenBuilderClass()`
- `RenderWhenChainClass()`
- `RenderVoidWhenMatcherClasses()`
- `RenderVoidWhenChainClass()`

**Impact:** ~250 lines removed from ClassRenderer.

### Response to Concern 4: `InlineInterceptorPropertyModel` for Groups

**Decision:** Yes, generate ONE `InlineInterceptorPropertyModel` per method group, not per overload.

**Current (problematic for overloads):**
```csharp
// Creates GetDefault1, GetDefault2 properties
for (int i = 0; i < group.Members.Count; i++)
{
    var handlerName = hasOverloads ? $"{group.Name}{i + 1}" : group.Name;
    interceptorProperties.Add(new InlineInterceptorPropertyModel(
        PropertyName: handlerName,
        ...));
}
```

**New (one per group):**
```csharp
// Creates single GetDefault property for the group
foreach (var group in methodGroups.Values)
{
    interceptorProperties.Add(new InlineInterceptorPropertyModel(
        PropertyName: group.Name,  // e.g., "GetDefault"
        InterceptorTypeName: $"{method.InterceptorClassName}{typeParamList}",
        NeedsNewKeyword: false,
        Description: $"Interceptor for {group.Name}."));
    resetStatements.Add($"{group.Name}.Reset();");
}
```

**Note:** The incompatibility fallback still creates numbered groups (`Process1`, `Process2`), each with its own interceptor property.

### Response to Concern 5: Compatibility Check Methods Location

**Decision:** Create parallel implementation in `ClassModelBuilder` using `ClassMemberInfo`.

**Rationale:**
- Converting to common type adds complexity with no benefit
- Generic helpers would require abstraction overhead
- Parallel implementation is straightforward and maintains clear ownership

**Implementation:**

```csharp
// In ClassModelBuilder.cs (parallel to InlineModelBuilder methods)

private static bool AreMethodsCompatibleForSharedInterceptor(ClassMemberInfo m1, ClassMemberInfo m2)
{
    // Check if shared parameter names have different types
    var m1Params = m1.Parameters.ToDictionary(p => p.Name, p => p.Type);
    foreach (var p2 in m2.Parameters)
    {
        if (m1Params.TryGetValue(p2.Name, out var m1Type) && m1Type != p2.Type)
            return false;
    }

    // Different return types with different parameter sets need separate interceptors
    if (m1.ReturnType != m2.ReturnType)
    {
        var m1ParamNames = new HashSet<string>(m1.Parameters.Select(p => p.Name));
        var m2ParamNames = new HashSet<string>(m2.Parameters.Select(p => p.Name));
        if (!m1ParamNames.SetEquals(m2ParamNames))
            return false;
    }

    return true;
}

private static bool AreAllOverloadsCompatible(List<ClassMemberInfo> overloads)
{
    for (int i = 0; i < overloads.Count; i++)
    {
        for (int j = i + 1; j < overloads.Count; j++)
        {
            if (!AreMethodsCompatibleForSharedInterceptor(overloads[i], overloads[j]))
                return false;
        }
    }
    return true;
}
```

**Alternative Considered (Rejected):** Extract shared generic interface `IMethodInfo` with properties `Parameters`, `ReturnType`. Rejected because:
- Adds interface/adapter overhead
- Types are already similar enough that parallel code is cleaner
- Both implementations are ~20 lines each

---

## Implementation Contract

**Created:** 2026-02-03
**Approved by:** knockoff-developer

### In Scope

#### Phase 1: Model Updates

- [ ] `src/Generator/Model/Inline/InlineClassStubModel.cs`:
  - [ ] Change `EquatableArray<InlineClassMethodModel> Methods` to `EquatableArray<UnifiedMethodInterceptorModel> Methods`
  - [ ] Add `InvokeSuffix` field to `InlineClassImplMethodModel` record

- [ ] **Checkpoint:** Project compiles (will have errors in ClassModelBuilder/ClassRenderer until Phase 2-3)

#### Phase 2: Builder Updates

- [ ] `src/Generator/Builder/ClassModelBuilder.cs`:
  - [ ] Add `ToMethodSignatureInfo(ClassMemberInfo, string)` helper method
  - [ ] Add `AreMethodsCompatibleForSharedInterceptor(ClassMemberInfo, ClassMemberInfo)` method
  - [ ] Add `AreAllOverloadsCompatible(List<ClassMemberInfo>)` method
  - [ ] Update `GroupMethodsByName()` to check compatibility and create numbered groups for incompatible overloads
  - [ ] Update `Build()` method loop to call `UnifiedInterceptorBuilder.BuildMethodInterceptor()`
  - [ ] Update interceptor property generation: one per method group (not per overload)
  - [ ] Update reset statements: one per method group
  - [ ] Update `BuildImplMethodModel()` to accept and store `invokeSuffix` parameter
  - [ ] Remove old `BuildMethodModel()` (no longer needed)

- [ ] **Checkpoint:** Project compiles, ClassModelBuilder produces `UnifiedMethodInterceptorModel`

#### Phase 3: Renderer Updates

- [ ] `src/Generator/Renderer/ClassRenderer.cs`:
  - [ ] Replace method interceptor rendering loop with `MethodInterceptorRenderer.RenderInterceptorClass()`
  - [ ] Delete `RenderMethodInterceptorClass()` method
  - [ ] Delete `RenderMethodInterceptorInvoke()` method
  - [ ] Delete `RenderWhenMatcherClasses()` method
  - [ ] Delete `RenderWhenBuilderClass()` method
  - [ ] Delete `RenderWhenChainClass()` method
  - [ ] Delete `RenderVoidWhenMatcherClasses()` method
  - [ ] Delete `RenderVoidWhenChainClass()` method
  - [ ] Delete `GetAsyncTypeInfoForMethod()` helper (if only used by deleted methods)
  - [ ] Update `RenderImplMethodOverride()` to use `InvokeSuffix` for multi-overload interceptors

- [ ] **Checkpoint:** Project compiles, generated code compiles

#### Phase 4: Test Updates

- [x] `src/Design/Design.Tests/GenericOverloadTests/OpenGenericOverloadTests.cs`:
  - [x] Update `OpenGenericClass_GetDefault_Overloads()` test: change `stub.GetDefault1`/`stub.GetDefault2` to `stub.GetDefault.OnCall(...)` with appropriate lambda signatures
  - **Note:** Already updated in Phase 3 - uses new API

- [x] Create `src/Design/Design.Tests/GenericOverloadTests/InlineClassOverloadTests.cs`:
  - [x] Add test for Inline Class with compatible overloads (Process methods)
  - [x] Add test for Inline Class with incompatible overloads (Transform methods - numbered fallback)

- [x] Add edge case tests (can be in same file or separate):
  - [x] Compatible overloads with different param counts (Process: 0, 1, 2 params)
  - [x] Compatible overloads with different param names (no overlap) (Calculate: x, x+y)
  - [x] Incompatible: different return types with different params -> numbered (Transform1, Transform2)
  - [x] Strict mode behavior with unconfigured overloads
  - [x] Base fallback for unconfigured overloads
  - [x] When matching with overloads
  - [x] Sequence chaining per overload

- [x] **Checkpoint:** All tests pass (188 Design.Tests, 4000+ total)

#### Phase 5: Verification

- [x] Run full test suite: `dotnet test src/KnockOff.sln`
- [x] Verify generated code for class stub with overloads shows single interceptor with multiple `OnCall` overloads
- [x] Verify generated code for class stub with incompatible overloads shows numbered interceptors

### Explicitly Out of Scope

- **Standalone patterns** - Not affected, already use interface stub pattern
- **Generic Standalone patterns** - Not affected, already use interface stub pattern
- **Inline Interface patterns** - Not affected, already work correctly
- **Inline Delegate patterns** - Not affected, no overloads
- **Properties/Indexers/Events** - Not affected, only methods change
- **New diagnostics** - Not needed
- **InlineModelBuilder changes** - Already works correctly for interfaces
- **Shared renderer modifications** - Using existing `MethodInterceptorRenderer` as-is

### Verification Gates

1. **After Phase 1:** Model files compile. Some dependent files will have errors (expected).
2. **After Phase 2:** Builder produces `UnifiedMethodInterceptorModel` for class stubs. Renderer may still have errors.
3. **After Phase 3:** Full project compiles. Generated code compiles. Existing tests may fail due to API change.
4. **After Phase 4:** All tests pass with new API.
5. **Final:** Full test suite passes, generated code shows correct pattern.

### Stop Conditions

If any of these occur, STOP and report:
- **Out-of-scope test fails** that wasn't failing before (e.g., interface stub tests, property tests)
- **Architectural contradiction** discovered (e.g., `MethodInterceptorRenderer` can't handle class stub requirements)
- **Generated code doesn't compile** after Phase 3
- **Breaking changes to interface stubs** detected (should not happen)

---

## Implementation Progress

### Phase 4: Test Updates (2026-02-03)

**Completed by:** knockoff-developer

**Work performed:**
1. Verified `OpenGenericOverloadTests.cs` already uses new API (updated in Phase 3)
2. Created `ProcessorBase.cs` in `Design.Domain.Abstractions` - non-generic abstract class with:
   - Compatible overloads: `Process()`, `Process(string)`, `Process(string, int)`
   - Incompatible overloads: `Transform(int) -> string`, `Transform(string) -> int`
   - Additional compatible: `Calculate(int)`, `Calculate(int, int)`
3. Created `InlineClassOverloadTests.cs` with 11 tests covering:
   - Single interceptor with multiple `OnCall` for compatible overloads
   - Per-overload tracking and total tracking
   - When matching with overloads
   - Sequence chaining per overload
   - Numbered interceptors for incompatible overloads
   - Base fallback for unconfigured overloads
   - Strict mode behavior
   - Property stubbing (verifies class stub basics still work)
4. Ran full test suite - all tests pass

**No numbered API tests found for class stubs:**
- Searched for `GetDefault[0-9]` and `.[A-Z][a-zA-Z]+[0-9].OnCall` patterns
- Found usages only in interface stubs with intentionally incompatible overloads (different return types)
- These are correct - numbered interceptors are the expected behavior for incompatible overloads

---

## Completion Evidence

### Tests Passing

```
Design.Tests: Passed! - Failed: 0, Passed: 188, Skipped: 0 (net8.0, net9.0, net10.0)
KnockOffTests: Passed! - Failed: 0, Passed: 1033 (net9.0, net10.0), 1032 (net8.0)
KnockOff.NeatooInterfaceTests: Passed! - Failed: 0, Passed: 473
KnockOff.Documentation.Samples: Passed! - Failed: 0, Passed: 406
KnockOffTests.AssemblyStrict: Passed! - Failed: 0, Passed: 14
```

### Generated Code Sample

**Compatible overloads (single interceptor):**
```csharp
public sealed class ProcessorBase_ProcessInterceptor
{
    // Single interceptor with multiple OnCall overloads
    public delegate string ProcessDelegate_NoParams_String();
    public delegate string ProcessDelegate_String_String(string message);
    public delegate string ProcessDelegate_String_Int32_String(string message, int priority);

    public MethodCallBuilderImpl_NoParams_String OnCall(ProcessDelegate_NoParams_String callback) { ... }
    public MethodCallBuilderImpl_String_String OnCall(ProcessDelegate_String_String callback) { ... }
    public MethodCallBuilderImpl_String_Int32_String OnCall(ProcessDelegate_String_Int32_String callback) { ... }
}
```

**Incompatible overloads (numbered interceptors):**
```csharp
public sealed class ProcessorBase_Transform1Interceptor  // Transform(int) -> string
{
    public delegate string TransformDelegate(int @value);
    public MethodCallBuilderImpl OnCall(TransformDelegate callback) { ... }
}

public sealed class ProcessorBase_Transform2Interceptor  // Transform(string) -> int
{
    public delegate int TransformDelegate(string text);
    public MethodCallBuilderImpl OnCall(TransformDelegate callback) { ... }
}
```

### All Checklist Items

**Phase 4 checklist:** 100% complete
- [x] OpenGenericOverloadTests uses new API
- [x] InlineClassOverloadTests created with 11 tests
- [x] Edge case tests for compatible/incompatible overloads
- [x] All tests pass

**Files created:**
- `/home/keithvoels/neatoodotnet/KnockOff/src/Design/Design.Domain/Abstractions/ProcessorBase.cs`
- `/home/keithvoels/neatoodotnet/KnockOff/src/Design/Design.Tests/GenericOverloadTests/InlineClassOverloadTests.cs`
