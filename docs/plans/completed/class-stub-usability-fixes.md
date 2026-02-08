# Class Stub Usability: Event AccessModifier + KO0201 Diagnostic

**Date:** 2026-02-07
**Related Todo:** [Class Stub Usability](../todos/completed/class-stub-usability.md)
**Status:** Complete
**Last Updated:** 2026-02-07

---

## Overview

Two focused fixes to improve class stub usability:

1. **Event AccessModifier bug** -- Protected virtual events on class stubs generate `public override event` instead of `protected override event`, causing CS0507. Methods, properties, and indexers already handle AccessModifier correctly through the pipeline; events are the gap.

2. **KO0201 diagnostic** -- `[KnockOff]` on a class with a concrete base type silently generates nothing. Should emit a diagnostic guiding the user to `[KnockOffBase<T>]` or `[KnockOff<T>]`.

---

## Approach

### Fix 1: Event AccessModifier

The root cause is that `EventMemberInfo` does not carry an `AccessModifier` field and `InlineClassImplEventModel` does not carry one either. The renderers (`ClassRenderer.RenderImplEventOverride` and `StandaloneClassRenderer.RenderImplEventOverride`) hardcode `public override event`.

Compare with how properties work:
- `ClassMemberInfo.FromProperty()` extracts `DeclaredAccessibility` to an `AccessModifier` string
- `InlineClassImplPropertyModel` carries `AccessModifier`
- `ClassRenderer.RenderImplPropertyOverride` uses `{prop.AccessModifier} override`

Events must follow this same pattern:

1. Add `AccessModifier` field to `EventMemberInfo` record, extracted from `eventSymbol.DeclaredAccessibility` in `FromEvent()`
2. Add `AccessModifier` field to `InlineClassImplEventModel` record
3. Pass `AccessModifier` from `EventMemberInfo` through the builders (`ClassModelBuilder`, `StandaloneClassModelBuilder`) when constructing `InlineClassImplEventModel`
4. Use `{evt.AccessModifier}` instead of hardcoded `public` in `ClassRenderer.RenderImplEventOverride` and `StandaloneClassRenderer.RenderImplEventOverride`

### Fix 2: KO0201 Diagnostic

In `TransformClass` (line 650), when `directInterfaces.Length == 0`, the method returns `null` with no diagnostic. If the class has a concrete base type (non-object), this is likely a user mistake -- they meant to use `[KnockOffBase<T>]` or inline `[KnockOff<T>]`.

The fix adds a check before the `return null`:
1. If `classSymbol.BaseType` is not `System.Object`, emit KO0201 with guidance
2. Return with the diagnostic (not `null`), so the diagnostic is reported

### Design.Domain Setup

Add protected members to `ServiceBase` so Design.Stubs can exercise protected member stubbing across class stub patterns. This is needed for the acceptance criteria code.

---

## Design

### Fix 1: Event AccessModifier Pipeline Changes

**Step 1: EventMemberInfo** (`src/Generator/Models/EventModels.cs`)

Add `AccessModifier` field to the record and extract from `DeclaredAccessibility` in `FromEvent()`:

```csharp
internal sealed record EventMemberInfo(
    string Name,
    string FullDelegateTypeName,
    EventDelegateKind DelegateKind,
    EquatableArray<ParameterInfo> DelegateParameters,
    string? ReturnTypeName,
    bool IsAsync,
    string DeclaringInterfaceFullName,
    string AccessModifier = "public")  // NEW: defaults to "public" for interfaces
```

In `FromEvent()`, extract from the symbol:

```csharp
var accessModifier = eventSymbol.DeclaredAccessibility switch
{
    Accessibility.Public => "public",
    Accessibility.Protected => "protected",
    Accessibility.ProtectedOrInternal => "protected internal",
    Accessibility.Internal => "internal",
    _ => "public"
};
```

**Step 2: InlineClassImplEventModel** (`src/Generator/Model/Inline/InlineClassStubModel.cs`)

Add `AccessModifier` field:

```csharp
internal sealed record InlineClassImplEventModel(
    string EventName,
    string DelegateType,
    string AccessModifier = "public");  // NEW
```

**Step 3: Builders** (`ClassModelBuilder.cs`, `StandaloneClassModelBuilder.cs`)

Both builders create `InlineClassImplEventModel` in their Impl events loop. Currently:

```csharp
implEvents.Add(new InlineClassImplEventModel(
    EventName: evt.Name,
    DelegateType: evt.FullDelegateTypeName.TrimEnd('?')));
```

Change to pass through AccessModifier:

```csharp
implEvents.Add(new InlineClassImplEventModel(
    EventName: evt.Name,
    DelegateType: evt.FullDelegateTypeName.TrimEnd('?'),
    AccessModifier: evt.AccessModifier));
```

**Step 4: Renderers** (`ClassRenderer.cs`, `StandaloneClassRenderer.cs`)

Both renderers have `RenderImplEventOverride` that currently hardcodes `public`:

```csharp
w.Line($"{indent}public override event {evt.DelegateType}? {evt.EventName}");
```

Change to use model's AccessModifier:

```csharp
w.Line($"{indent}{evt.AccessModifier} override event {evt.DelegateType}? {evt.EventName}");
```

### Fix 2: KO0201 Diagnostic

**Step 1: Diagnostic Descriptor** (`KnockOffGenerator.cs`)

Add KO0201 in the standalone stub diagnostics section:

```csharp
/// <summary>
/// KO0201: [KnockOff] applied to a class with a concrete base type.
/// The user should use [KnockOffBase<T>] or inline [KnockOff<T>] instead.
/// </summary>
private static readonly DiagnosticDescriptor KO0201_ClassWithBaseType = new(
    id: "KO0201",
    title: "[KnockOff] on class with base type",
    messageFormat: "Class '{0}' has base type '{1}'. Use [KnockOffBase<{1}>] for standalone class stubs or [KnockOff<{1}>] for inline class stubs.",
    category: "KnockOff",
    defaultSeverity: DiagnosticSeverity.Error,
    isEnabledByDefault: true);
```

**Step 2: TransformClass** (`KnockOffGenerator.Transform.cs`)

Before line 650 (`if (directInterfaces.Length == 0) return null;`), add:

```csharp
// Check for class with concrete base type but no interfaces (KO0201)
// User likely meant to use [KnockOffBase<T>] or [KnockOff<T>]
if (directInterfaces.Length == 0 &&
    classSymbol.BaseType is { } baseTypeForDiag &&
    baseTypeForDiag.SpecialType != SpecialType.System_Object)
{
    var location = classDeclaration.Identifier.GetLocation();
    var lineSpan = location.GetLineSpan();
    diagnostics.Add(new DiagnosticInfo(
        "KO0201",
        filePath,
        lineSpan.StartLinePosition.Line,
        lineSpan.StartLinePosition.Character,
        new[] { classSymbol.Name, baseTypeForDiag.ToDisplayString() }));

    return new KnockOffTypeInfo(
        Namespace: namespaceName,
        ClassName: classSymbol.Name,
        ContainingTypes: containingTypes,
        TypeParameters: classTypeParameters,
        Interfaces: new EquatableArray<InterfaceInfo>(Array.Empty<InterfaceInfo>()),
        Diagnostics: new EquatableArray<DiagnosticInfo>(diagnostics.ToArray()),
        FlatMembers: new EquatableArray<InterfaceMemberInfo>(Array.Empty<InterfaceMemberInfo>()),
        FlatEvents: new EquatableArray<EventMemberInfo>(Array.Empty<EventMemberInfo>()),
        UserOverrideMethods: EquatableArray<string>.Empty,
        UserOverrideProperties: EquatableArray<string>.Empty,
        Strict: strict);
}
```

**Step 3: Diagnostic Dispatch** (`KnockOffGenerator.GenerateInline.cs`)

Add KO0201 to the diagnostic descriptor lookup:

```csharp
"KO0201" => KO0201_ClassWithBaseType,
```

### Design.Domain Changes

Add protected virtual and protected abstract members to `ServiceBase`:

```csharp
// Protected members for demonstrating access modifier preservation
protected virtual event EventHandler? InternalStateChanged;
protected virtual string Tag { get; set; } = "";
protected abstract string GetInternalId();
protected virtual string FormatTag() => $"[{Tag}]";
protected virtual string this[int index] { get => ""; set { } }
```

---

## Architectural Verification

### Nine Patterns Analysis

| # | Pattern | Fix 1 (Event AccessModifier) | Fix 2 (KO0201) |
|---|---------|------------------------------|----------------|
| 1 | Standalone | N/A (interfaces have no access modifiers on events) | Yes -- this is the affected pipeline |
| 2 | Generic Standalone | N/A (interfaces) | Yes -- same TransformClass pipeline |
| 3 | Standalone Class | **Yes** -- StandaloneClassModelBuilder + StandaloneClassRenderer | N/A (different attribute) |
| 4 | Generic Standalone Class | **Yes** -- same StandaloneClassModelBuilder pipeline | N/A (different attribute) |
| 5 | Inline Interface | N/A (interfaces) | N/A (different attribute, TransformInlineStubClass) |
| 6 | Inline Class | **Yes** -- ClassModelBuilder + ClassRenderer | N/A (different attribute) |
| 7 | Inline Delegate | N/A | N/A |
| 8 | Open Generic Interface | N/A (interfaces) | N/A (different attribute) |
| 9 | Open Generic Class | **Yes** -- same ClassModelBuilder pipeline | N/A (different attribute) |

### Pipeline Verification

**Fix 1 -- Event AccessModifier affects these pipelines:**

| Pipeline | Transform | Builder | Renderer | Status |
|----------|-----------|---------|----------|--------|
| Inline class (5, 6) | `ExtractClassStubInfo` | `ClassModelBuilder` line 175 | `ClassRenderer` line 677 | Needs fix |
| Open generic class (8, 9) | Same `ExtractClassStubInfo` | Same `ClassModelBuilder` | Same `ClassRenderer` | Needs fix |
| Standalone class (3, 4) | `TransformStandaloneClass` | `StandaloneClassModelBuilder` line 198 | `StandaloneClassRenderer` line 910 | Needs fix |

All three pipelines use the same `EventMemberInfo.FromEvent()` and `InlineClassImplEventModel` types, so the model changes are shared. Each pipeline's builder constructs `InlineClassImplEventModel` separately, so all three must pass through AccessModifier.

**Fix 2 -- KO0201 affects this pipeline:**

| Pipeline | File | Method |
|----------|------|--------|
| Standalone (1, 2) | `KnockOffGenerator.Transform.cs` | `TransformClass` line 650 |

### Design.Stubs Compilation Verification

**Build command:** `dotnet build src/Design/Design.Stubs`

**Fix 1 -- Protected events on class stubs:**

All errors are CS0507: `cannot change access modifiers when overriding 'protected' inherited member 'ServiceBase.InternalStateChanged'`

| Pattern | Stub File | Generated File | Status |
|---------|-----------|---------------|--------|
| Standalone Class (3) | `ProtectedMemberStubs.cs` | `ProtectedMemberServiceStub.g.cs:3508` | **Needs Implementation** -- CS0507 |
| Standalone Class (3) | `AllPatterns.cs` | `StandaloneServiceStub.g.cs:3508` | **Needs Implementation** -- CS0507 (existing stub, newly affected by ServiceBase change) |
| Standalone Class (3) | `StandaloneClassUserMethods.cs` | `StandaloneClassUserMethodStub.g.cs:3497` | **Needs Implementation** -- CS0507 (existing stub, newly affected) |
| Inline Class (6) | `ProtectedMemberStubs.cs` | `InlineProtectedMemberDemo.Stubs.g.cs:3513` | **Needs Implementation** -- CS0507 |
| Inline Class (6) | `AllPatterns.cs` | `InlineClassExample.Stubs.g.cs:3513` | **Needs Implementation** -- CS0507 (existing stub, newly affected) |
| Open Generic Class (9) | `AllPatterns.cs` | `OpenGenericClassExample.Stubs.g.cs:3513` | **Needs Implementation** -- CS0507 (existing stub, uses same ClassModelBuilder pipeline) |

**Total: 18 CS0507 errors (6 stubs x 3 target frameworks)**

All errors are on the same line pattern in generated code:
```
public override event global::System.EventHandler? InternalStateChanged
```
Should be:
```
protected override event global::System.EventHandler? InternalStateChanged
```

**Fix 2 -- KO0201 diagnostic:**

| Pattern | Status | Notes |
|---------|--------|-------|
| Standalone (1, 2) | **Needs Implementation** | KO0201 diagnostic is not testable via Design.Stubs compilation. Developer should add unit test in `src/Tests/KnockOffTests/` that verifies the diagnostic is emitted when `[KnockOff]` is applied to a class with a concrete base type. |

### Impact on Existing Tests

**KnockOffTests:** Not affected. Test stubs use their own domain classes, not Design.Domain.ServiceBase.

**Design.Stubs:** Fails to build (18 CS0507 errors). This is expected and intentional -- the failing code IS the acceptance criteria. After the fix, Design.Stubs must compile with zero errors.

**Design.Tests:** Cannot build because Design.Stubs fails. After the fix, `dotnet test src/Design/Design.Tests` must also pass.

### Design.Domain Change Notes

Added `CA1070` to suppressed warnings in `Design.Domain.csproj`. The CA1070 analyzer rule discourages virtual events, but we intentionally use a protected virtual event to test access modifier preservation. The suppression is documented in the csproj comment.

### Breaking Changes

**None.** Both changes are additive:
- Fix 1 adds a field with a default value to two records and changes rendering behavior only for non-public events (which currently fail to compile anyway)
- Fix 2 turns a silent failure into a helpful diagnostic

### Codebase Analysis

Files examined:
- `src/Generator/Models/EventModels.cs` -- EventMemberInfo record, no AccessModifier field today
- `src/Generator/Models/ClassModels.cs` -- ClassMemberInfo has AccessModifier; pattern to follow for events
- `src/Generator/Model/Inline/InlineClassStubModel.cs` -- InlineClassImplEventModel (lines 273-277), only EventName and DelegateType
- `src/Generator/Builder/ClassModelBuilder.cs` -- lines 172-177, constructs InlineClassImplEventModel without AccessModifier
- `src/Generator/Builder/StandaloneClassModelBuilder.cs` -- lines 196-200, same pattern
- `src/Generator/Renderer/ClassRenderer.cs` -- line 677, hardcoded `public override event`
- `src/Generator/Renderer/StandaloneClassRenderer.cs` -- line 910, hardcoded `public override event`
- `src/Generator/KnockOffGenerator.cs` -- diagnostic descriptors (KO0200 exists, KO0201 is the next number)
- `src/Generator/KnockOffGenerator.Transform.cs` -- line 650, silent `return null` when no interfaces
- `src/Generator/KnockOffGenerator.GenerateInline.cs` -- diagnostic dispatch mapping
- `src/Design/Design.Domain/Abstractions/ServiceBase.cs` -- current public members only
- `src/Design/Design.Domain/Abstractions/EventServiceBase.cs` -- public abstract events only
- `src/Design/Design.Stubs/Events/EventApiConsistencyVerification.cs` -- existing event patterns, all public
- `src/Design/Design.Stubs/StubPatterns/AllPatterns.cs` -- nine pattern documentation

---

## Design.Stubs Acceptance Criteria

### Failing Code Created

The following files contain code that will only compile after the fixes are implemented:

1. **`src/Design/Design.Stubs/ProtectedMembers/ProtectedMemberStubs.cs`** -- Exercises protected virtual events on class stubs (patterns 3 and 6). Currently fails with CS0507 because the generator emits `public override event` instead of `protected override event`.

2. **`src/Design/Design.Domain/Abstractions/ServiceBase.cs`** -- Updated with protected members (virtual event, virtual property, abstract method, virtual method, virtual indexer).

3. **Diagnostic verification** -- KO0201 cannot be directly tested via Design.Stubs compilation since it's a diagnostic, not generated code. The developer should add a unit test in KnockOffTests.

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-02-07

### Codebase Investigation

**Files Examined:**
- `src/Generator/Models/EventModels.cs` - Confirmed: no AccessModifier field on EventMemberInfo
- `src/Generator/Models/ClassModels.cs` - Confirmed: ClassMemberInfo has AccessModifier; pattern to follow
- `src/Generator/Model/Inline/InlineClassStubModel.cs` - Confirmed: InlineClassImplEventModel lacks AccessModifier (line 273-277); compare InlineClassImplPropertyModel (line 182) and InlineClassImplMethodModel (line 237) which have it
- `src/Generator/Builder/ClassModelBuilder.cs` - Confirmed: lines 175-177, constructs InlineClassImplEventModel without AccessModifier
- `src/Generator/Builder/StandaloneClassModelBuilder.cs` - Confirmed: lines 198-200, same pattern
- `src/Generator/Renderer/ClassRenderer.cs` - Confirmed: line 677, hardcoded `public override event`
- `src/Generator/Renderer/StandaloneClassRenderer.cs` - Confirmed: line 910, hardcoded `public override event`
- `src/Generator/KnockOffGenerator.cs` - Confirmed: KO0200 at line 90; KO0201 is next
- `src/Generator/KnockOffGenerator.Transform.cs` - Confirmed: line 650, silent `return null`; line 722, KO0200 pattern to follow
- `src/Generator/KnockOffGenerator.GenerateInline.cs` - Confirmed: ReportDiagnostics switch needs KO0201 entry
- `src/Design/Design.Stubs/ProtectedMembers/ProtectedMemberStubs.cs` - Confirmed: acceptance criteria code exists
- `src/Design/Design.Domain/Abstractions/ServiceBase.cs` - Confirmed: protected members added

**Design.Stubs Verification:**
- Fix 1 (CS0507 errors): Built Design.Stubs, confirmed 18 CS0507 errors across 6 stubs x 3 frameworks. All errors match the plan's description.
- Fix 2 (KO0201): Architect correctly noted this is a diagnostic, not testable via compilation.

### Observations

1. **Non-blocking:** The plan's switch default `_ => "public"` differs from ClassMemberInfo's `_ => "protected"`, but this is functionally correct since EventMemberInfo serves both interface events (public) and class events, and the default case is unreachable in practice (Private members are excluded by virtual check, ProtectedAndInternal by IsMemberAccessible).

2. Existing bug noted (not in scope): KO2007 and KO2008 diagnostic descriptors exist in KnockOffGenerator.cs but are NOT in the ReportDiagnostics switch statement, meaning they are silently dropped. Unrelated to this plan.

### Verdict

**Approved.** The plan is precise, follows established patterns, and has verified acceptance criteria. All file paths, line numbers, and code changes checked against the codebase.

---

## Implementation Contract

**Created:** 2026-02-07
**Approved by:** knockoff-developer

### Design.Stubs Acceptance Criteria

These failing stubs must compile after implementation:

- [ ] `src/Design/Design.Stubs/ProtectedMembers/ProtectedMemberStubs.cs` - ProtectedMemberServiceStub (pattern 3): CS0507 on InternalStateChanged
- [ ] `src/Design/Design.Stubs/StubPatterns/AllPatterns.cs` - StandaloneServiceStub (pattern 3): CS0507 on InternalStateChanged
- [ ] `src/Design/Design.Stubs/StandaloneClassUserMethods/StandaloneClassUserMethods.cs` - StandaloneClassUserMethodStub (pattern 3): CS0507 on InternalStateChanged
- [ ] `src/Design/Design.Stubs/ProtectedMembers/ProtectedMemberStubs.cs` - InlineProtectedMemberDemo (pattern 6): CS0507 on InternalStateChanged
- [ ] `src/Design/Design.Stubs/StubPatterns/AllPatterns.cs` - InlineClassExample (pattern 6): CS0507 on InternalStateChanged
- [ ] `src/Design/Design.Stubs/StubPatterns/AllPatterns.cs` - OpenGenericClassExample (pattern 9): CS0507 on InternalStateChanged

### In Scope

**Fix 1: Event AccessModifier**

- [ ] Add `AccessModifier` field to `EventMemberInfo` record in `src/Generator/Models/EventModels.cs`
- [ ] Extract `DeclaredAccessibility` in `EventMemberInfo.FromEvent()`
- [ ] Add `AccessModifier` field to `InlineClassImplEventModel` record in `src/Generator/Model/Inline/InlineClassStubModel.cs`
- [ ] Pass `AccessModifier` through in `ClassModelBuilder.cs` (line ~175)
- [ ] Pass `AccessModifier` through in `StandaloneClassModelBuilder.cs` (line ~198)
- [ ] Replace hardcoded `public` with `{evt.AccessModifier}` in `ClassRenderer.cs` (line 677)
- [ ] Replace hardcoded `public` with `{evt.AccessModifier}` in `StandaloneClassRenderer.cs` (line 910)
- [ ] **Checkpoint:** `dotnet build src/Design/Design.Stubs` -- 0 errors

**Fix 2: KO0201 Diagnostic**

- [ ] Add `KO0201_ClassWithBaseType` descriptor in `KnockOffGenerator.cs`
- [ ] Add KO0201 check in `TransformClass` in `KnockOffGenerator.Transform.cs` (before line 650)
- [ ] Add `"KO0201"` to `ReportDiagnostics` switch in `KnockOffGenerator.GenerateInline.cs`
- [ ] Add unit test in `src/Tests/KnockOffTests/` verifying KO0201 is emitted

**Final Verification:**

- [ ] **Checkpoint:** `dotnet test src/Tests/KnockOffTests` -- all pass
- [ ] **Checkpoint:** `dotnet test src/Design/Design.Tests` -- all pass
- [ ] **Checkpoint:** `dotnet build src/Design/Design.Stubs` -- 0 errors

### Explicitly Out of Scope

- KO2007/KO2008 silent diagnostic drop bug (existing, unrelated)
- Protected event unit tests in KnockOffTests beyond what exists (Design.Stubs provides compilation coverage)
- `private protected` event handling (not reachable via current `IsMemberAccessible` filtering)
- Documentation updates (deferred to Phase 7)

### Verification Gates

1. After Fix 1: `dotnet build src/Design/Design.Stubs` succeeds with 0 errors
2. After Fix 2: KnockOffTests pass including the new KO0201 test
3. Final: All tests pass, Design.Stubs compiles, Design.Tests pass

### Stop Conditions

If any of these occur, STOP and report:
- Out-of-scope test fails (KnockOffTests test not related to events or KO0201)
- Architectural contradiction discovered (e.g., event model used in unexpected place)
- Generated code does not compile after fix

---

## Implementation Progress

**Started:** 2026-02-07

### Phase 1: Event AccessModifier Fix

- [x] Added `AccessModifier` field (default `"public"`) to `EventMemberInfo` record in `src/Generator/Models/EventModels.cs`
- [x] Added `DeclaredAccessibility` extraction switch in `EventMemberInfo.FromEvent()` -- extracts public/protected/protected internal/internal
- [x] Updated both `return` paths in `FromEvent()` to pass `AccessModifier`
- [x] Added `AccessModifier` field (default `"public"`) to `InlineClassImplEventModel` record in `src/Generator/Model/Inline/InlineClassStubModel.cs`
- [x] Updated `ClassModelBuilder.cs` (line ~175) to pass `AccessModifier: evt.AccessModifier` when constructing `InlineClassImplEventModel`
- [x] Updated `StandaloneClassModelBuilder.cs` (line ~198) to pass `AccessModifier: evt.AccessModifier` when constructing `InlineClassImplEventModel`
- [x] Replaced `public override event` with `{evt.AccessModifier} override event` in `ClassRenderer.cs` (line 677)
- [x] Replaced `public override event` with `{evt.AccessModifier} override event` in `StandaloneClassRenderer.cs` (line 910)
- [x] **Verification:** `dotnet build src/Design/Design.Stubs` -- 0 errors (was 18 CS0507 errors)

### Phase 2: KO0201 Diagnostic

- [x] Added `KO0201_ClassWithBaseType` diagnostic descriptor in `KnockOffGenerator.cs` (after KO0200)
- [x] Added KO0201 check in `TransformClass` in `KnockOffGenerator.Transform.cs` -- checks for non-object base type when `directInterfaces.Length == 0`
- [x] Added `"KO0201" => KO0201_ClassWithBaseType` to `ReportDiagnostics` switch in `KnockOffGenerator.GenerateInline.cs`
- [x] Created `src/Tests/KnockOffTests/DiagnosticTests.cs` with 3 tests:
  - `KO0201_ClassWithBaseType_NoInterfaces_EmitsDiagnostic` -- verifies diagnostic is emitted
  - `KO0201_ClassWithBaseType_WithInterfaces_DoesNotEmitKO0201` -- verifies no false positive when interfaces present
  - `KO0201_ClassWithNoBaseType_NoInterfaces_DoesNotEmitKO0201` -- verifies no false positive for plain classes
- [x] **Verification:** All 3 diagnostic tests pass on all 3 TFMs (9 total)

### Final Verification

- [x] `dotnet test src/Tests/KnockOffTests` -- all pass (1193 net9.0, 1192 net8.0, 1193 net10.0)
- [x] `dotnet test src/Design/Design.Tests` -- all pass (259 per TFM, 777 total)
- [x] `dotnet build src/Design/Design.Stubs` -- 0 errors, 0 warnings

---

## Completion Evidence

### Test Results

**KnockOffTests:**
- net8.0: 1192 passed, 0 failed, 0 skipped
- net9.0: 1193 passed, 0 failed, 0 skipped
- net10.0: 1193 passed, 0 failed, 0 skipped

**Design.Tests:**
- net8.0: 259 passed, 0 failed, 0 skipped
- net9.0: 259 passed, 0 failed, 0 skipped
- net10.0: 259 passed, 0 failed, 0 skipped

**Design.Stubs:** Build succeeded, 0 errors, 0 warnings (all 3 TFMs)

### Generated Code Evidence

**Fix 1 -- Protected events now generate correct access modifier:**

Before (18 CS0507 errors):
```
public override event global::System.EventHandler? InternalStateChanged
```

After (compiles successfully):
```
protected override event global::System.EventHandler? InternalStateChanged
```

Verified in 6 generated files:
- `ProtectedMemberServiceStub.g.cs:3508` -- `protected override event`
- `StandaloneServiceStub.g.cs:3508` -- `protected override event`
- `StandaloneClassUserMethodStub.g.cs:3497` -- `protected override event`
- `InlineProtectedMemberDemo.Stubs.g.cs:3513` -- `protected override event`
- `InlineClassExample.Stubs.g.cs:3513` -- `protected override event`
- `OpenGenericClassExample.Stubs.g.cs:3513` -- `protected override event`

Public events continue to generate correctly:
- `EventServiceBaseStub.g.cs:758` -- `public override event` (unchanged)

**Fix 2 -- KO0201 diagnostic:**
Verified via CSharpGeneratorDriver-based unit tests:
- Emits KO0201 with severity Error when `[KnockOff]` is applied to class with concrete base type and no interfaces
- Does NOT emit KO0201 when class has interfaces (even with base type)
- Does NOT emit KO0201 when class has no base type

### All Contract Items Confirmed Complete

**Design.Stubs Acceptance Criteria:**
- [x] ProtectedMemberServiceStub (pattern 3): CS0507 resolved
- [x] StandaloneServiceStub (pattern 3): CS0507 resolved
- [x] StandaloneClassUserMethodStub (pattern 3): CS0507 resolved
- [x] InlineProtectedMemberDemo (pattern 6): CS0507 resolved
- [x] InlineClassExample (pattern 6): CS0507 resolved
- [x] OpenGenericClassExample (pattern 9): CS0507 resolved

### Files Modified

**Generator (production code):**
1. `src/Generator/Models/EventModels.cs` -- Added `AccessModifier` field and extraction
2. `src/Generator/Model/Inline/InlineClassStubModel.cs` -- Added `AccessModifier` to `InlineClassImplEventModel`
3. `src/Generator/Builder/ClassModelBuilder.cs` -- Pass `AccessModifier` through
4. `src/Generator/Builder/StandaloneClassModelBuilder.cs` -- Pass `AccessModifier` through
5. `src/Generator/Renderer/ClassRenderer.cs` -- Use `{evt.AccessModifier}` instead of hardcoded `public`
6. `src/Generator/Renderer/StandaloneClassRenderer.cs` -- Use `{evt.AccessModifier}` instead of hardcoded `public`
7. `src/Generator/KnockOffGenerator.cs` -- Added KO0201 diagnostic descriptor
8. `src/Generator/KnockOffGenerator.Transform.cs` -- Added KO0201 check in TransformClass
9. `src/Generator/KnockOffGenerator.GenerateInline.cs` -- Added KO0201 to dispatch switch

**Tests:**
10. `src/Tests/KnockOffTests/DiagnosticTests.cs` -- New file: 3 tests for KO0201 diagnostic

---

## Architect Verification

**Verified:** 2026-02-07
**Verdict:** VERIFIED

### Independent Test Results

All builds and tests run independently by the architect (not trusting developer's reported results):

- **KnockOff.sln build:** Succeeded, 0 errors, 0 warnings
- **Design.Stubs build:** Succeeded, 0 errors, 0 warnings
- **KnockOffTests:** net8.0: 1192 passed / 0 failed, net9.0: 1193 passed / 0 failed, net10.0: 1193 passed / 0 failed
- **Design.Tests:** net8.0: 259 passed / 0 failed, net9.0: 259 passed / 0 failed, net10.0: 259 passed / 0 failed

Zero test failures across all projects and all target frameworks.

### Design Match

**Fix 1 -- Event AccessModifier:**
- `EventMemberInfo` record: `AccessModifier` field added with `"public"` default, `DeclaredAccessibility` extraction implemented. Matches plan.
- `InlineClassImplEventModel` record: `AccessModifier` field added with `"public"` default. Matches plan.
- `ClassModelBuilder.cs` line 178: passes `AccessModifier: evt.AccessModifier`. Matches plan.
- `StandaloneClassModelBuilder.cs` line 201: passes `AccessModifier: evt.AccessModifier`. Matches plan.
- `ClassRenderer.cs` line 677: uses `{evt.AccessModifier} override event`. Matches plan.
- `StandaloneClassRenderer.cs` line 910: uses `{evt.AccessModifier} override event`. Matches plan.

**Fix 2 -- KO0201 Diagnostic:**
- `KnockOffGenerator.cs` line 102: `KO0201_ClassWithBaseType` descriptor with correct message format. Matches plan.
- `KnockOffGenerator.Transform.cs` line 651-677: KO0201 check before interface check, returns KnockOffTypeInfo with diagnostic. Matches plan.
- `KnockOffGenerator.GenerateInline.cs` line 25: `"KO0201" => KO0201_ClassWithBaseType` in dispatch switch. Matches plan.

### Generated Code Spot-Check

All 6 generated files confirmed to emit `protected override event` (not `public override event`):
- `ProtectedMemberServiceStub.g.cs:3508` -- `protected override event`
- `StandaloneServiceStub.g.cs:3508` -- `protected override event`
- `StandaloneClassUserMethodStub.g.cs:3497` -- `protected override event`
- `InlineProtectedMemberDemo.Stubs.g.cs:3513` -- `protected override event`
- `InlineClassExample.Stubs.g.cs:3513` -- `protected override event`
- `OpenGenericClassExample.Stubs.g.cs:3513` -- `protected override event`

Public events remain unaffected:
- `EventServiceBaseStub.g.cs:758` -- `public override event` (correct, unchanged)

### Diagnostic Test Verification

New file `src/Tests/KnockOffTests/DiagnosticTests.cs` exists with 3 well-structured tests:
1. `KO0201_ClassWithBaseType_NoInterfaces_EmitsDiagnostic` -- verifies diagnostic emitted with correct severity and message
2. `KO0201_ClassWithBaseType_WithInterfaces_DoesNotEmitKO0201` -- verifies no false positive
3. `KO0201_ClassWithNoBaseType_NoInterfaces_DoesNotEmitKO0201` -- verifies no false positive for plain classes

All 3 tests pass on all 3 TFMs (net8.0 has 1 fewer test elsewhere, unrelated).
